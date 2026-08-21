using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dn2Cpp;

internal enum TypeKind
{
    Primitive,
    Class,      // TypeDef in the input module (or a closed generic specialization)
    External,   // TypeRef into another assembly (System.String, System.Object, ...)
    ExternalGeneric, // a closed generic instantiation of a base-image type, resolved
                     // to its mangled registry name (hot-update patch converter only —
                     // the normal pipeline models a loaded generic as a Class)
    SZArray,
    ByRef,      // managed reference (ref/out/in parameter, ref local)
    GenericVar, // !n (type param) or !!n (method param) placeholder
    Template,   // open generic TypeDef awaiting instantiation
    MDArray,
    Pointer,    // unmanaged pointer (T*) / function pointer — opaque, only
                // emittable when actually reached (see BCL ingestion)
}

internal sealed class TypeDesc
{
    public TypeKind Kind;
    public PrimitiveTypeCode Primitive;
    public ClassInfo? Class;
    public string? ExternalName;   // namespace-qualified, e.g. "System.String"
    public TypeDesc? Element;      // SZArray element
    public int GenVarIndex;        // GenericVar: parameter index
    public bool GenVarIsMethod;    // GenericVar: !!n (method) vs !n (type)
    public TypeDefinitionHandle TemplateHandle; // Template: the open generic def
    public Module? TemplateModule;  // Template: the module owning the def
    public int Rank;               // MDArray: rank
    /// <summary>An interned canonical-generics placeholder standing in for a whole
    /// layout group of type arguments: the reference stand-in (Primitive Object,
    /// printed CnRef — every managed reference is one pointer) or a
    /// width-preserving enum-underlying primitive (printed Cn&lt;Primitive&gt;).
    /// Behaves as its primitive everywhere layout/codegen looks, but mangles and
    /// prints distinctly so canonical instantiation keys never collide with
    /// genuine instantiations over the same primitive.</summary>
    public bool IsCanonPlaceholder;
    /// <summary>For a canonical placeholder: the type-parameter index it stands for
    /// in a runtime-instantiation TEMPLATE (printed CnAny0, CnAny1, …), or -1 for
    /// the ordinary width/reference placeholders. Per-index identity is what lets
    /// the emitted rgctx slot descriptor say "slot i = type argument n"; the
    /// runtime MakeGenericType fill then stamps the real argument's handle.</summary>
    public int CanonAnyIndex = -1;
    public TypeDesc[]? GenericArgs; // ExternalGeneric: the closed type arguments
                                    // (kept so a member reference on the instance
                                    // can substitute the declaring type's !n params)

    /// <summary>A metadata function-pointer type (FNPTR — <c>delegate*&lt;...&gt;</c>, any
    /// calling convention). Kind stays Pointer over Void: a function pointer is one
    /// unmanaged pointer everywhere layout, codegen, mangling and the hot-update sigShape
    /// look, and the last two are baked into emitted builds, so this shape must not print
    /// or mangle apart from <c>void*</c>. The flag carries the one distinction real .NET
    /// draws: <c>[MarshalAs(FunctionPtr)]</c> is valid on it alone — refused on
    /// <c>void*</c>, as every width descriptor is on both (measured). Read by
    /// <c>Compilation.MarshalDescribedExtent</c>, <c>CppTypes.MarshalAsNativeType</c> and
    /// <c>CppTypes.StructFieldDescriptorSupported</c>.</summary>
    public bool IsFunctionPointer;

    /// <summary>Lazily cached <c>Compilation.MangleArg</c> fragment — write-once and
    /// content-derived, so the never-mutated-after-construction rule above stays honest. The
    /// Class arm is NOT cached here (it reads the live <c>ClassInfo.CppName</c>, which the
    /// CppNamePrefix setter invalidates); the cached arms (SZArray-of-class among them) bake
    /// a CppName-derived string, which is sound only because every CppNamePrefix write
    /// happens in Pass 1 and the first mangle is asked no earlier than Pass 2.</summary>
    internal string? MangleCache;

    /// <summary>The interned primitive descriptors, indexed by
    /// <see cref="PrimitiveTypeCode"/>. Signature decoding runs over every field and every
    /// method of every loaded assembly — reachable or not — and most signature elements are
    /// primitives, so a fresh descriptor per occurrence was the largest single term in the
    /// model's heap. (The array is longer than the enum's member count — see
    /// <see cref="MakePrimitives"/> — because the codes are not densely numbered.)
    /// <para>An ARRAY, not a Dictionary: a hash-keyed static populated before
    /// System.HashCode's seed cctor buckets its entries against seed 0 and then misses every
    /// later lookup (documented at <c>CppEmitter.EmitCctorEnsures</c>). Indexing by the enum
    /// sidesteps hashing entirely.</para>
    /// <para>Sharing is sound because <see cref="TypeDesc"/> is never mutated after
    /// construction, and it must stay that way or interning silently aliases unrelated
    /// signatures. The one place reference identity carries meaning is
    /// <c>CanonicalGenerics.CanonicalForm</c>'s "did canonicalizing change this argument?"
    /// test, and interning preserves it: the no-change arms return the very object passed in,
    /// and the changed arms return a canonical placeholder, which is constructed directly
    /// (never through this factory) and interned separately.</para></summary>
    private static readonly TypeDesc[] s_primitives = MakePrimitives();

    private static TypeDesc[] MakePrimitives()
    {
        // PrimitiveTypeCode.Object == 28 is the largest member.
        var table = new TypeDesc[(int)PrimitiveTypeCode.Object + 1];
        for (int i = 0; i < table.Length; i++)
            table[i] = new TypeDesc { Kind = TypeKind.Primitive, Primitive = (PrimitiveTypeCode)i };
        return table;
    }

    /// <summary>Counts a freshly-built descriptor for the model census. Every
    /// factory that allocates routes through here, so an un-interned kind reports
    /// its true churn (the interned primitive table does not — it hands back a
    /// shared object and never lands here).</summary>
    private static TypeDesc Born(TypeDesc t)
    {
        ModelCensus.TypeDescBorn(t.Kind);
        return t;
    }

    public static TypeDesc MakePrimitive(PrimitiveTypeCode code) =>
        (uint)code < (uint)s_primitives.Length
            ? s_primitives[(int)code]
            : Born(new TypeDesc { Kind = TypeKind.Primitive, Primitive = code });

    public static TypeDesc MakeClass(ClassInfo cls) =>
        cls.Desc ??= Born(new() { Kind = TypeKind.Class, Class = cls });

    /// <summary>An un-interned class descriptor, for the one caller that needs a
    /// fresh object every time: <c>CanonicalGenerics.CanonicalForm</c> decides "did
    /// canonicalizing change this argument?" with <c>ReferenceEquals</c> against the
    /// object it was handed, and its no-change arms return that very object. Handing
    /// its changed arm an interned descriptor would make the test depend on whether
    /// <c>Instantiate</c> could return the argument's own ClassInfo — it cannot (a
    /// changed argument mangles to a different key), but the test should not have to
    /// know that. A fresh object keeps the comparison structurally identical to the
    /// un-interned model, and costs one descriptor per canonical owner.</summary>
    public static TypeDesc MakeClassUnshared(ClassInfo cls) =>
        Born(new() { Kind = TypeKind.Class, Class = cls });

    public static TypeDesc MakeExternal(string fullName) =>
        Born(new() { Kind = TypeKind.External, ExternalName = fullName });

    /// <summary>A closed generic instantiation of a base-image type, already
    /// resolved to its mangled registry name (e.g.
    /// <c>System.Collections.Generic.List_Int32</c>). The hot-update patch
    /// converter produces these — it never loads the base assembly, so a
    /// base-image generic type is bound by its name against the runtime type
    /// registry exactly like a non-generic external reference type; the closed
    /// <paramref name="genericArgs"/> ride along so a member reference on the
    /// instance can substitute the declaring type's <c>!n</c> parameters.</summary>
    public static TypeDesc MakeExternalGeneric(string mangledName, TypeDesc[] genericArgs) =>
        Born(new() { Kind = TypeKind.ExternalGeneric, ExternalName = mangledName, GenericArgs = genericArgs });

    public static TypeDesc MakeSZArray(TypeDesc element) =>
        Born(new() { Kind = TypeKind.SZArray, Element = element });

    public static TypeDesc MakeMDArray(TypeDesc element, int rank) =>
        Born(new() { Kind = TypeKind.MDArray, Element = element, Rank = rank });

    public static TypeDesc MakeByRef(TypeDesc element) =>
        Born(new() { Kind = TypeKind.ByRef, Element = element });

    public static TypeDesc MakePointer(TypeDesc element) =>
        Born(new() { Kind = TypeKind.Pointer, Element = element });

    /// <summary>The interned function-pointer descriptor — ONE instance, because the
    /// decoded signature is deliberately dropped (see
    /// <c>SignatureProvider.GetFunctionPointerType</c>) and function pointers pervade real
    /// CoreLib signatures. Declared after <see cref="s_primitives"/>, whose Void entry it
    /// reads at static init.</summary>
    private static readonly TypeDesc s_functionPointer = new()
        { Kind = TypeKind.Pointer, IsFunctionPointer = true,
          Element = MakePrimitive(PrimitiveTypeCode.Void) };

    public static TypeDesc MakeFunctionPointer() => s_functionPointer;

    /// <summary>The interned generic-parameter placeholders, two rows (type <c>!n</c>
    /// / method <c>!!n</c>) indexed by parameter position. Real arities sit far under
    /// the bound; past it the factory falls back to a fresh object. ARRAYS, for the
    /// same reason <see cref="s_primitives"/> is one — a hash-keyed static populated
    /// at static-init buckets its entries against System.HashCode's zero seed and
    /// then misses every later lookup.</summary>
    private const int InternedGenericVars = 32;
    private static readonly TypeDesc[] s_genVarType = MakeGenVars(isMethod: false);
    private static readonly TypeDesc[] s_genVarMethod = MakeGenVars(isMethod: true);

    private static TypeDesc[] MakeGenVars(bool isMethod)
    {
        var table = new TypeDesc[InternedGenericVars];
        for (int i = 0; i < table.Length; i++)
            table[i] = new TypeDesc
                { Kind = TypeKind.GenericVar, GenVarIndex = i, GenVarIsMethod = isMethod };
        return table;
    }

    public static TypeDesc MakeGenericVar(int index, bool isMethod) =>
        (uint)index < InternedGenericVars
            ? (isMethod ? s_genVarMethod : s_genVarType)[index]
            : Born(new() { Kind = TypeKind.GenericVar, GenVarIndex = index, GenVarIsMethod = isMethod });

