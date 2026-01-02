using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Black Mage rotation based on Icy Veins Dawntrail 7.x guide.
/// Black Mage cycles between Astral Fire (damage) and Umbral Ice (MP recovery),
/// weaving in powerful spells like Despair and Xenoglossy.
/// </summary>
public class BlackMageRotation : IRotation
{
    public uint JobId => 25;

    #region Action IDs
    // Fire Spells
    private const uint Fire = 141;
    private const uint FireII = 147;             // AoE
    private const uint FireIII = 152;            // Astral Fire entry
    private const uint FireIV = 3577;            // Main damage spell
    private const uint Flare = 162;              // AoE finisher
    private const uint Despair = 16505;          // Astral finisher
    private const uint FlareStar = 36989;        // New Dawntrail

    // Ice Spells
    private const uint Blizzard = 142;
    private const uint BlizzardII = 146;         // AoE
    private const uint BlizzardIII = 154;        // Umbral Ice entry
    private const uint BlizzardIV = 3576;        // Umbral Hearts
    private const uint Freeze = 159;             // AoE
    private const uint UmbralSoul = 16506;       // Out of combat Umbral maintenance

    // Thunder (DoT)
    private const uint Thunder = 144;
    private const uint ThunderII = 7447;         // AoE
    private const uint ThunderIII = 153;
    private const uint ThunderIV = 7420;         // AoE
    private const uint HighThunder = 36986;      // Thunder upgrade
    private const uint HighThunderII = 36987;    // AoE upgrade

    // Polyglot Spenders
    private const uint Foul = 7422;              // AoE
    private const uint Xenoglossy = 16507;       // Single target

    // Paradox
    private const uint Paradox = 25797;

    // Enochian / Amplifier
    private const uint Enochian = 3575;          // Now passive trait
    private const uint Amplifier = 25796;        // Instant Polyglot

    // Utility / Movement
    private const uint Transpose = 149;
    private const uint Manaward = 157;
    private const uint AetherialManipulation = 155;
    private const uint BetweenTheLines = 7419;
    private const uint LeyLines = 3573;
    private const uint Retrace = 36988;          // New Dawntrail (move Ley Lines)
    private const uint Triplecast = 7421;
    private const uint Swiftcast = 7561;
    private const uint Sharpcast = 3574;         // Removed in Dawntrail
    private const uint Manafont = 158;
    #endregion

    #region Status IDs
    private const uint AstralFireI = 160;
    private const uint AstralFireII = 161;
    private const uint AstralFireIII = 162;
    private const uint UmbralIceI = 163;
    private const uint UmbralIceII = 164;
    private const uint UmbralIceIII = 165;
    private const uint Firestarter = 165;        // Free Fire III
    private const uint Thundercloud = 164;       // Free Thunder proc
    private const uint LeyLinesBuff = 737;
    private const uint Triplecast_Buff = 1211;
    private const uint EnhancedFlare = 2960;
    private const uint ParadoxReady = 3874;      // Paradox available in Ice
    #endregion

    private readonly ActionManager actionManager;

