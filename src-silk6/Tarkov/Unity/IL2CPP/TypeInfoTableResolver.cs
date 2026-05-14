// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

#pragma warning disable IDE0130
using UTF8String = eft_dma_radar.Silk6.Misc.UTF8String;

namespace eft_dma_radar.Silk6.Tarkov.Unity.IL2CPP
{
    public static partial class Il2CppDumper
    {
        // ── Constants ────────────────────────────────────────────────────────

        private const int MaxTableEntries = 64;
        private const int MaxLastResortEntries = 1024;
        private const int EarlyProbeCount = 16;
        private const int EarlyProbeRequired = 8;
        private const int MidProbeOffset = 5_000;
        private const int MidProbeCount = 8;
        private const int MidProbeRequired = 3;
        private const string GameAssemblyName = "GameAssembly.dll";
        private const string LogTag = "[Il2CppDumper]";

        // ── Signatures & TypeIndex map ──────────────────────────────────────

        // (Sig, RelOffset, InstrLen, Desc) — RelOffset/InstrLen used for RIP-relative decode
        private static readonly (string Sig, int RelOffset, int InstrLen, string Desc)[] TypeInfoTableSigs =
        [
            // Primary: long unique read-path; very robust across patches
            ("48 8B 05 ? ? ? ? ? ? ? ? ? ? ? 90 48 85 DB 75 ? 48 8D 2D ? ? ? ? 48 89 6C 24 ? 48 8B CD E8 ? ? ? ? 90 ? ? ? 48 85 DB 75 ? 8B CF", 3, 7, "read: mov rax,[rip+rel32] (table lookup)"),
            // Fallback A: index-lookup read — extended with movsxd+test to reduce false-positive risk
            ("48 8B 0D ? ? ? ? 48 63 D0 ? ? ? ? 48 85 C9 74", 3, 7, "read: mov rcx,[rip+rel32]; movsxd+test (index lookup)"),
            // Fallback B: full-init write — trailing E8 removed so a call-target relocation won't break it
            ("48 89 05 ? ? ? ? 4C 8B 05 ? ? ? ? 48 8B 05 ? ? ? ? 48 63 48 ? BA ? ? ? ? 41 FF D0 48 89 05 ? ? ? ?", 3, 7, "write: mov [rip+rel32],rax; mov r8,[rip+rel32] (full init)"),
        ];

        // ── Last-resort signatures (only used when all primary sigs fail) ────
        // These patterns are too broad for normal use (>64 matches) but can still
        // find the correct RVA via ValidateTypeInfoTable when nothing else works.
        private static readonly (string Sig, int RelOffset, int InstrLen, string Desc)[] LastResortTableSigs =
        [
            ("48 89 05 ? ? ? ?", 3, 7, "last-resort: any mov [rip+rel32],rax (wide write)"),
        ];

        private static readonly (string Il2CppName, string FieldName)[] TypeIndexMap =
        [
            ("EFTHardSettings",       nameof(Offsets.Special.EFTHardSettings_TypeIndex)),
            ("GPUInstancerManager",   nameof(Offsets.Special.GPUInstancerManager_TypeIndex)),
            ("WeatherController",     nameof(Offsets.Special.WeatherController_TypeIndex)),
            ("GlobalConfiguration",   nameof(Offsets.Special.GlobalConfiguration_TypeIndex)),
            ("MatchingProgress",      nameof(Offsets.Special.MatchingProgress_TypeIndex)),
            ("MatchingProgressView",  nameof(Offsets.Special.MatchingProgressView_TypeIndex)),
            ("GamePlayerOwner",       nameof(Offsets.Special.GamePlayerOwner_TypeIndex)),
            ("TarkovApplication",     nameof(Offsets.Special.TarkovApplication_TypeIndex)),
            ("EFT.Hideout.HideoutArea",       nameof(Offsets.Special.HideoutArea_TypeIndex)),
            ("EFT.Hideout.HideoutController", nameof(Offsets.Special.HideoutController_TypeIndex)),
            ("BtrController",         nameof(Offsets.Special.BtrController_TypeIndex)),
        ];

        // ── State ────────────────────────────────────────────────────────────

        private static readonly FieldInfo[] CachedTypeIndexFields =
            typeof(Offsets.Special).GetFields(BindingFlags.Public | BindingFlags.Static);

        private record struct SigScanResult(int Index, string Desc, string State, int Matches, int ValidMatches, ulong Rva);

        private static SigScanResult[] _lastSigResults = [];
        private static string _lastResolutionMode = "not run";

        // ── Resolution pipeline ──────────────────────────────────────────────

