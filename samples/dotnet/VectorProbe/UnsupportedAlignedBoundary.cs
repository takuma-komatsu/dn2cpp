using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.Intrinsics;

namespace VectorProbe;

internal static unsafe class UnsupportedAlignedBoundary<TVector, T> where TVector : struct where T : unmanaged
{
    private delegate TVector LoadCall(T* source);
    private delegate void StoreCall(TVector value, T* destination);
    private static string effects;
    private static int acquisitions;

    // Cecil replaces these operands with the internal interface declarations.
    private static TVector LoadAligned(T* source) => throw new InvalidOperationException("fixture not rewritten");
    private static void StoreAligned(TVector value, T* destination) => throw new InvalidOperationException("fixture not rewritten");

    internal static void CopyAligned(T* destination, T* source) => StoreAligned(LoadAligned(source), destination);

    private static TVector Value(string mode)
    {
        effects += "V";
        if (mode == "value-throw") throw new ArgumentException("value");
        return default;
    }

    private static T* Pointer(T* pointer, string mode)
    {
        effects += "P";
        if (mode == "address-throw") throw new ArgumentException("address");
        if (mode is "array-null" or "array-bounds")
        {
            nint[] array = mode == "array-null" ? null : Array.Empty<nint>();
            return (T*)array[0];
        }
        return pointer;
    }

    private static void Acquired(bool valid)
    {
        if (!valid || effects != "") throw new InvalidOperationException("acquisition must precede argument evaluation");
        acquisitions++;
    }

    private static void Invoke(bool store, string route, string mode, T* pointer)
    {
        bool retain = mode == "retain";
        if (route == "direct")
        {
            if (store) StoreAligned(Value(mode), Pointer(pointer, mode));
            else _ = LoadAligned(Pointer(pointer, mode));
        }
        else if (route == "address")
        {
            if (store)
            {
                delegate*<TVector, T*, void> target = &StoreAligned;
                Acquired(target != null);
                if (!retain) target(Value(mode), Pointer(pointer, mode));
            }
            else
            {
                delegate*<T*, TVector> target = &LoadAligned;
                Acquired(target != null);
                if (!retain) _ = target(Pointer(pointer, mode));
            }
        }
        else if (route == "calli")
        {
            if (store) ((delegate*<TVector, T*, void>)&StoreAligned)(Value(mode), Pointer(pointer, mode));
            else _ = ((delegate*<T*, TVector>)&LoadAligned)(Pointer(pointer, mode));
        }
        else
        {
            if (store)
            {
                var target = new StoreCall(StoreAligned);
                Acquired(target is not null);
                if (!retain) target(Value(mode), Pointer(pointer, mode));
            }
            else
            {
                var target = new LoadCall(LoadAligned);
                Acquired(target is not null);
                if (!retain) _ = target(Pointer(pointer, mode));
            }
        }
    }

    private static string Probe(bool store, string route, string mode, byte* aligned)
    {
        effects = "";
        acquisitions = 0;
        T* pointer = (T*)(mode == "null" ? null : aligned + (mode == "misaligned" ? 1 : mode == "aligned16-not32" ? 16 : 0));
        Exception caught = null;
        try { Invoke(store, route, mode, pointer); }
        catch (Exception error) { caught = error; }
        string expectedEffects = mode == "retain" ? "" : mode == "value-throw" ? "V" : store ? "VP" : "P";
        if (effects != expectedEffects || acquisitions != (route is "address" or "delegate" ? 1 : 0))
            throw new InvalidOperationException(route + " " + mode + ": effects=" + effects + ", acquisitions=" + acquisitions, caught);
        if (mode == "retain")
        {
            if (caught is not null) throw caught;
            return "acquired";
        }
        if (caught is null) throw new InvalidOperationException("unsupported aligned operation returned");
        if (mode == "value-throw" && caught.Message != "value" || mode == "address-throw" && caught.Message != "address")
            throw new InvalidOperationException("incorrect caller exception");
        if (caught is PlatformNotSupportedException && !caught.Message.Contains("unsupported vector element"))
            throw caught;
        return caught.GetType().Name;
    }

