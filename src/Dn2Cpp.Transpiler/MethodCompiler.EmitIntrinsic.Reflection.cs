using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>Pushes a runtime-allocated reflection member-enumeration array
    /// (GetConstructors/GetMethods/GetProperties/GetFields/GetParameters), retagged
    /// with the precise per-element <c>ti_arr_</c> handle and with the element's
    /// SZArray collection-interface map wired. The runtime helpers allocate under
    /// the shared ref-array handle, which carries no element identity and no
    /// interface map — but the BCL routinely runs LINQ over these results
    /// (`source is T[]`, IEnumerable&lt;T&gt;.GetEnumerator dispatch), so the array
    /// must look exactly like a compile-time <c>newarr</c> allocation.</summary>
    /// <summary>The Type members whose static-token fold yields the same answer
    /// for every real instantiation a placeholder stands for, plus the
    /// token-preserving pass-throughs — the members a shared body may run on a
    /// poisoned (placeholder-token) typeof without observing the instantiation.
    /// The agreement set depends on the placeholder kind:
    /// <list type="bullet">
    /// <item>an enum-width placeholder agrees with its enums on IsValueType,
    /// IsClass, sealedness, TypeCode-of-underlying, …; get_IsPrimitive and
    /// get_IsNested are deliberately absent (an enum is not primitive and can
    /// be nested — its placeholder is the opposite), so those folds taint;</item>
    /// <item>the reference placeholder stands for string/object/user classes/
    /// interfaces/delegates/arrays alike, so only the folds invariant over ALL
    /// reference types survive: IsValueType, IsByRefLike, IsPointer/IsByRef (all
    /// false). IsClass (false for an interface argument), IsSealed (string yes,
    /// object no), IsSZArray (an array argument collapses to the same placeholder)
    /// and GetTypeCode (String vs Object) disagree across members and must
    /// taint.</item>
    /// </list></summary>
    private static bool IsPlaceholderAgnosticFold(string name, bool refPlaceholder) => name is
        "GetTypeFromHandle" or "get_TypeHandle"
        or "get_IsValueType" or "get_IsByRefLike" or "get_IsPointer" or "get_IsByRef"
        || (!refPlaceholder
            && name is "get_IsClass" or "get_IsSealed" or "get_IsSZArray" or "GetTypeCode");

    private void PushReflectionMemberArray(string expr, TypeDesc arrayType)
    {
        if (arrayType is { Kind: TypeKind.SZArray,
                Element: { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External } elem })
        {
            Comp.NoteArrayEnumerableElement(elem);
            string arrTmp = NewTemp("Dn2CppArrayRef*");
            Emit($"{arrTmp} = {expr};");
            Emit($"if ({arrTmp} != nullptr) ((Dn2CppObject*){arrTmp})->type = {PreciseArrayTypeInfoExpr(elem)};");
            Push(StackKind.Ref, "Dn2CppArrayRef*", arrTmp);
            return;
        }
        Push(StackKind.Ref, "Dn2CppArrayRef*", expr);
    }

    private bool TryEmitReflectionIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        // Shared-body candidate: a typeof over a placeholder-bearing type rides the
        // stack as a poisoned (null) handle with its static token attached (see the
        // Ldtoken case). The folds that read only the token stay shareable; every other
        // reflection member would consume the poisoned runtime value, so it taints the
        // trial compile. The scan covers the args plus one deeper slot (a potential
        // receiver) — over-approximate, since a false hit only falls back to a
        // per-instantiation body.
        if (SharedTrial)
        {
            int probe = System.Math.Min(sig.ParameterTypes.Length + 1, StackDepth);
            for (int i = 1; i <= probe; i++)
            {
                if (Peek(i).TypeToken is not { } tk || !Compilation.ContainsCanonPlaceholder(tk))
                    continue;
                // A $CnAny token folds NOTHING (it stands for value and reference
                // arguments alike, so no static answer agrees across the group) and
                // taints nothing either: the Ldtoken case materialized a real
                // runtime-filled handle for it, so stripping the token routes every
                // member below to its runtime-read arm. A member with no runtime arm
                // falls through to the ordinary call path like any non-typeof Type
                // value would.
                if (Compilation.ContainsCanonAny(tk))
                {
                    // The shapes whose Ldtoken arm kept the poisoned null (no
                    // resolvable rgctx entry) must still taint — running a runtime
                    // arm on them would deref the null.
                    if (tk.Kind is TypeKind.MDArray or TypeKind.ByRef or TypeKind.Pointer)
                        ThrowSharedTaint("type-identity", $"{declType}.{name} on {tk}");
                    _stack[^i] = _stack[^i] with { TypeToken = null };
                    continue;
                }
                if (!IsPlaceholderAgnosticFold(name, Compilation.ContainsRefPlaceholder(tk)))
                    ThrowSharedTaint("type-identity", $"{declType}.{name} on {tk}");
            }
        }
        switch (declType, name)
        {
            case ("System.Type", "GetTypeFromHandle") when sig.ParameterTypes.Length == 1:
            {
                var arg = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_get_type_from_handle({arg.Expr})", arg.TypeToken);
                return true;
            }
            // Type.TypeHandle: the inverse of GetTypeFromHandle — a RuntimeTypeHandle
            // is modeled as the type-info pointer, which the runtime Type wraps
            // directly. The static type token rides along (like GetTypeFromHandle) so
            // a typeof(T).TypeHandle chain keeps the compile-time fold alive for
            // consumers like RuntimeHelpers.RunClassConstructor.
            case ("System.Type", "get_TypeHandle"):
            {
                var a = Pop();
                Push(StackKind.Ptr, "const Dn2CppTypeInfo*",
                    $"dn2cpp_type_require({Cast(a, "Dn2CppType*")})", a.TypeToken);
                return true;
            }
            // Type.GetType(string): resolve a CLR FullName against the embedded type
            // name registry. The 1-arg form returns null when missing; the
            // (string, bool throwOnError) form raises TypeLoadException instead. The
            // 3-arg ignoreCase overload is not modeled (registry match is exact).
            case ("System.Type", "GetType") when sig.ParameterTypes is [{ } p0] && p0.IsString:
            {
                var n = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_by_name({Cast(n, "Dn2CppString*")}, 0)");
                return true;
            }
            case ("System.Type", "GetType") when sig.ParameterTypes is [{ } q0, _] && q0.IsString:
            {
                var throwOnError = Pop();
                var n = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_by_name({Cast(n, "Dn2CppString*")}, {Cast(throwOnError, "int32_t")})");
                return true;
            }
            // obj.GetType: the runtime Type from the object header. A boxed value
            // carries its primitive type in the same header.
            case ("System.Object", "GetType") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                // Through Cast, not a C cast: a headerless intrinsic receiver
                // (CultureInfo/NumberFormatInfo/TextInfo — the
                // const Dn2CppNumberFormatInfo* lowering) has no type header at
                // all, so reading ->type off the raw pointer is the misread the
                // wrap boundary exists to prevent. This IS the object boundary —
                // GetType is declared on System.Object — so the receiver takes
                // the same interned wrapper every other escape does, and the
                // handle it carries is the one typeof(CultureInfo) names.
                Push(StackKind.Ref, "Dn2CppType*",
                    $"dn2cpp_get_type_from_handle(((Dn2CppObject*)({Cast(o, "Dn2CppObject*")}))->type)");
                return true;
            }
            // typeof(T).IsValueType: when T is statically known (the type token
            // rides the stack from ldtoken) fold it to a compile-time constant —
            // the BCL leans on this to dead-eliminate whole branches (e.g.
            // Dictionary's reference-type comparer path for value-type keys). For a
            // runtime GetType value, read the VALUETYPE flag.
            case ("System.Type", "get_IsValueType"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } tk
                    ? (IsValueTypeStatic(tk) ? "1" : "0")
                    : $"dn2cpp_type_is_value_type(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // IsClass = reference type (not a value type and not an interface).
            case ("System.Type", "get_IsClass"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } tk
                    ? ((!IsValueTypeStatic(tk) && !IsInterfaceStatic(tk)) ? "1" : "0")
                    : $"dn2cpp_type_is_class(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // IsPrimitive = one of the 14 CLR primitives (not decimal/enum/struct,
            // and not void or TypedReference — see IsPrimitiveStatic for why the
            // exclusion list is four long and not two).
            case ("System.Type", "get_IsPrimitive"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } tk
                    ? (IsPrimitiveStatic(tk) ? "1" : "0")
                    : $"dn2cpp_type_is_primitive(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // IsSealed = cannot be derived from: value types/enums/delegates/arrays/
            // sealed+static classes are sealed; object/interfaces/open/abstract
            // classes are not.
            case ("System.Type", "get_IsSealed"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } tk
                    ? (IsSealedStatic(tk) ? "1" : "0")
                    : $"dn2cpp_type_is_sealed(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // IsByRefLike = ref struct; reads the flag bit (set for emitted ref structs).
            // IsPointer / IsByRef fold to true only for a static pointer/byref typeof
            // token — dn2cpp never materializes such a Type at runtime, so the runtime
            // helper is constant 0 (it still takes the type so the receiver is evaluated).
            case ("System.Type", "get_IsByRefLike"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } tk
                    ? (IsByRefLikeStatic(tk) ? "1" : "0")
                    : $"dn2cpp_type_is_by_ref_like(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsPointer"):
            {
                var a = Pop();
                // A placeholder-bearing token folds to 0 (no placeholder ever
                // stands for a pointer type) instead of consuming the poisoned
                // runtime handle a shared body carries for it.
                Push(StackKind.I4, "int32_t", a.TypeToken is { Kind: TypeKind.Pointer }
                    ? "1"
                    : a.TypeToken is { } pk && Compilation.ContainsCanonPlaceholder(pk)
                        ? "0"
                        : $"dn2cpp_type_is_pointer(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsByRef"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { Kind: TypeKind.ByRef }
                    ? "1"
                    : a.TypeToken is { } bk && Compilation.ContainsCanonPlaceholder(bk)
                        ? "0"
                        : $"dn2cpp_type_is_by_ref(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // Type.GetTypeCode(Type): the TypeCode of a type — its primitive code,
            // String/Decimal/DateTime/DBNull, an enum's UNDERLYING integer code, or
            // Object for anything else (IntPtr/UIntPtr, arrays, nullables, reference
            // and other struct types). The compile-time fold serves a static typeof;
            // the runtime helper covers a non-static Type value.
            case ("System.Type", "GetTypeCode"):
            {
                var a = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                // The one STATIC member in this family, so its null is an ARGUMENT and
                // not a receiver: real .NET answers TypeCode.Empty rather than throwing,
                // which is why this arm alone keeps the raw ->typeInfo read out of
                // dn2cpp_type_require. dn2cpp_type_get_type_code null-checks for it.
                Push(CppTypes.KindOf(sig.ReturnType), ct, a.TypeToken is { } tk
                    ? $"({ct}){TypeCodeStatic(tk)}"
                    : $"({ct})dn2cpp_type_get_type_code(dn2cpp_type_info_or_null({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // Type.Assembly: the defining assembly's identity (its simple name, an
            // opaque const char* handle). op_Equality compares two such handles.
            // Reached from JsonConverter..ctor (IsInternalConverter).
            case ("System.Type", "get_Assembly"):
            {
                var a = Pop();
                Push(StackKind.Ref, "const char*", $"dn2cpp_type_assembly_name({Cast(a, "Dn2CppType*")})");
                return true;
            }
            // Type.AssemblyQualifiedName: "FullName, <assembly display name>" —
            // round-trips through Type.GetType(string) (which strips the
            // assembly-qualified suffix against the embedded registry).
            case ("System.Type", "get_AssemblyQualifiedName"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_type_assembly_qualified_name({Cast(a, "Dn2CppType*")})");
                return true;
            }
            case ("System.Reflection.Assembly", "op_Equality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_assembly_equals({Cast(a, "const char*")}, {Cast(b, "const char*")})");
                return true;
            }
            case ("System.Reflection.Assembly", "op_Inequality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"(!dn2cpp_assembly_equals({Cast(a, "const char*")}, {Cast(b, "const char*")}))");
                return true;
            }
            // Assembly.GetEntryAssembly() -> the app module's simple-name handle (an
            // Assembly is modeled as its simple-name const char* handle), so
            // typeof(X).Assembly == Assembly.GetEntryAssembly() holds for app types and
            // GetCustomAttributes resolves the app module's assembly registry entry.
            // MemberInfo-family equality: two reflected handles are equal when they
            // wrap the same metadata entry. The runtime interns handles per row
            // (matching real .NET's RuntimeType member cache, so reference identity
            // holds too); this helper's row comparison stays the semantic definition,
            // dispatching on the object header for MethodInfo/ConstructorInfo/
            // FieldInfo/PropertyInfo/Type alike.
            case ("System.Reflection.MemberInfo" or "System.Reflection.MethodBase"
                    or "System.Reflection.MethodInfo" or "System.Reflection.ConstructorInfo"
                    or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo",
                "op_Equality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_memberinfo_equals({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Reflection.MemberInfo" or "System.Reflection.MethodBase"
                    or "System.Reflection.MethodInfo" or "System.Reflection.ConstructorInfo"
                    or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo",
                "op_Inequality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"(!dn2cpp_memberinfo_equals({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")}))");
                return true;
            }
            // MemberInfo.IsCollectible: true only for members of a collectible
            // AssemblyLoadContext, which an AOT image never has.
            case ("System.Reflection.MemberInfo", "get_IsCollectible"):
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            // RuntimeHelpers.Box(ref byte, RuntimeTypeHandle): only
            // Enum.InternalBoxEnum reaches this (the Enum.ToObject family). The handle
            // value is the type-info pointer (see the RuntimeTypeHandle mapping in
            // CppTypes) and it is a RUN-TIME value, so the payload width must be read
            // off it in the runtime — a fixed sizeof(int32_t) here would truncate a
            // long/ulong enum.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "Box") when sig.ParameterTypes.Length == 2:
            {
                var h = Pop();
                var r = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_box_by_handle({h.Expr}, (const void*)({r.Expr}))");
                return true;
            }
            // RuntimeHelpers.GetUninitializedObject(Type): allocate without running
            // a constructor (deserializer-style callers construct the object this
            // way and invoke the constructor separately).
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "GetUninitializedObject")
                when sig.ParameterTypes.Length == 1:
            {
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_get_uninitialized_object({Cast(t, "Dn2CppType*")})");
                return true;
            }
            // Assembly.GetTypes(): the type-registry entries the assembly defines
            // (the AOT-preserved subset). Retagged to the declared Type[] identity —
            // reflection scans routinely LINQ over it.
            case ("System.Reflection.Assembly", "GetTypes") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_assembly_get_types({Cast(a, "const char*")})", sig.ReturnType);
                return true;
            }
            case ("System.Reflection.Assembly", "GetEntryAssembly")
                when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "const char*",
                    $"\"{Comp.Reader.GetString(Comp.Reader.GetAssemblyDefinition().Name)}\"");
                return true;
            // Assembly.GetExecutingAssembly(): folded at transpile time to the
            // CALLING method's defining assembly — exactly the real semantics (the
            // executing method's module is known statically; no stack walk needed).
            // GetCallingAssembly() folds the same way (DECLARED DIVERGENCE: real .NET
            // walks one frame further to the caller's caller, which no static fold can
            // see; the reachable callers use it for "which app assembly am I in"
            // identity, where every candidate caller lives in the same assembly).
            case ("System.Reflection.Assembly", "GetExecutingAssembly" or "GetCallingAssembly")
                when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "const char*", $"\"{Module.AssemblyName}\"");
                return true;
            // Assembly.get_Location -> null. A native binary has no assembly file on
            // disk (DECLARED DIVERGENCE; real NativeAOT returns "" here).
            // AppContext.BaseDirectory does NOT come through this — it is intrinsified
            // onto the process image — so nothing turns this null into a directory to
            // probe.
            case ("System.Reflection.Assembly", "get_Location"):
                Pop(); // the assembly receiver (const char* handle)
                Push(StackKind.Ref, "Dn2CppString*", "nullptr");
                return true;
            // Assembly.GetCustomAttributes([attrType,] inherit) / IsDefined(attrType,
            // inherit): the assembly-registry attribute table of the receiver handle.
            // inherit is popped and ignored like the member forms (assemblies have no
            // attribute inheritance anyway).
            case ("System.Reflection.Assembly", "GetCustomAttributes")
                when sig.ParameterTypes.Length == 1:
            {
                Pop(); // inherit
                var asm = Pop();
                PushAttributeArray(
                    $"dn2cpp_assembly_get_custom_attributes({Cast(asm, "const char*")}, nullptr)",
                    sig.ReturnType);
                return true;
            }
            case ("System.Reflection.Assembly", "GetCustomAttributes")
                when sig.ParameterTypes.Length == 2:
            {
                Pop(); // inherit
                var filter = Pop();
                var asm = Pop();
                PushAttributeArray(
                    $"dn2cpp_assembly_get_custom_attributes_typed({Cast(asm, "const char*")}, {Cast(filter, "Dn2CppType*")})",
                    sig.ReturnType);
                return true;
            }
            case ("System.Reflection.Assembly", "IsDefined") when sig.ParameterTypes.Length == 2:
            {
                Pop(); // inherit
                var filter = Pop();
                var asm = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_assembly_is_defined({Cast(asm, "const char*")}, {Cast(filter, "Dn2CppType*")})");
                return true;
            }
            // Assembly.GetName([copiedName]) -> a real managed AssemblyName carrying
            // the registry identity: the display name is composed at runtime
            // ("Name, Version=…, Culture=…, PublicKeyToken=…") and parsed back
            // through the transpiled BCL AssemblyName(string) ctor — AssemblyName is
            // NOT intrinsic-mapped, so Name/Version/FullName/ToString all run the
            // real BCL IL. copiedName is popped and ignored (it only governs
            // shadow-copy paths that do not exist here).
            case ("System.Reflection.Assembly", "GetName") when sig.ParameterTypes.Length <= 1:
            {
                if (Comp.ReachManagedMethod("System.Reflection.AssemblyName", ".ctor",
                        ps => ps is [{ IsString: true }], allocates: true) is not { } anCtor)
                    return false;
                if (sig.ParameterTypes.Length == 1)
                    Pop(); // copiedName
                var asm = Pop();
                var anCls = anCtor.DeclaringClass;
                string dn = NewTemp("Dn2CppString*");
                Emit($"{dn} = dn2cpp_assembly_full_name({Cast(asm, "const char*")});");
                string an = NewTemp(anCls.CppStructName + "*");
                Emit($"{an} = ({anCls.CppStructName}*)dn2cpp_alloc(sizeof({anCls.CppStructName}));");
                Emit($"((Dn2CppObject*){an})->type = &{anCls.CppTypeInfoName};");
                Emit($"{DirectCall(anCtor, new List<string> { an, dn })};");
                PushEntry(new StackEntry(an, StackKind.Ref, anCls.CppStructName + "*",
                    StaticType: TypeDesc.MakeClass(anCls)));
                return true;
            }
            // Assembly.FullName: the .NET display name composed from the assembly
            // registry's identity columns (version/culture/public-key token read
            // from the module metadata at emit time).
            case ("System.Reflection.Assembly", "get_FullName"):
            {
                var asm = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_assembly_full_name({Cast(asm, "const char*")})");
                return true;
            }
            // Assembly.GetType(name[, throwOnError[, ignoreCase]]): the type-name
            // registry restricted to the receiver assembly's rows (the global
            // Type.GetType(string) resolves across every loaded assembly).
            case ("System.Reflection.Assembly", "GetType")
                when sig.ParameterTypes.Length is >= 1 and <= 3 && sig.ParameterTypes[0].IsString:
            {
                string ignoreCase = "0", throwOnError = "0";
                if (sig.ParameterTypes.Length == 3)
                    ignoreCase = Cast(Pop(), "int32_t");
                if (sig.ParameterTypes.Length >= 2)
                    throwOnError = Cast(Pop(), "int32_t");
                var n = Pop();
                var asm = Pop();
                Push(StackKind.Ref, "Dn2CppType*",
                    $"dn2cpp_assembly_get_type({Cast(asm, "const char*")}, {Cast(n, "Dn2CppString*")}, {throwOnError}, {ignoreCase})");
                return true;
            }
            // Assembly.IsDynamic: true only for Reflection.Emit-built assemblies,
            // which the dynamic-codegen cut guarantees never exist here.
            case ("System.Reflection.Assembly", "get_IsDynamic"):
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            // Assembly.GetModules([getResourceModules]) / ManifestModule: dn2cpp
            // assemblies are single-module, so the manifest module IS the assembly
            // handle (Module is modeled as the same simple-name const char*).
            case ("System.Reflection.Assembly", "GetModules") when sig.ParameterTypes.Length <= 1:
            {
                if (sig.ParameterTypes.Length == 1)
                    Pop(); // getResourceModules — no resource modules exist
                var asm = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_assembly_get_modules({Cast(asm, "const char*")})", sig.ReturnType);
                return true;
            }
            case ("System.Reflection.Assembly", "get_ManifestModule"):
            {
                var asm = Pop();
                Push(StackKind.Ref, "const char*", Cast(asm, "const char*"));
                // Module static type — see the get_Module note (Object::ToString).
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            }
            // Assembly.Load(String) / LoadWithPartialName(String): NAME-based
            // resolution is a lookup over the linked-assembly registry, not a load —
            // every assembly the program can ever name is already statically linked
            // (the same registry Type.Assembly / GetEntryAssembly handles come from,
            // so the returned handle satisfies op_Equality and Assembly.GetType).
            // Misses match real .NET: Load throws FileNotFoundException, the obsolete
            // LoadWithPartialName returns null. Matching/divergence details at
            // dn2cpp_assembly_lookup_by_name.
            case ("System.Reflection.Assembly", "Load" or "LoadWithPartialName")
                when sig.ParameterTypes is [{ IsString: true }]:
            {
                var n = Pop();
                string helper = name == "Load"
                    ? "dn2cpp_assembly_load" : "dn2cpp_assembly_load_partial";
                Push(StackKind.Ref, "const char*", $"{helper}({Cast(n, "Dn2CppString*")})");
                return true;
            }
            // Assembly.Load(AssemblyName): the same registry lookup keyed on the
            // managed AssemblyName's simple name, read through its real transpiled
            // getter (AssemblyName is a plain managed class — the GetName() bridge in
            // reverse). A null AssemblyName maps to a null name, for which the helper
            // throws ArgumentNullException like real .NET; an AssemblyName WITH a null
            // Name also gets ArgumentNullException where real .NET says
            // ArgumentException (DECLARED DIVERGENCE, same catchable family).
            case ("System.Reflection.Assembly", "Load")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Reflection.AssemblyName" }]:
            {
                if (Comp.ReachManagedMethod("System.Reflection.AssemblyName", "get_Name",
                        static ps => ps.Length == 0) is not { } getName)
                    return false;
                var an = Pop();
                var anCls = getName.DeclaringClass;
                string recv = NewTemp(anCls.CppStructName + "*");
                Emit($"{recv} = {Cast(an, anCls.CppStructName + "*")};");
                string nm = NewTemp("Dn2CppString*");
                Emit($"{nm} = ({recv} == nullptr) ? nullptr : {DirectCall(getName, new List<string> { recv })};");
                Push(StackKind.Ref, "const char*", $"dn2cpp_assembly_load({nm})");
                return true;
            }
            // Assembly loading from a file path or a raw IL image is the definitional
            // AOT cut: there is no loader and no file image (name-based Load /
            // LoadWithPartialName resolve against the linked registry above).
            // Catchable runtime PlatformNotSupportedException — loud, never wrong data.
            case ("System.Reflection.Assembly",
                "Load" or "LoadFile" or "LoadFrom" or "LoadWithPartialName"
                or "UnsafeLoadFrom" or "LoadModule" or "ReflectionOnlyLoad"
                or "ReflectionOnlyLoadFrom"):
            {
                for (int i = 0; i < sig.ParameterTypes.Length; i++)
                    Pop();
                if (name == "LoadModule")
                    Pop(); // instance receiver
                Emit($"dn2cpp_throw_platform_not_supported(\"Assembly.{name} is not available in AOT-compiled code (no assembly loading)\");");
                Push(StackKind.Ref, "const char*", "nullptr"); // unreachable; stack typing only
                return true;
            }
            // Assembly.GetManifestResourceNames(): the embedded resources' names in
            // metadata order, out of the assembly registry's resource table. Noting the
            // use is what makes the emitter carry the blobs at all (see
            // Compilation.ManifestResourcesUsed).
            case ("System.Reflection.Assembly", "GetManifestResourceNames")
                when sig.ParameterTypes.Length == 0:
            {
                Comp.NoteManifestResourceUse();
                var asm = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_assembly_get_manifest_resource_names({Cast(asm, "const char*")})",
                    sig.ReturnType);
                return true;
            }
            // Assembly.GetManifestResourceStream(string) / (Type, string): the
            // resource's bytes wrapped in a READ-ONLY MemoryStream — real .NET hands
            // back an UnmanagedMemoryStream over the mapped image, whose body is
            // untranspilable (the SafeBuffer/Volatile subtree), and MemoryStream over a
            // copy is the same observable Stream: readable, seekable, not writable,
            // Length = the resource size. A missing resource is null, exactly as .NET.
            // The (Type, string) overload's namespace scoping happens in the runtime
            // helper, which is where the Type's namespace is available.
            case ("System.Reflection.Assembly", "GetManifestResourceStream")
                when sig.ParameterTypes.Length is 1 or 2 && sig.ParameterTypes[^1].IsString:
            {
                if (Comp.ReachManagedMethod("System.IO.MemoryStream", ".ctor",
                        static ps => ps is
                            [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Byte },
                             { Primitive: PrimitiveTypeCode.Boolean }],
                        allocates: true) is not { } msCtor)
                    return false;
                Comp.NoteManifestResourceUse();
                var resName = Pop();
                string scope = "nullptr";
                if (sig.ParameterTypes.Length == 2)
                    scope = Cast(Pop(), "Dn2CppType*");
                var asm = Pop();
                var byteElem = TypeDesc.MakePrimitive(PrimitiveTypeCode.Byte);
                Comp.NoteArrayElementType(byteElem); // emit ti_arr_Byte — GetType() is Byte[]
                string bytes = NewTemp("Dn2CppArrayN*");
                Emit($"{bytes} = dn2cpp_assembly_get_manifest_resource_bytes("
                    + $"{Cast(asm, "const char*")}, {scope}, {Cast(resName, "Dn2CppString*")}, "
                    + $"{PreciseArrayTypeInfoExpr(byteElem)});");
                var msCls = msCtor.DeclaringClass;
                string ms = NewTemp(msCls.CppStructName + "*");
                Emit($"{ms} = ({bytes} == nullptr) ? nullptr "
                    + $": ({msCls.CppStructName}*)dn2cpp_alloc(sizeof({msCls.CppStructName}));");
                Emit($"if ({ms} != nullptr) ((Dn2CppObject*){ms})->type = &{msCls.CppTypeInfoName};");
                Emit($"if ({ms} != nullptr) {DirectCall(msCtor, new List<string> { ms, bytes, "0" })};");
                PushEntry(new StackEntry(ms, StackKind.Ref, msCls.CppStructName + "*",
                    StaticType: TypeDesc.MakeClass(msCls)));
                return true;
            }
            // Assembly.GetManifestResourceInfo(string): a real managed
            // ManifestResourceInfo for an embedded resource — ReferencedAssembly and
            // FileName null, ResourceLocation Embedded|ContainedInManifestFile (5) — or
            // null when the resource is absent, matching real .NET. Assemblies here are
            // single-module and carry no linked resources, so the other two
            // ResourceLocation shapes cannot arise.
            case ("System.Reflection.Assembly", "GetManifestResourceInfo")
                when sig.ParameterTypes is [{ IsString: true }]:
            {
                if (Comp.ReachManagedMethod("System.Reflection.ManifestResourceInfo", ".ctor",
                        static ps => ps.Length == 3, allocates: true) is not { } infoCtor)
                    return false;
                Comp.NoteManifestResourceUse();
                var resName = Pop();
                var asm = Pop();
                string has = NewTemp("int32_t");
                Emit($"{has} = dn2cpp_assembly_has_manifest_resource("
                    + $"{Cast(asm, "const char*")}, {Cast(resName, "Dn2CppString*")});");
                var infoCls = infoCtor.DeclaringClass;
                string info = NewTemp(infoCls.CppStructName + "*");
                Emit($"{info} = ({has} == 0) ? nullptr "
                    + $": ({infoCls.CppStructName}*)dn2cpp_alloc(sizeof({infoCls.CppStructName}));");
                Emit($"if ({info} != nullptr) ((Dn2CppObject*){info})->type = &{infoCls.CppTypeInfoName};");
                Emit($"if ({info} != nullptr) "
                    + $"{DirectCall(infoCtor, new List<string> { info, "nullptr", "nullptr", "5" })};");
                PushEntry(new StackEntry(info, StackKind.Ref, infoCls.CppStructName + "*",
                    StaticType: TypeDesc.MakeClass(infoCls)));
                return true;
            }
            // Every other manifest-resource shape has no model: the resource table is
            // keyed by assembly and carries embedded resources only. Loud and catchable
            // rather than a null, which in real .NET means "no such resource" and would
            // be a wrong answer. The three arms above are the whole net10 surface —
            // `Assembly` declares exactly GetManifestResourceInfo(string),
            // GetManifestResourceNames(), GetManifestResourceStream(string) and
            // (Type, string), and `System.Reflection.Module` declares no resource API at
            // all on .NET Core — so this default guards a FUTURE overload, not a known
            // gap.
            case ("System.Reflection.Assembly",
                "GetManifestResourceStream" or "GetManifestResourceNames" or "GetManifestResourceInfo"):
            {
                for (int i = 0; i <= sig.ParameterTypes.Length; i++)
                    Pop(); // args + instance receiver
                Emit($"dn2cpp_throw_platform_not_supported(\"Assembly.{name}{SigArgs(sig)} is not modeled in AOT-compiled code (only an assembly's own embedded manifest resources are carried)\");");
                // Unreachable; stack typing only. The base object pointer avoids
                // naming the return type's opaque shell (t_System_IO_Stream),
                // which is never emitted for a body that only trips this cut.
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
                return true;
            }
            case ("System.Type", "op_Equality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_equals({Cast(a, "Dn2CppType*")}, {Cast(b, "Dn2CppType*")})");
                return true;
            }
            case ("System.Type", "op_Inequality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"!dn2cpp_type_equals({Cast(a, "Dn2CppType*")}, {Cast(b, "Dn2CppType*")})");
                return true;
            }
            // MemberInfo.Name: shared by Type and FieldInfo, so the runtime helper
            // dispatches on the managed object header. Type.Name (if dispatched
            // directly on Type) is the simple name (after the last '.'), unlike
            // ToString/FullName.
            case ("System.Reflection.MemberInfo", "get_Name"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_memberinfo_name({Cast(a, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Type", "get_Name"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_type_name(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // MemberInfo.DeclaringType: a FieldInfo's owning type (a Type's enclosing
            // type isn't modeled → null for top-level, matching.NET).
            case ("System.Reflection.MemberInfo", "get_DeclaringType"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_memberinfo_declaring_type({Cast(a, "Dn2CppObject*")})");
                return true;
            }
            // MemberInfo.ReflectedType: the type the member was obtained through,
            // stamped on the handle at mint time. One case serves every member kind
            // because the property is declared on MemberInfo alone (no MethodBase/
            // MethodInfo/FieldInfo/PropertyInfo redeclaration in the public surface),
            // so every call token names MemberInfo.
            case ("System.Reflection.MemberInfo", "get_ReflectedType"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_memberinfo_reflected_type({Cast(a, "Dn2CppObject*")})");
                return true;
            }
            // Reflection field enumeration. Type.GetFields([flags]) -> FieldInfo[],
            // Type.GetField(name[, flags]) -> FieldInfo or null. The no-flags overloads use
            // the.NET default BindingFlags.Public | Instance | Static (= 28).
            case ("System.Type", "GetFields") when sig.ParameterTypes.Length == 0:
            {
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_fields({Cast(t, "Dn2CppType*")}, 28)", sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetFields") when sig.ParameterTypes.Length == 1:
            {
                var flags = Pop();
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_fields({Cast(t, "Dn2CppType*")}, {Cast(flags, "int32_t")})", sig.ReturnType);
                return true;
            }
            // TypeInfo.DeclaredFields: this type's own field rows, every visibility,
            // instance and static (BindingFlags DeclaredOnly|Instance|Static|Public|
            // NonPublic = 62), surfaced as an IEnumerable<FieldInfo>-typed FieldInfo[]
            // (real .NET's RuntimeType answers an array too). Without this arm the
            // callvirt dispatches through dn2cpp_type_type's null vtable.
            case ("System.Reflection.TypeInfo", "get_DeclaredFields"):
            {
                var t = Pop();
                PushAttributeArray($"dn2cpp_type_get_fields({Cast(t, "Dn2CppType*")}, 62)", sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetField") when sig.ParameterTypes.Length == 1:
            {
                var n = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"((Dn2CppObject*)dn2cpp_type_get_field({Cast(t, "Dn2CppType*")}, {Cast(n, "Dn2CppString*")}, 28))");
                return true;
            }
            case ("System.Type", "GetField") when sig.ParameterTypes.Length == 2:
            {
                var flags = Pop();
                var n = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"((Dn2CppObject*)dn2cpp_type_get_field({Cast(t, "Dn2CppType*")}, {Cast(n, "Dn2CppString*")}, {Cast(flags, "int32_t")}))");
                return true;
            }
            case ("System.Reflection.FieldInfo", "get_FieldType"):
            {
                var f = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_fieldref_field_type((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            case ("System.Reflection.FieldInfo", "get_IsStatic"):
            {
                var f = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_fieldref_is_static((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            case ("System.Reflection.FieldInfo", "get_IsInitOnly"):
            {
                var f = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_fieldref_is_initonly((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            case ("System.Reflection.FieldInfo", "get_IsLiteral"):
            {
                var f = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_fieldref_is_literal((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            case ("System.Reflection.FieldInfo", "get_IsPublic"):
            {
                var f = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_fieldref_is_public((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            // FieldInfo.GetValue(obj) / SetValue(obj, value): obj is the
            // instance (null for a static field); value types box on get / unbox on set.
            case ("System.Reflection.FieldInfo", "GetValue") when sig.ParameterTypes.Length == 1:
            {
                var obj = Pop();
                var f = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_fieldref_get_value((Dn2CppFieldRef*)({f.Expr}), {Cast(obj, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Reflection.FieldInfo", "SetValue") when sig.ParameterTypes.Length == 2:
            {
                var value = Pop();
                var obj = Pop();
                var f = Pop();
                Emit($"dn2cpp_fieldref_set_value((Dn2CppFieldRef*)({f.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")});");
                return true;
            }
            // The abstract 5-arg SetValue(obj, value, BindingFlags, Binder,
            // CultureInfo): the extra arguments select invocation binding, which
            // dn2cpp does not model beyond the default — drop them and store like
            // the 2-arg form.
            case ("System.Reflection.FieldInfo", "SetValue") when sig.ParameterTypes.Length == 5:
            {
                Pop(); // culture
                Pop(); // binder (null in every supported caller)
                Pop(); // invokeAttr
                var value = Pop();
                var obj = Pop();
                var f = Pop();
                Emit($"dn2cpp_fieldref_set_value((Dn2CppFieldRef*)({f.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")});");
                return true;
            }
            // Reflection method enumeration. Type.GetMethods([flags]) ->
            // MethodInfo[], Type.GetMethod(name[, flags]) -> MethodInfo or null. The
            // no-flags overloads use the default BindingFlags.Public|Instance|Static (28).
            case ("System.Type", "GetMethods") when sig.ParameterTypes.Length == 0:
            {
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_methods({Cast(t, "Dn2CppType*")}, 28)", sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetMethods") when sig.ParameterTypes.Length == 1:
            {
                var flags = Pop();
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_methods({Cast(t, "Dn2CppType*")}, {Cast(flags, "int32_t")})", sig.ReturnType);
                return true;
            }
            // Type.GetMethod — the ENTIRE public overload surface. Every overload
            // is "name plus a subset of {int genericParameterCount, BindingFlags,
            // Binder, CallingConventions, Type[] types, ParameterModifier[]}", so
            // the arguments are classified by parameter type instead of
            // enumerating arities (see PopLookupArgs) and every overload over that
            // vocabulary maps onto the unified runtime lookup. Semantics cuts:
            // a non-null Binder throws a catchable PlatformNotSupportedException
            // at run time (Type.DefaultBinder is modeled as null, so the BCL's
            // null/DefaultBinder calls stay supported); ParameterModifier[] is
            // ignored (CoreCLR ignores it outside COM interop);
            // CallingConventions excludes methods only when it demands VarArgs,
            // which no transpiled method is (null result, matching real .NET).
            // Defaults mirror .NET: BindingFlags Public|Instance|Static (28).
            case ("System.Type", "GetMethod"):
            {
                if (PopLookupArgs(sig, name: true, generic: true, returnType: false) is not { } ma)
                    return false;
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_type_get_method_full({Cast(t, "Dn2CppType*")}, {ma.Name}, {ma.GenericCount ?? "-1"}, {ma.Types ?? "nullptr"}, {ma.Flags ?? "28"}, {ma.CallConv ?? "0"}, {ma.Binder ?? "nullptr"}))");
                return true;
            }
            case ("System.Reflection.MethodInfo", "get_ReturnType"):
            {
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_methodref_return_type((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            // MethodBase.ContainsGenericParameters: the reflection surface only
            // exposes closed methods (methtab rows are per-instantiation), so no
            // observable method ever carries an open parameter — constantly false.
            // This includes the GetGenericMethodDefinition view, whose arguments
            // stay closed (real .NET answers true there; DECLARED DIVERGENCE).
            case ("System.Reflection.MethodBase", "get_ContainsGenericParameters"):
            {
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            }
            // MethodBase.IsGenericMethodDefinition: true only for the
            // definition-view handle GetGenericMethodDefinition returns (see the
            // Dn2CppMethodRef::isGenericDefView model note); every enumerated /
            // looked-up row is a closed instantiation and answers false — where
            // real .NET's GetMethod would have surfaced the open definition
            // (DECLARED DIVERGENCE).
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo",
                "get_IsGenericMethodDefinition"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_generic_def((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            // MethodInfo.MakeGenericMethod(Type[]): in-image resolution against
            // the declaring type's compiled instantiation rows; a never-reached
            // instantiation throws the catchable PlatformNotSupportedException
            // (the AOT boundary, like MakeGenericType).
            case ("System.Reflection.MethodInfo", "MakeGenericMethod") when sig.ParameterTypes.Length == 1:
            {
                var types = Pop();
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_methodref_make_generic((Dn2CppMethodRef*)({m.Expr}), {Cast(types, "Dn2CppArrayRef*")}))");
                return true;
            }
            // MethodInfo.GetGenericMethodDefinition: the definition view over the
            // receiver's row (equality = definition token identity; MakeGenericMethod
            // on it re-resolves in-image).
            case ("System.Reflection.MethodInfo", "GetGenericMethodDefinition") when sig.ParameterTypes.Length == 0:
            {
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_methodref_get_generic_definition((Dn2CppMethodRef*)({m.Expr})))");
                return true;
            }
            // MethodBase.GetGenericArguments: the row's closed type arguments
            // (empty for a non-generic method; a definition view reports the
            // wrapped row's closed arguments — the documented divergence).
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo",
                "GetGenericArguments") when sig.ParameterTypes.Length == 0:
            {
                var m = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_methodref_get_generic_arguments((Dn2CppMethodRef*)({m.Expr}))", sig.ReturnType);
                return true;
            }
            // MethodInfo.GetBaseDefinition: the shallowest base-chain ancestor
            // declaring the receiver's vtable slot; a non-virtual method is its
            // own base definition.
            case ("System.Reflection.MethodInfo", "GetBaseDefinition") when sig.ParameterTypes.Length == 0:
            {
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_methodref_get_base_definition((Dn2CppMethodRef*)({m.Expr})))");
                return true;
            }
            // MethodBase.CallingConvention: no transpiled method is ever VarArgs,
            // so Standard (| HasThis for instance methods) is exact.
            case ("System.Reflection.MethodBase", "get_CallingConvention"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(dn2cpp_methodref_is_static((Dn2CppMethodRef*)({m.Expr})) != 0 ? 0x1 : 0x21)");
                return true;
            }
            case ("System.Reflection.MethodBase", "get_IsStatic"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_static((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase", "get_IsSpecialName"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_specialname((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase", "get_IsPublic"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_public((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase", "GetParameters"):
            {
                var m = Pop();
                PushReflectionMemberArray($"dn2cpp_methodref_get_parameters((Dn2CppMethodRef*)({m.Expr}))", sig.ReturnType);
                return true;
            }
            // MethodInfo.Invoke(object obj, object[] args): dispatch through the
            // method's invoker thunk. args is the boxed-argument object[] (null/empty for
            // a no-arg method); obj is the instance (null for static). Returns the boxed
            // result (null for void). The 5-arg binder overload is out of scope.
            case ("System.Reflection.MethodBase", "Invoke") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1].Kind == TypeKind.SZArray:
            {
                var args = Pop();
                var obj = Pop();
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_methodref_invoke((Dn2CppMethodRef*)({m.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(args, "Dn2CppArrayRef*")})");
                return true;
            }
            case ("System.Reflection.MethodBase", "Invoke") when sig.ParameterTypes.Length == 5
                && sig.ParameterTypes[3].Kind == TypeKind.SZArray:
            {
                Pop(); // CultureInfo
                var args = Pop(); // object[]
                Pop(); // Binder
                Pop(); // BindingFlags
                var obj = Pop();
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_methodref_invoke((Dn2CppMethodRef*)({m.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(args, "Dn2CppArrayRef*")})");
                return true;
            }
            // MethodInfo.CreateDelegate(Type[, object target]): bind the methtab row
            // into a fresh delegate of the requested type via the emitted
            // reflection-bind registry (boxed-invoker dispatch; see
            // CppEmitter.EmitDelegateReflBinds). The 2-arg form is an explicit-target
            // overload, so it admits the null-bound closed-instance shape like real
            // .NET. The generic CreateDelegate<T> forms route through
            // TranslateGenericIntrinsic to the same binder.
            case ("System.Reflection.MethodInfo", "CreateDelegate") when sig.ParameterTypes.Length is 1 or 2:
            {
                Comp.NeedsReflectionDelegateBind = true;
                string target = "nullptr";
                int closedForm = 0;
                if (sig.ParameterTypes.Length == 2)
                {
                    var t = Pop();
                    target = Cast(t, "Dn2CppObject*");
                    closedForm = 1;
                }
                var ty = Pop();
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_delegate_create({Cast(ty, "Dn2CppType*")}, {target}, (Dn2CppMethodRef*)({m.Expr}), {closedForm}, 1)");
                return true;
            }
            // Reflection constructor enumeration + invocation. GetConstructors
            // ([flags]) -> ConstructorInfo[], GetConstructor(Type[][, flags]) -> the
            // matching ctor or null. The no-flags overloads use BindingFlags.Public |
            // Instance (= 20). ConstructorInfo.Invoke(object[]) allocates + constructs.
            case ("System.Type", "GetConstructors") when sig.ParameterTypes.Length == 0:
            {
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_constructors({Cast(t, "Dn2CppType*")}, 20)", sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetConstructors") when sig.ParameterTypes.Length == 1:
            {
                var flags = Pop();
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_constructors({Cast(t, "Dn2CppType*")}, {Cast(flags, "int32_t")})", sig.ReturnType);
                return true;
            }
            // Type.GetConstructor — the ENTIRE public overload surface ("Type[]
            // types plus a subset of {BindingFlags, Binder, CallingConventions,
            // ParameterModifier[]}"), classified like GetMethod above with the
            // same Binder/modifiers/CallingConventions semantics cuts. Defaults
            // mirror .NET: BindingFlags Public|Instance (20).
            case ("System.Type", "GetConstructor"):
            {
                if (PopLookupArgs(sig, name: false, generic: false, returnType: false) is not { } ca)
                    return false;
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_type_get_constructor_full({Cast(t, "Dn2CppType*")}, {ca.Types}, {ca.Flags ?? "20"}, {ca.CallConv ?? "0"}, {ca.Binder ?? "nullptr"}))");
                return true;
            }
            case ("System.Reflection.ConstructorInfo", "Invoke") when sig.ParameterTypes.Length == 1
                && sig.ParameterTypes[0].Kind == TypeKind.SZArray:
            {
                var args = Pop();
                var c = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_ctorref_invoke((Dn2CppMethodRef*)({c.Expr}), {Cast(args, "Dn2CppArrayRef*")})");
                return true;
            }
            case ("System.Reflection.ConstructorInfo", "Invoke") when sig.ParameterTypes.Length == 4
                && sig.ParameterTypes[2].Kind == TypeKind.SZArray:
            {
                Pop(); // CultureInfo
                var args = Pop(); // object[]
                Pop(); // Binder
                Pop(); // BindingFlags
                var c = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_ctorref_invoke((Dn2CppMethodRef*)({c.Expr}), {Cast(args, "Dn2CppArrayRef*")})");
                return true;
            }
            // Non-generic Activator.CreateInstance(Type): invoke the type's
            // parameterless ctor (or zero-init a value type that has none). The generic
            // CreateInstance<T> is lowered in TranslateGenericIntrinsic.
            case ("System.Activator", "CreateInstance") when sig.ParameterTypes.Length == 1
                && sig.ParameterTypes[0].Kind != TypeKind.SZArray:
            {
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_activator_create_instance({Cast(t, "Dn2CppType*")})");
                return true;
            }
            // Activator.CreateInstance(Type, bool nonPublic): the parameterless
            // form with the visibility switch.
            case ("System.Activator", "CreateInstance") when sig.ParameterTypes.Length == 2
                && !sig.ParameterTypes[0].IsString
                && sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }:
            {
                var np = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_activator_create_instance_nonpublic({Cast(t, "Dn2CppType*")}, {Cast(np, "int32_t")})");
                return true;
            }
            // Activator.CreateInstance(Type, params object[] args) — DefaultBinder
            // ctor resolution in the runtime (arity + boxed-arg admissibility +
            // most-specific disambiguation; MissingMethodException on no match).
            // Default BindingFlags mirror .NET: Instance|Public (20).
            case ("System.Activator", "CreateInstance") when sig.ParameterTypes.Length == 2
                && !sig.ParameterTypes[0].IsString
                && sig.ParameterTypes[1].Kind == TypeKind.SZArray:
            {
                var args = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_activator_create_instance_args({Cast(t, "Dn2CppType*")}, {Cast(args, "Dn2CppArrayRef*")}, 20, nullptr, nullptr)");
                return true;
            }
            // Activator.CreateInstance(Type, object[] args, object[]
            // activationAttributes): non-empty attributes throw the
            // PlatformNotSupportedException real .NET raises.
            case ("System.Activator", "CreateInstance") when sig.ParameterTypes.Length == 3
                && !sig.ParameterTypes[0].IsString
                && sig.ParameterTypes[1].Kind == TypeKind.SZArray
                && sig.ParameterTypes[2].Kind == TypeKind.SZArray:
            {
                var attrs = Pop();
                var args = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_activator_create_instance_args({Cast(t, "Dn2CppType*")}, {Cast(args, "Dn2CppArrayRef*")}, 20, nullptr, {Cast(attrs, "Dn2CppArrayRef*")})");
                return true;
            }
            // Activator.CreateInstance(Type, BindingFlags, Binder, object[],
            // CultureInfo[, object[] activationAttributes]): flags respected, a
            // non-null Binder is the AOT PNSE (the member-lookup posture), culture ignored
            // (the DefaultBinder's non-parse path never reads it).
            case ("System.Activator", "CreateInstance") when sig.ParameterTypes.Length is 5 or 6
                && !sig.ParameterTypes[0].IsString
                && sig.ParameterTypes[3].Kind == TypeKind.SZArray:
            {
                string actAttrs = sig.ParameterTypes.Length == 6 ? Cast(Pop(), "Dn2CppArrayRef*") : "nullptr";
                Pop(); // CultureInfo — ignored
                var args = Pop();
                var binder = Pop();
                var flags = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_activator_create_instance_args({Cast(t, "Dn2CppType*")}, {Cast(args, "Dn2CppArrayRef*")}, {Cast(flags, "int32_t")}, {Cast(binder, "Dn2CppObject*")}, {actAttrs})");
                return true;
            }
            // Reflection property enumeration + access. GetProperties([flags]) ->
            // PropertyInfo[], GetProperty(name[, flags]) -> the match or null. The no-flags
            // overloads use the default BindingFlags.Public|Instance|Static (= 28).
            case ("System.Type", "GetProperties") when sig.ParameterTypes.Length == 0:
            {
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_properties({Cast(t, "Dn2CppType*")}, 28)", sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetProperties") when sig.ParameterTypes.Length == 1:
            {
                var flags = Pop();
                var t = Pop();
                PushReflectionMemberArray($"dn2cpp_type_get_properties({Cast(t, "Dn2CppType*")}, {Cast(flags, "int32_t")})", sig.ReturnType);
                return true;
            }
            // Type.GetProperty — the ENTIRE public overload surface ("name plus a
            // subset of {BindingFlags, Binder, Type returnType, Type[] types,
            // ParameterModifier[]}"), classified like GetMethod above with the
            // same Binder/modifiers semantics cuts. returnType filters on the
            // property type and types on the indexer parameter list (read off
            // the accessor rows), so overloaded indexers resolve exactly.
            // Defaults mirror .NET: BindingFlags Public|Instance|Static (28).
            case ("System.Type", "GetProperty"):
            {
                if (PopLookupArgs(sig, name: true, generic: false, returnType: true) is not { } pa)
                    return false;
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_type_get_property_full({Cast(t, "Dn2CppType*")}, {pa.Name}, {pa.Flags ?? "28"}, {pa.ReturnType ?? "nullptr"}, {pa.Types ?? "nullptr"}, {pa.Binder ?? "nullptr"}))");
                return true;
            }
            // Type.GetMember(name[, MemberTypes][, BindingFlags]) — MemberInfo[]
            // of every matching method/ctor/property/field/nested type; `name`
            // supports the documented trailing-'*' prefix wildcard. GetMembers
            // ([flags]) is the match-everything form; GetDefaultMembers() the
            // [DefaultMember]-driven one (the name is stamped on the type-info
            // at emit time — the attribute itself is a framework attribute,
            // outside the reflected attribute tables). MemberTypes.All == 191.
            case ("System.Type", "GetMember") when sig.ParameterTypes.Length is 1 or 2 or 3
                && sig.ParameterTypes[0].IsString:
            {
                string mflags = "28", memberTypes = "191";
                if (sig.ParameterTypes.Length == 3)
                {
                    mflags = Cast(Pop(), "int32_t");
                    memberTypes = Cast(Pop(), "int32_t");
                }
                else if (sig.ParameterTypes.Length == 2)
                {
                    mflags = Cast(Pop(), "int32_t");
                }
                var n = Pop();
                var t = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_type_get_member({Cast(t, "Dn2CppType*")}, {Cast(n, "Dn2CppString*")}, {memberTypes}, {mflags})",
                    sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetMembers") when sig.ParameterTypes.Length is 0 or 1:
            {
                string mflags = sig.ParameterTypes.Length == 1 ? Cast(Pop(), "int32_t") : "28";
                var t = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_type_get_members({Cast(t, "Dn2CppType*")}, {mflags})", sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetDefaultMembers") when sig.ParameterTypes.Length == 0:
            {
                var t = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_type_get_default_members({Cast(t, "Dn2CppType*")})", sig.ReturnType);
                return true;
            }
            // Type.GetMemberWithSameMetadataDefinitionAs(MemberInfo): matched by
            // (metadata token, defining assembly), so it crosses generic
            // instantiations; ArgumentException when this type has no match.
            case ("System.Type", "GetMemberWithSameMetadataDefinitionAs")
                when sig.ParameterTypes.Length == 1:
            {
                var mem = Pop();
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_type_get_member_same_metadata({Cast(t, "Dn2CppType*")}, {Cast(mem, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Reflection.PropertyInfo", "get_PropertyType"):
            {
                var p = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_propref_property_type((Dn2CppPropRef*)({p.Expr}))");
                return true;
            }
            case ("System.Reflection.PropertyInfo", "get_CanRead"):
            {
                var p = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_propref_can_read((Dn2CppPropRef*)({p.Expr}))");
                return true;
            }
            case ("System.Reflection.PropertyInfo", "get_CanWrite"):
            {
                var p = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_propref_can_write((Dn2CppPropRef*)({p.Expr}))");
                return true;
            }
            // PropertyInfo.GetValue(obj) / SetValue(obj, value) — the non-indexed forms
            // (indexed properties are out of scope). GetValue boxes; SetValue unboxes via
            // the accessor invoker thunks.
            // GetGetMethod/GetSetMethod (+ the GetMethod/SetMethod properties,
            // which include non-public accessors): the accessor's reflected
            // MethodInfo, or null. The no-arg overloads pass nonPublic: false.
            case ("System.Reflection.PropertyInfo", "GetGetMethod" or "GetSetMethod")
                when sig.ParameterTypes.Length is 0 or 1:
            {
                string nonPublic = sig.ParameterTypes.Length == 1 ? Cast(Pop(), "int32_t") : "0";
                var p = Pop();
                string setter = name == "GetSetMethod" ? "1" : "0";
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_propref_accessor((Dn2CppPropRef*)({p.Expr}), {setter}, {nonPublic}))");
                return true;
            }
            case ("System.Reflection.PropertyInfo", "get_GetMethod" or "get_SetMethod"):
            {
                var p = Pop();
                string setter = name == "get_SetMethod" ? "1" : "0";
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"((Dn2CppObject*)dn2cpp_propref_accessor((Dn2CppPropRef*)({p.Expr}), {setter}, 1))");
                return true;
            }
            case ("System.Reflection.PropertyInfo", "GetValue") when sig.ParameterTypes.Length == 1:
            {
                var obj = Pop();
                var p = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_propref_get_value((Dn2CppPropRef*)({p.Expr}), {Cast(obj, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Reflection.PropertyInfo", "SetValue") when sig.ParameterTypes.Length == 2:
            {
                var value = Pop();
                var obj = Pop();
                var p = Pop();
                Emit($"dn2cpp_propref_set_value((Dn2CppPropRef*)({p.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")});");
                return true;
            }
            // Indexed properties. GetIndexParameters() reads the indexer
            // parameters off the accessor rows; the GetValue(obj, object[]
            // index) / SetValue(obj, value, object[] index) forms route the
            // indices (+ trailing value) through the accessor invoker. The
            // BindingFlags/Binder/CultureInfo forms fold onto the same helpers
            // (non-null Binder -> AOT PNSE at runtime; flags/culture are no-ops
            // on an already-resolved PropertyInfo, like real .NET).
            case ("System.Reflection.PropertyInfo", "GetIndexParameters") when sig.ParameterTypes.Length == 0:
            {
                var p = Pop();
                PushReflectionMemberArray($"dn2cpp_propref_get_index_parameters((Dn2CppPropRef*)({p.Expr}))", sig.ReturnType);
                return true;
            }
            case ("System.Reflection.PropertyInfo", "GetValue") when sig.ParameterTypes.Length == 2
                && sig.ParameterTypes[1].Kind == TypeKind.SZArray:
            {
                var index = Pop();
                var obj = Pop();
                var p = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_propref_get_value_indexed((Dn2CppPropRef*)({p.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(index, "Dn2CppArrayRef*")}, nullptr)");
                return true;
            }
            case ("System.Reflection.PropertyInfo", "SetValue") when sig.ParameterTypes.Length == 3
                && sig.ParameterTypes[2].Kind == TypeKind.SZArray:
            {
                var index = Pop();
                var value = Pop();
                var obj = Pop();
                var p = Pop();
                Emit($"dn2cpp_propref_set_value_indexed((Dn2CppPropRef*)({p.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")}, {Cast(index, "Dn2CppArrayRef*")}, nullptr);");
                return true;
            }
            // GetValue(obj, BindingFlags, Binder, object[] index, CultureInfo).
            case ("System.Reflection.PropertyInfo", "GetValue") when sig.ParameterTypes.Length == 5
                && sig.ParameterTypes[3].Kind == TypeKind.SZArray:
            {
                Pop(); // CultureInfo — ignored
                var index = Pop();
                var binder = Pop();
                Pop(); // BindingFlags — a resolved PropertyInfo no longer filters
                var obj = Pop();
                var p = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_propref_get_value_indexed((Dn2CppPropRef*)({p.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(index, "Dn2CppArrayRef*")}, {Cast(binder, "Dn2CppObject*")})");
                return true;
            }
            // SetValue(obj, value, BindingFlags, Binder, object[] index, CultureInfo).
            case ("System.Reflection.PropertyInfo", "SetValue") when sig.ParameterTypes.Length == 6
                && sig.ParameterTypes[4].Kind == TypeKind.SZArray:
            {
                Pop(); // CultureInfo — ignored
                var index = Pop();
                var binder = Pop();
                Pop(); // BindingFlags
                var value = Pop();
                var obj = Pop();
                var p = Pop();
                Emit($"dn2cpp_propref_set_value_indexed((Dn2CppPropRef*)({p.Expr}), {Cast(obj, "Dn2CppObject*")}, {Cast(value, "Dn2CppObject*")}, {Cast(index, "Dn2CppArrayRef*")}, {Cast(binder, "Dn2CppObject*")});");
                return true;
            }
            // ParameterInfo members: ParameterType / Position / Name.
            case ("System.Reflection.ParameterInfo", "get_ParameterType"):
            {
                var p = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_paramref_parameter_type((Dn2CppParamRef*)({p.Expr}))");
                return true;
            }
            case ("System.Reflection.ParameterInfo", "get_Position"):
            {
                var p = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_paramref_position((Dn2CppParamRef*)({p.Expr}))");
                return true;
            }
            case ("System.Reflection.ParameterInfo", "get_Name"):
            {
                var p = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_paramref_name((Dn2CppParamRef*)({p.Expr}))");
                return true;
            }
            // IntrospectionExtensions.GetTypeInfo(Type): the identity — a
            // runtime Type handle IS the TypeInfo (real .NET's RuntimeType :
            // TypeInfo returns itself; the TypeDelegator wrap only applies to
            // custom IReflectableType implementations, which don't exist here).
            case ("System.Reflection.IntrospectionExtensions", "GetTypeInfo")
                when sig.ParameterTypes.Length == 1:
            {
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppType*", Cast(t, "Dn2CppType*"));
                return true;
            }
            // MethodInfo.ReturnParameter: a synthesized ParameterInfo over the
            // return type (Position -1, Name null), matching real .NET.
            case ("System.Reflection.MethodInfo", "get_ReturnParameter"):
            {
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_methodref_return_parameter((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Type", "ToString"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_type_tostring(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // Reflection-lite scalar Type properties: runtime helpers reading
            // Dn2CppTypeInfo, so they answer via a runtime GetType too — not only the
            // static-fold path of typeof(T). get_IsValueType stays the static fold.
            case ("System.Type", "get_FullName"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_type_fullname(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_Namespace"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_type_namespace(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsEnum"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_enum(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // Type.IsSZArray (single-rank array) and Type.IsNested — flag reads with
            // the same static-token fold the other Type properties use.
            case ("System.Type", "get_IsSZArray"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } sk
                    ? (sk.Kind == TypeKind.SZArray ? "1" : "0")
                    : $"dn2cpp_type_is_szarray(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsNested"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } nk
                    ? (IsNestedStatic(nk) ? "1" : "0")
                    : $"dn2cpp_type_is_nested(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsArray"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_array(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsInterface"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_interface(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsAbstract"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_abstract(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_BaseType"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_base_type({Cast(a, "Dn2CppType*")})");
                return true;
            }
            // Type.GetElementType/GetArrayRank: read the per-element array
            // type-info's elementType/arrayRank, so a Type obtained via GetType("T[]")
            // reports its element type and rank.
            case ("System.Type", "GetElementType") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_element_type({Cast(a, "Dn2CppType*")})");
                return true;
            }
            case ("System.Type", "GetArrayRank") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_get_array_rank({Cast(a, "Dn2CppType*")})");
                return true;
            }
            case ("System.Type", "IsAssignableFrom") when sig.ParameterTypes.Length == 1:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_assignable_from({Cast(a, "Dn2CppType*")}, {Cast(b, "Dn2CppType*")})");
                return true;
            }
            // Type.IsAssignableTo(other): the argument-swapped IsAssignableFrom, but it
            // gets its OWN helper rather than the swapped call — the swap exchanges the
            // receiver's null with the argument's, and those mean opposite things (NRE
            // vs. a plain false), so a shared helper's receiver guard would turn
            // `t.IsAssignableTo(null)` into a throw.
            case ("System.Type", "IsAssignableTo") when sig.ParameterTypes.Length == 1:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_assignable_to({Cast(a, "Dn2CppType*")}, {Cast(b, "Dn2CppType*")})");
                return true;
            }
            case ("System.Type", "IsSubclassOf") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_subclass_of({Cast(a, "Dn2CppType*")}, {Cast(c, "Dn2CppType*")})");
                return true;
            }
            case ("System.Type", "IsInstanceOfType") when sig.ParameterTypes.Length == 1:
            {
                var obj = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_instance_of_type({Cast(a, "Dn2CppType*")}, {Cast(obj, "Dn2CppObject*")})");
                return true;
            }
            // Generic reflection: flag/metadata reads on the type-info, so they work
            // through a runtime GetType too. IsConstructedGenericType is the
            // closed-instantiation predicate (genericArgCount > 0); an open definition is
            // never an instance value here (no typeof(List<>) emit), so it can share the
            // IsGenericType helper.
            case ("System.Type", "get_IsGenericType"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_generic_type(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsConstructedGenericType"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_constructed_generic(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsGenericTypeDefinition"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_generic_type_definition(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_ContainsGenericParameters"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_contains_generic_parameters(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "GetGenericTypeDefinition") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_generic_type_definition({Cast(a, "Dn2CppType*")})");
                return true;
            }
            // GenericTypeArguments is GetGenericArguments restricted to closed
            // instantiations; open definitions are not modeled with type
            // parameters, so both read the same closed-argument table.
            case ("System.Type", "GetGenericArguments") when sig.ParameterTypes.Length == 0:
            case ("System.Type", "get_GenericTypeArguments"):
            {
                var a = Pop();
                // Through the member-array wrapper so the result carries the
                // precise Type[] type-info — BCL callers foreach it as
                // IEnumerable<Type>.
                PushReflectionMemberArray($"dn2cpp_type_get_generic_arguments({Cast(a, "Dn2CppType*")})", sig.ReturnType);
                return true;
            }
            case ("System.Type", "MakeGenericType") when sig.ParameterTypes.Length == 1:
            {
                var args = Pop();
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_make_generic({Cast(a, "Dn2CppType*")}, {Cast(args, "Dn2CppArrayRef*")})");
                return true;
            }
            // Type.IsEquivalentTo: COM type equivalence never applies here, so it
            // reduces to type identity — and reflected Type objects are interned
            // per handle, so pointer equality is exact (null other compares false).
            case ("System.Type", "IsEquivalentTo") when sig.ParameterTypes.Length == 1:
            {
                var other = Pop();
                var self = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(({Cast(self, "Dn2CppType*")}) == ({Cast(other, "Dn2CppType*")}) ? 1 : 0)");
                return true;
            }
            // Type.GenericParameterAttributes: the reflected registry holds only
            // closed types — no Type handle is ever an open generic parameter, so
            // the variance bits are constantly None.
            case ("System.Type", "get_GenericParameterAttributes"):
            {
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            }
            // Type.MakeByRefType/MakePointerType: byref/pointer Types aren't
            // modeled in the reflected type registry — a catchable runtime
            // NotSupportedException, like an AOT-ungenerated MakeGenericType.
            case ("System.Type", "MakeByRefType" or "MakePointerType") when sig.ParameterTypes.Length == 0:
            {
                Pop();
                Emit("dn2cpp_throw_not_supported();");
                Push(StackKind.Ref, "Dn2CppType*", "nullptr"); // unreachable; stack typing only
                return true;
            }
            // Type/Enum reflection completion.
            case ("System.Type", "GetInterfaces") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                // Precise Type[] type-info, as for GetGenericArguments above.
                PushReflectionMemberArray($"dn2cpp_type_get_interfaces({Cast(a, "Dn2CppType*")})", sig.ReturnType);
                return true;
            }
            // Type.GetInterface(name[, ignoreCase]): single-interface lookup over the
            // same table GetInterfaces reads. ignoreCase is passed through, not
            // dropped — it is observable (it folds the simple-name part only, as .NET).
            case ("System.Type", "GetInterface") when sig.ParameterTypes.Length is 1 or 2:
            {
                string ignoreCase = "0";
                if (sig.ParameterTypes.Length == 2)
                    ignoreCase = Cast(Pop(), "int32_t");
                var n = Pop();
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*",
                    $"dn2cpp_type_get_interface({Cast(a, "Dn2CppType*")}, {Cast(n, "Dn2CppString*")}, {ignoreCase})");
                return true;
            }
            case ("System.Type", "GetEnumUnderlyingType") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_enum_underlying({Cast(a, "Dn2CppType*")})");
                return true;
            }
            case ("System.Type", "GetEnumNames") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_enum_get_names({Cast(a, "Dn2CppType*")})", sig.ReturnType);
                return true;
            }
            // Nested types. The BindingFlags overloads route to the same runtime
            // helper, which returns the public nested set (NonPublic is a carve-out).
            case ("System.Type", "GetNestedTypes"):
            {
                if (sig.ParameterTypes.Length == 1) Pop(); // BindingFlags (ignored)
                var a = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_type_get_nested_types({Cast(a, "Dn2CppType*")})", sig.ReturnType);
                return true;
            }
            case ("System.Type", "GetNestedType"):
            {
                if (sig.ParameterTypes.Length == 2) Pop(); // BindingFlags (ignored)
                var n = Pop();
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_nested_type({Cast(a, "Dn2CppType*")}, {Cast(n, "Dn2CppString*")})");
                return true;
            }
            // Type.Equals(Type)/Equals(object): type identity. The header-dispatched
            // member equality handles a non-Type `object` other (compares false).
            case ("System.Type", "Equals") when sig.ParameterTypes.Length == 1:
            {
                var other = Pop();
                var self = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_memberinfo_equals({Cast(self, "Dn2CppObject*")}, {Cast(other, "Dn2CppObject*")})");
                return true;
            }
            // Type.UnderlyingSystemType: identity — every runtime Type here IS the
            // runtime type (no reflection-only Type objects exist).
            case ("System.Type", "get_UnderlyingSystemType"):
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", Cast(a, "Dn2CppType*"), a.TypeToken);
                return true;
            }
            // Type.HasElementType: array/byref/pointer. Only array Types materialize
            // at runtime; a static pointer/byref/array token folds like IsSZArray.
            case ("System.Type", "get_HasElementType"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", a.TypeToken is { } hk
                    ? (hk.Kind is TypeKind.SZArray or TypeKind.MDArray or TypeKind.Pointer or TypeKind.ByRef ? "1" : "0")
                    : $"dn2cpp_type_has_element_type(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "GetRootElementType") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_get_root_element_type({Cast(a, "Dn2CppType*")})");
                return true;
            }
            // Type.FormatTypeName (internal; MethodBase.ToString's type rendering).
            case ("System.Type", "FormatTypeName") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_type_format_type_name({Cast(a, "Dn2CppType*")})");
                return true;
            }
            // Function-pointer Types never materialize in this model (no typeof of a
            // function pointer is emitted, and GetType never yields one).
            case ("System.Type", "get_IsFunctionPointer" or "get_IsUnmanagedFunctionPointer"):
            // Type.IsCOMObject: COM interop is not modeled — constantly false.
            case ("System.Type", "get_IsCOMObject"):
            {
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            }
            // Type.MakeArrayType(): resolve the SZ-array Type against the AOT image;
            // an array type never statically instantiated throws NotSupportedException
            // (the same AOT boundary as MakeGenericType). The rank overload throws
            // the catchable NotSupportedException unconditionally: it denotes a
            // MULTIDIMENSIONAL array type even for rank 1 (T[*], not T[]), and MD
            // arrays have no reflected registry identity in the image.
            case ("System.Type", "MakeArrayType") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_type_make_array_type({Cast(a, "Dn2CppType*")})");
                return true;
            }
            case ("System.Type", "MakeArrayType") when sig.ParameterTypes.Length == 1:
            {
                Pop(); // rank
                Pop(); // the type receiver
                Emit("dn2cpp_throw_not_supported();"); // MD array types are not modeled
                Push(StackKind.Ref, "Dn2CppType*", "nullptr"); // unreachable; stack typing only
                return true;
            }
            case ("System.Type", "GetEnumValuesAsUnderlyingType") when sig.ParameterTypes.Length == 0:
            {
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_type_get_enum_values_as_underlying({Cast(a, "Dn2CppType*")})");
                return true;
            }
            // Type.FindInterfaces(TypeFilter, object): GetInterfaces() filtered
            // through the managed predicate delegate (invoked by the runtime helper).
            case ("System.Type", "FindInterfaces") when sig.ParameterTypes.Length == 2:
            {
                var criteria = Pop();
                var filter = Pop();
                var a = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_type_find_interfaces({Cast(a, "Dn2CppType*")}, {Cast(filter, "Dn2CppObject*")}, {Cast(criteria, "Dn2CppObject*")})",
                    sig.ReturnType);
                return true;
            }
            // Type.ImplementInterface (internal BCL helper behind TypeInfo.
            // IsAssignableFrom): interface-set membership over the dispatch tables.
            case ("System.Type", "ImplementInterface") when sig.ParameterTypes.Length == 1:
            {
                var itf = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_type_implement_interface({Cast(a, "Dn2CppType*")}, {Cast(itf, "Dn2CppType*")})");
                return true;
            }
            // Type.DefaultBinder: null. BCL callers on the reachable paths only
            // compare a caller-supplied binder against it (binder != Type.DefaultBinder
            // with a null binder), so a null singleton answers those exactly; no
            // binder-driven overload resolution is modeled.
            case ("System.Type", "get_DefaultBinder"):
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
                return true;
            // Type.Attributes + the visibility family: the raw ECMA TypeAttributes
            // word stamped by the emitter (synthesized from flags for hand-written
            // runtime handles).
            case ("System.Type", "get_Attributes"):
            {
                var a = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})dn2cpp_type_il_attrs(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsPublic"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_public(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsNotPublic"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_not_public(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            case ("System.Type", "get_IsVisible"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_visible(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // Type.IsNestedPublic: the ECMA visibility nibble == NestedPublic.
            case ("System.Type", "get_IsNestedPublic"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_type_is_nested_public(dn2cpp_type_require({Cast(a, "Dn2CppType*")}))");
                return true;
            }
            // Type.IsGenericParameter: always false at runtime — dn2cpp never
            // materializes an open generic-parameter Type value (the registry and
            // typeof both yield closed types only), like IsPointer/IsByRef.
            case ("System.Type", "get_IsGenericParameter"):
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            // Type.Module / MemberInfo.Module: modeled as the defining assembly's
            // simple-name handle (single-module assemblies) — the same const char*
            // an Assembly is, so Module equality is a name compare below.
            case ("System.Type", "get_Module"):
            {
                var a = Pop();
                Push(StackKind.Ref, "const char*", $"dn2cpp_type_assembly_name({Cast(a, "Dn2CppType*")})");
                // The static type distinguishes a Module handle from an Assembly
                // handle at an Object::ToString receiver (both ride as const char*).
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            }
            case ("System.Reflection.MemberInfo", "get_Module"):
            {
                var a = Pop();
                Push(StackKind.Ref, "const char*", $"dn2cpp_memberinfo_module({Cast(a, "Dn2CppObject*")})");
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            }
            case ("System.Reflection.Module", "op_Equality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_assembly_equals({Cast(a, "const char*")}, {Cast(b, "const char*")})");
                return true;
            }
            case ("System.Reflection.Module", "op_Inequality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"(!dn2cpp_assembly_equals({Cast(a, "const char*")}, {Cast(b, "const char*")}))");
                return true;
            }
            case ("System.Reflection.Module", "Equals") when sig.ParameterTypes.Length == 1:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_assembly_equals({Cast(a, "const char*")}, {Cast(b, "const char*")})");
                return true;
            }
            // Module.Assembly: identity under the single-module model — the Module
            // handle IS the assembly's simple-name handle. The pushed static type
            // flips the handle's Object::ToString answer back to the Assembly form.
            case ("System.Reflection.Module", "get_Assembly"):
            {
                var m = Pop();
                Push(StackKind.Ref, "const char*", Cast(m, "const char*"));
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            }
            // Module.Name / ScopeName / ToString: the manifest module's file name
            // "<AssemblyName>.dll" (matching real .NET). FullyQualifiedName maps to
            // the same (DECLARED DIVERGENCE: real .NET reports the full on-disk path,
            // which a native binary does not have).
            case ("System.Reflection.Module",
                "get_Name" or "get_ScopeName" or "get_FullyQualifiedName" or "ToString"):
            {
                var m = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_module_name({Cast(m, "const char*")})");
                return true;
            }
            // Module.ModuleHandle / ResolveType(int, …): raw metadata-token
            // machinery (RuntimeTypeCache-shaped callers) an AOT image cannot
            // serve — the tokens stamped on our rows identify members, but there is
            // no token->row index. Catchable PlatformNotSupportedException, like
            // MethodBase.MethodHandle.
            case ("System.Reflection.Module", "get_ModuleHandle"):
            {
                Pop();
                Emit("dn2cpp_throw_platform_not_supported(\"Module.ModuleHandle is not available in AOT-compiled code\");");
                // Unreachable; stack typing only — but the value-struct dummy names
                // the ModuleHandle layout, so force its (fieldful) emission.
                if (sig.ReturnType is { Kind: TypeKind.Class, Class: { } mhCls })
                    Comp.NoteForceEmit(mhCls);
                string hct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), hct, $"{hct}{{}}");
                return true;
            }
            case ("System.Reflection.Module", "ResolveType" or "ResolveMethod" or "ResolveField"
                or "ResolveMember" or "ResolveString" or "ResolveSignature"):
            {
                for (int i = 0; i <= sig.ParameterTypes.Length; i++)
                    Pop(); // args + instance receiver
                Emit($"dn2cpp_throw_platform_not_supported(\"Module.{name} is not available in AOT-compiled code (no metadata-token index)\");");
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr"); // unreachable; stack typing only
                return true;
            }
            // Module.ModuleVersionId: the on-disk metadata MVID is not recorded in
            // the AOT image, and its consumers are dynamic-codegen machinery anyway.
            case ("System.Reflection.Module", "get_ModuleVersionId"):
            {
                Pop();
                Emit("dn2cpp_throw_platform_not_supported(\"Module.ModuleVersionId is not available in AOT-compiled code (the metadata MVID is not recorded)\");");
                // Unreachable; stack typing only — the value-struct dummy names the
                // Guid layout, so force its (fieldful) emission.
                if (sig.ReturnType is { Kind: TypeKind.Class, Class: { } gCls })
                    Comp.NoteForceEmit(gCls);
                string gct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), gct, $"{gct}{{}}");
                return true;
            }
            // MemberInfo.CacheEquals (internal; behind MemberInfo.Equals): the same
            // wraps-the-same-metadata-entry equality as op_Equality.
            case ("System.Reflection.MemberInfo", "CacheEquals") when sig.ParameterTypes.Length == 1:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_memberinfo_equals({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})");
                return true;
            }
            // MemberInfo.MemberType/MetadataToken: header-dispatched (the receiver may
            // be any reflected handle). Constructors share the MethodRef representation,
            // discriminated by name in the runtime.
            case ("System.Reflection.MemberInfo" or "System.Type" or "System.Reflection.MethodBase"
                    or "System.Reflection.MethodInfo" or "System.Reflection.ConstructorInfo"
                    or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo",
                "get_MemberType"):
            {
                var a = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})dn2cpp_memberinfo_member_type({Cast(a, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Reflection.MemberInfo" or "System.Type" or "System.Reflection.MethodBase"
                    or "System.Reflection.MethodInfo" or "System.Reflection.ConstructorInfo"
                    or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo",
                "get_MetadataToken"):
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_memberinfo_metadata_token({Cast(a, "Dn2CppObject*")})");
                return true;
            }
            // MethodBase attribute flags and misc, read from the raw ECMA words the
            // emitter stamps on every method/ctor row.
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "get_Attributes"):
            {
                var m = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})dn2cpp_methodref_attributes((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo",
                "get_MethodImplementationFlags" or "GetMethodImplementationFlags"):
            {
                var m = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})dn2cpp_methodref_impl_flags((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "get_IsVirtual"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_virtual((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "get_IsAbstract"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_abstract((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "get_IsFinal"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_final((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "get_IsConstructor"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_constructor((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "get_IsGenericMethod"):
            {
                var m = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_methodref_is_generic((Dn2CppMethodRef*)({m.Expr}))");
                return true;
            }
            // MethodBase.GetParameterTypes (internal): the parameter types as Type[].
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "GetParameterTypes")
                when sig.ParameterTypes.Length == 0:
            {
                var m = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_methodref_get_parameter_types((Dn2CppMethodRef*)({m.Expr}))", sig.ReturnType);
                return true;
            }
            // MethodBase.GetParametersAsSpan (internal; the BCL invoker paths): a
            // ReadOnlySpan<ParameterInfo> over a freshly built handle array — the
            // {reference, length} span pair points at the array's element storage.
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "GetParametersAsSpan")
                when sig.ParameterTypes.Length == 0:
            {
                var m = Pop();
                string arrT = NewTemp("Dn2CppArrayRef*");
                Emit($"{arrT} = dn2cpp_methodref_get_parameters((Dn2CppMethodRef*)({m.Expr}));");
                string ct = CppTypes.Of(sig.ReturnType);
                string est = sig.ReturnType is { Kind: TypeKind.Class, Class.Context.TypeArgs: [{ } pel] }
                    ? CppTypes.StorageOf(pel)
                    : "Dn2CppObject*";
                Push(StackKind.Struct, ct, $"{ct}{{ ({est}*)(void*)({arrT}->data), {arrT}->length }}");
                return true;
            }
            // MethodBase.MethodHandle: a RuntimeMethodHandle is CoreCLR's raw
            // function-description handle; the reachable BCL callers (CustomAttribute
            // filtering, RuntimeTypeCache) consume it through metadata machinery that
            // does not exist in an AOT image — a catchable runtime
            // PlatformNotSupportedException (loud, never wrong data).
            case ("System.Reflection.MethodBase" or "System.Reflection.MethodInfo"
                    or "System.Reflection.ConstructorInfo", "get_MethodHandle"):
            {
                Pop();
                Emit("dn2cpp_throw_platform_not_supported(\"MethodBase.MethodHandle is not available in AOT-compiled code\");");
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct, $"{ct}{{}}"); // unreachable; stack typing only
                return true;
            }
            // FieldInfo raw-word long-tail.
            case ("System.Reflection.FieldInfo", "get_Attributes"):
            {
                var f = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})dn2cpp_fieldref_attributes((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            case ("System.Reflection.FieldInfo", "get_IsPrivate"):
            {
                var f = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_fieldref_is_private((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            case ("System.Reflection.FieldInfo", "get_IsSpecialName"):
            {
                var f = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_fieldref_is_specialname((Dn2CppFieldRef*)({f.Expr}))");
                return true;
            }
            // Type.GetGenericParameterConstraints: only meaningful on a
            // generic-parameter Type, which never materializes at runtime
            // (IsGenericParameter is constantly false) — so any execution is on an
            // invalid receiver; throw the catchable PlatformNotSupportedException rather
            // than fabricate an answer (real .NET: InvalidOperationException).
            case ("System.Type", "GetGenericParameterConstraints") when sig.ParameterTypes.Length == 0:
            {
                Pop();
                Emit("dn2cpp_throw_platform_not_supported(\"Type.GetGenericParameterConstraints: no generic-parameter Type materializes in AOT-compiled code\");");
                Push(StackKind.Ref, "Dn2CppArrayRef*", "((Dn2CppArrayRef*)nullptr)"); // unreachable; stack typing only
                return true;
            }
            // ParameterInfo long-tail: Member (the declaring MethodBase handle),
            // Attributes (raw ECMA word), IsOptional.
            case ("System.Reflection.ParameterInfo", "get_Member"):
            {
                var p = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_paramref_member((Dn2CppParamRef*)({p.Expr}))");
                return true;
            }
            case ("System.Reflection.ParameterInfo", "get_Attributes"):
            {
                var p = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})dn2cpp_paramref_attributes((Dn2CppParamRef*)({p.Expr}))");
                return true;
            }
            case ("System.Reflection.ParameterInfo", "get_IsOptional"):
            {
                var p = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_paramref_is_optional((Dn2CppParamRef*)({p.Expr}))");
                return true;
            }
            // ParameterInfo.IsOut / HasDefaultValue: the raw ECMA
            // ParameterAttributes word is on the row (Out = 0x2, HasDefault =
            // 0x1000), so both answer exactly.
            case ("System.Reflection.ParameterInfo", "get_IsOut"):
            {
                var p = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((dn2cpp_paramref_attributes((Dn2CppParamRef*)({p.Expr})) & 0x2) != 0 ? 1 : 0)");
                return true;
            }
            case ("System.Reflection.ParameterInfo", "get_HasDefaultValue"):
            {
                var p = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((dn2cpp_paramref_attributes((Dn2CppParamRef*)({p.Expr})) & 0x1000) != 0 ? 1 : 0)");
                return true;
            }
            // ParameterInfo.DefaultValue: the Constant-table blob is not carried
            // into the AOT image, so the value cannot be reported — loud
            // (catchable) rather than a silently-wrong null. Callers guard with
            // HasDefaultValue, which answers exactly above.
            case ("System.Reflection.ParameterInfo", "get_DefaultValue" or "get_RawDefaultValue"):
            {
                Pop();
                Emit("dn2cpp_throw_platform_not_supported(\"ParameterInfo.DefaultValue: the default-value constant is not recorded in the AOT image\");");
                Push(StackKind.Ref, "Dn2CppObject*", "((Dn2CppObject*)nullptr)"); // unreachable; stack typing only
                return true;
            }
            // MemberInfo.CustomAttributes / GetCustomAttributesData(): the element's
            // attribute rows as CustomAttributeData handles (AttributeType only —
            // the argument views throw below). One header-dispatched mechanism for
            // Type/field/method/ctor/property/parameter; Assembly has its own form.
            case ("System.Reflection.MemberInfo" or "System.Type" or "System.Reflection.MethodBase"
                    or "System.Reflection.MethodInfo" or "System.Reflection.ConstructorInfo"
                    or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo"
                    or "System.Reflection.ParameterInfo",
                "get_CustomAttributes" or "GetCustomAttributesData")
                when sig.ParameterTypes.Length == 0:
            {
                var mem = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_member_custom_attributes_data({Cast(mem, "Dn2CppObject*")})",
                    TypeDesc.MakeSZArray(CustomAttributeDataElem(sig.ReturnType)));
                return true;
            }
            case ("System.Reflection.Assembly", "get_CustomAttributes" or "GetCustomAttributesData")
                when sig.ParameterTypes.Length == 0:
            {
                var asm = Pop();
                PushReflectionMemberArray(
                    $"dn2cpp_assembly_custom_attributes_data({Cast(asm, "const char*")})",
                    TypeDesc.MakeSZArray(CustomAttributeDataElem(sig.ReturnType)));
                return true;
            }
            case ("System.Reflection.CustomAttributeData", "get_AttributeType"):
            {
                var d = Pop();
                Push(StackKind.Ref, "Dn2CppType*", $"dn2cpp_attrdata_attribute_type((Dn2CppAttrDataRef*)({d.Expr}))");
                return true;
            }
            // The declarative argument views need the encoded ctor blob, which the
            // emitted attribute tables don't carry (they bake instantiation factories
            // instead) — a catchable runtime PlatformNotSupportedException.
            case ("System.Reflection.CustomAttributeData", "get_ConstructorArguments" or "get_NamedArguments"):
            {
                Pop();
                Emit("dn2cpp_throw_platform_not_supported(\"CustomAttributeData exposes AttributeType only in AOT-compiled code (constructor/named arguments are not recorded)\");");
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr"); // unreachable; stack typing only
                return true;
            }
            // RuntimeHelpers.GetRawData(object): ref byte to the object's payload
            // (the first byte past the header) — the boxed value's storage, matching
            // the CLR's RawData layout for the modeled representations.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "GetRawData")
                when sig.ParameterTypes.Length == 1:
            {
                var o = Pop();
                Push(StackKind.Ptr, "uint8_t*", $"(uint8_t*)((Dn2CppObject*)({o.Expr}) + 1)");
                return true;
            }
            // RuntimeHelpers.GetElementSize(Array): the element width in bytes. The real
            // body is an InternalCall into MethodTable::GetComponentSize; the helper reads
            // it off the array's own LAYOUT, so it answers for an MD array and for an
            // imprecise runtime handle alike. Reached from
            // Marshal.UnsafeAddrOfPinnedArrayElement.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "GetElementSize")
                when sig.ParameterTypes.Length == 1:
            {
                var arr = Pop();
                string es = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), es,
                    $"({es})dn2cpp_array_element_size({Cast(arr, "Dn2CppObject*")})");
                return true;
            }
            // RuntimeHelpers.AllocateTypeAssociatedMemory(Type, int): zeroed,
            // GC-scanned, never-collected memory (type lifetime == process in AOT).
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "AllocateTypeAssociatedMemory")
                when sig.ParameterTypes.Length == 2:
            {
                var size = Pop();
                var t = Pop();
                Push(StackKind.Ptr, "intptr_t",
                    $"(intptr_t)dn2cpp_alloc_type_associated({Cast(t, "Dn2CppType*")}, {Cast(size, "int32_t")})");
                return true;
            }
            // Enum.InternalGetCorElementType: the CorElementType of the boxed enum's
            // underlying primitive, read from the type-info header (the real body is
            // MethodTable pointer math).
            case ("System.Enum", "InternalGetCorElementType") when sig.ParameterTypes.Length == 0:
            {
                var e = Pop();
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})dn2cpp_enum_cor_element_type({Cast(e, "Dn2CppObject*")})");
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>The CustomAttributeData element TypeDesc for a
    /// <c>get_CustomAttributes</c>/<c>GetCustomAttributesData</c> return type
    /// (IEnumerable&lt;CustomAttributeData&gt; / IList&lt;CustomAttributeData&gt;):
    /// the resolved generic argument when available, else the external name.</summary>
    private static TypeDesc CustomAttributeDataElem(TypeDesc ret) =>
        ret is { Kind: TypeKind.Class, Class.Context.TypeArgs: [{ } cad] }
            ? cad
            : TypeDesc.MakeExternal("System.Reflection.CustomAttributeData");

    /// <summary>The classified+popped arguments of one Type.GetMethod /
    /// GetConstructor / GetProperty overload: each is a C++ expression, or null
    /// when the overload doesn't carry the argument (the emission substitutes the
    /// family default). ParameterModifier[] arguments are popped and dropped.</summary>
    private sealed class LookupArgs
    {
        public string Name = "nullptr";
        public string? GenericCount;
        public string? Flags;
        public string? Binder;
        public string? CallConv;
        public string? ReturnType;
        public string? Types;
    }

    private enum LookupSlot { Name, GenericCount, Flags, Binder, CallConv, ReturnType, Types, Modifiers }

    private static string? LookupParamFullName(TypeDesc t) => t.Kind switch
    {
        TypeKind.Class => t.Class!.FullName,
        TypeKind.External => t.ExternalName,
        _ => null,
    };

    /// <summary>Maps one parameter of a member-lookup overload onto the shared
    /// argument vocabulary, or null for a type outside it.</summary>
    private static LookupSlot? ClassifyLookupParam(TypeDesc p)
    {
        if (p.IsString)
            return LookupSlot.Name;
        if (p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 })
            return LookupSlot.GenericCount;
        switch (LookupParamFullName(p))
        {
            case "System.Reflection.BindingFlags": return LookupSlot.Flags;
            case "System.Reflection.Binder": return LookupSlot.Binder;
            case "System.Reflection.CallingConventions": return LookupSlot.CallConv;
            case "System.Type": return LookupSlot.ReturnType;
        }
        if (p is { Kind: TypeKind.SZArray, Element: { } el })
            switch (LookupParamFullName(el))
            {
                case "System.Type": return LookupSlot.Types;
                case "System.Reflection.ParameterModifier": return LookupSlot.Modifiers;
            }
        return null;
    }

    /// <summary>Classifies a Get{Method,Constructor,Property} overload's
    /// parameters against the shared lookup vocabulary and, when the whole
    /// signature fits, pops the arguments into a <see cref="LookupArgs"/>
    /// (receiver left on the stack). Returns null — with the STACK UNTOUCHED —
    /// when any parameter falls outside the family's vocabulary (duplicate or
    /// disallowed slot), so the call site reports a normal gap instead of
    /// silently mis-binding.</summary>
    private LookupArgs? PopLookupArgs(MethodSignature<TypeDesc> sig, bool name, bool generic, bool returnType)
    {
        var ps = sig.ParameterTypes;
        var slots = new LookupSlot[ps.Length];
        var seen = new System.Collections.Generic.HashSet<LookupSlot>();
        bool sawName = false, sawTypes = false;
        for (int i = 0; i < ps.Length; i++)
        {
            if (ClassifyLookupParam(ps[i]) is not { } s || !seen.Add(s))
                return null;
            bool allowed = s switch
            {
                LookupSlot.Name => name,
                LookupSlot.GenericCount => generic,
                LookupSlot.ReturnType => returnType,
                _ => true,
            };
            if (!allowed)
                return null;
            sawName |= s == LookupSlot.Name;
            sawTypes |= s == LookupSlot.Types;
            slots[i] = s;
        }
        // The name-keyed families require the name; the ctor family (no name)
        // requires the Type[] list — every GetConstructor overload carries it.
        if (name != sawName || (!name && !sawTypes))
            return null;
        var a = new LookupArgs();
        for (int i = ps.Length - 1; i >= 0; i--)
        {
            var e = Pop();
            switch (slots[i])
            {
                case LookupSlot.Name: a.Name = Cast(e, "Dn2CppString*"); break;
                case LookupSlot.GenericCount: a.GenericCount = Cast(e, "int32_t"); break;
                case LookupSlot.Flags: a.Flags = Cast(e, "int32_t"); break;
                case LookupSlot.Binder: a.Binder = Cast(e, "Dn2CppObject*"); break;
                case LookupSlot.CallConv: a.CallConv = Cast(e, "int32_t"); break;
                case LookupSlot.ReturnType: a.ReturnType = Cast(e, "Dn2CppType*"); break;
                case LookupSlot.Types: a.Types = Cast(e, "Dn2CppArrayRef*"); break;
                case LookupSlot.Modifiers: break; // ignored (CoreCLR ignores it outside COM)
            }
        }
        return a;
    }

    /// <summary>System.Diagnostics.StackTrace / StackFrame — the stack-trace model.
    /// Both are intrinsic types, so EVERY member reached lands here and an
    /// unmapped one throws loudly (no silent carve-out); the ctors are in
    /// <see cref="EmitStackDiagnosticNewobj"/>.
    ///
    /// The current-stack ctors materialize the live shadow stack's frame names under
    /// <c>--shadow-stack</c> (zero frames flag-off — there is no CLR stack walker in a
    /// native binary), and StackTrace(Exception) reuses the trace stamped at throw in
    /// every build. The degradation doctrine — a zero-frame trace names itself rather
    /// than answering "", the frame format is declared-divergent — is stated in
    /// docs/ARCHITECTURE.md §4-B; this table implements it.
    ///
    /// Not covered here, deliberately: Exception.StackTrace renders the trace stamped at
    /// throw and stays null for an UNTHROWN exception — EXACT. Environment.StackTrace
    /// needs no intrinsic either: its real getter is
    /// `new StackTrace(true).ToString(TraceFormat.Normal)`, which transpiles straight
    /// through this table.</summary>
    private bool TryEmitStackDiagnosticsIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (declType, name)
        {
            // ---- System.Diagnostics.StackTrace ----
            case ("System.Diagnostics.StackTrace", "get_IsSupported") when ps.Length == 0:
            {
                Push(StackKind.I4, "int32_t", "1");
                return true;
            }
            case ("System.Diagnostics.StackTrace", "get_FrameCount") when ps.Length == 0:
            {
                string st = Cast(Pop(), "Dn2CppStackTrace*");
                Push(StackKind.I4, "int32_t", $"dn2cpp_stacktrace_frame_count({st})");
                return true;
            }
            case ("System.Diagnostics.StackTrace", "GetFrames") when ps.Length == 0:
            {
                string st = Cast(Pop(), "Dn2CppStackTrace*");
                // The array is already stamped with the precise StackFrame[] handle at
                // construction, so it flows into an IEnumerable<StackFrame> position (a
                // foreach over GetFrames()) with its element identity intact.
                var elemCls = Comp.FindClassByFullName("System.Diagnostics.StackFrame");
                var frames = new StackEntry($"dn2cpp_stacktrace_get_frames({st})",
                    StackKind.Ref, "Dn2CppArrayRef*",
                    StaticType: elemCls is null ? null : TypeDesc.MakeSZArray(TypeDesc.MakeClass(elemCls)));
                if (elemCls is not null)
                    Comp.NoteArrayEnumerableElement(TypeDesc.MakeClass(elemCls));
                PushEntry(frames);
                return true;
            }
            case ("System.Diagnostics.StackTrace", "GetFrame") when ps.Length == 1:
            {
                string idx = Cast(Pop(), "int32_t");
                string st = Cast(Pop(), "Dn2CppStackTrace*");
                // Null when out of range — real .NET's contract, and therefore the answer
                // GetFrame(0) gives on every zero-frame (degraded) trace here.
                Push(StackKind.Ref, "Dn2CppStackFrame*",
                    $"(Dn2CppStackFrame*)dn2cpp_stacktrace_get_frame({st}, {idx})");
                return true;
            }
            // ToString() and the internal ToString(TraceFormat) — the latter is what
            // Environment.StackTrace's getter calls. The format is dropped: the rendered
            // text is dn2cpp's declared-divergent frame format either way (every line
            // '\n'-terminated), so Normal / TrailingNewLine / NoResourceLookup coincide.
            case ("System.Diagnostics.StackTrace", "ToString") when ps.Length <= 1:
            {
                for (int i = 0; i < ps.Length; i++)
                    Pop();
                string st = Cast(Pop(), "Dn2CppObject*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_stacktrace_tostring({st})");
                return true;
            }
            // The internal void ToString(TraceFormat, StringBuilder): appends the same text.
            case ("System.Diagnostics.StackTrace", "ToString") when ps.Length == 2:
            {
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Pop(); // TraceFormat
                string st = Cast(Pop(), "Dn2CppObject*");
                Emit($"dn2cpp_sb_append_str({sb}, dn2cpp_stacktrace_tostring({st}));");
                return true;
            }

            // ---- System.Diagnostics.StackFrame ----
            // Every accessor is null-tolerant on the receiver: GetFrame(i) hands out null
            // on every degraded trace, so a null frame reports exactly what a degraded
            // frame would. See runtime/core/dn2cpp.h.
            //
            // GetMethod() stays null even for a frame materialized from a shadow-stack /
            // exception capture: a captured frame is a baked RENDERED STRING, and minting
            // a MethodBase from it would need a name→method-row lookup that lies three
            // ways:
            //   1. a canonical shared body's frame names its REPRESENTATIVE instantiation
            //      — the minted MethodBase could describe a method that never executed;
            //   2. the string carries no signature — overloads are ambiguous, and any
            //      pick is a plausible wrong answer;
            //   3. under --trim-reflection the method tables are stripped, and the
            //      honesty rule there (a stripped type must throw, not answer empty)
            //      would turn every trace-formatting logger into a thrower.
            // On the CLR this never returns null, so callers do not check — which is why
            // the null must behave like the degraded frame's answer rather than trap; the
            // name itself stays reachable through ToString().
            case ("System.Diagnostics.StackFrame", "GetMethod") when ps.Length == 0:
            {
                Pop(); // receiver: deliberately unread — see above
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
                return true;
            }
            case ("System.Diagnostics.StackFrame", "GetFileName") when ps.Length == 0:
            {
                string f = Cast(Pop(), "Dn2CppStackFrame*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_stackframe_file_name({f})");
                return true;
            }
            case ("System.Diagnostics.StackFrame", "GetFileLineNumber") when ps.Length == 0:
            {
                string f = Cast(Pop(), "Dn2CppStackFrame*");
                Push(StackKind.I4, "int32_t", $"dn2cpp_stackframe_line_number({f})");
                return true;
            }
            case ("System.Diagnostics.StackFrame", "GetFileColumnNumber") when ps.Length == 0:
            {
                string f = Cast(Pop(), "Dn2CppStackFrame*");
                Push(StackKind.I4, "int32_t", $"dn2cpp_stackframe_column_number({f})");
                return true;
            }
            // OFFSET_UNKNOWN (-1): what real .NET reports for a frame it has no IL/native
            // offset for, which is every frame here.
            case ("System.Diagnostics.StackFrame", "GetILOffset") when ps.Length == 0:
            case ("System.Diagnostics.StackFrame", "GetNativeOffset") when ps.Length == 0:
            {
                Pop();
                Push(StackKind.I4, "int32_t", "((int32_t)-1)");
                return true;
            }
            case ("System.Diagnostics.StackFrame", "ToString") when ps.Length == 0:
            {
                string f = Cast(Pop(), "Dn2CppObject*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_stackframe_tostring({f})");
                return true;
            }
        }
        return false;
    }

    /// <summary>Pushes an attribute-array expression (the array a
    /// <c>dn2cpp_*get_custom_attributes*</c> helper builds) retagged with the precise
    /// per-element type-info of the intercepted method's STATIC return — object[] for
    /// the MemberInfo forms, Attribute[] for the <c>System.Attribute</c> statics, the T
    /// of an <c>IEnumerable&lt;T&gt;</c>-returning extension — and that element's SZArray
    /// interface map wired, because callers funnel the result into non-generic LINQ
    /// (<c>.OfType&lt;T&gt;()</c>) or enumerate it at the static element typing
    /// (<c>Attribute[]</c> as <c>IEnumerable&lt;Attribute&gt;</c>) while the runtime
    /// helper allocates with the shared ref-array handle. The retag is CONDITIONAL on
    /// that shared handle: the typed-filter helpers stamp the filter's own attrType[]
    /// identity when the image carries it (matching .NET's runtime array type), and the
    /// static element must not overwrite the more precise one.</summary>
    private void PushAttributeArray(string expr, TypeDesc returnType)
    {
        TypeDesc elem = returnType switch
        {
            { Kind: TypeKind.SZArray, Element: { } e } => e,
            { Kind: TypeKind.Class, Class: { IsInterface: true } ic }
                when ic.Context.TypeArgs is [{ } arg] => arg,
            _ => TypeDesc.MakePrimitive(PrimitiveTypeCode.Object),
        };
        Comp.NoteArrayEnumerableElement(elem);
        string arrTmp = NewTemp("Dn2CppArrayRef*");
        Emit($"{arrTmp} = {expr};");
        Emit($"if (((Dn2CppObject*){arrTmp})->type == &dn2cpp_array_ref_type) ((Dn2CppObject*){arrTmp})->type = {PreciseArrayTypeInfoExpr(elem)};");
        Push(StackKind.Ref, "Dn2CppArrayRef*", arrTmp);
    }

    /// <summary>Shared-body candidate guard for the attribute funnels below: a typeof
    /// over a placeholder-bearing type rides the stack as a poisoned (null) handle with
    /// its static token attached (see the Ldtoken case), and every attribute member
    /// consumes the runtime VALUE — a canonical body baking the null filter would answer
    /// "no attributes" for every group member, since the typed helper reads a null Type
    /// as an absent type. The probe is the same over-approximate scan as
    /// <see cref="TryEmitReflectionIntrinsic"/>'s head — args plus one deeper slot — and
    /// is needed HERE because these funnels are keyed-chain arms: the fallback-probe head
    /// never runs for a call the keyed chain answers. No
    /// <see cref="IsPlaceholderAgnosticFold"/> exemption applies — none of these members
    /// fold to a token-only read.</summary>
    private void TaintPlaceholderAttrFilter(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        if (!SharedTrial)
            return;
        int probe = System.Math.Min(sig.ParameterTypes.Length + 1, StackDepth);
        for (int i = 1; i <= probe; i++)
            if (Peek(i).TypeToken is { } tk && Compilation.ContainsCanonPlaceholder(tk))
                ThrowSharedTaint("type-identity", $"{declType}.{name} on {tk}");
    }

    /// <summary>Emits GetCustomAttributes / IsDefined. GetCustomAttributes(bool)
    /// returns all attributes; GetCustomAttributes(Type, bool) and IsDefined(Type, bool)
    /// filter to the given attribute type. The trailing <c>inherit</c> flag is popped and
    /// ignored (AttributeUsage inheritance is a carve-out).</summary>
    private bool TryEmitCustomAttributeIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        TaintPlaceholderAttrFilter(declType, name, sig);
        bool hasType = sig.ParameterTypes.Length == 2; // (Type, bool) vs (bool)
        if (name == "GetCustomAttributes")
        {
            Pop(); // inherit
            string filter = hasType ? Cast(Pop(), "Dn2CppType*") : "nullptr";
            var member = Pop();
            // A typed filter that folded to a null token means an absent (unreachable)
            // type -> no match; the no-type form keeps "all" semantics.
            string fn = hasType ? "dn2cpp_get_custom_attributes_typed" : "dn2cpp_get_custom_attributes";
            PushAttributeArray($"{fn}({Cast(member, "Dn2CppObject*")}, {filter})", sig.ReturnType);
            return true;
        }
        if (name == "IsDefined" && hasType)
        {
            Pop(); // inherit
            string filter = Cast(Pop(), "Dn2CppType*");
            var member = Pop();
            Push(StackKind.I4, "int32_t",
                $"dn2cpp_is_defined({Cast(member, "Dn2CppObject*")}, {filter})");
            return true;
        }
        return false;
    }

    /// <summary>Emits the <c>static</c> attribute getters where the reflected element is an
    /// explicit first parameter rather than the receiver: <c>System.Attribute</c>'s and
    /// <c>CustomAttributeExtensions</c>' <c>GetCustomAttribute</c>/<c>GetCustomAttributes</c>/
    /// <c>IsDefined</c> — the BCL targets the generic <c>GetCustomAttribute&lt;T&gt;</c> /
    /// <c>GetCustomAttributes&lt;T&gt;</c> extension methods lower to. Args are pushed
    /// element, [Type], [bool inherit]; <c>inherit</c> is popped and ignored.</summary>
    private bool TryEmitStaticAttributeIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        TaintPlaceholderAttrFilter(declType, name, sig);
        var ps = sig.ParameterTypes;
        if (ps.Length > 0 && ps[^1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean })
            Pop(); // inherit
        bool hasType = false;
        foreach (var p in ps)
            if (p.ToString() == "System.Type") { hasType = true; break; }
        string filter = hasType ? Cast(Pop(), "Dn2CppType*") : "nullptr";
        // An Assembly element is a simple-name const char* handle, not a managed
        // object — route it to the assembly-registry helpers.
        if (ps.Length > 0 && ps[0].ToString() == "System.Reflection.Assembly")
        {
            string asm = Cast(Pop(), "const char*");
            switch (name)
            {
                case "GetCustomAttribute":
                    Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_assembly_get_custom_attribute({asm}, {filter})");
                    return true;
                case "GetCustomAttributes":
                    PushAttributeArray(hasType
                        ? $"dn2cpp_assembly_get_custom_attributes_typed({asm}, {filter})"
                        : $"dn2cpp_assembly_get_custom_attributes({asm}, nullptr)", sig.ReturnType);
                    return true;
                case "IsDefined":
                    Push(StackKind.I4, "int32_t", $"dn2cpp_assembly_is_defined({asm}, {filter})");
                    return true;
            }
            return false;
        }
        var elem = Cast(Pop(), "Dn2CppObject*");
        switch (name)
        {
            case "GetCustomAttribute":
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_get_custom_attribute({elem}, {filter})");
                return true;
            case "GetCustomAttributes":
                PushAttributeArray(hasType
                    ? $"dn2cpp_get_custom_attributes_typed({elem}, {filter})"
                    : $"dn2cpp_get_custom_attributes({elem}, {filter})", sig.ReturnType);
                return true;
            case "IsDefined":
                Push(StackKind.I4, "int32_t", $"dn2cpp_is_defined({elem}, {filter})");
                return true;
        }
        return false;
    }
}
