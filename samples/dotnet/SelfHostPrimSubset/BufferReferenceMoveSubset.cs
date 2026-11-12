using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BufferReferenceMoveSubset;

internal static class Program
{
    private sealed class Payload
    {
        internal readonly int Value;
        internal Payload(int value) { Value = value; }
    }

    // Standalone CLR runs copy whole reference slots. The gate replaces these
    // bodies with calls to the private CoreLib byte-count leaves on both sides.
    private static void MoveInternal(ref byte destination, ref byte source, nuint bytes)
    {
        int count = checked((int)(bytes / (nuint)IntPtr.Size));
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, object>(ref source), count)
            .CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.As<byte, object>(ref destination), count));
    }
    private static void MoveBatch(ref byte destination, ref byte source, nuint bytes)
        => MoveInternal(ref destination, ref source, bytes);

    private static unsafe void Move(object[] array, int destination, int source, int count, bool batch)
    {
        ref byte dst = ref Unsafe.As<object, byte>(ref array[destination]);
        ref byte src = ref Unsafe.As<object, byte>(ref array[source]);
        nuint bytes = (nuint)count * (nuint)sizeof(nint);
        if (batch) MoveBatch(ref dst, ref src, bytes);
        else MoveInternal(ref dst, ref src, bytes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object[] References(bool batch)
    {
        int length = batch ? 8193 : 2;
        var source = new object[length];
        source[0] = new Payload(41);
        source[length - 1] = new Payload(73);
        var destination = new object[length];
        ref byte dst = ref Unsafe.As<object, byte>(ref destination[0]);
        ref byte src = ref Unsafe.As<object, byte>(ref source[0]);
        unsafe
        {
            nuint bytes = (nuint)(length * sizeof(nint));
            if (batch) MoveBatch(ref dst, ref src, bytes);
            else MoveInternal(ref dst, ref src, bytes);
        }
        Array.Clear(source);
        return destination;
    }

    internal static void Run()
    {
        Console.WriteLine("== Buffer reference moves ==");
        foreach (bool batch in new[] { false, true })
        {
            string label = batch ? "batch" : "internal";
            // The private Batch entry assumes more than one 16-KiB chunk.
            // Keep that precondition on both 32-bit and 64-bit hosts.
            int count = batch ? 8193 : 3;
            var original = new object[count + 1];
            for (int i = 0; i < original.Length; i++) original[i] = new Payload(i);
            var values = (object[])original.Clone();
            Move(values, 1, 0, count, batch);
            bool right = ReferenceEquals(values[0], original[0]);
            for (int i = 1; i < values.Length; i++) right &= ReferenceEquals(values[i], original[i - 1]);
            Console.WriteLine("buffer-" + label + "-overlap-right:" + right);
            Move(values, 0, 1, count, batch);
            bool left = ReferenceEquals(values[count], original[count - 1]);
            for (int i = 0; i < count; i++) left &= ReferenceEquals(values[i], original[i]);
            Console.WriteLine("buffer-" + label + "-overlap-left:" + left);
            if (!batch) Move(values, count, 0, 0, false);
            Move(values, 0, 0, values.Length, batch);
            bool same = ReferenceEquals(values[count], original[count - 1]);
            for (int i = 0; i < count; i++) same &= ReferenceEquals(values[i], original[i]);
            Console.WriteLine("buffer-" + label + "-empty-self:" + same);
            object[] moved = References(batch);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            Console.WriteLine("buffer-" + label + "-references:"
                + (((Payload)moved[0]).Value == 41 && ((Payload)moved[moved.Length - 1]).Value == 73));
            GC.KeepAlive(moved);
        }
        Console.WriteLine("Buffer reference moves end");
    }
}
