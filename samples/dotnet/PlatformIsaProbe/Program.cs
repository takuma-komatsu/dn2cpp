using System;
using System.Collections.Generic;
using System.Globalization;
using Arm = System.Runtime.Intrinsics.Arm;
using Wasm = System.Runtime.Intrinsics.Wasm;
using X86 = System.Runtime.Intrinsics.X86;

namespace PlatformIsaProbe;

// The platform-ISA capability contract, diffed against real .NET.
//
//   PlatformIsaProbe [selection]
//
// selection: comma-separated contract row names (X86.Lzcnt.X64), an arch name
// (X86 / Arm / Wasm) for every row of that arch, `all`, or absent (= all).
// Output order follows the selection; an arch or `all` expands in the table's
// landing order below.
//
// The contract block prints every selected row's IsSupported. Then each
// selected top-level family gets a section: when supported it runs the
// family's exercise (registered by the *Sections classes; a family without
// one prints a header-only section), otherwise it calls one representative
// instruction, which must throw PlatformNotSupportedException as it does in
// .NET. A successful probe prints nothing.
//
// Never print Vector128/256/512.IsHardwareAccelerated or Vector<T>.Count: they
// flip under DOTNET_EnableHWIntrinsic=0, which is the oracle for the masked run.
internal static class Program
{
    private sealed class Family
    {
        public readonly string Arch;
        public readonly string Name;
        public readonly Func<bool> IsSupported;
        public readonly Action Probe;
        public readonly (string Name, Func<bool> IsSupported)[] Nested;

        public Family(string arch, string name, Func<bool> isSupported, Action probe,
            params (string, Func<bool>)[] nested)
        {
            Arch = arch;
            Name = name;
            IsSupported = isSupported;
            Probe = probe;
            Nested = nested;
        }

        public string RowName => Arch + "." + Name;
    }

    private readonly struct Row
    {
        public readonly string Name;
        public readonly Func<bool> IsSupported;
        public readonly Family TopLevel;

        public Row(string name, Func<bool> isSupported, Family topLevel)
        {
            Name = name;
            IsSupported = isSupported;
            TopLevel = topLevel;
        }
    }

