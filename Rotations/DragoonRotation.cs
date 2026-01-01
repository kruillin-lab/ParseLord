using System.Linq;
using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using AutoRotationPlugin.Rotations;
using Dalamud.Game.ClientState.Objects.Enums;
using AutoRotationPlugin.Managers;

namespace AutoRotationPlugin.Rotations.Jobs
{
    public class DragoonRotation : IRotation
    {
        public uint JobId => 22;

        #region Action IDs
        // Base GCDs
        private const uint TrueThrust = 75;
        private const uint VorpalThrust = 78;
        private const uint Disembowel = 87;
        private const uint FullThrust = 84;
        private const uint ChaosThrust = 88;
        private const uint WheelingThrust = 3556;
        private const uint FangAndClaw = 3554;
        private const uint RaidenThrust = 16479;   // True Thrust Upgrade

        // Upgraded GCDs (Dawntrail) - Vital for Combo Checks
        private const uint LanceBarrage = 36954;   // Vorpal Thrust Upgrade
        private const uint SpiralBlow = 36955;     // Disembowel Upgrade
        private const uint HeavensThrust = 25771;  // Full Thrust Upgrade
        private const uint ChaoticSpring = 25772;  // Chaos Thrust Upgrade
        private const uint Drakesbane = 36949;     // 5th Hit

        // AoE GCDs
        private const uint DoomSpike = 86;
        private const uint DraconianFury = 25770;  // Doom Spike Upgrade
        private const uint SonicThrust = 7397;
        private const uint CoerthanTorment = 16477;

        // oGCDs
        private const uint HighJump = 16478;
        private const uint MirageDive = 7399;
        private const uint DragonfireDive = 96;
        private const uint RiseOfTheDragon = 36952;
        private const uint StarDiver = 16480;
        private const uint Starcross = 36953;
        private const uint Geirskogul = 3555;
        private const uint Nastrond = 7400;
        private const uint WyrmwindThrust = 25773;

        // Buffs
        private const uint LanceCharge = 85;
        private const uint BattleLitany = 3557;
        private const uint LifeSurge = 83;

        // Utility
        private const uint TrueNorth = 7546;
        #endregion

        #region Status IDs
        private const uint PowerSurge = 2720;
        private const uint ChaosDot = 2719;
        private const uint DiveReady = 1243;
        private const uint DraconianFire = 2721;
        private const uint DragonsFlight = 3848;
        private const uint StarcrossReady = 3849;
        private const uint DrakesbaneReady = 3847;
        private const uint LifeOfTheDragon = 116;
        #endregion

        private readonly ActionManager actionManager;
        private RotationState rotationState = new();

        public DragoonRotation()
        {
            actionManager = ActionManager.Instance;
        }

        public ActionInfo? GetNextAction(Configuration config)
        {
            if (!config.DRG_Enabled) return null;

            var player = GameState.LocalPlayer;
            if (player == null || !player.StatusFlags.HasFlag(StatusFlags.InCombat)) return null;

            var target = GameState.TargetAsBattleChara;
            if (target == null) return null;

            UpdateRotationState(player, target);

            // 1. oGCD Weaving
            if (actionManager.CanWeave())
            {
                var oGCD = GetOGCDAction(player, target, config);
                if (oGCD != null) return oGCD;
            }

            // 2. GCD Logic
            if (config.DRG_AoE_Enabled && config.UseAoE && RotationManager.GetHostileCountAround(player, 10f) >= config.DRG_AoE_Threshold)
                return GetAoEAction(player, config);

            return GetSingleTargetAction(player, target, config);
        }

        private void UpdateRotationState(IPlayerCharacter player, IBattleChara target)
        {
            bool inCombat = player.StatusFlags.HasFlag(StatusFlags.InCombat);
            float playerHP = player.MaxHp > 0 ? (float)player.CurrentHp / player.MaxHp : 1f;
            rotationState.Update(0.016f, inCombat, true, playerHP, 1f);
        }

