using AutoRotationPlugin.Rotations;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;

namespace AutoRotationPlugin.Rotations.Jobs;

public class WhiteMageRotation : IRotation
{
    public uint JobId => 24;

    #region IDs (Truncated for brevity, paste your IDs here or use previous file)
    // ... [Paste your existing Action/Status IDs here, they are fine] ...
    // Note: I am not listing all IDs to save space, but DO NOT delete them from your file.
    // The key logic change is below in GetLowestHPMember
    #endregion

    // ... [Previous Action IDs Code] ...
    // GCDs
    private const uint GlareIII = 25859;
    private const uint GlareIV = 37009;
    private const uint Dia = 16532;
    private const uint HolyIII = 25860;
    private const uint AfflatusMisery = 16535;
    private const uint CureII = 135;
    private const uint MedicaIII = 37010;
    private const uint AfflatusSolace = 16531;
    private const uint AfflatusRapture = 16534;
    private const uint Regen = 137;
    // oGCDs
    private const uint Assize = 3571;
    private const uint PresenceOfMind = 136;
    private const uint LucidDreaming = 7562;
    private const uint ThinAir = 7430;
    private const uint Benediction = 140;
    private const uint Tetragrammaton = 3570;
    private const uint DivineBenison = 7432;
    private const uint Aquaveil = 25861;
    private const uint DivineCaress = 37011;
    // Status
    private const uint DiaDot = 1871;
    private const uint SacredSight = 3879;
    private const uint DivineGrace = 3878;

    private readonly ActionManager actionManager;

    public WhiteMageRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.WHM_Enabled) return null;

        var player = GameState.LocalPlayer;
        if (player == null || !player.StatusFlags.HasFlag(StatusFlags.InCombat)) return null;

        // 1. CRITICAL HEALING
        var criticalMember = GetLowestHPMember(0.30f);
        if (criticalMember != null)
        {
            if (actionManager.CanUseAction(Benediction))
                return new ActionInfo(Benediction, "Benediction (Emergency)") { TargetOverrideId = criticalMember.GameObjectId };
            if (actionManager.CanUseAction(Tetragrammaton))
                return new ActionInfo(Tetragrammaton, "Tetra (Emergency)") { TargetOverrideId = criticalMember.GameObjectId };
            if (JobGaugeReader.WHM_HasLily && actionManager.CanUseAction(AfflatusSolace))
                return new ActionInfo(AfflatusSolace, "Solace (Emergency)") { TargetOverrideId = criticalMember.GameObjectId };
        }

        // 2. oGCD Weaving
        if (actionManager.CanWeave())
        {
            var oGCD = GetOGCDAction(player, config);
            if (oGCD != null) return oGCD;
        }

        // 3. STANDARD HEALING
        var injuredMember = GetLowestHPMember(0.70f);
        if (injuredMember != null)
        {
            if (JobGaugeReader.WHM_HasLily && actionManager.CanUseAction(AfflatusSolace))
                return new ActionInfo(AfflatusSolace, "Solace") { TargetOverrideId = injuredMember.GameObjectId };
            if (GetHPPercent(injuredMember) < 0.60f && actionManager.CanUseAction(CureII))
                return new ActionInfo(CureII, "Cure II") { TargetOverrideId = injuredMember.GameObjectId };
        }

        // AoE Healing
        if (GetCountBelowHP(0.80f) >= 3)
        {
            if (JobGaugeReader.WHM_HasLily && actionManager.CanUseAction(AfflatusRapture))
                return new ActionInfo(AfflatusRapture, "Rapture");
            if (actionManager.CanUseAction(MedicaIII))
                return new ActionInfo(MedicaIII, "Medica III");
        }

        // 4. DPS
        var target = GameState.TargetAsBattleChara;
        if (target == null) return null;

        if (config.WHM_DPS_AoE_Enabled && config.UseAoE && AutoRotationPlugin.GameState.GetHostileCountAround(player, 8f) >= config.WHM_DPS_AoE_Threshold)
            return GetAoEDPSAction(player, config);

        return GetSingleTargetDPSAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, Configuration config)
    {
        if (player.CurrentMp < 7000 && actionManager.CanUseAction(LucidDreaming))
            return new ActionInfo(LucidDreaming, "Lucid Dreaming", true);
        if (config.WHM_Buff_PresenceOfMind && actionManager.CanUseAction(PresenceOfMind))
            return new ActionInfo(PresenceOfMind, "Presence of Mind", true);
        if (actionManager.CanUseAction(Assize))
            return new ActionInfo(Assize, "Assize", true);
        if (GameState.HasStatus(DivineGrace) && actionManager.CanUseAction(DivineCaress))
            return new ActionInfo(DivineCaress, "Divine Caress", true);
        return null;
    }

    private ActionInfo? GetSingleTargetDPSAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        if (GameState.HasStatus(SacredSight) && actionManager.CanUseAction(GlareIV))
            return new ActionInfo(GlareIV, "Glare IV");

        float dotTime = GameState.GetMyStatusDurationOnTarget(DiaDot);
        if (dotTime < 3.0f && actionManager.CanUseAction(Dia))
            return new ActionInfo(Dia, "Dia");

        if (JobGaugeReader.WHM_BloodLilyReady && actionManager.CanUseAction(AfflatusMisery))
            return new ActionInfo(AfflatusMisery, "Afflatus Misery");

        return new ActionInfo(GlareIII, "Glare III");
    }

    private ActionInfo? GetAoEDPSAction(IPlayerCharacter player, Configuration config)
    {
        if (JobGaugeReader.WHM_BloodLilyReady && actionManager.CanUseAction(AfflatusMisery))
            return new ActionInfo(AfflatusMisery, "Afflatus Misery (AoE)");
        if (GameState.HasStatus(SacredSight) && actionManager.CanUseAction(GlareIV))
            return new ActionInfo(GlareIV, "Glare IV (AoE)");
        return new ActionInfo(HolyIII, "Holy III");
    }

    // --- Helpers Refactored for WRATH ARCHITECTURE ---
    private IBattleChara? GetLowestHPMember(float threshold)
    {
        IBattleChara? lowest = null;
        float minHp = threshold;

        // FIX: Use Svc.Party
        foreach (var member in Svc.Party)
        {
            if (member.GameObject is IBattleChara bc && !bc.IsDead)
            {
                float hp = (float)bc.CurrentHp / bc.MaxHp;
                if (hp < minHp)
                {
                    minHp = hp;
                    lowest = bc;
                }
            }
        }
        // Check self
        var player = GameState.LocalPlayer;
        if (player != null && (float)player.CurrentHp / player.MaxHp < minHp) return player;
        return lowest;
    }

    private int GetCountBelowHP(float threshold)
    {
        int count = 0;
        // FIX: Use Svc.Party
        foreach (var member in Svc.Party)
        {
            if (member.GameObject is IBattleChara bc && !bc.IsDead && ((float)bc.CurrentHp / bc.MaxHp) < threshold)
                count++;
        }
        if (GameState.LocalPlayer != null && ((float)GameState.LocalPlayer.CurrentHp / GameState.LocalPlayer.MaxHp) < threshold)
            count++;
        return count;
    }

    private float GetHPPercent(IBattleChara chara) => (float)chara.CurrentHp / chara.MaxHp;
}