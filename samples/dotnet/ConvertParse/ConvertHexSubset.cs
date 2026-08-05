#nullable disable
using System;

namespace ConvertHexSubset
{
    // Convert.ToHexString/ToHexStringLower over byte[] and the
    // (byte[], int offset, int count) overloads, plus FromHexString
    // round-trips and the BCL's argument validation (catchable
    // ArgumentNull/ArgumentOutOfRange).
    internal static class Program
    {
        internal static void __GateEntry()
        {
            byte[] data = new byte[] { 0x00, 0x1A, 0x2B, 0xC3, 0xD4, 0xFF };
            Console.WriteLine(Convert.ToHexString(data));                             // 001A2BC3D4FF
            Console.WriteLine(Convert.ToHexStringLower(data));                        // 001a2bc3d4ff
            Console.WriteLine(Convert.ToHexString(data, 0, data.Length));             // full range
            Console.WriteLine(Convert.ToHexString(data, 2, 3));                       // 2BC3D4
            Console.WriteLine(Convert.ToHexStringLower(data, 2, 3));                  // 2bc3d4
            Console.WriteLine("[" + Convert.ToHexString(data, 3, 0) + "]");           // []
            Console.WriteLine("[" + Convert.ToHexString(data, data.Length, 0) + "]"); // []
            Console.WriteLine(Convert.FromHexString(Convert.ToHexString(data, 1, 4)).Length); // 4

            byte[] back = Convert.FromHexString("001a2bC3D4ff");
            Console.WriteLine(back.Length);    // 6
            Console.WriteLine((int)back[3]);   // 195

            try { Convert.ToHexString(data, 5, 3); }
            catch (Exception e) { Console.WriteLine(e.GetType().Name); }  // ArgumentOutOfRangeException
            try { Convert.ToHexString(data, -1, 2); }
            catch (Exception e) { Console.WriteLine(e.GetType().Name); }  // ArgumentOutOfRangeException
            try { Convert.ToHexStringLower(data, 0, 99); }
            catch (Exception e) { Console.WriteLine(e.GetType().Name); }  // ArgumentOutOfRangeException
            try { Convert.ToHexString(null, 0, 0); }
            catch (Exception e) { Console.WriteLine(e.GetType().Name); }  // ArgumentNullException
        }
    }
}
