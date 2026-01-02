using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Dancer rotation based on Icy Veins Dawntrail 7.x guide.
/// Dancer uses proc-based combos, Standard/Technical Steps for buffs,
/// and has strong party support capabilities.
/// </summary>
public class DancerRotation : IRotation
{
    public uint JobId => 38;

    #region Action IDs
    // Basic Combo (proc-based)
    private const uint Cascade = 15989;
    private const uint Fountain = 15990;
    private const uint ReverseCascade = 15991;   // Proc from Cascade
    private const uint Fountainfall = 15992;     // Proc from Fountain

    // AoE Combo
    private const uint Windmill = 15993;
    private const uint Bladeshower = 15994;
    private const uint RisingWindmill = 15995;   // Proc
    private const uint Bloodshower = 15996;      // Proc

    // Dances
    private const uint StandardStep = 15997;
    private const uint StandardFinish = 16003;
    private const uint DoubleStandardFinish = 16192; // 2-step version
    private const uint TechnicalStep = 15998;
    private const uint TechnicalFinish = 16004;
    private const uint QuadrupleTechnicalFinish = 16196;
    private const uint Tillana = 25790;          // Followup to Technical
    private const uint LastDance = 36983;        // New Dawntrail (Standard followup)
    private const uint FinishingMove = 36984;    // New Dawntrail
    private const uint DanceOfTheDawn = 36985;   // New Dawntrail

    // Dance Steps
    private const uint Emboite = 15999;          // Red
    private const uint Entrechat = 16000;        // Blue
    private const uint Jete = 16001;             // Green
    private const uint Pirouette = 16002;        // Yellow

    // Esprit Spenders
    private const uint SaberDance = 16005;
    private const uint StarfallDance = 25792;    // During Technical buff

    // Feather Spenders
    private const uint FanDance = 16007;
    private const uint FanDanceII = 16008;       // AoE
    private const uint FanDanceIII = 16009;      // Proc from I/II
    private const uint FanDanceIV = 25791;       // Proc

    // Buffs/oGCDs
    private const uint Flourish = 16013;
    private const uint Devilment = 16011;
    private const uint ClosedPosition = 16006;   // Dance Partner

    // Utility
    private const uint Improvisation = 16014;
    private const uint CuringWaltz = 16015;
    private const uint ShieldSamba = 16012;
    private const uint EnAvant = 16010;
    #endregion

    #region Status IDs
    private const uint SilkenSymmetry = 2693;    // Reverse Cascade ready
    private const uint SilkenFlow = 2694;        // Fountainfall ready
    private const uint FlourishingSymmetry = 3017;
    private const uint FlourishingFlow = 3018;
    private const uint ThreefoldFanDance = 1820; // Fan Dance III ready
    private const uint FourfoldFanDance = 2699;  // Fan Dance IV ready
    private const uint StandardFinishBuff = 1821;
    private const uint TechnicalFinishBuff = 1822;
    private const uint DevilmentBuff = 1825;
    private const uint DanceOfTheDawnReady = 3867;
    private const uint FinishingMoveReady = 3868;
    private const uint LastDanceReady = 3867;
    private const uint StandardStepBuff = 1818;
    private const uint TechnicalStepBuff = 1819;
    private const uint FlourishingStarfall = 2700;
    #endregion

    private readonly ActionManager actionManager;

