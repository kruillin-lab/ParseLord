using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Enums;

namespace AutoRotationPlugin.Rotations.Jobs;

/// <summary>
/// Pictomancer rotation based on Icy Veins Dawntrail 7.x guide.
/// Pictomancer paints motifs (Creature, Weapon, Landscape) and uses them for attacks.
/// Has a unique canvas/palette system with burst windows around Starry Muse.
/// </summary>
public class PictomancerRotation : IRotation
{
    public uint JobId => 42;

    #region Action IDs
    // Basic Spells (Subtractive Palette)
    private const uint FireInRed = 34650;
    private const uint AeroInGreen = 34651;
    private const uint WaterInBlue = 34652;
    private const uint BlizzardInCyan = 34653;    // Subtractive
    private const uint StoneInYellow = 34654;     // Subtractive
    private const uint ThunderInMagenta = 34655;  // Subtractive

    // AoE Spells
    private const uint FireIIInRed = 34656;
    private const uint AeroIIInGreen = 34657;
    private const uint WaterIIInBlue = 34658;
    private const uint BlizzardIIInCyan = 34659;
    private const uint StoneIIInYellow = 34660;
    private const uint ThunderIIInMagenta = 34661;

    // Holy/Comet (White Paint spenders)
    private const uint HolyInWhite = 34662;
    private const uint CometInBlack = 34663;      // Enhanced Holy

    // Creature Motif
    private const uint CreatureMotif = 34689;
    private const uint PomMotif = 34664;
    private const uint WingMotif = 34665;
    private const uint ClawMotif = 34666;
    private const uint MawMotif = 34667;
    private const uint LivingMuse = 35347;
    private const uint PomMuse = 34670;
    private const uint WingedMuse = 34671;
    private const uint ClawedMuse = 34672;
    private const uint FangedMuse = 34673;
    private const uint MogOfTheAges = 34676;
    private const uint RetributionOfTheMadeen = 34677;

    // Weapon Motif
    private const uint WeaponMotif = 34690;
    private const uint HammerMotif = 34668;
    private const uint SteelMuse = 35348;
    private const uint StrikingMuse = 34674;
    private const uint HammerStamp = 34678;
    private const uint HammerBrush = 34679;
    private const uint PolishingHammer = 34680;

    // Landscape Motif
    private const uint LandscapeMotif = 34691;
    private const uint StarrySkyMotif = 34669;
    private const uint ScenicMuse = 35349;
    private const uint StarryMuse = 34675;       // Big burst buff

    // Rainbow Drip
    private const uint RainbowDrip = 34688;

    // Subtractive Palette
    private const uint SubtractivePalette = 34683;

    // Star Prism
    private const uint StarPrism = 34681;

    // Utility
    private const uint Smudge = 34684;           // Movement
    private const uint TemperaCoat = 34685;      // Shield
    private const uint TemperaGrassa = 34686;    // Party shield
    private const uint Swiftcast = 7561;
    #endregion

    #region Status IDs
    private const uint Aetherhues = 3675;        // Basic combo buff
    private const uint AetherhuesII = 3676;
    private const uint SubtractivePaletteBuff = 3674;
    private const uint MonochromeTones = 3691;   // Comet ready
    private const uint StarryMuseBuff = 3685;
    private const uint HammerTime = 3680;        // Hammer combo active
    private const uint Starstruck = 3681;        // Star Prism ready
    private const uint RainbowBright = 3679;     // Rainbow Drip ready
    private const uint Hyperphantasia = 3688;    // Enhanced during Starry Muse
    private const uint Inspiration = 3689;       // Motif cast speed buff
    private const uint SubtractiveSpectrum = 3690;
    #endregion

    private readonly ActionManager actionManager;

    public PictomancerRotation()
    {
        actionManager = ActionManager.Instance;
    }

    public ActionInfo? GetNextAction(Configuration config)
    {
        if (!config.PCT_Enabled) return null;

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
        if (config.PCT_AoE_Enabled && GameState.GetHostileCountAround(player, 10f) >= config.PCT_AoE_Threshold)
            return GetAoEAction(player, config);

        return GetSingleTargetAction(player, target, config);
    }

