using System;
using System.Reflection;

namespace MulticastOperationsSubset;

internal static class Program
{
    private static string trace = "";
    private delegate void Other();
    private sealed class Target
    {
        internal void A() => trace += "A";
        internal void B() => trace += "B";
    }
    private static void C() => trace += "C";
    private static T Echo<T>(T value) => value;

    private static Delegate Combine(Delegate source, Delegate value)
    {
        MethodInfo method = typeof(MulticastDelegate).GetMethod("CombineImpl",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Delegate) }, null);
        return (Delegate)method.Invoke(source, new object[] { value });
    }
    private static Delegate Remove(Delegate source, Delegate value)
    {
        MethodInfo method = typeof(MulticastDelegate).GetMethod("RemoveImpl",
            BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(Delegate) }, null);
        return (Delegate)method.Invoke(source, new object[] { value });
    }
    private static bool CombineMismatch(Delegate a, Delegate b)
    {
        try { Combine(a, b); return false; }
        catch (TargetInvocationException ex) { return ex.InnerException is ArgumentException; }
    }

    internal static void Run()
    {
        Console.WriteLine("== Retained multicast operations ==");
        var target = new Target();
        Action a1 = new Action(target.A), a2 = new Action(target.A), b = target.B, c = C;
        Action pair = a1 + b;
        var combined = (Action)Combine(pair, a2 + c);
        trace = "";
        combined();
        Console.WriteLine("multicast-combine-order:" + (trace == "ABAC" && combined.Equals(pair + a2 + c)));
        Console.WriteLine("multicast-combine-identity:" + (!ReferenceEquals(combined, pair)
            && combined.GetType() == pair.GetType() && ReferenceEquals(combined.GetInvocationList()[0], a1)
            && ReferenceEquals(combined.GetInvocationList()[2], a2)));
        Console.WriteLine("multicast-null-noop:" + (ReferenceEquals(Combine(pair, null), pair)
            && ReferenceEquals(Remove(pair, null), pair)));
        Console.WriteLine("multicast-no-match:" + ReferenceEquals(Remove(pair, c), pair));
        Action source = a1 + b + a2 + b + c;
        var removed = (Action)Remove(source, a1 + b);
        trace = "";
        removed();
        Console.WriteLine("multicast-remove-last:" + (!ReferenceEquals(a1, a2) && a1.Equals(a2)
            && trace == "ABC" && removed.Equals(a1 + b + c)
            && ReferenceEquals(removed.GetInvocationList()[0], a1)));
        Console.WriteLine("multicast-source-immutable:" + (source.GetInvocationList().Length == 5
            && ReferenceEquals(source.GetInvocationList()[2], a2)));
        Console.WriteLine("multicast-remove-transitions:" + (ReferenceEquals(Remove(pair, b), a1)
            && Remove(a1, a2) == null && Remove(pair, pair) == null));
        Console.WriteLine("multicast-remove-longer:" + ReferenceEquals(Remove(pair, source), pair));
        Other other = C;
        Console.WriteLine("multicast-protected-mismatch:" + (CombineMismatch(pair, other)
            && ReferenceEquals(Remove(pair, other), pair)));
        bool publicMismatch = false;
        try { Delegate.Remove(pair, other); }
        catch (ArgumentException) { publicMismatch = true; }
        Console.WriteLine("multicast-public-mismatch:" + publicMismatch);
        Func<string, string> text = Echo<string>;
        Func<object, object> obj = Echo<object>;
        var genericPair = (Func<string, string>)Combine(text, text);
        Console.WriteLine("multicast-closed-types:" + (genericPair("ok") == "ok"
            && CombineMismatch(text, obj) && ReferenceEquals(Remove(text, obj), text)
            && ReferenceEquals(Remove(genericPair, text), text)));
        Console.WriteLine("Retained multicast operations end");
    }
}
