using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;

namespace AutoRotationPlugin;

/// <summary>
/// Centralized game state access layer.
/// Provides clean, null-safe abstractions over Dalamud services.
/// Single point of change when APIs deprecate or change.
/// </summary>
public static class GameState
{
    /// <summary>
    /// Lightweight, API-stable snapshot of a status effect.
    /// We avoid returning Dalamud's internal Status types directly.
    /// </summary>
    public readonly record struct StatusSnapshot(uint StatusId, float RemainingTime, byte Stacks, uint SourceId);

    #region Player State
    
    /// <summary>
    /// The local player character. Null if not logged in.
    /// Uses the non-deprecated IObjectTable.LocalPlayer.
    /// </summary>
    public static IPlayerCharacter? LocalPlayer => Plugin.ObjectTable?.LocalPlayer;
    
    /// <summary>
    /// Current player HP as a percentage (0.0 - 1.0).
    /// </summary>
    public static float PlayerHPPercent => LocalPlayer is { MaxHp: > 0 } p 
        ? (float)p.CurrentHp / p.MaxHp 
        : 1f;
    
    /// <summary>
    /// Current player MP value.
    /// </summary>
    public static uint PlayerMP => LocalPlayer?.CurrentMp ?? 0;
    
    /// <summary>
    /// Current player MP as a percentage (0.0 - 1.0).
    /// </summary>
    public static float PlayerMPPercent => LocalPlayer is { MaxMp: > 0 } p 
        ? (float)p.CurrentMp / p.MaxMp 
        : 1f;
    
    /// <summary>
    /// Whether the player is currently in combat.
    /// </summary>
    public static bool InCombat => LocalPlayer?.StatusFlags.HasFlag(StatusFlags.InCombat) ?? false;
    
    /// <summary>
    /// Whether the player is currently dead.
    /// </summary>
    public static bool IsDead => LocalPlayer?.IsDead ?? true;
    
    /// <summary>
    /// Whether the player is currently casting.
    /// </summary>
    public static bool IsCasting => LocalPlayer?.IsCasting ?? false;
    
    /// <summary>
    /// The player's current job ID.
    /// </summary>
    public static uint JobId => LocalPlayer?.ClassJob.RowId ?? 0;
    
    /// <summary>
    /// The player's current level.
    /// </summary>
    public static byte Level => LocalPlayer?.Level ?? 0;
    
    /// <summary>
    /// The player's current position.
    /// </summary>
    public static Vector3 Position => LocalPlayer?.Position ?? Vector3.Zero;
    
    #endregion
    
    #region Target State
    
    /// <summary>
    /// The player's current hard target.
    /// </summary>
    public static IGameObject? Target => LocalPlayer?.TargetObject;
    
    /// <summary>
    /// The current target as a battle character (for HP, status, etc).
    /// </summary>
    public static IBattleChara? TargetAsBattleChara => Target as IBattleChara;
    
    /// <summary>
    /// Current target HP as a percentage (0.0 - 1.0).
    /// </summary>
    public static float TargetHPPercent => TargetAsBattleChara is { MaxHp: > 0 } t 
        ? (float)t.CurrentHp / t.MaxHp 
        : 1f;
    
    /// <summary>
    /// Whether we have a valid target.
    /// </summary>
    public static bool HasTarget => Target != null;
    
    /// <summary>
    /// Whether the current target is hostile.
    /// </summary>
    public static bool TargetIsHostile => TargetAsBattleChara != null && IsHostile(TargetAsBattleChara);
    
    /// <summary>
    /// Distance to current target.
    /// </summary>
    public static float TargetDistance => Target != null 
        ? Vector3.Distance(Position, Target.Position) 
        : float.MaxValue;
    
    /// <summary>
    /// The soft target (mouseover priority target).
    /// </summary>
    public static IGameObject? SoftTarget => Plugin.TargetManager?.SoftTarget;
    
    /// <summary>
    /// The focus target.
    /// </summary>
    public static IGameObject? FocusTarget => Plugin.TargetManager?.FocusTarget;
    
    /// <summary>
    /// The mouseover target.
    /// </summary>
    public static IGameObject? MouseOverTarget => Plugin.TargetManager?.MouseOverTarget;
    
    #endregion
    
    #region Status Effects
    
    /// <summary>
    /// Get a status effect on the local player by ID.
    /// Returns null if not found.
    /// </summary>
    public static StatusSnapshot? GetPlayerStatus(uint statusId)
    {
        if (LocalPlayer == null) return null;
        
        foreach (var status in LocalPlayer.StatusList)
        {
            if (status.StatusId == statusId)
                return new StatusSnapshot(status.StatusId, status.RemainingTime, (byte)System.Math.Min(status.Param, (ushort)byte.MaxValue), status.SourceId);
        }
        return null;
    }
    
    /// <summary>
    /// Check if the player has a specific status effect.
    /// </summary>
    public static bool HasStatus(uint statusId) => GetPlayerStatus(statusId) != null;
    
    /// <summary>
    /// Get the remaining duration of a status effect on the player.
    /// Returns 0 if not found.
    /// </summary>
    public static float GetStatusDuration(uint statusId)
    {
        var status = GetPlayerStatus(statusId);
        return status?.RemainingTime ?? 0f;
    }
    
    /// <summary>
    /// Get the stack count of a status effect on the player.
    /// Returns 0 if not found.
    /// </summary>
    public static byte GetStatusStacks(uint statusId)
    {
        var status = GetPlayerStatus(statusId);
        return status?.Stacks ?? 0;
    }
    
