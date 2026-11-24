using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal bool FieldNullabilityDataUsed { get; private set; }
    private HashSet<(int Module, TypeDefinitionHandle Handle)>? _fieldNullabilityDefinitions;
    private Dictionary<string, (Module Module, TypeDefinitionHandle Handle)>? _nullableContextEnclosures;

    internal enum AttributeRows { All, ReachedConstructors, FieldNullability, NullableContext }
    private const string NullableContextAttributeName = "System.Runtime.CompilerServices.NullableContextAttribute";

    private static bool ReadsFieldNullability(MethodInfo method) =>
        method.Name == "Create" && method.DeclaringClass.FullName == "System.Reflection.NullabilityInfoContext"
        && method.Module.AssemblyName == "System.Private.CoreLib" && !method.IsStatic
        && method.Signature.ParameterTypes is [{ Class: { FullName: "System.Reflection.FieldInfo" } field }]
        && field.Module == method.Module && method.Signature.ReturnType.Class?.FullName == "System.Reflection.NullabilityInfo";

    // The CoreLib reader compares attribute names and namespaces. No user typeof
    // is present, and reading the metadata does not invoke the attribute constructor.
    private bool ReadsFieldNullabilityRow(Module module, CustomAttribute attribute)
    {
        if (!FieldNullabilityDataUsed || attribute.Parent.Kind is not (HandleKind.FieldDefinition
            or HandleKind.TypeDefinition or HandleKind.GenericParameter or HandleKind.ModuleDefinition))
            return false;
        string? name = AttributeTypeName(module.Reader, attribute);
        return attribute.Parent.Kind switch
        {
            HandleKind.ModuleDefinition => name == "System.Runtime.CompilerServices.NullablePublicOnlyAttribute",
            HandleKind.TypeDefinition => name is NullableContextAttributeName
                or "System.Runtime.CompilerServices.NullableAttribute",
            HandleKind.GenericParameter => name == "System.Runtime.CompilerServices.NullableAttribute",
            _ => name is "System.Runtime.CompilerServices.NullableAttribute" or "System.Diagnostics.CodeAnalysis.NotNullAttribute"
                or "System.Diagnostics.CodeAnalysis.MaybeNullAttribute" or "System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute"
                or "System.Diagnostics.CodeAnalysis.AllowNullAttribute" or "System.Diagnostics.CodeAnalysis.DisallowNullAttribute",
        };
    }

    internal bool HasFieldNullabilityDefinition(Module module, TypeDefinitionHandle handle) =>
        _fieldNullabilityDefinitions?.Contains((module.Index, handle)) == true;

    private void NoteFieldNullabilityDefinition(Module module, TypeDefinitionHandle handle)
    {
        _fieldNullabilityDefinitions ??= new();
        if (!_fieldNullabilityDefinitions.Add((module.Index, handle)))
            return;
        var reader = module.Reader;
        var definition = reader.GetTypeDefinition(handle);
        var attributes = definition.GetCustomAttributes();
        ReachAttributesOf(module, attributes, false, AttributeRows.FieldNullability);
        foreach (var field in definition.GetFields())
            ReachAttributesOf(module, reader.GetFieldDefinition(field).GetCustomAttributes(), false, AttributeRows.FieldNullability);
        foreach (var parameter in definition.GetGenericParameters())
            ReachAttributesOf(module, reader.GetGenericParameter(parameter).GetCustomAttributes(), false, AttributeRows.FieldNullability);

        // GetNullableContext follows DeclaringType until it finds the first context.
        var parent = definition.GetDeclaringType();
        if (!parent.IsNil && !HasNullableContext(module, attributes))
            NoteEnclosingNullableContext(module, parent);
    }

    private static bool HasNullableContext(Module module, CustomAttributeHandleCollection attributes) =>
        attributes.Any(handle => AttributeTypeName(module.Reader, module.Reader.GetCustomAttribute(handle))
            == NullableContextAttributeName);

    private bool NoteEnclosingNullableContext(Module module, TypeDefinitionHandle handle)
    {
        if (handle.IsNil)
            return false;
        var definition = module.Reader.GetTypeDefinition(handle);
        var attributes = definition.GetCustomAttributes();
        if (HasNullableContext(module, attributes))
            ReachAttributesOf(module, attributes, false, AttributeRows.NullableContext);
        else if (!NoteEnclosingNullableContext(module, definition.GetDeclaringType()))
            return false;
        if (module.GenericTemplates.Contains(handle))
        {
            _nullableContextEnclosures ??= new(StringComparer.Ordinal);
            _nullableContextEnclosures.TryAdd(GenericDefinitionKey(module, handle), (module, handle));
        }
        else
            NoteTypeIdentityClosure(GetTypeDescForDefinition(module, handle), keepSeed: false);
        return true;
    }

    internal bool IsNullableContextEnclosure(string key) => _nullableContextEnclosures?.ContainsKey(key) == true;

    // A declaring-type link needs a handle even when no site requested the definition's members.
    internal void NoteNullableContextEnclosureHandles()
    {
        if (_nullableContextEnclosures is null)
            return;
        foreach (var (name, owner) in _nullableContextEnclosures)
            NoteTypeofOpenGenericDef(name, ResolveOpenGenericDefKind(owner.Module, owner.Handle),
                GenericParamNames(owner.Module, owner.Handle));
    }

    private readonly Dictionary<string, List<(Module Module, CustomAttributeHandle Handle)>> _backendAttributeDemands = new(StringComparer.Ordinal);
    internal (MethodInfo ArgumentCtor, MethodInfo AsReadOnly)? AttributeArgumentMethods { get; private set; }

    private void ReachAttributeArgumentMethods(MethodInfo getter)
    {
        if (AttributeArgumentMethods is not null)
            return;
        var arg = getter.Signature.ReturnType.Class!.Context.TypeArgs[0];
        var element = arg.Class!;
        var ctor = element.EnsureMembers().Methods.First(m => m.Name == ".ctor" && !m.IsStatic
            && m.Signature.ParameterTypes is [{ Class.FullName: "System.Type" }, { IsObject: true }]);
        if (RawMetadataDefinition(getter.Module, "System.Array") is not { } arrayHandle
            || !getter.Module.ClassMap.TryGetValue(arrayHandle, out var array))
            throw new NotSupportedException("CustomAttributeData.ConstructorArguments requires the CoreLib Array definition.");
        var reader = array.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(array.Handle).GetMethods())
        {
            var definition = reader.GetMethodDefinition(handle);
            if (!reader.StringComparer.Equals(definition.Name, "AsReadOnly") || definition.GetGenericParameters().Count != 1)
                continue;
            var wrapper = InstantiateMethodOnClass(array, array.Module, handle, new[] { arg });
            if (wrapper.Signature.ParameterTypes is not [{ Kind: TypeKind.SZArray, Element: { } e }] || e != arg)
                continue;
            AttributeArgumentMethods = (ctor, wrapper);
            NoteArrayEnumerableElement(arg);
            NoteReferencedType(element);
            Reach(ctor);
            ReachIntrinsicTypeMethod(wrapper);
            return;
        }
        throw new NotSupportedException("CustomAttributeData.ConstructorArguments requires Array.AsReadOnly<CustomAttributeTypedArgument>.");
    }

    private readonly HashSet<(int Module, CustomAttributeHandle Handle)> _backendAttributeRows = new();
    private readonly Queue<(Module Module, CustomAttributeHandle Handle)> _pendingBackendAttributes = new();
    private long _backendAttributeReplayCount;
    private int _backendAllocationCursor;

    private void ReachBackendAttributes(Module module, CustomAttributeHandleCollection handles)
    {
        foreach (var handle in handles)
        {
            string? name = AttributeTypeName(module.Reader, module.Reader.GetCustomAttribute(handle));
            if (name is null || IsUserTypeofNamedFrameworkType(name)
                || !_backendAttributeRows.Add((module.Index, handle)))
                continue;
            if (!_backendAttributeDemands.TryGetValue(name, out var rows))
                _backendAttributeDemands.Add(name, rows = new List<(Module, CustomAttributeHandle)>());
            rows.Add((module, handle));
        }
        ReachAttributesOf(module, handles, reach: true);
    }

    private void NoteUserTypeofFrameworkType(string name)
    {
        if (!_userTypeofNamedFrameworkTypes.Add(name))
            return;
        // A backend's instance read also constructs rows admitted by a later typeof.
        // Only the registered rows for that type need another admission attempt.
        if (_backendAttributeDemands.TryGetValue(name, out var rows))
        {
            foreach (var row in rows)
                _pendingBackendAttributes.Enqueue(row);
            _backendAttributeDemands.Remove(name);
        }
    }

    private bool ReachPendingBackendDemands()
    {
        bool pending = false;
        if (_backend is not null)
            while (_backendAllocationCursor < _allocatedTypeOrder.Count)
            {
                var allocated = _allocatedTypeOrder[_backendAllocationCursor++];
                if (_backend.AllocatedTypeRoots(this, allocated) is not { } roots)
                    continue;
                pending = true;
                foreach (var method in roots.Methods)
                {
                    Reach(method);
                    ReachRootInitializer(method);
                }
                foreach (var cls in roots.AttributeReadClasses)
                    ReachBackendAttributeRows(cls);
            }
        pending |= _pendingBackendAttributes.Count > 0;
        while (_pendingBackendAttributes.Count > 0)
        {
            var row = _pendingBackendAttributes.Dequeue();
            _backendAttributeReplayCount++;
            if (DecodeCustomAttribute(row.Module, row.Handle, AttributeRows.All) is { } attribute)
                ConstructAttributeRow(attribute);
        }
        return pending;
    }
}
