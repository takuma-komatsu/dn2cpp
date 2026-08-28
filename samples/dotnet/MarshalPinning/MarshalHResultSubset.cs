#nullable enable
using System;
using System.Runtime.InteropServices;

namespace MarshalHResultSubset;

internal static class Program
{
    private static void Probe(string label, int hresult)
    {
        Exception? converted = Marshal.GetExceptionForHR(hresult);
        Console.WriteLine(label + " type=" + converted?.GetType()
            + " hresult=" + converted?.HResult.ToString("X8"));

        try
        {
            Marshal.ThrowExceptionForHR(hresult);
            Console.WriteLine(label + " catch=NOTHROW");
        }
        catch (ArgumentException)
        {
            Console.WriteLine(label + " catch=ArgumentException");
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine(label + " catch=OutOfMemoryException");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine(label + " catch=UnauthorizedAccessException");
        }
        catch (COMException)
        {
            Console.WriteLine(label + " catch=COMException");
        }
        catch (Exception ex)
        {
            Console.WriteLine(label + " catch=" + ex.GetType());
        }
    }

    internal static void __GateEntry()
    {
        Console.WriteLine("== marshal hresult ==");

        Console.WriteLine("success null=" + (Marshal.GetExceptionForHR(0) is null));
        Marshal.ThrowExceptionForHR(0);
        Console.WriteLine("throw success=returned");

        Probe("E_FAIL", unchecked((int)0x80004005));
        Probe("unknown", unchecked((int)0x81234567));
        Probe("E_INVALIDARG", unchecked((int)0x80070057));
        Probe("E_OUTOFMEMORY", unchecked((int)0x8007000E));
        Probe("E_ACCESSDENIED", unchecked((int)0x80070005));
    }
}
