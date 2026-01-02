using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Reaper rotation based on Icy Veins Dawntrail 7.x guide.
/// Reaper builds Soul Gauge through combos, spends on Soul Reaver actions,
/// then enters Enshroud for burst damage windows.
/// </summary>
public class ReaperRotation : IRotation
{
    public uint JobId => 39;

    #region Action IDs
    // Basic Combo
    private const uint Slice = 24373;
    private const uint WaxingSlice = 24374;
    private const uint InfernalSlice = 24375;

    // AoE Combo
    private const uint SpinningScythe = 24376;
    private const uint NightmareScythe = 24377;

    // Soul Gauge Builders (oGCD)
    private const uint SoulSlice = 24380;
    private const uint SoulScythe = 24381;       // AoE

    // Soul Reaver Actions (50 Soul)
    private const uint BloodStalk = 24389;
    private const uint UnveiledGallows = 24390;
    private const uint UnveiledGibbet = 24391;
    private const uint GrimSwathe = 24392;       // AoE

    // Enhanced Soul Reaver (after Blood Stalk)
    private const uint Gallows = 24383;
    private const uint Gibbet = 24382;
    private const uint Guillotine = 24384;       // AoE
    private const uint ExecutionersGallows = 36940;
    private const uint ExecutionersGibbet = 36939;
    private const uint ExecutionersGuillotine = 36941;

    // Enshroud Actions (50 Shroud)
    private const uint Enshroud = 24394;
    private const uint VoidReaping = 24395;
    private const uint CrossReaping = 24396;
    private const uint GrimReaping = 24397;      // AoE
    private const uint Communio = 24398;
    private const uint LemuresSlice = 24399;
    private const uint LemuresScythe = 24400;    // AoE
    private const uint Sacrificium = 36942;      // New Dawntrail
    private const uint Perfectio = 36943;        // New Dawntrail finisher

    // Buffs / oGCDs
    private const uint ArcaneCircle = 24405;
    private const uint Gluttony = 24393;
    private const uint PlentifulHarvest = 24385;

    // Utility / Movement
    private const uint ShadowOfDeath = 24378;    // DoT + Death's Design debuff
    private const uint WhorlOfDeath = 24379;     // AoE DoT
    private const uint HellsIngress = 24401;
    private const uint HellsEgress = 24402;
    private const uint Regress = 24403;
    private const uint ArcaneCrest = 24404;
    private const uint Soulsow = 24387;
    private const uint HarvestMoon = 24388;
    private const uint TrueNorth = 7546;
    #endregion

    #region Status IDs
    private const uint DeathsDesign = 2586;      // Vuln debuff on target
    private const uint SoulReaver = 2587;        // Enables Gibbet/Gallows
    private const uint EnhancedGibbet = 2588;
    private const uint EnhancedGallows = 2589;
    private const uint EnhancedCrossReaping = 2591;
    private const uint EnhancedVoidReaping = 2590;
    private const uint Enshrouded = 2593;
    private const uint ArcaneCircleBuff = 2599;
    private const uint ImmortalSacrifice = 2592; // Plentiful Harvest stacks
    private const uint IdealHost = 3905;         // Perfectio ready
    private const uint Oblatio = 3857;           // Sacrificium ready
    private const uint Executioner = 3858;       // Enhanced Soul Reaver
    private const uint Soulsow_Buff = 2594;
    #endregion

    private readonly ActionManager actionManager;

