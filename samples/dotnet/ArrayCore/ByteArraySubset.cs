#nullable disable
using System;

namespace ByteArraySubset
{
    // byte[]/sbyte[] now pack to 1-byte element slots. Exercises the
    // InitializeArray RVA-blob path (>=3 constant elements), element-wise stelem,
    // sum/mutation, sbyte sign handling and a sub-word loop — all must match.NET.
    internal static class Program
    {
        internal static void Run()
        {
            // >=3 constant elements -> Roslyn uses RuntimeHelpers.InitializeArray.
            byte[] data = new byte[] { 10, 20, 30, 40, 250, 255 };
            Console.WriteLine(data.Length);            // 6
            int sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i];
            }
            Console.WriteLine(sum);                    // 605
            Console.WriteLine((int)data[4]);           // 250
            Console.WriteLine((int)data[5]);           // 255

            // Mutation through stelem then read back.
            data[0] = 200;
            data[5] = 1;
            Console.WriteLine((int)data[0]);           // 200
            Console.WriteLine((int)data[5]);           // 1

            // Element-wise constructed array (no InitializeArray).
            byte[] built = new byte[3];
            built[0] = 1;
            built[1] = 128;
            built[2] = 255;
            Console.WriteLine((int)built[0] + "," + (int)built[1] + "," + (int)built[2]); // 1,128,255

            // sbyte sign extension on read.
            sbyte[] signed = new sbyte[] { -128, -1, 0, 1, 127 };
            int s = 0;
            for (int i = 0; i < signed.Length; i++)
            {
                s += signed[i];
            }
            Console.WriteLine(s);                      // -1
            Console.WriteLine((int)signed[0]);         // -128
            Console.WriteLine((int)signed[1]);         // -1

            // Adjacent-element independence (catches over-wide writes).
            byte[] adj = new byte[4];
            adj[1] = 0xFF;
            Console.WriteLine((int)adj[0] + "," + (int)adj[1] + "," + (int)adj[2]); // 0,255,0
        }
    }
}
