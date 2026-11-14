using System;
using System.Threading;

namespace WaitHandleRawWaitSubset
{
    // Public waits on events agree with .NET. SynchronizationContext.Wait reaches the
    // CoreCLR WaitHandle QCall, which natively refuses at the call rather than binding.
    internal static class Program
    {
        public static void Run()
        {
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
            try
            {
                new SynchronizationContext().Wait(null!, false, 0);
                Console.WriteLine("raw-wait-null-array=none");
            }
            catch (ArgumentNullException error)
            {
                Console.WriteLine("raw-wait-null-array=ArgumentNullException/" + error.ParamName);
            }
            Console.WriteLine("raw-wait existing event APIs end");
        }

        // Native only: the CLR performs the wait.
        public static void RunNativeBoundary()
        {
            using var first = new ManualResetEvent(true);
            using var second = new ManualResetEvent(true);
            nint[] handles = { first.SafeWaitHandle.DangerousGetHandle(), second.SafeWaitHandle.DangerousGetHandle() };
            try
            {
                new SynchronizationContext().Wait(handles, true, 17);
                Console.WriteLine("raw-wait-public-wait=returned");
            }
            catch (PlatformNotSupportedException error)
            {
                Console.WriteLine("raw-wait-public-wait=PlatformNotSupportedException:"
                    + error.Message.Contains("QCall!WaitHandle_WaitMultipleIgnoringSyncContext") + ":"
                    + error.Message.Contains("requires the CoreCLR runtime"));
            }
        }
    }
}
