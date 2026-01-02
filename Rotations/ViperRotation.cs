using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Viper rotation based on Icy Veins Dawntrail 7.x guide.
/// Viper uses dual swords with a 3-hit combo system, building Serpent's Offerings
/// to enter Reawaken for burst damage.
/// </summary>
public class ViperRotation : IRotation
{
    public uint JobId => 41;

    #region Action IDs
    // Basic Combos (alternate between left/right paths)
    private const uint SteelFangs = 34606;       // Combo starter 1
    private const uint ReavingFangs = 34607;     // Combo starter 2
    private const uint HuntersString = 34608;    // 2nd hit (from Steel)
    private const uint SwiftskinsString = 34609; // 2nd hit (from Reaving)
    private const uint FlankstingStrike = 34610; // 3rd hit finisher
    private const uint FlanksbaneFang = 34611;   // 3rd hit finisher
    private const uint HindstingStrike = 34612;  // 3rd hit finisher
    private const uint HindsbaneFang = 34613;    // 3rd hit finisher

    // Vicewinder Combo (oGCD combo that grants buffs)
    private const uint Vicewinder = 34620;
    private const uint HuntersCoil = 34621;
    private const uint SwiftskinsCoil = 34622;

    // Vicepit Combo (AoE version)
    private const uint Vicepit = 34623;
    private const uint HuntersDen = 34624;
    private const uint SwiftskinsDen = 34625;

    // AoE Combo
    private const uint SteelMaw = 34614;
    private const uint ReavingMaw = 34615;
    private const uint HuntersBite = 34616;
    private const uint SwiftskinsBite = 34617;
    private const uint JaggedMaw = 34618;
    private const uint BloodiedMaw = 34619;

    // Reawaken Actions (burst phase)
    private const uint Reawaken = 34626;
    private const uint FirstGeneration = 34627;
    private const uint SecondGeneration = 34628;
    private const uint ThirdGeneration = 34629;
    private const uint FourthGeneration = 34630;
    private const uint Ouroboros = 34631;

    // Uncoiled Fury (Rattling Coil spender)
    private const uint UncoiledFury = 34633;
    private const uint UncoiledTwinfang = 34644;
    private const uint UncoiledTwinblood = 34645;

    // Twinblade Actions (followups)
    private const uint Twinfang = 34636;
    private const uint Twinblood = 34637;
    private const uint TwinfangBite = 34638;
    private const uint TwinbloodBite = 34639;
    private const uint TwinfangThresh = 34640;
    private const uint TwinbloodThresh = 34641;

    // Serpent's Tail (oGCD followups)
    private const uint SerpentsTail = 35920;
    private const uint DeathRattle = 34634;
    private const uint LastLash = 34635;
    private const uint FirstLegacy = 34640;
    private const uint SecondLegacy = 34641;
    private const uint ThirdLegacy = 34642;
    private const uint FourthLegacy = 34643;

    // Utility
    private const uint Slither = 34646;
    private const uint TrueNorth = 7546;
    #endregion

    #region Status IDs
    private const uint NoxiousGnash = 3667;      // DoT/Debuff from finishers
    private const uint HuntersInstinct = 3668;   // Damage buff
    private const uint Swiftscaled = 3669;       // Haste buff
    private const uint FlankstungVenom = 3645;
    private const uint FlanksbaneVenom = 3646;
    private const uint HindstungVenom = 3647;
    private const uint HindsbaneVenom = 3648;
    private const uint GrimhuntersVenom = 3649;
    private const uint GrimskinsVenom = 3650;
    private const uint HuntersVenom = 3657;
    private const uint SwiftskinsVenom = 3658;
    private const uint FellhuntersVenom = 3659;
    private const uint FellskinsVenom = 3660;
    private const uint PoisedForTwinfang = 3665;
    private const uint PoisedForTwinblood = 3666;
    private const uint Reawakened = 3670;
    #endregion

    private readonly ActionManager actionManager;

