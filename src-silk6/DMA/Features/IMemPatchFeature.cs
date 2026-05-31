// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

namespace eft_dma_radar.Silk6.DMA.Features
{
    /// <summary>
    /// One-shot in-process patch feature — overwrites bytes inside the game's
    /// own code (typically a method body in GameAssembly.dll). Unlike
    /// <see cref="IMemWriteFeature"/>, patches do not batch through the
    /// per-tick scatter handle; they call <see cref="Memory.WriteBufferEnsure{T}"/>
    /// directly because the writes are persistent (memory stays patched until
    /// the game process exits).
    /// </summary>
    public interface IMemPatchFeature : IFeature
    {
        /// <summary>True once the patch has been written successfully.</summary>
        bool IsApplied { get; }

        /// <summary>
        /// Try to apply the patch. Must not throw. Returns true if the patch
        /// is now in place (either just applied or already applied earlier).
        /// </summary>
        bool TryApply();
    }
}
