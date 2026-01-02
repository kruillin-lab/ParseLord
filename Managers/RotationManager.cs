using AutoRotationPlugin.Rotations;
using AutoRotationPlugin.Rotations.Jobs;
using ECommons.DalamudServices;
using Dalamud.Game.ClientState.Objects.Enums;
using System.Collections.Generic;

namespace AutoRotationPlugin.Managers;

public class RotationManager
{
    private readonly Configuration Config;

    public TargetingManager Targeting { get; }

    private readonly Dictionary<uint, IRotation> Rotations;

    // Diagnostic: Last failure reason for debugging
    public string LastFailureReason { get; private set; } = "Not yet evaluated";
    public uint LastChosenActionId { get; private set; } = 0;
    public string LastChosenActionName { get; private set; } = "None";

    // Known fallback GCDs per job (for proof-of-life testing)
    private static readonly Dictionary<uint, (uint actionId, string name)> FallbackGCDs = new()
    {
        { 22, (75, "True Thrust") },      // DRG
        { 19, (9, "Fast Blade") },        // PLD
        { 24, (119, "Stone") },           // WHM
    };

    public RotationManager(Configuration config)
    {
        Config = config;
        Targeting = new TargetingManager(config);

        // ClassJob.RowId values (jobs, not base classes)
        Rotations = new Dictionary<uint, IRotation>
        {
            { 22, new DragoonRotation() },
            { 19, new PaladinRotation() },
            { 24, new WhiteMageRotation() },
        };
    }

    public ActionInfo? GetNextAction()
    {
        // Gate 1: Global Enable
        if (!Config.Enabled)
        {
            LastFailureReason = "Config.Enabled is FALSE";
            LastChosenActionId = 0;
            LastChosenActionName = "None";
            return null;
        }

        // Keep targeting state fresh before calculating rotation
        Targeting.UpdateTargeting();

        // Gate 2: Player exists
        var player = Svc.ClientState.LocalPlayer;
        if (player == null)
        {
            LastFailureReason = "LocalPlayer is NULL";
            LastChosenActionId = 0;
            LastChosenActionName = "None";
            return null;
        }

        var jobId = player.ClassJob.RowId;

        // Gate 3: Rotation exists for job
        if (!Rotations.TryGetValue(jobId, out var rotation))
        {
            LastFailureReason = $"No rotation registered for JobId={jobId}";
            LastChosenActionId = 0;
            LastChosenActionName = "None";
            return null;
        }

        // Try the rotation's brain
        var action = rotation.GetNextAction(Config);

        if (action != null)
        {
            LastFailureReason = "OK";
            LastChosenActionId = action.ActionId;
            LastChosenActionName = action.Name;
            return action;
        }

        // Rotation returned null - diagnose why
        // Check if we SHOULD have gotten an action (for fallback logic)
        bool inCombat = player.StatusFlags.HasFlag(StatusFlags.InCombat);
        var target = GameState.TargetAsBattleChara;
        bool hasTarget = target != null;

        // Build detailed failure reason
        var jobEnabledField = jobId switch
        {
            22 => Config.DRG_Enabled,
            19 => Config.PLD_Enabled,
            24 => Config.WHM_Enabled,
            _ => false
        };

        if (!jobEnabledField)
        {
            LastFailureReason = $"Job-specific enable flag is FALSE (JobId={jobId})";
        }
        else if (!inCombat)
        {
            LastFailureReason = "Player is NOT in combat";
        }
        else if (!hasTarget)
        {
            LastFailureReason = "No valid target (TargetAsBattleChara is NULL)";
        }
        else
        {
            // All gates passed but rotation still returned null - logic issue
            LastFailureReason = "Rotation logic returned NULL despite valid conditions (check combo/condition logic)";

            // TEMPORARY FALLBACK: Return a known-good GCD for proof-of-life
            // This confirms the execution path works; rotation logic needs fixing
            if (FallbackGCDs.TryGetValue(jobId, out var fallback))
            {
                LastFailureReason = $"FALLBACK ACTIVE: Rotation returned null, using {fallback.name} (ID:{fallback.actionId})";
                LastChosenActionId = fallback.actionId;
                LastChosenActionName = $"[FALLBACK] {fallback.name}";
                return new ActionInfo(fallback.actionId, $"[FALLBACK] {fallback.name}");
            }
        }

        LastChosenActionId = 0;
        LastChosenActionName = "None";
        return null;
    }
}
