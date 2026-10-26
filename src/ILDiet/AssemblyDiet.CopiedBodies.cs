using Mono.Cecil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly Dictionary<TypeDefinition, bool> _copiedIntrinsicTypes = new();
    private readonly Dictionary<MethodDefinition, bool> _copiedBodyTraversal = new();

    private bool UsesReachedCopyTraversal(TypeDefinition type) => _byModule.TryGetValue(type.Module, out var assembly)
        && assembly.Copy && !_traversedCopies.Contains(type.Module);

    private bool TracksMethodBody(MethodDefinition method) => IsStripped(method.DeclaringType)
        || _traversedCopies.Contains(method.Module) || Node(method) is { State: var state } && (state & MethodState.Body) != 0;

    private bool CopiedIntrinsicType(TypeDefinition type)
    {
        if (_copiedIntrinsicTypes.TryGetValue(type, out bool known)) return known;
        Spend();
        string simple = type.Name;
        int tick = simple.IndexOf('`');
        if (tick >= 0) simple = simple[..tick];
        string bare = type.Namespace.Length == 0 ? simple : type.Namespace + "." + simple;
        // A generic vector's non-primitive element keeps real IL. Original-definition
        // analysis must allow that body without splitting closed argument contexts.
        bool intrinsic = CoreIntrinsicTypeMaps.IsIntrinsicType(Name(type))
            || CoreIntrinsicTypeMaps.IntrinsicGenericCppType(bare, primitiveElement: !type.HasGenericParameters) is not null
            || type.DeclaringType is { } parent && CoreIntrinsicTypeMaps.IsIntrinsicNested(Name(parent), type.Name);
        _copiedIntrinsicTypes.Add(type, intrinsic);
        return intrinsic;
    }

    private bool CopiedRuntimeOwnedType(TypeDefinition type) => CopiedIntrinsicType(type)
        || CoreIntrinsicTypeMaps.RuntimeInterfaceDispatch(Name(type)) is not null
        || CoreIntrinsicTypeMaps.IsDelegateBaseType(Name(type));

    private bool TraverseCopiedBody(MethodDefinition method)
    {
        if (_copiedBodyTraversal.TryGetValue(method, out bool known)) return known;
        Spend();
        string owner = Name(method.DeclaringType);
        bool traverse;
        if (CoreIntrinsicTypeMaps.IsMulticastAccessorMethod(owner, method.Name, method.IsStatic,
                method.GenericParameters.Count, method.Parameters.Count, method.ReturnType.FullName,
                method.Parameters.Count == 1 ? method.Parameters[0].ParameterType.FullName : null))
            traverse = false;
        else if (CoreIntrinsicTypeMaps.IsMulticastOperationMethod(owner, method.Name, method.IsStatic,
                method.GenericParameters.Count, method.Parameters.Count, method.ReturnType.FullName,
                method.Parameters.Count == 1 ? method.Parameters[0].ParameterType.FullName : null))
            traverse = false;
        else if (CoreIntrinsicTypeMaps.IsMulticastFactoryMethod(owner, method.Name, method.IsStatic,
                method.GenericParameters.Count, method.Parameters.Count, method.ReturnType.FullName,
                method.Parameters.Count >= 1 ? method.Parameters[0].ParameterType.FullName : null,
                method.Parameters.Count >= 2 ? method.Parameters[1].ParameterType.FullName : null,
                method.Parameters.Count == 3 ? method.Parameters[2].ParameterType.FullName : null))
            traverse = false;
        else if (CoreIntrinsicTypeMaps.IsDelegateValueMember(owner, method.Name)
            && CoreIntrinsicTypeMaps.IsDelegateValueMethod(owner, method.Name, method.IsStatic,
                method.GenericParameters.Count, method.Parameters.Count, method.ReturnType.FullName,
                method.Parameters.Count == 1 ? method.Parameters[0].ParameterType.FullName : null))
            traverse = false;
        else if (owner is "System.Array" or "System.Delegate")
            traverse = CoreIntrinsicTypeMaps.IsRealBodyGeneric(owner, method.Name) || CopiedArrayRealBodyMember(method);
        else
            traverse = !CoreIntrinsicTypeMaps.IsInlineLoweredPrimitiveMember(owner, method.Name)
                && !CoreIntrinsicTypeMaps.IsEnumInstanceFormatMethod(owner, method.Name)
                && !CoreIntrinsicTypeMaps.IsEnumElementTypeMethod(owner, method.Name,
                    method.IsStatic, method.Parameters.Count, method.ReturnType.FullName,
                    method.Parameters.Count == 1 ? method.Parameters[0].ParameterType.FullName : null)
                && !CoreIntrinsicTypeMaps.IsEnumUnderlyingTypeMethod(owner, method.Name,
                    method.IsStatic, method.Parameters.Count, method.ReturnType.FullName,
                    method.Parameters.Count == 1 ? method.Parameters[0].ParameterType.FullName : null)
                && !CoreIntrinsicTypeMaps.IsEnumScalarGetNameMethod(owner, method.Name,
                    method.IsStatic, method.Parameters.Count, method.ReturnType.FullName,
                    method.Parameters.Count == 2 ? method.Parameters[0].ParameterType.FullName : null,
                    method.Parameters.Count == 2 ? method.Parameters[1].ParameterType.FullName : null)
                && !CoreIntrinsicTypeMaps.IsEnumTryFormatMethod(owner, method.Name)
                && (!CopiedIntrinsicType(method.DeclaringType)
                    || CoreIntrinsicTypeMaps.IntrinsicManagedBodyFamily(owner)
                    || ClassifyReflectionTarget(method) is null && CopiedInterfaceImplementation(method));
        _copiedBodyTraversal.Add(method, traverse);
        return traverse;
    }

    private static bool CopiedArrayRealBodyMember(MethodDefinition method)
    {
        string owner = Name(method.DeclaringType);
        if (CoreIntrinsicTypeMaps.IsArrayRealBodyMember(owner, method.Name, hasInt64Parameter: false)) return true;
        if (!CoreIntrinsicTypeMaps.IsArrayRealBodyMember(owner, method.Name, hasInt64Parameter: true)) return false;
        foreach (var parameter in method.Parameters)
        {
            var type = parameter.ParameterType;
            while (type is OptionalModifierType or RequiredModifierType or PinnedType)
                type = ((TypeSpecification)type).ElementType;
            if (type.MetadataType == MetadataType.Int64
                || type is ArrayType { IsVector: true } array && RawTypeName(array.ElementType) == "System.Int64")
                return true;
        }
        return false;
    }

    private bool CopiedInterfaceImplementation(MethodDefinition method)
    {
        if (method.IsStatic || method.IsConstructor) return false;
        foreach (var declaration in method.Overrides)
            if (Resolve(declaration.DeclaringType) is { IsInterface: true }) return true;
        if (!method.IsPublic) return false;
        // Matching an actual declared interface slot admits a conservative managed
        // fallback. An unrelated virtual slot (notably ValueType.Equals) does not.
        var pending = new Stack<TypeDefinition>();
        var seen = new HashSet<TypeDefinition>();
        pending.Push(method.DeclaringType);
        while (pending.Count != 0)
        {
            var type = pending.Pop();
            if (!seen.Add(type)) continue;
            Spend();
            if (type.IsInterface)
                foreach (var declaration in type.Methods)
                {
                    Spend();
                    if (declaration.Name == method.Name && declaration.IsStatic == method.IsStatic
                        && declaration.GenericParameters.Count == method.GenericParameters.Count
                        && CopiedInterfaceSignature(declaration, method)) return true;
                }
            if (type.BaseType is { } baseType && Resolve(baseType) is { } parent) pending.Push(parent);
            foreach (var implementation in type.Interfaces)
                if (Resolve(implementation.InterfaceType) is { } contract) pending.Push(contract);
        }
        return false;
    }

    private bool CopiedInterfaceSignature(MethodDefinition declaration, MethodDefinition method)
    {
        if (declaration.Parameters.Count != method.Parameters.Count
            || !CopiedInterfaceSlotType(declaration.ReturnType, method.ReturnType)) return false;
        for (int i = 0; i < declaration.Parameters.Count; i++)
            if (!CopiedInterfaceSlotType(declaration.Parameters[i].ParameterType, method.Parameters[i].ParameterType))
                return false;
        return true;
    }

    private bool CopiedInterfaceSlotType(TypeReference declaration, TypeReference implementation)
    {
        Spend();
        if (declaration is GenericParameter || implementation is GenericParameter) return true;
        if (declaration is GenericInstanceType || implementation is GenericInstanceType)
        {
            if (declaration is not GenericInstanceType a || implementation is not GenericInstanceType b
                || a.GenericArguments.Count != b.GenericArguments.Count
                || !CopiedInterfaceSlotType(a.ElementType, b.ElementType)) return false;
            for (int i = 0; i < a.GenericArguments.Count; i++)
                if (!CopiedInterfaceSlotType(a.GenericArguments[i], b.GenericArguments[i])) return false;
            return true;
        }
        if (declaration is FunctionPointerType || implementation is FunctionPointerType)
        {
            if (declaration is not FunctionPointerType a || implementation is not FunctionPointerType b
                || a.CallingConvention != b.CallingConvention || a.HasThis != b.HasThis
                || a.ExplicitThis != b.ExplicitThis || a.Parameters.Count != b.Parameters.Count
                || !CopiedInterfaceSlotType(a.ReturnType, b.ReturnType)) return false;
            for (int i = 0; i < a.Parameters.Count; i++)
                if (!CopiedInterfaceSlotType(a.Parameters[i].ParameterType, b.Parameters[i].ParameterType)) return false;
            return true;
        }
        if (declaration is ArrayType || implementation is ArrayType)
        {
            return declaration is ArrayType a && implementation is ArrayType b
                && a.IsVector == b.IsVector && a.Rank == b.Rank
                && CopiedInterfaceSlotType(a.ElementType, b.ElementType);
        }
        if (declaration is IModifierType || implementation is IModifierType)
        {
            if (declaration is not IModifierType a || implementation is not IModifierType b
                || !CopiedInterfaceSlotType(a.ModifierType, b.ModifierType)) return false;
        }
        if (declaration is TypeSpecification || implementation is TypeSpecification)
        {
            return declaration is TypeSpecification a && implementation is TypeSpecification b
                && a.MetadataType == b.MetadataType && CopiedInterfaceSlotType(a.ElementType, b.ElementType);
        }
        return RawTypeName(declaration) == RawTypeName(implementation)
            && _metadataResolver.SameSignatureIdentity(declaration, implementation);
    }

    private void ApplyCopiedRuntimeType(TypeDefinition type)
    {
        // Copied metadata already survives byte-for-byte. Only executable owner
        // edges matter here; runtime-owned layouts do not run their BCL initializers.
        if (CopiedRuntimeOwnedType(type)) return;
        if (_initializesArrays) KeepArrayInitializer(type);
        MarkType(type.BaseType);
        foreach (var implementation in type.Interfaces) MarkType(implementation.InterfaceType);
        if (type.IsValueType)
            foreach (var field in type.Fields)
                if (!field.IsStatic) QueueDemand(field.FieldType, Demand.Boxed);
        if (IsDelegate(type))
        {
            _runtimeDelegates.Add(type);
            if (_delegateInvokeBoxesUsed) NoteDelegateInvokeBoxes(type);
        }
        // The CLI TypedReference primitive has no boxed object dispatch.
        bool implicitVirtuals = type.FullName != "System.TypedReference"
            || Resolve(type.Module.TypeSystem.TypedReference) != type;
        foreach (var method in type.Methods)
            if (method.IsConstructor && method.IsStatic
                || implicitVirtuals && (method.IsVirtual || method.HasOverrides
                    || type.IsInterface || IsDelegate(type))) MarkMethod(method);
    }
}
