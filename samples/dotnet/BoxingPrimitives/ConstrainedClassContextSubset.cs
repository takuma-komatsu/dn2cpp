using System;
using System.Collections.Generic;
using System.Globalization;

// IComparable<T>.CompareTo and IEquatable<T>.Equals called through `constrained. !B` in a
// method of a closed generic class: the callee's !0 is the interface's own argument, never
// the class's first type argument, so a decimal, date/time or primitive B compares as
// itself behind a string or object A. A string B reaches the same calls from shared
// generic bodies, which dispatch them through String's interface map.
//
// RunTypedSlots: the same calls bind the slot of the instantiation they name. A struct
// implementing IEquatable/IComparable for another type beside itself, only for another
// type, explicitly beside a public overload of the same shape, or explicitly alone runs
// the body its interface map names, a generic struct over a reference and a value type
// argument included, and so does a call through the boxed interface.
//
// RunInterfaceShapes: a constrained call to an interface member named like an Object
// virtual — IEqualityComparer<T>.Equals/GetHashCode, IFormattable.ToString(string,
// IFormatProvider), IConvertible.ToString(IFormatProvider) — runs that member on a struct
// that overrides the Object virtual, on a primitive and on an enum, as does
// IConvertible.GetTypeCode.
namespace ConstrainedClassContextSubset
{
    internal static class Trace
    {
        internal static string Text = "";

        internal static string Take(object result)
        {
            string row = result + ":" + Text;
            Text = "";
            return row;
        }
    }

    internal struct Money : IEquatable<Money>, IEquatable<decimal>, IComparable<Money>, IComparable<decimal>
    {
        public decimal Value;

        public Money(decimal value)
        {
            Value = value;
        }

        public bool Equals(Money other)
        {
            Trace.Text += "M";
            return Value == other.Value;
        }

        public bool Equals(decimal other)
        {
            Trace.Text += "D";
            return Value == other;
        }

        public override bool Equals(object obj)
        {
            Trace.Text += "O";
            return obj is Money other && other.Value == Value;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(Money other)
        {
            Trace.Text += "m";
            return Value.CompareTo(other.Value);
        }

        public int CompareTo(decimal other)
        {
            Trace.Text += "d";
            return Value.CompareTo(other) * 10;
        }
    }

    internal struct ForeignOnly : IEquatable<decimal>, IComparable<decimal>
    {
        public decimal Value;

        public bool Equals(decimal other)
        {
            Trace.Text += "D";
            return Value == other;
        }

        public override bool Equals(object obj)
        {
            Trace.Text += "O";
            return false;
        }

        public override int GetHashCode() => 0;

        public int CompareTo(decimal other)
        {
            Trace.Text += "d";
            return Value.CompareTo(other) * 10;
        }
    }

    internal struct ExplicitBeside : IEquatable<ExplicitBeside>, IComparable<ExplicitBeside>
    {
        public int Value;

        public bool Equals(ExplicitBeside other)
        {
            Trace.Text += "P";
            return Value == other.Value;
        }

        bool IEquatable<ExplicitBeside>.Equals(ExplicitBeside other)
        {
            Trace.Text += "X";
            return Value != other.Value;
        }

        public int CompareTo(ExplicitBeside other)
        {
            Trace.Text += "p";
            return 1;
        }

        int IComparable<ExplicitBeside>.CompareTo(ExplicitBeside other)
        {
            Trace.Text += "x";
            return -1;
        }

        public override bool Equals(object obj)
        {
            Trace.Text += "O";
            return false;
        }

        public override int GetHashCode() => 0;
    }

    internal struct ExplicitAlone : IEquatable<ExplicitAlone>, IEquatable<long>
    {
        public int Value;

        bool IEquatable<ExplicitAlone>.Equals(ExplicitAlone other)
        {
            Trace.Text += "X";
            return Value == other.Value;
        }

        bool IEquatable<long>.Equals(long other)
        {
            Trace.Text += "L";
            return Value == other;
        }

        public override bool Equals(object obj)
        {
            Trace.Text += "O";
            return false;
        }

        public override int GetHashCode() => 0;
    }

    internal struct Wrap<T> : IEquatable<Wrap<T>>, IEquatable<T>
    {
        public T Value;

        public bool Equals(Wrap<T> other)
        {
            Trace.Text += "W";
            return System.Collections.Generic.EqualityComparer<T>.Default.Equals(Value, other.Value);
        }

        public bool Equals(T other)
        {
            Trace.Text += "T";
            return System.Collections.Generic.EqualityComparer<T>.Default.Equals(Value, other);
        }

        public override bool Equals(object obj)
        {
            Trace.Text += "O";
            return false;
        }

        public override int GetHashCode() => 0;
    }

    internal sealed class Pair<A, B> where B : IComparable<B>, IEquatable<B>
    {
        public string Row(B low, B high) => low.CompareTo(high) + " " + high.CompareTo(low) + " "
            + low.CompareTo(low) + " " + low.Equals(low) + " " + low.Equals(high);
    }

    internal struct ModComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => x % 10 == y % 10;

        public int GetHashCode(int obj) => obj % 10 + 100;

        public override bool Equals(object obj) => false;

        public override int GetHashCode() => -1;
    }

    internal struct LengthComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => x.Length == y.Length;

