namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool NeedsFixedVectorFieldCopy(ClassInfo owner, FieldInfo field) =>
        owner.IntrinsicCppName is null && (_c.ContainsFixedVectorStorage(field.Type)
            || _c.ContainsFixedVectorStorage(TypeDesc.MakeClass(owner)));

    private string FixedVectorFieldAddress(ClassInfo owner, FieldInfo field, StackEntry receiver)
    {
        _c.NoteNamedStructSymbol(_method, owner.CppStructName);
        string address = receiver.Kind == StackKind.Struct ? $"&({StructLValue(receiver)})" : NullCheckReceiver(receiver);
        // Form the offset from bytes, never an aligned field lvalue in a boxed payload.
        return $"((uint8_t*)({address}) + offsetof({owner.CppStructName}, {field.CppName}))";
    }

    private string? FixedVectorFieldRead(ClassInfo owner, FieldInfo field, StackEntry receiver)
    {
        if (receiver.Kind == StackKind.Struct || !NeedsFixedVectorFieldCopy(owner, field)) return null;
        string storage = LayoutFieldType(owner, field);
        string value = NewTemp(storage);
        Emit($"std::memcpy(&{value}, {FixedVectorFieldAddress(owner, field, receiver)}, sizeof({value}));");
        return value;
    }

    private bool TryFixedVectorFieldStore(ClassInfo owner, FieldInfo field, StackEntry receiver, StackEntry value)
    {
        if (!NeedsFixedVectorFieldCopy(owner, field)) return false;
        string storage = LayoutFieldType(owner, field);
        string copy = NewTemp(storage);
        Emit($"{copy} = {Cast(value, storage)};");
        Emit($"std::memcpy({FixedVectorFieldAddress(owner, field, receiver)}, &{copy}, sizeof({copy}));");
        return true;
    }
}
