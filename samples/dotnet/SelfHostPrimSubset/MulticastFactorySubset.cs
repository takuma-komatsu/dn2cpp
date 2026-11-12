using System;
using System.Reflection;
using DelegateCallableBodies;
using Driver = DelegateCallableBodies.Program;

namespace MulticastFactorySubset;

internal static class Program
{
    private static string trace = "";
    private sealed class Target
    {
        internal void A() => trace += "A";
        internal void B() => trace += "B";
    }
    private static void C() => trace += "C";
    private static T Echo<T>(T value) => value;

    private static Delegate Make(Delegate receiver, object[] entries, int count, bool? stubFlag)
    {
        if (Driver.Route == 1 || Driver.Route == 3)
            return stubFlag.HasValue ? Routes.CallFactoryFlag((MulticastDelegate)receiver, entries, count, stubFlag.Value)
                : Routes.CallFactory((MulticastDelegate)receiver, entries, count);
        if (Driver.Route == 2 || Driver.Route == 4)
            return stubFlag.HasValue ? Routes.CalliFactoryFlag((MulticastDelegate)receiver, entries, count, stubFlag.Value)
                : Routes.CalliFactory((MulticastDelegate)receiver, entries, count);
        if (stubFlag.HasValue)
        {
            MethodInfo factory = typeof(MulticastDelegate).GetMethod("NewMulticastDelegate",
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(object[]), typeof(int), typeof(bool) }, null);
            return (Delegate)factory.Invoke(receiver, new object[] { entries, count, stubFlag.Value });
        }
        MethodInfo shortFactory = typeof(MulticastDelegate).GetMethod("NewMulticastDelegate",
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new[] { typeof(object[]), typeof(int) }, null);
        return (Delegate)shortFactory.Invoke(receiver, new object[] { entries, count });
    }

    internal static void Run()
    {
        Console.WriteLine("== Retained multicast construction ==");
        var target = new Target();
        Action a = target.A, b = target.B, c = C;
        Action ab = a + b;
        foreach (int shape in new[] { 0, 1, 2 })
        {
            bool? flag = shape == 0 ? null : shape == 2;
            Delegate receiver = shape == 2 ? ab : a;
            object[] entries = { a, b, c, null };
            var result = (Action)Make(receiver, entries, 3, flag);
            trace = "";
            result();
            Console.WriteLine("factory-order-" + shape + ":" + (trace == "ABC"));
            Console.WriteLine("factory-identity-" + shape + ":" + (!ReferenceEquals(result, receiver)
                && result.GetType() == receiver.GetType() && result.Equals(a + b + c)));
            var list = result.GetInvocationList();
            Console.WriteLine("factory-entries-" + shape + ":" + (list.Length == 3
                && ReferenceEquals(list[0], a) && ReferenceEquals(list[1], b)
                && ReferenceEquals(list[2], c) && ReferenceEquals(list[1].Target, target)));
            entries[2] = new object();
            entries[3] = a + b;
            var prefix = (Action)Make(receiver, entries, 2, flag);
            trace = "";
            prefix();
            Console.WriteLine("factory-prefix-" + shape + ":" + (trace == "AB"
                && prefix.GetInvocationList().Length == 2 && prefix.Equals(ab)
                && !ReferenceEquals(prefix, ab)));
        }
        Func<string, string> text = Echo<string>;
        Func<object, object> obj = Echo<object>;
        var textPair = (Func<string, string>)Make(text, new object[] { text, text }, 2, false);
        var objectPair = (Func<object, object>)Make(obj, new object[] { obj, obj }, 2, null);
        Console.WriteLine("factory-closed-type:" + (textPair("ok") == "ok"
            && textPair.GetType() == text.GetType() && objectPair.GetType() == obj.GetType()
            && !textPair.Equals(objectPair)));
        Console.WriteLine("factory-public-transitions:" + (Delegate.Remove(ab, b).Equals(a)
            && Delegate.Remove(a, a) == null && Delegate.Remove(ab, ab) == null
            && Delegate.Combine(null, a).Equals(a)));
        Console.WriteLine("Retained multicast construction end");
    }

    private static bool Refuses(Delegate receiver, object[] entries, int count, bool? flag)
    {
        try { Make(receiver, entries, count, flag); return false; }
        catch (Exception error)
        {
            error = (error as TargetInvocationException)?.InnerException ?? error;
            string reason = count < 2 ? "Empty or single-entry private multicast state"
                : entries is null || count > entries.Length ? "in-bounds invocation prefix" : "flattened entries";
            return error is PlatformNotSupportedException && error.Message.Contains(reason);
        }
    }

    internal static void RunNativeBoundary()
    {
        Console.WriteLine("== Multicast factory refusals ==");
        Action a = C;
        Func<string, string> other = Echo<string>;
        foreach (int shape in new[] { 0, 1, 2 })
        {
            bool? flag = shape == 0 ? null : shape == 2;
            Console.WriteLine("factory-refuse-null-" + shape + ":" + Refuses(a, null, 2, flag));
            Console.WriteLine("factory-refuse-empty-" + shape + ":" + Refuses(a, Array.Empty<object>(), 0, flag));
            Console.WriteLine("factory-refuse-single-" + shape + ":" + Refuses(a, new object[] { a }, 1, flag));
            Console.WriteLine("factory-refuse-negative-" + shape + ":" + Refuses(a, new object[] { a, a }, -1, flag));
            Console.WriteLine("factory-refuse-bounds-" + shape + ":" + Refuses(a, new object[] { a, a }, 3, flag));
            Console.WriteLine("factory-refuse-null-entry-" + shape + ":" + Refuses(a, new object[] { a, null }, 2, flag));
            Console.WriteLine("factory-refuse-object-" + shape + ":" + Refuses(a, new object[] { a, new object() }, 2, flag));
            Console.WriteLine("factory-refuse-type-" + shape + ":" + Refuses(a, new object[] { a, other }, 2, flag));
            Console.WriteLine("factory-refuse-nested-" + shape + ":" + Refuses(a, new object[] { a, a + a }, 2, flag));
        }
        Console.WriteLine("Multicast factory refusals end");
    }
}
