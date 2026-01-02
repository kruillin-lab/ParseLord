using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics.Arm;

namespace AutoRotationPlugin;

/// <summary>
/// WRATH ARCHITECTURE: Centralized game state access layer.
/// Now uses ECommons Svc for cleaner service access.
/// </summary>
public static class GameState
{
    public readonly record struct StatusSnapshot(uint StatusId, float RemainingTime, byte Stacks, uint SourceId);

    #region Player State

    // WRATH ARCHITECTURE: Use Svc.ClientState.LocalPlayer directly
    public static IPlayerCharacter? LocalPlayer => Svc.ClientState.LocalPlayer;

    public static float PlayerHPPercent => LocalPlayer is { MaxHp: > 0 } p
        ? (float)p.CurrentHp / p.MaxHp
        : 1f;

    public static uint PlayerMP => LocalPlayer?.CurrentMp ?? 0;

    public static bool InCombat => LocalPlayer?.StatusFlags.HasFlag(StatusFlags.InCombat) ?? false;
    public static bool IsDead => LocalPlayer?.IsDead ?? true;
    public static bool IsCasting => LocalPlayer?.IsCasting ?? false;
    public static uint JobId => LocalPlayer?.ClassJob.RowId ?? 0;
    public static Vector3 Position => LocalPlayer?.Position ?? Vector3.Zero;

    #endregion

    #region Target State

    public static IGameObject? Target => Svc.Targets.Target;
    public static IBattleChara? TargetAsBattleChara => Target as IBattleChara;

    public static float TargetDistance => Target != null
        ? Vector3.Distance(Position, Target.Position)
        : float.MaxValue;

    public static bool HasTarget => Target != null;

    public static bool IsHostile(IBattleChara character)
    {
        if (!character.IsTargetable) return false;
        if (IsPartyMember(character)) return false;
        if (character.ObjectKind == ObjectKind.BattleNpc) return true;
        return false;
    }

    #endregion

    #region Status Effects

    public static bool HasStatus(uint statusId) => GetPlayerStatus(statusId) != null;

    public static StatusSnapshot? GetPlayerStatus(uint statusId)
    {
        if (LocalPlayer == null) return null;
        foreach (var status in LocalPlayer.StatusList)
        {
            if (status.StatusId == statusId)
                return new StatusSnapshot(status.StatusId, status.RemainingTime, (byte)Math.Min(status.Param, (ushort)byte.MaxValue), status.SourceId);
        }
        return null;
    }

    public static float GetStatusDuration(uint statusId)
    {
        var status = GetPlayerStatus(statusId);
        return status?.RemainingTime ?? 0f;
    }

    public static float GetMyStatusDurationOnTarget(uint statusId)
    {
        if (TargetAsBattleChara == null || LocalPlayer == null) return 0f;
        var mySourceId = unchecked((uint)LocalPlayer.GameObjectId);

        foreach (var status in TargetAsBattleChara.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == mySourceId)
                return status.RemainingTime;
        }
        return 0f;
    }

    #endregion

    #region Party & Objects

    public static IEnumerable<IBattleChara> PartyMembers
    {
        get
        {
            // Use Svc.Party instead of Plugin.PartyList
            if (Svc.Party.Length > 0)
            {
                foreach (var member in Svc.Party)
                {
                    if (member?.GameObject is IBattleChara bc)
                        yield return bc;
                }
            }
            else if (LocalPlayer != null)
            {
                yield return LocalPlayer;
            }
        }
    }

    public static bool IsPartyMember(IBattleChara character)
    {
        return Svc.Party.Any(p => p.EntityId == character.GameObjectId);
    }

    public static int HostileCountAroundPosition(Vector3 position, float range)
    {
        // Use Svc.Objects instead of Plugin.ObjectTable
        return Svc.Objects
            .OfType<IBattleChara>()
            .Count(obj => IsHostile(obj) && Vector3.Distance(position, obj.Position) <= range);
    }

    /// <summary>
    /// Convenience wrapper used by rotations: count hostile enemies around the player.
    /// </summary>
    public static int GetHostileCountAround(IPlayerCharacter player, float range)
    {
        return HostileCountAroundPosition(player.Position, range);
    }


    #endregion
}