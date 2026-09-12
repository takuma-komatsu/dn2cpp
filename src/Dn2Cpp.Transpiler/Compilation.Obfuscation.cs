using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private HashSet<string>? _obfuscationMemberCandidates;

    private void NoteObfuscationCall(Module module, EntityHandle handle, GenericContext context)
    {
        if (!ObfuscationEnabled)
            return;
        EntityHandle method = handle.Kind == HandleKind.MethodSpecification
            ? module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method : handle;
        if (method.Kind == HandleKind.MethodDefinition)
        {
            if (module.MethodMap.TryGetValue((MethodDefinitionHandle)method, out var definition)
                && definition.HasObfuscateAttribute)
                NoteObfuscationMethod(handle.Kind == HandleKind.MethodSpecification
                    ? ResolveMethodSpec(module, (MethodSpecificationHandle)handle, context) : definition);
            return;
        }
        if (method.Kind != HandleKind.MemberReference)
            return;
        if (_obfuscationMemberCandidates is null)
        {
            _obfuscationMemberCandidates = new HashSet<string>(StringComparer.Ordinal);
            // Raw metadata is the name gate: inspecting attributes must not decode
            // unrelated signatures or root methods before the ordinary reachability cuts.
            foreach (var loaded in Modules)
            {
                var reader = loaded.Reader;
                foreach (var definitionHandle in reader.MethodDefinitions)
                {
                    var definition = reader.GetMethodDefinition(definitionHandle);
                    foreach (var attribute in definition.GetCustomAttributes())
                    {
                        if (AttributeTypeName(reader, reader.GetCustomAttribute(attribute))
                            != "Dn2Cpp.Runtime.ObfuscateAttribute")
                            continue;
                        _obfuscationMemberCandidates.Add(
                            RawSignatureProvider.TypeDefinitionName(reader, definition.GetDeclaringType())
                            + "::" + reader.GetString(definition.Name));
                        break;
                    }
                }
            }
        }
        var reference = module.Reader.GetMemberReference((MemberReferenceHandle)method);
        string? owner = reference.Parent.Kind switch
        {
            HandleKind.TypeReference => RawSignatureProvider.TypeReferenceName(module.Reader,
                (TypeReferenceHandle)reference.Parent),
            HandleKind.TypeSpecification => module.Reader.GetTypeSpecification(
                (TypeSpecificationHandle)reference.Parent).DecodeSignature(RawSignatureProvider.Instance, null),
            _ => null,
        };
        if (owner is null)
            return;
        if (reference.Parent.Kind == HandleKind.TypeSpecification)
        {
            int arguments = owner.IndexOf('<');
            if (arguments >= 0)
                owner = owner[..arguments];
        }
        if (!_obfuscationMemberCandidates.Contains(owner + "::" + module.Reader.GetString(reference.Name)))
            return;
        // Resolve only a candidate's exact overload. Record it without Reach: even
        // a replaced body must be diagnosed, while its callees remain stripped.
        var target = handle.Kind == HandleKind.MethodSpecification
            ? ResolveMethodSpec(module, (MethodSpecificationHandle)handle, context)
            : ResolveMemberRefMethod(module, (MemberReferenceHandle)method, context);
        NoteObfuscationMethod(target);
    }
}
