using System.Reflection.Metadata;

namespace Dn2Cpp;

/// <summary>The architecture a platform-ISA facade belongs to — the namespace
/// segment below <c>System.Runtime.Intrinsics</c>.</summary>
internal enum IsaArch
{
    X86,
    Arm,
    Wasm,
}

/// <summary>One CoreLib platform-ISA facade type (<c>Sse2</c>, <c>Lzcnt.X64</c>,
/// <c>AdvSimd.Arm64</c>, <c>PackedSimd</c>, …) and its capability contract with the
/// C++ runtime. The rows come from the generated table
/// (<c>CoreIntrinsics.PlatformIsa.g.cs</c>); nothing here is hand-edited.
///
/// <para>Every static member of a facade is lowered at the call site — the
/// <c>IsSupported</c> getter to the runtime's capability token or to constant 0, an
/// instruction to a <c>dn2cpp_isa_*</c> helper or to a PlatformNotSupportedException
/// — so its IL is never a reachability edge and no body, method table or type-info is
/// emitted for it. A facade that is not <see cref="Lowered"/> folds its getter to 0,
/// which prunes the guarded SIMD arm exactly as real .NET does on hardware without the
/// instruction set; its instructions throw where .NET would throw.</para></summary>
internal sealed class IsaFamily
{
    public readonly IsaArch Arch;

    /// <summary>The CLR reflection name: nested facades join the declaring chain
    /// with '+' (<c>System.Runtime.Intrinsics.X86.Lzcnt+X64</c>).</summary>
    public readonly string QualifiedName;

    /// <summary>The C++ capability macro the runtime defines per target
    /// (<c>DN2CPP_ISA_X86_Lzcnt_X64</c>) — derived from <see cref="QualifiedName"/>,
    /// checked by <see cref="CoreIntrinsics.CheckPlatformIsaTable"/>.</summary>
    public readonly string Token;

    /// <summary>The generator's verdict: every instruction this facade declares has a
    /// runtime helper, and (for a nested facade) the enclosing facade is lowered too —
    /// <see cref="CoreIntrinsics.CheckPlatformIsaTable"/> rejects a table saying
    /// otherwise. Askers read <see cref="CoreIntrinsics.IsaLowered"/>, which folds the
    /// chain itself so a Release build (where the check is off) cannot miscompile on a
    /// bad row.</summary>
    public readonly bool Lowered;

    /// <summary>The generated helper header relative to the runtime include root, or
    /// null when this facade declares no mapped instruction body.</summary>
    public string? Header { get; internal set; }

    /// <summary>The declaring facade of a nested one (<c>Lzcnt</c> for
    /// <c>Lzcnt+X64</c>), resolved from <see cref="EnclosingName"/> once the whole table
    /// exists; null for a top-level facade.</summary>
    public IsaFamily? Enclosing { get; internal set; }

    internal readonly string? EnclosingName;

    public IsaFamily(IsaArch arch, string qualifiedName, string token, string? enclosing, bool lowered)
    {
        Arch = arch;
        QualifiedName = qualifiedName;
        Token = token;
        EnclosingName = enclosing;
        Lowered = lowered;
    }
}

internal static partial class CoreIntrinsics
{
    private const string IsaNamespacePrefix = "System.Runtime.Intrinsics.";

    /// <summary>Table-order lookup by qualified name, built on first use: the table
    /// lives in another partial file and static initializer order across partial
    /// declarations is unspecified. Ordinal, and the only index over the table —
    /// emitted text depends on table order alone, never on hashing.</summary>
    private static Dictionary<string, IsaFamily>? s_isaByQualifiedName;

    private static Dictionary<string, IsaFamily> IsaIndex
    {
        get
        {
            if (s_isaByQualifiedName is { } built)
                return built;
            var map = new Dictionary<string, IsaFamily>(StringComparer.Ordinal);
            if (s_isaFamilies.Length != s_isaFamilyHeaders.Length)
                throw new InvalidOperationException(
                    $"ISA family/header table length mismatch: {s_isaFamilies.Length} != {s_isaFamilyHeaders.Length}");
            for (int i = 0; i < s_isaFamilies.Length; i++)
            {
                var f = s_isaFamilies[i];
                f.Header = s_isaFamilyHeaders[i];
                map.Add(f.QualifiedName, f);
            }
            foreach (var f in s_isaFamilies)
                if (f.EnclosingName is { } enc && map.TryGetValue(enc, out var parent))
                    f.Enclosing = parent;
            s_isaByQualifiedName = map;
            return map;
        }
    }