        public int GetHashCode(string obj) => obj.Length;
    }

    internal struct Price : IFormattable
    {
        public int Cents;

        public override string ToString() => "plain";

        public string ToString(string format, IFormatProvider provider) => "fmt:" + format + ":" + Cents;
    }

    internal enum Tone { Low, Mid, High }

    [Flags]
    internal enum Mask : byte { None = 0, A = 1, B = 2 }

    internal static class Program
    {
        private static bool Same<T>(T value, T other) where T : IEquatable<T> => value.Equals(other);

        internal static void Run()
        {
            Console.WriteLine("== constrained typed calls in a generic class ==");
            Console.WriteLine("TimeSpan: " + new Pair<string, TimeSpan>().Row(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2))
                + " | " + new Pair<object, TimeSpan>().Row(TimeSpan.Zero, TimeSpan.MaxValue));
            Console.WriteLine("decimal: " + new Pair<string, decimal>().Row(1.5m, 2m) + " | " + new Pair<object, decimal>().Row(-1m, 0m));
            Console.WriteLine("DateTime: " + new Pair<string, DateTime>().Row(new DateTime(2020, 1, 1), new DateTime(2021, 1, 1))
                + " | " + new Pair<object, DateTime>().Row(DateTime.MinValue, DateTime.MaxValue));
            Console.WriteLine("DateTimeOffset: " + new Pair<string, DateTimeOffset>().Row(
                new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.FromHours(-1))));
            Console.WriteLine("DateOnly: " + new Pair<object, DateOnly>().Row(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 2)));
            Console.WriteLine("TimeOnly: " + new Pair<string, TimeOnly>().Row(new TimeOnly(1, 0), new TimeOnly(2, 0)));
            Console.WriteLine("int: " + new Pair<string, int>().Row(1, 2) + " | " + new Pair<object, int>().Row(-5, 5));
            Console.WriteLine("string: " + new Pair<object, string>().Row("a", "b") + " | " + Same("x", "x") + " " + Same("x", "y"));
        }

        private static bool Eq<T, U>(T value, U other) where T : IEquatable<U> => value.Equals(other);

        private static int Cmp<T, U>(T value, U other) where T : IComparable<U> => value.CompareTo(other);

        internal static void RunTypedSlots()
        {
            Console.WriteLine("== constrained typed slots ==");
            var money = new Money(5m);
            Console.WriteLine("beside itself: " + Trace.Take(Eq<Money, decimal>(money, 5m)) + " "
                + Trace.Take(Eq<Money, Money>(money, new Money(5m))) + " " + Trace.Take(Cmp<Money, decimal>(money, 4m))
                + " " + Trace.Take(Cmp<Money, Money>(money, new Money(4m))));
            var foreign = new ForeignOnly { Value = 2m };
            Console.WriteLine("another type only: " + Trace.Take(Eq<ForeignOnly, decimal>(foreign, 2m)) + " "
                + Trace.Take(Cmp<ForeignOnly, decimal>(foreign, 3m)));
            var beside = new ExplicitBeside { Value = 1 };
            Console.WriteLine("explicit beside public: " + Trace.Take(Eq<ExplicitBeside, ExplicitBeside>(beside, beside)) + " "
                + Trace.Take(Cmp<ExplicitBeside, ExplicitBeside>(beside, beside)) + " " + Trace.Take(beside.Equals(beside))
                + " " + Trace.Take(beside.CompareTo(beside)));
            var alone = new ExplicitAlone { Value = 7 };
            Console.WriteLine("explicit alone: " + Trace.Take(Eq<ExplicitAlone, ExplicitAlone>(alone, alone)) + " "
                + Trace.Take(Eq<ExplicitAlone, long>(alone, 7L)));
            var text = new Wrap<string> { Value = "a" };
            var number = new Wrap<int> { Value = 3 };
            Console.WriteLine("generic struct: " + Trace.Take(Eq<Wrap<string>, string>(text, "a")) + " "
                + Trace.Take(Eq<Wrap<string>, Wrap<string>>(text, text)) + " " + Trace.Take(Eq<Wrap<int>, int>(number, 3))
                + " " + Trace.Take(Eq<Wrap<int>, Wrap<int>>(number, number)));
            IEquatable<decimal> equatable = money;
            IComparable<decimal> comparable = money;
            Console.WriteLine("boxed interface: " + Trace.Take(equatable.Equals(5m)) + " " + Trace.Take(comparable.CompareTo(1m)));
            Console.WriteLine("constrained typed slots end");
        }

        private static string Match<TComparer, U>(TComparer comparer, U x, U y) where TComparer : IEqualityComparer<U> =>
            comparer.Equals(x, y) + ":" + comparer.GetHashCode(x);

        private static string Format<T>(T value, string format) where T : IFormattable =>
            value.ToString(format, CultureInfo.InvariantCulture);

        private static string Convert<T>(T value) where T : IConvertible =>
            value.ToString(CultureInfo.InvariantCulture) + "/" + value.GetTypeCode();

        internal static void RunInterfaceShapes()
        {
            Console.WriteLine("== constrained interface shapes ==");
            Console.WriteLine("struct comparer: " + Match(new ModComparer(), 13, 23) + " "
                + Match(new LengthComparer(), "ab", "cd"));
            Console.WriteLine("struct formattable: " + Format(new Price { Cents = 5 }, "C"));
            Console.WriteLine("primitive formattable: " + Format(255, "X4") + " " + Format(2.5, "F2") + " "
                + Format((byte)10, "D3"));
            Console.WriteLine("enum formattable: " + Format(Tone.High, "D") + " " + Format(Tone.High, "G") + " "
                + Format(Mask.A | Mask.B, "G") + " " + Format(Mask.B, "X"));
            Console.WriteLine("convertible: " + Convert(Tone.Mid) + " " + Convert(42) + " " + Convert(true) + " "
                + Convert('c') + " " + Convert("s"));
            Console.WriteLine("bool with provider: " + true.ToString(CultureInfo.InvariantCulture));
            Console.WriteLine("constrained interface shapes end");
        }
    }
}