    internal static void Run(string label, bool ordered)
    {
        byte* storage = stackalloc byte[192];
        byte* aligned = (byte*)(((nuint)storage + 63) & ~(nuint)63);
        for (int i = 0; i < 192; i++) storage[i] = 0x5a;
        foreach (bool store in new[] { false, true })
        {
            string operation = store ? "store" : "load";
            foreach (string route in UnsupportedAlignedBoundary.Routes)
            {
                if (!ordered && route is "address" or "calli") continue;
                foreach (string mode in UnsupportedAlignedBoundary.Modes)
                {
                    if (mode == "value-throw" && !store || mode == "retain" && route is "direct" or "calli") continue;
                    if (!ordered && mode is not ("aligned" or "misaligned" or "null" or "address-throw")) continue;
                    string result = Probe(store, route, mode, aligned);
                    for (int i = 0; i < 192; i++)
                        if (storage[i] != 0x5a) throw new InvalidOperationException("unsupported aligned operation wrote memory");
                    if (ordered) Console.WriteLine(label + " " + operation + " " + route + " " + mode + " " + result);
                    else if (result != (mode == "address-throw" ? "ArgumentException" : "PlatformNotSupportedException"))
                        throw new InvalidOperationException("unsupported aligned boundary: " + result);
                }
                if (!ordered) Console.WriteLine("unsupported aligned " + label + " " + (store ? 2 : 0) + " "
                    + (route == "delegate" ? "address" : "direct") + ": boundary, caller order, canary");
            }
        }
    }
}

internal static class UnsupportedAlignedBoundary
{
    internal static readonly string[] Routes = { "direct", "address", "calli", "delegate" };
    internal static readonly string[] Modes = { "retain", "aligned", "null", "misaligned", "aligned16-not32",
        "value-throw", "address-throw", "array-bounds", "array-null" };

    internal static void Run()
    {
        UnsupportedAlignedBoundary<Vector<char>, char>.Run("Vector<char>", false);
        UnsupportedAlignedBoundary<Vector512<decimal>, decimal>.Run("Vector512<decimal>", false);
        Console.WriteLine("unsupported aligned boundary end");
    }

    internal static void RunOrdered()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("unsupported aligned profile " + Vector<byte>.Count);
        UnsupportedAlignedBoundary<Vector<decimal>, decimal>.Run("Vector<decimal>", true);
        UnsupportedAlignedBoundary<Vector64<char>, char>.Run("Vector64<char>", true);
        UnsupportedAlignedBoundary<Vector64<Int128>, Int128>.Run("Vector64<Int128>", true);
        UnsupportedAlignedBoundary<Vector64<UInt128>, UInt128>.Run("Vector64<UInt128>", true);
        UnsupportedAlignedBoundary<Vector128<char>, char>.Run("Vector128<char>", true);
        UnsupportedAlignedBoundary<Vector128<Int128>, Int128>.Run("Vector128<Int128>", true);
        UnsupportedAlignedBoundary<Vector128<UInt128>, UInt128>.Run("Vector128<UInt128>", true);
        UnsupportedAlignedBoundary<Vector256<char>, char>.Run("Vector256<char>", true);
        UnsupportedAlignedBoundary<Vector256<Int128>, Int128>.Run("Vector256<Int128>", true);
        UnsupportedAlignedBoundary<Vector256<UInt128>, UInt128>.Run("Vector256<UInt128>", true);
        UnsupportedAlignedBoundary<Vector512<char>, char>.Run("Vector512<char>", true);
        UnsupportedAlignedBoundary<Vector512<Int128>, Int128>.Run("Vector512<Int128>", true);
        UnsupportedAlignedBoundary<Vector512<UInt128>, UInt128>.Run("Vector512<UInt128>", true);
        NumericControl();
        Console.WriteLine("unsupported aligned order end");
    }

    private static unsafe void NumericControl()
    {
        byte* buffer = stackalloc byte[64];
        int* source = (int*)(((nuint)buffer + 15) & ~(nuint)15);
        int* destination = source + 4;
        for (int i = 0; i < 4; i++)
        {
            source[i] = i + 1;
            destination[i] = 0;
        }
        UnsupportedAlignedBoundary<Vector128<int>, int>.CopyAligned(destination, source);
        for (int i = 0; i < 4; i++)
            if (destination[i] != source[i]) throw new InvalidOperationException("numeric aligned copy");
        for (int i = 0; i < 4; i++) source[i] = -1;
        try
        {
            UnsupportedAlignedBoundary<Vector128<int>, int>.CopyAligned((int*)((byte*)destination + 1), source);
            throw new InvalidOperationException("numeric misaligned store returned");
        }
        catch (AccessViolationException) { }
        for (int i = 0; i < 4; i++)
            if (destination[i] != i + 1) throw new InvalidOperationException("numeric misaligned store wrote memory");
        Console.WriteLine("numeric aligned memory passed");
    }
}
