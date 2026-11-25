using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal readonly Dictionary<MethodInfo, MethodInfo> UnsafeAccessorGetters = new();

    private void ReachUnsafeAccessorGetter(MethodInfo accessor)
    {
        if (UnsafeAccessorGetters.ContainsKey(accessor) || BindUnsafeAccessorGetter(accessor) is not { } target) return;
        GetClass(target.Module, target.Module.Reader.GetMethodDefinition(target.Method).GetDeclaringType()).EnsureMembers();
        var getter = GetMethod(target.Module, target.Method);
        UnsafeAccessorGetters.Add(accessor, getter);
        NoteInterceptFtnTarget(accessor);
        _predTrace.TryAdd(accessor, _currentScan);
        if (ResolveCallTarget(getter.Module, getter.Handle, getter.Context) is { } body)
        {
            _predTrace.TryAdd(body, accessor);
            Reach(body);
        }
    }

    private (Module Module, MethodDefinitionHandle Method)? BindUnsafeAccessorGetter(MethodInfo accessor) =>
        accessor.Rva == 0 && accessor.IsStatic && accessor.PInvoke is null && !accessor.Handle.IsNil
            ? UnsafeAccessorGetter.Bind(accessor.Module, accessor.Handle, static module => module.Reader, ResolveAccessorType,
                (module, type) => EnsureShape(GetClass(module, type)) is { IsValueType: false, IsEnum: false },
                (assembly, ns, name, definition) => AssemblyTypeResolver.ForwardsTo(assembly, ns, name, definition))
            : null;

    private void ValidateUnsafeAccessorGetterReference(MethodInfo method, BlobReader signature, MethodSignature<TypeDesc> decoded)
    {
        if (!UnsafeAccessorGetter.Receiver(signature).IsNil && decoded.ParameterTypes is [{ Class: { } receiver }]
            && method.Signature.ParameterTypes is [{ Class: { } declared }] && receiver == declared) return;
        if (BindUnsafeAccessorGetter(method) is not null)
            throw new NotSupportedException($"UnsafeAccessor {method.Name} requires its exact static signature and receiver type");
    }

    private (Module Module, TypeDefinitionHandle Type)? ResolveAccessorType(Module source, EntityHandle token)
    {
        if (token.Kind == HandleKind.TypeDefinition) return (source, (TypeDefinitionHandle)token);
        if (token.Kind == HandleKind.TypeReference
            && ResolveTypeRef(source, (TypeReferenceHandle)token) is { Class: { } type })
            return (type.Module, type.Handle);
        return null;
    }
}
