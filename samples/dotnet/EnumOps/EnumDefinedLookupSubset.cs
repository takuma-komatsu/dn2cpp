using System;

namespace EnumDefinedLookupSubset;

internal static class Program
{
    private enum I1 : sbyte { Zero = 0, Alias = 0, 多 = 1, Max = sbyte.MaxValue }
    private enum U1 : byte { Zero = 0, Alias = 0, 多 = 1, Max = byte.MaxValue }
    private enum I2 : short { Zero = 0, Alias = 0, 多 = 1, Max = short.MaxValue }
    private enum U2 : ushort { Zero = 0, Alias = 0, 多 = 1, Max = ushort.MaxValue }
    private enum I4 : int { Zero = 0, Alias = 0, 多 = 1, Max = int.MaxValue, Neg = -1 }
    private enum U4 : uint { Zero = 0, Alias = 0, 多 = 1, Max = uint.MaxValue }
    private enum I8 : long { Zero = 0, Alias = 0, 多 = 1, Max = long.MaxValue }
    private enum U8 : ulong { Zero = 0, Alias = 0, 多 = 1, Max = ulong.MaxValue }

    internal static void Run()
    {
        Console.WriteLine("== enum defined lookup ==");
        Type[] types = { typeof(I1), typeof(U1), typeof(I2), typeof(U2), typeof(I4), typeof(U4), typeof(I8), typeof(U8) };
        object[] maxima = { sbyte.MaxValue, byte.MaxValue, short.MaxValue, ushort.MaxValue, int.MaxValue, uint.MaxValue, long.MaxValue, ulong.MaxValue };
        object[] enums = { I1.Max, U1.Max, I2.Max, U2.Max, I4.Max, U4.Max, I8.Max, U8.Max };
        object[] unknown = { (sbyte)2, (byte)2, (short)2, (ushort)2, 2, 2u, 2L, 2UL };
        for (int i = 0; i < types.Length; i++)
        {
            Show(types[i], "max", maxima[i]);
            Show(types[i], "enum", enums[i]);
            Show(types[i], "unknown", unknown[i]);
            Show(types[i], "alias", "Alias");
            Show(types[i], "unicode", "多");
            Show(types[i], "case", "alias");
            Show(types[i], "nul", "Alias\0ignored");
        }
        object[] values = { null, (sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL,
            I1.多, U1.多, I2.多, U2.多, I4.多, U4.多, I8.多, U8.多,
            "多", "Alias", "", "1", true, (char)1, (nint)1, (nuint)1, 1f, 1d, 1m, new object(), -1, ulong.MaxValue };
        for (int i = 0; i < values.Length; i++)
            Show(typeof(I4), "input-" + i, values[i]);
        Show(typeof(int), "null", null);
        Show(typeof(int), "number", 1);
        Show(typeof(int), "name", "Alias");
        Show(typeof(int), "bool", true);
        Show(null, "null", null);
        Show(null, "number", 1);
        Console.WriteLine("defined:message-enum:" + Message(typeof(I4), U1.多));
        Console.WriteLine("defined:message-underlying:" + Message(typeof(I4), (byte)1));
        Console.WriteLine("defined:message-unknown:" + Message(typeof(I4), true));
        Console.WriteLine("enum defined lookup end");
    }

    private static string Message(Type type, object value)
    {
        try { type.IsEnumDefined(value); return "no-error"; }
        catch (Exception ex) { return ex.Message; }
    }

    private static void Show(Type type, string label, object value)
    {
        Console.WriteLine("defined:" + (type == null ? "null" : type.Name) + ":" + label
            + ":" + Direct(type, value) + ":" + Static(type, value) + ":" + Bound(type, value));
    }
    private static string Error(Exception ex) => ex.GetType().Name + "/" + (ex is ArgumentException arg ? arg.ParamName : "");
    private static string Direct(Type type, object value)
    {
        try { return type.IsEnumDefined(value).ToString(); }
        catch (Exception ex) { return Error(ex); }
    }
    private static string Static(Type type, object value)
    {
        try { return Enum.IsDefined(type, value).ToString(); }
        catch (Exception ex) { return Error(ex); }
    }
    private static string Bound(Type type, object value)
    {
        try { Func<object, bool> method = type.IsEnumDefined; return method(value).ToString(); }
        catch (Exception ex) { return Error(ex); }
    }
}
