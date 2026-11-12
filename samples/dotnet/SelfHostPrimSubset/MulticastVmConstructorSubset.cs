using System;
using System.Reflection;
using DelegateCallableBodies;

namespace MulticastVmConstructorSubset;

internal static class Program
{
    private struct Number { public int Read() => 17; }
    private sealed class Target { public int Read() => 23; }
    private static int Static() => 41;
    private static T Echo<T>(T value) => value;

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

    internal static void RunNativeBoundary()
    {
        Console.WriteLine("== CoreCLR delegate constructor refusals ==");
        var target = new Target();
        Func<int> receiver = target.Read;
        Func<int> unchanged = target.Read;
        foreach (MethodInfo method in Methods())
        {
            bool refused = false;
            try { Routes.VmConstructor(method, receiver); }
            catch (Exception error)
            {
                error = (error as TargetInvocationException)?.InnerException ?? error;
                refused = error is PlatformNotSupportedException && error.Message.Contains(method.Name)
                    && error.Message.Contains("Delegate.CreateDelegate");
            }
            Console.WriteLine("vm-ctor-refuse-" + method.Name + ":" + (refused && receiver() == 23
                && ReferenceEquals(receiver.Target, target) && receiver.Equals(unchanged)));
        }
        Console.WriteLine("CoreCLR delegate constructor refusals end");
    }
}
