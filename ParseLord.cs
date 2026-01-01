using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using AutoRotationPlugin.Managers;
using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace AutoRotationPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Parse Lord";
        private const string CommandName = "/parselord";
        private const string CommandAlias = "/pl";

        // Dalamud Services
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
        [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] public static IJobGauges JobGauges { get; private set; } = null!;
        [PluginService] public static IPartyList PartyList { get; private set; } = null!;
        [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;

        [PluginService] public static IPluginLog Log { get; private set; } = null!;

        // Plugin Components
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem { get; init; } = new("ParseLord");
        public ConfigWindow ConfigWindow { get; init; }

        // Managers
        public RotationManager RotationManager { get; private set; }
        public ActionManager ActionManager { get; private set; }

        public Plugin()
        {
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this, Configuration);
            WindowSystem.AddWindow(ConfigWindow);

            ActionManager = ActionManager.Instance;
            RotationManager = new RotationManager(Configuration);

            // Register Main Command
            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the configuration window. Use '/parselord toggle' to enable/disable."
            });

            // Register Alias Command
            CommandManager.AddHandler(CommandAlias, new CommandInfo(OnCommand)
            {
                HelpMessage = "Alias for /parselord"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            Framework.Update += OnFrameworkUpdate;

            Log.Debug("[ParseLord] Plugin initialized successfully.");
        }

        public void Dispose()
        {
            Framework.Update -= OnFrameworkUpdate;
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;

            CommandManager.RemoveHandler(CommandName);
            CommandManager.RemoveHandler(CommandAlias);

            WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();
            ActionManager.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            if (string.IsNullOrEmpty(args))
            {
                ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
                return;
            }

            var subCommand = args.ToLower().Trim();

            if (subCommand == "toggle")
            {
                Configuration.Enabled = !Configuration.Enabled;
                Configuration.Save();
                ChatGui.Print($"[Parse Lord] {(Configuration.Enabled ? "Enabled" : "Disabled")}");
            }
            else if (subCommand == "diag")
            {
                // FIX: Use ObjectTable[0] instead of ClientState.LocalPlayer
                var player = ObjectTable.Length > 0 ? ObjectTable[0] as IPlayerCharacter : null;

                if (player == null)
                {
                    ChatGui.Print("[Parse Lord] Not logged in.");
                    return;
                }

                ChatGui.Print($"[Parse Lord Diag] Job: {player.ClassJob.RowId}, InCombat: {GameState.InCombat}, Enabled: {Configuration.Enabled}");
            }
            else
            {
                ConfigWindow.IsOpen = !ConfigWindow.IsOpen;
            }
        }

        private void DrawUI() => WindowSystem.Draw();
        public void DrawConfigUI() => ConfigWindow.IsOpen = true;

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (!Configuration.Enabled) return;
            if (ClientState.IsPvP) return;

            // FIX: Use ObjectTable[0] to get the local player safely
            if (ObjectTable.Length == 0) return;
            var player = ObjectTable[0];

            if (player == null) return;

            // 2. Get Target
            var currentTarget = TargetManager.Target;

            // 3. SAFETY CHECK: Gaze Mechanic Protection
            // If we have a hard target, but we are looking away from it, DO NOT ATTACK.
            if (currentTarget != null)
            {
                if (!Safety.IsFacingTarget(player, currentTarget))
                {
                    // Logic: You are looking away, likely for a mechanic. 
                    // We pause execution so the game doesn't snap you back.
                    return;
                }
            }

            try
            {
                var action = RotationManager.GetNextAction();
                if (action != null)
                {
                    ActionManager.ExecuteAction(action.ActionId, action.TargetsSelf, action.TargetOverrideId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[ParseLord] Critical error in Framework Update");
                Configuration.Enabled = false;
                ChatGui.PrintError("[Parse Lord] Critical error. Plugin disabled.");
            }
        }
    }
}