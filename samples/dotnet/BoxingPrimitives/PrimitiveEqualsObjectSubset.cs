using System;

namespace PrimitiveEqualsObjectSubset
{
    internal readonly struct Agi
    {
        private readonly int value;

        public Agi(int value) { this.value = value; }

        public override bool Equals(object obj) =>
            obj is Agi other && value.Equals((object)other.value);

        public override int GetHashCode() => value.GetHashCode();
    }

    internal readonly struct Ticks
    {
        private readonly long value;

        public Ticks(long value) { this.value = value; }

        public override bool Equals(object obj) =>
            obj is Ticks other && value.Equals((object)other.value);

        public override int GetHashCode() => value.GetHashCode();
    }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("-- primitive Equals(object) payload --");

            bool boolean = true;
            char character = '\uffff';
            sbyte signedByte = -7;
            byte unsignedByte = 7;
            short signedShort = -300;
            ushort unsignedShort = 60000;
            int signedInt = -1234567;
            uint unsignedInt = 4000000000U;
            long signedLong = -5000000000L;
            ulong unsignedLong = 15000000000UL;
            nint signedPointer = (nint)0x12345678;
            nuint unsignedPointer = (nuint)0x12345678U;

            Print("bool", boolean.Equals((object)true), boolean.Equals((object)false));
            Print("char", character.Equals((object)'\uffff'), character.Equals((object)'\0'));
            Print("sbyte", signedByte.Equals((object)(sbyte)-7), signedByte.Equals((object)(sbyte)7));
            Print("byte", unsignedByte.Equals((object)(byte)7), unsignedByte.Equals((object)(byte)8));
            Print("short", signedShort.Equals((object)(short)-300), signedShort.Equals((object)(short)300));
            Print("ushort", unsignedShort.Equals((object)(ushort)60000), unsignedShort.Equals((object)(ushort)1));
            Print("int", signedInt.Equals((object)(-1234567)), signedInt.Equals((object)1234567));
            Print("uint", unsignedInt.Equals((object)4000000000U), unsignedInt.Equals((object)1U));
            Print("long", signedLong.Equals((object)(-5000000000L)), signedLong.Equals((object)5000000000L));
            Print("ulong", unsignedLong.Equals((object)15000000000UL), unsignedLong.Equals((object)1UL));
            Print("nint", signedPointer.Equals((object)(nint)0x12345678),
                signedPointer.Equals((object)(nint)0x12345677));
            Print("nuint", unsignedPointer.Equals((object)(nuint)0x12345678U),
                unsignedPointer.Equals((object)(nuint)0x12345677U));

            Console.WriteLine("subword edges="
                + unsignedByte.Equals((object)(sbyte)7) + "," + unsignedByte.Equals(null));
            Console.WriteLine("pointer edges="
                + signedPointer.Equals((object)unsignedPointer) + "," + signedPointer.Equals(null));
            Console.WriteLine("typed siblings="
                + unsignedByte.Equals((byte)7) + "," + signedPointer.Equals((nint)0x12345678));

            Console.WriteLine("wrappers="
                + new Agi(7).Equals(new Agi(7)) + ","
                + new Agi(7).Equals(new Agi(8)) + ","
                + new Agi(7).Equals(new Ticks(7)) + ","
                + new Ticks(long.MaxValue).Equals(new Ticks(long.MaxValue)) + ","
                + new Ticks(1).Equals(null));

            Console.WriteLine("-- primitive Equals(object) virtual method groups --");
            Func<object, bool> byteEquals = ((byte)7).Equals;
            Func<object, bool> intEquals = 1234567.Equals;
            Func<object, bool> nintEquals = ((nint)0x12345678).Equals;
            PrintMethodGroup("byte", byteEquals, (byte)7, (byte)8, (sbyte)7);
            PrintMethodGroup("int", intEquals, 1234567, -1234567, 1234567L);
            PrintMethodGroup("nint", nintEquals, (nint)0x12345678,
                (nint)0x12345677, (nuint)0x12345678U);
        }

        private static void Print(string name, bool same, bool different) =>
            Console.WriteLine(name + "=" + same + "," + different);

        private static void PrintMethodGroup(string name, Func<object, bool> equals,
            object same, object different, object foreign) =>
            Console.WriteLine(name + "=" + equals(same) + "," + equals(different) + ","
                + equals(foreign) + "," + equals(null));
    }
}
