using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace AutoRotationPlugin;

// THE FIX: Define the Enum here so it exists in the 'AutoRotationPlugin' namespace
public enum TargetPriority : int
{
    Closest = 0,
    LowestHP = 1,
    HighestHP = 2,
    MostTargeted = 3
}

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // General & Targeting
    public bool Enabled = false;
    public bool InCombatOnly = true;
    public bool UseAoE = true;
    public float AutoTargetRange = 25.0f;

    // Use the Enum type here instead of a raw int
    public TargetPriority TargetPriority = TargetPriority.Closest;

    // =====================================================
    // TANKS
    // =====================================================

    // Paladin (PLD) Settings
    public bool PLD_Enabled = true;
    public bool PLD_Def_Sheltron = true;
    public bool PLD_AoE_Enabled = true;
    public int PLD_AoE_Threshold = 3;
    public bool PLD_Buff_FightOrFlight = true;
    public bool PLD_Magic_Requiescat = true;

    // Warrior (WAR) Settings
    public bool WAR_Enabled = true;
    public bool WAR_AoE_Enabled = true;
    public int WAR_AoE_Threshold = 3;
    public bool WAR_Buff_InnerRelease = true;

    // Dark Knight (DRK) Settings
    public bool DRK_Enabled = true;
    public bool DRK_AoE_Enabled = true;
    public int DRK_AoE_Threshold = 3;
    public bool DRK_Buff_Delirium = true;
    public bool DRK_Buff_LivingShadow = true;

    // Gunbreaker (GNB) Settings
    public bool GNB_Enabled = true;
    public bool GNB_AoE_Enabled = true;
    public int GNB_AoE_Threshold = 3;
    public bool GNB_Buff_NoMercy = true;
    public bool GNB_Buff_Bloodfest = true;

    // =====================================================
    // HEALERS
    // =====================================================

    // White Mage (WHM) Settings
    public bool WHM_Enabled = true;
    public bool WHM_DPS_AoE_Enabled = true;
    public int WHM_DPS_AoE_Threshold = 3;
    public bool WHM_Buff_PresenceOfMind = true;

    // Scholar (SCH) Settings
    public bool SCH_Enabled = true;
    public bool SCH_AoE_Enabled = true;
    public int SCH_AoE_Threshold = 3;
    public bool SCH_Buff_ChainStratagem = true;

    // Astrologian (AST) Settings
    public bool AST_Enabled = true;
    public bool AST_AoE_Enabled = true;
    public int AST_AoE_Threshold = 3;
    public bool AST_Buff_Divination = true;

    // Sage (SGE) Settings
    public bool SGE_Enabled = true;
    public bool SGE_AoE_Enabled = true;
    public int SGE_AoE_Threshold = 3;

    // =====================================================
    // MELEE DPS
    // =====================================================

    // Dragoon (DRG) Settings
    public bool DRG_Enabled = true;
    public bool DRG_AoE_Enabled = true;
    public int DRG_AoE_Threshold = 3;
    public bool DRG_Buff_LanceCharge = true;
    public bool DRG_Buff_BattleLitany = true;
    public bool DRG_Buff_LifeSurge = true;
    public bool DRG_Jump_HighJump = true;
    public bool DRG_Gauge_Geirskogul = true;
    public bool DRG_Jump_DragonfireDive = true;

    // Monk (MNK) Settings
    public bool MNK_Enabled = true;
    public bool MNK_AoE_Enabled = true;
    public int MNK_AoE_Threshold = 3;
    public bool MNK_Buff_RiddleOfFire = true;
    public bool MNK_Buff_Brotherhood = true;

    // Ninja (NIN) Settings
    public bool NIN_Enabled = true;
    public bool NIN_AoE_Enabled = true;
    public int NIN_AoE_Threshold = 3;
    public int NIN_AoEThreshold = 3;  // Alternate naming for UI compatibility
    public bool NIN_Buff_Mug = true;
    public bool NIN_Buff_Kassatsu = true;
    public bool NIN_Buff_Bunshin = true;

    // Samurai (SAM) Settings
    public bool SAM_Enabled = true;
    public bool SAM_AoE_Enabled = true;
    public int SAM_AoE_Threshold = 3;
    public bool SAM_Buff_MeikyoShisui = true;
    public bool SAM_Buff_Ikishoten = true;
    public bool SAM_Kenki_Senei = true;
    public bool SAM_Kenki_Guren = true;

    // Reaper (RPR) Settings
    public bool RPR_Enabled = true;
    public bool RPR_AoE_Enabled = true;
    public int RPR_AoE_Threshold = 3;
    public bool RPR_Buff_ArcaneCircle = true;
    public bool RPR_Buff_Enshroud = true;
    public bool RPR_oGCD_Gluttony = true;

    // Viper (VPR) Settings
    public bool VPR_Enabled = true;
    public bool VPR_AoE_Enabled = true;
    public int VPR_AoE_Threshold = 3;
    public bool VPR_Buff_Reawaken = true;
    public bool VPR_oGCD_Vicewinder = true;

    // =====================================================
    // PHYSICAL RANGED DPS
    // =====================================================

    // Bard (BRD) Settings
    public bool BRD_Enabled = true;
    public bool BRD_AoE_Enabled = true;
    public int BRD_AoE_Threshold = 3;
    public bool BRD_Buff_RagingStrikes = true;
    public bool BRD_Buff_BattleVoice = true;
    public bool BRD_Buff_RadiantFinale = true;
    public bool BRD_Buff_Barrage = true;

    // Machinist (MCH) Settings
    public bool MCH_Enabled = true;
    public bool MCH_AoE_Enabled = true;
    public int MCH_AoE_Threshold = 3;
    public bool MCH_Buff_Wildfire = true;
    public bool MCH_Buff_BarrelStabilizer = true;
    public bool MCH_Buff_Reassemble = true;
    public bool MCH_Summon_Queen = true;

    // Dancer (DNC) Settings
    public bool DNC_Enabled = true;
    public bool DNC_AoE_Enabled = true;
    public int DNC_AoE_Threshold = 3;
    public bool DNC_Buff_StandardStep = true;
    public bool DNC_Buff_TechnicalStep = true;
    public bool DNC_Buff_Devilment = true;
    public bool DNC_Buff_Flourish = true;

    // =====================================================
    // MAGICAL RANGED DPS (CASTERS)
    // =====================================================

    // Black Mage (BLM) Settings
    public bool BLM_Enabled = true;
    public bool BLM_AoE_Enabled = true;
    public int BLM_AoE_Threshold = 3;
    public bool BLM_Buff_LeyLines = true;
    public bool BLM_Buff_Triplecast = true;

    // Summoner (SMN) Settings
    public bool SMN_Enabled = true;
    public bool SMN_AoE_Enabled = true;
    public int SMN_AoE_Threshold = 3;
    public bool SMN_Buff_SearingLight = true;

    // Red Mage (RDM) Settings
    public bool RDM_Enabled = true;
    public bool RDM_AoE_Enabled = true;
    public int RDM_AoE_Threshold = 3;
    public bool RDM_Buff_Embolden = true;
    public bool RDM_Buff_Manafication = true;
    public bool RDM_Buff_Acceleration = true;

    // Pictomancer (PCT) Settings
    public bool PCT_Enabled = true;
    public bool PCT_AoE_Enabled = true;
    public int PCT_AoE_Threshold = 3;
    public bool PCT_Buff_StarryMuse = true;

    [NonSerialized]
    private IDalamudPluginInterface? PluginInterface;

    
    // Priority Stacks (ReAction-style options + per-ability stack selection)
    public Dictionary<uint, JobPriorityStacksConfig> PriorityStacksByJob = new();

public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.PluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.PluginInterface!.SavePluginConfig(this);
    }
}
