using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly HashSet<(MethodInfo Factory, bool BindingOnly)> _arraySortFactoryClosures = new();

    // The factory's real guard admits exactly these constrained instantiations.
    // Work on each closed owner, before sharing erases its argument identities.
    private void ReachArraySortFactory(MethodInfo factory)
    {
        var owner = factory.DeclaringClass;
        if (factory.Name != "CreateArraySortHelper" || owner.Module.AssemblyName != "System.Private.CoreLib"
            || factory.Module != owner.Module || factory.Handle.IsNil || owner.Handle.IsNil
            || factory.IsSynthetic || owner.IsValueType || owner.IsEnum || owner.IsInterface || owner.IsByRefLike
            || factory.Attributes != (MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig)
            || factory.ImplAttributes != MethodImplAttributes.IL || factory.Rva == 0 || factory.PInvoke is not null)
            return;
        var reader = owner.Module.Reader;
        var definition = reader.GetTypeDefinition(owner.Handle);
        if (!definition.GetDeclaringType().IsNil
            || !reader.StringComparer.Equals(definition.Namespace, "System.Collections.Generic"))
            return;
        int arity = reader.StringComparer.Equals(definition.Name, "ArraySortHelper`1") ? 1
            : reader.StringComparer.Equals(definition.Name, "ArraySortHelper`2") ? 2 : 0;
        if (arity == 0 || owner.Context.TypeArgs.Length != arity || ContainsCanonPlaceholder(owner)
            || ContainsGenericVar(owner) || _arraySortFactoryClosures.Contains((factory, _reachingBindingOnlyBody)))
            return;
        var method = reader.GetMethodDefinition(factory.Handle);
        if (method.GetDeclaringType() != owner.Handle || method.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(method.Name, factory.Name)
            || method.Attributes != factory.Attributes || method.ImplAttributes != factory.ImplAttributes
            || method.RelativeVirtualAddress != factory.Rva || definition.GetGenericParameters().Count != arity
            || (definition.Attributes & TypeAttributes.Interface) != 0 || owner.GenericArity != arity
            || factory.Context.MethodArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || factory.Context.TypeArgs.Length != arity
            || !IsArraySortFactorySignature(owner.Module, reader.GetBlobReader(method.Signature), arity, out var contract)
            || factory.Signature.Header.RawValue != 0 || factory.Signature.GenericParameterCount != 0
            || factory.Signature.RequiredParameterCount != 0 || factory.Signature.ParameterTypes.Length != 0
            || factory.Signature.ReturnType is not { Kind: TypeKind.Class, Class: { } result }
            || result.Module != owner.Module || result.Handle != contract || !result.IsInterface
            || result.GenericArity != arity || result.Context.TypeArgs.Length != arity)
            throw new NotSupportedException("ArraySortHelper factory requires its parameterless CoreLib declaration");
        for (int i = 0; i < arity; i++)
            if (factory.Context.TypeArgs[i] != owner.Context.TypeArgs[i]
                || result.Context.TypeArgs[i] != owner.Context.TypeArgs[i])
                throw new NotSupportedException("ArraySortHelper factory requires its original closed context");
        string name = arity == 1 ? "GenericArraySortHelper`1" : "GenericArraySortHelper`2";
        TypeDefinitionHandle target = default;
        foreach (var handle in reader.TypeDefinitions)
        {
            var candidate = reader.GetTypeDefinition(handle);
            if (candidate.GetDeclaringType().IsNil
                && reader.StringComparer.Equals(candidate.Namespace, "System.Collections.Generic")
                && reader.StringComparer.Equals(candidate.Name, name))
            {
                target = handle;
                break;
            }
        }
        if (target.IsNil)
            throw new NotSupportedException("ArraySortHelper's constrained helper definition is not retained");
        var arguments = owner.Context.TypeArgs;
        bool? admitted = GenericArgumentsAdmit(owner.Module, reader.GetTypeDefinition(target).GetGenericParameters(),
            arguments, Array.Empty<TypeDesc>());
        if (admitted == false)
        {
            _arraySortFactoryClosures.Add((factory, _reachingBindingOnlyBody));
            return;
        }
        if (admitted is null)
            throw new NotSupportedException("ArraySortHelper's optimized generic constraints cannot be proved for " + owner.FullName);
        var helper = StaticReflectionType(owner.Module, target, arguments)
            ?? throw new InvalidOperationException("proved ArraySortHelper instantiation was not materialized");
        EnsureCompleted(helper);
        var ctor = ParameterlessCtor(helper)
            ?? throw new NotSupportedException("ArraySortHelper's optimized helper has no default constructor");
        if (helper.IsAbstract || helper.IsInterface || helper.IsValueType || helper.IntrinsicCppName is not null
            || ctor.Rva == 0 || _backend?.ShouldSkipMethodBody(helper, ctor) == true)
            throw new NotSupportedException("ArraySortHelper's optimized helper is not executable");
        NoteTypeIdentityClosure(TypeDesc.MakeClass(helper));
        ConstructSelected(ctor, factory);
        _arraySortFactoryClosures.Add((factory, _reachingBindingOnlyBody));
    }

    private static bool IsArraySortFactorySignature(Module module, BlobReader signature, int arity,
        out TypeDefinitionHandle contract)
    {
        contract = default;
        if (signature.RemainingBytes < 5 || signature.ReadByte() != 0 || signature.ReadByte() != 0
            || signature.ReadByte() != 0x15 || signature.ReadByte() != 0x12
            || !signature.TryReadCompressedInteger(out int token) || token <= 0 || (token & 3) != 0
            || token >> 2 == 0 || token >> 2 > module.Reader.TypeDefinitions.Count)
            return false;
        contract = SRME.TypeDefinitionHandle(token >> 2);
        var definition = module.Reader.GetTypeDefinition(contract);
        if (!definition.GetDeclaringType().IsNil || definition.GetGenericParameters().Count != arity
            || (definition.Attributes & TypeAttributes.Interface) == 0
            || !module.Reader.StringComparer.Equals(definition.Namespace, "System.Collections.Generic")
            || !module.Reader.StringComparer.Equals(definition.Name, arity == 1 ? "IArraySortHelper`1" : "IArraySortHelper`2")
            || signature.RemainingBytes == 0 || signature.ReadByte() != arity)
            return false;
        for (int i = 0; i < arity; i++)
            if (signature.RemainingBytes < 2 || signature.ReadByte() != 0x13 || signature.ReadByte() != i)
                return false;
        return signature.RemainingBytes == 0;
    }
}
