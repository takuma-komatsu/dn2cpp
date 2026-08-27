using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitInterpStringIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {

            // ---- interpolated strings ----
            // The C# compiler lowers $"..." to DefaultInterpolatedStringHandler
            // (a ref struct in System.Runtime.CompilerServices). Its real body
            // reaches Buffer.Memmove (InternalCall); we model it as the runtime
            // Dn2CppISB growable UTF-16 buffer. Non-generic members are handled
            // here; the generic AppendFormatted<T> routes through
            // TranslateGenericIntrinsic.
            // string.Create([IFormatProvider], [Span<char> initialBuffer],
            // ref DefaultInterpolatedStringHandler) — the interpolated-string factory the
            // C# compiler emits for `$"..."` used as a method argument / return value. It
            // just materializes the handler's accumulated buffer, so it maps to
            // dn2cpp_isb_to_string; any leading provider/initialBuffer args are popped and
            // discarded (the ISB owns its own growable buffer). The generic
            // string.Create<TState>(int, TState, SpanAction) overload is a MethodSpec and
            // never reaches here.
            case ("System.String", "Create")
                when sig.ParameterTypes.Length >= 1
                    && sig.ParameterTypes[^1] is { Kind: TypeKind.ByRef, Element: { } el }
                    && el.ToString() == "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler":
            {
                var handlerRef = Pop(); // ref to the handler local (the ISB)
                for (int i = sig.ParameterTypes.Length - 2; i >= 0; i--)
                    Pop(); // provider and/or initialBuffer span
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_isb_to_string((Dn2CppISB*)({handlerRef.Expr}))");
                return true;
            }

            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", ".ctor")
                when sig.ParameterTypes.Length >= 2
                    && sig.ParameterTypes[0].Kind == TypeKind.Primitive
                    && sig.ParameterTypes[1].Kind == TypeKind.Primitive:
            {
                //.ctor(int literalLength, int formattedCount,
                // [IFormatProvider? provider], [Span<char> initialBuffer]).
                // The real type stack-allocates a scratch buffer and threads a
                // culture; we model it as the growable Dn2CppISB, so the extra
                // args are popped and discarded (InvariantCulture, and we own the
                // buffer).
                for (int i = sig.ParameterTypes.Length - 1; i >= 2; i--)
                    Pop(); // provider and/or initialBuffer span
                var formattedCount = Pop();
                var literalLength = Pop();
                var self = Pop(); // managed pointer to the handler local
                Emit($"*((Dn2CppISB*)({self.Expr})) = dn2cpp_isb_new({literalLength.Expr}, {formattedCount.Expr});");
                return true;
            }
            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "AppendLiteral"):
            {
                var s = Pop();
                var self = Pop();
                Emit($"dn2cpp_isb_append_literal((Dn2CppISB*)({self.Expr}), {Cast(s, "Dn2CppString*")});");
                return true;
            }
            // AppendFormatted(string) — the non-generic overload for string holes.
            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "AppendFormatted")
                when sig.ParameterTypes.Length == 1 && sig.ParameterTypes[0].IsString:
            {
                var s = Pop();
                var self = Pop();
                Emit($"dn2cpp_isb_append_aligned((Dn2CppISB*)({self.Expr}), {Cast(s, "Dn2CppString*")}, 0);");
                return true;
            }
            // AppendFormatted(string, int alignment, string format) — honor the
            // alignment; a format string is ignored for a string hole (.NET does
            // the same — string has no standard format specifiers).
            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "AppendFormatted")
                when sig.ParameterTypes.Length >= 2 && sig.ParameterTypes[0].IsString:
            {
                string alignExpr = "0";
                for (int i = sig.ParameterTypes.Length - 1; i >= 1; i--)
                {
                    var arg = Pop();
                    if (!sig.ParameterTypes[i].IsString) // the int alignment param
                        alignExpr = arg.Expr;
                }
                var s = Pop();
                var self = Pop();
                Emit($"dn2cpp_isb_append_aligned((Dn2CppISB*)({self.Expr}), {Cast(s, "Dn2CppString*")}, {alignExpr});");
                return true;
            }
            // AppendFormatted(ReadOnlySpan<char>) and its (value, alignment, format)
            // sibling — the hole of `$"{someSpan}"`. Like the string overloads (and
            // like real .NET, which ignores `format` for both) only the alignment is
            // honored.
            //
            // The span is materialized into a string and handed to the same
            // dn2cpp_isb_append_aligned the string overloads use, rather than getting an
            // append-span helper of its own: a second helper would mean a second copy of
            // the padding rule, and the two must agree — `$"{s,10}"` and
            // `$"{s.AsSpan(),10}"` are the same output in real .NET.
            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "AppendFormatted")
                when sig.ParameterTypes is [{ } sp0, ..] && IsCharSpan(sp0):
            {
                string alignExpr = "0";
                for (int i = sig.ParameterTypes.Length - 1; i >= 1; i--)
                {
                    var arg = Pop();
                    if (!sig.ParameterTypes[i].IsString) // the int alignment param
                        alignExpr = arg.Expr;
                }
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
                string sp = SpanPtr(Pop(), spanCt);
                var self = Pop();
                Emit($"dn2cpp_isb_append_aligned((Dn2CppISB*)({self.Expr}), "
                    + $"dn2cpp_string_from_chars((const char16_t*){sp}->f__reference, {sp}->f__length), "
                    + $"{alignExpr});");
                return true;
            }
            // AppendFormatted(object, int alignment, string format) — the boxed hole.
            // Real .NET's body forwards straight to AppendFormatted<object>, so this
            // lowers through the very helper that overload's arm uses, with the declared
            // parameter type (System.Object) as the hole type: they are one method
            // upstream and so cannot answer differently.
            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "AppendFormatted")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object }, ..]:
            {
                string formatExpr = "nullptr";
                string alignExpr = "0";
                for (int i = sig.ParameterTypes.Length - 1; i >= 1; i--)
                {
                    var arg = Pop();
                    if (sig.ParameterTypes[i].IsString)
                        formatExpr = Cast(arg, "Dn2CppString*");
                    else
                        alignExpr = arg.Expr;
                }
                var val = Pop();
                var self = Pop();
                string valStr = FormatInterpolationHole(sig.ParameterTypes[0], val, formatExpr);
                Emit($"dn2cpp_isb_append_aligned((Dn2CppISB*)({self.Expr}), {valStr}, {alignExpr});");
                return true;
            }
            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "ToStringAndClear"):
            {
                var self = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_isb_to_string((Dn2CppISB*)({self.Expr}))");
                return true;
            }
            case ("System.Runtime.CompilerServices.DefaultInterpolatedStringHandler", "ToString"):
            {
                var self = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_isb_copy_to_string((Dn2CppISB*)({self.Expr}))");
                return true;
            }

            // ---- StringBuilder.AppendInterpolatedStringHandler ----
            // The handler the C# compiler lowers `sb.Append($"...")` through. It is
            // modeled as the underlying StringBuilder pointer (CoreIntrinsics nested map):
            // the handler local is a `Dn2CppStringBuilder*`, so `self` (its address) is a
            // `Dn2CppStringBuilder**`. The ctor captures the builder; AppendLiteral / the
            // non-generic AppendFormatted overloads append straight to it. The generic
            // AppendFormatted<T> is handled in TranslateGenericIntrinsic. The dispatch key
            // is the enclosing-qualified nested name (NestedDispatchName).
            case ("System.Text.StringBuilder+AppendInterpolatedStringHandler", ".ctor"):
            {
                //.ctor(int literalLength, int formattedCount, StringBuilder sb
                // [, IFormatProvider provider]). Capture the builder; the counts and
                // provider are advisory (we own a growable buffer + InvariantCulture).
                for (int i = sig.ParameterTypes.Length - 1; i >= 3; i--)
                    Pop(); // provider
                string sbArg = Cast(Pop(), "Dn2CppStringBuilder*"); // the StringBuilder
                Pop(); // formattedCount
                Pop(); // literalLength
                var self = Pop(); // address of the handler local
                Emit($"*((Dn2CppStringBuilder**)({self.Expr})) = {sbArg};");
                return true;
            }
            case ("System.Text.StringBuilder+AppendInterpolatedStringHandler", "AppendLiteral"):
            {
                var s = Pop();
                var self = Pop();
                Emit($"dn2cpp_sb_append_str(*((Dn2CppStringBuilder**)({self.Expr})), {Cast(s, "Dn2CppString*")});");
                return true;
            }
            // Non-generic AppendFormatted(string [, int alignment, string format]) — a
            // string hole. The format specifier is ignored for a string (no standard
            // specifiers); the alignment, if present, pads via a throwaway DISH.
            case ("System.Text.StringBuilder+AppendInterpolatedStringHandler", "AppendFormatted")
                when sig.ParameterTypes.Length >= 1 && sig.ParameterTypes[0].IsString:
            {
                string alignExpr = "0";
                for (int i = sig.ParameterTypes.Length - 1; i >= 1; i--)
                {
                    var arg = Pop();
                    if (!sig.ParameterTypes[i].IsString) // the int alignment param
                        alignExpr = arg.Expr;
                }
                string s = Cast(Pop(), "Dn2CppString*");
                var self = Pop();
                if (alignExpr == "0")
                    Emit($"dn2cpp_sb_append_str(*((Dn2CppStringBuilder**)({self.Expr})), {s});");
                else
                {
                    string isb = NewTemp("Dn2CppISB");
                    Emit($"{isb} = dn2cpp_isb_new(0, 1);");
                    Emit($"dn2cpp_isb_append_aligned(&{isb}, {s}, {alignExpr});");
                    Emit($"dn2cpp_sb_append_str(*((Dn2CppStringBuilder**)({self.Expr})), dn2cpp_isb_to_string(&{isb}));");
                }
                return true;
            }

            // Unsafe.{Copy,Init}Block[Unaligned] — the non-generic block memory ops (the
            // generic Unsafe.* live in TryUnsafeIntrinsic). The real BCL bodies are
            // literally `cpblk`/`initblk`, but Unsafe is an intrinsic type so the body is
            // never transpiled — intercept by name and map to the same memmove/memset as
            // the raw opcodes. The (Un)aligned variants differ only in an alignment hint we
            // ignore. Stack:..., dest, src|value, byteCount.
            case ("System.Runtime.CompilerServices.Unsafe", "CopyBlock"):
            case ("System.Runtime.CompilerServices.Unsafe", "CopyBlockUnaligned"):
            {
                var size = Pop();
                var src = Pop();
                var dest = Pop();
                Emit($"std::memmove((void*)({dest.Expr}), (void*)({src.Expr}), (size_t)({size.Expr}));");
                // The destination is untyped, so it may have received references:
                // barrier conservatively, like the raw cpblk lowering.
                Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({dest.Expr}));");
                return true;
            }
            case ("System.Runtime.CompilerServices.Unsafe", "InitBlock"):
            case ("System.Runtime.CompilerServices.Unsafe", "InitBlockUnaligned"):
            {
                var size = Pop();
                var val = Pop();
                var dest = Pop();
                Emit($"std::memset((void*)({dest.Expr}), (int)({val.Expr}), (size_t)({size.Expr}));");
                // Untyped destination: barrier conservatively (see CopyBlock above).
                Emit($"dn2cpp_gc_write_barrier_if_heap((void*)({dest.Expr}));");
                return true;
            }

            default:
                return false;
        }
    }
}
