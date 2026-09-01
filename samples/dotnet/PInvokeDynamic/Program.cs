using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using PInvokeRefLib;

namespace PInvokeDynamic;

internal static class Program
{
    private const int ConcurrentCallers = 8;
    private const int WaitTimeoutMilliseconds = 10_000;

    private static readonly Barrier s_firstResolverWave = new(ConcurrentCallers);
    private static int s_resolverCalls;
    private static int s_multicastObserverCalls;
    private static int s_wrongAssembly;
    private static int s_wrongMethodSearchPath;
    private static int s_wrongAssemblySearchPath;
    private static int s_observedAssemblySearchPath;
    private static int s_wrongAssemblyDirectorySearchPath;
    private static int s_observedAssemblyDirectorySearchPath;
    private static int s_wrongSystem32SearchPath;
    private static int s_observedSystem32SearchPath;
    private static int s_barrierTimedOut;

    [DllImport("dn2cpp_absent_library")]
    private static extern int MissingLibrary();

    [DllImport("dn2cpptest", EntryPoint = "dn2cpptest_absent_export")]
    private static extern int MissingExport();

    private static IntPtr Resolve(string libraryName, Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        Interlocked.Increment(ref s_resolverCalls);
        if (assembly != typeof(NativeBridge).Assembly)
            Volatile.Write(ref s_wrongAssembly, 1);
        if (libraryName == "logical_dn2cpptest")
        {
            if (searchPath != DllImportSearchPath.AssemblyDirectory)
                Volatile.Write(ref s_wrongMethodSearchPath, 1);
            // Keep every first-wave callback in flight until all callers have entered.
            // CoreCLR permits each unresolved caller to run the resolver; this makes the
            // exact count deterministic while a later call proves the published cache.
            if (!s_firstResolverWave.SignalAndWait(WaitTimeoutMilliseconds))
                Volatile.Write(ref s_barrierTimedOut, 1);
            return NativeLibrary.Load(
                Environment.GetEnvironmentVariable("DN2CPP_PINVOKE_TEST_LIBRARY")!);
        }
        if (libraryName == "dn2cpptest_assembly")
        {
            Volatile.Write(ref s_observedAssemblyDirectorySearchPath, 1);
            if (searchPath != DllImportSearchPath.AssemblyDirectory)
                Volatile.Write(ref s_wrongAssemblyDirectorySearchPath, 1);
            return IntPtr.Zero;
        }
        if (libraryName == "kernel32.dll")
        {
            Volatile.Write(ref s_observedSystem32SearchPath, 1);
            if (searchPath != DllImportSearchPath.System32)
                Volatile.Write(ref s_wrongSystem32SearchPath, 1);
            return IntPtr.Zero;
        }
        if (searchPath != DllImportSearchPath.LegacyBehavior)
            Volatile.Write(ref s_wrongAssemblySearchPath, 1);
        if (libraryName == "resolver_throw")
        {
            Volatile.Write(ref s_observedAssemblySearchPath, 1);
            throw new ApplicationException("resolver exception");
        }
        return IntPtr.Zero;
    }

    private static IntPtr ObserveResolver(string libraryName, Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        Interlocked.Increment(ref s_multicastObserverCalls);
        return IntPtr.Zero;
    }

    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        NativeBridge.ReachDarwinSelectorProbe();

