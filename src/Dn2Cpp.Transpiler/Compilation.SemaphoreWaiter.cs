namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal ClassInfo RequireSemaphoreWaiterNode(MethodInfo method)
    {
        method.EnsureSignature();
        var node = (method.Name == "CreateAndAddAsyncWaiter"
            ? method.Signature.ReturnType : method.Signature.ParameterTypes[0]).Class
            ?? throw new InvalidOperationException("Semaphore waiter declaration has no node type");
        NoteReferencedType(node);
        EnsureCompleted(node);
        if (method.Name == "CreateAndAddAsyncWaiter")
        {
            ReachAllocatedType(node);
            DrainReachability();
        }
        return node.EnsureMembers();
    }
}
