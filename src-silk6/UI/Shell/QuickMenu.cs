// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using System.Numerics;
using eft_dma_radar.Silk6.Config;
using eft_dma_radar.Silk6.UI.ESP;
using ImGuiNET;

namespace eft_dma_radar.Silk6.UI.Shell
{
    /// <summary>
    /// Controller / AnyDesk-friendly radial quick menu. Bind <c>QuickMenuOpen</c>
    /// in the Hotkeys panel (e.g. Q on keyboard, LB on controller). While the
    /// hotkey is held the radial appears centered on the screen with 8 slices
    /// for the most-used in-raid toggles. Each slice is a big circular hit
    /// target so it works through AnyDesk and on a TV.
    ///
    /// Activation model:
    ///   - <see cref="Open"/>: show the radial, latch the currently hovered slice.
    ///   - <see cref="Close"/>: if a slice is hovered when closed, invoke it.
    /// This mirrors how a controller LB-hold radial feels: hold, point, release.
    /// </summary>
    internal static class QuickMenu
    {
        public static bool IsOpen { get; private set; }

        private sealed record Slice(string Label, string Tooltip, Action Toggle, Func<bool> IsActive);

        private static readonly Slice[] _slices =
        [
            new("Battle",   "Battle Mode",        () => SilkProgram.Config.SetBattleMode(!SilkProgram.Config.BattleMode), () => SilkProgram.Config.BattleMode),
            new("Aim",      "Aimlines",           () => SilkProgram.Config.ShowAimlines = !SilkProgram.Config.ShowAimlines, () => SilkProgram.Config.ShowAimlines),
            new("Loot",     "Loot Overlay",       () => SilkProgram.Config.ShowLoot = !SilkProgram.Config.ShowLoot,         () => SilkProgram.Config.ShowLoot),
            new("Alert",    "High Alert",         () => SilkProgram.Config.HighAlert = !SilkProgram.Config.HighAlert,       () => SilkProgram.Config.HighAlert),
            new("Groups",   "Connect Groups",     () => SilkProgram.Config.ConnectGroups = !SilkProgram.Config.ConnectGroups, () => SilkProgram.Config.ConnectGroups),
            new("Exfils",   "Exfils",             () => SilkProgram.Config.ShowExfils = !SilkProgram.Config.ShowExfils,     () => SilkProgram.Config.ShowExfils),
            new("Doors",    "Doors",              () => SilkProgram.Config.ShowDoors = !SilkProgram.Config.ShowDoors,       () => SilkProgram.Config.ShowDoors),
            new("Drops",    "Airdrops",           () => SilkProgram.Config.ShowAirdrops = !SilkProgram.Config.ShowAirdrops, () => SilkProgram.Config.ShowAirdrops),
        ];

        public static void Open()
        {
            IsOpen = true;
        }

        public static void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            if (_hoveredIndex >= 0 && _hoveredIndex < _slices.Length)
            {
                _slices[_hoveredIndex].Toggle();
                SilkProgram.Config.MarkDirty();
            }
            _hoveredIndex = -1;
        }

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        private static int _hoveredIndex = -1;

