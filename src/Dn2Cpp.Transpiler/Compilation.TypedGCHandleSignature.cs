using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private TypedGCHandleSignatureProvider? _typedGCHandleSignatureProvider;

    internal MethodSignature<TypeDesc> DecodeTypedGCHandleMember(Module module, MemberReference reference,
        GenericContext context, ClassInfo receiver, bool constrained = false)
    {
        var provider = _typedGCHandleSignatureProvider ??= new(this);
        var reader = module.Reader;
        var parent = reference.Parent.Kind switch
        {
            HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)reference.Parent)
                .DecodeSignature(provider, context),
            HandleKind.TypeReference => ResolveTypeRef(module, (TypeReferenceHandle)reference.Parent),
            HandleKind.TypeDefinition => GetTypeDescForDefinition(module, (TypeDefinitionHandle)reference.Parent),
            _ => null,
        };
        string name = reader.GetString(reference.Name);
        var signature = reference.DecodeMethodSignature(provider, parent?.Class?.Context ?? context);
        if (!IsTypedGCHandleCallDeclaration(receiver, parent, name, signature, constrained))
            throw new NotSupportedException($"{receiver.FullName}::{name}: typed GCHandle call requires its exact declaring type");
        return signature;
    }

    private bool IsTypedGCHandleCallDeclaration(ClassInfo receiver, TypeDesc? parent, string name,
        MethodSignature<TypeDesc> signature, bool constrained)
    {
        if (parent is { Kind: TypeKind.Class, Class: { } owner } && owner == receiver)
            return true;
        if (!constrained)
            return false;
        if (parent?.Class is { } itf && IsTypedGCHandleInterface(receiver, itf))
            return itf.FullName == "System.IDisposable" ? name == "Dispose"
                : name == "Equals" && signature.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } argument }]
                    && argument == receiver;
        bool objectContract = parent?.IsObject == true
            || parent?.Class is { FullName: "System.Object" or "System.ValueType" } baseType
                && baseType.Module == receiver.Module;
        return objectContract && (name == "GetHashCode"
            || name == "Equals" && signature.ParameterTypes is [{ IsObject: true }]);
    }

    internal void ValidateTypedGCHandleTypeEncoding(Module module, EntityHandle token, GenericContext context)
    {
        var provider = _typedGCHandleSignatureProvider ??= new(this);
        if (token.Kind == HandleKind.TypeSpecification)
            _ = module.Reader.GetTypeSpecification((TypeSpecificationHandle)token).DecodeSignature(provider, context);
    }

    internal void ValidateTypedGCHandleConstrainedEncoding(Module module, EntityHandle handle,
        GenericContext context, ClassInfo receiver)
    {
        var provider = _typedGCHandleSignatureProvider ??= new(this);
        if (handle.Kind == HandleKind.MemberReference)
        {
            var reference = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
            var signature = DecodeTypedGCHandleMember(module, reference, context, receiver, constrained: true);
            ValidateTypedGCHandleSignature(receiver, module.Reader.GetString(reference.Name), signature);
            return;
        }
        if (handle.Kind == HandleKind.MethodDefinition)
        {
            var definition = module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            var type = GetTypeDescForDefinition(module, definition.GetDeclaringType());
            string name = module.Reader.GetString(definition.Name);
            var signature = definition.DecodeSignature(provider, type.Class?.Context ?? context);
            if (!IsTypedGCHandleCallDeclaration(receiver, type, name, signature, constrained: true))
                throw new NotSupportedException($"{receiver.FullName}::{name}: typed GCHandle call requires its exact declaring type");
            ValidateTypedGCHandleSignature(receiver, name, signature);
            return;
        }
        throw new NotSupportedException($"{receiver.FullName}: typed GCHandle call requires a non-generic member");
    }

    // Ordinary signature decoding erases modifiers and the class/value marker.
    // Validate them before substituting the native handle representation.
    private sealed class TypedGCHandleSignatureProvider(Compilation compilation) : ISignatureTypeProvider<TypeDesc, object?>
    {
        private static NotSupportedException Refusal() =>
            new("typed GCHandle member signature is not modeled: invalid type encoding or modifier");

        private static string DefinitionName(MetadataReader reader, TypeDefinitionHandle handle)
        {
            var definition = reader.GetTypeDefinition(handle);
            return reader.GetString(definition.Namespace) + "." + reader.GetString(definition.Name);
        }

        private TypeDesc CheckKind(TypeDesc type, byte rawTypeKind)
        {
            bool value;
            if (type is { Kind: TypeKind.Template, TemplateModule: { } module })
            {
                var reader = module.Reader;
                var parent = reader.GetTypeDefinition(type.TemplateHandle).BaseType;
                string name = parent.IsNil ? "" : parent.Kind switch
                {
                    HandleKind.TypeDefinition => DefinitionName(reader, (TypeDefinitionHandle)parent),
                    HandleKind.TypeReference => TypeRefFullName(reader, (TypeReferenceHandle)parent),
                    _ => "",
                };
                value = name is "System.ValueType" or "System.Enum";
            }
            else if (type.Class is { } cls)
            {
                compilation.EnsureShape(cls);
                value = cls.IsValueType || cls.IsEnum;
            }
            else
                value = type.Kind == TypeKind.Primitive && !type.IsObject && !type.IsString;
            if (rawTypeKind != (value ? (byte)SignatureTypeKind.ValueType : (byte)SignatureTypeKind.Class))
                throw Refusal();
            return type;
        }

        public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            CheckKind(compilation.SigProvider.GetTypeFromDefinition(reader, handle, rawTypeKind), rawTypeKind);
        public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            CheckKind(compilation.SigProvider.GetTypeFromReference(reader, handle, rawTypeKind), rawTypeKind);
        public TypeDesc GetTypeFromSpecification(MetadataReader reader, object? context, TypeSpecificationHandle handle, byte rawTypeKind) =>
            CheckKind(reader.GetTypeSpecification(handle).DecodeSignature(this, context), rawTypeKind);
        public TypeDesc GetPrimitiveType(PrimitiveTypeCode code) => compilation.SigProvider.GetPrimitiveType(code);
        public TypeDesc GetSZArrayType(TypeDesc element) => compilation.SigProvider.GetSZArrayType(element);
        public TypeDesc GetArrayType(TypeDesc element, ArrayShape shape) => compilation.SigProvider.GetArrayType(element, shape);
        public TypeDesc GetByReferenceType(TypeDesc element) => compilation.SigProvider.GetByReferenceType(element);
        public TypeDesc GetPointerType(TypeDesc element) => compilation.SigProvider.GetPointerType(element);
        public TypeDesc GetFunctionPointerType(MethodSignature<TypeDesc> signature) => compilation.SigProvider.GetFunctionPointerType(signature);
        public TypeDesc GetGenericInstantiation(TypeDesc type, ImmutableArray<TypeDesc> arguments) =>
            compilation.SigProvider.GetGenericInstantiation(type, arguments);
        public TypeDesc GetGenericMethodParameter(object? context, int index) => compilation.SigProvider.GetGenericMethodParameter(context, index);
        public TypeDesc GetGenericTypeParameter(object? context, int index) => compilation.SigProvider.GetGenericTypeParameter(context, index);
        public TypeDesc GetModifiedType(TypeDesc modifier, TypeDesc unmodified, bool required) => throw Refusal();
        public TypeDesc GetPinnedType(TypeDesc element) => throw Refusal();
    }
}
