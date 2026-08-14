using System.Text;

namespace Dn2Cpp.DotnetModule;

/// <summary>Godot .NET-module backend: emits the <c>godotsharp_game_main_init</c>
/// export the engine's mono module loads as a NativeAOT-style drop-in, bootstrapping
/// the transpiled real GodotSharp bridge (NativeFuncs / ManagedCallbacks /
/// ScriptManagerBridge) instead of a console <c>main</c> or GDExtension tables.
/// The input is the game assembly built by the real Godot.NET.Sdk, transpiled
/// against the real GodotSharp.dll (passed with -r); no shims are involved.</summary>
internal sealed class DotnetModuleBackend : IEmitBackend
{
    // --trim-godot-classes state (see GodotClassTrim below). Constructor-injected
    // by the CLI; default off, so every other caller keeps byte-identical output.
    private readonly bool _trimGodotClasses;
    private readonly IReadOnlyList<string> _godotClassRoots;

    public DotnetModuleBackend(bool trimGodotClasses = false, IReadOnlyList<string>? godotClassRoots = null)
    {
        _trimGodotClasses = trimGodotClasses;
        _godotClassRoots = godotClassRoots ?? Array.Empty<string>();
    }

    /// <summary>Real GodotSharp's engine interop surface: one declaration per slot of
    /// the table, each invoking its <c>delegate* unmanaged</c> field by <c>calli</c>.</summary>
    private const string NativeFuncsType = "Godot.NativeInterop.NativeFuncs";

    public string RuntimeHeader => "dn2cpp_dotnetmodule.h";

    public ICallIntrinsics? CallIntrinsics { get; } = new DotnetModuleCallIntrinsics();

    /// <summary>Real-GodotSharp cuts (merged into the bounded-method set — subtree
    /// cut at reachability, call/ldftn sites neutralized). On the embedded game
    /// path the ALC-reload machinery is runtime-dead
    /// (AlcReloadCfg.IsAlcReloadingEnabled is only ever set by the hostfxr editor
    /// host), so cutting these roots keeps AssemblyLoadContext /
    /// ConditionalWeakTable / DependentHandle out of the tree.
    ///
    /// <para>Stack-frame decoration is deliberately NOT cut: StackTrace / StackFrame are
    /// core degraded intrinsics, so GodotSharp's DebuggingUtils / ExceptionUtils reach the
    /// same answers by running. GD.PushError/PushWarning still deliver the message; only
    /// the caller file/line/method decoration degrades, there being no stack to walk.</para></summary>
    private static readonly (string Type, string Method)[] s_boundedMethods =
    {
        // ALC-reload bookkeeping registries (would subscribe OnAlcUnloading): no-op.
        ("Godot.Bridge.ScriptManagerBridge", "AddTypeForAlcReloading"),
        ("Godot.Bridge.ScriptManagerBridge", "TrackAlcForUnloading"),
        // GodotPlugins.Game.Main's engine-handle DllImport resolver body (reached
        // only as the delegate target of the NativeLibrary.SetDllImportResolver
        // registration, itself cut lane-neutrally in CoreIntrinsics; its Windows
        // arm calls Win32.GetModuleHandle, an unmodeled InternalCall).
        ("Godot.NativeInterop.GodotDllImportResolver", "OnResolveDllImport"),
        // --- ManagedCallbacks cuts (the engine's mono-module callback table takes
        // the address of these; a cut entry becomes a no-op stub with the same
        // shape — see the bounded branch of Ldftn in MethodCompiler). Each is
        // editor/debugger-only machinery on the embedded game path:
        // the ALC-reload script-reload callback (scans loaded assemblies via
        // AppDomain.GetAssemblies; reloading is permanently off, like the
        // AddTypeForAlcReloading/TrackAlcForUnloading cuts above);
        ("Godot.Bridge.ScriptManagerBridge", "TryReloadRegisteredScriptWithClass"),
        // the delegate serialization callbacks (Delegate.Method/GetInvocationList
        // reflection + BinaryWriter persistence, for editor-reload state). The Try*
        // pair reports false, which the engine handles as an unserializable
        // delegate. The rest of the DelegateUtils family (DelegateEquals /
        // DelegateHash / GetArgumentCount / InvokeWithVariantArgs) transpiles REAL —
        // engine-side Callable identity, dedup and dispatch need it.
        ("Godot.DelegateUtils", "TrySerializeDelegateWithGCHandle"),
        ("Godot.DelegateUtils", "TryDeserializeDelegateWithGCHandle"),
        // the inner (non-GCHandle) serialize pair called by the generated
        // SaveGodotObjectData/RestoreGodotObjectData overrides of a [Signal] script
        // class (editor hot-reload state capture, dead on the embedded game path).
        // Cutting here keeps the Delegate.Method/BinaryWriter/AppDomain subtree out
        // while the runtime delegate callbacks stay real.
        ("Godot.DelegateUtils", "TrySerializeDelegate"),
        ("Godot.DelegateUtils", "TryDeserializeDelegate"),
        // GD.OnCoreApiAssemblyLoaded's task-scheduler install (main-thread
        // continuation marshalling). Un-cutting is impossible, not merely
        // undesirable: GodotTaskScheduler derives from the intrinsic-mapped
        // System.Threading.Tasks.TaskScheduler (an opaque Dn2CppObject* with no
        // fields, vtable or layout), so no transpiled class can extend it. Its job
        // is mirrored natively instead — GodotTaskScheduler's two-stage Activate
        // maps to the two per-frame main-thread calls in the host's frame callback:
        // dn2cpp_sched_pump() and the SynchronizationContext drain (EmitEpilogue's
        // dn2cpp_dm_sync_ctx owns the singleton the cut install would have created).
        // With the install cut the scheduler stays null and the per-frame Activate
        // is unreachable, so cut it too and TaskScheduler.TryExecuteTask with its
        // ConditionalWeakTable/DependentHandle subtree stays out of the tree.
        ("Godot.Dispatcher", "InitializeDefaultGodotTaskScheduler"),
        ("Godot.GodotTaskScheduler", "Activate"),
        // Dispatcher.SynchronizationContext's real getter reads the cut
        // scheduler's field (NRE by construction here). Call sites are
        // intercepted first (DotnetModuleCallIntrinsics pushes the native
        // singleton), so like the CustomGCHandle rows this bound only cuts the
        // body; the call never sees the neutralization.
        ("Godot.Dispatcher", "get_SynchronizationContext"),
        // Variant -> GodotObject[] conversion builds the typed array via
        // Activator.CreateInstance(arrayType, length) — runtime array creation from
        // a Type, which dn2cpp does not model; the converter reports null for that
        // rare target shape. (RuntimeTypeConversionHelper is in the global namespace.)
        ("RuntimeTypeConversionHelper", "<ConvertToObjectOfType>g__ConvertToSystemArrayOfGodotObject|3_0"),
        // Delegate collectibility scan (walks GetInvocationList + Method
        // reflection). An AOT image has no collectible AssemblyLoadContext, so
        // "not collectible" is the permanently-correct answer, not a degrade.
        ("Godot.DelegateUtils", "IsDelegateCollectible"),
        // GDScript-facing static-method dispatch on a C# script class: invokes the
        // reflected method through MethodInfo.CreateDelegate<T>, a
        // reflection-to-delegate bridge dn2cpp does not model. The stubbed callback
        // reports "no such method" (false).
        ("Godot.Bridge.ScriptManagerBridge", "CallStatic"),
    };

