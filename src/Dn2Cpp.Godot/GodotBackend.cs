using System.Reflection.Metadata;
using System.Text;
using Vk = global::Godot.VariantKind;

namespace Dn2Cpp.Godot;

/// <summary>Godot GDExtension backend: emits the static-method export table and
/// node-class table consumed by runtime/godot/dn2cpp_godot.cpp, instead of a
/// console <c>main</c>.</summary>
internal sealed class GodotBackend : IEmitBackend
{
    public string RuntimeHeader => "dn2cpp_godot.h";

    /// <summary>The engine API dump that drives the call map and the epilogue's
    /// class tables. Resolution: the <c>--godot-api</c> flag when passed, else the
    /// <c>DN2CPP_GODOT_API</c> environment variable (the documented fallback for
    /// callers that cannot pass a flag — an input locator, not an output knob:
    /// every route must name the same dump or the transpile fails loudly), else
    /// <c>extension_api.json</c> in the working directory (the build scripts run
    /// from the repo root).</summary>
    private readonly string _extensionApiPath;
    private GodotCallIntrinsics? _callIntrinsics;

    public GodotBackend(string? extensionApiPath = null)
    {
        _extensionApiPath = extensionApiPath
            ?? EnvKnobs.StringOr(EnvKnobs.GodotApi, "extension_api.json");
    }

    /// <summary>Built lazily, NOT at construction: the CLI constructs the backend
    /// before <c>TranspileDriver.Run</c>'s try, so an eager load of a missing API dump
    /// would escape as a raw FileNotFoundException. The first read happens inside the
    /// driver, where <see cref="RequireExtensionApi"/>'s NotSupportedException lands in
    /// the standard error formatting (one <c>error:</c> line, exit 2).</summary>
    public ICallIntrinsics? CallIntrinsics =>
        _callIntrinsics ??= new GodotCallIntrinsics(RequireExtensionApi());

    /// <summary>A missing dump is bad input, not a transpiler bug — so it throws
    /// the driver-rendered NotSupportedException, naming the path and both
    /// remedies.</summary>
    private string RequireExtensionApi()
    {
        if (!File.Exists(_extensionApiPath))
            throw new NotSupportedException(
                $"extension_api.json not found: {_extensionApiPath} (pass --godot-api <path>, "
                + "or generate one with `godot --headless --dump-extension-api`)");
        return _extensionApiPath;
    }

    // Godot.GD and the engine shim classes are placeholders: their non-virtual
    // API calls are replaced inline by the intrinsics, so those bodies are never
    // referenced and need not be emitted. Constructors (base-construction chains)
    // and virtuals (vtable slots / engine callbacks) are still required. The
    // transpiler-internal `__`-prefixed helpers (e.g. Object.__SetHandle, used by
    // the Variant→shim conversion to plant a borrowed handle) are genuine bodies
    // the intrinsics do NOT intercept — a transpiled caller emits a direct call,
    // so they must be emitted like any reachable method.
    public bool ShouldSkipMethodBody(ClassInfo cls, MethodInfo m)
    {
        if (cls.FullName == "Godot.GD")
            return true;
        return IsEngineShimClass(cls) && m.Name != ".ctor" && !m.IsVirtual
            && !m.Name.StartsWith("__", System.StringComparison.Ordinal);
    }

    /// <summary>Hot-update base builds synthesize a real, invoker-compatible
    /// body for every skipped engine-shim method (its parameters driven through
    /// the same call lowering an AOT call site gets), so interpreted patch code
    /// can bind and call the reachable shim surface through the standard
    /// reflection fnPtr/invoker pair. A marshalling shape the lowering cannot
    /// express is skipped by the emitter and stays bodyless (unresolved at
    /// patch load — the missing-AOT boundary). Never consulted in a normal
    /// build, whose output stays byte-identical.</summary>
    public bool WantsSyntheticBody(ClassInfo cls, MethodInfo m) => ShouldSkipMethodBody(cls, m);

    /// <summary>Same opt-out as DotnetModuleBackend's: the shim assembly (also
    /// named GodotSharp) typeof-names its wrapper surface while every call is
    /// lowered inline by <see cref="GodotCallIntrinsics"/> — reflection never
    /// invokes it, so the surface would only emit placeholder bodies.</summary>
    public bool WantsTypeofReflectionSurface(ClassInfo cls)
        => cls.Module.AssemblyName != "GodotSharp";

    /// <summary>The placeholder-bodied shim surface outside
    /// <see cref="ShouldSkipMethodBody"/>: Mathf (a static class — not
    /// Godot.Object-derived) and the Godot.Collections wrappers (namespace excluded
    /// from the engine-shim predicate). Their C# bodies are `return default` stand-ins
    /// with every call site lowered inline by <see cref="GodotCallIntrinsics"/>, yet
    /// they carry real fnPtr/invoker metadata — so a hot-update patch import would
    /// bind one and silently get a default value. Excluding them from the wiring turns
    /// that into the standard unresolved-import load failure. The Collections members
    /// with genuine bodies (the duck-typed GetEnumerator, the finalizer chaining into
    /// __Destroy) stay wired.</summary>
    public bool HasPlaceholderBody(ClassInfo cls, MethodInfo m)
    {
        if (cls.FullName == "Godot.Mathf")
            return true;
        return cls.Namespace == "Godot.Collections" && !cls.IsValueType
            && m.Name is not "GetEnumerator" and not "Finalize";
    }

    /// <summary>A generated engine shim: a reference type in namespace Godot that
    /// derives (transitively) from the Godot.Object shim. Excludes the value-type
    /// helpers (Vector2, Callable) and attributes.</summary>
    private static bool IsEngineShimClass(ClassInfo cls)
    {
        if (cls.Namespace != "Godot" || cls.IsValueType || cls.IsEnum || cls.IsInterface)
            return false;
        for (var c = cls; c is not null; c = c.BaseClass)
        {
            if (c.FullName == "Godot.Object")
                return true;
        }
        return false;
    }

    /// <summary>Every C# class ClassDB can instantiate through its
    /// create_instance_func (GDScript `.new()`, scene/resource deserialization)
    /// without ever running a C# `newobj` — so Compilation must seed reachability
    /// for these explicitly. Same set <see cref="EmitGodotNodeClasses"/> registers.</summary>
    public IEnumerable<ClassInfo> ExternallyAllocatedClasses(Compilation c)
    {
        foreach (var cls in c.Classes.Where(IsExportedGodotClass))
            yield return cls;
    }

