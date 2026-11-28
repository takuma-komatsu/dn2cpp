using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private int _methodConstraintDeclarationCount;

    private sealed partial class TypeMetadataEmitter
    {
        private readonly HashSet<(Module, TypeDefinitionHandle)> _methodConstraintTypes = new();
        private bool _dependentMethodConstraint, _methodConstructorConstraint;
        private readonly Dictionary<string, (bool Constructor, ConstraintSignature? Base, ConstraintSignature[] Interfaces)>
            _methodConstraintDeclarations = new(StringComparer.Ordinal);

        private void NoteMethodParameterConstraints(Module module, GenericParameterHandle parameter)
        {
            var formal = module.Reader.GetGenericParameter(parameter);
            bool valueType = (formal.Attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0;
            if (_c.MakesGenericMethods && !valueType && (formal.Attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0)
                _methodConstructorConstraint = true;
            var provider = new ConstraintProvider(_c);
            foreach (var row in formal.GetConstraints())
            {
                var handle = module.Reader.GetGenericParameterConstraint(row).Type;
                var constraint = provider.Decode(module, handle);
                NoteConstraintSignature(constraint);
                if (!_c.MakesGenericMethods) continue;
                if (constraint.Kind is 5 or 9)
                    _dependentMethodConstraint = true;
                else if (!(valueType && constraint.Primitive?.Class is { Module.AssemblyName: "System.Private.CoreLib", FullName: "System.ValueType" })
                    && _c.TypeDefinitionOf(module, handle) is { } owner)
                    _methodConstraintTypes.Add(owner);
            }
        }

        // The constraint can be a type or method parameter. In that case any
        // declared ancestor can be the requirement after substitution.
        private void NoteMethodConstraintDeclarations()
        {
            if (!_methodConstructorConstraint && !_dependentMethodConstraint && _methodConstraintTypes.Count == 0)
                return;
            var provider = new ConstraintProvider(_c);
            var ancestors = new Dictionary<(Module, TypeDefinitionHandle), bool>();
            var seen = new HashSet<(Module, TypeDefinitionHandle)>();

            bool MayMatch(Module module, EntityHandle handle)
            {
                if (handle.IsNil || !_dependentMethodConstraint && _methodConstraintTypes.Count == 0
                    || _c.TypeDefinitionOf(module, handle) is not { } owner)
                    return false;
                if (_dependentMethodConstraint || _methodConstraintTypes.Contains(owner))
                    return true;
                if (ancestors.TryGetValue(owner, out bool found))
                    return found;
                ancestors[owner] = false;
                var reader = owner.Module.Reader;
                var type = reader.GetTypeDefinition(owner.Handle);
                bool matches = MayMatch(owner.Module, type.BaseType) || type.GetInterfaceImplementations()
                    .Any(i => MayMatch(owner.Module, reader.GetInterfaceImplementation(i).Interface));
                ancestors[owner] = matches;
                return matches;
            }

            void NoteIdentity(ConstraintSignature signature)
            {
                if (signature.Primitive is { } type)
                {
                    if (_c.OpenGenericDefinitionKey(type) is { } key && type.TemplateModule is { } module)
                        _c.NoteTypeofOpenGenericDef(key, Compilation.ResolveOpenGenericDefKind(module, type.TemplateHandle),
                            Compilation.GenericParamNames(module, type.TemplateHandle));
                    else
                        _e.NoteQueryIdentity(type);
                }
                foreach (var child in signature.Children)
                    NoteIdentity(child);
            }

            string SignatureKey(ConstraintSignature signature)
            {
                var type = signature.Primitive;
                string? identity = type is null ? null
                    : _c.OpenGenericDefinitionKey(type) ?? MethodCompiler.TypeInfoExprOf(type);
                return signature.Kind + "/" + signature.Value + "/" + identity + "["
                    + string.Join(",", signature.Children.Select(SignatureKey)) + "]";
            }

            void Note(Module module, TypeDefinitionHandle handle, string identity)
            {
                if (handle.IsNil || !seen.Add((module, handle)))
                    return;
                var reader = module.Reader;
                var definition = reader.GetTypeDefinition(handle);
                bool generic = definition.GetGenericParameters().Count != 0;
                if (generic)
                {
                    string key = _c.GenericDefinitionKey(module, handle);
                    identity = "&gendef_" + CppNaming.GenericDefinitionStem(key);
                    if (_constraintDefinitions.ContainsKey("generic:" + key))
                        return;
                }
                else if (!identity.StartsWith("&dn2cpp_", StringComparison.Ordinal)
                    && _constraintDefinitions.ContainsKey("type:" + identity[1..]))
                    return;

                var visited = new HashSet<string>(StringComparer.Ordinal);
                ConstraintSignature? baseType = null;
                var interfaces = new List<ConstraintSignature>();
                void Visit(Module source, EntityHandle ancestor, ConstraintSignature[]? arguments)
                {
                    if (!MayMatch(source, ancestor) || _c.TypeDefinitionOf(source, ancestor) is not { } owner)
                        return;
                    var signature = provider.Decode(source, ancestor, arguments);
                    if (!visited.Add(SignatureKey(signature)))
                        return;
                    var parentReader = owner.Module.Reader;
                    var parent = parentReader.GetTypeDefinition(owner.Handle);
                    if (_dependentMethodConstraint || _methodConstraintTypes.Contains(owner))
                    {
                        NoteIdentity(signature);
                        if ((parent.Attributes & TypeAttributes.Interface) != 0)
                            interfaces.Add(signature);
                        else
                            baseType ??= signature;
                    }
                    Visit(owner.Module, parent.BaseType, signature.Children);
                    foreach (var row in parent.GetInterfaceImplementations())
                        Visit(owner.Module, parentReader.GetInterfaceImplementation(row).Interface, signature.Children);
                }
                Visit(module, definition.BaseType, null);
                foreach (var row in definition.GetInterfaceImplementations())
                    Visit(module, reader.GetInterfaceImplementation(row).Interface, null);
                bool constructor = _methodConstructorConstraint && _c.HasPublicBareDefaultCtor(module, handle);
                if (constructor || baseType is not null || interfaces.Count != 0)
                    _methodConstraintDeclarations.TryAdd(identity, (constructor, baseType, interfaces.ToArray()));
            }

            // Each concrete enum carries the value-type flag and inherits its
            // interfaces from System.Enum, including enums nested in generic types.
            var owners = _e.TopoOrder().Where(c => !c.IsEnum).Concat(ReferencedIntrinsicTypeInfos()).ToList();
            foreach (var cls in owners)
                if (!_e.IsCanonicalWorld(cls) && !CoreIntrinsics.RuntimeOwnsTypeInfo(cls)
                    && MethodCompiler.TypeInfoExprOf(TypeDesc.MakeClass(cls)) is { } identity)
                    Note(cls.Module, cls.Handle, identity);
            foreach (var (identity, cls) in _e.UnboundRuntimeHandles())
                Note(cls.Module, cls.Handle, identity);
            foreach (var module in _c.Modules)
                if (module.AssemblyName == "System.Private.CoreLib")
                    foreach (var cls in module.ClassMap.Values)
                        if (cls.FullName is "System.Object" or "System.Enum")
                            Note(module, cls.Handle, "&" + cls.CppTypeInfoName);
        }

        private void EmitMethodConstraintDeclarations()
        {
            if (_methodConstraintDeclarations.Count == 0)
                return;
            var sets = new List<string>();
            const string constructorOnly = "method_constraint_public_ctor";
            bool hasConstructorOnly = false;
            foreach (var pair in _methodConstraintDeclarations.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var declaration = pair.Value;
                string symbol = constructorOnly;
                if (declaration.Base is not null || declaration.Interfaces.Length != 0)
                {
                    symbol = "method_constraint_type_" + sets.Count;
                    string baseType = declaration.Base is { } b ? RenderConstraintSignature(b) : "nullptr";
                    string interfaces = RenderConstraintVector(declaration.Interfaces);
                    _sb.AppendLine($"static const Dn2CppGenericTypeMetadata {symbol} = {{ nullptr, 0, {baseType}, {interfaces}, {declaration.Interfaces.Length}, false, {(declaration.Constructor ? "true" : "false")} }};");
                }
                else if (!hasConstructorOnly)
                {
                    _sb.AppendLine($"static const Dn2CppGenericTypeMetadata {constructorOnly} = {{ nullptr, 0, nullptr, nullptr, 0, false, true }};");
                    hasConstructorOnly = true;
                }
                sets.Add($"{{ {pair.Key}, &{symbol} }}");
            }
            _e._metadataHeader.AppendLine("extern const Dn2CppConstraintDeclaration method_constraint_declarations[];");
            _sb.AppendLine($"extern const Dn2CppConstraintDeclaration method_constraint_declarations[] = {{ {string.Join(", ", sets)} }};");
            _e._methodConstraintDeclarationCount = sets.Count;
        }
    }
}