    // Landing order: each top-level type immediately followed by its nested
    // types in ordinal order. This order IS the contract table's order.
    private static readonly Family[] Table =
    {
        new("X86", "X86Base", () => X86.X86Base.IsSupported, X86Sections.ProbeX86Base,
            ("X64", () => X86.X86Base.X64.IsSupported)),
        new("X86", "Lzcnt", () => X86.Lzcnt.IsSupported, X86Sections.ProbeLzcnt,
            ("X64", () => X86.Lzcnt.X64.IsSupported)),
        new("X86", "Popcnt", () => X86.Popcnt.IsSupported, X86Sections.ProbePopcnt,
            ("X64", () => X86.Popcnt.X64.IsSupported)),
        new("X86", "Bmi1", () => X86.Bmi1.IsSupported, X86Sections.ProbeBmi1,
            ("X64", () => X86.Bmi1.X64.IsSupported)),
        new("X86", "Bmi2", () => X86.Bmi2.IsSupported, X86Sections.ProbeBmi2,
            ("X64", () => X86.Bmi2.X64.IsSupported)),
        new("X86", "X86Serialize", () => X86.X86Serialize.IsSupported, X86Sections.ProbeX86Serialize,
            ("X64", () => X86.X86Serialize.X64.IsSupported)),
        new("Arm", "ArmBase", () => Arm.ArmBase.IsSupported, ArmSections.ProbeArmBase,
            ("Arm64", () => Arm.ArmBase.Arm64.IsSupported)),
        new("Arm", "Crc32", () => Arm.Crc32.IsSupported, ArmSections.ProbeCrc32,
            ("Arm64", () => Arm.Crc32.Arm64.IsSupported)),
        new("Arm", "AdvSimd", () => Arm.AdvSimd.IsSupported, ArmSections.ProbeAdvSimd,
            ("Arm64", () => Arm.AdvSimd.Arm64.IsSupported)),
        new("Arm", "Aes", () => Arm.Aes.IsSupported, ArmSections.ProbeAes,
            ("Arm64", () => Arm.Aes.Arm64.IsSupported)),
        new("Arm", "Sha1", () => Arm.Sha1.IsSupported, ArmSections.ProbeSha1,
            ("Arm64", () => Arm.Sha1.Arm64.IsSupported)),
        new("Arm", "Sha256", () => Arm.Sha256.IsSupported, ArmSections.ProbeSha256,
            ("Arm64", () => Arm.Sha256.Arm64.IsSupported)),
        new("Arm", "Dp", () => Arm.Dp.IsSupported, ArmSections.ProbeDp,
            ("Arm64", () => Arm.Dp.Arm64.IsSupported)),
        new("Arm", "Rdm", () => Arm.Rdm.IsSupported, ArmSections.ProbeRdm,
            ("Arm64", () => Arm.Rdm.Arm64.IsSupported)),
        new("Wasm", "PackedSimd", () => Wasm.PackedSimd.IsSupported, WasmSections.ProbePackedSimd),
        new("X86", "Sse", () => X86.Sse.IsSupported, X86Sections.ProbeSse,
            ("X64", () => X86.Sse.X64.IsSupported)),
        new("X86", "Sse2", () => X86.Sse2.IsSupported, X86Sections.ProbeSse2,
            ("X64", () => X86.Sse2.X64.IsSupported)),
        new("X86", "Sse3", () => X86.Sse3.IsSupported, X86Sections.ProbeSse3,
            ("X64", () => X86.Sse3.X64.IsSupported)),
        new("X86", "Ssse3", () => X86.Ssse3.IsSupported, X86Sections.ProbeSsse3,
            ("X64", () => X86.Ssse3.X64.IsSupported)),
        new("X86", "Sse41", () => X86.Sse41.IsSupported, X86Sections.ProbeSse41,
            ("X64", () => X86.Sse41.X64.IsSupported)),
        new("X86", "Sse42", () => X86.Sse42.IsSupported, X86Sections.ProbeSse42,
            ("X64", () => X86.Sse42.X64.IsSupported)),
        new("X86", "Pclmulqdq", () => X86.Pclmulqdq.IsSupported, X86Sections.ProbePclmulqdq,
            ("V256", () => X86.Pclmulqdq.V256.IsSupported),
            ("V512", () => X86.Pclmulqdq.V512.IsSupported),
            ("X64", () => X86.Pclmulqdq.X64.IsSupported)),
        new("X86", "Aes", () => X86.Aes.IsSupported, X86Sections.ProbeAes,
            ("X64", () => X86.Aes.X64.IsSupported)),
        new("X86", "Avx", () => X86.Avx.IsSupported, X86Sections.ProbeAvx,
            ("X64", () => X86.Avx.X64.IsSupported)),
        new("X86", "Avx2", () => X86.Avx2.IsSupported, X86Sections.ProbeAvx2,
            ("X64", () => X86.Avx2.X64.IsSupported)),
        new("X86", "Fma", () => X86.Fma.IsSupported, X86Sections.ProbeFma,
            ("X64", () => X86.Fma.X64.IsSupported)),
        new("X86", "AvxVnni", () => X86.AvxVnni.IsSupported, X86Sections.ProbeAvxVnni,
            ("X64", () => X86.AvxVnni.X64.IsSupported)),
        new("X86", "Avx512F", () => X86.Avx512F.IsSupported, X86Sections.ProbeAvx512F,
            ("VL", () => X86.Avx512F.VL.IsSupported),
            ("X64", () => X86.Avx512F.X64.IsSupported)),
        new("X86", "Avx512BW", () => X86.Avx512BW.IsSupported, X86Sections.ProbeAvx512BW,
            ("VL", () => X86.Avx512BW.VL.IsSupported),
            ("X64", () => X86.Avx512BW.X64.IsSupported)),
        new("X86", "Avx512CD", () => X86.Avx512CD.IsSupported, X86Sections.ProbeAvx512CD,
            ("VL", () => X86.Avx512CD.VL.IsSupported),
            ("X64", () => X86.Avx512CD.X64.IsSupported)),
        new("X86", "Avx512DQ", () => X86.Avx512DQ.IsSupported, X86Sections.ProbeAvx512DQ,
            ("VL", () => X86.Avx512DQ.VL.IsSupported),
            ("X64", () => X86.Avx512DQ.X64.IsSupported)),
        new("X86", "Avx512Vbmi", () => X86.Avx512Vbmi.IsSupported, X86Sections.ProbeAvx512Vbmi,
            ("VL", () => X86.Avx512Vbmi.VL.IsSupported),
            ("X64", () => X86.Avx512Vbmi.X64.IsSupported)),
        new("X86", "Avx512Vbmi2", () => X86.Avx512Vbmi2.IsSupported, X86Sections.ProbeAvx512Vbmi2,
            ("VL", () => X86.Avx512Vbmi2.VL.IsSupported),
            ("X64", () => X86.Avx512Vbmi2.X64.IsSupported)),
        new("X86", "Avx10v1", () => X86.Avx10v1.IsSupported, X86Sections.ProbeAvx10v1,
            ("V512", () => X86.Avx10v1.V512.IsSupported),
            ("V512.X64", () => X86.Avx10v1.V512.X64.IsSupported),
            ("X64", () => X86.Avx10v1.X64.IsSupported)),
        new("X86", "Avx10v2", () => X86.Avx10v2.IsSupported, X86Sections.ProbeAvx10v2,
            ("V512", () => X86.Avx10v2.V512.IsSupported),
            ("V512.X64", () => X86.Avx10v2.V512.X64.IsSupported),
            ("X64", () => X86.Avx10v2.X64.IsSupported)),
        new("X86", "AvxVnniInt8", () => X86.AvxVnniInt8.IsSupported, X86Sections.ProbeAvxVnniInt8,
            ("V512", () => X86.AvxVnniInt8.V512.IsSupported),
            ("X64", () => X86.AvxVnniInt8.X64.IsSupported)),
        new("X86", "AvxVnniInt16", () => X86.AvxVnniInt16.IsSupported, X86Sections.ProbeAvxVnniInt16,
            ("V512", () => X86.AvxVnniInt16.V512.IsSupported),
            ("X64", () => X86.AvxVnniInt16.X64.IsSupported)),
        new("X86", "Gfni", () => X86.Gfni.IsSupported, X86Sections.ProbeGfni,
            ("V256", () => X86.Gfni.V256.IsSupported),
            ("V512", () => X86.Gfni.V512.IsSupported),
            ("X64", () => X86.Gfni.X64.IsSupported)),
        new("Arm", "Sve", () => Arm.Sve.IsSupported, ArmSections.ProbeSve,
            ("Arm64", () => Arm.Sve.Arm64.IsSupported)),
        new("Arm", "Sve2", () => Arm.Sve2.IsSupported, ArmSections.ProbeSve2,
            ("Arm64", () => Arm.Sve2.Arm64.IsSupported)),
    };

