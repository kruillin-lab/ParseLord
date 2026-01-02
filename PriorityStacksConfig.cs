using System;
using System.Collections.Generic;

namespace AutoRotationPlugin;

[Serializable]
public enum PriorityRoleTab
{
    DPS = 0,
    Heal = 1,
    Tank = 2,
}

[Serializable]
public enum StackConditionType
{
    Always = 0,
    InCombat = 1,
    TargetHpBelowPct = 2,
    SelfHpBelowPct = 3,
    PartyMembersBelowPct = 4,
    TargetIsBoss = 5,
}

[Serializable]
public class StackCondition
{
    public bool Enabled = true;
    public StackConditionType Type = StackConditionType.Always;

    // Generic parameters (kept simple for config stability)
    public float ThresholdPct = 0f;     // e.g., HP% threshold
    public int Count = 0;              // e.g., party members count threshold
    public bool Flag = false;          // misc boolean
    public string Note = "";           // label shown in UI
}

[Serializable]
public class PriorityStack
{
    public string Name = "Stack";
    public bool Enabled = true;

    // ReAction-like options (config only; execution policy is handled elsewhere)
    public uint ModifierKeys = 0u;      // bitmask: 1=Shift, 2=Ctrl, 4=Alt
    public bool BlockOriginal = false;
    public bool CheckRange = true;
    public bool CheckCooldown = true;

    // Optional condition list (evaluation happens in manager)
    public List<StackCondition> Conditions = new();
}

[Serializable]
public class AbilityStackBinding
{
    public uint ActionId = 0;
    public string Label = "";
    public int StackIndex = 0; // index into Stacks list
}

[Serializable]
public class RolePriorityStacksConfig
{
    public int DefaultStackIndex = 0;
    public List<PriorityStack> Stacks = new();
    public List<AbilityStackBinding> AbilityBindings = new();
}

[Serializable]
public class JobPriorityStacksConfig
{
    public uint JobId = 0;
    public RolePriorityStacksConfig DPS = new();
    public RolePriorityStacksConfig Heal = new();
    public RolePriorityStacksConfig Tank = new();
}
