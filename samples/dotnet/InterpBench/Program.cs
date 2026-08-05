using System;
using Dn2Cpp.Runtime;

namespace InterpBench;

// The interpreter-benchmark kernel: every hot loop lives in a virtual method,
// so the SAME workload runs either on this AOT body (no patch loaded) or on
// the patch's textually identical interpreted override (BPI loaded, which
// installs a PatchedKernel into Active). The bodies stay inside the patch
// converter's supported surface — scalar int/long/double arithmetic,
// comparisons, branches, locals, an int[] with ldelem/stelem/ldlen, and
// kernel-internal calls — so both sides execute the same logic and produce
// byte-identical checksums.
public class Kernel
{
    public static Kernel? Active;

    // Mixed int/long/double arithmetic, comparisons and branches with a serial
    // dependence on the checksum, so neither side can hoist or vectorize it.
    public virtual long Scalar(int iters)
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

    // A call to the kernel's OWN private helper per iteration: on the AOT side
    // a plain static-shaped call, on the interpreted side a patch-internal
    // method-to-method call (no import boundary).
    public virtual long Calls(int iters)
    {
        long acc = 1;
        for (int i = 0; i < iters; i++)
            acc = AddMul(acc, i);
        return acc;
    }

    // Wrap-indexed read-modify-write over a small array: ldelem/stelem/ldlen
    // throughput with a bounded element range so the checksum stays exact.
    public virtual long Array(int iters)
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

    // A tiny body: the per-call dispatch-cost probe. The caller loops on the
    // AOT side, so every call crosses the dispatch boundary (an N2M transition
    // per call when the patch override is installed).
    public virtual int Tick(int x)
    {
        return x + 1;
    }

    private long AddMul(long acc, int i)
    {
        return (acc * 31 + i) % 1000000007;
    }
}

// Usage: InterpBench <section> <iters> [bpiPath]
//
// With a third argument the BPI is loaded FIRST (its entry installs the
// patched kernel into Kernel.Active); the impl marker on stderr says which
// implementation actually ran, so a silently failed load cannot masquerade
// as a 1.0x ratio. The single stdout line is the checksum, byte-identical
// between the AOT and interpreted runs.
internal static class Program
{
    private static void Main(string[] args)
    {
        string section = args[0];
        int iters = int.Parse(args[1]);
        if (args.Length == 3)
            HotUpdate.Run(args[2]);
        if (Kernel.Active == null)
            Kernel.Active = new Kernel();
        Console.Error.WriteLine("impl:" + Kernel.Active.GetType().Name);

        long checksum = 0;
        if (section == "scalar")
            checksum = Kernel.Active.Scalar(iters);
        else if (section == "calls")
            checksum = Kernel.Active.Calls(iters);
        else if (section == "array")
            checksum = Kernel.Active.Array(iters);
        else if (section == "virt")
        {
            // The loop stays on the AOT side: each iteration is one virtual
            // dispatch into the (possibly interpreted) tiny Tick body.
            for (int i = 0; i < iters; i++)
                checksum += Kernel.Active.Tick(i & 15);
        }
        // "noop" runs nothing: it measures process startup + patch load.
        Console.WriteLine("checksum:" + section + ":" + checksum);
    }
}
