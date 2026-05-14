// Copyright (c) 2025 HuiTeab.
// Licensed under the PolyForm Noncommercial License 1.0.0.
// See LICENSE in the repository root for details.

using SilkUtils = eft_dma_radar.Silk6.Misc.Utils;

namespace eft_dma_radar.Silk6.Tarkov.Unity
{
    // ─────────────────────────────────────────────────────────────────────────────
    // IL2CPP Unity engine constants, layout structs, and GOM resolution.
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// All Unity engine offsets — both IL2CPP object layout and native transform hierarchy.
    /// Update when game patches break functionality.
    /// </summary>
    internal static class UnityOffsets
    {
        // ── GameObject ──────────────────────────────────────────────────────
        // IDA: UnityPlayer.dll — function TransferComponents
        //   lea rsi, [rsi+38h]      ← rsi = GameObject* → ComponentArray embedded at +0x38
        // Own-game Unity 6000.3.6f1 = 0x20; EFT adds +0x18 to most base fields → 0x38.
        //
        // IDA: UnityPlayer.dll — function GameObject::Transfer (search string "m_Name")
        //   lea rsi, [rsi+68h]      ← rsi = GameObject* → m_Name native C-string ptr at +0x68
        // Own-game = 0x50; EFT +0x18 → 0x68.
        //
        // NOTE: GO_ObjectClass does NOT exist in Unity 6000.3.6f1.
        // The IL2CPP GCHandle back-ref lives on the Component side (Comp_ObjectClass below).
        // Any code using a GameObject→ObjectClass hop is wrong for this engine version.
        public const uint GO_Components    = 0x38;  // IDA: TransferComponents → lea [rsi+38h]
        public const uint GO_Name          = 0x68;  // IDA: GameObject::Transfer → lea [rsi+68h]

        // ── Component ───────────────────────────────────────────────────────
        // IDA: UnityPlayer.dll — function Object::SetCachedScriptingObject
        //   mov [rbx+28h], rax      ← rbx = Component* → IL2CPP GCHandle / ObjectClass at +0x28
        // Own-game Unity 6000.3.6f1 = 0x18; EFT +0x10 → 0x28.
        //
        // IDA: UnityPlayer.dll — function Component::Transfer (search string "m_GameObject")
        //   lea rcx, [rcx+38h]      ← rcx = Component* → parent GameObject ptr at +0x38
        // Own-game = 0x20; EFT +0x18 → 0x38.
        public const uint Comp_ObjectClass = 0x28;  // IDA: SetCachedScriptingObject → mov [rbx+28h]
        public const uint Comp_GameObject  = 0x38;  // IDA: Component::Transfer → lea [rcx+38h]
        // ── ObjectClass name chain ──────────────────────────────────────────
        public static readonly uint[] ObjClass_ToNamePtr = [0x0, 0x10];

        /// <summary>
        /// 6-element pointer chain: C# object → MonoBehaviour → GameObject → Components → Transform → ObjectClass → TransformInternal.
        /// Works for any MonoBehaviour-derived object (exfils, loot items, etc.).
        /// </summary>
        public static readonly uint[] TransformChain =
        [
            0x10,               // ObjectClass → MonoBehaviour
            Comp_GameObject,    // 0x38 — Component → GameObject (EFT Unity 6000.3.6f1, confirmed IDA)
            GO_Components,      // 0x38 — GameObject → ComponentArray (EFT Unity 6000.3.6f1, confirmed IDA)
            0x08,               // First component entry ptr (Transform) — stride 0x10, ptr at +0x08 (EFT Unity 6000.3.6f1, confirmed IDA)
            Comp_ObjectClass,   // 0x28 — Transform → IL2CPP GCHandle / ObjectClass (EFT Unity 6000.3.6f1, confirmed IDA)
            0x10,               // ObjectClass → TransformInternal
        ];

        // ── ModuleBase (UnityPlayer.dll offsets) ────────────────────────────
        // IDA: UnityPlayer.dll — function FindActiveTransformWithPath (search Exports tab)
        //   mov rcx, cs:qword_1821A4450  → rcx = GOM*  (subtract module base → RVA 0x21A4450)
        //   mov rbx, [rcx+20h]           → first active node (sentinel.NextObjectLink)
        //   lea rdi, [rcx+18h]           → &sentinel embedded inline at GOM+0x18
        //   ; loop:
        //   mov rbx, [rbx+8]             → node.NextObjectLink (+0x08)
        //   mov rdx, [rbx+10h]           → node.ThisObject    (+0x10)
        // GOM sentinel layout:
        //   GOM+0x18 = PreviousObjectLink → last active node
        //   GOM+0x20 = NextObjectLink     → first active node
        //   GOM+0x28 = ThisObject         = 0 (end-of-list)
        //
        // IDA: UnityPlayer.dll — function Camera::GetWorldToCameraMatrix
        //   lea rax, [rcx+88h]           → rcx = Camera* → 4×4 view matrix at +0x88
        // IDA: Camera_CUSTOM_GetGateFittedFieldOfView — take the SECOND movss:
        //   movss xmm0, [rcx+188h]       → FOV float at +0x188
        public const uint GomFallback        = 0x21A4450;  // IDA: FindActiveTransformWithPath → cs:qword_1821A4450 (RVA)
        public const uint AllCameras         = 0x19F3080;  // AllCameras static — UnityPlayer.dll Unity 6000.3.6f1 (EFT)

