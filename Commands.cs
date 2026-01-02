using Dalamud.Game.Command;
using ECommons.DalamudServices;

namespace AutoRotationPlugin;

public partial class ParseLord
{
    private const string CommandName = "/parselord";
    private const string CommandAlias = "/pl";

    private void RegisterCommands()
    {
        // Remove existing handlers first to prevent "Command already registered" errors on reload
        Svc.Commands.RemoveHandler(CommandName);
        Svc.Commands.RemoveHandler(CommandAlias);

        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens ParseLord config. /pl debug for file dump. /pl diag for chat diagnostic. /pl toggle to enable/disable. /pl test <actionId> to test execution."
        });

        Svc.Commands.AddHandler(CommandAlias, new CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /parselord"
        });
    }

    private void UnregisterCommands()
    {
        Svc.Commands.RemoveHandler(CommandName);
        Svc.Commands.RemoveHandler(CommandAlias);
    }

    private void OnCommand(string command, string args)
    {
        var parts = args.ToLower().Trim().Split(' ', 2);
        var subCommand = parts.Length > 0 ? parts[0] : "";
        var subArgs = parts.Length > 1 ? parts[1] : "";

        switch (subCommand)
        {
            case "debug":
            case "dbg":
                // Call the file-based debugger
                DebugDumper.Generate();
                break;

            case "diag":
                // Quick in-chat diagnostic
                PrintDiagnostic();
                break;

            case "toggle":
                Configuration.Enabled = !Configuration.Enabled;
                Configuration.Save();
                var status = Configuration.Enabled ? "Enabled" : "Disabled";
                Svc.Chat.Print($"[Parse Lord] Rotation {status}");
                break;

            case "test":
                // Test action execution: /pl test <actionId>
                TestAction(subArgs);
                break;

            default:
                ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
                break;
        }
    }

    private void PrintDiagnostic()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null)
        {
            Svc.Chat.Print("[Parse Lord] Player is NULL");
            return;
        }

        var jobId = player.ClassJob.RowId;
        var inCombat = player.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat);
        var target = GameState.TargetAsBattleChara;

        Svc.Chat.Print($"[Parse Lord] === DIAGNOSTIC ===");
        Svc.Chat.Print($"[Parse Lord] Enabled={Configuration.Enabled} JobId={jobId} InCombat={inCombat} HasTarget={target != null}");
        
        // Job-specific enable
        var jobEnabled = jobId switch
        {
            22 => Configuration.DRG_Enabled,
            19 => Configuration.PLD_Enabled,
            24 => Configuration.WHM_Enabled,
            _ => false
        };
        Svc.Chat.Print($"[Parse Lord] JobEnabled={jobEnabled}");

        // Try to get next action
        var action = RotationManager.GetNextAction();
        Svc.Chat.Print($"[Parse Lord] LastChoice: Action={RotationManager.LastChosenActionId} Name={RotationManager.LastChosenActionName}");
        Svc.Chat.Print($"[Parse Lord] FailureReason: {RotationManager.LastFailureReason}");

        // ActionManager state
        Svc.Chat.Print($"[Parse Lord] AnimLock={ActionManager.AnimationLock:F3} GCDRem={ActionManager.GetGCDRemaining():F3} CanWeave={ActionManager.CanWeave()}");
        Svc.Chat.Print($"[Parse Lord] ComboAction={ActionManager.ComboAction} Adjusted={ActionManager.GetAdjustedActionId(ActionManager.ComboAction)}");
    }

    private void TestAction(string actionIdStr)
    {
        if (!uint.TryParse(actionIdStr.Trim(), out var actionId))
        {
            Svc.Chat.Print($"[Parse Lord] Usage: /pl test <actionId>");
            Svc.Chat.Print($"[Parse Lord] Example: /pl test 75  (True Thrust)");
            return;
        }

        var canUse = ActionManager.CanUseAction(actionId);
        Svc.Chat.Print($"[Parse Lord] TEST actionId={actionId} canUse={canUse}");

        if (canUse)
        {
            var executed = ActionManager.ExecuteAction(actionId);
            Svc.Chat.Print($"[Parse Lord] TEST actionId={actionId} executed={executed}");
        }
        else
        {
            // Try to get more info on why it can't be used
            var adjusted = ActionManager.GetAdjustedActionId(actionId);
            Svc.Chat.Print($"[Parse Lord] TEST adjusted={adjusted} (may differ from input)");
            
            var canUseAdjusted = ActionManager.CanUseAction(adjusted);
            Svc.Chat.Print($"[Parse Lord] TEST canUseAdjusted={canUseAdjusted}");
        }
    }
}
