using System;
using System.Reflection;

namespace MulticastAccessorSubset;

internal static class Program
{
    private static string trace = "";
    private sealed class Target
    {
        internal void A() => trace += "A";
        internal void B() => trace += "B";
    }
    private sealed class Box<T> { internal T Echo(T value) => value; }
    private static void C() => trace += "C";
    private static T Echo<T>(T value) => value;

    private static object TargetOf(Delegate value)
    {
        MethodInfo method = typeof(MulticastDelegate).GetMethod("GetTarget",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null);
        return method.Invoke(value, null);
    }
    private static MethodInfo MethodOf(Delegate value)
    {
        MethodInfo method = typeof(MulticastDelegate).GetMethod("GetMethodImpl",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null);
        return (MethodInfo)method.Invoke(value, null);
    }
    private static Delegate[] ListOf(Delegate value)
    {
        MethodInfo method = typeof(MulticastDelegate).GetMethod("GetInvocationList",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null);
        return (Delegate[])method.Invoke(value, null);
    }
    private static bool Single(Delegate value)
    {
        MethodInfo method = typeof(MulticastDelegate).GetMethod("get_HasSingleTarget",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null);
        return (bool)method.Invoke(value, null);
    }
    private static object At(Delegate value, int index)
    {
        MethodInfo method = typeof(MulticastDelegate).GetMethod("TryGetAt",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            null, new[] { typeof(int) }, null);
        return method.Invoke(value, new object[] { index });
    }

    internal static void Run()
    {
        Console.WriteLine("== Retained multicast accessors ==");
        var target = new Target();
        Action a = target.A, b = target.B, c = C;
        Action abc = a + b + c;
        Console.WriteLine("accessor-target:" + (ReferenceEquals(TargetOf(a), target) && TargetOf(c) == null
            && TargetOf(abc) == null && ReferenceEquals(TargetOf(c + a), target)));
        // No public Delegate.Method or explicit lookup of A/C: this retained read
        // alone must keep the selected declaration's method row.
        MethodInfo am = MethodOf(a), cm = MethodOf(c);
        Console.WriteLine("accessor-method:" + (am != null && cm != null && am.Name == "A" && cm.Name == "C"
            && am.DeclaringType == typeof(Target) && cm.DeclaringType == typeof(Program)
            && am.Equals(MethodOf(c + a)) && cm.Equals(MethodOf(abc))
            && am.MetadataToken == MethodOf(a).MetadataToken));
        var bound = (Action)am.CreateDelegate(typeof(Action), target);
        Console.WriteLine("accessor-bound:" + (ReferenceEquals(TargetOf(bound), target) && MethodOf(bound).Equals(am)));
        var first = ListOf(abc);
        var second = ListOf(abc);
        Console.WriteLine("accessor-list:" + (first.GetType() == typeof(Delegate[]) && first.Length == 3
            && !ReferenceEquals(first, second) && ReferenceEquals(first[0], a)
            && ReferenceEquals(first[1], b) && ReferenceEquals(first[2], c)));
        first[0] = c;
        Console.WriteLine("accessor-list-isolated:" + (ReferenceEquals(ListOf(abc)[0], a)
            && ReferenceEquals(ListOf(a)[0], a) && ListOf(a).Length == 1));
        Console.WriteLine("accessor-single:" + (Single(a) && Single(c) && !Single(abc) && !Single(a + a)));
        Console.WriteLine("accessor-index:" + (ReferenceEquals(At(a, 0), a) && At(a, 1) == null
            && ReferenceEquals(At(abc, 0), a) && ReferenceEquals(At(abc, 2), c)
            && At(abc, 3) == null && At(abc, -1) == null
            && At(abc, int.MinValue) == null && At(abc, int.MaxValue) == null));
        var box = new Box<string>();
        Func<string, string> closedOwner = box.Echo, genericMethod = Echo<string>;
        MethodInfo ownerMethod = MethodOf(closedOwner), generic = MethodOf(genericMethod);
        Console.WriteLine("accessor-closed-method:" + (ownerMethod.DeclaringType == typeof(Box<string>)
            && ownerMethod.GetParameters()[0].ParameterType == typeof(string)
            && generic.IsGenericMethod && generic.GetGenericArguments()[0] == typeof(string)
            && ReferenceEquals(TargetOf(closedOwner), box)));
        trace = "";
        int count = 0;
        foreach (Action entry in Delegate.EnumerateInvocationList(abc))
        {
            entry();
            count++;
        }
        int empty = 0;
        foreach (Action entry in Delegate.EnumerateInvocationList<Action>(null)) empty++;
        Console.WriteLine("accessor-public-enumerator:" + (trace == "ABC" && count == 3 && empty == 0));
        Console.WriteLine("Retained multicast accessors end");
    }
}
