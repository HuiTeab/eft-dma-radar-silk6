// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

namespace eft_dma_radar.Silk6.DMA.Features
{
    /// <summary>
    /// Shellcode constants for whole-method overwrites. Hand-encoded so silk6
    /// does not pull in Reloaded.Assembler. The old codebase assembled these
    /// at startup; the byte sequences below match what the assembler emits
    /// (verified against x64 MSVC calling convention — return values in rax).
    /// </summary>
    /// <remarks>
    /// Add new constants only when a port actually needs them. Resist the urge
    /// to mirror the full pre-1.0 catalogue here — most of those shellcodes
    /// (PatchReturnZeroFloat, ReturnInt(n)) are only used by patches outside
    /// the current scope.
    /// </remarks>
    public static class ShellKeeper
    {
        /// <summary>Equivalent to <c>return true;</c> — <c>mov rax, 1; ret</c>.</summary>
        public static readonly byte[] PatchTrue =
        {
            0x48, 0xC7, 0xC0, 0x01, 0x00, 0x00, 0x00, // mov rax, 1
            0xC3                                      // ret
        };

        /// <summary>Equivalent to <c>return false;</c> — <c>mov rax, 0; ret</c>.</summary>
        public static readonly byte[] PatchFalse =
        {
            0x48, 0xC7, 0xC0, 0x00, 0x00, 0x00, 0x00, // mov rax, 0
            0xC3                                      // ret
        };

        /// <summary>Equivalent to bare <c>return;</c> — single <c>ret</c> opcode.</summary>
        public static readonly byte[] PatchReturn = { 0xC3 };
    }
}
