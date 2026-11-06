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
    private readonly HashSet<MethodDefinition> _signatureMethods = new();
    private readonly HashSet<FieldDefinition> _fields = new();
    private readonly HashSet<PropertyDefinition> _properties = new();
    private readonly HashSet<EventDefinition> _events = new();
    private readonly HashSet<TypeDefinition> _conditional = new();
    private readonly HashSet<TypeDefinition> _interfaceHierarchies = new();
    private readonly HashSet<TypeDefinition> _reflectionDataTypes = new();
    private readonly HashSet<GenericParameter> _genericParameters = new();
    private readonly Dictionary<ModuleDefinition, ReflectionSummarizer> _summarizers = new();
    private readonly Dictionary<ModuleDefinition, DietAssembly> _byModule = new();
    private readonly HashSet<MethodDefinition> _cutMethods = new();
    private readonly Dictionary<Mono.Cecil.MemberReference, IMemberDefinition?> _resolved = new();
    private readonly HashSet<ModuleDefinition> _traversedCopies = new();
    private bool _cutsValidated = true;
    private string _fallback = "none";
    private string? _originalPreservationText;

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
        ValidateRoots();
        ConfigureBackendPolicy();
        InitializeBudget();
        if (_request.CopyAll || !_cutsValidated)
        {
            _fallback = _request.CopyAll ? "hot-update" : "generic-cut";
            foreach (var assembly in _assemblies) assembly.Copy = true;
        }
        else
        {
            try
            {
                InitializeGraph();
                InitializeAttributeInventory();
                FinishModelActivationInventory();
                Seed();
                Drain();
                CloseRegistries();
                Spend();
                _frozen = true;
            }
            catch (DietBudgetExceeded)
            {
                _fallback = "budget";
                foreach (var assembly in _assemblies) assembly.Copy = true;
                _constructorRegistriesRewritten = false;
                _frozen = true;
            }
        }
        _frozen = true;
        CheckModelActivationFreeze();
        _originalPreservationText = EffectivePreservation();
        var plan = BuildPlan();
        FailPoint("plan");
        ValidatePlan(plan);
        ApplyPlan(plan);
        FailPoint("apply");
        WriteOutputs(resultPath);
        foreach (var diagnostic in _registryDiagnostics.OrderBy(d => d.Scope, StringComparer.Ordinal)
                     .ThenBy(d => d.Reason, StringComparer.Ordinal))
            Console.WriteLine("ILDiet: constructor registry " + diagnostic.Scope + " " + diagnostic.Reason);
        Console.WriteLine($"ILDiet: removed {plan.RemovedTypes} types and {plan.RemovedMethods} methods -> {Path.GetFullPath(_request.Output)}");
        Console.WriteLine("ILDiet: counters " + string.Join(" ", GraphCounters().Select(p => p.Key + "=" + p.Value)));
    }

    private static void FailPoint(string point)
    {
        if (EnvKnobs.Raw(EnvKnobs.ILDietFailPoint) == point)
            throw new InvalidOperationException("ILDiet injected failure: " + point);
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
            var item = new DietAssembly
            {
                Index = _assemblies.Count, Path = path, Assembly = definition, PE = pe,
                Copy = copy,
            };
            _assemblies.Add(item);
            _byModule.Add(definition.MainModule, item);
            _resolver.Add(definition);
        }
        foreach (string name in _request.SkipAssemblies)
        {
            var assembly = _assemblies.SingleOrDefault(a => name.Length != 0 && a.Assembly.Name.Name == name)
                ?? throw new NotSupportedException("ILDiet skip assembly is not loaded: " + name);
            assembly.Copy = true;
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
            if (type.HasGenericParameters && (policy.Kind != PreservationReader.PreserveKind.None
                || policy.Fields.Count != 0 || policy.Methods.Count != 0 || policy.Properties.Count != 0 || policy.Events.Count != 0))
                _modelHasGenericPolicy = true;
        }
        foreach (var pair in _policyTypes) _exportPolicyTypes.Add(pair.Key, pair.Value);
        foreach (int index in reader.PreservedAssemblies)
        {
            _assemblies[index].PreserveAll = true;
            _assemblies[index].Copy = true;
        }
        foreach (var assembly in _assemblies)
            if (!assembly.Copy && (assembly.Assembly.MainModule.Attributes & ModuleAttributes.ILOnly) == 0)
                throw new NotSupportedException("mixed-mode assemblies are not supported: " + assembly.Path);
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

    private void ValidateRoots()
    {
        foreach (var root in _request.Roots)
            if (!RootTypes(root.Assembly, root.Type).Any())
                throw new NotSupportedException("preservation root was not found: " + root.Type);
        foreach (string root in _request.ReflectionRoots)
            if (!RootTypes("", root).Any())
                throw new NotSupportedException("reflection root was not found: " + root);
        foreach (var root in _request.TypeRoots) FindPolicyType(root.Assembly, root.Type);
        foreach (var root in _request.MethodRoots)
            if (!FindPolicyType(root.Assembly, root.Type).Methods.Any(m => m.Name == root.Method))
                throw new NotSupportedException("method root was not found: " + root.Type + "::" + root.Method);
    }

    private IEnumerable<TypeDefinition> RootTypes(string assembly, string type) => _assemblies
        .Where(a => assembly.Length == 0 || a.Assembly.Name.Name == assembly)
        .SelectMany(a => AllTypes(a.Assembly.MainModule.Types))
        .Where(t => RootMatches(t, type, assembly.Length != 0));

    private void Seed()
    {
        foreach (var pair in _policyTypes.OrderBy(p => _byModule[p.Key.Module].Index)
                     .ThenBy(p => p.Key.MetadataToken.ToInt32())) ApplyPolicy(pair.Key, pair.Value, false);
        foreach (var root in _request.Roots)
            foreach (var type in RootTypes(root.Assembly, root.Type)) KeepAll(type);
        foreach (string root in _request.ReflectionRoots)
            foreach (var type in RootTypes("", root))
            {
                Require(type, TypeState.Metadata | TypeState.Members | TypeState.Explicit);
                KeepReflectionAncestors(type, new HashSet<TypeDefinition>());
            }
        foreach (var root in _request.TypeRoots)
            Require(FindPolicyType(root.Assembly, root.Type), TypeState.Runtime | TypeState.Explicit);
        foreach (var root in _request.MethodRoots)
            foreach (var method in FindPolicyType(root.Assembly, root.Type).Methods.Where(m => m.Name == root.Method))
            {
                Require(method.DeclaringType, TypeState.Explicit);
                MarkMethod(method);
            }
        foreach (var method in _cutMethods.OrderBy(m => _byModule[m.Module].Index).ThenBy(m => m.MetadataToken.ToInt32()))
            KeepCutMethod(method);
        var app = _assemblies[0].Assembly.MainModule;
        if (app.EntryPoint is not null) MarkMethod(app.EntryPoint);
        var stripped = _assemblies.Where(a => !a.Copy).Select(a => a.Assembly.Name.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in _assemblies)
            if (assembly.Copy && assembly.Assembly.MainModule.AssemblyReferences.Any(r => stripped.Contains(r.Name)))
                _traversedCopies.Add(assembly.Assembly.MainModule);
        foreach (var assembly in _assemblies)
        {
            bool traversedCopy = assembly.Copy && assembly.Assembly.MainModule.AssemblyReferences.Any(r => stripped.Contains(r.Name));
            if (assembly.Copy && !traversedCopy)
            {
                if (!PreservationReader.IsFrameworkAssemblyName(assembly.Assembly.Name.Name))
                    foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
                    {
                        if (type.Name == "<Module>") MarkType(type);
                        foreach (var method in type.Methods)
                            if (HasAttribute(method, "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"))
                                MarkMethod(method);
                    }
                continue;
            }
            MarkAttributes(assembly.Assembly);
            MarkSecurity(assembly.Assembly);
            MarkAttributes(assembly.Assembly.MainModule);
            foreach (var type in AllTypes(assembly.Assembly.MainModule.Types).OrderBy(t => t.MetadataToken.ToInt32()))
            {
                if (traversedCopy) Require(type, TypeState.Metadata);
                if (type.Name == "<Module>") MarkType(type);
                foreach (var method in type.Methods)
                    if (traversedCopy
                        || ((assembly.Index == 0 && !_suppressDefaultSeeds.Contains(type) || type.Name == "<Module>")
                            && method.IsConstructor && method.IsStatic)
                        || HasAttribute(method, "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute")
                        || HasAttribute(method, "Dn2Cpp.Runtime.NativeImplementationAttribute")
                        || assembly.Index == 0 && app.EntryPoint is null && PublicType(type)
                            && !_suppressDefaultSeeds.Contains(type) && (method.IsPublic || method.IsConstructor))
                        MarkMethod(method);
            }
        }
        foreach (var assembly in _assemblies)
            foreach (var exported in assembly.Assembly.MainModule.ExportedTypes)
            {
                try { MarkSignatureType(exported.Resolve()); }
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
            Require(parent, TypeState.Members);
            KeepReflectionAncestors(parent, seen);
        }
        foreach (var implementation in type.Interfaces)
            if (Resolve(implementation.InterfaceType) is { } contract)
            {
                Require(contract, TypeState.Members);
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

    private IMemberDefinition? ResolveMember(Mono.Cecil.MemberReference reference)
    {
        if (reference is ProjectedTypeReference) return null;
        if (_resolved.TryGetValue(reference, out var cached)) return cached;
        PreserveMemberArrayKinds(reference);
        IMemberDefinition? definition = null;
        try
        {
            definition = reference switch
            {
                TypeReference type when type is not GenericParameter => type.Resolve(),
                MethodReference method => method.Resolve(),
                FieldReference field => field.Resolve(),
                _ => null,
            };
        }
        catch (AssemblyResolutionException) { }
        _resolved.Add(reference, definition);
        return definition;
    }

    private TypeDefinition? Resolve(TypeReference reference) => ResolveMember(reference) as TypeDefinition;

    private void MarkType(TypeReference? reference) => RequireType(reference, TypeState.Runtime);

    private void ApplyConditionalPolicy(TypeDefinition type)
    {
        if (_policyTypes.TryGetValue(type, out var policy) && _conditional.Add(type))
        {
            KeepOriginalModelSources(type);
            ApplyPolicy(type, policy, true);
        }
    }

    private static bool IsDelegate(TypeDefinition type) => CoreIntrinsicTypeMaps.IsDelegateBaseType(type.BaseType?.FullName);

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

    // The shared classification of a Cecil call target, named as RawSignatureProvider names it.
    private ReflectionApi? ClassifyReflectionTarget(MethodReference target)
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
        if (owner.FullName == "System.RuntimeType" && target.Name == "MakeGenericType"
            && target.MetadataToken.RID != 0 && _byModule.ContainsKey(target.Module))
            return GetReflectionSummarizer(target.Module).Call(target.MetadataToken.ToInt32())?.Api;
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

    private void MarkMarshal(IMarshalInfoProvider provider)
    {
        if (provider.HasMarshalInfo && provider.MarshalInfo is CustomMarshalInfo custom
            && custom.ManagedType is not null && Resolve(custom.ManagedType) is { } type)
            KeepAll(type);
    }

    private void MarkField(FieldReference reference)
    {
        MarkType(reference.DeclaringType);
        MarkSignatureType(reference.FieldType);
        var field = ResolveMember(reference) as FieldDefinition;
        if (_passiveAttributeMetadataDepth == 0 && field is not null) ReplayPassiveBindings(field);
        if (field is null || !IsStripped(field.DeclaringType) || !_fields.Add(field)) return;
        MarkAttributes(field);
        MarkMarshal(field);
    }

    private void MarkProperty(PropertyDefinition property, bool getter, bool setter)
    {
        KeepPropertyRow(property, allowAccessorless: !getter && !setter);
        if (getter) MarkReflectedAccessor(property.GetMethod);
        if (setter) MarkReflectedAccessor(property.SetMethod);
    }

    private void MarkEvent(EventDefinition @event)
    {
        if (_passiveAttributeMetadataDepth == 0) ReplayPassiveBindings(@event);
        MarkSignatureType(@event.EventType);
        if (_events.Add(@event) && !UsesReachedCopyTraversal(@event.DeclaringType)) MarkAttributes(@event);
        MarkReflectedAccessor(@event.AddMethod);
        MarkReflectedAccessor(@event.RemoveMethod);
        MarkReflectedAccessor(@event.InvokeMethod);
        foreach (var method in @event.OtherMethods) MarkReflectedAccessor(method);
    }

    private void MarkReflectedAccessor(MethodDefinition? method)
    {
        if (method is null) return;
        MarkMethod(method);
        NoteReflectionInvokeBoxes(method);
    }

    private void MarkAttributes(ICustomAttributeProvider provider, bool signatureOnly = false)
    {
        if (!provider.HasCustomAttributes) return;
        if (_passiveAttributeMetadataDepth != 0)
        {
            KeepPassiveAttributeProvider(provider);
            return;
        }
        foreach (var attribute in provider.CustomAttributes) MarkAttribute(attribute, provider, signatureOnly);
    }

    private void MarkAttribute(CustomAttribute attribute, ICustomAttributeProvider provider, bool signatureOnly)
    {
        if (UsesSelectedAttributeRetention(attribute, provider)) return;
        if (DeferFrameworkAttribute(attribute, provider)) return;
        MarkMethod(attribute.Constructor);
        try
        {
            bool registration = IsRegistrationAttribute(attribute, provider);
            bool stateMachine = signatureOnly && IsStateMachineAttribute(attribute);
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
                    foreach (var property in current.Properties.Where(p => p.Name == argument.Name)) MarkProperty(property, true, true);
            }
        }
        catch (AssemblyResolutionException)
        {
            // An external attribute blob stays opaque when its defining DLL is not an input.
        }
    }

    private static bool IsStateMachineAttribute(CustomAttribute attribute) => attribute.AttributeType.FullName is
        "System.Runtime.CompilerServices.AsyncStateMachineAttribute"
        or "System.Runtime.CompilerServices.IteratorStateMachineAttribute"
        or "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute";

    private void ApplyExecutableAttributeDelta(MethodDefinition method)
    {
        if (!method.HasCustomAttributes) return;
        foreach (var attribute in method.CustomAttributes)
        {
            Spend();
            if (!IsStateMachineAttribute(attribute)) continue;
            try
            {
                foreach (var argument in attribute.ConstructorArguments)
                    if (argument.Value is TypeReference machine) MarkType(machine);
            }
            catch (AssemblyResolutionException)
            {
                // The same external attribute blobs stay opaque in both phases.
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

    private void ApplyPolicy(TypeDefinition type, PreservePolicy policy, bool conditional)
    {
        PreserveKind kind = conditional ? policy.ConditionalKind : policy.Kind;
        var fields = conditional ? policy.ConditionalFields : policy.Fields;
        var methods = conditional ? policy.ConditionalMethods : policy.Methods;
        var properties = conditional ? policy.ConditionalProperties : policy.Properties;
        var events = conditional ? policy.ConditionalEvents : policy.Events;
        if (kind != PreserveKind.None || fields.Count != 0 || methods.Count != 0 || properties.Count != 0 || events.Count != 0)
        {
            RequestModelOwnerPromotion(type);
            Require(type, TypeState.Runtime | TypeState.Explicit);
        }
        foreach (var method in type.Methods)
            if ((kind & PreserveKind.Methods) != 0 || IsDelegate(type) && kind != PreserveKind.None
                || (kind & PreserveKind.DefaultConstructor) != 0 && method.IsConstructor && !method.IsStatic && method.Parameters.Count == 0
                || methods.Contains(MetadataTokens.MethodDefinitionHandle((int)method.MetadataToken.RID)))
            {
                MarkPreservedMethod(method);
            }
        foreach (var field in type.Fields)
            if ((kind & PreserveKind.Fields) != 0 || fields.Contains(MetadataTokens.FieldDefinitionHandle((int)field.MetadataToken.RID)))
            {
                MarkPreservedField(field);
            }
        foreach (var property in type.Properties)
            if (properties.Contains(MetadataTokens.PropertyDefinitionHandle((int)property.MetadataToken.RID)))
            {
                NotePreservedReference(property.PropertyType);
                foreach (var parameter in property.Parameters) NotePreservedReference(parameter.ParameterType);
                if (!UsesReachedCopyTraversal(type)) MarkProperty(property, false, false);
            }
        foreach (var @event in type.Events)
            if (events.Contains(MetadataTokens.EventDefinitionHandle((int)@event.MetadataToken.RID)))
            {
                NotePreservedReference(@event.EventType);
                if (!UsesReachedCopyTraversal(type)) MarkEvent(@event);
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
    internal bool PreserveAll;
    internal bool RegistryRewritten;
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
        if (SameSignatureIdentity(resolved, method)) return resolved;
        for (var type = Resolve(method.DeclaringType); type is not null;
            type = type.BaseType is null ? null : Resolve(type.BaseType))
        {
            var candidates = new Mono.Collections.Generic.Collection<MethodDefinition>();
            foreach (var candidate in type.Methods)
            {
                if (candidate.Name != method.Name) continue;
                _preserveArrayKinds(candidate);
                if (SameSignatureIdentity(candidate, method)) candidates.Add(candidate);
            }
            // Cecil retains its generic/vararg matching; the filter adds original
            // array kinds and defining scopes through composed signatures.
            if (MetadataResolver.GetMethod(candidates, method) is { } match) return match;
        }
        return null!;
    }

    private bool SameSignatureIdentity(MethodReference definition, MethodReference reference)
    {
        if (!SameSignatureIdentity(definition.ReturnType, reference.ReturnType)
            || definition.Parameters.Count > reference.Parameters.Count) return false;
        for (int i = 0; i < definition.Parameters.Count; i++)
        {
            var parameter = reference.Parameters[i].ParameterType;
            if (parameter is SentinelType sentinel) parameter = sentinel.ElementType;
            if (!SameSignatureIdentity(definition.Parameters[i].ParameterType, parameter)) return false;
        }
        return true;
    }

    internal bool SameSignatureIdentity(TypeReference a, TypeReference b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is ArrayType || b is ArrayType)
            return a is ArrayType aa && b is ArrayType ab
                && aa.IsVector == ab.IsVector && SameSignatureIdentity(aa.ElementType, ab.ElementType);
        if (a is GenericInstanceType ga && b is GenericInstanceType gb)
        {
            if (ga.GenericArguments.Count != gb.GenericArguments.Count) return false;
            for (int i = 0; i < ga.GenericArguments.Count; i++)
                if (!SameSignatureIdentity(ga.GenericArguments[i], gb.GenericArguments[i])) return false;
        }
        if (a is FunctionPointerType || b is FunctionPointerType)
        {
            if (a is not FunctionPointerType fa || b is not FunctionPointerType fb) return false;
            if (!SameSignatureIdentity(fa.ReturnType, fb.ReturnType) || fa.Parameters.Count != fb.Parameters.Count) return false;
            for (int i = 0; i < fa.Parameters.Count; i++)
                if (!SameSignatureIdentity(fa.Parameters[i].ParameterType, fb.Parameters[i].ParameterType)) return false;
            return true;
        }
        if (a is IModifierType ma && b is IModifierType mb
            && !SameSignatureIdentity(ma.ModifierType, mb.ModifierType)) return false;
        if (a is TypeSpecification sa && b is TypeSpecification sb)
            return SameSignatureIdentity(sa.ElementType, sb.ElementType);
        // Cecil checks generic positions, primitive identities and wrapper shapes.
        if (a is GenericParameter || b is GenericParameter || a is TypeSpecification || b is TypeSpecification
            || a.MetadataType is not (MetadataType.Class or MetadataType.ValueType)
            || b.MetadataType is not (MetadataType.Class or MetadataType.ValueType)) return true;
        if (a.FullName != b.FullName) return false;
        var definitionA = SignatureDefinition(a);
        var definitionB = SignatureDefinition(b);
        if (definitionA is not null || definitionB is not null)
            return definitionA is not null && definitionB is not null
                && definitionA.Module == definitionB.Module && definitionA.MetadataToken == definitionB.MetadataToken;
        if (a.Scope is AssemblyNameReference assemblyA && b.Scope is AssemblyNameReference assemblyB)
            return assemblyA.FullName == assemblyB.FullName;
        if (a.Scope is Mono.Cecil.ModuleReference moduleA && b.Scope is Mono.Cecil.ModuleReference moduleB)
            return a.Module.Assembly == b.Module.Assembly && moduleA.Name == moduleB.Name;
        return ReferenceEquals(a.Scope, b.Scope) && (a.Scope is not null || a.Module == b.Module);
    }

    private TypeDefinition? SignatureDefinition(TypeReference reference)
    {
        try { return Resolve(reference); }
        catch (AssemblyResolutionException) { return null; }
    }
}
