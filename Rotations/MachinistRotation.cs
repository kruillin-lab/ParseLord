using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Machinist rotation based on Icy Veins Dawntrail 7.x guide.
/// Machinist uses powerful tool weaponskills (Drill, Air Anchor, Chain Saw),
/// builds Heat for Hypercharge windows, and summons Automaton Queen.
/// </summary>
public class MachinistRotation : IRotation
{
    public uint JobId => 31;

    #region Action IDs
    // Basic Combo
    private const uint SplitShot = 2866;
    private const uint HeatedSplitShot = 7411;
    private const uint SlugShot = 2868;
    private const uint HeatedSlugShot = 7412;
    private const uint CleanShot = 2873;
    private const uint HeatedCleanShot = 7413;

    // AoE Combo
    private const uint SpreadShot = 2870;
    private const uint Scattergun = 25786;       // Spread Shot upgrade
    private const uint AutoCrossbow = 16497;     // AoE during Hypercharge

    // Tool Weaponskills (high priority)
    private const uint Drill = 16498;
    private const uint Bioblaster = 16499;       // AoE DoT (shares CD with Drill)
    private const uint AirAnchor = 16500;
    private const uint ChainSaw = 25788;
    private const uint Excavator = 36981;        // New Dawntrail (follows Chain Saw)
    private const uint FullMetalField = 36982;   // New Dawntrail

    // Hypercharge
    private const uint Hypercharge = 17209;
    private const uint HeatBlast = 7410;
    private const uint BlazingShot = 36978;      // Heat Blast upgrade

    // oGCDs
    private const uint GaussRound = 2874;
    private const uint DoubleCheck = 36979;      // Gauss Round upgrade
    private const uint Ricochet = 2890;
    private const uint Checkmate = 36980;        // Ricochet upgrade
    private const uint Reassemble = 2876;
    private const uint BarrelStabilizer = 7414;
    private const uint Wildfire = 2878;

    // Automaton Queen
    private const uint AutomatonQueen = 16501;
    private const uint QueenOverdrive = 16502;

    // Utility
    private const uint Tactician = 16889;
    private const uint Dismantle = 2887;
    #endregion

    #region Status IDs
    private const uint Reassembled = 851;
    private const uint WildfireBuff = 1946;
    private const uint Overheated = 2688;
    private const uint ExcavatorReady = 3865;
    private const uint FullMetalMachinist = 3866;
    private const uint Hypercharged = 3864;      // From Barrel Stabilizer
    #endregion

    private readonly ActionManager actionManager;

