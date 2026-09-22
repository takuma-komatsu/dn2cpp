using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using UnrealSharp.Core;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
Assembly owner = typeof(OwnedValue).Assembly;
try
{
    GCHandleUtilities.AllocateStrongPointer(new OwnedValue(), owner);
    throw new Exception("Unknown owner accepted");
}
catch (InvalidOperationException) { }
GCHandleUtilities.RegisterAssembly(owner);
NativeCallbackGate.Open();
int invocations = 0;
Action multicast = () => ++invocations;
multicast += () => invocations += 10;
GCHandle callback = GCHandleUtilities.AllocateStrongPointer(multicast, owner);
GCHandle wrongCallback = GCHandleUtilities.AllocateStrongPointer((Func<int>)(() => 1), owner);
unsafe
{
    delegate* unmanaged<nint, void> invoke = &UnmanagedCallbacks.InvokeDelegate;
    invoke(GCHandle.ToIntPtr(callback));
    invoke(GCHandle.ToIntPtr(wrongCallback));
}
if (invocations != 11) throw new Exception("Multicast Action callback lost invocation order");
if (LogUnrealSharpCore.LastError is null || !LogUnrealSharpCore.LastError.Contains("must be Action"))
    throw new Exception("Unsupported delegate was not reported at the callback boundary");
nint retained = MakeStrong(owner);
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
if (GCHandleUtilities.GetObjectFromHandlePtr<OwnedValue>(retained) is null)
    throw new Exception("Strong handle failed to retain target");
GCHandleUtilities.Free(GCHandle.FromIntPtr(retained), owner);
OwnedValue value = new();
GCHandle handle = GCHandleUtilities.AllocateStrongPointer(value, owner);
nint pointer = GCHandle.ToIntPtr(handle);
if (!ReferenceEquals(value, GCHandleUtilities.GetObjectFromHandlePtr<OwnedValue>(pointer)))
    throw new Exception("Strong handle lost identity");
unsafe
{
    delegate* unmanaged<nint, void> free = &UnmanagedCallbacks.FreeHandle;
    free(pointer);
    free(pointer);
}
if (value.DisposeCount != 1) throw new Exception("FreeHandle did not dispose exactly once");
if (GCHandleUtilities.GetObjectFromHandlePtr<OwnedValue>(pointer) is not null)
    throw new Exception("Freed handle resolved");
nint ephemeral = MakeWeak(owner);
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();
if (GCHandleUtilities.GetObjectFromHandlePtr<OwnedValue>(ephemeral) is not null)
    throw new Exception("Weak handle retained its target");
GCHandle weak = GCHandleUtilities.AllocateWeakPointer(value);
nint weakPointer = GCHandle.ToIntPtr(weak);
using ManualResetEventSlim entered = new();
using ManualResetEventSlim leave = new();
Action blocking = () => { entered.Set(); leave.Wait(); };
GCHandle blockingHandle = GCHandleUtilities.AllocateStrongPointer(blocking, owner);
Task active = Task.Run(() =>
{
    unsafe
    {
        delegate* unmanaged<nint, void> invoke = &UnmanagedCallbacks.InvokeDelegate;
        invoke(GCHandle.ToIntPtr(blockingHandle));
    }
});
if (!entered.Wait(5000)) throw new Exception("Callback did not enter");
try
{
    NativeCallbackGate.Close(0);
    throw new Exception("Close ignored active callback");
}
catch (TimeoutException) { }
leave.Set();
active.GetAwaiter().GetResult();
NativeCallbackGate.Close(5000);
unsafe
{
    delegate* unmanaged<nint, void> invoke = &UnmanagedCallbacks.InvokeDelegate;
    invoke(GCHandle.ToIntPtr(callback));
}
if (invocations != 11) throw new Exception("Callback admitted after close");
GCHandleUtilities.Free(blockingHandle, owner);
try
{
    NativeCallbackGate.Open();
    throw new Exception("Callback admission reopened after close");
}
catch (InvalidOperationException) { }
GCHandleUtilities.Free(callback, owner);
GCHandleUtilities.Free(wrongCallback, owner);
Parallel.For(0, 128, index =>
{
    try
    {
        GCHandle allocated = GCHandleUtilities.AllocateStrongPointer(value, owner);
        if ((index & 1) == 0) GCHandleUtilities.Free(allocated, owner);
    }
    catch (InvalidOperationException) { }
    if (index == 64) GCHandleUtilities.FreeAssembly(owner);
});
GCHandleUtilities.FreeAssembly(owner);
if (GCHandleUtilities.GetObjectFromHandlePtr<OwnedValue>(weakPointer) is not null)
    throw new Exception("Weak handle survived owner shutdown");
try
{
    GCHandleUtilities.RegisterAssembly(owner);
    throw new Exception("Closed owner reopened");
}
catch (InvalidOperationException) { }
try
{
    GCHandleUtilities.AllocateStrongPointer(value, owner);
    throw new Exception("Allocation after shutdown accepted");
}
catch (InvalidOperationException) { }
NativeCallbackGate.Close(5000);
Console.WriteLine("UNREALSHARP_OWNER_LIFETIME_OK");

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static nint MakeWeak(Assembly owner) => GCHandle.ToIntPtr(GCHandleUtilities.AllocateWeakPointer(new OwnedValue(), owner));

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static nint MakeStrong(Assembly owner) => GCHandle.ToIntPtr(GCHandleUtilities.AllocateStrongPointer(new OwnedValue(), owner));

sealed class OwnedValue : IDisposable
{
    public int DisposeCount;
    public void Dispose() => ++DisposeCount;
}
