using System.Collections.Immutable;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly StrippingLevel _strippingLevel;
    private readonly HashSet<ClassInfo> _mediumTypes = new();
    private readonly HashSet<(int Module, TypeDefinitionHandle Handle)> _mediumDefinitions = new();
    private readonly HashSet<(int Module, TypeDefinitionHandle Handle)> _mediumAttributeDefinitions = new();
    private readonly HashSet<MethodInfo> _mediumMethods = new();
    private readonly Queue<TypeDesc> _mediumPending = new();
    private readonly Dictionary<ClassInfo, ClassInfo?> _mediumPredecessors = new();
    private readonly Dictionary<ClassInfo, MethodInfo?> _mediumSources = new();
    private readonly Dictionary<MethodInfo, ClassInfo> _mediumMethodOwners = new();
    private readonly HashSet<ClassInfo> _mediumOnlyAllocations = new();
    private readonly HashSet<MethodInfo> _mediumOnlyVirtualDemands = new();
    private readonly HashSet<GvmDispatch> _mediumOnlyGvmDemands = new();
    private readonly HashSet<ClassInfo> _mediumOnlyRuntimeRaisedOwners = new();
    private bool _mediumOnlyObjectEqualityDemand;
    private ClassInfo? _mediumOwner;

    private void NoteMediumAllocation(ClassInfo cls)
    {
        if (_strippingLevel != StrippingLevel.Medium)
            return;
        if (!_reachingBindingOnlyBody)
            _mediumOnlyAllocations.Remove(cls);
        else if (!_allocatedRefTypes.Contains(cls))
            _mediumOnlyAllocations.Add(cls);
    }

    private void NoteMediumVirtualDemand(MethodInfo method)
    {
        if (_strippingLevel != StrippingLevel.Medium)
            return;
        if (!_reachingBindingOnlyBody)
            _mediumOnlyVirtualDemands.Remove(method);
        else if (!_usedVirtualDecls.Contains(method))
            _mediumOnlyVirtualDemands.Add(method);
    }

    internal bool KeepsTypeAttributeMetadata(ClassInfo cls) => KeepsTypeAttributeMetadata(cls.Module, cls.Handle);

    internal bool KeepsTypeAttributeMetadata(Module module, TypeDefinitionHandle handle) => IsUserModule(module)
        || _mediumAttributeDefinitions.Contains((module.Index, handle));

    internal bool IsMediumOnlyInitializer(MethodInfo method) => _strippingLevel == StrippingLevel.Medium
        && IsBindingOnlyInitializer(method);

    // Attribute blobs may serialize a scalar under a loaded forwarding facade.
    // Reuse the TypeRef resolver so retention and attribute emission share its identity.
    internal TypeDesc ResolveAttributeTypeName(string name)
    {
        var type = ResolveSerializedTypeName(name);
        if (_strippingLevel != StrippingLevel.Medium || type.Kind != TypeKind.External)
            return type;
        string chain = SerializedTypeNamePrefix(name);
        int comma = name.IndexOf(',');
        if (comma < 0 || chain.IndexOfAny(new[] { '[', ']', '\\', '*', '&' }) >= 0)
            return type;
        string assembly = name[(comma + 1)..].Split(',')[0].Trim();
        var facade = Modules.FirstOrDefault(m => m.AssemblyName.Equals(assembly, StringComparison.OrdinalIgnoreCase));
        if (facade is not null)
        {
            int plus = chain.IndexOf('+');
            string head = plus < 0 ? chain : chain[..plus];
            int dot = head.LastIndexOf('.');
            string ns = dot < 0 ? "" : head[..dot], simple = dot < 0 ? head : head[(dot + 1)..];
            if (TypeIndex().TryGetValue((ns, simple), out var candidates))
            {
                var resolved = ResolveAssemblyType(assembly, ns, simple, candidates, facade, default);
                for (int link = plus; link >= 0 && resolved is not null;)
                {
                    var (module, declaring) = TemplateOrClassDef(resolved);
                    if (module is null)
                    {
                        resolved = null;
                        break;
                    }
                    int next = chain.IndexOf('+', link + 1);
                    string nestedName = next < 0 ? chain[(link + 1)..] : chain[(link + 1)..next];
                    resolved = null;
                    foreach (var nested in module.Reader.GetTypeDefinition(declaring).GetNestedTypes())
                        if (module.Reader.StringComparer.Equals(module.Reader.GetTypeDefinition(nested).Name, nestedName))
                        {
                            resolved = MakeTypeDescFor(module, nested);
                            break;
                        }
                    link = next;
                }
                if (resolved is not null)
                    return resolved;
            }
        }
        return TypeDesc.MakeExternal(type.ExternalName!, assembly: assembly);
    }

    internal PrimitiveTypeCode AttributeEnumUnderlying(TypeDesc type)
    {
        if (_strippingLevel == StrippingLevel.Medium
            && (type.Class is not { } cls || !EnsureShape(cls).IsEnum))
            throw new NotSupportedException("Custom attribute enum " + type
                + (type.ExternalAssembly is { } assembly ? ", " + assembly : "")
                + " has no resolved enum definition; its encoded width is unknown");
        return SerializedEnumUnderlying(type);
    }

    // Only executable edges and retained structure feed this queue. A model shell,
    // signature probe or overload candidate is not evidence of static use.
    private void NoteMediumType(TypeDesc? type)
    {
        if (_strippingLevel != StrippingLevel.Medium || type is null || type.IsCanonPlaceholder)
            return;
        if (type.FunctionSignature is { } function)
            foreach (var part in function.Types)
                NoteMediumType(part);
        if (type.Element is { } element)
            NoteMediumType(element);
        if (type.GenericArgs is { } arguments)
            foreach (var argument in arguments)
                NoteMediumType(argument);
        if (type.Kind == TypeKind.Primitive)
        {
            if (FindClassByFullName("System." + type.Primitive) is { } primitive)
                NoteMediumType(TypeDesc.MakeClass(primitive));
            return;
        }
        if (type.Kind is TypeKind.External or TypeKind.ExternalGeneric)
        {
            if (type.ExternalName is { } name)
            {
                if (FindClassByFullName(name) is { } external)
                    NoteMediumType(TypeDesc.MakeClass(external));
                else if (OpenGenericDefHandleByName(name) is { } definition)
                    NoteMediumType(TypeDesc.MakeTemplate(definition.Module, definition.Handle));
            }
            return;
        }
        if (type.Kind == TypeKind.Template)
        {
            _mediumAttributeDefinitions.Add((type.TemplateModule!.Index, type.TemplateHandle));
            if (_mediumDefinitions.Add((type.TemplateModule!.Index, type.TemplateHandle)))
                _mediumPending.Enqueue(type);
            return;
        }
        if (type.Kind != TypeKind.Class || type.Class is not { } cls)
            return;
        foreach (var argument in cls.Context.TypeArgs)
            NoteMediumType(argument);
        if (ContainsCanonPlaceholder(cls))
            return;
        if (ContainsGenericVar(cls))
        {
            NoteMediumType(TypeDesc.MakeTemplate(cls.Module, cls.Handle));
            return;
        }
        if (!_mediumTypes.Add(cls))
            return;
        _mediumAttributeDefinitions.Add((cls.Module.Index, cls.Handle));
        _mediumPredecessors.Add(cls, _mediumOwner);
        _mediumSources.Add(cls, _scanningBody ? _currentScan : null);
        _mediumPending.Enqueue(type);
    }

    private void NoteMediumSignature(MethodSignature<TypeDesc> signature)
    {
        NoteMediumType(signature.ReturnType);
        foreach (var parameter in signature.ParameterTypes)
            NoteMediumType(parameter);
    }

    private void NoteMediumMethod(MethodInfo method)
    {
        if (_strippingLevel != StrippingLevel.Medium || !_mediumMethods.Add(method))
            return;
        NoteMediumType(TypeDesc.MakeClass(method.DeclaringClass));
        NoteMediumSignature(method.Signature);
        foreach (var argument in method.Context.MethodArgs)
            NoteMediumType(argument);
    }

    private void NoteMediumBodyTypes(MethodInfo method, MethodBodyBlock body)
    {
        if (_strippingLevel != StrippingLevel.Medium)
            return;
        NoteMediumBodyTypes(method.Module, body, method.Context);
    }

    private void NoteMediumBodyTypes(Module module, MethodBodyBlock body, GenericContext context)
    {
        if (!body.LocalSignature.IsNil)
            foreach (var type in module.Reader.GetStandaloneSignature(body.LocalSignature)
                         .DecodeLocalSignature(SigProvider, context))
                NoteMediumType(type);
        foreach (var region in body.ExceptionRegions)
            if (region.Kind == ExceptionRegionKind.Catch)
                NoteMediumType(ResolveTypeTokenForScan(module, region.CatchType, context));
    }

    private void NoteMediumOperand(Module module, EntityHandle handle, GenericContext context,
        TypeDesc[]? methodArguments = null)
    {
        if (_strippingLevel != StrippingLevel.Medium)
            return;
        var reader = module.Reader;
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
            case HandleKind.TypeReference:
            case HandleKind.TypeSpecification:
                NoteMediumType(ResolveTypeTokenForScan(module, handle, context));
                break;
            case HandleKind.FieldDefinition:
            {
                var field = reader.GetFieldDefinition((FieldDefinitionHandle)handle);
                NoteMediumType(GetTypeDescForDefinition(module, field.GetDeclaringType()));
                NoteMediumType(field.DecodeSignature(SigProvider, context));
                break;
            }
            case HandleKind.MethodDefinition:
            {
                var method = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
                NoteMediumType(GetTypeDescForDefinition(module, method.GetDeclaringType()));
                NoteMediumSignature(method.DecodeSignature(SigProvider,
                    new GenericContext(context.TypeArgs, methodArguments ?? Array.Empty<TypeDesc>())));
                break;
            }
            case HandleKind.MemberReference:
            {
                var member = reader.GetMemberReference((MemberReferenceHandle)handle);
                var owner = ResolveTypeTokenForScan(module, member.Parent, context);
                NoteMediumType(owner);
                var memberContext = new GenericContext(
                    owner?.Class?.Context.TypeArgs ?? owner?.GenericArgs ?? Array.Empty<TypeDesc>(),
                    methodArguments ?? Array.Empty<TypeDesc>());
                if (member.GetKind() == MemberReferenceKind.Field)
                    NoteMediumType(member.DecodeFieldSignature(SigProvider, memberContext));
                else
                    NoteMediumSignature(member.DecodeMethodSignature(SigProvider, memberContext));
                break;
            }
            case HandleKind.MethodSpecification:
            {
                var method = reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                var arguments = method.DecodeSignature(SigProvider, context).ToArray();
                foreach (var argument in arguments)
                    NoteMediumType(argument);
                NoteMediumOperand(module, method.Method, context, arguments);
                break;
            }
            case HandleKind.StandaloneSignature:
                NoteMediumSignature(reader.GetStandaloneSignature((StandaloneSignatureHandle)handle)
                    .DecodeMethodSignature(SigProvider, context));
                break;
        }
    }

    private bool RetainPendingMediumTypes()
    {
        if (_mediumPending.Count == 0)
            return false;
        // Snapshot the queue: newly named structure is shaped by the next drain
        // before its members are read under strict completion.
        int count = _mediumPending.Count;
        while (count-- > 0)
        {
            var type = _mediumPending.Dequeue();
            bool previousBinding = _reachingBindingOnlyBody;
            var previousOwner = _mediumOwner;
            var previousScan = _currentScan;
            _reachingBindingOnlyBody = true;
            _mediumOwner = type.Class;
            _currentScan = null;
            try
            {
                if (type.Class is { } cls)
                    RetainMediumClass(cls);
                else
                    RetainMediumDefinition(type.TemplateModule!, type.TemplateHandle, GenericContext.Empty);
            }
            catch (NotSupportedException exception) when (!IsMustEscape(exception))
            {
                throw new NotSupportedException("Medium stripping while retaining "
                    + (type.Class is { } owner ? MediumRetentionChain(owner)
                        : MethodCompiler.OpenDefBacktickName(type.TemplateModule!, type.TemplateHandle))
                    + ": " + exception.Message, exception);
            }
            finally
            {
                _mediumOwner = previousOwner;
                _reachingBindingOnlyBody = previousBinding;
                _currentScan = previousScan;
            }
        }
        return true;
    }

    private string MediumRetentionChain(ClassInfo cls)
    {
        var chain = new List<string>();
        var seen = new HashSet<ClassInfo>();
        for (ClassInfo? current = cls; current is not null && seen.Add(current);)
        {
            chain.Add(current.FullName);
            if (_mediumSources.TryGetValue(current, out var source) && source is not null)
            {
                chain.Add(source.DeclaringClass.FullName + "." + source.Name);
                break;
            }
            _mediumPredecessors.TryGetValue(current, out current);
        }
        return string.Join(" <- ", chain);
    }

    private void RetainMediumClass(ClassInfo cls)
    {
        EnsureCompleted(cls);
        NoteForceEmit(cls);
        _explicitReflectionKeep.Add(cls);
        TrimNoteNamedClass(cls);
        RetainMediumDefinition(cls.Module, cls.Handle, cls.Context);
        bool constructs = false;
        foreach (var method in cls.Methods.ToList())
        {
            NoteMediumMethod(method);
            // Retention needs a callable symbol, but cannot revive IL replaced by
            // a member-specific cut. Reuse the same body as an address-taken call.
            if (cls.IntrinsicCppName is null && !CoreIntrinsics.IsIntrinsicType(cls.FullName)
                && cls.PlatformIsa is null && !IsBoundedMethod(cls.FullName, method.Name)
                && !LowersToPInvoke(method)
                && (method.PInvoke is not { } import || NativeImplFor(import) is null)
                && CoreIntrinsics.TryFindCutRow(method, out _))
                NoteInterceptFtnTarget(method);
            else
                Reach(method);
            ReachRootInitializer(method);
            NoteReflectionInvokeBoxes(method);
            constructs |= method.Name == ".ctor" && method.Rva != 0;
        }
        foreach (var field in cls.Fields)
            NoteReflectionBoxed(field.Type);
        if (constructs && !cls.IsValueType && !cls.IsAbstract && !cls.IsInterface && !cls.IsDelegate)
            ReachAllocatedType(cls);
    }

    private void RetainMediumDefinition(Module module, TypeDefinitionHandle handle, GenericContext context)
    {
        if (handle.IsNil)
            return;
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(handle);
        if (!definition.BaseType.IsNil)
            NoteMediumType(ResolveTypeTokenForScan(module, definition.BaseType, context));
        foreach (var implemented in definition.GetInterfaceImplementations())
        {
            var implementation = reader.GetInterfaceImplementation(implemented);
            NoteMediumType(ResolveTypeTokenForScan(module, implementation.Interface, context));
            NoteMediumAttributes(module, implementation.GetCustomAttributes());
        }
        NoteMediumConstraints(module, definition.GetGenericParameters(), context);
        NoteMediumAttributes(module, definition.GetCustomAttributes());
        foreach (var fieldHandle in definition.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            NoteMediumType(field.DecodeSignature(SigProvider, context));
            NoteMediumAttributes(module, field.GetCustomAttributes());
        }
        foreach (var methodHandle in definition.GetMethods())
        {
            var method = reader.GetMethodDefinition(methodHandle);
            NoteMediumSignature(method.DecodeSignature(SigProvider, context));
            NoteMediumConstraints(module, method.GetGenericParameters(), context);
            NoteMediumAttributes(module, method.GetCustomAttributes());
            foreach (var parameter in method.GetParameters())
                NoteMediumAttributes(module, reader.GetParameter(parameter).GetCustomAttributes());
            if ((context.TypeArgs.Length == 0 && definition.GetGenericParameters().Count > 0
                    || method.GetGenericParameters().Count > 0)
                && MediumTraversesOpenBody(module, handle, method))
            {
                var body = module.PE.GetMethodBody(method.RelativeVirtualAddress);
                NoteMediumBodyTypes(module, body, context);
                foreach (var instruction in ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat()))
                    if (instruction.Token != 0 && (uint)instruction.Token >> 24 != 0x70)
                        NoteMediumOperand(module, SRME.EntityHandle(instruction.Token), context);
            }
        }
        foreach (var propertyHandle in definition.GetProperties())
        {
            var property = reader.GetPropertyDefinition(propertyHandle);
            NoteMediumSignature(property.DecodeSignature(SigProvider, context));
            NoteMediumAttributes(module, property.GetCustomAttributes());
        }
        foreach (var eventHandle in definition.GetEvents())
        {
            var @event = reader.GetEventDefinition(eventHandle);
            NoteMediumType(ResolveTypeTokenForScan(module, @event.Type, context));
            NoteMediumAttributes(module, @event.GetCustomAttributes());
        }
    }

    // An open body contributes concrete references without becoming an executable
    // instantiation. Runtime replacements keep the same managed-body boundary.
    private bool MediumTraversesOpenBody(Module module, TypeDefinitionHandle owner, MethodDefinition method)
    {
        if (method.RelativeVirtualAddress == 0)
            return false;
        var reader = module.Reader;
        string name = reader.GetString(method.Name);
        var definition = reader.GetTypeDefinition(owner);
        string fullName = string.Join("+", RawSignatureProvider.TypeDefinitionName(reader, owner)
            .Split('+').Select(StripArity));
        if (IsBoundedMethod(fullName, name)
            || AdoptedAsyncCpp(module, owner) is not null
            || definition.GetDeclaringType() is { IsNil: false } parent
                && CoreIntrinsicTypeMaps.IsIntrinsicNested(RawSignatureProvider.TypeDefinitionName(reader, parent),
                    reader.GetString(definition.Name))
            || PlatformIsaQualifiedName(reader, definition) is { } isa
                && CoreIntrinsics.PlatformIsaFamily(isa) is not null)
            return false;
        foreach (var intercept in CoreIntrinsics.BoundedIntercepts)
            if (intercept.Matches(fullName, name))
                return false;
        if (CoreIntrinsics.IsIntrinsicType(fullName)
            || CoreIntrinsicTypeMaps.IntrinsicGenericCppType(fullName, primitiveElement: false) is not null)
            return CoreIntrinsicTypeMaps.IsRealBodyGeneric(fullName, name);
        return true;
    }

    private void NoteMediumConstraints(Module module, GenericParameterHandleCollection parameters,
        GenericContext context)
    {
        foreach (var handle in parameters)
        {
            var parameter = module.Reader.GetGenericParameter(handle);
            NoteMediumAttributes(module, parameter.GetCustomAttributes());
            foreach (var constraintHandle in parameter.GetConstraints())
            {
                var constraint = module.Reader.GetGenericParameterConstraint(constraintHandle);
                NoteMediumType(ResolveTypeTokenForScan(module, constraint.Type, context));
                NoteMediumAttributes(module, constraint.GetCustomAttributes());
            }
        }
    }

    private void NoteMediumAttributes(Module module, CustomAttributeHandleCollection handles)
    {
        foreach (var handle in handles)
        {
            var attribute = module.Reader.GetCustomAttribute(handle);
            if (AttributeTypeName(module.Reader, attribute) is { } name)
                NoteUserTypeofFrameworkType(name);
            NoteMediumOperand(module, attribute.Constructor, GenericContext.Empty);
            CustomAttributeValue<TypeDesc> value;
            try { value = attribute.DecodeValue(AttrProvider); }
            catch (Exception exception) when (exception is BadImageFormatException
                || exception is NotSupportedException && !IsMustEscape(exception))
            {
                throw new NotSupportedException("Cannot decode custom attribute "
                    + AttributeTypeName(module.Reader, attribute) + " in " + module.AssemblyName
                    + " on " + MediumAttributeTarget(module, attribute.Parent)
                    + " (attribute 0x" + SRME.GetToken(handle).ToString("X8")
                    + ", target 0x" + SRME.GetToken(attribute.Parent).ToString("X8") + "): "
                    + exception.Message, exception);
            }
            foreach (var argument in value.FixedArguments)
                NoteMediumAttributeArgument(argument.Type, argument.Value);
            foreach (var argument in value.NamedArguments)
                NoteMediumAttributeArgument(argument.Type, argument.Value);
        }
        ReachAttributesOf(module, handles, reach: true);
    }

    private string MediumAttributeTarget(Module module, EntityHandle parent)
    {
        var reader = module.Reader;
        if (parent.Kind == HandleKind.TypeDefinition)
            return RawSignatureProvider.TypeDefinitionName(reader, (TypeDefinitionHandle)parent);
        if (parent.Kind == HandleKind.MethodDefinition)
        {
            var method = reader.GetMethodDefinition((MethodDefinitionHandle)parent);
            return RawSignatureProvider.TypeDefinitionName(reader, method.GetDeclaringType())
                + "." + reader.GetString(method.Name);
        }
        if (parent.Kind == HandleKind.FieldDefinition)
        {
            var field = reader.GetFieldDefinition((FieldDefinitionHandle)parent);
            return RawSignatureProvider.TypeDefinitionName(reader, field.GetDeclaringType())
                + "." + reader.GetString(field.Name);
        }
        return (_mediumOwner?.FullName ?? module.AssemblyName) + "/" + parent.Kind;
    }

    private void NoteMediumAttributeArgument(TypeDesc type, object? value)
    {
        NoteMediumType(type);
        if (value is TypeDesc named)
            NoteMediumType(named);
        if (value is ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> items)
            foreach (var item in items)
                NoteMediumAttributeArgument(item.Type, item.Value);
    }
}
