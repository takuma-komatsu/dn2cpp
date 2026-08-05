using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private bool TryEmitStringsIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        // Numeric Parse/TryParse (all integer widths + Double/Single, every
        // NumberStyles/provider/span shape) — consolidated below the switch.
        if (TryEmitParseIntrinsic(declType, name, sig))
            return true;
        switch (declType, name)
        {

            case ("System.Console", "WriteLine") when sig.ParameterTypes.Length == 0:
                Emit("dn2cpp_console_writeline_empty();");
                return true;
            case ("System.Console", "WriteLine") when sig.ParameterTypes.Length == 1:
                EmitConsoleWrite(Pop(), sig.ParameterTypes[0], newline: true);
                return true;
            case ("System.Console", "Write") when sig.ParameterTypes.Length == 1:
                EmitConsoleWrite(Pop(), sig.ParameterTypes[0], newline: false);
                return true;
            // Composite-format overloads: build the string via the string.Format
            // helpers, then write it. The params ReadOnlySpan<object> overload (4+
            // loose args, net10) formats over the span's data pointer + length.
            case ("System.Console", "WriteLine") when IsConsoleFormatOverload(sig):
            {
                string fmt = BuildStringFormatExpr(sig);
                Emit($"dn2cpp_console_writeline_str({fmt});");
                return true;
            }
            case ("System.Console", "Write") when IsConsoleFormatOverload(sig):
            {
                string fmt = BuildStringFormatExpr(sig);
                Emit($"dn2cpp_console_write_str({fmt});");
                return true;
            }
            case ("System.Console", "WriteLine") when IsSpanFormatOverload(sig):
            {
                string fmt = BuildStringFormatExprSpan(sig);
                Emit($"dn2cpp_console_writeline_str({fmt});");
                return true;
            }
            case ("System.Console", "Write") when IsSpanFormatOverload(sig):
            {
                string fmt = BuildStringFormatExprSpan(sig);
                Emit($"dn2cpp_console_write_str({fmt});");
                return true;
            }

            // Console.Error is the process stderr TextWriter. System.Console is an intrinsic
            // type, so get_Error routes here with no real body. Return a cached,
            // header-stamped managed singleton (Dn2Cpp.Runtime.Dn2CppConsoleWriter, a real
            // TextWriter subtype) rather than the header-less runtime writer struct, so a
            // callvirt against it dispatches through a real vtable and a spilled receiver
            // (the interpolated-string case) stays sound. The non-spilled fast path below
            // still intercepts Write/WriteLine by the receiver's static C++ type, keeping
            // byte-identical output. With no CoreLib in the load set the subtype is
            // inexpressible (its base is an External name, so it has no vtable to dispatch
            // through) and ConsoleErrorWriter answers null: fall back to the header-less
            // runtime writer, which is the whole of what that load set can model.
            case ("System.Console", "get_Error"):
            {
                if (Comp.ConsoleErrorWriter() is { } ew)
                    PushEntry(new StackEntry("dn2cpp_console_error_writer()", StackKind.Ref,
                        ew.Cls.CppStructName + "*", StaticType: TypeDesc.MakeClass(ew.Cls)));
                else
                    Push(StackKind.Ref, "Dn2CppTextWriter*", "dn2cpp_console_error()");
                return true;
            }
            // Write/WriteLine on that TextWriter. The receiver must be the get_Error writer
            // (PopTextWriterReceiver throws loudly otherwise — no silent carve-out); the
            // value is rendered with the same per-type formatting as Console.Write/WriteLine
            // but routed to the writer's stream. Composite-format / char[] / other overloads
            // are not reached by the console closure and fall through to a loud NotSupported.
            case ("System.IO.TextWriter", "WriteLine") when sig.ParameterTypes.Length == 0:
            {
                string w = PopTextWriterReceiver();
                Emit($"dn2cpp_textwriter_writeline_empty({w});");
                return true;
            }
            case ("System.IO.TextWriter", "WriteLine") when sig.ParameterTypes.Length == 1:
            {
                var arg = Pop();
                string w = PopTextWriterReceiver();
                EmitConsoleWrite(arg, sig.ParameterTypes[0], newline: true, writer: w);
                return true;
            }
            case ("System.IO.TextWriter", "Write") when sig.ParameterTypes.Length == 1:
            {
                var arg = Pop();
                string w = PopTextWriterReceiver();
                EmitConsoleWrite(arg, sig.ParameterTypes[0], newline: false, writer: w);
                return true;
            }

            case ("System.String", "Concat") when sig.ParameterTypes.Length is >= 2 and <= 4
                                                  && sig.ParameterTypes.All(t => t.IsString):
            {
                int n = sig.ParameterTypes.Length;
                var args = new string[n];
                for (int i = n - 1; i >= 0; i--)
                    args[i] = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_concat{n}({string.Join(", ", args)})");
                return true;
            }
            // string -> ReadOnlySpan<char> implicit conversion: a span {ptr,len} over
            // the string's UTF-16 buffer (null string -> empty span). Roslyn emits this
            // when lowering a char-in-concat to the span String.Concat overload.
            case ("System.String", "op_Implicit") when sig.ParameterTypes is [{ IsString: true }]
                                                       && IsCharSpan(sig.ReturnType):
            {
                string st = NewTemp("Dn2CppString*");
                Emit($"{st} = {Cast(Pop(), "Dn2CppString*")};");
                string span = NewTemp(CppTypes.Of(sig.ReturnType));
                Emit($"{span}.f__reference = {st} ? (char16_t*){st}->chars : nullptr;");
                Emit($"{span}.f__length = {st} ? {st}->length : 0;");
                Push(StackKind.Struct, CppTypes.Of(sig.ReturnType), span);
                return true;
            }
            // String.Concat(ReadOnlySpan<char>...) — the overload Roslyn lowers a
            // char-in-concat to (e.g. `c.ToString + s` builds a 1-char span over the char
            // and span-wraps the strings). Lift each operand's {ptr,len} and concatenate in
            // the runtime; net10 added the 5-span overload.
            case ("System.String", "Concat") when sig.ParameterTypes.Length is >= 2 and <= 5
                                                  && sig.ParameterTypes.All(IsCharSpan):
            {
                int n = sig.ParameterTypes.Length;
                var spans = new string[n];
                for (int i = n - 1; i >= 0; i--)
                {
                    string sp = NewTemp(CppTypes.Of(sig.ParameterTypes[i]));
                    Emit($"{sp} = {Pop().Expr};");
                    spans[i] = sp;
                }
                string args = string.Join(", ", spans.Select(sp => $"{sp}.f__reference, {sp}.f__length"));
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_concat_spanchars{n}({args})");
                return true;
            }
            // string.Join(separator, string[], int startIndex, int count) — the
            // 4-arg slice form; the helper carries .NET's checks (null array ANE,
            // bad slice catchable AOORE).
            case ("System.String", "Join")
                when sig.ParameterTypes is [{ IsString: true }, { Kind: TypeKind.SZArray },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var arr = Pop();
                var sep = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_join_ref_range({Cast(sep, "Dn2CppString*")}, (Dn2CppArrayRef*)({arr.Expr}), "
                    + $"{Cast(start, "int32_t")}, {Cast(count, "int32_t")})");
                return true;
            }
            // string.Join(separator, string[]/object[]) — the non-generic array
            // overloads (string[]/object[] bind here rather than the IEnumerable<T>
            // generic). Separator is a string or char; elements via Object.ToString
            // (works for both string[] and object[]).
            case ("System.String", "Join")
                when sig.ParameterTypes is [_, { Kind: TypeKind.SZArray }]:
            {
                var arr = Pop();
                var sep = Pop();
                string sepStr = sep.Kind == StackKind.Ref
                    ? Cast(sep, "Dn2CppString*")
                    : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_join_ref({sepStr}, (Dn2CppArrayRef*)({arr.Expr}))");
                return true;
            }
            // string.Join(string|char, params ReadOnlySpan<object|string>) — the
            // net10 params-span overloads (Roslyn prefers them over the array
            // forms for loose args). Elements via Object.ToString over the span's
            // data pointer + length.
            case ("System.String", "Join")
                when sig.ParameterTypes is [_, var jsp] && (IsObjectSpan(jsp) || IsStringSpan(jsp)):
            {
                string sp = PopSpanTemp(sig.ParameterTypes[1]);
                var sep = Pop();
                string sepStr = sep.Kind == StackKind.Ref
                    ? Cast(sep, "Dn2CppString*")
                    : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_join_objs({sepStr}, (Dn2CppObject* const*){sp}.f__reference, {sp}.f__length)");
                return true;
            }
            // string.Concat(params ReadOnlySpan<object|string>) — the net10
            // params-span overloads; concat of the elements' ToString.
            case ("System.String", "Concat")
                when sig.ParameterTypes is [var csp] && (IsObjectSpan(csp) || IsStringSpan(csp)):
            {
                string sp = PopSpanTemp(sig.ParameterTypes[0]);
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_concat_objs((Dn2CppObject* const*){sp}.f__reference, {sp}.f__length)");
                return true;
            }
            // string.Join(separator, IEnumerable<string>) over a List<string> — the
            // non-generic overload binds here (preferred over the Join<T> generic)
            // for a List<string> argument; materialize the live prefix and join via
            // the count-aware ref helper. Value-element lists (List<int>, …)
            // have no such non-generic overload and bind to Join<T> instead, handled
            // in TranslateGenericIntrinsic.
            case ("System.String", "Join")
                when sig.ParameterTypes.Length == 2 && StackDepth > 0
                     && TryListBacking(Top) is { Elem: { } jel } && RepOf(jel) == ArrRep.Ref:
            {
                var lb = TryListBacking(Pop())!.Value;
                var sep = Pop();
                string sepStr = sep.Kind == StackKind.Ref
                    ? Cast(sep, "Dn2CppString*")
                    : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_join_ref_n({sepStr}, (Dn2CppArrayRef*)({lb.Items}), {lb.Count})");
                return true;
            }
            // string.Join(separator, IEnumerable<string>) over a concrete non-List
            // collection (SortedSet<string>, a Sorted* Keys/Values view): the element
            // type of this non-generic overload is always string. Enumerate via the
            // interface. Guarded before pop so a non-match falls through clean.
            case ("System.String", "Join")
                when sig.ParameterTypes.Length == 2 && StackDepth > 0
                     && IsConcreteEnumerableOperand(Top)
                     && CanEmitEnumerableJoin(TypeDesc.MakePrimitive(PrimitiveTypeCode.String)):
            {
                var col = Pop();
                var sep = Pop();
                string sepStr = sep.Kind == StackKind.Ref
                    ? Cast(sep, "Dn2CppString*")
                    : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
                TryEmitEnumerableJoin(col, TypeDesc.MakePrimitive(PrimitiveTypeCode.String), sepStr);
                return true;
            }
            // string.Join(separator, IEnumerable<string>) over a bare interface
            // operand (a string[] or a managed collection at runtime) — runtime-
            // discriminated dual path; element type is string.
            case ("System.String", "Join")
                when sig.ParameterTypes.Length == 2 && StackDepth > 0
                     && IsBareCollectionInterface(Top)
                     && CanEmitEnumerableJoin(TypeDesc.MakePrimitive(PrimitiveTypeCode.String)):
            {
                var col = Pop();
                var sep = Pop();
                string sepStr = sep.Kind == StackKind.Ref
                    ? Cast(sep, "Dn2CppString*")
                    : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
                TryEmitBareInterfaceJoin(col, TypeDesc.MakePrimitive(PrimitiveTypeCode.String), sepStr);
                return true;
            }
            // Concat over an object[]/string[] (params) — each element via ToString.
            case ("System.String", "Concat") when sig.ParameterTypes is [{ Kind: TypeKind.SZArray }]:
            {
                var arr = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_concat_objects((Dn2CppArrayRef*)({arr.Expr}))");
                return true;
            }
            // string.Concat(IEnumerable<string>) over a List<string> — the
            // non-generic overload binds here for a List<string> argument; without
            // this guard the single-object fallback below would ToString the List
            // itself (silently wrong). Restricted to reference-element Lists, whose
            // elements concat_objects formats via Object.ToString.
            case ("System.String", "Concat")
                when sig.ParameterTypes.Length == 1 && StackDepth > 0
                     && TryListBacking(Top) is { Elem: { } cel } && RepOf(cel) == ArrRep.Ref:
            {
                var lb = TryListBacking(Pop())!.Value;
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_concat_objects_n((Dn2CppArrayRef*)({lb.Items}), {lb.Count})");
                return true;
            }
            // string.Concat(IEnumerable<string>) over a concrete non-List collection
            // (SortedSet<string>, a Sorted* Keys/Values view): element type is string.
            // Enumerate with an empty separator. MUST precede the single-object Concat
            // fallback below, which would otherwise ToString the collection itself
            // (silently wrong). Guarded before pop so a non-match falls through.
            case ("System.String", "Concat")
                when sig.ParameterTypes.Length == 1 && StackDepth > 0
                     && IsConcreteEnumerableOperand(Top)
                     && CanEmitEnumerableJoin(TypeDesc.MakePrimitive(PrimitiveTypeCode.String)):
            {
                TryEmitEnumerableJoin(Pop(), TypeDesc.MakePrimitive(PrimitiveTypeCode.String), null);
                return true;
            }
            // string.Concat(IEnumerable<string>) over a bare interface operand (a
            // string[] or a managed collection at runtime). MUST precede the single-
            // object fallback below. Runtime-discriminated dual path.
            case ("System.String", "Concat")
                when sig.ParameterTypes.Length == 1 && StackDepth > 0
                     && IsBareCollectionInterface(Top)
                     && CanEmitEnumerableJoin(TypeDesc.MakePrimitive(PrimitiveTypeCode.String)):
            {
                TryEmitBareInterfaceJoin(Pop(), TypeDesc.MakePrimitive(PrimitiveTypeCode.String), null);
                return true;
            }
            // Concat with object/mixed operands (value + string lowers to these):
            // each operand is formatted via Object.ToString, then concatenated.
            case ("System.String", "Concat") when sig.ParameterTypes.Length is >= 1 and <= 4:
            {
                int n = sig.ParameterTypes.Length;
                var args = new string[n];
                for (int i = n - 1; i >= 0; i--)
                    args[i] = $"dn2cpp_object_tostring({Cast(Pop(), "Dn2CppObject*")})";
                Push(StackKind.Ref, "Dn2CppString*",
                    n == 1 ? args[0] : $"dn2cpp_string_concat{n}({string.Join(", ", args)})");
                return true;
            }
            case ("System.Delegate", "Combine") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_delegate_combine({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Delegate", "Remove") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_delegate_remove({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})");
                return true;
            }
            // Delegate.Target: the bound receiver out of the uniform delegate
            // layout ({target, method, prev}); null for a static-method delegate.
            // The runtime helper unwraps a reflection-bind node (CreateDelegate)
            // to the user-visible bound target.
            case ("System.Delegate", "get_Target"):
            {
                var d = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_delegate_get_target({Cast(d, "Dn2CppObject*")})");
                return true;
            }
            // Delegate.CreateDelegate — the MethodInfo-taking static forms:
            // (Type, MethodInfo[, bool throwOnBindFailure]) and (Type, object firstArgument,
            // MethodInfo[, bool]). They route to the same runtime binder as
            // MethodInfo.CreateDelegate; the firstArgument overloads are the closed forms (a
            // static method's first parameter can be bound; an instance method may bind a
            // null receiver). The string-method-name forms stay unmapped.
            case ("System.Delegate", "CreateDelegate") when sig.ParameterTypes.Length is >= 2 and <= 4:
            {
                var pt = sig.ParameterTypes;
                for (int i = 0; i < pt.Length; i++)
                    if (pt[i].IsString)
                        return false;
                bool hasThrowFlag = pt[^1].Kind == TypeKind.Primitive;
                int core = pt.Length - (hasThrowFlag ? 1 : 0);
                if (core is not (2 or 3))
                    return false;
                bool hasFirstArg = core == 3;
                Comp.NeedsReflectionDelegateBind = true;
                string throwExpr = "1";
                if (hasThrowFlag)
                {
                    var tf = Pop();
                    throwExpr = Cast(tf, "int32_t");
                }
                var m = Pop();
                string target = "nullptr";
                if (hasFirstArg)
                {
                    var fa = Pop();
                    target = Cast(fa, "Dn2CppObject*");
                }
                var ty = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_delegate_create({Cast(ty, "Dn2CppType*")}, {target}, (Dn2CppMethodRef*)({m.Expr}), {(hasFirstArg ? 1 : 0)}, {throwExpr})");
                return true;
            }
            // Delegate value equality/hash: the BCL bodies walk MulticastDelegate's
            // _invocationList (a field the uniform delegate layout does not have),
            // so they lower to the chain-aware runtime helpers — the same pair the
            // DN2CPP_TF_DELEGATE branches of dn2cpp_object_gethashcode/_equals
            // dispatch, so a direct call and a virtual Object-rooted call agree.
            case ("System.Delegate", "op_Equality") when sig.ParameterTypes.Length == 2:
            case ("System.Delegate", "op_Inequality") when sig.ParameterTypes.Length == 2:
            {
                var b = Pop();
                var a = Pop();
                string eq = $"dn2cpp_delegate_equal({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})";
                Push(StackKind.I4, "int32_t", name == "op_Equality" ? eq : $"({eq} == 0 ? 1 : 0)");
                return true;
            }
            case ("System.Delegate", "Equals") when sig.ParameterTypes.Length == 1:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_delegate_equal({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})");
                return true;
            }
            case ("System.Delegate", "GetHashCode") when sig.ParameterTypes.Length == 0:
            {
                var d = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_delegate_hash({Cast(d, "Dn2CppObject*")})");
                return true;
            }
            // Delegate.HasSingleTarget: a single-entry invocation chain (the
            // uniform layout's prev is the chain link, null = single).
            case ("System.Delegate", "get_HasSingleTarget"):
            {
                var d = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((((Dn2CppDelegate*){Cast(d, "Dn2CppObject*")})->prev == nullptr) ? 1 : 0)");
                return true;
            }
            // Delegate.Method: a reflection-bound delegate (CreateDelegate)
            // reports its methtab row's MethodInfo; an IL-constructed delegate
            // carries no method-metadata back-reference (f_method is a bare code
            // address), so it stays null — callers null-propagate a missing
            // MethodInfo into their "not valid" fallback.
            case ("System.Delegate", "get_Method"):
            {
                var d = Pop();
                Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_delegate_get_method({Cast(d, "Dn2CppObject*")})");
                return true;
            }

            // string.GetEnumerator() — delegate to the real transpiled CoreLib body
            // (`new CharEnumerator(this)`): the call site must be intercepted because
            // String is an intrinsic type, but the body and CharEnumerator transpile
            // like any other CoreLib IL. ReachStringMethod wires String's interface
            // map alongside (GetEnumerator is enumeration).
            case ("System.String", "GetEnumerator") when sig.ParameterTypes.Length == 0:
            {
                if (Comp.ReachStringMethod("GetEnumerator", ps => ps.Length == 0) is not { } ge)
                    return false;
                var str = Pop();
                Comp.NoteNamedBodySymbol(Method, ge.Emittable);
                Push(StackKind.Ref, CppTypes.Of(ge.Signature.ReturnType),
                    $"{ge.Emittable.CppName}({Cast(str, "Dn2CppString*")})");
                return true;
            }
            // string.CompareTo(string) — ordinal three-way compare, matching every other
            // string ordering in dn2cpp (Comparer<string>.Default, sorts — see CompareExpr;
            // the real body's CurrentCulture compare is out of scope).
            case ("System.String", "CompareTo") when sig.ParameterTypes is [{ IsString: true }]:
            {
                var arg = Pop();
                var self = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare({Cast(self, "Dn2CppString*")}, {Cast(arg, "Dn2CppString*")}, 4)");
                return true;
            }
            // string.CompareTo(object) — delegate to the real transpiled body (its
            // null/type checks are plain IL; its inner CompareTo(string) call lands
            // on the ordinal case above).
            case ("System.String", "CompareTo") when sig.ParameterTypes is [{ IsObject: true }]:
            {
                if (Comp.ReachStringMethod("CompareTo",
                        ps => ps.Length == 1 && ps[0].IsObject) is not { } ct)
                    return false;
                var arg = Pop();
                var self = Pop();
                Comp.NoteNamedBodySymbol(Method, ct.Emittable);
                Push(StackKind.I4, "int32_t",
                    $"{ct.Emittable.CppName}({Cast(self, "Dn2CppString*")}, {Cast(arg, "Dn2CppObject*")})");
                return true;
            }
            // string.Intern / IsInterned — the runtime intern pool (all ldstr
            // literals are interned at startup, matching .NET).
            case ("System.String", "Intern") when sig.ParameterTypes is [{ IsString: true }]:
            {
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_intern({Cast(s, "Dn2CppString*")})");
                return true;
            }
            case ("System.String", "IsInterned") when sig.ParameterTypes is [{ IsString: true }]:
            {
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_is_interned({Cast(s, "Dn2CppString*")})");
                return true;
            }
            case ("System.String", "get_Length"):
            {
                // Nothing downstream of this dereference checks, so it carries the shared
                // inline guard on the DN2CPP_NULL_CHECKS axis rather than a private one.
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_null_check({Cast(a, "Dn2CppString*")})->length");
                return true;
            }
            case ("System.String", "get_Chars"):
            {
                var idx = Pop();
                var str = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_string_get_char({Cast(str, "Dn2CppString*")}, {idx.Expr})");
                return true;
            }
            // String.ToCharArray — copy the string's UTF-16 code units
            // into a fresh char[] (packed Dn2CppArrayN, elemSize 2). The char[] type-info
            // is the precise per-element array handle so result.GetType == typeof(char[])
            //; NoteArrayElementType emits that symbol. The (int, int) substring
            // form rides the range helper (catchable AOORE on a bad slice).
            case ("System.String", "ToCharArray")
                when sig.ParameterTypes.Length == 0
                    || sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                string range = "";
                if (sig.ParameterTypes.Length == 2)
                {
                    var len = Pop();
                    var start = Pop();
                    range = $", {Cast(start, "int32_t")}, {Cast(len, "int32_t")}";
                }
                var str = Pop();
                var charEl = TypeDesc.MakePrimitive(PrimitiveTypeCode.Char);
                Comp.NoteArrayElementType(charEl);
                string ti = PreciseArrayTypeInfoExpr(charEl);
                string r = NewTemp("Dn2CppArrayN*");
                string fn = range.Length == 0 ? "dn2cpp_string_to_chararray" : "dn2cpp_string_to_chararray_range";
                Emit($"{r} = {fn}({Cast(str, "Dn2CppString*")}{range}, {ti});");
                PushEntry(new StackEntry(r, StackKind.Ref, "Dn2CppArrayN*",
                    StaticType: TypeDesc.MakeSZArray(charEl)));
                return true;
            }
            // string.EnumerateRunes(): the real body is just
            // `new StringRuneEnumerator(this)`, but String is an intrinsic type so it never
            // compiles — build the enumerator struct here instead. StringRuneEnumerator
            // itself and the Rune decoding it drives are ordinary CoreLib IL that transpiles
            // clean; only this entry point needs modeling. Zero-init covers _current
            // (Rune U+0000) and _nextIndex, exactly like the real ctor.
            case ("System.String", "EnumerateRunes")
                    when sig.ParameterTypes.Length == 0
                         && sig.ReturnType is { Kind: TypeKind.Class, Class: { } runeEnumCls }:
            {
                Comp.EnsureCompleted(runeEnumCls);
                var strFld = runeEnumCls.Fields.FirstOrDefault(f => f.Name == "_string")
                    ?? throw new NotSupportedException(
                        "String.EnumerateRunes: StringRuneEnumerator has no _string field (layout drift)");
                string ct = CppTypes.Of(sig.ReturnType);
                string en = NewTemp(ct);
                Emit($"{en} = {ct}{{}};");
                Emit($"{en}.{strFld.CppName} = {Cast(Pop(), "Dn2CppString*")};");
                Push(StackKind.Struct, ct, en);
                return true;
            }
            // GetRawStringData: ref to the first char of the string's buffer. The real body
            // uses object-header pointer arithmetic (Unsafe); a Dn2CppString already stores
            // its buffer pointer, so return it. This is what string.AsSpan /
            // MemoryExtensions over a string reach.
            //
            // GetPinnableReference: the same ref-to-first-char — what `fixed (char* p = str)`
            // lowers to. A Dn2CppString's buffer is always NUL-terminated
            // (dn2cpp_string_alloc allocates length+1 and string literals emit `u"..."`), so
            // an empty string yields a valid ref to the terminating '\0', matching .NET's ref
            // to `_firstChar` with no length-zero null-ref special case (unlike Span). The
            // non-moving GC makes the pin itself a no-op.
            case ("System.String", "GetRawStringData"):
            case ("System.String", "GetPinnableReference"):
            {
                var str = Pop();
                Push(StackKind.Ptr, "char16_t*", $"(char16_t*)({Cast(str, "Dn2CppString*")})->chars");
                return true;
            }
            // FastAllocateString(length): an uninitialized (zeroed here, matching
            // .NET's GC-cleared memory) writable string the caller fills through
            // GetRawStringData — the Guid.ToString / TryFormatCore shape. The
            // net10 overload takes nint; the classic one int32.
            case ("System.String", "FastAllocateString")
                when sig.ParameterTypes is [{ Primitive: PrimitiveTypeCode.Int32 or PrimitiveTypeCode.IntPtr }]:
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_fast_allocate((int32_t)({Pop().Expr}))");
                return true;
            // string.CopyTo(Span<char>): copy the whole string into the destination span,
            // throwing ArgumentException when it does not fit (real .NET's
            // ThrowArgumentException_DestinationTooShort; the real body routes through
            // Buffer.Memmove, an InternalCall).
            case ("System.String", "CopyTo")
                    when sig.ParameterTypes is [{ } d0] && IsCharSpan(d0):
            {
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
                string sp = SpanPtr(Pop(), spanCt);
                string st = NewTemp("Dn2CppString*");
                Emit($"{st} = {Cast(Pop(), "Dn2CppString*")};");
                Emit($"if ({st}->length > {sp}->f__length) dn2cpp_throw_argument();");
                Emit($"if ({st}->length > 0) std::memcpy({sp}->f__reference, "
                    + $"{st}->chars, (size_t)({st}->length) * sizeof(char16_t));");
                return true;
            }
            // string.TryCopyTo(Span<char>): the non-throwing twin — false when the
            // destination is too short (the real body routes through Buffer.Memmove, an
            // InternalCall).
            case ("System.String", "TryCopyTo")
                    when sig.ParameterTypes is [{ } t0] && IsCharSpan(t0):
            {
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
                string sp = SpanPtr(Pop(), spanCt);
                string st = NewTemp("Dn2CppString*");
                Emit($"{st} = {Cast(Pop(), "Dn2CppString*")};");
                string okT = NewTemp("int32_t");
                Emit($"{okT} = ({st}->length <= {sp}->f__length) ? 1 : 0;");
                Emit($"if ({okT} && {st}->length > 0) std::memcpy({sp}->f__reference, "
                    + $"{st}->chars, (size_t)({st}->length) * sizeof(char16_t));");
                Push(StackKind.I4, "int32_t", okT);
                return true;
            }
            // string.CopyTo(int sourceIndex, char[] destination, int destinationIndex,
            // int count) — the legacy array form; the helper carries .NET's checks
            // (null destination ANE first, then catchable AOORE range checks).
            case ("System.String", "CopyTo")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.SZArray },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var dstIdx = Pop();
                var arr = Pop();
                var srcIdx = Pop();
                var s = Pop();
                Emit($"dn2cpp_str_copyto_chararray({Cast(s, "Dn2CppString*")}, {Cast(srcIdx, "int32_t")}, "
                    + $"(Dn2CppArrayN*)({arr.Expr}), {Cast(dstIdx, "int32_t")}, {Cast(count, "int32_t")});");
                return true;
            }
            // String.strlen(byte* ptr) — the internal NUL-scan behind
            // Marshal.PtrToStringUTF8. The C strlen, truncated to int like the BCL contract.
            case ("System.String", "strlen") when sig.ParameterTypes.Length == 1:
            {
                var ptr = Pop();
                Push(StackKind.I4, "int32_t", $"(int32_t)::strlen((const char*)({ptr.Expr}))");
                return true;
            }
            // string.GetHashCode() (ordinal, case-sensitive). Real .NET uses a
            // per-process-randomized Marvin hash, so its value is never observable across
            // runs — a hash table only needs equal strings to hash equally and a stable
            // result within the run, which dn2cpp_string_hashcode (the same helper the
            // object-hash path uses for strings) provides.
            case ("System.String", "GetHashCode") when sig.ParameterTypes.Length == 0:
            {
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_string_hashcode({Cast(s, "Dn2CppString*")})");
                return true;
            }
            // Static string.GetHashCode(ReadOnlySpan<char>): the ordinal span hash
            // backing Dictionary<string,…>'s ReadOnlySpan<char> alternate lookup.
            // Shares dn2cpp_chars_hashcode with the instance form, so a span and the
            // equal string hash identically (required for the alternate key to hit).
            case ("System.String", "GetHashCode")
                    when sig.ParameterTypes is [{ } p0] && IsCharSpan(p0):
            {
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
                string sp = SpanPtr(Pop(), spanCt);
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_chars_hashcode((const char16_t*){sp}->f__reference, {sp}->f__length)");
                return true;
            }
            // Static string.GetHashCode(ReadOnlySpan<char>, StringComparison): the ordinal
            // (4) / OrdinalIgnoreCase (5) span hashes reuse the same helpers as the
            // comparisonless span form, so a span and the equal string hash identically.
            // The comparison value routes through dn2cpp_str_comparison_fold — THE single
            // StringComparison map (dn2cpp_system_string.cpp) — so the culture-sensitive
            // values (0..3) hash with the same ordinal fold the comparison helpers answer
            // them with (equal-under-the-fold inputs must hash alike). Matched on
            // (charspan, enum) so the comparisonless form above keeps its own arm.
            case ("System.String", "GetHashCode")
                    when sig.ParameterTypes is [{ } gp0, { Kind: TypeKind.Class, Class.IsEnum: true }] && IsCharSpan(gp0):
            {
                string cmp = NewTemp("int32_t");
                Emit($"{cmp} = dn2cpp_str_comparison_fold((int32_t)({Pop().Expr}));");
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
                string sp = SpanPtr(Pop(), spanCt);
                Push(StackKind.I4, "int32_t",
                    $"(({cmp}) == 5 ? dn2cpp_chars_hashcode_oic((const char16_t*){sp}->f__reference, {sp}->f__length)"
                    + $" : dn2cpp_chars_hashcode((const char16_t*){sp}->f__reference, {sp}->f__length))");
                return true;
            }
            // GetHashCodeOrdinalIgnoreCase: the real body folds via Marvin + TextInfo/ICU
            // for non-ASCII; lower to the ASCII-fold FNV hash, consistent with the
            // EqualsIgnoreCase interception, so the ICU cascade stays unreachable. The
            // instance form hashes the receiver string; the static form a
            // ReadOnlySpan<char>.
            case ("System.String", "GetHashCodeOrdinalIgnoreCase") when sig.ParameterTypes.Length == 0:
            {
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_string_hashcode_oic({Cast(s, "Dn2CppString*")})");
                return true;
            }
            case ("System.String", "GetHashCodeOrdinalIgnoreCase") when sig.ParameterTypes.Length == 1:
            {
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
                string sp = SpanPtr(Pop(), spanCt);
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_chars_hashcode_oic((const char16_t*){sp}->f__reference, {sp}->f__length)");
                return true;
            }
            case ("System.String", "op_Equality"):
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_string_equals({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")})");
                return true;
            }
            case ("System.String", "op_Inequality"):
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t", $"(!dn2cpp_string_equals({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}))");
                return true;
            }
            // ---- common String members (ordinal; ASCII case mapping) ----
            case ("System.String", "Substring"):
            {
                if (sig.ParameterTypes.Length == 2)
                {
                    var len = Pop();
                    var start = Pop();
                    var s = Pop();
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_str_substring({Cast(s, "Dn2CppString*")}, {start.Expr}, {len.Expr})");
                }
                else
                {
                    var start = Pop();
                    var s = Pop();
                    // The missing length is `Length - startIndex`, and the receiver
                    // read it needs must NOT happen here: spliced in as
                    // `(s)->length - i` it dereferences ahead of the helper's own
                    // null check, turning a null string into a SIGSEGV where .NET
                    // throws a catchable NullReferenceException. The _to_end helper
                    // does the same subtraction after that check, for free.
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_str_substring_to_end({Cast(s, "Dn2CppString*")}, {start.Expr})");
                }
                return true;
            }
            // IndexOf(string, StringComparison) — Ordinal/OrdinalIgnoreCase like
            // the Contains/StartsWith comparison overloads. Matched on the second
            // parameter's type: the other two-arg forms take an int startIndex,
            // and a handler that popped only (needle, receiver) here would
            // strand the receiver on the stack.
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ IsString: true }, var cmpTy]
                    && cmpTy.ToString() == "System.StringComparison":
            {
                var cmp = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_cmp({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {cmp.Expr})");
                return true;
            }
            // IndexOf(string, int startIndex, StringComparison) — Ordinal/
            // OrdinalIgnoreCase only, the forward-search mirror of the
            // LastIndexOf comparison overload. The third parameter must be the
            // enum: IndexOf(string, int, int count) is a different overload and
            // must not be consumed here.
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var cmp = Pop();
                var start = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_str_cmp({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {Cast(start, "int32_t")}, {cmp.Expr})");
                return true;
            }
            // ---- IndexOf, explicit arities. Each overload gets its own arm so the pops
            // always match the signature exactly: a shared guard lets IndexOf(char,
            // startIndex, count) fall into the two-arg handler, consuming count as
            // startIndex and stranding the receiver. Any unlisted shape falls through to
            // the loud NotSupportedException. ----
            // IndexOf(char): plain ordinal scan.
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var ch = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_char({Cast(s, "Dn2CppString*")}, (char16_t)({ch.Expr}), 0)");
                return true;
            }
            // IndexOf(char, int startIndex): forward scan to the end; the range
            // helper carries .NET's startIndex validation (catchable AOORE).
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var start = Pop();
                var ch = Pop();
                var s = Pop();
                // The count is `Length - startIndex`, computed inside the helper
                // behind its null check rather than off the receiver here, which
                // would fault the emitted body on a null string.
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_char_to_end({Cast(s, "Dn2CppString*")}, (char16_t)({ch.Expr}), {Cast(start, "int32_t")})");
                return true;
            }
            // IndexOf(char, int startIndex, int count).
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var ch = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_char_range({Cast(s, "Dn2CppString*")}, (char16_t)({ch.Expr}), {Cast(start, "int32_t")}, {Cast(count, "int32_t")})");
                return true;
            }
            // IndexOf(char, StringComparison): Ordinal/OrdinalIgnoreCase (culture
            // values fold onto them in the helper, like the other comparison
            // overloads — dn2cpp_str_comparison_fold).
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var cmp = Pop();
                var ch = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_char_cmp({Cast(s, "Dn2CppString*")}, (char16_t)({ch.Expr}), {cmp.Expr})");
                return true;
            }
            // IndexOf(string), ordinal. (.NET's 1-arg default is CurrentCulture;
            // dn2cpp has no ICU model, so this is a documented divergence — for
            // ASCII-only inputs the results coincide.)
            case ("System.String", "IndexOf") when sig.ParameterTypes is [{ IsString: true }]:
            {
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_str({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")})");
                return true;
            }
            // IndexOf(string, int startIndex), ordinal — the same documented
            // culture-default divergence as the 1-arg form. Rides the comparison
            // helper with Ordinal (4); empty needle -> startIndex, bad start ->
            // catchable AOORE.
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var start = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_str_cmp({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {Cast(start, "int32_t")}, 4)");
                return true;
            }
            // IndexOf(string, int startIndex, int count), ordinal — the same
            // documented culture-default divergence as the 1-arg form. The range
            // helper carries .NET's checks (null needle ANE, bad start/count
            // catchable AOORE, a match must fit inside the count window).
            case ("System.String", "IndexOf")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_indexof_str_range({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, "
                    + $"{Cast(start, "int32_t")}, {Cast(count, "int32_t")}, 4)");
                return true;
            }
            // Contains(string, StringComparison) — Ordinal/OrdinalIgnoreCase like
            // the StartsWith/EndsWith comparison overloads. (Must be matched before
            // the 1-arg case: a shared handler that popped only two values would
            // strand the receiver on the stack.)
            case ("System.String", "Contains")
                when sig.ParameterTypes.Length == 2 && sig.ParameterTypes[0].IsString:
            {
                var cmp = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_contains_cmp({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {cmp.Expr})");
                return true;
            }
            case ("System.String", "Contains") when sig.ParameterTypes.Length == 1:
            {
                var arg = Pop();
                var s = Pop();
                string find = sig.ParameterTypes[0].IsString
                    ? $"dn2cpp_str_indexof_str({Cast(s, "Dn2CppString*")}, {Cast(arg, "Dn2CppString*")})"
                    : $"dn2cpp_str_indexof_char({Cast(s, "Dn2CppString*")}, (char16_t)({arg.Expr}), 0)";
                Push(StackKind.I4, "int32_t", $"(({find}) >= 0)");
                return true;
            }
            case ("System.String", "StartsWith") when sig.ParameterTypes.Length == 1 && sig.ParameterTypes[0].IsString:
            {
                var p = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_str_startswith({Cast(s, "Dn2CppString*")}, {Cast(p, "Dn2CppString*")})");
                return true;
            }
            case ("System.String", "EndsWith") when sig.ParameterTypes.Length == 1 && sig.ParameterTypes[0].IsString:
            {
                var p = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_str_endswith({Cast(s, "Dn2CppString*")}, {Cast(p, "Dn2CppString*")})");
                return true;
            }
            // char overloads — s.StartsWith('/') / s.EndsWith('*').
            case ("System.String", "StartsWith")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var c = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_str_startswith_char({Cast(s, "Dn2CppString*")}, (char16_t)({c.Expr}))");
                return true;
            }
            case ("System.String", "EndsWith")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var c = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_str_endswith_char({Cast(s, "Dn2CppString*")}, (char16_t)({c.Expr}))");
                return true;
            }
            // (string, StringComparison) overloads — s.StartsWith("__", Ordinal) /
            // s.EndsWith(".dll", OrdinalIgnoreCase). The runtime helper takes the comparison
            // value (Ordinal=4 / OrdinalIgnoreCase=5; culture-aware values fold onto
            // them via dn2cpp_str_comparison_fold — no ICU model).
            case ("System.String", "StartsWith") when sig.ParameterTypes.Length == 2 && sig.ParameterTypes[0].IsString:
            {
                var cmp = Pop();
                var p = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_startswith_cmp({Cast(s, "Dn2CppString*")}, {Cast(p, "Dn2CppString*")}, {cmp.Expr})");
                return true;
            }
            case ("System.String", "EndsWith") when sig.ParameterTypes.Length == 2 && sig.ParameterTypes[0].IsString:
            {
                var cmp = Pop();
                var p = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_endswith_cmp({Cast(s, "Dn2CppString*")}, {Cast(p, "Dn2CppString*")}, {cmp.Expr})");
                return true;
            }
            // ---- LastIndexOf, explicit arities (same discipline as IndexOf). ----
            // LastIndexOf(char) — s.LastIndexOf(')').
            case ("System.String", "LastIndexOf")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var c = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_str_lastindexof_char({Cast(s, "Dn2CppString*")}, (char16_t)({c.Expr}))");
                return true;
            }
            // LastIndexOf(char, int startIndex): backward scan from startIndex
            // inclusive (count = startIndex + 1); the range helper carries .NET's
            // empty-source early-out and catchable AOORE validation.
            case ("System.String", "LastIndexOf")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var start = Pop();
                var ch = Pop();
                var s = Pop();
                string st = NewTemp("int32_t");
                Emit($"{st} = {Cast(start, "int32_t")};");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_lastindexof_char_range({Cast(s, "Dn2CppString*")}, (char16_t)({ch.Expr}), {st}, {st} + 1)");
                return true;
            }
            // LastIndexOf(char, int startIndex, int count).
            case ("System.String", "LastIndexOf")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var ch = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_lastindexof_char_range({Cast(s, "Dn2CppString*")}, (char16_t)({ch.Expr}), {Cast(start, "int32_t")}, {Cast(count, "int32_t")})");
                return true;
            }
            // LastIndexOf(string), ordinal. (.NET's 1-arg default is
            // CurrentCulture; dn2cpp has no ICU model, so this is the same
            // documented divergence as IndexOf(string) — ASCII inputs coincide.)
            // Empty needle -> Length, the .NET 5+ contract.
            case ("System.String", "LastIndexOf") when sig.ParameterTypes is [{ IsString: true }]:
            {
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_lastindexof_str({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")})");
                return true;
            }
            // LastIndexOf(string, StringComparison) — Ordinal/OrdinalIgnoreCase;
            // culture values fold onto them in the helper.
            case ("System.String", "LastIndexOf")
                when sig.ParameterTypes is [{ IsString: true }, { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var cmp = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_lastindexof_cmp({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {cmp.Expr})");
                return true;
            }
            // LastIndexOf(string, int startIndex), ordinal — the culture-default
            // divergence again. A match must end at or before startIndex (the
            // .NET 5+ range contract the helper implements).
            case ("System.String", "LastIndexOf")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var start = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_lastindexof_str_cmp({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {Cast(start, "int32_t")}, 4)");
                return true;
            }
            // LastIndexOf(string, int startIndex, int count), ordinal.
            case ("System.String", "LastIndexOf")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_lastindexof_str_range({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {Cast(start, "int32_t")}, {Cast(count, "int32_t")}, 4)");
                return true;
            }
            // LastIndexOf(string, int startIndex, StringComparison) — Ordinal/
            // OrdinalIgnoreCase only, like the StartsWith/EndsWith comparison
            // overloads. Enum third parameter required so the (string, int, int)
            // overload above is never consumed here.
            case ("System.String", "LastIndexOf")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var cmp = Pop();
                var start = Pop();
                var sub = Pop();
                var s = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_lastindexof_str_cmp({Cast(s, "Dn2CppString*")}, {Cast(sub, "Dn2CppString*")}, {Cast(start, "int32_t")}, {cmp.Expr})");
                return true;
            }
            // ---- IndexOfAny / LastIndexOfAny over a char[] set. The array
            // lowers to its data pointer + length (packed Dn2CppArrayN, elemSize
            // 2); a null array reaches the helper as a null set -> catchable
            // ArgumentNullException, .NET's contract. ----
            case ("System.String", "IndexOfAny") or ("System.String", "LastIndexOfAny")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray }]:
            {
                var arr = Pop();
                string sc = Cast(Pop(), "Dn2CppString*");
                string at = NewTemp("Dn2CppArrayN*");
                Emit($"{at} = (Dn2CppArrayN*)({arr.Expr});");
                string set = $"{at} != nullptr ? (const char16_t*)({at}->data) : nullptr";
                string setLen = $"{at} != nullptr ? {at}->length : 0";
                // The range these bare overloads scan is the receiver's own, so the
                // _to_end / _all helpers derive it after their null check — naming
                // `({sc})->length` here would dereference first and turn a null
                // string into a SIGSEGV.
                Push(StackKind.I4, "int32_t", name == "IndexOfAny"
                    ? $"dn2cpp_str_indexofany_to_end({sc}, {set}, {setLen}, 0)"
                    : $"dn2cpp_str_lastindexofany_all({sc}, {set}, {setLen})");
                return true;
            }
            // (char[], int startIndex): forward form scans to the end, backward
            // form from startIndex inclusive (count = startIndex + 1).
            case ("System.String", "IndexOfAny") or ("System.String", "LastIndexOfAny")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var start = Pop();
                var arr = Pop();
                string sc = Cast(Pop(), "Dn2CppString*");
                string at = NewTemp("Dn2CppArrayN*");
                Emit($"{at} = (Dn2CppArrayN*)({arr.Expr});");
                string st = NewTemp("int32_t");
                Emit($"{st} = {Cast(start, "int32_t")};");
                string set = $"{at} != nullptr ? (const char16_t*)({at}->data) : nullptr";
                string setLen = $"{at} != nullptr ? {at}->length : 0";
                // Same on the forward arm — its count is `Length - start`.
                // The backward arm takes both bounds from the caller (startIndex
                // and startIndex + 1) and never reads the receiver, so it keeps
                // the base helper.
                Push(StackKind.I4, "int32_t", name == "IndexOfAny"
                    ? $"dn2cpp_str_indexofany_to_end({sc}, {set}, {setLen}, {st})"
                    : $"dn2cpp_str_lastindexofany({sc}, {set}, {setLen}, {st}, {st} + 1)");
                return true;
            }
            // (char[], int startIndex, int count).
            case ("System.String", "IndexOfAny") or ("System.String", "LastIndexOfAny")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var count = Pop();
                var start = Pop();
                var arr = Pop();
                string sc = Cast(Pop(), "Dn2CppString*");
                string at = NewTemp("Dn2CppArrayN*");
                Emit($"{at} = (Dn2CppArrayN*)({arr.Expr});");
                string set = $"{at} != nullptr ? (const char16_t*)({at}->data) : nullptr";
                string setLen = $"{at} != nullptr ? {at}->length : 0";
                string fn = name == "IndexOfAny" ? "dn2cpp_str_indexofany" : "dn2cpp_str_lastindexofany";
                Push(StackKind.I4, "int32_t",
                    $"{fn}({sc}, {set}, {setLen}, {Cast(start, "int32_t")}, {Cast(count, "int32_t")})");
                return true;
            }
            case ("System.String", "ToUpper") when sig.ParameterTypes.Length == 0:
            case ("System.String", "ToUpperInvariant"):
            {
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_to_case({Cast(s, "Dn2CppString*")}, 1)");
                return true;
            }
            case ("System.String", "ToLower") when sig.ParameterTypes.Length == 0:
            case ("System.String", "ToLowerInvariant"):
            {
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_to_case({Cast(s, "Dn2CppString*")}, 0)");
                return true;
            }
            // The (CultureInfo) casing overloads. dn2cpp is invariant-only, so the culture
            // operand is popped and ignored and both lower to the same invariant BMP case
            // maps as the 0-arg forms above — exact for InvariantCulture and any culture
            // whose simple case fold coincides with it. Matched on the arity-1 CultureInfo
            // shape so the 0-arg forms keep their own arms.
            case ("System.String", "ToUpper")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Globalization.CultureInfo" }]:
            {
                Pop(); // culture — ignored (invariant-only)
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_to_case({Cast(s, "Dn2CppString*")}, 1)");
                return true;
            }
            case ("System.String", "ToLower")
                when sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.FullName: "System.Globalization.CultureInfo" }]:
            {
                Pop(); // culture — ignored (invariant-only)
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_to_case({Cast(s, "Dn2CppString*")}, 0)");
                return true;
            }
            // The IConvertible/IFormattable string.ToString(IFormatProvider) — a
            // string formats to itself, so the receiver is handed straight back and
            // the provider (culture formatting we don't model, and a plain string has
            // none) is popped and ignored. The 0-arg ToString() keeps its own path.
            case ("System.String", "ToString")
                when sig.ParameterTypes is [{ } pfp] && IsFormatProvider(pfp):
            {
                Pop(); // format provider — ignored
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", Cast(s, "Dn2CppString*"));
                return true;
            }
            // The internal string.TryGetSpan(int startIndex, int count, out
            // ReadOnlySpan<char> slice) — a bounds-checked slice over the receiver's UTF-16
            // buffer. The bounds test is .NET's unsigned form ((uint)start + (uint)count >
            // (uint)Length, widened to 64-bit so the addition cannot wrap); on failure the
            // out span is default {nullptr, 0} and it returns false.
            case ("System.String", "TryGetSpan")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.ByRef, Element: { } sliceEl }] && IsCharSpan(sliceEl):
            {
                var outRef = Pop();
                var count = Pop();
                var start = Pop();
                var s = Pop(); // receiver string
                string st = NewTemp("Dn2CppString*");
                Emit($"{st} = {Cast(s, "Dn2CppString*")};");
                string si = NewTemp("int32_t");
                Emit($"{si} = {Cast(start, "int32_t")};");
                string cnt = NewTemp("int32_t");
                Emit($"{cnt} = {Cast(count, "int32_t")};");
                string ok = NewTemp("int32_t");
                Emit($"{ok} = (((uint64_t)(uint32_t){si} + (uint64_t)(uint32_t){cnt}) > (uint64_t)(uint32_t){st}->length) ? 0 : 1;");
                string spanCt = CppTypes.Of(sliceEl);
                Emit($"if ({ok}) {{ (({spanCt}*)({outRef.Expr}))->f__reference = (char16_t*){st}->chars + {si};"
                    + $" (({spanCt}*)({outRef.Expr}))->f__length = {cnt}; }}"
                    + $" else {{ (({spanCt}*)({outRef.Expr}))->f__reference = nullptr;"
                    + $" (({spanCt}*)({outRef.Expr}))->f__length = 0; }}");
                Push(StackKind.I4, "int32_t", ok);
                return true;
            }
            case ("System.String", "Replace")
                when sig.ParameterTypes.Length == 2 && !sig.ParameterTypes[0].IsString:
            {
                var newc = Pop();
                var oldc = Pop();
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_replace_char({Cast(s, "Dn2CppString*")}, (char16_t)({oldc.Expr}), (char16_t)({newc.Expr}))");
                return true;
            }
            // String.Replace(string, string) — ordinal, all occurrences (the runtime helper
            // mirrors .NET's argument checks and returns the original instance when nothing
            // matches).
            case ("System.String", "Replace")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true }]:
            {
                var newv = Pop();
                var oldv = Pop();
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_replace_str({Cast(s, "Dn2CppString*")}, {Cast(oldv, "Dn2CppString*")}, {Cast(newv, "Dn2CppString*")})");
                return true;
            }
            // String.Replace(string, string, StringComparison) — Ordinal delegates
            // to the ordinal scan, OrdinalIgnoreCase to the BMP fold; the helper
            // validates the comparison value first (out-of-range is a catchable
            // ArgumentException even alongside a null oldValue, .NET's order) and
            // folds the culture-sensitive values onto Ordinal/OrdinalIgnoreCase.
            case ("System.String", "Replace")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true },
                    { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var cmp = Pop();
                var newv = Pop();
                var oldv = Pop();
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_replace_str_cmp({Cast(s, "Dn2CppString*")}, {Cast(oldv, "Dn2CppString*")}, "
                    + $"{Cast(newv, "Dn2CppString*")}, {cmp.Expr})");
                return true;
            }
            // String.Replace(string, string, bool ignoreCase, CultureInfo) — the
            // culture operand is dropped (no ICU model): ignoreCase=false is the
            // ordinal scan, true the OrdinalIgnoreCase fold — exact for inputs
            // whose culture case fold coincides with the ordinal one.
            case ("System.String", "Replace")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }, _]:
            {
                Pop(); // culture — ignored
                var ign = Pop();
                var newv = Pop();
                var oldv = Pop();
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_replace_str_cmp({Cast(s, "Dn2CppString*")}, {Cast(oldv, "Dn2CppString*")}, "
                    + $"{Cast(newv, "Dn2CppString*")}, ({ign.Expr}) != 0 ? 5 : 4)");
                return true;
            }
            // String.Normalize/IsNormalized — the invariant-globalization model:
            // the input is treated as already normalized (the runtime helper
            // documents the divergence for not-yet-composed input); the
            // NormalizationForm argument (FormC=1 for the 0-arg forms) is still
            // validated with a catchable ArgumentException.
            case ("System.String", "Normalize") or ("System.String", "IsNormalized")
                when sig.ParameterTypes.Length == 0
                    || sig.ParameterTypes is [{ Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                string form = sig.ParameterTypes.Length == 1 ? Pop().Expr : "1";
                var s = Pop();
                if (name == "Normalize")
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_str_normalize({Cast(s, "Dn2CppString*")}, {form})");
                else
                    Push(StackKind.I4, "int32_t",
                        $"dn2cpp_str_is_normalized({Cast(s, "Dn2CppString*")}, {form})");
                return true;
            }
            // String.CheckStringComparison(comparisonType) — the five-value StringComparison
            // range guard ((uint)value > 5 throws ArgumentException).
            case ("System.String", "CheckStringComparison"):
            {
                var cmp = Pop();
                Emit($"if ((uint32_t)({cmp.Expr}) > 5u) dn2cpp_throw_argument();");
                return true;
            }
            // String.GetCaseCompareOfComparisonCulture(comparisonType) —
            // `(CompareOptions)(comparisonType & StringComparison.CurrentCultureIgnoreCase)`
            // in the real body: the IgnoreCase bit (1) of the comparison value.
            case ("System.String", "GetCaseCompareOfComparisonCulture"):
            {
                var cmp = Pop();
                Push(StackKind.I4, "int32_t", $"(({cmp.Expr}) & 1)");
                return true;
            }
            case ("System.String", "Trim") when sig.ParameterTypes.Length == 0:
            {
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_str_trim({Cast(s, "Dn2CppString*")})");
                return true;
            }
            // TrimStart()/TrimEnd(): one-sided whitespace trim (null set = the
            // full .NET char.IsWhiteSpace set; mode 1 = start, 2 = end).
            case ("System.String", "TrimStart") or ("System.String", "TrimEnd")
                when sig.ParameterTypes.Length == 0:
            {
                var s = Pop();
                string mode0 = name == "TrimStart" ? "1" : "2";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_trim_set({Cast(s, "Dn2CppString*")}, nullptr, 0, {mode0})");
                return true;
            }
            // Trim/TrimStart/TrimEnd(char): the single trim char rides a
            // one-element stack temp into the set-based helper.
            case ("System.String", "Trim") or ("System.String", "TrimStart") or ("System.String", "TrimEnd")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }]:
            {
                var ch = Pop();
                var s = Pop();
                string ct = NewTemp("char16_t");
                Emit($"{ct} = (char16_t)({ch.Expr});");
                string mode1 = name == "TrimStart" ? "1" : name == "TrimEnd" ? "2" : "0";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_trim_set({Cast(s, "Dn2CppString*")}, &{ct}, 1, {mode1})");
                return true;
            }
            // Trim/TrimStart/TrimEnd(params char[]): the array lowers to data
            // pointer + length; a null (or empty) array means whitespace trim,
            // .NET's contract for these overloads.
            case ("System.String", "Trim") or ("System.String", "TrimStart") or ("System.String", "TrimEnd")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray }]:
            {
                var arr = Pop();
                var s = Pop();
                string at = NewTemp("Dn2CppArrayN*");
                Emit($"{at} = (Dn2CppArrayN*)({arr.Expr});");
                string mode2 = name == "TrimStart" ? "1" : name == "TrimEnd" ? "2" : "0";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_trim_set({Cast(s, "Dn2CppString*")}, {at} != nullptr ? (const char16_t*)({at}->data) : nullptr, {at} != nullptr ? {at}->length : 0, {mode2})");
                return true;
            }
            // String.CreateFromChar(c) is the net10 internal behind char.ToString()
            // and Rune.ToString() for a BMP scalar — a one-char string.
            case ("System.String", "CreateFromChar") when sig.ParameterTypes.Length == 1:
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_char_to_string((char16_t)({Pop().Expr}))");
                return true;
            // CreateFromChar(hi, lo) — Rune.ToString() for a supplementary scalar
            // builds the two-char surrogate pair; concat two one-char strings.
            case ("System.String", "CreateFromChar") when sig.ParameterTypes.Length == 2:
            {
                var lo = Pop();
                var hi = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_concat2(dn2cpp_char_to_string((char16_t)({hi.Expr})), dn2cpp_char_to_string((char16_t)({lo.Expr})))");
                return true;
            }
            // PadLeft/PadRight([totalWidth], [paddingChar]); default pad is ' '.
            case ("System.String", "PadLeft") or ("System.String", "PadRight"):
            {
                string pad = sig.ParameterTypes.Length == 2 ? $"(char16_t)({Pop().Expr})" : "u' '";
                var width = Pop();
                var s = Pop();
                string left = name == "PadLeft" ? "1" : "0";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_pad({Cast(s, "Dn2CppString*")}, {width.Expr}, {pad}, {left})");
                return true;
            }
            // Remove(startIndex) drops to the end; Remove(startIndex, count) a range.
            case ("System.String", "Remove"):
            {
                if (sig.ParameterTypes.Length == 2)
                {
                    var count = Pop();
                    var start = Pop();
                    var s = Pop();
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_str_remove({Cast(s, "Dn2CppString*")}, {start.Expr}, {count.Expr})");
                }
                else
                {
                    var start = Pop();
                    var s = Pop();
                    // As Substring(int) above: the `Length - start` the one-argument
                    // form needs is computed inside the helper, behind its null
                    // check, never off the receiver here.
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_str_remove_to_end({Cast(s, "Dn2CppString*")}, {start.Expr})");
                }
                return true;
            }
            // Insert(startIndex, value).
            case ("System.String", "Insert"):
            {
                var value = Pop();
                var start = Pop();
                var s = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_str_insert({Cast(s, "Dn2CppString*")}, {start.Expr}, {Cast(value, "Dn2CppString*")})");
                return true;
            }
            // Format(format, object...) composite formatting: the 1-3 explicit object
            // overloads and the explicit (format, object[]) overload route to runtime
            // helpers (shared with Console.Write/WriteLine).
            case ("System.String", "Format") when IsConsoleFormatOverload(sig):
                Push(StackKind.Ref, "Dn2CppString*", BuildStringFormatExpr(sig));
                return true;
            // Format(IFormatProvider, format, object...) — composite formatting under
            // a culture; the holes format through the *_c formatters.
            case ("System.String", "Format") when IsProviderFormatOverload(sig):
                Push(StackKind.Ref, "Dn2CppString*", BuildStringFormatExprC(sig));
                return true;
            // Format(format, params ReadOnlySpan<object>) (+ provider variant) — the
            // net10 params-span overloads Roslyn prefers for 4+ loose args (or an
            // explicit span argument); same formatting core over the span's
            // data pointer + length.
            case ("System.String", "Format") when IsSpanFormatOverload(sig):
                Push(StackKind.Ref, "Dn2CppString*", BuildStringFormatExprSpan(sig));
                return true;
            case ("System.String", "Format") when IsProviderSpanFormatOverload(sig):
                Push(StackKind.Ref, "Dn2CppString*", BuildStringFormatExprSpanC(sig));
                return true;
            // Format(IFormatProvider, CompositeFormat, object[] | ReadOnlySpan<object>) —
            // the net8+ pre-parsed-format family's non-generic half. The generic
            // Format<TArg0..TArg2> siblings arrive as MethodSpecifications and are lowered
            // by the same builder from TranslateGenericIntrinsic; both mouths ask
            // IsCompositeFormatOverload, so the family has one shape test and one lowering.
            case ("System.String", "Format") when IsCompositeFormatOverload(sig):
                Push(StackKind.Ref, "Dn2CppString*", BuildCompositeFormatExprC(sig));
                return true;
            // ---- Split -> string[], the full overload family. The runtime engine
            // handles the separator modes (char set / string array / whitespace),
            // the kept-entry count budget with its remainder rule, and the
            // count/options validation; here each overload just routes its
            // separator shape. An absent count means no cap (int.MaxValue). ----
            // String.CheckStringSplitOptions (internal): the shared argument guard the BCL's
            // span Split extensions call. Valid bits are RemoveEmptyEntries (1) |
            // TrimEntries (2); anything else is ArgumentException, matching the real body.
            case ("System.String", "CheckStringSplitOptions") when sig.ParameterTypes.Length == 1:
            {
                var opt = Pop();
                Emit($"if (((uint32_t)({opt.Expr}) & ~3u) != 0) dn2cpp_throw_argument();");
                return true;
            }
            // String.MakeSeparatorList / MakeSeparatorListAny (internal statics): the
            // separator-position scanners behind MemoryExtensions.Split's SplitCore.
            // Delegate to the real transpiled CoreLib bodies (span IL over
            // ValueListBuilder<int>); MakeSeparatorListVectorized is their intra-String SIMD
            // fast path (Vector128 IL, which transpiles) — same delegation so the outer
            // bodies' calls to it resolve.
            case ("System.String",
                "MakeSeparatorList" or "MakeSeparatorListAny" or "MakeSeparatorListVectorized"):
            {
                if (Comp.ReachStringStaticMethod(name,
                        ps => ps.Length == sig.ParameterTypes.Length) is not { } msl)
                    return false;
                var mps = msl.Signature.ParameterTypes;
                var margs = new string[mps.Length];
                for (int i = mps.Length - 1; i >= 0; i--)
                    margs[i] = Cast(Pop(), CppTypes.Of(mps[i]));
                Emit($"{DirectCall(msl, margs)};");
                return true;
            }
            // Split(char [, StringSplitOptions]): `s.Split(',')` binds to the
            // (char, StringSplitOptions = None) overload, so options is present.
            case ("System.String", "Split")
                when sig.ParameterTypes.Length is 1 or 2
                    && sig.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }
                    && (sig.ParameterTypes.Length == 1
                        || sig.ParameterTypes[1] is { Kind: TypeKind.Class, Class.IsEnum: true }):
            {
                string opts = sig.ParameterTypes.Length == 2 ? Pop().Expr : "0";
                var sep = Pop();
                var s = Pop();
                PushStringSplitResult(
                    $"dn2cpp_str_split_char({Cast(s, "Dn2CppString*")}, (char16_t)({sep.Expr}), 2147483647, {opts})");
                return true;
            }
            // Split(char, int count [, StringSplitOptions]).
            case ("System.String", "Split")
                when sig.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }, ..]:
            {
                string opts = sig.ParameterTypes.Length == 3 ? Pop().Expr : "0";
                var count = Pop();
                var sep = Pop();
                var s = Pop();
                PushStringSplitResult(
                    $"dn2cpp_str_split_char({Cast(s, "Dn2CppString*")}, (char16_t)({sep.Expr}), {Cast(count, "int32_t")}, {opts})");
                return true;
            }
            // Split(params char[]), Split(char[], int), Split(char[],
            // StringSplitOptions), Split(char[], int, StringSplitOptions). The
            // array lowers to data pointer + length (packed Dn2CppArrayN,
            // elemSize 2); a null or empty array means whitespace split.
            case ("System.String", "Split")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.Primitive: PrimitiveTypeCode.Char }, ..]
                    && sig.ParameterTypes.Length <= 3:
            {
                var (count2, opts2) = PopSplitCountOptions(sig);
                var arr = Pop();
                var s = Pop();
                string at = NewTemp("Dn2CppArrayN*");
                Emit($"{at} = (Dn2CppArrayN*)({arr.Expr});");
                PushStringSplitResult(
                    $"dn2cpp_str_split({Cast(s, "Dn2CppString*")}, {at} != nullptr ? (const char16_t*)({at}->data) : nullptr, {at} != nullptr ? {at}->length : 0, nullptr, {count2}, {opts2})");
                return true;
            }
            // Split(string, StringSplitOptions), Split(string, int [,
            // StringSplitOptions]). A null/empty separator string yields the
            // whole string as the sole entry (string mode with no occurrences),
            // NOT a whitespace split — that is the separator-array contract.
            case ("System.String", "Split")
                when sig.ParameterTypes is [{ IsString: true }, ..] && sig.ParameterTypes.Length <= 3:
            {
                var (count3, opts3) = PopSplitCountOptions(sig);
                var sep = Pop();
                var s = Pop();
                PushStringSplitResult(
                    $"dn2cpp_str_split_str({Cast(s, "Dn2CppString*")}, {Cast(sep, "Dn2CppString*")}, {count3}, {opts3})");
                return true;
            }
            // Split(string[], StringSplitOptions), Split(string[], int,
            // StringSplitOptions). The array passes through whole; a null or
            // empty array means whitespace split, null/empty entries are
            // skipped by the engine's scan.
            case ("System.String", "Split")
                when sig.ParameterTypes is [{ Kind: TypeKind.SZArray, Element.IsString: true }, ..]
                    && sig.ParameterTypes.Length <= 3:
            {
                var (count4, opts4) = PopSplitCountOptions(sig);
                var arr = Pop();
                var s = Pop();
                PushStringSplitResult(
                    $"dn2cpp_str_split({Cast(s, "Dn2CppString*")}, nullptr, 0, (Dn2CppArrayRef*)({arr.Expr}), {count4}, {opts4})");
                return true;
            }
            case ("System.String", "IsNullOrEmpty"):
            {
                string sc = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.I4, "int32_t", $"(({sc}) == nullptr || ({sc})->length == 0 ? 1 : 0)");
                return true;
            }
            case ("System.String", "IsNullOrWhiteSpace"):
            {
                Push(StackKind.I4, "int32_t", $"dn2cpp_str_is_null_or_whitespace({Cast(Pop(), "Dn2CppString*")})");
                return true;
            }
            // ---- ordinal Compare/CompareOrdinal/Equals (+ ASCII OrdinalIgnoreCase) ----
            // CompareOrdinal(a, b): pure ordinal code-unit comparison.
            case ("System.String", "CompareOrdinal") when sig.ParameterTypes is [{ IsString: true }, { IsString: true }]:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}, 4)");
                return true;
            }
            // CompareOrdinal(ReadOnlySpan<char>, ReadOnlySpan<char>) — the span
            // form MemoryExtensions.CompareTo's Ordinal arm calls.
            case ("System.String", "CompareOrdinal")
                    when sig.ParameterTypes is [{ } sa, { } sb] && IsCharSpan(sa) && IsCharSpan(sb):
            {
                string spanCt = CppTypes.Of(sig.ParameterTypes[0]);
                string sb2 = SpanPtr(Pop(), spanCt);
                string sa2 = SpanPtr(Pop(), spanCt);
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_chars_compare_ordinal((const char16_t*){sa2}->f__reference, {sa2}->f__length, "
                    + $"(const char16_t*){sb2}->f__reference, {sb2}->f__length)");
                return true;
            }
            // CompareOrdinal(strA, indexA, strB, indexB, length) — the substring form. It is
            // Compare(..., StringComparison.Ordinal) under a different name, so it routes to
            // that sibling's helper rather than repeating the clamp below; the two must
            // never disagree.
            //
            // dn2cpp_str_compare_sub clamps `length` against EACH side separately —
            // min(length, strA.Length - indexA) and min(length, strB.Length - indexB) — and
            // compares the two clamped lengths as lengths. That is what makes
            // CompareOrdinal("ab", 0, "abcd", 0, 4) == -2: A clamps to 2, B stays 4, and the
            // -2 is the LENGTH difference, not a code-unit one; clamping once against a
            // single minimum would answer 0. The result is a raw difference throughout
            // (real .NET returns 'e' - 'y' == -20, never a -1/0/1 sign).
            case ("System.String", "CompareOrdinal")
                when sig.ParameterTypes is [{ IsString: true }, _, { IsString: true }, _, _]:
            {
                var len = Pop();
                var ib = Pop();
                var b = Pop();
                var ia = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare_sub({Cast(a, "Dn2CppString*")}, {ia.Expr}, "
                    + $"{Cast(b, "Dn2CppString*")}, {ib.Expr}, {len.Expr}, 4)");
                return true;
            }
            // Compare(a, b, StringComparison): Ordinal/OrdinalIgnoreCase honored at
            // runtime; culture-sensitive values fold onto them in the helper
            // (dn2cpp_str_comparison_fold — the invariant-globalization posture).
            case ("System.String", "Compare")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true }, { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var ct = Pop();
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}, {ct.Expr})");
                return true;
            }
            // The (strA, indexA, strB, indexB, length, StringComparison) substring form —
            // same ordinal-only posture as the 3-arg form.
            case ("System.String", "Compare")
                when sig.ParameterTypes is [{ IsString: true }, _, { IsString: true }, _, _, { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var ct = Pop();
                var len = Pop();
                var ib = Pop();
                var b = Pop();
                var ia = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare_sub({Cast(a, "Dn2CppString*")}, {ia.Expr}, {Cast(b, "Dn2CppString*")}, {ib.Expr}, {len.Expr}, {ct.Expr})");
                return true;
            }
            // Compare(a, b, CultureInfo, CompareOptions) — the culture operand is dropped
            // (no ICU model): the IgnoreCase (0x1) and OrdinalIgnoreCase (0x10000000) bits
            // select the OrdinalIgnoreCase fold (5), anything else compares Ordinal (4).
            // The options value is a runtime operand, so an emitted test picks the fold.
            case ("System.String", "Compare")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true },
                    { Kind: TypeKind.Class, Class.IsEnum: false }, { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var opts = Pop();
                Pop(); // culture — ignored
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}, "
                    + $"(({opts.Expr}) & 0x10000001) != 0 ? 5 : 4)");
                return true;
            }
            // Compare(strA, indexA, strB, indexB, length) — no StringComparison, so
            // real .NET defaults to the current culture; here it is the ordinal
            // approximation (comparisonType 4), sharing the substring helper with
            // the 6-arg form above (same per-side clamp and null/range order).
            case ("System.String", "Compare")
                when sig.ParameterTypes is [{ IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }, { IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var len = Pop();
                var ib = Pop();
                var b = Pop();
                var ia = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare_sub({Cast(a, "Dn2CppString*")}, {ia.Expr}, {Cast(b, "Dn2CppString*")}, {ib.Expr}, {len.Expr}, 4)");
                return true;
            }
            // Compare(a, b, bool ignoreCase, CultureInfo) — the culture operand is
            // dropped: ignoreCase=false compares Ordinal, true the
            // OrdinalIgnoreCase fold, mirroring the Replace sibling.
            case ("System.String", "Compare")
                when sig.ParameterTypes is [{ IsString: true }, { IsString: true },
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }, _]:
            {
                Pop(); // culture — ignored
                var ign = Pop();
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_str_compare({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}, "
                    + $"({ign.Expr}) != 0 ? 5 : 4)");
                return true;
            }
            // Equals — ordinal forms: instance Equals(value) and static Equals(a, b).
            case ("System.String", "Equals") when sig.ParameterTypes is [{ IsString: true }]:
            case ("System.String", "Equals") when sig.ParameterTypes is [{ IsString: true }, { IsString: true }]:
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_string_equals({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")})");
                return true;
            }
            // Equals with StringComparison — instance Equals(value, cmp) and static
            // Equals(a, b, cmp); equality is compare == 0 (ordinal / ASCII ignore-case).
            case ("System.String", "Equals") when sig.ParameterTypes is [{ IsString: true }, { Kind: TypeKind.Class, Class.IsEnum: true }]:
            case ("System.String", "Equals") when sig.ParameterTypes is [{ IsString: true }, { IsString: true }, { Kind: TypeKind.Class, Class.IsEnum: true }]:
            {
                var ct = Pop();
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"(dn2cpp_str_compare({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}, {ct.Expr}) == 0)");
                return true;
            }

            default:
                return false;
        }
    }

    /// <summary>Pushes a String.Split runtime-call result. Split returns string[]; the
    /// result is stamped with the precise ti_arr_String handle (its SZArray interface map)
    /// so a foreach / LINQ over it dispatches IEnumerable&lt;string&gt; on the array itself —
    /// the shared object[] handle the runtime stamps carries no interface map. The static
    /// type is set to string[] so downstream sees the precise element type.</summary>
    private void PushStringSplitResult(string callExpr)
    {
        var splitElem = TypeDesc.MakePrimitive(PrimitiveTypeCode.String);
        Comp.NoteArrayEnumerableElement(splitElem);
        string splitTmp = NewTemp("Dn2CppArrayRef*");
        Emit($"{splitTmp} = {callExpr};");
        Emit($"if ({splitTmp} != nullptr) ((Dn2CppObject*){splitTmp})->type = {PreciseArrayTypeInfoExpr(splitElem)};");
        PushEntry(new StackEntry(splitTmp, StackKind.Ref, "Dn2CppArrayRef*",
            StaticType: TypeDesc.MakeSZArray(splitElem)));
    }

    /// <summary>Pops the trailing count/options args of a separator-first
    /// String.Split overload (separator[, count][, options]) and returns their
    /// C++ expressions; absent args default to no cap (int.MaxValue) and
    /// StringSplitOptions.None.</summary>
    private (string Count, string Opts) PopSplitCountOptions(MethodSignature<TypeDesc> sig)
    {
        string count = "2147483647", opts = "0";
        if (sig.ParameterTypes.Length == 3)
        {
            opts = Pop().Expr;
            count = Cast(Pop(), "int32_t");
        }
        else if (sig.ParameterTypes.Length == 2)
        {
            if (sig.ParameterTypes[1] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 })
                count = Cast(Pop(), "int32_t");
            else
                opts = Pop().Expr;
        }
        return (count, opts);
    }

    /// <summary>Consolidated numeric Parse/TryParse lowering onto the runtime NumberStyles
    /// engine: the eight integer widths (bit-width/signedness parameterized) plus
    /// Double/Single, in the plain / (NumberStyles) / (IFormatProvider) / (NumberStyles,
    /// IFormatProvider) shapes and their TryParse out-forms. A null/absent provider parses
    /// invariant; the defaulted styles are .NET's (Integer for the integers,
    /// Float|AllowThousands for the floats). Unrecognized shapes return false and stay a
    /// loud failure.
    ///
    /// <para>The input may be a string, a ReadOnlySpan&lt;char&gt; (ISpanParsable) or a
    /// ReadOnlySpan&lt;byte&gt; of UTF-8 (IUtf8SpanParsable) — .NET 10 declares all three for
    /// every one of these types, sub-word included, and a call site binds whichever it was
    /// handed. They differ only in how the runtime reaches the characters; ONE engine
    /// decides what a number is.</para></summary>
    private bool TryEmitParseIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        if (name is not ("Parse" or "TryParse"))
            return false;
        (int Bits, int Signed)? intShape = declType switch
        {
            "System.Byte" => (8, 0),
            "System.SByte" => (8, 1),
            "System.Int16" => (16, 1),
            "System.UInt16" => (16, 0),
            "System.Int32" => (32, 1),
            "System.UInt32" => (32, 0),
            "System.Int64" => (64, 1),
            "System.UInt64" => (64, 0),
            _ => null,
        };
        bool isFloat = declType is "System.Double" or "System.Single";
        if (intShape is null && !isFloat)
            return false;

        // Shape: input [, NumberStyles] [, IFormatProvider] [, out T for TryParse].
        var ps = sig.ParameterTypes;
        bool isTry = name == "TryParse";
        if (isTry && (ps.Length < 2 || ps[^1].Kind != TypeKind.ByRef))
            return false;
        int valueArgs = isTry ? ps.Length - 1 : ps.Length;
        if (valueArgs is < 1 or > 3)
            return false;
        bool isString = ps[0].IsString;
        // The UTF-8 input form. Classified by the span's ELEMENT TYPE, never by arity: the
        // char and byte spans are shape-identical otherwise, and taking the char16 lane for
        // a byte buffer would read two bytes per digit off the end of it.
        bool isUtf8 = !isString && IsReadOnlyByteSpan(ps[0]);
        if (!isString && !isUtf8 && !IsReadOnlyCharSpan(ps[0]))
            return false;
        bool hasStyles = false, hasProvider = false;
        for (int i = 1; i < valueArgs; i++)
        {
            if (i == 1 && IsNumberStyles(ps[i]))
                hasStyles = true;
            else if (i == valueArgs - 1 && IsFormatProvider(ps[i]))
                hasProvider = true;
            else
                return false;
        }

        // The entry, not just its text: the sub-word store below needs the ref's
        // PROVENANCE to decide whether an int32-promoted slot has to be re-normalized.
        StackEntry? outEntry = isTry ? Pop() : null;
        string? outRef = outEntry?.Expr;
        string nfiExpr = hasProvider ? Cast(Pop(), "const Dn2CppNumberFormatInfo*") : "nullptr";
        string stylesExpr = hasStyles
            ? $"(int32_t)({Pop().Expr})"
            : isFloat ? "231" /* Float | AllowThousands */ : "7" /* Integer */;
        string args, suffix;
        if (isString)
        {
            args = $"{Cast(Pop(), "Dn2CppString*")}, {stylesExpr}, {nfiExpr}";
            suffix = "str";
        }
        else
        {
            string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
            // The _utf8 helpers widen the bytes and hand them to the SAME engine the char16
            // ones use, so the two lanes cannot disagree about what a number is.
            args = $"(const {(isUtf8 ? "char" : "char16_t")}*){sp}.f__reference, {sp}.f__length, "
                + $"{stylesExpr}, {nfiExpr}";
            suffix = isUtf8 ? "utf8" : "chars";
        }

        if (intShape is { } iw)
        {
            var prim = Compilation.WellKnownPrimitive(declType)!;
            string storeCt = CppTypes.StorageOf(prim);
            var kind = CppTypes.KindOf(prim);
            string stackCt = CppTypes.DefaultForKind(kind);
            if (isTry)
            {
                // The engine reports through an int64 slot; narrow-store to the
                // real out width (0 on failure, matching .NET's default-on-failure).
                string pv = NewTemp("int64_t");
                string ok = NewTemp("int32_t");
                Emit($"{ok} = dn2cpp_integer_tryparse_{suffix}({args}, {iw.Bits}, {iw.Signed}, &{pv});");
                Emit($"*(({storeCt}*)({outRef})) = ({storeCt}){pv};");
                // The narrow store leaves an int32-promoted slot's upper bytes stale, so a
                // negative sbyte/short read with `ldloc` came out zero-extended (a concat
                // reads the slot's ADDRESS instead, which is why nothing saw it). The
                // float arm below needs no fixup: there the store width IS the stack one.
                NoteByRefSlotFixup(outEntry!, ps[^1]);
                EmitByRefSlotFixups();
                Push(StackKind.I4, "int32_t", ok);
            }
            else
            {
                Push(kind, stackCt,
                    $"(({stackCt})({storeCt})dn2cpp_integer_parse_{suffix}({args}, {iw.Bits}, {iw.Signed}))");
            }
            return true;
        }

        int single = declType == "System.Single" ? 1 : 0;
        if (isTry)
        {
            string pv = NewTemp("double");
            string ok = NewTemp("int32_t");
            Emit($"{ok} = dn2cpp_fp_tryparse_{suffix}({args}, {single}, &{pv});");
            Emit(single == 1
                ? $"*((float*)({outRef})) = (float){pv};"
                : $"*((double*)({outRef})) = {pv};");
            Push(StackKind.I4, "int32_t", ok);
        }
        else
        {
            // float on the CLR stack is the F type — already double-widened.
            Push(StackKind.R8, "double", $"dn2cpp_fp_parse_{suffix}({args}, {single})");
        }
        return true;
    }

    /// <summary>ReadOnlySpan&lt;char&gt; — the ISpanParsable input form.</summary>
    private bool IsReadOnlyCharSpan(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } c }
        && Comp.GenericDefFullName(c) == "System.ReadOnlySpan"
        && c.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }];

    /// <summary>ReadOnlySpan&lt;byte&gt; — the IUtf8SpanParsable input form, UTF-8 text.
    /// Distinguished from the char span by its ELEMENT type: the two are otherwise the same
    /// <c>{ pointer, length }</c> shape at the same arity, and .NET 10 declares both for
    /// every integer and float width.</summary>
    private bool IsReadOnlyByteSpan(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } c }
        && Comp.GenericDefFullName(c) == "System.ReadOnlySpan"
        && c.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Byte }];

    /// <summary>System.Globalization.NumberStyles (kept as a Class TypeDesc by
    /// signature decoding, its enum-ness intact).</summary>
    private static bool IsNumberStyles(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { IsEnum: true, FullName: "System.Globalization.NumberStyles" } };
}
