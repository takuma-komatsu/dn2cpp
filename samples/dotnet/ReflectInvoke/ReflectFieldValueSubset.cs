#nullable enable
using System;
using System.Reflection;

namespace ReflectFieldValueSubset
{
    enum Mood { Calm, Bold }

    struct Point
    {
        public int X;
        public int Y;
        public override string ToString() => $"({X},{Y})";
    }

    class Bag
    {
        public int Count = 10;
        public string Label = "init";
        public double Ratio;
        public Mood Mood;
        public Point Where;
        public static int Total;
    }

    static class Program
    {
        static FieldInfo F(string name) => typeof(Bag).GetField(name)!;internal static void Run()
        {
            Bag bag = new Bag();

            // GetValue reads (boxing value types); reference fields pass through.
            Console.WriteLine("== get ==");
            Console.WriteLine(F("Count").GetValue(bag));   // 10
            Console.WriteLine(F("Label").GetValue(bag));   // init

            // SetValue writes (unboxing value types), observable on the instance.
            Console.WriteLine("== set instance ==");
            F("Count").SetValue(bag, 42);
            Console.WriteLine(bag.Count);                  // 42
            Console.WriteLine(F("Count").GetValue(bag));   // 42
            F("Label").SetValue(bag, "hello");
            Console.WriteLine(bag.Label);                  // hello
            F("Ratio").SetValue(bag, 3.5);
            Console.WriteLine(F("Ratio").GetValue(bag));   // 3.5
            // The abstract 5-arg overload (BindingFlags/Binder/CultureInfo)
            // stores like the 2-arg form with the default binder.
            F("Count").SetValue(bag, 43, System.Reflection.BindingFlags.Default, null, null);
            Console.WriteLine(F("Count").GetValue(bag));   // 43

            // Enum and struct value-type fields round-trip through boxing.
            Console.WriteLine("== value types ==");
            F("Mood").SetValue(bag, Mood.Bold);
            Console.WriteLine(F("Mood").GetValue(bag));    // Bold
            F("Where").SetValue(bag, new Point { X = 3, Y = 4 });
            Console.WriteLine(F("Where").GetValue(bag));   // (3,4)
            Console.WriteLine(bag.Where);                  // (3,4)

            // Static field: obj is null.
            Console.WriteLine("== static ==");
            F("Total").SetValue(null, 7);
            Console.WriteLine(Bag.Total);                  // 7
            Console.WriteLine(F("Total").GetValue(null));  // 7

            // The boxed value can be unboxed back to its CLR type.
            Console.WriteLine("== unbox ==");
            int c = (int)F("Count").GetValue(bag)!;        // 42
            Console.WriteLine(c + 1);                       // 43
        }
    }
}