    /// <summary>The open generic definition a TypeSpec is about to instantiate. Interned per
    /// module: the decoder builds one on the way to every <c>GetGenericInstantiation</c>,
    /// hands it straight to <c>Instantiate</c>, and drops it, so an un-interned model churns
    /// hundreds of thousands of them and holds none. The cache is module-instance state (like
    /// <c>Module.ClassMap</c>), never a hash-keyed static, so the seed-cctor trap above
    /// cannot apply.</summary>
    public static TypeDesc MakeTemplate(Module module, TypeDefinitionHandle handle)
    {
        var cache = module.TemplateDescs ??= new();
        if (!cache.TryGetValue(handle, out var t))
            cache[handle] = t = Born(new TypeDesc
                { Kind = TypeKind.Template, TemplateModule = module, TemplateHandle = handle });
        return t;
    }

    public bool IsVoid => Kind == TypeKind.Primitive && Primitive == PrimitiveTypeCode.Void;
    public bool IsString => (Kind == TypeKind.Primitive && Primitive == PrimitiveTypeCode.String)
                            || (Kind == TypeKind.External && ExternalName == "System.String");
    public bool IsObject => (Kind == TypeKind.Primitive && Primitive == PrimitiveTypeCode.Object)
                            || (Kind == TypeKind.External && ExternalName == "System.Object");

    /// <summary>Replaces generic parameters with concrete arguments.</summary>
    public TypeDesc Substitute(GenericContext ctx) => Kind switch
    {
        TypeKind.GenericVar => GenVarIsMethod ? ctx.MethodArgs[GenVarIndex] : ctx.TypeArgs[GenVarIndex],
        TypeKind.SZArray => MakeSZArray(Element!.Substitute(ctx)),
        TypeKind.MDArray => MakeMDArray(Element!.Substitute(ctx), Rank),
        TypeKind.ByRef => MakeByRef(Element!.Substitute(ctx)),
        // A function pointer's element is the interned Void — nothing to substitute, and
        // rebuilding through MakePointer would silently drop the flag.
        TypeKind.Pointer when IsFunctionPointer => this,
        TypeKind.Pointer => MakePointer(Element!.Substitute(ctx)),
        _ => this,
    };

    /// <summary>Whether a value of this type is, or transitively contains, a GC
    /// reference — the predicate behind RuntimeHelpers.IsReferenceOrContainsReferences
    /// and behind routing thread-static storage through GC-visible blocks.</summary>
    public bool ContainsGcReferences() => Kind switch
    {
        TypeKind.Primitive => Primitive is PrimitiveTypeCode.String or PrimitiveTypeCode.Object,
        TypeKind.SZArray or TypeKind.MDArray => true,
        TypeKind.ByRef => true,   // a by-ref field is a managed reference
        TypeKind.Pointer => false, // unmanaged pointer
        // Derived from the same cascade that renders the C++ storage type: a name
        // CppTypes maps to a GC-managed pointer (Exception, Type, the reflection
        // infos, …) contains a reference, so every write-barrier gate keyed on this
        // predicate agrees with the emitted field/element type. A name with no
        // mapping cannot be stored anywhere and stays "no references".
        TypeKind.External => CppTypes.IsGcRefCppType(CppTypes.ExternalCppName(ExternalName)),
        TypeKind.ExternalGeneric => true, // a base-image generic reference type
        TypeKind.Class when Class!.IsEnum => false,
        TypeKind.Class when Class!.IsValueType =>
            Class!.Fields.Any(f => !f.IsStatic && !f.IsLiteral && f.Type.ContainsGcReferences()),
        TypeKind.Class => true, // reference type
        _ => true,              // unmodeled (e.g. open generic var): assume references
    };

    public override string ToString() => Kind switch
    {
        // Placeholders print distinctly so signature keys (virtual-override
        // matching) never merge a placeholder with the genuine primitive.
        TypeKind.Primitive when IsCanonPlaceholder =>
            CanonAnyIndex >= 0 ? "CnAny" + CanonAnyIndex : IsObject ? "CnRef" : "Cn" + Primitive,
        TypeKind.Primitive => Primitive.ToString(),
        TypeKind.Class => Class!.FullName,
        TypeKind.External or TypeKind.ExternalGeneric => ExternalName!,
        TypeKind.SZArray => Element + "[]",
        TypeKind.MDArray => Element + "[" + new string(',', Rank - 1) + "]",
        TypeKind.ByRef => Element + "&",
        TypeKind.Pointer => Element + "*",
        TypeKind.GenericVar => (GenVarIsMethod ? "!!" : "!") + GenVarIndex,
        TypeKind.Template => "template",
        _ => "?",
    };
}

/// <summary>Concrete type/method arguments active while decoding a
/// specialization's signatures and IL.</summary>
internal sealed class GenericContext
{
    public static readonly GenericContext Empty = new(Array.Empty<TypeDesc>(), Array.Empty<TypeDesc>());

    public TypeDesc[] TypeArgs { get; }
    public TypeDesc[] MethodArgs { get; }

    public GenericContext(TypeDesc[] typeArgs, TypeDesc[] methodArgs)
    {
        TypeArgs = typeArgs;
        MethodArgs = methodArgs;
    }
}

internal sealed class FieldInfo
{
    public required ClassInfo DeclaringClass;
    public required string Name;

    /// <summary>The field's type — decoded on the first read, not when the field is made. The
    /// field ROWS are part of what a type IS and are populated as soon as the type is named;
    /// what those rows are TYPED at is a separate question, asked only by whoever lays the
    /// type out, reflects over it, or touches it with a body.
    ///
    /// <para>Everything the decode needs hangs off <see cref="DeclaringClass"/>: its
    /// <c>Module</c> names the reader and, through <c>Module.Owner</c>, the compilation;
    /// <see cref="Handle"/> names the row; its <c>Context</c> names the generic
    /// arguments.</para>
    ///
    /// <para>The "not yet" test is the null descriptor rather than a flag beside it, for the
    /// reason <see cref="MethodInfo.Signature"/> gives: a flag can fall out of step with the
    /// value, and <c>SignatureProvider</c> has no hook that can return a null TypeDesc, so
    /// this cannot.</para></summary>
    public TypeDesc Type
    {
        get
        {
            if (_type is null)
                DeclaringClass.Module.Owner.DecodeFieldType(this);
            return _type!;
        }
        set => _type = value;
    }

    private TypeDesc? _type;

    /// <summary>True when reading <see cref="Type"/> decodes nothing — the peek an instrument
    /// needs, for the reason <see cref="MethodInfo.SignatureReady"/> exists: a decode appends
    /// to <c>Compilation.Classes</c>, and a census walks that list. Emission never wants this;
    /// a reader that needs the type reads <see cref="Type"/>.</summary>
    public bool TypeReady => _type is not null;

    /// <summary>Decodes the type now, for callers that need the decode to land at a moment
    /// they choose rather than wherever the first read falls: at the one door into the emit
    /// set, and ahead of a walk that would otherwise read and grow the model at once.</summary>
    public FieldInfo EnsureType()
    {
        _ = Type;
        return this;
    }

    // The field's metadata row, for reading per-field attributes (e.g. its
    // marshalling descriptor) that aren't captured at construction.
    public FieldDefinitionHandle Handle;
    public bool IsStatic;
    public bool IsLiteral;
    // The SizeConst (element count) of a
    // [MarshalAs(UnmanagedType.ByValArray, SizeConst = N)] descriptor — the field is
    // an inline fixed-length array laid out contiguously inside its declaring struct.
    // -1 when the field has no ByValArray descriptor (a normal field).
    public int ByValArraySize = -1;

    /// <summary>The <c>UnmanagedType</c> of the field's <c>[MarshalAs]</c> descriptor, or
    /// <c>default</c> (0) when the field carries none — <c>UnmanagedType</c> has no member
    /// valued 0, so the zero IS the absence and needs no companion flag. Decoded by
    /// <c>Compilation.ReadFieldMarshal</c> from the metadata blob, in one pass with
    /// <see cref="ByValArraySize"/>, <see cref="MarshalSizeConst"/> and
    /// <see cref="MarshalArraySubType"/>.
    ///
    /// <para>This is the descriptor as WRITTEN, not a verdict on it: whether the shape is one
    /// dn2cpp models is <c>Compilation.MarshalLayout.cs</c>'s question. Keep the two apart — a
    /// predicate that can only ask whether a descriptor EXISTS takes every described struct
    /// out of a measurable size.</para></summary>
    public System.Runtime.InteropServices.UnmanagedType MarshalAs;

    /// <summary>The descriptor's <c>SizeConst</c> — the inline element count of a
    /// <c>ByValArray</c> or the inline character count of a <c>ByValTStr</c> — or -1 when the
    /// descriptor carries none. Distinct from <see cref="ByValArraySize"/>, which is the
    /// ByValArray case alone, as the P/Invoke layout emitter reads it.</summary>
    public int MarshalSizeConst = -1;

    /// <summary>A <c>ByValArray</c> descriptor's optional <c>ArraySubType</c> (the element's
    /// own <c>UnmanagedType</c>), or <c>default</c> (0) when unspecified. On CoreCLR it moves
    /// the element width only for <c>bool[]</c> and <c>char[]</c> and is ignored for every
    /// other element type — see <c>Compilation.MarshalElementExtent</c>.</summary>
    public System.Runtime.InteropServices.UnmanagedType MarshalArraySubType;
    // [ThreadStatic]: the static field gets one instance per thread (emitted as a
    // C++ thread_local). Only meaningful with IsStatic.
    public bool IsThreadStatic;
    // The field's explicit byte offset ([FieldOffset(N)] under
    // LayoutKind.Explicit, from the metadata FieldLayout row), or -1 when the
    // field carries no explicit offset.
    public int ExplicitOffset = -1;
    // Accessibility for reflection metadata, from the field's
    // FieldAttributes. Defaults to 0 (neither public nor private — e.g. internal).
    public System.Reflection.FieldAttributes Attributes;

    public bool IsPublic => (Attributes & System.Reflection.FieldAttributes.FieldAccessMask) == System.Reflection.FieldAttributes.Public;
    public bool IsPrivate => (Attributes & System.Reflection.FieldAttributes.FieldAccessMask) == System.Reflection.FieldAttributes.Private;

    // Cached: Name/DeclaringClass are construction-only, and the declaring
    // class's CppNamePrefix is finalized before any FieldInfo exists.
    private string? _cppName;
    public string CppName => _cppName ??= "f_" + CppNaming.Sanitize(Name);
    private string? _cppStaticName;
    public string CppStaticName => _cppStaticName ??= "sf_" + DeclaringClass.CppName + "_" + CppNaming.Sanitize(Name);

