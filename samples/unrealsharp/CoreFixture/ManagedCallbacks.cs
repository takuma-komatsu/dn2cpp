namespace UnrealSharp.Core;

public struct ManagedCallbacks
{
    public nint CreateManagedObject;
    public nint InvokeManagedMethod;
    public nint InvokeDelegate;
    public nint LookupManagedMethod;
    public nint LookupManagedType;
    public nint Dispose;
    public nint Tick;
    public nint ReleaseHandle;
    public nint Shutdown;
}

public static class UnmanagedCallbacks
{
    [System.Runtime.InteropServices.UnmanagedCallersOnly]
    public static int InvokeManagedMethod(nint receiver, nint method, nint arguments, nint result, nint error)
    {
        throw new System.InvalidOperationException("fixture int callback failure");
    }
}
