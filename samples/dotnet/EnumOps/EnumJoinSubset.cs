using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// string.Join<T>, StringBuilder.AppendJoin<T> and string.Concat<T> over enum elements:
// each element formats by name, as Enum.ToString does — an undefined value as its number,
// a [Flags] combination by its names — at every underlying width, from an array and from
// a List<T>; a null sequence is rejected with .NET's ArgumentNullException. The last
// block reads the same elements through IEnumerable<T> (an array, a List<T> and an
// iterator behind it) and from other collections, and disposes the enumerator once the
// join ends, also when reading an element throws.
namespace EnumJoinSubset
{
    internal enum Tone { Low = 1, High = 7 }
    internal enum Small : byte { A = 3, B = 250 }
    internal enum Tiny : sbyte { N = -100, P = 100 }
    internal enum Half : short { N = -30000, P = 30000 }
    internal enum HalfWide : ushort { A = 3, B = ushort.MaxValue }
    internal enum Wide : uint { A = 1, B = uint.MaxValue }
    internal enum Big : long { N = long.MinValue, P = long.MaxValue }
    internal enum BigWide : ulong { A = 1, B = 1UL << 63 }
    internal enum EmptyWide : ulong { }
    internal enum Empty32 : uint { }
    [Flags] internal enum UnsignedPerm : uint { Read = 1 }
    [Flags] internal enum BigPerm : ulong { Read = 1 }
    [Flags] internal enum Perm { None = 0, Read = 1, Write = 2 }

