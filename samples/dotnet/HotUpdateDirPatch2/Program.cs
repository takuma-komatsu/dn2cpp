using System;
using HotUpdateBase;

namespace HotDirPatch;

// Second slice of the directory-deployment pair (baked with --patch-version 2;
// see HotUpdateDirPatch1 for the pairing contract). Redefines the same
// FullName — HotDirPatch.Probe — so loading it after version 1 makes THIS
// registration the one name lookups resolve to (append-only newest-wins),
// while version 1's already-constructed instance keeps its old type-info.
// Leaves Counter.FirstProbe alone: only the freshest probe lands in
// Counter.LastProbe.
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
        Console.WriteLine("probe v2 describing");
        return base.Describe();
    }
}

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("install v2");
        Probe p = new Probe(2);
        p.Add(20);
        Counter.LastProbe = p;
    }
}