    /// <summary>A [ThreadStatic] field whose type is (or contains) a GC reference.
    /// Raw C++ thread_local (TLV) storage is not scanned by the collector on every
    /// platform (Darwin TLV blocks are malloc-backed), so such a field would not be
    /// a GC root — it is rerouted into a per-thread GC-visible block instead
    /// (see CppEmitter.EmitStaticFields).</summary>
    public bool IsGcRootedThreadStatic => IsThreadStatic && Type.ContainsGcReferences();

    /// <summary>The C++ lvalue expression that reads/writes this static field:
    /// the plain storage symbol, or the field's slot inside the per-thread
    /// GC-visible block for a GC-reference-carrying [ThreadStatic].</summary>
    public string CppStaticAccess => IsGcRootedThreadStatic
        ? $"dn2cpp_threadstatics()->{CppStaticName}"
        : CppStaticName;
}

internal sealed class MethodInfo
{
    public required ClassInfo DeclaringClass;
    public required string Name;
    public required MethodDefinitionHandle Handle;
    public Module Module = null!; // owning assembly (for token resolution)

    /// <summary>The decoded parameter and return types — decoded on the first read, not at
    /// construction. Decoding a signature is the most expensive thing the model does, and it
    /// is also what mints the closed generics a signature names; most methods of most loaded
    /// assemblies are never asked for theirs.
    ///
    /// <para>Everything the decode needs is already here: <see cref="Module"/> names the
    /// reader and, through <see cref="Module.Owner"/>, the compilation; <see cref="Handle"/>
    /// names the row; <see cref="Context"/> names the generic arguments. A construction site
    /// therefore only has to leave Context right.</para>
    ///
    /// <para>The "not yet" test is the default signature's null ReturnType rather than a flag
    /// beside it. A flag can fall out of step with the value; this cannot, because
    /// <see cref="SignatureProvider"/> has no hook that can return a null TypeDesc. It also
    /// fails loudly if anyone reads <c>_sig</c> behind the property's back — a default
    /// ImmutableArray throws on .Length instead of reporting a parameterless
    /// method.</para></summary>
    public MethodSignature<TypeDesc> Signature
    {
        get
        {
            if (_sig.ReturnType is null)
                Module.Owner.DecodeSignature(this);
            return _sig;
        }
        set => _sig = value;
    }

    private MethodSignature<TypeDesc> _sig;

    /// <summary>True when reading <see cref="Signature"/> decodes nothing — the peek an
    /// instrument needs. A census that decoded the signatures it counts would be building a
    /// model, not measuring one; worse, a decode appends to <c>Compilation.Classes</c>, which
    /// is the list such a census walks. Emission never wants this; a reader that needs the
    /// signature reads <see cref="Signature"/>.</summary>
    public bool SignatureReady => _sig.ReturnType is not null;

    /// <summary>Decodes the signature now, for callers that need the decode to happen at a
    /// moment they choose rather than wherever the first read lands: at the reachability choke
    /// point, which puts it inside the drain that shapes what it names, and ahead of the arms
    /// that catch a decode failure and degrade — they cannot catch one that fires after they
    /// return.</summary>
    public MethodInfo EnsureSignature()
    {
        _ = Signature;
        return this;
    }

    public MethodAttributes Attributes;

    /// <summary>The MethodDef row's ImplFlags. Honored: <c>Synchronized</c>
    /// (RAII monitor guard around the body), <c>NoInlining</c>
    /// (<c>[[gnu::noinline]]</c>), <c>AggressiveInlining</c> (the body is
    /// emitted <c>inline</c> into the shared header so cross-TU call sites can
    /// inline it). Ignored: <c>InternalCall</c> (already modeled as
    /// <see cref="Rva"/> == 0 → intrinsic mapping or the unmapped-extern error),
    /// <c>ForwardRef</c>, <c>PreserveSig</c>, <c>NoOptimization</c>,
    /// <c>AggressiveOptimization</c>.</summary>
    public MethodImplAttributes ImplAttributes;
    public int Rva;
    public int VtableSlot = -1;

    /// <summary>Memoized <see cref="BranchLiveness"/> result for this method's body, owned
    /// exclusively by <see cref="BranchLiveness.ComputeCached"/>, whose doc carries the
    /// soundness invariant. Null means "not yet computed" and a private sentinel of the
    /// owner's means "computed, pass not applicable", so the field must never be read or
    /// written directly. One reference wide on purpose: most MethodInfos are never compiled,
    /// and most computed bodies answer "not applicable".</summary>
    public BranchLiveness? LivenessCache;

    /// <summary>A body the transpiler MINTS rather than transcribes: the structural
    /// equality/hash of a value type that overrides neither. Real .NET's
    /// <c>ValueType.Equals</c>/<c>GetHashCode</c> bottom out in QCall externs over the
    /// runtime's MethodTable, so there is no IL to transpile and synthesis is the only
    /// road — the same one the C# compiler takes for a record.
    ///
    /// <para>It carries a decoded <see cref="Signature"/> and no metadata row:
    /// <see cref="Rva"/> stays 0 and <see cref="Handle"/> is nil, so nothing ever asks the
    /// PE for a body, and it is deliberately NOT in its declaring class's
    /// <see cref="ClassInfo.Methods"/> — which is what keeps it out of the vtable, the
    /// reflection member table and the ABI contract. <c>Compilation.Reach</c> admits it to
    /// the reachable set without a scan (a scan reads IL); its call edges are reached
    /// structurally by whoever demanded it.</para></summary>
    public bool IsSynthetic;

    /// <summary>P/Invoke (`[DllImport]`) descriptor for a <c>pinvokeimpl</c> method,
    /// or null for a normal method. A P/Invoke method has no IL body (<see cref="Rva"/>
    /// == 0); instead of throwing as an unmapped extern, the call site lowers to a
    /// direct native call and the emitter declares the entry point <c>extern "C"</c>.
    /// Captured at construction from <c>MethodDefinition.GetImport()</c>.</summary>
    public PInvokeInfo? PInvoke;

    /// <summary>True for a method carrying <c>[UnmanagedCallersOnly]</c>: native code
    /// calls it directly (its address is taken via <c>ldftn</c> or exported by symbol,
    /// possibly with no managed call edge at all), so it is a reachability root and its
    /// body opens with the native-callback prologue (foreign-thread GC registration).</summary>
    public bool IsUnmanagedCallersOnly;
    /// <summary>The <c>[UnmanagedCallersOnly(EntryPoint = ...)]</c> symbol name the
    /// method is exported under (an <c>extern "C"</c> wrapper with default visibility),
    /// or null when the attribute carries no EntryPoint (address-only use).</summary>
    public string? UnmanagedEntryPoint;

    /// <summary>True for a method carrying <c>[Dn2Cpp.Runtime.HotPathAttribute]</c>
    /// (matched by full name only, like [NativeImplementation] — an assembly can
    /// declare its own internal copy). A marked body is routed into the dedicated
    /// <c>generated_hot.cpp</c> TU, which the build compiles with stronger
    /// per-file optimization flags (runtime/CMakeLists.txt); it never donates
    /// itself as a shared canonical body (every instantiation compiles its own
    /// monomorphic copy), and it is excluded from inline header promotion (a
    /// promoted body would compile under each including TU's plain flags instead
    /// of the hot TU's). Read lazily off the method's own metadata row, so every
    /// mint site — Pass 2, generic-method instantiation, specialization member
    /// completion — answers identically without copying a flag around; a
    /// synthetic body (nil handle) is never marked.</summary>
    public bool IsHotPath => (HotPathBits & HotPathPresent) != 0;

    /// <summary>True when the [HotPath] attribute sets
    /// <c>SkipBoundsChecks = true</c>: the body's array element accesses compile
    /// to raw indexing (no <c>dn2cpp_bounds_check</c> / checked-wrapper calls; a
    /// null array joins the field-access posture — the hardware fault), and a
    /// Span/ReadOnlySpan indexer read at the body's call sites lowers to raw
    /// <c>f__reference + index</c> arithmetic. An out-of-range index is UB by the
    /// knob's contract. Implies <see cref="IsHotPath"/> (it rides the same
    /// attribute), so a marked body is already hot-TU-routed and never donates a
    /// shared canonical body — the raw accesses never serve a placeholder.</summary>
    public bool SkipBoundsChecks => (HotPathBits & HotPathSkipBounds) != 0;

    /// <summary>True when the [HotPath] attribute sets <c>NoAlloc = true</c>: the
    /// transpiler verifies at emit time that this method's direct-call closure
    /// allocates nothing and dispatches nothing dynamically, failing the transpile
    /// otherwise (see <c>CppEmitter.AssertNoAllocClosures</c>). Implies
    /// <see cref="IsHotPath"/> (it rides the same attribute), so a verified body is
    /// already hot-TU-routed and compiled per instantiation — its own C++ symbol is
    /// the closure root, never a shared canonical body's. Decoding the bit registers
    /// the method as a verification root via <c>Compilation.NoteNoAllocMethod</c>.</summary>
    public bool NoAlloc => (HotPathBits & HotPathNoAlloc) != 0;

    /// <summary>True when the [HotPath] attribute sets <c>FastMath = true</c>: the
    /// body is routed into a second dedicated TU (<c>generated_hot_fast.cpp</c>)
    /// that the build compiles with relaxed floating-point semantics
    /// (<c>-ffast-math</c> / <c>/fp:fast</c>) on top of the hot TU's flags, so
    /// reassociation and contraction are permitted and results may differ from
    /// IEEE-exact .NET. Implies <see cref="IsHotPath"/> (it rides the same
    /// attribute) and takes routing precedence over it — the two hot TUs
    /// partition the marked set, so no body is defined twice.</summary>
    public bool FastMath => (HotPathBits & HotPathFastMath) != 0;

    /// <summary>True when the [HotPath] attribute sets <c>NoAlias = true</c>: the
    /// body's array, byref and pointer parameters are emitted <c>__restrict</c>,
    /// and a <c>Span&lt;T&gt;</c>/<c>ReadOnlySpan&lt;T&gt;</c> parameter's element
    /// pointer is hoisted into a <c>__restrict</c> prologue local that the
    /// <see cref="SkipBoundsChecks"/> indexer route addresses. Overlapping
    /// arguments are UB by the knob's contract — nothing is checked. Implies
    /// <see cref="IsHotPath"/> (it rides the same attribute).</summary>
    public bool NoAlias => (HotPathBits & HotPathNoAlias) != 0;

    private const byte HotPathPresent = 1;
    private const byte HotPathSkipBounds = 2;
    private const byte HotPathNoAlloc = 4;
    private const byte HotPathFastMath = 8;
    private const byte HotPathNoAlias = 16;
    private byte HotPathBits => _hotPathBits ??= ComputeHotPathBits();
    private byte? _hotPathBits;

