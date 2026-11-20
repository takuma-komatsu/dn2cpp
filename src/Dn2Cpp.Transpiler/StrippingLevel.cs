namespace Dn2Cpp;

/// <summary>The retention policy, independent of managed DLL rewriting.</summary>
public enum StrippingLevel
{
    High,
    Medium,
}

internal static class DebuggerProxyRetention
{
    internal const string AttributeName = "System.Diagnostics.DebuggerTypeProxyAttribute";

    internal static bool IsProxyTypeConstructor(string declaringType, bool isCoreLib, string parameterType)
        => isCoreLib && declaringType == AttributeName && parameterType == "System.Type";
}
