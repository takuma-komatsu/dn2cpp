using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitLifetimeDispose(string owner, string name, MethodSignature<TypeDesc> sig)
    {
        if (!CoreIntrinsics.BrLifetimeDispose.Matches(owner, name))
            return false;
        bool baseCleanup = name == "Dispose" && sig.ParameterTypes is
            [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }];
        if (!sig.Header.IsInstance || !sig.ReturnType.IsVoid
            || !baseCleanup && sig.ParameterTypes.Length != 0)
            throw new NotSupportedException($"{owner}.{name}: unsupported lifetime signature");
        if (baseCleanup)
        {
            Pop(); // disposing
            var self = Pop();
            if (owner == "System.Threading.WaitHandle")
                Emit($"dn2cpp_waithandle_close((Dn2CppObject*)({self.Expr}));");
            return true;
        }
        var slot = name == "Close" && _callIsVirtual
            ? _c.ReachLifetimeClose(owner) : _c.ReachLifetimeDisposeCore(owner);
        _c.DrainReachability();
        if (slot.VtableSlot < 0)
            throw new NotSupportedException($"{owner}.{slot.Name}: lifetime slot has no vtable index");
        string receiver = NewTemp("Dn2CppObject*");
        Emit($"{receiver} = (Dn2CppObject*)dn2cpp_null_check({Pop().Expr});");
        string arguments = name == "Close" && _callIsVirtual ? "" : ", 1";
        string receiverType = ReceiverCppType(slot.DeclaringClass);
        string pointerType = $"void (*)({receiverType}"
            + (slot.Signature.ParameterTypes.Length == 0 ? ")" : $", {CppTypes.Of(slot.Signature.ParameterTypes[0])})");
        Emit($"if ({receiver}->type->vtable != nullptr)");
        Emit($"    (({pointerType})({receiver}->type->vtable[{slot.VtableSlot}]))"
            + $"(({receiverType}){receiver}{arguments});");
        if (owner == "System.Threading.WaitHandle")
        {
            Emit("else");
            Emit($"    dn2cpp_waithandle_close({receiver});");
        }
        if (name != "Close" || !_callIsVirtual)
            Emit($"dn2cpp_gc_suppress_finalize({receiver});");
        return true;
    }
}
