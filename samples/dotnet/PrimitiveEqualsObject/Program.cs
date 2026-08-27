using System;

namespace PrimitiveEqualsObject
{
    // Gate driver: the Equals(object) overload of the integer primitives, reached
    // through value-object wrappers of the shape source generators emit
    // (`obj is Wrapper other && value.Equals(other.value)` lowers the inner call to
    // Int32.Equals(object) once `other.value` is boxed by the generic path). Real .NET
    // answers true only for a box of the very same primitive type with an equal
    // payload: no cross-type numeric comparison, null is false.
    internal readonly struct Agi
    {
        private readonly int value;
        public Agi(int value) { this.value = value; }
        public override bool Equals(object obj) => obj is Agi other && value.Equals((object)other.value);
        public override int GetHashCode() => value.GetHashCode();
    }

    internal readonly struct Ticks
    {
        private readonly long value;
        public Ticks(long value) { this.value = value; }
        public override bool Equals(object obj) => obj is Ticks other && value.Equals((object)other.value);
        public override int GetHashCode() => value.GetHashCode();
    }

    internal static class Program
    {
        private static void Main()
        {
            object boxedInt = 42;
            object boxedLong = 42L;
            object boxedShort = (short)42;
            object boxedByte = (byte)42;
            object boxedChar = 'x';
            object boxedBool = true;

            Console.WriteLine(42.Equals(boxedInt));
            Console.WriteLine(42.Equals(boxedLong));
            Console.WriteLine(42.Equals((object)43));
            Console.WriteLine(42.Equals(null));
            Console.WriteLine(42L.Equals(boxedLong));
            Console.WriteLine(42L.Equals(boxedInt));
            Console.WriteLine(((short)42).Equals(boxedShort));
            Console.WriteLine(((short)42).Equals(boxedInt));
            Console.WriteLine(((byte)42).Equals(boxedByte));
            Console.WriteLine('x'.Equals(boxedChar));
            Console.WriteLine('x'.Equals((object)'y'));
            Console.WriteLine(true.Equals(boxedBool));
            Console.WriteLine(true.Equals((object)false));
            Console.WriteLine(int.MinValue.Equals((object)int.MinValue));
            Console.WriteLine((-1).Equals((object)uint.MaxValue));

            Console.WriteLine(new Agi(7).Equals(new Agi(7)));
            Console.WriteLine(new Agi(7).Equals(new Agi(8)));
            Console.WriteLine(new Agi(7).Equals(new Ticks(7)));
            Console.WriteLine(new Ticks(long.MaxValue).Equals(new Ticks(long.MaxValue)));
            Console.WriteLine(new Ticks(1).Equals(null));
        }
    }
}
