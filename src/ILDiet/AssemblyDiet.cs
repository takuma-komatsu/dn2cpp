using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using PreserveKind = Dn2Cpp.PreservationReader.PreserveKind;
using PreservePolicy = Dn2Cpp.PreservationReader.PreservePolicy;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using MethodDefinition = Mono.Cecil.MethodDefinition;
using FieldDefinition = Mono.Cecil.FieldDefinition;
using PropertyDefinition = Mono.Cecil.PropertyDefinition;
using EventDefinition = Mono.Cecil.EventDefinition;
using TypeReference = Mono.Cecil.TypeReference;
using CustomAttribute = Mono.Cecil.CustomAttribute;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using GenericParameter = Mono.Cecil.GenericParameter;
using TypeSpecification = Mono.Cecil.TypeSpecification;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet : IDisposable
{
    private readonly DietRequest _request;
    private readonly List<DietAssembly> _assemblies = new();
    private readonly ClosedResolver _resolver = new();
    private readonly ClosedMetadataResolver _metadataResolver;
    private readonly Dictionary<TypeDefinition, PreservePolicy> _policyTypes = new();
    private readonly Dictionary<TypeDefinition, PreservePolicy> _exportPolicyTypes = new();
    private readonly HashSet<TypeDefinition> _types = new();
    private readonly HashSet<MethodDefinition> _methods = new();
    private readonly HashSet<FieldDefinition> _fields = new();
    private readonly HashSet<PropertyDefinition> _properties = new();
    private readonly HashSet<EventDefinition> _events = new();
    private readonly HashSet<TypeDefinition> _conditional = new();
    private readonly Queue<MethodDefinition> _pending = new();
    private readonly HashSet<TypeDefinition> _interfaceHierarchies = new();
    private readonly HashSet<GenericParameter> _genericParameters = new();
    private readonly Dictionary<ModuleDefinition, DietAssembly> _byModule = new();
    private bool _cutsValidated = true;

    internal AssemblyDiet(DietRequest request)
    {
        _request = request;
        _metadataResolver = new ClosedMetadataResolver(_resolver);
    }

    internal void Run(string? resultPath)
    {
        Load();
        ReadPolicies();
        ValidateCuts();
        Seed();
        while (_pending.Count != 0) Scan(_pending.Dequeue());
        int removedTypes = 0, removedMethods = 0;
        foreach (var assembly in _assemblies)
        {
            if (assembly.Copy) continue;
            Sweep(assembly.Assembly.MainModule.Types, ref removedTypes, ref removedMethods);
        }
        WriteOutputs(resultPath);
        Console.WriteLine($"ILDiet: removed {removedTypes} types and {removedMethods} methods -> {Path.GetFullPath(_request.Output)}");
    }

    private void Load()
    {
        var paths = new List<string> { _request.Input };
        paths.AddRange(_request.References);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string value in paths)
        {
            string path = Path.GetFullPath(value);
            if (!File.Exists(path)) throw new NotSupportedException("assembly not found: " + path);
            var pe = new PEReader(File.OpenRead(path));
            if (!pe.HasMetadata || !pe.GetMetadataReader().IsAssembly)
            {
                pe.Dispose();
                throw new NotSupportedException("not a managed assembly: " + path);
            }
            var reader = pe.GetMetadataReader();
            string name = reader.GetString(reader.GetAssemblyDefinition().Name);
            if (!names.Add(name))
            {
                pe.Dispose();
                throw new NotSupportedException("duplicate assembly identity: " + name);
            }
            var definition = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
            {
                AssemblyResolver = _resolver, MetadataResolver = _metadataResolver,
                InMemory = true, ReadingMode = ReadingMode.Deferred,
                ReadSymbols = false,
            });
            if (definition.Modules.Count != 1)
                throw new NotSupportedException("multi-module assemblies are not supported: " + path);
            bool copy = _request.CopyAll || IsProtected(name);
            if (!copy && (definition.MainModule.Attributes & ModuleAttributes.ILOnly) == 0)
                throw new NotSupportedException("mixed-mode assemblies are not supported: " + path);
            var item = new DietAssembly
            {
                Index = _assemblies.Count, Path = path, Assembly = definition, PE = pe,
                Copy = copy,
            };
            _assemblies.Add(item);
            _byModule.Add(definition.MainModule, item);
            _resolver.Add(definition);
        }
    }

    private static bool IsProtected(string name) =>
        name is "mscorlib" or "netstandard" or "GodotSharp" or "GodotSharpEditor"
            or "Dn2Cpp.Runtime" or "DnZlib" or "DnBrotli" or "DnHttp"
        || name.StartsWith("System.", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.", StringComparison.Ordinal);

    private void ReadPolicies()
    {
        var modules = _assemblies.Select(a => new PreservationModule
        {
            Index = a.Index, AssemblyName = a.Assembly.Name.Name, Reader = a.PE.GetMetadataReader(),
        }).ToList();
        var reader = new PreservationReader(modules, _request.ProjectRoots, _request.LinkXml, _request.Features);
        foreach (var (key, policy) in reader.Read())
        {
            var type = (TypeDefinition)_assemblies[key.Module].Assembly.MainModule.LookupToken(MetadataTokens.GetToken(key.Type));
            _policyTypes.Add(type, policy);
        }
        var xmlReader = new PreservationReader(modules, _request.ProjectRoots, _request.LinkXml, _request.Features);
        foreach (var (key, policy) in xmlReader.Read(includeAttributes: false, warnings: false))
        {
            var type = (TypeDefinition)_assemblies[key.Module].Assembly.MainModule.LookupToken(MetadataTokens.GetToken(key.Type));
            _exportPolicyTypes.Add(type, policy);
        }
    }

    private void ValidateCuts()
    {
        if (_request.Cuts.Count == 0) return;
        var types = _assemblies.SelectMany(a => AllTypes(a.Assembly.MainModule.Types)
            .OrderBy(t => t.MetadataToken.ToInt32())).ToList();
        foreach (string cut in _request.Cuts)
        {
            int separator = cut.IndexOf("::", StringComparison.Ordinal);
            if (separator <= 0 || separator + 2 == cut.Length)
                throw new NotSupportedException("--cut " + cut + ": expected Type::Method");
            string typeName = cut[..separator], methodName = cut[(separator + 2)..];
            if (types.Any(t => t.HasGenericParameters
                    && typeName.StartsWith(GenericNameStem(t) + "_", StringComparison.Ordinal)
                    && t.Methods.Any(m => !m.HasGenericParameters && m.Name == methodName)))
            {
                _cutsValidated = false;
                continue;
            }
            // Compilation validates before completing generic templates. Its type names
            // use the metadata namespace and simple name, including for nested types.
            var type = types.FirstOrDefault(t => !t.HasGenericParameters
                    && (t.Namespace.Length == 0 ? t.Name : t.Namespace + "." + t.Name) == typeName);
            if (type is null || !type.Methods.Any(m => !m.HasGenericParameters && m.Name == methodName))
                throw new NotSupportedException("--cut " + cut + ": no matching method in the input assemblies");
        }
        if (!_cutsValidated)
        {
            // Closed names depend on the original model's base/interface discovery and
            // argument mangling. Preserve that input so Compilation can validate them.
            foreach (var assembly in _assemblies) assembly.Copy = true;
            Console.WriteLine("ILDiet: copying all assemblies for post-model validation of generic --cut selectors");
        }
    }

    private void Seed()
    {
        foreach (var (type, policy) in _policyTypes) ApplyPolicy(type, policy, false);
        foreach (var root in _request.Roots)
        {
            var matches = _assemblies.Where(a => root.Assembly.Length == 0 || a.Assembly.Name.Name == root.Assembly)
                .SelectMany(a => AllTypes(a.Assembly.MainModule.Types))
                .Where(t => RootMatches(t, root.Type, root.Assembly.Length != 0)).ToList();
            if (matches.Count == 0) throw new NotSupportedException("preservation root was not found: " + root.Type);
            foreach (var type in matches)
            {
                KeepAll(type);
                if (root.Assembly.Length == 0) KeepReflectionAncestors(type, new HashSet<TypeDefinition>());
            }
        }
        var app = _assemblies[0].Assembly.MainModule;
        if (app.EntryPoint is not null) MarkMethod(app.EntryPoint);
        foreach (var assembly in _assemblies)
        {
            if (assembly.Copy) continue;
            MarkAttributes(assembly.Assembly);
            MarkSecurity(assembly.Assembly);
            MarkAttributes(assembly.Assembly.MainModule);
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
            {
                if (type.Name == "<Module>") MarkType(type);
                foreach (var method in type.Methods)
                {
                    if ((assembly.Index == 0 || type.Name == "<Module>") && method.IsConstructor && method.IsStatic
                        || HasAttribute(method, "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute")
                        || HasAttribute(method, "Dn2Cpp.Runtime.NativeImplementationAttribute"))
                        MarkMethod(method);
                    if (assembly.Index == 0 && app.EntryPoint is null && PublicType(type)
                        && (method.IsPublic || method.IsConstructor)) MarkMethod(method);
                }
            }
        }
        // A copied assembly can still have static references into a stripped library.
        var stripped = _assemblies.Where(a => !a.Copy).Select(a => a.Assembly.Name.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in _assemblies.Where(a => a.Copy))
            if (assembly.Assembly.MainModule.AssemblyReferences.Any(r => stripped.Contains(r.Name)))
            {
                MarkAttributes(assembly.Assembly);
                MarkSecurity(assembly.Assembly);
                MarkAttributes(assembly.Assembly.MainModule);
                foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                {
                    MarkType(type.BaseType);
                    foreach (var implementation in type.Interfaces)
                    {
                        MarkType(implementation.InterfaceType);
                        MarkAttributes(implementation);
                    }
                    foreach (var parameter in type.GenericParameters) MarkGenericParameter(parameter);
                    MarkAttributes(type);
                    foreach (var field in type.Fields)
                    {
                        MarkType(field.FieldType);
                        MarkAttributes(field);
                        MarkMarshal(field);
                    }
                    foreach (var method in type.Methods)
                    {
                        MarkMethodMetadata(method);
                        Scan(method);
                    }
                }
            }
        foreach (var assembly in _assemblies)
            foreach (var exported in assembly.Assembly.MainModule.ExportedTypes)
            {
                try { MarkType(exported.Resolve()); }
                catch (AssemblyResolutionException) { }
            }
    }

    private static bool PublicType(TypeDefinition type) => type.IsPublic
        || type.IsNestedPublic && type.DeclaringType is not null && PublicType(type.DeclaringType);

    private static bool RootMatches(TypeDefinition type, string root, bool qualified)
    {
        if (Name(type) == root) return true;
        if (qualified) return false;
        string Full(string name) => type.Namespace.Length == 0 ? name : type.Namespace + "." + name;
        if (Full(type.Name) == root) return true;
        if (!type.HasGenericParameters) return false;
        static string Strip(string name) => name.IndexOf('`') is var tick && tick >= 0 ? name[..tick] : name;
        string name = Strip(type.Name);
        if (Full(name) == root) return true;
        // Closed mangled roots keep their definition; Compilation subsequently validates
        // that the requested instantiation actually exists in the program.
        return root.StartsWith(GenericNameStem(type) + "_", StringComparison.Ordinal);
    }

    private static string GenericNameStem(TypeDefinition type)
    {
        static string Strip(string name) => name.IndexOf('`') is var tick && tick >= 0 ? name[..tick] : name;
        string name = Strip(type.Name);
        for (var parent = type.DeclaringType; parent is not null; parent = parent.DeclaringType)
            name = Strip(parent.Name) + "_" + name;
        return type.Namespace.Length == 0 ? name : type.Namespace + "." + name;
    }

    private void KeepReflectionAncestors(TypeDefinition type, HashSet<TypeDefinition> seen)
    {
        if (!seen.Add(type)) return;
        if (type.BaseType is not null && Resolve(type.BaseType) is { } parent)
        {
            KeepAll(parent);
            KeepReflectionAncestors(parent, seen);
        }
        foreach (var implementation in type.Interfaces)
            if (Resolve(implementation.InterfaceType) is { } contract)
            {
                KeepAll(contract);
                KeepReflectionAncestors(contract, seen);
            }
    }

    private static bool HasAttribute(ICustomAttributeProvider provider, string name) =>
        provider.HasCustomAttributes && provider.CustomAttributes.Any(a => a.AttributeType.FullName == name);

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> roots)
    {
        foreach (var type in roots)
        {
            yield return type;
            foreach (var nested in AllTypes(type.NestedTypes)) yield return nested;
        }
    }

    private static string Name(TypeReference type) => type.FullName.Replace('/', '+');

    private bool IsStripped(TypeDefinition type) => _byModule.TryGetValue(type.Module, out var assembly) && !assembly.Copy;

    private TypeDefinition? Resolve(TypeReference reference)
    {
        if (reference is GenericParameter) return null;
        try { return reference.Resolve(); }
        catch (AssemblyResolutionException) { return null; }
    }

    private void MarkType(TypeReference? reference)
    {
        if (reference is null) return;
        if (reference is GenericParameter parameter)
        {
            if (!_genericParameters.Add(parameter)) return;
            foreach (var constraint in parameter.Constraints) MarkType(constraint.ConstraintType);
            return;
        }
        if (reference is GenericInstanceType generic)
        {
            foreach (var argument in generic.GenericArguments) MarkType(argument);
            // new T() and BCL generic factories do not carry a constructor token for T.
            foreach (var argument in generic.GenericArguments)
                if (Resolve(argument) is { } concrete) KeepDefaultConstructor(concrete);
        }
        if (reference is IModifierType modifier) MarkType(modifier.ModifierType);
        if (reference is TypeSpecification specification) MarkType(specification.ElementType);
        if (reference is FunctionPointerType pointer)
        {
            MarkType(pointer.ReturnType);
            foreach (var parameterType in pointer.Parameters) MarkType(parameterType.ParameterType);
            return;
        }
        var type = Resolve(reference);
        if (type is null || !IsStripped(type) || !_types.Add(type)) return;
        MarkType(type.DeclaringType);
        MarkType(type.BaseType);
        MarkAttributes(type);
        MarkSecurity(type);
        foreach (var parameterType in type.GenericParameters)
            MarkGenericParameter(parameterType);
        foreach (var implementation in type.Interfaces)
        {
            MarkType(implementation.InterfaceType);
            MarkAttributes(implementation);
        }
        // Instance/static field rows are retained together: layout, RVA initializers,
        // marshalling, and runtime structural equality can observe unused field rows.
        foreach (var field in type.Fields) MarkField(field);
        foreach (var method in type.Methods)
            if (method.IsVirtual || method.HasOverrides || type.IsInterface || IsDelegate(type)
                || method.IsConstructor && method.IsStatic) MarkMethod(method);
        if (type.HasInterfaces) KeepInterfaceHierarchy(type);
        if (_policyTypes.TryGetValue(type, out var policy) && _conditional.Add(type)) ApplyPolicy(type, policy, true);
    }

    private static bool IsDelegate(TypeDefinition type) => type.BaseType?.FullName is "System.Delegate" or "System.MulticastDelegate";

    private void KeepInterfaceHierarchy(TypeDefinition type)
    {
        if (!_interfaceHierarchies.Add(type)) return;
        // CLR interface mapping admits inherited public non-virtual methods too.
        for (TypeDefinition? current = type; current is not null; current = current.BaseType is null ? null : Resolve(current.BaseType))
        {
            if (!IsStripped(current)) break;
            MarkType(current);
            foreach (var method in current.Methods)
                if (method.IsPublic || method.HasOverrides) MarkMethod(method);
        }
    }

    private void KeepDefaultConstructor(TypeDefinition type)
    {
        if (!IsStripped(type)) return;
        foreach (var method in type.Methods)
            if (method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0) MarkMethod(method);
    }

    private void KeepAll(TypeDefinition type)
    {
        MarkType(type);
        if (!IsStripped(type)) return;
        foreach (var method in type.Methods) MarkMethod(method);
        foreach (var property in type.Properties) MarkProperty(property, true, true);
        foreach (var @event in type.Events) MarkEvent(@event);
        foreach (var nested in type.NestedTypes) KeepAll(nested);
    }

    private void MarkMethod(MethodReference reference)
    {
        MarkType(reference.DeclaringType);
        MarkType(reference.ReturnType);
        foreach (var parameter in reference.Parameters) MarkType(parameter.ParameterType);
        // Multidimensional array .ctor/Get/Set/Address are CLR pseudo-methods.
        if (reference.DeclaringType is ArrayType) return;
        if (reference is GenericInstanceMethod generic)
            foreach (var argument in generic.GenericArguments)
            {
                MarkType(argument);
                if (Resolve(argument) is { } concrete) KeepDefaultConstructor(concrete);
            }
        MethodDefinition? method;
        try { method = reference.Resolve(); }
        catch (AssemblyResolutionException) { return; }
        if (method is null)
        {
            if (Resolve(reference.DeclaringType) is { } target && IsStripped(target))
                throw new NotSupportedException("cannot resolve managed method: " + reference.FullName);
            return;
        }
        if (!IsStripped(method.DeclaringType) || !_methods.Add(method)) return;
        _pending.Enqueue(method);
        MarkMethodMetadata(method);
        foreach (var property in method.DeclaringType.Properties)
            if (property.GetMethod == method || property.SetMethod == method || property.OtherMethods.Contains(method))
                MarkProperty(property, false, false);
        foreach (var @event in method.DeclaringType.Events)
            if (@event.AddMethod == method || @event.RemoveMethod == method || @event.InvokeMethod == method
                || @event.OtherMethods.Contains(method)) MarkEvent(@event);
    }

    private void MarkGenericParameter(GenericParameter parameter)
    {
        MarkAttributes(parameter);
        foreach (var constraint in parameter.Constraints)
        {
            MarkType(constraint.ConstraintType);
            MarkAttributes(constraint);
        }
    }

    private void MarkMethodMetadata(MethodDefinition method)
    {
        MarkAttributes(method);
        MarkSecurity(method);
        MarkAttributes(method.MethodReturnType);
        MarkMarshal(method.MethodReturnType);
        foreach (var parameter in method.Parameters)
        {
            MarkAttributes(parameter);
            MarkMarshal(parameter);
        }
        foreach (var parameter in method.GenericParameters) MarkGenericParameter(parameter);
        foreach (var overridden in method.Overrides) MarkMethod(overridden);
    }

    private void MarkMarshal(IMarshalInfoProvider provider)
    {
        if (provider.HasMarshalInfo && provider.MarshalInfo is CustomMarshalInfo custom
            && custom.ManagedType is not null && Resolve(custom.ManagedType) is { } type)
            KeepAll(type);
    }

    private void MarkField(FieldReference reference)
    {
        MarkType(reference.DeclaringType);
        MarkType(reference.FieldType);
        FieldDefinition? field;
        try { field = reference.Resolve(); }
        catch (AssemblyResolutionException) { return; }
        if (field is null || !IsStripped(field.DeclaringType) || !_fields.Add(field)) return;
        MarkAttributes(field);
        MarkMarshal(field);
    }

    private void MarkProperty(PropertyDefinition property, bool getter, bool setter)
    {
        if (_properties.Add(property))
        {
            MarkType(property.PropertyType);
            foreach (var parameter in property.Parameters) MarkType(parameter.ParameterType);
            MarkAttributes(property);
        }
        if (getter && property.GetMethod is not null) MarkMethod(property.GetMethod);
        if (setter && property.SetMethod is not null) MarkMethod(property.SetMethod);
    }

    private void MarkEvent(EventDefinition @event)
    {
        if (!_events.Add(@event)) return;
        MarkType(@event.EventType);
        MarkAttributes(@event);
        if (@event.AddMethod is not null) MarkMethod(@event.AddMethod);
        if (@event.RemoveMethod is not null) MarkMethod(@event.RemoveMethod);
        if (@event.InvokeMethod is not null) MarkMethod(@event.InvokeMethod);
        foreach (var method in @event.OtherMethods) MarkMethod(method);
    }

    private void MarkAttributes(ICustomAttributeProvider provider)
    {
        if (!provider.HasCustomAttributes) return;
        foreach (var attribute in provider.CustomAttributes)
        {
            MarkMethod(attribute.Constructor);
            try
            {
                foreach (var argument in attribute.ConstructorArguments) MarkArgument(argument);
                var type = Resolve(attribute.AttributeType);
                foreach (var argument in attribute.Fields)
                {
                    MarkArgument(argument.Argument);
                    for (var current = type; current is not null; current = current.BaseType is null ? null : Resolve(current.BaseType))
                        foreach (var field in current.Fields.Where(f => f.Name == argument.Name)) MarkField(field);
                }
                foreach (var argument in attribute.Properties)
                {
                    MarkArgument(argument.Argument);
                    for (var current = type; current is not null; current = current.BaseType is null ? null : Resolve(current.BaseType))
                        foreach (var property in current.Properties.Where(p => p.Name == argument.Name)) MarkProperty(property, true, true);
                }
            }
            catch (AssemblyResolutionException)
            {
                // An external attribute blob stays opaque when its defining DLL is not an input.
            }
        }
    }

    private void MarkArgument(CustomAttributeArgument argument)
    {
        MarkType(argument.Type);
        if (argument.Value is TypeReference type) MarkType(type);
        else if (argument.Value is CustomAttributeArgument nested) MarkArgument(nested);
        else if (argument.Value is CustomAttributeArgument[] values)
            foreach (var value in values) MarkArgument(value);
    }

    private void MarkSecurity(ISecurityDeclarationProvider provider)
    {
        if (!provider.HasSecurityDeclarations) return;
        foreach (var declaration in provider.SecurityDeclarations)
        {
            foreach (var attribute in declaration.SecurityAttributes)
            {
                if (Resolve(attribute.AttributeType) is { } type) KeepAll(type);
                foreach (var argument in attribute.Fields) MarkArgument(argument.Argument);
                foreach (var argument in attribute.Properties) MarkArgument(argument.Argument);
            }
        }
    }

    private void Scan(MethodDefinition method)
    {
        MarkType(method.ReturnType);
        foreach (var parameter in method.Parameters) MarkType(parameter.ParameterType);
        if (!method.HasBody) return;
        foreach (var variable in method.Body.Variables) MarkType(variable.VariableType);
        foreach (var handler in method.Body.ExceptionHandlers) MarkType(handler.CatchType);
        foreach (var instruction in method.Body.Instructions)
        {
            switch (instruction.Operand)
            {
                case MethodReference target: MarkMethod(target); break;
                case FieldReference field: MarkField(field); break;
                case TypeReference type: MarkType(type); break;
                case CallSite signature:
                    MarkType(signature.ReturnType);
                    foreach (var parameter in signature.Parameters) MarkType(parameter.ParameterType);
                    break;
            }
        }
    }

    private void ApplyPolicy(TypeDefinition type, PreservePolicy policy, bool conditional)
    {
        PreserveKind kind = conditional ? policy.ConditionalKind : policy.Kind;
        var fields = conditional ? policy.ConditionalFields : policy.Fields;
        var methods = conditional ? policy.ConditionalMethods : policy.Methods;
        var properties = conditional ? policy.ConditionalProperties : policy.Properties;
        var events = conditional ? policy.ConditionalEvents : policy.Events;
        if (kind != PreserveKind.None || fields.Count != 0 || methods.Count != 0 || properties.Count != 0 || events.Count != 0)
            MarkType(type);
        if (!IsStripped(type)) return;
        foreach (var method in type.Methods)
            if ((kind & PreserveKind.Methods) != 0 || IsDelegate(type) && kind != PreserveKind.None
                || (kind & PreserveKind.DefaultConstructor) != 0 && method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0
                || methods.Contains(MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID))) MarkMethod(method);
        foreach (var field in type.Fields)
            if ((kind & PreserveKind.Fields) != 0 || fields.Contains(MetadataTokens.FieldDefinitionHandle((int)field.MetadataToken.RID))) MarkField(field);
        foreach (var property in type.Properties)
            if (properties.Contains(MetadataTokens.PropertyDefinitionHandle((int)property.MetadataToken.RID))) MarkProperty(property, false, false);
        foreach (var @event in type.Events)
            if (events.Contains(MetadataTokens.EventDefinitionHandle((int)@event.MetadataToken.RID))) MarkEvent(@event);
    }

    private void Sweep(Mono.Collections.Generic.Collection<TypeDefinition> types, ref int removedTypes, ref int removedMethods)
    {
        for (int i = types.Count - 1; i >= 0; i--)
        {
            var type = types[i];
            if (!_types.Contains(type) && type.Name != "<Module>")
            {
                removedTypes += AllTypes(new[] { type }).Count();
                removedMethods += AllTypes(new[] { type }).Sum(t => t.Methods.Count);
                types.RemoveAt(i);
                continue;
            }
            Sweep(type.NestedTypes, ref removedTypes, ref removedMethods);
            for (int j = type.Methods.Count - 1; j >= 0; j--)
                if (!_methods.Contains(type.Methods[j])) { type.Methods.RemoveAt(j); removedMethods++; }
            for (int j = type.Properties.Count - 1; j >= 0; j--)
            {
                var property = type.Properties[j];
                if (!_properties.Contains(property)) { type.Properties.RemoveAt(j); continue; }
                if (property.GetMethod is not null && !_methods.Contains(property.GetMethod)) property.GetMethod = null;
                if (property.SetMethod is not null && !_methods.Contains(property.SetMethod)) property.SetMethod = null;
                for (int k = property.OtherMethods.Count - 1; k >= 0; k--)
                    if (!_methods.Contains(property.OtherMethods[k])) property.OtherMethods.RemoveAt(k);
            }
            for (int j = type.Events.Count - 1; j >= 0; j--)
                if (!_events.Contains(type.Events[j])) type.Events.RemoveAt(j);
        }
    }

    public void Dispose()
    {
        foreach (var assembly in _assemblies)
        {
            assembly.Assembly.Dispose();
            assembly.PE.Dispose();
        }
        _resolver.Dispose();
    }
}

