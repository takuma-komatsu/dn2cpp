using System.Reflection.Metadata;
using System.Text;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    // ---- calls ----

    /// <summary>Match-and-route for one MethodDefinition-arm descriptor row
    /// (<see cref="MethodDefIntercept"/>): false when the row does not match and
    /// the chain continues; true when the row's emit arm has lowered the call.
    /// The switch is exhaustive with a throwing default, so a row minted without
    /// an arm fails the first transpile that matches it instead of falling
    /// through to EmitManagedCall and naming a body its cut deleted.</summary>
    private bool TryEmitMethodDefIntercept(in MethodDefIntercept row, MethodInfo callee)
    {
        if (!row.Matches(callee))
            return false;
        switch (row.EmitArm)
        {
            case InterceptEmitArm.RuntimePrimitive:
                EmitRuntimePrimitive(callee);
                return true;
            case InterceptEmitArm.EventSourceNoOp:
                // Cannot decline: the helper's first arm tests exactly this row's
                // predicate (the provider arm behind it is a separate route).
                if (!TryEmitEventSourceNoOp(callee))
                    throw new InvalidOperationException(
                        $"EventSourceNoOp arm declined {callee.DeclaringClass.FullName}.{callee.Name}");
                return true;
            case InterceptEmitArm.RegexCompileFold:
                if (!TryEmitRegexCompileFold(callee))
                    throw new InvalidOperationException(
                        $"RegexCompileFold arm declined {callee.DeclaringClass.FullName}.{callee.Name}");
                return true;
            case InterceptEmitArm.ConstFoldedGetter:
                if (!TryEmitConstFoldedGetter(callee))
                    throw new InvalidOperationException(
                        $"ConstFoldedGetter arm declined {callee.DeclaringClass.FullName}.{callee.Name}");
                return true;
            case InterceptEmitArm.ConstFoldedStringCall:
                if (!TryEmitConstFoldedStringCall(callee))
                    throw new InvalidOperationException(
                        $"ConstFoldedStringCall arm declined {callee.DeclaringClass.FullName}.{callee.Name}");
                return true;
            case InterceptEmitArm.DeserializationGuardNoOp:
                for (int i = callee.Signature.ParameterTypes.Length - 1; i >= 0; i--)
                    Pop();
                return true;
            case InterceptEmitArm.EnvIntrinsic:
                EmitIntrinsic("System.Environment", callee.Name, callee.Signature);
                return true;
            case InterceptEmitArm.InlinePrimitive:
                EmitIntrinsic(callee.DeclaringClass.FullName, callee.Name, callee.Signature);
                return true;
            case InterceptEmitArm.ComparerCompare:
                EmitIntrinsic(callee.DeclaringClass.FullName, callee.Name, callee.Signature);
                return true;
            case InterceptEmitArm.ExecutionContextCaptureNull:
            {
                for (int i = callee.Signature.ParameterTypes.Length - 1; i >= 0; i--)
                    Pop();
                if (callee.Signature.ReturnType is { Kind: TypeKind.Class, Class: { } ecCls })
                    _c.NoteReferencedType(ecCls);
                string ecT = CppTypes.Of(callee.Signature.ReturnType);
                Push(StackKind.Ref, ecT, $"({ecT})nullptr");
                return true;
            }
            case InterceptEmitArm.ExecutionContextRunDirect:
            {
                var state = Pop();
                var callback = Pop();
                Pop(); // ExecutionContext
                Emit($"dn2cpp_paramthread_invoke({Cast(callback, "Dn2CppObject*")}, {Cast(state, "Dn2CppObject*")});");
                return true;
            }
            case InterceptEmitArm.IntrinsicUnderDeclType:
                EmitIntrinsic(callee.DeclaringClass.FullName, callee.Name, callee.Signature);
                return true;
            case InterceptEmitArm.SyncContextSlot:
                EmitIntrinsic("System.Threading.SynchronizationContext", callee.Name, callee.Signature);
                return true;
            case InterceptEmitArm.IntrinsicDispatch:
                EmitIntrinsic(callee.DeclaringClass.FullName, callee.Name, callee.Signature);
                return true;
            default:
                throw new InvalidOperationException(
                    $"no MethodDefinition emit arm for {row.EmitArm} "
                    + $"({callee.DeclaringClass.FullName}.{callee.Name})");
        }
    }

    /// <summary>Match-and-route for one MemberReference-arm descriptor row
    /// (<see cref="MemberRefIntercept"/>): false when the row does not match and
    /// the chain continues; true when the row's emit arm has lowered the call.
    /// <paramref name="sig"/> is the caller's per-token memoized thunk (decoding
    /// under _method.Context) — the same thunk the row's predicate saw, so an arm
    /// that reads it re-uses the one decode. The switch is exhaustive with a
    /// throwing default; a row whose emit mouth is NOT this funnel (the
    /// post-resolution MethodDef-row reference behind
    /// <see cref="CoreIntrinsics.MrConstFoldedGetter"/>) deliberately has no case
    /// here, so routing such a row through this funnel fails the first transpile
    /// that tries.</summary>
    private bool TryEmitMemberRefIntercept(in MemberRefIntercept row, MemberReference mr,
        string? declType, string name, Func<MethodSignature<TypeDesc>> sig)
    {
        if (!row.Matches(declType, name, sig))
            return false;
        switch (row.EmitArm)
        {
            case InterceptEmitArm.IntrinsicUnderDeclType:
                EmitIntrinsic(declType!, name, sig());
                return true;
            case InterceptEmitArm.IntrinsicUnderDeclTypeNullCtx:
                // The Enum / Nullable reflection statics decode in the NULL
                // generic context, not the caller's memoized _method.Context thunk
                // — the exact call the pre-descriptor site made (these signatures
                // carry no GenVar, so the two contexts agree, but the decode stays
                // where it was).
                EmitIntrinsic(declType!, name, mr.DecodeMethodSignature(_c.SigProvider, null));
                return true;
            case InterceptEmitArm.ExecutionContextCaptureNull:
            {
                var callSig = sig();
                for (int i = callSig.ParameterTypes.Length - 1; i >= 0; i--)
                    Pop();
                if (callSig.ReturnType is { Kind: TypeKind.Class, Class: { } ecCls })
                    _c.NoteReferencedType(ecCls);
                string ecT = CppTypes.Of(callSig.ReturnType);
                Push(StackKind.Ref, ecT, $"({ecT})nullptr");
                return true;
            }
            case InterceptEmitArm.ExecutionContextRunDirect:
            {
                var state = Pop();
                var callback = Pop();
                Pop(); // ExecutionContext
                Emit($"dn2cpp_paramthread_invoke({Cast(callback, "Dn2CppObject*")}, {Cast(state, "Dn2CppObject*")});");
                return true;
            }
            case InterceptEmitArm.GcFamily:
                switch (name)
                {
                    case "SuppressFinalize":
                    {
                        var o = Pop();
                        Emit($"dn2cpp_gc_suppress_finalize({Cast(o, "Dn2CppObject*")});");
                        return true;
                    }
                    case "ReRegisterForFinalize":
                    {
                        var o = Pop();
                        Emit($"dn2cpp_gc_reregister_for_finalize({Cast(o, "Dn2CppObject*")});");
                        return true;
                    }
                    case "Collect":
                        Emit("dn2cpp_gc_collect();");
                        return true;
                    case "WaitForPendingFinalizers":
                        Emit("dn2cpp_gc_wait_for_pending_finalizers();");
                        return true;
                    case "GetTotalMemory":
                    {
                        var force = Pop();
                        Push(StackKind.I8, "int64_t", $"dn2cpp_gc_get_total_memory((int32_t)({force.Expr}))");
                        return true;
                    }
                    case "GetAllocatedBytesForCurrentThread":
                        Push(StackKind.I8, "int64_t", "dn2cpp_gc_allocated_bytes_current_thread()");
                        return true;
                    case "GetTotalAllocatedBytes":
                        // Boehm keeps one lifetime total; precise/approximate is a
                        // distinction its accounting does not make, so drop the flag.
                        Pop();
                        Push(StackKind.I8, "int64_t", "dn2cpp_gc_total_allocated_bytes()");
                        return true;
                    case "KeepAlive":
                    {
                        var o = Pop();
                        Emit($"dn2cpp_keep_alive({Cast(o, "Dn2CppObject*")});");
                        return true;
                    }
                    default:
                        throw new InvalidOperationException(
                            $"GcFamily arm: no body for System.GC.{name}");
                }
            case InterceptEmitArm.ObjectFinalizeNoOp:
                Pop(); // the receiver (`this`); a bare ldarg.0, side-effect free
                return true;
            case InterceptEmitArm.EncodingGetString:
                // Total over the modeled decoder shapes: TryEmitEncodingGetString
                // raises its own throw for an unmodeled shape and never returns
                // false, so a decline here is an internal invariant break.
                if (!TryEmitEncodingGetString(declType!, sig()))
                    throw new InvalidOperationException(
                        $"EncodingGetString arm declined {declType}.{name}");
                return true;
            case InterceptEmitArm.SpanCaseFold:
            {
                var csePs = sig().ParameterTypes;
                string dstT = NewTemp(CppTypes.Of(csePs[1]));
                Emit($"{dstT} = {Pop().Expr};");
                string srcT = NewTemp(CppTypes.Of(csePs[0]));
                Emit($"{srcT} = {Pop().Expr};");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_span_case_invariant({srcT}.f__reference, {srcT}.f__length, "
                    + $"{dstT}.f__reference, {dstT}.f__length, "
                    + $"{(name == "ToUpperInvariant" ? 1 : 0)})");
                return true;
            }
            case InterceptEmitArm.IoIntrinsic:
                // The row is boolean; WHICH lowering is re-asked here. The
                // second predicate call reads the memoized thunk — no second
                // decode — and it is non-null because the row Matched, which
                // asked this same predicate. A decline is therefore an internal
                // invariant break, and it must throw rather than return true
                // having emitted nothing: the operands would stay on the
                // evaluation stack and the transpile would SUCCEED, emitting C++
                // that compiles, links, and does the wrong thing — the one
                // failure shape with no diagnostic anywhere in the toolchain.
                if (CoreIntrinsics.LoweredIoMember(declType, name, sig) is not { } io)
                    throw new InvalidOperationException(
                        $"IoIntrinsic arm declined {declType}.{name}");
                EmitIoIntrinsic(io);
                return true;
            case InterceptEmitArm.EnvIntrinsic:
                EmitIntrinsic("System.Environment", name, sig());
                return true;
            case InterceptEmitArm.InlinePrimitive:
                EmitIntrinsic(declType!, name, sig());
                return true;
            case InterceptEmitArm.ComparerCompare:
                EmitIntrinsic(declType!, name, sig());
                return true;
            case InterceptEmitArm.WideTryFormat:
                EmitIntrinsic(declType!, "TryFormat", sig());
                return true;
            default:
                throw new InvalidOperationException(
                    $"no MemberReference emit arm for {row.EmitArm} ({declType}.{name})");
        }
    }

    /// <summary>Whether a MemberReference call token is the Enum.HasFlag the emitter
    /// lowers to an inline bit test — the SAME row the reachability scan asks
    /// (<see cref="CoreIntrinsics.ScEnumHasFlag"/>), so the intercepted member set
    /// cannot drift between the scan's skip and this route.
    ///
    /// <para>The name is tested first, through the shared predicate rather than by
    /// reading the row's gate field: composing the token's parent-type name builds a
    /// string, and every call token of every compiled body passes here. Asking
    /// <c>Matches</c> behind it is not a second copy of the set — it is the same row
    /// twice.</para></summary>
    private bool IsEnumHasFlagCall(MemberReferenceHandle mrh)
    {
        string n = _reader.GetString(_reader.GetMemberReference(mrh).Name);
        return CoreIntrinsics.IsEnumHasFlagMemberName(n)
            && CoreIntrinsics.ScEnumHasFlag.Matches(_c.MemberRefParentTypeName(_module, mrh), n);
    }

    /// <summary>Runs a non-beforefieldinit declaring type's cctor before any call route
    /// can replace the method and return. Reads only the MemberRef signature header to
    /// distinguish static from instance; decoding its parameter types here would grow the
    /// compilation for every call before the route decides whether it needs them.</summary>
    private void EnsureStaticCallCctorBefore(EntityHandle handle)
    {
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            EnsureStaticCallCctorBefore(
                _reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method);
            return;
        }

        ClassInfo? cls;
        if (handle.Kind == HandleKind.MethodDefinition)
        {
            var md = _reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            if ((md.Attributes & System.Reflection.MethodAttributes.Static) == 0)
                return;
            var decl = md.GetDeclaringType();
            cls = _module.ClassMap.TryGetValue(decl, out var mapped) ? mapped
                : _method.DeclaringClass.Handle == decl ? _method.DeclaringClass
                : null;
        }
        else if (handle.Kind == HandleKind.MemberReference)
        {
            var mr = _reader.GetMemberReference((MemberReferenceHandle)handle);
            if (_reader.GetBlobReader(mr.Signature).ReadSignatureHeader().IsInstance)
                return;
            cls = mr.Parent.Kind switch
            {
                HandleKind.TypeSpecification => _reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent)
                    .DecodeSignature(_c.SigProvider, _method.Context).Class,
                HandleKind.TypeDefinition => _module.ClassMap.TryGetValue(
                    (TypeDefinitionHandle)mr.Parent, out var mapped) ? mapped
                    : _method.DeclaringClass.Handle == (TypeDefinitionHandle)mr.Parent
                        ? _method.DeclaringClass : null,
                HandleKind.TypeReference => _c.ResolveTypeRef(_module, (TypeReferenceHandle)mr.Parent)?.Class,
                _ => null,
            };
        }
        else
        {
            return;
        }

        if (cls is { IsBeforeFieldInit: false })
            EnsureCctorBefore(cls);
    }

    private void TranslateCall(Instruction insn, bool isCallvirt)
    {
        // The raw call token, for rgctx slot keying (verified before use).
        _callSiteToken = insn.Token;
        _callIsVirtual = isCallvirt;
        var handle = SRME.EntityHandle(insn.Token);
        EnsureStaticCallCctorBefore(handle);
        // Enum.HasFlag(flag): both the receiver and the flag arrive as boxed Enum
        // values; the call is non-virtual (HasFlag is sealed on Enum). Test the bits
        // inline rather than reaching the real (GetMethodTable/InternalCall) body.
        //
        // This mouth's POSITION is semantics and stays hand-written: it runs at the very
        // top of TranslateCall, ahead of the constrained handling, and clears the pending
        // `constrained.` prefix itself, so moving it anywhere a chain walk could reach
        // would change which arm consumes the prefix (InterceptEmitArm.EnumHasFlagBitTest).
        // The member SET, however, is asked from the row its reach-side twin asks
        // (CoreIntrinsics.ScEnumHasFlag) — position being unmovable is a reason not to
        // move the test, never a reason to re-spell the question it asks.
        if (handle.Kind == HandleKind.MemberReference
            && IsEnumHasFlagCall((MemberReferenceHandle)handle))
        {
            var flag = Pop();   // boxed Enum
            var self = Pop();   // boxed Enum (receiver)
            string sv = $"(*(int32_t*)((Dn2CppObject*)({self.Expr}) + 1))";
            string fv = $"(*(int32_t*)((Dn2CppObject*)({flag.Expr}) + 1))";
            Push(StackKind.I4, "int32_t", $"((({sv}) & ({fv})) == ({fv}) ? 1 : 0)");
            _constrained = null;
            return;
        }
        // constrained. callvirt on a primitive or enum (e.g. a key's GetHashCode
        // in Dictionary.TryInsert) devirtualizes to a type-specialized op.
        // A value-type struct calling an intrinsic Object virtual (ToString) is
        // also routed here — object::ToString resolves to an intrinsic, so the
        // normal constrained path (EmitConstrainedCall) never runs and the raw
        // struct pointer would otherwise reach dn2cpp_object_tostring.
        if (isCallvirt && _constrained is { } cn
            && (cn.Kind == TypeKind.Primitive || cn is { Kind: TypeKind.Class, Class.IsEnum: true }
                || cn is { Kind: TypeKind.Class, Class.IsValueType: true }
                || IsDefaultNameExternalIntrinsic(cn))
            && TryEmitValueConstrained(handle, cn))
            return;
        // A generic/interpolation constrained call can close over an intrinsic CLR
        // reference type that is represented by-value in C++ (memory-map handles).
        // There is no object pointer to dereference or box; inherited ToString depends
        // only on the exact CLR type. Keep a declared override on normal intrinsic
        // dispatch so an unmodeled custom formatter remains loud.
        if (isCallvirt && _constrained is { Kind: TypeKind.Class,
                Class: { IsValueType: false, IntrinsicCppName: not null } vrr } vrc
            && CppTypes.KindOf(vrc) == StackKind.Struct
            && !_c.DeclaresIntrinsicToStringOverride(vrr)
            && (handle.Kind switch
                {
                    HandleKind.MemberReference => _reader.GetString(_reader.GetMemberReference((MemberReferenceHandle)handle).Name),
                    HandleKind.MethodDefinition => _reader.GetString(_reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
                    _ => "",
                }) == "ToString"
            && ConstrainedCalleeSig(handle).ParameterTypes.Length == 0)
        {
            var receiver = Pop();
            string ti = TypeInfoExpr(vrc)
                ?? throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: constrained ToString on {vrc} has no emitted type-info");
            Push(StackKind.Ref, "Dn2CppString*",
                $"((void)({receiver.Expr}), dn2cpp_type_tostring({ti}))");
            _constrained = null;
            return;
        }
        // constrained. callvirt on a *reference* class/interface — a generic
        // parameter (e.g. ConcurrentDictionary<TKey,…>'s TKey) instantiated as a
        // reference type. Per ECMA III.2.1, the managed-pointer receiver is
        // dereferenced and a normal callvirt is dispatched on the object. Do the
        // deref here and drop the prefix so the call routes through the regular
        // virtual/interface/intrinsic dispatch below: an intrinsic Object method
        // (GetHashCode/Equals/ToString) is served from the intrinsic table, which
        // bypasses EmitConstrainedCall and would otherwise pop the raw byref and
        // pass it unread — reading the key's fields as a vtable and crashing. The
        // value-type case is already handled above (TryEmitValueConstrained).
        // The reference primitives take the same path: Object itself, and the
        // CnRef placeholder standing for every reference argument of a shared
        // canonical body (String-specific shapes were already served above).
        if (isCallvirt && _constrained is { } rc
            && (rc is { Kind: TypeKind.Class, Class.IsValueType: false }
                || rc is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object or PrimitiveTypeCode.String }))
        {
            // The receiver byref sits BELOW the call's arguments on the stack, so
            // deref it in place at its slot (the args stay on top) — popping the top
            // would deref the last argument for an arg-bearing method such as
            // Object::Equals(object). Dropping the prefix then routes the call through
            // the regular virtual/interface/intrinsic dispatch below.
            int recvIdx = _stack.Count - 1 - ConstrainedReceiverArgCount(handle);
            var byref = _stack[recvIdx];
            string rct = CppTypes.Of(rc);
            _stack[recvIdx] = new StackEntry($"(*({rct}*)({byref.Expr}))", StackKind.Ref, rct);
            _constrained = null;
        }
        switch (handle.Kind)
        {
            case HandleKind.MethodDefinition:
            {
                var callee = ResolveMethodDef((MethodDefinitionHandle)handle);
                // The intra-CoreLib runtime primitives reached only via in-CoreLib MethodDef
                // calls: GC's finalizer/collect/KeepAlive entry points, SpanHelpers.Memmove,
                // Marshal's errno slot + UTF-8 decoder, NativeLibrary.GetSymbol,
                // Interop.GetRandomBytes, the entry-assembly InternalCall. Their real
                // FCall/InternalCall/QCall bodies are deleted from the reachability tree, and
                // Compilation.ResolveCallTarget asks THIS SAME PREDICATE to delete them: one
                // question, two askers, no second list to drift.
                //
                // The cut is by NAME, over the whole overload family, so the route may not be
                // by shape. That mismatch — a name-only cut paired with a shape-guarded route —
                // is precisely where an overload the tables do not model falls: body deleted,
                // no lowering found, the call emitted against a symbol nothing generated. Not a
                // transpile error; an undefined symbol at C++ LINK time, long afterwards. So
                // EmitRuntimePrimitive is TOTAL over the predicate — it still selects on the
                // shape, but an unmodeled shape leaves through a throw, never through
                // EmitManagedCall.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdRuntimePrimitive, callee))
                    return;
                // Framework EventSource tracing guards: EventSource.IsEnabled() ->
                // false and a provider's event methods -> no-op (its Log singleton is
                // folded to null; see the Ldsfld handler + the ReachCctor skip).
                // This keeps the whole EventSource subtree — its finalizer -> Dispose ->
                // SendManifest -> manifest-generation cascade (Guid/HexConverter/reflection/
                // ResourceManager/ICU) — out of the tree.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdEventSourceIsEnabled, callee))
                    return;
                // The provider members behind the same fold: not a descriptor row —
                // IsFrameworkEventSourceProvider needs the ClassInfo, so the route
                // stays the non-pure helper (its IsEnabled arm was answered above).
                if (TryEmitEventSourceNoOp(callee))
                    return;
                // Regex.Compile -> a dead null factory (its dynamic-code guard is
                // const-folded false, so RegexOptions.Compiled degrades to the
                // interpreter, like NativeAOT). Edges cut in
                // Compilation.ResolveCallTarget.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdRegexCompileFold, callee))
                    return;
                // Const-folded capability getters (invariant globalization, dynamic
                // code) -> constants; guards branching on them have their dead arms
                // pruned by BranchLiveness. See CoreIntrinsics.ConstFoldedGetter.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdConstFoldedGetter, callee))
                    return;
                // Const-folded string-returning calls (the Windows-CoreLib TimeZoneInfo
                // display-name internals) -> the literal their every live exit returns.
                // Not the getter row above: these take arguments and push a string, so
                // they prune no branch and BranchLiveness cannot reach them — their dead
                // offsets sit inside a `using (RegistryKey)` exception region. Edges cut
                // in Compilation.ResolveCallTarget. See
                // CoreIntrinsics.ConstFoldedStringCall.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdConstFoldedStringCall, callee))
                    return;
                // SerializationInfo.ThrowIfDeserializationInProgress guards state
                // mutation during a BinaryFormatter deserialization; dn2cpp has no
                // runtime deserialization, so both static overloads are a no-op. The
                // real body pulls AsyncLocal<bool> -> ExecutionContext.GetLocalValue ->
                // Thread internals (intrinsic-modeled, no transpiled struct). Reached
                // from FileStreamHelpers.SerializationGuard on the real file-I/O path.
                // The edge is cut in Compilation.ResolveCallTarget.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdDeserializationGuard, callee))
                    return;
                // The System.Environment OVERLOADS a dn2cpp_env_* / _process_path /
                // _environment_* helper models, reached intra-corelib (MethodDef):
                // GetEnvironmentVariable(string) from AppContextConfigHelper.GetBooleanConfig
                // <- SafeFileHandle..cctor on the real file-I/O path, FailFast (the whole
                // overload family) from ExecutionContext.OnValuesChanged under the thread-pool
                // work-item dispatch. Lowered exactly as the app-facing MemberRef form lowers
                // them: their real bodies are the Kernel32/registry P/Invoke branch, the
                // Interop.Sys startup-path QCall, the _Exit InternalCall plus CLR shutdown
                // plumbing, and the StackCrawlMark fail-fast InternalCall — none linkable here.
                //
                // ONE predicate decides, and ResolveCallTarget asks the same one to cut the
                // real body's reachability edge. A hand-kept pair of lists drifts: a member
                // the cut deletes with no route here to lower the call leaves anything
                // reaching it emitting a call to a body nothing put in the tree — an
                // undefined symbol at C++ LINK time, not a transpile error.
                //
                // The row's TypeGate is deliberate: invoking the predicate unconditionally
                // would allocate a sig-thunk closure for every MethodDef call this loop
                // compiles, so the gate short-circuits the non-Environment callees out
                // first. It duplicates the predicate's TYPE test, never its member set —
                // a type name cannot drift.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdEnvMember, callee))
                    return;
                // Byte/SByte/Int16/UInt16 ToString/Parse/TryParse/TryFormat reached
                // intra-corelib (MethodDef) — AssemblyNameParser.TryParseVersion calls
                // UInt16::TryParse this way, from AssemblyName..ctor. Lower them to the same
                // runtime formatters and width-parameterized NumberStyles engine as the
                // app-facing MemberRef form, which this mirrors exactly: ONE predicate names
                // the member set, and ResolveCallTarget asks the same one to cut the real
                // body's reachability edge.
                // Without the MethodDef route the intra-corelib call falls through to
                // EmitManagedCall and names a body the cut deleted — not a transpile error
                // but an undefined symbol at C++ LINK time.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdInlinePrimitive, callee))
                    return;
                // System.Collections.Comparer.Compare — body-intercepted: a direct call lowers
                // to dn2cpp_object_compare (the same lowering the synthesized body and the interface
                // slot use). Same predicate as the reachability cut (ResolveCallTarget), so the two
                // askers cannot disagree.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdComparerCompare, callee))
                    return;
                // ExecutionContext.Capture() reached intra-corelib (MethodDef) — the
                // thread-pool work-item schedulers (ThreadPoolValueTaskSource.
                // QueueToThreadPool) stash the captured context to flow it into
                // Execute. dn2cpp models no ExecutionContext flow across a pool hop,
                // so Capture lowers to the null "nothing to flow" encoding every
                // consumer already handles (the null/IsDefault fast arms). Its real
                // body reads Thread._executionContext on the intrinsic Thread model
                // (no transpiled struct). The edge is cut in
                // Compilation.ResolveCallTarget.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdExecutionContextCapture, callee))
                    return;
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdExecutionContextRun, callee))
                    return;
                // SynchronizationContext's thread-slot statics reached intra-corelib
                // (MethodDef): Current reads / SetSynchronizationContext writes the
                // per-thread runtime slot (a constant-folded null Current would
                // deadlock any installed context's Send fast path:
                // `Current == this` never true). The real bodies touch
                // Thread._synchronizationContext on the intrinsic Thread model, so
                // both edges are cut in Compilation.ResolveCallTarget; instance
                // members (Post/Send/…) transpile from real IL (the base Post stays
                // bounded — its body reaches ThreadPool.QueueUserWorkItem<TState>).
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdSyncContextSlot, callee))
                    return;
                // The non-generic MemoryMarshal.GetArrayDataReference(Array) reached
                // intra-CoreLib — Marshal.UnsafeAddrOfPinnedArrayElement calls it
                // this way. Lowered to dn2cpp_pinned_data_addr exactly as the app-facing
                // MemberRef form is; the SAME row's predicate cuts the real body's edge in
                // Compilation.ResolveCallTarget, whose GetMethodTable pointer math has no
                // mapping.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdMemoryMarshalArrayData, callee))
                    return;
                // An intra-assembly call into an intrinsic-mapped type (e.g.
                // corelib's Span<T> -> ThrowHelper) is emitted inline, not
                // transpiled — its body is never pulled into the tree.
                if (TryEmitMethodDefIntercept(CoreIntrinsics.MdIntrinsicType, callee))
                    return;
                // A nested intrinsic type (e.g. StringBuilder.AppendInterpolatedStringHandler,
                // reached intra-corelib from DecoderExceptionFallbackBuffer.Throw via a
                // MethodDefinition): its bare FullName is not a top-level intrinsic key, so
                // route on the enclosing-qualified dispatch name, matching the MemberReference
                // path (TranslateIntrinsic). Without this its .ctor / AppendLiteral fall through
                // to EmitManagedCall and name a struct/method that is never emitted.
                // The same route carries an adopted third-party async task type reached
                // WITHIN its own assembly: IntrinsicDispatchName maps it to its BCL
                // key, which is never its own FullName, so this guard admits it.
                if (callee.DeclaringClass.IntrinsicCppName is not null
                    && IntrinsicDispatchName(callee.DeclaringClass) is { } nestedKey
                    && nestedKey != callee.DeclaringClass.FullName)
                {
                    EmitIntrinsic(nestedKey, callee.Name, callee.Signature,
                        AdoptedFrom(callee.DeclaringClass));
                    return;
                }
                // Force every file open onto the synchronous Windows FileStream
                // strategy (see NeutralizeFileStreamAsyncArgs). Rewrites the
                // FileOptions/isAsync argument in place, then falls through to the
                // ordinary transpiled call.
                NeutralizeFileStreamAsyncArgs(callee);
                EmitManagedCall(callee, isCallvirt);
                return;
            }
            case HandleKind.MethodSpecification:
            {
                // A generic method on an intrinsic-mapped type (e.g. Unsafe.Add<T>)
                // is emitted inline rather than transpiling its body. The
                // spec's method is a MemberRef across assemblies and a MethodDef
                // within one (e.g. corelib's Span<T> -> Unsafe.Add<T>).
                var ms = _reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                // AsyncTaskMethodBuilder.Start<TSM> /.AwaitUnsafeOnCompleted —
                // generic (AsyncTaskMethodBuilder<T>) or non-generic (async Task)
                // — are emitted inline.
                // Task<T>.ContinueWith<TNewResult>(Func<Task<T>, TNewResult>) — an
                // instance generic method whose MemberRef parent is the closed
                // Task<T> TypeSpec, invisible to MethodSpecParentTypeName below
                // (TypeRef parents only) and matched BEFORE the async-builder route
                // (IsAsyncBuilderSpec admits every Task-family TypeSpec parent). The
                // non-generic Task receiver's form goes through the ordinary
                // intrinsic-type route; both land in the same TranslateGenericIntrinsic
                // case, which re-derives the parent context to close the signature's !0.
                if (_c.MethodSpecMethodName(_module, ms) == "ContinueWith"
                    && _c.TaskFamilyMethodSpecParent(_module, ms, _method.Context) is { } cwParent
                    && _c.GenericDefFullName(cwParent) == "System.Threading.Tasks.Task")
                {
                    TranslateGenericIntrinsic("System.Threading.Tasks.Task", "ContinueWith", (MethodSpecificationHandle)handle);
                    return;
                }
                if (_c.IsAsyncBuilderSpec(_module, ms, _method.Context))
                {
                    TranslateAsyncGenericIntrinsic(_c.MethodSpecMethodName(_module, ms), (MethodSpecificationHandle)handle);
                    return;
                }
                // TSelf.Create{Checked,Truncating,Saturating}<TOther> reached through
                // the closed interface instantiation (INumberBase<TSelf>.Create*, a
                // MemberRef whose parent is a TypeSpec — MethodSpecParentTypeName only
                // reads TypeReference parents, so the primitive-decl dispatch below
                // never sees this shape). On genuine closed primitives it is the same
                // pure numeric cast as the direct form; transpiling the real interface
                // body instead leaves a per-element typeof identity check plus a
                // box/unbox round-trip inside generic-math reduction hot loops
                // (Enumerable.Sum's checked accumulate). A canonical-placeholder TSelf
                // or TOther falls through: the shared trial taints on the body's type
                // identity and each specialization comes back here closed.
                //
                // TSelf is not required to be a PRIMITIVE: Int128/UInt128 are transpiled
                // structs (TypeKind.Class) reached the same way when generic-math parse
                // code (System.Number.ParseBinaryInteger<TInteger>) does
                // `constrained. Int128 call INumberBase<TSelf>::CreateTruncating<TOther>`
                // — the constrained STATIC-virtual form of the same call. Admit any
                // non-placeholder TSelf and let CreateTargetPrimitiveName decide: it maps
                // exactly the integer/float primitives and Int128/UInt128, returning null
                // (fall-through) for anything else. Same shape the reach-side constrained
                // cut admits (Compilation.Reachability's static-virtual arm,
                // CoreIntrinsics.MsInt128CreateConversion), so route and cut cannot drift.
                if (ms.Method.Kind == HandleKind.MemberReference
                    && _reader.GetMemberReference((MemberReferenceHandle)ms.Method) is { Parent.Kind: HandleKind.TypeSpecification } numMr
                    && _reader.GetString(numMr.Name) is "CreateTruncating" or "CreateChecked" or "CreateSaturating"
                    && _reader.GetTypeSpecification((TypeSpecificationHandle)numMr.Parent)
                            .DecodeSignature(_c.SigProvider, _method.Context) is
                        { Kind: TypeKind.Class, Class: { Namespace: "System.Numerics" } numItf }
                    && numItf.Name.StartsWith("INumberBase_", StringComparison.Ordinal)
                    && numItf.Context.TypeArgs is [{ IsCanonPlaceholder: false } numTSelf]
                    && ms.DecodeSignature(_c.SigProvider, _method.Context) is [{ Kind: TypeKind.Primitive, IsCanonPlaceholder: false } numTOther]
                    && CreateTargetPrimitiveName(numTSelf, numTOther) is { } numDecl)
                {
                    TranslateGenericIntrinsic(numDecl, _reader.GetString(numMr.Name), (MethodSpecificationHandle)handle);
                    return;
                }
                if (_c.MethodSpecParentTypeName(_module, ms) is { } pn)
                {
                    string mname = ms.Method.Kind == HandleKind.MemberReference
                        ? _reader.GetString(_reader.GetMemberReference((MemberReferenceHandle)ms.Method).Name)
                        : _reader.GetString(_reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).Name);
                    // MemoryMarshal isn't a fully intrinsic-mapped type (its other
                    // methods transpile normally), but GetArrayDataReference must be
                    // intercepted — its real body tail-calls the non-generic (Array)
                    // overload we don't model and degenerates into infinite recursion.
                    // MemoryExtensions.Contains/IndexOf (what `arr.Contains(x)` binds to,
                    // via the array -> ReadOnlySpan<T> implicit conversion) vectorize with
                    // SIMD + Unsafe.BitCast in the real BCL — untranspilable. Intercept the
                    // (span, value) shape and emit a scalar loop instead.
                    // System.Enum isn't a fully intrinsic-mapped type, but its generic
                    // reflection statics (GetValues<T>/GetNames<T>/GetName<T>/Parse<T>/
                    // TryParse<T>/IsDefined<T>) are lowered inline from the per-enum
                    // (name, value) table — their real bodies route through the
                    // reflection metadata stack we don't model.
                    // Activator.CreateInstance<T> is the generic factory; its real
                    // body reflects, so it is lowered inline to `new T`.
                    // Each row below is the SAME row the reachability cut asks
                    // (Compilation.ResolveCallTarget's MethodSpecification arm), referenced
                    // here at THIS chain's own position — the descriptor registry is a shared
                    // truth source, not a shared walk order (see NameKeyedIntercept).
                    if (CoreIntrinsics.IsIntrinsicType(pn)
                        // GetArrayDataReference (above) plus the span-shaping helpers
                        //: their real bodies use JIT intrinsics we don't model
                        // (IsReferenceOrContainsReferences / Unsafe.As / nuint math), so
                        // we build the result span struct directly in TranslateGenericIntrinsic.
                        //
                        // NOT a descriptor row: this is a route WITHOUT a matching cut in
                        // ResolveCallTarget's MethodSpec arm (the edges these would delete are
                        // already gone via IsIntrinsicType on the receiver span types). Route
                        // without cut is mere bloat — a body transpiled that nothing calls —
                        // never a link error, so the asymmetry is sound and stays written out.
                        || (pn == "System.Runtime.InteropServices.MemoryMarshal"
                            && mname is "GetArrayDataReference" or "GetReference" or "Cast"
                                or "AsBytes" or "Read" or "Write" or "CreateSpan" or "CreateReadOnlySpan")
                        || CoreIntrinsics.MsActivatorCreateInstance.Matches(pn, mname)
                        // Marshal.{SizeOf,PtrToStructure,StructureToPtr,OffsetOf}<T> (
                        // OffsetOf added ): blittable struct <-> native memory marshalling.
                        // Their real bodies reflect / P-Invoke into the marshaller we don't
                        // model, so emit the sizeof / value copy / offsetof inline.
                        // Marshal.GetFunctionPointerForDelegate<T> / GetDelegateForFunction-
                        // Pointer<T> lower to the generated per-delegate-type thunk-pool
                        // helpers (their real bodies are QCalls into the native marshaller).
                        || CoreIntrinsics.MsMarshalGenerics.Matches(pn, mname)
                        || CoreIntrinsics.MsEnumStatics.Matches(pn, mname)
                        // INumberBase<T>.CreateTruncating/CreateChecked/CreateSaturating<TOther>
                        // on a concrete integer primitive: a pure numeric cast. The row's own
                        // predicate is what makes the sub-word primitive targets (Byte/SByte/
                        // Int16/UInt16) — which aren't in the intrinsic-type set — dispatch too;
                        // the 32/64-bit targets already qualify via IsIntrinsicType.
                        || CoreIntrinsics.MsCreateConversion.Matches(pn, mname)
                        // Int128/UInt128.CreateTruncating<TOther>: transpiled structs (not
                        // primitives), lowered inline to the sign/zero-extending widening into
                        // {_lower,_upper}. Same row the reachability cut asks (Resolve.cs's
                        // MethodSpec arm), referenced here at this chain's position.
                        || CoreIntrinsics.MsInt128CreateConversion.Matches(pn, mname)
                        || CoreIntrinsics.MsMemoryExtScan.Matches(pn, mname)
                        // ContainsAny[Except] route inline only in their SearchValues<T>
                        // 2-arg shape (see TranslateGenericIntrinsic). The row owns the name
                        // set; the shape test is the one shared call to
                        // IsMemoryExtSearchValuesForm, which the reachability cut makes too
                        // (it needs this asker's own generic context, so it cannot be a row).
                        || (CoreIntrinsics.MsMemoryExtSearchValues.Matches(pn, mname)
                            && _c.IsMemoryExtSearchValuesForm(_module, ms, _method.Context))
                        // CustomAttributeExtensions.GetCustomAttributes<T> returns
                        // IEnumerable<T>; its real body casts an Attribute[] to
                        // IEnumerable<T>, which a foreach lowers to an IEnumerator<T> loop.
                        // Its intermediate IEnumerable<Attribute> typing loses the covariance
                        // a later cast to IEnumerable<T> needs, so intercept the call and hand
                        // back the typed-filtered array retagged with its precise ti_arr_<T>
                        // handle (the SZArray interface map serves the foreach, / ).
                        //
                        // NOT a descriptor row: route without cut, like the MemoryMarshal
                        // helpers above — the real body stays in the tree, which costs a
                        // transpiled body and cannot cost a link error.
                        || (pn == "System.Reflection.CustomAttributeExtensions" && mname == "GetCustomAttributes")
                        // StringBuilder.AppendInterpolatedStringHandler.AppendFormatted<T>
                        // (the `sb.Append($"...{value}...")` runtime-formatted hole). The
                        // handler is modeled as the underlying StringBuilder; this generic
                        // method formats T and appends straight to it. Its real body reaches
                        // Enum.TryFormatUnconstrained -> the EnumInfo/Number-format cascade, so
                        // it is lowered inline.
                        || CoreIntrinsics.MsAppendInterpolatedFormatted.Matches(pn, mname)
                        // GC.AllocateUninitializedArray<T>(length [, pinned]) -> new T[length].
                        // "Uninitialized" is only a zero-fill optimization; a zero-init array is
                        // always a valid result. The real worker reflects on typeof(T).TypeHandle
                        // (an InternalCall) to pick a fast path, so lower it inline instead.
                        || CoreIntrinsics.MsGcAllocateUninitializedArray.Matches(pn, mname))
                    {
                        TranslateGenericIntrinsic(pn, mname, (MethodSpecificationHandle)handle);
                        return;
                    }
                }
                EmitManagedCall(_c.ResolveMethodSpec(_module, (MethodSpecificationHandle)handle, _method.Context), isCallvirt);
                return;
            }
            case HandleKind.MemberReference:
            {
                var mr = _reader.GetMemberReference((MemberReferenceHandle)handle);
                string mrName = _reader.GetString(mr.Name);
                if (mr.Parent.Kind == HandleKind.TypeSpecification)
                {
                    var parent = _reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(_c.SigProvider, _method.Context);
                    if (parent.Kind == TypeKind.SZArray || parent.Kind == TypeKind.MDArray)
                    {
                        TranslateArrayMethodCall(parent, mr);
                        return;
                    }
                    // A member on a generic Task-family async type (Task<T>,
                    // AsyncTaskMethodBuilder<T>, TaskAwaiter<T>) is emitted inline.
                    // Its signature's type params (!0) are closed by the parent's
                    // context.
                    if (parent is { Kind: TypeKind.Class, Class: { IntrinsicCppName: not null } pc })
                    {
                        var pcSig = mr.DecodeMethodSignature(_c.SigProvider, pc.Context);
                        // A member/operator on a closed Vector64/128/256/512<T> / Vector<T>
                        // struct: the receiver supplies the element (instance get_Item /
                        // GetLower / get_Count have no vector in their signature), so dispatch
                        // directly with the closed element rather than via the helper scan.
                        if (pc.IntrinsicCppName.StartsWith("Dn2CppVector", StringComparison.Ordinal)
                            && pc.Context.TypeArgs.Length == 1
                            && TryEmitVectorOp(pc.IntrinsicCppName, VecWidthBytes(pc.IntrinsicCppName),
                                pc.Context.TypeArgs[0], mrName, pcSig, null))
                            return;
                        if ((_c.AdoptedAsyncKey(pc) ?? _c.GenericDefFullName(pc))
                                == "System.Threading.Tasks.ValueTask"
                            && pc.Context.TypeArgs is [var resultType]
                            && mrName == "ToString" && pcSig.ParameterTypes.Length == 0)
                        {
                            var receiver = Pop();
                            string resultTi = TypeInfoExpr(resultType)
                                ?? throw new NotSupportedException(
                                    $"{_method.DeclaringClass.FullName}.{_method.Name}: ValueTask<{resultType}> result has no emitted type-info");
                            Push(StackKind.Ref, "Dn2CppString*",
                                $"dn2cpp_valuetask_tostring((Dn2CppTaskAwaiter*)({receiver.Expr}), {resultTi})");
                            return;
                        }
                        // An adopted third-party task type (GDTask<T>, its builder, its
                        // awaiter) answers to a BCL key; anything else is its own name.
                        EmitIntrinsic(_c.AdoptedAsyncKey(pc) ?? _c.GenericDefFullName(pc),
                            mrName, pcSig, AdoptedFrom(pc));
                        return;
                    }
                    // A callvirt through IEqualityComparer<T> on a Dictionary's
                    // _comparer. A null / default-wrapper comparer devirtualizes to
                    // the type-specialized op; a real user comparer (e.g. a
                    // case-insensitive or by-length one) is dispatched through the
                    // object at runtime.
                    if (parent is { Kind: TypeKind.Class, Class: { } ic }
                        && _c.GenericDefFullName(ic) == "System.Collections.Generic.IEqualityComparer"
                        && TryEmitComparerDispatch(
                            _c.ResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context),
                            ic.Context.TypeArgs[0]))
                        return;
                    // A callvirt through IComparable<T>.CompareTo on a boxed primitive/
                    // enum/string — the unconstrained `(IComparable<T>)box.CompareTo`
                    // cast form. The boxed
                    // value's intrinsic type-info has no IComparable<T> interface map, so
                    // real dispatch can't resolve it; devirtualize to the same typed
                    // three-way compare, unboxing the receiver.
                    if (parent is { Kind: TypeKind.Class, Class: { } cc }
                        && _c.GenericDefFullName(cc) == "System.IComparable"
                        && mrName == "CompareTo"
                        && ComparablePrimitiveArg(parent) is { } cmpT)
                    {
                        EmitBoxedComparableCompareTo(cmpT);
                        return;
                    }
                    // Span<T>/ReadOnlySpan<T> instance bulk methods (Clear/Fill/CopyTo/
                    // ToArray) — the BCL bodies use SpanHelpers/Buffer.Memmove (SIMD /
                    // InternalCall), untranspilable; emit element loops instead.
                    if (TryEmitSpanInstanceBulk(mr, parent))
                        return;
                    // The span indexer under [HotPath(SkipBoundsChecks = true)]:
                    // raw pointer arithmetic replaces the checked BCL body at this
                    // caller only (route-without-cut; cold callers keep the call).
                    if (_method.SkipBoundsChecks && TryEmitHotSpanItem(mr, parent))
                        return;
                    EmitManagedCall(_c.ResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context), isCallvirt);
                    return;
                }
                // Every interceptor below probes the same MemberReference, so the
                // parent type name is read once and the signature decoded lazily —
                // on first use, keeping the decode at the point the first
                // interceptor to need it would have decoded.
                string? mrParent = _c.MemberRefParentTypeName(_module, (MemberReferenceHandle)handle);
                MethodSignature<TypeDesc>? mrSig = null;
                MethodSignature<TypeDesc> Sig() => mrSig ??= mr.DecodeMethodSignature(_c.SigProvider, _method.Context);
                // Non-generic Enum reflection statics taking a runtime Type
                // (GetNames/GetName/IsDefined/Parse/TryParse/GetUnderlyingType/GetValues) —
                // the per-enum runtime table; the generic Enum.*<T> forms are a
                // MethodSpecification. The arm decodes in the NULL context (see the row).
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrEnumStatics, mr, mrParent, mrName, Sig))
                    return;
                // Nullable.GetUnderlyingType(Type): the real body reflects; the runtime
                // helper reads the synthesized Nullable<U> type-info (NULL-context decode).
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrNullableGetUnderlyingType, mr, mrParent, mrName, Sig))
                    return;
                // SynchronizationContext.get_Current / SetSynchronizationContext — the
                // per-thread runtime slot, same as the MethodDef intercept above.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrSyncContextSlot, mr, mrParent, mrName, Sig))
                    return;
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrExecutionContextCapture, mr, mrParent, mrName, Sig))
                    return;
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrExecutionContextRun, mr, mrParent, mrName, Sig))
                    return;
                // NativeMemory's C-heap wrappers / memset / memmove; the real bodies are
                // InternalCall / P-Invoke into the unmanaged allocator.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrNativeMemory, mr, mrParent, mrName, Sig))
                    return;
                // The Marshal surface (37-name family: heap alloc/free/copy, SizeOf /
                // PtrToStructure / StructureToPtr / OffsetOf, the cached last-error slot,
                // the CoTaskMem reallocators, the PtrToString / StringTo / ZeroFree
                // decoders/encoders, the typed Read*/Write* accessors). Real bodies reflect
                // / P-Invoke / FCall. ONE predicate, verified char-for-char against the cut.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrMarshalInterop, mr, mrParent, mrName, Sig))
                    return;
                // The non-generic MemoryMarshal.GetArrayDataReference(Array) overload only
                // (GenericParameterCount == 0) -> dn2cpp_pinned_data_addr; the generic
                // GetArrayDataReference<T>(T[]) is a MethodSpecification, handled above.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrMemoryMarshalArrayData, mr, mrParent, mrName, Sig))
                    return;
                // The System.GC surface -> dn2cpp_gc_* helpers. The cut family
                // (SuppressFinalize / ReRegisterForFinalize / Collect /
                // WaitForPendingFinalizers / GetTotalMemory / GetAllocatedBytesFor-
                // CurrentThread / GetTotalAllocatedBytes, shape-gated) shares its
                // predicate with the reachability cut; KeepAlive is EMIT-ONLY
                // (route-without-cut — an empty transpiled body is inlinable and -O2 erases
                // the liveness barrier), so it is a distinct row the resolver never
                // references. Both route into the GcFamily arm.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrGc, mr, mrParent, mrName, Sig))
                    return;
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrGcKeepAlive, mr, mrParent, mrName, Sig))
                    return;
                // Object.Finalize(): the compiler-generated base call in a C# destructor's
                // finally block; a pure no-op (pop the receiver). Lowered inline so own-code
                // destructors compile even when no BCL assembly is loaded.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrObjectFinalize, mr, mrParent, mrName, Sig))
                    return;
                // The System.IO.Path / System.IO.File overloads one of the dn2cpp_path_* /
                // dn2cpp_file_* helpers models lower inline — the fast path that keeps the
                // file-I/O + globalization cascade out of the tree (and the transpiler's own
                // self-host build off it). ONE predicate decides, and ResolveCallTarget asks
                // that same one to cut the real body's reachability edge, so the route and the
                // cut cannot disagree about which overloads are lowered.
                //
                // Any OTHER member or overload — the ReadOnlySpan<char> Path ops, params
                // Combine, GetFullPath(string,string), the Encoding/span File overloads —
                // returns null and falls through to the real BCL body below. That body is
                // reachable precisely because the predicate declined it here too. Routing on
                // the method name alone would cut the edge for those
                // overloads and then find no lowering for them — a hard transpile
                // failure with nothing left to fall back on.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrIoMember, mr, mrParent, mrName, Sig))
                    return;
                // System.Buffer.BlockCopy / ByteLength / Get/SetByte -> the dn2cpp_buffer_*
                // helpers; intercepted before real resolution (and excluded from reachability).
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrBufferExtent, mr, mrParent, mrName, Sig))
                    return;
                // System.IO.MemoryMappedFiles file-backed map subset (POSIX mmap): the
                // factory + view-accessor surface lowers to the dn2cpp_mmap_* helpers /
                // inline typed accessors. The real bodies are the SafeHandle /
                // UnmanagedMemoryAccessor + OS-mapping P/Invoke cascade; intercept before
                // real resolution (the types are intrinsic-mapped, so ResolveCallTarget
                // already cuts the edge). The generic Read/Write/ReadArray/WriteArray<T>
                // forms are MethodSpecs, routed through TranslateGenericIntrinsic.
                if (mrParent is "System.IO.MemoryMappedFiles.MemoryMappedFile"
                        or "System.IO.MemoryMappedFiles.MemoryMappedViewAccessor"
                        or "System.IO.UnmanagedMemoryAccessor")
                {
                    EmitIntrinsic(mrParent, mrName, Sig());
                    return;
                }
                // The view's SafeMemoryMappedViewHandle exposes AcquirePointer /
                // ReleasePointer / ByteLength (declared on SafeBuffer) and DangerousGetHandle
                // (on SafeHandle). The declaring type is the base class, so dispatch by the
                // receiver's intrinsic C++ type and only for our handle — unrelated
                // SafeBuffer/SafeHandle calls are untouched.
                if (mrParent is "System.Runtime.InteropServices.SafeBuffer"
                        or "System.Runtime.InteropServices.SafeHandle"
                    && mrName is "AcquirePointer" or "ReleasePointer" or "get_ByteLength" or "DangerousGetHandle")
                {
                    var hsig = Sig();
                    int recvIdx = _stack.Count - 1 - hsig.ParameterTypes.Length;
                    if (recvIdx >= 0 && _stack[recvIdx].CppType == "Dn2CppMappedSafeHandle")
                    {
                        EmitIntrinsic("Microsoft.Win32.SafeHandles.SafeMemoryMappedViewHandle",
                            mrName, hsig);
                        return;
                    }
                }
                // The System.Environment OVERLOADS a dn2cpp_env_* / _process_path /
                // _environment_* helper models lower inline: their real bodies are the
                // Kernel32/registry P/Invoke branch, the _Exit InternalCall plus CLR
                // shutdown plumbing, and the CLR fail-fast InternalCall — none of them
                // linkable here. ONE predicate decides, and ResolveCallTarget asks that same
                // one to cut the real body's reachability edge, so the route and the cut
                // cannot disagree about which overloads are lowered.
                //
                // GetEnvironmentVariable(string, EnvironmentVariableTarget) is the overload
                // a name-routed cut beside this shape-guarded table would drop in the
                // gap; it returns false here and transpiles from its real body, whose
                // Process arm tail-calls the one-argument form this DOES lower.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrEnvMember, mr, mrParent, mrName, Sig))
                    return;
                // AppContext.BaseDirectory -> the running executable's directory. Only this
                // member; the type's switch/data accessors transpile as real IL.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrAppContextBaseDir, mr, mrParent, mrName, Sig))
                    return;
                // Directory.Exists / GetCurrentDirectory / SetCurrentDirectory -> the runtime
                // helpers. (CreateDirectory is deliberately NOT here — callers deref the
                // returned DirectoryInfo, so its real body transpiles.)
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrDirectory, mr, mrParent, mrName, Sig))
                    return;
                // Byte/SByte/Int16/UInt16 ToString/Parse/TryParse/TryFormat lower to the same
                // runtime formatters and the same width-parameterized NumberStyles engine as
                // Int32 (which is an intrinsic TYPE, so every member of it routes inline
                // anyway). The sub-word primitives are NOT intrinsic types — their other
                // members transpile from real IL — so without this the real
                // System.Byte::ToString/Parse bodies would pull in the System.Number
                // .Format*/ParseBinaryInteger subtree, the biggest remaining self-host
                // cascade. Which is why these do NOT fall through the way Path/File do: the
                // fall-through here IS the cascade the cut exists to avoid, so the remedy for
                // an unmodeled overload is to model it, and the emit table covers the whole
                // .NET 10 surface (all 4 ToString, all 8 Parse, all 9 TryParse, both
                // TryFormat). An overload beyond it is a LOUD transpile failure naming the
                // shape, never a silent divergence.
                //
                // One predicate names the member set, and the reachability cut asks the same
                // one — as do the constrained static-virtual dispatch and its reach twin. No
                // second list to drift.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrInlinePrimitive, mr, mrParent, mrName, Sig))
                    return;
                // System.Collections.Comparer.Compare — body-intercepted, lowered inline to
                // dn2cpp_object_compare. Same predicate as the reachability cut (ResolveCallTarget).
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrComparerCompare, mr, mrParent, mrName, Sig))
                    return;
                // The WIDE integer primitives' TryFormat (the span-write formatter). They are
                // intrinsic types, so their members already route inline; this is the belt to
                // that braces, and it exists because SRM's SignatureDecoder.CheckHeader
                // reaches Byte.TryFormat from dn2cpp's own Compilation.Build — the dominant
                // remaining self-host cascade if the real System.Number.TryFormat* body were
                // ever an edge.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrWideIntTryFormat, mr, mrParent, mrName, Sig))
                    return;
                // Encoding.GetString (Encoding / UTF8Encoding / UnicodeEncoding) -> the
                // portable .NET-exact decode helper, collapsing the SIMD UTF-8 transcode
                // cascade. The row is name+type (reach cut); the emit arm adds the
                // shape-total-throw of TryEmitEncodingGetString (it has no false path).
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrEncodingGetString, mr, mrParent, mrName, Sig))
                    return;
                // System.Console.Error is the stderr TextWriter (System.Console
                // is an intrinsic type, so get_Error already routes to EmitIntrinsic). The
                // TextWriter it returns is modeled as Dn2CppTextWriter*; intercept its
                // Write/WriteLine before real resolution so the real SyncTextWriter body
                // never executes on it. Receiver-sensitive: only a receiver that is
                // statically the Dn2CppTextWriter* Console.get_Error produced routes to the
                // intrinsic — a real writer (e.g. the StreamWriter File.CreateText returns,
                // whose file-I/O bodies transpile) falls through to the normal
                // virtual dispatch below.
                if (mrParent == "System.IO.TextWriter"
                    && mrName is "Write" or "WriteLine")
                {
                    var twSig = Sig();
                    // The fast path fires only for a receiver that is statically the
                    // Console.get_Error singleton (its C++ type is Dn2CppConsoleWriter*);
                    // a spilled base System.IO.TextWriter* local falls through to the real
                    // vtable dispatch below. ConsoleErrorWriterCppType is set the
                    // moment get_Error ran, which any TextWriter receiver here came from.
                    if (_c.ConsoleErrorWriterCppType is { } ewCpp
                        && _stack[_stack.Count - 1 - twSig.ParameterTypes.Length].CppType == ewCpp)
                    {
                        EmitIntrinsic("System.IO.TextWriter", mrName, twSig);
                        return;
                    }
                }
                // TypeInfo.DeclaredFields: a runtime Type IS its own TypeInfo
                // (GetTypeInfo is the identity), so the property must answer from the
                // reflection member tables — the resolved virtual dispatch would enter
                // the runtime-owned dn2cpp_type_type, which carries no vtable.
                // Intercepted before real resolution; the real body stays reachable
                // (route-without-cut = bloat, never a link error).
                if (mrParent == "System.Reflection.TypeInfo" && mrName == "get_DeclaredFields")
                {
                    EmitIntrinsic("System.Reflection.TypeInfo", mrName, Sig());
                    return;
                }
                // MemoryExtensions.ToUpperInvariant/ToLowerInvariant(ReadOnlySpan<char>,
                // Span<char>): the per-code-unit BMP invariant fold in the runtime
                // (returns the source length, -1 when the destination is too short;
                // no overlap detection, unlike .NET's InvalidOperationException —
                // callers must keep the buffers disjoint). The real bodies route
                // through GlobalizationMode.get_Invariant -> InitICUFunctions
                // (untranspilable).
                //
                // ONE predicate decides, and ResolveCallTarget asks that same one to cut the
                // real body's edge. A name-only cut beside a shape-tested route is the
                // asymmetry that does not announce itself: an
                // overload the shape declines falls through to a real body the cut
                // already deleted — an undefined symbol at C++ LINK time, not a transpile
                // error. .NET 10 declares one overload of each, so nothing falls in the gap
                // today; asking one predicate removes the possibility rather than the instance.
                if (TryEmitMemberRefIntercept(CoreIntrinsics.MrSpanCaseFold, mr, mrParent, mrName, Sig))
                    return;
                // Portable SIMD vector static helpers (Vector64/128/256/512,
                // System.Numerics.Vector): the non-generic Create / ConvertTo* / Narrow /
                // Widen* / get_IsHardwareAccelerated members lower to the software-vector
                // runtime (dn2cpp_vec_*); the generic forms are MethodSpecs handled in
                // TranslateGenericIntrinsic. Intercept before real resolution would transpile
                // the [Intrinsic] BCL body's SIMD/Unsafe fallback (the body is intrinsic-typed,
                // so Reach already cuts it from the tree).
                if (mrParent is "System.Runtime.Intrinsics.Vector64" or "System.Runtime.Intrinsics.Vector128"
                        or "System.Runtime.Intrinsics.Vector256" or "System.Runtime.Intrinsics.Vector512"
                        or "System.Numerics.Vector")
                {
                    EmitIntrinsic(mrParent, mrName, Sig());
                    return;
                }
                // Non-generic parent: a real loaded BCL method (CoreLib via -r)
                // is transpiled; intrinsic-mapped types fall through to the
                // intrinsic table.
                if (_c.TryResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context) is { } real)
                {
                    // The framework EventSource tracing guards, reached as a
                    // MemberReference too: ConcurrentDictionary lives in
                    // System.Collections.Concurrent.dll (pulled in via --auto-ref),
                    // so its AcquireAllLocks/GrowTable guards reference
                    // EventSource.IsEnabled cross-assembly — the MethodDefinition-
                    // branch lowering above never sees them. Same folding: the null
                    // Log singleton must never be dereferenced (GetKeys does execute
                    // at runtime, so the fold must be safe, not merely a dead-code
                    // assumption).
                    // The IsEnabled fold is the descriptor row; the provider fold
                    // behind it stays the non-pure helper (it needs the ClassInfo),
                    // exactly as in the MethodDefinition arm.
                    if (TryEmitMethodDefIntercept(CoreIntrinsics.MdEventSourceIsEnabled, real))
                        return;
                    if (TryEmitEventSourceNoOp(real))
                        return;
                    // Regex.Compile referenced cross-assembly — same null-factory
                    // folding as the MethodDefinition branch above (same row).
                    if (TryEmitMethodDefIntercept(CoreIntrinsics.MdRegexCompileFold, real))
                        return;
                    // Const-folded getters, same lowering as the MethodDefinition
                    // branch. RuntimeFeature.IsDynamicCodeSupported/Compiled arrive
                    // this way: Regex/STJ live outside CoreLib under --auto-ref and
                    // reference the public getters as MemberRefs.
                    // This post-resolution MethodDef-row reference IS the emit
                    // mouth of the MemberRef ConstFolded row (MrConstFoldedGetter
                    // cuts; the two rows share the arm id and the predicate).
                    if (TryEmitMethodDefIntercept(CoreIntrinsics.MdConstFoldedGetter, real))
                        return;
                    EmitManagedCall(real, isCallvirt);
                    return;
                }
                TranslateIntrinsic((MemberReferenceHandle)handle);
                return;
            }
            default:
                throw new NotSupportedException($"Call target kind {handle.Kind} is not supported");
        }
    }

    /// <summary>calli: an indirect call through a function pointer that sits on top
    /// of the stack, with the arguments below it, against a standalone call-site
    /// signature (ECMA-335 III.3.20). This is what a C# function pointer call
    /// (<c>delegate*&lt;...&gt;</c> invoked as <c>fp(args)</c>) lowers to. The
    /// pointer is the raw method address produced by <c>ldftn</c> or any
    /// <c>void*</c>/<c>IntPtr</c>; we reconstruct the C++ function-pointer type from
    /// the signature and call through it. C# function pointers are static, so a
    /// HasThis signature — and varargs/<c>arglist</c> — are carved out.</summary>
    private void TranslateCalli(Instruction insn)
    {
        var handle = SRME.EntityHandle(insn.Token);
        if (handle.Kind != HandleKind.StandaloneSignature)
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: calli without a standalone signature is not supported");
        var sig = _reader.GetStandaloneSignature((StandaloneSignatureHandle)handle)
            .DecodeMethodSignature(_c.SigProvider, _method.Context);
        if (sig.Header.IsInstance)
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: calli with a HasThis signature is not supported (C# function pointers are static)");
        if (sig.Header.CallingConvention == SignatureCallingConvention.VarArgs
            || sig.RequiredParameterCount != sig.ParameterTypes.Length)
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: calli with varargs (arglist) is not supported");

        // calli's target is a raw function pointer — statically unprovable — so for
        // [HotPath(NoAlloc)] verification it counts as dynamic dispatch. The emitted call
        // (a cast through a reconstructed fn-ptr type) carries no distinctive runtime token
        // to scan for, so mark this body's facts directly. No-op unless recording is armed.
        _c.NoteHotIndirectCall(_method);

        var ftn = Pop(); // ECMA-335 III.3.20: the function pointer is on top, args below.
        var ps = sig.ParameterTypes;
        var args = new string[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
            args[i] = CoerceTo(Pop(), ps[i], CppTypes.Of(ps[i]));

        string retC = sig.ReturnType.IsVoid ? "void" : CppTypes.Of(sig.ReturnType);
        // The pointer must be spelled with the callee's ABI return, which the backend
        // may know to be narrower than the declaration (IEmitBackend.CalliAbiType).
        string abiRetC = sig.ReturnType.IsVoid ? "void"
            : _backend?.CalliAbiType(_method, sig.ReturnType) ?? retC;
        string fnPtrType = $"{abiRetC} (*)({string.Join(", ", ps.Select(CppTypes.Of))})";
        string call = $"(({fnPtrType})({Cast(ftn, "void*")}))({string.Join(", ", args)})";
        // A plain cast, so the widening takes its direction from the narrow type's
        // signedness rather than from the declaration's.
        if (abiRetC != retC)
            call = $"(({retC}){call})";

        if (sig.ReturnType.IsVoid)
            Emit(call + ";");
        else
            Push(CppTypes.KindOf(sig.ReturnType), retC, call);
    }

    /// <summary>Whether a <c>newobj</c> instruction constructs a delegate type, and if
    /// so which one (<paramref name="dgClass"/>, null when the token would not
    /// resolve). Used to classify a preceding <c>ldftn</c> as a delegate creation vs a
    /// function-pointer use, and — through the class — to tell an open static delegate
    /// from a closed one. A resolution failure conservatively reports <c>true</c> with
    /// an unknown class, so an ambiguous ldftn keeps the pre- delegate-adapter
    /// behavior and existing delegate code can never regress.</summary>
    private bool NewobjTargetIsDelegate(Instruction newobjInsn, out ClassInfo? dgClass)
    {
        dgClass = null;
        try
        {
            var handle = SRME.EntityHandle(newobjInsn.Token);
            MethodInfo? ctor = handle.Kind switch
            {
                HandleKind.MethodDefinition => ResolveMethodDef((MethodDefinitionHandle)handle),
                HandleKind.MemberReference when _reader.GetMemberReference((MemberReferenceHandle)handle).Parent.Kind == HandleKind.TypeSpecification
                    => _c.ResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context),
                HandleKind.MemberReference when _c.TryResolveMemberRefMethod(_module, (MemberReferenceHandle)handle, _method.Context) is { } real
                    => real,
                _ => null,
            };
            if (ctor is null)
                return true;
            if (!ctor.DeclaringClass.IsDelegate)
                return false;
            dgClass = ctor.DeclaringClass;
            return true;
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return true;
        }
    }

    /// <summary>Whether a delegate of <paramref name="dgClass"/> over the static
    /// <paramref name="target"/> BINDS the static's first argument into the delegate's
    /// target slot (a closed static delegate) rather than leaving the slot unused (an
    /// open one). The test is the one the CLR's own delegate binder uses — arity:
    /// <c>Invoke</c> takes exactly one argument fewer than the static, so the static's
    /// first parameter is the argument the target slot carries. It is group-invariant
    /// (canonicalization changes no arity), so a shared canonical body picks the same
    /// shape for every member of its group and needs no taint.
    /// <para>An unresolved delegate class answers "open" — today's behavior for every
    /// site that ever worked. A resolved one has its members decoded already, by the
    /// structural invariant that reaching a method means having resolved it: the class
    /// came out of the delegate <c>newobj</c>'s own ctor resolution, and the only arm
    /// of that resolution which can see a specialization
    /// (<c>Compilation.ResolveMemberRefMethod</c>) completes it. So the
    /// <see cref="ClassInfo.Methods"/> read below decodes nothing that was not owed,
    /// and it passes <c>DN2CPP_STRICT_COMPLETION</c>.</para>
    /// <para>The bound argument arrives through an <c>object</c>-typed slot, so a
    /// value-type first parameter cannot be carried: real .NET rejects exactly that
    /// (<c>Delegate.CreateDelegate</c> over a struct/enum <c>firstArgument</c> throws
    /// <c>ArgumentException</c>) and C# refuses the method-group form outright (CS1113,
    /// "extension method defined on a value type cannot be used to create a
    /// delegate"). So the shape is unreachable rather than unimplemented — fail loudly
    /// instead of emitting a cast that would reinterpret an unboxed value as a
    /// pointer.</para></summary>
    private bool IsClosedStaticDelegate(ClassInfo? dgClass, MethodInfo target)
    {
        var ps = target.Signature.ParameterTypes;
        if (ps.Length == 0 || dgClass is null)
            return false;
        if (dgClass.Methods.FirstOrDefault(m => m.Name == "Invoke") is not { } invoke
            || invoke.Signature.ParameterTypes.Length != ps.Length - 1)
            return false;
        if (CppTypes.KindOf(ps[0]) != StackKind.Ref)
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: a delegate closed over the "
                + $"first argument of static {target.DeclaringClass.FullName}.{target.Name} is not "
                + $"supported — that argument is the value type {ps[0]}, which the delegate's "
                + "object-typed target slot cannot carry (real .NET rejects it too).");
        return true;
    }

    /// <summary>Resolves a MethodDef token, including self-references inside a
    /// generic specialization (which point at the open template method).</summary>
    private MethodInfo ResolveMethodDef(MethodDefinitionHandle handle)
    {
        if (_method.DeclaringClass.GenericArity > 0
            && _method.DeclaringClass.MethodByTemplate.TryGetValue(handle, out var inst))
            return inst;
        return _c.GetMethod(_module, handle);
    }

    // FileOptions.Asynchronous / FILE_FLAG_OVERLAPPED — the bit that steers the win-x64
    // CoreLib onto its overlapped/IOCP-backed file I/O. dn2cpp's runtime implements no
    // IOCP: the whole family (ThreadPoolBoundHandle.BindHandleCore,
    // OverlappedValueTaskSource, RandomAccess.*SyncUsingAsyncHandle, ...) is bounded on
    // the standing assumption that only the SYNCHRONOUS FileStream strategy ever runs.
    // Unix's CoreLib reaches that path unconditionally; Windows does not.
    //
    // So the choice is pinned to the synchronous strategy at the two decision points that
    // read the async flag, keeping the handle side and the strategy side consistent (a
    // sync strategy over an overlapped handle would still route reads through the dead
    // RandomAccess.*SyncUsingAsyncHandle path): SafeFileHandle.Open (masking the flag out
    // of `options` opens a plain synchronous handle and makes get_IsAsync report false)
    // and FileStreamHelpers.ChooseStrategyCore (masking `options` / zeroing `isAsync`).
    // AsyncWindowsFileStreamStrategy stays statically reachable but runtime-dead.
    //
    // This fires on EVERY platform. On Unix it changes no I/O path, but it is NOT
    // invisible: FileStream.IsAsync reports false everywhere while the user's own
    // FileStreamOptions.Options still reads back Asynchronous (only the call-site
    // expression is rewritten, never the user's object), and CoreLib's ValidateHandle
    // throws ArgumentException for `new FileStream(handle, …, isAsync: true)`, which real
    // .NET accepts. Documented carve-outs; ReadAsync/WriteAsync results are unaffected.
    private void NeutralizeFileStreamAsyncArgs(MethodInfo callee)
    {
        string dt = callee.DeclaringClass.FullName;
        bool isChoose = dt == "System.IO.Strategies.FileStreamHelpers" && callee.Name == "ChooseStrategyCore";
        bool isOpen = dt == "Microsoft.Win32.SafeHandles.SafeFileHandle" && callee.Name == "Open";
        if (!isChoose && !isOpen)
            return;
        // Both targets are static: their N declared parameters are exactly the top N
        // stack entries, with parameter i at _stack[_stack.Count - N + i].
        var ps = callee.Signature.ParameterTypes;
        int baseIdx = _stack.Count - ps.Length;
        if (baseIdx < 0)
            return;
        for (int i = 0; i < ps.Length; i++)
        {
            var p = ps[i];
            int slot = baseIdx + i;
            bool isFileOptions =
                (p.Kind == TypeKind.Class && p.Class is { FullName: "System.IO.FileOptions" })
                || (p.Kind == TypeKind.External && p.ExternalName == "System.IO.FileOptions");
            if (isFileOptions)
            {
                // FileOptions.Asynchronous == 0x40000000. Clear it (enums are erased
                // to their int32 storage on the stack).
                var e = _stack[slot];
                _stack[slot] = e with { Expr = $"(({e.Expr}) & ~0x40000000)" };
            }
            else if (isChoose && p is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean })
            {
                // ChooseStrategyCore(SafeFileHandle, FileAccess, bool isAsync): force sync.
                var e = _stack[slot];
                _stack[slot] = e with { Expr = "0" };
            }
        }
    }

    /// <summary>The BASE <c>Stream.ReadAsync</c>/<c>WriteAsync</c> bodies funnel through two
    /// private methods — <c>BeginEndReadAsync</c> / <c>BeginEndWriteAsync</c> — whose real
    /// implementations reach the APM / ReadWriteTask scheduler machinery dn2cpp does not
    /// model, and which are therefore bounded. Rewrite the funnel instead of letting the
    /// bounded default stand: dispatch synchronously through the <c>Read</c>/<c>Write</c>
    /// slot the subclass overrides and hand back an already-completed Task. Sync-over-async
    /// is exactly what the machinery being replaced amounts to once its scheduler is taken
    /// out — the ReadWriteTask it builds calls this same virtual <c>Read</c>.
    ///
    /// <para>Why the funnel and not the two public virtuals: the funnel is where the
    /// untranspilable subtree begins, so rewriting it leaves the real IL of
    /// <c>ReadAsync</c>/<c>WriteAsync</c> in place — argument validation and the
    /// pre-canceled-token early-out (<c>Task.FromCanceled</c>) keep .NET's semantics for
    /// free instead of being re-implemented here. And it is <c>private</c>, so no override
    /// can call it: the rewrite reaches the base slot's body and nothing else, which is what
    /// keeps FileStream / MemoryStream / DeflateStream — all of which override the whole
    /// async surface — dispatching to their own overrides.</para>
    ///
    /// <para>The bounded default it replaces was a <b>null Task</b>, and awaiting one is a
    /// nullptr dereference. Any Stream subclass overriding only the abstract sync
    /// <c>Read</c>/<c>Write</c> — the shape of every wrapper and decorator stream — inherits
    /// the base slot, so this was reachable from ordinary user code.</para></summary>
    private bool TryEmitStreamSyncOverAsyncFunnel(MethodInfo callee)
    {
        if (!CoreIntrinsics.BdStreamSyncFunnel.Matches(callee.DeclaringClass.FullName, callee.Name))
            return false;
        bool read = callee.Name == "BeginEndReadAsync";
        var slot = Compilation.StreamSyncSlot(callee.DeclaringClass, read)
            ?? throw new NotSupportedException(
                $"System.IO.Stream::{callee.Name}: the synchronous "
                + $"{(read ? "Read" : "Write")}(byte[], int, int) slot that the base async "
                + "surface is rewritten onto is not present in this CoreLib");
        // The funnel's parameter list IS the sync slot's — (this, buffer, offset, count) —
        // so the operands already on the stack are the dispatch's arguments unchanged, and
        // the ordinary virtual-call emission below takes them as they stand.
        EmitManagedCall(slot, isCallvirt: true);
        if (read)
        {
            // Task<int>: the byte count the sync Read returned, in a completed task.
            var n = Pop();
            PushStampedTask($"dn2cpp_task_from_result({EmitTaskResultStore(n)})", callee.Signature.ReturnType);
        }
        else
        {
            // Task: Write returns void, so the completed task carries no result.
            PushStampedTask("dn2cpp_task_completed()", callee.Signature.ReturnType);
        }
        return true;
    }

    private void EmitManagedCall(MethodInfo callee, bool isCallvirt)
    {
        // Target-specific call intrinsics (e.g. Godot shim methods) get first
        // refusal; the core emitter itself has no target knowledge.
        if (_intrinsics is not null && _intrinsics.TryEmitCall(this, callee, isCallvirt))
            return;

        if (callee.DeclaringClass.IsDelegate && callee.Name == "Invoke")
        {
            TranslateDelegateInvoke(callee);
            return;
        }

        // VectorMath.Min/Max<...>(vector, vector): the SIMD element-wise min/max. Its
        // body dispatches ISimdVector<...>.LessThan — a hardware InternalCall dn2cpp does
        // not generate (the permanent SIMD carve-out). The BCL only reaches it from the
        // Vector128/256-accelerated fast paths in Enumerable.Min/Max (the IMinMaxCalc<T>
        // vector Compare overloads), all guarded by Vector*.IsHardwareAccelerated — folded
        // to false here, so those blocks are runtime-dead. Return a zero vector so the dead
        // code links (its result is never observed); the body is cut from reachability.
        if (callee.DeclaringClass.FullName == "System.Runtime.Intrinsics.VectorMath"
            && callee.Name is "Min" or "Max")
        {
            PopArgs(callee, hasThis: false);
            var vrt = callee.Signature.ReturnType;
            string vrct = CppTypes.Of(vrt);
            string vtmp = NewTemp(vrct);
            string vzero = CppTypes.ZeroInitExpr(vrct);
            Emit($"{vtmp} = {vzero};");
            Push(CppTypes.KindOf(vrt), vrct, vtmp);
            return;
        }

        // Comparer<T>.Default -> a freshly allocated GenericComparer<T>. The
        // real getter reflects (CreateInstanceForAnotherGenericParameter); emit the
        // concrete comparer instead so it can be stored and dispatched virtually
        // (SortedDictionary's key comparer). Its Compare uses x.CompareTo(y).
        if (_c.IsComparerGetDefault(callee)
            && _c.GenericComparerFor(callee.DeclaringClass.Context.TypeArgs[0]) is { } gc
            && Compilation.ParameterlessCtor(gc) is { } gctor)
        {
            // The synthesized comparer is stamped with the closed element's
            // GenericComparer type-info — for a placeholder element the shared
            // body reads the real instantiation's comparer type-info out of an
            // rgctx slot keyed on the get_Default call token (the layout and
            // ctor are group-shared; only the identity stamp varies).
            string gcTi = "&" + gc.CppTypeInfoName;
            if (SharedTrial && Compilation.ContainsCanonPlaceholder(callee.DeclaringClass.Context.TypeArgs[0]))
            {
                if (!CallTokenResolvesTo(callee))
                    ThrowSharedTaint("comparer-default", callee.DeclaringClass.FullName);
                gcTi = "(const Dn2CppTypeInfo*)" + RgctxSlotAccess(
                    RgctxSlotKind.ComparerDefault, _callSiteToken, "comparer-default",
                    callee.DeclaringClass.FullName);
            }
            string o = NewTemp(gc.CppStructName + "*");
            Emit($"{o} = ({gc.CppStructName}*)dn2cpp_alloc(sizeof({gc.CppStructName}));");
            Emit($"((Dn2CppObject*){o})->type = {gcTi};");
            Emit($"{DirectCallSym(gctor)}({ArgsWithRgctx(o, gctor)});");
            Push(StackKind.Ref, gc.CppStructName + "*", o);
            return;
        }

        // StringComparer.CurrentCulture/InvariantCulture -> a freshly allocated ordinal
        // GenericComparer<string> (see IsStringComparerCultureGetter). dn2cpp has no
        // culture support, so culture-sensitive string ordering becomes ordinal, and the
        // CultureAwareComparer -> CompareInfo -> ICU subtree stays unreachable. The result
        // is only ever used as IComparer<string> (the EnumerableSorter sort path).
        if (_c.IsStringComparerCultureGetter(callee)
            && _c.GenericComparerFor(TypeDesc.MakePrimitive(PrimitiveTypeCode.String)) is { } sgc
            && Compilation.ParameterlessCtor(sgc) is { } sgctor)
        {
            string o = NewTemp(sgc.CppStructName + "*");
            Emit($"{o} = ({sgc.CppStructName}*)dn2cpp_alloc(sizeof({sgc.CppStructName}));");
            Emit($"((Dn2CppObject*){o})->type = &{sgc.CppTypeInfoName};");
            Emit($"{DirectCallSym(sgctor)}({ArgsWithRgctx(o, sgctor)});");
            Push(StackKind.Ref, sgc.CppStructName + "*", o);
            return;
        }

        // Hardware-intrinsic / SIMD feature gates. The portable vector types
        // (System.Runtime.Intrinsics.Vector64/128/256/512<T> and System.Numerics.Vector /
        // Vector<T>) front the vectorized Enumerable.Sum/Min/Max/Average / BitArray / STJ
        // fast paths (which would otherwise read Vector<T>.Count — a throwing SIMD stub).
        // Their IsSupported / IsHardwareAccelerated getters emit the DN2CPP_SIMD_HW_ACCEL
        // token: 0 in the scalar build (the BCL takes its software fallback — the permanent
        // SIMD carve-out), 1 under the opt-in Highway backend (the fast paths run against
        // the SIMD-backed vec ops). The platform-specific intrinsics
        // (System.Runtime.Intrinsics.X86/.Arm/.Wasm — Sse/Avx/Lzcnt/AdvSimd/PackedSimd/…)
        // have no lowering, so they stay folded to 0 either way. The real getters are
        // [Intrinsic] stubs that transpile to an infinite self-recursive call (JIT would
        // replace them), so calling one stack-overflows; folding the gate also lets e.g.
        // BitOperations.Log2 reach its De Bruijn fallback. The vector fast-path code after
        // the gate stays emitted (live or dead per the token).
        //
        // Inside the System.Linq assembly the gate emits the separate
        // DN2CPP_SIMD_HW_ACCEL_LINQ token — an independent lever, backend-selected now
        // that trap-exact checked arithmetic keeps the software fallback scalar (see
        // runtime/core/dn2cpp_vectors.h). Both builds emit the same token, so the
        // self-host text fixpoint is preserved.
        if (callee.IsStatic
            && callee.Name is "get_IsSupported" or "get_IsHardwareAccelerated")
        {
            string simdNs = callee.DeclaringClass.Namespace;
            if (simdNs == "System.Runtime.Intrinsics" || simdNs == "System.Numerics")
            {
                // Vector*<T>.IsSupported is per-element: real .NET reports false
                // outside the public supported set (decimal, Int128, …), and an
                // unsupported closed instantiation is not intrinsic-mapped — its
                // other members are real throwing stubs — so its gate folds to
                // constant 0 to keep e.g. Enumerable.Sum<decimal>'s vector arm
                // dead. (Covered by the backend-selected LINQ gate.)
                bool unsupportedElem = callee.Name == "get_IsSupported"
                    && callee.DeclaringClass.GenericArity == 1
                    && callee.DeclaringClass.Context.TypeArgs is [{ } velem]
                    && !VectorElemIsSupported(velem);
                Push(StackKind.I4, "int32_t", unsupportedElem ? "0" : SimdHwAccelToken);
                return;
            }
            if (simdNs.StartsWith("System.Runtime.Intrinsics")
                || callee.DeclaringClass.CppNamePrefix.StartsWith("System.Runtime.Intrinsics."))
            {
                Push(StackKind.I4, "int32_t", "0");
                return;
            }
        }

        // System.Numerics.BitOperations.{TrailingZeroCount, LeadingZeroCount,
        // PopCount, Log2} over a single int/uint/long/ulong argument: lower to the
        // same clang builtins as the IBinaryInteger<T> statics on the primitive
        // integer types, taking the bit width from the argument. BitOperations is
        // BCL-as-IL (not an intrinsic type); inside these methods the ArmBase /
        // X86Base.IsSupported gates fold to 0, so the body would otherwise fall
        // through to a De Bruijn software-table lookup whose getter is an out-of-line
        // cross-TU call. The remaining BitOperations methods (RotateLeft/Right,
        // IsPow2, RoundUpToPowerOf2, the nint/nuint overloads) have no builtin here
        // and keep falling through to BCL-as-IL.
        if (callee.IsStatic
            && callee.DeclaringClass.FullName == "System.Numerics.BitOperations"
            && callee.Name is "TrailingZeroCount" or "LeadingZeroCount" or "PopCount" or "Log2"
            && callee.Signature.ParameterTypes is [{ Kind: TypeKind.Primitive, Primitive: var argPc }]
            && argPc is PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32
                or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64)
        {
            int width = argPc is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64 ? 64 : 32;
            EmitBitCountOp(callee.Name, Pop(), width, "int32_t", StackKind.I4);
            return;
        }

        // IntPtr.Size / UIntPtr.Size / Environment.Is64BitProcess: the 64-bit
        // CoreLib IL constant-folds these getters (ldc.i4.8 / `8 == 8`), which
        // would bake the build machine's pointer width into the generated C++.
        // The output must stay target-neutral (a wasm32 build compiles the same
        // C++), so emit the target's own sizeof instead of calling the folded
        // bodies.
        if (callee.IsStatic && callee.Name == "get_Size"
            && callee.DeclaringClass.FullName is "System.IntPtr" or "System.UIntPtr")
        {
            Push(StackKind.I4, "int32_t", "(int32_t)sizeof(intptr_t)");
            return;
        }
        if (callee.IsStatic && callee.Name == "get_Is64BitProcess"
            && callee.DeclaringClass.FullName == "System.Environment")
        {
            Push(StackKind.I4, "int32_t", "((int32_t)(sizeof(void*) == 8))");
            return;
        }

        // The base Stream async funnels: bounded, but rewritten rather than
        // neutralized. Ahead of the bounded check because the default it would
        // otherwise push is a NULL Task.
        if (TryEmitStreamSyncOverAsyncFunnel(callee))
            return;

        // A bounded method's body is cut at reachability and neutralized at the
        // call site: drop the args and yield the default result. The result (null /
        // zero / nothing-for-void) is semantically correct for our model — e.g. the
        // string-comparer factories' null means "use the default comparer", and
        // JsonSerializerOptions.TrackOptionsInstance's registration into an unread
        // ConditionalWeakTable is unobservable.
        if (_c.IsBoundedMethod(callee.DeclaringClass.FullName, callee.Name))
        {
            // If the callee is a bodyless [DllImport], the bound is not neutralizing a
            // modelled BCL path — it is standing in for a native module this build does
            // not provide, with a zero the caller cannot tell from a real answer. Nothing
            // downstream names it (reachability's Rva == 0 early-out precedes its bounded
            // arm, so no gap row; the cut keeps it out of pinvoke-libs.txt, so no link
            // error), so record it here for the driver's report — see
            // Compilation.NoteBoundedImport for why this is the only site that knows.
            _c.NoteBoundedImport(callee);
            // The receiver/args/result are cast to these classes' C++ types at the
            // call site, so their layouts must at least be declared opaquely.
            if (!callee.DeclaringClass.IsValueType)
                _c.NoteReferencedType(callee.DeclaringClass);
            NoteReferenceClass(callee.Signature.ReturnType);
            NoteLocalValueLayout(callee.Signature.ReturnType);
            PopArgs(callee, hasThis: !callee.IsStatic);
            // The loudness half, mouth 1 of 2 (the other is the ldftn stub in
            // MethodCompiler.cs). A bounded import whose row says Loud does not answer at
            // all: the zero it would push is indistinguishable from a real result, so the
            // site throws a catchable PlatformNotSupportedException naming the module. The
            // value pushed after the [[noreturn]] throw is unreachable and only keeps the
            // eval stack typed — the same shape as the dynamic-codegen arm below.
            //
            // NOT gated on Phase: the recording above is, this is not. See
            // Compilation.TryBoundedImport — the planning pass has to emit the same text the
            // real pass will, or a shared-body verdict is decided about a program that never
            // ships.
            if (_c.TryBoundedImport(callee, out var bimp) && bimp.Verdict == BoundedVerdict.Loud)
            {
                Emit("dn2cpp_throw_platform_not_supported(\""
                    + Compilation.BoundedImportThrowMessage(bimp) + "\");");
            }
            if (!callee.Signature.ReturnType.IsVoid)
            {
                string ct = CppTypes.Of(callee.Signature.ReturnType);
                string zero = CppTypes.ZeroInitExpr(ct);
                Push(CppTypes.KindOf(callee.Signature.ReturnType), ct, zero);
            }
            return;
        }

        // The dynamic-code-generation surface: the body is cut at reachability
        // (see the Reach twin) and the call site throws a catchable
        // PlatformNotSupportedException naming the member. Unlike a bounded
        // method this is a real unsupported-feature error, so it must fail
        // loudly rather than yield a default; the value pushed after the
        // [[noreturn]] throw is unreachable and only keeps the eval stack typed.
        if (_c.IsDynamicCodegenMember(callee.DeclaringClass, callee.Name))
        {
            if (!callee.DeclaringClass.IsValueType)
                _c.NoteReferencedType(callee.DeclaringClass);
            NoteReferenceClass(callee.Signature.ReturnType);
            NoteLocalValueLayout(callee.Signature.ReturnType);
            PopArgs(callee, hasThis: !callee.IsStatic);
            Emit($"dn2cpp_throw_platform_not_supported(\"{_c.GenericDefFullName(callee.DeclaringClass)}.{callee.Name}"
                + " requires dynamic code generation\");");
            if (!callee.Signature.ReturnType.IsVoid)
            {
                string ct = CppTypes.Of(callee.Signature.ReturnType);
                string zero = CppTypes.ZeroInitExpr(ct);
                Push(CppTypes.KindOf(callee.Signature.ReturnType), ct, zero);
            }
            return;
        }

        // The absent socket / name-resolution platform layer: same shape as the
        // dynamic-codegen arm above (body cut at reachability, call site throws), and loud
        // for the stronger of the two reasons — a caller that wanted a connected socket
        // would dereference the zero, and a body admitted here would not even reach a wrong
        // answer, it would fail at C++ link time on a libSystem.Native symbol nothing
        // defines. See Compilation.IsAbsentNetworkPalMember.
        if (_c.IsAbsentNetworkPalMember(callee.DeclaringClass, callee.Name))
        {
            if (!callee.DeclaringClass.IsValueType)
                _c.NoteReferencedType(callee.DeclaringClass);
            NoteReferenceClass(callee.Signature.ReturnType);
            NoteLocalValueLayout(callee.Signature.ReturnType);
            PopArgs(callee, hasThis: !callee.IsStatic);
            Emit("dn2cpp_throw_platform_not_supported(\""
                + Compilation.AbsentNetworkPalThrowMessage(
                    _c.GenericDefFullName(callee.DeclaringClass), callee.Name) + "\");");
            if (!callee.Signature.ReturnType.IsVoid)
            {
                string ct = CppTypes.Of(callee.Signature.ReturnType);
                Push(CppTypes.KindOf(callee.Signature.ReturnType), ct, CppTypes.ZeroInitExpr(ct));
            }
            return;
        }

        // AssemblyLoadContext runtime-assembly-load internals: bodyless InternalCall /
        // QCall primitives that create the native ALC, load an IL assembly by path, or
        // resolve an assembly's load context. A transpiled AOT binary resolves all IL at
        // link time and cannot load or register an assembly at run time (mods/plugins are
        // a structural non-goal). Trap loudly with a catchable
        // PlatformNotSupportedException naming the limitation rather than returning a
        // default 0/null that would surface as a silent mod-load failure or a later NPE.
        // Written out (not a bounded default, not the
        // fixed-message dynamic-codegen throw whose prefilter excludes System.Runtime.Loader).
        if (CoreIntrinsics.IsAssemblyLoadContextRuntimeLoad(callee.DeclaringClass.FullName, callee.Name))
        {
            if (!callee.DeclaringClass.IsValueType)
                _c.NoteReferencedType(callee.DeclaringClass);
            NoteReferenceClass(callee.Signature.ReturnType);
            NoteLocalValueLayout(callee.Signature.ReturnType);
            PopArgs(callee, hasThis: !callee.IsStatic);
            Emit("dn2cpp_throw_platform_not_supported(\"System.Runtime.Loader.AssemblyLoadContext: "
                + "loading or resolving an IL assembly at run time is not supported by an "
                + "AOT-transpiled binary (mods/plugins are a structural non-goal)\");");
            if (!callee.Signature.ReturnType.IsVoid)
            {
                string ct = CppTypes.Of(callee.Signature.ReturnType);
                Push(CppTypes.KindOf(callee.Signature.ReturnType), ct, CppTypes.ZeroInitExpr(ct));
            }
            return;
        }

        // AppDomain.GetAssemblies -> AssemblyLoadContext.GetLoadedAssemblies: an AOT binary
        // collapses every input IL module into one image, so there is no run-time-mutable
        // set of loaded assemblies to enumerate. Answer with an empty Assembly[] — never
        // null: CommandRegistry.RegisterCommands iterates it in its ctor at startup, and a
        // null would NPE the process before it runs. Reflection-driven discovery that scans
        // this set finds nothing (a documented degrade, not a crash); a trap here would take
        // out startup on a value-returning query no static analysis can answer.
        // The empty array must carry the DECLARED element identity (a typed Assembly[]
        // via EmitEmptyArray's precise ti_arr_ handle + SZArray static type), never the
        // shared object[] handle: LINQ's `source is Assembly[]` fast path and the
        // IEnumerable<Assembly> boundary both read the runtime type, and an
        // object[]-tagged result correctly REFUSES interface dispatch as
        // IEnumerable<Assembly> — aborting exactly the GetAssemblies().Where(...)
        // startup scan the degrade exists to keep alive.
        if (callee.DeclaringClass.FullName == "System.Runtime.Loader.AssemblyLoadContext"
            && callee.Name == "GetLoadedAssemblies")
        {
            PopArgs(callee, hasThis: !callee.IsStatic);
            if (callee.Signature.ReturnType is { Kind: TypeKind.SZArray, Element: { } asmElem })
            {
                EmitEmptyArray(asmElem);
                return;
            }
            string rt = CppTypes.Of(callee.Signature.ReturnType);
            Push(StackKind.Ref, rt, $"({rt})dn2cpp_newarr_ref(0)");
            return;
        }

        if (_constrained is { } c && isCallvirt)
        {
            EmitConstrainedCall(callee, c);
            return;
        }

        // constrained. call ISimdVector<TVec,T>::member — the generic SIMD abstraction
        // that Vector64/128/256/512 implement as [Intrinsic] static-abstract members. The
        // constrained TSelf is a concrete vector type, so route the member straight to the
        // software-vector op (the static analogue of the constrained-callvirt vector path
        // in TryEmitValueConstrained). Reached by BCL code generic over ISimdVector — the
        // Ascii transcode SIMD, BitArray's vectorized Not/And/Or — all runtime-dead under
        // the folded SIMD gate but still emitted. The member's first parameter is the
        // vector operand (no `this`), exactly the stack shape the vector ops expect.
        if (_constrained is { Kind: TypeKind.Class, Class: { IntrinsicCppName: { } vicn, IsValueType: true } vICls }
            && !isCallvirt && callee.IsStatic && callee.DeclaringClass.IsInterface
            && vicn.StartsWith("Dn2CppVector", StringComparison.Ordinal) && vICls.Context.TypeArgs.Length == 1
            && TryEmitVectorOp(vicn, VecWidthBytes(vicn), vICls.Context.TypeArgs[0], callee.Name, callee.Signature, callee.Context.MethodArgs))
            return;

        // constrained. + `call` (not callvirt) to a static interface method = a
        // static-virtual interface dispatch (.NET 7+ static abstract members). A
        // canonical-shared TSelf (the reference placeholder, or a placeholder-bearing
        // reference class) has no statically resolvable impl: the implementing static
        // differs per real instantiation and there is no rgctx dispatch mechanism for
        // static virtuals, so taint the shared trial — each grouped user then compiles
        // its own body with TSelf concrete. Width-preserving integer placeholders are
        // deliberately not tainted: their members lower through the generic-math table
        // below, which is instantiation-independent (an enum's operators are its
        // underlying primitive's).
        if (!isCallvirt && callee.IsStatic && callee.DeclaringClass.IsInterface && SharedTrial
            && (_constrained is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object, IsCanonPlaceholder: true }
                || (_constrained is { Kind: TypeKind.Class, Class.IsValueType: false }
                    && Compilation.ContainsCanonPlaceholder(_constrained))))
            ThrowSharedTaint("static-virtual", callee.DeclaringClass.FullName + "." + callee.Name);

        // The constrained type is the concrete TSelf; resolve to its implementing
        // static method and call it directly. This covers value-type structs — e.g.
        // Linq's IMinMaxCalc<T> Min/Max comparer structs, whose Compare devirtualizes
        // to `left < right` (itself generic-math-lowered), and SearchValues'
        // IResultMapper<T,int>.FirstIndex<TVector> (a generic member, whose closed
        // callee re-opens to match the struct's open template) — concrete reference
        // classes implementing an operator interface (their resolved impl is a real
        // static body, reached alongside by the reach-pass twin) — and, via
        // ConstrainedStaticVirtualSelf, TSelf closed to double/float, whose math
        // statics (Sqrt, Sin, …) resolve to the Double/Single intrinsic surface here;
        // other primitives are handled by the generic-math intrinsic below (it reads
        // the closed operand/return types). The member may also carry a default body
        // (`static virtual` with an implementation): the struct's override still wins
        // when present — only a struct providing no body falls through to the direct
        // call of the interface's own default below.
        if (!isCallvirt && callee.IsStatic && callee.DeclaringClass.IsInterface
            && ConstrainedStaticVirtualSelf() is { } scc)
        {
            // For a primitive TSelf the generic-math table keeps priority:
            // Double/Single and the integer structs publish their operator impls
            // under the plain op_* metadata names, so the resolver below would find
            // them and route to per-type intrinsic names the tables don't carry,
            // while the generic-math table already lowers the operators, constants,
            // predicates and TryConvert* shapes from the interface side. Only table
            // misses — the math-function statics (Sqrt, Sin, MaxMagnitude, …) and
            // the Parse/TryParse family — resolve against the struct.
            if (_constrained is { Kind: TypeKind.Primitive } && TryEmitGenericMathIntrinsic(callee))
                return;
            if (_c.ResolveStaticVirtualImpl(scc, callee) is { } simpl)
            {
                // The resolved impl may live on an intrinsic-mapped type — e.g. TSelf =
                // decimal closes IAdditionOperators<TSelf,…>::op_Addition to
                // Decimal.op_Addition, whose real IL body Reach() cuts and is never
                // emitted, so a direct call would name a dangling function. Route it
                // through the intrinsic lowering instead (which pops its own operands),
                // mirroring the intra-assembly MethodDefinition dispatch above. The
                // sub-word integers are not intrinsic types, but their inline-lowered
                // member set (the NumberStyles-engine Parse/TryParse and the format
                // family) has its real bodies reach-cut, so those route the same way;
                // any other sub-word member direct-calls its real transpiled body
                // (which the reach-pass twin pulls into the tree). A table miss is
                // NOT loud here: it falls through to the later stages in order — the
                // member's own default interface body (Rva != 0), the bodyless
                // intrinsics, then the precise no-intrinsic-mapping diagnostic.
                if (CoreIntrinsics.IsIntrinsicType(simpl.DeclaringClass.FullName)
                    || simpl.DeclaringClass.IntrinsicCppName is not null
                    || CoreIntrinsics.IsInlineLoweredPrimitiveMember(simpl.DeclaringClass.FullName, simpl.Name))
                {
                    if (TryEmitIntrinsic(IntrinsicDispatchName(simpl.DeclaringClass), simpl.Name, simpl.Signature))
                        return;
                }
                else
                {
                    var sargs = PopArgs(callee, hasThis: false);
                    EmitCallResult(simpl, DirectCall(simpl, sargs));
                    EmitByRefSlotFixups();
                    return;
                }
            }
        }

        // constrained. + call to a static interface member carrying a default body,
        // with TSelf closed to a primitive: prefer the generic-math intrinsic lowering
        // over transpiling the default impl. The op_Checked* operators' interface
        // defaults forward to the unchecked operator — the concrete primitives override
        // them with trapping bodies real .NET dispatches to, so transpiling the default
        // would silently drop the overflow trap. Bodyless members reach the same table
        // through the bodyless-intrinsic dispatch below; a member the table doesn't
        // cover still falls through to its default body.
        if (_constrained is { Kind: TypeKind.Primitive } && !isCallvirt
            && callee.IsStatic && callee.DeclaringClass.IsInterface && callee.Rva != 0
            && TryEmitGenericMathIntrinsic(callee))
            return;

        // IUtfChar<TSelf>.CastFrom(uint) — the static-abstract "build a UTF code unit
        // from a uint". On the closed TSelf (byte for UTF-8, char/ushort for UTF-16)
        // it is a width cast. The interface method is an [Intrinsic] InternalCall with
        // no IL, reached from Number.*ToDecChars / the Utf8Formatter number path
        // (JSON value escaping). TSelf is a primitive here, so the constrained
        // static-impl block above (value-type structs only) doesn't catch it.
        // CastToUInt32(TSelf) is the inverse width cast (Guid.TryParseGuid's
        // hex-digit reads); CastToUInt64 rides along for symmetry.
        if (callee.IsStatic && callee.Name is "CastFrom" or "CastToUInt32" or "CastToUInt64"
            && _c.GenericDefFullName(callee.DeclaringClass) == "System.IUtfChar"
            && callee.DeclaringClass.Context.TypeArgs is [{ } utfSelf])
        {
            var value = Pop();
            if (callee.Name == "CastFrom")
            {
                string selfCpp = CppTypes.StorageOf(utfSelf);
                var kind = CppTypes.KindOf(utfSelf);
                string stackCpp = CppTypes.DefaultForKind(kind);
                Push(kind, stackCpp, $"(({stackCpp})({selfCpp})({value.Expr}))");
            }
            else if (callee.Name == "CastToUInt32")
            {
                Push(StackKind.I4, "uint32_t", $"((uint32_t)({value.Expr}))");
            }
            else
            {
                Push(StackKind.I8, "uint64_t", $"((uint64_t)({value.Expr}))");
            }
            return;
        }

        // INumberBase<TSelf>::TryConvertTo{Checked,Saturating,Truncating}<Half>
        // for a signed-primitive TSelf — the generic-math cross-conversion
        // fallback in Half.Create{Checked,Saturating,Truncating}'s second arm
        // (reached from Number.NumberToFloatingPointBits on the transpiled Half
        // parse path, and from direct Half.CreateChecked<int>(…)-style calls).
        // TSelf is a primitive whose intrinsic-typed impl body is cut, so fold
        // the closed instantiation like the IUtfChar block above: in the BCL's
        // ConvertFrom/ConvertTo split, Half.TryConvertFrom handles every signed
        // source (double/float/sbyte/short/int/long/nint) and always wins first,
        // so a signed TSelf's own TryConvertTo<Half> returns false with a
        // default-initialized out. An unsigned TSelf (whose TryConvertTo really
        // converts to Half) stays a loud failure below.
        if (callee.IsStatic
            && callee.Name is "TryConvertToSaturating" or "TryConvertToChecked" or "TryConvertToTruncating"
            && _c.GenericDefFullName(callee.DeclaringClass) == "System.Numerics.INumberBase"
            && callee.DeclaringClass.Context.TypeArgs is
                [{ Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double or PrimitiveTypeCode.Single
                    or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.Int32
                    or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.IntPtr }]
            && callee.Context.MethodArgs is [{ Kind: TypeKind.Class, Class.FullName: "System.Half" }])
        {
            var cvOut = Pop();  // out TOther (ref Half)
            Pop();              // the TSelf value operand
            Emit($"*(uint16_t*)({cvOut.Expr}) = 0;");
            Push(StackKind.I4, "int32_t", "0");
            return;
        }

        // IBinaryIntegerParseAndFormatInfo<TSelf> — the static-abstract per-type
        // parse constants and width helpers behind Number.TryParseBinaryInteger*
        // (Guid's hex parsing reaches them on uint/ushort/byte). TSelf is a
        // primitive whose intrinsic-typed impl bodies are cut, so constant-fold
        // per closed TSelf here, same shape as the IUtfChar block above. An
        // unrecognized member falls through to the loud no-mapping error.
        if (callee.IsStatic
            && _c.GenericDefFullName(callee.DeclaringClass) == "System.IBinaryIntegerParseAndFormatInfo"
            && callee.DeclaringClass.Context.TypeArgs is [{ Kind: TypeKind.Primitive, Primitive: var ipCode } ipSelf])
        {
            (bool Signed, int Bits, int MaxDigits, string MaxDiv10, string OverflowNoun)? pf = ipCode switch
            {
                PrimitiveTypeCode.SByte => (true, 8, 3, "12", "a signed byte"),
                PrimitiveTypeCode.Byte => (false, 8, 3, "25", "an unsigned byte"),
                PrimitiveTypeCode.Int16 => (true, 16, 5, "3276", "an Int16"),
                PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => (false, 16, 5, "6553", "a UInt16"),
                PrimitiveTypeCode.Int32 => (true, 32, 10, "214748364", "an Int32"),
                PrimitiveTypeCode.UInt32 => (false, 32, 10, "429496729", "a UInt32"),
                PrimitiveTypeCode.Int64 => (true, 64, 19, "922337203685477580LL", "an Int64"),
                PrimitiveTypeCode.UInt64 => (false, 64, 20, "1844674407370955161ULL", "a UInt64"),
                PrimitiveTypeCode.IntPtr => (true, 64, 19, "922337203685477580LL", "an IntPtr"),
                PrimitiveTypeCode.UIntPtr => (false, 64, 20, "1844674407370955161ULL", "a UIntPtr"),
                _ => null,
            };
            if (pf is { } p)
            {
                string selfCt = CppTypes.StorageOf(ipSelf);
                string uct = p.Bits switch { 64 => "uint64_t", 32 => "uint32_t", 16 => "uint16_t", _ => "uint8_t" };
                var selfKind = CppTypes.KindOf(ipSelf);
                string selfStackCt = CppTypes.DefaultForKind(selfKind);
                switch (callee.Name)
                {
                    case "get_IsSigned":
                        Push(StackKind.I4, "int32_t", p.Signed ? "1" : "0");
                        return;
                    case "get_MaxDigitCount":
                        Push(StackKind.I4, "int32_t", p.MaxDigits.ToString());
                        return;
                    case "get_MaxHexDigitCount":
                        Push(StackKind.I4, "int32_t", (p.Bits / 4).ToString());
                        return;
                    case "get_MaxValueDiv10":
                        Push(selfKind, selfStackCt, $"(({selfStackCt})({selfCt}){p.MaxDiv10})");
                        return;
                    case "get_OverflowMessage":
                        Push(StackKind.Ref, "Dn2CppString*",
                            _literals.GetOrAdd($"Value was either too large or too small for {p.OverflowNoun}."));
                        return;
                    case "IsGreaterThanAsUnsigned":
                    {
                        var right = Pop();
                        var left = Pop();
                        Push(StackKind.I4, "int32_t",
                            $"((({uct})({left.Expr})) > (({uct})({right.Expr})) ? 1 : 0)");
                        return;
                    }
                    // Unchecked wraparound in TSelf's width: multiply in the
                    // unsigned counterpart (signed overflow is UB in C++), then
                    // narrow back to TSelf.
                    case "MultiplyBy10" or "MultiplyBy16":
                    {
                        var value10 = Pop();
                        string factor = callee.Name == "MultiplyBy10" ? "10" : "16";
                        Push(selfKind, selfStackCt,
                            $"(({selfStackCt})({selfCt})((({uct})({value10.Expr})) * ({uct}){factor}))");
                        return;
                    }
                }
            }
        }

        // A handful of InternalCall BCL methods reached by state-machine
        // plumbing have no IL body but a trivial single-threaded meaning.
        if (callee.Rva == 0 && TryEmitBodylessIntrinsic(callee))
            return;

        // A P/Invoke with a registered managed native implementation
        // ([Dn2Cpp.Runtime.NativeImplementation] in any loaded assembly): lower the
        // call to a direct call to that managed method instead of a native call, so
        // the native symbol is never referenced or linked. Checked before the native
        // lowering below so it applies to app-module DllImports and runtime-provided
        // modules alike (and rescues an otherwise-unlinkable module). Inside the
        // implementation's own body a call to the same import falls through to the
        // native path — no self-recursion, and a deliberate escape hatch for an
        // implementation that wraps rather than replaces the native call.
        if (callee.Rva == 0 && callee.PInvoke is { } mnpinv
            && _c.NativeImplFor(mnpinv) is { } mnimpl
            && !ReferenceEquals(mnimpl, _method))
        {
            EmitNativeImplCall(callee, mnimpl);
            return;
        }

        // A P/Invoke (`[DllImport]`) method has no IL body; lower a call to it to a
        // direct native call into its extern "C" entry point. Bounded to the
        // app module — a real CoreLib pulled in with -r keeps its own P/Invokes on the
        // intrinsic/throw boundary (their native symbols are runtime-internal, modeled
        // by hand-written intrinsics, not linked) — EXCEPT the .NET PAL
        // (`libSystem.Native`, the SystemNative_* POSIX wrappers, which the dn2cpp
        // runtime implements itself in runtime/core/platform/posix/) and plain `libc`
        // imports (always-linked platform symbols, e.g. macOS `clonefile`). Those two
        // modules make the real BCL file-I/O bodies (FileStream / SafeFileHandle /
        // RandomAccess behind System.IO.File) transpilable as ordinary native calls.
        // A module the run opted in with `--pinvoke-module` lowers the same way from
        // any loaded assembly — the route for an external binding library pulled in
        // with -r. Which modules lower is Compilation.LowersToDirectNativeCall, the
        // one predicate this route shares with the address-taken (ldftn) path
        // (an address-taken import synthesizes its body from this same lowering),
        // so the two cannot drift.
        if (callee.PInvoke is { } pinv && _c.LowersToDirectNativeCall(callee))
        {
            EmitPInvokeCall(callee, pinv);
            return;
        }

        // A direct call to a body-less method (InternalCall/extern/abstract that
        // wasn't intrinsic-mapped) would link against a missing symbol; surface
        // it as a precise, actionable error instead.
        if (callee.Rva == 0 && !(isCallvirt && (callee.IsVirtual || callee.DeclaringClass.IsInterface)))
            throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: {callee.DeclaringClass.FullName}::{callee.Name} " +
                $"is an InternalCall/extern method with no IL body and no intrinsic mapping [chain: {_c.ReachChain(_method)}]");

        // `using (CancellationTokenSource ...)` / `using (Task ...)` disposal
        // arrives as `callvirt IDisposable::Dispose` on a receiver that is
        // statically an intrinsic type. The intrinsic runtime object's type-info
        // carries no interface table, so dynamic interface dispatch would abort
        // at run time — devirtualize to whatever the direct call to that type's
        // Dispose already lowers to (see the direct-call intrinsics) instead of
        // emitting an unresolvable interface dispatch. Task's is a no-op (the object
        // is GC-managed); the source's disarms its pending CancelAfter timer, and
        // this — not the direct-call arm — is the path a `using` takes, which is how
        // a CancellationTokenSource is normally scoped.
        if (isCallvirt && callee.Name == "Dispose"
            && callee.DeclaringClass.FullName == "System.IDisposable"
            && _stack.Count > 0 && _stack[^1].CppType is "Dn2CppCancelSource*" or "Dn2CppTask*")
        {
            bool disposingCts = _stack[^1].CppType == "Dn2CppCancelSource*";
            var recv = Pop(); // this
            if (disposingCts)
                Emit($"dn2cpp_cts_dispose((Dn2CppCancelSource*)({recv.Expr}));");
            return;
        }

        var args = PopArgs(callee, hasThis: !callee.IsStatic);

        // ECMA-335 attaches the null test to `callvirt`, not to instance calls in
        // general, and this is where it is easy to lose: the C# compiler emits
        // `callvirt` for EVERY instance call on a reference type precisely to get
        // that test, and the four arms below then devirtualize most of them to a
        // direct call. Without the guard a null receiver reaches the callee as
        // nullptr and dies with a SIGSEGV naming nothing, where .NET raises a
        // catchable NRE that an engine trampoline can log.
        //
        // Guarding HERE rather than in the callee prologue is what makes the
        // `call` / `callvirt` distinction expressible: a plain `call` on a null
        // receiver is defined NOT to throw at the call site, and its body then
        // faults on the first field access — which the FieldAccess guard turns
        // into the same NRE .NET raises, at the same instruction .NET raises it.
        // A prologue check would throw for both and would fire on a body that
        // never touches `this`.
        //
        // A value-type declaring class is skipped: its receiver is a managed
        // pointer to storage, not an object reference (a `constrained.` callvirt
        // arrives through EmitConstrainedCall, which forms its own receiver).
        //
        bool defaultComparerReceiver = callee.DeclaringClass.FullName == "System.Collections.IEqualityComparer"
            && callee.Name is "GetHashCode" or "Equals";
        if (isCallvirt && !callee.IsStatic && !callee.DeclaringClass.IsValueType
            && args.Count > 0)
            args[0] = $"dn2cpp_null_check({args[0]})";

        string call;
        if (isCallvirt && Compilation.IsGvmCall(callee))
        {
            // A generic virtual method has no vtable slot; dispatch through its
            // dedicated type-switch dispatcher (emitted by CppEmitter for every used
            // GVM instantiation). Args already include the receiver in slot 0.
            // Checked before the interface branch: an interface-declared GVM
            // (IOrderedEnumerable<T>.CreateOrderedEnumerable<TKey> behind
            // Enumerable.ThenBy) has no interface-table slot either — the plain
            // interface dispatch would index the slots array at its unassigned -1.
            // A shared body's dispatcher name would carry placeholder-mangled
            // method arguments and its case set is per-instantiation — always
            // instantiation-dependent in this slice.
            if (SharedTrial)
                ThrowSharedTaint("gvm", callee.DeclaringClass.FullName + "." + callee.Name);
            if (callee.DeclaringClass.IsInterface && callee.DeclaringClass.IntrinsicCppName is null)
                NoteReferencedType(callee.DeclaringClass);
            call = $"{Compilation.GvmDispatchName(callee)}({string.Join(", ", args)})";
        }
        else if (isCallvirt && callee.DeclaringClass.IsInterface)
        {
            // The call names the interface's ti_ (for dn2cpp_resolve_interface) and casts
            // the receiver to its struct pointer in FnPtrType. An interface specialization
            // reached only through this site — e.g. IEquatable<Byte> from a generic
            // EqualityComparer's `((IEquatable<T>)x).Equals(y)` — is never otherwise emitted,
            // so note it for (opaque) emission, else both names dangle. (The actual dispatch
            // resolves against the receiver type's own interface table.)
            if (callee.DeclaringClass.IntrinsicCppName is null)
                NoteReferencedType(callee.DeclaringClass);
            NoteCanonicalItfDispatch(callee.DeclaringClass);
            NoteDispatchSignatureTypes(callee);
            NfiWrapErasedCallArgs(callee, args);
            string fnPtrType = FnPtrType(callee);
            string receiver = args[0];
            call = $"(({fnPtrType})(dn2cpp_resolve_interface(((Dn2CppObject*){receiver})->type, &{ItfDispatchTi(callee.DeclaringClass).CppTypeInfoName})[{callee.VtableSlot}]))({string.Join(", ", args)})";
            // Tuple passes EqualityComparer<object>.Default through the non-generic
            // interface. Its opaque runtime identity has no interface map, so use the
            // boxed-object default operations only for that identity; user null already
            // failed the callvirt null check above.
            if (defaultComparerReceiver)
            {
                string dflt = callee.Name == "GetHashCode"
                    ? $"dn2cpp_object_gethashcode((Dn2CppObject*)({args[1]}))"
                    : $"dn2cpp_object_equals((Dn2CppObject*)({args[1]}), (Dn2CppObject*)({args[2]}))";
                call = $"(dn2cpp_is_default_equality_comparer((Dn2CppObject*)({receiver})) ? {dflt} : {call})";
            }
        }
        else if (isCallvirt && callee.IsVirtual
            && CoreIntrinsics.BrEnumInstanceFormat.Matches(callee.DeclaringClass.FullName, callee.Name))
        {
            // An enum's virtual-final ToString(IFormatProvider) / ToString(string,
            // IFormatProvider) / GetTypeCode / CompareTo(object) reached as a callvirt on a
            // System.Enum-typed receiver (EnumConverter.ConvertTo calls `((Enum)value).ToString()`
            // and `.GetTypeCode()`; `e.CompareTo(x)` on a System.Enum reaches
            // Enum::CompareTo here likewise). Enums carry NO C++ vtable (their virtuals dispatch
            // through the type-info tostring slot / runtime helpers), so the vtable[slot] dispatch
            // below would index a null vtable and crash. The body is BodyReplace'd — one
            // synthesized body (CompileEnumInstanceFormatBody) that dispatches on the boxed enum's
            // runtime type, so it is the single correct implementation for every enum — so
            // devirtualize to a direct call to it. The itable forms (`((IFormattable)e).ToString(...)`,
            // `((IComparable)e).CompareTo(...)`) still resolve through the interface branch above
            // (the enum's slot points at this same body); the non-virtual ToString(string) and the
            // Object::ToString/Equals/GetHashCode forms take their own direct/intrinsic routes
            // (Equals/GetHashCode land on dn2cpp_object_* — never on Enum's symbol) and never reach
            // here.
            call = DirectCall(callee, args);
        }
        else if (isCallvirt && callee.IsVirtual)
        {
            // The receiver is cast to the declaring class's struct pointer in FnPtrType,
            // so a reference-only declaring class must at least be opaquely
            // forward-declared, else the cast names an unknown type. E.g.
            // MemoryManager<T> reached only through the array-backed
            // ReadOnlyMemory<T>.Span branch stays an opaque forward-decl; when user
            // code instantiates a subclass, normal reachability emits it fully and
            // these virtual calls (GetSpan/Pin/...) dispatch through its vtable.
            if (!callee.DeclaringClass.IsValueType && callee.DeclaringClass.IntrinsicCppName is null)
                NoteReferencedType(callee.DeclaringClass);
            NoteDispatchSignatureTypes(callee);
            NfiWrapErasedCallArgs(callee, args);
            string fnPtrType = FnPtrType(callee);
            string receiver = args[0];
            call = $"(({fnPtrType})(((Dn2CppObject*){receiver})->type->vtable[{callee.VtableSlot}]))({string.Join(", ", args)})";
        }
        else
        {
            // A trivially-constant body (`ldc; ret`) folds to its literal rather
            // than an out-of-line cross-TU direct call. The args were popped above;
            // discarding them is side-effect-free (see TryFoldTrivialConstBody).
            if (TryFoldTrivialConstBody(callee, out string constLit))
            {
                Push(CppTypes.KindOf(callee.Signature.ReturnType),
                    CppTypes.Of(callee.Signature.ReturnType), constLit);
                return;
            }
            // Direct call: bind to the symbol that actually carries the body —
            // the canonical shared implementation when one is assigned, else
            // the method's own.
            call = DirectCall(callee, args);
        }

        EmitCallResult(callee, call);
        EmitByRefSlotFixups();
    }

    /// <summary>Maps the few body-less (InternalCall) BCL methods that turn up
    /// in compiler-generated state machines to their runtime meaning, so
    /// reachability need not transpile a runtime-internal body.
    /// Returns false (and emits nothing) for anything unmapped.</summary>
    private bool TryEmitBodylessIntrinsic(MethodInfo callee)
    {
        switch (callee.DeclaringClass.FullName, callee.Name)
        {
            // The calling thread's real managed id (main = 1, spawned threads
            // and pool workers get fresh ids). Iterators stamp this into the
            // state machine so GetEnumerator can hand back `this` on the same
            // thread — a cross-thread enumerate now correctly falls back to a
            // clone, matching .NET.
            case ("System.Environment", "get_CurrentManagedThreadId"):
                Push(StackKind.I4, "int32_t", "dn2cpp_thread_managed_id(dn2cpp_thread_current())");
                return true;
            // Environment.ProcessorCount's GetProcessorCount InternalCall (reached from
            // Environment..cctor, itself triggered by ConcurrentDictionary's default
            // concurrency level). The hardware concurrency, clamped to >= 1.
            case ("System.Environment", "GetProcessorCount"):
                Push(StackKind.I4, "int32_t", "dn2cpp_environment_processor_count()");
                return true;
            default:
                return TryEmitGenericMathIntrinsic(callee);
        }
    }

    /// <summary>System.Numerics generic-math interface methods (INumberBase&lt;T&gt;,
    /// IComparisonOperators&lt;…&gt;, …) are [Intrinsic] and bodyless. They are reached
    /// pervasively through the BCL's `ArgumentOutOfRangeException.ThrowIf*` argument
    /// guards (e.g. a collection capacity/index check). Emit the primitive op for a
    /// concrete numeric operand; the static-abstract dispatch already closed T to the
    /// primitive.</summary>
    private bool TryEmitGenericMathIntrinsic(MethodInfo callee)
    {
        if (!callee.DeclaringClass.Namespace.StartsWith("System.Numerics"))
            return false;
        var ps = callee.Signature.ParameterTypes;

        // Nullary numeric constants (INumberBase<T>.Zero/One/..., IMinMaxValue<T>.
        // MinValue/MaxValue, I*Identity): the static-abstract dispatch closed TSelf to
        // the concrete primitive, which is the (substituted) return type. `Sum` seeds
        // its accumulator with `T.Zero`, `Average`/conversions use One, the Range
        // iterator walks with these.
        if (ps.Length == 0 && callee.Signature.ReturnType is { Kind: TypeKind.Primitive } rt)
        {
            string ct = CppTypes.Of(rt);
            bool floatRt = rt.Primitive is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double;
            string? konst = callee.Name switch
            {
                "get_Zero" or "get_AdditiveIdentity" => "0",
                "get_One" or "get_MultiplicativeIdentity" => "1",
                "get_NegativeOne" => "-1",
                // float MinValue is the most-negative finite value (lowest), NOT the
                // smallest positive (numeric_limits::min) — match .NET's TSelf.MinValue.
                // The limits are taken at TSelf's REAL storage width (a sub-word
                // operand rides the stack int32-promoted, whose limits would be the
                // int32 ones — byte.MaxValue is 255, not 2147483647) and re-enter the
                // stack slot through the cast below.
                "get_MinValue" => $"std::numeric_limits<{CppTypes.StorageOf(rt)}>::lowest()",
                "get_MaxValue" => $"std::numeric_limits<{CppTypes.StorageOf(rt)}>::max()",
                // IEEE-754 constants (IFloatingPointIeee754<T>): the Double/Single
                // impls are explicit property impls, invisible to the static-virtual
                // resolver, so lower them from the interface side here. .NET's
                // double.Epsilon (4.94e-324) / float.Epsilon (1.4e-45) are exactly
                // the smallest positive subnormal, i.e. denorm_min.
                "get_NaN" when floatRt => $"std::numeric_limits<{ct}>::quiet_NaN()",
                "get_PositiveInfinity" when floatRt => $"std::numeric_limits<{ct}>::infinity()",
                "get_NegativeInfinity" when floatRt => $"(-std::numeric_limits<{ct}>::infinity())",
                "get_Epsilon" when floatRt => $"std::numeric_limits<{ct}>::denorm_min()",
                "get_NegativeZero" when floatRt => $"(-({ct})0.0)",
                // IFloatingPointConstants: the correctly-rounded double constants;
                // the wrapper's cast rounds them to the correctly-rounded float
                // values for a float TSelf (matching float.Pi/E/Tau exactly).
                "get_Pi" when floatRt => "3.141592653589793",
                "get_E" when floatRt => "2.718281828459045",
                "get_Tau" when floatRt => "6.283185307179586",
                // IBinaryNumber<TSelf>.AllBitsSet — every bit set at TSelf's REAL
                // storage width. Integers ride the all-ones storage value into the
                // (possibly promoted) stack slot (byte -> 255, sbyte -> -1); the
                // floats are the all-ones BIT PATTERN (a negative quiet NaN with a
                // full payload, pinned by the interface contract and observable via
                // BitConverter) — built by bit-reinterpretation, never a NaN
                // literal, so the exact pattern survives.
                "get_AllBitsSet" when rt.Primitive is PrimitiveTypeCode.Single =>
                    "dn2cpp_bits_r4(0xFFFFFFFFu)",
                "get_AllBitsSet" when rt.Primitive is PrimitiveTypeCode.Double =>
                    "dn2cpp_bits_r8(UINT64_C(0xFFFFFFFFFFFFFFFF))",
                "get_AllBitsSet" =>
                    $"(({CppTypes.StorageOf(rt)})~({CppTypes.StorageOf(rt)})0)",
                _ => null,
            };
            if (konst is not null)
            {
                Push(CppTypes.KindOf(rt), ct, $"(({ct})({konst}))");
                return true;
            }
        }

        // INumberBase<TSelf>.Radix — the base of the representation: 2 for every
        // binary primitive (the integers and the IEEE floats), 10 for the intrinsic
        // decimal. The member returns int whatever TSelf is, so it cannot ride the
        // return-typed constant block above; TSelf comes from the closed interface
        // instantiation. (Double/Single carry Radix only as explicit property impls,
        // invisible to the constrained resolver, so float TSelf needs this row too;
        // a non-intrinsic struct TSelf — Half — resolves its own explicit impl.)
        if (ps.Length == 0 && callee.Name == "get_Radix"
            && callee.DeclaringClass.Context.TypeArgs is [{ } radixSelf])
        {
            int? radix = radixSelf.Kind == TypeKind.Primitive ? 2 : IsDecimal(radixSelf) ? 10 : null;
            if (radix is { } rx)
            {
                Push(StackKind.I4, "int32_t", rx.ToString());
                return true;
            }
        }

        // The same nullary constants with TSelf closed to the intrinsic decimal
        // (the real Enumerable.Sum/Average(decimal) seed their accumulator with
        // T.Zero): lower to the Dn2CppDecimal constructors. Min/MaxValue are the
        // all-ones 96-bit mantissa at scale 0 (±79228162514264337593543950335).
        if (ps.Length == 0 && IsDecimal(callee.Signature.ReturnType))
        {
            string? dconst = callee.Name switch
            {
                "get_Zero" or "get_AdditiveIdentity" => "dn2cpp_decimal_from_i4(0)",
                "get_One" or "get_MultiplicativeIdentity" => "dn2cpp_decimal_from_i4(1)",
                "get_NegativeOne" => "dn2cpp_decimal_from_i4(-1)",
                "get_MinValue" => "dn2cpp_decimal_from_parts(-1, -1, -1, 1, 0)",
                "get_MaxValue" => "dn2cpp_decimal_from_parts(-1, -1, -1, 0, 0)",
                _ => null,
            };
            if (dconst is not null)
            {
                Push(StackKind.Struct, "Dn2CppDecimal", dconst);
                return true;
            }
        }

        // Two-operand comparison / equality operators (IComparisonOperators /
        // IEqualityOperators): op_LessThan(a, b) etc. -> (a OP b).
        string? op = callee.Name switch
        {
            "op_LessThan" => "<",
            "op_GreaterThan" => ">",
            "op_LessThanOrEqual" => "<=",
            "op_GreaterThanOrEqual" => ">=",
            "op_Equality" => "==",
            "op_Inequality" => "!=",
            _ => null,
        };
        if (op is not null && ps is [{ Kind: TypeKind.Primitive } pl, { Kind: TypeKind.Primitive }])
        {
            string ct = CppTypes.Of(pl);
            var b = Pop();
            var a = Pop();
            Push(StackKind.I4, "int32_t", $"(({Cast(a, ct)}) {op} ({Cast(b, ct)}) ? 1 : 0)");
            return true;
        }

        // Two-operand arithmetic / bitwise operators (IAdditionOperators /
        // ISubtractionOperators / IMultiplyOperators / IDivisionOperators /
        // IBitwiseOperators): op_Addition(a, b) etc. -> (a OP b), result typed as the
        // operand. `Sum` accumulates via op_(Checked)Addition, `Average` divides via
        // op_Division. The op_Checked* forms trap integer overflow with the same
        // managed OverflowException the *.ovf opcodes raise; on float/double checked
        // equals unchecked, so the plain op stays correct there. Checked division has
        // no distinct overflow behavior at this layer: its only traps (divide-by-zero,
        // MinValue/-1) come from the div instruction itself, which this transpiler
        // lowers to plain `/` as a standing carve-out, so it shares the plain row.
        string? arith = callee.Name switch
        {
            "op_Addition" or "op_CheckedAddition" => "+",
            "op_Subtraction" or "op_CheckedSubtraction" => "-",
            "op_Multiply" or "op_CheckedMultiply" => "*",
            "op_Division" or "op_CheckedDivision" => "/",
            "op_BitwiseAnd" => "&",
            "op_BitwiseOr" => "|",
            "op_ExclusiveOr" => "^",
            _ => null,
        };
        if (arith is not null && ps is [{ Kind: TypeKind.Primitive } al, { Kind: TypeKind.Primitive }])
        {
            string ct = CppTypes.Of(al);
            var b = Pop();
            var a = Pop();
            if (callee.Name is "op_CheckedAddition" or "op_CheckedSubtraction" or "op_CheckedMultiply"
                && IsCheckedOverflowPrimitive(al.Primitive))
            {
                string chk = callee.Name switch
                {
                    "op_CheckedAddition" => "add",
                    "op_CheckedSubtraction" => "sub",
                    _ => "mul",
                };
                EmitGenericMathCheckedOp(chk, al, Cast(a, ct), Cast(b, ct));
            }
            else
            {
                Push(CppTypes.KindOf(al), ct, WrapToStorage(al, $"(({Cast(a, ct)}) {arith} ({Cast(b, ct)}))"));
            }
            return true;
        }

        // Remainder (IModulusOperators): op_Modulus(a, b) -> the truncated-division
        // remainder, result typed as the left operand like the arithmetic rows.
        // Integers use C++ `%` (same truncated semantics as .NET's rem); float/double
        // use fmod, mirroring the IL rem lowering (see BinaryRem) — the remainder of
        // two representable values is exactly representable, so computing in double
        // and narrowing loses nothing for a float operand.
        if (callee.Name == "op_Modulus"
            && ps is [{ Kind: TypeKind.Primitive } ml, { Kind: TypeKind.Primitive }])
        {
            string ct = CppTypes.Of(ml);
            var b = Pop();
            var a = Pop();
            if (ml.Primitive is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double)
                Push(StackKind.R8, ct, $"(({ct})fmod((double)({Cast(a, ct)}), (double)({Cast(b, ct)})))");
            else
                Push(CppTypes.KindOf(ml), ct, WrapToStorage(ml, $"(({Cast(a, ct)}) % ({Cast(b, ct)}))"));
            return true;
        }

        // Shift operators (IShiftOperators): op_LeftShift / op_RightShift /
        // op_UnsignedRightShift(value, int amount), result typed as the value (left)
        // operand. Matches the BCL operator bodies exactly — which differ from the
        // IL shl/shr masking of the promoted stack slot: the amount masks to the
        // operand's REAL storage width (&7 for sbyte/byte, &15 for short/ushort/char,
        // &31/&63 for the word sizes), so byte << 8 == byte << 0 == byte, and >>>
        // zero-extends from the storage width before the logical shift, so
        // (sbyte)-8 >>> 1 is 124 (0xF8 >> 1 at 8 bits), not the int32-promoted -4.
        // The result truncates back to storage. Left shifts run in unsigned
        // arithmetic so a set high bit can never hit C++'s signed-left-shift UB (the
        // same trap the IL shl lowering guards; see Shift in
        // MethodCompiler.Arithmetic).
        if (callee.Name is "op_LeftShift" or "op_RightShift" or "op_UnsignedRightShift"
            && ps is [{ Kind: TypeKind.Primitive } sl, { Kind: TypeKind.Primitive }])
        {
            string ct = CppTypes.Of(sl);
            bool wide = sl.Primitive is PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr;
            string ut = wide ? "uint64_t" : "uint32_t";
            int mask = sl.Primitive switch
            {
                PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => 7,
                PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => 15,
                _ => wide ? 63 : 31,
            };
            // >>>'s zero-extension width: the unsigned storage type for sub-word
            // operands, the full unsigned word otherwise.
            string ust = sl.Primitive switch
            {
                PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => "uint8_t",
                PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => "uint16_t",
                _ => ut,
            };
            // A word-size unsigned operand right-shifts logically; everything else
            // (signed, and the int32-promoted non-negative sub-word unsigned) shifts
            // its natural promoted type arithmetically. Of() maps nuint to intptr_t,
            // so UIntPtr must take the unsigned route explicitly.
            bool logicalShr = sl.Primitive is PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.UInt64
                or PrimitiveTypeCode.UIntPtr;
            var amount = Pop();
            var value = Pop();
            string amt = $"((int32_t)({amount.Expr}) & {mask})";
            string core = callee.Name switch
            {
                "op_LeftShift" => $"(({ct})((({ut})({Cast(value, ct)})) << {amt}))",
                "op_RightShift" when !logicalShr => $"(({ct})(({Cast(value, ct)}) >> {amt}))",
                "op_RightShift" => $"(({ct})((({ut})({Cast(value, ct)})) >> {amt}))",
                _ => $"(({ct})((({ust})({Cast(value, ct)})) >> {amt}))",
            };
            Push(CppTypes.KindOf(sl), ct, WrapToStorage(sl, core));
            return true;
        }

        // Magnitude selectors (INumberBase<TSelf>) for integer operands, matching
        // the BCL per-type bodies: signed compares |x| in the unsigned counterpart
        // (so |MinValue| stays exact) and breaks ties toward the non-negative
        // operand for Max* / the negative one for Min*; unsigned is the plain
        // max/min. The *Number variants are identical for integers (no NaN to
        // drop). Float operands stay off this row — they resolve to the
        // Double/Single intrinsic surface (dn2cpp_math_maxmag & co).
        if (callee.Name is "MaxMagnitude" or "MaxMagnitudeNumber" or "MinMagnitude" or "MinMagnitudeNumber"
            && ps is [{ Kind: TypeKind.Primitive } mg, { Kind: TypeKind.Primitive }]
            && IsIntegerPrimitive(mg.Primitive))
        {
            string ct = CppTypes.Of(mg);
            string ut = StorageBitWidth(mg.Primitive) == 64 ? "uint64_t" : "uint32_t";
            bool magUnsigned = mg.Primitive is PrimitiveTypeCode.Byte or PrimitiveTypeCode.UInt16
                or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.UIntPtr;
            bool isMax = callee.Name.StartsWith("Max", StringComparison.Ordinal);
            var b = Pop();
            var a = Pop();
            string at = NewTemp(ct), bt = NewTemp(ct);
            Emit($"{at} = {Cast(a, ct)};");
            Emit($"{bt} = {Cast(b, ct)};");
            if (magUnsigned)
            {
                string cmp = isMax ? ">" : "<";
                Push(CppTypes.KindOf(mg), ct, $"((({ut})({at})) {cmp} (({ut})({bt})) ? ({at}) : ({bt}))");
            }
            else
            {
                string ma = NewTemp(ut), mb = NewTemp(ut);
                Emit($"{ma} = (({at}) < 0 ? ({ut})0 - ({ut})({at}) : ({ut})({at}));");
                Emit($"{mb} = (({bt}) < 0 ? ({ut})0 - ({ut})({bt}) : ({ut})({bt}));");
                Push(CppTypes.KindOf(mg), ct, isMax
                    ? $"({ma} > {mb} ? ({at}) : ({ma} < {mb} ? ({bt}) : (({at}) < 0 ? ({bt}) : ({at}))))"
                    : $"({ma} < {mb} ? ({at}) : ({ma} > {mb} ? ({bt}) : (({at}) < 0 ? ({at}) : ({bt}))))");
            }
            return true;
        }

        // RotateLeft/RotateRight(value, int amount) (IBinaryInteger<TSelf>): a true
        // rotate at TSelf's REAL storage width with the amount masked to that width
        // (mod-width, like the BCL bodies — Byte.RotateLeft masks &7, so amount 8
        // is identity), computed in unsigned arithmetic. Sub-word operands are
        // narrowed out of their int32-promoted slot first (a sign-extended sbyte
        // must rotate its 8 stored bits, not 32) and the result re-enters the slot
        // through WrapToStorage.
        if (callee.Name is "RotateLeft" or "RotateRight"
            && ps is [{ Kind: TypeKind.Primitive } rp, { Kind: TypeKind.Primitive }]
            && IsIntegerPrimitive(rp.Primitive))
        {
            int rw = StorageBitWidth(rp.Primitive);
            string ct = CppTypes.Of(rp);
            string ut = rw == 64 ? "uint64_t" : "uint32_t";
            string narrowed = rw switch { 8 => "(uint8_t)", 16 => "(uint16_t)", _ => "" };
            var off = Pop();
            var v = Pop();
            string vt = NewTemp(ut);
            Emit($"{vt} = ({ut}){narrowed}({Cast(v, ct)});");
            string amt = NewTemp("int32_t");
            Emit($"{amt} = (int32_t)({off.Expr}) & {rw - 1};");
            string rot = callee.Name == "RotateLeft"
                ? $"(({vt} << {amt}) | ({vt} >> (({rw} - {amt}) & {rw - 1})))"
                : $"(({vt} >> {amt}) | ({vt} << (({rw} - {amt}) & {rw - 1})))";
            Push(CppTypes.KindOf(rp), ct, WrapToStorage(rp, $"(({ct})({rot}))"));
            return true;
        }

        // Numeric conversions (INumberBase<T>): TryConvertFrom/To{Checked,Truncating,
        // Saturating}(value, out result) -> bool. Both numeric types are closed to
        // primitives, so the conversion is emitted inline with its real range
        // semantics: Checked traps out of range (managed OverflowException; NaN
        // included), Saturating clamps to [T.MinValue, T.MaxValue] (NaN -> 0), and
        // Truncating wraps integer sources / clamps float sources (.NET's saturating
        // float->integer cast). A float target is always representable (IEEE rounding,
        // infinity on overflow), so every mode is the plain cast there. The boolean
        // result is always success. `Average` reaches these through
        // CreateChecked/CreateTruncating when widening the sum/count.
        if (callee.Name is "TryConvertFromChecked" or "TryConvertFromTruncating" or "TryConvertFromSaturating"
                or "TryConvertToChecked" or "TryConvertToTruncating" or "TryConvertToSaturating"
            && ps is [{ Kind: TypeKind.Primitive } srcT, { Kind: TypeKind.ByRef, Element: { Kind: TypeKind.Primitive } dstT }])
        {
            // The helpers convert at the true storage widths (sub-word primitives are
            // int32-promoted on the stack, so Of() would lose the target's range).
            string srcSt = CppTypes.StorageOf(srcT);
            string dstSt = CppTypes.StorageOf(dstT);
            var outPtr = Pop(); // out result (managed pointer, top of stack)
            var val = Pop();    // value
            string srcVal = $"(({srcSt})({Cast(val, CppTypes.Of(srcT))}))";
            bool srcFloat = srcT.Primitive is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double;
            bool dstFloat = dstT.Primitive is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double;
            string conv;
            if (dstFloat)
                conv = $"(({dstSt}){srcVal})";
            else if (callee.Name is "TryConvertFromChecked" or "TryConvertToChecked")
                conv = $"dn2cpp_create_checked<{dstSt}, {srcSt}>({srcVal})";
            else if (srcFloat)
                conv = $"dn2cpp_convert_to_integer_native<{dstSt}, {srcSt}>({srcVal})";
            else if (callee.Name is "TryConvertFromSaturating" or "TryConvertToSaturating")
                conv = $"dn2cpp_create_saturating<{dstSt}, {srcSt}>({srcVal})";
            else
                conv = $"(({dstSt}){srcVal})";
            // Storage width, never the stack-promoted one: the CALLER picks the byref, so
            // it can be `out arr[i]` or `out s.Field`, where a wide store hits a neighbor.
            Emit($"*({Cast(outPtr, dstSt + "*")}) = ({dstSt})({conv});");
            // …and it can equally be an int32-promoted slot, which that store leaves with
            // stale upper bytes. Same fixup a byref ARGUMENT gets; PopArgs never saw this
            // one, since the site pops its own ref (Enum.TryParse's store does the same).
            NoteByRefSlotFixup(outPtr, ps[1]);
            EmitByRefSlotFixups();
            Push(StackKind.I4, "int32_t", "1");
            return true;
        }

        // IBinaryInteger<TSelf>.ReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned)
        // -> TSelf. A static-virtual whose DEFAULT interface body calls the static-abstract
        // TryReadBigEndian (an InternalCall with no IL) and throws OverflowException on
        // failure — dn2cpp_read_big_endian reproduces both: it reads the span big-endian,
        // sign/zero-extends per isUnsigned, and raises the catchable OverflowException when
        // the value does not fit TSelf. Reached from System.Formats.Tar (GNU/PAX base-256
        // numeric fields) closed to Int32/Int64. TSelf is the (substituted) return type.
        // Only the ReadOnlySpan/Span<byte> overload is modeled — the (byte[], …) overloads
        // are a loud decline (Tar uses the span form); a name-gated route may DECLINE a
        // shape but must never POP the wrong one, so the shape is matched before any Pop.
        // Same predicate the reach-side cut asks (Compilation.IsInterceptedReadBigEndian),
        // called here at this asker's own position (cut ⟹ route): the reach cut deletes the
        // DIM body, so this route must name none of it.
        if (Comp.IsInterceptedReadBigEndian(callee))
        {
            var rbeSelf = callee.Signature.ReturnType;
            string selfSt = CppTypes.StorageOf(rbeSelf);
            var rbeUnsigned = Pop();                            // bool isUnsigned (top of stack)
            string sv = SpanValue(Pop(), CppTypes.Of(ps[0]));  // ReadOnlySpan<byte> source
            var rbeKind = CppTypes.KindOf(rbeSelf);
            string rbeStack = CppTypes.DefaultForKind(rbeKind);
            Push(rbeKind, rbeStack,
                $"(({rbeStack})dn2cpp_read_big_endian<{selfSt}>((const uint8_t*)({sv}.f__reference), "
                + $"{sv}.f__length, {Cast(rbeUnsigned, "int32_t")}))");
            return true;
        }

        // The same conversions with the intrinsic decimal on at least one side and
        // a primitive (or decimal) on the other, via the Dn2CppDecimal conversion
        // helpers (shared with the Decimal intrinsic table, which the constrained
        // static-virtual dispatch reaches for Decimal's own TryConvert* bodies).
        if (TryEmitDecimalTryConvert(callee.Name, callee.Signature))
            return true;

        // Other members with TSelf closed to the intrinsic decimal whose impls are
        // explicit (dotted metadata names, invisible to the constrained resolver —
        // IsZero, IsInteger, IsNaN reached from the INumber default bodies, …):
        // route through the Decimal intrinsic table, the same lowering the resolver
        // picks for the plainly-named statics.
        if (ps.Any(IsDecimal) && TryEmitDecimalIntrinsic(callee.Name, callee.Signature))
            return true;

        // Single-operand sign/category predicates (INumberBase<T>): the BCL guards
        // use IsNegative; the rest round out the common set for numeric arguments.
        if (ps is [{ Kind: TypeKind.Primitive } p0])
        {
            string ct = CppTypes.Of(p0);
            bool isFloat = p0.Primitive is PrimitiveTypeCode.Single or PrimitiveTypeCode.Double;
            bool isUnsigned = p0.Primitive is PrimitiveTypeCode.Byte or PrimitiveTypeCode.UInt16
                or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.UInt64 or PrimitiveTypeCode.UIntPtr;
            switch (callee.Name)
            {
                // Float operands read the sign bit (like double.IsNegative /
                // double.IsPositive): -0.0 is negative and NaN carries a sign,
                // where an arithmetic compare would say the opposite.
                case "IsNegative":
                {
                    var v = Pop();
                    Push(StackKind.I4, "int32_t", isUnsigned ? "0"
                        : isFloat ? $"(std::signbit({Cast(v, ct)}) ? 1 : 0)"
                        : $"(({Cast(v, ct)}) < 0 ? 1 : 0)");
                    return true;
                }
                case "IsPositive":
                {
                    var v = Pop();
                    Push(StackKind.I4, "int32_t", isUnsigned ? "1"
                        : isFloat ? $"(std::signbit({Cast(v, ct)}) ? 0 : 1)"
                        : $"(({Cast(v, ct)}) >= 0 ? 1 : 0)");
                    return true;
                }
                case "IsZero":
                {
                    var v = Pop();
                    Push(StackKind.I4, "int32_t", $"(({Cast(v, ct)}) == 0 ? 1 : 0)");
                    return true;
                }
                case "IsNaN" when isFloat:
                {
                    var v = Pop();
                    string x = Cast(v, ct);
                    Push(StackKind.I4, "int32_t", $"(({x}) != ({x}) ? 1 : 0)");
                    return true;
                }
                case "IsNaN":
                    Pop();
                    Push(StackKind.I4, "int32_t", "0");
                    return true;

                // Constant category predicates (INumberBase<TSelf>): every primitive
                // value is canonical and real, none is complex/imaginary — the BCL
                // bodies are the matching `return true/false` for every TSelf here.
                // IsCanonical/IsComplexNumber/IsImaginaryNumber cover floats too
                // (Double/Single carry them only as explicit interface impls,
                // invisible to the constrained resolver); the value-dependent float
                // predicates keep resolving to the Double/Single intrinsic surface,
                // so the remaining rows take integer operands only.
                case "IsCanonical":
                case "IsRealNumber" or "IsInteger" or "IsFinite" when !isFloat:
                    Pop();
                    Push(StackKind.I4, "int32_t", "1");
                    return true;
                case "IsComplexNumber" or "IsImaginaryNumber":
                case "IsInfinity" or "IsPositiveInfinity" or "IsNegativeInfinity"
                    or "IsSubnormal" when !isFloat:
                    Pop();
                    Push(StackKind.I4, "int32_t", "0");
                    return true;
                // The BCL integer bodies: IsNormal is `value != 0`, the even/odd
                // split tests the lowest bit (correct for a negative two's-complement
                // slot value: -3 & 1 == 1).
                case "IsNormal" when !isFloat:
                {
                    var v = Pop();
                    Push(StackKind.I4, "int32_t", $"(({Cast(v, ct)}) != 0 ? 1 : 0)");
                    return true;
                }
                case "IsEvenInteger" or "IsOddInteger" when !isFloat:
                {
                    var v = Pop();
                    string bit = callee.Name == "IsEvenInteger" ? "== 0" : "!= 0";
                    Push(StackKind.I4, "int32_t", $"(((({Cast(v, ct)}) & 1) {bit}) ? 1 : 0)");
                    return true;
                }

                // INumber<TSelf>.Sign — always int (-1/0/+1). The member carries a
                // default body, but for a float operand that body answers ±1 on NaN
                // (its IsNegative reads the sign bit) where real .NET dispatches to
                // the concrete Sign and raises ArithmeticException — so the table
                // takes priority with the exact NaN guard (mirroring the Math.Sign
                // intrinsic); integer operands get the same three-way the default
                // body computes.
                case "Sign":
                {
                    var v = Pop();
                    string st = NewTemp(ct);
                    Emit($"{st} = {Cast(v, ct)};");
                    if (isFloat)
                        Emit($"if (std::isnan({st})) dn2cpp_throw_arithmetic();");
                    Push(StackKind.I4, "int32_t", $"(({st}) > 0 ? 1 : (({st}) < 0 ? -1 : 0))");
                    return true;
                }

                // Abs (INumberBase<TSelf>) for integer operands — the same
                // storage-width helper as the Math.Abs intrinsic (Abs(MinValue)
                // raises the managed OverflowException); unsigned Abs is identity.
                // Float operands resolve to the Double/Single fabs instead.
                case "Abs" when !isFloat:
                {
                    var v = Pop();
                    Push(CppTypes.KindOf(p0), ct, isUnsigned ? Cast(v, ct)
                        : $"({ct})dn2cpp_math_abs_int(({CppTypes.StorageOf(p0)})({v.Expr}))");
                    return true;
                }

                // IBinaryNumber<TSelf>.IsPow2 for integer operands — the BCL bit
                // test: no bit in common with value-1 and (strictly) positive; an
                // unsigned operand only excludes zero. Float operands resolve to
                // the Double/Single significand test.
                case "IsPow2" when !isFloat:
                {
                    var v = Pop();
                    string t = NewTemp(ct);
                    Emit($"{t} = {Cast(v, ct)};");
                    string nz = isUnsigned ? $"{t} != 0" : $"{t} > 0";
                    Push(StackKind.I4, "int32_t",
                        $"(((({t}) & (({t}) - 1)) == 0 && {nz}) ? 1 : 0)");
                    return true;
                }

                // The IBinaryNumber/IBinaryInteger bit counts for integer operands,
                // at TSelf's REAL storage width (a sub-word value is narrowed out of
                // its int32-promoted slot first, so sbyte -1 popcounts 8 bits, and
                // the ZeroCounts answer the storage width for 0). A signed TSelf's
                // Log2 raises the ArgumentOutOfRangeException .NET pins for a
                // negative input (zero stays the no-throw 0). Float Log2 resolves
                // to std::log2.
                case "Log2" or "PopCount" or "LeadingZeroCount" or "TrailingZeroCount" when !isFloat:
                {
                    EmitBitCountOp(callee.Name, Pop(), StorageBitWidth(p0.Primitive),
                        ct, CppTypes.KindOf(p0),
                        signedLog2Throws: p0.Primitive is PrimitiveTypeCode.SByte
                            or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.Int32
                            or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.IntPtr);
                    return true;
                }

                // op_OnesComplement (IBitwiseOperators) — bitwise NOT, truncated
                // back into a sub-word operand's storage width like the arithmetic
                // rows ((byte)~value). Float/double (IBinaryNumber's `~` over the
                // IEEE bits) NOT the raw bit pattern through the width-exact
                // reinterpret helpers — the result is generally a NaN whose exact
                // payload is observable via BitConverter, so no arithmetic detour.
                case "op_OnesComplement" when isFloat:
                {
                    var v = Pop();
                    Push(StackKind.R8, ct, p0.Primitive is PrimitiveTypeCode.Single
                        ? $"dn2cpp_bits_r4(~dn2cpp_r4_bits({Cast(v, ct)}))"
                        : $"dn2cpp_bits_r8(~dn2cpp_r8_bits({Cast(v, ct)}))");
                    return true;
                }
                case "op_OnesComplement":
                {
                    var v = Pop();
                    Push(CppTypes.KindOf(p0), ct, WrapToStorage(p0, $"(~({Cast(v, ct)}))"));
                    return true;
                }

                // Unary arithmetic operators (IIncrementOperators / IDecrementOperators /
                // IUnaryNegationOperators / IUnaryPlusOperators): result typed as the
                // operand. The Range iterator advances its cursor via op_Increment.
                // The op_Checked* forms trap integer overflow like the binary ones;
                // checked negation is checked(0 - value), so a signed operand traps
                // only at T.MinValue while an unsigned one traps for any nonzero value.
                case "op_Increment" or "op_CheckedIncrement":
                {
                    var v = Pop();
                    if (callee.Name is "op_CheckedIncrement" && IsCheckedOverflowPrimitive(p0.Primitive))
                        EmitGenericMathCheckedOp("add", p0, Cast(v, ct), $"({ct})1");
                    else
                        Push(CppTypes.KindOf(p0), ct, WrapToStorage(p0, $"(({Cast(v, ct)}) + 1)"));
                    return true;
                }
                case "op_Decrement" or "op_CheckedDecrement":
                {
                    var v = Pop();
                    if (callee.Name is "op_CheckedDecrement" && IsCheckedOverflowPrimitive(p0.Primitive))
                        EmitGenericMathCheckedOp("sub", p0, Cast(v, ct), $"({ct})1");
                    else
                        Push(CppTypes.KindOf(p0), ct, WrapToStorage(p0, $"(({Cast(v, ct)}) - 1)"));
                    return true;
                }
                case "op_UnaryNegation" or "op_CheckedUnaryNegation":
                {
                    var v = Pop();
                    if (callee.Name is "op_CheckedUnaryNegation" && IsCheckedOverflowPrimitive(p0.Primitive))
                        EmitGenericMathCheckedOp("sub", p0, $"({ct})0", Cast(v, ct));
                    else
                        Push(CppTypes.KindOf(p0), ct, WrapToStorage(p0, $"(-({Cast(v, ct)}))"));
                    return true;
                }
                case "op_UnaryPlus":
                {
                    var v = Pop();
                    Push(CppTypes.KindOf(p0), ct, Cast(v, ct));
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>TSelf primitives whose op_Checked* generic-math operators trap on
    /// overflow: every integer width including char/nint. Float/double checked
    /// equals unchecked, so they keep the plain op.</summary>
    private static bool IsCheckedOverflowPrimitive(PrimitiveTypeCode p) =>
        IsIntegerPrimitive(p) || p == PrimitiveTypeCode.Char;

    /// <summary>Wrap an unchecked generic-math result back into a sub-word operand's
    /// real storage width. The int32-promoted stack slot would otherwise keep the
    /// unwrapped value — ushort.op_Subtraction is (ushort)(a - b), so ' ' - 'a' must
    /// re-enter the ushort domain as 65471, not stay -65. Word-size operands already
    /// compute at their own width and pass through untouched.</summary>
    private static string WrapToStorage(TypeDesc t, string expr)
    {
        string ct = CppTypes.Of(t);
        string st = CppTypes.StorageOf(t);
        return st == ct ? expr : $"((({ct})({st}){expr}))";
    }

    /// <summary>The real storage width in bits of an integer primitive (the width
    /// the generic-math bit ops run at — a sub-word operand's int32-promoted stack
    /// slot must not leak into PopCount/RotateLeft/&hellip;). The pointer-sized
    /// integers follow the 64-bit targets dn2cpp emits for.</summary>
    private static int StorageBitWidth(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.SByte or PrimitiveTypeCode.Byte => 8,
        PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => 16,
        PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
            or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr => 64,
        _ => 32,
    };

    /// <summary>The value range of a sub-word integer primitive (whose stack slot is
    /// int32-promoted), or null for a word-size one.</summary>
    private static (string Lo, string Hi)? SubWordBounds(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.SByte => ("-128", "127"),
        PrimitiveTypeCode.Byte => ("0", "255"),
        PrimitiveTypeCode.Int16 => ("-32768", "32767"),
        PrimitiveTypeCode.UInt16 or PrimitiveTypeCode.Char => ("0", "65535"),
        _ => null,
    };

    /// <summary>Overflow-checked generic-math arithmetic (op_CheckedAddition/…, with
    /// TSelf closed to a concrete integer primitive): `ax OP bx`, trapping a result
    /// outside TSelf's range with the same managed OverflowException the *.ovf
    /// opcodes raise. Word-size operands take the __builtin_*_overflow shape of
    /// <see cref="CheckedBinary"/> (covered by the MSVC polyfill in dn2cpp.h), with
    /// the operand's signedness picking the signed/unsigned check like the
    /// .ovf/.ovf.un split. Sub-word operands compute in 64-bit — wide enough for any
    /// such sum/product — and range-check back into TSelf's width, matching the
    /// BCL's `checked((byte)(left + right))` operator bodies.</summary>
    private void EmitGenericMathCheckedOp(string op, TypeDesc t, string ax, string bx)
    {
        var kind = CppTypes.KindOf(t);
        if (SubWordBounds(t.Primitive) is var (lo, hi))
        {
            string st = CppTypes.StorageOf(t);
            string sym = op switch { "add" => "+", "sub" => "-", _ => "*" };
            Push(kind, "int32_t",
                $"(int32_t)dn2cpp_conv_ovf<{st}>(((int64_t)({ax})) {sym} ((int64_t)({bx})), (int64_t)({lo}), (int64_t)({hi}))");
            return;
        }
        string ct = CppTypes.Of(t);
        string opT = t.Primitive switch
        {
            PrimitiveTypeCode.UInt32 => "uint32_t",
            PrimitiveTypeCode.UInt64 => "uint64_t",
            PrimitiveTypeCode.UIntPtr => "uintptr_t",
            _ => ct,
        };
        string r = NewTemp(ct);
        Emit($"if (__builtin_{op}_overflow(({opT})({ax}), ({opT})({bx}), ({opT}*)&{r})) dn2cpp_overflow();");
        _stack.Add(new StackEntry(r, kind, ct));
    }

    private void TranslateDelegateInvoke(MethodInfo invoke)
    {
        var cls = invoke.DeclaringClass;
        // This line NAMES dginvoke_<CppName>, so record the class for the invoker
        // emitter's union walk (Compilation.DelegateInvokerUses): a delegate
        // type reached only as a variance view is not in the emit set, and without
        // the record its invoker was never defined.
        _c.DelegateInvokerUses.Add(cls);
        var ps = invoke.Signature.ParameterTypes;
        var callArgs = new string[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
            callArgs[i] = Cast(Pop(), CppTypes.Of(ps[i]));
        var dg = Cast(Pop(), cls.CppStructName + "*");

        var allArgs = new List<string> { dg };
        allArgs.AddRange(callArgs);
        string call = $"dginvoke_{cls.CppName}({string.Join(", ", allArgs)})";

        if (invoke.Signature.ReturnType.IsVoid)
            Emit(call + ";");
        else
            Push(CppTypes.KindOf(invoke.Signature.ReturnType),
                CppTypes.Of(invoke.Signature.ReturnType), call);
    }

    /// <summary>The EmitIntrinsic dispatch key for an intrinsic declaring class: an adopted
    /// third-party async task-family type answers to its BCL key — its task / builder /
    /// awaiter member names are the ones the C# compiler and the await pattern require, i.e.
    /// the BCL's own, so every existing Task/ValueTask intrinsic case fires unchanged —
    /// every other type to <see cref="NestedIntrinsicDispatchName"/>.</summary>
    private string IntrinsicDispatchName(ClassInfo cls) =>
        _c.AdoptedAsyncKey(cls) ?? NestedIntrinsicDispatchName(cls);

    /// <summary>The dispatch key of an intrinsic class by its own identity only. A nested
    /// intrinsic (e.g. StringBuilder.AppendInterpolatedStringHandler) decodes to a bare
    /// simple FullName that collides with other nested types and is not what the
    /// (declType, name) switch keys on, so resolve its enclosing type from metadata and
    /// return the enclosing-qualified name (<see cref="CoreIntrinsics.NestedDispatchName"/>).
    /// A top-level / non-registered type returns its plain FullName unchanged.</summary>
    private string NestedIntrinsicDispatchName(ClassInfo cls)
    {
        try
        {
            var td = cls.Module.Reader.GetTypeDefinition(cls.Handle);
            if (td.GetDeclaringType() is { IsNil: false } dh)
            {
                var decl = cls.Module.Reader.GetTypeDefinition(dh);
                string encNs = cls.Module.Reader.GetString(decl.Namespace);
                string encNm = cls.Module.Reader.GetString(decl.Name);
                string encFull = string.IsNullOrEmpty(encNs) ? encNm : encNs + "." + encNm;
                if (CoreIntrinsics.IsIntrinsicNested(encFull, cls.Name))
                    return CoreIntrinsics.NestedDispatchName(encFull, cls.Name);
            }
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        { /* unreadable metadata -> fall back to FullName */ }
        return cls.FullName;
    }

    /// <summary>The argument count (excluding the receiver) of the method named by a
    /// call <paramref name="handle"/>, decoded from its signature without resolving the
    /// method — used to locate the receiver slot beneath the pushed arguments when a
    /// reference-type constrained callvirt dereferences its receiver in place.</summary>
    private int ConstrainedReceiverArgCount(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MemberReference =>
            _reader.GetMemberReference((MemberReferenceHandle)handle)
                .DecodeMethodSignature(_c.SigProvider, _method.Context).ParameterTypes.Length,
        HandleKind.MethodDefinition =>
            _reader.GetMethodDefinition((MethodDefinitionHandle)handle)
                .DecodeSignature(_c.SigProvider, _method.Context).ParameterTypes.Length,
        HandleKind.MethodSpecification =>
            ConstrainedReceiverArgCount(_reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method),
        _ => 0,
    };

    /// <summary>constrained.&lt;C&gt; callvirt: the receiver is a managed pointer.
    /// For a value type with a direct implementation, call it on the pointer;
    /// for a reference type, dereference and dispatch virtually.</summary>
    private void EmitConstrainedCall(MethodInfo callee, TypeDesc c)
    {
        var ps = callee.Signature.ParameterTypes;
        // Pop the args raw — the cast target depends on which body the call binds
        // to. A `constrained.<T> callvirt IEquatable<!T>::Equals(!0)` erases the
        // interface parameter to object, but the value-type impl it devirtualizes
        // to takes the struct itself (unboxed on the IL stack); casting to the
        // interface's erased signature here would cast a struct value to
        // Dn2CppObject*. Each branch below casts to its resolved target's types.
        var rawArgs = new StackEntry[ps.Length];
        for (int i = ps.Length - 1; i >= 0; i--)
            rawArgs[i] = Pop();
        var receiver = Pop(); // managed pointer to the constrained value

        if (c.Kind == TypeKind.Class && c.Class!.IsValueType)
        {
            // A GENERIC state machine (`async GDTask<T> Foo<T>()` -> `<Foo>d__0<T>`)
            // arrives as a still-uncompleted specialization whose Methods are empty,
            // and this path — unlike StateMachineMoveNext / CustomAwaiterOnCompleted —
            // never completed it: the resolution then found nothing and the
            // fallback below emitted a run-time trap for a call that MUST bind
            // (an earlier "no lowering" diagnosis misread this — the lowering exists,
            // the receiver was a shell). Same guard those two reach paths already carry.
            _c.EnsureCompleted(c.Class);
            // ONE resolution, shared with the reachability cut (ReachConstrainedImpl):
            // a private copy here would call a body nothing transpiled.
            var impl = _c.ConstrainedImplOf(c.Class, callee);
            if (impl is null)
            {
                // `constrained.<T> callvirt IDisposable::Dispose` with no resolvable
                // impl is the foreach/using disposal of a value-type enumerator that has
                // no (or only an empty) Dispose — e.g. SRM's struct
                // TypeDefinitionHandleCollection.Enumerator, whose IEnumerator<T>.Dispose
                // is empty. Disposing it does nothing, so emit a no-op rather than the
                // loud trap below: the trap is correct for a *value-returning* missing
                // impl (a real logic gap) but wrong for a vacuous disposal, which a
                // reachable foreach actually executes (this aborted native dn2cpp
                // mid-transpile). dn2cpp's heap is GC'd, so there is nothing to release.
                if (callee.Name == "Dispose"
                    && callee.Signature.ReturnType.IsVoid
                    && ps.Length == 0
                    && callee.DeclaringClass.FullName == "System.IDisposable")
                    return;
                // The struct DECLARES the interface this call constrains to, so the
                // dispatch must bind — a null impl here is a transpiler resolution
                // bug (a shell that slipped past EnsureCompleted, an unmatched
                // explicit-impl name), not the dead-code shape below. Fail the
                // transpile instead of planting a runtime trap on a live path.
                if (c.Class.Interfaces.Contains(callee.DeclaringClass))
                    throw new NotSupportedException(
                        $"constrained callvirt: {c.Class.FullName} implements " +
                        $"{callee.DeclaringClass.FullName} but no body resolved for " +
                        $"{callee.Name} (transpiler bug — the call must bind)");
                // A value type with no direct impl of the constrained virtual. This is
                // reachable-but-dead in practice — e.g. GenericComparer<T>.Compare's
                // `x.CompareTo(y)` instantiated for a non-IComparable T (KeyValuePair)
                // via SortedSet's `comparer ?? Comparer<T>.Default` fallback that is
                // never taken (a real comparer is always supplied). Emit a runtime
                // throw rather than a transpile error: loud if ever executed.
                Emit($"dn2cpp_fail(\"InvalidOperationException ({c.Class.FullName} has no {callee.Name})\");");
                if (!callee.Signature.ReturnType.IsVoid)
                {
                    string ct = CppTypes.Of(callee.Signature.ReturnType);
                    Push(CppTypes.KindOf(callee.Signature.ReturnType), ct,
                        CppTypes.ZeroInitExpr(ct));
                }
                return;
            }
            // A trivially-constant impl (`ldc; ret`) folds to its literal; the
            // receiver/args were popped above (discarding them is side-effect-free).
            if (TryFoldTrivialConstBody(impl, out string constLit))
            {
                Push(CppTypes.KindOf(impl.Signature.ReturnType),
                    CppTypes.Of(impl.Signature.ReturnType), constLit);
                return;
            }
            var implPs = impl.Signature.ParameterTypes;
            var all = new List<string> { Cast(receiver, c.Class.CppStructName + "*") };
            for (int i = 0; i < rawArgs.Length; i++)
                all.Add(Cast(rawArgs[i], CppTypes.Of(implPs[i])));
            EmitCallResult(impl, DirectCall(impl, all));
            return;
        }

        // A placeholder-typed constrained receiver reaching the reference path
        // would be dereferenced as an object pointer while holding a value —
        // any such shape a shared body needs must be devirtualized above.
        TaintIfCanonical(c, "constrained");

        // Reference type (or boxed): the pointer holds an object reference. The cast to
        // the declaring type's struct pointer (and, for an interface, the resolve_interface
        // ti_) names a type that may be reached only through this site; note it for (opaque)
        // emission so neither name dangles (mirrors the virtual-call and interface-call nets).
        if (!callee.DeclaringClass.IsValueType && callee.DeclaringClass.IntrinsicCppName is null)
            NoteReferencedType(callee.DeclaringClass);
        // A `constrained.` callvirt whose receiver turned out to be a reference:
        // the byref holds an object pointer, and THAT is what can be null (the
        // byref itself is the address of live storage). .NET raises an NRE here
        // exactly as for a plain callvirt, so the guard wraps the loaded pointer
        // rather than the byref.
        string obj = $"dn2cpp_null_check(*((Dn2CppObject**)({receiver.Expr})))";
        string typedObj = $"(({callee.DeclaringClass.CppStructName}*){obj})";
        var args2 = new List<string> { typedObj };
        for (int i = 0; i < rawArgs.Length; i++)
            args2.Add(Cast(rawArgs[i], CppTypes.Of(ps[i])));
        NoteDispatchSignatureTypes(callee);
        string fnPtrType = FnPtrType(callee);
        if (callee.DeclaringClass.IsInterface)
            NoteCanonicalItfDispatch(callee.DeclaringClass);
        string dispatch = callee.DeclaringClass.IsInterface
            ? $"dn2cpp_resolve_interface({obj}->type, &{ItfDispatchTi(callee.DeclaringClass).CppTypeInfoName})[{callee.VtableSlot}]"
            : $"{obj}->type->vtable[{callee.VtableSlot}]";
        EmitCallResult(callee, $"(({fnPtrType})({dispatch}))({string.Join(", ", args2)})");
    }

    /// <summary>True for a (closed) <c>ReadOnlySpan&lt;char&gt;</c>/<c>Span&lt;char&gt;</c>
    /// value type — the operand/return shape of the span <c>String.Concat</c> overloads
    /// and the <c>string</c>→<c>ReadOnlySpan&lt;char&gt;</c> <c>op_Implicit</c> that modern
    /// Roslyn lowers a char-in-concat through. The monomorphized struct is
    /// <c>{ char16_t* _reference; int _length }</c> (matched by the mangled FullName).
    ///
    /// <para>Forwards to the one definition, which lives in CoreIntrinsics because the
    /// reachability cut asks it too (the MemoryExtensions case fold) and cannot reach into
    /// MethodCompiler. Two copies of what a char span IS would be a rule that can drift.</para></summary>
    private static bool IsCharSpan(TypeDesc t) => CoreIntrinsics.IsCharSpan(t);

    /// <summary>Whether a statically known type is a value type (used to
    /// constant-fold <c>typeof(T).IsValueType</c>). Strings and Object are the
    /// reference-type primitives; arrays/pointers/byrefs are reference.</summary>
    private static bool IsValueTypeStatic(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => t.Primitive is not (PrimitiveTypeCode.String or PrimitiveTypeCode.Object),
        TypeKind.Class => t.Class!.IsValueType,
        _ => false,
    };

    /// <summary>Whether a statically known type is an interface (used to fold
    /// <c>typeof(T).IsClass</c>, which is "reference type and not interface").</summary>
    private static bool IsInterfaceStatic(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.IsInterface: true };

    /// <summary>Whether a statically known type is nested inside another type
    /// (folds <c>typeof(T).IsNested</c> from the metadata DeclaringType). Synthetic
    /// classes with no metadata handle report top-level.</summary>
    private static bool IsNestedStatic(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } c } && !c.Handle.IsNil
        && !c.Module.Reader.GetTypeDefinition(c.Handle).GetDeclaringType().IsNil;

    /// <summary>Whether a statically known type is sealed (folds
    /// <c>typeof(T).IsSealed</c>). Every primitive except Object is a sealed value
    /// type or a sealed class (String); array types report sealed; for a class the
    /// metadata Sealed bit covers value types/enums/delegates/sealed+static classes.</summary>
    private static bool IsSealedStatic(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => t.Primitive is not PrimitiveTypeCode.Object,
        TypeKind.SZArray => true,
        TypeKind.Class => t.Class!.IsSealed,
        _ => false,
    };

    /// <summary>Whether a statically known type is a ref struct (folds
    /// <c>typeof(T).IsByRefLike</c>): a value type carrying IsByRefLikeAttribute.</summary>
    private static bool IsByRefLikeStatic(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class.IsByRefLike: true };

    /// <summary>Whether a statically known type is a CLR primitive (folds
    /// <c>typeof(T).IsPrimitive</c>): the <b>14</b> types .NET answers true for are
    /// the <see cref="PrimitiveTypeCode"/>s less String, Object, Void and
    /// TypedReference. Decimal and enums are value types but not primitive.
    /// <para>All four exclusions are load-bearing, and two of them were found by
    /// counting rather than by a failure. The comment here said "the 12 primitives
    /// are the codes other than String/Object" — wrong twice over: that set has 16
    /// members, and it admits <c>Void</c> and <c>TypedReference</c>, for both of
    /// which .NET answers <c>false</c>. So <c>typeof(void).IsPrimitive</c> folded to
    /// <c>true</c>, disagreeing with the runtime arm right beside it
    /// (<c>dn2cpp_type_is_primitive</c> reads the emitter's stamped
    /// <c>DN2CPP_TF_PRIMITIVE</c>) and with real .NET. Nothing caught it because no
    /// corpus writes <c>typeof(void).IsPrimitive</c>; the same pair is already
    /// excluded, correctly, by <c>Compilation.Reachability</c>'s emittable-type
    /// test.</para></summary>
    private static bool IsPrimitiveStatic(TypeDesc t) =>
        t.Kind == TypeKind.Primitive &&
        t.Primitive is not (PrimitiveTypeCode.String or PrimitiveTypeCode.Object
                            or PrimitiveTypeCode.Void or PrimitiveTypeCode.TypedReference);

    /// <summary>The System.TypeCode numeric value of a statically known type (folds
    /// <c>Type.GetTypeCode(typeof(T))</c>). Primitives map to their code, an enum to
    /// its underlying integer's code (real .NET unwraps the enum), String/Decimal/
    /// DateTime/DBNull to their codes; everything else — IntPtr/UIntPtr, arrays,
    /// nullables, reference and other struct types — is Object.</summary>
    private static int TypeCodeStatic(TypeDesc t) => t.Kind switch
    {
        TypeKind.Primitive => PrimitiveTypeCodeToTypeCode(t.Primitive),
        TypeKind.Class when t.Class!.IsEnum => PrimitiveTypeCodeToTypeCode(t.Class!.EnumUnderlying),
        TypeKind.Class => t.Class!.FullName switch
        {
            "System.Decimal" => 15,
            "System.DateTime" => 16,
            "System.DBNull" => 2,
            "System.String" => 18,
            _ => 1, // Object
        },
        _ => 1, // SZArray / pointer / byref / anything else
    };

    /// <summary>Maps an IL primitive code to its System.TypeCode value. IntPtr/UIntPtr
    /// and Object (and any non-primitive code) are TypeCode.Object. TypeCode values:
    /// Object=1, DBNull=2, Boolean=3, Char=4, SByte=5, Byte=6, Int16=7, UInt16=8,
    /// Int32=9, UInt32=10, Int64=11, UInt64=12, Single=13, Double=14, Decimal=15,
    /// DateTime=16, String=18.</summary>
    private static int PrimitiveTypeCodeToTypeCode(PrimitiveTypeCode p) => p switch
    {
        PrimitiveTypeCode.Boolean => 3,
        PrimitiveTypeCode.Char => 4,
        PrimitiveTypeCode.SByte => 5,
        PrimitiveTypeCode.Byte => 6,
        PrimitiveTypeCode.Int16 => 7,
        PrimitiveTypeCode.UInt16 => 8,
        PrimitiveTypeCode.Int32 => 9,
        PrimitiveTypeCode.UInt32 => 10,
        PrimitiveTypeCode.Int64 => 11,
        PrimitiveTypeCode.UInt64 => 12,
        PrimitiveTypeCode.Single => 13,
        PrimitiveTypeCode.Double => 14,
        PrimitiveTypeCode.String => 18,
        _ => 1, // IntPtr / UIntPtr / Object / TypedReference / Void -> Object
    };

    /// <summary>The hash a key of the given (closed) type contributes — the
    /// devirtualized EqualityComparer&lt;T&gt;.Default.GetHashCode / T.GetHashCode
    /// the BCL collections use. Mirrors the primitive GetHashCode contracts so the
    /// FindValue (comparer) and TryInsert (key.GetHashCode) paths agree.</summary>
    /// <summary>The runtime helper prefix for an intrinsic value-type key
    /// (Decimal/TimeSpan/DateTime); its <c>_hash</c>/<c>_cmp</c> back the
    /// devirtualized EqualityComparer/Comparer default for these keys, which have no
    /// emitted GetHashCode/Equals. Null for any other type.</summary>
    internal static string? IntrinsicValueTypeFn(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { IsValueType: true } c } && c.IntrinsicCppName is not null
            ? c.FullName switch
            {
                "System.Decimal" => "dn2cpp_decimal",
                "System.TimeSpan" => "dn2cpp_timespan",
                "System.DateTime" => "dn2cpp_datetime",
                "System.DateTimeOffset" => "dn2cpp_datetimeoffset",
                "System.DateOnly" => "dn2cpp_dateonly", // _hash / _cmp back the devirtualized comparer
                "System.TimeOnly" => "dn2cpp_timeonly",
                _ => null,
            }
            : null;

    private string EqualityHashExpr(TypeDesc keyType, StackEntry key)
    {
        if (keyType.IsCanonPlaceholder && !keyType.IsObject)
            return $"(int32_t)({Cast(key, "int32_t")})";
        if (keyType.IsString)
            return $"dn2cpp_string_hashcode({Cast(key, "Dn2CppString*")})";
        if (keyType.Kind == TypeKind.Primitive && !keyType.IsObject)
            return PrimitiveHashExpr(keyType, key);
        if (keyType is { Kind: TypeKind.Class, Class.IsEnum: true })
            return $"(int32_t)({Cast(key, "int32_t")})";
        // A reference-type key (a record/class with value equality, System.Type for
        // a record's EqualityContract, or plain object identity) dispatches its
        // GetHashCode override via the type-info slot, else an identity hash.
        if (IsReferenceKeyType(keyType))
            return $"dn2cpp_object_gethashcode({Cast(key, "Dn2CppObject*")})";
        // An intrinsic value-type key (Decimal/TimeSpan/DateTime) has no emitted
        // GetHashCode — hash its payload with the runtime helper.
        if (IntrinsicValueTypeFn(keyType) is { } ih)
            return $"{ih}_hash({Cast(key, CppTypes.Of(keyType))})";
        // A pointer-modeled intrinsic value-type key hashes its representation —
        // the same mix PrimitiveHashExpr gives IntPtr (no emitted GetHashCode).
        if (IntrinsicPointerValueType(keyType) is { } ipn)
            return $"(int32_t)((uint64_t)({Cast(key, ipn)}) ^ ((uint64_t)({Cast(key, ipn)}) >> 32))";
        // A value-type (struct) key with a GetHashCode override (a hand-written or
        // record struct used as a HashSet/Dictionary key) calls the override on the
        // key value directly — no boxing. The override is reached at the comparer-
        // dispatch site in the reachability scan (use-site gated). A struct with NO
        // override hashes by the synthesized field walk, which is the same function the
        // type-info slot's box fallback calls.
        if (EqualityStructArm(keyType) is { } sc
            && (Compilation.EffectiveGetHashCode(sc) ?? _c.ReachedSynthesizedValueHash(sc)) is { } gh)
        {
            // This is the non-lvalue entry point (a caller may hand it a raw Pop()), and the
            // typed GetHashCode wants an address — spill, exactly as EqualityEqualsExpr does.
            string ct = CppTypes.Of(keyType);
            string tmp = NewTemp(ct);
            Emit($"{tmp} = {Cast(key, ct)};");
            return $"{DirectCallSym(gh)}({ArgsWithRgctx($"&{tmp}", gh)})";
        }
        throw new NotSupportedException(
            $"{_method.DeclaringClass.FullName}.{_method.Name}: element type {keyType} has no hash mapping yet");
    }

    /// <summary>The reached synthesized structural hash of a struct that overrides
    /// GetHashCode nowhere, tainting a shared trial first: a placeholder-bearing struct's
    /// fields ARE the placeholder's, so no field walk can be baked for the group — each
    /// real instantiation compiles its own body and synthesizes against its own fields.
    /// </summary>
    private MethodInfo? SynthesizedValueHash(ClassInfo sc)
    {
        TaintIfCanonical(sc, "valuetype-equality");
        return _c.ReachedSynthesizedValueHash(sc);
    }

    /// <summary>The reached synthesized structural equality of a struct that overrides
    /// Equals(object) nowhere — see <see cref="SynthesizedValueHash"/>.</summary>
    private MethodInfo? SynthesizedValueEquals(ClassInfo sc)
    {
        TaintIfCanonical(sc, "valuetype-equality");
        return _c.ReachedSynthesizedValueEquals(sc);
    }

    /// <summary>Whether <paramref name="t"/> represents a reference-type key
    /// (class/record/array/interface) for <see cref="TryEqualityEqualsLValue"/> and
    /// <see cref="EqualityHashExpr"/> — the object-dispatch arm (dn2cpp_object_equals /
    /// dn2cpp_object_gethashcode).</summary>
    internal static bool IsReferenceKeyType(TypeDesc t) => !t.IsString && t switch
    {
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Object } => true,
        // System.Enum is a reference class in metadata (IsValueType false), but its runtime
        // instances are boxed primitive values; routing it to object-equals/hash would pass a
        // box pointer that dn2cpp_object_equals's value-unboxing arm dereferences as Object
        // rather than the underlying scalar. Exclude it here so the enum branch (or primitive
        // arm reads the underlying at width) rather than to the value-struct arm. Model it as
        // a value type again and EqualityComparer<System.Enum>.Default / List<System.Enum> /
        // HashSet<System.Enum> emit Enum::Equals(&box, …) — a value-type `this` against a
        // Dn2CppObject* receiver.
        { Kind: TypeKind.Class } => !t.Class!.IsValueType && !t.Class!.IsEnum,
        { Kind: TypeKind.External } => true,
        { Kind: TypeKind.SZArray or TypeKind.MDArray } => true,
        _ => false,
    };

    /// <summary>The C++ hash expression for a primitive value, matching each
    /// type's <c>GetHashCode</c> (int-family return the value; 64-bit fold the
    /// halves; float/double hash their bits, via the runtime).</summary>
    private string PrimitiveHashExpr(TypeDesc t, StackEntry val)
    {
        string v = Cast(val, CppTypes.Of(t));
        return t.Primitive switch
        {
            // Double/Single.GetHashCode is the BITS, not the number: truncating to
            // int32 would collide every value sharing an integer part (1.5 and 1.7
            // hash alike) and, for a NaN or an infinity, the conversion is undefined
            // behavior outright. The runtime helper is the same one the boxed arm of
            // dn2cpp_object_gethashcode calls, so a key hashed here and the same key
            // hashed through a box agree on the bucket.
            PrimitiveTypeCode.Double => $"dn2cpp_double_hash({v})",
            PrimitiveTypeCode.Single => $"dn2cpp_single_hash({v})",
            PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
                or PrimitiveTypeCode.IntPtr or PrimitiveTypeCode.UIntPtr
                => $"(int32_t)((uint64_t)({v}) ^ ((uint64_t)({v}) >> 32))",
            _ => $"(int32_t)({v})",
        };
    }

    /// <summary>The one arm of the equality chain that needs the ADDRESS of its
    /// operands (a struct's Equals takes <c>this</c> by reference, and the
    /// Object-virtual fallback boxes the second operand from its storage): the
    /// class of a non-intrinsic, non-enum value type. Null for every arm whose
    /// operands are plain values. The spelling of the test mirrors the branch order
    /// of <see cref="TryEqualityEqualsLValue"/> exactly — it is what tells a caller
    /// holding stack values whether it must spill them to addressable temps first,
    /// and what tells one already holding lvalues that it need not.</summary>
    private static ClassInfo? EqualityStructArm(TypeDesc t) =>
        t is { Kind: TypeKind.Class, IsCanonPlaceholder: false, Class: { IsValueType: true, IsEnum: false, IntrinsicCppName: null } c }
        && !Compilation.ContainsCanonPlaceholder(c)
        && IntrinsicValueTypeFn(t) is null
            ? c
            : null;

    /// <summary>An intrinsic value type modeled as a raw POINTER
    /// (CancellationTokenRegistration = Dn2CppCancelReg*, …): its members are never
    /// transpiled, so naming its Equals/GetHashCode is the dangling-symbol trap
    /// (AGENTS.md, intrinsic-type equality) — and identity of the representation IS
    /// the .NET equality for these opaque handles. The rendered representation decides:
    /// table/closed-generic intrinsics stamp <c>IntrinsicCppName</c>, while special loaded
    /// structs such as <c>RuntimeTypeHandle</c> are mapped by <c>CppTypes.Of</c>.
    /// Non-pointer intrinsic structs get no arm and stay a loud NotSupported/null at the
    /// caller.</summary>
    internal static string? IntrinsicPointerValueType(TypeDesc t)
    {
        if (t is not { Kind: TypeKind.Class,
                Class: { IsValueType: true, IsEnum: false } c })
            return null;
        string n = c.IntrinsicCppName ?? CppTypes.Of(t);
        return n.EndsWith('*') && (c.IntrinsicCppName is not null
            || CoreIntrinsics.SpecialTypeCppName(c.FullName) == n) ? n : null;
    }

    /// <summary>Whether <see cref="TryEqualityEqualsLValue"/> can compare two values
    /// of <paramref name="t"/> — the same branch chain as a pure predicate, with none
    /// of its side effects (a DirectCallSym records a shared-body call edge; a boxed
    /// struct arm taints a canonical trial). A caller that must decide, before it has
    /// popped its operands or opened its loop, whether an element type is comparable
    /// at all asks THIS; it builds the expression later, once, at the emit position.
    /// </summary>
    private bool CanEqualityEquals(TypeDesc t) =>
        t.IsString
        || (t.Kind == TypeKind.Primitive && !t.IsObject)
        || t.IsCanonPlaceholder
        || t is { Kind: TypeKind.Class, Class.IsEnum: true }
        || IsReferenceKeyType(t)
        || IntrinsicValueTypeFn(t) is not null
        || IntrinsicPointerValueType(t) is not null
        || (EqualityStructArm(t) is { } sc
            && (Compilation.EffectiveTypedEquals(sc) is not null
                || Compilation.EffectiveEquals(sc) is not null
                || _c.ReachedSynthesizedValueEquals(sc) is not null));

    /// <summary>The devirtualized EqualityComparer&lt;T&gt;.Default.Equals for a
    /// closed key type, as a pure expression over two operands that are already
    /// C++ <em>lvalues</em> — nothing is emitted, so the result may be placed inside
    /// a loop body whose locals it reads (the array/span scans do exactly that).
    /// For the struct arm the operands must additionally be ADDRESSABLE (see
    /// <see cref="EqualityStructArm"/>); every other arm is happy with any value
    /// expression. Null when no comparison exists for the type — the loud
    /// NotSupported belongs to the caller, which knows what it was scanning.</summary>
    private string? TryEqualityEqualsLValue(TypeDesc keyType, StackEntry a, StackEntry b)
    {
        if (keyType.IsCanonPlaceholder && !keyType.IsObject)
        {
            string ct = CppTypes.Of(keyType);
            return $"(({Cast(a, ct)}) == ({Cast(b, ct)}) ? 1 : 0)";
        }
        if (keyType.IsString)
            return $"dn2cpp_string_equals({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")})";
        if (keyType.Kind == TypeKind.Primitive && !keyType.IsObject)
        {
            string ct = CppTypes.Of(keyType);
            // Double/Single.Equals is not `==`: NaN equals NaN. `==` says otherwise,
            // so a Dictionary<double,V> keyed on NaN could never find its own key
            // back, and a HashSet<float> would take NaN twice. (+0.0/-0.0 are equal
            // under both.) Same runtime helper the boxed arm of dn2cpp_object_equals
            // calls — see PrimitiveHashExpr for the hash half of the pair.
            if (keyType.Primitive is PrimitiveTypeCode.Double)
                return $"dn2cpp_double_equals({Cast(a, ct)}, {Cast(b, ct)})";
            if (keyType.Primitive is PrimitiveTypeCode.Single)
                return $"dn2cpp_single_equals({Cast(a, ct)}, {Cast(b, ct)})";
            return $"(({Cast(a, ct)}) == ({Cast(b, ct)}) ? 1 : 0)";
        }
        // An enum compares at its own width: Of() is int64_t for a 64-bit-backed
        // enum, and a fixed int32_t cast would compare only the low half of two
        // values that differ in the high one.
        if (keyType is { Kind: TypeKind.Class, Class.IsEnum: true })
        {
            string ect = CppTypes.Of(keyType);
            return $"(({Cast(a, ect)}) == ({Cast(b, ect)}) ? 1 : 0)";
        }
        // A reference-type key dispatches its Equals(object) override via the
        // type-info slot, else reference equality.
        if (IsReferenceKeyType(keyType))
            return $"dn2cpp_object_equals({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")})";
        // An intrinsic value-type key (Decimal/TimeSpan/DateTime) compares by its
        // runtime three-way compare — no emitted Equals.
        if (IntrinsicValueTypeFn(keyType) is { } ie)
        {
            string ict = CppTypes.Of(keyType);
            return $"({ie}_cmp({Cast(a, ict)}, {Cast(b, ict)}) == 0 ? 1 : 0)";
        }
        // A pointer-modeled intrinsic value type compares by identity — its .NET
        // equality (same registration handle) and its whole representation.
        if (IntrinsicPointerValueType(keyType) is { } ip)
            return $"(({Cast(a, ip)}) == ({Cast(b, ip)}) ? 1 : 0)";
        // A value-type (struct) key calls its typed IEquatable<T>.Equals(T) override
        // on the two key values directly (the form EqualityComparer<T>.Default uses);
        // falls back to the Object-virtual Equals(object) with the second operand
        // boxed. Both reached at the comparer-dispatch scan site (use-site).
        if (EqualityStructArm(keyType) is { } sc)
        {
            string ct = CppTypes.Of(keyType);
            if (Compilation.EffectiveTypedEquals(sc) is { } teq)
                return $"({DirectCallSym(teq)}({ArgsWithRgctx($"&{a.Expr}, {Cast(b, ct)}", teq)}) ? 1 : 0)";
            if (Compilation.EffectiveEquals(sc) is { } oeq)
            {
                // Boxes the operand with the struct's own type-info — a
                // placeholder-bearing struct key must not bake the owner's.
                TaintIfCanonical(sc, "typeinfo");
                string tinfo = "&" + sc.CppTypeInfoName;
                return $"({DirectCallSym(oeq)}({ArgsWithRgctx($"&{a.Expr}, dn2cpp_box({tinfo}, &{b.Expr}, sizeof({ct}))", oeq)}) ? 1 : 0)";
            }
            // Neither: this key is what ValueType.Equals answers for, and that is a QCall
            // extern over the runtime's MethodTable — nothing to transpile, so the field
            // walk is synthesized (Compilation.SynthesizedValueEquals) and called here at
            // its typed shape. Real .NET reaches the same walk through
            // ObjectEqualityComparer<T>, which boxes the second operand to re-enter
            // Equals(object); devirtualizing to the typed body skips the allocation and
            // computes the identical answer (both operands are statically T, so the type
            // check the boxed entry makes can only succeed).
            if (SynthesizedValueEquals(sc) is { } syn)
                return $"({DirectCallSym(syn)}({ArgsWithRgctx($"&{a.Expr}, {Cast(b, ct)}", syn)}) ? 1 : 0)";
        }
        return null;
    }

    /// <summary>The devirtualized EqualityComparer&lt;T&gt;.Default.Equals over two
    /// stack values: spills the struct arm's operands into addressable temps (a
    /// stack entry is an arbitrary expression — the typed Equals wants its address)
    /// and hands the rest to <see cref="TryEqualityEqualsLValue"/> unchanged.</summary>
    private string EqualityEqualsExpr(TypeDesc keyType, StackEntry a, StackEntry b)
    {
        if (EqualityStructArm(keyType) is not null)
        {
            string ct = CppTypes.Of(keyType);
            var ta = new StackEntry(NewTemp(ct), StackKind.Struct, ct);
            Emit($"{ta.Expr} = {Cast(a, ct)};");
            var tb = new StackEntry(NewTemp(ct), StackKind.Struct, ct);
            Emit($"{tb.Expr} = {Cast(b, ct)};");
            (a, b) = (ta, tb);
        }
        return TryEqualityEqualsLValue(keyType, a, b)
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: Equals for key type {keyType} is not supported yet");
    }

    /// <summary>The devirtualized Comparer&lt;T&gt;.Default.Compare /
    /// IComparable&lt;T&gt;.CompareTo for a closed key type — ordinal string
    /// compare, numeric three-way, or enum-as-int. The intrinsic primitive/string
    /// types have no generated interface map, so real dispatch can't resolve a
    /// comparer; this is the type-specialized op the JIT would devirtualize to.
    /// Throws for unsupported key types — loud, never silent.</summary>
    private string CompareExpr(TypeDesc keyType, StackEntry a, StackEntry b) =>
        TryCompareLValue(keyType, a, b)
        ?? throw new NotSupportedException(
            $"{_method.DeclaringClass.FullName}.{_method.Name}: Compare for key type {keyType} is not supported yet");

    /// <summary>The three-way compare of <see cref="CompareExpr"/> as a pure
    /// expression over two operands that are already C++ lvalues — the ordering twin
    /// of <see cref="TryEqualityEqualsLValue"/>, emitting nothing so a scan loop can
    /// place it over locals it declares itself. Null for a type with no compare.
    /// Each operand is evaluated several times, so it must be an lvalue and not a call.
    /// </summary>
    private string? TryCompareLValue(TypeDesc keyType, StackEntry a, StackEntry b)
    {
        if (keyType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String })
            return $"dn2cpp_str_compare({Cast(a, "Dn2CppString*")}, {Cast(b, "Dn2CppString*")}, 4)";
        // System.Object — the ObjectComparer<object> order real .NET's
        // Comparer<object>.Default uses: null-handling + non-generic System.IComparable
        // dispatch (dn2cpp_object_compare), which orders boxed primitives inline and
        // throws (ArgumentException-equivalent) for a genuinely uncomparable element,
        // exactly as .NET's ObjectComparer<object> does. It must NOT silently compare
        // pointers (the old behaviour before this arm was a loud NotSupported).
        //
        // The reference/canon placeholder (IsCanonPlaceholder — also IsObject) is
        // DECLINED: a comparable reference type in a shared group orders by its typed
        // CompareTo, not this non-generic path, so a shared body must not bake it in.
        // Returning null keeps the trial treating it as unshareable, so the ordering
        // body stays per-instantiation and is recompiled where keyType is the concrete
        // type. NonGenericIComparableTiName's type-info is wired on the reach side by
        // Compilation.NoteNonGenericIComparableUsed (the object constrained-CompareTo arm
        // of ReachConstrainedImpl).
        if (keyType.IsObject)
            return keyType.IsCanonPlaceholder
                ? null
                : $"dn2cpp_object_compare({Cast(a, "Dn2CppObject*")}, {Cast(b, "Dn2CppObject*")}, &{NonGenericIComparableTiName()})";
        // A primitive scalar orders by </> (Object was handled just above, so the
        // scalar branch never silently pointer-compares a reference).
        if (keyType.Kind == TypeKind.Primitive && !keyType.IsObject)
        {
            string ct = CppTypes.Of(keyType);
            string x = Cast(a, ct), y = Cast(b, ct);
            // Float ordering is a TOTAL order, not the raw three-way: a NaN sorts
            // below every number (including -inf) and compares 0 to itself, so
            // `<`/`>` alone — both false for a NaN — would call it equal to
            // everything and leave a sort's result dependent on the visit order.
            // The same expression Double/Single.CompareTo emits (the direct-call
            // intrinsic), so a value ordered here and one ordered through a
            // CompareTo call agree, and the ordering agrees with the equality that
            // already says NaN == NaN (TryEqualityEqualsLValue).
            if (keyType.Primitive is PrimitiveTypeCode.Double or PrimitiveTypeCode.Single)
                return $"(({x}) < ({y}) ? -1 : (({x}) > ({y}) ? 1 : (({x}) == ({y}) ? 0 "
                     + $": (({x}) != ({x}) ? (({y}) != ({y}) ? 0 : -1) : 1))))";
            return $"(({x}) < ({y}) ? -1 : (({x}) > ({y}) ? 1 : 0))";
        }
        if (keyType is { Kind: TypeKind.Class, Class.IsEnum: true })
        {
            // Same width rule as the equality arm: a 64-bit-backed enum orders on
            // all 64 bits, not on a truncated low half.
            string ect = CppTypes.Of(keyType);
            string x = Cast(a, ect), y = Cast(b, ect);
            return $"(({x}) < ({y}) ? -1 : (({x}) > ({y}) ? 1 : 0))";
        }
        // An intrinsic value type (Decimal/TimeSpan/DateTime/…) orders by the same
        // runtime three-way its equality arm already tests against zero — there is
        // no emitted CompareTo to devirtualize to.
        if (IntrinsicValueTypeFn(keyType) is { } ie)
        {
            string ict = CppTypes.Of(keyType);
            return $"{ie}_cmp({Cast(a, ict)}, {Cast(b, ict)})";
        }
        return null;
    }

    /// <summary>If <paramref name="t"/> is a closed <c>System.IComparable&lt;T&gt;</c>
    /// whose argument is a primitive, string or enum (a key type CompareExpr can
    /// devirtualize), returns that argument; otherwise null. Used to recognise the
    /// boxed <c>(IComparable&lt;T&gt)box.CompareTo</c> cast form — a
    /// reference-type T has a real interface map and takes the normal path.</summary>
    private TypeDesc? ComparablePrimitiveArg(TypeDesc t)
    {
        if (t is { Kind: TypeKind.Class, Class: { } cls }
            && _c.GenericDefFullName(cls) == "System.IComparable"
            && cls.Context.TypeArgs.Length == 1)
        {
            var arg = cls.Context.TypeArgs[0];
            // Object (and the reference placeholder it models) is not a
            // devirtualizable compare — CompareExpr rejects it.
            if ((arg.Kind == TypeKind.Primitive && !arg.IsObject)
                || arg is { Kind: TypeKind.Class, Class.IsEnum: true })
                return arg;
        }
        return null;
    }

    /// <summary><c>IComparable&lt;T&gt;.CompareTo(T)</c> on a boxed primitive/enum/
    /// string receiver — the unconstrained cast form. Pops the argument (an unboxed
    /// T) and the boxed receiver, then emits the same typed three-way compare the
    /// constrained path uses, unboxing the receiver from the box header.
    /// A string box is identity, so its receiver is the reference itself.</summary>
    private void EmitBoxedComparableCompareTo(TypeDesc t)
    {
        var arg = Pop();      // the other value (y), an unboxed T
        var receiver = Pop(); // the boxed receiver (x)
        StackEntry x = t is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }
            ? new StackEntry(Cast(receiver, "Dn2CppString*"), StackKind.Ref, "Dn2CppString*")
            : new StackEntry($"(*({CppTypes.Of(t)}*)((Dn2CppObject*)({receiver.Expr}) + 1))",
                CppTypes.KindOf(t), CppTypes.Of(t));
        Push(StackKind.I4, "int32_t", CompareExpr(t, x, arg));
    }

    /// <summary>An <c>IEqualityComparer&lt;T&gt;</c> interface call (GetHashCode/Equals)
    /// on a Dictionary's <c>_comparer</c>. The receiver may be null, the opaque
    /// default-string wrapper (NonRandomizedStringEqualityComparer), or a real user
    /// comparer. A runtime probe (<c>dn2cpp_try_resolve_interface</c>) discriminates: a
    /// type that actually implements <c>IEqualityComparer&lt;T&gt;</c> dispatches through
    /// the object; otherwise fall back to the type-specialized default op
    ///. Returns false for any other member so normal dispatch applies.</summary>
    private bool TryEmitComparerDispatch(MethodInfo callee, TypeDesc keyType)
    {
        switch (callee.Name)
        {
            case "GetHashCode":
            {
                var key = Pop();
                var comparer = Pop();
                EmitComparerObjectDispatch(callee, keyType, comparer, key, null);
                return true;
            }
            case "Equals":
            {
                var b = Pop();
                var a = Pop();
                var comparer = Pop();
                EmitComparerObjectDispatch(callee, keyType, comparer, a, b);
                return true;
            }
            default:
                return false;
        }
    }

    /// <summary>Null-checks an <c>EqualityComparer&lt;T&gt;</c> receiver into a temp, unless
    /// it is statically non-null. On the dominant <c>Default.Equals(a, b)</c> shape the answer
    /// comes from <c>T</c>'s default op and the check is the receiver's ONLY use, so emitting
    /// it pins a whole <c>get_Default</c> call the C++ compiler may not delete —
    /// <c>dn2cpp_null_check</c> can throw. Safe for the other arm because
    /// <see cref="EmitComparerObjectDispatch"/> materializes the receiver itself, so the
    /// unchecked expression is still evaluated once.</summary>
    private StackEntry ComparerReceiverNullChecked(StackEntry recv)
    {
        if (recv.NonNull)
            return recv;
        string checkedRecv = NewTemp("Dn2CppObject*");
        Emit($"{checkedRecv} = (Dn2CppObject*)dn2cpp_null_check({recv.Expr});");
        return recv with { Expr = checkedRecv, NonNull = true, KnownNull = false };
    }

    /// <summary>The shared body behind every <c>IEqualityComparer&lt;T&gt;</c> dispatch:
    /// probe the (possibly null) comparer object for the interface once, dispatch through
    /// its slot when it implements it, else devirtualize to <c>T</c>'s default op — the ONE
    /// rule for "these two values are equal / this value's hash" over a comparer object, so
    /// a Dictionary's <c>_comparer</c> callvirt and an <c>EqualityComparer&lt;T&gt;</c>-typed
    /// <c>Equals</c>/<c>GetHashCode</c> on a real subclass receiver cannot drift. The
    /// operands are passed pre-popped so the receiver-null fast path can be decided by the
    /// caller (the intrinsic arm keeps the byte-identical default-op form for the Default
    /// opaque identity). <paramref name="b"/> is null for the one-operand GetHashCode.</summary>
    private void EmitComparerObjectDispatch(MethodInfo callee, TypeDesc keyType,
        StackEntry comparer, StackEntry a, StackEntry? b)
    {
        string keyCpp = CppTypes.Of(keyType);
        NoteCanonicalItfDispatch(callee.DeclaringClass);
        string itf = "&" + ItfDispatchTi(callee.DeclaringClass).CppTypeInfoName;
        string fnPtr = FnPtrType(callee);
        string cmp = NewTemp("Dn2CppObject*");
        Emit($"{cmp} = {Cast(comparer, "Dn2CppObject*")};");
        string at = NewTemp(keyCpp);
        Emit($"{at} = {Cast(a, keyCpp)};");
        string bt = "";
        if (b is { } bv)
        {
            bt = NewTemp(keyCpp);
            Emit($"{bt} = {Cast(bv, keyCpp)};");
        }
        string slots = NewTemp("const void**");
        Emit($"{slots} = {cmp} ? dn2cpp_try_resolve_interface({cmp}->type, {itf}) : nullptr;");
        string res = NewTemp("int32_t");
        var aEntry = new StackEntry(at, CppTypes.KindOf(keyType), keyCpp);
        if (b is null)
        {
            Emit($"if ({slots} != nullptr) {res} = (({fnPtr})({slots}[{callee.VtableSlot}]))(({callee.DeclaringClass.CppStructName}*){cmp}, {at});");
            Emit("else {");
            Emit($"{res} = {EqualityHashExpr(keyType, aEntry)};");
            Emit("}");
        }
        else
        {
            Emit($"if ({slots} != nullptr) {res} = (({fnPtr})({slots}[{callee.VtableSlot}]))(({callee.DeclaringClass.CppStructName}*){cmp}, {at}, {bt});");
            Emit("else {");
            Emit($"{res} = {EqualityEqualsExpr(keyType, aEntry, new StackEntry(bt, CppTypes.KindOf(keyType), keyCpp))};");
            Emit("}");
        }
        Push(StackKind.I4, "int32_t", res);
    }

    /// <summary>Dereference a managed pointer to a constrained primitive/enum
    /// receiver at its REAL storage width (<see cref="CppTypes.StorageOf"/>),
    /// promoting to the int32 stack model (<see cref="CppTypes.Of"/>) — the ldobj
    /// dual pattern. The pointer can target packed storage (a value-type struct's
    /// narrowed sub-word field, an array element via ldelema, a span's backing
    /// ref), where an Of-width deref would read the neighboring bytes.</summary>
    private static string ConstrainedReceiverValue(TypeDesc c, string ptrExpr)
    {
        string ct = CppTypes.Of(c);
        string st = CppTypes.StorageOf(c);
        return st == ct
            ? $"(*({ct}*)({ptrExpr}))"
            : $"(({ct})(*({st}*)({ptrExpr})))";
    }

    /// <summary>A <c>constrained.</c> callvirt on a primitive, string or enum to an
    /// Object/IEquatable/IComparable-rooted virtual. The JIT devirtualizes these;
    /// emit the type-specialized op instead of boxing (the intrinsic primitive/
    /// string types have no generated interface map, so real dispatch can't resolve
    /// them anyway). Returns false (normal dispatch) for anything but the handled
    /// members.</summary>
    private bool TryEmitValueConstrained(EntityHandle handle, TypeDesc c)
    {
        string name = handle.Kind switch
        {
            HandleKind.MemberReference => _reader.GetString(_reader.GetMemberReference((MemberReferenceHandle)handle).Name),
            HandleKind.MethodDefinition => _reader.GetString(_reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
            _ => "",
        };
        if (IsDefaultNameExternalIntrinsic(c) && name == "ToString"
            && ConstrainedCalleeSig(handle).ParameterTypes.Length == 0)
        {
            var receiver = Pop();
            string ti = TypeInfoExpr(c)
                ?? throw new NotSupportedException(
                    $"{_method.DeclaringClass.FullName}.{_method.Name}: constrained ToString on {c} has no runtime type-info");
            Push(StackKind.Ref, "Dn2CppString*",
                $"((void)({receiver.Expr}), dn2cpp_type_tostring({ti}))");
            return true;
        }
        // An intrinsic value type (Decimal / TimeSpan / DateTime) overriding an Object
        // virtual (ToString/GetHashCode/Equals) or a typed IEquatable/IComparable via
        // `constrained. callvirt`: its real corelib body is intrinsic and never emitted,
        // so the EffectiveToString/GetHashCode/Equals cases below would name a dangling
        // m_ symbol. Route the call to the intrinsic table instead.
        if (c is { Kind: TypeKind.Class, Class: { IntrinsicCppName: not null, IsValueType: true } ivc })
        {
            var csig = handle.Kind == HandleKind.MemberReference
                ? _reader.GetMemberReference((MemberReferenceHandle)handle).DecodeMethodSignature(_c.SigProvider, _method.Context)
                : _reader.GetMethodDefinition((MethodDefinitionHandle)handle).DecodeSignature(_c.SigProvider, _method.Context);
            // ValueType.ToString on an intrinsic with no override is still observable:
            // it returns the exact CLR type name. These values may be ref structs or
            // pointer-represented handles, so boxing is neither legal nor necessary.
            // Keep types with a real override on the intrinsic table: an unclassified
            // formatter must fail loudly instead of silently degrading to a type name.
            if (name == "ToString" && csig.ParameterTypes.Length == 0)
            {
                string intrinsicKey = _c.AdoptedAsyncKey(ivc) ?? _c.GenericDefFullName(ivc);
                if (intrinsicKey == "System.Threading.Tasks.ValueTask"
                    && ivc.Context.TypeArgs is [var resultType])
                {
                    var receiver = Pop();
                    string resultTi = TypeInfoExpr(resultType)
                        ?? throw new NotSupportedException(
                            $"{_method.DeclaringClass.FullName}.{_method.Name}: ValueTask<{resultType}> result has no emitted type-info");
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_valuetask_tostring((Dn2CppTaskAwaiter*)({receiver.Expr}), {resultTi})");
                    return true;
                }
                if (!_c.DeclaresIntrinsicToStringOverride(ivc))
                {
                    var receiver = Pop();
                    string ti = TypeInfoExpr(c)
                        ?? throw new NotSupportedException(
                            $"{_method.DeclaringClass.FullName}.{_method.Name}: constrained ToString on {c} has no emitted type-info");
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"((void)({receiver.Expr}), dn2cpp_type_tostring({ti}))");
                    return true;
                }
            }
            // A vector struct virtual reached via `constrained. callvirt` (e.g.
            // IEquatable<Vector128<T>>.Equals): dispatch with the closed element, like the
            // TypeSpec member path. Equals(object)/GetHashCode/ToString fall through.
            if (ivc.IntrinsicCppName!.StartsWith("Dn2CppVector", StringComparison.Ordinal)
                && ivc.Context.TypeArgs.Length == 1
                && TryEmitVectorOp(ivc.IntrinsicCppName, VecWidthBytes(ivc.IntrinsicCppName),
                    ivc.Context.TypeArgs[0], name, csig, null))
                return true;
            // Dispatch on the generic-DEF name (ValueTaskAwaiter<int> -> System.
            // Runtime.CompilerServices.ValueTaskAwaiter; a nested def collapses to
            // its short name), matching the MemberRef/TypeSpec path: the closed
            // FullName (ValueTaskAwaiter_Int32) matches no intrinsic case. A real
            // (un-adopted) builder's AwaitUnsafeOnCompleted body reaches here —
            // `constrained.<TAwaiter> callvirt UnsafeOnCompleted` at
            // TAwaiter = ValueTaskAwaiter<T>. Non-generic intrinsics (Decimal,
            // DateTime) are unchanged: their def name IS their FullName. An
            // adopted task-family type answers to its BCL key.
            EmitIntrinsic(_c.AdoptedAsyncKey(ivc) ?? _c.GenericDefFullName(ivc),
                name, csig, AdoptedFrom(ivc));
            return true;
        }
        // a `constrained. <int> callvirt ISpanFormattable::TryFormat`
        // (the span-write formatter, reached via a `{value:fmt}` interpolation hole that
        // Roslyn lowers through DefaultInterpolatedStringHandler -> ISpanFormattable) or
        // its shape-identical IUtf8SpanFormattable::TryFormat twin (Span<byte> UTF-8
        // destination — e.g. generic `T : IUtf8SpanFormattable` dispatch). The interface
        // MemberRef doesn't name a concrete integer type, so the EmitIntrinsic
        // TryFormat cases below (keyed on the concrete type) never fire on this path; route
        // it here by the constrained type `c`. dn2cpp's own SRM SignatureDecoder.CheckHeader
        // hits this with a byte, so without it the real System.Number.TryFormat* subtree
        // stays reachable (ReachConstrainedImpl cuts the matching reach edge).
        //
        // Derive the concrete type name, then ask the SAME predicate the reachability
        // cuts ask (ReachConstrainedImpl and ReachVirtualImpl both go through
        // CoreIntrinsics.CvIntegerTryFormat). The eight-width set has exactly one
        // spelling; a hand-written copy here would fail at C++ LINK time, not here.
        if (c.Kind == TypeKind.Primitive
            && (CoreIntrinsics.PrimitiveIntegerFullName(c.Primitive)
                ?? (c.Primitive == PrimitiveTypeCode.Double ? "System.Double" : c.Primitive == PrimitiveTypeCode.Single ? "System.Single" : null)) is { } intName
            && (CoreIntrinsics.LoweredIntegerTryFormat(intName, name) || name == "TryFormat"))
        {
            // A placeholder receiver stands for a whole width-preserving group, which
            // mixes integer primitives with same-underlying ENUMS — and an enum's
            // TryFormat is the member NAME (or [Flags] decomposition), not the numeric
            // formatter this branch emits. Taint so the enum instantiations drop to
            // per-instantiation, where c is the real enum and the enum arm below fires;
            // a concrete primitive is not a placeholder, so this is a no-op for it.
            // Mirrors the enum ToString primitive arm's TaintIfCanonical.
            TaintIfCanonical(c, "enum-tryformat");
            var csig = ConstrainedCalleeSig(handle);
            // Only the primitive 4-arg (Span<char|byte>, out int, ReadOnlySpan
            // <char>, IFormatProvider) shape is lowered (EmitIntrinsic discriminates the
            // destination element type).
            if (csig.ParameterTypes is [_, { Kind: TypeKind.ByRef }, _, _])
            {
                EmitIntrinsic(intName, "TryFormat", csig);
                return true;
            }
            // TOTAL over the predicate: an unmodeled shape must NOT fall through. The
            // reachability cut is by NAME over all eight widths, so by the time control
            // reaches here the real TryFormat body has already been deleted from the tree
            // — falling through would emit an ordinary constrained call naming a symbol
            // nothing defines, i.e. an undefined symbol at C++ LINK time, discovered in a
            // toolchain that knows nothing about IL or reachability (AGENTS.md, "An
            // intercept has two askers"). Throw instead: loud, attributed, and at the one
            // moment the cause is still in hand. No shape in the current corpus reaches
            // this, so it changes no output; it removes the possibility that an overload
            // added upstream silently does.
            throw new NotSupportedException(
                $"Unmodeled {intName}.TryFormat overload shape in a constrained call: "
                + $"{csig.ParameterTypes.Length} parameter(s). The reachability cut deletes "
                + "every TryFormat body on the eight integer primitives "
                + "(CoreIntrinsics.LoweredIntegerTryFormat), so this call has no body to "
                + "fall back to. Model the shape in EmitIntrinsic's TryFormat case, or "
                + "narrow the cut to match the route.");
        }
        // The enum analogue of the integer TryFormat route above:
        // `constrained. <enum> callvirt ISpanFormattable::TryFormat(Span<char>, out int,
        // ReadOnlySpan<char>, IFormatProvider)`, out of a `{enumValue}` interpolation hole
        // lowered through DefaultInterpolatedStringHandler. Box the enum (its ti_ and
        // (name,value) table are already emitted — the constrained token marks it reachable)
        // and format via dn2cpp_enum_format, then copy into the destination span (fits ->
        // copy + count + true; too short -> false: the .NET TryFormat contract,
        // dn2cpp_string_try_copy_to_span). An empty/default format is "G". This is the
        // CONSTRAINED mouth of CoreIntrinsics.CvEnumTryFormat (the boxed mouth is the row,
        // cut in ReachVirtualImpl); it is written out here because the predicate is non-pure
        // (needs c.IsEnum). The reach cut (ReachConstrainedImpl, same c.IsEnum && name ==
        // "TryFormat" test) deletes the real body — which reaches TryFormatPrimitiveDefault
        // -> GetEnumInfo -> GetEnumValuesAndNames (InternalCall) — so this route is TOTAL:
        // an unmodeled overload shape throws rather than falling through to a deleted body.
        if (name == "TryFormat" && c is { Kind: TypeKind.Class, Class.IsEnum: true })
        {
            var esig = ConstrainedCalleeSig(handle);
            if (esig.ParameterTypes is [var eDest, { Kind: TypeKind.ByRef }, var eFmt, _]
                && IsSpanOfPrimitive(eDest, PrimitiveTypeCode.Char) && IsReadOnlySpanChar(eFmt))
            {
                string eti = TypeInfoExpr(c)
                    ?? throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: constrained TryFormat on enum {c} has no emitted type-info");
                Pop();                                            // IFormatProvider — enum formatting is culture-independent
                string efSpan = SpanValue(Pop(), CppTypes.Of(eFmt)); // ReadOnlySpan<char> format
                string eWrote = Cast(Pop(), "int32_t*");          // out int charsWritten
                string eDst = SpanPtr(Pop(), CppTypes.Of(eDest));    // Span<char> destination
                var eRecv = Pop();                                // managed pointer to the unboxed enum value
                string ect = CppTypes.Of(c);
                string etmp = NewTemp(ect);
                Emit($"{etmp} = {ConstrainedReceiverValue(c, eRecv.Expr)};");
                string efmt = NewTemp("Dn2CppString*");
                Emit($"{efmt} = {efSpan}.f__length == 0 ? dn2cpp_string_from_utf8(\"G\", 1) "
                    + $": dn2cpp_string_from_chars((const char16_t*){efSpan}.f__reference, {efSpan}.f__length);");
                string estr = NewTemp("Dn2CppString*");
                // dn2cpp_enum_format wants the reflection Type* (it reads t->typeInfo),
                // not the raw type-info; dn2cpp_get_type_from_handle is the typeof seam.
                Emit($"{estr} = dn2cpp_enum_format(dn2cpp_get_type_from_handle({eti}), dn2cpp_box({eti}, &{etmp}, sizeof({ect})), {efmt});");
                Push(StackKind.I4, "int32_t",
                    $"dn2cpp_string_try_copy_to_span({estr}, (char16_t*){eDst}->f__reference, {eDst}->f__length, {eWrote})");
                return true;
            }
            // TOTAL over the predicate (see the integer twin above): the reach cut deletes
            // the enum ISpanFormattable::TryFormat body, so an unmodeled shape must throw
            // rather than fall through to a body nothing defines.
            throw new NotSupportedException(
                $"Unmodeled enum TryFormat overload shape in a constrained call: "
                + $"{esig.ParameterTypes.Length} parameter(s). The reach cut (ReachConstrainedImpl) "
                + "deletes the enum ISpanFormattable::TryFormat body, so this call has no body to "
                + "fall back to. Model the shape here or narrow the cut to match the route.");
        }
        switch (name)
        {
            case "GetHashCode" when c.Kind == TypeKind.Primitive || c is { Kind: TypeKind.Class, Class.IsEnum: true }:
            {
                var receiver = Pop(); // managed pointer to the constrained primitive
                string ct = CppTypes.Of(c);
                var val = new StackEntry(ConstrainedReceiverValue(c, receiver.Expr), CppTypes.KindOf(c), ct);
                Push(StackKind.I4, "int32_t", EqualityHashExpr(c, val));
                return true;
            }
            // GetHashCode on a value-type struct that overrides it — e.g.
            // `new Coord(1,2).GetHashCode` lowers to `constrained. Coord callvirt
            // object::GetHashCode`. The receiver is a managed pointer to the struct;
            // call its override directly rather than dn2cpp_object_gethashcode, which
            // would read the raw struct as a boxed-object header and crash (;
            // mirrors the struct ToString devirtualization. ReachConstrainedImpl
            // pulls the impl into the tree).
            case "GetHashCode" when c is { Kind: TypeKind.Class, Class: { IsValueType: true } ghs }
                && Compilation.EffectiveGetHashCode(ghs) is { } ghImpl:
            {
                var receiver = Pop();
                Push(StackKind.I4, "int32_t", $"{DirectCallSym(ghImpl)}({ArgsWithRgctx(Cast(receiver, ghs.CppStructName + "*"), ghImpl)})");
                return true;
            }
            // Equals(object) on a value-type struct that overrides it — call the
            // override directly with the struct pointer and the (boxed) argument,
            // not dn2cpp_object_equals on the raw struct pointer. The typed
            // IEquatable<T>::Equals(!0) shape (its argument is the UNBOXED T on the
            // stack — casting it to Dn2CppObject* wouldn't even compile for a
            // multi-word struct like SRM's Symbolic.BitVector) is excluded: it falls
            // through to EmitConstrainedCall, which devirtualizes to Equals(T).
            case "Equals" when c is { Kind: TypeKind.Class, Class: { IsValueType: true } eqs }
                && Compilation.EffectiveEquals(eqs) is { } eqImpl
                && !(IsTypedItfConstrained(handle, "System.IEquatable")
                     && Compilation.EffectiveTypedEquals(eqs) is not null):
            {
                var other = Pop();
                var receiver = Pop();
                Push(StackKind.I4, "int32_t", $"{DirectCallSym(eqImpl)}({ArgsWithRgctx($"{Cast(receiver, eqs.CppStructName + "*")}, {Cast(other, "Dn2CppObject*")}", eqImpl)})");
                return true;
            }
            // IEquatable<T>.Equals(T) on a numeric primitive or enum key — e.g.
            // SpanHelpers.CountValueType<byte>'s `((IEquatable<T>)value).Equals(other)`.
            // A primitive constrained type is TypeKind.Primitive, so EmitConstrainedCall's
            // value-type branch (keyed on TypeKind.Class) is skipped and it would fall to the
            // reference path, which neither boxes the value nor emits the interface's ti_/struct.
            // Devirtualize to a typed value compare (the EqualityComparer<T>.Default.Equals form)
            // instead. Distinguish the typed Equals (the value is on the stack) from
            // Object::Equals(object) (a boxed reference arg), which the arm below claims.
            case "Equals" when ((c.Kind == TypeKind.Primitive && !c.IsObject && !c.IsString)
                                || c is { Kind: TypeKind.Class, Class.IsEnum: true })
                && _stack.Count >= 2 && _stack[^1].Kind != StackKind.Ref:
            {
                var arg = Pop();      // the other value (y)
                var receiver = Pop(); // managed pointer to the constrained value (x)
                string ct = CppTypes.Of(c);
                var x = new StackEntry(ConstrainedReceiverValue(c, receiver.Expr), CppTypes.KindOf(c), ct);
                Push(StackKind.I4, "int32_t", EqualityEqualsExpr(c, x, arg));
                return true;
            }
            // Object::Equals(object) on a primitive or enum receiver — only the ARGUMENT
            // is boxed by the IL, so box the receiver too rather than handing
            // dn2cpp_object_equals a managed pointer whose pointee it would read as an
            // object header (the struct twin below, for the same reason).
            case "Equals" when ((c.Kind == TypeKind.Primitive && !c.IsObject && !c.IsString)
                                || c is { Kind: TypeKind.Class, Class.IsEnum: true })
                && ConstrainedCalleeSig(handle).ParameterTypes is [{ IsObject: true }]:
            {
                var other = Pop();
                var receiver = Pop(); // managed pointer to the constrained value
                string boxed = BoxedConstrainedReceiver(c, receiver);
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_equals({boxed}, {Cast(other, "Dn2CppObject*")})");
                return true;
            }
            // IComparable<T>.CompareTo(T) on a primitive or string key (the LINQ
            // OrderBy / generic sort path). Devirtualize to a typed three-way
            // compare — string uses ordinal (the project's string-ordering model);
            // numeric uses </> (NaN sorts as 0, consistent with the primitive
            // Equals model). The unconstrained (IComparable<T>)box.CompareTo cast
            // form is still unsupported — callers should constrain TKey.
            case "CompareTo" when c.Kind == TypeKind.Primitive || c is { Kind: TypeKind.Class, Class.IsEnum: true }:
            {
                var arg = Pop();      // the other value (y)
                var receiver = Pop(); // managed pointer to the constrained value (x)
                string ct = CppTypes.Of(c);
                var x = new StackEntry(ConstrainedReceiverValue(c, receiver.Expr), CppTypes.KindOf(c), ct);
                Push(StackKind.I4, "int32_t", CompareExpr(c, x, arg));
                return true;
            }
            // object::ToString on a primitive key — e.g. a generic field's
            // `Item1?.ToString` inside ValueTuple<...>.ToString. The JIT
            // devirtualizes the constrained callvirt to the primitive's typed
            // formatter; emit the same expression the System.Int32::ToString
            // intrinsic does, instead of reaching dn2cpp_object_tostring with the
            // raw managed pointer (which read garbage as an object → crash).
            case "ToString" when IsToStringablePrimitive(c):
            {
                // A placeholder receiver stands for an enum, whose ToString is
                // the member NAME — the underlying primitive's numeric
                // formatter would silently print the value instead.
                TaintIfCanonical(c, "enum-tostring");
                var receiver = Pop(); // managed pointer to the constrained primitive
                Push(StackKind.Ref, "Dn2CppString*", PrimitiveToStringExpr(c, ConstrainedReceiverValue(c, receiver.Expr)));
                return true;
            }
            // object::ToString on an enum value — `enumField.ToString` /
            // `enumLocal.ToString` lowers to `ldflda/ldloca; constrained. <enum>;
            // callvirt object::ToString`. Unlike a primitive, an enum formats by its
            // member NAME (or [Flags] decomposition), which the runtime recovers from a
            // boxed object's wired tostring slot / per-enum (name,value) table — so box
            // the value (copied from the constrained managed pointer, exactly as the
            // `box` opcode and a field getter do) and hand the boxed object to
            // dn2cpp_object_tostring, rather than passing the *unboxed* interior pointer:
            // the runtime would read the raw enum bits as a type-info and dereference
            // garbage → SIGSEGV. The matching explicit-box path (`object o = e;
            // o.ToString`) already round-trips through this runtime helper; this closes
            // the unboxed `constrained.` shape (the constrained token already marks the
            // enum reachable, so its ti_/tostring/(name,value) table are emitted). Without
            // this case the enum falls through to the Object::ToString intrinsic, which
            // pops the same pointer and passes it unboxed ( native build: this aborted
            // native dn2cpp mid-transpile — TypeDesc.ToString does `Primitive.ToString`
            // on a byte-underlying external enum field).
            case "ToString" when c is { Kind: TypeKind.Class, Class.IsEnum: true }:
            {
                // Only the parameterless `object::ToString` is the box-and-format
                // shape (one receiver on the stack). A `ToString(format)` /
                // `ToString(format, provider)` overload also lowers to a
                // `constrained. callvirt ToString` but carries extra args; popping just
                // the receiver here would corrupt the stack — leave those to the normal
                // constrained path (their existing behavior is unchanged).
                if (ConstrainedCalleeSig(handle).ParameterTypes.Length != 0)
                    return false;
                var receiver = Pop(); // managed pointer to the unboxed enum value
                string eti = TypeInfoExpr(c)
                    ?? throw new NotSupportedException(
                        $"{_method.DeclaringClass.FullName}.{_method.Name}: constrained ToString on enum {c} has no emitted type-info");
                // Read the receiver at the enum's real storage width (a narrowed
                // struct field / packed array slot may be 1 or 2 bytes) and widen
                // into an Of-width temp — the box payload convention is Of-width,
                // exactly what the box opcode writes.
                string ect = CppTypes.Of(c);
                string etmp = NewTemp(ect);
                Emit($"{etmp} = {ConstrainedReceiverValue(c, receiver.Expr)};");
                // The virtual entry point, like every other ToString() lowering: a
                // fresh box is never null, so the null arm cannot fire here, but the
                // rule that an explicit ToString() names _virtual is what keeps the
                // next arm added here from picking up the concat fold.
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_object_tostring_virtual(dn2cpp_box({eti}, &{etmp}, sizeof({ect})))");
                return true;
            }
            // object::ToString on a string key — String.ToString returns the
            // instance itself. The receiver is a managed pointer to the field/local
            // holding the Dn2CppString*; dereference it (no formatting).
            case "ToString" when c is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.String }:
            {
                var receiver = Pop(); // managed pointer to the constrained string
                Push(StackKind.Ref, "Dn2CppString*", $"(*(Dn2CppString**)({receiver.Expr}))");
                return true;
            }
            // object::ToString on a value-type struct that overrides it — e.g.
            // `(a, b).ToString` lowers to `constrained. ValueTuple<..> callvirt
            // object::ToString`. The receiver is a managed pointer to the struct;
            // call the struct's own override on it (ReachConstrainedImpl already
            // pulled the impl into the tree). A struct with no override falls
            // through (rare; its default name-formatting is not modelled).
            case "ToString" when c is { Kind: TypeKind.Class, Class: { IsValueType: true } vc }
                && Compilation.EffectiveToString(vc) is { } tsImpl:
            {
                var receiver = Pop(); // managed pointer to the constrained struct
                EmitCallResult(tsImpl, $"{DirectCallSym(tsImpl)}({ArgsWithRgctx(Cast(receiver, vc.CppStructName + "*"), tsImpl)})");
                return true;
            }
            // The same three Object-rooted virtuals on a struct that overrides NONE of
            // them — a corelib struct such as KeyValuePair<K,V>, whose base chain names
            // only System.ValueType's untranspilable bodies (Compilation.Effective*
            // reports those as "no override", so the arms above decline). Box the
            // receiver and dispatch through the runtime helper, the shape the enum arms
            // already use.
            //
            // The box is not incidental: without it the call falls through to the Object
            // intrinsic, which hands dn2cpp_object_* the raw `constrained.` byref UNBOXED,
            // and the helper reads the struct's first field as a type-info pointer.
            //
            // ToString is exact this way — dn2cpp_object_tostring answers an unwired
            // tostring slot with the type's full name, which IS ValueType.ToString().
            // GetHashCode and Equals are right only once the type-info equals/gethashcode
            // slots carry the struct's structural equality; an unwired slot falls back to
            // the identity hash and reference equality, the same answer a struct boxed by
            // any other route gets, so these arms inherit that gap rather than widening it.
            //
            // Byref-like is excluded throughout: a ref struct cannot be boxed at all, so
            // there is nothing to dispatch (ReachAllocatedType skips it for the same
            // reason). The ones that matter — Span<T> and friends — override all three
            // themselves, so the arms above claim them before these.
            case "GetHashCode" when c is { Kind: TypeKind.Class, Class: { IsValueType: true, IsByRefLike: false } }:
            {
                var receiver = Pop(); // managed pointer to the constrained struct
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_gethashcode({BoxedConstrainedReceiver(c, receiver)})");
                return true;
            }
            // The typed IEquatable<T>::Equals(!0) shape is left to EmitConstrainedCall,
            // which devirtualizes it to the struct's Equals(T) — only the Object-rooted
            // Equals(object) boxes (its argument is already an object on the IL stack).
            case "Equals" when c is { Kind: TypeKind.Class, Class: { IsValueType: true, IsByRefLike: false } }
                && !IsTypedItfConstrained(handle, "System.IEquatable"):
            {
                var other = Pop();
                var receiver = Pop(); // managed pointer to the constrained struct
                string boxed = BoxedConstrainedReceiver(c, receiver);
                Push(StackKind.I4, "int32_t", $"dn2cpp_object_equals({boxed}, {Cast(other, "Dn2CppObject*")})");
                return true;
            }
            // Parameterless object::ToString only — a ToString(format[, provider])
            // overload lowers to a constrained. callvirt too but carries extra args, and
            // popping just the receiver would corrupt the stack (same guard as the enum
            // arm).
            case "ToString" when c is { Kind: TypeKind.Class, Class: { IsValueType: true, IsByRefLike: false } }
                && ConstrainedCalleeSig(handle).ParameterTypes.Length == 0:
            {
                var receiver = Pop(); // managed pointer to the constrained struct
                Push(StackKind.Ref, "Dn2CppString*", $"dn2cpp_object_tostring_virtual({BoxedConstrainedReceiver(c, receiver)})");
                return true;
            }
            default:
                return false;
        }
    }

    private static bool IsDefaultNameExternalIntrinsic(TypeDesc t) =>
        t is { Kind: TypeKind.External,
            ExternalName: "System.Threading.Tasks.ParallelLoopResult" };

    /// <summary>The decoded signature of a <c>constrained.</c> callvirt's callee, from
    /// either token shape — used to tell the parameterless <c>object::ToString()</c> apart
    /// from the <c>ToString(format[, provider])</c> overloads that lower the same way.</summary>
    private MethodSignature<TypeDesc> ConstrainedCalleeSig(EntityHandle handle) =>
        handle.Kind == HandleKind.MemberReference
            ? _reader.GetMemberReference((MemberReferenceHandle)handle).DecodeMethodSignature(_c.SigProvider, _method.Context)
            : _reader.GetMethodDefinition((MethodDefinitionHandle)handle).DecodeSignature(_c.SigProvider, _method.Context);

    /// <summary>Boxes a <c>constrained.</c> value-type receiver — a managed pointer to the
    /// raw struct — into the real object header the <c>dn2cpp_object_*</c> helpers expect.
    /// Spills the payload into an Of-width temp first (what the box opcode itself writes),
    /// so a receiver pointing at packed storage is read at its true width.</summary>
    private string BoxedConstrainedReceiver(TypeDesc c, StackEntry receiver)
    {
        string ti = TypeInfoExpr(c)   // taints a canonical body: no baked owner type-info
            ?? throw new NotSupportedException(
                $"{_method.DeclaringClass.FullName}.{_method.Name}: constrained call on {c} has no emitted type-info");
        string ct = CppTypes.Of(c);
        string tmp = NewTemp(ct);
        Emit($"{tmp} = {ConstrainedReceiverValue(c, receiver.Expr)};");
        return $"dn2cpp_box({ti}, &{tmp}, sizeof({ct}))";
    }

    /// <summary>The concrete TSelf of a <c>constrained. call</c> to a static-abstract
    /// interface member, as the class <see cref="Compilation.ResolveStaticVirtualImpl"/>
    /// probes for the implementing static body. Value-type structs pass through
    /// unchanged. A concrete reference-type class (statically known at
    /// monomorphization) passes too — its resolved impl is a real static body, called
    /// directly like a struct's. An interface passes for the same reason: C# admits
    /// an interface type argument against a static-abstract constraint only when the
    /// interface itself carries the explicit static impls, and the resolver probes its
    /// own ExplicitInterfaceImpls (a no-impl interface resolves to null and falls
    /// through). Placeholder-bearing (canonical-shared) classes stay out, tainting
    /// the shared trial at the call site so each real instantiation compiles its own
    /// body. Arrays, strings and other non-Class
    /// TypeDesc shapes never match. TSelf closed to double/float maps to the CoreLib
    /// Double/Single struct so the plain-named public static impls (Sqrt, Sin, MaxMagnitude,
    /// IsInteger, …) resolve to the intrinsic math surface; the struct is
    /// intrinsic-mapped, so the resolver's guards keep default-body members and
    /// dotted-named explicit impls (the IEEE constants, TryConvert*) on the
    /// generic-math-table path, and the caller tries that table first so the
    /// plain-named op_* operator impls keep their table lowering too. The eight
    /// integer primitives map to their CoreLib structs the same way: the caller's
    /// table-first order keeps the operator/constant/predicate lowering, and a
    /// table miss resolves the plain-named statics (Parse/TryParse onto the
    /// NumberStyles-engine intrinsics via the probe, Clamp/Max/… onto the sub-word
    /// types' real transpiled bodies) — a probe miss falls through to the member's
    /// default interface body or the loud no-intrinsic-mapping diagnostic. A width-preserving canonical
    /// placeholder stays out (the resolved impl would differ per real
    /// instantiation) and keeps the instantiation-independent table-only route, as
    /// do nint/nuint (their statics have no intrinsic arms yet — the loud
    /// no-intrinsic-mapping diagnostic).</summary>
    private ClassInfo? ConstrainedStaticVirtualSelf() => _constrained switch
    {
        { Kind: TypeKind.Class, Class: { IsValueType: true } c } => c,
        { Kind: TypeKind.Class, Class: { IsValueType: false } c }
            when !Compilation.ContainsCanonPlaceholder(c) => c,
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Double } => _c.FindClassByFullName("System.Double"),
        { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Single } => _c.FindClassByFullName("System.Single"),
        { Kind: TypeKind.Primitive, Primitive: var pc, IsCanonPlaceholder: false }
            when CoreIntrinsics.PrimitiveIntegerFullName(pc) is { } pin => _c.FindClassByFullName(pin),
        _ => null,
    };

    /// <summary>The <c>System.*</c> declaring name to route an interface-mediated
    /// <c>INumberBase&lt;TSelf&gt;.Create*&lt;TOther&gt;</c> through the generic-math
    /// conversion intrinsic, or null when the (TSelf, TOther) pair is outside the
    /// shapes that intrinsic lowers (mirroring its integer-target and
    /// float/double-target guards exactly, so the fall-through never reaches its
    /// hard NotSupportedException).</summary>
    private static string? CreateTargetPrimitiveName(TypeDesc tself, TypeDesc tother)
    {
        // Int128/UInt128 are transpiled STRUCTS (TypeKind.Class), not primitives, so
        // they never appear in the tself.Primitive switch below. Over an integer-
        // primitive source they route to the same TranslateGenericIntrinsic Int128
        // widening the call-site (MethodSpec) pair uses (GenericIntrinsic.cs). A non-
        // integer source falls through to that arm's own loud NotSupportedException.
        if (tself is { Kind: TypeKind.Class, Class.FullName: "System.Int128" or "System.UInt128" } i128
            && (IsIntegerPrimitive(tother.Primitive) || tother.Primitive == PrimitiveTypeCode.Char))
            return i128.Class!.FullName;
        return tself.Primitive switch
        {
            _ when IsIntegerPrimitive(tself.Primitive)
                   && (IsIntegerPrimitive(tother.Primitive) || tother.Primitive == PrimitiveTypeCode.Char)
                => CoreIntrinsics.PrimitiveIntegerFullName(tself.Primitive),
            PrimitiveTypeCode.Single => "System.Single",
            PrimitiveTypeCode.Double => "System.Double",
            _ => null,
        };
    }

    /// <summary>Whether a constrained <c>ToString</c> on this primitive can be
    /// devirtualized to a typed formatter. Enums (name lookup) and the
    /// pointer-sized integers are out of scope and fall through.</summary>
    private static bool IsToStringablePrimitive(TypeDesc c) => c.Kind == TypeKind.Primitive && c.Primitive is
        PrimitiveTypeCode.Int32 or PrimitiveTypeCode.UInt32 or PrimitiveTypeCode.Int16 or PrimitiveTypeCode.UInt16
        or PrimitiveTypeCode.Byte or PrimitiveTypeCode.SByte or PrimitiveTypeCode.Int64 or PrimitiveTypeCode.UInt64
        or PrimitiveTypeCode.Double or PrimitiveTypeCode.Single or PrimitiveTypeCode.Boolean or PrimitiveTypeCode.Char;

    /// <summary>The type-specialized <c>ToString</c> for a primitive value, matching
    /// each type's <c>System.&lt;T&gt;::ToString</c> intrinsic. Assumes
    /// <see cref="IsToStringablePrimitive"/> already vetted the code.</summary>
    private static string PrimitiveToStringExpr(TypeDesc c, string v) => c.Primitive switch
    {
        PrimitiveTypeCode.Int64 => $"dn2cpp_long_to_string((int64_t)({v}))",
        // UInt64/UInt32 must format unsigned — the signed formatters would print
        // values above the signed max as negative (e.g. uint.MaxValue as -1).
        PrimitiveTypeCode.UInt64 => $"dn2cpp_format_uint((uint64_t)({v}), 8, nullptr)",
        PrimitiveTypeCode.UInt32 => $"dn2cpp_format_uint((uint64_t)(uint32_t)({v}), 4, nullptr)",
        PrimitiveTypeCode.Double => $"dn2cpp_double_to_string((double)({v}))",
        PrimitiveTypeCode.Single => $"dn2cpp_float_to_string((float)({v}))",
        PrimitiveTypeCode.Boolean => $"dn2cpp_bool_to_string((int32_t)({v}))",
        PrimitiveTypeCode.Char => $"dn2cpp_char_to_string((char16_t)({v}))",
        _ => $"dn2cpp_int_to_string((int32_t)({v}))",
    };

    /// <summary>Whether a constrained-callvirt <paramref name="handle"/> names a member
    /// of an instantiated 1-arg generic interface whose GenericDefFullName is
    /// <paramref name="itfName"/> (<c>System.IEquatable</c> / <c>System.IComparable</c>) —
    /// i.e. the typed <c>Equals(!0)</c>/<c>CompareTo(!0)</c> whose IL argument is the
    /// unboxed T. The non-generic <c>System.IComparable</c> resolves through a TypeRef
    /// (no TypeSpec) and never matches.</summary>
    private bool IsTypedItfConstrained(EntityHandle handle, string itfName)
    {
        if (handle.Kind != HandleKind.MemberReference)
            return false;
        var mr = _reader.GetMemberReference((MemberReferenceHandle)handle);
        if (mr.Parent.Kind != HandleKind.TypeSpecification)
            return false;
        var parent = _reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent)
            .DecodeSignature(_c.SigProvider, _method.Context);
        return parent is { Kind: TypeKind.Class, Class: { IsInterface: true } pi }
            && pi.Context.TypeArgs.Length == 1
            && _c.GenericDefFullName(pi) == itfName;
    }

    /// <summary>Constant-folds a direct call whose callee body is exactly
    /// <c>ldc.i4*|ldc.i8 &lt;k&gt;; ret</c> — a trivially-constant getter/method
    /// (e.g. <c>int BUCKET_BITS =&gt; 17;</c>) — into the integer literal, so a hot
    /// per-call getter is not emitted as an out-of-line cross-TU call. On success
    /// <paramref name="literal"/> is the C++ literal cast to the callee's declared
    /// return type (e.g. <c>((int32_t)17)</c>).
    ///
    /// Safety conditions:
    /// (1) the return type is a primitive integer (its C++ spelling is the literal's;
    ///     enums/floats/pointers/refs are out of scope);
    /// (2) the declaring class has no static constructor, so the fold cannot skip a
    ///     type-initialization trigger — dn2cpp actually runs every .cctor eagerly at
    ///     startup regardless of call sites (the plain direct-call path emits no cctor
    ///     guard), so ordering is unaffected either way, but this stays conservative;
    /// (3) not taken under <see cref="SharedTrial"/>, where the callee resolution
    ///     could be instantiation-dependent (the literal itself is instantiation-
    ///     independent, but "when in doubt for shared bodies, don't fold");
    /// (4) the callee's own IL body (its module, its RVA) is exactly two instructions
    ///     with no exception regions.
    /// Discarding the already-popped receiver/arguments is side-effect-free: every
    /// stack value is materialized into a temp at its own <see cref="Push"/> time, so
    /// all side effects (bounds checks, field reads, sub-calls) were already emitted
    /// before this call site; the popped strings are just those temp names. A
    /// <c>ldc;ret</c> body never dereferences its receiver, so folding also matches
    /// the direct-call path's own behavior (it emits no explicit null check).</summary>
    private bool TryFoldTrivialConstBody(MethodInfo callee, out string literal)
    {
        literal = "";
        // Shared canonical bodies: callee resolution may vary per instantiation.
        if (SharedTrial)
            return false;
        // The callee must carry its own IL body.
        if (callee.Rva == 0)
            return false;

        var ret = callee.Signature.ReturnType;
        if (ret.Kind != TypeKind.Primitive)
            return false;
        string cppRet = CppTypes.Of(ret);
        if (cppRet is not ("int32_t" or "uint32_t" or "int64_t" or "uint64_t"))
            return false;

        // Never fold across a type-initialization trigger.
        if (callee.DeclaringClass.StaticCctor is not null)
            return false;

        MethodBodyBlock body;
        try
        {
            // The callee may live in a different module than the current method,
            // so decode against its own owning module's PE image.
            body = callee.Module.PE.GetMethodBody(callee.Rva);
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return false;
        }
        if (!body.ExceptionRegions.IsEmpty)
            return false;

        List<Instruction> insns;
        try
        {
            insns = ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat());
        }
        catch (Exception e) when (!Compilation.IsMustEscape(e))
        {
            return false;
        }
        if (insns.Count != 2 || insns[1].OpCode != ILOpCode.Ret)
            return false;

        var ld = insns[0];
        long value;
        switch (ld.OpCode)
        {
            case ILOpCode.Ldc_i4_m1: value = -1; break;
            case ILOpCode.Ldc_i4_0: value = 0; break;
            case ILOpCode.Ldc_i4_1: value = 1; break;
            case ILOpCode.Ldc_i4_2: value = 2; break;
            case ILOpCode.Ldc_i4_3: value = 3; break;
            case ILOpCode.Ldc_i4_4: value = 4; break;
            case ILOpCode.Ldc_i4_5: value = 5; break;
            case ILOpCode.Ldc_i4_6: value = 6; break;
            case ILOpCode.Ldc_i4_7: value = 7; break;
            case ILOpCode.Ldc_i4_8: value = 8; break;
            case ILOpCode.Ldc_i4_s:
            case ILOpCode.Ldc_i4:
            case ILOpCode.Ldc_i8:
                value = ld.Operand;
                break;
            default:
                return false;
        }

        // A 64-bit literal fits only a 64-bit return, and a 32-bit ldc only a
        // 32-bit return. Valid IL always agrees; guard against a truncating cast.
        bool ret64 = cppRet is "int64_t" or "uint64_t";
        bool ld64 = ld.OpCode == ILOpCode.Ldc_i8;
        if (ret64 != ld64)
            return false;

        literal = $"(({cppRet})({value}))";
        return true;
    }

    /// <summary>Wraps the NFI-typed arguments (CultureInfo/NumberFormatInfo/
    /// TextInfo — the headerless intrinsic pointer) of a virtual/interface
    /// dispatch whose bound implementation reads the position ERASED
    /// (Dn2CppObject*): the vtable/interface slots of a grouped instantiation
    /// hold the shared canonical body, which treats such an argument as a real
    /// managed object (equality, stores into erased slots), so a raw pointer
    /// must not pun into it — it gets the interned wrapper, like every other
    /// NFI→object boundary. The erased spelling is read off the shared donor
    /// (vtable) / the canonical interface's same-slot method (interface); a
    /// concrete NFI position (a literal IFormatProvider parameter, not a
    /// substituted T) spells NFI on the donor too and stays raw. An array
    /// receiver's dispatch map thunk declares the concrete element type and
    /// re-wraps tolerantly, so a wrapped argument passes through it unchanged.</summary>
    private void NfiWrapErasedCallArgs(MethodInfo callee, IList<string> args)
    {
        MethodInfo? erased = null;
        if (callee.DeclaringClass.IsInterface)
        {
            var citf = _c.CanonicalInterfaceOf(callee.DeclaringClass);
            if (citf is not null && !ReferenceEquals(citf, callee.DeclaringClass))
                erased = citf.Methods.FirstOrDefault(m =>
                    m.Name == callee.Name && m.VtableSlot == callee.VtableSlot);
        }
        else
        {
            var donor = _c.SharedDonor(callee).Emittable;
            if (!ReferenceEquals(donor, callee))
                erased = donor;
        }
        if (erased is null)
            return;
        var real = callee.Signature.ParameterTypes;
        var er = erased.Signature.ParameterTypes;
        int off = args.Count - real.Length;
        for (int i = 0; i < real.Length && i < er.Length; i++)
            if (IsHeaderlessWrapCpp(CppTypes.Of(real[i])) && CppTypes.Of(er[i]) == "Dn2CppObject*")
                // The trailing cast re-spells the wrapper at the concrete headerless
                // parameter type (NFI, or Assembly/Module's const char*): the
                // dispatch expression's function-pointer cast (FnPtrType) is built
                // from the CONCRETE signature, while the bound body reads the slot
                // as Dn2CppObject* — pointer-identical, so only the C++ spelling
                // needs to agree.
                args[off + i] = $"(({CppTypes.Of(real[i])})(void*)"
                    + $"{HeaderlessWrapExpr(args[off + i], CppTypes.Of(real[i]), real[i])})";
    }

    private void EmitCallResult(MethodInfo callee, string call)
    {
        if (callee.Signature.ReturnType.IsVoid)
        {
            Emit(call + ";");
            return;
        }
        string retC = CppTypes.Of(callee.Signature.ReturnType);
        // An NFI return (CultureInfo/NumberFormatInfo/TextInfo/IFormatProvider —
        // the headerless intrinsic pointer): an implementation bound through a
        // shared canonical body or an erased vtable/interface dispatch hands
        // back the interned WRAPPER (erased slots hold the managed-object
        // form), which must not pun into the raw pointer at the call site —
        // unwrap it. The unwrap is tolerant (a raw pointer from a body whose
        // declared return type is NFI passes through unchanged), so this one
        // arm covers every dispatch shape without knowing which body answers.
        if (IsHeaderlessWrapCpp(retC))
        {
            // Assembly/Module returns (`const char*`) take the same tolerant unwrap
            // through their own funnel (dn2cpp_asm_unwrap), for the same reason.
            call = HeaderlessUnwrapExpr(call, retC);
        }
        else
        {
            // A call bound to a shared canonical body returns the erased type
            // (a reference return collapsed to Dn2CppObject*); cast it back to the
            // call site's static type. Non-pointer returns never differ (layout
            // identity is the grouping invariant).
            var donor = _c.SharedDonor(callee).Emittable;
            if (!ReferenceEquals(donor, callee)
                && retC.EndsWith("*", StringComparison.Ordinal)
                && CppTypes.Of(donor.Signature.ReturnType) != retC)
                call = $"(({retC})({call}))";
        }
        Push(CppTypes.KindOf(callee.Signature.ReturnType), retC, call);
        // Thread the declared return type as the result's StaticType so a List<T>
        // arriving directly from a call (e.g. seq.ToList) is recognised by
        // TryListBacking — lifting the call-result boundary for
        // string.Join/Concat without first binding to a local. SZArray is
        // included so a raw `T[]` returned from a call/indexer (e.g.
        // `List<byte[]>.get_Item(i)`, whose `byte[]` the shim Enumerable.Select
        // hands to its `IEnumerable<T>` parameter) carries its SZArray static type —
        // otherwise CoerceTo can't see the array→IEnumerable<T> flow and never wires
        // the array's SZArray interface-dispatch map, so the `<Select>d__1<…>`
        // iterator's `callvirt IEnumerable<T>.GetEnumerator` traps in
        // dn2cpp_resolve_interface (the call-result analogue of the `Ldfld`
        // static-type broadening; locals/args thread it via PushVar).
        if (callee.Signature.ReturnType is { Kind: TypeKind.Class or TypeKind.SZArray })
            _stack[^1] = _stack[^1] with { StaticType = callee.Signature.ReturnType };
    }
}
