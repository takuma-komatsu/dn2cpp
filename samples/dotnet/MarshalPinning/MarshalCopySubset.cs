#nullable enable
using System;
using System.Runtime.InteropServices;

// Marshal.Copy in both directions (managed array <-> native IntPtr buffer) over
// int[], byte[], and double[] element types, including a partial copy with a length
// shorter than the source. The native buffer comes from AllocHGlobal; each copy is a
// raw memcpy of length*sizeof(T) bytes. CoreLib only; diffed exact vs real .NET.
namespace MarshalCopySubset;

unsafe class Program
{
    internal static void __GateEntry()
    {
        // int[] managed -> native -> managed
        int[] si = { 11, 22, 33, 44, 55 };
        IntPtr nbuf = Marshal.AllocHGlobal(si.Length * sizeof(int));
        Marshal.Copy(si, 0, nbuf, si.Length);
        int[] di = new int[si.Length];
        Marshal.Copy(nbuf, di, 0, di.Length);
        Console.WriteLine($"{di[0]},{di[1]},{di[2]},{di[3]},{di[4]}");  // 11,22,33,44,55
        int[] dpart = new int[3];
        Marshal.Copy(nbuf, dpart, 0, 3);                               // first 3 only
        Console.WriteLine($"{dpart[0]},{dpart[1]},{dpart[2]}");        // 11,22,33
        Marshal.FreeHGlobal(nbuf);

        // byte[] round trip
        byte[] sb = { 1, 2, 4, 8, 16, 32 };
        IntPtr bbuf = Marshal.AllocHGlobal(sb.Length);
        Marshal.Copy(sb, 0, bbuf, sb.Length);
        byte[] db = new byte[sb.Length];
        Marshal.Copy(bbuf, db, 0, db.Length);
        long bsum = 0;
        foreach (byte x in db) bsum += x;
        Console.WriteLine(bsum);                                       // 63
        Marshal.FreeHGlobal(bbuf);

        // double[] round trip
        double[] sd = { 0.5, 1.5, 2.5 };
        IntPtr dbuf = Marshal.AllocHGlobal(sd.Length * sizeof(double));
        Marshal.Copy(sd, 0, dbuf, sd.Length);
        double[] dd = new double[sd.Length];
        Marshal.Copy(dbuf, dd, 0, dd.Length);
        Console.WriteLine($"{dd[0]},{dd[1]},{dd[2]}");                 // 0.5,1.5,2.5
        Marshal.FreeHGlobal(dbuf);
    }
}
