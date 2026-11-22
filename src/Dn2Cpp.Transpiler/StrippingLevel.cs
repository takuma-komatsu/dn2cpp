namespace Dn2Cpp;

/// <summary>The retention policy, independent of managed DLL rewriting.</summary>
public enum StrippingLevel
{
    High,
    Medium,
    Low,
}

internal static class StrippingLevelPolicy
{
    internal static bool UsesTypeWideRetention(this StrippingLevel level)
        => level is StrippingLevel.Medium or StrippingLevel.Low;

    internal static string OptionName(this StrippingLevel level) => level switch
    {
        StrippingLevel.High => "high",
        StrippingLevel.Medium => "medium",
        StrippingLevel.Low => "low",
        _ => throw new NotSupportedException("Unknown stripping level: " + level),
    };
}

internal static class DebuggerProxyRetention
{
    internal const string AttributeName = "System.Diagnostics.DebuggerTypeProxyAttribute";

    internal static bool IsProxyTypeConstructor(string declaringType, bool isCoreLib, string parameterType)
        => isCoreLib && declaringType == AttributeName && parameterType == "System.Type";
}
