using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;
using Mono.Cecil.Cil;

internal static class DateTimeFamilyAggregateFixture
{
    public static bool Run(string[] args)
    {
        if (args.Length != 3 || args[0] != "--datetime-family-aggregate") return false;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string path = Path.GetFullPath(args[1]);
        string directory = Path.GetDirectoryName(path)!;
        if (Path.GetFileName(path) != "DateTimeFamily.dll") throw new InvalidOperationException("wrong aggregate input");
        var families = new (string Assembly, string Namespace, string Flag)[]
        {
            ("AbbreviatedDay", "AbbreviatedDaySubset", "--abbreviated-day"),
            ("FullDay", "FullDaySubset", "--full-day"),
            ("AbbreviatedMonth", "AbbreviatedMonthSubset", "--abbreviated-month"),
            ("FullMonth", "FullMonthSubset", "--full-month"),
            ("DtfICalendar", "DtfICalendarSubset", "--dtfi-calendar"),
            ("DtfICalendarSetter", "DtfICalendarSetterSubset", "--dtfi-calendar-setter"),
            ("InternalMonth", "InternalMonthSubset", "--internal-month"),
            ("EraName", "EraNameSubset", "--era-name"),
            ("FormatFlags", "FormatFlagsSubset", "--format-flags"),
            ("ForceTwoDigitYears", "ForceTwoDigitYearsSubset", "--force-two-digit-years"),
            ("GeneralShortTimePattern", "GeneralShortTimePatternSubset", "--general-short-time-pattern"),
            ("GeneralLongTimePattern", "GeneralLongTimePatternSubset", "--general-long-time-pattern"),
            ("DateTimeOffsetPattern", "DateTimeOffsetPatternSubset", "--datetime-offset-pattern"),
            ("AllDateTimePatterns", "AllDateTimePatternsSubset", "--all-datetime-patterns"),
            ("DateTimeTChar", "DateTimeTChar", "--datetime-tchar"),
            ("DateOnlyDeconstruct", "DateOnlyDeconstructSubset", "--dateonly-deconstruct"),
            ("YearMonthAdjustment", "YearMonthAdjustmentSubset", "--year-month-adjustment"),
            ("DateTimeTrio", "DateTimeTrioSubset", "--datetime-trio"),
            ("DtfIParserFlags", "DtfIParserFlagsSubset", "--dtfi-parser-flags"),
            ("DtfINameArrays", "DtfINameArraysSubset", "--dtfi-name-arrays"),
        };
        var receipts = new List<object>();
        foreach (var family in families)
        {
            string dependency = Path.Combine(directory, family.Assembly + ".dll");
            if (!File.Exists(dependency)) throw new InvalidOperationException("missing copied aggregate dependency: " + dependency);
            string before = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dependency)));
            string[] invocation = { family.Flag, dependency, args[2] };
            bool handled = family.Flag switch
            {
                "--abbreviated-day" or "--full-day" => AbbreviatedDayFixture.Run(invocation),
                "--abbreviated-month" or "--full-month" => AbbreviatedMonthFixture.Run(invocation),
                "--dtfi-calendar" => DtfICalendarFixture.Run(invocation),
                "--dtfi-calendar-setter" => DtfICalendarSetterFixture.Run(invocation),
                "--internal-month" => InternalMonthFixture.Run(invocation),
                "--era-name" => EraNameFixture.Run(invocation),
                "--format-flags" => FormatFlagsFixture.Run(invocation),
                "--force-two-digit-years" => ForceTwoDigitYearsFixture.Run(invocation),
                "--general-short-time-pattern" or "--general-long-time-pattern" or "--datetime-offset-pattern" => GeneralShortTimePatternFixture.Run(invocation),
                "--all-datetime-patterns" => AllDateTimePatternsFixture.Run(invocation),
                "--datetime-tchar" => DateTimeTCharFixture.Run(invocation),
                "--dateonly-deconstruct" => DateOnlyDeconstructFixture.Run(invocation),
                "--year-month-adjustment" => YearMonthAdjustmentFixture.Run(invocation),
                "--datetime-trio" => DateTimeTrioFixture.Run(invocation),
                "--dtfi-parser-flags" => DtfIParserFlagsFixture.Run(invocation),
                "--dtfi-name-arrays" => DtfINameArraysFixture.Run(invocation),
                _ => false
            };
            if (!handled) throw new InvalidOperationException("unhandled aggregate family " + family.Flag);
            receipts.Add(new { family.Assembly, family.Flag, before, after = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dependency))) });
        }
        using var app = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { InMemory = true });
        var entry = app.MainModule.GetType("DateTimeFamily.Entry").Methods.Single(method => method.Name == "RunAll");
        entry.Body = new MethodBody(entry);
        var il = entry.Body.GetILProcessor();
        var writeLine = app.MainModule.ImportReference(typeof(Console).GetMethod(nameof(Console.WriteLine), new[] { typeof(string) })!);
        foreach (var family in families)
        {
            using var dependency = AssemblyDefinition.ReadAssembly(Path.Combine(directory, family.Assembly + ".dll"));
            if (dependency.Name.Name != family.Assembly) throw new InvalidOperationException("wrong aggregate dependency identity");
            var run = dependency.MainModule.GetType(family.Namespace + ".Program").Methods.Single(method => method.Name == "Run" && method.Parameters.Count == 0);
            if (!run.IsStatic || run.ReturnType.MetadataType != MetadataType.Void) throw new InvalidOperationException("unexpected family entry signature");
            il.Emit(OpCodes.Ldstr, "== family begin " + family.Assembly + " ==");
            il.Emit(OpCodes.Call, writeLine);
            il.Emit(OpCodes.Call, app.MainModule.ImportReference(run));
            il.Emit(OpCodes.Ldstr, "== family end " + family.Assembly + " ==");
            il.Emit(OpCodes.Call, writeLine);
        }
        il.Emit(OpCodes.Ret);
        app.Write(path, new WriterParameters { Timestamp = 0, DeterministicMvid = true });
        File.WriteAllText(Path.Combine(directory, "aggregate-rewrite.json"), JsonSerializer.Serialize(receipts, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllLines(Path.Combine(directory, "family-refs.txt"), families.Select(family => Path.Combine(directory, family.Assembly + ".dll")));
        Console.WriteLine("datetime-family-aggregate:rewritten-copied-dependencies=" + families.Length);
        return true;
    }
}
