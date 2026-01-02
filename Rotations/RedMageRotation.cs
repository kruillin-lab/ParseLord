using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Red Mage rotation based on Icy Veins Dawntrail 7.x guide.
/// Red Mage balances Black and White mana through Dualcast procs,
/// then spends mana on melee combos. Has strong party utility.
/// </summary>
public class RedMageRotation : IRotation
{
    public uint JobId => 35;

    #region Action IDs
    // White Magic (build White Mana)
    private const uint Jolt = 7503;
    private const uint JoltII = 7524;
    private const uint JoltIII = 37004;          // Dawntrail upgrade
    private const uint Veraero = 7507;
    private const uint VeraeroII = 16525;        // AoE
    private const uint VeraeroIII = 25856;
    private const uint Verstone = 7511;          // Proc
    private const uint Verholy = 7526;           // Finisher

    // Black Magic (build Black Mana)
    private const uint Verthunder = 7505;
    private const uint VerthunderII = 16524;     // AoE
    private const uint VerthunderIII = 25855;
    private const uint Verfire = 7510;           // Proc
    private const uint Verflare = 7525;          // Finisher

    // Neutral / Both
    private const uint Impact = 16526;           // AoE Dualcast
    private const uint GrandImpact = 37006;      // New Dawntrail
    private const uint Scatter = 7509;           // AoE

    // Melee Combo
    private const uint Riposte = 7504;
    private const uint EnchantedRiposte = 7527;
    private const uint Zwerchhau = 7512;
    private const uint EnchantedZwerchhau = 7528;
    private const uint Redoublement = 7516;
    private const uint EnchantedRedoublement = 7529;
    private const uint Moulinet = 7513;
    private const uint EnchantedMoulinet = 7530; // AoE melee
    private const uint EnchantedMoulinetDeux = 37002;
    private const uint EnchantedMoulinetTrois = 37003;

    // Finishers
    private const uint Scorch = 16530;           // After Verflare/Verholy
    private const uint Resolution = 25858;       // After Scorch
    private const uint Prefulgence = 37005;      // New Dawntrail

    // oGCDs
    private const uint Embolden = 7520;          // Party buff
    private const uint ViceOfThorns = 37007;     // Embolden followup
    private const uint Manafication = 7521;
    private const uint Acceleration = 7518;
    private const uint Fleche = 7517;
    private const uint ContreSixte = 7519;       // AoE
    private const uint Engagement = 16527;       // Gap closer (replaces Displacement)
    private const uint Displacement = 7515;
    private const uint CorpsACorps = 7506;

    // Utility
    private const uint Verraise = 7523;
    private const uint Vercure = 7514;
    private const uint MagickBarrier = 25857;
    #endregion

    #region Status IDs
    private const uint Dualcast = 1249;
    private const uint VerstoneReady = 1235;
    private const uint VerfireReady = 1234;
    private const uint Acceleration_Buff = 1238;
    private const uint Embolden_Buff = 1239;
    private const uint MagickedSwordplay = 3875; // From Manafication
    private const uint GrandImpactReady = 3877;
    private const uint PrefulgenceReady = 3876;
    private const uint ThornedFlourish = 3878;   // Vice of Thorns ready
    #endregion

    private readonly ActionManager actionManager;