    public MachinistRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.MCH_Enabled) return null;

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
        if (config.MCH_AoE_Enabled && GameState.GetHostileCountAround(player, 10f) >= config.MCH_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Wildfire - burst window
        if (config.MCH_Buff_Wildfire && actionManager.CanUseAction(Wildfire))
            return new ActionInfo(Wildfire, "Wildfire");

        // Barrel Stabilizer - Heat generator + Full Metal buff
        if (config.MCH_Buff_BarrelStabilizer && actionManager.CanUseAction(BarrelStabilizer))
            return new ActionInfo(BarrelStabilizer, "Barrel Stabilizer", true);

        // Reassemble - use before Drill/Air Anchor/Chain Saw
        if (config.MCH_Buff_Reassemble && actionManager.CanUseAction(Reassemble))
        {
            // Only use if a tool skill is coming up
            if (actionManager.CanUseAction(Drill) ||
                actionManager.CanUseAction(AirAnchor) ||
                actionManager.CanUseAction(ChainSaw))
                return new ActionInfo(Reassemble, "Reassemble", true);
        }

        // Hypercharge - enter burst mode (when Heat is 50+)
        if (actionManager.CanUseAction(Hypercharge))
            return new ActionInfo(Hypercharge, "Hypercharge", true);

        // Automaton Queen - summon robot (at 50+ Battery)
        if (config.MCH_Summon_Queen && actionManager.CanUseAction(AutomatonQueen))
            return new ActionInfo(AutomatonQueen, "Automaton Queen", true);

        // Double Check / Gauss Round
        if (actionManager.CanUseAction(DoubleCheck))
            return new ActionInfo(DoubleCheck, "Double Check");
        if (actionManager.CanUseAction(GaussRound))
            return new ActionInfo(GaussRound, "Gauss Round");

        // Checkmate / Ricochet
        if (actionManager.CanUseAction(Checkmate))
            return new ActionInfo(Checkmate, "Checkmate");
        if (actionManager.CanUseAction(Ricochet))
            return new ActionInfo(Ricochet, "Ricochet");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // Full Metal Field (from Barrel Stabilizer buff)
        if (GameState.HasStatus(FullMetalMachinist) && actionManager.CanUseAction(FullMetalField))
            return new ActionInfo(FullMetalField, "Full Metal Field");

        // Excavator (Chain Saw followup)
        if (GameState.HasStatus(ExcavatorReady) && actionManager.CanUseAction(Excavator))
            return new ActionInfo(Excavator, "Excavator");

        // During Hypercharge - spam Heat Blast
        if (GameState.HasStatus(Overheated))
        {
            if (actionManager.CanUseAction(BlazingShot))
                return new ActionInfo(BlazingShot, "Blazing Shot");
            if (actionManager.CanUseAction(HeatBlast))
                return new ActionInfo(HeatBlast, "Heat Blast");
        }

        // Tool weaponskills (highest priority when off CD)
        // Use Reassemble before these for guaranteed crit/DH
        if (actionManager.CanUseAction(Drill))
            return new ActionInfo(Drill, "Drill");

        if (actionManager.CanUseAction(AirAnchor))
            return new ActionInfo(AirAnchor, "Air Anchor");

        if (actionManager.CanUseAction(ChainSaw))
            return new ActionInfo(ChainSaw, "Chain Saw");

        // Basic combo
        if (adjustedLast == SlugShot || adjustedLast == HeatedSlugShot)
        {
            if (actionManager.CanUseAction(HeatedCleanShot))
                return new ActionInfo(HeatedCleanShot, "Heated Clean Shot");
            return new ActionInfo(CleanShot, "Clean Shot");
        }

        if (adjustedLast == SplitShot || adjustedLast == HeatedSplitShot)
        {
            if (actionManager.CanUseAction(HeatedSlugShot))
                return new ActionInfo(HeatedSlugShot, "Heated Slug Shot");
            return new ActionInfo(SlugShot, "Slug Shot");
        }

        // Start combo
        if (actionManager.CanUseAction(HeatedSplitShot))
            return new ActionInfo(HeatedSplitShot, "Heated Split Shot");

        return new ActionInfo(SplitShot, "Split Shot");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        // Full Metal Field (works in AoE)
        if (GameState.HasStatus(FullMetalMachinist) && actionManager.CanUseAction(FullMetalField))
            return new ActionInfo(FullMetalField, "Full Metal Field");

        // Excavator
        if (GameState.HasStatus(ExcavatorReady) && actionManager.CanUseAction(Excavator))
            return new ActionInfo(Excavator, "Excavator");

        // During Hypercharge - Auto Crossbow
        if (GameState.HasStatus(Overheated))
        {
            if (actionManager.CanUseAction(AutoCrossbow))
                return new ActionInfo(AutoCrossbow, "Auto Crossbow");
        }

        // Bioblaster (AoE DoT, shares CD with Drill)
        if (actionManager.CanUseAction(Bioblaster))
            return new ActionInfo(Bioblaster, "Bioblaster");

        // Chain Saw (line AoE)
        if (actionManager.CanUseAction(ChainSaw))
            return new ActionInfo(ChainSaw, "Chain Saw");

        // AoE filler
        if (actionManager.CanUseAction(Scattergun))
            return new ActionInfo(Scattergun, "Scattergun");

        return new ActionInfo(SpreadShot, "Spread Shot");
    }
}
