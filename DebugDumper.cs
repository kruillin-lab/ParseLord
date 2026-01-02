using AutoRotationPlugin.Managers;
using Dalamud.Game.ClientState.Objects.Enums;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace AutoRotationPlugin;

public static class DebugDumper
{
    public static void Generate()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("      PARSE LORD DEBUG LOG");
        sb.AppendLine("========================================");
        sb.AppendLine($"Time: {DateTime.Now}");
        sb.AppendLine($"Version: {P.GetType().Assembly.GetName().Version}");
        sb.AppendLine();

        // 1. Player Info
        sb.AppendLine("--- PLAYER STATE ---");
        if (Svc.ClientState.LocalPlayer != null)
        {
            var p = Svc.ClientState.LocalPlayer;
            sb.AppendLine($"Name: {p.Name} (World: {p.HomeWorld.Value.Name})");
            sb.AppendLine($"Job ID: {p.ClassJob.RowId}");
            sb.AppendLine($"HP: {p.CurrentHp}/{p.MaxHp} ({GameState.PlayerHPPercent:P1})");
            sb.AppendLine($"MP: {p.CurrentMp}/{p.MaxMp}");
            sb.AppendLine($"In Combat: {GameState.InCombat}");
            sb.AppendLine($"Is Casting: {GameState.IsCasting}");
            sb.AppendLine($"Is Dead: {GameState.IsDead}");
        }
        else
        {
            sb.AppendLine("LocalPlayer is NULL");
        }
        sb.AppendLine();

        // 2. Target Info
        sb.AppendLine("--- TARGET STATE ---");
        if (Svc.Targets.Target != null)
        {
            var t = Svc.Targets.Target;
            sb.AppendLine($"Name: {t.Name}");
            sb.AppendLine($"Distance: {GameState.TargetDistance:F2}y");

            var battleChara = t as Dalamud.Game.ClientState.Objects.Types.IBattleChara;
            if (battleChara != null)
            {
                sb.AppendLine($"Is BattleChara: True");
                sb.AppendLine($"Is Hostile: {GameState.IsHostile(battleChara)}");
                sb.AppendLine($"Target HP: {battleChara.CurrentHp}/{battleChara.MaxHp}");
            }
            else
            {
                sb.AppendLine($"Is BattleChara: False (Cannot attack)");
            }

            if (Svc.ClientState.LocalPlayer != null)
            {
                sb.AppendLine($"Facing Target: {Safety.IsFacingTarget(Svc.ClientState.LocalPlayer, t)}");
            }
        }
        else
        {
            sb.AppendLine("No target selected (Target is NULL)");
        }
        sb.AppendLine();

        // 3. Action Manager State
        sb.AppendLine("--- ACTION MANAGER STATE ---");
        sb.AppendLine($"Animation Lock: {ActionManager.Instance.AnimationLock:F3}s");
        sb.AppendLine($"Is Animation Locked: {ActionManager.Instance.IsAnimationLocked}");
        sb.AppendLine($"GCD Remaining: {ActionManager.Instance.GetGCDRemaining():F3}s");
        sb.AppendLine($"Can Weave: {ActionManager.Instance.CanWeave()}");
        sb.AppendLine($"Combo Action: {ActionManager.Instance.ComboAction}");
        sb.AppendLine($"Combo Action (Adjusted): {ActionManager.Instance.GetAdjustedActionId(ActionManager.Instance.ComboAction)}");
        sb.AppendLine();

        // 4. Rotation Manager State (NEW)
        sb.AppendLine("--- ROTATION MANAGER STATE ---");
        // Trigger a rotation evaluation to populate diagnostics
        var nextAction = P.RotationManager.GetNextAction();
        sb.AppendLine($"Last Failure Reason: {P.RotationManager.LastFailureReason}");
        sb.AppendLine($"Last Chosen Action ID: {P.RotationManager.LastChosenActionId}");
        sb.AppendLine($"Last Chosen Action Name: {P.RotationManager.LastChosenActionName}");
        if (nextAction != null)
        {
            sb.AppendLine($"Next Action: {nextAction.Name} (ID: {nextAction.ActionId}, TargetsSelf: {nextAction.TargetsSelf})");
        }
        else
        {
            sb.AppendLine($"Next Action: NULL");
        }
        sb.AppendLine();

        // 5. Gate Check Summary
        sb.AppendLine("--- GATE CHECK SUMMARY ---");
        sb.AppendLine($"Config.Enabled: {P.Configuration.Enabled}");
        if (Svc.ClientState.LocalPlayer != null)
        {
            var jobId = Svc.ClientState.LocalPlayer.ClassJob.RowId;
            var jobEnabled = jobId switch
            {
                22 => P.Configuration.DRG_Enabled,
                19 => P.Configuration.PLD_Enabled,
                24 => P.Configuration.WHM_Enabled,
                _ => false
            };
            sb.AppendLine($"Job {jobId} Enabled: {jobEnabled}");
            sb.AppendLine($"Player InCombat: {Svc.ClientState.LocalPlayer.StatusFlags.HasFlag(StatusFlags.InCombat)}");
        }
        sb.AppendLine($"Has Valid Target (BattleChara): {GameState.TargetAsBattleChara != null}");
        sb.AppendLine();

        // 6. Configuration Dump
        sb.AppendLine("--- CONFIGURATION ---");
        try
        {
            var json = JsonConvert.SerializeObject(P.Configuration, Newtonsoft.Json.Formatting.Indented);
            sb.AppendLine(json);
        }
        catch (Exception e)
        {
            sb.AppendLine($"Config dump failed: {e.Message}");
        }
        sb.AppendLine();

        sb.AppendLine("========================================");
        sb.AppendLine("           END OF DEBUG LOG");
        sb.AppendLine("========================================");

        // 7. Save to Desktop
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var path = Path.Combine(desktop, "ParseLordDebug.txt");
        File.WriteAllText(path, sb.ToString());

        Svc.Chat.Print($"[Parse Lord] Debug file saved to: {path}");
        DuoLog.Information($"Debug file saved to: {path}");
    }
}
