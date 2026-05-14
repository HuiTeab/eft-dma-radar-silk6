// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using eft_dma_radar.Silk6.DMA.ScatterAPI;

namespace eft_dma_radar.Silk6.DMA.Features
{
    /// <summary>
    /// Base class for scatter-write memory features.
    /// Subclass registers itself as a singleton via the static constructor.
    /// </summary>
    public abstract class MemWriteFeature<T> : IFeature, IMemWriteFeature
        where T : IMemWriteFeature
    {
        /// <summary>Singleton instance created and registered at class-load time.</summary>
        public static T Instance { get; }

        private readonly Stopwatch _sw = Stopwatch.StartNew();

        static MemWriteFeature()
        {
            Instance = Activator.CreateInstance<T>();
            IFeature.Register(Instance);
            Log.WriteLine($"[MemWriteFeature] Registered: {typeof(T).Name}");
        }

        public virtual bool Enabled { get; set; }

        /// <summary>Minimum interval between consecutive TryApply calls. Override to change.</summary>
        protected virtual TimeSpan Delay => TimeSpan.FromMilliseconds(10);

        protected bool DelayElapsed => Delay == TimeSpan.Zero || _sw.Elapsed >= Delay;

        public virtual bool CanRun
        {
            get
            {
                if (!Memory.InRaid)
                    return false;
                if (!DelayElapsed)
                    return false;
                return true;
            }
        }

        public virtual void TryApply(ScatterWriteHandle writes) { }

        public void OnApply()
        {
            if (Delay != TimeSpan.Zero)
                _sw.Restart();
        }

        public virtual void OnGameStart() { }
        public virtual void OnRaidStart() { }
        public virtual void OnRaidEnd() { }
        public virtual void OnGameStop() { }
    }
}
