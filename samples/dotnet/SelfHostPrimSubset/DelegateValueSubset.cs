using System;
using System.Collections.Generic;
using System.Reflection;
using DelegateCallableBodies;
using Driver = DelegateCallableBodies.Program;

namespace DelegateValueSubset;

internal static class Program
{
    private static string evaluationTrace = "";
    private delegate string Other(string value);
    private sealed class Target { internal void Touch() { } }
    private static T Echo<T>(T value) => value;
    private static void First() { }
    private static void Second() { }

    internal static object EffectArgument()
    {
        evaluationTrace += "A";
        return null;
    }

    private static Delegate EffectReceiver(Delegate value)
    {
        evaluationTrace += "R";
        return value;
    }

    private static void Check(string label, Delegate a, Delegate b, bool equal)
    {
        MethodInfo equals = typeof(MulticastDelegate).GetMethod("Equals", new[] { typeof(object) });
        MethodInfo hash = typeof(MulticastDelegate).GetMethod("GetHashCode", Type.EmptyTypes);
        bool retainedEquals = Driver.Route == 0 ? (bool)equals.Invoke(a, new object[] { b })
            : Routes.Equals(equals, a, b);
        Console.WriteLine(label + ":" + (a.Equals(b) == equal && ((object)a).Equals(b) == equal
            && EqualityComparer<Delegate>.Default.Equals(a, b) == equal && retainedEquals == equal));
        if (equal)
        {
            int retainedHashA = Driver.Route == 0 ? (int)hash.Invoke(a, null) : Routes.Hash(hash, a);
            int retainedHashB = Driver.Route == 0 ? (int)hash.Invoke(b, null) : Routes.Hash(hash, b);
            Console.WriteLine(label + "-hash:" + (a.GetHashCode() == b.GetHashCode()
                && retainedHashA == retainedHashB && retainedHashA == a.GetHashCode()));
        }
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
        bool reflectedEquals = Driver.Route == 0 ? (bool)multiEquals.Invoke(a, new object[] { b })
            : Routes.Equals(multiEquals, a, b);
        bool reflectedOther = Driver.Route == 0 ? (bool)multiEquals.Invoke(a, new object[] { otherClosed })
            : Routes.Equals(multiEquals, a, otherClosed);
        int reflectedHash = Driver.Route == 0 ? (int)multiHash.Invoke(a, null) : Routes.Hash(multiHash, a);
        bool delegateEquals = Driver.Route == 0 ? (bool)baseEquals.Invoke(a, new object[] { b })
            : Routes.Equals(baseEquals, a, b);
        int delegateHash = Driver.Route == 0 ? (int)baseHash.Invoke(a, null) : Routes.Hash(baseHash, a);
        Console.WriteLine("delegate-reflected-equals:" + (reflectedEquals && !reflectedOther));
        Console.WriteLine("delegate-reflected-hash:" + (reflectedHash == a.GetHashCode()));
        Console.WriteLine("delegate-base-equals:" + delegateEquals);
        Console.WriteLine("delegate-base-hash:" + (delegateHash == a.GetHashCode()));
        var boundEquals = (Func<object, bool>)multiEquals.CreateDelegate(typeof(Func<object, bool>), a);
        var boundHash = (Func<int>)multiHash.CreateDelegate(typeof(Func<int>), a);
        Console.WriteLine("delegate-bound-equals:" + (boundEquals(b) && !boundEquals(otherDeclaration)));
        Console.WriteLine("delegate-bound-hash:" + (boundHash() == a.GetHashCode()));
        Console.WriteLine("Retained delegate value members end");
    }

    internal static void RunOwnerContracts()
    {
        Console.WriteLine("== Delegate declared-owner value contracts ==");
        MethodInfo baseEquals = typeof(Delegate).GetMethod("Equals", new[] { typeof(object) });
        MethodInfo multiEquals = typeof(MulticastDelegate).GetMethod("Equals", new[] { typeof(object) });
        MethodInfo baseHash = typeof(Delegate).GetMethod("GetHashCode", Type.EmptyTypes);
        MethodInfo multiHash = typeof(MulticastDelegate).GetMethod("GetHashCode", Type.EmptyTypes);
        bool nonvirtualBase = Driver.Route == 1 || Driver.Route == 2;
        Action first = First, second = Second;
        var target = new Target();
        Action instance = target.Touch;
        Action source = first + second;
        Action[] others = { second + first, first + second + first, instance + second, first };
        for (int index = 0; index < others.Length; index++)
        {
            Action other = others[index];
            bool byBase = Driver.Route == 0 ? (bool)baseEquals.Invoke(source, new object[] { other })
                : Routes.Equals(baseEquals, source, other);
            bool byMulticast = Driver.Route == 0 ? (bool)multiEquals.Invoke(source, new object[] { other })
                : Routes.Equals(multiEquals, source, other);
            bool expectedBase = nonvirtualBase && index < 3;
            Console.WriteLine("delegate-owner-equals-" + index + ":" + (byBase == expectedBase && !byMulticast));
        }
        int staticBaseHash = Routes.CallDelegateHash(first);
        Action[] values = { source, second + first, source + first, instance + second, instance };
        for (int index = 0; index < values.Length; index++)
        {
            Action value = values[index];
            int byBase = Driver.Route == 0 ? (int)baseHash.Invoke(value, null) : Routes.Hash(baseHash, value);
            int byMulticast = Driver.Route == 0 ? (int)multiHash.Invoke(value, null) : Routes.Hash(multiHash, value);
            int expectedBase = nonvirtualBase && index < 4 ? staticBaseHash : byMulticast;
            Console.WriteLine("delegate-owner-hash-" + index + ":" + (byBase == expectedBase));
        }
        Console.WriteLine("Delegate declared-owner value contracts end");
    }

