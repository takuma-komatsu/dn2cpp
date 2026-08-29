using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace PlatformIsaProbe;

// One representative instruction per Arm family, and the exercise of every
// Lowered family; see X86Sections for both rules.
internal static class ArmSections
{
    internal static void RegisterExercises(Dictionary<string, Action> exercises)
    {
        exercises["Arm.ArmBase"] = ArmBaseExercise;
        exercises["Arm.Crc32"] = Crc32Exercise;
    }

    internal static void ProbeArmBase() { _ = ArmBase.LeadingZeroCount(1); }
    internal static void ProbeCrc32() { _ = Crc32.ComputeCrc32(0u, (byte)0); }
    internal static void ProbeAdvSimd() { _ = AdvSimd.Add(Vector128<int>.Zero, Vector128<int>.Zero); }
    internal static void ProbeAes() { _ = Aes.Encrypt(Vector128<byte>.Zero, Vector128<byte>.Zero); }
    internal static void ProbeSha1() { _ = Sha1.FixedRotate(Vector64<uint>.Zero); }
    internal static void ProbeSha256() { _ = Sha256.ScheduleUpdate0(Vector128<uint>.Zero, Vector128<uint>.Zero); }
    internal static void ProbeDp() { _ = Dp.DotProduct(Vector128<int>.Zero, Vector128<sbyte>.Zero, Vector128<sbyte>.Zero); }
    internal static void ProbeRdm() { _ = Rdm.MultiplyRoundedDoublingAndAddSaturateHigh(Vector128<short>.Zero, Vector128<short>.Zero, Vector128<short>.Zero); }
    internal static void ProbeSve() { _ = Sve.Abs(Vector<int>.Zero); }
    internal static void ProbeSve2() { _ = Sve2.AbsSaturate(Vector<int>.Zero); }

    private static void ArmBaseExercise()
    {
        Console.WriteLine("LeadingZeroCount(i32 0x00F0)=" + ArmBase.LeadingZeroCount(0x00F0) + " ref=" + BitOperations.LeadingZeroCount(0x00F0u));
        Console.WriteLine("LeadingZeroCount(i32 -1)=" + ArmBase.LeadingZeroCount(-1) + " ref=" + 0);
        Console.WriteLine("LeadingZeroCount(u32 0)=" + ArmBase.LeadingZeroCount(0u) + " ref=" + BitOperations.LeadingZeroCount(0u));
        Console.WriteLine("LeadingZeroCount(u32 0x80000000)=" + ArmBase.LeadingZeroCount(0x80000000u) + " ref=" + BitOperations.LeadingZeroCount(0x80000000u));
        Console.WriteLine("ReverseElementBits(i32 0x0000000F)=" + Fmt.Hex((uint)ArmBase.ReverseElementBits(0x0000000F)) + " ref=" + Fmt.Hex(0xF0000000u));
        Console.WriteLine("ReverseElementBits(u32 0x00000001)=" + Fmt.Hex(ArmBase.ReverseElementBits(0x00000001u)) + " ref=" + Fmt.Hex(0x80000000u));
        Console.WriteLine("ReverseElementBits(u32 0x12345678)=" + Fmt.Hex(ArmBase.ReverseElementBits(0x12345678u)) + " ref=" + Fmt.Hex(0x1E6A2C48u));
        ArmBase.Yield();
        Console.WriteLine("Yield=ok");

        if (ArmBase.Arm64.IsSupported)
        {
            Console.WriteLine("Arm64.LeadingSignCount(i32 0)=" + ArmBase.Arm64.LeadingSignCount(0) + " ref=" + Fmt.ClsRef(0));
            Console.WriteLine("Arm64.LeadingSignCount(i32 -1)=" + ArmBase.Arm64.LeadingSignCount(-1) + " ref=" + Fmt.ClsRef(-1));
            Console.WriteLine("Arm64.LeadingSignCount(i32 1)=" + ArmBase.Arm64.LeadingSignCount(1) + " ref=" + Fmt.ClsRef(1));
            Console.WriteLine("Arm64.LeadingSignCount(i32 -2)=" + ArmBase.Arm64.LeadingSignCount(-2) + " ref=" + Fmt.ClsRef(-2));
            Console.WriteLine("Arm64.LeadingSignCount(i32 MinValue)=" + ArmBase.Arm64.LeadingSignCount(int.MinValue) + " ref=" + Fmt.ClsRef(int.MinValue));
            Console.WriteLine("Arm64.LeadingSignCount(i64 0)=" + ArmBase.Arm64.LeadingSignCount(0L) + " ref=" + Fmt.ClsRef(0L));
            Console.WriteLine("Arm64.LeadingSignCount(i64 1)=" + ArmBase.Arm64.LeadingSignCount(1L) + " ref=" + Fmt.ClsRef(1L));
            Console.WriteLine("Arm64.LeadingSignCount(i64 MinValue)=" + ArmBase.Arm64.LeadingSignCount(long.MinValue) + " ref=" + Fmt.ClsRef(long.MinValue));
            Console.WriteLine("Arm64.LeadingZeroCount(i64 0x00F0)=" + ArmBase.Arm64.LeadingZeroCount(0x00F0L) + " ref=" + BitOperations.LeadingZeroCount(0x00F0ul));
            Console.WriteLine("Arm64.LeadingZeroCount(i64 -1)=" + ArmBase.Arm64.LeadingZeroCount(-1L) + " ref=" + 0);
            Console.WriteLine("Arm64.LeadingZeroCount(u64 0)=" + ArmBase.Arm64.LeadingZeroCount(0ul) + " ref=" + BitOperations.LeadingZeroCount(0ul));
            Console.WriteLine("Arm64.MultiplyHigh(i64 -3,7)=" + ArmBase.Arm64.MultiplyHigh(-3L, 7L) + " ref=" + Math.BigMul(-3L, 7L, out _));
            Console.WriteLine("Arm64.MultiplyHigh(i64 1<<40,1<<40)=" + ArmBase.Arm64.MultiplyHigh(1L << 40, 1L << 40) + " ref=" + Math.BigMul(1L << 40, 1L << 40, out _));
            Console.WriteLine("Arm64.MultiplyHigh(i64 MinValue,MinValue)=" + ArmBase.Arm64.MultiplyHigh(long.MinValue, long.MinValue) + " ref=" + Math.BigMul(long.MinValue, long.MinValue, out _));
            Console.WriteLine("Arm64.MultiplyHigh(i64 MinValue,MaxValue)=" + ArmBase.Arm64.MultiplyHigh(long.MinValue, long.MaxValue) + " ref=" + Math.BigMul(long.MinValue, long.MaxValue, out _));
            Console.WriteLine("Arm64.MultiplyHigh(u64 ~0,~0)=" + Fmt.Hex(ArmBase.Arm64.MultiplyHigh(ulong.MaxValue, ulong.MaxValue)) + " ref=" + Fmt.Hex(Math.BigMul(ulong.MaxValue, ulong.MaxValue, out _)));
            Console.WriteLine("Arm64.MultiplyHigh(u64 1<<40,1<<40)=" + Fmt.Hex(ArmBase.Arm64.MultiplyHigh(1ul << 40, 1ul << 40)) + " ref=" + Fmt.Hex(Math.BigMul(1ul << 40, 1ul << 40, out _)));
            Console.WriteLine("Arm64.ReverseElementBits(i64 0xF)=" + Fmt.Hex((ulong)ArmBase.Arm64.ReverseElementBits(0xFL)) + " ref=" + Fmt.Hex(0xF000000000000000ul));
            Console.WriteLine("Arm64.ReverseElementBits(u64 1)=" + Fmt.Hex(ArmBase.Arm64.ReverseElementBits(1ul)) + " ref=" + Fmt.Hex(0x8000000000000000ul));
            Console.WriteLine("Arm64.ReverseElementBits(u64 w)=" + Fmt.Hex(ArmBase.Arm64.ReverseElementBits(0x123456789ABCDEF0ul)) + " ref=" + Fmt.Hex(0x0F7B3D591E6A2C48ul));
        }
    }

