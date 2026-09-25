// A class virtual dispatched through a base whose slot a subclass hides.
// `new virtual` opens a fresh slot: an override below the hider overrides the
// hider's slot, so a base-typed call still runs the base slot's most derived
// body. A generic base's vtable is built per specialization, shared bodies
// included, so the chain is repeated over one.
using System;

namespace VirtualHiderSubset
{
    internal class Animal
    {
        public virtual string Speak() => "animal";
    }

    internal class Dog : Animal
    {
        public override string Speak() => "dog";
    }

    internal class Puppy : Dog
    {
        public new virtual string Speak() => "puppy";
    }

    internal sealed class LoudPuppy : Puppy
    {
        public override string Speak() => "loud-puppy";
    }

    internal class Cell<T>
    {
        public virtual string Name() => "cell:" + typeof(T).Name;
    }

    internal class HiddenCell<T> : Cell<T>
    {
        public new virtual string Name() => "hidden:" + typeof(T).Name;
    }

    internal sealed class LeafCell<T> : HiddenCell<T>
    {
        public override string Name() => "leaf:" + typeof(T).Name;
    }

    internal static class Program
    {
        private static string AsAnimal(Animal animal) => animal.Speak();

        private static string AsDog(Dog dog) => dog.Speak();

        private static string AsPuppy(Puppy puppy) => puppy.Speak();

        private static string AsCell<T>(Cell<T> cell) => cell.Name();

        private static string AsHidden<T>(HiddenCell<T> cell) => cell.Name();

        internal static void Run()
        {
            var loud = new LoudPuppy();
            Console.WriteLine("virtual hider base=" + AsAnimal(loud));
            Console.WriteLine("virtual hider mid=" + AsDog(loud));
            Console.WriteLine("virtual hider hider=" + AsPuppy(loud));
            Console.WriteLine("virtual hider plain=" + AsAnimal(new Puppy()) + "/" + AsPuppy(new Puppy()));
            var number = new LeafCell<int>();
            Console.WriteLine("virtual hider value base=" + AsCell(number));
            Console.WriteLine("virtual hider value hider=" + AsHidden(number));
            var text = new LeafCell<string>();
            Console.WriteLine("virtual hider shared base=" + AsCell(text));
            Console.WriteLine("virtual hider shared hider=" + AsHidden(text));
        }
    }
}
