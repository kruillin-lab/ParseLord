using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Bard rotation based on Icy Veins Dawntrail 7.x guide.
/// Bard cycles through songs, manages DoTs, and has strong party buffs.
/// Priority-based with proc management.
/// </summary>
public class BardRotation : IRotation
{
    public uint JobId => 23;

    #region Action IDs
    // Basic GCDs
    private const uint HeavyShot = 97;
    private const uint BurstShot = 16495;        // Heavy Shot upgrade
    private const uint StraightShot = 98;
    private const uint RefulgentArrow = 7409;    // Straight Shot upgrade
    private const uint ApexArrow = 16496;        // Soul Voice spender
    private const uint BlastArrow = 25784;       // Apex Arrow followup
    private const uint ResonantArrow = 36976;    // New Dawntrail

    // DoTs
    private const uint VenomousBite = 100;
    private const uint CausticBite = 7406;       // Venomous Bite upgrade
    private const uint Windbite = 113;
    private const uint Stormbite = 7407;         // Windbite upgrade
    private const uint IronJaws = 3560;          // Refresh both DoTs

    // AoE
    private const uint QuickNock = 106;
    private const uint Ladonsbite = 25783;       // Quick Nock upgrade
    private const uint Shadowbite = 16494;       // AoE proc
    private const uint RainOfDeath = 117;        // AoE oGCD
    private const uint WideVolley = 36974;       // New Dawntrail AoE

    // Songs
    private const uint WanderersMinuet = 3559;
    private const uint MagesBallad = 114;
    private const uint ArmysPaeon = 116;

    // Song Procs
    private const uint PitchPerfect = 7404;      // Wanderer's proc
    private const uint EmpyrealArrow = 3558;     // All songs
    private const uint HeartbreakShot = 36975;   // New Dawntrail

    // Buffs
    private const uint RagingStrikes = 101;
    private const uint BattleVoice = 118;
    private const uint RadiantFinale = 25785;
    private const uint RadiantEncore = 36977;    // New Dawntrail
    private const uint Barrage = 107;

    // Utility
    private const uint Bloodletter = 110;
    private const uint RepellingShot = 112;
    private const uint WardensPaean = 3561;
    private const uint NaturesMinne = 7408;
    private const uint Troubadour = 7405;
    #endregion

    #region Status IDs
    private const uint CausticBite_DoT = 1200;
    private const uint Stormbite_DoT = 1201;
    private const uint StraightShotReady = 122;
    private const uint BlastArrowReady = 2692;
    private const uint ResonantArrowReady = 3862;
    private const uint RadiantEncoreReady = 3863;
    private const uint RagingStrikesBuff = 125;
    private const uint BattleVoiceBuff = 141;
    private const uint RadiantFinaleBuff = 2964;
    private const uint BarrageBuff = 128;
    private const uint WanderersMinuetBuff = 2216;
    private const uint MagesBalladBuff = 2217;
    private const uint ArmysPaeonBuff = 2218;
    private const uint ShadowbiteReady = 3002;
    #endregion

    private readonly ActionManager actionManager;