    // ComputeCrc32 is the ISO 3309 polynomial, ComputeCrc32C the Castagnoli one;
    // the C variant has a second reference in BitOperations.Crc32C.
    private static void Crc32Exercise()
    {
        const uint seed = 0xFFFFFFFFu;
        const uint iso = 0xEDB88320u;
        const uint castagnoli = 0x82F63B78u;
        // Not a constant: the narrowing casts below are the runtime's, as an
        // application's would be.
        ulong data = 0x6867666564636261ul;
        Console.WriteLine("ComputeCrc32(u8)=" + Fmt.Hex(Crc32.ComputeCrc32(seed, (byte)data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 1, iso)));
        Console.WriteLine("ComputeCrc32(u16)=" + Fmt.Hex(Crc32.ComputeCrc32(seed, (ushort)data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 2, iso)));
        Console.WriteLine("ComputeCrc32(u32)=" + Fmt.Hex(Crc32.ComputeCrc32(seed, (uint)data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 4, iso)));
        Console.WriteLine("ComputeCrc32C(u8)=" + Fmt.Hex(Crc32.ComputeCrc32C(seed, (byte)data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 1, castagnoli)) + " bitops=" + Fmt.Hex(BitOperations.Crc32C(seed, (byte)data)));
        Console.WriteLine("ComputeCrc32C(u16)=" + Fmt.Hex(Crc32.ComputeCrc32C(seed, (ushort)data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 2, castagnoli)) + " bitops=" + Fmt.Hex(BitOperations.Crc32C(seed, (ushort)data)));
        Console.WriteLine("ComputeCrc32C(u32)=" + Fmt.Hex(Crc32.ComputeCrc32C(seed, (uint)data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 4, castagnoli)) + " bitops=" + Fmt.Hex(BitOperations.Crc32C(seed, (uint)data)));
        if (Crc32.Arm64.IsSupported)
        {
            Console.WriteLine("Arm64.ComputeCrc32(u64)=" + Fmt.Hex(Crc32.Arm64.ComputeCrc32(seed, data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 8, iso)));
            Console.WriteLine("Arm64.ComputeCrc32C(u64)=" + Fmt.Hex(Crc32.Arm64.ComputeCrc32C(seed, data)) + " ref=" + Fmt.Hex(Fmt.Crc32Ref(seed, data, 8, castagnoli)) + " bitops=" + Fmt.Hex(BitOperations.Crc32C(seed, data)));
        }
    }
}
