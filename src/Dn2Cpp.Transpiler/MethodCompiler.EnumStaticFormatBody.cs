namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    internal string CompileEnumStaticFormatBody()
    {
        if (!CoreIntrinsics.BrEnumStaticFormat.Matches(_method))
            throw new NotSupportedException("Enum static formatting requires an owned closed integer declaration");
        bool formatted = _method.Signature.ParameterTypes.Length == 3;
        string raw = formatted ? "a2" : "a1";
        string format = formatted ? "(char16_t)a1" : "u'G'";
        string underlying = TypeInfoExpr(_method.Context.MethodArgs[0])
            ?? throw new InvalidOperationException("Enum formatter underlying type has no native identity");
        Emit($"return dn2cpp_enum_format_raw_integer((Dn2CppType*)a0, {raw}, "
            + $"{underlying}, {format});");
        return RenderWrapperBody("dn2cpp enum integer raw formatter");
    }
}
