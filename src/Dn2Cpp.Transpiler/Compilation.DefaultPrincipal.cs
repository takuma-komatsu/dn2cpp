namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo ReachDefaultPrincipalFactory()
    {
        var module = Modules.SingleOrDefault(m => m.AssemblyName == DefaultPrincipalContract.TargetAssembly)
            ?? throw new NotSupportedException("default principal requires the System.Security.Claims implementation assembly");
        var handle = DefaultPrincipalContract.FindTarget(module.Reader);
        if (handle.IsNil)
            throw new NotSupportedException("default principal requires the exact managed GenericPrincipal.GetDefaultInstance factory");
        GetClass(module, module.Reader.GetMethodDefinition(handle).GetDeclaringType()).EnsureMembers();
        var method = GetMethod(module, handle);
        ReachIntrinsicTypeMethod(method);
        DrainReachability();
        return method;
    }
}
