#nullable enable
using System;
using System.Runtime.InteropServices;

namespace MarshalHResultSubset;

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== marshal hresult ==");

        Console.WriteLine("success null=" + (Marshal.GetExceptionForHR(0) is null));

        int failure = unchecked((int)0x80004005);
        Exception? converted = Marshal.GetExceptionForHR(failure);
        Console.WriteLine("failure present=" + (converted is not null)
            + " hresult=" + converted?.HResult.ToString("X8"));

        Marshal.ThrowExceptionForHR(0);
        Console.WriteLine("throw success=returned");
        try
        {
            Marshal.ThrowExceptionForHR(failure);
            Console.WriteLine("throw failure=NOTHROW");
        }
        catch (Exception ex)
        {
            Console.WriteLine("throw failure=" + ex.HResult.ToString("X8"));
        }
    }
}