        private static bool ResolveTypeInfoTableRva(ulong gaBase, bool quiet = false)
        {
            var testedRvas = new HashSet<ulong>();
            var sigResults = new List<SigScanResult>(TypeInfoTableSigs.Length);
            (ulong rva, ulong sigAddr, string sig)? first = null;

            for (int i = 0; i < TypeInfoTableSigs.Length; i++)
            {
                var (sig, relOff, instrLen, desc) = TypeInfoTableSigs[i];
                var (result, scanResult) = TryResolveFromSignature(i, sig, relOff, instrLen, desc, gaBase, testedRvas);
                sigResults.Add(scanResult);
                if (result.HasValue && first is null)
                    first = result;
            }

            // Last-resort pass — only runs when every primary sig produced nothing
            if (first is null)
            {
                if (!quiet) Log.WriteLine($"{LogTag} Primary sigs exhausted, trying last-resort signatures...");
                for (int i = 0; i < LastResortTableSigs.Length; i++)
                {
                    var (sig, relOff, instrLen, desc) = LastResortTableSigs[i];
                    var (result, scanResult) = TryResolveFromSignature(
                        TypeInfoTableSigs.Length + i, sig, relOff, instrLen, desc, gaBase, testedRvas,
                        MaxLastResortEntries);
                    sigResults.Add(scanResult);
                    if (result.HasValue && first is null)
                        first = result;
                }
            }

            bool success;
            if (first.HasValue)
            {
                var prev = Offsets.Special.TypeInfoTableRva;
                Offsets.Special.TypeInfoTableRva = first.Value.rva;
                Log.WriteLine($"{LogTag} TypeInfoTable resolved: rva=0x{first.Value.rva:X}, unique={testedRvas.Count}");
                if (prev != first.Value.rva)
                    Log.WriteLine($"{LogTag} TypeInfoTableRva UPDATED: 0x{prev:X} → 0x{first.Value.rva:X}");
                _lastResolutionMode = "signature";
                success = true;
            }
            else if (Offsets.Special.TypeInfoTableRva != 0 && ValidateTypeInfoTable(gaBase, Offsets.Special.TypeInfoTableRva))
            {
                Log.WriteLine($"{LogTag} TypeInfoTable using fallback RVA: 0x{Offsets.Special.TypeInfoTableRva:X}");
                _lastResolutionMode = "fallback (hardcoded)";
                success = true;
            }
            else
            {
                if (!quiet)
                    Log.WriteLine($"{LogTag} WARNING: All TypeInfoTable resolution strategies failed — offsets may be stale!");
                _lastResolutionMode = "FAILED";
                success = false;
            }

            _lastSigResults = [.. sigResults];
            return success;
        }

        private static ((ulong rva, ulong sigAddr, string sig)?, SigScanResult) TryResolveFromSignature(
            int index, string sig, int relOff, int instrLen, string desc, ulong gaBase, HashSet<ulong> testedRvas,
            int maxEntries = MaxTableEntries)
        {
            ulong[] sigAddrs;
            try
            {
                sigAddrs = Memory.FindSignatures(sig, GameAssemblyName, maxEntries);
            }
            catch (Exception ex)
            {
                Log.WriteLine($"{LogTag} TypeInfoTable sig[{index}] scan error: {ex.Message}");
                return (null, new SigScanResult(index, desc, "ERROR", 0, 0, 0));
            }

            if (sigAddrs.Length == 0)
                return (null, new SigScanResult(index, desc, "MISS", 0, 0, 0));

            ulong duplicateRva = 0;
            int validCount = 0;
            foreach (var sigAddr in sigAddrs)
            {
                var rva = ResolveRipRelativeRva(sigAddr, relOff, instrLen, gaBase);
                if (rva == 0 || !ValidateTypeInfoTable(gaBase, rva))
                    continue;

                validCount++;
                if (testedRvas.Add(rva))
                    return ((rva, sigAddr, sig), new SigScanResult(index, desc, "OK", sigAddrs.Length, validCount, rva));

                duplicateRva = rva; // valid RVA but already found by a prior signature
            }

            if (duplicateRva != 0)
                return (null, new SigScanResult(index, desc, "DUPLICATE", sigAddrs.Length, validCount, duplicateRva));

            return (null, new SigScanResult(index, desc, "INVALID", sigAddrs.Length, validCount, 0));
        }

        // ── User Diagnostic Report ───────────────────────────────────────────

