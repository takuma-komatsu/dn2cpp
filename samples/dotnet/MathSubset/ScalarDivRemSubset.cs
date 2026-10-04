using System;

namespace Dn2Cpp;

internal static class ScalarDivRemSubset
{
    public static void Run()
    {
        Console.WriteLine("== scalar DivRem ==");
        {
            foreach (sbyte dividend in new sbyte[] { 0, 17, sbyte.MinValue, sbyte.MaxValue })
                foreach (sbyte divisor in new sbyte[] { 0, 1, 5, -1 })
                {
                    try
                    {
                        var result = sbyte.DivRem(dividend, divisor);
                        Console.WriteLine("sbyte:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("sbyte:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (byte dividend in new byte[] { 0, 17, byte.MinValue, byte.MaxValue })
                foreach (byte divisor in new byte[] { 0, 1, 5 })
                {
                    try
                    {
                        var result = byte.DivRem(dividend, divisor);
                        Console.WriteLine("byte:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("byte:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (short dividend in new short[] { 0, 17, short.MinValue, short.MaxValue })
                foreach (short divisor in new short[] { 0, 1, 5, -1 })
                {
                    try
                    {
                        var result = short.DivRem(dividend, divisor);
                        Console.WriteLine("short:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("short:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (ushort dividend in new ushort[] { 0, 17, ushort.MinValue, ushort.MaxValue })
                foreach (ushort divisor in new ushort[] { 0, 1, 5 })
                {
                    try
                    {
                        var result = ushort.DivRem(dividend, divisor);
                        Console.WriteLine("ushort:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ushort:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (int dividend in new int[] { 0, 17, int.MinValue, int.MaxValue })
                foreach (int divisor in new int[] { 0, 1, 5, -1 })
                {
                    try
                    {
                        var result = int.DivRem(dividend, divisor);
                        Console.WriteLine("int:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("int:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (uint dividend in new uint[] { 0, 17, uint.MinValue, uint.MaxValue })
                foreach (uint divisor in new uint[] { 0, 1, 5 })
                {
                    try
                    {
                        var result = uint.DivRem(dividend, divisor);
                        Console.WriteLine("uint:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("uint:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (long dividend in new long[] { 0, 17, long.MinValue, long.MaxValue })
                foreach (long divisor in new long[] { 0, 1, 5, -1 })
                {
                    try
                    {
                        var result = long.DivRem(dividend, divisor);
                        Console.WriteLine("long:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("long:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (ulong dividend in new ulong[] { 0, 17, ulong.MinValue, ulong.MaxValue })
                foreach (ulong divisor in new ulong[] { 0, 1, 5 })
                {
                    try
                    {
                        var result = ulong.DivRem(dividend, divisor);
                        Console.WriteLine("ulong:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ulong:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (nint dividend in new nint[] { 0, 17, nint.MinValue, nint.MaxValue })
                foreach (nint divisor in new nint[] { 0, 1, 5, -1 })
                {
                    try
                    {
                        var result = nint.DivRem(dividend, divisor);
                        Console.WriteLine("nint:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("nint:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        {
            foreach (nuint dividend in new nuint[] { 0, 17, nuint.MinValue, nuint.MaxValue })
                foreach (nuint divisor in new nuint[] { 0, 1, 5 })
                {
                    try
                    {
                        var result = nuint.DivRem(dividend, divisor);
                        Console.WriteLine("nuint:" + dividend + ":" + divisor + "=" + result.Quotient + ":" + result.Remainder);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("nuint:" + dividend + ":" + divisor + "=" + ex.GetType().Name);
                    }
                }
        }
        Console.WriteLine("scalar DivRem end");
    }
}
