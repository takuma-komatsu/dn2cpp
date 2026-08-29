using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PlatformIsaProbe;

// One representative instruction per X86 family, and the exercise of every
// Lowered family. A probe is called only when the family reports unsupported,
// so its value is discarded: the section's text must not change when a family
// becomes supported. An exercise calls every method of the family and of its
// nested type (behind the nested IsSupported), printing fixed-input results
// beside a portable reference; nothing machine-dependent is printed.
internal static class X86Sections
{
    internal static void RegisterExercises(Dictionary<string, Action> exercises)
    {
        exercises["X86.X86Base"] = X86BaseExercise;
        exercises["X86.Lzcnt"] = LzcntExercise;
        exercises["X86.Popcnt"] = PopcntExercise;
        exercises["X86.Bmi1"] = Bmi1Exercise;
        exercises["X86.Bmi2"] = Bmi2Exercise;
        exercises["X86.X86Serialize"] = X86SerializeExercise;
        Exercises.RegisterX86(exercises);
    }

    internal static void ProbeX86Base() { _ = X86Base.CpuId(0, 0); }
    internal static void ProbeLzcnt() { _ = Lzcnt.LeadingZeroCount(1u); }
    internal static void ProbePopcnt() { _ = Popcnt.PopCount(1u); }
    internal static void ProbeBmi1() { _ = Bmi1.TrailingZeroCount(1u); }
    internal static void ProbeBmi2() { _ = Bmi2.ZeroHighBits(1u, 1u); }
    internal static void ProbeX86Serialize() { X86Serialize.Serialize(); }
    internal static void ProbeSse() { _ = Sse.Add(Vector128<float>.Zero, Vector128<float>.Zero); }
    internal static void ProbeSse2() { _ = Sse2.Add(Vector128<int>.Zero, Vector128<int>.Zero); }
    internal static void ProbeSse3() { _ = Sse3.HorizontalAdd(Vector128<float>.Zero, Vector128<float>.Zero); }
    internal static void ProbeSsse3() { _ = Ssse3.Abs(Vector128<short>.Zero); }
    internal static void ProbeSse41() { _ = Sse41.Ceiling(Vector128<float>.Zero); }
    internal static void ProbeSse42() { _ = Sse42.Crc32(0u, (byte)0); }
    internal static void ProbePclmulqdq() { _ = Pclmulqdq.CarrylessMultiply(Vector128<long>.Zero, Vector128<long>.Zero, 0); }
    internal static void ProbeAes() { _ = Aes.Encrypt(Vector128<byte>.Zero, Vector128<byte>.Zero); }
    internal static void ProbeAvx() { _ = Avx.Add(Vector256<float>.Zero, Vector256<float>.Zero); }
    internal static void ProbeAvx2() { _ = Avx2.Abs(Vector256<int>.Zero); }
    internal static void ProbeFma() { _ = Fma.MultiplyAdd(Vector128<float>.Zero, Vector128<float>.Zero, Vector128<float>.Zero); }
    internal static void ProbeAvxVnni() { _ = AvxVnni.MultiplyWideningAndAdd(Vector128<int>.Zero, Vector128<byte>.Zero, Vector128<sbyte>.Zero); }
    internal static void ProbeAvx512F() { _ = Avx512F.Add(Vector512<int>.Zero, Vector512<int>.Zero); }
    internal static void ProbeAvx512BW() { _ = Avx512BW.Abs(Vector512<short>.Zero); }
    internal static void ProbeAvx512CD() { _ = Avx512CD.DetectConflicts(Vector512<int>.Zero); }
    internal static void ProbeAvx512DQ() { _ = Avx512DQ.BroadcastPairScalarToVector512(Vector128<int>.Zero); }
    internal static void ProbeAvx512Vbmi() { _ = Avx512Vbmi.PermuteVar64x8(Vector512<byte>.Zero, Vector512<byte>.Zero); }
    internal static void ProbeAvx512Vbmi2() { _ = Avx512Vbmi2.Compress(Vector512<byte>.Zero, Vector512<byte>.Zero, Vector512<byte>.Zero); }
    internal static void ProbeAvx10v1() { _ = Avx10v1.Abs(Vector128<long>.Zero); }
    internal static void ProbeAvx10v2() { _ = Avx10v2.ConvertToByteWithSaturationAndZeroExtendToInt32(Vector128<float>.Zero); }
    internal static void ProbeAvxVnniInt8() { _ = AvxVnniInt8.MultiplyWideningAndAdd(Vector128<int>.Zero, Vector128<sbyte>.Zero, Vector128<sbyte>.Zero); }
    internal static void ProbeAvxVnniInt16() { _ = AvxVnniInt16.MultiplyWideningAndAdd(Vector128<int>.Zero, Vector128<short>.Zero, Vector128<ushort>.Zero); }
    internal static void ProbeGfni() { _ = Gfni.GaloisFieldMultiply(Vector128<byte>.Zero, Vector128<byte>.Zero); }

