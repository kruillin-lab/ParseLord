using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;
using System.Collections.Generic;

namespace AutoRotationPlugin
{
    public class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration config;
        private string selectedTab = "General";
        private readonly Dictionary<string, bool> sectionStates = new();

        public ConfigWindow(Plugin plugin, Configuration configuration) : base("Parse Lord Configuration###ParseLordConfig")
        {
            config = configuration;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(800, 600),
                MaximumSize = new Vector2(1200, 900)
            };
        }

        public void Dispose() { }

        public override void Draw()
        {
            bool enabled = config.Enabled;
            if (ImGui.Checkbox("Enable Parse Lord", ref enabled))
            {
                config.Enabled = enabled;
                config.Save();
            }

            ImGui.SameLine();
            ImGui.TextColored(enabled ? new Vector4(0, 1, 0, 1) : new Vector4(1, 0, 0, 1),
                enabled ? "[ENABLED]" : "[DISABLED]");

            ImGui.Separator();

            if (ImGui.BeginTable("MainTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
            {
                ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 180f);
                ImGui.TableSetupColumn("Content", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();

                // Sidebar
                ImGui.TableSetColumnIndex(0);
                DrawSidebar();

                // Content
                ImGui.TableSetColumnIndex(1);
                DrawBody();

                ImGui.EndTable();
            }
        }

        private void DrawSidebar()
        {
            if (ImGui.Selectable("General", selectedTab == "General")) selectedTab = "General";
            ImGui.Separator();
            if (ImGui.Selectable("Dragoon", selectedTab == "Dragoon")) selectedTab = "Dragoon";
            if (ImGui.Selectable("Paladin", selectedTab == "Paladin")) selectedTab = "Paladin";
            if (ImGui.Selectable("White Mage", selectedTab == "White Mage")) selectedTab = "White Mage";
        }

        private void DrawBody()
        {
            switch (selectedTab)
            {
                case "General": DrawGeneralTab(); break;
                case "Dragoon": DrawDragoonTab(); break;
                case "Paladin": DrawPaladinTab(); break;
                case "White Mage": DrawWhiteMageTab(); break;
            }
        }

        private void DrawGeneralTab()
        {
            ImGui.Text("Global Settings");
            ImGui.Separator();

            bool combatOnly = config.InCombatOnly;
            if (ImGui.Checkbox("Combat Only", ref combatOnly))
            {
                config.InCombatOnly = combatOnly;
                config.Save();
            }

            bool useAoE = config.UseAoE;
            if (ImGui.Checkbox("Use AoE", ref useAoE))
            {
                config.UseAoE = useAoE;
                config.Save();
            }

            if (useAoE)
            {
                int aoeCount = config.AoETargetCount;
                if (ImGui.SliderInt("Min AoE Targets", ref aoeCount, 2, 10))
                {
                    config.AoETargetCount = aoeCount;
                    config.Save();
                }
            }

            ImGui.Spacing();
            ImGui.Separator();

            if (ImGui.CollapsingHeader("Auto Targeting", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent();

                var targetPriority = config.TargetPriority;
                if (ImGui.BeginCombo("Target Priority", targetPriority.ToString()))
                {
                    foreach (var type in Enum.GetValues(typeof(TargetPriority)))
                    {
                        if (ImGui.Selectable(type.ToString(), type.Equals(targetPriority)))
                        {
                            config.TargetPriority = (TargetPriority)type;
                            config.Save();
                        }
                    }
                    ImGui.EndCombo();
                }

                var range = config.AutoTargetRange;
                if (ImGui.SliderFloat("Scan Range", ref range, 5f, 50f))
                {
                    config.AutoTargetRange = range;
                    config.Save();
                }

                bool autoSwitch = config.AutoTargetSwitch;
                if (ImGui.Checkbox("Auto Switch Target", ref autoSwitch))
                {
                    config.AutoTargetSwitch = autoSwitch;
                    config.Save();
                }

                ImGui.Unindent();
            }
        }

        private void DrawDragoonTab()
        {
            ImGui.Text("Dragoon Settings");
            ImGui.Separator();

            bool drgEnabled = config.DRG_Enabled;
            if (ImGui.Checkbox("Enable Dragoon", ref drgEnabled))
            {
                config.DRG_Enabled = drgEnabled;
                config.Save();
            }

            if (ImGui.CollapsingHeader("Buffs"))
            {
                DrawCheckbox("Use Lance Charge", nameof(Configuration.DRG_Buff_LanceCharge));
                DrawCheckbox("Use Battle Litany", nameof(Configuration.DRG_Buff_BattleLitany));
                DrawCheckbox("Use Life Surge", nameof(Configuration.DRG_Buff_LifeSurge));
            }

            if (ImGui.CollapsingHeader("Jumps"))
            {
                DrawCheckbox("High Jump", nameof(Configuration.DRG_Jump_HighJump));
                DrawCheckbox("Dragonfire Dive", nameof(Configuration.DRG_Jump_DragonfireDive));
                DrawCheckbox("Geirskogul", nameof(Configuration.DRG_Gauge_Geirskogul));
            }
        }

        private void DrawPaladinTab()
        {
            ImGui.Text("Paladin Settings");
            ImGui.Separator();
            DrawCheckbox("Enable Paladin", nameof(Configuration.PLD_Enabled));

            if (ImGui.CollapsingHeader("Defensives"))
            {
                DrawCheckbox("Auto Sheltron", nameof(Configuration.PLD_Def_Sheltron));
            }
            if (ImGui.CollapsingHeader("Offense"))
            {
                DrawCheckbox("Fight or Flight", nameof(Configuration.PLD_Buff_FightOrFlight));
                DrawCheckbox("Requiescat", nameof(Configuration.PLD_Magic_Requiescat));
            }
        }

        private void DrawWhiteMageTab()
        {
            ImGui.Text("White Mage Settings");
            ImGui.Separator();
            DrawCheckbox("Enable White Mage", nameof(Configuration.WHM_Enabled));

            if (ImGui.CollapsingHeader("DPS"))
            {
                DrawCheckbox("Enable AoE DPS", nameof(Configuration.WHM_DPS_AoE_Enabled));
            }
            if (ImGui.CollapsingHeader("Healing"))
            {
                DrawCheckbox("Use Benediction", nameof(Configuration.WHM_oGCD_Benediction));
                DrawCheckbox("Use Tetragrammaton", nameof(Configuration.WHM_oGCD_Tetra));
            }
        }

        private void DrawCheckbox(string label, string propertyName)
        {
            var prop = typeof(Configuration).GetProperty(propertyName);
            if (prop == null) return;

            bool val = (bool)(prop.GetValue(config) ?? false);
            if (ImGui.Checkbox(label, ref val))
            {
                prop.SetValue(config, val);
                config.Save();
            }
        }
    }
}