    public void EmitEpilogue(CppEmitter emitter, StringBuilder sb, IReadOnlyList<MethodInfo> cctors)
    {
        var (_, all, _, _, _, globalEnums) = GodotApi.Load(RequireExtensionApi());

        var exports = new List<(MethodInfo Method, int RetGd, List<int> ArgGds)>();
        // App-module classes only: the public-surface root rule roots just the app
        // module's public methods, so a wrapper for a public CoreLib method the program
        // never calls would reference a body that was never emitted. Intrinsic-mapped
        // types are bodiless too — calls are replaced inline — and the IsIntrinsicType
        // name check covers only the BCL's own, so the mapping itself is tested as well
        // (a game-declared custom async task type adopted into the intrinsic Task
        // family is just as bodiless).
        // Snapshotted, because emitter.Classes is the live Compilation.Classes: reading
        // a method's signature below decodes it, and a decode instantiates the generics
        // that signature names — appending to the list walked here.
        foreach (var cls in emitter.Classes.ToList().Where(c =>
                     c.IsPublic && c.Namespace != "Godot"
                     && emitter.IsAppModuleClass(c)
                     && !CoreIntrinsics.IsIntrinsicType(c.FullName)
                     && c.IntrinsicCppName is null))
        {
            bool isNodeClass = GodotExportParent(cls) is not null;
            // The methods this class resolves as engine callbacks (the `_*`
            // virtuals bridged through the node-class virtual table, and
            // _Notification through the notification slot) are engine entry
            // points, never GDScript-callable user exports. Any other
            // leading-underscore method (an `_On...` handler wired up in a
            // scene) is a normal method bind and stays exported.
            var engineCallbacks = isNodeClass
                ? EngineVirtualOverridesOf(cls, all, globalEnums)
                : null;

            foreach (var m in cls.Methods.Where(m => m.IsPublic && m.Rva != 0))
            {
                if (m.Name == ".ctor") continue;
                if (!m.IsStatic && !isNodeClass) continue;
                if (engineCallbacks is not null && engineCallbacks.Contains(m)) continue;
                // Property accessors must also be exported for ClassDB property registration.

                int? ret = GdTypeOf(m.Signature.ReturnType);
                var argGds = m.Signature.ParameterTypes.Select(GdTypeOf).ToList();
                if (ret is null || argGds.Any(a => a is null or 0) || argGds.Count > 8)
                {
                    Console.Error.WriteLine($"warning: skipping Godot export {cls.Name}.{m.Name} (unsupported signature)");
                    continue;
                }
                exports.Add((m, ret.Value, argGds.Select(a => a!.Value).ToList()));
            }
        }

        sb.AppendLine("// ---- Godot export wrappers ----");
        for (int i = 0; i < exports.Count; i++)
        {
            var (m, retGd, argGds) = exports[i];
            sb.AppendLine($"static void gw_{i}(void* inst, [[maybe_unused]] const Dn2CppValue* a, [[maybe_unused]] Dn2CppValue* r)");
            sb.AppendLine("{");
            var callArgs = new List<string>();
            if (!m.IsStatic)
                callArgs.Add($"({m.DeclaringClass.CppStructName}*)inst");

            for (int ai = 0; ai < argGds.Count; ai++)
            {
                string cppType = CppTypes.Of(m.Signature.ParameterTypes[ai]);
                // Large value types (>16B) arrive in the generic `big` slot in Godot's
                // native float order; rebuild the C# shim struct by mapping flat floats
                // (the slot/shim orders differ for the transposed Basis/Transform3D).
                if (GdTag.BigTypeFloatMap(argGds[ai]) is { } map)
                {
                    string local = $"_a{ai}";
                    sb.Append($"    {cppType} {local}; {{ float* _p = (float*)&{local}; const float* _s = a[{ai}].big;");
                    for (int k = 0; k < map.Length; k++)
                        sb.Append($" _p[{k}] = _s[{map[k]}];");
                    sb.AppendLine(" }");
                    callArgs.Add(local);
                    continue;
                }
                callArgs.Add(argGds[ai] switch
                {
                    GdTag.Float => $"({cppType})a[{ai}].d",
                    GdTag.String => $"a[{ai}].s",
                    // A bare Variant: the runtime wrote the decoded POD into the
                    // (layout-identical) Dn2CppValue slot; read it straight back out
                    // as the shim struct.
                    GdTag.Variant => $"*reinterpret_cast<const {cppType}*>(&a[{ai}])",
                    GdTag.Vector2 => $"{cppType}{{ (float)a[{ai}].v2[0], (float)a[{ai}].v2[1] }}",
                    GdTag.Vector3 => $"{cppType}{{ (float)a[{ai}].v3[0], (float)a[{ai}].v3[1], (float)a[{ai}].v3[2] }}",
                    // Vector4 and Color are both four contiguous floats — a
                    // positional brace-init fills X/Y/Z/W (R/G/B/A) in declared order.
                    // Direct-list-init (`T{...}`), not a `(T){...}` compound literal —
                    // the latter is a GNU/clang extension MSVC rejects (C4576).
                    GdTag.Vector4 or GdTag.Color => $"{cppType}{{ (float)a[{ai}].v4[0], (float)a[{ai}].v4[1], (float)a[{ai}].v4[2], (float)a[{ai}].v4[3] }}",
                    // Packed*/Array (a managed Dn2CppArray*) and the container
                    // wrappers (Dictionary/GodotArray, a managed Godot.Collections.*
                    // pointer) arrive in the `arr` slot.
                    _ when GdTag.RidesArraySlot(argGds[ai]) => $"({cppType})a[{ai}].arr",
                    _ => $"({cppType})a[{ai}].i",
                });
            }
            string callee = emitter.RequireDefinedBodySymbol(
                m, $"Godot method bind {m.DeclaringClass.Name}.{m.Name}");
            string call = $"{callee}({string.Join(", ", callArgs)})";
            if (GdTag.BigTypeFloatMap(retGd) is { } retMap)
            {
                // Returned shim struct (flat floats) → the native-order `big` slot.
                var s = new StringBuilder($"{{ auto _rv = {call}; const float* _p = (const float*)&_rv;");
                for (int j = 0; j < retMap.Length; j++)
                    s.Append($" r->big[{j}] = _p[{retMap[j]}];");
                s.Append(" }");
                sb.AppendLine("    " + s);
            }
            else
            {
                sb.AppendLine("    " + retGd switch
                {
                    GdTag.Void => $"{call};",
                    GdTag.Float => $"r->d = (double){call};",
                    GdTag.String => $"r->s = {call};",
                    // A bare Variant return: stash the returned shim struct in
                    // the (layout-identical, 64-byte) Dn2CppValue slot; the runtime
                    // encodes it into the engine type from there.
                    GdTag.Variant => $"{{ auto _rv = {call}; *reinterpret_cast<decltype(_rv)*>(r) = _rv; }}",
                    GdTag.Vector2 => $"{{ auto _rv = {call}; r->v2[0] = _rv.f_X; r->v2[1] = _rv.f_Y; }}",
                    GdTag.Vector3 => $"{{ auto _rv = {call}; r->v3[0] = _rv.f_X; r->v3[1] = _rv.f_Y; r->v3[2] = _rv.f_Z; }}",
                    GdTag.Vector4 => $"{{ auto _rv = {call}; r->v4[0] = _rv.f_X; r->v4[1] = _rv.f_Y; r->v4[2] = _rv.f_Z; r->v4[3] = _rv.f_W; }}",
                    GdTag.Color => $"{{ auto _rv = {call}; r->v4[0] = _rv.f_R; r->v4[1] = _rv.f_G; r->v4[2] = _rv.f_B; r->v4[3] = _rv.f_A; }}",
                    // Packed*/Array return (a managed Dn2CppArray*) and the container
                    // wrappers (Dictionary/GodotArray): stash the managed pointer in
                    // the `arr` slot.
                    _ when GdTag.RidesArraySlot(retGd) => $"r->arr = (void*)({call});",
                    _ => $"r->i = (int64_t){call};",
                });
            }
            sb.AppendLine("}");
        }
        sb.AppendLine();

        sb.AppendLine("const Dn2CppGodotMethod dn2cpp_godot_methods[] =");
        sb.AppendLine("{");
        for (int i = 0; i < exports.Count; i++)
        {
            var (m, retGd, argGds) = exports[i];
            string argTypes = string.Join(", ", argGds);
            sb.AppendLine($"    {{ \"{m.DeclaringClass.Name}\", \"{m.Name}\", &gw_{i}, {retGd}, {argGds.Count}, {{ {argTypes} }}, {(m.IsStatic ? 1 : 0)} }},");
        }
        if (exports.Count == 0)
            sb.AppendLine("    { nullptr, nullptr, nullptr, 0, 0, {}, 0 },");
        sb.AppendLine("};");
        sb.AppendLine($"const int dn2cpp_godot_method_count = {exports.Count};");
        sb.AppendLine();

        EmitGodotNodeClasses(emitter, sb, all, globalEnums);

        // Custom-callable dispatch bridge: the runtime's callable trampoline
        // (a delegate-backed Godot.Callable invoked by the engine) re-enters
        // managed code through Godot.Callable.__Dispatch, passing the pinned
        // shim-struct copy the callable carries plus the decoded Variant args.
        var dispatch = FindCallableDispatch(emitter.Classes);
        if (dispatch is not null && emitter.IsEmitted(dispatch.DeclaringClass))
        {
            string callableStruct = dispatch.DeclaringClass.CppStructName;
            string argsCpp = CppTypes.Of(dispatch.Signature.ParameterTypes[1]);
            sb.AppendLine("// ---- Godot custom-callable dispatch bridge ----");
            sb.AppendLine("static void gw_callable_dispatch(void* c, void* args)");
            sb.AppendLine("{");
            string dispatchSym = emitter.RequireDefinedBodySymbol(
                dispatch, "Godot custom-callable dispatch bridge");
            sb.AppendLine($"    {dispatchSym}(*({callableStruct}*)c, ({argsCpp})args);");
            sb.AppendLine("}");
            sb.AppendLine();
        }

        sb.AppendLine("void dn2cpp_godot_init_managed()");
        sb.AppendLine("{");
        if (dispatch is not null && emitter.IsEmitted(dispatch.DeclaringClass))
            sb.AppendLine("    dn2cpp_godot_set_callable_dispatch(&gw_callable_dispatch);");
        EmitVariantPayloadRegistrations(emitter, sb);
        emitter.EmitInitCalls(sb, cctors);
        sb.AppendLine("}");
    }