    private byte ComputeHotPathBits()
    {
        if (Handle.IsNil || Module is null)
            return 0;
        var reader = Module.Reader;
        foreach (var cah in reader.GetMethodDefinition(Handle).GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(cah);
            if (Compilation.AttributeTypeName(reader, ca) != "Dn2Cpp.Runtime.HotPathAttribute")
                continue;
            byte bits = HotPathPresent;
            try
            {
                foreach (var na in ca.DecodeValue(Module.Owner.AttrProvider).NamedArguments)
                {
                    if (na is { Name: "SkipBoundsChecks", Value: true })
                        bits |= HotPathSkipBounds;
                    else if (na is { Name: "NoAlloc", Value: true })
                        bits |= HotPathNoAlloc;
                    else if (na is { Name: "FastMath", Value: true })
                        bits |= HotPathFastMath;
                    else if (na is { Name: "NoAlias", Value: true })
                        bits |= HotPathNoAlias;
                }
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                // An undecodable blob keeps the attribute's presence (hot-TU
                // routing) and drops the knobs — the same degrade as the interop
                // attribute reads in ReadMethodInteropAttributes. NoAlloc degrades
                // the same way: a dropped verify claims nothing, the safe direction.
            }
            // Register the verification root once. ComputeHotPathBits is cached
            // (_hotPathBits), so this appends to Compilation.NoAllocMethods exactly once.
            if ((bits & HotPathNoAlloc) != 0)
                Module.Owner.NoteNoAllocMethod(this);
            return bits;
        }
        return 0;
    }

    /// <summary>Canonical shared generics: the layout-canonical owner
    /// instantiation's method this method's emission binds to instead of its own
    /// body. Null (the overwhelmingly common case) means the method emits and is
    /// referenced under its own name. Assigned by
    /// <c>Compilation.FinalizeSharedGenerics</c> for a grouped specialization's
    /// method whose owner body compiled instantiation-independent (taint-free);
    /// a tainted owner body leaves this null and the method compiles its own
    /// body as before (per-method monomorphic fallback).</summary>
    public MethodInfo? SharedImpl;

    /// <summary>Canonical shared generics, method dimension: the canonical
    /// counterpart of a generic-method instantiation — the same template
    /// instantiated on the declaring class's canonical owner (or the class
    /// itself when ungrouped) at canonicalized method arguments. Null when the
    /// instantiation is its own canonical form, unlinked, or sharing is off.
    /// The analog of <see cref="ClassInfo.SharedOwner"/> one dimension down:
    /// the class-level counterpart lookup keys on the template handle
    /// (<c>MethodByTemplate</c>), which cannot address one of several
    /// instantiations sharing that handle, so method instantiations carry the
    /// link themselves.</summary>
    public MethodInfo? SharedOwner;
    /// <summary>Inverse of <see cref="SharedOwner"/>: the instantiations linked
    /// under this canonical generic-method instantiation.
    /// <para>Allocated on first use, not in the constructor: only a canonical owner ever has
    /// one, so on a real corpus all but a fraction of a percent of methods would otherwise
    /// carry an empty list forever. Exposed read-only so a write has to go through
    /// <see cref="AddSharedUser"/>.</para></summary>
    public IReadOnlyList<MethodInfo> SharedUsers => _sharedUsers ?? NoMethods;
    private List<MethodInfo>? _sharedUsers;
    private static readonly List<MethodInfo> NoMethods = new();

    public void AddSharedUser(MethodInfo user) => (_sharedUsers ??= new()).Add(user);

    /// <summary>Canonical shared generics: whether this (canonical) method's
    /// shared body reads the runtime generic context — it loads at least one
    /// rgctx slot, or passes the context on to a callee that needs it. Set by
    /// <c>Compilation.FinalizeSharedGenerics</c> from the planning-pass trial
    /// compiles; drives the context-source choice (receiver-derived prologue vs
    /// hidden trailing parameter).</summary>
    public bool RgctxUses;
    /// <summary>Whether the shared body receives its runtime generic context as
    /// a hidden trailing <c>const void* const* __rgctx</c> parameter — the
    /// context-using methods with no receiver-derivable source (statics,
    /// value-type receivers, and reference receivers of nested generic classes,
    /// which have no open-definition anchor type-info). A grouped user of such
    /// a body emits a one-line forwarder under its own symbol that appends its
    /// class's table, so every boundary (vtables, interface tables, ldftn,
    /// reflection) keeps a per-instantiation function with the original
    /// signature.</summary>
    public bool RgctxParam;

    /// <summary>The method whose emitted C++ symbol represents this method:
    /// the shared canonical body when one is assigned, else the method itself.
    /// Every place that names a method's C++ symbol (direct calls, vtables,
    /// interface tables, reflection tables, ldftn, GVM dispatchers) goes
    /// through this. A shared body that takes the hidden rgctx parameter is the
    /// exception: the method keeps its own symbol — emitted as a one-line
    /// forwarder appending the instantiation's table — so callers and boundary
    /// tables stay signature-compatible and per-instantiation.</summary>
    public MethodInfo Emittable => SharedImpl is null || SharedImpl.RgctxParam ? this : SharedImpl;

    /// <summary>Type/method arguments in scope when this (specialized)
    /// method's signature and IL are decoded.</summary>
    public GenericContext Context = GenericContext.Empty;
    /// <summary>Disambiguator appended to the C++ name for generic-method
    /// instantiations (which share Handle/row with their template).</summary>
    public string NameSuffix = "";

