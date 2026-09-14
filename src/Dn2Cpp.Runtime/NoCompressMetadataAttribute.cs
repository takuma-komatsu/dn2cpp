using System;

namespace Dn2Cpp.Runtime;

[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Interface
    | AttributeTargets.Enum
    | AttributeTargets.Delegate,
    AllowMultiple = false,
    Inherited = true)]
public class NoCompressMetadataAttribute : Attribute
{
}
