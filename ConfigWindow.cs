using System;
using System.Numerics;
using AutoRotationPlugin.Managers;
using ECommons.ImGuiMethods;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using Dalamud.Interface.Colors;

namespace AutoRotationPlugin
{
    public class ConfigWindow : Window, IDisposable
    {
        private ParseLord P;
        private Configuration Configuration;

        private uint _selectedJobId = 22;
        private PriorityRoleTab _selectedRole = PriorityRoleTab.DPS;
        private int _selectedStackIndex = 0;

        private SettingsSection _settingsSection = SettingsSection.General;
        private uint _settingsJobId = 22;
        private string _settingsSearch = string.Empty;
        private bool _showOnlyEnabledJobs = false;
        private int _cardCounter = 0;

        private readonly Vector4 _accentColor = ImGuiColors.ParsedBlue;
        private readonly Vector4 _sectionColor = ImGuiColors.ParsedGold;

        public ConfigWindow(ParseLord p, Configuration configuration) : base("Parse Lord Configuration")
        {
            this.P = p;
            this.Configuration = configuration;
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(420, 520),
                MaximumSize = new Vector2(1200, 1000)
            };
        }

        public void Dispose() { }

        public override void Draw()
        {
            _cardCounter = 0;
            DrawHeader();

            if (ImGui.BeginTabBar("MainTabBar"))
            {
                if (ImGui.BeginTabItem("Settings"))
                {
                    DrawSettings();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Priority Stacks"))
                {
                    DrawPriorityStacks();
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

        private void DrawHeader()
        {
            ImGuiEx.TextV(_accentColor, "Parse Lord");
            ImGui.SameLine();
            ImGui.TextDisabled("| Wrath Combo Inspired UI");
            ImGui.Separator();
        }

        private void DrawSettings()
        {
            var avail = ImGui.GetContentRegionAvail();
            var leftW = MathF.Max(200, avail.X * 0.22f);
            var rightW = MathF.Max(300, avail.X - leftW - 12);

            ImGui.BeginChild("##PL_SettingsNav", new Vector2(leftW, 0), true);
            DrawSectionHeader("Navigation");
            if (DrawNavButton("General", _settingsSection == SettingsSection.General))
                _settingsSection = SettingsSection.General;
            if (DrawNavButton("Targeting", _settingsSection == SettingsSection.Targeting))
                _settingsSection = SettingsSection.Targeting;
            if (DrawNavButton("Jobs", _settingsSection == SettingsSection.Jobs))
                _settingsSection = SettingsSection.Jobs;
            ImGui.EndChild();

            ImGui.SameLine();

            ImGui.BeginChild("##PL_SettingsContent", new Vector2(rightW, 0), true);
            switch (_settingsSection)
            {
                case SettingsSection.General:
                    DrawGeneralSettings();
                    break;
                case SettingsSection.Targeting:
                    DrawTargetingSettings();
                    break;
                case SettingsSection.Jobs:
                    DrawJobSettings();
                    break;
            }
            ImGui.EndChild();
        }

        private void DrawGeneralSettings()
        {
            DrawSectionHeader("Core Automation");
            BeginCard();
            if (ImGui.Checkbox("Enable Auto Rotation", ref this.Configuration.Enabled))
                this.Configuration.Save();
            DrawHelpText("Master toggle for all rotations and automation.");

            if (ImGui.Checkbox("In Combat Only", ref this.Configuration.InCombatOnly))
                this.Configuration.Save();
            DrawHelpText("Only run rotations while in combat.");

            if (ImGui.Checkbox("Enable AoE Logic", ref this.Configuration.UseAoE))
                this.Configuration.Save();
            DrawHelpText("Allow AoE decisions when enemy count meets thresholds.");

            var range = this.Configuration.AutoTargetRange;
            if (ImGui.SliderFloat("Auto Target Range", ref range, 5f, 30f, "%.0f y"))
            {
                this.Configuration.AutoTargetRange = range;
                this.Configuration.Save();
            }
            DrawHelpText("Maximum range for target selection and AoE checks.");
            EndCard();
        }

        private void DrawTargetingSettings()
        {
            DrawSectionHeader("Target Selection");
            BeginCard();
            ImGui.Text("Target Priority");
            ImGui.SetNextItemWidth(-1);
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
            DrawHelpText("Defines how targets are chosen when multiple are valid.");
            EndCard();
        }

        private void DrawJobSettings()
        {
            DrawSectionHeader("Job Configuration");
            BeginCard();

            ImGui.Text("Search");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.InputText("##jobSearch", ref _settingsSearch, 64))
            {
                if (_settingsSearch == null)
                    _settingsSearch = string.Empty;
            }

            if (ImGui.Checkbox("Show Enabled Jobs Only", ref _showOnlyEnabledJobs))
            {
            }

            ImGui.Spacing();
            ImGui.Text("Select Job");
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##jobSelect", GetJobLabel(_settingsJobId)))
            {
                foreach (var job in JobList)
                {
                    if (_showOnlyEnabledJobs && !IsJobEnabled(job.Id))
                        continue;

                    if (!JobMatchesSearch(job.Label))
                        continue;

                    if (ImGui.Selectable(job.Label, _settingsJobId == job.Id))
                        _settingsJobId = job.Id;
                }
                ImGui.EndCombo();
            }

            EndCard();

            ImGui.Spacing();
            DrawSectionHeader("Job Options");
            BeginCard();
            DrawJobOptions(_settingsJobId);
            EndCard();
        }

        private void DrawJobOptions(uint jobId)
        {
            switch (jobId)
            {
                case 19:
                    DrawJobToggle("Enabled", ref this.Configuration.PLD_Enabled, "Master toggle for Paladin.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.PLD_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.PLD_AoE_Threshold);
                    DrawJobToggle("Fight or Flight", ref this.Configuration.PLD_Buff_FightOrFlight, "Uses Fight or Flight in burst windows.");
                    DrawJobToggle("Requiescat", ref this.Configuration.PLD_Magic_Requiescat, "Enables magic burst windows.");
                    DrawJobToggle("Sheltron", ref this.Configuration.PLD_Def_Sheltron, "Use defensive mitigation automatically.");
                    break;
                case 22:
                    DrawJobToggle("Enabled", ref this.Configuration.DRG_Enabled, "Master toggle for Dragoon.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.DRG_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.DRG_AoE_Threshold);
                    DrawJobToggle("Lance Charge", ref this.Configuration.DRG_Buff_LanceCharge, "Burst buff usage.");
                    DrawJobToggle("Battle Litany", ref this.Configuration.DRG_Buff_BattleLitany, "Raid buff usage.");
                    DrawJobToggle("Life Surge", ref this.Configuration.DRG_Buff_LifeSurge, "Critical guarantee skill usage.");
                    DrawJobToggle("High Jump", ref this.Configuration.DRG_Jump_HighJump, "Jump option usage.");
                    DrawJobToggle("Geirskogul", ref this.Configuration.DRG_Gauge_Geirskogul, "Gauge spender usage.");
                    DrawJobToggle("Dragonfire Dive", ref this.Configuration.DRG_Jump_DragonfireDive, "AoE jump usage.");
                    break;
                case 24:
                    DrawJobToggle("Enabled", ref this.Configuration.WHM_Enabled, "Master toggle for White Mage.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.WHM_DPS_AoE_Enabled, "Use AoE DPS when threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.WHM_DPS_AoE_Threshold);
                    DrawJobToggle("Presence of Mind", ref this.Configuration.WHM_Buff_PresenceOfMind, "Buff usage for speed.");
                    break;
                case 20:
                    DrawJobToggle("Enabled", ref this.Configuration.MNK_Enabled, "Master toggle for Monk.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.MNK_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.MNK_AoE_Threshold);
                    DrawJobToggle("Riddle of Fire", ref this.Configuration.MNK_Buff_RiddleOfFire, "Burst buff usage.");
                    DrawJobToggle("Brotherhood", ref this.Configuration.MNK_Buff_Brotherhood, "Party buff usage.");
                    break;
                case 30:
                    DrawJobToggle("Enabled", ref this.Configuration.NIN_Enabled, "Master toggle for Ninja.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.NIN_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.NIN_AoE_Threshold);
                    DrawJobToggle("Mug", ref this.Configuration.NIN_Buff_Mug, "Raid buff usage.");
                    DrawJobToggle("Kassatsu", ref this.Configuration.NIN_Buff_Kassatsu, "Ninjutsu amplification.");
                    DrawJobToggle("Bunshin", ref this.Configuration.NIN_Buff_Bunshin, "Shadow clone usage.");
                    break;
                case 34:
                    DrawJobToggle("Enabled", ref this.Configuration.SAM_Enabled, "Master toggle for Samurai.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.SAM_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.SAM_AoE_Threshold);
                    DrawJobToggle("Meikyo Shisui", ref this.Configuration.SAM_Buff_MeikyoShisui, "Combo flexibility usage.");
                    DrawJobToggle("Ikishoten", ref this.Configuration.SAM_Buff_Ikishoten, "Gauge generation usage.");
                    DrawJobToggle("Senei", ref this.Configuration.SAM_Kenki_Senei, "Kenki spender usage.");
                    DrawJobToggle("Guren", ref this.Configuration.SAM_Kenki_Guren, "AoE Kenki spender usage.");
                    break;
                case 39:
                    DrawJobToggle("Enabled", ref this.Configuration.RPR_Enabled, "Master toggle for Reaper.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.RPR_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.RPR_AoE_Threshold);
                    DrawJobToggle("Arcane Circle", ref this.Configuration.RPR_Buff_ArcaneCircle, "Raid buff usage.");
                    DrawJobToggle("Enshroud", ref this.Configuration.RPR_Buff_Enshroud, "Shroud burst usage.");
                    DrawJobToggle("Gluttony", ref this.Configuration.RPR_oGCD_Gluttony, "oGCD usage.");
                    break;
                case 41:
                    DrawJobToggle("Enabled", ref this.Configuration.VPR_Enabled, "Master toggle for Viper.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.VPR_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.VPR_AoE_Threshold);
                    DrawJobToggle("Reawaken", ref this.Configuration.VPR_Buff_Reawaken, "Burst window usage.");
                    DrawJobToggle("Vicewinder", ref this.Configuration.VPR_oGCD_Vicewinder, "oGCD usage.");
                    break;
                case 23:
                    DrawJobToggle("Enabled", ref this.Configuration.BRD_Enabled, "Master toggle for Bard.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.BRD_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.BRD_AoE_Threshold);
                    DrawJobToggle("Raging Strikes", ref this.Configuration.BRD_Buff_RagingStrikes, "Burst buff usage.");
                    DrawJobToggle("Battle Voice", ref this.Configuration.BRD_Buff_BattleVoice, "Raid buff usage.");
                    DrawJobToggle("Radiant Finale", ref this.Configuration.BRD_Buff_RadiantFinale, "Party buff usage.");
                    DrawJobToggle("Barrage", ref this.Configuration.BRD_Buff_Barrage, "Burst skill usage.");
                    break;
                case 31:
                    DrawJobToggle("Enabled", ref this.Configuration.MCH_Enabled, "Master toggle for Machinist.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.MCH_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.MCH_AoE_Threshold);
                    DrawJobToggle("Wildfire", ref this.Configuration.MCH_Buff_Wildfire, "Burst window usage.");
                    DrawJobToggle("Barrel Stabilizer", ref this.Configuration.MCH_Buff_BarrelStabilizer, "Heat generation usage.");
                    DrawJobToggle("Reassemble", ref this.Configuration.MCH_Buff_Reassemble, "Guaranteed critical usage.");
                    DrawJobToggle("Summon Queen", ref this.Configuration.MCH_Summon_Queen, "Automaton usage.");
                    break;
                case 38:
                    DrawJobToggle("Enabled", ref this.Configuration.DNC_Enabled, "Master toggle for Dancer.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.DNC_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.DNC_AoE_Threshold);
                    DrawJobToggle("Standard Step", ref this.Configuration.DNC_Buff_StandardStep, "Dance buff usage.");
                    DrawJobToggle("Technical Step", ref this.Configuration.DNC_Buff_TechnicalStep, "Raid buff usage.");
                    DrawJobToggle("Devilment", ref this.Configuration.DNC_Buff_Devilment, "Burst buff usage.");
                    DrawJobToggle("Flourish", ref this.Configuration.DNC_Buff_Flourish, "Proc generation usage.");
                    break;
                case 25:
                    DrawJobToggle("Enabled", ref this.Configuration.BLM_Enabled, "Master toggle for Black Mage.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.BLM_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.BLM_AoE_Threshold);
                    DrawJobToggle("Ley Lines", ref this.Configuration.BLM_Buff_LeyLines, "Buff usage.");
                    DrawJobToggle("Triplecast", ref this.Configuration.BLM_Buff_Triplecast, "Movement casting usage.");
                    break;
                case 27:
                    DrawJobToggle("Enabled", ref this.Configuration.SMN_Enabled, "Master toggle for Summoner.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.SMN_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.SMN_AoE_Threshold);
                    DrawJobToggle("Searing Light", ref this.Configuration.SMN_Buff_SearingLight, "Party buff usage.");
                    break;
                case 35:
                    DrawJobToggle("Enabled", ref this.Configuration.RDM_Enabled, "Master toggle for Red Mage.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.RDM_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.RDM_AoE_Threshold);
                    DrawJobToggle("Embolden", ref this.Configuration.RDM_Buff_Embolden, "Raid buff usage.");
                    DrawJobToggle("Manafication", ref this.Configuration.RDM_Buff_Manafication, "Mana burst usage.");
                    DrawJobToggle("Acceleration", ref this.Configuration.RDM_Buff_Acceleration, "Proc usage.");
                    break;
                case 42:
                    DrawJobToggle("Enabled", ref this.Configuration.PCT_Enabled, "Master toggle for Pictomancer.");
                    DrawJobToggle("Enable AoE", ref this.Configuration.PCT_AoE_Enabled, "Use AoE when enemy count threshold is met.");
                    DrawJobThreshold("AoE Threshold", ref this.Configuration.PCT_AoE_Threshold);
                    DrawJobToggle("Starry Muse", ref this.Configuration.PCT_Buff_StarryMuse, "Buff usage.");
                    break;
                default:
                    ImGui.TextDisabled("No configuration available for this job.");
                    break;
            }
        }

        private void DrawJobToggle(string label, ref bool value, string help)
        {
            if (ImGui.Checkbox(label, ref value))
                this.Configuration.Save();
            DrawHelpText(help);
        }

        private void DrawJobThreshold(string label, ref int value)
        {
            var local = value;
            if (ImGui.SliderInt(label, ref local, 1, 8))
            {
                value = local;
                this.Configuration.Save();
            }
        }

        private void DrawPriorityStacks()
        {
            DrawSectionHeader("Priority Stacks");
            BeginCard();
            ImGui.Text("Job");
            ImGui.SameLine();

            var jobLabel = GetJobLabel(_selectedJobId);

            if (ImGui.BeginCombo("##PL_JobSelect", jobLabel))
            {
                foreach (var job in JobList)
                {
                    if (ImGui.Selectable(job.Label, _selectedJobId == job.Id))
                        _selectedJobId = job.Id;
                }
                ImGui.EndCombo();
            }
            EndCard();

            ImGui.Spacing();

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

            var avail = ImGui.GetContentRegionAvail();
            var leftW = MathF.Max(240, avail.X * 0.32f);
            var rightW = MathF.Max(300, avail.X - leftW - 12);

            ImGui.BeginChild("##PL_StacksLeft", new Vector2(leftW, 0), true);

            DrawSectionHeader("Stacks");

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

                    foreach (var b in roleCfg.AbilityBindings)
                        b.StackIndex = Math.Clamp(b.StackIndex, 0, roleCfg.Stacks.Count - 1);

                    Configuration.Save();
                }
            }

            ImGui.Spacing();
            ImGui.Text("Default Stack");
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

            DrawSectionHeader($"Editing Stack: {_selectedStackIndex}");

            ImGui.SetNextItemWidth(-1);
            var name = stack.Name ?? "";
            if (ImGui.InputText("Name", ref name, 64))
            {
                stack.Name = name;
                Configuration.Save();
            }

            var enabled = stack.Enabled;
            if (ImGui.Checkbox("Enabled", ref enabled))
            {
                stack.Enabled = enabled;
                Configuration.Save();
            }

            ImGui.Spacing();
            ImGui.Text("ReAction-style Options");
            ImGui.Separator();

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

            ImGuiEx.TextV(ImGuiColors.ParsedBlue, "Gate Checks:");
            ImGui.Columns(2, "gateData", true);

            ImGui.Text("Global Enabled:"); ImGui.NextColumn();
            ImGuiEx.Text(this.Configuration.Enabled ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed, this.Configuration.Enabled.ToString()); ImGui.NextColumn();

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

        private void DrawSectionHeader(string text)
        {
            ImGuiEx.TextV(_sectionColor, text);
            ImGui.Separator();
        }

        private void BeginCard()
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.08f, 0.08f, 0.08f, 0.6f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.BeginChild($"##card{_cardCounter++}", new Vector2(0, 0), true);
        }

        private void EndCard()
        {
            ImGui.EndChild();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor();
        }

        private bool DrawNavButton(string label, bool active)
        {
            if (active)
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.35f, 0.6f, 1f));
            var pressed = ImGui.Button(label, new Vector2(-1, 0));
            if (active)
                ImGui.PopStyleColor();
            return pressed;
        }

        private void DrawHelpText(string text)
        {
            ImGui.TextDisabled(text);
            ImGui.Spacing();
        }

        private bool IsJobEnabled(uint jobId)
        {
            return jobId switch
            {
                19 => this.Configuration.PLD_Enabled,
                20 => this.Configuration.MNK_Enabled,
                22 => this.Configuration.DRG_Enabled,
                23 => this.Configuration.BRD_Enabled,
                24 => this.Configuration.WHM_Enabled,
                25 => this.Configuration.BLM_Enabled,
                27 => this.Configuration.SMN_Enabled,
                30 => this.Configuration.NIN_Enabled,
                31 => this.Configuration.MCH_Enabled,
                34 => this.Configuration.SAM_Enabled,
                35 => this.Configuration.RDM_Enabled,
                38 => this.Configuration.DNC_Enabled,
                39 => this.Configuration.RPR_Enabled,
                41 => this.Configuration.VPR_Enabled,
                42 => this.Configuration.PCT_Enabled,
                _ => false
            };
        }

        private bool JobMatchesSearch(string label)
        {
            if (string.IsNullOrWhiteSpace(_settingsSearch))
                return true;

            return label.Contains(_settingsSearch, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetJobLabel(uint jobId)
        {
            return jobId switch
            {
                19 => "Paladin (PLD)",
                20 => "Monk (MNK)",
                22 => "Dragoon (DRG)",
                23 => "Bard (BRD)",
                24 => "White Mage (WHM)",
                25 => "Black Mage (BLM)",
                27 => "Summoner (SMN)",
                30 => "Ninja (NIN)",
                31 => "Machinist (MCH)",
                34 => "Samurai (SAM)",
                35 => "Red Mage (RDM)",
                38 => "Dancer (DNC)",
                39 => "Reaper (RPR)",
                41 => "Viper (VPR)",
                42 => "Pictomancer (PCT)",
                _ => $"Job {jobId}"
            };
        }

        private static readonly (uint Id, string Label)[] JobList =
        {
            (19, "Paladin (PLD)"),
            (20, "Monk (MNK)"),
            (22, "Dragoon (DRG)"),
            (23, "Bard (BRD)"),
            (24, "White Mage (WHM)"),
            (25, "Black Mage (BLM)"),
            (27, "Summoner (SMN)"),
            (30, "Ninja (NIN)"),
            (31, "Machinist (MCH)"),
            (34, "Samurai (SAM)"),
            (35, "Red Mage (RDM)"),
            (38, "Dancer (DNC)"),
            (39, "Reaper (RPR)"),
            (41, "Viper (VPR)"),
            (42, "Pictomancer (PCT)")
        };

        private enum SettingsSection
        {
            General,
            Targeting,
            Jobs
        }
    }
}
