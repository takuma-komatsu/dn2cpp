using System;
using System.Reflection;

namespace MulticastVmConstructorSubset;

internal static class Program
{
    private struct Number { public int Read() => 17; }
    private sealed class Target { public int Read() => 23; }
    private static int Static() => 41;
    private static T Echo<T>(T value) => value;
    // Rewritten only in the native artifact copy; never execute VM stubs on CLR.
    private static void StubCtorClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("VM fixture patch missing");
    private static void StubCtorClosedStatic(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("VM fixture patch missing");
    private static void StubCtorRTClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("VM fixture patch missing");
    private static void StubCtorOpened(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("VM fixture patch missing");
    private static void StubCtorVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("VM fixture patch missing");
    private static void StubCtorCollectibleClosedStatic(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("VM fixture patch missing");
    private static void StubCtorCollectibleOpened(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("VM fixture patch missing");
    private static void StubCtorCollectibleVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("VM fixture patch missing");

    private static MethodInfo[] Methods() => new[]
    {
        typeof(MulticastDelegate).GetMethod("CtorClosed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr) }, null),
        typeof(MulticastDelegate).GetMethod("CtorClosedStatic", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr) }, null),
        typeof(MulticastDelegate).GetMethod("CtorRTClosed", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr) }, null),
        typeof(MulticastDelegate).GetMethod("CtorOpened", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null),
        typeof(MulticastDelegate).GetMethod("CtorVirtualDispatch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null),
        typeof(MulticastDelegate).GetMethod("CtorCollectibleClosedStatic", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null),
        typeof(MulticastDelegate).GetMethod("CtorCollectibleOpened", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) }, null),
        typeof(MulticastDelegate).GetMethod("CtorCollectibleVirtualDispatch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly, null, new[] { typeof(object), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) }, null),
    };

    internal static void Run()
    {
        Console.WriteLine("== CoreCLR delegate construction boundary ==");
        bool shape = true;
        MethodInfo[] methods = Methods();
        foreach (MethodInfo method in methods)
        {
            shape &= method != null && method.IsPrivate && !method.IsStatic && !method.IsGenericMethod
                && method.ReturnType == typeof(void);
        }
        Console.WriteLine("vm-ctor-metadata:" + (shape && methods.Length == 8));
        var number = new Number();
        var target = new Target();
        Func<int> value = new Func<int>(number.Read), instance = new Func<int>(target.Read), stat = new Func<int>(Static);
        Func<string, string> generic = new Func<string, string>(Echo<string>);
        var rebound = (Func<int>)typeof(Target).GetMethod("Read").CreateDelegate(typeof(Func<int>), target);
        Console.WriteLine("vm-ctor-public-value:" + (value() == 17));
        Console.WriteLine("vm-ctor-public-instance:" + (instance() == 23));
        Console.WriteLine("vm-ctor-public-static:" + (stat() == 41));
        Console.WriteLine("vm-ctor-public-generic:" + (generic("ok") == "ok"));
        Console.WriteLine("vm-ctor-public-bind:" + (rebound() == 23));
        Console.WriteLine("CoreCLR delegate construction boundary end");
    }

    private static void Refuse(string label, Action action, Func<int> receiver)
    {
        try { action(); Console.WriteLine(label + ":accepted"); }
        catch (Exception error)
        {
            if (error is TargetInvocationException invocation) error = invocation.InnerException;
            Console.WriteLine(label + ":" + error.GetType().Name + ":" + error.Message);
        }
        if (receiver() != 23) throw new InvalidOperationException("VM refusal changed the receiver");
    }

    internal static unsafe void RunNativeBoundary()
    {
        var target = new Target();
        Func<int> receiver = target.Read;
        Refuse("vm-CtorClosed-direct", () => StubCtorClosed(receiver, target, IntPtr.Zero), receiver);
        Refuse("vm-CtorClosedStatic-direct", () => StubCtorClosedStatic(receiver, target, IntPtr.Zero), receiver);
        Refuse("vm-CtorRTClosed-direct", () => StubCtorRTClosed(receiver, target, IntPtr.Zero), receiver);
        Refuse("vm-CtorOpened-direct", () => StubCtorOpened(receiver, target, IntPtr.Zero, IntPtr.Zero), receiver);
        Refuse("vm-CtorVirtualDispatch-direct", () => StubCtorVirtualDispatch(receiver, target, IntPtr.Zero, IntPtr.Zero), receiver);
        Refuse("vm-CtorCollectibleClosedStatic-direct", () => StubCtorCollectibleClosedStatic(receiver, target, IntPtr.Zero, IntPtr.Zero), receiver);
        Refuse("vm-CtorCollectibleOpened-direct", () => StubCtorCollectibleOpened(receiver, target, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero), receiver);
        Refuse("vm-CtorCollectibleVirtualDispatch-direct", () => StubCtorCollectibleVirtualDispatch(receiver, target, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero), receiver);
        delegate* managed<MulticastDelegate, object, IntPtr, void> pointer = &StubCtorRTClosed;
        try { pointer(receiver, target, IntPtr.Zero); Console.WriteLine("vm-pointer:accepted"); }
        catch (Exception error) { Console.WriteLine("vm-pointer:" + error.GetType().Name + ":" + error.Message); }
        if (receiver() != 23) throw new InvalidOperationException("VM pointer refusal changed the receiver");
        MethodInfo[] methods = Methods();
        foreach (MethodInfo method in methods)
        {
            int count = method.GetParameters().Length;
            object[] arguments = new object[count];
            arguments[0] = target;
            for (int i = 1; i < count; i++) arguments[i] = IntPtr.Zero;
            Refuse("vm-" + method.Name + "-reflection", () => method.Invoke(receiver, arguments), receiver);
        }
        var bound = (Action<object, IntPtr>)methods[2].CreateDelegate(typeof(Action<object, IntPtr>), receiver);
        Refuse("vm-bound", () => bound(target, IntPtr.Zero), receiver);
        Console.WriteLine("vm-ctor-receiver-unchanged:" + (receiver() == 23 && ReferenceEquals(receiver.Target, target)
            && receiver.GetInvocationList().Length == 1));
        Console.WriteLine("CoreCLR delegate constructor refusal end");
    }
}
