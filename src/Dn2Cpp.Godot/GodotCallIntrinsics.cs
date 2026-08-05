using System.Reflection.Metadata;
using Gd = Dn2Cpp.Godot.GdKind;
using Vk = global::Godot.VariantKind;

namespace Dn2Cpp.Godot;

/// <summary>Maps calls on the Godot shim types (Godot.GD, Godot.Node) to the
/// GDExtension bridge intrinsics in runtime/godot/dn2cpp_godot.cpp. The engine
/// method-bind table is built at construction from <c>extension_api.json</c>
/// (the same dump <see cref="BindingGenerator"/> uses for the C# shims), so the
/// shims and this map cannot drift apart.</summary>
internal sealed class GodotCallIntrinsics : ICallIntrinsics
{
    // IsStatic: no receiver (null-instance call). IsVararg: a `params Variant[]`
    // tail after the fixed Args, routed through the variant-call interface. Args
    // holds only the fixed (declared) argument kinds in both cases.
    // Singleton: the engine singleton whose object is this call's receiver — set on
    // the entries registered under a singleton's static facade (Godot.ProjectSettings),
    // null on the ordinary entries registered under a class that takes a `this`
    // (Godot.ProjectSettingsInstance, Godot.Node). It never overrides IsStatic: an
    // `is_static` ENGINE bind reached through a facade still wants a null instance.
    private sealed record EngineMethod(string EngineClass, string EngineName, long Hash, Gd Ret, Gd[] Args,
        bool IsStatic = false, bool IsVararg = false, string? Singleton = null);

    // A builtin value-type method routed through the engine bridge. GdType/
    // RetGd/ArgGds are Dn2CppGdType tags (the runtime DN2CPP_GD_* enum); EngineName
    // is the engine snake_case method name.
    private sealed record BuiltinMethod(int GdType, string EngineName, long Hash, int RetGd, int[] ArgGds);

    // (C# shim class full name, C# method name) -> engine method-bind descriptor.
    // Keying on the declaring shim class keeps the match precise: an unrelated
    // Godot.* type with a same-named method won't be hijacked.
    private readonly Dictionary<(string ShimClass, string Method), EngineMethod> _engineMethods;

    // (C# value-type shim full name, C# method name) -> builtin-method bridge descriptor.
    private readonly Dictionary<(string ShimClass, string Method), BuiltinMethod> _builtinMethods;

    // The engine names of the singleton classes, i.e. the names of their generated
    // static facades. Drives the `<Facade>.Singleton` intercept, which is not a
    // method bind and so is not in the map above.
    private readonly HashSet<string> _singletons;

    public GodotCallIntrinsics(string extensionApiPath)
    {
        _engineMethods = BuildEngineMethods(extensionApiPath);
        _builtinMethods = BuildBuiltinMethods(extensionApiPath);
        var (objectClasses, _, _, _, _, _) = GodotApi.Load(extensionApiPath);
        _singletons = new HashSet<string>(
            objectClasses.Where(c => c.IsSingleton).Select(c => c.Name), StringComparer.Ordinal);
    }

    /// <summary>Builds the builtin-method bridge map from the curated allowlist
    /// (<see cref="GodotApi.BridgedBuiltinMethods"/>), resolving each method's engine
    /// hash + return/argument <c>DN2CPP_GD_*</c> tags from the API dump. An entry that
    /// does not resolve to a supported bridge shape is a HARD ERROR, not a drop (the
    /// reflection-root typo policy): a dropped entry still has its placeholder shim
    /// body, so every call would compile and silently return <c>default</c> — a wrong
    /// answer with no diagnostic, in a shipped game. The generator side
    /// (<c>BindingGenerator.EmitBuiltinBridgeMethods</c>) applies the same gates.</summary>
    private static Dictionary<(string, string), BuiltinMethod> BuildBuiltinMethods(string extensionApiPath)
    {
        var (_, _, _, _, allBuiltins, _) = GodotApi.Load(extensionApiPath);
        var map = new Dictionary<(string, string), BuiltinMethod>();

        foreach (var (type, method) in GodotApi.BridgedBuiltinMethods)
        {
            if (!allBuiltins.TryGetValue(type, out var bc) || bc.Methods is null)
                throw new NotSupportedException(
                    $"BridgedBuiltinMethods: builtin type {type} not in the API dump ({type}.{method})");
            var m = bc.Methods.FirstOrDefault(x => x.Name == method)
                ?? throw new NotSupportedException(
                    $"BridgedBuiltinMethods: {type} has no method {method} in the API dump");
            if (m.IsStatic)
                throw new NotSupportedException(
                    $"BridgedBuiltinMethods: {type}.{method} is static — not a bridgeable instance method");
            if (GodotApi.BridgeGdTag(type) is not { } recv)
                throw new NotSupportedException(
                    $"BridgedBuiltinMethods: receiver type {type} has no bridge tag ({type}.{method})");
            if (GodotApi.BridgeGdTag(m.ReturnType) is not { } ret)
                throw new NotSupportedException(
                    $"BridgedBuiltinMethods: return type {m.ReturnType} has no bridge tag ({type}.{method})");

            var args = m.Arguments ?? new List<GodotApi.ArgumentApi>();
            var argTags = new int[args.Count];
            for (int i = 0; i < args.Count; i++)
            {
                if (GodotApi.BridgeGdTag(args[i].Type) is not { } t || t == GdTag.Void)
                    throw new NotSupportedException(
                        $"BridgedBuiltinMethods: argument type {args[i].Type} has no bridge tag ({type}.{method})");
                argTags[i] = t;
            }

            map[($"Godot.{type}", GodotApi.ToPascalCase(method))] =
                new BuiltinMethod(recv, method, m.Hash, ret, argTags);
        }

        return map;
    }

    /// <summary>Builds the engine-call map from the API dump. Mirrors the shim
    /// surface emitted by <see cref="BindingGenerator"/>: each non-virtual engine
    /// method is registered under its declaring class, and each property maps to
    /// its <c>get_*</c>/<c>set_*</c> accessor names.
    ///
    /// <para>A singleton is registered <b>twice</b>, because the generator surfaces it
    /// as two C# types: once under its instance class
    /// (<c>Godot.ProjectSettingsInstance</c>) with the ordinary popped-receiver shape,
    /// and once under its static facade (<c>Godot.ProjectSettings</c>) carrying the
    /// singleton name, whose receiver is the engine's own singleton object.</para></summary>
    private static Dictionary<(string, string), EngineMethod> BuildEngineMethods(string extensionApiPath)
    {
        var (objectClasses, all, _, _, _, globalEnums) = GodotApi.Load(extensionApiPath);
        var map = new Dictionary<(string, string), EngineMethod>();

        foreach (var cls in objectClasses)
        {
            RegisterClass(map, cls, all, globalEnums, $"Godot.{GodotApi.InstanceCSharpName(cls)}", singleton: null);
            if (cls.IsSingleton)
                RegisterClass(map, cls, all, globalEnums, $"Godot.{cls.Name}", singleton: cls.Name);
        }

        return map;
    }

    /// <summary>Registers one engine class's binds under <paramref name="shimClass"/>.
    /// <paramref name="singleton"/> is the engine singleton supplying the receiver
    /// (the static-facade registration) or null (the ordinary instance one) — the
    /// binds themselves are identical either way, only the receiver differs.</summary>
    private static void RegisterClass(Dictionary<(string, string), EngineMethod> map, GodotApi.ClassApi cls,
        Dictionary<string, GodotApi.ClassApi> all, List<GodotApi.EnumApi> globalEnums,
        string shimClass, string? singleton)
    {
        if (cls.Methods != null)
        {
            foreach (var method in cls.Methods)
            {
                // Virtuals are the _-prefixed engine callbacks, not call targets.
                // Statics (null-instance) and varargs (variant-call tail) ARE call
                // targets — registered with their flags for the emit side.
                if (method.IsVirtual) continue;
                if (GodotApi.ExcludedMethods.Contains((cls.Name, method.Name))) continue;

                // For a vararg method the declared arguments are the fixed args
                // (the tail is implicit); TryDescribe covers them and the return.
                if (!TryDescribe(method, all, globalEnums, out var ret, out var args)) continue;

                // An `is_static` ENGINE method (ResourceUID's uid_to_path / path_to_uid /
                // ensure_path are the only three on a singleton) keeps the null-instance
                // sentinel even on the facade: the engine bind takes no receiver, and
                // handing it the singleton object would be a wrong call, not a no-op.
                map[(shimClass, GodotApi.ToPascalCase(method.Name))] =
                    new EngineMethod(cls.Name, method.Name, method.Hash, ret, args,
                        method.IsStatic, method.IsVararg, singleton);
            }
        }

        if (cls.Properties == null)
            return;

        foreach (var prop in cls.Properties)
        {
            var getter = cls.Methods?.FirstOrDefault(m => m.Name == prop.Getter);

            string? propType = GodotApi.MapTypeToCSharp(prop.Type, all, globalEnums);
            // An enum-typed property is dumped with type `int` while its
            // accessors carry the real enum type; mirror the generator, which
            // surfaces the property as the enum.
            if (prop.Type == "int" && getter?.ReturnValue is not null
                && GodotApi.TryResolveEnum(getter.ReturnValue.Type, all, globalEnums, out _, out var propEnum))
            {
                propType = propEnum;
            }
            if (propType is null) continue;
            string propName = GodotApi.ToPascalCase(prop.Name);
            if (getter != null && !getter.IsVirtual && !getter.IsStatic &&
                !GodotApi.ExcludedMethods.Contains((cls.Name, prop.Getter)) &&
                (getter.Arguments == null || getter.Arguments.Count == 0) &&
                getter.ReturnValue != null &&
                GodotApi.MapTypeToCSharp(getter.ReturnValue.Type, all, globalEnums) == propType &&
                GodotApi.MapTypeToGd(getter.ReturnValue.Type, all, globalEnums) is { } getRet)
            {
                map[(shimClass, $"get_{propName}")] =
                    new EngineMethod(cls.Name, prop.Getter, getter.Hash, getRet, Array.Empty<Gd>(),
                        Singleton: singleton);
            }

            var setter = string.IsNullOrEmpty(prop.Setter) ? null : cls.Methods?.FirstOrDefault(m => m.Name == prop.Setter);
            if (setter != null && !setter.IsVirtual && !setter.IsStatic &&
                !GodotApi.ExcludedMethods.Contains((cls.Name, prop.Setter)) &&
                setter.Arguments != null && setter.Arguments.Count == 1 &&
                GodotApi.MapTypeToCSharp(setter.Arguments[0].Type, all, globalEnums) == propType &&
                GodotApi.MapTypeToGd(setter.Arguments[0].Type, all, globalEnums) is { } setArg)
            {
                map[(shimClass, $"set_{propName}")] =
                    new EngineMethod(cls.Name, prop.Setter, setter.Hash, Gd.Void, new[] { setArg },
                        Singleton: singleton);
            }
        }
    }

