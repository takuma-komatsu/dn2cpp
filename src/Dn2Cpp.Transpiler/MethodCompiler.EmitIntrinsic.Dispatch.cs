using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void TranslateIntrinsic(MemberReferenceHandle handle)
    {
        var mr = Reader.GetMemberReference(handle);
        string name = Reader.GetString(mr.Name);
        string declType = "?";
        string? adoptedFrom = null;
        if (mr.Parent.Kind == HandleKind.TypeReference)
        {
            var tr = Reader.GetTypeReference((TypeReferenceHandle)mr.Parent);
            string ns = Reader.GetString(tr.Namespace);
            string nm = Reader.GetString(tr.Name);
            declType = string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
            // An adopted third-party async task type reached CROSS-ASSEMBLY — the route a
            // non-generic `async GDTask` method's builder members (Create, SetResult,
            // get_Task) actually take. Route it to the BCL key its members answer to.
            if (Comp.HasAdoptedAsync
                && Comp.ResolveTypeRef(Module, (TypeReferenceHandle)mr.Parent)?.Class is { } pc
                && Comp.AdoptedAsyncKey(pc) is { } adopted)
            {
                adoptedFrom = AdoptedFrom(pc);
                declType = adopted;
            }
            // A nested intrinsic type (e.g. Lock.Scope) decodes to a bare simple name;
            // qualify the dispatch key with its enclosing type so EmitIntrinsic can
            // match it without colliding with any other nested same-named type.
            else if (tr.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                var encl = Reader.GetTypeReference((TypeReferenceHandle)tr.ResolutionScope);
                string encNs = Reader.GetString(encl.Namespace);
                string encFull = string.IsNullOrEmpty(encNs)
                    ? Reader.GetString(encl.Name)
                    : encNs + "." + Reader.GetString(encl.Name);
                if (CoreIntrinsics.IsIntrinsicNested(encFull, nm))
                    declType = CoreIntrinsics.NestedDispatchName(encFull, nm);
            }
        }

        // A member of a BCL exception type that did not resolve to a real body
        // (`base(message)` in a corelib-less `MyEx : SystemException` ctor,
        // `base.Message`, …): try the type's OWN intrinsic arms first — they must keep
        // winning, since an intrinsic-typed member ref never resolves — then fall back to
        // the System.Exception key. Every BCL exception's remaining observable surface in
        // dn2cpp's model IS System.Exception's (the uniform Dn2CppExceptionObject prefix),
        // so the member either lowers there or fails loudly under that key (the reach chain
        // in the message still names the true call site).
        // Route-without-cut on the call-site pair: nothing resolved, so there is no edge
        // to cut (same shape as the RuntimeExceptionTypeInfo newobj-by-name arm in
        // MethodCompiler.Newobj.cs).
        if (adoptedFrom is null && declType != "System.Exception"
            && CoreIntrinsics.IsExternalBclExceptionName(declType))
        {
            var excSig = mr.DecodeMethodSignature(Comp.SigProvider, null);
            if (!TryEmitIntrinsic(declType, name, excSig))
                EmitIntrinsic("System.Exception", name, excSig);
            return;
        }

        EmitIntrinsic(declType, name, mr.DecodeMethodSignature(Comp.SigProvider, null), adoptedFrom);
    }

    /// <summary>Emits a one-argument Console.Write/WriteLine. The value is rendered
    /// by its declared/stack type (char -> 1-char string; object -> Object.ToString);
    /// Write and WriteLine share the same per-type formatting. When
    /// <paramref name="writer"/> is non-null it is a <c>Dn2CppTextWriter*</c> receiver
    /// (Console.Error): the same formatting is routed to the
    /// <c>dn2cpp_textwriter_*(writer, …)</c> family instead of the stdout
    /// <c>dn2cpp_console_*</c> family — byte-for-byte the same text.</summary>
    private void EmitConsoleWrite(StackEntry a, TypeDesc p, bool newline, string? writer = null)
    {
        string fn = newline ? "writeline" : "write";
        // stdout (Console.Out) -> dn2cpp_console_*( … ); a TextWriter receiver
        // (Console.Error) -> dn2cpp_textwriter_*( writer, … ).
        string fam = writer is null ? "console" : "textwriter";
        string pre = writer is null ? "" : writer + ", ";
        if (p.IsString)
            Emit($"dn2cpp_{fam}_{fn}_str({pre}{Cast(a, "Dn2CppString*")});");
        else if (IsDecimal(p))
            Emit($"dn2cpp_{fam}_{fn}_str({pre}dn2cpp_decimal_to_string({DecVal(a)}));");
        else if (p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char })
            Emit($"dn2cpp_{fam}_{fn}_str({pre}dn2cpp_char_to_string((char16_t)({a.Expr})));");
        else if (p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean })
            Emit($"dn2cpp_{fam}_{fn}_bool({pre}{a.Expr});");
        // float and double share the R8 stack slot; the static parameter type tells
        // them apart so a float prints at float (not double) shortest precision.
        else if (p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single })
            Emit($"dn2cpp_{fam}_{fn}_r4({pre}(float)({a.Expr}));");
        else if (a.Kind == StackKind.R8)
            Emit($"dn2cpp_{fam}_{fn}_r8({pre}{a.Expr});");
        // UInt64 shares the I8 stack slot but must print unsigned — the signed _i8
        // helper would render ulong values above Int64.MaxValue as negative.
        else if (p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 })
            Emit($"dn2cpp_{fam}_{fn}_str({pre}dn2cpp_format_uint((uint64_t)({a.Expr}), 8, nullptr));");
        // UInt32 shares the I4 stack slot but must print unsigned — the signed _i4
        // helper would render uint values above Int32.MaxValue as negative (e.g.
        // uint.MaxValue as -1). Truncate through uint32_t so a signed I4 expression
        // is not sign-extended into the high word.
        else if (p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 })
            Emit($"dn2cpp_{fam}_{fn}_str({pre}dn2cpp_format_uint((uint64_t)(uint32_t)({a.Expr}), 4, nullptr));");
        else if (a.Kind == StackKind.I8)
            Emit($"dn2cpp_{fam}_{fn}_i8({pre}{a.Expr});");
        else if (a.Kind == StackKind.I4)
            Emit($"dn2cpp_{fam}_{fn}_i4({pre}{a.Expr});");
        // An Assembly/Module handle rides as a raw const char* (no object header) —
        // dn2cpp_object_tostring would dispatch garbage. Same static-type routing
        // as the Object::ToString intrinsic: a Module prints its file name, an
        // Assembly (the default) its display FullName, matching real .NET.
        else if (a.Kind == StackKind.Ref && a.CppType == "const char*")
            Emit(a.StaticType is
                    { Kind: TypeKind.Class, Class.FullName: "System.Reflection.Module" }
                    or { Kind: TypeKind.External, ExternalName: "System.Reflection.Module" }
                ? $"dn2cpp_{fam}_{fn}_str({pre}dn2cpp_module_name({a.Expr}));"
                : $"dn2cpp_{fam}_{fn}_str({pre}dn2cpp_assembly_full_name({a.Expr}));");
        else if (a.Kind == StackKind.Ref)
            Emit($"dn2cpp_{fam}_{fn}_str({pre}dn2cpp_object_tostring({Cast(a, "Dn2CppObject*")}));");
        else
            throw new NotSupportedException($"Console.{(newline ? "WriteLine" : "Write")}({p}) is not supported yet");
    }

    /// <summary>Pops the receiver of a fast-path <c>System.IO.TextWriter</c> call and
    /// returns the <c>Dn2CppTextWriter*</c> stderr stream to write to. dn2cpp models exactly
    /// one TextWriter — the Console.Error writer (the managed <c>Dn2CppConsoleWriter</c>
    /// singleton produced by <c>Console.get_Error</c>). The managed receiver object is
    /// discarded here: the fast path writes straight to the stderr stream
    /// (<c>dn2cpp_console_error()</c>). Any other receiver is a loud
    /// <see cref="NotSupportedException"/>, never a silent miscompile — the caller
    /// (Call.cs) only reaches this after matching the singleton's C++ type.
    /// <para>In a CoreLib-less load set there is no managed writer at all and
    /// <c>get_Error</c> pushes the runtime writer itself, so that is the receiver
    /// shape to accept there. The two are never both live: which one <c>get_Error</c>
    /// pushes is decided once per Compilation.</para></summary>
    private string PopTextWriterReceiver()
    {
        var recv = Pop();
        string expected = Comp.ConsoleErrorWriterCppType ?? "Dn2CppTextWriter*";
        if (recv.CppType != expected)
            throw new NotSupportedException(
                "System.IO.TextWriter members are only supported on Console.Error (the stderr writer)");
        return "dn2cpp_console_error()";
    }

    /// <summary>True for the Console composite-format overloads we map: a string
    /// format followed by 1-3 object args or a single object[].</summary>
    private static bool IsConsoleFormatOverload(MethodSignature<TypeDesc> sig) =>
        sig.ParameterTypes is [{ IsString: true }, { Kind: TypeKind.SZArray }]
        or [{ IsString: true }, { IsObject: true }]
        or [{ IsString: true }, { IsObject: true }, { IsObject: true }]
        or [{ IsString: true }, { IsObject: true }, { IsObject: true }, { IsObject: true }];

    /// <summary>True when the type is System.IFormatProvider (which a CultureInfo /
    /// NumberFormatInfo satisfies) — the runtime culture handle.</summary>
    private static bool IsFormatProvider(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.IFormatProvider" };

    /// <summary>ReadOnlySpan&lt;object&gt; — the params-span argument of the net10
    /// composite-format / join / concat overloads (Roslyn prefers them over the
    /// params-array forms; e.g. 4+ loose Format args land here).</summary>
    private bool IsObjectSpan(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } c }
        && Comp.GenericDefFullName(c) == "System.ReadOnlySpan"
        && c.Context.TypeArgs is [{ IsObject: true }];

    /// <summary>ReadOnlySpan&lt;string&gt; — the params-span argument of the net10
    /// string-element Join/Concat/AppendJoin overloads.</summary>
    private bool IsStringSpan(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } c }
        && Comp.GenericDefFullName(c) == "System.ReadOnlySpan"
        && c.Context.TypeArgs is [{ IsString: true }];

    /// <summary>The (string format, params ReadOnlySpan&lt;object&gt;) composite-format
    /// shape shared by string.Format / Console.Write(Line) / AppendFormat.</summary>
    private bool IsSpanFormatOverload(MethodSignature<TypeDesc> sig) =>
        sig.ParameterTypes is [{ IsString: true }, var sp] && IsObjectSpan(sp);

    /// <summary>The (IFormatProvider, string format, params ReadOnlySpan&lt;object&gt;)
    /// culture-aware composite-format shape.</summary>
    private bool IsProviderSpanFormatOverload(MethodSignature<TypeDesc> sig) =>
        sig.ParameterTypes is [var p, { IsString: true }, var sp]
        && IsFormatProvider(p) && IsObjectSpan(sp);

    /// <summary>Pops a span value into a struct temp and returns the temp's name
    /// (its {f__reference, f__length} fields are the data pointer + length).</summary>
    private string PopSpanTemp(TypeDesc spanType)
    {
        var sp = Pop();
        string t = NewTemp(CppTypes.Of(spanType));
        Emit($"{t} = {sp.Expr};");
        return t;
    }

    /// <summary>Pops a (format, ReadOnlySpan&lt;object&gt;) argument list and returns the
    /// runtime string.Format call over the span's data pointer + length.</summary>
    private string BuildStringFormatExprSpan(MethodSignature<TypeDesc> sig)
    {
        string sp = PopSpanTemp(sig.ParameterTypes[^1]);
        string fmt = Cast(Pop(), "Dn2CppString*");
        return $"dn2cpp_string_format_spanobjs({fmt}, (Dn2CppObject* const*){sp}.f__reference, {sp}.f__length)";
    }

    /// <summary>Pops an (IFormatProvider, format, ReadOnlySpan&lt;object&gt;) argument
    /// list and returns the culture-aware runtime string.Format call.</summary>
    private string BuildStringFormatExprSpanC(MethodSignature<TypeDesc> sig)
    {
        string sp = PopSpanTemp(sig.ParameterTypes[^1]);
        string fmt = Cast(Pop(), "Dn2CppString*");
        string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
        return $"dn2cpp_string_format_spanobjs_c({prov}, {fmt}, (Dn2CppObject* const*){sp}.f__reference, {sp}.f__length)";
    }

    /// <summary>ToString(IFormatProvider) — the value's default culture-aware text.</summary>
    private static bool IsProviderOnly(MethodSignature<TypeDesc> sig) =>
        sig.ParameterTypes is [var p] && IsFormatProvider(p);

    /// <summary>ToString(string format, IFormatProvider provider).</summary>
    private static bool IsFormatThenProvider(MethodSignature<TypeDesc> sig) =>
        sig.ParameterTypes is [{ IsString: true }, var p] && IsFormatProvider(p);

    /// <summary>string.Format(IFormatProvider, format, object... | object[]).</summary>
    private static bool IsProviderFormatOverload(MethodSignature<TypeDesc> sig) =>
        sig.ParameterTypes is [_, { IsString: true }, { Kind: TypeKind.SZArray }]
            or [_, { IsString: true }, { IsObject: true }]
            or [_, { IsString: true }, { IsObject: true }, { IsObject: true }]
            or [_, { IsString: true }, { IsObject: true }, { IsObject: true }, { IsObject: true }]
        && IsFormatProvider(sig.ParameterTypes[0]);

    /// <summary>Pops a (format, object...) or (format, object[]) argument list and
    /// returns the runtime string.Format call producing the composed string.
    /// Shared by string.Format and Console.Write/WriteLine.</summary>
    private string BuildStringFormatExpr(MethodSignature<TypeDesc> sig)
    {
        if (sig.ParameterTypes is [_, { Kind: TypeKind.SZArray }])
        {
            var args = Pop();
            var fmt = Pop();
            return $"dn2cpp_string_format_arr({Cast(fmt, "Dn2CppString*")}, {Cast(args, "Dn2CppArrayRef*")})";
        }
        int n = sig.ParameterTypes.Length - 1;
        var objs = new string[n];
        for (int i = n - 1; i >= 0; i--)
            objs[i] = Cast(Pop(), "Dn2CppObject*");
        string fmtExpr = Cast(Pop(), "Dn2CppString*");
        return $"dn2cpp_string_format{n}({fmtExpr}, {string.Join(", ", objs)})";
    }

    /// <summary>Pops an (IFormatProvider, format, object... | object[]) argument list
    /// and returns the culture-aware runtime string.Format call.</summary>
    private string BuildStringFormatExprC(MethodSignature<TypeDesc> sig)
    {
        if (sig.ParameterTypes is [_, _, { Kind: TypeKind.SZArray }])
        {
            var args = Cast(Pop(), "Dn2CppArrayRef*");
            var fmt = Cast(Pop(), "Dn2CppString*");
            var prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
            return $"dn2cpp_string_format_arr_c({prov}, {fmt}, {args})";
        }
        int n = sig.ParameterTypes.Length - 2; // minus provider and format
        var objs = new string[n];
        for (int i = n - 1; i >= 0; i--)
            objs[i] = Cast(Pop(), "Dn2CppObject*");
        string fmtExpr = Cast(Pop(), "Dn2CppString*");
        string provExpr = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
        return $"dn2cpp_string_format{n}_c({provExpr}, {fmtExpr}, {string.Join(", ", objs)})";
    }

    /// <summary>System.Text.CompositeFormat — the net8+ holder of a PRE-PARSED composite
    /// format string. Its whole content is a cache: the original format string plus the
    /// segment list a parse of it produces, so a formatter that re-parses the string
    /// answers identically (see <see cref="PopCompositeFormatString"/>).</summary>
    private static bool IsCompositeFormat(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.Text.CompositeFormat" };

    /// <summary>string.Format(IFormatProvider, CompositeFormat, …) — the whole net8+
    /// overload family: the <c>params object?[]</c> form, the net9+ <c>params
    /// ReadOnlySpan&lt;object?&gt;</c> form, and the three generic
    /// <c>Format&lt;TArg0…TArg2&gt;</c> forms Roslyn binds a call with 1-3 loose
    /// arguments to. Every one of them takes the provider first — CompositeFormat has no
    /// provider-less overload, hence one predicate here rather than the with/without pair
    /// the format-string overloads need.
    ///
    /// <para>The generic forms arrive as a MethodSpecification, i.e. through
    /// <c>TranslateGenericIntrinsic</c> rather than through this switch, and their decoded
    /// signature carries the SUBSTITUTED argument types — so both mouths ask this one
    /// predicate and both hand the same signature to
    /// <see cref="BuildCompositeFormatExprC"/>.</para></summary>
    private bool IsCompositeFormatOverload(MethodSignature<TypeDesc> sig) =>
        sig.ParameterTypes.Length is >= 3 and <= 5
        && IsFormatProvider(sig.ParameterTypes[0])
        && IsCompositeFormat(sig.ParameterTypes[1]);

    /// <summary>Pops a CompositeFormat operand and yields the ORIGINAL format string it
    /// holds, read straight out of the backing field of its public <c>Format</c> property —
    /// the same private-field read <see cref="TryListBacking"/> does for List&lt;T&gt;'s
    /// <c>_items</c>/<c>_size</c>. The field is asked for by BOTH spellings a
    /// string-valued member can have (the auto-property backing field .NET 10 declares, and
    /// a plain <c>_format</c> field should the property ever be hand-written), and a miss
    /// throws rather than degrading: the format string IS the lowering.
    ///
    /// <para>The parsed segments are ignored because a CompositeFormat is a cache of a
    /// parse, not a different format language: it stores the string verbatim beside the
    /// segments derived from it, and the runtime engine (<c>dn2cpp_string_format_impl</c>)
    /// parses the identical <c>{index[,align][:spec]}</c> grammar. What is given up is the
    /// caching — a cost, not a wrong answer.</para>
    ///
    /// <para>DECLARED DIVERGENCE, two edges. A NULL CompositeFormat raises
    /// NullReferenceException where real .NET raises ArgumentNullException
    /// (<c>FieldAccess</c>'s null guard is the receiver check, not an argument check). And
    /// too FEW arguments raise FormatException from the engine's out-of-range hole test
    /// rather than from CompositeFormat's own <c>ValidateNumberOfArgs</c> up front, so the
    /// exception TYPE matches and the message does not.</para></summary>
    private string PopCompositeFormatString(TypeDesc cfType)
    {
        var cls = cfType.Class
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: string.Format's CompositeFormat "
                + "argument has no class shape");
        Comp.EnsureCompleted(cls);
        var fld = cls.Fields.FirstOrDefault(f => !f.IsStatic
                && f.Name is "<Format>k__BackingField" or "_format")
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: System.Text.CompositeFormat carries "
                + "neither <Format>k__BackingField nor _format (layout drift — the original format "
                + "string is what the runtime formatter re-parses)");
        var cf = Pop();
        return $"(Dn2CppString*)({FieldAccess(cls, fld, cf)})";
    }

    /// <summary>Pops one composite-format argument as the uniform <c>Dn2CppObject*</c> the
    /// runtime formatter takes: a reference argument passes through, a value-type one is
    /// boxed through an addressable temp of its exact unboxed layout. Only the GENERIC
    /// <c>Format&lt;TArg0…&gt;</c> forms need it — the array/span forms already carry boxed
    /// elements. Pre-formatting each hole instead is not possible: the <c>:spec</c> a hole
    /// applies is in the format string, i.e. not known until run time.</summary>
    private string BoxCompositeFormatArg(TypeDesc t)
    {
        var v = Pop();
        if (CppTypes.KindOf(t) == StackKind.Ref)
            return $"(Dn2CppObject*)({v.Expr})";
        string ct = CppTypes.Of(t);
        string ti = TypeInfoExpr(t)
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: string.Format(CompositeFormat) "
                + $"argument type {t} has no boxable type-info");
        string tmp = NewTemp(ct);
        Emit($"{tmp} = {Cast(v, ct)};");
        return $"dn2cpp_box({ti}, &{tmp}, sizeof({ct}))";
    }

    /// <summary>Pops an (IFormatProvider, CompositeFormat, args…) argument list and returns
    /// the culture-aware runtime string.Format call over the format string the
    /// CompositeFormat holds. Shape-dispatched over the whole family; an unmodeled shape
    /// throws, because System.String is an intrinsic type whose members never transpile —
    /// there is no real body to fall through to (docs/ARCHITECTURE.md §4-B).</summary>
    private string BuildCompositeFormatExprC(MethodSignature<TypeDesc> sig)
    {
        var pts = sig.ParameterTypes;
        // The two NON-generic forms carry the whole argument list in one operand. Gated on
        // arity 0 so an exotic Format<object[]>(provider, cf, arr) — which C# never binds,
        // the params-array overload being the better match — cannot be mistaken for the
        // params form and spread an argument that was meant as one hole.
        if (sig.GenericParameterCount == 0 && pts.Length == 3)
        {
            if (pts[2].Kind == TypeKind.SZArray)
            {
                string args = Cast(Pop(), "Dn2CppArrayRef*");
                string fmt = PopCompositeFormatString(pts[1]);
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                return $"dn2cpp_string_format_arr_c({prov}, {fmt}, {args})";
            }
            if (IsObjectSpan(pts[2]))
            {
                string sp = PopSpanTemp(pts[2]);
                string fmt = PopCompositeFormatString(pts[1]);
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                return $"dn2cpp_string_format_spanobjs_c({prov}, {fmt}, "
                    + $"(Dn2CppObject* const*){sp}.f__reference, {sp}.f__length)";
            }
        }
        int n = pts.Length - 2; // minus provider and CompositeFormat
        if (n is < 1 or > 3)
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: string.Format(IFormatProvider, "
                + $"CompositeFormat, …) with {n} loose arguments is not a .NET overload shape");
        var objs = new string[n];
        for (int i = n - 1; i >= 0; i--)
            objs[i] = BoxCompositeFormatArg(pts[2 + i]);
        string fmtExpr = PopCompositeFormatString(pts[1]);
        string provExpr = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
        return $"dn2cpp_string_format{n}_c({provExpr}, {fmtExpr}, {string.Join(", ", objs)})";
    }

    /// <summary>Emits a NumberFormatInfo property setter: pops the value + receiver
    /// and emits `helper(receiver, value);` as a statement.</summary>
    private void EmitNfiSet(string helper)
    {
        string val = Cast(Pop(), "Dn2CppString*");
        string recv = Cast(Pop(), "Dn2CppNumberFormatInfo*");
        Emit($"{helper}({recv}, {val});");
    }
}
