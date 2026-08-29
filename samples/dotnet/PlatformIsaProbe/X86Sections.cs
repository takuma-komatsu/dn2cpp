using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace PlatformIsaProbe;

// One representative instruction per X86 family. A probe is called only when
// the family reports unsupported, so its value is discarded: the section's text
// must not change when a family becomes supported.
internal static class X86Sections
{
    // Exercises for supported families land here, one entry per family
    // (exercises["X86.Sse2"] = Sse2Exercise;). None yet.
    internal static void RegisterExercises(Dictionary<string, Action> exercises)
    {
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
}
