using System;

// Enum.CompareTo(object) through every mouth: a constrained call on an enum value
// (the shape `e.CompareTo(x)` compiles to), a System.Enum-typed and an
// IComparable-typed receiver, and a generic constrained call. It orders by the
// underlying value, sorts null first, and rejects a box of any other type with
// .NET's message naming both types.
namespace EnumCompareToSubset
{
    internal enum Tone { Low = 1, High = 7 }
    internal enum Other { X = 7 }
    internal enum Small : byte { A = 3, B = 250 }
    internal enum Wide : uint { A = 1, B = uint.MaxValue }
    internal enum Big : long { N = long.MinValue, P = long.MaxValue }
    internal enum UBig : ulong { A = 1, B = ulong.MaxValue }

    internal static class Program
    {
        private static string Try(Func<int> compare)
        {
            try
            {
                return compare().ToString();
            }
            catch (ArgumentException ex)
            {
                return ex.GetType().Name + ": " + ex.Message + (ex.ParamName is null ? "" : " [" + ex.ParamName + "]");
            }
        }

        private static int Compare<T>(T value, object other) where T : IComparable => value.CompareTo(other);

        internal static void __GateEntry()
        {
            Console.WriteLine("== enum CompareTo ==");
            Tone tone = Tone.High;
            Console.WriteLine("int: " + tone.CompareTo(Tone.Low) + " " + Tone.Low.CompareTo(Tone.High) + " "
                + tone.CompareTo(Tone.High) + " " + tone.CompareTo(null));
            Console.WriteLine("uint: " + Wide.B.CompareTo(Wide.A) + " " + Wide.A.CompareTo(Wide.B));
            Console.WriteLine("long: " + Big.P.CompareTo(Big.N) + " " + Big.N.CompareTo(Big.P));
            Console.WriteLine("ulong: " + UBig.B.CompareTo(UBig.A) + " " + UBig.A.CompareTo(UBig.B));
            Console.WriteLine("other enum: " + Try(() => tone.CompareTo(Other.X)));
            Console.WriteLine("boxed int: " + Try(() => tone.CompareTo(7)));
            Console.WriteLine("string: " + Try(() => Small.A.CompareTo("A")));
            Enum boxed = Tone.High;
            IComparable comparable = Tone.High;
            Console.WriteLine("Enum receiver: " + boxed.CompareTo(Tone.Low) + " " + Try(() => boxed.CompareTo(Small.A)));
            Console.WriteLine("IComparable receiver: " + comparable.CompareTo(Tone.Low) + " "
                + Try(() => comparable.CompareTo(3)));
            Console.WriteLine("generic: " + Compare(Tone.High, Tone.Low) + " " + Compare(Big.N, Big.P) + " "
                + Compare(Tone.High, null) + " " + Try(() => Compare(Tone.High, Other.X)));
            Console.WriteLine("equals: " + tone.Equals(Tone.High) + " " + tone.Equals(7) + " " + tone.Equals(Other.X)
                + " " + tone.Equals(null));
        }
    }
}
