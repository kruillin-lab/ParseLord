using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Ninja rotation based on Icy Veins Dawntrail 7.x guide.
/// Ninja focuses on burst windows with Kunai's Bane, using Ninjutsu for big damage.
/// Builder phase -> Burst phase cycle.
/// </summary>
public class NinjaRotation : IRotation
{
    public uint JobId => 30;

    #region Action IDs
    // Basic Combo
    private const uint SpinningEdge = 2240;
    private const uint GustSlash = 2242;
    private const uint AeolianEdge = 2255;
    private const uint ArmorCrush = 3563;

    // AoE Combo
    private const uint DeathBlossom = 2254;
    private const uint HakkeMujinsatsu = 16488;

    // Ninjutsu (Mudras)
    private const uint Ten = 2259;
    private const uint Chi = 2261;
    private const uint Jin = 2263;
    private const uint Ninjutsu = 2260;
    private const uint FumaShuriken = 2265;
    private const uint Raiton = 2267;
    private const uint Suiton = 2271;
    private const uint Huton = 2269;           // AoE haste
    private const uint Doton = 2270;           // AoE ground DoT
    private const uint Katon = 2266;           // AoE damage
    private const uint Hyoton = 2268;          // Bind
    private const uint GokaMekkyaku = 16491;   // Enhanced Katon
    private const uint HyoshoRanryu = 16492;   // Enhanced Hyoton

    // Ninki Spenders
    private const uint Bhavacakra = 7402;
    private const uint HellfrogMedium = 7401;  // AoE Ninki
    private const uint Bunshin = 16493;
    private const uint Meisui = 16489;
    private const uint Zesho = 36958;          // New Dawntrail

    // oGCDs
    private const uint Mug = 2248;
    private const uint TrickAttack = 2258;     // Now Kunai's Bane
    private const uint KunaisBane = 36958;
    private const uint DreamWithinADream = 3566;
    private const uint Assassinate = 2246;
    private const uint Shukuchi = 2262;
    private const uint Kassatsu = 2264;
    private const uint TenChiJin = 7403;
    private const uint TenriJindo = 36961;     // TCJ finisher

    // Utility
    private const uint ShadeShift = 2241;
    private const uint Hide = 2245;
    private const uint TrueNorth = 7546;
    #endregion

    #region Status IDs
    private const uint Kazematoi = 3713;       // From Armor Crush
    private const uint KunaisBaneBuff = 3906;  // Vuln on target
    private const uint Suiton_Buff = 507;      // Enables Trick Attack
    private const uint Kassatsu_Buff = 497;
    private const uint TenChiJinBuff = 1186;
    private const uint Bunshin_Buff = 1954;
    private const uint RaijuReady = 3012;
    private const uint PhantomReady = 3681;
    private const uint TenriJindoReady = 3851;
    #endregion

    private readonly ActionManager actionManager;

