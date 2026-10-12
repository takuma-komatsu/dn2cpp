using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitRuntimeHeldIntrinsic(string declaringType, string name, MethodSignature<TypeDesc> signature)
    {
        if (declaringType != "System.Threading.Tasks.Task" || signature.ParameterTypes.Length != 0)
            return false;
        switch (name)
        {
            case "get_AsyncState":
            {
                var receiver = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"((Dn2CppTask*)({receiver.Expr}))->asyncState");
                return true;
            }
            case "get_AsyncWaitHandle":
            {
                var receiver = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_task_async_wait_handle((Dn2CppTask*)({receiver.Expr}))");
                return true;
            }
            case "get_CompletedSynchronously":
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
        }
        return false;
    }
}
