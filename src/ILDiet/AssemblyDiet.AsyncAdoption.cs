using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using CecilType = Mono.Cecil.TypeDefinition;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private sealed class OriginalAsyncCandidate
    {
        internal required int Order;
        internal required RawAsyncAdoption.AsyncAdoptionCandidate Raw;
        internal required CecilType Task, Builder;
        internal required CecilType[] Roles;
    }

    private Dictionary<CecilType, OriginalAsyncCandidate>? _originalAsyncTasks;
    private readonly Dictionary<CecilType, List<OriginalAsyncCandidate>> _originalAsyncByRow = new();
    private readonly Dictionary<string, List<OriginalAsyncCandidate>> _originalAsyncByAssembly =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<CecilType, List<RawAsyncAdoption.ContractReference>> _originalAsyncReferences = new();
    private readonly Dictionary<(string Namespace, string Name), CecilType?> _originalAsyncBuilderWinners = new();

    private Dictionary<CecilType, OriginalAsyncCandidate> OriginalAsyncTasks()
    {
        if (_originalAsyncTasks is not null) return _originalAsyncTasks;
        var modules = _assemblies.Select(assembly => new RawAsyncModule
        {
            Index = assembly.Index, AssemblyName = assembly.Assembly.Name.Name,
            Reader = assembly.PE.GetMetadataReader(),
        }).ToArray();
        var resolver = new OriginalAsyncResolver(modules, Spend);
        var classifier = new RawAsyncAdoption(modules, resolver.Resolve, resolver.Serialized,
            OriginalAsyncResolver.AttributeName, step: Spend, captureEvidence: true);
        classifier.Classify(Array.Empty<string>());
        var alwaysDeclined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var witness in classifier.DeclineWitnesses)
        {
            Spend();
            if (witness.RejectsEveryRole) alwaysDeclined.Add(witness.AssemblyName);
        }
        var result = new Dictionary<CecilType, OriginalAsyncCandidate>();
        for (int order = 0; order < classifier.OriginalCandidates.Count; order++)
        {
            Spend();
            var raw = classifier.OriginalCandidates[order];
            var roles = new List<CecilType>();
            foreach (var row in raw.Rows)
            {
                Spend();
                roles.Add(Definition(row.Module, row.Handle));
            }
            var candidate = new OriginalAsyncCandidate
            {
                Order = order, Raw = raw, Task = roles[0], Builder = roles[1], Roles = roles.ToArray(),
            };
            if (!_originalAsyncByAssembly.TryGetValue(raw.AssemblyName, out var siblings))
                _originalAsyncByAssembly.Add(raw.AssemblyName, siblings = new());
            siblings.Add(candidate);
            foreach (var role in candidate.Roles)
            {
                Spend();
                if (!_originalAsyncByRow.TryGetValue(role, out var owners))
                    _originalAsyncByRow.Add(role, owners = new());
                owners.Add(candidate);
            }
            // Opting out a conflicting candidate can enable another task.
            // Only a witness rejected in every role rules out all such subsets.
            if (raw.IsStruct && raw.TaskArity == 1 && !alwaysDeclined.Contains(raw.AssemblyName))
                result.TryAdd(candidate.Task, candidate);
        }
        foreach (var reference in classifier.ContractReferences)
        {
            Spend();
            var target = Definition(reference.TargetModule, reference.Target);
            if (!_originalAsyncReferences.TryGetValue(target, out var references))
                _originalAsyncReferences.Add(target, references = new());
            references.Add(reference);
        }
        foreach (var lookup in resolver.SerializedWinners)
        {
            Spend();
            _originalAsyncBuilderWinners.Add((lookup.Namespace, lookup.Name),
                lookup.Module is { } module ? Definition(module, lookup.Handle) : null);
        }
        _originalAsyncTasks = result;
        return result;

        CecilType Definition(RawAsyncModule module, TypeDefinitionHandle handle) =>
            (CecilType)_assemblies[module.Index].Assembly.MainModule.LookupToken(MetadataTokens.GetToken(handle));
    }

    // Match the primary's original TypeRef scope, forwarder and serialized-name
    // rules without constructing ClassInfo or asking Cecil to broaden a scope.
    private sealed class OriginalAsyncResolver
    {
        private readonly IReadOnlyList<RawAsyncModule> _modules;
        private readonly Action _step;
        private readonly Dictionary<(string Namespace, string Name),
            List<(RawAsyncModule Module, TypeDefinitionHandle Handle)>> _types = new();
        private readonly Dictionary<string, string> _assemblies = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<(string Assembly, string Namespace, string Name),
            (RawAsyncModule Module, AssemblyReferenceHandle Scope)> _forwarders = new();
        private readonly HashSet<(string Namespace, string Name)> _serializedNames = new();
        internal readonly List<(string Namespace, string Name, RawAsyncModule? Module,
            TypeDefinitionHandle Handle)> SerializedWinners = new();

        internal OriginalAsyncResolver(IReadOnlyList<RawAsyncModule> modules, Action step)
        {
            _modules = modules;
            _step = step;
            foreach (var module in modules)
            {
                _assemblies.TryAdd(module.AssemblyName, module.AssemblyName);
                var reader = module.Reader;
                foreach (var handle in reader.TypeDefinitions)
                {
                    step();
                    var type = reader.GetTypeDefinition(handle);
                    var key = (reader.GetString(type.Namespace), reader.GetString(type.Name));
                    if (!_types.TryGetValue(key, out var definitions)) _types.Add(key, definitions = new());
                    definitions.Add((module, handle));
                }
                foreach (var handle in reader.ExportedTypes)
                {
                    step();
                    var type = reader.GetExportedType(handle);
                    if (type.Implementation.Kind == HandleKind.AssemblyReference)
                        _forwarders.TryAdd((module.AssemblyName, reader.GetString(type.Namespace),
                            reader.GetString(type.Name)), (module, (AssemblyReferenceHandle)type.Implementation));
                }
            }
        }

        internal (RawAsyncModule Module, TypeDefinitionHandle Handle)? Serialized(string serialized)
        {
            int comma = serialized.IndexOf(',');
            string full = (comma >= 0 ? serialized[..comma] : serialized).Trim();
            if (full.Contains('+')) return null;
            int dot = full.LastIndexOf('.');
            var key = dot >= 0 ? (full[..dot], full[(dot + 1)..]) : ("", full);
            (RawAsyncModule Module, TypeDefinitionHandle Handle)? winner =
                _types.TryGetValue(key, out var definitions) && definitions.Count > 0 ? definitions[0] : null;
            if (_serializedNames.Add(key))
            {
                _step();
                SerializedWinners.Add((key.Item1, key.Item2, winner?.Module, winner?.Handle ?? default));
            }
            return winner;
        }

        internal (RawAsyncModule Module, TypeDefinitionHandle Handle)? Resolve(
            RawAsyncModule from, TypeReferenceHandle handle)
        {
            _step();
            var reader = from.Reader;
            var reference = reader.GetTypeReference(handle);
            string ns = reader.GetString(reference.Namespace), name = reader.GetString(reference.Name);
            if (!_types.TryGetValue((ns, name), out var candidates)) return null;
            if (reference.ResolutionScope.Kind == HandleKind.TypeReference)
            {
                if (Resolve(from, (TypeReferenceHandle)reference.ResolutionScope) is not { } parent) return null;
                foreach (var candidate in candidates)
                    if (candidate.Module == parent.Module
                        && candidate.Module.Reader.GetTypeDefinition(candidate.Handle).GetDeclaringType() == parent.Handle)
                        return candidate;
                return null;
            }
            if (reference.ResolutionScope.Kind == HandleKind.AssemblyReference)
            {
                var scope = (AssemblyReferenceHandle)reference.ResolutionScope;
                string assembly = reader.GetString(reader.GetAssemblyReference(scope).Name);
                return ResolveAssembly(assembly, ns, name, candidates, from, scope);
            }
            if (reference.ResolutionScope.Kind == HandleKind.ModuleDefinition || reference.ResolutionScope.IsNil)
                foreach (var candidate in candidates)
                    if (candidate.Module == from
                        && from.Reader.GetTypeDefinition(candidate.Handle).GetDeclaringType().IsNil)
                        return candidate;
            return null;
        }

        private (RawAsyncModule Module, TypeDefinitionHandle Handle)? ResolveAssembly(
            string assembly, string ns, string name,
            List<(RawAsyncModule Module, TypeDefinitionHandle Handle)> candidates,
            RawAsyncModule scopeModule, AssemblyReferenceHandle scope)
        {
            for (int hops = 0; hops <= _modules.Count; hops++)
            {
                _step();
                if (_assemblies.TryGetValue(assembly, out var canonical)) assembly = canonical;
                foreach (var candidate in candidates)
                    if (candidate.Module.AssemblyName.Equals(assembly, StringComparison.OrdinalIgnoreCase)
                        && candidate.Module.Reader.GetTypeDefinition(candidate.Handle).GetDeclaringType().IsNil)
                        return candidate;
                if (_forwarders.TryGetValue((assembly, ns, name), out var forwarded))
                {
                    scopeModule = forwarded.Module;
                    scope = forwarded.Scope;
                    assembly = scopeModule.Reader.GetString(scopeModule.Reader.GetAssemblyReference(scope).Name);
                    continue;
                }
                if (!_assemblies.ContainsKey(assembly) && SignedFacade(scopeModule, scope, assembly))
                {
                    string target = PrivateUri(scopeModule, scope, assembly, ns, name)
                        ? "System.Private.Uri" : "System.Private.CoreLib";
                    foreach (var candidate in candidates)
                        if (candidate.Module.AssemblyName == target
                            && candidate.Module.Reader.GetTypeDefinition(candidate.Handle).GetDeclaringType().IsNil)
                            return candidate;
                }
                return null;
            }
            return null;
        }

        private static bool PrivateUri(RawAsyncModule module, AssemblyReferenceHandle scope,
            string assembly, string ns, string name)
        {
            bool runtime = assembly.Equals("System.Runtime", StringComparison.OrdinalIgnoreCase);
            if (ns != "System" || (!runtime && !assembly.Equals("netstandard", StringComparison.OrdinalIgnoreCase)))
                return false;
            var reference = module.Reader.GetAssemblyReference(scope);
            var token = module.Reader.GetBlobReader(reference.PublicKeyOrToken);
            if (token.Length != 8 || token.ReadUInt64() != (runtime
                ? 0x3a0ad5117f5f3fb0UL : 0x51dd2dcdff137bccUL)) return false;
            return name is "FileStyleUriParser" or "FtpStyleUriParser" or "GenericUriParser"
                or "GenericUriParserOptions" or "GopherStyleUriParser" or "HttpStyleUriParser"
                or "LdapStyleUriParser" or "NetPipeStyleUriParser" or "NetTcpStyleUriParser"
                or "NewsStyleUriParser" or "Uri" or "UriBuilder" or "UriComponents"
                or "UriFormat" or "UriFormatException" or "UriHostNameType" or "UriKind"
                or "UriParser" or "UriPartial" || (runtime && name == "UriCreationOptions");
        }

        private static bool SignedFacade(RawAsyncModule module, AssemblyReferenceHandle scope, string assembly)
        {
            if (!assembly.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
                && !assembly.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
                && !assembly.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)) return false;
            var reference = module.Reader.GetAssemblyReference(scope);
            var token = module.Reader.GetBlobReader(reference.PublicKeyOrToken);
            return token.Length == 8 && token.ReadUInt64() is 0x3a0ad5117f5f3fb0UL or 0x51dd2dcdff137bccUL
                or 0x8e79a7bed785ec7cUL or 0x89e03419565c7ab7UL;
        }

        internal static string? AttributeName(MetadataReader reader, CustomAttribute attribute)
        {
            EntityHandle owner = attribute.Constructor.Kind switch
            {
                HandleKind.MethodDefinition => reader.GetMethodDefinition(
                    (MethodDefinitionHandle)attribute.Constructor).GetDeclaringType(),
                HandleKind.MemberReference => reader.GetMemberReference(
                    (MemberReferenceHandle)attribute.Constructor).Parent,
                _ => default,
            };
            StringHandle ns, name;
            if (owner.Kind == HandleKind.TypeDefinition)
            {
                var type = reader.GetTypeDefinition((TypeDefinitionHandle)owner);
                ns = type.Namespace; name = type.Name;
            }
            else if (owner.Kind == HandleKind.TypeReference)
            {
                var type = reader.GetTypeReference((TypeReferenceHandle)owner);
                ns = type.Namespace; name = type.Name;
            }
            else return null;
            string space = reader.GetString(ns), simple = reader.GetString(name);
            return space.Length == 0 ? simple : space + "." + simple;
        }
    }
}
