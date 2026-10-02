using System.Runtime.InteropServices;
using UnrealSharp.Core;

namespace ManagedBaseline;

internal static class PendingShutdownProbe
{
#if DN2CPP
    private sealed class Payload
    {
        public int Value = 73;
    }

    private static readonly ManualResetEventSlim Started = new(false);
    private static readonly ManualResetEventSlim Release = new(false);
    private static Task? _worker;
    private static GCHandle _handle;
#endif
    private static string? _path;

    public static void Start()
    {
        _path = Environment.GetEnvironmentVariable("DN2CPP_SMOKE_PENDING_FILE");
        if (string.IsNullOrEmpty(_path)) return;
        if (!Path.IsPathFullyQualified(_path))
            throw new InvalidOperationException("Pending shutdown artifact path must be absolute");
#if !DN2CPP
        throw new NotSupportedException("Pending shutdown probe requires native module shutdown");
#else
        _handle = GCHandleUtilities.AllocateStrongPointer(new Payload(), typeof(PendingShutdownProbe).Assembly);
        _worker = Task.Run(() =>
        {
            File.AppendAllText(_path, "worker-started\n");
            Started.Set();
            if (!Release.Wait(120_000))
            {
                File.AppendAllText(_path, "worker-timeout\n");
                return;
            }
            // Leave work pending when the module returns to runtime quiescence.
            Thread.Sleep(100);
            GC.Collect();
            Payload? target = GCHandleUtilities.GetObjectFromHandlePtr<Payload>(GCHandle.ToIntPtr(_handle));
            File.AppendAllText(_path, target?.Value == 73 ? "worker-completed-handle-alive\n" : "worker-handle-lost\n");
        });
        if (!Started.Wait(5000))
            throw new TimeoutException("Pending worker did not start");
#endif
    }

    public static void Stop()
    {
        if (string.IsNullOrEmpty(_path)) return;
#if DN2CPP
        if (_worker is null || _worker.IsCompleted)
            throw new InvalidOperationException("Worker was not pending at module shutdown");
        File.AppendAllText(_path, "shutdown-worker-pending\n");
        Release.Set();
#endif
    }
}
