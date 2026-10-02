// string.Join<T>, string.Concat<T> and StringBuilder.AppendJoin<T> in generic
// methods called over int and an int-backed enum, and over uint and a
// uint-backed enum. The width placeholder that could share each pair's body
// does not say which type it stands for, so every element still formats by
// its real type: the enum by name.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WidthJoinSubset
{
    internal enum Color { Red, Green, Blue }

    internal enum Mode : uint { Off, On = 4000000000 }

    internal struct Gauge : IEquatable<int>, IEquatable<Color>, IComparable<int>, IComparable<Color>
    {
        public int Value;

        public bool Equals(int other) => Value == other;

        public bool Equals(Color other) => Value == (int)other + 100;

        public int CompareTo(int other) => Value.CompareTo(other);

        public int CompareTo(Color other) => -Value.CompareTo((int)other);
    }

    internal struct GaugeComparer : IEqualityComparer<int>, IEqualityComparer<Color>
    {
        public bool Equals(int x, int y) => x % 10 == y % 10;

        public int GetHashCode(int obj) => obj % 10;

        public bool Equals(Color x, Color y) => x != y;

        public int GetHashCode(Color obj) => 40 + (int)obj;
    }

    internal sealed class Tag<T>
    {
        public override string ToString() => "tag";
    }

    internal sealed class Dual : IEnumerable<string>, IEnumerable<object>
    {
        IEnumerator<string> IEnumerable<string>.GetEnumerator() =>
            ((IEnumerable<string>)new[] { "string" }).GetEnumerator();

        IEnumerator<object> IEnumerable<object>.GetEnumerator() =>
            ((IEnumerable<object>)new object[] { "object" }).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<string>)this).GetEnumerator();
    }

    internal static class Program
    {
        private static string Join<T>(T[] xs) => string.Join(",", xs);

        private static string JoinList<T>(List<T> xs) => string.Join('|', xs);

        private static string Concat<T>(T[] xs) => string.Concat(xs);

        private static string Append<T>(IEnumerable<T> xs) =>
            new StringBuilder("[").AppendJoin(";", xs).Append(']').ToString();

        private static string JoinTags<T>(IEnumerable<Tag<T>> xs) => string.Join("+", xs);

        private static string JoinSequence<T>(IEnumerable<T> xs) => string.Join("|", xs);

        private static string ConcatSequence<T>(IEnumerable<T> xs) => string.Concat(xs);

        internal static void Run()
        {
            Console.WriteLine("width join array=" + Join(new[] { 1, 2 }) + "/"
                + Join(new[] { Color.Red, Color.Green, (Color)7 }));
            Console.WriteLine("width join uint=" + Join(new[] { 3u, 4000000000u }) + "/"
                + Join(new[] { Mode.On, Mode.Off }));
            Console.WriteLine("width join list=" + JoinList(new List<int> { 5, 6 }) + "/"
                + JoinList(new List<Color> { Color.Blue, Color.Red }));
            Console.WriteLine("width concat=" + Concat(new[] { 7, 8 }) + "/"
                + Concat(new[] { Color.Green, Color.Blue }));
            Console.WriteLine("width append=" + Append(new List<int> { 9 }) + "/"
                + Append(new[] { Color.Red, Color.Blue }));
            Console.WriteLine("reference nested join="
                + JoinTags(new List<Tag<string>> { new Tag<string>(), new Tag<string>() }) + "/"
                + JoinTags(new List<Tag<object>> { new Tag<object>() }));
            var dual = new Dual();
            Console.WriteLine("reference join views=" + JoinSequence<string>(dual) + "/" + JoinSequence<object>(dual));
            Console.WriteLine("reference concat views=" + ConcatSequence<string>(dual) + "/" + ConcatSequence<object>(dual));
            Console.WriteLine("reference append views=" + Append<string>(dual) + "/" + Append<object>(dual));
        }

        private static bool Same<T, U>(T value, U other) where T : IEquatable<U> => value.Equals(other);

        private static int Order<T, U>(T value, U other) where T : IComparable<U> => value.CompareTo(other);

        private static string Match<TComparer, U>(TComparer comparer, U x, U y) where TComparer : IEqualityComparer<U> =>
            comparer.Equals(x, y) + ":" + comparer.GetHashCode(x);

        internal static void RunTypedSlots()
        {
            Console.WriteLine("== same-width constrained interface slots ==");
            var gauge = new Gauge { Value = 101 };
            Console.WriteLine("width typed slots=" + Same(gauge, 101) + "," + Same(gauge, Color.Green) + ","
                + Same(gauge, 1) + "/" + Order(gauge, 5) + "," + Order(gauge, Color.Blue));
            var comparer = new GaugeComparer();
            Console.WriteLine("width typed comparer=" + Match(comparer, 13, 23) + "/"
                + Match(comparer, Color.Red, Color.Blue));
            Console.WriteLine("same-width constrained interface slots end");
        }
    }
}
