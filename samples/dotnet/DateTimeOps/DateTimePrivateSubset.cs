using System;
using System.Globalization;
using System.Reflection;
using static DateTimeFormatInfoSubset.Program;

namespace DateTimePrivateSubset;

internal static class Program
{
    internal static bool Add(ref DateTime value, long ticks, out DateTime result) => throw new InvalidOperationException("IL fixture required");
    internal static bool AddAddress(ref DateTime value, long ticks, out DateTime result) => throw new InvalidOperationException("IL fixture required");
    internal static bool Create(int year, int month, int day, int hour, int minute, int second, int millisecond, out DateTime result) => throw new InvalidOperationException("IL fixture required");
    internal static DateTime Local(long ticks, DateTimeKind kind, bool ambiguous) => throw new InvalidOperationException("IL fixture required");
    internal static bool VerifyName(string name, bool throws) => throw new InvalidOperationException("IL fixture required");
    internal static bool VerifyNameAddress(string name, bool throws) => throw new InvalidOperationException("IL fixture required");
    private static string Value(DateTime value) => value.Ticks + "/" + value.Kind;

    // Native DateTime has no leap-second table; Windows CLR may accept this date.
    internal static void RunLeapSecondPolicy()
    {
        var result = new DateTime(123, DateTimeKind.Utc);
        string before = Value(result);
        bool created = Create(2016, 12, 31, 23, 59, 60, 0, out result);
        Console.WriteLine("native leap-second TryCreate=" + created + " before=" + before + " after=" + Value(result));
    }

    internal static void Run()
    {
        Console.WriteLine("== DateTime private arithmetic ==");
        MethodInfo add = typeof(DateTime).GetMethod("TryAddTicks", BindingFlags.NonPublic | BindingFlags.Instance,
            null, new[] { typeof(long), typeof(DateTime).MakeByRefType() }, null);
        foreach (DateTimeKind kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Utc, DateTimeKind.Local })
            foreach (long ticks in new[] { 0L, DateTime.MaxValue.Ticks })
                foreach (long delta in new[] { -1L, 0L, 1L, long.MinValue, long.MaxValue })
                {
                    var date = new DateTime(ticks, kind);
                    object[] arguments = { delta, new DateTime(123) };
                    bool reflected = (bool)add.Invoke(date, arguments);
                    bool direct = Add(ref date, delta, out DateTime result);
                    bool addressed = AddAddress(ref date, delta, out DateTime pointerResult);
                    bool aliased = Add(ref date, delta, out date);
                    Console.WriteLine("try-add " + ticks + "/" + kind + "/" + delta + "=" + direct + ":" + Value(result)
                        + " calli=" + addressed + ":" + Value(pointerResult) + " alias=" + aliased + ":" + Value(date)
                        + " reflect=" + reflected + ":" + Value((DateTime)arguments[1]));
                }
        MethodInfo create = typeof(DateTime).GetMethod("TryCreate", BindingFlags.NonPublic | BindingFlags.Static,
            null, new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(DateTime).MakeByRefType() }, null);
        int[][] cases = {
            new[] { 1, 1, 1, 0, 0, 0, 0 }, new[] { 9999, 12, 31, 23, 59, 59, 999 },
            new[] { 2000, 2, 29, 12, 30, 40, 123 }, new[] { 1900, 2, 29, 0, 0, 0, 0 },
            new[] { 0, 1, 1, 0, 0, 0, 0 }, new[] { 10000, 1, 1, 0, 0, 0, 0 },
            new[] { 2024, 0, 1, 0, 0, 0, 0 }, new[] { 2024, 1, 0, 0, 0, 0, 0 },
            new[] { 2024, 1, 1, 24, 0, 0, 0 }, new[] { 2024, 1, 1, 0, 60, 0, 0 },
            new[] { 2024, 1, 1, 0, 0, 60, 0 }, new[] { 2024, 1, 1, 0, 0, 0, 1000 },
            new[] { 2024, 1, 1, 0, 0, 0, -1 }, new[] { 2024, 1, 1, 0, 0, 0, int.MinValue }
        };
        foreach (int[] fields in cases)
        {
            object[] arguments = { fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6], new DateTime(123) };
            bool reflected = (bool)create.Invoke(null, arguments);
            bool direct = Create(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5], fields[6], out DateTime result);
            Console.WriteLine("try-create " + string.Join(",", fields) + "=" + direct + ":" + Value(result)
                + " reflect=" + reflected + ":" + Value((DateTime)arguments[7]));
        }
        ConstructorInfo local = typeof(DateTime).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
            null, new[] { typeof(long), typeof(DateTimeKind), typeof(bool) }, null);
        foreach (long ticks in new[] { 0L, new DateTime(2024, 11, 3, 1, 30, 0).Ticks, DateTime.MaxValue.Ticks, -1L, long.MaxValue })
            foreach (bool ambiguous in new[] { false, true })
                Console.WriteLine("local " + ticks + "/" + ambiguous
                    + " direct=" + Show(() => { var value = Local(ticks, (DateTimeKind)99, ambiguous); return Value(value) + "/" + value.ToBinary(); })
                    + " reflect=" + Show(() => { var value = (DateTime)local.Invoke(new object[] { ticks, DateTimeKind.Utc, ambiguous }); return Value(value) + "/" + value.ToBinary(); }));
        var overlap = Local(new DateTime(2024, 11, 3, 1, 30, 0).Ticks, DateTimeKind.Local, true);
        bool added = Add(ref overlap, 1, out overlap);
        Console.WriteLine("try-add ambiguous alias=" + added + ":" + Value(overlap) + "/" + overlap.ToBinary());
        Console.WriteLine("DateTime private arithmetic end");
    }

    internal static void RunCultureNames()
    {
        Console.WriteLine("== CultureInfo string validator ==");
        MethodInfo validator = typeof(CultureInfo).GetMethod("VerifyCultureName", BindingFlags.NonPublic | BindingFlags.Static,
            null, new[] { typeof(string), typeof(bool) }, null);
        Console.WriteLine("validator signature=" + validator.ReturnType.Name + "/" + validator.GetParameters()[0].ParameterType.Name);
        foreach (string name in new[] { "", "en-US", "fr_FR", "123", "\u00e9", "de!DE", "en US", null })
            foreach (bool throws in new[] { false, true })
                Console.WriteLine("verify " + (name ?? "<null>") + "/" + throws + "="
                    + Show(() => validator.Invoke(null, new object[] { name, throws }).ToString()));
        Console.WriteLine("verify direct=" + VerifyName("en-US", false) + "/" + VerifyName("en US", false)
            + "/" + Show(() => VerifyName("en US", true).ToString()));
        Console.WriteLine("verify calli=" + VerifyNameAddress("en-US", false) + "/" + VerifyNameAddress("en US", false)
            + "/" + Show(() => VerifyNameAddress("en US", true).ToString()));
        Console.WriteLine("CultureInfo string validator end");
    }
}
