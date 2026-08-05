namespace Dn2Cpp;

/// <summary>Translates specific managed calls into target-specific C++ directly
/// at the call site, bypassing the normal managed-call path. Consulted by
/// <see cref="MethodCompiler"/> before its default handling, so the core
/// compiler carries no knowledge of any particular target API.</summary>
internal interface ICallIntrinsics
{
    /// <summary>Attempts to emit <paramref name="callee"/> as an intrinsic.
    /// Returns true when fully handled (arguments consumed, any result pushed);
    /// false to fall through to the normal managed call.</summary>
    bool TryEmitCall(MethodCompiler mc, MethodInfo callee, bool isCallvirt);

    /// <summary>Attempts to emit a `newobj` (object construction) as an intrinsic.
    /// Returns true when fully handled (ctor args consumed, managed allocation +
    /// any target-specific construction done, result pushed); false to fall
    /// through to the normal generic newobj lowering.</summary>
    bool TryEmitNewobj(MethodCompiler mc, MethodInfo ctor);
}