    // CPUID answers are machine facts, so only shape facts every x86-64 CPU
    // shares are printed. DivRem divides (upper:lower) by divisor; a zero
    // divisor and a quotient wider than the operand both fault in hardware,
    // which .NET reports as DivideByZeroException.
    private static void X86BaseExercise()
    {
        (int eax, int ebx, int ecx, int edx) = X86Base.CpuId(0, 0);
        Console.WriteLine("CpuId(0,0).maxLeaf>=1=" + Fmt.Bool(eax >= 1));
        Console.WriteLine("CpuId(0,0).vendorAscii=" + Fmt.Bool(Fmt.IsPrintableAscii(ebx) && Fmt.IsPrintableAscii(edx) && Fmt.IsPrintableAscii(ecx)));
        (_, _, _, int leaf1Edx) = X86Base.CpuId(1, 0);
        Console.WriteLine("CpuId(1,0).sse2=" + Fmt.Bool((leaf1Edx & (1 << 26)) != 0));

        (uint uq, uint ur) = X86Base.DivRem(7u, 1u, 10u);
        Console.WriteLine("DivRem(u32 7,1,10)=" + uq + "," + ur);
        (int iq, int ir) = X86Base.DivRem(0xFFFFFFF6u, -1, 7);
        Console.WriteLine("DivRem(i32 -10,7)=" + iq + "," + ir);
        (nuint nuq, nuint nur) = X86Base.DivRem((nuint)7, (nuint)1, (nuint)10);
        Console.WriteLine("DivRem(nuint 7,1,10)=" + nuq + "," + nur);
        (nint niq, nint nir) = X86Base.DivRem(unchecked((nuint)(-10L)), (nint)(-1), (nint)7);
        Console.WriteLine("DivRem(nint -10,7)=" + niq + "," + nir);
        Console.WriteLine("DivRem(u32 1,0,0)=" + Fmt.Thrown(() => { _ = X86Base.DivRem(1u, 0u, 0u); }));
        Console.WriteLine("DivRem(u32 0,1,1)=" + Fmt.Thrown(() => { _ = X86Base.DivRem(0u, 1u, 1u); }));
        Console.WriteLine("DivRem(i32 1,0,0)=" + Fmt.Thrown(() => { _ = X86Base.DivRem(1u, 0, 0); }));
        Console.WriteLine("DivRem(i32 0,1,1)=" + Fmt.Thrown(() => { _ = X86Base.DivRem(0u, 1, 1); }));
        Console.WriteLine("DivRem(nuint 0,1,1)=" + Fmt.Thrown(() => { _ = X86Base.DivRem((nuint)0, (nuint)1, (nuint)1); }));
        Console.WriteLine("DivRem(nint 1,0,0)=" + Fmt.Thrown(() => { _ = X86Base.DivRem((nuint)1, (nint)0, (nint)0); }));
        X86Base.Pause();
        Console.WriteLine("Pause=ok");

        if (X86Base.X64.IsSupported)
        {
            (ulong lq, ulong lr) = X86Base.X64.DivRem(7ul, 1ul, 10ul);
            Console.WriteLine("X64.DivRem(u64 7,1,10)=" + lq + "," + lr);
            (long sq, long sr) = X86Base.X64.DivRem(unchecked((ulong)(-10L)), -1L, 7L);
            Console.WriteLine("X64.DivRem(i64 -10,7)=" + sq + "," + sr);
            Console.WriteLine("X64.DivRem(u64 1,0,0)=" + Fmt.Thrown(() => { _ = X86Base.X64.DivRem(1ul, 0ul, 0ul); }));
            Console.WriteLine("X64.DivRem(u64 0,1,1)=" + Fmt.Thrown(() => { _ = X86Base.X64.DivRem(0ul, 1ul, 1ul); }));
            Console.WriteLine("X64.DivRem(i64 1,0,0)=" + Fmt.Thrown(() => { _ = X86Base.X64.DivRem(1ul, 0L, 0L); }));
            Console.WriteLine("X64.DivRem(i64 0,1,1)=" + Fmt.Thrown(() => { _ = X86Base.X64.DivRem(0ul, 1L, 1L); }));
        }
    }

