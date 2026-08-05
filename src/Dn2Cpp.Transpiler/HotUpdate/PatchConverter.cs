using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

/// <summary>The <c>--emit-patch</c> driver: bakes a managed patch assembly into
/// a Baked Patch Image (docs/BPI-FORMAT.md) for the hot-update interpreter. The
/// converter is an ordinary build-time consumer of <see cref="Compilation"/> —
/// metadata loading, signature decoding, and reachability run unchanged; only
/// the results are re-encoded, so the normal transpile pipeline carries no
/// patch awareness.
///
/// The supported patch surface — the type/member/body fence, what each shape
/// bakes into, and what stays outside — is specified in docs/BPI-FORMAT.md
/// §"Conversion surface (the patch fence)" (with §"v1 carve-outs" for the
/// boundary list). Everything outside the fence throws
/// <see cref="NotSupportedException"/> at conversion.</summary>
internal static class PatchConverter
{
    /// <summary>Operand-kind hint values baked into arithmetic/comparison/
    /// conversion/branch instructions (docs/BPI-FORMAT.md "Operand-kind
    /// hints"): the abstract eval-stack type decided by the converter, so the
    /// interpreter never infers operand types at run time. Signedness is not
    /// part of the hint — the opcode itself (.un forms, conv.u*) carries it.</summary>
    private enum SlotKind : uint
    {
        I32 = 0,
        I64 = 1,
        F32 = 2,
        F64 = 3,
        Ref = 4,
    }

    /// <summary>Array element storage kinds baked into the canonical
    /// `newarr`/`ldelem`/`stelem` instructions (docs/BPI-FORMAT.md): the
    /// element's byte width, signedness for sub-int32 loads, and the array
    /// representation the interpreter accesses (i4 = the int32 array layout,
    /// ref = the reference array layout, everything else the packed
    /// element-sized layout — the same representations the AOT lane
    /// allocates, so arrays flow across the boundary without conversion).</summary>
    private enum ElemKind : uint
    {
        I1 = 0,
        U1 = 1,
        I2 = 2,
        U2 = 3,
        I4 = 4,
        I8 = 5,
        R4 = 6,
        R8 = 7,
        Ref = 8,
    }

    /// <summary>The frame shape of one baked method: eval-stack kinds for its
    /// arguments and locals plus the LocalSigTable EntityRefs (args then
    /// locals) and the return kind (null = void).</summary>
    private sealed class FrameShape
    {
        public required SlotKind[] ArgKinds;
        public required SlotKind[] LocalKinds;
        public required uint[] SlotRefs;
        public required SlotKind? ReturnKind;
    }

    /// <summary>A resolved external call target: its import index plus the
    /// shape the caller's stack simulation needs.</summary>
    private sealed class ImportCall
    {
        public required uint ImportIdx;
        public required bool IsInstance;
        public required SlotKind[] ArgKinds;
        public required SlotKind? RetKind;
    }

    /// <summary>One exception region normalized to instruction-index ranges
    /// (half-open), kept in metadata order — ECMA-335 orders the table
    /// innermost-first, which is the order the interpreter's linear EH scan
    /// relies on. Kind values match the EHTable encoding (0=catch / 2=finally /
    /// 3=fault; filters are fenced out).</summary>
    private sealed class EHRegion
    {
        public required uint Kind;
        public required uint TryStart;
        public required uint TryEnd;
        public required uint HandlerStart;
        public required uint HandlerEnd;
        public required uint CatchTypeRef;
    }

    // EntityRef encodings (docs/BPI-FORMAT.md "Tagged reference EntityRef").
    private static uint PrimRef(PrimitiveTypeCode code) => (2u << 30) | (uint)code;
    private static uint ImportRef(uint importIdx) => (1u << 30) | importIdx;

    /// <summary>TypeRecord.flags interp-specific bit: the type has a static
    /// constructor, baked as the FIRST entry of its MethodTable run
    /// (docs/BPI-FORMAT.md "TypeTable") so the runtime's lazy-cctor guard
    /// finds it without a load-time name scan.</summary>
    private const uint TypeFlagHasCctor = 0x80000000u;

    /// <summary>One baked patch field: its FieldTable index (the PatchEntity
    /// ld/st operand), the declaring TypeTable index (the instruction's `b`
    /// operand — the runtime cctor-guard anchor for statics), its staticness,
    /// and the field's eval-stack kind for the caller's stack simulation.</summary>
    private sealed class PatchField
    {
        public required uint FieldIdx;
        public required uint TypeIdx;
        public required bool IsStatic;
        public required SlotKind Kind;
    }

    /// <summary>Header flag bit0 (docs/BPI-FORMAT.md "Header"): the CodeSection
    /// carries the register code format (mirrors DN2CPP_BPI_FLAG_REGCODE).</summary>
    private const uint BpiFlagRegCode = 0x1u;

