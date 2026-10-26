using System;
using System.Collections.Generic;
using System.Reflection;

namespace DelegateValueSubset;

internal static class Program
{
    private delegate string Other(string value);
    private sealed class Target { internal void Touch() { } }
    private static T Echo<T>(T value) => value;
    private static void First() { }
    private static void Second() { }

    private static void Check(string label, Delegate a, Delegate b, bool equal)
    {
        Console.WriteLine(label + ":" + (a.Equals(b) == equal && ((object)a).Equals(b) == equal
            && EqualityComparer<Delegate>.Default.Equals(a, b) == equal));
        if (equal) Console.WriteLine(label + "-hash:" + (a.GetHashCode() == b.GetHashCode()));
    }

    internal static void Run()
    {
        Console.WriteLine("== Retained delegate value members ==");
        Func<string, string> a = Echo<string>, b = Echo<string>;
        Func<object, object> otherClosed = Echo<object>;
        Other otherDeclaration = Echo<string>;
        Check("delegate-same", a, b, true);
        Check("delegate-closed-type", a, otherClosed, false);
        Check("delegate-declaration", a, otherDeclaration, false);
        Check("delegate-null", a, null, false);
        var target = new Target();
        var otherTarget = new Target();
        Action ta = target.Touch, tb = target.Touch, tc = otherTarget.Touch;
        Check("delegate-target-same", ta, tb, true);
        Check("delegate-target-other", ta, tc, false);
        Action first = First, second = Second;
        Check("delegate-chain", ta + first + second, tb + first + second, true);
        Check("delegate-order", first + second, second + first, false);
        Check("delegate-length", first + second, first, false);

        MethodInfo multiEquals = typeof(MulticastDelegate).GetMethod("Equals", new[] { typeof(object) });
        MethodInfo multiHash = typeof(MulticastDelegate).GetMethod("GetHashCode", Type.EmptyTypes);
        MethodInfo baseEquals = typeof(Delegate).GetMethod("Equals", new[] { typeof(object) });
        MethodInfo baseHash = typeof(Delegate).GetMethod("GetHashCode", Type.EmptyTypes);
        Console.WriteLine("delegate-reflected-equals:" + ((bool)multiEquals.Invoke(a, new object[] { b })
            && !(bool)multiEquals.Invoke(a, new object[] { otherClosed })));
        Console.WriteLine("delegate-reflected-hash:" + ((int)multiHash.Invoke(a, null) == a.GetHashCode()));
        Console.WriteLine("delegate-base-equals:" + (bool)baseEquals.Invoke(a, new object[] { b }));
        Console.WriteLine("delegate-base-hash:" + ((int)baseHash.Invoke(a, null) == a.GetHashCode()));
        var boundEquals = (Func<object, bool>)multiEquals.CreateDelegate(typeof(Func<object, bool>), a);
        var boundHash = (Func<int>)multiHash.CreateDelegate(typeof(Func<int>), a);
        Console.WriteLine("delegate-bound-equals:" + (boundEquals(b) && !boundEquals(otherDeclaration)));
        Console.WriteLine("delegate-bound-hash:" + (boundHash() == a.GetHashCode()));
        Console.WriteLine("Retained delegate value members end");
    }
}
