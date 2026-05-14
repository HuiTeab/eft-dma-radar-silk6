// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using System.Numerics;
using ImGuiNET;

namespace eft_dma_radar.Silk6.UI.Shell
{
    /// <summary>
    /// Keyboard-only command palette (Ctrl+K). Lists every registered hotkey
    /// action (and a few synthetic "open panel" entries) and lets the user
    /// fuzzy-search by name and invoke with Enter. This is the power-user
    /// fallback to the controller / sidebar / radial flows.
    /// </summary>
    internal static class CommandPalette
    {
        public static bool IsOpen { get; private set; }

        private static string _query = string.Empty;
        private static int _selected;
        private static bool _focusInputNextFrame;

        private sealed record Command(string Title, string Subtitle, Action Run);

        private static readonly Command[] _builtins =
        [
            new("Open Settings",      "Panel",  static () => Panels.SettingsPanel.IsOpen = true),
            new("Open Hotkeys",       "Panel",  static () => Panels.HotkeyManagerPanel.IsOpen = true),
            new("Open Loot Filters",  "Panel",  static () => Panels.LootFiltersPanel.IsOpen = true),
            new("Open Killfeed",      "Panel",  static () => Panels.KillfeedPanel.IsOpen = true),
            new("Open Player History","Panel",  static () => Panels.PlayerHistoryPanel.IsOpen = true),
            new("Open Watchlist",     "Panel",  static () => Panels.PlayerWatchlistPanel.IsOpen = true),
            new("Open Quest Planner", "Panel",  static () => Panels.QuestPlannerPanel.IsOpen = true),
            new("Open Hideout",       "Panel",  static () => Panels.HideoutPanel.IsOpen = true),
            new("Toggle Sidebar",     "Layout", static () => Sidebar.ToggleVisibility()),
            new("Toggle Side Dock",   "Layout", static () => { var c = SilkProgram.Config; c.DockSidePanels = !c.DockSidePanels; c.MarkDirty(); }),
            new("Reset Side Panels",  "Layout", static () => RightDock.RequestResnap()),
            new("Show Welcome Tour",  "Help",   static () => FirstRunTour.Open()),
        ];

        public static void Open()
        {
            IsOpen = true;
            _query = string.Empty;
            _selected = 0;
            _focusInputNextFrame = true;
        }

        public static void Close()
        {
            IsOpen = false;
            _query = string.Empty;
            _selected = 0;
        }

        /// <summary>
        /// Call once per frame. Detects Ctrl+K via ImGui input so it works
        /// whenever the main window has focus, independent of <see cref="Misc.Input.InputManager"/>.
        /// </summary>
        public static void Update()
        {
            var io = ImGui.GetIO();
            if (!IsOpen && io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.K, false))
                Open();
            else if (IsOpen && ImGui.IsKeyPressed(ImGuiKey.Escape, false))
                Close();
        }

        public static void Draw()
        {
            if (!IsOpen)
                return;

            var io = ImGui.GetIO();
            var viewport = ImGui.GetMainViewport();
            var center = viewport.GetCenter();
            float scale = SilkProgram.Config.UIScale;
            var size = new Vector2(560f * scale, 440f * scale);
            var pos = center - size * 0.5f;

            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(size, ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.96f);

            const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                           | ImGuiWindowFlags.NoResize
                                           | ImGuiWindowFlags.NoMove
                                           | ImGuiWindowFlags.NoCollapse
                                           | ImGuiWindowFlags.NoSavedSettings;

            if (ImGui.Begin("##commandpalette", flags))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.20f, 0.85f, 1.00f, 1.0f));
                ImGui.TextUnformatted("Command Palette");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.TextDisabled("  (Ctrl+K · Esc to close · ↑/↓ to navigate · Enter to run)");
                ImGui.Separator();

                if (_focusInputNextFrame)
                {
                    ImGui.SetKeyboardFocusHere();
                    _focusInputNextFrame = false;
                }
                ImGui.SetNextItemWidth(-1f);
                ImGui.InputTextWithHint("##q", "Type to filter…", ref _query, 128);

                var matches = BuildMatches(_query);

                if (ImGui.IsKeyPressed(ImGuiKey.DownArrow, true))
                    _selected = Math.Min(_selected + 1, Math.Max(0, matches.Count - 1));
                if (ImGui.IsKeyPressed(ImGuiKey.UpArrow, true))
                    _selected = Math.Max(_selected - 1, 0);
                bool runNow = ImGui.IsKeyPressed(ImGuiKey.Enter, false)
                              || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter, false);

                ImGui.BeginChild("##cmd-list", new Vector2(0, 0), ImGuiChildFlags.Borders);
                for (int i = 0; i < matches.Count; i++)
                {
                    var (title, subtitle, run) = matches[i];
                    bool isSel = i == _selected;
                    if (ImGui.Selectable($"{title}##cmd{i}", isSel, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        _selected = i;
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                            runNow = true;
                    }
                    ImGui.SameLine();
                    ImGui.TextDisabled($"  {subtitle}");
                    if (isSel)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndChild();

                if (runNow && _selected >= 0 && _selected < matches.Count)
                {
                    try { matches[_selected].Run(); }
                    catch (Exception ex) { Log.WriteLine($"[CommandPalette] '{matches[_selected].Title}' error: {ex.Message}"); }
                    SilkProgram.Config.MarkDirty();
                    Close();
                }
            }
            ImGui.End();
        }

        private static List<Command> BuildMatches(string query)
        {
            var all = new List<Command>(_builtins.Length + Misc.Input.HotkeyManager.AvailableActions.Length);
            all.AddRange(_builtins);

            foreach (var def in Misc.Input.HotkeyManager.AvailableActions)
            {
                var local = def;
                all.Add(new Command(local.DisplayName, local.Category, () => local.Handler(new Misc.Input.InputManager.KeyInputEventArgs(0, true))));
            }

            if (string.IsNullOrWhiteSpace(query))
                return all;

            var q = query.Trim();
            return all
                .Select(c => (cmd: c, score: Score(c, q)))
                .Where(t => t.score > 0)
                .OrderByDescending(t => t.score)
                .Select(t => t.cmd)
                .ToList();
        }

        private static int Score(Command cmd, string query)
        {
            int score = 0;
            if (cmd.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 100;
            if (cmd.Subtitle.Contains(query, StringComparison.OrdinalIgnoreCase)) score += 30;

            // Subsequence match (very small fuzzy bonus).
            int qi = 0;
            foreach (var ch in cmd.Title)
            {
                if (qi < query.Length && char.ToLowerInvariant(ch) == char.ToLowerInvariant(query[qi]))
                    qi++;
            }
            if (qi == query.Length) score += 10;
            return score;
        }
    }
}