    public bool IsStatic => (Attributes & MethodAttributes.Static) != 0;
    public bool IsPublic => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
    public bool IsPrivate => (Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
    public bool IsVirtual => (Attributes & MethodAttributes.Virtual) != 0;
    public bool IsNewSlot => (Attributes & MethodAttributes.NewSlot) != 0;
    public bool IsAbstract => (Attributes & MethodAttributes.Abstract) != 0;

    public bool IsSynchronized => (ImplAttributes & MethodImplAttributes.Synchronized) != 0;
    public bool IsNoInlining => (ImplAttributes & MethodImplAttributes.NoInlining) != 0;
    public bool IsAggressiveInlining => (ImplAttributes & MethodImplAttributes.AggressiveInlining) != 0;

    /// <summary>IL-size ceiling for <see cref="IsTinyIlBody"/>: covers field
    /// accessors (ldarg; ldfld; ret ≈ 7 B) and one-op arithmetic wrappers
    /// (ldarg; ldarg; add.ovf; ret ≈ 5 B) while excluding anything with a
    /// branch chain. A build-time constant, never environment-driven — the
    /// emitted text must be identical across hosts for the self-host fixpoint.</summary>
    private const int TinyInlineIlBytes = 16;
    private bool? _tinyIlBody;
    /// <summary>Whether the body is small enough to promote into the shared
    /// header as an <c>inline</c> definition even without
    /// <c>[MethodImpl(AggressiveInlining)]</c>: real IL of at most
    /// <see cref="TinyInlineIlBytes"/> bytes and no exception regions. Decided
    /// from the input-assembly bytes only (never the emitted text), so the
    /// planning and emission passes agree; <c>.cctor</c> is excluded because
    /// EmitCctorEnsures hand-writes its bare prototype. Cached — Signature()
    /// re-reads it for every prototype, body, and trap-fallback emission.</summary>
    public bool IsTinyIlBody => _tinyIlBody ??= ComputeInlineIlBody(TinyInlineIlBytes);

    private bool ComputeInlineIlBody(int maxIlBytes)
    {
        if (Rva == 0 || Module is null || Name == ".cctor")
            return false;
        var body = Module.PE.GetMethodBody(Rva);
        return body.GetILBytes() is { } il && il.Length <= maxIlBytes
            && body.ExceptionRegions.Length == 0;
    }

    /// <summary>IL-size ceiling for <see cref="IsSmallIlBody"/>: caps which
    /// <c>[MethodImpl(AggressiveInlining)]</c> bodies are promoted into the shared header as
    /// <c>inline</c> definitions. The attribute is a per-call hint, but header promotion has a
    /// global cost — every TU re-parses each promoted body — and the BCL stamps the attribute
    /// on some huge generic bodies whose instantiations can dominate the header while clang
    /// would never inline them anyway. 128 B keeps the genuinely inlinable wrappers (the
    /// 65-128 B band is full of hot per-element helpers whose call sites sit inside byte/char
    /// loops) and demotes the rest to ordinary single-TU definitions. A build-time constant,
    /// never environment-driven — the emitted text must be identical across hosts for the
    /// self-host fixpoint.</summary>
    private const int PromoteInlineIlBytes = 128;
    private bool? _smallIlBody;
    /// <summary>Whether an <c>[MethodImpl(AggressiveInlining)]</c> body is
    /// small enough to honor the hint with a header <c>inline</c> definition
    /// (see <see cref="MethodCompiler.EmitsInline"/>): real IL of at most
    /// <see cref="PromoteInlineIlBytes"/> bytes, mirroring the
    /// <see cref="IsTinyIlBody"/> exclusions — no exception regions (a
    /// try/catch body emits a multi-line handler frame far larger than its IL
    /// length suggests, and a body clang must outline around is not worth
    /// re-parsing in every TU) and not a <c>.cctor</c> (EmitCctorEnsures
    /// hand-writes its bare prototype, which must not disagree with an inline
    /// header definition). Decided from the input-assembly bytes only (never
    /// the emitted text), so the planning and emission passes agree. Cached —
    /// Signature() re-reads it for every prototype and body emission.</summary>
    public bool IsSmallIlBody => _smallIlBody ??= ComputeInlineIlBody(PromoteInlineIlBytes);

    // NameSuffix is sanitized here (not at construction) because the raw suffix
    // doubles as a distinct mangle key; for genuine instantiations sanitization
    // is the identity, while a canonical placeholder's '$Cn' token becomes a
    // valid identifier chunk in the emitted symbol.
    // Cached: Name/Handle/NameSuffix are construction-only, and the declaring
    // class's CppNamePrefix is finalized before any MethodInfo exists.
    private string? _cppName;
    public string CppName => _cppName ??= "m_" + DeclaringClass.CppName + "_" + CppNaming.Sanitize(Name)
                             + "_" + MetadataTokens.GetRowNumber(Handle) + CppNaming.Sanitize(NameSuffix);

    /// <summary>Total order over the methods of the whole compilation: declaring class
    /// first (see <see cref="ClassInfo.CompareByOrder"/> for why emission must not read
    /// discovery order), then the row of the defining MethodDef, then the suffix that
    /// tells a generic method's instantiations apart — they share a row.
    /// The one comparison every emission-order-sensitive sequence of methods is sorted
    /// by.</summary>
    internal static int CompareByOrder(MethodInfo a, MethodInfo b)
    {
        if (ReferenceEquals(a, b))
            return 0;
        int c = ClassInfo.CompareByOrder(a.DeclaringClass, b.DeclaringClass);
        if (c != 0)
            return c;
        c = MetadataTokens.GetRowNumber(a.Handle).CompareTo(MetadataTokens.GetRowNumber(b.Handle));
        return c != 0 ? c : string.CompareOrdinal(a.NameSuffix, b.NameSuffix);
    }

    /// <summary>Name+parameter shape key used for virtual override matching:
    /// the name immediately followed by <see cref="SigShape"/>.
    ///
    /// <para><b>Guard every comparison with the name:</b> write
    /// <c>x.Name == t.Name &amp;&amp; x.SigKey == t.SigKey</c>, never the key test alone. The
    /// key bakes the name in, so a bare key comparison has nothing to short-circuit on — it
    /// reads the SHAPE of every candidate it scans, and reading a shape decodes a signature
    /// where reading the name does not. These comparisons walk base chains, so that is the
    /// difference between paying for the methods that could match and paying for every method
    /// on the chain. The guard is equivalent-or-stricter: the only pairs it newly rejects are
    /// ones whose differing (name, shape) splits concatenate to the same string.</para>
    ///
    /// Cached: the name is set at construction, and a signature never changes once
    /// decoded.</summary>
    private string? _sigKey;
    public string SigKey => _sigKey ??= Name + SigShape;

    /// <summary>The method's v1 sigShape (<c>(paramTypes):retType</c>) — SigKey
    /// without the leading name. The base emitter bakes it into each reflected
    /// method row of a <c>--hotupdate-base</c> build so the hot-update loader can
    /// tell same-<c>(name, arity, static)</c> overloads (and a generic method's
    /// several instantiations, all emitted under one name) apart.
    /// Cached — and reading it is what decodes <see cref="Signature"/>, which is the
    /// whole reason the comparisons above guard on the name first.</summary>
    private string? _sigShape;
    public string SigShape => _sigShape ??= AbiContract.SigShape(Signature);
}

/// <summary>The decoded `[DllImport]` metadata of a P/Invoke method:
/// the native module/library name (e.g. <c>"libc"</c>), the entry-point symbol
/// (the explicit <c>EntryPoint</c> or, when unspecified, the method name), the
/// raw import flags (calling convention, charset, SetLastError), and any explicit
/// per-parameter / return <c>[MarshalAs(UnmanagedType.*)]</c> override (
/// <see cref="ReturnMarshalAs"/> for the return, <see cref="ParamMarshalAs"/>
/// keyed by 0-based parameter index — both absent when no <c>[MarshalAs]</c> is
/// present, in which case the marshalling follows the parameter type + CharSet).</summary>
internal sealed record PInvokeInfo(
    string ModuleName, string EntryPoint, MethodImportAttributes ImportAttributes,
    UnmanagedType? ReturnMarshalAs,
    IReadOnlyDictionary<int, UnmanagedType> ParamMarshalAs)
{
    /// <summary>The C++-side alias the generated code calls for a P/Invoke method with
    /// the given native signature. The native symbol is bound to the alias via an
    /// <c>asm</c> label / <c>/alternatename</c> on the <c>extern "C"</c> declaration, so
    /// the alias name dodges any prototype a system header (e.g. <c>&lt;cstring&gt;</c>)
    /// already pulled in for the same entry point.
    ///
    /// The alias is keyed on the entry point <em>and the native signature shape</em>.
    /// Same-EntryPoint P/Invokes whose native signatures agree (the common case) collapse
    /// to one extern (identical shape → identical alias); those whose signatures DIFFER —
    /// e.g. the win-x64 CoreLib's two <c>ReadFile</c>/<c>WriteFile</c> overloads, which
    /// bind the same entry point but marshal <c>lpNumberOfBytesRead</c> as <c>out int</c>
    /// (→ <c>void*</c>) in one and <c>IntPtr</c> (→ <c>intptr_t</c>) in the other — get
    /// distinct externs, each still bound to the same real symbol. Without the shape key a
    /// call from the second overload passes an <c>intptr_t</c> where the shared extern
    /// (declared from the first) expects <c>void*</c>, a C++ type error (MSVC C2664). Both
    /// the extern declaration and the call site compute this from the method's own
    /// signature, so they always agree without any global cross-method knowledge.</summary>
    public string CppAlias(MethodSignature<TypeDesc> sig)
    {
        bool uni = (ImportAttributes & MethodImportAttributes.CharSetMask)
            == MethodImportAttributes.CharSetUnicode;
        string shape = CppTypes.PInvokeNativeType(sig.ReturnType, isReturn: true, uni, ReturnMarshalAs) ?? "?";
        for (int i = 0; i < sig.ParameterTypes.Length; i++)
            shape += "|" + (CppTypes.PInvokeNativeType(
                sig.ParameterTypes[i], charUnicode: uni,
                marshalAs: ParamMarshalAs.TryGetValue(i, out var u) ? u : (UnmanagedType?)null) ?? "?");
        return "dn2cpp_pinvoke_" + CppNaming.Sanitize(EntryPoint) + "_" + CppNaming.ShapeHash(shape);
    }
}

internal sealed class ClassInfo
{
    public required string Namespace;
    public required string Name;
    public required TypeDefinitionHandle Handle;
    public Module Module = null!;           // owning assembly
    public ClassInfo? BaseClass;            // null => derives from System.Object (or is interface/static)
    /// <summary>The CLR full name of a base-type TypeReference whose declaring
    /// assembly is NOT loaded (so <see cref="BaseClass"/> stayed null), or null when the base
    /// resolved (or is System.Object). Shape — populated by the Pass-2 / CompleteShape base
    /// fill, no decode. A corelib-less build's app exception class <c>MyEx : SystemException</c>
    /// has no base ClassInfo chain to walk, so <see cref="Compilation.InheritsFromException"/>
    /// reads this name at the chain's end to recognize an External BCL exception ancestor;
    /// without it the class would root at Dn2CppObject and the base-Exception ctor intrinsic
    /// would write the message/inner/trace prefix past the allocation.</summary>
    public string? ExternalBaseName;
    public bool IsAbstract;
    public bool IsSealed;                   // metadata Sealed bit (value types/enums/delegates/static classes carry it)
    public bool IsPublic;
    public bool IsValueType;                // base is System.ValueType
    public bool IsByRefLike;                // ref struct (IsByRefLikeAttribute): never boxed
    /// <summary>N for a <c>[InlineArray(N)]</c> struct (the fixed-buffer types
    /// Roslyn synthesizes for params-<c>ReadOnlySpan</c> lowering): the single
    /// declared field provides storage for N contiguous elements, so the struct is
    /// laid out as that field repeated N times. 0 = not an inline array.</summary>
    public int InlineArrayLength;
    /// <summary>[StructLayout(LayoutKind.Explicit)]: every instance field carries an
    /// explicit byte offset, so the emitted C++ layout must honor the offsets exactly
    /// (overlapping fields form unions). See CppEmitter's explicit-layout emission.</summary>
    public bool IsExplicitLayout;
    /// <summary>The metadata LayoutMask reads AutoLayout — the CLR is free to place the
    /// fields, so there is no unmanaged layout at all. It is the C# default for a
    /// <b>class</b> and for an <b>enum</b>, and it is what <c>[StructLayout(LayoutKind.Auto)]</c>
    /// puts on a struct (System.DateTime and System.DateTimeOffset carry it). Nothing about
    /// the EMITTED layout depends on it — dn2cpp lays every value type out sequentially —
    /// so this exists for the two marshalling readers: the blittability verdict
    /// (<see cref="Compilation.MarshalVerdictOf"/>) and the marshalled-layout model
    /// (<c>Compilation.MarshalLayout.cs</c>). In both it is precisely the condition under
    /// which real .NET's <c>Marshal.SizeOf</c> refuses to answer. Do not read it as a
    /// layout instruction.
    ///
    /// <para>It is a top-level gate and very nearly an inherited one too: .NET refuses a
    /// struct holding a <c>DateTimeOffset</c>, and refuses one holding a user
    /// <c>[StructLayout(LayoutKind.Auto)]</c> struct even with a single <c>long</c> field.
    /// <c>System.DateTime</c> is the sole auto-layout CoreLib value type measurable as a
    /// field, so the layout model names it and refuses the rest. The gate is still asked
    /// separately at the two positions, because the top level refuses DateTime as
    /// well.</para>
    ///
    /// <para>Decoded in Pass 2 for every type of every loaded assembly, and at
    /// specialization completion for every closed generic — so the false default means
    /// "sequential" only for a ClassInfo that went through neither, which no emitted class
    /// does (<c>CppEmitter.EmitAdd</c> decodes what it emits). Were one to slip through, the
    /// verdict would call it INEXACT rather than NotMarshalable: a
    /// PlatformNotSupportedException where .NET raises ArgumentException — the wrong
    /// diagnostic, but still a refusal, never a number.</para></summary>
    public bool IsAutoLayout;
    /// <summary>The metadata ClassLayout packing size ([StructLayout(Pack = N)]):
    /// each field's alignment is capped at N bytes. 0 = default packing.</summary>
    public int LayoutPack;
    /// <summary>The metadata ClassLayout explicit size ([StructLayout(Size = N)],
    /// also carried by compiler-generated fixed-buffer structs). 0 = none.</summary>
    public int LayoutSize;
    /// <summary>Whether the type's <c>[StructLayout].CharSet</c> is <c>Unicode</c> — the
    /// metadata StringFormat on the TypeDef, which is what real .NET reads to marshal the
    /// type's own <c>char</c> and <c>string</c> fields (independent of any importing
    /// method's CharSet).
    ///
    /// <para><b>The axis is binary here, and <c>CharSet.Auto</c> is Ansi.</b> Auto resolves to
    /// the platform's native charset, which is Ansi everywhere but Windows, and it is the
    /// reading the P/Invoke struct marshaller makes too — <c>CppEmitter</c>'s
    /// <c>tn_&lt;Name&gt;</c> encodes an Auto struct's strings as UTF-8. The two readers must
    /// agree, because <c>Marshal.SizeOf</c> answering a width the emitted marshaller does not
    /// use is a silently wrong number; sharing this one decoded bit makes them agree
    /// structurally rather than by review.</para>
    ///
    /// <para>The Windows divergence is real and is a declared one: there
    /// <c>CharSet.Auto</c> is Unicode, so a <c>char</c>/<c>string</c> field of an
    /// Auto-CharSet struct marshals at 2 bytes / UTF-16 and dn2cpp would report the Ansi
    /// number. It is not host-driven output — the bit is read from the INPUT metadata, so a
    /// given assembly transpiles identically on every host, which is the self-host
    /// requirement; what a Windows target would need is a target flag, not a host
    /// read.</para></summary>
    public bool LayoutCharSetUnicode;
    public bool IsEnum;                     // base is System.Enum
    /// <summary>An enum's underlying integer type code (the <c>value__</c> field's
    /// type), defaulting to Int32. The enum's *value* rides the int32 stack model for
    /// every underlying narrower than 64 bits and the int64 one for Int64/UInt64
    /// (<see cref="CppTypes.Of"/>); its **array element storage** follows the real
    /// width (byte→1, short→2, int→4, long→8) so a packed enum array matches the
    /// underlying-width ldelem/stelem opcodes Roslyn emits for it.</summary>
    public PrimitiveTypeCode EnumUnderlying = PrimitiveTypeCode.Int32;
    public bool IsInterface;
    public bool IsDelegate;                 // base is System.MulticastDelegate
    public int GenericArity;                // count of type parameters (0 = non-generic)
    /// <summary>Type-argument nesting depth of a closed specialization: 0 for a
    /// non-generic class, else <c>1 + max(depth of the type arguments)</c> — so
    /// <c>List&lt;int&gt;</c> is 1 and <c>List&lt;List&lt;int&gt;&gt;</c> is 2.
    /// Assigned once at <see cref="Compilation.Instantiate"/>, which is what makes
    /// the depth of a new instantiation an O(arity) lookup instead of a recursive
    /// walk. Monomorphization is a fixpoint with no natural fixed point over a
    /// self-nesting generic (<c>M&lt;T&gt;() =&gt; M&lt;List&lt;T&gt;&gt;()</c>
    /// deepens forever), so this is the quantity the instantiation bound watches.</summary>
    public int GenericDepth;
    /// <summary>Set for async/await Task-family types: the C++ runtime
    /// type this managed type lowers to (e.g. "Dn2CppTask*"). Non-null means the
    /// type is modeled by a hand-written runtime struct — its IL is never
    /// transpiled and no struct/vtable/type-info is emitted for it.</summary>
    public string? IntrinsicCppName;
    /// <summary>A closed specialization is completed in two stages, and the seam is not
    /// arbitrary.
    ///
    /// <para><b>Shape</b> — what the type <em>is</em>: its base, the kind flags derived
    /// from that base, its layout attributes, its interfaces and its fields. Every one of
    /// those is read by code that only ever <em>names</em> the type — <see cref="CppTypes.Of"/>
    /// deciding struct-versus-pointer from <see cref="IsValueType"/>, the struct emitter
    /// laying out <see cref="Fields"/>, <see cref="TypeDesc.ContainsGcReferences"/>, the
    /// Godot backend walking <see cref="BaseClass"/> to decide whether a class registers
    /// with ClassDB. A shell that answered those with defaults would not fail, it would
    /// quietly emit a pointer where a struct belongs. So shape is populated eagerly, by
    /// the drain, exactly as before: no reader can observe a specialization lying about
    /// what it is.</para>
    ///
    /// <para><b>Members</b> — what the type can <em>do</em>: its methods, and the vtable and
    /// dispatch tables built from them. These are pulled on demand (see
    /// <see cref="Methods"/>), because decoding a member's signature is the most expensive
    /// thing the model does and the least often needed — most specializations have no
    /// reachable method at all. It is also the operation with no fixpoint: a signature naming
    /// a deeper instantiation of its own declaring type
    /// (<c>GDTask&lt;T&gt;.SuppressCancellationThrow() -&gt; GDTask&lt;(bool, T)&gt;</c>)
    /// recurses forever if members are decoded whether or not anything calls them. On demand,
    /// nothing reaches the member, so nothing decodes it.</para></summary>
    public bool ShapeCompleted;
    public bool MembersCompleted;

