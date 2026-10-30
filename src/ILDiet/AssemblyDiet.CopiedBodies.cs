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

    private static bool IsCopiedDateTimeFormatInfoHasYearMonthAdjustment(MethodDefinition method, string owner) =>
        method.Module.Assembly?.Name.Name == "System.Private.CoreLib"
        && owner == "System.Globalization.DateTimeFormatInfo"
        && method.DeclaringType.DeclaringType is null
        && !method.DeclaringType.IsValueType && !method.DeclaringType.IsInterface
        && !method.DeclaringType.HasGenericParameters
        && method.Name == "get_HasYearMonthAdjustment"
        && method.Attributes == (MethodAttributes.Assembly | MethodAttributes.HideBySig | MethodAttributes.SpecialName)
        && method.ImplAttributes == MethodImplAttributes.IL
        && method.HasBody && method.Body.Instructions.Count != 0
        && !method.IsPInvokeImpl && !method.HasPInvokeInfo
        && method.HasThis && !method.ExplicitThis && method.CallingConvention == MethodCallingConvention.Default
        && !method.HasGenericParameters && method.Parameters.Count == 0
        && method.ReturnType is not TypeSpecification && method.ReturnType.MetadataType == MetadataType.Boolean;

    private static bool IsCopiedCoreClrDelegateVmConstructor(MethodDefinition method, string owner)
    {
        int arity = CoreIntrinsicTypeMaps.CoreClrDelegateVmConstructorArity(owner, method.Name);
        if (arity == 0 || method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || !method.IsPrivate || method.IsStatic || !method.HasThis || method.Parameters.Count != arity)
            return false;
        bool nativeIntTail = true;
        for (int i = 1; i < method.Parameters.Count; i++)
            if (method.Parameters[i].ParameterType.MetadataType != MetadataType.IntPtr)
            {
                nativeIntTail = false;
                break;
            }
        return CoreIntrinsicTypeMaps.IsCoreClrDelegateVmConstructorMethod(
            method.Module.Assembly.Name.Name, owner, method.Name, method.IsPrivate, method.IsStatic,
            method.GenericParameters.Count, method.ReturnType.MetadataType == MetadataType.Void,
            method.Parameters.Count, method.Parameters[0].ParameterType.MetadataType == MetadataType.Object,
            nativeIntTail);
    }

    private static bool IsCopiedNullableReboxing(MethodDefinition method, string owner)
    {
        if (owner != "System.RuntimeMethodHandle"
            || method.Name is not ("ReboxFromNullable" or "ReboxToNullable"))
            return false;
        bool runtimeType = false;
        if (method.Parameters.Count > 1)
        {
            var type = method.Parameters[1].ParameterType;
            bool core = type.Scope is AssemblyNameReference reference
                ? reference.Name == "System.Private.CoreLib"
                : type.Scope is ModuleDefinition module && module.Assembly?.Name.Name == "System.Private.CoreLib";
            runtimeType = core && type.FullName == "System.RuntimeType";
        }
        return CoreIntrinsicTypeMaps.IsNullableReboxingMethod(
            method.Module.Assembly?.Name.Name, owner, method.Name, method.IsAssembly,
            method.IsStatic && !method.HasThis && !method.ExplicitThis
                && method.CallingConvention == MethodCallingConvention.Default, method.GenericParameters.Count,
            method.HasBody && method.IsIL && method.IsManaged && !method.IsInternalCall,
            method.IsPInvokeImpl, method.Parameters.Count, method.ReturnType.MetadataType == MetadataType.Object,
            method.Parameters.Count > 0 && method.Parameters[0].ParameterType.MetadataType == MetadataType.Object,
            runtimeType);
    }

    private static bool IsCopiedCoreClrTypeMaterialization(MethodDefinition method, string owner)
    {
        if (method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || owner != "System.RuntimeTypeHandle"
            || method.Name is not ("GetRuntimeType" or "GetRuntimeTypeFromHandle"
                or "GetRuntimeTypeFromHandleMaybeNull" or "GetRuntimeTypeFromHandleSlow")
            || !method.IsStatic || method.HasThis || method.HasGenericParameters
            || !method.IsIL || !method.IsManaged || method.IsInternalCall)
            return false;
        static string? CoreType(TypeReference type)
        {
            if (type.MetadataType == MetadataType.Void) return "System.Void";
            if (type.MetadataType == MetadataType.IntPtr) return "System.IntPtr";
            var element = type is PointerType pointer ? pointer.ElementType : type;
            bool core = element.Scope is AssemblyNameReference reference
                ? reference.Name == "System.Private.CoreLib"
                : element.Scope is ModuleDefinition module && module.Assembly?.Name.Name == "System.Private.CoreLib";
            return core ? type.FullName : null;
        }
        if (method.IsPInvokeImpl && method.PInvokeInfo is not { Module.Name: "QCall",
            EntryPoint: "RuntimeTypeHandle_GetRuntimeTypeFromHandleSlow" })
            return false;
        return CoreIntrinsicTypeMaps.IsCoreClrTypeMaterializationMethod(
            method.Module.Assembly?.Name.Name, owner, method.Name, method.IsAssembly, method.IsPrivate,
            method.IsStatic && !method.HasThis, method.GenericParameters.Count,
            method.HasBody && !method.IsInternalCall, method.IsPInvokeImpl, method.Parameters.Count,
            CoreType(method.ReturnType), method.Parameters.Count > 0 ? CoreType(method.Parameters[0].ParameterType) : null,
            method.Parameters.Count > 1 ? CoreType(method.Parameters[1].ParameterType) : null);
    }

    private static bool IsCopiedMemberMetadataDefinition(MethodDefinition method, string owner)
    {
        if (!CoreIntrinsicTypeMaps.IsMemberMetadataDefinitionOwner(owner)
            || method.Name != "HasSameMetadataDefinitionAs")
            return false;
        bool memberParameter = method.Parameters.Count == 1
            && method.Parameters[0].ParameterType is { FullName: "System.Reflection.MemberInfo" } parameter
            && parameter is not TypeSpecification
            && (parameter.Scope is AssemblyNameReference reference ? reference.Name == "System.Private.CoreLib"
                : parameter.Scope is ModuleDefinition module && module.Assembly?.Name.Name == "System.Private.CoreLib");
        return CoreIntrinsicTypeMaps.IsMemberMetadataDefinitionMethod(method.Module.Assembly?.Name.Name,
            owner, method.Name, method.IsPublic && method.IsVirtual && !method.IsAbstract, method.IsFinal,
            !method.IsStatic && method.HasThis && !method.ExplicitThis && method.CallingConvention == MethodCallingConvention.Default,
            method.GenericParameters.Count, method.HasBody && method.IsIL && method.IsManaged && !method.IsInternalCall
                && method.Body.Instructions.Count != 0, method.IsPInvokeImpl || method.HasPInvokeInfo,
            method.Parameters.Count, method.ReturnType.MetadataType == MetadataType.Boolean,
            memberParameter ? "System.Reflection.MemberInfo" : null);
    }

    private static bool IsCopiedMemberNotFoundExceptionFactory(MethodDefinition method, string owner)
    {
        if (method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || owner != "System.Type" || method.Name != "CreateGetMemberWithSameMetadataDefinitionAsNotFoundException")
            return false;
        static string? Shape(TypeReference type)
        {
            bool core = type.Scope is AssemblyNameReference reference
                ? reference.Name == "System.Private.CoreLib"
                : type.Scope is ModuleDefinition module && module.Assembly?.Name.Name == "System.Private.CoreLib";
            return core && type is not TypeSpecification ? type.FullName : null;
        }
        return CoreIntrinsicTypeMaps.IsMemberNotFoundExceptionFactoryMethod(
            method.Module.Assembly?.Name.Name, owner, method.Name, method.IsFamilyAndAssembly,
            method.IsStatic && !method.IsVirtual && !method.IsAbstract && !method.HasThis && !method.ExplicitThis
                && method.CallingConvention == MethodCallingConvention.Default,
            method.GenericParameters.Count, method.HasBody && method.IsIL && method.IsManaged && !method.IsInternalCall
                && method.Body.Instructions.Count != 0,
            method.IsPInvokeImpl || method.HasPInvokeInfo, method.Parameters.Count, Shape(method.ReturnType),
            method.Parameters.Count == 1 ? Shape(method.Parameters[0].ParameterType) : null);
    }

    private static bool IsCopiedPrimitiveElementHelper(MethodDefinition method, string owner)
    {
        if (method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || owner != "System.Runtime.CompilerServices.RuntimeHelpers"
            || method.Name is not ("IsPrimitiveType" or "CanPrimitiveWiden"))
            return false;
        int arity = method.Name == "IsPrimitiveType" ? 1 : 2;
        bool elementType = method.Parameters.Count == arity;
        TypeDefinition? element = null;
        foreach (var parameter in method.Parameters)
        {
            elementType &= parameter.ParameterType is TypeDefinition { FullName: "System.Reflection.CorElementType", IsEnum: true } type
                && type.Module == method.Module
                && type.Fields.Any(field => field.Name == "value__" && !field.IsStatic
                    && field.FieldType.MetadataType == MetadataType.Byte)
                && (element is null || element == type);
            element = parameter.ParameterType as TypeDefinition;
        }
        return CoreIntrinsicTypeMaps.IsPrimitiveElementHelperMethod(method.Module.Assembly?.Name.Name,
            owner, method.Name, method.IsAssembly,
            method.IsStatic && !method.IsVirtual && !method.IsAbstract && !method.HasThis && !method.ExplicitThis
                && method.CallingConvention == MethodCallingConvention.Default,
            method.GenericParameters.Count, method.HasBody && method.IsIL && method.IsManaged && !method.IsInternalCall
                && method.Body.Instructions.Count != 0,
            method.IsPInvokeImpl || method.HasPInvokeInfo, method.Parameters.Count,
            method.ReturnType.MetadataType == MetadataType.Boolean, elementType);
    }

    private static bool IsCopiedAmbiguousMatchExceptionFactory(MethodDefinition method, string owner)
    {
        if (method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || owner != "System.ThrowHelper" || method.Name != "GetAmbiguousMatchException")
            return false;
        static string? Shape(TypeReference type)
        {
            bool core = type.Scope is AssemblyNameReference reference
                ? reference.Name == "System.Private.CoreLib"
                : type.Scope is ModuleDefinition module && module.Assembly?.Name.Name == "System.Private.CoreLib";
            return core && type is not TypeSpecification ? type.FullName : null;
        }
        return CoreIntrinsicTypeMaps.IsAmbiguousMatchExceptionFactoryMethod(
            method.Module.Assembly?.Name.Name, owner, method.Name, method.IsAssembly,
            method.IsStatic && !method.IsVirtual && !method.IsAbstract && !method.HasThis && !method.ExplicitThis
                && method.CallingConvention == MethodCallingConvention.Default,
            method.GenericParameters.Count, method.HasBody && method.IsIL && method.IsManaged && !method.IsInternalCall
                && method.Body.Instructions.Count != 0,
            method.IsPInvokeImpl || method.HasPInvokeInfo, method.Parameters.Count, Shape(method.ReturnType),
            method.Parameters.Count == 1 ? Shape(method.Parameters[0].ParameterType) : null);
    }

    private static bool IsCopiedRuntimeTypeIntroducedMethodsFactory(MethodDefinition method, string owner)
    {
        if (method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || owner != "System.RuntimeTypeHandle" || method.Name != "GetIntroducedMethods")
            return false;
        static bool Core(TypeReference type) => type.Scope is AssemblyNameReference reference
            ? reference.Name == "System.Private.CoreLib"
            : type.Scope is ModuleDefinition module && module.Assembly?.Name.Name == "System.Private.CoreLib";
        var result = method.ReturnType;
        bool nested = result is not TypeSpecification && result.IsValueType && !result.HasGenericParameters
            && result.Name == "IntroducedMethodEnumerator" && result.Namespace.Length == 0
            && result.DeclaringType is { FullName: "System.RuntimeTypeHandle" } parent && Core(parent) && Core(result);
        bool input = method.Parameters.Count == 1 && method.Parameters[0].ParameterType is var parameter
            && parameter is not TypeSpecification && !parameter.IsValueType
            && parameter.FullName == "System.RuntimeType" && Core(parameter);
        return CoreIntrinsicTypeMaps.IsRuntimeTypeIntroducedMethodsFactoryMethod(
            method.Module.Assembly?.Name.Name, owner, method.Name,
            method.Attributes == (MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig),
            !method.HasThis && !method.ExplicitThis && method.CallingConvention == MethodCallingConvention.Default,
            method.GenericParameters.Count, method.HasBody && method.ImplAttributes == MethodImplAttributes.IL
                && method.Body.Instructions.Count != 0,
            method.IsPInvokeImpl || method.HasPInvokeInfo, method.Parameters.Count, nested, input);
    }

    private static bool IsCopiedRuntimeTypeHandleCopy(MethodDefinition method, string owner)
    {
        if (method.Module.Assembly?.Name.Name != "System.Private.CoreLib"
            || owner != "System.RuntimeTypeHandle" || method.Name != "CopyRuntimeTypeHandles")
            return false;
        static bool Core(TypeReference type) => type.Scope is AssemblyNameReference reference
            ? reference.Name == "System.Private.CoreLib"
            : type.Scope is ModuleDefinition module && module.Assembly?.Name.Name == "System.Private.CoreLib";
        static string? Shape(TypeReference type)
        {
            if (type is ByReferenceType { ElementType.MetadataType: MetadataType.Int32 }) return "System.Int32&";
            if (type is ArrayType { IsVector: true } array)
            {
                if (array.ElementType.MetadataType == MetadataType.IntPtr) return "System.IntPtr[]";
                if (Core(array.ElementType))
                {
                    if (array.ElementType.FullName == "System.Type") return "System.Type[]";
                    if (array.ElementType.FullName == "System.RuntimeTypeHandle") return "System.RuntimeTypeHandle[]";
                }
            }
            if (type is GenericInstanceType span && Core(span.ElementType) && span.GenericArguments.Count == 1
                && span.GenericArguments[0].MetadataType == MetadataType.IntPtr)
            {
                if (span.ElementType.FullName == "System.Span`1") return "System.Span<System.IntPtr>";
                if (span.ElementType.FullName == "System.ReadOnlySpan`1") return "System.ReadOnlySpan<System.IntPtr>";
            }
            return null;
        }
        return CoreIntrinsicTypeMaps.IsRuntimeTypeHandleCopyMethod(
            method.Module.Assembly?.Name.Name, owner, method.Name, method.IsAssembly,
            method.IsStatic && !method.HasThis && !method.ExplicitThis && method.CallingConvention == MethodCallingConvention.Default,
            method.GenericParameters.Count, method.HasBody && method.IsIL && method.IsManaged && !method.IsInternalCall,
            method.IsPInvokeImpl || method.HasPInvokeInfo, method.Parameters.Count, Shape(method.ReturnType),
            method.Parameters.Count > 0 ? Shape(method.Parameters[0].ParameterType) : null,
            method.Parameters.Count > 1 ? Shape(method.Parameters[1].ParameterType) : null);
    }

    private bool TraverseCopiedBody(MethodDefinition method)
    {
        if (_copiedBodyTraversal.TryGetValue(method, out bool known)) return known;
        Spend();
        string owner = Name(method.DeclaringType);
        bool traverse;
        if (CoreIntrinsicTypeMaps.IsDelegateNamedConstructorMethod(method.Module.Assembly?.Name.Name,
            owner, method.Name, method.IsFamily, method.IsStatic || !method.HasThis,
            method.GenericParameters.Count, method.ReturnType.MetadataType == MetadataType.Void,
            method.Parameters.Count, method.Parameters.Count == 2 ? method.Parameters[0].ParameterType.FullName : null,
            method.Parameters.Count == 2 && method.Parameters[1].ParameterType.MetadataType == MetadataType.String))
            traverse = false;
        else if (IsCopiedCoreClrTypeMaterialization(method, owner) || IsCopiedNullableReboxing(method, owner))
            traverse = false;
        else if (IsCopiedCoreClrDelegateVmConstructor(method, owner))
            traverse = false;
        else if (CoreIntrinsicTypeMaps.IsMulticastAccessorMethod(owner, method.Name, method.IsStatic,
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
        else if (IsCopiedMemberMetadataDefinition(method, owner))
            traverse = owner == "System.Reflection.MemberInfo";
        else if (IsCopiedDateTimeFormatInfoHasYearMonthAdjustment(method, owner)
            || IsCopiedRuntimeTypeIntroducedMethodsFactory(method, owner)
            || IsCopiedRuntimeTypeHandleCopy(method, owner) || IsCopiedAmbiguousMatchExceptionFactory(method, owner)
            || IsCopiedMemberNotFoundExceptionFactory(method, owner) || IsCopiedPrimitiveElementHelper(method, owner))
            traverse = true;
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
