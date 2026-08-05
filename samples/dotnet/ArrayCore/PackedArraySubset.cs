#nullable disable
using System;

namespace PackedArraySubset
{
    // sub-word arrays (char/short/ushort/bool) pack to their real element width.
    // A literal with >= 3 constant elements lands in a <PrivateImplementationDetails>
    // RVA blob copied by InitializeArray; with 4-byte slots the packed blob was copied
    // misaligned (silent corruption). Now StorageOf packs them (char16_t/int16_t/
    // uint16_t/uint8_t) and the sizeof opcode reports the storage width.
    internal static class Program
    {
        internal static void Run()
        {
            char[] cs = { 'a', 'e', 'i', 'o', 'u' };
            foreach (var c in cs) Console.Write(c);
            Console.WriteLine();                       // aeiou
            Console.WriteLine((int)cs[3]);             // 111

            short[] ss = { 10, -20, 30, -40, 50 };     // signed: sign-extends
            int sum = 0; foreach (var x in ss) sum += x;
            Console.WriteLine(sum);                    // 30
            Console.WriteLine(ss[1]);                  // -20

            ushort[] us = { 60000, 1, 65535, 2, 3 };   // unsigned: zero-extends
            Console.WriteLine(us[0] + us[2]);          // 125535

            bool[] bs = { true, false, true, true, false };
            int t = 0; foreach (var b in bs) if (b) t++;
            Console.WriteLine(t);                       // 3

            // stelem round-trip into a packed char[]
            cs[0] = 'X';
            Console.Write(cs[0]); Console.WriteLine(cs[4]); // Xu
        }
    }
}
