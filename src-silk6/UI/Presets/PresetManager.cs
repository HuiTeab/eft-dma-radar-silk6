// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using eft_dma_radar.Silk6.Config;
using System.Linq;

namespace eft_dma_radar.Silk6.UI.Presets
{
    /// <summary>
    /// A bundle of radar-layer and player-display toggles that can be applied
    /// in one click / hotkey. Designed for controller / AnyDesk / web users who
    /// shouldn't have to micromanage dozens of options every raid.
    ///
    /// Presets only touch UI/visibility flags. They never touch memory writes,
    /// DMA settings, hotkey bindings, or window geometry.
    /// </summary>
    internal sealed class RadarPreset
    {
        public string Id { get; init; } = "";
        public string DisplayName { get; init; } = "";

        // Radar layer toggles
        public bool BattleMode { get; init; }
        public bool ShowLoot { get; init; } = true;
        public bool ShowCorpses { get; init; } = true;
        public bool ShowContainers { get; init; } = true;
        public bool ShowExfils { get; init; } = true;
        public bool ShowDoors { get; init; } = true;
        public bool ShowAirdrops { get; init; } = true;
        public bool ShowSwitches { get; init; } = true;
        public bool ShowTransits { get; init; } = true;

        // Player display
        public bool ShowAimlines { get; init; } = true;
        public bool ConnectGroups { get; init; } = true;
        public bool HighAlert { get; init; } = true;
        public bool PlayersOnTop { get; init; } = false;
    }

    /// <summary>
    /// Built-in presets + application logic. Presets are pure data; the active
    /// preset id lives in <see cref="SilkConfig.ActivePresetId"/>.
    /// </summary>
    internal static class PresetManager
    {
        public const string CustomId = "Custom";

        /// <summary>
        /// Built-in presets, ordered for the top-bar selector and hotkey cycling.
        /// "Custom" is appended at runtime so user tweaks always have a slot.
        ///
        /// Each preset is intentionally different from the others — Stealth and
        /// PvP previously shared identical toggles, which made cycling between
        /// them feel like a no-op. The four presets now lean into distinct
        /// playstyles: Stealth = silent escape, Loot Run = max info, PvP =
        /// hunter focus, Quests = objective focus.
        /// </summary>
        public static readonly RadarPreset[] BuiltIn =
        [
            // ── Stealth ─────────────────────────────────────────────────────
            // Silent extract: clean map, only what's needed to evade and exfil.
            // No loot / corpses / containers (clutter). Doors / switches off
            // because keyed routes are usually pre-planned in this mode.
            new RadarPreset
            {
                Id = "Stealth",
                DisplayName = "Stealth",
                BattleMode = true,
                ShowLoot = false,
                ShowCorpses = false,
                ShowContainers = false,
                ShowExfils = true,
                ShowDoors = false,
                ShowAirdrops = false,
                ShowSwitches = false,
                ShowTransits = true,
                ShowAimlines = true,
                ConnectGroups = true,
                HighAlert = true,
                PlayersOnTop = true,
            },
            // ── Loot Run ───────────────────────────────────────────────────
            // Max info: every world entity visible, players don't float on top
            // (so you can read the loot underneath), HighAlert off so the long
            // red aimlines don't drown out the loot dots.
            new RadarPreset
            {
                Id = "LootRun",
                DisplayName = "Loot Run",
                BattleMode = false,
                ShowLoot = true,
                ShowCorpses = true,
                ShowContainers = true,
                ShowExfils = true,
                ShowDoors = true,
                ShowAirdrops = true,
                ShowSwitches = true,
                ShowTransits = true,
                ShowAimlines = true,
                ConnectGroups = true,
                HighAlert = false,
                PlayersOnTop = false,
            },
            // ── PvP ─────────────────────────────────────────────────────────
            // Hunter mode: BattleMode hides non-combat clutter, but we keep
            // corpses (recent kills = active engagement zones) and airdrops
            // (PvP magnets) and doors (key-room fights). Players draw on top
            // so combat callouts are always readable.
            new RadarPreset
            {
                Id = "PvP",
                DisplayName = "PvP",
                BattleMode = true,
                ShowLoot = false,
                ShowCorpses = true,
                ShowContainers = false,
                ShowExfils = true,
                ShowDoors = true,
                ShowAirdrops = true,
                ShowSwitches = false,
                ShowTransits = false,
                ShowAimlines = true,
                ConnectGroups = true,
                HighAlert = true,
                PlayersOnTop = true,
            },
            // ── Quests ──────────────────────────────────────────────────────
            // Objective focus: loot on (quest items), doors / switches on
            // (mechanics), corpses + containers off (visual noise that doesn't
            // help objectives). Airdrops off because they pull people away.
            new RadarPreset
            {
                Id = "Quests",
                DisplayName = "Quests",
                BattleMode = false,
                ShowLoot = true,
                ShowCorpses = false,
                ShowContainers = false,
                ShowExfils = true,
                ShowDoors = true,
                ShowAirdrops = false,
                ShowSwitches = true,
                ShowTransits = true,
                ShowAimlines = true,
                ConnectGroups = true,
                HighAlert = true,
                PlayersOnTop = false,
            },
        ];

