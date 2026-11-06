namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool NeedsVectorStorageFieldCopy(ClassInfo owner, FieldInfo field) =>
        owner.IntrinsicCppName is null && (_c.ContainsVectorStorage(field.Type)
            || _c.ContainsVectorStorage(TypeDesc.MakeClass(owner)));

    private string VectorStorageFieldAddress(ClassInfo owner, FieldInfo field, StackEntry receiver)
    {
        _c.NoteNamedStructSymbol(_method, owner.CppStructName);
        string address = receiver.Kind == StackKind.Struct ? $"&({StructLValue(receiver)})" : NullCheckReceiver(receiver);
        // Form the offset from bytes, never an aligned field lvalue in a boxed payload.
        return $"((uint8_t*)({address}) + offsetof({owner.CppStructName}, {field.CppName}))";
    }

    private string? VectorStorageFieldRead(ClassInfo owner, FieldInfo field, StackEntry receiver)
    {
        if (receiver.Kind == StackKind.Struct || !NeedsVectorStorageFieldCopy(owner, field)) return null;
        string storage = LayoutFieldType(owner, field);
        string value = NewTemp(storage);
        Emit($"std::memcpy(&{value}, {VectorStorageFieldAddress(owner, field, receiver)}, sizeof({value}));");
        return value;
    }

    private bool TryVectorStorageFieldStore(ClassInfo owner, FieldInfo field, StackEntry receiver, StackEntry value)
    {
        if (!NeedsVectorStorageFieldCopy(owner, field)) return false;
        string storage = LayoutFieldType(owner, field);
        string copy = NewTemp(storage);
        Emit($"{copy} = {Cast(value, storage)};");
        Emit($"std::memcpy({VectorStorageFieldAddress(owner, field, receiver)}, &{copy}, sizeof({copy}));");
        return true;
    }
}
