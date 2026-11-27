using System.Reflection.Metadata;
using AccessedMembers = System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<(Module, EntityHandle), (int Index, AccessedMembers Members)[]> _genericParameterContracts = new();
    private readonly HashSet<ClassInfo> _genericContractTypes = new();
    private readonly Queue<ClassInfo> _pendingGenericContractTypes = new();
    private readonly HashSet<(ClassInfo, AccessedMembers, bool)> _genericMemberDemands = new();
    private readonly Queue<(ClassInfo Owner, AccessedMembers Members, bool Inherited)> _pendingGenericParameterContracts = new();

    private void NoteGenericParameterContracts(MethodInfo method)
    {
        NoteGenericParameterContracts(method.DeclaringClass);
        if (method.Handle.IsNil)
            return;
        var signature = method.Signature;
        NoteGenericContractType(signature.ReturnType);
        foreach (var parameter in signature.ParameterTypes)
            NoteGenericContractType(parameter);
        var args = method.Context.MethodArgs;
        if (args.Length != 0)
            NoteGenericParameterContracts(method.Module, method.Handle, args);
    }

    private void NoteGenericParameterContracts(ClassInfo type)
    {
        if (!type.Handle.IsNil && _genericContractTypes.Add(type))
            _pendingGenericContractTypes.Enqueue(type);
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
            NoteGenericContractType(arg);
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

    private void NoteGenericContractType(TypeDesc type)
    {
        if (type.Class is { } cls)
            NoteGenericParameterContracts(cls);
        else if (type.FunctionSignature is { } function)
            foreach (var part in function.Types)
                NoteGenericContractType(part);
        else if (type.Element is { } element)
            NoteGenericContractType(element);
    }

    internal bool ApplyPendingGenericParameterContracts()
    {
        if (_pendingGenericContractTypes.Count == 0 && _pendingGenericParameterContracts.Count == 0)
            return false;
        while (_pendingGenericContractTypes.Count > 0)
        {
            // A newly named specialization learns its projected base and interfaces
            // when its shape is completed. Only reachable uses enter this queue.
            var type = EnsureShape(_pendingGenericContractTypes.Dequeue());
            if (type.GenericArity != 0)
                NoteGenericParameterContracts(type.Module, type.Handle, type.Context.TypeArgs);
            if (type.BaseClass is { } parent)
                NoteGenericParameterContracts(parent);
            foreach (var contract in type.Interfaces)
                NoteGenericParameterContracts(contract);
        }
        while (_pendingGenericParameterContracts.Count > 0)
        {
            var request = _pendingGenericParameterContracts.Dequeue();
            ReachAccessedMembers(request.Owner, request.Members, request.Inherited);
        }
        return true;
    }
}