        private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
        {
            bool inBurst = rotationState.IsInBurstWindow;
            bool hasLotd = GameState.HasStatus(LifeOfTheDragon);

            // Buffs
            if (config.DRG_Buff_LanceCharge && actionManager.CanUseAction(LanceCharge))
                return new ActionInfo(LanceCharge, "Lance Charge", true);

            if (config.DRG_Buff_BattleLitany && inBurst && actionManager.CanUseAction(BattleLitany))
                return new ActionInfo(BattleLitany, "Battle Litany", true);

            // Life Surge (On Finishers)
            if (config.DRG_Buff_LifeSurge && !actionManager.HasStatus(player, LifeSurge))
            {
                var nextGCD = GetSingleTargetAction(player, target, config)?.ActionId;
                uint adjusted = actionManager.GetAdjustedActionId(nextGCD ?? 0);

                // Check Heavens' Thrust OR Drakesbane
                if (adjusted == HeavensThrust || adjusted == FullThrust || adjusted == Drakesbane || adjusted == CoerthanTorment)
                {
                    int charges = actionManager.GetActionCharges(LifeSurge);
                    if (charges == 2 || (charges == 1 && inBurst))
                        return new ActionInfo(LifeSurge, "Life Surge", true);
                }
            }

            // High Priority Procs
            if (actionManager.HasStatus(player, DragonsFlight) && actionManager.CanUseAction(RiseOfTheDragon))
                return new ActionInfo(RiseOfTheDragon, "Rise of the Dragon");

            if (actionManager.HasStatus(player, StarcrossReady) && actionManager.CanUseAction(Starcross))
                return new ActionInfo(Starcross, "Starcross");

            if (actionManager.HasStatus(player, DiveReady) && actionManager.CanUseAction(MirageDive))
                return new ActionInfo(MirageDive, "Mirage Dive");

            // Cooldowns
            if (config.DRG_Jump_HighJump && actionManager.CanUseAction(HighJump))
                return new ActionInfo(HighJump, "High Jump");

            if (config.DRG_Gauge_Geirskogul && actionManager.CanUseAction(Geirskogul))
                return new ActionInfo(Geirskogul, "Geirskogul");

            if (config.DRG_Jump_DragonfireDive && actionManager.CanUseAction(DragonfireDive))
                return new ActionInfo(DragonfireDive, "Dragonfire Dive");

            // Spenders
            if (hasLotd && actionManager.CanUseAction(Nastrond))
                return new ActionInfo(Nastrond, "Nastrond");

            if (hasLotd && actionManager.CanUseAction(StarDiver))
                return new ActionInfo(StarDiver, "Stardiver");

            if (JobGaugeReader.DRG_HasMaxFocus && actionManager.CanUseAction(WyrmwindThrust))
                return new ActionInfo(WyrmwindThrust, "Wyrmwind Thrust");

            return null;
        }

        private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
        {
            // IMPORTANT: Get the raw Last Action ID to avoid confusion
            uint lastAction = actionManager.ComboAction;
            uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

            // --- 5th HIT ---
            if (actionManager.HasStatus(player, DrakesbaneReady))
                return new ActionInfo(Drakesbane, "Drakesbane");

            // --- 4th HIT ---
            // Check Base OR Upgraded IDs
            if (adjustedLast == ChaosThrust || adjustedLast == ChaoticSpring ||
                adjustedLast == FullThrust || adjustedLast == HeavensThrust)
            {
                return new ActionInfo(WheelingThrust, "Wheeling Thrust");
            }
            if (adjustedLast == WheelingThrust || adjustedLast == FangAndClaw)
            {
                return new ActionInfo(FangAndClaw, "Fang and Claw");
            }

            // --- 3rd HIT ---
            // If we just did Spiral Blow (Upgrade) OR Disembowel (Base) -> Go Chaos
            if (adjustedLast == Disembowel || adjustedLast == SpiralBlow)
                return new ActionInfo(ChaosThrust, "Chaotic Spring");

            // If we just did Lance Barrage (Upgrade) OR Vorpal Thrust (Base) -> Go Full
            if (adjustedLast == VorpalThrust || adjustedLast == LanceBarrage)
                return new ActionInfo(FullThrust, "Heavens' Thrust");

            // --- 2nd HIT ---
            // If we just did Raiden Thrust (Upgrade) OR True Thrust (Base)
            if (adjustedLast == TrueThrust || adjustedLast == RaidenThrust)
            {
                // Logic: Maintain Power Surge buff (Disembowel)
                float buffRem = GameState.GetStatusDuration(PowerSurge);
                float dotRem = GameState.GetMyStatusDurationOnTarget(ChaosDot);

                if (buffRem < 6.0f || dotRem < 6.0f)
                    return new ActionInfo(Disembowel, "Spiral Blow");

                return new ActionInfo(VorpalThrust, "Lance Barrage");
            }

            // --- 1st HIT ---
            // Fallback: If no combo is active, start over.
            return new ActionInfo(TrueThrust, "True Thrust");
        }

        private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
        {
            uint lastAction = actionManager.ComboAction;
            uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

            if (adjustedLast == SonicThrust)
                return new ActionInfo(CoerthanTorment, "Coerthan Torment");

            if (adjustedLast == DoomSpike || adjustedLast == DraconianFury)
                return new ActionInfo(SonicThrust, "Sonic Thrust");

            return new ActionInfo(DoomSpike, "Draconian Fury");
        }
    }
}