    /// <summary>The cuts above, plus the CustomGCHandle wrappers — bounded for the
    /// reachability cut ONLY: their call sites are taken first by
    /// <see cref="DotnetModuleCallIntrinsics"/> and forwarded to the Dn2CppGCHandle
    /// model, so the bounded call-site neutralization never sees them. Projected from
    /// the intrinsic's own method list so the two cannot drift. (An <c>ldftn</c> of one
    /// would get the bounded no-op stub; real GodotSharp only calls them directly.)</summary>
    public IEnumerable<(string Type, string Method)> AdditionalBoundedMethods =>
        s_boundedMethods.Concat(DotnetModuleCallIntrinsics.s_customGCHandleMethods
            .Select(m => (DotnetModuleCallIntrinsics.CustomGCHandleType, m)));

    // Resolved by AdditionalRootMethods (which Compilation calls before the first
    // reachability drain, so well before EmitEpilogue) so the epilogue can name
    // the transpiled bootstrap functions directly.
    private MethodInfo? _nativeFuncsInitialize;
    private MethodInfo? _managedCallbacksCreate;
    private MethodInfo? _lookupScriptsInAssembly;
    private MethodInfo? _syncCtxCtor;
    private MethodInfo? _syncCtxExecute;
    private ClassInfo? _unmanagedCallbacks;
    private ClassInfo? _exceptionClass;
    private MethodInfo? _logException;
    private MethodInfo? _pushError;
    private string? _nativeCtorNotFound;
    private int? _frameCallbackSlot;
    private string _appAssemblyName = "";

    /// <summary>The source-generated <c>GodotPlugins.Game.Main</c> class carries the
    /// managed-host bootstrap (the <c>godotsharp_game_main_init</c> UnmanagedCallersOnly
    /// entry and its DllImportResolver machinery). The emitted C++ export below replaces
    /// it wholesale, so none of its bodies may be transpiled.</summary>
    public bool ShouldSkipMethodBody(ClassInfo cls, MethodInfo m)
        => cls.FullName == "GodotPlugins.Game.Main";

    /// <summary>GodotSharp typeof-names its whole wrapper surface and invokes it
    /// through source-generated trampolines, never reflection — the reflection
    /// surface would more than double the emit set, past V8's per-function ceiling
    /// on the wasm drop-in's relocation applier.</summary>
    public bool WantsTypeofReflectionSurface(ClassInfo cls)
        => cls.Module.AssemblyName != "GodotSharp";

    /// <summary>An interop slot returning a core enum returns <c>int32_t</c>: Godot's
    /// C++ enums are int-width and the glue spells the return so, while the bindings
    /// generator widens every core enum to <c>: long</c> on the C# side. A predicate
    /// and not a list of names, so a re-pin that adds such a slot is covered.</summary>
    public string? CalliAbiType(MethodInfo enclosing, TypeDesc declared)
    {
        if (enclosing.DeclaringClass.FullName != NativeFuncsType)
            return null;
        return declared.Kind == TypeKind.Class && declared.Class is { IsEnum: true }
            ? "int32_t" : null;
    }