    /// <summary>True when this class's shape tier — base link and interface list among
    /// it — is final: the class is non-generic (wired once at load, before any
    /// reachability question is asked) or its <see cref="ShapeCompleted"/> pass ran.
    /// The shape-tier analogue of <see cref="MembersReady"/>: what a cache over
    /// shape-derived facts (the interface closure below) must see on every node it
    /// read before it may keep the answer.</summary>
    public bool ShapeReady => GenericArity == 0 || ShapeCompleted;

    public GenericContext Context = GenericContext.Empty; // for closed specializations
    public List<ClassInfo> Interfaces = new(); // directly implemented interfaces

    /// <summary>Cached transitive interface closure — every interface this class (or a
    /// class/interface on any base chain the walk crosses) implements, directly or
    /// through interface inheritance. Interfaces are shape: populated at load
    /// (non-generic) or <c>CompleteShape</c> (specializations) and immutable after, so
    /// the closure is final once every node the walk visited was
    /// <see cref="ShapeReady"/> — Compilation.ImplementsInterface only stores it then,
    /// and a walk that crossed a not-yet-completed specialization stays uncached so a
    /// later ask re-reads the live lists exactly as the uncached walk always did.
    /// Membership-only (never enumerated), so set order cannot reach the output.</summary>
    internal HashSet<ClassInfo>? InterfaceClosureCache;
    // Interfaces implemented by CLR FullName but not modeled as a loaded
    // ClassInfo — the assembly declaring them was not referenced at transpile
    // (e.g. a hot-update patch that implements a base-image interface without
    // the base being loaded). Kept so the patch converter can resolve them
    // against the base-image ABI manifest; the normal pipeline ignores them.
    public List<string> ExternalInterfaceNames = new();
    public List<FieldInfo> Fields = new();

    // The member tier. Reading any of these on a specialization decodes them first, so a
    // caller that forgot to pull loses memory, never correctness — the model ends up bigger
    // than it needed to be, which peak RSS shows and DN2CPP_STRICT_COMPLETION localizes.
    // Completion itself writes through these, which is safe only because it sets
    // MembersCompleted before it fills them and so cannot re-enter.
    public List<MethodInfo> Methods { get { PullMembers(); return _methods; } set { _methods = value; _methodsByName = null; } }
    public List<MethodInfo?> Vtable { get { PullMembers(); return _vtable; } set { _vtable = value; } } // entries may be null for abstract methods
    public List<MethodInfo> SlotOwners { get { PullMembers(); return _slotOwners; } set { _slotOwners = value; } } // per-slot signature owner (for override matching)
    public Dictionary<MethodDefinitionHandle, MethodInfo> MethodByTemplate { get { PullMembers(); return _methodByTemplate; } } // specs: template method -> instance
    public Dictionary<MethodInfo, MethodInfo> ExplicitInterfaceImpls { get { PullMembers(); return _explicitItfImpls; } } // explicit interface declaration -> body

    /// <summary>Lazy name→methods index over <see cref="Methods"/>, shared by the linear
    /// same-name scans (Compilation.ResolveItfImplOrNull's signature-match loop,
    /// ResolveMemberRefMethod's overload scan). Reading it pulls members through the
    /// <see cref="Methods"/> accessor — exactly where the scans it replaces read them — and
    /// the index itself reads only <see cref="MethodInfo.Name"/> (a plain field), so building
    /// it decodes nothing. <see cref="Methods"/> is append-only after members complete
    /// (generic-method instantiations arrive at the tail), so the index syncs incrementally by
    /// count; per-name lists keep list order, so a scan over one visits the same methods in
    /// the same order the full scan did. The setter above drops the index.</summary>
    private Dictionary<string, List<MethodInfo>>? _methodsByName;
    private int _methodsByNameSynced;

    /// <summary>The methods named <paramref name="name"/>, in <see cref="Methods"/> order,
    /// or null when there are none. The returned list is the index's own shared
    /// backing list, handed out raw — read-only by contract, never mutate it.
    /// See <see cref="_methodsByName"/>.</summary>
    public List<MethodInfo>? MethodsNamed(string name)
    {
        var methods = Methods; // pulls members exactly where the scans it replaces did
        if (_methodsByName is null)
        {
            _methodsByName = new Dictionary<string, List<MethodInfo>>();
            _methodsByNameSynced = 0;
        }
        for (int i = _methodsByNameSynced; i < methods.Count; i++)
        {
            var m = methods[i];
            if (!_methodsByName.TryGetValue(m.Name, out var list))
                _methodsByName[m.Name] = list = new List<MethodInfo>();
            list.Add(m);
        }
        _methodsByNameSynced = methods.Count;
        return _methodsByName.TryGetValue(name, out var hit) ? hit : null;
    }

    /// <summary>The first non-static method named <paramref name="name"/> on this class
    /// or up its base chain (most-derived first), or null. The named-attribute-argument setter
    /// lookup: a named property may be declared on an attribute BASE class, so the
    /// declared-members list alone misses it and the whole attribute row silently drops. Used
    /// by BOTH sides of the attribute pipeline — Compilation.ReachAttributesOf and
    /// CppEmitter.RenderNamedArg — which must agree: an emit-side find whose reach side missed
    /// fails the Reachable test and drops the attribute all the same.</summary>
    public MethodInfo? InstanceMethodOnBaseChain(string name)
    {
        for (var c = this; c is not null; c = c.BaseClass)
            if (c.MethodsNamed(name) is { } ms)
                foreach (var m in ms)
                    if (!m.IsStatic)
                        return m;
        return null;
    }

    /// <summary>The first non-static field named <paramref name="name"/> on this class
    /// or up its base chain (most-derived first), or null. The named-attribute-argument
    /// field counterpart of <see cref="InstanceMethodOnBaseChain"/>: the emitted C++
    /// struct chains through its base, so <c>o-&gt;f_…</c> reaches an inherited field
    /// directly and no reach-side pairing is needed (field storage rides the struct
    /// layout).</summary>
    public FieldInfo? InstanceFieldOnBaseChain(string name)
    {
        for (var c = this; c is not null; c = c.BaseClass)
            foreach (var f in c.Fields)
                if (!f.IsStatic && f.Name == name)
                    return f;
        return null;
    }

    private List<MethodInfo> _methods = new();
    private List<MethodInfo?> _vtable = new();
    private List<MethodInfo> _slotOwners = new();
    private Dictionary<MethodDefinitionHandle, MethodInfo> _methodByTemplate = new();
    private Dictionary<MethodInfo, MethodInfo> _explicitItfImpls = new();

