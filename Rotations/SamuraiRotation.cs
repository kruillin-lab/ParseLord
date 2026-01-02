using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Samurai rotation based on Icy Veins Dawntrail 7.x guide.
/// Samurai builds Sen (Setsu, Getsu, Ka) through combos and spends them on powerful Iaijutsu.
/// 29 GCD loop with alternating odd/even bursts.
/// </summary>
public class SamuraiRotation : IRotation
{
    public uint JobId => 34;

    #region Action IDs
    // Setsu Combo (Snow)
    private const uint Hakaze = 7477;
    private const uint Gyofu = 36963;            // Hakaze upgrade
    private const uint Yukikaze = 7480;

    // Getsu Combo (Moon)
    private const uint Jinpu = 7478;
    private const uint Gekko = 7481;

    // Ka Combo (Flower)
    private const uint Shifu = 7479;
    private const uint Kasha = 7482;

    // AoE Combos
    private const uint Fuga = 7483;
    private const uint Fuko = 25780;             // Fuga upgrade
    private const uint Mangetsu = 7484;          // Grants Getsu
    private const uint Oka = 7485;               // Grants Ka

    // Iaijutsu (Sen Spenders)
    private const uint Iaijutsu = 7867;
    private const uint Higanbana = 7489;         // 1 Sen - DoT
    private const uint TenkaGoken = 7488;        // 2 Sen - AoE
    private const uint MidareSetsugekka = 7487;  // 3 Sen - Big hit
    private const uint TendoSetsugekka = 36965;  // Midare upgrade
    private const uint TendoGoken = 36967;       // Tenka upgrade
    private const uint TendoKaeshiSetsugekka = 36968;

    // Tsubame-gaeshi (Iaijutsu followups)
    private const uint TsubameGaeshi = 16483;
    private const uint KaeshiGoken = 16485;
    private const uint KaeshiSetsugekka = 16486;
    private const uint KaeshiHiganbana = 16484;

    // Kenki Spenders
    private const uint HissatsuShinten = 7490;
    private const uint HissatsuKyuten = 7491;    // AoE
    private const uint HissatsuGuren = 7496;     // Line AoE
    private const uint HissatsuSenei = 16481;    // Single target
    private const uint Zanshin = 36964;          // New Dawntrail

    // oGCDs / Buffs
    private const uint MeikyoShisui = 7499;
    private const uint Ikishoten = 16482;
    private const uint Shoha = 16487;
    private const uint OgiNamikiri = 25781;
    private const uint KaeshiNamikiri = 25782;

    // Utility
    private const uint ThirdEye = 7498;
    private const uint Tengentsu = 36962;        // Third Eye upgrade
    private const uint Hagakure = 7495;
    private const uint Enpi = 7486;
    private const uint TrueNorth = 7546;
    #endregion

    #region Status IDs
    private const uint Fugetsu = 1298;           // Damage buff from Jinpu/Mangetsu
    private const uint Fuka = 1299;              // Speed buff from Shifu/Oka
    private const uint MeikyoShisuiBuff = 1233;
    private const uint OgiNamikiriReady = 2959;
    private const uint Higanbana_DoT = 1228;
    private const uint ZanshinReady = 3855;
    private const uint Tendo = 3856;             // Enhanced Iaijutsu ready
    #endregion

    private readonly ActionManager actionManager;