    public ReaperRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.RPR_Enabled) return null;

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
        if (config.RPR_AoE_Enabled && GameState.GetHostileCountAround(player, 5f) >= config.RPR_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Perfectio (Enshroud finisher - new Dawntrail)
        if (GameState.HasStatus(IdealHost) && actionManager.CanUseAction(Perfectio))
            return new ActionInfo(Perfectio, "Perfectio");

        // Sacrificium (during Enshroud - new Dawntrail)
        if (GameState.HasStatus(Oblatio) && actionManager.CanUseAction(Sacrificium))
            return new ActionInfo(Sacrificium, "Sacrificium");

        // Arcane Circle - raid buff
        if (config.RPR_Buff_ArcaneCircle && actionManager.CanUseAction(ArcaneCircle))
            return new ActionInfo(ArcaneCircle, "Arcane Circle", true);

        // Enshroud - enter burst mode
        if (config.RPR_Buff_Enshroud && actionManager.CanUseAction(Enshroud))
            return new ActionInfo(Enshroud, "Enshroud", true);

        // Gluttony - grants 2 Soul Reaver stacks
        if (config.RPR_oGCD_Gluttony && actionManager.CanUseAction(Gluttony))
            return new ActionInfo(Gluttony, "Gluttony");

        // Lemure's Slice (during Enshroud)
        if (GameState.HasStatus(Enshrouded) && actionManager.CanUseAction(LemuresSlice))
            return new ActionInfo(LemuresSlice, "Lemure's Slice");

        // Blood Stalk - Soul spender for Soul Reaver
        if (actionManager.CanUseAction(BloodStalk))
            return new ActionInfo(BloodStalk, "Blood Stalk");

        // Unveiled actions (enhanced Blood Stalk followups)
        if (GameState.HasStatus(EnhancedGibbet) && actionManager.CanUseAction(UnveiledGibbet))
            return new ActionInfo(UnveiledGibbet, "Unveiled Gibbet");

        if (GameState.HasStatus(EnhancedGallows) && actionManager.CanUseAction(UnveiledGallows))
            return new ActionInfo(UnveiledGallows, "Unveiled Gallows");

        // Soul Slice - Soul generator
        if (actionManager.CanUseAction(SoulSlice))
            return new ActionInfo(SoulSlice, "Soul Slice");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // Plentiful Harvest (after Arcane Circle, when stacks are up)
        if (GameState.HasStatus(ImmortalSacrifice) && actionManager.CanUseAction(PlentifulHarvest))
            return new ActionInfo(PlentifulHarvest, "Plentiful Harvest");

        // Harvest Moon (pre-charged Soulsow)
        if (GameState.HasStatus(Soulsow_Buff) && actionManager.CanUseAction(HarvestMoon))
            return new ActionInfo(HarvestMoon, "Harvest Moon");

        // Enshroud rotation
        if (GameState.HasStatus(Enshrouded))
        {
            // Communio (finisher)
            if (actionManager.CanUseAction(Communio))
                return new ActionInfo(Communio, "Communio");

            // Void/Cross Reaping based on enhanced buff
            if (GameState.HasStatus(EnhancedVoidReaping) && actionManager.CanUseAction(VoidReaping))
                return new ActionInfo(VoidReaping, "Void Reaping");

            if (GameState.HasStatus(EnhancedCrossReaping) && actionManager.CanUseAction(CrossReaping))
                return new ActionInfo(CrossReaping, "Cross Reaping");

            // Default to Cross Reaping
            if (actionManager.CanUseAction(CrossReaping))
                return new ActionInfo(CrossReaping, "Cross Reaping");
            if (actionManager.CanUseAction(VoidReaping))
                return new ActionInfo(VoidReaping, "Void Reaping");
        }

        // Soul Reaver actions (from Blood Stalk or Gluttony)
        if (GameState.HasStatus(SoulReaver) || GameState.HasStatus(Executioner))
        {
            // Use enhanced version based on positional buff
            if (GameState.HasStatus(EnhancedGibbet))
            {
                if (actionManager.CanUseAction(ExecutionersGibbet))
                    return new ActionInfo(ExecutionersGibbet, "Executioner's Gibbet");
                return new ActionInfo(Gibbet, "Gibbet");
            }
            if (GameState.HasStatus(EnhancedGallows))
            {
                if (actionManager.CanUseAction(ExecutionersGallows))
                    return new ActionInfo(ExecutionersGallows, "Executioner's Gallows");
                return new ActionInfo(Gallows, "Gallows");
            }

            // Default to Gibbet
            if (actionManager.CanUseAction(Gibbet))
                return new ActionInfo(Gibbet, "Gibbet");
        }

        // Shadow of Death - maintain DoT/debuff
        float deathsDesignRemaining = GameState.GetMyStatusDurationOnTarget(DeathsDesign);
        if (deathsDesignRemaining < 5f && actionManager.CanUseAction(ShadowOfDeath))
            return new ActionInfo(ShadowOfDeath, "Shadow of Death");

        // Basic combo
        if (adjustedLast == WaxingSlice)
            return new ActionInfo(InfernalSlice, "Infernal Slice");

        if (adjustedLast == Slice)
            return new ActionInfo(WaxingSlice, "Waxing Slice");

        return new ActionInfo(Slice, "Slice");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // Plentiful Harvest
        if (GameState.HasStatus(ImmortalSacrifice) && actionManager.CanUseAction(PlentifulHarvest))
            return new ActionInfo(PlentifulHarvest, "Plentiful Harvest");

        // Enshroud AoE
        if (GameState.HasStatus(Enshrouded))
        {
            if (actionManager.CanUseAction(Communio))
                return new ActionInfo(Communio, "Communio");

            if (actionManager.CanUseAction(LemuresScythe))
                return new ActionInfo(LemuresScythe, "Lemure's Scythe");

            if (actionManager.CanUseAction(GrimReaping))
                return new ActionInfo(GrimReaping, "Grim Reaping");
        }

        // Soul Reaver AoE
        if (GameState.HasStatus(SoulReaver) || GameState.HasStatus(Executioner))
        {
            if (actionManager.CanUseAction(ExecutionersGuillotine))
                return new ActionInfo(ExecutionersGuillotine, "Executioner's Guillotine");
            if (actionManager.CanUseAction(Guillotine))
                return new ActionInfo(Guillotine, "Guillotine");
        }

        // AoE Soul spender
        if (actionManager.CanUseAction(GrimSwathe))
            return new ActionInfo(GrimSwathe, "Grim Swathe");

        // AoE Soul builder
        if (actionManager.CanUseAction(SoulScythe))
            return new ActionInfo(SoulScythe, "Soul Scythe");

        // Whorl of Death - AoE DoT
        float deathsDesignRemaining = GameState.GetMyStatusDurationOnTarget(DeathsDesign);
        if (deathsDesignRemaining < 5f && actionManager.CanUseAction(WhorlOfDeath))
            return new ActionInfo(WhorlOfDeath, "Whorl of Death");

        // AoE combo
        if (adjustedLast == SpinningScythe)
            return new ActionInfo(NightmareScythe, "Nightmare Scythe");

        return new ActionInfo(SpinningScythe, "Spinning Scythe");
    }
}
