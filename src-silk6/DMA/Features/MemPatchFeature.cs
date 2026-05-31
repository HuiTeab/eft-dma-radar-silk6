// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

namespace eft_dma_radar.Silk6.DMA.Features
{
    /// <summary>
    /// Abstract singleton base for one-shot byte-rewrite patches. A subclass
    /// returns the target method address (typically <c>Memory.GameAssemblyBase
    /// + RVA</c>), supplies <see cref="Patch"/> bytes, and optionally a
    /// <see cref="Signature"/> to locate the patch site inside the method body.
    /// </summary>
    /// <remarks>
    /// Lifecycle: registers itself on type-load via the static cctor (same
    /// pattern as <see cref="MemWriteFeature{T}"/>). The <see cref="FeatureManager"/>
    /// worker polls <see cref="CanRun"/> each tick and calls <see cref="TryApply"/>
    /// directly when ready. Once <see cref="IsApplied"/> flips true the feature
    /// becomes a no-op until the next game process (or a manual reset by the
    /// subclass).
    /// </remarks>
    public abstract class MemPatchFeature<T> : IFeature, IMemPatchFeature
        where T : IMemPatchFeature
    {
        /// <summary>Singleton instance created and registered at class-load time.</summary>
        public static T Instance { get; }

        private readonly Stopwatch _sw = Stopwatch.StartNew();

        static MemPatchFeature()
        {
            Instance = Activator.CreateInstance<T>();
            IFeature.Register(Instance);
            Log.WriteLine($"[MemPatchFeature] Registered: {typeof(T).Name}");
        }

        public virtual bool Enabled { get; set; }

        public bool IsApplied { get; protected set; }

        /// <summary>Minimum interval between consecutive TryApply attempts.</summary>
        protected virtual TimeSpan Delay => TimeSpan.FromSeconds(1);

        protected bool DelayElapsed => Delay == TimeSpan.Zero || _sw.Elapsed >= Delay;

        public virtual bool CanRun
        {
            get
            {
                if (!Memory.Ready || !Enabled || IsApplied)
                    return false;
                if (!DelayElapsed)
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Absolute address of the start of the method body to patch. Typically
        /// <c>Memory.GameAssemblyBase + Offsets.&lt;Class&gt;.&lt;Method&gt;_RVA</c>.
        /// Return 0 if the address can't be resolved yet (the patch is retried
        /// after <see cref="Delay"/>).
        /// </summary>
        protected abstract ulong ResolveMethodAddress();

        /// <summary>Bytes to write at the patch location.</summary>
        protected abstract byte[] Patch { get; }

        /// <summary>
        /// If non-null, <see cref="TryApply"/> reads <see cref="ReadSize"/>
        /// bytes from the method start and writes <see cref="Patch"/> at the
        /// first occurrence of this signature inside the body. If null, the
        /// patch is written at the method start (whole-method overwrite).
        /// </summary>
        protected virtual byte[]? Signature => null;

        /// <summary>Bytes to read when scanning for <see cref="Signature"/>. Default 0x100.</summary>
        protected virtual int ReadSize => 0x100;

        public virtual bool TryApply()
        {
            if (IsApplied) return true;

            try
            {
                ulong methodAddr = ResolveMethodAddress();
                if (!methodAddr.IsValidVirtualAddress())
                    return false;

                byte[] patch = Patch;
                byte[]? sig = Signature;

                if (sig is null)
                {
                    Memory.WriteBufferEnsure<byte>(methodAddr, patch);
                    IsApplied = true;
                    Log.WriteLine($"[MemPatch] {typeof(T).Name} applied (whole-method, {patch.Length} bytes).");
                    return true;
                }

                Span<byte> body = ReadSize <= 0x1000 ? stackalloc byte[ReadSize] : new byte[ReadSize];
                Memory.ReadBuffer(methodAddr, body, false);

                int sigOffset = IndexOf(body, sig);
                if (sigOffset >= 0)
                {
                    Memory.WriteBufferEnsure<byte>(methodAddr + (uint)sigOffset, patch);
                    IsApplied = true;
                    Log.WriteLine($"[MemPatch] {typeof(T).Name} applied at +0x{sigOffset:X} ({patch.Length} bytes).");
                    return true;
                }

                if (IndexOf(body, patch) >= 0)
                {
                    IsApplied = true;
                    Log.WriteLine($"[MemPatch] {typeof(T).Name} already applied (patch bytes present).");
                    return true;
                }

                Log.WriteLine($"[MemPatch] {typeof(T).Name} signature not found in first 0x{ReadSize:X} bytes.");
            }
            catch (Exception ex)
            {
                Log.WriteLine($"[MemPatch] {typeof(T).Name} threw: {ex.Message}");
            }

            return false;
        }

        public void OnApply()
        {
            if (Delay != TimeSpan.Zero)
                _sw.Restart();
        }

        public virtual void OnGameStart() { }
        public virtual void OnRaidStart() { }
        public virtual void OnRaidEnd() { }

        /// <summary>
        /// Default: clear <see cref="IsApplied"/> so the patch re-runs against
        /// the next game process. Overrides should call <c>base.OnGameStop()</c>.
        /// </summary>
        public virtual void OnGameStop()
        {
            IsApplied = false;
        }

        private static int IndexOf(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
        {
            if (needle.Length == 0 || needle.Length > haystack.Length)
                return -1;
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                if (haystack.Slice(i, needle.Length).SequenceEqual(needle))
                    return i;
            }
            return -1;
        }
    }
}
