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
                Emit($"dn2cpp_throw_platform_not_supported(\"System.MulticastDelegate.{name} is a CoreCLR-owned delegate initialization stub. Use normal delegate construction or Delegate.CreateDelegate instead.\");");
            }
            else if (name == "Equals")
            {
                var other = Pop();
                var self = Pop();
                string helper = _method.DeclaringClass.FullName == "System.Delegate"
                    ? "dn2cpp_delegate_base_equal" : "dn2cpp_delegate_equal";
                // The IL returns false for a null argument before reading this.
                // Public operator helpers are null-safe, but an instance body
                // with a non-null argument must fault a null receiver.
                string argument = Cast(other, "Dn2CppObject*");
                Push(StackKind.I4, "int32_t", $"({argument} == nullptr ? 0 : "
                    + $"{helper}(dn2cpp_null_check({Cast(self, "Dn2CppObject*")}), {argument}))");
            }
            else if (name == "GetHashCode")
            {
                var self = Pop();
                string helper = _method.DeclaringClass.FullName == "System.Delegate"
                    ? "dn2cpp_delegate_base_hash" : "dn2cpp_delegate_hash";
                Push(StackKind.I4, "int32_t", $"{helper}(dn2cpp_null_check({Cast(self, "Dn2CppObject*")}))");
            }
            else if (name == "NewMulticastDelegate")
            {
                // The flag selects a CoreCLR invoke stub; native delegates use one layout.
                if (signature.ParameterTypes.Length == 3) Pop();
                var count = Pop();
                var entries = Pop();
                var self = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_delegate_from_invocation_prefix({DelegateReceiver(self)}, {Cast(entries, "Dn2CppArrayRef*")}, {Cast(count, "int32_t")})");
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
                string projection = name switch { "GetTarget" => "get_Target", "GetMethodImpl" => "get_Method", _ => name };
                Top = Top with { Expr = $"dn2cpp_null_check({Top.Expr})" };
                if (!TryEmitStringsIntrinsic("System.Delegate", projection, signature)) return false;
            }
            if (!signature.ReturnType.IsVoid) Top = Top with { StaticType = signature.ReturnType };
            return true;
        }, "uniform delegate callable body");
}
