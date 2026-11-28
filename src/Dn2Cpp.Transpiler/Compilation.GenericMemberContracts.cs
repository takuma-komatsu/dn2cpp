using System.Reflection.Metadata;
using AccessedMembers = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<(Module, EntityHandle), (int Index, AccessedMembers Members)[]> _genericParameterContracts = new();
    private readonly Dictionary<(Module, TypeDefinitionHandle), AccessedMembers> _typeMemberContracts = new();
    private readonly HashSet<ClassInfo> _contractTypes = new();
    private readonly Queue<ClassInfo> _pendingContractTypes = new();
    private readonly HashSet<(ClassInfo, AccessedMembers, bool)> _genericMemberDemands = new();
    private readonly Queue<(ClassInfo Owner, AccessedMembers Members, bool Inherited)> _pendingGenericParameterContracts = new();

    private void NoteMethodContracts(MethodInfo method)
    {
        NoteTypeContracts(method.DeclaringClass);
        if (method.Handle.IsNil)
            return;
        var signature = method.Signature;
        NoteTypeContracts(signature.ReturnType);
        foreach (var parameter in signature.ParameterTypes)
            NoteTypeContracts(parameter);
        var args = method.Context.MethodArgs;
        if (args.Length != 0)
            NoteGenericParameterContracts(method.Module, method.Handle, args);
    }

    private void NoteTypeContracts(ClassInfo type)
    {
        if (!type.Handle.IsNil && _contractTypes.Add(type))
            _pendingContractTypes.Enqueue(type);
    }

    private void NoteDeclaringTypeContracts(ClassInfo nested)
    {
        // DeclaringType exposes the enclosing definition even when no call names it.
        var module = nested.Module;
        var row = module.Reader.GetTypeDefinition(nested.Handle);
        for (var owner = row.GetDeclaringType(); !owner.IsNil; owner = module.Reader.GetTypeDefinition(owner).GetDeclaringType())
            if (module.ClassMap.TryGetValue(owner, out var declaring) && TypeMemberContract(module, owner) != AccessedMembers.None)
                NoteForceEmit(declaring);
    }

    private AccessedMembers TypeMemberContract(Module module, TypeDefinitionHandle handle)
    {
        var key = (module, handle);
        if (_typeMemberContracts.TryGetValue(key, out var members))
            return members;
        var reader = module.Reader;
        var type = reader.GetTypeDefinition(handle);
        members = ReflectionCoreContracts.GetDeclaredTypeAccessedMembers(reader, handle);
        _typeMemberContracts.Add(key, members);
        // Substitution changes member signatures, but each annotation belongs to a definition.
        if (ResolveTypeDefRefSpec(module, type.BaseType) is ({ } baseModule, var baseType))
            members |= TypeMemberContract(baseModule, baseType);
        foreach (var row in type.GetInterfaceImplementations())
            if (ResolveTypeDefRefSpec(module, reader.GetInterfaceImplementation(row).Interface) is ({ } itfModule, var itf))
                members |= TypeMemberContract(itfModule, itf);
        _typeMemberContracts[key] = members;
        return members;
    }

    private void NoteGenericParameterContracts(Module module, EntityHandle owner, TypeDesc[] args)
    {
        // Canonical instances may share a body, but only a concrete argument
        // carries a preservation obligation. Model formation alone is not use.
        if (!_genericParameterContracts.TryGetValue((module, owner), out var contracts))
        {
            var reader = module.Reader;
            var parameters = owner.Kind == HandleKind.TypeDefinition
                ? reader.GetTypeDefinition((TypeDefinitionHandle)owner).GetGenericParameters()
                : reader.GetMethodDefinition((MethodDefinitionHandle)owner).GetGenericParameters();
            List<(int, AccessedMembers)>? annotated = null;
            foreach (var handle in parameters)
            {
                var row = reader.GetGenericParameter(handle);
                var members = ReflectionCoreContracts.ReadAccessedMembers(reader, row.GetCustomAttributes());
                if (members != AccessedMembers.None)
                    (annotated ??= new()).Add((row.Index, members));
            }
            _genericParameterContracts[(module, owner)] = contracts = annotated?.ToArray()
                ?? Array.Empty<(int, AccessedMembers)>();
        }

        foreach (var arg in args)
            NoteTypeContracts(arg);
        foreach (var (index, members) in contracts)
        {
            if ((uint)index >= (uint)args.Length)
                continue;
            var arg = args[index];
            if (ContainsCanonPlaceholder(arg) || ContainsGenericVar(arg)
                || AccessedTypeOwner(arg, out bool inherited) is not { } target)
                continue;
            var demand = (target, members, inherited);
            if (_genericMemberDemands.Add(demand))
                _pendingGenericParameterContracts.Enqueue(demand);
        }
    }

    private void NoteTypeContracts(TypeDesc type)
    {
        if (type.Class is { } cls)
            NoteTypeContracts(cls);
        else if (type.FunctionSignature is { } function)
            foreach (var part in function.Types)
                NoteTypeContracts(part);
        else if (type.Element is { } element)
            NoteTypeContracts(element);
    }

    internal bool ApplyPendingMemberContracts()
    {
        if (_pendingContractTypes.Count == 0 && _pendingGenericParameterContracts.Count == 0)
            return false;
        while (_pendingContractTypes.Count > 0)
        {
            // A newly named specialization learns its projected base and interfaces
            // when its shape is completed. Only reachable uses enter this queue.
            var type = EnsureShape(_pendingContractTypes.Dequeue());
            if (type.GenericArity != 0)
                NoteGenericParameterContracts(type.Module, type.Handle, type.Context.TypeArgs);
            if (type.BaseClass is { } parent)
                NoteTypeContracts(parent);
            foreach (var contract in type.Interfaces)
                NoteTypeContracts(contract);
            var members = TypeMemberContract(type.Module, type.Handle);
            if (members != AccessedMembers.None)
                ReachAccessedMembers(type, members);
        }
        while (_pendingGenericParameterContracts.Count > 0)
        {
            var request = _pendingGenericParameterContracts.Dequeue();
            ReachAccessedMembers(request.Owner, request.Members, request.Inherited);
        }
        return true;
    }
}
