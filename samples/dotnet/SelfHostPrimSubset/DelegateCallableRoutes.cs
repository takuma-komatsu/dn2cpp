using System;
using System.Reflection;

namespace DelegateCallableBodies;

// The fixture replaces these stubs with exact CoreLib call or ldftn/calli sites.
internal static class Routes
{
    internal static bool Equals(MethodInfo method, Delegate receiver, object value)
    {
        bool multicast = method.DeclaringType == typeof(MulticastDelegate);
        if (Program.Route == 3)
            return multicast ? CallvirtMulticastEquals((MulticastDelegate)receiver, value) : CallvirtDelegateEquals(receiver, value);
        if (Program.Route == 4)
            return multicast ? VirtCalliMulticastEquals((MulticastDelegate)receiver, value) : VirtCalliDelegateEquals(receiver, value);
        return Program.Route == 1
            ? multicast ? CallMulticastEquals((MulticastDelegate)receiver, value) : CallDelegateEquals(receiver, value)
            : multicast ? CalliMulticastEquals((MulticastDelegate)receiver, value) : CalliDelegateEquals(receiver, value);
    }

    internal static int Hash(MethodInfo method, Delegate receiver)
    {
        bool multicast = method.DeclaringType == typeof(MulticastDelegate);
        if (Program.Route == 3)
            return multicast ? CallvirtMulticastHash((MulticastDelegate)receiver) : CallvirtDelegateHash(receiver);
        if (Program.Route == 4)
            return multicast ? VirtCalliMulticastHash((MulticastDelegate)receiver) : VirtCalliDelegateHash(receiver);
        return Program.Route == 1
            ? multicast ? CallMulticastHash((MulticastDelegate)receiver) : CallDelegateHash(receiver)
            : multicast ? CalliMulticastHash((MulticastDelegate)receiver) : CalliDelegateHash(receiver);
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
                    typeof(MulticastDelegate).GetMethod("CtorClosed", flags, null,
                        new[] { typeof(object), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorClosed(receiver, null, pointer);
                else CalliCtorClosed(receiver, null, pointer);
                break;
            case "CtorClosedStatic":
                if (Program.Route == 0)
                    typeof(MulticastDelegate).GetMethod("CtorClosedStatic", flags, null,
                        new[] { typeof(object), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorClosedStatic(receiver, null, pointer);
                else CalliCtorClosedStatic(receiver, null, pointer);
                break;
            case "CtorRTClosed":
                if (Program.Route == 0)
                    typeof(MulticastDelegate).GetMethod("CtorRTClosed", flags, null,
                        new[] { typeof(object), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorRTClosed(receiver, null, pointer);
                else CalliCtorRTClosed(receiver, null, pointer);
                break;
            case "CtorOpened":
                if (Program.Route == 0)
                    typeof(MulticastDelegate).GetMethod("CtorOpened", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorOpened(receiver, null, pointer, pointer);
                else CalliCtorOpened(receiver, null, pointer, pointer);
                break;
            case "CtorVirtualDispatch":
                if (Program.Route == 0)
                    typeof(MulticastDelegate).GetMethod("CtorVirtualDispatch", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorVirtualDispatch(receiver, null, pointer, pointer);
                else CalliCtorVirtualDispatch(receiver, null, pointer, pointer);
                break;
            case "CtorCollectibleClosedStatic":
                if (Program.Route == 0)
                    typeof(MulticastDelegate).GetMethod("CtorCollectibleClosedStatic", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorCollectibleClosedStatic(receiver, null, pointer, pointer);
                else CalliCtorCollectibleClosedStatic(receiver, null, pointer, pointer);
                break;
            case "CtorCollectibleOpened":
                if (Program.Route == 0)
                    typeof(MulticastDelegate).GetMethod("CtorCollectibleOpened", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorCollectibleOpened(receiver, null, pointer, pointer, pointer);
                else CalliCtorCollectibleOpened(receiver, null, pointer, pointer, pointer);
                break;
            case "CtorCollectibleVirtualDispatch":
                if (Program.Route == 0)
                    typeof(MulticastDelegate).GetMethod("CtorCollectibleVirtualDispatch", flags, null,
                        new[] { typeof(object), typeof(IntPtr), typeof(IntPtr), typeof(IntPtr) }, null).Invoke(receiver, new object[] { null, pointer, pointer, pointer });
                else if (Program.Route == 1 || Program.Route == 3) CallCtorCollectibleVirtualDispatch(receiver, null, pointer, pointer, pointer);
                else CalliCtorCollectibleVirtualDispatch(receiver, null, pointer, pointer, pointer);
                break;
            default: throw new InvalidOperationException(method.Name);
        }
    }

    internal static bool CallDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CallDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CallMulticastEquals(MulticastDelegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CallMulticastHash(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MulticastDelegate CallFactory(MulticastDelegate receiver, object[] entries, int count) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MulticastDelegate CallFactoryFlag(MulticastDelegate receiver, object[] entries, int count, bool flag) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallCombine(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallRemove(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object CallTarget(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo CallMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate[] CallList(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CallSingle(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object CallAt(MulticastDelegate receiver, int index) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallDelegateCtorObject(Delegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallDelegateCtorType(Delegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallMulticastCtorObject(MulticastDelegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallMulticastCtorType(MulticastDelegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorClosedStatic(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorRTClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorOpened(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorCollectibleClosedStatic(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorCollectibleOpened(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CallCtorCollectibleVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CalliDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CalliDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CalliMulticastEquals(MulticastDelegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CalliMulticastHash(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MulticastDelegate CalliFactory(MulticastDelegate receiver, object[] entries, int count) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MulticastDelegate CalliFactoryFlag(MulticastDelegate receiver, object[] entries, int count, bool flag) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CalliCombine(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CalliRemove(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object CalliTarget(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo CalliMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate[] CalliList(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CalliSingle(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object CalliAt(MulticastDelegate receiver, int index) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliDelegateCtorObject(Delegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliDelegateCtorType(Delegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliMulticastCtorObject(MulticastDelegate receiver, object target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliMulticastCtorType(MulticastDelegate receiver, Type target, string name) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorClosedStatic(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorRTClosed(MulticastDelegate receiver, object target, IntPtr p0) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorOpened(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorCollectibleClosedStatic(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorCollectibleOpened(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static void CalliCtorCollectibleVirtualDispatch(MulticastDelegate receiver, object target, IntPtr p0, IntPtr p1, IntPtr p2) => throw new InvalidOperationException("IL fixture not rewritten");

    internal static bool CallvirtDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool VirtCalliDelegateEquals(Delegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CallvirtDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int VirtCalliDelegateHash(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CallvirtMulticastEquals(MulticastDelegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool VirtCalliMulticastEquals(MulticastDelegate receiver, object value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int CallvirtMulticastHash(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static int VirtCalliMulticastHash(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallvirtCombine(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate VirtCalliCombine(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate CallvirtRemove(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate VirtCalliRemove(MulticastDelegate receiver, Delegate value) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object CallvirtTarget(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static object VirtCalliTarget(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo CallvirtMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static MethodInfo VirtCalliMethod(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate[] CallvirtList(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Delegate[] VirtCalliList(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static bool CallvirtEqualsOrder(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static IntPtr VirtualBaseAddress(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static IntPtr VirtualMulticastAddress(MulticastDelegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Func<object, bool> VirtualBoundEquals(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
    internal static Func<object, bool> VirtualBoundEqualsLocal(Delegate receiver) => throw new InvalidOperationException("IL fixture not rewritten");
}
