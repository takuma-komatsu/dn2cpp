using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GCHandleCheckUninitializedWitness;

internal static class Program
{
    private static int tokenCount;

    private static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "reflection-only") ReflectionOnlyMain();
        else DirectMain();
    }

    private static void DirectMain()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== GCHandle.CheckUninitialized genuine weak tokens ==");
        Zero(false); Zero(true);
        var sentinel = new Payload(71);
        var empty = default(WeakGCHandle<Payload>);
        Check(!empty.IsAllocated && WeakGCHandle<Payload>.ToIntPtr(empty) == IntPtr.Zero, "default weak handle is zero");
        Uninitialized(ref empty, sentinel, "default");
        empty.Dispose(); empty.Dispose();
        Uninitialized(ref empty, sentinel, "default-disposed");
        Live(false); Live(true);
        GC.KeepAlive(sentinel);
        Console.WriteLine("GCHandle.CheckUninitialized genuine weak tokens end");
    }

    private static void Live(bool trackResurrection)
    {
        string label = trackResurrection ? "long" : "short";
        var first = new Payload(11); var second = new Payload(23); var sentinel = new Payload(37);
        var handle = new WeakGCHandle<Payload>(first, trackResurrection);
        try
        {
            IntPtr token = WeakGCHandle<Payload>.ToIntPtr(handle);
            Check(handle.IsAllocated && token != IntPtr.Zero, label + " live native weak token");
            tokenCount = 0; Direct(CountedToken(token)); Check(tokenCount == 1, label + " direct token once");
            tokenCount = 0; Calli(CountedToken(token)); Check(tokenCount == 1, label + " calli token once");
            Payload? found = sentinel;
            Check(handle.TryGetTarget(out found) && ReferenceEquals(found, first), label + " initial identity and out overwrite");
            var alias = WeakGCHandle<Payload>.FromIntPtr(token);
            Check(alias.Equals(handle), label + " token alias identity");
            handle.SetTarget(second);
            found = sentinel;
            Check(alias.TryGetTarget(out found) && ReferenceEquals(found, second), label + " set observed through live alias");
            handle.SetTarget(null);
            found = sentinel;
            Check(!handle.TryGetTarget(out found) && found is null && handle.IsAllocated, label + " null target clears out, token remains allocated");
            Direct(token); Calli(token);
            alias.SetTarget(first);
            Check(handle.TryGetTarget(out found) && ReferenceEquals(found, first), label + " reset identity");
            Pass(label + ":live-null-reset;identity-out;direct-calli;token-once");
            // The alias is not read or freed after its owner's disposal.
        }
        finally { handle.Dispose(); }
        Check(!handle.IsAllocated && WeakGCHandle<Payload>.ToIntPtr(handle) == IntPtr.Zero, label + " disposed token is zero");
        Uninitialized(ref handle, sentinel, label + "-disposed");
        handle.Dispose();
        var nullHandle = new WeakGCHandle<Payload>(null, trackResurrection);
        try
        {
            var token = WeakGCHandle<Payload>.ToIntPtr(nullHandle);
            Check(nullHandle.IsAllocated && token != IntPtr.Zero, label + " allocated-null token");
            Direct(token); Calli(token);
            Payload? found = sentinel;
            Check(!nullHandle.TryGetTarget(out found) && found is null, label + " initially-null out overwrite");
            nullHandle.SetTarget(second);
            Check(nullHandle.TryGetTarget(out found) && ReferenceEquals(found, second), label + " initially-null set");
            Pass(label + ":initially-null-then-live");
        }
        finally { nullHandle.Dispose(); }
        GC.KeepAlive(first); GC.KeepAlive(second); GC.KeepAlive(sentinel);
    }

    private static void Uninitialized(ref WeakGCHandle<Payload> handle, Payload sentinel, string label)
    {
        Payload? found = sentinel; bool threw = false;
        try { _ = handle.TryGetTarget(out found); } catch (NullReferenceException) { threw = true; }
        Check(threw && ReferenceEquals(found, sentinel), label + " tryget faults before out writeback");
        threw = false;
        try { handle.SetTarget(sentinel); } catch (NullReferenceException) { threw = true; }
        Check(threw && !handle.IsAllocated, label + " set faults without allocation");
        Pass(label + ":TryGet-Set-NullReferenceException;out-unchanged");
    }

    private static void Zero(bool address)
    {
        tokenCount = 0; bool threw = false;
        try
        {
            if (address) Calli(CountedToken(IntPtr.Zero)); else Direct(CountedToken(IntPtr.Zero));
        }
        catch (NullReferenceException) { threw = true; }
        Check(threw && tokenCount == 1, "zero token fault and single evaluation");
        Pass(address ? "zero-calli:NullReferenceException;token-once" : "zero-direct:NullReferenceException;token-once");
    }

    private static void ReflectionOnlyMain()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== GCHandle.CheckUninitialized named reflection only ==");
        var method = NamedMethod();
        Check(method.DeclaringType == typeof(GCHandle) && method.IsStatic && method.ReturnType == typeof(void)
            && method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(IntPtr), "genuine named declaration");
        Check(typeof(GCHandle).GetMethod("CheckUninitialized", BindingFlags.Static | BindingFlags.Public,
            null, new[] { typeof(IntPtr) }, null) is null, "private leaf is not public");
        Check(typeof(GCHandle).GetMethod("CheckUninitialized", BindingFlags.Instance | BindingFlags.NonPublic,
            null, new[] { typeof(IntPtr) }, null) is null, "static leaf is not an instance member");
        Check(typeof(GCHandle).GetMethod("CheckUninitialized", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(UIntPtr) }, null) is null, "parameter identity is exact");
        Check(typeof(GCHandle).GetMethod("CheckUninitializedMissing", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(IntPtr) }, null) is null, "unrelated name remains absent");
        Pass("named-negative:public-instance-parameter-name");
        bool threw = false;
        try { Named(IntPtr.Zero); } catch (NullReferenceException) { threw = true; }
        Check(threw, "named zero fault"); Pass("named-zero:NullReferenceException");
        var target = new Payload(43);
        var live = new WeakGCHandle<Payload>(target, false);
        var emptyTarget = new WeakGCHandle<Payload>(null, true);
        try
        {
            // No TryGetTarget/SetTarget here: either would itself directly reach the leaf.
            var liveToken = WeakGCHandle<Payload>.ToIntPtr(live);
            var nullToken = WeakGCHandle<Payload>.ToIntPtr(emptyTarget);
            Check(liveToken != IntPtr.Zero && nullToken != IntPtr.Zero, "genuine allocated weak tokens");
            Named(liveToken); Pass("named-live-token");
            Named(nullToken); Pass("named-null-target-token");
        }
        finally { live.Dispose(); emptyTarget.Dispose(); }
        GC.KeepAlive(target);
        Console.WriteLine("GCHandle.CheckUninitialized named reflection only end");
    }

    private static MethodInfo NamedMethod() => typeof(GCHandle).GetMethod("CheckUninitialized",
        BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(IntPtr) }, null)
        ?? throw new MissingMethodException("System.Runtime.InteropServices.GCHandle.CheckUninitialized");

    private static void Named(IntPtr token)
    {
        try
        {
            var method = typeof(GCHandle).GetMethod("CheckUninitialized", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { typeof(IntPtr) }, null) ?? throw new MissingMethodException("System.Runtime.InteropServices.GCHandle.CheckUninitialized");
            _ = method.Invoke(null, new object[] { token });
        }
        catch (TargetInvocationException error) when (error.InnerException is not null) { throw error.InnerException; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IntPtr CountedToken(IntPtr token) { tokenCount++; return token; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Direct(IntPtr token) => Oracle(token, false);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Calli(IntPtr token) => Oracle(token, true);

    private static void Oracle(IntPtr token, bool address)
    {
        var target = typeof(GCHandle).GetMethod("CheckUninitialized", BindingFlags.Static | BindingFlags.NonPublic,
            null, new[] { typeof(IntPtr) }, null) ?? throw new MissingMethodException("System.Runtime.InteropServices.GCHandle.CheckUninitialized");
        var method = new DynamicMethod("ActualGCHandleCheckUninitialized", typeof(void), new[] { typeof(IntPtr) }, typeof(Program).Module, true);
        var il = method.GetILGenerator(); il.Emit(OpCodes.Ldarg_0);
        if (address)
        {
            il.Emit(OpCodes.Ldftn, target);
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(void), new[] { typeof(IntPtr) }, null);
        }
        else il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Ret);
        ((Action<IntPtr>)method.CreateDelegate(typeof(Action<IntPtr>)))(token);
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
    }
    private static void Pass(string label) => Console.WriteLine("gchandle-check-uninitialized:" + label + ":pass");
    private static void UnusedDietSentinel() { }
}

internal sealed class Payload
{
    internal readonly int Value;
    internal Payload(int value) { Value = value; }
}