    public ViperRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.VPR_Enabled) return null;

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
        if (config.VPR_AoE_Enabled && GameState.GetHostileCountAround(player, 5f) >= config.VPR_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Serpent's Tail followups (highest priority)
        if (actionManager.CanUseAction(DeathRattle))
            return new ActionInfo(DeathRattle, "Death Rattle");

        if (actionManager.CanUseAction(LastLash))
            return new ActionInfo(LastLash, "Last Lash");

        // Legacy actions (Reawaken followups)
        if (actionManager.CanUseAction(FirstLegacy))
            return new ActionInfo(FirstLegacy, "First Legacy");
        if (actionManager.CanUseAction(SecondLegacy))
            return new ActionInfo(SecondLegacy, "Second Legacy");
        if (actionManager.CanUseAction(ThirdLegacy))
            return new ActionInfo(ThirdLegacy, "Third Legacy");
        if (actionManager.CanUseAction(FourthLegacy))
            return new ActionInfo(FourthLegacy, "Fourth Legacy");

        // Twinfang/Twinblood (after Vice combos)
        if (GameState.HasStatus(PoisedForTwinfang) && actionManager.CanUseAction(Twinfang))
            return new ActionInfo(Twinfang, "Twinfang");
        if (GameState.HasStatus(PoisedForTwinblood) && actionManager.CanUseAction(Twinblood))
            return new ActionInfo(Twinblood, "Twinblood");

        // Uncoiled Twinfang/Twinblood (after Uncoiled Fury)
        if (actionManager.CanUseAction(UncoiledTwinfang))
            return new ActionInfo(UncoiledTwinfang, "Uncoiled Twinfang");
        if (actionManager.CanUseAction(UncoiledTwinblood))
            return new ActionInfo(UncoiledTwinblood, "Uncoiled Twinblood");

        // Vicewinder combo (builds gauge and buffs)
        if (actionManager.CanUseAction(HuntersCoil))
            return new ActionInfo(HuntersCoil, "Hunter's Coil");
        if (actionManager.CanUseAction(SwiftskinsCoil))
            return new ActionInfo(SwiftskinsCoil, "Swiftskin's Coil");

        if (config.VPR_oGCD_Vicewinder && actionManager.CanUseAction(Vicewinder))
            return new ActionInfo(Vicewinder, "Vicewinder");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Reawaken phase
        if (GameState.HasStatus(Reawakened))
        {
            // Generation attacks in sequence
            if (actionManager.CanUseAction(Ouroboros))
                return new ActionInfo(Ouroboros, "Ouroboros");
            if (actionManager.CanUseAction(FourthGeneration))
                return new ActionInfo(FourthGeneration, "Fourth Generation");
            if (actionManager.CanUseAction(ThirdGeneration))
                return new ActionInfo(ThirdGeneration, "Third Generation");
            if (actionManager.CanUseAction(SecondGeneration))
                return new ActionInfo(SecondGeneration, "Second Generation");
            if (actionManager.CanUseAction(FirstGeneration))
                return new ActionInfo(FirstGeneration, "First Generation");
        }

        // Enter Reawaken when gauge is full
        if (config.VPR_Buff_Reawaken && actionManager.CanUseAction(Reawaken))
            return new ActionInfo(Reawaken, "Reawaken", true);

        // Uncoiled Fury (Rattling Coil spender)
        if (actionManager.CanUseAction(UncoiledFury))
            return new ActionInfo(UncoiledFury, "Uncoiled Fury");

        // 3rd hit finishers (based on venom buffs)
        if (GameState.HasStatus(FlankstungVenom) && actionManager.CanUseAction(FlankstingStrike))
            return new ActionInfo(FlankstingStrike, "Flanksting Strike");
        if (GameState.HasStatus(FlanksbaneVenom) && actionManager.CanUseAction(FlanksbaneFang))
            return new ActionInfo(FlanksbaneFang, "Flanksbane Fang");
        if (GameState.HasStatus(HindstungVenom) && actionManager.CanUseAction(HindstingStrike))
            return new ActionInfo(HindstingStrike, "Hindsting Strike");
        if (GameState.HasStatus(HindsbaneVenom) && actionManager.CanUseAction(HindsbaneFang))
            return new ActionInfo(HindsbaneFang, "Hindsbane Fang");

        // 2nd hit (based on which starter was used)
        if (GameState.HasStatus(HuntersInstinct) || actionManager.CanUseAction(SwiftskinsString))
        {
            if (actionManager.CanUseAction(SwiftskinsString))
                return new ActionInfo(SwiftskinsString, "Swiftskin's String");
        }

        if (actionManager.CanUseAction(HuntersString))
            return new ActionInfo(HuntersString, "Hunter's String");

        // Start combo - alternate between Steel and Reaving
        // Check buffs to determine which to use
        float huntersInstinct = GameState.GetStatusDuration(HuntersInstinct);
        float swiftscaled = GameState.GetStatusDuration(Swiftscaled);

        // Prioritize refreshing the buff that's lower
        if (huntersInstinct < swiftscaled || huntersInstinct < 10f)
        {
            if (actionManager.CanUseAction(SteelFangs))
                return new ActionInfo(SteelFangs, "Steel Fangs");
        }

        if (actionManager.CanUseAction(ReavingFangs))
            return new ActionInfo(ReavingFangs, "Reaving Fangs");

        return new ActionInfo(SteelFangs, "Steel Fangs");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        // Reawaken phase (same in AoE)
        if (GameState.HasStatus(Reawakened))
        {
            if (actionManager.CanUseAction(Ouroboros))
                return new ActionInfo(Ouroboros, "Ouroboros");
            if (actionManager.CanUseAction(FourthGeneration))
                return new ActionInfo(FourthGeneration, "Fourth Generation");
            if (actionManager.CanUseAction(ThirdGeneration))
                return new ActionInfo(ThirdGeneration, "Third Generation");
            if (actionManager.CanUseAction(SecondGeneration))
                return new ActionInfo(SecondGeneration, "Second Generation");
            if (actionManager.CanUseAction(FirstGeneration))
                return new ActionInfo(FirstGeneration, "First Generation");
        }

        // Reawaken
        if (config.VPR_Buff_Reawaken && actionManager.CanUseAction(Reawaken))
            return new ActionInfo(Reawaken, "Reawaken", true);

        // AoE Uncoiled Fury
        if (actionManager.CanUseAction(UncoiledFury))
            return new ActionInfo(UncoiledFury, "Uncoiled Fury");

        // Vicepit combo (AoE version of Vicewinder)
        if (actionManager.CanUseAction(HuntersDen))
            return new ActionInfo(HuntersDen, "Hunter's Den");
        if (actionManager.CanUseAction(SwiftskinsDen))
            return new ActionInfo(SwiftskinsDen, "Swiftskin's Den");

        // AoE 3rd hit finishers
        if (GameState.HasStatus(GrimhuntersVenom) && actionManager.CanUseAction(JaggedMaw))
            return new ActionInfo(JaggedMaw, "Jagged Maw");
        if (GameState.HasStatus(GrimskinsVenom) && actionManager.CanUseAction(BloodiedMaw))
            return new ActionInfo(BloodiedMaw, "Bloodied Maw");

        // AoE 2nd hit
        if (actionManager.CanUseAction(SwiftskinsBite))
            return new ActionInfo(SwiftskinsBite, "Swiftskin's Bite");
        if (actionManager.CanUseAction(HuntersBite))
            return new ActionInfo(HuntersBite, "Hunter's Bite");

        // AoE combo starters
        float huntersInstinct = GameState.GetStatusDuration(HuntersInstinct);
        float swiftscaled = GameState.GetStatusDuration(Swiftscaled);

        if (huntersInstinct < swiftscaled || huntersInstinct < 10f)
        {
            if (actionManager.CanUseAction(SteelMaw))
                return new ActionInfo(SteelMaw, "Steel Maw");
        }

        if (actionManager.CanUseAction(ReavingMaw))
            return new ActionInfo(ReavingMaw, "Reaving Maw");

        return new ActionInfo(SteelMaw, "Steel Maw");
    }
}