        public static void Draw()
        {
            if (!IsOpen)
                return;

            var io = ImGui.GetIO();
            var viewport = ImGui.GetMainViewport();
            var center = viewport.GetCenter();
            var scale = SilkProgram.Config.UIScale;
            float outer = 180f * scale;
            float inner = 70f * scale;

            ImGui.SetNextWindowPos(Vector2.Zero);
            ImGui.SetNextWindowSize(io.DisplaySize);
            ImGui.SetNextWindowBgAlpha(0.55f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                           | ImGuiWindowFlags.NoResize
                                           | ImGuiWindowFlags.NoMove
                                           | ImGuiWindowFlags.NoScrollbar
                                           | ImGuiWindowFlags.NoSavedSettings
                                           | ImGuiWindowFlags.NoInputs
                                           | ImGuiWindowFlags.NoFocusOnAppearing
                                           | ImGuiWindowFlags.NoNav;

            if (ImGui.Begin("##quickmenu", flags))
            {
                var dl = ImGui.GetWindowDrawList();
                var mouse = io.MousePos;
                var dir = mouse - center;
                float dist = dir.Length();

                // Pick hovered slice based on angle (only if inside outer ring).
                _hoveredIndex = -1;
                if (dist >= inner * 0.6f && dist <= outer * 1.4f)
                {
                    float angle = MathF.Atan2(dir.Y, dir.X);
                    // Normalize so slice 0 is at the top (-PI/2) and rotation is clockwise.
                    float normalized = angle + MathF.PI / 2f;
                    if (normalized < 0) normalized += MathF.Tau;
                    if (normalized >= MathF.Tau) normalized -= MathF.Tau;
                    _hoveredIndex = (int)(normalized / (MathF.Tau / _slices.Length)) % _slices.Length;
                }

                // Backplate
                dl.AddCircleFilled(center, outer, ImGui.GetColorU32(new Vector4(0.06f, 0.06f, 0.08f, 0.85f)), 64);
                dl.AddCircle(center, outer, ImGui.GetColorU32(new Vector4(0.20f, 0.85f, 1.00f, 0.55f)), 64, 2f);

                int n = _slices.Length;
                float step = MathF.Tau / n;
                float labelRadius = (outer + inner) * 0.5f;
                var accent = new Vector4(0.20f, 0.85f, 1.00f, 1.0f);

                for (int i = 0; i < n; i++)
                {
                    float a0 = -MathF.PI / 2f + step * i - step * 0.5f;
                    float a1 = a0 + step;
                    bool hovered = i == _hoveredIndex;
                    bool active = _slices[i].IsActive();
                    var fill = hovered
                        ? new Vector4(accent.X, accent.Y, accent.Z, 0.35f)
                        : active
                            ? new Vector4(accent.X, accent.Y, accent.Z, 0.18f)
                            : new Vector4(0.14f, 0.15f, 0.17f, 0.85f);

                    // Wedge using a triangle fan.
                    const int segs = 12;
                    var p0 = center;
                    var prev = center + new Vector2(MathF.Cos(a0), MathF.Sin(a0)) * outer;
                    for (int s = 1; s <= segs; s++)
                    {
                        float t = s / (float)segs;
                        float a = a0 + (a1 - a0) * t;
                        var cur = center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * outer;
                        dl.AddTriangleFilled(p0, prev, cur, ImGui.GetColorU32(fill));
                        prev = cur;
                    }

                    // Knock out the inner hole.
                    // (Drawn last per-slice would be expensive; we just overlay one
                    //  big disk after the wedge loop.)

                    float ang = -MathF.PI / 2f + step * i;
                    var labelPos = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * labelRadius;
                    var label = _slices[i].Label;
                    var size = ImGui.CalcTextSize(label);
                    var textColor = hovered || active ? Vector4.One : new Vector4(0.85f, 0.87f, 0.90f, 1.0f);
                    dl.AddText(labelPos - size * 0.5f, ImGui.GetColorU32(textColor), label);

                    // Active dot indicator
                    if (active)
                    {
                        var dot = center + new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * (outer - 14f * scale);
                        dl.AddCircleFilled(dot, 4f * scale, ImGui.GetColorU32(accent), 12);
                    }
                }

                // Inner hole + center label
                dl.AddCircleFilled(center, inner, ImGui.GetColorU32(new Vector4(0.06f, 0.06f, 0.08f, 1.0f)), 48);
                dl.AddCircle(center, inner, ImGui.GetColorU32(new Vector4(0.20f, 0.85f, 1.00f, 0.40f)), 48, 1.5f);

                string title = "Quick Menu";
                string subtitle = _hoveredIndex >= 0 ? _slices[_hoveredIndex].Tooltip : "release to confirm";
                var titleSize = ImGui.CalcTextSize(title);
                var subSize = ImGui.CalcTextSize(subtitle);
                dl.AddText(center - new Vector2(titleSize.X * 0.5f, titleSize.Y + 2f),
                    ImGui.GetColorU32(new Vector4(0.20f, 0.85f, 1.00f, 1.0f)), title);
                dl.AddText(center - new Vector2(subSize.X * 0.5f, -2f),
                    ImGui.GetColorU32(new Vector4(0.80f, 0.82f, 0.85f, 1.0f)), subtitle);
            }
            ImGui.End();
            ImGui.PopStyleVar(3);
        }
    }
}
