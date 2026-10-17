using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private void MarkSignatureType(TypeReference? reference)
    {
        if (_passiveAttributeMetadataDepth != 0) NotePassiveAttributeType(reference);
        RequireType(reference, TypeState.Metadata);
    }

    private void KeepMethodSignature(MethodDefinition method)
    {
        // Initializer rows can execute eagerly; a metadata-only initializer is removed.
        if (method.IsConstructor && method.IsStatic && !_methods.Contains(method)) return;
        if (_passiveAttributeMetadataDepth != 0)
        {
            _passiveAttributeMethods.Add(method);
            NotePassiveAttributeType(method.DeclaringType);
        }
        else _independentMetadataMethods.Add(method);
        Require(method, MethodState.Metadata);
    }

    private readonly HashSet<PropertyDefinition> _rowOnlyProperties = new();

    private void KeepPropertyRow(PropertyDefinition property, bool allowAccessorless = false)
    {
        if (_passiveAttributeMetadataDepth == 0) ReplayPassiveBindings(property);
        var bindingOwner = _passiveBindingOwner;
        if (_passiveAttributeMetadataDepth != 0) _passiveBindingOwner = property;
        try { KeepPropertyRowCore(property, allowAccessorless); }
        finally { _passiveBindingOwner = bindingOwner; }
    }

    private void KeepPropertyRowCore(PropertyDefinition property, bool allowAccessorless)
    {
        if (UsesReachedCopyTraversal(property.DeclaringType))
        {
            MarkSignatureType(property.PropertyType);
            foreach (var parameter in property.Parameters) MarkSignatureType(parameter.ParameterType);
            return;
        }
        if (allowAccessorless) _rowOnlyProperties.Add(property);
        if (!_properties.Add(property)) return;
        MarkSignatureType(property.PropertyType);
        foreach (var parameter in property.Parameters)
        {
            MarkSignatureType(parameter.ParameterType);
            MarkAttributes(parameter);
            MarkMarshal(parameter);
        }
        MarkAttributes(property);
    }

    private void KeepPropertySignature(PropertyDefinition property)
    {
        KeepPropertyRow(property);
        _rowOnlyProperties.Remove(property);
        if (property.GetMethod is not null) KeepMethodSignature(property.GetMethod);
        if (property.SetMethod is not null) KeepMethodSignature(property.SetMethod);
        foreach (var method in property.OtherMethods) KeepMethodSignature(method);
    }

    private void KeepEventSignature(EventDefinition item)
    {
        if (_passiveAttributeMetadataDepth == 0) ReplayPassiveBindings(item);
        var bindingOwner = _passiveBindingOwner;
        if (_passiveAttributeMetadataDepth != 0) _passiveBindingOwner = item;
        try { KeepEventSignatureCore(item); }
        finally { _passiveBindingOwner = bindingOwner; }
    }

    private void KeepEventSignatureCore(EventDefinition item)
    {
        if (_events.Add(item))
        {
            MarkSignatureType(item.EventType);
            if (!UsesReachedCopyTraversal(item.DeclaringType)) MarkAttributes(item);
        }
        if (item.AddMethod is not null) KeepMethodSignature(item.AddMethod);
        if (item.RemoveMethod is not null) KeepMethodSignature(item.RemoveMethod);
        if (item.InvokeMethod is not null) KeepMethodSignature(item.InvokeMethod);
        foreach (var method in item.OtherMethods) KeepMethodSignature(method);
    }

    private void MarkGenericParameter(GenericParameter parameter)
    {
        if (_passiveAttributeMetadataDepth == 0) ReplayPassiveBindings(parameter);
        var bindingOwner = _passiveBindingOwner;
        if (_passiveAttributeMetadataDepth != 0) _passiveBindingOwner = parameter;
        try { MarkGenericParameterCore(parameter); }
        finally { _passiveBindingOwner = bindingOwner; }
    }

    private void MarkGenericParameterCore(GenericParameter parameter)
    {
        if (!_genericParameters.Add(parameter)) return;
        MarkAttributes(parameter);
        foreach (var constraint in parameter.Constraints)
        {
            MarkSignatureType(constraint.ConstraintType);
            MarkAttributes(constraint);
        }
    }

    private void KeepMethodToken(MethodReference reference)
    {
        MarkSignatureType(reference.DeclaringType);
        MarkSignatureType(reference.ReturnType);
        foreach (var parameter in reference.Parameters) MarkSignatureType(parameter.ParameterType);
        if (reference.DeclaringType is ArrayType) return;
        var method = ResolveMember(reference) as MethodDefinition;
        if (reference is GenericInstanceMethod generic)
        {
            if (method is not null) BindParameters(method.GenericParameters, generic.GenericArguments);
            foreach (var argument in generic.GenericArguments) MarkSignatureType(argument);
        }
        if (method is null)
        {
            if (Resolve(reference.DeclaringType) is { } owner && IsStripped(owner))
                throw new NotSupportedException("cannot resolve managed method: " + reference.FullName);
            return;
        }
        // An explicit initializer token must retain a valid row; initializers
        // cannot use the throwing metadata stub used for ordinary methods.
        if (IsStripped(method.DeclaringType) && method.IsConstructor && method.IsStatic) MarkMethod(method);
        else KeepMethodSignature(method);
    }

    private void MarkMethod(MethodReference reference)
    {
        int modelDepth = _observingOriginalModel, passiveDepth = _passiveAttributeMetadataDepth;
        _observingOriginalModel = 0;
        _passiveAttributeMetadataDepth = 0;
        try { MarkExecutableMethod(reference); }
        finally
        {
            _observingOriginalModel = modelDepth;
            _passiveAttributeMetadataDepth = passiveDepth;
        }
    }

    private void MarkExecutableMethod(MethodReference reference)
    {
        MarkType(reference.DeclaringType);
        MarkSignatureType(reference.ReturnType);
        foreach (var parameter in reference.Parameters) MarkSignatureType(parameter.ParameterType);
        if (reference.DeclaringType is ArrayType) return;
        var method = ResolveMember(reference) as MethodDefinition;
        if (reference is GenericInstanceMethod generic)
        {
            if (method is not null) BindParameters(method.GenericParameters, generic.GenericArguments);
            foreach (var argument in generic.GenericArguments)
            {
                MarkSignatureType(argument);
                if (method is null || !IsStripped(method.DeclaringType)) QueueDemand(argument, Demand.OpaqueData);
            }
        }
        if (method is null)
        {
            if (Resolve(reference.DeclaringType) is { } owner && IsStripped(owner))
                throw new NotSupportedException("cannot resolve managed method: " + reference.FullName);
            return;
        }
        Require(method, MethodState.Body);
    }

    private void KeepAll(TypeDefinition type)
    {
        Require(type, TypeState.Runtime | TypeState.Members | TypeState.Explicit);
        foreach (var method in type.Methods) MarkMethod(method);
        foreach (var property in type.Properties) MarkProperty(property, true, true);
        foreach (var item in type.Events) MarkEvent(item);
        foreach (var nested in type.NestedTypes) KeepAll(nested);
    }

    private bool KeepsMethod(MethodDefinition method) => _signatureMethods.Contains(method) || _methods.Contains(method);

    private static void StubBody(MethodDefinition method)
    {
        method.Body = new MethodBody(method);
        method.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(OpCodes.Ldc_I4, 0x494C4454));
        method.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(OpCodes.Pop));
        method.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(OpCodes.Ldnull));
        method.Body.Instructions.Add(Mono.Cecil.Cil.Instruction.Create(OpCodes.Throw));
    }
}
