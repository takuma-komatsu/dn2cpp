using System;
using System.Reflection;
using DelegateCallableBodies;

namespace MulticastNamedConstructorSubset;

internal static class Program
{
    private sealed class Target
    {
        public int Old() => 23;
        public int Fresh() => 31;
        public static int Static() => 47;
    }
    private static void Check(string label, bool value)
    {
        Console.WriteLine(label + ":" + value);
        if (!value) throw new InvalidOperationException(label);
    }
    private static Exception Failure(Type owner, Delegate receiver, object target, string name)
    {
        try { Routes.Rebind(owner, receiver, target, name); return null; }
        catch (Exception error) { return (error as TargetInvocationException)?.InnerException ?? error; }
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

    // Instance names match case-sensitively; static names ignore case, and a
    // missing static name leaves the receiver as it was.
    internal static void RunRebind()
    {
        foreach (Type owner in new[] { typeof(Delegate), typeof(MulticastDelegate) })
        {
            var target = new Target();
            Func<int> rebound = target.Old;
            object alias = rebound;
            Routes.Rebind(owner, rebound, target, "Fresh");
            int bound = rebound();
            Routes.Rebind(owner, rebound, typeof(Target), "STATIC");
            Routes.Rebind(owner, rebound, typeof(Target), "missing");
            Exception caseFailure = Failure(owner, rebound, target, "fresh");
            Check("named-rebind-" + owner.Name, bound == 31 && rebound() == 47 && rebound.Target == null
                && ReferenceEquals(alias, rebound) && caseFailure is ArgumentException);
        }
    }

    // CLR reinitialization can create hybrid multicast states. Native must refuse
    // before mutating either a list or a binding snapshotted by another list.
    internal static void RunNativeBoundary()
    {
        Console.WriteLine("== Named delegate construction refusals ==");
        foreach (Type owner in new[] { typeof(Delegate), typeof(MulticastDelegate) })
        {
            foreach (bool useType in new[] { false, true })
            {
                var target = new Target();
                object binding = useType ? typeof(Target) : target;
                string name = useType ? "Static" : "Fresh";
                Func<int> old = target.Old, fresh = target.Fresh;
                Func<int> list = old + fresh;
                Exception multicast = Failure(owner, list, binding, name);
                Check("named-multicast-refuse-" + owner.Name + "-" + useType,
                    multicast is PlatformNotSupportedException && multicast.Message.Contains("delegate")
                    && list() == 31 && list.GetInvocationList().Length == 2);
                Func<int> copied = target.Old;
                Func<int> snapshot = fresh + copied;
                Exception snapshotted = Failure(owner, copied, binding, name);
                Check("named-snapshot-refuse-" + owner.Name + "-" + useType,
                    snapshotted is PlatformNotSupportedException && snapshotted.Message.Contains("delegate")
                    && copied() == 23 && snapshot() == 23 && ReferenceEquals(copied.Target, target));
            }
        }
        Console.WriteLine("Named delegate construction refusals end");
    }
}
