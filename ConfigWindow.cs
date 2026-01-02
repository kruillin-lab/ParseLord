using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using AutoRotationPlugin.Managers;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;

namespace AutoRotationPlugin;

public class ConfigWindow : Window, IDisposable
{
    private ParseLord P;
    private Configuration Configuration;

    // UI State
    private uint? _selectedJobId = null;
    private bool _sidebarCollapsed = false;

    // Priority Stack UI state
    private int _selectedStackIndex = 0;

    public ConfigWindow(ParseLord p, Configuration configuration) : base("Parse Lord###ParseLordConfig")
    {
        this.P = p;
        this.Configuration = configuration;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 600),
            MaximumSize = new Vector2(1600, 1200)
        };
        this.Flags = ImGuiWindowFlags.NoScrollbar;
    }

    public void Dispose() { }

    public override void Draw()
    {
        DrawMainLayout();
    }

    private void DrawMainLayout()
    {
        var avail = ImGui.GetContentRegionAvail();
        float sidebarWidth = _sidebarCollapsed ? 40f : 220f;
        
        if (ImGui.BeginTable("MainLayoutTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.NoSavedSettings | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, sidebarWidth);
            ImGui.TableSetupColumn("Body", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow(ImGuiTableRowFlags.None, avail.Y);
            ImGui.TableNextColumn();

            DrawSidebar();

            ImGui.TableNextColumn();

            DrawBody();

            ImGui.EndTable();
        }
    }

    private void DrawSidebar()
    {
        using var child = ImRaii.Child("SidebarChild", new Vector2(-1, -1), true);
        if (!child) return;

        if (!_sidebarCollapsed)
        {
            ImGuiEx.TextV(ImGuiColors.ParsedGold, "Jobs");
            ImGui.Separator();
            ImGui.Spacing();

            DrawJobCategory("Tanks", new uint[] { 19, 21, 32, 37 });
            DrawJobCategory("Healers", new uint[] { 24, 28, 33, 40 });
            DrawJobCategory("Melee DPS", new uint[] { 22, 20, 29, 34, 39, 41 });
            DrawJobCategory("Ranged DPS", new uint[] { 23, 31, 38 });
            DrawJobCategory("Magical DPS", new uint[] { 25, 27, 35, 42 });

            ImGui.SetCursorPosY(ImGui.GetWindowHeight() - 60);
            ImGui.Separator();
            if (ImGui.Selectable("Global Settings", _selectedJobId == null)) _selectedJobId = null;
        }
        else
        {
            if (ImGui.Button(">>")) _sidebarCollapsed = false;
        }
    }

    private void DrawJobCategory(string name, uint[] jobIds)
    {
        if (ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach (var id in jobIds)
            {
                string jobName = GetJobName(id);
                if (ImGui.Selectable($"{jobName}###Job{id}", _selectedJobId == id))
                {
                    _selectedJobId = id;
                }
            }
            ImGui.TreePop();
        }
    }

    private void DrawBody()
    {
        using var child = ImRaii.Child("BodyChild", new Vector2(-1, -1), false);
        if (!child) return;

        if (_selectedJobId == null)
        {
            DrawGlobalSettings();
        }
        else
        {
            DrawJobConfiguration(_selectedJobId.Value);
        }
    }

    private void DrawGlobalSettings()
    {
        ImGuiEx.TextV(ImGuiColors.ParsedGold, "Global Configuration");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("GlobalTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                ImGui.Checkbox("Enable Auto Rotation", ref Configuration.Enabled);
                ImGui.Checkbox("In Combat Only", ref Configuration.InCombatOnly);
                
                ImGui.SetNextItemWidth(200);
                if (ImGui.BeginCombo("Target Priority", Configuration.TargetPriority.ToString()))
                {
                    foreach (TargetPriority p in Enum.GetValues(typeof(TargetPriority)))
                    {
                        if (ImGui.Selectable(p.ToString(), Configuration.TargetPriority == p))
                        {
                            Configuration.TargetPriority = p;
                            Configuration.Save();
                        }
                    }
                    ImGui.EndCombo();
                }
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebug();
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawJobConfiguration(uint jobId)
    {
        string jobName = GetJobName(jobId);
        ImGuiEx.TextV(ImGuiColors.ParsedGold, $"{jobName} Configuration");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.BeginTabBar("JobConfigTabs"))
        {
            if (ImGui.BeginTabItem("Rotation Features"))
            {
                DrawRotationFeatures(jobId);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Priority Stacks"))
            {
                DrawPriorityStacksForJob(jobId);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawRotationFeatures(uint jobId)
    {
        bool enabled = GetJobEnabled(jobId);
        if (ImGui.Checkbox($"Enable {GetJobName(jobId)} Rotation", ref enabled))
        {
            SetJobEnabled(jobId, enabled);
            Configuration.Save();
        }

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Core Logic", ImGuiTreeNodeFlags.DefaultOpen))
        {
            int threshold = GetJobAoEThreshold(jobId);
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("AoE Target Threshold", ref threshold, 1, 10))
            {
                SetJobAoEThreshold(jobId, threshold);
                Configuration.Save();
            }
            
            ImGui.TextDisabled("This controls when the rotation switches from single-target to AoE mode.");
        }
    }

    private void DrawPriorityStacksForJob(uint jobId)
    {
        var stackMgr = P.PriorityStackManager;
        var jobCfg = stackMgr.GetOrCreateJob(jobId);

        if (ImGui.BeginTabBar("##RoleTabs"))
        {
            if (ImGui.BeginTabItem("DPS")) { DrawRoleStacks(jobCfg, PriorityRoleTab.DPS); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Heal")) { DrawRoleStacks(jobCfg, PriorityRoleTab.Heal); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Tank")) { DrawRoleStacks(jobCfg, PriorityRoleTab.Tank); ImGui.EndTabItem(); }
            ImGui.EndTabBar();
        }
    }

    private void DrawRoleStacks(JobPriorityStacksConfig jobCfg, PriorityRoleTab role)
    {
        var roleCfg = P.PriorityStackManager.GetRole(jobCfg, role);
        
        if (ImGui.BeginTable("StacksTable", 2, ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("StackList", ImGuiTableColumnFlags.WidthFixed, 200f);
            ImGui.TableSetupColumn("StackEditor", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            if (ImGui.Button("+ Add")) { roleCfg.Stacks.Add(new PriorityStack { Name = "New Stack", Enabled = true }); Configuration.Save(); }
            ImGui.SameLine();
            if (ImGui.Button("- Rem") && roleCfg.Stacks.Count > 1) { roleCfg.Stacks.RemoveAt(_selectedStackIndex); _selectedStackIndex = 0; Configuration.Save(); }

            for (int i = 0; i < roleCfg.Stacks.Count; i++)
            {
                if (ImGui.Selectable($"{i}: {roleCfg.Stacks[i].Name}", _selectedStackIndex == i)) _selectedStackIndex = i;
            }

            ImGui.TableNextColumn();
            if (roleCfg.Stacks.Count > 0)
            {
                _selectedStackIndex = Math.Clamp(_selectedStackIndex, 0, roleCfg.Stacks.Count - 1);
                var stack = roleCfg.Stacks[_selectedStackIndex];
                
                string name = stack.Name ?? "";
                if (ImGui.InputText("Stack Name", ref name, 64)) { stack.Name = name; Configuration.Save(); }
                ImGui.Checkbox("Enabled", ref stack.Enabled);
                
                ImGui.Separator();
                ImGui.Text("Conditions");
                if (ImGui.Button("+ Condition")) { stack.Conditions.Add(new StackCondition()); Configuration.Save(); }
            }
            ImGui.EndTable();
        }
    }

    private string GetJobName(uint jobId) => jobId switch
    {
        19 => "Paladin", 21 => "Warrior", 32 => "Dark Knight", 37 => "Gunbreaker",
        24 => "White Mage", 28 => "Scholar", 33 => "Astrologian", 40 => "Sage",
        22 => "Dragoon", 20 => "Monk", 29 => "Ninja", 34 => "Samurai", 39 => "Reaper", 41 => "Viper",
        23 => "Bard", 31 => "Machinist", 38 => "Dancer", 25 => "Black Mage", 27 => "Summoner", 35 => "Red Mage", 42 => "Pictomancer",
        _ => $"Job {jobId}"
    };

    private bool GetJobEnabled(uint jobId) => jobId switch
    {
        19 => Configuration.PLD_Enabled, 21 => Configuration.WAR_Enabled, 32 => Configuration.DRK_Enabled, 37 => Configuration.GNB_Enabled,
        24 => Configuration.WHM_Enabled, 28 => Configuration.SCH_Enabled, 33 => Configuration.AST_Enabled, 40 => Configuration.SGE_Enabled,
        22 => Configuration.DRG_Enabled, 20 => Configuration.MNK_Enabled, 29 => Configuration.NIN_Enabled, 34 => Configuration.SAM_Enabled, 
        39 => Configuration.RPR_Enabled, 41 => Configuration.VPR_Enabled,
        23 => Configuration.BRD_Enabled, 31 => Configuration.MCH_Enabled, 38 => Configuration.DNC_Enabled,
        25 => Configuration.BLM_Enabled, 27 => Configuration.SMN_Enabled, 35 => Configuration.RDM_Enabled, 42 => Configuration.PCT_Enabled,
        _ => false
    };

    private void SetJobEnabled(uint jobId, bool val)
    {
        switch (jobId)
        {
            case 19: Configuration.PLD_Enabled = val; break;
            case 21: Configuration.WAR_Enabled = val; break;
            case 32: Configuration.DRK_Enabled = val; break;
            case 37: Configuration.GNB_Enabled = val; break;
            case 24: Configuration.WHM_Enabled = val; break;
            case 28: Configuration.SCH_Enabled = val; break;
            case 33: Configuration.AST_Enabled = val; break;
            case 40: Configuration.SGE_Enabled = val; break;
            case 22: Configuration.DRG_Enabled = val; break;
            case 20: Configuration.MNK_Enabled = val; break;
            case 29: Configuration.NIN_Enabled = val; break;
            case 34: Configuration.SAM_Enabled = val; break;
            case 39: Configuration.RPR_Enabled = val; break;
            case 41: Configuration.VPR_Enabled = val; break;
            case 23: Configuration.BRD_Enabled = val; break;
            case 31: Configuration.MCH_Enabled = val; break;
            case 38: Configuration.DNC_Enabled = val; break;
            case 25: Configuration.BLM_Enabled = val; break;
            case 27: Configuration.SMN_Enabled = val; break;
            case 35: Configuration.RDM_Enabled = val; break;
            case 42: Configuration.PCT_Enabled = val; break;
        }
    }

    private int GetJobAoEThreshold(uint jobId) => jobId switch
    {
        19 => Configuration.PLD_AoE_Threshold, 21 => Configuration.WAR_AoE_Threshold, 32 => Configuration.DRK_AoE_Threshold, 37 => Configuration.GNB_AoE_Threshold,
        24 => Configuration.WHM_DPS_AoE_Threshold, 28 => Configuration.SCH_AoE_Threshold, 33 => Configuration.AST_AoE_Threshold, 40 => Configuration.SGE_AoE_Threshold,
        22 => Configuration.DRG_AoE_Threshold, 20 => Configuration.MNK_AoE_Threshold, 29 => Configuration.NIN_AoE_Threshold, 34 => Configuration.SAM_AoE_Threshold,
        39 => Configuration.RPR_AoE_Threshold, 41 => Configuration.VPR_AoE_Threshold,
        23 => Configuration.BRD_AoE_Threshold, 31 => Configuration.MCH_AoE_Threshold, 38 => Configuration.DNC_AoE_Threshold,
        25 => Configuration.BLM_AoE_Threshold, 27 => Configuration.SMN_AoE_Threshold, 35 => Configuration.RDM_AoE_Threshold, 42 => Configuration.PCT_AoE_Threshold,
        _ => 3
    };

    private void SetJobAoEThreshold(uint jobId, int val)
    {
        switch (jobId)
        {
            case 19: Configuration.PLD_AoE_Threshold = val; break;
            case 21: Configuration.WAR_AoE_Threshold = val; break;
            case 32: Configuration.DRK_AoE_Threshold = val; break;
            case 37: Configuration.GNB_AoE_Threshold = val; break;
            case 24: Configuration.WHM_DPS_AoE_Threshold = val; break;
            case 28: Configuration.SCH_AoE_Threshold = val; break;
            case 33: Configuration.AST_AoE_Threshold = val; break;
            case 40: Configuration.SGE_AoE_Threshold = val; break;
            case 22: Configuration.DRG_AoE_Threshold = val; break;
            case 20: Configuration.MNK_AoE_Threshold = val; break;
            case 29: Configuration.NIN_AoE_Threshold = val; break;
            case 34: Configuration.SAM_AoE_Threshold = val; break;
            case 39: Configuration.RPR_AoE_Threshold = val; break;
            case 41: Configuration.VPR_AoE_Threshold = val; break;
            case 23: Configuration.BRD_AoE_Threshold = val; break;
            case 31: Configuration.MCH_AoE_Threshold = val; break;
            case 38: Configuration.DNC_AoE_Threshold = val; break;
            case 25: Configuration.BLM_AoE_Threshold = val; break;
            case 27: Configuration.SMN_AoE_Threshold = val; break;
            case 35: Configuration.RDM_AoE_Threshold = val; break;
            case 42: Configuration.PCT_AoE_Threshold = val; break;
        }
    }

    private void DrawDebug()
    {
        ImGuiEx.TextV(ImGuiColors.ParsedGold, "Rotation Status:");
        ImGui.Separator();

        if (Svc.ClientState.LocalPlayer == null)
        {
            ImGui.TextDisabled("Player not found - Log into the game.");
            return;
        }

        var player = Svc.ClientState.LocalPlayer;
        var jobId = player.ClassJob.RowId;
        var inCombat = player.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat);
        var target = GameState.TargetAsBattleChara;

        ImGui.Text($"Job: {GetJobName(jobId)}");
        ImGui.Text($"In Combat: {inCombat}");
        ImGui.Text($"Target: {(target != null ? target.Name : "None")}");
        
        ImGui.Separator();
        var nextAction = P.RotationManager.GetNextAction();
        ImGui.Text($"Next Action: {(nextAction != null ? nextAction.Name : "None")}");
        ImGui.Text($"Reason: {P.RotationManager.LastFailureReason}");
    }
}
