using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;

namespace AutoRotationPlugin.Managers;

/// <summary>
/// Configuration-only priority stack system inspired by ReAction's stack options.
/// This does NOT execute actions by itself; it only evaluates whether a stack should apply
/// and exposes per-ability stack selection to the rest of the engine.
/// </summary>
public sealed class PriorityStackManager
{
    private readonly Configuration config;

    public PriorityStackManager(Configuration config)
    {
        this.config = config;
    }

    public JobPriorityStacksConfig GetOrCreateJob(uint jobId)
    {
        if (!config.PriorityStacksByJob.TryGetValue(jobId, out var jobCfg) || jobCfg == null)
        {
            jobCfg = new JobPriorityStacksConfig { JobId = jobId };
            SeedDefaults(jobCfg);
            config.PriorityStacksByJob[jobId] = jobCfg;
            config.Save();
        }

        // Ensure minimal sane structure
        EnsureRoleDefaults(jobCfg.DPS);
        EnsureRoleDefaults(jobCfg.Heal);
        EnsureRoleDefaults(jobCfg.Tank);

        return jobCfg;
    }

    public RolePriorityStacksConfig GetRole(JobPriorityStacksConfig jobCfg, PriorityRoleTab role)
        => role switch
        {
            PriorityRoleTab.DPS => jobCfg.DPS,
            PriorityRoleTab.Heal => jobCfg.Heal,
            PriorityRoleTab.Tank => jobCfg.Tank,
            _ => jobCfg.DPS
        };

    public PriorityStack GetStackForAbility(uint jobId, PriorityRoleTab role, uint actionId)
    {
        var job = GetOrCreateJob(jobId);
        var roleCfg = GetRole(job, role);

        var bind = roleCfg.AbilityBindings.FirstOrDefault(b => b.ActionId == actionId);
        var idx = bind != null ? bind.StackIndex : roleCfg.DefaultStackIndex;

        if (roleCfg.Stacks.Count == 0)
            EnsureRoleDefaults(roleCfg);

        idx = Math.Clamp(idx, 0, roleCfg.Stacks.Count - 1);
        return roleCfg.Stacks[idx];
    }

    public bool EvaluateStack(PriorityStack stack, int partyMembersBelowPct, float targetHpPct, float selfHpPct, bool inCombat)
    {
        // Basic ReAction-style gates
        if (!stack.Enabled) return false;

        // ModifierKeys are UI-only for now (we don't read keyboard state here to keep dependencies clean).
        // Future: if you add keyboard state support, gate here.

        foreach (var c in stack.Conditions)
        {
            if (!c.Enabled) continue;

            switch (c.Type)
            {
                case StackConditionType.Always:
                    break;

                case StackConditionType.InCombat:
                    if (!inCombat) return false;
                    break;

                case StackConditionType.TargetHpBelowPct:
                    if (targetHpPct > c.ThresholdPct) return false;
                    break;

                case StackConditionType.SelfHpBelowPct:
                    if (selfHpPct > c.ThresholdPct) return false;
                    break;

                case StackConditionType.PartyMembersBelowPct:
                    if (partyMembersBelowPct < c.Count) return false;
                    break;

                case StackConditionType.TargetIsBoss:
                    // Not evaluated in this configuration-only manager.
                    // If you later add boss detection to your game-state layer, wire it in here.
                    break;
            }
        }

        return true;
    }

    private static void SeedDefaults(JobPriorityStacksConfig jobCfg)
    {
        // Create a small set of useful stacks so the UI has something immediately.
        SeedRole(jobCfg.DPS, "DPS");
        SeedRole(jobCfg.Heal, "Heal");
        SeedRole(jobCfg.Tank, "Tank");
    }

    private static void SeedRole(RolePriorityStacksConfig roleCfg, string roleName)
    {
        roleCfg.DefaultStackIndex = 0;

        roleCfg.Stacks.Add(new PriorityStack
        {
            Name = $"{roleName} Default",
            Enabled = true,
            CheckRange = true,
            CheckCooldown = true,
            BlockOriginal = false,
            Conditions = new List<StackCondition>
            {
                new StackCondition { Type = StackConditionType.Always, Note = "Always" }
            }
        });

        roleCfg.Stacks.Add(new PriorityStack
        {
            Name = $"{roleName} Emergency",
            Enabled = true,
            CheckRange = true,
            CheckCooldown = true,
            BlockOriginal = false,
            Conditions = new List<StackCondition>
            {
                new StackCondition { Type = StackConditionType.InCombat, Note = "Only in combat" }
            }
        });
    }

    private static void EnsureRoleDefaults(RolePriorityStacksConfig roleCfg)
    {
        if (roleCfg.Stacks == null) roleCfg.Stacks = new List<PriorityStack>();
        if (roleCfg.AbilityBindings == null) roleCfg.AbilityBindings = new List<AbilityStackBinding>();

        if (roleCfg.Stacks.Count == 0)
        {
            roleCfg.Stacks.Add(new PriorityStack { Name = "Default", Enabled = true, CheckRange = true, CheckCooldown = true });
            roleCfg.DefaultStackIndex = 0;
        }
        roleCfg.DefaultStackIndex = Math.Clamp(roleCfg.DefaultStackIndex, 0, roleCfg.Stacks.Count - 1);
    }
}
