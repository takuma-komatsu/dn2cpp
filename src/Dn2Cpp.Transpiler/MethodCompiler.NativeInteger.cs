namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitNativeIntegerEndian(MethodInfo target)
    {
        if (!CoreIntrinsics.BrNativeIntegerEndian.Matches(target))
            throw new NotSupportedException("Native integer endian read requires its exact CoreLib declaration");
        var output = Pop();
        var unsigned = Pop();
        string span = SpanValue(Pop(), CppTypes.Of(target.Signature.ParameterTypes[0]));
        string storage = target.DeclaringClass.FullName == "System.IntPtr" ? "intptr_t" : "uintptr_t";
        string little = target.Name == BinaryIntegerEndianContract.LeafName(target.DeclaringClass.FullName, "TryReadLittleEndian")
            ? "true" : "false";
        string value = NewTemp(storage), ok = NewTemp("int32_t");
        Emit($"{ok} = dn2cpp_try_read_endian<{storage}, {little}>((const uint8_t*){span}.f__reference, "
            + $"{span}.f__length, (int32_t)({unsigned.Expr}), {value});");
        // Complete reads before the store: the source span may alias the output.
        Emit($"*dn2cpp_null_check((intptr_t*)({output.Expr})) = (intptr_t){value};");
        Push(StackKind.I4, "int32_t", ok);
    }
}
