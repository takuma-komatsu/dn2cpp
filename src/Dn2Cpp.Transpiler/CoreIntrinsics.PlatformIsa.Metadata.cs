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

}
