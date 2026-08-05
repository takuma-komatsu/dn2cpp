using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitCharIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {

            // ---- System.Char static classification/casing (full BMP) ----
            // char is an int32 code unit on the stack; the static helpers take one.
            // GetUnicodeCategory(char): the full BMP category from the generated
            // two-level table (dn2cpp_unicode_category.cpp) — exact vs .NET for
            // every BMP code point. CharUnicodeInfo.GetUnicodeCategory(char) is
            // the same function one layer down.
            case ("System.Char", "GetUnicodeCategory")
                or ("System.Globalization.CharUnicodeInfo", "GetUnicodeCategory")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_char_unicode_category((char16_t)({c.Expr}))");
                return true;
            }
            // Category-backed classification (the generated full-BMP
            // dn2cpp_char_unicode_category table), exact vs .NET for every BMP
            // code point. IsLetter is Lu/Ll/Lt/Lm/Lo (0..4), IsDigit is Nd (8)
            // only, and IsWhiteSpace the .NET Zs/Zl/Zp + U+0009-000D + U+0085
            // definition (dn2cpp_char_is_whitespace).
            case ("System.Char", "IsDigit") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(dn2cpp_char_unicode_category((char16_t)({c.Expr})) == 8 ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsLetter") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(dn2cpp_char_unicode_category((char16_t)({c.Expr})) <= 4 ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsLetterOrDigit") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"((dn2cpp_char_unicode_category((char16_t)({c.Expr})) <= 4 || dn2cpp_char_unicode_category((char16_t)({c.Expr})) == 8) ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsWhiteSpace") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_char_is_whitespace((char16_t)({c.Expr}))");
                return true;
            }
            case ("System.Char", "IsUpper") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(dn2cpp_char_unicode_category((char16_t)({c.Expr})) == 0 ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsLower") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(dn2cpp_char_unicode_category((char16_t)({c.Expr})) == 1 ? 1 : 0)");
                return true;
            }
            // UTF-16 surrogate range predicates. IsSurrogate is the full surrogate range
            // U+D800..U+DFFF; IsHighSurrogate the lead half U+D800..U+DBFF, IsLowSurrogate
            // the trail half U+DC00..U+DFFF. All three are mapped together because the BCL
            // string-encoding fallback paths call them in sequence, so mapping only one
            // would move the gap rather than close it.
            case ("System.Char", "IsSurrogate") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(({c.Expr}) >= 0xD800 && ({c.Expr}) <= 0xDFFF ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsHighSurrogate") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(({c.Expr}) >= 0xD800 && ({c.Expr}) <= 0xDBFF ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsLowSurrogate") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(({c.Expr}) >= 0xDC00 && ({c.Expr}) <= 0xDFFF ? 1 : 0)");
                return true;
            }
            // The (string, int) surrogate predicate overloads — the same range
            // tests against the indexed code unit (bounds-checked by
            // dn2cpp_string_get_char). The three siblings share one shape, so all
            // are mapped together.
            case ("System.Char", "IsSurrogate" or "IsHighSurrogate" or "IsLowSurrogate")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }, _]:
            {
                var idx = Pop();
                var str = Pop();
                string cu = NewTemp("int32_t");
                Emit($"{cu} = dn2cpp_string_get_char({Cast(str, "Dn2CppString*")}, {idx.Expr});");
                (string lo, string hi) = name switch
                {
                    "IsHighSurrogate" => ("0xD800", "0xDBFF"),
                    "IsLowSurrogate" => ("0xDC00", "0xDFFF"),
                    _ => ("0xD800", "0xDFFF"),
                };
                Push(StackKind.I4, "int32_t", $"({cu} >= {lo} && {cu} <= {hi} ? 1 : 0)");
                return true;
            }
            // The (string, int) classification overloads — the same category-backed
            // tests as the (char) forms above (IsDigit == Nd(8), IsUpper == Lu(0),
            // IsWhiteSpace the dn2cpp_char_is_whitespace definition) applied to the
            // indexed code unit. The index is bounds-checked by dn2cpp_string_get_char,
            // matching .NET's ArgumentOutOfRangeException. Matched on the (String, _)
            // shape so the single-char forms (Length == 1) keep their own arms.
            case ("System.Char", "IsUpper" or "IsWhiteSpace" or "IsDigit")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }, _]:
            {
                var idx = Pop();
                var str = Pop();
                string cu = NewTemp("int32_t");
                Emit($"{cu} = dn2cpp_string_get_char({Cast(str, "Dn2CppString*")}, {idx.Expr});");
                string expr = name switch
                {
                    "IsDigit" => $"(dn2cpp_char_unicode_category((char16_t){cu}) == 8 ? 1 : 0)",
                    "IsUpper" => $"(dn2cpp_char_unicode_category((char16_t){cu}) == 0 ? 1 : 0)",
                    _ => $"dn2cpp_char_is_whitespace((char16_t){cu})",
                };
                Push(StackKind.I4, "int32_t", expr);
                return true;
            }
            // The surrogate-pair (char high, char low) overloads — reached by the BCL
            // encoder fallback (EncoderExceptionFallbackBuffer.Fallback formats a bad
            // char's scalar value). IsSurrogatePair tests the lead/trail ranges;
            // ConvertToUtf32 combines the pair into its UTF-32 scalar.
            case ("System.Char", "IsSurrogatePair")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }, _]:
            {
                var low = Pop();
                var high = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((({high.Expr}) >= 0xD800 && ({high.Expr}) <= 0xDBFF && ({low.Expr}) >= 0xDC00 && ({low.Expr}) <= 0xDFFF) ? 1 : 0)");
                return true;
            }
            case ("System.Char", "ConvertToUtf32")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }, _]:
            {
                var low = Pop();
                var high = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((((int32_t)({high.Expr}) - 0xD800) * 0x400) + ((int32_t)({low.Expr}) - 0xDC00) + 0x10000)");
                return true;
            }
            // The (string, int) pair walkers. Index bounds ride the checked
            // dn2cpp_string_get_char. DECLARED DIVERGENCE: ConvertToUtf32 returns the raw
            // code unit where real .NET would throw on an unpaired surrogate (its BCL
            // callers test IsSurrogatePair first, so the throw path is unobserved); a
            // proper pair combines exactly as in .NET.
            case ("System.Char", "IsSurrogatePair")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }, _]:
            {
                var idx = Pop();
                var str = Pop();
                string s = NewTemp("Dn2CppString*");
                Emit($"{s} = {Cast(str, "Dn2CppString*")};");
                string i = NewTemp("int32_t");
                Emit($"{i} = {idx.Expr};");
                string hi2 = NewTemp("int32_t");
                Emit($"{hi2} = dn2cpp_string_get_char({s}, {i});");
                Push(StackKind.I4, "int32_t",
                    $"(({hi2} >= 0xD800 && {hi2} <= 0xDBFF && {i} + 1 < {s}->length"
                    + $" && dn2cpp_string_get_char({s}, {i} + 1) >= 0xDC00"
                    + $" && dn2cpp_string_get_char({s}, {i} + 1) <= 0xDFFF) ? 1 : 0)");
                return true;
            }
            case ("System.Char", "ConvertToUtf32")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }, _]:
            {
                var idx = Pop();
                var str = Pop();
                string s = NewTemp("Dn2CppString*");
                Emit($"{s} = {Cast(str, "Dn2CppString*")};");
                string i = NewTemp("int32_t");
                Emit($"{i} = {idx.Expr};");
                string cu = NewTemp("int32_t");
                Emit($"{cu} = dn2cpp_string_get_char({s}, {i});");
                string r = NewTemp("int32_t");
                Emit($"{r} = ({cu} >= 0xD800 && {cu} <= 0xDBFF && {i} + 1 < {s}->length"
                    + $" && dn2cpp_string_get_char({s}, {i} + 1) >= 0xDC00"
                    + $" && dn2cpp_string_get_char({s}, {i} + 1) <= 0xDFFF)"
                    + $" ? ((({cu} - 0xD800) * 0x400) + ((int32_t)dn2cpp_string_get_char({s}, {i} + 1) - 0xDC00) + 0x10000)"
                    + $" : {cu};");
                Push(StackKind.I4, "int32_t", r);
                return true;
            }
            // ConvertFromUtf32 — the scalar back to a 1- or 2-code-unit string.
            // DECLARED DIVERGENCE: the .NET range validation (surrogate scalars,
            // > U+10FFFF) is skipped — the BCL callers range-check first.
            case ("System.Char", "ConvertFromUtf32") when sig.ParameterTypes.Length == 1:
            {
                var cp = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_from_utf32({cp.Expr})");
                return true;
            }
            // Remaining classification (category-backed, full BMP).
            case ("System.Char", "IsControl") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_char_is_control((char16_t)({c.Expr}))");
                return true;
            }
            case ("System.Char", "IsPunctuation") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_char_is_punctuation((char16_t)({c.Expr}))");
                return true;
            }
            case ("System.Char", "IsSymbol") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_char_is_symbol((char16_t)({c.Expr}))");
                return true;
            }
            case ("System.Char", "IsSeparator") when sig.ParameterTypes.Length == 1:
            {
                // The separator categories Zs/Zl/Zp (11..13).
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"((dn2cpp_char_unicode_category((char16_t)({c.Expr})) >= 11 && dn2cpp_char_unicode_category((char16_t)({c.Expr})) <= 13) ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsNumber") when sig.ParameterTypes.Length == 1:
            {
                // The number categories Nd/Nl/No (8..10) — wider than IsDigit's Nd.
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"((dn2cpp_char_unicode_category((char16_t)({c.Expr})) >= 8 && dn2cpp_char_unicode_category((char16_t)({c.Expr})) <= 10) ? 1 : 0)");
                return true;
            }
            case ("System.Char", "GetNumericValue") when sig.ParameterTypes.Length == 1:
            {
                // The generated BMP numeric-value table (dn2cpp_char_numeric.cpp);
                // -1.0 for code points with no numeric value, like .NET.
                var c = Pop();
                Push(StackKind.R8, "double", $"dn2cpp_char_get_numeric_value((char16_t)({c.Expr}))");
                return true;
            }
            // Simple one-to-one BMP invariant case maps (generated tables in
            // dn2cpp_invariant_casing.cpp), exact vs .NET's ToUpperInvariant/
            // ToLowerInvariant. The culture-free 1-arg ToUpper/ToLower map to the
            // same invariant helpers.
            case ("System.Char", "ToUpper") when sig.ParameterTypes.Length == 1:
            case ("System.Char", "ToUpperInvariant") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_char_upper_invariant((char16_t)({c.Expr}))");
                return true;
            }
            case ("System.Char", "ToLower") when sig.ParameterTypes.Length == 1:
            case ("System.Char", "ToLowerInvariant") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_char_lower_invariant((char16_t)({c.Expr}))");
                return true;
            }
            // The (char, CultureInfo) casing overloads. DECLARED DIVERGENCE: dn2cpp is
            // invariant-only, so the culture operand is popped and ignored and both lower
            // to the same invariant BMP case maps as the culture-free forms above — exact
            // for InvariantCulture, and for every culture whose simple case fold coincides
            // with it. Matched on the char + CultureInfo shape so the single-arg forms
            // (Length == 1, above) keep their own arms.
            case ("System.Char", "ToUpper")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Class, Class.FullName: "System.Globalization.CultureInfo" }]:
            {
                Pop(); // culture — ignored (invariant-only)
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_char_upper_invariant((char16_t)({c.Expr}))");
                return true;
            }
            case ("System.Char", "ToLower")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Class, Class.FullName: "System.Globalization.CultureInfo" }]:
            {
                Pop(); // culture — ignored (invariant-only)
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_char_lower_invariant((char16_t)({c.Expr}))");
                return true;
            }
            // ASCII helpers (.NET 7+) — exact ASCII checks.
            case ("System.Char", "IsAscii") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(({c.Expr}) <= 0x7F ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsAsciiDigit") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(({c.Expr}) >= u'0' && ({c.Expr}) <= u'9' ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsAsciiLetter") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"((({c.Expr}) >= u'A' && ({c.Expr}) <= u'Z') || (({c.Expr}) >= u'a' && ({c.Expr}) <= u'z') ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsAsciiLetterUpper") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(({c.Expr}) >= u'A' && ({c.Expr}) <= u'Z' ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsAsciiLetterLower") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"(({c.Expr}) >= u'a' && ({c.Expr}) <= u'z' ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsAsciiLetterOrDigit") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"((({c.Expr}) >= u'0' && ({c.Expr}) <= u'9') || (({c.Expr}) >= u'A' && ({c.Expr}) <= u'Z') || (({c.Expr}) >= u'a' && ({c.Expr}) <= u'z') ? 1 : 0)");
                return true;
            }
            // IsBetween(c, min, max) — inclusive range check, culture-free, so the
            // inline form is exact for every code point.
            case ("System.Char", "IsBetween") when sig.ParameterTypes.Length == 3:
            {
                var max = Pop();
                var min = Pop();
                var c = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(({c.Expr}) >= ({min.Expr}) && ({c.Expr}) <= ({max.Expr}) ? 1 : 0)");
                return true;
            }
            case ("System.Char", "IsAsciiHexDigit") when sig.ParameterTypes.Length == 1:
            {
                var c = Pop();
                Push(StackKind.I4, "int32_t", $"((({c.Expr}) >= u'0' && ({c.Expr}) <= u'9') || (({c.Expr}) >= u'A' && ({c.Expr}) <= u'F') || (({c.Expr}) >= u'a' && ({c.Expr}) <= u'f') ? 1 : 0)");
                return true;
            }
            // char instance ToString -> the 1-char string. Deref the receiver at the
            // real 2-byte width (SubWordCppType): a char16_t* into packed storage (a
            // string's chars, a span ref, a narrowed struct field) must read one code
            // unit, not an int32 that splices in the adjacent bytes.
            case ("System.Char", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_char_to_string((char16_t)({DerefReceiver(SubWordCppType(declType))}))");
                return true;
            // The static char.ToString(char) — the 1-char string over the argument
            // (no receiver). Distinguished from the instance IFormatProvider overload
            // (both arity 1) by the argument TYPE: this one takes a char, that one an
            // IFormatProvider, so neither pops the other's shape.
            case ("System.Char", "ToString")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var c = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_char_to_string((char16_t)({c.Expr}))");
                return true;
            }
            // The instance char.ToString(IFormatProvider) — same 1-char string as the
            // 0-arg form; the provider is culture formatting we don't model (a char has
            // no culture-dependent rendering anyway) and is popped and ignored.
            case ("System.Char", "ToString")
                when sig.ParameterTypes is [{ } pft] && IsFormatProvider(pft):
            {
                Pop(); // format provider — ignored
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_char_to_string((char16_t)({DerefReceiver(SubWordCppType(declType))}))");
                return true;
            }
            // The static char.TryParse(string, out char) — succeeds iff the string is
            // exactly one code unit long (real .NET's definition, culture-free), writing
            // that unit to the out slot; on failure the out slot is 0, like .NET. A null
            // string is a failure (length test short-circuits), never a deref.
            case ("System.Char", "TryParse")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String },
                    { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } }]:
            {
                var outRef = Pop();
                var s = Pop();
                string st = NewTemp("Dn2CppString*");
                Emit($"{st} = {Cast(s, "Dn2CppString*")};");
                string ok = NewTemp("int32_t");
                Emit($"{ok} = ({st} != nullptr && {st}->length == 1) ? 1 : 0;");
                Emit($"*((char16_t*)({outRef.Expr})) = {ok} ? (char16_t)dn2cpp_string_get_char({st}, 0) : (char16_t)0;");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }

            default:
                return false;
        }
    }
}
