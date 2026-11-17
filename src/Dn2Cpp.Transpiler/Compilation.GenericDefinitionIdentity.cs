using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<(Module Module, TypeDefinitionHandle Handle), string> _genericDefinitionKeys = new();
    private readonly Dictionary<string, (Module Module, TypeDefinitionHandle Handle)> _scopedGenericDefinitions = new(StringComparer.Ordinal);

    // Names remain display data; only colliding definitions need a scoped internal key.
    internal string GenericDefinitionKey(Module module, TypeDefinitionHandle handle)
    {
        if (_genericDefinitionKeys.TryGetValue((module, handle), out var cached))
            return cached;
        string name = RawSignatureProvider.TypeDefinitionName(module.Reader, handle);
        bool collision = Modules.Any(other => other != module && RawMetadataDefinition(other, name) is not null);
        string key = collision ? "m" + module.Index + "\0" + name : name;
        _genericDefinitionKeys[(module, handle)] = key;
        if (collision)
            _scopedGenericDefinitions[key] = (module, handle);
        return key;
    }

    internal static string GenericDefinitionName(string key)
    {
        int separator = key.IndexOf('\0');
        return separator < 0 ? key : key.Substring(separator + 1);
    }

    internal string? OpenGenericDefinitionKey(TypeDesc type)
    {
        if (OpenGenericDefBacktickNameOf(type) is not { } name)
            return null;
        if (type.Kind == TypeKind.Template)
            return GenericDefinitionKey(type.TemplateModule!, type.TemplateHandle);
        if (type.Kind == TypeKind.Class)
            return GenericDefinitionKey(type.Class!.Module, type.Class.Handle);
        if (type.ExternalModule is { } from)
        {
            var (module, handle) = TemplateOrClassDef(ResolveTypeRef(from, type.ExternalHandle));
            return module is null ? null : GenericDefinitionKey(module, handle);
        }
        return OpenGenericDefHandleByName(name) is { } owner
            ? GenericDefinitionKey(owner.Module, owner.Handle) : name;
    }
}
