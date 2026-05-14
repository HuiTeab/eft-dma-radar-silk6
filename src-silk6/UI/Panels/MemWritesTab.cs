// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using ImGuiNET;

namespace eft_dma_radar.Silk6.UI.Panels
{
    internal static partial class SettingsPanel
    {
        private static void DrawMemWritesTab()
        {
            ImGui.Spacing();

            bool masterEnabled = Config.MemWritesEnabled;
            if (UIControls.ToggleRow("Enable Memory Writes", ref masterEnabled, "Master toggle — enables all active memory write features"))
            {
                Config.MemWritesEnabled = masterEnabled;
                Config.MarkDirty();
            }

            if (!masterEnabled)
                ImGui.BeginDisabled();

            // ═══════════════════════════════════════════════════════════════
            // Weapons
            // ═══════════════════════════════════════════════════════════════
            UIControls.Section("Weapons");

            bool noRecoil = Config.MemWrites.NoRecoil;
            if (UIControls.ToggleRow("No Recoil", ref noRecoil))
            {
                Config.MemWrites.NoRecoil = noRecoil;
                Config.MarkDirty();
            }
            if (noRecoil)
            {
                ImGui.Indent(16);
                int recoilAmt = Config.MemWrites.NoRecoilAmount;
                if (UIControls.Stepper("Recoil %", ref recoilAmt, 0, 100, 5, tooltip: "0 = no recoil, 100 = full recoil"))
                {
                    Config.MemWrites.NoRecoilAmount = recoilAmt;
                    Config.MarkDirty();
                }

                int swayAmt = Config.MemWrites.NoSwayAmount;
                if (UIControls.Stepper("Sway %", ref swayAmt, 0, 100, 5, tooltip: "0 = no sway, 100 = full sway"))
                {
                    Config.MemWrites.NoSwayAmount = swayAmt;
                    Config.MarkDirty();
                }
                ImGui.Unindent(16);
            }

            bool magDrills = Config.MemWrites.MagDrills;
            if (UIControls.ToggleRow("Mag Drills", ref magDrills, "Fast magazine load/unload speed"))
            {
                Config.MemWrites.MagDrills = magDrills;
                Config.MarkDirty();
            }

            bool weapCol = Config.MemWrites.DisableWeaponCollision;
            if (UIControls.ToggleRow("Disable Weapon Collision", ref weapCol, "Prevent weapon from folding when near walls"))
            {
                Config.MemWrites.DisableWeaponCollision = weapCol;
                Config.MarkDirty();
            }

            // ═══════════════════════════════════════════════════════════════
            // Movement
            // ═══════════════════════════════════════════════════════════════
            UIControls.Section("Movement");

            bool infStamina = Config.MemWrites.InfStamina;
            if (UIControls.ToggleRow("Infinite Stamina", ref infStamina, "Refill stamina and oxygen when they drop below 33%"))
            {
                Config.MemWrites.InfStamina = infStamina;
                Config.MarkDirty();
            }

            bool fastDuck = Config.MemWrites.FastDuck;
            if (UIControls.ToggleRow("Fast Duck", ref fastDuck, "Instant crouch/stand transitions"))
            {
                Config.MemWrites.FastDuck = fastDuck;
                Config.MarkDirty();
            }

            bool noInertia = Config.MemWrites.NoInertia;
            if (UIControls.ToggleRow("No Inertia", ref noInertia, "Remove movement inertia for instant direction changes"))
            {
                Config.MemWrites.NoInertia = noInertia;
                Config.MarkDirty();
            }

            bool mule = Config.MemWrites.MuleMode;
            if (UIControls.ToggleRow("M.U.L.E Mode", ref mule, "Remove overweight penalties (movement, sprint, inertia)"))
            {
                Config.MemWrites.MuleMode = mule;
                Config.MarkDirty();
            }

            bool wideLean = Config.MemWrites.WideLean.Enabled;
            if (UIControls.ToggleRow("Wide Lean", ref wideLean))
            {
                Config.MemWrites.WideLean.Enabled = wideLean;
                Config.MarkDirty();
            }
            if (wideLean)
            {
                ImGui.Indent(16);
                float wlAmt = Config.MemWrites.WideLean.Amount;
                if (UIControls.StepperFloat("Amount", ref wlAmt, 0.1f, 5f, 0.1f, "{0:0.0}",
                    "Lean offset amount (higher = wider lean)"))
                {
                    Config.MemWrites.WideLean.Amount = wlAmt;
                    Config.MarkDirty();
                }

                int dirIdx = (int)WideLean.Direction;
                if (UIControls.ComboRow("Direction", ref dirIdx, _wideLeanDirNames,
                    "Lean axis: Off / Left / Right / Up"))
                {
                    WideLean.Direction = (WideLean.EWideLeanDirection)dirIdx;
                }
                ImGui.Unindent(16);
            }

            bool longJump = Config.MemWrites.LongJump.Enabled;
            if (UIControls.ToggleRow("Long Jump", ref longJump))
            {
                Config.MemWrites.LongJump.Enabled = longJump;
                Config.MarkDirty();
            }
            if (longJump)
            {
                ImGui.Indent(16);
                float ljMult = Config.MemWrites.LongJump.Multiplier;
                if (UIControls.StepperFloat("Multiplier", ref ljMult, 1f, 10f, 0.5f, "{0:0.0}x",
                    "Air control multiplier (higher = longer jumps)"))
                {
                    Config.MemWrites.LongJump.Multiplier = ljMult;
                    Config.MarkDirty();
                }
                ImGui.Unindent(16);
            }

            bool moveSpeed = Config.MemWrites.MoveSpeed.Enabled;
            if (UIControls.ToggleRow("Move Speed", ref moveSpeed))
            {
                Config.MemWrites.MoveSpeed.Enabled = moveSpeed;
                Config.MarkDirty();
            }
            if (moveSpeed)
            {
                ImGui.Indent(16);
                float mult = Config.MemWrites.MoveSpeed.Multiplier;
                if (UIControls.StepperFloat("Multiplier", ref mult, 0.5f, 3.0f, 0.05f, "{0:0.00}x",
                    "Animator speed multiplier (1.0 = normal, disabled when overweight)"))
                {
                    Config.MemWrites.MoveSpeed.Multiplier = mult;
                    Config.MarkDirty();
                }
                ImGui.Unindent(16);
            }

            // ═══════════════════════════════════════════════════════════════
            // World
            // ═══════════════════════════════════════════════════════════════
            UIControls.Section("World");

            bool fb = Config.MemWrites.FullBright.Enabled;
            if (UIControls.ToggleRow("Full Bright", ref fb))
            {
                Config.MemWrites.FullBright.Enabled = fb;
                Config.MarkDirty();
            }
            if (fb)
            {
                ImGui.Indent(16);
                float brightness = Config.MemWrites.FullBright.Brightness;
                if (UIControls.StepperFloat("Brightness", ref brightness, 0f, 2f, 0.05f, "{0:0.00}",
                    "Ambient light intensity (1.0 = full white)"))
                {
                    Config.MemWrites.FullBright.Brightness = brightness;
                    Config.MarkDirty();
                }
                ImGui.Unindent(16);
            }

            bool reach = Config.MemWrites.ExtendedReach.Enabled;
            if (UIControls.ToggleRow("Extended Reach", ref reach))
            {
                Config.MemWrites.ExtendedReach.Enabled = reach;
                Config.MarkDirty();
            }
            if (reach)
            {
                ImGui.Indent(16);
                float reachDist = Config.MemWrites.ExtendedReach.Distance;
                if (UIControls.StepperFloat("Distance", ref reachDist, 1f, 20f, 0.5f, "{0:0.0}m",
                    "Loot/door interaction distance (default ~1.3m)"))
                {
                    Config.MemWrites.ExtendedReach.Distance = reachDist;
                    Config.MarkDirty();
                }
                ImGui.Unindent(16);
            }

            // ═══════════════════════════════════════════════════════════════
            // Camera
            // ═══════════════════════════════════════════════════════════════
            UIControls.Section("Camera");

            bool noVisor = Config.MemWrites.NoVisor;
            if (UIControls.ToggleRow("No Visor", ref noVisor, "Remove visor overlay effect (e.g. face shield darkening)"))
            {
                Config.MemWrites.NoVisor = noVisor;
                Config.MarkDirty();
            }

            bool nv = Config.MemWrites.NightVision;
            if (UIControls.ToggleRow("Night Vision", ref nv, "Force NightVision component on (no NVG required)"))
            {
                Config.MemWrites.NightVision = nv;
                Config.MarkDirty();
            }

            bool thermal = Config.MemWrites.ThermalVision;
            if (UIControls.ToggleRow("Thermal Vision", ref thermal, "Force ThermalVision component on (auto-disables while ADS)"))
            {
                Config.MemWrites.ThermalVision = thermal;
                Config.MarkDirty();
            }

            bool thirdPerson = Config.MemWrites.ThirdPerson;
            if (UIControls.ToggleRow("Third Person", ref thirdPerson, "Move camera behind player for third-person view"))
            {
                Config.MemWrites.ThirdPerson = thirdPerson;
                Config.MarkDirty();
            }

            bool owl = Config.MemWrites.OwlMode;
            if (UIControls.ToggleRow("Owl Mode", ref owl, "Remove mouse look limits (360° head rotation)"))
            {
                Config.MemWrites.OwlMode = owl;
                Config.MarkDirty();
            }

            bool frostbite = Config.MemWrites.DisableFrostbite;
            if (UIControls.ToggleRow("Disable Frostbite", ref frostbite, "Remove frostbite screen overlay effect"))
            {
                Config.MemWrites.DisableFrostbite = frostbite;
                Config.MarkDirty();
            }

            // ═══════════════════════════════════════════════════════════════
            // Misc
            // ═══════════════════════════════════════════════════════════════
            UIControls.Section("Misc");

            bool instantPlant = Config.MemWrites.InstantPlant;
            if (UIControls.ToggleRow("Instant Plant", ref instantPlant, "Near-instant planting (e.g. quest items)"))
            {
                Config.MemWrites.InstantPlant = instantPlant;
                Config.MarkDirty();
            }

            bool medPanel = Config.MemWrites.MedPanel;
            if (UIControls.ToggleRow("Med Panel", ref medPanel, "Show med effect using panel (health effects UI)"))
            {
                Config.MemWrites.MedPanel = medPanel;
                Config.MarkDirty();
            }

            bool invBlur = Config.MemWrites.DisableInventoryBlur;
            if (UIControls.ToggleRow("Disable Inventory Blur", ref invBlur, "Remove background blur when inventory is open"))
            {
                Config.MemWrites.DisableInventoryBlur = invBlur;
                Config.MarkDirty();
            }

            if (!masterEnabled)
                ImGui.EndDisabled();
        }
    }
}
