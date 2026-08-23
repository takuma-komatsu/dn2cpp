using System;

namespace PrimitiveObjectCompareSubset
{
    internal static class Program
    {
        private static string Foreign(Func<int> compare)
        {
            try
            {
                compare();
                return "no-throw";
            }
            catch (Exception ex)
            {
                return ex.GetType().Name;
            }
        }

        private static void Print(string name, int typed, int nil, int equal, int less, int greater,
            Func<int> foreign) =>
            Console.WriteLine($"{name}=typed:{typed}|object:{nil},{equal},{less},{greater},{Foreign(foreign)}");

        internal static void Run()
        {
            Print("bool", false.CompareTo(true), false.CompareTo(null), false.CompareTo((object)false),
                false.CompareTo((object)true), true.CompareTo((object)false),
                () => false.CompareTo((object)0));
            Print("char", '\uffff'.CompareTo('\0'), '\0'.CompareTo(null), '\0'.CompareTo((object)'\0'),
                '\0'.CompareTo((object)'\uffff'), '\uffff'.CompareTo((object)'\0'),
                () => '\0'.CompareTo((object)(ushort)0));
            Print("byte", ((byte)200).CompareTo((byte)1), ((byte)1).CompareTo(null), ((byte)1).CompareTo((object)(byte)1),
                ((byte)1).CompareTo((object)(byte)2), ((byte)2).CompareTo((object)(byte)1),
                () => ((byte)1).CompareTo((object)(sbyte)1));
            Print("sbyte", ((sbyte)-2).CompareTo((sbyte)1), ((sbyte)-2).CompareTo(null), ((sbyte)-2).CompareTo((object)(sbyte)-2),
                ((sbyte)-2).CompareTo((object)(sbyte)1), ((sbyte)1).CompareTo((object)(sbyte)-2),
                () => ((sbyte)1).CompareTo((object)(byte)1));
            Print("short", ((short)-2).CompareTo((short)1), ((short)-2).CompareTo(null), ((short)-2).CompareTo((object)(short)-2),
                ((short)-2).CompareTo((object)(short)1), ((short)1).CompareTo((object)(short)-2),
                () => ((short)1).CompareTo((object)(ushort)1));
            Print("ushort", ((ushort)65000).CompareTo((ushort)1), ((ushort)1).CompareTo(null), ((ushort)1).CompareTo((object)(ushort)1),
                ((ushort)1).CompareTo((object)(ushort)2), ((ushort)2).CompareTo((object)(ushort)1),
                () => ((ushort)1).CompareTo((object)(short)1));
            Print("int", int.MinValue.CompareTo(int.MaxValue), (-2).CompareTo(null), (-2).CompareTo((object)-2),
                (-2).CompareTo((object)1), 1.CompareTo((object)-2),
                () => 1.CompareTo((object)1u));
            Print("uint", uint.MaxValue.CompareTo(0u), 1u.CompareTo(null), 1u.CompareTo((object)1u),
                1u.CompareTo((object)2u), 2u.CompareTo((object)1u),
                () => 1u.CompareTo((object)1));
            Print("long", long.MinValue.CompareTo(long.MaxValue), (-2L).CompareTo(null), (-2L).CompareTo((object)-2L),
                (-2L).CompareTo((object)1L), 1L.CompareTo((object)-2L),
                () => 1L.CompareTo((object)1UL));
            Print("ulong", ulong.MaxValue.CompareTo(0UL), 1UL.CompareTo(null), 1UL.CompareTo((object)1UL),
                1UL.CompareTo((object)2UL), 2UL.CompareTo((object)1UL),
                () => 1UL.CompareTo((object)1L));
            Print("float", float.NaN.CompareTo(0f), (-2f).CompareTo(null), (-2f).CompareTo((object)-2f),
                float.NaN.CompareTo((object)0f), 0f.CompareTo((object)float.NaN),
                () => 1f.CompareTo((object)1d));
            Print("double", 0d.CompareTo(double.NaN), (-2d).CompareTo(null), (-2d).CompareTo((object)-2d),
                double.NaN.CompareTo((object)0d), 0d.CompareTo((object)double.NaN),
                () => 1d.CompareTo((object)1f));

            nint niLow = -2;
            nint niHigh = 1;
            nuint nuLow = 1;
            nuint nuHigh = 2;
            Print("nint", niLow.CompareTo(niHigh), niLow.CompareTo(null), niLow.CompareTo((object)niLow),
                niLow.CompareTo((object)niHigh), niHigh.CompareTo((object)niLow),
                () => niLow.CompareTo((object)nuLow));
            Print("nuint", nuLow.CompareTo(nuHigh), nuLow.CompareTo(null), nuLow.CompareTo((object)nuLow),
                nuLow.CompareTo((object)nuHigh), nuHigh.CompareTo((object)nuLow),
                () => nuLow.CompareTo((object)niLow));

            Console.WriteLine("char-interface="
                + ((IComparable)'\uffff').CompareTo((object)'\0'));
            Console.WriteLine("subword-interface="
                + ((IComparable)(sbyte)-2).CompareTo((object)(sbyte)1) + ","
                + ((IComparable)(byte)200).CompareTo((object)(byte)1) + ","
                + ((IComparable)(short)-2).CompareTo((object)(short)1) + ","
                + ((IComparable)(ushort)65000).CompareTo((object)(ushort)1));
        }
    }
}
