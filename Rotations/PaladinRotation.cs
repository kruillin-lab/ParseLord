using System.Linq;
using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using AutoRotationPlugin.Rotations;
using Dalamud.Game.ClientState.Objects.Enums;
using AutoRotationPlugin.Managers;

namespace AutoRotationPlugin.Rotations.Jobs
{
    public class PaladinRotation : IRotation
    {
        // FIX: Paladin is Job 19 (GLA=1). Job 21 is Warrior.
        public uint JobId => 19;

        #region Action IDs
        // GCDs - Physical
        private const uint FastBlade = 9;
        private const uint RiotBlade = 15;
        private const uint RoyalAuthority = 3539;
        private const uint GoringBlade = 3538;

        // GCDs - Atonement Combo
        private const uint Atonement = 16460;
        private const uint Supplication = 36918;
        private const uint Sepulchre = 36919;

        // GCDs - Magic
        private const uint HolySpirit = 7384;
        private const uint Confiteor = 16459;
        private const uint BladeOfFaith = 25748;
        private const uint BladeOfTruth = 25749;
        private const uint BladeOfValor = 25750;

        // GCDs - AoE
        private const uint TotalEclipse = 7381;
        private const uint Prominence = 16457;
        private const uint HolyCircle = 16458;

        // oGCDs
        private const uint FightOrFlight = 20;
        private const uint Requiescat = 7383;
        private const uint Imperator = 36921;
        private const uint SpiritsWithin = 29;
        private const uint Expiacion = 25747;
        private const uint CircleOfScorn = 23;
        private const uint Intervene = 16461;
        private const uint BladeOfHonor = 36922;

        // Defensives
        private const uint Sheltron = 3542;
        private const uint HolySheltron = 25746;
        #endregion

        #region Status IDs
        private const uint FightOrFlightStatus = 76;
        private const uint RequiescatStatus = 1368;
        private const uint ConfiteorReady = 3019;
        private const uint AtonementReady = 1902;
        private const uint SupplicationReady = 3827;
        private const uint SepulchreReady = 3828;
        private const uint BladeOfHonorReady = 3831;
        private const uint DivineMight = 2673;
        #endregion

        private readonly ActionManager actionManager;

        public PaladinRotation()
        {
            actionManager = ActionManager.Instance;
        }

        public ActionInfo? GetNextAction(Configuration config)
        {
            if (!config.PLD_Enabled) return null;

            var player = GameState.LocalPlayer;
            if (player == null || !player.StatusFlags.HasFlag(StatusFlags.InCombat)) return null;

            var target = GameState.TargetAsBattleChara;
            if (target == null) return null;

            // 1. Defensives
            byte gauge = JobGaugeReader.PLD_OathGauge;
            float hpPercent = (float)player.CurrentHp / player.MaxHp;

            if (config.PLD_Def_Sheltron && (gauge >= 80 || (gauge >= 50 && hpPercent < 0.70f)))
            {
                if (actionManager.CanUseAction(HolySheltron)) return new ActionInfo(HolySheltron, "Holy Sheltron", true);
                if (actionManager.CanUseAction(Sheltron)) return new ActionInfo(Sheltron, "Sheltron", true);
            }

            // 2. oGCD Weaving
            if (actionManager.CanWeave())
            {
                var oGCD = GetOGCDAction(player, target, config);
                if (oGCD != null) return oGCD;
            }

            // 3. GCD Logic
            if (config.PLD_AoE_Enabled && config.UseAoE && RotationManager.GetHostileCountAround(player, 5f) >= config.PLD_AoE_Threshold)
                return GetAoEAction(player, config);

            return GetSingleTargetAction(player, target, config);
        }

        private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
        {
            if (config.PLD_Buff_FightOrFlight && actionManager.CanUseAction(FightOrFlight))
                return new ActionInfo(FightOrFlight, "Fight or Flight", true);

            if (config.PLD_Magic_Requiescat && GameState.HasStatus(FightOrFlightStatus))
            {
                if (actionManager.CanUseAction(Imperator)) return new ActionInfo(Imperator, "Imperator");
                if (actionManager.CanUseAction(Requiescat)) return new ActionInfo(Requiescat, "Requiescat");
            }

            if (GameState.HasStatus(BladeOfHonorReady) && actionManager.CanUseAction(BladeOfHonor))
                return new ActionInfo(BladeOfHonor, "Blade of Honor");

            if (actionManager.CanUseAction(Expiacion)) return new ActionInfo(Expiacion, "Expiacion");
            if (actionManager.CanUseAction(SpiritsWithin)) return new ActionInfo(SpiritsWithin, "Spirits Within");
            if (actionManager.CanUseAction(CircleOfScorn)) return new ActionInfo(CircleOfScorn, "Circle of Scorn");

            if (GameState.HasStatus(FightOrFlightStatus) && actionManager.CanUseAction(Intervene))
                return new ActionInfo(Intervene, "Intervene");

            return null;
        }

        private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
        {
            uint lastAction = actionManager.ComboAction;
            uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

            // Magic Phase
            if (GameState.HasStatus(ConfiteorReady) || GameState.HasStatus(RequiescatStatus))
                return new ActionInfo(Confiteor, "Confiteor Combo");

            if (GameState.HasStatus(DivineMight))
                return new ActionInfo(HolySpirit, "Holy Spirit");

            // Goring Blade
            if (GameState.HasStatus(FightOrFlightStatus) && actionManager.CanUseAction(GoringBlade))
                return new ActionInfo(GoringBlade, "Goring Blade");

            // Atonement Combo
            if (GameState.HasStatus(SepulchreReady)) return new ActionInfo(Sepulchre, "Sepulchre");
            if (GameState.HasStatus(SupplicationReady)) return new ActionInfo(Supplication, "Supplication");
            if (GameState.HasStatus(AtonementReady)) return new ActionInfo(Atonement, "Atonement");

            // Standard Combo
            if (adjustedLast == RiotBlade) return new ActionInfo(RoyalAuthority, "Royal Authority");
            if (adjustedLast == FastBlade) return new ActionInfo(RiotBlade, "Riot Blade");

            // Default Fallback
            return new ActionInfo(FastBlade, "Fast Blade");
        }

        private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
        {
            if (GameState.HasStatus(ConfiteorReady) || GameState.HasStatus(RequiescatStatus))
                return new ActionInfo(Confiteor, "Confiteor (AoE)");

            if (GameState.HasStatus(DivineMight))
                return new ActionInfo(HolyCircle, "Holy Circle");

            uint lastAction = actionManager.ComboAction;
            uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

            if (adjustedLast == TotalEclipse)
                return new ActionInfo(Prominence, "Prominence");

            return new ActionInfo(TotalEclipse, "Total Eclipse");
        }
    }
}