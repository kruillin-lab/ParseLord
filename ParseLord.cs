using AutoRotationPlugin.Managers;
using PunishLib;
using ECommons;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using Dalamud.Plugin.Services;
using System;

namespace AutoRotationPlugin;

public sealed partial class ParseLord : IDalamudPlugin
{
    private readonly Action uiDraw;
    internal static ParseLord P = null!;
    public string Name => "Parse Lord";

    public Configuration Configuration { get; init; }
    public WindowSystem WindowSystem { get; init; }
    public ConfigWindow ConfigWindow { get; init; }
    public RotationManager RotationManager { get; private set; }
    public PriorityStackManager PriorityStackManager { get; private set; }
    public ActionManager ActionManager { get; private set; }

    public ParseLord(IDalamudPluginInterface pluginInterface)
    {
        P = this;

        ECommonsMain.Init(pluginInterface, this, ECommons.Module.All);
        PunishLibMain.Init(pluginInterface, "ParseLord");

        Configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(pluginInterface);

        ConfigWindow = new ConfigWindow(this, Configuration);
        WindowSystem = new WindowSystem("ParseLord");
        WindowSystem.AddWindow(ConfigWindow);

        uiDraw = () => WindowSystem.Draw();
        ActionManager = ActionManager.Instance;
        RotationManager = new RotationManager(Configuration);
        PriorityStackManager = new PriorityStackManager(Configuration);

        pluginInterface.UiBuilder.Draw += uiDraw;
        pluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
        Svc.Framework.Update += OnFrameworkUpdate;

        RegisterCommands();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Configuration.Enabled || Svc.ClientState.IsPvP || Svc.ClientState.LocalPlayer is null) return;

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
            Svc.Log.Error(ex, "Error in Framework Update");
        }
    }

    public void Dispose()
    {
        UnregisterCommands();

        Svc.PluginInterface.UiBuilder.Draw -= uiDraw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
        Svc.Framework.Update -= OnFrameworkUpdate;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        ActionManager.Dispose();

        ECommonsMain.Dispose();
        P = null!;
    }

    internal void OnOpenConfigUi() => ConfigWindow.IsOpen = true;
}