    public RedMageRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.RDM_Enabled) return null;

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
        if (config.RDM_AoE_Enabled && GameState.GetHostileCountAround(player, 10f) >= config.RDM_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Prefulgence (new Dawntrail - from Manafication)
        if (GameState.HasStatus(PrefulgenceReady) && actionManager.CanUseAction(Prefulgence))
            return new ActionInfo(Prefulgence, "Prefulgence");

        // Vice of Thorns (from Embolden)
        if (GameState.HasStatus(ThornedFlourish) && actionManager.CanUseAction(ViceOfThorns))
            return new ActionInfo(ViceOfThorns, "Vice of Thorns");

        // Embolden - party buff
        if (config.RDM_Buff_Embolden && actionManager.CanUseAction(Embolden))
            return new ActionInfo(Embolden, "Embolden", true);

        // Manafication - mana boost + Magicked Swordplay
        if (config.RDM_Buff_Manafication && actionManager.CanUseAction(Manafication))
            return new ActionInfo(Manafication, "Manafication", true);

        // Acceleration - guaranteed proc
        if (config.RDM_Buff_Acceleration && actionManager.CanUseAction(Acceleration))
            return new ActionInfo(Acceleration, "Acceleration", true);

        // Fleche - on cooldown
        if (actionManager.CanUseAction(Fleche))
            return new ActionInfo(Fleche, "Fleche");

        // Contre Sixte - on cooldown
        if (actionManager.CanUseAction(ContreSixte))
            return new ActionInfo(ContreSixte, "Contre Sixte");

        // Engagement
        if (actionManager.CanUseAction(Engagement))
            return new ActionInfo(Engagement, "Engagement");

        // Corps-a-corps (gap closer)
        if (actionManager.CanUseAction(CorpsACorps))
            return new ActionInfo(CorpsACorps, "Corps-a-corps");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        bool hasDualcast = GameState.HasStatus(Dualcast);
        bool hasAcceleration = GameState.HasStatus(Acceleration_Buff);
        bool canInstantCast = hasDualcast || hasAcceleration;

        // Melee Combo Finishers (highest priority)
        if (actionManager.CanUseAction(Resolution))
            return new ActionInfo(Resolution, "Resolution");

        if (actionManager.CanUseAction(Scorch))
            return new ActionInfo(Scorch, "Scorch");

        // Verflare / Verholy (after melee combo)
        if (actionManager.CanUseAction(Verflare))
            return new ActionInfo(Verflare, "Verflare");
        if (actionManager.CanUseAction(Verholy))
            return new ActionInfo(Verholy, "Verholy");

        // Melee Combo (when mana is 50/50+)
        // Check if in combo
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // Continue melee combo
        if (adjustedLast == EnchantedZwerchhau || adjustedLast == Zwerchhau)
        {
            if (actionManager.CanUseAction(EnchantedRedoublement))
                return new ActionInfo(EnchantedRedoublement, "Enchanted Redoublement");
            return new ActionInfo(Redoublement, "Redoublement");
        }

        if (adjustedLast == EnchantedRiposte || adjustedLast == Riposte)
        {
            if (actionManager.CanUseAction(EnchantedZwerchhau))
                return new ActionInfo(EnchantedZwerchhau, "Enchanted Zwerchhau");
            return new ActionInfo(Zwerchhau, "Zwerchhau");
        }

        // Start melee combo (at 50+ mana or Manafication buff)
        if (GameState.HasStatus(MagickedSwordplay) || actionManager.CanUseAction(EnchantedRiposte))
        {
            // Only start if we have enough mana (simplified check)
            if (actionManager.CanUseAction(EnchantedRiposte))
                return new ActionInfo(EnchantedRiposte, "Enchanted Riposte");
        }

        // Grand Impact (from Acceleration)
        if (GameState.HasStatus(GrandImpactReady) && actionManager.CanUseAction(GrandImpact))
            return new ActionInfo(GrandImpact, "Grand Impact");

        // CASTING PHASE
        // With Dualcast/Acceleration - use long cast spells
        if (canInstantCast)
        {
            // Prioritize the mana we need more of (simplified)
            // Check procs first
            if (GameState.HasStatus(VerstoneReady) && actionManager.CanUseAction(Verstone))
                return new ActionInfo(Verstone, "Verstone");
            if (GameState.HasStatus(VerfireReady) && actionManager.CanUseAction(Verfire))
                return new ActionInfo(Verfire, "Verfire");

            // Cast long spells (Verthunder III / Veraero III)
            if (actionManager.CanUseAction(VerthunderIII))
                return new ActionInfo(VerthunderIII, "Verthunder III");
            if (actionManager.CanUseAction(VeraeroIII))
                return new ActionInfo(VeraeroIII, "Veraero III");
        }

        // Procs (instant cast, build Dualcast)
        if (GameState.HasStatus(VerstoneReady) && actionManager.CanUseAction(Verstone))
            return new ActionInfo(Verstone, "Verstone");
        if (GameState.HasStatus(VerfireReady) && actionManager.CanUseAction(Verfire))
            return new ActionInfo(Verfire, "Verfire");

        // Jolt (short cast, builds Dualcast)
        if (actionManager.CanUseAction(JoltIII))
            return new ActionInfo(JoltIII, "Jolt III");
        if (actionManager.CanUseAction(JoltII))
            return new ActionInfo(JoltII, "Jolt II");

        return new ActionInfo(Jolt, "Jolt");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        bool hasDualcast = GameState.HasStatus(Dualcast);
        bool hasAcceleration = GameState.HasStatus(Acceleration_Buff);
        bool canInstantCast = hasDualcast || hasAcceleration;

        // Finishers
        if (actionManager.CanUseAction(Resolution))
            return new ActionInfo(Resolution, "Resolution");
        if (actionManager.CanUseAction(Scorch))
            return new ActionInfo(Scorch, "Scorch");
        if (actionManager.CanUseAction(Verflare))
            return new ActionInfo(Verflare, "Verflare");
        if (actionManager.CanUseAction(Verholy))
            return new ActionInfo(Verholy, "Verholy");

        // AoE Melee Combo (Moulinet)
        if (actionManager.CanUseAction(EnchantedMoulinetTrois))
            return new ActionInfo(EnchantedMoulinetTrois, "Enchanted Moulinet Trois");
        if (actionManager.CanUseAction(EnchantedMoulinetDeux))
            return new ActionInfo(EnchantedMoulinetDeux, "Enchanted Moulinet Deux");
        if (GameState.HasStatus(MagickedSwordplay) && actionManager.CanUseAction(EnchantedMoulinet))
            return new ActionInfo(EnchantedMoulinet, "Enchanted Moulinet");

        // Grand Impact
        if (GameState.HasStatus(GrandImpactReady) && actionManager.CanUseAction(GrandImpact))
            return new ActionInfo(GrandImpact, "Grand Impact");

        // AoE with Dualcast
        if (canInstantCast)
        {
            if (actionManager.CanUseAction(Impact))
                return new ActionInfo(Impact, "Impact");
            if (actionManager.CanUseAction(VerthunderII))
                return new ActionInfo(VerthunderII, "Verthunder II");
            if (actionManager.CanUseAction(VeraeroII))
                return new ActionInfo(VeraeroII, "Veraero II");
        }

        // AoE short cast (builds Dualcast)
        if (actionManager.CanUseAction(Scatter))
            return new ActionInfo(Scatter, "Scatter");
        if (actionManager.CanUseAction(VerthunderII))
            return new ActionInfo(VerthunderII, "Verthunder II");

        return new ActionInfo(Jolt, "Jolt");
    }
}
