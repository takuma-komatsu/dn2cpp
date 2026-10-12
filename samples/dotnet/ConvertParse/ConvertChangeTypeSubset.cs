#nullable enable
// (practical reflection): Convert.ChangeType(object, Type) — the runtime-Type-driven
// conversion a deserializer / data-binding layer leans on, where the target type is only
// known as a System.Type. Covers string<->primitive, numeric<->numeric (banker's
// rounding), x->string, same-type passthrough, and a deserialize-style loop that converts
// each cell to its column's target type. Diffed exact vs real.NET.
using System;
using System.Globalization;
using SampleSupport;

namespace ConvertChangeTypeSubset;

public static class Program
{
    static void Show(object v) => Console.WriteLine($"{v} :: {v.GetType().Name}");

    internal static void __GateEntry()
    {
        // string -> primitives
        Show(Convert.ChangeType("42", typeof(int)));
        Show(Convert.ChangeType("3.5", typeof(double)));
        Show(Convert.ChangeType("true", typeof(bool)));
        Show(Convert.ChangeType("100", typeof(long)));
        Show(Convert.ChangeType("7", typeof(byte)));

        // numeric -> numeric (banker's rounding on narrowing)
        Show(Convert.ChangeType(3.9, typeof(int)));
        Show(Convert.ChangeType(2.5, typeof(int)));
        Show(Convert.ChangeType(65, typeof(double)));
        Show(Convert.ChangeType(1, typeof(bool)));
        Show(Convert.ChangeType(0, typeof(bool)));

        // anything -> string
        Show(Convert.ChangeType(123, typeof(string)));
        Show(Convert.ChangeType(2.5, typeof(string)));
        Show(Convert.ChangeType(true, typeof(string)));

        // same-type passthrough
        Show(Convert.ChangeType(99, typeof(int)));

        // null passthrough
        object? n = Convert.ChangeType(null!, typeof(string));
        Console.WriteLine("null -> " + (n is null ? "null" : "?"));

        // deserialize-style: convert each cell into its column's target type
        object[] cells = { "10", "2.0", "false", "255" };
        Type[] types = { typeof(int), typeof(double), typeof(bool), typeof(byte) };
        for (int i = 0; i < cells.Length; i++)
            Show(Convert.ChangeType(cells[i], types[i]));
    }

    private sealed class CountingProvider : IFormatProvider
    {
        internal int Calls;
        internal object? Format = new NumberFormatInfo
        {
            NegativeSign = "minus", NumberDecimalSeparator = ","
        };

        public object? GetFormat(Type? type)
        {
            Calls++;
            if (type != typeof(NumberFormatInfo))
                throw new InvalidOperationException("unexpected format request");
            return Format;
        }
    }

    private sealed class UnusedProvider : IFormatProvider
    {
        public object? GetFormat(Type? type) => throw new InvalidOperationException("unused provider body");
    }

    private static void ProviderCase(string label, object value, Type target, TypeCode code, CountingProvider provider)
    {
        provider.Calls = 0;
        try
        {
            object result = Convert.ChangeType(value, target, provider);
            Console.WriteLine("provider Type:" + label + "=" + Convert.ToString(result, CultureInfo.InvariantCulture) + "/" + provider.Calls);
        }
        catch (Exception ex)
        {
            Console.WriteLine("provider Type:" + label + "=" + ex.GetType().Name + "/" + provider.Calls);
        }
        provider.Calls = 0;
        try
        {
            object result = Convert.ChangeType(value, code, provider)!;
            Console.WriteLine("provider TypeCode:" + label + "=" + Convert.ToString(result, CultureInfo.InvariantCulture) + "/" + provider.Calls);
        }
        catch (Exception ex)
        {
            Console.WriteLine("provider TypeCode:" + label + "=" + ex.GetType().Name + "/" + provider.Calls);
        }
    }

