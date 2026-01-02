using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Summoner rotation based on Icy Veins Dawntrail 7.x guide.
/// Summoner cycles through Demi summons (Bahamut, Phoenix, Solar Bahamut)
/// and elemental primals (Ifrit, Titan, Garuda) with their attunement spells.
/// </summary>
public class SummonerRotation : IRotation
{
    public uint JobId => 27;

    #region Action IDs
    // Basic Spells
    private const uint Ruin = 163;
    private const uint RuinII = 172;
    private const uint RuinIII = 3579;
    private const uint RuinIV = 7426;            // Proc from Egi Assault
    private const uint Outburst = 16511;         // AoE
    private const uint TriDisaster = 25826;      // AoE upgrade

    // Demi Summons
    private const uint SummonBahamut = 7427;
    private const uint SummonPhoenix = 25831;
    private const uint SummonSolarBahamut = 36992; // New Dawntrail

    // Demi Actions
    private const uint AstralImpulse = 25820;    // Bahamut
    private const uint FountainOfFire = 16514;   // Phoenix
    private const uint UmbralImpulse = 36994;    // Solar Bahamut
    private const uint EnkindleBahamut = 7429;   // Akh Morn
    private const uint EnkindlePhoenix = 16516;  // Revelation
    private const uint EnkindleSolarBahamut = 36998; // Exodus
    private const uint AstralFlow = 25822;       // Deathflare / Rekindle / Sunflare
    private const uint Deathflare = 3582;
    private const uint Rekindle = 25830;
    private const uint Sunflare = 36996;         // New Dawntrail

    // Primal Summons
    private const uint SummonRuby = 25802;       // Ifrit
    private const uint SummonTopaz = 25803;      // Titan
    private const uint SummonEmerald = 25804;    // Garuda

    // Primal Gems (upgraded summons)
    private const uint SummonIfrit = 25838;
    private const uint SummonTitan = 25839;
    private const uint SummonGaruda = 25840;
    private const uint SummonIfritII = 25838;
    private const uint SummonTitanII = 25839;
    private const uint SummonGarudaII = 25840;

    // Attunement Spells
    // Ruby/Ifrit
    private const uint RubyRuin = 25808;
    private const uint RubyRuinIII = 25817;      // Upgrade
    private const uint RubyOutburst = 25814;     // AoE
    private const uint RubyDisaster = 25827;     // AoE upgrade
    private const uint RubyRite = 25823;         // Combo finisher
    private const uint CrimsonCyclone = 25835;   // Dash
    private const uint CrimsonStrike = 25885;    // Followup

    // Topaz/Titan
    private const uint TopazRuin = 25809;
    private const uint TopazRuinIII = 25818;
    private const uint TopazOutburst = 25815;
    private const uint TopazDisaster = 25828;
    private const uint TopazRite = 25824;
    private const uint MountainBuster = 25836;

    // Emerald/Garuda
    private const uint EmeraldRuin = 25810;
    private const uint EmeraldRuinIII = 25819;
    private const uint EmeraldOutburst = 25816;
    private const uint EmeraldDisaster = 25829;
    private const uint EmeraldRite = 25825;
    private const uint Slipstream = 25837;

    // oGCDs
    private const uint EnergyDrain = 16508;
    private const uint EnergySiphon = 16510;     // AoE
    private const uint Fester = 181;
    private const uint Necrotize = 36990;        // Fester upgrade
    private const uint Painflare = 3578;         // AoE
    private const uint SearingLight = 25801;     // Party buff
    private const uint SearingFlash = 36991;     // New Dawntrail followup
    private const uint LuxSolaris = 36997;       // New Dawntrail heal

    // Utility
    private const uint RadiantAegis = 25799;     // Shield
    private const uint Physick = 190;
    private const uint Resurrection = 173;
    #endregion

    #region Status IDs
    private const uint FurtherRuin = 2701;       // Ruin IV ready
    private const uint SearingLightBuff = 2703;
    private const uint IfritsFavor = 2724;       // Crimson Cyclone ready
    private const uint GarudasFavor = 2725;      // Slipstream ready
    private const uint TitansFavor = 2853;       // Mountain Buster ready
    private const uint RubysGlimmer = 3873;      // New proc
    private const uint RefulgentLux = 3874;      // Lux Solaris ready
    #endregion

    private readonly ActionManager actionManager;