    public BardRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.BRD_Enabled) return null;

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
        if (config.BRD_AoE_Enabled && GameState.GetHostileCountAround(player, 10f) >= config.BRD_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Raging Strikes - personal buff
        if (config.BRD_Buff_RagingStrikes && actionManager.CanUseAction(RagingStrikes))
            return new ActionInfo(RagingStrikes, "Raging Strikes", true);

        // Battle Voice - party buff
        if (config.BRD_Buff_BattleVoice && actionManager.CanUseAction(BattleVoice))
            return new ActionInfo(BattleVoice, "Battle Voice", true);

        // Radiant Finale - party buff (needs Coda)
        if (config.BRD_Buff_RadiantFinale && actionManager.CanUseAction(RadiantFinale))
            return new ActionInfo(RadiantFinale, "Radiant Finale", true);

        // Barrage - guarantees procs
        if (config.BRD_Buff_Barrage && actionManager.CanUseAction(Barrage))
            return new ActionInfo(Barrage, "Barrage", true);

        // Pitch Perfect (during Wanderer's Minuet, at 3 stacks ideally)
        if (GameState.HasStatus(WanderersMinuetBuff) && actionManager.CanUseAction(PitchPerfect))
            return new ActionInfo(PitchPerfect, "Pitch Perfect");

        // Empyreal Arrow - on cooldown
        if (actionManager.CanUseAction(EmpyrealArrow))
            return new ActionInfo(EmpyrealArrow, "Empyreal Arrow");

        // Heartbreak Shot (new Dawntrail)
        if (actionManager.CanUseAction(HeartbreakShot))
            return new ActionInfo(HeartbreakShot, "Heartbreak Shot");

        // Bloodletter
        if (actionManager.CanUseAction(Bloodletter))
            return new ActionInfo(Bloodletter, "Bloodletter");

        // Song cycling: Wanderer's Minuet > Mage's Ballad > Army's Paeon
        // Check if no song is active
        bool hasSong = GameState.HasStatus(WanderersMinuetBuff) ||
                       GameState.HasStatus(MagesBalladBuff) ||
                       GameState.HasStatus(ArmysPaeonBuff);

        if (!hasSong)
        {
            if (actionManager.CanUseAction(WanderersMinuet))
                return new ActionInfo(WanderersMinuet, "The Wanderer's Minuet", true);
            if (actionManager.CanUseAction(MagesBallad))
                return new ActionInfo(MagesBallad, "Mage's Ballad", true);
            if (actionManager.CanUseAction(ArmysPaeon))
                return new ActionInfo(ArmysPaeon, "Army's Paeon", true);
        }

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Radiant Encore (new Dawntrail followup)
        if (GameState.HasStatus(RadiantEncoreReady) && actionManager.CanUseAction(RadiantEncore))
            return new ActionInfo(RadiantEncore, "Radiant Encore");

        // Resonant Arrow (new Dawntrail)
        if (GameState.HasStatus(ResonantArrowReady) && actionManager.CanUseAction(ResonantArrow))
            return new ActionInfo(ResonantArrow, "Resonant Arrow");

        // Blast Arrow (followup to Apex Arrow)
        if (GameState.HasStatus(BlastArrowReady) && actionManager.CanUseAction(BlastArrow))
            return new ActionInfo(BlastArrow, "Blast Arrow");

        // Apex Arrow - spend Soul Voice at 80+ (or 100 for max potency)
        if (actionManager.CanUseAction(ApexArrow))
            return new ActionInfo(ApexArrow, "Apex Arrow");

        // Refulgent Arrow (proc from Straight Shot Ready)
        if (GameState.HasStatus(StraightShotReady))
        {
            if (actionManager.CanUseAction(RefulgentArrow))
                return new ActionInfo(RefulgentArrow, "Refulgent Arrow");
            if (actionManager.CanUseAction(StraightShot))
                return new ActionInfo(StraightShot, "Straight Shot");
        }

        // DoT management
        float causticRemaining = GameState.GetMyStatusDurationOnTarget(CausticBite_DoT);
        float stormRemaining = GameState.GetMyStatusDurationOnTarget(Stormbite_DoT);

        // Iron Jaws to refresh both if both are up and low
        if (causticRemaining > 0 && stormRemaining > 0 &&
            (causticRemaining < 5f || stormRemaining < 5f) &&
            actionManager.CanUseAction(IronJaws))
            return new ActionInfo(IronJaws, "Iron Jaws");

        // Apply DoTs if missing
        if (stormRemaining <= 0)
        {
            if (actionManager.CanUseAction(Stormbite))
                return new ActionInfo(Stormbite, "Stormbite");
            if (actionManager.CanUseAction(Windbite))
                return new ActionInfo(Windbite, "Windbite");
        }

        if (causticRemaining <= 0)
        {
            if (actionManager.CanUseAction(CausticBite))
                return new ActionInfo(CausticBite, "Caustic Bite");
            if (actionManager.CanUseAction(VenomousBite))
                return new ActionInfo(VenomousBite, "Venomous Bite");
        }

        // Filler: Burst Shot
        if (actionManager.CanUseAction(BurstShot))
            return new ActionInfo(BurstShot, "Burst Shot");

        return new ActionInfo(HeavyShot, "Heavy Shot");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        // Radiant Encore
        if (GameState.HasStatus(RadiantEncoreReady) && actionManager.CanUseAction(RadiantEncore))
            return new ActionInfo(RadiantEncore, "Radiant Encore");

        // Blast Arrow
        if (GameState.HasStatus(BlastArrowReady) && actionManager.CanUseAction(BlastArrow))
            return new ActionInfo(BlastArrow, "Blast Arrow");

        // Apex Arrow (works in AoE)
        if (actionManager.CanUseAction(ApexArrow))
            return new ActionInfo(ApexArrow, "Apex Arrow");

        // Shadowbite (proc)
        if (GameState.HasStatus(ShadowbiteReady) && actionManager.CanUseAction(Shadowbite))
            return new ActionInfo(Shadowbite, "Shadowbite");

        // Wide Volley (new Dawntrail AoE)
        if (actionManager.CanUseAction(WideVolley))
            return new ActionInfo(WideVolley, "Wide Volley");

        // Rain of Death (AoE oGCD)
        if (actionManager.CanUseAction(RainOfDeath))
            return new ActionInfo(RainOfDeath, "Rain of Death");

        // AoE filler
        if (actionManager.CanUseAction(Ladonsbite))
            return new ActionInfo(Ladonsbite, "Ladonsbite");

        return new ActionInfo(QuickNock, "Quick Nock");
    }
}