    internal static void RunProviders()
    {
        Console.WriteLine("== ChangeType format providers ==");
        var provider = new CountingProvider();
        object[] numbers = { (sbyte)-12, (short)-12, -12, -12L, 12, 12U, 12UL, -12.5f, -12.5, -12.5m };
        foreach (object number in numbers)
            ProviderCase("format " + number.GetType().Name, number, typeof(string), TypeCode.String, provider);
        Type[] types = { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) };
        TypeCode[] codes = { TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal };
        for (int i = 0; i < types.Length; i++)
            ProviderCase("parse " + types[i].Name, "12", types[i], codes[i], provider);
        ProviderCase("parse sign", "minus12", typeof(int), TypeCode.Int32, provider);
        ProviderCase("parse fraction", "minus12,5", typeof(double), TypeCode.Double, provider);
        ProviderCase("parse decimal", "minus12,5", typeof(decimal), TypeCode.Decimal, provider);
        ProviderCase("parse invalid", "bad", typeof(int), TypeCode.Int32, provider);
        ProviderCase("parse overflow", "999", typeof(byte), TypeCode.Byte, provider);
        ProviderCase("numeric", -12, typeof(double), TypeCode.Double, provider);
        ProviderCase("identity", -12, typeof(int), TypeCode.Int32, provider);
        ProviderCase("string identity", "minus12", typeof(string), TypeCode.String, provider);
        ProviderCase("Boolean", true, typeof(string), TypeCode.String, provider);
        ProviderCase("Char", 'x', typeof(string), TypeCode.String, provider);
        ProviderCase("parse Boolean", "true", typeof(bool), TypeCode.Boolean, provider);
        ProviderCase("parse Char", "x", typeof(char), TypeCode.Char, provider);
        ProviderCase("null", null!, typeof(string), TypeCode.String, provider);
        provider.Format = null;
        ProviderCase("null format", "-12.5", typeof(double), TypeCode.Double, provider);
        provider.Format = new object();
        ProviderCase("wrong format", -12.5, typeof(string), TypeCode.String, provider);
        RunKnownProviders();
        Console.WriteLine("ChangeType format providers end");
    }

    internal static void RunKnownProviders()
    {
        var unused = new UnusedProvider();
        Console.WriteLine("ChangeType unused provider=" + (unused is IFormatProvider));
        Console.WriteLine("ChangeType unused identity=" + Convert.ChangeType(-12, typeof(int), unused) + "/" + Convert.ChangeType(-12, TypeCode.Int32, unused));
        Console.WriteLine("ChangeType unused numeric=" + Convert.ChangeType(-12, typeof(double), unused) + "/" + Convert.ChangeType(-12, TypeCode.Double, unused));
        Console.WriteLine("ChangeType unused Boolean=" + Convert.ChangeType(true, typeof(string), unused) + "/" + Convert.ChangeType(true, TypeCode.String, unused));
        Console.WriteLine("ChangeType unused Char=" + Convert.ChangeType('x', typeof(string), unused) + "/" + Convert.ChangeType('x', TypeCode.String, unused));
        Console.WriteLine("ChangeType unused parse Boolean=" + Convert.ChangeType("true", typeof(bool), unused) + "/" + Convert.ChangeType("true", TypeCode.Boolean, unused));
        Console.WriteLine("ChangeType unused parse Char=" + Convert.ChangeType("x", typeof(char), unused) + "/" + Convert.ChangeType("x", TypeCode.Char, unused));
        NumberFormatInfo format = new NumberFormatInfo { NegativeSign = "minus", NumberDecimalSeparator = "," };
        Console.WriteLine("ChangeType NumberFormatInfo=" + Convert.ChangeType(-12, typeof(string), format) + "/" + Convert.ChangeType(-12, TypeCode.String, format));
        using (CultureScope.Use(new CultureInfo("de-DE")))
        {
            Console.WriteLine("ChangeType null provider=" + Convert.ChangeType(-12.5, typeof(string), null) + "/" + Convert.ChangeType(-12.5, TypeCode.String, null));
            Console.WriteLine("ChangeType omitted provider=" + Convert.ChangeType(-12.5, typeof(string)) + "/" + Convert.ChangeType(-12.5, TypeCode.String));
            Console.WriteLine("ChangeType culture=" + Convert.ChangeType("-12,5", typeof(double), CultureInfo.CurrentCulture) + "/" + Convert.ChangeType("-12,5", TypeCode.Double, CultureInfo.CurrentCulture));
        }
#if CHANGE_TYPE_KNOWN_PROVIDERS_ONLY
        Console.WriteLine("ChangeType unused Byte=" + Convert.ChangeType((byte)12, typeof(string), unused) + "/" + Convert.ChangeType((byte)12, TypeCode.String, unused));
        Console.WriteLine("ChangeType unused UInt16=" + Convert.ChangeType((ushort)12, typeof(string), unused) + "/" + Convert.ChangeType((ushort)12, TypeCode.String, unused));
        Console.WriteLine("ChangeType unused UInt32=" + Convert.ChangeType(uint.MaxValue, typeof(string), unused) + "/" + Convert.ChangeType(uint.MaxValue, TypeCode.String, unused));
        Console.WriteLine("ChangeType unused UInt64=" + Convert.ChangeType(ulong.MaxValue, typeof(string), unused) + "/" + Convert.ChangeType(ulong.MaxValue, TypeCode.String, unused));
#endif
    }

    private static void FaultCase(string label, object value, Type target, TypeCode code)
    {
        try
        {
            Console.WriteLine("ChangeType fault Type:" + label + "=" + Convert.ChangeType(value, target, CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            Console.WriteLine("ChangeType fault Type:" + label + "=" + ex.GetType().Name);
        }
        try
        {
            Console.WriteLine("ChangeType fault TypeCode:" + label + "=" + Convert.ChangeType(value, code, CultureInfo.InvariantCulture));
        }
        catch (Exception ex)
        {
            Console.WriteLine("ChangeType fault TypeCode:" + label + "=" + ex.GetType().Name);
        }
    }

    internal static void RunFaults()
    {
        Console.WriteLine("== ChangeType unsupported conversions ==");
        object[] values = { true, 'x', (sbyte)-12, (byte)12, (short)-12, (ushort)12, -12, 12U, -12L, 12UL, -12.5f, -12.5, -12.5m };
        foreach (object value in values)
            FaultCase(value.GetType().Name + " to DateTime", value, typeof(DateTime), TypeCode.DateTime);
        var date = new DateTime(2024, 1, 2, 3, 4, 5);
        Type[] types = { typeof(bool), typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) };
        TypeCode[] codes = { TypeCode.Boolean, TypeCode.Char, TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32, TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal };
        for (int i = 0; i < types.Length; i++)
            FaultCase("DateTime to " + types[i].Name, date, types[i], codes[i]);
        FaultCase("DateTime identity", date, typeof(DateTime), TypeCode.DateTime);
        FaultCase("DateTime string", date, typeof(string), TypeCode.String);
        FaultCase("DateTime parse", "01/02/2024 03:04:05", typeof(DateTime), TypeCode.DateTime);
        FaultCase("DateTime bad parse", "bad", typeof(DateTime), TypeCode.DateTime);
        FaultCase("Boolean to Char", true, typeof(char), TypeCode.Char);
        FaultCase("Char to Boolean", 'x', typeof(bool), TypeCode.Boolean);
        FaultCase("Char to Single", 'x', typeof(float), TypeCode.Single);
        FaultCase("Char to Double", 'x', typeof(double), TypeCode.Double);
        FaultCase("Char to Decimal", 'x', typeof(decimal), TypeCode.Decimal);
        FaultCase("Single to Char", 12.5f, typeof(char), TypeCode.Char);
        FaultCase("Double to Char", 12.5, typeof(char), TypeCode.Char);
        FaultCase("Decimal to Char", 12.5m, typeof(char), TypeCode.Char);
        FaultCase("Object to Int32", new object(), typeof(int), TypeCode.Int32);
        FaultCase("Object to String", new object(), typeof(string), TypeCode.String);
        Console.WriteLine("ChangeType unsupported conversions end");
    }
}

#if CHANGE_TYPE_KNOWN_PROVIDERS_ONLY
internal static class KnownProviderProgram
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Program.RunKnownProviders();
    }
}
#endif
