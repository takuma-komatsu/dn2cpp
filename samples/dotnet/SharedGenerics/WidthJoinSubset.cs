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
    }
}
