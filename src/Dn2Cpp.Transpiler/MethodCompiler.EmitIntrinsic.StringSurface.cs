using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private static bool IsStringComparison(TypeDesc type) =>
        type.Class is { FullName: "System.StringComparison", Module.AssemblyName: "System.Private.CoreLib" };

    private static bool IsStringRune(TypeDesc type) =>
        type.Class is { FullName: "System.Text.Rune", Module.AssemblyName: "System.Private.CoreLib" };

    private string StringRuneScalar(TypeDesc type, StackEntry rune)
    {
        var cls = type.Class!;
        Comp.EnsureCompleted(cls);
        var value = cls.Fields.FirstOrDefault(f => f.Name == "_value" && !f.IsStatic
            && f.Type.Primitive == PrimitiveTypeCode.UInt32)
            ?? throw new NotSupportedException("System.Text.Rune has no UInt32 _value field (layout drift)");
        return $"({rune.Expr}).{value.CppName}";
    }

    // Forward only to the exact CoreLib signature. The selected bodies use the
    // existing String entry points rather than addressing CoreCLR's inline buffer.
    private bool TryEmitStringManagedBody(string name, MethodSignature<TypeDesc> sig)
    {
        var parameters = sig.ParameterTypes;
        var body = Comp.ReachStringMethod(name, ps => ps.SequenceEqual(parameters));
        if (body is null)
            return false;
        var args = new string[parameters.Length + 1];
        for (int i = parameters.Length - 1; i >= 0; i--)
            args[i + 1] = Cast(Pop(), CppTypes.Of(parameters[i]));
        args[0] = VirtualReceiver(Pop());
        Push(CppTypes.KindOf(sig.ReturnType), CppTypes.Of(sig.ReturnType), DirectCall(body, args));
        return true;
    }

    private bool TryEmitStringSurface(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (name)
        {
            case "Compare" when !sig.Header.IsInstance && ps is [{ IsString: true }, { IsString: true }]:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_str_compare({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}, 4)");
                return true;
            }
            case "Copy" when !sig.Header.IsInstance && ps is [{ IsString: true }]:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_copy({Cast(Pop(), "Dn2CppString*")})");
                return true;
            case "GetTypeCode" when sig.Header.IsInstance && ps.Length == 0:
            {
                string receiver = VirtualReceiver(Pop());
                Emit($"(void)({receiver});");
                Push(StackKind.I4, "int32_t", "18"); // TypeCode.String
                return true;
            }
            case "ToString" when sig.Header.IsInstance && ps.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", VirtualReceiver(Pop()));
                return true;
            case "Equals" when sig.Header.IsInstance && ps is [{ IsObject: true }]:
                return TryEmitStringManagedBody(name, sig);
            case "EqualsHelper" when !sig.Header.IsInstance && ps is [{ IsString: true }, { IsString: true }]:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_string_equals({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")})");
                return true;
            }
            case "StartsWith" or "EndsWith" when ps is [{ IsString: true }, { Primitive: PrimitiveTypeCode.Boolean },
                { Class.FullName: "System.Globalization.CultureInfo" }]:
            {
                Pop(); // The existing invariant string policy disregards the supplied culture.
                var ignoreCase = Pop();
                var value = Pop();
                var self = Pop();
                string op = name == "StartsWith" ? "startswith" : "endswith";
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_{op}_cmp({Cast(self, "Dn2CppString*")}, {Cast(value, "Dn2CppString*")}, ({ignoreCase.Expr}) ? 5 : 4)");
                return true;
            }
            case "StartsWith" or "EndsWith"
                when (ps.Length == 1 && IsStringRune(ps[0]))
                    || (ps.Length == 2 && IsStringComparison(ps[1])
                        && (ps[0].Primitive == PrimitiveTypeCode.Char || IsStringRune(ps[0]))):
            {
                string comparison = ps.Length == 2 ? Pop().Expr : "4";
                var value = Pop();
                string scalar = IsStringRune(ps[0]) ? StringRuneScalar(ps[0], value) : $"(uint16_t)({value.Expr})";
                var self = Pop();
                string suffix = name == "EndsWith" ? "true" : "false";
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_scalar_at_edge({Cast(self, "Dn2CppString*")}, {scalar}, {comparison}, {suffix})");
                return true;
            }
            case "IndexOf" or "LastIndexOf"
                when ps is [{ IsString: true }, { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }, var comp]
                    && IsStringComparison(comp):
            {
                var comparison = Pop();
                var count = Pop();
                var start = Pop();
                var value = Pop();
                var self = Pop();
                string op = name == "IndexOf" ? "indexof" : "lastindexof";
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_{op}_str_range({Cast(self, "Dn2CppString*")}, {Cast(value, "Dn2CppString*")}, {start.Expr}, {count.Expr}, {comparison.Expr})");
                return true;
            }
            case "IndexOf" or "LastIndexOf"
                when ps is [var rune, { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }, var comp]
                    && IsStringRune(rune) && IsStringComparison(comp):
            {
                var comparison = Pop();
                var count = Pop();
                var start = Pop();
                string scalar = StringRuneScalar(rune, Pop());
                var self = Pop();
                string reverse = name == "LastIndexOf" ? "true" : "false";
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_rune_search({Cast(self, "Dn2CppString*")}, {scalar}, {start.Expr}, {count.Expr}, {comparison.Expr}, {reverse})");
                return true;
            }
            case "IndexOf" or "LastIndexOf" or "Contains" or "Trim" or "TrimStart" or "TrimEnd"
                when ps.Length > 0 && IsStringRune(ps[0]):
                return TryEmitStringManagedBody(name, sig);
            case "IndexOf" or "LastIndexOf"
                when ps.Length is >= 2 and <= 4 && ps[0].Primitive == PrimitiveTypeCode.Char && IsStringComparison(ps[^1])
                    && (name == "LastIndexOf" || ps.Length >= 3):
                return TryEmitStringManagedBody(name, sig);
            case "IndexOfCharOrdinalIgnoreCase" or "LastIndexOfCharOrdinalIgnoreCase"
                when ps is [{ Primitive: PrimitiveTypeCode.Char },
                    { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var value = Pop();
                var self = Pop();
                string reverse = name == "LastIndexOfCharOrdinalIgnoreCase" ? "true" : "false";
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_char_ignorecase_range({Cast(self, "Dn2CppString*")}, (char16_t)({value.Expr}), {start.Expr}, {count.Expr}, {reverse})");
                return true;
            }
            case "Trim" or "TrimStart" or "TrimEnd" when ps is [var span] && IsReadOnlySpanChar(span):
            {
                string value = PopSpanTemp(span);
                var self = Pop();
                int mode = name == "TrimStart" ? 1 : name == "TrimEnd" ? 2 : 0;
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_trim_set({Cast(self, "Dn2CppString*")}, {value}.f__reference, {value}.f__length, {mode})");
                return true;
            }
            case "Replace" when ps is [var before, var after] && IsStringRune(before) && IsStringRune(after):
            {
                string replacement = StringRuneScalar(after, Pop());
                string oldValue = StringRuneScalar(before, Pop());
                var self = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_replace_rune({Cast(self, "Dn2CppString*")}, {oldValue}, {replacement})");
                return true;
            }
            case "Join" when !sig.Header.IsInstance && ps is [{ Primitive: PrimitiveTypeCode.Char },
                { Kind: TypeKind.SZArray, Element.IsString: true }, { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var arr = Pop();
                var sep = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_join_ref_range(dn2cpp_char_to_string((char16_t)({sep.Expr})), (Dn2CppArrayRef*)({arr.Expr}), {start.Expr}, {count.Expr})");
                return true;
            }
            case "ReplaceLineEndings" when sig.Header.IsInstance && (ps.Length == 0 || ps is [{ IsString: true }]):
            {
                string replacement = ps.Length == 0 ? "nullptr" : Cast(Pop(), "Dn2CppString*");
                var self = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_replace_line_endings({Cast(self, "Dn2CppString*")}, {replacement}, {(ps.Length == 0 ? "true" : "false")})");
                return true;
            }
            case "ToUpperOrdinal" or "ToLowerOrdinal" when sig.Header.IsInstance && ps.Length == 0:
                return TryEmitStringManagedBody(name, sig);
            case "ChangeCaseOrdinal" when sig.Header.IsInstance && ps is [{ Primitive: PrimitiveTypeCode.Boolean }]:
                return TryEmitStringManagedBody(name, sig);
            default:
                return false;
        }
    }
}