        // ── ObjectClass helpers ──────────────────────────────────────────────
        public static class ObjectClass
        {
            /// <summary>ObjectClass + 0x10 → MonoBehaviour.</summary>
            public const uint MonoBehaviourOffset = 0x10;
        }

        // ── Camera struct offsets (sig-scanned at runtime, fallback values) ──
        // IDA: UnityPlayer.dll — Camera::GetWorldToCameraMatrix
        //   lea rax, [rcx+88h]              → rcx = Camera* → 4×4 view matrix at +0x88
        // IDA: UnityPlayer.dll — Camera_CUSTOM_GetGateFittedFieldOfView
        //   movss xmm0, [rcx+188h]          → SECOND movss in the function body = FOV float
        // AspectRatio: Camera_Get_Custom_PropAspect; confirmed at runtime by scanning
        //   0x4F0–0x510 for a float matching the screen ratio (1.7778 for 16:9),
        //   then cross-checked against sig-scanner log output.
        public static class Camera
        {
            /// <summary>Camera + 0x88 → 4×4 ViewProjection matrix.
            /// IDA: GetWorldToCameraMatrix → lea rax,[rcx+88h].</summary>
            public static uint ViewMatrix = 0x88;

            /// <summary>Camera + 0x188 → FOV float (degrees).
            /// IDA: GetGateFittedFieldOfView → 2nd movss xmm0,[rcx+188h].</summary>
            public static uint FOV = 0x188;

            /// <summary>Camera + 0x4F8 → Aspect Ratio float.
            /// Confirmed at runtime; no single clean IDA export.</summary>
            public static uint AspectRatio = 0x4F8;

            /// <summary>IsAdded offset after +0x10 dereference.</summary>
            public const uint DerefIsAddedOffset = 0x35;
        }

        // ── Il2Cpp generic List<T> layout ────────────────────────────────────
        public static class List
        {
            /// <summary>Offset from List base to _items (the backing array pointer).</summary>
            public const uint ArrOffset = 0x10;

            /// <summary>Offset from the array base to the first element (element[0]).</summary>
            public const uint ArrStartOffset = 0x20;
        }

        // ── IL2CPP Managed List<T> ──────────────────────────────────────────
        public static class ManagedList
        {
            public const uint ItemsPtr = 0x10;  // Pointer to items array
            public const uint Count = 0x18;     // Count of items (_size)
        }

        // ── IL2CPP Managed Array header ─────────────────────────────────────
        public static class ManagedArray
        {
            public const uint FirstElement = 0x20;  // First element (after header)
            public const int ElementSize = 0x8;     // Size of pointer element
        }

        // ── MongoID struct layout ───────────────────────────────────────────
        public static class MongoID
        {
            public const uint TimeStamp = 0x00;     // uint
            public const uint Counter = 0x08;       // ulong
            public const uint StringID = 0x10;      // string pointer
        }

        // ── HashSet<T> with inline MongoID storage ──────────────────────────
        public static class IL2CPPHashSet
        {
            public const uint Entries = 0x18;           // Pointer to entries array
            public const uint Count = 0x1C;             // int — count
            public const int EntrySize = 0x20;          // 32 bytes per entry
            public const uint EntryValueOffset = 0x08;  // MongoID value starts here

            // Entry layout:
            //   +0x00: hashCode (int) + next (int)
            //   +0x08: MongoID value (struct inline)
            //     +0x00: _timeStamp (uint)
            //     +0x08: _counter (ulong)
            //     +0x10: _stringID (string pointer)
        }

        // ── TransformInternal native layout ──────────────────────────────────
        // IDA: UnityPlayer.dll — exported functions (visible in Exports tab):
        //   TransformAccess_CUSTOM_GetLocalPosition  (sub_1800CF500 in known build)
        //   TransformAccess_CUSTOM_GetLocalRotation  (sub_1800CF580)
        //   TransformAccess_CUSTOM_GetRotation       (sub_1800CF3A0)
        //
        // First instructions of GetLocalPosition:
        //   ; rcx = TransformInternal* (the TransformAccess object)
        //   mov  rax, [rcx+40h]              → rax = TransformHierarchy*   ← HierarchyOffset
        //   movsxd rdx, dword ptr [rcx+48h]  → edx = transform index (int) ← IndexOffset
        //
        // Live runtime dump confirming the pair (player 'Nadeus', Interchange):
        //   nativeTI = 0x1C2842CC140
        //     +0x40 → 0x1C508A320F0   valid heap pointer  (real hierarchy)
        //     +0x48 → 0x36 = 54       small positive int  (sane scene-range index)
        //     +0x58 → nativeTI+0x40   self-referential sub-struct  ← wrong candidate
        //     +0x60 → garbage int     (-1872308 when read as int32) ← wrong candidate
        // The 0x58/0x60 pair landed on an internal sub-struct and produced
        // "taIndex out of range" warnings for every player.
        public static class TransformAccess
        {
            /// <summary>TransformInternal + 0x40 → TransformHierarchy pointer.
            /// IDA: first mov in TransformAccess_CUSTOM_GetLocalPosition → mov rax,[rcx+40h].</summary>
            public const uint HierarchyOffset = 0x40;