    /// <summary>The managed re-entry point for delegate-backed callables, or
    /// null when the compilation has no Godot.Callable (then no delegate-backed
    /// callable can exist either).</summary>
    private static MethodInfo? FindCallableDispatch(IEnumerable<ClassInfo> classes)
    {
        // Lowest SortKey wins, not first-in-Classes-order: nothing emitted may depend
        // on the order discovery built the model in (see ClassInfo.SortKey).
        ClassInfo? cls = null;
        foreach (var c in classes)
            if (c.FullName == "Godot.Callable"
                && (cls is null || ClassInfo.CompareByOrder(c, cls) < 0))
                cls = c;
        return cls?.Methods.FirstOrDefault(m =>
            m.IsStatic && m.Name == "__Dispatch" && m.Rva != 0
            && m.Signature.ParameterTypes.Length == 2);
    }

    /// <summary>Seeds the methods the engine calls back into that no C# call graph
    /// reaches: Godot.Callable.__Dispatch (the custom-callable dispatch bridge), and
    /// every leading-underscore instance method of an exported class — the engine
    /// `_*` virtual overrides bridged through the node-class virtual table (and
    /// _Notification through the notification slot), which the emitted epilogue
    /// references directly by name.</summary>
    public IEnumerable<MethodInfo> AdditionalRootMethods(Compilation c)
    {
        if (FindCallableDispatch(c.Classes) is { } dispatch)
            yield return dispatch;

        foreach (var cls in c.Classes.Where(IsExportedGodotClass))
        {
            for (var k = cls; k is not null && k.Namespace != "Godot"; k = k.BaseClass)
            {
                foreach (var m in k.Methods)
                {
                    if (!m.IsStatic && m.Rva != 0 && m.Name.StartsWith("_", System.StringComparison.Ordinal))
                        yield return m;
                }
            }
        }
    }

    /// <summary>Registers with the runtime Variant codec the transpiled type-infos
    /// it must allocate on decode: the boxed shim struct for each big-POD payload
    /// kind and the precise <c>ti_arr_*</c> for each packed-array payload kind —
    /// but only those this compilation actually emitted (an unregistered payload
    /// decodes as nil; the program has no type to receive it anyway). The kind
    /// numbers are the shim Variant's marshalling tags (see Variant.cs).</summary>
    private static void EmitVariantPayloadRegistrations(CppEmitter emitter, StringBuilder sb)
    {
        foreach (var cls in emitter.Classes)
        {
            int? kind = cls.FullName switch
            {
                "Godot.Rect2" => Vk.Rect2,
                "Godot.Plane" => Vk.Plane,
                "Godot.AABB" => Vk.Aabb,
                "Godot.Quaternion" => Vk.Quaternion,
                "Godot.Transform2D" => Vk.Transform2D,
                "Godot.Basis" => Vk.Basis,
                "Godot.Transform3D" => Vk.Transform3D,
                "Godot.Projection" => Vk.Projection,
                // The engine-backed container wrappers: the codec allocates one
                // around a decoded engine Array/Dictionary (finalizer-owned).
                // Typed Array<T> specializations decode as the untyped base.
                "Godot.Collections.Array" => Vk.GodotArray,
                "Godot.Collections.Dictionary" => Vk.GodotDictionary,
                // Callable/Signal payloads decode to a boxed shim struct.
                "Godot.Callable" => Vk.Callable,
                "Godot.Signal" => Vk.Signal,
                // Not a Variant payload kind of its own: this slot registers the
                // Godot.Object shim so the runtime can wrap a decoded standard
                // Callable's target / Signal's owner as a borrowed shim.
                "Godot.Object" => Vk.ObjectShimSlot,
                _ => null,
            };
            if (kind is { } k && emitter.IsEmitted(cls))
                sb.AppendLine($"    dn2cpp_godot_variant_register_type({k}, {emitter.TypeInfoRef(cls, "Godot Variant payload type registration")});");
        }

        foreach (var (key, elem) in emitter.ArrayElementTypes.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (VariantPackedKindOf(elem) is { } kind)
                sb.AppendLine($"    dn2cpp_godot_variant_register_type({kind}, &ti_arr_{key});");
        }
    }

    /// <summary>The shim Variant's packed-array payload kind for an array element
    /// type, or null when an array of that element is no Variant payload. The shim
    /// kinds are the runtime marshalling tags shifted by the fixed offset
    /// (<c>VariantKind.Packed* == GdTag.Packed* + PackedTagOffset</c>), so this is
    /// the one shared element→tag map re-based.</summary>
    private static int? VariantPackedKindOf(TypeDesc e)
        => GdTag.PackedOfElement(e) is { } tag ? tag + Vk.PackedTagOffset : null;

