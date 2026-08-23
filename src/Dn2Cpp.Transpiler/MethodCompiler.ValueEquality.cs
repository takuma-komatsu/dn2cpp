namespace Dn2Cpp;

/// <summary>Synthesizes the structural equality and hash of a value type that overrides
/// neither — the bodies <c>System.ValueType.Equals(object)</c> and
/// <c>ValueType.GetHashCode()</c> stand for. Their real CoreLib IL bottoms out in QCall
/// externs over the runtime's <c>MethodTable</c> and mixes its ADDRESS into the hash, so
/// there is no body to transcribe.
///
/// <para>It matches .NET bit for bit because .NET takes its <c>memcmp</c> fast path only
/// when the type has no GC pointers, no padding, no float/double and no field whose type
/// overrides <c>Equals</c> — exactly the conditions under which memcmp and a field walk are
/// the same function. The one shape where they genuinely part is an explicit-layout union,
/// whose overlapping fields a walk double-counts; carved out by
/// <c>Compilation.SynthesizableShape</c>.</para>
///
/// <para><c>ValueType.Equals</c> boxes each field and calls the <b>Object-virtual</b>
/// <c>Equals(object)</c> on it, NOT <c>IEquatable&lt;T&gt;.Equals(T)</c> — so a struct field
/// that only implements <c>IEquatable&lt;T&gt;</c> gets its own field walk. Hence a separate
/// builder from the key-path one (<see cref="MethodCompiler.TryEqualityEqualsLValue"/>,
/// which prefers the typed override because <c>EqualityComparer&lt;T&gt;.Default</c> does).
/// </para></summary>
internal sealed partial class MethodCompiler
{
    /// <summary>Roslyn's record-hash multiplier — the fold the C# compiler already emits,
    /// which satisfies the only contract a hash owes ("equal values hash equal").
    /// Spelled UNSIGNED (−1521134295 reinterpreted) and folded in <c>uint32_t</c>: the
    /// multiply overflows by design and signed overflow is UB in C++. <c>System.HashCode</c>
    /// is unusable here — its <c>s_seed</c> is a .cctor fill, and dn2cpp runs .cctors eagerly
    /// in reach order (AGENTS.md, self-host shapes).</summary>
    private const string HashMultiplier = "2773833001u";

    /// <summary>Compiles the minted body — the field walk for the equality or the hash,
    /// told apart by the name the mint gave it (Compilation.MintValueBody).</summary>
    internal string CompileSynthesizedValueBody() =>
        Method.Name == "GetHashCode" ? SynthesizedValueHashBody() : SynthesizedValueEqualsBody();

    /// <summary>The typed <c>Equals(T)</c>: every instance field compared, short-circuiting.
    /// A field-less struct is equal to itself.</summary>
    private string SynthesizedValueEqualsBody()
    {
        var cls = Method.DeclaringClass;
        string ct = cls.CppStructName;
        var recv = new StackEntry("a0", StackKind.Ptr, ct + "*");
        var other = new StackEntry("a1", StackKind.Struct, ct);
        var terms = new List<string>();
        foreach (var f in Compilation.StructuralFields(cls))
            terms.Add(StructuralFieldEqualsExpr(cls, f, recv, other));
        Emit(terms.Count == 0
            ? "return 1;"
            : $"return ({string.Join("\n         && ", terms)}) ? 1 : 0;");
        return RenderWrapperBody("synthesized ValueType field-by-field equality");
    }

    /// <summary>The <c>GetHashCode()</c>: every instance field's hash folded in the order
    /// the equality compares them, so two values the equality calls equal cannot fold to
    /// different results.</summary>
    private string SynthesizedValueHashBody()
    {
        var cls = Method.DeclaringClass;
        var recv = new StackEntry("a0", StackKind.Ptr, cls.CppStructName + "*");
        string h = NewTemp("uint32_t");
        Emit($"{h} = 0;");
        foreach (var f in Compilation.StructuralFields(cls))
            Emit($"{h} = {h} * {HashMultiplier} + (uint32_t)({StructuralFieldHashExpr(cls, f, recv)});");
        Emit($"return (int32_t){h};");
        return RenderWrapperBody("synthesized ValueType field-by-field hash");
    }

    /// <summary>One field's contribution to the equality: the two lvalues that read it out
    /// of the receiver and the operand, handed to <see cref="StructuralEqualsExpr"/>. The
    /// loud failure belongs here — this is the frame that knows which field it was.</summary>
    private string StructuralFieldEqualsExpr(ClassInfo cls, FieldInfo f, StackEntry recv, StackEntry other)
    {
        var t = f.Type;
        // The DECLARED spelling, not the site-resolved one: under a shared struct layout a
        // reference field carries the canonical owner's erased Dn2CppObject*, and Cast is
        // what reconciles the two (MethodCompiler.LayoutFieldType).
        string decl = LayoutFieldType(cls, f);
        var x = new StackEntry(FieldAccess(cls, f, recv), CppTypes.KindOf(t), decl);
        var y = new StackEntry(FieldAccess(cls, f, other), CppTypes.KindOf(t), decl);
        return StructuralEqualsExpr(t, x, y)
            ?? throw new NotSupportedException(
                $"{cls.FullName}.{Method.Name}: field {f.Name} of type {t} has no structural equality");
    }

