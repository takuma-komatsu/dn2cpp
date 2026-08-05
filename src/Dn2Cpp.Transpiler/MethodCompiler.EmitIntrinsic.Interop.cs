using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>System.Runtime.CompilerServices.Unsafe — pointer/byref primitives
    /// the JIT normally lowers; we map them to the equivalent C++ pointer ops.
    /// <paramref name="ga"/> are the (closed) method type arguments.</summary>
    private bool TryUnsafeIntrinsic(string name, TypeDesc[] ga)
    {
        switch (name)
        {
            // T's element width must be the real storage width, not the int32-promoted
            // stack type — otherwise `Unsafe.Add<byte>`/`SizeOf<byte>` stride/size by 4
            // and a byte buffer (e.g. a ReadOnlySpan<byte> backing ref) is misindexed.
            // StorageOf matches the packed array/byref storage.
            case "SizeOf": // SizeOf<T> -> int
                // The sizeof needs T's complete struct, but the result is an int, so T flows
                // nowhere the emit closure looks: a lone SizeOf<T> over an otherwise-unreached
                // struct would leave it undeclared.
                NoteBuiltStructLayout(ga[0]);
                Push(StackKind.I4, "int32_t", $"(int32_t)sizeof({CppTypes.StorageOf(ga[0])})");
                return true;
            case "BitCast" when ga.Length == 2: // BitCast<TFrom,TTo>(TFrom) -> TTo (bit reinterpret, sizes match)
            {
                // The real BCL requires sizeof(TFrom) == sizeof(TTo) (else NotSupportedException).
                // Reinterpret the bits via memcpy: pin the source value in its storage type,
                // copy the bytes into a TTo-storage temp, then promote to the TTo stack value.
                var src = Pop();
                string fromSt = CppTypes.StorageOf(ga[0]);
                string toSt = CppTypes.StorageOf(ga[1]);
                string toCt = CppTypes.Of(ga[1]);
                // The reinterpret builds a by-value TTo temp (and takes its sizeof), so TTo's
                // full struct must be emitted even when nothing else reaches it — an arm
                // dead for the concrete type argument still names it (NoteBuiltStructLayout).
                NoteBuiltStructLayout(ga[1]);
                string s = NewTemp(fromSt);
                Emit($"{s} = {Cast(src, fromSt)};");
                string d = NewTemp(toSt);
                Emit($"std::memcpy(&{d}, &{s}, sizeof({toSt}));");
                Push(CppTypes.KindOf(ga[1]), toCt, toSt == toCt ? d : $"({toCt})({d})");
                return true;
            }
            case "Add": // Add<T>(ref T source, {int|nint|nuint} off) -> ref T
            {
                var off = Pop();
                var src = Pop();
                string et = CppTypes.StorageOf(ga[0]);
                Push(StackKind.Ptr, et + "*", $"(({et}*)({src.Expr}) + ({off.Expr}))");
                return true;
            }
            case "Subtract": // Subtract<T>(ref T source, {int|nint|nuint} off) -> ref T
            {
                var off = Pop();
                var src = Pop();
                string et = CppTypes.StorageOf(ga[0]);
                Push(StackKind.Ptr, et + "*", $"(({et}*)({src.Expr}) - ({off.Expr}))");
                return true;
            }
            case "AddByteOffset": // AddByteOffset<T>(ref T, {nint|nuint}) -> ref T
            {
                var off = Pop();
                var src = Pop();
                Push(StackKind.Ptr, "int8_t*", $"((int8_t*)({src.Expr}) + ({off.Expr}))");
                return true;
            }
            case "SubtractByteOffset": // SubtractByteOffset<T>(ref T, {nint|nuint}) -> ref T (dual of AddByteOffset)
            {
                var off = Pop();
                var src = Pop();
                Push(StackKind.Ptr, "int8_t*", $"((int8_t*)({src.Expr}) - ({off.Expr}))");
                return true;
            }
            case "ByteOffset": // ByteOffset<T>(ref T origin, ref T target) -> IntPtr (target - origin, bytes)
            {
                var target = Pop();
                var origin = Pop();
                Push(StackKind.I8, "intptr_t",
                    $"(intptr_t)((int8_t*)({target.Expr}) - (int8_t*)({origin.Expr}))");
                return true;
            }
            case "As" when ga.Length == 2: // As<TFrom,TTo>(ref TFrom) -> ref TTo
            {
                var src = Pop();
                string to = CppTypes.Of(ga[1]);
                Push(StackKind.Ptr, to + "*", $"({to}*)({src.Expr})");
                return true;
            }
            case "As" when ga.Length == 1: // As<T>(object) -> T (reference reinterpret)
            {
                var src = Pop();
                string to = CppTypes.Of(ga[0]);
                Push(StackKind.Ref, to, $"({to})({src.Expr})");
                return true;
            }
            case "Unbox": // Unbox<T>(object box) -> ref T (a ref into the box's payload)
            {
                // Same lowering as the unbox opcode: dn2cpp_unbox validates (null ->
                // NullReferenceException, type mismatch -> InvalidCastException) and
                // returns the interior payload pointer. The payload layout is what the
                // box opcode wrote — the int32-promoted CppTypes.Of width — so the ref
                // must use Of, not StorageOf. As in .NET, dn2cpp_unbox accepts the ECMA
                // verifier-assignable widening (enum ↔ underlying, enums of one
                // underlying) beyond the exact type-info match.
                var obj = Pop();
                string uti = TypeInfoExpr(ga[0])
                    ?? throw new NotSupportedException($"Unsafe.Unbox<{ga[0]}> is not supported yet");
                string uct = CppTypes.Of(ga[0]);
                Push(StackKind.Ptr, uct + "*",
                    $"({uct}*)dn2cpp_unbox({Cast(obj, "Dn2CppObject*")}, {uti})");
                return true;
            }
            case "AsRef": // AsRef<T>(void*) / AsRef<T>(in T) -> ref T
            {
                var src = Pop();
                string et = CppTypes.Of(ga[0]);
                Push(StackKind.Ptr, et + "*", $"({et}*)({src.Expr})");
                return true;
            }
            case "AsPointer": // AsPointer<T>(ref T) -> void*
            {
                var src = Pop();
                Push(StackKind.Ptr, "void*", $"(void*)({src.Expr})");
                return true;
            }
            // OpportunisticMisalignment<T>(in T, nuint alignment) -> nuint: the address's
            // misalignment in bytes against `alignment`. It is executed, not merely
            // compiled, so it needs the real modulo even though under the scalar fold the
            // value only steers loop bounds.
            case "OpportunisticMisalignment":
            {
                var align = Pop();
                var addr = Pop();
                Push(StackKind.I8, "uint64_t",
                    $"((uint64_t)((uintptr_t)({addr.Expr}) % (uintptr_t)({align.Expr})))");
                return true;
            }
            case "NullRef": // NullRef<T> -> ref T (a null managed pointer)
                Push(StackKind.Ptr, CppTypes.Of(ga[0]) + "*", "nullptr");
                return true;
            case "IsNullRef": // IsNullRef<T>(ref T) -> bool (Dictionary.FindValue miss)
            {
                var src = Pop();
                Push(StackKind.I4, "int32_t", $"(({src.Expr}) == nullptr ? 1 : 0)");
                return true;
            }
            case "AreSame": // AreSame<T>(ref T, ref T) -> bool (pointer identity)
            {
                var b = Pop();
                var a = Pop();
                // Compare as void*: the two refs can carry different static C++
                // pointer types for the same T (e.g. a stack-slot-typed int32_t*
                // from Unsafe.As<...,byte> vs a storage-typed uint8_t* from
                // GetArrayDataReference), and a raw == on distinct pointer types
                // is a hard error.
                Push(StackKind.I4, "int32_t", $"({Cast(a, "void*")} == {Cast(b, "void*")} ? 1 : 0)");
                return true;
            }
            // IsAddressGreaterThan/LessThan<T>(ref T, ref T) -> bool: an *unsigned* address
            // comparison (the BCL casts both refs to nuint), so compare as uintptr_t rather
            // than raw pointers — that avoids the UB of `<`/`>` on pointers into different
            // objects and matches .NET's nuint ordering.
            case "IsAddressGreaterThan":
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((uintptr_t)({a.Expr}) > (uintptr_t)({b.Expr}) ? 1 : 0)");
                return true;
            }
            case "IsAddressLessThan":
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((uintptr_t)({a.Expr}) < (uintptr_t)({b.Expr}) ? 1 : 0)");
                return true;
            }
            case "IsAddressGreaterThanOrEqualTo":
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((uintptr_t)({a.Expr}) >= (uintptr_t)({b.Expr}) ? 1 : 0)");
                return true;
            }
            case "IsAddressLessThanOrEqualTo":
            {
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"((uintptr_t)({a.Expr}) <= (uintptr_t)({b.Expr}) ? 1 : 0)");
                return true;
            }
            case "SkipInit": // SkipInit<T>(out T) -> void: deliberately leave the target uninitialized
            {
                Pop(); // the out ref; the whole point is to emit nothing for it
                return true;
            }
            // Read/Write a T through a raw address. Memory width is the real StorageOf
            // (byte=1, short=2, struct=sizeof) — NOT the int32-promoted Of — so sub-word
            // reads load the right bytes and sign/zero-extend correctly on the promote to
            // the I4 stack value (StorageOf keeps int8_t vs uint8_t). The *Unaligned forms
            // go through memcpy (the address may be misaligned; a bare deref would be UB and
            // clang may assume alignment), which the optimizer folds back to a single
            // unaligned load/store. The aligned forms deref directly, like ldobj/stobj.
            // Each of the five below spells T's storage where C++ demands a COMPLETE type
            // while taking its operands as bare pointers, so the emit closure can miss T
            // entirely — the SizeOf<T> trap — hence NoteBuiltStructLayout on all of them.
            case "Read": // Read<T>(void* source) -> T (aligned)
            {
                var src = Pop();
                NoteBuiltStructLayout(ga[0]);
                string st = CppTypes.StorageOf(ga[0]);
                string ct = CppTypes.Of(ga[0]);
                string load = $"*(({st}*)({src.Expr}))";
                Push(CppTypes.KindOf(ga[0]), ct, st == ct ? load : $"({ct})({load})");
                return true;
            }
            case "ReadUnaligned": // ReadUnaligned<T>(void*|ref byte source) -> T
            {
                var src = Pop();
                NoteBuiltStructLayout(ga[0]);
                string st = CppTypes.StorageOf(ga[0]);
                string ct = CppTypes.Of(ga[0]);
                string tmp = NewTemp(st);
                Emit($"std::memcpy(&{tmp}, (const void*)({src.Expr}), sizeof({st}));");
                Push(CppTypes.KindOf(ga[0]), ct, st == ct ? tmp : $"({ct})({tmp})");
                return true;
            }
            case "Write": // Write<T>(void* destination, T value) (aligned)
            {
                var val = Pop();
                var dest = Pop();
                NoteBuiltStructLayout(ga[0]);
                string st = CppTypes.StorageOf(ga[0]);
                Emit($"*(({st}*)({dest.Expr})) = {Cast(val, st)};");
                return true;
            }
            case "WriteUnaligned": // WriteUnaligned<T>(void*|ref byte destination, T value)
            {
                var val = Pop();
                var dest = Pop();
                NoteBuiltStructLayout(ga[0]);
                string st = CppTypes.StorageOf(ga[0]);
                string tmp = NewTemp(st);
                Emit($"{tmp} = {Cast(val, st)};");
                Emit($"std::memcpy((void*)({dest.Expr}), &{tmp}, sizeof({st}));");
                return true;
            }
            case "Copy": // Copy<T>(void* dest, ref T src) / Copy<T>(ref T dest, void* src)
            {
                var src = Pop();
                var dest = Pop();
                // Pointer operands and a bare sizeof(T) — the SizeOf<T> shape exactly.
                NoteBuiltStructLayout(ga[0]);
                string st = CppTypes.StorageOf(ga[0]);
                Emit($"std::memcpy((void*)({dest.Expr}), (const void*)({src.Expr}), sizeof({st}));");
                return true;
            }
            // NB: CopyBlock/InitBlock are non-generic, so they arrive on the
            // non-generic call path (the (declType, name) switch), not here.
            default:
                return false;
        }
    }

    /// <summary>System.Runtime.InteropServices.NativeMemory raw-heap allocation.
    /// Alloc/AllocZeroed/Realloc return an unmanaged <c>void*</c>; Free releases it. The
    /// block lives on the C heap (dn2cpp_native_*), outside the GC — blittable data only.
    /// The bulk byte ops Clear/Copy/Fill lower directly to std::memset/std::memmove (Copy is
    /// overlap-safe in the BCL, hence memmove — and .NET's argument order is
    /// (source, destination), the reverse of memcpy).</summary>
    private bool TryEmitNativeMemoryIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        switch (name)
        {
            case "Alloc" when sig.ParameterTypes.Length == 1: // Alloc(nuint byteCount)
            {
                var n = Pop();
                Push(StackKind.Ptr, "void*", $"dn2cpp_native_alloc((size_t)({n.Expr}))");
                return true;
            }
            case "Alloc": // Alloc(nuint elementCount, nuint elementSize) — uninitialized
            {
                var size = Pop();
                var count = Pop();
                Push(StackKind.Ptr, "void*",
                    $"dn2cpp_native_alloc_checked((size_t)({count.Expr}), (size_t)({size.Expr}))");
                return true;
            }
            case "AllocZeroed" when sig.ParameterTypes.Length == 1: // AllocZeroed(nuint byteCount)
            {
                var n = Pop();
                Push(StackKind.Ptr, "void*", $"dn2cpp_native_calloc((size_t)({n.Expr}), 1)");
                return true;
            }
            case "AllocZeroed": // AllocZeroed(nuint elementCount, nuint elementSize)
            {
                var size = Pop();
                var count = Pop();
                Push(StackKind.Ptr, "void*",
                    $"dn2cpp_native_calloc((size_t)({count.Expr}), (size_t)({size.Expr}))");
                return true;
            }
            case "Realloc": // Realloc(void* ptr, nuint byteCount)
            {
                var n = Pop();
                var ptr = Pop();
                Push(StackKind.Ptr, "void*",
                    $"dn2cpp_native_realloc({Cast(ptr, "void*")}, (size_t)({n.Expr}))");
                return true;
            }
            case "Free": // Free(void* ptr)
            {
                var ptr = Pop();
                Emit($"dn2cpp_native_free({Cast(ptr, "void*")});");
                return true;
            }
            // The aligned native heap. AlignedAlloc rounds byteCount up to a multiple of
            // alignment (a power of two) in the runtime wrapper.
            case "AlignedAlloc": // AlignedAlloc(nuint byteCount, nuint alignment) -> void*
            {
                var alignment = Pop();
                var byteCount = Pop();
                Push(StackKind.Ptr, "void*",
                    $"dn2cpp_native_aligned_alloc((size_t)({byteCount.Expr}), (size_t)({alignment.Expr}))");
                return true;
            }
            case "AlignedRealloc": // AlignedRealloc(void* ptr, nuint byteCount, nuint alignment) -> void*
            {
                var alignment = Pop();
                var byteCount = Pop();
                var ptr = Pop();
                Push(StackKind.Ptr, "void*",
                    $"dn2cpp_native_aligned_realloc({Cast(ptr, "void*")}, (size_t)({byteCount.Expr}), (size_t)({alignment.Expr}))");
                return true;
            }
            case "AlignedFree": // AlignedFree(void* ptr)
            {
                var ptr = Pop();
                Emit($"dn2cpp_native_aligned_free({Cast(ptr, "void*")});");
                return true;
            }
            case "Clear": // Clear(void* ptr, nuint byteCount)
            {
                var n = Pop();
                var ptr = Pop();
                Emit($"std::memset({Cast(ptr, "void*")}, 0, (size_t)({n.Expr}));");
                return true;
            }
            case "Copy": // Copy(void* source, void* destination, nuint byteCount) — overlap-safe
            {
                var n = Pop();
                var dest = Pop();
                var src = Pop();
                Emit($"std::memmove({Cast(dest, "void*")}, {Cast(src, "void*")}, (size_t)({n.Expr}));");
                return true;
            }
            case "Fill": // Fill(void* ptr, nuint byteCount, byte value)
            {
                var value = Pop();
                var n = Pop();
                var ptr = Pop();
                Emit($"std::memset({Cast(ptr, "void*")}, (int)(uint8_t)({value.Expr}), (size_t)({n.Expr}));");
                return true;
            }
        }
        return false;
    }

    /// <summary>Lowers a call to a member <see cref="CoreIntrinsics.LoweredRuntimePrimitive"/>
    /// names — the intra-CoreLib runtime primitives the reachability cut deletes from the tree.
    ///
    /// <para>TOTAL over that predicate by construction: it ends in a throw. The per-family
    /// tables below still select on the SHAPE, but a shape none of them models must not fall
    /// out the bottom into <c>EmitManagedCall</c> and name a body nothing emitted (an
    /// undefined symbol at C++ link time). Every family the predicate names is untranspilable
    /// runtime plumbing, so there is nothing worth falling through to.</para></summary>
    private void EmitRuntimePrimitive(MethodInfo callee)
    {
        if (TryEmitGcMemmoveMarshalPrimitive(callee))
            return;
        if (TryEmitRandomBytesEntryAssemblyPrimitive(callee))
            return;
        if (TryEmitNativeLibraryGetSymbol(callee))
            return;
        if (TryEmitOrdinalIgnoreCaseIntrinsic(callee))
            return;
        if (TryEmitArrayPoolRuntimePrimitive(callee))
            return;
        if (TryEmitEnvironmentRegistry(callee))
            return;
        // Buffer's block moves. TryEmitBufferIntrinsic keeps its bool contract — the
        // MemberReference path calls it too and relies on the false — so the throw for an
        // unmodeled shape is this dispatcher's, not the helper's.
        if (callee.DeclaringClass.FullName == "System.Buffer"
            && TryEmitBufferIntrinsic(callee.Name, callee.Signature))
            return;
        if (TryEmitNumberGroupSizes(callee))
            return;
        if (TryEmitAppContextBaseDirectory(callee))
            return;
        if (TryEmitAsciiTranscode(callee))
            return;
        if (TryEmitUtf8Transcode(callee))
            return;
        // Encoding.GetString. TryEmitEncodingGetString raises its OWN throw for an unmodeled
        // shape, so it never returns false.
        if (callee.DeclaringClass.FullName
                is "System.Text.Encoding" or "System.Text.UTF8Encoding" or "System.Text.UnicodeEncoding"
            && TryEmitEncodingGetString(callee.DeclaringClass.FullName, callee.Signature))
            return;
        throw new NotSupportedException(
            $"{Method.DeclaringClass.FullName}.{Method.Name}: call to "
            + $"{callee.DeclaringClass.FullName}.{callee.Name} ({callee.Signature.ParameterTypes.Length} args) "
            + "— an unmodeled overload of an intercepted runtime primitive. The reachability cut "
            + "deletes the whole name family (CoreIntrinsics.LoweredRuntimePrimitive), so there is "
            + "no real body left to fall through to: model this overload here, or take the family "
            + "out of the predicate so its real body stays in the tree.");
    }

    /// <summary>System.Number's private group-sizes readers — the only members of the
    /// transpiled Number that <c>ldfld</c> the intrinsic-mapped NumberFormatInfo directly (its
    /// C++ lowering is the runtime NFI struct, so a managed field access cannot be emitted).
    /// The runtime model carries ONE NumberGroupSizes array, used for the number/currency/
    /// percent groups alike, so all three readers lower to the same helper.</summary>
    private bool TryEmitNumberGroupSizes(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName != "System.Number"
            || callee.Name is not ("NumberGroupSizes" or "CurrencyGroupSizes" or "PercentGroupSizes")
            || callee.Signature.ParameterTypes.Length != 1)
            return false;
        var gsNfi = Pop();
        string gsArr = NewTemp("Dn2CppArrayI4*");
        Emit($"{gsArr} = dn2cpp_nfi_group_sizes({Cast(gsNfi, "const Dn2CppNumberFormatInfo*")}, "
            + $"{PrimArrayTypeInfoExpr(PrimitiveTypeCode.Int32)});");
        Push(StackKind.Ref, "Dn2CppArrayI4*", gsArr);
        return true;
    }

    /// <summary><c>AppContext.BaseDirectory</c> → the running executable's directory, reached
    /// intra-corelib. AppContext is not an intrinsic-mapped type, so this one member is routed
    /// explicitly; its real getter's GetBaseDirectoryCore fallback resolves
    /// Assembly.GetEntryAssembly()?.Location — meaningless in a binary with no managed entry
    /// assembly — and cutting it also keeps the AppContext data dictionary out of the
    /// tree.</summary>
    private bool TryEmitAppContextBaseDirectory(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName != "System.AppContext"
            || callee.Name != "get_BaseDirectory"
            || callee.Signature.ParameterTypes.Length != 0)
            return false;
        EmitIntrinsic("System.AppContext", callee.Name, callee.Signature);
        return true;
    }

    /// <summary><c>NativeLibrary.GetSymbol(IntPtr handle, string symbolName, bool
    /// throwOnError)</c> → dn2cpp_native_library_get_symbol. Its real body is a QCall into the
    /// CLR's own native runtime, which does not exist here — but for a plain handle (no COM,
    /// no AssemblyLoadContext bookkeeping, neither of which dn2cpp has) that QCall is exactly
    /// GetProcAddress/dlsym on the narrowed export name. throwOnError=true raises a hard fail
    /// rather than a typed EntryPointNotFoundException.</summary>
    private bool TryEmitNativeLibraryGetSymbol(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName != "System.Runtime.InteropServices.NativeLibrary"
            || callee.Name != "GetSymbol"
            || callee.Signature.ParameterTypes is not
                [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.IntPtr },
                 { IsString: true },
                 { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }])
            return false;
        var throwOnError = Pop();
        var symbolName = Pop();
        var libHandle = Pop();
        string symTmp = NewTemp("void*");
        Emit($"{symTmp} = dn2cpp_native_library_get_symbol((void*){Cast(libHandle, "intptr_t")}, "
            + $"{Cast(symbolName, "Dn2CppString*")});");
        Emit($"if ({symTmp} == nullptr && ({throwOnError.Expr}) != 0) "
            + "dn2cpp_fail(\"EntryPointNotFoundException\");");
        Push(StackKind.I8, "intptr_t", $"(intptr_t){symTmp}");
        return true;
    }

    /// <summary>The GC surface, SpanHelpers.Memmove, and the Marshal last-error /
    /// PtrToStringUTF8 intercepts, each reached only via an in-CoreLib MethodDef call.
    /// Lowered inline so the real FCall/InternalCall bodies are never transpiled; the
    /// reachability edges are cut in Compilation.ResolveCallTarget. Returns false for any
    /// shape it does not model — the EmitRuntimePrimitive dispatcher above turns that into
    /// the loud throw.
    /// <list type="bullet">
    /// <item><c>System.GC::SuppressFinalize(object)</c> → dn2cpp_gc_suppress_finalize.
    /// Finalizers really run here, so suppression must too (a no-op only when the object's
    /// type has no Finalize override). This also makes the real body — which calls
    /// RuntimeHelpers.GetMethodTable to test MethodTable.HasFinalizer — unreachable.</item>
    /// <item><c>System.SpanHelpers::Memmove(ref byte dest, ref byte src, nuint len)</c> →
    /// <c>std::memmove</c>. The managed entry's body is a hand-unrolled Unsafe.* copy loop
    /// that also calls the SpanHelpers::memmove InternalCall fallback; mapping the managed
    /// entry closes both.</item>
    /// <item><c>System.Runtime.InteropServices.Marshal::GetLastPInvokeError</c> → the
    /// per-thread errno slot (dn2cpp_marshal_get_last_error). The MemberRef form routes
    /// through TryEmitMarshalIntrinsic; this routes the in-CoreLib MethodDef form the same
    /// way.</item>
    /// </list></summary>
    private bool TryEmitGcMemmoveMarshalPrimitive(MethodInfo callee)
    {
        switch (callee.DeclaringClass.FullName, callee.Name)
        {
            case ("System.GC", "SuppressFinalize")
                when callee.Signature.ParameterTypes.Length == 1 && callee.IsStatic:
            {
                var o = Pop();
                Emit($"dn2cpp_gc_suppress_finalize({Cast(o, "Dn2CppObject*")});");
                return true;
            }
            // The remaining GC.* intercepts, mirroring the MemberRef forms in
            // MethodCompiler.TranslateCall so an in-CoreLib MethodDef call lowers the same
            // way: reachability already cut all of them, so without these cases such a call
            // would name a function that is never generated.
            case ("System.GC", "ReRegisterForFinalize")
                when callee.Signature.ParameterTypes.Length == 1 && callee.IsStatic:
            {
                var o = Pop();
                Emit($"dn2cpp_gc_reregister_for_finalize({Cast(o, "Dn2CppObject*")});");
                return true;
            }
            case ("System.GC", "Collect")
                when callee.Signature.ParameterTypes.Length == 0 && callee.IsStatic:
                Emit("dn2cpp_gc_collect();");
                return true;
            case ("System.GC", "WaitForPendingFinalizers")
                when callee.Signature.ParameterTypes.Length == 0 && callee.IsStatic:
                Emit("dn2cpp_gc_wait_for_pending_finalizers();");
                return true;
            case ("System.GC", "GetTotalMemory")
                when callee.Signature.ParameterTypes.Length == 1 && callee.IsStatic:
            {
                var force = Pop();
                Push(StackKind.I8, "int64_t", $"dn2cpp_gc_get_total_memory((int32_t)({force.Expr}))");
                return true;
            }
            case ("System.GC", "KeepAlive")
                when callee.Signature.ParameterTypes.Length == 1 && callee.IsStatic:
            {
                var o = Pop();
                Emit($"dn2cpp_keep_alive({Cast(o, "Dn2CppObject*")});");
                return true;
            }
            case ("System.GC", "_CollectionCount")
                when callee.Signature.ParameterTypes.Length == 2 && callee.IsStatic:
            {
                // _CollectionCount(generation, getSpecialGCCount). Boehm is non-generational
                // and has no special (background/ephemeral) GC counter, so both selectors
                // collapse to the single collection-cycle counter (GC_get_gc_no).
                Pop();
                Pop();
                Push(StackKind.I4, "int32_t", "dn2cpp_gc_collection_count()");
                return true;
            }
            case ("System.GC", "GetMemoryInfo")
                when callee.Signature.ParameterTypes.Length == 2 && callee.IsStatic:
            {
                // GetMemoryInfo(GCMemoryInfoData data, int kind): fill the caller-allocated
                // data object with what Boehm can answer. kind is dropped — Boehm gives one
                // heap snapshot regardless of collection kind. The data object is a fresh
                // new(), so every field NOT set below stays 0, the conservative documented
                // value for accounting Boehm does not track. The stores go through dn2cpp's
                // own emitted layout, so no offsets are baked into the runtime.
                // TotalCommittedBytes mirrors HeapSizeBytes; FragmentedBytes is the free
                // bytes within it.
                Pop(); // kind
                var data = Pop();
                if (callee.Signature.ParameterTypes[0] is not { Kind: TypeKind.Class, Class: { } gcMemCls })
                    throw new NotSupportedException(
                        "System.GC.GetMemoryInfo: expected a GCMemoryInfoData class as the first "
                        + $"parameter [chain: {_c.ReachChain(_method)}]");
                string Store(string fieldName) =>
                    FieldAccess(gcMemCls, gcMemCls.Fields.First(f => f.Name == fieldName), data);
                Emit($"{Store("_heapSizeBytes")} = (int64_t)dn2cpp_gc_heap_size_bytes();");
                Emit($"{Store("_totalCommittedBytes")} = (int64_t)dn2cpp_gc_heap_size_bytes();");
                Emit($"{Store("_fragmentedBytes")} = (int64_t)dn2cpp_gc_free_bytes();");
                return true;
            }
            case ("System.SpanHelpers", "Memmove")
                when callee.Signature.ParameterTypes.Length == 3 && callee.IsStatic:
            {
                var len = Pop();
                var src = Pop();
                var dest = Pop();
                // memmove is the overlap-safe superset of memcpy, matching the BCL entry.
                Emit($"std::memmove((void*)({dest.Expr}), (void*)({src.Expr}), (size_t)({len.Expr}));");
                return true;
            }
            // GetLastPInvokeError reads, SetLastPInvokeError(int) writes the per-thread errno
            // slot. SafeHandle.InternalRelease saves/restores the slot around its
            // ReleaseHandle callvirt, so both must be mapped — mapping the getter alone
            // re-roots the body to the setter's MethodDef call.
            case ("System.Runtime.InteropServices.Marshal", "GetLastPInvokeError")
                when callee.Signature.ParameterTypes.Length == 0 && callee.IsStatic:
            case ("System.Runtime.InteropServices.Marshal", "SetLastPInvokeError")
                when callee.Signature.ParameterTypes.Length == 1 && callee.IsStatic:
            // PtrToStringUTF8: routes the MethodDef form to the same intrinsic as the
            // MemberRef form; the reachability edge is cut in ResolveCallTarget (the real
            // body reaches String.CreateStringFromEncoding, the UTF-8-decode subtree).
            case ("System.Runtime.InteropServices.Marshal", "PtrToStringUTF8")
                when callee.IsStatic:
                EmitIntrinsic(callee.DeclaringClass.FullName, callee.Name, callee.Signature);
                return true;
        }
        return false;
    }

    /// <summary>The nonsecure-random fill and the entry-assembly stub, each reached only via
    /// an in-CoreLib MethodDef call. Lowered inline so the real FCall/InternalCall bodies are
    /// never transpiled; the reachability edges are cut in Compilation.ResolveCallTarget.
    /// Returns false (→ normal managed call) for any other member on these types, so an
    /// unmodeled overload is a loud failure, not a silent carve-out.
    /// <list type="bullet">
    /// <item><c>Interop::GetRandomBytes(byte* buffer, int length)</c> → a deterministic
    /// fixed-seed fill (<c>dn2cpp_fill_nonsecure_random</c>). This is the IL forwarder to the
    /// bodyless InternalCall
    /// <c>Interop+Sys::GetNonCryptographicallySecureRandomBytes</c>; intercepting and cutting
    /// the forwarder makes that InternalCall unreachable. Its consumer,
    /// <c>System.HashCode.GenerateGlobalSeed</c>, needs only one stable process-global seed —
    /// Dictionary/HashSet lookups and insertion-order enumeration are
    /// hash-value-independent.</item>
    /// <item><c>System.Reflection.Assembly::GetEntryAssemblyNative(ObjectHandleOnStack)</c>
    /// → no-op (a null stub). The bodyless InternalCall's only job is to write the entry
    /// RuntimeAssembly into the handle's target slot; doing nothing leaves
    /// <c>GetEntryAssemblyInternal</c>'s local null, so <c>Assembly.GetEntryAssembly</c>
    /// returns null. DECLARED DIVERGENCE: a native binary has no managed entry assembly on
    /// disk to hand back. Nothing derives a file-system location from it —
    /// <c>AppContext.BaseDirectory</c> is intrinsified onto the process image instead — so
    /// the null stub costs only the entry-assembly identity itself.</item>
    /// </list></summary>
    private bool TryEmitRandomBytesEntryAssemblyPrimitive(MethodInfo callee)
    {
        switch (callee.DeclaringClass.FullName, callee.Name)
        {
            case ("Interop", "GetRandomBytes")
                when callee.Signature.ParameterTypes.Length == 2 && callee.IsStatic:
            {
                var len = Pop();
                var buf = Pop();
                Emit($"dn2cpp_fill_nonsecure_random((void*)({buf.Expr}), (int32_t)({len.Expr}));");
                return true;
            }
            case ("System.Reflection.Assembly", "GetEntryAssemblyNative")
                when callee.Signature.ParameterTypes.Length == 1 && callee.IsStatic:
                Pop(); // the ObjectHandleOnStack out-param — left unwritten (null stub)
                return true;
        }
        return false;
    }

    /// <summary>The framework EventSource tracing guards — the bounded no-op diagnostics
    /// surface (the IL2CPP/NativeAOT posture: no EventPipe/ETW/EventListener delivery, no
    /// manifest generation). Framework providers wrap their tracing in
    /// <c>if (Log.IsEnabled()) Log.SomeEvent(…)</c>. The <c>Log</c> singleton is folded to
    /// null (its .cctor is skipped in ReachCctor, so EventSource is never allocated and its
    /// finalizer -> Dispose -> SendManifest cascade — the
    /// Guid/HexConverter/reflection/ResourceManager/ICU subtree — never enters the tree).
    /// The only calls on that null Log are the guards, lowered here:
    /// <list type="bullet">
    /// <item><c>EventSource.IsEnabled()</c> (any bool-returning overload) ->
    /// <c>false</c>;</item>
    /// <item>the provider's <c>void</c> event methods -> a no-op (discard receiver +
    /// args);</item>
    /// <item>a non-void provider member — the scope-id / activity-handle returns -> its
    /// return type's default, which is exactly the value .NET returns when no listener is
    /// attached, and which its paired stop method already accepts;</item>
    /// <item>a byref / pointer / generic-parameter return, or a member that computes a
    /// real value even with no listener (see <see cref="IsRealValueProviderMember"/>), has
    /// no safe default and still fails loudly.</item>
    /// </list>
    /// Reached both intra-assembly (MethodDef) and cross-assembly (MemberRef), and the
    /// guards do execute at runtime, so the lowering must fold safely rather than assume
    /// dead code. The reachability edges are cut in Compilation.ResolveCallTarget.</summary>
    private bool TryEmitEventSourceNoOp(MethodInfo callee)
    {
        string dt = callee.DeclaringClass.FullName;
        if (dt == "System.Diagnostics.Tracing.EventSource" && callee.Name == "IsEnabled"
            && callee.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean })
        {
            for (int i = 0; i < callee.Signature.ParameterTypes.Length; i++)
                Pop(); // IsEnabled(level, keywords) overload args — discarded
            if (!callee.IsStatic)
                Pop(); // the null Log receiver
            Push(StackKind.I4, "int32_t", "0");
            return true;
        }
        if (Compilation.IsFrameworkEventSourceProvider(callee.DeclaringClass))
        {
            if (callee.Name == "IsEnabled"
                && callee.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean })
            {
                for (int i = 0; i < callee.Signature.ParameterTypes.Length; i++)
                    Pop();
                if (!callee.IsStatic)
                    Pop(); // the null Log receiver
                Push(StackKind.I4, "int32_t", "0");
                return true;
            }
            if (callee.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Void })
            {
                for (int i = 0; i < callee.Signature.ParameterTypes.Length; i++)
                    Pop(); // event payload — discarded
                if (!callee.IsStatic)
                    Pop(); // the null Log receiver
                return true;
            }
            // A non-void provider member with no listener attached. .NET's own
            // listener-absent path returns the method's default — 0 for the scope-id
            // longs, a null Activity, a zeroed activity struct — and every paired stop
            // method accepts that sentinel, so folding to the return type's default is
            // observably equal to real .NET's no-listener path. Two shapes have no safe
            // default and still fail loudly: a byref / pointer / generic-parameter return
            // (a forward guard — no modeled zero), and the members that compute a real
            // value even with no listener attached.
            TypeDesc rt = callee.Signature.ReturnType;
            if (rt.Kind is TypeKind.ByRef or TypeKind.Pointer or TypeKind.GenericVar
                || IsRealValueProviderMember(dt, callee.Name))
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: call to "
                    + $"{dt}.{callee.Name} — a non-void member on a no-op EventSource "
                    + "provider is an unmodeled provider shape");
            NoteReferenceClass(rt);
            for (int i = 0; i < callee.Signature.ParameterTypes.Length; i++)
                Pop(); // event payload — discarded
            if (!callee.IsStatic)
                Pop(); // the null Log receiver
            string ct = CppTypes.Of(rt);
            Push(CppTypes.KindOf(rt), ct,
                CppTypes.ZeroInitExpr(ct));
            return true;
        }
        return false;
    }

    /// <summary>The only two framework EventSource-provider members whose value is computed
    /// unconditionally — not gated on a listener — so folding them to their return-type
    /// default would be a wrong answer, not the no-listener answer:
    /// <c>PlinqEtwProvider.NextQueryId</c> (a monotonic query-id counter) and
    /// <c>TplEventSource.CreateGuidForTaskID</c> (a deterministic Guid from the task
    /// id).</summary>
    private static bool IsRealValueProviderMember(string declType, string name) =>
        (declType == "System.Linq.Parallel.PlinqEtwProvider" && name == "NextQueryId")
        || (declType == "System.Threading.Tasks.TplEventSource" && name == "CreateGuidForTaskID");

    /// <summary>The runtime primitives the real <c>ArrayPool&lt;T&gt;.Shared</c> BCL IL
    /// reaches, each an in-CoreLib MethodDef call whose real body is a QCall/InternalCall or
    /// native-GC read dn2cpp does not model. Lowered inline; the reachability edges are cut
    /// in Compilation.ResolveCallTarget.
    /// <list type="bullet">
    /// <item><c>Environment.GetEnvironmentVariableCore_NoArrayPool(string)</c> →
    /// dn2cpp_env_get_variable (the same helper as GetEnvironmentVariable);</item>
    /// <item><c>Environment.get_TickCount64</c> → dn2cpp_tickcount64 (monotonic
    /// milliseconds). The int TickCount getter is plain IL over this and transpiles;</item>
    /// <item><c>Utilities.GetMemoryPressure</c> → Low (0). The real body reads
    /// GC.GetGCMemoryInfo — native GC accounting dn2cpp does not model. Only the trim
    /// heuristic consumes it, degrading trimming to time-based-only;</item>
    /// <item><c>Gen2GcCallback.Register(Func&lt;bool&gt;)</c> /
    /// <c>Register(Func&lt;object,bool&gt;, object)</c> → a no-op (drop the delegate).
    /// Boehm has no gen2-notification hook; the only framework use is ArrayPool trim
    /// scheduling, and pool trimming is QoS, not contract — parked buffers simply stay
    /// parked. Cutting the class also keeps its CriticalFinalizerObject/WeakGCHandle
    /// machinery out of the tree.</item>
    /// </list></summary>
    private bool TryEmitArrayPoolRuntimePrimitive(MethodInfo callee)
    {
        switch (callee.DeclaringClass.FullName, callee.Name)
        {
            case ("System.Environment", "GetEnvironmentVariableCore_NoArrayPool")
                when callee.IsStatic && callee.Signature.ParameterTypes is [{ IsString: true }]:
            {
                var nm = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_env_get_variable({Cast(nm, "Dn2CppString*")})");
                return true;
            }
            case ("System.Environment", "get_TickCount64")
                when callee.IsStatic && callee.Signature.ParameterTypes.Length == 0:
                Push(StackKind.I8, "int64_t", "dn2cpp_tickcount64()");
                return true;
            case ("System.Buffers.Utilities", "GetMemoryPressure")
                when callee.IsStatic && callee.Signature.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", "0");
                return true;
            case ("System.Gen2GcCallback", "Register") when callee.IsStatic:
            {
                for (int i = 0; i < callee.Signature.ParameterTypes.Length; i++)
                    Pop(); // the callback delegate (+ optional target) — dropped
                return true;
            }
        }
        return false;
    }

    /// <summary><c>Environment.GetEnvironmentVariableFromRegistry(string variable, bool
    /// fromMachine)</c> → dn2cpp_env_get_variable_from_registry, the User/Machine target of
    /// GetEnvironmentVariable. Its real Windows body reads HKCU\Environment /
    /// HKLM\...\Session Manager\Environment through Internal.Win32.RegistryKey — a
    /// SafeRegistryHandle wrapper over Advapi32 P/Invokes with no intrinsic mapping; the
    /// runtime helper reads the same key so native and real .NET agree. The reachability edge
    /// is cut in Compilation.ResolveCallTarget via CoreIntrinsics.LoweredRuntimePrimitive.
    /// Declines any other shape — the EmitRuntimePrimitive dispatcher turns that into the
    /// loud throw.</summary>
    private bool TryEmitEnvironmentRegistry(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName != "System.Environment"
            || callee.Name != "GetEnvironmentVariableFromRegistry"
            || !callee.IsStatic
            || callee.Signature.ParameterTypes is not
                [{ IsString: true }, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }])
            return false;
        var fromMachine = Pop();
        var variable = Pop();
        Push(StackKind.Ref, "Dn2CppString*",
            $"dn2cpp_env_get_variable_from_registry({Cast(variable, "Dn2CppString*")}, "
            + $"{Cast(fromMachine, "int32_t")})");
        return true;
    }

    /// <summary>Regex's <c>Compile</c> factory folded to a null RegexRunnerFactory — the
    /// RegexOptions.Compiled arm is guarded by
    /// <c>RuntimeFeature.IsDynamicCodeSupported</c>/<c>IsDynamicCodeCompiled</c>, which the
    /// const-fold oracle pins to false (see <see cref="CoreIntrinsics.ConstFoldedGetter"/>),
    /// so the call is emitted-dead and RegexOptions.Compiled degrades to the interpreter,
    /// exactly like NativeAOT. The reachability edges are cut in
    /// Compilation.ResolveCallTarget so RegexCompiler and the Reflection.Emit closure never
    /// enter the tree.</summary>
    private bool TryEmitRegexCompileFold(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName == "System.Text.RegularExpressions.Regex"
            && callee.Name == "Compile" && callee.IsStatic)
        {
            for (int i = 0; i < callee.Signature.ParameterTypes.Length; i++)
                Pop(); // pattern/tree/options/timeout — discarded (emitted-dead)
            Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
            return true;
        }
        return false;
    }

    /// <summary>Const-folded capability getters (the invariant-globalization
    /// posture; the full mechanism is documented on
    /// <see cref="CoreIntrinsics.ConstFoldedGetter"/>): the call site pushes the
    /// constant. The reachability edge is cut in Compilation.ResolveCallTarget,
    /// and a guard branching directly on the result has its dead arm pruned
    /// from both reachability and emission by <see cref="BranchLiveness"/>.</summary>
    private bool TryEmitConstFoldedGetter(MethodInfo callee)
    {
        if (CoreIntrinsics.ConstFoldedGetter(callee.DeclaringClass.FullName, callee.Name) is bool value)
        {
            Push(StackKind.I4, "int32_t", value ? "1" : "0");
            return true;
        }
        return false;
    }

    /// <summary>Const-folded string-returning calls (the oracle and the reasoning are on
    /// <see cref="CoreIntrinsics.ConstFoldedStringCall"/>): the arguments are discarded and
    /// the call site pushes the interned literal. Unlike
    /// <see cref="TryEmitConstFoldedGetter"/> these are not zero-argument getters and the
    /// constant prunes no branch — the fold IS the answer every live exit of the real body
    /// returns under this binary's invariant-globalization posture. The reachability edge is
    /// cut in Compilation.ResolveCallTarget, so the real body never enters the tree.</summary>
    private bool TryEmitConstFoldedStringCall(MethodInfo callee)
    {
        if (CoreIntrinsics.ConstFoldedStringCall(callee.DeclaringClass.FullName, callee.Name) is string text)
        {
            for (int i = 0; i < callee.Signature.ParameterTypes.Length; i++)
                Pop(); // culture / registry key / resource id — discarded (emitted-dead)
            Push(StackKind.Ref, "Dn2CppString*", Literals.GetOrAdd(text));
            return true;
        }
        return false;
    }

    /// <summary>System.Globalization.Ordinal's OrdinalIgnoreCase comparison primitives,
    /// reached as intra-corelib MethodDef calls from OrdinalIgnoreCaseComparer. Their real
    /// bodies fall through a SIMD/scalar ASCII fast path into
    /// CompareStringIgnoreCaseNonAscii -> GlobalizationMode.get_Invariant -> the ICU
    /// InitICUFunctions InternalCall — the globalization carve-out. Lower the reached
    /// overloads to the ordinal-fold runtime helpers (the exact BMP OrdinalCasing.ToUpper
    /// fold; dn2cpp_ordinal_casing.cpp), keeping the ICU cascade unreachable. The
    /// reachability edges are cut in Compilation.ResolveCallTarget's MethodDefinition
    /// branch. Other Ordinal members are left to normal transpilation.</summary>
    private bool TryEmitOrdinalIgnoreCaseIntrinsic(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName != "System.Globalization.Ordinal")
            return false;
        switch (callee.Name)
        {
            // EqualsIgnoreCase(ref char charA, ref char charB, int length): the caller has
            // already checked equal lengths; compare `length` folded code units.
            case "EqualsIgnoreCase"
                when callee.Signature.ParameterTypes is
                    [{ Kind: TypeKind.ByRef }, { Kind: TypeKind.ByRef },
                     { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var len = Pop();
                var b = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_chars_equals_ignorecase_ordinal((const char16_t*)({a.Expr}), "
                    + $"(const char16_t*)({b.Expr}), {len.Expr})");
                return true;
            }
            // CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB):
            // the three-way OrdinalIgnoreCase compare.
            case "CompareStringIgnoreCase"
                when callee.Signature.ParameterTypes is
                    [{ Kind: TypeKind.ByRef }, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 },
                     { Kind: TypeKind.ByRef }, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var lenB = Pop();
                var b = Pop();
                var lenA = Pop();
                var a = Pop();
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_chars_compare_ignorecase_ordinal((const char16_t*)({a.Expr}), {lenA.Expr}, "
                    + $"(const char16_t*)({b.Expr}), {lenB.Expr})");
                return true;
            }
            // IndexOfOrdinalIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value):
            // the OrdinalIgnoreCase substring scan behind
            // MemoryExtensions.IndexOf(…, OrdinalIgnoreCase).
            case "IndexOfOrdinalIgnoreCase"
                when callee.Signature.ParameterTypes.Length == 2:
            {
                var value = Pop();
                var source = Pop();
                string sv = NewTemp(source.CppType);
                Emit($"{sv} = {source.Expr};");
                string vv = NewTemp(value.CppType);
                Emit($"{vv} = {value.Expr};");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_chars_indexof_ignorecase_ordinal((const char16_t*){sv}.f__reference, {sv}.f__length, "
                    + $"(const char16_t*){vv}.f__reference, {vv}.f__length)");
                return true;
            }
        }
        return false;
    }

    /// <summary>True if a parameter is a native-int pointer handle (IntPtr/UIntPtr) —
    /// used to select the IntPtr-based Marshal.Read*/Write* overloads over the obsolete
    /// object-based forms.</summary>
    private static bool IsIntPtrParam(TypeDesc t) =>
        t.Kind == TypeKind.Primitive
        && t.Primitive is PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr;

    /// <summary>The byte address (<c>uint8_t*</c>) for a Marshal.Read*/Write* access: the
    /// IntPtr base, plus an optional int byte offset.</summary>
    private static string MarshalByteAddr(StackEntry ptr, StackEntry? ofs) =>
        ofs is { } o
            ? $"((uint8_t*)({ptr.Expr}) + (intptr_t)({o.Expr}))"
            : $"((uint8_t*)({ptr.Expr}))";

    /// <summary>The subset of System.Runtime.InteropServices.Marshal dn2cpp lowers: the
    /// unmanaged-heap allocators (AllocHGlobal/FreeHGlobal and the CoTaskMem aliases), the
    /// string encode/decode pairs, Copy (memcpy between a managed blittable array and a
    /// native pointer, both directions), the typed Read*/Write* accessors, and the
    /// layout-dependent Type-based forms (OffsetOf/SizeOf/PtrToStructure/StructureToPtr).</summary>
    private bool TryEmitMarshalIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        switch (name)
        {
            case "AllocHGlobal": // AllocHGlobal(int cb) | AllocHGlobal(IntPtr cb) -> IntPtr
            {
                var cb = Pop();
                Push(StackKind.I8, "intptr_t", $"(intptr_t)dn2cpp_native_alloc((size_t)({cb.Expr}))");
                return true;
            }
            case "FreeHGlobal": // FreeHGlobal(IntPtr hglobal)
            {
                var h = Pop();
                Emit($"dn2cpp_native_free((void*)({h.Expr}));");
                return true;
            }
            // PtrToStringUTF8/Ansi(IntPtr [, int byteLen]) -> a managed string decoded from
            // a caller-owned buffer (NOT freed — unlike the string-return marshaller). The
            // 1-arg form scans to the NUL and returns null for a null pointer, matching
            // .NET. PtrToStringUTF8 decodes UTF-8; PtrToStringAnsi the host default narrow
            // encoding (the system code page on Windows, UTF-8 on Unix).
            case "PtrToStringUTF8" or "PtrToStringAnsi" when sig.ParameterTypes.Length == 1:
            {
                string dec = name == "PtrToStringAnsi"
                    ? "dn2cpp_string_from_ansi" : "dn2cpp_string_from_utf8";
                var ptr = Pop();
                string t = NewTemp("const char*");
                Emit($"{t} = (const char*)({ptr.Expr});");
                Push(StackKind.Ref, "Dn2CppString*",
                    $"({t} == nullptr ? (Dn2CppString*)nullptr : {dec}({t}, (int32_t)::strlen({t})))");
                return true;
            }
            case "PtrToStringUTF8" or "PtrToStringAnsi" when sig.ParameterTypes.Length == 2:
            {
                string dec = name == "PtrToStringAnsi"
                    ? "dn2cpp_string_from_ansi" : "dn2cpp_string_from_utf8";
                var len = Pop();
                var ptr = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"{dec}((const char*)({ptr.Expr}), (int32_t)({len.Expr}))");
                return true;
            }
            // PtrToStringUni(IntPtr [, int len]) -> a managed string copied from a
            // caller-owned UTF-16 buffer (the internal representation, no transcoding;
            // never freed). The 1-arg form scans to the NUL in the runtime helper and
            // returns null for a null pointer, matching .NET.
            case "PtrToStringUni" when sig.ParameterTypes.Length == 1:
            {
                var ptr = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_marshal_ptr_to_string_utf16((const char16_t*)({ptr.Expr}))");
                return true;
            }
            case "PtrToStringUni" when sig.ParameterTypes.Length == 2:
            {
                var len = Pop();
                var ptr = Pop();
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_string_from_chars((const char16_t*)({ptr.Expr}), (int32_t)({len.Expr}))");
                return true;
            }
            // StringTo{HGlobal,CoTaskMem}{Ansi,Uni} / StringToCoTaskMemUTF8 (string) ->
            // IntPtr: encode into a caller-owned NUL-terminated native-heap buffer (HGlobal
            // and CoTaskMem share the process-heap allocator, so both free through
            // dn2cpp_native_free). The Ansi forms use the host default narrow encoding;
            // StringToCoTaskMemUTF8 is always UTF-8. null -> IntPtr.Zero.
            case "StringToHGlobalAnsi" or "StringToCoTaskMemAnsi":
            {
                var s = Pop();
                Push(StackKind.I8, "intptr_t",
                    $"(intptr_t)dn2cpp_marshal_string_to_ansi({Cast(s, "Dn2CppString*")})");
                return true;
            }
            case "StringToCoTaskMemUTF8":
            {
                var s = Pop();
                Push(StackKind.I8, "intptr_t",
                    $"(intptr_t)dn2cpp_marshal_string_to_utf8({Cast(s, "Dn2CppString*")})");
                return true;
            }
            case "StringToHGlobalUni" or "StringToCoTaskMemUni":
            {
                var s = Pop();
                Push(StackKind.I8, "intptr_t",
                    $"(intptr_t)dn2cpp_marshal_string_to_utf16({Cast(s, "Dn2CppString*")})");
                return true;
            }
            // ZeroFree{GlobalAlloc,CoTaskMem}{Ansi,Unicode} / ZeroFreeCoTaskMemUTF8
            // (IntPtr) -> void: wipe the NUL-terminated buffer, then free it.
            // IntPtr.Zero is a no-op.
            case "ZeroFreeGlobalAllocAnsi" or "ZeroFreeCoTaskMemAnsi" or "ZeroFreeCoTaskMemUTF8":
            {
                var h = Pop();
                Emit($"dn2cpp_marshal_zero_free_utf8((void*)({h.Expr}));");
                return true;
            }
            case "ZeroFreeGlobalAllocUnicode" or "ZeroFreeCoTaskMemUnicode":
            {
                var h = Pop();
                Emit($"dn2cpp_marshal_zero_free_utf16((void*)({h.Expr}));");
                return true;
            }
            case "Copy":
                return TryEmitMarshalCopy(sig);
            // ReAllocHGlobal(IntPtr pv, IntPtr cb) -> IntPtr: grow/shrink a native block
            // in place (realloc behind the IntPtr handle). AllocCoTaskMem(int)/
            // ReAllocCoTaskMem(IntPtr, int)/FreeCoTaskMem(IntPtr) share the same
            // process-heap allocator here (dn2cpp has no separate COM task allocator).
            case "ReAllocHGlobal" when sig.ParameterTypes.Length == 2:
            case "ReAllocCoTaskMem" when sig.ParameterTypes.Length == 2:
            {
                var cb = Pop();
                var pv = Pop();
                Push(StackKind.I8, "intptr_t",
                    $"(intptr_t)dn2cpp_native_realloc((void*)({pv.Expr}), (size_t)({cb.Expr}))");
                return true;
            }
            case "AllocCoTaskMem" when sig.ParameterTypes.Length == 1:
            {
                var cb = Pop();
                Push(StackKind.I8, "intptr_t", $"(intptr_t)dn2cpp_native_alloc((size_t)({cb.Expr}))");
                return true;
            }
            case "FreeCoTaskMem" when sig.ParameterTypes.Length == 1:
            {
                var h = Pop();
                Emit($"dn2cpp_native_free((void*)({h.Expr}));");
                return true;
            }
            // Read{Byte,Int16,Int32,Int64,IntPtr}(IntPtr ptr [, int ofs]) -> a typed
            // load from native memory at ptr (+ a byte offset). Only the IntPtr-based
            // overloads — the obsolete object-based forms (first parameter object) are
            // carved out by the IsIntPtrParam guard. A byte/short result is the IL stack's
            // int32 (zero-/sign-extended); long/IntPtr keep their width.
            case "ReadByte" or "ReadInt16" or "ReadInt32" or "ReadInt64" or "ReadIntPtr"
                when sig.ParameterTypes is [{ } rp0, ..] && IsIntPtrParam(rp0)
                    && sig.ParameterTypes.Length is 1 or 2:
            {
                StackEntry? ofs = sig.ParameterTypes.Length == 2 ? Pop() : null;
                var ptr = Pop();
                string addr = MarshalByteAddr(ptr, ofs);
                var (sk, ct, expr) = name switch
                {
                    "ReadByte" => (StackKind.I4, "int32_t", $"(int32_t)(*(uint8_t*)({addr}))"),
                    "ReadInt16" => (StackKind.I4, "int32_t", $"(int32_t)(*(int16_t*)({addr}))"),
                    "ReadInt32" => (StackKind.I4, "int32_t", $"(*(int32_t*)({addr}))"),
                    "ReadInt64" => (StackKind.I8, "int64_t", $"(*(int64_t*)({addr}))"),
                    _ => (StackKind.I8, "intptr_t", $"(*(intptr_t*)({addr}))"), // ReadIntPtr
                };
                Push(sk, ct, expr);
                return true;
            }
            // Write{Byte,Int16,Int32,Int64,IntPtr}(IntPtr ptr [, int ofs], val) -> a typed
            // store to native memory. The value is the last argument; the offset overload
            // adds an int byte offset between the pointer and the value.
            case "WriteByte" or "WriteInt16" or "WriteInt32" or "WriteInt64" or "WriteIntPtr"
                when sig.ParameterTypes is [{ } wp0, ..] && IsIntPtrParam(wp0)
                    && sig.ParameterTypes.Length is 2 or 3:
            {
                var val = Pop();
                StackEntry? ofs = sig.ParameterTypes.Length == 3 ? Pop() : null;
                var ptr = Pop();
                string addr = MarshalByteAddr(ptr, ofs);
                string ct = name switch
                {
                    "WriteByte" => "uint8_t",
                    "WriteInt16" => "int16_t",
                    "WriteInt32" => "int32_t",
                    "WriteInt64" => "int64_t",
                    _ => "intptr_t", // WriteIntPtr
                };
                Emit($"*({ct}*)({addr}) = ({ct})({val.Expr});");
                return true;
            }
            // OffsetOf(Type t, string fieldName) -> IntPtr. The non-generic Type form of
            // OffsetOf<T>: the type rides the stack as a typeof handle (its TypeToken), the
            // field name as a string literal. The answer is the field's MARSHALLED offset
            // from the layout model — never offsetof() of the emitted C++ struct, which is a
            // different layout for anything non-blittable.
            case "OffsetOf" when sig.ParameterTypes is [{ } tp, _] && IsSystemTypeParam(tp):
            {
                var field = Pop();
                var typeArg = Pop();
                if (typeArg.TypeToken is not { } tt)
                    throw new NotSupportedException(
                        $"{Method.DeclaringClass.FullName}.{Method.Name}: Marshal.OffsetOf(Type, …) "
                        + "requires a typeof(T) literal (a runtime Type value is carved out)");
                Push(StackKind.I8, "intptr_t", $"(intptr_t){MarshalFieldOffset(tt, field)}");
                return true;
            }
            // SizeOf(Type) -> int. The runtime helper runs the marshalling verdict
            // (dn2cpp_marshal_require_size) and, past it, reads the type-info's instanceSize
            // — which IS the marshalled size for a blittable value type, and is refused for
            // everything else. Works for a runtime Type, not only a typeof literal.
            // (SizeOf(object) — the boxed-instance overload — is carved out.)
            case "SizeOf" when sig.ParameterTypes is [{ } stp] && IsSystemTypeParam(stp):
            {
                var t = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_marshal_sizeof({Cast(t, "Dn2CppType*")})");
                return true;
            }
            // PtrToStructure(IntPtr ptr, Type t) -> object: box a struct read from
            // native memory. The runtime helper boxes instanceSize bytes under t's type-info.
            case "PtrToStructure" when sig.ParameterTypes is [_, { } ptp] && IsSystemTypeParam(ptp):
            {
                var t = Pop();
                var ptr = Pop();
                Push(StackKind.Ref, "Dn2CppObject*",
                    $"dn2cpp_marshal_ptr_to_structure((void*)({ptr.Expr}), {Cast(t, "Dn2CppType*")})");
                return true;
            }
            // StructureToPtr(object structure, IntPtr ptr, bool fDeleteOld) -> void:
            // copy a boxed struct's payload to native memory. The size comes from the boxed
            // object's own type-info (its header), so no Type argument is needed.
            case "StructureToPtr" when sig.ParameterTypes.Length == 3 && sig.ParameterTypes[0].IsObject:
            {
                Pop();              // fDeleteOld — a blittable struct owns no unmanaged sub-resources
                var ptr = Pop();
                var structure = Pop();
                Emit($"dn2cpp_marshal_structure_to_ptr({Cast(structure, "Dn2CppObject*")}, (void*)({ptr.Expr}));");
                return true;
            }
            // GetLastWin32Error / GetLastPInvokeError -> int: the cached P/Invoke error from
            // the most recent SetLastError=true call on this thread (errno on Unix). Both
            // names read the same per-thread slot. GetLastSystemError is deliberately NOT
            // mapped — it reads the LIVE OS errno, a different value — so it stays a
            // carve-out.
            case "GetLastWin32Error" or "GetLastPInvokeError" when sig.ParameterTypes.Length == 0:
                Push(StackKind.I4, "int32_t", "dn2cpp_marshal_get_last_error()");
                return true;
            // SetLastPInvokeError(int) -> void: write the cached P/Invoke error slot (the
            // inverse of the getters). SetLastSystemError sets LIVE errno instead — carved
            // out with GetLastSystemError.
            case "SetLastPInvokeError" when sig.ParameterTypes.Length == 1:
            {
                var e = Pop();
                Emit($"dn2cpp_pinvoke_set_last_error({Cast(e, "int32_t")});");
                return true;
            }
        }
        return false;
    }

    /// <summary>The non-generic MemoryMarshal.GetArrayDataReference(Array) overload —
    /// ref to element 0 (typed ref byte) of an array whose element type is unknown at
    /// the call site. The real body is RawData + GetMethodTable(array)-&gt;BaseSize
    /// pointer math we don't model, so lower to dn2cpp_pinned_data_addr, which
    /// discovers the array's rank and rep (MD, or SZ Ref/I4/packed-N) from the runtime
    /// type-info and returns element 0's address — the flat payload head at rank &gt;= 2,
    /// which is what .NET answers there too. Both askers reach this arm: the
    /// app-facing MemberRef (<see cref="CoreIntrinsics.MrMemoryMarshalArrayData"/>) and
    /// the intra-CoreLib MethodDef (<see cref="CoreIntrinsics.MdMemoryMarshalArrayData"/>).
    /// No bounds check — .NET returns the ref unconditionally, even for an empty array.
    /// DECLARED DIVERGENCE: on a null array .NET throws NullReferenceException at the call,
    /// while the helper returns nullptr and the fault surfaces at the first
    /// dereference.</summary>
    private bool TryEmitMemoryMarshalIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        switch (name)
        {
            case "GetArrayDataReference" when sig.GenericParameterCount == 0 && sig.ParameterTypes.Length == 1:
            {
                var arr = Pop();
                Push(StackKind.Ptr, "uint8_t*",
                    $"(uint8_t*)dn2cpp_pinned_data_addr({Cast(arr, "Dn2CppObject*")})");
                return true;
            }
        }
        return false;
    }

    /// <summary>The System.IO.Path / System.IO.File lowerings: the pure-lexical path ops
    /// to the dn2cpp_path_* runtime helpers (Unix '/' separator), and file
    /// read/write/exists/delete to the dn2cpp_file_* libc helpers. Semantics match real
    /// .NET exactly (probed).
    ///
    /// <para>Total over <see cref="IoLowering"/>, and the only source of an
    /// <see cref="IoLowering"/> is <see cref="CoreIntrinsics.LoweredIoMember"/> — the same
    /// predicate <c>Compilation.ResolveCallTarget</c> asks to decide whether the real
    /// body's reachability edge is cut. So an overload arrives here if and only if its edge
    /// was cut, and an overload whose edge was NOT cut lands on its real BCL body instead.
    /// There is no third outcome.</para>
    ///
    /// <para>FileReadAllBytes notes the byte element type so its result carries the precise
    /// ti_arr_Byte handle (GetType() == Byte[]).</para></summary>
    private void EmitIoIntrinsic(IoLowering io)
    {
        switch (io)
        {
            case IoLowering.PathGetFileName:
                EmitPathUnary("dn2cpp_path_get_filename");
                return;
            case IoLowering.PathGetDirectoryName:
                EmitPathUnary("dn2cpp_path_get_directory_name");
                return;
            case IoLowering.PathGetExtension:
                EmitPathUnary("dn2cpp_path_get_extension");
                return;
            case IoLowering.PathGetFileNameWithoutExtension:
                EmitPathUnary("dn2cpp_path_get_filename_without_extension");
                return;
            case IoLowering.PathGetFullPath:
                EmitPathUnary("dn2cpp_path_get_full_path");
                return;
            case IoLowering.PathIsPathRooted:
            {
                var arg = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_path_is_rooted({Cast(arg, "Dn2CppString*")})");
                return;
            }
            case IoLowering.PathCombine2:
                EmitPathCombine(2, "dn2cpp_path_combine2");
                return;
            case IoLowering.PathCombine3:
                EmitPathCombine(3, "dn2cpp_path_combine3");
                return;
            case IoLowering.PathCombine4:
                EmitPathCombine(4, "dn2cpp_path_combine4");
                return;
            case IoLowering.FileExists:
            {
                var path = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_file_exists({Cast(path, "Dn2CppString*")})");
                return;
            }
            case IoLowering.FileDelete:
            {
                var path = Pop();
                Emit($"dn2cpp_file_delete({Cast(path, "Dn2CppString*")});");
                return;
            }
            case IoLowering.FileReadAllText:
            {
                var path = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_file_read_all_text({Cast(path, "Dn2CppString*")})");
                return;
            }
            case IoLowering.FileWriteAllText:
            {
                var contents = Pop();
                var path = Pop();
                Emit($"dn2cpp_file_write_all_text({Cast(path, "Dn2CppString*")}, {Cast(contents, "Dn2CppString*")});");
                return;
            }
            case IoLowering.FileReadAllBytes:
            {
                var byteElem = TypeDesc.MakePrimitive(PrimitiveTypeCode.Byte);
                Comp.NoteArrayElementType(byteElem); // emit ti_arr_Byte so GetType is Byte[]
                var path = Pop();
                Push(StackKind.Ref, "Dn2CppArrayN*",
                    $"dn2cpp_file_read_all_bytes({Cast(path, "Dn2CppString*")}, {PreciseArrayTypeInfoExpr(byteElem)})");
                return;
            }
            case IoLowering.FileWriteAllBytes:
            {
                var bytes = Pop();
                var path = Pop();
                Emit($"dn2cpp_file_write_all_bytes({Cast(path, "Dn2CppString*")}, {Cast(bytes, "Dn2CppArrayN*")});");
                return;
            }
        }
        // Unreachable: the switch is total over IoLowering. A new lowering added to the
        // enum without an arm here lands on this throw rather than on a silent miscompile.
        throw new NotSupportedException(
            $"internal: {nameof(IoLowering)}.{io} has no emit arm");
    }

    /// <summary>A single-string-argument lexical path op: pop the path, push the call.</summary>
    private void EmitPathUnary(string fn)
    {
        var arg = Pop();
        Push(StackKind.Ref, "Dn2CppString*", $"{fn}({Cast(arg, "Dn2CppString*")})");
    }

    /// <summary>Path.Combine's fixed-arity all-string overloads. Pop in reverse so args[]
    /// is left-to-right, then call the arity helper.</summary>
    private void EmitPathCombine(int arity, string fn)
    {
        var args = new StackEntry[arity];
        for (int i = arity - 1; i >= 0; i--) args[i] = Pop();
        Push(StackKind.Ref, "Dn2CppString*",
            $"{fn}({string.Join(", ", args.Select(a => Cast(a, "Dn2CppString*")))})");
    }

    /// <summary>System.Buffer.BlockCopy(Array src, int srcOffset, Array dst, int dstOffset,
    /// int count) — a byte-granular block move — Buffer.ByteLength(Array), the extent it
    /// bounds itself by, and Buffer.Get/SetByte, one byte of it. Their real bodies reach the
    /// <c>Array.NativeLength</c> / <c>GetCorElementTypeOfElementType</c> /
    /// <c>GetElementSize</c> InternalCalls (element-size + primitive-blittable validation);
    /// lower them to <c>dn2cpp_buffer_blockcopy</c> / <c>dn2cpp_buffer_bytelength</c> /
    /// <c>_getbyte</c> / <c>_setbyte</c>, which carry that same validation.
    /// srcOffset/dstOffset/count are already in bytes (BlockCopy's contract), so no
    /// element-size scaling is needed — the guard's bounds are the operand's byte extent,
    /// which is why it takes the rep. The reachability edge is cut in
    /// Compilation.ResolveCallTarget. Any other Buffer member falls through to a loud
    /// failure (no silent carve-out).</summary>
    private bool TryEmitBufferIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        if (name == "BlockCopy" && sig.ParameterTypes.Length == 5)
        {
            var count = Pop();
            var dstOffset = Pop();
            var dst = Pop();
            var srcOffset = Pop();
            var src = Pop();
            Emit($"dn2cpp_buffer_blockcopy({BufferOperand(src)}, (int32_t)({srcOffset.Expr}), " +
                 $"{BufferOperand(dst)}, (int32_t)({dstOffset.Expr}), (int32_t)({count.Expr}));");
            return true;
        }
        if (name == "ByteLength" && sig.ParameterTypes.Length == 1)
        {
            var arr = Pop();
            Push(StackKind.I4, "int32_t", $"dn2cpp_buffer_bytelength({BufferOperand(arr)})");
            return true;
        }
        // GetByte(Array, int) / SetByte(Array, int, byte) — one byte of that same extent,
        // bounded by it. Their real bodies index the non-generic
        // MemoryMarshal.GetArrayDataReference(Array), whose own body is
        // RuntimeHelpers.GetMethodTable BaseSize pointer math with no mapping.
        if (name == "GetByte" && sig.ParameterTypes.Length == 2)
        {
            var index = Pop();
            var arr = Pop();
            Push(StackKind.I4, "int32_t",
                $"(int32_t)dn2cpp_buffer_getbyte({BufferOperand(arr)}, (int32_t)({index.Expr}))");
            return true;
        }
        if (name == "SetByte" && sig.ParameterTypes.Length == 3)
        {
            var value = Pop();
            var index = Pop();
            var arr = Pop();
            Emit($"dn2cpp_buffer_setbyte({BufferOperand(arr)}, (int32_t)({index.Expr}), "
                 + $"(uint8_t)({value.Expr}));");
            return true;
        }
        // Buffer.BulkMoveWithWriteBarrier(ref byte dst, ref byte src, nuint byteCount) —
        // the overlap-safe move the CLR pairs with GC card-table maintenance. dn2cpp's
        // Boehm GC has no write barrier or GC poll points, so it collapses to a plain
        // memmove (same argument order).
        if (name == "BulkMoveWithWriteBarrier" && sig.ParameterTypes.Length == 3)
        {
            var count = Pop();
            var src = Pop();
            var dst = Pop();
            Emit($"std::memmove((void*)({dst.Expr}), (const void*)({src.Expr}), (size_t)({count.Expr}));");
            return true;
        }
        // Buffer.ZeroMemoryInternal(void* b, nuint byteLength) — the large-block zeroing
        // InternalCall SpanHelpers.ClearWithoutReferences tail-calls into. A plain memset:
        // the caller has already excluded managed references and Boehm needs no card table.
        if (name == "ZeroMemoryInternal" && sig.ParameterTypes.Length == 2)
        {
            var len = Pop();
            var ptr = Pop();
            Emit($"std::memset((void*)({ptr.Expr}), 0, (size_t)({len.Expr}));");
            return true;
        }
        return false;
    }

    /// <summary>System.Text.Ascii.WidenAsciiToUtf16(byte*, char*, nuint) /
    /// NarrowUtf16ToAscii(char*, byte*, nuint) — the leaf ASCII↔UTF-16 transcode helpers,
    /// lowered to scalar runtime helpers. The real bodies branch into SIMD fast paths over
    /// the generic <c>ISimdVector</c> abstraction that dn2cpp does not transpile; at runtime
    /// the SIMD gate folds to false and the scalar fallback runs anyway. Both convert
    /// leading elements until the first non-ASCII element, returning the count converted
    /// (the BCL contract). The edge is cut in <c>Compilation.ResolveCallTarget</c>.</summary>
    private bool TryEmitAsciiTranscode(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName != "System.Text.Ascii")
            return false;
        bool widen = callee.Name == "WidenAsciiToUtf16";
        if (!widen && callee.Name != "NarrowUtf16ToAscii")
            return false;
        // The (src, dst, count) shape this models. A name test alone would pop three operands
        // off an overload that does not have three — a silent miscompile — so decline the
        // shape and let EmitRuntimePrimitive's throw name it.
        if (callee.Signature.ParameterTypes.Length != 3)
            return false;
        var count = Pop();
        var dst = Pop();
        var src = Pop();
        string call = widen
            ? $"dn2cpp_ascii_widen_to_utf16((const uint8_t*)({src.Expr}), (char16_t*)({dst.Expr}), (size_t)({count.Expr}))"
            : $"dn2cpp_ascii_narrow_to_ascii((const char16_t*)({src.Expr}), (uint8_t*)({dst.Expr}), (size_t)({count.Expr}))";
        var rt = callee.Signature.ReturnType;
        Push(CppTypes.KindOf(rt), CppTypes.Of(rt), $"({CppTypes.Of(rt)})({call})");
        return true;
    }

    /// <summary>System.Text.Unicode.Utf8Utility.TranscodeToUtf8 — the strict UTF-16 -> UTF-8
    /// transcode workhorse both Utf8.FromUtf16 and UTF8Encoding.GetBytes forward to — lowers
    /// to a portable .NET-exact helper, bypassing the real DWORD/SIMD pointer-stepped body.
    /// Transpiled, that body mis-advances past a non-ASCII char following an ASCII run,
    /// silently truncating the transcode (it reports Done having consumed fewer chars than
    /// it was given); intercepting the one workhorse fixes every entry. Signature:
    /// TranscodeToUtf8(char* pInputBuffer, int inputLength, byte* pOutputBuffer,
    /// int outputBytesRemaining, out char* pInputBufferRemaining,
    /// out byte* pOutputBufferRemaining) -> OperationStatus. The edge is cut in
    /// Compilation.ResolveCallTarget.</summary>
    private bool TryEmitUtf8Transcode(MethodInfo callee)
    {
        if (callee.DeclaringClass.FullName != "System.Text.Unicode.Utf8Utility"
            || callee.Name != "TranscodeToUtf8")
            return false;
        // The six-operand shape this models — popping six off an overload that does not have
        // six is a silent miscompile, so decline and let EmitRuntimePrimitive's throw name it.
        if (callee.Signature.ParameterTypes.Length != 6)
            return false;
        // Pop top-down: pOutputBufferRemaining (out byte*), pInputBufferRemaining
        // (out char*), outputBytesRemaining (int), pOutputBuffer (byte*),
        // inputLength (int), pInputBuffer (char*).
        var pOutRem = Pop();
        var pInRem = Pop();
        var outLen = Pop();
        var pOut = Pop();
        var inLen = Pop();
        var pIn = Pop();
        string call =
            $"dn2cpp_utf8_transcode_to_utf8((const char16_t*)({pIn.Expr}), {inLen.Expr}, "
            + $"(uint8_t*)({pOut.Expr}), {outLen.Expr}, "
            + $"(const char16_t**)({pInRem.Expr}), (uint8_t**)({pOutRem.Expr}))";
        var rt = callee.Signature.ReturnType;
        Push(CppTypes.KindOf(rt), CppTypes.Of(rt), $"({CppTypes.Of(rt)})({call})");
        return true;
    }

    /// <summary>System.Environment — GetEnvironmentVariable (null when unset), the
    /// CurrentDirectory get/set accessors and ProcessPath lower to the dn2cpp_env_* /
    /// dn2cpp_process_path helpers (getenv / getcwd / chdir / the PAL's executable-path
    /// query; the real ProcessPath body is an Interop.Sys QCall). CommandLine /
    /// GetCommandLineArgs / GetEnvironmentVariables cannot match a native build's command
    /// line / full env against real .NET, so they return false → a hard
    /// NotSupportedException (no silent carve-out).</summary>
    private bool TryEmitEnvironmentIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (name)
        {
            case "GetEnvironmentVariable" when ps is [{ } p] && p.IsString:
            {
                var nm = Pop();
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_env_get_variable({Cast(nm, "Dn2CppString*")})");
                return true;
            }
            case "get_CurrentDirectory" when ps.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", "dn2cpp_env_get_current_directory()");
                return true;
            case "set_CurrentDirectory" when ps is [{ } p] && p.IsString:
            {
                var v = Pop();
                Emit($"dn2cpp_env_set_current_directory({Cast(v, "Dn2CppString*")});");
                return true;
            }
            // Environment.ProcessPath is string? — null on a host with no process
            // image (wasm), the executable's absolute path everywhere else.
            case "get_ProcessPath" when ps.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", "dn2cpp_process_path()");
                return true;
            // Terminates the process through the same funnel as a generated `main`:
            // flush stdio, then _Exit without running static destructors (the detached
            // finalizer thread and pool workers outlive us and would lock destroyed
            // mutexes). As in .NET, this kills the whole process even when the managed
            // code is hosted in a shared library.
            case "Exit" when ps is [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }]:
            {
                var code = Pop();
                Emit($"dn2cpp_environment_exit({Cast(code, "int32_t")});");
                return true;
            }
            // Environment.FailFast — an immediate, uncatchable process abort (no
            // unwinding, no finally, no exit code). Every overload of the five-member
            // family lands here, matched by SHAPE rather than by arity: read the message
            // out of the first string parameter and the exception out of the Exception
            // parameter wherever they sit, and drop the rest (stack-crawl marks and
            // errorMessage are Watson bucketing data with no counterpart here).
            case "FailFast":
            {
                var vals = new StackEntry[ps.Length];
                for (int i = ps.Length - 1; i >= 0; i--)
                    vals[i] = Pop();
                string? msg = null, exc = null;
                for (int i = 0; i < ps.Length; i++)
                {
                    if (msg is null && ps[i].IsString)
                        msg = Cast(vals[i], "Dn2CppString*");
                    else if (exc is null && ps[i] is { Kind: TypeKind.Class, Class.FullName: "System.Exception" }
                                 or { Kind: TypeKind.External, ExternalName: "System.Exception" })
                        exc = Cast(vals[i], "Dn2CppObject*");
                }
                Emit($"dn2cpp_environment_failfast({msg ?? "nullptr"}, {exc ?? "nullptr"});");
                return true;
            }
        }
        return false;
    }

    /// <summary>System.AppContext.BaseDirectory — the directory holding the running
    /// executable, with a trailing separator (dn2cpp_app_base_directory). The real getter
    /// falls back to GetBaseDirectoryCore, which reads the entry assembly's Location; a
    /// native binary has no assembly file on disk, so that chain yields "" and every caller
    /// resolving a sibling file against it silently looks in the wrong place. NativeAOT
    /// reports the executable's directory here, and so does dn2cpp.
    ///
    /// AppContext is NOT an intrinsic-mapped type: TryGetSwitch / GetData transpile as real
    /// IL and are reached. Only this one member is routed, explicitly, from both call paths
    /// in MethodCompiler.Call.</summary>
    private bool TryEmitAppContextIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        if (name == "get_BaseDirectory" && sig.ParameterTypes.Length == 0)
        {
            Push(StackKind.Ref, "Dn2CppString*", "dn2cpp_app_base_directory()");
            return true;
        }
        return false;
    }

    /// <summary>System.IO.Directory — Exists / GetCurrentDirectory / SetCurrentDirectory
    /// lower to the dn2cpp helpers (the cwd get/set share the Environment helpers).
    /// CreateDirectory is NOT intercepted: callers deref the returned DirectoryInfo, so its
    /// real BCL body transpiles. The enumeration overloads transpile real too
    /// (FileSystemEnumerator over the SystemNative_*Dir PAL).</summary>
    private bool TryEmitDirectoryIntrinsic(string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (name)
        {
            case "Exists" when ps is [{ } p] && p.IsString:
            {
                var path = Pop();
                Push(StackKind.I4, "int32_t", $"dn2cpp_directory_exists({Cast(path, "Dn2CppString*")})");
                return true;
            }
            case "GetCurrentDirectory" when ps.Length == 0:
                Push(StackKind.Ref, "Dn2CppString*", "dn2cpp_env_get_current_directory()");
                return true;
            case "SetCurrentDirectory" when ps is [{ } p] && p.IsString:
            {
                var v = Pop();
                Emit($"dn2cpp_env_set_current_directory({Cast(v, "Dn2CppString*")});");
                return true;
            }
        }
        return false;
    }

    /// <summary>True when a parameter's type is <c>System.Type</c> — whether it resolves
    /// as an External ref or a loaded BCL class (under -r). Distinguishes the Type-based
    /// Marshal overloads from their object-based siblings.</summary>
    private static bool IsSystemTypeParam(TypeDesc p) =>
        (p.Kind == TypeKind.External && p.ExternalName == "System.Type")
        || (p.Kind == TypeKind.Class && p.Class?.FullName == "System.Type");

    /// <summary>Lowers the generic <c>Marshal.SizeOf&lt;T&gt;()</c>. The two spellings of this
    /// question — <c>SizeOf&lt;T&gt;()</c> here and <c>SizeOf(typeof(T))</c> through
    /// <c>dn2cpp_marshal_sizeof</c> — must give the SAME answer, so both consult
    /// <see cref="Compilation.TopLevelMarshalExtent"/>: this one folds its number, and the
    /// runtime reads the same number from the <c>marshalSize</c> the emitter stamped from it.
    ///
    /// <para>Three arms, in order:</para>
    /// <list type="number">
    /// <item>A type whose marshalled size the model knows outright — the primitives. NOT a
    /// <c>sizeof</c>: on CoreCLR <c>bool</c> is 4 (the Win32 BOOL default) and <c>char</c>
    /// is 1 (the default Ansi CharSet).</item>
    /// <item>A type whose MARSHALLED layout the model computes — wider than the blittable
    /// set: a <c>bool</c>/<c>char</c>/<c>string</c> field, a <c>[MarshalAs]</c> descriptor
    /// and a <c>[StructLayout(Sequential)]</c> class all answer, none of them the emitted
    /// struct's <c>sizeof</c>. The constant comes from the model, never from <c>sizeof</c>,
    /// so widening the answer cannot start reporting the REPRESENTATION width.</item>
    /// <item>Everything else routes to the runtime verdict, which throws .NET's own
    /// <c>ArgumentException</c> for what .NET refuses (an enum, DateTime, a reference type)
    /// and a diagnosable <c>PlatformNotSupportedException</c> for what dn2cpp cannot size.
    /// With no type-info expression — a shared-body placeholder, a shape with no runtime
    /// metadata — <c>sizeof</c> stands rather than emitting a null handle.</item>
    /// </list></summary>
    private void EmitMarshalSizeOfGeneric(TypeDesc t)
    {
        if (Compilation.MarshalKnownSize(t) is { } known)
        {
            Push(StackKind.I4, "int32_t", known.ToString());
            return;
        }
        // A generic is refused by .NET whatever its layout, so it must not be folded here —
        // it falls through to the runtime verdict, which raises .NET's own distinct message.
        if (t is { Kind: TypeKind.Class, Class: { GenericArity: 0 } mc }
            && _c.TopLevelMarshalExtent(mc) is { IsKnown: true } ext)
        {
            Push(StackKind.I4, "int32_t", ext.Size.ToString());
            return;
        }
        // The type-info has to EXIST for the routed call, and a value type named only here
        // is exactly the SizeOf<T> trap the interop intrinsics' header comment describes —
        // note it so its ti_ is emitted.
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IntrinsicCppName: null } nc })
            _c.NoteReferencedType(nc);
        if (TypeInfoExpr(t) is { } ti)
        {
            Push(StackKind.I4, "int32_t", $"dn2cpp_marshal_require_size({ti})");
            return;
        }
        Push(StackKind.I4, "int32_t", $"(int32_t)sizeof({CppTypes.StorageOf(t)})");
    }

    /// <summary>Lowers <c>Marshal.OffsetOf</c> — both spellings — to the field's MARSHALLED
    /// offset, as a compile-time constant from the marshalled-layout model
    /// (<c>Compilation.MarshalLayout.cs</c>). A non-literal runtime field name is carved
    /// out, which is what lets the answer be a constant at all.
    ///
    /// <para>Never <c>offsetof</c> of the emitted C++ struct: that is the right number only
    /// for a blittable struct, and for a REFERENCE type is not a number at all
    /// (<c>StorageOf</c> a reference type is <c>t_X*</c>). The model computes the unmanaged
    /// layout from the metadata rows instead, so a <c>[StructLayout(Sequential)]</c> class
    /// answers, base chain's fields first.</para></summary>
    private long MarshalFieldOffset(TypeDesc t, StackEntry field)
    {
        if (field.StrLiteral is not { } fname)
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Marshal.OffsetOf needs a "
                + "string-literal field name (a runtime string is carved out)");
        if (t.Class is not { } cls)
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Marshal.OffsetOf<{t}> is not a struct type");
        if (cls.GenericArity > 0)
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Marshal.OffsetOf<{cls.FullName}>: "
                + "the specified type must not be a generic type");
        var fi = cls.Fields.FirstOrDefault(f => f.Name == fname && !f.IsStatic)
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: field '{fname}' not found on {cls.FullName}");
        var layout = _c.TopLevelMarshalLayout(cls)
            ?? throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Marshal.OffsetOf<{cls.FullName}>: "
                + "the type cannot be marshaled as an unmanaged structure, so no meaningful "
                + "offset can be computed (an auto-layout type, or one holding a field with no "
                + "unmanaged form)");
        if (!layout.Extent.IsKnown || !layout.Offsets.TryGetValue(fi, out int off))
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Marshal.OffsetOf<{cls.FullName}>.{fname}: "
                + "real .NET reports an offset here and dn2cpp does not model this type's "
                + $"marshalled layout ({MarshalUnmodeledReason(cls)})");
        return off;
    }

    /// <summary>The shape that took a type out of the marshalled-layout model, named so the
    /// refusal says what to change. Best-effort by design (first declining field, else a
    /// generic sentence): a diagnostic that could itself fail is worse than a vague one.</summary>
    private string MarshalUnmodeledReason(ClassInfo cls)
    {
        foreach (var f in cls.Fields)
        {
            if (f.IsStatic || f.IsLiteral)
                continue;
            if (!_c.MarshalFieldIsModelled(f))
                return $"the field '{f.Name}' has a [MarshalAs] descriptor or a field type "
                    + "whose unmanaged form dn2cpp does not model";
        }
        return "a nested type, a [MarshalAs] descriptor or a layout attribute outside the model";
    }

    /// <summary>Validates and registers TDelegate for the Marshal delegate ⇔ raw
    /// function pointer intrinsics (GetFunctionPointerForDelegate&lt;T&gt; /
    /// GetDelegateForFunctionPointer&lt;T&gt;). The type must be a concrete delegate
    /// whose Invoke signature is fully blittable (the same shape the P/Invoke callback
    /// marshaller accepts); under shared generics a canonical TDelegate taints the
    /// shared body so each real instantiation compiles its own. On success the class
    /// joins <see cref="Compilation.MarshalFnPtrDelegates"/> (thunk-pool emission) and
    /// is force-emitted (its layout, type-info, and dginvoke invoker back the pool).</summary>
    private ClassInfo MarshalFnPtrDelegateClass(TypeDesc t)
    {
        TaintIfCanonical(t, "call");
        if (t.Kind != TypeKind.Class || t.Class is not { IsDelegate: true } dgCls
            || !CppTypes.IsBlittableCallbackDelegate(t))
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Marshal delegate <-> function "
                + $"pointer conversion needs a concrete delegate type with a fully blittable Invoke "
                + $"signature; <{t}> is not (a string/array/bool/char/delegate parameter or return, "
                + "or a canonical shared-generic placeholder, is a carve-out)");
        Comp.MarshalFnPtrDelegates.Add(dgCls);
        Comp.NoteForceEmit(dgCls);
        return dgCls;
    }

    /// <summary>Marshal.Copy(T[] ⇔ IntPtr) for a blittable element T — a raw memcpy of
    /// <c>length * sizeof(T)</c> bytes between the managed array's element storage (at
    /// startIndex) and the native pointer. Direction is read off the signature: the array
    /// parameter is first (managed→native) or second (native→managed).</summary>
    private bool TryEmitMarshalCopy(MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        if (ps.Length != 4)
            return false;
        bool managedToNative = ps[0].Kind == TypeKind.SZArray; // (T[] src, int start, IntPtr dst, int len)
        var arrType = managedToNative ? ps[0] : ps[1];
        if (arrType.Kind != TypeKind.SZArray)
            return false;
        var element = arrType.Element!;
        string st = CppTypes.StorageOf(element);

        // Pop in reverse signature order.
        if (managedToNative)
        {
            var len = Pop();
            var dst = Pop();   // IntPtr destination
            var start = Pop(); // int startIndex into the source array
            var arr = Pop();   // T[] source
            string srcAddr = ArrayElemByteAddr(arr, element, start.Expr);
            Emit($"std::memcpy((void*)({dst.Expr}), (const void*)({srcAddr}), (size_t)({len.Expr}) * sizeof({st}));");
        }
        else // Copy(IntPtr src, T[] dst, int start, int len)
        {
            var len = Pop();
            var start = Pop(); // int startIndex into the destination array
            var arr = Pop();   // T[] destination
            var src = Pop();   // IntPtr source
            string dstAddr = ArrayElemByteAddr(arr, element, start.Expr);
            Emit($"std::memcpy((void*)({dstAddr}), (const void*)({src.Expr}), (size_t)({len.Expr}) * sizeof({st}));");
        }
        return true;
    }

    /// <summary>The byte address (<c>uint8_t*</c>) of element <paramref name="idxExpr"/>
    /// in a blittable array, reusing the ldelema rep switch. Only the I4 (int/uint) and
    /// N (everything else blittable) reps occur for Marshal.Copy element types.</summary>
    private string ArrayElemByteAddr(StackEntry arr, TypeDesc element, string idxExpr) =>
        RepOf(element) switch
        {
            ArrRep.I4 => $"(uint8_t*)&(({Cast(arr, "Dn2CppArrayI4*")})->data[{idxExpr}])",
            _ => $"(uint8_t*)dn2cpp_elem_addr(({Cast(arr, "Dn2CppArrayN*")}), {idxExpr})",
        };

    /// <summary>The base data pointer (cast to <c>void*</c>) of a blittable array,
    /// reusing the ldelema rep switch but WITHOUT a bounds check: an empty array must
    /// still yield a valid (non-faulting) pointer, matching .NET's pin-and-pass. The
    /// inline <c>data</c> member is the first element for both the I4 and N reps; a
    /// Ref-rep array is non-blittable and never reaches here.</summary>
    private string ArrayDataBasePtr(StackEntry arr, TypeDesc element) =>
        RepOf(element) switch
        {
            ArrRep.I4 => $"(void*)({Cast(arr, "Dn2CppArrayI4*")}->data)",
            _ => $"(void*)({Cast(arr, "Dn2CppArrayN*")}->data)",
        };
}
