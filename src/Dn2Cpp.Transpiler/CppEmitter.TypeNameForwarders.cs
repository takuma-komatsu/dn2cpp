namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private List<(string Name, string Source, string Target, int Rank)> BuildTypeForwarders(
        Dictionary<(string Assembly, string Name), int> caseRanks, HashSet<string> retainedNames)
    {
        var result = new List<(string Name, string Source, string Target, int Rank)>();
        var snapshot = TypeNames();
        var pending = new Queue<string>(_c.Modules.Select(m => m.AssemblyName)
            .Concat(new[] { "mscorlib", "netstandard", "System.Runtime" })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.Ordinal));
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (pending.Count != 0)
        {
            string source = pending.Dequeue();
            if (!visited.Add(source))
                continue;
            if (!snapshot.TryGet(source, out var metadata))
                continue;
            foreach (var (type, target) in metadata.Forwarders)
            {
                if (!retainedNames.Contains(type))
                    continue;
                int rank = caseRanks.TryGetValue((source, type), out int known) ? known : 0;
                result.Add((type, source, target, rank));
                pending.Enqueue(target);
            }
        }
        result.Sort((left, right) =>
        {
            int compare = StringComparer.Ordinal.Compare(left.Source, right.Source);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Name, right.Name);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.Target, right.Target);
        });
        return result;
    }

}
