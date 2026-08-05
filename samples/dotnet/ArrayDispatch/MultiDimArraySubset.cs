#nullable enable
// rectangular multi-dimensional arrays (T[,], T[,,]) — common in grid /
// tilemap game code. The Dn2CppMDArray header + newmdarr + element get/set infra
// already existed; this adds the System.Array shape queries a T[,] reaches as a
// method receiver (it is not an ldlen target): GetLength(dim), GetUpperBound,
// GetLowerBound, Rank and the total Length (product of the per-dimension lengths).
using System;
namespace MultiDimArraySubset;


class Program
{internal static void Run()
    {
        int[,] g = new int[2, 3];
        g[0, 0] = 1;
        g[1, 2] = 9;
        g[0, 1] = g[0, 0] + 5;

        int sum = 0;
        for (int i = 0; i < g.GetLength(0); i++)
            for (int j = 0; j < g.GetLength(1); j++)
                sum += g[i, j];

        Console.WriteLine(sum + " " + g.GetLength(0) + "x" + g.GetLength(1));
        Console.WriteLine("rank=" + g.Rank + " len=" + g.Length);
        Console.WriteLine("ub=" + g.GetUpperBound(0) + "," + g.GetUpperBound(1) + " lb=" + g.GetLowerBound(0));

        // 3-D
        int[,,] c = new int[2, 2, 2];
        c[1, 1, 1] = 7;
        c[0, 0, 0] = 3;
        Console.WriteLine((c[1, 1, 1] + c[0, 0, 0]) + " " + c.Length + " rank=" + c.Rank);

        // reference-element 2-D array
        string[,] names = new string[2, 2];
        names[0, 0] = "hi";
        names[1, 1] = "bye";
        Console.WriteLine(names[0, 0] + "/" + names[1, 1]);
    }
}
