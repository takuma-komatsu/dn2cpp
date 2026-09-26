using System;
using System.Globalization;
using System.Runtime;
using AmbiguousDefaultLib;

namespace AmbiguousDefault
{
    // Compiled against a library whose IRight overrides nothing; the output
    // directory holds the next version, where IRight and ILeft both override.
    public sealed class Both : ILeft, IRight, IBoxLeft<string>, IBoxRight<string>
    {
    }

    // Named only through typeof, so MakeGenericType synthesizes the receiver
    // from a runtime template.
    public sealed class BothOf<T> : ILeft, IRight
    {
    }

    // A constrained call on a value type runs a default body on its box.
    public struct BothValue : ILeft, IRight
    {
    }

    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            Console.WriteLine("== ambiguous default interface slots ==");
            IBase value = new Both();
            Console.WriteLine("plain: " + value.Plain());
            try
            {
                Console.WriteLine("pick: " + value.Pick(2, "two"));
            }
            catch (AmbiguousImplementationException e)
            {
                Report("pick", e);
            }
            try
            {
                Console.WriteLine("pick-generic<int>: " + value.PickGeneric<int>());
            }
            catch (AmbiguousImplementationException e)
            {
                Report("pick-generic<int>", e);
            }
            try
            {
                Console.WriteLine("pick-generic<string>: " + value.PickGeneric<string>());
            }
            catch (AmbiguousImplementationException e)
            {
                Report("pick-generic<string>", e);
            }
            IBox<string> box = new Both();
            try
            {
                Console.WriteLine("take<string>: " + box.Take("item", null));
            }
            catch (AmbiguousImplementationException e)
            {
                Report("take<string>", e);
            }
            Console.WriteLine("after: " + value.Plain());

            Console.WriteLine("== overloads whose messages match ==");
            try
            {
                Console.WriteLine("find(First.Key): " + value.Find(new First.Key()));
            }
            catch (AmbiguousImplementationException e)
            {
                Report("find(First.Key)", e);
            }
            try
            {
                Console.WriteLine("find(Second.Key): " + value.Find(new Second.Key()));
            }
            catch (AmbiguousImplementationException e)
            {
                Report("find(Second.Key)", e);
            }

            Console.WriteLine("== a MakeGenericType receiver ==");
            Type made = typeof(BothOf<>).MakeGenericType(typeof(int));
            IBase madeValue = (IBase)Activator.CreateInstance(made);
            Console.WriteLine("made plain: " + madeValue.Plain());
            try
            {
                Console.WriteLine("made pick: " + madeValue.Pick(3, "three"));
            }
            catch (AmbiguousImplementationException e)
            {
                Report("made pick", e);
            }
            try
            {
                Console.WriteLine("made pick-generic<int>: " + madeValue.PickGeneric<int>());
            }
            catch (AmbiguousImplementationException e)
            {
                Report("made pick-generic<int>", e);
            }
            Console.WriteLine("made after: " + madeValue.Plain());

            Console.WriteLine("== a constrained call on a struct ==");
            Console.WriteLine("constrained plain: " + PlainOf(new BothValue()));
            try
            {
                Console.WriteLine("constrained pick: " + PickOf(new BothValue()));
            }
            catch (AmbiguousImplementationException e)
            {
                Report("constrained pick", e);
            }
            Console.WriteLine("constrained after: " + PlainOf(new BothValue()));
        }

        private static string PlainOf<T>(T value) where T : IBase => value.Plain();

        private static string PickOf<T>(T value) where T : IBase => value.Pick(4, "four");

        private static void Report(string label, AmbiguousImplementationException e)
        {
            Console.WriteLine(label + ": " + e.GetType().FullName + ": " + e.Message);
            Console.WriteLine(label + " hresult: 0x" + e.HResult.ToString("X8"));
        }
    }
}
