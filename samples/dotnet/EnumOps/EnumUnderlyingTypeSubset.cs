using System;
using System.Reflection;

namespace EnumUnderlyingTypeSubset;

internal static class Program
{
    private enum I1 : sbyte { Value = -1 }
    private enum U1 : byte { Value = 1 }
    private enum I2 : short { Value = -1 }
    private enum U2 : ushort { Value = 1 }
    private enum I4 : int { Value = -1 }
    private enum U4 : uint { Value = 1 }
    private enum I8 : long { Value = -1 }
    private enum U8 : ulong { Value = 1 }

    internal static void Run()
    {
        Console.WriteLine("== enum internal underlying type ==");
        MethodInfo method = typeof(Enum).GetMethod("InternalGetUnderlyingType",
            BindingFlags.Static | BindingFlags.NonPublic);
        Show(method, "i1", typeof(I1));
        Show(method, "u1", typeof(U1));
        Show(method, "i2", typeof(I2));
        Show(method, "u2", typeof(U2));
        Show(method, "i4", typeof(I4));
        Show(method, "u4", typeof(U4));
        Show(method, "i8", typeof(I8));
        Show(method, "u8", typeof(U8));
        Show(method, "bool", typeof(bool));
        Show(method, "char", typeof(char));
        Show(method, "float", typeof(float));
        Show(method, "double", typeof(double));
        Show(method, "nint", typeof(nint));
        Show(method, "nuint", typeof(nuint));
        Show(method, "void", typeof(void));
        Show(method, "decimal", typeof(decimal));
        Show(method, "object", typeof(object));
        Show(method, "string", typeof(string));
        Show(method, "vector", typeof(int[]));
        Show(method, "matrix", typeof(int[,]));
        Show(method, "typedref", typeof(TypedReference));
        Show(method, "null", null);
        Console.WriteLine("enum internal underlying type end");
    }

    private static void Show(MethodInfo method, string label, Type type)
    {
        try
        {
            Type result = (Type)method.Invoke(null, new object[] { type });
            Console.WriteLine("underlying:" + label + ":" + (result == null ? "null" : result.FullName));
        }
        catch (TargetInvocationException ex)
        {
            Console.WriteLine("underlying:" + label + ":" + ex.InnerException.GetType().Name);
        }
    }
}
