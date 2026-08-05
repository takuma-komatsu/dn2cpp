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
    /// null when the operand has no tracked List&lt;T&gt; type (e.g. a call result),
    /// letting callers fall back to their existing diagnostic.</summary>
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

    /// <summary>True if <paramref name="op"/>'s tracked static type is a concrete
    /// (instantiable) class — i.e. not the bare interface or a value type. Used to
    /// gate the enumerator-path Join/Concat: a concrete operand is a real
    /// managed object with an interface map, so dispatching IEnumerable&lt;T&gt;
    /// on it is sound; a bare <c>IEnumerable&lt;T&gt;</c> static type could be a raw
    /// array (no managed interface map), so it stays out of scope.</summary>
    private static bool IsConcreteEnumerableOperand(StackEntry op) =>
        op.StaticType is { Kind: TypeKind.Class, Class: { IsInterface: false, IsValueType: false } };

    /// <summary>True if <paramref name="op"/>'s static type is a bare generic
    /// collection *interface* (<c>IEnumerable&lt;T&gt;</c>, <c>IReadOnlyList</c>,
    /// <c>IReadOnlyCollection</c>, <c>ICollection</c>, <c>IList</c>) or a LINQ
    /// enumerable-result interface (<c>IOrderedEnumerable&lt;T&gt;</c>, the static
    /// type <c>OrderBy</c>/<c>ThenBy</c> return — <c>string.Join(sep,
    /// seq.Select(...).OrderBy(...))</c> hands one straight to <c>Join&lt;T&gt;</c>).
    /// All guarantee <c>IEnumerable&lt;T&gt;</c>, so the enumeration dispatch is sound.
    /// The runtime object behind such a type could be EITHER a raw array (no managed
    /// interface map) OR a managed collection, so the Join/Concat dual path
    /// discriminates at runtime by the array type-info.</summary>
    private bool IsBareCollectionInterface(StackEntry op) =>
        op.StaticType is { Kind: TypeKind.Class, Class: { IsInterface: true } ic }
        && _c.GenericDefFullName(ic) is "System.Collections.Generic.IEnumerable"
            or "System.Collections.Generic.IReadOnlyList"
            or "System.Collections.Generic.IReadOnlyCollection"
            or "System.Collections.Generic.ICollection"
            or "System.Collections.Generic.IList"
            or "System.Linq.IOrderedEnumerable";

    /// <summary>Whether the enumerator-path Join/Concat supports element type
    /// <paramref name="elem"/> — checked before popping the operand so a Concat
    /// fallthrough leaves the stack intact.</summary>
    private bool CanEmitEnumerableJoin(TypeDesc elem) =>
        _c.EnumerationDispatch(elem) is not null && FormatElement(elem) is not null;

    /// <summary>Emits <c>string.Join</c>/<c>Concat</c> over a concrete
    /// <c>IEnumerable&lt;T&gt;</c> collection (e.g. <c>SortedSet&lt;T&gt;</c>, a
    /// <c>SortedDictionary</c> <c>Keys</c>/<c>Values</c> view) as an inline
    /// interface-enumeration loop, then pushes the result. <paramref name="sepStr"/>
    /// is the lowered separator, or null for <c>Concat</c>. Returns false (emitting
    /// nothing, consuming nothing) when the element type is unsupported.</summary>
    private bool TryEmitEnumerableJoin(StackEntry src, TypeDesc elem, string? sepStr)
    {
        if (EmitEnumerationToSb(src, elem, sepStr) is not { } sb)
            return false;
        Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_sb_tostring({sb})");
        return true;
    }

    /// <summary>Emits <c>string.Join</c>/<c>Concat</c> over a bare collection
    /// <em>interface</em> operand, which could be a raw array or a managed collection
    /// at runtime. Branches on the array type-info: an array uses the count-aware
    /// array Join/Concat helper; anything else is a managed collection and enumerates
    /// via the interface. Closes the array-as-<c>IEnumerable&lt;T&gt;</c> + bare
    /// arbitrary-<c>IEnumerable&lt;T&gt;</c> shapes. Returns false (emitting nothing)
    /// when either branch can't be built for the element type.</summary>
    private bool TryEmitBareInterfaceJoin(StackEntry src, TypeDesc elem, string? sepStr)
    {
        if (!CanEmitEnumerableJoin(elem) || ArrayJoinExpr(elem, "$", sepStr) is null)
            return false;
        string result = NewTemp("Dn2CppString*");
        // An array carries DN2CPP_TF_ARRAY in its type-info (whether the shared
        // array_{ref,i4} handle or a precise per-element ti_arr_<T>); a managed
        // collection carries its own class type-info, never an array one — so the flag
        // test discriminates "array vs managed collection" regardless of which handle.
        Emit($"if ((((Dn2CppObject*)({src.Expr}))->type->flags & DN2CPP_TF_ARRAY) != 0) {{");
        Emit($"    {result} = {ArrayJoinExpr(elem, src.Expr, sepStr)};");
        Emit("} else {");
        string sb = EmitEnumerationToSb(src, elem, sepStr)!; // support pre-checked
        Emit($"    {result} = dn2cpp_sb_tostring({sb});");
        Emit("}");
        _stack.Add(new StackEntry(result, StackKind.Ref, "Dn2CppString*"));
        return true;
    }

    /// <summary>Emits the interface-enumeration loop (cast to <c>IEnumerable&lt;T&gt;</c>,
    /// callvirt <c>GetEnumerator</c>, loop <c>MoveNext</c>/<c>get_Current</c>
    /// appending each formatted element) and returns the <c>StringBuilder</c> temp
    /// holding the accumulation — exactly what a hand-written <c>foreach</c> lowers
    /// to. Returns null (emitting nothing) when the element type is unsupported or the
    /// enumeration interfaces aren't loaded. <paramref name="sepStr"/> null ⇒ no
    /// separator (<c>Concat</c>).</summary>
    private string? EmitEnumerationToSb(StackEntry src, TypeDesc elem, string? sepStr)
    {
        if (_c.EnumerationDispatch(elem) is not { } ed || FormatElement(elem) is null)
            return null;

        string enumCpp = CppTypes.Of(ed.GetEnumerator.Signature.ReturnType); // IEnumerator<T>*
        string e = NewTemp(enumCpp);
        Emit($"{e} = {EmitIfaceDispatch(ed.GetEnumerator, src.Expr)};");
        string sb = NewTemp("Dn2CppStringBuilder*");
        Emit($"{sb} = dn2cpp_sb_new();");
        string first = sepStr is null ? "" : NewTemp("int32_t");
        if (sepStr is not null)
            Emit($"{first} = 1;");
        string cur = NewTemp(CppTypes.Of(elem));
        Emit($"while ({EmitIfaceDispatch(ed.MoveNext, e)}) {{");
        if (sepStr is not null)
        {
            Emit($"    if (!{first}) dn2cpp_sb_append_str({sb}, {sepStr});");
            Emit($"    {first} = 0;");
        }
        Emit($"    {cur} = {EmitIfaceDispatch(ed.GetCurrent, e)};");
        Emit($"    dn2cpp_sb_append_str({sb}, {FormatElement(elem, cur)});");
        Emit("}");
        return sb;
    }

    /// <summary>An interface-method callvirt: resolve <paramref name="mth"/>'s slot in
    /// the receiver's runtime type-info via <c>dn2cpp_resolve_interface</c> and call
    /// through it. Used by the inline enumeration loops.</summary>
    private string EmitIfaceDispatch(MethodInfo mth, string recvExpr)
    {
        NoteDispatchSignatureTypes(mth);
        return $"(({FnPtrType(mth)})(dn2cpp_resolve_interface(((Dn2CppObject*){recvExpr})->type, "
            + $"&{mth.DeclaringClass.CppTypeInfoName})[{mth.VtableSlot}]))"
            + $"(({mth.DeclaringClass.CppStructName}*){recvExpr})";
    }

    /// <summary>The array Join (with <paramref name="sepStr"/>) or Concat
    /// (<paramref name="sepStr"/> null ⇒ empty separator) helper call for an array of
    /// <paramref name="t"/> at <paramref name="arrExpr"/>, or null for an unsupported
    /// element type. Used by the bare-interface dual path's array branch.</summary>
    private static string? ArrayJoinExpr(TypeDesc t, string arrExpr, string? sepStr)
    {
        string sep = sepStr ?? "dn2cpp_string_literal(u\"\", 0)";
        return RepOf(t) switch
        {
            ArrRep.I4 => $"dn2cpp_string_join_i4({sep}, (Dn2CppArrayI4*)({arrExpr}))",
            ArrRep.Ref when sepStr is null => $"dn2cpp_string_concat_objects((Dn2CppArrayRef*)({arrExpr}))",
            ArrRep.Ref => $"dn2cpp_string_join_ref({sepStr}, (Dn2CppArrayRef*)({arrExpr}))",
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 } =>
                $"dn2cpp_string_join_i8({sep}, (Dn2CppArrayN*)({arrExpr}))",
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } =>
                $"dn2cpp_string_join_r8({sep}, (Dn2CppArrayN*)({arrExpr}))",
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } =>
                $"dn2cpp_string_join_ch({sep}, (Dn2CppArrayN*)({arrExpr}))",
            _ => null,
        };
    }

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

    /// <summary>The runtime call that formats a single enumerated element to its
    /// invariant string, or null when the element type is unsupported (the
    /// int/long/double/reference set of the array Join helpers, plus bool/char).
    /// Enums are excluded — their invariant ToString is the member name, not the
    /// integer, which this path does not yet produce. <paramref name="cur"/> defaults
    /// to a placeholder so null-ness can be probed for supportedness before a temp
    /// exists.</summary>
    private static string? FormatElement(TypeDesc elem, string cur = "$")
    {
        if (elem is { Kind: TypeKind.Class, Class.IsEnum: true })
            return null;
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
            return arr;
        }
        throw new NotSupportedException(
            $"{_method.DeclaringClass.FullName}.{_method.Name}: Task.WhenAll/WhenAny over " +
            "this operand shape is not supported (expected an array, a params " +
            "ReadOnlySpan<Task>, 2+ loose tasks, or an IEnumerable<Task>)");
    }
}
