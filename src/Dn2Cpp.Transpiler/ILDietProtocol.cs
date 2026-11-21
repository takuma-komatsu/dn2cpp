namespace Dn2Cpp;

internal static class ILDietProtocol
{
    internal static readonly string[] CounterNames =
    {
        "types.metadata", "types.runtime", "types.members", "types.explicit",
        "methods.metadata", "methods.body", "methods.evidence-body", "methods.stub", "methods.removed",
        "decode.signature", "decode.body", "decode.max-per-method",
        "summary.methods", "summary.unknown", "summary.budget",
        "rules.reflection", "rules.demand", "rules.opaque",
        "scope.unknown-members.types", "scope.unknown-types.types", "work", "work.budget",
        "fallback", "fallback.assemblies", "registry.factories.kept", "registry.factories.redirected",
        "registry.factories.unknown", "registry.scripts.kept", "registry.scripts.filtered",
    };

    // The compiler and companion must agree on the policy for every original
    // assembly ordinal, including when none has verified pack provenance.
    internal static string FrameworkAssetsDigest(int count, IReadOnlySet<int> indexes)
    {
        var hash = new StableHash();
        hash.AddText("ildiet-framework-assets-v1");
        for (int i = 0; i < count; i++)
            hash.AddText(indexes.Contains(i) ? "1" : "0");
        return hash.Hex();
    }

    // Input/output locations and budgets cannot change a backend retention
    // contract. Sort clauses and attributes to give equivalent requests one id.
    internal static string ContractDigest(LinkNode request)
    {
        var clauses = new List<string>();
        foreach (var child in request.Children)
        {
            if (child.Name is not ("root" or "reflectionRoot" or "rewriteAssembly" or "skipAssembly" or "typeRoot"
                or "methodRoot" or "conditionalMembers" or "suppressDefaultSeeds"
                or "registrationAttribute" or "constructorRegistry" or "cut" or "feature"))
                continue;
            var clause = new StableHash();
            clause.AddText(child.Name);
            foreach (var attribute in child.Attributes.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                clause.AddText(attribute.Key);
                clause.AddText(attribute.Value);
            }
            clauses.Add(clause.Hex());
        }
        clauses.Sort(StringComparer.Ordinal);
        var hash = new StableHash();
        hash.AddText("ildiet-contract-v2");
        hash.AddText(request.Attr("copyAll") ?? "false");
        if (request.Attr("strippingLevel") is { } level && level != "high")
        {
            hash.AddText("strippingLevel");
            hash.AddText(level);
        }
        if (request.Attr("frameworkAssets") is { } frameworkAssets)
        {
            hash.AddText("frameworkAssets");
            hash.AddText(frameworkAssets);
        }
        foreach (string clause in clauses)
            hash.AddText(clause);
        return hash.Hex();
    }
}