    /// <summary>The transpiled bootstrap chain the emitted export calls: engine
    /// interop-table install, managed-callback table fill, script registration.
    /// None of these is reachable from an app-module root (the game assembly's
    /// classes only call inward), so they are seeded explicitly.</summary>
    public IEnumerable<MethodInfo> AdditionalRootMethods(Compilation c)
    {
        _appAssemblyName = c.AppModule.AssemblyName;
        _nativeFuncsInitialize = ResolveRoot(c, NativeFuncsType, "Initialize", 2);
        _managedCallbacksCreate = ResolveRoot(c, "Godot.Bridge.ManagedCallbacks", "Create", 1);
        _lookupScriptsInAssembly = ResolveRoot(c, "Godot.Bridge.ScriptManagerBridge", "LookupScriptsInAssembly", 1);
        // The main-thread SynchronizationContext singleton: the epilogue's
        // dn2cpp_dm_sync_ctx/pump call the ctor and ExecutePendingContinuations
        // directly, and Post/Send are what the vtable dispatch of the
        // virtual-dispatch-bounded SynchronizationContext.Post (and Send) lands
        // on — root all four so they are emitted whether or not the app's own
        // call graph reaches them.
        _syncCtxCtor = ResolveRoot(c, "Godot.GodotSynchronizationContext", ".ctor", 0);
        _syncCtxExecute = ResolveRoot(c, "Godot.GodotSynchronizationContext", "ExecutePendingContinuations", 0);
        var syncCtxPost = ResolveRoot(c, "Godot.GodotSynchronizationContext", "Post", 2);
        var syncCtxSend = ResolveRoot(c, "Godot.GodotSynchronizationContext", "Send", 2);
        // The struct whose size the emitted entry's interop-size probe reports (see
        // EmitEpilogue). Resolved here, with the other roots, so a missing/renamed
        // one fails at the same loud place they do rather than inside the emit.
        _unmanagedCallbacks = ResolveNestedStruct(c, NativeFuncsType, "UnmanagedCallbacks");
        // The ManagedCallbacks slot the emitted entry wraps (the per-frame
        // callback), resolved BY NAME from the model — the same structural
        // resolution UnmanagedCallbacks gets above — so a GodotSharp re-pin that
        // reorders the struct moves the patch with it instead of silently
        // wrapping whatever callback now sits at the old index.
        _frameCallbackSlot = ResolveInstanceFieldOrdinal(
            _managedCallbacksCreate.DeclaringClass, "ScriptManagerBridge_FrameCallback");
        // The host-boundary error sink (dn2cpp_core.h). GodotSharp's own bridge
        // callbacks end in `catch (Exception e) { ExceptionUtils.LogException(e); }`,
        // so dn2cpp's own boundaries route through the same pair: script-debugger
        // routing when one is attached, GD.PushError otherwise. PushError carries the
        // boundary NAME, which LogException has no parameter for.
        _logException = ResolveRoot(c, "Godot.NativeInterop.ExceptionUtils", "LogException", 1);
        _pushError = ResolveRoot(c, "Godot.NativeInterop.ExceptionUtils", "PushError", 1);
        _exceptionClass = c.FindClassByFullName("System.Exception")
            ?? throw new NotSupportedException(
                "--dotnet-module: type System.Exception not found (no CoreLib in the load set?)");
        // The ONE startup-cctor failure that is not news — see the emitted loop.
        // Resolved from the model rather than spelled as a literal so a
        // GodotSharp re-pin that renames or moves it fails the transpile here,
        // where the remedy is one line, instead of turning the filter into a
        // silent no-op that reports a guaranteed false error every frame-1.
        _nativeCtorNotFound = Compilation.ReflectionTypeName(
            ResolveNestedStruct(c, "Godot.GodotObject", "NativeConstructorNotFoundException"));
        return new[]
        {
            _nativeFuncsInitialize, _managedCallbacksCreate, _lookupScriptsInAssembly,
            _syncCtxCtor, _syncCtxExecute, syncCtxPost, syncCtxSend,
            _logException, _pushError,
        };
    }

    /// <summary>GodotSynchronizationContext is constructed by the emitted C++
    /// (the epilogue's <c>dn2cpp_dm_sync_ctx</c>), not by any managed newobj the
    /// reachability scan could see — without this its vtable/type-info would not
    /// emit and the virtual Post/Send dispatch would land on nothing.</summary>
    public IEnumerable<ClassInfo> ExternallyAllocatedClasses(Compilation c)
    {
        var ctx = c.FindClassByFullName("Godot.GodotSynchronizationContext");
        return ctx is null ? Array.Empty<ClassInfo>() : new[] { ctx };
    }

    /// <summary>The <c>--trim-godot-classes</c> nomination (see
    /// <see cref="IEmitBackend.GodotClassTrim"/>): the real GodotSharp's
    /// <c>Godot.Constructors..cctor</c> is the registry whose per-class allocate
    /// lambdas root the whole engine-wrapper surface; <c>Godot.GodotObject</c> bounds
    /// eligibility (registry module + GodotObject-derived = an engine wrapper); the
    /// floor is GodotObject (native name "Object") and RefCounted — the guaranteed
    /// redirect terminators — plus every <c>--godot-class-root</c>, each resolved with
    /// a hard error: a typo silently becoming a no-op root would surface only as an
    /// ancestor-typed wrapper in a shipped game.</summary>
    public GodotClassTrimSpec? GodotClassTrim(Compilation c)
    {
        if (!_trimGodotClasses)
            return null;
        var registry = c.FindClassByFullName("Godot.Constructors")
            ?? throw new NotSupportedException(
                "--trim-godot-classes: type Godot.Constructors not found — "
                + "is the real GodotSharp.dll passed with -r?");
        var cctor = registry.EnsureMembers().StaticCctor
            ?? throw new NotSupportedException(
                "--trim-godot-classes: Godot.Constructors has no static constructor "
                + "(unexpected GodotSharp shape)");
        var wrapperRoot = ResolveEngineWrapper(c, registry, "Godot.GodotObject", flag: null);
        var floor = new List<ClassInfo>
        {
            wrapperRoot,
            ResolveEngineWrapper(c, registry, "Godot.RefCounted", flag: null),
        };
        foreach (var root in _godotClassRoots)
            floor.Add(ResolveEngineWrapper(c, registry, root, flag: "--godot-class-root"));
        return new GodotClassTrimSpec(cctor, wrapperRoot, floor);
    }

