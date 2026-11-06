using System;
using System.Reflection;

namespace EnumElementTypeSubset;

internal static class Program
{
    private enum I1 : sbyte { Min = sbyte.MinValue, Max = sbyte.MaxValue }
    private enum U1 : byte { Min = 0, Max = byte.MaxValue }
    private enum I2 : short { Min = short.MinValue, Max = short.MaxValue }
    private enum U2 : ushort { Min = 0, Max = ushort.MaxValue }
    private enum I4 : int { Min = int.MinValue, Max = int.MaxValue }
    private enum U4 : uint { Min = 0, Max = uint.MaxValue }
    private enum I8 : long { Min = long.MinValue, Max = long.MaxValue }
    private enum U8 : ulong { Min = 0, Max = ulong.MaxValue }

    internal static void Run()
    {
        Console.WriteLine("== enum primitive element type ==");
        MethodInfo method = typeof(Enum).GetMethod("InternalGetCorElementType",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Show(method, "i1", I1.Min, I1.Max);
        Show(method, "u1", U1.Min, U1.Max);
        Show(method, "i2", I2.Min, I2.Max);
        Show(method, "u2", U2.Min, U2.Max);
        Show(method, "i4", I4.Min, I4.Max);
        Show(method, "u4", U4.Min, U4.Max);
        Show(method, "i8", I8.Min, I8.Max);
        Show(method, "u8", U8.Min, U8.Max);
        MethodInfo typeMethod = typeof(Enum).GetMethod("InternalGetCorElementType",
            BindingFlags.Static | BindingFlags.NonPublic);
        ShowType(typeMethod, "i1", typeof(I1));
        ShowType(typeMethod, "u1", typeof(U1));
        ShowType(typeMethod, "i2", typeof(I2));
        ShowType(typeMethod, "u2", typeof(U2));
        ShowType(typeMethod, "i4", typeof(I4));
        ShowType(typeMethod, "u4", typeof(U4));
        ShowType(typeMethod, "i8", typeof(I8));
        ShowType(typeMethod, "u8", typeof(U8));
        ShowType(typeMethod, "bool", typeof(bool));
        ShowType(typeMethod, "char", typeof(char));
        ShowType(typeMethod, "float", typeof(float));
        ShowType(typeMethod, "double", typeof(double));
        ShowType(typeMethod, "nint", typeof(nint));
        ShowType(typeMethod, "nuint", typeof(nuint));
        ShowType(typeMethod, "void", typeof(void));
        ShowType(typeMethod, "decimal", typeof(decimal));
        ShowType(typeMethod, "object", typeof(object));
        ShowType(typeMethod, "string", typeof(string));
        ShowType(typeMethod, "vector", typeof(int[]));
        ShowType(typeMethod, "matrix", typeof(int[,]));
        ShowType(typeMethod, "typedref", typeof(TypedReference));
        Console.WriteLine("enum primitive element type end");
    }

    private static void ShowType(MethodInfo method, string label, Type type)
    {
        Console.WriteLine("type:" + label + ":" + Convert.ToInt32(method.Invoke(null, new object[] { type })));
    }

    private static void Show(MethodInfo method, string label, Enum minimum, Enum maximum)
    {
        Console.WriteLine(label + ":" + Convert.ToInt32(method.Invoke(minimum, null))
            + ":" + Convert.ToInt32(method.Invoke(maximum, null)));
    }
}
