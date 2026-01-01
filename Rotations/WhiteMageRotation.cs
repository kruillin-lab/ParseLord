using System.Linq;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using AutoRotationPlugin.Rotations;
using Dalamud.Game.ClientState.Objects.Enums;
using AutoRotationPlugin.Managers;

namespace AutoRotationPlugin.Rotations.Jobs
{
    public class WhiteMageRotation : IRotation
    {
        public uint JobId => 24;

        #region Action IDs
        // GCDs - DPS
        private const uint GlareIII = 25859;
        private const uint GlareIV = 37009;        // Lv 92 (Proc)
        private const uint Dia = 16532;            // DoT
        private const uint HolyIII = 25860;
        private const uint AfflatusMisery = 16535; // Blood Lily

        // GCDs - Healing
        private const uint CureII = 135;
        private const uint MedicaIII = 37010;      // Lv 96 (Replaces Medica II)
        private const uint AfflatusSolace = 16531; // Single Lily
        private const uint AfflatusRapture = 16534;// AoE Lily
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
        private const uint DivineCaress = 37011;   // Lv 100 Shield
        #endregion

        #region Status IDs
        private const uint DiaDot = 1871;
        private const uint SacredSight = 3879;     // Proc for Glare IV
        private const uint DivineGrace = 3878;     // Proc for Divine Caress
        #endregion

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

            // 1. CRITICAL HEALING (Priority over everything)
            // If anyone is dying (< 30%), use Emergency oGCDs or Instant Lilies.
            var criticalMember = GetLowestHPMember(0.30f);
            if (criticalMember != null)
            {
                // Benediction (Tank/Self mostly, but anyone dying works)
                if (actionManager.CanUseAction(Benediction))
                    return new ActionInfo(Benediction, "Benediction (Emergency)") { TargetOverrideId = criticalMember.GameObjectId };

                // Tetragrammaton
                if (actionManager.CanUseAction(Tetragrammaton))
                    return new ActionInfo(Tetragrammaton, "Tetra (Emergency)") { TargetOverrideId = criticalMember.GameObjectId };

                // Afflatus Solace (Instant GCD)
                if (JobGaugeReader.WHM_HasLily && actionManager.CanUseAction(AfflatusSolace))
                    return new ActionInfo(AfflatusSolace, "Solace (Emergency)") { TargetOverrideId = criticalMember.GameObjectId };
            }

            // 2. oGCD Weaving (DPS/Utility)
            if (actionManager.CanWeave())
            {
                var oGCD = GetOGCDAction(player, config);
                if (oGCD != null) return oGCD;
            }

            // 3. STANDARD HEALING
            // Single Target < 70%
            var injuredMember = GetLowestHPMember(0.70f);
            if (injuredMember != null)
            {
                // Use Lilies first for movement/Blood Lily generation
                if (JobGaugeReader.WHM_HasLily && actionManager.CanUseAction(AfflatusSolace))
                    return new ActionInfo(AfflatusSolace, "Solace") { TargetOverrideId = injuredMember.GameObjectId };

                // Fallback to Cure II if no Lilies and dangerous
                if (GetHPPercent(injuredMember) < 0.60f && actionManager.CanUseAction(CureII))
                    return new ActionInfo(CureII, "Cure II") { TargetOverrideId = injuredMember.GameObjectId };
            }

            // AoE Healing (if 3+ people < 80%)
            if (GetCountBelowHP(0.80f) >= 3)
            {
                if (JobGaugeReader.WHM_HasLily && actionManager.CanUseAction(AfflatusRapture))
                    return new ActionInfo(AfflatusRapture, "Rapture");

                if (actionManager.CanUseAction(MedicaIII))
                    return new ActionInfo(MedicaIII, "Medica III");
            }

            // 4. DPS ROTATION
            var target = GameState.TargetAsBattleChara;
            if (target == null) return null; // No target to attack

            // AoE DPS
            if (config.WHM_DPS_AoE_Enabled && config.UseAoE && RotationManager.GetHostileCountAround(player, 8f) >= config.WHM_DPS_AoE_Threshold)
                return GetAoEDPSAction(player, config);

            return GetSingleTargetDPSAction(player, target, config);
        }

        private ActionInfo? GetOGCDAction(IPlayerCharacter player, Configuration config)
        {
            // Lucid Dreaming (Mana Management)
            if (player.CurrentMp < 7000 && actionManager.CanUseAction(LucidDreaming))
                return new ActionInfo(LucidDreaming, "Lucid Dreaming", true);

            // Presence of Mind (Burst)
            if (config.WHM_Buff_PresenceOfMind && actionManager.CanUseAction(PresenceOfMind))
                return new ActionInfo(PresenceOfMind, "Presence of Mind", true);

            // Assize (Damage + Heal + MP) - Use on CD
            if (actionManager.CanUseAction(Assize))
                return new ActionInfo(Assize, "Assize", true);

            // Divine Caress (Follow up to Temperance/Shields)
            if (GameState.HasStatus(DivineGrace) && actionManager.CanUseAction(DivineCaress))
                return new ActionInfo(DivineCaress, "Divine Caress", true);

            return null;
        }

        private ActionInfo? GetSingleTargetDPSAction(IPlayerCharacter player, IBattleChara target, Configuration config)
        {
            // Glare IV (High Damage Proc)
            if (GameState.HasStatus(SacredSight) && actionManager.CanUseAction(GlareIV))
                return new ActionInfo(GlareIV, "Glare IV");

            // Maintain Dia (DoT)
            // Refresh if < 3 seconds remaining
            float dotTime = GameState.GetMyStatusDurationOnTarget(DiaDot);
            if (dotTime < 3.0f && actionManager.CanUseAction(Dia))
                return new ActionInfo(Dia, "Dia");

            // Afflatus Misery (Damage Neutral/Gain)
            // Use if Blood Lily ready AND (Burst Window OR movement needed OR nearly capped)
            // For simple logic: Use if ready.
            if (JobGaugeReader.WHM_BloodLilyReady && actionManager.CanUseAction(AfflatusMisery))
                return new ActionInfo(AfflatusMisery, "Afflatus Misery");

            // Filler
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

        // --- Helpers ---
        private IBattleChara? GetLowestHPMember(float threshold)
        {
            IBattleChara? lowest = null;
            float minHp = threshold;

            foreach (var member in Plugin.PartyList)
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
            // Also check self (if not in party list for some reason, though PartyList usually includes self)
            var player = GameState.LocalPlayer;
            if (player != null && (float)player.CurrentHp / player.MaxHp < minHp) return player;

            return lowest;
        }

        private int GetCountBelowHP(float threshold)
        {
            int count = 0;
            foreach (var member in Plugin.PartyList)
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
}