    public DancerRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.DNC_Enabled) return null;

        var player = GameState.LocalPlayer;
        if (player == null || !player.StatusFlags.HasFlag(StatusFlags.InCombat)) return null;

        var target = GameState.TargetAsBattleChara;
        if (target == null) return null;

        // Check if in dance mode
        if (GameState.HasStatus(StandardStepBuff) || GameState.HasStatus(TechnicalStepBuff))
            return GetDanceStep(player);

        // oGCD weaving
        if (actionManager.CanWeave())
        {
            var oGCD = GetOGCDAction(player, target, config);
            if (oGCD != null) return oGCD;
        }

        // AoE check
        if (config.DNC_AoE_Enabled && GameState.GetHostileCountAround(player, 10f) >= config.DNC_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetDanceStep(IPlayerCharacter player)
    {
        // During dance, use the correct step or finish
        // The game replaces these automatically, but we check anyway
        if (actionManager.CanUseAction(QuadrupleTechnicalFinish))
            return new ActionInfo(QuadrupleTechnicalFinish, "Quadruple Technical Finish");

        if (actionManager.CanUseAction(TechnicalFinish))
            return new ActionInfo(TechnicalFinish, "Technical Finish");

        if (actionManager.CanUseAction(DoubleStandardFinish))
            return new ActionInfo(DoubleStandardFinish, "Double Standard Finish");

        if (actionManager.CanUseAction(StandardFinish))
            return new ActionInfo(StandardFinish, "Standard Finish");

        // Dance steps - the game shows which one to press
        if (actionManager.CanUseAction(Emboite))
            return new ActionInfo(Emboite, "Emboite");
        if (actionManager.CanUseAction(Entrechat))
            return new ActionInfo(Entrechat, "Entrechat");
        if (actionManager.CanUseAction(Jete))
            return new ActionInfo(Jete, "Jete");
        if (actionManager.CanUseAction(Pirouette))
            return new ActionInfo(Pirouette, "Pirouette");

        // Fallback - shouldn't reach here
        return new ActionInfo(StandardStep, "Standard Step");
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Technical Step (start dance - 2min burst)
        if (config.DNC_Buff_TechnicalStep && actionManager.CanUseAction(TechnicalStep))
            return new ActionInfo(TechnicalStep, "Technical Step", true);

        // Standard Step (start dance - every 30s)
        if (config.DNC_Buff_StandardStep && actionManager.CanUseAction(StandardStep))
            return new ActionInfo(StandardStep, "Standard Step", true);

        // Devilment - personal + partner buff
        if (config.DNC_Buff_Devilment && actionManager.CanUseAction(Devilment))
            return new ActionInfo(Devilment, "Devilment", true);

        // Flourish - grants all procs
        if (config.DNC_Buff_Flourish && actionManager.CanUseAction(Flourish))
            return new ActionInfo(Flourish, "Flourish", true);

        // Fan Dance IV (proc)
        if (GameState.HasStatus(FourfoldFanDance) && actionManager.CanUseAction(FanDanceIV))
            return new ActionInfo(FanDanceIV, "Fan Dance IV");

        // Fan Dance III (proc from I/II)
        if (GameState.HasStatus(ThreefoldFanDance) && actionManager.CanUseAction(FanDanceIII))
            return new ActionInfo(FanDanceIII, "Fan Dance III");

        // Fan Dance (feather spender)
        if (actionManager.CanUseAction(FanDance))
            return new ActionInfo(FanDance, "Fan Dance");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Dance of the Dawn (new Dawntrail - Technical window)
        if (GameState.HasStatus(DanceOfTheDawnReady) && actionManager.CanUseAction(DanceOfTheDawn))
            return new ActionInfo(DanceOfTheDawn, "Dance of the Dawn");

        // Finishing Move (new Dawntrail)
        if (GameState.HasStatus(FinishingMoveReady) && actionManager.CanUseAction(FinishingMove))
            return new ActionInfo(FinishingMove, "Finishing Move");

        // Last Dance (new Dawntrail - Standard followup)
        if (GameState.HasStatus(LastDanceReady) && actionManager.CanUseAction(LastDance))
            return new ActionInfo(LastDance, "Last Dance");

        // Tillana (Technical Finish followup)
        if (actionManager.CanUseAction(Tillana))
            return new ActionInfo(Tillana, "Tillana");

        // Starfall Dance (during Technical buff)
        if (GameState.HasStatus(FlourishingStarfall) && actionManager.CanUseAction(StarfallDance))
            return new ActionInfo(StarfallDance, "Starfall Dance");

        // Saber Dance (Esprit spender - use at 50+)
        if (actionManager.CanUseAction(SaberDance))
            return new ActionInfo(SaberDance, "Saber Dance");

        // Proc: Fountainfall (from Silken Flow)
        if ((GameState.HasStatus(SilkenFlow) || GameState.HasStatus(FlourishingFlow)) &&
            actionManager.CanUseAction(Fountainfall))
            return new ActionInfo(Fountainfall, "Fountainfall");

        // Proc: Reverse Cascade (from Silken Symmetry)
        if ((GameState.HasStatus(SilkenSymmetry) || GameState.HasStatus(FlourishingSymmetry)) &&
            actionManager.CanUseAction(ReverseCascade))
            return new ActionInfo(ReverseCascade, "Reverse Cascade");

        // Basic combo
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        if (adjustedLast == Cascade)
            return new ActionInfo(Fountain, "Fountain");

        return new ActionInfo(Cascade, "Cascade");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        // Dance of the Dawn
        if (GameState.HasStatus(DanceOfTheDawnReady) && actionManager.CanUseAction(DanceOfTheDawn))
            return new ActionInfo(DanceOfTheDawn, "Dance of the Dawn");

        // Tillana
        if (actionManager.CanUseAction(Tillana))
            return new ActionInfo(Tillana, "Tillana");

        // Starfall Dance
        if (GameState.HasStatus(FlourishingStarfall) && actionManager.CanUseAction(StarfallDance))
            return new ActionInfo(StarfallDance, "Starfall Dance");

        // Saber Dance
        if (actionManager.CanUseAction(SaberDance))
            return new ActionInfo(SaberDance, "Saber Dance");

        // Fan Dance II (AoE feather)
        if (actionManager.CanUseAction(FanDanceII))
            return new ActionInfo(FanDanceII, "Fan Dance II");

        // AoE Procs
        if ((GameState.HasStatus(SilkenFlow) || GameState.HasStatus(FlourishingFlow)) &&
            actionManager.CanUseAction(Bloodshower))
            return new ActionInfo(Bloodshower, "Bloodshower");

        if ((GameState.HasStatus(SilkenSymmetry) || GameState.HasStatus(FlourishingSymmetry)) &&
            actionManager.CanUseAction(RisingWindmill))
            return new ActionInfo(RisingWindmill, "Rising Windmill");

        // AoE combo
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        if (adjustedLast == Windmill)
            return new ActionInfo(Bladeshower, "Bladeshower");

        return new ActionInfo(Windmill, "Windmill");
    }
}
