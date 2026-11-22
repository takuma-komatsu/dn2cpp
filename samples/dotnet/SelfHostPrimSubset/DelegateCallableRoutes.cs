using System;
using System.Reflection;

namespace DelegateCallableBodies;

// The fixture replaces these stubs with exact CoreLib call or ldftn/calli sites.
internal static class Routes
{
    internal static bool Equals(Delegate receiver, object value)
    {
        if (Program.Route == 3) return CallvirtDelegateEquals(receiver, value);
        if (Program.Route == 4) return VirtCalliDelegateEquals(receiver, value);
        return Program.Route == 1 ? CallDelegateEquals(receiver, value) : CalliDelegateEquals(receiver, value);
    }

    internal static int Hash(Delegate receiver)
    {
        if (Program.Route == 3) return CallvirtDelegateHash(receiver);
        if (Program.Route == 4) return VirtCalliDelegateHash(receiver);
        return Program.Route == 1 ? CallDelegateHash(receiver) : CalliDelegateHash(receiver);
    }

    internal static void Rebind(Type owner, Delegate receiver, object target, string name)
    {
        if (Program.Route == 0)
        {
            ConstructorInfo ctor = owner.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { target is Type ? typeof(Type) : typeof(object), typeof(string) }, null);
            ctor.Invoke(receiver, new[] { target, name });
            return;
        }
        bool multicast = owner == typeof(MulticastDelegate);
        if (target is Type type)
        {
            if (Program.Route == 1 || Program.Route == 3)
            {
                if (multicast) CallMulticastCtorType((MulticastDelegate)receiver, type, name);
                else CallDelegateCtorType(receiver, type, name);
            }
            else
            {
                if (multicast) CalliMulticastCtorType((MulticastDelegate)receiver, type, name);
                else CalliDelegateCtorType(receiver, type, name);
            }
        }
        else if (Program.Route == 1 || Program.Route == 3)
        {
            if (multicast) CallMulticastCtorObject((MulticastDelegate)receiver, target, name);
            else CallDelegateCtorObject(receiver, target, name);
        }
        else
        {
            if (multicast) CalliMulticastCtorObject((MulticastDelegate)receiver, target, name);
            else CalliDelegateCtorObject(receiver, target, name);
        }
    }

    internal static void VmConstructor(MethodInfo method, MulticastDelegate receiver)
    {
        IntPtr pointer = new IntPtr(1);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        switch (method.Name)
        {
            case "CtorClosed":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorClosed", flags, null,
                        new[] { typeof(object), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorClosed(receiver, null, pointer);
                else CalliCtorClosed(receiver, null, pointer);
                break;
            case "CtorClosedStatic":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorClosedStatic", flags, null,
                        new[] { typeof(object), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorClosedStatic(receiver, null, pointer);
                else CalliCtorClosedStatic(receiver, null, pointer);
                break;
            case "CtorRTClosed":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorRTClosed", flags, null,
                        new[] { typeof(object), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorRTClosed(receiver, null, pointer);
                else CalliCtorRTClosed(receiver, null, pointer);
                break;
            case "CtorOpen":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorOpen", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorOpen(receiver, null, pointer, pointer);
                else CalliCtorOpen(receiver, null, pointer, pointer);
                break;
            case "CtorVirtualDispatch":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorVirtualDispatch", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorVirtualDispatch(receiver, null, pointer, pointer);
                else CalliCtorVirtualDispatch(receiver, null, pointer, pointer);
                break;
            case "CtorCollectibleClosedStatic":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorCollectibleClosedStatic", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorCollectibleClosedStatic(receiver, null, pointer, pointer);
                else CalliCtorCollectibleClosedStatic(receiver, null, pointer, pointer);
                break;
            case "CtorCollectibleOpen":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorCollectibleOpen", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorCollectibleOpen(receiver, null, pointer, pointer, pointer);
                else CalliCtorCollectibleOpen(receiver, null, pointer, pointer, pointer);
                break;
            case "CtorCollectibleVirtualDispatch":
                if (Program.Route == 0)
                    typeof(Delegate).GetMethod("CtorCollectibleVirtualDispatch", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorCollectibleVirtualDispatch(receiver, null, pointer, pointer, pointer);
                else CalliCtorCollectibleVirtualDispatch(receiver, null, pointer, pointer, pointer);
                break;
            default: throw new InvalidOperationException(method.Name);
        }
    }

    internal static bool CallDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CallDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallFactory(MulticastDelegate receiver, Array entries, int count, bool flag) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallCombine(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallRemove(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object CallTarget(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo CallMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate[] CallList(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CallSingle(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallAt(MulticastDelegate receiver, int index) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallDelegateCtorObject(Delegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallDelegateCtorType(Delegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallMulticastCtorObject(MulticastDelegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallMulticastCtorType(MulticastDelegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorClosedStatic(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorRTClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorOpen(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorCollectibleClosedStatic(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorCollectibleOpen(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorCollectibleVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CalliDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CalliDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CalliFactory(MulticastDelegate receiver, Array entries, int count, bool flag) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CalliCombine(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CalliRemove(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object CalliTarget(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo CalliMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate[] CalliList(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CalliSingle(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CalliAt(MulticastDelegate receiver, int index) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliDelegateCtorObject(Delegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliDelegateCtorType(Delegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliMulticastCtorObject(MulticastDelegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliMulticastCtorType(MulticastDelegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorClosedStatic(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorRTClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorOpen(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorCollectibleClosedStatic(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorCollectibleOpen(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorCollectibleVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");

    internal static bool CallvirtDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool VirtCalliDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CallvirtDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int VirtCalliDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo CallvirtMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo VirtCalliMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CallvirtEqualsOrder(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static IntPtr VirtualBaseAddress(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static IntPtr VirtualFinalAddress(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Func<object, bool> VirtualBoundEquals(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Func<object, bool> VirtualBoundEqualsLocal(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
}