    /// <summary>
    /// Get a status effect on a target by ID.
    /// Optionally filter by source (caster).
    /// </summary>
    public static StatusSnapshot? GetTargetStatus(IBattleChara target, uint statusId, uint? sourceId = null)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId)
            {
                if (sourceId == null || status.SourceId == sourceId)
                    return new StatusSnapshot(status.StatusId, status.RemainingTime, (byte)System.Math.Min(status.Param, (ushort)byte.MaxValue), status.SourceId);
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if the target has a status effect applied by us.
    /// </summary>
    public static bool TargetHasMyStatus(uint statusId)
    {
        if (TargetAsBattleChara == null || LocalPlayer == null) return false;
        // Dalamud exposes GameObjectId as ulong; status.SourceId is uint.
        // In practice, the object id fits in 32 bits, so this cast is safe.
        var mySourceId = unchecked((uint)LocalPlayer.GameObjectId);
        return GetTargetStatus(TargetAsBattleChara, statusId, mySourceId) != null;
    }
    
    /// <summary>
    /// Get remaining duration of our DoT/debuff on target.
    /// </summary>
    public static float GetMyStatusDurationOnTarget(uint statusId)
    {
        if (TargetAsBattleChara == null || LocalPlayer == null) return 0f;
        var mySourceId = unchecked((uint)LocalPlayer.GameObjectId);
        var status = GetTargetStatus(TargetAsBattleChara, statusId, mySourceId);
        return status?.RemainingTime ?? 0f;
    }
    
    #endregion
    
    #region Party State
    
    /// <summary>
    /// Get all party members as battle characters.
    /// </summary>
    public static IEnumerable<IBattleChara> PartyMembers
    {
        get
        {
            // In a party, use party list
            if (Plugin.PartyList.Length > 0)
            {
                foreach (var member in Plugin.PartyList)
                {
                    if (member?.GameObject is IBattleChara bc)
                        yield return bc;
                }
            }
            // Solo, just return the player
            else if (LocalPlayer != null)
            {
                yield return LocalPlayer;
            }
        }
    }
    
    /// <summary>
    /// Get the party member with the lowest HP percentage.
    /// </summary>
    public static IBattleChara? LowestHPPartyMember => PartyMembers
        .Where(m => !m.IsDead && m.CurrentHp > 0)
        .OrderBy(m => (float)m.CurrentHp / m.MaxHp)
        .FirstOrDefault();
    
    /// <summary>
    /// Get count of party members below a certain HP threshold.
    /// </summary>
    public static int PartyMembersBelowHP(float threshold) => PartyMembers
        .Count(m => !m.IsDead && (float)m.CurrentHp / m.MaxHp < threshold);
    
    /// <summary>
    /// Check if any party member needs healing (below threshold).
    /// </summary>
    public static bool PartyNeedsHealing(float threshold = 0.8f) => PartyMembersBelowHP(threshold) > 0;
    
    #endregion
    
    #region Enemy Detection
    
    /// <summary>
    /// Check if a character is hostile (enemy).
    /// </summary>
    public static bool IsHostile(IBattleChara character)
    {
        // Not targetable = not a valid enemy
        if (!character.IsTargetable) return false;
        
        // Party members are not hostile
        if (IsPartyMember(character)) return false;
        
        // Check object kind - BattleNpc are typically enemies
        if (character.ObjectKind == ObjectKind.BattleNpc)
            return true;
        
        return false;
    }
    
    /// <summary>
    /// Check if a character is a party member.
    /// </summary>
    public static bool IsPartyMember(IBattleChara character)
    {
        return Plugin.PartyList.Any(p => p.EntityId == character.GameObjectId);
    }
    
    /// <summary>
    /// Get count of hostile targets within range of the player.
    /// </summary>
    public static int HostileCountInRange(float range)
    {
        return Plugin.ObjectTable
            .OfType<IBattleChara>()
            .Count(obj => IsHostile(obj) && Vector3.Distance(Position, obj.Position) <= range);
    }
    
    /// <summary>
    /// Get count of hostile targets within range of a specific position.
    /// </summary>
    public static int HostileCountAroundPosition(Vector3 position, float range)
    {
        return Plugin.ObjectTable
            .OfType<IBattleChara>()
            .Count(obj => IsHostile(obj) && Vector3.Distance(position, obj.Position) <= range);
    }
    
    #endregion
    
    #region Role Helpers
    
    private static readonly uint[] TankJobs = { 1, 3, 19, 21, 32, 37 }; // GLA, MRD, PLD, WAR, DRK, GNB
    private static readonly uint[] HealerJobs = { 6, 24, 28, 33, 40 }; // CNJ, WHM, SCH, AST, SGE
    
    /// <summary>
    /// Check if a character is a tank.
    /// </summary>
    public static bool IsTank(IBattleChara character) => TankJobs.Contains(character.ClassJob.RowId);
    
    /// <summary>
    /// Check if a character is a healer.
    /// </summary>
    public static bool IsHealer(IBattleChara character) => HealerJobs.Contains(character.ClassJob.RowId);
    
    /// <summary>
    /// Check if a character is a DPS.
    /// </summary>
    public static bool IsDPS(IBattleChara character) => !IsTank(character) && !IsHealer(character);
    
    /// <summary>
    /// Get the main tank (first tank in party, or tank with most enmity).
    /// </summary>
    public static IBattleChara? MainTank => PartyMembers.FirstOrDefault(IsTank);
    
    #endregion
}