    public static int Run(string patchDll, IReadOnlyList<string> references, string baseAbiPath, string outDir, uint patchVersion = 0, bool regCode = false)
    {
        if (!File.Exists(patchDll))
        {
            Console.Error.WriteLine($"error: patch assembly not found: {patchDll}");
            return 1;
        }
        if (string.IsNullOrEmpty(baseAbiPath) || !File.Exists(baseAbiPath))
        {
            Console.Error.WriteLine($"error: --emit-patch requires --base-abi <base-abi.json> (a --hotupdate-base build's sidecar): {baseAbiPath}");
            return 1;
        }

        try
        {
            string manifestJson = File.ReadAllText(baseAbiPath);
            ulong baseHash = ReadManifestHash(manifestJson);
            var baseVtables = ReadManifestVtables(manifestJson);
            var baseInterfaces = ReadManifestInterfaces(manifestJson);
            var baseInstantiations = ReadManifestInstantiations(manifestJson);
            var baseDefaultRefs = ReadManifestDefaultRefs(manifestJson);

            var paths = new List<string> { patchDll };
            paths.AddRange(references);
            // Before anything is loaded: a shim in the -r set that the base image
            // does not carry is refused here, naming the shim. Loading it first is
            // what makes that mistake unreadable.
            CheckDefaultRefSymmetry(paths, baseDefaultRefs);
            // A closed generic instantiation of a base-image type resolves to its
            // mangled registry name against the manifest — the converter never
            // loads the base, so a base-image generic type is bound by name like
            // any external reference type (docs/BPI-FORMAT.md "instantiations").
            // The resolver must be live before Build() decodes patch signatures.
            // HotUpdatePosture: the patch model must keep every branch arm — the
            // typeof fold could otherwise prune a call the interpreted patch body
            // still takes at run time.
            // SharedGenerics off: the converter emits no C++ — its model feeds the
            // BPI writer — but sharing flips ClassInfo.ShareStructLayout, and the
            // mangled names baked into the patch image must not change.
            // DefaultRefDir unset, and that is a decision rather than an accident: the
            // patch's model must be a copy of the base build's -r set, so the load set
            // here is exactly what the caller passed. Injecting a shipped shim would let
            // a patch name resolve against a class that was never in the base image, and
            // the baked import would then bind against nothing at load time.
            //
            // The BASE build does inject, so the two load sets are asymmetric by exactly
            // the shims its trigger assemblies pulled in. That direction is inert: a patch
            // reaches the base only through name-based imports and never re-lowers a base
            // body, so an injected-but-unreached shim emits no type and moves neither the
            // ABI hash nor the BPI. The reverse direction is not, and
            // CheckDefaultRefSymmetry rejects it above against the sidecar's record.
            var compilation = Compilation.Create(paths, new TranspileOptions
            {
                ExternalGenericResolver =
                    (openDef, args) => ResolveExternalGeneric(baseInstantiations, openDef, args),
                HotUpdatePosture = true,
                SharedGenerics = false,
            });

            var w = new BpiWriter();
            // Import dedupe: type imports by FullName; method imports by
            // declaring type + name + staticness + sigShape; field imports by
            // declaring type + name.
            var typeImports = new Dictionary<string, uint>(StringComparer.Ordinal);
            var methodImports = new Dictionary<string, uint>(StringComparer.Ordinal);
            var fieldImports = new Dictionary<string, uint>(StringComparer.Ordinal);
            uint entryMethodIdx = 0xFFFFFFFFu;

            // Pass 1: fence every baked type/method and assign its
            // TypeTable/MethodTable index up front, so bodies can reference
            // forward (an intra-patch call operand is a method-table index; a
            // patch-typed slot reference is a type-table index). A `.cctor`
            // goes FIRST in its type's run (the has-cctor TypeRecord flag
            // points the runtime's lazy guard at MethodTable[methodStart]) and
            // is baked whenever it has a body — lazy initialization must run
            // even when no reachability edge reaches the initializer itself.
            var collected = new List<(ClassInfo Class, List<MethodInfo> Baked, bool HasCctor)>();
            // Per patch type: the base-image interfaces it implements, each with
            // the (slot, interpreted implementing method) pairs. Computed here so
            // FenceType can exempt interface-implementation methods from the
            // new-virtual-slot fence, and pass 2 can bake the ItfDesc run.
            var itfImplsByType = new Dictionary<ClassInfo, List<(string ItfName, List<(uint Slot, MethodInfo Impl)> Slots)>>();
            // Snapshot. Reading a specialization's .Methods decodes it, decoding instantiates
            // the generics its signatures name, and those are appended to Classes — which is
            // the list being walked. The emit-patch flow never goes through
            // CppEmitter.ComputeEmitted, so nothing has decoded the app module's
            // specializations before this point; here is where it would first happen.
            foreach (var c in compilation.Classes.ToList())
            {
                if (c.Module != compilation.AppModule)
                    continue;
                // A patch may implement a base-image interface, but declaring one
                // in the patch is not supported — its slots are frozen in no base
                // ABI, so nothing anchors its dispatch. Reject here (a fields-only
                // or all-abstract interface would otherwise skip the collect
                // below and dodge FenceType).
                if (c.IsInterface)
                    throw new NotSupportedException($"emit-patch: patch type {c.FullName} must be a plain class (declaring an interface in a patch is not supported — implement a base-image interface instead)");
                var baked = new List<MethodInfo>();
                MethodInfo? cctor = null;
                foreach (var m in c.Methods)
                {
                    if (m.Rva == 0)
                        continue;
                    if (m.IsStatic && m.Name == ".cctor")
                        cctor = m;
                    // A virtual override bakes even without a patch-side call
                    // edge: its callers can be AOT callvirt sites the
                    // reachability walk never sees (dispatch reaches it through
                    // the patched vtable slot).
                    else if (compilation.Reachable.Contains(m) || m.IsVirtual)
                        baked.Add(m);
                }
                // A type is baked when anything of it is: reachable methods, a
                // static constructor (a fields-only type still initializes
                // lazily), or fields another patch type touches.
                bool hasFields = false;
                foreach (var f in c.Fields)
                    if (!f.IsLiteral)
                        hasFields = true;
                if (baked.Count == 0 && cctor is null && !hasFields)
                    continue;
                if (cctor is not null)
                    baked.Insert(0, cctor);

                var itfImpls = ResolveTypeInterfaces(compilation, c, baseInterfaces);
                itfImplsByType[c] = itfImpls;
                var itfImplMethods = new HashSet<MethodInfo>();
                foreach (var (_, slots) in itfImpls)
                    foreach (var (_, impl) in slots)
                        itfImplMethods.Add(impl);
                FenceType(c, itfImplMethods);
                collected.Add((c, baked, cctor is not null));
            }

            // TypeTable order is base-before-derived: a patch type's patch
            // base (if any) bakes at a smaller index, so the loader constructs
            // types in one pass (each base type-info — and the instance size
            // the derived field append builds on — exists before its derived
            // types). Ordering by patch-base-chain depth is enough (a base is
            // always strictly shallower); the sort is stable, so unrelated
            // types keep their declaration order.
            var patchClasses = new List<(ClassInfo Class, List<MethodInfo> Baked, uint MethodStart, bool HasCctor)>();
            var typeIndex = new Dictionary<ClassInfo, uint>();
            var methodIndex = new Dictionary<MethodInfo, uint>();
            var fieldIndex = new Dictionary<FieldDefinitionHandle, PatchField>();
            uint nextMethodIdx = 0;
            foreach (var (c, baked, hasCctor) in collected.OrderBy(e => PatchBaseDepth(e.Class)))
            {
                typeIndex[c] = (uint)patchClasses.Count;
                patchClasses.Add((c, baked, nextMethodIdx, hasCctor));
                foreach (var m in baked)
                {
                    FenceMethod(compilation, m);
                    methodIndex[m] = nextMethodIdx;
                    nextMethodIdx++;
                }
            }

            // Pass 1b: bake the FieldTable — after every patch type has its
            // index, so a patch-class-typed field can reference forward.
            // Static fields get image-consecutive staticSlot indices; instance
            // fields carry only their per-type declaration ordinal in `offset`
            // (the loader assigns real byte offsets against the live base
            // layout — the converter never computes numeric layout).
            var fieldRuns = new (uint Start, uint Count)[patchClasses.Count];
            uint nextStaticSlot = 0;
            for (int t = 0; t < patchClasses.Count; t++)
            {
                var c = patchClasses[t].Class;
                uint fieldStart = w.FieldCount;
                uint instanceOrdinal = 0;
                foreach (var f in c.Fields)
                {
                    if (f.IsLiteral)
                        continue;
                    uint typeRef = SlotRefOf(w, typeImports, typeIndex, f.Type, c.FullName,
                        $"{(f.IsStatic ? "static " : "")}field {f.Name}");
                    uint fieldIdx = f.IsStatic
                        ? w.AddField(
                            nameOff: w.InternName(f.Name),
                            typeRef: typeRef,
                            offset: 0,
                            flags: 0x1, // static (mirrors DN2CPP_FLDA_STATIC)
                            staticSlot: nextStaticSlot++)
                        : w.AddField(
                            nameOff: w.InternName(f.Name),
                            typeRef: typeRef,
                            offset: instanceOrdinal++,
                            flags: 0,
                            staticSlot: 0xFFFFFFFFu);
                    fieldIndex[f.Handle] = new PatchField
                    {
                        FieldIdx = fieldIdx,
                        TypeIdx = (uint)t,
                        IsStatic = f.IsStatic,
                        Kind = KindOfType(compilation, f.Type)!.Value,
                    };
                }
                fieldRuns[t] = (fieldStart, w.FieldCount - fieldStart);
            }

            // Pass 2: bake bodies. Record append order matches the pass-1 index
            // assignment, so AddMethod returns the pre-assigned indices.
            for (int t = 0; t < patchClasses.Count; t++)
            {
                var (c, baked, methodStart, hasCctor) = patchClasses[t];
                // Virtual overrides: each resolves to its frozen base-image
                // vtable slot (manifest V lines) — stamped into the
                // MethodRecord and collected into the type's VtableDesc run,
                // which the loader applies to the copied base vtable.
                var overrides = new List<(uint Slot, uint MethodIdx)>();
                foreach (var m in baked)
                {
                    var body = m.DeclaringClass.Module.PE.GetMethodBody(m.Rva);
                    var frame = BuildFrame(compilation, w, typeImports, typeIndex, m, body);
                    uint localSigOff = w.LocalSigCount;
                    foreach (var slotRef in frame.SlotRefs)
                        w.AddLocalSig(slotRef);
                    uint codeOff = (uint)w.CodeByteLength;
                    var (insnCount, maxStack, ehStart, ehCount) = BakeBody(compilation, w, typeImports, methodImports, fieldImports, typeIndex, methodIndex, fieldIndex, m, body, frame, regCode);
                    uint vtableSlot = 0xFFFFu;
                    // A class-virtual override occupies a frozen base vtable slot;
                    // a new virtual slot that is a base-image interface
                    // implementation occupies no class vtable slot (0xFFFF) — it is
                    // reached through the interface-dispatch map (the ItfDesc run
                    // below), not the vtable.
                    if (m.IsVirtual && !m.IsNewSlot)
                    {
                        vtableSlot = ResolveOverrideSlot(baseVtables, c, m);
                        overrides.Add((vtableSlot, methodIndex[m]));
                    }
                    uint idx = w.AddMethod(
                        nameOff: w.InternName(m.Name),
                        declTypeIdx: (uint)t,
                        sigShapeOff: w.InternName(SigShape(m.Signature)),
                        maxStack: maxStack,
                        localCount: (uint)frame.LocalKinds.Length,
                        argCount: (uint)frame.ArgKinds.Length,
                        localSigOff: localSigOff,
                        codeOff: codeOff,
                        codeInsnCount: insnCount,
                        ehStart: ehStart,
                        ehCount: ehCount,
                        vtableSlot: vtableSlot);
                    if (compilation.EntryPoint == m)
                        entryMethodIdx = idx;
                }

                // ItfDesc run: for each base-image interface this type
                // implements, its import + full slot count + the (slot,
                // implementing patch method) pairs. The loader builds an extended
                // interface-dispatch map from it (base entries + these overrides,
                // each overridden slot filled with an N2M interface trampoline).
                uint itfDescOff = 0xFFFFFFFFu;
                var typeItfImpls = itfImplsByType[c];
                if (typeItfImpls.Count > 0)
                {
                    var itfEntries = new List<(uint ItfImport, uint FullSlotCount, List<(uint Slot, uint MethodIdx)> Impls)>();
                    foreach (var (itfName, slots) in typeItfImpls)
                    {
                        uint itfImport = ImportRef(TypeImportOf(w, typeImports, itfName));
                        uint fullSlotCount = (uint)baseInterfaces[itfName].Count;
                        var impls = new List<(uint Slot, uint MethodIdx)>();
                        foreach (var (slot, impl) in slots)
                            impls.Add((slot, methodIndex[impl]));
                        itfEntries.Add((itfImport, fullSlotCount, impls));
                    }
                    itfDescOff = w.AddItfDesc(itfEntries);
                }

                uint vtableDescOff = 0xFFFFFFFFu;
                if (overrides.Count > 0)
                {
                    // The desc leads with the base vtable's total slot count —
                    // the copy length the loader needs (Dn2CppTypeInfo carries
                    // no vtable length; the manifest's slot list is the same V
                    // lines the ABI hash freezes against the live base). The
                    // AOT ancestor owns every occupiable slot, so its count is
                    // the length for patch-derived types too.
                    var baseSlots = baseVtables[AotAncestorName(c)!];
                    vtableDescOff = w.AddVtableDesc((uint)baseSlots.Count, overrides);
                }

                w.AddType(
                    nameOff: w.InternName(c.FullName),
                    baseRef: BakeBaseRef(w, typeImports, typeIndex, c),
                    flags: hasCctor ? TypeFlagHasCctor : 0,
                    itfDescOff: itfDescOff, // repurposed instanceSize slot; loader owns layout
                    fieldStart: fieldRuns[t].Start,
                    fieldCount: fieldRuns[t].Count,
                    methodStart: methodStart,
                    methodCount: (uint)baked.Count,
                    vtableDescOff: vtableDescOff);
            }

            if (entryMethodIdx == 0xFFFFFFFFu)
                throw new NotSupportedException("emit-patch: the patch assembly has no reachable entry point (build it as an Exe with a static void Main())");

            byte[] blob = w.Finish(baseHash, entryMethodIdx, patchVersion, regCode ? BpiFlagRegCode : 0u);

            Directory.CreateDirectory(outDir);
            string fileName = Path.GetFileName(patchDll);
            int dot = fileName.LastIndexOf('.');
            string baseName = dot > 0 ? fileName.Substring(0, dot) : fileName;
            string outPath = Path.Combine(outDir, baseName + ".bpi");
            File.WriteAllBytes(outPath, blob);
            Console.WriteLine($"dn2cpp --emit-patch: {w.TypeCount} types, {w.MethodCount} methods, {w.ImportCount} imports, {blob.Length} bytes, patchVersion {patchVersion} -> {outPath}");
            return 0;
        }
        catch (NotSupportedException ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
    }

    /// <summary>The length of a class's same-module (patch) base chain: 0 for
    /// a type rooted at System.Object or an AOT base-image class, 1 for a type
    /// deriving from such a type, and so on. The TypeTable bakes in ascending
    /// depth order, so a patch base always precedes its derived types.</summary>
    private static int PatchBaseDepth(ClassInfo c)
    {
        int depth = 0;
        for (var b = c.BaseClass; b is not null && b.Module == c.Module; b = b.BaseClass)
            depth++;
        return depth;
    }

    /// <summary>Type fence: a patch type is a plain class deriving from
    /// System.Object, from an AOT base-image class, or from another patch
    /// class of the same image, with no interface implementations. Virtual
    /// methods are allowed only as overrides of existing base-image vtable
    /// slots (resolved against the manifest where they bake — a patch-derived
    /// type re-overriding its patch base's override lands on the same frozen
    /// slot): a new virtual slot declaration (newslot) is rejected — the
    /// patched vtable keeps the frozen base slot layout — and so are the
    /// object overrides (ToString/GetHashCode/Equals/Finalize), which the
    /// runtime dispatches through dedicated type-info entries rather than
    /// vtable slots. Static and instance fields are allowed (their types are
    /// fenced where they bake); [ThreadStatic] is not.</summary>
    private static void FenceType(ClassInfo c, HashSet<MethodInfo> itfImplMethods)
    {
        if (c.GenericArity > 0)
            throw new NotSupportedException($"emit-patch: generic patch type {c.FullName} is not supported yet");
        if (c.IsValueType || c.IsEnum || c.IsInterface || c.IsDelegate)
            throw new NotSupportedException($"emit-patch: patch type {c.FullName} must be a plain class");
        // Modeled interfaces (a closed generic interface instantiation, or a
        // patch-declared interface) stay outside the fence — only non-generic
        // base-image interfaces (surfaced as ExternalInterfaceNames, validated in
        // ResolveTypeInterfaces against the manifest) are supported.
        if (c.Interfaces.Count > 0)
            throw new NotSupportedException($"emit-patch: patch type {c.FullName} must not implement generic or patch-declared interfaces yet");
        foreach (var m in c.Methods)
        {
            if (!m.IsVirtual)
                continue;
            // A new virtual slot is allowed only when it implements a base-image
            // interface method (an interface-implementation method has no class
            // vtable slot; it fills an interface-dispatch slot instead).
            if (m.IsNewSlot && !itfImplMethods.Contains(m))
                throw new NotSupportedException($"emit-patch: patch type {c.FullName} must not declare new virtual slots yet ({m.Name} — only overrides of base-image virtual methods and base-image interface implementations are supported)");
            if (m.SigKey is "ToString():String" or "GetHashCode():Int32"
                or "Equals(Object):Boolean" or "Finalize():Void")
            {
                throw new NotSupportedException($"emit-patch: patch type {c.FullName} — overriding {m.Name} is not supported yet (it dispatches through a dedicated type-info entry, not a vtable slot)");
            }
        }
        foreach (var f in c.Fields)
        {
            if (f.IsLiteral)
                continue;
            if (f.IsThreadStatic)
                throw new NotSupportedException($"emit-patch: patch type {c.FullName} — [ThreadStatic] field {f.Name} is not supported yet");
        }
    }

    /// <summary>The CLR FullName of a patch type's cross-assembly (AOT) base
    /// class, or null for a System.Object root. Callers resolve same-module
    /// (patch) bases before asking; a generic (TypeSpecification) base is
    /// rejected here.</summary>
    private static string? BaseTypeName(ClassInfo c)
    {
        var reader = c.Module.Reader;
        var td = reader.GetTypeDefinition(c.Handle);
        if (td.BaseType.IsNil)
            return null;
        if (td.BaseType.Kind == HandleKind.TypeReference)
        {
            var tr = reader.GetTypeReference((TypeReferenceHandle)td.BaseType);
            string ns = reader.GetString(tr.Namespace);
            string name = reader.GetString(tr.Name);
            string full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            return full == "System.Object" ? null : full;
        }
        throw new NotSupportedException($"emit-patch: patch type {c.FullName} has an unsupported base-type shape (generic bases are not supported yet)");
    }

    /// <summary>The nearest AOT (base-image) ancestor's CLR FullName: walks
    /// past any same-module patch bases, then reads the cross-assembly base
    /// reference. Null when the whole chain roots at System.Object. Every
    /// vtable slot a patch override can occupy is frozen on this ancestor
    /// (patch types declare no new slots), so it is the manifest key.</summary>
    private static string? AotAncestorName(ClassInfo c)
    {
        var cur = c;
        while (cur.BaseClass is { } b && b.Module == cur.Module)
            cur = b;
        return BaseTypeName(cur);
    }

    /// <summary>Resolves the base-image interfaces a patch type implements to
    /// their (slot, implementing patch method) pairs. Each interface must be a
    /// non-generic base-image interface present in the manifest (surfaced as an
    /// <see cref="ClassInfo.ExternalInterfaceNames"/> entry). For each of the
    /// interface's slots, the implementing method is an explicit implementation
    /// (a `.override` MethodImpl targeting the interface method) or an implicit
    /// one (a same-signature method on the type or its patch base chain); a slot
    /// left to an AOT-base inheritance carries no override (the loader fills it
    /// from the base's interface map). A type rooted at object that leaves a slot
    /// unimplemented is rejected (a C# compile would already have failed).</summary>
    private static List<(string ItfName, List<(uint Slot, MethodInfo Impl)> Slots)> ResolveTypeInterfaces(
        Compilation compilation, ClassInfo c, Dictionary<string, List<string>> baseInterfaces)
    {
        var result = new List<(string, List<(uint, MethodInfo)>)>();
        if (c.ExternalInterfaceNames.Count == 0)
            return result;

        var reader = c.Module.Reader;
        var td = reader.GetTypeDefinition(c.Handle);
        // Explicit interface implementations (.override rows), keyed by the
        // interface method's fully-qualified SigKey (declaring type + SigKey).
        var explicitImpls = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
        foreach (var mih in td.GetMethodImplementations())
        {
            var mi = reader.GetMethodImplementation(mih);
            if (mi.MethodDeclaration.Kind != HandleKind.MemberReference
                || mi.MethodBody.Kind != HandleKind.MethodDefinition)
                continue;
            var mr = reader.GetMemberReference((MemberReferenceHandle)mi.MethodDeclaration);
            if (mr.Parent.Kind != HandleKind.TypeReference)
                continue;
            var tr = reader.GetTypeReference((TypeReferenceHandle)mr.Parent);
            string ns = reader.GetString(tr.Namespace);
            string declType = string.IsNullOrEmpty(ns)
                ? reader.GetString(tr.Name)
                : ns + "." + reader.GetString(tr.Name);
            var sig = mr.DecodeMethodSignature(compilation.SigProvider, null);
            string sigKey = reader.GetString(mr.Name) + SigShape(sig);
            if (compilation.AppModule.MethodMap.TryGetValue((MethodDefinitionHandle)mi.MethodBody, out var body)
                && body.DeclaringClass.Module == c.Module)
                explicitImpls[declType + "::" + sigKey] = body;
        }

        bool rootsAtObject = AotAncestorName(c) is null;
        foreach (var itfName in c.ExternalInterfaceNames)
        {
            if (!baseInterfaces.TryGetValue(itfName, out var slots))
                throw new NotSupportedException($"emit-patch: {c.FullName} implements interface {itfName}, which is not a non-generic base-image interface in the base-image ABI manifest (is the base built with --hotupdate-base?)");
            var slotImpls = new List<(uint, MethodInfo)>();
            for (int k = 0; k < slots.Count; k++)
            {
                string sigKey = slots[k];
                // The manifest hands us a key, not a MethodInfo, so recover the name to guard
                // the scan with (see MethodInfo.SigKey): a key is Name + SigShape and a shape
                // always opens with '(' (AbiContract.SigShape), so the first '(' splits it.
                // A name carrying a '(' of its own would leave sigName a proper prefix of the
                // real name and the candidate would be rejected — which is the same
                // equivalent-or-stricter trade the MethodInfo-to-MethodInfo guards make.
                int lparen = sigKey.IndexOf('(');
                string sigName = lparen >= 0 ? sigKey[..lparen] : sigKey;
                MethodInfo? impl = null;
                if (explicitImpls.TryGetValue(itfName + "::" + sigKey, out var ex))
                {
                    impl = ex;
                }
                else
                {
                    for (var b = c; b is not null && b.Module == c.Module; b = b.BaseClass)
                    {
                        impl = b.Methods.FirstOrDefault(m => !m.IsStatic && m.Rva != 0
                            && m.Name == sigName && m.SigKey == sigKey);
                        if (impl is not null)
                            break;
                    }
                }
                if (impl is null)
                {
                    // No patch-side implementation: only sound when an AOT base
                    // already implements the interface (the loader inherits its
                    // slot). An object-rooted type must implement every slot.
                    if (rootsAtObject)
                        throw new NotSupportedException($"emit-patch: {c.FullName} does not implement interface method {itfName}.{sigKey}");
                    continue;
                }
                slotImpls.Add(((uint)k, impl));
            }
            result.Add((itfName, slotImpls));
        }
        return result;
    }

    /// <summary>Bakes a patch type's TypeRecord.baseRef: 0xFFFFFFFF for a
    /// System.Object root, a PatchEntity-tagged TypeTable index for a patch
    /// base (always smaller than the derived type's own index — see the
    /// depth-ordered bake), or an Import-tagged type reference to the AOT base
    /// class (resolved against the live registry at load).</summary>
    private static uint BakeBaseRef(BpiWriter w, Dictionary<string, uint> typeImports,
        Dictionary<ClassInfo, uint> typeIndex, ClassInfo c)
    {
        if (c.BaseClass is { } pb && pb.Module == c.Module)
        {
            if (!typeIndex.TryGetValue(pb, out uint baseIdx))
                throw new NotSupportedException($"emit-patch: patch type {c.FullName} derives from {pb.FullName}, which bakes nothing into the image");
            return baseIdx; // PatchEntity tag (0) + TypeTable index
        }
        string? full = BaseTypeName(c);
        if (full is null)
            return 0xFFFFFFFFu;
        return ImportRef(TypeImportOf(w, typeImports, full));
    }

    /// <summary>Resolves a virtual override to its base-image vtable slot: the
    /// index of the method's SigKey among the AOT ancestor type's slot
    /// signatures in the base-abi.json manifest (the same V lines the ABI hash
    /// freezes, so a baked slot number can never silently drift — a base
    /// rebuild that moves slots changes the hash and the loader rejects the
    /// stale BPI). A patch-derived override of a patch base's override lands
    /// on the same frozen slot by construction.</summary>
    private static uint ResolveOverrideSlot(Dictionary<string, List<string>> baseVtables,
        ClassInfo c, MethodInfo m)
    {
        string? baseName = AotAncestorName(c);
        if (baseName is null || !baseVtables.TryGetValue(baseName, out var slots))
            throw new NotSupportedException($"emit-patch: {c.FullName}.{m.Name} overrides a virtual method, but base type {baseName ?? "System.Object"} has no vtable in the base-image ABI manifest (is the base built with --hotupdate-base?)");
        int slot = -1;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == m.SigKey)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0)
            throw new NotSupportedException($"emit-patch: {c.FullName}.{m.Name} does not match any virtual slot of base type {baseName} ({m.SigKey})");
        if (slot >= 0xFFFF)
            throw new NotSupportedException($"emit-patch: {c.FullName}.{m.Name} — vtable slot {slot} exceeds the MethodRecord slot range");
        return (uint)slot;
    }

    /// <summary>Method fence: a static method, an instance constructor, or a
    /// plain non-virtual instance method (virtuals were fenced at the type)
    /// over scalar/String/reference-type parameters and returns (a `.cctor`
    /// is parameterless void by definition); the entry point stays static,
    /// parameterless, and void.</summary>
    private static void FenceMethod(Compilation compilation, MethodInfo m)
    {
        string full = $"{m.DeclaringClass.FullName}.{m.Name}";
        if (compilation.EntryPoint == m)
        {
            if (!m.IsStatic || m.Signature.ParameterTypes.Length != 0 || !m.Signature.ReturnType.IsVoid)
                throw new NotSupportedException($"emit-patch: {full} — the patch entry point must be static, parameterless, and void");
            return;
        }
        foreach (var p in m.Signature.ParameterTypes)
            if (KindOfType(compilation, p) is null)
                throw new NotSupportedException($"emit-patch: {full} — parameter type {p} is not supported yet (scalars, string, and reference types only)");
        if (!m.Signature.ReturnType.IsVoid && KindOfType(compilation, m.Signature.ReturnType) is null)
            throw new NotSupportedException($"emit-patch: {full} — return type {m.Signature.ReturnType} is not supported yet (scalars, string, reference types, or void only)");
    }

    /// <summary>The eval-stack kind of a supported arg/local/return/field slot
    /// type, or null when the type is outside the fence: the six scalars,
    /// String, a cross-assembly (AOT base image) reference type, a
    /// patch-defined class, or an SZArray over one of those. Object,
    /// jagged/multi-dimensional arrays, byrefs, pointers, and value types
    /// stay out.</summary>
    private static SlotKind? KindOfType(Compilation compilation, TypeDesc t)
    {
        if (t.IsString)
            return SlotKind.Ref;
        if (t.Kind == TypeKind.Primitive)
            return t.Primitive switch
            {
                PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char or PrimitiveTypeCode.Int32 => SlotKind.I32,
                PrimitiveTypeCode.Int64 => SlotKind.I64,
                PrimitiveTypeCode.Single => SlotKind.F32,
                PrimitiveTypeCode.Double => SlotKind.F64,
                _ => null,
            };
        if (t.Kind == TypeKind.External && !t.IsObject)
            return SlotKind.Ref;
        // A closed generic instantiation of a base-image reference type is bound
        // by its mangled registry name, exactly like a non-generic AOT type.
        if (t.Kind == TypeKind.ExternalGeneric)
            return SlotKind.Ref;
        if (t.Kind == TypeKind.Class && t.Class!.Module == compilation.AppModule && !t.Class.IsValueType)
            return SlotKind.Ref;
        if (t.Kind == TypeKind.SZArray && t.Element is { } el
            && el.Kind is not (TypeKind.SZArray or TypeKind.MDArray)
            && KindOfType(compilation, el) is not null)
        {
            return SlotKind.Ref;
        }
        return null;
    }

    /// <summary>Encodes a supported slot type as a LocalSigTable/signature
    /// EntityRef: a Primitive-tagged code for scalars/String, an Import-tagged
    /// type-import index for an AOT reference type (or a synthetic SZArray
    /// type import for an array over the fence), or a PatchEntity-tagged
    /// TypeTable index for a baked patch class. Throws outside the fence.</summary>
    private static uint SlotRefOf(BpiWriter w, Dictionary<string, uint> typeImports,
        Dictionary<ClassInfo, uint> typeIndex, TypeDesc t, string site, string what)
    {
        if (t.IsString)
            return PrimRef(PrimitiveTypeCode.String);
        if (t.Kind == TypeKind.Primitive && t.Primitive is PrimitiveTypeCode.Boolean
            or PrimitiveTypeCode.Char or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64
            or PrimitiveTypeCode.Single or PrimitiveTypeCode.Double)
        {
            return PrimRef(t.Primitive);
        }
        if (t.Kind == TypeKind.External && !t.IsObject)
            return ImportRef(TypeImportOf(w, typeImports, t.ExternalName!));
        // A base-image generic instantiation: its mangled registry name is a
        // plain type import the loader binds against the type-name registry.
        if (t.Kind == TypeKind.ExternalGeneric)
            return ImportRef(TypeImportOf(w, typeImports, t.ExternalName!));
        if (t.Kind == TypeKind.Class && !t.Class!.IsValueType
            && typeIndex.TryGetValue(t.Class, out uint patchTypeIdx))
        {
            return patchTypeIdx; // PatchEntity tag (0) + TypeTable index
        }
        if (t.Kind == TypeKind.SZArray)
            return ImportRef(ArrayImportOf(w, typeImports, typeIndex, t.Element!, site, what));
        throw new NotSupportedException($"emit-patch: {site} — {what} type {t} is not supported yet (scalars, string, and reference types only)");
    }

    /// <summary>Interns (deduped by CLR array name) the synthetic SZArray type
    /// import for an array of <paramref name="elem"/>: an ImportTable Type
    /// record whose aux0 bit0 marks it as an SZArray and whose aux1 carries the
    /// element type's EntityRef (docs/BPI-FORMAT.md "ImportTable"). The loader
    /// binds it by name to the base image's per-element array type-info when
    /// one exists, and otherwise constructs an array type-info from the element
    /// reference — after patch-type construction, so a patch-class element
    /// resolves too. The element must itself be inside the slot fence; jagged
    /// and multi-dimensional arrays are rejected.</summary>
    private static uint ArrayImportOf(BpiWriter w, Dictionary<string, uint> typeImports,
        Dictionary<ClassInfo, uint> typeIndex, TypeDesc elem, string site, string what)
    {
        if (elem.Kind is TypeKind.SZArray or TypeKind.MDArray)
            throw new NotSupportedException($"emit-patch: {site} — {what}: jagged or multi-dimensional array type {elem}[] is not supported yet");
        uint elemRef = SlotRefOf(w, typeImports, typeIndex, elem, site, what);
        string name = ElemClrName(elem, site, what) + "[]";
        if (!typeImports.TryGetValue(name, out uint arrImport))
        {
            arrImport = w.AddImport(1, w.InternName(name), 0xFFFFFFFFu, w.InternName(""), 1, elemRef);
            typeImports[name] = arrImport;
        }
        return arrImport;
    }

    /// <summary>The CLR FullName an SZArray element contributes to the array's
    /// registry name (what typeof(T[]).FullName prefixes before "[]").</summary>
    private static string ElemClrName(TypeDesc t, string site, string what)
    {
        if (t.IsString)
            return "System.String";
        if (t.Kind == TypeKind.Primitive)
        {
            return t.Primitive switch
            {
                PrimitiveTypeCode.Boolean => "System.Boolean",
                PrimitiveTypeCode.Char => "System.Char",
                PrimitiveTypeCode.Int32 => "System.Int32",
                PrimitiveTypeCode.Int64 => "System.Int64",
                PrimitiveTypeCode.Single => "System.Single",
                PrimitiveTypeCode.Double => "System.Double",
                _ => throw new NotSupportedException($"emit-patch: {site} — {what} type {t}[] is not supported yet"),
            };
        }
        if (t.Kind == TypeKind.External)
            return t.ExternalName!;
        if (t.Kind == TypeKind.Class)
            return t.Class!.FullName;
        throw new NotSupportedException($"emit-patch: {site} — {what} type {t}[] is not supported yet");
    }

    /// <summary>The element storage kind baked into newarr/ldelem/stelem for
    /// an SZArray of <paramref name="elem"/> (docs/BPI-FORMAT.md "element
    /// storage kinds"): Boolean/Char store at their packed 1/2-byte widths
    /// (the AOT array layouts), the other scalars at their natural widths,
    /// and every reference type as a pointer element.</summary>
    private static uint ElemKindOf(Compilation compilation, TypeDesc elem, string site, int offset)
    {
        if (elem.Kind == TypeKind.Primitive)
        {
            switch (elem.Primitive)
            {
                case PrimitiveTypeCode.Boolean: return (uint)ElemKind.U1;
                case PrimitiveTypeCode.Char: return (uint)ElemKind.U2;
                case PrimitiveTypeCode.Int32: return (uint)ElemKind.I4;
                case PrimitiveTypeCode.Int64: return (uint)ElemKind.I8;
                case PrimitiveTypeCode.Single: return (uint)ElemKind.R4;
                case PrimitiveTypeCode.Double: return (uint)ElemKind.R8;
            }
        }
        if (KindOfType(compilation, elem) == SlotKind.Ref && elem.Kind != TypeKind.SZArray)
            return (uint)ElemKind.Ref;
        throw new NotSupportedException($"emit-patch: {site} — array element type {elem} is not supported yet at IL_{offset:X4}");
    }

    /// <summary>The element storage kind a typed ldelem.*/stelem.* short form
    /// carries in the opcode itself (the canonical baked form erases the
    /// distinction — every form normalizes to ldelem/stelem + kind).</summary>
    private static uint TypedElemKind(ILOpCode op, string site, int offset) => op switch
    {
        ILOpCode.Ldelem_i1 or ILOpCode.Stelem_i1 => (uint)ElemKind.I1,
        ILOpCode.Ldelem_u1 => (uint)ElemKind.U1,
        ILOpCode.Ldelem_i2 or ILOpCode.Stelem_i2 => (uint)ElemKind.I2,
        ILOpCode.Ldelem_u2 => (uint)ElemKind.U2,
        ILOpCode.Ldelem_i4 or ILOpCode.Ldelem_u4 or ILOpCode.Stelem_i4 => (uint)ElemKind.I4,
        ILOpCode.Ldelem_i8 or ILOpCode.Stelem_i8 => (uint)ElemKind.I8,
        ILOpCode.Ldelem_r4 or ILOpCode.Stelem_r4 => (uint)ElemKind.R4,
        ILOpCode.Ldelem_r8 or ILOpCode.Stelem_r8 => (uint)ElemKind.R8,
        ILOpCode.Ldelem_ref or ILOpCode.Stelem_ref => (uint)ElemKind.Ref,
        _ => throw new NotSupportedException($"emit-patch: {site} — opcode {op} is not supported yet at IL_{offset:X4}"),
    };

    /// <summary>The eval-stack kind an element of the given storage kind loads
    /// to / stores from (sub-int32 elements ride i32 slots).</summary>
    private static SlotKind ElemSlotKind(uint kind) => (ElemKind)kind switch
    {
        ElemKind.I8 => SlotKind.I64,
        ElemKind.R4 => SlotKind.F32,
        ElemKind.R8 => SlotKind.F64,
        ElemKind.Ref => SlotKind.Ref,
        _ => SlotKind.I32,
    };

    /// <summary>Resolves a type-token instruction operand (the newarr/ldelem/
    /// stelem/isinst element and target positions) to a TypeDesc: a
    /// TypeDefinition is a patch-module type, a TypeReference decodes through
    /// the signature provider (with the primitive System.* names normalized to
    /// their primitive codes — IL tokens reference primitives by name, unlike
    /// signature blobs), and a TypeSpecification decodes the full signature
    /// shape (array types).</summary>
    private static TypeDesc DecodeTypeToken(Compilation compilation, MethodInfo m, int token, string site)
    {
        var module = m.DeclaringClass.Module;
        var handle = SRME.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return compilation.GetTypeDescForDefinition(module, (TypeDefinitionHandle)handle);
            case HandleKind.TypeReference:
            {
                var t = compilation.SigProvider.GetTypeFromReference(module.Reader, (TypeReferenceHandle)handle, 0);
                if (t.Kind == TypeKind.External)
                {
                    switch (t.ExternalName)
                    {
                        case "System.Boolean": return TypeDesc.MakePrimitive(PrimitiveTypeCode.Boolean);
                        case "System.Char": return TypeDesc.MakePrimitive(PrimitiveTypeCode.Char);
                        case "System.Int32": return TypeDesc.MakePrimitive(PrimitiveTypeCode.Int32);
                        case "System.Int64": return TypeDesc.MakePrimitive(PrimitiveTypeCode.Int64);
                        case "System.Single": return TypeDesc.MakePrimitive(PrimitiveTypeCode.Single);
                        case "System.Double": return TypeDesc.MakePrimitive(PrimitiveTypeCode.Double);
                        case "System.String": return TypeDesc.MakePrimitive(PrimitiveTypeCode.String);
                    }
                }
                return t;
            }
            case HandleKind.TypeSpecification:
                return module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle)
                    .DecodeSignature(compilation.SigProvider, m.Context);
            default:
                throw new NotSupportedException($"emit-patch: {site} — unsupported type token kind {handle.Kind}");
        }
    }

    /// <summary>Interns (deduped) a Type import for an AOT type named by its
    /// CLR FullName; returns the import index.</summary>
    private static uint TypeImportOf(BpiWriter w, Dictionary<string, uint> typeImports, string typeName)
    {
        if (!typeImports.TryGetValue(typeName, out uint typeImport))
        {
            typeImport = w.AddImport(1, w.InternName(typeName), 0xFFFFFFFFu, w.InternName(""), 0, 0);
            typeImports[typeName] = typeImport;
        }
        return typeImport;
    }

    /// <summary>Fences the local signature and shapes the method frame: arg and
    /// local kinds, the LocalSigTable run (args then locals), the return kind.
    /// An instance method's frame leads with its `this` slot (a PatchEntity
    /// reference to the declaring type), matching IL argument numbering.</summary>
    private static FrameShape BuildFrame(Compilation compilation, BpiWriter w,
        Dictionary<string, uint> typeImports, Dictionary<ClassInfo, uint> typeIndex,
        MethodInfo m, MethodBodyBlock body)
    {
        string full = $"{m.DeclaringClass.FullName}.{m.Name}";
        TypeDesc[] locals = Array.Empty<TypeDesc>();
        if (!body.LocalSignature.IsNil)
        {
            var localSig = m.DeclaringClass.Module.Reader.GetStandaloneSignature(body.LocalSignature);
            locals = localSig.DecodeLocalSignature(compilation.SigProvider, m.Context).ToArray();
        }

        var ps = m.Signature.ParameterTypes;
        int self = m.IsStatic ? 0 : 1;
        var argKinds = new SlotKind[self + ps.Length];
        var localKinds = new SlotKind[locals.Length];
        var slotRefs = new uint[self + ps.Length + locals.Length];
        if (self == 1)
        {
            argKinds[0] = SlotKind.Ref;
            slotRefs[0] = typeIndex[m.DeclaringClass]; // PatchEntity self reference
        }
        for (int i = 0; i < ps.Length; i++)
        {
            slotRefs[self + i] = SlotRefOf(w, typeImports, typeIndex, ps[i], full, "parameter");
            argKinds[self + i] = KindOfType(compilation, ps[i])!.Value;
        }
        for (int i = 0; i < locals.Length; i++)
        {
            slotRefs[self + ps.Length + i] = SlotRefOf(w, typeImports, typeIndex, locals[i], full, "local");
            localKinds[i] = KindOfType(compilation, locals[i])!.Value;
        }

        SlotKind? returnKind = null;
        if (!m.Signature.ReturnType.IsVoid)
        {
            returnKind = KindOfType(compilation, m.Signature.ReturnType)
                ?? throw new NotSupportedException($"emit-patch: {full} — return type {m.Signature.ReturnType} is not supported yet (scalars, string, reference types, or void only)");
        }

        return new FrameShape
        {
            ArgKinds = argKinds,
            LocalKinds = localKinds,
            SlotRefs = slotRefs,
            ReturnKind = returnKind,
        };
    }

    private static ILOpCode CanonicalBranch(ILOpCode op) => op switch
    {
        ILOpCode.Br_s => ILOpCode.Br,
        ILOpCode.Brtrue_s => ILOpCode.Brtrue,
        ILOpCode.Brfalse_s => ILOpCode.Brfalse,
        ILOpCode.Beq_s => ILOpCode.Beq,
        ILOpCode.Bge_s => ILOpCode.Bge,
        ILOpCode.Bgt_s => ILOpCode.Bgt,
        ILOpCode.Ble_s => ILOpCode.Ble,
        ILOpCode.Blt_s => ILOpCode.Blt,
        ILOpCode.Bne_un_s => ILOpCode.Bne_un,
        ILOpCode.Bge_un_s => ILOpCode.Bge_un,
        ILOpCode.Bgt_un_s => ILOpCode.Bgt_un,
        ILOpCode.Ble_un_s => ILOpCode.Ble_un,
        ILOpCode.Blt_un_s => ILOpCode.Blt_un,
        _ => op,
    };

    /// <summary>One buffered register-form instruction (--patch-regcode). The
    /// arms append these instead of calling <see cref="BpiWriter.AddInsn"/>;
    /// when <see cref="TargetFixup"/> is set, <see cref="A"/> is an IL
    /// instruction index remapped to a register-stream index at flush (a
    /// branch/leave target — zero-record IL instructions map to the next
    /// emitted record, which is exactly where control lands).</summary>
    private struct RegInsn
    {
        public RegOp Op;
        public byte R0;
        public byte R1;
        public uint A;
        public uint B;
        public bool TargetFixup;
        /// <summary>A leader (branch/leave target or EH boundary) lies between
        /// the previously buffered record and this one: an incoming edge can
        /// land here, so the copy-propagation peephole must not fold this
        /// record with its predecessor.</summary>
        public bool FenceBefore;
    }

    /// <summary>Whether the record reads register <paramref name="t"/> through
    /// any of its source fields (r0/r1, or `a`'s low byte for the C3
    /// class).</summary>
    private static bool RegReadsSrc(in RegInsn rec, byte t) =>
        (RegOpFold.ReadsR0(rec.Op) && rec.R0 == t)
        || (RegOpFold.ReadsR1(rec.Op) && rec.R1 == t)
        || (RegOpFold.ReadsR2(rec.Op) && rec.A == t);

    /// <summary>Rewrites every source field of the record that reads
    /// <paramref name="from"/> to read <paramref name="to"/> instead;
    /// destination fields are never touched.</summary>
    private static void RegRewriteSrc(ref RegInsn rec, byte from, byte to)
    {
        if (RegOpFold.ReadsR0(rec.Op) && rec.R0 == from)
            rec.R0 = to;
        if (RegOpFold.ReadsR1(rec.Op) && rec.R1 == from)
            rec.R1 = to;
        if (RegOpFold.ReadsR2(rec.Op) && rec.A == from)
            rec.A = to;
    }

    /// <summary>The width-specialized register opcode of a binary arithmetic
    /// IL opcode over operands of kind <paramref name="k"/> (the same kind the
    /// v1 hint bakes). The int groups are laid out in I32/I64 pairs and the
    /// float groups in F32/F64 pairs, in the opcode table's order.</summary>
    private static RegOp RegBinOp(ILOpCode op, SlotKind k)
    {
        if (k is SlotKind.F32 or SlotKind.F64)
        {
            RegOp fbase = op switch
            {
                ILOpCode.Add => RegOp.R_ADD_F32,
                ILOpCode.Sub => RegOp.R_SUB_F32,
                ILOpCode.Mul => RegOp.R_MUL_F32,
                ILOpCode.Div => RegOp.R_DIV_F32,
                _ => RegOp.R_REM_F32, // Rem (the int-only ops are fenced off floats)
            };
            return (RegOp)((int)fbase + (k == SlotKind.F64 ? 1 : 0));
        }
        RegOp ibase = op switch
        {
            ILOpCode.Add => RegOp.R_ADD_I32,
            ILOpCode.Sub => RegOp.R_SUB_I32,
            ILOpCode.Mul => RegOp.R_MUL_I32,
            ILOpCode.Div => RegOp.R_DIV_I32,
            ILOpCode.Div_un => RegOp.R_DIV_UN_I32,
            ILOpCode.Rem => RegOp.R_REM_I32,
            ILOpCode.Rem_un => RegOp.R_REM_UN_I32,
            ILOpCode.And => RegOp.R_AND_I32,
            ILOpCode.Or => RegOp.R_OR_I32,
            _ => RegOp.R_XOR_I32,
        };
        return (RegOp)((int)ibase + (k == SlotKind.I64 ? 1 : 0));
    }

    /// <summary>The register opcode of a ceq/cgt/clt family comparison over
    /// operands of kind <paramref name="k"/>. Each group is laid out
    /// I32/I64/F32/F64 (the SlotKind order), with the two ref idioms
    /// (ceq / the cgt.un non-null test) as dedicated opcodes.</summary>
    private static RegOp RegCmpOp(ILOpCode op, SlotKind k)
    {
        if (k == SlotKind.Ref)
            return op == ILOpCode.Ceq ? RegOp.R_CEQ_REF : RegOp.R_CGT_UN_REF;
        RegOp baseOp = op switch
        {
            ILOpCode.Ceq => RegOp.R_CEQ_I32,
            ILOpCode.Cgt => RegOp.R_CGT_I32,
            ILOpCode.Cgt_un => RegOp.R_CGT_UN_I32,
            ILOpCode.Clt => RegOp.R_CLT_I32,
            _ => RegOp.R_CLT_UN_I32,
        };
        return (RegOp)((int)baseOp + (int)k);
    }

    /// <summary>The register opcode of a canonical compare-branch over
    /// operands of kind <paramref name="k"/>; the same I32/I64/F32/F64 layout
    /// as the comparisons, plus the two ref identity branches (the only
    /// reference comparisons the fence admits).</summary>
    private static RegOp RegBrCmpOp(ILOpCode canon, SlotKind k)
    {
        if (k == SlotKind.Ref)
            return canon == ILOpCode.Beq ? RegOp.R_BEQ_REF : RegOp.R_BNE_UN_REF;
        RegOp baseOp = canon switch
        {
            ILOpCode.Beq => RegOp.R_BEQ_I32,
            ILOpCode.Bne_un => RegOp.R_BNE_UN_I32,
            ILOpCode.Bge => RegOp.R_BGE_I32,
            ILOpCode.Bgt => RegOp.R_BGT_I32,
            ILOpCode.Ble => RegOp.R_BLE_I32,
            ILOpCode.Blt => RegOp.R_BLT_I32,
            ILOpCode.Bge_un => RegOp.R_BGE_UN_I32,
            ILOpCode.Bgt_un => RegOp.R_BGT_UN_I32,
            ILOpCode.Ble_un => RegOp.R_BLE_UN_I32,
            _ => RegOp.R_BLT_UN_I32,
        };
        return (RegOp)((int)baseOp + (int)k);
    }

    /// <summary>The register opcode of a conv.* over a source of kind
    /// <paramref name="src"/>: target-major, source order I32/I64/F — the v1
    /// source-kind hint promoted into the opcode. F covers both float widths
    /// (the slot holds a widened double either way).</summary>
    private static RegOp RegConvOp(ILOpCode op, SlotKind src)
    {
        RegOp baseOp = op switch
        {
            ILOpCode.Conv_i4 => RegOp.R_CONV_I4_FROM_I32,
            ILOpCode.Conv_u4 => RegOp.R_CONV_U4_FROM_I32,
            ILOpCode.Conv_i8 => RegOp.R_CONV_I8_FROM_I32,
            ILOpCode.Conv_u8 => RegOp.R_CONV_U8_FROM_I32,
            ILOpCode.Conv_r4 => RegOp.R_CONV_R4_FROM_I32,
            _ => RegOp.R_CONV_R8_FROM_I32,
        };
        int srcIdx = src switch
        {
            SlotKind.I32 => 0,
            SlotKind.I64 => 1,
            _ => 2, // F32/F64 both ride the widened-double slot
        };
        return (RegOp)((int)baseOp + srcIdx);
    }

    /// <summary>Re-encodes one method body into pre-resolved BPI instructions:
    /// short forms normalize to canonical long forms, branch targets become
    /// absolute instruction indices, every token resolves at conversion time,
    /// and an abstract eval-stack simulation stamps operand-kind hints onto
    /// arithmetic/comparison/conversion/branch instructions (a control-flow
    /// merge whose stack shapes disagree, or unreachable code whose kinds are
    /// undecidable, is rejected). Exception regions bake into EHTable records
    /// (instruction-index ranges, metadata order = innermost-first); handler
    /// entries are pre-seeded with their ECMA entry stack (the exception object
    /// for catch, empty for finally/fault). Returns the instruction count, the
    /// body's declared max stack depth, and the method's EHTable run.
    ///
    /// Under <paramref name="regCode"/> the SAME abstract simulation drives
    /// register-form emission instead (docs/BPI-FORMAT.md "Register code
    /// format"): the eval temp at depth d is register slotCount + d — a pure
    /// function of the program point (merge points already enforce identical
    /// stack shapes, so no phis ever arise), nop/pop bake no record, and the
    /// method record's maxStack becomes the simulated maximum eval depth. An
    /// adjacent-record copy-propagation peephole folds most ldarg/ldloc R_MOVs
    /// into the consuming record and most stloc/starg R_MOVs into the
    /// producing record — never across a leader or EH boundary, and never
    /// into a call window. Both modes reject exactly the same bodies; the
    /// stack-format output is byte-for-byte what it was without the
    /// flag.</summary>
    private static (uint InsnCount, uint MaxStack, uint EHStart, uint EHCount) BakeBody(Compilation compilation, BpiWriter w,
        Dictionary<string, uint> typeImports, Dictionary<string, uint> methodImports,
        Dictionary<string, uint> fieldImports, Dictionary<ClassInfo, uint> typeIndex,
        Dictionary<MethodInfo, uint> methodIndex, Dictionary<FieldDefinitionHandle, PatchField> fieldIndex,
        MethodInfo m, MethodBodyBlock body, FrameShape frame, bool regCode)
    {
        string site = $"{m.DeclaringClass.FullName}.{m.Name}";
        var module = m.DeclaringClass.Module;
        var ilBytes = body.GetILBytes()!;
        var insns = ILDecoder.Decode(ilBytes.ToImmutableArrayCompat());

        // Byte offset -> instruction index (the decoder resolves branch operands
        // to absolute byte offsets; the BPI stores absolute instruction indices).
        var indexOf = new Dictionary<int, int>();
        for (int i = 0; i < insns.Count; i++)
            indexOf[insns[i].Offset] = i;

        int IndexAt(int offset, string what)
        {
            if (!indexOf.TryGetValue(offset, out int idx))
                throw new NotSupportedException($"emit-patch: {site} — {what} does not start on an instruction boundary");
            return idx;
        }
        // A region's exclusive end may coincide with the end of the body.
        int EndIndexAt(int offset, string what)
        {
            if (indexOf.TryGetValue(offset, out int idx))
                return idx;
            if (offset == ilBytes.Length)
                return insns.Count;
            throw new NotSupportedException($"emit-patch: {site} — {what} does not end on an instruction boundary");
        }

        // Exception regions, normalized to instruction indices in metadata
        // order (innermost-first, the interpreter's scan order). Filters stay
        // outside the fence.
        var ehRegions = new List<EHRegion>();
        foreach (var r in body.ExceptionRegions)
        {
            uint kind = r.Kind switch
            {
                ExceptionRegionKind.Catch => 0u,
                ExceptionRegionKind.Finally => 2u,
                ExceptionRegionKind.Fault => 3u,
                _ => throw new NotSupportedException($"emit-patch: {site} — filter clauses (catch ... when) are not supported yet"),
            };
            uint catchTypeRef = 0xFFFFFFFFu;
            if (r.Kind == ExceptionRegionKind.Catch)
            {
                // The catch type must be an AOT base-image type (patch types
                // cannot derive from Exception under the type fence).
                if (r.CatchType.Kind != HandleKind.TypeReference)
                    throw new NotSupportedException($"emit-patch: {site} — catch type must be an AOT base-image type");
                var tr = module.Reader.GetTypeReference((TypeReferenceHandle)r.CatchType);
                string ns = module.Reader.GetString(tr.Namespace);
                string typeName = string.IsNullOrEmpty(ns)
                    ? module.Reader.GetString(tr.Name)
                    : ns + "." + module.Reader.GetString(tr.Name);
                catchTypeRef = ImportRef(TypeImportOf(w, typeImports, typeName));
            }
            ehRegions.Add(new EHRegion
            {
                Kind = kind,
                TryStart = (uint)IndexAt(r.TryOffset, "try region"),
                TryEnd = (uint)EndIndexAt(r.TryOffset + r.TryLength, "try region"),
                HandlerStart = (uint)IndexAt(r.HandlerOffset, "handler region"),
                HandlerEnd = (uint)EndIndexAt(r.HandlerOffset + r.HandlerLength, "handler region"),
                CatchTypeRef = catchTypeRef,
            });
        }

        // Register-form emission state (--patch-regcode). Arms buffer RegInsn
        // records here instead of calling w.AddInsn; branch/leave targets and
        // EH boundaries stay IL instruction indices during the walk and are
        // remapped through ilToReg at flush (nop/pop emit no record, so the
        // two streams number instructions differently). RegOf gives the eval
        // temp's register at abstract depth d.
        var regBuf = new List<RegInsn>();
        var ilToReg = regCode ? new int[insns.Count + 1] : Array.Empty<int>();
        int slotCount = frame.ArgKinds.Length + frame.LocalKinds.Length;
        uint RegOf(int depth) => (uint)(slotCount + depth);

        // Leader pre-scan: mark every branch/leave target and EH boundary. In
        // the register stream a control transfer must land on a record
        // boundary — ilToReg pins one per IL instruction, so this is a flush
        // -time consistency check and the fence the copy-propagation peephole
        // in Reg respects (no fold across a point an edge can land on).
        var isLeader = new bool[insns.Count + 1];
        if (regCode)
        {
            foreach (var insn in insns)
            {
                switch (insn.OpCode)
                {
                    case ILOpCode.Br: case ILOpCode.Br_s:
                    case ILOpCode.Brtrue: case ILOpCode.Brtrue_s:
                    case ILOpCode.Brfalse: case ILOpCode.Brfalse_s:
                    case ILOpCode.Beq: case ILOpCode.Beq_s:
                    case ILOpCode.Bge: case ILOpCode.Bge_s:
                    case ILOpCode.Bgt: case ILOpCode.Bgt_s:
                    case ILOpCode.Ble: case ILOpCode.Ble_s:
                    case ILOpCode.Blt: case ILOpCode.Blt_s:
                    case ILOpCode.Bne_un: case ILOpCode.Bne_un_s:
                    case ILOpCode.Bge_un: case ILOpCode.Bge_un_s:
                    case ILOpCode.Bgt_un: case ILOpCode.Bgt_un_s:
                    case ILOpCode.Ble_un: case ILOpCode.Ble_un_s:
                    case ILOpCode.Blt_un: case ILOpCode.Blt_un_s:
                    case ILOpCode.Leave: case ILOpCode.Leave_s:
                        if (indexOf.TryGetValue((int)insn.Operand, out int target))
                            isLeader[target] = true;
                        break;
                }
            }
            foreach (var r in ehRegions)
            {
                isLeader[(int)r.TryStart] = true;
                isLeader[(int)r.TryEnd] = true;
                isLeader[(int)r.HandlerStart] = true;
                isLeader[(int)r.HandlerEnd] = true;
            }
        }

        // Abstract stack simulation state. entryState[i] is the eval-stack
        // shape on entry to instruction i — recorded when first reached in the
        // linear pass or when targeted by a branch, then verified on every
        // other edge into i.
        var entryState = new SlotKind[insns.Count][];
        var stack = new List<SlotKind>();
        bool reachable = true;
        // The simulated maximum eval depth: the register format's method-record
        // maxStack (the register verifier bounds temp operands against it) —
        // tracked in both modes, returned only under regCode.
        int simMax = 0;

        // Copy-propagation peephole state (register form). regFoldFence is
        // true while a leader lies between the last buffered record and the
        // one being emitted — an incoming edge can land in the gap, so no
        // fold may reach across it. regCurIl is the IL index driving the
        // current emission, for ilToReg repair when a fold deletes a record.
        bool regFoldFence = true;
        int regCurIl = 0;
        void RemoveLastReg()
        {
            // Deleting the last record shifts the boundary that the current
            // IL instruction — and any zero-record IL instructions since the
            // deleted record's — were pinned to; walk the (monotone) map back
            // down onto the deleted index.
            int delIdx = regBuf.Count - 1;
            regBuf.RemoveAt(delIdx);
            for (int k = regCurIl; k >= 0 && ilToReg[k] > delIdx; k--)
                ilToReg[k]--;
        }
        void Reg(RegOp op, uint r0 = 0xFF, uint r1 = 0xFF, uint a = 0, uint b = 0, bool targetFixup = false)
        {
            var rec = new RegInsn
            {
                Op = op, R0 = (byte)r0, R1 = (byte)r1, A = a, B = b,
                TargetFixup = targetFixup, FenceBefore = regFoldFence,
            };
            regFoldFence = false;
            // Copy-propagation peephole against the last buffered record only:
            // adjacency is the safety argument (nothing can touch the copied
            // value in between — zero-record IL never pushes), and the fence
            // keeps every incoming edge out of the folded pair. The call
            // family joins neither direction: its r0 is a window base
            // (argument windows must stay contiguous temps, and the result
            // write is not a free dst field), so it exposes no source fields
            // and no plain destination.
            while (!rec.FenceBefore && regBuf.Count > 0)
            {
                var prev = regBuf[regBuf.Count - 1];
                // Load fold: prev copied an arg/local into an eval temp that
                // this record consumes and leaves dead — read the slot
                // directly and drop the copy. The temp is provably dead when
                // it sits at or above the current abstract depth (popped
                // operands live there; only dup reads below the top without
                // popping, and its source depth fails the bound) or when this
                // record's own destination overwrites it.
                bool tempDead = prev.R0 >= RegOf(stack.Count)
                    || (RegOpFold.WritesR0(rec.Op) && rec.R0 == prev.R0);
                if (prev.Op == RegOp.R_MOV && prev.R1 < slotCount && tempDead
                    && RegReadsSrc(rec, prev.R0))
                {
                    RegRewriteSrc(ref rec, prev.R0, prev.R1);
                    rec.FenceBefore = prev.FenceBefore; // now spans the deleted record's gap
                    RemoveLastReg();
                    continue; // the rewrite may expose another foldable copy
                }
                // Store fold: this record is a stloc/starg copy of the temp
                // prev just produced (the copy pops it dead) — retarget
                // prev's destination to the slot and bake no record at all.
                // The register equality is also the adjacency proof: an
                // intervening zero-record pop leaves the depths disagreeing.
                if (rec.Op == RegOp.R_MOV && rec.R0 < slotCount && rec.R1 >= slotCount
                    && RegOpFold.WritesR0(prev.Op) && prev.R0 == rec.R1)
                {
                    prev.R0 = rec.R0;
                    regBuf[regBuf.Count - 1] = prev;
                    return;
                }
                break;
            }
            regBuf.Add(rec);
        }

        // Handler entries are reached by EH dispatch, not by an encoded edge:
        // a catch handler starts with the exception object as the only stack
        // slot, a finally/fault handler with an empty stack.
        foreach (var r in ehRegions)
        {
            entryState[r.HandlerStart] = r.Kind == 0u
                ? new[] { SlotKind.Ref }
                : Array.Empty<SlotKind>();
        }

        SlotKind Pop(int offset)
        {
            if (stack.Count == 0)
                throw new NotSupportedException($"emit-patch: {site} — eval-stack underflow at IL_{offset:X4} (unsupported IL shape)");
            var k = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return k;
        }
        void Push(SlotKind k)
        {
            stack.Add(k);
            if (stack.Count > simMax)
                simMax = stack.Count;
        }
        bool SameShape(IReadOnlyList<SlotKind> x, IReadOnlyList<SlotKind> y)
        {
            if (x.Count != y.Count)
                return false;
            for (int i = 0; i < x.Count; i++)
                if (x[i] != y[i])
                    return false;
            return true;
        }
        // Records/verifies the current stack shape as the entry state of a
        // branch target and returns the target's absolute instruction index.
        uint Branch(int targetOffset, int atOffset)
        {
            if (!indexOf.TryGetValue(targetOffset, out int t))
                throw new NotSupportedException($"emit-patch: {site} — branch target inside an instruction at IL_{atOffset:X4}");
            var state = stack.ToArray();
            if (entryState[t] is { } existing)
            {
                if (!SameShape(existing, state))
                    throw new NotSupportedException($"emit-patch: {site} — eval-stack shapes disagree at a control-flow merge (IL_{targetOffset:X4})");
            }
            else
            {
                entryState[t] = state;
            }
            return (uint)t;
        }

        for (int i = 0; i < insns.Count; i++)
        {
            var insn = insns[i];
            if (regCode)
            {
                ilToReg[i] = regBuf.Count;
                regCurIl = i;
                if (isLeader[i])
                    regFoldFence = true;
            }
            if (entryState[i] is { } snap)
            {
                if (!reachable)
                {
                    stack.Clear();
                    stack.AddRange(snap);
                    if (stack.Count > simMax)
                        simMax = stack.Count; // e.g. a catch handler's entry depth of 1
                    reachable = true;
                }
                else if (!SameShape(stack, snap))
                {
                    throw new NotSupportedException($"emit-patch: {site} — eval-stack shapes disagree at a control-flow merge (IL_{insn.Offset:X4})");
                }
            }
            else if (!reachable)
            {
                // Not fall-through-reachable and not yet targeted by a forward
                // branch: the entry of a block only reached by a later backward
                // branch — e.g. a loop body the compiler laid out before its
                // condition. Compiler output enters such blocks with an empty
                // eval stack; assume that and let the branch edge verify it
                // when it arrives (a mismatch is rejected at the merge).
                stack.Clear();
                reachable = true;
                entryState[i] = Array.Empty<SlotKind>();
            }
            else
            {
                entryState[i] = stack.ToArray();
            }

            switch (insn.OpCode)
            {
                case ILOpCode.Nop:
                    if (!regCode) // no register record: nothing to do
                        w.AddInsn((uint)ILOpCode.Nop, 0, 0, 0);
                    break;

                case ILOpCode.Ldc_i4_m1:
                case ILOpCode.Ldc_i4_0:
                case ILOpCode.Ldc_i4_1:
                case ILOpCode.Ldc_i4_2:
                case ILOpCode.Ldc_i4_3:
                case ILOpCode.Ldc_i4_4:
                case ILOpCode.Ldc_i4_5:
                case ILOpCode.Ldc_i4_6:
                case ILOpCode.Ldc_i4_7:
                case ILOpCode.Ldc_i4_8:
                {
                    uint value = unchecked((uint)((int)insn.OpCode - (int)ILOpCode.Ldc_i4_0));
                    if (regCode)
                        Reg(RegOp.R_LDC_I32, r0: RegOf(stack.Count), a: value);
                    else
                        w.AddInsn((uint)ILOpCode.Ldc_i4, 0, value, 0);
                    Push(SlotKind.I32);
                    break;
                }
                case ILOpCode.Ldc_i4_s:
                case ILOpCode.Ldc_i4:
                    if (regCode)
                        Reg(RegOp.R_LDC_I32, r0: RegOf(stack.Count), a: unchecked((uint)(int)insn.Operand));
                    else
                        w.AddInsn((uint)ILOpCode.Ldc_i4, 0, unchecked((uint)(int)insn.Operand), 0);
                    Push(SlotKind.I32);
                    break;
                case ILOpCode.Ldc_i8:
                {
                    ulong bits = unchecked((ulong)insn.Operand);
                    if (regCode)
                        Reg(RegOp.R_LDC_I64, r0: RegOf(stack.Count), a: (uint)bits, b: (uint)(bits >> 32));
                    else
                        w.AddInsn((uint)ILOpCode.Ldc_i8, 0, (uint)bits, (uint)(bits >> 32));
                    Push(SlotKind.I64);
                    break;
                }
                case ILOpCode.Ldc_r4:
                {
                    uint bits = unchecked((uint)BitConverter.SingleToInt32Bits((float)insn.FloatOperand));
                    if (regCode)
                        Reg(RegOp.R_LDC_R4, r0: RegOf(stack.Count), a: bits);
                    else
                        w.AddInsn((uint)ILOpCode.Ldc_r4, 0, bits, 0);
                    Push(SlotKind.F32);
                    break;
                }
                case ILOpCode.Ldc_r8:
                {
                    ulong bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(insn.FloatOperand));
                    if (regCode)
                        Reg(RegOp.R_LDC_R8, r0: RegOf(stack.Count), a: (uint)bits, b: (uint)(bits >> 32));
                    else
                        w.AddInsn((uint)ILOpCode.Ldc_r8, 0, (uint)bits, (uint)(bits >> 32));
                    Push(SlotKind.F64);
                    break;
                }

                case ILOpCode.Ldarg_0:
                case ILOpCode.Ldarg_1:
                case ILOpCode.Ldarg_2:
                case ILOpCode.Ldarg_3:
                case ILOpCode.Ldarg_s:
                case ILOpCode.Ldarg:
                {
                    int slot = insn.OpCode >= ILOpCode.Ldarg_0 && insn.OpCode <= ILOpCode.Ldarg_3
                        ? (int)insn.OpCode - (int)ILOpCode.Ldarg_0
                        : (int)insn.Operand;
                    if (slot >= frame.ArgKinds.Length)
                        throw new NotSupportedException($"emit-patch: {site} — argument slot {slot} out of range");
                    if (regCode) // args live at regs[0..argCount)
                        Reg(RegOp.R_MOV, r0: RegOf(stack.Count), r1: (uint)slot);
                    else
                        w.AddInsn((uint)ILOpCode.Ldarg, 0, (uint)slot, 0);
                    Push(frame.ArgKinds[slot]);
                    break;
                }
                case ILOpCode.Starg_s:
                case ILOpCode.Starg:
                {
                    int slot = (int)insn.Operand;
                    if (slot >= frame.ArgKinds.Length)
                        throw new NotSupportedException($"emit-patch: {site} — argument slot {slot} out of range");
                    if (Pop(insn.Offset) != frame.ArgKinds[slot])
                        throw new NotSupportedException($"emit-patch: {site} — starg operand kind mismatch at IL_{insn.Offset:X4}");
                    if (regCode)
                        Reg(RegOp.R_MOV, r0: (uint)slot, r1: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)ILOpCode.Starg, 0, (uint)slot, 0);
                    break;
                }
                case ILOpCode.Ldloc_0:
                case ILOpCode.Ldloc_1:
                case ILOpCode.Ldloc_2:
                case ILOpCode.Ldloc_3:
                case ILOpCode.Ldloc_s:
                case ILOpCode.Ldloc:
                {
                    int slot = insn.OpCode >= ILOpCode.Ldloc_0 && insn.OpCode <= ILOpCode.Ldloc_3
                        ? (int)insn.OpCode - (int)ILOpCode.Ldloc_0
                        : (int)insn.Operand;
                    if (slot >= frame.LocalKinds.Length)
                        throw new NotSupportedException($"emit-patch: {site} — local slot {slot} out of range");
                    if (regCode) // locals live at regs[argCount + localIdx]
                        Reg(RegOp.R_MOV, r0: RegOf(stack.Count), r1: (uint)(frame.ArgKinds.Length + slot));
                    else
                        w.AddInsn((uint)ILOpCode.Ldloc, 0, (uint)slot, 0);
                    Push(frame.LocalKinds[slot]);
                    break;
                }
                case ILOpCode.Stloc_0:
                case ILOpCode.Stloc_1:
                case ILOpCode.Stloc_2:
                case ILOpCode.Stloc_3:
                case ILOpCode.Stloc_s:
                case ILOpCode.Stloc:
                {
                    int slot = insn.OpCode >= ILOpCode.Stloc_0 && insn.OpCode <= ILOpCode.Stloc_3
                        ? (int)insn.OpCode - (int)ILOpCode.Stloc_0
                        : (int)insn.Operand;
                    if (slot >= frame.LocalKinds.Length)
                        throw new NotSupportedException($"emit-patch: {site} — local slot {slot} out of range");
                    if (Pop(insn.Offset) != frame.LocalKinds[slot])
                        throw new NotSupportedException($"emit-patch: {site} — stloc operand kind mismatch at IL_{insn.Offset:X4}");
                    if (regCode)
                        Reg(RegOp.R_MOV, r0: (uint)(frame.ArgKinds.Length + slot), r1: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)ILOpCode.Stloc, 0, (uint)slot, 0);
                    break;
                }

                case ILOpCode.Dup:
                {
                    if (stack.Count == 0)
                        throw new NotSupportedException($"emit-patch: {site} — eval-stack underflow at IL_{insn.Offset:X4} (unsupported IL shape)");
                    if (regCode)
                        Reg(RegOp.R_MOV, r0: RegOf(stack.Count), r1: RegOf(stack.Count - 1));
                    else
                        w.AddInsn((uint)ILOpCode.Dup, 0, 0, 0);
                    Push(stack[stack.Count - 1]);
                    break;
                }
                case ILOpCode.Pop:
                    Pop(insn.Offset);
                    if (!regCode) // no register record: the temp just goes dead
                        w.AddInsn((uint)ILOpCode.Pop, 0, 0, 0);
                    break;

                case ILOpCode.Ldstr:
                {
                    string value = module.Reader.GetUserString(SRME.UserStringHandle(insn.Token));
                    uint strOff = w.InternUserString(value);
                    if (regCode)
                        Reg(RegOp.R_LDSTR, r0: RegOf(stack.Count), a: strOff);
                    else
                        w.AddInsn((uint)ILOpCode.Ldstr, 0, strOff, 0);
                    Push(SlotKind.Ref);
                    break;
                }
                case ILOpCode.Ldnull:
                    if (regCode)
                        Reg(RegOp.R_LDNULL, r0: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)ILOpCode.Ldnull, 0, 0, 0);
                    Push(SlotKind.Ref);
                    break;

                case ILOpCode.Br:
                case ILOpCode.Br_s:
                {
                    uint target = Branch((int)insn.Operand, insn.Offset);
                    if (regCode)
                        Reg(RegOp.R_BR, a: target, targetFixup: true);
                    else
                        w.AddInsn((uint)ILOpCode.Br, 0, target, 0);
                    reachable = false;
                    stack.Clear();
                    break;
                }
                case ILOpCode.Brtrue:
                case ILOpCode.Brtrue_s:
                case ILOpCode.Brfalse:
                case ILOpCode.Brfalse_s:
                {
                    var k = Pop(insn.Offset);
                    if (k is SlotKind.F32 or SlotKind.F64)
                        throw new NotSupportedException($"emit-patch: {site} — brtrue/brfalse on a float operand at IL_{insn.Offset:X4}");
                    bool isTrue = insn.OpCode is ILOpCode.Brtrue or ILOpCode.Brtrue_s;
                    uint target = Branch((int)insn.Operand, insn.Offset);
                    if (regCode)
                    {
                        var op = k switch
                        {
                            SlotKind.I64 => isTrue ? RegOp.R_BRTRUE_I64 : RegOp.R_BRFALSE_I64,
                            SlotKind.Ref => isTrue ? RegOp.R_BRTRUE_REF : RegOp.R_BRFALSE_REF,
                            _ => isTrue ? RegOp.R_BRTRUE_I32 : RegOp.R_BRFALSE_I32,
                        };
                        Reg(op, r0: RegOf(stack.Count), a: target, targetFixup: true);
                    }
                    else
                    {
                        w.AddInsn(isTrue ? (uint)ILOpCode.Brtrue : (uint)ILOpCode.Brfalse, 0, target, (uint)k);
                    }
                    break;
                }
                case ILOpCode.Beq:
                case ILOpCode.Beq_s:
                case ILOpCode.Bge:
                case ILOpCode.Bge_s:
                case ILOpCode.Bgt:
                case ILOpCode.Bgt_s:
                case ILOpCode.Ble:
                case ILOpCode.Ble_s:
                case ILOpCode.Blt:
                case ILOpCode.Blt_s:
                case ILOpCode.Bne_un:
                case ILOpCode.Bne_un_s:
                case ILOpCode.Bge_un:
                case ILOpCode.Bge_un_s:
                case ILOpCode.Bgt_un:
                case ILOpCode.Bgt_un_s:
                case ILOpCode.Ble_un:
                case ILOpCode.Ble_un_s:
                case ILOpCode.Blt_un:
                case ILOpCode.Blt_un_s:
                {
                    var b = Pop(insn.Offset);
                    var a = Pop(insn.Offset);
                    if (a != b)
                        throw new NotSupportedException($"emit-patch: {site} — comparison operand kinds disagree at IL_{insn.Offset:X4}");
                    var canon = CanonicalBranch(insn.OpCode);
                    if (a == SlotKind.Ref && canon is not (ILOpCode.Beq or ILOpCode.Bne_un))
                        throw new NotSupportedException($"emit-patch: {site} — ordered comparison on references at IL_{insn.Offset:X4}");
                    uint target = Branch((int)insn.Operand, insn.Offset);
                    if (regCode) // operands at their pre-pop depths
                        Reg(RegBrCmpOp(canon, a), r0: RegOf(stack.Count), r1: RegOf(stack.Count + 1),
                            a: target, targetFixup: true);
                    else
                        w.AddInsn((uint)canon, 0, target, (uint)a);
                    break;
                }

                case ILOpCode.Add:
                case ILOpCode.Sub:
                case ILOpCode.Mul:
                case ILOpCode.Div:
                case ILOpCode.Div_un:
                case ILOpCode.Rem:
                case ILOpCode.Rem_un:
                case ILOpCode.And:
                case ILOpCode.Or:
                case ILOpCode.Xor:
                {
                    var b = Pop(insn.Offset);
                    var a = Pop(insn.Offset);
                    if (a != b)
                        throw new NotSupportedException($"emit-patch: {site} — arithmetic operand kinds disagree at IL_{insn.Offset:X4}");
                    if (a == SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — arithmetic on references at IL_{insn.Offset:X4}");
                    bool intOnly = insn.OpCode is ILOpCode.Div_un or ILOpCode.Rem_un
                        or ILOpCode.And or ILOpCode.Or or ILOpCode.Xor;
                    if (intOnly && a is SlotKind.F32 or SlotKind.F64)
                        throw new NotSupportedException($"emit-patch: {site} — integer-only operation on floats at IL_{insn.Offset:X4}");
                    if (regCode) // dst overwrites the first operand's temp
                        Reg(RegBinOp(insn.OpCode, a), r0: RegOf(stack.Count), r1: RegOf(stack.Count),
                            a: RegOf(stack.Count + 1));
                    else
                        w.AddInsn((uint)insn.OpCode, 0, (uint)a, 0);
                    Push(a);
                    break;
                }
                case ILOpCode.Shl:
                case ILOpCode.Shr:
                case ILOpCode.Shr_un:
                {
                    var amount = Pop(insn.Offset);
                    var value = Pop(insn.Offset);
                    if (amount != SlotKind.I32 || value is not (SlotKind.I32 or SlotKind.I64))
                        throw new NotSupportedException($"emit-patch: {site} — unsupported shift operand kinds at IL_{insn.Offset:X4}");
                    if (regCode)
                    {
                        var op = insn.OpCode switch
                        {
                            ILOpCode.Shl => RegOp.R_SHL_I32,
                            ILOpCode.Shr => RegOp.R_SHR_I32,
                            _ => RegOp.R_SHR_UN_I32,
                        };
                        Reg((RegOp)((int)op + (value == SlotKind.I64 ? 1 : 0)), r0: RegOf(stack.Count),
                            r1: RegOf(stack.Count), a: RegOf(stack.Count + 1));
                    }
                    else
                    {
                        w.AddInsn((uint)insn.OpCode, 0, (uint)value, 0);
                    }
                    Push(value);
                    break;
                }
                case ILOpCode.Neg:
                {
                    var k = Pop(insn.Offset);
                    if (k == SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — neg on a reference at IL_{insn.Offset:X4}");
                    if (regCode) // the unary block is laid out in SlotKind order
                        Reg((RegOp)((int)RegOp.R_NEG_I32 + (int)k), r0: RegOf(stack.Count), r1: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)ILOpCode.Neg, 0, (uint)k, 0);
                    Push(k);
                    break;
                }
                case ILOpCode.Not:
                {
                    var k = Pop(insn.Offset);
                    if (k is not (SlotKind.I32 or SlotKind.I64))
                        throw new NotSupportedException($"emit-patch: {site} — not on a non-integer operand at IL_{insn.Offset:X4}");
                    if (regCode)
                        Reg((RegOp)((int)RegOp.R_NOT_I32 + (k == SlotKind.I64 ? 1 : 0)), r0: RegOf(stack.Count), r1: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)ILOpCode.Not, 0, (uint)k, 0);
                    Push(k);
                    break;
                }

                case ILOpCode.Ceq:
                case ILOpCode.Cgt:
                case ILOpCode.Cgt_un:
                case ILOpCode.Clt:
                case ILOpCode.Clt_un:
                {
                    var b = Pop(insn.Offset);
                    var a = Pop(insn.Offset);
                    if (a != b)
                        throw new NotSupportedException($"emit-patch: {site} — comparison operand kinds disagree at IL_{insn.Offset:X4}");
                    if (a == SlotKind.Ref && insn.OpCode is not (ILOpCode.Ceq or ILOpCode.Cgt_un))
                        throw new NotSupportedException($"emit-patch: {site} — ordered comparison on references at IL_{insn.Offset:X4}");
                    if (regCode)
                        Reg(RegCmpOp(insn.OpCode, a), r0: RegOf(stack.Count), r1: RegOf(stack.Count),
                            a: RegOf(stack.Count + 1));
                    else
                        w.AddInsn((uint)insn.OpCode, 0, (uint)a, 0);
                    Push(SlotKind.I32);
                    break;
                }

                case ILOpCode.Conv_i4:
                case ILOpCode.Conv_u4:
                case ILOpCode.Conv_i8:
                case ILOpCode.Conv_u8:
                case ILOpCode.Conv_r4:
                case ILOpCode.Conv_r8:
                {
                    var k = Pop(insn.Offset);
                    if (k == SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — conversion of a reference at IL_{insn.Offset:X4}");
                    if (regCode)
                        Reg(RegConvOp(insn.OpCode, k), r0: RegOf(stack.Count), r1: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)insn.OpCode, 0, (uint)k, 0);
                    Push(insn.OpCode switch
                    {
                        ILOpCode.Conv_i4 or ILOpCode.Conv_u4 => SlotKind.I32,
                        ILOpCode.Conv_i8 or ILOpCode.Conv_u8 => SlotKind.I64,
                        ILOpCode.Conv_r4 => SlotKind.F32,
                        _ => SlotKind.F64,
                    });
                    break;
                }

                case ILOpCode.Ret:
                {
                    uint hasValue = 0;
                    if (frame.ReturnKind is { } rk)
                    {
                        if (Pop(insn.Offset) != rk)
                            throw new NotSupportedException($"emit-patch: {site} — return operand kind mismatch at IL_{insn.Offset:X4}");
                        hasValue = 1;
                    }
                    if (stack.Count != 0)
                        throw new NotSupportedException($"emit-patch: {site} — eval stack not empty at ret (unsupported IL shape)");
                    if (regCode)
                    {
                        if (hasValue == 1)
                            Reg(RegOp.R_RET, r0: RegOf(0)); // the popped value was the depth-0 temp
                        else
                            Reg(RegOp.R_RET_VOID);
                    }
                    else
                    {
                        w.AddInsn((uint)ILOpCode.Ret, 0, hasValue, 0);
                    }
                    reachable = false;
                    break;
                }

                case ILOpCode.Throw:
                    if (Pop(insn.Offset) != SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — throw operand is not a reference at IL_{insn.Offset:X4}");
                    if (regCode)
                        Reg(RegOp.R_THROW, r0: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)ILOpCode.Throw, 0, 0, 0);
                    reachable = false;
                    stack.Clear();
                    break;

                case ILOpCode.Rethrow:
                {
                    // Rethrow re-raises the exception the innermost enclosing
                    // catch handler is holding; bake that handler's
                    // method-relative EHTable index so the interpreter never
                    // scans for it at run time.
                    int rec = -1;
                    for (int r = 0; r < ehRegions.Count; r++)
                    {
                        if (ehRegions[r].Kind == 0u
                            && ehRegions[r].HandlerStart <= (uint)i && (uint)i < ehRegions[r].HandlerEnd)
                        {
                            rec = r;
                            break;
                        }
                    }
                    if (rec < 0)
                        throw new NotSupportedException($"emit-patch: {site} — rethrow outside a catch handler at IL_{insn.Offset:X4}");
                    if (regCode) // the method-relative EH index, not a branch target
                        Reg(RegOp.R_RETHROW, a: (uint)rec);
                    else
                        w.AddInsn((uint)ILOpCode.Rethrow, 0, (uint)rec, 0);
                    reachable = false;
                    stack.Clear();
                    break;
                }

                case ILOpCode.Leave:
                case ILOpCode.Leave_s:
                {
                    // Leave empties the eval stack before it targets (ECMA);
                    // the interpreter routes through the finally handlers of
                    // every region left between here and the target.
                    stack.Clear();
                    uint target = Branch((int)insn.Operand, insn.Offset);
                    if (regCode)
                        Reg(RegOp.R_LEAVE, a: target, targetFixup: true);
                    else
                        w.AddInsn((uint)ILOpCode.Leave, 0, target, 0);
                    reachable = false;
                    break;
                }

                case ILOpCode.Endfinally: // also endfault
                    if (regCode)
                        Reg(RegOp.R_ENDFINALLY);
                    else
                        w.AddInsn((uint)ILOpCode.Endfinally, 0, 0, 0);
                    reachable = false;
                    stack.Clear();
                    break;

                case ILOpCode.Call:
                case ILOpCode.Callvirt:
                {
                    var handle = SRME.EntityHandle(insn.Token);
                    if (handle.Kind == HandleKind.MethodDefinition)
                    {
                        // Intra-patch call (static, instance, a base/`this(...)`
                        // ctor chain, or a virtual dispatch): the operand is the
                        // callee's MethodTable index (PatchEntity tag = 0), with
                        // the has-result flag in `b` bit0 and the instance flag
                        // (receiver null-checked by the interpreter) in bit1. A
                        // callvirt on a NON-virtual patch method canonicalizes
                        // to call (the direct call is the exact dispatch); a
                        // callvirt on a virtual patch method — an override,
                        // reached through a patch-class-typed receiver — stays
                        // callvirt, and the interpreter re-resolves the frozen
                        // slot against the receiver's live patch type, so a
                        // patch-derived receiver lands on its own re-override.
                        // (An override reached through an AOT-base-typed
                        // receiver is a method *import* callvirt instead, which
                        // dispatches the live vtable.)
                        if (!module.MethodMap.TryGetValue((MethodDefinitionHandle)handle, out var target)
                            || !methodIndex.TryGetValue(target, out uint calleeIdx))
                            throw new NotSupportedException($"emit-patch: {site} — call target is not a baked patch method");
                        if (target.Name == ".cctor")
                            throw new NotSupportedException($"emit-patch: {site} — a static constructor cannot be called directly");
                        int d = stack.Count; // pre-pop depth: the call window's top
                        var ps = target.Signature.ParameterTypes;
                        for (int p = ps.Length - 1; p >= 0; p--)
                            if (Pop(insn.Offset) != KindOfType(compilation, ps[p]))
                                throw new NotSupportedException($"emit-patch: {site} — call argument kind mismatch at IL_{insn.Offset:X4}");
                        uint bBits = 0;
                        if (!target.IsStatic)
                        {
                            if (Pop(insn.Offset) != SlotKind.Ref)
                                throw new NotSupportedException($"emit-patch: {site} — instance-call receiver is not a reference at IL_{insn.Offset:X4}");
                            bBits |= 2;
                        }
                        if (!target.Signature.ReturnType.IsVoid)
                        {
                            Push(KindOfType(compilation, target.Signature.ReturnType)!.Value);
                            bBits |= 1;
                        }
                        bool virtCall = insn.OpCode == ILOpCode.Callvirt && target.IsVirtual;
                        if (regCode)
                        {
                            // Window = [receiver?, args...]: the consumed run of
                            // temps; the result (b bit0) lands back in its base.
                            int consumed = ps.Length + (target.IsStatic ? 0 : 1);
                            Reg(virtCall ? RegOp.R_CALLVIRT : RegOp.R_CALL,
                                r0: RegOf(d - consumed), a: calleeIdx, b: bBits);
                        }
                        else
                        {
                            w.AddInsn(virtCall ? (uint)ILOpCode.Callvirt : (uint)ILOpCode.Call, 0, calleeIdx, bBits);
                        }
                    }
                    else
                    {
                        // A root patch ctor chains to `System.Object::.ctor()`,
                        // which is a no-op on the runtime object layout: fold
                        // the call to dropping the receiver.
                        if (IsObjectCtorCall(module.Reader, handle))
                        {
                            if (Pop(insn.Offset) != SlotKind.Ref)
                                throw new NotSupportedException($"emit-patch: {site} — instance-call receiver is not a reference at IL_{insn.Offset:X4}");
                            if (!regCode) // register form: the temp just goes dead
                                w.AddInsn((uint)ILOpCode.Pop, 0, 0, 0);
                            break;
                        }
                        // External (AOT base image) call through a method
                        // import — static or instance (including the `.ctor`
                        // an inheriting patch ctor chains into); a callvirt
                        // dispatches through the receiver's vtable at run time
                        // when the bound method is virtual. `b` bit0 = pushes
                        // a result.
                        var import = ResolveCallImport(compilation, w, typeImports, methodImports, m, insn.Token, isNewobj: false);
                        int d = stack.Count; // pre-pop depth: the call window's top
                        for (int p = import.ArgKinds.Length - 1; p >= 0; p--)
                            if (Pop(insn.Offset) != import.ArgKinds[p])
                                throw new NotSupportedException($"emit-patch: {site} — call argument kind mismatch at IL_{insn.Offset:X4}");
                        if (import.IsInstance && Pop(insn.Offset) != SlotKind.Ref)
                            throw new NotSupportedException($"emit-patch: {site} — instance-call receiver is not a reference at IL_{insn.Offset:X4}");
                        uint pushesResult = 0;
                        if (import.RetKind is { } rk)
                        {
                            Push(rk);
                            pushesResult = 1;
                        }
                        if (regCode)
                        {
                            int consumed = import.ArgKinds.Length + (import.IsInstance ? 1 : 0);
                            Reg(insn.OpCode == ILOpCode.Callvirt ? RegOp.R_CALLVIRT : RegOp.R_CALL,
                                r0: RegOf(d - consumed), a: ImportRef(import.ImportIdx), b: pushesResult);
                        }
                        else
                        {
                            w.AddInsn((uint)insn.OpCode, 0, ImportRef(import.ImportIdx), pushesResult);
                        }
                    }
                    break;
                }

                case ILOpCode.Newobj:
                {
                    var handle = SRME.EntityHandle(insn.Token);
                    if (handle.Kind == HandleKind.MethodDefinition)
                    {
                        // Patch-type construction: the operand is the baked
                        // `.ctor`'s MethodTable index (PatchEntity tag = 0);
                        // the interpreter allocates from the load-time
                        // constructed type-info and runs the ctor frame with
                        // the fresh object as its `this` slot.
                        if (!module.MethodMap.TryGetValue((MethodDefinitionHandle)handle, out var target)
                            || !methodIndex.TryGetValue(target, out uint ctorIdx))
                            throw new NotSupportedException($"emit-patch: {site} — newobj target is not a baked patch constructor");
                        if (target.Name != ".ctor" || target.IsStatic)
                            throw new NotSupportedException($"emit-patch: {site} — newobj target {target.DeclaringClass.FullName}.{target.Name} is not an instance constructor");
                        var ps = target.Signature.ParameterTypes;
                        for (int p = ps.Length - 1; p >= 0; p--)
                            if (Pop(insn.Offset) != KindOfType(compilation, ps[p]))
                                throw new NotSupportedException($"emit-patch: {site} — ctor argument kind mismatch at IL_{insn.Offset:X4}");
                        if (regCode)
                        {
                            // The window holds the ctor parameters only (the
                            // interpreter allocates the receiver itself); the
                            // constructed reference lands in the window base —
                            // exactly the temp the result is pushed to.
                            Reg(RegOp.R_NEWOBJ, r0: RegOf(stack.Count), a: ctorIdx, b: 1);
                        }
                        else
                        {
                            w.AddInsn((uint)ILOpCode.Newobj, 0, ctorIdx, 0);
                        }
                        Push(SlotKind.Ref);
                        break;
                    }
                    // Delegate construction: a base-image delegate's
                    // `.ctor(object, native int)` — that ctor shape is the
                    // reliable discriminator (the delegate type itself is a
                    // base-image type not loaded here). The stack holds the
                    // ldftn method reference on top of the target; bake a
                    // delegate newobj carrying the delegate type import, and the
                    // interpreter builds the real delegate over the popped
                    // closure + target. A generic delegate (Func<>/Action<>) is a
                    // closed generic instantiation resolved to its mangled
                    // registry name against the manifest (the AOT boundary rejects
                    // one the base image never emitted).
                    if (handle.Kind == HandleKind.MemberReference)
                    {
                        var dmr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
                        var dsig = dmr.DecodeMethodSignature(compilation.SigProvider, m.Context);
                        if (IsDelegateCtorSig(module.Reader, dmr, dsig))
                        {
                            string delName;
                            if (dmr.Parent.Kind == HandleKind.TypeReference)
                            {
                                var dtr = module.Reader.GetTypeReference((TypeReferenceHandle)dmr.Parent);
                                string dns = module.Reader.GetString(dtr.Namespace);
                                delName = string.IsNullOrEmpty(dns)
                                    ? module.Reader.GetString(dtr.Name)
                                    : dns + "." + module.Reader.GetString(dtr.Name);
                            }
                            else if (dmr.Parent.Kind == HandleKind.TypeSpecification)
                            {
                                // A generic delegate: resolve the instantiation to
                                // its mangled registry name (or the AOT boundary).
                                var dt = module.Reader.GetTypeSpecification((TypeSpecificationHandle)dmr.Parent)
                                    .DecodeSignature(compilation.SigProvider, m.Context);
                                if (dt.Kind != TypeKind.ExternalGeneric)
                                    throw new NotSupportedException($"emit-patch: {site} — unsupported delegate type {dt}");
                                delName = dt.ExternalName!;
                            }
                            else
                            {
                                throw new NotSupportedException($"emit-patch: {site} — unsupported delegate parent {dmr.Parent.Kind}");
                            }
                            if (Pop(insn.Offset) != SlotKind.Ref)
                                throw new NotSupportedException($"emit-patch: {site} — delegate method reference is not a reference at IL_{insn.Offset:X4}");
                            if (Pop(insn.Offset) != SlotKind.Ref)
                                throw new NotSupportedException($"emit-patch: {site} — delegate target is not a reference at IL_{insn.Offset:X4}");
                            uint delImport = TypeImportOf(w, typeImports, delName);
                            if (regCode) // window = [target, ftn]; the delegate lands in its base
                                Reg(RegOp.R_NEWOBJ, r0: RegOf(stack.Count), a: ImportRef(delImport), b: 1);
                            else
                                w.AddInsn((uint)ILOpCode.Newobj, 0, ImportRef(delImport), 0);
                            Push(SlotKind.Ref);
                            break;
                        }
                    }
                    // AOT reference-type construction: a ctor import; the
                    // interpreter allocates from the bound type-info and runs
                    // the ctor through its invoker thunk.
                    var import = ResolveCallImport(compilation, w, typeImports, methodImports, m, insn.Token, isNewobj: true);
                    for (int p = import.ArgKinds.Length - 1; p >= 0; p--)
                        if (Pop(insn.Offset) != import.ArgKinds[p])
                            throw new NotSupportedException($"emit-patch: {site} — ctor argument kind mismatch at IL_{insn.Offset:X4}");
                    if (regCode) // window = the ctor parameters; the object lands in its base
                        Reg(RegOp.R_NEWOBJ, r0: RegOf(stack.Count), a: ImportRef(import.ImportIdx), b: 1);
                    else
                        w.AddInsn((uint)ILOpCode.Newobj, 0, ImportRef(import.ImportIdx), 0);
                    Push(SlotKind.Ref);
                    break;
                }

                case ILOpCode.Ldfld:
                case ILOpCode.Stfld:
                case ILOpCode.Ldsfld:
                case ILOpCode.Stsfld:
                {
                    // A FieldDefinition token is a patch-declared field: the
                    // operand is its FieldTable index (PatchEntity tag = 0)
                    // with the declaring TypeTable index in `b` (the runtime's
                    // lazy-cctor guard anchor for statics). Instance-field
                    // byte offsets are loader-assigned, so the instruction
                    // carries no numeric layout.
                    var fieldHandle = SRME.EntityHandle(insn.Token);
                    if (fieldHandle.Kind == HandleKind.FieldDefinition)
                    {
                        if (!fieldIndex.TryGetValue((FieldDefinitionHandle)fieldHandle, out var pf))
                            throw new NotSupportedException($"emit-patch: {site} — field target is not a baked patch field");
                        bool wantStatic = insn.OpCode is ILOpCode.Ldsfld or ILOpCode.Stsfld;
                        if (pf.IsStatic != wantStatic)
                            throw new NotSupportedException($"emit-patch: {site} — field-access staticness mismatch at IL_{insn.Offset:X4}");
                        int d = stack.Count; // pre-pop depth
                        switch (insn.OpCode)
                        {
                            case ILOpCode.Ldfld:
                                if (Pop(insn.Offset) != SlotKind.Ref)
                                    throw new NotSupportedException($"emit-patch: {site} — ldfld receiver is not a reference at IL_{insn.Offset:X4}");
                                Push(pf.Kind);
                                break;
                            case ILOpCode.Stfld:
                                if (Pop(insn.Offset) != pf.Kind)
                                    throw new NotSupportedException($"emit-patch: {site} — stfld operand kind mismatch at IL_{insn.Offset:X4}");
                                if (Pop(insn.Offset) != SlotKind.Ref)
                                    throw new NotSupportedException($"emit-patch: {site} — stfld receiver is not a reference at IL_{insn.Offset:X4}");
                                break;
                            case ILOpCode.Ldsfld:
                                Push(pf.Kind);
                                break;
                            default: // stsfld
                                if (Pop(insn.Offset) != pf.Kind)
                                    throw new NotSupportedException($"emit-patch: {site} — stsfld operand kind mismatch at IL_{insn.Offset:X4}");
                                break;
                        }
                        if (regCode)
                        {
                            switch (insn.OpCode)
                            {
                                case ILOpCode.Ldfld: // dst overwrites the object's temp
                                    Reg(RegOp.R_LDFLD, r0: RegOf(d - 1), r1: RegOf(d - 1), a: pf.FieldIdx, b: pf.TypeIdx);
                                    break;
                                case ILOpCode.Stfld: // r0 = obj, r1 = value
                                    Reg(RegOp.R_STFLD, r0: RegOf(d - 2), r1: RegOf(d - 1), a: pf.FieldIdx, b: pf.TypeIdx);
                                    break;
                                case ILOpCode.Ldsfld:
                                    Reg(RegOp.R_LDSFLD, r0: RegOf(d), a: pf.FieldIdx, b: pf.TypeIdx);
                                    break;
                                default: // stsfld: r0 = value
                                    Reg(RegOp.R_STSFLD, r0: RegOf(d - 1), a: pf.FieldIdx, b: pf.TypeIdx);
                                    break;
                            }
                        }
                        else
                        {
                            w.AddInsn((uint)insn.OpCode, 0, pf.FieldIdx, pf.TypeIdx);
                        }
                        break;
                    }
                    var (importIdx, kind) = ResolveFieldImport(compilation, w, typeImports, fieldImports, m, insn.Token);
                    int di = stack.Count; // pre-pop depth
                    switch (insn.OpCode)
                    {
                        case ILOpCode.Ldfld:
                            if (Pop(insn.Offset) != SlotKind.Ref)
                                throw new NotSupportedException($"emit-patch: {site} — ldfld receiver is not a reference at IL_{insn.Offset:X4}");
                            Push(kind);
                            break;
                        case ILOpCode.Stfld:
                            if (Pop(insn.Offset) != kind)
                                throw new NotSupportedException($"emit-patch: {site} — stfld operand kind mismatch at IL_{insn.Offset:X4}");
                            if (Pop(insn.Offset) != SlotKind.Ref)
                                throw new NotSupportedException($"emit-patch: {site} — stfld receiver is not a reference at IL_{insn.Offset:X4}");
                            break;
                        case ILOpCode.Ldsfld:
                            Push(kind);
                            break;
                        default: // stsfld
                            if (Pop(insn.Offset) != kind)
                                throw new NotSupportedException($"emit-patch: {site} — stsfld operand kind mismatch at IL_{insn.Offset:X4}");
                            break;
                    }
                    if (regCode)
                    {
                        switch (insn.OpCode)
                        {
                            case ILOpCode.Ldfld:
                                Reg(RegOp.R_LDFLD, r0: RegOf(di - 1), r1: RegOf(di - 1), a: ImportRef(importIdx));
                                break;
                            case ILOpCode.Stfld:
                                Reg(RegOp.R_STFLD, r0: RegOf(di - 2), r1: RegOf(di - 1), a: ImportRef(importIdx));
                                break;
                            case ILOpCode.Ldsfld:
                                Reg(RegOp.R_LDSFLD, r0: RegOf(di), a: ImportRef(importIdx));
                                break;
                            default: // stsfld
                                Reg(RegOp.R_STSFLD, r0: RegOf(di - 1), a: ImportRef(importIdx));
                                break;
                        }
                    }
                    else
                    {
                        w.AddInsn((uint)insn.OpCode, 0, ImportRef(importIdx), 0);
                    }
                    break;
                }

                case ILOpCode.Isinst:
                case ILOpCode.Castclass:
                {
                    // Type tests bake a pre-resolved type EntityRef: a
                    // PatchEntity TypeTable index for a patch class, a type
                    // import for an AOT base-image class. Both feed the shared
                    // runtime base-chain walk at execution (a patch instance's
                    // header carries its loader-constructed type-info, so
                    // testing an AOT-typed value against a patch type — and
                    // the reverse — both resolve). Value-type targets (the
                    // box/unbox surface) stay outside the fence.
                    var typeHandle = SRME.EntityHandle(insn.Token);
                    uint testRef;
                    if (typeHandle.Kind == HandleKind.TypeDefinition
                        && module.ClassMap.TryGetValue((TypeDefinitionHandle)typeHandle, out var testCls))
                    {
                        if (testCls.IsValueType || !typeIndex.TryGetValue(testCls, out uint patchTypeIdx))
                            throw new NotSupportedException($"emit-patch: {site} — isinst/castclass target {testCls.FullName} is not a baked patch class at IL_{insn.Offset:X4}");
                        testRef = patchTypeIdx; // PatchEntity tag (0) + TypeTable index
                    }
                    else if (typeHandle.Kind == HandleKind.TypeReference)
                    {
                        var tr = module.Reader.GetTypeReference((TypeReferenceHandle)typeHandle);
                        string ns = module.Reader.GetString(tr.Namespace);
                        string typeName = string.IsNullOrEmpty(ns)
                            ? module.Reader.GetString(tr.Name)
                            : ns + "." + module.Reader.GetString(tr.Name);
                        testRef = ImportRef(TypeImportOf(w, typeImports, typeName));
                    }
                    else if (typeHandle.Kind == HandleKind.TypeSpecification
                        && DecodeTypeToken(compilation, m, insn.Token, site) is { Kind: TypeKind.SZArray } arrTest)
                    {
                        // An SZArray target: the synthetic array type import
                        // resolves to a live array type-info at load, and the
                        // runtime's array-covariance arm decides the test by
                        // element assignability (string[] is object[], a
                        // patch-derived-element array is its AOT-base-element
                        // array, value elements exact).
                        testRef = ImportRef(ArrayImportOf(w, typeImports, typeIndex, arrTest.Element!,
                            site, "type-test element"));
                    }
                    else
                    {
                        throw new NotSupportedException($"emit-patch: {site} — isinst/castclass target must be a class or SZ array type at IL_{insn.Offset:X4}");
                    }
                    if (Pop(insn.Offset) != SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — type-test operand is not a reference at IL_{insn.Offset:X4}");
                    if (regCode) // dst overwrites the source's temp
                        Reg(insn.OpCode == ILOpCode.Isinst ? RegOp.R_ISINST : RegOp.R_CASTCLASS,
                            r0: RegOf(stack.Count), r1: RegOf(stack.Count), a: testRef);
                    else
                        w.AddInsn((uint)insn.OpCode, 0, testRef, 0);
                    Push(SlotKind.Ref);
                    break;
                }

                case ILOpCode.Newarr:
                {
                    // A single-dimension zero-based array allocation: `a` = the
                    // synthetic SZArray type import (the loader's live array
                    // type-info — the allocation's header stamp), `b` = the
                    // element storage kind selecting the array representation.
                    var elem = DecodeTypeToken(compilation, m, insn.Token, site);
                    uint arrRef = ImportRef(ArrayImportOf(w, typeImports, typeIndex, elem, site, "newarr element"));
                    uint elemKind = ElemKindOf(compilation, elem, site, insn.Offset);
                    if (Pop(insn.Offset) != SlotKind.I32)
                        throw new NotSupportedException($"emit-patch: {site} — newarr length is not an int32 at IL_{insn.Offset:X4}");
                    if (regCode) // r1 = length; the array overwrites its temp
                        Reg(RegOp.R_NEWARR, r0: RegOf(stack.Count), r1: RegOf(stack.Count), a: arrRef, b: elemKind);
                    else
                        w.AddInsn((uint)ILOpCode.Newarr, 0, arrRef, elemKind);
                    Push(SlotKind.Ref);
                    break;
                }

                case ILOpCode.Ldlen:
                    // ECMA pushes a native uint; compilers immediately conv.i4
                    // it and array lengths are int32 on this runtime, so the
                    // result rides an i32 slot.
                    if (Pop(insn.Offset) != SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — ldlen operand is not an array reference at IL_{insn.Offset:X4}");
                    if (regCode)
                        Reg(RegOp.R_LDLEN, r0: RegOf(stack.Count), r1: RegOf(stack.Count));
                    else
                        w.AddInsn((uint)ILOpCode.Ldlen, 0, 0, 0);
                    Push(SlotKind.I32);
                    break;

                case ILOpCode.Ldelem:
                case ILOpCode.Ldelem_i1:
                case ILOpCode.Ldelem_u1:
                case ILOpCode.Ldelem_i2:
                case ILOpCode.Ldelem_u2:
                case ILOpCode.Ldelem_i4:
                case ILOpCode.Ldelem_u4:
                case ILOpCode.Ldelem_i8:
                case ILOpCode.Ldelem_r4:
                case ILOpCode.Ldelem_r8:
                case ILOpCode.Ldelem_ref:
                {
                    // Every load form normalizes to the canonical ldelem with
                    // `a` = the element storage kind (the typed short forms
                    // carry it in the opcode, the generic form in its type
                    // token); the interpreter reads the element at that width
                    // and converts to the slot invariants.
                    uint kind = insn.OpCode == ILOpCode.Ldelem
                        ? ElemKindOf(compilation, DecodeTypeToken(compilation, m, insn.Token, site), site, insn.Offset)
                        : TypedElemKind(insn.OpCode, site, insn.Offset);
                    if (Pop(insn.Offset) != SlotKind.I32)
                        throw new NotSupportedException($"emit-patch: {site} — ldelem index is not an int32 at IL_{insn.Offset:X4}");
                    if (Pop(insn.Offset) != SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — ldelem operand is not an array reference at IL_{insn.Offset:X4}");
                    if (regCode) // r1 = array, r2 (a's low byte) = index; dst overwrites the array's temp
                        Reg(RegOp.R_LDELEM, r0: RegOf(stack.Count), r1: RegOf(stack.Count),
                            a: RegOf(stack.Count + 1), b: kind);
                    else
                        w.AddInsn((uint)ILOpCode.Ldelem, 0, kind, 0);
                    Push(ElemSlotKind(kind));
                    break;
                }

                case ILOpCode.Stelem:
                case ILOpCode.Stelem_i1:
                case ILOpCode.Stelem_i2:
                case ILOpCode.Stelem_i4:
                case ILOpCode.Stelem_i8:
                case ILOpCode.Stelem_r4:
                case ILOpCode.Stelem_r8:
                case ILOpCode.Stelem_ref:
                {
                    // Same normalization as ldelem. A reference-element store
                    // skips the covariance check, matching the AOT lane's
                    // stelem helpers (docs/BPI-FORMAT.md carve-outs).
                    uint kind = insn.OpCode == ILOpCode.Stelem
                        ? ElemKindOf(compilation, DecodeTypeToken(compilation, m, insn.Token, site), site, insn.Offset)
                        : TypedElemKind(insn.OpCode, site, insn.Offset);
                    if (Pop(insn.Offset) != ElemSlotKind(kind))
                        throw new NotSupportedException($"emit-patch: {site} — stelem value kind mismatch at IL_{insn.Offset:X4}");
                    if (Pop(insn.Offset) != SlotKind.I32)
                        throw new NotSupportedException($"emit-patch: {site} — stelem index is not an int32 at IL_{insn.Offset:X4}");
                    if (Pop(insn.Offset) != SlotKind.Ref)
                        throw new NotSupportedException($"emit-patch: {site} — stelem operand is not an array reference at IL_{insn.Offset:X4}");
                    if (regCode) // r0 = array, r1 = index, r2 (a's low byte) = value
                        Reg(RegOp.R_STELEM, r0: RegOf(stack.Count), r1: RegOf(stack.Count + 1),
                            a: RegOf(stack.Count + 2), b: kind);
                    else
                        w.AddInsn((uint)ILOpCode.Stelem, 0, kind, 0);
                    break;
                }

                case ILOpCode.Ldftn:
                {
                    // A method reference for a following delegate newobj: a patch
                    // method (MethodDefinition) bakes as a PatchEntity method index
                    // (the interpreter captures the receiver at newobj and routes
                    // Invoke through the delegate's thunk); a base-image instance
                    // method bakes as a method import (its AOT pointer becomes the
                    // delegate's f_method directly — the natural AOT delegate).
                    var ftnHandle = SRME.EntityHandle(insn.Token);
                    if (ftnHandle.Kind == HandleKind.MethodDefinition)
                    {
                        if (!module.MethodMap.TryGetValue((MethodDefinitionHandle)ftnHandle, out var ftnTarget)
                            || !methodIndex.TryGetValue(ftnTarget, out uint ftnIdx))
                            throw new NotSupportedException($"emit-patch: {site} — ldftn target is not a baked patch method at IL_{insn.Offset:X4}");
                        if (ftnTarget.Name == ".cctor")
                            throw new NotSupportedException($"emit-patch: {site} — ldftn of a static constructor is not supported");
                        if (regCode)
                            Reg(RegOp.R_LDFTN, r0: RegOf(stack.Count), a: ftnIdx); // PatchEntity tag (0) + methodIdx
                        else
                            w.AddInsn((uint)ILOpCode.Ldftn, 0, ftnIdx, 0); // PatchEntity tag (0) + methodIdx
                    }
                    else
                    {
                        var ftnImport = ResolveCallImport(compilation, w, typeImports, methodImports, m, insn.Token, isNewobj: false);
                        if (regCode)
                            Reg(RegOp.R_LDFTN, r0: RegOf(stack.Count), a: ImportRef(ftnImport.ImportIdx));
                        else
                            w.AddInsn((uint)ILOpCode.Ldftn, 0, ImportRef(ftnImport.ImportIdx), 0);
                    }
                    Push(SlotKind.Ref);
                    break;
                }

                case ILOpCode.Ldvirtftn:
                    throw new NotSupportedException($"emit-patch: {site} — ldvirtftn (a delegate over a virtual method) is not supported yet");

                case ILOpCode.Ldelema:
                    throw new NotSupportedException($"emit-patch: {site} — ldelema (taking an array element's address) is not supported yet");

                default:
                    throw new NotSupportedException(
                        $"emit-patch: {site} — opcode {insn.OpCode} is not supported yet");
            }
        }

        uint ehStart = w.EHCount;
        if (regCode)
        {
            // End sentinel: an exclusive region end at the end of the body (or
            // a target following trailing zero-record IL) maps to one past the
            // last register record.
            ilToReg[insns.Count] = regBuf.Count;
            if (slotCount > RegFormat.SlotLimit || simMax > RegFormat.TempLimit)
                throw new NotSupportedException($"emit-patch: {site} — frame shape exceeds the register format's {RegFormat.SlotLimit}-slot/{RegFormat.TempLimit}-temp limits (args+locals {slotCount}, eval depth {simMax})");
            // Flush: rewrite branch/leave targets from IL instruction indices
            // to register-stream indices, then append the records and the
            // method's EHTable run with its boundaries remapped the same way
            // (exclusive ends stay correct under the mapping).
            foreach (var ri in regBuf)
            {
                uint a = ri.A;
                if (ri.TargetFixup)
                {
                    if (!isLeader[(int)a])
                        throw new InvalidOperationException($"emit-patch: {site} — internal: branch target IL index {a} was not marked as a leader");
                    a = (uint)ilToReg[(int)a];
                }
                w.AddInsn((uint)ri.Op, (uint)(ri.R0 | (ri.R1 << 8)), a, ri.B);
            }
            foreach (var r in ehRegions)
                w.AddEH(r.Kind, (uint)ilToReg[(int)r.TryStart], (uint)ilToReg[(int)r.TryEnd],
                    (uint)ilToReg[(int)r.HandlerStart], (uint)ilToReg[(int)r.HandlerEnd],
                    0xFFFFFFFFu, r.CatchTypeRef);
            return ((uint)regBuf.Count, (uint)simMax, ehStart, (uint)ehRegions.Count);
        }
        foreach (var r in ehRegions)
            w.AddEH(r.Kind, r.TryStart, r.TryEnd, r.HandlerStart, r.HandlerEnd, 0xFFFFFFFFu, r.CatchTypeRef);
        return ((uint)insns.Count, (uint)body.MaxStack, ehStart, (uint)ehRegions.Count);
    }

    /// <summary>The interpreter's per-call boxed-argument buffer size; the
    /// converter rejects wider external calls up front.</summary>
    private const int MaxImportArgs = 8;

    /// <summary>Import signatures reference base-image types only — never a
    /// patch table — so their SlotRefOf calls get an empty patch-type index
    /// and a patch type surfacing there hits the standard fence message.</summary>
    private static readonly Dictionary<ClassInfo, uint> NoPatchTypes = new();

    /// <summary>Reads a MemberReference's declaring-type name, the member name,
    /// and the generic context to decode the member's signature under. A plain
    /// TypeReference parent is an AOT base-image type (empty context). A
    /// TypeSpecification parent is a closed generic instantiation of a
    /// base-image type: it resolves to the instantiation's mangled registry name
    /// (or hits the missing-AOT-instantiation boundary), and its type arguments
    /// form the context so the member signature's <c>!n</c> parameters
    /// substitute to concrete types (e.g. <c>List&lt;int&gt;.Add(!0)</c> →
    /// <c>Add(Int32)</c>).</summary>
    private static (string TypeName, string MemberName, MemberReference Mr, GenericContext DeclContext) DecodeMemberRef(
        Compilation compilation, MethodInfo m, EntityHandle handle, string site, string what)
    {
        var reader = m.DeclaringClass.Module.Reader;
        // A generic-method call/ldftn carries a MethodSpecification: the closed
        // method type arguments plus the underlying member (a MemberReference into
        // a base-image type). Decode the args under the caller's context and unwrap
        // to that member; the returned GenericContext carries them so the signature
        // decode below substitutes the method's !!n parameters to concrete types
        // (Echo<int>: !!0 -> Int32), making the baked sigShape the instantiation's —
        // the loader's discriminator across a generic method's several same-named
        // instantiations. A generic method defined in the patch itself unwraps to a
        // MethodDefinition and is rejected by the token-kind check below (a later
        // slice, like generic patch types).
        TypeDesc[] methodArgs = Array.Empty<TypeDesc>();
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            var spec = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
            methodArgs = spec.DecodeSignature(compilation.SigProvider, m.Context).ToArray();
            handle = spec.Method;
        }
        if (handle.Kind != HandleKind.MemberReference)
            throw new NotSupportedException($"emit-patch: {site} — unsupported {what} token kind {handle.Kind}");
        var mr = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (mr.Parent.Kind == HandleKind.TypeReference)
        {
            var tr = reader.GetTypeReference((TypeReferenceHandle)mr.Parent);
            string ns = reader.GetString(tr.Namespace);
            string typeName = string.IsNullOrEmpty(ns)
                ? reader.GetString(tr.Name)
                : ns + "." + reader.GetString(tr.Name);
            return (typeName, reader.GetString(mr.Name), mr, new GenericContext(Array.Empty<TypeDesc>(), methodArgs));
        }
        if (mr.Parent.Kind == HandleKind.TypeSpecification)
        {
            var declType = reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent)
                .DecodeSignature(compilation.SigProvider, m.Context);
            if (declType.Kind != TypeKind.ExternalGeneric)
                throw new NotSupportedException($"emit-patch: {site} — unsupported {what} declaring type {declType}");
            var ctx = new GenericContext(declType.GenericArgs!, methodArgs);
            return (declType.ExternalName!, reader.GetString(mr.Name), mr, ctx);
        }
        throw new NotSupportedException($"emit-patch: {site} — unsupported {what} parent {mr.Parent.Kind}");
    }

    /// <summary>Whether a call token is `System.Object::.ctor()` — the chain
    /// target of a patch constructor rooted directly at object. The runtime
    /// object layout has no constructor work to do, so the caller folds the
    /// call to dropping the receiver.</summary>
    private static bool IsObjectCtorCall(MetadataReader reader, EntityHandle handle)
    {
        if (handle.Kind != HandleKind.MemberReference)
            return false;
        var mr = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (mr.Parent.Kind != HandleKind.TypeReference || reader.GetString(mr.Name) != ".ctor")
            return false;
        var tr = reader.GetTypeReference((TypeReferenceHandle)mr.Parent);
        return reader.GetString(tr.Namespace) == "System" && reader.GetString(tr.Name) == "Object";
    }

    /// <summary>Whether a member reference is a delegate constructor: the
    /// <c>.ctor(object, native int)</c> shape every delegate type declares. It is
    /// the reliable delegate discriminator on the patch surface — a base-image
    /// delegate type is not loaded here (no <see cref="ClassInfo.IsDelegate"/> to
    /// read), so the ctor signature stands in.</summary>
    private static bool IsDelegateCtorSig(MetadataReader reader, MemberReference mr, MethodSignature<TypeDesc> sig)
    {
        if (reader.GetString(mr.Name) != ".ctor")
            return false;
        if (sig.ParameterTypes.Length != 2 || !sig.ReturnType.IsVoid || !sig.Header.IsInstance)
            return false;
        var p1 = sig.ParameterTypes[1];
        bool secondIsIntPtr =
            (p1.Kind == TypeKind.Primitive && p1.Primitive == PrimitiveTypeCode.IntPtr)
            || (p1.Kind == TypeKind.External && p1.ExternalName == "System.IntPtr");
        return sig.ParameterTypes[0].IsObject && secondIsIntPtr;
    }

    /// <summary>Resolves a call/newobj token to a (deduped) method import.
    /// External call fence: a cross-assembly static or instance method (or
    /// `.ctor` for newobj) over scalar/String/AOT-reference-type arguments and
    /// returns. The import's [return, params...] EntityRef run goes into the
    /// LocalSigTable (its start index in aux1) so the runtime binder derives
    /// the box/unbox marshalling without parsing signatures.</summary>
    private static ImportCall ResolveCallImport(Compilation compilation, BpiWriter w,
        Dictionary<string, uint> typeImports, Dictionary<string, uint> methodImports,
        MethodInfo m, int token, bool isNewobj)
    {
        string site = $"{m.DeclaringClass.FullName}.{m.Name}";
        var (typeName, methodName, mr, declCtx) = DecodeMemberRef(
            compilation, m, SRME.EntityHandle(token), site, isNewobj ? "newobj" : "call-target");

        // Decode the member signature under the declaring type's generic context
        // so a call on a closed generic instantiation substitutes the declaring
        // type's !n parameters to concrete types (List<int>.Add(!0) → Add(Int32)).
        var sig = mr.DecodeMethodSignature(compilation.SigProvider, declCtx);
        bool isInstance = sig.Header.IsInstance;
        // Multicast: `+=`/`-=` on a delegate lower to Delegate.Combine/Remove.
        // Only single-target delegates are supported (the combined invocation
        // list is not baked), so reject the combinators with a clear message.
        if (typeName is "System.Delegate" or "System.MulticastDelegate"
            && methodName is "Combine" or "Remove")
            throw new NotSupportedException($"emit-patch: {site} — combining delegates (Delegate.{methodName} / multicast +=) is not supported yet (single-target delegates only)");
        // A generic-method call: DecodeMemberRef captured the closed method type
        // arguments into declCtx.MethodArgs and the signature above was decoded
        // under them, so sig.ParameterTypes/ReturnType are the instantiation's
        // concrete types and the baked sigShape/param EntityRefs are the
        // instantiation's — the loader binds the import to that one instantiation
        // among the several the base emits under this name. The instantiation must
        // be AOT-present: the --hotupdate-base build must have emitted this closed
        // method (a hotupdate-refs.txt method root force-emits one the base program
        // never calls), else the loader's method-import bind fails as unresolved —
        // the missing-AOT-instantiation boundary for methods. An open generic
        // method reached without a MethodSpecification stays a rejection.
        if (sig.GenericParameterCount != declCtx.MethodArgs.Length)
            throw new NotSupportedException($"emit-patch: {site} — generic external call {typeName}.{methodName} is not a closed instantiation");
        if (isNewobj)
        {
            if (methodName != ".ctor" || !isInstance || !sig.ReturnType.IsVoid)
                throw new NotSupportedException($"emit-patch: {site} — newobj target {typeName}.{methodName} is not a plain instance constructor");
        }
        else if (methodName == ".cctor" || (methodName == ".ctor" && !isInstance))
        {
            // A direct instance `.ctor` call is the base-ctor chain of an
            // inheriting patch constructor and binds like any instance call.
            throw new NotSupportedException($"emit-patch: {site} — direct constructor call {typeName}.{methodName} is not supported");
        }
        if (sig.ParameterTypes.Length > MaxImportArgs)
            throw new NotSupportedException($"emit-patch: {site} — external call {typeName}.{methodName} takes more than {MaxImportArgs} arguments (not supported yet)");

        var argKinds = new SlotKind[sig.ParameterTypes.Length];
        var paramRefs = new uint[sig.ParameterTypes.Length];
        for (int i = 0; i < sig.ParameterTypes.Length; i++)
        {
            paramRefs[i] = SlotRefOf(w, typeImports, NoPatchTypes, sig.ParameterTypes[i], site,
                $"external call {typeName}.{methodName} argument");
            argKinds[i] = KindOfType(compilation, sig.ParameterTypes[i])!.Value;
        }
        SlotKind? retKind = null;
        uint retRef = 0xFFFFFFFFu;
        if (!sig.ReturnType.IsVoid)
        {
            retRef = SlotRefOf(w, typeImports, NoPatchTypes, sig.ReturnType, site,
                $"external call {typeName}.{methodName} return");
            retKind = KindOfType(compilation, sig.ReturnType)!.Value;
        }

        uint typeImport = TypeImportOf(w, typeImports, typeName);
        string shape = SigShape(sig);
        string key = typeName + "::" + methodName + (isInstance ? "#i" : "#s") + shape;
        if (!methodImports.TryGetValue(key, out uint methodImport))
        {
            // The signature run: return EntityRef (0xFFFFFFFF for void), then
            // one EntityRef per parameter.
            uint sigOff = w.LocalSigCount;
            w.AddLocalSig(retRef);
            foreach (var r in paramRefs)
                w.AddLocalSig(r);
            uint aux0 = (uint)sig.ParameterTypes.Length | (isInstance ? 0x10000u : 0u);
            methodImport = w.AddImport(2, w.InternName(methodName), typeImport,
                w.InternName(shape), aux0, sigOff);
            methodImports[key] = methodImport;
        }
        return new ImportCall
        {
            ImportIdx = methodImport,
            IsInstance = isInstance,
            ArgKinds = argKinds,
            RetKind = retKind,
        };
    }

    /// <summary>Resolves a field token to a (deduped) field import. Field
    /// fence: a cross-assembly field of scalar/String/AOT-reference type
    /// (fields are unique by name — no overload discrimination). aux1 carries
    /// the field type's EntityRef so the runtime binder derives the box/unbox
    /// marshalling; the actual storage is reached through the bound
    /// Dn2CppFieldInfo accessors. Returns the import index and the field's
    /// eval-stack kind for the caller's stack simulation.</summary>
    private static (uint ImportIdx, SlotKind Kind) ResolveFieldImport(Compilation compilation, BpiWriter w,
        Dictionary<string, uint> typeImports, Dictionary<string, uint> fieldImports,
        MethodInfo m, int token)
    {
        string site = $"{m.DeclaringClass.FullName}.{m.Name}";
        var (typeName, fieldName, mr, declCtx) = DecodeMemberRef(
            compilation, m, SRME.EntityHandle(token), site, "field");

        var fieldType = mr.DecodeFieldSignature(compilation.SigProvider, declCtx);
        uint typeRef = SlotRefOf(w, typeImports, NoPatchTypes, fieldType, site, $"field {typeName}.{fieldName}");
        var kind = KindOfType(compilation, fieldType)!.Value;

        uint typeImport = TypeImportOf(w, typeImports, typeName);
        string key = typeName + "::" + fieldName;
        if (!fieldImports.TryGetValue(key, out uint fieldImport))
        {
            fieldImport = w.AddImport(3, w.InternName(fieldName), typeImport,
                w.InternName(fieldType.ToString()), 0, typeRef);
            fieldImports[key] = fieldImport;
        }
        return (fieldImport, kind);
    }

    /// <summary>The v1 sigShape string: SigKey without the leading name —
    /// <c>(paramTypes):retType</c> in TypeDesc rendering. Delegates to the shared
    /// <see cref="AbiContract.SigShape"/> renderer so a converter-baked import
    /// shape and a base-emitted method-row shape agree byte-for-byte (the loader
    /// matches them by string equality).</summary>
    private static string SigShape(MethodSignature<TypeDesc> sig) => AbiContract.SigShape(sig);

    /// <summary>Pulls the vtable slot layouts out of a base-abi.json body:
    /// canonical type name → the SigKey of each vtable slot, in slot order
    /// (schema in BPI-FORMAT.md; hand-parsed — the manifest is
    /// machine-written). A manifest without a "vtables" object (an older base
    /// build) yields an empty map, so any override then fails with the
    /// no-vtable message.</summary>
    private static Dictionary<string, List<string>> ReadManifestVtables(string json) =>
        ReadManifestSlotMap(json, "\"vtables\"");

    /// <summary>Pulls the interface method-slot layouts out of a base-abi.json
    /// body: canonical interface name → the SigKey of each method slot, in slot
    /// (declaration) order. A manifest without an "interfaces" object (an older
    /// base build) yields an empty map, so any patch implementing an interface
    /// then fails with the not-in-manifest message.</summary>
    private static Dictionary<string, List<string>> ReadManifestInterfaces(string json) =>
        ReadManifestSlotMap(json, "\"interfaces\"");

    /// <summary>Pulls the closed generic instantiation map out of a
    /// base-abi.json body: the base⇄converter lookup key
    /// (<see cref="AbiContract.InstantiationKey"/>) → the mangled registry name
    /// the runtime type registry is keyed by. A manifest without an
    /// "instantiations" object (an older base build) yields an empty map, so any
    /// patch naming a base-image generic type then hits the
    /// missing-AOT-instantiation boundary.</summary>
    /// <summary>Resolves a closed generic instantiation of a base-image type to
    /// its mangled registry name against the manifest's instantiations map, or
    /// null when the base image never emitted it (the missing-AOT-instantiation
    /// boundary — the SignatureProvider turns null into a clear rejection). The
    /// open-definition name arrives with its arity backtick (e.g.
    /// <c>System.Collections.Generic.List`1</c>); the lookup key strips it and
    /// appends the arguments' <see cref="TypeDesc.ToString"/> rendering, the same
    /// key the base build wrote (<see cref="AbiContract.InstantiationKey"/>).</summary>
    private static TypeDesc? ResolveExternalGeneric(
        Dictionary<string, string> instantiations, string openDefName, TypeDesc[] args)
    {
        int tick = openDefName.IndexOf('`');
        string stripped = tick >= 0 ? openDefName.Substring(0, tick) : openDefName;
        string key = AbiContract.InstantiationKey(stripped, args);
        return instantiations.TryGetValue(key, out string? mangled)
            ? TypeDesc.MakeExternalGeneric(mangled, args)
            : null;
    }

    private static Dictionary<string, string> ReadManifestInstantiations(string json) =>
        ReadManifestStringMap(json, "\"instantiations\"");

    /// <summary>Pulls the base build's conditional default-reference verdicts out
    /// of a base-abi.json body: shim simple name → the
    /// <see cref="Compilation.DefaultRefOutcome"/> name the base recorded. A
    /// manifest without a "defaultRefs" object (a base built before the key
    /// existed) yields an empty map, which
    /// <see cref="CheckDefaultRefSymmetry"/> reads as "nothing known" and lets
    /// through — an older sidecar must keep baking.</summary>
    private static Dictionary<string, string> ReadManifestDefaultRefs(string json) =>
        ReadManifestStringMap(json, "\"defaultRefs\"");

    /// <summary>Parses a base-abi.json flat <c>"&lt;key&gt;": { "k": "v", ... }</c>
    /// object into a string map (hand-parsed — the manifest is machine-written).
    /// Shared by the instantiations map and the default-reference record.</summary>
    private static Dictionary<string, string> ReadManifestStringMap(string json, string key)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        int at = json.IndexOf(key, StringComparison.Ordinal);
        if (at < 0)
            return result;
        int i = json.IndexOf('{', at + key.Length);
        if (i < 0)
            throw new NotSupportedException($"emit-patch: malformed {key} object in base-abi.json");
        i++;
        while (true)
        {
            while (i < json.Length && (json[i] is ' ' or '\t' or '\r' or '\n' or ','))
                i++;
            if (i >= json.Length)
                throw new NotSupportedException($"emit-patch: malformed {key} object in base-abi.json");
            if (json[i] == '}')
                break;
            string k = ParseJsonString(json, ref i);
            while (i < json.Length && (json[i] is ' ' or '\t' or '\r' or '\n' or ':'))
                i++;
            if (i >= json.Length || json[i] != '"')
                throw new NotSupportedException($"emit-patch: malformed {key} object in base-abi.json");
            result[k] = ParseJsonString(json, ref i);
        }
        return result;
    }

    /// <summary>Rejects a patch whose <c>-r</c> set carries a shipped shim the BASE
    /// image did not: the patch's model would resolve names against classes that
    /// exist nowhere in the base, and the baked import would bind against nothing at
    /// load time — a failure with its cause long gone. Refuse at bake instead,
    /// naming the shim, the base's own verdict, and the remedy. The opposite
    /// asymmetry is benign and deliberately allowed: a patch binds to the base by
    /// name and never re-lowers a base body, so a shim the base injected is
    /// invisible from here.
    ///
    /// <para>An outcome of <c>Injected</c> or <c>AlreadyLoaded</c> means the base
    /// image carried the shim; everything else — <c>TriggerAbsent</c>,
    /// <c>Suppressed</c>, <c>NotFound</c> — means it did not. An empty record (a
    /// sidecar written before this key existed) asserts nothing.</para>
    ///
    /// <para>Runs BEFORE <c>Compilation.Create</c> and matches on the reference FILE
    /// name, because the claim is about the <c>-r</c> set the caller passed, not
    /// about a built model. Loading the shim first produces the unreadable failure
    /// this replaces: the shim drags in generics the base never emitted, so Create
    /// dies on an AOT-instantiation boundary naming nothing about the shim.</para></summary>
    private static void CheckDefaultRefSymmetry(
        IReadOnlyList<string> paths, Dictionary<string, string> baseDefaultRefs)
    {
        if (baseDefaultRefs.Count == 0)
            return;
        foreach (string path in paths)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            string? outcome = null;
            foreach (var entry in baseDefaultRefs)
            {
                if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
                {
                    outcome = entry.Value;
                    break;
                }
            }
            if (outcome is null
                || outcome is nameof(Compilation.DefaultRefOutcome.Injected)
                           or nameof(Compilation.DefaultRefOutcome.AlreadyLoaded))
                continue;
            throw new NotSupportedException(
                $"emit-patch: the patch load set carries the shim '{name}', which the "
                + $"base image does not (the base build recorded '{outcome}'). A patch's "
                + "model must be a copy of the base build's -r set: names resolved "
                + "against a shim the base never loaded bake into imports that bind "
                + "against nothing at load time. Drop the -r, or rebuild the base so it "
                + "carries the shim too.");
        }
    }

    /// <summary>Parses a base-abi.json <c>"&lt;key&gt;": { "name": [ "sig", ... ] }</c>
    /// object into a name → slot-SigKey-list map (hand-parsed — the manifest is
    /// machine-written). Shared by the vtable and interface slot maps.</summary>
    private static Dictionary<string, List<string>> ReadManifestSlotMap(string json, string key)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        int at = json.IndexOf(key, StringComparison.Ordinal);
        if (at < 0)
            return result;
        int i = json.IndexOf('{', at + key.Length);
        if (i < 0)
            throw new NotSupportedException($"emit-patch: malformed {key} object in base-abi.json");
        i++;
        while (true)
        {
            while (i < json.Length && (json[i] is ' ' or '\t' or '\r' or '\n' or ','))
                i++;
            if (i >= json.Length)
                throw new NotSupportedException($"emit-patch: malformed {key} object in base-abi.json");
            if (json[i] == '}')
                break;
            string name = ParseJsonString(json, ref i);
            while (i < json.Length && (json[i] is ' ' or '\t' or '\r' or '\n' or ':'))
                i++;
            if (i >= json.Length || json[i] != '[')
                throw new NotSupportedException($"emit-patch: malformed {key} object in base-abi.json");
            i++;
            var slots = new List<string>();
            while (true)
            {
                while (i < json.Length && (json[i] is ' ' or '\t' or '\r' or '\n' or ','))
                    i++;
                if (i >= json.Length)
                    throw new NotSupportedException($"emit-patch: malformed {key} object in base-abi.json");
                if (json[i] == ']')
                {
                    i++;
                    break;
                }
                slots.Add(ParseJsonString(json, ref i));
            }
            result[name] = slots;
        }
        return result;
    }

    /// <summary>Parses the JSON string starting at <paramref name="i"/>
    /// (which must point at the opening quote), leaving <paramref name="i"/>
    /// just past the closing quote. Handles the two escapes the manifest
    /// writer emits (\\ and \").</summary>
    private static string ParseJsonString(string json, ref int i)
    {
        if (i >= json.Length || json[i] != '"')
            throw new NotSupportedException("emit-patch: malformed string in base-abi.json");
        i++;
        var sb = new System.Text.StringBuilder();
        while (i < json.Length && json[i] != '"')
        {
            char ch = json[i];
            if (ch == '\\')
            {
                i++;
                if (i >= json.Length)
                    break;
                ch = json[i];
            }
            sb.Append(ch);
            i++;
        }
        if (i >= json.Length)
            throw new NotSupportedException("emit-patch: malformed string in base-abi.json");
        i++; // past the closing quote
        return sb.ToString();
    }

    /// <summary>Pulls the 16-hex-digit hash out of a base-abi.json body (schema
    /// in BPI-FORMAT.md; hand-parsed — the manifest is machine-written).</summary>
    private static ulong ReadManifestHash(string json)
    {
        const string key = "\"hash\": \"0x";
        int at = json.IndexOf(key, StringComparison.Ordinal);
        if (at < 0 || at + key.Length + 16 > json.Length)
            throw new NotSupportedException("emit-patch: base-abi.json carries no \"hash\": \"0x…\" entry");
        ulong value = 0;
        for (int i = 0; i < 16; i++)
        {
            char c = json[at + key.Length + i];
            int nibble = c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                >= 'A' and <= 'F' => c - 'A' + 10,
                _ => throw new NotSupportedException("emit-patch: malformed hash in base-abi.json"),
            };
            value = (value << 4) | (uint)nibble;
        }
        return value;
    }
}
