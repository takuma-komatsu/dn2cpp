using System;

namespace BoxPrimitiveSubset
{
    // box char + the small integer primitives (Byte/SByte/Int16/UInt16/
    // UInt32). Previously `box of Char is not supported`. Each boxes to object,
    // ToStrings (via Object.ToString / value concat), and unbox-round-trips.
    internal static class Program
    {
        internal static void Run()
        {
            char c = 'Q';
            object oc = c;                       // box char
            Console.WriteLine(oc);               // Q
            Console.WriteLine("v=" + oc);        // v=Q (Concat(object) -> Object.ToString)
            char back = (char)oc;                // unbox.any
            Console.WriteLine(back == 'Q');      // True

            byte b = 200;
            object ob = b;
            Console.WriteLine(ob);               // 200

            sbyte sb = -7;
            object osb = sb;
            Console.WriteLine(osb);              // -7

            short s16 = -32000;
            object os16 = s16;
            Console.WriteLine(os16);             // -32000

            ushort u16 = 65000;
            object ou16 = u16;
            Console.WriteLine(ou16);             // 65000

            uint u32 = 4000000000;
            object ou32 = u32;
            Console.WriteLine(ou32);             // 4000000000 (>int.MaxValue stays positive)
        }
    }
}
