using System.Reflection.Metadata;
using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    /// <summary>A signature type as .NET formats it, keyed as .NET tells function
    /// pointer types apart: by calling convention and signature, without custom
    /// modifiers. <see cref="FunctionPointer"/> is the function pointer type it is,
    /// or that its pointer or by-ref levels end at.</summary>
    internal sealed class SpelledType
    {
        private readonly SpelledType? _functionPointer;
        private readonly bool _isFunctionPointer;

        internal TypeDesc Type { get; }
        internal string Name { get; }
        internal string Key { get; }
        internal bool Open { get; }
        internal BindingSignature Binding { get; }
        internal SpelledType? FunctionPointer => _isFunctionPointer ? this : _functionPointer;

        internal SpelledType(TypeDesc type, string name, string key, SpelledType? functionPointer = null,
            bool isFunctionPointer = false, bool open = false, BindingSignature? binding = null)
        {
            Type = type;
            Name = name;
            Key = key;
            Open = open;
            Binding = binding ?? new BindingSignature(type: type);
            _functionPointer = functionPointer;
            _isFunctionPointer = isFunctionPointer;
        }
    }

    /// <summary>Signature decoder that spells function pointer types, whose
    /// signatures the main TypeDesc decoder drops.</summary>
    private sealed class FunctionPointerSpellingProvider : ISignatureTypeProvider<SpelledType, object?>
    {
        private readonly CppEmitter? _e;
        private readonly SignatureProvider _types;

        internal FunctionPointerSpellingProvider(CppEmitter? e, SignatureProvider types)
        {
            _e = e;
            _types = types;
        }

        // Keyed by the model's identity, so same-named types of two assemblies
        // name two function pointer types, as they do in .NET.
        private SpelledType Leaf(TypeDesc type)
        {
            string name = type.IsVoid ? "System.Void" : _e is not null ? _e.ReflectionSignatureType(type, qualifyPrimitive: true) : "";
            return new SpelledType(type, name, Compilation.IdentityMangle(type),
                open: Compilation.ContainsGenericVar(type) || Compilation.ContainsCanonPlaceholder(type));
        }

        public SpelledType GetPrimitiveType(PrimitiveTypeCode typeCode) => Leaf(_types.GetPrimitiveType(typeCode));
        public SpelledType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            Leaf(_types.GetTypeFromDefinition(reader, handle, rawTypeKind));
        public SpelledType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            Leaf(_types.GetTypeFromReference(reader, handle, rawTypeKind));
        public SpelledType GetSZArrayType(SpelledType elementType) =>
            new(_types.GetSZArrayType(elementType.Type), elementType.Name + "[]", elementType.Key + "[]", open: elementType.Open,
                binding: new BindingSignature(6, children: new[] { elementType.Binding }));
        public SpelledType GetArrayType(SpelledType elementType, ArrayShape shape)
        {
            string rank = "[" + new string(',', shape.Rank - 1) + "]";
            return new(_types.GetArrayType(elementType.Type, shape), elementType.Name + rank, elementType.Key + rank, open: elementType.Open,
                binding: new BindingSignature(7, value: shape.Rank, children: new[] { elementType.Binding }));
        }
        public SpelledType GetByReferenceType(SpelledType elementType) =>
            new(_types.GetByReferenceType(elementType.Type), elementType.Name + "&", elementType.Key + "&",
                elementType.FunctionPointer, open: elementType.Open,
                binding: new BindingSignature(2, children: new[] { elementType.Binding }));
        public SpelledType GetPointerType(SpelledType elementType) =>
            new(_types.GetPointerType(elementType.Type), elementType.Name + "*", elementType.Key + "*",
                elementType.FunctionPointer, open: elementType.Open,
                binding: new BindingSignature(1, children: new[] { elementType.Binding }));
        public SpelledType GetFunctionPointerType(MethodSignature<SpelledType> signature)
        {
            var names = new string[signature.ParameterTypes.Length];
            var keys = new string[names.Length];
            bool open = signature.ReturnType.Open;
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = signature.ParameterTypes[i].Name;
                keys[i] = signature.ParameterTypes[i].Key;
                open |= signature.ParameterTypes[i].Open;
            }
            string name = signature.ReturnType.Name + "(" + string.Join(", ", names) + ")";
            int convention = signature.Header.RawValue;
            // CLR function-pointer identity merges the unmanaged ABI conventions.
            if ((convention & 0xf) is >= 1 and <= 4)
                convention = (convention & ~0xf) | 9;
            string key = "fnptr " + convention.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + " " + signature.ReturnType.Key + "(" + string.Join(", ", keys) + ")";
            var children = new BindingSignature[signature.ParameterTypes.Length + 1];
            children[0] = signature.ReturnType.Binding;
            for (int i = 0; i < signature.ParameterTypes.Length; i++)
                children[i + 1] = signature.ParameterTypes[i].Binding;
            return new SpelledType(TypeDesc.MakeFunctionPointer(), name, key, isFunctionPointer: true, open: open,
                binding: new BindingSignature(3, value: convention, children: children));
        }
        public SpelledType GetGenericInstantiation(SpelledType genericType,
            System.Collections.Immutable.ImmutableArray<SpelledType> typeArguments)
        {
            var args = new TypeDesc[typeArguments.Length];
            for (int i = 0; i < args.Length; i++) args[i] = typeArguments[i].Type;
            var type = _types.GetGenericInstantiation(genericType.Type,
                System.Collections.Immutable.ImmutableArray.Create(args));
            var leaf = Leaf(type);
            var children = new BindingSignature[typeArguments.Length];
            for (int i = 0; i < children.Length; i++)
                children[i] = typeArguments[i].Binding;
            return new SpelledType(type, leaf.Name, leaf.Key, open: leaf.Open,
                binding: new BindingSignature(4, type, children: children));
        }
        public SpelledType GetGenericMethodParameter(object? genericContext, int index) =>
            Leaf(_types.GetGenericMethodParameter(genericContext, index));
        public SpelledType GetGenericTypeParameter(object? genericContext, int index)
        {
            var leaf = Leaf(_types.GetGenericTypeParameter(genericContext, index));
            return new SpelledType(leaf.Type, leaf.Name, leaf.Key, open: leaf.Open,
                binding: leaf.Open ? new BindingSignature(5, value: index) : leaf.Binding);
        }
        public SpelledType GetModifiedType(SpelledType modifier, SpelledType unmodifiedType, bool isRequired) =>
            unmodifiedType;
        public SpelledType GetPinnedType(SpelledType elementType) => elementType;
        public SpelledType GetTypeFromSpecification(MetadataReader reader, object? genericContext,
            TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }


    private readonly Dictionary<MethodInfo, MethodSignature<SpelledType>?> _reflectionSignatures = new();

    private MethodSignature<SpelledType>? ReflectionSignature(MethodInfo method)
    {
        if (!_reflectionSignatures.TryGetValue(method, out var signature))
        {
            try
            {
                signature = method.Module.Reader.GetMethodDefinition(method.Handle).DecodeSignature(
                    new FunctionPointerSpellingProvider(this, _c.SigProvider), method.Context);
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                signature = null;
            }
            _reflectionSignatures[method] = signature;
        }
        return signature;
    }

    internal static bool HasSignatureShape(TypeDesc type) => type.Kind is TypeKind.ByRef or TypeKind.Pointer
        || type.Element is { } element && HasSignatureShape(element);

    private static BindingSignature PointerQuerySignature(TypeDesc type, BindingSignature? functionPointer)
    {
        if (type.IsFunctionPointer)
            return functionPointer ?? new BindingSignature(8);
        return type is { Kind: TypeKind.Pointer, Element: { } element }
            ? new BindingSignature(1, children: new[] { PointerQuerySignature(element, functionPointer) })
            : new BindingSignature(type: type);
    }

    private string EmitReflectionSignature(StringBuilder sb, MethodInfo method, int parameter)
    {
        var type = parameter < 0 ? method.Signature.ReturnType : method.Signature.ParameterTypes[parameter];
        if (!HasSignatureShape(type))
            return "nullptr";
        var signature = ReflectionSignature(method);
        var spelled = signature is { } decoded
            ? parameter < 0 ? decoded.ReturnType : decoded.ParameterTypes[parameter] : null;
        return EmitBindingSignature(sb, QuerySignature(spelled?.Binding ?? new BindingSignature(8)), query: true);
    }

    // A closed leaf uses its actual identity. The binding codec's generic tree is
    // retained only when a runtime-template owner must substitute its arguments.
    private BindingSignature QuerySignature(BindingSignature signature)
    {
        if (signature.Kind == 4 && signature.Type is { } type
            && !Compilation.ContainsGenericVar(type) && !Compilation.ContainsCanonPlaceholder(type)
            && type.Class is { } cls && TypeInfoSymbolDefined(cls.CppTypeInfoName))
            return new BindingSignature(type: type);
        var children = new BindingSignature[signature.Children.Length];
        for (int i = 0; i < children.Length; i++)
            children[i] = QuerySignature(signature.Children[i]);
        return new BindingSignature(signature.Kind, signature.Type, signature.Value, children);
    }

    internal static SpelledType DecodeReflectionType(Compilation compilation, MethodInfo method, int token)
    {
        var handle = System.Reflection.Metadata.Ecma335.MetadataTokens.EntityHandle(token);
        if (handle.Kind != HandleKind.TypeSpecification)
            throw new InvalidOperationException("Signature type token is not a TypeSpec");
        return method.Module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(
            new FunctionPointerSpellingProvider(null, compilation.SigProvider), method.Context);
    }

    private readonly HashSet<ClassInfo> _querySignatureIntrinsicTypes = new();

    private bool IsQueryOnlyIntrinsic(ClassInfo cls) => _querySignatureIntrinsicTypes.Contains(cls)
        && _c.IsIdentityOnlyReference(cls) && !_emit.Contains(cls);

    private void NoteQueryIdentity(TypeDesc type)
    {
        if (type.Class is { } cls)
        {
            if (cls.IntrinsicCppName is not null && CoreIntrinsics.RuntimeTypeInfoSymbol(cls) is null
                && !_c.ReferencedTypes.Contains(cls))
                _querySignatureIntrinsicTypes.Add(cls);
            foreach (var argument in cls.Context.TypeArgs)
                NoteQueryIdentity(argument);
        }
        if (type.Element is { } element)
            NoteQueryIdentity(element);
        _c.NoteTypeIdentityClosure(type, keepSeed: false);
    }

    private void NoteReflectionSignatureLeaves(BindingSignature signature, Action<TypeDesc> add)
    {
        if (signature.Type is { } type && !Compilation.ContainsGenericVar(type)
            && !Compilation.ContainsCanonPlaceholder(type))
        {
            NoteQueryIdentity(type);
            add(type);
        }
        foreach (var child in signature.Children)
            NoteReflectionSignatureLeaves(child, add);
    }

    private void EmitReflectionTypeTokens(CppOutput output)
    {
        foreach (var pair in _c.ReflectionTypeTokens)
        {
            output.Header.AppendLine($"const Dn2CppTypeInfo* {pair.Key}();");
            string signature = EmitBindingSignature(output.Data, QuerySignature(pair.Value), query: true);
            output.Data.AppendLine($"const Dn2CppTypeInfo* {pair.Key}() {{ return dn2cpp_signature_type({signature}); }}");
        }
    }
}