    /// <summary>Resolves one engine-wrapper class for the trim spec, hard-failing on
    /// a name that is not a loaded GodotSharp GodotObject-derived class. <paramref
    /// name="flag"/> names the CLI flag in the diagnostic for user-passed roots;
    /// null marks the built-in floor classes (whose absence means a GodotSharp
    /// shape this backend does not understand).</summary>
    private static ClassInfo ResolveEngineWrapper(Compilation c, ClassInfo registry, string fullName, string? flag)
    {
        string what = flag is null ? "--trim-godot-classes" : $"{flag} {fullName}";
        var cls = c.FindClassByFullName(fullName)
            ?? throw new NotSupportedException(
                $"{what}: no loaded assembly declares type {fullName}");
        if (cls.Module != registry.Module)
            throw new NotSupportedException(
                $"{what}: {fullName} is not declared in GodotSharp (found in {cls.Module.AssemblyName})");
        bool derivesFromGodotObject = false;
        for (var b = cls; b is not null; b = b.BaseClass)
            if (b.FullName == "Godot.GodotObject")
            {
                derivesFromGodotObject = true;
                break;
            }
        if (!derivesFromGodotObject)
            throw new NotSupportedException(
                $"{what}: {fullName} is not an engine wrapper (does not derive from Godot.GodotObject)");
        return cls;
    }

    /// <summary>Resolve a NESTED value type by its enclosing type and simple name. A
    /// nested type's <see cref="ClassInfo.FullName"/> is its BARE simple name — the
    /// enclosing chain lives in <c>CppNamePrefix</c>, not in the full name — so
    /// <see cref="Compilation.FindClassByFullName"/> is namespace-blind here and could
    /// hand back any same-named nested type from any loaded assembly. Re-check the
    /// enclosing chain rather than trusting the hit (<c>EndsWith</c>, not equality: a
    /// C++-name collision can prepend a module discriminator to the prefix).</summary>
    private static ClassInfo ResolveNestedStruct(Compilation c, string enclosingFullName, string name)
    {
        var cls = c.FindClassByFullName(name);
        if (cls is null || !cls.CppNamePrefix.EndsWith(enclosingFullName + ".", StringComparison.Ordinal))
            throw new NotSupportedException(
                $"--dotnet-module: nested type {enclosingFullName}.{name} not found — " +
                "is the real GodotSharp.dll passed with -r?");
        return cls;
    }

    /// <summary>The 0-based ordinal of <paramref name="fieldName"/> among
    /// <paramref name="cls"/>'s instance fields — for a sequential-layout struct of
    /// function pointers (ManagedCallbacks) that IS the <c>void*[]</c> index the
    /// emitted entry patches. Hard error when the field is absent: falling back to
    /// a counted constant would wrap the wrong callback with no diagnostic.</summary>
    private static int ResolveInstanceFieldOrdinal(ClassInfo cls, string fieldName)
    {
        int ordinal = 0;
        foreach (var f in cls.Fields)
        {
            if (f.IsStatic || f.IsLiteral)
                continue;
            if (f.Name == fieldName)
                return ordinal;
            ordinal++;
        }
        throw new NotSupportedException(
            $"--dotnet-module: field {cls.FullName}.{fieldName} not found (GodotSharp shape changed?)");
    }

    private static MethodInfo ResolveRoot(Compilation c, string typeFullName, string methodName, int paramCount)
    {
        var cls = c.FindClassByFullName(typeFullName)
            ?? throw new NotSupportedException(
                $"--dotnet-module: type {typeFullName} not found — is the real GodotSharp.dll passed with -r?");
        return cls.Methods.FirstOrDefault(m =>
                m.Name == methodName && m.Rva != 0 && m.Signature.ParameterTypes.Length == paramCount)
            ?? throw new NotSupportedException(
                $"--dotnet-module: method {typeFullName}.{methodName} ({paramCount} parameter(s)) not found");
    }

