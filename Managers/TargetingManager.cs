using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using AutoRotationPlugin;

namespace AutoRotationPlugin.Managers;

public class TargetingManager
{
    private Configuration Config;

    // FIX: Added constructor that takes 1 argument
    public TargetingManager(Configuration config)
    {
        this.Config = config;
    }

    // FIX: Added the missing UpdateTargeting method
    public void UpdateTargeting()
    {
        if (!Config.Enabled) return;

        // Logic to automatically switch targets if current is invalid
        var currentTarget = Svc.Targets.Target;
        if (currentTarget == null || (currentTarget is ICharacter c && c.IsDead))
        {
            var best = GetBestTarget();
            if (best != null)
            {
                Svc.Targets.Target = best;
            }
        }
    }

    public IGameObject? GetBestTarget()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return null;

        var objects = Svc.Objects
            .Where(x => x is ICharacter c && c.IsTargetable && !c.IsDead)
            .ToList();

        if (Config.TargetPriority == TargetPriority.Closest)
        {
            return objects
                .OrderBy(x => Vector3.Distance(player.Position, x.Position))
                .FirstOrDefault();
        }

        if (Config.TargetPriority == TargetPriority.LowestHP)
        {
            return objects
                .Where(x => x is IBattleChara)
                .Cast<IBattleChara>()
                .OrderBy(x => x.CurrentHp)
                .FirstOrDefault();
        }

        return Svc.Targets.Target;
    }
}