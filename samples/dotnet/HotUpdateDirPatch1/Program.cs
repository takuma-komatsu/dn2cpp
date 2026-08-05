using System;
using HotUpdateBase;

namespace HotDirPatch;

// First slice of the directory-deployment pair (baked with --patch-version 1;
// its sibling HotUpdateDirPatch2 carries version 2). Both assemblies define
// the SAME FullName — HotDirPatch.Probe — so the gate can observe the
// append-only newest-wins registry: after a directory load applies both in
// ascending patchVersion order, name lookups resolve to version 2's Probe
// while instances constructed from this one keep this type-info. Each patch
// binds only base-image members (the Counter ctor chain, an override of its
// AOT Describe slot, a reference-typed static field store, Console.WriteLine)
// and never the other patch's types — cross-BPI references stay a v1
// carve-out.
public class Probe : Counter
{
    public Probe(int start)
        : base(start)
    {
    }

    // A version marker printed from the interpreted override (string
    // concatenation is off the patch import surface, so the version tag and
    // the base's counter state come out as separate lines).
    public override string Describe()
    {
        Console.WriteLine("probe v1 describing");
        return base.Describe();
    }
}

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("install v1");
        Probe p = new Probe(1);
        p.Add(10);
        Counter.FirstProbe = p;
        Counter.LastProbe = p;
    }
}
