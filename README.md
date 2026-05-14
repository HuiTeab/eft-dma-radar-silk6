# EFT DMA Radar — Silk.NET Edition (Unity 6)

A modern DMA (Direct Memory Access) radar overlay for **Escape from Tarkov** (Unity 6000.3.6f1 EFT build), built on [Silk.NET](https://github.com/dotnet/Silk.NET) (Windowing / Input / OpenGL), [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) panels, and [SkiaSharp](https://github.com/mono/SkiaSharp) 2D rendering. Ships with an embedded ASP.NET Core web radar for browser / phone / tablet buddies.

> **Targeting the Unity 2022 EFT build?** See the sibling repo [**eft-dma-radar-silk**](https://github.com/HuiTeab/eft-dma-radar-silk) — same UI, same web radar, same features; Unity 2022.3.43f1 engine offsets.

This is a Unity 6 fork of [`eft-dma-radar-silk`](https://github.com/HuiTeab/eft-dma-radar-silk). All feature code (UI shell, presets, web radar, loot filters, satellite map, BTR tracking, killfeed, quests, etc.) carries over unchanged from silk; only the Unity-engine touch points (struct layouts, SDK offsets, transform-chain hops, IL2CPP runtime patterns) differ. See [`src-silk6/Docs/UNITY_ENGINE_CHANGES.md`](src-silk6/Docs/UNITY_ENGINE_CHANGES.md) for the full Unity 2022 → Unity 6 delta.

The `src-silk6/` codebase is an **original work written from scratch by [HuiTeab](https://github.com/HuiTeab)**. The only third-party code in this repository is `lib/VmmSharpEx/` — a separately-licensed (AGPL-3.0) wrapper around [MemProcFS](https://github.com/ufrisk/MemProcFS), included unmodified-in-attribution as part of the radar's DMA stack. See [LICENSE](LICENSE) for the full license breakdown.

---

## Repo Layout

```
eft-dma-radar-silk6/
├── eft-dma-radar-silk6.sln      # Visual Studio solution (VmmSharpEx + src-silk6)
├── Directory.Build.props         # Common MSBuild props (net10.0-windows, x64, unsafe)
├── version.json                  # Nerdbank.GitVersioning version source
├── LICENSE                       # PolyForm Noncommercial License 1.0.0
├── Maps/                         # EFT map SVGs + JSON metadata (Customs, Streets, …)
├── Resources/                    # Embedded font + default item DB
├── lib/
│   └── VmmSharpEx/               # Managed MemProcFS / LeechCore wrapper + native DLLs
├── docs/
│   └── UX_MODERNIZATION_PLAN.md  # Phase-by-phase modernization log
└── src-silk6/                    # The radar itself (entry: Program.cs → SilkProgram.Main)
    └── Docs/
        ├── UNITY_ENGINE_CHANGES.md   # Unity 2022 → Unity 6 reference
        ├── DEBUG_OUTPUT_REFERENCE.md
        └── MIGRATION_ROADMAP.md
```

---

## Requirements

- **DMA hardware** supported by [MemProcFS](https://github.com/ufrisk/MemProcFS) (FPGA card, `usb3380`, etc.)
- **Windows 10 / 11 (x64)** — project targets `net10.0-windows`, `PlatformTarget=x64`
- **[.NET 10 SDK / Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)**
- **Visual Studio 2022 17.12+** (or 2026 Insiders) with the **.NET desktop development** workload
- **EFT on the Unity 6 build** (currently 6000.3.6f1 → 6000.3.14f1 confirmed). For Unity 2022 builds use the [silk](https://github.com/HuiTeab/eft-dma-radar-silk) sibling.
- The native MemProcFS binaries (`vmm.dll`, `leechcore.dll`, `FTD3XX.dll`, …) ship under `lib/VmmSharpEx/native/` and are copied to the build output automatically.

---

## Build & Run

```powershell
git clone https://github.com/HuiTeab/eft-dma-radar-silk6.git
cd eft-dma-radar-silk6

# Build (Release, x64)
dotnet build eft-dma-radar-silk6.sln -c Release

# Run
dotnet run --project src-silk6\eft-dma-radar-silk6.csproj -c Release
```

Pass `-debug` on the command line (or set `debugLogging=true` in the config) for verbose startup logging.

In Visual Studio: open `eft-dma-radar-silk6.sln`, set `eft-dma-radar-silk6` as the startup project, press **F5**.

---

## Highlights

Same surface area as the silk variant — UI shell, presets, web radar, loot filters, satellite map, BTR, killfeed, quests, ESP. Below is a quick recap; see the silk README for full feature descriptions.

**Desktop shell** — icon sidebar, top command bar, big-chip status bar, radial quick menu (hold-to-open), command palette (`Ctrl+K`), toasts, first-run tour, configurable hotkeys.

**Presets** — Stealth · Loot Run · PvP · Quests · Custom; 13 toggles each; auto-demote-to-Custom drift detection.

**Loot Filters** — Quick View chips (All Loot · Important+ · Wishlist · Quest), live `visible / total` counter.

**Web radar** (`src-silk6/Web/wwwroot/`) — mobile-first bottom tab bar, FAB radial, follow-me default with double-tap recenter, pinch-to-zoom, independent web presets (Spotter · Battle Buddy · Loot Hunter · Quest Helper · Custom).

**Map rendering** — SVG layers with height-aware overlays, **satellite imagery** via assets.tarkov.dev tile pyramid (Customs, Interchange, Reserve, Shoreline, Woods, Ground Zero), tile cache at `%LocalAppData%\eft-dma-radar-silk6\tilecache`, independent desktop and web toggles, server-side proxy at `/api/tile/{cacheKey}/{z}/{x}/{y}.png` to bypass browser CORS.

**Config**
- `%AppData%\eft-dma-radar-silk6\config.json` — debounced JSON persistence.
- IL2CPP offsets resolved at startup and cached to `il2cpp_offsets.json`; hard-coded fallbacks live in `src-silk6/SDK/Offsets.cs`.

---

## Unity 6 specifics

The Unity 6 engine changed enough internal struct layouts and IL2CPP pointer semantics that several runtime touchpoints had to be re-derived from scratch. The authoritative reference is [`src-silk6/Docs/UNITY_ENGINE_CHANGES.md`](src-silk6/Docs/UNITY_ENGINE_CHANGES.md). High-level summary:

- **GameObject / Component offsets** shifted: `GO_Components` `0x58 → 0x38`, `GO_Name` `0x88 → 0x68`, `Comp_GameObject` `0x58 → 0x38`, `Comp_ObjectClass` `0x20 → 0x28`. The old `GO_ObjectClass` (`+0x80`) field was removed entirely in Unity 6 — all GameObject ↔ ObjectClass navigation now goes via the Component side.
- **TransformAccess / TransformHierarchy offsets** shifted: `TA.HierarchyOffset` `0x70 → 0x40`, `TA.IndexOffset` `0x78 → 0x48`, `TH.VerticesOffset` `0x68 → 0x88`, `TH.IndicesOffset` `0x40 → 0x70`.
- **GameObjectManager (GOM) struct** rewritten — the sentinel `LinkedListObject` is now embedded inline at `GOM+0x18`; `FirstNode` / `LastNode` replace the old `ActiveNodes` / `LastActiveNode` fields; `FindBehaviourByKlassPtr` / `FindBehaviourByClassName` / `GetGameObjectByName` are now static methods that read `Memory.GOM` on each call.
- **`Comp_ObjectClass` semantics changed**: on Unity 6 `Component+0x28` stores an IL2CPP **GCHandle index** (not a raw pointer). Old loot/scene-object chains that did `MonoBehaviour + Comp_ObjectClass → +0x10` to reach a managed wrapper now land on garbage. Loot, containers, corpses, airdrops, doors, switches, exfils, and grenades use a shortened native chain instead: `ic → +0x10 → +Comp_GameObject → +GO_Components → +0x08` (the last hop is **already** the native `TransformInternal` on Unity 6).
- **Player transform init** uses a **Path A / Path B managed-wrapper probe** — the pointer at `lookTransform+0x10` may be either the native `TransformAccess` or a managed wrapper; the probe reads `+HierarchyOffset(0x40)` and picks the right path.
- **Camera offsets** shifted: `ViewMatrix` `0x128 → 0x88`, `FOV` `0x1A8 → 0x188`, `AspectRatio` `0x518 → 0x4F8`.
- **TypeInfoTableRva** defaults to `0x0` and is resolved at runtime via sig-scan with extended Unity 6 patterns plus a wide last-resort fallback in [`src-silk6/Tarkov/Unity/IL2CPP/TypeInfoTableResolver.cs`](src-silk6/Tarkov/Unity/IL2CPP/TypeInfoTableResolver.cs).
- `GomFallback` RVA `0x1A233A0 → 0x21A4450` (UnityPlayer.dll Unity 6000.3.6f1).

---

## Project Details

### `lib/VmmSharpEx`

A managed C# wrapper around [MemProcFS](https://github.com/ufrisk/MemProcFS) (`vmm.dll`) and LeechCore (`leechcore.dll`). Provides a high-level `Vmm` handle (read / write / VFS / process enumeration), a `LeechCore` device wrapper, a scatter API for batched gathers / writes, a memory search engine, a refresh manager, strongly-typed flag enums, a Win32 virtual-key DMA input manager, and a `VmmPointer` abstraction with a rich `VmmException` hierarchy.

- TFM: `net10.0-windows`, `Nullable=enable`, doc-file generated.
- Native bin: `lib/VmmSharpEx/native/` (`vmm.dll`, `leechcore.dll`, `leechcore_driver.dll`, `FTD3XX.dll`, `dbghelp.dll`, `symsrv.dll`, `tinylz4.dll`, `vcruntime140.dll`).
- License: **AGPL-3.0** — original MemProcFS API © Ulf Frisk; `VmmSharpEx` modifications © Lone (Lone DMA), 2025.

### `src-silk6`

- AssemblyName: `eft-dma-radar-silk6` · RootNamespace: `eft_dma_radar.Silk6`
- Entry point: [`SilkProgram.Main`](src-silk6/Program.cs)
- Packages: `ImGui.NET 1.91.6.1`, `Silk.NET.Windowing/Input/OpenGL/OpenGL.Extensions.ImGui 2.23.0`, `SkiaSharp 3.119.2`, `Svg.Skia 3.0.3`, `Open.Nat.imerzan 2.2.0` (+ `Microsoft.AspNetCore.App` framework reference for the web radar).
- In-tree docs: [`src-silk6/Docs/UNITY_ENGINE_CHANGES.md`](src-silk6/Docs/UNITY_ENGINE_CHANGES.md), `src-silk6/Docs/DEBUG_OUTPUT_REFERENCE.md`, `src-silk6/Docs/MIGRATION_ROADMAP.md`.

---

## License

The source code in this repository (everything outside `lib/VmmSharpEx/`) is licensed under the **[PolyForm Noncommercial License 1.0.0](LICENSE)** — free to use and modify for personal / non-commercial purposes; commercial use, resale, hosting paid services, or any other revenue-generating use is **not permitted**.

The component under `lib/VmmSharpEx/` is a wrapper around [MemProcFS](https://github.com/ufrisk/MemProcFS) and is licensed separately under **AGPL-3.0** — its original copyright notices are retained in the source files of that directory. Because the compiled radar binary links AGPL-3.0 code, **redistributors of compiled binaries must also satisfy AGPL-3.0 requirements** (source availability, etc.). The PolyForm Noncommercial terms govern this repository's own source code.

If you want to use this project commercially, that means writing a clean replacement for VmmSharpEx (talking to MemProcFS yourself) **and** obtaining a separate commercial license from the copyright holder of this repository.

---

## Credits

- MemProcFS by **Ulf Frisk** (<https://github.com/ufrisk/MemProcFS>) — the DMA stack everything is built on.
- Reference data from [tarkov.dev](https://tarkov.dev/) (see in-app credits).
- Thanks to the broader EFT DMA / MemProcFS community for offsets and reverse-engineering work over the years.
