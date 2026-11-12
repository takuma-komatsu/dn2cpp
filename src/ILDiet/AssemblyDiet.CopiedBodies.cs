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

    private static bool IsCopiedPrimitiveBigMul(MethodDefinition method, string owner)
    {
        if (method.Name != "BigMul" || !CoreIntrinsicTypeMaps.IsPrimitiveBigMulOwner(owner)
            || method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || method.DeclaringType.DeclaringType is not null || !method.DeclaringType.IsValueType
            || method.DeclaringType.IsEnum || method.DeclaringType.HasGenericParameters
            || method.Attributes != (MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || !method.HasBody
            || method.HasThis || method.ExplicitThis || method.CallingConvention != MethodCallingConvention.Default
            || method.HasGenericParameters || method.HasPInvokeInfo) return false;
        var code = owner switch {
            "System.Int32" => MetadataType.Int32, "System.UInt32" => MetadataType.UInt32,
            "System.Int64" => MetadataType.Int64, "System.UInt64" => MetadataType.UInt64,
            "System.IntPtr" => MetadataType.IntPtr, _ => MetadataType.UIntPtr };
        bool native = owner is "System.IntPtr" or "System.UIntPtr";
        if (method.Parameters.Count != (native ? 3 : 2)
            || method.Parameters[0].ParameterType is TypeSpecification
            || method.Parameters[1].ParameterType is TypeSpecification
            || method.Parameters[0].ParameterType.MetadataType != code
            || method.Parameters[1].ParameterType.MetadataType != code
            || method.Parameters[0].ParameterType.FullName != owner
            || method.Parameters[1].ParameterType.FullName != owner) return false;
        if (native) return method.ReturnType.MetadataType == code && method.ReturnType.FullName == owner
            && method.Parameters[2].Attributes == ParameterAttributes.Out
            && method.Parameters[2].ParameterType is ByReferenceType byref
            && byref.ElementType.MetadataType == code && byref.ElementType.FullName == owner;
        string result = owner switch {
            "System.Int32" => "System.Int64", "System.UInt32" => "System.UInt64",
            "System.Int64" => "System.Int128", _ => "System.UInt128" };
        if (method.ReturnType is TypeSpecification || method.ReturnType.FullName != result) return false;
        if (owner is "System.Int32" or "System.UInt32") return method.ReturnType.MetadataType ==
            (owner == "System.Int32" ? MetadataType.Int64 : MetadataType.UInt64);
        if (method.ReturnType.MetadataType != MetadataType.ValueType) return false;
        var definition = method.ReturnType.Resolve();
        if (definition is null || definition.Module != method.Module || !definition.IsValueType
            || definition.IsInterface || definition.IsEnum || definition.BaseType is not TypeDefinition baseType
            || baseType.Module != method.Module || baseType.FullName != "System.ValueType"
            || !definition.IsSequentialLayout || definition.DeclaringType is not null || definition.HasGenericParameters) return false;
        int lower = 0, upper = 0;
        foreach (var field in definition.Fields)
        {
            if (field.IsStatic) continue;
            if (field.Attributes != (FieldAttributes.Private | FieldAttributes.InitOnly)
                || field.FieldType.MetadataType != MetadataType.UInt64) return false;
            if (field.Name == "_lower") lower++;
            else if (field.Name == "_upper") upper++;
            else return false;
        }
        return lower == 1 && upper == 1;
    }

    private static bool IsCopiedVectorManagedThrowHelper(MethodDefinition method, string owner)
    {
        if (owner != "System.ThrowHelper" || method.Name != "ThrowForUnsupportedNumericsVectorBaseType"
            || method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || method.DeclaringType.DeclaringType is not null || method.DeclaringType.HasGenericParameters
            || method.Attributes != (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.AggressiveInlining || !method.HasBody
            || method.Body.Instructions.Count == 0 || method.RVA == 0 || method.HasThis || method.ExplicitThis
            || method.CallingConvention != MethodCallingConvention.Generic || method.IsPInvokeImpl || method.HasPInvokeInfo
            || method.GenericParameters.Count != 1 || method.Parameters.Count != 0
            || method.ReturnType is TypeSpecification || method.ReturnType.MetadataType != MetadataType.Void)
            return false;
        var parameter = method.GenericParameters[0];
        return parameter.Position == 0 && parameter.Owner == method
            && parameter.Attributes == GenericParameterAttributes.NonVariant && parameter.Constraints.Count == 0;
    }

    private static bool IsCopiedCultureInfoNameValidator(MethodDefinition method, string owner) =>
        CoreIntrinsicTypeMaps.IsCultureInfoNameValidatorMethod(method.Module.Assembly?.Name.Name, owner, method.Name,
            method.DeclaringType.DeclaringType is null && !method.DeclaringType.HasGenericParameters
                && !method.DeclaringType.IsValueType && !method.DeclaringType.IsInterface
                && method.Attributes == (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig)
                && method.ImplAttributes == MethodImplAttributes.IL && method.HasBody && method.RVA != 0
                && method.Body.Instructions.Count != 0 && !method.HasPInvokeInfo && !method.IsPInvokeImpl
                && !method.HasGenericParameters,
            !method.HasThis && !method.ExplicitThis && method.CallingConvention == MethodCallingConvention.Default
                && method.ReturnType is not TypeSpecification && method.ReturnType.MetadataType == MetadataType.Boolean
                && method.Parameters.Count == 2
                && method.Parameters[0].ParameterType is not TypeSpecification
                && method.Parameters[0].ParameterType.MetadataType == MetadataType.String
                && method.Parameters[1].ParameterType is not TypeSpecification
                && method.Parameters[1].ParameterType.MetadataType == MetadataType.Boolean);

    private bool TraverseCopiedBody(MethodDefinition method)
    {
        if (_copiedBodyTraversal.TryGetValue(method, out bool known)) return known;
        Spend();
        string owner = Name(method.DeclaringType);
        bool traverse;
        if (IsCopiedPrimitiveBigMul(method, owner)
            || IsCopiedNativeIntegerParse(method, owner)
            || BinaryIntegerEndianContract.IsNativeOwner(owner) && IsCopiedBinaryIntegerEndianBody(method, owner))
            traverse = false;
        else if (IsCopiedCultureInfoNameValidator(method, owner)
            || IsCopiedDateTimeManagedBody(method, owner)
            || IsCopiedDateTimeFormatInfoManagedBody(method, owner)
            || IsCopiedRangeThrowManagedBody(method, owner)
            || IsCopiedScalarNumericLeaf(method, owner)
            || IsCopiedBinaryIntegerEndianBody(method, owner)
            || IsCopiedPrimitiveParseBody(method, owner)
            || IsCopiedDecimalBinaryBody(method, owner)
            || IsCopiedPrimitiveWideConversionBody(method, owner)
            || IsCopiedVectorManagedThrowHelper(method, owner))
            traverse = true;
        else if (owner is "System.Array" or "System.Delegate")
            traverse = CoreIntrinsicTypeMaps.IsRealBodyGeneric(owner, method.Name) || CopiedArrayRealBodyMember(method);
        else
            traverse = !CoreIntrinsicTypeMaps.IsInlineLoweredPrimitiveMember(owner, method.Name)
                && !CoreIntrinsicTypeMaps.IsEnumInstanceFormatMethod(owner, method.Name)
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
