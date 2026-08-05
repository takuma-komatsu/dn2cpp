#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// `#nullable enable` is file-local; the bucket's csproj disables it.
namespace MemoryMarshalSubset;

// MemoryMarshal over byte spans. GetReference / Cast / AsBytes / Read / Write /
// CreateSpan / CreateReadOnlySpan are transpiler intrinsics — the real BCL bodies
// use JIT intrinsics dn2cpp does not model — while TryRead / TryWrite / AsRef
// transpile from the real CoreLib IL. Reinterpretation results are host-endian,
// which is exact only because the gate diffs against .NET on the same machine.
internal static class Program
{
    internal static void __GateEntry()
    {
        // --- GetReference: a writable ref to element 0 ---
        int[] arr = { 10, 20, 30 };
        ref int r0 = ref MemoryMarshal.GetReference(arr.AsSpan());
        r0 = 99;
        Console.WriteLine(arr[0]);                  // 99

        // --- Cast<int,byte>: narrow reinterpret (length grows) ---
        int[] data = { 0x04030201 };
        Span<byte> bytes = MemoryMarshal.Cast<int, byte>(data.AsSpan());
        Console.WriteLine(bytes.Length);            // 4
        Console.WriteLine($"{bytes[0]},{bytes[1]},{bytes[2]},{bytes[3]}");

        // --- AsBytes: whole span as raw bytes ---
        Span<int> two = new int[] { 0x01020304, 0x05060708 }.AsSpan();
        Span<byte> ab = MemoryMarshal.AsBytes(two);
        Console.WriteLine(ab.Length);               // 8

        // --- Cast<byte,int>: widen reinterpret (length shrinks), ReadOnlySpan overload ---
        byte[] raw = { 1, 0, 0, 0, 2, 0, 0, 0 };
        ReadOnlySpan<byte> ros = raw;
        ReadOnlySpan<int> ri = MemoryMarshal.Cast<byte, int>(ros);
        Console.WriteLine($"{ri.Length}:{ri[0]},{ri[1]}");

        // --- Read<T> / Write<T> over a byte span ---
        byte[] buf = new byte[8];
        Span<byte> bs = buf;
        MemoryMarshal.Write(bs, 0x11223344);
        Console.WriteLine(MemoryMarshal.Read<int>(bs));   // 287454020

        MemoryMarshal.Write(bs, 9876543210L);
        Console.WriteLine(MemoryMarshal.Read<long>(bs));  // 9876543210

        // --- CreateSpan / CreateReadOnlySpan over a single ref ---
        int single = 777;
        Span<int> cs = MemoryMarshal.CreateSpan(ref single, 1);
        cs[0] = 555;
        Console.WriteLine(single);                  // 555

        ReadOnlySpan<int> cros = MemoryMarshal.CreateReadOnlySpan(ref single, 1);
        Console.WriteLine($"{cros.Length}:{cros[0]}");    // 1:555

        // --- TryRead<T>: success, then false on a too-short buffer ---
        MemoryMarshal.Write(bs, 0x0A0B0C0D);
        Console.WriteLine(MemoryMarshal.TryRead(bs, out int tr));         // True
        Console.WriteLine(tr);                            // 168496141
        int trShort = -1;
        Console.WriteLine(MemoryMarshal.TryRead(bs.Slice(0, 2), out trShort)); // False
        Console.WriteLine(trShort);                       // 0 (out zeroed on failure)

        // --- TryWrite<T>: success (readable back), then false on a too-short buffer ---
        Console.WriteLine(MemoryMarshal.TryWrite(bs, 0x21436587));        // True
        Console.WriteLine(MemoryMarshal.Read<int>(bs));   // 558065031
        Console.WriteLine(MemoryMarshal.TryWrite(bs.Slice(0, 3), 0x21436587)); // False
        Console.WriteLine(MemoryMarshal.Read<int>(bs));   // 558065031 (untouched)

        // --- AsRef<T>(Span<byte>): a writable ref that hits the original buffer ---
        byte[] ar = new byte[8];
        Span<byte> ars = ar;
        ref long lr = ref MemoryMarshal.AsRef<long>(ars);
        lr = 0x0102030405060708L;
        Console.WriteLine($"{ar[0]},{ar[7]}");            // host-endian: 8,1 on LE

        // --- AsRef<T>(ReadOnlySpan<byte>): read-only view of the same bytes ---
        ReadOnlySpan<byte> arr8 = ar;
        ref readonly long lro = ref MemoryMarshal.AsRef<long>(arr8);
        Console.WriteLine(lro);                           // 72623859790382856

        // --- GetArrayDataReference(Array): the non-generic, intrinsic overload.
        // Returns ref byte to element 0 whatever the element type, checked against
        // the generic form with Unsafe.AreSame. SZ arrays only. A null array
        // diverges (real .NET throws NRE, the helper returns a null ref) and is
        // deliberately not exercised. ---
        byte[] gab = { 0xAA, 0xBB, 0xCC };
        ref byte gabRef = ref MemoryMarshal.GetArrayDataReference((Array)gab);
        Console.WriteLine(gabRef);                        // 170
        Console.WriteLine(Unsafe.AreSame(ref gabRef, ref MemoryMarshal.GetArrayDataReference(gab))); // True

        int[] gai = { 0x11223344, 55 };
        ref byte gaiRef = ref MemoryMarshal.GetArrayDataReference((Array)gai);
        Console.WriteLine(Unsafe.ReadUnaligned<int>(ref gaiRef));   // 287454020
        Unsafe.WriteUnaligned(ref gaiRef, 0x0F0E0D0C);
        Console.WriteLine(gai[0]);                        // 252579084 (write lands in the array)
        Console.WriteLine(gai[1]);                        // 55 (neighbor untouched)

        string[] gas = { "alpha", "beta" };
        ref byte gasRef = ref MemoryMarshal.GetArrayDataReference((Array)gas);
        Console.WriteLine(Unsafe.As<byte, string>(ref gasRef));     // alpha
        Console.WriteLine(Unsafe.AreSame(
            ref Unsafe.As<string, byte>(ref MemoryMarshal.GetArrayDataReference(gas)),
            ref gasRef));                                 // True
    }
}
