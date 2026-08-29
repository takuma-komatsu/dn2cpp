// gen-isa-map.cs — regenerates the hardware-intrinsics family contract from the public
// surface of System.Private.CoreLib. Outputs, all checked in:
//
//   runtime/core/isa/dn2cpp_isa_tokens.g.h                  one IsSupported token per family
//   runtime/core/isa/dn2cpp_isa_families.g.h                includes every generated family header
//   runtime/core/isa/dn2cpp_isa_manifest.txt                every lowered helper name, sorted
//   runtime/core/isa/<arch>/dn2cpp_isa_<arch>_<family>.h    one per family that has map rows
//   src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs   the family table the transpiler reads
//   samples/dotnet/PlatformIsaProbe/Exercises.g.cs          one exercise per generated-lowered family
//
//   Run from the repository root:
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll> [--check] [--root <repo>]
//                                                    [--lowered-preview <Arch.Family,...>]
//
// A .NET 10 file-based program, like gen-culture-table: a manual aid, not a build step, not
// transpiled by the self-host, and referenced by nothing the CLI ships. `--check` regenerates
// in memory and fails when any checked-in output differs byte for byte, so a gate can prove
// the outputs are current without giving the generator write access to the tree.
//
// The oracle is CoreLib METADATA, never method bodies: the public static surface of the
// System.Runtime.Intrinsics.{X86,Arm,Wasm} families is identical across the x64 and arm64
// flavours of CoreLib (the foreign-arch flavour ships PlatformNotSupported bodies behind the
// same signatures), so any host's CoreLib of the pinned version produces the same outputs.
//
// families.csv is the human-owned side of the contract: which families exist, which feature
// bits each requires, and the landing order. The generator asserts that its family set equals
// the metadata set, so a CoreLib update that adds or removes a family fails here first. The
// feature bits and their implications are read from runtime/core/dn2cpp_cpu_features.h
// (the DN2CPP_CPU_FEATURE_TABLE rows), the one place they are defined.
//
// `Lowered` is derived, never edited: a family is lowered when every public static method it
// declares has a map row AND every family it implies is lowered — the families whose bits lie
// in the implication closure of its own bits, which covers the enclosing type (a nested type
// carries its enclosing type's bits) and .NET's instruction-set implications (Dp implies
// AdvSimd). Otherwise Dp.IsSupported could answer true while AdvSimd.IsSupported is the
// constant 0, a state .NET never has, and BCL code guarded by Dp would reach AdvSimd calls
// that throw. A family with no methods of its own is vacuously covered; a family with no
// feature bits (Sve, Sve2) is never lowered, whatever its maps say.
//
// `--lowered-preview` names families to treat as covered while their maps are incomplete: the
// unmapped methods get throwing native stubs, and the exercise file exercises the mapped ones,
// so a partial map can be run against real .NET before the family is complete. Every output
// carries a PREVIEW banner; the mode refuses --check and is never a checked-in state.
//
// MAP GRAMMAR — tools/gen-isa-map/map/<arch>/<familypath>.map, or a directory
// map/<arch>/<familypath>/ of *.map files read in ordinal name order, where <familypath> is the
// lowercase type path below the arch namespace with '+' as '_' (sse2.map, sse2_x64.map,
// avx512f_vl.map, advsimd/arithmetic.map, packedsimd.map).
//
//   # comment                                blank lines and '#' lines are ignored
//   target = sse4.2                          family-level default for DN2CPP_ISA_TARGET(...)
//   derive = X86.Avx512F.VL, X86.Avx512BW.VL the rows of the listed families for every method this
//                                            family shares with them (first source wins, an explicit
//                                            row in this file wins over all), under this file's target;
//                                            a method no source covers is an error
//   Method(codes) [@ann ...] = expression    exact row: codes are the helper-name argument codes
//   Method(v{W}{T},v{W}{T}) = vadd{q}_{neon}($0,$1) for W in 64,128 for T in i8,u8
//                                            pattern row: repeated per width W and element T;
//                                            {…} placeholders are spelled per combination by the
//                                            tables in Maps.Substitute, in the codes, the
//                                            annotations and the expression alike
//
//   In the expression, $0 $1 ... name the parameters. A vector parameter arrives converted to
//   the arch's native vector type (dn2cpp_isa_bits<...>); `$k:u8` converts it to the same-width
//   vector of another element instead. A vector return is wrapped back (dn2cpp_isa_vec<...>).
//   $k.j names item j (1-based) of a tuple parameter k, `$k.*` the whole tuple as the NEON
//   multi-register aggregate (int8x16x2_t); $r1 $r2 ... name the out-pointer items of a tuple
//   return, already dereferenced, &$r1 &$r2 ... the out-pointers themselves and `&$r*` all of
//   them in order. Scalars and pointers pass through unchanged.
//
//   Annotations, written between the closing ')' and '=':
//     @imm8            the LAST parameter is an immediate in [0..256); the body becomes
//                      DN2CPP_ISA_IMM8_SWITCH(<param>, <expression>) and the expression refers
//                      to the immediate as DN2CPP_IMM (its $k is rewritten to that name)
//     @imm[lo..hi)     same, valid in [lo, hi) — or [lo..hi] inclusive — for a count that is a
//                      power of two up to 256, through DN2CPP_ISA_IMM_RANGE_SWITCH
//     @imm$k[lo..hi)   the immediate is parameter k; two such annotations make a two-immediate
//                      helper (DN2CPP_IMM and DN2CPP_IMM2 in parameter order, at most 256 cases)
//     @imm{1,2,4,8}    the immediate takes the listed values only (a gather scale), through one
//                      switch case per value; `@imm$k{...}` names the parameter
//     @target("isa")   per-row DN2CPP_ISA_TARGET override of the family-level `target =`
//     @throws          documentation only: the intrinsic can raise (fault, #UD); no code effect
//     @ref(<C#>)       a portable System.Runtime.Intrinsics expression over $0, $1, ... that
//                      computes the same result; the generated exercise prints ` ref=OK` or
//                      ` ref=MISMATCH(<reference>)` beside the helper's bytes. The portable
//                      layer is an independent implementation, which is what checks a family
//                      whose gate output is frozen because no host .NET can be its oracle.
//
// Every helper is emitted inside its target and compiler-capability `#if`
// (Contract.HelperCondition; wasm also tests __wasm_simd128__, since SIMD is a property of the
// whole module) with the real body — which first tests the family's IsSupported token through
// dn2cpp_isa_require, throwing PlatformNotSupportedException as .NET does for a call made while
// IsSupported is false — and `#else` with a [[noreturn]] stub calling
// dn2cpp_isa_not_lowered("<QualifiedName>.<Method>"), so a foreign-arch dead arm in generated
// code still compiles against a declaration.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

string? corelibPath = null;
string root = Directory.GetCurrentDirectory();
bool check = false;
string[] preview = Array.Empty<string>();
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--corelib" when i + 1 < args.Length:
            corelibPath = args[++i];
            break;
        case "--root" when i + 1 < args.Length:
            root = Path.GetFullPath(args[++i]);
            break;
        case "--check":
            check = true;
            break;
        case "--lowered-preview" when i + 1 < args.Length:
            preview = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            break;
        default:
            Console.Error.WriteLine($"error: unknown or incomplete argument '{args[i]}'");
            Console.Error.WriteLine("usage: dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll> [--check] [--root <repo>] [--lowered-preview <Arch.Family,...>]");
            return 2;
    }
}
if (corelibPath is null || !File.Exists(corelibPath))
{
    Console.Error.WriteLine("error: --corelib <System.Private.CoreLib.dll> is required and must exist");
    return 2;
}
if (check && preview.Length > 0)
{
    Console.Error.WriteLine("error: --lowered-preview writes a preview tree and cannot be combined with --check");
    return 2;
}
string toolDir = Path.Combine(root, "tools", "gen-isa-map");
if (!File.Exists(Path.Combine(toolDir, "families.csv")))
{
    Console.Error.WriteLine($"error: {Path.Combine(toolDir, "families.csv")} not found — run from the repository root or pass --root");
    return 2;
}

try
{
    var cpu = CpuFeatures.Read(Path.Combine(root, "runtime", "core", "dn2cpp_cpu_features.h"));
    var families = Csv.Read(Path.Combine(toolDir, "families.csv"), cpu);
    using var pe = new PEReader(File.OpenRead(corelibPath));
    var md = pe.GetMetadataReader();
    Surface.Populate(md, families);
    Maps.Apply(toolDir, families);
    Emit.Preview = preview.Length > 0 ? string.Join(",", preview) : null;
    foreach (string name in preview)
    {
        var f = families.FirstOrDefault(x => Contract.Display(x.QualifiedName) == name)
            ?? throw new ContractException($"--lowered-preview: no family is named '{name}' (use the display name, e.g. Arm.AdvSimd)");
        if (f.NeverLowered)
            throw new ContractException($"--lowered-preview: {name} has no feature bits and is never lowered");
        f.Preview = true;
    }
    Lowering.Settle(families, cpu);

    var outputs = Emit.All(families);
    // The per-arch directories hold nothing but generated family headers, so any header no
    // family produces is stale output from a removed map.
    var stale = Contract.Archs.SelectMany(a =>
            {
                string d = Path.Combine(root, "runtime", "core", "isa", a);
                return Directory.Exists(d)
                    ? Directory.GetFiles(d, "dn2cpp_isa_*.h").Select(f => $"runtime/core/isa/{a}/{Path.GetFileName(f)}")
                    : Enumerable.Empty<string>();
            })
            .Where(rel => !outputs.ContainsKey(rel))
            .OrderBy(rel => rel, StringComparer.Ordinal)
            .ToList();

    if (check)
    {
        var problems = new List<string>();
        foreach (var (rel, text) in outputs.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            string path = Path.Combine(root, rel);
            if (!File.Exists(path))
            {
                problems.Add($"missing   {rel}");
                continue;
            }
            // Outputs are written with '\n'; a checkout that rewrote them to CRLF is
            // still current, so the comparison reads line endings as '\n' too.
            string actual = File.ReadAllText(path, Emit.Utf8NoBom).Replace("\r\n", "\n");
            if (!string.Equals(actual, text, StringComparison.Ordinal))
                problems.Add($"differs   {rel} (first difference at line {FirstDifferingLine(actual, text)})");
        }
        foreach (string rel in stale)
            problems.Add($"stale     {rel} (no family produces it)");
        if (problems.Count > 0)
        {
            Console.Error.WriteLine("error: generated ISA outputs are out of date; regenerate with");
            Console.Error.WriteLine("  dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>");
            foreach (string p in problems)
                Console.Error.WriteLine("  " + p);
            return 1;
        }
        Console.Error.WriteLine("check passed: every generated ISA output is current");
    }
    else
    {
        foreach (var (rel, text) in outputs)
        {
            string path = Path.Combine(root, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text, Emit.Utf8NoBom);
        }
        foreach (string rel in stale)
            File.Delete(Path.Combine(root, rel));
        Console.Error.WriteLine($"wrote {outputs.Count} file(s) under {root}" + (stale.Count > 0 ? $", deleted {stale.Count} stale family header(s)" : ""));
    }

    Report(corelibPath, families);
    return 0;
}
catch (ContractException e)
{
    Console.Error.WriteLine("error: " + e.Message);
    return 2;
}

static int FirstDifferingLine(string a, string b)
{
    string[] la = a.Split('\n');
    string[] lb = b.Split('\n');
    int n = Math.Min(la.Length, lb.Length);
    for (int i = 0; i < n; i++)
    {
        if (!string.Equals(la[i], lb[i], StringComparison.Ordinal))
            return i + 1;
    }
    return n + 1;
}

