#nullable enable
using System;
using System.Runtime.InteropServices;

// Pins the SafeBuffer/SafeHandle reachability cut against the emit guard.
// DangerousGetHandle is declared on the base type, so app code calls the real
// body; the one receiver dn2cpp lowers inline is keyed on the receiver's static
// C++ type, which reachability cannot see. So a cut keyed on declaring type and
// name deletes the body while the call site still emits the call — a green
// transpile and a C++ link error. Only this section holds that line.
namespace SafeHandleSubset;

internal sealed class NativeThing : SafeHandle
{
    public NativeThing(nint h) : base(IntPtr.Zero, ownsHandle: true) => SetHandle(h);

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        Released = true;
        return true;
    }

    internal static bool Released;
}

internal static class Program
{
    internal static void __GateEntry()
    {
        NativeThing.Released = false;
        using (var h = new NativeThing(42))
        {
            Console.WriteLine("shRaw=" + (long)h.DangerousGetHandle());
            Console.WriteLine("shInvalid=" + h.IsInvalid + " shClosed=" + h.IsClosed);

            // An AddRef pins the handle; DangerousGetHandle stays valid across it.
            bool taken = false;
            h.DangerousAddRef(ref taken);
            Console.WriteLine("shAddRef=" + taken + " raw=" + (long)h.DangerousGetHandle());
            h.DangerousRelease();
        }
        Console.WriteLine("shReleased=" + NativeThing.Released);

        // An invalid (zero) handle never runs ReleaseHandle.
        NativeThing.Released = false;
        using (var z = new NativeThing(0))
            Console.WriteLine("shZeroInvalid=" + z.IsInvalid);
        Console.WriteLine("shZeroReleased=" + NativeThing.Released);
    }
}
