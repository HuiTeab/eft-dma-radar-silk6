// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using System.Collections.Frozen;

namespace eft_dma_radar.Silk6.UI.Maps
{
    /// <summary>
    /// Static catalog mapping silk in-game map IDs to tarkov.dev SVG map metadata.
    /// Source-of-truth file: <c>https://raw.githubusercontent.com/the-hideout/tarkov-dev/main/src/data/maps.json</c>
    /// — values here are the committed fallback used when the runtime HTTP fetch
    /// (<see cref="TarkovDevMapsClient"/>) fails or is offline.
    /// </summary>
    /// <remarks>
    /// Projection: tarkov.dev's <c>transform = [scaleX, marginX, scaleY, marginY]</c>
    /// gives world → SVG-pixel:
    ///   pixelX =  scaleX * lng + marginX
    ///   pixelY = -scaleY * lat + marginY     (Leaflet's getCRS negates scaleY)
    /// where (lng, lat) = (worldX, worldZ). All current catalog entries also carry
    /// the playable area <c>bounds</c> (world-coord rectangle) for documentation +
    /// possible future cropping. <c>coordinateRotation</c> is honoured by the
    /// per-rotation cfg.X/cfg.Y offsets below — the rasterized bitmap is rotated
    /// to match and the projection lands on the right pixel.
    /// </remarks>
    internal static class TarkovDevSvgCatalog
    {
        /// <summary>One tarkov.dev SVG map entry.</summary>
        internal sealed record Entry(
            /// <summary>tarkov.dev <c>nameId</c> (e.g. <c>customs</c>). Used as the cache filename stem.</summary>
            string NameId,
            /// <summary>Full URL to the SVG file on assets.tarkov.dev.</summary>
            string SvgUrl,
            /// <summary><c>baseTransform[0]</c> — pixels per world unit along X.</summary>
            float ScaleX,
            /// <summary><c>baseTransform[1]</c> — pixel offset for X (world origin → SVG pixel X).</summary>
            float MarginX,
            /// <summary><c>baseTransform[2]</c> — pixels per world unit along Z. Note this is the RAW value; the projection negates it like Leaflet's getCRS does.</summary>
            float ScaleY,
            /// <summary><c>baseTransform[3]</c> — pixel offset for Y.</summary>
            float MarginY,
            /// <summary><c>coordinateRotation</c> in degrees (0, 90, 180, 270).</summary>
            int Rotation,
            /// <summary>Playable area world-coord bounds (min lng, max lng, min lat, max lat). Currently informational.</summary>
            float LngMin, float LngMax, float LatMin, float LatMax);

