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

    private static MethodInfo Factory() => typeof(Delegate).GetMethod("NewMulticastDelegate",
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

    // The invocation-list argument is CoreCLR storage, of the factory's private
    // element type; non-null storage is built through reflection.
    private static Delegate Make(Delegate receiver, Array storage, int count, bool multicastReceiver)
    {
        if (Driver.Route == 1 || Driver.Route == 3)
            return Routes.CallFactory((MulticastDelegate)receiver, storage, count, multicastReceiver);
        if (Driver.Route == 2 || Driver.Route == 4)
            return Routes.CalliFactory((MulticastDelegate)receiver, storage, count, multicastReceiver);
        return (Delegate)Factory().Invoke(receiver, new object[] { storage, count, multicastReceiver });
    }

    internal static void Run()
    {
        Console.WriteLine("== Retained multicast construction ==");
        var target = new Target();
        Action a = target.A, b = target.B, c = C;
        Action ab = a + b;
        MethodInfo factory = Factory();
        Console.WriteLine("factory-metadata:" + (factory != null && factory.IsPrivate && !factory.IsStatic
            && !factory.IsGenericMethod && factory.ReturnType == typeof(Delegate)));
        var result = (Action)Delegate.Combine(a, b, c);
        trace = "";
        result();
        var list = result.GetInvocationList();
        Console.WriteLine("factory-public-order:" + (trace == "ABC" && result.GetType() == a.GetType()
            && list.Length == 3 && ReferenceEquals(list[0], a) && ReferenceEquals(list[1], b)
            && ReferenceEquals(list[2], c) && ReferenceEquals(list[1].Target, target)));
        Func<string, string> text = Echo<string>;
        Func<object, object> obj = Echo<object>;
        var textPair = (Func<string, string>)Delegate.Combine(text, text);
        var objectPair = (Func<object, object>)Delegate.Combine(obj, obj);
        Console.WriteLine("factory-closed-type:" + (textPair("ok") == "ok"
            && textPair.GetType() == text.GetType() && objectPair.GetType() == obj.GetType()
            && !textPair.Equals(objectPair)));
        Console.WriteLine("factory-public-transitions:" + (Delegate.Remove(ab, b).Equals(a)
            && Delegate.Remove(a, a) == null && Delegate.Remove(ab, ab) == null
            && Delegate.Combine(null, a).Equals(a)));
        Console.WriteLine("Retained multicast construction end");
    }

    private static bool Refuses(Delegate receiver, Array storage, bool multicastReceiver)
    {
        try { Make(receiver, storage, 2, multicastReceiver); return false; }
        catch (Exception error)
        {
            error = (error as TargetInvocationException)?.InnerException ?? error;
            return error is PlatformNotSupportedException && error.Message.Contains("NewMulticastDelegate")
                && error.Message.Contains("Delegate.Combine");
        }
    }

    internal static void RunNativeBoundary()
    {
        Console.WriteLine("== Multicast factory refusals ==");
        Action a = C;
        Action pair = a + a;
        Array storage = Array.CreateInstance(Factory().GetParameters()[0].ParameterType.GetElementType(), 2);
        foreach (bool multicastReceiver in new[] { false, true })
        {
            Delegate receiver = multicastReceiver ? pair : a;
            Console.WriteLine("factory-refuse-" + multicastReceiver + ":" + (Refuses(receiver, null, multicastReceiver)
                && receiver.GetInvocationList().Length == (multicastReceiver ? 2 : 1)));
        }
        foreach (bool multicastReceiver in new[] { false, true })
        {
            Delegate receiver = multicastReceiver ? pair : a;
            Console.WriteLine("factory-refuse-storage-" + multicastReceiver + ":" + (Refuses(receiver, storage, multicastReceiver)
                && receiver.GetInvocationList().Length == (multicastReceiver ? 2 : 1)));
        }
        Console.WriteLine("Multicast factory refusals end");
    }
}