    private static bool TryDescribe(GodotApi.MethodApi method, Dictionary<string, GodotApi.ClassApi> all,
        List<GodotApi.EnumApi> globalEnums, out Gd ret, out Gd[] args)
    {
        ret = Gd.Void;
        args = Array.Empty<Gd>();

        if (method.ReturnValue != null)
        {
            if (GodotApi.MapTypeToGd(method.ReturnValue.Type, all, globalEnums) is not { } r) return false;
            ret = r;
        }

        if (method.Arguments is { Count: > 0 })
        {
            var list = new Gd[method.Arguments.Count];
            for (int i = 0; i < list.Length; i++)
            {
                if (GodotApi.MapTypeToGd(method.Arguments[i].Type, all, globalEnums) is not { } a) return false;
                list[i] = a;
            }
            args = list;
        }

        return true;
    }

    public bool TryEmitCall(MethodCompiler mc, MethodInfo callee, bool isCallvirt)
    {
        // Godot.GD is a shim class; its methods map to bridge intrinsics.
        if (callee.DeclaringClass.FullName == "Godot.GD")
        {
            // GD.Print(params Variant[]) — the official signature. Decode each
            // managed Godot.Variant (the shim's field-carrying value type) into
            // the runtime's Dn2CppGodotVariant POD and route them through the
            // variadic print bridge.
            var p0 = callee.Signature.ParameterTypes.Length == 1 ? callee.Signature.ParameterTypes[0] : null;
            if (callee.Name == "Print"
                && p0 is { Kind: TypeKind.SZArray }
                && p0.Element?.Class?.FullName == "Godot.Variant")
            {
                var a = mc.Pop();
                string e = p0.Element!.Class!.CppStructName;
                mc.Emit(
                    "{ Dn2CppArrayN* _va = (Dn2CppArrayN*)(" + a.Expr + "); "
                    + "int32_t _vn = _va ? _va->length : 0; if (_vn > 64) _vn = 64; "
                    + "Dn2CppGodotVariant _vb[64]; "
                    + "for (int32_t _vi = 0; _vi < _vn; _vi++) { "
                    + e + "* _ve = &((" + e + "*)_va->data)[_vi]; "
                    + CopyShimToPod("_vb[_vi]", "(*_ve)") + " } "
                    + "dn2cpp_godot_print_variants(_vb, _vn); }");
                return true;
            }
            throw new NotSupportedException($"Godot.GD.{callee.Name} has no intrinsic mapping yet");
        }

        // Godot.Mathf (generated from extension_api.json) — the pure single/two
        // argument float functions map straight to C++ <cmath>, matching the
        // System.Math intrinsics. Unmapped Mathf functions throw loudly rather
        // than silently transpiling their `return default` shim body.
        if (callee.DeclaringClass.FullName == "Godot.Mathf")
        {
            int argc = callee.Signature.ParameterTypes.Length;
            string? unary = callee.Name switch
            {
                "Sqrt" => "std::sqrt", "Sin" => "std::sin", "Cos" => "std::cos", "Tan" => "std::tan",
                "Asin" => "std::asin", "Acos" => "std::acos", "Atan" => "std::atan",
                "Sinh" => "std::sinh", "Cosh" => "std::cosh", "Tanh" => "std::tanh",
                "Exp" => "std::exp", "Log" => "std::log", "Floor" => "std::floor", "Ceil" => "std::ceil",
                "Round" => "std::round", "Absf" => "std::fabs",
                _ => null,
            };
            if (unary is not null && argc == 1)
            {
                var a = mc.Pop();
                mc.Push(StackKind.R8, "double", $"(double){unary}((double)({a.Expr}))");
                return true;
            }
            string? binary = callee.Name switch
            {
                "Pow" => "std::pow", "Atan2" => "std::atan2", "Fmod" => "std::fmod",
                _ => null,
            };
            if (binary is not null && argc == 2)
            {
                var b = mc.Pop();
                var a = mc.Pop();
                mc.Push(StackKind.R8, "double", $"(double){binary}((double)({a.Expr}), (double)({b.Expr}))");
                return true;
            }
            throw new NotSupportedException($"Godot.Mathf.{callee.Name} has no intrinsic mapping yet");
        }

        // Special intrinsics (Node/Object level). Connect has no special case
        // anymore: the generated `connect(StringName, Callable, int)` shim
        // rides the general ptrcall path with a Callable argument.
        if (callee.DeclaringClass.FullName == "Godot.Object")
        {
            if (callee.Name == "EmitSignal"
                && callee.Signature.ParameterTypes.Length == 2
                && callee.Signature.ParameterTypes[0].IsString
                && callee.Signature.ParameterTypes[1].Kind == TypeKind.SZArray)
            {
                var args = mc.Pop();
                var signal = mc.Pop();
                var self = mc.Pop();
                string handle = $"(void*)(({callee.DeclaringClass.CppStructName}*){self.Expr})->f__handle";
                mc.Emit($"dn2cpp_godot_emit_signal({handle}, {MethodCompiler.Cast(signal, "Dn2CppString*")}, {MethodCompiler.Cast(args, "Dn2CppArrayRef*")});");
                return true;
            }
        }

        // Godot.Callable.Call(params Variant[]): build the engine Callable from
        // the shim struct and invoke through the engine's own `call` (a vararg
        // builtin method — it cannot be ptrcalled, so the runtime routes it via
        // variant_call). A delegate-backed callable re-enters managed code
        // through the custom-callable trampoline; the engine's result Variant
        // decodes into the shim Variant return.
        if (callee.DeclaringClass.FullName == "Godot.Callable" && callee.Name == "Call"
            && callee.Signature.ParameterTypes.Length == 1
            && callee.Signature.ParameterTypes[0].Kind == TypeKind.SZArray)
        {
            var argsArr = mc.Pop();
            var self = mc.Pop();
            string selfPtr = MethodCompiler.Cast(self, callee.DeclaringClass.CppStructName + "*");
            string cpod = mc.NewLocal("Dn2CppGodotCallableShim");
            mc.Emit(CopyCallableShimToPod(cpod, $"(*({selfPtr}))"));
            string vpod = mc.NewLocal("Dn2CppGodotVariant");
            mc.Emit($"dn2cpp_godot_callable_call(&{cpod}, (void*)({argsArr.Expr}), &{vpod});");
            string retCpp = CppTypes.Of(callee.Signature.ReturnType); // t_Godot_Variant
            string r = mc.NewLocal(retCpp);
            mc.Emit(CopyPodToShim(r, vpod));
            mc.Push(StackKind.Struct, retCpp, r);
            return true;
        }

        // Godot.RefCounted.__Destroy() — a hand-written bridge helper (see
        // src/GodotSharpShim/RefCounted.cs) with no native-callable body of its
        // own; the actual GDExtension object_destroy call has to be emitted here.
        if (callee.DeclaringClass.FullName == "Godot.RefCounted" && callee.Name == "__Destroy")
        {
            var self = mc.Pop();
            // Save the handle and clear the field *before* destroying: destroy can
            // synchronously reach free_instance_func -> dn2cpp_free_pinned(self),
            // after which a store to f__handle would write into a GC_FREEd block.
            string handleField = $"(({callee.DeclaringClass.CppStructName}*){self.Expr})->f__handle";
            mc.Emit($"{{ void* _h = (void*){handleField}; {handleField} = 0; dn2cpp_godot_object_destroy(_h); }}");
            return true;
        }

        // Godot.RefCounted.__DetachBinding() — another bodyless bridge helper.
        // Clears the engine→shim extension binding (object_set_instance(obj, cls,
        // null)) so a collectible C#-`new` extension instance can be reclaimed by
        // the GC without the engine's free_instance_func double-reclaiming it, and
        // without leaving a dangling instance pointer if the engine still holds a
        // reference. The runtime helper derives the class name from the shim's own
        // type-info, so this base-typed call resolves to the concrete registered
        // class at run time.
        if (callee.DeclaringClass.FullName == "Godot.RefCounted" && callee.Name == "__DetachBinding")
        {
            var self = mc.Pop();
            mc.Emit($"dn2cpp_godot_object_clear_instance((Dn2CppObject*){self.Expr});");
            return true;
        }

        // Godot.RefCounted.__TryEngineAnchor() — another bodyless bridge helper.
        // Anchors the shim in the runtime's GC-scanned strong-reference table (see
        // dn2cpp_godot_try_anchor_refcounted) so it survives collection while the
        // engine still independently references it.
        if (callee.DeclaringClass.FullName == "Godot.RefCounted" && callee.Name == "__TryEngineAnchor")
        {
            var self = mc.Pop();
            string r = mc.NewLocal("uint8_t");
            mc.Emit($"{r} = dn2cpp_godot_try_anchor_refcounted((Dn2CppObject*){self.Expr});");
            mc.Push(StackKind.I4, "int32_t", $"(int32_t){r}");
            return true;
        }

        // Godot.Collections engine-backed container wrappers (Array / Array<T> /
        // Dictionary): every surface member lowers to a runtime bridge call
        // working on the wrapper's engine container value.
        if (callee.DeclaringClass.Namespace == "Godot.Collections"
            && TryEmitCollectionCall(mc, callee))
        {
            return true;
        }

        // `<Facade>.Singleton` — the engine's own singleton object. Not a method bind,
        // so it is intercepted AHEAD of the engine-method map: it lowers to
        // global_get_singleton, not to a ptrcall. The handle is borrowed (no singleton
        // is RefCounted, so EmitWrapEngineObject registers no finalizer — the wrapper
        // is a passive view of an object the engine owns for its whole lifetime), and a
        // singleton absent from this build hands back nullptr, which wraps as a null
        // reference (the documented degrade) rather than crashing.
        if (callee.DeclaringClass.Namespace == "Godot"
            && callee.Name == "get_Singleton"
            && _singletons.Contains(callee.DeclaringClass.Name))
        {
            string h = mc.NewLocal("void*");
            mc.Emit($"{h} = dn2cpp_godot_get_singleton(\"{callee.DeclaringClass.Name}\");");
            EmitWrapEngineObject(mc, callee.Signature.ReturnType, h, callee);
            return true;
        }

        if (callee.DeclaringClass.Namespace == "Godot"
            && _engineMethods.TryGetValue((callee.DeclaringClass.FullName, callee.Name), out var em))
        {
            EmitEngineCall(mc, callee, em);
            return true;
        }

        // Builtin value-type method -> engine builtin-method bridge. The arg
        // count must match (guards against an unintended overload binding to the map).
        if (callee.DeclaringClass.Namespace == "Godot"
            && callee.DeclaringClass.IsValueType
            && _builtinMethods.TryGetValue((callee.DeclaringClass.FullName, callee.Name), out var bm)
            && callee.Signature.ParameterTypes.Length == bm.ArgGds.Length)
        {
            EmitBuiltinCall(mc, callee, bm);
            return true;
        }

        return false;
    }

