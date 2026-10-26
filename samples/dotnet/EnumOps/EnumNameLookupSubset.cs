using System;
using System.Reflection;

namespace EnumNameLookupSubset;

internal static class Program
{
    private enum I1 : sbyte { Min = sbyte.MinValue, Neg = -1, Zero = 0, One = 1, Max = sbyte.MaxValue }
    private enum U1 : byte { Zero = 0, One = 1, Max = byte.MaxValue }
    private enum I2 : short { Min = short.MinValue, Neg = -1, Zero = 0, One = 1, Max = short.MaxValue }
    private enum U2 : ushort { Zero = 0, One = 1, Max = ushort.MaxValue }
    private enum I4 : int { Min = int.MinValue, Neg = -1, Zero = 0, One = 1, Max = int.MaxValue }
    private enum U4 : uint { Zero = 0, One = 1, Max = uint.MaxValue }
    private enum I8 : long { Min = long.MinValue, Neg = -1, Zero = 0, One = 1, Max = long.MaxValue }
    private enum U8 : ulong { Zero = 0, One = 1, Max = ulong.MaxValue }

    internal static void Run()
    {
        Console.WriteLine("== enum name lookup ==");
        Type[] types = { typeof(I1), typeof(U1), typeof(I2), typeof(U2), typeof(I4), typeof(U4), typeof(I8), typeof(U8), typeof(int) };
        object[] values = { (sbyte)-1, byte.MaxValue, (short)-1, ushort.MaxValue, -1, uint.MaxValue,
            -1L, ulong.MaxValue, 0, 1, 256, 257, long.MinValue, long.MaxValue,
            (char)1, I1.Min, U1.Max, I2.Min, U2.Max, I4.Min, U4.Max, I8.Min, U8.Max,
            null, true, (nint)1, (nuint)1, 1f, 1d, 1m, "One", new object() };
        foreach (Type type in types)
        {
            Func<object, string> bound = type.GetEnumName;
            for (int i = 0; i < values.Length; i++)
            {
                object value = values[i];
                Console.WriteLine("name:" + type.Name + ":" + i + ":" + Direct(type, value)
                    + ":" + Static(type, value) + ":" + Bound(bound, value));
            }
        }
        Console.WriteLine("name:null:" + Direct(null, null) + ":" + Static(null, null));
        MethodInfo scalar = typeof(Enum).GetMethod("GetName", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { Type.GetType("System.RuntimeType"), typeof(ulong) }, null);
        ulong[] bits = { 0, 1, 127, 128, 255, 256, 32767, 32768, 65535, 65536, int.MaxValue, 2147483648, uint.MaxValue, 4294967296, long.MaxValue, 9223372036854775808, ulong.MaxValue };
        foreach (Type type in types)
            for (int i = 0; i < bits.Length; i++)
                Console.WriteLine("scalar:" + type.Name + ":" + i + ":" + Scalar(scalar, type, bits[i]));
        Console.WriteLine("scalar:null:" + Scalar(scalar, null, 0));
        Console.WriteLine("enum name lookup end");
    }

    private static string Error(Exception ex) => ex.GetType().Name + "/" + (ex is ArgumentException arg ? arg.ParamName : "");
    private static string Direct(Type type, object value)
    {
        try { return type.GetEnumName(value) ?? "null"; }
        catch (Exception ex) { return Error(ex); }
    }
    private static string Static(Type type, object value)
    {
        try { return Enum.GetName(type, value) ?? "null"; }
        catch (Exception ex) { return Error(ex); }
    }
    private static string Bound(Func<object, string> bound, object value)
    {
        try { return bound(value) ?? "null"; }
        catch (Exception ex) { return Error(ex); }
    }
    private static string Scalar(MethodInfo method, Type type, ulong value)
    {
        try { return (string)method.Invoke(null, new object[] { type, value }) ?? "null"; }
        catch (TargetInvocationException ex) { return Error(ex.InnerException); }
    }
}
