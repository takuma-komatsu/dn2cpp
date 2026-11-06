using System;

namespace EnumValuesLookupSubset;

internal static class Program
{
    private enum I1 : sbyte { Min = sbyte.MinValue, Neg = -1, Zero = 0, Alias = 0, One = 1, Max = sbyte.MaxValue }
    private enum U1 : byte { Zero = 0, Alias = 0, One = 1, Max = byte.MaxValue }
    private enum I2 : short { Min = short.MinValue, Neg = -1, Zero = 0, Alias = 0, One = 1, Max = short.MaxValue }
    private enum U2 : ushort { Zero = 0, Alias = 0, One = 1, Max = ushort.MaxValue }
    private enum I4 : int { Min = int.MinValue, Neg = -1, Zero = 0, Alias = 0, One = 1, Max = int.MaxValue }
    private enum U4 : uint { Zero = 0, Alias = 0, One = 1, Max = uint.MaxValue }
    private enum I8 : long { Min = long.MinValue, Neg = -1, Zero = 0, Alias = 0, One = 1, Max = long.MaxValue }
    private enum U8 : ulong { Zero = 0, Alias = 0, One = 1, Max = ulong.MaxValue }
    private enum Empty : byte { }
    private enum Dynamic : ushort { Max = ushort.MaxValue, Alias = ushort.MaxValue, Zero = 0 }

    private enum FieldOnly : ushort { Zero = 0, Alias = 0, Max = ushort.MaxValue }
    private sealed class Holder { public FieldOnly Value; }

    private static void ShowField(System.Reflection.FieldInfo field)
    {
        string values = "";
        foreach (object value in field.FieldType.GetEnumValues())
            values += ((Enum)value).ToString("D") + ",";
        Console.WriteLine("field-only:" + values);
    }

    internal static void Run()
    {
        Console.WriteLine("== enum values lookup ==");
        Type[] types = { typeof(I1), typeof(U1), typeof(I2), typeof(U2), typeof(I4), typeof(U4), typeof(I8), typeof(U8), typeof(Empty) };
        Type[] arrays = { typeof(I1[]), typeof(U1[]), typeof(I2[]), typeof(U2[]), typeof(I4[]), typeof(U4[]), typeof(I8[]), typeof(U8[]), typeof(Empty[]) };
        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            Array direct = type.GetEnumValues();
            Func<Array> bound = type.GetEnumValues;
            Array delegated = bound();
            Console.WriteLine("values:" + type.Name + ":" + (direct.GetType() == arrays[i]) + ":" + direct.Length + ":" + Values(direct) + ":" + Values(delegated));
            string enumerated = "";
            foreach (object value in direct)
                enumerated += ((Enum)value).ToString("D") + ",";
            Console.WriteLine("enumerated:" + type.Name + ":" + enumerated);
            if (direct.Length != 0)
            {
                direct.SetValue(direct.GetValue(direct.Length - 1), 0);
                Console.WriteLine("fresh:" + type.Name + ":" + Values(type.GetEnumValues()));
            }
        }
        // No Dynamic[] token: this exercises the allocator's runtime-only array identity.
        Array dynamicValues = typeof(Dynamic).GetEnumValues();
        Console.WriteLine("dynamic:" + dynamicValues.GetType().Name + ":" + dynamicValues.Length + ":" + Values(dynamicValues));
        string dynamicEnumerated = "";
        foreach (object value in dynamicValues)
            dynamicEnumerated += ((Enum)value).ToString("D") + ",";
        Console.WriteLine("dynamic-enumerated:" + dynamicEnumerated);
        ShowField(typeof(Holder).GetField("Value"));
        Console.WriteLine("cast:" + ((I1[])typeof(I1).GetEnumValues())[4] + ":" + ((U8[])typeof(U8).GetEnumValues())[3].ToString("D"));
        Console.WriteLine("values:error:" + Error(typeof(int)) + ":" + Error(null));
        Console.WriteLine("enum values lookup end");
    }

    private static string Values(Array array)
    {
        string result = "";
        for (int i = 0; i < array.Length; i++)
            result += ((Enum)array.GetValue(i)).ToString("D") + ",";
        return result;
    }
    private static string Error(Type type)
    {
        try { type.GetEnumValues(); return "no-error"; }
        catch (Exception ex) { return ex.GetType().Name + "/" + (ex is ArgumentException arg ? arg.ParamName : ""); }
    }
}
