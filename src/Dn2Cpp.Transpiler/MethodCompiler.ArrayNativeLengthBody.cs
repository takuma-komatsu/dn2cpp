namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    internal string CompileArrayNativeLengthBody()
    {
        if (!CoreIntrinsics.BrArrayNativeLength.Matches(_method))
            throw new NotSupportedException("System.Array.get_NativeLength requires its exact owned declaration");
        // Native arrays carry their actual SZ/MD extent; the CLR RawArrayData layout is never read.
        Emit("return (uintptr_t)(uint32_t)dn2cpp_array_length_dyn((Dn2CppObject*)a0);");
        return RenderWrapperBody("dn2cpp System.Array.get_NativeLength");
    }
}
