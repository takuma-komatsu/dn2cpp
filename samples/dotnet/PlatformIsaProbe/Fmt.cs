using System;
using System.Numerics;

namespace PlatformIsaProbe;

// Output spellings and software references shared by the exercise sections.
// Every value printed by an exercise goes through here or through the BCL's
// culture-invariant integer formatting.
internal static class Fmt
{
    internal static string Bool(bool b) => b ? "True" : "False";

    internal static string Hex(uint v) => "0x" + v.ToString("X8");

    internal static string Hex(ulong v) => "0x" + v.ToString("X16");

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

    // CLS: the leading bits equal to the sign bit, the sign bit itself excluded.
    internal static int ClsRef(int x) => BitOperations.LeadingZeroCount((uint)(x ^ (x >> 31))) - 1;

    internal static int ClsRef(long x) => BitOperations.LeadingZeroCount((ulong)(x ^ (x >> 63))) - 1;
}
