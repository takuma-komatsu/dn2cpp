namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private void EmitSemaphoreWaiter(MethodInfo method)
    {
        var node = _c.RequireSemaphoreWaiterNode(method);
        var previous = node.Fields.First(f => !f.IsStatic && f.Name == "Prev");
        var next = node.Fields.First(f => !f.IsStatic && f.Name == "Next");
        string offsets = $"offsetof({node.CppStructName}, {previous.CppName}), offsetof({node.CppStructName}, {next.CppName})";
        if (method.Name == "RemoveAsyncWaiter")
        {
            var waiter = Pop();
            var receiver = Pop();
            Push(StackKind.I4, "int32_t",
                $"dn2cpp_semaphore_remove_async_waiter((Dn2CppObject*)({receiver.Expr}), (Dn2CppTask*)({waiter.Expr}), {offsets})");
            return;
        }
        var self = Pop();
        Push(StackKind.Ref, CppTypes.Of(method.Signature.ReturnType),
            $"({node.CppStructName}*)dn2cpp_semaphore_create_async_waiter((Dn2CppObject*)({self.Expr}), "
            + $"&{node.CppTypeInfoName}, sizeof({node.CppStructName}), {offsets})");
    }
}
