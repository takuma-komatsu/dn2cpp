namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    internal string CompileStringRawByteReferenceBody()
    {
        if (!CoreIntrinsics.BrStringRawByteReference.Matches(_method))
            throw new NotSupportedException("String byte reference requires its exact owned declaration");
        return SynthesizeWrapperBody(ReceiverCppType(_method.DeclaringClass), () =>
        {
            NullCheckReceiverUnder(0);
            var value = Pop();
            Push(StackKind.Ptr, "uint8_t*", $"(uint8_t*)({Cast(value, "Dn2CppString*")})->chars");
            return true;
        }, "native String byte-buffer reference")
            ?? throw new InvalidOperationException("String byte-reference body must return one reference");
    }
}
