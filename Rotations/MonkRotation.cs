using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Monk rotation based on Icy Veins Dawntrail 7.x guide.
/// Monk revolves around generating and spending Beast Chakra, maintaining buffs,
/// and dumping damage into Riddle of Fire windows.
/// </summary>
public class MonkRotation : IRotation
{
    public uint JobId => 20;

    #region Action IDs
    // Basic Combos (Opo-opo, Raptor, Coeurl forms)
    private const uint Bootshine = 53;
    private const uint LeapingOpo = 36945;        // Bootshine upgrade
    private const uint TrueStrike = 54;
    private const uint RisingRaptor = 36946;      // True Strike upgrade
    private const uint SnapPunch = 56;
    private const uint PouncingCoeurl = 36947;    // Snap Punch upgrade
    private const uint DragonKick = 74;
    private const uint TwinSnakes = 61;
    private const uint Demolish = 66;

    // AoE
    private const uint ArmOfTheDestroyer = 62;
    private const uint ShadowOfTheDestroyer = 25767;
    private const uint FourPointFury = 16473;
    private const uint Rockbreaker = 70;

    // Blitz Actions (Beast Chakra spenders)
    private const uint ElixirField = 3545;
    private const uint FlintStrike = 25882;       // Elixir Field upgrade (Celestial Revolution)
    private const uint RisingPhoenix = 25768;
    private const uint PhantomRush = 25769;
    private const uint ElixirBurst = 36948;       // AoE Blitz
    private const uint WindsReply = 36949;        // New Dawntrail finisher

    // Chakra Spenders
    private const uint TheForbiddenChakra = 3547;
    private const uint SteelPeak = 25761;         // Forbidden Chakra upgrade
    private const uint HowlingFist = 25763;       // AoE chakra
    private const uint EnlightenedMeditation = 36943; // Enlightenment upgrade

    // oGCDs
    private const uint RiddleOfFire = 7395;
    private const uint RiddleOfWind = 25766;
    private const uint Brotherhood = 7396;
    private const uint PerfectBalance = 69;
    private const uint Thunderclap = 25762;
    private const uint FormShift = 4262;
    private const uint Meditation = 3546;
    private const uint FiresReply = 36950;        // New Dawntrail
    private const uint WindsReply2 = 36951;       // Followup

    // Utility
    private const uint TrueNorth = 7546;
    private const uint RiddleOfEarth = 7394;
    private const uint Mantra = 65;
    #endregion

    #region Status IDs
    private const uint DisciplinedFist = 3001;    // Damage buff from Twin Snakes
    private const uint LeadenFist = 1861;         // Bootshine guaranteed crit
    private const uint PerfectBalanceBuff = 110;
    private const uint FormlessFist = 2513;
    private const uint RiddleOfFireBuff = 1181;
    private const uint Brotherhood_Buff = 1182;
    private const uint OpoOpoForm = 107;
    private const uint RaptorForm = 108;
    private const uint CoeurlForm = 109;
    private const uint FiresRumination = 3843;
    private const uint WindsRumination = 3842;
    #endregion

    private readonly ActionManager actionManager;

