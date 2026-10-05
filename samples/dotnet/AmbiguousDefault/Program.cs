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
            if (args.Length != 0 && args[0] == "before-static-calls")
                return;
            RunStaticCalls();
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

        private sealed class StaticBoth : IStaticLeft, IStaticRight
        {
        }

        private struct StaticBothValue : IStaticLeft, IStaticRight
        {
        }

        private sealed class StaticBothOf<T> : IStaticLeft, IStaticRight
        {
        }

        private sealed class StaticLeftOnly : IStaticLeft
        {
        }

        private sealed class StaticResolved : IStaticResolved
        {
        }

        private sealed class StaticClassOverride : IStaticLeft, IStaticRight
        {
            public static string Default() => "class";
            public static string DefaultGeneric<U>() => "class";
            public static string Abstract() => "class";
            public static string AbstractGeneric<U>() => "class";
        }

        private sealed class StaticDefaultOnly : IStaticBase
        {
            public static string Abstract() => "class";
            public static string AbstractGeneric<U>() => "class";
        }

        private static int staticEntries;

        private static string StaticDefaultOf<T>(bool call) where T : IStaticBase
        {
            staticEntries++;
            return call ? T.Default() : "skipped";
        }

        private static string StaticGenericOf<T, U>(bool call) where T : IStaticBase
        {
            staticEntries++;
            return call ? T.DefaultGeneric<U>() : "skipped";
        }

        private static string StaticAbstractOf<T>(bool call) where T : IStaticBase
        {
            staticEntries++;
            return call ? T.Abstract() : "skipped";
        }

        private static string StaticAbstractGenericOf<T, U>(bool call) where T : IStaticBase
        {
            staticEntries++;
            return call ? T.AbstractGeneric<U>() : "skipped";
        }

        private static string ConcreteDefault(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static string ConcreteDefaultGeneric(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static string ConcreteAbstract(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static string ConcreteAbstractGeneric(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static string ConcreteValueDefault(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static string ConcreteValueDefaultGeneric(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static string ConcreteValueAbstract(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static string ConcreteValueAbstractGeneric(bool call)
        {
            staticEntries++;
            return call ? "rewritten" : "skipped";
        }

        private static void RunStaticCalls()
        {
            Console.WriteLine("== constrained static interface calls ==");
            ObserveStaticCall("static default", () => StaticDefaultOf<StaticBoth>(true));
            ObserveStaticCall("static default skipped", () => StaticDefaultOf<StaticBoth>(false));
            ObserveStaticCall("static generic", () => StaticGenericOf<StaticBoth, int>(true));
            ObserveStaticCall("static generic skipped", () => StaticGenericOf<StaticBoth, string>(false));
            ObserveStaticCall("static abstract", () => StaticAbstractOf<StaticBoth>(true));
            ObserveStaticCall("static abstract skipped", () => StaticAbstractOf<StaticBoth>(false));
            ObserveStaticCall("static generic abstract", () => StaticAbstractGenericOf<StaticBoth, int>(true));
            ObserveStaticCall("static generic ref", () => StaticGenericOf<StaticBoth, string>(true));
            ObserveStaticCall("static generic receiver", () => StaticDefaultOf<StaticBothOf<string>>(true));
            ObserveStaticCall("static generic owner ref", () => StaticBoxOf<string, BoxBoth<string>, int>());
            ObserveStaticCall("static generic owner value", () => StaticBoxAbstractOf<int, BoxBoth<int>, string>());
            ObserveStaticCall("value default", () => StaticDefaultOf<StaticBothValue>(true));
            ObserveStaticCall("value default skipped", () => StaticDefaultOf<StaticBothValue>(false));
            ObserveStaticCall("value generic", () => StaticGenericOf<StaticBothValue, int>(true));
            ObserveStaticCall("value generic skipped", () => StaticGenericOf<StaticBothValue, string>(false));
            ObserveStaticCall("value abstract", () => StaticAbstractOf<StaticBothValue>(true));
            ObserveStaticCall("value abstract skipped", () => StaticAbstractOf<StaticBothValue>(false));
            ObserveStaticCall("value generic abstract", () => StaticAbstractGenericOf<StaticBothValue, int>(true));
            ObserveStaticCall("concrete default", () => ConcreteDefault(true));
            ObserveStaticCall("concrete default skipped", () => ConcreteDefault(false));
            ObserveStaticCall("concrete generic", () => ConcreteDefaultGeneric(true));
            ObserveStaticCall("concrete generic skipped", () => ConcreteDefaultGeneric(false));
            ObserveStaticCall("concrete abstract", () => ConcreteAbstract(true));
            ObserveStaticCall("concrete abstract skipped", () => ConcreteAbstract(false));
            ObserveStaticCall("concrete generic abstract", () => ConcreteAbstractGeneric(true));
            ObserveStaticCall("concrete value default", () => ConcreteValueDefault(true));
            ObserveStaticCall("concrete value default skipped", () => ConcreteValueDefault(false));
            ObserveStaticCall("concrete value generic", () => ConcreteValueDefaultGeneric(true));
            ObserveStaticCall("concrete value generic skipped", () => ConcreteValueDefaultGeneric(false));
            ObserveStaticCall("concrete value abstract", () => ConcreteValueAbstract(true));
            ObserveStaticCall("concrete value abstract skipped", () => ConcreteValueAbstract(false));
            ObserveStaticCall("concrete value generic abstract", () => ConcreteValueAbstractGeneric(true));
            staticEntries = 0;
            Func<bool, string> deferred = StaticDefaultOf<StaticBoth>;
            Console.WriteLine("static group created entries: " + staticEntries);
            ObserveStaticCall("static group invoked", () => deferred(true));
            Console.WriteLine("static left bodies: " + StaticBodies<StaticLeftOnly>());
            Console.WriteLine("static specific bodies: " + StaticBodies<StaticResolved>());
            Console.WriteLine("static class bodies: " + StaticBodies<StaticClassOverride>());
            Console.WriteLine("static fallback bodies: " + StaticBodies<StaticDefaultOnly>());
            Console.WriteLine("static interface calls end");
        }

        private static string StaticBoxOf<TItem, T, U>() where T : IBox<TItem>
        {
            staticEntries++;
            return T.StaticSelect<U>();
        }

        private static string StaticBoxAbstractOf<TItem, T, U>() where T : IBox<TItem>
        {
            staticEntries++;
            return T.StaticAbstract<U>();
        }

        private static string StaticBodies<T>() where T : IStaticBase =>
            T.Default() + "," + T.DefaultGeneric<int>() + "," + T.Abstract() + "," + T.AbstractGeneric<string>();

        private static void ObserveStaticCall(string label, Func<string> call)
        {
            staticEntries = 0;
            try
            {
                Console.WriteLine(label + " returned: " + call());
            }
            catch (Exception e)
            {
                Console.WriteLine(label + ": " + e.GetType().FullName + ": " + e.Message);
                Console.WriteLine(label + " hresult: 0x" + e.HResult.ToString("X8"));
            }
            Console.WriteLine(label + " entries: " + staticEntries);
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