static void Report(string corelibPath, List<Family> families)
{
    string version = FileVersionInfo.GetVersionInfo(corelibPath).ProductVersion ?? "unknown";
    int methods = families.Sum(f => f.Methods.Count);
    int lowerable = families.Where(f => !f.NeverLowered).Sum(f => f.Methods.Count);
    int mapped = families.Sum(f => f.Methods.Count(m => m.Row is not null));
    var lowered = families.Where(f => f.Lowered).Select(f => Contract.Display(f.QualifiedName)).ToList();
    // Families whose own maps are complete but which imply an uncovered family.
    var held = families.Where(f => f.Covered && !f.Lowered && f.HasRows)
        .Select(f => $"{Contract.Display(f.QualifiedName)} (implies {string.Join(", ", f.Implied.Where(g => !g.Covered).Select(g => Contract.Display(g.QualifiedName)))})")
        .ToList();
    Console.Error.WriteLine($"  corelib    {version}");
    Console.Error.WriteLine($"  families   {families.Count} ({families.Count(f => f.NeverLowered)} never lowered)");
    Console.Error.WriteLine($"  methods    {methods} ({lowerable} lowerable, {mapped} mapped)");
    Console.Error.WriteLine($"  lowered    {lowered.Count} family(ies), {families.Count(f => f.HasRows)} with helper headers");
    if (lowered.Count > 0)
        Console.Error.WriteLine($"             {string.Join(" ", lowered)}");
    foreach (string h in held)
        Console.Error.WriteLine($"  covered, not lowered: {h}");
    if (Emit.Preview is not null)
        Console.Error.WriteLine($"  PREVIEW    {Emit.Preview}: unmapped methods stubbed; not a checked-in state");
}

// ---------------------------------------------------------------------------------------
// The contract's fixed vocabulary.
// ---------------------------------------------------------------------------------------

static class Contract
{
    public const string IntrinsicsPrefix = "System.Runtime.Intrinsics.";
    public static readonly string[] Archs = { "x86", "arm", "wasm" };

    public static string Namespace(string arch) => arch switch
    {
        "x86" => IntrinsicsPrefix + "X86",
        "arm" => IntrinsicsPrefix + "Arm",
        "wasm" => IntrinsicsPrefix + "Wasm",
        _ => throw new ContractException($"unknown arch '{arch}'"),
    };

    public static string IsaArch(string arch) => arch switch
    {
        "x86" => "X86",
        "arm" => "Arm",
        _ => "Wasm",
    };

    public static string TargetMacro(string arch) => arch switch
    {
        "x86" => "DN2CPP_TARGET_X64",
        "arm" => "DN2CPP_TARGET_ARM64",
        _ => "DN2CPP_TARGET_WASM32",
    };

    // The compiler capability is part of IsSupported: a mapped family cannot answer true when
    // the compiler that consumes the generated headers has no spelling for its instructions.
    // Families whose bit set includes AVX10V2 share that capability with their nested V512
    // surfaces, including the V512 AvxVnniInt families whose instructions AVX10.2 introduces.
    public static string CompilerCondition(Family family) =>
        family.Bits.Any(bit => bit is "X86_AVX10V2" or "X86_AVX10V2_512")
            ? "DN2CPP_HAS_X86_AVX10V2_INTRINSICS"
            : "1";

    // The `#if` a helper's real body sits under. wasm SIMD is a module-wide property
    // (-msimd128, DN2CPP_WASM_SIMD), while a compiler capability selects either the mapped
    // body or the same throwing stub used on a foreign architecture.
    public static string HelperCondition(Family family)
    {
        string target = family.Arch == "wasm"
            ? TargetMacro(family.Arch) + " && defined(__wasm_simd128__)"
            : TargetMacro(family.Arch);
        string compiler = CompilerCondition(family);
        return compiler == "1" ? target : target + " && " + compiler;
    }

    public static string Token(string qualifiedName) =>
        "DN2CPP_ISA_" + qualifiedName.Substring(IntrinsicsPrefix.Length).Replace('.', '_').Replace('+', '_');

    // The display name the gates and the probe use: X86.Lzcnt.X64.
    public static string Display(string qualifiedName) =>
        qualifiedName.Substring(IntrinsicsPrefix.Length).Replace('+', '.');

    // The type path below the arch namespace: "Sse2+X64" for System.Runtime.Intrinsics.X86.Sse2+X64.
    public static string TypePath(string qualifiedName, string arch) =>
        qualifiedName.Substring(Namespace(arch).Length + 1);

    public static string HelperTypePath(string typePath) => typePath.ToLowerInvariant().Replace('+', '_');
}

sealed class ContractException : Exception
{
    public ContractException(string message) : base(message) { }
}

// ---------------------------------------------------------------------------------------
// runtime/core/dn2cpp_cpu_features.h: the feature bits and what each implies.
// ---------------------------------------------------------------------------------------

sealed class CpuFeatures
{
    // X(ID, "Name", ARCH, parentsMask) rows of DN2CPP_CPU_FEATURE_TABLE; the mask is `0` or
    // DN2CPP_CPU_<ID> terms joined by '|'.
    static readonly Regex XRow = new(@"^\s*X\(([A-Z][A-Z0-9_]*),\s*""[^""]*"",\s*[A-Z0-9]+,\s*([^)]*)\)\s*\\?\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    public readonly Dictionary<string, string[]> Parents = new(StringComparer.Ordinal);

    public static CpuFeatures Read(string path)
    {
        if (!File.Exists(path))
            throw new ContractException($"{path} not found — the feature bits and their implications are read from it");
        var cpu = new CpuFeatures();
        foreach (Match m in XRow.Matches(File.ReadAllText(path)))
        {
            string id = m.Groups[1].Value;
            string mask = m.Groups[2].Value.Trim();
            string[] parents = mask == "0"
                ? Array.Empty<string>()
                : mask.Split('|').Select(t => t.Trim()).Select(t =>
                    t.StartsWith("DN2CPP_CPU_", StringComparison.Ordinal)
                        ? t.Substring("DN2CPP_CPU_".Length)
                        : throw new ContractException($"{path}: parent '{t}' of {id} is not a DN2CPP_CPU_ enumerator")).ToArray();
            if (!cpu.Parents.TryAdd(id, parents))
                throw new ContractException($"{path}: duplicate feature bit {id}");
        }
        if (cpu.Parents.Count == 0)
            throw new ContractException($"{path}: no DN2CPP_CPU_FEATURE_TABLE rows found");
        foreach (var (id, parents) in cpu.Parents)
        {
            foreach (string p in parents)
            {
                if (!cpu.Parents.ContainsKey(p))
                    throw new ContractException($"{path}: {id} names parent {p}, which is not a feature bit");
            }
        }
        return cpu;
    }

    // The bits and, transitively, every bit they imply.
    public HashSet<string> Closure(IEnumerable<string> bits)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        var work = new Stack<string>(bits);
        while (work.Count > 0)
        {
            string b = work.Pop();
            if (!set.Add(b))
                continue;
            foreach (string p in Parents[b])
                work.Push(p);
        }
        return set;
    }
}

// ---------------------------------------------------------------------------------------
// Model.
// ---------------------------------------------------------------------------------------

sealed class Family
{
    public required string QualifiedName;
    public required string Arch;
    public required string[] Bits;
    public required int Group;
    public string Token => Contract.Token(QualifiedName);
    public string TypePath => Contract.TypePath(QualifiedName, Arch);
    public string HelperPath => Contract.HelperTypePath(TypePath);
    public string? Enclosing => QualifiedName.Contains('+') ? QualifiedName.Substring(0, QualifiedName.LastIndexOf('+')) : null;
    public bool NeverLowered => Bits.Length == 0;
    public List<Method> Methods = new();
    public string? Target;
    public string? TargetSource;
    // The families whose rows this one takes for the methods it shares with them (the
    // `derive =` directive), in precedence order; null for a family mapped directly.
    public string[]? Derive;
    public string? DeriveSource;
    public bool Preview;
    public bool HasRows => Methods.Any(m => m.Row is not null) || (Preview && Methods.Count > 0);
    public bool Covered => !NeverLowered && (Preview || Methods.All(m => m.Row is not null));
    public bool Lowered;
    // The families this one implies: every family whose bits lie in the closure of this one's.
    public List<Family> Implied = new();
    public string HeaderRelPath => $"runtime/core/isa/{Arch}/dn2cpp_isa_{Arch}_{HelperPath}.h";
}

sealed class Method
{
    public required string Name;
    public required Ty Return;
    public required ImmutableArray<Ty> Params;
    public required string HelperName;
    public required string Key;   // Name(code,code,...) — what a map row is matched by
    public MapRow? Row;
}

// One immediate parameter: valid values are [Lo, Lo + Count), or the listed Values (a gather
// scale is 1, 2, 4 or 8 and nothing between).
sealed record ImmSpec(int Param, int Lo, int Count, ImmutableArray<int> Values = default)
{
    public bool IsSet => !Values.IsDefault;
    // The value the exercise passes: the middle of the range, or the middle listed value.
    public int Exercised => IsSet ? Values[Values.Length / 2] : Lo + (Count - 1) / 2;
    // A byte one past the range, for the exercise that witnesses the range check; null when
    // every byte is valid.
    public int? OutOfRange
    {
        get
        {
            int v = IsSet ? Values[Values.Length - 1] + 1 : Lo + Count;
            return v <= 255 ? v : null;
        }
    }
}

// Ref is the row's portable C# cross-check (@ref), spelled over the exercise's operands.
sealed record MapRow(string Expression, string? Target, ImmutableArray<ImmSpec> Imms, string? Ref, string SourceLine);

static class Lowering
{
    // Lowered starts as Covered and only ever falls: a family drops when any family it implies
    // is not lowered, until nothing changes. Families with equal bits (a type and its X64 /
    // Arm64 nested type) imply each other and so are lowered together or not at all.
    public static void Settle(List<Family> families, CpuFeatures cpu)
    {
        foreach (var f in families)
        {
            f.Lowered = f.Covered;
            if (f.NeverLowered)
                continue;
            var closure = cpu.Closure(f.Bits);
            f.Implied = families.Where(g => g != f && !g.NeverLowered && g.Bits.All(closure.Contains)).ToList();
        }
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var f in families)
            {
                if (f.Lowered && f.Implied.Any(g => !g.Lowered))
                {
                    f.Lowered = false;
                    changed = true;
                }
            }
        }
    }
}

enum TyKind { Void, Scalar, Vector, Pointer, ByRef, Tuple, VectorDef, TupleDef, Unsupported }

// A decoded signature type. Scalar carries the helper-name code and the C++ spelling (and,
// for an enum, the enum's C# name so an exercise can spell the argument); Vector carries the
// width in bits and its scalar element; Pointer/ByRef wrap an element (null for void*); Tuple
// carries its items. VectorDef/TupleDef are the open generic markers the signature provider
// hands back before instantiation, and Unsupported names what it could not model so the
// failure message points at the offending member.
sealed record Ty(TyKind Kind, string Code = "", string Cpp = "", int Bits = 0, Ty? Elem = null, ImmutableArray<Ty> Items = default, string Describe = "", string? CsEnum = null)
{
    public static readonly Ty Void = new(TyKind.Void, Describe: "void");
    public static Ty Scalar(string code, string cpp) => new(TyKind.Scalar, code, cpp, Describe: code);
    public static Ty Unsupported(string what) => new(TyKind.Unsupported, Describe: what);
    public static Ty Vector(int bits, string elem) => new(TyKind.Vector, Bits: bits, Elem: Codes.ScalarOf(elem));

    public override string ToString() => Kind switch
    {
        TyKind.Vector => $"v{Bits}{Elem!.Code}",
        TyKind.Pointer => Elem is null ? "void*" : Elem + "*",
        TyKind.ByRef => "ref " + Elem,
        TyKind.Tuple => "(" + string.Join(", ", Items) + ")",
        _ => Describe,
    };
}

// ---------------------------------------------------------------------------------------
// families.csv
// ---------------------------------------------------------------------------------------

static class Csv
{
    public static List<Family> Read(string path, CpuFeatures cpu)
    {
        var rows = File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .ToList();
        if (rows.Count == 0 || rows[0] != "qualified_name,arch,bits,landing_order_group")
            throw new ContractException($"{path}: first non-comment line must be the header 'qualified_name,arch,bits,landing_order_group'");

        var families = new List<Family>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string line in rows.Skip(1))
        {
            string[] c = line.Split(',');
            if (c.Length != 4)
                throw new ContractException($"{path}: expected 4 columns: '{line}'");
            string name = c[0];
            string arch = c[1];
            if (!Contract.Archs.Contains(arch))
                throw new ContractException($"{path}: arch '{arch}' must be x86, arm or wasm: '{line}'");
            if (!name.StartsWith(Contract.Namespace(arch) + ".", StringComparison.Ordinal))
                throw new ContractException($"{path}: '{name}' is not in the {arch} namespace {Contract.Namespace(arch)}");
            string[] bits = c[2].Length == 0 ? Array.Empty<string>() : c[2].Split('|');
            foreach (string b in bits)
            {
                if (!cpu.Parents.ContainsKey(b))
                    throw new ContractException($"{path}: feature bit '{b}' in '{line}' is not a DN2CPP_CPU_FEATURE_TABLE row");
                if (!b.StartsWith(arch.ToUpperInvariant() + "_", StringComparison.Ordinal))
                    throw new ContractException($"{path}: feature bit '{b}' belongs to another arch in '{line}'");
            }
            if (!int.TryParse(c[3], NumberStyles.None, CultureInfo.InvariantCulture, out int group))
                throw new ContractException($"{path}: landing_order_group must be an integer: '{line}'");
            if (!seen.Add(name))
                throw new ContractException($"{path}: duplicate family '{name}'");
            families.Add(new Family { QualifiedName = name, Arch = arch, Bits = bits, Group = group });
        }

