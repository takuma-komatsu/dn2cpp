using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace PlatformIsaProbe;

// One representative instruction per Arm family; see X86Sections for the rule.
internal static class ArmSections
{
    // Exercises for supported families land here, one entry per family
    // (exercises["Arm.AdvSimd"] = AdvSimdExercise;). None yet.
    internal static void RegisterExercises(Dictionary<string, Action> exercises)
    {
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
}
