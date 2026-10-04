using System;
using System.Collections.Generic;

namespace NestedInterfaceVarianceSubset
{
    internal class Animal { }
    internal sealed class Dog : Animal { }
    internal interface IOut<out T> { T Get(); }
    internal interface Outer_IOut<out T> { T Get(); }
    internal interface Outer_002BIOut<out T> { T Get(); }

    internal static class Outer
    {
        internal interface IOut<out T> { T Get(); }
        internal static class Middle
        {
            internal interface IOut<out T> { T Get(); }
        }
    }

    internal static class GenericOuter<TEnclosing>
    {
        internal static class Middle<TMiddle>
        {
            internal interface IOut<out T> { T Get(); }
        }
    }

    internal static class OtherOuter
    {
        internal interface IOut<out T> { T Get(); }
    }

    internal sealed class TopPublic : IOut<Dog>, IComparable<IOut<Animal>>
    {
        private readonly int _value;
        internal TopPublic(int value) { _value = value; }
        public Dog Get() => null;
        public int CompareTo(IOut<Animal> other) => _value.CompareTo(((TopPublic)other)._value);
        public override string ToString() => _value.ToString();
    }

    internal sealed class TopExplicit : IOut<Dog>, IComparable<IOut<Animal>>
    {
        private readonly int _value;
        internal TopExplicit(int value) { _value = value; }
        public Dog Get() => null;
        public int CompareTo(IOut<Animal> other) => ((TopExplicit)other)._value.CompareTo(_value);
        int IComparable<IOut<Animal>>.CompareTo(IOut<Animal> other) => _value.CompareTo(((TopExplicit)other)._value);
        public override string ToString() => _value.ToString();
    }

    internal sealed class NestedPublic : Outer.IOut<Dog>, IComparable<Outer.IOut<Animal>>
    {
        private readonly int _value;
        internal NestedPublic(int value) { _value = value; }
        public Dog Get() => null;
        public int CompareTo(Outer.IOut<Animal> other) => _value.CompareTo(((NestedPublic)other)._value);
        public override string ToString() => _value.ToString();
    }

    internal sealed class NestedExplicit : Outer.IOut<Dog>, IComparable<Outer.IOut<Animal>>
    {
        private readonly int _value;
        internal NestedExplicit(int value) { _value = value; }
        public Dog Get() => null;
        public int CompareTo(Outer.IOut<Animal> other) => ((NestedExplicit)other)._value.CompareTo(_value);
        int IComparable<Outer.IOut<Animal>>.CompareTo(Outer.IOut<Animal> other) => _value.CompareTo(((NestedExplicit)other)._value);
        public override string ToString() => _value.ToString();
    }

    internal sealed class DeepOrder : Outer.Middle.IOut<Dog>, IComparable<Outer.Middle.IOut<Animal>>
    {
        private readonly int _value;
        internal DeepOrder(int value) { _value = value; }
        public Dog Get() => null;
        public int CompareTo(Outer.Middle.IOut<Animal> other) => _value.CompareTo(((DeepOrder)other)._value);
        public override string ToString() => _value.ToString();
    }

    internal sealed class GenericOrder : GenericOuter<string>.Middle<int>.IOut<Dog>,
        IComparable<GenericOuter<string>.Middle<int>.IOut<Animal>>
    {
        private readonly int _value;
        internal GenericOrder(int value) { _value = value; }
        public Dog Get() => null;
        public int CompareTo(GenericOuter<string>.Middle<int>.IOut<Animal> other) => _value.CompareTo(((GenericOrder)other)._value);
        public override string ToString() => _value.ToString();
    }

    internal static class Program
    {
        private static string Join<T>(T[] values) => values[0] + "," + values[1] + "," + values[2];

        private static void Orders<T>(string name, T low, T middle, T high)
        {
            Console.WriteLine(name + " default=" + Comparer<T>.Default.Compare(low, high));
            IComparer<T> comparer = Comparer<T>.Default;
            Console.WriteLine(name + " held=" + comparer.Compare(high, low));
            var values = new[] { high, low, middle };
            Array.Sort(values);
            Console.WriteLine(name + " array=" + Join(values));
            Console.WriteLine(name + " search=" + Array.BinarySearch(values, middle));
            values = new[] { high, low, middle };
            values.AsSpan().Sort();
            Console.WriteLine(name + " span=" + Join(values));
            var list = new List<T> { high, low, middle };
            list.Sort();
            Console.WriteLine(name + " list=" + Join(list.ToArray()) + "/" + list.BinarySearch(middle));
        }

        internal static void Run()
        {
            Console.WriteLine("== nested generic interface variance ==");
            Orders("top public", new TopPublic(1), new TopPublic(2), new TopPublic(3));
            Orders("top explicit", new TopExplicit(1), new TopExplicit(2), new TopExplicit(3));
            var low = new NestedPublic(1);
            var high = new NestedPublic(3);
            Console.WriteLine("nested direct=" + low.CompareTo(high));
            Console.WriteLine("nested covariance=" + ((object)low is Outer.IOut<Animal>));
            Console.WriteLine("nested owner distinct=" + ((object)low is OtherOuter.IOut<Animal>));
            Console.WriteLine("nested flat neighbor=" + ((object)low is Outer_IOut<long>));
            Console.WriteLine("nested escape neighbor=" + ((object)low is Outer_002BIOut<short>));
            Console.WriteLine("variance nested definition=" + typeof(Outer.IOut<Dog>).GetGenericTypeDefinition().FullName);
            Console.WriteLine("variance flat definition=" + typeof(Outer_IOut<long>).GetGenericTypeDefinition().FullName);
            Orders("nested public", low, new NestedPublic(2), high);
            var explicitLow = new NestedExplicit(1);
            var explicitHigh = new NestedExplicit(3);
            Console.WriteLine("nested explicit body=" + explicitLow.CompareTo(explicitHigh));
            IComparable<Outer.IOut<Animal>> explicitOrder = explicitLow;
            Console.WriteLine("nested explicit interface=" + explicitOrder.CompareTo(explicitHigh));
            Orders("nested explicit", explicitLow, new NestedExplicit(2), explicitHigh);
            Orders("deep", new DeepOrder(1), new DeepOrder(2), new DeepOrder(3));
            var generic = new GenericOrder(1);
            Console.WriteLine("generic covariance=" + ((object)generic is GenericOuter<string>.Middle<int>.IOut<Animal>));
            Console.WriteLine("generic enclosing invariant=" + ((object)generic is GenericOuter<object>.Middle<int>.IOut<Animal>));
            Console.WriteLine("generic middle invariant=" + ((object)generic is GenericOuter<string>.Middle<long>.IOut<Animal>));
            Orders("generic enclosing", generic, new GenericOrder(2), new GenericOrder(3));
            Console.WriteLine("nested generic interface variance end");
        }
    }
}