        CheckOrder(path, families);
        return families;
    }

    // Row order is the contract order the gates print, so it must follow one rule rather than
    // taste: a nested type sits immediately after its enclosing type's earlier nested subtree,
    // siblings in ordinal order of the nested name, and a nested type's group is its parent's.
    static void CheckOrder(string path, List<Family> families)
    {
        var byName = families.ToDictionary(f => f.QualifiedName, StringComparer.Ordinal);
        for (int i = 0; i < families.Count; i++)
        {
            var f = families[i];
            if (f.Enclosing is null)
                continue;
            if (!byName.TryGetValue(f.Enclosing, out var parent))
                throw new ContractException($"{path}: '{f.QualifiedName}' has no row for its enclosing type '{f.Enclosing}'");
            if (parent.Group != f.Group)
                throw new ContractException($"{path}: '{f.QualifiedName}' must share landing_order_group with '{f.Enclosing}'");
            int p = families.IndexOf(parent);
            if (p > i)
                throw new ContractException($"{path}: '{f.QualifiedName}' must follow its enclosing type '{f.Enclosing}'");
            // Everything between the parent and this row must belong to the parent's subtree,
            // and the preceding sibling (if any) must sort before this one.
            string? prevSibling = null;
            for (int k = p + 1; k < i; k++)
            {
                string between = families[k].QualifiedName;
                if (!between.StartsWith(f.Enclosing + "+", StringComparison.Ordinal))
                    throw new ContractException($"{path}: '{between}' interrupts the nested types of '{f.Enclosing}' before '{f.QualifiedName}'");
                if (families[k].Enclosing == f.Enclosing)
                    prevSibling = between;
            }
            if (prevSibling is not null && string.CompareOrdinal(prevSibling, f.QualifiedName) >= 0)
                throw new ContractException($"{path}: nested types of '{f.Enclosing}' must be in ordinal order ('{prevSibling}' before '{f.QualifiedName}')");
        }
    }
}

// ---------------------------------------------------------------------------------------
// CoreLib metadata.
// ---------------------------------------------------------------------------------------

static class Surface
{
    public static void Populate(MetadataReader md, List<Family> families)
    {
        var found = new Dictionary<string, TypeDefinitionHandle>(StringComparer.Ordinal);
        foreach (var h in md.TypeDefinitions)
        {
            var td = md.GetTypeDefinition(h);
            if (!td.GetDeclaringType().IsNil)
                continue;
            string ns = md.GetString(td.Namespace);
            if (ns != Contract.Namespace("x86") && ns != Contract.Namespace("arm") && ns != Contract.Namespace("wasm"))
                continue;
            Collect(md, h, ns + "." + md.GetString(td.Name), found);
        }

        var csvSet = families.Select(f => f.QualifiedName).ToHashSet(StringComparer.Ordinal);
        var onlyCsv = csvSet.Except(found.Keys).OrderBy(n => n, StringComparer.Ordinal).ToList();
        var onlyMd = found.Keys.Except(csvSet).OrderBy(n => n, StringComparer.Ordinal).ToList();
        if (onlyCsv.Count > 0 || onlyMd.Count > 0)
        {
            var sb = new StringBuilder("families.csv and CoreLib disagree on the family set");
            foreach (string n in onlyCsv)
                sb.Append("\n  only in families.csv: ").Append(n);
            foreach (string n in onlyMd)
                sb.Append("\n  only in CoreLib:      ").Append(n);
            throw new ContractException(sb.ToString());
        }

        var provider = new TyProvider();
        var helpers = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var f in families)
        {
            f.Methods = Decode(md, provider, found[f.QualifiedName], f);
            if (f.NeverLowered)
                continue;
            foreach (var m in f.Methods)
            {
                if (helpers.TryGetValue(m.HelperName, out string? other))
                    throw new ContractException($"helper name collision: {m.HelperName} for {f.QualifiedName}.{m.Name} and {other}");
                helpers[m.HelperName] = f.QualifiedName + "." + m.Name;
            }
        }
    }

    // A family is a public abstract class (top-level or nested at any depth); the public enums
    // that share these namespaces are parameter types, not families.
    static void Collect(MetadataReader md, TypeDefinitionHandle h, string qualifiedName, Dictionary<string, TypeDefinitionHandle> found)
    {
        var td = md.GetTypeDefinition(h);
        var attrs = td.Attributes;
        var vis = attrs & TypeAttributes.VisibilityMask;
        bool isPublic = vis == TypeAttributes.Public || vis == TypeAttributes.NestedPublic;
        bool isAbstractClass = (attrs & TypeAttributes.Abstract) != 0 && (attrs & TypeAttributes.Interface) == 0;
        if (!isPublic || !isAbstractClass)
            return;
        found[qualifiedName] = h;
        foreach (var nh in td.GetNestedTypes())
            Collect(md, nh, qualifiedName + "+" + md.GetString(md.GetTypeDefinition(nh).Name), found);
    }

    static List<Method> Decode(MetadataReader md, TyProvider provider, TypeDefinitionHandle h, Family f)
    {
        var methods = new List<Method>();
        foreach (var mh in md.GetTypeDefinition(h).GetMethods())
        {
            var m = md.GetMethodDefinition(mh);
            if ((m.Attributes & MethodAttributes.Static) == 0
                || (m.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.Public)
                continue;
            string name = md.GetString(m.Name);
            if (name == "get_IsSupported")
                continue;
            string member = f.QualifiedName + "." + name;
            if ((m.Attributes & MethodAttributes.SpecialName) != 0)
                throw new ContractException($"{member}: unexpected special-name public static member");
            if (m.GetGenericParameters().Count != 0)
                throw new ContractException($"{member}: generic intrinsic methods are outside the contract");

            var sig = m.DecodeSignature(provider, null);
            // A never-lowered family's signatures (System.Numerics.Vector<T>, the Sve enums) stay
            // outside the argument-code vocabulary; only the method's existence is recorded.
            string[] codes = f.NeverLowered ? Array.Empty<string>() : sig.ParameterTypes.Select(p => Codes.Arg(p, member, f.Arch)).ToArray();
            if (!f.NeverLowered)
                Codes.CheckReturn(sig.ReturnType, member);
            methods.Add(new Method
            {
                Name = name,
                Return = sig.ReturnType,
                Params = sig.ParameterTypes,
                HelperName = $"dn2cpp_isa_{f.Arch}_{f.HelperPath}_{name.ToLowerInvariant()}" + string.Concat(codes.Select(c => "_" + c)),
                Key = f.NeverLowered ? name : name + "(" + string.Join(",", codes) + ")",
            });
        }
        return methods;
    }
}

sealed class TyProvider : ISignatureTypeProvider<Ty, object?>
{
    public Ty GetPrimitiveType(PrimitiveTypeCode code) => code switch
    {
        PrimitiveTypeCode.Void => Ty.Void,
        PrimitiveTypeCode.SByte => Codes.ScalarOf("i8"),
        PrimitiveTypeCode.Byte => Codes.ScalarOf("u8"),
        PrimitiveTypeCode.Int16 => Codes.ScalarOf("i16"),
        PrimitiveTypeCode.UInt16 => Codes.ScalarOf("u16"),
        PrimitiveTypeCode.Int32 => Codes.ScalarOf("i32"),
        PrimitiveTypeCode.UInt32 => Codes.ScalarOf("u32"),
        PrimitiveTypeCode.Int64 => Codes.ScalarOf("i64"),
        PrimitiveTypeCode.UInt64 => Codes.ScalarOf("u64"),
        PrimitiveTypeCode.Single => Codes.ScalarOf("f32"),
        PrimitiveTypeCode.Double => Codes.ScalarOf("f64"),
        PrimitiveTypeCode.IntPtr => Codes.ScalarOf("nint"),
        PrimitiveTypeCode.UIntPtr => Codes.ScalarOf("nuint"),
        PrimitiveTypeCode.Boolean => Codes.ScalarOf("bool"),
        PrimitiveTypeCode.Char => Codes.ScalarOf("char"),
        _ => Ty.Unsupported(code.ToString()),
    };

    public Ty GetTypeFromDefinition(MetadataReader md, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var td = md.GetTypeDefinition(handle);
        string ns = md.GetString(td.Namespace);
        string name = md.GetString(td.Name);
        if (ns == "System.Runtime.Intrinsics" && name is "Vector64`1" or "Vector128`1" or "Vector256`1" or "Vector512`1")
            return new Ty(TyKind.VectorDef, Bits: int.Parse(name.AsSpan(6, name.Length - 8), CultureInfo.InvariantCulture), Describe: name);
        if (ns == "System" && name.StartsWith("ValueTuple`", StringComparison.Ordinal))
            return new Ty(TyKind.TupleDef, Describe: name);

        // An enum is its underlying integer: find value__ and reuse the primitive mapping.
        var baseType = td.BaseType;
        if (baseType.Kind == HandleKind.TypeReference)
        {
            var tr = md.GetTypeReference((TypeReferenceHandle)baseType);
            if (md.GetString(tr.Namespace) == "System" && md.GetString(tr.Name) == "Enum")
                return Underlying(md, td, ns + "." + name);
        }
        else if (baseType.Kind == HandleKind.TypeDefinition)
        {
            var bt = md.GetTypeDefinition((TypeDefinitionHandle)baseType);
            if (md.GetString(bt.Namespace) == "System" && md.GetString(bt.Name) == "Enum")
                return Underlying(md, td, ns + "." + name);
        }
        return Ty.Unsupported(ns + "." + name);
    }

    Ty Underlying(MetadataReader md, TypeDefinition td, string enumName)
    {
        foreach (var fh in td.GetFields())
        {
            var f = md.GetFieldDefinition(fh);
            if (md.GetString(f.Name) == "value__")
                return f.DecodeSignature(this, null) with { CsEnum = enumName };
        }
        return Ty.Unsupported(enumName + " (enum without value__)");
    }

    public Ty GetTypeFromReference(MetadataReader md, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var tr = md.GetTypeReference(handle);
        return Ty.Unsupported(md.GetString(tr.Namespace) + "." + md.GetString(tr.Name));
    }

    public Ty GetTypeFromSpecification(MetadataReader md, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
        md.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

    public Ty GetGenericInstantiation(Ty genericType, ImmutableArray<Ty> typeArguments)
    {
        if (genericType.Kind == TyKind.VectorDef && typeArguments.Length == 1 && typeArguments[0].Kind == TyKind.Scalar)
            return new Ty(TyKind.Vector, Bits: genericType.Bits, Elem: typeArguments[0]);
        if (genericType.Kind == TyKind.TupleDef)
            return new Ty(TyKind.Tuple, Items: typeArguments);
        return Ty.Unsupported(genericType.Describe + "<" + string.Join(",", typeArguments) + ">");
    }

    public Ty GetPointerType(Ty elementType) => new(TyKind.Pointer, Elem: elementType.Kind == TyKind.Void ? null : elementType);
    public Ty GetByReferenceType(Ty elementType) => new(TyKind.ByRef, Elem: elementType);
    public Ty GetSZArrayType(Ty elementType) => Ty.Unsupported(elementType + "[]");
    public Ty GetArrayType(Ty elementType, ArrayShape shape) => Ty.Unsupported(elementType + "[,]");
    public Ty GetGenericMethodParameter(object? genericContext, int index) => Ty.Unsupported("!!" + index);
    public Ty GetGenericTypeParameter(object? genericContext, int index) => Ty.Unsupported("!" + index);
    public Ty GetFunctionPointerType(MethodSignature<Ty> signature) => Ty.Unsupported("function pointer");
    public Ty GetModifiedType(Ty modifier, Ty unmodifiedType, bool isRequired) => unmodifiedType;
    public Ty GetPinnedType(Ty elementType) => elementType;
}

// ---------------------------------------------------------------------------------------
// Argument codes and C++ / C# spellings.
// ---------------------------------------------------------------------------------------

static class Codes
{
    // code -> (C++ spelling, C# spelling)
    static readonly Dictionary<string, (string Cpp, string Cs)> Scalars = new(StringComparer.Ordinal)
    {
        ["i8"] = ("int8_t", "sbyte"), ["u8"] = ("uint8_t", "byte"),
        ["i16"] = ("int16_t", "short"), ["u16"] = ("uint16_t", "ushort"),
        ["i32"] = ("int32_t", "int"), ["u32"] = ("uint32_t", "uint"),
        ["i64"] = ("int64_t", "long"), ["u64"] = ("uint64_t", "ulong"),
        ["f32"] = ("float", "float"), ["f64"] = ("double", "double"),
        ["nint"] = ("intptr_t", "nint"), ["nuint"] = ("uintptr_t", "nuint"),
        ["bool"] = ("bool", "bool"), ["char"] = ("uint16_t", "char"),
    };

