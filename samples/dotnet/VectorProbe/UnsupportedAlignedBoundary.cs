using System;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static unsafe class UnsupportedAlignedBoundary<TVector, T> where TVector : struct where T : unmanaged
{
    private delegate TVector LoadCall(T* source);
    private delegate void StoreCall(TVector value, T* destination);
    private static string effects;

    // The gate rewrites these call and address operands to the internal defaults.
    private static TVector LoadAligned(T* source) => throw new InvalidOperationException("fixture not rewritten");
    private static void StoreAligned(TVector value, T* destination) => throw new InvalidOperationException("fixture not rewritten");

    private static TVector Value()
    {
        effects += "V";
        return default;
    }

    private static T* Pointer(T* pointer, bool fail)
    {
        effects += "P";
        if (fail) throw new ArgumentException("caller");
        return pointer;
    }

    private static void Probe(int mode, T* pointer, bool address, bool fail)
    {
        effects = "";
        string exception = "none";
        try
        {
            if (address)
            {
                if (mode < 2)
                {
                    LoadCall load = LoadAligned;
                    _ = load(Pointer(pointer, fail));
                }
                else
                {
                    StoreCall store = StoreAligned;
                    store(Value(), Pointer(pointer, fail));
                }
            }
            else
            {
                switch (mode)
                {
                    case 0: _ = LoadAligned(Pointer(pointer, fail)); break;
                    case 2: StoreAligned(Value(), Pointer(pointer, fail)); break;
                }
            }
        }
        catch (PlatformNotSupportedException ex)
        {
            if (!ex.Message.Contains("unsupported vector element")) throw;
            exception = "boundary";
        }
        catch (ArgumentException ex)
        {
            if (ex.Message != "caller") throw;
            exception = "caller";
        }
        if (exception != (fail ? "caller" : "boundary") || effects != (mode < 2 ? "P" : "VP"))
            throw new InvalidOperationException("Unsupported aligned boundary or argument order");
    }

    internal static void Run(string label)
    {
        byte* storage = stackalloc byte[192];
        for (int i = 0; i < 192; i++) storage[i] = 0x5a;
        T* aligned = (T*)(((nuint)storage + 63) & ~(nuint)63);
        foreach (int mode in new[] { 0, 2 })
            foreach (bool address in new[] { false, true })
            {
                Probe(mode, aligned, address, false);
                Probe(mode, (T*)((byte*)aligned + 1), address, false);
                Probe(mode, null, address, false);
                Probe(mode, aligned, address, true);
                for (int i = 0; i < 192; i++)
                    if (storage[i] != 0x5a) throw new InvalidOperationException("Unsupported aligned boundary wrote memory");
                Console.WriteLine("unsupported aligned " + label + " " + mode + " " + (address ? "address" : "direct") + ": boundary, caller order, canary");
            }
    }
}

internal static class UnsupportedAlignedBoundary
{
    internal static void Run()
    {
        UnsupportedAlignedBoundary<Vector<decimal>, decimal>.Run("Vector<decimal>");
        UnsupportedAlignedBoundary<Vector<char>, char>.Run("Vector<char>");
        UnsupportedAlignedBoundary<Vector64<Int128>, Int128>.Run("Vector64<Int128>");
        UnsupportedAlignedBoundary<Vector128<char>, char>.Run("Vector128<char>");
        UnsupportedAlignedBoundary<Vector256<UInt128>, UInt128>.Run("Vector256<UInt128>");
        UnsupportedAlignedBoundary<Vector512<decimal>, decimal>.Run("Vector512<decimal>");
        Console.WriteLine("unsupported aligned boundary end");
    }
}