    public SamuraiRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.SAM_Enabled) return null;

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
        if (config.SAM_AoE_Enabled && GameState.GetHostileCountAround(player, 5f) >= config.SAM_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Zanshin (new Dawntrail followup)
        if (GameState.HasStatus(ZanshinReady) && actionManager.CanUseAction(Zanshin))
            return new ActionInfo(Zanshin, "Zanshin");

        // Kaeshi: Namikiri (followup to Ogi Namikiri)
        if (actionManager.CanUseAction(KaeshiNamikiri))
            return new ActionInfo(KaeshiNamikiri, "Kaeshi: Namikiri");

        // Meikyo Shisui - combo skip
        if (config.SAM_Buff_MeikyoShisui && actionManager.CanUseAction(MeikyoShisui))
            return new ActionInfo(MeikyoShisui, "Meikyo Shisui", true);

        // Ikishoten - Kenki generator
        if (config.SAM_Buff_Ikishoten && actionManager.CanUseAction(Ikishoten))
            return new ActionInfo(Ikishoten, "Ikishoten", true);

        // Hissatsu: Senei - big single target
        if (config.SAM_Kenki_Senei && actionManager.CanUseAction(HissatsuSenei))
            return new ActionInfo(HissatsuSenei, "Hissatsu: Senei");

        // Hissatsu: Guren - line AoE (use even in ST for burst)
        if (config.SAM_Kenki_Guren && actionManager.CanUseAction(HissatsuGuren))
            return new ActionInfo(HissatsuGuren, "Hissatsu: Guren");

        // Shoha - Meditation stacks spender
        if (actionManager.CanUseAction(Shoha))
            return new ActionInfo(Shoha, "Shoha");

        // Hissatsu: Shinten - Kenki dump
        if (actionManager.CanUseAction(HissatsuShinten))
            return new ActionInfo(HissatsuShinten, "Hissatsu: Shinten");

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // Ogi Namikiri (requires buff from Ikishoten)
        if (GameState.HasStatus(OgiNamikiriReady) && actionManager.CanUseAction(OgiNamikiri))
            return new ActionInfo(OgiNamikiri, "Ogi Namikiri");

        // Tsubame-gaeshi followups
        if (actionManager.CanUseAction(KaeshiSetsugekka))
            return new ActionInfo(KaeshiSetsugekka, "Kaeshi: Setsugekka");

        if (actionManager.CanUseAction(TendoKaeshiSetsugekka))
            return new ActionInfo(TendoKaeshiSetsugekka, "Tendo Kaeshi Setsugekka");

        // Iaijutsu - check Sen and use appropriate finisher
        // Tendo versions if buff is up
        if (GameState.HasStatus(Tendo))
        {
            if (actionManager.CanUseAction(TendoSetsugekka))
                return new ActionInfo(TendoSetsugekka, "Tendo Setsugekka");
        }

        if (actionManager.CanUseAction(MidareSetsugekka))
            return new ActionInfo(MidareSetsugekka, "Midare Setsugekka");

        // Higanbana DoT (1 Sen) - apply if not up or low duration
        float higanbanaRemaining = GameState.GetMyStatusDurationOnTarget(Higanbana_DoT);
        if (higanbanaRemaining < 5f && actionManager.CanUseAction(Higanbana))
            return new ActionInfo(Higanbana, "Higanbana");

        // Meikyo Shisui active - use combo finishers directly
        if (GameState.HasStatus(MeikyoShisuiBuff))
        {
            // Priority: Gekko > Kasha > Yukikaze based on buff needs
            float fugetsuRemaining = GameState.GetStatusDuration(Fugetsu);
            float fukaRemaining = GameState.GetStatusDuration(Fuka);

            if (fugetsuRemaining < 10f && actionManager.CanUseAction(Gekko))
                return new ActionInfo(Gekko, "Gekko");
            if (fukaRemaining < 10f && actionManager.CanUseAction(Kasha))
                return new ActionInfo(Kasha, "Kasha");
            if (actionManager.CanUseAction(Gekko))
                return new ActionInfo(Gekko, "Gekko");
        }

        // Normal combo routing
        // Gekko combo (Jinpu path)
        if (adjustedLast == Jinpu)
            return new ActionInfo(Gekko, "Gekko");

        // Kasha combo (Shifu path)
        if (adjustedLast == Shifu)
            return new ActionInfo(Kasha, "Kasha");

        // Yukikaze combo (direct from Hakaze)
        if (adjustedLast == Hakaze || adjustedLast == Gyofu)
        {
            // Decide which combo to continue based on buff timers
            float fugetsuRemaining = GameState.GetStatusDuration(Fugetsu);
            float fukaRemaining = GameState.GetStatusDuration(Fuka);

            // Maintain buffs first
            if (fugetsuRemaining < 10f)
                return new ActionInfo(Jinpu, "Jinpu");
            if (fukaRemaining < 10f)
                return new ActionInfo(Shifu, "Shifu");

            // Otherwise alternate for Sen
            // Simple: go Jinpu for Gekko
            return new ActionInfo(Jinpu, "Jinpu");
        }

        // Start combo
        if (actionManager.CanUseAction(Gyofu))
            return new ActionInfo(Gyofu, "Gyofu");

        return new ActionInfo(Hakaze, "Hakaze");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        uint lastAction = actionManager.ComboAction;
        uint adjustedLast = actionManager.GetAdjustedActionId(lastAction);

        // AoE Iaijutsu
        if (GameState.HasStatus(Tendo) && actionManager.CanUseAction(TendoGoken))
            return new ActionInfo(TendoGoken, "Tendo Goken");

        if (actionManager.CanUseAction(TenkaGoken))
            return new ActionInfo(TenkaGoken, "Tenka Goken");

        if (actionManager.CanUseAction(KaeshiGoken))
            return new ActionInfo(KaeshiGoken, "Kaeshi: Goken");

        // Ogi Namikiri (works in AoE too)
        if (GameState.HasStatus(OgiNamikiriReady) && actionManager.CanUseAction(OgiNamikiri))
            return new ActionInfo(OgiNamikiri, "Ogi Namikiri");

        // AoE Kenki spender
        if (actionManager.CanUseAction(HissatsuKyuten))
            return new ActionInfo(HissatsuKyuten, "Hissatsu: Kyuten");

        // Meikyo - use AoE finishers
        if (GameState.HasStatus(MeikyoShisuiBuff))
        {
            if (actionManager.CanUseAction(Mangetsu))
                return new ActionInfo(Mangetsu, "Mangetsu");
            if (actionManager.CanUseAction(Oka))
                return new ActionInfo(Oka, "Oka");
        }

        // AoE combo
        if (adjustedLast == Fuga || adjustedLast == Fuko)
        {
            // Alternate Mangetsu and Oka for both buffs
            float fugetsuRemaining = GameState.GetStatusDuration(Fugetsu);
            if (fugetsuRemaining < 10f)
                return new ActionInfo(Mangetsu, "Mangetsu");
            return new ActionInfo(Oka, "Oka");
        }

        // Start AoE combo
        if (actionManager.CanUseAction(Fuko))
            return new ActionInfo(Fuko, "Fuko");

        return new ActionInfo(Fuga, "Fuga");
    }
}
