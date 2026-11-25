namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private Compilation.UnsupportedAlignedEdges? _unsupportedAlignedEdges;

    private bool TryEmitUnsupportedAlignedEdge(Instruction call)
    {
        var edges = _unsupportedAlignedEdges!;
        bool terminal = call.Offset == edges.UnsafeCall.Offset;
        if (!terminal && call.Offset != edges.AlignmentCall.Offset) return false;
        var expected = terminal ? edges.UnsafeCall : edges.AlignmentCall;
        if (call.OpCode != System.Reflection.Metadata.ILOpCode.Call || call.Token != expected.Token
            || System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(_constrainedHandle) != edges.ConstrainedToken
            || _constrained is not { Kind: TypeKind.Class, IsCanonPlaceholder: false } self || self.Class != edges.Vector)
            throw Compilation.UnsupportedAlignedEdge();
        _constrained = null;
        _constrainedHandle = default;
        if (terminal)
        {
            // The original arguments have already been evaluated and spilled.
            Pop();
            if (_method.Name == "StoreAligned") Pop();
            _byRefSlotFixups.Clear();
            PushI4Const(edges.Resource);
            EmitThrowHelperSink(edges.Sink.Name, edges.Sink.Signature);
            _unreachable = true;
        }
        else
            EmitCallResult(edges.Alignment, DirectCall(edges.Alignment, Array.Empty<string>()));
        return true;
    }

    private string CompileUnsupportedAlignedDefaultBody()
    {
        if (!CoreIntrinsics.BrUnsupportedAlignedDefault.Matches(_method))
            throw new NotSupportedException("Unsupported aligned defaults require their exact closed CoreLib declaration and signature");
        Emit("dn2cpp_throw_platform_not_supported(\"Aligned ISimdVector memory operations for this unsupported vector element are not supported\");");
        return RenderWrapperBody("unsupported aligned-memory boundary");
    }
}