        /// <summary>All selectable preset ids in cycle order (built-in + Custom).</summary>
        public static IReadOnlyList<string> AllIds { get; } =
            [.. BuiltIn.Select(p => p.Id), CustomId];

        /// <summary>Returns the built-in preset with the given id, or null.</summary>
        public static RadarPreset? FindBuiltIn(string id)
            => BuiltIn.FirstOrDefault(p => p.Id == id);

        /// <summary>Display name for any id, including "Custom".</summary>
        public static string DisplayNameFor(string id)
            => id == CustomId ? "Custom" : (FindBuiltIn(id)?.DisplayName ?? id);

        /// <summary>
        /// Applies the named preset to the given config and persists it.
        /// "Custom" is a no-op (it represents the user's current tweaks).
        /// </summary>
        public static void Apply(string id, SilkConfig cfg)
        {
            if (id == CustomId)
            {
                cfg.ActivePresetId = CustomId;
                cfg.MarkDirty();
                return;
            }

            var preset = FindBuiltIn(id);
            if (preset is null)
                return;

            cfg.BattleMode = preset.BattleMode;
            cfg.ShowLoot = preset.ShowLoot;
            cfg.ShowCorpses = preset.ShowCorpses;
            cfg.ShowContainers = preset.ShowContainers;
            cfg.ShowExfils = preset.ShowExfils;
            cfg.ShowDoors = preset.ShowDoors;
            cfg.ShowAirdrops = preset.ShowAirdrops;
            cfg.ShowSwitches = preset.ShowSwitches;
            cfg.ShowTransits = preset.ShowTransits;
            cfg.ShowAimlines = preset.ShowAimlines;
            cfg.ConnectGroups = preset.ConnectGroups;
            cfg.HighAlert = preset.HighAlert;
            cfg.PlayersOnTop = preset.PlayersOnTop;
            cfg.ActivePresetId = preset.Id;
            cfg.MarkDirty();
            eft_dma_radar.Silk6.UI.Shell.ToastManager.Info($"Preset: {preset.DisplayName}");
        }

        /// <summary>
        /// Cycle the active preset by <paramref name="delta"/> slots
        /// (e.g. +1 for next, -1 for previous). Wraps around.
        /// </summary>
        public static void Cycle(int delta, SilkConfig cfg)
        {
            var ids = AllIds;
            int idx = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == cfg.ActivePresetId)
                {
                    idx = i;
                    break;
                }
            }
            int next = ((idx + delta) % ids.Count + ids.Count) % ids.Count;
            Apply(ids[next], cfg);
        }

        /// <summary>
        /// Returns true if the current config toggles still match the active
        /// built-in preset. If they don't, the user has drifted into "Custom".
        /// </summary>
        public static bool MatchesActive(SilkConfig cfg)
        {
            if (cfg.ActivePresetId == CustomId)
                return true;

            var p = FindBuiltIn(cfg.ActivePresetId);
            if (p is null)
                return false;

            return cfg.BattleMode == p.BattleMode
                && cfg.ShowLoot == p.ShowLoot
                && cfg.ShowCorpses == p.ShowCorpses
                && cfg.ShowContainers == p.ShowContainers
                && cfg.ShowExfils == p.ShowExfils
                && cfg.ShowDoors == p.ShowDoors
                && cfg.ShowAirdrops == p.ShowAirdrops
                && cfg.ShowSwitches == p.ShowSwitches
                && cfg.ShowTransits == p.ShowTransits
                && cfg.ShowAimlines == p.ShowAimlines
                && cfg.ConnectGroups == p.ConnectGroups
                && cfg.HighAlert == p.HighAlert
                && cfg.PlayersOnTop == p.PlayersOnTop;
        }

        /// <summary>
        /// Demotes the active preset to "Custom" if the user has drifted
        /// from the built-in defaults. Called once per frame from the UI.
        /// </summary>
        public static void ReconcileDrift(SilkConfig cfg)
        {
            if (cfg.ActivePresetId == CustomId)
                return;
            if (!MatchesActive(cfg))
            {
                cfg.ActivePresetId = CustomId;
                cfg.MarkDirty();
            }
        }
    }
}