    public NinjaRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.NIN_Enabled) return null;

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
        if (config.NIN_AoE_Enabled && GameState.GetHostileCountAround(player, 5f) >= config.NIN_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Ten Chi Jin finisher
        if (GameState.HasStatus(TenriJindoReady) && actionManager.CanUseAction(TenriJindo))
            return new ActionInfo(TenriJindo, "Tenri Jindo");

        // Mug (Ninki generator + vuln debuff)
        if (config.NIN_Buff_Mug && actionManager.CanUseAction(Mug))
            return new ActionInfo(Mug, "Mug");

        // Kunai's Bane (requires Suiton buff)
        if (GameState.HasStatus(Suiton_Buff) && actionManager.CanUseAction(KunaisBane))
            return new ActionInfo(KunaisBane, "Kunai's Bane");

        if (GameState.HasStatus(Suiton_Buff) && actionManager.CanUseAction(TrickAttack))
            return new ActionInfo(TrickAttack, "Trick Attack");

        // Dream Within a Dream
        if (actionManager.CanUseAction(DreamWithinADream))
            return new ActionInfo(DreamWithinADream, "Dream Within a Dream");

        // Kassatsu (enhanced ninjutsu)
        if (config.NIN_Buff_Kassatsu && actionManager.CanUseAction(Kassatsu))
            return new ActionInfo(Kassatsu, "Kassatsu", true);

        // Bunshin
        if (config.NIN_Buff_Bunshin && actionManager.CanUseAction(Bunshin))
            return new ActionInfo(Bunshin, "Bunshin", true);

        // Ten Chi Jin
        if (actionManager.CanUseAction(TenChiJin))
            return new ActionInfo(TenChiJin, "Ten Chi Jin", true);

        // Bhavacakra (Ninki spender)
        if (actionManager.CanUseAction(Bhavacakra))
            return new ActionInfo(Bhavacakra, "Bhavacakra");

        // Zesho Meppo (new Dawntrail)
        if (actionManager.CanUseAction(Zesho))
            return new ActionInfo(Zesho, "Zesho Meppo");

        // Meisui (converts Suiton buff to Ninki when not using Trick)
        if (GameState.HasStatus(Suiton_Buff) && actionManager.CanUseAction(Meisui))
            return new ActionInfo(Meisui, "Meisui", true);

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // Raiju Ready (from Raiton)
        if (GameState.HasStatus(RaijuReady))
        {
            uint fleetingRaiju = 25778;
            uint forkedRaiju = 25777;
            if (actionManager.CanUseAction(fleetingRaiju))
                return new ActionInfo(fleetingRaiju, "Fleeting Raiju");
            if (actionManager.CanUseAction(forkedRaiju))
                return new ActionInfo(forkedRaiju, "Forked Raiju");
        }

        // Phantom Kamaitachi (from Bunshin)
        if (GameState.HasStatus(PhantomReady))
        {
            uint phantomKamaitachi = 25774;
            if (actionManager.CanUseAction(phantomKamaitachi))
                return new ActionInfo(phantomKamaitachi, "Phantom Kamaitachi");
        }

        // Ninjutsu priority - use Raiton for damage, Suiton to setup Trick
        // Check if we have mudras prepared (simplified - real implementation would track mudra state)
        if (actionManager.CanUseAction(HyoshoRanryu))
            return new ActionInfo(HyoshoRanryu, "Hyosho Ranryu");

        if (actionManager.CanUseAction(Raiton))
            return new ActionInfo(Raiton, "Raiton");

        if (actionManager.CanUseAction(Suiton))
            return new ActionInfo(Suiton, "Suiton");

        if (actionManager.CanUseAction(FumaShuriken))
            return new ActionInfo(FumaShuriken, "Fuma Shuriken");

        // Combo actions
        // 3rd hit - Aeolian Edge or Armor Crush
        if (adjustedLast == GustSlash)
        {
            // Use Armor Crush to maintain Kazematoi stacks, otherwise Aeolian Edge
            float kazematoiDuration = GameState.GetStatusDuration(Kazematoi);
            if (kazematoiDuration < 10f && actionManager.CanUseAction(ArmorCrush))
                return new ActionInfo(ArmorCrush, "Armor Crush");

            return new ActionInfo(AeolianEdge, "Aeolian Edge");
        }

        // 2nd hit
        if (adjustedLast == SpinningEdge)
            return new ActionInfo(GustSlash, "Gust Slash");

        // Start combo
        return new ActionInfo(SpinningEdge, "Spinning Edge");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // AoE Ninki spender
        if (actionManager.CanUseAction(HellfrogMedium))
            return new ActionInfo(HellfrogMedium, "Hellfrog Medium");

        // AoE Ninjutsu
        if (actionManager.CanUseAction(GokaMekkyaku))
            return new ActionInfo(GokaMekkyaku, "Goka Mekkyaku");

        if (actionManager.CanUseAction(Katon))
            return new ActionInfo(Katon, "Katon");

        if (actionManager.CanUseAction(Doton))
            return new ActionInfo(Doton, "Doton");

        // AoE combo
        if (adjustedLast == DeathBlossom)
            return new ActionInfo(HakkeMujinsatsu, "Hakke Mujinsatsu");

        return new ActionInfo(DeathBlossom, "Death Blossom");
    }
}
