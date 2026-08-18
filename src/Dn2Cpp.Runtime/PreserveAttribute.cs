using System;

namespace Dn2Cpp.Scripting;

[AttributeUsage(
    AttributeTargets.Assembly
    | AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Enum
    | AttributeTargets.Interface
    | AttributeTargets.Delegate
    | AttributeTargets.Constructor
    | AttributeTargets.Method
    | AttributeTargets.Property
    | AttributeTargets.Field
    | AttributeTargets.Event,
    AllowMultiple = false,
    Inherited = false)]
public class PreserveAttribute : Attribute
{
}
