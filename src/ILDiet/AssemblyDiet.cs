using Mono.Cecil;
using Mono.Cecil.Cil;
using CecilInstruction = Mono.Cecil.Cil.Instruction;
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
    private readonly HashSet<TypeDefinition> _runtimeTypes = new();
    private readonly HashSet<MethodDefinition> _methods = new();
    private readonly HashSet<MethodDefinition> _depthMethods = new();
    private readonly HashSet<FieldDefinition> _fields = new();
    private readonly HashSet<PropertyDefinition> _properties = new();
    private readonly HashSet<EventDefinition> _events = new();
    private readonly HashSet<TypeDefinition> _conditional = new();
    private readonly Queue<MethodDefinition> _pending = new();
    private readonly HashSet<TypeDefinition> _interfaceHierarchies = new();
    private readonly HashSet<TypeDefinition> _reflectionDataTypes = new();
    private readonly HashSet<GenericParameter> _genericParameters = new();
    private readonly HashSet<TypeDefinition> _reflectedOwnerSeen = new();
    private bool _constructsFromRuntimeType;
    private bool _runsReflectedMethods;
    private bool _depthRunsReflectedMethods;
    private bool _depthConstructsFromRuntimeType;
    private bool _depthReadsReflectedFields;
    private bool _depthBindsNamedConstructors;
    private bool _depthInitializesArrays;
    private bool _readsReflectedFields;
    private bool _bindsNamedConstructors;
    private readonly HashSet<TypeDefinition> _namedConstructorAllocations = new();
    private readonly HashSet<TypeDefinition> _namedConstructorOwners = new();
    private readonly List<(TypeDefinition Type, bool Direct)> _reflectedOwners = new();
    private readonly HashSet<TypeDefinition> _inheritedOwnerSeen = new();
    private bool _followsBaseTypes;
    private readonly HashSet<TypeDefinition> _instantiatedDefinitions = new();
    private int _reflectedOwnerCursor;
    private bool _keepingReflectedOwners;
    private bool _selectsReflectedMembers;
    private bool _reflectsGenericArguments;
    private bool _followsMemberTypes;
    private bool _followsNestedTypes;
    private bool _listsNestedTypes;
    // Sites naming a type parameter, whose rows every generic argument the program supplies selects.
    private readonly List<ReflectedTermWalk> _typeParameterRows = new();
    private readonly MemberNames _executedMemberNames = new();
    private readonly MemberNames _namedDelegateMemberNames = new();
    private bool _executesAnyMember;
    private readonly Dictionary<ModuleDefinition, ReflectionSummarizer> _summarizers = new();
    private bool _initializesArrays;
    private readonly Dictionary<ModuleDefinition, DietAssembly> _byModule = new();
    private readonly HashSet<MethodDefinition> _cutMethods = new();
    private bool _cutsValidated = true;

    internal AssemblyDiet(DietRequest request)
    {
        _request = request;
        _metadataResolver = new ClosedMetadataResolver(_resolver, PreserveMemberArrayKinds);
    }

    internal void Run(string? resultPath)
    {
        Load();
        ReadPolicies();
        ValidateCuts();
        ConfigureBackendPolicy();
        Seed();
        while (_pending.Count != 0) Scan(_pending.Dequeue());
        NoteClosedCallerDepths();
        KeepClosedApplicationSignatures();
        while (_pending.Count != 0) Scan(_pending.Dequeue());
        NoteClosedCallerDepths();
        CompleteBackendPolicy();
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
            bool copy = _request.CopyAll || IsProtected(name) && !_request.RewriteAssemblies.Contains(name);
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
        _resolver.Complete();
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
            foreach (var owner in types.Where(t => !t.HasGenericParameters
                    && (t.Namespace.Length == 0 ? t.Name : t.Namespace + "." + t.Name) == typeName))
                foreach (var method in owner.Methods)
                    if (method.Name == methodName) _cutMethods.Add(method);
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
        foreach (var root in _request.TypeRoots)
        {
            var type = FindPolicyType(root.Assembly, root.Type);
            MarkType(type);
            NoteAllocatedTypeDepths(type, type);
            foreach (var method in type.Methods)
                if (method.IsConstructor && method.IsStatic) MarkMethod(method);
        }
        foreach (var root in _request.MethodRoots)
        {
            var type = FindPolicyType(root.Assembly, root.Type);
            var methods = type.Methods.Where(m => m.Name == root.Method).ToList();
            if (methods.Count == 0)
                throw new NotSupportedException("method root was not found: " + root.Type + "::" + root.Method);
            foreach (var method in methods) MarkMethod(method);
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
                    if ((assembly.Index == 0 && !_suppressDefaultSeeds.Contains(type) || type.Name == "<Module>")
                            && method.IsConstructor && method.IsStatic
                        || HasAttribute(method, "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute")
                        || HasAttribute(method, "Dn2Cpp.Runtime.NativeImplementationAttribute"))
                        MarkMethod(method);
                    if (assembly.Index == 0 && app.EntryPoint is null && PublicType(type) && !_suppressDefaultSeeds.Contains(type)
                        && (method.IsPublic || method.IsConstructor)) MarkMethod(method);
                }
            }
        }
        // ILDiet does not scan every copied body, so any constructing member reference
        // in a copied assembly arms.
        foreach (var assembly in _assemblies)
            if (assembly.Copy && ReferencesRuntimeTypeConstruction(assembly.PE.GetMetadataReader()))
                ArmRuntimeTypeConstruction();
        foreach (var assembly in _assemblies)
            if (assembly.Copy && ReferencesReflectedMethodRun(assembly.PE.GetMetadataReader(),
                    !PreservationReader.IsFrameworkAssemblyName(assembly.Assembly.Name.Name)))
                ArmReflectedMethodRuns();
        foreach (var assembly in _assemblies)
            if (assembly.Copy && ReferencesReflectedFieldRead(assembly.PE.GetMetadataReader()))
                ArmReflectedFieldReads();
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

    private void MarkType(TypeReference? reference, bool recordDepth = true)
    {
        if (reference is null) return;
        if (reference is GenericParameter parameter)
        {
            if (!_genericParameters.Add(parameter)) return;
            foreach (var constraint in parameter.Constraints) MarkType(constraint.ConstraintType, recordDepth);
            return;
        }
        if (reference is GenericInstanceType generic)
        {
            NoteApplicationInstance(generic, shapeOwner: !recordDepth);
            foreach (var argument in generic.GenericArguments) MarkType(argument, recordDepth);
            // Generic serializers and factories select data members without direct
            // constructor or accessor tokens for T.
            foreach (var argument in generic.GenericArguments)
                if (Resolve(argument) is { } concrete) KeepReflectionData(concrete);
        }
        if (reference is IModifierType modifier) MarkType(modifier.ModifierType, recordDepth);
        if (reference is TypeSpecification specification) MarkType(specification.ElementType, recordDepth);
        if (reference is FunctionPointerType pointer)
        {
            MarkType(pointer.ReturnType, recordDepth);
            foreach (var parameterType in pointer.Parameters) MarkType(parameterType.ParameterType, recordDepth);
            return;
        }
        var type = Resolve(reference);
        if (reference is GenericInstanceType instance && type is not null
            && _depthRunsReflectedMethods && type.Module == _assemblies[0].Assembly.MainModule)
            NoteRuntimeTypeDepthContexts(instance, type, reflectionOnly: true);
        if (type is not null && !type.IsAbstract && _depthConstructsFromRuntimeType && _constructedTypes.Contains(type))
            NoteAllocatedTypeDepths(reference, type);
        if (type is null || !IsStripped(type) || !_runtimeTypes.Add(type)) return;
        _types.Add(type);
        MarkType(type.DeclaringType, recordDepth);
        MarkType(type.BaseType, recordDepth);
        MarkAttributes(type);
        MarkSecurity(type);
        foreach (var parameterType in type.GenericParameters)
            MarkGenericParameter(parameterType);
        foreach (var implementation in type.Interfaces)
        {
            MarkType(implementation.InterfaceType, recordDepth);
            MarkAttributes(implementation);
        }
        // Instance/static field rows are retained together: layout, RVA initializers,
        // marshalling, and runtime structural equality can observe unused field rows.
        foreach (var field in type.Fields) MarkField(field, recordDepth);
        foreach (var method in type.Methods)
            if (method.IsVirtual || method.HasOverrides || type.IsInterface || IsDelegate(type)
                || method.IsConstructor && method.IsStatic) MarkMethod(method, method.IsConstructor && type.Module == _assemblies[0].Assembly.MainModule);
        if (type.HasInterfaces) KeepInterfaceHierarchy(type);
        ApplyConditionalPolicy(type);
        if (_conditionalOwnMembers.Contains(type)) KeepOwnMembers(type);
        if (type.Module == _assemblies[0].Assembly.MainModule)
        {
            KeepApplicationMetadata(type);
            if (_runsReflectedMethods) KeepReflectedMethods(type);
            if (_constructsFromRuntimeType)
            {
                KeepRuntimeConstructionSurface(type);
                if (_depthConstructsFromRuntimeType) NoteAllocatedTypeDepths(reference, type);
            }
        }
        if (_bindsNamedConstructors && type.Module == _assemblies[0].Assembly.MainModule)
            KeepNamedConstructorHierarchy(type);
    }

    // A conditional rule applies once the type is used. Like the linker, a type a
    // recognized reflection site names or selects rows on counts as used even when only
    // its declaration is retained.
    private void ApplyConditionalPolicy(TypeDefinition type)
    {
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
                if (method.IsPublic || method.HasOverrides) MarkMethod(method, false);
        }
    }

    private void KeepReflectionData(TypeDefinition type)
    {
        // Data members and constructor signatures can refer back to their owner.
        if (!IsStripped(type) || !_reflectionDataTypes.Add(type)) return;
        MarkType(type);
        if (_reflectsGenericArguments) NoteReflectedOwner(type);
        foreach (var walk in _typeParameterRows.ToList()) KeepReflectedRows(walk, new[] { type });
        KeepReflectionDataType(type.BaseType);
        foreach (var method in type.Methods)
            if (method.IsConstructor && !method.IsStatic)
            {
                MarkMethod(method, false);
                foreach (var parameter in method.Parameters) KeepReflectionDataType(parameter.ParameterType);
            }
        foreach (var field in type.Fields)
            if (!field.IsStatic) KeepReflectionDataType(field.FieldType);
        foreach (var property in type.Properties)
            if (property.GetMethod is { IsStatic: false } || property.SetMethod is { IsStatic: false })
            {
                MarkProperty(property, true, true, false);
                KeepReflectionDataType(property.PropertyType);
            }
    }

    private void KeepReflectionDataType(TypeReference? reference)
    {
        if (reference is null) return;
        MarkType(reference);
        if (reference is FunctionPointerType) return;
        if (reference is TypeSpecification specification)
        {
            KeepReflectionDataType(specification.ElementType);
            return;
        }
        if (Resolve(reference) is { } type) KeepReflectionData(type);
    }

    // The shared classification of a method MemberReference whose parent is a TypeReference.
    private static ReflectionApi? ClassifyMemberReference(MetadataReader reader,
        System.Reflection.Metadata.MemberReference member)
    {
        if (member.Parent.Kind != HandleKind.TypeReference || member.GetKind() != MemberReferenceKind.Method)
            return null;
        string name = reader.GetString(member.Name);
        if (!ReflectionApis.IsCandidateName(name))
            return null;
        var signature = member.DecodeMethodSignature(RawSignatureProvider.Instance, null);
        return ReflectionApis.Classify(
            RawSignatureProvider.TypeReferenceName(reader, (TypeReferenceHandle)member.Parent), name,
            signature.ParameterTypes.AsSpan(), signature.Header.IsInstance, signature.GenericParameterCount);
    }

    // The shared classification of a Cecil call target, named as RawSignatureProvider names it.
    private static ReflectionApi? ClassifyReflectionTarget(MethodReference target)
    {
        if (!ReflectionApis.IsCandidateName(target.Name))
            return null;
        var parameters = new string[target.Parameters.Count];
        for (int i = 0; i < parameters.Length; i++)
            parameters[i] = RawTypeName(target.Parameters[i].ParameterType);
        int arity = target is GenericInstanceMethod instance
            ? instance.GenericArguments.Count
            : target.GenericParameters.Count;
        var owner = target.DeclaringType is GenericInstanceType generic ? generic.ElementType : target.DeclaringType;
        return ReflectionApis.Classify(RawTypeName(owner), target.Name, parameters, target.HasThis, arity);
    }

    private static string RawTypeName(TypeReference type)
    {
        switch (type)
        {
            case GenericParameter parameter:
                return (parameter.Type == GenericParameterType.Method ? "!!" : "!") + parameter.Position;
            case ArrayType array:
                return RawTypeName(array.ElementType)
                    + (array.IsVector ? "[]" : "[" + new string(',', array.Rank - 1) + "]");
            case ByReferenceType byReference:
                return RawTypeName(byReference.ElementType) + "&";
            case PointerType pointer:
                return RawTypeName(pointer.ElementType) + "*";
            case FunctionPointerType:
                return "method*";
            case GenericInstanceType generic:
                {
                    var text = new System.Text.StringBuilder(RawTypeName(generic.ElementType)).Append('<');
                    for (int i = 0; i < generic.GenericArguments.Count; i++)
                    {
                        if (i != 0) text.Append(',');
                        text.Append(RawTypeName(generic.GenericArguments[i]));
                    }
                    return text.Append('>').ToString();
                }
            case TypeSpecification specification:
                return RawTypeName(specification.ElementType);
            default:
                return type.DeclaringType is { } declaring
                    ? RawTypeName(declaring) + "+" + type.Name
                    : type.Namespace.Length == 0 ? type.Name : type.Namespace + "." + type.Name;
        }
    }

    private static bool ReferencesRuntimeTypeConstruction(MetadataReader reader)
    {
        foreach (var handle in reader.MemberReferences)
        {
            var member = reader.GetMemberReference(handle);
            // A generic method definition is called through a MethodSpec, which never arms.
            if (ClassifyMemberReference(reader, member) is { } api
                && ReflectionRoutes.ConstructsFromRuntimeType(api.Op)
                && !reader.GetBlobReader(member.Signature).ReadSignatureHeader().IsGeneric) return true;
        }
        return false;
    }

    private void ArmRuntimeTypeConstruction()
    {
        if (_constructsFromRuntimeType) return;
        _constructsFromRuntimeType = true;
        foreach (var type in ReflectionApplicationTypes().ToList())
        {
            MarkType(type);
            KeepRuntimeConstructionSurface(type);
        }
        foreach (var instance in _signatureApplicationInstances.ToList())
            if (Resolve(instance) is { } owner && !_suppressDefaultSeeds.Contains(owner)) MarkType(instance);
        KeepReflectedOwnerSurfaces(true);
    }

    private static bool ReferencesReflectedFieldRead(MetadataReader reader)
    {
        foreach (var handle in reader.MemberReferences)
            if (ClassifyMemberReference(reader, reader.GetMemberReference(handle)) is { } api
                && ReflectionRoutes.ReadsReflectedField(api.Op)) return true;
        return false;
    }

    private static bool ReferencesReflectedMethodRun(MetadataReader reader, bool userAssembly)
    {
        foreach (var handle in reader.MemberReferences)
            if (ClassifyMemberReference(reader, reader.GetMemberReference(handle)) is { } api
                && ReflectionRoutes.RunsCopiedReflectedMethod(api.Op, userAssembly)) return true;
        return false;
    }

    // Once a body runs or binds a reflected method, a recognized reflection site in the
    // transpiler can execute any non-constructor body of an application type, and an
    // application type's metadata lists every declared member. Removing one here would
    // make a by-name lookup answer null where the unstripped program finds the member.
    private void ArmReflectedMethodRuns()
    {
        if (_runsReflectedMethods) return;
        _runsReflectedMethods = true;
        // Emission models ordinary application definitions before walking the armed route.
        foreach (var type in ReflectionApplicationTypes().ToList())
        {
            MarkType(type);
            KeepReflectedMethods(type);
        }
        foreach (var instance in _signatureApplicationInstances.ToList())
            if (Resolve(instance) is { } owner && !_suppressDefaultSeeds.Contains(owner)) MarkType(instance);
        KeepReflectedOwnerSurfaces(true);
    }

    private void KeepReflectedMethods(TypeDefinition type)
    {
        foreach (var method in type.Methods)
            if (!method.IsConstructor)
            {
                MarkMethod(method, false);
                if (_depthRunsReflectedMethods && _reflectionDepthMethods.Add(method))
                    _virtualDepthContextsChanged = true;
            }
    }

    // The retained set must contain every body a recognized reflection site in the
    // transpiler admits. Such a site resolves its owner types only from a type token, a
    // type argument, an allocation, a member token, a constant type name or an assembly
    // listing, and from the element types, generic arguments, interfaces, member types,
    // nested types and base types of those; a lookup on an owner also selects members its
    // base chain inherits. Every such owner keeps its identity, a user-library owner keeps
    // the bodies the admitting sites' constant names select, and every site's own owners
    // keep the member rows its lookups and listings select.
    // A protected assembly a backend rewrites is its engine binding, which generated
    // trampolines invoke, never reflection; the backend opts it out.
    private void NoteReflectedOwner(TypeReference? reference)
    {
        var leaves = new List<TypeDefinition>();
        AddOwnerLeaves(reference, leaves);
        foreach (var type in leaves) NoteReflectedOwner(type);
    }

    // Adds the definitions a type reference denotes as owners: its element type and its
    // generic arguments. Returns whether it names a type parameter, which a site in an
    // exact instantiation resolves to the instantiation's argument.
    private bool AddOwnerLeaves(TypeReference? reference, List<TypeDefinition> leaves)
    {
        switch (reference)
        {
            case null or FunctionPointerType:
                return false;
            case GenericParameter:
                return true;
            case GenericInstanceType generic:
                bool parameter = AddOwnerLeaves(generic.ElementType, leaves);
                foreach (var argument in generic.GenericArguments) parameter |= AddOwnerLeaves(argument, leaves);
                return parameter;
            case TypeSpecification specification:
                return AddOwnerLeaves(specification.ElementType, leaves);
        }
        if (Resolve(reference) is { } type) leaves.Add(type);
        return false;
    }

    private bool IsReflectedOwnerCandidate(TypeDefinition type) =>
        IsStripped(type) && !IsProtected(type.Module.Assembly.Name.Name);

    private void NoteReflectedOwner(TypeDefinition type)
    {
        if (!IsReflectedOwnerCandidate(type) || !_reflectedOwnerSeen.Add(type)) return;
        _reflectedOwners.Add((type, true));
        // Its declaration suffices; a selected body retains the owner as a runtime type.
        MarkSignatureType(type);
        ApplyConditionalPolicy(type);
        // An interface listing or lookup answers with the interface closure.
        foreach (var implementation in type.Interfaces) NoteReflectedOwner(implementation.InterfaceType);
        NoteInheritedOwner(type.BaseType);
        if (_bindsNamedConstructors && type.Module != _assemblies[0].Assembly.MainModule)
            KeepNamedConstructorHierarchy(type);
        KeepReflectedOwnerSurfaces(false);
    }

    // Ordinary lookups exclude private base members; named delegate binding can select
    // them along the same base chain.
    private void NoteInheritedOwner(TypeReference? reference)
    {
        if (reference is null || Resolve(reference) is not { } type || !IsReflectedOwnerCandidate(type)
            || !_inheritedOwnerSeen.Add(type)) return;
        _reflectedOwners.Add((type, false));
        ApplyConditionalPolicy(type);
        foreach (var implementation in type.Interfaces) NoteReflectedOwner(implementation.InterfaceType);
        NoteInheritedOwner(type.BaseType);
        KeepReflectedOwnerSurfaces(false);
    }

    private void ReflectGenericArguments()
    {
        if (_reflectsGenericArguments) return;
        _reflectsGenericArguments = true;
        foreach (var type in _reflectionDataTypes.ToList()) NoteReflectedOwner(type);
    }

    private void ArmNamedConstructorBinding()
    {
        if (_bindsNamedConstructors) return;
        _bindsNamedConstructors = true;
        foreach (var type in _types.Where(t => t.Module == _assemblies[0].Assembly.MainModule).ToList())
            KeepNamedConstructorHierarchy(type);
        foreach (var type in _reflectedOwners.Where(owner => owner.Direct).Select(owner => owner.Type)
            .Concat(_namedConstructorAllocations).ToList())
            KeepNamedConstructorHierarchy(type);
    }

    private void KeepNamedConstructorHierarchy(TypeDefinition owner)
    {
        for (TypeDefinition? type = owner; type is not null; type = type.BaseType is null ? null : Resolve(type.BaseType))
        {
            if (!IsStripped(type) || IsProtected(type.Module.Assembly.Name.Name)) break;
            if (!_namedConstructorOwners.Add(type)) continue;
            foreach (var method in type.Methods)
                if (method.IsConstructor) MarkMethod(method, _depthBindsNamedConstructors);
        }
    }

    private bool KeepsReflectedOwnerSurfaces => _selectsReflectedMembers || _runsReflectedMethods || _constructsFromRuntimeType;

    // Applies the selection to owners not yet visited, or to every owner once the names,
    // the selection flags or the depth-root mode change. Member and nested types a
    // selection names join the owners and are visited by the same loop.
    private void KeepReflectedOwnerSurfaces(bool fromStart)
    {
        if (fromStart) _reflectedOwnerCursor = 0;
        if (_keepingReflectedOwners || !KeepsReflectedOwnerSurfaces) return;
        _keepingReflectedOwners = true;
        try
        {
            while (_reflectedOwnerCursor < _reflectedOwners.Count)
            {
                var (type, direct) = _reflectedOwners[_reflectedOwnerCursor++];
                KeepReflectedOwnerSurface(type, direct);
            }
        }
        finally
        {
            _keepingReflectedOwners = false;
        }
    }

    private void KeepReflectedOwnerSurface(TypeDefinition type, bool direct)
    {
        // Application owners keep their bodies through the armed application routes.
        if (type.Module != _assemblies[0].Assembly.MainModule)
        {
            bool initializes = direct || InitializesSelectedField(type);
            foreach (var method in type.Methods)
                if (method.HasBody && (method.IsConstructor && method.IsStatic ? initializes
                        : (direct || !method.IsConstructor
                            && (!method.IsPrivate || _namedDelegateMemberNames.Contains(method.Name)))
                        && (_executesAnyMember || IsExecutedName(method))))
                    MarkMethod(method, _depthRunsReflectedMethods);
        }
        if (direct && _instantiatedDefinitions.Contains(type))
            foreach (var method in type.Methods)
                // A runtime-instantiation template constructs through every instance
                // constructor and, once reflection invokes, runs any of its bodies.
                if (method.HasBody && (method.IsConstructor && !method.IsStatic || _runsReflectedMethods))
                    MarkMethod(method, false);
        if (direct && _followsBaseTypes) NoteReflectedOwner(type.BaseType);
        if (_followsMemberTypes)
        {
            foreach (var field in type.Fields)
                if (_executesAnyMember || _executedMemberNames.Contains(field.Name)) NoteReflectedOwner(field.FieldType);
            foreach (var property in type.Properties)
                if (_executesAnyMember || _executedMemberNames.Contains(property.Name))
                {
                    NoteReflectedOwner(property.PropertyType);
                    foreach (var parameter in property.Parameters) NoteReflectedOwner(parameter.ParameterType);
                }
            foreach (var @event in type.Events)
                if (_executesAnyMember || _executedMemberNames.Contains(@event.Name)) NoteReflectedOwner(@event.EventType);
            foreach (var method in type.Methods)
                if (_executesAnyMember || IsExecutedName(method))
                {
                    NoteReflectedOwner(method.ReturnType);
                    foreach (var parameter in method.Parameters) NoteReflectedOwner(parameter.ParameterType);
                }
        }
        if (direct && _followsNestedTypes)
            foreach (var nested in type.NestedTypes)
                if (_listsNestedTypes || _executesAnyMember || _executedMemberNames.Contains(nested.Name))
                    NoteReflectedOwner(nested);
    }

    private bool IsExecutedName(MethodDefinition method) => IsSelectedName(method, _executedMemberNames);

    // Reading or writing a selected static field runs its declaring type's initializer,
    // and a flattened lookup on a derived owner selects the base's non-private ones.
    private bool InitializesSelectedField(TypeDefinition type)
    {
        foreach (var field in type.Fields)
            if (field.IsStatic && !field.IsLiteral && !field.IsPrivate
                && (_executesAnyMember || _executedMemberNames.Contains(field.Name))) return true;
        return false;
    }

    // A name selects a method, or the accessors of a property or event it names.
    private static bool IsSelectedName(MethodDefinition method, MemberNames names)
    {
        if (names.Contains(method.Name)) return true;
        if (!method.IsSpecialName) return false;
        foreach (var property in method.DeclaringType.Properties)
            if ((property.GetMethod == method || property.SetMethod == method) && names.Contains(property.Name))
                return true;
        foreach (var @event in method.DeclaringType.Events)
            if ((@event.AddMethod == method || @event.RemoveMethod == method || @event.InvokeMethod == method)
                && names.Contains(@event.Name)) return true;
        return false;
    }

    // A recognized site of any kind observes its owners' declarations and the member rows
    // its lookups and listings select on them. Those rows survive as signature-only stubs;
    // a type initializer never does, since eager startup would run it. Bodies stay with
    // the admitting sites and the owners' activated conditional rules.
    private void KeepReflectedRows(ReflectedTermWalk walk, IEnumerable<TypeDefinition> leaves)
    {
        var owners = new List<(TypeDefinition Type, bool Direct)>();
        var seen = new HashSet<(TypeDefinition, bool)>();
        foreach (var type in leaves) AddRowOwner(owners, seen, type, true);
        for (int i = 0; i < owners.Count; i++)
        {
            var (type, direct) = owners[i];
            MarkSignatureType(type);
            ApplyConditionalPolicy(type);
            // An application owner keeps every declared member row once it is retained.
            if (type.Module != _assemblies[0].Assembly.MainModule)
            {
                foreach (var method in type.Methods)
                    if (!(method.IsConstructor && method.IsStatic) && (direct || !method.IsConstructor
                            && (!method.IsPrivate || walk.NamedDelegate))
                        && (walk.AnyMember || IsSelectedName(method, walk.Names)))
                        KeepMethodSignature(method);
                foreach (var property in type.Properties)
                    if (walk.AnyMember || walk.Names.Contains(property.Name)) KeepPropertySignature(property);
                foreach (var @event in type.Events)
                    if (walk.AnyMember || walk.Names.Contains(@event.Name)) KeepEventSignature(@event);
            }
            if (direct && walk.BaseTypes) AddRowOwners(owners, seen, type.BaseType);
            if (walk.MemberTypes)
            {
                foreach (var field in type.Fields)
                    if (walk.AnyMember || walk.Names.Contains(field.Name)) AddRowOwners(owners, seen, field.FieldType);
                foreach (var property in type.Properties)
                    if (walk.AnyMember || walk.Names.Contains(property.Name))
                    {
                        AddRowOwners(owners, seen, property.PropertyType);
                        foreach (var parameter in property.Parameters) AddRowOwners(owners, seen, parameter.ParameterType);
                    }
                foreach (var @event in type.Events)
                    if (walk.AnyMember || walk.Names.Contains(@event.Name)) AddRowOwners(owners, seen, @event.EventType);
                foreach (var method in type.Methods)
                    if (walk.AnyMember || IsSelectedName(method, walk.Names))
                    {
                        AddRowOwners(owners, seen, method.ReturnType);
                        foreach (var parameter in method.Parameters) AddRowOwners(owners, seen, parameter.ParameterType);
                    }
            }
            if (direct && walk.NestedTypes)
                foreach (var nested in type.NestedTypes)
                    if (walk.ListsNestedTypes || walk.AnyMember || walk.Names.Contains(nested.Name))
                        AddRowOwner(owners, seen, nested, true);
        }
    }

    private void AddRowOwners(List<(TypeDefinition Type, bool Direct)> owners, HashSet<(TypeDefinition, bool)> seen,
        TypeReference? reference)
    {
        var leaves = new List<TypeDefinition>();
        AddOwnerLeaves(reference, leaves);
        foreach (var type in leaves) AddRowOwner(owners, seen, type, true);
    }

    // Mirrors the owner closure: interfaces are owners, and a base chain contributes
    // the members it passes on.
    private void AddRowOwner(List<(TypeDefinition Type, bool Direct)> owners, HashSet<(TypeDefinition, bool)> seen,
        TypeDefinition type, bool direct)
    {
        if (!IsReflectedOwnerCandidate(type) || !seen.Add((type, direct))) return;
        owners.Add((type, direct));
        foreach (var implementation in type.Interfaces)
        {
            var leaves = new List<TypeDefinition>();
            AddOwnerLeaves(implementation.InterfaceType, leaves);
            foreach (var leaf in leaves) AddRowOwner(owners, seen, leaf, true);
        }
        if (type.BaseType is not null && Resolve(type.BaseType) is { } parent) AddRowOwner(owners, seen, parent, false);
    }

    // The site kinds whose admission can select a body: execution, binding, construction,
    // attribute rows, instantiation templates and a type initializer run.
    private static bool AdmitsReflectedBodies(ReflectionApi api) =>
        api.Recognized && (api.Kind is ReflectionOpKind.Execute or ReflectionOpKind.Bind or ReflectionOpKind.Construct
            or ReflectionOpKind.AttributeInstances or ReflectionOpKind.AttributeData or ReflectionOpKind.Instantiate
            || api.Op == ReflectionOp.RunClassConstructor);

    // The transpiler admits a body only for a member an admitting site selects by a
    // constant name, or by a nameless lookup or an enumeration, which may select any
    // member, on an owner the site's own summary resolves. Every owner the summary terms
    // can denote joins the owner set and a name selects on every owner, which
    // over-approximates the pairs the transpiler resolves. Every recognized site, admitting
    // or not, also keeps the declarations and member rows it observes on its own owners.
    private void NoteReflectedSelections(MethodDefinition method)
    {
        if (method.RVA == 0 || !_byModule.TryGetValue(method.Module, out var assembly)) return;
        if (!_summarizers.TryGetValue(method.Module, out var summarizer))
            _summarizers.Add(method.Module, summarizer = new ReflectionSummarizer(assembly.PE.GetMetadataReader(), assembly.PE));
        var body = assembly.PE.GetMethodBody(method.RVA);
        var summary = summarizer.Summarize(MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID),
            body, ILDecoder.Decode(body.GetILContent()));
        bool selects = _selectsReflectedMembers, any = _executesAnyMember, members = _followsMemberTypes;
        bool nested = _followsNestedTypes, listsNested = _listsNestedTypes, bases = _followsBaseTypes;
        int names = _executedMemberNames.Count, delegateNames = _namedDelegateMemberNames.Count;
        foreach (var site in summary.Sites)
        {
            var api = site.Api;
            if (!api.Recognized) continue;
            var walk = new ReflectedTermWalk(summary, method.Module);
            walk.NamedDelegate = api.Op == ReflectionOp.CreateDelegateNamed;
            if (api.Kind == ReflectionOpKind.Construct || api.Op == ReflectionOp.InvokeMember)
                walk.Names.Add(".ctor");
            if (api.Op is ReflectionOp.ActivatorCreateNamed or ReflectionOp.AssemblyCreateInstance
                or ReflectionOp.TypeGetType or ReflectionOp.AssemblyGetType)
                NoteTypeNameStrings(walk, SiteOperand(site, api.Name));
            else
                NoteMemberNameStrings(walk, SiteOperand(site, api.Name));
            // The site's own lookup or listing selects on its owners as a derived one does.
            if (api.Kind == ReflectionOpKind.Lookup) NoteLookup(walk, api.Op, api.Name >= 0, -1);
            else if (api.Kind == ReflectionOpKind.Enumerate) NoteEnumeration(walk, api.Op);
            NoteReflectedTerms(walk, site.Receiver);
            NoteReflectedTerms(walk, site.TypeArgument);
            foreach (var operand in site.Operands) NoteReflectedTerms(walk, operand);
            NoteListedAssemblyTypes(walk);
            KeepReflectedRows(walk, walk.Leaves);
            if (walk.TypeParameters)
            {
                _typeParameterRows.Add(walk);
                KeepReflectedRows(walk, _reflectionDataTypes.ToList());
            }
            if (!AdmitsReflectedBodies(api)) continue;
            _selectsReflectedMembers = true;
            _executedMemberNames.UnionWith(walk.Names);
            if (walk.NamedDelegate) _namedDelegateMemberNames.UnionWith(walk.Names);
            _executesAnyMember |= walk.AnyMember;
            _followsMemberTypes |= walk.MemberTypes;
            _followsNestedTypes |= walk.NestedTypes;
            _listsNestedTypes |= walk.ListsNestedTypes;
            _followsBaseTypes |= walk.BaseTypes;
            foreach (var type in walk.Leaves) NoteReflectedOwner(type);
            if (walk.TypeParameters) ReflectGenericArguments();
            if (api.Op == ReflectionOp.MakeGenericType)
            {
                var target = new ReflectedTermWalk(summary, method.Module);
                NoteReflectedTerms(target, SiteOperand(site, api.Target));
                foreach (var definition in target.Leaves)
                    if (definition.HasGenericParameters && _instantiatedDefinitions.Add(definition))
                        _reflectedOwnerCursor = 0;
            }
        }
        if (selects != _selectsReflectedMembers || any != _executesAnyMember || members != _followsMemberTypes
            || nested != _followsNestedTypes || listsNested != _listsNestedTypes || bases != _followsBaseTypes
            || names != _executedMemberNames.Count || delegateNames != _namedDelegateMemberNames.Count)
            _reflectedOwnerCursor = 0;
        KeepReflectedOwnerSurfaces(false);
    }

    private static RValue SiteOperand(RSite site, sbyte slot) => slot switch
    {
        ReflectionApi.Receiver => site.Receiver,
        ReflectionApi.MethodTypeArgument => site.TypeArgument,
        >= 0 when slot < site.Operands.Length => site.Operands[slot],
        _ => RValue.None,
    };

    // The member names sites select, ignoring case as the widest lookup does. A name
    // ending in '*' also selects every name it starts, as GetMember, GetDefaultMembers
    // and InvokeMember read it.
    private sealed class MemberNames
    {
        private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _prefixes = new();

        internal int Count => _names.Count;

        internal void Add(string name)
        {
            if (_names.Add(name) && name.Length > 0 && name[^1] == '*') _prefixes.Add(name[..^1]);
        }

        internal void UnionWith(MemberNames other)
        {
            foreach (var name in other._names) Add(name);
        }

        internal bool Contains(string name)
        {
            if (_names.Contains(name)) return true;
            foreach (var prefix in _prefixes)
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }

    private sealed class ReflectedTermWalk
    {
        internal readonly ReflectionSummary Summary;
        internal readonly ModuleDefinition Module;
        internal readonly HashSet<int> Visited = new();
        internal readonly HashSet<int> NameVisited = new();
        internal readonly List<TypeDefinition> Leaves = new();
        internal readonly List<RTerm> Assemblies = new();
        internal bool ListsAssemblyTypes;
        // The members the site's lookups and listings select, and the derivations its
        // owners follow.
        internal readonly MemberNames Names = new();
        internal bool AnyMember, MemberTypes, NestedTypes, ListsNestedTypes, BaseTypes, TypeParameters, NamedDelegate;

        internal ReflectedTermWalk(ReflectionSummary summary, ModuleDefinition module)
        {
            Summary = summary;
            Module = module;
        }
    }

    // Visits every term a site operand depends on, so every owner source and every
    // derivation the transpiler's resolution follows from it is seen.
    private void NoteReflectedTerms(ReflectedTermWalk walk, RValue value)
    {
        if (value.Alternatives is null) return;
        foreach (int id in value.Alternatives) NoteReflectedTerm(walk, id);
    }

    private void NoteReflectedTerm(ReflectedTermWalk walk, int id)
    {
        if (id < 0 || !walk.Visited.Add(id)) return;
        var term = walk.Summary.Terms[id];
        switch (term.Kind)
        {
            case TermKind.TypeToken or TermKind.ExactObject or TermKind.Closure:
                walk.TypeParameters |= AddOwnerLeaves(walk.Module.LookupToken(term.Token) as TypeReference, walk.Leaves);
                break;
            case TermKind.MethodSpecArg:
                if (walk.Module.LookupToken(term.Token) is GenericInstanceMethod generic
                    && term.Number >= 0 && term.Number < generic.GenericArguments.Count)
                    walk.TypeParameters |= AddOwnerLeaves(generic.GenericArguments[(int)term.Number], walk.Leaves);
                break;
            case TermKind.MemberToken:
                if (walk.Module.LookupToken(term.Token) is Mono.Cecil.MemberReference member)
                    walk.TypeParameters |= AddOwnerLeaves(member.DeclaringType, walk.Leaves);
                break;
            case TermKind.TypeName:
                if (term.Text is { } text) NoteReflectedTypeName(walk, text);
                break;
            case TermKind.Assembly:
                walk.Assemblies.Add(term);
                break;
            case TermKind.Lookup:
            {
                int name = term.Children.Length > ReflectionSummary.LookupName ? term.Children[ReflectionSummary.LookupName] : -1;
                NoteLookup(walk, (ReflectionOp)term.Number, name >= 0, name);
                break;
            }
            case TermKind.Enumeration:
                if ((ReflectionOp)term.Number == ReflectionOp.GetMethods && term.Children.Length > 2 && term.Children[2] >= 0)
                    NoteLookup(walk, (ReflectionOp)term.Number, true, term.Children[2]);
                else
                    NoteEnumeration(walk, (ReflectionOp)term.Number);
                break;
            case TermKind.TypeOfMember or TermKind.Parameter:
                walk.MemberTypes = true;
                break;
            case TermKind.BaseOf:
                walk.BaseTypes = true;
                break;
        }
        foreach (int child in term.Children) NoteReflectedTerm(walk, child);
    }

    // A lookup selects the members its constant names spell, or any member without a name
    // operand. A site's own name operand supplies its names before the walk.
    private static void NoteLookup(ReflectedTermWalk walk, ReflectionOp op, bool named, int nameTerm)
    {
        if (op is ReflectionOp.GetConstructor) walk.Names.Add(".ctor");
        else if (op is ReflectionOp.TypeInitializer) walk.Names.Add(".cctor");
        else if (!named) walk.AnyMember = true;
        else if (nameTerm >= 0) NoteMemberNameStrings(walk, walk.Summary.ValueOf(nameTerm));
        if (op is ReflectionOp.GetNestedType or ReflectionOp.GetMember) walk.NestedTypes = true;
    }

    private static void NoteEnumeration(ReflectedTermWalk walk, ReflectionOp op)
    {
        switch (op)
        {
            case ReflectionOp.GetConstructors:
                walk.Names.Add(".ctor");
                break;
            case ReflectionOp.GetMethods or ReflectionOp.GetProperties or ReflectionOp.GetFields
                or ReflectionOp.GetEvents or ReflectionOp.GetDefaultMembers:
                walk.AnyMember = true;
                break;
            case ReflectionOp.GetMembers:
                walk.AnyMember = walk.NestedTypes = true;
                break;
            case ReflectionOp.GetNestedTypes:
                walk.NestedTypes = walk.ListsNestedTypes = true;
                break;
            case ReflectionOp.AssemblyGetTypes:
                walk.ListsAssemblyTypes = true;
                break;
        }
    }

    private static void NoteMemberNameStrings(ReflectedTermWalk walk, RValue value)
    {
        if (value.Alternatives is null) return;
        foreach (int id in value.Alternatives)
        {
            var term = walk.Summary.Terms[id];
            if (term.Kind == TermKind.Str && term.Text is { } text) walk.Names.Add(text);
            else if (term.Kind == TermKind.Set && walk.NameVisited.Add(id)) NoteMemberNameStrings(walk, walk.Summary.ValueOf(id));
        }
    }

    private void NoteTypeNameStrings(ReflectedTermWalk walk, RValue value)
    {
        if (value.Alternatives is null) return;
        foreach (int id in value.Alternatives)
        {
            var term = walk.Summary.Terms[id];
            if (term.Kind == TermKind.Str && term.Text is { } text) NoteReflectedTypeName(walk, text);
            else if (term.Kind == TermKind.Set && walk.NameVisited.Add(id)) NoteTypeNameStrings(walk, walk.Summary.ValueOf(id));
        }
    }

    // A type name selects its type and generic arguments in every stripped assembly,
    // ignoring case and assembly qualification: a superset of Type.GetType's search.
    private void NoteReflectedTypeName(ReflectedTermWalk walk, string text)
    {
        if (ReflectionTypeName.Parse(text) is { } name) NoteReflectedTypeName(walk, name);
    }

    private void NoteReflectedTypeName(ReflectedTermWalk walk, ReflectionTypeName name)
    {
        foreach (var argument in name.GenericArguments) NoteReflectedTypeName(walk, argument);
        string top = name.TopLevelName;
        int dot = top.LastIndexOf('.');
        string space = dot < 0 ? "" : top.Substring(0, dot), simple = dot < 0 ? top : top.Substring(dot + 1);
        foreach (var assembly in _assemblies)
        {
            if (assembly.Copy) continue;
            foreach (var candidate in assembly.Assembly.MainModule.Types)
            {
                if (!string.Equals(candidate.Name, simple, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(candidate.Namespace, space, StringComparison.OrdinalIgnoreCase)) continue;
                TypeDefinition? type = candidate;
                foreach (string nested in name.NestedNames)
                    type = type?.NestedTypes.FirstOrDefault(t => string.Equals(t.Name, nested, StringComparison.OrdinalIgnoreCase));
                if (type is null) continue;
                walk.Leaves.Add(type);
            }
        }
    }

    // An assembly listing selects every type of each assembly the site's terms can name.
    private void NoteListedAssemblyTypes(ReflectedTermWalk walk)
    {
        if (!walk.ListsAssemblyTypes) return;
        var modules = new List<ModuleDefinition>();
        foreach (var term in walk.Assemblies)
        {
            if (term.Text is { } text)
            {
                int comma = text.IndexOf(',');
                string simple = (comma < 0 ? text : text.Substring(0, comma)).Trim();
                foreach (var assembly in _assemblies)
                    if (string.Equals(assembly.Assembly.Name.Name, simple, StringComparison.OrdinalIgnoreCase))
                        modules.Add(assembly.Assembly.MainModule);
            }
            else if (term.Children.Length > 0 && term.Children[0] >= 0)
            {
                if (NamesLeafTypes(walk, term.Children[0], new HashSet<int>()))
                    foreach (var type in walk.Leaves) modules.Add(type.Module);
                else
                    foreach (var assembly in _assemblies)
                        if (!assembly.Copy) modules.Add(assembly.Assembly.MainModule);
            }
            else
                modules.Add(term.Number == 2 ? _assemblies[0].Assembly.MainModule : walk.Module);
        }
        var listed = new List<TypeDefinition>();
        foreach (var module in modules.Distinct())
            foreach (var type in AllTypes(module.Types))
                if (!_suppressDefaultSeeds.Contains(type)) listed.Add(type);
        walk.Leaves.AddRange(listed);
    }

    // Whether the walk's leaves hold every type a term can denote: one the summary names,
    // not a type argument an exact instantiation supplies, a type deriving from a boxed
    // one, or a type derived from another (a base, element, member or generic argument).
    private bool NamesLeafTypes(ReflectedTermWalk walk, int id, HashSet<int> seen)
    {
        if (id < 0 || !seen.Add(id)) return true;
        var term = walk.Summary.Terms[id];
        var leaves = new List<TypeDefinition>();
        switch (term.Kind)
        {
            case TermKind.Set:
                foreach (int child in term.Children)
                    if (!NamesLeafTypes(walk, child, seen)) return false;
                return true;
            case TermKind.TypeToken or TermKind.ExactObject or TermKind.Closure:
                return term.Number != ReflectionSummary.InexactBox
                    && !AddOwnerLeaves(walk.Module.LookupToken(term.Token) as TypeReference, leaves);
            case TermKind.MethodSpecArg:
                return walk.Module.LookupToken(term.Token) is GenericInstanceMethod generic
                    && term.Number >= 0 && term.Number < generic.GenericArguments.Count
                    && !AddOwnerLeaves(generic.GenericArguments[(int)term.Number], leaves);
            case TermKind.TypeName or TermKind.Null:
                return true;
        }
        return false;
    }

    // Array.Initialize runs a value-type element's parameterless constructor, which no
    // IL names, so once the program calls it, directly or through a method group, every
    // stripped value type keeps its own.
    private void ArmArrayInitialize()
    {
        if (_initializesArrays) return;
        _initializesArrays = true;
        foreach (var assembly in _assemblies)
        {
            if (assembly.Copy) continue;
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                if (type.IsValueType)
                    foreach (var method in type.Methods)
                        if (method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0)
                            MarkMethod(method, _depthInitializesArrays);
        }
    }

    private void KeepInstanceConstructors(TypeDefinition type, bool depthRoot = true)
    {
        foreach (var method in type.Methods)
            if (method.IsConstructor && !method.IsStatic) MarkMethod(method, depthRoot);
    }

    private void KeepAll(TypeDefinition type)
    {
        MarkType(type);
        NoteAllocatedTypeDepths(type, type);
        if (!IsStripped(type)) return;
        foreach (var method in type.Methods) MarkMethod(method);
        foreach (var property in type.Properties) MarkProperty(property, true, true);
        foreach (var @event in type.Events) MarkEvent(@event);
        foreach (var nested in type.NestedTypes) KeepAll(nested);
    }

    private void MarkMethod(MethodReference reference, bool depthRoot = true)
    {
        MarkType(reference.DeclaringType, depthRoot);
        MarkType(reference.ReturnType, depthRoot);
        foreach (var parameter in reference.Parameters) MarkType(parameter.ParameterType, depthRoot);
        // Multidimensional array .ctor/Get/Set/Address are CLR pseudo-methods.
        if (reference.DeclaringType is ArrayType) return;
        if (reference is GenericInstanceMethod generic)
        {
            if (depthRoot) NoteGenericMethodDepth(generic);
            foreach (var argument in generic.GenericArguments)
            {
                MarkType(argument, depthRoot);
                if (Resolve(argument) is { } concrete) KeepReflectionData(concrete);
            }
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
        if (depthRoot)
        {
            if (_depthMethods.Add(method)) NoteRootedMethodReceiverDepths(method);
            NoteMethodDepthContext(reference, method, null,
                allocateReceiver: method.IsConstructor && !method.IsStatic);
        }
        if (!IsStripped(method.DeclaringType) || !_methods.Add(method)) return;
        _pending.Enqueue(method);
        MarkMethodMetadata(method);
        foreach (var property in method.DeclaringType.Properties)
            if (property.GetMethod == method || property.SetMethod == method || property.OtherMethods.Contains(method))
                MarkProperty(property, false, false);
        foreach (var @event in method.DeclaringType.Events)
            if (@event.AddMethod == method || @event.RemoveMethod == method || @event.InvokeMethod == method
                || @event.OtherMethods.Contains(method)) MarkEvent(@event, false);
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
        foreach (var overridden in method.Overrides) MarkMethod(overridden, false);
    }

    private void MarkMarshal(IMarshalInfoProvider provider)
    {
        if (provider.HasMarshalInfo && provider.MarshalInfo is CustomMarshalInfo custom
            && custom.ManagedType is not null && Resolve(custom.ManagedType) is { } type)
            KeepAll(type);
    }

    private void MarkField(FieldReference reference, bool recordDepth = true)
    {
        MarkType(reference.DeclaringType, recordDepth);
        MarkType(reference.FieldType, recordDepth);
        FieldDefinition? field;
        try { field = reference.Resolve(); }
        catch (AssemblyResolutionException) { return; }
        if (field is null || !IsStripped(field.DeclaringType) || !_fields.Add(field)) return;
        MarkAttributes(field);
        MarkMarshal(field);
    }

    private void MarkProperty(PropertyDefinition property, bool getter, bool setter, bool depthRoot = true)
    {
        if (_properties.Add(property))
        {
            MarkType(property.PropertyType);
            foreach (var parameter in property.Parameters) MarkType(parameter.ParameterType);
            MarkAttributes(property);
        }
        if (getter && property.GetMethod is not null) MarkMethod(property.GetMethod, depthRoot);
        if (setter && property.SetMethod is not null) MarkMethod(property.SetMethod, depthRoot);
    }

    private void MarkEvent(EventDefinition @event, bool depthRoot = true)
    {
        MarkType(@event.EventType);
        if (_events.Add(@event)) MarkAttributes(@event);
        if (@event.AddMethod is not null) MarkMethod(@event.AddMethod, depthRoot);
        if (@event.RemoveMethod is not null) MarkMethod(@event.RemoveMethod, depthRoot);
        if (@event.InvokeMethod is not null) MarkMethod(@event.InvokeMethod, depthRoot);
        foreach (var method in @event.OtherMethods) MarkMethod(method, depthRoot);
    }

    private void MarkAttributes(ICustomAttributeProvider provider, bool signatureOnly = false)
    {
        if (!provider.HasCustomAttributes) return;
        foreach (var attribute in provider.CustomAttributes)
        {
            MarkMethod(attribute.Constructor, false);
            try
            {
                bool registration = IsRegistrationAttribute(attribute, provider);
                bool stateMachine = signatureOnly && attribute.AttributeType.FullName is
                    "System.Runtime.CompilerServices.AsyncStateMachineAttribute"
                    or "System.Runtime.CompilerServices.IteratorStateMachineAttribute"
                    or "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute";
                foreach (var argument in attribute.ConstructorArguments)
                    if (registration && IsTypeArray(argument)) MarkType(argument.Type);
                    else if (stateMachine && argument.Value is TypeReference machine)
                    {
                        // Compiler state-machine metadata must not root a deferred body.
                        MarkType(argument.Type);
                        MarkSignatureType(machine);
                    }
                    else MarkArgument(argument);
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
                        foreach (var property in current.Properties.Where(p => p.Name == argument.Name)) MarkProperty(property, true, true, false);
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
        if (argument.Value is TypeReference type)
        {
            MarkType(type);
        }
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
        // Original-body retention also scans unused calls. Executable caller
        // contexts record their closed type depths independently.
        MarkType(method.ReturnType, false);
        foreach (var parameter in method.Parameters) MarkType(parameter.ParameterType, false);
        if (!method.HasBody) return;
        foreach (var variable in method.Body.Variables) MarkType(variable.VariableType, false);
        foreach (var handler in method.Body.ExceptionHandlers) MarkType(handler.CatchType, false);
        bool selectsReflectively = false, observesReflectively = false;
        foreach (var instruction in method.Body.Instructions)
        {
            switch (instruction.Operand)
            {
                case MethodReference target:
                    if (!_registryFactories.ContainsKey(instruction)) MarkMethod(target, false);
                    if (instruction.OpCode.Code == Code.Newobj && Resolve(target.DeclaringType) is { } owner)
                    {
                        _namedConstructorAllocations.Add(owner);
                        if (_bindsNamedConstructors) KeepNamedConstructorHierarchy(owner);
                    }
                    // A method group runs its target through the delegate, so it arms
                    // whatever a call to the target arms.
                    bool runs = instruction.OpCode.Code is Code.Call or Code.Callvirt or Code.Ldftn or Code.Ldvirtftn;
                    var api = runs ? ClassifyReflectionTarget(target) : null;
                    ReflectionOp? op = api?.Op;
                    if (api is { } admits && AdmitsReflectedBodies(admits))
                        selectsReflectively = true;
                    // A type token or an allocated object's type is already retained.
                    else if (api is { Recognized: true, Kind: not ReflectionOpKind.Transfer,
                        Op: not (ReflectionOp.GetTypeFromHandle or ReflectionOp.ObjectGetType) })
                        observesReflectively = true;
                    if (op is { } constructs && target.MetadataToken.TokenType == TokenType.MemberRef
                        && ReflectionRoutes.ConstructsFromRuntimeType(constructs))
                        ArmRuntimeTypeConstruction();
                    if (runs && target.Name == "Initialize" && target.Parameters.Count == 0
                        && target.DeclaringType.FullName == "System.Array")
                        ArmArrayInitialize();
                    // Every scanned body is user code, whose binding runs the method too.
                    if (runs && target.DeclaringType.FullName == "System.Delegate" && target.Name == "CreateDelegate"
                        && target.Parameters.Count >= 3 && target.Parameters[2].ParameterType.FullName == "System.String")
                        ArmNamedConstructorBinding();
                    if (op is { } invokes && !_runsReflectedMethods
                        && (ReflectionRoutes.RunsReflectedMethod(invokes) || ReflectionRoutes.BindsReflectedMethod(invokes)))
                        ArmReflectedMethodRuns();
                    if (op is { } reads && ReflectionRoutes.ReadsReflectedField(reads))
                        ArmReflectedFieldReads();
                    break;
                case FieldReference field: MarkField(field, false); break;
                case TypeReference type:
                    MarkType(type, false);
                    if (instruction.OpCode.Code == Code.Ldtoken)
                        NoteReflectedOwner(type);
                    break;
                case CallSite signature:
                    MarkType(signature.ReturnType, false);
                    foreach (var parameter in signature.Parameters) MarkType(parameter.ParameterType, false);
                    break;
            }
        }
        if (selectsReflectively || observesReflectively) NoteReflectedSelections(method);
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
            {
                var method = type.Methods[j];
                if (_methods.Contains(method)) continue;
                if (_signatureMethods.Contains(method))
                {
                    if (method.HasBody) StubBody(method);
                }
                else { type.Methods.RemoveAt(j); removedMethods++; }
            }
            for (int j = type.Properties.Count - 1; j >= 0; j--)
            {
                var property = type.Properties[j];
                if (!_properties.Contains(property)) { type.Properties.RemoveAt(j); continue; }
                if (property.GetMethod is not null && !KeepsMethod(property.GetMethod)) property.GetMethod = null;
                if (property.SetMethod is not null && !KeepsMethod(property.SetMethod)) property.SetMethod = null;
                for (int k = property.OtherMethods.Count - 1; k >= 0; k--)
                    if (!KeepsMethod(property.OtherMethods[k])) property.OtherMethods.RemoveAt(k);
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
    private readonly List<AssemblyDefinition> _assemblyOrder = new();
    private readonly Dictionary<string, TypeDefinition> _types = new(StringComparer.Ordinal);
    private bool _complete;

    internal void Add(AssemblyDefinition assembly)
    {
        if (_complete) throw new InvalidOperationException("assembly resolver is already complete");
        _assemblies.Add(assembly.Name.Name, assembly);
        _assemblyOrder.Add(assembly);
    }

    internal void Complete()
    {
        if (_complete) throw new InvalidOperationException("assembly resolver is already complete");
        foreach (var assembly in _assemblyOrder)
            AddTypes(assembly.MainModule.Types);
        _complete = true;
    }

    private void AddTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            _types.TryAdd(type.FullName, type);
            AddTypes(type.NestedTypes);
        }
    }

    internal TypeDefinition? FindDefinition(string fullName)
    {
        if (!_complete) throw new InvalidOperationException("assembly resolver is not complete");
        return _types.TryGetValue(fullName, out var type) ? type : null;
    }
    public AssemblyDefinition Resolve(AssemblyNameReference name) => _assemblies.TryGetValue(name.Name, out var assembly)
        ? assembly : throw new AssemblyResolutionException(name);
    public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);
    public void Dispose() { }
}

internal sealed class ClosedMetadataResolver : MetadataResolver
{
    private readonly ClosedResolver _assemblies;
    private readonly Action<Mono.Cecil.MemberReference> _preserveArrayKinds;
    internal ClosedMetadataResolver(ClosedResolver assemblies, Action<Mono.Cecil.MemberReference> preserveArrayKinds)
        : base(assemblies)
    {
        _assemblies = assemblies;
        _preserveArrayKinds = preserveArrayKinds;
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

    public override MethodDefinition Resolve(MethodReference method)
    {
        _preserveArrayKinds(method);
        var resolved = base.Resolve(method);
        if (resolved is null) return null!;
        method = method.GetElementMethod();
        _preserveArrayKinds(resolved);
        if (SameArrayKinds(resolved, method)) return resolved;
        for (var type = Resolve(method.DeclaringType); type is not null;
            type = type.BaseType is null ? null : Resolve(type.BaseType))
        {
            var candidates = new Mono.Collections.Generic.Collection<MethodDefinition>();
            foreach (var candidate in type.Methods)
            {
                if (candidate.Name != method.Name) continue;
                _preserveArrayKinds(candidate);
                if (SameArrayKinds(candidate, method)) candidates.Add(candidate);
            }
            // Cecil retains its generic/vararg matching; the filter adds original
            // SZ versus MD identity recursively through composed signatures.
            if (MetadataResolver.GetMethod(candidates, method) is { } match) return match;
        }
        return null!;
    }

    private static bool SameArrayKinds(MethodReference definition, MethodReference reference)
    {
        if (!SameArrayKinds(definition.ReturnType, reference.ReturnType)
            || definition.Parameters.Count > reference.Parameters.Count) return false;
        for (int i = 0; i < definition.Parameters.Count; i++)
        {
            var parameter = reference.Parameters[i].ParameterType;
            if (parameter is SentinelType sentinel) parameter = sentinel.ElementType;
            if (!SameArrayKinds(definition.Parameters[i].ParameterType, parameter)) return false;
        }
        return true;
    }

    private static bool SameArrayKinds(TypeReference a, TypeReference b)
    {
        if (a is ArrayType || b is ArrayType)
            return a is ArrayType aa && b is ArrayType ab
                && aa.IsVector == ab.IsVector && SameArrayKinds(aa.ElementType, ab.ElementType);
        if (a is GenericInstanceType ga && b is GenericInstanceType gb)
        {
            if (ga.GenericArguments.Count != gb.GenericArguments.Count) return false;
            for (int i = 0; i < ga.GenericArguments.Count; i++)
                if (!SameArrayKinds(ga.GenericArguments[i], gb.GenericArguments[i])) return false;
        }
        if (a is FunctionPointerType fa && b is FunctionPointerType fb)
        {
            if (!SameArrayKinds(fa.ReturnType, fb.ReturnType) || fa.Parameters.Count != fb.Parameters.Count) return false;
            for (int i = 0; i < fa.Parameters.Count; i++)
                if (!SameArrayKinds(fa.Parameters[i].ParameterType, fb.Parameters[i].ParameterType)) return false;
        }
        if (a is IModifierType ma && b is IModifierType mb
            && !SameArrayKinds(ma.ModifierType, mb.ModifierType)) return false;
        if (a is TypeSpecification sa && b is TypeSpecification sb)
            return SameArrayKinds(sa.ElementType, sb.ElementType);
        return true;
    }
}
