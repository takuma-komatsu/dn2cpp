using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace PlatformIsaProbe;

// Output spellings and software references shared by the exercise sections.
// Every value printed by an exercise goes through here or through the BCL's
// culture-invariant integer formatting.
internal static class Fmt
{
    internal static string Bool(bool b) => b ? "True" : "False";

    internal static string Hex(uint v) => "0x" + v.ToString("X8");

    internal static string Hex(ulong v) => "0x" + v.ToString("X16");

    internal static string Hex(byte v) => "0x" + v.ToString("X2");

    internal static string Hex(ushort v) => "0x" + v.ToString("X4");

    // A vector or a buffer as its bytes in memory order, lowest address first.
    internal static string Hex(Vector64<byte> v) => Bytes(v);

    internal static string Hex(Vector128<byte> v) => Bytes(v);

    internal static string Hex(Vector256<byte> v) => Bytes(v);

    internal static string Hex(Vector512<byte> v) => Bytes(v);

    internal static unsafe string Hex(byte* p, int count)
    {
        var sb = new System.Text.StringBuilder(2 + 2 * count);
        sb.Append("0x");
        for (int i = 0; i < count; i++)
        {
            sb.Append(p[i].ToString("X2"));
        }
        return sb.ToString();
    }

    private static string Bytes(Vector64<byte> v)
    {
        var sb = new System.Text.StringBuilder("0x");
        for (int i = 0; i < 8; i++)
        {
            sb.Append(v.GetElement(i).ToString("X2"));
        }
        return sb.ToString();
    }

    private static string Bytes(Vector128<byte> v)
    {
        var sb = new System.Text.StringBuilder("0x");
        for (int i = 0; i < 16; i++)
        {
            sb.Append(v.GetElement(i).ToString("X2"));
        }
        return sb.ToString();
    }

    private static string Bytes(Vector256<byte> v)
    {
        var sb = new System.Text.StringBuilder("0x");
        for (int i = 0; i < 32; i++)
        {
            sb.Append(v.GetElement(i).ToString("X2"));
        }
        return sb.ToString();
    }

    private static string Bytes(Vector512<byte> v)
    {
        var sb = new System.Text.StringBuilder("0x");
        for (int i = 0; i < 64; i++)
        {
            sb.Append(v.GetElement(i).ToString("X2"));
        }
        return sb.ToString();
    }

    // The first address at or after p on an `alignment`-byte boundary: the aligned
    // 256-bit loads and stores fault below 32 bytes, and stackalloc promises less.
    internal static unsafe byte* Align(byte* p, int alignment) =>
        (byte*)(((nuint)p + (nuint)(alignment - 1)) & ~(nuint)(alignment - 1));

    // The fixed byte pattern a load reads and a store overwrites; seed tells the
    // buffers of one call apart.
    internal static unsafe void Fill(byte* p, int count, int seed)
    {
        for (int i = 0; i < count; i++)
        {
            p[i] = (byte)(i * 7 + seed * 41 + 3);
        }
    }

    // The portable cross-check a generated exercise prints beside a helper's bytes:
    // both spellings come from the same Hex overload, so equality is byte equality.
    internal static string Ref(string actual, string reference) =>
        actual == reference ? " ref=OK" : " ref=MISMATCH(" + reference + ")";

    // A value the JIT cannot treat as a constant, so an out-of-range immediate
    // takes .NET's run-time range check rather than a compile-time fold.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static T NonConstant<T>(T value) => value;

    // The four bytes of a CPUID vendor register are printable ASCII on every
    // x86-64 CPU; the bytes themselves are a machine fact and never printed.
    internal static bool IsPrintableAscii(int reg)
    {
        for (int shift = 0; shift < 32; shift += 8)
        {
            int b = (reg >> shift) & 0xFF;
            if (b < 0x20 || b > 0x7E)
            {
                return false;
            }
        }
        return true;
    }

    // The name of the exception a faulting instruction raises, or "returned".
    internal static string Thrown(Action action)
    {
        try
        {
            action();
            return "returned";
        }
        catch (DivideByZeroException)
        {
            return "DivideByZeroException";
        }
        catch (OverflowException)
        {
            return "OverflowException";
        }
        catch (ArithmeticException)
        {
            return "ArithmeticException";
        }
        catch (ArgumentOutOfRangeException)
        {
            return "ArgumentOutOfRangeException";
        }
        catch (Exception)
        {
            return "Exception";
        }
    }

    // PDEP: the low bits of value scattered to the set bits of mask, in order.
    internal static ulong Pdep(ulong value, ulong mask)
    {
        ulong result = 0;
        for (ulong bit = 1; mask != 0; bit <<= 1)
        {
            ulong lowest = mask & (0ul - mask);
            if ((value & bit) != 0)
            {
                result |= lowest;
            }
            mask ^= lowest;
        }
        return result;
    }

    // PEXT: the bits of value under the set bits of mask, packed low.
    internal static ulong Pext(ulong value, ulong mask)
    {
        ulong result = 0;
        for (ulong bit = 1; mask != 0; bit <<= 1)
        {
            ulong lowest = mask & (0ul - mask);
            if ((value & lowest) != 0)
            {
                result |= bit;
            }
            mask ^= lowest;
        }
        return result;
    }

    // One CRC32 instruction: the reflected bitwise update over the low `bytes`
    // bytes of data, least significant first, with no initial or final
    // inversion. ISO 3309 is polynomial 0xEDB88320, Castagnoli 0x82F63B78.
    internal static uint Crc32Ref(uint crc, ulong data, int bytes, uint polynomial)
    {
        for (int i = 0; i < bytes; i++)
        {
            crc ^= (byte)(data >> (8 * i));
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc >> 1) ^ (polynomial & (0u - (crc & 1u)));
            }
        }
        return crc;
    }

    // PHMINPOSUW: lane 0 the smallest ushort lane, lane 1 the index of its first
    // occurrence, the other lanes zero.
    internal static Vector128<ushort> MinHorizontalRef(Vector128<ushort> v)
    {
        ushort min = v.GetElement(0);
        ushort index = 0;
        for (int i = 1; i < 8; i++)
        {
            if (v.GetElement(i) < min)
            {
                min = v.GetElement(i);
                index = (ushort)i;
            }
        }
        return Vector128.Create(min, index, 0, 0, 0, 0, 0, 0);
    }

    // CLS: the leading bits equal to the sign bit, the sign bit itself excluded.
    internal static int ClsRef(int x) => BitOperations.LeadingZeroCount((uint)(x ^ (x >> 31))) - 1;

    internal static int ClsRef(long x) => BitOperations.LeadingZeroCount((ulong)(x ^ (x >> 63))) - 1;
}
