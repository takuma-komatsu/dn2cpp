using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal readonly Dictionary<MethodInfo, (MethodInfo? Getter, FieldInfo? Field)> UnsafeAccessorTargets = new();

    private void ReachUnsafeAccessor(MethodInfo accessor)
    {
        if (UnsafeAccessorTargets.ContainsKey(accessor) || BindUnsafeAccessor(accessor) is not { } target) return;
        if (target.Member.Kind == HandleKind.FieldDefinition)
        {
            var field = (FieldDefinitionHandle)target.Member;
            var owner = GetClass(target.Module, target.Module.Reader.GetFieldDefinition(field).GetDeclaringType());
            // A native-owned layout need not contain the fields declared in the managed image.
            if (owner.IntrinsicCppName is not null || CoreIntrinsics.IsIntrinsicType(owner.FullName) || owner.IsDelegate) return;
            owner.EnsureMembers();
            UnsafeAccessorTargets.Add(accessor, (null, owner.Fields.First(f => f.Handle == field)));
        }
        else
        {
            var method = (MethodDefinitionHandle)target.Member;
            GetClass(target.Module, target.Module.Reader.GetMethodDefinition(method).GetDeclaringType()).EnsureMembers();
            var getter = GetMethod(target.Module, method);
            UnsafeAccessorTargets.Add(accessor, (getter, null));
            if (ResolveCallTarget(getter.Module, getter.Handle, getter.Context) is { } body)
            {
                _predTrace.TryAdd(body, accessor);
                Reach(body);
            }
        }
        NoteInterceptFtnTarget(accessor);
        _predTrace.TryAdd(accessor, _currentScan);
    }

    private (Module Module, EntityHandle Member)? BindUnsafeAccessor(MethodInfo accessor) =>
        accessor.Rva == 0 && accessor.IsStatic && accessor.PInvoke is null && !accessor.Handle.IsNil
            ? UnsafeAccessorBinding.Bind(accessor.Module, accessor.Handle, static module => module.Reader, ResolveAccessorType,
                (module, type) => EnsureShape(GetClass(module, type)) switch
                {
                    { IsEnum: true } => UnsafeAccessorBinding.TypeShape.Enum,
                    { IsValueType: true } => UnsafeAccessorBinding.TypeShape.Value,
                    _ => UnsafeAccessorBinding.TypeShape.Reference,
                },
                (assembly, name) => AssemblyTypeResolver.ResolveNamed(accessor.Module, assembly, name, AccessorTypeCandidates),
                (assembly, ns, name, definition) => AssemblyTypeResolver.ForwardsTo(assembly, ns, name, definition))
            : null;

    private void ValidateUnsafeAccessorReference(Module caller, MemberReference reference, MethodInfo method, MethodSignature<TypeDesc> decoded)
    {
        var signature = caller.Reader.GetBlobReader(reference.Signature);
        if (!UnsafeAccessorBinding.GetterReceiver(signature).IsNil && decoded.ParameterTypes is [{ Class: { } receiver }]
            && method.Signature.ParameterTypes is [{ Class: { } declared }] && receiver == declared) return;
        var fieldEnum = UnsafeAccessorBinding.FieldEnum(signature);
        if (!fieldEnum.IsNil && decoded.ParameterTypes is [{ IsObject: true }]
            && method.Signature.ParameterTypes is [{ IsObject: true }] && decoded.ReturnType is { Kind: TypeKind.ByRef, Element.Class: { } result }
            && method.Signature.ReturnType is { Kind: TypeKind.ByRef, Element.Class: { } expected } && result == expected
            && ResolveAccessorType(caller, reference.Parent, true) is not null
            && ResolveAccessorType(caller, fieldEnum, true) is { } incoming
            && incoming.Module == expected.Module && incoming.Type == expected.Handle) return;
        if (BindUnsafeAccessor(method) is not null)
            throw new NotSupportedException($"UnsafeAccessor {method.Name} requires its exact static signature and receiver type");
    }

    private IEnumerable<(Module Module, TypeDefinitionHandle Handle)> AccessorTypeCandidates(string ns, string name) =>
        TypeIndex().TryGetValue((ns, name), out var candidates) ? candidates : [];

    private (Module Module, TypeDefinitionHandle Type)? ResolveAccessorType(Module source, EntityHandle token, bool checkReferences)
    {
        if (checkReferences) return AssemblyTypeResolver.ResolveType(source, token, AccessorTypeCandidates, true);
        if (token.Kind == HandleKind.TypeDefinition) return (source, (TypeDefinitionHandle)token);
        if (token.Kind == HandleKind.TypeReference
            && ResolveTypeRef(source, (TypeReferenceHandle)token) is { Class: { } type })
            return (type.Module, type.Handle);
        return null;
    }
}