    public static Ty ScalarOf(string code) =>
        Scalars.TryGetValue(code, out var s) ? Ty.Scalar(code, s.Cpp) : throw new ContractException($"'{code}' is not a scalar code");

    public static string Cs(string code) => Scalars[code].Cs;

    // The helper-name code of a parameter. The surface is finite, so anything outside the
    // vocabulary is a contract change and fails naming the member.
    public static string Arg(Ty t, string member, string arch)
    {
        switch (t.Kind)
        {
            case TyKind.Scalar:
                return t.Code;
            case TyKind.Vector:
                // ElemBits spells a native-integer lane as 32 bits, which holds only on
                // wasm32; the x86 and Arm families declare no such vector, and one
                // appearing there would need a per-arch lane width.
                if (t.Elem!.Code is "nint" or "nuint" && arch != "wasm")
                    throw new ContractException($"{member}: a {t} parameter outside the wasm family; native-integer lanes are spelled for wasm32 only");
                return $"v{t.Bits}{t.Elem!.Code}";
            case TyKind.Pointer:
                if (t.Elem is null)
                    return "pvoid";
                if (t.Elem.Kind == TyKind.Scalar)
                    return "p" + t.Elem.Code;
                break;
            case TyKind.ByRef:
                if (t.Elem!.Kind == TyKind.Scalar)
                    return "r" + t.Elem.Code;
                break;
            case TyKind.Tuple:
                // Only homogeneous vector tuples occur (multi-register loads and stores); the
                // code is the arity followed by the shared item code.
                if (t.Items.Length >= 2 && t.Items.All(i => i.Kind == TyKind.Vector && i.ToString() == t.Items[0].ToString()))
                    return $"t{t.Items.Length}{Arg(t.Items[0], member, arch)}";
                break;
        }
        throw new ContractException($"{member}: parameter type '{t}' has no argument code");
    }

    // Return types are not encoded, but they must still be expressible as a C++ helper result.
    public static void CheckReturn(Ty t, string member)
    {
        switch (t.Kind)
        {
            case TyKind.Void:
            case TyKind.Scalar:
            case TyKind.Vector:
                return;
            case TyKind.Pointer when t.Elem is null || t.Elem.Kind == TyKind.Scalar:
                return;
            case TyKind.Tuple when t.Items.All(i => i.Kind is TyKind.Scalar or TyKind.Vector):
                return;
        }
        throw new ContractException($"{member}: return type '{t}' has no C++ helper shape");
    }

    public static string CppParam(Ty t) => t.Kind switch
    {
        TyKind.Scalar => t.Cpp,
        TyKind.Vector => $"const Dn2CppVector{t.Bits}&",
        TyKind.Pointer => t.Elem is null ? "void*" : t.Elem.Cpp + "*",
        TyKind.ByRef => t.Elem!.Cpp + "*",
        _ => throw new ContractException($"no C++ parameter spelling for '{t}'"),
    };

    public static string CppValue(Ty t) => t.Kind switch
    {
        TyKind.Void => "void",
        TyKind.Scalar => t.Cpp,
        TyKind.Vector => $"Dn2CppVector{t.Bits}",
        TyKind.Pointer => t.Elem is null ? "void*" : t.Elem.Cpp + "*",
        _ => throw new ContractException($"no C++ value spelling for '{t}'"),
    };

    // The arch's native register type a Dn2CppVector converts to and from.
    public static string Native(string arch, Ty v)
    {
        string e = v.Elem!.Code;
        switch (arch)
        {
            case "x86":
                if (v.Bits is not (128 or 256 or 512))
                    throw new ContractException($"x86 has no native {v.Bits}-bit vector");
                return $"__m{v.Bits}" + (e == "f32" ? "" : e == "f64" ? "d" : "i");
            case "arm":
                if (v.Bits is not (64 or 128))
                    throw new ContractException($"arm has no native {v.Bits}-bit vector");
                return NeonType(v.Bits, e);
            default:
                if (v.Bits != 128)
                    throw new ContractException($"wasm has no native {v.Bits}-bit vector");
                return "v128_t";
        }
    }

    public static string NeonType(int bits, string elem) => $"{Neon[elem]}x{bits / ElemBits(elem)}_t";

    // The NEON multi-register aggregate of `arity` vectors: int8x16x2_t.
    public static string NativeTuple(string arch, Ty item, int arity)
    {
        if (arch != "arm")
            throw new ContractException($"{arch} has no multi-register vector aggregate for a tuple parameter");
        return $"{Neon[item.Elem!.Code]}x{item.Bits / ElemBits(item.Elem.Code)}x{arity}_t";
    }

    // A native-integer lane is 32 bits: only the wasm family declares Vector128<nint> /
    // <nuint>, and wasm32 is its one target (Arg enforces the first half).
    public static int ElemBits(string code) => code switch
    {
        "i8" or "u8" => 8,
        "i16" or "u16" => 16,
        "i32" or "u32" or "f32" or "nint" or "nuint" => 32,
        "i64" or "u64" or "f64" => 64,
        _ => throw new ContractException($"'{code}' is not a vector element code"),
    };

    public static bool IsVectorElem(string code) => Neon.ContainsKey(code) || code is "nint" or "nuint";

    public static readonly Dictionary<string, string> Neon = new(StringComparer.Ordinal)
    {
        ["i8"] = "int8", ["u8"] = "uint8", ["i16"] = "int16", ["u16"] = "uint16",
        ["i32"] = "int32", ["u32"] = "uint32", ["i64"] = "int64", ["u64"] = "uint64",
        ["f32"] = "float32", ["f64"] = "float64",
    };
}

// ---------------------------------------------------------------------------------------
// Map files.
// ---------------------------------------------------------------------------------------

static class Maps
{
    static readonly Regex RowStart = new(@"^([A-Za-z_][A-Za-z0-9_]*)\((.*?)\)\s*(.*)$", RegexOptions.Compiled);
    static readonly Regex ForClause = new(@"\s+for\s+([WT])\s+in\s+([a-z0-9]+(?:\s*,\s*[a-z0-9]+)*)\s*$", RegexOptions.Compiled);
    static readonly Regex ImmAnn = new(@"^@imm(?:\$(\d+))?\[(\d+)\.\.(\d+)([\)\]])$", RegexOptions.Compiled);
    static readonly Regex ImmSetAnn = new(@"^@imm(?:\$(\d+))?\{(\d+(?:,\d+)*)\}$", RegexOptions.Compiled);
    static readonly Regex TargetAnn = new(@"^@target\(""([^""]+)""\)$", RegexOptions.Compiled);
    // Only identifier-like braces are placeholders, so C++ brace initializers in an
    // expression pass through.
    static readonly Regex Placeholder = new(@"\{([A-Za-z][A-Za-z0-9|]*)\}", RegexOptions.Compiled);

    // Per-element spellings a pattern row may use. Missing entries are deliberate: an integer
    // suffix asked for a float lane (or vice versa) is a map bug, not something to guess.
    static readonly Dictionary<string, Dictionary<string, string>> Tables = new(StringComparer.Ordinal)
    {
        ["epi"] = new()
        {
            ["i8"] = "epi8", ["u8"] = "epi8", ["i16"] = "epi16", ["u16"] = "epi16",
            ["i32"] = "epi32", ["u32"] = "epi32", ["i64"] = "epi64", ["u64"] = "epi64",
        },
        ["ps|pd"] = new() { ["f32"] = "ps", ["f64"] = "pd" },
        ["ss|sd"] = new() { ["f32"] = "ss", ["f64"] = "sd" },
        // The unsigned SSE integer suffix whatever the element (_mm_max_epu8 serves the
        // byte overload alone), and the suffix in the element's own signedness for an
        // instruction that has both forms (_mm_cvtepi8_epi16 / _mm_cvtepu8_epi16).
        ["epu"] = new()
        {
            ["i8"] = "epu8", ["u8"] = "epu8", ["i16"] = "epu16", ["u16"] = "epu16",
            ["i32"] = "epu32", ["u32"] = "epu32", ["i64"] = "epu64", ["u64"] = "epu64",
        },
        ["ep"] = new()
        {
            ["i8"] = "epi8", ["u8"] = "epu8", ["i16"] = "epi16", ["u16"] = "epu16",
            ["i32"] = "epi32", ["u32"] = "epu32", ["i64"] = "epi64", ["u64"] = "epu64",
        },
        ["neon"] = new()
        {
            ["i8"] = "s8", ["u8"] = "u8", ["i16"] = "s16", ["u16"] = "u16",
            ["i32"] = "s32", ["u32"] = "u32", ["i64"] = "s64", ["u64"] = "u64",
            ["f32"] = "f32", ["f64"] = "f64",
        },
        // The same-width unsigned element: the operand type of a NEON select mask or
        // table index, and the result type of a comparison.
        ["uT"] = new()
        {
            ["i8"] = "u8", ["u8"] = "u8", ["i16"] = "u16", ["u16"] = "u16",
            ["i32"] = "u32", ["u32"] = "u32", ["i64"] = "u64", ["u64"] = "u64",
            ["f32"] = "u32", ["f64"] = "u64",
        },
        // The same-width signed element (the right operand of USQADD).
        ["sT"] = new()
        {
            ["i8"] = "i8", ["u8"] = "i8", ["i16"] = "i16", ["u16"] = "i16",
            ["i32"] = "i32", ["u32"] = "i32", ["i64"] = "i64", ["u64"] = "i64",
            ["f32"] = "i32", ["f64"] = "i64",
        },
        // The element of twice / half the width (widening and narrowing operations), the
        // unsigned element of half the width (SQXTUN, SQSHRUN), and their ACLE suffixes.
        ["wT"] = new()
        {
            ["i8"] = "i16", ["u8"] = "u16", ["i16"] = "i32", ["u16"] = "u32", ["i32"] = "i64", ["u32"] = "u64",
        },
        ["nT"] = new()
        {
            ["i16"] = "i8", ["u16"] = "u8", ["i32"] = "i16", ["u32"] = "u16", ["i64"] = "i32", ["u64"] = "u32",
        },
        ["unT"] = new()
        {
            ["i16"] = "u8", ["u16"] = "u8", ["i32"] = "u16", ["u32"] = "u16", ["i64"] = "u32", ["u64"] = "u32",
        },
        ["wneon"] = new()
        {
            ["i8"] = "s16", ["u8"] = "u16", ["i16"] = "s32", ["u16"] = "u32", ["i32"] = "s64", ["u32"] = "u64",
        },
        ["nneon"] = new()
        {
            ["i16"] = "s8", ["u16"] = "u8", ["i32"] = "s16", ["u32"] = "u16", ["i64"] = "s32", ["u64"] = "u32",
        },
        // Half the element width: the shift range of a narrowing shift.
        ["hbits"] = new()
        {
            ["i16"] = "8", ["u16"] = "8", ["i32"] = "16", ["u32"] = "16", ["i64"] = "32", ["u64"] = "32",
        },
        // The scalar-register letter of a one-lane ACLE intrinsic (vqrdmlahh_s16, vqaddd_s64).
        ["bhsd"] = new()
        {
            ["i8"] = "b", ["u8"] = "b", ["i16"] = "h", ["u16"] = "h",
            ["i32"] = "s", ["u32"] = "s", ["i64"] = "d", ["u64"] = "d",
            ["f32"] = "s", ["f64"] = "d",
        },
        // The wasm lane shape in the intrinsic's own signedness (wasm_u8x16_min), and the
        // signed / unsigned spelling regardless of the element's (wasm_i8x16_add exists,
        // wasm_u8x16_add does not; wasm_u16x8_extend_low_u8x16 is the zero-extension of
        // a signed operand too). A native-integer lane is i32x4 / u32x4 on wasm32.
        ["lane"] = new()
        {
            ["i8"] = "i8x16", ["u8"] = "u8x16", ["i16"] = "i16x8", ["u16"] = "u16x8",
            ["i32"] = "i32x4", ["u32"] = "u32x4", ["i64"] = "i64x2", ["u64"] = "u64x2",
            ["f32"] = "f32x4", ["f64"] = "f64x2", ["nint"] = "i32x4", ["nuint"] = "u32x4",
        },
        ["slane"] = new()
        {
            ["i8"] = "i8x16", ["u8"] = "i8x16", ["i16"] = "i16x8", ["u16"] = "i16x8",
            ["i32"] = "i32x4", ["u32"] = "i32x4", ["i64"] = "i64x2", ["u64"] = "i64x2",
            ["f32"] = "f32x4", ["f64"] = "f64x2", ["nint"] = "i32x4", ["nuint"] = "i32x4",
        },
        ["ulane"] = new()
        {
            ["i8"] = "u8x16", ["u8"] = "u8x16", ["i16"] = "u16x8", ["u16"] = "u16x8",
            ["i32"] = "u32x4", ["u32"] = "u32x4", ["i64"] = "u64x2", ["u64"] = "u64x2",
            ["nint"] = "u32x4", ["nuint"] = "u32x4",
        },
        // The lane shape of the twice-width element (a widening result), in the element's
        // signedness and in the signed / unsigned spelling.
        ["wlane"] = new()
        {
            ["i8"] = "i16x8", ["u8"] = "u16x8", ["i16"] = "i32x4", ["u16"] = "u32x4", ["i32"] = "i64x2", ["u32"] = "u64x2",
        },
        ["swlane"] = new()
        {
            ["i8"] = "i16x8", ["u8"] = "i16x8", ["i16"] = "i32x4", ["u16"] = "i32x4", ["i32"] = "i64x2", ["u32"] = "i64x2",
        },
        ["uwlane"] = new()
        {
            ["i8"] = "u16x8", ["u8"] = "u16x8", ["i16"] = "u32x4", ["u16"] = "u32x4", ["i32"] = "u64x2", ["u32"] = "u64x2",
        },
        // The C# element type, and its same-width signed / unsigned counterpart: the casts
        // and As<,>() reinterpretations a @ref expression needs.
        ["cs"] = new()
        {
            ["i8"] = "sbyte", ["u8"] = "byte", ["i16"] = "short", ["u16"] = "ushort",
            ["i32"] = "int", ["u32"] = "uint", ["i64"] = "long", ["u64"] = "ulong",
            ["f32"] = "float", ["f64"] = "double", ["nint"] = "nint", ["nuint"] = "nuint",
        },
        ["scs"] = new()
        {
            ["i8"] = "sbyte", ["u8"] = "sbyte", ["i16"] = "short", ["u16"] = "short",
            ["i32"] = "int", ["u32"] = "int", ["i64"] = "long", ["u64"] = "long",
            ["nint"] = "nint", ["nuint"] = "nint",
        },
        ["ucs"] = new()
        {
            ["i8"] = "byte", ["u8"] = "byte", ["i16"] = "ushort", ["u16"] = "ushort",
            ["i32"] = "uint", ["u32"] = "uint", ["i64"] = "ulong", ["u64"] = "ulong",
            ["nint"] = "nuint", ["nuint"] = "nuint",
        },
        // The C# type of the element of half the width: the zero operand a @ref narrows with.
        ["ncs"] = new()
        {
            ["i16"] = "sbyte", ["u16"] = "byte", ["i32"] = "short", ["u32"] = "ushort", ["i64"] = "int", ["u64"] = "uint",
        },
    };