    /// <summary>Intercepts `newobj` on any Godot.RefCounted-derived class: besides the
    /// managed allocation, the engine-side instance must be constructed and init_ref()
    /// run before the managed ctor body, via
    /// <c>dn2cpp_godot_construct_engine_object</c>. The ClassDB-driven
    /// <c>NodeCreateInstance</c> path shares that helper but always passes
    /// is_ref_counted=0, since ClassDB::instantiate() establishes the first reference
    /// itself; only this direct-`new` path, which the engine never wraps in a
    /// Ref&lt;T&gt;, must init_ref() itself.
    ///
    /// <para>Both a bare RefCounted and a ClassDB-registered user subclass get a normal
    /// collectible allocation plus a finalizer registration, so ~RefCounted()
    /// unreferences the engine object once the managed shim is unreachable. A registered
    /// subclass additionally binds the shim into the engine via object_set_instance;
    /// ~RefCounted() clears that binding before unreferencing (see RefCounted.cs), so
    /// the shim can never be double-reclaimed by free_instance_func nor leave a dangling
    /// instance pointer. Non-RefCounted classes fall through unchanged.</para>
    ///
    /// <para>Unlike NodeCreateInstance's pinned, engine-owned instances, a C# `new` is
    /// C#-owned. Known limitation: if such an instance is handed to the engine and every
    /// C# reference is then dropped, the GC may finalize the shim while the engine still
    /// holds the object — memory-safe (the binding is cleared) but the C# behaviour
    /// detaches.</para></summary>
    public bool TryEmitNewobj(MethodCompiler mc, MethodInfo ctor)
    {
        var cls = ctor.DeclaringClass;

        // `new Godot.Collections.Array()` / `Array<T>()` / `Dictionary()`: allocate
        // the managed wrapper and give it a fresh empty engine container value it
        // owns; the finalizer (~Array/~Dictionary -> __Destroy) destroys the value
        // once the wrapper is unreachable. The placeholder ctor body never runs.
        if ((IsArrayWrapperClass(cls) || IsDictionaryWrapperClass(cls))
            && ctor.Signature.ParameterTypes.Length == 0)
        {
            int tag = IsDictionaryWrapperClass(cls) ? GdTag.Dictionary : GdTag.GodotArray;
            string wobj = mc.NewLocal(cls.CppStructName + "*");
            mc.Emit($"{wobj} = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
            mc.Emit($"((Dn2CppObject*){wobj})->type = &{cls.CppTypeInfoName};");
            mc.Emit($"{wobj}->f__handle = (intptr_t)dn2cpp_godot_container_new({tag});");
            if (Compilation.EffectiveFinalize(cls) is not null)
                mc.Emit($"dn2cpp_register_finalizer((Dn2CppObject*){wobj});");
            mc.Push(StackKind.Ref, cls.CppStructName + "*", wobj);
            return true;
        }

        if (!GodotBackend.IsRefCountedFamily(cls))
            return false; // Node-derived / non-Godot: fall through to the generic newobj path.

        bool isRegistered = cls.Namespace != "Godot" && GodotBackend.GodotExportParent(cls) is not null;
        string engineParentClass = isRegistered ? GodotBackend.GodotExportParent(cls)!.Name : cls.Name;
        string? extensionClassName = isRegistered ? cls.Name : null;

        var ps = ctor.Signature.ParameterTypes;
        var ctorArgs = new string[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
            ctorArgs[i] = MethodCompiler.Cast(mc.Pop(), CppTypes.Of(ps[i]));

        // Collectible for both paths: a C# `new` shim is a GC root while in use, so
        // (unlike NodeCreateInstance) it need not be pinned, which is what lets the
        // finalizer run and release the engine reference instead of leaking it.
        string obj = mc.NewLocal(cls.CppStructName + "*");
        mc.Emit($"{obj} = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
        mc.Emit($"((Dn2CppObject*){obj})->type = &{cls.CppTypeInfoName};");
        string extArg = extensionClassName is null ? "nullptr" : $"\"{extensionClassName}\"";
        mc.Emit($"dn2cpp_godot_construct_engine_object((Dn2CppObject*){obj}, \"{engineParentClass}\", {extArg}, 1);");
        // Registered = object_set_instance bound the shim; flag it so ~RefCounted()
        // detaches the binding before releasing (keeps the collectible shim safe
        // against free_instance_func / a dangling engine instance pointer).
        if (isRegistered)
            mc.Emit($"{obj}->f__engineBound = 1;");
        // Register the finalizer BEFORE the ctor runs — matches the generic
        // newobj path (see MethodCompiler.Newobj.cs). If the ctor throws, the C++
        // exception unwinds past this frame, and the already-constructed engine
        // object must still be reclaimed via ~RefCounted() when the partially
        // constructed shim is collected; without pre-registration, that ctor
        // path leaks the engine object with the binding still attached.
        if (Compilation.EffectiveFinalize(cls) is not null)
            mc.Emit($"dn2cpp_register_finalizer((Dn2CppObject*){obj});");
        var allArgs = new List<string> { obj };
        allArgs.AddRange(ctorArgs);
        mc.Emit($"{ctor.CppName}({string.Join(", ", allArgs)});");
        mc.Push(StackKind.Ref, cls.CppStructName + "*", obj);
        return true;
    }

    /// <summary>Statements copying a decoded runtime <c>Dn2CppGodotVariant</c> POD
    /// (<paramref name="pod"/>) into the shim Variant struct's fields
    /// (<paramref name="shim"/>). The two are layout-identical by contract (see
    /// Variant.cs / dn2cpp_godot.h), but the emitted code copies field-wise so a
    /// drift is a compile error, not silent corruption. Internal (not private):
    /// the virtual trampoline's Variant arm (<c>GodotBackend.DecodeVirtualArg</c>,
    /// family 3) shares this exact codec direction.</summary>
    internal static string CopyPodToShim(string shim, string pod) =>
        $"{shim}.f__kind = {pod}.kind; {shim}.f__int = {pod}.i; {shim}.f__float = {pod}.f; {shim}.f__str = {pod}.s; "
        + $"{shim}.f__vx = {pod}.vx; {shim}.f__vy = {pod}.vy; {shim}.f__vz = {pod}.vz; {shim}.f__vw = {pod}.vw; "
        + $"{shim}.f__obj = {pod}.obj; {shim}.f__refGuard = {pod}.refGuard;";

    /// <summary>The inverse of <see cref="CopyPodToShim"/>: decode a shim Variant
    /// struct into a runtime POD the bridge functions consume.</summary>
    private static string CopyShimToPod(string pod, string shim) =>
        $"{pod}.kind = {shim}.f__kind; {pod}.i = {shim}.f__int; {pod}.f = {shim}.f__float; {pod}.s = {shim}.f__str; "
        + $"{pod}.vx = {shim}.f__vx; {pod}.vy = {shim}.f__vy; {pod}.vz = {shim}.f__vz; {pod}.vw = {shim}.f__vw; "
        + $"{pod}.obj = {shim}.f__obj; {pod}.refGuard = {shim}.f__refGuard;";

    /// <summary>Statements copying a shim Callable struct expression
    /// (<paramref name="shim"/>) into the runtime <c>Dn2CppGodotCallableShim</c>
    /// POD (<paramref name="pod"/>). Field-wise (the Variant codec convention),
    /// so a layout drift is a compile error, not silent corruption.</summary>
    private static string CopyCallableShimToPod(string pod, string shim) =>
        $"{pod}.target = (Dn2CppObject*){shim}.f_Target; {pod}.method = {shim}.f_Method; "
        + $"{pod}.action = (Dn2CppObject*){shim}.f__action; "
        + $"{pod}.actionVariant = (Dn2CppObject*){shim}.f__actionVariant; "
        + $"{pod}.actionVariants = (Dn2CppObject*){shim}.f__actionVariants;";

    /// <summary>The inverse of <see cref="CopyCallableShimToPod"/>: decode a
    /// runtime Callable POD into the shim struct. The per-field cast types come
    /// from the shim class's own field declarations, so they can never drift
    /// from the generated struct.</summary>
    private static string CopyPodToCallableShim(ClassInfo callableCls, string shim, string pod)
    {
        string Cast(string field) =>
            CppTypes.Of(callableCls.Fields.First(f => f.Name == field).Type);
        return $"{shim}.f_Target = ({Cast("Target")}){pod}.target; {shim}.f_Method = ({Cast("Method")}){pod}.method; "
            + $"{shim}.f__action = ({Cast("_action")}){pod}.action; "
            + $"{shim}.f__actionVariant = ({Cast("_actionVariant")}){pod}.actionVariant; "
            + $"{shim}.f__actionVariants = ({Cast("_actionVariants")}){pod}.actionVariants;";
    }

    /// <summary>Shim Signal struct → runtime <c>Dn2CppGodotSignalShim</c> POD.</summary>
    private static string CopySignalShimToPod(string pod, string shim) =>
        $"{pod}.owner = (Dn2CppObject*){shim}.f_Owner; {pod}.name = {shim}.f_Name;";

    /// <summary>Runtime Signal POD → shim Signal struct.</summary>
    private static string CopyPodToSignalShim(ClassInfo signalCls, string shim, string pod)
    {
        string Cast(string field) =>
            CppTypes.Of(signalCls.Fields.First(f => f.Name == field).Type);
        return $"{shim}.f_Owner = ({Cast("Owner")}){pod}.owner; {shim}.f_Name = ({Cast("Name")}){pod}.name;";
    }

    /// <summary>The engine-backed <c>Godot.Collections.Array</c> wrapper, or one
    /// of its generic <c>Array&lt;T&gt;</c> specializations (whose ClassInfo name
    /// is the mangled <c>Array_&lt;arg&gt;</c>).</summary>
    private static bool IsArrayWrapperClass(ClassInfo cls) =>
        cls.Namespace == "Godot.Collections"
        && (cls.Name == "Array" || cls.Name.StartsWith("Array_", StringComparison.Ordinal));

    private static bool IsDictionaryWrapperClass(ClassInfo cls) =>
        cls.Namespace == "Godot.Collections" && cls.Name == "Dictionary";

    /// <summary>Whether a class models an engine object (its base chain reaches
    /// the Godot.Object shim) — the element kinds a typed array wraps by handle.</summary>
    private static bool IsGodotObjectFamily(ClassInfo cls)
    {
        for (var c = cls; c is not null; c = c.BaseClass)
        {
            if (c.FullName == "Godot.Object")
                return true;
        }
        return false;
    }

    /// <summary>Wraps a raw engine object pointer held in <paramref name="handleLocal"/>
    /// into a fresh managed shim of the signature type <paramref name="retType"/> and
    /// pushes it (null handle wraps as a null reference). A RefCounted-family type gets
    /// a finalizer registration: by convention every producer of the handle (an engine
    /// ptrcall Ref&lt;T&gt; return, the typed-array element bridge) has already
    /// transferred exactly one engine reference to us, which ~RefCounted() returns
    /// once the wrapper is collected.</summary>
    private static void EmitWrapEngineObject(MethodCompiler mc, TypeDesc retType, string handleLocal, MethodInfo callee)
        => EmitWrapHandle(mc, retType, handleLocal, callee, refCountedOnly: true);

    /// <summary>Wraps an engine container value (a runtime-allocated pinned slot
    /// whose ownership transfers to the wrapper) into a fresh managed
    /// Array/Array&lt;T&gt;/Dictionary instance of the signature type and pushes it.
    /// The registered finalizer destroys the engine value once the wrapper is
    /// unreachable; a null handle (null receiver upstream) wraps as null.</summary>
    private static void EmitWrapContainer(MethodCompiler mc, TypeDesc retType, string handleLocal, MethodInfo callee)
        => EmitWrapHandle(mc, retType, handleLocal, callee, refCountedOnly: false);

    /// <summary>The shared alloc-stamp-plant emission behind the two wrappers
    /// above, differing only in the finalizer predicate: a container always
    /// finalizes (the wrapper owns its engine value), an engine object only for
    /// the RefCounted family (a borrowed Node handle has nothing to release).</summary>
    private static void EmitWrapHandle(MethodCompiler mc, TypeDesc retType, string handleLocal,
        MethodInfo callee, bool refCountedOnly)
    {
        var retClass = retType.Class
            ?? throw new NotSupportedException(
                $"Godot engine return has no shim class: {callee.DeclaringClass.FullName}.{callee.Name}");
        mc.NoteReferencedType(retClass);

        string retCpp = CppTypes.Of(retType);
        string o = mc.NewLocal(retCpp);
        mc.Emit($"{o} = nullptr;");
        bool finalize = (!refCountedOnly || GodotBackend.IsRefCountedFamily(retClass))
            && Compilation.EffectiveFinalize(retClass) is not null;
        string fin = finalize ? $"dn2cpp_register_finalizer((Dn2CppObject*){o}); " : "";
        mc.Emit($"if ({handleLocal} != nullptr) {{ {o} = ({retCpp})dn2cpp_alloc(sizeof({retClass.CppStructName})); "
            + $"((Dn2CppObject*){o})->type = &{retClass.CppTypeInfoName}; "
            + $"{o}->f__handle = (intptr_t){handleLocal}; "
            + fin
            + "}");
        mc.Push(StackKind.Ref, retCpp, o);
    }

    /// <summary>Lowers a call on the Godot.Collections wrapper surface (Array /
    /// Array&lt;T&gt; / Dictionary) to its runtime bridge. Element values convert
    /// through the runtime Variant POD; a typed array's engine-object elements
    /// take the dedicated handle bridge instead (reference transfer, matching
    /// the engine-object return convention). Returns false for a non-wrapper
    /// class or an unrecognized member (which then compiles normally).</summary>
    private static bool TryEmitCollectionCall(MethodCompiler mc, MethodInfo callee)
    {
        var cls = callee.DeclaringClass;
        bool isDict = IsDictionaryWrapperClass(cls);
        bool isArray = IsArrayWrapperClass(cls);
        if (!isDict && !isArray)
            return false;
        int tag = isDict ? GdTag.Dictionary : GdTag.GodotArray;

        string HandleOf(StackEntry self) => $"(void*)((({cls.CppStructName}*){self.Expr})->f__handle)";
        var ps = callee.Signature.ParameterTypes;

        switch (callee.Name)
        {
            case "get_Count" when ps.Length == 0:
            {
                var self = mc.Pop();
                mc.Push(StackKind.I4, "int32_t", $"dn2cpp_godot_container_size({tag}, {HandleOf(self)})");
                return true;
            }
            case "Clear" when ps.Length == 0:
            {
                var self = mc.Pop();
                mc.Emit($"dn2cpp_godot_container_clear({tag}, {HandleOf(self)});");
                return true;
            }
            case "__Destroy" when ps.Length == 0:
            {
                // Finalizer bridge: destroy the wrapper's engine container value.
                // Clear the field before destroying (the RefCounted.__Destroy
                // pattern), so a re-entrant look never sees a dangling handle.
                var self = mc.Pop();
                string hf = $"((({cls.CppStructName}*){self.Expr})->f__handle)";
                mc.Emit($"{{ void* _h = (void*){hf}; {hf} = 0; dn2cpp_godot_container_destroy({tag}, _h); }}");
                return true;
            }
        }

        if (isArray)
        {
            switch (callee.Name)
            {
                case "get_Item" when ps.Length == 1:
                {
                    var idx = mc.Pop();
                    var self = mc.Pop();
                    var elem = callee.Signature.ReturnType;
                    if (elem.Class is { } ec && ec.FullName != "Godot.Variant" && IsGodotObjectFamily(ec))
                    {
                        // Engine-object element: the bridge extracts the handle and
                        // (for RefCounted) transfers one engine reference to us.
                        string h = mc.NewLocal("void*");
                        mc.Emit($"{h} = dn2cpp_godot_array_get_object({HandleOf(self)}, (int64_t)({idx.Expr}));");
                        EmitWrapEngineObject(mc, elem, h, callee);
                        return true;
                    }
                    string pod = mc.NewLocal("Dn2CppGodotVariant");
                    mc.Emit($"dn2cpp_godot_array_get({HandleOf(self)}, (int64_t)({idx.Expr}), &{pod});");
                    PushPodAsValue(mc, callee, elem, pod);
                    return true;
                }
                case "set_Item" when ps.Length == 2:
                {
                    var val = mc.Pop();
                    var idx = mc.Pop();
                    var self = mc.Pop();
                    string pod = EmitValueToPod(mc, callee, ps[1], val);
                    mc.Emit($"dn2cpp_godot_array_set({HandleOf(self)}, (int64_t)({idx.Expr}), &{pod});");
                    return true;
                }
                case "Add" when ps.Length == 1:
                {
                    var val = mc.Pop();
                    var self = mc.Pop();
                    string pod = EmitValueToPod(mc, callee, ps[0], val);
                    mc.Emit($"dn2cpp_godot_array_add({HandleOf(self)}, &{pod});");
                    return true;
                }
            }
            return false;
        }

        switch (callee.Name)
        {
            case "get_Item" when ps.Length == 1:
            {
                var key = mc.Pop();
                var self = mc.Pop();
                string kpod = EmitValueToPod(mc, callee, ps[0], key);
                string vpod = mc.NewLocal("Dn2CppGodotVariant");
                mc.Emit($"dn2cpp_godot_dict_get({HandleOf(self)}, &{kpod}, &{vpod});");
                PushPodAsValue(mc, callee, callee.Signature.ReturnType, vpod);
                return true;
            }
            case "set_Item" or "Add" when ps.Length == 2:
            {
                var val = mc.Pop();
                var key = mc.Pop();
                var self = mc.Pop();
                string kpod = EmitValueToPod(mc, callee, ps[0], key);
                string vpod = EmitValueToPod(mc, callee, ps[1], val);
                mc.Emit($"dn2cpp_godot_dict_set({HandleOf(self)}, &{kpod}, &{vpod});");
                return true;
            }
            case "ContainsKey" when ps.Length == 1:
            {
                var key = mc.Pop();
                var self = mc.Pop();
                string kpod = EmitValueToPod(mc, callee, ps[0], key);
                mc.Push(StackKind.I4, "int32_t", $"dn2cpp_godot_dict_has({HandleOf(self)}, &{kpod})");
                return true;
            }
            case "get_Keys" or "get_Values" when ps.Length == 0:
            {
                // keys()/values() hand back a fresh engine Array value the new
                // wrapper owns.
                string fn = callee.Name == "get_Keys" ? "dn2cpp_godot_dict_keys" : "dn2cpp_godot_dict_values";
                var self = mc.Pop();
                string h = mc.NewLocal("void*");
                mc.Emit($"{h} = {fn}({HandleOf(self)});");
                EmitWrapContainer(mc, callee.Signature.ReturnType, h, callee);
                return true;
            }
        }
        return false;
    }

    /// <summary>Pushes a decoded runtime Variant POD as the signature type
    /// <paramref name="t"/> — the typed-array / dictionary element read
    /// direction. The shim Variant itself copies field-wise; scalars, strings,
    /// RID, packed arrays and nested container wrappers read the matching POD
    /// payload. An unsupported element type throws (loud, never silent).</summary>
    private static void PushPodAsValue(MethodCompiler mc, MethodInfo callee, TypeDesc t, string pod)
    {
        if (t.Class?.FullName == "Godot.Variant")
        {
            string retCpp = CppTypes.Of(t);
            string r = mc.NewLocal(retCpp);
            mc.Emit(CopyPodToShim(r, pod));
            mc.Push(StackKind.Struct, retCpp, r);
            return;
        }
        if (t.IsString)
        {
            // Kinds 4/11/12 (String/StringName/NodePath) all decode into `s`.
            mc.Push(StackKind.Ref, "Dn2CppString*", $"{pod}.s");
            return;
        }
        if (t.Kind == TypeKind.Primitive)
        {
            switch (t.Primitive)
            {
                case PrimitiveTypeCode.Int64:
                    mc.Push(StackKind.I8, "int64_t", $"{pod}.i");
                    return;
                case PrimitiveTypeCode.Int32:
                    mc.Push(StackKind.I4, "int32_t", $"(int32_t){pod}.i");
                    return;
                case PrimitiveTypeCode.Boolean:
                    mc.Push(StackKind.I4, "int32_t", $"(int32_t)({pod}.i != 0)");
                    return;
                case PrimitiveTypeCode.Double:
                    mc.Push(StackKind.R8, "double", $"{pod}.f");
                    return;
                case PrimitiveTypeCode.Single:
                    // Loaded like ldelem.r4: the float32 value widened onto the
                    // R8 evaluation stack.
                    mc.Push(StackKind.R8, "double", $"(double)(float){pod}.f");
                    return;
            }
        }
        if (t.Class?.FullName == "Godot.Rid")
        {
            string rc = CppTypes.Of(t);
            mc.Push(StackKind.Struct, rc, $"({rc}{{ (uint64_t){pod}.i }})");
            return;
        }
        if (t.Class is { } wc && (IsArrayWrapperClass(wc) || IsDictionaryWrapperClass(wc)))
        {
            // A nested container payload is already the managed wrapper (the
            // codec allocated it around the decoded engine value). The decode
            // yields the base Array wrapper, so only untyped nesting casts.
            mc.NoteReferencedType(wc);
            string rc = CppTypes.Of(t);
            mc.Push(StackKind.Ref, rc, $"({rc})({pod}.obj)");
            return;
        }
        if (t.Kind == TypeKind.SZArray)
        {
            // A packed-array payload decodes as a managed array stamped with its
            // precise type-info (nil when the compilation never mentioned it).
            string rc = CppTypes.Of(t);
            mc.Push(StackKind.Ref, rc, $"({rc})({pod}.obj)");
            return;
        }
        throw new NotSupportedException(
            $"Godot container element type {t} is not supported: {callee.DeclaringClass.FullName}.{callee.Name}");
    }

    /// <summary>Builds a runtime Variant POD local from a value of signature type
    /// <paramref name="t"/> — the write direction of <see cref="PushPodAsValue"/>.
    /// Returns the POD local's name. An unsupported type throws.</summary>
    private static string EmitValueToPod(MethodCompiler mc, MethodInfo callee, TypeDesc t, StackEntry e)
    {
        string pod = mc.NewLocal("Dn2CppGodotVariant");
        if (t.Class?.FullName == "Godot.Variant")
        {
            string sv = mc.NewLocal(CppTypes.Of(t));
            mc.Emit($"{sv} = {MethodCompiler.Cast(e, CppTypes.Of(t))};");
            mc.Emit(CopyShimToPod(pod, sv));
            return pod;
        }
        mc.Emit($"{pod} = {{}};");
        if (t.IsString)
        {
            string v = mc.NewLocal("Dn2CppString*");
            mc.Emit($"{v} = (Dn2CppString*)({e.Expr});");
            // A null string encodes as NIL (GodotSharp maps null to an empty
            // engine value; NIL is the closest the POD models without a copy).
            mc.Emit($"{pod}.kind = ({v} == nullptr) ? {Vk.Nil} : {Vk.String}; {pod}.s = {v};");
            return pod;
        }
        if (t.Kind == TypeKind.Primitive)
        {
            switch (t.Primitive)
            {
                case PrimitiveTypeCode.Int64 or PrimitiveTypeCode.Int32:
                    mc.Emit($"{pod}.kind = {Vk.Int}; {pod}.i = (int64_t)({e.Expr});");
                    return pod;
                case PrimitiveTypeCode.Boolean:
                    mc.Emit($"{pod}.kind = {Vk.Bool}; {pod}.i = (int64_t)({e.Expr});");
                    return pod;
                case PrimitiveTypeCode.Double or PrimitiveTypeCode.Single:
                    mc.Emit($"{pod}.kind = {Vk.Float}; {pod}.f = (double)({e.Expr});");
                    return pod;
            }
        }
        if (t.Class?.FullName == "Godot.Rid")
        {
            string v = mc.NewLocal(CppTypes.Of(t));
            mc.Emit($"{v} = {e.Expr};");
            mc.Emit($"{pod}.kind = {Vk.Rid}; {pod}.i = (int64_t){v}.f__id;");
            return pod;
        }
        if (t.Class is { } wc && (IsArrayWrapperClass(wc) || IsDictionaryWrapperClass(wc)))
        {
            int kind = IsDictionaryWrapperClass(wc) ? Vk.GodotDictionary : Vk.GodotArray;
            string v = mc.NewLocal(CppTypes.Of(t));
            mc.Emit($"{v} = {MethodCompiler.Cast(e, CppTypes.Of(t))};");
            mc.Emit($"{pod}.kind = ({v} == nullptr) ? {Vk.Nil} : {kind}; {pod}.obj = (Dn2CppObject*){v};");
            return pod;
        }
        if (t.Class is { } oc && IsGodotObjectFamily(oc))
        {
            string v = mc.NewLocal(CppTypes.Of(t));
            mc.Emit($"{v} = {MethodCompiler.Cast(e, CppTypes.Of(t))};");
            mc.Emit($"{pod}.kind = ({v} == nullptr) ? {Vk.Nil} : {Vk.Object}; {pod}.i = ({v} == nullptr) ? 0 : (int64_t){v}->f__handle;");
            return pod;
        }
        if (t.Kind == TypeKind.SZArray)
        {
            int kind = PackedGdTag(t, callee) + Vk.PackedTagOffset; // DN2CPP_GD_PACKED_* -> Variant payload kind
            string v = mc.NewLocal(CppTypes.Of(t));
            mc.Emit($"{v} = {MethodCompiler.Cast(e, CppTypes.Of(t))};");
            mc.Emit($"{pod}.kind = ({v} == nullptr) ? {Vk.Nil} : {kind}; {pod}.obj = (Dn2CppObject*){v};");
            return pod;
        }
        throw new NotSupportedException(
            $"Godot container element type {t} is not supported: {callee.DeclaringClass.FullName}.{callee.Name}");
    }

    /// <summary>A hoisted local for the RETURN slot of a call through one of the two
    /// <c>void* r_ret</c> bridges (<c>dn2cpp_godot_call_ptrcall</c>,
    /// <c>dn2cpp_godot_builtin_call</c>), zero-initialized.
    ///
    /// <para><b>The zeroing is load-bearing, not hygiene.</b> <c>MethodCompiler.NewLocal</c>
    /// hoists an <i>uninitialized</i> declaration (only IL locals are ZeroInit'd), and
    /// both bridges leave <c>r_ret</c> untouched when they decline to call — which they
    /// do whenever the receiver is null (an absent singleton is the documented degrade
    /// path) or the method bind fails to resolve. Without the store that decline hands
    /// back whatever was on the C++ stack, turning a clean default into a silent wrong
    /// answer. A typed bridge self-initializes instead; these two cannot, because a
    /// <c>void*</c> out-param does not know its own size. The store is dead whenever the
    /// call does write.</para></summary>
    private static string NewRetLocal(MethodCompiler mc, string cppType)
    {
        string r = mc.NewLocal(cppType);
        mc.Emit($"{r} = {CppTypes.ZeroInit(cppType)};");
        return r;
    }

    /// <summary>The array form of <see cref="NewRetLocal"/> — the native-layout float
    /// buffer a transpose-matrix return is read into. Same reason, same hazard: a
    /// declined call would otherwise transpose stack garbage into the shim struct.</summary>
    private static string NewRetLocalArray(MethodCompiler mc, string elemCppType, int count)
    {
        string r = mc.NewLocalArray(elemCppType, count);
        mc.Emit($"std::memset({r}, 0, sizeof({r}));");
        return r;
    }

    /// <summary>Permutes the <paramref name="map"/>.Length floats at <paramref name="srcPtrExpr"/>
    /// into a fresh native-layout <c>float[]</c> local and returns its name (which decays
    /// to a <c>float*</c>). Used to transpose a shim Basis/Transform3D into Godot's
    /// row-major order for the receiver/argument pointers.</summary>
    private static string EmitTranspose(MethodCompiler mc, int[] map, string srcPtrExpr)
    {
        string buf = mc.NewLocalArray("float", map.Length);
        for (int k = 0; k < map.Length; k++)
            mc.Emit($"{buf}[{k}] = ((const float*)({srcPtrExpr}))[{map[k]}];");
        return buf;
    }

    /// <summary>Lowers a call to a generated builtin value-type method to the runtime
    /// <c>dn2cpp_godot_builtin_call</c> bridge. The receiver is already a <c>T*</c>
    /// (value-type <c>this</c>), used directly as the native <c>base</c>; each argument
    /// is materialized into a native-layout local (the shim struct == Godot's native
    /// layout for the supported non-transpose types) and passed by address; the result
    /// is read back from a native-layout local.</summary>
    private static void EmitBuiltinCall(MethodCompiler mc, MethodInfo callee, BuiltinMethod bm)
    {
        // Pop arguments (last pushed first), then the receiver.
        var argEntries = new StackEntry[bm.ArgGds.Length];
        for (int i = bm.ArgGds.Length - 1; i >= 0; i--)
            argEntries[i] = mc.Pop();
        var self = mc.Pop();

        // The receiver is the value's `T*`. For the transpose matrix types the shim
        // stores columns where Godot wants rows, so build a native-layout float buffer;
        // otherwise the shim layout already equals native and the pointer passes through.
        string selfPtr = MethodCompiler.Cast(self, callee.DeclaringClass.CppStructName + "*");
        string basePtr = GdTag.TransposeMap(bm.GdType) is { } recvMap
            ? $"(const void*){EmitTranspose(mc, recvMap, selfPtr)}"
            : $"(const void*)({selfPtr})";

        var argAddrs = new string[bm.ArgGds.Length];
        for (int i = 0; i < bm.ArgGds.Length; i++)
        {
            // Scalar int/float args use the builtin-method ptrcall's widened encoding
            // (int64/double), mirroring the int/float return readback below — Godot's
            // GDExtension passes scalars as int64_t/double even in a single-precision
            // build (the narrow shim field would under-read the engine's 8 bytes).
            // Value-type args (Vector*/Color/…) keep the shim struct, whose layout equals
            // the native ptrcall layout — except the transpose matrix args, handled below.
            string at = bm.ArgGds[i] switch
            {
                GdTag.Bool => "uint8_t", // ptrcall-encoded as a single byte (Color.to_html's with_alpha)
                GdTag.Int => "int64_t",
                GdTag.Float => "double",
                _ => CppTypes.Of(callee.Signature.ParameterTypes[i]),
            };
            string al = mc.NewLocal(at);
            mc.Emit($"{al} = {MethodCompiler.Cast(argEntries[i], at)};");
            argAddrs[i] = GdTag.TransposeMap(bm.ArgGds[i]) is { } argMap
                ? EmitTranspose(mc, argMap, $"&{al}")
                : $"&{al}";
        }

        string argsExpr;
        if (bm.ArgGds.Length > 0)
        {
            string arr = mc.NewLocalArray("const void*", bm.ArgGds.Length);
            for (int i = 0; i < bm.ArgGds.Length; i++)
                mc.Emit($"{arr}[{i}] = {argAddrs[i]};");
            argsExpr = arr;
        }
        else
        {
            argsExpr = "nullptr";
        }

        int n = bm.ArgGds.Length;
        string meta = $"{bm.GdType}, \"{bm.EngineName}\", {bm.Hash}LL, {basePtr}, {argsExpr}";

        switch (bm.RetGd)
        {
            case GdTag.Void:
                mc.Emit($"dn2cpp_godot_builtin_call({meta}, nullptr, {n});");
                return;
            case GdTag.Bool:
            {
                string r = NewRetLocal(mc, "uint8_t");
                mc.Emit($"dn2cpp_godot_builtin_call({meta}, &{r}, {n});");
                mc.Push(StackKind.I4, "int32_t", $"(int32_t){r}");
                return;
            }
            case GdTag.Int:
            {
                string r = NewRetLocal(mc, "int64_t");
                mc.Emit($"dn2cpp_godot_builtin_call({meta}, &{r}, {n});");
                mc.Push(StackKind.I8, "int64_t", r);
                return;
            }
            case GdTag.Float:
            {
                string r = NewRetLocal(mc, "double");
                mc.Emit($"dn2cpp_godot_builtin_call({meta}, &{r}, {n});");
                mc.Push(StackKind.R8, "double", r);
                return;
            }
            case GdTag.String: // the engine writes a heap Godot String; the bridge
            {                  // converts it to a managed Dn2CppString* and frees the engine String.
                string r = mc.NewLocal("Dn2CppString*");
                mc.Emit($"{r} = dn2cpp_godot_builtin_call_str({meta}, {n});");
                mc.Push(StackKind.Ref, "Dn2CppString*", r);
                return;
            }
            case GdTag.Variant: // marshal the engine Variant into the shim Variant
            {                   // via a decoded Dn2CppGodotVariant.
                string tmp = mc.NewLocal("Dn2CppGodotVariant");
                mc.Emit($"dn2cpp_godot_builtin_call_variant({meta}, &{tmp}, {n});");
                string retCpp = CppTypes.Of(callee.Signature.ReturnType); // t_Godot_Variant
                string r = mc.NewLocal(retCpp);
                mc.Emit(CopyPodToShim(r, tmp));
                mc.Push(StackKind.Struct, retCpp, r);
                return;
            }
            default: // value type (Vector2/3/4/Color/Quaternion/Transform2D/Projection/…)
            {
                string retCpp = CppTypes.Of(callee.Signature.ReturnType);
                if (GdTag.TransposeMap(bm.RetGd) is { } retMap)
                {
                    // The engine writes the result in native (row-major) float order;
                    // transpose it back into the column-major shim struct.
                    string nat = NewRetLocalArray(mc, "float", retMap.Length);
                    mc.Emit($"dn2cpp_godot_builtin_call({meta}, {nat}, {n});");
                    string r = mc.NewLocal(retCpp);
                    for (int k = 0; k < retMap.Length; k++)
                        mc.Emit($"((float*)&{r})[{k}] = {nat}[{retMap[k]}];");
                    mc.Push(StackKind.Struct, retCpp, r);
                    return;
                }
                // Non-transpose value type — shim struct layout equals native.
                string rr = NewRetLocal(mc, retCpp);
                mc.Emit($"dn2cpp_godot_builtin_call({meta}, &{rr}, {n});");
                mc.Push(StackKind.Struct, retCpp, rr);
                return;
            }
        }
    }

    /// <summary>Emits a ptrcall through the engine handle stored in the node's
    /// first field, routed through the shape-generic method-bind bridge
    /// (<see cref="EmitGenericPtrcall"/>; varargs take the variant-call path).</summary>
    private static void EmitEngineCall(MethodCompiler mc, MethodInfo callee, EngineMethod em)
    {
        if (em.IsVararg)
        {
            EmitVarargCall(mc, callee, em);
            return;
        }

        // Pop arguments (last pushed first), then the receiver.
        var argExprs = new string[em.Args.Length];
        for (int i = em.Args.Length - 1; i >= 0; i--)
            argExprs[i] = CastArg(mc.Pop(), em.Args[i], callee.Signature.ParameterTypes[i]);
        string handle = ReceiverHandle(mc, callee, em);
        string meta = $"{handle}, \"{em.EngineClass}\", \"{em.EngineName}\", {em.Hash}LL";
        EmitGenericPtrcall(mc, callee, em, meta, argExprs);
    }

    /// <summary>The engine receiver of a call — the one place both the ptrcall and the
    /// vararg path get it, so they cannot come to disagree about what a receiver is.
    /// Called only after the arguments have been popped (the receiver is under them).
    ///
    /// <para><b>The order of the three cases is load-bearing.</b> An <c>is_static</c>
    /// ENGINE method takes the null-instance sentinel even when it is reached through a
    /// singleton facade (ResourceUID's three static binds): the bind has no receiver,
    /// and handing it the singleton object would be a wrong call. Only then does a
    /// facade call take the engine's singleton object — fetched, not popped, since a
    /// static C# call pushed no <c>this</c>. Everything else pops the receiver it was
    /// given.</para></summary>
    private static string ReceiverHandle(MethodCompiler mc, MethodInfo callee, EngineMethod em)
    {
        if (em.IsStatic)
            return "DN2CPP_GODOT_STATIC_INSTANCE";
        if (em.Singleton is { } singleton)
            return $"dn2cpp_godot_get_singleton(\"{singleton}\")";
        var self = mc.Pop();
        return $"(void*)(({callee.DeclaringClass.CppStructName}*){self.Expr})->f__handle";
    }

    /// <summary>Lowers a vararg engine method — surfaced as
    /// <c>(fixed args…, params Variant[] tail)</c> — to the variant-call bridge
    /// (<c>object_method_bind_call</c>; a vararg bind cannot be ptrcalled). Each
    /// fixed argument is encoded into a runtime Variant POD (the
    /// <c>Dn2CppGodotVariant</c> codec, via <see cref="EmitValueToPod"/>); the
    /// managed Variant[] tail passes straight through (layout-identical to the
    /// POD), and the runtime concatenates the two into one engine Variant array.
    /// The result Variant decodes into the shim return (a Variant, or an
    /// enum/scalar carried as the Variant's int/float payload, or void). A static
    /// vararg method (none in the 4.7 dump, but handled) passes the null-instance
    /// sentinel.</summary>
    private static void EmitVarargCall(MethodCompiler mc, MethodInfo callee, EngineMethod em)
    {
        // Signature: (fixed args…, params Variant[] tail). Pop the tail array,
        // then each fixed arg (reverse order), then the receiver.
        var ps = callee.Signature.ParameterTypes;
        int fixedCount = em.Args.Length; // declared fixed args; ps[fixedCount] is the tail
        var tail = mc.Pop();
        var fixedEntries = new StackEntry[fixedCount];
        for (int i = fixedCount - 1; i >= 0; i--)
            fixedEntries[i] = mc.Pop();

        // Singleton varargs exist (ClassDB.class_call_static, JavaScriptBridge.create_object),
        // so this path shares the receiver rule rather than restating it.
        string handle = ReceiverHandle(mc, callee, em);

        // Encode the fixed args into a runtime Variant POD array the bridge reads
        // before the managed Variant[] tail.
        string fixedArr = "nullptr";
        if (fixedCount > 0)
        {
            fixedArr = mc.NewLocalArray("Dn2CppGodotVariant", fixedCount);
            for (int i = 0; i < fixedCount; i++)
            {
                string pod = EmitValueToPod(mc, callee, ps[i], fixedEntries[i]);
                mc.Emit($"{fixedArr}[{i}] = {pod};");
            }
        }

        string outPod = mc.NewLocal("Dn2CppGodotVariant");
        mc.Emit($"dn2cpp_godot_call_vararg({handle}, \"{em.EngineClass}\", \"{em.EngineName}\", {em.Hash}LL, "
            + $"{fixedArr}, {fixedCount}, (void*)({tail.Expr}), &{outPod});");

        switch (em.Ret)
        {
            case Gd.Void:
                return;
            case Gd.Variant:
            {
                string retCpp = CppTypes.Of(callee.Signature.ReturnType); // t_Godot_Variant
                string r = mc.NewLocal(retCpp);
                mc.Emit(CopyPodToShim(r, outPod));
                mc.Push(StackKind.Struct, retCpp, r);
                return;
            }
            case Gd.Enum:
                // An enum result travels in the Variant as an int; the generated
                // enums are int32-backed.
                mc.Push(StackKind.I4, "int32_t", $"(int32_t){outPod}.i");
                return;
            case Gd.Bool:
                mc.Push(StackKind.I4, "int32_t", $"(int32_t)({outPod}.i != 0)");
                return;
            case Gd.Int:
                mc.Push(StackKind.I8, "int64_t", $"{outPod}.i");
                return;
            case Gd.Float:
                mc.Push(StackKind.R8, "double", $"{outPod}.f");
                return;
            default:
                throw new NotSupportedException(
                    $"Godot vararg call returning {em.Ret} is not supported yet: {callee.DeclaringClass.FullName}.{callee.Name}");
        }
    }

    /// <summary>Generic method-bind ptrcall for shapes without a dedicated bridge
    /// variant. Supports Bool, Int, Float, Enum, Vector2, Pod (every builtin POD
    /// value type + RID), Packed (packed arrays as managed C# arrays), Variant
    /// (the shim's tagged Variant struct, converted to/from a real engine Variant
    /// by the runtime codec), GdArray/GdDictionary (the engine-backed
    /// Godot.Collections wrapper classes — args borrow the wrapper's engine
    /// value, returns wrap a fresh wrapper-owned value), Callable/Signal (the
    /// field-carrying shim structs, converted to/from a pinned 16-byte engine
    /// value at the boundary — a delegate-backed Callable becomes a custom
    /// engine callable), GdObject, and String-family arguments, and Void, Bool,
    /// Int, Float, Enum, Vector2, Pod, Packed, Variant, GdArray/GdDictionary,
    /// Callable/Signal, String-family, and GdObject returns. Unsupported return
    /// shapes throw rather than emit wrong code.</summary>
    private static void EmitGenericPtrcall(MethodCompiler mc, MethodInfo callee, EngineMethod em, string meta, string[] argExprs)
    {
        // Each argument is materialized into a hoisted local. Primitive/Vector2/object
        // args use their ptrcall-encoded value by address; a string-family or packed
        // arg is converted to its engine heap value by the runtime and freed after
        // the call (the post-call statements collect in engineArgFrees).
        var argPtrs = new string[em.Args.Length];
        var engineArgFrees = new List<string>();
        for (int i = 0; i < em.Args.Length; i++)
        {
            if (StringFamilyArgFns(em.Args[i]) is { } fns)
            {
                string sv = mc.NewLocal("void*");
                mc.Emit($"{sv} = {fns.NewFn}({argExprs[i]});");
                argPtrs[i] = sv; // the engine heap value's address is the native ptrcall arg
                engineArgFrees.Add($"{fns.FreeFn}({sv});");
                continue;
            }
            if (em.Args[i] == Gd.Packed)
            {
                // A packed array is built from the managed array (build engine
                // value -> ptrcall -> destroy engine value, like the string
                // family). The concrete packed kind comes from the signature's
                // array element type.
                int tag = PackedGdTag(callee.Signature.ParameterTypes[i], callee);
                string pv = mc.NewLocal("void*");
                mc.Emit($"{pv} = dn2cpp_godot_packed_arg_new({tag}, (void*)({argExprs[i]}));");
                argPtrs[i] = pv; // the engine packed value's address is the native ptrcall arg
                engineArgFrees.Add($"dn2cpp_godot_packed_arg_free({tag}, {pv});");
                continue;
            }
            if (em.Args[i] == Gd.Variant)
            {
                // A bare Variant argument: decode the shim struct into a runtime
                // POD, from which the bridge builds a pinned engine Variant (the
                // string family's build-call-destroy ownership pattern; an Object
                // payload gets the engine's own RefCounted reference for the
                // Variant's lifetime — see dn2cpp_godot_variant_arg_new).
                string sv = mc.NewLocal(CppTypes.Of(callee.Signature.ParameterTypes[i])); // t_Godot_Variant
                mc.Emit($"{sv} = {argExprs[i]};");
                string pod = mc.NewLocal("Dn2CppGodotVariant");
                mc.Emit(CopyShimToPod(pod, sv));
                string ev = mc.NewLocal("void*");
                mc.Emit($"{ev} = dn2cpp_godot_variant_arg_new(&{pod});");
                argPtrs[i] = ev; // the engine Variant's address is the native ptrcall arg
                engineArgFrees.Add($"dn2cpp_godot_variant_arg_free({ev});");
                continue;
            }
            if (em.Args[i] is Gd.GdArray or Gd.GdDictionary)
            {
                // A container argument passes a pointer to the wrapper's own
                // engine value — borrowed for the call; the engine references
                // the shared container itself if it keeps the value (identity
                // semantics, no copy). A null wrapper passes a fresh empty
                // container destroyed after the call (GodotSharp's null->empty
                // mapping).
                int ctag = em.Args[i] == Gd.GdDictionary ? GdTag.Dictionary : GdTag.GodotArray;
                string w = mc.NewLocal(CppTypes.Of(callee.Signature.ParameterTypes[i]));
                mc.Emit($"{w} = {argExprs[i]};");
                string cv = mc.NewLocal("void*");
                mc.Emit($"{cv} = {w} != nullptr ? (void*)({w}->f__handle) : dn2cpp_godot_container_new({ctag});");
                argPtrs[i] = cv; // the engine container value's address is the native ptrcall arg
                engineArgFrees.Add($"if ({w} == nullptr) dn2cpp_godot_container_destroy({ctag}, {cv});");
                continue;
            }
            if (em.Args[i] is Gd.Callable or Gd.Signal)
            {
                // A Callable/Signal argument: decode the shim struct into a
                // runtime POD, from which the bridge builds a pinned 16-byte
                // engine value destroyed after the call (the String ownership
                // pattern). A delegate-backed Callable becomes a custom engine
                // callable whose userdata pins a copy of the POD, keeping the
                // delegates GC-reachable for the engine value's lifetime.
                string sv = mc.NewLocal(CppTypes.Of(callee.Signature.ParameterTypes[i]));
                mc.Emit($"{sv} = {argExprs[i]};");
                string ev = mc.NewLocal("void*");
                if (em.Args[i] == Gd.Callable)
                {
                    string cpod = mc.NewLocal("Dn2CppGodotCallableShim");
                    mc.Emit(CopyCallableShimToPod(cpod, sv));
                    mc.Emit($"{ev} = dn2cpp_godot_callable_arg_new(&{cpod});");
                    engineArgFrees.Add($"dn2cpp_godot_callable_arg_free({ev});");
                }
                else
                {
                    string spod = mc.NewLocal("Dn2CppGodotSignalShim");
                    mc.Emit(CopySignalShimToPod(spod, sv));
                    mc.Emit($"{ev} = dn2cpp_godot_signal_arg_new(&{spod});");
                    engineArgFrees.Add($"dn2cpp_godot_signal_arg_free({ev});");
                }
                argPtrs[i] = ev; // the engine value's address is the native ptrcall arg
                continue;
            }
            if (em.Args[i] == Gd.Pod)
            {
                // A builtin POD passes by pointer to its native-layout storage. The
                // shim struct's layout equals native for every Pod type except the
                // column-major Basis/Transform3D, which are transposed into a
                // native-order float buffer (as the builtin-method bridge does).
                string pv = mc.NewLocal(CppTypes.Of(callee.Signature.ParameterTypes[i]));
                mc.Emit($"{pv} = {argExprs[i]};");
                argPtrs[i] = GdTag.TransposeMapOf(callee.Signature.ParameterTypes[i]) is { } argMap
                    ? EmitTranspose(mc, argMap, $"&{pv}")
                    : $"&{pv}";
                continue;
            }
            string cppType = GenericArgCppType(em.Args[i], callee);
            string v = mc.NewLocal(cppType);
            mc.Emit($"{v} = {argExprs[i]};");
            argPtrs[i] = $"&{v}";
        }

        string argsExpr;
        if (em.Args.Length > 0)
        {
            string arr = mc.NewLocalArray("const void*", em.Args.Length);
            for (int i = 0; i < em.Args.Length; i++)
                mc.Emit($"{arr}[{i}] = {argPtrs[i]};");
            argsExpr = arr;
        }
        else
        {
            argsExpr = "nullptr";
        }

        void FreeEngineArgs()
        {
            foreach (var stmt in engineArgFrees)
                mc.Emit(stmt);
        }

        if (em.Ret == Gd.Void)
        {
            mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, nullptr);");
            FreeEngineArgs();
            return;
        }

        switch (em.Ret)
        {
            case Gd.Bool:
            {
                string r = NewRetLocal(mc, "uint8_t");
                mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, &{r});");
                FreeEngineArgs();
                mc.Push(StackKind.I4, "int32_t", $"(int32_t){r}");
                return;
            }
            case Gd.Int:
            {
                string r = NewRetLocal(mc, "int64_t");
                mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, &{r});");
                FreeEngineArgs();
                mc.Push(StackKind.I8, "int64_t", r);
                return;
            }
            case Gd.Float:
            {
                string r = NewRetLocal(mc, "double");
                mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, &{r});");
                FreeEngineArgs();
                mc.Push(StackKind.R8, "double", r);
                return;
            }
            case Gd.Enum:
            {
                // The engine writes an enum return as a 64-bit int; every generated
                // C# enum is int32-backed (wider ones are filtered out of the map),
                // so narrow back to the enum's I4 stack representation.
                string r = NewRetLocal(mc, "int64_t");
                mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, &{r});");
                FreeEngineArgs();
                mc.Push(StackKind.I4, "int32_t", $"(int32_t){r}");
                return;
            }
            case Gd.Vector2:
            {
                string r = NewRetLocal(mc, "Dn2CppVector2");
                string retCppType = CppTypes.Of(callee.Signature.ReturnType);
                mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, &{r});");
                FreeEngineArgs();
                mc.Push(StackKind.Struct, retCppType, $"{retCppType}{{ (float){r}.x, (float){r}.y }}");
                return;
            }
            case Gd.Pod:
            {
                // The engine writes the return into caller storage in its native
                // layout — the shim struct itself for the identity-layout PODs, a
                // row-major float buffer transposed back into the column-major shim
                // struct for Basis/Transform3D.
                string retCpp = CppTypes.Of(callee.Signature.ReturnType);
                if (GdTag.TransposeMapOf(callee.Signature.ReturnType) is { } retMap)
                {
                    string nat = NewRetLocalArray(mc, "float", retMap.Length);
                    mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, {nat});");
                    FreeEngineArgs();
                    string r = mc.NewLocal(retCpp);
                    for (int k = 0; k < retMap.Length; k++)
                        mc.Emit($"((float*)&{r})[{k}] = {nat}[{retMap[k]}];");
                    mc.Push(StackKind.Struct, retCpp, r);
                    return;
                }
                string rr = NewRetLocal(mc, retCpp);
                mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, &{rr});");
                FreeEngineArgs();
                mc.Push(StackKind.Struct, retCpp, rr);
                return;
            }
            case Gd.Packed:
            {
                // The runtime ptrcalls into an engine packed value, copies its
                // elements out into a fresh managed array and destroys the engine
                // value (the String return's ownership pattern). Note the element
                // type and give the allocation its precise ti_arr_* handle so
                // arr.GetType()/casts see the exact array type.
                var retType = callee.Signature.ReturnType;
                int tag = PackedGdTag(retType, callee);
                var elem = retType.Element!;
                mc.NoteArrayElementType(elem);
                string retCpp = CppTypes.Of(retType); // Dn2CppArray{I4,Ref,N}*
                string r = mc.NewLocal(retCpp);
                mc.Emit($"{r} = ({retCpp})dn2cpp_godot_call_ptrcall_ret_packed({tag}, {meta}, {argsExpr});");
                FreeEngineArgs();
                mc.Emit($"if ({r} != nullptr) ((Dn2CppObject*){r})->type = {mc.PreciseArrayTypeInfoExpr(elem)};");
                mc.Push(StackKind.Ref, retCpp, r);
                return;
            }
            case Gd.Variant:
            {
                // The runtime ptrcalls into a caller-owned engine Variant, decodes
                // it into the runtime POD (a RefCounted Object payload gets its
                // lifetime guard) and destroys the engine value; the decoded POD
                // is copied field-wise into the shim Variant struct.
                string tmp = mc.NewLocal("Dn2CppGodotVariant");
                mc.Emit($"dn2cpp_godot_call_ptrcall_ret_variant({meta}, {argsExpr}, &{tmp});");
                FreeEngineArgs();
                string retCpp = CppTypes.Of(callee.Signature.ReturnType); // t_Godot_Variant
                string r = mc.NewLocal(retCpp);
                mc.Emit(CopyPodToShim(r, tmp));
                mc.Push(StackKind.Struct, retCpp, r);
                return;
            }
            case Gd.GdArray or Gd.GdDictionary:
            {
                // The runtime ptrcalls into a fresh pinned engine container value
                // and hands its slot over; the managed wrapper wraps and OWNS it
                // (the registered finalizer destroys the value — releasing the
                // one engine reference the ptrcall return transferred — once the
                // wrapper is unreachable).
                int ctag = em.Ret == Gd.GdDictionary ? GdTag.Dictionary : GdTag.GodotArray;
                string h = mc.NewLocal("void*");
                mc.Emit($"{h} = dn2cpp_godot_call_ptrcall_ret_container({ctag}, {meta}, {argsExpr});");
                FreeEngineArgs();
                EmitWrapContainer(mc, callee.Signature.ReturnType, h, callee);
                return;
            }
            case Gd.Callable or Gd.Signal:
            {
                // The runtime ptrcalls into a caller-owned 16-byte engine value,
                // decodes it into the runtime POD and destroys it. One of our own
                // delegate-backed callables round-trips via its custom-callable
                // userdata (the pinned POD copy — delegates intact); a standard
                // callable / signal decodes to its target+method (owner+name)
                // fields, the target wrapped as a borrowed Godot.Object shim.
                var retClass = callee.Signature.ReturnType.Class
                    ?? throw new NotSupportedException(
                        $"Godot Callable/Signal return has no shim class: {callee.DeclaringClass.FullName}.{callee.Name}");
                mc.NoteReferencedType(retClass);
                string retCpp = CppTypes.Of(callee.Signature.ReturnType);
                string r = mc.NewLocal(retCpp);
                if (em.Ret == Gd.Callable)
                {
                    string cpod = mc.NewLocal("Dn2CppGodotCallableShim");
                    mc.Emit($"dn2cpp_godot_call_ptrcall_ret_callable({meta}, {argsExpr}, &{cpod});");
                    FreeEngineArgs();
                    mc.Emit(CopyPodToCallableShim(retClass, r, cpod));
                }
                else
                {
                    string spod = mc.NewLocal("Dn2CppGodotSignalShim");
                    mc.Emit($"dn2cpp_godot_call_ptrcall_ret_signal({meta}, {argsExpr}, &{spod});");
                    FreeEngineArgs();
                    mc.Emit(CopyPodToSignalShim(retClass, r, spod));
                }
                mc.Push(StackKind.Struct, retCpp, r);
                return;
            }
            case Gd.String or Gd.StringName or Gd.NodePath:
            {
                // The runtime ptrcalls into the engine heap value and marshals it to
                // a managed Dn2CppString* (freeing the engine value).
                string retFn = em.Ret switch
                {
                    Gd.String => "dn2cpp_godot_call_ptrcall_ret_str",
                    Gd.StringName => "dn2cpp_godot_call_ptrcall_ret_strname",
                    _ => "dn2cpp_godot_call_ptrcall_ret_nodepath",
                };
                string r = mc.NewLocal("Dn2CppString*");
                mc.Emit($"{r} = {retFn}({meta}, {argsExpr});");
                FreeEngineArgs();
                mc.Push(StackKind.Ref, "Dn2CppString*", r);
                return;
            }
            case Gd.GdObject:
            {
                // ptrcall yields an engine object pointer; wrap it in a fresh managed
                // shim of the static return type (its handle drives further engine
                // calls). A null result wraps as a null reference. A RefCounted-derived
                // return is not a borrowed handle: Godot's ptrcall marshaling for a
                // Ref<T>-returning method already transfers one reference across the
                // GDExtension boundary, so calling reference() again would double the
                // refcount and leak it (the finalizer's single Unreference() balances
                // only one). The wrap therefore just registers the finalizer. The
                // wrapper stays a passive view — no object_set_instance here; that
                // applies only to newobj's registered user-class path.
                string h = NewRetLocal(mc, "void*");
                mc.Emit($"dn2cpp_godot_call_ptrcall({meta}, {argsExpr}, &{h});");
                FreeEngineArgs();
                EmitWrapEngineObject(mc, callee.Signature.ReturnType, h, callee);
                return;
            }
            default:
                throw new NotSupportedException(
                    $"Godot engine call returning {em.Ret} is not supported yet: {callee.DeclaringClass.FullName}.{callee.Name}");
        }
    }

    /// <summary>C++ type of a generic-ptrcall argument temp, matching the encoding
    /// produced by <see cref="CastArg"/> for the same Gd kind.</summary>
    private static string GenericArgCppType(Gd kind, MethodInfo callee) => kind switch
    {
        Gd.Bool => "uint8_t",
        Gd.Int => "int64_t",
        Gd.Float => "double",
        // GDExtension ptrcall passes enum/bitfield args as 64-bit ints, whatever
        // the C# declared backing (always int32 for the generated enums).
        Gd.Enum => "int64_t",
        Gd.Vector2 => "Dn2CppVector2",
        Gd.GdObject => "void*",
        _ => throw new NotSupportedException(
            $"Godot engine call with {kind} argument is not supported yet: {callee.DeclaringClass.FullName}.{callee.Name}"),
    };

    /// <summary>The runtime <c>DN2CPP_GD_PACKED_*</c> tag for a
    /// <see cref="GdKind.Packed"/> signature type — a managed array whose element
    /// selects the concrete packed kind (the shared element→tag map,
    /// <see cref="GdTag.PackedOfElement"/>). The map is total over the shim surface
    /// the shared filters admit, so a miss is a hard error, not a silent
    /// fallthrough.</summary>
    private static int PackedGdTag(TypeDesc t, MethodInfo callee)
    {
        if (t.Kind == TypeKind.SZArray && t.Element is { } e
            && GdTag.PackedOfElement(e) is { } tag)
        {
            return tag;
        }
        throw new NotSupportedException(
            $"Godot packed-array marshalling has no tag for {t}: {callee.DeclaringClass.FullName}.{callee.Name}");
    }

    /// <summary>The runtime arg-marshalling helper pair (build engine heap value /
    /// destroy it after the call) for a string-family kind, or null for any other
    /// kind. The "new" helper returns a <c>void*</c> whose address is the native
    /// ptrcall arg; the "free" helper destroys the engine value post-call.</summary>
    private static (string NewFn, string FreeFn)? StringFamilyArgFns(Gd kind) => kind switch
    {
        Gd.String => ("dn2cpp_godot_str_arg_new", "dn2cpp_godot_str_arg_free"),
        Gd.StringName => ("dn2cpp_godot_strname_arg_new", "dn2cpp_godot_strname_arg_free"),
        Gd.NodePath => ("dn2cpp_godot_nodepath_arg_new", "dn2cpp_godot_nodepath_arg_free"),
        _ => null,
    };

    private static string CastArg(StackEntry e, Gd kind, TypeDesc paramType) => kind switch
    {
        Gd.Bool => $"(uint8_t)({e.Expr})",
        Gd.Int => $"(int64_t)({e.Expr})",
        // An enum stack value is its int32 underlying; widen to the 64-bit
        // ptrcall encoding.
        Gd.Enum => $"(int64_t)({e.Expr})",
        Gd.Float => $"(double)({e.Expr})",
        Gd.Vector2 => $"Dn2CppVector2{{ (float)({e.Expr}).f_X, (float)({e.Expr}).f_Y }}",
        // A Pod value type's stack entry is already the shim struct value; the
        // generic ptrcall materializes it into a native-layout local by address.
        Gd.Pod => e.Expr,
        // A Packed arg's stack entry is the managed array pointer; the generic
        // ptrcall's packed branch builds/destroys the engine value from it.
        Gd.Packed => e.Expr,
        // A bare Variant arg's stack entry is the shim struct value; the generic
        // ptrcall's Variant branch decodes it and builds the engine Variant.
        Gd.Variant => e.Expr,
        // Callable/Signal args are the shim struct value; the generic ptrcall's
        // branch decodes it and builds the pinned engine value.
        Gd.Callable or Gd.Signal => e.Expr,
        // A container arg's stack entry is the managed wrapper pointer (cast to
        // the signature's wrapper struct — a `null` literal's temp is a bare
        // Dn2CppObject*); the generic ptrcall's container branch borrows its
        // engine value.
        Gd.GdArray or Gd.GdDictionary => MethodCompiler.Cast(e, CppTypes.Of(paramType)),
        // The string-family kinds are all managed Dn2CppString* on the C# side; the
        // per-kind runtime arg helper turns each into its engine value.
        Gd.String or Gd.StringName or Gd.NodePath => $"(Dn2CppString*)({e.Expr})",
        // f__handle is an intptr_t, so cast both ternary branches to void* (a bare
        // nullptr would be incompatible with the intptr_t branch). The handle is
        // read through the signature's shim struct: a `null` literal argument's
        // stack temp is a bare Dn2CppObject*, which has no f__handle field.
        Gd.GdObject => $"(({e.Expr}) == nullptr ? (void*)nullptr : (void*)(({MethodCompiler.Cast(e, CppTypes.Of(paramType))})->f__handle))",
        _ => e.Expr,
    };
}
