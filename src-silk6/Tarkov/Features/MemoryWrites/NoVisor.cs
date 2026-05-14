// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using eft_dma_radar.Silk6.DMA.Features;
using eft_dma_radar.Silk6.DMA.ScatterAPI;
using eft_dma_radar.Silk6.Tarkov.Unity;

namespace eft_dma_radar.Silk6.Tarkov.Features.MemoryWrites
{
    public sealed class NoVisor : MemWriteFeature<NoVisor>
    {
        private bool _lastEnabledState;
        private ulong _cachedVisorEffect;

        private const float VISOR_DISABLED = 0f;
        private const float VISOR_ENABLED = 1f;

        public override bool Enabled
        {
            get => SilkProgram.Config.MemWrites.NoVisor;
            set => SilkProgram.Config.MemWrites.NoVisor = value;
        }

        protected override TimeSpan Delay => TimeSpan.FromMilliseconds(500);

        public override void TryApply(ScatterWriteHandle writes)
        {
            try
            {
                if (Memory.Game is not LocalGameWorld game)
                    return;

                if (Enabled && _cachedVisorEffect != 0)
                {
                    var currentIntensity = Memory.ReadValue<float>(_cachedVisorEffect + Offsets.VisorEffect.Intensity);
                    var targetIntensity = Enabled ? VISOR_DISABLED : VISOR_ENABLED;
                    _lastEnabledState = (currentIntensity == targetIntensity);
                }

                if (Enabled != _lastEnabledState)
                {
                    var visorEffect = GetVisorEffect(game);
                    if (!visorEffect.IsValidVirtualAddress())
                        return;

                    var targetIntensity = Enabled ? VISOR_DISABLED : VISOR_ENABLED;
                    writes.AddValueEntry(visorEffect + Offsets.VisorEffect.Intensity, targetIntensity);

                    writes.Callbacks += () =>
                    {
                        _lastEnabledState = Enabled;
                        Log.WriteLine($"[NoVisor] {(Enabled ? "Enabled" : "Disabled")}");
                    };
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine($"[NoVisor]: {ex.Message}");
                _cachedVisorEffect = default;
            }
        }

        private ulong GetVisorEffect(LocalGameWorld game)
        {
            if (_cachedVisorEffect.IsValidVirtualAddress())
                return _cachedVisorEffect;

            var fps = game.CameraManager?.FPSCamera ?? 0ul;
            if (!fps.IsValidVirtualAddress()) return 0;

            var comp = GOM.GetComponentFromBehaviour(fps, "VisorEffect");
            if (comp.IsValidVirtualAddress())
                _cachedVisorEffect = comp;
            return comp;
        }

        public override void OnRaidStart()
        {
            _lastEnabledState = default;
            _cachedVisorEffect = default;
        }

        public override void OnRaidEnd()
        {
            _lastEnabledState = default;
            _cachedVisorEffect = default;
        }
    }
}