    public static void Apply(string toolDir, List<Family> families)
    {
        var byPath = new Dictionary<string, Family>(StringComparer.Ordinal);
        foreach (var f in families)
            byPath[$"{f.Arch}/{f.HelperPath}"] = f;

        string mapDir = Path.Combine(toolDir, "map");
        if (!Directory.Exists(mapDir))
            return;
        foreach (string file in Directory.GetFiles(mapDir, "*.map", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(mapDir, file).Replace('\\', '/');
            // arch/family.map, or arch/family/topic.map
            string noExt = rel.Substring(0, rel.Length - ".map".Length);
            string dir = Path.GetDirectoryName(rel)!.Replace('\\', '/');
            Family? family;
            if (!byPath.TryGetValue(noExt, out family) && !byPath.TryGetValue(dir, out family))
                throw new ContractException($"{file}: no family has the map path '{noExt}' or the directory '{dir}'");
            if (family.NeverLowered)
                throw new ContractException($"{file}: {family.QualifiedName} has no feature bits and is never lowered");
            Parse(file, family);
        }
        foreach (var f in families.Where(f => f.Derive is not null))
            Derive(f, families);
    }

    // A derived family takes, for each method it shares with a source family (same name and
    // argument codes), the source's row under its own file's target; the first source in the
    // list wins, and an explicit row in the family's own file wins over every source. .NET 10's
    // Avx10v1 is the AVX-512 VL and scalar surfaces re-exposed under one token, so copying the
    // rows would drift silently; a method no source covers is an error rather than an unmapped
    // method, so a surface addition to the derived family cannot hide behind the derivation.
    static void Derive(Family f, List<Family> families)
    {
        string where = f.DeriveSource!;
        var byKey = f.Methods.ToDictionary(m => m.Key, StringComparer.Ordinal);
        foreach (string name in f.Derive!)
        {
            var src = families.FirstOrDefault(g => Contract.Display(g.QualifiedName) == name)
                ?? throw new ContractException($"{where}: no family is named '{name}' (use the display name, e.g. X86.Avx512F.VL)");
            if (src == f || src.Arch != f.Arch)
                throw new ContractException($"{where}: {name} is not another {f.Arch} family");
            if (src.Derive is not null)
                throw new ContractException($"{where}: {name} derives its own rows; a source must be mapped directly");
            if (!src.Methods.Any(m => m.Row is not null))
                throw new ContractException($"{where}: {name} has no map rows to derive from");
            foreach (var m in src.Methods)
            {
                if (m.Row is not null && byKey.TryGetValue(m.Key, out var mine) && mine.Row is null)
                    mine.Row = m.Row with { Target = null, SourceLine = $"{where} (from {name}: {m.Row.SourceLine})" };
            }
        }
        var uncovered = f.Methods.Where(m => m.Row is null).Select(m => m.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
        if (uncovered.Count > 0)
            throw new ContractException($"{where}: no source covers {uncovered.Count} method(s) of {f.QualifiedName}; add explicit rows for {string.Join(" ", uncovered)}");
    }

    sealed record RowSpec(string Method, string[] Codes, string[] Annotations, string Expression, string[] Widths, string[] Elems, string Where);

    // The annotation at the start of `rest`: up to the next space, or — for one that opens
    // a parenthesis (@ref(Vector128.Add($0, $1))) — to its matching close, so a C#
    // expression with spaces and nested calls is one annotation.
    static string TakeAnnotation(string rest, string where)
    {
        int paren = rest.IndexOf('(');
        int space = rest.IndexOf(' ');
        if (paren < 0 || (space >= 0 && space < paren))
            return space < 0 ? rest : rest.Substring(0, space);
        int depth = 0;
        for (int i = paren; i < rest.Length; i++)
        {
            if (rest[i] == '(')
                depth++;
            else if (rest[i] == ')' && --depth == 0)
                return rest.Substring(0, i + 1);
        }
        throw new ContractException($"{where}: unbalanced parentheses in annotation '{rest}'");
    }

    static void Parse(string file, Family family)
    {
        var byKey = family.Methods.ToDictionary(m => m.Key, StringComparer.Ordinal);
        string[] lines = File.ReadAllLines(file);
        for (int ln = 0; ln < lines.Length; ln++)
        {
            string line = lines[ln].Trim();
            string where = $"{file}:{ln + 1}";
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("target", StringComparison.Ordinal) && line.Length > 6 && (line[6] == ' ' || line[6] == '='))
            {
                int eq = line.IndexOf('=');
                if (eq < 0)
                    throw new ContractException($"{where}: expected 'target = <isa>'");
                string target = line.Substring(eq + 1).Trim();
                if (target.Length == 0)
                    throw new ContractException($"{where}: empty target");
                if (family.Target is not null && family.Target != target)
                    throw new ContractException($"{where}: target '{target}' disagrees with '{family.Target}' at {family.TargetSource}; a family has one target");
                family.Target = target;
                family.TargetSource = where;
                continue;
            }

            if (line.StartsWith("derive", StringComparison.Ordinal) && line.Length > 6 && (line[6] == ' ' || line[6] == '='))
            {
                int eq = line.IndexOf('=');
                if (eq < 0)
                    throw new ContractException($"{where}: expected 'derive = <Arch.Family>, ...'");
                string[] names = line.Substring(eq + 1).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (names.Length == 0)
                    throw new ContractException($"{where}: empty derive list");
                if (family.Derive is not null)
                    throw new ContractException($"{where}: a second derive list; {family.DeriveSource} already has one");
                family.Derive = names;
                family.DeriveSource = where;
                continue;
            }

            var spec = ParseRow(line, where);
            foreach (string w in spec.Widths.Length == 0 ? new[] { "" } : spec.Widths)
            {
                foreach (string t in spec.Elems.Length == 0 ? new[] { "" } : spec.Elems)
                    Instantiate(spec, w, t, family, byKey);
            }
        }
    }

    static RowSpec ParseRow(string line, string where)
    {
        var m = RowStart.Match(line);
        if (!m.Success)
            throw new ContractException($"{where}: expected 'Method(codes) [@annotation ...] = expression'");
        string method = m.Groups[1].Value;
        string[] codes = m.Groups[2].Value.Length == 0
            ? Array.Empty<string>()
            : m.Groups[2].Value.Split(',').Select(c => c.Trim()).ToArray();
        string rest = m.Groups[3].Value;

        var annotations = new List<string>();
        while (rest.StartsWith('@'))
        {
            string ann = TakeAnnotation(rest, where);
            annotations.Add(ann);
            rest = rest.Substring(ann.Length).TrimStart();
        }
        if (!rest.StartsWith('='))
            throw new ContractException($"{where}: expected '=' after the signature and annotations");
        string expression = rest.Substring(1).Trim();

        string[] widths = Array.Empty<string>();
        string[] elems = Array.Empty<string>();
        for (var fc = ForClause.Match(expression); fc.Success; fc = ForClause.Match(expression))
        {
            string[] values = fc.Groups[2].Value.Split(',').Select(e => e.Trim()).ToArray();
            if (fc.Groups[1].Value == "W")
            {
                if (widths.Length > 0)
                    throw new ContractException($"{where}: two 'for W in' clauses");
                if (values.Any(v => v is not ("64" or "128" or "256" or "512")))
                    throw new ContractException($"{where}: 'for W in' takes vector widths (64, 128, 256, 512)");
                widths = values;
            }
            else
            {
                if (elems.Length > 0)
                    throw new ContractException($"{where}: two 'for T in' clauses");
                if (values.Any(v => !Codes.IsVectorElem(v)))
                    throw new ContractException($"{where}: 'for T in' takes vector element codes");
                elems = values;
            }
            expression = expression.Substring(0, fc.Index).TrimEnd();
        }
        if (expression.Length == 0)
            throw new ContractException($"{where}: empty expression");

        string all = string.Join(" ", codes) + " " + string.Join(" ", annotations) + " " + expression;
        bool usesT = false, usesW = false;
        foreach (Match p in Placeholder.Matches(all))
        {
            (bool t, bool w) = Dependency(p.Groups[1].Value, where);
            usesT |= t;
            usesW |= w;
        }
        if (usesT && elems.Length == 0)
            throw new ContractException($"{where}: a row with element placeholders needs 'for T in ...'");
        if (!usesT && elems.Length > 0)
            throw new ContractException($"{where}: 'for T in ...' on a row without element placeholders");
        if (usesW && widths.Length == 0)
            throw new ContractException($"{where}: a row with width placeholders needs 'for W in ...'");
        if (!usesW && widths.Length > 0)
            throw new ContractException($"{where}: 'for W in ...' on a row without width placeholders");
        return new RowSpec(method, codes, annotations.ToArray(), expression, widths, elems, where);
    }

    // Which loop variables a placeholder reads: (element, width).
    static (bool T, bool W) Dependency(string name, string where) => name switch
    {
        "T" or "bits" or "N64" or "N128" or "uT" or "sT" or "wT" or "nT" or "unT" or "hbits"
            or "neon" or "wneon" or "nneon" or "bhsd" or "epi" or "epu" or "ep" or "ps|pd" or "ss|sd"
            or "lane" or "slane" or "ulane" or "wlane" or "swlane" or "uwlane"
            or "cs" or "scs" or "ucs" or "ncs" => (true, false),
        "W" or "q" or "mm" or "m" => (false, true),
        "N" or "ntype" => (true, true),
        _ => throw new ContractException($"{where}: unknown placeholder '{{{name}}}'"),
    };

    static void Instantiate(RowSpec spec, string w, string t, Family family, Dictionary<string, Method> byKey)
    {
        string where = spec.Where + (t.Length > 0 || w.Length > 0 ? $" [{string.Join(" ", new[] { w.Length > 0 ? "W=" + w : "", t.Length > 0 ? "T=" + t : "" }.Where(s => s.Length > 0))}]" : "");
        string[] cs = spec.Codes.Select(c => Substitute(c, w, t, where)).ToArray();
        string key = spec.Method + "(" + string.Join(",", cs) + ")";
        if (!byKey.TryGetValue(key, out var mm))
        {
            var overloads = family.Methods.Where(x => x.Name == spec.Method).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
            string hint = overloads.Count == 0 ? "" : "; its overloads are " + string.Join(" ", overloads);
            throw new ContractException($"{where}: {family.QualifiedName} has no method {key}{hint}");
        }
        if (mm.Row is not null)
            throw new ContractException($"{where}: {key} already has a row ({mm.Row.SourceLine})");

        string? target = null;
        string? reference = null;
        var imms = new List<ImmSpec>();
        foreach (string raw in spec.Annotations)
        {
            string ann = Substitute(raw, w, t, where);
            if (ann.StartsWith("@ref(", StringComparison.Ordinal) && ann.EndsWith(')'))
            {
                if (reference is not null)
                    throw new ContractException($"{where}: two @ref annotations");
                reference = ann.Substring(5, ann.Length - 6).Trim();
                if (reference.Length == 0)
                    throw new ContractException($"{where}: empty @ref");
            }
            else if (ann == "@imm8")
                imms.Add(new ImmSpec(mm.Params.Length - 1, 0, 256));
            else if (ImmAnn.Match(ann) is { Success: true } r)
            {
                int param = r.Groups[1].Success ? int.Parse(r.Groups[1].Value, CultureInfo.InvariantCulture) : mm.Params.Length - 1;
                int lo = int.Parse(r.Groups[2].Value, CultureInfo.InvariantCulture);
                int hi = int.Parse(r.Groups[3].Value, CultureInfo.InvariantCulture);
                int count = hi - lo + (r.Groups[4].Value == "]" ? 1 : 0);
                if (count < 1 || count > 256 || (count & (count - 1)) != 0)
                    throw new ContractException($"{where}: {ann} spans {count} values; the dispatch takes a power of two up to 256");
                imms.Add(new ImmSpec(param, lo, count));
            }
            else if (ImmSetAnn.Match(ann) is { Success: true } rs)
            {
                int param = rs.Groups[1].Success ? int.Parse(rs.Groups[1].Value, CultureInfo.InvariantCulture) : mm.Params.Length - 1;
                var values = rs.Groups[2].Value.Split(',').Select(v => int.Parse(v, CultureInfo.InvariantCulture)).ToImmutableArray();
                for (int i = 1; i < values.Length; i++)
                {
                    if (values[i] <= values[i - 1])
                        throw new ContractException($"{where}: {ann} must list its values in ascending order");
                }
                if (values[values.Length - 1] > 255)
                    throw new ContractException($"{where}: {ann} lists a value above 255");
                imms.Add(new ImmSpec(param, values[0], values.Length, values));
            }
            else if (TargetAnn.Match(ann) is { Success: true } ta)
                target = ta.Groups[1].Value;
            else if (ann != "@throws")
                throw new ContractException($"{where}: unknown annotation '{ann}'");
        }
        imms.Sort((a, b) => a.Param.CompareTo(b.Param));
        foreach (var imm in imms)
        {
            if (imm.Param < 0 || imm.Param >= mm.Params.Length || mm.Params[imm.Param].Kind != TyKind.Scalar)
                throw new ContractException($"{where}: @imm needs a scalar parameter; ${imm.Param} of {key} is not one");
        }
        if (imms.Select(i => i.Param).Distinct().Count() != imms.Count)
            throw new ContractException($"{where}: one parameter annotated as an immediate twice");
        if (imms.Count > 2)
            throw new ContractException($"{where}: at most two immediates per helper");
        if (imms.Count == 2 && imms[0].Count * imms[1].Count > 256)
            throw new ContractException($"{where}: two immediates spanning {imms[0].Count * imms[1].Count} cases; the bound is 256");
        if (imms.Count == 2 && imms.Any(i => i.IsSet))
            throw new ContractException($"{where}: a listed-values immediate is dispatched alone");

        string expr = Substitute(spec.Expression, w, t, where);
        mm.Row = new MapRow(expr, target, imms.ToImmutableArray(), reference, where);
    }

    static string Substitute(string text, string w, string t, string where) =>
        Placeholder.Replace(text, m =>
        {
            string name = m.Groups[1].Value;
            (bool needT, bool needW) = Dependency(name, where);
            if (needT && t.Length == 0 || needW && w.Length == 0)
                throw new ContractException($"{where}: '{{{name}}}' needs a loop variable this row does not bind");
            switch (name)
            {
                case "T": return t;
                case "W": return w;
                case "q": return w == "64" ? "" : "q";
                case "mm": return w == "128" ? "_mm" : "_mm" + w;
                case "m": return "__m" + w;
                case "bits": return Codes.ElemBits(t).ToString(CultureInfo.InvariantCulture);
                case "N64": return (64 / Codes.ElemBits(t)).ToString(CultureInfo.InvariantCulture);
                case "N128": return (128 / Codes.ElemBits(t)).ToString(CultureInfo.InvariantCulture);
                case "N": return (int.Parse(w, CultureInfo.InvariantCulture) / Codes.ElemBits(t)).ToString(CultureInfo.InvariantCulture);
                case "ntype": return Codes.NeonType(int.Parse(w, CultureInfo.InvariantCulture), t);
            }
            if (!Tables[name].TryGetValue(t, out string? spelled))
                throw new ContractException($"{where}: '{{{name}}}' has no spelling for element code '{t}'");
            return spelled;
        });
}

// ---------------------------------------------------------------------------------------
// Output text.
// ---------------------------------------------------------------------------------------

static class Emit
{
    public static readonly UTF8Encoding Utf8NoBom = new(false);
    const string Regenerate = "dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>";
    public static string? Preview;

