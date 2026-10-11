using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private readonly Dictionary<string, string> _genericMetadata = new(StringComparer.Ordinal);
    private readonly HashSet<string> _strippedGenericMetadata = new(StringComparer.Ordinal);

    private sealed partial class TypeMetadataEmitter
    {
        private sealed class ConstraintSignature
        {
            internal int Kind, Value;
            internal string? Name;
            internal TypeDesc? Primitive;
            internal ConstraintSignature[] Children = Array.Empty<ConstraintSignature>();
        }

        // Decode symbolic trees without creating ClassInfo specializations or layouts.
        private sealed class ConstraintProvider : ISignatureTypeProvider<ConstraintSignature, object?>
        {
            private readonly Compilation _c;
            internal ConstraintProvider(Compilation compilation) => _c = compilation;
            public ConstraintSignature GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
                new() { Kind = 8, Name = RawSignatureProvider.TypeDefinitionName(reader, handle),
                    Primitive = _c.GetTypeDescForDefinition(_c.ModuleOf(reader), handle) };
            public ConstraintSignature GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                var resolved = _c.ResolveTypeRef(_c.ModuleOf(reader), handle);
                // An unloaded scope must not bind to another assembly's same-named type.
                return new() { Kind = 8, Name = resolved is not null ? RawSignatureProvider.TypeReferenceName(reader, handle) : null,
                    Primitive = resolved };
            }
            public ConstraintSignature GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle, byte rawTypeKind) =>
                reader.GetTypeSpecification(handle).DecodeSignature(this, context);
            public ConstraintSignature GetPrimitiveType(PrimitiveTypeCode code) => new() { Primitive = TypeDesc.MakePrimitive(code) };
            public ConstraintSignature GetGenericTypeParameter(object? context, int index) => new() { Kind = 5, Value = index };
            public ConstraintSignature GetGenericMethodParameter(object? context, int index) => new() { Kind = 9, Value = index };
            public ConstraintSignature GetGenericInstantiation(ConstraintSignature genericType, ImmutableArray<ConstraintSignature> arguments) =>
                new() { Kind = 4, Name = genericType.Name, Primitive = genericType.Primitive, Children = arguments.ToArray() };
            private static ConstraintSignature Wrap(int kind, ConstraintSignature element, int value = 0) =>
                new() { Kind = kind, Value = value, Children = new[] { element } };
            public ConstraintSignature GetArrayType(ConstraintSignature element, ArrayShape shape) => Wrap(7, element, shape.Rank);
            public ConstraintSignature GetSZArrayType(ConstraintSignature element) => Wrap(6, element);
            public ConstraintSignature GetByReferenceType(ConstraintSignature element) => Wrap(2, element);
            public ConstraintSignature GetPointerType(ConstraintSignature element) => Wrap(1, element);
            public ConstraintSignature GetFunctionPointerType(MethodSignature<ConstraintSignature> signature) =>
                new() { Kind = 3, Value = (int)signature.Header.CallingConvention,
                    Children = new[] { signature.ReturnType }.Concat(signature.ParameterTypes).ToArray() };
            public ConstraintSignature GetModifiedType(ConstraintSignature modifier, ConstraintSignature type, bool required) => type;
            public ConstraintSignature GetPinnedType(ConstraintSignature element) => element;
            internal ConstraintSignature Decode(Module module, EntityHandle handle) => handle.Kind switch
            {
                HandleKind.TypeDefinition => GetTypeFromDefinition(module.Reader, (TypeDefinitionHandle)handle, 0),
                HandleKind.TypeReference => GetTypeFromReference(module.Reader, (TypeReferenceHandle)handle, 0),
                HandleKind.TypeSpecification => GetTypeFromSpecification(module.Reader, null, (TypeSpecificationHandle)handle, 0),
                _ => throw new NotSupportedException("Unsupported generic constraint signature"),
            };
        }

        private sealed class ConstraintDefinition
        {
            internal string Name = "";
            internal Module Module = null!;
            internal TypeDefinitionHandle Handle;
            internal ConstraintSignature? Base;
            internal ConstraintSignature[] Interfaces = Array.Empty<ConstraintSignature>();
        }

        private readonly Dictionary<string, ConstraintDefinition> _constraintDefinitions = new(StringComparer.Ordinal);
        private readonly HashSet<string> _genericDefinitionRoots = new(StringComparer.Ordinal);
        private int _constraintSequence;

        private ConstraintSignature[] ConstraintTypes(Module module, GenericParameterHandle handle)
        {
            var provider = new ConstraintProvider(_c);
            return module.Reader.GetGenericParameter(handle).GetConstraints().Select(c =>
                provider.Decode(module, module.Reader.GetGenericParameterConstraint(c).Type)).ToArray();
        }

        private void NoteConstraintSignature(ConstraintSignature signature)
        {
            if (signature.Name is { } name)
                NoteConstraintDefinition(name, signature.Primitive);
            foreach (var child in signature.Children)
                NoteConstraintSignature(child);
        }

        private void NoteConstraintDefinition(string name, TypeDesc? resolved = null)
        {
            var definitionOwner = resolved is { Kind: TypeKind.Template, TemplateModule: { } templateModule }
                ? (Module: templateModule, Handle: resolved.TemplateHandle)
                : resolved is { Kind: TypeKind.Class, Class: { } cls }
                    ? (Module: cls.Module, Handle: cls.Handle) : _c.OpenGenericDefHandleByName(name);
            if (definitionOwner is not { } owner)
                return;
            var reader = owner.Module.Reader;
            var definition = reader.GetTypeDefinition(owner.Handle);
            bool generic = definition.GetGenericParameters().Count != 0;
            var type = generic ? null : _c.GetTypeDescForDefinition(owner.Module, owner.Handle);
            string key = generic ? "generic:" + name : "type:" + (type is { Kind: TypeKind.Class, Class: { } leafClass }
                ? CoreIntrinsics.RuntimeOwnsTypeInfo(leafClass) ? leafClass.CppTypeInfoName : leafClass.CppTypeInfoDefName
                : MethodCompiler.TypeInfoExprOf(type!)![1..]);
            if (_constraintDefinitions.ContainsKey(key))
                return;
            var descriptor = new ConstraintDefinition { Name = name, Module = owner.Module, Handle = owner.Handle };
            _constraintDefinitions.Add(key, descriptor);
            if (generic)
                _c.NoteTypeofOpenGenericDef(name, Compilation.ResolveOpenGenericDefKind(owner.Module, owner.Handle),
                    Compilation.GenericParamNames(owner.Module, owner.Handle));
            else
                _c.NoteTypeIdentityClosure(type!, keepSeed: false);
            var provider = new ConstraintProvider(_c);
            if (!definition.BaseType.IsNil)
                descriptor.Base = provider.Decode(owner.Module, definition.BaseType);
            descriptor.Interfaces = definition.GetInterfaceImplementations().Select(i =>
                provider.Decode(owner.Module, reader.GetInterfaceImplementation(i).Interface)).ToArray();
            if (descriptor.Base is { } baseType)
                NoteConstraintSignature(baseType);
            foreach (var itf in descriptor.Interfaces)
                NoteConstraintSignature(itf);
            foreach (var parameter in definition.GetGenericParameters())
                foreach (var constraint in ConstraintTypes(owner.Module, parameter))
                    NoteConstraintSignature(constraint);
        }

        private void NoteConstraintMetadata()
        {
            _genericDefinitionRoots.UnionWith(_c.TypeofOpenGenericDefs.Keys);
            foreach (var cls in _e.TopoOrder().ToList())
            {
                if (_e.IsOpaque(cls) || _e.IsCanonicalWorld(cls))
                    continue;
                var accessors = PropertyAccessorHandles(cls);
                foreach (var method in _e.ReflectionMethods(cls).ToList())
                {
                    if (method.Handle.IsNil || !_e.KeepsReflectionMethodRow(cls, method, accessors))
                        continue;
                    var methodDefinition = method.Module.Reader.GetMethodDefinition(method.Handle);
                    if (methodDefinition.GetGenericParameters().Count > 0)
                    {
                        var signature = methodDefinition.DecodeSignature(new ConstraintProvider(_c), null);
                        NoteConstraintSignature(signature.ReturnType);
                        foreach (var parameterType in signature.ParameterTypes)
                            NoteConstraintSignature(parameterType);
                    }
                    foreach (var parameter in method.Module.Reader.GetMethodDefinition(method.Handle).GetGenericParameters())
                        foreach (var constraint in ConstraintTypes(method.Module, parameter))
                            NoteConstraintSignature(constraint);
                }
                if (_e.GenericDefInfo(cls) is { } generic)
                    NoteConstraintDefinition(generic.DefName, TypeDesc.MakeClass(cls));
            }
            foreach (string name in _c.TypeofOpenGenericDefs.Keys.ToList())
                NoteConstraintDefinition(name);
            NoteGenericDefinitionMemberTypes();
        }

        private string RenderConstraintSignature(ConstraintSignature signature)
        {
            var children = signature.Children.Select(RenderConstraintSignature).Select(s => "*" + s).ToArray();
            string symbol = "constraint_sig_" + _constraintSequence++;
            string childExpr = "nullptr";
            if (children.Length > 0)
            {
                childExpr = symbol + "_children";
                _sb.AppendLine($"static const Dn2CppBindingSignature {childExpr}[] = {{ {string.Join(", ", children)} }};");
            }
            string type = signature.Kind != 4 && signature.Primitive is { Kind: TypeKind.Primitive or TypeKind.Class } leaf
                ? MethodCompiler.TypeInfoExprOf(leaf) ?? "nullptr" : "nullptr";
            string name = signature.Name is { } n ? "\"" + CLiteral(n) + "\"" : "nullptr";
            _sb.AppendLine($"static const Dn2CppBindingSignature {symbol} = {{ {signature.Kind}, {type}, {signature.Value}, {childExpr}, {children.Length}, {name} }};");
            return "&" + symbol;
        }

        private string RenderConstraintVector(ConstraintSignature[] signatures)
        {
            if (signatures.Length == 0)
                return "nullptr";
            var values = signatures.Select(RenderConstraintSignature).Select(s => "*" + s).ToArray();
            string symbol = "constraint_vec_" + _constraintSequence++;
            _sb.AppendLine($"static const Dn2CppBindingSignature {symbol}[] = {{ {string.Join(", ", values)} }};");
            return symbol;
        }

        private string RenderFormalParameters(Module module, GenericParameterHandleCollection handles)
        {
            if (handles.Count == 0)
                return "nullptr";
            var entries = new string[handles.Count];
            foreach (var handle in handles)
            {
                var parameter = module.Reader.GetGenericParameter(handle);
                var constraints = ConstraintTypes(module, handle);
                string vector = RenderConstraintVector(constraints);
                entries[parameter.Index] = "{ \"" + CLiteral(module.Reader.GetString(parameter.Name))
                    + "\", " + (int)parameter.Attributes + ", " + vector + ", " + constraints.Length + " }";
            }
            string symbol = "constraint_params_" + _constraintSequence++;
            _sb.AppendLine($"static const Dn2CppMethodGenericParameter {symbol}[] = {{ {string.Join(", ", entries)} }};");
            return symbol;
        }

        private string RenderMethodDefinitionSignature(MethodInfo method)
        {
            if (method.Handle.IsNil)
                return "nullptr";
            var definition = method.Module.Reader.GetMethodDefinition(method.Handle);
            if (definition.GetGenericParameters().Count == 0)
                return "nullptr";
            var signature = definition.DecodeSignature(new ConstraintProvider(_c), null);
            string returnType = RenderConstraintSignature(signature.ReturnType);
            string parameters = RenderConstraintVector(signature.ParameterTypes.ToArray());
            string symbol = "method_signature_" + _constraintSequence++;
            _sb.AppendLine($"static const Dn2CppMethodDefinitionSignature {symbol} = {{ {returnType}, {parameters}, {signature.ParameterTypes.Length} }};");
            return "&" + symbol;
        }

        private void EmitConstraintMetadata()
        {
            foreach (var pair in _constraintDefinitions.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var definition = pair.Value;
                var handles = definition.Module.Reader.GetTypeDefinition(definition.Handle).GetGenericParameters();
                string parameters = RenderFormalParameters(definition.Module, handles);
                string baseType = definition.Base is { } b ? RenderConstraintSignature(b) : "nullptr";
                string interfaces = RenderConstraintVector(definition.Interfaces);
                string symbol = "constraint_type_" + _constraintSequence++;
                bool available = !_c.TrimsReflection || definition.Module == _c.AppModule || _genericDefinitionRoots.Contains(definition.Name) || _c.Classes.Any(c => c.Module == definition.Module
                    && c.Handle == definition.Handle && _c.KeepsReflectionMetadata(c));
                if (!available)
                    _e._strippedGenericMetadata.Add(definition.Name);
                bool defaultConstructor = definition.Module.Reader.GetTypeDefinition(definition.Handle).GetMethods().Any(handle =>
                {
                    var method = definition.Module.Reader.GetMethodDefinition(handle);
                    return definition.Module.Reader.GetString(method.Name) == ".ctor"
                        && (method.Attributes & System.Reflection.MethodAttributes.MemberAccessMask) == System.Reflection.MethodAttributes.Public
                        && method.DecodeSignature(new ConstraintProvider(_c), null).ParameterTypes.Length == 0;
                });
                _e._metadataHeader.AppendLine($"extern const Dn2CppGenericTypeMetadata {symbol};");
                _sb.AppendLine($"extern const Dn2CppGenericTypeMetadata {symbol} = {{ {parameters}, {handles.Count}, {baseType}, {interfaces}, {definition.Interfaces.Length}, {(available ? "true" : "false")}, {(defaultConstructor ? "true" : "false")} }};");
                _e._genericMetadata[pair.Key] = "&" + symbol;
            }
        }
    }
}
