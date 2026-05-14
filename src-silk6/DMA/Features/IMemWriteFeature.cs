// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using eft_dma_radar.Silk6.DMA.ScatterAPI;

namespace eft_dma_radar.Silk6.DMA.Features
{
    public interface IMemWriteFeature : IFeature
    {
        /// <summary>Apply the feature by queuing scatter-write entries. Must not throw.</summary>
        void TryApply(ScatterWriteHandle writes);
    }
}