    private static void LzcntExercise()
    {
        Console.WriteLine("LeadingZeroCount(0x00F0)=" + Lzcnt.LeadingZeroCount(0x00F0u) + " ref=" + BitOperations.LeadingZeroCount(0x00F0u));
        Console.WriteLine("LeadingZeroCount(0)=" + Lzcnt.LeadingZeroCount(0u) + " ref=" + BitOperations.LeadingZeroCount(0u));
        if (Lzcnt.X64.IsSupported)
        {
            Console.WriteLine("X64.LeadingZeroCount(0x00F0)=" + Lzcnt.X64.LeadingZeroCount(0x00F0ul) + " ref=" + BitOperations.LeadingZeroCount(0x00F0ul));
            Console.WriteLine("X64.LeadingZeroCount(0)=" + Lzcnt.X64.LeadingZeroCount(0ul) + " ref=" + BitOperations.LeadingZeroCount(0ul));
        }
    }

    private static void PopcntExercise()
    {
        Console.WriteLine("PopCount(0xF0F0F00F)=" + Popcnt.PopCount(0xF0F0F00Fu) + " ref=" + BitOperations.PopCount(0xF0F0F00Fu));
        Console.WriteLine("PopCount(0)=" + Popcnt.PopCount(0u) + " ref=" + BitOperations.PopCount(0u));
        if (Popcnt.X64.IsSupported)
        {
            Console.WriteLine("X64.PopCount(0xF0F0F00F0F0F0FF0)=" + Popcnt.X64.PopCount(0xF0F0F00F0F0F0FF0ul) + " ref=" + BitOperations.PopCount(0xF0F0F00F0F0F0FF0ul));
            Console.WriteLine("X64.PopCount(0)=" + Popcnt.X64.PopCount(0ul) + " ref=" + BitOperations.PopCount(0ul));
        }
    }