    public BlackMageRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.BLM_Enabled) return null;

        var player = GameState.LocalPlayer;
        if (player == null || !player.StatusFlags.HasFlag(StatusFlags.InCombat)) return null;

        var target = GameState.TargetAsBattleChara;
        if (target == null) return null;

        // oGCD weaving (BLM has limited weave windows)
        if (actionManager.CanWeave())
        {
            var oGCD = GetOGCDAction(player, target, config);
            if (oGCD != null) return oGCD;
        }

        // AoE check
        if (config.BLM_AoE_Enabled && GameState.GetHostileCountAround(player, 10f) >= config.BLM_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Ley Lines - DPS buff zone
        if (config.BLM_Buff_LeyLines && actionManager.CanUseAction(LeyLines))
            return new ActionInfo(LeyLines, "Ley Lines", true);

        // Amplifier - instant Polyglot
        if (actionManager.CanUseAction(Amplifier))
            return new ActionInfo(Amplifier, "Amplifier", true);

        // Triplecast - for movement
        if (config.BLM_Buff_Triplecast && actionManager.CanUseAction(Triplecast))
            return new ActionInfo(Triplecast, "Triplecast", true);

        // Manafont - MP recovery + extends Astral
        if (actionManager.CanUseAction(Manafont))
            return new ActionInfo(Manafont, "Manafont", true);

        // Retrace (move Ley Lines - new Dawntrail)
        if (GameState.HasStatus(LeyLinesBuff) && actionManager.CanUseAction(Retrace))
            return new ActionInfo(Retrace, "Retrace", true);

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        uint mp = GameState.PlayerMP;
        bool inAstralFire = GameState.HasStatus(AstralFireIII) || GameState.HasStatus(AstralFireII) || GameState.HasStatus(AstralFireI);
        bool inUmbralIce = GameState.HasStatus(UmbralIceIII) || GameState.HasStatus(UmbralIceII) || GameState.HasStatus(UmbralIceI);

        // Flare Star (new Dawntrail - use with Astral Soul stacks)
        if (actionManager.CanUseAction(FlareStar))
            return new ActionInfo(FlareStar, "Flare Star");

        // Xenoglossy (Polyglot spender - use for movement or to dump)
        if (actionManager.CanUseAction(Xenoglossy))
            return new ActionInfo(Xenoglossy, "Xenoglossy");

        // Thundercloud proc (instant Thunder)
        if (GameState.HasStatus(Thundercloud))
        {
            if (actionManager.CanUseAction(HighThunder))
                return new ActionInfo(HighThunder, "High Thunder");
            if (actionManager.CanUseAction(ThunderIII))
                return new ActionInfo(ThunderIII, "Thunder III");
        }

        // Firestarter proc (instant Fire III)
        if (GameState.HasStatus(Firestarter) && actionManager.CanUseAction(FireIII))
            return new ActionInfo(FireIII, "Fire III (Firestarter)");

        // ASTRAL FIRE PHASE
        if (inAstralFire)
        {
            // Despair when low MP (finisher)
            if (mp < 1600 && actionManager.CanUseAction(Despair))
                return new ActionInfo(Despair, "Despair");

            // Fire IV - main damage spell
            if (mp >= 1600 && actionManager.CanUseAction(FireIV))
                return new ActionInfo(FireIV, "Fire IV");

            // Paradox (if available in Fire)
            if (GameState.HasStatus(ParadoxReady) && actionManager.CanUseAction(Paradox))
                return new ActionInfo(Paradox, "Paradox");

            // Fire I to refresh Astral Fire timer if needed
            if (actionManager.CanUseAction(Fire))
                return new ActionInfo(Fire, "Fire");

            // Transition to Ice when out of MP
            if (mp < 800)
            {
                if (actionManager.CanUseAction(BlizzardIII))
                    return new ActionInfo(BlizzardIII, "Blizzard III");
                if (actionManager.CanUseAction(Transpose))
                    return new ActionInfo(Transpose, "Transpose", true);
            }
        }

        // UMBRAL ICE PHASE
        if (inUmbralIce)
        {
            // Thunder DoT maintenance
            float thunderRemaining = GameState.GetMyStatusDurationOnTarget(1210); // High Thunder DoT
            if (thunderRemaining < 5f)
            {
                if (actionManager.CanUseAction(HighThunder))
                    return new ActionInfo(HighThunder, "High Thunder");
                if (actionManager.CanUseAction(ThunderIII))
                    return new ActionInfo(ThunderIII, "Thunder III");
            }

            // Blizzard IV for Umbral Hearts
            if (actionManager.CanUseAction(BlizzardIV))
                return new ActionInfo(BlizzardIV, "Blizzard IV");

            // Paradox (free in Umbral Ice III)
            if (GameState.HasStatus(ParadoxReady) && actionManager.CanUseAction(Paradox))
                return new ActionInfo(Paradox, "Paradox");

            // Transition to Fire when MP is full
            if (mp >= 10000)
            {
                if (actionManager.CanUseAction(FireIII))
                    return new ActionInfo(FireIII, "Fire III");
            }

            // Filler while waiting for MP
            if (actionManager.CanUseAction(Blizzard))
                return new ActionInfo(Blizzard, "Blizzard");
        }

        // NO STANCE - Start rotation
        // Check if we have MP to go into Fire
        if (mp >= 10000)
        {
            if (actionManager.CanUseAction(FireIII))
                return new ActionInfo(FireIII, "Fire III");
        }

        // Otherwise start with Blizzard III
        if (actionManager.CanUseAction(BlizzardIII))
            return new ActionInfo(BlizzardIII, "Blizzard III");

        return new ActionInfo(Blizzard, "Blizzard");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        uint mp = GameState.PlayerMP;
        bool inAstralFire = GameState.HasStatus(AstralFireIII) || GameState.HasStatus(AstralFireII);
        bool inUmbralIce = GameState.HasStatus(UmbralIceIII) || GameState.HasStatus(UmbralIceII);

        // Flare Star
        if (actionManager.CanUseAction(FlareStar))
            return new ActionInfo(FlareStar, "Flare Star");

        // Foul (AoE Polyglot)
        if (actionManager.CanUseAction(Foul))
            return new ActionInfo(Foul, "Foul");

        // Thunder AoE proc
        if (GameState.HasStatus(Thundercloud))
        {
            if (actionManager.CanUseAction(HighThunderII))
                return new ActionInfo(HighThunderII, "High Thunder II");
            if (actionManager.CanUseAction(ThunderIV))
                return new ActionInfo(ThunderIV, "Thunder IV");
        }

        // ASTRAL FIRE
        if (inAstralFire)
        {
            // Flare (AoE finisher)
            if (actionManager.CanUseAction(Flare))
                return new ActionInfo(Flare, "Flare");

            // Fire II
            if (mp >= 3000 && actionManager.CanUseAction(FireII))
                return new ActionInfo(FireII, "Fire II");

            // Transition to Ice
            if (mp < 800)
            {
                if (actionManager.CanUseAction(BlizzardIII))
                    return new ActionInfo(BlizzardIII, "Blizzard III");
            }
        }

        // UMBRAL ICE
        if (inUmbralIce)
        {
            // Freeze for Umbral Hearts
            if (actionManager.CanUseAction(Freeze))
                return new ActionInfo(Freeze, "Freeze");

            // Thunder AoE
            float thunderRemaining = GameState.GetMyStatusDurationOnTarget(1210);
            if (thunderRemaining < 5f)
            {
                if (actionManager.CanUseAction(HighThunderII))
                    return new ActionInfo(HighThunderII, "High Thunder II");
            }

            // Transition to Fire
            if (mp >= 10000)
            {
                if (actionManager.CanUseAction(FireIII))
                    return new ActionInfo(FireIII, "Fire III");
            }
        }

        // Start in Ice
        if (actionManager.CanUseAction(BlizzardIII))
            return new ActionInfo(BlizzardIII, "Blizzard III");

        return new ActionInfo(Blizzard, "Blizzard");
    }
}
