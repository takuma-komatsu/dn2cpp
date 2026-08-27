using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>Lower a call to a P/Invoke method to a direct call to its registered
    /// managed implementation (<c>[Dn2Cpp.Runtime.NativeImplementation]</c>) instead of
    /// a native call. The two signatures need not name the same managed types — the
    /// import typically uses BCL-internal pointer/enum types the implementation cannot
    /// reference — but each position must lower to the same blittable native shape
    /// (<see cref="CppTypes.NativeAbiType"/>, with the pointer-width spellings
    /// <c>void*</c>/<c>intptr_t</c>/<c>uintptr_t</c> treated as one class), which also
    /// excludes every marshalled shape (string/array/bool/char/struct): a substituted
    /// import must be pure blittable, so no call-site marshalling is woven. The C++
    /// spelling differences that remain (all pointers are <c>void*</c>, enums are their
    /// underlying integer) are bridged by the ordinary argument coercion in
    /// <see cref="PopArgs"/> plus one cast on a differing return. The native entry
    /// point is never referenced (no <see cref="Compilation.PInvokeCalls"/> record, no
    /// extern declaration, no link flag), so the native library drops out of the
    /// binary entirely when every import it serves is substituted.</summary>
    private void EmitNativeImplCall(MethodInfo callee, MethodInfo impl)
    {
        string Where() =>
            $"{_method.DeclaringClass.FullName}.{_method.Name}: P/Invoke "
            + $"{callee.DeclaringClass.FullName}.{callee.Name} "
            + $"[NativeImplementation {impl.DeclaringClass.FullName}.{impl.Name}]";
        // Collapse the pointer-width spellings: the ABI passes them identically, and
        // an import's IntPtr routinely maps to an implementation's typed pointer.
        static string? AbiClass(string? abi) =>
            abi is "intptr_t" or "uintptr_t" ? "void*" : abi;

        var cps = callee.Signature.ParameterTypes;
        var ips = impl.Signature.ParameterTypes;
        if (cps.Length != ips.Length)
            throw new NotSupportedException(
                $"{Where()}: parameter count mismatch ({cps.Length} vs {ips.Length})");
        for (int i = 0; i < cps.Length; i++)
        {
            string? ca = CppTypes.NativeAbiType(cps[i]);
            string? ia = CppTypes.NativeAbiType(ips[i]);
            if (ca is null || ia is null || AbiClass(ca) != AbiClass(ia))
                throw new NotSupportedException(
                    $"{Where()}: parameter {i} native shape mismatch — import {cps[i]} "
                    + $"({ca ?? "not blittable"}) vs implementation {ips[i]} ({ia ?? "not blittable"})");
        }
        string? cra = CppTypes.NativeAbiType(callee.Signature.ReturnType, isReturn: true);
        string? ira = CppTypes.NativeAbiType(impl.Signature.ReturnType, isReturn: true);
        if (cra is null || ira is null || AbiClass(cra) != AbiClass(ira))
            throw new NotSupportedException(
                $"{Where()}: return native shape mismatch — import {callee.Signature.ReturnType} "
                + $"({cra ?? "not blittable"}) vs implementation {impl.Signature.ReturnType} ({ira ?? "not blittable"})");

        var args = PopArgs(impl, hasThis: false);
        string call = DirectCall(impl, args);
        // EmitCallResult pushes under the callee's declared return type (downstream IL
        // compares against the import's enum/IntPtr), so bridge a differing return
        // spelling here.
        string calleeRet = CppTypes.Of(callee.Signature.ReturnType);
        if (!callee.Signature.ReturnType.IsVoid && calleeRet != CppTypes.Of(impl.Signature.ReturnType))
            call = $"(({calleeRet})({call}))";
        EmitCallResult(callee, call);
    }

    /// <summary>Lower a call to a P/Invoke (`[DllImport]`) method to a direct native
    /// call into its <c>extern "C"</c> entry point. The return and every
    /// parameter must be marshallable: blittable (integer/float primitive, IntPtr/
    /// UIntPtr, unmanaged pointer, blittable-underlying enum) — passed by a precise
    /// native-ABI-width cast — or <c>string</c>, marshalled as a NUL-terminated buffer
    /// (managed→native on the way in, native→managed-string on return, the return pointer
    /// freed to match.NET's default marshaller) — UTF-8 under the default/Ansi CharSet
    /// or UTF-16/LPWStr under CharSet.Unicode. The call site registers
    /// the method so <see cref="CppEmitter"/> declares the entry-point symbol once.
    ///
    /// The full supported surface — arrays, structs, bool/char widths, StringBuilder
    /// and byref-string write-backs, [MarshalAs] overrides, direction semantics — is
    /// tabulated in docs/PINVOKE-MARSHALLING.md. Everything outside it (object
    /// marshalling, non-blittable structs, array returns, the COM-era [MarshalAs]
    /// kinds) is a precise NotSupportedException, never a silent default.</summary>
    private void EmitPInvokeCall(MethodInfo callee, PInvokeInfo pinv)
    {
        string Where(string what) =>
            $"{_method.DeclaringClass.FullName}.{_method.Name}: P/Invoke {callee.DeclaringClass.FullName}.{callee.Name} {what}";

        // Calling convention: on the Unix target ABI (x86-64 System V /
        // AArch64 AAPCS) there is exactly one C calling convention, and.NET on Unix
        // collapses Cdecl/StdCall/ThisCall/FastCall/Winapi to that single platform
        // default — the x86 register/cleanup distinctions only exist on 32-bit Windows.
        // So every CallingConvention is accepted and lowered identically to the cdecl
        // extern "C" call below; the one carve-out is Windows-specific stdcall name
        // decoration / the 32-bit stdcall ABI, which has no target here.

        var ret = callee.Signature.ReturnType;
        var pts = callee.Signature.ParameterTypes;

        // The CharSet flag drives both string marshalling (default/Ansi/Auto = UTF-8 on
        // Unix; CharSet.Unicode = UTF-16/LPWStr) and char marshalling
        // (1-byte UTF-8-first-byte vs 2-byte UTF-16 code unit).
        var charSet = pinv.ImportAttributes & System.Reflection.MethodImportAttributes.CharSetMask;
        bool charUnicode = charSet == System.Reflection.MethodImportAttributes.CharSetUnicode;

        // Per-parameter / return [MarshalAs(UnmanagedType.*)] overrides: a
        // string encoding (LPStr/LPUTF8Str/LPWStr), a bool width (I1 = 1-byte, Bool =
        // the 4-byte default), or LPArray on a blittable array. PInvokeNativeType returns
        // null for an unsupported (type, UnmanagedType) pair — including any [MarshalAs] on
        // a fixed-size integer/enum, which.NET itself rejects — reported below as a
        // precise carve-out.
        var retMa = pinv.ReturnMarshalAs;
        System.Runtime.InteropServices.UnmanagedType? Ma(int i) =>
            pinv.ParamMarshalAs.TryGetValue(i, out var v)
                ? v
                : (System.Runtime.InteropServices.UnmanagedType?)null;

        // Validate return + every parameter is marshallable (blittable scalar, string, a
        // blittable array, a blittable value struct, a bool, a char, or a supported
        // [MarshalAs] override). PInvokeNativeType doubles as the carve-out gate (null for a
        // non-blittable array/struct / object / unsupported [MarshalAs]).
        if (CppTypes.PInvokeNativeType(ret, isReturn: true, charUnicode, retMa) is null)
            throw new NotSupportedException(Where(retMa is { } rm
                ? $"return [MarshalAs(UnmanagedType.{rm})] on {ret} is not supported (deliberate carve-out: COM/BStr/ByValArray/SafeArray marshalling, or a descriptor invalid on the type — FunctionPtr fits only a function-pointer (delegate*) type)"
                : $"return type {ret} is not marshallable — object (and array-return / non-blittable-struct) marshalling is not supported yet"));
        for (int i = 0; i < pts.Length; i++)
            if (CppTypes.PInvokeNativeType(pts[i], charUnicode: charUnicode, marshalAs: Ma(i)) is null)
                throw new NotSupportedException(Where(Ma(i) is { } pm
                    ? $"parameter {i} [MarshalAs(UnmanagedType.{pm})] on {pts[i]} is not supported (deliberate carve-out: COM/BStr/ByValArray/SafeArray marshalling, or a descriptor invalid on the type — FunctionPtr fits only a function-pointer (delegate*) type)"
                    : pts[i].Kind == TypeKind.Class && pts[i].Class!.IsDelegate
                        ? $"parameter {i} delegate {pts[i]} cannot marshal as a native callback function pointer: {CppTypes.CallbackDelegateRejectReason(pts[i])}"
                        // A struct whose only obstacle is a field's [MarshalAs] descriptor
                        // (ByValTStr, a bool width override, a width-MISMATCHED descriptor
                        // on a blittable field, a ByValArray of non-blittable elements)
                        // gets the field-naming sentence. ByRef and array params are
                        // unwrapped so the element struct's field is named.
                        : CppTypes.StructFieldDescriptorReject(
                              pts[i].Kind is TypeKind.ByRef or TypeKind.SZArray
                                  ? pts[i].Element! : pts[i]) is { } fr
                        ? $"parameter {i} struct {pts[i]} cannot cross the P/Invoke boundary: {fr}"
                        : $"parameter {i} of type {pts[i]} is not marshallable — non-blittable array/struct or object marshalling is not supported yet"));

        // Record for the emitter's one-per-symbol extern "C" declaration.
        _c.PInvokeCalls.Add(callee);

        // P/Invoke methods are always static (no `this`). Pop args right-to-left and
        // marshal each: a string -> a GC-allocated NUL-terminated UTF-8 buffer cast to
        // void*; a blittable array -> a pointer to element 0 (null array -> null); a
        // blittable value -> a precise native-ABI-width cast.
        // A StringBuilder is a caller-allocated [In,Out] buffer the native fills; record
        // (builder, buffer, unicode) here so the content is read back after the call.
        // A byref/[Out] string records (slot, temp, in-buffer, unicode) so
        // the native's pointer is decoded back into the ByRef slot. Other
        // shapes don't post-process their args.
        var sbWritebacks = new List<(string sb, string buf, bool unicode)>();
        var byrefStrWritebacks = new List<(string slot, string tmp, string inbuf, int enc)>();
        // An Ansi char[] marshalled to an [In,Out]/[Out] native buffer records (array,
        // buffer) so the buffer is decoded back into the array after the call.
        var charArrWritebacks = new List<(string arr, string buf)>();
        // A blittable struct array marshalled [In,Out]/[Out] records (array, buffer) so
        // the native buffer is copied back into the array after the call.
        var structArrWritebacks = new List<(string arr, string buf)>();
        // A string[] marshalled [In,Out]/[Out] records (array, pointer-buffer, pre-call
        // pointer snapshot, unicode) so each slot's (possibly native-replaced) pointer is
        // decoded back into a managed string after the call — and only a replaced slot
        // (its pointer differs from the snapshot) is freed, never our GC-allocated buffers.
        var strArrWritebacks = new List<(string arr, string buf, string inbuf, bool unicode)>();
        // A by-ref non-blittable struct marshalled [In,Out]/[Out] records (struct C++ name,
        // native local, managed pointer, IN snapshot) so the native struct is marshalled
        // back into the managed location after the call — string fields decoded with the
        // free-only-if-replaced discipline (via the snapshot), blittable fields copied.
        var structMarshalWritebacks = new List<(string name, string nloc, string mptr, string insnap)>();
        var args = new string[pts.Length];
        for (int i = pts.Length - 1; i >= 0; i--)
        {
            var e = Pop();
            var ma = Ma(i);
            if (pts[i].Kind == TypeKind.Class && pts[i].Class!.IsDelegate
                && CppTypes.IsBlittableCallbackDelegate(pts[i]))
            {
                // A managed delegate -> a native C function pointer, routed through the
                // persistent, GC-rooted per-delegate-type thunk pool — the SAME machinery
                // Marshal.GetFunctionPointerForDelegate<T> uses (CppEmitter.EmitMarshalFnPtr-
                // Thunks), never a synchronous thread-local slot.
                //
                // LIFETIME CONTRACT: the pool parks the delegate in a file-scope static
                // array (a Boehm root) for the program's lifetime and hands back a stable,
                // identity-deduplicated C function pointer. So the pointer stays valid after
                // THIS call returns — the native side may STORE it and invoke it on a later
                // frame (the collision-filter / error-callback idiom), which the transpiler
                // cannot statically rule out and which a slot restored on return would turn
                // into silent UB. The pool holds 8 distinct delegates per delegate TYPE
                // over the program lifetime (a reused delegate maps to one slot); a 9th
                // distinct delegate traps loudly (dn2cpp_fail) rather than corrupting
                // dispatch. Every delegate parameter marshals this way: the
                // qsort/apply/reduce synchronous idiom works too (the parked pointer is valid
                // during the call as well), at the cost of that bounded, loud capacity.
                var dgCls = pts[i].Class!;
                _c.MarshalFnPtrDelegates.Add(dgCls);
                _c.NoteForceEmit(dgCls);
                // A null delegate marshals as native NULL — the standard "unregister the
                // callback" idiom — NOT the pool helper's ArgumentNullException, which is
                // correct only for the explicit Marshal.GetFunctionPointerForDelegate<T>
                // intrinsic. Spill to a temp so the operand is evaluated once and guard the
                // pool call on it.
                string dgTmp = NewTemp("Dn2CppObject*");
                Emit($"{dgTmp} = {Cast(e, "Dn2CppObject*")};");
                args[i] = $"({dgTmp} == nullptr ? (void*)nullptr "
                    + $": dn2cpp_fnptr_for_delegate_{dgCls.CppName}({dgTmp}))";
            }
            else if (pts[i].IsString)
            {
                // A string -> a NUL-terminated native buffer. The encoding is per-parameter:
                // [MarshalAs] forces it (LPWStr = UTF-16, LPStr = Ansi, LPUTF8Str = UTF-8),
                // else the method CharSet (CharSet.Unicode = UTF-16,
                // otherwise the default Ansi — the host code page). Ansi is the
                // real-.NET default: the system code page on Windows, UTF-8 on Unix.
                args[i] = CppTypes.StringMarshalEncoding(ma, charUnicode) switch
                {
                    CppTypes.PInvokeStrEnc.Unicode =>
                        $"((void*)dn2cpp_pinvoke_str_to_utf16({Cast(e, "Dn2CppString*")}))",
                    CppTypes.PInvokeStrEnc.Utf8 =>
                        $"((void*)dn2cpp_pinvoke_str_to_utf8({Cast(e, "Dn2CppString*")}))",
                    _ =>
                        $"((void*)dn2cpp_pinvoke_str_to_ansi({Cast(e, "Dn2CppString*")}))",
                };
            }
            else if (pts[i].Kind == TypeKind.SZArray && pts[i].Element!.IsString)
            {
                // A string[] -> char** / char16_t**: a pointer array, each slot a
                // NUL-terminated UTF-8 (default/Ansi) or UTF-16 (CharSet.Unicode) buffer (a
                // null element -> a null pointer, a null array -> a null pointer). The default
                // direction is [In] (encode each element in, no write-back). For [In,Out]/[Out]
                // the slots are decoded back after the call: spill the array and the
                // pointer buffer to temps, encode the input in unless the parameter is
                // [Out]-only, snapshot the pre-call pointers, pass the buffer, and decode each
                // slot's (possibly native-replaced) pointer into a fresh managed string written
                // back into the array. Only a slot the native REPLACED (its pointer differs from
                // the snapshot) is freed — a native heap pointer, matching .NET's
                // CoTaskMemFree-on-Unix ownership; an untouched slot still holds our GC buffer
                // and is never free()d. The native must heap-allocate (malloc/CoTaskMem) any
                // pointer it writes — exactly as a .NET-correct native does.
                (bool hasIn, bool hasOut) = PInvokeParamInOut(callee, i);
                bool copyIn = !(hasOut && !hasIn); // copy in unless [Out]-only
                string arr = NewTemp("Dn2CppArrayRef*");
                Emit($"{arr} = {Cast(e, "Dn2CppArrayRef*")};");
                string buf = NewTemp("void**");
                Emit($"{buf} = {(charUnicode
                    ? "dn2cpp_pinvoke_strarr_to_utf16"
                    : "dn2cpp_pinvoke_strarr_to_ansi")}({arr}, {(copyIn ? 1 : 0)});");
                args[i] = $"(void**){buf}";
                if (hasOut)
                {
                    // Snapshot the IN pointers before the native call (null array -> 0 length,
                    // and buf is null too, so the snapshot is null) so the write-back can tell a
                    // native-replaced slot from one still holding our GC-allocated buffer.
                    string inbuf = NewTemp("void**");
                    Emit($"{inbuf} = dn2cpp_pinvoke_ptrarr_dup({buf}, {arr} != nullptr ? {arr}->length : 0);");
                    strArrWritebacks.Add((arr, buf, inbuf, charUnicode));
                }
            }
            else if (pts[i].Kind == TypeKind.SZArray
                && pts[i].Element! is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }
                && !charUnicode)
            {
                // An Ansi char[]: unlike a CharSet.Unicode char[] (the
                // blittable path below),.NET encodes the whole array to a NUL-terminated
                // UTF-8 buffer (the buffer length and array length diverge). Spill the
                // array to a temp, encode it (copying the input in unless the parameter is
                // [Out]-only), pass the buffer, and decode it back into the array after the
                // call iff the parameter is [Out]/[In,Out] — the default direction is [In]
                // (no write-back), exactly as real.NET marshals an array (probe-confirmed).
                (bool hasIn, bool hasOut) = PInvokeParamInOut(callee, i);
                bool copyIn = !(hasOut && !hasIn); // copy in unless [Out]-only
                string arr = NewTemp("Dn2CppArrayN*");
                Emit($"{arr} = {Cast(e, "Dn2CppArrayN*")};");
                string buf = NewTemp("void*");
                Emit($"{buf} = dn2cpp_pinvoke_chararr_to_ansi({arr}, {(copyIn ? 1 : 0)});");
                args[i] = buf;
                if (hasOut)
                    charArrWritebacks.Add((arr, buf));
            }
            else if (pts[i].Kind == TypeKind.SZArray && CppTypes.IsBlittableStruct(pts[i].Element!))
            {
                // A blittable struct array: unlike a primitive blittable array
                // (the pin-and-pass branch below),.NET marshals a struct array BY COPY
                // with direction semantics (probe-confirmed) — copy the managed buffer to a
                // fresh native buffer in (unless the parameter is [Out]-only, which zeroes
                // it), pass the buffer, and copy it back into the array only when the
                // parameter is [Out]/[In,Out]. The default direction is [In] (copy in, no
                // write-back), so a mutating native is NOT observed unless [Out] is set —
                // matching real.NET (a primitive array, being pinned, always writes back).
                // The buffer is length * sizeof(t_<Struct>) bytes (the packed Dn2CppArrayN
                // stride == the native C array layout). A null array -> a null pointer.
                (bool hasIn, bool hasOut) = PInvokeParamInOut(callee, i);
                bool copyIn = !(hasOut && !hasIn); // copy in unless [Out]-only
                string arr = NewTemp("Dn2CppArrayN*");
                Emit($"{arr} = {Cast(e, "Dn2CppArrayN*")};");
                string buf = NewTemp("void*");
                Emit($"{buf} = dn2cpp_pinvoke_blitarr_to_native({arr}, {(copyIn ? 1 : 0)});");
                args[i] = buf;
                if (hasOut)
                    structArrWritebacks.Add((arr, buf));
            }
            else if (pts[i].Kind == TypeKind.SZArray)
                //.NET pins the blittable array (no copy) and passes the element pointer,
                // so [In] reads and [In,Out] write-backs both hit the managed buffer. A
                // null array marshals as a null pointer. e.Expr is a single-assignment
                // temp, so evaluating it twice in the null guard is side-effect-free. A
                // char[] under CharSet.Unicode reaches here too: its packed
                // char16_t data IS the 2-byte UTF-16 buffer, so the same data pointer is
                // passed and write-backs are likewise visible (probe-confirmed vs.NET).
                args[i] = $"({e.Expr} == nullptr ? (void*)nullptr : {ArrayDataBasePtr(e, pts[i].Element!)})";
            else if (CppTypes.IsBlittableStruct(pts[i]))
                // A blittable value struct is passed by value: its C++ value already has
                // the standard layout t_<Name> matching the native struct (Cast is a
                // no-op, the stack value's type is already t_<Name>).
                args[i] = Cast(e, pts[i].Class!.CppStructName);
            else if (CppTypes.IsMarshalableNonBlittableStruct(pts[i]))
            {
                // A non-blittable but marshalable value struct (string/bool/blittable/
                // nested-blittable fields) passed BY VALUE [In]. The managed t_<Name> layout
                // differs field for field (a string is a Dn2CppString*, a bool is 1 byte
                // against the 4-byte Win32 BOOL), so a raw copy would corrupt the native
                // struct. (Sub-word integers are at their real width on both sides.) Spill
                // the managed value to an addressable local, marshal it field by field into
                // a native tn_<Name> local, and pass that by value. No write-back (a by-value
                // struct is [In]). Any string field's GC buffer stays rooted across the call
                // through the native local on the conservatively scanned stack.
                var sc = pts[i].Class!;
                NotePInvokeMarshalStruct(sc);
                string mloc = NewTemp(sc.CppStructName);
                Emit($"{mloc} = {Cast(e, sc.CppStructName)};");
                string nloc = NewTemp(CppTypes.MarshalStructName(sc));
                Emit($"dn2cpp_marshalin_{sc.CppName}(&{mloc}, &{nloc});");
                args[i] = nloc;
            }
            else if (pts[i].Kind == TypeKind.Primitive && pts[i].Primitive == PrimitiveTypeCode.Boolean)
                // bool -> 4-byte Win32 BOOL by default, or the 1-byte width an
                // explicit [MarshalAs(I1)] forces. The managed bool is already
                // an int32 holding exactly 0/1, so narrowing to the native width is value-
                // preserving.
                args[i] = Cast(e, (ma is { } bm ? CppTypes.BoolMarshalCType(bm) : null) ?? "int32_t");
            else if (pts[i].Kind == TypeKind.Primitive && pts[i].Primitive == PrimitiveTypeCode.Char)
                // char -> 1 byte (default/Ansi: the first byte of its UTF-8 encoding) or
                // 2 bytes (CharSet.Unicode: the raw UTF-16 code unit). The managed char is
                // an int32 holding the 16-bit code unit; narrow to char16_t first.
                args[i] = charUnicode
                    ? Cast(e, "uint16_t")
                    : $"dn2cpp_pinvoke_char_to_ansi({Cast(e, "char16_t")})";
            else if (pts[i].Kind == TypeKind.Class
                && pts[i].Class?.FullName == "System.Text.StringBuilder")
            {
                // StringBuilder -> a caller-allocated native buffer holding the current
                // content. Spill the builder and the buffer pointer to temps:
                // the buffer is the native arg, and both feed the post-call write-back.
                string sbTmp = NewTemp("Dn2CppStringBuilder*");
                Emit($"{sbTmp} = {Cast(e, "Dn2CppStringBuilder*")};");
                string buf = NewTemp("void*");
                Emit($"{buf} = dn2cpp_pinvoke_sb_to_buffer({sbTmp}, {(charUnicode ? 1 : 0)});");
                args[i] = buf;
                sbWritebacks.Add((sbTmp, buf, charUnicode));
            }
            else if (pts[i].Kind == TypeKind.ByRef && pts[i].Element!.IsString)
            {
                // out string / ref string -> the native takes &tmp (a void*) and writes
                // the allocated string through it. Spill the ByRef slot
                // (Dn2CppString**) to a temp, seed the in-buffer (null for [Out]; the
                // marshalled current value for ref), pass &tmp, and decode it back after
                // the call. Out vs ref is told apart by the [Out] parameter attribute.
                var strEnc = CppTypes.StringMarshalEncoding(ma, charUnicode);
                string toHelper = strEnc switch
                {
                    CppTypes.PInvokeStrEnc.Unicode => "dn2cpp_pinvoke_str_to_utf16",
                    CppTypes.PInvokeStrEnc.Utf8 => "dn2cpp_pinvoke_str_to_utf8",
                    _ => "dn2cpp_pinvoke_str_to_ansi",
                };
                string slot = NewTemp("Dn2CppString**");
                Emit($"{slot} = {Cast(e, "Dn2CppString**")};");
                string inbuf = NewTemp("void*");
                if (IsByRefOutParam(callee, i))
                    Emit($"{inbuf} = nullptr;");
                else
                    Emit($"{inbuf} = (void*)({toHelper}(*{slot}));");
                string tmp = NewTemp("void*");
                Emit($"{tmp} = {inbuf};");
                args[i] = $"(void**)&{tmp}";
                byrefStrWritebacks.Add((slot, tmp, inbuf, (int)strEnc));
            }
            else if (pts[i].Kind == TypeKind.ByRef
                && CppTypes.PInvokeByRefNativeType(pts[i].Element!) is { } brt)
                // A ref/out/in of a blittable type:.NET pins the location and
                // passes its address with no copy, so the native reads/writes the managed
                // storage directly — write-backs (out / ref) are automatically visible and
                // need no post-call decode. The managed byref is already a pointer to the
                // element's native-layout storage; cast it to the native element pointer.
                args[i] = Cast(e, brt);
            else if (pts[i].Kind == TypeKind.ByRef
                && CppTypes.IsMarshalableNonBlittableStruct(pts[i].Element!))
            {
                // A ref/out/in of a non-blittable but marshalable value struct. Unlike a
                // blittable byref (pinned, passed by address), the managed t_<Name> layout
                // differs field for field, so we marshal field by field into a native
                // tn_<Name> local, pass its address, and (for [In,Out]/[Out]) marshal the
                // native struct back into the managed location after the call. A byref's
                // default direction is [In,Out]: copy in unless [Out]-only, write back
                // unless [In]-only. The IN snapshot lets the string write-back free only a
                // pointer the native replaced (our [In] buffer is GC memory).
                var sc = pts[i].Element!.Class!;
                NotePInvokeMarshalStruct(sc);
                (bool hasIn, bool hasOut) = PInvokeParamInOut(callee, i);
                bool copyIn = hasIn || !hasOut;     // copy in unless [Out]-only
                bool writeBack = hasOut || !hasIn;  // write back unless [In]-only
                string mptr = NewTemp(sc.CppStructName + "*");
                Emit($"{mptr} = {Cast(e, sc.CppStructName + "*")};");
                string nloc = NewTemp(CppTypes.MarshalStructName(sc));
                string insnap = NewTemp(CppTypes.MarshalStructName(sc));
                if (copyIn)
                    Emit($"dn2cpp_marshalin_{sc.CppName}({mptr}, &{nloc});");
                else
                    // [Out]-only: zero the native struct (string fields null, blittable 0) so
                    // the IN snapshot is empty and the native fills it from scratch.
                    Emit($"{nloc} = {{}};");
                Emit($"{insnap} = {nloc};");
                args[i] = $"&{nloc}";
                if (writeBack)
                    structMarshalWritebacks.Add((sc.CppName, nloc, mptr, insnap));
            }
            else
                // A blittable scalar at its precise native width. (An integer/enum
                // [MarshalAs] never reaches here — validation rejects it as a carve-out,
                // since.NET requires a matching width and a mismatch is illegal.)
                args[i] = Cast(e, CppTypes.NativeAbiType(pts[i])!);
        }

        string call = $"{pinv.CppAlias(callee.Signature)}({string.Join(", ", args)})";

        // SetLastError=true: capture the platform error (errno on POSIX, GetLastError() on
        // Windows — dn2cpp_pinvoke_capture_last_error picks the right one internally, see
        // runtime/core/dn2cpp_runtime.cpp) immediately after the native call, before any
        // post-processing (string decode / bool normalize) or later managed code can perturb
        // it, into the per-thread slot Marshal.GetLastWin32Error reads. The capture
        // call's first statement reads the value, so nothing it does can perturb it.
        bool setLastError = (pinv.ImportAttributes
            & System.Reflection.MethodImportAttributes.SetLastError) != 0;

        // Post-call write-backs run right after the native call (and after the last-error
        // capture, since they allocate/free and would perturb errno/GetLastError): each
        // StringBuilder buffer is read back into its builder, and each byref/[Out]
        // string's native pointer is decoded + conditionally freed into its ByRef slot.
        void EmitPostCallWritebacks()
        {
            foreach (var (sb, buf, uni) in sbWritebacks)
                Emit($"dn2cpp_pinvoke_sb_from_buffer({sb}, {buf}, {(uni ? 1 : 0)});");
            foreach (var (slot, tmp, inbuf, enc) in byrefStrWritebacks)
                Emit($"*{slot} = dn2cpp_pinvoke_byref_str_result({tmp}, {inbuf}, {enc});");
            foreach (var (arr, buf) in charArrWritebacks)
                Emit($"dn2cpp_pinvoke_chararr_from_ansi({arr}, {buf});");
            foreach (var (arr, buf) in structArrWritebacks)
                Emit($"dn2cpp_pinvoke_blitarr_from_native({arr}, {buf});");
            foreach (var (arr, buf, inbuf, uni) in strArrWritebacks)
                Emit($"{(uni ? "dn2cpp_pinvoke_strarr_from_utf16" : "dn2cpp_pinvoke_strarr_from_ansi")}({arr}, {buf}, {inbuf});");
            // Marshal each by-ref non-blittable struct back into its managed location.
            foreach (var (name, nloc, mptr, insnap) in structMarshalWritebacks)
                Emit($"dn2cpp_marshalout_{name}(&{nloc}, {mptr}, &{insnap});");
        }

        if (setLastError || sbWritebacks.Count > 0 || byrefStrWritebacks.Count > 0
            || charArrWritebacks.Count > 0 || structArrWritebacks.Count > 0
            || strArrWritebacks.Count > 0
            || structMarshalWritebacks.Count > 0)
        {
            if (ret.IsVoid)
            {
                Emit(call + ";");
                if (setLastError)
                    Emit("dn2cpp_pinvoke_capture_last_error();");
                EmitPostCallWritebacks();
                return;
            }
            // Spill the raw native result so the last-error capture is the very next
            // statement, then run the write-backs, then post-process the captured temp below.
            string raw = NewTemp(CppTypes.PInvokeNativeType(ret, isReturn: true, charUnicode, retMa)!);
            Emit($"{raw} = {call};");
            if (setLastError)
                Emit("dn2cpp_pinvoke_capture_last_error();");
            EmitPostCallWritebacks();
            call = raw;
        }

        // A string return: the native call yields a NUL-terminated buffer (declared
        // void*); decode it to a managed string and free it — UTF-16 under CharSet.Unicode,
        // UTF-8 otherwise. EmitCallResult then pushes the result.
        if (ret.IsString)
        {
            // The return decode mirrors the arg encoding: [MarshalAs] forces it (LPWStr =
            // UTF-16, LPStr = Ansi, LPUTF8Str = UTF-8), else the method CharSet.
            // Ansi = the host code page on Windows, UTF-8 on Unix.
            call = CppTypes.StringMarshalEncoding(retMa, charUnicode) switch
            {
                CppTypes.PInvokeStrEnc.Unicode => $"dn2cpp_pinvoke_str_from_utf16((char16_t*)({call}))",
                CppTypes.PInvokeStrEnc.Utf8 => $"dn2cpp_pinvoke_str_from_utf8((char*)({call}))",
                _ => $"dn2cpp_pinvoke_str_from_ansi((char*)({call}))",
            };
        }
        // A bool return: the native call yields a 4-byte Win32 BOOL;.NET normalizes any
        // non-zero value to managed true (1) and zero to false (0), so the managed bool
        // is a clean 0/1.
        else if (ret.Kind == TypeKind.Primitive && ret.Primitive == PrimitiveTypeCode.Boolean)
            call = $"((({call}) != 0) ? 1 : 0)";
        // A char return: the native call yields 1 byte (default/Ansi: decode as UTF-8, so
        // any 0x80-0xFF byte -> U+FFFD) or 2 bytes (CharSet.Unicode: the raw UTF-16 code
        // unit). EmitCallResult then pushes it as the int32 char value.
        else if (ret.Kind == TypeKind.Primitive && ret.Primitive == PrimitiveTypeCode.Char)
            call = charUnicode
                ? $"((char16_t)({call}))"
                : $"dn2cpp_pinvoke_char_from_ansi((uint8_t)({call}))";
        EmitCallResult(callee, call);
    }

    /// <summary>True when the <paramref name="paramIndex"/>-th parameter of a P/Invoke
    /// is `out` (<c>[Out]</c> set, <c>[In]</c> not) rather than `ref` (<c>[In,Out]</c> /
    /// no marshal attribute) — read from the method's Parameter table by sequence number
    /// (1-based; 0 is the return). Out byref strings take no input buffer;
    /// a metadata shape we cannot read defaults to ref ([In,Out]).</summary>
    private bool IsByRefOutParam(MethodInfo callee, int paramIndex)
    {
        try
        {
            var reader = callee.Module.Reader;
            var md = reader.GetMethodDefinition(callee.Handle);
            foreach (var ph in md.GetParameters())
            {
                var p = reader.GetParameter(ph);
                if (p.SequenceNumber == paramIndex + 1)
                    return (p.Attributes & System.Reflection.ParameterAttributes.Out) != 0
                        && (p.Attributes & System.Reflection.ParameterAttributes.In) == 0;
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            // A metadata shape we can't read: fall back to ref semantics.
        }
        return false;
    }

    /// <summary>The <c>[In]</c>/<c>[Out]</c> directional attributes on the
    /// <paramref name="paramIndex"/>-th P/Invoke parameter, read from the Parameter
    /// table by sequence number (1-based; 0 is the return). Used by the copy-marshalled
    /// array marshallers (Ansi char[], blittable struct[], string[]): the default
    /// direction for an array reference is [In] (no write-back), so a buffer is decoded
    /// back into the array only when <c>[Out]</c> is set ([Out] / [In,Out]), and the input
    /// is copied in unless the parameter is [Out]-only. A metadata shape we cannot read
    /// defaults to (false, false) = the plain [In] default.</summary>
    private (bool hasIn, bool hasOut) PInvokeParamInOut(MethodInfo callee, int paramIndex)
    {
        try
        {
            var reader = callee.Module.Reader;
            var md = reader.GetMethodDefinition(callee.Handle);
            foreach (var ph in md.GetParameters())
            {
                var p = reader.GetParameter(ph);
                if (p.SequenceNumber == paramIndex + 1)
                    return ((p.Attributes & System.Reflection.ParameterAttributes.In) != 0,
                            (p.Attributes & System.Reflection.ParameterAttributes.Out) != 0);
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            // A metadata shape we can't read: fall back to the [In] default.
        }
        return (false, false);
    }

    private static string FnPtrType(MethodInfo callee)
    {
        string retC = callee.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(callee.Signature.ReturnType);
        var paramCs = new List<string> { callee.DeclaringClass.CppStructName + "*" };
        paramCs.AddRange(callee.Signature.ParameterTypes.Select(CppTypes.Of));
        return $"{retC} (*)({string.Join(", ", paramCs)})";
    }

    /// <summary>Pops call arguments (last-pushed first) and returns them in
    /// declaration order, cast to the callee's parameter types. Also records the
    /// post-call slot fixups for sub-word byref args (see
    /// <see cref="NoteByRefSlotFixup"/>); the call-emitting caller flushes them
    /// with <see cref="EmitByRefSlotFixups"/> right after the call.</summary>
    private List<string> PopArgs(MethodInfo callee, bool hasThis)
    {
        _byRefSlotFixups.Clear();
        var ps = callee.Signature.ParameterTypes;
        var args = new string[ps.Length + (hasThis ? 1 : 0)];
        for (int i = ps.Length - 1; i >= 0; i--)
        {
            var entry = Pop();
            args[i + (hasThis ? 1 : 0)] = CoerceTo(entry, ps[i], CppTypes.Of(ps[i]));
            NoteByRefSlotFixup(entry, ps[i]);
        }
        if (hasThis)
            // A bare `foreach ((IEnumerable<T>)arr)` calls IEnumerable<T>.GetEnumerator
            // with the array itself as the receiver (Roslyn elides the interface-typed
            // local), so the `this` slot is also an array->IEnumerable<T> coercion.
            args[0] = CoerceTo(Pop(), TypeDesc.MakeClass(callee.DeclaringClass),
                callee.DeclaringClass.CppStructName + "*");
        return args.ToList();
    }

    private readonly List<string> _byRefSlotFixups = new();

    /// <summary>Records the post-call re-normalization for a byref argument whose
    /// pointee is a sub-word primitive (or narrow enum) but whose backing storage
    /// is an int32-promoted local/arg slot (an ldloca/ldarga pointer, marked
    /// <see cref="StackEntry.SlotAddr"/>, typed <c>int32_t*</c>). The callee
    /// writes only the storage-width low bytes (<c>*(int16_t*)p = v</c>), so the
    /// slot's upper bytes go stale — a later read of the slot must see the
    /// sign/zero-extended value (IL defines ldloc of an int16 local as
    /// sign-extending), e.g. `short.Parse` writing -1234 through `out short`
    /// must not read back as 64302. The SlotAddr provenance is required, not
    /// just the pointer type: an Unsafe-reinterpreted buffer pointer (a span's
    /// `ref char` viewed as `ref byte`) can carry the same C++ pointer type, and
    /// rewriting four bytes there would stomp neighboring elements. Pointers
    /// already at storage width (array elements, span refs, real-width struct
    /// fields) don't match the promoted-slot pointer type and need no fixup.</summary>
    private void NoteByRefSlotFixup(StackEntry arg, TypeDesc param)
    {
        if (param is not { Kind: TypeKind.ByRef, Element: { } el })
            return;
        string st = CppTypes.StorageOf(el);
        string ct = CppTypes.Of(el);
        if (st == ct || arg.Kind != StackKind.Ptr || !arg.SlotAddr || arg.CppType != ct + "*")
            return;
        _byRefSlotFixups.Add($"*({ct}*)({arg.Expr}) = ({ct})(*({st}*)({arg.Expr}));");
    }

    /// <summary>Emits (and clears) the pending byref slot fixups recorded by the
    /// last <see cref="PopArgs"/>. Must run right after the call expression is
    /// emitted (Push spills eagerly, so the call has executed by then).</summary>
    private void EmitByRefSlotFixups()
    {
        foreach (string fixup in _byRefSlotFixups)
            Emit(fixup);
        _byRefSlotFixups.Clear();
    }

    /// <summary>Best-effort name of a newobj target type, for diagnostics.</summary>
    private string NewobjTypeName(EntityHandle handle)
    {
        if (handle.Kind == HandleKind.MemberReference
            && _c.MemberRefParentTypeName(_module, (MemberReferenceHandle)handle) is { } n)
            return n;
        // A within-assembly ctor (e.g. CoreLib's own `new string(...)`) is a MethodDef;
        // resolve its declaring type so intrinsic-type interception still fires.
        if (handle.Kind == HandleKind.MethodDefinition)
        {
            var cls = ResolveMethodDef((MethodDefinitionHandle)handle).DeclaringClass;
            // Nested-qualified only, never the adopted-async BCL key: `.ctor` is outside the
            // adopted contract, and answering the key here would let an adopted task type's
            // own ctor match a BCL-name-keyed arm (the cold-Task one takes MethodDefs) and
            // be lowered with foreign operand semantics.
            return cls.IntrinsicCppName is not null ? NestedIntrinsicDispatchName(cls) : cls.FullName;
        }
        return handle.Kind.ToString();
    }

    /// <summary>Decode a ctor token's signature from whichever form it resolves to: a
    /// MemberRef when the newobj sits in an app assembly, a MethodDef when it sits in
    /// CoreLib itself.</summary>
    private MethodSignature<TypeDesc> DecodeCtorSignature(EntityHandle handle) =>
        handle.Kind == HandleKind.MemberReference
            ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                .DecodeMethodSignature(_c.SigProvider, _method.Context)
            : _reader.GetMethodDefinition((MethodDefinitionHandle)handle)
                .DecodeSignature(_c.SigProvider, _method.Context);

    /// <summary>Map a ThreadLocal&lt;T&gt; element type to the (kind, type-info, boxing
    /// size, factory thunk) the runtime threadlocal helpers need. kind: 0=reference,
    /// 1=int32, 2=int64/native-int, 3=float, 4=double, 5=struct. Reference T stores the
    /// object directly (ti=nullptr, size=0, no thunk — every Func&lt;T&gt; there returns a
    /// pointer, so the runtime's one cast serves them all); every value kind boxes its
    /// payload through the emitted thunk.
    /// <para>The thunk is what lets a <em>struct</em>-valued factory work at all: a
    /// by-value struct return is a LAYOUT, not a width, so only this side — which knows
    /// <c>CppTypes.Of(T)</c> — can spell the function-pointer type the C ABI uses. The
    /// captureless lambda converts to a plain function pointer (the leading <c>+</c>
    /// forces it), so no emitter registry, header row or per-T symbol is needed.</para></summary>
    private (int kind, string ti, string size, string boxFn) ThreadLocalKindTiSize(TypeDesc t)
    {
        int kind = CppTypes.KindOf(t) switch
        {
            StackKind.Ref => 0,
            StackKind.I4 => 1,
            StackKind.I8 => 2,
            StackKind.R8 => t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single } ? 3 : 4,
            StackKind.Struct => 5,
            _ => throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: ThreadLocal<{t}> value kind not supported"),
        };
        if (kind == 0)
            return (0, "nullptr", "0", "nullptr");
        string ti = TypeInfoExpr(t)
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: ThreadLocal<{t}> has no boxable type-info");
        string ct = CppTypes.Of(t);
        // `__v` lives on the thunk's frame across the box, so a T carrying references is
        // conservatively scanned there exactly as it is in any other emitted body.
        string boxFn =
            $"+[](Dn2CppObject* __dg) -> Dn2CppObject* {{ {ct} __v = "
            + $"(({ct} (*)(Dn2CppObject*))((Dn2CppDelegate*)__dg)->method)(((Dn2CppDelegate*)__dg)->target); "
            + $"return dn2cpp_box({ti}, &__v, sizeof({ct})); }}";
        return (kind, ti, $"(int32_t)sizeof({ct})", boxFn);
    }

    /// <summary>Box a BlockingCollection&lt;T&gt; element into the uniform Dn2CppObject*
    /// slot: a reference T passes through directly; a value-type T is boxed (via an
    /// addressable temp of the exact unboxed layout). Mirrors ThreadLocal's set.</summary>
    private string BlockingCollBoxElement(TypeDesc elemT, StackEntry val)
    {
        if (CppTypes.KindOf(elemT) == StackKind.Ref)
            return $"(Dn2CppObject*)({val.Expr})";
        string ct = CppTypes.Of(elemT);
        string ti = TypeInfoExpr(elemT)
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: BlockingCollection<{elemT}> element has no boxable type-info");
        string tmp = NewTemp(ct);
        Emit($"{tmp} = {Cast(val, ct)};");
        return $"dn2cpp_box({ti}, &{tmp}, sizeof({ct}))";
    }

    /// <summary>The element read out of a boxed BlockingCollection&lt;T&gt; slot: a
    /// reference T casts; a value-type T unboxes. Returns the (stack kind, C++ type,
    /// expression) the caller pushes. Mirrors ThreadLocal's get.</summary>
    private (StackKind kind, string ct, string expr) BlockingCollUnboxElement(TypeDesc elemT, string boxedExpr)
    {
        string ct = CppTypes.Of(elemT);
        if (CppTypes.KindOf(elemT) == StackKind.Ref)
            return (StackKind.Ref, ct, $"({ct})({boxedExpr})");
        string ti = TypeInfoExpr(elemT)
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: BlockingCollection<{elemT}> element has no boxable type-info");
        return (CppTypes.KindOf(elemT), ct, $"*(({ct}*)dn2cpp_unbox({boxedExpr}, {ti}))");
    }

    private void TranslateNewobj(Instruction insn)
    {
        // The raw ctor token, for rgctx slot keying (verified before use).
        _callSiteToken = insn.Token;
        var handle = SRME.EntityHandle(insn.Token);
        if (handle.Kind == HandleKind.MemberReference)
        {
            var mr = _reader.GetMemberReference((MemberReferenceHandle)handle);
            if (mr.Parent.Kind == HandleKind.TypeSpecification)
            {
                var parent = _reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(_c.SigProvider, _method.Context);
                if (parent.Kind == TypeKind.MDArray)
                {
                    TranslateArrayConstructor(parent, mr);
                    return;
                }
            }
        }

        // new Vector64/128/256/512<T>(...) / new Vector<T>(...) — intrinsic value types,
        // so the ctor doesn't resolve to a transpilable method. The ctor shapes (broadcast
        // scalar / array / span) are a subset of the Create static-helper shapes, so route
        // the newobj-expression form through the same software-vector construction.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var nvmr
            && nvmr.Parent.Kind == HandleKind.TypeSpecification
            && _reader.GetTypeSpecification((TypeSpecificationHandle)nvmr.Parent).DecodeSignature(_c.SigProvider, _method.Context)
                is { Kind: TypeKind.Class, Class: { IntrinsicCppName: { } nvicn } nvcls }
            && nvicn.StartsWith("Dn2CppVector", StringComparison.Ordinal)
            && nvcls.Context.TypeArgs.Length == 1)
        {
            var nvsig = nvmr.DecodeMethodSignature(_c.SigProvider, nvcls.Context);
            if (TryEmitVectorOp(nvicn, VecWidthBytes(nvicn), nvcls.Context.TypeArgs[0], "Create", nvsig, null))
                return;
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: new {nvcls.FullName} ctor shape is not supported");
        }

        // new ValueTask<T>(IValueTaskSource<T> source, short token) — the
        // IValueTaskSource-backed form (RandomAccess' ThreadPoolValueTaskSource
        // read/write scheduler constructs it). The dn2cpp ValueTask is always a
        // {task} struct, so bridge the source onto a real pending task:
        // dn2cpp_vts_task registers a runtime continuation through the source's
        // OnCompleted (both interface impls dispatched via the slots resolved at
        // this site) that completes the task with GetResult's value or its thrown
        // fault. Matched before the 1-arg forms below; the reach twin is
        // Compilation.ReachValueTaskSourceBridge.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var bvmr
            && bvmr.Parent.Kind == HandleKind.TypeSpecification
            && _reader.GetTypeSpecification((TypeSpecificationHandle)bvmr.Parent).DecodeSignature(_c.SigProvider, _method.Context)
                is { Kind: TypeKind.Class, Class: { } bvcls }
            && _c.GenericDefFullName(bvcls) == "System.Threading.Tasks.ValueTask"
            && bvmr.DecodeMethodSignature(_c.SigProvider, bvcls.Context).ParameterTypes is
                [{ Kind: TypeKind.Class, Class: { IsInterface: true } bvItf },
                 { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int16 }])
        {
            EmitValueTaskSourceCtor(bvItf);
            return;
        }
        // The same source-backed ctor on the NON-generic ValueTask reached
        // intra-corelib is a MethodDef token (mirroring the (Task) forms below).
        if (handle.Kind == HandleKind.MethodDefinition
            && ResolveMethodDef((MethodDefinitionHandle)handle) is { } bvmd
            && bvmd.DeclaringClass.FullName == "System.Threading.Tasks.ValueTask"
            && bvmd.Signature.ParameterTypes is
                [{ Kind: TypeKind.Class, Class: { IsInterface: true } bvItf2 },
                 { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int16 }])
        {
            EmitValueTaskSourceCtor(bvItf2);
            return;
        }
        // ...and written by a NON-CoreLib assembly it is a MemberRef on the plain
        // (non-generic) ValueTask TypeRef — neither the TypeSpec form above (which only
        // matches the generic ValueTask<T>) nor the MethodDef form (CoreLib's own code)
        // sees it. Every `async IAsyncEnumerable<T>` iterator emits exactly this: the
        // state machine's IAsyncDisposable.DisposeAsync ends in
        // `newobj ValueTask::.ctor(IValueTaskSource, int16)` over the iterator itself.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var bvrr
            && bvrr.Parent.Kind == HandleKind.TypeReference
            && NewobjTypeName(handle) == "System.Threading.Tasks.ValueTask"
            && bvrr.DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes is
                [{ Kind: TypeKind.Class, Class: { IsInterface: true } bvItf3 },
                 { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int16 }])
        {
            EmitValueTaskSourceCtor(bvItf3);
            return;
        }
        // new ValueTask<T>(T result) / new ValueTask<T>(Task<T>) / new ValueTask(Task)
        // — ValueTask is intrinsic (a {task} struct), so its ctor would not resolve to
        // a transpilable method. A Task operand is carried directly; a sync result
        // becomes a pre-completed task.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var vmr
            && vmr.Parent.Kind == HandleKind.TypeSpecification
            && _reader.GetTypeSpecification((TypeSpecificationHandle)vmr.Parent).DecodeSignature(_c.SigProvider, _method.Context)
                is { Kind: TypeKind.Class, Class: { } vcls }
            && _c.GenericDefFullName(vcls) == "System.Threading.Tasks.ValueTask")
        {
            var vsig = vmr.DecodeMethodSignature(_c.SigProvider, vcls.Context);
            var arg = Pop();
            bool fromTask = vsig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } pcl }]
                && _c.GenericDefFullName(pcl) == "System.Threading.Tasks.Task";
            string task = fromTask
                ? $"(Dn2CppTask*)({arg.Expr})"
                : StampTask($"dn2cpp_task_from_result({EmitTaskResultStore(arg)})",
                    TaskTypeForResult(vcls.Context.TypeArgs[0]));
            Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ {task} }}");
            return;
        }
        // The same `new ValueTask(Task)` reached intra-assembly (CoreLib's own code,
        // e.g. the base Stream.WriteAsync(ReadOnlyMemory<byte>) bridging body and the
        // MemoryStream.WriteAsync cancel/catch arms) is a MethodDefinition token on
        // the non-generic ValueTask TypeDef, which the TypeSpec form above never
        // sees. (The generic `new ValueTask<T>(...)` is always a TypeSpec MemberRef,
        // even within CoreLib.) Same lowering: carry the Task / wrap a sync result.
        if (handle.Kind == HandleKind.MethodDefinition
            && ResolveMethodDef((MethodDefinitionHandle)handle) is { } vmd
            && vmd.DeclaringClass.FullName == "System.Threading.Tasks.ValueTask")
        {
            var arg = Pop();
            bool fromTask = vmd.Signature.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } pcl }]
                && _c.GenericDefFullName(pcl) == "System.Threading.Tasks.Task";
            string task = fromTask
                ? $"(Dn2CppTask*)({arg.Expr})"
                : $"dn2cpp_task_from_result({EmitTaskResultStore(arg)})";
            Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ {task} }}");
            return;
        }
        // The same `new ValueTask(Task)` written by a non-CoreLib assembly is a
        // MemberRef on the plain (non-generic) ValueTask TypeRef — the TypeSpec form
        // above only matches the generic ValueTask<T>, and the MethodDef form only
        // CoreLib's own code. Same lowering: carry the Task directly. Only the (Task)
        // ctor shape is intercepted; anything else (e.g. the IValueTaskSource ctor)
        // stays a loud unsupported-newobj error below.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var vrr
            && vrr.Parent.Kind == HandleKind.TypeReference
            && NewobjTypeName(handle) == "System.Threading.Tasks.ValueTask"
            && vrr.DecodeMethodSignature(_c.SigProvider, _method.Context) is { } vrsig
            && vrsig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } vrcl }]
            && _c.GenericDefFullName(vrcl) == "System.Threading.Tasks.Task")
        {
            var varg = Pop();
            Push(StackKind.Struct, "Dn2CppTaskAwaiter", $"Dn2CppTaskAwaiter{{ (Dn2CppTask*)({varg.Expr}) }}");
            return;
        }

        // new Task<TResult>(Func<TResult> [, state/hints...]) — Task is intrinsic (an
        // opaque Dn2CppTask*), so the ctor never resolves to a transpilable method.
        // Allocate a cold (unstarted) task carrying the delegate; Task.Start submits it
        // to the worker pool, RunSynchronously runs it inline. The generic form is
        // always a TypeSpec MemberRef, mirroring the ValueTask<T> intercept above.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var ctmr
            && ctmr.Parent.Kind == HandleKind.TypeSpecification
            && _reader.GetTypeSpecification((TypeSpecificationHandle)ctmr.Parent).DecodeSignature(_c.SigProvider, _method.Context)
                is { Kind: TypeKind.Class, Class: { } ctcls }
            && _c.GenericDefFullName(ctcls) == "System.Threading.Tasks.Task")
        {
            var ctsig = ctmr.DecodeMethodSignature(_c.SigProvider, ctcls.Context);
            EmitColdTaskCtor(ctcls.Context.TypeArgs[0], ctsig.ParameterTypes, TypeDesc.MakeClass(ctcls));
            return;
        }
        // The same cold-task ctor on the NON-generic Task: a plain TypeRef MemberRef
        // from an app assembly, or a MethodDef when CoreLib's own code constructs one.
        if (NewobjTypeName(handle) == "System.Threading.Tasks.Task"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var ctps = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            ClassInfo nonGenericTaskClass = handle.Kind == HandleKind.MemberReference
                ? _c.ResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context).DeclaringClass
                : ResolveMethodDef((MethodDefinitionHandle)handle).DeclaringClass;
            EmitColdTaskCtor(null, ctps, TypeDesc.MakeClass(nonGenericTaskClass));
            return;
        }

        // new TaskCompletionSource(<T>)([state][, TaskCreationOptions]) — the source
        // wrapper and its pending task are distinct objects. The generic form is a TypeSpec MemberRef,
        // the non-generic a TypeRef MemberRef / MethodDef, mirroring Task above.
        // Every ctor argument is popped and ignored: TaskCreationOptions is a
        // scheduling hint (like StartNew's), and the `object? state` argument only
        // feeds Task.AsyncState, which is not modeled.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var tcmr
            && tcmr.Parent.Kind == HandleKind.TypeSpecification
            && _reader.GetTypeSpecification((TypeSpecificationHandle)tcmr.Parent).DecodeSignature(_c.SigProvider, _method.Context)
                is { Kind: TypeKind.Class, Class: { } tccls }
            && _c.GenericDefFullName(tccls) == "System.Threading.Tasks.TaskCompletionSource")
        {
            var tcsig = tcmr.DecodeMethodSignature(_c.SigProvider, tccls.Context);
            for (int i = 0; i < tcsig.ParameterTypes.Length; i++)
                Pop(); // object? state (AsyncState is unmodeled) / TaskCreationOptions hint
            string tcsType = TypeInfoExpr(TypeDesc.MakeClass(tccls), SRME.GetToken(tcmr.Parent))
                ?? throw new InvalidOperationException($"{tccls.FullName} has no runtime identity");
            TypeDesc taskType = TaskTypeForResult(tccls.Context.TypeArgs[0]);
            string taskTypeInfo = TaskTypeInfo(taskType);
            Push(StackKind.Ref, "Dn2CppTaskCompletionSource*",
                $"dn2cpp_tcs_alloc({tcsType}, {taskTypeInfo})");
            return;
        }
        if (NewobjTypeName(handle) == "System.Threading.Tasks.TaskCompletionSource"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var tcps = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            for (int i = 0; i < tcps.Length; i++)
                Pop(); // object? state (AsyncState is unmodeled) / TaskCreationOptions hint
            ClassInfo nonGenericSourceClass = handle.Kind == HandleKind.MemberReference
                ? _c.ResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context).DeclaringClass
                : ResolveMethodDef((MethodDefinitionHandle)handle).DeclaringClass;
            string tcsType = TypeInfoExpr(TypeDesc.MakeClass(nonGenericSourceClass))
                ?? throw new InvalidOperationException($"{nonGenericSourceClass.FullName} has no runtime identity");
            TypeDesc taskType = Comp.FindClassByFullName("System.Threading.Tasks.Task") is { } taskClass
                ? TypeDesc.MakeClass(taskClass)
                : throw new InvalidOperationException("System.Threading.Tasks.Task is not in the completed image");
            string taskTypeInfo = TypeInfoExpr(taskType)
                ?? throw new InvalidOperationException($"{taskType} has no runtime identity");
            Push(StackKind.Ref, "Dn2CppTaskCompletionSource*",
                $"dn2cpp_tcs_alloc({tcsType}, {taskTypeInfo})");
            return;
        }

        // new CancellationTokenSource — intrinsic runtime object. Register the
        // OperationCanceledException type so a canceled task is catchable by a typed
        // clause. The ctor token is a MemberRef for a cross-assembly `new` and a MethodDef
        // when CoreLib's own code constructs one (reachable once its defining assembly is
        // loaded, e.g. via --auto-ref) — read the signature from whichever form, mirroring
        // StringBuilder, so the intrinsic push fires regardless of how the token resolves.
        //
        // The timed ctors — (int millisecondsDelay) / (TimeSpan delay) — ARE CancelAfter,
        // armed at construction, and real .NET models them as literally that (a later
        // CancelAfter reschedules the ctor's timer rather than adding a second one), so the
        // delay routes to the same runtime helper CancelAfter uses; dropping it would build
        // a source that silently never cancels. .NET 8's (TimeSpan, TimeProvider) overload
        // drops the provider — the runtime timer is the only clock dn2cpp has.
        if (NewobjTypeName(handle) == "System.Threading.CancellationTokenSource"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var ctsParams = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            for (int i = ctsParams.Length - 1; i >= 1; i--)
                Pop(); // TimeProvider — unmodeled
            string ctsNew = "dn2cpp_cts_new()";
            if (ctsParams.Length >= 1)
            {
                var delay = Pop();
                string ctorMs = IsTimeSpan(ctsParams[0])
                    ? $"(int64_t)dn2cpp_timespan_total({TSVal(delay)}, 10000LL)"
                    : $"(int64_t)({delay.Expr})";
                ctsNew = $"dn2cpp_cts_new_after({ctorMs})";
            }
            EmitCanceledExcRegistration();
            Push(StackKind.Ref, "Dn2CppCancelSource*", ctsNew);
            return;
        }
        // new CancellationToken(bool canceled) — the expression (newobj) form.
        if (NewobjTypeName(handle) == "System.Threading.CancellationToken")
        {
            var arg = Pop(); // bool canceled
            EmitCanceledExcRegistration();
            Push(StackKind.Struct, "Dn2CppCancelToken",
                $"Dn2CppCancelToken{{ ({arg.Expr}) ? dn2cpp_cts_canceled() : nullptr }}");
            return;
        }
        // new Thread(ThreadStart|ParameterizedThreadStart [, int maxStackSize]) — the
        // delegate is the first arg; any maxStackSize is ignored. The ThreadStart vs
        // ParameterizedThreadStart distinction is resolved at Start() vs Start(object).
        // Both ctor token forms are accepted, for the reason at the ManualResetEvent arm.
        if (NewobjTypeName(handle) == "System.Threading.Thread"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var thSig = DecodeCtorSignature(handle);
            for (int i = 0; i < thSig.ParameterTypes.Length - 1; i++)
                Pop(); // maxStackSize (ignored)
            var start = Pop(); // the delegate
            Push(StackKind.Ref, "Dn2CppThread*", $"dn2cpp_thread_new((Dn2CppObject*)({start.Expr}))");
            return;
        }
        // new SemaphoreSlim(initialCount [, maxCount]) — a real counting semaphore.
        // The ctor token is a MemberRef for a cross-assembly `new` and a MethodDef
        // when CoreLib's own code constructs one (Stream.
        // EnsureAsyncActiveSemaphoreInitialized's `new SemaphoreSlim(1, 1)` on the
        // async Stream path) — read the signature from whichever form, mirroring
        // CancellationTokenSource above.
        if (NewobjTypeName(handle) == "System.Threading.SemaphoreSlim"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var ssigParams = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            string max = "2147483647"; // SemaphoreSlim(int) defaults maxCount to int.MaxValue
            if (ssigParams.Length == 2)
                max = Pop().Expr;
            var initial = Pop();
            Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_semaphore_new({initial.Expr}, {max})");
            return;
        }
        // new ManualResetEvent(bool) / ManualResetEventSlim([bool [, int spinCount]]) —
        // a manual-reset event (Set releases all waiters until Reset).
        //
        // BOTH ctor token forms are accepted here and in the sibling event/lock arms: the
        // token is a MemberRef for a cross-assembly `new` and a MethodDef when CoreLib's
        // own code constructs one. These types are intrinsic-mapped, so reachability nulls
        // the ctor edge for both forms — an arm answering only the MemberRef mouth leaves
        // the MethodDef mouth emitting a managed call to a body the cut deleted, a
        // `cut ⟹ route` violation that fails at C++ LINK time and nowhere earlier.
        //
        // The handle each arm stamps is the CONSTRUCTED type's, not the family's.
        // ManualResetEvent and ManualResetEventSlim share this arm because both are
        // manual-reset, but they are different CLR types with different BASES — Slim is not
        // a WaitHandle — so they must not share a type-info. The reset mode and the type are
        // independent facts; do not derive one from the other.
        if (NewobjTypeName(handle) is "System.Threading.ManualResetEvent" or "System.Threading.ManualResetEventSlim"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var esig = DecodeCtorSignature(handle);
            int pc = esig.ParameterTypes.Length;
            for (int i = 0; i < pc - 1; i++)
                Pop(); // spinCount (ignored)
            string init = pc >= 1 ? Pop().Expr : "0"; // parameterless Slim defaults to false
            string eti = NewobjTypeName(handle) == "System.Threading.ManualResetEventSlim"
                ? "&dn2cpp_manualreseteventslim_type"
                : "&dn2cpp_manualresetevent_type";
            Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_event_new({init}, 1, {eti})");
            return;
        }
        // new AutoResetEvent(bool) — an auto-reset event (each Set releases one waiter).
        if (NewobjTypeName(handle) == "System.Threading.AutoResetEvent"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var arg = Pop(); // bool initialState
            Push(StackKind.Ref, "Dn2CppObject*",
                $"dn2cpp_event_new({arg.Expr}, 0, &dn2cpp_autoresetevent_type)");
            return;
        }
        // new EventWaitHandle(bool initialState, EventResetMode mode) — the base type
        // itself, whose reset mode is an ARGUMENT rather than a property of the class.
        // EventResetMode.AutoReset is 0 and ManualReset is 1, which is already this
        // helper's `manualReset` convention, so the enum value passes straight through.
        //
        // EXACTLY the two-parameter overload. The (…, string name[, out bool createdNew])
        // forms name a SYSTEM-WIDE event this runtime has no registry for; they are
        // declined rather than popped, since answering with a process-local event would
        // hand back something that looks like it worked and shares nothing.
        if (NewobjTypeName(handle) == "System.Threading.EventWaitHandle"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition
            && DecodeCtorSignature(handle).ParameterTypes is [{ Primitive: PrimitiveTypeCode.Boolean }, _])
        {
            var mode = Pop();     // EventResetMode
            var ewhInit = Pop();  // bool initialState
            Push(StackKind.Ref, "Dn2CppObject*",
                $"dn2cpp_event_new({ewhInit.Expr}, {mode.Expr}, &dn2cpp_event_type)");
            return;
        }
        // new SafeWaitHandle(IntPtr handle, bool ownsHandle). Runtime events lazily mint
        // an alias wrapper in get_SafeWaitHandle; this constructor is the distinct route
        // for an OS handle later attached through WaitHandle.set_SafeWaitHandle.
        if (NewobjTypeName(handle) == "Microsoft.Win32.SafeHandles.SafeWaitHandle"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition
            && DecodeCtorSignature(handle).ParameterTypes.Length == 2)
        {
            var owns = Pop();
            var raw = Pop();
            Push(StackKind.Ref, "Dn2CppObject*",
                $"dn2cpp_safewaithandle_new((intptr_t)({raw.Expr}), (int32_t)({owns.Expr}))");
            return;
        }
        // new CountdownEvent(int initialCount) — a real countdown latch (Signal counts
        // down; Wait blocks until it reaches zero). initialCount == 0 starts already set.
        //
        // BOTH ctor token forms are accepted here and in the four arms below, for the
        // reason spelled out at the ManualResetEvent arm above.
        if (NewobjTypeName(handle) == "System.Threading.CountdownEvent"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            // The intrinsic's only mint point: note it so the emitter installs the
            // IDisposable row onto the runtime type-info — a `using`, an interface-typed
            // local or a cast dispatches Dispose through that map.
            _c.NoteIntrinsicInterfaces("System.Threading.CountdownEvent");
            var initial = Pop();
            Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_countdown_new({initial.Expr})");
            return;
        }
        // new Barrier(int participantCount [, Action<Barrier> postPhaseAction]) — a real
        // cyclic phase barrier. The optional post-phase action runs once per phase, on the
        // last arriving thread.
        if (NewobjTypeName(handle) == "System.Threading.Barrier"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            _c.NoteIntrinsicInterfaces("System.Threading.Barrier"); // IDisposable row
            var bsig = DecodeCtorSignature(handle);
            string post = "nullptr";
            if (bsig.ParameterTypes.Length == 2)
                post = $"(Dn2CppObject*)({Pop().Expr})"; // Action<Barrier> postPhaseAction
            var count = Pop();
            Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_barrier_new({count.Expr}, {post})");
            return;
        }
        // new ReaderWriterLockSlim([LockRecursionPolicy]) — a real reader-writer lock.
        // The policy is carried through: the runtime's per-thread ownership tracking
        // applies real .NET's recursion matrix under either policy, so the enum value
        // decides throw-vs-count at each re-entry.
        if (NewobjTypeName(handle) == "System.Threading.ReaderWriterLockSlim"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            _c.NoteIntrinsicInterfaces("System.Threading.ReaderWriterLockSlim"); // IDisposable row
            var rwSig = DecodeCtorSignature(handle);
            string policy = "0"; // LockRecursionPolicy.NoRecursion (the parameterless default)
            if (rwSig.ParameterTypes.Length == 1)
                policy = Pop().Expr;
            Push(StackKind.Ref, "Dn2CppObject*", $"dn2cpp_rwlock_new({policy})");
            return;
        }
        // new ParallelOptions() — the parameterless ctor only (it has no others).
        // CancellationToken/TaskScheduler stay default (a get/set on them is a carve-out,
        // raised loudly in TryEmitConcurrentIntrinsic).
        if (NewobjTypeName(handle) == "System.Threading.Tasks.ParallelOptions"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            Push(StackKind.Ref, "Dn2CppObject*", "dn2cpp_parallel_options_new()");
            return;
        }
        // new Timer(TimerCallback [, object state, dueTime, period]) — a per-timer OS
        // thread. The 1-arg form starts idle (dueMs/periodMs = -1, no state): it must be
        // Change()d to begin firing. The 4-arg forms take dueTime/period as int, long,
        // uint, or TimeSpan (converted to int64 ms by TimerMs). A non-positive period (or
        // Timeout.Infinite) is one-shot; a positive period re-fires periodically.
        if (NewobjTypeName(handle) == "System.Threading.Timer"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            // The only mint point of the intrinsic timer: note it so the emitter can
            // install its interface rows onto the runtime timer type-info — `using (new
            // Timer(...))` dispatches Dispose through the interface map. The note sits
            // INSIDE the token-kind gate, so a mouth this arm declined would leave the row
            // uninstalled and `using (new Timer(...))` a runtime fatal — silently, since a
            // missing interface row is not a link error.
            _c.NoteIntrinsicInterfaces("System.Threading.Timer");
            var tmSig = DecodeCtorSignature(handle);
            if (tmSig.ParameterTypes.Length == 1)
            {
                var cb1 = Pop(); // TimerCallback
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_timer_new((Dn2CppObject*)({cb1.Expr}), nullptr, -1LL, -1LL)");
                return;
            }
            var period = Pop();
            var dueTime = Pop();
            var state = Pop();
            var cb = Pop();
            string dueMs = TimerMs(dueTime, tmSig.ParameterTypes[2]);
            string periodMs = TimerMs(period, tmSig.ParameterTypes[3]);
            Push(StackKind.Ref, "Dn2CppObject*",
                $"dn2cpp_timer_new((Dn2CppObject*)({cb.Expr}), (Dn2CppObject*)({state.Expr}), {dueMs}, {periodMs})");
            return;
        }
        // TimeProvider.System.CreateTimer constructs CoreLib's private adapter with
        // (dueTime, period, callback, state). Its real ctor reaches TimerQueue's runtime
        // thread/ThreadPool internals; represent it with the same timer object as the
        // public Timer intrinsic. A custom TimeProvider override remains ordinary IL.
        if (NewobjTypeName(handle) == "System.TimeProvider+SystemTimeProviderTimer"
            && handle.Kind == HandleKind.MethodDefinition)
        {
            _c.NoteIntrinsicInterfaces("System.TimeProvider+SystemTimeProviderTimer");
            var tmSig = DecodeCtorSignature(handle);
            // Four operands are popped below and the first two parameters read, so the shape
            // is asserted first: a name-gated route may decline a shape, never pop one.
            if (tmSig.ParameterTypes.Length != 4)
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: "
                    + "System.TimeProvider+SystemTimeProviderTimer..ctor expects "
                    + $"(dueTime, period, callback, state), got {SigArgs(tmSig)}");
            var state = Pop();
            var callback = Pop();
            var period = Pop();
            var dueTime = Pop();
            Push(StackKind.Ref, "Dn2CppObject*",
                $"dn2cpp_timeprovider_timer_new((Dn2CppObject*)({callback.Expr}), (Dn2CppObject*)({state.Expr}), " +
                $"{TimerMs(dueTime, tmSig.ParameterTypes[0])}, {TimerMs(period, tmSig.ParameterTypes[1])})");
            return;
        }
        // new ThreadLocal<T>() / ThreadLocal<T>(Func<T>) / ThreadLocal<T>(Func<T>, bool
        // trackAllValues) — per-instance, per-thread storage. ThreadLocal is intrinsic (a
        // Dn2CppThreadLocal* reference), so its ctor would not resolve to a transpilable
        // method; intercept newobj and allocate the runtime object. The value is stored
        // boxed in a uniform slot (one shape for every T) and the optional Func<T> factory
        // runs lazily in the runtime — through the boxing thunk ThreadLocalKindTiSize
        // emits for a value-type T, which is what makes a struct-valued factory work.
        // trackAllValues is dropped (the all-thread snapshot Values is a carve-out).
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var tlMr
            && tlMr.Parent.Kind == HandleKind.TypeSpecification
            && _reader.GetTypeSpecification((TypeSpecificationHandle)tlMr.Parent)
                .DecodeSignature(_c.SigProvider, _method.Context)
                is { Kind: TypeKind.Class, Class: { } tlCls }
            && _c.GenericDefFullName(tlCls) == "System.Threading.ThreadLocal")
        {
            _c.NoteIntrinsicInterfaces("System.Threading.ThreadLocal`1"); // IDisposable row
            var tlSig = tlMr.DecodeMethodSignature(_c.SigProvider, tlCls.Context);
            var elemT = tlCls.Context.TypeArgs[0];
            if (tlSig.ParameterTypes.Length == 2)
                Pop(); // trackAllValues (dropped — the all-thread Values snapshot is not modeled)
            string factory = tlSig.ParameterTypes.Length >= 1
                ? $"(Dn2CppObject*)({Pop().Expr})" // Func<T> valueFactory
                : "nullptr";                         // parameterless ctor => default(T)
            var (tlKind, tlTi, tlSize, tlBoxFn) = ThreadLocalKindTiSize(elemT);
            Push(StackKind.Ref, "Dn2CppThreadLocal*",
                $"(Dn2CppThreadLocal*)dn2cpp_threadlocal_new({factory}, {tlTi}, {tlSize}, {tlKind}, {tlBoxFn})");
            return;
        }
        // new BlockingCollection<T>() / BlockingCollection<T>(int boundedCapacity) — a
        // producer/consumer blocking queue. Intrinsic (a Dn2CppBlockingCollection*), so its
        // ctor would not resolve to a transpilable method; intercept newobj and allocate the
        // runtime object. Elements are stored boxed in a uniform slot (one shape for int/long/
        // float/double/reference T), boxed/unboxed by T's kind at each Add/Take call site, so
        // the element type is not needed here — only the bound. boundedCapacity 0 => unbounded.
        // The IProducerConsumerCollection<T>-backed ctors are a carve-out.
        if (handle.Kind == HandleKind.MemberReference
            && _reader.GetMemberReference((MemberReferenceHandle)handle) is var bcMr
            && bcMr.Parent.Kind == HandleKind.TypeSpecification
            && _reader.GetTypeSpecification((TypeSpecificationHandle)bcMr.Parent)
                .DecodeSignature(_c.SigProvider, _method.Context)
                is { Kind: TypeKind.Class, Class: { } bcCls }
            && _c.GenericDefFullName(bcCls) == "System.Collections.Concurrent.BlockingCollection")
        {
            // IDisposable row
            _c.NoteIntrinsicInterfaces("System.Collections.Concurrent.BlockingCollection`1");
            var bcSig = bcMr.DecodeMethodSignature(_c.SigProvider, bcCls.Context);
            string capacity;
            if (bcSig.ParameterTypes.Length == 0)
                capacity = "0"; // unbounded
            else if (bcSig.ParameterTypes.Length == 1
                && bcSig.ParameterTypes[0] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 })
                capacity = $"(int32_t)({Pop().Expr})"; // boundedCapacity
            else
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: only new BlockingCollection<T>() " +
                    "and new BlockingCollection<T>(int boundedCapacity) are supported (the " +
                    "IProducerConsumerCollection<T> ctors are a carve-out)");
            Push(StackKind.Ref, "Dn2CppBlockingCollection*",
                $"(Dn2CppBlockingCollection*)dn2cpp_blockingcoll_new({capacity})");
            return;
        }

        // new ResourceManager(baseName, assembly) / new ResourceManager(Type).
        // ResourceManager is intrinsic (see CoreIntrinsics.s_intrinsicTypes: its real
        // lookup walks CultureInfo.Parent chains and probes satellite assemblies, and
        // dn2cpp has neither), so its ctor resolves to no transpilable body; intercept
        // newobj and allocate the runtime (baseName, assembly) pair. Both token kinds,
        // for the StringBuilder reason above.
        if (NewobjTypeName(handle) == "System.Resources.ResourceManager"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var rmParams = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            if (rmParams is [{ IsString: true }, { Kind: TypeKind.Class, Class.FullName: "System.Reflection.Assembly" }])
            {
                string asm = Cast(Pop(), "const char*");
                string bn = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppResourceManager*",
                    $"dn2cpp_resourcemanager_new({bn}, {asm})");
            }
            else if (rmParams is [{ Kind: TypeKind.Class, Class.FullName: "System.Type" }])
            {
                // new ResourceManager(Type resourceSource): .NET's base name is the
                // type's FullName and the assembly is the type's own. Composed in the
                // runtime, where both are one call away from the Type handle.
                string t = Cast(Pop(), "Dn2CppType*");
                Push(StackKind.Ref, "Dn2CppResourceManager*",
                    $"dn2cpp_resourcemanager_new_for_type({t})");
            }
            else
            {
                // new ResourceManager(baseName, assembly, Type usingResourceSet) is the
                // one remaining public ctor, and it names a CUSTOM ResourceSet subclass
                // to instantiate — i.e. it asks for exactly the reader this model
                // replaces. Refusing at the transpile is the honest answer; silently
                // dropping the argument would run the builtin reader under a name that
                // promised the caller's own.
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: only " +
                    "new ResourceManager(string baseName, Assembly) and " +
                    "new ResourceManager(Type) are supported. The " +
                    "(baseName, assembly, Type usingResourceSet) ctor names a custom " +
                    "ResourceSet type to construct, which is the reader dn2cpp replaces " +
                    "with its own .resources reader");
            }
            return;
        }

        // new StringBuilder([capacity] / [string]) — StringBuilder is intrinsic, so
        // its ctor would not resolve to a transpilable method; intercept newobj and
        // allocate the runtime growable buffer. The ctor token is a MemberRef
        // for a cross-assembly `new StringBuilder` and a MethodDef when CoreLib's own
        // code (e.g. PathInternal.NormalizeDirectorySeparators) constructs one — handle
        // both, else the MethodDef form falls through to a raw t_System_Text_StringBuilder
        // alloc + a dangling ctor call (the scan cuts the intrinsic ctor body).
        if (NewobjTypeName(handle) == "System.Text.StringBuilder"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var sbParams = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            // Capacity is.NET's initial buffer size, which a P/Invoke StringBuilder
            // passes as the native buffer length, so seed it precisely:
            // new=16, new(int n)=n, new(string)=max(16,len), new(string,int)=that cap.
            if (sbParams is [{ IsString: true }])
            {
                string s = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_new_str({s})");
            }
            else if (sbParams is [{ IsString: true }, _])
            {
                // new(string value, int capacity)
                string cap = Cast(Pop(), "int32_t");
                string s = Cast(Pop(), "Dn2CppString*");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_new_str_cap({s}, {cap})");
            }
            else if (sbParams.Length >= 1 && !sbParams[0].IsString)
            {
                // new(int capacity) / new(int capacity, int maxCapacity) — maxCapacity
                // is ignored (the buffer grows as needed; it caps nothing here).
                for (int i = sbParams.Length - 1; i >= 1; i--)
                    Pop();
                string cap = Cast(Pop(), "int32_t");
                Push(StackKind.Ref, "Dn2CppStringBuilder*", $"dn2cpp_sb_new_cap({cap})");
            }
            else
            {
                // new — and the rare substring overload new(string, int, int, int),
                // whose start/length slice is a carve-out (lowers to an empty
                // builder, like the parameterless form).
                for (int i = 0; i < sbParams.Length; i++)
                    Pop();
                Push(StackKind.Ref, "Dn2CppStringBuilder*", "dn2cpp_sb_new()");
            }
            return;
        }

        // new StackTrace(...) / new StackFrame(...) — the stack-trace model
        // (see CoreIntrinsics.s_intrinsicTypes and runtime/core/dn2cpp.h). Both
        // types are intrinsic, so their real ctors — which bottom out in
        // StackFrameHelper's InternalCall stack walker — never resolve to a
        // transpilable method; intercept newobj and build the runtime object. The
        // token is a MemberRef for a cross-assembly `new` and a MethodDef when
        // CoreLib's own code constructs one (Environment.StackTrace's getter does),
        // so both forms are handled, as with StringBuilder above.
        //
        // Every overload of both families lands here — the sibling-overload rule:
        // one left out would fall through to a raw t_System_Diagnostics_StackTrace
        // alloc plus a dangling call to a ctor whose body the scan cut.
        if (NewobjTypeName(handle) is "System.Diagnostics.StackTrace" or "System.Diagnostics.StackFrame"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            bool isTrace = NewobjTypeName(handle) == "System.Diagnostics.StackTrace";
            var stParams = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            EmitStackDiagnosticNewobj(isTrace, stParams);
            return;
        }

        // new CultureInfo(name[, useUserOverride]) / new NumberFormatInfo — both
        // intrinsic, lowering to a const Dn2CppNumberFormatInfo*. CultureInfo
        // resolves the built-in named-culture table; NumberFormatInfo is a fresh
        // mutable invariant copy that an object initializer / setters customize.
        // As with StringBuilder/CancellationTokenSource, the ctor token is a MemberRef
        // cross-assembly and a MethodDef when CoreLib's own code constructs one
        // (reachable once its defining assembly is loaded, e.g. via --auto-ref) — read
        // the signature from whichever form so the intrinsic push fires regardless.
        if (NewobjTypeName(handle) == "System.Globalization.CultureInfo"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var ciParams = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            for (int i = ciParams.Length - 1; i >= 1; i--)
                Pop(); // useUserOverride / CultureInfo-parent overloads: keep only the name
            string nm = Cast(Pop(), "Dn2CppString*");
            Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", $"dn2cpp_culture_by_name({nm})");
            return;
        }
        if (NewobjTypeName(handle) == "System.Globalization.NumberFormatInfo")
        {
            Push(StackKind.Ref, "const Dn2CppNumberFormatInfo*", "dn2cpp_nfi_new()");
            return;
        }
        // new IntPtr(int/long/void*) / new UIntPtr(...) — native ints are primitives
        // (intptr_t/uintptr_t) in dn2cpp, so the ctor is a width cast, never a struct
        // construction (without this the newobj lowers to a t_System_IntPtr temp + a
        // transpiled .ctor call, and the value can't convert back to intptr_t).
        // Reached intra-corelib from SafeFileHandle..ctor's `new IntPtr(-1)`.
        if (NewobjTypeName(handle) is "System.IntPtr" or "System.UIntPtr")
        {
            bool unsNative = NewobjTypeName(handle) == "System.UIntPtr";
            string nativeCt = unsNative ? "uintptr_t" : "intptr_t";
            var nv = Pop();
            Push(StackKind.I8, nativeCt, $"(({nativeCt})({nv.Expr}))");
            return;
        }
        // new decimal(...) — Decimal is intrinsic (Dn2CppDecimal value type), so its
        // ctor doesn't resolve to a transpilable method. The newobj expression form
        // pushes the constructed value. A MemberRef from an app assembly but a
        // MethodDef when the newobj sits in CoreLib itself (the integer structs'
        // TryConvert* bodies construct `new decimal(MaxValue)` bounds); accept
        // both, mirroring the TimeSpan/DateTime intercepts below.
        if (NewobjTypeName(handle) == "System.Decimal"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            Push(StackKind.Struct, "Dn2CppDecimal", EmitDecimalCtorExpr(DecodeCtorSignature(handle)));
            return;
        }
        // new TimeSpan(...) / new DateTime(...) — both intrinsic value types, so the
        // newobj expression form pushes the constructed value. A MemberRef from an app
        // assembly but a MethodDef when the newobj sits in CoreLib itself (Stopwatch.
        // get_Elapsed → `new TimeSpan(GetElapsedDateTimeTicks())`); accept both, mirroring
        // the DateTimeOffset intercept below.
        if (NewobjTypeName(handle) == "System.TimeSpan"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            Push(StackKind.Struct, "Dn2CppTimeSpan", EmitTimeSpanCtorExpr(DecodeCtorSignature(handle)));
            return;
        }
        if (NewobjTypeName(handle) == "System.DateTime"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            Push(StackKind.Struct, "Dn2CppDateTime", EmitDateTimeCtorExpr(DecodeCtorSignature(handle)));
            return;
        }
        // A MemberRef from an app assembly but a MethodDef when the newobj sits in
        // CoreLib itself (e.g. FileStatus's mtime bridge constructing the
        // DateTimeOffset it returns) — read the signature from whichever form,
        // mirroring the String ctor intercept, so the intrinsic push fires
        // regardless of how the token resolves.
        if (NewobjTypeName(handle) == "System.DateTimeOffset"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            Push(StackKind.Struct, "Dn2CppDateTimeOffset",
                EmitDateTimeOffsetCtorExpr(DecodeCtorSignature(handle)));
            return;
        }
        // new DateOnly(year, month, day) — intrinsic value type, newobj expression form.
        //
        // BOTH ctor token forms are accepted here, at the TimeOnly and GCHandle arms
        // below, and at the two interpolated-string-handler arms further down, for the
        // reason at the ManualResetEvent arm. For a VALUE type the fall-through is worse
        // than a link error: it emits `{cls.CppStructName}{}`, and an intrinsic type's
        // CppStructName (t_System_DateOnly) is not the emitted struct (Dn2CppDateOnly) —
        // nothing declares it, so the failure is a C++ COMPILE error on an undeclared
        // type, with no cause attached.
        if (NewobjTypeName(handle) == "System.DateOnly"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            Push(StackKind.Struct, "Dn2CppDateOnly", EmitDateOnlyCtorExpr(DecodeCtorSignature(handle)));
            return;
        }
        // new TimeOnly(...) — intrinsic value type, newobj expression form.
        if (NewobjTypeName(handle) == "System.TimeOnly"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            Push(StackKind.Struct, "Dn2CppTimeOnly", EmitTimeOnlyCtorExpr(DecodeCtorSignature(handle)));
            return;
        }
        // new GCHandle(object value [, GCHandleType type]) — GCHandle.Alloc is
        // [AggressiveInlining], so Roslyn may inline it to this private value-type
        // newobj at the call site. Mirror the Alloc handling (shared EmitGCHandleAlloc):
        // forward the GCHandleType so the runtime picks strong vs weak storage; the
        // type-less ctor is Normal (2).
        if (NewobjTypeName(handle) == "System.Runtime.InteropServices.GCHandle"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var gsig = DecodeCtorSignature(handle);
            var handleType = gsig.ParameterTypes.Length == 2 ? $"(int32_t)({Pop().Expr})" : "2";
            var value = Pop();
            Push(StackKind.Struct, "Dn2CppGCHandle", EmitGCHandleAlloc(value, handleType));
            return;
        }
        // new DependentHandle(object? target, object? dependent) — the ephemeron
        // primitive behind ConditionalWeakTable (CWT's CreateEntryNoResize constructs
        // one per entry). Allocates the weak-target/strong-dependent runtime cell;
        // the member surface is lowered in TryEmitDependentHandleIntrinsic.
        if (NewobjTypeName(handle) == "System.Runtime.DependentHandle")
        {
            var dependent = Pop();
            var target = Pop();
            Push(StackKind.Struct, "Dn2CppDependentHandle",
                $"dn2cpp_dependenthandle_alloc((Dn2CppObject*)({target.Expr}), (Dn2CppObject*)({dependent.Expr}))");
            return;
        }
        // new DefaultInterpolatedStringHandler(int literalLength, int formattedCount,
        // [IFormatProvider], [Span<char> initialBuffer]) — DISH is an intrinsic value
        // type (the growable Dn2CppISB buffer). The usual `$"..."` lowering constructs it
        // with `ldloca + call.ctor` (handled in EmitIntrinsic), but Roslyn emits a
        // value-type `newobj` for some shapes — notably an interpolated string inside a
        // `try` block — and then `stloc`s the value into a temp. Push the constructed ISB
        // value, mirroring the TimeSpan/DateTime value-type newobj path; the subsequent
        // ldloca + AppendLiteral/AppendFormatted/ToStringAndClear calls run on its address
        // exactly as in the ldloca-first path. Extra provider/initialBuffer args are popped
        // and discarded (we keep InvariantCulture and own the buffer). DISH is
        // intrinsic so reachability already skips its ctor body (ResolveCallTarget→null).
        if (NewobjTypeName(handle) == "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var dsig = DecodeCtorSignature(handle);
            for (int i = dsig.ParameterTypes.Length - 1; i >= 2; i--)
                Pop(); // provider and/or initialBuffer span
            var formattedCount = Pop();
            var literalLength = Pop();
            Push(StackKind.Struct, "Dn2CppISB",
                $"dn2cpp_isb_new({literalLength.Expr}, {formattedCount.Expr})");
            return;
        }
        // new StringBuilder.AppendInterpolatedStringHandler(int literalLength,
        // int formattedCount, StringBuilder sb [, IFormatProvider]) — the value-type
        // newobj form (the common `ldloca + call.ctor` form is handled in EmitIntrinsic).
        // The handler is modeled as the underlying StringBuilder pointer, so the value it
        // pushes IS the builder; the counts/provider are advisory. BOTH names must be
        // matched or the widened token-kind gate is dead: NewobjTypeName answers the bare
        // nested name for the MemberRef mouth (its TypeRef carries no namespace) and the
        // enclosing-qualified nested dispatch name for the MethodDef mouth. The handler is
        // intrinsic-mapped by the NESTED key ("System.Text.StringBuilder",
        // "AppendInterpolatedStringHandler"), so the cut that obliges the route is
        // Compilation.Reach's IntrinsicCppName guard, not the s_intrinsicTypes name table.
        if (NewobjTypeName(handle) is "AppendInterpolatedStringHandler"
                or "System.Text.StringBuilder+AppendInterpolatedStringHandler"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var hsig = DecodeCtorSignature(handle);
            for (int i = hsig.ParameterTypes.Length - 1; i >= 3; i--)
                Pop(); // provider
            string sbArg = Cast(Pop(), "Dn2CppStringBuilder*"); // the StringBuilder
            Pop(); // formattedCount
            Pop(); // literalLength
            Push(StackKind.Struct, "Dn2CppStringBuilder*", sbArg);
            return;
        }
        // new object — a bare allocation (commonly a `lock` sentinel). Object is
        // intrinsic, so allocate the header and stamp the runtime object type-info; no
        // ctor body to run.
        // new Lock (.NET 9+) is the same: Lock is intrinsic (a minimal reference
        // object on the single-threaded model), so allocate a header object.
        if (NewobjTypeName(handle) is "System.Object" or "System.Threading.Lock")
        {
            string o = NewTemp("Dn2CppObject*");
            Emit($"{o} = (Dn2CppObject*)dn2cpp_alloc(sizeof(Dn2CppObject));");
            Emit($"{o}->type = &dn2cpp_object_type;");
            _stack.Add(new StackEntry(o, StackKind.Ref, "Dn2CppObject*"));
            return;
        }
        // new System.Exception([message[, inner]]) — Exception itself is an opaque
        // intrinsic type, so (unlike a derived exception) it has no resolvable ctor and
        // would otherwise fall through to the "external type" throw. Allocate the same
        // message-carrying object as the derived-exception interception,
        // stamped with the runtime dn2cpp_exception_type. The derived types route through
        // the post-ctor-resolution interception below.
        //
        // This arm is deliberately MemberReference-ONLY, the one exception to the widened
        // token-kind gate across this chain: a MethodDef-shaped `newobj
        // System.Exception::.ctor` always resolves and falls through to
        // Compilation.IsInterceptedExceptionCtor's OPAQUE branch, which emits the SAME
        // dn2cpp_exception_new call over the same message/inner recovery. Contrast the
        // AggregateException arm below, where that fall-through is NOT equivalent.
        if (NewobjTypeName(handle) == "System.Exception" && handle.Kind == HandleKind.MemberReference)
        {
            var exSig = _reader.GetMemberReference((MemberReferenceHandle)handle)
                .DecodeMethodSignature(_c.SigProvider, _method.Context);
            int msgIdx = Compilation.ExceptionMessageArgIndex(exSig.ParameterTypes);
            int innerIdx = Compilation.ExceptionInnerArgIndex(exSig.ParameterTypes);
            string message = "nullptr";
            string inner = "nullptr";
            for (int i = exSig.ParameterTypes.Length - 1; i >= 0; i--)
            {
                var a = Pop();
                if (i == msgIdx)
                    message = Cast(a, "Dn2CppString*");
                else if (i == innerIdx)
                    inner = Cast(a, "Dn2CppObject*");
            }
            string ex = NewTemp("Dn2CppObject*");
            Emit($"{ex} = dn2cpp_exception_new(&dn2cpp_exception_type, {message}, {inner});");
            _stack.Add(new StackEntry(ex, StackKind.Ref, "Dn2CppObject*"));
            return;
        }
        // new AggregateException(...) — AggregateException is an opaque intrinsic type (no
        // resolvable ctor), so it must be intercepted here (like System.Exception) before
        // the external-ctor throw. Build the runtime aggregate object — the larger
        // InnerExceptions-carrying layout the Parallel aggregation path also produces — so
        // AggregateException.InnerExceptions always reads a valid array (not the generic
        // message-only exception object). The (params) Exception[] ctor seeds
        // InnerExceptions from its array; the other shapes (parameterless /
        // IEnumerable<Exception> / (string[, inner])) build an empty aggregate — their
        // inner content is a carve-out (the Message text isn't modeled either), but the
        // object is always the correct size.
        //
        // BOTH ctor token forms are accepted, UNLIKE the System.Exception arm directly
        // above — the two arms look interchangeable and are not. Declining here would fall
        // through to Compilation.IsInterceptedExceptionCtor's OPAQUE branch, which is a
        // different lowering in two ways, each fatal:
        //
        //   1. It sizes the allocation by `ti->instanceSize`, and the hand-written
        //      dn2cpp_aggregate_exception_type literal (runtime/core/dn2cpp_exceptions.cpp)
        //      has instanceSize 0, so the object is only sizeof(Dn2CppExceptionObject) —
        //      the `innerExceptions` slot sits PAST its end and get_InnerExceptions reads
        //      whatever follows.
        //   2. It recovers only message/inner, so the (params) Exception[] operand is
        //      popped and dropped and the aggregate loses its inners.
        if (NewobjTypeName(handle) == "System.AggregateException"
            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
        {
            var aggSig = DecodeCtorSignature(handle);
            string innerArr = "nullptr";
            for (int i = aggSig.ParameterTypes.Length - 1; i >= 0; i--)
            {
                var a = Pop();
                if (aggSig.ParameterTypes.Length == 1 && aggSig.ParameterTypes[i] is { Kind: TypeKind.SZArray })
                    innerArr = Cast(a, "Dn2CppArrayRef*");
            }
            string agg = NewTemp("Dn2CppObject*");
            Emit($"{agg} = dn2cpp_aggregate_exception_new({innerArr});");
            _stack.Add(new StackEntry(agg, StackKind.Ref, "Dn2CppObject*",
                StaticType: TypeDesc.MakeExternal("System.AggregateException")));
            return;
        }
        // new string(...) from char sources — String is intrinsic, so its ctor isn't a
        // transpilable method. Build a Dn2CppString that copies the chars (the source may
        // alias another string or a mutable array). The newobj is a MemberRef from
        // an app assembly but a MethodDef when it sits in CoreLib itself (e.g. the
        // ReadOnlySpan<char>.ToString body's `new string(this)`).
        if (NewobjTypeName(handle) == "System.String")
        {
            var sps = DecodeCtorSignature(handle).ParameterTypes;
            // new string(char c, int count)
            if (sps is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char },
                        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }])
            {
                var count = Pop();
                var ch = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_repeat_char((char16_t)({ch.Expr}), {count.Expr})");
                return;
            }
            // new string(ReadOnlySpan<char>) / new string(Span<char>)
            if (sps is [{ Kind: TypeKind.Class, Class: { } spc }]
                && _c.GenericDefFullName(spc) is "System.ReadOnlySpan" or "System.Span")
            {
                string sv = SpanValue(Pop(), CppTypes.Of(sps[0]));
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_from_chars((char16_t*){sv}.f__reference, {sv}.f__length)");
                return;
            }
            // new string(char[]) / new string(char[], int start, int length)
            if (sps is [{ Kind: TypeKind.SZArray }] or [{ Kind: TypeKind.SZArray }, _, _])
            {
                string start = "0";
                string length = null!;
                if (sps.Length == 3)
                {
                    length = Pop().Expr;
                    start = Pop().Expr;
                }
                var arr = Pop();
                if (sps.Length == 1)
                    length = $"((Dn2CppArray*)({arr.Expr}))->length";
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_from_chars((char16_t*)dn2cpp_elem_addr((Dn2CppArrayN*)({arr.Expr}), {start}), {length})");
                return;
            }
            // new string(char* value) — a single NUL-terminated pointer (no explicit
            // length). Reached on Windows by Environment.GetOSVersion's
            // `fixed (char* c = osvi.szCSDVersion) { ... new string(c); }` over a
            // fixed-size native struct field; a null pointer yields Empty (the real
            // ctor's documented behavior for this extern/VM-implemented overload).
            // The pointee decides the DECODE, so char* and sbyte* take separate
            // helpers rather than one scan: char* is UTF-16, sbyte* is narrow bytes
            // through the system ANSI code page (the arm below). Reading either as
            // the other garbles non-ASCII text silently — no exception, no diagnostic.
            if (sps is [{ Kind: TypeKind.Pointer,
                          Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } }])
            {
                var ptr = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_from_wcs((const char16_t*)({ptr.Expr}))");
                return;
            }
            // new string(sbyte* value) — a NUL-terminated run of narrow bytes, decoded
            // through the system ANSI code page (dn2cpp_string_from_mbs -> the ANSI PAL
            // seam: CP_ACP on Windows, UTF-8 on POSIX), which is what the real ctor
            // does — NOT Encoding.Default, which .NET Core reports as UTF-8 even where
            // the ACP is not (probe-confirmed on a CP932 host; see dn2cpp.h). Reached on
            // Windows by NameResolutionPal.GetHostName, stringifying the buffer
            // gethostname(2) filled. The (sbyte*, int, int[, Encoding]) siblings still
            // fall through to the loud NotSupportedException below.
            if (sps is [{ Kind: TypeKind.Pointer,
                          Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.SByte } }])
            {
                var ptr = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_string_from_mbs((const char*)({ptr.Expr}))");
                return;
            }
            // new string(char* value, int startIndex, int length) — copy
            // `length` UTF-16 code units starting at `value + startIndex` (the unsafe-lane
            // pointer model represents `char*` as void*). Reached by SRM's
            // MemoryBlock.PeekUtf16 (the little-endian fast path: a char* into the metadata
            // blob, startIndex 0, byteCount/2 units). The copy is verbatim — embedded NUL
            // and lone surrogates are preserved (length is explicit, not NUL-terminated),
            // matching the real ctor (verified by real.NET probe).
            if (sps is [{ Kind: TypeKind.Pointer,
                          Element: { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } },
                        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }])
            {
                var length = Pop();
                var start = Pop();
                var ptr = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_from_chars((char16_t*)({ptr.Expr}) + ({start.Expr}), {length.Expr})");
                return;
            }
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: new string(...) overload "
                + $"with {sps.Length} param(s) is not supported yet (span<char> / char[] / (char,count)) "
                + $"[{string.Join(", ", sps.Select(p => p.Kind + (p.Kind == TypeKind.Class ? ":" + _c.GenericDefFullName(p.Class!) : "")))}]");
        }

        // `new <RuntimeExceptionType>(...)` in a build without the CoreLib IL:
        // the ctor MemberReference has nothing to resolve against, but the type
        // is one of the runtime-raised exception types whose shared runtime
        // handle exists unconditionally, so the newobj intercepts by name into
        // the same uniform dn2cpp_exception_new object the resolved path
        // (below) builds — Message / innerException recovered from the exact
        // (string) / (string, Exception) ctor shapes, null otherwise.
        if (handle.Kind == HandleKind.MemberReference
            && CoreIntrinsics.RuntimeExceptionTypeInfo(NewobjTypeName(handle)) is { } runtimeExTi
            && _c.TryResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context) is null)
        {
            var reps = _reader.GetMemberReference((MemberReferenceHandle)handle)
                .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes;
            int rMsgIdx = Compilation.ExceptionMessageArgIndex(reps);
            int rInnerIdx = Compilation.ExceptionInnerArgIndex(reps);
            string rMessage = "nullptr";
            string rInner = "nullptr";
            for (int i = reps.Length - 1; i >= 0; i--)
            {
                var a = Pop();
                if (i == rMsgIdx)
                    rMessage = Cast(a, "Dn2CppString*");
                else if (i == rInnerIdx)
                    rInner = Cast(a, "Dn2CppObject*");
            }
            string rex = NewTemp("Dn2CppObject*");
            Emit($"{rex} = dn2cpp_exception_new({runtimeExTi}, {rMessage}, {rInner});");
            _stack.Add(new StackEntry(rex, StackKind.Ref, "Dn2CppObject*",
                StaticType: TypeDesc.MakeExternal(NewobjTypeName(handle))));
            return;
        }

        // `new <dynamic-codegen-surface type>(...)` (DynamicMethod, ILGenerator,
        // ...): the ctor body is cut at reachability, so resolving and
        // DirectCall-ing it would name a symbol that never exists. Intercepted
        // by name BEFORE resolution — this also covers configs where the
        // declaring assembly isn't loaded at all (the MemberReference resolves
        // to nothing) — dropping the args and throwing the same catchable
        // PlatformNotSupportedException the call-site lowering emits. The
        // placeholder pushed after the [[noreturn]] throw is unreachable and
        // only keeps the eval stack typed. Surface types are never value types,
        // so the object shape is a plain reference.
        if (CoreIntrinsics.IsDynamicCodegenType(NewobjTypeName(handle)))
        {
            var dcps = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            for (int i = dcps.Length - 1; i >= 0; i--)
                Pop();
            Emit($"dn2cpp_throw_platform_not_supported(\"{NewobjTypeName(handle)}..ctor"
                + " requires dynamic code generation\");");
            _stack.Add(new StackEntry("nullptr", StackKind.Ref, "Dn2CppObject*",
                StaticType: TypeDesc.MakeExternal(NewobjTypeName(handle))));
            return;
        }

        // `new Socket(...)` / `new NetworkStream(...)` / `new SocketAsyncEventArgs()`: the
        // ctor body is cut at reachability (Compilation.IsAbsentNetworkPalMember), so
        // resolving and DirectCall-ing it would name a symbol that never exists. Same
        // pre-resolution, name-keyed shape as the dynamic-codegen arm above and for the same
        // two reasons — it also covers a config where the declaring assembly is not loaded —
        // and it throws the sentence the call site throws. Only the TYPE test is needed here:
        // System.Net.Dns is static, and a nested socket type's ctor is reachable only from a
        // member of its declaring type, which the cut already deleted. Surface types are
        // never value types, so the object shape is a plain reference.
        if (CoreIntrinsics.IsAbsentNetworkPalType(NewobjTypeName(handle)))
        {
            var nps = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes
                : ResolveMethodDef((MethodDefinitionHandle)handle).Signature.ParameterTypes;
            for (int i = nps.Length - 1; i >= 0; i--)
                Pop();
            Emit("dn2cpp_throw_platform_not_supported(\""
                + Compilation.AbsentNetworkPalThrowMessage(NewobjTypeName(handle), ".ctor") + "\");");
            _stack.Add(new StackEntry("nullptr", StackKind.Ref, "Dn2CppObject*",
                StaticType: TypeDesc.MakeExternal(NewobjTypeName(handle))));
            return;
        }

        MethodInfo ctor = handle.Kind switch
        {
            HandleKind.MethodDefinition => ResolveMethodDef((MethodDefinitionHandle)handle),
            HandleKind.MemberReference when _reader.GetMemberReference((MemberReferenceHandle)handle).Parent.Kind == HandleKind.TypeSpecification
                => _c.ResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context),
            // A non-generic ctor in another loaded assembly — e.g. an iterator's
            // `throw new NotSupportedException(...)` where the exception's IL
            // lives in the CoreLib passed with -r.
            HandleKind.MemberReference when _c.TryResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context) is { } real
                => real,
            _ => throw new NotSupportedException(
                $"newobj of external type {NewobjTypeName(handle)} is not supported yet"),
        };
        var cls = ctor.DeclaringClass;

        if (!cls.IsBeforeFieldInit)
            EnsureCctorBefore(cls);

        if (_intrinsics is not null && _intrinsics.TryEmitNewobj(this, ctor))
            return;

        // A same-assembly `new MyTask(...)` on an adopted custom async task type. `.ctor` is
        // outside the mapped contract and the MemberRef pre-scan cannot see a MethodDef, so
        // this is the fail-loud hole the adoption owes: the type is an empty intrinsic shell
        // whose ctor body reachability already cut, and both tails below would call it.
        if (_c.AdoptedAsyncKey(cls) is { } adoptedKey)
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: newobj {cls.FullName}"
                + $"{SigArgs(ctor.Signature)} — {cls.FullName} is a custom async task type, which "
                + $"dn2cpp models by adopting it into its intrinsic Task family (as {adoptedKey}) "
                + "rather than transpiling the library's own scheduler and object-pool machinery. "
                + "Only the members the C# compiler and the await pattern require are mapped; a "
                + $"constructor is not one of them. Remedy: --no-adopt-async {cls.Module.AssemblyName}, "
                + "so the library's real IL transpiles instead");

        // `new <ExceptionType>(...)` for any type whose base chain reaches
        // System.Exception. Two forms:
        //
        //   1. System.Exception itself and runtime-raised BCL exceptions (Overflow,
        //      ArgumentException, ...) carry no field beyond the base message/inner
        //      slots in our fieldless-in-our-model Exception design. Use the uniform
        //      dn2cpp_exception_new allocator — the ctor body has nothing to run.
        //
        //   2. A user-defined derived exception may declare its own instance fields
        //      (e.g. ZlibException.Result). Allocate sizeof(derived struct) so those
        //      fields have storage past the base Dn2CppExceptionObject prefix, seed
        //      the type/message/inner slots inline, then DirectCall the derived ctor
        //      body so its `Result = code;` writes land. The CppEmitter roots each
        //      such derived struct at Dn2CppExceptionObject so the prefix layout
        //      matches what dn2cpp_exception_message / _inner reinterpret_cast onto.
        //
        // ALL ctor shapes are intercepted so every exception object shares the
        // message-carrying prefix (get_Message never reads past a plain struct); the
        // message + inner strings are recovered for the shapes ExceptionMessageArgIndex
        // recognizes, null otherwise.
        if (Compilation.IsInterceptedExceptionCtor(cls, ctor.Signature.ParameterTypes))
        {
            TaintIfCanonical(cls, "newobj");
            var eps = ctor.Signature.ParameterTypes;
            int msgIdx = Compilation.ExceptionMessageArgIndex(eps);
            int innerIdx = Compilation.ExceptionInnerArgIndex(eps);
            // Pop each arg once for both forms: keep the full arg list for a derived
            // ctor's DirectCall, and capture the message / inner casts for the OPAQUE
            // path only (System.Exception / AggregateException, whose ctor body never
            // runs). A derived exception seeds neither here — its real ctor chain runs
            // and the base System.Exception::.ctor intrinsic stores them (see
            // MethodCompiler.EmitIntrinsic.Numbers.cs), which fixes the Argument* family
            // the positional recovery misread.
            var ctorArgExprs = new string[eps.Length];
            string message = "nullptr";
            string inner = "nullptr";
            for (int i = eps.Length - 1; i >= 0; i--)
            {
                var a = Pop();
                ctorArgExprs[i] = Cast(a, CppTypes.Of(eps[i]));
                if (i == msgIdx)
                    message = Cast(a, "Dn2CppString*");
                else if (i == innerIdx)
                    inner = Cast(a, "Dn2CppObject*");
            }
            // An OPAQUE exception intrinsic — System.Exception itself and
            // System.AggregateException — has no emitted layout to allocate: its struct is
            // not even rooted at Dn2CppExceptionObject (Exception) or is a runtime object
            // whose extra state the runtime, not the emitted struct, describes
            // (AggregateException.InnerExceptions). The uniform allocator IS its layout,
            // and its ctor body has nothing we model to write.
            if (CoreIntrinsics.IsIntrinsicType(cls.FullName))
            {
                string opaqueTi = CoreIntrinsics.RuntimeExceptionTypeInfo(cls.FullName) ?? "&dn2cpp_exception_type";
                string e = NewTemp("Dn2CppObject*");
                Emit($"{e} = dn2cpp_exception_new({opaqueTi}, {message}, {inner});");
                _stack.Add(new StackEntry(e, StackKind.Ref, "Dn2CppObject*",
                    StaticType: TypeDesc.MakeClass(cls)));
                return;
            }
            // Every other exception type — including the BCL ones the runtime also raises
            // (ArgumentNullException, FormatException, …) — is allocated at its emitted size
            // and constructed by its REAL ctor, so a field the type declares
            // (ArgumentException._paramName) has storage and a value. The stamped handle is
            // CppTypeInfoName, which for those types is the runtime's own handle (carrying
            // the emitted vtable + size by then, see EmitTypeBinds) — so the object is the
            // same type to catch/isinst/typeof as one the runtime raised, and a callvirt of
            // a virtual it declares finds a slot. The alloc is zero-filled, so message/inner
            // start null and the ctor chain fills them through the base
            // System.Exception::.ctor intrinsic. No positional seeding here: it would
            // misread ArgumentNullException(paramName) as a (message) shape, whereas the
            // ctor chain lands the BCL-computed message whatever the derived shape.
            string exTi = "&" + cls.CppTypeInfoName;
            string derived = NewTemp(cls.CppStructName + "*");
            Emit($"{derived} = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
            Emit($"((Dn2CppObject*){derived})->type = {exTi};");
            // Seed System.Exception's base HResult default (COR_E_EXCEPTION). The alloc
            // is zero-filled, and dn2cpp's base System.Exception::.ctor intrinsic (unlike
            // real .NET's, which does the `_HResult = COR_E_EXCEPTION` field write) only
            // stores message/inner — so without this a derived exception that never calls
            // set_HResult would read HResult 0 instead of the base default. The derived
            // ctor chain's own set_HResult (e.g. ArgumentException's COR_E_ARGUMENT), run
            // by the DirectCall below, overwrites it with the per-type value.
            Emit($"((Dn2CppExceptionObject*){derived})->hresult = (int32_t)0x80131500;");
            if (Compilation.EffectiveFinalize(cls) is { } finD && _c.Reachable.Contains(finD))
                Emit($"dn2cpp_register_finalizer((Dn2CppObject*){derived});");
            var derivedCallArgs = new List<string> { derived };
            derivedCallArgs.AddRange(ctorArgExprs);
            Emit($"{DirectCall(ctor, derivedCallArgs)};");
            _stack.Add(new StackEntry(derived, StackKind.Ref, cls.CppStructName + "*",
                StaticType: TypeDesc.MakeClass(cls)));
            return;
        }

        if (cls.IsDelegate)
        {
            // The delegate object is stamped with its class's type-info.
            TaintIfCanonical(cls, "newobj");
            // A delegate created for an intrinsic consumer (e.g. Comparison<int>
            // newobj'd for Array.Sort, whose intrinsic body never names the delegate
            // in a reachable signature) enters the emit set through no other edge,
            // leaving its struct / type-info / dginvoke undeclared. Note it so the
            // delegate layout + invoker are emitted. Already-emitted delegates
            // (named in a reachable signature) are unaffected — ComputeEmitted wins.
            NoteReferencedType(cls);
            var fnPtr = Pop();
            var target = Pop();
            string dg = NewTemp(cls.CppStructName + "*");
            Emit($"{dg} = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
            Emit($"((Dn2CppObject*){dg})->type = &{cls.CppTypeInfoName};");
            Emit($"{dg}->f_target = {Cast(target, "Dn2CppObject*")};");
            Emit($"{dg}->f_method = {Cast(fnPtr, "void*")};");
            _stack.Add(new StackEntry(dg, StackKind.Ref, cls.CppStructName + "*"));
            return;
        }

        // A class the reachability cut deletes WHOLESALE (Gen2GcCallback; the compiled-Regex
        // classes) has no .ctor body either, so a newobj of it would call a symbol nothing
        // emitted — the same C++ link error the call path rules out, arriving through the
        // other door. Both are dead today, which is precisely why opening a path to one must
        // fail here rather than at link time. The name-keyed families are unaffected:
        // ".ctor" is in none of their name sets.
        if (CoreIntrinsics.LoweredRuntimePrimitive(ctor.DeclaringClass.FullName, ctor.Name))
        {
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: newobj "
                + $"{ctor.DeclaringClass.FullName} — the reachability cut deletes this class "
                + "wholesale (CoreIntrinsics.LoweredRuntimePrimitive), so its constructor has no "
                + "emitted body to call.");
        }

        var ps = ctor.Signature.ParameterTypes;
        var ctorArgs = new string[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
            ctorArgs[i] = Cast(Pop(), CppTypes.Of(ps[i]));

        if (cls.IsValueType)
        {
            // No type-info is baked (the struct lives unboxed), so a
            // placeholder-bearing satellite struct ctor is a plain direct-call
            // edge — shareable when the ctor body itself is.
            string val = NewTemp(cls.CppStructName);
            Emit($"{val} = {cls.CppStructName}{{}};");
            var vargs = new List<string> { $"&{val}" };
            vargs.AddRange(ctorArgs);
            Emit($"{DirectCall(ctor, vargs)};");
            _stack.Add(new StackEntry(val, StackKind.Struct, cls.CppStructName));
            return;
        }

        // A reference allocation stamps the class's own type-info — a shared
        // body allocating an owner-group satellite (KeyCollection and friends)
        // stamps the real instantiation's type-info out of an rgctx slot keyed
        // on the newobj ctor token instead. The size is group-uniform: the
        // alias structs share the canonical layout, so sizeof the canonical
        // struct is every member's allocation size.
        string allocTi = SharedTrial && Compilation.ContainsCanonPlaceholder(cls)
            ? "(const Dn2CppTypeInfo*)"
              + RgctxSlotAccess(RgctxSlotKind.ClassAlloc, _callSiteToken, "newobj", cls.FullName)
            : "&" + cls.CppTypeInfoName;
        string obj = NewTemp(cls.CppStructName + "*");
        Emit($"{obj} = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
        Emit($"((Dn2CppObject*){obj})->type = {allocTi};");
        // Register with Boehm's finalizer queue only for a type whose effective
        // Finalize is actually reached — a program with no finalizable type never
        // touches dn2cpp_register_finalizer, so it never pays for the (lazily-started)
        // finalizer thread. Gating on Reachable, not merely on a non-null
        // EffectiveFinalize, is what keeps a subclass of an OPAQUE INTRINSIC BASE with
        // a real Finalize (EventSource: `~EventSource()` exists but is intrinsic-cut, so
        // never reached) from registering against a null type-info slot and starting the
        // thread for nothing. For an ordinary transpiled `~Foo()`,
        // Compilation.ReachAllocatedType has already put it in Reachable, so this is the
        // same set the type-info finalize slot is emitted from (CppEmitter). Registration
        // happens BEFORE the ctor runs, matching .NET: an object is finalizable from the
        // moment it is allocated, so a throwing ctor still leaves the (partially
        // constructed) object to be finalized.
        if (Compilation.EffectiveFinalize(cls) is { } fin && _c.Reachable.Contains(fin))
            Emit($"dn2cpp_register_finalizer((Dn2CppObject*){obj});");
        var allArgs = new List<string> { obj };
        allArgs.AddRange(ctorArgs);
        Emit($"{DirectCall(ctor, allArgs)};");
        // Thread the constructed type as StaticType so a `new List<T>{...}` passed
        // straight into string.Join/Concat (no local binding) is recognised by
        // TryListBacking — the same lift as for call results / fields.
        _stack.Add(new StackEntry(obj, StackKind.Ref, cls.CppStructName + "*", StaticType: TypeDesc.MakeClass(cls)));
    }

    /// <summary>The cold-task <c>new Task(...)</c> / <c>new Task&lt;TResult&gt;(...)</c>
    /// lowering: pop the delegate (plus its <c>object? state</c> argument for the
    /// Action&lt;object?&gt; form) and push a pending Dn2CppTask* carrying the unstarted
    /// work — Task.Start / RunSynchronously claims and runs it. <paramref name="tres"/>
    /// is Task&lt;TResult&gt;'s result type (null for the non-generic Task); it picks the
    /// result-kind thunk exactly like Task.Run&lt;TResult&gt;. Trailing CancellationToken
    /// / TaskCreationOptions arguments are scheduling hints only — popped and ignored,
    /// like TaskFactory.StartNew's (the delegate always runs on the real pool or, for
    /// RunSynchronously, the calling thread).</summary>
    private void EmitColdTaskCtor(TypeDesc? tres, System.Collections.Immutable.ImmutableArray<TypeDesc> ps,
                                  TypeDesc taskType)
    {
        if (ps.Length == 0 || ps[0] is not { Kind: TypeKind.Class, Class: { } delCls })
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: new Task ctor shape is not supported " +
                "(expected a leading Action/Func delegate)");
        // Action<object?> (arity 1) / Func<object?, TResult> (arity 2) mark the
        // state-carrying forms; plain Action (arity 0) / Func<TResult> (arity 1)
        // the stateless ones.
        bool hasState = delCls.Context.TypeArgs.Length == (tres is null ? 1 : 2);
        int kept = hasState ? 2 : 1; // delegate [+ state]
        for (int i = ps.Length - 1; i >= kept; i--)
            Pop(); // CancellationToken / TaskCreationOptions hints
        if (hasState)
        {
            if (tres is not null)
                throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: new Task<{tres}> with a state " +
                    "argument (Func<object?, TResult>, state) is not supported yet");
            var state = Pop(); // object? state (passed to the delegate)
            var sdel = Pop();  // Action<object?>
            PushStampedTask(
                $"dn2cpp_task_cold_void_state((Dn2CppObject*)({sdel.Expr}), {Cast(state, "Dn2CppObject*")})",
                taskType);
            return;
        }
        // A value-type result rides the boxing trampoline (TaskStructResultThunk) stored as
        // the cold task's thunk — the newobj mirror of Task.Run<TStruct>. Start() /
        // RunSynchronously() run it, and .Result / await read the boxed struct back.
        if (tres is not null && CppTypes.KindOf(tres) == StackKind.Struct)
        {
            var sdel = Pop(); // Func<TResult>
            PushStampedTask(
                $"dn2cpp_task_cold_struct((Dn2CppObject*)({sdel.Expr}), {TaskStructResultThunk(CppTypes.Of(tres))})",
                taskType);
            return;
        }
        string fn = tres is null ? "dn2cpp_task_cold_void" : CppTypes.KindOf(tres) switch
        {
            StackKind.I8 => "dn2cpp_task_cold_i8",
            StackKind.R8 => tres is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }
                ? "dn2cpp_task_cold_r4" : "dn2cpp_task_cold_r8",
            StackKind.I4 => "dn2cpp_task_cold_i4",
            StackKind.Ref => "dn2cpp_task_cold_ref",
            _ => throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: new Task<{tres}> " +
                "result kind not supported yet (int/long/float/double/reference only)"),
        };
        var del = Pop(); // Action or Func<TResult>
        PushStampedTask($"{fn}((Dn2CppObject*)({del.Expr}))", taskType);
    }

    /// <summary>The IValueTaskSource-backed <c>new ValueTask(&lt;T&gt;)(source,
    /// token)</c> lowering: pop (source, token), resolve the closed interface's
    /// GetStatus/GetResult/OnCompleted implementations through the source's interface table,
    /// and push a {task} struct over the pending task dn2cpp_vts_task returns
    /// (completed by a runtime continuation registered via OnCompleted; the
    /// result kind tells the runtime how to read GetResult's return into the
    /// task's 8-byte result slot). Reach twin:
    /// <c>Compilation.ReachValueTaskSourceBridge</c>.</summary>
    private void EmitValueTaskSourceCtor(ClassInfo itf) =>
        Push(StackKind.Struct, "Dn2CppTaskAwaiter", ValueTaskSourceAwaiter(itf));

    /// <summary>The shared body of <see cref="EmitValueTaskSourceCtor"/>: pops (source,
    /// token) and returns the {task}-struct expression, leaving the caller to place it —
    /// the <c>newobj</c> form pushes it, the <c>ldloca</c> + <c>call .ctor</c> form
    /// stores it through the receiver address.</summary>
    private string ValueTaskSourceAwaiter(ClassInfo itf)
    {
        if (_c.ValueTaskSourceMembers(itf) is not { } members)
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: new ValueTask(source, token) over " +
                $"{itf.FullName} without the GetResult/OnCompleted pair is not supported");
        var rt = members.GetResult.Signature.ReturnType;
        TaintIfCanonAnyValue(rt, "IValueTaskSource result");
        int kind = rt.IsVoid ? 0 : CppTypes.KindOf(rt) switch
        {
            StackKind.I4 => 1,
            StackKind.I8 => 2,
            StackKind.Ref => 3,
            StackKind.Struct => 4,
            _ => throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: IValueTaskSource<{rt}> result kind " +
                "is not supported (void/int32/int64/reference/struct results only)"),
        };
        var actionCls = members.OnCompleted.Signature.ParameterTypes[0].Class!;
        _c.NoteReferencedType(itf);
        _c.NoteReferencedType(actionCls);
        EmitCanceledExcRegistration();
        var token = Pop();  // short token
        var src = Pop();    // IValueTaskSource(<T>) receiver
        string vts = NewTemp("Dn2CppObject*");
        Emit($"{vts} = {Cast(src, "Dn2CppObject*")};");
        string slots = NewTemp("const void**");
        Emit($"{slots} = dn2cpp_resolve_interface({vts}->type, &{itf.CppTypeInfoName});");
        TypeDesc taskType = rt.IsVoid
            ? Comp.FindClassByFullName("System.Threading.Tasks.Task") is { } task
                ? TypeDesc.MakeClass(task)
                : throw new InvalidOperationException("System.Threading.Tasks.Task is not loaded")
            : TaskTypeForResult(rt);
        string structResult = kind == 4
            ? $"+[](const void* __fn, Dn2CppObject* __src, int16_t __version) -> uint64_t {{ " +
              $"{CppTypes.Of(rt)} __result = reinterpret_cast<{CppTypes.Of(rt)} (*)(Dn2CppObject*, int32_t)>(" +
              $"const_cast<void*>(__fn))(__src, __version); " +
              $"return (uint64_t)(uintptr_t)dn2cpp_struct_result_box(&__result, (int32_t)sizeof({CppTypes.Of(rt)})); }}"
            : "nullptr";
        string bridge = $"dn2cpp_vts_task({vts}, (int16_t)({token.Expr}), " +
               $"{slots}[{members.GetStatus.VtableSlot}], {slots}[{members.GetResult.VtableSlot}], " +
               $"{slots}[{members.OnCompleted.VtableSlot}], " +
               $"&{actionCls.CppTypeInfoName}, {kind}, {structResult})";
        return $"Dn2CppTaskAwaiter{{ {StampTask(bridge, taskType)} }}";
    }

    /// <summary>Whether a parameter names <c>System.Diagnostics.StackFrame</c> (loaded
    /// as a Class with CoreLib on the reference set; External without it).</summary>
    private static bool IsStackFrameParam(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.Diagnostics.StackFrame" }
            or { Kind: TypeKind.External, ExternalName: "System.Diagnostics.StackFrame" };

    /// <summary>Whether a parameter names <c>System.Exception</c> exactly (the declared
    /// param type of the StackTrace(Exception, ...) ctors — never a derived type).</summary>
    private static bool IsExceptionParam(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.FullName: "System.Exception" }
            or { Kind: TypeKind.External, ExternalName: "System.Exception" };

    /// <summary>The precise <c>StackFrame[]</c> array handle a StackTrace's frame array
    /// is stamped with, so <c>GetFrames()</c> hands back an exactly-typed array (and
    /// noting the element emits the symbol). Falls back to the shared reference-array
    /// handle in the pathological case where the program names StackTrace without
    /// StackFrame being resolvable at all.</summary>
    private string StackFrameArrayTypeInfo()
    {
        if (_c.FindClassByFullName("System.Diagnostics.StackFrame") is not { } sfCls)
            return "&dn2cpp_array_ref_type";
        var elem = TypeDesc.MakeClass(sfCls);
        _c.NoteArrayElementType(elem);
        return PreciseArrayTypeInfoExpr(elem);
    }

    /// <summary>The stack-trace model's newobj lowering. Covers EVERY ctor of both
    /// families (the sibling-overload rule): an overload left out would fall through to a
    /// raw struct allocation plus a call to a ctor whose body reachability cut, failing at
    /// link time.
    ///
    /// <para>The current-stack forms — () / (bool) / (int) / (int, bool) — materialize the
    /// live shadow stack's frame names under <c>--shadow-stack</c> and degrade to the
    /// zero-frame trace flag-off. The StackTrace(Exception, ...) forms reuse the trace
    /// stamped into the exception at throw UNCONDITIONALLY of the flag, because that stamp
    /// exists flag-off too and ex.StackTrace already renders it — answering "unavailable"
    /// beside it is the silent inconsistency docs/ARCHITECTURE.md §4-B forbids.
    /// fNeedFileInfo is discarded (no debug map); skipFrames forwards to the runtime.</para>
    ///
    /// <para>The non-capturing ctors carry exactly what the caller gave them.
    /// StackTrace(IEnumerable&lt;StackFrame&gt;) is the one shape the runtime cannot serve
    /// (enumerating a managed sequence from C++) and is a request for a SPECIFIC trace, so
    /// it fails loud with a catchable PlatformNotSupportedException rather than silently
    /// reporting zero frames.</para></summary>
    private void EmitStackDiagnosticNewobj(bool isTrace, System.Collections.Immutable.ImmutableArray<TypeDesc> ps)
    {
        if (isTrace)
        {
            // StackTrace(StackFrame frame): a trace over the one supplied frame.
            if (ps is [{ } only] && IsStackFrameParam(only))
            {
                string f = Cast(Pop(), "Dn2CppObject*");
                Push(StackKind.Ref, "Dn2CppStackTrace*",
                    $"dn2cpp_stacktrace_of_frame({StackFrameArrayTypeInfo()}, {f})");
                return;
            }
            // StackTrace(IEnumerable<StackFrame> frames): the caller wants a specific
            // trace, and we cannot build it. Fail loud (see the summary).
            if (ps.Length == 1 && ps[0].Kind == TypeKind.Class && ps[0].Class?.GenericArity > 0)
            {
                Pop();
                Emit("dn2cpp_throw_platform_not_supported(\"System.Diagnostics.StackTrace"
                     + "(IEnumerable<StackFrame>) is not supported in AOT\");");
                Push(StackKind.Ref, "Dn2CppStackTrace*", "nullptr");
                return;
            }
            // The eight capturing overloads: [Exception][, int skipFrames][, bool
            // fNeedFileInfo] in every combination. Classified by parameter TYPES,
            // never by count (the overload rule in AGENTS.md — the two families
            // share arities), and classified FULLY before anything is popped: a
            // shape this table does not recognize must reach the fall-through
            // below with the operand stack untouched — a name-gated route may
            // decline a shape, never pop one wrongly.
            int at = 0;
            bool hasEx = ps.Length > at && IsExceptionParam(ps[at]);
            if (hasEx)
                at++;
            bool hasSkip = ps.Length > at
                && ps[at] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 };
            if (hasSkip)
                at++;
            bool hasNeedFileInfo = ps.Length > at
                && ps[at] is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean };
            if (hasNeedFileInfo)
                at++;
            if (at == ps.Length)
            {
                // Recognized: pop right-to-left. fNeedFileInfo is discarded (no debug
                // map to consult), skipFrames forwards, the exception forwards.
                if (hasNeedFileInfo)
                    Pop();
                string skip = hasSkip ? Cast(Pop(), "int32_t") : "0";
                if (hasEx)
                {
                    string ex = Cast(Pop(), "Dn2CppObject*");
                    Push(StackKind.Ref, "Dn2CppStackTrace*",
                        $"dn2cpp_stacktrace_of_exception({StackFrameArrayTypeInfo()}, {ex}, {skip})");
                }
                else
                {
                    Push(StackKind.Ref, "Dn2CppStackTrace*",
                        $"dn2cpp_stacktrace_new({StackFrameArrayTypeInfo()}, {skip})");
                }
                return;
            }
            // An unrecognized capturing shape (a future overload): keep it safe — pop
            // exactly what the signature declares and answer the current-stack capture
            // with no skip, the same degrade-not-dangle posture as the flag-off
            // zero-frame trace.
            for (int i = 0; i < ps.Length; i++)
                Pop();
            Push(StackKind.Ref, "Dn2CppStackTrace*",
                $"dn2cpp_stacktrace_new({StackFrameArrayTypeInfo()}, 0)");
            return;
        }
        // StackFrame(fileName, line) / (fileName, line, column): honest synthetic frames.
        if (ps.Length >= 2 && ps[0].IsString)
        {
            string col = ps.Length >= 3 ? Cast(Pop(), "int32_t") : "0";
            string line = Cast(Pop(), "int32_t");
            string file = Cast(Pop(), "Dn2CppString*");
            Push(StackKind.Ref, "Dn2CppStackFrame*", $"dn2cpp_stackframe_new({file}, {line}, {col})");
            return;
        }
        // StackFrame() / (bool) / (int) / (int, bool): every capturing overload — a real
        // object with nothing in it (no method, no file, no line).
        for (int i = 0; i < ps.Length; i++)
            Pop();
        Push(StackKind.Ref, "Dn2CppStackFrame*", "dn2cpp_stackframe_new(nullptr, 0, 0)");
    }

    /// <summary>Register a struct for P/Invoke field-by-field marshalling, and note the
    /// element of every <c>[MarshalAs(ByValArray)]</c> field so the emitter can name that
    /// element's precise <c>ti_arr_</c> handle in the copy-back.
    ///
    /// <para>The note has to happen HERE, in the body-compile phase, and not at the
    /// emitter's <c>MarshalOutField</c>: the marshal helpers are written by
    /// <c>EmitPInvokeMarshalStructs</c>, which runs BEFORE <c>EmitTypeInfos</c>, so at that
    /// point <see cref="CppEmitter.ArrayTypeInfoDeclared"/> cannot answer and the site has
    /// no way to note a new element either. Without the note the copy-back allocates through
    /// the untyped allocator and the freshly allocated array — which real .NET makes a
    /// genuine <c>T[]</c> — reports <c>System.Object[]</c>, or worse, carries a REF-array
    /// tag over packed value storage.</para>
    ///
    /// <para>A canonical placeholder element cannot reach here:
    /// <see cref="CppTypes.IsByValArrayField"/> requires a native ABI type for the element,
    /// which a placeholder has none of, so the field is not a ByValArray field at all and
    /// the emitter names no handle for it.</para></summary>
    private void NotePInvokeMarshalStruct(ClassInfo sc)
    {
        _c.PInvokeMarshalStructs.Add(sc);
        _c.NoteForceEmit(sc);
        foreach (var f in sc.Fields)
        {
            if (f.IsStatic || f.IsLiteral || !CppTypes.IsByValArrayField(f))
                continue;
            // The enumerable form, not the plain one: a precise handle without the SZArray
            // map is worse than the shared handle (see PrimArrayTypeInfoExpr).
            _c.NoteArrayEnumerableElement(f.Type.Element!);
        }
    }
}