    /// <summary>C# classes deriving from the Godot.Node shim become instantiable
    /// extension classes. Every engine <c>_*</c> virtual the class overrides is
    /// bridged by reverse dispatch: a per-virtual trampoline decodes the ptrcall
    /// args into their managed form, calls the override, and encodes the return.
    /// The set of virtuals and each one's precise arg/return kinds come from the
    /// engine API dump — the authoritative source the shim's C# surface flattens
    /// (String vs StringName vs NodePath, the engine enum identities).</summary>
    private static void EmitGodotNodeClasses(CppEmitter emitter, StringBuilder sb,
        Dictionary<string, GodotApi.ClassApi> all, List<GodotApi.EnumApi> globalEnums)
    {
        var deferred = new SortedSet<string>(StringComparer.Ordinal);
        var rows = new List<string>();
        // Snapshotted for the same reason as EmitEpilogue's walk: the .ctor probe below
        // reads a signature, and emitter.Classes is the live Compilation.Classes.
        foreach (var cls in emitter.Classes.ToList())
        {
            if (!IsExportedGodotClass(cls))
                continue;
            var engineBase = GodotExportParent(cls)!;
            string engineName = EngineAncestorName(cls, all)!;

            var ctor = cls.Methods.FirstOrDefault(m =>
                m.Name == ".ctor" && !m.IsStatic && m.Signature.ParameterTypes.Length == 0 && m.Rva != 0);

            // Every engine `_*` virtual (dump `is_virtual`) this class overrides,
            // bridged generically. _Notification is not among them — it is not a
            // ptrcall virtual — and keeps its dedicated slot below.
            var virtRows = new List<string>();
            foreach (var v in CollectEngineVirtuals(engineName, all))
            {
                var ov = ResolveVirtualOverride(cls, v.Pascal, v.RetType, v.ArgTypes, all, globalEnums);
                if (ov is null)
                    continue; // only the shim's empty default body exists — not overridden

                var retGd = MapReturnKind(v.RetType, all, globalEnums);
                var argGds = v.ArgTypes.Select(t => GodotApi.MapTypeToGd(t, all, globalEnums)).ToList();
                if (retGd is null || !ReverseRetSupported(retGd.Value)
                    || argGds.Any(a => a is null || !ReverseArgSupported(a.Value)))
                {
                    string shape = $"{v.RetType} <- ({string.Join(", ", v.ArgTypes)})";
                    deferred.Add($"{engineName}.{v.Pascal}  [{shape}]");
                    Console.Error.WriteLine(
                        $"warning: Godot virtual {cls.Name}.{v.Pascal} overridden but not bridged (deferred reverse shape: {shape})");
                    continue;
                }

                string fn = EmitVirtualTrampoline(emitter, sb, cls, ov, v.Pascal, v.ArgTypes.Count,
                    retGd.Value, argGds.Select(a => a!.Value).ToList(), emitter.HotUpdateBase);
                virtRows.Add($"    {{ \"{v.Snake}\", &{fn} }},");
            }

            var notification = ResolveVirtualOverride(cls, "_Notification", 1);

            // Extract signals and emit their static tables
            var signals = GetSignals(cls, emitter.SigProvider);
            string signalsTableName = "nullptr";
            if (signals.Count > 0)
            {
                signalsTableName = $"signals_for_{cls.CppName}";
                sb.AppendLine($"static const Dn2CppGodotSignal {signalsTableName}[] =");
                sb.AppendLine("{");
                foreach (var sig in signals)
                {
                    string argTypesStr = string.Join(", ", sig.ArgGds);
                    if (sig.ArgGds.Count > 0)
                    {
                        sb.AppendLine($"    {{ \"{sig.Name}\", {sig.ArgGds.Count}, {{ {argTypesStr} }} }},");
                    }
                    else
                    {
                        sb.AppendLine($"    {{ \"{sig.Name}\", 0, {{}} }},");
                    }
                }
                sb.AppendLine("};");
                sb.AppendLine();
            }

            // Extract properties and emit their static tables
            var properties = GetProperties(cls);
            string propertiesTableName = "nullptr";
            if (properties.Count > 0)
            {
                propertiesTableName = $"properties_for_{cls.CppName}";
                sb.AppendLine($"static const Dn2CppGodotProperty {propertiesTableName}[] =");
                sb.AppendLine("{");
                foreach (var prop in properties)
                {
                    string getterStr = string.IsNullOrEmpty(prop.Getter) ? "nullptr" : $"\"{prop.Getter}\"";
                    string setterStr = string.IsNullOrEmpty(prop.Setter) ? "nullptr" : $"\"{prop.Setter}\"";
                    sb.AppendLine($"    {{ \"{prop.Name}\", {prop.GdType}, {getterStr}, {setterStr} }},");
                }
                sb.AppendLine("};");
                sb.AppendLine();
            }

            string virtualsTableName = "nullptr";
            if (virtRows.Count > 0)
            {
                virtualsTableName = $"virtuals_for_{cls.CppName}";
                sb.AppendLine($"static const Dn2CppGodotVirtual {virtualsTableName}[] =");
                sb.AppendLine("{");
                foreach (var vr in virtRows)
                    sb.AppendLine(vr);
                sb.AppendLine("};");
                sb.AppendLine();
            }

            // `nullptr` here means the class genuinely HAS no such member (no
            // parameterless ctor, no _Notification override), never "it has one whose
            // body this emission dropped" — RequireDefinedBodySymbol fails the transpile
            // on the second case, which would otherwise ship as a dead registration slot.
            string ctorExpr = ctor is null ? "nullptr"
                : $"(void (*)(Dn2CppObject*))&{emitter.RequireDefinedBodySymbol(ctor, $"Godot ClassDB ctor slot for {cls.Name}")}";
            string notificationExpr = notification is null ? "nullptr"
                : $"(void (*)(Dn2CppObject*, int32_t))&{emitter.RequireDefinedBodySymbol(notification, $"Godot ClassDB notification slot for {cls.Name}")}";
            // The ClassDB parent: an engine shim's engine name (via the same
            // singleton-suffix strip as engineName above — a registration under
            // `ProjectSettingsInstance` would name a class ClassDB does not have), or
            // an exported user class's own registered name.
            rows.Add($"    {{ \"{cls.Name}\", \"{EngineClassName(engineBase, all)}\", {emitter.TypeInfoRef(cls, "Godot ClassDB registration row")}, (int32_t)sizeof({cls.CppStructName}), {ctorExpr}, {notificationExpr}, {virtualsTableName}, {virtRows.Count}, {signalsTableName}, {signals.Count}, {properties.Count}, {propertiesTableName}, {(IsRefCountedFamily(cls) ? 1 : 0)} }},");
        }

        if (deferred.Count > 0)
        {
            Console.Error.WriteLine(
                $"note: {deferred.Count} overridden Godot virtual(s) left unbridged (deferred reverse-marshalling shapes — container/Callable/Signal/object-return/unmodelable-pointer):");
            foreach (var d in deferred)
                Console.Error.WriteLine($"  - {d}");
        }

        sb.AppendLine("// ---- Godot node classes ----");
        sb.AppendLine("const Dn2CppGodotNodeClass dn2cpp_godot_node_classes[] =");
        sb.AppendLine("{");
        if (rows.Count == 0)
            sb.AppendLine("    { nullptr, nullptr, nullptr, 0, nullptr, nullptr, nullptr, 0, nullptr, 0, 0, nullptr, 0 },");
        foreach (var row in rows)
            sb.AppendLine(row);
        sb.AppendLine("};");
        sb.AppendLine($"const int dn2cpp_godot_node_class_count = {rows.Count};");
        sb.AppendLine();
    }

    /// <summary>The ClassDB name of a class the engine knows: a <c>Godot</c>-namespace
    /// shim's own engine class name, or an exported user class's registered name.
    ///
    /// <para>A singleton's ClassDB class is generated as <c>&lt;Name&gt;Instance</c> —
    /// the engine name belongs to its static facade (see
    /// <see cref="GodotApi.InstanceCSharpName"/>) — so the suffix is stripped back here.
    /// That is what keeps the C#-name → engine-name mapping <b>total</b>: without it, a
    /// class deriving from one would be registered with ClassDB, and dispatched against,
    /// under a name the engine has never heard of.</para></summary>
    private static string EngineClassName(ClassInfo cls, Dictionary<string, GodotApi.ClassApi> all)
    {
        const string suffix = "Instance";
        if (cls.Namespace == "Godot" && cls.Name.EndsWith(suffix, StringComparison.Ordinal)
            && all.TryGetValue(cls.Name.Substring(0, cls.Name.Length - suffix.Length), out var engine)
            && engine.IsSingleton)
        {
            return engine.Name;
        }
        return cls.Name;
    }

