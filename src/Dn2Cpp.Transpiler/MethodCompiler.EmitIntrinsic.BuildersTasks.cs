using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitBuildersTasksIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        switch (declType, name)
        {

            // ---- Interlocked (real hardware atomics) ----
            case ("System.Threading.Interlocked", "CompareExchange")
                when sig.ParameterTypes is [{ Kind: TypeKind.ByRef }, _, _]:
            {
                // Non-generic overloads: ref object/string routes to the pointer-atomic
                // helper, every primitive to the atomic of ITS declared width — no
                // fallthrough to a wider helper. (The generic CompareExchange<T> is
                // handled earlier in TranslateGenericIntrinsic; same shared emitter.)
                var comparand = Pop();
                var value = Pop();
                var loc = Pop();
                EmitInterlockedSwap(sig.ParameterTypes[0].Element!, loc, value, comparand, name);
                return true;
            }
            case ("System.Threading.Interlocked", "Exchange")
                when sig.ParameterTypes is [{ Kind: TypeKind.ByRef }, _]:
            {
                var value = Pop();
                var loc = Pop();
                EmitInterlockedSwap(sig.ParameterTypes[0].Element!, loc, value, comparand: null, name);
                return true;
            }
            case ("System.Threading.Interlocked", "Read"):
            {
                // Read(ref long/ulong): a seq_cst atomic 64-bit load (tear-free even on
                // a 32-bit target). These are the only two Read overloads in the BCL.
                var loc = Pop();
                Push(StackKind.I8, "int64_t", $"dn2cpp_interlocked_read_i8((int64_t*)({loc.Expr}))");
                return true;
            }
            case ("System.Threading.Interlocked", "Add"):
            case ("System.Threading.Interlocked", "Increment"):
            case ("System.Threading.Interlocked", "Decrement"):
            {
                bool i8 = sig.ParameterTypes[0] is { Element.Primitive: PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr };
                string ct = i8 ? "int64_t" : "int32_t";
                string delta = name switch
                {
                    "Increment" => "1",
                    "Decrement" => "-1",
                    _ => Pop().Expr, // Add(ref, value)
                };
                var loc = Pop();
                Push(i8 ? StackKind.I8 : StackKind.I4, ct,
                    $"dn2cpp_interlocked_add_i{(i8 ? 8 : 4)}(({ct}*)({loc.Expr}), {delta})");
                return true;
            }
            case ("System.Threading.Interlocked", "Or"):
            case ("System.Threading.Interlocked", "And"):
            {
                // Or/And(ref x, value) return the ORIGINAL value (fetch_or/fetch_and),
                // unlike Add which returns the new value. The BCL's And/Or surface is
                // int/uint/long/ulong only — no nint/nuint overloads exist, so the i8
                // test deliberately omits IntPtr/UIntPtr (unlike Add's, which has them).
                bool i8 = sig.ParameterTypes[0] is { Element.Primitive: PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 };
                string ct = i8 ? "int64_t" : "int32_t";
                var value = Pop();
                var loc = Pop();
                Push(i8 ? StackKind.I8 : StackKind.I4, ct,
                    $"dn2cpp_interlocked_{name.ToLowerInvariant()}_i{(i8 ? 8 : 4)}(({ct}*)({loc.Expr}), {value.Expr})");
                return true;
            }

            // ---- System.Threading.ThreadPool (fire-and-forget on the worker pool) ----
            // QueueUserWorkItem(WaitCallback [, object state]) -> bool (always true): enqueue
            // the delegate on the same pool as Task.Run, returning no Task. The 1-arg overload
            // passes a null state. A GC holder (dn2cpp_threadpool_queue) keeps the delegate +
            // state reachable while queued. The generic QueueUserWorkItem<TState>(Action<TState>,
            // TState, bool preferLocal) routes through TranslateGenericIntrinsic (carved out
            // there); the IThreadPoolWorkItem form is handled just below.
            //
            // UnsafeQueueUserWorkItem(WaitCallback, object) is the SAME lowering and shares
            // this arm: the only thing "unsafe" names is that it does not capture and flow the
            // caller's ExecutionContext to the worker, and no ExecutionContext flow is modeled
            // in either form. Custom awaitables commonly use one spelling from OnCompleted and
            // the other from UnsafeOnCompleted, so a lone safe arm leaves the await path
            // half-mapped.
            case ("System.Threading.ThreadPool", "QueueUserWorkItem")
                when DelegateParamName(sig.ParameterTypes[0]) == "System.Threading.WaitCallback":
            case ("System.Threading.ThreadPool", "UnsafeQueueUserWorkItem")
                when DelegateParamName(sig.ParameterTypes[0]) == "System.Threading.WaitCallback":
            {
                string state = sig.ParameterTypes.Length == 2 ? Cast(Pop(), "Dn2CppObject*") : "nullptr";
                var callback = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_threadpool_queue({Cast(callback, "Dn2CppObject*")}, {state})");
                return true;
            }
            // UnsafeQueueUserWorkItem(IThreadPoolWorkItem, bool preferLocal) -> bool
            // (always true): fire-and-forget the work item's Execute() on the same
            // pool. The Execute implementation is resolved through the receiver's
            // interface table at enqueue time and packaged as the callback+state
            // pseudo-delegate the pool's fire-and-forget path already invokes
            // (dn2cpp_threadpool_queue_workitem). The reachability twin — marking
            // IThreadPoolWorkItem.Execute used so every allocated work-item type
            // wires + reaches its body — is ReachThreadPoolWorkItemExecute.
            case ("System.Threading.ThreadPool", "UnsafeQueueUserWorkItem")
                when sig.ParameterTypes is
                    [{ Kind: TypeKind.Class, Class: { IsInterface: true } wiItf },
                     { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]
                    && wiItf.Methods.FirstOrDefault(m =>
                        m.Name == "Execute" && !m.IsStatic && m.Signature.ParameterTypes.Length == 0)
                        is { VtableSlot: >= 0 } wiExec:
            {
                Pop(); // bool preferLocal — scheduling hint the fixed pool ignores
                var wi = Pop();
                Comp.NoteReferencedType(wiItf);
                string tmp = NewTemp("Dn2CppObject*");
                Emit($"{tmp} = {Cast(wi, "Dn2CppObject*")};");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_threadpool_queue_workitem({tmp}, " +
                    $"dn2cpp_resolve_interface({tmp}->type, &{wiItf.CppTypeInfoName})[{wiExec.VtableSlot}])");
                return true;
            }
            // UseWindowsThreadPool — a feature-switch flag selecting the real OS
            // Windows thread pool (native `TP_POOL`/`TP_CALLBACK_ENVIRON` APIs) vs
            // the portable managed one. The ThreadPool model above
            // (dn2cpp_threadpool_queue/_workitem) is the portable pool in either case —
            // there is no native-Windows-thread-pool arm to select — so this is always
            // false, the branch every caller falls back to when the real BCL isn't built
            // for the native pool.
            case ("System.Threading.ThreadPool", "get_UseWindowsThreadPool"):
                Push(StackKind.I4, "int32_t", "0");
                return true;
            case ("System.Threading.ThreadPool", "QueueUserWorkItem"):
            case ("System.Threading.ThreadPool", "UnsafeQueueUserWorkItem"):
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: only the non-generic " +
                    "ThreadPool.QueueUserWorkItem(WaitCallback [, object]) overloads are supported " +
                    "(the generic QueueUserWorkItem<TState>(Action<TState>, TState, bool preferLocal) " +
                    "and UnsafeQueueUserWorkItem / IThreadPoolWorkItem forms are carve-outs)");

            // ---- System.Text.StringBuilder ----
            // Append(ref AppendInterpolatedStringHandler) — the overload `sb.Append($"...")`
            // binds to. The handler is modeled as the underlying StringBuilder pointer and
            // already appended every literal/hole to it during construction,
            // so this just pops the handler ref and returns the receiver (fluent).
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Class, Class.IntrinsicCppName: "Dn2CppStringBuilder*" } }]:
            {
                Pop(); // the handler ref (its contents are already in the builder)
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", sb);
                return true;
            }
            // Append(IFormatProvider, ref AppendInterpolatedStringHandler) — the culture-aware
            // overload `sb.Append(provider, $"...")`. Like the handler-only form the handler
            // already appended every literal/hole during construction (formatting them
            // invariantly), so the provider is redundant. Args push left-to-right (receiver,
            // provider, handler-ref), so pop order is handler-ref, provider, receiver. The
            // second-parameter byref-to-handler shape makes this disjoint from the
            // Append(char, int) sibling below.
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [
                    { },
                    { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Class, Class.IntrinsicCppName: "Dn2CppStringBuilder*" } }]:
            {
                Pop(); // the handler ref
                Pop(); // the IFormatProvider (dn2cpp formats the holes invariantly at construction)
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", sb);
                return true;
            }
            // Append(ReadOnlySpan<char>/Span<char>) — materialize the span's
            // window as a string and append it (the builder stores strings).
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ } spt0] && IsCharSpan(spt0):
            {
                var sp = Pop();
                string sbsp = Cast(Pop(), "Dn2CppStringBuilder*");
                string sptmp = NewTemp(CppTypes.Of(sig.ParameterTypes[0]));
                Emit($"{sptmp} = {sp.Expr};");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_append_str({sbsp}, dn2cpp_string_from_chars((const char16_t*){sptmp}.f__reference, {sptmp}.f__length))");
                return true;
            }
            // Append(char, int repeatCount) — the run-of-one-char overload
            // (e.g. the "[,,]" array-rank suffix a type-name formatter
            // builds). Loop the single-char append.
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var n = Pop();
                var ch = Pop();
                string sbr = Cast(Pop(), "Dn2CppStringBuilder*");
                string tmp = NewTemp("Dn2CppStringBuilder*");
                Emit($"{tmp} = {sbr};");
                Emit($"for (int32_t __rep = 0, __repN = {Cast(n, "int32_t")}; __rep < __repN; __rep++) dn2cpp_sb_append_char({tmp}, (char16_t)({ch.Expr}));");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", tmp);
                return true;
            }
            // Append(char[]) — a null array appends nothing (probe-confirmed);
            // otherwise copy the raw code units, no intermediate string.
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char }]:
            {
                var arr = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                string at = NewTemp("Dn2CppArrayN*");
                Emit($"{at} = (Dn2CppArrayN*)({arr.Expr});");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"({at} != nullptr ? dn2cpp_sb_append_chars({sb}, (const char16_t*){at}->data, {at}->length) : {sb})");
                return true;
            }
            // Append(char[], int startIndex, int charCount) — the runtime slice
            // helper validates (null valid only as (null, 0, 0), else
            // ArgumentNull/ArgumentOutOfRange) and materializes the slice.
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var cnt = Pop();
                var start = Pop();
                var arr = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_append_str({sb}, dn2cpp_sb_char_arr_str((Dn2CppArrayN*)({arr.Expr}), {start.Expr}, {cnt.Expr}))");
                return true;
            }
            // Append(string, int startIndex, int count) — the substring window
            // (IdnMapping's Punycode decoder copies each label's basic run with
            // it). A null value appends nothing (dn2cpp_str_substring's bounds
            // check stands in for .NET's ArgumentNull on a nonzero window —
            // both only fire on invalid input).
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var cnt = Pop();
                var start = Pop();
                var val = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                string vs = NewTemp("Dn2CppString*");
                Emit($"{vs} = {Cast(val, "Dn2CppString*")};");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"({vs} != nullptr ? dn2cpp_sb_append_str({sb}, dn2cpp_str_substring({vs}, {start.Expr}, {cnt.Expr})) : {sb})");
                return true;
            }
            // Append(StringBuilder) — a null value appends nothing; self-append
            // doubles the content (both probe-confirmed, handled in the helper).
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Text.StringBuilder" }]:
            {
                var v = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_append_sb({sb}, {Cast(v, "Dn2CppStringBuilder*")})");
                return true;
            }
            // Append(StringBuilder, int startIndex, int count) — the slice form.
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Text.StringBuilder" },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var cnt = Pop();
                var start = Pop();
                var v = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_append_sb_range({sb}, {Cast(v, "Dn2CppStringBuilder*")}, {start.Expr}, {cnt.Expr})");
                return true;
            }
            // Single-argument Append over the growable runtime buffer. Each member
            // returns the builder for fluent chaining. Sub-int integers arrive on
            // the IL stack already sign/zero-extended to int32, so they share the
            // int formatting.
            case ("System.Text.StringBuilder", "Append")
                when sig.ParameterTypes is [{ } pt] && pt.Kind != TypeKind.SZArray:
            {
                var arg = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                string call = pt switch
                {
                    { IsString: true } => $"dn2cpp_sb_append_str({sb}, {Cast(arg, "Dn2CppString*")})",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } => $"dn2cpp_sb_append_char({sb}, (char16_t)({arg.Expr}))",
                    // uint shares the I4 stack slot but must format unsigned — the
                    // signed formatter would render values above Int32.MaxValue as
                    // negative. Truncate through uint32_t against sign-extension.
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 } => $"dn2cpp_sb_append_str({sb}, dn2cpp_format_uint((uint64_t)(uint32_t)({arg.Expr}), 4, nullptr))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 } => $"dn2cpp_sb_append_str({sb}, dn2cpp_format_uint((uint64_t)({arg.Expr}), 8, nullptr))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32
                        or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                        or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 } => $"dn2cpp_sb_append_str({sb}, dn2cpp_int_to_string({arg.Expr}))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } => $"dn2cpp_sb_append_str({sb}, dn2cpp_long_to_string({arg.Expr}))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } => $"dn2cpp_sb_append_str({sb}, dn2cpp_double_to_string({arg.Expr}))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single } => $"dn2cpp_sb_append_str({sb}, dn2cpp_float_to_string((float)({arg.Expr})))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean } => $"dn2cpp_sb_append_str({sb}, dn2cpp_bool_to_string({arg.Expr}))",
                    _ when IsDecimal(pt) => $"dn2cpp_sb_append_str({sb}, dn2cpp_decimal_to_string({DecVal(arg)}))",
                    _ => $"dn2cpp_sb_append_str({sb}, dn2cpp_object_tostring({Cast(arg, "Dn2CppObject*")}))",
                };
                Push(StackKind.Ref, "Dn2CppStringBuilder*", call);
                return true;
            }
            // AppendFormat(format, object...) — compose via the string.Format
            // helper, then append (fluent). BuildStringFormatExpr pops the format +
            // args, leaving the receiver on top.
            case ("System.Text.StringBuilder", "AppendFormat") when IsConsoleFormatOverload(sig):
            {
                string fmt = BuildStringFormatExpr(sig);
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_append_str({sb}, {fmt})");
                return true;
            }
            // AppendFormat(IFormatProvider, format, object...) — compose under the
            // culture via the *_c formatters (shared with string.Format), then append.
            case ("System.Text.StringBuilder", "AppendFormat") when IsProviderFormatOverload(sig):
            {
                string fmt = BuildStringFormatExprC(sig);
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_append_str({sb}, {fmt})");
                return true;
            }
            // AppendFormat([IFormatProvider,] format, params ReadOnlySpan<object>) —
            // the net10 params-span overloads (4+ loose args); same composition over
            // the span's data pointer + length.
            case ("System.Text.StringBuilder", "AppendFormat") when IsSpanFormatOverload(sig):
            {
                string fmt = BuildStringFormatExprSpan(sig);
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_append_str({sb}, {fmt})");
                return true;
            }
            case ("System.Text.StringBuilder", "AppendFormat") when IsProviderSpanFormatOverload(sig):
            {
                string fmt = BuildStringFormatExprSpanC(sig);
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_append_str({sb}, {fmt})");
                return true;
            }
            // AppendJoin(string|char separator, object[]/string[]) — compose via the
            // string.Join ref-array helper (null elements contribute nothing), then
            // append. The generic AppendJoin<T>(sep, IEnumerable<T>) binds in
            // TranslateGenericIntrinsic.
            case ("System.Text.StringBuilder", "AppendJoin")
                when sig.ParameterTypes is [_, { Kind: TypeKind.SZArray }]:
            {
                var arr = Pop();
                var sep = Pop();
                string sepStr = sep.Kind == StackKind.Ref
                    ? Cast(sep, "Dn2CppString*")
                    : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_append_str({sb}, dn2cpp_string_join_ref({sepStr}, (Dn2CppArrayRef*)({arr.Expr})))");
                return true;
            }
            // AppendJoin(string|char, params ReadOnlySpan<object|string>) — the
            // net10 params-span overloads; join over the span's data pointer +
            // length, then append.
            case ("System.Text.StringBuilder", "AppendJoin")
                when sig.ParameterTypes is [_, var ajsp] && (IsObjectSpan(ajsp) || IsStringSpan(ajsp)):
            {
                string sp = PopSpanTemp(sig.ParameterTypes[1]);
                var sep = Pop();
                string sepStr = sep.Kind == StackKind.Ref
                    ? Cast(sep, "Dn2CppString*")
                    : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_append_str({sb}, dn2cpp_string_join_objs({sepStr}, (Dn2CppObject* const*){sp}.f__reference, {sp}.f__length))");
                return true;
            }
            // AppendLine(ref AppendInterpolatedStringHandler) — `sb.AppendLine($"...")`.
            // The handler already appended its content; just add the trailing newline
            // and return the builder.
            case ("System.Text.StringBuilder", "AppendLine")
                when sig.ParameterTypes is [{ Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Class, Class.IntrinsicCppName: "Dn2CppStringBuilder*" } }]:
            {
                Pop(); // the handler ref (its contents are already in the builder)
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_append_newline({sb})");
                return true;
            }
            case ("System.Text.StringBuilder", "AppendLine")
                when sig.ParameterTypes is [{ } alpt] && alpt.IsString:
            {
                string s = Cast(Pop(), "Dn2CppString*");
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_append_newline(dn2cpp_sb_append_str({sb}, {s}))");
                return true;
            }
            case ("System.Text.StringBuilder", "AppendLine") when sig.ParameterTypes.Length == 0:
            {
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_append_newline({sb})");
                return true;
            }
            case ("System.Text.StringBuilder", "ToString") when sig.ParameterTypes.Length == 0:
            {
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_sb_tostring({sb})");
                return true;
            }
            // ToString(int startIndex, int length) — a substring of the built text. The
            // runtime str_substring over the snapshot enforces the same bounds .NET does
            // (startIndex/length against the builder length -> ArgumentOutOfRange).
            case ("System.Text.StringBuilder", "ToString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var len = Pop();
                var start = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_substring(dn2cpp_sb_tostring({sb}), {Cast(start, "int32_t")}, {Cast(len, "int32_t")})");
                return true;
            }
            // CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
            // — bounds-checked copy of `count` code units into the caller's char[]. The
            // runtime helper raises the BCL's ArgumentOutOfRange/ArgumentException on a bad
            // range. (StringBuilder has no other CopyTo overload.)
            case ("System.Text.StringBuilder", "CopyTo")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char },
                    { Primitive: PrimitiveTypeCode.Int32 }, { Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var destIndex = Pop();
                var dest = Pop();
                var srcIndex = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Emit($"dn2cpp_sb_copy_to({sb}, {Cast(srcIndex, "int32_t")}, {Cast(dest, "Dn2CppArrayN*")}, " +
                     $"{Cast(destIndex, "int32_t")}, {Cast(count, "int32_t")});");
                return true;
            }
            // GetChunks() + the ChunkEnumerator members: the runtime StringBuilder
            // is contiguous, so enumeration yields one chunk — a snapshot string
            // wrapped in a real ReadOnlyMemory<char> (its transpiled Span getter
            // takes the string arm). Regex's RegexCharClass.ToStringClass is the
            // reaching consumer.
            case ("System.Text.StringBuilder", "GetChunks"):
            {
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                string en = NewTemp("Dn2CppSbChunkEnum");
                Emit($"{en} = Dn2CppSbChunkEnum{{ dn2cpp_sb_tostring({sb}), 0 }};");
                Push(StackKind.Struct, "Dn2CppSbChunkEnum", en);
                return true;
            }
            case ("System.Text.StringBuilder+ChunkEnumerator", "GetEnumerator"):
            {
                var recv = Pop(); // address of the enumerator (instance struct method)
                Push(StackKind.Struct, "Dn2CppSbChunkEnum", $"(*(Dn2CppSbChunkEnum*)({recv.Expr}))");
                return true;
            }
            case ("System.Text.StringBuilder+ChunkEnumerator", "MoveNext"):
            {
                var recv = Pop();
                string r = NewTemp("int32_t");
                Emit($"{{ Dn2CppSbChunkEnum* _e = (Dn2CppSbChunkEnum*)({recv.Expr}); "
                     + $"{r} = _e->state == 0 ? 1 : 0; _e->state = 1; }}");
                Push(StackKind.I4, "int32_t", r);
                return true;
            }
            case ("System.Text.StringBuilder+ChunkEnumerator", "get_Current"):
            {
                var recv = Pop();
                if (sig.ReturnType is not { Kind: TypeKind.Class, Class: { } memCls })
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: ChunkEnumerator.Current return type is not a class");
                Comp.EnsureCompleted(memCls);
                var fObj = memCls.Fields.First(f => f.Name == "_object").CppName;
                var fLen = memCls.Fields.First(f => f.Name == "_length").CppName;
                string memCt = CppTypes.Of(sig.ReturnType);
                string cur = NewTemp(memCt);
                Emit($"{cur} = {memCt}{{}};");
                Emit($"{cur}.{fObj} = (Dn2CppObject*)((Dn2CppSbChunkEnum*)({recv.Expr}))->snapshot;");
                Emit($"{cur}.{fLen} = ((Dn2CppSbChunkEnum*)({recv.Expr}))->snapshot->length;");
                Push(StackKind.Struct, memCt, cur);
                return true;
            }
            case ("System.Text.StringBuilder", "get_Length"):
            {
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.I4, "int32_t", $"dn2cpp_sb_length({sb})");
                return true;
            }
            case ("System.Text.StringBuilder", "get_Capacity"):
            {
                // The current buffer size; seeded by the ctor and grown on demand. A
                // P/Invoke StringBuilder passes this as the native buffer length (4h).
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.I4, "int32_t", $"dn2cpp_sb_capacity({sb})");
                return true;
            }
            case ("System.Text.StringBuilder", "Clear"):
            {
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_clear({sb})");
                return true;
            }
            // Insert(index, char[]) — the index is validated first (insert_str's
            // order), then a null array inserts nothing (probe-confirmed).
            case ("System.Text.StringBuilder", "Insert")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char }]:
            {
                var arr = Pop();
                var index = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                string at = NewTemp("Dn2CppArrayN*");
                Emit($"{at} = (Dn2CppArrayN*)({arr.Expr});");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_insert_str({sb}, {index.Expr}, {at} != nullptr "
                    + $"? dn2cpp_string_from_chars((const char16_t*){at}->data, {at}->length) : (Dn2CppString*)nullptr)");
                return true;
            }
            // Insert(index, char[], int startIndex, int charCount) — the shared
            // slice helper validates and materializes, then the string inserts.
            case ("System.Text.StringBuilder", "Insert")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var cnt = Pop();
                var start = Pop();
                var arr = Pop();
                var index = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_sb_char_arr_str((Dn2CppArrayN*)({arr.Expr}), {start.Expr}, {cnt.Expr}))");
                return true;
            }
            // Insert(index, string, int count) — count repetitions; a null/empty
            // value or count 0 is a validated no-op.
            case ("System.Text.StringBuilder", "Insert")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var cnt = Pop();
                var val = Pop();
                var index = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_insert_str_count({sb}, {index.Expr}, {Cast(val, "Dn2CppString*")}, {cnt.Expr})");
                return true;
            }
            // Insert(index, value): char inserts directly, every other value type is
            // formatted to a string first (mirroring Append; sub-int integers arrive
            // int32-widened and share the int formatting).
            case ("System.Text.StringBuilder", "Insert") when sig.ParameterTypes.Length == 2:
            {
                var arg = Pop();
                var index = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                string call = sig.ParameterTypes[1] switch
                {
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }
                        => $"dn2cpp_sb_insert_char({sb}, {index.Expr}, (char16_t)({arg.Expr}))",
                    { IsString: true }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, {Cast(arg, "Dn2CppString*")})",
                    // uint/ulong must format unsigned (see the Append arm above).
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_format_uint((uint64_t)(uint32_t)({arg.Expr}), 4, nullptr))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_format_uint((uint64_t)({arg.Expr}), 8, nullptr))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32
                        or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte
                        or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_int_to_string({arg.Expr}))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_long_to_string({arg.Expr}))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_double_to_string({arg.Expr}))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_float_to_string((float)({arg.Expr})))",
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_bool_to_string({arg.Expr}))",
                    _ when IsDecimal(sig.ParameterTypes[1])
                        => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_decimal_to_string({DecVal(arg)}))",
                    _ => $"dn2cpp_sb_insert_str({sb}, {index.Expr}, dn2cpp_object_tostring({Cast(arg, "Dn2CppObject*")}))",
                };
                Push(StackKind.Ref, "Dn2CppStringBuilder*", call);
                return true;
            }
            // Remove(startIndex, length).
            case ("System.Text.StringBuilder", "Remove"):
            {
                var count = Pop();
                var start = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_remove({sb}, {start.Expr}, {count.Expr})");
                return true;
            }
            // Replace(char, char) and Replace(string, string), all occurrences, plus
            // the (oldValue, newValue, startIndex, count) range forms (a string match
            // must lie fully inside the range).
            case ("System.Text.StringBuilder", "Replace")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true }]:
            {
                var newv = Pop();
                var oldv = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_replace_str({sb}, {Cast(oldv, "Dn2CppString*")}, {Cast(newv, "Dn2CppString*")})");
                return true;
            }
            case ("System.Text.StringBuilder", "Replace")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var cnt = Pop();
                var start = Pop();
                var newv = Pop();
                var oldv = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_replace_str_range({sb}, {Cast(oldv, "Dn2CppString*")}, {Cast(newv, "Dn2CppString*")}, {start.Expr}, {cnt.Expr})");
                return true;
            }
            case ("System.Text.StringBuilder", "Replace")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var cnt = Pop();
                var start = Pop();
                var newc = Pop();
                var oldc = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_replace_char_range({sb}, (char16_t)({oldc.Expr}), (char16_t)({newc.Expr}), {start.Expr}, {cnt.Expr})");
                return true;
            }
            case ("System.Text.StringBuilder", "Replace")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }, _]:
            {
                var newc = Pop();
                var oldc = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*",
                    $"dn2cpp_sb_replace_char({sb}, (char16_t)({oldc.Expr}), (char16_t)({newc.Expr}))");
                return true;
            }
            // Length setter: truncate, or grow zero-('\0')-padded (probe-confirmed).
            case ("System.Text.StringBuilder", "set_Length"):
            {
                var v = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Emit($"dn2cpp_sb_set_length({sb}, {v.Expr});");
                return true;
            }
            // The indexer. .NET's asymmetry (probe-confirmed): the getter throws
            // IndexOutOfRangeException, the setter ArgumentOutOfRangeException.
            case ("System.Text.StringBuilder", "get_Chars"):
            {
                var idx = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.I4, "int32_t", $"(int32_t)dn2cpp_sb_get_char({sb}, {idx.Expr})");
                return true;
            }
            case ("System.Text.StringBuilder", "set_Chars"):
            {
                var v = Pop();
                var idx = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Emit($"dn2cpp_sb_set_char({sb}, {idx.Expr}, (char16_t)({v.Expr}));");
                return true;
            }
            // EnsureCapacity(int): grow-only to exactly the requested capacity,
            // returning the (possibly larger pre-existing) capacity.
            case ("System.Text.StringBuilder", "EnsureCapacity"):
            {
                var cap = Pop();
                string sb = Cast(Pop(), "Dn2CppStringBuilder*");
                Push(StackKind.I4, "int32_t", $"dn2cpp_sb_ensure_capacity({sb}, {cap.Expr})");
                return true;
            }

            case ("System.Runtime.CompilerServices.RuntimeHelpers", "GetHashCode") when sig.ParameterTypes.Length == 1:
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_hashcode({Cast(a, "Dn2CppObject*")})");
                return true;
            }
            // RuntimeHelpers.TryGetHashCode(o): the no-allocation probe — "the identity
            // hash if one was already assigned, else 0 without creating one" (real .NET
            // reads the sync-block/header hash lazily; ConditionalWeakTable.FindEntry
            // uses the 0 as a definite miss). dn2cpp's identity hash is derived from the
            // object's non-moving address, so it always "exists" — same value as
            // GetHashCode, which callers treat as a plain (slower-path) hit.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "TryGetHashCode") when sig.ParameterTypes.Length == 1:
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_hashcode({Cast(a, "Dn2CppObject*")})");
                return true;
            }
            // RuntimeHelpers.ObjectHasComponentSize(o): true for arrays/strings (the JIT's
            // HasComponentSize MethodTable bit). ReadOnlyMemory<T>.Span uses it to pick the
            // array-backed branch over the MemoryManager-backed one.
            case ("System.Runtime.CompilerServices.RuntimeHelpers", "ObjectHasComponentSize")
                when sig.ParameterTypes.Length == 1:
            {
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_has_component_size({Cast(a, "Dn2CppObject*")})");
                return true;
            }

            // System.SR.Format(resourceFormat, args...) — the composite-format helper the
            // BCL builds most exception messages through. Its first argument is the
            // resource TEXT the getter case below recovers (not the key), so lowering it
            // onto the same dn2cpp_string_format engine string.Format/Console use produces
            // the real .NET message. The (string, object[]) and (string, object[1..3])
            // shapes match IsConsoleFormatOverload exactly. Must precede the SR catch-all,
            // which would otherwise fold Format to the literal "Format".
            case ("System.SR", "Format") when IsConsoleFormatOverload(sig):
            {
                Push(StackKind.Ref, "Dn2CppString*", BuildStringFormatExpr(sig));
                return true;
            }
            // System.SR — every BCL resource-string getter. Recover the real English
            // text from the calling assembly's own embedded .resources blob (see
            // Compilation.SrResourceText) and fold it in as a literal, so a BCL
            // exception message reads as real .NET does — without the ResourceManager /
            // CultureInfo subtree ever entering the tree. A key with no recovered text
            // (a stripped resources blob, an unknown key) falls back to the key itself.
            // All the getters are static and return string; a non-getter, non-Format
            // member (e.g. GetResourceString(string)) keeps the pass-through behavior —
            // its argument is dropped and the key-or-text pushed.
            // The one non-string shape is bool — UsingResourceKeys() and its private
            // twin GetUsingResourceKeysSwitchValue(), the "raw resource key vs.
            // localized text" guard the BCL emits before nearly every SR call site —
            // folded to false, because the string arm recovers the TEXT, not the key.
            // Any other non-void shape is unmodeled: this arm intercepts a private
            // runtime type wholesale, so it throws rather than fall through (a pushed
            // nothing surfaces as an IL stack underflow at a distance).
            case ("System.SR", _):
            {
                for (int i = 0; i < sig.ParameterTypes.Length; i++)
                    Pop();
                if (sig.ReturnType.IsString)
                {
                    string key = name.StartsWith("get_") ? name.Substring(4) : name;
                    string text = Comp.SrResourceText(Module, key) ?? key;
                    Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd(text));
                }
                else if (sig.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean })
                {
                    Push(StackKind.I4, "int32_t", "0");
                }
                else if (!sig.ReturnType.IsVoid)
                {
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: System.SR.{name} has an " +
                        "unmodeled return type (only string, bool and void SR members are lowered)");
                }
                return true;
            }

            // ---- System.ThrowHelper: the GUARDS, which must be told from the SINKS ----
            //
            // A `Throw*` member of System.ThrowHelper is a [DoesNotReturn] SINK and the trap
            // arm below replaces it wholesale. A `ThrowIf*` member is a conditional GUARD —
            // it returns normally on the happy path — and lowering one as a sink drops the
            // TEST: an unconditional throw where .NET throws only for a null argument.
            //
            // CoreLib's own System.ThrowHelper has sinks only, but `System.ThrowHelper` is a
            // SHARED-SOURCE POLYFILL as well: dotnet/runtime's ThrowHelper.cs is compiled
            // into other assemblies, and that copy carries ThrowIfNull and
            // IfNullOrWhitespace beside the sinks. So a trap keyed on
            // `name.StartsWith("Throw")` fires unconditionally on the GUARDS.
            //
            // The guards get REAL lowerings rather than falling through, because
            // System.ThrowHelper is an INTRINSIC-MAPPED type (CoreIntrinsics
            // s_intrinsicTypes): reachability cuts every member of it, so there is no real
            // body to fall back to and a fall-through would name a deleted symbol at C++
            // link time. Cut by name, lower by shape (docs/ARCHITECTURE.md §4-B).
            //
            // The paramName operand is dropped: the trap raises a correctly-TYPED
            // ArgumentNullException carrying its real .NET default text, but `.ParamName`
            // is null — a runtime-raised exception has no managed field for it.
            case ("System.ThrowHelper", "ThrowIfNull") when sig.ParameterTypes.Length == 2:
            {
                Pop();                                  // paramName — see above
                var tinArg = Pop();                     // the argument under test
                string tinTmp = NewTemp("Dn2CppObject*");
                Emit($"{tinTmp} = {Cast(tinArg, "Dn2CppObject*")};");
                Emit($"if ({tinTmp} == nullptr) dn2cpp_throw_argument_null();");
                return true;
            }
            // The polyfill's other guard — not named Throw* at all, so excluding `ThrowIf*`
            // from the sink arm would not have covered it. It RETURNS its argument, and
            // both of its faults are real: null is an ArgumentNullException, blank is an
            // ArgumentException.
            case ("System.ThrowHelper", "IfNullOrWhitespace") when sig.ParameterTypes.Length == 2:
            {
                Pop();                                  // paramName — see above
                var wsArg = Pop();
                string wsTmp = NewTemp("Dn2CppString*");
                Emit($"{wsTmp} = {Cast(wsArg, "Dn2CppString*")};");
                Emit($"if ({wsTmp} == nullptr) dn2cpp_throw_argument_null();");
                Emit($"else if (dn2cpp_str_is_null_or_whitespace({wsTmp})) dn2cpp_throw_argument();");
                Push(StackKind.Ref, "Dn2CppString*", wsTmp);
                return true;
            }
            // The [DoesNotReturn] SINKS. Their real bodies build large exception objects
            // (message formatting pulls in CultureInfo, resource lookups, …); drop the
            // arguments and emit a runtime trap raising a catchable managed exception, so
            // none of that IL is transpiled. `ThrowIf*` is excluded structurally, not
            // merely shadowed by the two arms above: an unmodeled GUARD this type gains
            // upstream must reach the switch default and fail LOUDLY (the intrinsic-type
            // posture, docs/ARCHITECTURE.md §4-B) rather than silently becoming an
            // unconditional throw of the wrong type.
            //
            // ArgumentOutOfRangeException_Enum_Value is the one [DoesNotReturn]
            // ThrowHelper member not named Throw*; same trap lowering.
            case ("System.ThrowHelper", _)
                when (name.StartsWith("Throw") && !name.StartsWith("ThrowIf"))
                     || name == "ArgumentOutOfRangeException_Enum_Value":
            {
                EmitThrowHelperSink(name, sig);
                return true;
            }

            // ---- async/await ----
            // The builder/task/awaiter are modeled by runtime structs
            // (Dn2CppAsyncBuilder/Dn2CppTask/Dn2CppTaskAwaiter).
            case ("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "Create"):
                // static: a fresh builder owning a pending task.
                Push(StackKind.Struct, "Dn2CppAsyncBuilder", "Dn2CppAsyncBuilder{ dn2cpp_task_alloc() }");
                return true;
            case ("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "get_Task"):
            {
                var b = Pop(); // ref builder
                Push(StackKind.Ref, "Dn2CppTask*", $"((Dn2CppAsyncBuilder*)({b.Expr}))->task");
                return true;
            }
            case ("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "SetResult"):
            {
                // Completing the task fires any awaiters' continuations onto the
                // scheduler (dn2cpp_task_set_result), so suspended callers resume.
                if (sig.ParameterTypes.Length == 1)
                {
                    var v = Pop();
                    var b = Pop();
                    string store = EmitTaskResultStore(v); // may emit a boxing temp first
                    Emit($"dn2cpp_task_set_result(((Dn2CppAsyncBuilder*)({b.Expr}))->task, {store});");
                }
                else // non-generic builder (async Task): no result value
                {
                    var b = Pop();
                    Emit($"dn2cpp_task_set_result(((Dn2CppAsyncBuilder*)({b.Expr}))->task, 0);");
                }
                return true;
            }
            case ("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "SetException"):
            {
                var e = Pop();
                var b = Pop();
                Emit($"dn2cpp_task_set_exception(((Dn2CppAsyncBuilder*)({b.Expr}))->task, (Dn2CppObject*)({e.Expr}));");
                return true;
            }
            // The machine calls SetStateMachine during setup; we start the machine
            // directly (synchronous), so it is a no-op.
            case ("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", "SetStateMachine"):
                Pop(); // arg (IAsyncStateMachine)
                Pop(); // this
                return true;

            case ("System.Threading.Tasks.Task", "get_CompletedTask"):
                Push(StackKind.Ref, "Dn2CppTask*", "dn2cpp_task_completed()");
                return true;
            // Task.ThrowAsync(Exception, SynchronizationContext) — the async-void
            // unhandled-exception re-raise. AsyncVoidMethodBuilder.SetException calls it to
            // surface the exception off the synchronous stack (an async void fault crashes
            // the process in real .NET); dn2cpp posts it to the worker pool as a rethrow-only
            // fire-and-forget item, whose unhandled exception then fails the process. The
            // SynchronizationContext is not honored (dropped), like the rest of the surface.
            case ("System.Threading.Tasks.Task", "ThrowAsync"):
            {
                var syncCtx = Pop(); // SynchronizationContext (dropped)
                var exc = Pop();     // Exception
                Emit($"dn2cpp_task_throw_async((Dn2CppObject*)({exc.Expr}), (Dn2CppObject*)({syncCtx.Expr}));");
                return true;
            }
            // Non-generic Task.WhenAny(Task[]) / WhenAny(Task, Task) -> Task<Task>
            // completing with the first input task to finish (the generic Task<T>
            // overloads bind in TranslateGenericIntrinsic). Same runtime helper.
            case ("System.Threading.Tasks.Task", "WhenAny"):
                Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_when_any({PopTaskArrayOperand(sig.ParameterTypes)})");
                return true;
            // Non-generic Task.WhenAll(Task[] / IEnumerable<Task>) -> a plain Task
            // (no result array) that completes once every input completes, or faults
            // with the first input fault. The generic WhenAll<T> (result array) binds
            // in TranslateGenericIntrinsic; this is the void-result join.
            case ("System.Threading.Tasks.Task", "WhenAll"):
                Push(StackKind.Ref, "Dn2CppTask*",
                    // Non-generic WhenAll completes with no result array at all
                    // (DN2CPP_WHENALL_VOID), so there is no element handle to carry.
                    $"dn2cpp_task_when_all({PopTaskArrayOperand(sig.ParameterTypes)}, DN2CPP_WHENALL_VOID, nullptr)");
                return true;
            // Task.WaitAll(Task[] / ReadOnlySpan<Task> / IEnumerable<Task>) — the BLOCKING
            // join: drain until every input settles, then raise one AggregateException over
            // every failure. Only the single-collection overloads bind: the timeout / token
            // forms (WaitAll(Task[], int), (Task[], CancellationToken), (Task[], int,
            // CancellationToken)) DECLINE here rather than popping an argument they cannot
            // honor — a bounded wait is a different contract, and popping a timeout as a
            // task would corrupt the stack (AGENTS.md: a name-gated route may decline a
            // shape, never pop one).
            case ("System.Threading.Tasks.Task", "WaitAll") when sig.ParameterTypes.Length == 1:
                Emit($"dn2cpp_task_wait_all({PopTaskArrayOperand(sig.ParameterTypes)});");
                return true;
            // Task.WaitAny(Task[] / ReadOnlySpan<Task> / IEnumerable<Task>) -> the index of
            // the first input to settle. Same single-collection restriction as WaitAll; the
            // winner's fault is NOT raised here (it is observed through that task), matching
            // real .NET.
            case ("System.Threading.Tasks.Task", "WaitAny") when sig.ParameterTypes.Length == 1:
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_task_wait_any({PopTaskArrayOperand(sig.ParameterTypes)})");
                return true;
            // Task.Delay(ms | TimeSpan [, CancellationToken]) — a task completing after
            // the given virtual-time duration. The scheduler advances a logical clock
            // (no wall clock), so concurrent delays complete in duration order without
            // real sleeping. A trailing CancellationToken is unmodeled.
            case ("System.Threading.Tasks.Task", "Delay"):
            {
                // A trailing CancellationToken makes the delay cancellable: if the
                // token's source cancels before the timer fires, the delay task
                // transitions to CANCELED.
                string? src = null;
                if (sig.ParameterTypes is [.., { Kind: TypeKind.Class, Class.FullName: "System.Threading.CancellationToken" }])
                {
                    var tok = Pop(); // CancellationToken struct value
                    src = $"(({tok.Expr}).source)";
                }
                var dur = Pop();
                var dt = sig.ParameterTypes[0];
                string ms = dt is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }
                    ? $"(int64_t)({dur.Expr})"
                    : dt is { Kind: TypeKind.Class, Class.FullName: "System.TimeSpan" }
                        ? $"(({TSVal(dur)}).ticks / 10000)" // 100ns ticks -> ms
                        : throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: Task.Delay duration " +
                            $"type '{dt}' is not supported (expected int milliseconds or TimeSpan)");
                if (src is not null)
                {
                    EmitCanceledExcRegistration();
                    Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_delay_ct({ms}, {src})");
                }
                else
                    Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_delay({ms})");
                return true;
            }
            // Task.Run(Func<Task> [, CancellationToken]) -> Task, the non-generic async-
            // lambda unwrap: the delegate returns an inner Task whose completion (success/
            // fault/cancellation) settles the outer Task. No result is carried (void inner
            // Task). Matched before the Action case via the delegate's Task return type.
            case ("System.Threading.Tasks.Task", "Run")
                when DelegateReturnsTask(sig.ParameterTypes[0]):
            {
                if (sig.ParameterTypes.Length == 2)
                    Pop(); // CancellationToken (ignored)
                var del = Pop(); // Func<Task>
                Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_run_unwrap((Dn2CppObject*)({del.Expr}))");
                return true;
            }
            // Task.Run(Action [, CancellationToken]) -> Task, dispatched to the real
            // worker pool. Matches only when the first parameter is System.Action (the
            // Func<Task> unwrap overload is handled by the case above). A trailing
            // CancellationToken is unmodeled.
            case ("System.Threading.Tasks.Task", "Run")
                when DelegateParamName(sig.ParameterTypes[0]) == "System.Action":
            {
                if (sig.ParameterTypes.Length == 2)
                    Pop(); // CancellationToken (ignored)
                var del = Pop(); // Action
                Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_run_void((Dn2CppObject*)({del.Expr}))");
                return true;
            }
            // Task.Factory / TaskScheduler.Default / TaskScheduler.Current — opaque
            // nullptr sentinels. The factory sentinel is only ever the receiver of
            // the StartNew intercept below (never dereferenced); the scheduler
            // sentinel is only ever an ignored StartNew/ContinueWith hint argument.
            // Intercepting get_Default here keeps the whole TaskScheduler..cctor /
            // ThreadPoolTaskScheduler / Debugger / DependentHandle subtree unreached.
            case ("System.Threading.Tasks.Task", "get_Factory"):
            case ("System.Threading.Tasks.TaskScheduler", "get_Default"):
            case ("System.Threading.Tasks.TaskScheduler", "get_Current"):
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
                return true;
            // TaskFactory.StartNew(Action[, ...]) / StartNew(Action<object?>, object?
            // state[, ...]) -> Task on the same worker pool as Task.Run. The trailing
            // CancellationToken / TaskCreationOptions / TaskScheduler arguments are
            // scheduling hints only — popped and ignored; the delegate always runs on
            // the real pool. The 5-arg state form is what the base Stream.FlushAsync
            // compiles (`Task.Factory.StartNew(static s => ..., this, ct,
            // DenyChildAttach, TaskScheduler.Default)`). The generic StartNew<TResult>
            // forms bind in TranslateGenericIntrinsic. Unlike Task.Run, StartNew never
            // unwraps an async delegate (real .NET returns Task<Task>), and no Action
            // overload can return one — so there is no unwrap path here.
            case ("System.Threading.Tasks.TaskFactory", "StartNew"):
            {
                // Action<object?> (a closed 1-arg generic delegate) marks the state
                // form; plain Action (non-generic, TypeDef or TypeRef) the stateless.
                bool hasState = sig.ParameterTypes[0] is { Kind: TypeKind.Class, Class: { } dcl }
                    && dcl.Context.TypeArgs.Length == 1;
                int kept = hasState ? 2 : 1; // delegate [+ state]
                for (int i = sig.ParameterTypes.Length - 1; i >= kept; i--)
                    Pop(); // CancellationToken / TaskCreationOptions / TaskScheduler hints
                if (hasState)
                {
                    var state = Pop(); // object? state (passed to the delegate)
                    var del = Pop();   // Action<object?>
                    Pop();             // receiver — the Task.Factory nullptr sentinel
                    Push(StackKind.Ref, "Dn2CppTask*",
                        $"dn2cpp_task_run_void_state((Dn2CppObject*)({del.Expr}), {Cast(state, "Dn2CppObject*")})");
                }
                else
                {
                    var del = Pop(); // Action
                    Pop();           // receiver — the Task.Factory nullptr sentinel
                    Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_run_void((Dn2CppObject*)({del.Expr}))");
                }
                return true;
            }
            case ("System.Threading.Tasks.Task", "GetAwaiter"):
            {
                var t = Pop();
                Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ (Dn2CppTask*)({t.Expr}) }}");
                return true;
            }
            // Blocking get on the result: drain the scheduler until the task
            // completes, wrapping a fault or cancellation in an AggregateException
            // (dn2cpp_task_block_wait — the wrap is the blocking wait's contract;
            // await/GetAwaiter().GetResult() stay unwrapped), then load it.
            case ("System.Threading.Tasks.Task", "get_Result"):
            {
                var t = Pop();
                PushTaskResult($"dn2cpp_task_block_wait((Dn2CppTask*)({t.Expr}))", sig.ReturnType);
                return true;
            }
            // Blocking wait at the top of an async program: drain the scheduler
            // until the task completes, surfacing a fault wrapped in an
            // AggregateException like real .NET.
            case ("System.Threading.Tasks.Task", "Wait") when sig.ParameterTypes.Length == 0:
            {
                var t = Pop();
                Emit($"dn2cpp_task_block_wait((Dn2CppTask*)({t.Expr}));");
                return true;
            }
            // Wait(TimeSpan) -> bool: dn2cpp_task_block_wait drains this thread's
            // scheduler until the task settles, so the wait always completes and
            // the timeout bound is never consulted — the result is constant true
            // (a settled fault still throws the AggregateException, as in .NET).
            // Wait(int) / Wait(CancellationToken) stay loud unmapped errors.
            case ("System.Threading.Tasks.Task", "Wait") when sig.ParameterTypes is [{ } waitTimeout]
                && IsTimeSpan(waitTimeout):
            {
                Pop(); // TimeSpan timeout — never hit (the drain runs to completion)
                var t = Pop();
                Emit($"dn2cpp_task_block_wait((Dn2CppTask*)({t.Expr}));");
                Push(StackKind.I4, "int32_t", "1"); // completed within the timeout
                return true;
            }
            // Task.Dispose() — pop the receiver and do nothing: the runtime task is
            // GC-managed and carries no counterpart of real .NET's lazily-allocated
            // wait handle, so there is no resource to release. Only the public
            // parameterless form; the protected Dispose(bool) is not mapped.
            case ("System.Threading.Tasks.Task", "Dispose") when sig.ParameterTypes.Length == 0:
            {
                Pop(); // this — nothing to release (the task is GC-managed)
                return true;
            }
            case ("System.Threading.Tasks.Task", "GetResult") when sig.ParameterTypes.Length == 0:
            {
                // Task.GetResult (the awaiter-less form some patterns use). Awaiter
                // semantics, so deliberately dn2cpp_task_block: a fault re-raises
                // unwrapped, unlike the Wait/.Result arms above.
                var t = Pop();
                Emit($"dn2cpp_task_block((Dn2CppTask*)({t.Expr}));");
                return true;
            }
            // Task.Start([TaskScheduler]) — submit a cold task's (`new Task(...)`)
            // work to the worker pool. A second Start, or Start on a task that was
            // never cold (async-method / Task.Run / settled), throws a catchable
            // InvalidOperationException at runtime, like real .NET. The TaskScheduler
            // argument is a scheduling hint only — popped and ignored.
            case ("System.Threading.Tasks.Task", "Start"):
            {
                if (sig.ParameterTypes.Length == 1)
                    Pop(); // TaskScheduler hint (ignored)
                var t = Pop();
                Emit($"dn2cpp_task_start((Dn2CppTask*)({t.Expr}));");
                return true;
            }
            // Task.RunSynchronously([TaskScheduler]) — claim a cold task's work and
            // run it inline on the calling thread. A fault settles the task FAULTED
            // (observed at Wait/Result/await), it is not re-thrown here — matching
            // real .NET. Same InvalidOperationException rules as Start.
            case ("System.Threading.Tasks.Task", "RunSynchronously"):
            {
                if (sig.ParameterTypes.Length == 1)
                    Pop(); // TaskScheduler hint (ignored)
                var t = Pop();
                Emit($"dn2cpp_task_run_synchronously((Dn2CppTask*)({t.Expr}));");
                return true;
            }
            // Task.ContinueWith(Action<Task[<T>]>[, object? state][, hints...]) — the
            // non-generic-method overloads (the Func<Task, TNewResult> forms are
            // MethodSpecs, bound in TranslateGenericIntrinsic). The continuation task
            // is returned; the delegate runs with the settled antecedent (+ state) on
            // the registering thread's scheduler. Trailing CancellationToken /
            // TaskScheduler arguments are scheduling hints — popped and ignored.
            // TaskContinuationOptions is NOT a hint and is not dropped: its NotOn* trio
            // decides whether the delegate runs at all, so the value travels to the
            // runtime node (PopContinuationHints), which skips a filtered-out
            // continuation and settles it CANCELED as .NET does.
            // Task.WaitAsync(CancellationToken) -> a task mirroring this one's outcome that
            // settles CANCELED as soon as the token cancels. The awaited work is NOT
            // cancelled — WaitAsync bounds the wait, not the operation — which is exactly
            // what the runtime node does (dn2cpp_task_wait_async).
            //
            // The four TIMEOUT overloads — WaitAsync(TimeSpan[, TimeProvider][,
            // CancellationToken]) — deliberately stay UNMAPPED and reach the loud
            // "no intrinsic mapping" throw, the same posture SemaphoreSlim.WaitAsync's timed
            // overloads take next door. They are not more of the same shape: a timeout
            // settles the wrapper with a TimeoutException rather than cancelling it, so
            // routing one through this arm would answer a different exception type
            // (docs/ARCHITECTURE.md §4-B).
            case ("System.Threading.Tasks.Task", "WaitAsync")
                when sig.ParameterTypes is [{ } waTok] && CppTypes.Of(waTok) == "Dn2CppCancelToken":
            {
                var waT = Pop();  // CancellationToken (by value)
                var waSelf = Pop();
                Push(StackKind.Ref, "Dn2CppTask*",
                    $"dn2cpp_task_wait_async((Dn2CppTask*)({waSelf.Expr}), ({waT.Expr}).source)");
                return true;
            }
            case ("System.Threading.Tasks.Task", "ContinueWith"):
            {
                // Action<Task> closes 1 generic arg; the state form's Action<Task,
                // object?> closes 2 (mirroring the StartNew state detection).
                if (sig.ParameterTypes is not [{ Kind: TypeKind.Class, Class: { } cwDel }, ..])
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: Task.ContinueWith ctor shape " +
                        "is not supported (expected a leading Action delegate)");
                bool cwHasState = cwDel.Context.TypeArgs.Length == 2;
                int cwKept = cwHasState ? 2 : 1; // delegate [+ state]
                string cwOpts = PopContinuationHints(sig.ParameterTypes, cwKept);
                var cwState = cwHasState ? Pop() : null;
                var cwD = Pop();
                var cwT = Pop();
                string stateExpr = cwState is null ? "nullptr" : Cast(cwState, "Dn2CppObject*");
                string kind = cwHasState ? "DN2CPP_CONTWITH_VOID_STATE" : "DN2CPP_CONTWITH_VOID";
                Push(StackKind.Ref, "Dn2CppTask*",
                    $"dn2cpp_task_continue_with((Dn2CppTask*)({cwT.Expr}), (Dn2CppObject*)({cwD.Expr}), "
                    + $"{stateExpr}, {kind}, {cwOpts})");
                return true;
            }
            // ---- TaskCompletionSource(<T>) — the bare pending task it completes ----
            // get_Task is the identity (the TCS *is* its Dn2CppTask*); the setters are
            // exactly-once transitions: Set* throws InvalidOperationException when the
            // task has already settled, TrySet* returns false instead. A TCS<T> result
            // packs into the task's 8-byte slot exactly like an async Task<T>'s
            // (EmitTaskResultStore: floats bit-cast, structs heap-boxed).
            case ("System.Threading.Tasks.TaskCompletionSource", "get_Task"):
            {
                var t = Pop();
                Push(StackKind.Ref, "Dn2CppTask*", $"(Dn2CppTask*)({t.Expr})");
                return true;
            }
            case ("System.Threading.Tasks.TaskCompletionSource", "SetResult"):
            case ("System.Threading.Tasks.TaskCompletionSource", "TrySetResult"):
            {
                string store = sig.ParameterTypes.Length == 1 ? EmitTaskResultStore(Pop()) : "0";
                var t = Pop();
                if (name == "SetResult")
                    Emit($"dn2cpp_tcs_set_result((Dn2CppTask*)({t.Expr}), {store});");
                else
                    Push(StackKind.I4, "int32_t", $"dn2cpp_task_try_set_result((Dn2CppTask*)({t.Expr}), {store})");
                return true;
            }
            // Only the single-Exception overloads; SetException(IEnumerable<Exception>)
            // stays a loud unsupported-intrinsic error (no AggregateException model).
            case ("System.Threading.Tasks.TaskCompletionSource", "SetException")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Exception" }]:
            case ("System.Threading.Tasks.TaskCompletionSource", "TrySetException")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Exception" }]:
            {
                var ex = Pop();
                var t = Pop();
                if (name == "SetException")
                    Emit($"dn2cpp_tcs_set_exception((Dn2CppTask*)({t.Expr}), {Cast(ex, "Dn2CppObject*")});");
                else
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_task_try_set_exception((Dn2CppTask*)({t.Expr}), {Cast(ex, "Dn2CppObject*")})");
                return true;
            }
            // SetCanceled([CancellationToken]) — the transition allocates its own
            // OperationCanceledException (the token argument is popped and ignored);
            // register the exception type so a typed catch matches an awaited fault.
            case ("System.Threading.Tasks.TaskCompletionSource", "SetCanceled"):
            case ("System.Threading.Tasks.TaskCompletionSource", "TrySetCanceled"):
            {
                if (sig.ParameterTypes.Length == 1)
                    Pop(); // CancellationToken (ignored — the OCE carries no token)
                var t = Pop();
                EmitCanceledExcRegistration();
                if (name == "SetCanceled")
                    Emit($"dn2cpp_tcs_set_canceled((Dn2CppTask*)({t.Expr}));");
                else
                    Push(StackKind.I4, "int32_t", $"dn2cpp_task_try_set_canceled((Dn2CppTask*)({t.Expr}))");
                return true;
            }
            // Task state inspection (also inherited by Task<T>): map directly to the
            // runtime status field.
            case ("System.Threading.Tasks.Task", "get_IsCompleted"):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"(((Dn2CppTask*)({t.Expr}))->status != DN2CPP_TASK_PENDING ? 1 : 0)");
                return true;
            }
            case ("System.Threading.Tasks.Task", "get_IsCompletedSuccessfully"):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"(((Dn2CppTask*)({t.Expr}))->status == DN2CPP_TASK_SUCCEEDED ? 1 : 0)");
                return true;
            }
            case ("System.Threading.Tasks.Task", "get_IsFaulted"):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"(((Dn2CppTask*)({t.Expr}))->status == DN2CPP_TASK_FAULTED ? 1 : 0)");
                return true;
            }
            case ("System.Threading.Tasks.Task", "get_IsCanceled"):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"(((Dn2CppTask*)({t.Expr}))->status == DN2CPP_TASK_CANCELED ? 1 : 0)");
                return true;
            }
            // Task.Id: the positive per-task identifier minted at alloc time.
            // Monotonic and unique, like real .NET's; the numbering itself differs,
            // so deterministic programs may compare Ids but not print raw values.
            case ("System.Threading.Tasks.Task", "get_Id"):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"(((Dn2CppTask*)({t.Expr}))->id)");
                return true;
            }
            // Task.Exception: FAULTED -> the fault wrapped in an AggregateException
            // (minted on first access, cached — identity-stable across reads); any
            // other status -> null, CANCELED included, matching real .NET. The bare
            // stored fault is untouched: await/Wait re-raise it directly.
            case ("System.Threading.Tasks.Task", "get_Exception"):
            {
                var t = Pop();
                PushEntry(new StackEntry($"dn2cpp_task_exception((Dn2CppTask*)({t.Expr}))",
                    StackKind.Ref, "Dn2CppObject*",
                    StaticType: TypeDesc.MakeExternal("System.AggregateException")));
                return true;
            }
            // Task.Status: the same status field widened to real .NET's TaskStatus,
            // whose values are the ABI (verified against real .NET): Created 0,
            // WaitingForActivation 1, WaitingToRun 2, Running 3,
            // WaitingForChildrenToComplete 4, RanToCompletion 5, Canceled 6, Faulted 7.
            // Three of the four modeled states map exactly (SUCCEEDED/CANCELED/FAULTED);
            // PENDING is the only one that has to choose, because five real statuses
            // collapse into it. The `cold` pointer splits the two that are
            // DETERMINISTICALLY observable and so must be told apart:
            //   * cold != null  -> `new Task(...)` whose work Start() has not claimed
            //                      yet, which real .NET reports as Created.
            //   * cold == null  -> a promise (an async method's task, a TCS's task),
            //                      which real .NET reports as WaitingForActivation for
            //                      both shapes.
            // The remaining collapse is a started task still in flight: real .NET says
            // WaitingToRun/Running, this says WaitingForActivation — real .NET's own
            // answer there races the scheduler, so there is no fixed value to match.
            case ("System.Threading.Tasks.Task", "get_Status"):
            {
                var t = Pop();
                string tt = NewTemp("Dn2CppTask*");
                Emit($"{tt} = (Dn2CppTask*)({t.Expr});");
                string ct = CppTypes.Of(sig.ReturnType);
                Push(CppTypes.KindOf(sig.ReturnType), ct,
                    $"({ct})({tt}->status == DN2CPP_TASK_PENDING ? ({tt}->cold != nullptr ? 0 : 1)"
                    + $" : ({tt}->status == DN2CPP_TASK_SUCCEEDED ? 5"
                    + $" : ({tt}->status == DN2CPP_TASK_CANCELED ? 6 : 7)))");
                return true;
            }
            // Non-generic Task.FromException(Exception) -> a pre-completed faulted
            // Task (the generic FromException<T> binds in TranslateGenericIntrinsic).
            case ("System.Threading.Tasks.Task", "FromException") when sig.ParameterTypes.Length == 1:
            {
                var ex = Pop();
                Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_from_exception({Cast(ex, "Dn2CppObject*")})");
                return true;
            }
            // Non-generic Task.FromCanceled(CancellationToken) -> a pre-completed
            // CANCELED task (the generic FromCanceled<TResult> binds in
            // TranslateGenericIntrinsic). Awaiting/blocking on it throws
            // OperationCanceledException via the shared canceled-task path. Real .NET
            // throws ArgumentOutOfRangeException when the token is NOT canceled; that
            // validation is not modeled (callers only take the branch on an already-
            // canceled token).
            case ("System.Threading.Tasks.Task", "FromCanceled"):
            {
                Pop(); // CancellationToken (its canceled state is not re-validated)
                EmitCanceledExcRegistration();
                Push(StackKind.Ref, "Dn2CppTask*", "dn2cpp_task_from_canceled()");
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>The atomic operand shape an <c>Interlocked.Exchange</c>/<c>CompareExchange</c>
    /// location dispatches at — shared by the non-generic overloads (T = the ByRef
    /// element) and the generic MethodSpec forms (T = the instantiation argument).
    /// Enums classify by their UNDERLYING primitive's width (that is both the
    /// location's storage width and what the IL stack holds); Boolean is a 1-byte
    /// unsigned slot inside structs/arrays, so it swaps at U1. None = no atomic
    /// mapping (structs, ...) — the emitter carves it out loudly.</summary>
    private enum InterlockedWidth { None, Ref, U1, S1, U2, S2, I4, I8, R4, R8 }

    private static InterlockedWidth InterlockedWidthOfPrimitive(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Byte => InterlockedWidth.U1,
        PrimitiveTypeCode.SByte => InterlockedWidth.S1,
        PrimitiveTypeCode.Int16 => InterlockedWidth.S2,
        PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => InterlockedWidth.U2,
        PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 => InterlockedWidth.I4,
        PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
            or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => InterlockedWidth.I8,
        PrimitiveTypeCode.Single => InterlockedWidth.R4,
        PrimitiveTypeCode.Double => InterlockedWidth.R8,
        _ => InterlockedWidth.None,
    };

    private static InterlockedWidth InterlockedWidthOf(TypeDesc t)
    {
        var w = t switch
        {
            { Kind: TypeKind.Primitive } => InterlockedWidthOfPrimitive(t.Primitive),
            { Kind: TypeKind.Class, Class: { IsEnum: true } e } => InterlockedWidthOfPrimitive(e.EnumUnderlying),
            _ => InterlockedWidth.None,
        };
        // Everything the numeric table doesn't claim falls back on the stack-kind
        // test: object/string arrive as PrimitiveTypeCode.Object/String (still
        // Ref-kinded), classes/interfaces/arrays as their own TypeKinds.
        if (w == InterlockedWidth.None && CppTypes.KindOf(t) == StackKind.Ref)
            return InterlockedWidth.Ref;
        return w;
    }

    /// <summary>Emit <c>Interlocked.Exchange</c> (null <paramref name="comparand"/>) or
    /// <c>CompareExchange</c> over a T location: one runtime atomic at the location's
    /// DECLARED width, pushing the original value at its IL stack width.
    ///
    /// Sub-word (1-/2-byte) locations must never widen to the 4-byte helper: a
    /// `ref byte` may point into a packed struct field or array element (real-width
    /// storage) where a 4-byte RMW would clobber the live neighboring bytes. The
    /// declared-width op is also value-correct when the same `ref byte` points at an
    /// int32-widened slot (class instance fields and statics widen sub-word storage):
    /// the widened slot's upper bytes are always zero, and on a little-endian target
    /// the low-order byte the atomic touches IS the value byte, so those upper bytes
    /// stay zero — every supported target is little-endian, which this relies on.
    ///
    /// float/double route through one integer atomic over the bit pattern, matching
    /// .NET's bitwise CompareExchange comparison (-0.0 comparand does not match a
    /// +0.0 location; a bit-identical NaN comparand does); the result re-enters the
    /// IL stack as R8-as-double per the repo's float-return convention.</summary>
    private void EmitInterlockedSwap(TypeDesc t, StackEntry loc, StackEntry value, StackEntry? comparand, string opName)
    {
        if (InterlockedWidthOf(t) == InterlockedWidth.Ref)
        {
            string cpp = CppTypes.Of(t);
            string call = comparand is { } rc
                ? $"dn2cpp_interlocked_cmpxchg_ref((Dn2CppObject**)({loc.Expr}), {Cast(value, "Dn2CppObject*")}, {Cast(rc, "Dn2CppObject*")})"
                : $"dn2cpp_interlocked_exchange_ref((Dn2CppObject**)({loc.Expr}), {Cast(value, "Dn2CppObject*")})";
            Push(StackKind.Ref, cpp, $"({cpp}){call}");
            return;
        }
        // (helper suffix, its C++ operand type, result re-widening cast(s), push shape)
        var (suffix, argT, wrap, kind, ct) = InterlockedWidthOf(t) switch
        {
            InterlockedWidth.U1 => ("i1", "int8_t", "(int32_t)(uint8_t)", StackKind.I4, "int32_t"),
            InterlockedWidth.S1 => ("i1", "int8_t", "(int32_t)", StackKind.I4, "int32_t"),
            InterlockedWidth.S2 => ("i2", "int16_t", "(int32_t)", StackKind.I4, "int32_t"),
            InterlockedWidth.U2 => ("i2", "int16_t", "(int32_t)(uint16_t)", StackKind.I4, "int32_t"),
            InterlockedWidth.I4 => ("i4", "int32_t", "", StackKind.I4, "int32_t"),
            InterlockedWidth.I8 => ("i8", "int64_t", "", StackKind.I8, "int64_t"),
            InterlockedWidth.R4 => ("r4", "float", "(double)", StackKind.R8, "double"),
            InterlockedWidth.R8 => ("r8", "double", "", StackKind.R8, "double"),
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Interlocked.{opName}<{t}> " +
                "has no atomic width mapping (reference / integer / enum / float / double T only)"),
        };
        string fn = comparand is null ? $"dn2cpp_interlocked_exchange_{suffix}" : $"dn2cpp_interlocked_cmpxchg_{suffix}";
        string args = $"({argT}*)({loc.Expr}), ({argT})({value.Expr})";
        if (comparand is { } c)
            args += $", ({argT})({c.Expr})";
        Push(kind, ct, $"{wrap}{fn}({args})");
        // Push spills eagerly, so the atomic has executed here. When the ref is an
        // ldloca/ldarga pointer into an int32-promoted local/arg slot (SlotAddr),
        // the declared-width write left the slot's upper bytes stale (e.g. an sbyte
        // local holding -1 is the sign-extended slot 0xFFFFFFFF; a 1-byte exchange
        // of 0 leaves 0xFFFFFF00, which a direct slot read would see as -256) —
        // re-normalize exactly like NoteByRefSlotFixup does after ordinary calls.
        // Real-width pointers (array elements, span refs, real-width struct fields)
        // carry the storage-typed pointer, not the promoted one, and are skipped.
        string st = CppTypes.StorageOf(t);
        string ctOf = CppTypes.Of(t);
        if (st != ctOf && loc.Kind == StackKind.Ptr && loc.SlotAddr && loc.CppType == ctOf + "*")
            Emit($"*({ctOf}*)({loc.Expr}) = ({ctOf})(*({st}*)({loc.Expr}));");
    }

    /// <summary>Pops a ContinueWith overload's trailing arguments — everything past the
    /// delegate (and the state operand, where the overload has one) — and returns the
    /// C++ expression for its <c>TaskContinuationOptions</c>, or <c>"0"</c> when the
    /// overload carries none.
    ///
    /// <para><b>The distinction this method exists to keep is between a HINT and a
    /// FILTER.</b> A trailing CancellationToken or TaskScheduler is a scheduling hint
    /// dn2cpp's cooperative scheduler does not honour, so dropping it changes nothing an
    /// awaiting program can observe. <c>TaskContinuationOptions</c> is not that: its
    /// NotOnRanToCompletion / NotOnFaulted / NotOnCanceled bits — which the OnlyOn* names
    /// are pairs of — decide whether the delegate RUNS, and dropping them would run an
    /// OnlyOnFaulted continuation after a successful antecedent. So the value is carried
    /// to the runtime node, and as a RUN-TIME operand rather than folded, because the IL
    /// is free to compute it.</para>
    ///
    /// <para>Shared by the non-generic <c>ContinueWith</c> arm and the generic
    /// <c>ContinueWith&lt;TNewResult&gt;</c> one in MethodCompiler.GenericIntrinsic.cs —
    /// one popper, so the two cannot disagree about which trailing argument is which.</para></summary>
    private string PopContinuationHints(System.Collections.Immutable.ImmutableArray<TypeDesc> pts, int kept)
    {
        string options = "0";
        for (int i = pts.Length - 1; i >= kept; i--)
        {
            var popped = Pop();
            if (pts[i] is { Kind: TypeKind.Class, Class.FullName: "System.Threading.Tasks.TaskContinuationOptions" })
                options = $"(int32_t)({popped.Expr})";
        }
        return options;
    }
}
