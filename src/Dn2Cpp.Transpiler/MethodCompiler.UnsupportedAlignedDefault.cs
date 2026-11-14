namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string CompileUnsupportedAlignedDefaultBody()
    {
        if (!CoreIntrinsics.BrUnsupportedAlignedDefault.Matches(_method))
            throw new NotSupportedException("Unsupported aligned defaults require their exact closed CoreLib declaration and signature");
        Emit("dn2cpp_throw_platform_not_supported(\"Aligned ISimdVector memory operations for this unsupported vector element are not supported\");");
        return RenderWrapperBody("unsupported aligned-memory boundary");
    }
}
