namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo ResolveUInt128CheckedInterface(MethodInfo method, ClassInfo target)
    {
        if (!CoreIntrinsics.IsUInt128CheckedInterface(method)
            || method.DeclaringClass.Context.TypeArgs[0].Class != target)
            throw new NotSupportedException("UInt128.CreateChecked requires its exact constrained interface signature");
        foreach (var handle in target.Module.Reader.GetTypeDefinition(target.Handle).GetMethods())
        {
            var definition = target.Module.Reader.GetMethodDefinition(handle);
            if (!target.Module.Reader.StringComparer.Equals(definition.Name, "CreateChecked")) continue;
            var concrete = InstantiateMethodOnClass(target, target.Module, handle, method.Context.MethodArgs);
            if (CoreIntrinsics.IsUInt128Checked(concrete)) return concrete;
        }
        throw new NotSupportedException("UInt128.CreateChecked concrete integer factory is unavailable");
    }
}