    /// <summary>The nearest engine (namespace <c>Godot</c>) ancestor's class name —
    /// the engine class whose virtual surface an exported user class inherits.</summary>
    private static string? EngineAncestorName(ClassInfo cls, Dictionary<string, GodotApi.ClassApi> all)
    {
        for (var c = cls; c is not null; c = c.BaseClass)
        {
            if (c.Namespace == "Godot")
                return EngineClassName(c, all);
        }
        return null;
    }

    /// <summary>Every engine <c>_*</c> virtual (<c>is_virtual</c>, non-vararg)
    /// available to override on <paramref name="engineName"/>, walking its API-dump
    /// inheritance chain. Keyed by (PascalCase name, arity): the most-derived
    /// declaration wins, and the pair is unique within one chain, so it matches a
    /// C# override by name + parameter count.</summary>
    private static List<(string Snake, string Pascal, string RetType, List<string> ArgTypes)> CollectEngineVirtuals(
        string engineName, Dictionary<string, GodotApi.ClassApi> all)
    {
        var result = new List<(string, string, string, List<string>)>();
        var seen = new HashSet<(string, int)>();
        for (string? cn = engineName; !string.IsNullOrEmpty(cn) && all.TryGetValue(cn, out var capi); cn = capi.Inherits)
        {
            foreach (var m in capi.Methods ?? new List<GodotApi.MethodApi>())
            {
                if (!m.IsVirtual || m.IsVararg)
                    continue;
                string pascal = GodotApi.ToPascalCase(m.Name);
                int arity = m.Arguments?.Count ?? 0;
                if (!seen.Add((pascal, arity)))
                    continue;
                string ret = m.ReturnValue?.Type ?? "void";
                var args = (m.Arguments ?? new List<GodotApi.ArgumentApi>()).Select(a => a.Type).ToList();
                result.Add((m.Name, pascal, ret, args));
            }
        }
        return result;
    }

    /// <summary>The user override of an engine virtual identified by PascalCase
    /// name + arity, searching from <paramref name="cls"/> up to (but not into) the
    /// Godot shim — so the shim's empty default is never mistaken for an override.
    /// Returns null when the class does not override it.</summary>
    private static MethodInfo? ResolveVirtualOverride(ClassInfo cls, string pascal, int arity)
    {
        for (var c = cls; c is not null && c.Namespace != "Godot"; c = c.BaseClass)
        {
            var m = c.Methods.FirstOrDefault(x => !x.IsStatic && x.Rva != 0
                && x.Name == pascal && x.Signature.ParameterTypes.Length == arity);
            if (m is not null)
                return m;
        }
        return null;
    }

    /// <summary>The user override of an engine virtual, matched by PascalCase
    /// name + arity <b>and</b> the return/parameter types against the engine
    /// signature — so an unrelated same-name method (e.g. a user
    /// <c>_Process(int)</c> next to the engine's <c>_process(double)</c>) is
    /// never bound and mis-decoded. A name/arity match whose types differ is
    /// warned about and treated as a plain method.</summary>
    private static MethodInfo? ResolveVirtualOverride(ClassInfo cls, string pascal,
        string engineRetType, List<string> engineArgTypes,
        Dictionary<string, GodotApi.ClassApi> all, List<GodotApi.EnumApi> globalEnums)
    {
        for (var c = cls; c is not null && c.Namespace != "Godot"; c = c.BaseClass)
        {
            foreach (var m in c.Methods)
            {
                if (m.IsStatic || m.Rva == 0 || m.Name != pascal
                    || m.Signature.ParameterTypes.Length != engineArgTypes.Count)
                    continue;
                if (IsVirtualOverrideMatch(m, engineRetType, engineArgTypes, all, globalEnums))
                    return m;
                Console.Error.WriteLine(
                    $"warning: {c.Name}.{pascal} matches engine virtual {pascal} by name and arity "
                    + "but not by signature types; treating it as a plain method");
            }
        }
        return null;
    }

    /// <summary>Whether <paramref name="m"/> is the C# override of the engine
    /// virtual with the given return/argument engine types: name and arity are
    /// assumed pre-checked by the caller for the arguments; the return and each
    /// parameter must map to the same C# surface the shim generator emits (an
    /// actual <c>override</c> matches it exactly by C# rules).</summary>
    private static bool IsVirtualOverrideMatch(MethodInfo m, string engineRetType,
        List<string> engineArgTypes,
        Dictionary<string, GodotApi.ClassApi> all, List<GodotApi.EnumApi> globalEnums)
    {
        if (!TypeMatchesEngineType(m.Signature.ReturnType, engineRetType, all, globalEnums))
            return false;
        for (int i = 0; i < engineArgTypes.Count; i++)
        {
            if (!TypeMatchesEngineType(m.Signature.ParameterTypes[i], engineArgTypes[i], all, globalEnums))
                return false;
        }
        return true;
    }