        // Source: https://raw.githubusercontent.com/the-hideout/tarkov-dev/main/src/data/maps.json
        private static readonly FrozenDictionary<string, Entry> _entries =
            new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase)
            {
                ["bigmap"] = new Entry(
                    NameId: "customs",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/Customs.svg",
                    ScaleX: 0.239f, MarginX: 168.65f,
                    ScaleY: 0.239f, MarginY: 136.35f,
                    Rotation: 180,
                    LngMin: -372f, LngMax: 698f, LatMin: -307f, LatMax: 237f),

                ["woods"] = new Entry(
                    NameId: "woods",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/Woods.svg",
                    ScaleX: 0.1855f, MarginX: 112.95f,
                    ScaleY: 0.1855f, MarginY: 167.85f,
                    Rotation: 180,
                    LngMin: -761f, LngMax: 646f, LatMin: -914f, LatMax: 442f),

                ["interchange"] = new Entry(
                    NameId: "interchange",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/Interchange.svg",
                    ScaleX: 0.265f, MarginX: 150.6f,
                    ScaleY: 0.265f, MarginY: 134.6f,
                    Rotation: 180,
                    LngMin: -433f, LngMax: 598f, LatMin: -442f, LatMax: 426f),

                ["rezervbase"] = new Entry(
                    NameId: "reserve",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/Reserve.svg",
                    ScaleX: 0.395f, MarginX: 122.0f,
                    ScaleY: 0.395f, MarginY: 137.65f,
                    Rotation: 180,
                    LngMin: -303f, LngMax: 289f, LatMin: -274f, LatMax: 272f),

                ["shoreline"] = new Entry(
                    NameId: "shoreline",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/Shoreline.svg",
                    ScaleX: 0.16f, MarginX: 83.2f,
                    ScaleY: 0.16f, MarginY: 111.1f,
                    Rotation: 180,
                    LngMin: -1056f, LngMax: 504f, LatMin: -415f, LatMax: 618f),

                ["tarkovstreets"] = new Entry(
                    NameId: "streets-of-tarkov",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/StreetsOfTarkov.svg",
                    ScaleX: 0.0573f, MarginX: 188.1f,
                    ScaleY: 0.0573f, MarginY: 109.65f,
                    Rotation: 180,
                    LngMin: -280f, LngMax: 323f, LatMin: -295f, LatMax: 532f),

                ["lighthouse"] = new Entry(
                    NameId: "lighthouse",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/Lighthouse.svg",
                    ScaleX: 0.103f, MarginX: 60.0f,
                    ScaleY: 0.103f, MarginY: 99.0f,
                    Rotation: 180,
                    LngMin: -545f, LngMax: 515f, LatMin: -998f, LatMax: 725f),

                ["sandbox"] = new Entry(
                    NameId: "ground-zero",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/GroundZero.svg",
                    ScaleX: 0.524f, MarginX: 167.3f,
                    ScaleY: 0.524f, MarginY: 65.1f,
                    Rotation: 180,
                    LngMin: -99f, LngMax: 249f, LatMin: -124f, LatMax: 364f),

                ["sandbox_high"] = new Entry(
                    NameId: "ground-zero",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/GroundZero.svg",
                    ScaleX: 0.524f, MarginX: 167.3f,
                    ScaleY: 0.524f, MarginY: 65.1f,
                    Rotation: 180,
                    LngMin: -99f, LngMax: 249f, LatMin: -124f, LatMax: 364f),

                ["terminal"] = new Entry(
                    NameId: "terminal",
                    SvgUrl: "https://assets.tarkov.dev/maps/svg/Terminal.svg",
                    ScaleX: 0.1f, MarginX: 100f,
                    ScaleY: 0.1f, MarginY: 100f,
                    Rotation: 180,
                    LngMin: -433f, LngMax: 463f, LatMin: -580f, LatMax: 475f),
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        public static bool TryGet(string mapId, out Entry entry) =>
            _entries.TryGetValue(mapId, out entry!);

        public static bool IsSupported(string mapId) => _entries.ContainsKey(mapId);

        /// <summary>All bundled fallback entries — used to seed the on-disk cache on first run.</summary>
        public static IReadOnlyDictionary<string, Entry> All => _entries;

        /// <summary>
        /// Builds a renderer <see cref="MapConfig"/> for a CROPPED + ROTATED tarkov.dev
        /// SVG bitmap. The crop rect (in raw SVG pixel space) removes the empty padding
        /// around the playable area so the playable map fills the canvas. The rotation
        /// is then applied to the cropped bitmap, and the projection offsets compensate
        /// so markers land on the correct rotated pixels.
        ///
        /// Projection: tarkov.dev's <c>baseTransform</c> maps (lng, lat) → raw SVG pixel.
        /// After crop + rotation, the offsets become (cw = cropped width, ch = cropped height):
        ///   rot 0  : bx = marginX - cropL       ; by = marginY - cropT
        ///   rot 90 : bx = marginY - cropT       ; by = cw + cropL - marginX
        ///   rot 180: bx = cw + cropL - marginX  ; by = ch + cropT - marginY
        ///   rot 270: bx = ch + cropT - marginY  ; by = marginX - cropL
        /// </summary>
        /// <param name="rotationDeg">0, 90, 180, or 270. Visual CCW rotation of the bitmap.</param>
        /// <param name="rasterWidth">Width of the rasterized bitmap (already cropped + rotated).</param>
        /// <param name="rasterHeight">Height of the rasterized bitmap (already cropped + rotated).</param>
        /// <param name="cropL">Left edge of the crop rect in raw SVG pixel space.</param>
        /// <param name="cropT">Top edge of the crop rect in raw SVG pixel space.</param>
        /// <param name="cropW">Width of the crop rect in raw SVG pixel space (before rotation).</param>
        /// <param name="cropH">Height of the crop rect in raw SVG pixel space (before rotation).</param>
        public static MapConfig BuildConfig(
            string mapId,
            MapConfig svgConfig,
            Entry e,
            int rasterWidth,
            int rasterHeight,
            float cropL,
            float cropT,
            float cropW,
            float cropH,
            int rotationDeg)
        {
            float scale = e.ScaleX; // assume scaleX == scaleY (true for every current map)
            float cfgX, cfgY;
            switch (rotationDeg)
            {
                case 90:
                    cfgX = e.MarginY - cropT;
                    cfgY = cropW + cropL - e.MarginX;
                    break;
                case 180:
                    cfgX = cropW + cropL - e.MarginX;
                    cfgY = cropH + cropT - e.MarginY;
                    break;
                case 270:
                    cfgX = cropH + cropT - e.MarginY;
                    cfgY = e.MarginX - cropL;
                    break;
                default: // 0°
                    cfgX = e.MarginX - cropL;
                    cfgY = e.MarginY - cropT;
                    break;
            }

            return new MapConfig
            {
                MapID = svgConfig.MapID,
                X = cfgX,
                Y = cfgY,
                Scale = scale,
                SvgScale = 1f,
                DisableDimming = true,
                Rotation = rotationDeg,
                MapLayers = svgConfig.MapLayers,
            };
        }
    }
}
