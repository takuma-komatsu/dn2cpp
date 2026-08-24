using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    // ---- token resolution ----

    /// <summary>Peeks a field token's (declaring TypeDesc, field name) without resolving a
    /// FieldInfo — for intercepting the vector half-field reads
    /// (Vector256/512&lt;T&gt;._lower/_upper), whose intrinsic-mapped declaring types model
    /// no member fields. Returns (null, "") for any field whose parent is not a closed
    /// generic instance (the only shape these vector fields take).</summary>
    private (TypeDesc?, string) PeekFieldRef(int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind == HandleKind.MemberReference)
        {
            var mr = _reader.GetMemberReference((MemberReferenceHandle)handle);
            if (mr.Parent.Kind == HandleKind.TypeSpecification)
                return (_reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent)
                            .DecodeSignature(_c.SigProvider, _method.Context),
                        _reader.GetString(mr.Name));
        }
        return (null, "");
    }

    private (ClassInfo, FieldInfo) ResolveField(int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind == HandleKind.MemberReference)
            return _c.ResolveMemberRefField(_module, (MemberReferenceHandle)handle, _method.Context);
        if (handle.Kind != HandleKind.FieldDefinition)
            throw new NotSupportedException("External field access is not supported");
        var fd = _reader.GetFieldDefinition((FieldDefinitionHandle)handle);
        var declType = fd.GetDeclaringType();
        string fname = _reader.GetString(fd.Name);
        // A FieldDef inside a generic instance body resolves against the
        // current specialization (same template), substituting type args.
        var cls = _method.DeclaringClass.GenericArity > 0 && _method.DeclaringClass.Handle == declType
            ? _method.DeclaringClass
            : _c.GetClass(_module, declType);
        var fld = cls.Fields.First(f => f.Name == fname);
        return (cls, fld);
    }

    /// <summary>Resolves a method token the way a call site does — a plain
    /// MethodDefinition directly, a generic-instantiated method (MethodSpecification)
    /// or a method on a generic / cross-module type (MemberReference) through the
    /// same resolvers a call goes through. <paramref name="op"/> names the opcode in
    /// the unsupported-kind throw (the ldftn/ldvirtftn arms).</summary>
    private MethodInfo ResolveMethodHandle(EntityHandle handle, string op) => handle.Kind switch
    {
        HandleKind.MethodDefinition => _c.GetMethod(_module, (MethodDefinitionHandle)handle),
        HandleKind.MethodSpecification => _c.ResolveMethodSpec(_module, (MethodSpecificationHandle)handle, _method.Context),
        HandleKind.MemberReference => _c.ResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context),
        _ => throw new NotSupportedException($"{op} of {handle.Kind} is not supported"),
    };

    private TypeDesc ResolveTypeToken(int token)
    {
        var handle = SRME.EntityHandle(token);
        var t = handle.Kind switch
        {
            HandleKind.TypeDefinition => _c.GetTypeDescForDefinition(_module, (TypeDefinitionHandle)handle),
            HandleKind.TypeReference => MakeExternal((TypeReferenceHandle)handle),
            HandleKind.TypeSpecification =>
                _reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(_c.SigProvider, _method.Context),
            _ => throw new NotSupportedException($"Type token kind {handle.Kind} is not supported"),
        };
        // A reference type named by a type token may be live only in a dead
        // branch; record it so its metadata is emitted opaquely. An enum is
        // recorded too — `o is DayOfWeek`/`(DayOfWeek)o`/`box` reference its per-enum
        // type-info, which the emitter materializes for each referenced enum.
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: false } cls })
            _c.NoteReferencedType(cls);
        return t;

        TypeDesc MakeExternal(TypeReferenceHandle h)
        {
            var tr = _reader.GetTypeReference(h);
            string ns = _reader.GetString(tr.Namespace);
            string n = _reader.GetString(tr.Name);
            string full = string.IsNullOrEmpty(ns) ? n : ns + "." + n;
            return full switch
            {
                "System.Boolean" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Boolean),
                "System.Char" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Char),
                "System.SByte" => TypeDesc.MakePrimitive(PrimitiveTypeCode.SByte),
                "System.Byte" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Byte),
                "System.Int16" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int16),
                "System.UInt16" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt16),
                "System.Int32" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int32),
                "System.UInt32" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt32),
                "System.Int64" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Int64),
                "System.UInt64" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UInt64),
                "System.IntPtr" => TypeDesc.MakePrimitive(PrimitiveTypeCode.IntPtr),
                "System.UIntPtr" => TypeDesc.MakePrimitive(PrimitiveTypeCode.UIntPtr),
                "System.Single" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Single),
                "System.Double" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Double),
                "System.String" => TypeDesc.MakePrimitive(PrimitiveTypeCode.String),
                "System.Object" => TypeDesc.MakePrimitive(PrimitiveTypeCode.Object),
                // Prefer a loaded cross-assembly *value type* so it keeps its class
                // identity — and any intrinsic mapping — for initobj/ldobj and struct
                // layout. An **enum** needs its own arm (it carries IsEnum, not
                // IsValueType) so `o is DayOfWeek` lowers through the int32 enum path
                // instead of the unsupported-External throw. The **non-generic SZArray
                // collection interfaces** resolve to their loaded class so a runtime
                // `(IList)arr` cast/`is` verifies against the interface's type-info — the
                // same symbol the array's per-element dispatch map entry uses. Other
                // reference types stay External: a caught exception type must keep
                // degrading to a catch-all rather than naming a per-type metadata symbol
                // the runtime-trap exceptions never materialize.
                _ => _c.ResolveTypeRef(_module, h) is { Kind: TypeKind.Class, Class: { } rcls } rc
                        && (rcls.IsValueType || rcls.IsEnum
                            || rcls.FullName is "System.Collections.IList" or "System.Collections.ICollection")
                    ? rc
                    : TypeDesc.MakeExternal(full),
            };
        }
    }

    /// <summary>The parenthesised assembly-reference note for a type token that degraded to
    /// an <see cref="TypeKind.External"/> — <c>" (assembly 'System.Runtime.Numerics')"</c> — or
    /// the empty string when the token names no AssemblyReference (a TypeSpec, or a
    /// same-module scope). The name is what the caller has to pass to <c>-r</c>, so the
    /// diagnostic that carries it is actionable rather than merely accurate; a NESTED
    /// TypeRef scopes to its declaring TypeRef, so the walk climbs to the assembly row
    /// (a bare "no scope" note for <c>Outer.Inner</c> would name nothing to install).</summary>
    private string ExternalTokenAssemblyNote(int token)
    {
        var handle = SRME.EntityHandle(token);
        if (handle.Kind != HandleKind.TypeReference)
            return "";
        var scope = _reader.GetTypeReference((TypeReferenceHandle)handle).ResolutionScope;
        // Bounded by the nesting depth of the TypeRef row; a malformed self-referential
        // scope would otherwise spin, so the walk is capped rather than trusted.
        for (int depth = 0; scope.Kind == HandleKind.TypeReference && depth < 64; depth++)
            scope = _reader.GetTypeReference((TypeReferenceHandle)scope).ResolutionScope;
        if (scope.Kind != HandleKind.AssemblyReference)
            return "";
        string asm = _reader.GetString(
            _reader.GetAssemblyReference((AssemblyReferenceHandle)scope).Name);
        return $" (assembly '{asm}')";
    }

    /// <summary>Resolves a <c>castclass</c>/<c>isinst</c>/catch/<c>ldtoken</c> TARGET type token.
    /// Unlike <see cref="ResolveTypeToken"/>, which degrades a cross-assembly reference
    /// TypeRef to a catch-all <c>External</c>, this promotes such a degraded reference type
    /// to its loaded <see cref="ClassInfo"/> so the cast verifies a unique per-type type-info
    /// instead of folding to an identity match — an un-emitted ref target makes
    /// <c>o is T</c> always true and a typed catch a catch-all. The promoted type is noted so
    /// its <c>ti_*</c> is emitted; the target needs only a unique symbol, not a walked base
    /// chain, since whatever materializes an instance emits the chain. A runtime-raised
    /// exception type stays <c>External</c>: <see cref="TypeInfoExpr"/> maps it to the shared
    /// runtime handle, so the cast still verifies a real type with no per-type emitted
    /// symbol.</summary>
    private TypeDesc ResolveCastTarget(int token)
    {
        var t = ResolveTypeToken(token);
        if (t is not { Kind: TypeKind.External, ExternalName: { } name }
            || CoreIntrinsics.RuntimeExceptionTypeInfo(name) is not null)
            return t;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind == HandleKind.TypeReference
            && _c.ResolveTypeRef(_module, (TypeReferenceHandle)handle)
                is { Kind: TypeKind.Class, Class: { IsValueType: false, IsEnum: false } rcls } rc)
        {
            _c.NoteReferencedType(rcls);
            return rc;
        }
        return t;
    }

    /// <summary>If <paramref name="target"/> is an OPEN generic definition — the operand of
    /// <c>typeof(List&lt;&gt;)</c> / <c>typeof(IDictionary&lt;,&gt;)</c> — the shared
    /// <c>&amp;gendef_&lt;sym&gt;</c> open-definition type-info handle its <c>ldtoken</c>
    /// lowers to, else null. A cross-assembly definition decodes to an <c>External</c>
    /// backtick name (dn2cpp mints no ClassInfo for it); a same-assembly one to a
    /// <c>Template</c> over its TypeDef. Either way the symbol mirrors
    /// <see cref="CppEmitter"/>'s <c>GenericDefInfo</c> naming exactly — so it resolves to the
    /// same handle a closed instantiation's <c>genericDef</c> points at — and
    /// <see cref="Compilation.NoteTypeofOpenGenericDef"/> makes the emitter guarantee the
    /// symbol is defined even when the program instantiates no close of the definition
    /// (cut ⟹ route). NESTED definitions are carved out exactly as <c>GenericDefInfo</c>
    /// carves them (a nested type's FullName is not the simple namespaced name), staying the
    /// pre-existing null fold.</summary>
    private string? OpenGenericDefTypeInfoExpr(TypeDesc target)
    {
        // The name plus, for a synthetic gendef (a def no closed instantiation minted a
        // handle for), the definition's KIND bits so IsInterface/IsAbstract/
        // IsValueType/IsSealed answer, and its parameter names so ToString spells the
        // bracket group. All three are facts about the definition's own metadata.
        (string? Name, GenericDefKind Kind, string? ParamNames) def = target switch
        {
            // Cross-assembly: no ClassInfo or Template to open, so the metadata is located
            // by NAME through the type index — one lookup answering both facts.
            { Kind: TypeKind.External, ExternalName: { } en } when en.Contains('`') =>
                _c.OpenGenericDefFactsByName(en),
            { Kind: TypeKind.Template, TemplateModule: { } tm } =>
                (OpenDefBacktickName(tm, target.TemplateHandle), Compilation.ResolveOpenGenericDefKind(tm, target.TemplateHandle),
                 Compilation.GenericParamNames(tm, target.TemplateHandle)),
            // Defensive: a same-assembly open def that reached ClassMap as an arity-bearing,
            // argument-free ClassInfo rather than a Template.
            { Kind: TypeKind.Class, Class: { GenericArity: > 0 } c } when c.Context.TypeArgs.Length == 0
                => (OpenDefBacktickName(c.Module, c.Handle), ClassKindFlags(c), Compilation.GenericParamNames(c.Module, c.Handle)),
            _ => (null, GenericDefKind.Unknown, null),
        };
        if (def.Name is null)
            return null;
        _c.NoteTypeofOpenGenericDef(def.Name, def.Kind, def.ParamNames);
        return "&gendef_" + CppNaming.Sanitize(def.Name);
    }

    /// <summary>The <see cref="GenericDefKind"/> bits of a resolved open-definition
    /// <see cref="ClassInfo"/> — its interface/abstract/value-type/sealed nature (invariant
    /// across every close). An interface is also abstract, mirroring the type-info kind rule.</summary>
    private static GenericDefKind ClassKindFlags(ClassInfo c)
    {
        var kind = GenericDefKind.Unknown;
        if (c.IsValueType) kind |= GenericDefKind.ValueType;
        if (c.IsInterface) kind |= GenericDefKind.Interface | GenericDefKind.Abstract;
        else if (c.IsAbstract) kind |= GenericDefKind.Abstract;
        if (c.IsSealed) kind |= GenericDefKind.Sealed;
        return kind;
    }

    /// <summary>The CLR backtick full name of the top-level open generic definition at
    /// <paramref name="handle"/> in <paramref name="m"/> (e.g. "MyNs.Box`1"), or null for a
    /// nested one — the same shape and carve-out as <c>CppEmitter.GenericDefInfo</c>.</summary>
    internal static string? OpenDefBacktickName(Module m, TypeDefinitionHandle handle)
    {
        var reader = m.Reader;
        var td = reader.GetTypeDefinition(handle);
        if (!td.GetDeclaringType().IsNil)
            return null;
        string name = reader.GetString(td.Name);
        string ns = reader.GetString(td.Namespace);
        return string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    }

    /// <summary>Body-emission wrapper over <see cref="TypeInfoExprOf"/>: a
    /// shared-body candidate materializing a placeholder-bearing type's runtime
    /// identity is instantiation-dependent (a placeholder primitive would
    /// silently yield its underlying primitive's handle — e.g. an enum boxing
    /// with int's type-info), so it taints the trial compile.</summary>
    internal string? TypeInfoExpr(TypeDesc t)
    {
        TaintIfCanonical(t, "typeinfo");
        var e = TypeInfoExprOf(t);
        NoteTypeInfoUse(t, e);
        return e;
    }

    /// <summary>Records that this body names the type-info handle <paramref name="expr"/>
    /// for <paramref name="t"/> — the named half of the type-info backstop — and, when the
    /// handle is a per-type <c>&amp;ti_&lt;T&gt;</c> of an INTRINSIC-modeled class, notes that
    /// class referenced so a minimal type-info is emitted for it. A reference intrinsic type
    /// (StringBuilder, CultureInfo, …) is already noted at token resolution; a boxed intrinsic
    /// VALUE type (Vector128&lt;T&gt;) is not noted anywhere else — <c>box</c> never notes and
    /// <c>ldtoken</c> notes only NON-intrinsic value types — and the opaque referenced-type
    /// pass skips it because it is intrinsic, so without this its <c>ti_</c> is undeclared at
    /// C++ link. Idempotent, and scoped to intrinsic classes so non-intrinsic emit is
    /// untouched (they are already in the emit set). A runtime handle / rgctx slot / null fold
    /// is not a <c>&amp;ti_</c> and notes nothing.</summary>
    private void NoteTypeInfoUse(TypeDesc t, string? expr)
    {
        _c.NoteNamedTypeInfoSymbol(_method, expr);
        if (expr is not null && expr.StartsWith("&ti_", System.StringComparison.Ordinal)
            && t is { Kind: TypeKind.Class, Class: { IntrinsicCppName: not null } cls })
            _c.NoteTypeIdentityClosure(t);
    }

    /// <summary>Token-carrying variant for instruction-level sites (box/unbox/
    /// castclass/isinst, whose raw type token IS what resolved to
    /// <paramref name="t"/>): a placeholder-bearing type loads its real
    /// per-instantiation type-info out of an rgctx slot keyed on that token
    /// instead of tainting.</summary>
    internal string? TypeInfoExpr(TypeDesc t, int token)
    {
        if (SharedTrial && Compilation.ContainsCanonPlaceholder(t))
            return "(const Dn2CppTypeInfo*)"
                + RgctxSlotAccess(RgctxSlotKind.TypeInfo, token, "typeinfo", t);
        var e = TypeInfoExprOf(t);
        NoteTypeInfoUse(t, e);
        return e;
    }

    /// <summary>Like <see cref="TypeInfoExpr(TypeDesc,int)"/> for sites whose
    /// emitted identity is the token type's FIRST type argument rather than the
    /// token type itself — Nullable&lt;T&gt; boxing (the box carries T) and the
    /// IComparable&lt;T&gt; boxed-primitive cast. <paramref name="arg0"/> is the
    /// already-projected argument; the slot re-projects per instantiation.</summary>
    internal string? TypeArg0TypeInfoExpr(TypeDesc arg0, int token)
    {
        if (SharedTrial && Compilation.ContainsCanonPlaceholder(arg0))
            return "(const Dn2CppTypeInfo*)"
                + RgctxSlotAccess(RgctxSlotKind.TypeArg0TypeInfo, token, "typeinfo", arg0);
        var e = TypeInfoExprOf(arg0);
        NoteTypeInfoUse(arg0, e);
        return e;
    }

    /// <summary>C++ expression for the Dn2CppTypeInfo of a type, or null when
    /// no runtime metadata exists (e.g. System.Object).</summary>
    internal static string? TypeInfoExprOf(TypeDesc t) => t.Kind switch
    {
        // INVARIANT: a CLASS-kind type's identity is ONE question, answered by
        // ClassInfo.CppTypeInfoName (the `TypeKind.Class` fall-through near the bottom of
        // this switch). A second, name-keyed list here would cover `typeof` while the base
        // pointers, registry rows and reflected FieldTypes that ask CppTypeInfoName got the
        // other answer — one CLR type, two type-infos, and
        // `typeof(MyClass).BaseType == typeof(object)` false. The arms below therefore key
        // on the runtime STRUCT (IntrinsicCppName), never on a name: one struct serves many
        // names, which no per-name table can express.
        //
        // The intrinsic Task-family awaiter structs, keyed on their runtime struct (every
        // TaskAwaiter(<T>)/ValueTaskAwaiter(<T>)/Configured* form shares Dn2CppTaskAwaiter):
        // a real async builder's AwaitUnsafeOnCompleted IL boxes its TAwaiter (Roslyn's
        // `(object)default(TAwaiter) != null` value-type probe). The branch folds, but the
        // box expression still renders, so these need a shared runtime handle rather than
        // the never-emitted ti_*.
        TypeKind.Class when t.Class!.IsValueType && t.Class.IntrinsicCppName == "Dn2CppTaskAwaiter"
            => "&dn2cpp_task_awaiter_type",
        TypeKind.Class when t.Class!.IsValueType && t.Class.IntrinsicCppName == "Dn2CppYieldAwaiter"
            => "&dn2cpp_yield_awaiter_type",
        // The non-generic Task owns the runtime handle every fresh Dn2CppTask starts with.
        // Closed Task<T> and adopted reference-task types keep the same C++ struct but name
        // emitted per-close handles; producer mouths stamp those before the task escapes.
        TypeKind.Class when t.Class!.IntrinsicCppName == "Dn2CppTask*"
                            && t.Class.GenericArity == 0
                            && t.Class.FullName == "System.Threading.Tasks.Task"
            => "&dn2cpp_task_type",
        // ThreadLocal<T> and BlockingCollection<T> remain erased — one
        // struct, one runtime handle, T riding as data rather than in the header
        // (CoreIntrinsics.s_runtimeTypeInfoNotRowed). Without an arm here they fall to the
        // Class arm's never-stamped ti_, and `o is ThreadLocal<string>` reads False on an
        // object whose type IS ThreadLocal<string>.
        //
        // The residue of the erasure, stated rather than hidden: one handle cannot
        // distinguish instantiations, so `o is ThreadLocal<int>` on a ThreadLocal<string>
        // reads TRUE where real .NET reads False. Exact whenever the program holds a single instantiation, and
        // over-accepting only a test a correct program does not write; a per-instantiation
        // handle would mean giving each close its own runtime struct, i.e. dropping the
        // erasure.
        TypeKind.Class when t.Class!.IntrinsicCppName == "Dn2CppThreadLocal*"
            => "&dn2cpp_threadlocal_type",
        TypeKind.Class when t.Class!.IntrinsicCppName == "Dn2CppBlockingCollection*"
            => "&dn2cpp_blockingcollection_type",
        // The one answer for a class: its type-info symbol. That is the runtime's own
        // handle whenever the runtime defines one — the exception set the traps stamp, the
        // reflection hierarchy every Dn2CppType/Dn2CppMethodRef carries, the degraded
        // stack-trace objects, the NFI wrappers dn2cpp_nfi_wrap mints, the primitives,
        // String, Object, Enum — and the emitted ti_ otherwise
        // (CoreIntrinsics.RuntimeTypeInfoSymbol, consulted by ClassInfo.CppTypeInfoName).
        // An enum lands here too: its ti_ is emitted from ReferencedTypes.
        TypeKind.Class => "&" + t.Class!.CppTypeInfoName,
        TypeKind.Primitive => t.Primitive switch
        {
            PrimitiveTypeCode.Boolean => "&dn2cpp_bool_type",
            PrimitiveTypeCode.Char => "&dn2cpp_char_type",
            PrimitiveTypeCode.Byte => "&dn2cpp_byte_type",
            PrimitiveTypeCode.SByte => "&dn2cpp_sbyte_type",
            PrimitiveTypeCode.Int16 => "&dn2cpp_int16_type",
            PrimitiveTypeCode.UInt16 => "&dn2cpp_uint16_type",
            PrimitiveTypeCode.UInt32 => "&dn2cpp_uint32_type",
            PrimitiveTypeCode.Int32 => "&dn2cpp_int32_type",
            PrimitiveTypeCode.Int64 => "&dn2cpp_int64_type",
            PrimitiveTypeCode.UInt64 => "&dn2cpp_uint64_type",
            // IntPtr/UIntPtr box to an 8-byte payload (CppTypes.Of -> intptr_t). The
            // signed/unsigned distinction lives in the type-info, so ToString /
            // interpolation read the payload at the right signedness.
            PrimitiveTypeCode.IntPtr => "&dn2cpp_intptr_type",
            PrimitiveTypeCode.UIntPtr => "&dn2cpp_uintptr_type",
            PrimitiveTypeCode.Single => "&dn2cpp_single_type",
            PrimitiveTypeCode.Double => "&dn2cpp_double_type",
            PrimitiveTypeCode.String => "&dn2cpp_string_type",
            PrimitiveTypeCode.Object => "&dn2cpp_object_type",
            _ => null,
        },
        // An UNRESOLVED type name asks the same table the Class arm does: a cross-assembly
        // TypeRef to System.Object / System.String / System.Void / the reflection classes /
        // the runtime-raised exceptions must keep the SAME symbol a resolved reference to
        // them keeps, or the corelib-less lane hands back a second identity for exactly the
        // types whose objects the runtime allocates.
        TypeKind.External when CoreIntrinsics.RuntimeTypeInfoSymbol(t.ExternalName) is { } rt => rt,
        // A reference-element array reached through THIS generic fallback reads as the
        // shared array_ref_type ("System.Object[]"). The identity-bearing sites do not land
        // here: typeof(T[]), newarr / Array.Empty, the castclass/isinst targets
        // (CastTargetTypeInfoExpr) and the reflection member tables all note the element and
        // name the precise per-element ti_arr_ handle, which is what makes Span<T>'s
        // array-covariance guard (`array.GetType != typeof(T[])`) match exactly. What lands
        // here is a site with no noted element (a catch type, a box identity, an intrinsic's
        // element-type ask), where the shared handle is the conservative answer a
        // runtime-helper-tagged ref array also carries.
        TypeKind.SZArray when CppTypes.KindOf(t.Element!) == StackKind.Ref => "&dn2cpp_array_ref_type",
        _ => null,
    };

    private void TranslateArrayConstructor(TypeDesc arrayType, MemberReference mr)
    {
        // Keys the shared rank>=2 dispatch map: a minted MD array can flow
        // into any of its six CLR interfaces without a statically visible cast.
        _c.NoteMdArrayUse();
        int rank = arrayType.Rank;
        var lengths = new string[rank];
        for (int i = rank - 1; i >= 0; i--)
        {
            lengths[i] = Cast(Pop(), "int32_t");
        }
        
        // The slot width is the packed storage width (StorageOf), so a sub-word
        // element (short/char/bool) array packs like the single-dim path and like
        // the.NET RVA blob — not the int32-promoted Of width.
        string st = CppTypes.StorageOf(arrayType.Element!);
        string lengthsInit = string.Join(", ", lengths);
        string temp = NewTemp("Dn2CppMDArray*");
        // The MD array's runtime identity: an interned per-(element, rank)
        // type-info carrying elementType + arrayRank, so the reflection surface
        // (Array.GetValue/SetValue/GetLength/Rank on a System.Array-typed
        // receiver) can rediscover the shape. A placeholder-bearing element in
        // a shared-body candidate keeps the legacy null header (identity would
        // be instantiation-dependent) rather than tainting the trial.
        string elemTi = MdSzElementTypeInfoExpr(arrayType.Element!)
            ?? (SharedTrial && Compilation.ContainsCanonPlaceholder(arrayType.Element!)
                ? null : TypeInfoExpr(arrayType.Element!)) ?? "nullptr";
        // dn2cpp_i32s(...).v, not the C99 compound literal (const int32_t[]){...}:
        // that form is a GNU extension in C++ that real MSVC rejects (C4576). The
        // helper's temporary lives to the end of the full expression, and
        // dn2cpp_newmdarr copies the lengths during the call. Same at the
        // md_elem_addr sites below.
        Emit($"{temp} = dn2cpp_newmdarr(dn2cpp_mdarr_ti({elemTi}, {rank}), {rank}, dn2cpp_i32s({lengthsInit}).v, (int32_t)sizeof({st}));");
        Push(StackKind.Ref, "Dn2CppMDArray*", temp);
    }

    private void TranslateArrayMethodCall(TypeDesc arrayType, MemberReference mr)
    {
        // An MD accessor member implies an MD array exists somewhere — belt-and-braces
        // beside the ctor's note (a note is a flag, so over-noting costs nothing
        // in an MD-free program).
        if (arrayType.Kind == TypeKind.MDArray)
            _c.NoteMdArrayUse();
        string name = _reader.GetString(mr.Name);
        var sig = mr.DecodeMethodSignature(_c.SigProvider, _method.Context);
        int rank = arrayType.Rank;
        if (arrayType.Kind == TypeKind.SZArray)
        {
            rank = 1;
        }

        if (arrayType.Kind == TypeKind.SZArray)
        {
            if (name == "Get")
            {
                var idx = Pop();
                var arr = Pop();
                string et = CppTypes.Of(arrayType.Element!);
                switch (RepOf(arrayType.Element!))
                {
                    case ArrRep.I4:
                        Push(StackKind.I4, "int32_t", $"dn2cpp_ldelem_i4(({Cast(arr, "Dn2CppArrayI4*")}), {idx.Expr})");
                        break;
                    case ArrRep.Ref:
                        Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_ldelem_ref(({Cast(arr, "Dn2CppArrayRef*")}), {idx.Expr})");
                        break;
                    default:
                        Push(CppTypes.KindOf(arrayType.Element!), et, $"*({et}*)dn2cpp_elem_addr(({Cast(arr, "Dn2CppArrayN*")}), {idx.Expr})");
                        break;
                }
            }
            else if (name == "Set")
            {
                var value = Pop();
                var idx = Pop();
                var arr = Pop();
                string et = CppTypes.Of(arrayType.Element!);
                switch (RepOf(arrayType.Element!))
                {
                    case ArrRep.I4:
                        Emit($"dn2cpp_stelem_i4(({Cast(arr, "Dn2CppArrayI4*")}), {idx.Expr}, {Cast(value, "int32_t")});");
                        break;
                    case ArrRep.Ref:
                        Emit($"dn2cpp_stelem_ref(({Cast(arr, "Dn2CppArrayRef*")}), {idx.Expr}, {Cast(value, "Dn2CppObject*")});");
                        break;
                    default:
                    {
                        string addr = $"dn2cpp_elem_addr(({Cast(arr, "Dn2CppArrayN*")}), {idx.Expr})";
                        Emit($"*({et}*){addr} = {Cast(value, et)};");
                        if (arrayType.Element!.ContainsGcReferences())
                            Emit($"dn2cpp_gc_write_barrier((void*)({addr}));");
                        break;
                    }
                }
            }
            else if (name == "Address")
            {
                var idx = Pop();
                var arr = Pop();
                string et = CppTypes.Of(arrayType.Element!);
                Push(StackKind.Ptr, et + "*", $"({et}*)dn2cpp_elem_addr(({Cast(arr, "Dn2CppArrayN*")}), {idx.Expr})");
            }
            return;
        }

        if (name == "Get")
        {
            var indices = new string[rank];
            for (int i = rank - 1; i >= 0; i--)
                indices[i] = Cast(Pop(), "int32_t");
                
            var arr = Pop();
            string ct = CppTypes.Of(arrayType.Element!);
            string st = CppTypes.StorageOf(arrayType.Element!);
            string indicesInit = string.Join(", ", indices);
            string castedArr = Cast(arr, "Dn2CppMDArray*");

            string addr;
            if (rank == 2)
                addr = $"dn2cpp_md_elem_addr2({castedArr}, {indices[0]}, {indices[1]})";
            else if (rank == 3)
                addr = $"dn2cpp_md_elem_addr3({castedArr}, {indices[0]}, {indices[1]}, {indices[2]})";
            else
                addr = $"dn2cpp_md_elem_addr({castedArr}, dn2cpp_i32s({indicesInit}).v)";

            // Read the packed slot (st) and widen to the int32 stack type; st == ct
            // for every non-sub-word element, so this is the original expression.
            Push(CppTypes.KindOf(arrayType.Element!), ct,
                st == ct ? $"*({ct}*){addr}" : $"({ct})(*({st}*){addr})");
        }
        else if (name == "Set")
        {
            var value = Pop();
            var indices = new string[rank];
            for (int i = rank - 1; i >= 0; i--)
                indices[i] = Cast(Pop(), "int32_t");
                
            var arr = Pop();
            string ct = CppTypes.Of(arrayType.Element!);
            string st = CppTypes.StorageOf(arrayType.Element!);
            string indicesInit = string.Join(", ", indices);
            string castedArr = Cast(arr, "Dn2CppMDArray*");

            string addrExpr;
            if (rank == 2)
                addrExpr = $"dn2cpp_md_elem_addr2({castedArr}, {indices[0]}, {indices[1]})";
            else if (rank == 3)
                addrExpr = $"dn2cpp_md_elem_addr3({castedArr}, {indices[0]}, {indices[1]}, {indices[2]})";
            else
                addrExpr = $"dn2cpp_md_elem_addr({castedArr}, dn2cpp_i32s({indicesInit}).v)";

            // Narrow the int32 stack value into the packed slot; st == ct for every
            // non-sub-word element, so the cast is the original one.
            Emit(st == ct
                ? $"*({ct}*){addrExpr} = {Cast(value, ct)};"
                : $"*({st}*){addrExpr} = ({st})({Cast(value, ct)});");
            if (arrayType.Element!.ContainsGcReferences())
                Emit($"dn2cpp_gc_write_barrier((void*)({addrExpr}));");
        }
        else if (name == "Address")
        {
            var indices = new string[rank];
            for (int i = rank - 1; i >= 0; i--)
                indices[i] = Cast(Pop(), "int32_t");
                
            var arr = Pop();
            // The address points at the packed slot, so its type is the storage type
            // (st == ct for every non-sub-word element).
            string st = CppTypes.StorageOf(arrayType.Element!);
            string indicesInit = string.Join(", ", indices);
            string castedArr = Cast(arr, "Dn2CppMDArray*");

            string addrExpr;
            if (rank == 2)
                addrExpr = $"dn2cpp_md_elem_addr2({castedArr}, {indices[0]}, {indices[1]})";
            else if (rank == 3)
                addrExpr = $"dn2cpp_md_elem_addr3({castedArr}, {indices[0]}, {indices[1]}, {indices[2]})";
            else
                addrExpr = $"dn2cpp_md_elem_addr({castedArr}, dn2cpp_i32s({indicesInit}).v)";

            Push(StackKind.Ptr, st + "*", $"({st}*){addrExpr}");
        }
        else
        {
            throw new NotSupportedException($"Array method {name} is not supported yet");
        }
    }
}