            /// <summary>TransformInternal + 0x48 → int32 index into hierarchy arrays.
            /// IDA: second instr in GetLocalPosition → movsxd rdx,dword ptr [rcx+48h].</summary>
            public const uint IndexOffset = 0x48;
        }

        // ── TransformHierarchy native layout ─────────────────────────────────
        // IDA: UnityPlayer.dll — TransformAccess_CUSTOM_GetLocalPosition
        //   ; rax = TransformHierarchy* (loaded via HierarchyOffset above)
        //   mov  rcx, [rax+88h]          → rcx = TrsX* (vertices array)  ← VerticesOffset
        //   imul rdx, rdx, 30h           → confirms TrsX stride = 0x30 (48 bytes)
        //   movaps xmm0, [rcx+rdx]       → loads T at vertex+0x00
        //   movaps xmm1, [rcx+rdx+10h]   → loads Q at vertex+0x10
        //   movaps xmm2, [rcx+rdx+20h]   → loads S at vertex+0x20
        //
        // IDA: UnityPlayer.dll — TransformAccess_CUSTOM_GetRotation
        //   mov  rcx, [rax+70h]          → rcx = int* (parent indices array) ← IndicesOffset
        //   mov  edx, [rcx+rdx*4]        → edx = parentIndices[index]  (int[] stride confirmed)
        //   ; loop until edx == -1 (root sentinel)
        //
        // Cross-check against own-game Unity 6000.3.6f1:
        //   own-game: vertices=[h+0x18]  parents=[h+0x20]
        //   EFT:      vertices=[h+0x88]  parents=[h+0x70]
        // EFT's UnityPlayer is statically linked with extra modules that push field offsets up.
        //
        // The hierarchy's cached world-TRS slot is intentionally not exposed:
        // Unity does not refresh it every frame for every actor, causing multi-second
        // position freezes. World positions are computed from the live TrsX[] via
        // TrsX.ComputeWorldPosition() instead.
        public static class TransformHierarchy
        {
            /// <summary>TransformHierarchy + 0x70 → int[] parent indices array.
            /// IDA: GetRotation → mov rcx,[rax+70h] feeding the parent-walk loop.</summary>
            public const uint IndicesOffset = 0x70;

            /// <summary>TransformHierarchy + 0x88 → TrsX[] vertices array.
            /// IDA: GetLocalPosition → mov rcx,[rax+88h]; imul rdx,rdx,30h confirms stride.</summary>
            public const uint VerticesOffset = 0x88;
        }

        // ── Unity Animator ────────────────────────────────────────────────────
        public static class UnityAnimator
        {
            /// <summary>Animator.m_Speed field offset.</summary>
            public const uint Speed = 0x4B0;
        }

        // ── LevelSettings pointer chain ──────────────────────────────────────
        public static class LevelSettings
        {
            /// <summary>
            /// Chain from a "---Custom_levelsettings---" GameObject to the managed LevelSettings instance.
            /// GO → +GO_Components(0x38) → second component (+0x18) → +Comp_ObjectClass(0x28) → ObjectClass.
            /// </summary>
            public static readonly uint[] LevelSettingsChain =
            [
                GO_Components,      // 0x38 — GameObject → ComponentArray
                0x18,               // Second component (LevelSettings)
                Comp_ObjectClass,   // 0x28 — Component → ObjectClass
            ];
        }

