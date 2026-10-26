using System;
using System.Globalization;
using System.Reflection;

namespace MulticastNamedConstructorSubset;

internal static class Program
{
    private sealed class Target
    {
        public int Old() => 23;
        public int Fresh() => 31;
        public static int Static() => 47;
        public string Wrong() => "bad";
    }
    private sealed class InitializerTarget
    {
        public static int Calls;
        static InitializerTarget() { Calls++; }
    }
    private static void Empty() { }
    private sealed class Generic<T> { public static int Read() => 7; }
    private static MethodBase Constructor(Type owner, bool stat)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        if (owner == typeof(Delegate))
            return stat ? typeof(Delegate).GetConstructor(flags, null, new[] { typeof(Type), typeof(string) }, null)
                : typeof(Delegate).GetConstructor(flags, null, new[] { typeof(object), typeof(string) }, null);
        return stat ? typeof(MulticastDelegate).GetConstructor(flags, null, new[] { typeof(Type), typeof(string) }, null)
            : typeof(MulticastDelegate).GetConstructor(flags, null, new[] { typeof(object), typeof(string) }, null);
    }
    private static void InvokeClr(Type owner, bool stat, Delegate receiver, object target, string name)
    {
        MethodBase ctor = Constructor(owner, stat);
        if (!ctor.IsFamily || ctor.IsStatic || ctor.IsGenericMethod
            || ctor.Invoke(receiver, new object[] { target, name }) != null)
            throw new InvalidOperationException("Named constructor contract changed");
    }
    // Cecil replaces only these bodies with exact direct constructor calls.
    private static void StubDelegateObject(Delegate receiver, object target, string name) => InvokeClr(typeof(Delegate), false, receiver, target, name);
    private static void StubDelegateType(Delegate receiver, Type target, string name) => InvokeClr(typeof(Delegate), true, receiver, target, name);
    private static void StubMulticastObject(MulticastDelegate receiver, object target, string name) => InvokeClr(typeof(MulticastDelegate), false, receiver, target, name);
    private static void StubMulticastType(MulticastDelegate receiver, Type target, string name) => InvokeClr(typeof(MulticastDelegate), true, receiver, target, name);
    private static void Bind(Type owner, bool stat, Func<int> receiver, object target, string name)
    {
        if (owner == typeof(Delegate))
        {
            if (stat) StubDelegateType(receiver, (Type)target, name);
            else StubDelegateObject(receiver, target, name);
        }
        else if (stat) StubMulticastType(receiver, (Type)target, name);
        else StubMulticastObject(receiver, target, name);
    }
    private static void Check(string label, bool value)
    {
        Console.WriteLine(label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    private static void Failure(string label, Action action)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (Exception error)
        {
            if (error is TargetInvocationException invocation) error = invocation.InnerException;
            Console.WriteLine(label + ":" + error.GetType().Name + ":" + (error as ArgumentException)?.ParamName);
        }
    }
    internal static void Run()
    {
        Console.WriteLine("== Named delegate construction public paths ==");
        var target = new Target();
        Func<int> instance = target.Fresh, stat = Target.Static;
        Check("named-public-instance", instance() == 31 && ReferenceEquals(instance.Target, target));
        Check("named-public-static", stat() == 47 && stat.Target == null);
        Check("named-public-multicast", (instance + stat)() == 47 && (instance + stat).GetInvocationList().Length == 2);
        Console.WriteLine("Named delegate construction public paths end");
    }
    internal static void RunContracts()
    {
        Console.WriteLine("== Retained delegate named constructors ==");
        Check("named-static-target-root", Target.Static() == 47);
        foreach (Type owner in new[] { typeof(Delegate), typeof(MulticastDelegate) })
        foreach (bool stat in new[] { false, true })
        {
            string label = "named-" + owner.Name + (stat ? "-static" : "-instance");
            var target = new Target();
            object bindTarget = stat ? (object)typeof(Target) : target;
            Func<int> receiver = target.Old;
            object alias = receiver;
            Bind(owner, stat, receiver, bindTarget, stat ? "static" : "Fresh");
            Check(label + "-bind", receiver() == (stat ? 47 : 31));
            Check(label + "-identity", ReferenceEquals(alias, receiver) && receiver.GetType() == typeof(Func<int>));
            Check(label + "-target", stat ? receiver.Target == null : ReferenceEquals(receiver.Target, target));
            Check(label + "-method", receiver.Method.Name == (stat ? "Static" : "Fresh"));
            Check(label + "-list", receiver.GetInvocationList().Length == 1 && ReferenceEquals(receiver.GetInvocationList()[0], receiver));
            if (stat)
            {
                Bind(owner, stat, receiver, bindTarget, "missing");
                Bind(owner, stat, receiver, bindTarget, "Fresh");
                Bind(owner, stat, receiver, bindTarget, "Wrong");
                Check(label + "-no-match", receiver() == 47 && receiver.Target == null);
                Failure(label + "-open", () => Bind(owner, stat, receiver, typeof(Generic<>), "Read"));
            }
            else
            {
                Failure(label + "-case", () => Bind(owner, stat, receiver, bindTarget, "fresh"));
                Failure(label + "-static-only", () => Bind(owner, stat, receiver, bindTarget, "Static"));
                Failure(label + "-signature", () => Bind(owner, stat, receiver, bindTarget, "Wrong"));
                Check(label + "-failed-unchanged", receiver() == 31);
            }
            Failure(label + "-target-null", () => Bind(owner, stat, receiver, null, null));
            Failure(label + "-method-null", () => Bind(owner, stat, receiver, bindTarget, null));
        }
        int before = InitializerTarget.Calls;
        Action initializer = Empty;
        StubDelegateType(initializer, typeof(InitializerTarget), ".cctor");
        initializer();
        Check("named-Delegate-static-initializer", InitializerTarget.Calls == before + 1);
        StubMulticastType(initializer, typeof(InitializerTarget), ".cctor");
        initializer();
        Check("named-MulticastDelegate-static-initializer", InitializerTarget.Calls == before + 2);
        Console.WriteLine("Retained delegate named constructors end");
    }
    // Cecil selects this entry point, excluding all earlier CreateDelegate sections.
    internal static void RunArtifactChecks(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "contracts") { RunContracts(); return; }
        Check("named-direct-static-target-root", Target.Static() == 47);
        var target = new Target();
        Func<int> first = target.Old, second = target.Old;
        object alias = first;
        StubDelegateObject(first, target, "Fresh");
        Check("named-direct-delegate-instance", first() == 31 && ReferenceEquals(first, alias));
        StubDelegateType(first, typeof(Target), "static");
        Check("named-direct-delegate-static", first() == 47 && first.Target == null);
        StubMulticastObject(second, target, "Fresh");
        Check("named-direct-multicast-instance", second() == 31 && second.Method.Name == "Fresh");
        StubMulticastType(second, typeof(Target), "static");
        Check("named-direct-multicast-static", second() == 47 && second.GetInvocationList().Length == 1);
        if (args.Length > 0 && args[0] == "native-boundary") RunNativeBoundary();
        Console.WriteLine("Named constructor artifact end");
    }
    internal static void RunNativeBoundary()
    {
        foreach (Type owner in new[] { typeof(Delegate), typeof(MulticastDelegate) })
        {
            var target = new Target();
            Func<int> first = target.Old, last = target.Fresh;
            Func<int> receiver = first + last;
            object alias = receiver;
            string label = "named-hybrid-" + owner.Name;
            Failure(label + "-instance", () => Bind(owner, false, receiver, target, "Fresh"));
            Failure(label + "-static", () => Bind(owner, true, receiver, typeof(Target), "static"));
            Failure(label + "-missing-instance", () => Bind(owner, false, receiver, target, "missing"));
            Bind(owner, true, receiver, typeof(Target), "missing");
            Delegate[] entries = receiver.GetInvocationList();
            Check(label + "-unchanged", ReferenceEquals(receiver, alias) && receiver() == 31 && entries.Length == 2
                && ReferenceEquals(entries[0], first) && ReferenceEquals(entries[1], last));
            Func<int> copied = new Func<int>(target.Old);
            Func<int> chain = last + copied;
            Failure(label + "-copied-instance", () => Bind(owner, false, copied, target, "Fresh"));
            Failure(label + "-copied-static", () => Bind(owner, true, copied, typeof(Target), "static"));
            Failure(label + "-copied-missing", () => Bind(owner, false, copied, target, "missing"));
            Bind(owner, true, copied, typeof(Target), "missing");
            Check(label + "-copied-unchanged", copied() == 23 && chain() == 23
                && ReferenceEquals(chain.GetInvocationList()[1], copied));
            var clone = (Func<int>)copied.Clone();
            Bind(owner, false, clone, target, "Fresh");
            Check(label + "-clone-independent", clone() == 31 && copied() == 23 && chain() == 23
                && !ReferenceEquals(clone, copied));
            var removed = (Func<int>)Delegate.Remove(chain, last);
            Failure(label + "-removed-entry", () => Bind(owner, false, removed, target, "Fresh"));
            Check(label + "-removed-unchanged", ReferenceEquals(removed, copied) && removed() == 23);
        }
    }
}
