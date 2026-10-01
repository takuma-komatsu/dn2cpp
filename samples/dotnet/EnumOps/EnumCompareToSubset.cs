using System;
using System.Collections;
using System.Collections.Generic;

// Enum.CompareTo(object) through every mouth: a constrained call on an enum value
// (the shape `e.CompareTo(x)` compiles to), a System.Enum-typed and an
// IComparable-typed receiver, a generic constrained call, and the comparers. It
// answers the underlying type's CompareTo — the raw difference for a sub-word
// underlying type, whose sign still orders sorts and searches — sorts null first,
// and rejects a box of any other type with .NET's message naming both types.
namespace EnumCompareToSubset
{
    internal enum Tone { Low = 1, High = 7 }
    internal enum Other { X = 7 }
    internal enum Small : byte { A = 3, B = 250 }
    internal enum Tiny : sbyte { N = -100, P = 100 }
    internal enum Half : short { N = -30000, P = 30000 }
    internal enum UHalf : ushort { A = 1, B = 65000 }
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
            Console.WriteLine("byte: " + Small.B.CompareTo(Small.A) + " " + Small.A.CompareTo(Small.B));
            Console.WriteLine("sbyte: " + Tiny.P.CompareTo(Tiny.N) + " " + Tiny.N.CompareTo(Tiny.P));
            Console.WriteLine("short: " + Half.P.CompareTo(Half.N) + " " + Half.N.CompareTo(Half.P));
            Console.WriteLine("ushort: " + UHalf.B.CompareTo(UHalf.A) + " " + UHalf.A.CompareTo(UHalf.B));
            Enum small = Small.B;
            IComparable smallComparable = Small.B;
            Console.WriteLine("sub-word mouths: " + small.CompareTo(Small.A) + " " + smallComparable.CompareTo(Small.A)
                + " " + Compare(Small.B, Small.A) + " " + Compare(Half.N, Half.P));
            Console.WriteLine("comparer: " + Comparer<Small>.Default.Compare(Small.B, Small.A) + " "
                + Comparer<Tiny>.Default.Compare(Tiny.N, Tiny.P) + " " + Comparer<UHalf>.Default.Compare(UHalf.B, UHalf.A)
                + " " + Comparer.Default.Compare(Small.B, Small.A) + " " + Comparer<object>.Default.Compare(Half.P, Half.N));
            var halves = new[] { Half.P, Half.N, (Half)1 };
            Array.Sort(halves);
            var boxedSmalls = new object[] { Small.B, Small.A };
            Array.Sort(boxedSmalls);
            var tinies = new List<Tiny> { Tiny.P, Tiny.N };
            tinies.Sort();
            Console.WriteLine("sorted: " + halves[0] + "," + halves[1] + "," + halves[2] + " " + boxedSmalls[0] + "," + boxedSmalls[1] + " "
                + tinies[0] + "," + tinies[1] + " " + Array.BinarySearch(halves, Half.P));
            var wides = new[] { Wide.B, Wide.A };
            Array.Sort(wides);
            var ubigs = new List<UBig> { UBig.B, UBig.A };
            ubigs.Sort();
            Console.WriteLine("unsigned comparer: " + Comparer<Wide>.Default.Compare(Wide.B, Wide.A) + " "
                + Comparer<UBig>.Default.Compare(UBig.B, UBig.A) + " " + wides[0] + "," + wides[1] + " "
                + ubigs[0] + "," + ubigs[1]);
        }
    }
}
