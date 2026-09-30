using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void PushVar((string Name, string CppType, StackKind Kind, TypeDesc? Type) v)
    {
        Push(v.Kind, v.CppType, v.Name);
        if (v.Type is not null)
            _stack[^1] = _stack[^1] with { StaticType = v.Type };
    }

    /// <summary>If <paramref name="t"/> is a closed <c>Nullable&lt;U&gt;</c>, returns its
    /// underlying type U plus the C++ field names of the real <c>System.Nullable`1</c>
    /// layout (<c>hasValue</c>/<c>value</c>); null otherwise. The <c>box</c>/<c>unbox.any</c>
    /// of a Nullable carry special CLR semantics (box → the underlying value or null),
    /// which we emit by reading these fields directly.
    ///
    /// <para><b>Handing the field names out FORCES the layout</b> (the NoteForceEmit below),
    /// which is why the note is here and not at the emitting call sites: those lowerings
    /// spell <c>tN.f_hasValue</c> / <c>tN.f_value</c> straight into the C++ without going
    /// through <c>FieldAccess</c>, so neither the emit-set closure nor
    /// <c>CppEmitter.AssertNamedStructsDefined</c> learns anything, and a
    /// <c>Nullable&lt;U&gt;</c> reached ONLY that way is emitted as an opaque shell whose
    /// members the C++ compile then cannot find. At the funnel, "you cannot learn the field
    /// names without the layout being emitted" is structural.</para>
    ///
    /// <para>One caller asks this as a pure GUARD (<c>TranslateGenericIntrinsic</c>) and so
    /// pays a forced layout for fields it will not read — bounded bloat, one struct
    /// definition per instantiation, against a C++ compile error with no cause attached.
    /// </para></summary>
    private (TypeDesc Underlying, string HasValueField, string ValueField)? NullableLayout(TypeDesc t)
    {
        if (t is not { Kind: TypeKind.Class, Class: { } cls }
            || _c.GenericDefFullName(cls) != "System.Nullable"
            || cls.Context.TypeArgs.Length != 1)
            return null;
        string hv = cls.Fields.First(f => f.Name == "hasValue").CppName;
        string val = cls.Fields.First(f => f.Name == "value").CppName;
        _c.NoteForceEmit(cls);
        return (cls.Context.TypeArgs[0], hv, val);
    }

    /// <summary>Returns an addressable C++ lvalue holding the by-value struct
    /// <paramref name="obj"/>, so a field of it can have its address taken
    /// (ldflda) or be stored into (stfld). Push spills every struct value into a
    /// named temp local, so the Expr is normally already a plain identifier and is
    /// returned unchanged — which also keeps a dup'd sibling sharing the same
    /// storage, so a stfld is observed by the duplicate. A non-identifier (a true
    /// rvalue expression) is copied into a fresh temp; a store into that copy is a
    /// dead store, matching .NET's semantics for a non-addressable value.</summary>
    private string StructLValue(StackEntry obj)
    {
        if (IsSimpleIdentifier(obj.Expr))
            return obj.Expr;
        string tmp = NewTemp(obj.CppType);
        Emit($"{tmp} = {obj.Expr};");
        return tmp;
    }

    private static bool IsSimpleIdentifier(string s)
    {
        if (s.Length == 0 || !(char.IsLetter(s[0]) || s[0] == '_'))
            return false;
        foreach (char ch in s)
            if (!(char.IsLetterOrDigit(ch) || ch == '_'))
                return false;
        return true;
    }

    /// <summary>True when a field's declaring class is System.Exception itself — an
    /// intrinsic type with no emitted struct, so its instance fields cannot be accessed
    /// through the ordinary layout. Only the two fields dn2cpp's exception model carries
    /// (message, inner) map onto the Dn2CppExceptionObject prefix; the accessors below
    /// fail loudly on any other. Reached by the get_Message override bodies the
    /// used-virtual reach pulls in (FileNotFoundException reads/writes _message).</summary>
    private static bool IsExceptionBaseField(ClassInfo cls) => cls.FullName == "System.Exception";

    /// <summary>The C++ prefix-slot type of a mapped System.Exception field.</summary>
    private static string ExceptionFieldCppType(FieldInfo fld) =>
        fld.Name == "_message" ? "Dn2CppString*" : "Dn2CppObject*";

    /// <summary>The lvalue for a System.Exception-declared instance field, reinterpreted
    /// onto the Dn2CppExceptionObject prefix. Only <c>_message</c> and
    /// <c>_innerException</c> are modeled; any other Exception field (_HResult, _source,
    /// _stackTrace, _data, …) throws a loud, catchable transpile-time error naming it —
    /// far better than emitting C++ that references a struct member that does not exist,
    /// which would fail in the C++ compile.</summary>
    private string ExceptionFieldLValue(FieldInfo fld, StackEntry obj)
    {
        string recv = $"((Dn2CppExceptionObject*)({obj.Expr}))";
        return fld.Name switch
        {
            "_message" => $"{recv}->message",
            "_innerException" => $"{recv}->inner",
            _ => throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: System.Exception field "
                + $"'{fld.Name}' is not modeled by dn2cpp's exception layout — only _message and "
                + "_innerException map onto the Dn2CppExceptionObject prefix "
                + $"[chain: {_c.ReachChain(_method)}]"),
        };
    }

    /// <summary>Field access expression for a struct value, managed pointer,
    /// or object reference receiver. The non-struct (pointer) form casts the receiver to
    /// <c>(t_cls*)</c> and so names <c>t_cls</c> as a C++ type — recorded for the named-struct
    /// backstop, since a transpiled BCL body pointer-form-accessing an intrinsic-modeled
    /// reference type (the intrinsic Thread's <c>_executionContext</c>) names a <c>t_</c>
    /// nothing declares. The by-value struct form emits <c>(expr).f_</c> and names no
    /// <c>t_</c>, so it is not recorded (and such a struct is on the stack by value, hence
    /// always emitted anyway).</summary>
    private string FieldAccess(ClassInfo cls, FieldInfo fld, StackEntry obj)
    {
        if (obj.Kind == StackKind.Struct)
            return $"({obj.Expr}).{fld.CppName}";
        if (!cls.IsEnum)
            _c.NoteNamedStructSymbol(_method, cls.CppStructName);
        return $"(({cls.CppStructName}*){NullCheckReceiver(obj)})->{fld.CppName}";
    }

    /// <summary>Wraps a receiver expression in the <c>dn2cpp_null_check</c> guard
    /// — the emitted body's counterpart to the null test .NET performs
    /// before it forms a member address. The guard RETURNS the pointer, so the
    /// splice sits inside the cast and the result is still an lvalue: one call
    /// site serves <c>ldfld</c>, <c>stfld</c> and <c>ldflda</c>'s address-of alike,
    /// and the check is sequenced before the member offset is added, which a bare
    /// compare emitted beside it would not be.
    ///
    /// It applies to a REFERENCE receiver only. A managed pointer or byref reaches
    /// the same lvalue builder, and .NET does not null-check those: `ldfld` on a
    /// byref is unverifiable rather than guarded, the pointers the emitter forms
    /// are addresses of live storage, and guarding them would put a branch on every
    /// field of every struct accessed through a `ref` — the span/Memory hot paths.
    ///
    /// <c>KnownNull</c> is deliberately NOT special-cased into a direct throw: the
    /// guard is an inline function over a single-assignment temp, so clang folds a
    /// provably-null argument into the unconditional call on its own.</summary>
    private string NullCheckReceiver(StackEntry obj) =>
        obj.Kind == StackKind.Ref ? $"dn2cpp_null_check({obj.Expr})" : obj.Expr;

    /// <summary>The C++ spelling of an instance field as it is actually declared
    /// in the emitted struct layout. When the declaring class shares its struct
    /// layout with its canonical group owner, the member carries the owner's
    /// erased spelling (a reference field is the CnRef placeholder's
    /// <c>Dn2CppObject*</c>), so a body compiled under the real instantiation's
    /// context must cast between this and its site-resolved spelling on every
    /// load/store/address-of. Identical to <see cref="CppTypes.FieldOf"/> for
    /// ungrouped classes and for spellings the canonicalization preserves
    /// (primitives, enum underlyings, grouped class types via the
    /// <see cref="ClassInfo.CppStructName"/> redirect).</summary>
    private string LayoutFieldType(ClassInfo cls, FieldInfo fld)
    {
        if (fld.IsStatic || !ClassInfo.ShareStructLayout || cls.SharedOwner is not { } owner)
            return CppTypes.FieldOf(fld);
        _c.EnsureCompleted(owner);
        var ownerFld = owner.Fields.FirstOrDefault(f => f.Name == fld.Name);
        return ownerFld is null ? CppTypes.FieldOf(fld) : CppTypes.FieldOf(ownerFld);
    }

    /// <summary>The declared C++ spelling of <paramref name="memberName"/> in
    /// <paramref name="structType"/>'s emitted layout, for aggregate initializers
    /// built by intrinsics (span shaping): the canonical owner's erased spelling
    /// when the struct shares its layout, <paramref name="fallback"/> (the
    /// site-computed spelling, identical to the member for an unshared layout)
    /// otherwise.</summary>
    private string LayoutMemberType(TypeDesc structType, string memberName, string fallback)
    {
        if (ClassInfo.ShareStructLayout
            && structType is { Kind: TypeKind.Class, Class: { SharedOwner: { } owner } })
        {
            _c.EnsureCompleted(owner);
            if (owner.Fields.FirstOrDefault(f => f.Name == memberName) is { } fld)
                return CppTypes.FieldOf(fld);
        }
        return fallback;
    }

    /// <summary>If <paramref name="op"/>'s tracked static type is a
    /// <c>List&lt;T&gt;</c>, returns C++ expressions for its backing array
    /// (<c>_items</c>, same repr as a <c>T[]</c>) and live element count
    /// (<c>_size</c>) plus the element type T. The array's allocated length is the
    /// capacity (≥ Count), so callers must iterate <c>Count</c>, not the array
    /// length — the count-aware <c>_n</c> string helpers do exactly that. Returns
    /// null when the tracked type is not a List&lt;T&gt; — another type, or none
    /// after predecessors of different types join.</summary>
    private (string Items, string Count, TypeDesc Elem)? TryListBacking(StackEntry op)
    {
        // A closed generic's ClassInfo carries a *mangled* Name (e.g. "List_int32"),
        // so identify List<T> by its open-definition name (the spec's Handle points
        // at the List`1 TypeDefinition in its owning module).
        if (op.StaticType is not { Kind: TypeKind.Class, Class: { GenericArity: > 0 } cls })
            return null;
        var defReader = cls.Module.Reader;
        var defTd = defReader.GetTypeDefinition(cls.Handle);
        if (defReader.GetString(defTd.Name) != "List`1"
            || defReader.GetString(defTd.Namespace) != "System.Collections.Generic")
            return null;
        _c.EnsureCompleted(cls);
        var items = cls.Fields.FirstOrDefault(f => f.Name == "_items");
        var size = cls.Fields.FirstOrDefault(f => f.Name == "_size");
        if (items is null || size is null
            || items.Type is not { Kind: TypeKind.SZArray, Element: { } elem })
            return null;
        return (FieldAccess(cls, items, op), FieldAccess(cls, size, op), elem);
    }

    /// <summary>Whether the object behind <paramref name="op"/> may be a raw array, which
    /// has no managed interface map to enumerate through: its tracked static type is
    /// unknown, object, System.Array, an array type, or a collection interface arrays
    /// implement. Any other class, a box, or another interface (ISet&lt;T&gt;,
    /// IGrouping&lt;K,T&gt;, IOrderedEnumerable&lt;T&gt;, …) is a managed object.</summary>
    private bool MayBeArray(StackEntry op) => op.StaticType switch
    {
        null or { Kind: TypeKind.SZArray } or { IsObject: true } => true,
        { Kind: TypeKind.Class, Class: { } c } => c.FullName is "System.Array" or "System.Object"
            || (c.IsInterface && (c.FullName is "System.Collections.IEnumerable"
                    or "System.Collections.ICollection" or "System.Collections.IList"
                || _c.GenericDefFullName(c) is "System.Collections.Generic.IEnumerable"
                    or "System.Collections.Generic.IReadOnlyList"
                    or "System.Collections.Generic.IReadOnlyCollection"
                    or "System.Collections.Generic.ICollection"
                    or "System.Collections.Generic.IList")),
        _ => false,
    };

    /// <summary>The join of a managed <c>IEnumerable&lt;T&gt;</c> collection (a
    /// <c>HashSet&lt;T&gt;</c>, a Dictionary key view, a LINQ grouping, …) through an
    /// inline interface-enumeration loop, or null (emitting nothing) when the element
    /// type is unsupported. <paramref name="sepStr"/> is the lowered separator, or null
    /// for <c>Concat</c>.</summary>
    private string? EnumerationJoin(StackEntry src, TypeDesc elem, string? sepStr) =>
        EmitEnumerationToSb(src, elem, sepStr) is { } sb ? $"dn2cpp_sb_tostring({sb})" : null;

    /// <summary>The join of an operand that could be a raw array or a managed collection
    /// at run time (<see cref="MayBeArray"/>): an array takes the array helper, anything
    /// else enumerates through the interface. Null (emitting nothing) when either branch
    /// cannot format the element type.</summary>
    private string? ArrayOrEnumerationJoin(StackEntry src, TypeDesc elem, string? sepStr)
    {
        if (_c.EnumerationDispatch(elem) is null || !CanFormatElement(elem) || !CanJoinArray(elem))
            return null;
        string result = NewTemp("Dn2CppString*");
        // An array carries DN2CPP_TF_ARRAY in its type-info (whether the shared
        // array_{ref,i4} handle or a precise per-element ti_arr_<T>); a managed
        // collection carries its own class type-info, never an array one — so the flag
        // test discriminates "array vs managed collection" regardless of which handle.
        Emit($"if ((((Dn2CppObject*)({src.Expr}))->type->flags & DN2CPP_TF_ARRAY) != 0) {{");
        string arrayJoin = ArrayJoinCall(elem, src.Expr, null, sepStr)!;
        Emit($"    {result} = {arrayJoin};");
        Emit("} else {");
        string sb = EmitEnumerationToSb(src, elem, sepStr)!;
        Emit($"    {result} = dn2cpp_sb_tostring({sb});");
        Emit("}");
        return result;
    }

    /// <summary>Emits the interface-enumeration loop appending each formatted element
    /// to a fresh <c>StringBuilder</c> and returns the builder's temp — what the real
    /// Join body's loop over the enumerator does. Returns null (emitting nothing) when
    /// the element type is unsupported or the enumeration interfaces aren't loaded.
    /// <paramref name="sepStr"/> null ⇒ no separator (<c>Concat</c>).</summary>
    private string? EmitEnumerationToSb(StackEntry src, TypeDesc elem, string? sepStr)
    {
        if (_c.EnumerationDispatch(elem) is not { } ed || !CanFormatElement(elem))
            return null;
        string sb = NewTemp("Dn2CppStringBuilder*");
        Emit($"{sb} = dn2cpp_sb_new();");
        string first = sepStr is null ? "" : NewTemp("int32_t");
        if (sepStr is not null)
            Emit($"{first} = 1;");
        EmitForEach(src, ed, elem, cur =>
        {
            if (sepStr is not null)
            {
                Emit($"    if (!{first}) dn2cpp_sb_append_str({sb}, {sepStr});");
                Emit($"    {first} = 0;");
            }
            Emit($"    dn2cpp_sb_append_str({sb}, {FormatElement(elem, cur)});");
        });
        return sb;
    }

    /// <summary>Emits a C# <c>foreach</c> over the <c>IEnumerable&lt;T&gt;</c> in
    /// <paramref name="src"/>, dispatched through the interfaces: GetEnumerator, then
    /// MoveNext and get_Current into a temp that <paramref name="body"/> consumes, then
    /// Dispose — also when the loop throws, as the foreach's finally does. The caller
    /// reaches the dispatched methods (<see cref="Compilation.EnumerationDispatch"/>).</summary>
    private void EmitForEach(StackEntry src, Compilation.EnumerationMethods ed, TypeDesc elem, Action<string> body)
    {
        string e = NewTemp(CppTypes.Of(ed.GetEnumerator.Signature.ReturnType)); // IEnumerator<T>*
        // A null enumerator faults at its first MoveNext, before anything could dispose it.
        Emit($"{e} = dn2cpp_null_check({EmitIfaceDispatch(ed.GetEnumerator, src.Expr)});");
        string cur = NewTemp(CppTypes.Of(elem));
        string dispose = EmitIfaceDispatch(ed.Dispose, e);
        Emit("try {");
        Emit($"while ({EmitIfaceDispatch(ed.MoveNext, e)}) {{");
        Emit($"    {cur} = {EmitIfaceDispatch(ed.GetCurrent, e)};");
        body(cur);
        Emit("}");
        Emit("} catch (...) {");
        Emit($"    {dispose};");
        Emit("    throw;");
        Emit("}");
        Emit($"{dispose};");
    }

    /// <summary>An interface-method callvirt: resolve <paramref name="mth"/>'s slot in
    /// the receiver's runtime type-info via <c>dn2cpp_resolve_interface</c> and call
    /// through it. A shared loop uses the same canonical interface identity as an
    /// ordinary interface call and records possible alias collisions.</summary>
    private string EmitIfaceDispatch(MethodInfo mth, string recvExpr)
    {
        NoteDispatchSignatureTypes(mth);
        NoteCanonicalItfDispatch(mth.DeclaringClass);
        return $"(({FnPtrType(mth)})(dn2cpp_resolve_interface(((Dn2CppObject*){recvExpr})->type, "
            + $"&{ItfDispatchTi(mth.DeclaringClass).CppTypeInfoName})[{mth.VtableSlot}]))"
            + $"(({mth.DeclaringClass.CppStructName}*){recvExpr})";
    }

    /// <summary>Whether <see cref="ArrayJoinCall"/> formats an array of
    /// <paramref name="t"/>.</summary>
    private static bool CanJoinArray(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.IsEnum: true } || ArrayJoinHelper(t) is not null;

    /// <summary>The runtime call joining the <paramref name="t"/>[] at
    /// <paramref name="arrExpr"/> — its first <paramref name="count"/> elements, or all
    /// of them when that is null (a List&lt;T&gt;'s backing array is longer than the
    /// list) — with <paramref name="sepStr"/>, or concatenating it when that is null.
    /// Null when no helper formats the element type. An enum element is boxed under its
    /// own type-info, so it formats by name as Enum.ToString does.</summary>
    private string? ArrayJoinCall(TypeDesc t, string arrExpr, string? count, string? sepStr)
    {
        string sep = sepStr ?? "dn2cpp_string_literal(u\"\", 0)";
        if (t is { Kind: TypeKind.Class, Class.IsEnum: true })
        {
            string eti = TypeInfoExpr(t)
                ?? throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: joining {t} has no emitted type-info");
            string arrCt = ArrayCppPtr(t);
            string arrT = NewTemp(arrCt);
            Emit($"{arrT} = ({arrCt})({arrExpr});");
            var (data, stride) = ArrayDataStride(t, arrT);
            return $"dn2cpp_string_join_enum_n({sep}, {data}, {stride}, {count ?? arrT + "->length"}, {eti})";
        }
        if (ArrayJoinHelper(t) is not { } h)
            return null;
        return count is null
            ? $"{h.Helper}({sep}, ({h.ArrayType})({arrExpr}))"
            : $"{h.Helper}_n({sep}, ({h.ArrayType})({arrExpr}), {count})";
    }

    /// <summary>The runtime helper joining an array of the non-enum
    /// <paramref name="t"/>, and the array representation it reads, or null.</summary>
    private static (string Helper, string ArrayType)? ArrayJoinHelper(TypeDesc t) => t switch
    {
        { Kind: TypeKind.Class, Class.IsEnum: true } => null,
        // Unsigned elements format unsigned: the signed helpers would print
        // uint.MaxValue as -1, and uint shares int's array representation.
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 } => ("dn2cpp_string_join_u4", "Dn2CppArrayI4*"),
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 } => ("dn2cpp_string_join_u8", "Dn2CppArrayN*"),
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } => ("dn2cpp_string_join_i8", "Dn2CppArrayN*"),
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } => ("dn2cpp_string_join_r8", "Dn2CppArrayN*"),
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } => ("dn2cpp_string_join_ch", "Dn2CppArrayN*"),
        _ => RepOf(t) switch
        {
            ArrRep.I4 => ("dn2cpp_string_join_i4", "Dn2CppArrayI4*"),
            ArrRep.Ref => ("dn2cpp_string_join_ref", "Dn2CppArrayRef*"),
            _ => null,
        },
    };

    /// <summary>The precise per-element array type-info handle for <c>element[]</c>
    ///: <c>&amp;ti_arr_&lt;mangle&gt;</c>, where the mangle matches CppEmitter's
    /// emitted <c>ti_arr_</c> symbol. The caller must have noted the element type
    /// (<see cref="Compilation.NoteArrayElementType"/>) so the symbol is emitted —
    /// every <c>newarr</c> / <c>typeof(T[])</c> site does. A shared-body
    /// candidate naming a placeholder-element array handle is
    /// instantiation-dependent (the alias's array must carry its own enum
    /// element identity), so it taints the trial compile.</summary>
    internal string PreciseArrayTypeInfoExpr(TypeDesc element)
    {
        TaintIfCanonical(element, "array-ti");
        var e = PreciseArrayTypeInfoExprOf(element);
        _c.NoteNamedTypeInfoSymbol(_method, e);
        return e;
    }

    /// <summary>Token-carrying variant for instruction-level sites (newarr,
    /// typeof(T[]), array cast targets): a placeholder-bearing element loads the
    /// real instantiation's precise array handle out of an rgctx slot keyed on
    /// the site's raw type token — which resolves to either the element itself
    /// (newarr) or the SZArray (typeof/cast); the fill projects the element.</summary>
    internal string PreciseArrayTypeInfoExpr(TypeDesc element, int token)
    {
        if (SharedTrial && Compilation.ContainsCanonPlaceholder(element))
            return "(const Dn2CppTypeInfo*)"
                + RgctxSlotAccess(RgctxSlotKind.ArrayTypeInfo, token, "array-ti", element);
        var e = PreciseArrayTypeInfoExprOf(element);
        _c.NoteNamedTypeInfoSymbol(_method, e);
        return e;
    }

    /// <summary>See <see cref="PreciseArrayTypeInfoExpr(TypeDesc)"/> — the raw
    /// handle expression, for emitter (non-body) contexts.</summary>
    internal static string PreciseArrayTypeInfoExprOf(TypeDesc element) =>
        "&ti_arr_" + Compilation.ArrayElemMangle(element);

    /// <summary>The precise handle for an MD array's SZArray ELEMENT, for the three MD
    /// identity mouths (<c>new T[,]</c> / <c>typeof(T[,])</c> / castclass-isinst
    /// targets): <c>typeof(int[,][])</c> names <c>int[]</c> where the generic
    /// TypeInfoExpr fallback answered null, so <c>dn2cpp_mdarr_ti(nullptr, …)</c> handed
    /// typeof a null Type — an NRE at the first <c>.Name</c>. Null for every
    /// other element kind (the callers keep their existing fallback) and for a
    /// placeholder-bearing element in a shared-body candidate, whose identity is
    /// instantiation-dependent and keeps the null degrade the ctor site always had.</summary>
    private string? MdSzElementTypeInfoExpr(TypeDesc el)
    {
        if (SharedTrial && Compilation.ContainsCanonPlaceholder(el))
            return null;
        if (el is not { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray } szEl })
            return null;
        _c.NoteArrayElementType(szEl);
        return PreciseArrayTypeInfoExpr(szEl);
    }

    /// <summary>The precise array handle for a STATICALLY KNOWN primitive element,
    /// for the runtime helpers whose result escapes to managed code with an element the
    /// lowering already knows: <c>Convert.FromBase64String</c>/<c>FromHexString</c>
    /// (Byte), <c>decimal.GetBits</c> and the NumberFormatInfo group sizes (Int32).
    /// A primitive element never carries a canonical placeholder, so unlike the
    /// token-carrying overload this needs no rgctx route and cannot taint a shared trial.
    ///
    /// <para><b>The note is <see cref="Compilation.NoteArrayEnumerableElement"/>, not
    /// <see cref="Compilation.NoteArrayElementType"/>, and the difference is a segfault.</b>
    /// The weaker note emits the <c>ti_arr_</c> symbol but does NOT wire the array's SZArray
    /// interface-dispatch map, so a precise handle over an unwired map is strictly worse than
    /// the shared handle: the array claims an <c>IEnumerable&lt;T&gt;</c> it has no slots
    /// for, and the first interface call through it loads a null slot and calls it. A retag
    /// obliges the map.</para></summary>
    internal string PrimArrayTypeInfoExpr(PrimitiveTypeCode prim)
    {
        var elem = TypeDesc.MakePrimitive(prim);
        Comp.NoteArrayEnumerableElement(elem);
        return PreciseArrayTypeInfoExpr(elem);
    }

    /// <summary>The <c>byte[]</c> handle — see <see cref="PrimArrayTypeInfoExpr"/>.</summary>
    internal string ByteArrayTypeInfoExpr() => PrimArrayTypeInfoExpr(PrimitiveTypeCode.Byte);

    /// <summary>The Dn2CppTypeInfo* expression for a <c>castclass</c>/<c>isinst</c>
    /// target: the precise per-element array handle for an SZArray target (noting
    /// the element so the symbol is emitted), so an array cast checks element covariance
    /// against the exact type rather than the shared object[] handle; otherwise
    /// <see cref="TypeInfoExpr"/>. Null when the target has no runtime type-info
    /// (System.Object / an external exception type) — the caller treats that as an
    /// unconditional/identity match.</summary>
    private string? CastTargetTypeInfoExpr(TypeDesc target, int token = 0)
    {
        if (target is { Kind: TypeKind.SZArray, Element: { Kind: TypeKind.Primitive or TypeKind.Class or TypeKind.External or TypeKind.SZArray or TypeKind.MDArray } el })
        {
            _c.NoteArrayElementType(el);
            return token != 0 ? PreciseArrayTypeInfoExpr(el, token) : PreciseArrayTypeInfoExpr(el);
        }
        // A multidim array target (int[,]): without a real type-info the isinst opcode's null
        // arm folds the test to an unconditional TRUE, so `int[] is int[,]` would answer True.
        // Build the target's interned (element, rank) identity at run time via dn2cpp_mdarr_ti
        // — the SAME handle a `new T[,]` of that shape carries — so dn2cpp_isinst runs its
        // element+rank compare. A reference/enum element needs its ti emitted for the symbol
        // to link; a primitive names a runtime handle. A null element ti (a shared-body
        // placeholder) degrades dn2cpp_mdarr_ti to a fabricated identity — no match, no crash.
        if (target is { Kind: TypeKind.MDArray, Element: { } mdel, Rank: var mdrank })
        {
            // A type token naming an MD array (typeof/castclass/isinst target) keys
            // the shared rank>=2 dispatch map too: the program expects one.
            _c.NoteMdArrayUse();
            if (mdel.Kind is TypeKind.Class && mdel.Class is { } mdCls)
                NoteReferencedType(mdCls);
            string mdElemTi = MdSzElementTypeInfoExpr(mdel)
                ?? (token != 0 ? TypeInfoExpr(mdel, token) : TypeInfoExpr(mdel)) ?? "nullptr";
            return $"dn2cpp_mdarr_ti({mdElemTi}, {mdrank})";
        }
        // A runtime cast to a closed SZArray collection interface (IEnumerable<E>/
        // ICollection<E>/IList<E>/IReadOnly{List,Collection}<E>): let an array of E carry
        // its real interface-dispatch map so the cast/`is`/member dispatch resolves on the
        // array itself, not only at the statically-known boundary. Noting the element wires
        // the full SZArray interface set; covariant targets additionally drive
        // ExpandArrayEnumerableMaps for derived-element arrays.
        if (target is { Kind: TypeKind.Class, Class: { IsInterface: true } ic }
            && _c.GenericDefFullName(ic) is "System.Collections.Generic.IEnumerable"
                or "System.Collections.Generic.ICollection"
                or "System.Collections.Generic.IReadOnlyCollection"
                or "System.Collections.Generic.IList"
                or "System.Collections.Generic.IReadOnlyList"
            && ic.Context.TypeArgs is [{ } ee])
            _c.NoteArrayEnumerableElement(ee);
        // A runtime cast to an interface String implements (IEnumerable<char>,
        // IComparable, ICloneable, …) can find a string behind the object — e.g.
        // Comparer<object>.Default's `(IComparable)x` — so wire String's dispatch
        // map; entry presence is what makes the cast/`is` succeed.
        if (target is { Kind: TypeKind.Class, Class: { IsInterface: true } tic }
            && _c.IsStringDispatchInterface(tic))
            _c.NoteStringInterfaces();
        // A runtime cast to an interface every enum implements via System.Enum can
        // find a boxed enum behind the object the same way — TypeDescriptor's
        // (IConvertible)value, Comparer<object>.Default's (IComparable)x — so wire
        // the shared System.Enum dispatch map. The type TEST is map-independent
        // (dn2cpp_wellknown_itf_mask's TF_ENUM arm answers is/castclass); this keeps
        // the subsequent CALL sound.
        if (target is { Kind: TypeKind.Class, Class: { IsInterface: true } enic }
            && Compilation.IsEnumDispatchInterface(enic))
            _c.NoteEnumInterfaces();
        return token != 0 ? TypeInfoExpr(target, token) : TypeInfoExpr(target);
    }

    /// <summary>Whether <see cref="FormatElement"/> formats an element of
    /// <paramref name="elem"/>.</summary>
    private static bool CanFormatElement(TypeDesc elem) =>
        elem is { Kind: TypeKind.Class, Class.IsEnum: true } || ScalarFormat(elem, "$") is not null;

    /// <summary>The runtime call that formats the enumerated element in the temp
    /// <paramref name="cur"/> as its ToString does: an enum boxed under its own type-info,
    /// so it formats by name, a reference or primitive through
    /// <see cref="ScalarFormat"/>.</summary>
    private string FormatElement(TypeDesc elem, string cur)
    {
        if (elem is not { Kind: TypeKind.Class, Class.IsEnum: true })
            return ScalarFormat(elem, cur)
                ?? throw new InvalidOperationException($"{elem} elements are not formattable");
        string eti = TypeInfoExpr(elem)
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: joining {elem} has no emitted type-info");
        return $"dn2cpp_object_tostring(dn2cpp_box({eti}, &{cur}, sizeof({CppTypes.Of(elem)})))";
    }

    /// <summary>The runtime call that formats a reference or primitive element to its
    /// invariant string, or null when the element type is unsupported (the
    /// int/long/double/reference set of the array Join helpers, plus bool/char).</summary>
    private static string? ScalarFormat(TypeDesc elem, string cur)
    {
        if (CppTypes.KindOf(elem) == StackKind.Ref)
            return $"dn2cpp_object_tostring((Dn2CppObject*)({cur}))";
        if (elem.Kind != TypeKind.Primitive)
            return null;
        return elem.Primitive switch
        {
            PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
                or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                => $"dn2cpp_int_to_string((int32_t)({cur}))",
            // The unsigned 32/64-bit elements must format unsigned —
            // dn2cpp_int_to_string would print uint.MaxValue as -1.
            PrimitiveTypeCode.UInt32 => $"dn2cpp_format_uint((uint32_t)({cur}), 4, nullptr)",
            PrimitiveTypeCode.Int64 => $"dn2cpp_long_to_string((int64_t)({cur}))",
            PrimitiveTypeCode.UInt64 => $"dn2cpp_format_uint((uint64_t)({cur}), 8, nullptr)",
            PrimitiveTypeCode.Double => $"dn2cpp_double_to_string({cur})",
            PrimitiveTypeCode.Boolean => $"dn2cpp_bool_to_string({cur})",
            PrimitiveTypeCode.Char => $"dn2cpp_char_to_string((char16_t)({cur}))",
            _ => null,
        };
    }

    /// <summary>Decodes the closed signature of a generic-method call (a
    /// MethodSpecification), substituting <paramref name="methodArgs"/> for the
    /// method's generic parameters — so the parameter types come back fully
    /// instantiated (e.g. <c>IEnumerable&lt;Task&lt;int&gt;&gt;</c>).</summary>
    private MethodSignature<TypeDesc> DecodeGenericCallSignature(
        MethodSpecificationHandle msh, TypeDesc[] methodArgs)
    {
        var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
        var ms = _reader.GetMethodSpecification(msh);
        return ms.Method.Kind == HandleKind.MethodDefinition
            ? _reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(_c.SigProvider, ctx)
            : _reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(_c.SigProvider, ctx);
    }

    /// <summary>Emits the one-time registration of the real OperationCanceledException
    /// and TaskCanceledException type-infos with the runtime, so a CANCELED task carries
    /// a TaskCanceledException and ThrowIfCancellationRequested throws an
    /// OperationCanceledException — both catchable by a typed clause.
    /// No-op if the type was not reached (a program can't catch what it never names).</summary>
    private void EmitCanceledExcRegistration()
    {
        if (_c.CanceledExceptionTypeInfoName is { } n)
            Emit($"dn2cpp_set_canceled_exception_type(&{n});");
        if (_c.TaskCanceledExceptionTypeInfoName is { } tn)
            Emit($"dn2cpp_set_task_canceled_exception_type(&{tn});");
    }

    /// <summary>Pops the input-task operand(s) of a <c>Task.WhenAll</c>/<c>WhenAny</c>
    /// call and yields a <c>Dn2CppArrayRef*</c> of them, covering all source shapes: a
    /// single <c>Task[]</c>/<c>Task&lt;T&gt;[]</c> operand passes straight through; loose
    /// <c>Task</c> operands are gathered into a fresh ref array; and a single
    /// <c>IEnumerable&lt;Task&lt;T&gt;&gt;</c> is materialized by an inline
    /// interface-enumeration loop into a growable <c>Dn2CppRefList</c> → ref array.</summary>
    private string PopTaskArrayOperand(System.Collections.Immutable.ImmutableArray<TypeDesc> paramTypes)
    {
        if (paramTypes is [{ Kind: TypeKind.SZArray }])
            return $"(Dn2CppArrayRef*)({Pop().Expr})";
        // The.NET 9+ `params ReadOnlySpan<Task<T>>` overload (3+ loose tasks): Roslyn
        // lowers the loose args through an [InlineArray] of N tasks and a span over it
        // (already transpiled by the time we consume it). Copy the span's contiguous
        // {reference,length} into a ref array.
        if (paramTypes is [{ Kind: TypeKind.Class, Class: { IsValueType: true } sp }]
            && _c.GenericDefFullName(sp) is "System.ReadOnlySpan" or "System.Span"
            && sp.Context.TypeArgs.Length == 1)
        {
            var span = Pop();
            string s = NewTemp(CppTypes.Of(paramTypes[0]));
            Emit($"{s} = {span.Expr};");
            return $"dn2cpp_refspan_to_array((Dn2CppObject**){s}.f__reference, {s}.f__length)";
        }
        // A single IEnumerable<Task<T>> operand: enumerate it into a ref array. The
        // element type (Task / Task<T>) is the IEnumerable's closed type argument.
        if (paramTypes is [{ Kind: TypeKind.Class, Class: { } col }]
            && _c.GenericDefFullName(col) == "System.Collections.Generic.IEnumerable"
            && col.Context.TypeArgs.Length == 1)
        {
            var elem = col.Context.TypeArgs[0];
            if (_c.EnumerationDispatch(elem) is not { } ed)
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: Task.WhenAll/WhenAny over " +
                    $"IEnumerable<{elem}> could not resolve the enumeration interfaces");
            var src = Pop();
            string e = NewTemp(CppTypes.Of(ed.GetEnumerator.Signature.ReturnType)); // IEnumerator<T>*
            Emit($"{e} = {EmitIfaceDispatch(ed.GetEnumerator, src.Expr)};");
            string list = NewTemp("Dn2CppRefList*");
            Emit($"{list} = dn2cpp_reflist_new();");
            Emit($"while ({EmitIfaceDispatch(ed.MoveNext, e)}) {{");
            Emit($"    dn2cpp_reflist_add({list}, (Dn2CppObject*)({EmitIfaceDispatch(ed.GetCurrent, e)}));");
            Emit("}");
            string arr = NewTemp("Dn2CppArrayRef*");
            Emit($"{arr} = dn2cpp_reflist_to_array({list});");
            return arr;
        }
        if (paramTypes.Length >= 2)
        {
            int n = paramTypes.Length;
            var elems = new string[n];
            for (int i = n - 1; i >= 0; i--)
                elems[i] = Cast(Pop(), "Dn2CppObject*");
            string arr = NewTemp("Dn2CppArrayRef*");
            Emit($"{arr} = dn2cpp_newarr_ref({n});");
            for (int i = 0; i < n; i++)
                Emit($"{arr}->data[{i}] = {elems[i]};");
            Emit($"dn2cpp_gc_write_barrier((void*)({arr}));");
            return arr;
        }
        throw new NotSupportedException(
            $"{_method.DeclaringClass.FullName}.{_method.Name}: Task.WhenAll/WhenAny over " +
            "this operand shape is not supported (expected an array, a params " +
            "ReadOnlySpan<Task>, 2+ loose tasks, or an IEnumerable<Task>)");
    }
}
