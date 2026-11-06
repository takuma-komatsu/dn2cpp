using System;
using System.Runtime.CompilerServices;

namespace EnumOps;

internal static class EnumAliasSubset
{
    private enum Narrow : short { Negative = -1, One = 1, Alias = 1, Zero = 0 }
    private enum Wide : ulong { Top = 1UL << 63, TopAlias = 1UL << 63, One = 1, Alias = 1 }
    private enum Unsigned : uint { High = 0x80000000, Last = uint.MaxValue }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type RuntimeType(bool wide) => wide ? typeof(Wide) : typeof(Narrow);

    internal static void Run()
    {
        Console.WriteLine("== enum alias metadata ==");
        Console.WriteLine("narrow names generic=" + string.Join(",", Enum.GetNames<Narrow>()));
        Console.WriteLine("narrow names runtime=" + string.Join(",", Enum.GetNames(RuntimeType(false))));
        var narrow = Enum.GetValues<Narrow>();
        Console.WriteLine("narrow values generic=" + narrow.Length + ":" + (short)narrow[0]
            + ":" + (short)narrow[1] + ":" + (short)narrow[2] + ":" + (short)narrow[3]);
        var narrowRuntime = Enum.GetValues(RuntimeType(false));
        Console.WriteLine("narrow values runtime=" + narrowRuntime.Length + ":" + (short)(Narrow)narrowRuntime.GetValue(0)!
            + ":" + (short)(Narrow)narrowRuntime.GetValue(1)! + ":" + (short)(Narrow)narrowRuntime.GetValue(2)!
            + ":" + (short)(Narrow)narrowRuntime.GetValue(3)!);
        Console.WriteLine("wide names generic=" + string.Join(",", Enum.GetNames<Wide>()));
        Console.WriteLine("wide names runtime=" + string.Join(",", Enum.GetNames(RuntimeType(true))));
        var wide = Enum.GetValues<Wide>();
        Console.WriteLine("wide values generic=" + wide.Length + ":" + (ulong)wide[0]
            + ":" + (ulong)wide[1] + ":" + (ulong)wide[2] + ":" + (ulong)wide[3]);
        var wideRuntime = Enum.GetValues(RuntimeType(true));
        Console.WriteLine("wide values runtime=" + wideRuntime.Length + ":" + (ulong)(Wide)wideRuntime.GetValue(0)!
            + ":" + (ulong)(Wide)wideRuntime.GetValue(1)! + ":" + (ulong)(Wide)wideRuntime.GetValue(2)!
            + ":" + (ulong)(Wide)wideRuntime.GetValue(3)!);
        Console.WriteLine("boxed name wide source=" + (Enum.GetName(typeof(Narrow), (1L << 32) + 1) ?? "null"));
        Console.WriteLine("boxed name small source=" + Enum.GetName(typeof(Wide), (byte)1));
        Console.WriteLine("boxed name signed=" + Enum.GetName(typeof(Narrow), -1L));
        Console.WriteLine("boxed name unsigned=" + Enum.GetName(typeof(Unsigned), 0x80000000u)
            + ":" + Enum.GetName(typeof(Unsigned), uint.MaxValue));
        Console.WriteLine("boxed defined exact=" + Enum.IsDefined(typeof(Narrow), (short)1)
            + ":" + Enum.IsDefined(typeof(Narrow), (short)2));
        Fault("boxed name bool", () => Enum.GetName(typeof(Narrow), true));
        Fault("boxed name float", () => Enum.GetName(typeof(Narrow), 1f));
        Fault("boxed defined wrong primitive", () => Enum.IsDefined(typeof(Narrow), 1));
        Fault("boxed defined wrong enum", () => Enum.IsDefined(typeof(Narrow), Wide.One));
        Fault("boxed defined float", () => Enum.IsDefined(typeof(Narrow), 1f));
        Console.WriteLine("enum alias metadata end");
    }

    private static void Fault(string label, Func<object?> action)
    {
        try
        {
            Console.WriteLine(label + "=" + action());
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + "=" + ex.GetType().Name + ":" + ex.Message);
        }
    }
}