    // BitFieldExtract is BEXTR: start in control bits 0-7, length in bits 8-15;
    // a field running past the top of the operand is truncated, not an error.
    private static void Bmi1Exercise()
    {
        const uint v = 0x12345678u;
        Console.WriteLine("AndNot(0xFF00FF00,0xFFFFFFFF)=" + Fmt.Hex(Bmi1.AndNot(0xFF00FF00u, 0xFFFFFFFFu)) + " ref=" + Fmt.Hex(~0xFF00FF00u & 0xFFFFFFFFu));
        Console.WriteLine("BitFieldExtract(v,8,16)=" + Fmt.Hex(Bmi1.BitFieldExtract(v, 8, 16)) + " ref=" + Fmt.Hex((v >> 8) & 0xFFFFu));
        Console.WriteLine("BitFieldExtract(v,0x1008)=" + Fmt.Hex(Bmi1.BitFieldExtract(v, (ushort)0x1008)) + " ref=" + Fmt.Hex((v >> 8) & 0xFFFFu));
        Console.WriteLine("BitFieldExtract(v,28,8)=" + Fmt.Hex(Bmi1.BitFieldExtract(v, 28, 8)) + " ref=" + Fmt.Hex(v >> 28));
        Console.WriteLine("BitFieldExtract(v,0,0)=" + Fmt.Hex(Bmi1.BitFieldExtract(v, 0, 0)) + " ref=" + Fmt.Hex(0u));
        Console.WriteLine("ExtractLowestSetBit(0x0F30)=" + Fmt.Hex(Bmi1.ExtractLowestSetBit(0x0F30u)) + " ref=" + Fmt.Hex(unchecked(0x0F30u & (0u - 0x0F30u))));
        Console.WriteLine("ExtractLowestSetBit(0)=" + Fmt.Hex(Bmi1.ExtractLowestSetBit(0u)) + " ref=" + Fmt.Hex(0u));
        Console.WriteLine("GetMaskUpToLowestSetBit(0x0F30)=" + Fmt.Hex(Bmi1.GetMaskUpToLowestSetBit(0x0F30u)) + " ref=" + Fmt.Hex(0x0F30u ^ (0x0F30u - 1u)));
        Console.WriteLine("GetMaskUpToLowestSetBit(0)=" + Fmt.Hex(Bmi1.GetMaskUpToLowestSetBit(0u)) + " ref=" + Fmt.Hex(0xFFFFFFFFu));
        Console.WriteLine("ResetLowestSetBit(0x0F30)=" + Fmt.Hex(Bmi1.ResetLowestSetBit(0x0F30u)) + " ref=" + Fmt.Hex(0x0F30u & (0x0F30u - 1u)));
        Console.WriteLine("TrailingZeroCount(0x0F30)=" + Bmi1.TrailingZeroCount(0x0F30u) + " ref=" + BitOperations.TrailingZeroCount(0x0F30u));
        Console.WriteLine("TrailingZeroCount(0)=" + Bmi1.TrailingZeroCount(0u) + " ref=" + 32);
        if (Bmi1.X64.IsSupported)
        {
            const ulong w = 0x123456789ABCDEF0ul;
            Console.WriteLine("X64.AndNot(0xFF00FF00FF00FF00,~0)=" + Fmt.Hex(Bmi1.X64.AndNot(0xFF00FF00FF00FF00ul, ulong.MaxValue)) + " ref=" + Fmt.Hex(~0xFF00FF00FF00FF00ul));
            Console.WriteLine("X64.BitFieldExtract(w,8,16)=" + Fmt.Hex(Bmi1.X64.BitFieldExtract(w, 8, 16)) + " ref=" + Fmt.Hex((w >> 8) & 0xFFFFul));
            Console.WriteLine("X64.BitFieldExtract(w,0x1008)=" + Fmt.Hex(Bmi1.X64.BitFieldExtract(w, (ushort)0x1008)) + " ref=" + Fmt.Hex((w >> 8) & 0xFFFFul));
            Console.WriteLine("X64.BitFieldExtract(w,60,8)=" + Fmt.Hex(Bmi1.X64.BitFieldExtract(w, 60, 8)) + " ref=" + Fmt.Hex(w >> 60));
            Console.WriteLine("X64.ExtractLowestSetBit(w)=" + Fmt.Hex(Bmi1.X64.ExtractLowestSetBit(w)) + " ref=" + Fmt.Hex(unchecked(w & (0ul - w))));
            Console.WriteLine("X64.GetMaskUpToLowestSetBit(w)=" + Fmt.Hex(Bmi1.X64.GetMaskUpToLowestSetBit(w)) + " ref=" + Fmt.Hex(w ^ (w - 1ul)));
            Console.WriteLine("X64.ResetLowestSetBit(w)=" + Fmt.Hex(Bmi1.X64.ResetLowestSetBit(w)) + " ref=" + Fmt.Hex(w & (w - 1ul)));
            Console.WriteLine("X64.TrailingZeroCount(w)=" + Bmi1.X64.TrailingZeroCount(w) + " ref=" + BitOperations.TrailingZeroCount(w));
            Console.WriteLine("X64.TrailingZeroCount(0)=" + Bmi1.X64.TrailingZeroCount(0ul) + " ref=" + 64);
        }
    }

