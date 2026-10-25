namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private TypeNameSnapshot? _typeNameSnapshot;

    private TypeNameSnapshot TypeNames()
    {
        if (_typeNameSnapshot is not null)
            return _typeNameSnapshot;
        string loadedDirectory = Path.GetDirectoryName(_c.Modules[0].Path)!;
        string preservation = Path.Combine(loadedDirectory, "preservation.xml");
        _typeNameSnapshot = TypeNameSnapshot.FromPreservation(preservation)
            ?? TypeNameSnapshot.Capture(_c.Modules.Select(module =>
                (module.AssemblyName, _c.OriginalAssemblyPaths[module.Index])).ToArray());
        foreach (var module in _c.Modules)
            _typeNameSnapshot.AddMissing(module.AssemblyName, _c.OriginalAssemblyPaths[module.Index]);
        return _typeNameSnapshot;
    }

    private static int NextTypeNameBucketCount(int count)
    {
        int[] primes =
        [
            4861, 5839, 7013, 8419, 10103, 12143, 14591, 17519, 21023,
            25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523,
            108631, 130363, 156437, 187751, 225307, 270371, 324449,
            389357, 467237, 560689, 672827, 807403, 968897, 1162687,
            1395263, 1674319, 2009191, 2411033, 2893249, 3471899,
            4166287, 4999559, 5999471, 7199369, 8639249, 10367101,
            12440521, 14928637, 17914367, 21497257, 25796711, 30956053,
            37147273, 44576759, 53492113, 64190537, 77028659, 92434393,
            110921273, 133105543, 159726653, 191671993, 230006431,
            276007757, 331209331, 397451207, 476941459, 572329759,
            686795723, 824154901, 988985923, 1186783133, 1424139767,
            1708967731, 2050761299
        ];
        int target = count * 4;
        foreach (int prime in primes)
            if (prime >= target)
                return prime;
        return target;
    }

    // CoreCLR walks the sensitive table bucket by bucket when building its folded
    // table. Replaying insertion and growth gives each metadata name that walk's
    // position, including nested names and modules that outgrow their first table.
    private Dictionary<(string Assembly, string Name), int> BuildTypeNameCaseRanks()
    {
        var ranks = new Dictionary<(string Assembly, string Name), int>();
        foreach (var module in _c.Modules)
        {
            if (!TypeNames().TryGet(module.AssemblyName, out var source))
                throw new InvalidOperationException("Missing original type-name metadata: " + module.AssemblyName);
            var entries = source.Entries;

            var buckets = new List<int>[1024];
            int inserted = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                int bucket = (int)(entries[i].Hash % (uint)buckets.Length);
                (buckets[bucket] ??= new List<int>()).Insert(0, i);
                inserted++;
                if (inserted <= buckets.Length * 2)
                    continue;
                var grown = new List<int>[NextTypeNameBucketCount(buckets.Length)];
                foreach (var chain in buckets)
                    if (chain is not null)
                        foreach (int index in chain)
                        {
                            int next = (int)(entries[index].Hash % (uint)grown.Length);
                            (grown[next] ??= new List<int>()).Add(index);
                        }
                buckets = grown;
            }

            int rank = 0;
            foreach (var chain in buckets)
                if (chain is not null)
                    foreach (int index in chain)
                        ranks[(module.AssemblyName, entries[index].Name)] = ++rank;
        }
        return ranks;
    }
}
