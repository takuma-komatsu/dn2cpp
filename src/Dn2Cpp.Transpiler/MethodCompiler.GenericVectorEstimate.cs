namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string CompileVectorMagnitudeManagedGuard()
    {
        var guard = _c.ResolveVectorMagnitudeManagedGuard(_method);
        EmitManagedCall(guard, isCallvirt: false);
        Emit($"(void)({Pop().Expr});");
        Emit("dn2cpp_fail(\"unsupported vector Count guard returned\");");
        return RenderWrapperBody("unsupported vector magnitude guard");
    }

    private string CompileGenericVectorEstimateGuard()
    {
        var guard = _c.ResolveGenericVectorEstimateGuard(_method);
        EmitManagedCall(guard, isCallvirt: false);
        Emit($"(void)({Pop().Expr});");
        Emit("dn2cpp_fail(\"unsupported vector Count guard returned\");");
        return RenderWrapperBody("unsupported vector estimate guard");
    }
}