    // MultiplyNoFlags returns the high half of the product; the pointer
    // overload stores the low half through the pointer.
    private static unsafe void Bmi2Exercise()
    {
        uint low32;
        Console.WriteLine("MultiplyNoFlags(~0,~0)=" + Fmt.Hex(Bmi2.MultiplyNoFlags(0xFFFFFFFFu, 0xFFFFFFFFu)) + " ref=" + Fmt.Hex((uint)(((ulong)0xFFFFFFFFu * 0xFFFFFFFFu) >> 32)));
        Console.WriteLine("MultiplyNoFlags(~0,~0,&low)=" + Fmt.Hex(Bmi2.MultiplyNoFlags(0xFFFFFFFFu, 0xFFFFFFFFu, &low32)) + " low=" + Fmt.Hex(low32) + " ref=" + Fmt.Hex(unchecked((uint)((ulong)0xFFFFFFFFu * 0xFFFFFFFFu))));
        Console.WriteLine("ParallelBitDeposit(0x0B,0xF0F0)=" + Fmt.Hex(Bmi2.ParallelBitDeposit(0x0Bu, 0xF0F0u)) + " ref=" + Fmt.Hex(Fmt.Pdep(0x0Bul, 0xF0F0ul)));
        Console.WriteLine("ParallelBitExtract(0x12345678,0xF0F0)=" + Fmt.Hex(Bmi2.ParallelBitExtract(0x12345678u, 0xF0F0u)) + " ref=" + Fmt.Hex(Fmt.Pext(0x12345678ul, 0xF0F0ul)));
        Console.WriteLine("ZeroHighBits(~0,8)=" + Fmt.Hex(Bmi2.ZeroHighBits(0xFFFFFFFFu, 8u)) + " ref=" + Fmt.Hex(0xFFu));
        Console.WriteLine("ZeroHighBits(~0,40)=" + Fmt.Hex(Bmi2.ZeroHighBits(0xFFFFFFFFu, 40u)) + " ref=" + Fmt.Hex(0xFFFFFFFFu));
        if (Bmi2.X64.IsSupported)
        {
            ulong low64;
            ulong refHigh = Math.BigMul(ulong.MaxValue, ulong.MaxValue, out ulong refLow);
            Console.WriteLine("X64.MultiplyNoFlags(~0,~0)=" + Fmt.Hex(Bmi2.X64.MultiplyNoFlags(ulong.MaxValue, ulong.MaxValue)) + " ref=" + Fmt.Hex(refHigh));
            Console.WriteLine("X64.MultiplyNoFlags(~0,~0,&low)=" + Fmt.Hex(Bmi2.X64.MultiplyNoFlags(ulong.MaxValue, ulong.MaxValue, &low64)) + " low=" + Fmt.Hex(low64) + " ref=" + Fmt.Hex(refLow));
            Console.WriteLine("X64.ParallelBitDeposit(0x0B,0xF0F0<<32)=" + Fmt.Hex(Bmi2.X64.ParallelBitDeposit(0x0Bul, 0xF0F0ul << 32)) + " ref=" + Fmt.Hex(Fmt.Pdep(0x0Bul, 0xF0F0ul << 32)));
            Console.WriteLine("X64.ParallelBitExtract(w,0xF0F0<<32)=" + Fmt.Hex(Bmi2.X64.ParallelBitExtract(0x123456789ABCDEF0ul, 0xF0F0ul << 32)) + " ref=" + Fmt.Hex(Fmt.Pext(0x123456789ABCDEF0ul, 0xF0F0ul << 32)));
            Console.WriteLine("X64.ZeroHighBits(~0,40)=" + Fmt.Hex(Bmi2.X64.ZeroHighBits(ulong.MaxValue, 40ul)) + " ref=" + Fmt.Hex((1ul << 40) - 1ul));
        }
    }

    private static void X86SerializeExercise()
    {
        X86Serialize.Serialize();
        Console.WriteLine("Serialize=ok");
    }
}
