// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using ImGuiNET;

namespace eft_dma_radar.Silk6.UI.Panels
{
    internal static partial class SettingsPanel
    {
        private static void DrawPlayersTab()
        {
            ImGui.Spacing();

            UIControls.Section("Rendering");

            bool playersOnTop = Config.PlayersOnTop;
            if (UIControls.ToggleRow("Players On Top", ref playersOnTop, "Draw players above all other entities"))
            {
                Config.PlayersOnTop = playersOnTop;
                Config.MarkDirty();
            }

            bool connectGroups = Config.ConnectGroups;
            if (UIControls.ToggleRow("Connect Groups", ref connectGroups, "Draw lines connecting players in the same group"))
            {
                Config.ConnectGroups = connectGroups;
                Config.MarkDirty();
            }

            UIControls.Section("Aimline");

            bool showAimlines = Config.ShowAimlines;
            if (UIControls.ToggleRow("Show Aimlines", ref showAimlines, "Show facing direction lines on player markers"))
            {
                Config.ShowAimlines = showAimlines;
                Config.MarkDirty();
            }

            if (Config.ShowAimlines)
            {
                ImGui.Indent(16);

                int aimlineLength = Config.AimlineLength;
                if (UIControls.Stepper("Length", ref aimlineLength, 0, 100, 5, tooltip: "Aimline length in pixels (human players)"))
                {
                    Config.AimlineLength = aimlineLength;
                    Config.MarkDirty();
                }

                bool highAlert = Config.HighAlert;
                if (UIControls.ToggleRow("High Alert", ref highAlert, "Extend aimline when an enemy is aiming at you"))
                {
                    Config.HighAlert = highAlert;
                    Config.MarkDirty();
                }

                ImGui.Unindent(16);
            }

            UIControls.Section("Aimview");

            bool showAimview = Config.ShowAimview;
            if (UIControls.ToggleRow("Show Aimview", ref showAimview, "First-person projection widget showing nearby players"))
            {
                Config.ShowAimview = showAimview;
                Config.MarkDirty();
            }

            if (Config.ShowAimview)
            {
                ImGui.Indent(16);

                bool aimviewLoot = Config.AimviewShowLoot;
                if (UIControls.ToggleRow("Show Loot", ref aimviewLoot, "Show nearby filtered loot items in the aimview"))
                {
                    Config.AimviewShowLoot = aimviewLoot;
                    Config.MarkDirty();
                }

                bool aimviewCorpses = Config.AimviewShowCorpses;
                if (UIControls.ToggleRow("Show Corpses", ref aimviewCorpses, "Show nearby corpses with gear value in the aimview"))
                {
                    Config.AimviewShowCorpses = aimviewCorpses;
                    Config.MarkDirty();
                }

                bool aimviewContainers = Config.AimviewShowContainers;
                if (UIControls.ToggleRow("Show Containers", ref aimviewContainers, "Show nearby static containers in the aimview"))
                {
                    Config.AimviewShowContainers = aimviewContainers;
                    Config.MarkDirty();
                }

                bool aimviewSkeleton = Config.AimviewShowSkeleton;
                if (UIControls.ToggleRow("Show Skeleton", ref aimviewSkeleton, "Draw bone skeleton for players (advanced aimview only).\nFalls back to a dot when off or skeleton data isn't ready yet."))
                {
                    Config.AimviewShowSkeleton = aimviewSkeleton;
                    Config.MarkDirty();
                }

                bool aimviewPlayerLabels = Config.AimviewShowPlayerLabels;
                if (UIControls.ToggleRow("Show Player Labels", ref aimviewPlayerLabels, "Show \"Name (distance)\" labels under each player"))
                {
                    Config.AimviewShowPlayerLabels = aimviewPlayerLabels;
                    Config.MarkDirty();
                }

                bool aimviewItemLabels = Config.AimviewShowItemLabels;
                if (UIControls.ToggleRow("Show Item Labels", ref aimviewItemLabels, "Show labels under loot, corpse, and container markers.\nTurn off for a less cluttered view — markers stay visible."))
                {
                    Config.AimviewShowItemLabels = aimviewItemLabels;
                    Config.MarkDirty();
                }

                bool aimviewHideAI = Config.AimviewHideAIPlayers;
                if (UIControls.ToggleRow("Hide AI Players", ref aimviewHideAI, "Hide Scav / Raider / Boss AI from the aimview.\nUseful on raids with many AI."))
                {
                    Config.AimviewHideAIPlayers = aimviewHideAI;
                    Config.MarkDirty();
                }

                if (UIControls.BeginAdvanced("Aimview tuning"))
                {

                    float playerDist = Config.AimviewPlayerDistance;
                    if (UIControls.StepperFloat("Player Range", ref playerDist, 50f, 500f, 10f, "{0:0}m",
                        "Max distance for players to appear in the aimview"))
                    {
                        Config.AimviewPlayerDistance = playerDist;
                        Config.MarkDirty();
                    }

                    float lootDist = Config.AimviewLootDistance;
                    if (UIControls.StepperFloat("Loot Range", ref lootDist, 5f, 50f, 1f, "{0:0}m",
                        "Max distance for loot and corpses in the aimview"))
                    {
                        Config.AimviewLootDistance = lootDist;
                        Config.MarkDirty();
                    }

                    float eyeHeight = Config.AimviewEyeHeight;
                    if (UIControls.StepperFloat("Eye Height", ref eyeHeight, 0.5f, 2.0f, 0.05f, "{0:0.00}m",
                        "Camera height above body root — adjust if loot\nappears too high or too low (default: 1.50m)"))
                    {
                        Config.AimviewEyeHeight = eyeHeight;
                        Config.MarkDirty();
                    }

                    float zoom = Config.AimviewZoom;
                    if (UIControls.StepperFloat("Zoom", ref zoom, 0.5f, 3.0f, 0.1f, "{0:0.0}x",
                        "Zoom level (1.0 = ~90\u00b0 FOV, higher = zoomed in)"))
                    {
                        Config.AimviewZoom = zoom;
                        Config.MarkDirty();
                    }

                    int minLootValue = Config.AimviewMinLootValue;
                    if (UIControls.Stepper("Min Loot \u20bd", ref minLootValue, 0, 10_000_000, 5000, "{0:N0}",
                        "Hide loot cheaper than this price to reduce clutter.\nWishlisted items are always shown. 0 = no filter."))
                    {
                        Config.AimviewMinLootValue = Math.Max(minLootValue, 0);
                        Config.MarkDirty();
                    }

                    int maxLoot = Config.AimviewMaxLoot;
                    if (UIControls.Stepper("Max Loot", ref maxLoot, 0, 64, 1,
                        tooltip: "Maximum number of loot markers drawn at once"))
                    {
                        Config.AimviewMaxLoot = maxLoot;
                        Config.MarkDirty();
                    }

                    int maxCorpses = Config.AimviewMaxCorpses;
                    if (UIControls.Stepper("Max Corpses", ref maxCorpses, 0, 32, 1,
                        tooltip: "Maximum number of corpse markers drawn at once"))
                    {
                        Config.AimviewMaxCorpses = maxCorpses;
                        Config.MarkDirty();
                    }

                    int maxContainers = Config.AimviewMaxContainers;
                    if (UIControls.Stepper("Max Containers", ref maxContainers, 0, 32, 1,
                        tooltip: "Maximum number of container markers drawn at once"))
                    {
                        Config.AimviewMaxContainers = maxContainers;
                        Config.MarkDirty();
                    }

                    UIControls.EndAdvanced();
                }

                UIControls.Section("Advanced Aimview");

                bool advancedAimview = Config.UseAdvancedAimview;
                if (UIControls.ToggleRow("Use Advanced Aimview", ref advancedAimview, "Use real game camera data (ViewMatrix) for pixel-accurate\nprojection. Requires CameraManager — falls back to synthetic\ncamera if unavailable."))
                {
                    Config.UseAdvancedAimview = advancedAimview;
                    Config.MarkDirty();
                }

                if (Config.UseAdvancedAimview)
                {
                    ImGui.SetNextItemWidth(160);
                    int monW = Config.GameMonitorWidth;
                    if (ImGui.InputInt("Game Monitor Width", ref monW, 0, 0))
                    {
                        monW = Math.Clamp(monW, 640, 7680);
                        Config.GameMonitorWidth = monW;
                        Config.MarkDirty();
                        CameraManager.UpdateViewportRes(Config.GameMonitorWidth, Config.GameMonitorHeight);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Width of the monitor running EFT (pixels)");

                    ImGui.SetNextItemWidth(160);
                    int monH = Config.GameMonitorHeight;
                    if (ImGui.InputInt("Game Monitor Height", ref monH, 0, 0))
                    {
                        monH = Math.Clamp(monH, 480, 4320);
                        Config.GameMonitorHeight = monH;
                        Config.MarkDirty();
                        CameraManager.UpdateViewportRes(Config.GameMonitorWidth, Config.GameMonitorHeight);
                    }
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Height of the monitor running EFT (pixels)");

                    if (!CameraManager.IsActive)
                    {
                        ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), "CameraManager not active — waiting for raid");
                    }
                }

                ImGui.Unindent(16);
            }

