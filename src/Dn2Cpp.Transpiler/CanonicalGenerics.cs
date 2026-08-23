using System.Collections.Generic;
using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>Thrown while trial-compiling a shared-body candidate (a canonical
/// owner method) when the body hits an instantiation-dependent site — one that
/// would bake a placeholder-bearing type's identity (type-info, statics,
/// allocation stamp, array handle, function address, …) into the shared text.
/// The planning pass catches it and marks the method unshareable, so each
/// grouped user compiles its own body instead.</summary>
internal sealed class SharedBodyTaintException : Exception
{
    public string Kind { get; }

    public SharedBodyTaintException(string kind, string message) : base(message)
    {
        Kind = kind;
    }
}

/// <summary>What a runtime-generic-context slot resolves to per real
/// instantiation. Every slot is keyed by the raw IL token of the site that
/// allocated it (unambiguous even when two type parameters canonicalize to the
/// same placeholder); the fill re-resolves the token under each grouped user's
/// real generic context.</summary>
internal enum RgctxSlotKind
{
    /// <summary>The resolved type's own type-info (box/unbox/castclass/isinst).</summary>
    TypeInfo,
    /// <summary>The Task identity produced by an intrinsic call, constructor or
    /// adopted-task field. The raw member token is re-resolved under each real
    /// context; ValueTask and TaskCompletionSource sites project to their backing
    /// Task.</summary>
    TaskTypeInfo,
    /// <summary>The type-info of the resolved closed generic's first type
    /// argument — Nullable&lt;T&gt; boxing (the box carries T's identity) and the
    /// IComparable&lt;T&gt; boxed-primitive cast.</summary>
    TypeArg0TypeInfo,
    /// <summary>The precise per-element array type-info (<c>ti_arr_*</c>) for a
    /// newarr (element token) or typeof(T[]) (SZArray token) site.</summary>
    ArrayTypeInfo,
    /// <summary>The type-info stamped on a reference-type newobj — the token is
    /// the ctor method token; the allocation size is group-uniform
    /// (sizeof of the shared owner layout), so the type-info is the whole slot.</summary>
    ClassAlloc,
    /// <summary>The address of the resolved static field's per-instantiation
    /// storage (the token is the field token).</summary>
    StaticFieldAddr,
    /// <summary>The resolved static field's declaring class's idempotent
    /// <c>__ensure</c> cctor wrapper, as a <c>void(*)()</c>.</summary>
    CctorEnsureFn,
    /// <summary>The type-info of the GenericComparer&lt;T&gt; the intercepted
    /// Comparer&lt;T&gt;.Default synthesizes (the token is the get_Default call).</summary>
    ComparerDefault,
    /// <summary>The <c>EqualityComparer&lt;T&gt;</c> type-info the intercepted
    /// <c>EqualityComparer&lt;T&gt;.Default</c> stamps as its singleton's base, and
    /// the closed <c>IEqualityComparer&lt;T&gt;</c> handle that is the singleton's
    /// cache key and interface row (both tokens are the get_Default call). Two
    /// kinds because one slot carries one identity and the site needs both.</summary>
    EqualityComparerDefault,
    EqualityComparerInterface,
    /// <summary>Another grouped class's rgctx table — passed as the hidden
    /// parameter when a shared body direct-calls a context-needing shared body
    /// of a different canonical class (the token is the call/newobj method
    /// token, re-resolved to the real callee's declaring class).</summary>
    RgctxTable,
    /// <summary>A linked real generic-method instantiation's per-method rgctx
    /// table — passed as the hidden parameter when a shared body direct-calls
    /// a context-needing shared generic-method instantiation other than itself
    /// (the token is the call site's MethodSpec token, re-resolved to the real
    /// instantiation whose table the callee runs under).</summary>
    MethodRgctxTable,
}

/// <summary>One runtime-generic-context slot of a canonical owner class: the
/// resolution kind plus the raw IL token it re-resolves per real instantiation.
/// Tokens are meaningful within the owner class's defining module (every body
/// contributing slots is declared on that class).</summary>
internal readonly record struct RgctxSlot(RgctxSlotKind Kind, int Token);

/// <summary>The ordered rgctx slot registry of one canonical owner class:
/// slots are appended in first-use order during body compilation and deduped by
/// (kind, token), so re-compiling the same bodies reuses indices. The emitter
/// resets registries between the planning and emission passes — the final
/// registry holds exactly the retained shared bodies' slots in deterministic
/// compile order.</summary>
internal sealed class RgctxRegistry
{
    public List<RgctxSlot> Slots { get; } = new();
    private readonly Dictionary<RgctxSlot, int> _index = new();

    public int GetOrAdd(RgctxSlotKind kind, int token)
    {
        var slot = new RgctxSlot(kind, token);
        if (_index.TryGetValue(slot, out int i))
            return i;
        i = Slots.Count;
        Slots.Add(slot);
        _index[slot] = i;
        return i;
    }
}

/// <summary>Maps generic type arguments to their layout-canonical form, grouping
/// instantiations whose emitted C++ layout coincides so their method bodies can be
/// shared (IL2CPP-style canonical shared generics).
///
/// <para>Enum and small-integer arguments collapse to an interned placeholder of
/// their width — the array representation splits by underlying width, so width has
/// to stay part of the group key, and 8-byte scalars keep their own instantiations.
/// Reference-type arguments collapse to the single CnRef placeholder, every managed
/// reference being one pointer. A value type keeps its identity but its own generic
/// arguments canonicalize recursively, so List&lt;KeyValuePair&lt;SomeEnum,string&gt;&gt;
/// groups under List&lt;KeyValuePair&lt;CnInt32,CnRef&gt;&gt;. Anything of unknown
/// value-ness or still open is left untouched: no sharing rather than wrong
/// sharing.</para>
///
/// <para>Placeholders are interned per compilation, so reference equality against
/// the input argument means "unchanged".</para></summary>
internal sealed class CanonicalGenerics
{
    private readonly Compilation _c;

    /// <summary>Reference-argument collapsing (the CnRef group): every managed
    /// reference is one pointer, so all reference-type arguments erase to the single
    /// CnRef placeholder (Dn2CppObject* in the shared body, C-style casts at the
    /// boundaries). A switch, so it can be turned off independently of the enum
    /// canonicalization; the group linkage is computed the same way either way.</summary>
    private readonly bool _canonicalizeReferences = true;

    private readonly Dictionary<PrimitiveTypeCode, TypeDesc> _enumPlaceholders = new();
    private readonly TypeDesc _refPlaceholder = new()
    {
        Kind = TypeKind.Primitive,
        Primitive = PrimitiveTypeCode.Object,
        IsCanonPlaceholder = true,
    };

    public CanonicalGenerics(Compilation c)
    {
        _c = c;
    }

    /// <summary>The canonical form of one generic type argument. Returns the
    /// input instance itself (reference-equal) when nothing canonicalizes.</summary>
    public TypeDesc CanonicalForm(TypeDesc t)
    {
        if (t.IsCanonPlaceholder)
            return t;
        switch (t.Kind)
        {
            case TypeKind.Primitive:
                // String/Object are reference types spelled as primitives.
                if (_canonicalizeReferences
                    && t.Primitive is PrimitiveTypeCode.String or PrimitiveTypeCode.Object)
                    return _refPlaceholder;
                // A genuine sub-8-byte integer scalar joins its width placeholder:
                // it is layout- and codegen-identical to an enum of that underlying,
                // and every whitelisted static fold agrees (the disagreeing members —
                // IsPrimitive, IsEnum, constrained ToString — already taint). This is
                // what lets Dictionary<int,string> share one body set with
                // Dictionary<SomeEnum,object>. Char/Boolean keep their identity (their
                // TypeCode differs from the same-width integers'), as do the 8-byte
                // scalars, mirroring the 8-byte enum exclusion.
                return t.Primitive is PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                    or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
                    or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                    ? EnumPlaceholder(t.Primitive) : t;
            case TypeKind.External:
                // An unresolved TypeRef's value-ness is unknown; only the
                // certainly-reference corlib roots collapse.
                return _canonicalizeReferences
                       && t.ExternalName is "System.String" or "System.Object"
                    ? _refPlaceholder : t;
            case TypeKind.SZArray:
            case TypeKind.MDArray:
                // Arrays are reference types regardless of their element.
                return _canonicalizeReferences ? _refPlaceholder : t;
            case TypeKind.Class:
                break;
            default:
                // Open (generic var/template) or exotic (byref/pointer) forms
                // are never canonicalized.
                return t;
        }

        var cls = t.Class!;
        if (cls.IsEnum)
        {
            // Width-preserving placeholder of the underlying primitive; the
            // 8-byte underlyings keep their own instantiations.
            return cls.EnumUnderlying is PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
                or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                ? EnumPlaceholder(cls.EnumUnderlying) : t;
        }
        // A specialization still awaiting its SHAPE has unknown value-ness; leave it
        // ungrouped rather than guess. Shape, not members: value-ness comes from the
        // base, which the drain decodes for every instantiation. Keying this on the
        // member tier would instead fire for every specialization nothing reaches and
        // silently switch shared generics off for them.
        if (cls.GenericArity > 0 && !cls.ShapeCompleted)
            return t;
        if (!cls.IsValueType && _canonicalizeReferences)
            return _refPlaceholder;
        // A value type keeps its identity, but its own generic arguments canonicalize
        // recursively (via re-instantiation) so instantiations over layout-equal value
        // types still group. With reference collapsing off, a reference-type argument
        // recurses the same way: a shared body's signature can carry reference
        // satellites like Stack<SortedSet<T>.Node>, and only the recursive form makes
        // the grouped user's C++ pointer type name the same struct. Intrinsic generics
        // (Task-family, SIMD vectors) lower to runtime structs and stay put.
        if (cls.GenericArity == 0 || cls.Context.TypeArgs.Length != cls.GenericArity
            || cls.IntrinsicCppName is not null)
            return t;
        var args = cls.Context.TypeArgs;
        var canon = new TypeDesc[args.Length];
        bool changed = false;
        for (int i = 0; i < args.Length; i++)
        {
            canon[i] = CanonicalForm(args[i]);
            changed |= !ReferenceEquals(canon[i], args[i]);
        }
        // Unshared deliberately: this method's contract is the ReferenceEquals test
        // above, and the no-change arms answer it by returning the very object they
        // were handed. A fresh object on the changed arm keeps that test free of any
        // reasoning about interning (see TypeDesc.MakeClassUnshared).
        return changed ? TypeDesc.MakeClassUnshared(_c.Instantiate(cls.Module, cls.Handle, canon)) : t;
    }

    /// <summary>The per-index ANY placeholder ($CnAny0, $CnAny1, …) a
    /// runtime-instantiation template's type arguments are built from. Unlike
    /// CnRef it stands for value and reference arguments alike, which is sound
    /// only for a definition whose bodies never use T in a value position — the
    /// template pass establishes that by trial. Per-INDEX identity (not one
    /// shared instance) is what lets the emitted rgctx slot descriptor resolve a
    /// slot back to "type argument n". Interned, like the other placeholders.</summary>
    public TypeDesc AnyPlaceholder(int index)
    {
        while (_anyPlaceholders.Count <= index)
            _anyPlaceholders.Add(new TypeDesc
            {
                Kind = TypeKind.Primitive,
                Primitive = PrimitiveTypeCode.Object,
                IsCanonPlaceholder = true,
                CanonAnyIndex = _anyPlaceholders.Count,
            });
        return _anyPlaceholders[index];
    }

    private readonly List<TypeDesc> _anyPlaceholders = new();

    private TypeDesc EnumPlaceholder(PrimitiveTypeCode underlying)
    {
        if (!_enumPlaceholders.TryGetValue(underlying, out var p))
        {
            p = new TypeDesc { Kind = TypeKind.Primitive, Primitive = underlying, IsCanonPlaceholder = true };
            _enumPlaceholders.Add(underlying, p);
        }
        return p;
    }
}
