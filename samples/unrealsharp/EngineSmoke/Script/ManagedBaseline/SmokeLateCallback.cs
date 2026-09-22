using System.Runtime.InteropServices;
using UnrealSharp.Attributes;
using UnrealSharp.Core;
using UnrealSharp;
using UnrealSharp.CoreUObject;
using UnrealSharp.Engine;

namespace ManagedBaseline;

[UClass]
public partial class AShutdownProbeActor : AActor
{
    [StructLayout(LayoutKind.Sequential)]
    private struct StorageEmbedding
    {
        public byte Prefix;
        public NativeStructHandleData Data;
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public int ProbeAbiShapes()
    {
        FMatrix identity = new(new FPlane { X = 1 }, new FPlane { Y = 1 },
            new FPlane { Z = 1 }, new FPlane { W = 1 });
        FRotator rotation = new(identity);
        int result = rotation.Pitch == 0 && rotation.Yaw == 0 && rotation.Roll == 0 ? 1 : 0;
        if (Marshal.SizeOf<NativeStructHandleData>() == 64 &&
            Marshal.OffsetOf<StorageEmbedding>(nameof(StorageEmbedding.Data)).ToInt64() == IntPtr.Size)
            result |= 2;
        using NativeStructHandle small = new(FRotator.GetNativeClassPtr());
        using NativeStructHandle large = new(FMatrix.GetNativeClassPtr());
        result |= 4;
        return result;
    }

    [UFunction(FunctionFlags.BlueprintCallable)]
    public long PrepareLateCallback()
    {
#if !DN2CPP
        throw new NotSupportedException("Late callback probe requires native shutdown");
#else
        string path = Environment.GetEnvironmentVariable("DN2CPP_SMOKE_LATE_CALLBACK_FILE")
            ?? throw new InvalidOperationException("Missing late callback result path");
        if (!Path.IsPathFullyQualified(path))
            throw new InvalidOperationException("Late callback artifact path must be absolute");
        int invocations = 0;
        Action callback = () => File.AppendAllText(path,
            Interlocked.Increment(ref invocations) == 1 ? "managed-control-entry\n" : "managed-late-entry\n");
        GCHandle handle = GCHandleUtilities.AllocateStrongPointer(callback, typeof(AShutdownProbeActor).Assembly);
        return GCHandle.ToIntPtr(handle).ToInt64();
#endif
    }
}