    /// <summary>Emits the startup pass's name-list accumulator and the one loud line
    /// it ends in. The list is a fixed-capacity buffer because it is filled from a
    /// <c>catch</c> during init, where an allocation is the least welcome thing
    /// available; the price is that a long enough list is elided, so <c>count</c> is
    /// tracked apart from the text and the message says when the two disagree.</summary>
    private static void EmitDisabledTypeReporter(StringBuilder sb, string pushErrorSym)
    {
        sb.AppendLine("// A \", \"-joined, fixed-capacity list of type names, with an EXACT count");
        sb.AppendLine("// beside it: `named` is how many of `count` actually fit in `buf`.");
        sb.AppendLine("struct Dn2CppDmTypeList { char buf[480]; int len; int count; int named; };");
        sb.AppendLine("static void dn2cpp_dm_type_list_add(Dn2CppDmTypeList* __l, const char* __name)");
        sb.AppendLine("{");
        sb.AppendLine("    // The names come from the cctor table's baked C-string literals, so");
        sb.AppendLine("    // they are never null and never move; only the capacity can refuse one.");
        sb.AppendLine("    ++__l->count;");
        sb.AppendLine("    int __n = (int)std::strlen(__name);");
        sb.AppendLine("    int __sep = __l->len > 0 ? 2 : 0;");
        sb.AppendLine("    if (__l->len + __sep + __n >= (int)sizeof(__l->buf))");
        sb.AppendLine("        return;");
        sb.AppendLine("    if (__sep != 0) { __l->buf[__l->len++] = ','; __l->buf[__l->len++] = ' '; }");
        sb.AppendLine("    std::memcpy(__l->buf + __l->len, __name, (size_t)__n);");
        sb.AppendLine("    __l->len += __n;");
        sb.AppendLine("    __l->buf[__l->len] = '\\0';");
        sb.AppendLine("    ++__l->named;");
        sb.AppendLine("}");
        sb.AppendLine("static void dn2cpp_dm_report_disabled_types(const Dn2CppDmTypeList* __l)");
        sb.AppendLine("{");
        sb.AppendLine("    char __msg[640];");
        sb.AppendLine("    int __n = std::snprintf(__msg, sizeof(__msg),");
        sb.AppendLine("        \"dn2cpp: %d type(s) left unusable by a failed startup static constructor \"");
        sb.AppendLine("        \"(every use of one re-raises its failure): %s%s\",");
        sb.AppendLine("        __l->count, __l->buf, __l->named < __l->count ? \", ...\" : \"\");");
        sb.AppendLine("    if (__n < 0) __n = 0;");
        sb.AppendLine("    if (__n > (int)sizeof(__msg) - 1) __n = (int)sizeof(__msg) - 1;");
        sb.AppendLine("    // PushError directly, not dn2cpp_report_boundary_exception: there is no");
        sb.AppendLine("    // exception here to report. Each failure already went through the");
        sb.AppendLine("    // boundary reporter as it happened; this line is a fact about the PASS,");
        sb.AppendLine("    // and it goes to the channel that reporter's own sink ends in, which is");
        sb.AppendLine("    // where the per-failure lines it indexes are. By the time the deferred");
        sb.AppendLine("    // pass runs (the first engine frame) the entry has returned, so");
        sb.AppendLine("    // GodotSharp's statics and the interop table are live — the same");
        sb.AppendLine("    // precondition the per-failure reports above already rely on.");
        sb.AppendLine("    try {");
        sb.AppendLine($"        {pushErrorSym}(dn2cpp_string_from_utf8(__msg, (int32_t)__n));");
        sb.AppendLine("    } catch (Dn2CppException& __ex) {");
        sb.AppendLine("        // A summary whose whole subject is that init went wrong must not be");
        sb.AppendLine("        // the thing that takes the process down when the managed reporter is");
        sb.AppendLine("        // part of what went wrong. stderr is the channel that cannot fail.");
        sb.AppendLine("        dn2cpp_exc_inflight_pop(__ex.obj);");
        sb.AppendLine("        std::fprintf(stderr, \"[dn2cpp] %s\\n\", __msg);");
        sb.AppendLine("        std::fflush(stderr);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }

    public void EmitEpilogue(CppEmitter emitter, StringBuilder sb, IReadOnlyList<MethodInfo> cctors)
    {
        var initialize = _nativeFuncsInitialize
            ?? throw new InvalidOperationException("AdditionalRootMethods did not run before EmitEpilogue");
        var create = _managedCallbacksCreate!;
        var lookup = _lookupScriptsInAssembly!;
        string initializeSym = emitter.RequireDefinedBodySymbol(
            initialize, "--dotnet-module godotsharp_game_main_init interop install");
        string createSym = emitter.RequireDefinedBodySymbol(
            create, "--dotnet-module managed-callbacks table create");
        string lookupSym = emitter.RequireDefinedBodySymbol(
            lookup, "--dotnet-module script-class registration lookup");
        // Both report entry points are spelled by two pieces of this epilogue, so they
        // are resolved once here. An epilogue is emitted TEXT, not a compiled body, so
        // the named-symbol backstop cannot see it: without this check a --cut on one of
        // them gives a green transpile whose C++ does not link.
        string pushErrorSym = emitter.RequireDefinedBodySymbol(
            _pushError ?? throw new InvalidOperationException(
                "AdditionalRootMethods did not run before EmitEpilogue"),
            "--dotnet-module host-boundary report (ExceptionUtils.PushError)");
        string logExceptionSym = emitter.RequireDefinedBodySymbol(
            _logException ?? throw new InvalidOperationException(
                "AdditionalRootMethods did not run before EmitEpilogue"),
            "--dotnet-module host-boundary report (ExceptionUtils.LogException)");

        sb.AppendLine("// ---- Godot .NET-module entry (mono-module NativeAOT drop-in ABI) ----");
        sb.AppendLine("// The engine loads this library in place of the hostfxr-managed game");
        sb.AppendLine("// assembly and calls godotsharp_game_main_init once at module init.");
        sb.AppendLine("//");
        sb.AppendLine("// The eager startup cctor pass, deferred to the first engine frame (see");
        sb.AppendLine("// dn2cpp_dm_set_deferred_cctor_pass): the engine copies the managed-callback");
        sb.AppendLine("// table into GDMonoCache only after the entry returns, so a cctor that");
        sb.AppendLine("// reaches back into the engine during init (an Engine-singleton wrapper, a");
        sb.AppendLine("// method-bind resolve on a wrapped object) would jump through a still-null");
        sb.AppendLine("// callback slot. Until the pass runs, cctors trigger lazily through the");
        sb.AppendLine("// use-site first-use guards — the same ordering real .NET gives this code.");
        sb.AppendLine("//");
        sb.AppendLine("// Each cctor runs isolated: an engine wrapper class absent from this engine");
        sb.AppendLine("// build (editor-only, disabled module — e.g. LightmapperRD in a headless");
        sb.AppendLine("// template) throws NativeConstructorNotFoundException from its cctor's");
        sb.AppendLine("// ClassDB constructor probe. Real .NET would only ever run that cctor on");
        sb.AppendLine("// first use of the class; here the failure marks the type unusable — the");
        sb.AppendLine("// runtime remembers it and every first-use guard re-raises it rather than");
        sb.AppendLine("// rerunning the cctor or reading its half-set statics.");
        sb.AppendLine("//");
        sb.AppendLine("// Every such failure is reported loudly, through the shared host-boundary");
        sb.AppendLine("// reporter (dn2cpp_core.h), and the pass ends with one line naming the whole");
        sb.AppendLine("// set of types it left unusable — the per-failure lines print hundreds of");
        sb.AppendLine("// engine lines before the first frame, and only the summary shows the SET.");
        sb.AppendLine("//");
        sb.AppendLine("// The absent-class case is the ONE exception, on both channels: it hits on");
        sb.AppendLine("// every run of every headless template, so a loud report would be a standing");
        sb.AppendLine("// false alarm, and it is not a fact about the program at all — real .NET");
        sb.AppendLine("// would never run that cctor, and its first-use guard raises the same thing");
        sb.AppendLine("// when something actually touches the class. It keeps its own summary line");
        sb.AppendLine("// on the trace channel, so DN2CPP_DM_TRACE=1 shows the complete picture.");
        if (cctors.Count > 0)
        {
            EmitDisabledTypeReporter(sb, pushErrorSym);
            // Carry each cctor's declaring type name beside its function pointer so a
            // throw names the type whose static init failed: the exception type alone
            // does not identify it (an absent engine wrapper throws from deep in a
            // ClassDB probe). The name is the CLR reflection name, baked as a
            // C-string literal the same way type-infos carry it.
            sb.AppendLine("struct Dn2CppDmCctorEntry { void (*fn)(); const char* type; };");
            sb.AppendLine("static const Dn2CppDmCctorEntry dn2cpp_dm_startup_cctor_table[] = {");
            foreach (var cctor in cctors)
                sb.AppendLine($"    {{ &{cctor.CppName}__ensure, \"{Compilation.ReflectionTypeName(cctor.DeclaringClass)}\" }},");
            sb.AppendLine("};");
        }
        sb.AppendLine("static void dn2cpp_dm_run_startup_cctors()");
        sb.AppendLine("{");
        if (cctors.Count > 0)
        {
            sb.AppendLine("    Dn2CppDmTypeList __disabled{};");
            sb.AppendLine("    Dn2CppDmTypeList __absent{};");
            sb.AppendLine("    for (const Dn2CppDmCctorEntry& __e : dn2cpp_dm_startup_cctor_table)");
            sb.AppendLine("    {");
            sb.AppendLine("        try {");
            sb.AppendLine("            __e.fn();");
            sb.AppendLine("        } catch (Dn2CppException& __ex) {");
            sb.AppendLine("            const char* __tn = (__ex.obj != nullptr && __ex.obj->type != nullptr)");
            sb.AppendLine("                ? __ex.obj->type->name : nullptr;");
            sb.AppendLine($"            if (__tn != nullptr && std::strcmp(__tn, \"{_nativeCtorNotFound}\") == 0) {{");
            sb.AppendLine("                // Swallowed, so drop the in-flight root the throw pushed —");
            sb.AppendLine("                // the reporting arm does this inside the shared helper.");
            sb.AppendLine("                dn2cpp_exc_inflight_pop(__ex.obj);");
            sb.AppendLine("                dn2cpp_dm_trace(\"startup cctor for %s: class absent from this engine build\", __e.type);");
            sb.AppendLine("                dn2cpp_dm_type_list_add(&__absent, __e.type);");
            sb.AppendLine("            } else {");
            sb.AppendLine("                dn2cpp_report_boundary_exception(__ex.obj, \"the startup static constructor of %s\", __e.type);");
            sb.AppendLine("                dn2cpp_dm_type_list_add(&__disabled, __e.type);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    // Both summaries are emitted only when they have something to say: a");
            sb.AppendLine("    // \"0 types disabled\" line every run is a line people learn to skip, and");
            sb.AppendLine("    // learning to skip THIS line is the failure mode it exists to prevent.");
            sb.AppendLine("    if (__disabled.count > 0)");
            sb.AppendLine("        dn2cpp_dm_report_disabled_types(&__disabled);");
            sb.AppendLine("    if (__absent.count > 0)");
            sb.AppendLine("        dn2cpp_dm_trace(\"%d type(s) unusable - class absent from this engine build: %s%s\",");
            sb.AppendLine("                        __absent.count, __absent.buf,");
            sb.AppendLine("                        __absent.named < __absent.count ? \", ...\" : \"\");");
        }
        sb.AppendLine("}");
        sb.AppendLine();
        var syncCtxCls = (_syncCtxCtor ?? throw new InvalidOperationException(
            "AdditionalRootMethods did not run before EmitEpilogue")).DeclaringClass;
        string ctxT = syncCtxCls.CppStructName;
        // Every method symbol this epilogue spells goes through
        // RequireDefinedBodySymbol, for the reason given at pushErrorSym above.
        string syncCtxCtorSym = emitter.RequireDefinedBodySymbol(
            _syncCtxCtor, "--dotnet-module main-thread SynchronizationContext ctor");
        string syncCtxExecuteSym = emitter.RequireDefinedBodySymbol(
            _syncCtxExecute!, "--dotnet-module main-thread SynchronizationContext frame drain");
        sb.AppendLine("// ---- main-thread SynchronizationContext bootstrap + drain ----");
        sb.AppendLine("// The native owner of the singleton the cut GodotTaskScheduler install");
        sb.AppendLine("// would have created (see dn2cpp_dotnetmodule.h). The cache is a static in");
        sb.AppendLine("// this image's __DATA, which self-roots mode registers with the collector.");
        sb.AppendLine($"static {ctxT}* g_dn2cpp_dm_sync_ctx = nullptr;");
        sb.AppendLine("Dn2CppObject* dn2cpp_dm_sync_ctx()");
        sb.AppendLine("{");
        sb.AppendLine("    if (g_dn2cpp_dm_sync_ctx == nullptr)");
        sb.AppendLine("    {");
        sb.AppendLine($"        auto* __ctx = ({ctxT}*)dn2cpp_alloc(sizeof({ctxT}));");
        sb.AppendLine($"        ((Dn2CppObject*)__ctx)->type = {emitter.TypeInfoRef(syncCtxCls, "main-thread SynchronizationContext singleton")};");
        sb.AppendLine($"        {syncCtxCtorSym}(__ctx);");
        sb.AppendLine("        g_dn2cpp_dm_sync_ctx = __ctx;");
        sb.AppendLine("        // The creating thread's Current — by construction the engine main");
        sb.AppendLine("        // thread: the frame pump below is the first caller, on frame 1,");
        sb.AppendLine("        // before any script or worker code runs. Its Send can then take the");
        sb.AppendLine("        // `Current == this` inline fast path instead of deadlocking.");
        sb.AppendLine("        dn2cpp_sync_ctx_set((Dn2CppObject*)__ctx);");
        sb.AppendLine("    }");
        sb.AppendLine("    return (Dn2CppObject*)g_dn2cpp_dm_sync_ctx;");
        sb.AppendLine("}");
        sb.AppendLine("static void dn2cpp_dm_sync_ctx_pump()");
        sb.AppendLine("{");
        sb.AppendLine($"    {syncCtxExecuteSym}(({ctxT}*)dn2cpp_dm_sync_ctx());");
        sb.AppendLine("}");
        sb.AppendLine();
        string excT = (_exceptionClass ?? throw new InvalidOperationException(
            "AdditionalRootMethods did not run before EmitEpilogue")).CppStructName;
        sb.AppendLine("// ---- host boundary error sink (see dn2cpp_core.h) ----");
        sb.AppendLine("// This lane's half of the shared contract: a managed fault the engine");
        sb.AppendLine("// cannot be allowed to unwind through gets reported the way real");
        sb.AppendLine("// GodotSharp reports one — through its own ExceptionUtils, so the script");
        sb.AppendLine("// debugger picks it up when attached and GD.PushError delivers it to the");
        sb.AppendLine("// engine's error log otherwise. The extra PushError line carries the");
        sb.AppendLine("// boundary NAME: LogException has no parameter for it, and \"which node");
        sb.AppendLine("// method\" is the half that makes the report actionable.");
        sb.AppendLine("static void dn2cpp_dm_boundary_sink(const char* __where, Dn2CppObject* __exc)");
        sb.AppendLine("{");
        sb.AppendLine("    char __buf[640];");
        sb.AppendLine("    int __n = std::snprintf(__buf, sizeof(__buf),");
        sb.AppendLine("        \"dn2cpp: unhandled managed exception in %s\", __where != nullptr ? __where : \"<unknown>\");");
        sb.AppendLine("    if (__n < 0) __n = 0;");
        sb.AppendLine("    if (__n > (int)sizeof(__buf) - 1) __n = (int)sizeof(__buf) - 1;");
        sb.AppendLine($"    {pushErrorSym}(dn2cpp_string_from_utf8(__buf, (int32_t)__n));");
        sb.AppendLine("    if (__exc != nullptr)");
        sb.AppendLine($"        {logExceptionSym}(({excT}*)__exc);");
        sb.AppendLine("}");
        sb.AppendLine();
        var unmanagedCallbacks = _unmanagedCallbacks
            ?? throw new InvalidOperationException("AdditionalRootMethods did not run before EmitEpilogue");
        // Both entries below are the drop-in's public surface: the engine dlopens the
        // library and dlsyms godotsharp_game_main_init out of it. The two toolchains
        // spell the export differently, and a bare __attribute__ fails to compile on
        // MSVC. Same split, same reason, as DN2CPP_UCO_EXPORT (CppEmitter) and
        // DN2CPP_RT_EXPORT (runtime/core/dn2cpp.h): keyed on _WIN32, not _MSC_VER —
        // MinGW-w64 defines _WIN32 without _MSC_VER, and the visibility attribute does
        // not put a symbol in a Windows DLL's export table.
        sb.AppendLine("#if defined(_WIN32) && !defined(__CYGWIN__)");
        sb.AppendLine("#define DN2CPP_DM_EXPORT extern \"C\" __declspec(dllexport)");
        sb.AppendLine("#else");
        sb.AppendLine("#define DN2CPP_DM_EXPORT extern \"C\" __attribute__((visibility(\"default\")))");
        sb.AppendLine("#endif");
        sb.AppendLine();
        sb.AppendLine("// ---- interop-table size probe ----");
        sb.AppendLine("// The entry below hands interop_funcs_size_bytes to NativeFuncs.Initialize, which");
        sb.AppendLine("// compares it against sizeof(UnmanagedCallbacks) and throws when they differ. The");
        sb.AppendLine("// engine knows that size because it built the table; a test host does not — and it");
        sb.AppendLine("// cannot discover the size by probing, because Initialize latches its `initialized`");
        sb.AppendLine("// flag BEFORE it validates, so a second call reports \"Already initialized.\" and the");
        sb.AppendLine("// real answer is gone. Nor is it a constant worth hard-coding: the table is a struct");
        sb.AppendLine("// of function pointers, so it is half the size on a 32-bit target (wasm32) as on a");
        sb.AppendLine("// 64-bit one. Export it, and a host can ask.");
        sb.AppendLine("DN2CPP_DM_EXPORT");
        sb.AppendLine("int32_t dn2cpp_dm_interop_size(void)");
        sb.AppendLine("{");
        sb.AppendLine($"    return (int32_t)sizeof({unmanagedCallbacks.CppStructName});");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("DN2CPP_DM_EXPORT");
        sb.AppendLine("int8_t godotsharp_game_main_init(void* godot_dll_handle, void* out_managed_callbacks,");
        sb.AppendLine("                                 const void** interop_funcs, int32_t interop_funcs_size_bytes)");
        sb.AppendLine("{");
        sb.AppendLine("    (void)godot_dll_handle; // symbol resolution is static — no DllImportResolver needed");
        sb.AppendLine("    // Real-time host defaults, set before dn2cpp_runtime_init reads them (the");
        sb.AppendLine("    // same set the GDExtension init applies): bounded GC pauses via Boehm's");
        sb.AppendLine("    // incremental mode, finalizers drained manually on the engine's main");
        sb.AppendLine("    // thread (per-frame, via the wrapped FrameCallback below) instead of a");
        sb.AppendLine("    // background finalizer thread — Godot object lifecycle is main-thread-affine —");
        sb.AppendLine("    // and self-roots mode: a windowed engine process loads hundreds of system");
        sb.AppendLine("    // frameworks, which overflows Boehm's per-image root-set table (\"Too many");
        sb.AppendLine("    // root sets\" abort), so only our image's __DATA is registered.");
        sb.AppendLine("    dn2cpp_gc_set_incremental_default(1);");
        sb.AppendLine("    dn2cpp_gc_set_manual_finalizer_drain(1);");
        sb.AppendLine("    dn2cpp_gc_set_self_roots_default(1);");
        sb.AppendLine("    // A game may hand [UnmanagedCallersOnly] function pointers to a native");
        sb.AppendLine("    // library (an audio middleware's I/O vtable) that invokes them from");
        sb.AppendLine("    // threads it spawned itself — threads Boehm has never seen, whose first");
        sb.AppendLine("    // managed allocation would abort the collector. Enable the prologue's");
        sb.AppendLine("    // register-on-entry (RAII unregister at thread exit) for every such entry.");
        sb.AppendLine("    dn2cpp_set_native_callback_gc_registration(1);");
        // Runtime + string-literal init only; no eager cctors here — the startup
        // pass is deferred to the first frame (see dn2cpp_dm_run_startup_cctors),
        // and anything init itself touches runs via the use-site guards.
        emitter.EmitInitCalls(sb, Array.Empty<MethodInfo>());
        sb.AppendLine("    // The engine calls this on a thread Boehm has not seen; register it.");
        sb.AppendLine("    dn2cpp_dotnetmodule_thread_guard();");
        sb.AppendLine("    dn2cpp_dm_trace(\"interop received (size=%d)\", (int)interop_funcs_size_bytes);");
        sb.AppendLine("    try {");
        sb.AppendLine("        // Install the engine's interop function table FIRST: static");
        sb.AppendLine("        // constructors (StringName caches, method binds) already call");
        sb.AppendLine("        // through it, so it must be live before any cctor can trigger.");
        sb.AppendLine($"        {initializeSym}((intptr_t)interop_funcs, interop_funcs_size_bytes);");
        sb.AppendLine($"        {createSym}((intptr_t)out_managed_callbacks);");
        // The slot index is resolved by name in AdditionalRootMethods; the emitted
        // comment's slot-layout note describes the current GodotSharp pin, but the
        // INDEX itself always comes from the model.
        int frameSlot = _frameCallbackSlot
            ?? throw new InvalidOperationException("AdditionalRootMethods did not run before EmitEpilogue");
        sb.AppendLine($"        // Wrap the ScriptManagerBridge_FrameCallback slot (field index {frameSlot} of");
        sb.AppendLine("        // ManagedCallbacks, 0-based: SignalAwaiter_SignalCallback + six");
        sb.AppendLine("        // DelegateUtils_* slots precede it) so every engine frame first runs");
        sb.AppendLine("        // the runtime's per-frame work (thread guard + finalizer drain), then");
        sb.AppendLine("        // the managed callback Create just wrote into the slot.");
        sb.AppendLine("        dn2cpp_dm_set_managed_frame_callback(");
        sb.AppendLine($"            reinterpret_cast<void (*)()>((uintptr_t)((const void**)out_managed_callbacks)[{frameSlot}]));");
        sb.AppendLine($"        ((const void**)out_managed_callbacks)[{frameSlot}] = (const void*)&dn2cpp_dm_frame_callback;");
        sb.AppendLine("        dn2cpp_dm_trace(\"managed callbacks written\");");
        sb.AppendLine("        // Register the game assembly's script classes. An Assembly is modeled");
        sb.AppendLine("        // as its simple-name handle (see CppTypes: System.Reflection.Assembly).");
        sb.AppendLine($"        {lookupSym}(\"{_appAssemblyName}\");");
        sb.AppendLine("        // Run the remaining startup cctors on the first engine frame — the");
        sb.AppendLine("        // engine-side callback cache is not live until this entry returns.");
        sb.AppendLine("        dn2cpp_dm_set_deferred_cctor_pass(&dn2cpp_dm_run_startup_cctors);");
        sb.AppendLine("        // Per-frame drain of the main-thread SynchronizationContext; its");
        sb.AppendLine("        // first call creates + installs the singleton on the main thread.");
        sb.AppendLine("        dn2cpp_dm_set_sync_ctx_pump(&dn2cpp_dm_sync_ctx_pump);");
        sb.AppendLine("        // Install the boundary reporter LAST, and that ordering is the");
        sb.AppendLine("        // contract, not tidiness: the sink calls GodotSharp code that needs");
        sb.AppendLine("        // the interop table installed and its own statics live. Registering");
        sb.AppendLine("        // it only once every step above has succeeded is what keeps the");
        sb.AppendLine("        // catch below on the stderr fallback — the one channel a half-built");
        sb.AppendLine("        // managed side cannot take away. It also tells the core there is now");
        sb.AppendLine("        // a host frame able to survive a managed fault, which is what makes");
        sb.AppendLine("        // dn2cpp_sched_pump report instead of aborting the engine.");
        sb.AppendLine("        dn2cpp_set_boundary_exception_sink(&dn2cpp_dm_boundary_sink);");
        sb.AppendLine("        dn2cpp_dm_trace(\"init ok\");");
        sb.AppendLine("        return 1;");
        sb.AppendLine("    } catch (Dn2CppException& __ex) {");
        sb.AppendLine("        dn2cpp_report_boundary_exception(__ex.obj, \"godotsharp_game_main_init\");");
        sb.AppendLine("        return 0;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
    }
}
