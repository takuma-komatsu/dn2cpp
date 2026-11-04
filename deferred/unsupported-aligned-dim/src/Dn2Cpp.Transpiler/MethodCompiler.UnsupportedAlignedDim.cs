using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitUnsupportedAlignedDimEdge(MethodInfo callee)
    {
        var target = _c.TryResolveUnsupportedAlignedDimEdge(_method, _callSiteOffset, _callSiteToken,
            _constrainedHandle, _constrained, out bool terminal);
        if (target is null) return false;
        // Push has already spilled every argument, including caller exceptions.
        // This terminal replacement neither evaluates a pointee nor writes back.
        var arguments = PopArgs(callee, hasThis: false);
        _constrained = null;
        _constrainedHandle = default;
        if (terminal)
        {
            _byRefSlotFixups.Clear();
            EmitCallResult(target, DirectCall(target, Array.Empty<string>()));
            Emit("dn2cpp_fail(\"unsupported aligned vector guard returned\");");
            _unreachable = true;
        }
        else
            EmitCallResult(target, DirectCall(target, arguments));
        return true;
    }
}
