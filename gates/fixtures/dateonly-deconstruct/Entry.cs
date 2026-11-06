using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DateOnlyDeconstructSubset;

internal static class Program
{
    private delegate void Query(ref DateOnly receiver, out int year, out int month, out int day);
    private delegate void Closed(out int year, out int month, out int day);
    private static void Direct(ref DateOnly receiver, out int year, out int month, out int day) => receiver.Deconstruct(out year, out month, out day);
    private static void Pointer(ref DateOnly receiver, out int year, out int month, out int day) => receiver.Deconstruct(out year, out month, out day);
    private static Closed Address(DateOnly receiver) => receiver.Deconstruct;
    private static void DelegateRoute(ref DateOnly receiver, out int year, out int month, out int day) => Address(receiver)(out year, out month, out day);
    private static MethodInfo Metadata() => typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct))!;
    private static readonly int[] Dates = { 1,1,1, 2024,2,29, 2001,12,31, 1,2,28, 1,3,1, 1900,2,28, 2000,2,29, 2000,3,1, 9999,12,31 };
    // Literal results from the installed public CLR oracle, not an implementation model.
    private static readonly int[] Aliases =
    {
        -10,2,1,2,1,1,-11,
        18,3,29,30,2,2024,17,
        31,12,31,31,12,2001,31,
        17,3,28,29,2,1,16,
        1,3,1,1,3,1,1,
        17,3,28,29,2,1900,16,
        18,3,29,30,2,2000,17,
        1,3,1,1,3,2000,1,
        31,12,31,31,12,9999,31
    };

    private static void Check(string label, bool condition)
    {
        Console.WriteLine("dateonly-deconstruct-" + label + ":" + condition);
        if (!condition) throw new InvalidOperationException(label);
    }

    private static void Aliasing(Query query, string route)
    {
        for (int index = 0; index < 9; index++)
        {
            int k = index * 3, expected = index * 7;
            var date = new DateOnly(Dates[k], Dates[k + 1], Dates[k + 2]);
            query(ref date, out int y, out int m, out int d);
            Check(route + "-separate-" + index, y == Dates[k] && m == Dates[k + 1] && d == Dates[k + 2]);
            int a = -1, b = -2;
            query(ref date, out a, out a, out a);
            Check(route + "-all-" + index, a == Aliases[expected]);
            query(ref date, out a, out a, out b);
            Check(route + "-ym-" + index, a == Aliases[expected + 1] && b == Aliases[expected + 2]);
            query(ref date, out a, out b, out a);
            Check(route + "-yd-" + index, a == Aliases[expected + 3] && b == Aliases[expected + 4]);
            query(ref date, out b, out a, out a);
            Check(route + "-md-" + index, b == Aliases[expected + 5] && a == Aliases[expected + 6]);
        }
    }

    private static void ReceiverStorage(Query query, string route)
    {
        for (int index = 0; index < 9; index++)
        for (int slot = 0; slot < 3; slot++)
        {
            int k = index * 3;
            var date = new DateOnly(Dates[k], Dates[k + 1], Dates[k + 2]);
            int y = -1, m = -2, d = -3;
            ref int storage = ref Unsafe.As<DateOnly, int>(ref date);
            if (slot == 0) query(ref date, out storage, out m, out d);
            if (slot == 1) query(ref date, out y, out storage, out d);
            if (slot == 2) query(ref date, out y, out m, out storage);
            Check(route + "-receiver-storage-" + index + "-" + slot,
                storage == Dates[k + slot]
                && y == (slot == 0 ? -1 : Dates[k])
                && m == (slot == 1 ? -2 : Dates[k + 1])
                && d == (slot == 2 ? -3 : Dates[k + 2]));
        }
    }

    private static void NullOutputs(Query query, string route)
    {
        for (int index = 0; index < 9; index++)
        for (int slot = 0; slot < 3; slot++)
        {
            int k = index * 3;
            var date = new DateOnly(Dates[k], Dates[k + 1], Dates[k + 2]);
            int y = -1, m = -2, d = -3;
            bool observed = false;
            try
            {
                if (slot == 0) query(ref date, out Unsafe.NullRef<int>(), out m, out d);
                if (slot == 1) query(ref date, out y, out Unsafe.NullRef<int>(), out d);
                if (slot == 2) query(ref date, out y, out m, out Unsafe.NullRef<int>());
            }
            catch (NullReferenceException) { observed = true; }
            int provisionalYear = Dates[k + 1] <= 2 ? Dates[k] - 1 : Dates[k];
            int provisionalMonth = Dates[k + 1] <= 2 ? Dates[k + 1] + 12 : Dates[k + 1];
            Check(route + "-null-output-" + index + "-" + slot, observed
                && y == (slot == 0 ? -1 : provisionalYear)
                && m == (slot < 2 ? -2 : provisionalMonth) && d == -3);
        }
        int year = -1, month = -2, day = -3;
        bool receiverError = false;
        try { query(ref Unsafe.NullRef<DateOnly>(), out year, out month, out day); }
        catch (NullReferenceException) { receiverError = true; }
        Check(route + "-null-receiver", receiverError && year == -1 && month == -2 && day == -3);
    }

    private static DateOnly producerDate;
    private static int producerYear, producerMonth, producerDay, trace;
    private static ref DateOnly ReceiverProducer(bool nullReceiver)
    {
        trace = trace * 10 + 1;
        if (nullReceiver) return ref Unsafe.NullRef<DateOnly>();
        return ref producerDate;
    }
    private static ref int YearProducer() { trace = trace * 10 + 2; return ref producerYear; }
    private static ref int MonthProducer() { trace = trace * 10 + 3; return ref producerMonth; }
    private static ref int DayProducer(bool throws)
    {
        trace = trace * 10 + 4;
        if (throws) throw new FormatException("day output producer");
        return ref producerDay;
    }
    private static void ProducerRoute(bool nullReceiver, bool throws) =>
        ReceiverProducer(nullReceiver).Deconstruct(out YearProducer(), out MonthProducer(), out DayProducer(throws));

    private static void Producers()
    {
        for (int mode = 0; mode < 3; mode++)
        {
            producerDate = new DateOnly(2024, 2, 29);
            producerYear = -1; producerMonth = -2; producerDay = -3; trace = 0;
            Exception observed = null!;
            try { ProducerRoute(mode != 0, mode == 2); }
            catch (Exception error) { observed = error; }
            Check("producer-" + mode, trace == 1234 && (mode == 0
                ? observed is null && producerYear == 2024 && producerMonth == 2 && producerDay == 29
                : producerYear == -1 && producerMonth == -2 && producerDay == -3
                    && (mode == 1 ? observed is NullReferenceException
                        : observed is FormatException && observed.Message == "day output producer")));
        }
    }

    public static void RunReflectionChecks()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        for (int index = 0; index < 9; index++)
        {
            int k = index * 3;
            var date = new DateOnly(Dates[k], Dates[k + 1], Dates[k + 2]);
            object[] values = { -1, -2, -3 };
            Check("reflection-void-" + index, Metadata().Invoke(date, values) is null);
            Check("reflection-values-" + index, (int)values[0] == Dates[k]
                && (int)values[1] == Dates[k + 1] && (int)values[2] == Dates[k + 2]);
        }
        Check("reflection-flags-static", typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct), BindingFlags.Public | BindingFlags.Static) is null);
        Check("reflection-flags-nonpublic", typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct), BindingFlags.NonPublic | BindingFlags.Instance) is null);
        Check("reflection-wrong-arity", typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct), Type.EmptyTypes) is null);
        Check("reflection-value-parameters", typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct), new[] { typeof(int), typeof(int), typeof(int) }) is null);
        Check("reflection-wrong-element", typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct), new[] { typeof(long).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType() }) is null);
        Check("reflection-exact-parameters", typeof(DateOnly).GetMethod(nameof(DateOnly.Deconstruct), new[] { typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType() }) == Metadata());
        var parameters = Metadata().GetParameters();
        Check("reflection-abi", Metadata().ReturnType == typeof(void) && parameters.Length == 3
            && parameters[0].ParameterType.IsByRef && parameters[0].ParameterType.GetElementType() == typeof(int)
            && parameters[1].ParameterType.IsByRef && parameters[1].ParameterType.GetElementType() == typeof(int)
            && parameters[2].ParameterType.IsByRef && parameters[2].ParameterType.GetElementType() == typeof(int));
        bool observed = false;
        try { Metadata().Invoke(null, new object[] { -1, -2, -3 }); }
        catch (TargetException) { observed = true; }
        Check("reflection-null-target", observed);
        Console.WriteLine("DateOnly Deconstruct reflection end");
    }

    public static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Aliasing(Direct, "direct");
        ReceiverStorage(Direct, "direct");
        NullOutputs(Direct, "direct");
        Aliasing(Pointer, "pointer");
        ReceiverStorage(Pointer, "pointer");
        NullOutputs(Pointer, "pointer");
        Aliasing(DelegateRoute, "delegate");
        Check("delegate-abi", Address(new DateOnly(2024, 2, 29)).Method.ReturnType == typeof(void));
        Check("delegate-named-identity", Metadata() == Address(new DateOnly(2024, 2, 29)).Method);
        Producers();
        RunReflectionChecks();
        Console.WriteLine("DateOnly Deconstruct alias end");
    }

    private static void Main() => Run();
}
