using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private bool TypeNameIsUncertain(ReflectionTypeName name, SummaryValue assemblies, ModuleDefinition caller,
        bool assemblyLookup, bool ignoreCase)
    {
        if (!assemblyLookup)
        {
            ProbeNamedTypeIdentity(name, caller, null, ignoreCase, out bool unknown);
            return unknown;
        }
        bool uncertain = assemblies.Unknown;
        foreach (var module in assemblies.Assemblies)
        {
            ProbeNamedTypeIdentity(name, caller, module, ignoreCase, out bool unknown);
            uncertain |= unknown;
        }
        return uncertain;
    }

    // Candidate retention is broader than admission's ordered name resolver. Probe
    // original identity only to preserve its uncertainty without creating contexts.
    private TypeDefinition? ProbeNamedTypeIdentity(ReflectionTypeName name, ModuleDefinition caller,
        ModuleDefinition? searched, bool ignoreCase, out bool unknown)
    {
        Spend();
        unknown = ignoreCase && (searched is null || name.GenericArguments.Length != 0
            || !AsciiTypeName(name.TopLevelName) || name.NestedNames.Any(n => !AsciiTypeName(n)));
        if (unknown) return null;
        var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        TypeDefinition? found = null;
        foreach (var module in NameIdentityScopes(name, caller, searched))
        {
            foreach (var type in module.Types)
            {
                Spend();
                string fullName = type.Namespace.Length == 0 ? type.Name : type.Namespace + "." + type.Name;
                if (!string.Equals(fullName, name.TopLevelName, comparison)) continue;
                if (found is not null)
                {
                    unknown = true;
                    return null;
                }
                found = type;
            }
            if (found is not null) break;
        }
        if (found is null) return null;
        foreach (string nestedName in name.NestedNames)
        {
            TypeDefinition? next = null;
            foreach (var nested in found.NestedTypes)
            {
                Spend();
                if (!string.Equals(nested.Name, nestedName, comparison)) continue;
                if (next is not null)
                {
                    unknown = true;
                    return null;
                }
                next = nested;
            }
            if (next is null) return null;
            found = next;
        }
        if (name.GenericArguments.Length != 0)
        {
            if (name.GenericArguments.Length != found.GenericParameters.Count) return null;
            foreach (var argument in name.GenericArguments)
                if (ProbeNamedTypeIdentity(argument, caller, null, false, out unknown) is null) return null;
        }
        foreach (int modifier in name.Modifiers)
        {
            Spend();
            if (modifier >= ReflectionTypeName.SZArray) continue;
            unknown = true;
            return null;
        }
        return found;
    }

    private IEnumerable<ModuleDefinition> NameIdentityScopes(ReflectionTypeName name, ModuleDefinition caller,
        ModuleDefinition? searched)
    {
        if (searched is not null)
        {
            yield return searched;
            yield break;
        }
        if (name.AssemblySimpleName is not { } simple)
        {
            yield return caller;
            foreach (var assembly in _assemblies)
            {
                Spend();
                if (assembly.Assembly.Name.Name is "System.Private.CoreLib" or "mscorlib"
                    && assembly.Assembly.MainModule != caller) yield return assembly.Assembly.MainModule;
            }
            yield break;
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (seen.Add(simple))
        {
            Spend();
            bool loaded = false;
            string? next = null;
            foreach (var assembly in _assemblies)
            {
                Spend();
                if (!string.Equals(assembly.Assembly.Name.Name, simple, StringComparison.OrdinalIgnoreCase)) continue;
                loaded = true;
                var module = assembly.Assembly.MainModule;
                yield return module;
                foreach (var exported in module.ExportedTypes)
                {
                    Spend();
                    string fullName = exported.Namespace.Length == 0 ? exported.Name : exported.Namespace + "." + exported.Name;
                    if (exported.DeclaringType is null && string.Equals(fullName, name.TopLevelName, StringComparison.Ordinal)
                        && exported.Scope is AssemblyNameReference destination) next = destination.Name;
                }
            }
            if (!loaded && simple is "mscorlib" or "netstandard" or "System.Runtime")
            {
                foreach (var assembly in _assemblies)
                {
                    Spend();
                    if (assembly.Assembly.Name.Name == "System.Private.CoreLib") yield return assembly.Assembly.MainModule;
                }
                yield break;
            }
            if (next is null) yield break;
            simple = next;
        }
    }

    private static bool AsciiTypeName(string text)
    {
        foreach (char character in text)
            if (character > 0x7F) return false;
        return true;
    }

    private bool InterfaceNameIsUncertain(TypeReference contract, SummaryValue names, SummaryValue flags)
    {
        if (!flags.Integers.Any(value => (value & 1) != 0) || Resolve(contract) is not { } definition) return false;
        while (definition.DeclaringType is { } parent)
        {
            Spend();
            definition = parent;
        }
        foreach (string query in names.Strings)
        {
            Spend();
            int dot = query.LastIndexOf('.');
            if (dot >= 0 && (definition.Namespace.Length == 0
                || !string.Equals(definition.Namespace, query[..dot], StringComparison.OrdinalIgnoreCase))) continue;
            if (!AsciiTypeName(dot < 0 ? query : query[(dot + 1)..])) return true;
        }
        return false;
    }
}