        private static List<string> BuildUserReportBox(int classCount, int updated, int fallback, int skipped)
        {
            var gaBase = Memory.GameAssemblyBase;
            string gaText = eft_dma_radar.Silk6.Misc.Utils.IsValidVirtualAddress(gaBase) ? $"0x{gaBase:X}" : "(not resolved)";

            int W = 56;
            const int SigTruncLen = 48;
            foreach (var r in _lastSigResults)
            {
                string rawSig = TypeInfoTableSigs[r.Index].Sig;
                int sigLen = Math.Min(rawSig.Length, SigTruncLen) + (rawSig.Length > SigTruncLen ? 3 : 0);
                int sigNeeded = 9 + sigLen;
                int statusNeeded = 4 + 7 + $"matches={r.Matches,-4} valid={r.ValidMatches,-4} rva=0x{r.Rva:X}".Length;
                if (sigNeeded > W) W = sigNeeded;
                if (statusNeeded > W) W = statusNeeded;
            }

            string Row(string text) => $"║  {text.PadRight(W - 2)}║";
            string Sep(string label) => $"╠── {label} {new string('─', W - 4 - label.Length)}╣";
            string Header(string text)
            {
                int pad = W - text.Length;
                int left = pad / 2;
                return $"║{new string(' ', left)}{text}{new string(' ', pad - left)}║";
            }

            var lines = new List<string>();
            lines.Add($"{LogTag} ╔{new string('═', W)}╗");
            lines.Add($"{LogTag} {Header("IL2CPP DIAGNOSTIC REPORT")}");
            lines.Add($"{LogTag} ╠{new string('═', W)}╣");
            lines.Add($"{LogTag} {Row("If you have issues, copy this entire block (╔ to ╚)")}");
            lines.Add($"{LogTag} {Row("and paste it when reporting to developers.")}");
            lines.Add($"{LogTag} ╠{new string('═', W)}╣");
            lines.Add($"{LogTag} {Row($"GameAssembly  : {gaText}")}");
            lines.Add($"{LogTag} {Row($"Resolution    : {_lastResolutionMode}")}");
            lines.Add($"{LogTag} {Row($"Table RVA     : 0x{Offsets.Special.TypeInfoTableRva:X}")}");
            lines.Add($"{LogTag} {Row($"Classes Found : {classCount}")}");

            int okCount = _lastSigResults.Count(r => r.State is "OK" or "DUPLICATE");
            lines.Add($"{LogTag} {Sep($"Signature Scan ({okCount}/{_lastSigResults.Length} OK)")}");
            foreach (var r in _lastSigResults)
            {
                string state = r.State is "OK" or "DUPLICATE" ? "OK" : r.State;
                string status = r.Rva != 0
                    ? $"  [{r.Index}] {state,-7} matches={r.Matches,-4} valid={r.ValidMatches,-4} rva=0x{r.Rva:X}"
                    : $"  [{r.Index}] {state,-7} matches={r.Matches,-4} valid={r.ValidMatches}";
                lines.Add($"{LogTag} {Row(status)}");
                string rawSig = TypeInfoTableSigs[r.Index].Sig;
                string sigLine = rawSig.Length > SigTruncLen ? rawSig[..SigTruncLen] + "..." : rawSig;
                lines.Add($"{LogTag} {Row($"       {sigLine}")}");
            }

            lines.Add($"{LogTag} {Sep("Offset Dump")}");
            lines.Add($"{LogTag} {Row($"Updated  : {updated}")}");
            lines.Add($"{LogTag} {Row($"Fallback : {fallback}")}");
            lines.Add($"{LogTag} {Row($"Skipped  : {skipped}")}");
            lines.Add($"{LogTag} {Sep("TypeIndex Values")}");
            foreach (var (il2cppName, fieldName) in TypeIndexMap)
            {
                var fi = GetTypeIndexField(fieldName);
                string val = fi is not null ? $"{fi.GetValue(null)}" : "(missing)";
                lines.Add($"{LogTag} {Row($"  {il2cppName + ":",-24} {val}")}");
            }

            lines.Add($"{LogTag} ╚{new string('═', W)}╝");
            return lines;
        }

        // ── RIP-relative decode ──────────────────────────────────────────────

        private static ulong ResolveRipRelativeRva(ulong sigAddr, int relOffset, int instrLen, ulong gaBase)
        {
            int rel;
            try { rel = Memory.ReadValue<int>(sigAddr + (ulong)relOffset, false); }
            catch { return 0; }

            ulong globalVa = sigAddr + (ulong)instrLen + (ulong)(long)rel;
            return globalVa > gaBase ? globalVa - gaBase : 0;
        }

        // ── TypeInfoTable validation ─────────────────────────────────────────

        private static bool ValidateTypeInfoTable(ulong gaBase, ulong rva)
        {
            ulong tablePtr;
            try { tablePtr = Memory.ReadPtr(gaBase + rva, false); }
            catch { return false; }

            return eft_dma_radar.Silk6.Misc.Utils.IsValidVirtualAddress(tablePtr)
                && ProbeTableEntries(tablePtr, 0, EarlyProbeCount, EarlyProbeRequired)
                && ProbeTableEntries(tablePtr, MidProbeOffset, MidProbeCount, MidProbeRequired);
        }

