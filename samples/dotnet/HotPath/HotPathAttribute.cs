using System;

namespace Dn2Cpp.Runtime
{
    // Internal copy of Dn2Cpp.Runtime.HotPathAttribute. The transpiler matches
    // the attribute by full name only (the IsExternalInit convention — see
    // src/Dn2Cpp.Runtime/HotPath.cs), so the sample needs no reference to
    // Dn2Cpp.Runtime; under real .NET (the gate's oracle side) the attribute is
    // inert either way.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class HotPathAttribute : Attribute
    {
        // Opt-in knob (see src/Dn2Cpp.Runtime/HotPath.cs): elide bounds checks
        // in the marked body; out-of-range becomes UB by the caller's contract.
        public bool SkipBoundsChecks { get; set; }

        // Opt-in verifying knob (see src/Dn2Cpp.Runtime/HotPath.cs): the marked
        // method's direct-call closure must allocate nothing and dispatch nothing
        // dynamically, or the transpile fails naming the offender and chain.
        public bool NoAlloc { get; set; }

        // Opt-in knob (see src/Dn2Cpp.Runtime/HotPath.cs): route the body into
        // generated_hot_fast.cpp, compiled with relaxed floating-point
        // semantics; results may differ from IEEE-exact .NET.
        public bool FastMath { get; set; }

        // Opt-in knob (see src/Dn2Cpp.Runtime/HotPath.cs): the body's array,
        // byref and pointer parameters are emitted __restrict; overlapping
        // arguments become UB by the caller's contract.
        public bool NoAlias { get; set; }
    }
}