    /// <summary>Whether a managed signature type corresponds to an engine API
    /// type, mirroring the C# surface <see cref="GodotApi.MapTypeToCSharp"/>
    /// generates (<c>int</c> → <c>long</c>, <c>float</c> → <c>double</c>, the
    /// string family → <c>string</c>, …). Typed arrays match any Array wrapper
    /// (the element conversion is keyed off the instantiation at emit time);
    /// enums match any int32-backed enum type.</summary>
    private static bool TypeMatchesEngineType(TypeDesc t, string engineType,
        Dictionary<string, GodotApi.ClassApi> all, List<GodotApi.EnumApi> globalEnums)
    {
        switch (engineType)
        {
            case "void":
                return t.IsVoid;
            case "bool":
                return t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean };
            case "int":
                return t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 };
            case "float":
                return t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double };
            case "String" or "StringName" or "NodePath":
                return t.IsString;
            case "Variant":
                return t.Class?.FullName == "Godot.Variant";
            case "RID":
                return t.Class?.FullName == "Godot.Rid";
            case "Array":
                return t.Class is { Namespace: "Godot.Collections", Name: "Array" };
            case "Dictionary":
                return t.Class is { Namespace: "Godot.Collections", Name: "Dictionary" };
            case "Callable":
                return t.Class?.FullName == "Godot.Callable";
            case "Signal":
                return t.Class?.FullName == "Godot.Signal";
        }
        if (GodotApi.PackedArrayTypes.ContainsKey(engineType))
            return t.Kind == TypeKind.SZArray && t.Element is { } e
                && PackedElementMatches(e, engineType);
        if (GodotApi.TypedArrayElement(engineType) is not null)
            return t.Class is { Namespace: "Godot.Collections" } ac
                && (ac.Name == "Array" || ac.Name.StartsWith("Array_", StringComparison.Ordinal));
        if (GodotApi.GeneratedBuiltins.Contains(engineType))
            return t.Class?.FullName == "Godot." + engineType;
        if (all.ContainsKey(engineType))
            return t.Class is { Namespace: "Godot" } oc && oc.Name == engineType;
        if (GodotApi.TryResolveEnum(engineType, all, globalEnums, out _, out _))
            return t.Class?.IsEnum == true;
        return false;
    }

    /// <summary>Whether a managed array element type matches a
    /// <c>Packed*Array</c> engine type's element (the GodotSharp surface:
    /// <c>PackedByteArray</c> ↔ <c>byte[]</c>, …) — a tag-equality test over the
    /// shared element→tag and engine-name→tag maps.</summary>
    private static bool PackedElementMatches(TypeDesc e, string packedType)
        => GdTag.PackedOfElement(e) is { } tag && GdTag.PackedOfEngineType(packedType) == tag;

    /// <summary>The methods of <paramref name="cls"/> (its own declarations)
    /// that resolve as engine callbacks: type-matched overrides of the engine
    /// ancestor's <c>_*</c> virtuals, plus <c>_Notification(int)</c> (bridged
    /// through the dedicated notification slot).</summary>
    private static HashSet<MethodInfo> EngineVirtualOverridesOf(ClassInfo cls,
        Dictionary<string, GodotApi.ClassApi> all, List<GodotApi.EnumApi> globalEnums)
    {
        var set = new HashSet<MethodInfo>();
        if (EngineAncestorName(cls, all) is not { } engineName)
            return set;

        foreach (var (_, pascal, retType, argTypes) in CollectEngineVirtuals(engineName, all))
        {
            foreach (var m in cls.Methods)
            {
                if (!m.IsStatic && m.Rva != 0 && m.Name == pascal
                    && m.Signature.ParameterTypes.Length == argTypes.Count
                    && IsVirtualOverrideMatch(m, retType, argTypes, all, globalEnums))
                {
                    set.Add(m);
                }
            }
        }
        foreach (var m in cls.Methods)
        {
            if (!m.IsStatic && m.Rva != 0 && m.Name == "_Notification"
                && m.Signature.ParameterTypes.Length == 1)
            {
                set.Add(m);
            }
        }
        return set;
    }

    private static GdKind? MapReturnKind(string retType, Dictionary<string, GodotApi.ClassApi> all, List<GodotApi.EnumApi> globalEnums)
        => retType == "void" ? GdKind.Void : GodotApi.MapTypeToGd(retType, all, globalEnums);

    /// <summary>The reverse-marshalling shapes a virtual <b>argument</b> can be
    /// decoded from (engine → managed). Containers, Callable and Signal are deferred
    /// (the borrowed-container / shim-struct reverse construction is not modelled
    /// yet); their virtuals stay overridable in C# but the engine won't dispatch.</summary>
    private static bool ReverseArgSupported(GdKind k) => k switch
    {
        GdKind.Bool or GdKind.Int or GdKind.Float or GdKind.Enum or GdKind.Vector2
            or GdKind.Pod or GdKind.GdObject or GdKind.String or GdKind.StringName
            or GdKind.NodePath or GdKind.Variant or GdKind.Packed => true,
        _ => false,
    };

    /// <summary>The reverse-marshalling shapes a virtual <b>return</b> can be
    /// encoded into (managed → engine). Object returns are deferred alongside the
    /// containers/Callable/Signal (a RefCounted return would need the +1-reference
    /// ptrcall-Ref ownership handled on the callee side).</summary>
    private static bool ReverseRetSupported(GdKind k) => k switch
    {
        GdKind.Void or GdKind.Bool or GdKind.Int or GdKind.Float or GdKind.Enum
            or GdKind.Vector2 or GdKind.Pod or GdKind.String or GdKind.StringName
            or GdKind.NodePath or GdKind.Variant or GdKind.Packed => true,
        _ => false,
    };

    /// <summary>Emits the reverse-dispatch trampoline for one overridden engine
    /// virtual: decode each ptrcall arg into its managed form, call the override,
    /// encode the return into r_ret. Returns the emitted function name.
    /// In a hot-update base build the inner call goes through the receiver's
    /// vtable slot instead of a direct static call: a patch subclass instance
    /// carries a rewritten vtable copy whose slot holds an N2M trampoline into
    /// the interpreter, so engine-driven virtual dispatch reaches interpreted
    /// overrides; for an AOT-only receiver the slot holds the same most-derived
    /// override the direct call named (one extra indirection, same semantics).</summary>
    private static string EmitVirtualTrampoline(CppEmitter emitter, StringBuilder sb, ClassInfo cls,
        MethodInfo ov, string pascal, int arity, GdKind retGd, List<GdKind> argGds, bool vtableRoute)
    {
        string fn = $"gwv_{cls.CppName}__{pascal}_{arity}";
        sb.AppendLine($"static void {fn}(Dn2CppObject* self, [[maybe_unused]] const void* const* p_args, [[maybe_unused]] void* r_ret)");
        sb.AppendLine("{");
        var callArgs = new List<string> { $"({ov.DeclaringClass.CppStructName}*)self" };
        for (int i = 0; i < arity; i++)
            callArgs.Add(DecodeVirtualArg(emitter, sb, i, argGds[i], ov.Signature.ParameterTypes[i]));
        string call;
        if (vtableRoute && ov.IsVirtual && ov.VtableSlot >= 0)
        {
            // The slot's C++ ABI, exactly as an AOT callvirt site renders it
            // (and as the N2M trampoline for this (slot, shape) is emitted).
            string retC = ov.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(ov.Signature.ReturnType);
            var paramCs = new List<string> { ov.DeclaringClass.CppStructName + "*" };
            paramCs.AddRange(ov.Signature.ParameterTypes.Select(CppTypes.Of));
            // No method symbol is spelled here — the slot is read at run time — so this arm
            // names nothing the C++ compiler could fail to declare, and RenderVtable's own
            // Reachable guard already decides what the slot holds. Only the direct arm below
            // has to ask whether a body exists.
            call = $"(({retC} (*)({string.Join(", ", paramCs)}))(self->type->vtable[{ov.VtableSlot}]))({string.Join(", ", callArgs)})";
        }
        else
        {
            string callee = emitter.RequireDefinedBodySymbol(
                ov, $"Godot engine-virtual bridge {cls.Name}.{pascal}");
            call = $"{callee}({string.Join(", ", callArgs)})";
        }
        EmitVirtualRet(sb, retGd, ov.Signature.ReturnType, call);
        sb.AppendLine("}");
        sb.AppendLine();
        return fn;
    }

    /// <summary>Emits the decode of virtual arg <paramref name="i"/> from its native
    /// ptrcall encoding at <c>p_args[i]</c> into the managed representation (the
    /// mirror of the forward ptrcall arg encoding), and returns the C++ expression
    /// to pass to the override. May append setup statements to <paramref name="sb"/>.</summary>
    private static string DecodeVirtualArg(CppEmitter emitter, StringBuilder sb, int i, GdKind gd, TypeDesc pt)
    {
        string cpp = CppTypes.Of(pt);
        switch (gd)
        {
            case GdKind.Bool:
                return $"(int32_t)(*(const uint8_t*)p_args[{i}])";
            case GdKind.Int:
                return $"(int64_t)(*(const int64_t*)p_args[{i}])";
            case GdKind.Float:
                return $"(*(const double*)p_args[{i}])";
            // ptrcall passes enum/bitfield args as 64-bit ints; the generated C#
            // enums are int32-backed.
            case GdKind.Enum:
                return $"(int32_t)(*(const int64_t*)p_args[{i}])";
            case GdKind.Vector2:
            case GdKind.Pod:
            {
                if (GdTag.TransposeMapOf(pt) is { } map)
                {
                    string loc = $"_a{i}";
                    var s = new StringBuilder($"    {cpp} {loc}; {{ float* _p = (float*)&{loc}; const float* _s = (const float*)p_args[{i}];");
                    for (int k = 0; k < map.Length; k++)
                        s.Append($" _p[{k}] = _s[{map[k]}];");
                    s.Append(" }");
                    sb.AppendLine(s.ToString());
                    return loc;
                }
                // Identity-layout POD (and Vector2): the shim struct matches the
                // native ptrcall storage, so read it straight through.
                return $"(*(const {cpp}*)p_args[{i}])";
            }
            case GdKind.GdObject:
            {
                // The engine passes a pointer to the borrowed engine object pointer;
                // wrap it in a fresh managed shim of the override's parameter type
                // (the input-virtual pattern), whose handle drives further calls.
                var pc = pt.Class!;
                string loc = $"_a{i}";
                sb.AppendLine($"    {cpp} {loc} = ({cpp})dn2cpp_godot_wrap_borrowed_object("
                    + $"{emitter.TypeInfoRef(pc, "Godot engine-virtual borrowed-object argument")}, *(void* const*)p_args[{i}]);");
                return loc;
            }
            case GdKind.String:
                return $"dn2cpp_godot_decode_str(p_args[{i}])";
            case GdKind.StringName:
                return $"dn2cpp_godot_decode_strname(p_args[{i}])";
            case GdKind.NodePath:
                return $"dn2cpp_godot_decode_nodepath(p_args[{i}])";
            case GdKind.Variant:
            {
                // The engine Variant decodes into the runtime POD, copied field-wise
                // into the shim Variant struct via CopyPodToShim (the shared POD→shim
                // codec direction), so a layout drift is a compile error rather than
                // silent corruption — a reinterpret_cast of the POD would not be.
                string loc = $"_a{i}";
                string pod = $"_ap{i}";
                sb.AppendLine($"    Dn2CppGodotVariant {pod}; dn2cpp_godot_decode_variant(p_args[{i}], &{pod});");
                sb.AppendLine($"    {cpp} {loc}; {GodotCallIntrinsics.CopyPodToShim(loc, pod)}");
                return loc;
            }
            case GdKind.Packed:
            {
                int tag = GdTypeOf(pt)!.Value;
                return $"({cpp})dn2cpp_godot_decode_packed({tag}, p_args[{i}])";
            }
            default:
                throw new NotSupportedException($"reverse virtual arg kind {gd} is not supported");
        }
    }

    /// <summary>Emits the call + the encode of its return into <c>r_ret</c> in the
    /// native ptrcall return encoding (the mirror of the forward return readback).</summary>
    private static void EmitVirtualRet(StringBuilder sb, GdKind gd, TypeDesc rt, string call)
    {
        string cpp = CppTypes.Of(rt);
        switch (gd)
        {
            case GdKind.Void:
                sb.AppendLine($"    {call};");
                return;
            case GdKind.Bool:
                sb.AppendLine($"    *(uint8_t*)r_ret = (uint8_t)({call});");
                return;
            case GdKind.Int:
                sb.AppendLine($"    *(int64_t*)r_ret = (int64_t)({call});");
                return;
            case GdKind.Float:
                sb.AppendLine($"    *(double*)r_ret = (double)({call});");
                return;
            case GdKind.Enum:
                sb.AppendLine($"    *(int64_t*)r_ret = (int64_t)({call});");
                return;
            case GdKind.Vector2:
            case GdKind.Pod:
            {
                if (GdTag.TransposeMapOf(rt) is { } map)
                {
                    var s = new StringBuilder($"    {{ {cpp} _rv = {call}; const float* _p = (const float*)&_rv; float* _d = (float*)r_ret;");
                    for (int j = 0; j < map.Length; j++)
                        s.Append($" _d[{j}] = _p[{map[j]}];");
                    s.Append(" }");
                    sb.AppendLine(s.ToString());
                    return;
                }
                sb.AppendLine($"    {{ {cpp} _rv = {call}; *reinterpret_cast<{cpp}*>(r_ret) = _rv; }}");
                return;
            }
            case GdKind.String:
                sb.AppendLine($"    dn2cpp_godot_encode_str(r_ret, {call});");
                return;
            case GdKind.StringName:
                sb.AppendLine($"    dn2cpp_godot_encode_strname(r_ret, {call});");
                return;
            case GdKind.NodePath:
                sb.AppendLine($"    dn2cpp_godot_encode_nodepath(r_ret, {call});");
                return;
            case GdKind.Variant:
                sb.AppendLine($"    {{ {cpp} _rv = {call}; dn2cpp_godot_encode_variant(r_ret, reinterpret_cast<const Dn2CppGodotVariant*>(&_rv)); }}");
                return;
            case GdKind.Packed:
            {
                int tag = GdTypeOf(rt)!.Value;
                sb.AppendLine($"    {{ {cpp} _rv = {call}; dn2cpp_godot_encode_packed({tag}, r_ret, (void*)_rv); }}");
                return;
            }
            default:
                throw new NotSupportedException($"reverse virtual ret kind {gd} is not supported");
        }
    }

    private static List<(string Name, List<int> ArgGds)> GetSignals(ClassInfo cls, SignatureProvider sigProvider)
    {
        var list = new List<(string Name, List<int> ArgGds)>();
        var reader = cls.Module.Reader;

        // The [Signal] delegates are nested types of the exported class; ask the
        // class for them (same nested-typedef row order as a full-table scan —
        // the NestedClass table is sorted by the nested row id).
        foreach (var tdh in reader.GetTypeDefinition(cls.Handle).GetNestedTypes())
        {
            var td = reader.GetTypeDefinition(tdh);
            if (HasSignalAttribute(reader, td))
            {
                string name = reader.GetString(td.Name);
                if (name.EndsWith("EventHandler"))
                    name = name.Substring(0, name.Length - "EventHandler".Length);

                var argGds = new List<int>();
                foreach (var methHandle in td.GetMethods())
                {
                    var md = reader.GetMethodDefinition(methHandle);
                    if (reader.GetString(md.Name) == "Invoke")
                    {
                        var sig = md.DecodeSignature(sigProvider, GenericContext.Empty);
                        foreach (var p in sig.ParameterTypes)
                        {
                            int? gdType = GdTypeOf(p);
                            if (gdType is null || gdType == GdTag.Void)
                            {
                                throw new NotSupportedException($"Signal {cls.Name}.{name} has unsupported argument type: {p}");
                            }
                            argGds.Add(gdType.Value);
                        }
                        break;
                    }
                }
                list.Add((name, argGds));
            }
        }
        return list;
    }

    /// <summary>The [Export] properties registered for <paramref name="cls"/>. An
    /// exported property that cannot be registered WARNS (stderr, the same
    /// "skipping Godot export" channel/format the method-export filter uses) rather
    /// than vanishing from the inspector with no diagnostic. A registered property
    /// whose setter will not survive as an exported method bind (non-public, bodyless,
    /// unsupported shape) also warns: the property table names the setter by string,
    /// so the failure would otherwise surface only when the engine's first write goes
    /// nowhere.</summary>
    private static List<(string Name, int GdType, string Getter, string Setter)> GetProperties(ClassInfo cls)
    {
        var list = new List<(string Name, int GdType, string Getter, string Setter)>();
        var reader = cls.Module.Reader;
        var td = reader.GetTypeDefinition(cls.Handle);

        foreach (var propHandle in td.GetProperties())
        {
            var pd = reader.GetPropertyDefinition(propHandle);
            if (!HasExportAttribute(reader, pd)) continue;

            string name = reader.GetString(pd.Name);
            var accessors = pd.GetAccessors();
            string getterName = accessors.Getter.IsNil ? "" : reader.GetString(reader.GetMethodDefinition(accessors.Getter).Name);
            string setterName = accessors.Setter.IsNil ? "" : reader.GetString(reader.GetMethodDefinition(accessors.Setter).Name);

            if (accessors.Getter.IsNil)
            {
                Console.Error.WriteLine($"warning: skipping Godot export {cls.Name}.{name} (no getter)");
                continue;
            }
            var getter = cls.Methods.FirstOrDefault(m => m.Name == getterName);
            if (getter is null)
            {
                Console.Error.WriteLine($"warning: skipping Godot export {cls.Name}.{name} (getter {getterName} not found)");
                continue;
            }
            int? gdType = GdTypeOf(getter.Signature.ReturnType);
            if (gdType is null)
            {
                Console.Error.WriteLine($"warning: skipping Godot export {cls.Name}.{name} (unsupported property type)");
                continue;
            }

            if (setterName.Length > 0 && !SetterSurvivesExport(cls, setterName))
            {
                Console.Error.WriteLine(
                    $"warning: Godot export {cls.Name}.{name}: setter {setterName} is not an exported method bind (engine writes will fail)");
            }

            list.Add((name, gdType.Value, getterName, setterName));
        }
        return list;
    }

    /// <summary>Whether a property's setter passes the same filter the
    /// method-export loop applies (public, has a body, one supported non-void
    /// argument) — i.e. whether the name the property table carries will actually
    /// resolve to a registered bind at run time.</summary>
    private static bool SetterSurvivesExport(ClassInfo cls, string setterName)
    {
        var setter = cls.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == setterName);
        if (setter is null || !setter.IsPublic || setter.Rva == 0)
            return false;
        var ps = setter.Signature.ParameterTypes;
        return ps.Length == 1 && GdTypeOf(ps[0]) is { } arg && arg != GdTag.Void;
    }

    private static bool HasExportAttribute(MetadataReader reader, PropertyDefinition pd)
    {
        foreach (var cah in pd.GetCustomAttributes())
        {
            string? attr = Compilation.AttributeTypeName(reader, reader.GetCustomAttribute(cah));
            if (attr == "Godot.ExportAttribute" || attr == "Godot.Export")
                return true;
        }
        return false;
    }

    private static bool HasSignalAttribute(MetadataReader reader, TypeDefinition td)
    {
        foreach (var cah in td.GetCustomAttributes())
        {
            string? attr = Compilation.AttributeTypeName(reader, reader.GetCustomAttribute(cah));
            if (attr == "Godot.SignalAttribute" || attr == "Godot.Signal")
                return true;
        }
        return false;
    }

    /// <summary>Nearest base class that models an engine type (namespace Godot,
    /// e.g. the Godot.Node shim) or an exported user class. This sets the parent
    /// in the Godot ClassDB correctly.</summary>
    internal static ClassInfo? GodotExportParent(ClassInfo cls)
    {
        for (var c = cls.Namespace == "Godot" ? cls : cls.BaseClass; c is not null; c = c.BaseClass)
        {
            if (c.Namespace == "Godot")
                return c;

            if (!c.IsInterface && !c.IsValueType && !c.IsEnum && !c.IsAbstract)
            {
                // If it transitively derives from Godot.Object, it's an exported user class
                for (var b = c.BaseClass; b is not null; b = b.BaseClass)
                {
                    if (b.FullName == "Godot.Object")
                        return c;
                }
            }
        }
        return null;
    }

    /// <summary>True for a C# class that ClassDB exports as an instantiable
    /// extension type (i.e. what <see cref="EmitGodotNodeClasses"/> registers).
    /// Shared with <see cref="ExternallyAllocatedClasses"/> so the reachability
    /// seed set matches the registration set exactly.</summary>
    internal static bool IsExportedGodotClass(ClassInfo cls)
        => !cls.IsInterface && !cls.IsValueType && !cls.IsEnum && !cls.IsAbstract
            && cls.Namespace != "Godot" && GodotExportParent(cls) is not null;

    /// <summary>True if <paramref name="cls"/> derives (directly or
    /// transitively) from Godot.RefCounted — the reference-counted engine
    /// object family, as opposed to Node's tree-owned lifecycle.</summary>
    internal static bool IsRefCountedFamily(ClassInfo cls)
    {
        for (var c = cls; c is not null; c = c.BaseClass)
            if (c.FullName == "Godot.RefCounted")
                return true;
        return false;
    }

    /// <summary>Maps a managed type to its Godot variant marshaling tag (the
    /// DN2CPP_GD_* enum, mirrored as <see cref="GdTag"/>), or null when
    /// unsupported.</summary>
    private static int? GdTypeOf(TypeDesc t)
    {
        if (t.IsVoid)
            return GdTag.Void;
        if (t.Kind == TypeKind.Primitive)
        {
            return t.Primitive switch
            {
                PrimitiveTypeCode.Boolean => GdTag.Bool,
                PrimitiveTypeCode.Int32 or
                PrimitiveTypeCode.Int64 => GdTag.Int,
                PrimitiveTypeCode.Single or
                PrimitiveTypeCode.Double => GdTag.Float,
                PrimitiveTypeCode.String => GdTag.String,
                _ => null,
            };
        }
        // An enum (user-defined or engine-generated): int32-backed on the
        // managed side and an INT at the export boundary — Variant has no enum
        // kind, so this is the same lowering real GodotSharp uses for an
        // enum-typed [Export] property, signal argument or exported method.
        // The wrapper's default arms carry it: the arg decode's `(int32_t).i`
        // cast narrows the slot, the return's `(int64_t)` cast widens back.
        if (t.Class?.IsEnum == true)
            return GdTag.Int;
        // A bare Godot.Variant arg/return: the engine Variant is
        // decoded into / encoded from the shim Variant struct (layout-identical to
        // the runtime's Dn2CppGodotVariant) at the export boundary, carrying the
        // payloads the codec models (nil/bool/int/float/string + Vector2/3/4/Color).
        if (t.Class?.FullName == "Godot.Variant")
            return GdTag.Variant;
        // The Godot.Collections engine-backed container wrappers (Dictionary,
        // Array and its Array<T> specializations — the mangled Array_<arg>
        // names): the slot carries the managed wrapper pointer; the runtime
        // shares the wrapper's engine container value across the boundary.
        if (t.Class is { Namespace: "Godot.Collections" } cc)
        {
            if (cc.Name == "Dictionary")
                return GdTag.Dictionary;
            if (cc.Name == "Array" || cc.Name.StartsWith("Array_", StringComparison.Ordinal))
                return GdTag.GodotArray;
        }
        if (t.Class?.FullName == "Godot.Vector2")
            return GdTag.Vector2;
        if (t.Class?.FullName == "Godot.Vector3")
            return GdTag.Vector3;
        if (t.Class?.FullName == "Godot.Vector4")
            return GdTag.Vector4;
        if (t.Class?.FullName == "Godot.Color")
            return GdTag.Color;
        if (t.Class?.FullName == "Godot.Transform2D")
            return GdTag.Transform2D;
        if (t.Class?.FullName == "Godot.Basis")
            return GdTag.Basis;
        if (t.Class?.FullName == "Godot.Transform3D")
            return GdTag.Transform3D;
        if (t.Class?.FullName == "Godot.Projection")
            return GdTag.Projection;
        // Quaternion/Plane/AABB: identity-layout float PODs riding the same
        // `big[]` slot as the types above (identity rows in BigTypeFloatMap).
        if (t.Class?.FullName == "Godot.Quaternion")
            return GdTag.Quaternion;
        if (t.Class?.FullName == "Godot.Plane")
            return GdTag.Plane;
        if (t.Class?.FullName == "Godot.AABB")
            return GdTag.Aabb;
        // Packed arrays <-> C# arrays: a single-rank array of a supported element
        // maps to the matching Packed* tag via the shared element→tag map
        // (element-wise marshalling), plus the heterogeneous Variant[] (engine
        // Array of decoded Variant elements).
        if (t.Kind == TypeKind.SZArray && t.Element is { } e)
        {
            if (GdTag.PackedOfElement(e) is { } packed)
                return packed;
            return e.Class?.FullName == "Godot.Variant" ? GdTag.Array : null;
        }
        return t.IsString ? GdTag.String : null;
    }

}