    static readonly Regex OutItemsAddr = new(@"&\$r\*", RegexOptions.Compiled);
    static readonly Regex OutItemAddr = new(@"&\$r(\d+)", RegexOptions.Compiled);
    static readonly Regex OutItem = new(@"\$r(\d+)", RegexOptions.Compiled);
    static readonly Regex Param = new(@"\$(\d+)(?:\.(\d+|\*))?(?::([iuf](?:8|16|32|64)))?", RegexOptions.Compiled);

    public static Dictionary<string, string> All(List<Family> families)
    {
        var outputs = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["runtime/core/isa/dn2cpp_isa_tokens.g.h"] = Tokens(families),
            ["runtime/core/isa/dn2cpp_isa_families.g.h"] = FamiliesHeader(families),
            ["runtime/core/isa/dn2cpp_isa_manifest.txt"] = Manifest(families),
            ["src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs"] = FamilyTable(families),
            ["samples/dotnet/PlatformIsaProbe/Exercises.g.cs"] = Exercises.File(families),
        };
        foreach (var f in families.Where(f => f.HasRows))
            outputs[f.HeaderRelPath] = FamilyHeader(f);
        return outputs;
    }

    static string PreviewLine(string comment) =>
        Preview is null ? "" : $"{comment} PREVIEW of {Preview}: unmapped methods are stubs; not a checked-in state.\n";

    static string CppBanner(string what) => $"""
        #pragma once
        // GENERATED FILE — do not edit by hand.
        //
        // {what}
        // Regenerate from System.Private.CoreLib with:
        //
        //     {Regenerate}
        //
        {PreviewLine("//")}
        """;

    public static string CsBanner(string what) => $"""
        // <auto-generated/>
        // {what}
        // Generated by tools/gen-isa-map from System.Private.CoreLib; regenerate with
        //   {Regenerate}
        {PreviewLine("//")}
        """;

    static string Tokens(List<Family> families)
    {
        var sb = new StringBuilder();
        sb.Append(CppBanner("One IsSupported token per hardware-intrinsics family, as the transpiler emits it."));
        sb.Append("""
            // On the family's own target, with every required compiler intrinsic available, the token
            // asks the feature detector for every bit the family requires. Otherwise it is false, so
            // the guarded arm is dead code the compiler drops. Nested X64/Arm64 types share their
            // enclosing type's bits: every target here is 64-bit.
            #include "dn2cpp_cpu_features.h"

            """);
        foreach (string arch in Contract.Archs)
        {
            var own = families.Where(f => f.Arch == arch && !f.NeverLowered).ToList();
            var never = families.Where(f => f.Arch == arch && f.NeverLowered).ToList();
            sb.Append('\n').Append("// ").Append(Contract.Namespace(arch)).Append('\n');
            if (own.Count > 0)
            {
                sb.Append("#if ").Append(Contract.TargetMacro(arch)).Append('\n');
                foreach (var f in own)
                {
                    string compiler = Contract.CompilerCondition(f);
                    sb.Append("#define ").Append(f.Token).Append(" (");
                    if (compiler != "1")
                        sb.Append(compiler).Append(" && ");
                    sb.Append("dn2cpp_cpu_has_all(")
                      .Append(string.Join(" | ", f.Bits.Select(b => "DN2CPP_CPU_" + b))).Append("))\n");
                }
                sb.Append("#else\n");
                foreach (var f in own)
                    sb.Append("#define ").Append(f.Token).Append(" 0\n");
                sb.Append("#endif\n");
            }
            if (never.Count > 0)
            {
                sb.Append("// Scalable vectors are never lowered: experimental in .NET 10 and without a fixed\n");
                sb.Append("// register width, so IsSupported is false on every target.\n");
                foreach (var f in never)
                    sb.Append("#define ").Append(f.Token).Append(" 0\n");
            }
        }
        return sb.ToString();
    }

    static string FamiliesHeader(List<Family> families)
    {
        var sb = new StringBuilder();
        sb.Append(CppBanner("Every generated family helper header."));
        sb.Append("""
            // Every header is included on every target: the foreign-arch stubs must exist so that a
            // dead `#if` arm in generated code still compiles.
            #include "dn2cpp_isa_common.h"

            """);
        foreach (var f in families.Where(f => f.HasRows).OrderBy(f => f.HeaderRelPath, StringComparer.Ordinal))
            sb.Append("#include \"").Append(f.Arch).Append('/').Append(Path.GetFileName(f.HeaderRelPath)).Append("\"\n");
        return sb.ToString();
    }

    // A method's helper is defined when it has a row, or when its family is previewed (a stub).
    public static bool HasHelper(Family f, Method m) => m.Row is not null || f.Preview;

    static string Manifest(List<Family> families)
    {
        var names = families.SelectMany(f => f.Methods.Where(m => HasHelper(f, m)).Select(m => m.HelperName))
            .OrderBy(n => n, StringComparer.Ordinal);
        var sb = new StringBuilder();
        foreach (string n in names)
            sb.Append(n).Append('\n');
        return sb.ToString();
    }

    static string FamilyTable(List<Family> families)
    {
        var sb = new StringBuilder();
        sb.Append(CsBanner("The platform-ISA family table. Lowered is derived from map coverage and the feature-bit implications, never edited."));
        sb.Append("""
            namespace Dn2Cpp;

            internal static partial class CoreIntrinsics
            {
                private static readonly IsaFamily[] s_isaFamilies =
                {

            """);
        foreach (var f in families)
        {
            string enclosing = f.Enclosing is null ? "null" : "\"" + f.Enclosing + "\"";
            sb.Append("        new(IsaArch.").Append(Contract.IsaArch(f.Arch)).Append(", \"").Append(f.QualifiedName)
              .Append("\", \"").Append(f.Token).Append("\", ").Append(enclosing).Append(", ")
              .Append(f.Lowered ? "true" : "false").Append("),\n");
        }
        sb.Append("""
                };
            }

            """);
        return sb.ToString();
    }

