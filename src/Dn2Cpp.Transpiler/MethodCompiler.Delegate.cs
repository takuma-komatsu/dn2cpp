namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string? CompileDelegateCallableBody() =>
        SynthesizeWrapperBody(ReceiverCppType(_method.DeclaringClass), () =>
        {
            string name = _method.Name;
            var signature = _method.Signature;
            if (name == ".ctor")
            {
                var memberName = Pop();
                var target = Pop();
                var receiver = Pop();
                bool staticForm = signature.ParameterTypes[0].Class?.FullName == "System.Type";
                NoteValueTypeRows();
                Emit($"dn2cpp_delegate_initialize_named({Cast(receiver, "Dn2CppObject*")}, "
                    + $"{(staticForm ? "nullptr" : Cast(target, "Dn2CppObject*"))}, "
                    + $"{(staticForm ? Cast(target, "Dn2CppType*") : "nullptr")}, "
                    + $"{Cast(memberName, "Dn2CppString*")}, {(staticForm ? 1 : 0)});");
            }
            else if (DelegateCallableBodyContract.VmConstructorArity(name) != 0)
            {
                foreach (var _ in signature.ParameterTypes) Pop();
                Pop();
                // Refuse before interpreting CoreCLR PCODE, shuffle or loader state.
                Emit($"dn2cpp_throw_platform_not_supported(\"System.Delegate.{name} is a CoreCLR-owned delegate initialization stub. Use normal delegate construction or Delegate.CreateDelegate instead.\");");
            }
            else if (name == "Equals")
            {
                var other = Pop();
                var self = Pop();
                // The IL returns false for a null argument before reading this.
                // Public operator helpers are null-safe, but an instance body
                // with a non-null argument must fault a null receiver.
                string argument = Cast(other, "Dn2CppObject*");
                Push(StackKind.I4, "int32_t", $"({argument} == nullptr ? 0 : "
                    + $"dn2cpp_delegate_equal(dn2cpp_null_check({Cast(self, "Dn2CppObject*")}), {argument}))");
            }
            else if (name == "GetHashCode")
            {
                var self = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_delegate_hash(dn2cpp_null_check({Cast(self, "Dn2CppObject*")}))");
            }
            else if (name == "NewMulticastDelegate")
            {
                foreach (var _ in signature.ParameterTypes) Pop();
                Pop();
                // The entries are CoreCLR invocation-list storage, which the
                // native chain replaces; refuse before reading the array.
                Emit("dn2cpp_throw_platform_not_supported(\"System.Delegate.NewMulticastDelegate takes CoreCLR-owned invocation-list storage. Use Delegate.Combine instead.\");");
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
            }
            else if (name is "CombineImpl" or "RemoveImpl")
            {
                var value = Pop();
                var self = Pop();
                string helper = name == "CombineImpl" ? "dn2cpp_delegate_combine" : "dn2cpp_delegate_remove_impl";
                string argument = Cast(value, "Dn2CppObject*");
                string receiver = Cast(self, "Dn2CppObject*");
                Push(StackKind.Ref, "Dn2CppObject*", $"({argument} == nullptr ? {receiver} : "
                    + $"{helper}(dn2cpp_null_check({receiver}), {argument}))");
            }
            else if (name == "TryGetAt")
            {
                var index = Pop();
                var self = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_delegate_try_get_at({DelegateReceiver(self)}, {Cast(index, "int32_t")})");
            }
            else
            {
                // Reuse public projections, including Method's row retention.
                string projection = name == "GetMethodImpl" ? "get_Method" : name;
                Top = Top with { Expr = $"dn2cpp_null_check({Top.Expr})" };
                if (!TryEmitStringsIntrinsic("System.Delegate", projection, signature)) return false;
            }
            if (!signature.ReturnType.IsVoid) Top = Top with { StaticType = signature.ReturnType };
            return true;
        }, "uniform delegate callable body");
}
