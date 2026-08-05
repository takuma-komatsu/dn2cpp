using System;
using System.Runtime.InteropServices;

// Aligned raw native (GC-unmanaged) heap allocation.
// NativeMemory.{AlignedAlloc,AlignedFree,AlignedRealloc} — the native
// heap, but aligned to a power-of-two boundary. Each lowers to the new
// dn2cpp_native_aligned_* C wrappers (std::aligned_alloc with byteCount rounded
// up to a multiple of the alignment, plain free, and aligned_alloc+memcpy+free
// for realloc).
//
// The test is fully deterministic and exercises the wrappers without printing
// raw pointer values: a byteCount that is NOT a multiple of the alignment must
// be rounded up (an un-rounded size makes std::aligned_alloc fail -> the program
// would abort, so the rounding is covered), and a full-buffer write/read over
// the requested region catches any under-allocation. Covers a non-multiple
// byteCount at two alignments, realloc-grows-and-preserves, and the zero-
// byteCount valid-pointer case. (Asserting `(nuint)p % alignment == 0` directly
// needs pointer-as-integer arithmetic.) CoreLib
// only (no Linq shim). Diffed exact vs real .NET. Allocation-wise this section
// is entirely off the managed heap (std::aligned_alloc / free), so it neither
// perturbs nor is perturbed by the bucket's GC-sensitive sections.
namespace AlignedAllocSubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        // AlignedAlloc(byteCount, alignment): 60 is not a multiple of 16, so the
        // runtime rounds it up to 64. Write/read the full requested 60 bytes.
        byte* b = (byte*)NativeMemory.AlignedAlloc(60, 16);
        for (int i = 0; i < 60; i++)
            b[i] = (byte)(i & 0xFF);
        int sum = 0;
        for (int i = 0; i < 60; i++)
            sum += b[i];
        Console.WriteLine(sum);                          // 1770
        NativeMemory.AlignedFree(b);

        // Stronger alignment (64) with a non-multiple byteCount (100 -> 128). Use
        // 25 ints (100 bytes) of the usable region.
        int* ip = (int*)NativeMemory.AlignedAlloc(100, 64);
        for (int i = 0; i < 25; i++)
            ip[i] = (i + 1) * 4;
        long isum = 0;
        for (int i = 0; i < 25; i++)
            isum += ip[i];
        Console.WriteLine(isum);                          // 1300
        NativeMemory.AlignedFree(ip);

        // AlignedRealloc: grow, preserve the original bytes, keep the alignment.
        int* rp = (int*)NativeMemory.AlignedAlloc(32, 32);   // 8 ints
        for (int i = 0; i < 8; i++)
            rp[i] = (i + 1) * 10;                            // 10..80
        rp = (int*)NativeMemory.AlignedRealloc(rp, 64, 32);  // 16 ints
        long preserved = 0;
        for (int i = 0; i < 8; i++)
            preserved += rp[i];
        Console.WriteLine(preserved);                     // 360 (original kept)
        for (int i = 8; i < 16; i++)
            rp[i] = (i + 1) * 10;                          // 90..160
        long all = 0;
        for (int i = 0; i < 16; i++)
            all += rp[i];
        Console.WriteLine(all);                           // 1360
        NativeMemory.AlignedFree(rp);

        // AlignedAlloc(0, alignment) still returns a valid pointer (raw pointers
        // can be compared against null).
        void* z = NativeMemory.AlignedAlloc(0, 16);
        Console.WriteLine(z != null);                     // True
        NativeMemory.AlignedFree(z);

        // A bad alignment is a CATCHABLE ArgumentException, not an abort: alignment
        // 0, 3 and 24 each give System.ArgumentException with no ParamName, while 1
        // and a zero byteCount are accepted — so the loop below has to survive the
        // bad rows AND keep allocating on the good ones, which is the half an abort
        // could never have. Type name only; the message is localized.
        //
        // The alignment values are read out of a table rather than written as
        // constants at the call: a caller that sizes its alignment from
        // configuration is the case this exists for, and a constant would let the
        // C# compiler or the transpiler fold the branch away.
        nuint[] alignments = { 0, 1, 3, 16, 24, 64 };
        int ok = 0, rejected = 0;
        foreach (nuint a in alignments)
        {
            try
            {
                void* p = NativeMemory.AlignedAlloc(48, a);
                if (p != null) ok++;
                NativeMemory.AlignedFree(p);
            }
            catch (ArgumentException)
            {
                rejected++;
            }
        }
        Console.WriteLine("aligned alloc: ok=" + ok + " rejected=" + rejected);   // ok=3 rejected=3

        // AlignedRealloc validates the same way. On POSIX it reaches the check by
        // delegating to AlignedAlloc; on Windows the _aligned_realloc arm carries
        // its own copy of it. A null first argument degenerates to a plain alloc,
        // so this probes the validation without needing a live block.
        try
        {
            void* bad = NativeMemory.AlignedRealloc(null, 16, 3);
            Console.WriteLine("unreachable " + (bad != null));
        }
        catch (ArgumentException)
        {
            Console.WriteLine("aligned realloc: ArgumentException");
        }
    }
}