    private static string NullAddress(bool multicast)
    {
        try
        {
            IntPtr address = multicast ? Routes.VirtualMulticastAddress(null) : Routes.VirtualBaseAddress(null);
            return address == IntPtr.Zero ? "zero" : "address";
        }
        catch (NullReferenceException) { return nameof(NullReferenceException); }
    }

    internal static void RunVirtualBindings()
    {
        Console.WriteLine("== Delegate virtual binding identity ==");
        Action first = First, second = Second;
        Action source = first + second, different = second + first;
        var immediate = Routes.VirtualBoundEquals(source);
        var local = Routes.VirtualBoundEqualsLocal(source);
        var reflected = (Func<object, bool>)typeof(Delegate).GetMethod("Equals", new[] { typeof(object) })
            .CreateDelegate(typeof(Func<object, bool>), source);
        Console.WriteLine("delegate-virtual-binding-values:" + (immediate(source) && local(source) && reflected(source)
            && !immediate(different) && !local(different) && !reflected(different)));
        MethodInfo immediateMethod = MulticastAccessorSubset.Program.MethodOf(immediate);
        MethodInfo localMethod = MulticastAccessorSubset.Program.MethodOf(local);
        MethodInfo reflectedMethod = MulticastAccessorSubset.Program.MethodOf(reflected);
        Console.WriteLine("delegate-virtual-binding-method:" + (immediateMethod.DeclaringType == typeof(MulticastDelegate)
            && immediateMethod.Name == "Equals" && immediateMethod.Equals(localMethod) && immediateMethod.Equals(reflectedMethod)));
        Console.WriteLine("delegate-virtual-binding-identity:" + (immediate.Equals(local) && immediate.Equals(reflected)
            && immediate.GetHashCode() == local.GetHashCode() && immediate.GetHashCode() == reflected.GetHashCode()
            && ReferenceEquals(immediate.Target, source) && ReferenceEquals(local.Target, source)
            && ReferenceEquals(reflected.Target, source)));
        foreach (bool multicast in new[] { false, true })
        {
            MethodInfo method = multicast
                ? typeof(MulticastDelegate).GetMethod("Equals", new[] { typeof(object) })
                : typeof(Delegate).GetMethod("Equals", new[] { typeof(object) });
            var closedNull = (Func<object, bool>)method.CreateDelegate(typeof(Func<object, bool>), null);
            bool nullOther = closedNull(null);
            bool caught = false;
            try { closedNull(source); }
            catch (NullReferenceException) { caught = true; }
            Console.WriteLine("delegate-closed-null-" + method.DeclaringType.Name + ":" + (!nullOther && caught));
        }
        Console.WriteLine("Delegate virtual binding identity end");
    }

    internal static void RunVirtualBoundaries()
    {
        Console.WriteLine("== Delegate virtual null boundaries ==");
        evaluationTrace = "";
        bool caught = false;
        try { Routes.CallvirtEqualsOrder(EffectReceiver(null)); }
        catch (NullReferenceException) { caught = true; }
        Console.WriteLine("delegate-callvirt-null-order:" + (caught && evaluationTrace == "RA"));
        evaluationTrace = "";
        Action first = First;
        Console.WriteLine("delegate-callvirt-value-order:" + (!Routes.CallvirtEqualsOrder(EffectReceiver(first))
            && evaluationTrace == "RA"));
        Console.WriteLine("delegate-ldvirtftn-base-null:" + NullAddress(false));
        Console.WriteLine("delegate-ldvirtftn-final-null:" + NullAddress(true));
        foreach (bool address in new[] { false, true })
        {
            bool nullOther = address ? Routes.CalliDelegateEquals(null, null) : Routes.CallDelegateEquals(null, null);
            bool equalsCaught = false, hashCaught = false;
            try
            {
                if (address) Routes.CalliDelegateEquals(null, first);
                else Routes.CallDelegateEquals(null, first);
            }
            catch (NullReferenceException) { equalsCaught = true; }
            try
            {
                if (address) Routes.CalliDelegateHash(null);
                else Routes.CallDelegateHash(null);
            }
            catch (NullReferenceException) { hashCaught = true; }
            Console.WriteLine("delegate-base-null-" + address + ":" + (!nullOther && equalsCaught && hashCaught));
        }
        Console.WriteLine("Delegate virtual null boundaries end");
    }
}