        private static bool ProbeTableEntries(ulong tablePtr, int startIndex, int count, int required)
        {
            ulong[] ptrs;
            try { ptrs = Memory.ReadArray<ulong>(tablePtr + (ulong)startIndex * 8, count, false); }
            catch { return false; }

            int valid = 0;
            foreach (var ptr in ptrs)
                if (IsValidClassPtr(ptr) && ++valid >= required)
                    return true;

            return false;
        }

        private static bool IsValidClassPtr(ulong ptr)
        {
            if (!eft_dma_radar.Silk6.Misc.Utils.IsValidVirtualAddress(ptr)) return false;
            try
            {
                var namePtr = Memory.ReadValue<ulong>(ptr + K_Name, false);
                if (!eft_dma_radar.Silk6.Misc.Utils.IsValidVirtualAddress(namePtr)) return false;
                var name = ReadStr(namePtr);
                return !string.IsNullOrEmpty(name) && name.Length < MaxNameLen && IsPlausibleClassName(name);
            }
            catch { return false; }
        }

        private static bool IsPlausibleClassName(string name)
        {
            foreach (char c in name)
                if (c < 0x20 || (c > 0x7E && c < 0xA0)) return false;
            return true;
        }

        // ── TypeIndex resolution ─────────────────────────────────────────────

        private static void ResolveTypeIndices(
            Dictionary<string, int> nameToIndex,
            List<(string Name, string Namespace, ulong KlassPtr, int Index)> classes)
        {
            foreach (var (il2cppName, fieldName) in TypeIndexMap)
            {
                var fi = GetTypeIndexField(fieldName);
                if (fi is null) continue;

                // Namespace-qualified entry (e.g. "EFT.Hideout.HideoutController"):
                // search the raw classes list by namespace + short name.
                int dotIdx = il2cppName.LastIndexOf('.');
                if (dotIdx > 0)
                {
                    var ns = il2cppName[..dotIdx];
                    var shortName = il2cppName[(dotIdx + 1)..];
                    bool found = false;
                    foreach (var (cName, cNs, _, cIdx) in classes)
                    {
                        if (cName == shortName && cNs == ns)
                        {
                            UpdateTypeIndexField(fi, (uint)cIdx);
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        Log.WriteLine($"{LogTag} WARN: '{il2cppName}' not found in type table — {fieldName} using fallback ({fi.GetValue(null) ?? 0u}).");
                }
                else if (nameToIndex.TryGetValue(il2cppName, out var index))
                {
                    UpdateTypeIndexField(fi, (uint)index);
                }
                else
                {
                    Log.WriteLine($"{LogTag} WARN: '{il2cppName}' not found in type table — {fieldName} using fallback ({fi.GetValue(null) ?? 0u}).");
                }
            }
        }

        private static FieldInfo? GetTypeIndexField(string fieldName) =>
            CachedTypeIndexFields.FirstOrDefault(f => f.Name == fieldName);

        private static void UpdateTypeIndexField(FieldInfo fi, uint newValue)
        {
            var previous = (uint)(fi.GetValue(null) ?? 0u);
            fi.SetValue(null, newValue);
            if (previous != newValue)
                Log.WriteLine($"{LogTag} {fi.Name} UPDATED: {previous} → {newValue}");
        }

        internal static void DebugDumpResolverState(int classCount, int updated, int fallback, int skipped) =>
            Log.WriteBlock(BuildUserReportBox(classCount, updated, fallback, skipped));

        /// <summary>
        /// Resolves an Il2CppClass pointer from the TypeInfoTable using a TypeIndex.
        /// Returns a valid klass pointer, or 0 on failure.
        /// Callers should cache the result.
        /// </summary>
        internal static ulong ResolveKlassByTypeIndex(uint typeIndex)
        {
            if (typeIndex == 0)
                return 0;

            var gaBase = Memory.GameAssemblyBase;
            if (!gaBase.IsValidVirtualAddress() || Offsets.Special.TypeInfoTableRva == 0)
                return 0;

            if (!Memory.TryReadPtr(gaBase + Offsets.Special.TypeInfoTableRva, out var tablePtr, false)
                || !tablePtr.IsValidVirtualAddress())
                return 0;

            return Memory.TryReadValue<ulong>(tablePtr + (ulong)typeIndex * 8, out var ptr, false)
                && ptr.IsValidVirtualAddress()
                ? ptr
                : 0;
        }

            }
        }