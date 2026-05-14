// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using System.Numerics;
using ImGuiNET;

namespace eft_dma_radar.Silk6.UI.Panels
{
    internal static partial class SettingsPanel
    {
        private static void DrawQuestZonesTab()
        {
            ImGui.Spacing();

            // ── Master toggle ─────────────────────────────────────────────────
            bool showQuests = Config.ShowQuests;
            if (UIControls.ToggleRow("Show Quest Zones", ref showQuests, "Master toggle — draw quest objective zones on the radar"))
            {
                Config.ShowQuests = showQuests;
                Config.MarkDirty();
            }

            if (!Config.ShowQuests)
            {
                ImGui.Spacing();
                ImGui.TextDisabled("Enable \"Show Quest Zones\" to configure options.");
                return;
            }

            UIControls.Section("Filters");

            // Kappa
            bool kappaFilter = Config.QuestKappaFilter;
            if (UIControls.ToggleRow("Kappa Only", ref kappaFilter, "Only show quest zones for quests required to unlock the Kappa container"))
            {
                Config.QuestKappaFilter = kappaFilter;
                Config.MarkDirty();
            }

            // Optional objectives
            bool showOptional = Config.QuestShowOptional;
            if (UIControls.ToggleRow("Show Optional Objectives", ref showOptional, "Include optional quest objectives in the radar display"))
            {
                Config.QuestShowOptional = showOptional;
                Config.MarkDirty();
            }

            UIControls.Section("Objective Types");

            // Per-type toggles — these directly control which categories of zones appear on radar
            bool showKill = Config.QuestShowKillZones;
            if (UIControls.ToggleRow("Kill Zones", ref showKill, "Show zones for \"eliminate\" / kill objectives"))
            {
                Config.QuestShowKillZones = showKill;
                Config.MarkDirty();
            }

            bool showFind = Config.QuestShowFindZones;
            if (UIControls.ToggleRow("Find / Collect Zones", ref showFind, "Show zones for \"find item\" / \"hand over item\" objectives"))
            {
                Config.QuestShowFindZones = showFind;
                Config.MarkDirty();
            }

            bool showPlace = Config.QuestShowPlaceZones;
            if (UIControls.ToggleRow("Place / Plant Zones", ref showPlace, "Show zones for \"mark\" / \"plant item\" objectives"))
            {
                Config.QuestShowPlaceZones = showPlace;
                Config.MarkDirty();
            }

            bool showReach = Config.QuestShowReachZones;
            if (UIControls.ToggleRow("Reach / Visit Zones", ref showReach, "Show zones for \"visit location\" and other movement objectives"))
            {
                Config.QuestShowReachZones = showReach;
                Config.MarkDirty();
            }

            UIControls.Section("Display");

            // Show names
            bool showNames = Config.QuestShowNames;
            if (UIControls.ToggleRow("Show Names", ref showNames, "Draw the quest name label next to each zone marker"))
            {
                Config.QuestShowNames = showNames;
                Config.MarkDirty();
            }

            // Show distance
            bool showDistance = Config.QuestShowDistance;
            if (UIControls.ToggleRow("Show Distance", ref showDistance, "Draw the distance (in metres) from the local player to each zone marker"))
            {
                Config.QuestShowDistance = showDistance;
                Config.MarkDirty();
            }

            // Show outlines
            bool showOutlines = Config.QuestShowOutlines;
            if (UIControls.ToggleRow("Show Outlines", ref showOutlines, "Draw the polygon outline of zone areas (where available)"))
            {
                Config.QuestShowOutlines = showOutlines;
                Config.MarkDirty();
            }

            UIControls.Section("Distance Culling");

            float maxDist = Config.QuestMaxDistance;
            bool limitDist = maxDist > 0f;

            if (UIControls.ToggleRow("Limit Visibility Range", ref limitDist, "Only draw quest zones within a maximum distance from the local player.\nHelps reduce clutter on large maps."))
            {
                Config.QuestMaxDistance = limitDist ? 200f : 0f;
                Config.MarkDirty();
            }

            if (limitDist)
            {
                ImGui.Indent(16);
                if (UIControls.StepperFloat("Max Distance", ref maxDist, 10f, 1000f, 10f, "{0:0} m",
                    "Quest zones farther than this distance are hidden"))
                {
                    Config.QuestMaxDistance = maxDist;
                    Config.MarkDirty();
                }
                ImGui.Unindent(16);
            }
        }
    }
}
