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
        private static void Main(string[] args)
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

            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_ORDINARY_CONSTRAINED_DEFAULT") == "1")
                return;
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
            if (args.Length != 0 && args[0] == "before-binding")
                return;
            RunBindings(value, madeValue);
            if (args.Length != 0 && args[0] == "before-generic-messages")
                return;
            RunGenericMessages();
        }

        private class VirtualGroups
        {
            public virtual string Choose<T>() => typeof(T).Name;
        }

        private static void RunBindings(IBase value, IBase madeValue)
        {
            Console.WriteLine("== virtual delegate creation ==");
            ObserveBinding("unused group", () => (Func<string>)value.Unused);
            ObserveBinding("generic<int> group", () => (Func<string>)value.UnusedGeneric<int>);
            ObserveBinding("generic<string> group", () => (Func<string>)value.UnusedGeneric<string>);
            ObserveBinding("generic receiver group", () => GroupOf(new Both()));
            ObserveBinding("made unused group", () => (Func<string>)madeValue.Unused);
            ObserveBinding("made generic group", () => (Func<string>)madeValue.UnusedGeneric<int>);
            IBase absent = null;
            ObserveBinding("null interface group", () => (Func<string>)absent.Unused);
            ObserveBinding("null generic interface group", () => (Func<string>)absent.UnusedGeneric<int>);
            VirtualGroups absentClass = null;
            ObserveBinding("null generic class group", () => (Func<string>)absentClass.Choose<int>);
            Func<string> plain = value.Plain;
            Func<string> madePlain = madeValue.Plain;
            Func<string> classGroup = new VirtualGroups().Choose<int>;
            Console.WriteLine("callable groups: " + plain() + "," + madePlain() + "," + classGroup());
        }

        private static Func<string> GroupOf<T>(T value) where T : class, IBase => value.Unused;

        private static void ObserveBinding(string label, Func<Delegate> bind)
        {
            bool created = false;
            try
            {
                Delegate group = bind();
                created = true;
                GC.KeepAlive(group);
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().FullName + ": " + e.Message);
                Console.WriteLine(label + " hresult: 0x" + e.HResult.ToString("X8"));
            }
            Console.WriteLine(label + " created: " + created);
        }

        private sealed class BoxBoth<T> : IBoxLeft<T>, IBoxRight<T>
        {
        }

        private sealed class DuoBoth<TFirst, TSecond> : IDuoLeft<TFirst, TSecond>, IDuoRight<TFirst, TSecond>
        {
        }

        private static void RunGenericMessages()
        {
            Console.WriteLine("== generic interface ambiguity messages ==");
            IBox<string> reference = new BoxBoth<string>();
            IBox<int> number = new BoxBoth<int>();
            IDuo<string, int> mixed = new DuoBoth<string, int>();
            IDuo<int, int> numbers = new DuoBoth<int, int>();
            ObserveCall("box ref/value", () => reference.Select("item", 5));
            ObserveCall("box value/ref", () => number.Select(4, "four"));
            ObserveCall("box pair", () => reference.Pair<int, string>());
            ObserveCall("duo mixed single", () => mixed.Single("item", 5, true));
            ObserveCall("duo values single", () => numbers.Single(4, 5, "item"));
            ObserveCall("duo pair", () => mixed.Pair<int, string>());
            ObserveCall("duo triple", () => mixed.Triple<string, int, bool>());
            ObserveBinding("box pair group", () => (Func<string>)reference.Pair<int, string>);
            ObserveBinding("duo single group", () => (Func<int, int, string, string>)numbers.Single<string>);
        }

        private static void ObserveCall(string label, Func<string> call)
        {
            try
            {
                Console.WriteLine(label + " returned: " + call());
            }
            catch (AmbiguousImplementationException e)
            {
                Report(label, e);
            }
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
