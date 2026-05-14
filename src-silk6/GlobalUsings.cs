// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

// Core framework
global using SDK;
global using SkiaSharp;
global using System.Buffers;
global using System.Collections.Concurrent;
global using System.Diagnostics;
global using System.Numerics;
global using System.Reflection;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;

// Silk project namespaces
global using eft_dma_radar.Silk6.Misc;
global using eft_dma_radar.Silk6.Misc.Data;
global using eft_dma_radar.Silk6.Misc.Input;
global using eft_dma_radar.Silk6.Misc.Pools;
global using eft_dma_radar.Silk6.DMA;
global using eft_dma_radar.Silk6.Tarkov;
global using eft_dma_radar.Silk6.Tarkov.GameWorld;
global using eft_dma_radar.Silk6.Tarkov.GameWorld.Exits;
global using eft_dma_radar.Silk6.Tarkov.GameWorld.Loot;
global using eft_dma_radar.Silk6.Tarkov.GameWorld.Player;
global using eft_dma_radar.Silk6.Tarkov.GameWorld.Quests;
global using eft_dma_radar.Silk6.Config;
global using eft_dma_radar.Silk6.Tarkov.Unity.Collections;
global using eft_dma_radar.Silk6.UI;
global using eft_dma_radar.Silk6.UI.Controls;
global using eft_dma_radar.Silk6.UI.Panels;
global using eft_dma_radar.Silk6.UI.ESP;
global using eft_dma_radar.Silk6.UI.Maps;
global using eft_dma_radar.Silk6.UI.Presets;
global using eft_dma_radar.Silk6.Tarkov.Features.MemoryWrites;
