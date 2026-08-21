using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // ---- call-target resolution: token -> MethodInfo, and its foldable views ----

    /// <summary>Resolves a call/newobj/ldftn target to a MethodInfo, or null
    /// when it is an intrinsic / external (TypeRef-based) reference.</summary>
    private MethodInfo? ResolveCallTarget(Module module, EntityHandle handle, GenericContext ctx)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var h = (MethodDefinitionHandle)handle;
                if (!module.MethodMap.TryGetValue(h, out var mi))
                    return null;
                // System.Exception::get_Message is cut as intrinsic (below), but a call to
                // it — even the non-virtual base.Message inside an override — means the
                // used-virtual × allocated-type cross product must reach every allocated
                // exception type's get_Message override, so `ex.Message` on a base-typed
                // receiver (which folds to the dispatching runtime helper) finds it. Over-
                // reaching on the non-virtual form is harmless.
                ReachExceptionGetMessage(mi);
                // Intrinsic-mapped types are emitted inline, not transpiled, so
                // an intra-assembly call into one (e.g. corelib's Span<T> ->
                // ThrowHelper) is not a reachability edge.
                //
                // This cut deliberately takes ONE of the two intrinsic-type tests —
                // the name table — while the emit route's MethodDefinition arm
                // (MethodCompiler.TranslateCall) also intercepts on
                // ClassInfo.IntrinsicCppName, which additionally catches a nested
                // intrinsic (StringBuilder.AppendInterpolatedStringHandler) and an
                // adopted async task type. So the cut is NARROWER than the route:
                // route-without-cut, the safe direction of the intercept invariant
                // (AGENTS.md — bloat, never a link error). And here it is not even
                // bloat: the resolved edge dies one asker later, at the drain —
                // Compilation.Reach tests IntrinsicCppName itself and returns before
                // scanning any IL — so the kept edge reaches nothing, compiles
                // nothing, and emits nothing. Widening this cut to the route's
                // two-part test leaves the emitted output byte-identical; widen it
                // only if the drain's guard ever moves.
                if (CoreIntrinsics.MdIntrinsicType.Matches(mi))
                    return null;
                // The sub-word integers' ToString/Parse/TryParse/TryFormat are lowered inline
                // at every call site (the MemberRef branch, the MethodDefinition arm of
                // MethodCompiler.TranslateCall, and the constrained static-virtual dispatch),
                // so their real bodies must NOT be a reachability edge: they reach
                // System.Number.Format*/ParseBinaryInteger and with it the whole numeric
                // formatting/parsing subtree — the biggest remaining console self-host
                // cascade, also reached from SRM's SignatureDecoder.CheckHeader via
                // Byte.TryFormat.
                //
                // ONE predicate names the member set here and at the emit route, so the two
                // askers cannot disagree about which overloads are lowered. A hand-kept
                // second list drifts, and the drift is silent: a list here shorter than the
                // MemberRef arm's keeps an intra-CoreLib TryParse's real body — and the
                // whole ParseBinaryInteger cascade behind it — in the tree, while the
                // identical cross-assembly call is cut.
                //
                // (No wide Int32/UInt32/Int64/UInt64 arm belongs here: those four ARE
                // intrinsic types, so the IsIntrinsicType test above has already returned
                // null for every member of them. The sub-word four are not intrinsic
                // types, which is the whole reason this predicate exists.)
                if (CoreIntrinsics.MdInlinePrimitive.Matches(mi))
                    return null;
                // System.Collections.Comparer.Compare(object,object) is body-intercepted: its
                // real IL (`x as IComparable` -> ArgumentException / CompareInfo, which a boxed
                // primitive cannot satisfy) is NOT a reachability edge. The emit route
                // (MethodCompiler.TranslateCall) and the synthesized body both lower it to
                // dn2cpp_object_compare — the SAME predicate names the member at both askers.
                if (CoreIntrinsics.MdComparerCompare.Matches(mi))
                    return null;
                // The intra-CoreLib runtime primitives lowered inline at every MethodDefinition
                // call site (GC's finalizer/collect/KeepAlive entry points, SpanHelpers.Memmove,
                // Marshal's errno slot + UTF-8 decoder, NativeLibrary.GetSymbol,
                // Interop.GetRandomBytes, the entry-assembly InternalCall): their real
                // FCall/InternalCall/QCall bodies must NOT be a reachability edge.
                //
                // ONE predicate names the member set here and at the emit route
                // (MethodCompiler.TranslateCall -> EmitRuntimePrimitive), so the two askers
                // cannot disagree about which overloads are lowered — and cannot disagree in
                // KIND either: a cut by NAME against a route matching the SIGNATURE deletes
                // the body of an overload upstream adds to one of these families and lowers
                // nothing in its place — a declined route does not fail, it falls through to
                // EmitManagedCall. The transpile SUCCEEDS and the C++ link does not, on an
                // undefined symbol with no cause attached.
                //
                // The predicate is name-keyed, and the emitter is total over it (an unmodeled
                // shape throws there rather than falling out the bottom) — see
                // CoreIntrinsics.LoweredRuntimePrimitive for why a loud throw, not a
                // fall-through, is the right remedy for THIS arm's families.
                if (CoreIntrinsics.MdRuntimePrimitive.Matches(mi))
                    return null;
                // Framework EventSource tracing guards (`if (Log.IsEnabled()) Log.Event(…)`
                // in ConcurrentDictionary's lock path, ArrayPool's Rent/Return, ...):
                // EventSource.IsEnabled() is folded to false and the provider's event
                // methods to no-ops (TryEmitEventSourceNoOp); the provider singleton is
                // never instantiated (its .cctor is skipped in ReachCctor). So neither is
                // a reachability edge — cutting them keeps the EventSource
                // manifest/finalizer cascade (Guid/HexConverter/reflection/
                // ResourceManager/ICU) out of the tree.
                //
                // DEAD in this chain: EventSource is an s_intrinsicTypes member, so the
                // MdIntrinsicType row above has already answered null for every member of
                // it. Kept at its position because the row is real — the emit chain asks
                // it AHEAD of its own intrinsic-type route, where it does fire.
                if (CoreIntrinsics.MdEventSourceIsEnabled.Matches(mi))
                    return null;
                if (IsFrameworkEventSourceProvider(mi.DeclaringClass))
                    return null;
                // Const-folded getters (see CoreIntrinsics.ConstFoldedGetter) are
                // emitted as constants, never as calls. The edge must not exist:
                // GlobalizationMode.get_Invariant's real body reads Settings..cctor
                // -> LoadAppLocalIcu -> InitICUFunctions, the ICU-loading icall
                // cascade, and RuntimeFeature's dynamic-code probes read an
                // AppContext switch defaulting to true — wrong for a native binary.
                if (CoreIntrinsics.MdConstFoldedGetter.Matches(mi))
                    return null;
                // Const-folded string-returning calls (see
                // CoreIntrinsics.ConstFoldedStringCall) are emitted as literals, never as
                // calls. These are the Windows-CoreLib TimeZoneInfo display-name
                // internals, whose real bodies reach User32.LoadString and
                // CultureInfo.get_Parent. Unlike the getters above, BranchLiveness cannot
                // reach them at all: the win-x64 GetUtcStandardDisplayName is guarded by
                // TimeZoneInfo.get_Invariant (not in the fold table) and wraps its
                // registry read in a `using (RegistryKey)`, so every dead offset sits
                // inside an exception region — the one shape BranchLiveness bails on by
                // design. The POSIX body of the same name folds itself and does not need
                // this row; it matches it anyway, harmlessly, on the same constant.
                if (CoreIntrinsics.MdConstFoldedStringCall.Matches(mi))
                    return null;
                // Regex.Compile is folded to a dead null factory
                // (TryEmitRegexCompileFold — its const-folded dynamic-code guard is
                // false, so RegexOptions.Compiled degrades to the interpreter, the
                // IL2CPP/NativeAOT posture). Not a reachability edge; cutting it plus
                // the backstop over the compiled-regex classes themselves — which
                // CoreIntrinsics.LoweredRuntimePrimitive now names, so the same predicate
                // cuts them here and fails loudly at the emit route — keeps RegexCompiler
                // and the whole System.Reflection.Emit closure out of the tree. Cut and
                // route share the row (name + IsStatic), so they cannot disagree.
                if (CoreIntrinsics.MdRegexCompileFold.Matches(mi))
                    return null;
                // The System.Environment OVERLOADS lowered to a dn2cpp_env_* /
                // _process_path / _environment_* helper are not a reachability edge: their
                // real bodies are the Kernel32/registry P/Invoke branch (reached
                // intra-corelib from AppContextConfigHelper <- SafeFileHandle..cctor on the
                // real file-I/O path), the Interop.Sys QCall for the host's cached startup
                // path, the _Exit InternalCall plus CLR shutdown plumbing, and the
                // fail-fast InternalCall — the last of these an overload FAMILY, since the
                // public pair forwards to the internal three-argument form, that to a
                // StackCrawlMark forwarder, and that to the InternalCall no native binary
                // can call (reached intra-corelib from ExecutionContext.OnValuesChanged).
                //
                // The SAME predicate answers the emit route — a private list here is how
                // a member gets cut with no MethodDefinition route to lower it.
                //
                // The row's TypeGate is deliberate, and it is not a second list: invoking
                // the predicate unconditionally would allocate a sig-thunk closure per
                // MethodDef call in the innermost scan loop, so the gate short-circuits
                // every non-Environment callee out before it. The predicate re-tests the
                // type name itself; a duplicated type name cannot drift, a duplicated
                // MEMBER SET is what can.
                if (CoreIntrinsics.MdEnvMember.Matches(mi))
                    return null;
                // SerializationInfo.ThrowIfDeserializationInProgress is a no-op (no
                // BinaryFormatter deserialization ever runs in a dn2cpp binary; see the
                // MethodDefinition intercept in MethodCompiler.TranslateCall). Its real
                // body pulls AsyncLocal<bool> -> ExecutionContext.GetLocalValue ->
                // Thread internals, so it must NOT be a reachability edge.
                if (CoreIntrinsics.MdDeserializationGuard.Matches(mi))
                    return null;
                // ExecutionContext.Capture() is lowered to the null "nothing to flow"
                // encoding (see the MethodDef intercept in MethodCompiler.TranslateCall);
                // its real body reads Thread._executionContext on the intrinsic Thread
                // model (no transpiled struct), so it must NOT be a reachability edge.
                // Reached from the thread-pool work-item schedulers
                // (ThreadPoolValueTaskSource.QueueToThreadPool) on the file-backed
                // async path.
                if (CoreIntrinsics.MdExecutionContextCapture.Matches(mi))
                    return null;
                if (CoreIntrinsics.MdExecutionContextRun.Matches(mi))
                    return null;
                // SynchronizationContext.get_Current / SetSynchronizationContext are
                // the same shape — lowered to the per-thread runtime slot
                // (dn2cpp_sync_ctx_get/set; see the intercepts in
                // MethodCompiler.TranslateCall); the real bodies read/write
                // Thread._synchronizationContext on the intrinsic Thread model.
                // get_Current is reached from ManualResetValueTaskSourceCore.
                // OnCompleted's scheduling-context capture arm via the
                // IValueTaskSource bridge.
                if (CoreIntrinsics.MdSyncContextSlot.Matches(mi))
                    return null;
                // The non-generic MemoryMarshal.GetArrayDataReference(Array) reached
                // intra-CoreLib: its real body is RuntimeHelpers.GetMethodTable BaseSize
                // pointer math, and GetMethodTable is a member of the intrinsic type
                // RuntimeHelpers with no mapping — so an uncut edge is not bloat, it is
                // a loud transpile failure at the first CoreLib body that steps on it
                // (Marshal.UnsafeAddrOfPinnedArrayElement). The emit route
                // (MethodCompiler.TranslateCall) asks THIS SAME row.
                if (CoreIntrinsics.MdMemoryMarshalArrayData.Matches(mi))
                    return null;
                // System.Enum.InternalGetCorElementType() (the 0-arg instance form): the emit
                // route reads the CorElementType from the boxed enum's type-info header
                // (dn2cpp_enum_cor_element_type — MethodCompiler.EmitIntrinsic.Reflection), so
                // its real body — MethodTable pointer math over the InternalCalls
                // GetMethodTable / GetPrimitiveCorElementType — must NOT be a reachability
                // edge. Reached from Enum.GetTypeCode / Enum.CompareTo / the ToString rare-type
                // path, so the leaf is cut here for every caller. The name gate short-circuits
                // before the signature decode (0-arg discriminates it from the static
                // InternalGetCorElementType(RuntimeType), which keeps its body).
                if (mi.Name == "InternalGetCorElementType" && mi.DeclaringClass.FullName == "System.Enum"
                    && mi.Signature.ParameterTypes.Length == 0)
                    return null;
                return mi;
            }
            case HandleKind.MethodSpecification:
            {
                // A generic method on an intrinsic-mapped type (e.g. Unsafe.Add<T>)
                // is emitted inline, not transpiled, so it is not a reachability
                // edge. The spec's method is a MemberRef when the call
                // crosses assemblies and a MethodDef when it stays within one
                // (e.g. corelib's Span<T> -> Unsafe.Add<T>).
                var ms = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                // AsyncTaskMethodBuilder.Start<TSM> / .AwaitUnsafeOnCompleted are
                // emitted inline. Both drive TSM.MoveNext (Start synchronously,
                // AwaitUnsafeOnCompleted by registering it as a continuation), so
                // their reachability edge is that MoveNext; nothing else.
                // AsyncIteratorMethodBuilder names the synchronous driver MoveNext<TSM>
                // rather than Start<TSM> — same shape, same edge.
                if (IsAsyncBuilderSpec(module, ms, ctx))
                {
                    var margs = ms.DecodeSignature(SigProvider, ctx).ToArray();
                    return MethodSpecMethodName(module, ms) switch
                    {
                        "Start" or "MoveNext" when margs.Length > 0 => StateMachineMoveNext(margs[0]),
                        "AwaitUnsafeOnCompleted" when margs.Length > 0 => StateMachineMoveNext(margs[^1]),
                        _ => null,
                    };
                }
                // Each row below is the SAME row the emit route asks
                // (MethodCompiler.TranslateCall's MethodSpecification arm), referenced here
                // at THIS chain's own position — the descriptor registry is a shared truth
                // source, not a shared walk order (see NameKeyedIntercept). The method name
                // is read once: it is a pure reader.GetString, no decode.
                if (MethodSpecParentTypeName(module, ms) is { } pn)
                {
                    // MethodSpecMethodName always answers (it degrades to "" for a handle
                    // kind that names no method), so it is read into a local rather than
                    // pattern-matched: `is { }` on a non-nullable string would read as a
                    // null case that exists.
                    string msName = MethodSpecMethodName(module, ms);
                    if (CoreIntrinsics.IsIntrinsicType(pn)
                        // MemoryExtensions.Contains/IndexOf/SequenceEqual are emitted inline
                        // as a scalar loop; their real SIMD bodies pull in
                        // Unsafe.BitCast / IsBitwiseEquatable, so they must NOT be a
                        // reachability edge.
                        || CoreIntrinsics.MsMemoryExtScan.Matches(pn, msName)
                        // ContainsAny[Except] are emitted inline only in their
                        // SearchValues<T> 2-arg shape (a runtime set scan), so only that
                        // overload is cut here; the [Last]IndexOfAnyExcept forms are all
                        // inline scalar scans (row above) — their real SIMD bodies
                        // miscompute, they must NOT be a reachability edge either. The row
                        // owns the name set; the shape test is the one shared call to
                        // IsMemoryExtSearchValuesForm, which the emit route makes too (it
                        // needs this asker's own generic context, so it cannot be a row).
                        || (CoreIntrinsics.MsMemoryExtSearchValues.Matches(pn, msName)
                            && IsMemoryExtSearchValuesForm(module, ms, ctx))
                        // Enum reflection-lite: GetValues/GetNames/GetName/Parse/
                        // TryParse/IsDefined are lowered inline from the (name, value)
                        // table; their real bodies route through the reflection metadata
                        // stack (ArrayPool/EventSource/Calli), so they are NOT an edge.
                        || CoreIntrinsics.MsEnumStatics.Matches(pn, msName)
                        // StringBuilder.AppendInterpolatedStringHandler.AppendFormatted<T>
                        // (and its AppendFormattedWithTempSpace<T> helper) is lowered inline
                        // — its real body reaches Enum.TryFormatUnconstrained ->
                        // the RuntimeType/EnumInfo/Number-format cascade, so it must NOT be a
                        // reachability edge. Cutting this edge is the core of the cascade
                        // collapse.
                        || CoreIntrinsics.MsAppendInterpolatedFormatted.Matches(pn, msName)
                        // Activator.CreateInstance<T>() is lowered inline to `new T()`;
                        // its real body reflects, so it is not an edge — the
                        // scan reaches T's ctor + allocated type instead (see below).
                        || CoreIntrinsics.MsActivatorCreateInstance.Matches(pn, msName)
                        // GC.AllocateUninitializedArray<T> is lowered inline to new T[length];
                        // its real worker reflects on typeof(T).TypeHandle (an InternalCall), so
                        // it is NOT a reachability edge.
                        || CoreIntrinsics.MsGcAllocateUninitializedArray.Matches(pn, msName)
                        // VectorMath.Min/Max<...> is lowered inline to a zero vector (its
                        // ISimdVector.LessThan body is the SIMD carve-out); the dead vector
                        // fast path is never a reachability edge.
                        //
                        // NOT a descriptor row, and deliberately: this cut's route is not the
                        // MethodSpecification arm at all — MethodCompiler.EmitManagedCall folds
                        // the call to a zero vector, so there is no row for the emit chain to
                        // reference and a row here would name an arm that never runs. Asymmetry
                        // in this direction (cut + route, but through a different mouth) is
                        // sound; what must never happen is a cut with NO route.
                        || (pn == "System.Runtime.Intrinsics.VectorMath"
                            && msName is "Min" or "Max")
                        // Marshal.{SizeOf,PtrToStructure,StructureToPtr,OffsetOf}<T>
                        // are lowered inline to sizeof / a value copy /
                        // offsetof; their real bodies reflect / P-Invoke into the marshaller,
                        // so they are NOT a reachability edge.
                        // Marshal.GetFunctionPointerForDelegate<T>/GetDelegateForFunctionPointer<T>
                        // lower to the generated per-delegate-type thunk-pool helpers; their
                        // real bodies are QCalls into the native marshaller, so they are NOT
                        // a reachability edge either.
                        || CoreIntrinsics.MsMarshalGenerics.Matches(pn, msName)
                        // INumberBase<T>.CreateTruncating/CreateChecked/CreateSaturating<TOther>
                        // on a concrete integer primitive: lowered inline to a
                        // numeric cast. The real bodies route through INumberBase TryConvert*
                        // (an InternalCall), so they are NOT a reachability edge. The 32/64-bit
                        // targets already cut via IsIntrinsicType; this covers the sub-word
                        // primitive targets (Byte/SByte/Int16/UInt16) that aren't intrinsic types.
                        || CoreIntrinsics.MsCreateConversion.Matches(pn, msName)
                        // Int128/UInt128.CreateTruncating<TOther> — transpiled structs, not
                        // primitives, so the row above misses them; their real body branches to
                        // TOther.TryConvertToTruncating (an InternalCall), so it is NOT an edge.
                        // Lowered inline to the sign/zero-extending widening.
                        || CoreIntrinsics.MsInt128CreateConversion.Matches(pn, msName))
                        return null;
                }
                // A generic method on a base-image type the patch converter never
                // loads (e.g. Counter.Echo<int>): like any external member it is not
                // a reachability edge — the base image carries the instantiation (a
                // hotupdate-refs.txt method root) and --emit-patch binds it as a
                // MethodSpecification import. Mirror the non-generic external-call
                // cut (TryResolveMemberRefMethod): an unloaded TypeRef declaring type
                // → null. In the normal pipeline the type resolves, so this is a
                // no-op and ResolveMethodSpec runs unchanged.
                if (ms.Method.Kind == HandleKind.MemberReference)
                {
                    var mm = module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method);
                    if (mm.Parent.Kind == HandleKind.TypeReference
                        && ResolveTypeRef(module, (TypeReferenceHandle)mm.Parent)?.Class is null)
                        return null;
                }
                return ResolveMethodSpec(module, (MethodSpecificationHandle)handle, ctx);
            }
            case HandleKind.MemberReference:
            {
                var mr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
                string mrName = module.Reader.GetString(mr.Name);
                if (mr.Parent.Kind == HandleKind.TypeSpecification)
                {
                    var parent = module.Reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(SigProvider, ctx);
                    if (parent.Kind == TypeKind.SZArray || parent.Kind == TypeKind.MDArray)
                        return null; // array intrinsics
                    // A member on a closed generic instantiation of a base-image
                    // type (hot-update patch converter): the type is monomorphized
                    // in the base image, not transpiled here — like any external
                    // member, it is not a reachability edge.
                    if (parent.Kind == TypeKind.ExternalGeneric)
                        return null;
                    // A member on a generic Task-family async type (Task<T>,
                    // AsyncTaskMethodBuilder<T>, TaskAwaiter<T>) is emitted inline,
                    // not transpiled.
                    if (parent is { Kind: TypeKind.Class, Class.IntrinsicCppName: not null })
                        return null;
                    // Span<T>/ReadOnlySpan<T> instance bulk methods (Clear/Fill/CopyTo/
                    // ToArray) are emitted inline as element loops; their real
                    // SpanHelpers/Buffer.Memmove bodies must NOT be a reachability edge.
                    if (parent is { Kind: TypeKind.Class, Class: { } sc }
                        && GenericDefFullName(sc) is "System.Span" or "System.ReadOnlySpan"
                        && mrName is "Clear" or "Fill" or "CopyTo" or "TryCopyTo" or "ToArray")
                        return null;
                    return ResolveMemberRefMethod(module, (MemberReferenceHandle)handle, ctx);
                }
                // Every cut below probes the same MemberReference's parent type
                // name; read it once (a pure metadata read).
                string? mrParent = MemberRefParentTypeName(module, (MemberReferenceHandle)handle);
                // Some cuts below need the OVERLOAD, not just the name — an intercept that
                // lowers one overload of a method must not cut the edge to the others.
                // Decode the signature at most once per token, and only once a cut whose
                // parent-name test already matched actually asks: a decode is not free (it
                // mints the closed generics the signature names; see AGENTS.md).
                MethodSignature<TypeDesc>? mrSig = null;
                MethodSignature<TypeDesc> Sig() => mrSig ??= mr.DecodeMethodSignature(SigProvider, GenericContext.Empty);
                // A member on an intrinsic-mapped nested type whose bare full name isn't in
                // s_intrinsicTypes — notably StringBuilder.AppendInterpolatedStringHandler
                // (its ctor / AppendLiteral / non-generic AppendFormatted overloads). The
                // handler is modeled as the underlying StringBuilder pointer and every member
                // is lowered inline, so its IL is never transpiled (same shape
                // as the Lock.Scope cut). Resolve the TypeRef parent and check IntrinsicCppName.
                if (mr.Parent.Kind == HandleKind.TypeReference
                    && ResolveTypeRef(module, (TypeReferenceHandle)mr.Parent)?.Class is { IntrinsicCppName: not null })
                    return null;
                // SafeBuffer.AcquirePointer/ReleasePointer/get_ByteLength and
                // SafeHandle.DangerousGetHandle are deliberately NOT cut, even though the
                // memory-mapped view handle lowers them inline. The emit intercept
                // (MethodCompiler.TranslateCall) fires only when the RECEIVER is statically
                // the intrinsic Dn2CppMappedSafeHandle; every other receiver — a
                // SafeFileHandle, an app's own SafeHandle subclass wrapping a native
                // resource — emits a call to the real transpiled body. Reachability cannot
                // see the receiver, so a cut keyed on the declaring type and member name
                // would delete that body for the whole program
                // while the call site still emits the call: not a transpile error, but
                // `use of undeclared identifier` in the C++ build. So there is NO cut here
                // (same shape as System.IO.TextWriter.Write/WriteLine below), and
                // the real bodies (refcount + the raw handle field) transpile fine.
                // Non-generic Enum reflection statics taking a runtime Type
                // (GetNames/GetName/IsDefined/Parse/TryParse/GetUnderlyingType/GetValues) —
                // the per-enum runtime table; the generic Enum.*<T> forms are cut in the
                // MethodSpecification case above. ONE predicate, shared with the emit route.
                if (CoreIntrinsics.MrEnumStatics.Matches(mrParent, mrName, Sig))
                    return null;
                // Nullable.GetUnderlyingType(Type): the real reflection body is not an edge.
                if (CoreIntrinsics.MrNullableGetUnderlyingType.Matches(mrParent, mrName, Sig))
                    return null;
                // SynchronizationContext.get_Current / SetSynchronizationContext — the
                // per-thread runtime slot; instance members (Post/Send) transpile.
                if (CoreIntrinsics.MrSyncContextSlot.Matches(mrParent, mrName, Sig))
                    return null;
                // ExecutionContext capture is represented by null across all pool hops.
                if (CoreIntrinsics.MrExecutionContextCapture.Matches(mrParent, mrName, Sig))
                    return null;
                if (CoreIntrinsics.MrExecutionContextRun.Matches(mrParent, mrName, Sig))
                    return null;
                // NativeMemory's C-heap wrappers / memset / memmove — InternalCall /
                // P-Invoke into the unmanaged allocator, never an edge.
                if (CoreIntrinsics.MrNativeMemory.Matches(mrParent, mrName, Sig))
                    return null;
                // The non-generic MemoryMarshal.GetArrayDataReference(Array) overload only
                // (GenericParameterCount == 0); the generic form is a MethodSpecification.
                if (CoreIntrinsics.MrMemoryMarshalArrayData.Matches(mrParent, mrName, Sig))
                    return null;
                // The Marshal surface (heap alloc/free/copy, SizeOf / PtrToStructure /
                // StructureToPtr / OffsetOf, the cached last-error slot, the CoTaskMem
                // reallocators, the PtrToString / StringTo / ZeroFree decoders/encoders,
                // the typed Read*/Write* accessors). Real bodies reflect
                // / P-Invoke / FCall. ONE predicate, shared with the emit route: there is
                // no second copy of the name set to drift.
                if (CoreIntrinsics.MrMarshalInterop.Matches(mrParent, mrName, Sig))
                    return null;
                // The System.GC CUT family (shape-gated: SuppressFinalize / ReRegisterForFinalize
                // / GetTotalMemory / GetTotalAllocatedBytes take one arg, Collect /
                // WaitForPendingFinalizers / GetAllocatedBytesForCurrentThread none). The
                // predicate is shared with the emit route, so the two cannot disagree about
                // which overloads are lowered. KeepAlive is EMIT-ONLY (route-without-cut), so
                // the resolver deliberately does NOT reference MrGcKeepAlive — its real body
                // must stay in the tree. SpanHelpers.Memmove (-> std::memmove) is cut just below.
                if (CoreIntrinsics.MrGc.Matches(mrParent, mrName, Sig))
                    return null;
                // Object.Finalize(): the compiler-generated base call in a C# destructor's
                // finally block; the MemberRef form (own-code destructors compiled against a
                // BCL that is not loaded, e.g. the GDExtension pipeline) is not an edge.
                if (CoreIntrinsics.MrObjectFinalize.Matches(mrParent, mrName, Sig))
                    return null;
                // SpanHelpers.Memmove — reach-only belt to the MethodDefinition
                // RuntimePrimitive route (SpanHelpers is internal, so no cross-assembly
                // MemberRef occurs; the emitter never sees this MemberRef shape).
                if (CoreIntrinsics.MrSpanHelpersMemmove.Matches(mrParent, mrName, Sig))
                    return null;
                // Buffer.BlockCopy / ByteLength / Get/SetByte -> dn2cpp_buffer_* — their real
                // bodies reach the Array.NativeLength / GetElementSize InternalCalls, and
                // Get/SetByte's the non-generic MemoryMarshal.GetArrayDataReference(Array).
                if (CoreIntrinsics.MrBufferExtent.Matches(mrParent, mrName, Sig))
                    return null;
                // Encoding.GetString (Encoding / UTF8Encoding / UnicodeEncoding) — cutting it
                // collapses the whole String.CreateStringFromEncoding -> SIMD UTF-8 transcode
                // subtree. Name+type cut (reach) / name gate + shape-total-throw (emit). The
                // encoding-creation path (get_UTF8 / get_Unicode) is deliberately NOT cut.
                if (CoreIntrinsics.MrEncodingGetString.Matches(mrParent, mrName, Sig))
                    return null;
                // The MemoryExtensions.ToUpperInvariant/ToLowerInvariant OVERLOADS lowered
                // inline to the runtime BMP invariant fold are NOT a reachability edge — the
                // real bodies reach GlobalizationMode.get_Invariant -> LoadICU /
                // InitICUFunctions. The SAME predicate answers the emit route, which is the
                // point: a cut on the NAME with a route testing the SHAPE lets an overload
                // the route declines fall through to a body the cut already deleted
                // — an undefined symbol at C++ LINK time, not a transpile error.
                if (CoreIntrinsics.MrSpanCaseFold.Matches(mrParent, mrName, Sig))
                    return null;
                // The System.IO.Path / System.IO.File OVERLOADS the call site lowers inline to
                // a dn2cpp_path_* / dn2cpp_file_* helper are not a reachability edge — the
                // emitter never calls their real bodies, which pull in the file-I/O +
                // globalization cascade (the biggest self-host gap cut, and what keeps the
                // transpiler's own build off it).
                //
                // The SAME predicate answers the emit route (MethodCompiler.TranslateCall), and
                // that is the whole point: the two cannot disagree about which overloads are
                // lowered. Cutting an edge the emitter does NOT lower is not a transpile error
                // — it is a call emitted against a body nothing put in the tree, i.e. an
                // undefined symbol at C++ LINK time, discovered long after the transpile said
                // it succeeded. Every overload the predicate declines (the ReadOnlySpan<char>
                // Path ops, params Combine, the Encoding File overloads) IS an edge and
                // transpiles from real BCL IL.
                if (CoreIntrinsics.MrIoMember.Matches(mrParent, mrName, Sig))
                    return null;
                // The System.Environment OVERLOADS lowered to a dn2cpp_env_* / _process_path /
                // _environment_* helper are not a reachability edge: their real bodies are the
                // Kernel32/registry P/Invoke branch, the _Exit InternalCall plus CLR shutdown
                // plumbing, and the CLR fail-fast InternalCall.
                //
                // The SAME predicate answers the emit route. A cut on the NAME against a
                // SIGNATURE-matched emit table drops an overload like
                // GetEnvironmentVariable(string, EnvironmentVariableTarget) in the gap —
                // body deleted, shape declined, the call lands on EmitIntrinsic's throw
                // with the fallback already gone — and disagrees with the MethodDefinition
                // arm below, which gates by shape: the intra-CoreLib call transpiles, the
                // identical cross-assembly one does not.
                if (CoreIntrinsics.MrEnvMember.Matches(mrParent, mrName, Sig))
                    return null;
                // AppContext.BaseDirectory -> the running executable's directory; its real
                // getter falls back to Assembly.GetEntryAssembly()?.Location, unanswerable
                // in a native binary. Only this member; the switch/data accessors transpile.
                if (CoreIntrinsics.MrAppContextBaseDir.Matches(mrParent, mrName, Sig))
                    return null;
                // Directory.Exists / GetCurrentDirectory / SetCurrentDirectory. (CreateDirectory
                // is deliberately NOT cut — callers deref the returned DirectoryInfo, so its
                // real body transpiles, matching the intra-CoreLib MethodDef path.)
                if (CoreIntrinsics.MrDirectory.Matches(mrParent, mrName, Sig))
                    return null;
                // Byte/SByte/Int16/UInt16 ToString/Parse/TryParse/TryFormat are lowered inline
                // to dn2cpp_int_to_string / dn2cpp_format_int|uint / dn2cpp_try_format_int|uint_c
                // and the width-parameterized NumberStyles engine (mirroring Int32/Int64, which
                // are intrinsic types). The sub-word primitives are NOT intrinsic types, so
                // without this their real System.Number.Format*/ParseBinaryInteger bodies are a
                // reachability edge dragging in the whole numeric-formatting/parsing subtree
                // (NumberFormatInfo / Math.DivRem / IUtfChar.CastFrom / Buffer.BulkMove /
                // FastAllocateString) — the biggest remaining console-self-host cascade.
                //
                // The SAME predicate names the member set at the emit route, and at the
                // constrained static-virtual dispatch and its reach twin. Unlike Path/File
                // these deliberately do NOT fall through — the fall-through IS the cascade the
                // cut exists to avoid — so the emit table covers the whole .NET 10 surface and
                // an overload beyond it fails loudly, naming the shape.
                if (CoreIntrinsics.MrInlinePrimitive.Matches(mrParent, mrName, Sig))
                    return null;
                // System.Collections.Comparer.Compare — body-intercepted, cut here so the real
                // IL's `x as IComparable`/CompareInfo/throw subtree is not a reachability edge; the
                // emit route lowers it to dn2cpp_object_compare. Same predicate as the MethodDef arm
                // and MethodCompiler.TranslateCall.
                if (CoreIntrinsics.MrComparerCompare.Matches(mrParent, mrName, Sig))
                    return null;
                // The WIDE integer primitives' TryFormat (span-write formatter). They are
                // intrinsic types, so their members are already cut; this is the belt to that
                // braces, and it exists because SRM's SignatureDecoder.CheckHeader reaches
                // Byte.TryFormat from dn2cpp's own Compilation.Build — the dominant remaining
                // console-self-host cascade if the real System.Number.TryFormat* body (
                // TryFormatUInt32 / Int32ToHexChars / UInt32ToDecChars→Math.DivRem /
                // NumberFormatInfo getters / IUtfChar.CastFrom / Span.TryCopyTo→Buffer.BulkMove /
                // FastAllocateString / UInt32.LeadingZeroCount) were ever an edge.
                if (CoreIntrinsics.MrWideIntTryFormat.Matches(mrParent, mrName, Sig))
                    return null;
                // Const-folded getters referenced cross-assembly (the MemberRef form
                // of the CoreIntrinsics.ConstFoldedGetter cut in the MethodDefinition
                // branch): RuntimeFeature's public dynamic-code probes arrive this
                // way from Regex/STJ under --auto-ref. The call site folds to a
                // constant, so the getter body is never needed.
                if (CoreIntrinsics.MrConstFoldedGetter.Matches(mrParent, mrName, Sig))
                    return null;
                // System.IO.TextWriter.Write/WriteLine on the Console.Error stderr writer
                // are lowered inline to the dn2cpp_textwriter_* helpers, but only when the
                // receiver is statically the Dn2CppTextWriter* Console.get_Error returns
                // (MethodCompiler's intercept is receiver-sensitive): a real writer (e.g.
                // the StreamWriter File.CreateText returns) dispatches the transpiled
                // virtual instead, since the real file-I/O bodies transpile. So the
                // MemberRef IS a reachability edge — the real TextWriter.Write/WriteLine
                // bodies must be in the tree for the virtual dispatch path. (Console is an
                // intrinsic type, so get_Error itself is still not an edge, and a
                // Console.Error receiver still never executes the real bodies.)
                // System.Exception::get_Message: cut as intrinsic (TryResolveMemberRefMethod
                // returns null for the intrinsic parent), but reach the used-virtual so a
                // base-typed `ex.Message` dispatches the derived override — the MemberRef
                // form of the MethodDef reach above. Resolve the real base MethodInfo (the
                // non-Try resolver does so even for an intrinsic parent) to cross it.
                if (mrParent == "System.Exception" && mrName == "get_Message")
                {
                    try { ReachExceptionGetMessage(ResolveMemberRefMethod(module, (MemberReferenceHandle)handle, ctx)); }
                    catch (NotSupportedException) { }
                    return null;
                }
                // Non-generic parent: reach the real method when it resolves to a
                // loaded (non-intrinsic) BCL type; otherwise treat as intrinsic.
                return TryResolveMemberRefMethod(module, (MemberReferenceHandle)handle, ctx);
            }
            default:
                return null;
        }
    }

    /// <summary>Reaches <c>System.Exception::get_Message</c> as a used-virtual when
    /// <paramref name="mi"/> is it, so the used × allocated cross product pulls every
    /// allocated exception type's get_Message override (ArgumentException's paramName
    /// append, FileNotFoundException's lazy build). A no-op for any other method.</summary>
    private void ReachExceptionGetMessage(MethodInfo mi)
    {
        if (mi.DeclaringClass.FullName == "System.Exception" && mi.Name == "get_Message")
            ReachUsedVirtual(mi);
    }

    /// <summary>Resolves (instantiating on first use) the closed generic method
    /// referenced by a MethodSpec, in the caller's generic context.</summary>
    public MethodInfo ResolveMethodSpec(Module module, MethodSpecificationHandle handle, GenericContext callerCtx)
    {
        var reader = module.Reader;
        var ms = reader.GetMethodSpecification(handle);
        var methodArgs = ms.DecodeSignature(SigProvider, callerCtx).ToArray();

        MethodDefinitionHandle templateHandle;
        ClassInfo declClass;
        Module defModule;
        if (ms.Method.Kind == HandleKind.MethodDefinition)
        {
            // A generic method on a non-generic type: the template MethodDef and
            // its declaring class live in the calling module.
            templateHandle = (MethodDefinitionHandle)ms.Method;
            defModule = module;
            var declType = reader.GetMethodDefinition(templateHandle).GetDeclaringType();
            if (!module.ClassMap.TryGetValue(declType, out declClass!))
                throw new NotSupportedException("Generic methods on generic types are not supported yet");
        }
        else if (ms.Method.Kind == HandleKind.MemberReference)
        {
            // A generic method referenced via a MemberRef. The parent is either a
            // closed generic type (TypeSpec — e.g. Container<int>.Map<U>) or a
            // non-generic type (TypeRef — e.g. another assembly's Lib.Echo<T>).
            // Resolve the declaring class, then instantiate the open method template
            // in a context binding both the class type args (empty for a non-generic
            // type) and the method type args.
            var mr = reader.GetMemberReference((MemberReferenceHandle)ms.Method);
            declClass = mr.Parent.Kind switch
            {
                HandleKind.TypeSpecification =>
                    reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(SigProvider, callerCtx).Class
                    ?? throw new NotSupportedException("Generic method's declaring type did not resolve to a class"),
                HandleKind.TypeReference =>
                    ResolveTypeRef(module, (TypeReferenceHandle)mr.Parent)?.Class
                    ?? throw new NotSupportedException(
                        $"Generic method's declaring type {MemberRefParentTypeName(module, (MemberReferenceHandle)ms.Method)} did not resolve to a loaded class"),
                _ => throw new NotSupportedException(
                    $"Generic method via MemberRef with a {mr.Parent.Kind} parent is not supported yet"),
            };
            if (declClass.GenericArity > 0)
                EnsureCompleted(declClass);
            defModule = declClass.Module;
            string mname = reader.GetString(mr.Name);
            // Decode the wanted parameter signature with an empty context so method/
            // type generic parameters stay as GenVar placeholders — the same form the
            // candidate templates decode to below, giving an apples-to-apples key that
            // disambiguates overloads differing only by a delegate parameter's arity
            // (e.g. Select(Func<T,R>) vs Select(Func<T,int,R>)).
            var wantParams = mr.DecodeMethodSignature(SigProvider, GenericContext.Empty).ParameterTypes;
            int paramCount = wantParams.Length;
            string wantKey = string.Join(",", wantParams.Select(p => p.ToString()));
            templateHandle = FindGenericMethodTemplate(defModule, declClass.Handle, mname, methodArgs.Length, paramCount, wantKey)
                ?? throw new NotSupportedException(
                    $"{declClass.FullName}: no generic method {mname}<{methodArgs.Length}>/{paramCount}");
        }
        else
        {
            throw new NotSupportedException(
                $"Generic method instantiation via {ms.Method.Kind} is not supported yet");
        }

        return InstantiateMethodOnClass(declClass, defModule, templateHandle, methodArgs);
    }

    /// <summary>Instantiates a generic-method template (<paramref name="templateHandle"/>,
    /// defined in <paramref name="defModule"/>) on the closed declaring class
    /// <paramref name="declClass"/> at <paramref name="methodArgs"/>, caching by the
    /// (class, template, args) triple. Shared by <see cref="ResolveMethodSpec"/> (the IL
    /// MethodSpec path) and the generic-virtual override resolver (which instantiates a
    /// subtype's override at the same method args without an IL token).</summary>
    private MethodInfo InstantiateMethodOnClass(
        ClassInfo declClass, Module defModule, MethodDefinitionHandle templateHandle, TypeDesc[] methodArgs)
    {
        var md = defModule.Reader.GetMethodDefinition(templateHandle);

        // Key on the closed declaring class plus the method template and its args,
        // so Container<int>.Map<int> and Container<string>.Map<int> stay distinct.
        var byKey = MethodInstancesFor(defModule, templateHandle);
        var mkey = (declClass.CppName, methodArgs);
        if (byKey.TryGetValue(mkey, out var existing))
            return existing;

        // The method dimension nests independently of the class dimension —
        // `M<T>() => M<List<T>>()` deepens its method arguments forever on a
        // non-generic class — so it feeds the same depth measure and the same bound.
        int depth = 1 + MaxArgDepth(methodArgs);
        if (depth > MaxGenericArgDepth)
            MaxGenericArgDepth = depth;
        CheckInstantiationBound(depth, declClass.FullName + "::"
            + defModule.Reader.GetString(md.Name)
            + "<" + string.Join(", ", methodArgs.Select(a => a.ToString())) + ">");

        var ctx = new GenericContext(declClass.Context.TypeArgs, methodArgs);
        ModelCensus.MethodsGenericInst++;
        // No decoded signature here either (MethodInfo.Signature): nothing reads it until
        // after the initializer has closed, at which point mi.Context IS ctx — so the
        // deferred decode needs no hand-fed local context. Callers that need the decode to
        // happen (and to fail) inside their own try still get it: they ask, with
        // EnsureSignature.
        var mi = new MethodInfo
        {
            DeclaringClass = declClass,
            Name = defModule.Reader.GetString(md.Name),
            Handle = templateHandle,
            Module = defModule,
            Attributes = md.Attributes,
            ImplAttributes = md.ImplAttributes,
            Rva = md.RelativeVirtualAddress,
            Context = ctx,
            PInvoke = ReadPInvoke(defModule.Reader, md),
            NameSuffix = "__" + string.Join("_", methodArgs.Select(MangleArg)),
        };
        byKey.Add(mkey, mi);
        _methodInstanceCount++;
        _methodInstanceOrder.Add(mi);
        declClass.Methods.Add(mi); // emitted (and scanned) alongside the declaring class
        ApplyPreservationToInstantiatedMethod(mi);
        return mi;
    }

    /// <summary>Resolves a <c>constrained. call</c> to a static-abstract interface
    /// member (.NET 7+ static virtuals) to the concrete static body on the constrained
    /// value type <paramref name="scc"/>. Two shapes:
    /// <list type="bullet">
    /// <item>Non-generic member (e.g. an <c>IMinMaxCalc&lt;T&gt;</c> comparer's
    /// <c>Compare</c>): the closed callee's name+params+return resolves the body
    /// directly.</item>
    /// <item>Generic member (e.g.
    /// <c>IndexOfAnyAsciiSearcher.IResultMapper&lt;T,int&gt;.FirstIndex&lt;TVector&gt;</c>):
    /// the body is a generic method on the (now closed) value type, which dn2cpp does not
    /// add to <see cref="ClassInfo.Methods"/> up front, so a <see cref="MethodInfo.SigKey"/>
    /// scan finds nothing. Resolve its open template like a generic-virtual override
    /// (<c>ReachUsedGvm</c>): match by name + generic/parameter arity + the interface
    /// template's open parameter signature, then instantiate at the callee's method
    /// args.</item>
    /// </list>
    /// Returns null when the value type provides no such body (the caller then either
    /// falls through to the member's own default body — a `static virtual` with an
    /// implementation — or surfaces the existing precise diagnostic). Shared by the
    /// emit side (<c>MethodCompiler.EmitManagedCall</c>) and reachability
    /// (<see cref="ReachStaticVirtualImpl"/>) so both pick the identical instantiation.</summary>
    internal MethodInfo? ResolveStaticVirtualImpl(ClassInfo scc, MethodInfo callee)
    {
        // An intrinsic-mapped TSelf (decimal, …) keeps its explicit implementations
        // invisible here: their IL bodies are cut, and their dotted metadata names
        // ("System.Numerics.INumberBase<System.Decimal>.get_Zero") appear in no
        // per-type intrinsic table. Leaving them unresolved routes the call to the
        // member's default interface body / the generic-math table, which lower these
        // members from the interface side. Implicitly-named members (op_Addition, …)
        // still resolve through the SigKey scan below and hit the per-type tables.
        bool sccIntrinsic = CoreIntrinsics.IsIntrinsicType(scc.FullName) || scc.IntrinsicCppName is not null;

        // Likewise, a member carrying a default interface body stays on that default
        // for an intrinsic-mapped TSelf even when the type shadows it with a plainly-
        // named specialization (Decimal.CreateChecked<TOther>): the shadow's IL body
        // is cut like every intrinsic-type body, while the default forwards to
        // abstract members the tables do lower — identical results.
        if (sccIntrinsic && callee.Rva != 0)
            return null;

        // Non-generic static-abstract member: name+params+return resolves it directly.
        if (callee.Context.MethodArgs.Length == 0)
        {
            // An explicit static interface implementation ("static int IFoo<Bar>.M(…)")
            // carries the dotted metadata name, invisible to the SigKey scan below; its
            // .override row lands in ExplicitInterfaceImpls (PopulateMethodImpls), so
            // probe that map first — the static analogue of ResolveItfImplOrNull.
            // (e.g. Half implements IBinaryFloatParseAndFormatInfo<Half> explicitly;
            // Number.FormatFloat / TryParseFloat dispatch every parse/format
            // parameter through it.)
            for (var c = scc; !sccIntrinsic && c is not null; c = c.BaseClass)
            {
                if (c.ExplicitInterfaceImpls.TryGetValue(callee, out var direct) && direct.IsStatic)
                    return direct;
                foreach (var kv in c.ExplicitInterfaceImpls)
                    if (kv.Value.IsStatic
                        && kv.Key.DeclaringClass.FullName == callee.DeclaringClass.FullName
                        && kv.Key.Name == callee.Name
                        && kv.Key.SigKey == callee.SigKey)
                        return kv.Value;
            }
            for (var c = scc; c is not null; c = c.BaseClass)
            {
                var m = c.Methods.FirstOrDefault(x => x.IsStatic && x.Rva != 0
                    && x.Name == callee.Name && x.SigKey == callee.SigKey);
                if (m is not null)
                    return m;
            }
            return null;
        }

        // Generic static-abstract member. The interface template and the struct's open
        // template both decode (in the empty context) to the same `!0&,…,!!0` key — the
        // struct closes the interface's element type with its own first type parameter —
        // so the interface template's open parameter signature finds the struct body.
        // The interface's simple name rides along so an explicit implementation's dotted
        // metadata name ("Ns.IItf<T>.Name") matches too, like the interface-GVM lookup
        // (suppressed for an intrinsic-mapped TSelf, per the note above).
        var openParams = callee.Module.Reader.GetMethodDefinition(callee.Handle)
            .DecodeSignature(SigProvider, GenericContext.Empty).ParameterTypes;
        string wantKey = string.Join(",", openParams.Select(p => p.ToString()));
        string? itfSimple = null;
        if (!sccIntrinsic)
        {
            itfSimple = callee.DeclaringClass.Module.Reader.GetString(
                callee.DeclaringClass.Module.Reader.GetTypeDefinition(callee.DeclaringClass.Handle).Name);
            int tick = itfSimple.IndexOf('`');
            if (tick >= 0)
                itfSimple = itfSimple[..tick];
        }
        for (var c = scc; c is not null; c = c.BaseClass)
        {
            if (FindGenericMethodTemplate(c.Module, c.Handle, callee.Name,
                    callee.Context.MethodArgs.Length, openParams.Length, wantKey, itfSimple) is { } tmpl)
                return InstantiateMethodOnClass(c, c.Module, tmpl, callee.Context.MethodArgs);
        }
        return null;
    }

    /// <summary>Finds the open generic-method template (MethodDef) on a type
    /// definition by name, method generic-parameter count and parameter count —
    /// for instantiating a generic method on a closed generic type. When
    /// several overloads collide on (name, genArity, paramCount), <paramref
    /// name="wantKey"/> (the wanted parameter signature, decoded with an empty
    /// context) breaks the tie so overloads differing only by a delegate
    /// parameter's arity — e.g. Select(Func&lt;T,R&gt;) vs Select(Func&lt;T,int,R&gt;)
    /// — bind to the right one instead of the first defined.</summary>
    private MethodDefinitionHandle? FindGenericMethodTemplate(
        Module mod, TypeDefinitionHandle classDef, string name, int genArity, int paramCount,
        string? wantKey = null, string? explicitItfName = null)
    {
        // Name-indexed: a full walk of the declaring TypeDef's method rows per call
        // would be ~1k rows for Enumerable, and the GVM reachers call this once
        // per allocated type per used GVM. The lazy per-TypeDef index narrows the
        // walk to the same-name rows; candidate order is metadata row order — the
        // index's per-name lists keep it, and the explicit-impl merge below restores
        // it across the two lists — so `first` and the wantKey tie-break pick exactly
        // the row the full walk picked, and the same candidates decode in the same
        // order.
        var reader = mod.Reader;
        var idx = TypeDefMethodNames(mod, classDef);
        idx.ByName.TryGetValue(name, out var exact);
        List<MethodDefinitionHandle>? candidates;
        if (explicitItfName is null)
        {
            candidates = exact;
        }
        else
        {
            // With explicitItfName set (interface-GVM implementation lookup), an
            // explicit interface implementation matches too: its metadata name is
            // the dotted "Ns.IItf<T>.Name" form, so require the ".Name" suffix and
            // the interface's simple name in the qualifier. (mname != name keeps a
            // dotted `name` from matching twice — its exact hit is already in
            // `exact`.)
            List<MethodDefinitionHandle>? dotted = null;
            foreach (var (mname, mh) in idx.Dotted)
                if (mname != name
                    && mname.EndsWith("." + name, StringComparison.Ordinal)
                    && mname.Contains(explicitItfName, StringComparison.Ordinal))
                    (dotted ??= new()).Add(mh);
            candidates = MergeByRow(exact, dotted);
        }
        if (candidates is null)
            return null;
        MethodDefinitionHandle? first = null;
        foreach (var mh in candidates)
        {
            var md = reader.GetMethodDefinition(mh);
            if (md.GetGenericParameters().Count != genArity) continue;
            // Decode with an empty context: generic parameters decode to GenVar
            // placeholders (substitution is deferred), so the arity count is exact.
            var ps = md.DecodeSignature(SigProvider, GenericContext.Empty).ParameterTypes;
            if (ps.Length != paramCount) continue;
            first ??= mh;
            // Exact parameter-signature match wins when disambiguating overloads;
            // fall back to the first arity-match (representational differences).
            if (wantKey is null || string.Join(",", ps.Select(p => p.ToString())) == wantKey)
                return mh;
        }
        return first;
    }

    /// <summary>Per-TypeDef method-name index for <see cref="FindGenericMethodTemplate"/>.
    /// Built lazily on the first template lookup against a TypeDef, from one
    /// pass over its method rows reading only their metadata names — decode-free, and
    /// never stale because TypeDef metadata is immutable. Per-name lists are in metadata
    /// row order; <see cref="Dotted"/> carries the dotted names (explicit interface
    /// implementations, .ctor/.cctor) in row order for the explicit-impl suffix match.</summary>
    private sealed class TypeDefMethodNameIndexEntry
    {
        public readonly Dictionary<string, List<MethodDefinitionHandle>> ByName = new();
        public readonly List<(string Name, MethodDefinitionHandle Handle)> Dotted = new();
    }

    private readonly Dictionary<(int Module, int Token), TypeDefMethodNameIndexEntry> _typeDefMethodNames = new();

    private TypeDefMethodNameIndexEntry TypeDefMethodNames(Module mod, TypeDefinitionHandle classDef)
    {
        var key = (mod.Index, SRME.GetToken(classDef));
        if (_typeDefMethodNames.TryGetValue(key, out var idx))
            return idx;
        idx = new TypeDefMethodNameIndexEntry();
        var reader = mod.Reader;
        foreach (var mh in reader.GetTypeDefinition(classDef).GetMethods())
        {
            string mname = reader.GetString(reader.GetMethodDefinition(mh).Name);
            if (!idx.ByName.TryGetValue(mname, out var list))
                idx.ByName[mname] = list = new List<MethodDefinitionHandle>();
            list.Add(mh);
            if (mname.IndexOf('.') >= 0)
                idx.Dotted.Add((mname, mh));
        }
        _typeDefMethodNames.Add(key, idx);
        return idx;
    }

    /// <summary>Merges two row-ascending handle lists into metadata row order — how
    /// <see cref="FindGenericMethodTemplate"/> restores the full-walk visit order across
    /// the exact-name hits and the explicit-impl (dotted-name) hits. Either side may be
    /// null (absent); the inputs are never mutated (the exact list is the shared index's).</summary>
    private static List<MethodDefinitionHandle>? MergeByRow(
        List<MethodDefinitionHandle>? exact, List<MethodDefinitionHandle>? dotted)
    {
        if (dotted is null)
            return exact;
        if (exact is null)
            return dotted;
        var merged = new List<MethodDefinitionHandle>(exact.Count + dotted.Count);
        int i = 0, j = 0;
        while (i < exact.Count && j < dotted.Count)
        {
            if (SRME.GetRowNumber(exact[i]) < SRME.GetRowNumber(dotted[j]))
                merged.Add(exact[i++]);
            else
                merged.Add(dotted[j++]);
        }
        while (i < exact.Count)
            merged.Add(exact[i++]);
        while (j < dotted.Count)
            merged.Add(dotted[j++]);
        return merged;
    }

    /// <summary>Resolves a field reference whose parent is either a closed
    /// generic instance (MemberRef with a TypeSpec parent) or a type in another
    /// assembly (TypeRef parent — e.g. a value-type field like Vector2.X reached
    /// through a referenced shim assembly).</summary>
    public (ClassInfo, FieldInfo) ResolveMemberRefField(Module module, MemberReferenceHandle handle, GenericContext callerCtx)
    {
        var reader = module.Reader;
        var mr = reader.GetMemberReference(handle);
        ClassInfo cls;
        if (mr.Parent.Kind == HandleKind.TypeSpecification)
        {
            var parent = reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(SigProvider, callerCtx);
            cls = parent.Class!;
        }
        else if (mr.Parent.Kind == HandleKind.TypeReference)
        {
            var tr = ResolveTypeRef(module, (TypeReferenceHandle)mr.Parent);
            if (tr?.Class == null)
                throw new NotSupportedException($"External type reference in field MemberRef (parent {mr.Parent.Kind} token 0x{SRME.GetToken(mr.Parent):X8}) could not be resolved");
            cls = tr.Class;
        }
        else
        {
            throw new NotSupportedException($"Unsupported field MemberRef parent kind: {mr.Parent.Kind}");
        }

        // Fields are shape, not members: the drain would have decoded them, but a token
        // resolved mid-scan can name a class whose turn in the queue has not come. Shape
        // is all this needs — decoding its methods to read a field would be gratuitous.
        if (cls.GenericArity > 0)
            CompleteShape(cls);
        string name = reader.GetString(mr.Name);
        var fld = cls.Fields.FirstOrDefault(f => f.Name == name)
            ?? throw new NotSupportedException(
                $"field '{name}' not found on {cls.FullName} "
                + $"(fields: {string.Join(", ", cls.Fields.Select(f => f.Name))})");
        return (cls, fld);
    }

    /// <summary>Resolves a method reference whose parent is a closed generic
    /// instance (MemberRef with a TypeSpec parent).</summary>
    public MethodInfo ResolveMemberRefMethod(Module module, MemberReferenceHandle handle, GenericContext callerCtx)
    {
        var reader = module.Reader;
        var mr = reader.GetMemberReference(handle);
        ClassInfo cls;
        bool typeRefParent = false;
        if (mr.Parent.Kind == HandleKind.TypeSpecification)
        {
            var parent = reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(SigProvider, callerCtx);
            cls = parent.Class!;
        }
        else if (mr.Parent.Kind == HandleKind.TypeReference)
        {
            // Memo probe before the parent even resolves: a TypeRef parent is
            // context-independent, so the token alone fixes the answer.
            if (_memberRefMethodsByTypeRef.TryGetValue((module.Index, SRME.GetToken(handle)), out var hitByRef))
                return hitByRef;
            typeRefParent = true;
            var tr = ResolveTypeRef(module, (TypeReferenceHandle)mr.Parent);
            if (tr?.Class == null)
                throw new NotSupportedException($"External type reference in MemberRef (parent {mr.Parent.Kind} token 0x{SRME.GetToken(mr.Parent):X8}) could not be resolved");
            cls = tr.Class;
        }
        else
        {
            throw new NotSupportedException($"Unsupported MemberRef parent kind: {mr.Parent.Kind}");
        }

        if (!typeRefParent
            && _memberRefMethodsBySpec.TryGetValue((module.Index, SRME.GetToken(handle), cls), out var hitBySpec))
            return hitBySpec;

        if (cls.GenericArity > 0)
            EnsureCompleted(cls); // members may be needed before the queue drains
        string name = reader.GetString(mr.Name);
        var sig = mr.DecodeMethodSignature(SigProvider, cls.Context);
        int argc = sig.ParameterTypes.Length;
        // Name-indexed: same candidates in the same order as the full Methods
        // scan, and Signature (a decode on first read) is still consulted only for
        // same-name candidates — exactly the set a full scan's short-circuit decodes.
        var named = cls.MethodsNamed(name);
        var candidates = named is null
            ? new List<MethodInfo>()
            : named.Where(x => x.Signature.ParameterTypes.Length == argc).ToList();
        if (candidates.Count == 0)
            throw new NotSupportedException($"{cls.FullName}: no method {name}/{argc}");
        // A single same-arity candidate is the common case; only when overloads
        // collide on name+arity (e.g. String.Concat(string, string) vs
        // Concat(object, object)) do we disambiguate by parameter types so the
        // exact MemberRef signature wins. Fall back to the first candidate when
        // no structural match (cross-module representational differences).
        MethodInfo resolved;
        if (candidates.Count == 1)
        {
            resolved = candidates[0];
        }
        else
        {
            static string Key(IEnumerable<TypeDesc> ps) => string.Join(",", ps.Select(p => p.ToString()));
            string want = Key(sig.ParameterTypes);
            // Conversion operators (op_Implicit/op_Explicit) overload on RETURN type
            // alone — every overload shares the same single parameter type — so a
            // params-only match binds the first-declared one (e.g. a struct-returning
            // conversion once bound the int-returning op_Implicit, then cast the int
            // to a pointer -> segfault). Prefer a candidate that matches both the
            // parameter types AND the return type; fall back to params-only, then the
            // first candidate (cross-module return-type representational differences).
            string wantRet = sig.ReturnType.ToString();
            resolved = candidates.FirstOrDefault(x => Key(x.Signature.ParameterTypes) == want
                                                      && x.Signature.ReturnType.ToString() == wantRet)
                ?? candidates.FirstOrDefault(x => Key(x.Signature.ParameterTypes) == want)
                ?? candidates[0];
        }
        if (typeRefParent)
            _memberRefMethodsByTypeRef[(module.Index, SRME.GetToken(handle))] = resolved;
        else
            _memberRefMethodsBySpec[(module.Index, SRME.GetToken(handle), cls)] = resolved;
        return resolved;
    }

    /// <summary>Best-effort resolution of a non-generic MemberRef call to a real
    /// loaded method — e.g. a BCL method whose IL lives in a CoreLib passed with
    /// -r. Returns null when the parent type is intrinsic-mapped, not loaded, or
    /// the member cannot be matched, so callers fall back to intrinsics.</summary>
    public MethodInfo? TryResolveMemberRefMethod(Module module, MemberReferenceHandle handle, GenericContext callerCtx)
    {
        var reader = module.Reader;
        var mr = reader.GetMemberReference(handle);
        if (mr.Parent.Kind != HandleKind.TypeReference)
            return null;
        var tr = reader.GetTypeReference((TypeReferenceHandle)mr.Parent);
        string ns = reader.GetString(tr.Namespace);
        string nm = reader.GetString(tr.Name);
        string full = string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
        if (CoreIntrinsics.IsIntrinsicType(full))
            return null;
        if (ResolveTypeRef(module, (TypeReferenceHandle)mr.Parent)?.Class is not { } parentCls)
            return null;
        // An intrinsic-mapped type whose bare full name isn't in s_intrinsicTypes —
        // notably a nested intrinsic value type like Lock.Scope (resolved via the
        // enclosing type). Its members route through the intrinsic table, not its IL.
        if (parentCls.IntrinsicCppName is not null)
            return null;
        try
        {
            return ResolveMemberRefMethod(module, handle, callerCtx);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>The declaring-type full name of a call/newobj target, resolving the
    /// MemberRef / MethodSpec / MethodDef forms; null if unresolved.</summary>
    private string? CallTargetTypeName(Module module, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MemberReference => MemberRefParentTypeName(module, (MemberReferenceHandle)handle),
        HandleKind.MethodSpecification =>
            MethodSpecParentTypeName(module, module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle)),
        HandleKind.MethodDefinition => MethodDefParentTypeName(module, (MethodDefinitionHandle)handle),
        _ => null,
    };

    /// <summary>The called method's own name for a Call/Callvirt token, across the same handle
    /// kinds as <see cref="CallTargetTypeName"/> — a raw metadata-name read, resolving nothing.</summary>
    private string? CallTargetMethodName(Module module, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MemberReference =>
            module.Reader.GetString(module.Reader.GetMemberReference((MemberReferenceHandle)handle).Name),
        HandleKind.MethodSpecification =>
            MethodSpecMethodName(module, module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle)),
        HandleKind.MethodDefinition =>
            module.Reader.GetString(module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
        _ => null,
    };

    /// <summary>The folded compile-time constant for a call token targeting a
    /// const-folded getter (see <see cref="CoreIntrinsics.ConstFoldedGetter"/>),
    /// or null for any other call. Works on raw metadata names only — it must
    /// not resolve or reach the target — and name-checks before touching the
    /// declaring type so the per-call-site probe stays cheap.</summary>
    internal bool? ConstFoldedCallTarget(Module module, int token)
    {
        if (token == 0 || (uint)token >> 24 == 0x70)
            return null;
        var handle = SRME.EntityHandle(token);
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var h = (MethodDefinitionHandle)handle;
                string name = module.Reader.GetString(module.Reader.GetMethodDefinition(h).Name);
                if (!CoreIntrinsics.IsConstFoldedGetterName(name))
                    return null;
                return MethodDefParentTypeName(module, h) is { } declType
                    ? CoreIntrinsics.ConstFoldedGetter(declType, name)
                    : null;
            }
            case HandleKind.MemberReference:
            {
                var h = (MemberReferenceHandle)handle;
                string name = module.Reader.GetString(module.Reader.GetMemberReference(h).Name);
                if (!CoreIntrinsics.IsConstFoldedGetterName(name))
                    return null;
                return MemberRefParentTypeName(module, h) is { } declType
                    ? CoreIntrinsics.ConstFoldedGetter(declType, name)
                    : null;
            }
            default:
                return null;
        }
    }

    /// <summary>Classifies a call token for the typeof-equality fold's identity window
    /// (<c>typeof(A) ==/!= typeof(B)</c> feeding a branch — see
    /// <see cref="BranchLiveness"/>). Raw metadata-name reads only, name gate
    /// first, so probing a call site costs one string read; both the MemberRef
    /// arm (the common cross-assembly case) and the MethodDef arm (the loaded
    /// CoreLib's own bodies name Type's operators with MethodDef tokens) are
    /// covered, like <see cref="ConstFoldedCallTarget"/>.</summary>
    internal TypeIdentityCall ClassifyTypeIdentityCall(Module module, int token)
    {
        if (token == 0 || (uint)token >> 24 == 0x70)
            return TypeIdentityCall.None;
        var handle = SRME.EntityHandle(token);
        string? name;
        string? declType;
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var h = (MethodDefinitionHandle)handle;
                name = module.Reader.GetString(module.Reader.GetMethodDefinition(h).Name);
                if (name is not ("GetTypeFromHandle" or "op_Equality" or "op_Inequality"))
                    return TypeIdentityCall.None;
                declType = MethodDefParentTypeName(module, h);
                break;
            }
            case HandleKind.MemberReference:
            {
                var h = (MemberReferenceHandle)handle;
                name = module.Reader.GetString(module.Reader.GetMemberReference(h).Name);
                if (name is not ("GetTypeFromHandle" or "op_Equality" or "op_Inequality"))
                    return TypeIdentityCall.None;
                declType = MemberRefParentTypeName(module, h);
                break;
            }
            default:
                return TypeIdentityCall.None;
        }
        if (declType != "System.Type")
            return TypeIdentityCall.None;
        return name switch
        {
            "GetTypeFromHandle" => TypeIdentityCall.GetTypeFromHandle,
            "op_Equality" => TypeIdentityCall.OpEquality,
            _ => TypeIdentityCall.OpInequality,
        };
    }

    /// <summary>The compile-time verdict for <c>typeof(A) == typeof(B)</c>
    /// over two ldtoken type tokens, or null when it must stay a runtime
    /// <c>dn2cpp_type_equals</c>. THE one shared predicate for the fold: both the
    /// reachability scanner and the emitter feed it (via the identical
    /// <see cref="BranchLiveness.ComputeCached"/> callbacks — memoized, so the
    /// scan's fill is the verdict the compiles read), which is what keeps the
    /// cut and the route in lockstep — never give one side a private variant.
    ///
    /// Null (no fold) whenever a side is open, unresolved, External, or carries a
    /// canonical placeholder (a shared body must not bake a per-instantiation
    /// answer — the poisoned-typeof taint then forces per-instantiation bodies,
    /// where the tokens are concrete and the verdict returns). Wrong sharing is
    /// worse than no fold. Decidable tokens compare by a kind-prefixed key of
    /// their own (<see cref="TypeIdentityKey"/>) — deliberately NOT
    /// <see cref="MangleArg"/> itself, whose future changes this fold must not
    /// silently track — with corlib's primitive TypeDefs/TypeRefs normalized
    /// through <see cref="WellKnownPrimitive"/> so <c>typeof(bool)</c> (a
    /// TypeRef resolving to the Boolean class) and a substituted
    /// <c>typeof(T)</c> (the interned Boolean primitive) agree.
    /// Disabled wholesale in the hot-update world (see <see cref="_hotUpdatePosture"/>).</summary>
    internal bool? TypeEqualityVerdict(Module module, int tokenA, int tokenB, GenericContext ctx)
    {
        if (_hotUpdatePosture)
            return null;
        if (FoldableTypeIdentityKey(module, tokenA, ctx) is not { } a
            || FoldableTypeIdentityKey(module, tokenB, ctx) is not { } b)
            return null;
        return a == b;
    }

    /// <summary>The identity key of one ldtoken operand for the typeof-equality fold, or
    /// null when the type is not statically decidable (which makes the whole
    /// comparison stay dynamic).</summary>
    private string? FoldableTypeIdentityKey(Module module, int token, GenericContext ctx)
    {
        if (token == 0 || (uint)token >> 24 == 0x70)
            return null;
        var handle = SRME.EntityHandle(token);
        if (handle.Kind is not (HandleKind.TypeDefinition or HandleKind.TypeReference
            or HandleKind.TypeSpecification))
            return null;
        TypeDesc? t;
        try
        {
            t = ResolveTypeTokenForScan(module, handle, ctx);
        }
        catch (Exception ex) when (!IsMustEscape(ex)
                                   && ex is NotSupportedException or InvalidOperationException)
        {
            return null;
        }
        return t is null ? null : TypeIdentityKey(t);
    }

    /// <summary>The canonical identity key of a closed type for the typeof-equality fold,
    /// or null for anything the fold must not decide. Only discriminating
    /// kinds participate: a primitive, a closed placeholder-free class (with
    /// corlib's own primitive-backing TypeDefs normalized to their primitive),
    /// and an SZ array over those. Everything else — External (not loaded),
    /// generic variables, templates, byref/pointer, MD arrays — answers null:
    /// their identity is not the transpiler's to assert.
    ///
    /// <para>The key is a private internal string, never emitted, and each
    /// kind carries its own prefix so the spaces cannot cross: a
    /// global-namespace user class named <c>Int32</c> has CppName "Int32",
    /// and an unprefixed key would equate it with the int primitive — folding
    /// <c>typeof(global::Int32) == typeof(int)</c> to TRUE while the runtime
    /// (type-info pointer identity) answers false, a silent wrong-arm prune.
    /// The SZArray suffix rides on the element's own prefixed key, so
    /// <c>p:Int32[]</c> and <c>c:Int32[]</c> stay distinct too.</para></summary>
    private static string? TypeIdentityKey(TypeDesc t)
    {
        if (t.IsCanonPlaceholder)
            return null;
        switch (t.Kind)
        {
            case TypeKind.Primitive:
                return "p:" + t.Primitive;
            case TypeKind.Class:
            {
                var c = t.Class!;
                if (WellKnownPrimitive(c.FullName) is { } prim)
                    return "p:" + prim.Primitive;
                if (ContainsCanonPlaceholder(c))
                    return null;
                // An unspecialized generic (template-shaped ClassInfo) has no
                // closed identity to compare.
                if (c.GenericArity != 0 && c.Context.TypeArgs.Length != c.GenericArity)
                    return null;
                return "c:" + c.CppName;
            }
            case TypeKind.SZArray:
                return TypeIdentityKey(t.Element!) is { } elem ? elem + "[]" : null;
            default:
                return null;
        }
    }

    /// <summary>Whether a call token names a constructor — a pure name compare off the
    /// metadata, decoding nothing.</summary>
    private static bool IsCtorToken(Module module, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MemberReference =>
            module.Reader.GetString(module.Reader.GetMemberReference((MemberReferenceHandle)handle).Name) == ".ctor",
        HandleKind.MethodDefinition =>
            module.Reader.GetString(module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name) == ".ctor",
        _ => false,
    };

    /// <summary>Full name of a MemberRef's parent type when it is a plain
    /// TypeReference (the common cross-assembly BCL case); null otherwise.</summary>
    public string? MemberRefParentTypeName(Module module, MemberReferenceHandle handle)
    {
        var mr = module.Reader.GetMemberReference(handle);
        if (mr.Parent.Kind != HandleKind.TypeReference)
            return null;
        var tr = module.Reader.GetTypeReference((TypeReferenceHandle)mr.Parent);
        string ns = module.Reader.GetString(tr.Namespace);
        string nm = module.Reader.GetString(tr.Name);
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    /// <summary>The CLR built-in value types whose boxed form
    /// <c>dn2cpp_object_tostring</c> formats directly in the runtime. Their managed
    /// ToString pulls culture/Calli, so a boxed-format site must never reach it —
    /// the runtime already handles them.</summary>
    private static readonly HashSet<string> s_runtimeFormattedPrimitives = new()
    {
        "System.Boolean", "System.Byte", "System.SByte", "System.Int16", "System.UInt16",
        "System.Int32", "System.UInt32", "System.Int64", "System.UInt64", "System.IntPtr",
        "System.UIntPtr", "System.Single", "System.Double", "System.Char", "System.Decimal",
        "System.TimeSpan", "System.DateTime", "System.DateTimeOffset",
    };

    private static bool IsRuntimeFormattedPrimitive(ClassInfo c) => s_runtimeFormattedPrimitives.Contains(c.FullName);

    /// <summary>Whether a call target is an Object-receiver formatting method
    /// (Console.Write/WriteLine, String.Concat/Format) — the sites that format a
    /// boxed struct via its ToString.</summary>
    private bool IsObjectFormattingCall(Module module, EntityHandle handle)
    {
        if (handle.Kind != HandleKind.MemberReference)
            return false;
        var mr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
        string name = module.Reader.GetString(mr.Name);
        return MemberRefParentTypeName(module, (MemberReferenceHandle)handle) switch
        {
            "System.Console" => name is "Write" or "WriteLine",
            "System.String" => name is "Concat" or "Format",
            _ => false,
        };
    }

    /// <summary>Full name of the type that declares a MethodDef (the
    /// within-assembly case, e.g. a corelib method calling another corelib
    /// method).</summary>
    public string? MethodDefParentTypeName(Module module, MethodDefinitionHandle handle)
    {
        var md = module.Reader.GetMethodDefinition(handle);
        var td = module.Reader.GetTypeDefinition(md.GetDeclaringType());
        string ns = module.Reader.GetString(td.Namespace);
        string nm = module.Reader.GetString(td.Name);
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    /// <summary>The arity-stripped full name of a class's open generic
    /// definition (e.g. an <c>AsyncTaskMethodBuilder&lt;int&gt;</c>
    /// specialization -> "System.Runtime.CompilerServices.AsyncTaskMethodBuilder");
    /// the plain <see cref="ClassInfo.FullName"/> for a non-generic class.</summary>
    public string GenericDefFullName(ClassInfo cls)
    {
        if (cls.GenericArity == 0)
            return cls.FullName;
        if (cls.GenericDefFullNameCache is { } cached)
            return cached;
        var td = cls.Module.Reader.GetTypeDefinition(cls.Handle);
        string ns = cls.Module.Reader.GetString(td.Namespace);
        string nm = cls.Module.Reader.GetString(td.Name);
        int tick = nm.IndexOf('`');
        if (tick >= 0)
            nm = nm.Substring(0, tick);
        return cls.GenericDefFullNameCache = string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    /// <summary>The closed specialization a MethodSpec's (or MemberRef's) parent
    /// TypeSpec decodes to, when that parent is a Task-family async type modeled
    /// by a runtime struct; null otherwise. Used to route both reachability
    /// and codegen of generic async members (Start, AwaitUnsafeOnCompleted) to
    /// intrinsics instead of transpiling the TPL.</summary>
    public ClassInfo? TaskFamilyMethodSpecParent(Module module, MethodSpecification ms, GenericContext ctx)
    {
        if (ms.Method.Kind != HandleKind.MemberReference)
            return null;
        var mr = module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method);
        if (mr.Parent.Kind != HandleKind.TypeSpecification)
            return null;
        var parent = module.Reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(SigProvider, ctx);
        return parent is { Kind: TypeKind.Class, Class.IntrinsicCppName: not null } ? parent.Class : null;
    }

    /// <summary>The method name a MethodSpec instantiates (whether its method is
    /// a MemberRef or a MethodDef).</summary>
    public string MethodSpecMethodName(Module module, MethodSpecification ms) => ms.Method.Kind switch
    {
        HandleKind.MemberReference =>
            module.Reader.GetString(module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method).Name),
        HandleKind.MethodDefinition =>
            module.Reader.GetString(module.Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).Name),
        _ => "",
    };

    /// <summary>Whether a MethodSpec targets a generic method on
    /// AsyncTaskMethodBuilder — i.e. Start / AwaitUnsafeOnCompleted — in either
    /// its generic (<c>AsyncTaskMethodBuilder&lt;T&gt;</c>, TypeSpec parent) or
    /// non-generic (<c>async Task</c>, TypeRef parent) form. These are emitted
    /// inline. AsyncIteratorMethodBuilder (the `async IAsyncEnumerable&lt;T&gt;`
    /// driver) is always non-generic, and names its synchronous driver
    /// MoveNext&lt;TSM&gt; where the others name it Start&lt;TSM&gt;.</summary>
    public bool IsAsyncBuilderSpec(Module module, MethodSpecification ms, GenericContext ctx) =>
        TaskFamilyMethodSpecParent(module, ms, ctx) is not null
        || MethodSpecParentTypeName(module, ms)
            is "System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
            or "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder"
            or "System.Runtime.CompilerServices.PoolingAsyncValueTaskMethodBuilder"
            or "System.Runtime.CompilerServices.AsyncIteratorMethodBuilder"
            or "System.Runtime.CompilerServices.AsyncVoidMethodBuilder"
        // An adopted third-party builder in its NON-generic form (`async GDTask`):
        // the spec's parent is a TypeRef/MethodDef, which the TypeSpec-only probe above
        // does not see. The generic form (`async GDTask<T>`) already qualifies there, its
        // parent TypeSpec being IntrinsicCppName-mapped. Only an adopted BUILDER counts:
        // admitting any adopted type would swallow a generic method on the task type
        // itself — the shape Task.FromResult<T> / WhenAll<T> / TaskFactory.StartNew<T>
        // take — into the async lowering, which has no case for it.
        || (HasAdoptedAsync && MethodSpecParentClass(module, ms) is { } pc
            && AdoptedAsyncKey(pc)
                is "System.Runtime.CompilerServices.AsyncTaskMethodBuilder"
                or "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder");

    /// <summary>The class owning the generic method a MethodSpec instantiates, for the
    /// TypeRef (cross-assembly) and MethodDef (within-assembly) parent forms — the two
    /// shapes <see cref="TaskFamilyMethodSpecParent"/> (TypeSpec parents only) cannot
    /// see. Null for a TypeSpec parent, or when the parent is not a loaded type.
    /// <para>The MethodDef arm resolves the DECLARING TYPE, not the method: a MethodSpec
    /// always instantiates a generic method, and Pass 2 files those under
    /// <see cref="Module.GenericMethodTemplates"/> rather than <see cref="Module.MethodMap"/>
    /// — so a MethodMap lookup here silently returns null for every within-assembly
    /// spec.</para></summary>
    private ClassInfo? MethodSpecParentClass(Module module, MethodSpecification ms) => ms.Method.Kind switch
    {
        HandleKind.MemberReference =>
            module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method).Parent is
                { Kind: HandleKind.TypeReference } p
                ? ResolveTypeRef(module, (TypeReferenceHandle)p)?.Class : null,
        HandleKind.MethodDefinition =>
            module.ClassMap.TryGetValue(
                module.Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).GetDeclaringType(),
                out var cls) ? cls : null,
        _ => null,
    };

    /// <summary>The state-machine MoveNext method that AsyncTaskMethodBuilder
    /// .Start&lt;TStateMachine&gt; drives. Start runs the machine synchronously, so
    /// this call is its only reachability/codegen effect.</summary>
    public MethodInfo? StateMachineMoveNext(TypeDesc stateMachineArg)
    {
        if (stateMachineArg is not { Kind: TypeKind.Class, Class: { } sm })
            return null;
        // A generic state machine arrives as a shell; complete it. A non-generic
        // one is already populated by the main model passes — completing it again
        // would duplicate its fields/methods.
        if (sm.GenericArity > 0)
            EnsureCompleted(sm);
        return sm.Methods.FirstOrDefault(m => m.Name == "MoveNext" && m.Signature.ParameterTypes.Length == 0);
    }

    /// <summary>The continuation-registering method of a <em>custom</em> (non-Task)
    /// awaiter that genuinely suspends — <c>UnsafeOnCompleted</c>
    /// (ICriticalNotifyCompletion) for the unsafe-await path, else <c>OnCompleted</c>
    /// (INotifyCompletion): a 1-arg instance method taking a <c>System.Action</c>.
    /// The async lowering registers a synthesized Action-over-MoveNext continuation
    /// through it (<see cref="ReachCustomAwaiterContinuation"/> + the emit). Null for
    /// an intrinsic Task/Yield/ValueTask awaiter (those take the dedicated resume
    /// paths) or when the awaiter exposes no such method.</summary>
    public MethodInfo? CustomAwaiterOnCompleted(TypeDesc awaiterType, bool preferUnsafe)
    {
        if (awaiterType is not { Kind: TypeKind.Class, Class: { } awaiter }
            || awaiter.IntrinsicCppName is not null) // Task/Yield/ValueTask awaiter
            return null;
        if (awaiter.GenericArity > 0)
            EnsureCompleted(awaiter);
        // A class/struct awaiter registers through its own body. An *interface*
        // awaiter (a GetAwaiter whose declared return type is an awaiter
        // interface deriving from INotifyCompletion) exposes only the abstract
        // declaration — on itself or an inherited interface — and the emit
        // dispatches it through the interface table, so the body requirement is
        // dropped and the interface closure is walked.
        MethodInfo? FindOn(ClassInfo cls, string n, bool needBody) => cls.Methods.FirstOrDefault(m =>
            !m.IsStatic && m.Name == n && (!needBody || m.Rva != 0)
            && m.Signature.ParameterTypes is [{ Kind: TypeKind.Class, Class.IsDelegate: true }]);
        MethodInfo? Find(string n)
        {
            if (!awaiter.IsInterface)
                return FindOn(awaiter, n, needBody: true);
            var seen = new HashSet<ClassInfo>();
            var queue = new Queue<ClassInfo>();
            queue.Enqueue(awaiter);
            while (queue.Count > 0)
            {
                var itf = queue.Dequeue();
                if (!seen.Add(itf))
                    continue;
                if (itf.GenericArity > 0)
                    EnsureCompleted(itf);
                if (FindOn(itf, n, needBody: false) is { } decl)
                    return decl;
                foreach (var inherited in itf.Interfaces)
                    queue.Enqueue(inherited);
            }
            return null;
        }
        string primary = preferUnsafe ? "UnsafeOnCompleted" : "OnCompleted";
        string secondary = preferUnsafe ? "OnCompleted" : "UnsafeOnCompleted";
        return Find(primary) ?? Find(secondary);
    }

    /// <summary>For an <c>AwaitOnCompleted</c>/<c>AwaitUnsafeOnCompleted</c> over a
    /// custom (non-Task) awaiter, the emit synthesizes a <c>System.Action</c> wrapping
    /// the boxed state machine's MoveNext and calls the awaiter's
    /// OnCompleted/UnsafeOnCompleted. Neither edge is visible to
    /// <see cref="ResolveCallTarget"/> (the spec resolves only to MoveNext, and the
    /// Action is heap-built by the emit, never by IL <c>newobj</c>), so reach them
    /// here: the awaiter's continuation method, and System.Action as an allocated
    /// delegate type so its struct/type-info/invoker are emitted.</summary>
    private void ReachCustomAwaiterContinuation(Module module, MethodSpecificationHandle msh, GenericContext ctx)
    {
        var ms = module.Reader.GetMethodSpecification(msh);
        if (!IsAsyncBuilderSpec(module, ms, ctx))
            return;
        string? nm = MethodSpecMethodName(module, ms);
        if (nm is not ("AwaitOnCompleted" or "AwaitUnsafeOnCompleted"))
            return;
        var margs = ms.DecodeSignature(SigProvider, ctx).ToArray();
        if (margs.Length == 0
            || CustomAwaiterOnCompleted(margs[0], preferUnsafe: nm == "AwaitUnsafeOnCompleted") is not { } onCompleted)
            return;
        // An interface-declared registration method (interface-typed awaiter) is
        // dispatched through the interface table: mark the slot used so every
        // allocated implementing type wires + reaches its body.
        if (onCompleted.DeclaringClass.IsInterface)
            ReachUsedVirtual(onCompleted);
        else
            Reach(onCompleted);
        if (onCompleted.Signature.ParameterTypes is [{ Kind: TypeKind.Class, Class: { IsDelegate: true } action }])
        {
            if (action.GenericArity > 0)
                EnsureCompleted(action);
            if (action.Methods.FirstOrDefault(mm => mm.Name == "Invoke") is { } inv)
                Reach(inv);
            ReachAllocatedType(action);
        }
    }

    /// <summary>For a call to <c>ThreadPool.UnsafeQueueUserWorkItem(IThreadPoolWorkItem,
    /// bool)</c> — a MethodDef intra-corelib (ThreadPoolValueTaskSource.QueueToThreadPool)
    /// or a MemberRef from another assembly — mark the interface's <c>Execute</c> slot
    /// used so every allocated work-item type wires + reaches its Execute body, and note
    /// the interface type so its type-info symbol exists. The emit lowers the call to
    /// <c>dn2cpp_threadpool_queue_workitem</c>, which dispatches Execute through the
    /// interface table at run time — invisible to the ordinary callvirt reach.</summary>
    private void ReachThreadPoolWorkItemExecute(Module module, EntityHandle handle, GenericContext ctx)
    {
        MethodSignature<TypeDesc> sig;
        if (handle.Kind == HandleKind.MethodDefinition
            && module.MethodMap.TryGetValue((MethodDefinitionHandle)handle, out var mi))
        {
            if (mi.DeclaringClass.FullName != "System.Threading.ThreadPool"
                || mi.Name != "UnsafeQueueUserWorkItem")
                return;
            sig = mi.Signature;
        }
        else if (handle.Kind == HandleKind.MemberReference)
        {
            if (MemberRefParentTypeName(module, (MemberReferenceHandle)handle) != "System.Threading.ThreadPool")
                return;
            var mr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
            if (module.Reader.GetString(mr.Name) != "UnsafeQueueUserWorkItem")
                return;
            sig = mr.DecodeMethodSignature(SigProvider, ctx);
        }
        else
            return;
        if (sig.ParameterTypes is not [{ Kind: TypeKind.Class, Class: { IsInterface: true } itf },
                                       { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }])
            return;
        if (itf.Methods.FirstOrDefault(mm =>
                mm.Name == "Execute" && !mm.IsStatic && mm.Signature.ParameterTypes.Length == 0)
            is not { VtableSlot: >= 0 } exec)
            return;
        ReachUsedVirtual(exec);
        NoteReferencedType(itf);
    }

    /// <summary>The <c>GetResult(short)</c> / <c>OnCompleted(Action&lt;object&gt;,
    /// object, short, flags)</c> pair of a (closed) <c>IValueTaskSource(&lt;T&gt;)</c>
    /// interface, or null when the shape doesn't match. Shared by the
    /// IValueTaskSource-backed <c>new ValueTask(source, token)</c> bridge's reach
    /// (<see cref="ReachValueTaskSourceBridge"/>) and emit sides.</summary>
    internal (MethodInfo GetResult, MethodInfo OnCompleted)? ValueTaskSourceMembers(ClassInfo itf)
    {
        if (itf.GenericArity > 0)
            EnsureCompleted(itf);
        var getResult = itf.Methods.FirstOrDefault(m =>
            m.Name == "GetResult" && !m.IsStatic && m.Signature.ParameterTypes.Length == 1);
        var onCompleted = itf.Methods.FirstOrDefault(m =>
            m.Name == "OnCompleted" && !m.IsStatic && m.Signature.ParameterTypes.Length == 4
            && m.Signature.ParameterTypes[0] is { Kind: TypeKind.Class, Class.IsDelegate: true });
        if (getResult is null || onCompleted is null
            || getResult.VtableSlot < 0 || onCompleted.VtableSlot < 0)
            return null;
        return (getResult, onCompleted);
    }

    /// <summary>For an IValueTaskSource-backed <c>new ValueTask(&lt;T&gt;)(source,
    /// token)</c> — a TypeSpec-parented MemberRef (the generic form) or a MethodDef
    /// on the non-generic ValueTask — the emit bridges the source onto a pending
    /// runtime task whose completion path dispatches the interface's GetResult /
    /// OnCompleted through the source's interface table and invokes a runtime-built
    /// Action&lt;object&gt;-shaped continuation delegate. None of those edges is an
    /// IL call site, so reach them here: the two interface slots (so every allocated
    /// source type wires + reaches its impls) and the Action&lt;object&gt; delegate
    /// type as allocated (the runtime stamps its type-info on the bridge delegate,
    /// and the source's own continuation-invoke IL calls through it).</summary>
    private void ReachValueTaskSourceBridge(Module module, EntityHandle handle, GenericContext ctx)
    {
        MethodSignature<TypeDesc> sig;
        if (handle.Kind == HandleKind.MemberReference)
        {
            var mr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
            if (mr.Parent.Kind == HandleKind.TypeSpecification)
            {
                if (module.Reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(SigProvider, ctx)
                        is not { Kind: TypeKind.Class, Class: { } vcls }
                    || GenericDefFullName(vcls) != "System.Threading.Tasks.ValueTask")
                    return;
                sig = mr.DecodeMethodSignature(SigProvider, vcls.Context);
            }
            // The NON-generic ValueTask's source-backed ctor written by an app assembly
            // is a TypeRef-parented MemberRef, which the TypeSpec probe above does not
            // see (it only matches the generic ValueTask<T>). Every `async
            // IAsyncEnumerable<T>` iterator emits one, in its DisposeAsync.
            else if (mr.Parent.Kind == HandleKind.TypeReference
                     && MemberRefParentTypeName(module, (MemberReferenceHandle)handle)
                        == "System.Threading.Tasks.ValueTask")
                sig = mr.DecodeMethodSignature(SigProvider, ctx);
            else
                return;
        }
        else if (handle.Kind == HandleKind.MethodDefinition
                 && module.MethodMap.TryGetValue((MethodDefinitionHandle)handle, out var mi)
                 && mi.DeclaringClass.FullName == "System.Threading.Tasks.ValueTask"
                 && mi.Name == ".ctor")
            sig = mi.Signature;
        else
            return;
        if (sig.ParameterTypes is not [{ Kind: TypeKind.Class, Class: { IsInterface: true } itf },
                                       { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int16 }])
            return;
        if (ValueTaskSourceMembers(itf) is not { } members)
            return;
        ReachUsedVirtual(members.GetResult);
        ReachUsedVirtual(members.OnCompleted);
        NoteReferencedType(itf);
        if (members.OnCompleted.Signature.ParameterTypes[0] is { Kind: TypeKind.Class, Class: { } action })
        {
            if (action.GenericArity > 0)
                EnsureCompleted(action);
            if (action.Methods.FirstOrDefault(mm => mm.Name == "Invoke") is { } inv)
                Reach(inv);
            ReachAllocatedType(action);
        }
    }

    /// <summary>The body a <c>constrained.&lt;cls&gt; callvirt callee</c> on a value type
    /// devirtualizes to, or null when the type provides none.
    ///
    /// <para><b>THE resolution</b> — the emit route (MethodCompiler.EmitConstrainedCall)
    /// and the reachability cut (ReachConstrainedImpl) both ask this and nothing else, so
    /// <c>cut ⟹ route</c> holds by construction. A second copy that drifted would either
    /// dangle a mangled name at C++ link time or, worse, call a different body than the one
    /// reachability transpiled.</para>
    ///
    /// <para>The typed <c>IEquatable&lt;T&gt;::Equals(!0)</c> /
    /// <c>IComparable&lt;T&gt;::CompareTo(!0)</c> slots are answered first: the erased
    /// <c>!0</c> makes the signature walk match the struct's <c>Equals(object)</c> override
    /// instead of the typed overload the interface dispatch must reach (SRM's
    /// Symbolic.BitVector has both). The non-generic <c>System.IComparable</c> (no type
    /// args, boxed argument) keeps the signature path.</para></summary>
    internal MethodInfo? ConstrainedImplOf(ClassInfo cls, MethodInfo callee)
    {
        EnsureCompleted(cls);
        if (callee.DeclaringClass is { IsInterface: true } itf
            && itf.Context.TypeArgs.Length == 1
            && callee.Signature.ParameterTypes.Length == 1)
        {
            switch (GenericDefFullName(itf))
            {
                case "System.IEquatable" when callee.Name == "Equals"
                    && EffectiveTypedEquals(cls) is { } teq:
                    return teq;
                case "System.IComparable" when callee.Name == "CompareTo"
                    && EffectiveTypedCompareTo(cls) is { } tct:
                    return tct;
            }
        }
        if (DeclaredImplOf(cls, callee) is { } direct)
            return direct;
        if (!callee.DeclaringClass.IsInterface)
            return null;
        // The callee names a DIFFERENT instantiation of an interface the type implements —
        // legal through variance (`struct S : I<object>` invoked as `I<string>::M` for a
        // contravariant `I<in T>`), and the exact-signature probe above cannot see it. Re-ask
        // against the slot the type really declares. Matching name + parameter COUNT instead
        // would pick whichever same-named overload metadata happened to order first.
        MethodInfo? found = null;
        foreach (var slot in VariantInterfaceSlots(cls, callee))
        {
            if (DeclaredImplOf(cls, slot) is not { } impl || ReferenceEquals(impl, found))
                continue;
            if (found is not null)
                throw new NotSupportedException(
                    $"constrained callvirt: {cls.FullName} implements several instantiations of "
                    + $"{callee.DeclaringClass.FullName} whose {callee.Name} slots bind to different "
                    + "bodies — the variance-compatible one cannot be told apart here");
            found = impl;
        }
        return found;
    }

    /// <summary>The implementation <paramref name="cls"/> or one of its bases declares for
    /// the EXACT slot <paramref name="target"/>: the .override row first (an explicit
    /// implementation's dotted metadata name is invisible to the signature scan), then a
    /// full name+signature match — <see cref="MethodInfo.SigKey"/> covers the parameter
    /// types and the return type, and one MethodDef row is one generic arity. Per level, so
    /// a derived level's match outranks a base's.</summary>
    private MethodInfo? DeclaredImplOf(ClassInfo cls, MethodInfo target)
    {
        for (var c = cls; c is not null; c = c.BaseClass)
        {
            EnsureCompleted(c);
            if (c.ExplicitInterfaceImpls.TryGetValue(target, out var em))
                return em;
            if (c.Methods.FirstOrDefault(x => !x.IsStatic && x.Rva != 0
                    && x.Name == target.Name && x.SigKey == target.SigKey) is { } m)
                return m;
        }
        return null;
    }

    /// <summary>The slots of the interfaces <paramref name="cls"/> actually implements that
    /// correspond to <paramref name="callee"/> on a DIFFERENT instantiation of the same
    /// interface definition. Correspondence is the shared MethodDef row, which is exact:
    /// same name, same parameter count, same generic arity, same declaration order.
    /// The DAG walk reads shape only (<c>Interfaces</c>); members are pulled for the few
    /// nodes that are instantiations of the callee's own definition, never for the rest.
    /// </summary>
    private List<MethodInfo> VariantInterfaceSlots(ClassInfo cls, MethodInfo callee)
    {
        var slots = new List<MethodInfo>();
        var def = callee.DeclaringClass;
        var seen = new HashSet<ClassInfo>();
        var pending = new Stack<ClassInfo>();
        for (var b = cls; b is not null; b = b.BaseClass)
            foreach (var i in b.Interfaces)
                pending.Push(i);
        while (pending.Count > 0)
        {
            var i = pending.Pop();
            if (!seen.Add(i))
                continue;
            foreach (var up in i.Interfaces)
                pending.Push(up);
            if (ReferenceEquals(i, def) || i.Handle != def.Handle || i.Module != def.Module)
                continue;
            EnsureCompleted(i);
            if (i.MethodByTemplate.TryGetValue(callee.Handle, out var slot))
                slots.Add(slot);
            else if (i.Methods.FirstOrDefault(m => m.Handle == callee.Handle) is { } scan)
                slots.Add(scan);
        }
        return slots;
    }

    /// <summary>Full name of the type owning the (generic) method a MethodSpec
    /// instantiates, resolving whether the spec's method is a MemberRef
    /// (cross-assembly) or a MethodDef (within-assembly).</summary>
    public string? MethodSpecParentTypeName(Module module, MethodSpecification ms) => ms.Method.Kind switch
    {
        HandleKind.MemberReference => MemberRefParentTypeName(module, (MemberReferenceHandle)ms.Method),
        HandleKind.MethodDefinition => MethodDefParentTypeName(module, (MethodDefinitionHandle)ms.Method),
        _ => null,
    };

    public MethodInfo? ResolveMethodHandle(Module module, EntityHandle handle, GenericContext ctx, ClassInfo? currentClass = null)
    {
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
                var h = (MethodDefinitionHandle)handle;
                if (currentClass != null && currentClass.GenericArity > 0 && currentClass.MethodByTemplate.TryGetValue(h, out var inst))
                    return inst;
                return module.MethodMap.TryGetValue(h, out var mi) ? mi : null;
            case HandleKind.MethodSpecification:
                return ResolveMethodSpec(module, (MethodSpecificationHandle)handle, ctx);
            case HandleKind.MemberReference:
                return ResolveMemberRefMethod(module, (MemberReferenceHandle)handle, ctx);
            default:
                return null;
        }
    }
}