            UIControls.Section("Profile");

            bool profileLookups = Config.ProfileLookups;
            if (UIControls.ToggleRow("Profile Lookups", ref profileLookups, "Fetch player stats from tarkov.dev (K/D, hours, survival rate)"))
            {
                Config.ProfileLookups = profileLookups;
                Config.MarkDirty();
            }

            UIControls.Section("Performance");

            bool distAware = Config.DistanceAwareRefresh;
            if (UIControls.ToggleRow("Distance-Aware Gear/Hands Refresh", ref distAware,
                "Re-read gear and hands less often for far-away players to reduce DMA load.\n" +
                "Safety guarantees:\n" +
                "  \u2022 The FIRST read for every player always runs at full cadence (so you still ID\n" +
                "    new targets at any range).\n" +
                "  \u2022 Throttling is automatically bypassed while you are aiming down sights (ADS),\n" +
                "    so a sniper sees fresh gear/weapon data on far targets during an engagement.\n" +
                "Disable this if you want maximum freshness everywhere at higher DMA cost."))
            {
                Config.DistanceAwareRefresh = distAware;
                Config.MarkDirty();
            }

            if (Config.DistanceAwareRefresh && UIControls.BeginAdvanced("Distance-aware tuning"))
            {
                float nearM = Config.DistanceRefreshNearMeters;
                if (UIControls.StepperFloat("Near Range", ref nearM, 25f, 500f, 25f, "{0:0} m",
                    "Players closer than this always use the full refresh cadence (1\u00d7).\nDefault: 150 m."))
                {
                    Config.DistanceRefreshNearMeters = nearM;
                    if (Config.DistanceRefreshMidMeters < nearM + 25f)
                        Config.DistanceRefreshMidMeters = nearM + 25f;
                    Config.MarkDirty();
                }

                float midM = Config.DistanceRefreshMidMeters;
                float midMin = MathF.Max(50f, Config.DistanceRefreshNearMeters + 25f);
                if (UIControls.StepperFloat("Mid Range", ref midM, midMin, 1500f, 25f, "{0:0} m",
                    "Players between Near and Mid use the \"Mid\" multipliers.\nAnything beyond Mid uses the \"Far\" multipliers.\nDefault: 300 m."))
                {
                    Config.DistanceRefreshMidMeters = midM;
                    Config.MarkDirty();
                }

                ImGui.Spacing();
                ImGui.TextDisabled("Multipliers (1\u00d7 = no throttling)");

                float gearMid = Config.GearRefreshMidMul;
                if (UIControls.StepperFloat("Gear Mid", ref gearMid, 1f, 10f, 0.1f, "{0:0.0}\u00d7",
                    "Multiplier for gear re-reads at mid range.\nBase gear interval is 10 s, so 2.0\u00d7 = 20 s between re-reads.\nDefault: 2.0\u00d7."))
                {
                    Config.GearRefreshMidMul = gearMid;
                    if (Config.GearRefreshFarMul < gearMid)
                        Config.GearRefreshFarMul = gearMid;
                    Config.MarkDirty();
                }

                float gearFar = Config.GearRefreshFarMul;
                if (UIControls.StepperFloat("Gear Far", ref gearFar, Config.GearRefreshMidMul, 20f, 0.5f, "{0:0.0}\u00d7",
                    "Multiplier for gear re-reads at far range (beyond Mid).\nBase 10 s, so 3.0\u00d7 = 30 s between re-reads.\nDefault: 3.0\u00d7."))
                {
                    Config.GearRefreshFarMul = gearFar;
                    Config.MarkDirty();
                }

                float handsMid = Config.HandsRefreshMidMul;
                if (UIControls.StepperFloat("Hands Mid", ref handsMid, 1f, 10f, 0.1f, "{0:0.0}\u00d7",
                    "Multiplier for hands (held weapon) re-reads at mid range.\nBase 3 s, so 2.0\u00d7 = 6 s between re-reads.\nDefault: 2.0\u00d7."))
                {
                    Config.HandsRefreshMidMul = handsMid;
                    if (Config.HandsRefreshFarMul < handsMid)
                        Config.HandsRefreshFarMul = handsMid;
                    Config.MarkDirty();
                }

                float handsFar = Config.HandsRefreshFarMul;
                if (UIControls.StepperFloat("Hands Far", ref handsFar, Config.HandsRefreshMidMul, 20f, 0.5f, "{0:0.0}\u00d7",
                    "Multiplier for hands re-reads at far range (beyond Mid).\nBase 3 s, so 4.0\u00d7 = 12 s between re-reads.\nDefault: 4.0\u00d7."))
                {
                    Config.HandsRefreshFarMul = handsFar;
                    Config.MarkDirty();
                }

                ImGui.Spacing();
                ImGui.TextDisabled("Tip: while ADS, throttling is bypassed automatically.");

                UIControls.EndAdvanced();
            }
        }
    }
}
