using System;

namespace Dn2Cpp.Runtime
{
    // Internal copy of Dn2Cpp.Runtime.HotPathAttribute — the transpiler matches
    // it by full name only (see src/Dn2Cpp.Runtime/HotPath.cs), so this bench
    // needs no reference to Dn2Cpp.Runtime.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class HotPathAttribute : Attribute
    {
        public bool SkipBoundsChecks { get; set; }
        public bool NoAlloc { get; set; }
        public bool FastMath { get; set; }
        public bool NoAlias { get; set; }
    }
}