    /// <summary>The equality of two addressable lvalues of one type, under
    /// <c>ValueType.Equals</c>'s rule — box the value, dispatch the Object virtual — as a
    /// pure expression. The recursion step of the field walk; null when the type has none.
    /// </summary>
    private string? StructuralEqualsExpr(TypeDesc t, StackEntry x, StackEntry y)
    {
        // Nullable<U> is the one type whose box is not a box of ITSELF: the CLR turns it into
        // null or a box of U, so the walk sees two null-or-U boxes. Boxing it as itself (what
        // the struct arm below would do, since Nullable<U> does override Equals(object)) hands
        // Nullable<U>.Equals its own box, which that body type-checks against U and rejects —
        // silently false for every value.
        if (Comp.NullableUnderlying(t) is { } u)
        {
            var (_, hvF, valF) = NullableLayout(t)!.Value;
            string uct = CppTypes.Of(u);   // U is a value type (C# forbids Nullable of a
                                           // reference), so no canonical erasure to reconcile
            var ux = new StackEntry($"({x.Expr}).{valF}", CppTypes.KindOf(u), uct);
            var uy = new StackEntry($"({y.Expr}).{valF}", CppTypes.KindOf(u), uct);
            if (StructuralEqualsExpr(u, ux, uy) is not { } inner)
                return null;
            return $"((({x.Expr}).{hvF} ? 1 : 0) == (({y.Expr}).{hvF} ? 1 : 0)"
                 + $" && (!({x.Expr}).{hvF} || {inner}))";
        }
        // Opaque pointer-backed handles compare by their whole representation; their
        // loaded CoreLib fields do not describe the emitted runtime payload.
        if (IntrinsicPointerValueType(t) is not null)
            return TryEqualityEqualsLValue(t, x, y);
        // A System.Enum-typed field holds a BOXED reference (CppTypes.Of -> Dn2CppObject*),
        // not a value-struct layout, so it must NOT take this arm's `&field` value-type call.
        // Modeled as a reference type, it fails the IsValueType test and falls through to
        // TryEqualityEqualsLValue, which routes it to dn2cpp_object_equals.
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } fc }
            && IntrinsicValueTypeFn(t) is null)
        {
            if (Compilation.EffectiveEquals(fc) is { } feq)
            {
                // The box .NET's field walk makes, made here: the override takes its `other`
                // as an object and isinst-checks it itself.
                TaintIfCanonical(fc, "typeinfo");
                string ct = CppTypes.Of(t);
                string args = $"&{x.Expr}, dn2cpp_box(&{fc.CppTypeInfoName}, &{y.Expr}, sizeof({ct}))";
                return $"({DirectCallSym(feq)}({ArgsWithRgctx(args, feq)}) ? 1 : 0)";
            }
            // No override: the field's own walk, reached at its typed shape — so unlike .NET,
            // which can only re-enter through Equals(object), no box is allocated.
            if (SynthesizedValueEquals(fc) is { } fsyn)
                return $"({DirectCallSym(fsyn)}({ArgsWithRgctx($"&{x.Expr}, {y.Expr}", fsyn)}) ? 1 : 0)";
            return null;
        }
        return TryEqualityEqualsLValue(t, x, y);
    }

    /// <summary>One field's contribution to the hash — the lvalue that reads it, handed to
    /// <see cref="StructuralHashExpr"/>. The twin of <see cref="StructuralFieldEqualsExpr"/>.
    /// </summary>
    private string StructuralFieldHashExpr(ClassInfo cls, FieldInfo f, StackEntry recv)
    {
        var t = f.Type;
        var x = new StackEntry(FieldAccess(cls, f, recv), CppTypes.KindOf(t), LayoutFieldType(cls, f));
        return StructuralHashExpr(t, x)
            ?? throw new NotSupportedException(
                $"{cls.FullName}.{Method.Name}: field {f.Name} of type {t} has no structural hash");
    }

    /// <summary>The hash of one addressable lvalue, under <c>ValueType.GetHashCode</c>'s
    /// rule — the same arms, and the same preference for the Object-virtual override over a
    /// typed one, as the equality: a hash that disagreed with the equality about which
    /// function answers for a field could put two equal values in different buckets.</summary>
    private string? StructuralHashExpr(TypeDesc t, StackEntry x)
    {
        // A Nullable<U> boxes to null-or-a-box-of-U, and Object.GetHashCode of a null
        // reference is 0 — which is also what Nullable<U>.GetHashCode() returns for an empty
        // one. Shaped to match the equality arm, so two Nullables it calls equal hash alike.
        if (Comp.NullableUnderlying(t) is { } u)
        {
            var (_, hvF, valF) = NullableLayout(t)!.Value;
            var ux = new StackEntry($"({x.Expr}).{valF}", CppTypes.KindOf(u), CppTypes.Of(u));
            return StructuralHashExpr(u, ux) is { } inner
                ? $"((({x.Expr}).{hvF}) ? ({inner}) : 0)"
                : null;
        }
        // Keep the hash twin on the same raw-handle representation as equality.
        if (IntrinsicPointerValueType(t) is not null)
            return EqualityHashExpr(t, x);
        // System.Enum-typed field: a boxed reference, not a value struct — see the equality
        // twin. Falls through to EqualityHashExpr, which routes it to
        // dn2cpp_object_gethashcode, matching the equality so equal fields hash alike.
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } fc }
            && IntrinsicValueTypeFn(t) is null)
        {
            if (Compilation.EffectiveGetHashCode(fc) is { } fgh)
                return $"{DirectCallSym(fgh)}({ArgsWithRgctx($"&{x.Expr}", fgh)})";
            if (SynthesizedValueHash(fc) is { } fsyn)
                return $"{DirectCallSym(fsyn)}({ArgsWithRgctx($"&{x.Expr}", fsyn)})";
            return null;
        }
        // Floats hash their BITS through the runtime helper here too (PrimitiveHashExpr) —
        // the same helper the boxed path calls, which is what makes a struct hashed as a key
        // and the same struct hashed through a box land in one bucket.
        return EqualityHashExpr(t, x);
    }
}
