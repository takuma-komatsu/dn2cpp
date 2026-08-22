using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitNumbersIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {
            // Any System.Exception constructor (parameterless, message, message+inner,
            // serialization). The real body is untranspilable (culture-dependent
            // formatting, ReadOnlyCollection copies), but its ONE observable effect on
            // dn2cpp's model — the message + inner slots of the uniform
            // Dn2CppExceptionObject prefix — is reproduced here by storing them straight
            // onto the receiver. Every transpiled derived-exception ctor chain bottoms
            // out in this base call, so the message the BCL computed lands on the object
            // regardless of how many derived ctors sit above it, and a parameterless BCL
            // exception gains its real default message (its parameterless ctor chains
            // base(SR.Arg_<Type>)). The opaque System.Exception / AggregateException
            // newobj paths, where no ctor body runs, keep the positional recovery.
            case ("System.Exception", ".ctor"):
            {
                int msgIdx = Compilation.ExceptionMessageArgIndex(sig.ParameterTypes);
                int innerIdx = Compilation.ExceptionInnerArgIndex(sig.ParameterTypes);
                string message = "nullptr";
                string inner = "nullptr";
                for (int i = sig.ParameterTypes.Length - 1; i >= 0; i--)
                {
                    var a = Pop();
                    if (i == msgIdx)
                        message = Cast(a, "Dn2CppString*");
                    else if (i == innerIdx)
                        inner = Cast(a, "Dn2CppObject*");
                }
                string recv = Cast(Pop(), "Dn2CppExceptionObject*");
                if (message != "nullptr")
                    Emit($"{recv}->message = {message};");
                if (inner != "nullptr")
                    Emit($"{recv}->inner = {inner};");
                return true;
            }
            // System.Exception.set_HResult: store the int32 onto the hresult slot of
            // the Dn2CppExceptionObject prefix. Each derived BCL ctor sets its per-type
            // value here (ArgumentException → COR_E_ARGUMENT, …); user code may set it too.
            case ("System.Exception", "set_HResult"):
            {
                var v = Pop(); // value
                string r = Cast(Pop(), "Dn2CppExceptionObject*");
                Emit($"{r}->hresult = (int32_t)({v.Expr});");
                return true;
            }
            // Exception bookkeeping setters we still don't model (no observable slot in
            // dn2cpp's exception layout): drop the value + receiver.
            case ("System.Exception", "set_Source"):
            case ("System.Exception", "set_HelpLink"):
                Pop(); // value
                Pop(); // this
                return true;
            // System.Exception.get_HResult: read the stored hresult (set_HResult above,
            // or the COR_E_EXCEPTION base default seeded at allocation). A real value
            // matters because SetMessageField probes it
            // (`if (_message == null && HResult == COR_E_ARGUMENT)`) to pick a
            // parameterless Argument*/FileNotFound*'s per-type resource default Message.
            case ("System.Exception", "get_HResult"):
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_exception_hresult((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // System.Exception.GetBaseException(): the innermost exception of the
            // inner-chain, or the exception itself when there is none (real .NET's
            // identity case).
            // INTENTIONAL DIVERGENCE: AggregateException overrides this to collapse
            // single-child chains — opaque here, so it takes the plain inner walk, the
            // same documented approximation as its Message.
            case ("System.Exception", "GetBaseException"):
            {
                var o = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_exception_get_base((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // System.Exception.get_Message: the message the exception carries. A callvirt
            // `ex.Message` (own-code reading a caught exception, or a base-typed receiver
            // whose callee resolves to the intrinsic base getter) DISPATCHES a derived
            // get_Message override through the vtable — ArgumentException appends
            // "(Parameter 'x')", FileNotFoundException builds its text lazily. A non-virtual
            // `call` — `base.Message` INSIDE such an override — must read the stored message
            // directly, or dispatching back to the override would recurse forever.
            case ("System.Exception", "get_Message"):
            {
                var o = Pop();
                string fn = CallIsVirtual ? "dn2cpp_exception_message" : "dn2cpp_exception_message_stored";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"{fn}((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // System.Exception.ToString: "FullTypeName: Message" plus the inner
            // " ---> " chain, then the stack-trace section when a trace was
            // captured at throw (absent for an unthrown exception, as in real .NET).
            case ("System.Exception", "ToString") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_exception_tostring((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // System.Exception.get_InnerException: the inner exception stored by the
            // (string, Exception) ctor shape, or null for the other shapes — exact for
            // both. A derived exception's ex.InnerException routes here via the opaque
            // Exception base.
            case ("System.Exception", "get_InnerException"):
            {
                var o = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_exception_inner((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // ExceptionDispatchInfo capture/restore (Exception.CaptureDispatchState /
            // RestoreDispatchState): dn2cpp preserves no stack-trace / Watson state, so
            // capturing yields an empty DispatchState and restoring is a no-op. With an
            // empty state a replayed Throw() simply re-raises the exception — the
            // stack-trace-preservation contract is the known no-symbolication carve-out.
            case ("System.Exception", "CaptureDispatchState"):
            {
                Pop(); // this
                string ct = CppTypes.Of(sig.ReturnType);
                string zero = CppTypes.ZeroInitExpr(ct);
                Push(CppTypes.KindOf(sig.ReturnType), ct, zero);
                return true;
            }
            case ("System.Exception", "RestoreDispatchState"):
                Pop(); // in DispatchState state
                Pop(); // this
                return true;
            // System.Exception.SetRemoteStackTrace(string): stamps _remoteStackTraceString
            // when an ExceptionDispatchInfo replays a captured exception across an
            // await/thread hop. dn2cpp models no managed stack traces, so there is nothing
            // to stamp: pop the receiver + string and no-op. The rethrow still re-raises
            // the same exception object — exact except for the trace text.
            case ("System.Exception", "SetRemoteStackTrace"):
                Pop(); // string stackTrace
                Pop(); // this
                return true;
            // System.Exception.SetCurrentStackTrace(): the sibling of SetRemoteStackTrace —
            // it stamps the CURRENT stack onto an exception that is about to be handed to a
            // faulted Task rather than thrown. Same posture, for the same reason: no
            // managed stack traces are modeled, so the exception object is unchanged
            // (right type, right message, right Task) and only its trace text is absent.
            case ("System.Exception", "SetCurrentStackTrace"):
                Pop(); // this
                return true;
            // System.Exception.GetObjectData(SerializationInfo, StreamingContext): the
            // ISerializable base hook every serializable exception's override calls.
            // Formatter-based serialization is obsolete/removed in modern .NET and dn2cpp
            // models no SerializationInfo writes, so there is nothing to store: pop the
            // two args + receiver and no-op. The override's OWN AddValue calls still run
            // against the transpiled SerializationInfo; only the base payload
            // (ClassName/Message/HResult/...) is absent, and nothing reads it back without
            // a formatter. Shape-guarded on the exact 2-arg form so an upstream overload
            // declines to the loud throw instead of popping blind.
            case ("System.Exception", "GetObjectData") when sig.ParameterTypes.Length == 2:
                Pop(); // StreamingContext context
                Pop(); // SerializationInfo info
                Pop(); // this
                return true;
            // AggregateException.get_InnerExceptions: a REAL ReadOnlyCollection<Exception>
            // wrapping the runtime's Exception[], which is what real .NET returns. Pushing
            // the bare array instead is a type lie the ordinary spelling
            // (`ae.InnerExceptions.Count`) turns into a wild dispatch — it binds
            // ReadOnlyCollection<Exception>.get_Count against an object whose header is an
            // array's, so the transpile and link are green and the shipped program faults.
            //
            // The wrapper is ordinary transpiled BCL IL over the array as its IList<T>, so
            // this buys the whole surface at once instead of an open-ended set of per-member
            // intercepts. `sig.ReturnType` is already the CLOSED instantiation (a generic in
            // a decoded signature is closed by SignatureProvider), so nothing is minted by
            // name.
            //
            // NoteArrayEnumerableElement is load-bearing: DirectCall bypasses CoerceTo, so
            // this call is the boundary where a raw Exception[] flows into an
            // IList<Exception> parameter, and the note is what wires the array's IList<T>
            // row. Without it the wrapper's own get_Count dispatches into a hole.
            case ("System.AggregateException", "get_InnerExceptions")
                when sig.ReturnType is { Kind: TypeKind.Class, Class: { } rocCls }:
            {
                // Match the ReadOnlyCollection(IList<T>) ctor. Decline before popping, per
                // the subtable contract, so a CoreLib without it leaves the stack alone.
                if (Comp.ReachManagedMethod(rocCls, ".ctor",
                        static ps => ps is [{ Kind: TypeKind.Class, Class.IsInterface: true }],
                        allocates: true) is not { } rocCtor)
                    return false;
                var o = Pop();
                var elem = TypeDesc.MakeClass(Comp.FindClassByFullName("System.Exception")!);
                Comp.NoteArrayEnumerableElement(elem);
                string self = NewTemp("Dn2CppObject*");
                Emit($"{self} = (Dn2CppObject*)({o.Expr});");
                // MEMOIZED on the exception object, because real .NET's InnerExceptions is a
                // stored field: two reads are reference-equal, which a fresh wrapper per
                // call would answer False for.
                string roc = NewTemp(rocCls.CppStructName + "*");
                Emit($"{roc} = ({rocCls.CppStructName}*)dn2cpp_aggregate_inner_wrapper({self});");
                Emit($"if ({roc} == nullptr) {{");
                string arr = NewTemp("Dn2CppArrayRef*");
                Emit($"    {arr} = dn2cpp_aggregate_inner_exceptions({self}, "
                    + $"{PreciseArrayTypeInfoExpr(elem)});");
                Emit($"    {roc} = ({rocCls.CppStructName}*)dn2cpp_alloc(sizeof({rocCls.CppStructName}));");
                Emit($"    ((Dn2CppObject*){roc})->type = &{rocCls.CppTypeInfoName};");
                // DirectCall inserts no parameter cast of its own, so the array -> IList<T>
                // conversion is spelled here.
                string listArg = $"({CppTypes.Of(rocCtor.Signature.ParameterTypes[0])}){arr}";
                Emit($"    {DirectCall(rocCtor, new List<string> { roc, listArg })};");
                Emit($"    dn2cpp_aggregate_set_inner_wrapper({self}, (Dn2CppObject*){roc});");
                Emit("}");
                PushEntry(new StackEntry(roc, StackKind.Ref, rocCls.CppStructName + "*",
                    StaticType: TypeDesc.MakeClass(rocCls)));
                return true;
            }
            // System.Exception.get_StackTrace: the trace dn2cpp_throw captured
            // into the exception's trace slot, resolved to text by the runtime.
            // Null for an un-thrown exception — EXACT, real .NET also returns null
            // until the exception is thrown — and a best-effort real trace for a
            // thrown one, where the divergence is the frame format and any
            // unresolved/dropped frames (declared at the runtime's
            // dn2cpp_exc_trace_render).
            case ("System.Exception", "get_StackTrace") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_exception_stacktrace((Dn2CppObject*)({o.Expr}))");
                return true;
            }
            // System.Exception.get_Source: null carve-out, symmetric with the
            // set_Source no-op above (dn2cpp does not retain the Source /
            // TargetSite assembly identity). EXACT for an un-thrown exception
            // (real .NET also returns null until the exception propagates) and an
            // INTENTIONAL DIVERGENCE for a thrown one (real .NET fills in the
            // throwing assembly's name).
            case ("System.Exception", "get_Source"):
            {
                Pop(); // this
                Push(StackKind.Ref, "Dn2CppString*", "nullptr");
                return true;
            }
            // System.Exception.GetType (and any derived exception's GetType, which
            // routes here via the Exception opaque base): the runtime Type from the
            // object header, identical to Object.GetType.
            case ("System.Exception", "GetType") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Push(StackKind.Ref, "Dn2CppType*",
                    $"dn2cpp_get_type_from_handle(((Dn2CppObject*)({o.Expr}))->type)");
                return true;
            }

            // System.Math / System.MathF — pure numeric functions.
            case ("System.Math", _) or ("System.MathF", _) when TryMathIntrinsic(declType, name, sig):
                return true;

            // System.Double / System.Single static math functions — the same map
            // as Math/MathF (double.Sqrt == Math.Sqrt, float.Abs == MathF.Abs)
            // plus the Double/Single-only surface it carries (the exp/log
            // compositions, the *Pi angle family, Lerp/MultiplyAddEstimate, the
            // Number/Native min-max variants, Ieee754Remainder). Guarded on the
            // static call shape so the instance members (ToString/CompareTo/
            // GetHashCode) and the non-math statics (Parse/TryParse, the IsXxx
            // predicates) fall through to their own cases below — a case whose
            // `when` returns false keeps flowing to the later labels, and
            // TryMathIntrinsic pops nothing unless it matches.
            case ("System.Double", _) or ("System.Single", _)
                    when !sig.Header.IsInstance && TryMathIntrinsic(declType, name, sig):
                return true;

            // Primitive ToString: the receiver is a managed pointer (ldloca)
            // or the value itself.
            case ("System.Int32", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_int_to_string({DerefReceiver("int32_t")})");
                return true;
            // UInt32 must format unsigned — int_to_string would print values above
            // Int32.MaxValue as negative (e.g. uint.MaxValue as -1).
            case ("System.UInt32", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint((uint64_t){DerefReceiver("uint32_t")}, 4, nullptr)");
                return true;
            // Sub-word integers (Byte/SByte/Int16/UInt16) are stored in int32 slots:
            // Byte/UInt16 are positive (0..65535), SByte/Int16 are already sign-extended,
            // so a signed int32 decimal print is exact for all four. These types are NOT
            // intrinsic types, so without this interception (+ the reachability cut in
            // Compilation.ResolveCallTarget) their real bodies route through
            // System.Number.Format*.
            case ("System.Byte", "ToString") or ("System.SByte", "ToString")
                    or ("System.Int16", "ToString") or ("System.UInt16", "ToString")
                    when sig.ParameterTypes.Length == 0:
                // Deref the receiver at its REAL sub-word width, not int32: a managed
                // pointer to a sub-word value may point into 1/2-byte storage (a byte*
                // into a packed byte[]), where reading 4 bytes would splice in adjacent
                // elements. A value receiver still carries the int32 slot, which
                // DerefReceiver casts correctly.
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_int_to_string((int32_t)({DerefReceiver(SubWordCppType(declType))}))");
                return true;
            case ("System.Int64", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_long_to_string({DerefReceiver("int64_t")})");
                return true;
            // UInt64 must format unsigned, like the boxed path (box_to_string).
            case ("System.UInt64", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint({DerefReceiver("uint64_t")}, 8, nullptr)");
                return true;
            case ("System.Double", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_double_to_string({DerefReceiver("double")})");
                return true;
            case ("System.Single", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_float_to_string({DerefReceiver("float")})");
                return true;
            // ToString(string format) — the explicit format-specifier / custom-pattern
            // overload, routed through the same runtime formatters as a `{0:fmt}` hole
            // (standard letters F/N/E/G/P/C/D/X and custom 0/#/./,/;/%/escapes),.
            case ("System.Int32", "ToString") or ("System.UInt32", "ToString")
                    when sig.ParameterTypes is [{ IsString: true }]:
            {
                string fmt = Cast(Pop(), "Dn2CppString*");
                bool uns = declType == "System.UInt32";
                Push(StackKind.Ref, "Dn2CppString*",
                    uns ? $"dn2cpp_format_uint((uint32_t)({DerefReceiver("int32_t")}), 4, {fmt})"
                        : $"dn2cpp_format_int({DerefReceiver("int32_t")}, 4, {fmt})");
                return true;
            }
            // Sub-word ToString(format) — the same runtime formatters as Int32, with the
            // type width in bytes (Byte/SByte=1, Int16/UInt16=2) so hex masks the two's-
            // complement pattern correctly: short(-1).ToString("x2") is "ffff", not
            // "ffffffff". Unsigned (Byte/UInt16) cast positive first.
            case ("System.Byte", "ToString") or ("System.UInt16", "ToString")
                    when sig.ParameterTypes is [{ IsString: true }]:
            {
                string fmt = Cast(Pop(), "Dn2CppString*");
                bool b1 = declType == "System.Byte";
                int width = b1 ? 1 : 2;
                string cast = b1 ? "uint8_t" : "uint16_t";
                // Deref at the real sub-word width (see the parameterless case above) so
                // a byte*/ushort* receiver into packed storage reads exactly one element.
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint(({cast})({DerefReceiver(SubWordCppType(declType))}), {width}, {fmt})");
                return true;
            }
            case ("System.SByte", "ToString") or ("System.Int16", "ToString")
                    when sig.ParameterTypes is [{ IsString: true }]:
            {
                string fmt = Cast(Pop(), "Dn2CppString*");
                int width = declType == "System.SByte" ? 1 : 2;
                // Deref at the real sub-word width (see the parameterless case above); the
                // value is already sign-extended so format_int prints/masks correctly.
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_int({DerefReceiver(SubWordCppType(declType))}, {width}, {fmt})");
                return true;
            }
            case ("System.Int64", "ToString") or ("System.UInt64", "ToString")
                    when sig.ParameterTypes is [{ IsString: true }]:
            {
                string fmt = Cast(Pop(), "Dn2CppString*");
                bool uns = declType == "System.UInt64";
                Push(StackKind.Ref, "Dn2CppString*",
                    uns ? $"dn2cpp_format_uint((uint64_t)({DerefReceiver("int64_t")}), 8, {fmt})"
                        : $"dn2cpp_format_int({DerefReceiver("int64_t")}, 8, {fmt})");
                return true;
            }
            // the integer-primitive span-write formatter
            // bool TryFormat(Span<char> dest, out int charsWritten,
            // ReadOnlySpan<char> format = default, IFormatProvider provider = null)
            // (ISpanFormattable). Lower to dn2cpp_try_format_int|uint_c, which reuse the
            // same byteWidth-aware formatters as ToString(format) and copy into the
            // Span<char>'s buffer with .NET semantics (fits ⇒ write + true, else
            // untouched + charsWritten=0 + false). format span -> Dn2CppString*
            // (empty = "G"); provider routed (null = current culture, .NET's rule).
            // These types are NOT added to s_intrinsicTypes — this is a targeted cut.
            // The destination span's element type discriminates the two shape-identical
            // interface methods: Span<char> is ISpanFormattable's char16 writer,
            // Span<byte> IUtf8SpanFormattable's UTF-8 one (without the check the char16
            // writer would silently corrupt a byte buffer).
            case ("System.Byte", "TryFormat") or ("System.SByte", "TryFormat")
                    or ("System.Int16", "TryFormat") or ("System.UInt16", "TryFormat")
                    or ("System.Int32", "TryFormat") or ("System.UInt32", "TryFormat")
                    or ("System.Int64", "TryFormat") or ("System.UInt64", "TryFormat")
                    when sig.ParameterTypes is [{ } dest, { Kind: TypeKind.ByRef }, _, _]
                        && IsSpanOfPrimitive(dest, PrimitiveTypeCode.Char):
            {
                EmitIntegerTryFormat(declType, sig, utf8: false);
                return true;
            }
            // bool TryFormat(Span<byte> utf8Destination, out int bytesWritten,
            // ReadOnlySpan<char> format = default, IFormatProvider provider = null)
            // (IUtf8SpanFormattable) — the same formatter cores narrowed to UTF-8.
            case ("System.Byte", "TryFormat") or ("System.SByte", "TryFormat")
                    or ("System.Int16", "TryFormat") or ("System.UInt16", "TryFormat")
                    or ("System.Int32", "TryFormat") or ("System.UInt32", "TryFormat")
                    or ("System.Int64", "TryFormat") or ("System.UInt64", "TryFormat")
                    when sig.ParameterTypes is [{ } dest, { Kind: TypeKind.ByRef }, _, _]
                        && IsSpanOfPrimitive(dest, PrimitiveTypeCode.Byte):
            {
                EmitIntegerTryFormat(declType, sig, utf8: true);
                return true;
            }
            case ("System.Double", "ToString") when sig.ParameterTypes is [{ IsString: true }]:
            {
                string fmt = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_format_r8({DerefReceiver("double")}, {fmt})");
                return true;
            }
            case ("System.Single", "ToString") when sig.ParameterTypes is [{ IsString: true }]:
            {
                string fmt = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_format_r4({DerefReceiver("float")}, {fmt})");
                return true;
            }
            // Double/Single.TryFormat(Span<char> dest, out int charsWritten,
            // ReadOnlySpan<char> format, IFormatProvider): render into the destination span
            // (fits -> copy + count + true; too short -> false). An empty/default format
            // takes the round-trip ToString (dn2cpp_float_to_string_c keeps float precision);
            // a standard format string routes through the format helper, which throws loudly
            // on one it does not cover. The provider routes to the *_c formatters like the
            // ToString(provider) arms; null keeps .NET's null-provider rule (current culture).
            case ("System.Double" or "System.Single", "TryFormat")
                when sig.ParameterTypes is [var f0, { Kind: TypeKind.ByRef }, var f2, _]
                    && (IsSpanOfPrimitive(f0, PrimitiveTypeCode.Char) || IsSpanOfPrimitive(f0, PrimitiveTypeCode.Byte))
                    && IsReadOnlyCharSpan(f2):
            {
                bool isByte = IsSpanOfPrimitive(f0, PrimitiveTypeCode.Byte);
                bool f32 = declType == "System.Single";
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string fmtSpan = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[2]));
                string wrote = Cast(Pop(), "int32_t*");
                string dPtr = SpanPtr(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                string vt = NewTemp(f32 ? "float" : "double");
                Emit($"{vt} = {DerefReceiver(f32 ? "float" : "double")};");
                string defExpr = f32 ? $"dn2cpp_float_to_string_c({vt}, {prov})" : $"dn2cpp_double_to_string_c({vt}, {prov})";
                string fmtCall = f32 ? "dn2cpp_format_r4_c" : "dn2cpp_format_r8_c";
                string sv = NewTemp("Dn2CppString*");
                Emit($"{sv} = {fmtSpan}.f__length == 0 ? {defExpr} " +
                     $": {fmtCall}({vt}, dn2cpp_string_from_chars((const char16_t*){fmtSpan}.f__reference, {fmtSpan}.f__length), {prov});");
                if (isByte)
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_string_try_copy_to_utf8_span({sv}, (uint8_t*){dPtr}->f__reference, {dPtr}->f__length, {wrote})");
                else
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_string_try_copy_to_span({sv}, (char16_t*){dPtr}->f__reference, {dPtr}->f__length, {wrote})");
                return true;
            }
            // ---- Culture-aware numeric ToString (IFormatProvider) overloads ----
            // A CultureInfo / NumberFormatInfo provider lowers to a
            // const Dn2CppNumberFormatInfo*; the *_c formatters take it. The
            // value-then-provider (ToString(provider)) and value-format-provider
            // (ToString(format, provider)) shapes both land here.
            case ("System.Double", "ToString") when IsProviderOnly(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_double_to_string_c({DerefReceiver("double")}, {prov})");
                return true;
            }
            case ("System.Single", "ToString") when IsProviderOnly(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_float_to_string_c({DerefReceiver("float")}, {prov})");
                return true;
            }
            case ("System.Int32", "ToString") or ("System.UInt32", "ToString")
                    or ("System.Int64", "ToString") or ("System.UInt64", "ToString")
                    when IsProviderOnly(sig):
            {
                Pop(); // provider: an integer's default representation is culture-invariant text
                bool i64 = declType is "System.Int64" or "System.UInt64";
                Push(StackKind.Ref, "Dn2CppString*",
                    i64 ? $"dn2cpp_long_to_string({DerefReceiver("int64_t")})"
                        : $"dn2cpp_int_to_string({DerefReceiver("int32_t")})");
                return true;
            }
            // Sub-word ToString(provider) — same culture-invariant decimal print as
            // the parameterless case above (deref at the real sub-word width; the
            // signed int32 print is exact for all four).
            case ("System.Byte", "ToString") or ("System.SByte", "ToString")
                    or ("System.Int16", "ToString") or ("System.UInt16", "ToString")
                    when IsProviderOnly(sig):
            {
                Pop(); // provider: an integer's default representation is culture-invariant text
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_int_to_string((int32_t)({DerefReceiver(SubWordCppType(declType))}))");
                return true;
            }
            case ("System.Double", "ToString") when IsFormatThenProvider(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string fmt = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_format_r8_c({DerefReceiver("double")}, {fmt}, {prov})");
                return true;
            }
            case ("System.Single", "ToString") when IsFormatThenProvider(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string fmt = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_format_r4_c({DerefReceiver("float")}, {fmt}, {prov})");
                return true;
            }

            // ---- Double/Single static category predicates ----
            // dn2cpp's own code (MethodCompiler's ldc_r4/r8 handling) maps the special
            // float constants to the C++ math macros: NaN is the only value != itself,
            // and ±Infinity compares equal to the IEEE infinity literal. (The generic-math
            // INumberBase forms are handled separately in TryEmitGenericMathIntrinsic.)
            case ("System.Double", "IsNaN") or ("System.Single", "IsNaN")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive }]:
            {
                string x = Cast(Pop(), declType == "System.Single" ? "float" : "double");
                Push(StackKind.I4, "int32_t", $"(({x}) != ({x}) ? 1 : 0)");
                return true;
            }
            case ("System.Double", "IsPositiveInfinity") or ("System.Single", "IsPositiveInfinity")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive }]:
            {
                string x = Cast(Pop(), declType == "System.Single" ? "float" : "double");
                Push(StackKind.I4, "int32_t", $"(({x}) == (double)INFINITY ? 1 : 0)");
                return true;
            }
            case ("System.Double", "IsNegativeInfinity") or ("System.Single", "IsNegativeInfinity")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive }]:
            {
                string x = Cast(Pop(), declType == "System.Single" ? "float" : "double");
                Push(StackKind.I4, "int32_t", $"(({x}) == -(double)INFINITY ? 1 : 0)");
                return true;
            }
            // The remaining Double/Single static IsXxx predicates (the INumberBase /
            // IFloatingPointIeee754 / IBinaryNumber category tests). Bit-level
            // semantics, not comparisons: IsNegative/IsPositive read the sign bit
            // (std::signbit), so a negative-signed NaN IS negative and ±0 keep
            // their signs — exactly .NET's ToInt64Bits test. IsInteger and the
            // even/odd forms are the BCL's IsFinite && v == Truncate(v) plus the
            // |v % 2| check (C++ fmod is C#'s % for floating operands); IsPow2 is
            // the BCL's bit test, ported to a runtime helper (a subnormal is a
            // power of two iff exactly one significand bit is set). The operand is
            // spilled once — most expansions read it several times.
            case ("System.Double" or "System.Single",
                    "IsFinite" or "IsInfinity" or "IsNegative" or "IsPositive"
                    or "IsNormal" or "IsSubnormal" or "IsInteger" or "IsEvenInteger"
                    or "IsOddInteger" or "IsRealNumber" or "IsPow2")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive }]:
            {
                bool f32 = declType == "System.Single";
                string fk = f32 ? "float" : "double";
                string t = NewTemp(fk);
                Emit($"{t} = {Cast(Pop(), fk)};");
                string isInt = $"std::isfinite({t}) && {t} == std::trunc({t})";
                string expr = name switch
                {
                    "IsFinite" => $"(std::isfinite({t}) ? 1 : 0)",
                    "IsInfinity" => $"(std::isinf({t}) ? 1 : 0)",
                    "IsNegative" => $"(std::signbit({t}) ? 1 : 0)",
                    "IsPositive" => $"(std::signbit({t}) ? 0 : 1)",
                    "IsNormal" => $"(std::isnormal({t}) ? 1 : 0)",
                    "IsSubnormal" => $"(std::fpclassify({t}) == FP_SUBNORMAL ? 1 : 0)",
                    "IsInteger" => $"(({isInt}) ? 1 : 0)",
                    "IsEvenInteger" =>
                        $"(({isInt} && std::fabs(std::fmod({t}, ({fk})2)) == ({fk})0) ? 1 : 0)",
                    "IsOddInteger" =>
                        $"(({isInt} && std::fabs(std::fmod({t}, ({fk})2)) == ({fk})1) ? 1 : 0)",
                    "IsRealNumber" => $"(({t}) == ({t}) ? 1 : 0)",
                    _ => $"(dn2cpp_math_ispow2{(f32 ? "_f" : "")}({t}) ? 1 : 0)",
                };
                Push(StackKind.I4, "int32_t", expr);
                return true;
            }
            case ("System.Int32", "ToString") or ("System.UInt32", "ToString") when IsFormatThenProvider(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string fmt = Cast(Pop(), "Dn2CppString*");
                bool uns = declType == "System.UInt32";
                Push(StackKind.Ref, "Dn2CppString*",
                    uns ? $"dn2cpp_format_uint_c((uint32_t)({DerefReceiver("int32_t")}), 4, {fmt}, {prov})"
                        : $"dn2cpp_format_int_c({DerefReceiver("int32_t")}, 4, {fmt}, {prov})");
                return true;
            }
            case ("System.Int64", "ToString") or ("System.UInt64", "ToString") when IsFormatThenProvider(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string fmt = Cast(Pop(), "Dn2CppString*");
                bool uns = declType == "System.UInt64";
                Push(StackKind.Ref, "Dn2CppString*",
                    uns ? $"dn2cpp_format_uint_c((uint64_t)({DerefReceiver("int64_t")}), 8, {fmt}, {prov})"
                        : $"dn2cpp_format_int_c({DerefReceiver("int64_t")}, 8, {fmt}, {prov})");
                return true;
            }
            // Sub-word ToString(format, provider) — the culture-aware twin of the
            // ToString(format) arms above, and the last of the four ToString overloads
            // .NET declares on these types. Same runtime formatters as the wide integers,
            // carrying the type's byte width so a hex mask covers exactly its two's-
            // complement pattern (short(-1).ToString("x2", inv) is "ffff", not "ffffffff").
            // Deref at the real sub-word width so a byte*/ushort* receiver into packed
            // storage reads exactly one element (see the parameterless case above).
            case ("System.Byte", "ToString") or ("System.UInt16", "ToString")
                    when IsFormatThenProvider(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string fmt = Cast(Pop(), "Dn2CppString*");
                bool b1 = declType == "System.Byte";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_uint_c(({(b1 ? "uint8_t" : "uint16_t")})({DerefReceiver(SubWordCppType(declType))}), "
                    + $"{(b1 ? 1 : 2)}, {fmt}, {prov})");
                return true;
            }
            case ("System.SByte", "ToString") or ("System.Int16", "ToString")
                    when IsFormatThenProvider(sig):
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string fmt = Cast(Pop(), "Dn2CppString*");
                // Already sign-extended in its int32 slot, so format_int prints and masks it
                // correctly at the declared width.
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_format_int_c({DerefReceiver(SubWordCppType(declType))}, "
                    + $"{(declType == "System.SByte" ? 1 : 2)}, {fmt}, {prov})");
                return true;
            }
            // ---- CultureInfo / NumberFormatInfo ----
            // Static culture accessors. All three culture axes read the HOST: the
            // CurrentCulture slot, the CurrentUICulture slot and InstalledUICulture each
            // default to the locale dn2cpp_pal_default_locale_name resolves. Only
            // get_InvariantCulture is a constant.
            case ("System.Globalization.CultureInfo", "get_InvariantCulture"):
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", "dn2cpp_nfi_invariant()");
                return true;
            // CultureInfo.CurrentUICulture / InstalledUICulture. Two functions, not one:
            // the first is a settable slot, the second is the OS's own language and no
            // setter moves it (see the runtime note at dn2cpp_culture_current_ui for what
            // reads this axis and what deliberately does not).
            case ("System.Globalization.CultureInfo", "get_CurrentUICulture"):
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", "dn2cpp_culture_current_ui()");
                return true;
            case ("System.Globalization.CultureInfo", "get_InstalledUICulture"):
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", "dn2cpp_culture_installed_ui()");
                return true;
            // NumberFormatInfo.InvariantInfo: a DISTINCT isNfi-tagged singleton
            // (same field values), so an InvariantInfo escaping to `object`
            // through an IFormatProvider-typed slot reports the NumberFormatInfo
            // identity .NET reports (dn2cpp_nfi_wrap's PROVIDER kind reads the
            // tag; the CultureInfo singleton above is untagged).
            case ("System.Globalization.NumberFormatInfo", "get_InvariantInfo"):
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", "dn2cpp_nfi_invariant_info()");
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            case ("System.Globalization.CultureInfo", "get_CurrentCulture")
                or ("System.Globalization.NumberFormatInfo", "get_CurrentInfo"):
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", "dn2cpp_culture_current()");
                return true;
            // CultureInfo.CurrentCulture = value (static setter).
            case ("System.Globalization.CultureInfo", "set_CurrentCulture"):
                Emit($"dn2cpp_culture_set_current({Cast(Pop(), "const Dn2CppNumberFormatInfo*")});");
                return true;
            // CultureInfo.CurrentUICulture = value (static setter). Its own slot — it must
            // NOT route through dn2cpp_culture_set_current, because a UI-culture write may
            // not perturb get_CurrentCulture (.NET lets the two differ, and every gate
            // driver pins them separately; see dn2cpp_culture_current_ui's note).
            case ("System.Globalization.CultureInfo", "set_CurrentUICulture"):
                Emit($"dn2cpp_culture_set_current_ui({Cast(Pop(), "const Dn2CppNumberFormatInfo*")});");
                return true;
            case ("System.Globalization.CultureInfo", "GetCultureInfo")
                or ("System.Globalization.CultureInfo", "CreateSpecificCulture")
                    when sig.ParameterTypes is [{ IsString: true }]:
            {
                string nm = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", $"dn2cpp_culture_by_name({nm})");
                return true;
            }
            // CultureInfo.GetCultureInfo(int lcid) — the reverse lookup over the same
            // culture table the name overload uses: 127 is the invariant culture, a
            // modeled culture's real LCID is its own, and anything else is rejected.
            // .NET's CultureNotFoundException IS an ArgumentException and is not
            // separately modeled — dn2cpp_throw_argument carries the family, as it does
            // for the ArgumentOutOfRangeException .NET raises for a non-positive LCID, so
            // one helper serves both and a `catch (ArgumentException)` behaves alike.
            case ("System.Globalization.CultureInfo", "GetCultureInfo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var lcid = Pop();
                string r = NewTemp("const Dn2CppNumberFormatInfo*");
                Emit($"{r} = dn2cpp_culture_by_lcid({lcid.Expr});");
                Emit($"if ({r} == nullptr) dn2cpp_throw_argument();");
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", r);
                return true;
            }
            // CultureInfo.NumberFormat / CultureInfo.GetFormat(Type) /
            // (NumberFormatInfo|IFormatProvider).GetFormat are identity on the runtime
            // pointer — the receiver already is the NFI. CultureInfo.GetFormat is the
            // direct public IFormatProvider implementation ON CultureInfo, reached as a
            // plain call token (CultureInfo::GetFormat), NOT through the IFormatProvider
            // interface slot — so its own (CultureInfo, GetFormat) tuple is required
            // beside the IFormatProvider one, which never matches a direct
            // CultureInfo.GetFormat call. The (Type) argument is dropped (dn2cpp's NFI
            // stands in for NumberFormatInfo; a DateTimeFormatInfo request is served the
            // same invariant NFI, as with the sibling GetFormat routes).
            case ("System.Globalization.CultureInfo", "get_NumberFormat")
                or ("System.Globalization.CultureInfo", "GetFormat")
                or ("System.Globalization.NumberFormatInfo", "GetFormat")
                or ("System.IFormatProvider", "GetFormat"):
            {
                var recv = Pop();
                if (sig.ParameterTypes.Length == 1) Pop(); // GetFormat(Type formatType)
                // Carry the declared return type as the entry's static type:
                // get_NumberFormat returns NumberFormatInfo, so an escape of the
                // result into `object` wraps with the identity .NET reports
                // (GetFormat declares `object` and stays PROVIDER-kind).
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*",
                    Cast(recv, "const Dn2CppNumberFormatInfo*"));
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            }
            // NumberFormatInfo.GetInstance(IFormatProvider) — provider-or-current.
            // Both CultureInfo and NumberFormatInfo lower to the same runtime NFI
            // pointer, so a non-null provider is used as-is; null reads the
            // process-global current culture, matching the managed null -> CurrentInfo
            // fallback.
            case ("System.Globalization.NumberFormatInfo", "GetInstance")
                when sig.ParameterTypes.Length == 1:
            {
                string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string nfi = NewTemp("const Dn2CppNumberFormatInfo*");
                Emit($"{nfi} = {prov};");
                Emit($"if ({nfi} == nullptr) {nfi} = dn2cpp_culture_current();");
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", nfi);
                return true;
            }
            // NumberFormatInfo property getters — direct reads of the runtime NFI
            // model's fields. NumberFormatInfo is an intrinsic-mapped type, so each
            // property call needs its own arm.
            case ("System.Globalization.NumberFormatInfo", "get_NegativeSign"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->negativeSign");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_NumberDecimalSeparator"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->numberDecimal");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_NumberGroupSeparator"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->numberGroup");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_NaNSymbol"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->nan");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_PositiveInfinitySymbol"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->posInf");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_NegativeInfinitySymbol"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->negInf");
                return true;
            // PositiveSign is not carried in the runtime NFI model; every modeled
            // culture keeps the invariant "+" (the model stores only the fields
            // that actually vary across the cultures dn2cpp ships).
            case ("System.Globalization.NumberFormatInfo", "get_PositiveSign"):
                Pop(); // the NFI receiver
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("+"));
                return true;
            // PerMilleSymbol is not carried in the runtime NFI model either; every modeled
            // culture keeps the invariant U+2030 ("‰") (matching the GenericIntrinsic
            // PerMilleSymbolTChar arm, which folds the same literal).
            case ("System.Globalization.NumberFormatInfo", "get_PerMilleSymbol"):
                Pop(); // the NFI receiver
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("‰"));
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_CurrencyDecimalDigits"):
                Push(StackKind.I4, "int32_t",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->currencyDigits");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_CurrencyPositivePattern"):
                Push(StackKind.I4, "int32_t",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->currencyPosPattern");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_CurrencyNegativePattern"):
                Push(StackKind.I4, "int32_t",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->currencyNegPattern");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_PercentPositivePattern"):
                Push(StackKind.I4, "int32_t",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->percentPosPattern");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_PercentNegativePattern"):
                Push(StackKind.I4, "int32_t",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->percentNegPattern");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_CurrencySymbol"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->currencySymbol");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_CurrencyDecimalSeparator"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->currencyDecimal");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_CurrencyGroupSeparator"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->currencyGroup");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_PercentSymbol"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->percentSymbol");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_PercentDecimalSeparator"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->percentDecimal");
                return true;
            case ("System.Globalization.NumberFormatInfo", "get_PercentGroupSeparator"):
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})->percentGroup");
                return true;
            // Number/PercentDecimalDigits are not carried in the runtime NFI model;
            // every modeled culture keeps the invariant default of 2.
            case ("System.Globalization.NumberFormatInfo", "get_NumberDecimalDigits")
                or ("System.Globalization.NumberFormatInfo", "get_PercentDecimalDigits"):
                Pop(); // the NFI receiver
                Push(StackKind.I4, "int32_t", "2");
                return true;
            // NumberNegativePattern is not carried in the runtime NFI model; every
            // modeled culture keeps the invariant default of 1 ("-n").
            case ("System.Globalization.NumberFormatInfo", "get_NumberNegativePattern"):
                Pop(); // the NFI receiver
                Push(StackKind.I4, "int32_t", "1");
                return true;
            // HasInvariantNumberSigns — the parser's fast-path test that the
            // culture's signs are the invariant "+"/"-". The runtime NFI model
            // keeps PositiveSign invariant for every culture (see get_PositiveSign
            // above), so only the negative sign decides; a model without an
            // explicit negative sign is the invariant "-".
            case ("System.Globalization.NumberFormatInfo", "get_HasInvariantNumberSigns"):
            {
                string invNfi = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string inv = NewTemp("int32_t");
                Emit($"{{ Dn2CppString* _ns = ({invNfi})->negativeSign; "
                     + $"{inv} = (_ns == nullptr || (_ns->length == 1 && _ns->chars[0] == (char16_t)0x2D)) ? 1 : 0; }}");
                Push(StackKind.I4, "int32_t", inv);
                return true;
            }
            // AllowHyphenDuringParsing — whether the culture's negative sign is one
            // of the typographic minus variants, in which case the parser also
            // accepts an ASCII hyphen. Computed from the runtime NFI model's
            // negative sign, mirroring the managed switch (false for the invariant
            // "-").
            case ("System.Globalization.NumberFormatInfo", "AllowHyphenDuringParsing")
                or ("System.Globalization.NumberFormatInfo", "get_AllowHyphenDuringParsing"):
            {
                string hnfi = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string hyph = NewTemp("int32_t");
                Emit($"{{ Dn2CppString* _ns = ({hnfi})->negativeSign; "
                     + "char16_t _nc = (_ns != nullptr && _ns->length == 1) ? _ns->chars[0] : (char16_t)0; "
                     + $"{hyph} = (_nc == 0x2012 || _nc == 0x207B || _nc == 0x208B || _nc == 0x2212 "
                     + "|| _nc == 0x2796 || _nc == 0xFE63 || _nc == 0xFF0D) ? 1 : 0; }");
                Push(StackKind.I4, "int32_t", hyph);
                return true;
            }
            // ValidateParseStyleInteger / ValidateParseStyleFloatingPoint(NumberStyles) —
            // static argument guards that throw only on caller-supplied invalid style
            // combinations (hex/binary specifiers on a float parse; the two together, or
            // either with non-hex flags, on an integer parse). The transpiled parse
            // entries pass the fixed valid default, so both are no-ops here.
            case ("System.Globalization.NumberFormatInfo", "ValidateParseStyleInteger"):
            case ("System.Globalization.NumberFormatInfo", "ValidateParseStyleFloatingPoint"):
                Pop(); // the NumberStyles argument
                return true;
            // CultureInfo.CompareInfo — a synthesized zero-initialized instance of the
            // transpiled CompareInfo. The real getter's `new CompareInfo(this)` reads
            // fields of the intrinsic-mapped CultureInfo, so it cannot transpile; a zeroed
            // instance is sound because under the invariant-globalization fold the
            // reachable CompareInfo members run their pure-managed invariant arms, which
            // read no instance state. Fresh per call — no reached path compares instances
            // by identity. The type-info emits via the reach-side hook in
            // Compilation.ScanBodyForGenerics.
            case ("System.Globalization.CultureInfo", "get_CompareInfo")
                when sig.ReturnType is { Kind: TypeKind.Class, Class: { } cmpCls }:
            {
                Pop(); // the CultureInfo receiver (the NFI model carries no comparer state)
                NoteReferenceClass(sig.ReturnType);
                string o = NewTemp(cmpCls.CppStructName + "*");
                Emit($"{o} = ({cmpCls.CppStructName}*)dn2cpp_alloc(sizeof({cmpCls.CppStructName}));");
                Emit($"((Dn2CppObject*){o})->type = &{cmpCls.CppTypeInfoName};");
                Push(StackKind.Ref, cmpCls.CppStructName + "*", o);
                return true;
            }
            // CultureInfo.DateTimeFormat — the real getter reads fields of the intrinsic-
            // mapped CultureInfo (Dn2CppNumberFormatInfo) and cannot transpile; dn2cpp's
            // culture is always invariant, so synthesize a bare invariant DateTimeFormatInfo.
            // Its fields stay zero and nothing may read them raw: dn2cpp does not construct a
            // DTFI's CultureData/Calendar, so every property whose real getter would lazily
            // init off that machinery has its own intercept below (the abbreviated month/day
            // name getters among them). dn2cpp formats dates through its own runtime tables,
            // never this DTFI — here it is only a dropped IFormatProvider.
            case ("System.Globalization.CultureInfo", "get_DateTimeFormat")
                when sig.ReturnType is { Kind: TypeKind.Class, Class: { } dtfiCls }:
            {
                Pop(); // the CultureInfo receiver (always invariant in dn2cpp's culture model)
                NoteReferenceClass(sig.ReturnType);
                string o = NewTemp(dtfiCls.CppStructName + "*");
                Emit($"{o} = ({dtfiCls.CppStructName}*)dn2cpp_alloc(sizeof({dtfiCls.CppStructName}));");
                Emit($"((Dn2CppObject*){o})->type = &{dtfiCls.CppTypeInfoName};");
                Push(StackKind.Ref, dtfiCls.CppStructName + "*", o);
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_InvariantInfo")
                or ("System.Globalization.DateTimeFormatInfo", "get_CurrentInfo")
                or ("System.Globalization.DateTimeFormatInfo", "GetInstance")
                when sig.ReturnType is { Kind: TypeKind.Class, Class: { } dtfiCls }:
            {
                if (sig.ParameterTypes.Length == 1) Pop(); // GetInstance(IFormatProvider)
                NoteReferenceClass(sig.ReturnType);
                string o = NewTemp(dtfiCls.CppStructName + "*");
                Emit($"{o} = ({dtfiCls.CppStructName}*)dn2cpp_alloc(sizeof({dtfiCls.CppStructName}));");
                Emit($"((Dn2CppObject*){o})->type = &{dtfiCls.CppTypeInfoName};");
                Push(StackKind.Ref, dtfiCls.CppStructName + "*", o);
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "ReadOnly"):
            {
                // Returns the same instance
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_DecimalSeparator"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("."));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_DateSeparator"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("/"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_TimeSeparator"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd(":"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_AMDesignator"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("AM"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_PMDesignator"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("PM"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_RFC1123Pattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_SortableDateTimePattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("yyyy'-'MM'-'dd'T'HH':'mm':'ss"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_UniversalSortableDateTimePattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("yyyy'-'MM'-'dd HH':'mm':'ss'Z'"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_ShortDatePattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("MM/dd/yyyy"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_LongDatePattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("dddd, dd MMMM yyyy"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_ShortTimePattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("HH:mm"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_LongTimePattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("HH:mm:ss"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_FullDateTimePattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("dddd, dd MMMM yyyy HH:mm:ss"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_MonthDayPattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("MMMM dd"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_YearMonthPattern"):
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("yyyy MMMM"));
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_FirstDayOfWeek")
                or ("System.Globalization.DateTimeFormatInfo", "get_CalendarWeekRule"):
            {
                Pop();
                Push(StackKind.I4, "int32_t", "0");
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_AbbreviatedMonthNames")
                or ("System.Globalization.DateTimeFormatInfo", "get_AbbreviatedMonthGenitiveNames")
                when sig.ReturnType is { Kind: TypeKind.SZArray, Element: { } me }:
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppArrayRef*", $"dn2cpp_dtfi_invariant_abbrev_month_names({PreciseArrayTypeInfoExpr(me)})");
                return true;
            }
            case ("System.Globalization.DateTimeFormatInfo", "get_AbbreviatedDayNames")
                when sig.ReturnType is { Kind: TypeKind.SZArray, Element: { } de }:
            {
                Pop();
                Push(StackKind.Ref, "Dn2CppArrayRef*", $"dn2cpp_dtfi_invariant_abbrev_day_names({PreciseArrayTypeInfoExpr(de)})");
                return true;
            }
            // CultureInfo.Name / ToString — read the culture's modeled name (the
            // invariant culture and plain NumberFormatInfo read as ""). BCL code keys
            // case-folding tiers and caches off it, so the name must survive the
            // CultureInfo -> NFI lowering.
            case ("System.Globalization.CultureInfo", "get_Name")
                or ("System.Globalization.CultureInfo", "ToString")
                    when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_culture_name({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})");
                return true;
            // ---- CultureInfo object-model accessors beyond the NFI symbols ----
            // These read per-culture metadata the runtime carries no table for, and each
            // answers the INVARIANT culture's value whatever the receiver is: DisplayName
            // is UI-CULTURE-KEYED (real .NET reads ICU's localized name sets), so a
            // faithful table is cultures × UI languages and a partial one would answer
            // right on one UI culture and wrong on the next.
            //
            // CultureInfo.EnglishName deliberately has no arm at all, so it fails the
            // TRANSPILE with a named error rather than answering a constant.
            //
            // CultureInfo.NativeName — the invariant culture's native/display-name literal.
            case ("System.Globalization.CultureInfo", "get_NativeName"):
                Pop(); // the CultureInfo receiver (no display-name table modeled)
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("Invariant Language (Invariant Country)"));
                return true;
            // CultureInfo.DisplayName — under InvariantGlobalization the display name is
            // the same literal as NativeName ("Invariant Language (Invariant Country)"):
            // no localized name table is modeled, so both read the invariant culture's
            // sole display name.
            case ("System.Globalization.CultureInfo", "get_DisplayName"):
                Pop(); // the CultureInfo receiver (no display-name table modeled)
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd("Invariant Language (Invariant Country)"));
                return true;
            // CultureInfo.LCID — read from the culture. Three answers, and the
            // third is the point: 127 for the invariant culture, the real Windows LCID for
            // a culture the runtime table carries, and 4096 (LOCALE_CUSTOM_UNSPECIFIED) for
            // one it does not — which is not a stand-in but exactly what real .NET reports
            // for a culture it has no LCID for. See Dn2CppNumberFormatInfo::lcid.
            case ("System.Globalization.CultureInfo", "get_LCID"):
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_culture_lcid({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})");
                return true;
            // CultureInfo.IsNeutralCulture — read from the culture: the table carries
            // language-only rows ("en", "az-Latn") for which .NET answers true, and those
            // rows exist precisely because a bare `LANG=de` resolves to one.
            case ("System.Globalization.CultureInfo", "get_IsNeutralCulture"):
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_culture_is_neutral({Cast(Pop(), "const Dn2CppNumberFormatInfo*")})");
                return true;
            // CultureInfo.GetCultures(CultureTypes) — the invariant culture alone, even
            // though the runtime does carry a culture table (dn2cpp_culture_table.inc).
            // GetCultures is an INVENTORY question — "which cultures does this machine
            // have?" — and the table is not an inventory: it is the set of cultures whose
            // formatting SYMBOLS are modeled. Handing it back would advertise every row
            // with a DisplayName/NativeName/EnglishName reading as the invariant literal.
            // The CultureTypes filter is dropped with it. Element type CultureInfo lowers
            // to const Dn2CppNumberFormatInfo*; a ref-array slot holds the managed-object
            // form, so the element is stored WRAPPED (dn2cpp_nfi_wrap — the same conversion
            // a stelem.ref's object cast emits) and the typed ldelem unwraps it.
            case ("System.Globalization.CultureInfo", "GetCultures")
                when sig.ReturnType is { Kind: TypeKind.SZArray, Element: { } cultElem }:
            {
                Pop(); // the CultureTypes filter
                Comp.NoteArrayElementType(cultElem); // emit ti_arr_CultureInfo (no IL newarr notes it)
                string arr = NewTemp("Dn2CppArrayRef*");
                Emit($"{arr} = dn2cpp_newarr_ref_t(1, {PreciseArrayTypeInfoExpr(cultElem)});");
                Emit($"{arr}->data[0] = dn2cpp_nfi_wrap(dn2cpp_nfi_invariant(), DN2CPP_NFI_KIND_CULTURE);");
                Push(StackKind.Ref, "Dn2CppArrayRef*", arr, TypeDesc.MakeSZArray(cultElem));
                return true;
            }
            // CultureInfo.Calendar — the invariant culture's calendar is GregorianCalendar,
            // but dn2cpp constructs no Calendar object: the real getter reads CultureInfo
            // fields (intrinsic-mapped away), and a synthesized GregorianCalendar would pull
            // the unmodeled CalendarData/globalization subtree its GetWeekOfYear/IsLeapYear
            // members consume. So this traps loudly (catchable PlatformNotSupportedException)
            // rather than returning a null Calendar a caller would dereference — the
            // "a stripped type throws, never answers empty" discipline (AGENTS.md).
            case ("System.Globalization.CultureInfo", "get_Calendar"):
                Pop(); // the CultureInfo receiver
                Emit("dn2cpp_throw_platform_not_supported(\"CultureInfo.Calendar: dn2cpp does not construct a Calendar "
                     + "object (globalization is invariant-only); the GregorianCalendar members are unavailable\");");
                Push(StackKind.Ref, "Dn2CppObject*", "((Dn2CppObject*)nullptr)"); // unreachable; stack typing only
                return true;
            // CultureInfo.TextInfo — identity on the runtime pointer (TextInfo carries the
            // same invariant culture model).
            case ("System.Globalization.CultureInfo", "get_TextInfo"):
            {
                var recv = Pop();
                // The declared return type (TextInfo) rides as the static type so
                // an escape into `object` wraps with the TextInfo identity.
                Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*",
                    Cast(recv, "const Dn2CppNumberFormatInfo*"));
                Top = Top with { StaticType = sig.ReturnType };
                return true;
            }
            // TextInfo.ListSeparator — the invariant culture's list separator is "," (no
            // OS locale is modeled, so CurrentUICulture folds to invariant).
            case ("System.Globalization.TextInfo", "get_ListSeparator"):
                Pop(); // the TextInfo receiver
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd(","));
                return true;
            // TextInfo casing — the TextInfo model is the invariant culture pointer (see
            // get_TextInfo), so these lower to the same invariant BMP case maps as
            // string.ToUpperInvariant; exact for the invariant culture and any culture
            // whose simple case fold coincides with it. The receiver is popped and dropped
            // (it carries no per-culture casing state) only when the arm is an instance
            // one — the *Invariant/*AsciiInvariant siblings are static. ToTitleCase is not
            // modeled and falls through to the loud miss (TextInfo is an intrinsic type).
            case ("System.Globalization.TextInfo", "ToLower")
                or ("System.Globalization.TextInfo", "ToLowerInvariant")
                or ("System.Globalization.TextInfo", "ToLowerAsciiInvariant")
                when sig.ParameterTypes is [{ IsString: true }]:
            {
                var s = Pop();
                if (sig.Header.IsInstance) Pop(); // the TextInfo receiver (if instance)
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_to_case({Cast(s, "Dn2CppString*")}, 0)");
                return true;
            }
            case ("System.Globalization.TextInfo", "ToUpper")
                or ("System.Globalization.TextInfo", "ToUpperInvariant")
                or ("System.Globalization.TextInfo", "ToUpperAsciiInvariant")
                when sig.ParameterTypes is [{ IsString: true }]:
            {
                var s = Pop();
                if (sig.Header.IsInstance) Pop(); // the TextInfo receiver (if instance)
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_to_case({Cast(s, "Dn2CppString*")}, 1)");
                return true;
            }
            case ("System.Globalization.TextInfo", "ToLower")
                or ("System.Globalization.TextInfo", "ToLowerInvariant")
                or ("System.Globalization.TextInfo", "ToLowerAsciiInvariant")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var c = Pop();
                if (sig.Header.IsInstance) Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_char_lower_invariant((char16_t)({c.Expr}))");
                return true;
            }
            case ("System.Globalization.TextInfo", "ToUpper")
                or ("System.Globalization.TextInfo", "ToUpperInvariant")
                or ("System.Globalization.TextInfo", "ToUpperAsciiInvariant")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var c = Pop();
                if (sig.Header.IsInstance) Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_char_upper_invariant((char16_t)({c.Expr}))");
                return true;
            }
            // SearchValues.Create(ReadOnlySpan<T> values) — build the runtime membership
            // set (byte / char). The real factory selects a SIMD/ProbabilisticMap subclass
            // we don't model; the set scan (see the IndexOfAny SearchValues case) replaces
            // the whole hierarchy.
            case ("System.Buffers.SearchValues", "Create")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } spanCls }]
                    && Comp.GenericDefFullName(spanCls) is "System.ReadOnlySpan" or "System.Span":
            {
                var elemTd = spanCls.Context.TypeArgs is [{ } e0] ? e0 : null;
                string suffix = SearchValuesSuffix(elemTd)
                    ?? throw new NotSupportedException(
                        $"SearchValues.Create<{elemTd}>: only byte/char element types are supported");
                string ptrCt = suffix == "u8" ? "const uint8_t*" : "const uint16_t*";
                string sv = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                string r = NewTemp("Dn2CppSearchValues*");
                Emit($"{r} = dn2cpp_search_values_create_{suffix}(({ptrCt}){sv}.f__reference, {sv}.f__length);");
                Push(StackKind.Ref, "Dn2CppSearchValues*", r);
                return true;
            }
            // SearchValues.Create(ReadOnlySpan<string> values, StringComparison) — the
            // string flavor (Regex's leading-strings prefix optimization). Only
            // Ordinal(4)/OrdinalIgnoreCase(5) exist in .NET (Create rejects the rest);
            // the OrdinalIgnoreCase fold is the exact BMP ordinal fold.
            case ("System.Buffers.SearchValues", "Create")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } sspanCls }, _]
                    && Comp.GenericDefFullName(sspanCls) is "System.ReadOnlySpan" or "System.Span"
                    && sspanCls.Context.TypeArgs is [{ IsString: true }]:
            {
                var cmp = Pop();
                string ssv = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                string r = NewTemp("Dn2CppSearchValues*");
                Emit($"{r} = dn2cpp_search_values_create_str((Dn2CppString**){ssv}.f__reference, "
                     + $"{ssv}.f__length, (({cmp.Expr}) == 5 ? 1 : 0));");
                Push(StackKind.Ref, "Dn2CppSearchValues*", r);
                return true;
            }
            // SearchValues<byte|char>.Contains(value) — the single-element membership
            // test. The real instance dispatches into the SIMD/ProbabilisticMap
            // subclass; bind it to the same scalar set the Create/IndexOfAny cases build.
            case ("System.Buffers.SearchValues", "Contains")
                when sig.ParameterTypes.Length == 1:
            {
                string cSuffix = SearchValuesSuffix(sig.ParameterTypes[0])
                    ?? throw new NotSupportedException(
                        $"SearchValues<{sig.ParameterTypes[0]}>.Contains: only byte/char element types are supported");
                var cval = Pop();
                var crecv = Pop();
                string cCast = cSuffix == "u8" ? "uint8_t" : "uint16_t";
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_search_values_contains_{cSuffix}(({cCast})({cval.Expr}), "
                    + $"{Cast(crecv, "Dn2CppSearchValues*")})");
                return true;
            }
            // SearchValues<string>.IndexOfAnyMultiString(ReadOnlySpan<char>) — the
            // instance scan MemoryExtensions.IndexOfAny(span, SearchValues<string>)
            // routes through. Leftmost start index of any candidate, -1 when none.
            case ("System.Buffers.SearchValues", "IndexOfAnyMultiString")
                when sig.ParameterTypes.Length == 1:
            {
                string span = SpanValue(Pop(), CppTypes.Of(sig.ParameterTypes[0]));
                var recv = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_search_values_index_of_any_str((const char16_t*){span}.f__reference, "
                    + $"{span}.f__length, {Cast(recv, "Dn2CppSearchValues*")})");
                return true;
            }
            // NumberFormatInfo property setters (object-initializer / fluent set).
            case ("System.Globalization.NumberFormatInfo", "set_NumberDecimalSeparator"):
                EmitNfiSet("dn2cpp_nfi_set_number_decimal"); return true;
            case ("System.Globalization.NumberFormatInfo", "set_NumberGroupSeparator"):
                EmitNfiSet("dn2cpp_nfi_set_number_group"); return true;
            case ("System.Globalization.NumberFormatInfo", "set_NegativeSign"):
                EmitNfiSet("dn2cpp_nfi_set_negative_sign"); return true;
            case ("System.Globalization.NumberFormatInfo", "set_NaNSymbol"):
                EmitNfiSet("dn2cpp_nfi_set_nan"); return true;
            case ("System.Globalization.NumberFormatInfo", "set_PositiveInfinitySymbol"):
                EmitNfiSet("dn2cpp_nfi_set_pos_inf"); return true;
            case ("System.Globalization.NumberFormatInfo", "set_NegativeInfinitySymbol"):
                EmitNfiSet("dn2cpp_nfi_set_neg_inf"); return true;
            case ("System.Globalization.NumberFormatInfo", "set_PercentSymbol"):
                EmitNfiSet("dn2cpp_nfi_set_percent_symbol"); return true;
            case ("System.Globalization.NumberFormatInfo", "set_CurrencySymbol"):
                EmitNfiSet("dn2cpp_nfi_set_currency_symbol"); return true;
            // set_CurrencyDecimalSeparator / set_CurrencyGroupSeparator — PLAIN FIELD
            // WRITES into the per-instance mutable copy, exactly like the number-separator
            // and CurrencySymbol siblings above. `new NumberFormatInfo()` is a fresh
            // GC-heap copy of the invariant (dn2cpp_nfi_new), so honoring the write is both
            // safe and correct: library code does set a non-invariant currency separator,
            // and a validate-and-trap would turn that live write into a runtime
            // PlatformNotSupportedException. Writing a shared culture singleton is outside
            // the modeled surface, the same caveat the other plain-write setters carry.
            case ("System.Globalization.NumberFormatInfo", "set_CurrencyDecimalSeparator"):
            {
                string val = Cast(Pop(), "Dn2CppString*");
                string recv = Cast(Pop(), "Dn2CppNumberFormatInfo*");
                Emit($"{recv}->currencyDecimal = {val};");
                return true;
            }
            case ("System.Globalization.NumberFormatInfo", "set_CurrencyGroupSeparator"):
            {
                string val = Cast(Pop(), "Dn2CppString*");
                string recv = Cast(Pop(), "Dn2CppNumberFormatInfo*");
                Emit($"{recv}->currencyGroup = {val};");
                return true;
            }
            // set_CurrencyDecimalDigits(int) — PLAIN FIELD WRITE (the currencyDigits slot
            // exists and get_CurrencyDecimalDigits already reads it, so this round-trips).
            // Only "C"-format output consults it.
            case ("System.Globalization.NumberFormatInfo", "set_CurrencyDecimalDigits"):
            {
                var val = Pop();
                string recv = Cast(Pop(), "Dn2CppNumberFormatInfo*");
                Emit($"{recv}->currencyDigits = (int32_t)({val.Expr});");
                return true;
            }
            // set_{Number,Currency,Percent}GroupSizes(int[]) — VALIDATE-AND-TRAP, like the
            // separator setters above. The runtime carries ONE array serving all three
            // properties (a culture row's three are equal by construction), so honoring a
            // write to one would silently move the other two. A write of the value the
            // model already holds is a true no-op; anything else traps loudly with a
            // catchable PlatformNotSupportedException naming the property.
            case ("System.Globalization.NumberFormatInfo", "set_CurrencyGroupSizes"):
            case ("System.Globalization.NumberFormatInfo", "set_NumberGroupSizes"):
            case ("System.Globalization.NumberFormatInfo", "set_PercentGroupSizes"):
            {
                string arr = Cast(Pop(), "Dn2CppArrayI4*");
                string recv = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                Emit($"dn2cpp_nfi_set_group_sizes({recv}, {arr}, \"{name["set_".Length..]}\");");
                return true;
            }
            // get_{Number,Currency,Percent}GroupSizes() — the modeled array as a fresh
            // int[], the defensive copy real .NET's getter returns (two reads are not
            // reference-equal, and writing through the result does not move the culture's
            // formatting). The same helper backs the internal System.Number readers, so
            // the public property and the format path can never disagree.
            case ("System.Globalization.NumberFormatInfo", "get_CurrencyGroupSizes"):
            case ("System.Globalization.NumberFormatInfo", "get_NumberGroupSizes"):
            case ("System.Globalization.NumberFormatInfo", "get_PercentGroupSizes"):
            {
                string recv = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string gsArr = NewTemp("Dn2CppArrayI4*");
                Emit($"{gsArr} = dn2cpp_nfi_group_sizes({recv}, {PrimArrayTypeInfoExpr(PrimitiveTypeCode.Int32)});");
                Push(StackKind.Ref, "Dn2CppArrayI4*", gsArr);
                return true;
            }
            // set_NumberDecimalDigits(int) — ACCEPT-AND-IGNORE. Unlike the currency-digit
            // slot there is NO numberDecimalDigits field: the model keeps every culture at
            // the invariant 2 (get_NumberDecimalDigits below returns that constant), and the
            // formatters this model localizes ("G"/round-trip, and "N"/"F" with an explicit
            // precision) never consult it. So the write is accepted and discarded rather
            // than trapped — library code sets it as a "max precision" sentinel, and
            // trapping would raise a runtime exception while changing no reachable output.
            // The value is not read back, so no consumer sees a stored-but-unhonored number.
            case ("System.Globalization.NumberFormatInfo", "set_NumberDecimalDigits"):
            {
                Pop();  // the int value — accepted, not carried by the model (see above)
                Pop();  // the receiver
                return true;
            }
            // Deref the receiver at the real 1-byte width (SubWordCppType): a bool* into
            // packed storage (a bool[] element, a narrowed struct field) must read one
            // byte, not an int32 that splices in the adjacent bytes.
            case ("System.Boolean", "ToString") when sig.ParameterTypes.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_bool_to_string({DerefReceiver(SubWordCppType(declType))})");
                return true;
            // bool.TryParse(string, out bool): "True"/"False" ordinal-case-insensitive
            // after trimming (e.g. AppContext switch values).
            case ("System.Boolean", "TryParse")
                when sig.ParameterTypes is [{ IsString: true }, { Kind: TypeKind.ByRef }]:
            {
                var outRef = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_bool_tryparse({Cast(Pop(), "Dn2CppString*")}, (uint8_t*)({outRef.Expr}))");
                return true;
            }
            // bool.Parse(string): the throwing form over the same trim + ordinal-case
            // matcher — a null input raises ArgumentNullException, anything the TryParse
            // matcher rejects raises FormatException, matching .NET.
            case ("System.Boolean", "Parse") when sig.ParameterTypes is [{ IsString: true }]:
            {
                string s = NewTemp("Dn2CppString*");
                Emit($"{s} = {Cast(Pop(), "Dn2CppString*")};");
                Emit($"if ({s} == nullptr) dn2cpp_throw_argument_null();");
                string b = NewTemp("uint8_t");
                Emit($"if (!dn2cpp_bool_tryparse({s}, &{b})) dn2cpp_throw_format();");
                Push(StackKind.I4, "int32_t", $"(int32_t){b}");
                return true;
            }
            // bool.IsTrueStringIgnoreCase / IsFalseStringIgnoreCase(ReadOnlySpan<char>):
            // the exact-literal ordinal-case-insensitive matchers behind Boolean.Parse
            // and the AppContext config readers. No trimming — .NET matches exactly the
            // 4/5 code units.
            case ("System.Boolean", "IsTrueStringIgnoreCase" or "IsFalseStringIgnoreCase")
                when sig.ParameterTypes.Length == 1:
            {
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]); // ReadOnlySpan<char>
                string sp = SpanPtr(Pop(), spanCt);
                string fn = name == "IsTrueStringIgnoreCase"
                    ? "dn2cpp_bool_is_true_chars"
                    : "dn2cpp_bool_is_false_chars";
                Push(StackKind.I4, "int32_t",
                    $"{fn}((const char16_t*){sp}->f__reference, {sp}->f__length)");
                return true;
            }
            // Exception.GetMessageFromNativeResources(kind): the localized default messages
            // for OOM/StackOverflow/etc. live in native resources dn2cpp has none of —
            // return the empty string (the default-message accessor is rarely observed and
            // never in an AOT image without those resources).
            case ("System.Exception", "GetMessageFromNativeResources"):
                Pop(); // the ExceptionMessageKind enum argument
                Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd(""));
                return true;
            // INumber static Min/Max(x, y) — the per-primitive min/max reached by
            // System.Runtime.Intrinsics.Scalar<T>'s typeof(T) branches in the generic-math
            // Min/Max path (each branch dead for the non-matching T). The Double/Single
            // forms route through the shared Math map above (the dn2cpp_math_min/max
            // templates — NaN propagates and -0.0 orders below +0.0, unlike
            // std::fmin/fmax); the integer types take a three-way with the right
            // signedness here. Sub-word integers promote to Int32.
            case ("System.Int32" or "System.Int64" or "System.UInt32" or "System.UInt64", "Min" or "Max")
                when sig.ParameterTypes.Length == 2:
            {
                bool isMax = name == "Max";
                bool wide = declType is "System.Int64" or "System.UInt64";
                bool uns = declType is "System.UInt32" or "System.UInt64";
                string sct = wide ? "int64_t" : "int32_t";
                string cct = uns ? (wide ? "uint64_t" : "uint32_t") : sct;
                var b = Pop(); var a = Pop();
                string ca = $"({sct})({a.Expr})", cb = $"({sct})({b.Expr})";
                string op = isMax ? ">" : "<";
                Push(wide ? StackKind.I8 : StackKind.I4, sct,
                    $"((({cct}){ca}) {op} (({cct}){cb}) ? ({ca}) : ({cb}))");
                return true;
            }
            // Object.ToString on an object reference: a boxed primitive formats by
            // its runtime type, a string returns itself, anything else its type name
            // (.NET default). Value-type receivers reach here boxed.
            //
            // This is THE virtual-dispatch mouth: Roslyn emits every `x.ToString()`
            // against the least-overridden System.Object::ToString, so a call on a null
            // receiver arrives here. It must therefore name dn2cpp_object_tostring_virtual
            // and not the plain formatter, whose null arm folds to string.Empty for the
            // concat callers it shares (invariant written out at the runtime definition).
            case ("System.Object", "ToString") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                // An NFI-typed receiver (CultureInfo/NumberFormatInfo/TextInfo/
                // IFormatProvider — the raw headerless pointer, on which the vtable
                // dispatch below would read garbage): route through the interned
                // wrapper, whose header dispatches the .NET answer — the culture NAME
                // for the CultureInfo identity (the fold the CultureInfo.ToString case
                // above makes), the full type name for NumberFormatInfo/TextInfo (no
                // override in .NET).
                if (IsNfiCppType(o.CppType))
                {
                    // dn2cpp_nfi_wrap passes a null pointer through as a null
                    // wrapper, so the virtual entry point still raises the NRE that
                    // `((CultureInfo)null).ToString()` is in real .NET.
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_object_tostring_virtual({NfiWrapExpr(o)})");
                    return true;
                }
                // An Assembly/Module receiver rides as the raw simple-name handle
                // (const char*, no object header — the vtable dispatch below would
                // read garbage). Roslyn binds x.ToString() to Object::ToString even
                // though Assembly/Module override it, so the receiver's static type
                // picks the .NET answer: Module.ToString() is the module file name,
                // Assembly.ToString() the display FullName (the default when the
                // static type is unknown — Assembly handles are the common case).
                if (o.CppType == "const char*")
                {
                    bool isModule = o.StaticType is
                        { Kind: TypeKind.Class, Class.FullName: "System.Reflection.Module" }
                        or { Kind: TypeKind.External, ExternalName: "System.Reflection.Module" };
                    Push(StackKind.Ref, "Dn2CppString*", isModule
                        ? $"dn2cpp_module_name({o.Expr})"
                        : $"dn2cpp_assembly_full_name({o.Expr})");
                    return true;
                }
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_object_tostring_virtual({Cast(o, "Dn2CppObject*")})");
                return true;
            }
            // Object.GetHashCode / Object.Equals(object) on a reference (or boxed
            // value) — dispatch the type's override via the type-info slots, else
            // fall back to identity hash / reference equality. A value-type
            // receiver arrives here boxed (the callvirt boxes it), matching the
            // boxed payload the wired thunks expect.
            case ("System.Object", "GetHashCode") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_gethashcode({Cast(o, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Object", "Equals") when sig.ParameterTypes is [{ IsObject: true }]:
            {
                var other = Pop();
                var o = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_equals({Cast(o, "Dn2CppObject*")}, {Cast(other, "Dn2CppObject*")})");
                return true;
            }
            // Static Object.Equals(objA, objB): no receiver, two boxed args. The
            // runtime helper already implements the exact static semantics — reference/both-
            // null equal, exactly-one-null unequal, else the runtime type's virtual Equals.
            case ("System.Object", "Equals") when sig.ParameterTypes is [{ IsObject: true }, { IsObject: true }]:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_equals({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})");
                return true;
            }
            // Static Object.ReferenceEquals(objA, objB): pointer identity. Roslyn
            // folds direct calls to a ceq, so this is reached only through IL that
            // kept the call — notably ldftn (ReferenceEquals bound as a delegate
            // method group), where the wrapper synthesizer replays this lowering.
            case ("System.Object", "ReferenceEquals") when sig.ParameterTypes is [{ IsObject: true }, { IsObject: true }]:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"(({Cast(a, "Dn2CppObject*")}) == ({Cast(b, "Dn2CppObject*")}) ? 1 : 0)");
                return true;
            }
            // Object.MemberwiseClone(): shallow copy — same runtime type, bit-copied
            // payload (reference fields shared). The runtime helper covers fixed-size
            // class instances; strings/arrays never reach it (their managed Clone
            // overrides return this / use array copy).
            case ("System.Object", "MemberwiseClone") when sig.ParameterTypes.Length == 0:
            {
                var o = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_object_memberwise_clone({Cast(o, "Dn2CppObject*")})");
                return true;
            }
            // Object.Finalize(): the base call a C# ~T() destructor's compiler-generated
            // try/finally always emits. Object.Finalize's real body is an empty
            // placeholder for the override chain to terminate on, so this is a pure no-op.
            case ("System.Object", "Finalize") when sig.ParameterTypes.Length == 0:
            {
                Pop();
                return true;
            }

            // ---- Primitive Int32/UInt32 CompareTo / Int32 GetHashCode ----
            // Int32/UInt32 are already s_intrinsicTypes members, so Compilation.ResolveCallTarget
            // already cuts their BCL bodies — this is emit-only. The receiver of a primitive
            // instance call arrives as a managed pointer (ldarga/ldloca/ldflda -> StackKind.Ptr)
            // or the value itself (DerefReceiver handles both); the argument is the by-value
            // operand on top of the stack, so pop it BEFORE deref-ing the receiver underneath.
            // Both operands are spilled to temps because each appears multiple times in the
            // comparison ladder.
            //
            // int.CompareTo(int) returns strictly -1/0/1 (the SIGN, not the difference —
            // 2000000000.CompareTo(-2000000000) == 1, never an overflowing subtraction).
            case ("System.Int32", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                string other = NewTemp("int32_t");
                Emit($"{other} = {Cast(Pop(), "int32_t")};");
                string self = NewTemp("int32_t");
                Emit($"{self} = {DerefReceiver("int32_t")};");
                Push(StackKind.I4, "int32_t", $"({self} < {other} ? -1 : ({self} > {other} ? 1 : 0))");
                return true;
            }
            // uint.CompareTo(uint): same -1/0/1 sign result, but with UNSIGNED comparison — the
            // stack/slot holds an int32 bit pattern, so reinterpret both as uint32_t before
            // comparing (0x80000000.CompareTo(0x7FFFFFFF) == 1).
            case ("System.UInt32", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 }]:
            {
                string other = NewTemp("uint32_t");
                Emit($"{other} = (uint32_t)({Cast(Pop(), "int32_t")});");
                string self = NewTemp("uint32_t");
                Emit($"{self} = (uint32_t)({DerefReceiver("int32_t")});");
                Push(StackKind.I4, "int32_t", $"({self} < {other} ? -1 : ({self} > {other} ? 1 : 0))");
                return true;
            }
            // long.CompareTo(long): the same -1/0/1 sign contract as int.CompareTo —
            // real .NET compares and returns the sign, never a (truncating) difference.
            case ("System.Int64", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 }]:
            {
                string other = NewTemp("int64_t");
                Emit($"{other} = {Cast(Pop(), "int64_t")};");
                string self = NewTemp("int64_t");
                Emit($"{self} = {DerefReceiver("int64_t")};");
                Push(StackKind.I4, "int32_t", $"({self} < {other} ? -1 : ({self} > {other} ? 1 : 0))");
                return true;
            }
            // ulong.CompareTo(ulong): the uint arm at 64 bits — the -1/0/1 sign of an
            // UNSIGNED comparison, reinterpreting the int64 bit pattern the stack/slot
            // holds (0x8000000000000000.CompareTo(0x7FFFFFFFFFFFFFFF) == 1).
            case ("System.UInt64", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 }]:
            {
                string other = NewTemp("uint64_t");
                Emit($"{other} = (uint64_t)({Cast(Pop(), "int64_t")});");
                string self = NewTemp("uint64_t");
                Emit($"{self} = (uint64_t)({DerefReceiver("int64_t")});");
                Push(StackKind.I4, "int32_t", $"({self} < {other} ? -1 : ({self} > {other} ? 1 : 0))");
                return true;
            }
            // float/double.CompareTo: sign result with .NET's total-order NaN handling —
            // NaN sorts below everything (including -inf) and NaN.CompareTo(NaN) == 0.
            // `x != x` is the NaN test.
            case ("System.Single", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }]:
            case ("System.Double", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double }]:
            {
                string ct = declType == "System.Single" ? "float" : "double";
                string other = NewTemp(ct);
                Emit($"{other} = {Cast(Pop(), ct)};");
                string self = NewTemp(ct);
                Emit($"{self} = {DerefReceiver(ct)};");
                Push(StackKind.I4, "int32_t",
                    $"({self} < {other} ? -1 : ({self} > {other} ? 1 : ({self} == {other} ? 0 : ({self} != {self} ? ({other} != {other} ? 0 : -1) : 1))))");
                return true;
            }
            // char.CompareTo(char): real .NET returns the raw difference m_value - value,
            // NOT a clamped sign — and that difference is observable (SpanHelpers.
            // SequenceCompareTo<char> propagates it as its return value, so string
            // ordinal comparison surfaces it). Both code units are zero-extended into
            // [0,65535] int32 slots, so the int subtraction reproduces .NET exactly.
            case ("System.Char", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                string other = NewTemp("int32_t");
                Emit($"{other} = {Cast(Pop(), "int32_t")};");
                string self = NewTemp("int32_t");
                // Deref the receiver at the real 2-byte width (SubWordCppType): a
                // char16_t* receiver (ldflda of a narrowed struct field, or a span/array
                // ref) must read one code unit, or an int32 deref splices the neighboring
                // field into the comparison. The int32 temp zero-extends.
                Emit($"{self} = {DerefReceiver(SubWordCppType(declType))};");
                Push(StackKind.I4, "int32_t", $"({self} - {other})");
                return true;
            }
            // bool.CompareTo(bool): false < true, returning strictly -1/0/1 (real .NET
            // compares m_value against value and returns the sign). Both operands are
            // normalized to 0/1 first — C# only ever materializes those, and normalizing
            // keeps any unsafe-minted nonzero byte comparing as `true`. Deref the
            // receiver at the real 1-byte width (SubWordCppType): a bool* into packed
            // storage (a bool[] element, a narrowed struct field) must read one byte.
            case ("System.Boolean", "CompareTo")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]:
            {
                string other = NewTemp("int32_t");
                Emit($"{other} = (({Cast(Pop(), "int32_t")}) != 0 ? 1 : 0);");
                string self = NewTemp("int32_t");
                Emit($"{self} = (({DerefReceiver(SubWordCppType(declType))}) != 0 ? 1 : 0);");
                Push(StackKind.I4, "int32_t", $"({self} < {other} ? -1 : ({self} > {other} ? 1 : 0))");
                return true;
            }
            // float/double.Equals(T) and .GetHashCode: the two runtime helpers the boxed
            // arm of dn2cpp_object_equals/_gethashcode and the transpiler's inline key
            // emit already share, so a value compared or hashed through any of the three
            // lands in the same bucket and answers the same way. Neither is `==` or a
            // cast: NaN equals NaN (and hashes to one value), +0 equals -0 (and hashes
            // with it), and truncating a double to int32 for a hash is undefined behavior
            // for a NaN outright. The CompareTo siblings are above; the ordering of a NaN
            // is a total order and disagrees with the equality on purpose.
            case ("System.Double", "GetHashCode") when sig.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", $"dn2cpp_double_hash({DerefReceiver("double")})");
                return true;
            case ("System.Single", "GetHashCode") when sig.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", $"dn2cpp_single_hash({DerefReceiver("float")})");
                return true;
            case ("System.Double", "Equals")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double }]:
            case ("System.Single", "Equals")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }]:
            {
                bool dbl = declType == "System.Double";
                string ct = dbl ? "double" : "float";
                string other = NewTemp(ct);
                Emit($"{other} = {Cast(Pop(), ct)};");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_{(dbl ? "double" : "single")}_equals({DerefReceiver(ct)}, {other})");
                return true;
            }
            // The Object-virtual sibling: `obj is double d && Equals(d)` — a boxed value
            // of the SAME type, then the value equality above. A boxed float never equals
            // a boxed double at the same value, matching .NET (and the boxed arm of
            // dn2cpp_object_equals, whose test this is).
            case ("System.Double", "Equals") or ("System.Single", "Equals")
                    when sig.ParameterTypes is [{ IsObject: true }]:
            {
                bool dbl = declType == "System.Double";
                string ct = dbl ? "double" : "float";
                string ti = dbl ? "&dn2cpp_double_type" : "&dn2cpp_single_type";
                string ob = NewTemp("Dn2CppObject*");
                Emit($"{ob} = {Cast(Pop(), "Dn2CppObject*")};");
                string self = NewTemp(ct);
                Emit($"{self} = {DerefReceiver(ct)};");
                Push(StackKind.I4, "int32_t",
                    $"(({ob}) != nullptr && ({ob})->type == {ti}"
                    + $" ? dn2cpp_{(dbl ? "double" : "single")}_equals({self}, *({ct}*)(({ob}) + 1)) : 0)");
                return true;
            }
            // Integer-primitive self-typed Equals(T) — the IEquatable<T> surface as a
            // direct statically-typed call (`x.Equals(y)`; the constrained.callvirt
            // generic-devirtualization path resolves through EqualityEqualsExpr in
            // MethodCompiler.Call.cs instead and never lands here). The Double/Single
            // template above, but plain `==`: integers have no NaN/-0, and a bit
            // pattern's signedness cannot change its equality, so one compare at the
            // stack-slot width serves the signed and unsigned twin alike.
            case ("System.Int32", "Equals")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            case ("System.UInt32", "Equals")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 }]:
            {
                string other = NewTemp("int32_t");
                Emit($"{other} = {Cast(Pop(), "int32_t")};");
                Push(StackKind.I4, "int32_t", $"(({DerefReceiver("int32_t")}) == ({other}) ? 1 : 0)");
                return true;
            }
            case ("System.Int64", "Equals")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 }]:
            case ("System.UInt64", "Equals")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 }]:
            {
                string other = NewTemp("int64_t");
                Emit($"{other} = {Cast(Pop(), "int64_t")};");
                Push(StackKind.I4, "int32_t", $"(({DerefReceiver("int64_t")}) == ({other}) ? 1 : 0)");
                return true;
            }
            // bool.Equals(bool): both sides normalized to 0/1 (the CompareTo posture
            // above), then compared. Deref the receiver at the real 1-byte width.
            case ("System.Boolean", "Equals")
                    when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]:
            {
                string other = NewTemp("int32_t");
                Emit($"{other} = (({Cast(Pop(), "int32_t")}) != 0 ? 1 : 0);");
                Push(StackKind.I4, "int32_t",
                    $"((({DerefReceiver(SubWordCppType(declType))}) != 0 ? 1 : 0) == ({other}) ? 1 : 0)");
                return true;
            }
            // int.GetHashCode: real .NET returns `this` (the value itself).
            case ("System.Int32", "GetHashCode") when sig.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", $"({DerefReceiver("int32_t")})");
                return true;
            // uint.GetHashCode: real .NET returns (int)m_value — the same bit pattern.
            case ("System.UInt32", "GetHashCode") when sig.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", $"({DerefReceiver("int32_t")})");
                return true;
            // bool.GetHashCode: real .NET returns `m_value ? 1 : 0`. Deref the receiver
            // at the real 1-byte width (a bool* into packed storage reads one byte).
            case ("System.Boolean", "GetHashCode") when sig.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", $"(({DerefReceiver(SubWordCppType(declType))}) != 0 ? 1 : 0)");
                return true;
            // UInt64.GetHashCode folds the two 32-bit halves (low ^ high), matching
            // .NET's `(int)m_value ^ (int)(m_value >> 32)`.
            case ("System.UInt64", "GetHashCode") when sig.ParameterTypes.Length == 0:
            {
                string h = NewTemp("uint64_t");
                Emit($"{h} = {DerefReceiver("uint64_t")};");
                Push(StackKind.I4, "int32_t", $"(int32_t)(({h}) ^ (({h}) >> 32))");
                return true;
            }
            // Int64.GetHashCode: the same low ^ high 32-bit fold — .NET's
            // `unchecked((int)m_value) ^ (int)(m_value >> 32)` (the arithmetic shift's
            // low 32 bits equal the logical shift's, so the unsigned fold is exact).
            case ("System.Int64", "GetHashCode") when sig.ParameterTypes.Length == 0:
            {
                string h = NewTemp("uint64_t");
                Emit($"{h} = (uint64_t){DerefReceiver("int64_t")};");
                Push(StackKind.I4, "int32_t", $"(int32_t)(({h}) ^ (({h}) >> 32))");
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>Whether <paramref name="t"/> is <c>System.Span&lt;T&gt;</c> closed over the
    /// given primitive element. The TryFormat destination discriminator: the char16 and
    /// UTF-8 span-write formatters share an otherwise identical parameter shape.</summary>
    private bool IsSpanOfPrimitive(TypeDesc t, PrimitiveTypeCode elem) =>
        t is { Kind: TypeKind.Class, Class: { } cls }
        && Comp.GenericDefFullName(cls) == "System.Span"
        && cls.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: var p }]
        && p == elem;

    /// <summary>The integer-primitive span-write formatter body, shared by the
    /// ISpanFormattable (Span&lt;char&gt;) and IUtf8SpanFormattable (Span&lt;byte&gt;,
    /// <paramref name="utf8"/>) arms: lower to dn2cpp_try_format_int|uint[_utf8]_c, the same
    /// byteWidth-aware formatter cores as ToString(format), writing into the span's buffer
    /// with .NET semantics (fits ⇒ write + count + true; too short ⇒ buffer untouched +
    /// 0 + false). format span -> Dn2CppString* (empty = "G"); provider routed
    /// (null = current culture, .NET's rule).</summary>
    private void EmitIntegerTryFormat(string declType, MethodSignature<TypeDesc> sig, bool utf8)
    {
        // Stack (top → bottom): provider, format(span), charsWritten(ByRef),
        // destination(span), receiver. Pop in that order; receiver last.
        string prov = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
        string fmtSpanCt = CppTypes.Of(sig.ParameterTypes[2]);  // ReadOnlySpan<char>
        string fmtPtr = SpanPtr(Pop(), fmtSpanCt);
        string fmt = NewTemp("Dn2CppString*");
        Emit($"{fmt} = dn2cpp_string_from_chars((const char16_t*){fmtPtr}->f__reference, {fmtPtr}->f__length);");
        string wrote = Cast(Pop(), "int32_t*");                  // out int chars/bytesWritten
        string destSpanCt = CppTypes.Of(sig.ParameterTypes[0]); // Span<char> / Span<byte>
        string destPtr = SpanPtr(Pop(), destSpanCt);
        // recvCt is the receiver's REAL width so a managed-pointer receiver into
        // packed sub-word storage reads exactly one element (see SubWordCppType).
        // The masked widening below normalizes it to int/long.
        (bool uns, int width, string recvCt) = declType switch
        {
            "System.Byte" => (true, 1, "uint8_t"),
            "System.SByte" => (false, 1, "int8_t"),
            "System.Int16" => (false, 2, "int16_t"),
            "System.UInt16" => (true, 2, "uint16_t"),
            "System.Int32" => (false, 4, "int32_t"),
            "System.UInt32" => (true, 4, "int32_t"),
            "System.Int64" => (false, 8, "int64_t"),
            _ => (true, 8, "int64_t"), // System.UInt64
        };
        // Mask sub-word/unsigned receivers to their value range before widening
        // (a sign-extended int32 slot holding a Byte/UInt16 must stay positive,
        // and width-correct hex needs the unsigned helper's masking input).
        string recv = DerefReceiver(recvCt);
        string helper = (uns ? "dn2cpp_try_format_uint" : "dn2cpp_try_format_int")
            + (utf8 ? "_utf8" : "") + "_c";
        string destCast = utf8 ? "char*" : "char16_t*";
        string valExpr = uns
            ? (width switch
            {
                1 => $"(uint64_t)(uint8_t)({recv})",
                2 => $"(uint64_t)(uint16_t)({recv})",
                4 => $"(uint64_t)(uint32_t)({recv})",
                _ => $"(uint64_t)({recv})",
            })
            : $"(int64_t)({recv})";
        Push(StackKind.I4, "int32_t",
            $"({helper}({valExpr}, {width}, {fmt}, ({destCast}){destPtr}->f__reference, {destPtr}->f__length, {wrote}, {prov}) ? 1 : 0)");
    }
}