    /// <summary>The facade named by a CLR qualified type name
    /// (<c>System.Runtime.Intrinsics.X86.Lzcnt+X64</c>), or null for any other type —
    /// including the ISA enums (<c>FloatComparisonMode</c>) and the portable
    /// <c>Vector64/128/256/512</c> types, which are not facades.</summary>
    public static IsaFamily? PlatformIsaFamily(string qualifiedName) =>
        IsaIndex.TryGetValue(qualifiedName, out var f) ? f : null;

    /// <summary>The C++ expression a lowered facade's <c>IsSupported</c> getter pushes:
    /// the runtime's per-target capability macro.</summary>
    public static string IsaSupportedToken(IsaFamily f) => f.Token;

    /// <summary>Whether the transpiler lowers a facade: its own <see cref="IsaFamily.Lowered"/>
    /// flag AND that of every enclosing facade. Real .NET defines a nested
    /// <c>IsSupported</c> as the enclosing one narrowed (<c>Sse3.X64.IsSupported</c> implies
    /// <c>Sse3.IsSupported</c>), and the runtime token of a nested facade is the enclosing
    /// family's CPU bits — so a nested facade whose enclosing instructions are not lowered
    /// must fold to 0, or a guard like <c>if (Sse3.X64.IsSupported)</c> would pass on SSE3
    /// hardware and reach <c>Sse3</c> instructions that throw.</summary>
    public static bool IsaLowered(IsaFamily f)
    {
        for (var g = f; g is not null; g = g.Enclosing)
            if (!g.Lowered)
                return false;
        return true;
    }

    /// <summary>The compile-time verdict for a facade's <c>IsSupported</c>: constant
    /// false while the facade is not lowered (the guarded arm is dead and
    /// <see cref="BranchLiveness"/> prunes it), null once it is (the runtime token
    /// decides per target, so no branch folds).</summary>
    public static bool? IsaGetterFold(IsaFamily f) => IsaLowered(f) ? null : false;

    /// <summary>The name the contract surface and the runtime map use for a facade:
    /// the qualified name below <c>System.Runtime.Intrinsics.</c> with the nesting
    /// '+' spelled '.' (<c>X86.Lzcnt.X64</c>).</summary>
    public static string IsaContractName(IsaFamily f) =>
        f.QualifiedName.Substring(IsaNamespacePrefix.Length).Replace('+', '.');

    /// <summary>Whether a closed class is a <c>System.ValueTuple`N</c> instantiation (the
    /// multi-register load/store operand and result shape of the ISA facades). Decided
    /// from the definition's metadata name alone — a specialization's own
    /// <see cref="ClassInfo.Name"/> is mangled, and its shape flags are not populated
    /// before reachability, which the surface dump runs without.</summary>
    public static bool IsIsaValueTuple(ClassInfo cls) =>
        cls.Context.TypeArgs.Length >= 2
        && cls.Module.Owner.GenericDefFullName(cls) == "System.ValueTuple";

    /// <summary>The C++ helper that lowers one instruction overload:
    /// <c>dn2cpp_isa_&lt;arch&gt;_&lt;typepath&gt;_&lt;method&gt;[_&lt;argsig&gt;]</c>, all
    /// lowercase — <c>dn2cpp_isa_x86_sse2_add_v128i32_v128i32</c>,
    /// <c>dn2cpp_isa_x86_lzcnt_x64_leadingzerocount_u64</c>,
    /// <c>dn2cpp_isa_x86_x86base_pause</c>. A ValueTuple parameter (the multi-register
    /// store operand) is <c>t&lt;arity&gt;&lt;item code&gt;</c> — <c>t2v64i8</c> — and the
    /// helper takes its items as consecutive parameters; every such tuple in CoreLib is
    /// homogeneous, and a heterogeneous one is refused rather than given a spelling the
    /// generator does not share. The return type is never encoded (.NET overloads never
    /// differ by it alone). A parameter shape the contract does not name throws, naming
    /// the member: a silently invented code would let the transpiler and the runtime spell
    /// one helper two ways.</summary>
    public static string IsaHelperName(IsaFamily f, string method, MethodSignature<TypeDesc> sig)
    {
        var sb = new System.Text.StringBuilder("dn2cpp_isa_");
        sb.Append(f.Arch switch
        {
            IsaArch.X86 => "x86",
            IsaArch.Arm => "arm",
            IsaArch.Wasm => "wasm",
            _ => throw new InvalidOperationException($"unknown ISA arch {f.Arch}"),
        });
        sb.Append('_');
        // <typepath>: the qualified name below the arch namespace, lowercased, '+' -> '_'.
        string contract = IsaContractName(f);
        sb.Append(contract.Substring(contract.IndexOf('.') + 1).Replace('.', '_').ToLowerInvariant());
        sb.Append('_');
        sb.Append(method.ToLowerInvariant());
        foreach (var p in sig.ParameterTypes)
        {
            sb.Append('_');
            sb.Append(IsaArgCode(f, method, p));
        }
        return sb.ToString();
    }

