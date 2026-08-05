// Console-wasm smoke over the real CRI ADX LE archives, run under node.
//
// node has no AudioContext, so this program deliberately stays on the
// no-audio surface: nothing here calls CriAtomCSharp.Initialize (whose wasm
// arm routes to InitializeWEBAUDIO) or anything else that would touch Web
// Audio output. What IS exercised, all before any library initialization:
//   - criAtom_GetVersionString / criAtom_IsInitialized (plain P/Invoke in,
//     single-pointer-field NativeString back by value),
//   - the error-callback machinery end to end: a raw function-pointer
//     registration round-trip ([UnmanagedCallersOnly] reverse call from the
//     CRI engine), the binding's own CriErr.Callback object (whose
//     [UnmanagedCallersOnly] static lives in the referenced browser-flavor
//     CriWare.CriAtomLE.dll), errid -> message conversion, and the error
//     counters. The deterministic error source is CriAtomEx.UnregisterAcf()
//     before initialization — it raises through criErr and touches no audio.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if CRI_PRESENT
using CriWare;
using CriWare.InteropHelpers;
#endif

namespace CriWasmSmoke;

internal static class Program
{
#if CRI_PRESENT
    private static int _rawFired;
    private static string _rawErrid = "<none>";

    // Raw error-callback target. The engine calls (errid, p1, p2, parray)
    // Cdecl; IntPtr stands in for the binding's NativeString (one pointer
    // field, identical wasm-level signature), keeping this UCO primitive.
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void OnRawError(IntPtr errid, uint p1, uint p2, IntPtr parray)
    {
        _rawFired++;
        _rawErrid = Marshal.PtrToStringUTF8(errid) ?? "<null>";
    }

    private static unsafe void RegisterRaw()
    {
        CriErr.SetCallback(
            (delegate* unmanaged[Cdecl]<NativeString, uint, uint, IntPtr, void>)
            (delegate* unmanaged[Cdecl]<IntPtr, uint, uint, IntPtr, void>)&OnRawError);
    }

    private static unsafe void UnregisterRaw()
    {
        CriErr.SetCallback(default);
    }

    // Deterministic no-audio error source: registering a null ACF data block
    // is rejected with an error through the error-callback machinery — before
    // any initialization, touching no AudioContext.
    private static void Trigger()
    {
        CriAtomEx.RegisterAcfData(IntPtr.Zero, 0);
    }

    private static string Counts()
    {
        return $"err={CriErr.GetErrorCount(CriErr.Level.Error)} warn={CriErr.GetErrorCount(CriErr.Level.Warning)}";
    }

    private static void Run()
    {
        Console.WriteLine("== CRI wasm smoke (no-audio surface) ==");
        string version = CriAtom.GetVersionString();
        Console.WriteLine($"atom version: {version}");
        Console.WriteLine($"initialized: {CriAtom.IsInitialized()}");
        CriErr.SetErrorNotificationLevel(CriErr.NotificationLevel.All);
        Console.WriteLine($"counts before: {Counts()}");

        // Raw function-pointer registration round-trip.
        RegisterRaw();
        Trigger();
        Console.WriteLine($"raw fired: {_rawFired} errid: {_rawErrid}");
        UnregisterRaw();
        Trigger();
        Console.WriteLine($"raw fired after unregister: {_rawFired}");
        Console.WriteLine($"counts after raw phase: {Counts()}");

        // The binding's own callback object: registration runs through
        // NativeCallbackBase, the reverse call through the browser DLL's
        // [UnmanagedCallersOnly] static, and the errid through
        // criErr_ConvertIdToMessage.
        int objFired = 0;
        string objMessage = "<none>";
        Action<(NativeString errid, uint p1, uint p2, IntPtr parray)> handler = args =>
        {
            objFired++;
            objMessage = CriErr.ConvertIdToMessage(args.errid, args.p1, args.p2);
        };
        CriErr.Callback.Event += handler;
        Trigger();
        Console.WriteLine($"object fired: {objFired} message: {objMessage}");
        CriErr.Callback.Event -= handler;
        Trigger();
        Console.WriteLine($"object fired after remove: {objFired}");

        CriErr.ResetErrorCount(CriErr.Level.Error);
        CriErr.ResetErrorCount(CriErr.Level.Warning);
        Console.WriteLine($"counts after reset: {Counts()}");
        Console.WriteLine("done");
    }
#endif

    private static void Main()
    {
#if CRI_PRESENT
        Run();
#else
        Console.WriteLine("CRI ADX LE package absent - stub build");
#endif
    }
}