    /// <summary>The class's static constructor — the <c>.cctor</c> row with a real body —
    /// or null. Reads <see cref="Methods"/>, so on a specialization the first ask decodes
    /// members exactly as the inline scans it replaces did. Cached: the metadata-row
    /// methods are all in once members are decoded, and the only later growth of
    /// <see cref="Methods"/> is generic-method instantiations
    /// (<c>Compilation.InstantiateMethodOnClass</c>), which are never named
    /// <c>.cctor</c>.</summary>
    public MethodInfo? StaticCctor
    {
        get
        {
            if (!_staticCctorScanned)
            {
                foreach (var m in Methods)
                {
                    if (m.Name == ".cctor" && m.IsStatic && m.Rva != 0)
                    {
                        _staticCctor = m;
                        break;
                    }
                }
                _staticCctorScanned = true;
            }
            return _staticCctor;
        }
    }
    private MethodInfo? _staticCctor;
    private bool _staticCctorScanned;

    /// <summary>Throw on an un-pulled member read instead of quietly decoding it. A
    /// legitimate reader has already called <see cref="Compilation.EnsureCompleted"/> and
    /// never trips; what trips is a walk over every class that reads members it does not use
    /// — not a correctness bug, but exactly what eats the saving. Held by
    /// gates/build-and-run-emit-order-stability.sh, which a self-healing accessor could not
    /// be.</summary>
    private static readonly bool StrictCompletion = EnvKnobs.BoolNonZero(EnvKnobs.StrictCompletion);

    /// <summary>True when reading <see cref="Methods"/> and its siblings decodes nothing:
    /// the class is not a specialization, or its members are already in. An instrument that
    /// completed the model it was measuring would not be an instrument, so
    /// <see cref="ModelCensus"/> asks this first.</summary>
    public bool MembersReady => GenericArity == 0 || MembersCompleted;

    /// <summary>Decode this class's members now — the explicit form of what the accessors do
    /// implicitly. Saying it is what distinguishes a reader that needs the members from one
    /// walking every class and reading members it will throw away: the first passes under
    /// DN2CPP_STRICT_COMPLETION, the second is what that mode is for. The static base-chain
    /// walkers use this because they have no Compilation in hand.</summary>
    public ClassInfo EnsureMembers()
    {
        Module.Owner.EnsureCompleted(this);
        return this;
    }

    private void PullMembers([CallerMemberName] string member = "")
    {
        if (GenericArity == 0 || MembersCompleted)
            return;
        if (StrictCompletion)
            throw new StrictCompletionException(
                $"DN2CPP_STRICT_COMPLETION: read {FullName}.{member} before its members were decoded. "
                + "Either pull it (Compilation.EnsureCompleted) or — if this walks every class — "
                + "skip the specializations nothing reaches, which is the point of deferring them.");
        Module.Owner.CompleteMembers(this);
    }

    /// <summary>Canonical shared generics: the layout-canonical owner
    /// instantiation this specialization is grouped under (its type arguments
    /// canonicalized — enums to width-preserving placeholders, references to
    /// CnRef), or null when the class is its own canonical form, ungrouped, or
    /// sharing is off. Each real instantiation always keeps its own type-info,
    /// statics and interface tables; only struct layout and method bodies are
    /// candidates for sharing with the owner.</summary>
    public ClassInfo? SharedOwner;
    /// <summary>Inverse of <see cref="SharedOwner"/>: the real specializations
    /// grouped under this canonical owner. Allocated on first use — only canonical
    /// owners have one — and exposed read-only, so writes go through
    /// <see cref="AddSharedUser"/> / <see cref="RemoveSharedUser"/>.</summary>
    public IReadOnlyList<ClassInfo> SharedUsers => _sharedUsers ?? NoClasses;
    private List<ClassInfo>? _sharedUsers;
    private static readonly List<ClassInfo> NoClasses = new();

    public void AddSharedUser(ClassInfo user) => (_sharedUsers ??= new()).Add(user);

    public void RemoveSharedUser(ClassInfo user)
    {
        // Not `_sharedUsers?.Remove(user)`: a null-conditional receiver skips the
        // argument expression too, and that idiom is how this codebase has silently
        // dropped side effects before. Spelled out, it cannot.
        if (_sharedUsers is { } users)
            users.Remove(user);
    }

    /// <summary>Gate for redirecting a grouped specialization's struct layout to
    /// its canonical owner's. Set (per process) by the Compilation constructor
    /// when shared generics are enabled; with the flag off every class keeps
    /// emitting and naming its own layout and the output is unchanged.</summary>
    internal static bool ShareStructLayout;

    // Cached: Namespace/Name are construction-only. Built fresh on every read this was one
    // of the transpiler's largest sources of allocation churn — it is read from hundreds of
    // sites, several of them per reached method and per compiled call site.
    private string? _fullName;
    public string FullName => _fullName ??=
        string.IsNullOrEmpty(Namespace) ? Name : Namespace + "." + Name;
    // Cached arity-stripped generic-definition full name, filled by
    // Compilation.GenericDefFullName on first read (the compute needs the metadata reader,
    // so it lives there). Same rationale as _fullName: construction-only inputs, immutable
    // once computed, read from many sites per scanned call site. Unlike CppName it is
    // metadata-derived, so the CppNamePrefix setter has nothing to invalidate here.
    internal string? GenericDefFullNameCache;
    // Enclosing-type qualifier for the emitted C++ names of a nested type (e.g. "A.W." for
    // A.W's nested state machine). A nested type carries an empty namespace and only its
    // simple name, so two same-named nested types would otherwise collide in C++. FullName
    // is left bare so (namespace, name) matching and intrinsic lookups are unaffected; only
    // the C++ identifiers are disambiguated. The CLR reflection name a nested type REPORTS
    // ("Ns.Outer+Inner") is a third rendering — Compilation.ReflectionTypeName — never
    // stored here.
    // A property (not a field) so mutating the prefix — nested-type qualification and
    // cross-module disambiguation, both before any member exists — invalidates the mangled
    // names cached below.
    private string _cppNamePrefix = "";
    public string CppNamePrefix
    {
        get => _cppNamePrefix;
        set
        {
            _cppNamePrefix = value;
            _cppName = null;
            _mangleFragment = null;
            _ownStructName = null;
            _ownStructPtrName = null;
        }
    }
    // Cached: Namespace/Name are construction-only; the prefix setter above
    // invalidates on the only other input.
    private string? _cppName;
    public string CppName => _cppName ??= CppNaming.Sanitize(CppNamePrefix + FullName);

    /// <summary>This class's fragment in a generic-argument mangle — <see cref="CppName"/>,
    /// escaped when another mangle spelling could produce it
    /// (<see cref="CppNaming.MangleFragment"/>). Cached HERE rather than in
    /// <c>TypeDesc.MangleCache</c> for the same reason CppName is not cached there: the
    /// <see cref="CppNamePrefix"/> setter can still move it, and it invalidates this
    /// alongside the name it derives from.</summary>
    private string? _mangleFragment;
    internal string MangleFragment => _mangleFragment ??= CppNaming.MangleFragment(CppName);

    /// <summary>A content-derived order key: where this class sits in the *input*
    /// metadata, never where discovery happened to put it in
    /// <see cref="Compilation.Classes"/>.
    /// <para>Emission reads this wherever order reaches the output — which same-CppName twin
    /// wins, declaration order, and (through the order bodies compile in) the numbering of
    /// the literal pool, the rgctx slots, the interned metadata pools and the cctor list.
    /// Those numbers are baked into the emitted text, so the same assemblies must emit the
    /// same bytes however the model was walked to find them.</para>
    /// <para>Module index, then the row of the defining TypeDef, then the name — which for a
    /// specialization already carries its mangled type arguments (Instantiate builds it that
    /// way), so the three together are unique.</para>
    /// <para>A comparison, not a cached key: this orders 10^4..10^6 classes, and the parts
    /// are already in hand, so a key string per class is memory for nothing.</para></summary>
    internal static int CompareByOrder(ClassInfo a, ClassInfo b)
    {
        if (ReferenceEquals(a, b))
            return 0;
        int c = a.Module.Index.CompareTo(b.Module.Index);
        if (c != 0)
            return c;
        c = MetadataTokens.GetRowNumber(a.Handle).CompareTo(MetadataTokens.GetRowNumber(b.Handle));
        return c != 0 ? c : string.CompareOrdinal(a.Name, b.Name);
    }

    /// <summary>This class's interned <see cref="TypeDesc"/> — one object per class, not one
    /// per signature element that names it. Signature decoding runs over every member of
    /// every loaded assembly, reachable or not, and a class-kind element is the commonest
    /// thing it decodes. Sound for the same reason the primitive table is — a TypeDesc is
    /// never mutated after construction — and invisible to emission, because the only key
    /// derived from a descriptor (<c>Compilation.MangleArg</c>) reads its content, never its
    /// identity.
    /// <para>The <see cref="CppNamePrefix"/> setter does not invalidate it: the descriptor
    /// names this ClassInfo, not its mangled name.</para></summary>
    internal TypeDesc? Desc;
    // Only the own-name arm is cached: SharedOwner is re-linked during emission
    // rounds, so the redirect must stay live.
    private string? _ownStructName;
    /// <summary>The C++ struct carrying this class's layout: its own, or — when it is grouped
    /// under a canonical owner — the owner's.
    ///
    /// <para>Reading it FIXES the grouping. A canonical link is made before its user has a
    /// vtable to compare against the owner's, so it starts out unverified, and verifying it
    /// is what can DROP it (Compilation.SameVtableShape: canonicalization can merge two
    /// overloads into one slot). This spelling is baked into every body that names the type,
    /// and bodies stream out as they compile — long before the emit closure decodes the
    /// class. If the link could still be dropped after that, a body would say
    /// <c>t_C__CnRef*</c> where the declaration says <c>t_C_string*</c> and the emitted C++
    /// would not compile. So the first read settles it, and the answer never changes.</para>
    ///
    /// <para>The verification costs nothing extra: a class an emitted body names gets
    /// emitted, and an emitted class has its members decoded anyway (CppEmitter.EmitAdd).
    /// Specializations nothing names have no owner to verify and never reach
    /// this.</para></summary>
    public string CppStructName => ShareStructLayout && VerifiedSharedOwner is { } o
        ? o.CppStructName : _ownStructName ??= "t_" + CppName;

    // The pointer spelling CppTypes.Of emits for every reference-type slot — read once per
    // local/argument/field rendering, so the `+ "*"` concat was one of the emitter's
    // commonest small allocations. Cached exactly like _ownStructName above: only the
    // own-name arm is cached (the redirect must stay live while links verify), and the
    // redirect goes through the same VerifiedSharedOwner gate, so the first read settles the
    // owner as a CppStructName read would and the two spellings cannot diverge.
    private string? _ownStructPtrName;
    public string CppStructPtrName => ShareStructLayout && VerifiedSharedOwner is { } o
        ? o.CppStructPtrName : _ownStructPtrName ??= CppStructName + "*";