        /// <summary>
        /// Reads the world-space position of a Unity transform from its <c>TransformInternal</c> pointer.
        /// Shared helper used by <c>Exfil</c>, <c>BtrTracker</c>, and any other code that holds a
        /// resolved <c>TransformInternal</c> (end of the standard 6-step <see cref="TransformChain"/>).
        /// Returns <see cref="Vector3.Zero"/> on any read failure.
        /// </summary>
        internal static Vector3 ReadWorldPosition(ulong transformInternal)
        {
            try
            {
                var hierarchy = Memory.ReadValue<ulong>(transformInternal + TransformAccess.HierarchyOffset);
                if (!SilkUtils.IsValidVirtualAddress(hierarchy))
                    return Vector3.Zero;

                var index = Memory.ReadValue<int>(transformInternal + TransformAccess.IndexOffset);
                if (index < 0 || index > 150_000)
                    return Vector3.Zero;

                var verticesPtr = Memory.ReadValue<ulong>(hierarchy + TransformHierarchy.VerticesOffset);
                var indicesPtr  = Memory.ReadValue<ulong>(hierarchy + TransformHierarchy.IndicesOffset);
                if (!SilkUtils.IsValidVirtualAddress(verticesPtr) || !SilkUtils.IsValidVirtualAddress(indicesPtr))
                    return Vector3.Zero;

                int count    = index + 1;
                var vertices = Memory.ReadArray<TrsX>(verticesPtr, count);
                var indices  = Memory.ReadArray<int>(indicesPtr, count);

                if (vertices.Length < count || indices.Length < count)
                    return Vector3.Zero;

                return TrsX.ComputeWorldPosition(vertices, indices, index);
            }
            catch
            {
                return Vector3.Zero;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// TRS element in a Unity TransformHierarchy vertices array.
    /// Layout: Translation(Vector3) + pad(float) + Rotation(Quaternion) + Scale(Vector3) + pad(float) = 48 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct TrsX
    {
        public readonly Vector3 T;        // translation (12 bytes)
        private readonly float _pad0;     // padding (4 bytes)
        public readonly Quaternion Q;     // rotation (16 bytes)
        public readonly Vector3 S;        // scale (12 bytes)
        private readonly float _pad1;     // padding (4 bytes)

        /// <summary>
        /// Walks the transform hierarchy from pre-read vertex/index data and returns the world position.
        /// Pure math — no DMA reads. Returns the raw computed position; callers validate as needed.
        /// </summary>
        internal static Vector3 ComputeWorldPosition(
            ReadOnlySpan<TrsX> vertices,
            ReadOnlySpan<int> parentIndices,
            int index,
            int maxIterations = 4096)
        {
            var pos = vertices[index].T;
            int parent = parentIndices[index];
            int iter = 0;

            while (parent >= 0 && parent < vertices.Length && iter++ < maxIterations)
            {
                ref readonly var p = ref vertices[parent];
                pos = Vector3.Transform(pos, p.Q);
                pos *= p.S;
                pos += p.T;
                parent = parentIndices[parent];
            }

            return pos;
        }

        /// <summary>
        /// Walks the transform hierarchy and returns the accumulated world-space rotation quaternion.
        /// </summary>
        internal static Quaternion ComputeWorldRotation(
            ReadOnlySpan<TrsX> vertices,
            ReadOnlySpan<int> parentIndices,
            int index,
            int maxIterations = 4096)
        {
            var rot = vertices[index].Q;
            int parent = parentIndices[index];
            int iter = 0;

            while (parent >= 0 && parent < vertices.Length && iter++ < maxIterations)
            {
                rot = Quaternion.Multiply(vertices[parent].Q, rot);
                parent = parentIndices[parent];
            }

            return rot;
        }

        /// <summary>
        /// Extracts the Y-axis rotation (yaw) in degrees from the accumulated world rotation quaternion.
        /// </summary>
        internal static float ComputeWorldYawDeg(
            ReadOnlySpan<TrsX> vertices,
            ReadOnlySpan<int> parentIndices,
            int index)
        {
            var q = ComputeWorldRotation(vertices, parentIndices, index);
            // Yaw around Unity Y axis: atan2(2(wy+xz), 1-2(yy+zz))  — standard formula
            float yaw = MathF.Atan2(2f * (q.W * q.Y + q.X * q.Z),
                                    1f - 2f * (q.Y * q.Y + q.Z * q.Z));
            return yaw * (180f / MathF.PI);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors the IL2CPP LinkedListObject struct (Sequential, Pack=8).
    /// [0x00] PreviousObjectLink
    /// [0x08] NextObjectLink
    /// [0x10] ThisObject (the actual GameObject ptr)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal readonly struct LinkedListObject
    {
        public readonly ulong PreviousObjectLink;
        public readonly ulong NextObjectLink;
        public readonly ulong ThisObject;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// IL2CPP ComponentArray layout — embedded inside a GameObject at offset <see cref="UnityOffsets.GO_Components"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct ComponentArray
    {
        public readonly ulong ArrayBase;
        public readonly ulong MemLabelId;
        public readonly ulong Size;
        public readonly ulong Capacity;

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        internal readonly struct Entry
        {
            [FieldOffset(0x8)]
            public readonly ulong Component;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors the IL2CPP GameObject struct layout.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct GameObject
    {
        [FieldOffset(0x08)]
        public readonly int InstanceID;

        [FieldOffset((int)UnityOffsets.GO_Components)]
        public readonly ComponentArray Components;

        [FieldOffset((int)UnityOffsets.GO_Name)]
        public readonly ulong NamePtr;
    }

    // ─────────────────────────────────────────────────────────────────────────────

    internal readonly struct GOM
    {
        private const int MaxWalkNodes = 100_000;

        // IDA: UnityPlayer.dll — FindActiveTransformWithPath (Exports tab)
        //   mov rcx, cs:qword_1821A4450  → GOM* global (RVA 0x21A4450)
        //   mov rbx, [rcx+20h]           → first active node = sentinel.NextObjectLink
        //   lea rdi, [rcx+18h]           → &sentinel embedded inline at GOM+0x18
        //   loop:
        //     mov rbx, [rbx+8]           → node.NextObjectLink (+0x08)
        //     mov rdx, [rbx+10h]         → node.ThisObject    (+0x10)
        // Confirmed identical in own-game Unity 6000.3.6f1 (named symbols available).
        // Sentinel LinkedListObject is embedded inline:
        //   GOM+0x18 = PreviousObjectLink → last  active node
        //   GOM+0x20 = NextObjectLink     → first active node
        //   GOM+0x28 = ThisObject         = 0     (end-of-list marker)
        private const uint FirstNodeOffset = 0x20;  // GOM+0x20 = sentinel.NextObjectLink = first node
        private const uint LastNodeOffset  = 0x18;  // GOM+0x18 = sentinel.PreviousObjectLink = last node

        public readonly ulong FirstNode;  // ptr to first active linked-list node
        public readonly ulong LastNode;   // ptr to last active linked-list node

        private GOM(ulong firstNode, ulong lastNode)
        {
            FirstNode = firstNode;
            LastNode  = lastNode;
        }

        // ── Name cache ───────────────────────────────────────────────────────
        private static readonly Dictionary<string, ulong> _nameCache = new();
        private static readonly Lock _cacheLock = new();

        public static void ClearCache() { lock (_cacheLock) _nameCache.Clear(); }

        // ── Cached resolved addresses ────────────────────────────────────────
        private static ulong _cachedGomAddr;

        // GOM layout confirmed via EFT + own-game Unity 6000.3.6f1 IDA — no runtime probe needed.
        public static GOM Get(ulong gomAddress)
        {
            if (!SilkUtils.IsValidVirtualAddress(gomAddress))
                return default;

            if (!Memory.TryReadValue<ulong>(gomAddress + FirstNodeOffset, out var firstNode, false))
                return default;
            if (!SilkUtils.IsValidVirtualAddress(firstNode))
                return default;
            if (!Memory.TryReadValue<ulong>(gomAddress + LastNodeOffset, out var lastNode, false))
                return default;
            if (!SilkUtils.IsValidVirtualAddress(lastNode))
                return default;

            return new GOM(firstNode, lastNode);
        }

        // ── GOM address resolution ────────────────────────────────────────────

        // ── Direct signatures: mov [rip+rel32] / mov reg,[rip+rel32] ─────
        // These reference the GOM global directly via a RIP-relative operand.
        // (Sig, RelOffset, InstrLen, Desc)
        private static readonly (string Sig, int RelOff, int InstrLen, string Desc)[] GomDirectSigs =
        [
            // mov [rip+rel32], rax — GOM init store
            ("48 89 05 ? ? ? ? 48 83 C4 ? C3 33 C9", 3, 7, "mov [rip+rel32],rax (GOM init store)"),
            // mov [rip+rel32], rbp — GOM store variant
            ("48 89 2D ? ? ? ? 48 8B 6C 24 ? 48 83 C4 ? 5E C3 33 ED", 3, 7, "mov [rip+rel32],rbp (GOM store)"),
            // mov rsi, [rip+rel32] — GOM read
            ("48 8B 35 ? ? ? ? 48 85 F6 0F 84 ? ? ? ? 8B 46", 3, 7, "mov rsi,[rip+rel32] (GOM read)"),
            // mov rdx, [rip+rel32] — GOM read variant
            ("48 8B 15 ? ? ? ? 48 83 C2 ? 48 3B DA", 3, 7, "mov rdx,[rip+rel32] (GOM read)"),
            // mov rcx, [rip+rel32] — GOM read variant
            ("48 8B 0D ? ? ? ? 4C 8D 4C 24 ? 4C 8D 44 24 ? 89 44 24", 3, 7, "mov rcx,[rip+rel32] (GOM read)"),
        ];

        // ── Call-site signatures: E8 rel32 → sub_180A40AB0 (GOM getter) ──
        // These are call-site patterns that invoke the GOM getter function
        // (sub_180A40AB0: mov rax, cs:qword_181A233A0; retn).
        // We resolve the E8 target, then read the getter body to find the global.
        // (Sig, RelOffset, InstrLen, Desc)
        private static readonly (string Sig, int RelOff, int InstrLen, string Desc)[] GomCallSiteSigs =
        [
            ("E8 ? ? ? ? 4C 8D 45 ? 89 5D ? 48 8D 55", 1, 5, "call GomGetter (variant 1)"),
            ("E8 ? ? ? ? 8B 48 ? ? ? ? ? ? ? ? 48 8D 77", 1, 5, "call GomGetter (variant 2)"),
            ("E8 ? ? ? ? 48 8B 58 ? 48 8D 78 ? 48 3B DF 74 ? ? ? ? 48 8B 53", 1, 5, "call GomGetter (variant 3)"),
            ("E8 ? ? ? ? 8B 48 ? ? ? ? ? ? ? ? 48 8D 6F", 1, 5, "call GomGetter (variant 4)"),
            ("E8 ? ? ? ? 48 8B 58 ? 48 8D 78 ? 48 3B DF 74 ? 66 66 66 0F 1F 84 00", 1, 5, "call GomGetter (variant 5)"),
            ("E8 ? ? ? ? 4C 8D 44 24 ? C7 44 24 ? ? ? ? ? 48 8D 54 24 ? 48 8B C8", 1, 5, "call GomGetter (variant 6)"),
            ("E8 ? ? ? ? 4C 8D 44 24 ? 89 5C 24 ? 48 8D 54 24", 1, 5, "call GomGetter (variant 7)"),
        ];

        // ── Broad signatures: short patterns that may match many sites ─────
        // Generic mov [rip+rel32] store patterns — extremely patch-resilient.
        // Will match 100+ locations, so we use FindSignatures (multi-match)
        // and validate each candidate with IsValidGomPtr().
        // (Sig, RelOffset, InstrLen, Desc)
        private static readonly (string Sig, int RelOff, int InstrLen, string Desc)[] GomBroadSigs =
        [
            // mov [rip+rel32],reg; add rsp,imm8 — generic store before epilogue
            ("48 89 05 ? ? ? ? 48 83 C4", 3, 7, "mov [rip+rel32],rax; add rsp (broad)"),
        ];

        private const int BroadSigMaxMatches = 256;

        public static ulong GetAddr(ulong unityBase)
        {
            if (SilkUtils.IsValidVirtualAddress(_cachedGomAddr))
                return _cachedGomAddr;

            // Phase 1: Try direct mov [rip+rel32] signatures — read the GOM global directly
            foreach (var (sig, relOff, instrLen, desc) in GomDirectSigs)
            {
                try
                {
                    ulong addr = Memory.FindSignature(sig, "UnityPlayer.dll");
                    if (!SilkUtils.IsValidVirtualAddress(addr))
                        continue;

                    int rva = Memory.ReadValue<int>(addr + (ulong)relOff, false);
                    ulong ptr = Memory.ReadPtr(addr + (ulong)instrLen + (ulong)rva, false);
                    if (SilkUtils.IsValidVirtualAddress(ptr))
                    {
                        Log.WriteLine($"[GOM] Located via direct sig: {desc}");
                        _cachedGomAddr = ptr;
                        return ptr;
                    }
                }
                catch { }
            }

            // Phase 2: Try E8 call-site signatures — resolve call target then read getter body
            foreach (var (sig, relOff, instrLen, desc) in GomCallSiteSigs)
            {
                try
                {
                    ulong callAddr = Memory.FindSignature(sig, "UnityPlayer.dll");
                    if (!SilkUtils.IsValidVirtualAddress(callAddr))
                        continue;

                    int callRel = Memory.ReadValue<int>(callAddr + (ulong)relOff, false);
                    ulong targetFunc = callAddr + (ulong)instrLen + (ulong)callRel;

                    if (!SilkUtils.IsValidVirtualAddress(targetFunc))
                        continue;

                    if (TryResolveGetterGlobal(targetFunc, out var globalPtr))
                    {
                        Log.WriteLine($"[GOM] Located via call-site sig: {desc}");
                        _cachedGomAddr = globalPtr;
                        return globalPtr;
                    }
                }
                catch { }
            }

            // Phase 3: Try broad/generic signatures (multi-match with validation)
            foreach (var (sig, relOff, instrLen, desc) in GomBroadSigs)
            {
                try
                {
                    var matches = Memory.FindSignatures(sig, "UnityPlayer.dll", BroadSigMaxMatches);
                    foreach (var addr in matches)
                    {
                        if (!SilkUtils.IsValidVirtualAddress(addr))
                            continue;

                        int rva = Memory.ReadValue<int>(addr + (ulong)relOff, false);
                        ulong ptr = addr + (ulong)instrLen + (ulong)rva;

                        if (!Memory.TryReadPtr(ptr, out var gomAddr, false))
                            continue;

                        if (IsValidGomPtr(gomAddr))
                        {
                            Log.WriteLine($"[GOM] Located via broad sig: {desc} (matched {matches.Length} sites)");
                            _cachedGomAddr = gomAddr;
                            return gomAddr;
                        }
                    }
                }
                catch { }
            }

            // Phase 4: Fallback — hardcoded offset
            try
            {
                ulong fallback = Memory.ReadPtr(unityBase + UnityOffsets.GomFallback, false);
                if (SilkUtils.IsValidVirtualAddress(fallback))
                {
                    Log.WriteLine("[GOM] Located via hardcoded offset");
                    _cachedGomAddr = fallback;
                    return fallback;
                }
            }
            catch { }

            throw new InvalidOperationException("Failed to locate GameObjectManager");
        }

        /// <summary>
        /// Reads the first 7 bytes of a getter function and checks for:
        ///   48 8B 05 XX XX XX XX  (mov rax, [rip+rel32])
        /// If matched, resolves the RIP-relative global and dereferences it.
        /// </summary>
        private static bool TryResolveGetterGlobal(ulong funcAddr, out ulong result)
        {
            result = 0;
            Span<byte> header = stackalloc byte[7];
            if (!Memory.TryReadBuffer(funcAddr, header, false))
                return false;

            // 48 8B 05 = REX.W mov rax, [rip+rel32]
            if (header[0] != 0x48 || header[1] != 0x8B || header[2] != 0x05)
                return false;

            int innerRel = BitConverter.ToInt32(header[3..]);
            ulong globalAddr = funcAddr + 7 + (ulong)innerRel;

            if (!Memory.TryReadPtr(globalAddr, out result, false))
                return false;

            return SilkUtils.IsValidVirtualAddress(result);
        }

        /// <summary>
        /// Validates that <paramref name="ptr"/> points to a plausible GOM struct.
        /// Confirmed layout (EFT + own-game Unity 6000.3.6f1 IDA):
        ///   GOM+0x18 = embedded sentinel node (end-of-list marker)
        ///   GOM+0x20 = ActiveNodes (ptr to first linked-list node)
        ///   node+0x10 = ThisObject (GameObject ptr)
        /// </summary>
        private static bool IsValidGomPtr(ulong ptr)
        {
            if (!SilkUtils.IsValidVirtualAddress(ptr))
                return false;

            if (!Memory.TryReadValue<ulong>(ptr + FirstNodeOffset, out var firstNode, false))
                return false;
            if (!SilkUtils.IsValidVirtualAddress(firstNode))
                return false;

            // Probe the first node — it should have a valid ThisObject
            if (!Memory.TryReadValue<LinkedListObject>(firstNode, out var firstLlo, false))
                return false;

            return SilkUtils.IsValidVirtualAddress(firstLlo.ThisObject);
        }

        internal static void ResetCachedAddresses()
        {
            _cachedGomAddr = 0;
            ClearCache();
        }

        // ── Linked-list walk ──────────────────────────────────────────────────

        public static ulong GetGameObjectByName(string name, bool ignoreCase = true, bool useCache = true)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            if (useCache)
            {
                lock (_cacheLock)
                {
                    if (_nameCache.TryGetValue(name, out var cached) && SilkUtils.IsValidVirtualAddress(cached))
                        return cached;
                }
            }
            var gom = Get(Memory.GOM);
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (!Memory.TryReadValue<LinkedListObject>(gom.FirstNode, out var first, false)) return 0;
            if (!Memory.TryReadValue<LinkedListObject>(gom.LastNode,  out var last,  false)) return 0;
            ulong result = WalkList(first, last, forward: true,
                (node) => MatchName(node.ThisObject, name, comparison) ? node.ThisObject : 0);
            if (result == 0)
                result = WalkList(last, first, forward: false,
                    (node) => MatchName(node.ThisObject, name, comparison) ? node.ThisObject : 0);
            if (SilkUtils.IsValidVirtualAddress(result) && useCache)
            {
                lock (_cacheLock)
                    _nameCache[name] = result;
            }
            return result;
        }

        public static ulong FindBehaviourByKlassPtr(ulong klassPtr)
        {
            if (!SilkUtils.IsValidVirtualAddress(klassPtr)) return 0;
            var gom = Get(Memory.GOM);
            if (!Memory.TryReadValue<LinkedListObject>(gom.FirstNode, out var first, true)) return 0;
            if (!Memory.TryReadValue<LinkedListObject>(gom.LastNode,  out var last,  true)) return 0;
            ulong result = WalkList(first, last, forward: true,
                node => GetComponentByKlassPtr(node.ThisObject, klassPtr), useCache: true);
            if (result == 0)
                result = WalkList(last, first, forward: false,
                    node => GetComponentByKlassPtr(node.ThisObject, klassPtr), useCache: true);
            return result;
        }

        public static ulong FindBehaviourByClassName(string className)
        {
            if (string.IsNullOrEmpty(className)) return 0;
            var gom = Get(Memory.GOM);
            if (!Memory.TryReadValue<LinkedListObject>(gom.FirstNode, out var first, true)) return 0;
            if (!Memory.TryReadValue<LinkedListObject>(gom.LastNode,  out var last,  true)) return 0;
            ulong result = WalkList(first, last, forward: true,
                node => GetComponentByClassName(node.ThisObject, className), useCache: true);
            if (result == 0)
                result = WalkList(last, first, forward: false,
                    node => GetComponentByClassName(node.ThisObject, className), useCache: true);
            return result;
        }

        /// <summary>
        /// Searches a single GameObject's component array for a component with a matching klass pointer.
        /// Returns the objectClass pointer, or 0.
        /// </summary>
        private static ulong GetComponentByKlassPtr(ulong gameObject, ulong klassPtr)
        {
            if (!Memory.TryReadValue<GameObject>(gameObject, out var go, true))
                return 0;

            ref readonly var compArr = ref go.Components;
            if (!SilkUtils.IsValidVirtualAddress(compArr.ArrayBase) || compArr.Size == 0)
                return 0;

            int count = (int)Math.Min(compArr.Size, 0x400);
            Span<ComponentArray.Entry> entries = count <= 64
                ? stackalloc ComponentArray.Entry[count]
                : new ComponentArray.Entry[count];

            if (!Memory.TryReadBuffer(compArr.ArrayBase, entries, true))
                return 0;

            for (int i = 0; i < count; i++)
            {
                var compPtr = entries[i].Component;
                if (!SilkUtils.IsValidVirtualAddress(compPtr))
                    continue;

                if (!Memory.TryReadPtr(compPtr + UnityOffsets.Comp_ObjectClass, out var objectClass, true)
                    || !SilkUtils.IsValidVirtualAddress(objectClass))
                    continue;

                if (!Memory.TryReadPtr(objectClass, out var klass, true))
                    continue;

                if (klass == klassPtr)
                    return objectClass;
            }
            return 0;
        }

        /// <summary>
        /// Searches a single GameObject's component array for a component whose IL2CPP class name matches.
        /// Returns the objectClass pointer, or 0.
        /// Uses read caching by default — class names are stable within a session and
        /// caching avoids thousands of redundant DMA reads when walking the GOM
        /// (many GameObjects share the same component types like Transform, MeshRenderer, etc.).
        /// </summary>
        private static ulong GetComponentByClassName(ulong gameObject, string className)
        {
            if (!Memory.TryReadValue<GameObject>(gameObject, out var go, true))
                return 0;

            ref readonly var compArr = ref go.Components;
            if (!SilkUtils.IsValidVirtualAddress(compArr.ArrayBase) || compArr.Size == 0)
                return 0;

            int count = (int)Math.Min(compArr.Size, 0x400);
            Span<ComponentArray.Entry> entries = count <= 64
                ? stackalloc ComponentArray.Entry[count]
                : new ComponentArray.Entry[count];

            if (!Memory.TryReadBuffer(compArr.ArrayBase, entries, true))
                return 0;

            for (int i = 0; i < count; i++)
            {
                var compPtr = entries[i].Component;
                if (!SilkUtils.IsValidVirtualAddress(compPtr))
                    continue;

                if (!Memory.TryReadPtr(compPtr + UnityOffsets.Comp_ObjectClass, out var objectClass, true)
                    || !SilkUtils.IsValidVirtualAddress(objectClass))
                    continue;

                var name = Il2CppClass.ReadName(objectClass, useCache: true);
                if (name is not null && name.Equals(className, StringComparison.Ordinal))
                    return objectClass;
            }
            return 0;
        }

        /// <summary>
        /// Given a behaviour/component pointer, navigates to its parent GameObject via
        /// <c>Comp_GameObject</c> (0x10) and then searches that single GameObject's component
        /// array for a component whose IL2CPP class name matches <paramref name="className"/>.
        /// Returns the objectClass pointer of the matching component, or 0.
        /// </summary>
        public static ulong GetComponentFromBehaviour(ulong behaviour, string className)
        {
            if (!SilkUtils.IsValidVirtualAddress(behaviour))
                return 0;

            if (!Memory.TryReadPtr(behaviour + UnityOffsets.Comp_GameObject, out var gameObject, false)
                || !SilkUtils.IsValidVirtualAddress(gameObject))
                return 0;

            return GetComponentByClassName(gameObject, className);
        }

        // ── Generic linked-list walker ───────────────────────────────────────

        /// <summary>
        /// Walks the GOM linked list from <paramref name="start"/> toward <paramref name="end"/>.
        /// For each valid node, invokes <paramref name="visitor"/>. If the visitor returns
        /// a non-zero value, the walk stops and that value is returned.
        /// </summary>
        private static ulong WalkList(
            LinkedListObject start,
            LinkedListObject end,
            bool forward,
            Func<LinkedListObject, ulong> visitor,
            bool useCache = false)
        {
            var current = start;
            for (int i = 0; i < MaxWalkNodes; i++)
            {
                if (!SilkUtils.IsValidVirtualAddress(current.ThisObject))
                    break;

                var hit = visitor(current);
                if (SilkUtils.IsValidVirtualAddress(hit))
                    return hit;

                if (current.ThisObject == end.ThisObject)
                    break;

                var nextLink = forward ? current.NextObjectLink : current.PreviousObjectLink;
                if (!Memory.TryReadValue<LinkedListObject>(nextLink, out current, useCache))
                    break;
            }
            return 0;
        }

        private static bool MatchName(ulong gameObject, string name, StringComparison comparison)
        {
            if (!Memory.TryReadValue<ulong>(gameObject + UnityOffsets.GO_Name, out var namePtr, false))
                return false;
            if (!SilkUtils.IsValidVirtualAddress(namePtr))
                return false;
            return Memory.TryReadString(namePtr, out var goName, 64, false)
                && goName is not null
                && goName.Contains(name, comparison);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the IL2CPP class name from a Unity object.
    /// Chain: objectClass → [0x0, 0x10] → name C-string
    /// </summary>
    internal static class Il2CppClass
    {
        /// <summary>
        /// Returns the IL2CPP class name for <paramref name="objectClass"/>, or null on failure.
        /// </summary>
        public static string? ReadName(ulong objectClass, int maxLength = 64, bool useCache = false)
        {
            if (!Memory.TryReadPtrChain(objectClass, UnityOffsets.ObjClass_ToNamePtr, out ulong namePtr, useCache))
                return null;
            if (!SilkUtils.IsValidVirtualAddress(namePtr))
                return null;
            return Memory.TryReadString(namePtr, out var name, maxLength, useCache) ? name : null;
        }
    }
}