    internal static class Program
    {
        internal static void __GateEntry()
        {
            Console.WriteLine("== enum Join ==");
            Console.WriteLine("int: " + string.Join(",", new[] { Tone.Low, Tone.High, (Tone)3 }));
            Console.WriteLine("byte: " + string.Join(",", new[] { Small.B, Small.A }));
            Console.WriteLine("sbyte: " + string.Join(",", new[] { Tiny.N, Tiny.P, (Tiny)(-1) }));
            Console.WriteLine("short: " + string.Join(",", new[] { Half.N, Half.P }));
            Console.WriteLine("uint: " + string.Join(",", new[] { Wide.B, Wide.A }));
            Console.WriteLine("long: " + string.Join(",", new[] { Big.N, Big.P }));
            Console.WriteLine("flags: " + string.Join(" | ", new[] { Perm.Read | Perm.Write, Perm.None }));
            var tones = new List<Tone> { Tone.High, Tone.Low };
            Console.WriteLine("list: " + string.Join(';', tones));
            Console.WriteLine("append: " + new StringBuilder("[").AppendJoin(", ", new List<Small> { Small.A, Small.B }).Append(']'));
            Tone[] none = null;
            try
            {
                Console.WriteLine("null: " + string.Join(",", none));
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("null: " + ex.GetType().Name + ": " + ex.Message);
            }

            Console.WriteLine("== enum Concat ==");
            Console.WriteLine("int: " + string.Concat(new List<Tone> { Tone.High, Tone.Low, (Tone)3 }));
            Console.WriteLine("byte: " + string.Concat(new List<Small> { Small.B, Small.A }));
            Console.WriteLine("sbyte: " + string.Concat(new List<Tiny> { Tiny.N, Tiny.P, (Tiny)(-1) }));
            Console.WriteLine("short: " + string.Concat(new List<Half> { Half.P, Half.N }));
            Console.WriteLine("uint: " + string.Concat(new List<Wide> { Wide.B, Wide.A }));
            Console.WriteLine("long: " + string.Concat(new List<Big> { Big.P, Big.N }));
            Console.WriteLine("flags: " + string.Concat(new List<Perm> { Perm.Read | Perm.Write, Perm.None }));
            Console.WriteLine("array: " + string.Concat(new[] { Tone.Low, (Tone)9, Tone.High }) + " " + string.Concat(new[] { Big.N }));
            List<Tone> noTones = null;
            try
            {
                Console.WriteLine("null: " + string.Concat(noTones));
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("null: " + ex.GetType().Name + ": " + ex.Message);
            }

            Console.WriteLine("== enum Join over enumerables ==");
            IEnumerable<Tone> toneSeq = new List<Tone> { Tone.High, (Tone)5, Tone.Low };
            Console.WriteLine("ienum list: " + string.Join(",", toneSeq));
            IEnumerable<Small> smallSeq = new[] { Small.B, Small.A, (Small)9 };
            Console.WriteLine("ienum array: " + string.Join("/", smallSeq));
            Console.WriteLine("iterator: " + string.Join(" ", Perms()) + " " + string.Concat(Perms()));
            Console.WriteLine("hashset: " + string.Join(",", new HashSet<Half> { Half.P, Half.N }));
            Console.WriteLine("hashset sbyte: " + string.Join(",", new HashSet<Tiny> { Tiny.P, Tiny.N, (Tiny)(-1) }));
            Console.WriteLine("keys: " + string.Join('|', new Dictionary<Wide, int> { [Wide.B] = 1, [Wide.A] = 2 }.Keys));
            Console.WriteLine("concat values: " + string.Concat(new Dictionary<int, Big> { [1] = Big.P, [2] = Big.N }.Values));
            Console.WriteLine("concat ienum: " + string.Concat((IEnumerable<Tone>)new[] { Tone.Low, (Tone)0 }));
            Console.WriteLine("append ienum: " + new StringBuilder("<").AppendJoin(", ",
                (IEnumerable<Perm>)new List<Perm> { Perm.Read | Perm.Write, (Perm)6, Perm.None }).Append('>'));
            Console.WriteLine("append keys: " + new StringBuilder().AppendJoin('+',
                new Dictionary<Small, int> { [Small.B] = 0, [Small.A] = 0 }.Keys));
            Console.WriteLine("disposed: " + string.Join(",", new Cursors(0)) + " " + Cursors.Disposals);
            Console.WriteLine("append disposed: " + new StringBuilder().AppendJoin(';', new Cursors(0)) + " " + Cursors.Disposals);
            try
            {
                Console.WriteLine("throwing cursor: " + string.Concat(new Cursors(2)));
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("throwing cursor: " + ex.Message + " " + Cursors.Disposals);
            }
            IEnumerable<Tone> noSeq = null;
            try
            {
                Console.WriteLine("null ienum: " + string.Concat(noSeq));
            }
            catch (ArgumentNullException ex)
            {
                Console.WriteLine("null ienum: " + ex.GetType().Name + ": " + ex.Message);
            }
            Console.WriteLine("ushort: " + string.Join(",", new[] { HalfWide.B, HalfWide.A, (HalfWide)40000 }));
            Console.WriteLine("ulong: " + string.Concat(new[] { BigWide.B, BigWide.A, (BigWide)ulong.MaxValue }));
            Console.WriteLine("uint undefined: " + string.Join(",", new[] { (Wide)4000000000u }) + " "
                + new StringBuilder().AppendJoin(',', (IEnumerable<Wide>)new List<Wide> { (Wide)4000000000u }));
            Console.WriteLine("unsigned flags: " + string.Join(",", new[] { (UnsignedPerm)uint.MaxValue }) + " "
                + string.Concat((IEnumerable<BigPerm>)new List<BigPerm> { (BigPerm)ulong.MaxValue }));
            Console.WriteLine("empty enums: " + string.Join(",", new[] { (Empty32)uint.MaxValue }) + " "
                + string.Concat(new[] { (EmptyWide)ulong.MaxValue }));
        }

        private static IEnumerable<Perm> Perms()
        {
            yield return Perm.Write;
            yield return Perm.Read | Perm.Write;
            yield return (Perm)8;
        }
    }

    // Counts enumerator disposals; its enumerator throws on reading element `failAt`.
    internal sealed class Cursors : IEnumerable<Tone>
    {
        internal static int Disposals;
        private readonly int _failAt;

        internal Cursors(int failAt) => _failAt = failAt;

        public IEnumerator<Tone> GetEnumerator() => new Cursor(_failAt);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Cursor : IEnumerator<Tone>
        {
            private readonly int _failAt;
            private int _index;

            internal Cursor(int failAt) => _failAt = failAt;

            public Tone Current => _index == _failAt
                ? throw new InvalidOperationException("cursor " + _index)
                : (Tone)(_index * 3 - 2);

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index <= 3;

            public void Reset() => _index = 0;

            public void Dispose() => Disposals++;
        }
    }
}
