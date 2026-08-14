using System.Text;

namespace Dn2Cpp;

/// <summary>Target-specific code generation hooks consulted by <see cref="CppEmitter"/>.
/// Implementations live alongside the runtime they target — the console backend
/// here in the core, the Godot backend in Dn2Cpp.Godot — keeping the core
/// emitter free of any target knowledge.</summary>
internal interface IEmitBackend
{
    /// <summary>Runtime header the generated translation unit includes.</summary>
    string RuntimeHeader { get; }

    /// <summary>Optional per-call intrinsic translator, or null for none.</summary>
    ICallIntrinsics? CallIntrinsics { get; }

    /// <summary>True to skip emitting a method body — e.g. an engine shim method
    /// whose calls are replaced inline by <see cref="CallIntrinsics"/>. Such
    /// placeholders are never referenced as real functions, so only members that
    /// genuinely need a definition (notably constructors, for base-construction
    /// chains) should be emitted.</summary>
    bool ShouldSkipMethodBody(ClassInfo cls, MethodInfo m);

    /// <summary>False to keep a typeof-named user-LIBRARY class out of the
    /// reflection-invoke surface <see cref="Compilation"/> opens (instance
    /// accessors + public niladic methods, reached so a reflection helper can
    /// invoke what nothing calls statically). An engine-binding assembly
    /// typeof-names its whole wrapper surface but invokes it through generated
    /// trampolines, never reflection, so opting it out keeps the wrapper
    /// accessors out of the emit set. Default: every class gets the surface.</summary>
    bool WantsTypeofReflectionSurface(ClassInfo cls) => true;

    /// <summary>True to synthesize a real, invoker-compatible body for a
    /// skipped-body method in a hot-update base build. The interpreter binds a
    /// patch's method imports through reflection metadata (fnPtr + invoker),
    /// which a body-less method leaves null — so a backend whose intrinsics
    /// replace calls inline (the engine shims) opts the skipped surface in
    /// here, and the emitter renders each method's body from the intrinsic's
    /// own call lowering (<see cref="MethodCompiler.CompileIntrinsicCallWrapper"/>;
    /// a shape the lowering cannot express is skipped and stays bodyless).
    /// Consulted only for reachable methods <see cref="ShouldSkipMethodBody"/>
    /// skipped, and only under <c>--hotupdate-base</c> — never in a normal
    /// build. Default: none.</summary>
    bool WantsSyntheticBody(ClassInfo cls, MethodInfo m) => false;

    /// <summary>True for a method that IS emitted but whose body is a
    /// compile-time placeholder: the backend's intrinsics replace every call
    /// site inline, so the emitted body is dead code that computes nothing
    /// (e.g. <c>return default</c>). A hot-update base build excludes such
    /// methods (and constructors) from the reflection fnPtr/invoker wiring, so
    /// a patch import fails at load as a standard unresolved import instead of
    /// silently binding the placeholder and getting a default value. Never
    /// consulted in a normal build, whose output stays byte-identical.
    /// Default: none.</summary>
    bool HasPlaceholderBody(ClassInfo cls, MethodInfo m) => false;

    /// <summary>Emits the tail of the translation unit: the program entry point
    /// (console) or the target's registration tables (GDExtension).</summary>
    void EmitEpilogue(CppEmitter emitter, StringBuilder sb, IReadOnlyList<MethodInfo> cctors);

    /// <summary>Classes a backend instantiates outside the normal C# call graph
    /// (e.g. Godot's ClassDB create_instance_func, driven by GDScript `.new()` /
    /// scene or resource deserialization — never runs a `newobj`). Without an
    /// explicit seed here, Compilation.ReachAllocatedType never runs for these
    /// types, so their virtual/interface dispatch and ToString/Equals/GetHashCode/
    /// Finalize wiring silently goes unwired. Called once, after class/method
    /// discovery and before the first reachability drain. Default: none
    /// (ConsoleBackend is unaffected).</summary>
    IEnumerable<ClassInfo> ExternallyAllocatedClasses(Compilation c) => Array.Empty<ClassInfo>();

    /// <summary>Methods a backend's emitted epilogue calls directly, outside the
    /// normal C# call graph (e.g. an engine-init export bootstrapping a transpiled
    /// interop layer that no app-module root reaches). Seeded as reachability
    /// roots next to the program roots. Ordering guarantee: called once, after
    /// class/method discovery and before the first reachability drain — and
    /// therefore always before <see cref="EmitEpilogue"/> — so an implementation
    /// may resolve and cache here the members its epilogue names
    /// (DotnetModuleBackend caches the bootstrap chain and the ManagedCallbacks
    /// frame-callback slot; GodotBackend seeds the engine-callback surface).
    /// Default: none (ConsoleBackend is unaffected).</summary>
    IEnumerable<MethodInfo> AdditionalRootMethods(Compilation c) => Array.Empty<MethodInfo>();

    /// <summary>Backend-specific bounded methods, merged with the core BCL set in
    /// <see cref="CoreIntrinsics"/> (see <see cref="Compilation.IsBoundedMethod"/>):
    /// each (declaring type full name, method name) pair names a method whose
    /// subtree is cut at reachability and which is neutralized at call/ldftn
    /// sites — args dropped, default result pushed, address-of replaced by a
    /// no-op stub of the same shape. Read once at Compilation construction,
    /// before the first reachability drain. Default: none.</summary>
    IEnumerable<(string Type, string Method)> AdditionalBoundedMethods
        => Array.Empty<(string, string)>();

    /// <summary>The C++ type a <c>calli</c> function pointer must spell in return
    /// position for <paramref name="declared"/>, or null to keep the signature's own
    /// mapping. A standalone signature describes the managed declaration, not
    /// necessarily the ABI of the callee it names: a register-width target absorbs a
    /// narrower return, wasm32 traps on it. The emitter casts the result back to the
    /// declared type. Default: the declaration is the ABI.</summary>
    string? CalliAbiType(MethodInfo enclosing, TypeDesc declared) => null;

    /// <summary>Engine-wrapper allowlist trim (<c>--trim-godot-classes</c>): a backend
    /// whose engine layer registers one "native class name → allocate managed wrapper"
    /// lambda per engine class in a single registry cctor (GodotSharp's
    /// <c>Godot.Constructors..cctor</c>, 955 entries) nominates that cctor here, and
    /// <see cref="Compilation"/> defers the cctor's <c>ldftn</c> edges instead of
    /// reaching them — releasing a lambda only when user code (outside the registry's
    /// own module) actually names its wrapper class, and redirecting the rest to the
    /// nearest released ancestor's lambda at emit. The spec carries the registry cctor,
    /// the wrapper root class bounding eligibility (a class is trimmable iff it lives
    /// in the cctor's module and derives from the root), and the floor classes released
    /// unconditionally (the redirect terminators plus any <c>--godot-class-root</c>).
    /// Called once, after class/method discovery and before the first reachability
    /// drain — the nomination must be installed before the cctor can be scanned.
    /// Default: null (disabled; ConsoleBackend and GodotBackend are unaffected — the
    /// shim lane has no registry cctor and no equivalent problem).</summary>
    GodotClassTrimSpec? GodotClassTrim(Compilation c) => null;
}

/// <summary>The <see cref="IEmitBackend.GodotClassTrim"/> nomination: the registry
/// cctor whose ldftn edges are deferred, the wrapper root bounding which classes the
/// trim may touch, and the unconditionally-released floor (see the hook's doc).</summary>
internal sealed record GodotClassTrimSpec(
    MethodInfo RegistryCctor, ClassInfo WrapperRoot, IReadOnlyList<ClassInfo> Floor);
