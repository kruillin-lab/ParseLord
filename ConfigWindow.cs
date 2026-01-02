using System;
using System.Numerics;
using AutoRotationPlugin.Managers;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using Dalamud.Interface.Colors;

namespace AutoRotationPlugin;

public class ConfigWindow : Window, IDisposable
{
    private ParseLord P;
    private Configuration Configuration;

    // Priority Stack UI state
    private uint _selectedJobId = 22; // default DRG
    private PriorityRoleTab _selectedRole = PriorityRoleTab.DPS;
    private int _selectedStackIndex = 0;

    public ConfigWindow(ParseLord p, Configuration configuration) : base("Parse Lord Configuration")
    {
        this.P = p;
        this.Configuration = configuration;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(350, 450),
            MaximumSize = new Vector2(1000, 1000)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("MainTabBar"))
        {
            if (ImGui.BeginTabItem("Settings"))
            {
                DrawSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Debug"))
            {
                DrawDebug();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Priority Stacks"))
            {
                DrawPriorityStacks();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    
    private void DrawPriorityStacks()
    {
        // Supported jobs for now (matches current project scope)
        // DRG=22, PLD=19, WHM=24 (RowId)
        ImGui.Text("Job");
        ImGui.SameLine();

        var jobLabel = _selectedJobId switch
        {
            22 => "Dragoon (DRG)",
            19 => "Paladin (PLD)",
            24 => "White Mage (WHM)",
            _ => $"Job {_selectedJobId}"
        };

        if (ImGui.BeginCombo("##PL_JobSelect", jobLabel))
        {
            if (ImGui.Selectable("Dragoon (DRG)", _selectedJobId == 22)) _selectedJobId = 22;
            if (ImGui.Selectable("Paladin (PLD)", _selectedJobId == 19)) _selectedJobId = 19;
            if (ImGui.Selectable("White Mage (WHM)", _selectedJobId == 24)) _selectedJobId = 24;
            ImGui.EndCombo();
        }

        var stackMgr = P.PriorityStackManager;
        var jobCfg = stackMgr.GetOrCreateJob(_selectedJobId);

        if (ImGui.BeginTabBar("##PL_RoleTabs"))
        {
            if (ImGui.BeginTabItem("DPS"))
            {
                _selectedRole = PriorityRoleTab.DPS;
                DrawRoleStacks(jobCfg, PriorityRoleTab.DPS);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Heal"))
            {
                _selectedRole = PriorityRoleTab.Heal;
                DrawRoleStacks(jobCfg, PriorityRoleTab.Heal);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Tank (Mitigation)"))
            {
                _selectedRole = PriorityRoleTab.Tank;
                DrawRoleStacks(jobCfg, PriorityRoleTab.Tank);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawRoleStacks(JobPriorityStacksConfig jobCfg, PriorityRoleTab role)
    {
        var roleCfg = P.PriorityStackManager.GetRole(jobCfg, role);

        // Layout: Left = stacks list, Right = stack editor + ability bindings
        var avail = ImGui.GetContentRegionAvail();
        var leftW = MathF.Max(220, avail.X * 0.32f);
        var rightW = MathF.Max(300, avail.X - leftW - 12);

        ImGui.BeginChild("##PL_StacksLeft", new Vector2(leftW, 0), true);

        ImGui.Text("Stacks");
        ImGui.Separator();

        // Add / Remove buttons
        if (ImGui.Button("+ Add Stack"))
        {
            roleCfg.Stacks.Add(new PriorityStack { Name = $"Stack {roleCfg.Stacks.Count + 1}", Enabled = true });
            _selectedStackIndex = roleCfg.Stacks.Count - 1;
            Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("- Remove Stack"))
        {
            if (roleCfg.Stacks.Count > 1)
            {
                var removeAt = Math.Clamp(_selectedStackIndex, 0, roleCfg.Stacks.Count - 1);
                roleCfg.Stacks.RemoveAt(removeAt);
                _selectedStackIndex = Math.Clamp(_selectedStackIndex, 0, roleCfg.Stacks.Count - 1);
                roleCfg.DefaultStackIndex = Math.Clamp(roleCfg.DefaultStackIndex, 0, roleCfg.Stacks.Count - 1);

                // Fix up bindings that pointed to removed index
                foreach (var b in roleCfg.AbilityBindings)
                    b.StackIndex = Math.Clamp(b.StackIndex, 0, roleCfg.Stacks.Count - 1);

                Configuration.Save();
            }
        }

        ImGui.Spacing();
        ImGui.Text("Default Stack");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##PL_DefaultStack", roleCfg.Stacks[Math.Clamp(roleCfg.DefaultStackIndex, 0, roleCfg.Stacks.Count - 1)].Name))
        {
            for (var i = 0; i < roleCfg.Stacks.Count; i++)
            {
                if (ImGui.Selectable($"{i}: {roleCfg.Stacks[i].Name}", roleCfg.DefaultStackIndex == i))
                {
                    roleCfg.DefaultStackIndex = i;
                    Configuration.Save();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("Select Stack");
        ImGui.Spacing();

        for (var i = 0; i < roleCfg.Stacks.Count; i++)
        {
            var s = roleCfg.Stacks[i];
            var label = $"{i}: {(string.IsNullOrEmpty(s.Name) ? "Unnamed" : s.Name)}";
            if (ImGui.Selectable(label, _selectedStackIndex == i))
                _selectedStackIndex = i;
        }

        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("##PL_StacksRight", new Vector2(rightW, 0), true);

        _selectedStackIndex = Math.Clamp(_selectedStackIndex, 0, roleCfg.Stacks.Count - 1);
        var stack = roleCfg.Stacks[_selectedStackIndex];

        ImGui.Text($"Editing Stack: {_selectedStackIndex}");
        ImGui.Separator();

        // Name
        ImGui.SetNextItemWidth(-1);
        var name = stack.Name ?? "";
        if (ImGui.InputText("Name", ref name, 64))
        {
            stack.Name = name;
            Configuration.Save();
        }

        // Enabled
        var enabled = stack.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            stack.Enabled = enabled;
            Configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Text("ReAction-style Options");
        ImGui.Separator();

        // Modifier keys bitmask
        bool shift = (stack.ModifierKeys & 1u) != 0;
        bool ctrl = (stack.ModifierKeys & 2u) != 0;
        bool alt = (stack.ModifierKeys & 4u) != 0;

        if (ImGui.Checkbox("Require Shift", ref shift)) { stack.ModifierKeys = (stack.ModifierKeys & ~1u) | (shift ? 1u : 0u); Configuration.Save(); }
        if (ImGui.Checkbox("Require Ctrl", ref ctrl)) { stack.ModifierKeys = (stack.ModifierKeys & ~2u) | (ctrl ? 2u : 0u); Configuration.Save(); }
        if (ImGui.Checkbox("Require Alt", ref alt)) { stack.ModifierKeys = (stack.ModifierKeys & ~4u) | (alt ? 4u : 0u); Configuration.Save(); }

        var block = stack.BlockOriginal;
        if (ImGui.Checkbox("Block Base Behavior", ref block)) { stack.BlockOriginal = block; Configuration.Save(); }

        var checkRange = stack.CheckRange;
        if (ImGui.Checkbox("Check Range", ref checkRange)) { stack.CheckRange = checkRange; Configuration.Save(); }

        var checkCd = stack.CheckCooldown;
        if (ImGui.Checkbox("Check Cooldown", ref checkCd)) { stack.CheckCooldown = checkCd; Configuration.Save(); }

        ImGui.Spacing();
        ImGui.Text("Conditions (Priority Gate)");
        ImGui.Separator();

        if (ImGui.Button("+ Add Condition"))
        {
            stack.Conditions.Add(new StackCondition { Type = StackConditionType.Always, Note = "New condition" });
            Configuration.Save();
        }
        ImGui.SameLine();
        if (ImGui.Button("- Remove Condition"))
        {
            if (stack.Conditions.Count > 0)
            {
                stack.Conditions.RemoveAt(stack.Conditions.Count - 1);
                Configuration.Save();
            }
        }

        for (var i = 0; i < stack.Conditions.Count; i++)
        {
            var c = stack.Conditions[i];
            ImGui.PushID(i);

            var cEnabled = c.Enabled;
            if (ImGui.Checkbox("##cEnabled", ref cEnabled))
            {
                c.Enabled = cEnabled;
                Configuration.Save();
            }
            ImGui.SameLine();

            // Type
            var typeLabel = c.Type.ToString();
            ImGui.SetNextItemWidth(180);
            if (ImGui.BeginCombo("##cType", typeLabel))
            {
                foreach (StackConditionType t in Enum.GetValues(typeof(StackConditionType)))
                {
                    if (ImGui.Selectable(t.ToString(), c.Type == t))
                    {
                        c.Type = t;
                        Configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(-1);
            var note = c.Note ?? "";
            if (ImGui.InputText("##cNote", ref note, 128))
            {
                c.Note = note;
                Configuration.Save();
            }

            // Params row
            switch (c.Type)
            {
                case StackConditionType.TargetHpBelowPct:
                case StackConditionType.SelfHpBelowPct:
                    ImGui.Text("Threshold %");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(120);
                    var th = c.ThresholdPct;
                    if (ImGui.SliderFloat("##th", ref th, 0, 100, "%.0f%%"))
                    {
                        c.ThresholdPct = th;
                        Configuration.Save();
                    }
                    break;

                case StackConditionType.PartyMembersBelowPct:
                    ImGui.Text("Party count");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(120);
                    var cnt = c.Count;
                    if (ImGui.SliderInt("##cnt", ref cnt, 0, 8))
                    {
                        c.Count = cnt;
                        Configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("HP%");
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(120);
                    var pth = c.ThresholdPct;
                    if (ImGui.SliderFloat("##pth", ref pth, 0, 100, "%.0f%%"))
                    {
                        c.ThresholdPct = pth;
                        Configuration.Save();
                    }
                    break;
            }

            ImGui.Separator();
            ImGui.PopID();
        }

        ImGui.Spacing();
        ImGui.Text("Per-Ability Stack Selection");
        ImGui.Separator();

        if (ImGui.Button("+ Add Binding"))
        {
            roleCfg.AbilityBindings.Add(new AbilityStackBinding { ActionId = 0, Label = "", StackIndex = roleCfg.DefaultStackIndex });
            Configuration.Save();
        }

        for (var i = 0; i < roleCfg.AbilityBindings.Count; i++)
        {
            var b = roleCfg.AbilityBindings[i];
            ImGui.PushID(10000 + i);

            ImGui.SetNextItemWidth(120);
            var id = (int)b.ActionId;
            if (ImGui.InputInt("Action ID", ref id))
            {
                b.ActionId = (uint)Math.Max(0, id);
                Configuration.Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            var lbl = b.Label ?? "";
            if (ImGui.InputText("##bindLabel", ref lbl, 64))
            {
                b.Label = lbl;
                Configuration.Save();
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(220);
            var current = Math.Clamp(b.StackIndex, 0, roleCfg.Stacks.Count - 1);
            var currentName = roleCfg.Stacks[current].Name;
            if (ImGui.BeginCombo("##bindStack", $"{current}: {currentName}"))
            {
                for (var s = 0; s < roleCfg.Stacks.Count; s++)
                {
                    if (ImGui.Selectable($"{s}: {roleCfg.Stacks[s].Name}", b.StackIndex == s))
                    {
                        b.StackIndex = s;
                        Configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();
            if (ImGui.Button("X"))
            {
                roleCfg.AbilityBindings.RemoveAt(i);
                Configuration.Save();
                ImGui.PopID();
                break;
            }

            ImGui.PopID();
        }

        ImGui.EndChild();
    }

private void DrawSettings()
    {
        // Use this.Configuration to access the instance
        if (ImGui.Checkbox("Enable Auto Rotation", ref this.Configuration.Enabled))
            this.Configuration.Save();

        if (ImGui.Checkbox("In Combat Only", ref this.Configuration.InCombatOnly))
            this.Configuration.Save();

        ImGui.Separator();
        ImGui.Text("Target Priority");

        if (ImGui.BeginCombo("##Priority", this.Configuration.TargetPriority.ToString()))
        {
            foreach (TargetPriority p in Enum.GetValues(typeof(TargetPriority)))
            {
                if (ImGui.Selectable(p.ToString(), this.Configuration.TargetPriority == p))
                {
                    this.Configuration.TargetPriority = p;
                    this.Configuration.Save();
                }
            }
            ImGui.EndCombo();
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

        // Gate Status Section
        ImGuiEx.TextV(ImGuiColors.ParsedBlue, "Gate Checks:");
        ImGui.Columns(2, "gateData", true);

        ImGui.Text("Global Enabled:"); ImGui.NextColumn();
        ImGuiEx.Text(this.Configuration.Enabled ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, this.Configuration.Enabled.ToString()); ImGui.NextColumn();

        // Job-specific enable
        var jobEnabled = jobId switch
        {
            22 => this.Configuration.DRG_Enabled,
            19 => this.Configuration.PLD_Enabled,
            24 => this.Configuration.WHM_Enabled,
            _ => false
        };
        var jobName = jobId switch
        {
            22 => "DRG",
            19 => "PLD",
            24 => "WHM",
            _ => $"Job {jobId}"
        };
        ImGui.Text($"{jobName} Enabled:"); ImGui.NextColumn();
        ImGuiEx.Text(jobEnabled ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, jobEnabled.ToString()); ImGui.NextColumn();

        ImGui.Text("In Combat:"); ImGui.NextColumn();
        ImGuiEx.Text(inCombat ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, inCombat.ToString()); ImGui.NextColumn();

        ImGui.Text("Has Target:"); ImGui.NextColumn();
        ImGuiEx.Text(target != null ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, (target != null).ToString()); ImGui.NextColumn();

        if (target != null)
        {
            ImGui.Text("Target Name:"); ImGui.NextColumn();
            ImGui.Text($"{target.Name}"); ImGui.NextColumn();

            ImGui.Text("Target Distance:"); ImGui.NextColumn();
            ImGui.Text($"{GameState.TargetDistance:F1}y"); ImGui.NextColumn();
        }

        ImGui.Columns(1);

        ImGui.Spacing();
        ImGui.Separator();

        // Run the rotation to get diagnostic info
        var nextAction = P.RotationManager.GetNextAction();

        ImGuiEx.TextV(ImGuiColors.ParsedBlue, "Rotation Decision:");
        ImGui.Columns(2, "rotationData", true);

        ImGui.Text("Last Failure Reason:"); ImGui.NextColumn();
        var failureColor = P.RotationManager.LastFailureReason == "OK" ? ImGuiColors.HealerGreen :
                          P.RotationManager.LastFailureReason.Contains("FALLBACK") ? ImGuiColors.ParsedOrange :
                          ImGuiColors.DalamudRed;
        ImGuiEx.Text(failureColor, P.RotationManager.LastFailureReason); ImGui.NextColumn();

        ImGui.Text("Last Chosen Action:"); ImGui.NextColumn();
        if (P.RotationManager.LastChosenActionId > 0)
        {
            ImGuiEx.Text(ImGuiColors.ParsedOrange, $"{P.RotationManager.LastChosenActionName} (ID:{P.RotationManager.LastChosenActionId})");
        }
        else
        {
            ImGui.TextDisabled("None");
        }
        ImGui.NextColumn();

        ImGui.Columns(1);

        ImGui.Spacing();
        ImGui.Separator();

        // Action Manager State
        ImGuiEx.TextV(ImGuiColors.ParsedBlue, "Action Manager State:");
        ImGui.Columns(2, "amData", true);

        ImGui.Text("Animation Lock:"); ImGui.NextColumn();
        ImGui.Text($"{ActionManager.Instance.AnimationLock:F3}s"); ImGui.NextColumn();

        ImGui.Text("GCD Remaining:"); ImGui.NextColumn();
        ImGui.Text($"{ActionManager.Instance.GetGCDRemaining():F3}s"); ImGui.NextColumn();

        ImGui.Text("Can Weave:"); ImGui.NextColumn();
        ImGuiEx.Text(ActionManager.Instance.CanWeave() ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, ActionManager.Instance.CanWeave().ToString()); ImGui.NextColumn();

        ImGui.Text("Combo Action:"); ImGui.NextColumn();
        var comboAction = ActionManager.Instance.ComboAction;
        var adjustedCombo = ActionManager.Instance.GetAdjustedActionId(comboAction);
        ImGui.Text($"{comboAction} (Adjusted: {adjustedCombo})"); ImGui.NextColumn();

        ImGui.Columns(1);

        ImGui.Spacing();
        ImGui.Separator();

        // Next Action Details (if available)
        ImGuiEx.TextV(ImGuiColors.ParsedBlue, "Next Calculated Action:");

        if (nextAction != null)
        {
            ImGui.Columns(2, "actionData", true);
            ImGui.Text("Action Name:"); ImGui.NextColumn();
            ImGuiEx.Text(ImGuiColors.ParsedOrange, $"{nextAction.Name}"); ImGui.NextColumn();

            ImGui.Text("Action ID:"); ImGui.NextColumn();
            ImGui.Text($"{nextAction.ActionId}"); ImGui.NextColumn();

            ImGui.Text("Targets Self:"); ImGui.NextColumn();
            ImGui.Text($"{nextAction.TargetsSelf}"); ImGui.NextColumn();
            ImGui.Columns(1);
        }
        else
        {
            ImGui.TextDisabled("No action selected this tick.");
        }

        ImGui.Spacing();
        if (ImGui.Button("Generate Debug Dump"))
        {
            DebugDumper.Generate();
        }
    }
}