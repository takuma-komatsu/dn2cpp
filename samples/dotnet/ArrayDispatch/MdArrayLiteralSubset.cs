#nullable enable
using System;

// multi-dim array literal initializers. A T[,]/T[,,] literal lowers to a
// newmdarr + RuntimeHelpers.InitializeArray copy of the packed RVA blob. Two bugs:
// InitializeArray threw on the MD array type (so even int[,] literals failed), and
// the MD slot width used the int32-promoted Of instead of the packed StorageOf,
// so a sub-word (short/char/bool) literal mis-packed vs the blob. Now the MD slot
// is StorageOf-sized (matching the blob + the single-dim path) and InitializeArray
// has an MD branch. Sub-word printing casts to int / uses char concat to avoid the
// separate culture-formatting path.
namespace MdArrayLiteralSubset;


class Program
{internal static void Run()
    {
        // int[,] literal (the InitializeArray-on-MD fix; Of == StorageOf for int).
        int[,] ii = { { 1, 2, 3 }, { 4, 5, 6 } };
        Console.WriteLine(ii[0, 0] + "," + ii[0, 1] + "," + ii[0, 2] + "," + ii[1, 0] + "," + ii[1, 1] + "," + ii[1, 2]);

        // short[,] literal (sub-word packing).
        short[,] ss = { { 10, 20 }, { 30, 40 }, { 50, 60 } };
        Console.WriteLine((int)ss[0, 0] + "," + (int)ss[0, 1] + "," + (int)ss[1, 0] + "," + (int)ss[2, 1]);

        // char[,] literal (sub-word packing; char concat is fine).
        char[,] cc = { { 'a', 'b' }, { 'c', 'd' } };
        Console.WriteLine("" + cc[0, 0] + cc[0, 1] + cc[1, 0] + cc[1, 1]);

        // bool[,] literal (1-byte packing).
        bool[,] bb = { { true, false }, { false, true } };
        Console.WriteLine((bb[0, 0] ? 1 : 0) + "," + (bb[0, 1] ? 1 : 0) + "," + (bb[1, 0] ? 1 : 0) + "," + (bb[1, 1] ? 1 : 0));

        // 3-D sub-word literal (byte[,,]).
        byte[,,] zzz = { { { 1, 2 }, { 3, 4 } }, { { 5, 6 }, { 7, 8 } } };
        Console.WriteLine((int)zzz[0, 0, 0] + "," + (int)zzz[0, 1, 1] + "," + (int)zzz[1, 0, 1] + "," + (int)zzz[1, 1, 0]);

        // Runtime-populated sub-word MD (the Set/Get packing, not a literal).
        short[,] s2 = new short[2, 3];
        s2[0, 0] = 7;
        s2[1, 2] = 99;
        Console.WriteLine((int)s2[0, 0] + "," + (int)s2[0, 1] + "," + (int)s2[1, 2]);
    }
}
