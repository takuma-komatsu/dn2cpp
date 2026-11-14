using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string EmitCancellationTokenCtorExpr(MethodSignature<TypeDesc> signature)
    {
        int kind = CoreIntrinsics.CancellationTokenConstructorKind(signature);
        if (kind == 0)
            throw new NotSupportedException("CancellationToken constructor requires Boolean or its exact CoreLib CancellationTokenSource signature");
        var argument = Pop();
        EmitCanceledExcRegistration();
        // The internal source constructor preserves identity, including a null source.
        return kind == 1
            ? $"Dn2CppCancelToken{{ ({argument.Expr}) ? dn2cpp_cts_canceled() : nullptr }}"
            : $"Dn2CppCancelToken{{ (Dn2CppCancelSource*)({argument.Expr}) }}";
    }
}
