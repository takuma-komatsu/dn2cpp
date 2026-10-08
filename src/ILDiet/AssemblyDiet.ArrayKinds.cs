using System.Collections.Immutable;
using System.Reflection.Metadata;
using Mono.Cecil;
using TypeReference = Mono.Cecil.TypeReference;
using TypeSpecification = Mono.Cecil.TypeSpecification;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using MemberReference = Mono.Cecil.MemberReference;
using PropertyDefinition = Mono.Cecil.PropertyDefinition;
using TypeFix = System.Action<Mono.Cecil.TypeReference>;
using MetadataTokens = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class AssemblyDiet
{
    private readonly Dictionary<ModuleDefinition, ArrayKindPreserver> _memberArrayKinds = new();

    private void PreserveMemberArrayKinds(MemberReference member)
    {
        if (!_byModule.TryGetValue(member.Module, out var assembly)) return;
        if (!_memberArrayKinds.TryGetValue(member.Module, out var kinds))
        {
            kinds = new ArrayKindPreserver(assembly.PE.GetMetadataReader(), member.Module);
            _memberArrayKinds.Add(member.Module, kinds);
        }
        kinds.Member(member);
    }

    private static void PreserveArrayKinds(DietAssembly assembly)
    {
        var reader = assembly.PE.GetMetadataReader();
        var kinds = new ArrayKindPreserver(reader, assembly.Assembly.MainModule);
        foreach (var type in AllTypes(assembly.Assembly.MainModule.Types))
        {
            kinds.Type(type.BaseType);
            foreach (var implementation in type.Interfaces) kinds.Type(implementation.InterfaceType);
            foreach (var parameter in type.GenericParameters)
                foreach (var constraint in parameter.Constraints) kinds.Type(constraint.ConstraintType);
            foreach (var field in type.Fields) kinds.Member(field);
            foreach (var method in type.Methods)
            {
                kinds.Member(method);
                foreach (var parameter in method.GenericParameters)
                    foreach (var constraint in parameter.Constraints) kinds.Type(constraint.ConstraintType);
                foreach (var target in method.Overrides) kinds.Member(target);
                if (!method.HasBody) continue;
                kinds.Locals(method.Body);
                foreach (var handler in method.Body.ExceptionHandlers) kinds.Type(handler.CatchType);
                foreach (var instruction in method.Body.Instructions)
                    switch (instruction.Operand)
                    {
                        case TypeReference reference: kinds.Type(reference); break;
                        case MemberReference member: kinds.Member(member); break;
                        case CallSite site: kinds.CallSite(site); break;
                    }
            }
            foreach (var property in type.Properties)
                kinds.Property(property);
            foreach (var item in type.Events) kinds.Type(item.EventType);
        }
    }

    private sealed class ArrayKindPreserver : ISignatureTypeProvider<TypeFix?, object?>
    {
        private readonly MetadataReader _reader;
        private readonly ModuleDefinition _module;
        private readonly HashSet<MemberReference> _members = new();
        private readonly HashSet<TypeReference> _types = new();

        internal ArrayKindPreserver(MetadataReader reader, ModuleDefinition module)
        {
            _reader = reader;
            _module = module;
        }

        internal void Type(TypeReference? type)
        {
            if (type is null || type.Module != _module || !_types.Add(type)
                || type.MetadataToken.TokenType != TokenType.TypeSpec || type.MetadataToken.RID == 0) return;
            _reader.GetTypeSpecification(MetadataTokens.TypeSpecificationHandle((int)type.MetadataToken.RID))
                .DecodeSignature(this, null)?.Invoke(type);
        }

        internal void Member(MemberReference member)
        {
            if (member.Module != _module || !_members.Add(member)) return;
            Type(member.DeclaringType);
            int row = (int)member.MetadataToken.RID;
            if (row == 0) return;
            if (member is GenericInstanceMethod generic)
            {
                var arguments = _reader.GetMethodSpecification(MetadataTokens.MethodSpecificationHandle(row))
                    .DecodeSignature(this, null);
                if (arguments.Length != generic.GenericArguments.Count) throw Mismatch();
                for (int i = 0; i < arguments.Length; i++) arguments[i]?.Invoke(generic.GenericArguments[i]);
                Member(generic.ElementMethod);
            }
            else if (member is MethodReference method)
            {
                var signature = member.MetadataToken.TokenType == TokenType.Method
                    ? _reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(row)).DecodeSignature(this, null)
                    : _reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(row)).DecodeMethodSignature(this, null);
                Signature(signature, method.ReturnType, method.Parameters);
            }
            else if (member is FieldReference field)
            {
                var fix = member.MetadataToken.TokenType == TokenType.Field
                    ? _reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(row)).DecodeSignature(this, null)
                    : _reader.GetMemberReference(MetadataTokens.MemberReferenceHandle(row)).DecodeFieldSignature(this, null);
                fix?.Invoke(field.FieldType);
            }
        }

        internal void Locals(Mono.Cecil.Cil.MethodBody body)
        {
            if (body.LocalVarToken.RID == 0) return;
            var locals = _reader.GetStandaloneSignature(
                MetadataTokens.StandaloneSignatureHandle((int)body.LocalVarToken.RID)).DecodeLocalSignature(this, null);
            if (locals.Length != body.Variables.Count) throw Mismatch();
            for (int i = 0; i < locals.Length; i++) locals[i]?.Invoke(body.Variables[i].VariableType);
        }

        internal void CallSite(CallSite site)
        {
            if (site.MetadataToken.RID == 0) return;
            Signature(_reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle((int)site.MetadataToken.RID))
                .DecodeMethodSignature(this, null), site.ReturnType, site.Parameters);
        }

        internal void Property(PropertyDefinition property)
        {
            var signature = _reader.GetPropertyDefinition(
                MetadataTokens.PropertyDefinitionHandle((int)property.MetadataToken.RID)).DecodeSignature(this, null);
            // Cecil derives index parameters from accessors, which Sweep can remove.
            if (property.GetMethod is null && property.SetMethod is null)
                signature.ReturnType?.Invoke(property.PropertyType);
            else
                Signature(signature, property.PropertyType, property.Parameters);
        }

        internal void Signature(MethodSignature<TypeFix?> signature, TypeReference result,
            Mono.Collections.Generic.Collection<ParameterDefinition> parameters)
        {
            if (signature.ParameterTypes.Length != parameters.Count) throw Mismatch();
            signature.ReturnType?.Invoke(result);
            for (int i = 0; i < parameters.Count; i++)
            {
                var type = parameters[i].ParameterType;
                if (type is SentinelType sentinel) type = sentinel.ElementType;
                signature.ParameterTypes[i]?.Invoke(type);
            }
        }

        public TypeFix? GetArrayType(TypeFix? elementType, ArrayShape shape)
        {
            bool rankOneWithoutLowerBound = shape.Rank == 1 && shape.LowerBounds.IsEmpty;
            if (!rankOneWithoutLowerBound && elementType is null) return null;
            return type =>
            {
                if (type is not ArrayType array || array.Rank != shape.Rank) throw Mismatch();
                elementType?.Invoke(array.ElementType);
                // Cecil writes a single unsized dimension as SZARRAY. An explicit zero
                // lower bound retains the original ARRAY kind and the same CLR type.
                if (rankOneWithoutLowerBound && array.IsVector) array.Dimensions[0] = new ArrayDimension(0, null);
            };
        }

        public TypeFix? GetSZArrayType(TypeFix? elementType)
        {
            if (elementType is null) return null;
            return type =>
            {
                if (type is not ArrayType array || !array.IsVector) throw Mismatch();
                elementType(array.ElementType);
            };
        }
        public TypeFix? GetByReferenceType(TypeFix? elementType) => Wrap<ByReferenceType>(elementType);
        public TypeFix? GetPointerType(TypeFix? elementType) => Wrap<PointerType>(elementType);
        public TypeFix? GetPinnedType(TypeFix? elementType) => Wrap<PinnedType>(elementType);

        private static TypeFix? Wrap<T>(TypeFix? elementType) where T : TypeSpecification
        {
            if (elementType is null) return null;
            return type =>
            {
                if (type is not T wrapper) throw Mismatch();
                elementType(wrapper.ElementType);
            };
        }

        public TypeFix? GetGenericInstantiation(TypeFix? genericType, ImmutableArray<TypeFix?> typeArguments)
        {
            if (genericType is null && typeArguments.All(argument => argument is null)) return null;
            return type =>
            {
                if (type is not GenericInstanceType generic || generic.GenericArguments.Count != typeArguments.Length)
                    throw Mismatch();
                genericType?.Invoke(generic.ElementType);
                for (int i = 0; i < typeArguments.Length; i++) typeArguments[i]?.Invoke(generic.GenericArguments[i]);
            };
        }

        public TypeFix? GetFunctionPointerType(MethodSignature<TypeFix?> signature)
        {
            if (signature.ReturnType is null && signature.ParameterTypes.All(parameter => parameter is null)) return null;
            return type =>
            {
                if (type is not FunctionPointerType pointer) throw Mismatch();
                Signature(signature, pointer.ReturnType, pointer.Parameters);
            };
        }

        public TypeFix? GetModifiedType(TypeFix? modifier, TypeFix? unmodifiedType, bool isRequired)
        {
            if (modifier is null && unmodifiedType is null) return null;
            return type =>
            {
                if (type is not IModifierType wrapper
                    || isRequired != (type is RequiredModifierType)) throw Mismatch();
                modifier?.Invoke(wrapper.ModifierType);
                unmodifiedType?.Invoke(wrapper.ElementType);
            };
        }

        public TypeFix? GetTypeFromSpecification(MetadataReader reader, object? genericContext,
            TypeSpecificationHandle handle, byte rawTypeKind) => reader.GetTypeSpecification(handle).DecodeSignature(this, null);
        public TypeFix? GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => null;
        public TypeFix? GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => null;
        public TypeFix? GetGenericMethodParameter(object? genericContext, int index) => null;
        public TypeFix? GetGenericTypeParameter(object? genericContext, int index) => null;
        public TypeFix? GetPrimitiveType(PrimitiveTypeCode typeCode) => null;

        private static InvalidOperationException Mismatch() =>
            new("Original array signature does not match the retained Cecil signature.");
    }
}
