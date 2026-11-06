using System;
using System.Globalization;
using System.Threading;

namespace Dn2Cpp.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class NativeImplementationAttribute : Attribute
    {
        public NativeImplementationAttribute(string module, string entryPoint) { }
    }
}

namespace WaitHandleRawWaitRoutes
{
    internal static unsafe class Program
    {
        // The fixture binds these to the input CoreLib. Rewritten raw calls run only natively.
        public static int Direct3(ReadOnlySpan<nint> handles, bool all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int Core3(ReadOnlySpan<nint> handles, bool all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int Direct4(ReadOnlySpan<nint> handles, int count, bool all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int Pointer3(ReadOnlySpan<nint> handles, bool all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int PointerCore3(ReadOnlySpan<nint> handles, bool all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int Pointer4(ReadOnlySpan<nint> handles, int count, bool all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int ImportDirect(nint* handles, int count, int all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int ImportPointer(nint* handles, int count, int all, int timeout) => throw new InvalidOperationException("fixture missing");
        public static int Managed(nint* handles, int count, int all, int timeout) => handles != null ? count * 1000 + all * 100 + timeout : -1;

        private static void PinCulture()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        public static void RunPublicChecks(string[] args)
        {
            PinCulture();
            Console.WriteLine("== raw-wait existing event APIs ==");
            using var first = new ManualResetEvent(false);
            using var second = new ManualResetEvent(true);
            using var auto = new AutoResetEvent(true);
            Console.WriteLine("event-any=" + WaitHandle.WaitAny(new WaitHandle[] { first, second }));
            first.Set();
            Console.WriteLine("event-priority=" + WaitHandle.WaitAny(new WaitHandle[] { first, second }));
            Console.WriteLine("event-auto=" + auto.WaitOne(0) + "/" + auto.WaitOne(0));
            first.Reset();
            Console.WriteLine("event-reset=" + first.WaitOne(0));
            Console.WriteLine("raw-wait existing event APIs end");
        }

        private static int Invoke(int route, nint[] handles)
        {
            switch (route)
            {
                case 0: return Direct3(handles, true, 17);
                case 1: return Core3(handles, true, 17);
                case 2: return Direct4(handles, 1, true, 17);
                case 3: return Pointer3(handles, true, 17);
                case 4: return PointerCore3(handles, true, 17);
                case 5: return Pointer4(handles, 1, true, 17);
                case 6: fixed (nint* p = handles) return ImportDirect(p, 2, 5, 17);
                case 7: fixed (nint* p = handles) return ImportPointer(p, 2, 5, 17);
                default: return new SynchronizationContext().Wait(handles, true, 17);
            }
        }

        public static void RunArtifactChecks(string[] args)
        {
            PinCulture();
            RunPublicChecks(args);
            bool binding = args.Length != 0 && args[0] == "managed-binding";
            using var first = new ManualResetEvent(true);
            using var second = new ManualResetEvent(true);
            nint[] handles = { first.SafeWaitHandle.DangerousGetHandle(), second.SafeWaitHandle.DangerousGetHandle() };
            string[] names = { "direct3", "core3", "direct4", "pointer3", "pointer-core3", "pointer4", "import-direct", "import-pointer", "public-wait" };
            for (int route = 0; route < names.Length; route++)
            {
                if (binding)
                {
                    int expected = route is 2 or 5 ? 1117 : route is 6 or 7 ? 2517 : 2117;
                    int actual = Invoke(route, handles);
                    if (actual != expected) throw new InvalidOperationException(names[route] + " native binding");
                    Console.WriteLine("raw-wait-" + names[route] + ":managed-binding:" + actual);
                }
                else
                {
                    bool refused = false;
                    try { Invoke(route, handles); }
                    catch (PlatformNotSupportedException error)
                    {
                        refused = error.Message.Contains("QCall!WaitHandle_WaitMultipleIgnoringSyncContext")
                            && error.Message.Contains("requires the CoreCLR runtime");
                    }
                    if (!refused) throw new InvalidOperationException(names[route] + " existing QCall boundary");
                    Console.WriteLine("raw-wait-" + names[route] + ":PlatformNotSupportedException:True");
                }
            }
            bool nullChecked = false;
            try { new SynchronizationContext().Wait(null!, false, 0); }
            catch (ArgumentNullException error) { nullChecked = error.ParamName == "waitHandles"; }
            if (!nullChecked) throw new InvalidOperationException("WaitHelper null validation");
            Console.WriteLine("raw-wait-null-array=ArgumentNullException/waitHandles");
            Console.WriteLine("raw-wait existing QCall boundary end");
        }
    }
}