    /// <summary>The smallest runtime header that defines an emitted helper. Public
    /// facade methods use their generated family header. CoreLib's known non-public
    /// X86Base residue uses its small hand-written header; any future hand-written
    /// helper falls back to the umbrella, preserving correctness until it is classified.</summary>
    public static string IsaHelperHeader(IsaFamily f, MethodInfo callee, string helper)
    {
        if (callee.IsPublic)
            return f.Header ?? "isa/dn2cpp_isa.h";
        return helper switch
        {
            "dn2cpp_isa_x86_x86base_bitscanforward_u32"
                or "dn2cpp_isa_x86_x86base_bitscanreverse_u32"
                or "dn2cpp_isa_x86_x86base_x64_bitscanforward_u64"
                or "dn2cpp_isa_x86_x86base_x64_bitscanreverse_u64"
                or "dn2cpp_isa_x86_x86base_x64_bigmul_u64_u64"
                or "dn2cpp_isa_x86_x86base_x64_bigmul_i64_i64"
                    => "isa/dn2cpp_isa_bcl_internal.h",
            _ => "isa/dn2cpp_isa.h",
        };
    }

    private static string IsaArgCode(IsaFamily f, string method, TypeDesc t)
    {
        switch (t.Kind)
        {
            case TypeKind.Primitive:
                return IsaPrimitiveCode(f, method, t.Primitive);
            case TypeKind.Pointer:
                return t.Element is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void }
                    ? "pvoid"
                    : "p" + IsaArgCode(f, method, t.Element!);
            case TypeKind.ByRef:
                return "r" + IsaArgCode(f, method, t.Element!);
            case TypeKind.Class when t.Class is { IsEnum: true } e:
                return IsaPrimitiveCode(f, method, e.EnumUnderlying);
            case TypeKind.Class when t.Class is { } tup && IsIsaValueTuple(tup):
            {
                var items = tup.Context.TypeArgs;
                string code = IsaArgCode(f, method, items[0]);
                if (!code.StartsWith('v'))
                    throw new NotSupportedException(
                        $"{IsaContractName(f)}.{method}: tuple parameter items must be vectors, not {items[0]}");
                for (int i = 1; i < items.Length; i++)
                    if (IsaArgCode(f, method, items[i]) != code)
                        throw new NotSupportedException(
                            $"{IsaContractName(f)}.{method}: heterogeneous tuple parameter {t} has no ISA helper-name code");
                return "t" + items.Length + code;
            }
            case TypeKind.Class when t.Class is { IntrinsicCppName: { } icn } v
                && v.Context.TypeArgs.Length == 1:
            {
                string? width = icn switch
                {
                    "Dn2CppVector64" => "v64",
                    "Dn2CppVector128" => "v128",
                    "Dn2CppVector256" => "v256",
                    "Dn2CppVector512" => "v512",
                    _ => null,
                };
                if (width is not null)
                    return width + IsaArgCode(f, method, v.Context.TypeArgs[0]);
                break;
            }
        }
        throw new NotSupportedException(
            $"{IsaContractName(f)}.{method}: parameter type {t} has no ISA helper-name code");
    }

    /// <summary>The C++ spelling the generator gives a helper parameter or tuple item
    /// where it differs from the transpiler's own: a scalar or enum at its precise width
    /// and sign (<c>uint8_t</c>, <c>uintptr_t</c> for <c>nuint</c>; the transpiler spells
    /// those <c>int32_t</c> and <c>intptr_t</c>), a pointer or byref as a pointer to
    /// that (the transpiler's pointer is <c>void*</c>). Null for a shape the two already
    /// spell alike (a vector) or that the contract does not name.</summary>
    public static string? IsaHelperCpp(TypeDesc t)
    {
        switch (t.Kind)
        {
            case TypeKind.Primitive:
                return CppTypes.NativeAbiType(t);
            case TypeKind.Class when t.Class is { IsEnum: true }:
                return CppTypes.NativeAbiType(t);
            case TypeKind.Pointer:
                if (t.Element is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void })
                    return "void*";
                return IsaHelperCpp(t.Element!) is { } pe ? pe + "*" : null;
            case TypeKind.ByRef:
                return IsaHelperCpp(t.Element!) is { } re ? re + "*" : null;
            default:
                return null;
        }
    }

    private static string IsaPrimitiveCode(IsaFamily f, string method, PrimitiveTypeCode pc) => pc switch
    {
        PrimitiveTypeCode.SByte => "i8",
        PrimitiveTypeCode.Byte => "u8",
        PrimitiveTypeCode.Int16 => "i16",
        PrimitiveTypeCode.UInt16 => "u16",
        PrimitiveTypeCode.Int32 => "i32",
        PrimitiveTypeCode.UInt32 => "u32",
        PrimitiveTypeCode.Int64 => "i64",
        PrimitiveTypeCode.UInt64 => "u64",
        PrimitiveTypeCode.Single => "f32",
        PrimitiveTypeCode.Double => "f64",
        PrimitiveTypeCode.IntPtr => "nint",
        PrimitiveTypeCode.UIntPtr => "nuint",
        PrimitiveTypeCode.Boolean => "bool",
        PrimitiveTypeCode.Char => "char",
        _ => throw new NotSupportedException(
            $"{IsaContractName(f)}.{method}: primitive {pc} has no ISA helper-name code"),
    };

    /// <summary>The token a qualified facade name derives to — the same spelling the
    /// generator writes, so the table check can prove the two agree.</summary>
    private static string IsaTokenFor(string qualifiedName) =>
        "DN2CPP_ISA_" + qualifiedName.Substring(IsaNamespacePrefix.Length).Replace('.', '_').Replace('+', '_');

    /// <summary>Structural invariants of the generated table, checked once per transpile
    /// from <see cref="VerifyInterceptRegistry"/>: every row lives under the ISA
    /// namespace, its token is derived from its qualified name, tokens are unique, every
    /// nested row names an enclosing row that exists and declares it, and a nested row is
    /// lowered only when its enclosing row is (real .NET defines the nested
    /// <c>IsSupported</c> as the enclosing one narrowed, and the runtime token of a nested
    /// facade is the enclosing family's CPU bits).</summary>
    public static void CheckPlatformIsaTable()
    {
        _ = IsaIndex;
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < s_isaFamilies.Length; i++)
        {
            var f = s_isaFamilies[i];
            if (!f.QualifiedName.StartsWith(IsaNamespacePrefix, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"ISA table[{i}] {f.QualifiedName}: not under {IsaNamespacePrefix}");
            if (f.Token != IsaTokenFor(f.QualifiedName))
                throw new InvalidOperationException(
                    $"ISA table[{i}] {f.QualifiedName}: token {f.Token} is not derived from the "
                    + $"qualified name (expected {IsaTokenFor(f.QualifiedName)})");
            if (!tokens.Add(f.Token))
                throw new InvalidOperationException($"ISA table[{i}] {f.QualifiedName}: duplicate token {f.Token}");
            if (f.EnclosingName is { } enc)
            {
                if (f.Enclosing is null)
                    throw new InvalidOperationException(
                        $"ISA table[{i}] {f.QualifiedName}: enclosing facade {enc} is not in the table");
                if (f.Enclosing.QualifiedName + "+" != f.QualifiedName.Substring(0, f.QualifiedName.LastIndexOf('+') + 1))
                    throw new InvalidOperationException(
                        $"ISA table[{i}] {f.QualifiedName}: enclosing facade {enc} does not declare it");
                if (f.Lowered && !f.Enclosing.Lowered)
                    throw new InvalidOperationException(
                        $"ISA table[{i}] {f.QualifiedName}: lowered while its enclosing facade {enc} is not");
            }
            else if (f.QualifiedName.Contains('+'))
            {
                throw new InvalidOperationException(
                    $"ISA table[{i}] {f.QualifiedName}: nested name with no enclosing facade");
            }
        }
    }
}
