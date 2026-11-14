namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    internal string CompileGCHandleCheckUninitializedBody()
    {
        if (!CoreIntrinsics.BrGCHandleCheckUninitialized.Matches(_method))
            throw new NotSupportedException("System.Runtime.InteropServices.GCHandle.CheckUninitialized requires its exact owned declaration");
        // Only the uninitialized-token fault is observable for valid native weak handles; no CLR cell is dereferenced.
        Emit("if (a0 == 0) dn2cpp_throw_null_reference();");
        Emit("return;");
        return RenderWrapperBody("dn2cpp System.Runtime.InteropServices.GCHandle.CheckUninitialized");
    }
}