    private ActionInfo? GetOGCDAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Star Prism (from Starry Muse buff)
        if (GameState.HasStatus(Starstruck) && actionManager.CanUseAction(StarPrism))
            return new ActionInfo(StarPrism, "Star Prism");

        // Mog of the Ages / Retribution (Creature Muse followups)
        if (actionManager.CanUseAction(RetributionOfTheMadeen))
            return new ActionInfo(RetributionOfTheMadeen, "Retribution of the Madeen");
        if (actionManager.CanUseAction(MogOfTheAges))
            return new ActionInfo(MogOfTheAges, "Mog of the Ages");

        // Starry Muse (big burst window)
        if (config.PCT_Buff_StarryMuse && actionManager.CanUseAction(StarryMuse))
            return new ActionInfo(StarryMuse, "Starry Muse", true);

        // Living Muse (Creature canvas)
        if (actionManager.CanUseAction(FangedMuse))
            return new ActionInfo(FangedMuse, "Fanged Muse");
        if (actionManager.CanUseAction(ClawedMuse))
            return new ActionInfo(ClawedMuse, "Clawed Muse");
        if (actionManager.CanUseAction(WingedMuse))
            return new ActionInfo(WingedMuse, "Winged Muse");
        if (actionManager.CanUseAction(PomMuse))
            return new ActionInfo(PomMuse, "Pom Muse");

        // Steel Muse (Weapon canvas)
        if (actionManager.CanUseAction(StrikingMuse))
            return new ActionInfo(StrikingMuse, "Striking Muse");

        // Scenic Muse handled in GCD section (Starry Muse)

        // Subtractive Palette
        if (actionManager.CanUseAction(SubtractivePalette))
            return new ActionInfo(SubtractivePalette, "Subtractive Palette", true);