    static string FamilyHeader(Family f)
    {
        var sb = new StringBuilder();
        sb.Append(CppBanner($"Helpers for {f.QualifiedName}: one per public static method that has a map row."));
        sb.Append("#include \"../dn2cpp_isa_common.h\"\n");
        foreach (var m in f.Methods.Where(m => HasHelper(f, m)).OrderBy(m => m.HelperName, StringComparer.Ordinal))
            Helper(sb, f, m);
        return sb.ToString();
    }

    // A helper is declared twice with one signature: the real body under the arch's target
    // macro, and a [[noreturn]] stub otherwise, so call sites in foreign-arch dead arms compile.
    static void Helper(StringBuilder sb, Family f, Method m)
    {
        var paramDecls = new List<string>();
        var stubDecls = new List<string>();
        for (int i = 0; i < m.Params.Length; i++)
        {
            var p = m.Params[i];
            if (p.Kind == TyKind.Tuple)
            {
                for (int j = 0; j < p.Items.Length; j++)
                {
                    string cpp = Codes.CppParam(p.Items[j]);
                    paramDecls.Add($"{cpp} a{i}_{j + 1}");
                    stubDecls.Add(cpp);
                }
                continue;
            }
            string t = Codes.CppParam(p);
            paramDecls.Add($"{t} a{i}");
            stubDecls.Add(t);
        }
        string ret;
        if (m.Return.Kind == TyKind.Tuple)
        {
            ret = "void";
            for (int j = 0; j < m.Return.Items.Length; j++)
            {
                string cpp = Codes.CppValue(m.Return.Items[j]) + "*";
                paramDecls.Add($"{cpp} item{j + 1}");
                stubDecls.Add(cpp);
            }
        }
        else
        {
            ret = Codes.CppValue(m.Return);
        }
        string display = f.QualifiedName + "." + m.Name;

        sb.Append('\n');
        sb.Append("#if ").Append(Contract.HelperCondition(f)).Append('\n');
        if (m.Row is null)
        {
            // A previewed family's unmapped method: throws on the native target too.
            sb.Append("[[noreturn]] DN2CPP_ISA_INLINE ").Append(ret).Append(' ').Append(m.HelperName)
              .Append('(').Append(string.Join(", ", stubDecls)).Append(")\n{\n    dn2cpp_isa_not_lowered(\"")
              .Append(display).Append(" (preview: no map row)\");\n}\n");
        }
        else
        {
            string body = Body(f, m, m.Row);
            string target = m.Row.Target ?? f.Target ?? "";
            string attr = target.Length > 0 ? $"DN2CPP_ISA_TARGET(\"{target}\") " : "";
            sb.Append(attr).Append("DN2CPP_ISA_INLINE ").Append(ret).Append(' ').Append(m.HelperName)
              .Append('(').Append(string.Join(", ", paramDecls)).Append(")\n{\n")
              .Append("    dn2cpp_isa_require(").Append(f.Token).Append(", \"").Append(display).Append("\");\n")
              .Append("    ").Append(body).Append("\n}\n");
        }
        sb.Append("#else\n");
        sb.Append("[[noreturn]] DN2CPP_ISA_INLINE ").Append(ret).Append(' ').Append(m.HelperName)
          .Append('(').Append(string.Join(", ", stubDecls)).Append(")\n{\n    dn2cpp_isa_not_lowered(\"")
          .Append(display).Append("\");\n}\n");
        sb.Append("#endif\n");
    }

    static string Body(Family f, Method m, MapRow row)
    {
        string OutItemIndex(Match x)
        {
            if (m.Return.Kind != TyKind.Tuple)
                throw new ContractException($"{row.SourceLine}: {x.Value} but {m.Key} does not return a tuple");
            int j = int.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture);
            if (j < 1 || j > m.Return.Items.Length)
                throw new ContractException($"{row.SourceLine}: {x.Value} is outside the returned tuple's items");
            return x.Groups[1].Value;
        }
        // The address forms first, so `&$r1` is the pointer itself rather than `&(*item1)`.
        string expr = OutItemsAddr.Replace(row.Expression, x =>
        {
            if (m.Return.Kind != TyKind.Tuple)
                throw new ContractException($"{row.SourceLine}: &$r* but {m.Key} does not return a tuple");
            return string.Join(", ", Enumerable.Range(1, m.Return.Items.Length).Select(j => "item" + j));
        });
        expr = OutItemAddr.Replace(expr, x => "item" + OutItemIndex(x));
        expr = OutItem.Replace(expr, x => "(*item" + OutItemIndex(x) + ")");
        expr = Param.Replace(expr, x =>
        {
            int k = int.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture);
            if (k >= m.Params.Length)
                throw new ContractException($"{row.SourceLine}: ${k} but {m.Key} has {m.Params.Length} parameter(s)");
            var p = m.Params[k];
            string? asElem = x.Groups[3].Success ? x.Groups[3].Value : null;
            int immIndex = row.Imms.ToList().FindIndex(i => i.Param == k);
            if (immIndex >= 0)
            {
                if (x.Groups[2].Success || asElem is not null)
                    throw new ContractException($"{row.SourceLine}: ${k} is an immediate and takes no item or element");
                return immIndex == 0 ? "DN2CPP_IMM" : "DN2CPP_IMM2";
            }
            if (p.Kind == TyKind.Tuple)
            {
                if (!x.Groups[2].Success)
                    throw new ContractException($"{row.SourceLine}: ${k} is a tuple; name an item as ${k}.<n> or the aggregate as ${k}.*");
                var item = asElem is null ? p.Items[0] : Ty.Vector(p.Items[0].Bits, asElem);
                if (x.Groups[2].Value == "*")
                {
                    string items = string.Join(", ", Enumerable.Range(1, p.Items.Length)
                        .Select(j => $"dn2cpp_isa_bits<{Codes.Native(f.Arch, item)}>(a{k}_{j})"));
                    // Parenthesized: the ACLE intrinsic may be a macro, and the aggregate's
                    // commas must not split its arguments.
                    return $"({Codes.NativeTuple(f.Arch, item, p.Items.Length)}{{{{{items}}}}})";
                }
                int j = int.Parse(x.Groups[2].Value, CultureInfo.InvariantCulture);
                if (j < 1 || j > p.Items.Length)
                    throw new ContractException($"{row.SourceLine}: ${k}.{j} is outside the tuple's items");
                return $"dn2cpp_isa_bits<{Codes.Native(f.Arch, item)}>(a{k}_{j})";
            }
            if (x.Groups[2].Success)
                throw new ContractException($"{row.SourceLine}: ${k} is not a tuple");
            if (p.Kind == TyKind.Vector)
                return $"dn2cpp_isa_bits<{Codes.Native(f.Arch, asElem is null ? p : Ty.Vector(p.Bits, asElem))}>(a{k})";
            if (asElem is not null)
                throw new ContractException($"{row.SourceLine}: ${k}:{asElem} but ${k} is not a vector");
            return $"a{k}";
        });

        string produce = m.Return.Kind switch
        {
            TyKind.Vector => $"dn2cpp_isa_vec<{m.Return.Bits / 8}>({expr})",
            _ => expr,
        };
        switch (row.Imms.Length)
        {
            case 0:
                return m.Return.Kind is TyKind.Void or TyKind.Tuple ? produce + ";" : "return " + produce + ";";
            case 1:
            {
                var i = row.Imms[0];
                if (i.IsSet)
                {
                    // Listed values: one case each, anything else out of range.
                    string cases = string.Concat(i.Values.Select(v => $"DN2CPP_ISA_IMM_CASE({v}, {produce}) "));
                    return $"switch ((int)a{i.Param}) {{ {cases}default: dn2cpp_throw_argument_out_of_range(); }}";
                }
                return i.Lo == 0 && i.Count == 256
                    ? $"DN2CPP_ISA_IMM8_SWITCH(a{i.Param}, {produce});"
                    : $"DN2CPP_ISA_IMM_RANGE_SWITCH({i.Lo}, {i.Count}, a{i.Param}, {produce});";
            }
            default:
            {
                var i1 = row.Imms[0];
                var i2 = row.Imms[1];
                return $"DN2CPP_ISA_IMM_RANGE_SWITCH2({i1.Lo}, {i1.Count}, a{i1.Param}, {i2.Lo}, {i2.Count}, a{i2.Param}, {produce});";
            }
        }
    }
}

// ---------------------------------------------------------------------------------------
// samples/dotnet/PlatformIsaProbe/Exercises.g.cs — one exercise per lowered family that has
// no hand-written one: every mapped method called once with fixed inputs, its result printed
// as hex bytes. Real .NET is the oracle (the native gates diff the output), so no reference
// value is computed here except a row's @ref cross-check, the portable computation printed as
// agreement or disagreement beside the bytes. Nothing machine-dependent is printed.
// ---------------------------------------------------------------------------------------

static class Exercises
{
    static readonly Regex RefParam = new(@"\$(\d+)", RegexOptions.Compiled);

    // The scalar families keep their hand-written exercises in X86Sections / ArmSections.
    static readonly HashSet<string> HandWritten = new(StringComparer.Ordinal)
    {
        "System.Runtime.Intrinsics.X86.X86Base", "System.Runtime.Intrinsics.X86.Lzcnt",
        "System.Runtime.Intrinsics.X86.Popcnt", "System.Runtime.Intrinsics.X86.Bmi1",
        "System.Runtime.Intrinsics.X86.Bmi2", "System.Runtime.Intrinsics.X86.X86Serialize",
        "System.Runtime.Intrinsics.Arm.ArmBase", "System.Runtime.Intrinsics.Arm.Crc32",
    };

    // The buffer a pointer operand addresses, and its alignment: the aligned 512-bit loads and
    // stores fault below 64 bytes, and a stack allocation guarantees less; a 512-bit load,
    // store, masked or compressed form touches at most the 64 bytes. A gather's index lanes
    // are folded modulo GatherIndexModulus: every lane at scale 8 stays inside the buffer,
    // and the lane pattern's strides (0x1F3A7, 0x1F3A7C5D1B) are 1 modulo 5, so the folded
    // indices differ per lane.
    const int BufferBytes = 64;
    const int BufferAlign = 64;
    const int GatherIndexModulus = 5;

    public static string File(List<Family> families)
    {
        var sb = new StringBuilder();
        sb.Append(Emit.CsBanner("The exercise of every lowered family without a hand-written one: each mapped method called once with fixed inputs, its result printed as hex bytes; real .NET is the oracle."));
        sb.Append("""
            using System;
            using System.Collections.Generic;
            using System.Runtime.Intrinsics;
            using Arm = System.Runtime.Intrinsics.Arm;
            using Wasm = System.Runtime.Intrinsics.Wasm;
            using X86 = System.Runtime.Intrinsics.X86;

            namespace PlatformIsaProbe;

            internal static class Exercises
            {

            """);
        var tops = families.Where(f => f.Enclosing is null && f.Lowered && !HandWritten.Contains(f.QualifiedName)).ToList();
        foreach (string arch in Contract.Archs)
        {
            sb.Append($"    internal static void Register{Contract.IsaArch(arch)}(Dictionary<string, Action> exercises)\n    {{\n");
            foreach (var f in tops.Where(f => f.Arch == arch))
                sb.Append($"        exercises[\"{Contract.Display(f.QualifiedName)}\"] = {MethodName(f)};\n");
            sb.Append("    }\n\n");
        }
        foreach (var f in tops)
            Exercise(sb, families, f);
        sb.Append("}\n");
        return sb.ToString();
    }

    static string MethodName(Family f) => Contract.IsaArch(f.Arch) + f.TypePath.Replace("+", "");

    static string CsFamily(Family f) => Contract.IsaArch(f.Arch) + "." + f.TypePath.Replace('+', '.');

    // The label prefix below the top-level type: "" for it, "Arm64." for a nested type.
    static string LabelPrefix(Family f)
    {
        int plus = f.TypePath.IndexOf('+');
        return plus < 0 ? "" : f.TypePath.Substring(plus + 1).Replace('+', '.') + ".";
    }

