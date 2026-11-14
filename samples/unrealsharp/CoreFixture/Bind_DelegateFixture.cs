using System;
using System.Runtime.InteropServices;

namespace UnrealSharp.Interop;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate int ProbeCallback(int value);

public static unsafe class Bind_FScriptSet
{
    private static nint _address;
    private static int Invoke(ProbeCallback first, ProbeCallback second, int value)
        => ((delegate* unmanaged<ProbeCallback, ProbeCallback, int, int>)_address)(first, second, value);

    public static bool Probe(nint address)
    {
        _address = address;
        for (int i = 0; i < 40; ++i)
        {
            int captured = i;
            ProbeCallback first = value => { GC.Collect(); return value + captured; };
            ProbeCallback second = value => value * 2 + 3;
            if (Invoke(first, second, i) != 4 * i + 3)
                return false;
        }
        ProbeCallback nested = value => Invoke(x => x + 1, x => x + 2, value);
        if (Invoke(nested, value => value, 4) != 15)
            return false;
        return Invoke(value => { GC.Collect(); return value; }, value => value, 777) == 1554;
    }

    public static void ThrowCallback()
    {
        Invoke(value => throw new InvalidOperationException("fixture scoped callback failure"), value => 0, 0);
    }

    private static int Recurse(int depth)
    {
        if (depth == 0)
            return 0;
        var callback = (delegate* unmanaged<ProbeCallback?, ProbeCallback?, int, int>)_address;
        try { return callback(value => Recurse(depth - 1), depth == 40 ? null : value => 0, depth); }
        catch (PlatformNotSupportedException) { return callback(value => 1, null, 0); }
    }

    public static bool ProbeCapacity() => Recurse(40) == 1 && Probe(_address);
}

public static unsafe class Bind_BoolFixture
{
    public static bool Probe(nint address)
    {
        var callback = (delegate* unmanaged<bool, bool>)address;
        return callback(false) && !callback(true);
    }
}
