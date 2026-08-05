using InterpBench;

namespace InterpBenchPatch;

// The interpreted side of the benchmark: overrides every Kernel virtual with a
// TEXTUALLY IDENTICAL body (plus its own private AddMul, so Calls stays a
// patch-internal call rather than an import), making the AOT-vs-interpreter
// wall-clock comparison apples-to-apples. Any edit to a Kernel body in
// InterpBench/Program.cs must be mirrored here verbatim.
public class PatchedKernel : Kernel
{
    public override long Scalar(int iters)
    {
        long acc = 7;
        double d = 1.0;
        for (int i = 0; i < iters; i++)
        {
            int t = (i * 31 + (int)(acc % 256)) & 0xFFFF;
            if ((t & 1) == 0)
                acc += t;
            else
                acc -= t >> 1;
            if (acc < 0)
                acc = -acc;
            acc %= 1000000007;
            d = d * 1.0000001 + acc % 8;
            if (d > 1000000.0)
                d -= 999999.0;
        }
        return acc + (long)d;
    }

    public override long Calls(int iters)
    {
        long acc = 1;
        for (int i = 0; i < iters; i++)
            acc = AddMul(acc, i);
        return acc;
    }

    public override long Array(int iters)
    {
        int[] buf = new int[256];
        long acc = 0;
        for (int i = 0; i < iters; i++)
        {
            int idx = i & 255;
            int v = buf[idx] * 3 + i;
            buf[idx] = v & 0xFFFFF;
            acc += buf[(i * 7) & 255];
        }
        return acc;
    }

    public override int Tick(int x)
    {
        return x + 1;
    }

    private long AddMul(long acc, int i)
    {
        return (acc * 31 + i) % 1000000007;
    }
}

internal static class Program
{
    private static void Main()
    {
        Kernel.Active = new PatchedKernel();
    }
}