        try { NativeLibrary.SetDllImportResolver(null!, Resolve); }
        catch (ArgumentNullException) { Console.WriteLine("null assembly"); }
        try { NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, null!); }
        catch (ArgumentNullException) { Console.WriteLine("null resolver"); }

        DllImportResolver resolver = ObserveResolver;
        resolver += Resolve;
        NativeLibrary.SetDllImportResolver(typeof(NativeBridge).Assembly, resolver);
        resolver = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var threads = new Thread[ConcurrentCallers];
        var redirected = new int[threads.Length];
        for (int i = 0; i < threads.Length; i++)
        {
            int slot = i;
            threads[i] = new Thread(() => redirected[slot] = NativeBridge.RedirectedAdd(slot, 10))
            {
                IsBackground = true,
            };
        }
        foreach (Thread thread in threads) thread.Start();
        bool firstWaveCompleted = true;
        foreach (Thread thread in threads)
            firstWaveCompleted &= thread.Join(WaitTimeoutMilliseconds);
        firstWaveCompleted &= Volatile.Read(ref s_barrierTimedOut) == 0;
        Console.WriteLine("concurrent first wave completed: " + firstWaveCompleted);
        if (!firstWaveCompleted)
            return;
        Console.WriteLine(string.Join(',', redirected));
        Console.WriteLine("resolver first-wave count: " + Volatile.Read(ref s_resolverCalls));
        Console.WriteLine("cached redirected call: " + NativeBridge.RedirectedAdd(20, 22));
        Console.WriteLine("first method-group call: " + NativeBridge.RedirectedAddViaDelegate(18, 24));
        Console.WriteLine("resolver after method group: " + Volatile.Read(ref s_resolverCalls));
        Console.WriteLine("cached method-group call: " + NativeBridge.RedirectedAddViaDelegate(17, 25));
        Console.WriteLine("second managed call site: " + NativeBridge.RedirectedAddAgain(19, 23));
        Console.WriteLine("resolver final count: " + Volatile.Read(ref s_resolverCalls));
        Console.WriteLine("multicast observer count: "
            + Volatile.Read(ref s_multicastObserverCalls));
        Console.WriteLine("resolver assembly identity: "
            + (Volatile.Read(ref s_wrongAssembly) == 0));
        Console.WriteLine("resolver method search path: "
            + (Volatile.Read(ref s_wrongMethodSearchPath) == 0));

        Console.WriteLine("assembly-directory fallback: "
            + NativeBridge.AssemblyDirectoryAdd(20, 22));
        Console.WriteLine("assembly-directory search path: "
            + (Volatile.Read(ref s_observedAssemblyDirectorySearchPath) != 0
                && Volatile.Read(ref s_wrongAssemblyDirectorySearchPath) == 0));
        Console.WriteLine("system32 fallback: " + NativeBridge.System32Probe());
        Console.WriteLine("system32 search path: "
            + (!OperatingSystem.IsWindows()
                || (Volatile.Read(ref s_observedSystem32SearchPath) != 0
                    && Volatile.Read(ref s_wrongSystem32SearchPath) == 0)));

        // The registered resolver returns zero for this logical name, so binding
        // falls through to the platform loader. Distinct entry points prove that
        // caches belong to imported methods rather than modules.
        Console.WriteLine(NativeBridge.Add(20, 22));
        Console.WriteLine(NativeBridge.Mul(6, 7));

        try
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeBridge).Assembly, Resolve);
            Console.WriteLine("duplicate missed");
        }
        catch (InvalidOperationException) { Console.WriteLine("duplicate resolver"); }

        try
        {
            NativeBridge.ResolverThrows();
            Console.WriteLine("resolver throw missed");
        }
        catch (ApplicationException e) { Console.WriteLine(e.Message); }
        Console.WriteLine("resolver assembly search path: "
            + (Volatile.Read(ref s_observedAssemblySearchPath) != 0
                && Volatile.Read(ref s_wrongAssemblySearchPath) == 0));

        string absolutePath = Environment.GetEnvironmentVariable("DN2CPP_PINVOKE_TEST_LIBRARY")!;
        Console.WriteLine(NativeLibrary.TryLoad(absolutePath, out IntPtr pathHandle));
        Console.WriteLine(NativeLibrary.TryGetExport(pathHandle, "dn2cpptest_add", out IntPtr export)
            && export != IntPtr.Zero);
        Console.WriteLine(!NativeLibrary.TryGetExport(pathHandle, "dn2cpptest_absent", out _));
        try { NativeLibrary.GetExport(pathHandle, "dn2cpptest_absent"); }
        catch (EntryPointNotFoundException e)
        {
            Console.WriteLine("get export missing symbol: "
                + e.Message.Contains("dn2cpptest_absent", StringComparison.Ordinal));
        }
        try { NativeLibrary.TryGetExport(IntPtr.Zero, "dn2cpptest_add", out _); }
        catch (ArgumentNullException) { Console.WriteLine("try export null handle"); }
        try { NativeLibrary.GetExport(IntPtr.Zero, "dn2cpptest_add"); }
        catch (ArgumentNullException) { Console.WriteLine("get export null handle"); }
        try { NativeLibrary.TryGetExport(pathHandle, null!, out _); }
        catch (ArgumentNullException) { Console.WriteLine("try export null name"); }
        try { NativeLibrary.GetExport(pathHandle, null!); }
        catch (ArgumentNullException) { Console.WriteLine("get export null name"); }
        NativeLibrary.Free(pathHandle);
        NativeLibrary.Free(IntPtr.Zero);
        Console.WriteLine("free null handle");

        IntPtr absoluteHandle = NativeLibrary.Load(absolutePath);
        Console.WriteLine(NativeLibrary.TryGetExport(
            absoluteHandle, "dn2cpptest_add", out IntPtr absoluteExport)
            && absoluteExport != IntPtr.Zero);
        NativeLibrary.Free(absoluteHandle);

        Console.WriteLine(NativeLibrary.TryLoad("dn2cpptest", typeof(Program).Assembly,
            null, out IntPtr nameHandle));
        Console.WriteLine(NativeLibrary.TryGetExport(
            nameHandle, "dn2cpptest_add", out IntPtr nameExport)
            && nameExport != IntPtr.Zero);
        NativeLibrary.Free(nameHandle);

        IntPtr assemblyDirectoryHandle = NativeLibrary.Load("dn2cpptest_assembly",
            typeof(Program).Assembly, DllImportSearchPath.AssemblyDirectory);
        Console.WriteLine(NativeLibrary.TryGetExport(
            assemblyDirectoryHandle, "dn2cpptest_add", out IntPtr assemblyDirectoryExport)
            && assemblyDirectoryExport != IntPtr.Zero);
        NativeLibrary.Free(assemblyDirectoryHandle);

        try { NativeLibrary.TryLoad("dn2cpptest", null!, null, out _); }
        catch (ArgumentNullException) { Console.WriteLine("try load null assembly"); }
        try { NativeLibrary.Load("dn2cpptest", null!, null); }
        catch (ArgumentNullException) { Console.WriteLine("load null assembly"); }

        Console.WriteLine(!NativeLibrary.TryLoad("", out _));
        try { NativeLibrary.Load(""); }
        catch (DllNotFoundException) { Console.WriteLine("empty path missing"); }

        try { MissingLibrary(); }
        catch (DllNotFoundException) { Console.WriteLine("dll not found"); }
        try { MissingExport(); }
        catch (EntryPointNotFoundException) { Console.WriteLine("entry point not found"); }
    }
}