    private ClassInfo? VerifiedSharedOwner
    {
        get
        {
            if (SharedOwner is not null && !SharedLinkVerified)
                Module.Owner.VerifyCanonicalLink(this);
            return SharedOwner;
        }
    }

    /// <summary>Set once <see cref="SharedOwner"/> has been checked against the owner's vtable
    /// shape — or found to need no check. Until then the link is provisional.</summary>
    public bool SharedLinkVerified;
    public string CppVtableName => "vt_" + CppName;

    /// <summary>The symbol every REFERENCE to this class's type-info names — a base
    /// pointer, a catch/isinst handle, a metadata row's declaring type, a reflected
    /// member's FieldType, the registry, <c>typeof</c>. **One managed type must have
    /// exactly ONE type-info**, or an object the runtime allocated and one the generated
    /// code allocated are different types to catch / isinst / typeof / == / GetHashCode /
    /// a GVM's type switch. So whenever the C++ runtime defines a handle for this CLR type
    /// (<see cref="CoreIntrinsics.RuntimeTypeInfoSymbol"/> — the single table that answers
    /// it) that handle IS the type-info and no emitted <c>ti_</c> competes with it.
    ///
    /// <para>Which side holds the CONTENT is the separate question
    /// <see cref="CoreIntrinsics.RuntimeOwnsTypeInfo"/> answers: the runtime owns it (the
    /// primitives, String, the reflection hierarchy — nothing is emitted at all), or the
    /// generated metadata is emitted under <see cref="CppTypeInfoDefName"/> and copied INTO
    /// the handle at startup (dn2cpp_type_binds — the runtime-raised exceptions and
    /// System.Enum), which is what gives the handle its vtable, layout size and reflection
    /// tables, none of which the runtime can know on its own. Either way the SYMBOL is
    /// one.</para></summary>
    public string CppTypeInfoName =>
        CoreIntrinsics.RuntimeTypeInfoSymbol(this) is { } rt ? rt[1..] : "ti_" + CppName;

    /// <summary>The symbol this class's type-info DEFINITION is emitted under. Same as
    /// <see cref="CppTypeInfoName"/> except for the bind-kind types, whose handle the
    /// runtime already defines: their emitted metadata is a separate object the startup
    /// bind copies from. For a runtime-OWNED type this name is never emitted — the
    /// emitter's declaration and definition loops skip the class entirely — and asking for
    /// it is a bug, so it throws rather than handing back a name that would produce a
    /// duplicate definition of the runtime's own handle.</summary>
    public string CppTypeInfoDefName =>
        CoreIntrinsics.RuntimeOwnsTypeInfo(this)
            ? throw new InvalidOperationException(
                $"{FullName}'s type-info is owned by the C++ runtime ({CppTypeInfoName}); "
                + "this emission defines none, so it has no definition symbol. The caller "
                + "should have skipped the class (CoreIntrinsics.RuntimeOwnsTypeInfo).")
            : CoreIntrinsics.RuntimeTypeInfoSymbol(this) is not null ? "tibind_" + CppName : "ti_" + CppName;
}

internal static class CppNaming
{
    public static string Sanitize(string name)
    {
        var sb = new System.Text.StringBuilder(name.Length);
        foreach (char c in name)
        {
            sb.Append(char.IsAsciiLetterOrDigit(c) ? c : '_');
        }
        return sb.ToString();
    }

    /// <summary>The marker a <see cref="MangleFragment"/> escape SUFFIXES onto a named type's
    /// fragment. A suffix, not a prefix, because the array closure appends on the right: a
    /// prefixed escape of an "Arr"-suffixed name still ends in "Arr", so <c>Cls_</c>-escaping
    /// <c>class Int32Arr</c> ("Cls_Int32Arr") would alias the array of the escaped
    /// <c>class Int32</c> ("Cls_Int32" + "Arr"). Identifier-safe on purpose: unlike the
    /// canonical placeholders' <c>'$'</c>, which reaches emitted text only through a
    /// <see cref="Sanitize"/> call, a fragment lands VERBATIM in the
    /// <c>ti_arr_&lt;elem&gt;</c> / <c>ty_arr_&lt;elem&gt;</c> symbols, so a marker outside
    /// <c>[A-Za-z0-9_]</c> would emit an identifier MSVC rejects.</summary>
    private const string NamedTypeMarker = "_Cls";

    /// <summary>The generic-argument mangle fragment of a type whose identity is a
    /// sanitized NAME — a loaded class's <see cref="ClassInfo.CppName"/>, or an external
    /// reference's sanitized full name. Normally the name itself; a name that could be
    /// read as another type's mangle is suffixed with <see cref="NamedTypeMarker"/>.
    ///
    /// <para>Without the escape a name is indistinguishable from the other mangle spellings
    /// sharing its token space: <c>class Int32 {}</c> reads as
    /// <c>PrimitiveTypeCode.Int32</c>, and — since <c>Compilation.MangleArg</c>'s SZArray arm
    /// appends "Arr" per array level — <c>class FooArr {}</c> reads as <c>Foo[]</c>. Either
    /// way the instantiation cache hands back ONE specialization for two distinct type
    /// arguments: their statics and type-infos alias, and the shared struct types its fields
    /// at whichever argument won.</para>
    ///
    /// <para>The escape keeps the whole mangle grammar injective over closed types, by
    /// making every fragment a base no other mangle can spell. A mangle is
    /// base + <see cref="IsKindMarkerSuffix">constructor marker</see>*, and NO base ends
    /// in a marker — primitive tokens and canonical placeholders don't by inspection, and
    /// a named fragment doesn't because a marker-suffixed name is escaped — so the
    /// (base, marker list) decomposition of any mangle is unique, read right to left, and
    /// injectivity reduces to the bases. Those are pairwise distinct across kinds:
    /// placeholders carry '$' (unreachable from <see cref="Sanitize"/>), the
    /// generic-parameter and open-template bases carry a reserved
    /// <see cref="HasKindLeafPrefix">prefix</see>, and the escape itself is injective
    /// over names — a name ending in the marker is escaped too, so <c>n + "_Cls"</c> is
    /// reachable only from <c>n</c>. It deliberately does NOT separate two named types that
    /// share a name — a same-CppName class twin, or a class and an external reference
    /// spelling the same thing. Those conflate by design and the model is built on it (see
    /// <c>Compilation.MethodInstanceKeyComparer</c>); this is a cross-KIND distinction
    /// only.</para>
    ///
    /// <para>The escape fires only for a primitive token, a marker-suffixed name or a
    /// reserved-prefix one, so an ordinary corpus mangles byte-identically — which the
    /// self-host fixpoint and the emit-order-stability gate assert.</para></summary>
    public static string MangleFragment(string sanitizedName) =>
        IsPrimitiveToken(sanitizedName)
            || HasKindLeafPrefix(sanitizedName)
            || IsKindMarkerSuffix(sanitizedName)
            || sanitizedName.EndsWith(NamedTypeMarker, StringComparison.Ordinal)
            ? sanitizedName + NamedTypeMarker
            : sanitizedName;

    /// <summary>Whether a name ends in one of the per-constructor markers
    /// <c>Compilation.MangleArg</c> appends — "Arr" (SZArray), "Md"+rank (MDArray),
    /// "Pointer", "Byref" — so it could be read as some other type's mangle.
    /// <para>No marker may be a suffix of a primitive token, or the (base, markers)
    /// decomposition stops being unique: "Ptr" cannot be the pointer marker, because
    /// <c>class Int</c>'s <c>Int**</c> and <c>IntPtr*</c> would both spell
    /// "IntPtrPtr".</para></summary>
    private static bool IsKindMarkerSuffix(string name) =>
        name.EndsWith("Arr", StringComparison.Ordinal)
        || name.EndsWith("Pointer", StringComparison.Ordinal)
        || name.EndsWith("Byref", StringComparison.Ordinal)
        || EndsWithMdRank(name);

    /// <summary>Whether a name ends in the MDArray marker, "Md" followed by the rank's
    /// decimal digits. The digits are read greedily right to left, so the rank cannot be
    /// split across two markers.</summary>
    private static bool EndsWithMdRank(string name)
    {
        int i = name.Length;
        while (i > 0 && char.IsAsciiDigit(name[i - 1]))
            i--;
        return i < name.Length && i >= 2 && name[i - 1] == 'd' && name[i - 2] == 'M';
    }

    /// <summary>The prefixes reserved for the mangle's two LEAF bases that are not names:
    /// a generic parameter ("Gvar") and an open generic definition ("Gtpl"). A prefix
    /// test rather than the exact token shapes, because a coarse reservation costs a
    /// suffixed name nobody spells and cannot drift out of step with the arms.</summary>
    private static bool HasKindLeafPrefix(string name) =>
        name.StartsWith("Gvar", StringComparison.Ordinal)
        || name.StartsWith("Gtpl", StringComparison.Ordinal);

    /// <summary>The <c>PrimitiveTypeCode.ToString()</c> renderings
    /// <c>Compilation.MangleArg</c>'s primitive arm can produce. Written as a switch
    /// rather than a hash-keyed static: a set populated at static-init is the
    /// System.HashCode-seed trap the self-host already tripped over once (see the
    /// s_primitives note above).</summary>
    private static bool IsPrimitiveToken(string name) => name switch
    {
        "Void" or "Boolean" or "Char" or "SByte" or "Byte" or "Int16" or "UInt16"
            or "Int32" or "UInt32" or "Int64" or "UInt64" or "Single" or "Double"
            or "String" or "TypedReference" or "IntPtr" or "UIntPtr" or "Object" => true,
        _ => false,
    };

    /// <summary>A stable 8-hex-digit FNV-1a hash of a P/Invoke native-signature shape
    /// string, appended to the entry-point alias so same-EntryPoint P/Invokes with
    /// different native signatures get distinct externs (see
    /// <see cref="PInvokeInfo.CppAlias"/>). Built with plain arithmetic + a manual hex
    /// encode (no format string) so it stays deterministic and transpilable under
    /// self-host.</summary>
    public static string ShapeHash(string s)
    {
        uint h = 2166136261u;
        foreach (char c in s)
        {
            h ^= c;
            h *= 16777619u;
        }
        const string hex = "0123456789abcdef";
        var sb = new System.Text.StringBuilder(8);
        for (int shift = 28; shift >= 0; shift -= 4)
            sb.Append(hex[(int)((h >> shift) & 0xFu)]);
        return sb.ToString();
    }
}

internal static class MetadataTokens
{
    public static int GetRowNumber(MethodDefinitionHandle h) =>
        System.Reflection.Metadata.Ecma335.MetadataTokens.GetRowNumber(h);

    public static int GetRowNumber(TypeDefinitionHandle h) =>
        System.Reflection.Metadata.Ecma335.MetadataTokens.GetRowNumber(h);
}
