using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal bool IsTypedGCHandle(ClassInfo owner) =>
        owner.IntrinsicCppName == "Dn2CppGCHandle" && owner.Context.TypeArgs.Length == 1
        && GenericDefFullName(owner) == "System.Runtime.InteropServices.GCHandle";

    private void CompleteTypedGCHandleInterfaces(ClassInfo owner)
    {
        var target = owner.Context.TypeArgs[0];
        if (target.Class is { } targetClass)
            EnsureShape(targetClass);
        if (owner.Module.AssemblyName != "System.Private.CoreLib"
            || !(target.IsObject || target.IsString || target.Kind is TypeKind.SZArray or TypeKind.MDArray or TypeKind.GenericVar
                || target is { Kind: TypeKind.Class, Class: { IsValueType: false, IsEnum: false } }))
            throw new NotSupportedException($"{owner.FullName}: typed GCHandle requires a CoreLib definition and a reference-type argument");
        var reader = owner.Module.Reader;
        foreach (var handle in reader.GetTypeDefinition(owner.Handle).GetInterfaceImplementations())
        {
            var token = reader.GetInterfaceImplementation(handle).Interface;
            var type = token.Kind switch
            {
                HandleKind.TypeDefinition => TypeDesc.MakeClass(owner.Module.ClassMap[(TypeDefinitionHandle)token]),
                HandleKind.TypeReference => ResolveTypeRef(owner.Module, (TypeReferenceHandle)token),
                HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)token)
                    .DecodeSignature(SigProvider, owner.Context),
                _ => null,
            };
            if (type is { Kind: TypeKind.Class, Class: { } itf } && IsTypedGCHandleInterface(owner, itf))
                owner.Interfaces.Add(itf);
            else
                throw new NotSupportedException($"{owner.FullName}: typed GCHandle interface is not modeled");
        }
    }

    internal bool IsTypedGCHandleInterface(ClassInfo owner, ClassInfo itf) =>
        itf.Module == owner.Module && itf.IsInterface
        && (itf.FullName == "System.IDisposable" && itf.Context.TypeArgs.Length == 0
            || GenericDefFullName(itf) == "System.IEquatable"
                && itf.Context.TypeArgs is [{ Kind: TypeKind.Class, Class: { } argument }] && argument == owner);

    private void NoteTypedGCHandleInterfaces(ClassInfo owner, bool keepSeed)
    {
        EnsureShape(owner);
        // IEquatable's argument points back to this owner; do not recurse through
        // its generic arguments while retaining the identity's interface rows.
        foreach (var itf in owner.Interfaces)
        {
            itf.EnsureMembers();
            _ = TypedGCHandleInterfaceMethod(owner, itf);
            if (keepSeed)
                NoteReferencedType(itf);
            else if (ReferencedTypes.Add(itf))
            {
                _identityOnlyReferences.Add(itf);
                NoteConditionalPreservationUse(itf);
            }
        }
    }

    internal MethodInfo TypedGCHandleInterfaceMethod(ClassInfo owner, ClassInfo itf)
    {
        if (!IsTypedGCHandleInterface(owner, itf) || itf.Methods.Count != 1)
            throw new NotSupportedException($"{owner.FullName}: typed GCHandle interface requires its exact declaration");
        var method = itf.Methods[0];
        string name = itf.FullName == "System.IDisposable" ? "Dispose" : "Equals";
        if (method.Name != name || method.IsStatic || !method.IsAbstract || method.VtableSlot < 0)
            throw new NotSupportedException($"{owner.FullName}: typed GCHandle interface requires its exact declaration");
        ValidateTypedGCHandleSignature(owner, name, method.Signature);
        return method;
    }

    internal void ValidateTypedGCHandleSignature(ClassInfo owner, string name, MethodSignature<TypeDesc> signature)
    {
        var parameters = signature.ParameterTypes;
        var result = signature.ReturnType;
        bool isStatic = name is "FromIntPtr" or "ToIntPtr";
        bool valid = signature.Header.RawValue == (isStatic ? 0 : 0x20)
            && signature.GenericParameterCount == 0 && signature.RequiredParameterCount == parameters.Length
            && (name switch
            {
                ".ctor" => result.IsVoid && parameters.Length == 1
                    && (parameters[0] == owner.Context.TypeArgs[0]
                        || parameters[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr }),
                "get_Target" => parameters.Length == 0 && result == owner.Context.TypeArgs[0],
                "set_Target" => result.IsVoid && parameters.Length == 1 && parameters[0] == owner.Context.TypeArgs[0],
                "get_IsAllocated" => parameters.Length == 0
                    && result is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean },
                "GetHashCode" => parameters.Length == 0
                    && result is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                "Dispose" => result.IsVoid && parameters.Length == 0,
                "FromIntPtr" => result is { Kind: TypeKind.Class, Class: { } from } && from == owner
                    && parameters is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr }],
                "ToIntPtr" => result is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr }
                    && parameters is [{ Kind: TypeKind.Class, Class: { } to }] && to == owner,
                "Equals" => result is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
                    && parameters.Length == 1 && (parameters[0].IsObject
                        || parameters[0] is { Kind: TypeKind.Class, Class: { } other } && other == owner),
                _ => false,
            });
        if (!valid)
            throw new NotSupportedException($"{owner.FullName}::{name}: typed GCHandle member signature is not modeled");
    }
}
