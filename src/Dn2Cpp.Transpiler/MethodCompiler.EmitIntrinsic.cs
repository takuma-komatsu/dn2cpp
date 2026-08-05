using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <param name="adoptedFrom">The real name of a third-party async task-family type
    /// adopted into the intrinsic Task family, when <paramref name="declType"/> is the BCL
    /// key standing in for it. Only affects the miss diagnostic, which would otherwise name
    /// the BCL stand-in rather than the type the program actually calls.</param>
    private void EmitIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig,
                               string? adoptedFrom = null)
    {
        if (TryEmitIntrinsic(declType, name, sig))
            return;
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: call to "
            + (adoptedFrom is null
                ? $"{declType}::{name}{SigArgs(sig)} has no intrinsic mapping yet"
                : $"{adoptedFrom}::{name}{SigArgs(sig)} has no intrinsic mapping yet — {adoptedFrom} is a custom "
                  + $"async task type, which dn2cpp models by adopting it into its intrinsic Task "
                  + $"family (as {declType}) rather than transpiling the library's own scheduler and "
                  + $"object-pool machinery. Only the members the C# compiler and the await pattern "
                  + $"require are mapped; this one has no BCL counterpart")
            + $" [chain: {Comp.ReachChain(Method)}]");
    }

    /// <summary>The decoded parameter types of the call that missed, for the diagnostic
    /// above. A miss is against an OVERLOAD, never a method — these intercepts are routed
    /// by name and matched on the signature — so a message naming only the method would
    /// read as a claim about a method dn2cpp DOES map.</summary>
    private static string SigArgs(MethodSignature<TypeDesc> sig) =>
        "(" + string.Join(", ", sig.ParameterTypes.Select(p => p.ToString())) + ")";

    /// <summary>The name to report a miss against for an adopted custom async task-family
    /// type — its real CLR name, not the BCL key it is dispatched under — or null for an
    /// ordinary intrinsic type.</summary>
    private string? AdoptedFrom(ClassInfo cls) =>
        Comp.AdoptedAsyncKey(cls) is null ? null : Comp.GenericDefFullName(cls);

    /// <summary>One arm of the intrinsic funnel. Static (the compiler instance arrives
    /// as an argument) so the whole dispatch table is a shared static structure built
    /// once, not a per-MethodCompiler allocation.</summary>
    private delegate bool IntrinsicProbe(MethodCompiler mc, string declType, string name,
        MethodSignature<TypeDesc> sig);

    /// <summary>The intrinsic-mapped value types: a box of one carries a shared runtime
    /// handle (<c>&amp;dn2cpp_&lt;bare&gt;_type</c>) instead of an emitted <c>ti_</c>, and its
    /// payload compares by <c>dn2cpp_&lt;bare&gt;_cmp</c>. The same six
    /// <see cref="IntrinsicValueTypeFn"/> names, in the one place that needs them by their
    /// managed name rather than by TypeDesc (the intrinsic dispatch has only the string).
    /// </summary>
    private static readonly string[] s_boxedIntrinsicValueTypes =
    [
        "System.Decimal", "System.TimeSpan", "System.DateTime",
        "System.DateTimeOffset", "System.DateOnly", "System.TimeOnly",
    ];

    /// <summary>The portable-SIMD facade types. Three probes share these five keys, and
    /// their order among themselves is load-bearing.</summary>
    private static readonly string[] s_vectorFacadeTypes =
    [
        "System.Runtime.Intrinsics.Vector64", "System.Runtime.Intrinsics.Vector128",
        "System.Runtime.Intrinsics.Vector256", "System.Runtime.Intrinsics.Vector512",
        "System.Numerics.Vector",
    ];

    /// <summary>The reflected-element receiver types whose GetCustomAttributes/IsDefined
    /// dispatch in the runtime on the object header (so one path serves them all).</summary>
    private static readonly string[] s_customAttributeReceiverTypes =
    [
        "System.Reflection.MemberInfo", "System.Reflection.ParameterInfo",
        "System.Type", "System.Reflection.MethodInfo", "System.Reflection.MethodBase",
        "System.Reflection.FieldInfo", "System.Reflection.PropertyInfo",
        "System.Reflection.ConstructorInfo",
    ];

    /// <summary>The file-backed memory-map surface (POSIX mmap subset).</summary>
    private static readonly string[] s_memoryMappedTypes =
    [
        "System.IO.MemoryMappedFiles.MemoryMappedFile",
        "System.IO.MemoryMappedFiles.MemoryMappedViewAccessor",
        "System.IO.UnmanagedMemoryAccessor",
        "Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle",
    ];

    /// <summary>The integer widths whose bit-counting / rotate statics map to clang
    /// builtins: <see cref="TryEmitIntegerBitOpIntrinsic"/> answers only for the 32- and
    /// 64-bit types (the sub-words have no builtin of their own width). The probe still
    /// type-gates internally, so this key set and its own switch cannot disagree about
    /// anything but wasted lookups.</summary>
    private static readonly string[] s_integerBitOpTypes =
    [
        "System.Int32", "System.UInt32", "System.Int64", "System.UInt64",
    ];

    /// <summary>The degraded stack-trace model's two types.</summary>
    private static readonly string[] s_stackDiagnosticsTypes =
    [
        "System.Diagnostics.StackTrace", "System.Diagnostics.StackFrame",
    ];

    /// <summary>The declaring-type-keyed arms of the intrinsic funnel, IN CHAIN ORDER.
    /// <see cref="s_intrinsicProbesByType"/> is built by grouping this array on the keys, so
    /// a key served by several probes keeps them in exactly the relative order written here.
    ///
    /// <para><b>The order among the probes of one key decides the answer:</b> the five
    /// vector facades run three probes (the hardware-acceleration constants, the concrete
    /// Vector2/3/4 reinterprets, then the general vector op); the six boxed intrinsic value
    /// types run the shared <c>Equals(object)</c> arm BEFORE their own per-type table, which
    /// is what makes a value compared through a box agree with one compared directly.</para>
    ///
    /// <para>A probe's own name/shape gates stay INSIDE it — the dictionary keys on the
    /// declaring type alone.</para>
    ///
    /// <para>(System.IO.Path / System.IO.File are deliberately absent: they route straight
    /// from the call site to EmitIoIntrinsic on a CoreIntrinsics.LoweredIoMember verdict.
    /// An overload with no lowering has a real body to fall back on — the reach cut asks the
    /// same predicate — so it must NOT be able to reach EmitIntrinsic's throw, and by not
    /// being keyed here it cannot.)</para></summary>
    private static readonly (string[] Keys, IntrinsicProbe Probe)[] s_intrinsicProbeChain =
    [
        // Portable SIMD vector static helpers reached as non-generic MemberRefs (the
        // element-list / broadcast Create overloads, the per-type ConvertTo*/Narrow/
        // Widen* overloads, get_IsHardwareAccelerated) and the closed generic structs'
        // members routed here from the TypeSpec path. Lower to the software-vector
        // runtime; the element is the closed signature's primary vector. An unrecognized
        // member declines to the fallback and then to a loud NotSupportedException.
        (s_vectorFacadeTypes, static (mc, _, n, _) => mc.TryEmitVectorHardwareAccel(n)),
        // The concrete System.Numerics.Vector2/3/4 reinterpret conversions (both the
        // struct<->struct AsVectorN forms and the Vector128<float> pivot) — a byte
        // reinterpret that TryVectorPrimary cannot see, since one side is a real struct.
        (s_vectorFacadeTypes, static (mc, _, n, sig) => mc.TryEmitVector234Conversion(n, sig)),
        (s_vectorFacadeTypes, static (mc, _, n, sig) => mc.TryEmitVectorPrimaryOp(n, sig)),
        // Hot update: Dn2Cpp.Runtime.HotUpdate.Run(string) loads a Baked Patch Image and
        // runs its entry method in the runtime interpreter; Load is the entry-optional
        // variant, LoadDirectory the version-ordered deployment-directory driver.
        (["Dn2Cpp.Runtime.HotUpdate"], static (mc, _, n, sig) => mc.TryEmitHotUpdateIntrinsic(n, sig)),
        // Console.Error stderr-write surface.
        (["Dn2Cpp.Runtime.ConsoleRuntime"], static (mc, _, n, sig) => mc.TryEmitConsoleRuntimeIntrinsic(n, sig)),
        // The Object-virtual Equals(object) of the intrinsic value-type family — one arm
        // for all six. MUST precede their per-type tables below.
        (s_boxedIntrinsicValueTypes, static (mc, dt, n, sig) => mc.TryEmitBoxedIntrinsicValueEquals(dt, n, sig)),
        // System.Decimal is an intrinsic value type (Dn2CppDecimal); its ctors,
        // operators, conversions, ToString and rounding statics lower to runtime helpers.
        (["System.Decimal"], static (mc, _, n, sig) => mc.TryEmitDecimalIntrinsic(n, sig)),
        // System.TimeSpan / System.DateTime are intrinsic value types; their ctors,
        // properties, arithmetic, comparison and default ToString lower to the runtime
        // Dn2CppTimeSpan / Dn2CppDateTime helpers.
        (["System.TimeSpan"], static (mc, _, n, sig) => mc.TryEmitTimeSpanIntrinsic(n, sig)),
        (["System.DateTime"], static (mc, _, n, sig) => mc.TryEmitDateTimeIntrinsic(n, sig)),
        // System.DateTimeOffset: intrinsic value type backed by Dn2CppDateTimeOffset.
        (["System.DateTimeOffset"], static (mc, _, n, sig) => mc.TryEmitDateTimeOffsetIntrinsic(n, sig)),
        // System.DateOnly / System.TimeOnly: backed by Dn2CppDateOnly / Dn2CppTimeOnly.
        (["System.DateOnly"], static (mc, _, n, sig) => mc.TryEmitDateOnlyIntrinsic(n, sig)),
        (["System.TimeOnly"], static (mc, _, n, sig) => mc.TryEmitTimeOnlyIntrinsic(n, sig)),
        // Raw native (GC-unmanaged) heap. NativeMemory.{Alloc,AllocZeroed,Free,Realloc}
        // and Marshal.{AllocHGlobal,FreeHGlobal,Copy} lower to the dn2cpp_native_* C-heap
        // wrappers / memcpy. Their real bodies are InternalCall / P-Invoke into the
        // unmanaged allocator we don't model.
        (["System.Runtime.InteropServices.NativeMemory"],
            static (mc, _, n, sig) => mc.TryEmitNativeMemoryIntrinsic(n, sig)),
        (["System.Runtime.InteropServices.Marshal"],
            static (mc, _, n, sig) => mc.TryEmitMarshalIntrinsic(n, sig)),
        // MemoryMarshal is NOT a fully intrinsic-mapped type (most members transpile from
        // the real CoreLib IL; the generic helpers route through TranslateGenericIntrinsic)
        // — only the non-generic GetArrayDataReference(Array) overload lands here.
        (["System.Runtime.InteropServices.MemoryMarshal"],
            static (mc, _, n, sig) => mc.TryEmitMemoryMarshalIntrinsic(n, sig)),
        // GCHandle: non-moving heap, so pinning is identity. Throws on an unmodeled
        // member (no silent carve-out).
        (["System.Runtime.InteropServices.GCHandle"],
            static (mc, _, n, sig) => mc.TryEmitGCHandleIntrinsic(n, sig)),
        // DependentHandle: the ephemeron primitive behind ConditionalWeakTable. Throws on
        // an unmodeled member (no silent carve-out).
        (["System.Runtime.DependentHandle"],
            static (mc, _, n, sig) => mc.TryEmitDependentHandleIntrinsic(n, sig)),
        // SynchronizationContext's thread-slot statics: Current reads and
        // SetSynchronizationContext writes the per-thread GC-visible runtime slot. Only
        // these two land here — instance members (Post/Send) transpile from real IL.
        (["System.Threading.SynchronizationContext"],
            static (mc, _, n, sig) => mc.TryEmitSyncContextIntrinsic(n, sig)),
        // System.Buffer's byte-granular surface (BlockCopy / ByteLength / Get/SetByte)
        // lowers to the dn2cpp_buffer_* helpers over one byte extent.
        (["System.Buffer"], static (mc, _, n, sig) => mc.TryEmitBufferIntrinsic(n, sig)),
        // The mmap subset: factory / view-accessor / SafeMemoryMappedViewHandle
        // non-generic members (the generic Read/Write<T> forms route through
        // TranslateGenericIntrinsic).
        (s_memoryMappedTypes, static (mc, dt, n, sig) => mc.TryEmitMemoryMappedFileIntrinsic(dt, n, sig)),
        // System.Environment (GetEnvironmentVariable / CurrentDirectory) and
        // System.IO.Directory (Exists / cwd get/set) lower to the dn2cpp_env_* /
        // _directory_* helpers; the real bodies pull in the same I/O cascade.
        (["System.Environment"], static (mc, _, n, sig) => mc.TryEmitEnvironmentIntrinsic(n, sig)),
        (["System.IO.Directory"], static (mc, _, n, sig) => mc.TryEmitDirectoryIntrinsic(n, sig)),
        // System.AppContext.BaseDirectory -> the running executable's directory. Only
        // this member is routed here, so its siblings keep transpiling as real IL.
        (["System.AppContext"], static (mc, _, n, sig) => mc.TryEmitAppContextIntrinsic(n, sig)),
        // MemberInfo/ParameterInfo.GetCustomAttributes([attrType,] inherit) + IsDefined.
        (s_customAttributeReceiverTypes, static (mc, dt, n, sig) =>
            n is "GetCustomAttributes" or "IsDefined" && mc.TryEmitCustomAttributeIntrinsic(dt, n, sig)),
        // Static element-as-parameter attribute getters: System.Attribute's
        // GetCustomAttribute/GetCustomAttributes/IsDefined (element + Type are explicit
        // params). The CustomAttributeExtensions extension methods — including the generic
        // forms — pass typeof(T) into these, so intercepting here rather than at the
        // extension keeps the T filter correct.
        (["System.Attribute"], static (mc, dt, n, sig) =>
            n is "GetCustomAttribute" or "GetCustomAttributes" or "IsDefined"
            && mc.TryEmitStaticAttributeIntrinsic(dt, n, sig)),
        // Integer bit-counting / rotate statics (TrailingZeroCount, LeadingZeroCount,
        // Log2, PopCount, RotateLeft/Right) map to clang builtins. Declines every other
        // name on those types, so their CompareTo / GetHashCode reach the fallback's
        // TryEmitNumbersIntrinsic.
        (s_integerBitOpTypes, static (mc, dt, n, sig) => mc.TryEmitIntegerBitOpIntrinsic(dt, n, sig)),
        // System.Diagnostics.StackTrace / StackFrame: the degraded stack-trace model
        // (zero captured frames, and a ToString that says so). Both are intrinsic types,
        // so an unmapped member of either falls through to the loud miss.
        (s_stackDiagnosticsTypes, static (mc, dt, n, sig) => mc.TryEmitStackDiagnosticsIntrinsic(dt, n, sig)),
        // System.Diagnostics.Tracing.EventSource: the opaque intrinsic base of every
        // tracing provider (see s_intrinsicTypes). Its construct/write/enable surface
        // folds to no-ops; an unmapped member falls through to the loud miss.
        (["System.Diagnostics.Tracing.EventSource"],
            static (mc, _, n, sig) => mc.TryEmitEventSourceIntrinsic(n, sig)),
        // System.Resources.ResourceManager: the lookup over the assembly's
        // carried .resources blob. An intrinsic type, so an unmapped member falls
        // through to the loud miss rather than to a body — there is none.
        (["System.Resources.ResourceManager"],
            static (mc, dt, n, sig) => mc.TryEmitResourcesIntrinsic(dt, n, sig)),
    ];

    /// <summary>The arms that are NOT keyed on a declaring type — each owns a member set
    /// spread across types it tests for itself. They run after the keyed chain, on a
    /// dictionary MISS and on a keyed-chain DECLINE alike; dropping the decline half is
    /// what would send <c>Int32.CompareTo</c> (declined by the bit-op probe, answered by
    /// the numbers probe) to a loud throw.</summary>
    private static readonly IntrinsicProbe[] s_intrinsicFallbackProbes =
    [
        static (mc, dt, n, sig) => mc.TryEmitReflectionIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitEnumArrayIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitThreadingIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitConcurrentIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitNumbersIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitCharIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitStringsIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitConvertIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitBuildersTasksIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitAwaitersTasksIntrinsic(dt, n, sig),
        static (mc, dt, n, sig) => mc.TryEmitInterpStringIntrinsic(dt, n, sig),
    ];

    /// <summary><see cref="s_intrinsicProbeChain"/> grouped by key, each group in chain
    /// order.</summary>
    private static readonly Dictionary<string, IntrinsicProbe[]> s_intrinsicProbesByType =
        BuildIntrinsicProbesByType();

    private static Dictionary<string, IntrinsicProbe[]> BuildIntrinsicProbesByType()
    {
        var byType = new Dictionary<string, List<IntrinsicProbe>>(StringComparer.Ordinal);
        foreach (var (keys, probe) in s_intrinsicProbeChain)
        {
            foreach (string key in keys)
            {
                if (!byType.TryGetValue(key, out var chain))
                {
                    chain = [];
                    byType[key] = chain;
                }
                chain.Add(probe);
            }
        }
        var result = new Dictionary<string, IntrinsicProbe[]>(StringComparer.Ordinal);
        foreach (var kv in byType)
            result[kv.Key] = kv.Value.ToArray();
        return result;
    }

    /// <summary>The probe-able form of <see cref="EmitIntrinsic"/>: runs the whole
    /// per-type intrinsic table chain and returns false on a miss instead of
    /// throwing, so a caller with a fallback (the constrained static-virtual
    /// resolution, which can still take the callee's default interface body) can
    /// fall through. Safe to probe because every subtable pops its operands only
    /// after its (declType, name, signature-pattern) match has committed — a miss
    /// leaves the eval stack untouched.
    ///
    /// <para>The type-agnostic fallback runs on a dictionary miss and on a per-key decline
    /// alike, which is what lets an arm answer some names of a type and hand the rest
    /// on.</para></summary>
    private bool TryEmitIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        if (s_intrinsicProbesByType.TryGetValue(declType, out var chain))
        {
            foreach (var probe in chain)
            {
                if (probe(this, declType, name, sig))
                    return true;
            }
        }
        foreach (var probe in s_intrinsicFallbackProbes)
        {
            if (probe(this, declType, name, sig))
                return true;
        }
        return false;
    }

    /// <summary>The vector facades' hardware-acceleration constants. dn2cpp's vectors are
    /// a software model, so both answer the same token.</summary>
    private bool TryEmitVectorHardwareAccel(string name)
    {
        if (name is not ("get_IsSupported" or "get_IsHardwareAccelerated"))
            return false;
        Push(StackKind.I4, "int32_t", SimdHwAccelToken);
        return true;
    }

    /// <summary>A vector facade member whose element type the closed signature's primary
    /// vector names — the general software-vector op.</summary>
    private bool TryEmitVectorPrimaryOp(string name, MethodSignature<TypeDesc> sig) =>
        TryVectorPrimary(sig, out var vcpp, out var vw, out var velem)
        && TryEmitVectorOp(vcpp, vw, velem, name, sig, null);

    /// <summary>Dn2Cpp.Runtime.HotUpdate: Run(string) loads a Baked Patch Image and runs
    /// its entry method in the runtime interpreter, Load is the entry-optional variant,
    /// LoadDirectory the version-ordered deployment-directory driver (returns the number
    /// of BPIs loaded).</summary>
    private bool TryEmitHotUpdateIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        if (sig.ParameterTypes is not [{ IsString: true }])
            return false;
        if (name == "Run")
        {
            var bpiPath = Pop();
            Emit($"dn2cpp_hotupdate_run({Cast(bpiPath, "Dn2CppString*")});");
            return true;
        }
        if (name == "Load")
        {
            var bpiPath = Pop();
            Emit($"dn2cpp_hotupdate_load({Cast(bpiPath, "Dn2CppString*")});");
            return true;
        }
        if (name == "LoadDirectory")
        {
            var dirPath = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_hotupdate_load_dir({Cast(dirPath, "Dn2CppString*")})");
            return true;
        }
        return false;
    }

    /// <summary>Console.Error stderr-write surface: the Dn2CppConsoleWriter TextWriter
    /// subtype's overrides call these, and a spilled <c>Console.Error.WriteLine($"…")</c>
    /// callvirt lands on those overrides. Reuse EmitConsoleWrite with the Console.Error
    /// stream so the write is byte-identical to the non-spilled fast path. The receiver of
    /// these static helpers is not on the stack — the write target is always the singleton
    /// stderr.</summary>
    private bool TryEmitConsoleRuntimeIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        const string errStream = "dn2cpp_console_error()";
        if (name == "ErrWriteLine" && sig.ParameterTypes.Length == 0)
        {
            Emit($"dn2cpp_textwriter_writeline_empty({errStream});");
            return true;
        }
        if (name == "ErrWriteLine" && sig.ParameterTypes.Length == 1)
        {
            var arg = Pop();
            EmitConsoleWrite(arg, sig.ParameterTypes[0], newline: true, writer: errStream);
            return true;
        }
        if (name == "ErrWrite" && sig.ParameterTypes.Length == 1)
        {
            var arg = Pop();
            EmitConsoleWrite(arg, sig.ParameterTypes[0], newline: false, writer: errStream);
            return true;
        }
        return false;
    }

    /// <summary>The Object-virtual Equals(object) of the intrinsic value-type family — one
    /// arm for all six of <see cref="s_boxedIntrinsicValueTypes"/>, which differ only in the
    /// runtime handle a box of them carries and the three-way compare behind it, both
    /// mechanical in the type's own name.
    ///
    /// <para>Each of the six intrinsifies its TYPED Equals(X) in its own table and stops
    /// there, which by the sibling rule (docs/ARCHITECTURE.md §4-B) leaves this overload
    /// falling through to a loud throw. It is reached through Nullable&lt;X&gt;, whose
    /// Equals(object) hands `other` on as an object rather than unwrapping it, so any
    /// struct with an `X?` field walks straight into it.</para>
    ///
    /// <para><c>obj is X x &amp;&amp; Equals(x)</c>: a box of any other type is never equal,
    /// and a box of this one compares by the same three-way its typed sibling uses — so a
    /// value compared here, through a box, or as a Dictionary key all agree. The declaring
    /// type is not re-tested: this arm is reached only under the six keys it is registered
    /// against.</para></summary>
    private bool TryEmitBoxedIntrinsicValueEquals(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        if (name != "Equals" || sig.ParameterTypes is not [{ IsObject: true }])
            return false;
        string bare = declType["System.".Length..];        // Decimal, DateTimeOffset, ...
        string ct = "Dn2Cpp" + bare;                       // Dn2CppDecimal, ...
        string fn = "dn2cpp_" + bare.ToLowerInvariant();   // dn2cpp_decimal, ...
        string ob = NewTemp("Dn2CppObject*");
        Emit($"{ob} = {Cast(Pop(), "Dn2CppObject*")};");   // the argument, then the receiver
        var recv = Pop();
        string self = NewTemp(ct);
        Emit($"{self} = {(recv.Kind == StackKind.Ptr ? $"(*({ct}*)({recv.Expr}))" : recv.Expr)};");
        Push(StackKind.I4, "int32_t",
            $"(({ob}) != nullptr && ({ob})->type == &{fn}_type"
            + $" ? ({fn}_cmp({self}, *({ct}*)(({ob}) + 1)) == 0 ? 1 : 0) : 0)");
        return true;
    }

    /// <summary>Members reached on the opaque EventSource base of a provider. A native build
    /// ships no EventPipe/ETW/EventListener, so every DELIVERY path is a no-op: the base
    /// ctor, the [Event] methods' WriteEvent calls, the self-describing Write, and Dispose
    /// all do nothing. <c>IsEnabled(*)</c> answers false — and it must, because making
    /// EventSource intrinsic reroutes the framework providers' own cross-assembly
    /// <c>if (Log.IsEnabled())</c> guards through here (their MemberRef no longer resolves to
    /// a real method), so this arm is what keeps them folded.
    /// <para>A no-op write is FAITHFUL, not degraded: every write path in .NET's own
    /// EventSource opens with <c>if (!IsEnabled()) return;</c> and no listener can attach in
    /// a native build.</para>
    /// <para>IDENTITY — Name, Guid, Settings, CurrentThreadActivityId — is not about
    /// delivery: .NET computes all four with no listener in sight, so they are answered for
    /// real here (runtime/core/intrinsics/dn2cpp_eventsource.cpp), off the type-info stamp
    /// CppEmitter.EventSourceIdentity writes plus whatever the base ctor supplied.</para>
    /// <para>What is unmapped is OBSERVATION and MANIFEST: EventListener, GenerateManifest,
    /// GetTrait, the EventCommand surface. They fall through to the loud "no intrinsic
    /// mapping yet" transpile abort rather than degrading silently — a listener that
    /// receives nothing is the load-bearing-consumer failure of docs/ARCHITECTURE.md
    /// §4-B.</para>
    /// <para>Observation would be a REIMPLEMENTATION, not a reachability hole: dropping
    /// EventSource from <c>CoreIntrinsics.s_intrinsicTypes</c> leaves MORE gaps, because the
    /// construct and attach paths bottom out in <c>EventPipeInternal</c> QCalls with no IL
    /// to transpile, and the manifest/descriptor half wants IL BYTES at run time
    /// (<c>MethodBase.GetMethodBody</c>), which a dn2cpp binary does not carry. Delivery has
    /// to be rebuilt in the runtime the way identity was.</para>
    /// <para>One boundary of the refusal is deliberate: a provider's own
    /// <c>OnEventCommand</c> override is NOT refused and is not emitted either — this base
    /// is intrinsic, so it owns no vtable slot to dispatch through and tree-shaking drops
    /// it, which is what real .NET does with no listener attached.</para></summary>
    private bool TryEmitEventSourceIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        void PopArgsAndReceiver()
        {
            for (int i = 0; i < sig.ParameterTypes.Length; i++)
                Pop(); // event payload / ctor args — discarded
            if (sig.Header.IsInstance)
                Pop(); // the provider receiver — never read
        }
        // The receiver of an instance member, after its arguments have been popped.
        string PopArgsThenReceiver()
        {
            for (int i = 0; i < sig.ParameterTypes.Length; i++)
                Pop();
            return Cast(Pop(), "Dn2CppObject*");
        }
        // A System.Guid-shaped out parameter: `Guid` is a value struct, so the runtime
        // fills a local through its address rather than returning one. Returns the temp.
        string GuidOutTemp(TypeDesc guidType)
        {
            NoteLocalValueLayout(guidType);
            string ct = CppTypes.Of(guidType);
            string tmp = NewTemp(ct);
            Emit($"{tmp} = {CppTypes.ZeroInitExpr(ct)};");
            return tmp;
        }
        switch (name)
        {
            // Every write/dispose path -> a no-op (see the summary: this is what real .NET
            // does with no listener attached, not a degrade). The provider object is still
            // a real allocation — its own [Event] methods and fields transpile.
            case "WriteEvent":
            case "WriteEventCore":
            case "WriteEventWithRelatedActivityId":
            case "WriteEventWithRelatedActivityIdCore":
            case "Write":
            case "Dispose":
                PopArgsAndReceiver();
                return true;
            // The base ctor. Construction stays a no-op as far as tracing goes, but the
            // overloads that carry IDENTITY hand it to the runtime: `base("MyProvider")`
            // and `base(EventSourceSettings.…)` are per-INSTANCE values that no type-info
            // stamp can hold, and dropping them silently is what would make get_Name answer
            // the type name for a provider that renamed itself.
            case ".ctor":
                return EmitEventSourceCtor(sig);
            // IsEnabled(*) -> false (see the summary: this also catches the rerouted
            // framework-provider guards).
            case "IsEnabled":
                PopArgsAndReceiver();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            // EventSourceSettings get_Settings: the ctor's settings with .NET's
            // ValidateSettings applied — EtwManifestEventFormat for the parameterless base
            // ctor. NOT EventSourceSettings.Default (0), which real .NET never reports.
            case "get_Settings":
                Push(StackKind.I4, "int32_t", $"dn2cpp_eventsource_settings({PopArgsThenReceiver()})");
                return true;
            // get_Name: the declared provider name — [EventSource(Name=…)], the base ctor's
            // argument, or the simple type name, in .NET's own precedence.
            case "get_Name":
            {
                string recv = PopArgsThenReceiver();
                NoteReferenceClass(sig.ReturnType);
                Push(StackKind.Ref, CppTypes.Of(sig.ReturnType), $"dn2cpp_eventsource_name({recv})");
                return true;
            }
            // get_Guid: the explicit [EventSource(Guid=…)] or, failing that, the guid .NET
            // derives from the provider name (GenerateGuidFromName — SHA-1 over a fixed
            // namespace, reproduced in the runtime so the two agree).
            case "get_Guid":
            {
                string recv = PopArgsThenReceiver();
                string tmp = GuidOutTemp(sig.ReturnType);
                Emit($"dn2cpp_eventsource_guid({recv}, &{tmp});");
                Push(CppTypes.KindOf(sig.ReturnType), CppTypes.Of(sig.ReturnType), tmp);
                return true;
            }
            // The static (Type)-keyed identity helpers, which never consult an instance —
            // neither does .NET's GetName/GetGuid.
            case "GetName" when sig.ParameterTypes.Length == 1:
            {
                var ty = Pop();
                NoteReferenceClass(sig.ReturnType);
                Push(StackKind.Ref, CppTypes.Of(sig.ReturnType),
                    $"dn2cpp_eventsource_type_name(((Dn2CppType*)({ty.Expr}))->typeInfo)");
                return true;
            }
            case "GetGuid" when sig.ParameterTypes.Length == 1:
            {
                var ty = Pop();
                string tmp = GuidOutTemp(sig.ReturnType);
                Emit($"dn2cpp_eventsource_type_guid(((Dn2CppType*)({ty.Expr}))->typeInfo, &{tmp});");
                Push(CppTypes.KindOf(sig.ReturnType), CppTypes.Of(sig.ReturnType), tmp);
                return true;
            }
            // CurrentThreadActivityId / SetCurrentThreadActivityId: a per-thread Guid slot,
            // which is all it is in .NET too — the listener-facing half of an activity id is
            // what the delivery no-op drops, and the slot itself round-trips exactly.
            case "get_CurrentThreadActivityId":
            {
                string tmp = GuidOutTemp(sig.ReturnType);
                Emit($"dn2cpp_eventsource_get_activity_id(&{tmp});");
                Push(CppTypes.KindOf(sig.ReturnType), CppTypes.Of(sig.ReturnType), tmp);
                return true;
            }
            case "SetCurrentThreadActivityId" when sig.ParameterTypes.Length is 1 or 2:
            {
                // The two-argument overload's trailing `out Guid` is a byref; the
                // one-argument overload passes null for it.
                string prev = "nullptr";
                if (sig.ParameterTypes.Length == 2)
                    prev = $"(void*)({Pop().Expr})";
                var val = Pop();
                string vct = CppTypes.Of(sig.ParameterTypes[0]);
                NoteLocalValueLayout(sig.ParameterTypes[0]);
                string vtmp = NewTemp(vct);
                Emit($"{vtmp} = {val.Expr};");
                Emit($"dn2cpp_eventsource_set_activity_id(&{vtmp}, {prev});");
                return true;
            }
            // THE OBSERVATION SIDE — the members EventListener's own IL calls on this base,
            // and the only unmapped ones that have to say WHY rather than fall through to
            // the generic abort below. Each has an answer that looks obviously right, and
            // every one of them is the same trap: a listener that transpiles and then
            // receives nothing, forever, with no diagnostic (docs/ARCHITECTURE.md §4-B).
            //
            // ALL FOUR CARRY THE MESSAGE, NOT JUST IsSupported. One EventListener program
            // reaches at least two of these doors (`EventListener..ctor` opens IsSupported
            // and AddListener), and which one aborts is decided by DRAIN ORDER rather than
            // by the program — so a diagnostic on one door alone leaves the same source
            // aborting with a bare "no intrinsic mapping yet" that names neither
            // EventListener nor a remedy.
            case "get_IsSupported":
            case "AddListener":
            case "RemoveListener":
            case "SendCommand":
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: "
                    + $"System.Diagnostics.Tracing.EventSource.{(name == "get_IsSupported" ? "IsSupported" : name)}"
                    + " is deliberately unmapped — it is part of the observation side that "
                    + "System.Diagnostics.Tracing.EventListener's own IL calls, and a native build "
                    + "delivers no events, so answering it would produce a listener that silently "
                    + "observes nothing. dn2cpp models EventSource's identity and write surface, not "
                    + "its observation side, and the real IL is not the missing piece: it needs the "
                    + "CLR's EventPipe QCalls to construct a provider and to attach a listener, and "
                    + "run-time IL introspection for the event descriptors. Remove the EventListener, "
                    + "or reimplement delivery in the runtime the way identity already is"
                    // The chain is what names the caller's OWN listener class — this abort is
                    // raised inside CoreLib, so without it the message says nothing about the
                    // program that has to change.
                    + $" [chain: {Comp.ReachChain(Method)}]");
            // Any other EventSource member is unmodeled: fall through to the loud
            // "no intrinsic mapping yet" transpile abort (see the summary).
            default:
                return false;
        }
    }

    /// <summary>The EventSource base ctor. Every overload is a no-op as far as tracing
    /// goes, but three of the seven carry identity into the instance and one more implies
    /// a non-default settings word, so the arguments are classified by SHAPE rather than
    /// counted: a leading <c>string</c> is the provider name, an <c>EventSourceSettings</c>
    /// is the settings word, a leading <c>bool</c> is
    /// <c>ThrowOnEventWriteErrors</c>, and a trailing <c>string[]</c> is the traits array
    /// (dropped — <c>GetTrait</c> is unmapped, so nothing can observe it).</summary>
    private bool EmitEventSourceCtor(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        string name = "nullptr";
        string settings = "0";
        // Pop right to left, then the receiver.
        var popped = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
            popped[i] = Pop();
        string recv = Cast(Pop(), "Dn2CppObject*");
        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            if (p.IsString && i == 0)
                name = Cast(popped[i], "Dn2CppString*");
            else if ((p.Class?.FullName ?? p.ExternalName)
                        == "System.Diagnostics.Tracing.EventSourceSettings")
                settings = $"(int32_t)({popped[i].Expr})";
            else if (i == 0 && p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean })
                // EventSource(bool throwOnEventWriteErrors) forwards to
                // this(throwOnEventWriteErrors ? ThrowOnEventWriteErrors : Default).
                settings = $"(({popped[i].Expr}) ? 1 : 0)";
        }
        Emit($"dn2cpp_eventsource_ctor({recv}, {name}, {settings});");
        return true;
    }

    /// <summary>The two SynchronizationContext thread-slot statics (see the
    /// dispatch comment above). Every other member falls through — the general
    /// pipeline transpiles the real IL.</summary>
    private bool TryEmitSyncContextIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        switch (name)
        {
            case "get_Current" when sig.ParameterTypes.Length == 0:
            {
                if (sig.ReturnType is { Kind: TypeKind.Class, Class: { } scCls })
                    Comp.NoteReferencedType(scCls);
                string scT = CppTypes.Of(sig.ReturnType);
                Push(StackKind.Ref, scT, $"(({scT})dn2cpp_sync_ctx_get())");
                return true;
            }
            case "SetSynchronizationContext" when sig.ParameterTypes.Length == 1:
            {
                var ctx = Pop();
                Emit($"dn2cpp_sync_ctx_set({Cast(ctx, "Dn2CppObject*")});");
                return true;
            }
            default:
                return false;
        }
    }
}
