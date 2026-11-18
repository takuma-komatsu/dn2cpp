using System.Reflection;
using System.Reflection.Metadata;

namespace Dn2Cpp;

internal static partial class CoreIntrinsics
{
    internal static bool IsSemaphoreWaiterName(string? owner, string name) =>
        owner == "System.Threading.SemaphoreSlim" && name is "CreateAndAddAsyncWaiter" or "RemoveAsyncWaiter";

    private static bool IsSemaphoreWaiter(MethodInfo method)
    {
        var owner = method.DeclaringClass;
        if (method.Module.AssemblyName != "System.Private.CoreLib" || owner.Module != method.Module
            || method.Handle.IsNil || owner.Handle.IsNil || method.IsSynthetic
            || owner.IsValueType || owner.IsInterface || owner.IsEnum || owner.IsByRefLike
            || owner.GenericArity != 0 || owner.Context.TypeArgs.Length != 0 || owner.Context.MethodArgs.Length != 0
            || method.Context.TypeArgs.Length != 0 || method.Context.MethodArgs.Length != 0
            || method.Attributes != (MethodAttributes.Private | MethodAttributes.HideBySig)
            || method.ImplAttributes != MethodImplAttributes.IL || method.Rva == 0 || method.PInvoke is not null)
            return false;
        var reader = method.Module.Reader;
        var definition = reader.GetMethodDefinition(method.Handle);
        var type = reader.GetTypeDefinition(owner.Handle);
        if (!type.GetDeclaringType().IsNil || type.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(type.Namespace, "System.Threading")
            || !reader.StringComparer.Equals(type.Name, "SemaphoreSlim")
            || definition.GetDeclaringType() != owner.Handle || definition.GetGenericParameters().Count != 0
            || !reader.StringComparer.Equals(definition.Name, method.Name)
            || definition.Attributes != method.Attributes || definition.ImplAttributes != method.ImplAttributes
            || definition.RelativeVirtualAddress != method.Rva
            || !IsSemaphoreWaiterSignature(reader.GetBlobReader(definition.Signature), method.Name))
            return false;
        var blob = reader.GetBlobReader(definition.Signature);
        blob.ReadByte();
        blob.ReadByte();
        if (method.Name == "RemoveAsyncWaiter") blob.ReadByte();
        blob.ReadByte();
        var handle = blob.ReadTypeHandle();
        if (handle.Kind != HandleKind.TypeDefinition || !IsSemaphoreWaiterNode(reader, (TypeDefinitionHandle)handle))
            return false;
        return !method.SignatureReady || IsSemaphoreWaiterDecodedSignature(method.Signature, method.Name);
    }

    private static bool IsSemaphoreWaiterSignature(BlobReader blob, string name)
    {
        bool remove = name == "RemoveAsyncWaiter";
        if (blob.RemainingBytes < 4 || blob.ReadByte() != 0x20
            || blob.ReadByte() != (remove ? 1 : 0)
            || remove && blob.ReadByte() != (byte)SignatureTypeCode.Boolean
            || blob.ReadByte() != 0x12 || !blob.TryReadCompressedInteger(out int token))
            return false;
        return token > 0 && (token & 3) <= 1 && blob.RemainingBytes == 0;
    }

    private static bool IsSemaphoreWaiterDecodedSignature(MethodSignature<TypeDesc> signature, string name)
    {
        bool remove = name == "RemoveAsyncWaiter";
        if (signature.Header.RawValue != 0x20 || signature.GenericParameterCount != 0
            || signature.RequiredParameterCount != (remove ? 1 : 0)
            || signature.ParameterTypes.Length != (remove ? 1 : 0)
            || remove && signature.ReturnType is not { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean, IsCanonPlaceholder: false })
            return false;
        TypeDesc node = remove ? signature.ParameterTypes[0] : signature.ReturnType;
        return node is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { } cls }
            && cls.Module.AssemblyName == "System.Private.CoreLib" && !cls.Handle.IsNil
            && cls.Context.TypeArgs.Length == 0 && cls.Context.MethodArgs.Length == 0
            && IsSemaphoreWaiterNode(cls.Module.Reader, cls.Handle);
    }

    // The bridge allocates the promise prefix and addresses these exact trailing links.
    private static bool IsSemaphoreWaiterNode(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var node = reader.GetTypeDefinition(handle);
        if (!reader.StringComparer.Equals(node.Name, "TaskNode") || !reader.StringComparer.Equals(node.Namespace, "")
            || node.GetDeclaringType().IsNil || node.GetGenericParameters().Count != 0
            || (node.Attributes & TypeAttributes.Sealed) == 0 || node.BaseType.Kind != HandleKind.TypeSpecification)
            return false;
        var owner = reader.GetTypeDefinition(node.GetDeclaringType());
        if (!owner.GetDeclaringType().IsNil || !reader.StringComparer.Equals(owner.Namespace, "System.Threading")
            || !reader.StringComparer.Equals(owner.Name, "SemaphoreSlim"))
            return false;
        var parent = reader.GetBlobReader(reader.GetTypeSpecification((TypeSpecificationHandle)node.BaseType).Signature);
        if (parent.RemainingBytes < 5 || parent.ReadByte() != 0x15 || parent.ReadByte() != 0x12)
            return false;
        var taskHandle = parent.ReadTypeHandle();
        if (taskHandle.Kind != HandleKind.TypeDefinition || parent.RemainingBytes != 2
            || parent.ReadByte() != 1 || parent.ReadByte() != (byte)SignatureTypeCode.Boolean)
            return false;
        var task = reader.GetTypeDefinition((TypeDefinitionHandle)taskHandle);
        if (!reader.StringComparer.Equals(task.Namespace, "System.Threading.Tasks")
            || !reader.StringComparer.Equals(task.Name, "Task`1"))
            return false;
        bool previous = false, next = false;
        foreach (var fieldHandle in node.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            if (field.Attributes != FieldAttributes.Assembly)
                return false;
            bool isPrevious = reader.StringComparer.Equals(field.Name, "Prev");
            bool isNext = reader.StringComparer.Equals(field.Name, "Next");
            if ((!isPrevious && !isNext) || isPrevious && previous || isNext && next)
                return false;
            var signature = reader.GetBlobReader(field.Signature);
            if (signature.RemainingBytes < 3 || signature.ReadByte() != 0x06 || signature.ReadByte() != 0x12
                || signature.ReadTypeHandle() != handle || signature.RemainingBytes != 0)
                return false;
            previous |= isPrevious;
            next |= isNext;
        }
        return previous && next;
    }
}
