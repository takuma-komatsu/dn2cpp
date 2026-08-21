using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>Generic core-intrinsic lowerings replayable from the closed type
    /// arguments alone (no call-site MethodSpec token). Shared between the call
    /// path (<see cref="TranslateGenericIntrinsic"/>, which decodes the token) and
    /// the address-taken body synthesizer (<see cref="CompileCoreIntrinsicWrapper"/>,
    /// which drives it from the resolved instantiation's Context.MethodArgs —
    /// ldftn / delegate method group / delegate*&lt;...&gt; over e.g.
    /// <c>Array.Empty&lt;T&gt;</c>).</summary>
    private bool TryTokenFreeGenericIntrinsic(string declType, string name, TypeDesc[] methodArgs)
    {
        // Array.Empty<T> -> the per-element-type cached singleton (matches .NET's
        // EmptyArray<T>.Value: the same instance every call, no per-call allocation).
        if (declType == "System.Array" && name == "Empty")
        {
            EmitEmptyArray(methodArgs[0]);
            return true;
        }
        return false;
    }

    /// <summary>Emits a generic method on an intrinsic-mapped type (reached as a
    /// MethodSpec over a MemberRef) inline. The instantiation's type arguments
    /// come from the MethodSpec; we never touch the (stub) IL body.</summary>
    private void TranslateGenericIntrinsic(string declType, string name, MethodSpecificationHandle msh)
    {
        var methodArgs = Reader.GetMethodSpecification(msh)
            .DecodeSignature(Comp.SigProvider, Method.Context).ToArray();

        // EventSource.Write<T> — the self-describing write. A no-op, exactly like the
        // non-generic write surface next door (MethodCompiler.TryEmitEventSourceIntrinsic):
        // .NET's own Write<T> opens with `if (!IsEnabled()) return;`, and no listener can
        // attach in a native build, so returning without doing anything IS what real .NET
        // does here. It is spelled in this file only because the generic dispatch runs
        // before the intrinsic-type arm — the two arms are one lowering.
        if (declType == "System.Diagnostics.Tracing.EventSource" && name == "Write")
        {
            var wsig = MethodSpecSig(msh, methodArgs);
            for (int i = 0; i < wsig.ParameterTypes.Length; i++)
                Pop();  // eventName / EventSourceOptions / the payload — discarded
            if (wsig.Header.IsInstance)
                Pop();  // the provider receiver — never read
            return;
        }

        // Portable SIMD vector static helpers reached as MethodSpecs (Create<T>, Add<T>,
        // Equals<T>, As<TFrom,TTo>, WidenLower<...>, …). The element comes from the closed
        // signature's primary vector; lower to the software-vector runtime.
        if (declType is "System.Runtime.Intrinsics.Vector64" or "System.Runtime.Intrinsics.Vector128"
                or "System.Runtime.Intrinsics.Vector256" or "System.Runtime.Intrinsics.Vector512"
                or "System.Numerics.Vector")
        {
            if (name is "get_IsSupported" or "get_IsHardwareAccelerated")
            {
                Push(StackKind.I4, "int32_t", SimdHwAccelToken);
                return;
            }
            var vsig = MethodSpecSig(msh, methodArgs);
            if (TryVectorPrimary(vsig, out var vcpp, out var vw, out var velem))
            {
                if (TryEmitVectorOp(vcpp, vw, velem, name, vsig, methodArgs))
                    return;
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: vector helper {declType}::{name}<{methodArgs.Length}> is not supported");
            }
            // No software-vector operand/result: the element is non-primitive (e.g.
            // Vector<Int128>). The software-vector runtime is primitive-only, and such a
            // helper is only ever called on the dead, hardware-gated-off SIMD branch
            // (Vector.IsHardwareAccelerated folds to 0, so the scalar path always runs).
            // The declaring helper class is intrinsic, so its real body is never emitted —
            // drop the args and yield a default. The stub compiles but never executes.
            var nv = Comp.ResolveMethodSpec(Module, msh, Method.Context);
            PopArgs(nv, hasThis: !nv.IsStatic);
            if (!nv.Signature.ReturnType.IsVoid)
            {
                string rct = CppTypes.Of(nv.Signature.ReturnType);
                string zero = CppTypes.ZeroInitExpr(rct);
                Push(CppTypes.KindOf(nv.Signature.ReturnType), rct, zero);
            }
            return;
        }

        if (declType == "System.Runtime.CompilerServices.Unsafe"
            && TryUnsafeIntrinsic(name, methodArgs))
            return;

        // GC.AllocateUninitializedArray<T>(int length [, bool pinned]) -> new T[length].
        // The trailing `pinned` flag (when present) is ignored — dn2cpp's GC arrays are
        // already stable. The result is zero-initialized, which is a valid "uninitialized"
        // array; only the fill optimization is dropped.
        if (declType == "System.GC" && name == "AllocateUninitializedArray")
        {
            var csig = MethodSpecSig(msh, methodArgs);
            if (csig.ParameterTypes.Length == 2)
                Pop(); // the `pinned` flag
            var len = Pop();
            EmitNewarr(methodArgs[0], len.Expr);
            return;
        }

        // Math.ThrowMinMaxException<T>(min, max) — the [DoesNotReturn] throw helper
        // behind every Clamp overload's min > max validation. The intrinsic-mapped
        // Math/MathF overloads never reach it (dn2cpp_math_clamp validates inline),
        // but transpiled BCL bodies do (Half.Clamp/ClampNative). Same lowering as
        // the ThrowHelper closures: drop the bounds, raise the catchable
        // ArgumentException, keep the message-formatting IL out of the tree.
        if (declType == "System.Math" && name == "ThrowMinMaxException")
        {
            Pop(); // max
            Pop(); // min
            Emit("dn2cpp_throw_argument();");
            return;
        }

        // The view's generic blittable accessors over the mapped bytes (declared on the
        // UnmanagedMemoryAccessor base): Read<T>(pos, out T) / Write<T>(pos, ref T) (a
        // single value, memcpy) and ReadArray<T>/WriteArray<T>(pos, T[], offset, count)
        // (a bulk run). The real bodies route through SafeBuffer pointer ops; lower them
        // over the view's base pointer (T is blittable — T : struct). The receiver (the
        // accessor) is below the args on the IL stack.
        if (declType == "System.IO.UnmanagedMemoryAccessor")
        {
            var t = methodArgs[0];
            string st = CppTypes.StorageOf(t);
            switch (name)
            {
                case "Read": // Read<T>(long position, out T structure) -> void
                {
                    var outRef = Pop(); // out T -> a managed pointer to the destination
                    var pos = Pop();
                    var view = Pop();
                    Emit($"std::memcpy((void*)({outRef.Expr}), (const void*)(({MmvVal(view)}).addr + ({pos.Expr})), sizeof({st}));");
                    return;
                }
                case "Write": // Write<T>(long position, ref T structure) -> void
                {
                    var val = Pop(); // ref T -> a managed pointer to the value
                    var pos = Pop();
                    var view = Pop();
                    Emit($"std::memcpy((void*)(({MmvVal(view)}).addr + ({pos.Expr})), (const void*)({val.Expr}), sizeof({st}));");
                    return;
                }
                case "ReadArray": // ReadArray<T>(long position, T[] array, int offset, int count) -> int
                case "WriteArray": // WriteArray<T>(long position, T[] array, int offset, int count) -> void
                {
                    var count = Pop();
                    var offset = Pop();
                    var arr = Pop();
                    var pos = Pop();
                    var view = Pop();
                    string elemBase = RepOf(t) switch
                    {
                        ArrRep.I4 => $"(void*)&(({Cast(arr, "Dn2CppArrayI4*")})->data[{offset.Expr}])",
                        ArrRep.N => $"(void*)((({Cast(arr, "Dn2CppArrayN*")})->data) + (size_t)({offset.Expr}) * sizeof({st}))",
                        _ => throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: MemoryMappedViewAccessor.{name}<{t}> "
                            + "supports blittable element types only (reference-typed arrays are a carve-out)"),
                    };
                    if (name == "ReadArray")
                        Push(StackKind.I4, "int32_t",
                            $"dn2cpp_mmap_read_into({MmvVal(view)}, {pos.Expr}, {elemBase}, {count.Expr}, (int32_t)sizeof({st}))");
                    else
                        Emit($"dn2cpp_mmap_write_from({MmvVal(view)}, {pos.Expr}, {elemBase}, {count.Expr}, (int32_t)sizeof({st}));");
                    return;
                }
            }
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: MemoryMappedViewAccessor.{name}<T> "
                + "is not modeled (supported: Read/Write/ReadArray/WriteArray)");
        }

        // The generic ThreadPool work-item overloads — QueueUserWorkItem<TState>(
        // Action<TState>, TState, bool preferLocal) and UnsafeQueueUserWorkItem<TState>
        // enqueue on the same pool as the non-generic WaitCallback form. Reference states
        // use its callback+object path directly; value states use a typed holder and thunk
        // so Action<TState> receives the unboxed value. preferLocal is a scheduling hint
        // the fixed pool ignores.
        if (declType == "System.Threading.ThreadPool")
        {
            if (name is "QueueUserWorkItem" or "UnsafeQueueUserWorkItem"
                && MethodSpecSig(msh, methodArgs).ParameterTypes.Length == 3)
            {
                Pop();             // bool preferLocal — scheduling hint the fixed pool ignores
                var state = Pop(); // TState
                var cb = Pop();    // Action<TState>
                bool refState = CppTypes.KindOf(methodArgs[0]) == StackKind.Ref;
                Push(StackKind.I4, "int32_t",
                    refState
                        ? $"dn2cpp_threadpool_queue({Cast(cb, "Dn2CppObject*")}, {Cast(state, "Dn2CppObject*")})"
                        : $"dn2cpp_threadpool_queue_value<{CppTypes.Of(methodArgs[0])}>" +
                          $"({Cast(cb, "Dn2CppObject*")}, {state.Expr})");
                return;
            }
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: the generic ThreadPool." +
                $"{name}<TState> overload shape is not modeled");
        }

        // Marshal.{SizeOf,PtrToStructure,StructureToPtr}<T> — blittable struct <-> native
        // memory marshalling. dn2cpp lays a value-type struct out as a plain
        // standard-layout C++ struct, so for a blittable sequential type its C++ `sizeof`
        // and a raw value copy match the.NET marshalled layout exactly.
        if (declType == "System.Runtime.InteropServices.Marshal")
        {
            var t = methodArgs[0];
            switch (name)
            {
                case "SizeOf": // SizeOf<T> -> int (the marshalled size of T)
                    EmitMarshalSizeOfGeneric(t);
                    return;
                case "PtrToStructure": // PtrToStructure<T>(IntPtr ptr) -> T (read a struct from native memory)
                {
                    if (MethodSpecSig(msh, methodArgs).ParameterTypes.Length != 1)
                        throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: only the returning "
                            + "PtrToStructure<T>(IntPtr) overload is supported (the fill form is carved out)");
                    var ptr = Pop();
                    // Native memory holds T at its REAL storage width (a sub-word
                    // primitive/enum T is 1/2 bytes there, not the int32 stack model)
                    // — deref at StorageOf and promote, the ldobj dual pattern.
                    string ct = CppTypes.Of(t);
                    string st = CppTypes.StorageOf(t);
                    Push(CppTypes.KindOf(t), ct,
                        st == ct ? $"(*({ct}*)({ptr.Expr}))" : $"({ct})(*({st}*)({ptr.Expr}))");
                    return;
                }
                case "StructureToPtr": // StructureToPtr<T>(T structure, IntPtr ptr, bool fDeleteOld) -> void
                {
                    if (MethodSpecSig(msh, methodArgs).ParameterTypes.Length != 3)
                        throw new NotSupportedException(
                            $"{Method.DeclaringClass.FullName}.{Method.Name}: only StructureToPtr<T>"
                            + "(T, IntPtr, bool) is supported");
                    Pop();              // fDeleteOld — a blittable struct owns no unmanaged sub-resources
                    var ptr = Pop();
                    var val = Pop();
                    // Store into native memory at T's real storage width (dual of
                    // the PtrToStructure read above).
                    string ct = CppTypes.Of(t);
                    string st = CppTypes.StorageOf(t);
                    Emit(st == ct
                        ? $"*(({ct}*)({ptr.Expr})) = {Cast(val, ct)};"
                        : $"*(({st}*)({ptr.Expr})) = ({st})({Cast(val, ct)});");
                    return;
                }
                case "OffsetOf": // OffsetOf<T>(string fieldName) -> IntPtr
                {
                    // The MARSHALLED offset, the same constant the Type-based spelling
                    // folds to — the two agree by asking one model rather than by two
                    // lowerings happening to match.
                    var field = Pop();
                    Push(StackKind.I8, "intptr_t", $"(intptr_t)({MarshalFieldOffset(t, field)})");
                    return;
                }
                case "GetFunctionPointerForDelegate":
                // GetFunctionPointerForDelegate<TDelegate>(TDelegate d) -> IntPtr: a C-ABI
                // function pointer natives can store and call later. Lowered to the generated
                // per-delegate-type thunk pool (CppEmitter.EmitMarshalFnPtrThunks): the
                // delegate is parked in a statically-rooted slot (a GC root with unbounded
                // lifetime, unlike the synchronous P/Invoke-callback thread-local slot) and
                // the matching slot thunk's address is returned. The same instance yields the
                // same pointer (the .NET identity guarantee). Requires a concrete delegate
                // type with a fully blittable Invoke signature — under shared generics a
                // canonical TDelegate taints the body so each instantiation compiles its own.
                // The non-generic GetFunctionPointerForDelegate(Delegate) overload is not
                // intercepted and stays a loud carve-out.
                {
                    var dgCls = MarshalFnPtrDelegateClass(t);
                    var dgArg = Pop();
                    Push(StackKind.I8, "intptr_t",
                        $"(intptr_t)dn2cpp_fnptr_for_delegate_{dgCls.CppName}({Cast(dgArg, "Dn2CppObject*")})");
                    return;
                }
                case "GetDelegateForFunctionPointer":
                // GetDelegateForFunctionPointer<TDelegate>(IntPtr ptr) -> TDelegate. A pointer
                // minted by GetFunctionPointerForDelegate round-trips to the ORIGINAL parked
                // delegate instance (a pool lookup — the .NET identity guarantee for managed
                // round-trips); a raw native pointer wraps into a fresh delegate whose target
                // boxes the pointer and whose method is a generated managed-ABI forwarder that
                // casts each argument to its native width and calls it (the reverse of the
                // callback trampoline). Same TDelegate rules as above; the non-generic
                // (IntPtr, Type) overload is not intercepted and stays a loud carve-out.
                {
                    var dgCls = MarshalFnPtrDelegateClass(t);
                    var ptr = Pop();
                    Push(StackKind.Ref, dgCls.CppStructName + "*",
                        $"({dgCls.CppStructName}*)dn2cpp_delegate_for_fnptr_{dgCls.CppName}"
                        + $"((void*)({Cast(ptr, "intptr_t")}))");
                    return;
                }
            }
        }

        // generic-math conversion intrinsics on a concrete integer primitive
        //: INumberBase<TTarget>.CreateTruncating/CreateChecked/
        // CreateSaturating<TOther>(TOther value). On a concrete primitive these are
        // a pure numeric cast — IL2CPP lowers them the same way. declType names the
        // target T (System.Int32/UInt32/…); methodArgs[0] is the concrete source
        // TOther. The real bodies route through INumberBase TryConvert* (an
        // InternalCall with no IL body), so emit the conversion inline. Float/double
        // sources need saturating round-to-nearest semantics distinct from the
        // integer wraparound cast; they don't reach here (the BCL float-source
        // INumberBase paths dispatch through a different intrinsic surface), so a
        // non-integer source falls to the hard NotSupportedException below.
        if (name is "CreateTruncating" or "CreateChecked" or "CreateSaturating"
            && Compilation.WellKnownPrimitive(declType) is { Kind: TypeKind.Primitive } target
            && IsIntegerPrimitive(target.Primitive)
            && methodArgs[0] is { Kind: TypeKind.Primitive } src
            && (IsIntegerPrimitive(src.Primitive) || src.Primitive == PrimitiveTypeCode.Char))
        {
            // char counts as an unsigned 16-bit source here (StorageOf -> uint16_t),
            // so int.CreateChecked<char>(c) widens to its code unit, never overflowing
            // — reached by IndexOfAnyAsciiSearcher's low-nibble bitmap over a
            // ReadOnlySpan<char>. The integer-only create helpers handle uint16_t.
            string srcCpp = CppTypes.StorageOf(src);     // recover the source's true sign/width
            string tgtCpp = CppTypes.StorageOf(target);  // truncate to the target's natural width
            var value = Pop();
            // The source value, with its declared signedness restored (a uint stack
            // slot is held as the int32_t bit pattern, so cast through srcCpp first).
            string srcVal = $"(({srcCpp})({value.Expr}))";
            var kind = CppTypes.KindOf(target);          // I4 for sub-word/32-bit, I8 for 64-bit
            string stackCpp = CppTypes.DefaultForKind(kind);
            string result = name switch
            {
                // Truncate / wraparound: a plain cast keeps the low bits and
                // sign/zero-extends back to the stack width (e.g. uint.Create-
                // Truncating(-1) == 0xFFFFFFFF, sbyte.CreateTruncating(255) == -1).
                "CreateTruncating" => $"(({stackCpp})(({tgtCpp}){srcVal}))",
                // Checked: trap with OverflowException unless the value fits T.
                "CreateChecked" => $"(({stackCpp})dn2cpp_create_checked<{tgtCpp}, {srcCpp}>({srcVal}))",
                // Saturating: clamp to [T.Min, T.Max] (no reach today, but exact if
                // it appears). The bounds compare in the source domain.
                _ => $"(({stackCpp})dn2cpp_create_saturating<{tgtCpp}, {srcCpp}>({srcVal}))",
            };
            Push(kind, stackCpp, result);
            return;
        }

        // generic-math conversion FROM a floating-point source TO an integer target:
        // INumberBase<intTarget>.CreateTruncating/CreateChecked/CreateSaturating<float|
        // double>(value). Distinct from the all-integer block above: .NET's float->integer
        // conversion for BOTH Truncating and Saturating saturates (Single/Double route
        // TryConvertTo{Truncating,Saturating} through the same clamp), so a plain wraparound
        // cast — which is UB on overflow in C++ — is wrong here. NaN -> 0, +Inf / over-max
        // -> T.MaxValue, -Inf / under-min -> T.MinValue (signed) or 0 (unsigned), else
        // truncate toward zero. Checked traps (OverflowException) out of range and on NaN.
        // Same helper split the Call.cs TryConvertFrom/To* handler uses (dn2cpp_convert_to_
        // integer_native for the saturating float->int cast, dn2cpp_create_checked for the
        // checked form). Reached via Enum.GetNameInlined / Enum.AreSequentialFromZero's
        // uint/ulong.CreateTruncating over a floating storage.
        if (name is "CreateTruncating" or "CreateChecked" or "CreateSaturating"
            && Compilation.WellKnownPrimitive(declType) is { Kind: TypeKind.Primitive } itarget
            && IsIntegerPrimitive(itarget.Primitive)
            && methodArgs[0] is { Kind: TypeKind.Primitive } isrc
            && isrc.Primitive is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double)
        {
            string srcCpp = CppTypes.StorageOf(isrc);    // float / double
            string tgtCpp = CppTypes.StorageOf(itarget); // the target's natural width
            var ikind = CppTypes.KindOf(itarget);        // I4 for sub-word/32-bit, I8 for 64-bit
            string istackCpp = CppTypes.DefaultForKind(ikind);
            var value = Pop();
            string srcVal = $"(({srcCpp})({value.Expr}))";
            string conv = name == "CreateChecked"
                ? $"dn2cpp_create_checked<{tgtCpp}, {srcCpp}>({srcVal})"
                : $"dn2cpp_convert_to_integer_native<{tgtCpp}, {srcCpp}>({srcVal})";
            Push(ikind, istackCpp, $"(({istackCpp})({conv}))");
            return;
        }

        // generic-math conversion to a floating-point target: INumberBase<float|double>
        //.CreateTruncating/CreateChecked/CreateSaturating<TOther>(TOther value).
        // Widening from any integer or a narrower float is a plain representation cast,
        // always in range for the checked form here — `Average` widens its (integer or
        // float) sum/count to double exactly this way. The only out-of-range case is a
        // double->float CreateChecked overflowing to infinity, a carve-out unreached by
        // these aggregates.
        if (name is "CreateTruncating" or "CreateChecked" or "CreateSaturating"
            && Compilation.WellKnownPrimitive(declType) is { Kind: TypeKind.Primitive } ftarget
            && ftarget.Primitive is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double
            && methodArgs[0] is { Kind: TypeKind.Primitive } fsrc)
        {
            string srcCpp = CppTypes.StorageOf(fsrc);    // recover the source's true sign/width
            string tgtCpp = CppTypes.StorageOf(ftarget);
            var fkind = CppTypes.KindOf(ftarget);
            string fstackCpp = CppTypes.DefaultForKind(fkind);
            var value = Pop();
            Push(fkind, fstackCpp, $"(({fstackCpp})(({tgtCpp})(({srcCpp})({value.Expr}))))");
            return;
        }

        // Int128/UInt128.CreateTruncating<TOther> from an integer-primitive source.
        // Int128/UInt128 are ordinary transpiled BCL structs — two ulong fields
        // {_lower, _upper} in little-endian declaration order (16 bytes, no padding) —
        // not primitives, so the primitive blocks above never match them. Widening a
        // 32/64-bit (or narrower) integer into 128 bits is a pure sign/zero extension:
        // the source's low 64 bits go in _lower, and _upper is the sign fill (all-ones
        // for a negative signed source, else zero). This is the reach corpus's only
        // Int128 generic-math shape (Int128Converter/UInt128Converter parse over
        // Int32/UInt32/Int64/UInt64); a float or non-integer source is a loud decline —
        // dn2cpp models no general Int128 generic math. The real body branches to
        // TOther.TryConvertToTruncating (an InternalCall with no IL) — cut on the reach
        // side (MsInt128CreateConversion), lowered here. The two ulong words are written
        // by raw offset (like the Range read in GetSubArray above), so the lowering is
        // independent of the emitted struct's field-member spelling.
        if (name == "CreateTruncating" && declType is "System.Int128" or "System.UInt128")
        {
            if (methodArgs[0] is not { Kind: TypeKind.Primitive } i128src
                || !(IsIntegerPrimitive(i128src.Primitive) || i128src.Primitive == PrimitiveTypeCode.Char))
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {declType}.CreateTruncating<{methodArgs[0]}> "
                    + "is modeled for integer-primitive sources only (Int128 generic-math over a "
                    + "floating-point or non-primitive source is a carve-out)");
            // Resolve the target struct by declType name, NOT off the MethodSpec's
            // declaring class: the call-site form's MethodSpec parent IS Int128, but the
            // constrained static-virtual form (`constrained. Int128 call
            // INumberBase<TSelf>::CreateTruncating`) has parent INumberBase<Int128>, whose
            // DeclaringClass is the interface. Both forms carry declType = the struct name.
            var i128cls = Comp.FindClassByFullName(declType)
                ?? throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {declType}.CreateTruncating "
                    + "target struct is not in the loaded type set");
            string i128Ct = CppTypes.Of(TypeDesc.MakeClass(i128cls)); // the emitted struct, by value
            string srcCpp = CppTypes.StorageOf(i128src);
            bool signed = i128src.Primitive is PrimitiveTypeCode.SByte or PrimitiveTypeCode.Int16
                or PrimitiveTypeCode.Int32 or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.IntPtr;
            var value = Pop();
            string res = NewTemp(i128Ct);
            if (signed)
            {
                string w = NewTemp("int64_t");
                Emit($"{w} = (int64_t)(({srcCpp})({value.Expr}));");
                Emit($"((uint64_t*)&{res})[0] = (uint64_t){w};");         // _lower
                Emit($"((uint64_t*)&{res})[1] = (uint64_t)({w} >> 63);");  // _upper: sign fill
            }
            else
            {
                Emit($"((uint64_t*)&{res})[0] = (uint64_t)(({srcCpp})({value.Expr}));"); // _lower
                Emit($"((uint64_t*)&{res})[1] = 0;");                                     // _upper
            }
            Push(StackKind.Struct, i128Ct, res);
            return;
        }

        // Enum reflection-lite: the generic statics lowered inline from the
        // per-enum (name, value) table — enums are int32 here, so values are int
        // constants and names are string literals.
        if (declType == "System.Enum")
        {
            TranslateEnumReflection(name, msh, methodArgs);
            return;
        }

        // CustomAttributeExtensions.GetCustomAttributes<T>: fetch the
        // typeof(T)-filtered attribute array (same runtime helper the non-generic /
        // singular forms use), then retag it with the precise ti_arr_<T> handle so it
        // carries the real SZArray interface map. The IEnumerable<T> result is the *raw
        // array*, so a foreach dispatches on the array itself (no compile-time
        // SZArrayEnumerable<T> wrap, whose intermediate IEnumerable<Attribute> typing lost
        // the covariance and whose runtime type was wrong). Overloads: (element) or
        // (element, bool inherit); inherit is ignored (matching ).
        if (declType == "System.Reflection.CustomAttributeExtensions" && name == "GetCustomAttributes")
        {
            var ms = Reader.GetMethodSpecification(msh);
            var msig = Reader.GetMemberReference((MemberReferenceHandle)ms.Method)
                .DecodeMethodSignature(Comp.SigProvider, new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs));
            var retType = msig.ReturnType; // IEnumerable<T>
            if (msig.ParameterTypes.Length == 2)
                Pop(); // bool inherit — ignored
            string element = Cast(Pop(), "Dn2CppObject*");
            var t = methodArgs[0];
            // A placeholder T bakes the FILTER's identity: the canonical body would
            // fall back to the object type-info and answer every group member with
            // ALL attributes — silently. Same rule as the non-generic funnels'
            // TaintPlaceholderAttrFilter.
            TaintIfCanonical(t, "type-identity");
            string tti = TypeInfoExpr(t) ?? "&dn2cpp_object_type";
            Comp.NoteArrayEnumerableElement(t); // wire ti_arr_<T> + its SZArray interface map
            string arrTmp = NewTemp("Dn2CppArrayRef*");
            Emit($"{arrTmp} = dn2cpp_get_custom_attributes_typed({element}, dn2cpp_get_type_from_handle({tti}));");
            Emit($"((Dn2CppObject*){arrTmp})->type = {PreciseArrayTypeInfoExpr(t)};");
            string retCpp = CppTypes.Of(retType);
            PushEntry(new StackEntry($"(({retCpp}){arrTmp})", StackKind.Ref, retCpp, StaticType: retType));
            return;
        }

        // Activator.CreateInstance<T>: the generic factory. The real body
        // reflects (RuntimeType.CreateInstanceOfT / CreateInstanceForAnotherGeneric-
        // Parameter), so emit `new T` directly — a reference type allocates and runs
        // its parameterless ctor (like newobj); a value type (struct/enum/primitive)
        // is the zero value, then an explicit struct parameterless ctor if it has one.
        if (declType == "System.Activator" && name == "CreateInstance")
        {
            var t = methodArgs[0];
            // A placeholder T bakes the instantiation's identity (which real
            // class to allocate); the reference stand-in would otherwise fall
            // through to the zero-value arm and silently produce null.
            TaintIfCanonical(t, "newobj");
            string ct = CppTypes.Of(t);
            // Enums are excluded here (IsEnum, not IsValueType, is the flag an enum
            // carries — the two are disjoint, so a bare `IsValueType: false` lets an
            // enum through): Activator.CreateInstance<TEnum>() returns default(TEnum)
            // in real .NET, never throws, so an enum must reach the value arm below.
            // Without the guard a long-underlying enum (Godot.OS.RenderingDriver) fell
            // into the no-ctor throw arm and pushed a `nullptr` placeholder into its
            // int64_t stack slot — a C++ type error on a dead path AND a wrong runtime
            // semantics (a throw where .NET returns 0).
            if (t.Kind == TypeKind.Class && t.Class is { IsValueType: false, IsEnum: false } cls)
            {
                // The instantiability verdict comes FIRST, and it is public-scoped:
                // Activator.CreateInstance<T>() reflects through RuntimeType.CreateInstanceOfT,
                // which binds ONLY a *public* parameterless ctor. Real .NET rejects none of
                // these at compile time — the miss is a RUN-time MissingMethodException, and
                // Lazy<T>'s CreateViaDefaultConstructor catches exactly that type to re-raise
                // MissingMemberException — so emit the catchable throw rather than aborting
                // the transpile, which would kill a merely-reachable-never-run path. The type
                // identity MUST be MissingMethodException for the catch to bind.
                // TaintIfCanonical above keeps a placeholder T from baking this verdict into
                // a shared body.
                //
                // This gate PRECEDES the intrinsic-mapped arm on purpose: an intrinsic
                // reference type with no public parameterless ctor (Task<T>) must land on the
                // run-time MissingMethodException, not the loud transpile-time throw the
                // intrinsic arm reserves for a constructible-but-unmodeled intrinsic.
                if (cls.IsAbstract || Compilation.PublicParameterlessCtor(cls) is not { } ctor)
                {
                    string reason = cls.IsInterface
                        ? "Cannot create an instance of an interface."
                        : cls.IsAbstract
                            ? "Cannot create an instance of an abstract class."
                            : $"No parameterless constructor defined for type '{cls.FullName}'.";
                    Emit($"dn2cpp_throw_missing_method(\"{reason}\");");
                    // [[noreturn]]; the push only keeps the eval stack typed. Use the
                    // slot's own zero (a reference type's ct is a pointer, so this is
                    // nullptr) rather than a bare "nullptr" that would not type-check
                    // against a non-pointer slot on this dead path.
                    Push(StackKind.Ref, ct, CppTypes.ZeroInitExpr(ct));
                    return;
                }
                // An intrinsic-mapped reference type (StringBuilder, ...) is emitted
                // inline and NEVER transpiles its ctor — Reach cuts the ctor body on its
                // intrinsic-type early return. So the DirectCallSym(ctor) path below would
                // name a ctor symbol nothing defines and the C++ link would fail on a
                // mangled name (cut ⟹ route). Route to the intrinsic parameterless
                // construction, exactly as a direct `new T()` newobj does
                // (MethodCompiler.Newobj's StringBuilder arm) — the shape a
                // DefaultObjectPool<StringBuilder> reaches via `new T()` where T:new().
                // Only intrinsic types WITH a public parameterless ctor reach here — the
                // gate above already routed the rest to the run-time throw.
                if (cls.IntrinsicCppName is not null)
                {
                    if (cls.FullName == "System.Text.StringBuilder")
                    {
                        Push(StackKind.Ref, "Dn2CppStringBuilder*", "dn2cpp_sb_new()");
                        return;
                    }
                    // No modeled parameterless construction for any other intrinsic
                    // reference type; throw at transpile rather than fall through and name
                    // the cut ctor. StringBuilder is the only one the corpus reaches;
                    // extend here if another appears.
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: "
                        + $"Activator.CreateInstance<{cls.FullName}> over an intrinsic-mapped "
                        + "type has no modeled parameterless construction");
                }
                TaintIfCanonical(cls, "newobj");
                string obj = NewTemp(cls.CppStructName + "*");
                Emit($"{obj} = ({cls.CppStructName}*)dn2cpp_alloc(sizeof({cls.CppStructName}));");
                Emit($"((Dn2CppObject*){obj})->type = &{cls.CppTypeInfoName};");
                // Same contract as the generic newobj path: a T whose effective
                // Finalize is reached is registered before its ctor runs (a throwing
                // ctor still leaves the partially constructed object to be finalized).
                // Gate on Reachable, not merely non-null EffectiveFinalize, so a T that
                // subclasses an opaque intrinsic base with an intrinsic-cut Finalize slot
                // does not register against a null slot.
                if (Compilation.EffectiveFinalize(cls) is { } fin && Comp.Reachable.Contains(fin))
                    Emit($"dn2cpp_register_finalizer((Dn2CppObject*){obj});");
                Emit($"{DirectCallSym(ctor)}({ArgsWithRgctx(obj, ctor)});");
                PushEntry(new StackEntry(obj, StackKind.Ref, cls.CppStructName + "*", StaticType: TypeDesc.MakeClass(cls)));
                return;
            }
            string zero = CppTypes.ZeroInitExpr(ct);
            // An intrinsic-mapped value type (Decimal, Vector<T>, ...) is emitted inline
            // and does not transpile a parameterless ctor; the intrinsic zero value is its
            // correct default. Guard the DirectCallSym(vctor) route on a non-intrinsic
            // type so it never names a cut ctor symbol (cut ⟹ route), mirroring the
            // reference arm above.
            if (t.Kind == TypeKind.Class && t.Class is { IsValueType: true, IntrinsicCppName: null } vcls
                && Compilation.ParameterlessCtor(vcls) is { } vctor)
            {
                string val = NewTemp(ct);
                Emit($"{val} = {zero};");
                Emit($"{DirectCallSym(vctor)}({ArgsWithRgctx($"&{val}", vctor)});");
                Push(StackKind.Struct, ct, val);
                return;
            }
            Push(CppTypes.KindOf(t), ct, zero);
            return;
        }

        // MemoryMarshal.GetArrayDataReference<T>(T[]) -> ref T at element 0. The real
        // generic body tail-calls the non-generic (Array) overload we don't model, so
        // emit element 0's address directly — same expression as `ldelema arr[0]`, with
        // the storage-width pointer. No bounds check:.NET returns the ref
        // unconditionally (even for an empty array, where it is just past the header).
        if (declType == "System.Runtime.InteropServices.MemoryMarshal" && name == "GetArrayDataReference")
        {
            var arr = Pop();
            var t = methodArgs[0];
            string ct = CppTypes.Of(t);
            string st = CppTypes.StorageOf(t);
            string addr = RepOf(t) switch
            {
                // The I4 representation backs int/uint/char/enum alike with an int32_t[]
                // payload, so cast to the element's own C++ type — a uint32_t*/int32_t*
                // signedness mismatch is otherwise a hard error (matches the Ref branch and
                // EmitLdelema). A no-op cast for int/enum.
                ArrRep.I4 => $"({ct}*)&(({Cast(arr, "Dn2CppArrayI4*")})->data[0])",
                ArrRep.Ref => $"({ct}*)&(({Cast(arr, "Dn2CppArrayRef*")})->data[0])",
                // The N path is the packed element buffer's base — element 0's address is
                // just `data`. Use it directly rather than dn2cpp_elem_addr(arr, 0), which
                // bounds-checks and so wrongly threw on an empty array;.NET returns the ref
                // unconditionally (`char[0]`→Span<char> for an empty TryFormat
                // dest reached this path via the real Span(T[]) ctor → GetArrayDataReference).
                _ => $"({st}*)(({Cast(arr, "Dn2CppArrayN*")})->data)",
            };
            Push(StackKind.Ptr, RepOf(t) == ArrRep.N ? st + "*" : ct + "*", addr);
            return;
        }

        // RuntimeHelpers.GetSubArray<T>(T[] array, Range range) = the C# `array[range]`
        // lowering. The real body resolves the Range against the array
        // length then routes through Array.CreateInstanceFromArrayType / MemoryMarshal /
        // Buffer.Memmove internals we don't model, so cut it (RuntimeHelpers is an
        // s_intrinsicTypes member → ResolveCallTarget already drops the body) and lower
        // here. A Range is laid out as two System.Index structs, each a single int32
        // _value (from-start = value, from-end = ~index, i.e. value < 0). Read both
        // _value words from the by-value Range (standard-layout, no padding: word [0] is
        // Start._value, word [1] is End._value), resolve (offset, length) + bounds-check
        // via dn2cpp_range_offset_length (== Range.GetOffsetAndLength), then a per-rep
        // helper allocates a fresh array of the source's precise type-info + slice length
        // and shallow-copies the [offset, offset+length) element run. Result is T[].
        if (declType == "System.Runtime.CompilerServices.RuntimeHelpers" && name == "GetSubArray")
        {
            var range = Pop();   // System.Range (by value)
            var arr = Pop();     // T[] source
            var t = methodArgs[0];
            // Spill the Range into a typed temp so we can take its address and read the
            // two Index._value words regardless of how the popped expression was formed.
            string rg = NewTemp(range.CppType);
            Emit($"{rg} = {range.Expr};");
            string startVal = $"((int32_t*)&{rg})[0]";
            string endVal = $"((int32_t*)&{rg})[1]";
            string srcLen = $"((Dn2CppArray*)({arr.Expr}))->length";
            string off = NewTemp("int32_t");
            string len = NewTemp("int32_t");
            Emit($"{len} = dn2cpp_range_offset_length({startVal}, {endVal}, {srcLen}, &{off});");
            string sub = RepOf(t) switch
            {
                ArrRep.I4 => $"dn2cpp_array_subarray_i4({Cast(arr, "Dn2CppArrayI4*")}, {off}, {len})",
                ArrRep.Ref => $"dn2cpp_array_subarray_ref({Cast(arr, "Dn2CppArrayRef*")}, {off}, {len})",
                _ => $"dn2cpp_array_subarray_n({Cast(arr, "Dn2CppArrayN*")}, {off}, {len})",
            };
            string retCpp = CppTypes.Of(TypeDesc.MakeSZArray(t));
            Push(StackKind.Ref, retCpp, $"({retCpp})({sub})", TypeDesc.MakeSZArray(t));
            return;
        }

        // RuntimeHelpers.CreateSpan<T>(RuntimeFieldHandle) — an inline constant array
        // literal used where a ReadOnlySpan<T> is expected: `Foo(new[]{1,2,3})`, a span
        // collection-expression, or a `"…"u8` UTF-8 literal. Roslyn lowers it to
        // `ldtoken <RVA field>` + this call. The ldtoken already materialized the field's
        // raw bytes as a static blob and pushed its pointer, so build the
        // ReadOnlySpan<T> {f__reference, f__length} over that blob: the reference is the
        // blob pointer (cast to the element storage type), the length is the blob byte
        // count / the element storage width. RVA blobs back blittable value elements only
        // (the JIT never emits CreateSpan for reference types).
        if (declType == "System.Runtime.CompilerServices.RuntimeHelpers" && name == "CreateSpan")
        {
            var t = methodArgs[0];
            var handleArg = Pop();
            if (handleArg.BlobLen is not { } byteLen)
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: RuntimeHelpers.CreateSpan expects "
                    + "an inline ldtoken'd RVA field — keep the array/UTF-8 literal at the call site");
            var rsCtx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
            var rsMs = Reader.GetMethodSpecification(msh);
            var rsSig = rsMs.Method.Kind == HandleKind.MethodDefinition
                ? Reader.GetMethodDefinition((MethodDefinitionHandle)rsMs.Method).DecodeSignature(Comp.SigProvider, rsCtx)
                : Reader.GetMemberReference((MemberReferenceHandle)rsMs.Method).DecodeMethodSignature(Comp.SigProvider, rsCtx);
            string spanCt = CppTypes.Of(rsSig.ReturnType);
            string st = CppTypes.StorageOf(t);
            // f__reference is the element-storage pointer (matches the span struct's field
            // type); f__length is the element count (a compile-time-folded constant).
            Push(StackKind.Struct, spanCt,
                $"{spanCt}{{ ({st}*){handleArg.Expr}, (int32_t)({byteLen} / sizeof({st})) }}");
            return;
        }

        // MemoryMarshal span-shaping intrinsics. The real bodies use JIT
        // intrinsics we don't model (IsReferenceOrContainsReferences / Unsafe.As /
        // nuint math), so build the result span's {f__reference, f__length} directly.
        // A span's `_reference` field is `ref T` => StorageOf(T)*, matching the rest of
        // the span codegen; widths use StorageOf so sub-word element spans are exact.
        if (declType == "System.Runtime.InteropServices.MemoryMarshal")
        {
            var mmSig = MethodSpecSig(msh, methodArgs);
            switch (name)
            {
                case "GetReference": // GetReference<T>(Span<T>|ReadOnlySpan<T>) -> ref T (element 0)
                {
                    var span = Pop();
                    string sv = SpanValue(span, CppTypes.Of(mmSig.ParameterTypes[0]));
                    string st = CppTypes.StorageOf(methodArgs[0]);
                    Push(StackKind.Ptr, st + "*", $"({st}*)({sv}.f__reference)");
                    return;
                }
                case "Cast": // Cast<TFrom,TTo>(Span<TFrom>|ROS<TFrom>) -> Span<TTo>|ROS<TTo>
                {
                    var span = Pop();
                    string sv = SpanValue(span, CppTypes.Of(mmSig.ParameterTypes[0]));
                    string outCt = CppTypes.Of(mmSig.ReturnType);
                    NoteBuiltStructLayout(mmSig.ReturnType);
                    string fromSt = CppTypes.StorageOf(methodArgs[0]);
                    string toSt = CppTypes.StorageOf(methodArgs[1]);
                    // newLen = (int)((long)len * sizeof(TFrom) / sizeof(TTo)) — int64
                    // intermediate avoids overflow, matching MemoryMarshal.Cast.
                    Push(StackKind.Struct, outCt,
                        $"{outCt}{{ ({toSt}*)({sv}.f__reference), "
                        + $"(int32_t)((int64_t){sv}.f__length * (int64_t)sizeof({fromSt}) / (int64_t)sizeof({toSt})) }}");
                    return;
                }
                case "AsBytes": // AsBytes<T>(Span<T>|ROS<T>) -> Span<byte>|ROS<byte>
                {
                    var span = Pop();
                    string sv = SpanValue(span, CppTypes.Of(mmSig.ParameterTypes[0]));
                    string outCt = CppTypes.Of(mmSig.ReturnType);
                    NoteBuiltStructLayout(mmSig.ReturnType);
                    string st = CppTypes.StorageOf(methodArgs[0]);
                    Push(StackKind.Struct, outCt,
                        $"{outCt}{{ (uint8_t*)({sv}.f__reference), (int32_t)((int64_t){sv}.f__length * (int64_t)sizeof({st})) }}");
                    return;
                }
                case "Read": // Read<T>(ReadOnlySpan<byte> source) -> T
                {
                    var span = Pop();
                    string sv = SpanValue(span, CppTypes.Of(mmSig.ParameterTypes[0]));
                    string st = CppTypes.StorageOf(methodArgs[0]);
                    string ct = CppTypes.Of(methodArgs[0]);
                    // MemoryMarshal.Read throws when the span is smaller than sizeof(T).
                    Emit($"if ((size_t)({sv}.f__length) < sizeof({st})) dn2cpp_fail(\"ArgumentOutOfRangeException\");");
                    string tmp = NewTemp(st);
                    Emit($"std::memcpy(&{tmp}, (const void*)({sv}.f__reference), sizeof({st}));");
                    Push(CppTypes.KindOf(methodArgs[0]), ct, st == ct ? tmp : $"({ct})({tmp})");
                    return;
                }
                case "Write": // Write<T>(Span<byte> destination, in T value) -> void
                {
                    var valRef = Pop();  // `in T` / `ref T` => a managed pointer
                    var span = Pop();
                    string sv = SpanValue(span, CppTypes.Of(mmSig.ParameterTypes[0]));
                    string st = CppTypes.StorageOf(methodArgs[0]);
                    Emit($"if ((size_t)({sv}.f__length) < sizeof({st})) dn2cpp_fail(\"ArgumentOutOfRangeException\");");
                    Emit($"std::memcpy((void*)({sv}.f__reference), (const void*)({valRef.Expr}), sizeof({st}));");
                    return;
                }
                case "CreateSpan":         // CreateSpan<T>(ref T reference, int length) -> Span<T>
                case "CreateReadOnlySpan": // CreateReadOnlySpan<T>(ref|in T, int length) -> ReadOnlySpan<T>
                {
                    var len = Pop();
                    var refp = Pop();
                    string outCt = CppTypes.Of(mmSig.ReturnType);
                    NoteBuiltStructLayout(mmSig.ReturnType);
                    string st = CppTypes.StorageOf(methodArgs[0]);
                    // A shared-layout span over a reference element declares
                    // f__reference with the erased spelling — cast to the member.
                    string refT = LayoutMemberType(mmSig.ReturnType, "_reference", st + "*");
                    Push(StackKind.Struct, outCt,
                        $"{outCt}{{ ({refT})({refp.Expr}), (int32_t)({len.Expr}) }}");
                    return;
                }
            }
            // Any other MemoryMarshal generic method transpiles normally — fall through.
        }

        // MemoryExtensions.{IndexOfAny,IndexOfAnyExcept,LastIndexOfAny,LastIndexOfAnyExcept,
        // ContainsAny,ContainsAnyExcept}(span, SearchValues<T>) — membership scan over the
        // runtime set SearchValues.Create built (byte / char). Intercepted BEFORE the
        // general span block (and before the real BCL bodies, which dispatch to the
        // SIMD/ProbabilisticMap SearchValues<T> virtuals). The `Except` forms negate the
        // membership test; the `ContainsAny*` forms return whether such an index exists.
        // Only the SearchValues<T> 2-arg shape is taken here — every other overload (the
        // ROS<T>/fixed-arity/value forms) falls through to the general block below.
        if (declType == "System.MemoryExtensions"
            && name is "IndexOfAny" or "IndexOfAnyExcept" or "LastIndexOfAny"
                       or "LastIndexOfAnyExcept" or "ContainsAny" or "ContainsAnyExcept")
        {
            var svSig = MethodSpecSig(msh, methodArgs).ParameterTypes;
            if (svSig.Length == 2
                && svSig[1] is { Kind: TypeKind.Class, Class: { } svcls }
                && Comp.GenericDefFullName(svcls) == "System.Buffers.SearchValues")
            {
                string suffix = SearchValuesSuffix(methodArgs[0])
                    ?? throw new NotSupportedException(
                        $"MemoryExtensions.{name}<{methodArgs[0]}>: SearchValues is modeled for byte/char elements only");
                string ptrCt = suffix == "u8" ? "const uint8_t*" : "const uint16_t*";
                bool last = name.StartsWith("Last", StringComparison.Ordinal);
                bool except = name.EndsWith("Except", StringComparison.Ordinal);
                bool contains = name.StartsWith("ContainsAny", StringComparison.Ordinal);
                string svp = Cast(Pop(), "Dn2CppSearchValues*");
                string sp = SpanValue(Pop(), CppTypes.Of(svSig[0]));
                string fn = $"dn2cpp_search_values_{(last ? "last_index_of_any" : "index_of_any")}_{suffix}";
                string res = NewTemp("int32_t");
                Emit($"{res} = {fn}(({ptrCt}){sp}.f__reference, {sp}.f__length, {svp}, {(except ? 1 : 0)});");
                Push(StackKind.I4, "int32_t", contains ? $"({res} >= 0 ? 1 : 0)" : res);
                return;
            }
            // Not the SearchValues shape — fall through to the general span block.
        }

        // MemoryExtensions span scan/structural ops over a span's {f__reference,
        // f__length}. The real BCL bodies vectorize (SIMD + Unsafe.BitCast /
        // IsBitwiseEquatable), untranspilable; emit scalar loops instead. Element
        // comparison is the devirtualized EqualityComparer<T>.Default.Equals shared
        // with the Dictionary key path and the array scans — CanEqualityEquals to
        // decide (a pure predicate: the guards run before the Pops, and before the
        // loop the expression will sit inside), TryEqualityEqualsLValue at the emit
        // position to build it, once, over the loop's own locals. The real signatures
        // say `where T : IEquatable<T>`, which is exactly what that dispatches to:
        // (span, value) Contains / IndexOf / LastIndexOf / [Last]IndexOfAnyExcept
        // (span, span) SequenceEqual / StartsWith / EndsWith
        // (span, v0, v1[, v2]) [Last]IndexOfAny / [Last]IndexOfAnyExcept (fixed arity)
        // (span, ROS<T> values) [Last]IndexOfAny / [Last]IndexOfAnyExcept
        // (span) Reverse (in-place)
        if (declType == "System.MemoryExtensions"
            && name is "Contains" or "IndexOf" or "LastIndexOf"
                       or "SequenceEqual" or "StartsWith" or "EndsWith"
                       or "IndexOfAny" or "LastIndexOfAny"
                       or "IndexOfAnyExcept" or "LastIndexOfAnyExcept" or "Reverse")
        {
            var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
            var ms = Reader.GetMethodSpecification(msh);
            var csig = ms.Method.Kind == HandleKind.MethodDefinition
                ? Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(Comp.SigProvider, ctx)
                : Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(Comp.SigProvider, ctx);
            var ps = csig.ParameterTypes;
            var elem = methodArgs[0];
            string elemCt = CppTypes.Of(elem);
            string elemSt = CppTypes.StorageOf(elem);
            string ElemAt(string span, string idx) => $"({elemCt})(({elemSt}*){span}.f__reference)[{idx}]";
            // A loop-body local holding one element — an addressable lvalue of the
            // element's own C++ type, which is what the struct arm of the comparison
            // needs (it calls Equals(T) on its address).
            StackEntry Elem(string name) => new(name, CppTypes.KindOf(elem), elemCt);
            bool canEq = CanEqualityEquals(elem);

            // Every one of these scans has a net9+ overload taking a trailing
            // IEqualityComparer<T>, and reaching it is not exotic: an element type that
            // does NOT implement IEquatable<T> — a plain struct with only an
            // Equals(object) override — binds to THAT overload, because the constrained
            // one does not apply to it. Take the comparer off the stack and match the
            // shapes on what is left.
            //
            // Null means default equality. EqualityComparer<T>.Default is a non-null
            // singleton whose relation-only interface row takes the same runtime fallback.
            //
            // A GENUINE custom comparer is HONORED: the interface probe is hoisted out of
            // the loop (the same dn2cpp_try_resolve_interface + interface-slot template as
            // the Dictionary._comparer path) and every element comparison the loops build
            // dispatches through the comparer object when the probe resolves, else falls
            // back to the element type's default equality. The dispatch names the closed
            // IEqualityComparer<T>'s type-info, emitted only when an allocated
            // implementer's RenderItfTables row lays it down —
            // Compilation.UserComparerInterfaceMethod is that gate. When it answers null
            // the loops are emitted bare and the runtime guard (EmitCustomComparerGuard)
            // stays on that arm, so a receiver reachability cannot see (a reflection-built
            // comparer) throws instead of being silently answered with default equality.
            // Either form refuses to produce a wrong answer with no diagnostic.
            string cmpObj = "", cmpSlots = "";
            MethodInfo? cmpEquals = null;
            if (ps.Length >= 2
                && ps[^1] is { Kind: TypeKind.Class, Class: { } cmpCls }
                && Comp.GenericDefFullName(cmpCls) == "System.Collections.Generic.IEqualityComparer")
            {
                var comparer = Pop();
                if (!comparer.KnownNull)
                {
                    // canEq gates the dispatch too: the probe's null arm needs the
                    // default-equality expression, and an element type with no
                    // comparison at all falls to this block's trailing throw anyway.
                    if (canEq && Comp.UserComparerInterfaceMethod(elem, "Equals") is { } em)
                    {
                        cmpEquals = em;
                        NoteCanonicalItfDispatch(em.DeclaringClass);
                        string itf = "&" + ItfDispatchTi(em.DeclaringClass).CppTypeInfoName;
                        cmpObj = NewTemp("Dn2CppObject*");
                        Emit($"{cmpObj} = {Cast(comparer, "Dn2CppObject*")};");
                        cmpSlots = NewTemp("const void**");
                        Emit($"{cmpSlots} = {cmpObj} ? dn2cpp_try_resolve_interface({cmpObj}->type, {itf}) : nullptr;");
                        Emit($"if ({cmpObj} != nullptr && !dn2cpp_is_default_equality_comparer({cmpObj}) && {cmpSlots} == nullptr) {{");
                        Emit($"    dn2cpp_throw_platform_not_supported(\"MemoryExtensions.{name}<{elem}>: "
                            + "the custom IEqualityComparer<T>'s type has no reachable "
                            + "IEqualityComparer<T> interface table to dispatch through.\");");
                        Emit("}");
                    }
                    else
                    {
                        EmitCustomComparerGuard(comparer, $"MemoryExtensions.{name}<{elem}>");
                    }
                }
                ps = ps.RemoveAt(ps.Length - 1);
            }
            // The ONE builder for "these two elements are equal" every loop below asks:
            // the hoisted comparer dispatch when one was set up above, else — and as the
            // dispatch's own runtime-null arm — the element type's default equality
            // (TryEqualityEqualsLValue, the rule shared with the Dictionary key path and
            // the array scans). Callers pass loop-body locals (Elem), so the dispatch
            // arm can name their expressions directly.
            string EqLValue(StackEntry x, StackEntry y)
            {
                string dflt = TryEqualityEqualsLValue(elem, x, y)!;
                return cmpEquals is null
                    ? dflt
                    : $"({cmpSlots} != nullptr"
                      + $" ? (int32_t)((({FnPtrType(cmpEquals)})({cmpSlots}[{cmpEquals.VtableSlot}]))"
                      + $"(({cmpEquals.DeclaringClass.CppStructName}*){cmpObj}, {x.Expr}, {y.Expr}))"
                      + $" : (int32_t)({dflt}))";
            }

            // (span): in-place reverse via the span's buffer (by-value span, same ptr).
            if (name == "Reverse" && ps.Length == 1)
            {
                string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
                string i = NewTemp("int32_t");
                string j = NewTemp("int32_t");
                string tmp = NewTemp(elemSt);
                Emit($"for ({i} = 0, {j} = {sp}.f__length - 1; {i} < {j}; {i}++, {j}--) {{");
                Emit($"    {tmp} = (({elemSt}*){sp}.f__reference)[{i}];");
                Emit($"    (({elemSt}*){sp}.f__reference)[{i}] = (({elemSt}*){sp}.f__reference)[{j}];");
                Emit($"    (({elemSt}*){sp}.f__reference)[{j}] = {tmp};");
                Emit("}");
                return;
            }

            // (span, span): IndexOf / LastIndexOf of a contiguous subsequence — the
            // substring-search overloads (e.g. message.AsSpan().IndexOf(marker.AsSpan())).
            // Same (span, value) arity as the single-element scan below, so it must be
            // distinguished by ps[1] being a span and matched first. Naive O(n*m) scan (the
            // BCL vectorizes). An empty value matches at 0 (IndexOf) / span length
            // (LastIndexOf), which the forward scan yields naturally.
            if (name is "IndexOf" or "LastIndexOf" && ps.Length == 2
                && ps[1] is { Kind: TypeKind.Class, Class: { } subCls }
                && Comp.GenericDefFullName(subCls) is "System.ReadOnlySpan" or "System.Span"
                && canEq)
            {
                string cmpSub = EqLValue(Elem("__a"), Elem("__b"));
                string vsp = SpanValue(Pop(), CppTypes.Of(ps[1]));
                string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
                string res = NewTemp("int32_t");
                Emit($"{res} = -1;");
                string i = NewTemp("int32_t");
                string j = NewTemp("int32_t");
                string ok = NewTemp("int32_t");
                Emit($"for ({i} = 0; {i} <= {sp}.f__length - {vsp}.f__length; {i}++) {{");
                Emit($"    {ok} = 1;");
                Emit($"    for ({j} = 0; {j} < {vsp}.f__length; {j}++) {{");
                Emit($"        {elemCt} __a = {ElemAt(sp, $"({i} + {j})")};");
                Emit($"        {elemCt} __b = {ElemAt(vsp, j)};");
                Emit($"        if (!({cmpSub})) {{ {ok} = 0; break; }}");
                Emit("    }");
                Emit(name == "LastIndexOf"
                    ? $"    if ({ok}) {{ {res} = {i}; }}"
                    : $"    if ({ok}) {{ {res} = {i}; break; }}");
                Emit("}");
                Push(StackKind.I4, "int32_t", res);
                return;
            }

            // (span, value): linear scan. Contains -> bool, IndexOf -> first index,
            // LastIndexOf -> last index (forward scan, no break, keeps the latest);
            // the [Last]IndexOfAnyExcept single-value forms negate the element test
            // (first/last index NOT equal to the value — e.g. ZipFile's
            // EntryFromPath leading-'/' trim). ps[1] must not be a values span:
            // that shape belongs to the membership branches below.
            if (name is "Contains" or "IndexOf" or "LastIndexOf"
                       or "IndexOfAnyExcept" or "LastIndexOfAnyExcept"
                && ps.Length == 2
                && !(ps[1] is { Kind: TypeKind.Class, Class: { } v1Cls }
                     && Comp.GenericDefFullName(v1Cls) is "System.ReadOnlySpan" or "System.Span")
                && canEq)
            {
                string cmp1 = EqLValue(Elem("__e"), Elem("__v"));
                bool exceptOne = name.EndsWith("Except", StringComparison.Ordinal);
                bool lastOne = name.StartsWith("Last", StringComparison.Ordinal);
                var value = Pop();
                string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
                string val = NewTemp(elemCt);
                Emit($"{val} = {Cast(value, elemCt)};");
                string res = NewTemp("int32_t");
                Emit($"{res} = -1;");
                string i = NewTemp("int32_t");
                string hit1 = exceptOne ? $"!({cmp1})" : cmp1;
                Emit($"for ({i} = 0; {i} < {sp}.f__length; {i}++) {{");
                Emit($"    {elemCt} __e = {ElemAt(sp, i)};");
                Emit($"    {elemCt} __v = {val};");
                Emit(lastOne
                    ? $"    if ({hit1}) {{ {res} = {i}; }}"
                    : $"    if ({hit1}) {{ {res} = {i}; break; }}");
                Emit("}");
                Push(StackKind.I4, "int32_t", name == "Contains" ? $"({res} >= 0 ? 1 : 0)" : res);
                return;
            }

            // (span, value): the net9+ single-element StartsWith/EndsWith overloads
            // (e.g. Path.IsPathRooted(span)'s span.StartsWith('/')) — non-empty span +
            // first/last element equality. Same arity as the (span, span) form below,
            // so it must be distinguished by ps[1] NOT being a span and matched first.
            if (name is "StartsWith" or "EndsWith" && ps.Length == 2
                && !(ps[1] is { Kind: TypeKind.Class, Class: { } vspanCls }
                     && Comp.GenericDefFullName(vspanCls) is "System.ReadOnlySpan" or "System.Span")
                && canEq)
            {
                string cmpV = EqLValue(Elem("__e"), Elem("__v"));
                var value = Pop();
                string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
                string val = NewTemp(elemCt);
                Emit($"{val} = {Cast(value, elemCt)};");
                string res = NewTemp("int32_t");
                Emit($"{res} = 0;");
                Emit($"if ({sp}.f__length >= 1) {{");
                string vIdx = name == "StartsWith" ? "0" : $"({sp}.f__length - 1)";
                Emit($"    {elemCt} __e = {ElemAt(sp, vIdx)};");
                Emit($"    {elemCt} __v = {val};");
                Emit($"    {res} = ({cmpV}) ? 1 : 0;");
                Emit("}");
                Push(StackKind.I4, "int32_t", res);
                return;
            }

            // (span, span): SequenceEqual (equal length + all equal), StartsWith /
            // EndsWith (self at least as long, then prefix/suffix compare). The
            // comparer-taking overload — what Enumerable.SequenceEqual routes an array
            // fast-path to — was folded away above.
            if (name is "SequenceEqual" or "StartsWith" or "EndsWith"
                && ps.Length == 2
                && canEq)
            {
                string cmp2 = EqLValue(Elem("__a"), Elem("__b"));
                var other = Pop();
                var self = Pop();
                string sb = SpanValue(other, CppTypes.Of(ps[1]));
                string sa = SpanValue(self, CppTypes.Of(ps[0]));
                string res = NewTemp("int32_t");
                string k = NewTemp("int32_t");
                string lenCond = name == "SequenceEqual"
                    ? $"{sa}.f__length == {sb}.f__length"
                    : $"{sa}.f__length >= {sb}.f__length";
                string aIdx = name == "EndsWith" ? $"({sa}.f__length - {sb}.f__length + {k})" : k;
                Emit($"{res} = ({lenCond}) ? 1 : 0;");
                Emit($"for ({k} = 0; {res} && {k} < {sb}.f__length; {k}++) {{");
                Emit($"    {elemCt} __a = {ElemAt(sa, aIdx)};");
                Emit($"    {elemCt} __b = {ElemAt(sb, k)};");
                Emit($"    if (!({cmp2})) {{ {res} = 0; break; }}");
                Emit("}");
                Push(StackKind.I4, "int32_t", res);
                return;
            }

            // (span, ReadOnlySpan<T> values): [Last]IndexOfAny / [Last]IndexOfAnyExcept —
            // first / last index whose element equals any element of the values span
            // (nested scan; the inner loop breaks on the first values match, the
            // first-index forms break the outer on their first hit, the Last forms keep
            // the latest). The Except forms negate the membership test (element in NONE
            // of the values).
            if (name is "IndexOfAny" or "LastIndexOfAny"
                       or "IndexOfAnyExcept" or "LastIndexOfAnyExcept"
                && ps.Length == 2
                && ps[1] is { Kind: TypeKind.Class, Class: { } vcls }
                && Comp.GenericDefFullName(vcls) is "System.ReadOnlySpan" or "System.Span"
                && canEq)
            {
                string cmpAny = EqLValue(Elem("__e"), Elem("__w"));
                bool exceptSet = name.EndsWith("Except", StringComparison.Ordinal);
                bool lastSet = name.StartsWith("Last", StringComparison.Ordinal);
                string vsp = SpanValue(Pop(), CppTypes.Of(ps[1]));
                string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
                string res = NewTemp("int32_t");
                Emit($"{res} = -1;");
                string i = NewTemp("int32_t");
                string j = NewTemp("int32_t");
                string found = NewTemp("int32_t");
                Emit($"for ({i} = 0; {i} < {sp}.f__length; {i}++) {{");
                Emit($"    {elemCt} __e = {ElemAt(sp, i)};");
                Emit($"    {found} = 0;");
                Emit($"    for ({j} = 0; {j} < {vsp}.f__length; {j}++) {{");
                Emit($"        {elemCt} __w = {ElemAt(vsp, j)};");
                Emit($"        if ({cmpAny}) {{ {found} = 1; break; }}");
                Emit("    }");
                Emit(lastSet
                    ? $"    if ({(exceptSet ? $"!{found}" : found)}) {{ {res} = {i}; }}"
                    : $"    if ({(exceptSet ? $"!{found}" : found)}) {{ {res} = {i}; break; }}");
                Emit("}");
                Push(StackKind.I4, "int32_t", res);
                return;
            }

            // (span, v0, v1[, v2]): [Last]IndexOfAny / [Last]IndexOfAnyExcept — first /
            // last index whose element equals any value — or, for the Except forms,
            // NONE of the values (fixed arity; forward scan, the first-index forms
            // break on the first hit, the Last forms keep the latest — reached by
            // ZipArchiveEntry.ParseFileName's Windows separator scan).
            if (name is "IndexOfAny" or "LastIndexOfAny"
                       or "IndexOfAnyExcept" or "LastIndexOfAnyExcept"
                && ps.Length is 3 or 4
                && canEq)
            {
                int nv = ps.Length - 1;
                var vals = new string[nv];
                for (int vi = nv - 1; vi >= 0; vi--)
                {
                    string vt = NewTemp(elemCt);
                    Emit($"{vt} = {Cast(Pop(), elemCt)};");
                    vals[vi] = vt;
                }
                string sp = SpanValue(Pop(), CppTypes.Of(ps[0]));
                // Materialize the Select with ToArray so this binds to the
                // non-generic string.Join(string, string[]) array overload, which
                // dn2cpp's own string.Join intrinsic supports — rather than the
                // generic Join<string>(string, IEnumerable<string>) over a lazy
                // Select, which dn2cpp can't transpile when building itself
                //. Semantically identical — eager vs lazy join.
                // Every arm of the comparison is a call or a parenthesized ternary,
                // so `||`-joining them needs no wrapping.
                string anyEq = string.Join(" || ",
                    vals.Select(v => EqLValue(Elem("__e"), Elem(v))).ToArray());
                string hitFx = name.EndsWith("Except", StringComparison.Ordinal)
                    ? $"!({anyEq})" : anyEq;
                string res = NewTemp("int32_t");
                Emit($"{res} = -1;");
                string i = NewTemp("int32_t");
                Emit($"for ({i} = 0; {i} < {sp}.f__length; {i}++) {{");
                Emit($"    {elemCt} __e = {ElemAt(sp, i)};");
                Emit(name.StartsWith("Last", StringComparison.Ordinal)
                    ? $"    if ({hitFx}) {{ {res} = {i}; }}"
                    : $"    if ({hitFx}) {{ {res} = {i}; break; }}");
                Emit("}");
                Push(StackKind.I4, "int32_t", res);
                return;
            }

            throw new NotSupportedException(
                $"MemoryExtensions.{name}<{string.Join(",", (object[])methodArgs)}>: this overload/element type "
                + "is not supported (the value/span/fixed-arity forms are covered for every element "
                + "type with a comparison; an element type with none has neither a typed Equals(T) nor "
                + "an Equals(object) override)");
        }

        // MemoryExtensions.Sort<T>(Span<T> [, Comparison<T> | IComparer]) and
        // Sort<TKey,TValue>(Span<TKey> keys, Span<TValue> items [, comparer]) —
        // `span.Sort(...)`. The real body reaches ArraySortHelper -> EventSource -> Calli
        // (untranspilable). A span is a {f__reference, f__length} value whose f__reference
        // points straight at the elements (no array header), so this is the Array.Sort emit
        // over a raw pointer + length: the same element-ordering machinery
        // (ComparerThunk / DefaultOrderCallback), the same key+value helper, the same
        // null-comparer-means-default rule.
        if (declType == "System.MemoryExtensions" && name == "Sort")
        {
            var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
            var ms = Reader.GetMethodSpecification(msh);
            var csig = ms.Method.Kind == HandleKind.MethodDefinition
                ? Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(Comp.SigProvider, ctx)
                : Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(Comp.SigProvider, ctx);
            var ps = csig.ParameterTypes;
            var t = methodArgs[0];

            // (keys, items) is the two-span key+value overload; a trailing IComparer<T> /
            // Comparison<T> is the comparer. Classified off the parameter TYPES, like
            // Array.Sort — the arities collide otherwise.
            bool secondIsSpan = ps.Length >= 2
                && ps[1] is { Kind: TypeKind.Class, Class: { } p1cls }
                && Comp.GenericDefFullName(p1cls) is "System.Span" or "System.ReadOnlySpan";
            bool hasComparer = ps.Length > 1 && IsComparerParam(ps[^1]);
            if (ps.Length != 1 + (secondIsSpan ? 1 : 0) + (hasComparer ? 1 : 0))
                throw new NotSupportedException(
                    $"MemoryExtensions.Sort<{string.Join(",", (object[])methodArgs)}>: this Sort overload "
                    + "shape is not supported yet");

            var cmpE = hasComparer ? Pop() : null;
            string? itemsSp = secondIsSpan ? SpanValue(Pop(), CppTypes.Of(ps[1])) : null;
            string keysSp = SpanValue(Pop(), CppTypes.Of(ps[0]));

            // (Span<TKey> keys, Span<TValue> items[, comparer]): sort an index permutation by
            // key and permute both buffers in lockstep. Keys reach the comparison by address,
            // so one helper serves every key rep — the same one Array.Sort<TKey,TValue> uses.
            if (secondIsSpan)
            {
                var tv = methodArgs[1];
                string kvCall = $"dn2cpp_sort_pair((void*)({keysSp}.f__reference), {SortElemSize(t)}, "
                              + $"(void*)({itemsSp}.f__reference), {SortElemSize(tv)}, "
                              + $"0, {keysSp}.f__length";
                if (cmpE is { } kvc)
                {
                    string kvCmpT = NewTemp("Dn2CppObject*");
                    Emit($"{kvCmpT} = {Cast(kvc, "Dn2CppObject*")};");
                    var kvCls = ps[^1].Class
                        ?? throw new NotSupportedException(
                            $"MemoryExtensions.Sort<{t}>: comparer parameter is not a class type");
                    Emit($"if ({kvCmpT} != nullptr) {{");
                    Emit($"    {kvCall}, (void*){kvCmpT}, {ComparerThunk(t, kvCls, byAddr: true)});");
                    Emit("} else {");
                    EmitDefaultSortPair(t, kvCall);
                    Emit("}");
                    return;
                }
                EmitDefaultSortPair(t, kvCall);
                return;
            }

            // Single-span sort. The comparer-driven helpers are keyed on the element's rep;
            // the comparerless int/long/double/string forms keep their natural-order helpers.
            (string Fn, string Ptr) rep = RepOf(t) switch
            {
                ArrRep.I4 => ("dn2cpp_span_sort_cmp_i4", "int32_t*"),
                ArrRep.Ref => ("dn2cpp_span_sort_cmp_ref", "Dn2CppObject**"),
                _ => ("dn2cpp_span_sort_cmp_n", "void*"),
            };
            string pT = NewTemp(rep.Ptr);
            Emit($"{pT} = ({rep.Ptr})({keysSp}.f__reference);");
            string nT = NewTemp("int32_t");
            Emit($"{nT} = {keysSp}.f__length;");
            // The element-sized helper needs the stride the span's elements are packed at.
            string cmpArgs = RepOf(t) == ArrRep.N
                ? $"{pT}, {nT}, {SortElemSize(t)}"
                : $"{pT}, {nT}";

            if (cmpE is not { } scmp)
            {
                EmitDefaultSpanSort(t, pT, nT, rep.Fn, cmpArgs);
                return;
            }
            string sCmpT = NewTemp("Dn2CppObject*");
            Emit($"{sCmpT} = {Cast(scmp, "Dn2CppObject*")};");
            var sCls = ps[^1].Class
                ?? throw new NotSupportedException(
                    $"MemoryExtensions.Sort<{t}>: comparer parameter is not a class type");
            Emit($"if ({sCmpT} != nullptr) {{");
            Emit($"    {rep.Fn}({cmpArgs}, (void*){sCmpT}, {ComparerThunk(t, sCls)});");
            Emit("} else {");
            EmitDefaultSpanSort(t, pT, nT, rep.Fn, cmpArgs);
            Emit("}");
            return;
        }

        // string.Create<TState>(int length, TState state, SpanAction<char,TState> action)
        // — allocate a string of `length` chars, build a writable Span<char> over its
        // buffer, invoke the SpanAction delegate with (span, state), return the string.
        // The real BCL body uses FastAllocateString (InternalCall) and so can't
        // transpile; we model the whole call. The Span<char> read/write the SpanAction
        // body does (get_Item -> ref char, get_Length) and the delegate body itself
        // already transpile via the real CoreLib IL — only this allocate/span/invoke
        // shell is the intrinsic.
        // string.Format<TArg0[,TArg1[,TArg2]]>(IFormatProvider, CompositeFormat, args…) —
        // the generic half of the net8+ pre-parsed-format family, which is what Roslyn binds
        // a call with 1-3 LOOSE arguments to (the params-array/params-span overloads only
        // win for an explicit array/span or 4+ args). The MethodSpec's decoded signature
        // carries the substituted argument types, so the shape test and the lowering are the
        // same two members the non-generic half uses in TryEmitIntrinsic — one predicate and
        // one builder for the whole family, per the sibling-overload rule
        // (docs/ARCHITECTURE.md §4-B). The reach cut is IsIntrinsicType's, as for every other
        // System.String member.
        if (declType == "System.String" && name == "Format")
        {
            var cfSig = MethodSpecSig(msh, methodArgs);
            if (IsCompositeFormatOverload(cfSig))
            {
                Push(StackKind.Ref, "Dn2CppString*", BuildCompositeFormatExprC(cfSig));
                return;
            }
        }

        if (declType == "System.String" && name == "Create")
        {
            var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
            var ms = Reader.GetMethodSpecification(msh);
            var csig = ms.Method.Kind == HandleKind.MethodDefinition
                ? Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(Comp.SigProvider, ctx)
                : Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(Comp.SigProvider, ctx);
            // The action is the closed SpanAction<char,TState> delegate; its Invoke
            // names the Span<char> and TState C++ types and drives the dginvoke_*.
            var dlg = csig.ParameterTypes[^1].Class
                ?? throw new NotSupportedException(
                    $"string.Create<TState>: action type {csig.ParameterTypes[^1]} is not a delegate");
            var invoke = dlg.Methods.FirstOrDefault(m => m.Name == "Invoke")
                ?? throw new NotSupportedException("string.Create<TState>: SpanAction has no Invoke");
            string spanCpp = CppTypes.Of(invoke.Signature.ParameterTypes[0]); // Span<char>
            string stateCpp = CppTypes.Of(invoke.Signature.ParameterTypes[1]); // TState

            string action = Cast(Pop(), dlg.CppStructName + "*");
            var state = Pop();
            string length = Cast(Pop(), "int32_t");

            string lenT = NewTemp("int32_t");
            string buf = NewTemp("char16_t*");
            string str = NewTemp("Dn2CppString*");
            string span = NewTemp(spanCpp);
            Emit($"{lenT} = {length};");
            Emit($"{str} = dn2cpp_string_create_buffer({lenT}, &{buf});");
            // Span<char> layout is { char16_t* _reference; int _length } — the same
            // value the Span(void*, int) ctor builds, aggregate-initialized inline so
            // string.Create needs no reachable Span ctor.
            Emit($"{span} = {spanCpp}{{ {buf}, {lenT} }};");
            Emit($"dginvoke_{dlg.CppName}({action}, {span}, {Cast(state, stateCpp)});");
            Push(StackKind.Ref, "Dn2CppString*", str);
            return;
        }

        if (TryTokenFreeGenericIntrinsic(declType, name, methodArgs))
            return;

        // Array.Resize<T>(ref T[] array, int newSize): allocate a new array, copy the
        // overlap, and write it back through the ref. The BCL growable-collection
        // path (Stack/Queue/etc. Grow). A null source counts as length 0.
        if (declType == "System.Array" && name == "Resize")
        {
            var t = methodArgs[0];
            var newSize = Pop();
            var arrRef = Pop(); // ref T[] — a pointer to the array slot
            string arrCpp = RepOf(t) switch
            {
                ArrRep.I4 => "Dn2CppArrayI4*",
                ArrRep.Ref => "Dn2CppArrayRef*",
                _ => "Dn2CppArrayN*",
            };
            string slot = $"(*({arrCpp}*)({arrRef.Expr}))";
            string nsz = NewTemp("int32_t");
            // Resize screens newSize itself: the newarr below answers a negative
            // one with OverflowException, .NET with ArgumentOutOfRangeException.
            Emit($"{nsz} = dn2cpp_array_resize_size((int32_t)({newSize.Expr}));");
            EmitNewarr(t, nsz);
            var allocated = Pop();
            string nw = NewTemp(arrCpp);
            Emit($"{nw} = ({arrCpp})({allocated.Expr});");
            string oldLen = $"((Dn2CppArray*){slot})->length";
            string cnt = $"({oldLen} < {nsz} ? {oldLen} : {nsz})";
            // Both operands are already known non-null inside this guard — the
            // source by the test on the line above (Array.Resize accepts a null
            // slot and simply allocates), the destination because it was just
            // allocated — so the copy takes no operand guard.
            Emit($"if ({slot} != nullptr) {{");
            // Same element by construction: the destination was just allocated
            // with the source's own element type.
            EmitArrayCopy(new StackEntry(slot, StackKind.Ref, arrCpp), "0",
                new StackEntry(nw, StackKind.Ref, arrCpp), "0", cnt,
                ArrayOperandKind.Unchecked, ArrayOperandKind.Unchecked,
                sameElementByConstruction: true, elementType: t);
            Emit("}");
            Emit($"{slot} = {nw};");
            return;
        }

        // Array.IndexOf<T> / Array.LastIndexOf<T>(T[] array, T value [, int startIndex
        // [, int count]]): the real body routes through EqualityComparer<T>.Default /
        // vectorized SpanHelpers (reflection + InternalCall we don't model). List<T>'s
        // Contains/IndexOf/LastIndexOf/Remove all funnel here. Emit an inline linear
        // scan with element-typed equality instead (the IL2CPP-style devirtualized
        // compare).
        if (declType == "System.Array" && name is "IndexOf" or "LastIndexOf")
        {
            EmitArrayIndexOf(msh, name, methodArgs[0]);
            return;
        }

        // Array.Fill<T>(T[] array, T value [, int startIndex, int count]): the real body
        // vectorizes via Span<T>.Fill -> SpanHelpers (Unsafe.BitCast). Emit a scalar
        // element-store loop instead (whole-array or the index/count range).
        if (declType == "System.Array" && name == "Fill")
        {
            EmitArrayFill(msh, methodArgs[0]);
            return;
        }

        // Array.Sort<T> / Array.Sort<TKey,TValue> / Array.Reverse<T> / Array.BinarySearch<T>
        // — every generic overload. The real bodies route through ArraySortHelper<T>.Default
        // (CreateInstanceForAnotherGenericParameter: a runtime type-loader operation with no
        // static lowering) and EventSource/calli, so all of them are emitted here; see
        // EmitArraySort / EmitArrayBinarySearch for the overload classification.
        if (declType == "System.Array" && name is "Sort" or "Reverse")
        {
            EmitArraySort(msh, name, methodArgs);
            return;
        }
        if (declType == "System.Array" && name == "BinarySearch")
        {
            EmitArrayBinarySearch(msh, methodArgs);
            return;
        }

        // string.Join<T>(separator, IEnumerable<T>) — the form `string.Join(",", arr)`
        // binds to when the elements are a value type (int[] -> IEnumerable<int>).
        // Supported over arrays (the dominant case) and over a List<T>: a
        // List<T> isn't a Dn2CppArray, so materialize the live prefix of its backing
        // array (_items, same repr; iterate _size via the count-aware `_n` helpers).
        // The element kind picks the formatting; the separator may be string or char.
        if (declType == "System.String" && name == "Join")
        {
            EmitStringJoinGeneric(methodArgs[0]);
            return;
        }

        // StringBuilder.AppendJoin<T>(string|char separator, IEnumerable<T>) —
        // the same composition as string.Join<T> (the shared emitter below),
        // then appended to the builder (fluent).
        if (declType == "System.Text.StringBuilder" && name == "AppendJoin")
        {
            EmitStringJoinGeneric(methodArgs[0]);
            var joined = Pop();
            string ajsb = Cast(Pop(), "Dn2CppStringBuilder*");
            Push(StackKind.Ref, "Dn2CppStringBuilder*",
                $"dn2cpp_sb_append_str({ajsb}, {Cast(joined, "Dn2CppString*")})");
            return;
        }

        // string.Concat<T>(IEnumerable<T>) over a List<T>. Reference
        // elements (e.g. List<string>) format each via Object.ToString. Value
        // elements (List<int>/<long>/<double>) reuse the count-aware Join helpers
        // with an EMPTY separator — concatenation is Join(""), and each element's
        // invariant formatting equals its ToString, so no per-element boxing is
        // needed. The non-generic Concat(IEnumerable<string>) binding is intercepted
        // in EmitIntrinsic's switch.
        if (declType == "System.String" && name == "Concat"
            && StackDepth > 0 && TryListBacking(Top) is { Elem: { } cElem })
        {
            var lb = TryListBacking(Pop())!.Value;
            string concat = RepOf(cElem) switch
            {
                // Unsigned 32/64-bit elements format unsigned, like the Join
                // lowering above (matched before the ArrRep buckets).
                _ when cElem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 } =>
                    $"dn2cpp_string_join_u4_n(dn2cpp_string_literal(u\"\", 0), (Dn2CppArrayI4*)({lb.Items}), {lb.Count})",
                _ when cElem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 } =>
                    $"dn2cpp_string_join_u8_n(dn2cpp_string_literal(u\"\", 0), (Dn2CppArrayN*)({lb.Items}), {lb.Count})",
                ArrRep.Ref => $"dn2cpp_string_concat_objects_n((Dn2CppArrayRef*)({lb.Items}), {lb.Count})",
                ArrRep.I4 => $"dn2cpp_string_join_i4_n(dn2cpp_string_literal(u\"\", 0), (Dn2CppArrayI4*)({lb.Items}), {lb.Count})",
                _ when cElem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } =>
                    $"dn2cpp_string_join_i8_n(dn2cpp_string_literal(u\"\", 0), (Dn2CppArrayN*)({lb.Items}), {lb.Count})",
                _ when cElem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } =>
                    $"dn2cpp_string_join_r8_n(dn2cpp_string_literal(u\"\", 0), (Dn2CppArrayN*)({lb.Items}), {lb.Count})",
                _ when cElem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } =>
                    $"dn2cpp_string_join_ch_n(dn2cpp_string_literal(u\"\", 0), (Dn2CppArrayN*)({lb.Items}), {lb.Count})",
                _ => throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: string.Concat<{cElem}> " +
                    "element type is not supported yet (reference / int / long / double / char only)"),
            };
            Push(StackKind.Ref, "Dn2CppString*", concat);
            return;
        }

        // string.Concat<T>(IEnumerable<T>) over a concrete collection that is not a
        // List<T> backing (SortedSet, Sorted*.Keys/.Values): enumerate via the
        // interface with an EMPTY separator (Concat == Join("")). Gated AFTER the
        // List fast path above so List<T> keeps its backing-array materialization,
        // and pre-checked so a fallthrough doesn't pop the operand.
        if (declType == "System.String" && name == "Concat"
            && methodArgs.Length == 1 && StackDepth > 0
            && IsConcreteEnumerableOperand(Top)
            && CanEmitEnumerableJoin(methodArgs[0]))
        {
            TryEmitEnumerableJoin(Pop(), methodArgs[0], null);
            return;
        }

        // string.Concat<T>(IEnumerable<T>) over a bare interface operand (array or
        // managed collection at runtime) — runtime-discriminated dual path.
        if (declType == "System.String" && name == "Concat"
            && methodArgs.Length == 1 && StackDepth > 0
            && IsBareCollectionInterface(Top)
            && CanEmitEnumerableJoin(methodArgs[0]) && ArrayJoinExpr(methodArgs[0], "$", null) is not null)
        {
            TryEmitBareInterfaceJoin(Pop(), methodArgs[0], null);
            return;
        }

        // Task.FromResult<T>(T) -> a completed Task<T> carrying the value.
        if (declType == "System.Threading.Tasks.Task" && name == "FromResult")
        {
            var v = Pop();
            Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_from_result({EmitTaskResultStore(v)})");
            return;
        }

        // Task.FromException<TResult>(Exception) -> a pre-completed faulted Task<T>
        // carrying the exception (the result slot is never read — it faults). Same
        // runtime helper as the non-generic FromException in EmitIntrinsic.
        if (declType == "System.Threading.Tasks.Task" && name == "FromException")
        {
            var ex = Pop();
            Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_from_exception({Cast(ex, "Dn2CppObject*")})");
            return;
        }

        // Task.FromCanceled<TResult>(CancellationToken) -> a pre-completed CANCELED
        // Task<TResult> (the result slot is never read — awaiting it throws
        // OperationCanceledException). ValueTask.FromCanceled<TResult> is the same
        // task wrapped in the {task} struct. Same runtime helper as the non-generic
        // forms in EmitIntrinsic; the token's canceled state is not re-validated
        // (real .NET's ArgumentOutOfRangeException on a non-canceled token is not
        // modeled — the guard callers only take the branch on a canceled token).
        if (declType == "System.Threading.Tasks.Task" && name == "FromCanceled")
        {
            Pop(); // CancellationToken
            EmitCanceledExcRegistration();
            Push(StackKind.Ref, "Dn2CppTask*", "dn2cpp_task_from_canceled()");
            return;
        }
        if (declType == "System.Threading.Tasks.ValueTask" && name == "FromCanceled")
        {
            Pop(); // CancellationToken
            EmitCanceledExcRegistration();
            Push(StackKind.Struct, "Dn2CppTaskAwaiter", "Dn2CppTaskAwaiter{ dn2cpp_task_from_canceled() }");
            return;
        }

        // ValueTask.FromException<TResult>(Exception) -> a pre-completed faulted
        // ValueTask<TResult> (the result slot is never read — it faults); the same
        // helper as Task.FromException, wrapped in the {task} struct. Reached as
        // the catch arm of MemoryStream.ReadAsync(Memory<byte>, CancellationToken).
        if (declType == "System.Threading.Tasks.ValueTask" && name == "FromException")
        {
            var ex = Pop();
            Push(StackKind.Struct, "Dn2CppTaskAwaiter",
                $"Dn2CppTaskAwaiter{{ dn2cpp_task_from_exception({Cast(ex, "Dn2CppObject*")}) }}");
            return;
        }

        // ValueTask.FromResult<TResult>(TResult) -> a pre-completed successful
        // ValueTask<TResult> carrying the value; the mirror of Task.FromResult,
        // wrapped in the {task} struct. Reached as the synchronous-completion arm
        // of OSFileStreamStrategy.ReadAsync / DeflateManagedStream.ReadAsyncInternal.
        if (declType == "System.Threading.Tasks.ValueTask" && name == "FromResult")
        {
            var v = Pop();
            Push(StackKind.Struct, "Dn2CppTaskAwaiter",
                $"Dn2CppTaskAwaiter{{ dn2cpp_task_from_result({EmitTaskResultStore(v)}) }}");
            return;
        }

        // ValueTask.DangerousCreateFromTypedValueTask<TResult>(ValueTask<TResult>) —
        // an unchecked ValueTask<T> -> ValueTask re-brand (Stream.ReadExactlyAsync's
        // return bridging). Both forms are the same {task} struct on the dn2cpp
        // model, so it is an identity copy.
        if (declType == "System.Threading.Tasks.ValueTask" && name == "DangerousCreateFromTypedValueTask")
        {
            var vt = Pop();
            Push(StackKind.Struct, "Dn2CppTaskAwaiter", vt.Expr);
            return;
        }

        // RuntimeHelpers.IsReferenceOrContainsReferences<T> — the JIT folds this
        // to a constant; T is closed here, so we do the same. Span<T>/array code
        // branches on it; folding picks the right (transpilable) path.
        if (declType == "System.Runtime.CompilerServices.RuntimeHelpers"
            && name == "IsReferenceOrContainsReferences")
        {
            Push(StackKind.I4, "int32_t", methodArgs[0].ContainsGcReferences() ? "1" : "0");
            return;
        }

        // RuntimeHelpers.IsBitwiseEquatable<T> — the SIMD memcmp gate
        // (MemoryExtensions.Count/SequenceEqual/IndexOf branch on it to choose a
        // vectorized byte/short scan). Fold to false so the scalar element-wise path
        // is taken: always correct (it falls back to EqualityComparer<T>), and it keeps
        // the SIMD subtree dead — the same carve-out lever as Vector.IsHardwareAccelerated.
        if (declType == "System.Runtime.CompilerServices.RuntimeHelpers"
            && name == "IsBitwiseEquatable")
        {
            Push(StackKind.I4, "int32_t", "0");
            return;
        }

        // ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(object value, argName):
        // the List<T> IList.Add null gate. Nulls are illegal exactly when T is a
        // non-nullable value type; T is closed here, so fold the gate and keep
        // only the null check when it matters.
        if (declType == "System.ThrowHelper" && name == "IfNullAndNullsAreIllegalThenThrow")
        {
            Pop(); // the ExceptionArgument name enum
            var nullGated = Pop();
            if (IsValueTypeStatic(methodArgs[0]) && NullableLayout(methodArgs[0]) is null)
                Emit($"if ({Cast(nullGated, "Dn2CppObject*")} == nullptr) dn2cpp_throw_argument_null();");
            return;
        }

        // Generic ThrowHelper closures ([DoesNotReturn], e.g.
        // ThrowKeyNotFoundException<TKey>(TKey)). Same as the non-generic ones:
        // drop the args and raise a catchable managed trap — and, for the same reason as
        // there, `ThrowIf*` is excluded: a GUARD returns normally on the happy path, so
        // trapping one drops the test. No generic guard exists on this type today, so the
        // exclusion sends one to the loud unsupported-shape throw instead of miscompiling it
        // (the non-generic twin's arm carries the full argument).
        if (declType == "System.ThrowHelper"
            && name.StartsWith("Throw") && !name.StartsWith("ThrowIf"))
        {
            var ms = Reader.GetMethodSpecification(msh);
            var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
            var msig = ms.Method.Kind == HandleKind.MethodDefinition
                ? Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(Comp.SigProvider, ctx)
                : Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(Comp.SigProvider, ctx);
            // The two whose message NAMES the key: .NET renders it into the
            // sentence, so the operand is formatted here rather than dropped. T is closed
            // at this point, so the hole formats exactly as an interpolation of it would.
            string? keyed = name switch
            {
                "ThrowKeyNotFoundException" => "DN2CPP_SR_KEY_NOT_FOUND_WITH_KEY",
                "ThrowAddingDuplicateWithKeyArgumentException" => "DN2CPP_SR_ADDING_DUPLICATE_WITH_KEY",
                _ => null,
            };
            // Two conservative conditions, both of which drop back to the type's default
            // message rather than to a failure or a wrong one:
            //  * a struct key without a ToString override has no interpolation lowering and
            //    FormatInterpolationHole's refusal is LOUD — a transpile must not fail over
            //    a diagnostic;
            //  * a NUMERIC canonical placeholder stands for a whole layout group, so its
            //    signedness is not the group members'. A canonical REF placeholder is fine
            //    — every reference formats through the same virtual ToString.
            if (keyed is not null && msig.ParameterTypes.Length == 1
                && msig.ParameterTypes[0] is var keyType
                && (CppTypes.KindOf(keyType) == StackKind.Ref
                    || (!keyType.IsCanonPlaceholder
                        && (keyType.Kind == TypeKind.Primitive
                            || keyType is { Kind: TypeKind.Class, Class.IsEnum: true }))))
            {
                string keyStr = FormatInterpolationHole(msig.ParameterTypes[0], Pop(), "nullptr");
                Emit($"dn2cpp_throw_sr1(&{ThrowHelperTypeInfo(name)}, {keyed}, {keyStr});");
                return;
            }
            for (int i = 0; i < msig.ParameterTypes.Length; i++)
                Pop();
            Emit(ThrowHelperTrap(name));
            return;
        }

        // StringBuilder.AppendInterpolatedStringHandler.AppendFormatted<T> /
        // AppendFormattedWithTempSpace<T> — the runtime-formatted hole of
        // `sb.Append($"...{value}...")`. The handler is modeled as the underlying
        // StringBuilder pointer (CoreIntrinsics nested map), so format T with the same
        // logic as the DefaultInterpolatedStringHandler hole (FormatInterpolationHole)
        // and append the result straight to the builder. Cutting the real generic body
        // (Compilation.ResolveCallTarget) collapses the Enum.TryFormatUnconstrained ->
        // EnumInfo/Number-format cascade. The `,N` alignment is honored
        // by spilling into a DISH and padding; in practice these holes carry no alignment.
        if (declType == "AppendInterpolatedStringHandler"
            && name is "AppendFormatted" or "AppendFormattedWithTempSpace")
        {
            // Layout mirrors the DISH overloads: T then optional (int alignment) and/or
            // (string format), with WithTempSpace always carrying the 3-arg shape.
            var pts = MethodSpecSig(msh, methodArgs).ParameterTypes;
            string formatExpr = "nullptr";
            string alignExpr = "0";
            if (pts.Length == 3)
            {
                formatExpr = Cast(Pop(), "Dn2CppString*"); // format (string)
                alignExpr = Pop().Expr;                    // alignment (int)
            }
            else if (pts.Length == 2)
            {
                if (pts[1].IsString)
                    formatExpr = Cast(Pop(), "Dn2CppString*");
                else
                    alignExpr = Pop().Expr;
            }
            var val = Pop();
            var self = Pop(); // managed pointer to the handler local (a Dn2CppStringBuilder*)
            var t = methodArgs[0];
            string valStr = FormatInterpolationHole(t, val, formatExpr);
            // The handler local holds the StringBuilder pointer; `self` is its address.
            // For a 0 alignment we append directly; a non-zero alignment pads through a
            // throwaway DISH (the BCL pads the value before appending — same result).
            if (alignExpr == "0")
                Emit($"dn2cpp_sb_append_str(*((Dn2CppStringBuilder**)({self.Expr})), {valStr});");
            else
            {
                string isb = NewTemp("Dn2CppISB");
                Emit($"{isb} = dn2cpp_isb_new(0, 1);");
                Emit($"dn2cpp_isb_append_aligned(&{isb}, {valStr}, {alignExpr});");
                Emit($"dn2cpp_sb_append_str(*((Dn2CppStringBuilder**)({self.Expr})), dn2cpp_isb_to_string(&{isb}));");
            }
            return;
        }

        // DefaultInterpolatedStringHandler.AppendFormatted<T>(T, [int alignment],
        // [string format]) — the generic overload for value-type holes. Format
        // the value (honoring the `:fmt` specifier) by closed type argument, then
        // append it padded to the `,N` alignment ( inc1 holes; inc2 adds the
        // alignment/format components).
        if (declType == "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler"
            && name == "AppendFormatted")
        {
            var ms = Reader.GetMethodSpecification(msh);
            var ctx = new GenericContext(System.Array.Empty<TypeDesc>(), methodArgs);
            var msig = ms.Method.Kind == HandleKind.MethodDefinition
                ? Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(Comp.SigProvider, ctx)
                : Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(Comp.SigProvider, ctx);

            // The optional args follow T as either (int alignment), (string
            // format), or (int alignment, string format). Pop them in reverse.
            var pts = msig.ParameterTypes;
            string formatExpr = "nullptr";
            string alignExpr = "0";
            if (pts.Length == 3)
            {
                formatExpr = Cast(Pop(), "Dn2CppString*"); // format (string)
                alignExpr = Pop().Expr;                    // alignment (int)
            }
            else if (pts.Length == 2)
            {
                if (pts[1].IsString)
                    formatExpr = Cast(Pop(), "Dn2CppString*");
                else
                    alignExpr = Pop().Expr;
            }
            var val = Pop();
            var self = Pop(); // managed pointer to the handler

            var t = methodArgs[0];
            string valStr = FormatInterpolationHole(t, val, formatExpr);
            Emit($"dn2cpp_isb_append_aligned((Dn2CppISB*)({self.Expr}), {valStr}, {alignExpr});");
            return;
        }

        // Task.WhenAll<TResult>(Task<TResult>[]) -> Task<TResult[]> that completes
        // once every input task completes (or faults with the first input fault).
        // The cooperative scheduler joins them; the result element kind selects how
        // each input's raw result slot is written into the array.
        if (declType == "System.Threading.Tasks.Task" && name == "WhenAll")
        {
            var tres = methodArgs[0];
            var rep = RepOf(tres);
            bool isStruct = CppTypes.KindOf(tres) == StackKind.Struct;
            if (rep == ArrRep.N && !isStruct && !(tres.Kind == TypeKind.Primitive
                && tres.Primitive is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.Double))
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Task.WhenAll<{tres}> " +
                    "result element kind is not supported yet (int / long / double / reference / struct only)");
            // The operand is a single Task<TResult>[] or IEnumerable<Task<TResult>>;
            // PopTaskArrayOperand normalizes both to a Dn2CppArrayRef*. Decode
            // the closed signature so it can tell the array shape from the enumerable.
            var asig = DecodeGenericCallSignature(msh, methodArgs);
            string operand = PopTaskArrayOperand(asig.ParameterTypes);
            // The TResult[] handle rides on the join state: the array is built inside
            // the completion callback, so there is no call site left to retag.
            // PreciseArrayTypeInfoExpr taints a canonical trial, which is right — a
            // shared body must not bake one instantiation's array identity in.
            // NoteArrayEnumerableElement, not NoteArrayElementType: a precise handle whose
            // SZArray interface map is unwired is worse than the shared one (see
            // PrimArrayTypeInfoExpr).
            Comp.NoteArrayEnumerableElement(tres);
            string resArrTi = PreciseArrayTypeInfoExpr(tres);
            // A struct result is heap-boxed per input; the join copies each into a
            // value array of sizeof(TStruct) elements.
            if (isStruct)
            {
                Push(StackKind.Ref, "Dn2CppTask*",
                    $"dn2cpp_task_when_all_struct({operand}, (int32_t)sizeof({CppTypes.Of(tres)}), {resArrTi})");
                return;
            }
            string kind = rep switch
            {
                ArrRep.I4 => "DN2CPP_WHENALL_I4",
                ArrRep.Ref => "DN2CPP_WHENALL_REF",
                _ => "DN2CPP_WHENALL_N8",
            };
            Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_when_all({operand}, {kind}, {resArrTi})");
            return;
        }

        // Task.WhenAny<TResult>(...) -> Task<Task<TResult>> completing with the first
        // input task to finish. Unlike WhenAll there is no result element kind (the
        // result is always a Task reference). Two overload shapes reach here: the
        // params/array form `Task<TResult>[]` (one SZArray operand), and the.NET 9+
        // fixed `Task<TResult>, Task<TResult>` form (N separate Task operands, which
        // we gather into a temp ref array). The IEnumerable<Task<TResult>> overload
        // (a single non-array collection operand) is not handled yet.
        if (declType == "System.Threading.Tasks.Task" && name == "WhenAny")
        {
            var wsig = DecodeGenericCallSignature(msh, methodArgs);
            Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_when_any({PopTaskArrayOperand(wsig.ParameterTypes)})");
            return;
        }

        // Task.Run<TResult>(Func<TResult> [, CancellationToken]) -> Task<TResult>,
        // dispatched to the real worker pool. The result kind selects the run thunk;
        // the worker completes the task and the async↔thread bridge resumes the awaiter
        // on its own thread. Task.Run<TResult>(Func<Task<TResult>>) is the async-lambda unwrap
        // overload (the delegate returns Task<TResult>): one runtime entry point settles
        // the outer Task<TResult> with the inner task's result/fault/cancellation — the
        // 8-byte result slot copies opaquely, the awaiter reinterprets it by TResult.
        if (declType == "System.Threading.Tasks.Task" && name == "Run")
        {
            var tres = methodArgs[0];
            var rsig = DecodeGenericCallSignature(msh, methodArgs);
            bool unwrap = DelegateReturnsTask(rsig.ParameterTypes[0]);
            if (rsig.ParameterTypes.Length == 2)
                Pop(); // trailing CancellationToken (ignored)
            var del = Pop(); // Func<TResult> or Func<Task<TResult>>
            if (unwrap)
            {
                Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_run_unwrap((Dn2CppObject*)({del.Expr}))");
                return;
            }
            // A value-type result rides a boxing trampoline (TaskStructResultThunk): the
            // struct does not fit the 8-byte slot, so the worker boxes it and the awaiter
            // reads it back by TResult, exactly like the async-builder SetResult path.
            if (CppTypes.KindOf(tres) == StackKind.Struct)
            {
                Push(StackKind.Ref, "Dn2CppTask*",
                    $"dn2cpp_task_run_struct((Dn2CppObject*)({del.Expr}), {TaskStructResultThunk(CppTypes.Of(tres))})");
                return;
            }
            string fn = CppTypes.KindOf(tres) switch
            {
                StackKind.I8 => "dn2cpp_task_run_i8",
                StackKind.R8 => tres is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }
                    ? "dn2cpp_task_run_r4" : "dn2cpp_task_run_r8",
                StackKind.I4 => "dn2cpp_task_run_i4",
                StackKind.Ref => "dn2cpp_task_run_ref",
                _ => throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Task.Run<{tres}> " +
                    "result kind not supported yet (int/long/float/double/reference only)"),
            };
            Push(StackKind.Ref, "Dn2CppTask*", $"{fn}((Dn2CppObject*)({del.Expr}))");
            return;
        }

        // Task(<T>).ContinueWith<TNewResult>(Func<Task(<T>), TNewResult> [, hints...])
        // -> Task<TNewResult>: register the continuation on the antecedent and settle
        // the returned task with the delegate's result once it completes (see
        // dn2cpp_task_continue_with — the delegate runs with the settled antecedent on
        // the registering thread's scheduler). The result kind picks the invoke thunk
        // like Task.Run<TResult>; the signature's !0 (Task<T>'s T) is closed by the
        // MemberRef's TypeSpec parent context (a TypeRef parent — the non-generic Task
        // receiver — has none). TaskContinuationOptions travels to the runtime node
        // (PopContinuationHints), the same as on the non-generic overloads: it is a
        // run/don't-run filter, not a hint, so it may not be dropped. The
        // Func<Task, object?, TNewResult> state form is not supported yet, mirroring
        // StartNew<TResult>'s carve-out.
        if (declType == "System.Threading.Tasks.Task" && name == "ContinueWith")
        {
            var tnew = methodArgs[0];
            var cwms = Reader.GetMethodSpecification(msh);
            if (cwms.Method.Kind != HandleKind.MemberReference)
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Task.ContinueWith<{tnew}> " +
                    "reached as a MethodDef (intra-corelib) is not supported");
            var cwmr = Reader.GetMemberReference((MemberReferenceHandle)cwms.Method);
            var cwTypeArgs = cwmr.Parent.Kind == HandleKind.TypeSpecification
                && Reader.GetTypeSpecification((TypeSpecificationHandle)cwmr.Parent)
                       .DecodeSignature(Comp.SigProvider, Method.Context) is { Kind: TypeKind.Class, Class: { } cwp }
                ? cwp.Context.TypeArgs
                : System.Array.Empty<TypeDesc>();
            var cwsig = cwmr.DecodeMethodSignature(Comp.SigProvider, new GenericContext(cwTypeArgs, methodArgs));
            if (cwsig.ParameterTypes[0] is not { Kind: TypeKind.Class, Class: { } cwDel }
                || cwDel.Context.TypeArgs.Length != 2)
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Task.ContinueWith<{tnew}> " +
                    "with a state argument (Func<Task, object?, TNewResult>) is not supported yet");
            string cwOpts = PopContinuationHints(cwsig.ParameterTypes, 1);
            // A value-type continuation result rides the boxing trampoline (the ContinueWith
            // mirror of Task.Run<TStruct>); the delegate takes the settled antecedent.
            if (CppTypes.KindOf(tnew) == StackKind.Struct)
            {
                var cwds = Pop(); // Func<Task(<T>), TNewResult>
                var cwts = Pop(); // the antecedent task receiver
                Push(StackKind.Ref, "Dn2CppTask*",
                    $"dn2cpp_task_continue_with_struct((Dn2CppTask*)({cwts.Expr}), " +
                    $"(Dn2CppObject*)({cwds.Expr}), {TaskStructContWithThunk(CppTypes.Of(tnew))}, {cwOpts})");
                return;
            }
            string cwKind = CppTypes.KindOf(tnew) switch
            {
                StackKind.I8 => "DN2CPP_CONTWITH_I8",
                StackKind.R8 => tnew is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }
                    ? "DN2CPP_CONTWITH_R4" : "DN2CPP_CONTWITH_R8",
                StackKind.I4 => "DN2CPP_CONTWITH_I4",
                StackKind.Ref => "DN2CPP_CONTWITH_REF",
                _ => throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Task.ContinueWith<{tnew}> " +
                    "result kind not supported yet (int/long/float/double/reference only)"),
            };
            var cwd = Pop(); // Func<Task(<T>), TNewResult>
            var cwt = Pop(); // the antecedent task receiver
            Push(StackKind.Ref, "Dn2CppTask*",
                $"dn2cpp_task_continue_with((Dn2CppTask*)({cwt.Expr}), (Dn2CppObject*)({cwd.Expr}), "
                + $"nullptr, {cwKind}, {cwOpts})");
            return;
        }

        // TaskFactory.StartNew<TResult>(Func<TResult> [, CancellationToken]
        // [, TaskCreationOptions] [, TaskScheduler]) -> Task<TResult>, dispatched to
        // the same worker pool as Task.Run<TResult> (the token/options/scheduler
        // arguments are scheduling hints only — popped and ignored). Unlike Task.Run,
        // StartNew never unwraps an async delegate: real .NET returns Task<Task<T>>.
        // A Task-returning delegate takes the dedicated dn2cpp_task_run_nested path —
        // the worker settles the outer task with the inner task pointer as soon as
        // the delegate returns, then drains its own scheduler until the inner settles
        // (a suspended async lambda parks its timers/continuations on that worker's
        // thread-local scheduler, which no other thread can drive).
        // An unsupported result kind throws NotSupported, which under canonical
        // shared generics taints the candidate to per-instantiation fallback.
        if (declType == "System.Threading.Tasks.TaskFactory" && name == "StartNew")
        {
            var tres = methodArgs[0];
            var fsig = DecodeGenericCallSignature(msh, methodArgs);
            if (fsig.ParameterTypes[0] is not { Kind: TypeKind.Class, Class.Context.TypeArgs.Length: 1 })
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: TaskFactory.StartNew<{tres}> " +
                    "with a state argument (Func<object?, TResult>) is not supported yet");
            for (int i = fsig.ParameterTypes.Length - 1; i >= 1; i--)
                Pop(); // CancellationToken / TaskCreationOptions / TaskScheduler hints
            var fdel = Pop(); // Func<TResult>
            Pop();            // receiver — the Task.Factory nullptr sentinel
            if (DelegateReturnsTask(fsig.ParameterTypes[0]))
            {
                Push(StackKind.Ref, "Dn2CppTask*", $"dn2cpp_task_run_nested((Dn2CppObject*)({fdel.Expr}))");
                return;
            }
            // A value-type result rides the same boxing trampoline as Task.Run<TStruct>.
            if (CppTypes.KindOf(tres) == StackKind.Struct)
            {
                Push(StackKind.Ref, "Dn2CppTask*",
                    $"dn2cpp_task_run_struct((Dn2CppObject*)({fdel.Expr}), {TaskStructResultThunk(CppTypes.Of(tres))})");
                return;
            }
            string ffn = CppTypes.KindOf(tres) switch
            {
                StackKind.I8 => "dn2cpp_task_run_i8",
                StackKind.R8 => tres is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }
                    ? "dn2cpp_task_run_r4" : "dn2cpp_task_run_r8",
                StackKind.I4 => "dn2cpp_task_run_i4",
                StackKind.Ref => "dn2cpp_task_run_ref",
                _ => throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: TaskFactory.StartNew<{tres}> " +
                    "result kind not supported yet (int/long/float/double/reference only)"),
            };
            Push(StackKind.Ref, "Dn2CppTask*", $"{ffn}((Dn2CppObject*)({fdel.Expr}))");
            return;
        }

        // Parallel.ForEach<TSource>(TSource[] source, [ParallelOptions,] Action<TSource>[,
        // ParallelLoopState] body): enumerate a real array source, or a List<T>'s backing
        // array, in parallel. The body's generic arg count picks the plain per-element
        // thunk (1 arg; always pushes the completed ParallelLoopResult) or the Break/Stop-
        // aware "_state" thunk (2 args, second arg ParallelLoopState; pushes a real
        // ParallelLoopResult). A List<T> source uses its live count (TryListBacking), not
        // the backing array's capacity, via the count-aware "_n" runtime entry points (same
        // convention as string.Join/Concat over List<T>). The element representation
        // (RepOf) + element primitive pick the typed runtime thunk and the array cast.
        // Carve-outs (loud NotSupported): a non-array, non-List<T> IEnumerable<T> source
        // (LINQ, SortedSet, ...), sub-word/struct/IntPtr element arrays, and the
        // thread-local-state overloads (a 3rd generic arg).
        if (declType == "System.Threading.Tasks.Parallel" && name == "ForEach")
        {
            var rsig = DecodeGenericCallSignature(msh, methodArgs);
            var elem = methodArgs[0];
            bool hasOptions = rsig.ParameterTypes.Length == 3 && IsParallelOptionsType(rsig.ParameterTypes[1]);
            int bodyIdx = hasOptions ? 2 : 1;
            if ((rsig.ParameterTypes.Length != 2 && !hasOptions)
                || rsig.ParameterTypes[bodyIdx] is not { Kind: TypeKind.Class, Class: { } feBody })
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {ParallelForEachBodyShapeError()}");
            bool withState = feBody.Context.TypeArgs.Length switch
            {
                1 => false,
                2 when IsParallelLoopStateType(feBody.Context.TypeArgs[1]) => true,
                _ => throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {ParallelForEachBodyShapeError()}"),
            };
            var feBodyArg = Pop();
            var feOptions = hasOptions ? Pop() : null;
            var feSrc = Pop();
            string srcExpr, countArg, suffix;
            if (feSrc.CppType.StartsWith("Dn2CppArray", StringComparison.Ordinal))
                (srcExpr, countArg, suffix) = (feSrc.Expr, "", "");
            else if (TryListBacking(feSrc) is { } lb)
                (srcExpr, countArg, suffix) = (lb.Items, $", {lb.Count}", "_n");
            else
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: Parallel.ForEach source must be a " +
                    $"concrete array or List<{elem}> (got {feSrc.CppType}); a non-array, non-List<T> " +
                    $"IEnumerable<{elem}> source is a follow-up");
            (string fn, string arrType) = RepOf(elem) switch
            {
                ArrRep.Ref => ("dn2cpp_parallel_foreach_ref", "Dn2CppArrayRef*"),
                ArrRep.I4 => ("dn2cpp_parallel_foreach_i4", "Dn2CppArrayI4*"),
                _ => elem switch // ArrRep.N — only the long / double / float primitive widths
                {
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 }
                        => ("dn2cpp_parallel_foreach_i8", "Dn2CppArrayN*"),
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double }
                        => ("dn2cpp_parallel_foreach_r8", "Dn2CppArrayN*"),
                    { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single }
                        => ("dn2cpp_parallel_foreach_r4", "Dn2CppArrayN*"),
                    _ => throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: Parallel.ForEach over T={elem} " +
                        "element arrays is a follow-up (reference / int|uint|enum / long|ulong / float / " +
                        "double element arrays are supported; sub-word, value-struct and IntPtr are not)"),
                },
            };
            string maxDop = feOptions is { } fo ? ParallelOptionsMaxDop(fo) : "-1";
            string srcCast = $"({arrType})({srcExpr}){countArg}";
            if (withState)
            {
                string res = NewTemp("Dn2CppParallelLoopResult");
                Emit($"{res} = {fn}_state{suffix}({srcCast}, (Dn2CppObject*)({feBodyArg.Expr}), {maxDop});");
                Push(StackKind.Struct, "Dn2CppParallelLoopResult", res);
            }
            else
            {
                Emit($"{fn}{suffix}({srcCast}, (Dn2CppObject*)({feBodyArg.Expr}), {maxDop});");
                Push(StackKind.Struct, "Dn2CppParallelLoopResult", "Dn2CppParallelLoopResult{ 1, 0, -1 }");
            }
            return;
        }

        // Interlocked.CompareExchange<T>/Exchange<T> — the MethodSpec forms. A
        // reference T (the field-like `event` accessors) routes to the pointer
        // swap helpers; .NET 9+ also exposes the generic form over primitives —
        // CoreLib itself binds e.g. Interlocked.Exchange<int> in DeflateStream's
        // AsyncOperationStarting. The shared emitter (EmitInterlockedSwap)
        // dispatches on T's DECLARED atomic width — sub-word T (bool/byte/short/
        // char) swaps 1/2 bytes, enums swap at their underlying primitive's
        // width, float/double via a bitwise integer atomic — identical to the
        // non-generic overloads' lowering.
        if (declType == "System.Threading.Interlocked" && name == "CompareExchange")
        {
            var comparand = Pop();
            var value = Pop();
            var loc = Pop();
            EmitInterlockedSwap(methodArgs[0], loc, value, comparand, name);
            return;
        }
        if (declType == "System.Threading.Interlocked" && name == "Exchange")
        {
            var value = Pop();
            var loc = Pop();
            EmitInterlockedSwap(methodArgs[0], loc, value, comparand: null, name);
            return;
        }

        // Volatile.Read<T>(ref T) / Write<T>(ref T, T), the `where T : class`
        // reference overloads lock-free readers use (e.g. PEReader's cached section
        // blocks). The non-generic primitive overloads arrive as a MemberRef and are
        // handled in EmitIntrinsic; this MethodSpec form is the only place the generic
        // overload lands, so the two paths never overlap. methodArgs[0] is T (the ref's
        // element); route to the same seq_cst atomic load/store, which already covers
        // both reference and primitive element kinds.
        if (declType == "System.Threading.Volatile" && name == "Read")
        {
            var loc = Pop(); // ref T
            EmitVolatileRead(methodArgs[0], loc);
            return;
        }
        if (declType == "System.Threading.Volatile" && name == "Write")
        {
            var value = Pop();
            var loc = Pop(); // ref T
            EmitVolatileWrite(methodArgs[0], loc, value);
            return;
        }

        // double/float.ConvertToIntegerNative<TInteger>(value): the .NET 8+ intrinsic
        // the C# compiler emits for explicit float→integer casts (Random.CompatSeedImpl.
        // Next casts a double sample to int). Route through the runtime's saturating
        // helper rather than a plain (TInteger)value C++ cast: the cast is UB on
        // out-of-range/NaN input, and this mapping fires for every caller, not just
        // ones whose input is provably in range.
        if (declType is "System.Double" or "System.Single"
            && name == "ConvertToIntegerNative" && methodArgs.Length == 1)
        {
            var value = Pop();
            var intT = methodArgs[0];
            string cpp = CppTypes.Of(intT);
            string tmp = NewTemp(cpp);
            Emit($"{tmp} = dn2cpp_convert_to_integer_native<{cpp}>({value.Expr});");
            Push(CppTypes.KindOf(intT), cpp, tmp);
            return;
        }

        // NumberFormatInfo's generic span accessors (NegativeSignTChar<TChar>() and
        // siblings) — the numeric-formatting subtree's TChar forms of the string
        // properties (reached from the transpiled Number.FormatFloat behind
        // Half.ToString). Only the TChar=char instantiation is modeled (the string
        // formatting path); the Utf8Char one stays a loud failure. Each returns a
        // ReadOnlySpan<char> over the runtime NFI model's string (PositiveSign and
        // PerMilleSymbol are not carried in the model — every modeled culture keeps
        // the invariant "+" / "‰").
        if (declType == "System.Globalization.NumberFormatInfo"
            && name.EndsWith("TChar", StringComparison.Ordinal))
        {
            if (methodArgs is not [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char }])
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {declType}::{name}" +
                    $"<{methodArgs[0]}> is only supported for TChar=char");
            var nfiRecv = Pop();
            string nfiPtr = $"({Cast(nfiRecv, "const Dn2CppNumberFormatInfo*")})";
            string strExpr = name switch
            {
                "NumberDecimalSeparatorTChar" => $"{nfiPtr}->numberDecimal",
                "NumberGroupSeparatorTChar" => $"{nfiPtr}->numberGroup",
                "NegativeSignTChar" => $"{nfiPtr}->negativeSign",
                "NaNSymbolTChar" => $"{nfiPtr}->nan",
                "PositiveInfinitySymbolTChar" => $"{nfiPtr}->posInf",
                "NegativeInfinitySymbolTChar" => $"{nfiPtr}->negInf",
                "PercentSymbolTChar" => $"{nfiPtr}->percentSymbol",
                "PercentDecimalSeparatorTChar" => $"{nfiPtr}->percentDecimal",
                "PercentGroupSeparatorTChar" => $"{nfiPtr}->percentGroup",
                "CurrencySymbolTChar" => $"{nfiPtr}->currencySymbol",
                "CurrencyDecimalSeparatorTChar" => $"{nfiPtr}->currencyDecimal",
                "CurrencyGroupSeparatorTChar" => $"{nfiPtr}->currencyGroup",
                "PositiveSignTChar" => Literals.GetOrAdd("+"),
                "PerMilleSymbolTChar" => Literals.GetOrAdd("‰"),
                _ => throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {declType}::{name} " +
                    "has no runtime NFI field mapping yet"),
            };
            var nfiSig = MethodSpecSig(msh, methodArgs);
            string spanCt = CppTypes.Of(nfiSig.ReturnType);
            string symStr = NewTemp("Dn2CppString*");
            Emit($"{symStr} = {strExpr};");
            string symSpan = NewTemp(spanCt);
            Emit($"{symSpan}.f__reference = {symStr} ? (char16_t*){symStr}->chars : nullptr;");
            Emit($"{symSpan}.f__length = {symStr} ? {symStr}->length : 0;");
            Push(StackKind.Struct, spanCt, symSpan);
            return;
        }

        // MethodInfo.CreateDelegate<T>([object target]): the generic forms of
        // the reflection -> delegate bridge. T is statically known here, so its
        // type-info resolves directly and the call routes through the same
        // runtime binder as the non-generic overloads (which take the delegate
        // Type as a runtime value); the result is already typed T.
        if (declType == "System.Reflection.MethodInfo" && name == "CreateDelegate"
            && methodArgs.Length == 1)
        {
            var dT = methodArgs[0];
            string tiExpr = TypeInfoExpr(dT)
                ?? throw new NotSupportedException("CreateDelegate<T>: T has no runtime type-info");
            Comp.NeedsReflectionDelegateBind = true;
            var cdSig = MethodSpecSig(msh, methodArgs);
            string cdTarget = "nullptr";
            int closedForm = 0;
            if (cdSig.ParameterTypes.Length == 1)
            {
                var t = Pop();
                cdTarget = Cast(t, "Dn2CppObject*");
                closedForm = 1;
            }
            var m = Pop();
            string ct = CppTypes.Of(dT);
            Push(StackKind.Ref, ct,
                $"({ct})dn2cpp_delegate_create(dn2cpp_get_type_from_handle({tiExpr}), {cdTarget}, (Dn2CppMethodRef*)({m.Expr}), {closedForm}, 1)");
            return;
        }

        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: generic intrinsic {declType}::{name}" +
            $"<{methodArgs.Length}> has no intrinsic mapping yet");
    }

    /// <summary>Guards the trailing <c>IEqualityComparer&lt;T&gt;</c> of a
    /// <c>MemoryExtensions</c> span scan on the arms that cannot dispatch through it —
    /// where <see cref="Compilation.UserComparerInterfaceMethod"/> cannot prove the closed
    /// interface's type-info emitted, so the loops run over default equality and a
    /// receiver that nonetheless arrives (a reflection-built comparer) must not be
    /// silently ignored. It throws a catchable <c>PlatformNotSupportedException</c> naming
    /// the scan, rather than answering the default-equality question nobody asked.
    ///
    /// <para>The test is at run time because Enumerable.SequenceEqual forwards a comparer
    /// parameter. Null and the non-null Default identity both select default equality.</para></summary>
    private void EmitCustomComparerGuard(StackEntry comparer, string scan)
    {
        if (comparer.KnownNull)
            return;
        string cmp = NewTemp("Dn2CppObject*");
        Emit($"{cmp} = {Cast(comparer, "Dn2CppObject*")};");
        Emit($"if ({cmp} != nullptr && !dn2cpp_is_default_equality_comparer({cmp})) {{");
        Emit($"    dn2cpp_throw_platform_not_supported(\"{scan}: a custom IEqualityComparer<T> arrived, but no "
            + "IEqualityComparer<T> implementation for this element type was statically reachable, so the scan "
            + "was compiled over T's default equality and cannot dispatch through the comparer; it throws rather "
            + "than silently return the default-equality answer. Construct the comparer type directly somewhere "
            + "reachable, or pass null / EqualityComparer<T>.Default.\");");
        Emit("}");
    }

    /// <summary>The <c>string.Join&lt;T&gt;(separator, IEnumerable&lt;T&gt;)</c> lowering,
    /// shared with <c>StringBuilder.AppendJoin&lt;T&gt;</c>: pops the values operand and
    /// the string-or-char separator, pushes the joined <c>Dn2CppString*</c>. Supported
    /// over arrays (the dominant case), a List&lt;T&gt; (materialize the live prefix of
    /// its backing array — _items, same repr; iterate _size via the count-aware
    /// <c>_n</c> helpers), a concrete IEnumerable&lt;T&gt; collection (enumerated via
    /// its interface), or a bare collection-interface static type (runtime-
    /// discriminated). The element kind picks the formatting.</summary>
    private void EmitStringJoinGeneric(TypeDesc t)
    {
        var arr = Pop();
        var sep = Pop();
        string sepStr = sep.Kind == StackKind.Ref
            ? Cast(sep, "Dn2CppString*")
            : $"dn2cpp_char_to_string((char16_t)({sep.Expr}))";
        string arrExpr, countArg, suffix;
        if (arr.CppType.StartsWith("Dn2CppArray"))
            (arrExpr, countArg, suffix) = (arr.Expr, "", "");
        else if (TryListBacking(arr) is { } lb)
            (arrExpr, countArg, suffix) = (lb.Items, $", {lb.Count}", "_n");
        // A concrete IEnumerable<T> collection (SortedSet, Sorted*.Keys/.Values)
        // that is neither a Dn2CppArray nor a List<T> backing: enumerate it via
        // its interface. Pushes the result itself, so return early.
        else if (IsConcreteEnumerableOperand(arr) && TryEmitEnumerableJoin(arr, t, sepStr))
            return;
        // A bare IEnumerable<T>/IList<T>/... static type: the runtime value is an
        // array or a managed collection — discriminate at runtime.
        else if (IsBareCollectionInterface(arr) && TryEmitBareInterfaceJoin(arr, t, sepStr))
            return;
        else
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: string.Join is only " +
                "supported over arrays, List<T>, or an IEnumerable<T> collection yet");
        string call = RepOf(t) switch
        {
            // Unsigned 32/64-bit elements format unsigned — the signed
            // join_i4/join_i8 would print uint.MaxValue as -1. Matched
            // before the ArrRep buckets (uint is I4-backed).
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt32 } =>
                $"dn2cpp_string_join_u4{suffix}({sepStr}, (Dn2CppArrayI4*)({arrExpr}){countArg})",
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.UInt64 } =>
                $"dn2cpp_string_join_u8{suffix}({sepStr}, (Dn2CppArrayN*)({arrExpr}){countArg})",
            ArrRep.I4 => $"dn2cpp_string_join_i4{suffix}({sepStr}, (Dn2CppArrayI4*)({arrExpr}){countArg})",
            ArrRep.Ref => $"dn2cpp_string_join_ref{suffix}({sepStr}, (Dn2CppArrayRef*)({arrExpr}){countArg})",
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int64 } =>
                $"dn2cpp_string_join_i8{suffix}({sepStr}, (Dn2CppArrayN*)({arrExpr}){countArg})",
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } =>
                $"dn2cpp_string_join_r8{suffix}({sepStr}, (Dn2CppArrayN*)({arrExpr}){countArg})",
            _ when t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char } =>
                $"dn2cpp_string_join_ch{suffix}({sepStr}, (Dn2CppArrayN*)({arrExpr}){countArg})",
            _ => throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: string.Join<{t}> " +
                "element type is not supported yet (int / long / double / char / string only)"),
        };
        Push(StackKind.Ref, "Dn2CppString*", call);
    }

}
