// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using Svg.Skia;
using Catalog = eft_dma_radar.Silk6.UI.Maps.TarkovDevSvgCatalog;

namespace eft_dma_radar.Silk6.UI.Maps
{
    /// <summary>
    /// Renderer for a tarkov.dev-hosted SVG map. Loads the SVG once from the
    /// <see cref="TarkovDevMapsClient"/> cache (downloading on first use), rasterizes
    /// it to a single <see cref="SKImage"/>, and serves it through the
    /// <see cref="IRadarMap"/> draw API. Coordinate projection uses the tarkov.dev
    /// <c>baseTransform</c>+<c>coordinateRotation</c> baked into <see cref="MapConfig"/>
    /// by <see cref="Catalog.BuildConfig"/>.
    /// </summary>
    /// <remarks>
    /// V1 limitations:
    ///   <list type="bullet">
    ///     <item>Single flat layer — tarkov.dev SVGs have named <c>&lt;g&gt;</c> groups
    ///       for each floor but we render all of them at once. Floor switching can be
    ///       added later by parsing the SVG and rasterizing per-group separately.</item>
    ///     <item>Only 0°/180° rotations supported (same as <see cref="SatelliteMap"/>);
    ///       maps with 90°/270° rotation fall back to the bundled local renderer at the
    ///       catalog level — they never reach this class.</item>
    ///   </list>
    /// </remarks>
    internal sealed class TarkovDevSvgMap : IRadarMap
    {
        private readonly SKImage _image;
        private readonly float _mapWidth;
        private readonly float _mapHeight;
        private bool _disposed;

        public string ID { get; }
        public MapConfig Config { get; }

        private static readonly SKPaint _svgPaint = new() { IsAntialias = true };
        private static readonly SKPaint _paintBitmap = new() { IsAntialias = true };

        /// <summary>
        /// Constructs the renderer for an already-cached SVG. Throws if the SVG fails
        /// to rasterize — <see cref="MapManager"/> catches and falls back to the local
        /// bundled renderer in that case. The <see cref="MapConfig"/> is computed from
        /// the catalog entry's world bounds and the rasterized image dimensions.
        /// </summary>
        public TarkovDevSvgMap(string id, MapConfig svgConfig, Catalog.Entry entry, string svgPath, int rotationDeg)
        {
            ID = id;

            // Compute the playable-area pixel rect on the RAW SVG by applying tarkov.dev's
            // transform to the world bounds. tarkov.dev's SVGs have lots of padding around
            // the actual playable area — cropping to this rect makes the playable map
            // content fill the canvas (matching silk's bundled SVGs) instead of being
            // crammed into ~25% of the image.
            float cropL = entry.ScaleX * entry.LngMin + entry.MarginX;
            float cropR = entry.ScaleX * entry.LngMax + entry.MarginX;
            float cropT = -entry.ScaleY * entry.LatMax + entry.MarginY;
            float cropB = -entry.ScaleY * entry.LatMin + entry.MarginY;
            // Make sure left<right and top<bottom (sign of scaleY can flip them).
            if (cropR < cropL) (cropL, cropR) = (cropR, cropL);
            if (cropB < cropT) (cropT, cropB) = (cropB, cropT);

            _image = RasterizeSvg(svgPath, cropL, cropT, cropR, cropB, rotationDeg)
                ?? throw new InvalidOperationException($"Failed to rasterize SVG '{svgPath}' for map '{id}'.");
            _mapWidth = _image.Width;
            _mapHeight = _image.Height;

            // Build the projection-space config now that we know the actual image dims.
            Config = Catalog.BuildConfig(id, svgConfig, entry, _image.Width, _image.Height, cropL, cropT, cropR - cropL, cropB - cropT, rotationDeg);

            Log.WriteLine($"[TarkovDevSvgMap] '{id}' loaded: image={_mapWidth}x{_mapHeight}, rotation={rotationDeg}°, crop=({cropL:0.0},{cropT:0.0})→({cropR:0.0},{cropB:0.0}) → cfg X={Config.X:0.00} Y={Config.Y:0.00} Scale={Config.Scale:0.0000}");
        }

