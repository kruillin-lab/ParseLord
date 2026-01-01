using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace AutoRotationPlugin
{
    public enum TargetPriority
    {
        None = 0,
        Nearest = 1,
        LowestHP = 2,
        HighestMaxHP = 3,
        TankAssist = 4
    }

    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // --- GLOBAL ---
        public bool Enabled { get; set; } = false;
        public bool InCombatOnly { get; set; } = true;
        public bool UseAoE { get; set; } = true;
        public int AoETargetCount { get; set; } = 3;

        // --- TARGETING ---
        public TargetPriority TargetPriority { get; set; } = TargetPriority.Nearest;
        public bool AutoTargetSwitch { get; set; } = true;
        public float AutoTargetRange { get; set; } = 25f;
        public float AutoTargetAngle { get; set; } = 180f;

        // --- DRAGOON (DRG) ---
        public bool DRG_Enabled { get; set; } = false;

        // Buffs
        public bool DRG_Buff_LanceCharge { get; set; } = true;
        public bool DRG_Buff_BattleLitany { get; set; } = true;
        public bool DRG_Buff_LifeSurge { get; set; } = true;

        // Jumps & Gauge
        public bool DRG_Jump_HighJump { get; set; } = true;
        public bool DRG_Jump_DragonfireDive { get; set; } = true;
        public bool DRG_Gauge_Geirskogul { get; set; } = true;
        public bool DRG_Gauge_WyrmwindThrust { get; set; } = true;

        // AoE
        public bool DRG_AoE_Enabled { get; set; } = true;
        public int DRG_AoE_Threshold { get; set; } = 3;

        // --- PALADIN (PLD) ---
        public bool PLD_Enabled { get; set; } = false;

        // Defensive
        public bool PLD_Def_Sheltron { get; set; } = true;

        // Offense
        public bool PLD_Buff_FightOrFlight { get; set; } = true;
        public bool PLD_Magic_Requiescat { get; set; } = true;

        // AoE
        public bool PLD_AoE_Enabled { get; set; } = true;
        public int PLD_AoE_Threshold { get; set; } = 3;

        // --- WHITE MAGE (WHM) ---
        public bool WHM_Enabled { get; set; } = false;

        // Healing
        public bool WHM_oGCD_Benediction { get; set; } = true;
        public bool WHM_oGCD_Tetra { get; set; } = true;

        // DPS
        public bool WHM_DPS_AoE_Enabled { get; set; } = true;
        public int WHM_DPS_AoE_Threshold { get; set; } = 3;
        public bool WHM_Buff_PresenceOfMind { get; set; } = true;

        #region Standard Config Boilerplate
        [NonSerialized] private IDalamudPluginInterface? pluginInterface;
        public void Initialize(IDalamudPluginInterface pluginInterface) => this.pluginInterface = pluginInterface;
        public void Save() => pluginInterface!.SavePluginConfig(this);
        #endregion
    }
}