    public MonkRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.MNK_Enabled) return null;

        var player = GameState.LocalPlayer;
        if (player == null || !player.StatusFlags.HasFlag(StatusFlags.InCombat)) return null;

        var target = GameState.TargetAsBattleChara;
        if (target == null) return null;

        // oGCD weaving
        if (actionManager.CanWeave())
        {
            var oGCD = GetOGCDAction(player, target, config);
            if (oGCD != null) return oGCD;
        }

        // AoE check
        if (config.MNK_AoE_Enabled && GameState.GetHostileCountAround(player, 5f) >= config.MNK_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Riddle of Fire - main burst window
        if (config.MNK_Buff_RiddleOfFire && actionManager.CanUseAction(RiddleOfFire))
            return new ActionInfo(RiddleOfFire, "Riddle of Fire", true);

        // Brotherhood - raid buff
        if (config.MNK_Buff_Brotherhood && actionManager.CanUseAction(Brotherhood))
            return new ActionInfo(Brotherhood, "Brotherhood", true);

        // Perfect Balance for Blitz setup
        if (actionManager.CanUseAction(PerfectBalance))
            return new ActionInfo(PerfectBalance, "Perfect Balance", true);

        // Riddle of Wind
        if (actionManager.CanUseAction(RiddleOfWind))
            return new ActionInfo(RiddleOfWind, "Riddle of Wind", true);

        // Fire's Reply (from Riddle of Fire)
        if (GameState.HasStatus(FiresRumination) && actionManager.CanUseAction(FiresReply))
            return new ActionInfo(FiresReply, "Fire's Reply");

        // Wind's Reply (from Riddle of Wind)
        if (GameState.HasStatus(WindsRumination) && actionManager.CanUseAction(WindsReply))
            return new ActionInfo(WindsReply, "Wind's Reply");

        // Steel Peak (Chakra spender) - use at 5 chakra
        if (actionManager.CanUseAction(SteelPeak))
            return new ActionInfo(SteelPeak, "Steel Peak");

        if (actionManager.CanUseAction(TheForbiddenChakra))
            return new ActionInfo(TheForbiddenChakra, "The Forbidden Chakra");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Check for Blitz ready (after Perfect Balance combo)
        // Phantom Rush / Rising Phoenix / Elixir Field based on Beast Chakra
        if (actionManager.CanUseAction(PhantomRush))
            return new ActionInfo(PhantomRush, "Phantom Rush");

        if (actionManager.CanUseAction(RisingPhoenix))
            return new ActionInfo(RisingPhoenix, "Rising Phoenix");

        if (actionManager.CanUseAction(ElixirField))
            return new ActionInfo(ElixirField, "Elixir Field");

        if (actionManager.CanUseAction(FlintStrike))
            return new ActionInfo(FlintStrike, "Flint Strike");

        // Perfect Balance active - use form finishers
        if (GameState.HasStatus(PerfectBalanceBuff))
        {
            // During PB, alternate between Opo and Raptor/Coeurl finishers for Beast Chakra
            if (actionManager.CanUseAction(LeapingOpo))
                return new ActionInfo(LeapingOpo, "Leaping Opo");
            if (actionManager.CanUseAction(Bootshine))
                return new ActionInfo(Bootshine, "Bootshine");
        }

        // Form-based rotation
        // Check current form and execute appropriate action

        // Coeurl Form - finishers
        if (GameState.HasStatus(CoeurlForm))
        {
            // Demolish for DoT/buff refresh, otherwise Snap Punch
            float dotRemaining = GameState.GetMyStatusDurationOnTarget(246); // Demolish DoT
            if (dotRemaining < 5f && actionManager.CanUseAction(Demolish))
                return new ActionInfo(Demolish, "Demolish");

            if (actionManager.CanUseAction(PouncingCoeurl))
                return new ActionInfo(PouncingCoeurl, "Pouncing Coeurl");
            if (actionManager.CanUseAction(SnapPunch))
                return new ActionInfo(SnapPunch, "Snap Punch");
        }

        // Raptor Form
        if (GameState.HasStatus(RaptorForm))
        {
            // Twin Snakes for buff, otherwise True Strike
            float buffRemaining = GameState.GetStatusDuration(DisciplinedFist);
            if (buffRemaining < 5f && actionManager.CanUseAction(TwinSnakes))
                return new ActionInfo(TwinSnakes, "Twin Snakes");

            if (actionManager.CanUseAction(RisingRaptor))
                return new ActionInfo(RisingRaptor, "Rising Raptor");
            if (actionManager.CanUseAction(TrueStrike))
                return new ActionInfo(TrueStrike, "True Strike");
        }

        // Opo-opo Form or no form - start combo
        // Dragon Kick if no Leaden Fist, otherwise Bootshine
        if (!GameState.HasStatus(LeadenFist) && actionManager.CanUseAction(DragonKick))
            return new ActionInfo(DragonKick, "Dragon Kick");

        if (actionManager.CanUseAction(LeapingOpo))
            return new ActionInfo(LeapingOpo, "Leaping Opo");

        return new ActionInfo(Bootshine, "Bootshine");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        // AoE Blitz
        if (actionManager.CanUseAction(ElixirBurst))
            return new ActionInfo(ElixirBurst, "Elixir Burst");

        // AoE chakra spender
        if (actionManager.CanUseAction(EnlightenedMeditation))
            return new ActionInfo(EnlightenedMeditation, "Enlightened Meditation");
        if (actionManager.CanUseAction(HowlingFist))
            return new ActionInfo(HowlingFist, "Howling Fist");

        // AoE combo based on form
        if (GameState.HasStatus(CoeurlForm))
        {
            if (actionManager.CanUseAction(Rockbreaker))
                return new ActionInfo(Rockbreaker, "Rockbreaker");
        }

        if (GameState.HasStatus(RaptorForm))
        {
            if (actionManager.CanUseAction(FourPointFury))
                return new ActionInfo(FourPointFury, "Four-point Fury");
        }

        // Start AoE combo
        if (actionManager.CanUseAction(ShadowOfTheDestroyer))
            return new ActionInfo(ShadowOfTheDestroyer, "Shadow of the Destroyer");

        return new ActionInfo(ArmOfTheDestroyer, "Arm of the Destroyer");
    }
}
