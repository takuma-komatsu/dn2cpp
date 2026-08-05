#nullable enable
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

// SUBJECT: a UInt64 key-derivation chain feeding HMACSHA1 — the shape a save-file
// integrity hash is built from. The hash core itself is covered by HmacSubset;
// what this section pins is everything AROUND it, because
// a single wrong bit anywhere in the derivation reaches the digest as a wholly
// different key and the only symptom is "hash mismatch":
//   * ulong.RotateLeft / ulong.RotateRight (IBinaryInteger generic math) at the
//     REAL 64-bit storage width — a rotate that silently ran at 32 bits, or one
//     whose shift-in half was computed on a narrowed temporary, still returns a
//     plausible number,
//   * unsigned 64-bit add wraparound and xor against a literal above int64 range,
//   * Convert.ToString(ulong) — the unsigned rendering of a value past long.MaxValue,
//   * Encoding.UTF8.GetBytes over the concatenated key, and the HMACSHA1 over it.
// Every intermediate is printed, so a red diff names the step that parted rather
// than only the final digest.
//
// The two trailing blocks widen that to the rest of the family the same slip sat
// in: Convert.ToString / ToInt32 / ToInt64 / ToDouble / ToSingle over uint and
// ulong sources, including the IFormatProvider forms and the out-of-range cases
// that must raise OverflowException rather than answer a wrapped number. None of
// this is a hashing test — it is here because it needs
// System.Security.Cryptography for the digest it feeds.
namespace AchievementKeyHmacSubset;

static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("-- u64 key derivation --");
        ulong value = 7484237571489941UL;
        Console.WriteLine("v0 " + value);
        value += 32454563;
        Console.WriteLine("v1 " + value);
        value = ulong.RotateLeft(value, 12);
        Console.WriteLine("v2 " + value);
        value ^= 45576465734523465;
        Console.WriteLine("v3 " + value);
        value = ulong.RotateRight(value, 7);
        Console.WriteLine("v4 " + value);
        value += 42;
        Console.WriteLine("v5 " + value);

        // Rotate at the extremes of the amount range, where a width slip shows.
        Console.WriteLine("r0 " + ulong.RotateLeft(0x0123456789ABCDEFUL, 0));
        Console.WriteLine("r1 " + ulong.RotateLeft(0x0123456789ABCDEFUL, 1));
        Console.WriteLine("r32 " + ulong.RotateLeft(0x0123456789ABCDEFUL, 32));
        Console.WriteLine("r63 " + ulong.RotateLeft(0x0123456789ABCDEFUL, 63));
        Console.WriteLine("rr32 " + ulong.RotateRight(0x0123456789ABCDEFUL, 32));
        Console.WriteLine("rr63 " + ulong.RotateRight(0x0123456789ABCDEFUL, 63));

        Console.WriteLine("-- derived key --");
        string second = Convert.ToString(value);
        Console.WriteLine("second [" + second + "]");
        byte[] key = Encoding.UTF8.GetBytes("Thirv1152570" + second);
        Console.WriteLine("keylen " + key.Length);
        Console.WriteLine("key " + Convert.ToHexString(key));

        Console.WriteLine("-- Convert.ToString over unsigned sources --");
        Console.WriteLine(Convert.ToString(uint.MaxValue));
        Console.WriteLine(Convert.ToString(2147483648u));
        Console.WriteLine(Convert.ToString(ulong.MaxValue));
        Console.WriteLine(Convert.ToString(9223372036854775808UL));
        Console.WriteLine(Convert.ToString(uint.MaxValue, CultureInfo.InvariantCulture));
        Console.WriteLine(Convert.ToString(ulong.MaxValue, CultureInfo.InvariantCulture));

        Console.WriteLine("-- Convert.To* over unsigned sources --");
        Console.WriteLine(Convert.ToDouble(ulong.MaxValue).ToString("R", CultureInfo.InvariantCulture));
        Console.WriteLine(Convert.ToSingle(ulong.MaxValue).ToString("R", CultureInfo.InvariantCulture));
        Console.WriteLine(Convert.ToDouble(uint.MaxValue).ToString("R", CultureInfo.InvariantCulture));
        Console.WriteLine(Report(() => Convert.ToInt64(ulong.MaxValue).ToString(CultureInfo.InvariantCulture)));
        Console.WriteLine(Report(() => Convert.ToInt32(ulong.MaxValue).ToString(CultureInfo.InvariantCulture)));
        Console.WriteLine(Report(() => Convert.ToInt32(4000000000u).ToString(CultureInfo.InvariantCulture)));
        Console.WriteLine(Report(() => Convert.ToInt64(uint.MaxValue).ToString(CultureInfo.InvariantCulture)));
        Console.WriteLine(Report(() => Convert.ToInt64(9223372036854775807UL).ToString(CultureInfo.InvariantCulture)));
        Console.WriteLine(Report(() => Convert.ToInt32(2147483647u).ToString(CultureInfo.InvariantCulture)));
        Console.WriteLine(Report(() => Convert.ToInt32(9223372036854775808UL).ToString(CultureInfo.InvariantCulture)));
        Console.WriteLine(Report(() => Convert.ToInt32(2147483647UL).ToString(CultureInfo.InvariantCulture)));

        Console.WriteLine("-- HMACSHA1 over the derived key --");
        byte[] data = Encoding.UTF8.GetBytes("{\"UnlockedAchievements\":[],\"Version\":1}");
        Console.WriteLine("hmac " + Convert.ToHexString(HMACSHA1.HashData(key, data)));
        Console.WriteLine("hmac-empty " + Convert.ToHexString(HMACSHA1.HashData(key, Array.Empty<byte>())));
    }

    // An out-of-range Convert.To* must THROW, not answer a wrapped number, so the
    // assert has to carry the exception as text or a silent wrap reads as a pass.
    private static string Report(Func<string> f)
    {
        try
        {
            return f();
        }
        catch (OverflowException)
        {
            return "OverflowException";
        }
    }
}