        /// <summary>
        /// Loads the SVG file and rasterizes the playable-area sub-rectangle to an
        /// SKImage on the CPU. The crop rect (in raw SVG pixel space) is brought to
        /// the origin and then rotated by <paramref name="rotationDeg"/> degrees CCW
        /// (0 / 90 / 180 / 270) so the output bitmap is both cropped AND rotated in a
        /// single render pass. The catalog's BuildConfig produces a MapConfig that
        /// compensates the projection so markers land on the correct rotated pixels.
        /// </summary>
        private static SKImage? RasterizeSvg(string svgPath, float cropL, float cropT, float cropR, float cropB, int rotationDeg)
        {
            try
            {
                using var stream = File.OpenRead(svgPath);
                using var svg = SKSvg.CreateFromStream(stream);
                if (svg is null) return null;

                var picture = svg.Picture;
                if (picture is null) return null;

                var cull = picture.CullRect;
                if (cull.Width <= 0 || cull.Height <= 0) return null;

                // Clamp crop rect to the picture's actual bounds (some SVGs have content
                // extending slightly beyond bounds, others have bounds slightly outside).
                cropL = Math.Max(cropL, cull.Left);
                cropT = Math.Max(cropT, cull.Top);
                cropR = Math.Min(cropR, cull.Right);
                cropB = Math.Min(cropB, cull.Bottom);

                int cropW = (int)Math.Ceiling(cropR - cropL);
                int cropH = (int)Math.Ceiling(cropB - cropT);
                if (cropW <= 0 || cropH <= 0)
                {
                    Log.WriteLine($"[TarkovDevSvgMap] Invalid crop {cropW}x{cropH} for '{svgPath}'");
                    return null;
                }

                // 90° / 270° rotations swap the bitmap dimensions; 0° / 180° keep them.
                bool swap = rotationDeg == 90 || rotationDeg == 270;
                int outW = swap ? cropH : cropW;
                int outH = swap ? cropW : cropH;

                var info = new SKImageInfo(outW, outH, SKColorType.Rgba8888, SKAlphaType.Premul);

                using var surface = SKSurface.Create(info);
                if (surface is null)
                {
                    Log.WriteLine($"[TarkovDevSvgMap] SKSurface.Create failed ({outW}x{outH}) for '{svgPath}'");
                    return null;
                }

                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Compose two transforms (last call applied first to picture coords):
                //   1. Translate picture so its (cropL, cropT) is at the local origin.
                //   2. Rotate CCW by rotationDeg and translate the result into (0,0)-(outW,outH).
                //
                // SkiaSharp's canvas API applies the LAST transform first, so we set them
                // here in OUTERMOST-first order:
                switch (rotationDeg)
                {
                    case 90:
                        canvas.Translate(0, cropW);
                        canvas.RotateDegrees(-90f); // CCW
                        break;
                    case 180:
                        canvas.Translate(cropW, cropH);
                        canvas.RotateDegrees(180f);
                        break;
                    case 270:
                        canvas.Translate(cropH, 0);
                        canvas.RotateDegrees(90f); // CCW 270° == CW 90°
                        break;
                    // 0°: no rotation transform — just the crop translate below.
                }
                canvas.Translate(-cropL, -cropT);

                canvas.DrawPicture(picture, _svgPaint);
                return surface.Snapshot();
            }
            catch (Exception ex)
            {
                Log.WriteLine($"[TarkovDevSvgMap] Rasterize failed for '{svgPath}': {ex.Message}");
                return null;
            }
        }

        public void Draw(SKCanvas canvas, float playerHeight, SKRect mapBounds, SKRect windowBounds)
        {
            // Draw the SVG image at its exact projected position on the canvas.
            // mapBounds is in image-pixel space (centered on player), so the image's
            // (0,0)→(width,height) corners map to a specific dst rect on the canvas.
            // This avoids SkiaSharp's ambiguous behavior when src extends past the
            // image bounds (which can happen when the player is near the image edge).
            float sx = windowBounds.Width  / mapBounds.Width;
            float sy = windowBounds.Height / mapBounds.Height;
            var imageDst = new SKRect(
                (0           - mapBounds.Left) * sx + windowBounds.Left,
                (0           - mapBounds.Top ) * sy + windowBounds.Top,
                (_mapWidth   - mapBounds.Left) * sx + windowBounds.Left,
                (_mapHeight  - mapBounds.Top ) * sy + windowBounds.Top);
            canvas.DrawImage(_image, imageDst, _paintBitmap);
        }

        public MapParams GetParameters(SKSize canvasSize, int zoom, ref Vector2 centerMapPos)
        {
            zoom = Math.Clamp(zoom, 1, 800);

            float zoomMul = 0.01f * zoom;
            float zoomWidth = _mapWidth * zoomMul;
            float zoomHeight = _mapHeight * zoomMul;

            var bounds = new SKRect(
                centerMapPos.X - zoomWidth * 0.5f,
                centerMapPos.Y - zoomHeight * 0.5f,
                centerMapPos.X + zoomWidth * 0.5f,
                centerMapPos.Y + zoomHeight * 0.5f);

            bounds = AspectFill(bounds, canvasSize);

            return new MapParams(
                Config,
                bounds,
                canvasSize.Width / bounds.Width,
                canvasSize.Height / bounds.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SKRect AspectFill(SKRect rect, SKSize size)
        {
            if (size.Width <= 0 || size.Height <= 0)
                return rect;

            float rectAspect = rect.Width / rect.Height;
            float targetAspect = size.Width / size.Height;

            float cx = rect.MidX;
            float cy = rect.MidY;
            float hw, hh;

            if (rectAspect > targetAspect)
            {
                hw = rect.Width * 0.5f;
                hh = hw / targetAspect;
            }
            else
            {
                hh = rect.Height * 0.5f;
                hw = hh * targetAspect;
            }
            return new SKRect(cx - hw, cy - hh, cx + hw, cy + hh);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _image.Dispose();
        }
    }
}