    // Exercise hook, keyed by top-level row name (X86.Lzcnt). A supported family
    // with an entry runs it inside its section; the *Sections classes register.
    private static readonly Dictionary<string, Action> Exercises = new(StringComparer.Ordinal);

    private static int Main(string[] args)
    {
        // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        X86Sections.RegisterExercises(Exercises);
        ArmSections.RegisterExercises(Exercises);
        WasmSections.RegisterExercises(Exercises);

        string selection = args.Length > 0 ? args[0] : "all";
        List<Row> rows = Select(selection);
        if (rows is null)
        {
            return 2;
        }

        Console.WriteLine("families=" + selection);
        Console.WriteLine("== contract ==");
        foreach (Row row in rows)
        {
            Console.WriteLine(row.Name + "=" + (row.IsSupported() ? "True" : "False"));
        }

        foreach (Row row in rows)
        {
            Family family = row.TopLevel;
            if (family is null)
            {
                continue;
            }
            Console.WriteLine("== " + family.RowName + " ==");
            if (family.IsSupported())
            {
                if (Exercises.TryGetValue(family.RowName, out Action exercise))
                {
                    exercise();
                }
            }
            else
            {
                try
                {
                    family.Probe();
                    Console.WriteLine("probe=returned");
                }
                catch (PlatformNotSupportedException)
                {
                    Console.WriteLine("probe=PlatformNotSupportedException");
                }
            }
        }
        return 0;
    }

    private static List<Row> Select(string selection)
    {
        var rows = new List<Row>();
        foreach (string rawToken in selection.Split(','))
        {
            string token = rawToken.Trim();
            if (token.Length == 0)
            {
                continue;
            }
            if (token == "all")
            {
                foreach (Family family in Table)
                {
                    AddFamilyRows(rows, family);
                }
                continue;
            }
            if (token == "X86" || token == "Arm" || token == "Wasm")
            {
                foreach (Family family in Table)
                {
                    if (family.Arch == token)
                    {
                        AddFamilyRows(rows, family);
                    }
                }
                continue;
            }
            if (!TryAddRow(rows, token))
            {
                Console.Error.WriteLine("PlatformIsaProbe: unknown family or arch: " + token);
                return null;
            }
        }
        return rows;
    }

    private static void AddFamilyRows(List<Row> rows, Family family)
    {
        rows.Add(new Row(family.RowName, family.IsSupported, family));
        foreach ((string name, Func<bool> isSupported) in family.Nested)
        {
            rows.Add(new Row(family.RowName + "." + name, isSupported, null));
        }
    }

    private static bool TryAddRow(List<Row> rows, string name)
    {
        foreach (Family family in Table)
        {
            if (family.RowName == name)
            {
                rows.Add(new Row(name, family.IsSupported, family));
                return true;
            }
            foreach ((string nested, Func<bool> isSupported) in family.Nested)
            {
                if (family.RowName + "." + nested == name)
                {
                    rows.Add(new Row(name, isSupported, null));
                    return true;
                }
            }
        }
        return false;
    }
}