    public SummonerRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.SMN_Enabled) return null;

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
        if (config.SMN_AoE_Enabled && GameState.GetHostileCountAround(player, 10f) >= config.SMN_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Searing Light - party buff
        if (config.SMN_Buff_SearingLight && actionManager.CanUseAction(SearingLight))
            return new ActionInfo(SearingLight, "Searing Light", true);

        // Searing Flash (followup to Searing Light)
        if (actionManager.CanUseAction(SearingFlash))
            return new ActionInfo(SearingFlash, "Searing Flash");

        // Enkindle (Demi summon finisher)
        if (actionManager.CanUseAction(EnkindleSolarBahamut))
            return new ActionInfo(EnkindleSolarBahamut, "Exodus");
        if (actionManager.CanUseAction(EnkindlePhoenix))
            return new ActionInfo(EnkindlePhoenix, "Revelation");
        if (actionManager.CanUseAction(EnkindleBahamut))
            return new ActionInfo(EnkindleBahamut, "Akh Morn");

        // Astral Flow (Deathflare / Rekindle / Sunflare)
        if (actionManager.CanUseAction(Sunflare))
            return new ActionInfo(Sunflare, "Sunflare");
        if (actionManager.CanUseAction(Rekindle))
            return new ActionInfo(Rekindle, "Rekindle");
        if (actionManager.CanUseAction(Deathflare))
            return new ActionInfo(Deathflare, "Deathflare");

        // Mountain Buster (Titan's Favor)
        if (GameState.HasStatus(TitansFavor) && actionManager.CanUseAction(MountainBuster))
            return new ActionInfo(MountainBuster, "Mountain Buster");

        // Lux Solaris (new Dawntrail)
        if (GameState.HasStatus(RefulgentLux) && actionManager.CanUseAction(LuxSolaris))
            return new ActionInfo(LuxSolaris, "Lux Solaris");

        // Energy Drain / Necrotize / Fester
        if (actionManager.CanUseAction(EnergyDrain))
            return new ActionInfo(EnergyDrain, "Energy Drain");

        if (actionManager.CanUseAction(Necrotize))
            return new ActionInfo(Necrotize, "Necrotize");

        if (actionManager.CanUseAction(Fester))
            return new ActionInfo(Fester, "Fester");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Demi Phase Actions (during Bahamut/Phoenix/Solar Bahamut)
        if (actionManager.CanUseAction(UmbralImpulse))
            return new ActionInfo(UmbralImpulse, "Umbral Impulse");
        if (actionManager.CanUseAction(FountainOfFire))
            return new ActionInfo(FountainOfFire, "Fountain of Fire");
        if (actionManager.CanUseAction(AstralImpulse))
            return new ActionInfo(AstralImpulse, "Astral Impulse");

        // Summon Demi (priority)
        if (actionManager.CanUseAction(SummonSolarBahamut))
            return new ActionInfo(SummonSolarBahamut, "Summon Solar Bahamut", true);
        if (actionManager.CanUseAction(SummonPhoenix))
            return new ActionInfo(SummonPhoenix, "Summon Phoenix", true);
        if (actionManager.CanUseAction(SummonBahamut))
            return new ActionInfo(SummonBahamut, "Summon Bahamut", true);

        // Primal Attunement Actions
        // Crimson Strike (followup to Crimson Cyclone)
        if (actionManager.CanUseAction(CrimsonStrike))
            return new ActionInfo(CrimsonStrike, "Crimson Strike");

        // Crimson Cyclone (Ifrit's Favor)
        if (GameState.HasStatus(IfritsFavor) && actionManager.CanUseAction(CrimsonCyclone))
            return new ActionInfo(CrimsonCyclone, "Crimson Cyclone");

        // Slipstream (Garuda's Favor)
        if (GameState.HasStatus(GarudasFavor) && actionManager.CanUseAction(Slipstream))
            return new ActionInfo(Slipstream, "Slipstream");

        // Ruby/Ifrit attunement
        if (actionManager.CanUseAction(RubyRite))
            return new ActionInfo(RubyRite, "Ruby Rite");
        if (actionManager.CanUseAction(RubyRuinIII))
            return new ActionInfo(RubyRuinIII, "Ruby Ruin III");

        // Topaz/Titan attunement
        if (actionManager.CanUseAction(TopazRite))
            return new ActionInfo(TopazRite, "Topaz Rite");
        if (actionManager.CanUseAction(TopazRuinIII))
            return new ActionInfo(TopazRuinIII, "Topaz Ruin III");

        // Emerald/Garuda attunement
        if (actionManager.CanUseAction(EmeraldRite))
            return new ActionInfo(EmeraldRite, "Emerald Rite");
        if (actionManager.CanUseAction(EmeraldRuinIII))
            return new ActionInfo(EmeraldRuinIII, "Emerald Ruin III");

        // Summon Primals (when no attunement active)
        if (actionManager.CanUseAction(SummonIfritII))
            return new ActionInfo(SummonIfritII, "Summon Ifrit II", true);
        if (actionManager.CanUseAction(SummonTitanII))
            return new ActionInfo(SummonTitanII, "Summon Titan II", true);
        if (actionManager.CanUseAction(SummonGarudaII))
            return new ActionInfo(SummonGarudaII, "Summon Garuda II", true);

        // Ruin IV (proc)
        if (GameState.HasStatus(FurtherRuin) && actionManager.CanUseAction(RuinIV))
            return new ActionInfo(RuinIV, "Ruin IV");

        // Filler: Ruin III
        if (actionManager.CanUseAction(RuinIII))
            return new ActionInfo(RuinIII, "Ruin III");

        return new ActionInfo(Ruin, "Ruin");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        // Demi Phase (same spells work for AoE)
        if (actionManager.CanUseAction(UmbralImpulse))
            return new ActionInfo(UmbralImpulse, "Umbral Impulse");
        if (actionManager.CanUseAction(FountainOfFire))
            return new ActionInfo(FountainOfFire, "Fountain of Fire");
        if (actionManager.CanUseAction(AstralImpulse))
            return new ActionInfo(AstralImpulse, "Astral Impulse");

        // Summon Demi
        if (actionManager.CanUseAction(SummonSolarBahamut))
            return new ActionInfo(SummonSolarBahamut, "Summon Solar Bahamut", true);
        if (actionManager.CanUseAction(SummonPhoenix))
            return new ActionInfo(SummonPhoenix, "Summon Phoenix", true);
        if (actionManager.CanUseAction(SummonBahamut))
            return new ActionInfo(SummonBahamut, "Summon Bahamut", true);

        // AoE Attunement spells
        if (actionManager.CanUseAction(CrimsonStrike))
            return new ActionInfo(CrimsonStrike, "Crimson Strike");
        if (GameState.HasStatus(IfritsFavor) && actionManager.CanUseAction(CrimsonCyclone))
            return new ActionInfo(CrimsonCyclone, "Crimson Cyclone");
        if (GameState.HasStatus(GarudasFavor) && actionManager.CanUseAction(Slipstream))
            return new ActionInfo(Slipstream, "Slipstream");

        if (actionManager.CanUseAction(RubyDisaster))
            return new ActionInfo(RubyDisaster, "Ruby Disaster");
        if (actionManager.CanUseAction(TopazDisaster))
            return new ActionInfo(TopazDisaster, "Topaz Disaster");
        if (actionManager.CanUseAction(EmeraldDisaster))
            return new ActionInfo(EmeraldDisaster, "Emerald Disaster");

        // Summon Primals
        if (actionManager.CanUseAction(SummonIfritII))
            return new ActionInfo(SummonIfritII, "Summon Ifrit II", true);
        if (actionManager.CanUseAction(SummonTitanII))
            return new ActionInfo(SummonTitanII, "Summon Titan II", true);
        if (actionManager.CanUseAction(SummonGarudaII))
            return new ActionInfo(SummonGarudaII, "Summon Garuda II", true);

        // Energy Siphon (AoE)
        if (actionManager.CanUseAction(EnergySiphon))
            return new ActionInfo(EnergySiphon, "Energy Siphon");

        // Painflare (AoE oGCD)
        if (actionManager.CanUseAction(Painflare))
            return new ActionInfo(Painflare, "Painflare");

        // Ruin IV
        if (GameState.HasStatus(FurtherRuin) && actionManager.CanUseAction(RuinIV))
            return new ActionInfo(RuinIV, "Ruin IV");

        // AoE filler
        if (actionManager.CanUseAction(TriDisaster))
            return new ActionInfo(TriDisaster, "Tri-disaster");

        return new ActionInfo(Outburst, "Outburst");
    }
}