internal sealed class DietAssembly
{
    internal required int Index;
    internal required string Path;
    internal required AssemblyDefinition Assembly;
    internal required PEReader PE;
    internal required bool Copy;
}

internal sealed class ClosedResolver : IAssemblyResolver
{
    private readonly Dictionary<string, AssemblyDefinition> _assemblies = new(StringComparer.OrdinalIgnoreCase);
    internal void Add(AssemblyDefinition assembly) => _assemblies.Add(assembly.Name.Name, assembly);
    internal TypeDefinition? FindDefinition(string fullName)
    {
        foreach (var assembly in _assemblies.Values)
            if (assembly.MainModule.GetType(fullName) is { } type) return type;
        return null;
    }
    public AssemblyDefinition Resolve(AssemblyNameReference name) => _assemblies.TryGetValue(name.Name, out var assembly)
        ? assembly : throw new AssemblyResolutionException(name);
    public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);
    public void Dispose() { }
}

internal sealed class ClosedMetadataResolver : MetadataResolver
{
    private readonly ClosedResolver _assemblies;
    internal ClosedMetadataResolver(ClosedResolver assemblies) : base(assemblies)
    {
        _assemblies = assemblies;
    }

    public override TypeDefinition Resolve(TypeReference type)
    {
        try { return base.Resolve(type); }
        catch (AssemblyResolutionException)
        {
            // dn2cpp accepts implementation CoreLib without its reference facades.
            // Resolve only from the supplied set; never probe the host framework.
            var element = type.GetElementType();
            if (element.Scope is AssemblyNameReference assembly
                && (assembly.Name is "mscorlib" or "netstandard"
                    || assembly.Name.StartsWith("System.", StringComparison.Ordinal))
                && _assemblies.FindDefinition(element.FullName) is { } definition)
                return definition;
            throw;
        }
    }
}