    static void Exercise(StringBuilder sb, List<Family> families, Family f)
    {
        sb.Append($"    private static unsafe void {MethodName(f)}()\n    {{\n");
        Calls(sb, families, f, "        ");
        sb.Append("    }\n\n");
    }

    static void Calls(StringBuilder sb, List<Family> families, Family f, string indent)
    {
        bool witnessed = false;
        foreach (var m in f.Methods.Where(m => m.Row is not null).OrderBy(m => m.HelperName, StringComparer.Ordinal))
            Call(sb, f, m, indent, ref witnessed);
        foreach (var nested in families.Where(g => g.Enclosing == f.QualifiedName && g.Lowered && HasCalls(families, g)))
        {
            sb.Append($"{indent}if ({CsFamily(nested)}.IsSupported)\n{indent}{{\n");
            Calls(sb, families, nested, indent + "    ");
            sb.Append($"{indent}}}\n");
        }
    }

    static bool HasCalls(List<Family> families, Family f) =>
        f.Methods.Any(m => m.Row is not null) || families.Any(g => g.Enclosing == f.QualifiedName && g.Lowered && HasCalls(families, g));

    // One call is one statement; a call that needs buffers, a tuple result or a @ref
    // cross-check gets a block. With a cross-check the operands are bound to locals a<k>,
    // which the reference expression names through $k, and the line ends in ` ref=OK` or
    // ` ref=MISMATCH(<reference bytes>)`.
    static void Call(StringBuilder outer, Family f, Method m, string indent, ref bool witnessed)
    {
        var row = m.Row!;
        string member = f.QualifiedName + "." + m.Name;
        string label = LabelPrefix(f) + m.Key;
        var sb = new StringBuilder();
        string inner = indent + "    ";
        var args = new List<string>();
        var refArgs = new List<string>();
        var pointers = new List<int>();
        bool bind = row.Ref is not null;
        for (int k = 0; k < m.Params.Length; k++)
        {
            var p = m.Params[k];
            var imm = row.Imms.FirstOrDefault(i => i.Param == k);
            string value;
            if (imm is not null)
            {
                value = Literal(p, imm.Exercised);
                args.Add(value);
                refArgs.Add(value);
                continue;
            }
            switch (p.Kind)
            {
                case TyKind.Scalar:
                    value = Literal(p, Lane(k, 0, p.Code));
                    break;
                case TyKind.Vector:
                    value = VectorLiteral(p, k, IndexModulus(m, k));
                    break;
                case TyKind.Tuple:
                    value = "(" + string.Join(", ", p.Items.Select((it, j) => VectorLiteral(it, k * 4 + j, 0))) + ")";
                    break;
                case TyKind.Pointer:
                    sb.Append($"{inner}byte* raw{k} = stackalloc byte[{BufferBytes + BufferAlign}];\n");
                    sb.Append($"{inner}byte* p{k} = Fmt.Align(raw{k}, {BufferAlign});\n");
                    sb.Append($"{inner}Fmt.Fill(p{k}, {BufferBytes}, {k + 1});\n");
                    value = p.Elem is null ? $"(void*)p{k}" : $"({Codes.Cs(p.Elem.Code)}*)p{k}";
                    args.Add(value);
                    refArgs.Add($"({value})");
                    pointers.Add(k);
                    continue;
                default:
                    throw new ContractException($"{member}: parameter '{p}' has no exercise spelling");
            }
            if (bind)
            {
                sb.Append($"{inner}var a{k} = {value};\n");
                value = $"a{k}";
            }
            args.Add(value);
            refArgs.Add(value);
        }
        string call = $"{CsFamily(f)}.{m.Name}({string.Join(", ", args)})";
        string mem = m.Return.Kind == TyKind.Void
            ? string.Concat(pointers.Select(k => $" + \" mem{k}=\" + Fmt.Hex(p{k}, {BufferBytes})"))
            : "";
        if (bind)
        {
            string reference = RefParam.Replace(row.Ref!, x =>
            {
                int k = int.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture);
                if (k >= refArgs.Count)
                    throw new ContractException($"{row.SourceLine}: @ref names ${k} but {m.Key} has {refArgs.Count} parameter(s)");
                return refArgs[k];
            });
            string actual, expected;
            switch (m.Return.Kind)
            {
                case TyKind.Vector:
                    actual = "Fmt.Hex(r.AsByte())";
                    expected = "Fmt.Hex(q.AsByte())";
                    break;
                case TyKind.Scalar:
                    actual = ScalarHex(m.Return, "r");
                    expected = ScalarHex(m.Return, "q");
                    break;
                default:
                    throw new ContractException($"{row.SourceLine}: @ref needs a vector or scalar result; {m.Key} returns '{m.Return}'");
            }
            sb.Append($"{inner}var r = {call};\n");
            sb.Append($"{inner}var q = {reference};\n");
            sb.Append($"{inner}string h = {actual};\n");
            sb.Append($"{inner}Console.WriteLine(\"{label}=\" + h + Fmt.Ref(h, {expected}));\n");
        }
        else switch (m.Return.Kind)
        {
            case TyKind.Void:
                sb.Append($"{inner}{call};\n");
                sb.Append($"{inner}Console.WriteLine(\"{label}=void\"{mem});\n");
                break;
            case TyKind.Vector:
                sb.Append($"{inner}Console.WriteLine(\"{label}=\" + Fmt.Hex({call}.AsByte()));\n");
                break;
            case TyKind.Scalar:
                sb.Append($"{inner}Console.WriteLine(\"{label}=\" + {ScalarHex(m.Return, call)});\n");
                break;
            case TyKind.Tuple:
                sb.Append($"{inner}var r = {call};\n");
                sb.Append($"{inner}Console.WriteLine(\"{label}=\" + ");
                sb.Append(string.Join(" + \",\" + ", m.Return.Items.Select((it, j) =>
                    it.Kind == TyKind.Vector ? $"Fmt.Hex(r.Item{j + 1}.AsByte())" : ScalarHex(it, $"r.Item{j + 1}"))));
                sb.Append(");\n");
                break;
            default:
                throw new ContractException($"{member}: return type '{m.Return}' has no exercise spelling");
        }
        // One immediate method per family also witnesses the range check .NET inserts for a
        // non-constant immediate: one past the top of the range throws.
        if (!witnessed && row.Imms.Length == 1 && row.Imms[0].OutOfRange is { } past)
        {
            var imm = row.Imms[0];
            var outArgs = new List<string>(args);
            outArgs[imm.Param] = $"Fmt.NonConstant({Literal(m.Params[imm.Param], past)})";
            string outCall = $"{CsFamily(f)}.{m.Name}({string.Join(", ", outArgs)})";
            string stmt = m.Return.Kind == TyKind.Void ? $"{outCall};" : $"_ = {outCall};";
            sb.Append($"{inner}Console.WriteLine(\"{label} imm={past}=\" + Fmt.Thrown(() => {{ {stmt} }}));\n");
            witnessed = true;
        }
        string[] lines = sb.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 1)
            outer.Append(indent).Append(lines[0].TrimStart()).Append('\n');
        else
            outer.Append($"{indent}{{\n").Append(sb).Append($"{indent}}}\n");
    }

    static string ScalarHex(Ty t, string value) => t.Code switch
    {
        "i8" or "u8" => $"Fmt.Hex((byte){value})",
        "i16" or "u16" or "char" => $"Fmt.Hex((ushort){value})",
        "i32" or "u32" => $"Fmt.Hex((uint){value})",
        "i64" or "u64" or "nint" or "nuint" => $"Fmt.Hex((ulong){value})",
        "f32" => $"Fmt.Hex(BitConverter.SingleToUInt32Bits({value}))",
        "f64" => $"Fmt.Hex(BitConverter.DoubleToUInt64Bits({value}))",
        "bool" => $"Fmt.Bool({value})",
        _ => throw new ContractException($"scalar '{t}' has no exercise spelling"),
    };

    // Lane i of operand k: a fixed pattern that differs per operand and per lane, with
    // negative values for signed types and small exact fractions for floats.
    static long Lane(int k, int i, string code)
    {
        long v = (k + 1) * 0x1D + i * 0x25 + 7;
        switch (code)
        {
            case "i8": return (sbyte)(v & 0xFF);
            case "u8": return v & 0xFF;
            case "i16": return (short)((v * 0x1F3) & 0xFFFF);
            case "u16": return (v * 0x1F3) & 0xFFFF;
            case "i32": return (int)((v * 0x1F3A7) & 0xFFFFFFFF);
            case "u32": return (v * 0x1F3A7) & 0xFFFFFFFF;
            case "i64": return v * 0x1F3A7C5D1BL;
            case "u64": return v * 0x1F3A7C5D1BL;
            case "nint": return (int)((v * 0x1F3A7) & 0xFFFFFFFF);
            case "nuint": return (v * 0x1F3A7) & 0xFFFFFFFF;
            case "f32":
            case "f64": return (v % 41) - 20;
            case "bool": return v & 1;
            case "char": return 0x41 + (v % 26);
            default: throw new ContractException($"'{code}' has no exercise lane value");
        }
    }

    static string Literal(Ty t, long v)
    {
        string s = t.Code switch
        {
            "i8" => $"(sbyte){v}",
            "u8" => $"(byte){v}",
            "i16" => $"(short){v}",
            "u16" => $"(ushort){v}",
            "i32" => v.ToString(CultureInfo.InvariantCulture),
            "u32" => v.ToString(CultureInfo.InvariantCulture) + "U",
            "i64" => v.ToString(CultureInfo.InvariantCulture) + "L",
            "u64" => ((ulong)v).ToString(CultureInfo.InvariantCulture) + "UL",
            "nint" => $"(nint){v}",
            "nuint" => $"(nuint){v}",
            "f32" => (v * 0.25).ToString("R", CultureInfo.InvariantCulture) + (v % 4 == 0 ? ".0f" : "f"),
            "f64" => (v * 0.125).ToString("R", CultureInfo.InvariantCulture) + (v % 8 == 0 ? ".0" : ""),
            "bool" => v != 0 ? "true" : "false",
            "char" => $"(char){v}",
            _ => throw new ContractException($"'{t}' has no exercise literal"),
        };
        return t.CsEnum is null ? s : $"({t.CsEnum}){s}";
    }

    // The index operand of a table lookup (the last parameter of VectorTableLookup*, the
    // second of Swizzle) is read as byte indices into 16 bytes per table register; the
    // fixed lane pattern is folded to a few values past the table so most lanes select an
    // entry and the rest exercise the out-of-range lane (zero for TBL and swizzle, the
    // default operand for TBX).
    static int IndexModulus(Method m, int k)
    {
        Debug.Assert((GatherIndexModulus - 1) * 8 + 8 <= BufferBytes, "a gathered lane at scale 8 must stay inside the buffer");
        if (m.Name == "Swizzle")
            return k == 1 ? 16 + 3 : 0;
        // A gather's index lanes are offsets from the base pointer at the scale exercised.
        if (m.Name.StartsWith("Gather", StringComparison.Ordinal))
            return k == (m.Name.StartsWith("GatherMask", StringComparison.Ordinal) ? 2 : 1) ? GatherIndexModulus : 0;
        if (!m.Name.StartsWith("VectorTableLookup", StringComparison.Ordinal) || k != m.Params.Length - 1)
            return 0;
        var table = m.Params[m.Name.EndsWith("Extension", StringComparison.Ordinal) ? 1 : 0];
        int registers = table.Kind == TyKind.Tuple ? table.Items.Length : 1;
        return 16 * registers + 3;
    }

    // Vector128.Create has no native-integer lane overload, so those lanes are created as
    // the 32-bit integers they are on wasm32 and reinterpreted.
    static string VectorLiteral(Ty v, int k, int modulus)
    {
        string elem = v.Elem!.Code;
        int lanes = v.Bits / Codes.ElemBits(elem);
        var created = elem switch { "nint" => Codes.ScalarOf("i32"), "nuint" => Codes.ScalarOf("u32"), _ => v.Elem };
        var items = Enumerable.Range(0, lanes).Select(i =>
        {
            long value = Lane(k, i, elem);
            return Literal(created, modulus == 0 ? value : ((value % modulus) + modulus) % modulus);
        });
        string vector = $"Vector{v.Bits}.Create({string.Join(", ", items)})";
        return elem switch { "nint" => vector + ".AsNInt()", "nuint" => vector + ".AsNUInt()", _ => vector };
    }
}
