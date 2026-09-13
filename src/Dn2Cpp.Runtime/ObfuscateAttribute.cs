using System;

namespace Dn2Cpp.Runtime;

/// <summary>Selects the reachable implementation for DeClang when --obfuscate is enabled.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class ObfuscateAttribute : Attribute
{
}
