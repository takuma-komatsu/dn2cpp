// gen-isa-map.cs — regenerates the hardware-intrinsics family contract from the public
// surface of System.Private.CoreLib. Outputs, all checked in:
//
//   runtime/core/isa/dn2cpp_isa_tokens.g.h                  one IsSupported token per family
//   runtime/core/isa/dn2cpp_isa_families.g.h                includes every generated family header
//   runtime/core/isa/dn2cpp_isa_manifest.txt                every lowered helper name, sorted
//   runtime/core/isa/<arch>/dn2cpp_isa_<arch>_<family>.h    one per family that has map rows
//   src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs   the family table the transpiler reads
//
//   Run from the repository root:
//     dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll> [--check] [--root <repo>]
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
// the metadata set, so a CoreLib update that adds or removes a family fails here first.
//
// `Lowered` is derived, never edited: a family is lowered when every public static method it
// declares has a map row and, for a nested family, its enclosing family is lowered too, so
// Sse3.X64.IsSupported can never report true while Sse3 calls are still unlowered. A family
// with no methods of its own is vacuously covered; a family with no feature bits (Sve, Sve2)
// is never lowered, whatever its maps say.
//
// MAP GRAMMAR — tools/gen-isa-map/map/<arch>/<familypath>.map, one file per family, where
// <familypath> is the lowercase type path below the arch namespace with '+' as '_'
// (sse2.map, sse2_x64.map, avx512f_vl.map, advsimd_arm64.map, packedsimd.map).
//
//   # comment                                blank lines and '#' lines are ignored
//   target = sse4.2                          family-level default for DN2CPP_ISA_TARGET(...)
//   Method(codes) [@ann ...] = expression    exact row: codes are the helper-name argument codes
//   Method(v128{T},v128{T}) = expr for T in i8,u8,i16
//                                            pattern row: {T} in the codes and the expression is
//                                            replaced per listed element code; the expression may
//                                            also use {epi} {ps|pd} {neon} {lane}, spelled per T
//                                            by the tables at the bottom of this file
//
//   In the expression, $0 $1 ... name the parameters. A vector parameter arrives converted to
//   the arch's native vector type (dn2cpp_isa_bits<...>); a vector return is wrapped back
//   (dn2cpp_isa_vec<...>). $k.j names item j (1-based) of a tuple parameter k; $r1 $r2 ... name
//   the out-pointer items of a tuple return, already dereferenced. Scalars and pointers pass
//   through unchanged.
//
//   Annotations, written between the closing ')' and '=':
//     @imm8            the LAST parameter is an immediate in [0..256); the body becomes
//                      DN2CPP_ISA_IMM8_SWITCH(<param>, <expression>) and the expression refers
//                      to the immediate as DN2CPP_IMM (its $k is rewritten to that name)
//     @imm[0..N)       same, in [0..N) for N in 2,4,8,16,32,64 (a lane or element index),
//                      through DN2CPP_ISA_IMM_SWITCH_N(N, <param>, <expression>)
//     @target("isa")   per-row DN2CPP_ISA_TARGET override of the family-level `target =`
//     @throws          documentation only: the intrinsic can raise (fault, #UD); no code effect
//
// Every helper is emitted inside `#if DN2CPP_TARGET_<ARCH>` with the real body, and `#else`
// with a [[noreturn]] stub calling dn2cpp_isa_not_lowered("<QualifiedName>.<Method>"), so a
// foreign-arch dead arm in generated code still compiles against a declaration.

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
        default:
            Console.Error.WriteLine($"error: unknown or incomplete argument '{args[i]}'");
            Console.Error.WriteLine("usage: dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll> [--check] [--root <repo>]");
            return 2;
    }
}
if (corelibPath is null || !File.Exists(corelibPath))
{
    Console.Error.WriteLine("error: --corelib <System.Private.CoreLib.dll> is required and must exist");
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
    var families = Csv.Read(Path.Combine(toolDir, "families.csv"));
    using var pe = new PEReader(File.OpenRead(corelibPath));
    var md = pe.GetMetadataReader();
    Surface.Populate(md, families);
    Maps.Apply(toolDir, families);
    // Enclosing families precede nested ones (Csv.CheckOrder), so one forward pass settles Lowered.
    var loweredByName = new Dictionary<string, bool>(StringComparer.Ordinal);
    foreach (var f in families)
    {
        f.Lowered = f.Covered && (f.Enclosing is null || loweredByName[f.Enclosing]);
        loweredByName[f.QualifiedName] = f.Lowered;
    }

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
    int lowered = families.Count(f => f.Lowered);
    Console.Error.WriteLine($"  corelib    {version}");
    Console.Error.WriteLine($"  families   {families.Count} ({families.Count(f => f.NeverLowered)} never lowered)");
    Console.Error.WriteLine($"  methods    {methods} ({lowerable} lowerable, {mapped} mapped)");
    Console.Error.WriteLine($"  lowered    {lowered} family(ies), {families.Count(f => f.HasRows)} with helper headers");
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

    public static string ArchOfNamespace(string ns) => ns switch
    {
        IntrinsicsPrefix + "X86" => "x86",
        IntrinsicsPrefix + "Arm" => "arm",
        IntrinsicsPrefix + "Wasm" => "wasm",
        _ => throw new ContractException($"namespace '{ns}' is not an intrinsics arch namespace"),
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

    // The feature-bit enumerators the runtime's detector defines, without the DN2CPP_CPU_
    // prefix that families.csv omits. A typo in the csv fails here rather than in a C++ build.
    public static readonly HashSet<string> Bits = new(StringComparer.Ordinal)
    {
        "X86_X86BASE", "X86_SSE", "X86_SSE2", "X86_SSE3", "X86_SSSE3", "X86_SSE41", "X86_SSE42",
        "X86_POPCNT", "X86_AES", "X86_VAES", "X86_PCLMULQDQ", "X86_VPCLMULQDQ", "X86_LZCNT",
        "X86_AVX", "X86_FMA", "X86_AVX2", "X86_BMI1", "X86_BMI2", "X86_AVXVNNI", "X86_AVXVNNIINT8",
        "X86_AVXVNNIINT16", "X86_AVX512", "X86_AVX512VBMI", "X86_AVX512VBMI2", "X86_GFNI",
        "X86_AVX10V1", "X86_AVX10V1_512", "X86_AVX10V2", "X86_AVX10V2_512", "X86_X86SERIALIZE",
        "ARM_ARMBASE", "ARM_ADVSIMD", "ARM_AES", "ARM_CRC32", "ARM_SHA1", "ARM_SHA256", "ARM_DP", "ARM_RDM",
        "WASM_PACKEDSIMD",
    };

    public static string Token(string qualifiedName) =>
        "DN2CPP_ISA_" + qualifiedName.Substring(IntrinsicsPrefix.Length).Replace('.', '_').Replace('+', '_');

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
    public bool HasRows => Methods.Any(m => m.Row is not null);
    public bool Covered => !NeverLowered && Methods.All(m => m.Row is not null);
    public bool Lowered;
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

sealed record MapRow(string Expression, string? Target, int ImmRange /* 0 = none */, string SourceLine);

enum TyKind { Void, Scalar, Vector, Pointer, ByRef, Tuple, VectorDef, TupleDef, Unsupported }

// A decoded signature type. Scalar carries the helper-name code and the C++ spelling; Vector
// carries the width in bits and its scalar element; Pointer/ByRef wrap an element (null for
// void*); Tuple carries its items. VectorDef/TupleDef are the open generic markers the
// signature provider hands back before instantiation, and Unsupported names what it could
// not model so the failure message points at the offending member.
sealed record Ty(TyKind Kind, string Code = "", string Cpp = "", int Bits = 0, Ty? Elem = null, ImmutableArray<Ty> Items = default, string Describe = "")
{
    public static readonly Ty Void = new(TyKind.Void, Describe: "void");
    public static Ty Scalar(string code, string cpp) => new(TyKind.Scalar, code, cpp, Describe: code);
    public static Ty Unsupported(string what) => new(TyKind.Unsupported, Describe: what);

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
    public static List<Family> Read(string path)
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
                if (!Contract.Bits.Contains(b))
                    throw new ContractException($"{path}: unknown feature bit '{b}' in '{line}'");
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
            string[] codes = f.NeverLowered ? Array.Empty<string>() : sig.ParameterTypes.Select(p => Codes.Arg(p, member)).ToArray();
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
        PrimitiveTypeCode.SByte => Ty.Scalar("i8", "int8_t"),
        PrimitiveTypeCode.Byte => Ty.Scalar("u8", "uint8_t"),
        PrimitiveTypeCode.Int16 => Ty.Scalar("i16", "int16_t"),
        PrimitiveTypeCode.UInt16 => Ty.Scalar("u16", "uint16_t"),
        PrimitiveTypeCode.Int32 => Ty.Scalar("i32", "int32_t"),
        PrimitiveTypeCode.UInt32 => Ty.Scalar("u32", "uint32_t"),
        PrimitiveTypeCode.Int64 => Ty.Scalar("i64", "int64_t"),
        PrimitiveTypeCode.UInt64 => Ty.Scalar("u64", "uint64_t"),
        PrimitiveTypeCode.Single => Ty.Scalar("f32", "float"),
        PrimitiveTypeCode.Double => Ty.Scalar("f64", "double"),
        PrimitiveTypeCode.IntPtr => Ty.Scalar("nint", "intptr_t"),
        PrimitiveTypeCode.UIntPtr => Ty.Scalar("nuint", "uintptr_t"),
        PrimitiveTypeCode.Boolean => Ty.Scalar("bool", "bool"),
        PrimitiveTypeCode.Char => Ty.Scalar("char", "uint16_t"),
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
                return f.DecodeSignature(this, null);
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
// Argument codes and C++ spellings.
// ---------------------------------------------------------------------------------------

static class Codes
{
    // The helper-name code of a parameter. The surface is finite, so anything outside the
    // vocabulary is a contract change and fails naming the member.
    public static string Arg(Ty t, string member)
    {
        switch (t.Kind)
        {
            case TyKind.Scalar:
                return t.Code;
            case TyKind.Vector:
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
                // Only homogeneous vector tuples occur (multi-register stores); the code is the
                // arity followed by the shared item code.
                if (t.Items.Length >= 2 && t.Items.All(i => i.Kind == TyKind.Vector && i.ToString() == t.Items[0].ToString()))
                    return $"t{t.Items.Length}{Arg(t.Items[0], member)}";
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
                return $"{Neon[e]}x{v.Bits / ElemBits(e)}_t";
            default:
                if (v.Bits != 128)
                    throw new ContractException($"wasm has no native {v.Bits}-bit vector");
                return "v128_t";
        }
    }

    public static int ElemBits(string code) => code switch
    {
        "i8" or "u8" => 8,
        "i16" or "u16" => 16,
        "i32" or "u32" or "f32" => 32,
        "i64" or "u64" or "f64" => 64,
        _ => throw new ContractException($"'{code}' is not a vector element code"),
    };

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
    static readonly Regex ForClause = new(@"\s+for\s+T\s+in\s+([a-z0-9]+(?:\s*,\s*[a-z0-9]+)*)\s*$", RegexOptions.Compiled);
    static readonly Regex ImmRange = new(@"^@imm\[0\.\.(\d+)\)$", RegexOptions.Compiled);
    static readonly Regex TargetAnn = new(@"^@target\(""([^""]+)""\)$", RegexOptions.Compiled);
    // Only identifier-like braces are placeholders, so C++ brace initializers in an
    // expression ({ $1.1, $1.2 } for a NEON multi-register struct) pass through.
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
        ["neon"] = new()
        {
            ["i8"] = "s8", ["u8"] = "u8", ["i16"] = "s16", ["u16"] = "u16",
            ["i32"] = "s32", ["u32"] = "u32", ["i64"] = "s64", ["u64"] = "u64",
            ["f32"] = "f32", ["f64"] = "f64",
        },
        ["lane"] = new()
        {
            ["i8"] = "i8x16", ["u8"] = "u8x16", ["i16"] = "i16x8", ["u16"] = "u16x8",
            ["i32"] = "i32x4", ["u32"] = "u32x4", ["i64"] = "i64x2", ["u64"] = "u64x2",
            ["f32"] = "f32x4", ["f64"] = "f64x2",
        },
    };

    public static void Apply(string toolDir, List<Family> families)
    {
        var byPath = new Dictionary<string, Family>(StringComparer.Ordinal);
        foreach (var f in families)
            byPath[$"{f.Arch}/{f.HelperPath}.map"] = f;

        string mapDir = Path.Combine(toolDir, "map");
        if (!Directory.Exists(mapDir))
            return;
        foreach (string file in Directory.GetFiles(mapDir, "*.map", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(mapDir, file).Replace('\\', '/');
            if (!byPath.TryGetValue(rel, out var family))
                throw new ContractException($"{file}: no family has the map path '{rel}'");
            if (family.NeverLowered)
                throw new ContractException($"{file}: {family.QualifiedName} has no feature bits and is never lowered");
            Parse(file, family);
        }
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
                family.Target = line.Substring(eq + 1).Trim();
                if (family.Target.Length == 0)
                    throw new ContractException($"{where}: empty target");
                continue;
            }

            var m = RowStart.Match(line);
            if (!m.Success)
                throw new ContractException($"{where}: expected 'Method(codes) [@annotation ...] = expression'");
            string method = m.Groups[1].Value;
            string[] codes = m.Groups[2].Value.Length == 0
                ? Array.Empty<string>()
                : m.Groups[2].Value.Split(',').Select(c => c.Trim()).ToArray();
            string rest = m.Groups[3].Value;

            int immRange = 0;
            string? target = null;
            while (rest.StartsWith('@'))
            {
                int end = rest.IndexOf(' ');
                string ann = end < 0 ? rest : rest.Substring(0, end);
                rest = end < 0 ? "" : rest.Substring(end + 1).TrimStart();
                if (ann == "@imm8")
                    immRange = 256;
                else if (ImmRange.Match(ann) is { Success: true } r)
                {
                    immRange = int.Parse(r.Groups[1].Value, CultureInfo.InvariantCulture);
                    if (immRange is not (2 or 4 or 8 or 16 or 32 or 64))
                        throw new ContractException($"{where}: DN2CPP_ISA_IMM_SWITCH_N dispatches 2, 4, 8, 16, 32 or 64 values, not {immRange}");
                }
                else if (TargetAnn.Match(ann) is { Success: true } t)
                    target = t.Groups[1].Value;
                else if (ann != "@throws")
                    throw new ContractException($"{where}: unknown annotation '{ann}'");
            }
            if (!rest.StartsWith('='))
                throw new ContractException($"{where}: expected '=' after the signature and annotations");
            string expression = rest.Substring(1).Trim();
            if (expression.Length == 0)
                throw new ContractException($"{where}: empty expression");

            string[] elems = Array.Empty<string>();
            var fc = ForClause.Match(expression);
            if (fc.Success)
            {
                elems = fc.Groups[1].Value.Split(',').Select(e => e.Trim()).ToArray();
                expression = expression.Substring(0, fc.Index).TrimEnd();
            }

            bool isPattern = codes.Any(c => c.Contains("{T}")) || Placeholder.IsMatch(expression);
            if (isPattern && elems.Length == 0)
                throw new ContractException($"{where}: a row with {{...}} placeholders needs 'for T in ...'");
            if (!isPattern && elems.Length > 0)
                throw new ContractException($"{where}: 'for T in ...' on a row without {{T}} placeholders");

            foreach (string elem in elems.Length == 0 ? new[] { "" } : elems)
            {
                string[] cs = codes.Select(c => c.Replace("{T}", elem)).ToArray();
                string key = method + "(" + string.Join(",", cs) + ")";
                if (!byKey.TryGetValue(key, out var mm))
                {
                    var overloads = family.Methods.Where(x => x.Name == method).Select(x => x.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
                    string hint = overloads.Count == 0 ? "" : "; its overloads are " + string.Join(" ", overloads);
                    throw new ContractException($"{where}: {family.QualifiedName} has no method {key}{hint}");
                }
                if (mm.Row is not null)
                    throw new ContractException($"{where}: {key} already has a row ({mm.Row.SourceLine})");
                if (immRange != 0)
                {
                    if (mm.Params.Length == 0 || mm.Params[^1].Kind != TyKind.Scalar)
                        throw new ContractException($"{where}: @imm needs a scalar last parameter on {key}");
                }
                string expr = Substitute(expression, elem, where);
                mm.Row = new MapRow(expr, target, immRange, where);
            }
        }
    }

    static string Substitute(string expression, string elem, string where) =>
        Placeholder.Replace(expression, m =>
        {
            string name = m.Groups[1].Value;
            if (name == "T")
                return elem;
            if (!Tables.TryGetValue(name, out var table))
                throw new ContractException($"{where}: unknown placeholder '{{{name}}}'");
            if (!table.TryGetValue(elem, out string? spelled))
                throw new ContractException($"{where}: '{{{name}}}' has no spelling for element code '{elem}'");
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

    static readonly Regex OutItem = new(@"\$r(\d+)", RegexOptions.Compiled);
    static readonly Regex Param = new(@"\$(\d+)(?:\.(\d+))?", RegexOptions.Compiled);

    public static Dictionary<string, string> All(List<Family> families)
    {
        var outputs = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["runtime/core/isa/dn2cpp_isa_tokens.g.h"] = Tokens(families),
            ["runtime/core/isa/dn2cpp_isa_families.g.h"] = FamiliesHeader(families),
            ["runtime/core/isa/dn2cpp_isa_manifest.txt"] = Manifest(families),
            ["src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs"] = FamilyTable(families),
        };
        foreach (var f in families.Where(f => f.HasRows))
            outputs[f.HeaderRelPath] = FamilyHeader(f);
        return outputs;
    }

    static string CppBanner(string what) => $"""
        #pragma once
        // GENERATED FILE — do not edit by hand.
        //
        // {what}
        // Regenerate from System.Private.CoreLib with:
        //
        //     {Regenerate}
        //

        """;

    static string Tokens(List<Family> families)
    {
        var sb = new StringBuilder();
        sb.Append(CppBanner("One IsSupported token per hardware-intrinsics family, as the transpiler emits it."));
        sb.Append("""
            // On the family's own target the token asks the feature detector for every bit the family
            // requires; on every other target it is the literal 0, so the guarded arm is dead code the
            // compiler drops. Nested X64/Arm64 types share their enclosing type's bits: every target
            // here is 64-bit.
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
                    sb.Append("#define ").Append(f.Token).Append(" (dn2cpp_cpu_has_all(")
                      .Append(string.Join(" | ", f.Bits.Select(b => "DN2CPP_CPU_" + b))).Append("))\n");
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

    static string Manifest(List<Family> families)
    {
        var names = families.SelectMany(f => f.Methods.Where(m => m.Row is not null).Select(m => m.HelperName))
            .OrderBy(n => n, StringComparer.Ordinal);
        var sb = new StringBuilder();
        foreach (string n in names)
            sb.Append(n).Append('\n');
        return sb.ToString();
    }

    static string FamilyTable(List<Family> families)
    {
        var sb = new StringBuilder();
        sb.Append("""
            // <auto-generated/>
            // Generated by tools/gen-isa-map from System.Private.CoreLib; regenerate with
            //   dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib <System.Private.CoreLib.dll>
            // Lowered is computed from map coverage and is never edited by hand.
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
        foreach (var m in f.Methods.Where(m => m.Row is not null).OrderBy(m => m.HelperName, StringComparer.Ordinal))
            Helper(sb, f, m);
        return sb.ToString();
    }

    // A helper is declared twice with one signature: the real body under the arch's target
    // macro, and a [[noreturn]] stub otherwise, so call sites in foreign-arch dead arms compile.
    static void Helper(StringBuilder sb, Family f, Method m)
    {
        var row = m.Row!;
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

        int immIndex = row.ImmRange != 0 ? m.Params.Length - 1 : -1;
        string expr = OutItem.Replace(row.Expression, x => "(*item" + x.Groups[1].Value + ")");
        expr = Param.Replace(expr, x =>
        {
            int k = int.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture);
            if (k >= m.Params.Length)
                throw new ContractException($"{row.SourceLine}: ${k} but {m.Key} has {m.Params.Length} parameter(s)");
            var p = m.Params[k];
            if (k == immIndex)
                return "DN2CPP_IMM";
            if (p.Kind == TyKind.Tuple)
            {
                if (!x.Groups[2].Success)
                    throw new ContractException($"{row.SourceLine}: ${k} is a tuple; name an item as ${k}.<n>");
                int j = int.Parse(x.Groups[2].Value, CultureInfo.InvariantCulture);
                if (j < 1 || j > p.Items.Length)
                    throw new ContractException($"{row.SourceLine}: ${k}.{j} is outside the tuple's items");
                return $"dn2cpp_isa_bits<{Codes.Native(f.Arch, p.Items[j - 1])}>(a{k}_{j})";
            }
            if (x.Groups[2].Success)
                throw new ContractException($"{row.SourceLine}: ${k} is not a tuple");
            return p.Kind == TyKind.Vector ? $"dn2cpp_isa_bits<{Codes.Native(f.Arch, p)}>(a{k})" : $"a{k}";
        });

        string produce = m.Return.Kind switch
        {
            TyKind.Vector => $"dn2cpp_isa_vec<{m.Return.Bits / 8}>({expr})",
            _ => expr,
        };
        string body;
        if (row.ImmRange != 0)
        {
            body = row.ImmRange == 256
                ? $"DN2CPP_ISA_IMM8_SWITCH(a{immIndex}, {produce});"
                : $"DN2CPP_ISA_IMM_SWITCH_N({row.ImmRange}, a{immIndex}, {produce});";
        }
        else
        {
            body = m.Return.Kind is TyKind.Void or TyKind.Tuple ? produce + ";" : "return " + produce + ";";
        }

        string target = row.Target ?? f.Target ?? "";
        string attr = target.Length > 0 ? $"DN2CPP_ISA_TARGET(\"{target}\") " : "";
        string display = f.QualifiedName + "." + m.Name;

        sb.Append('\n');
        sb.Append("#if ").Append(Contract.TargetMacro(f.Arch)).Append('\n');
        sb.Append(attr).Append("DN2CPP_ISA_INLINE ").Append(ret).Append(' ').Append(m.HelperName)
          .Append('(').Append(string.Join(", ", paramDecls)).Append(")\n{\n    ").Append(body).Append("\n}\n");
        sb.Append("#else\n");
        sb.Append("[[noreturn]] DN2CPP_ISA_INLINE ").Append(ret).Append(' ').Append(m.HelperName)
          .Append('(').Append(string.Join(", ", stubDecls)).Append(")\n{\n    dn2cpp_isa_not_lowered(\"")
          .Append(display).Append("\");\n}\n");
        sb.Append("#endif\n");
    }
}
