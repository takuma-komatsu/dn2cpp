namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string CompileGuardedVectorAs()
    {
        foreach (var guard in _c.ResolveGuardedVectorAsGuards(_method))
            EmitManagedCall(guard, isCallvirt: false);
        Emit("dn2cpp_fail(\"unsupported vector As guard returned\");");
        return RenderWrapperBody("unsupported vector As guard");
    }
}
