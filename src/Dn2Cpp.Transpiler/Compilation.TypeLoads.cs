using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private TypeNameSnapshot? _originalTypeNames;
    private bool _hasInvalidInitializers;
    internal readonly record struct TypeLoadError(Module Module, TypeDefinitionHandle Handle, bool IsEnum);

    private Dictionary<MethodInfo, TypeLoadError>? _methodTypeLoadErrors;
    private Dictionary<(Module, TypeDefinitionHandle), TypeLoadError?>? _declarationLoadErrors;
    internal bool HasMethodTypeLoadErrors => _methodTypeLoadErrors is not null;

    internal TypeNameSnapshot OriginalTypeNames()
    {
        if (_originalTypeNames is not null)
            return _originalTypeNames;
        var names = TryPreservationTypeNames() ?? TypeNameSnapshot.Capture(Modules.Select(module =>
            (module.AssemblyName, OriginalAssemblyPaths[module.Index])).ToArray());
        foreach (var module in Modules)
        {
            names.AddMissing(module.AssemblyName, OriginalAssemblyPaths[module.Index]);
            if (names.TryGet(module.AssemblyName, out var source) && source.HasInvalidInitializer)
                _hasInvalidInitializers = true;
        }
        return _originalTypeNames = names;
    }

    internal bool ContainsInvalidInitializers()
    {
        // The patch converter uses reachability without emitting native method bodies
        // or a type-name registry. It needs neither import guards nor a name snapshot.
        if (_hotUpdatePosture && !_hotUpdateBase)
            return false;
        _ = OriginalTypeNames();
        return _hasInvalidInitializers;
    }

    internal TypeLoadError? InvalidTypeLoad(MethodInfo method) =>
        _methodTypeLoadErrors is not null && _methodTypeLoadErrors.TryGetValue(method, out var error) ? error : null;

    private void NoteTypeLoad(MethodInfo method, TypeDesc? type)
    {
        if (type is null || !ContainsInvalidInitializers() || _methodTypeLoadErrors?.ContainsKey(method) == true)
            return;
        if (FindInvalidTypeLoad(type) is { } invalid)
        {
            (_methodTypeLoadErrors ??= new()).Add(method, invalid);
            NoteTypeIdentityClosure(type);
        }
    }

    // Parameter-dependent bases have no handle on an open definition. Their
    // concrete load failures still apply before its supplied arguments are read.
    internal (TypeLoadError Error, bool DeferredArgument)? InvalidOpenBase(string definitionName)
    {
        if (OpenGenericDefHandleByName(definitionName) is not { } owner)
            return null;
        var raw = owner.Module.Reader.GetTypeDefinition(owner.Handle).BaseType;
        if (raw.Kind != HandleKind.TypeSpecification)
            return null;
        var spec = owner.Module.Reader.GetTypeSpecification((TypeSpecificationHandle)raw);
        if (!spec.DecodeSignature(ParameterMentionProbe.Instance, null))
            return null;
        if (TypeDefinitionOf(owner.Module, raw) is { } parent
            && InvalidTypeDeclaration(parent.Module, parent.Handle) is { } invalid)
            return (invalid, false);
        var type = spec.DecodeSignature(new ReflectionDefinitionSignatureProvider(this), GenericContext.Empty);
        return FindInvalidTypeLoad(type) is { } argument ? (argument, true) : null;
    }

    // An approximate base loads its declaration and ancestor declarations. Its
    // type arguments are resolved when the loader later requests the exact base.
    private TypeLoadError? InvalidTypeDeclaration(Module module, TypeDefinitionHandle handle)
    {
        var key = (module, handle);
        var errors = _declarationLoadErrors ??= new();
        if (errors.TryGetValue(key, out var known))
            return known;
        errors[key] = null;
        var rawBase = module.Reader.GetTypeDefinition(handle).BaseType;
        var parent = rawBase.IsNil ? null : TypeDefinitionOf(module, rawBase);
        if (parent is { } inherited && InvalidTypeDeclaration(inherited.Module, inherited.Handle) is { } ancestor)
            return errors[key] = ancestor;
        if (OriginalTypeNames().TryGet(module.AssemblyName, out var source) && source.HasInvalidInitializer
            && source.Initializers.TryGetValue(TypeNameSnapshot.RawTypeDefKey(module.Reader, handle), out int bits)
            && (bits & TypeNameSnapshot.InvalidCctor) != 0)
        {
            bool isEnum = module.ClassMap.TryGetValue(handle, out var cls) && cls.IsEnum
                || parent is { } en && en.Module.AssemblyName == "System.Private.CoreLib"
                    && en.Module.ClassMap.TryGetValue(en.Handle, out var root) && root.FullName == "System.Enum";
            return errors[key] = new TypeLoadError(module, handle, isEnum);
        }
        return null;
    }

    private TypeLoadError? FindInvalidTypeLoad(TypeDesc type)
    {
        // After the declaration, import loads the supplied arguments in signature
        // order before constructing the exact base. C : Base<C> revisits C.
        var visited = new HashSet<ClassInfo>();
        var expandingBases = new HashSet<(Module, TypeDefinitionHandle)>();
        var provider = new ReflectionDefinitionSignatureProvider(this);
        TypeLoadError? Visit(TypeDesc current)
        {
            if (current.Element is { } element)
                return Visit(element);
            ClassInfo? cls = current.Class;
            if (cls is not null && !visited.Add(cls))
                return null;
            var (module, handle) = TemplateOrClassDef(current);
            if (module is null || handle.IsNil)
                return null;
            if (InvalidTypeDeclaration(module, handle) is { } declaration)
                return declaration;
            if (cls is not null)
                foreach (var argument in cls.Context.TypeArgs)
                    if (Visit(argument) is { } error)
                        return error;
            var rawBase = module.Reader.GetTypeDefinition(handle).BaseType;
            bool expandBase = !rawBase.IsNil && expandingBases.Add((module, handle));
            if (expandBase)
            {
                var context = cls?.Context ?? GenericContext.Empty;
                var parent = rawBase.Kind == HandleKind.TypeSpecification
                    ? module.Reader.GetTypeSpecification((TypeSpecificationHandle)rawBase).DecodeSignature(provider, context)
                    : ResolveTypeTokenForScan(module, rawBase, context);
                if (parent is not null && Visit(parent) is { } baseError)
                    return baseError;
                expandingBases.Remove((module, handle));
            }
            return null;
        }
        return Visit(type);
    }
}