        return null;
    }

    private ActionInfo? GetSingleTargetAction(IPlayerCharacter player, IBattleChara target, Configuration config)
    {
        // Rainbow Drip (proc from Subtractive combo)
        if (GameState.HasStatus(RainbowBright) && actionManager.CanUseAction(RainbowDrip))
            return new ActionInfo(RainbowDrip, "Rainbow Drip");

        // Hammer Combo (from Striking Muse)
        if (GameState.HasStatus(HammerTime))
        {
            if (actionManager.CanUseAction(PolishingHammer))
                return new ActionInfo(PolishingHammer, "Polishing Hammer");
            if (actionManager.CanUseAction(HammerBrush))
                return new ActionInfo(HammerBrush, "Hammer Brush");
            if (actionManager.CanUseAction(HammerStamp))
                return new ActionInfo(HammerStamp, "Hammer Stamp");
        }

        // Comet in Black (enhanced Holy)
        if (GameState.HasStatus(MonochromeTones) && actionManager.CanUseAction(CometInBlack))
            return new ActionInfo(CometInBlack, "Comet in Black");

        // Holy in White (White Paint spender)
        if (actionManager.CanUseAction(HolyInWhite))
            return new ActionInfo(HolyInWhite, "Holy in White");

        // Motif painting (during downtime or when canvas empty)
        // Priority: Creature > Weapon > Landscape
        if (actionManager.CanUseAction(MawMotif))
            return new ActionInfo(MawMotif, "Maw Motif");
        if (actionManager.CanUseAction(ClawMotif))
            return new ActionInfo(ClawMotif, "Claw Motif");
        if (actionManager.CanUseAction(WingMotif))
            return new ActionInfo(WingMotif, "Wing Motif");
        if (actionManager.CanUseAction(PomMotif))
            return new ActionInfo(PomMotif, "Pom Motif");

        if (actionManager.CanUseAction(HammerMotif))
            return new ActionInfo(HammerMotif, "Hammer Motif");

        if (actionManager.CanUseAction(StarrySkyMotif))
            return new ActionInfo(StarrySkyMotif, "Starry Sky Motif");

        // Subtractive Palette Combo (Cyan > Yellow > Magenta)
        if (GameState.HasStatus(SubtractivePaletteBuff))
        {
            if (actionManager.CanUseAction(ThunderInMagenta))
                return new ActionInfo(ThunderInMagenta, "Thunder in Magenta");
            if (actionManager.CanUseAction(StoneInYellow))
                return new ActionInfo(StoneInYellow, "Stone in Yellow");
            if (actionManager.CanUseAction(BlizzardInCyan))
                return new ActionInfo(BlizzardInCyan, "Blizzard in Cyan");
        }

        // Basic Combo (Fire > Aero > Water)
        if (GameState.HasStatus(AetherhuesII))
        {
            if (actionManager.CanUseAction(WaterInBlue))
                return new ActionInfo(WaterInBlue, "Water in Blue");
        }

        if (GameState.HasStatus(Aetherhues))
        {
            if (actionManager.CanUseAction(AeroInGreen))
                return new ActionInfo(AeroInGreen, "Aero in Green");
        }

        // Start combo
        if (actionManager.CanUseAction(FireInRed))
            return new ActionInfo(FireInRed, "Fire in Red");

        return new ActionInfo(FireInRed, "Fire in Red");
    }

    private ActionInfo? GetAoEAction(IPlayerCharacter player, Configuration config)
    {
        // Rainbow Drip
        if (GameState.HasStatus(RainbowBright) && actionManager.CanUseAction(RainbowDrip))
            return new ActionInfo(RainbowDrip, "Rainbow Drip");

        // Hammer Combo (works in AoE)
        if (GameState.HasStatus(HammerTime))
        {
            if (actionManager.CanUseAction(PolishingHammer))
                return new ActionInfo(PolishingHammer, "Polishing Hammer");
            if (actionManager.CanUseAction(HammerBrush))
                return new ActionInfo(HammerBrush, "Hammer Brush");
            if (actionManager.CanUseAction(HammerStamp))
                return new ActionInfo(HammerStamp, "Hammer Stamp");
        }

        // Comet in Black
        if (GameState.HasStatus(MonochromeTones) && actionManager.CanUseAction(CometInBlack))
            return new ActionInfo(CometInBlack, "Comet in Black");

        // Holy in White
        if (actionManager.CanUseAction(HolyInWhite))
            return new ActionInfo(HolyInWhite, "Holy in White");

        // Motifs
        if (actionManager.CanUseAction(MawMotif))
            return new ActionInfo(MawMotif, "Maw Motif");
        if (actionManager.CanUseAction(HammerMotif))
            return new ActionInfo(HammerMotif, "Hammer Motif");
        if (actionManager.CanUseAction(StarrySkyMotif))
            return new ActionInfo(StarrySkyMotif, "Starry Sky Motif");

        // AoE Subtractive Combo
        if (GameState.HasStatus(SubtractivePaletteBuff))
        {
            if (actionManager.CanUseAction(ThunderIIInMagenta))
                return new ActionInfo(ThunderIIInMagenta, "Thunder II in Magenta");
            if (actionManager.CanUseAction(StoneIIInYellow))
                return new ActionInfo(StoneIIInYellow, "Stone II in Yellow");
            if (actionManager.CanUseAction(BlizzardIIInCyan))
                return new ActionInfo(BlizzardIIInCyan, "Blizzard II in Cyan");
        }

        // AoE Basic Combo
        if (GameState.HasStatus(AetherhuesII))
        {
            if (actionManager.CanUseAction(WaterIIInBlue))
                return new ActionInfo(WaterIIInBlue, "Water II in Blue");
        }

        if (GameState.HasStatus(Aetherhues))
        {
            if (actionManager.CanUseAction(AeroIIInGreen))
                return new ActionInfo(AeroIIInGreen, "Aero II in Green");
        }

        if (actionManager.CanUseAction(FireIIInRed))
            return new ActionInfo(FireIIInRed, "Fire II in Red");

        return new ActionInfo(FireIIInRed, "Fire II in Red");
    }
}
