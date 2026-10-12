namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal MethodInfo? FormatProviderGetFormat { get; private set; }

    internal static bool RuntimeNumericProviderConsumer(MethodInfo method) =>
        (method.DeclaringClass.FullName is "System.IFormattable"
            or "System.ISpanFormattable" or "System.IUtf8SpanFormattable"
            || method.DeclaringClass.FullName == "System.IConvertible" && method.Name is "ToString" or "ToType")
        && method.Signature.ParameterTypes.Length > 0
        && method.Signature.ParameterTypes[^1] is
            { Kind: TypeKind.Class, Class.FullName: "System.IFormatProvider" };

    /// <summary>Reaches the interface slot used when a numeric intrinsic consumes
    /// a managed provider; merely storing or returning that provider creates no edge.</summary>
    internal MethodInfo ReachFormatProviderGetFormat()
    {
        var provider = FindClassByFullName("System.IFormatProvider")
            ?? throw new InvalidOperationException("IFormatProvider is not loaded");
        EnsureCompleted(provider);
        var getFormat = provider.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "GetFormat"
            && m.Signature.ParameterTypes is
                [{ Kind: TypeKind.Class, Class.FullName: "System.Type" }])
            ?? throw new NotSupportedException("IFormatProvider.GetFormat(Type) has no supported slot");
        ReachUsedVirtual(getFormat);
        NoteReferencedType(provider);
        FormatProviderGetFormat = getFormat;
        return getFormat;
    }
}
