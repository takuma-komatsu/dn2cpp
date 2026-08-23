using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using SRME = System.Reflection.Metadata.Ecma335.MetadataTokens;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    // ---- reachability: the reach fixpoint's edge producers and body scan ----

    private void Reach(MethodInfo m)
    {
        // A minted body (a value type's structural equality/hash) has no IL: scanning it
        // would ask the PE for a MethodBody at a row that does not exist. Its call edges
        // are not discovered, they are CONSTRUCTED — ReachSynthesizedValueEquality walks
        // the fields and reaches what the field walk will call — so admitting it to the
        // set is the whole of reaching it.
        if (m.IsSynthetic)
        {
            Reachable.Add(m);
            return;
        }
        // A System.Net.Http method whose body dn2cpp replaces: the SocketsHttpHandler transport
        // overrides (Send/SendAsync/Dispose -> the SetupHandlerChain -> connection pool ->
        // socket/DNS/TLS/QUIC subtree) and HttpClientHandler's ClientCertificateOptions
        // accessors (the setter -> CertificateHelper -> X509Store -> keychain -> ASN.1 ->
        // BigInteger; the getter so it answers the value the replaced setter accepted rather
        // than the never-corrected Automatic default), and the system-proxy lookup behind
        // HttpClient.DefaultProxy (MacProxy's two IWebProxy queries -> the CFNetwork
        // P/Invokes; HttpWindowsProxy's ctor plus the same pair -> WinHttp/IpHlpApi/Advapi32
        // and a ThreadPool registered wait — neither of which this build links; the queries
        // answer "no proxy", which is what the transport actually does). Keep the
        // method reachable — so RenderVtable gives a virtual slot a real pointer and the
        // abstract HttpMessageHandler dispatch lands on an emitted body — but do NOT enqueue it
        // for scanning, so the subtree under its real IL never enters the tree. cut ⟹ route:
        // the emit route emits the synthesized body under this method's own symbol, so no cut
        // edge is left dangling.
        if (CoreIntrinsics.BrHttpShim.Matches(m.DeclaringClass.FullName, m.Name))
        {
            if (Reachable.Add(m))
            {
                _predTrace.TryAdd(m, _currentScan);
                m.EnsureSignature();
                ReachHttpShimEdges(m); // reach DnHttpBackend.Send/SendAsync the shim body names
            }
            return; // no _toScan.Enqueue -> the replaced IL is never scanned
        }
        // System.Enum's instance ToString/GetTypeCode + the value trio Equals/GetHashCode/
        // CompareTo + the private GetValue: body replaced with a synthesized one
        // (CompileEnumInstanceFormatBody) that lowers to dn2cpp_object_tostring /
        // dn2cpp_enum_format / dn2cpp_type_get_type_code / dn2cpp_object_gethashcode /
        // dn2cpp_object_equals / dn2cpp_enum_compareto / dn2cpp_enum_get_value. Keep the
        // method reachable — so the enum's IFormattable/IConvertible/IComparable itable slot
        // (RenderInterfaceTable) and the direct `callvirt System.Enum::ToString()`/`::CompareTo`
        // both land on the emitted body — but do NOT enqueue the real IL: its ToStringInlined ->
        // GetEnumInfo -> GetEnumValuesAndNames (InternalCall) + RuntimeType-cache cascade,
        // and — the value trio and GetValue — the InternalGetCorElementType switch into the
        // underlying primitive's inline-only GetHashCode/CompareTo (or a per-CorElementType box
        // arm), must never enter the tree. GetValue is the boxer every IConvertible.To* override
        // calls, so replacing it closes that whole family (each To* keeps its real
        // Convert.ToXxx(GetValue(), …) body). The synthesized body names only runtime helpers, so
        // unlike BrHttpShim there is no managed edge to reach.
        if (CoreIntrinsics.BrEnumInstanceFormat.Matches(m.DeclaringClass.FullName, m.Name))
        {
            if (Reachable.Add(m))
            {
                _predTrace.TryAdd(m, _currentScan);
                m.EnsureSignature();
            }
            return; // no _toScan.Enqueue -> the replaced IL is never scanned
        }
        if (m.Rva == 0)
            return;
        // Methods of an intrinsic-mapped type are emitted inline, never
        // transpiled. Call edges already skip them (ResolveCallTarget), but an
        // allocated subtype's vtable can still surface an inherited intrinsic
        // slot — e.g. `throw new NotSupportedException(...)` would reach
        // System.Exception.GetObjectData (serialization → Dictionary → randomized
        // hashing → a P/Invoke). Cut it here so that closure never enters the
        // tree (generalizes the intrinsic-edge cut to the alloc path). The
        // explicitly-transpiled exemptions (String's interface impls) pass through.
        if (CoreIntrinsics.IsIntrinsicType(m.DeclaringClass.FullName)
            && !_intrinsicTypeTranspiled.Contains(m))
            return;
        // An intrinsic-mapped type whose bare full name isn't in s_intrinsicTypes —
        // a nested intrinsic value type (Lock.Scope, or the StringBuilder interpolated
        // -string handler), or an adopted async task type. It is emitted inline, never
        // transpiled, so its members are never a reachability edge even on the
        // alloc/vtable path. This guard is also what makes ResolveCallTarget's
        // MethodDefinition arm sound with only the name-table test: a call edge that
        // arm resolves into such a type dies right here, before any IL is scanned, so
        // the narrower cut there costs nothing. Do not narrow this to the name table —
        // the two-part test here is the backstop.
        if (m.DeclaringClass.IntrinsicCppName is not null)
            return;
        // Bounded methods (dead-but-statically-reachable BCL paths) are cut here;
        // their call sites emit a trap instead of a call. Through the instance merge,
        // which asks the core row (CoreIntrinsics.BdCoreBounded) and unions the
        // backend's and the CLI's sets — see Compilation.IsBoundedMethod.
        if (IsBoundedMethod(m.DeclaringClass.FullName, m.Name))
        {
            // Except the two base-Stream async funnels, whose call site is REWRITTEN to a
            // synchronous dispatch through the sync Read/Write slot rather than neutralized
            // (MethodCompiler.TryEmitStreamSyncOverAsyncFunnel). The body stays cut — that
            // is the point of the bound — but the slot the rewrite dispatches through has
            // to be REACHED, and in a program that only ever awaits its stream nothing else
            // names it. Unreached, the override is never emitted and the abstract slot
            // stays a null vtable entry: the same crash by a different road.
            if (CoreIntrinsics.BdStreamSyncFunnel.Matches(m.DeclaringClass.FullName, m.Name)
                && StreamSyncSlot(m.DeclaringClass, read: m.Name == "BeginEndReadAsync") is { } slot)
                ReachUsedVirtual(slot);
            return;
        }
        // The dynamic-code-generation surface (Reflection.Emit, DLR CallSite,
        // Expression.Compile) is cut generically: any library referencing it gets
        // the cut for free (the JIT-backed closures behind it — the
        // Linq.Expressions interpreter, ILGenerator — never enter the tree), and
        // every call site throws a catchable PlatformNotSupportedException, the
        // NativeAOT posture.
        if (IsDynamicCodegenMember(m.DeclaringClass, m.Name))
            return;
        // The socket / name-resolution platform layer is absent, so its whole live surface
        // is cut generically and every site throws a catchable PlatformNotSupportedException
        // (CoreIntrinsics.BdAbsentNetworkPal). This is the cut that has to happen HERE
        // rather than at a gap: libSystem.Native is a runtime-provided P/Invoke module, so a
        // socket import that entered the tree would lower to a direct native call to a
        // symbol nothing defines — the failure would be an undefined symbol at C++ link
        // time, which --measure cannot see at all (it emits no C++).
        if (IsAbsentNetworkPalMember(m.DeclaringClass, m.Name))
            return;
        // AssemblyLoadContext runtime-load internals + the GetLoadedAssemblies
        // enumeration query. Their CALL SITES are handled at emit
        // (MethodCompiler.EmitManagedCall — a trap for the load primitives, an empty
        // array for GetLoadedAssemblies), so their real bodies must not enter the tree.
        // GetLoadedAssemblies in particular carries a QCall-marshalling body
        // (ObjectHandleOnStack over GetLoadedAssembliesInternal) that would otherwise be
        // scanned and mis-emitted; cut it here, the cut half of that route-with-cut.
        if (CoreIntrinsics.IsAssemblyLoadContextRuntimeLoad(m.DeclaringClass.FullName, m.Name)
            || (m.DeclaringClass.FullName == "System.Runtime.Loader.AssemblyLoadContext"
                && m.Name == "GetLoadedAssemblies"))
            return;
        if (Reachable.Add(m))
        {
            _predTrace.TryAdd(m, _currentScan);
            // Reached => decoded. This adds no decode — a reachable method's signature is
            // spelled by its prototype, its body and its reflection row, so the decode was
            // always going to be paid; it MOVES it, to here. Which buys two things. It lands
            // inside DrainReachability, the one window where the pending-shape queue is
            // provably emptied before anything spells a type. And it makes the emitter's walk
            // over Reachable a pure read again — the walk that decides the emit set, which
            // must not be growing the model while it decides.
            m.EnsureSignature();
            // --trim-godot-classes release trigger: a REACHED method's signature
            // names the engine wrappers it can traffic in, and its declaring class
            // is a wrapper someone calls into. User code contributes its overrides
            // and helpers (`_Input(InputEvent e)`, a method returning a Node) — and
            // GodotSharp's own reached methods are deliberately NOT exempt here,
            // unlike every body-level trigger: a generated binding CASTS its return
            // value to its static return type (`(Viewport)` inside Node.GetViewport,
            // `(TimeInstance)` inside the singleton accessor), so the wrapper the
            // redirect materializes for an engine-returned object must derive from
            // that return type — i.e. the type must be released. This stays
            // demand-driven, not a cascade: a GodotSharp method is only reached
            // when something calls it, releasing a wrapper reaches only its
            // lambda/ctor/cctor (whose signatures name no further wrappers), and
            // the wrapper's own method surface stays unreached.
            if (_godotClassTrim is not null)
            {
                if (TrimEligibleClass(m.DeclaringClass))
                    TrimRelease(m.DeclaringClass);
                TrimNoteNamedType(m.Signature.ReturnType);
                foreach (var p in m.Signature.ParameterTypes)
                    TrimNoteNamedType(p);
            }
            _toScan.Enqueue(m);
            // Canonical shared generics: a grouped specialization's method is a
            // sharing candidate, so its owner counterpart's body must be
            // scanned/compiled too. (Links made after this reach are caught up
            // by SyncSharedGenerics.)
            if (SharedGenericsEnabled)
                ReachSharedCounterpart(m);
        }
    }

    /// <summary>Records a type as allocated and reaches the implementations of
    /// any virtual/interface slots already known to be dispatched.
    ///
    /// A ref struct (byref-like) can never be boxed, so its virtual slots are
    /// never dispatched — skip it entirely (its instance methods arrive as direct
    /// call edges). All other types (reference and value) use used-slot
    /// precision (see <see cref="_usedVirtualDecls"/>): an override is reached only
    /// when the type is allocated **and** some callvirt/constrained-call actually
    /// dispatches that slot. Eagerly reaching a value type's whole vtable drags
    /// dead overrides into the tree (e.g. KeyValuePair&lt;K,V&gt;.ToString ->
    /// string interpolation -> Buffer.Memmove InternalCall) — and a value type's
    /// type-info carries a null vtable anyway, so its slots are only ever reached
    /// through a recorded dispatch, not boxed indirection (generalizes the
    /// used-slot model to value types).</summary>
    private void ReachAllocatedType(ClassInfo c)
    {
        EnsureCompleted(c);   // vtable + the Effective* overrides an instance dispatches
        // --trim-godot-classes release triggers: a user-code allocation of a wrapper
        // and a user class's engine base chain. BEFORE the dedup below on purpose —
        // a wrapper first allocated GodotSharp-internally must still release when
        // user code later allocates it.
        if (_godotClassTrim is not null)
            TrimNoteAllocated(c);
        ReachCctor(c);
        if (c.IsByRefLike)
            return;
        if (!_allocatedRefTypes.Add(c))
            return;
        foreach (var decl in _usedVirtualDecls)
            ReachVirtualImpl(c, decl);
        // A newly-allocated type also contributes its override to every already-used
        // generic virtual method (the GVM half of the used×allocated cross product).
        foreach (var disp in _usedGvms.Values.ToList())
            ReachGvmImpl(disp, c);
        // A user comparer `class MyCmp : EqualityComparer<T>` inherits IEqualityComparer<T>
        // from its intrinsic-opaque base, whose shape decode (CompleteShape) short-circuits
        // on the intrinsic map before ever populating .Interfaces — so the base chain never
        // surfaces the interface, MyCmp would emit no interface map, and a Dictionary/HashSet
        // key path or an EqualityComparer<T>-typed call would silently fall back to default
        // equality — a wrong answer with no diagnostic. Give the concrete
        // subclass the closed IEqualityComparer<T> it genuinely implements, and reach that
        // interface's Equals/GetHashCode as used virtuals so this — and every allocated
        // sibling's — overrides land in the slots RenderItfTables builds. The dispatch itself
        // is emitted in MethodCompiler (TryEmitComparerDispatch / the EqualityComparer arms).
        if (!c.IsValueType && EqualityComparerElement(c) is { } eqElem
            && IEqualityComparerInterfaceFor(eqElem) is { } eqItf)
        {
            if (!c.Interfaces.Contains(eqItf))
                c.Interfaces.Add(eqItf);
            // ImplementsInterface caches c's interface closure, and the used×allocated
            // cross above already computed and froze it WITHOUT this interface. Drop the
            // stale cache so ReachVirtualImpl's ImplementsInterface check — and every
            // later dispatch reacher — sees the interface just added.
            c.InterfaceClosureCache = null;
            // Record that this element type's IEqualityComparer<T> type-info is now emitted
            // (RenderItfTables lays it down as c's interface). The concrete form AND its
            // canonical alias, so the emit-time dispatch gate resolves for both a concrete
            // and a shared-body (canonical) receiver.
            _anyUserComparerSubclass = true;
            _userComparerInterfaces.Add(eqItf);
            if (CanonicalInterfaceOf(eqItf) is { } eqCanon)
                _userComparerInterfaces.Add(eqCanon);
            eqItf.EnsureMembers();
            foreach (var m in eqItf.Methods)
                if (!m.IsStatic && m.Name is "Equals" or "GetHashCode")
                {
                    // Mark the interface method used so a sibling comparer allocated LATER
                    // crosses it too — but that alone cannot reach THIS receiver's impl:
                    // a Dictionary<T,…> body scanned earlier may already have marked the
                    // method used (before c gained the interface just above), so
                    // ReachUsedVirtual no-ops and the used×allocated cross never revisits c.
                    // Reach c's own impl directly — order-independent and idempotent.
                    ReachUsedVirtual(m);
                    ReachVirtualImpl(c, m);
                }
        }
        // Any allocated type carrying a closed IEqualityComparer<T> in its
        // interface set — a direct `class MyCmp : IEqualityComparer<T>`, a boxed
        // struct comparer (RenderItfTables gives it unboxing-thunk slots), not only
        // the EqualityComparer<T> subclasses the arm above hands their invisible
        // inherited interface (it adds that to c.Interfaces before this walk, so
        // they land here too) — is a dispatchable receiver for the MemoryExtensions
        // span scans' trailing comparer operand. Register the interface (and its
        // canonical alias) so UserComparerInterfaceMethod knows dispatch through it
        // is link-safe: this type's RenderItfTables row is what emits the
        // interface's type-info the probe names.
        for (var b = c; b is not null; b = b.BaseClass)
            foreach (var itf in b.Interfaces)
                if (GenericDefFullName(itf) == "System.Collections.Generic.IEqualityComparer"
                    && itf.Context.TypeArgs.Length == 1)
                {
                    _anyUserComparerSubclass = true;
                    _userComparerInterfaces.Add(itf);
                    if (CanonicalInterfaceOf(itf) is { } eqCanonItf)
                        _userComparerInterfaces.Add(eqCanonItf);
                }
        // An overridden ToString must be reachable so Object.ToString (used by
        // Concat(object)/Join/interpolation/Console/explicit calls) can dispatch
        // it via the type-info `tostring` field rather than formatting the type
        // name. Reference types only — a boxed value type's payload sits at
        // a different offset, so its ToString is not wired here.
        if (!c.IsValueType && EffectiveToString(c) is { } ts)
            Reach(ts);
        // Likewise the GetHashCode/Equals overrides, so dn2cpp_object_gethashcode /
        // _equals can dispatch them (records/classes as HashSet/Dictionary keys and
        // a direct obj.GetHashCode()/Equals call). Reference types only — a boxed
        // value type's payload sits at a different offset and eagerly reaching a
        // struct's overrides drags heavy BCL IL in, so those stay use-site gated
        // (same rationale as the ToString reach above). **Delegates are
        // excluded:** every allocated delegate would otherwise reach the inherited
        // MulticastDelegate.GetHashCode/Equals, which pull System.HashCode.Combine ->
        // Interop.GetRandomBytes (an unsupported InternalCall). Delegates instead
        // hash/compare through the DN2CPP_TF_DELEGATE branch of
        // dn2cpp_object_gethashcode/_equals (chain-aware value semantics).
        if (!c.IsValueType && !c.IsDelegate && EffectiveGetHashCode(c) is { } gh)
            Reach(gh);
        if (!c.IsValueType && !c.IsDelegate && EffectiveEquals(c) is { } eq)
            Reach(eq);
        // A Finalize() override must be reachable so newobj
        // (MethodCompiler.Newobj.cs) can wire it into dn2cpp_register_finalizer /
        // the type-info finalize slot. Unlike
        // ToString/GetHashCode/Equals, nothing in an ordinary call graph ever
        // invokes Finalize — only the GC does — so without this reach it would
        // never surface as an edge and CppEmitter would silently wire a null slot.
        // Reference types only — C# disallows a finalizer on a struct.
        if (!c.IsValueType && EffectiveFinalize(c) is { } fin)
            Reach(fin);
    }

    /// <summary>Whether <paramref name="c"/>'s base chain reaches System.Exception
    /// (so it is an exception type). The corelib (via -r) gives
    /// NotSupportedException -> SystemException -> Exception -> Object as a BaseClass
    /// chain, so the walk terminates at a class named "System.Exception".
    /// <para>Without the corelib the chain is CUT at the first unloaded base: a
    /// corelib-less app exception's BaseClass is null and only
    /// <see cref="ClassInfo.ExternalBaseName"/> knows what the metadata named. So the
    /// walk also recognizes an External BCL exception ancestor by that name at each
    /// chain end — `MyEx : SystemException` and `MyEx : Exception` alike.
    /// The name test is <see cref="CoreIntrinsics.IsExternalBclExceptionName"/>;
    /// it cannot walk further (the External type's own base chain is absent), which
    /// is exactly why it must answer from the name alone.</para></summary>
    internal static bool InheritsFromException(ClassInfo c)
    {
        for (var b = c; b is not null; b = b.BaseClass)
        {
            if (b.FullName == "System.Exception")
                return true;
            if (b.BaseClass is null && b.ExternalBaseName is { } ext
                && CoreIntrinsics.IsExternalBclExceptionName(ext))
                return true;
        }
        return false;
    }

    /// <summary>Whether a <c>newobj</c> of this type is intercepted at emit time:
    /// every type whose base chain reaches System.Exception, for ALL ctor shapes.
    /// Intercepting all shapes keeps every exception object on the uniform
    /// message-carrying layout — get_Message / get_InnerException reinterpret_cast onto
    /// a Dn2CppExceptionObject prefix.
    /// <para>Two paths seed that prefix, and they differ in WHERE the message comes from.
    /// The <b>opaque intrinsics</b> — System.Exception itself and AggregateException — have
    /// no transpiled ctor body, so their newobj recovers message + inner positionally from
    /// the ctor args (<see cref="ExceptionMessageArgIndex"/> /
    /// <see cref="ExceptionInnerArgIndex"/>), the only place the values are available.
    /// <b>Every other</b> exception type — user-defined or a BCL one — is emitted as an
    /// inline alloc (zero-filled) + type stamp + DirectCall(ctor): its real ctor chain runs,
    /// so its own instance-field writes (ZlibException.Result, ArgumentException._paramName)
    /// land AND the base System.Exception::.ctor intrinsic stores the message/inner the BCL
    /// computed — real resource text (SR recovery) for the Argument*/FileNotFound family
    /// that positional recovery would misread. So the positional indices below govern only
    /// the two opaque paths.</para></summary>
    internal static bool IsInterceptedExceptionCtor(ClassInfo c, ImmutableArray<TypeDesc> ctorParams)
    {
        _ = ctorParams; // every shape is intercepted; kept for call-site symmetry
        return InheritsFromException(c);
    }

    /// <summary>The ctor-arg index of the Message string for an OPAQUE-intrinsic exception
    /// newobj (System.Exception / AggregateException, whose ctor body never runs), or -1
    /// when the shape carries no plain message. Recognized shapes:
    /// <c>(string message)</c>, <c>(string message, Exception inner)</c>, and the common
    /// user-defined <c>(TCode, string message)</c> / <c>(TCode, string message, Exception
    /// inner)</c> where <c>TCode</c> is a non-string, non-Exception result code (e.g.
    /// ZlibException's <c>(ZlibResult, string)</c>).
    /// <para>Positional recovery is exact for the real System.Exception ctor shapes it
    /// governs (<c>()</c> → -1, <c>(string)</c> → 0, <c>(string, Exception)</c> → 0, the
    /// serialization ctor → -1). It must never widen back to every exception newobj: the
    /// Argument*Exception family does not fit positionally
    /// (<c>ArgumentNullException(string paramName)</c> has the same shape as
    /// <c>ArgumentException(string message)</c>, so paramName would seed as the message,
    /// and <c>(string message, string paramName)</c> matches nothing) — which is why
    /// non-opaque exceptions run their real ctor chain instead: the base
    /// <c>System.Exception::.ctor</c> intrinsic stores the message the BCL computed —
    /// the real resource string (SR recovery) plus, through the reached get_Message
    /// override, the "(Parameter 'x')" suffix.</para></summary>
    internal static int ExceptionMessageArgIndex(ImmutableArray<TypeDesc> ctorParams) => ctorParams switch
    {
        [{ } p0] when p0.IsString => 0,
        [{ } q0, { } q1] when q0.IsString && IsExceptionParam(q1) => 0,
        [{ } r0, { } r1] when !r0.IsString && !IsExceptionParam(r0) && r1.IsString => 1,
        [{ } s0, { } s1, { } s2] when !s0.IsString && !IsExceptionParam(s0)
                                       && s1.IsString && IsExceptionParam(s2) => 1,
        _ => -1,
    };

    /// <summary>The ctor-arg index of the innerException for an intercepted exception
    /// newobj, or -1 when the shape carries no inner. Only the
    /// <c>(string message, Exception inner)</c> shape carries one; it is stored on the
    /// uniform exception object and returned by get_InnerException. Other shapes store
    /// null (read back as null inner), matching .NET's default.</summary>
    internal static int ExceptionInnerArgIndex(ImmutableArray<TypeDesc> ctorParams) => ctorParams switch
    {
        [{ } q0, { } q1] when q0.IsString && IsExceptionParam(q1) => 1,
        [{ } s0, { } s1, { } s2] when !s0.IsString && !IsExceptionParam(s0)
                                       && s1.IsString && IsExceptionParam(s2) => 2,
        _ => -1,
    };

    /// <summary>Whether a ctor parameter is an Exception (the innerException of the
    /// (string, Exception) shape) — a class named System.Exception, or the External
    /// stand-in the SigProvider yields for a cross-assembly Exception ref. The
    /// External arm answers by the same BCL-exception-name predicate the
    /// External-base recognition uses, so an inner param declared at a
    /// narrower unloaded BCL type (`(string, InvalidOperationException)`) is an
    /// Exception here too.</summary>
    private static bool IsExceptionParam(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } pc } && InheritsFromException(pc)
        || t is { Kind: TypeKind.External, ExternalName: { } xn }
           && CoreIntrinsics.IsExternalBclExceptionName(xn);

    private int? _exceptionGetMessageSlot;

    /// <summary>The vtable slot of <c>System.Exception::get_Message</c>, or -1 when the
    /// corelib is absent (a corelib-less runtime-TI build). The runtime's
    /// <c>dn2cpp_exception_message</c> dispatches through this slot so a derived exception's
    /// get_Message override (ArgumentException's "(Parameter 'x')" append,
    /// FileNotFoundException's lazy build) is honored even when the receiver is statically
    /// System.Exception (a <c>catch (Exception e) { e.Message }</c> that resolves the callee
    /// to the intrinsic-mapped base getter). Scans by NAME only — get_Message is unique on
    /// Exception, so no SigKey read (a decode) is needed. Exception's members are eagerly
    /// decoded in every build via BuildVtables, so the slot is assigned by the time anyone
    /// asks.</summary>
    internal int ExceptionGetMessageSlot()
    {
        if (_exceptionGetMessageSlot is { } cached)
            return cached;
        int slot = -1;
        if (FindClassByFullName("System.Exception") is { } ex)
        {
            EnsureCompleted(ex);
            foreach (var m in ex.Methods)
                if (m.Name == "get_Message")
                {
                    slot = m.VtableSlot;
                    break;
                }
        }
        _exceptionGetMessageSlot = slot;
        return slot;
    }

    /// <summary>Per-module cache of the embedded <c>.resources</c> string table (null once
    /// read for a module that carries none), so a blob is parsed at most once per assembly.</summary>
    private readonly Dictionary<Module, Dictionary<string, string>?> _srResourceCache = new();

    /// <summary>The English text of a BCL <c>SR.*</c> resource key, read from the calling
    /// assembly's own embedded <c>.resources</c> blob, or null when the module carries no
    /// such blob or the key is absent. The Module is the SR call site's own module: System.SR
    /// is <c>internal</c>, so every SR reference resolves intra-assembly, and a library pulled
    /// in with <c>-r</c> must resolve against its own resources. Reads no model members — it
    /// touches only <see cref="Module.PE"/> / <see cref="Module.Reader"/> — so it is
    /// strict-completion safe.</summary>
    internal string? SrResourceText(Module m, string key)
    {
        if (!_srResourceCache.TryGetValue(m, out var table))
        {
            var read = ResourceStrings.ReadStrings(m.PE, m.Reader);
            table = read.Count == 0 ? null : read;
            _srResourceCache[m] = table;
        }
        return table is not null && table.TryGetValue(key, out var text) ? text : null;
    }

    /// <summary>The same read against the CORELIB's own resources — the source of every
    /// message the C++ runtime raises (<see cref="BclMessages"/>), which has no call site
    /// whose module could name the blob. Identified by the type every corelib defines
    /// rather than by assembly name, so a BCL-as-IL corelib under another name still
    /// resolves; null when there is no corelib or no such key.</summary>
    internal string? CoreLibSrText(string key) =>
        FindClassByFullName("System.Object")?.Module is { } m ? SrResourceText(m, key) : null;

    /// <summary>Drops a base-chain resolution that landed on an intrinsic-mapped type
    /// (System.Object, System.ValueType, System.Attribute, …). Every method of such a
    /// type is emitted inline and never transpiled — <see cref="Reach"/> cuts it — so a
    /// call site that NAMES one emits a reference to a function that is never declared
    /// and never defined, and the generated C++ does not compile.
    ///
    /// The base chain reaches one asymmetrically: a struct declared in the real corelib
    /// has a TypeDefinition base handle, so pass 1 gives it <c>BaseClass =
    /// System.ValueType</c>, where an app-module struct (a TypeReference base) is left
    /// with a null BaseClass. So without this cut,
    /// <c>EffectiveEquals(KeyValuePair&lt;K,V&gt;)</c> walks into the untranspilable
    /// <c>ValueType::Equals</c> and every Dictionary keyed
    /// on a corelib struct emits an undefined symbol.
    ///
    /// Null means "nothing overrides it", which is what each caller's existing
    /// fall-through already handles: a nullptr type-info slot (the runtime helpers'
    /// identity/reference/type-name default — precisely the Object/ValueType semantics
    /// the cut body would have implemented), or a loud NotSupportedException.
    /// <see cref="EffectiveFinalize"/> stops at System.Object by hand for the same
    /// reason.</summary>
    private static MethodInfo? NonIntrinsic(MethodInfo? m) =>
        m is not null && CoreIntrinsics.IsIntrinsicType(m.DeclaringClass.FullName) ? null : m;

    /// <summary>The ToString() override (a 0-arg, string-returning instance method
    /// with a body) that an instance of <paramref name="c"/> dispatches — its own
    /// or the nearest base's — or null when none overrides Object.ToString (or the
    /// nearest override is an intrinsic type's, see <see cref="NonIntrinsic"/>).</summary>
    internal static MethodInfo? EffectiveToString(ClassInfo c)
    {
        for (var b = c; b is not null; b = b.BaseClass)
        {
            b.EnsureMembers();
            if (b.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "ToString"
                    && m.Rva != 0 && m.Signature.ParameterTypes.Length == 0
                    && m.Signature.ReturnType.IsString) is { } ts)
                return NonIntrinsic(ts);
        }
        return null;
    }

    /// <summary>The GetHashCode() override (a 0-arg, int-returning instance method
    /// with a body) an instance of <paramref name="c"/> dispatches — its own or the
    /// nearest base's — or null when none overrides Object.GetHashCode (or the
    /// nearest override is an intrinsic type's, see <see cref="NonIntrinsic"/>).</summary>
    internal static MethodInfo? EffectiveGetHashCode(ClassInfo c)
    {
        for (var b = c; b is not null; b = b.BaseClass)
        {
            b.EnsureMembers();
            if (b.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "GetHashCode"
                    && m.Rva != 0 && m.Signature.ParameterTypes.Length == 0
                    && m.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }) is { } gh)
                return NonIntrinsic(gh);
        }
        return null;
    }

    /// <summary>The Equals(object) override (a 1-arg method taking System.Object and
    /// returning bool, with a body) an instance of <paramref name="c"/> dispatches —
    /// its own or the nearest base's — or null when none overrides Object.Equals (or
    /// the nearest override is an intrinsic type's, see <see cref="NonIntrinsic"/>).
    /// The typed <c>Equals(Point)</c> a record also synthesizes is a different
    /// method; the Object-virtual is the one collections dispatch.</summary>
    internal static MethodInfo? EffectiveEquals(ClassInfo c) => NonIntrinsic(DeclaredEquals(c));

    /// <summary>The nearest Equals(object) override in <paramref name="c"/>'s base chain,
    /// INCLUDING one an intrinsic-mapped base declares — the raw walk, before
    /// <see cref="EffectiveEquals"/> applies the <see cref="NonIntrinsic"/> cut. The
    /// two answers differ for exactly one shape, a type whose only override lives on an
    /// intrinsic base (System.Attribute field-walks its values): the raw walk names that
    /// body, the cut says "nothing overrides it". Callers want the cut answer — an
    /// uncallable body is not an override you can dispatch — which is why this is the
    /// private half and EffectiveEquals the public one.</summary>
    internal static MethodInfo? DeclaredEquals(ClassInfo c)
    {
        for (var b = c; b is not null; b = b.BaseClass)
        {
            b.EnsureMembers();
            if (b.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "Equals"
                    && m.Rva != 0 && m.Signature.ParameterTypes is [{ IsObject: true }]
                    && m.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }) is { } eq)
                return eq;
        }
        return null;
    }

    /// <summary>The Finalize() override (a 0-arg, void-returning instance method
    /// with a body — what a C# <c>~T()</c> destructor compiles down to) that an
    /// instance of <paramref name="c"/> dispatches — its own or the nearest
    /// base's — or null when none overrides Object.Finalize. Same shape as
    /// <see cref="EffectiveToString"/>, with one difference: the walk stops
    /// before reaching System.Object itself. Unlike ToString/GetHashCode/Equals
    /// (whose Object-level bodies are meaningful defaults worth dispatching to),
    /// real CoreLib's <c>Object</c> declares its own empty <c>~Object() {}</c>
    /// (Rva != 0, just an empty body) — walking into it would make EVERY
    /// reference type register a finalizer that does nothing, defeating the
    /// whole point of gating dn2cpp_register_finalizer on "has one". Used to
    /// wire Dn2CppTypeInfo.finalize and to decide whether newobj must call
    /// dn2cpp_register_finalizer.</summary>
    internal static MethodInfo? EffectiveFinalize(ClassInfo c)
    {
        for (var b = c; b is not null && b.FullName != "System.Object"; b = b.BaseClass)
        {
            b.EnsureMembers();
            if (b.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "Finalize"
                    && m.Rva != 0 && m.Signature.ParameterTypes.Length == 0
                    && m.Signature.ReturnType.IsVoid) is { } fin)
                return fin;
        }
        return null;
    }

    /// <summary>The <c>ToString(string format)</c> overload of a value type (a
    /// 1-arg instance method taking string, returning string, with a body). The
    /// interpolated-string hole emit uses it to honor <c>$"{struct:fmt}"</c> the
    /// way the real DefaultInterpolatedStringHandler does for an IFormattable
    /// value (Guid, for one). Null when the struct has no such overload — then
    /// the specifier is ignored and <see cref="EffectiveToString"/> applies.</summary>
    internal static MethodInfo? EffectiveToStringFormat(ClassInfo c) =>
        c.EnsureMembers().Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "ToString" && m.Rva != 0
            && m.Signature.ReturnType.IsString
            && m.Signature.ParameterTypes is [{ IsString: true }]);

    /// <summary><c>System.IO.Stream</c>'s abstract <c>Read(byte[], int, int)</c> /
    /// <c>Write(byte[], int, int)</c> — the SYNCHRONOUS slot, the one member every concrete
    /// Stream must override because it is abstract, and the one the base async funnels are
    /// rewritten to dispatch through (see
    /// <see cref="CoreIntrinsics.IsStreamSyncOverAsyncFunnel"/>).
    ///
    /// <para>Told apart from the span overloads (<c>Read(Span&lt;byte&gt;)</c>,
    /// <c>Write(ReadOnlySpan&lt;byte&gt;)</c>) by the parameter COUNT, which is unambiguous
    /// on this one class: those take one. The name test comes first, so the signature —
    /// whose read is a decode — is only pulled for the two candidates that could match.</para>
    /// </summary>
    internal static MethodInfo? StreamSyncSlot(ClassInfo stream, bool read) =>
        stream.EnsureMembers().Methods.FirstOrDefault(
            m => m.Name == (read ? "Read" : "Write") && !m.IsStatic && m.IsVirtual
                && m.Signature.ParameterTypes.Length == 3);

    /// <summary>The typed <c>Equals(T)</c> override of a value type — the
    /// <c>IEquatable&lt;T&gt;.Equals</c> a struct key's <c>EqualityComparer&lt;T&gt;.
    /// Default</c> dispatches (a 1-arg method taking the struct's own type and
    /// returning bool, with a body). Null when the struct has no such method (then
    /// the Object-virtual <see cref="EffectiveEquals"/> applies).</summary>
    internal static MethodInfo? EffectiveTypedEquals(ClassInfo c) =>
        c.EnsureMembers().Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "Equals" && m.Rva != 0
            && m.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }
            && m.Signature.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } pc }] && pc == c);

    /// <summary>The typed <c>CompareTo(T)</c> overload of a value type — the
    /// <c>IComparable&lt;T&gt;.CompareTo</c> a constrained callvirt on a struct
    /// devirtualizes to (a 1-arg method taking the struct's own type and returning
    /// int, with a body). Null when the struct only has the non-generic
    /// object-taking <c>CompareTo</c> (or none).</summary>
    internal static MethodInfo? EffectiveTypedCompareTo(ClassInfo c) =>
        c.EnsureMembers().Methods.FirstOrDefault(m => !m.IsStatic && m.Name == "CompareTo" && m.Rva != 0
            && m.Signature.ReturnType is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Int32 }
            && m.Signature.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } pc }] && pc == c);

    /// <summary>Reaches a type's static constructor. A reference assembly's
    /// .cctor is not a root (only the app module's are), so allocation, static-field
    /// access, and a static call on a non-beforefieldinit type must pull it in. Static
    /// calls on beforefieldinit types do not: their initializer need only precede a
    /// static-field access, and reaching it here pulls unwanted BCL initializers. Reached
    /// non-generic .cctors run at startup; closed generic ones run through first-use
    /// guards.</summary>
    private void ReachCctor(ClassInfo c)
    {
        EnsureCompleted(c);   // its .cctor is a member
        // A framework EventSource-derived tracing provider (ArrayPoolEventSource,
        // CDSCollectionETWBCLProvider, ...): its .cctor does `Log = new TheProvider()`,
        // allocating an EventSource whose finalizer -> Dispose -> SendManifest ->
        // CreateManifestAndDescriptors pulls the entire manifest-generation subtree
        // (Guid/HexConverter formatting, Type reflection, ResourceManager ->
        // GlobalizationMode -> the ICU InitICUFunctions InternalCall). EventSource is
        // never functional in a native dn2cpp build (the IL2CPP/NativeAOT-style no-op
        // diagnostics posture), so skip the cctor entirely — the singleton stays null
        // (folded inline in the Ldsfld handler) and the only reads are the
        // `if (Log.IsEnabled()) Log.Event(...)` tracing guards, lowered in
        // TryEmitEventSourceNoOp.
        if (IsFrameworkEventSourceProvider(c))
            return;
        var cctor = c.StaticCctor;
        if (cctor is not null)
            Reach(cctor);
    }

    /// <summary>Whether a class is a framework diagnostics provider: defined in a
    /// framework assembly (CoreLib / System.*) and derived from
    /// <c>System.Diagnostics.Tracing.EventSource</c>. dn2cpp models EventSource
    /// diagnostics as a bounded no-op surface (the IL2CPP/NativeAOT posture — no
    /// EventPipe/ETW/EventListener delivery, no manifest generation): a provider's
    /// singleton is folded to null (its .cctor is skipped in ReachCctor), its
    /// <c>IsEnabled()</c> guards fold to false and its event methods to no-ops
    /// (TryEmitEventSourceNoOp), and none of its members is a reachability edge
    /// (ResolveCallTarget). Scoped to framework assemblies on purpose: a user-defined
    /// or <c>Microsoft.*</c> provider is NOT folded here — it transpiles as ordinary C#
    /// over the opaque intrinsic EventSource base (s_intrinsicTypes; its construct/write
    /// surface no-op'd in TryEmitEventSourceIntrinsic, its unmodeled OBSERVATION side loud),
    /// so its singleton and .cctor stay live and its own [Event] bodies run.
    /// <para>The assembly test is the whole of the narrowing: the base-chain half is
    /// <see cref="InheritsFromEventSource"/>, shared with the emitter's identity stamp so
    /// the two cannot answer differently about what a provider IS.</para></summary>
    internal static bool IsFrameworkEventSourceProvider(ClassInfo c)
    {
        if (c.Module.AssemblyName != "System.Private.CoreLib"
            && !c.Module.AssemblyName.StartsWith("System.", StringComparison.Ordinal))
            return false;
        return InheritsFromEventSource(c);
    }

    /// <summary>Whether <paramref name="c"/> is an EventSource PROVIDER — its base chain
    /// reaches <c>System.Diagnostics.Tracing.EventSource</c>, whoever declared it.
    /// The framework predicate above narrows this by assembly; the emitter's provider
    /// identity stamp (<c>CppEmitter.EventSourceIdentity</c>) does not, because a user or
    /// <c>Microsoft.*</c> provider is exactly the kind whose Name and Guid a program reads.
    /// <para>Like <see cref="InheritsFromException"/>, the walk also recognizes the base by
    /// NAME at a cut chain end: without the corelib on the reference list a provider's
    /// BaseClass is null and only <see cref="ClassInfo.ExternalBaseName"/> knows what the
    /// metadata named.</para></summary>
    internal static bool InheritsFromEventSource(ClassInfo c)
    {
        for (var b = c.BaseClass; b is not null; b = b.BaseClass)
        {
            if (b.FullName == "System.Diagnostics.Tracing.EventSource")
                return true;
            if (b.BaseClass is null && b.ExternalBaseName == "System.Diagnostics.Tracing.EventSource")
                return true;
        }
        return c.BaseClass is null && c.ExternalBaseName == "System.Diagnostics.Tracing.EventSource";
    }

    /// <summary>Resolves the declaring type of a static field access seen during
    /// the reachability scan to its loaded ClassInfo, or null when it does not
    /// resolve to a loaded class (e.g. a primitive/external parent). Used to reach
    /// the type's static initializer so a never-allocated, only-static-read type
    /// (SZGenericArrayEnumerator&lt;T&gt;.Empty) is still emitted/initialized.</summary>
    private ClassInfo? ResolveStaticFieldClass(Module module, EntityHandle handle, GenericContext ctx)
    {
        var reader = module.Reader;
        switch (handle.Kind)
        {
            case HandleKind.FieldDefinition:
                return GetClass(module, reader.GetFieldDefinition((FieldDefinitionHandle)handle).GetDeclaringType());
            case HandleKind.MemberReference:
            {
                var mr = reader.GetMemberReference((MemberReferenceHandle)handle);
                return mr.Parent.Kind switch
                {
                    HandleKind.TypeSpecification => ResolveMemberRefField(module, (MemberReferenceHandle)handle, ctx).Item1,
                    HandleKind.TypeDefinition => GetClass(module, (TypeDefinitionHandle)mr.Parent),
                    HandleKind.TypeReference => ResolveTypeRef(module, (TypeReferenceHandle)mr.Parent) is { Kind: TypeKind.Class } t ? t.Class : null,
                    _ => null,
                };
            }
            default:
                return null;
        }
    }

    /// <summary>The loaded declaring class of a static call token, before any intrinsic
    /// or bounded route cuts its MethodInfo edge. This is the type-initialization asker:
    /// a non-beforefieldinit cctor is a first-use edge even when emission replaces the
    /// method body and returns before the ordinary managed-call path.</summary>
    private ClassInfo? ResolveStaticCallClass(Module module, EntityHandle handle, GenericContext ctx)
    {
        var reader = module.Reader;
        if (handle.Kind == HandleKind.MethodSpecification)
            return ResolveStaticCallClass(module,
                reader.GetMethodSpecification((MethodSpecificationHandle)handle).Method, ctx);
        if (handle.Kind == HandleKind.MethodDefinition)
        {
            var md = reader.GetMethodDefinition((MethodDefinitionHandle)handle);
            if ((md.Attributes & MethodAttributes.Static) == 0)
                return null;
            var decl = md.GetDeclaringType();
            return module.ClassMap.TryGetValue(decl, out var mapped) ? mapped
                : _currentScan?.DeclaringClass is { } current
                    && current.Module == module && current.Handle == decl ? current
                : null;
        }
        if (handle.Kind != HandleKind.MemberReference)
            return null;
        var mr = reader.GetMemberReference((MemberReferenceHandle)handle);
        if (reader.GetBlobReader(mr.Signature).ReadSignatureHeader().IsInstance)
            return null;
        return mr.Parent.Kind switch
        {
            HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent)
                .DecodeSignature(SigProvider, ctx).Class,
            HandleKind.TypeDefinition => module.ClassMap.TryGetValue(
                (TypeDefinitionHandle)mr.Parent, out var mapped) ? mapped
                : _currentScan?.DeclaringClass is { } current
                    && current.Module == module && current.Handle == (TypeDefinitionHandle)mr.Parent
                        ? current : null,
            HandleKind.TypeReference => ResolveTypeRef(module, (TypeReferenceHandle)mr.Parent)?.Class,
            _ => null,
        };
    }

    /// <summary>Resolves a type token seen during the reachability scan (the
    /// <c>constrained.</c> prefix operand): a TypeDef/TypeSpec/TypeRef to a
    /// TypeDesc, or null when it doesn't resolve to a loaded type.</summary>
    private TypeDesc? ResolveTypeTokenForScan(Module module, EntityHandle handle, GenericContext ctx) => handle.Kind switch
    {
        HandleKind.TypeDefinition => GetTypeDescForDefinition(module, (TypeDefinitionHandle)handle),
        HandleKind.TypeSpecification =>
            module.Reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(SigProvider, ctx),
        HandleKind.TypeReference => ResolveTypeRef(module, (TypeReferenceHandle)handle),
        _ => null,
    };

    /// <summary>Reaches a value-type (struct) key's equality overrides — its
    /// GetHashCode and its typed <c>IEquatable&lt;T&gt;.Equals(T)</c> (or, failing
    /// that, the Object-virtual <c>Equals(object)</c>) — so they are transpiled and
    /// available to the EqualityHashExpr/EqualityEqualsExpr emit for a struct used as
    /// a HashSet/Dictionary key. A no-op for non-struct, enum, or override-less key
    /// types (those use the inline primitive/reference paths).
    ///
    /// <paramref name="includeHash"/> is false for a use site that only ever compares —
    /// an array/span element scan — where reaching GetHashCode would pull a body the
    /// program never calls.</summary>
    private void ReachValueKeyEquality(TypeDesc keyType, bool includeHash = true)
    {
        // An object/reference/array key compares through dn2cpp_object_equals (the emit's
        // IsReferenceKeyType arm) — a Dictionary<object,V>, a List<object>.Contains. That is
        // an object-equality dispatch, and a boxed struct can be the very thing flowing into
        // it, so the boxed structural wiring is due.
        if (MethodCompiler.IsReferenceKeyType(keyType))
            NoteObjectEqualityDispatch();
        if (keyType is not { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } sc })
            return;
        // An intrinsic value type has no transpilable body to reach: the emit side
        // compares it inline (IntrinsicValueTypeFn / IntrinsicPointerValueType), and
        // reaching its Equals would name a body the emitter never defines (both
        // intrinsic tests, per AGENTS.md).
        if (CoreIntrinsics.IsIntrinsicType(sc.FullName) || sc.IntrinsicCppName is not null)
            return;
        if (includeHash && EffectiveGetHashCode(sc) is { } gh)
            Reach(gh);
        if (EffectiveTypedEquals(sc) is { } teq)
            Reach(teq);
        else if (EffectiveEquals(sc) is { } oeq)
            Reach(oeq);
        // A struct that overrides NEITHER — KeyValuePair<K,V>, a plain user struct, the
        // Asn1 family — is what ValueType.Equals/GetHashCode answer for, and those are
        // QCall externs (docs: AGENTS.md "BCL coverage"). Mint the field walk instead.
        // Hash and equality are asked for independently: a struct may override one and
        // not the other, and an element scan asks for equality alone.
        ReachSynthesizedValueEquality(sc,
            includeHash: includeHash && EffectiveGetHashCode(sc) is null,
            includeEquals: EffectiveTypedEquals(sc) is null && EffectiveEquals(sc) is null);
    }

    // ---- synthesized structural equality/hash for a value type that overrides neither ----

    /// <summary>Whether the program ever hands a boxed value to
    /// <c>dn2cpp_object_equals</c>/<c>_gethashcode</c> — an <c>object</c>-typed
    /// comparison, an <c>object</c>/reference key, or an <c>Object::Equals</c>/
    /// <c>GetHashCode</c> call site. A boxed struct's structural equality reaches the
    /// runtime helpers through the type-info slots and nowhere else, so a program that
    /// never calls them cannot observe those slots and must not pay for them: hello world
    /// grows by zero bytes. The used-slot model of <see cref="_usedVirtualDecls"/>, for the
    /// one virtual whose declaring type is intrinsic and therefore has no slot to mark.
    /// </summary>
    private bool _objectEqualityDispatched;

    /// <summary>Records an emit site that will lower to <c>dn2cpp_object_equals</c> /
    /// <c>dn2cpp_object_gethashcode</c>. Called from the reachability scan (never from
    /// emission — the wiring it drives is decided before a single body compiles).</summary>
    private void NoteObjectEqualityDispatch() => _objectEqualityDispatched = true;

    /// <summary>Minted structural bodies in mint order — the emitter's synthesis round
    /// reads this (a synthetic is not in its class's <c>Methods</c>, so the body walk
    /// cannot see it). Mint order is reach order, which is deterministic; the emitter
    /// sorts anyway.</summary>
    private readonly List<MethodInfo> _synthesizedValueBodies = new();
    internal IReadOnlyList<MethodInfo> SynthesizedValueBodies => _synthesizedValueBodies;

    private readonly Dictionary<ClassInfo, MethodInfo?> _synthValueEquals = new();
    private readonly Dictionary<ClassInfo, MethodInfo?> _synthValueHash = new();

    /// <summary>The instance fields a structural walk compares/hashes: the declared,
    /// non-static, non-literal ones (a value type's base is System.ValueType, which has
    /// none).</summary>
    internal static IEnumerable<FieldInfo> StructuralFields(ClassInfo c) =>
        c.Fields.Where(f => !f.IsStatic && !f.IsLiteral);

    /// <summary>Whether a value type is shaped so that a field walk is even meaningful.
    /// The carve-outs, and why each one is one:
    /// <list type="bullet">
    /// <item>explicit layout — overlapping fields are a union; walking them double-counts
    /// the bytes, and .NET's own memcmp fast path declines such a type too;</item>
    /// <item><c>[InlineArray(N)]</c> — the one declared field stands for N elements;</item>
    /// <item>ref struct — never boxed, and never a key;</item>
    /// <item>intrinsic-mapped (BOTH tests: the name table misses a closed generic
    /// intrinsic like Vector128&lt;T&gt;, which only <see cref="ClassInfo.IntrinsicCppName"/>
    /// catches) — its members are emitted inline and never transpiled;</item>
    /// <item>canonical-world — a placeholder's fields are the placeholder's, so a verdict
    /// taken here would be baked for every real T that shares the owner. The taint at the
    /// emit site (MethodCompiler) is what routes those to per-instantiation bodies.</item>
    /// </list></summary>
    private static bool SynthesizableShape(ClassInfo c) =>
        c.IsValueType && !c.IsEnum && !c.IsByRefLike && !c.IsExplicitLayout
        && c.InlineArrayLength == 0
        && !CoreIntrinsics.IsIntrinsicType(c.FullName) && c.IntrinsicCppName is null
        && !ContainsCanonPlaceholder(c);

    // The two abstract roots System.Enum / System.ValueType need no carve-out here, and
    // the invariant that keeps it that way lives one level up: BOTH are modeled as
    // reference types (ClassInfo.IsValueType false — see the System.Enum arm of the
    // base-type scan in Compilation.cs, and ValueType's own base, Object), so every
    // `IsValueType: true` pattern in this file already excludes them. Undo that model
    // and a field walk over either becomes silently meaningless: a slot declared Enum
    // holds a reference to some concrete enum, and the emitted t_System_Enum is an
    // empty shell whose walk would call every value equal.

    /// <summary>The U of a closed <c>Nullable&lt;U&gt;</c>, else null. The one type whose
    /// BOX is not a box of itself: the CLR turns a <c>Nullable&lt;U&gt;</c> into null or a
    /// box of U, so every path that reasons about "what does boxing this field produce"
    /// has to unwrap it first (the <c>box</c>/<c>unbox.any</c> opcodes already do — see
    /// MethodCompiler.NullableLayout).</summary>
    internal TypeDesc? NullableUnderlying(TypeDesc t) =>
        t is { Kind: TypeKind.Class, Class: { } cls }
        && GenericDefFullName(cls) == "System.Nullable"
        && cls.Context.TypeArgs.Length == 1
            ? cls.Context.TypeArgs[0]
            : null;

    /// <summary>Whether every instance field of <paramref name="c"/> has a comparison
    /// (<paramref name="hash"/> false) or a hash (true) the emit can build. Recurses into
    /// struct fields; <paramref name="visiting"/> only guards the walk (the CLR forbids a
    /// cyclic struct graph outright).</summary>
    private bool CanSynthesizeValueEquality(ClassInfo c, bool hash, HashSet<ClassInfo> visiting)
    {
        if (!SynthesizableShape(c))
            return false;
        if (!visiting.Add(c))
            return true;
        try
        {
            foreach (var f in StructuralFields(c))
            {
                // [MarshalAs(ByValArray, SizeConst = N)]: the field IS N elements laid out
                // inline, not one value — comparing it as a single field would read the first.
                if (f.ByValArraySize >= 0)
                    return false;
                if (!CanCompareStructuralField(f.Type, hash, visiting))
                    return false;
            }
            return true;
        }
        finally
        {
            visiting.Remove(c);
        }
    }

    /// <summary>Whether one field's type has a structural comparison/hash.
    ///
    /// <para>A struct field is the subtle arm, and it is the whole reason this is a
    /// separate function from the key-path builder (MethodCompiler.TryEqualityEqualsLValue):
    /// <c>ValueType.Equals</c> boxes each field and calls the <b>Object-virtual</b>
    /// <c>Equals(object)</c>, which for a struct that only implements
    /// <c>IEquatable&lt;T&gt;</c> lands back in <c>ValueType.Equals</c> — the typed
    /// <c>Equals(T)</c> is never called. So the test here is the Object-virtual override or
    /// the field's own synthesized walk, and never the typed one.</para></summary>
    private bool CanCompareStructuralField(TypeDesc t, bool hash, HashSet<ClassInfo> visiting)
    {
        // A Nullable<U> field is boxed by the CLR as null-or-a-box-of-U — never as a box of
        // Nullable<U> — so the walk compares/hashes U, and it is U that has to be walkable.
        if (NullableUnderlying(t) is { } nu)
            return CanCompareStructuralField(nu, hash, visiting);
        if (t.IsString)
            return true;
        if (t.Kind == TypeKind.Primitive && !t.IsObject)
            return t.Primitive is not (PrimitiveTypeCode.Void or PrimitiveTypeCode.TypedReference);
        if (t is { Kind: TypeKind.Class, Class.IsEnum: true })
            return true;
        // object / a class / an external reference / an array: the runtime object helpers,
        // which dispatch a wired override and otherwise answer reference equality — exactly
        // what a boxed field reaches in .NET.
        if (MethodCompiler.IsReferenceKeyType(t))
            return true;
        // Decimal/TimeSpan/DateTime/…: intrinsic-mapped, no emitted Equals, runtime payload
        // helpers instead.
        if (MethodCompiler.IntrinsicValueTypeFn(t) is not null)
            return true;
        // Opaque pointer-backed handles compare and hash their entire representation;
        // their loaded managed fields do not describe the emitted runtime payload.
        if (MethodCompiler.IntrinsicPointerValueType(t) is not null)
            return true;
        // A real struct. System.Enum/System.ValueType cannot arrive here — both are
        // modeled as reference types and were answered by the IsReferenceKeyType arm
        // above; see the note at SynthesizableShape.
        if (t is { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } fc })
            return (hash ? EffectiveGetHashCode(fc) : EffectiveEquals(fc)) is not null
                || CanSynthesizeValueEquality(fc, hash, visiting);
        // A pointer, a function pointer, a byref, a TypedReference, an unresolved
        // placeholder: no value semantics to build one from.
        return false;
    }

    /// <summary>The minted <c>Equals(T)</c> of a value type with no <c>Equals(object)</c>
    /// override — the typed shape, because every consumer already holds two typed operands
    /// (the key path, the field walk, the boxed thunk after it unboxes), and boxing them to
    /// re-enter an object-taking signature would allocate on every probe. Null when the
    /// shape or a field carves it out. Reach-phase only: minting reads field types, and a
    /// field-type read is a decode that appends to <see cref="Classes"/> (AGENTS.md).
    ///
    /// <para><b>Not</b> gated on the typed <c>Equals(T)</c> existing: a struct that
    /// implements only <c>IEquatable&lt;T&gt;</c> still runs the field walk when something
    /// boxes it, so the walk must exist for it too. The key path is what prefers the typed
    /// override, and it asks for it separately.</para></summary>
    internal MethodInfo? SynthesizedValueEquals(ClassInfo c)
    {
        if (_synthValueEquals.TryGetValue(c, out var cached))
            return cached;
        MethodInfo? m = EffectiveEquals(c) is null
            && CanSynthesizeValueEquality(c, hash: false, new HashSet<ClassInfo>())
                ? MintValueBody(c, "Equals", "__vteq",
                    TypeDesc.MakePrimitive(PrimitiveTypeCode.Boolean),
                    ImmutableArray.Create(TypeDesc.MakeClass(c)))
                : null;
        _synthValueEquals[c] = m;
        return m;
    }

    /// <summary>The minted <c>GetHashCode()</c> of a value type with no override — the
    /// twin of <see cref="SynthesizedValueEquals"/>, and independent of it (a struct may
    /// override one and not the other).</summary>
    internal MethodInfo? SynthesizedValueHash(ClassInfo c)
    {
        if (_synthValueHash.TryGetValue(c, out var cached))
            return cached;
        MethodInfo? m = EffectiveGetHashCode(c) is null
            && CanSynthesizeValueEquality(c, hash: true, new HashSet<ClassInfo>())
                ? MintValueBody(c, "GetHashCode", "__vthash",
                    TypeDesc.MakePrimitive(PrimitiveTypeCode.Int32), ImmutableArray<TypeDesc>.Empty)
                : null;
        _synthValueHash[c] = m;
        return m;
    }

    /// <summary>The reached synthesized equality of a value type, as a pure lookup — no
    /// mint, no field-type decode. What emission asks: by then reachability has decided
    /// which structs get a walk, and a struct it did not mint one for has no symbol to
    /// call. (The emit and reach sites are paired — every builder arm has a reach hook —
    /// so a null here means the pair drifted, and the caller's loud NotSupported is the
    /// right answer.)</summary>
    internal MethodInfo? ReachedSynthesizedValueEquals(ClassInfo c) =>
        _synthValueEquals.TryGetValue(c, out var m) && m is not null && Reachable.Contains(m) ? m : null;

    /// <summary>The reached synthesized hash of a value type — see
    /// <see cref="ReachedSynthesizedValueEquals"/>.</summary>
    internal MethodInfo? ReachedSynthesizedValueHash(ClassInfo c) =>
        _synthValueHash.TryGetValue(c, out var m) && m is not null && Reachable.Contains(m) ? m : null;

    private MethodInfo MintValueBody(ClassInfo c, string name, string suffix,
        TypeDesc returnType, ImmutableArray<TypeDesc> parameterTypes)
    {
        var m = new MethodInfo
        {
            DeclaringClass = c,
            Name = name,
            Handle = default,     // no metadata row: nothing reads a MethodBody for it
            Module = c.Module,
            Attributes = MethodAttributes.Public,
            IsSynthetic = true,
            // The suffix is what tells the two apart in CppName and in the total order
            // (MethodInfo.CompareByOrder tie-breaks on it, and both share row 0).
            NameSuffix = suffix,
            Signature = new MethodSignature<TypeDesc>(default, returnType,
                requiredParameterCount: parameterTypes.Length, genericParameterCount: 0,
                parameterTypes),
        };
        _synthesizedValueBodies.Add(m);
        return m;
    }

    /// <summary>Reaches a value type's synthesized structural bodies and, recursively,
    /// everything the field walk will call. A synthesized body has no IL for
    /// <see cref="ScanBodyForGenerics"/> to read, so this IS its edge set — miss an edge
    /// and the generated C++ names a function nothing defines.</summary>
    private void ReachSynthesizedValueEquality(ClassInfo c, bool includeHash, bool includeEquals = true)
    {
        if (includeEquals && SynthesizedValueEquals(c) is { } eq && Reachable.Add(eq))
            foreach (var f in StructuralFields(c))
                ReachStructuralField(f.Type, hash: false);
        if (includeHash && SynthesizedValueHash(c) is { } gh && Reachable.Add(gh))
            foreach (var f in StructuralFields(c))
                ReachStructuralField(f.Type, hash: true);
    }

    /// <summary>Reaches what one field of a structural walk calls. Only a struct field is
    /// an edge — a primitive/enum compares inline, a string and a reference go through the
    /// runtime helpers, an intrinsic value type through its payload helper. The recursion
    /// terminates on the <c>Reachable.Add</c> above (and the CLR forbids a cyclic struct
    /// graph anyway).</summary>
    private void ReachStructuralField(TypeDesc t, bool hash)
    {
        // Spelled against the same tests CanCompareStructuralField uses, and it has to stay
        // that way: this reaches what that one promised the emit could build.
        if (NullableUnderlying(t) is { } nu)
        {
            ReachStructuralField(nu, hash);   // the box a Nullable<U> makes is a box of U
            return;
        }
        // (System.Enum/System.ValueType are reference-typed in the model, so the
        // IsValueType test excludes them here as it does in the twin above.)
        if (t is not { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } fc }
            || MethodCompiler.IntrinsicValueTypeFn(t) is not null
            || MethodCompiler.IntrinsicPointerValueType(t) is not null)
            return;
        // The Object-virtual override ValueType.Equals/GetHashCode would dispatch through
        // the box — never the typed IEquatable<T>.Equals (see CanCompareStructuralField).
        if ((hash ? EffectiveGetHashCode(fc) : EffectiveEquals(fc)) is { } impl)
            Reach(impl);
        else
            ReachSynthesizedValueEquality(fc, includeHash: hash, includeEquals: !hash);
    }

    /// <summary>Wires the structural equality of every BOXED value type that overrides
    /// neither <c>Equals(object)</c> nor <c>GetHashCode</c> — the used×allocated cross
    /// product for the one virtual pair whose declaring type (System.ValueType) is
    /// intrinsic and therefore owns no slot to mark used.
    ///
    /// <para>Without it, <c>object o = kvp; o.Equals(other)</c> is not a gap but a silent
    /// wrong answer: <c>dn2cpp_object_equals</c> answers a null <c>equals</c> slot with
    /// reference equality, and two boxes of the same value are two allocations, so it
    /// returns false — and <c>dn2cpp_object_gethashcode</c> returns an address-derived
    /// hash.</para>
    ///
    /// <para>Both halves of the gate are load-bearing: boxed, so the helpers can reach it
    /// at all (<see cref="IsAllocated"/> of a value type IS "was boxed" — only the
    /// <c>box</c> opcode puts one in the set); and object-equality dispatched, so a program
    /// that never compares two objects pays nothing. Returns whether anything new was
    /// reached — a fresh box can appear in the drain it triggers.</para></summary>
    private bool ReachBoxedValueEquality()
    {
        if (!_objectEqualityDispatched)
            return false;
        int before = Reachable.Count;
        foreach (var c in _allocatedRefTypes.Where(c => c.IsValueType).ToList())
            ReachSynthesizedValueEquality(c, includeHash: true);
        return Reachable.Count != before;
    }

    /// <summary>The bare method name a constrained callee token names — for the cheap
    /// pre-checks that must decide before decoding a full signature. Null for a token
    /// that is neither a MemberReference nor a MethodDefinition.</summary>
    private string? ConstrainedCalleeName(Module module, EntityHandle calleeHandle) =>
        calleeHandle.Kind switch
        {
            HandleKind.MemberReference =>
                module.Reader.GetString(module.Reader.GetMemberReference((MemberReferenceHandle)calleeHandle).Name),
            HandleKind.MethodDefinition =>
                module.Reader.GetString(module.Reader.GetMethodDefinition((MethodDefinitionHandle)calleeHandle).Name),
            _ => null,
        };

    /// <summary>Reaches a value type's implementation of a <c>constrained.</c> callvirt
    /// target — an interface slot, or an Object/ValueType-rooted virtual whose declaring
    /// type is intrinsic so the normal call edge is cut. Either way the emit side lowers
    /// the call to a DIRECT call on the struct's body, which must therefore be transpiled.
    ///
    /// <para>The order below mirrors the emit side's decision order: the typed
    /// IEquatable/IComparable devirtualization, then the cuts and boxed-dispatch wiring
    /// <c>TryEmitValueConstrained</c> claims, then the resolution
    /// <c>EmitConstrainedCall</c> makes. A step that ran early here and returned would
    /// reach a body emit never calls while leaving the one it does call
    /// untranspiled.</para></summary>
    private void ReachConstrainedImpl(Module module, EntityHandle calleeHandle, TypeDesc constrained, GenericContext ctx)
    {
        // A `constrained. System.Object callvirt IComparable<object>::CompareTo` — the
        // reference-type default order inside GenericComparer<object>.Compare — is
        // emitted as dn2cpp_object_compare through the non-generic System.IComparable
        // (MethodCompiler.TryCompareLValue's Object arm), so lay that interface's
        // type-info down here. The canon placeholder is declined by that arm (kept a
        // per-instantiation body), so only genuine System.Object reaches this.
        if (constrained.IsObject && !constrained.IsCanonPlaceholder
            && ConstrainedCalleeName(module, calleeHandle) == "CompareTo")
            NoteNonGenericIComparableUsed();
        if (constrained is not { Kind: TypeKind.Class, Class: { IsValueType: true } c })
            return;
        string name;
        MethodSignature<TypeDesc> sig;
        // The resolved callee, when the token yields one: what the shared resolution below
        // (ConstrainedImplOf) needs, and the only thing that can see an .override row or a
        // variance-compatible interface slot. A token that resolves to nothing keeps the
        // name+shape walk at the bottom.
        MethodInfo? resolvedCallee = null;
        switch (calleeHandle.Kind)
        {
            case HandleKind.MemberReference:
                var mr = module.Reader.GetMemberReference((MemberReferenceHandle)calleeHandle);
                name = module.Reader.GetString(mr.Name);
                sig = mr.DecodeMethodSignature(SigProvider, ctx);
                // An unresolvable member ref is not an error here — the name+shape walk at
                // the bottom still answers. The filter is what keeps InstantiationBound
                // (a NotSupportedException subclass) escaping.
                try { resolvedCallee = ResolveMemberRefMethod(module, (MemberReferenceHandle)calleeHandle, ctx); }
                catch (NotSupportedException e) when (!IsMustEscape(e)) { }
                // The typed IEquatable<T>::Equals(!0) / IComparable<T>::CompareTo(!0)
                // devirtualizes to the struct's typed overload; under an erased caller
                // context the SigKey walk below would decode !0 to object and reach the
                // Equals(object) override instead, so reach the typed body explicitly. Kept
                // here rather than deferred to ConstrainedImplOf: that asks the RESOLVED
                // callee, and this token may not resolve to one.
                if (mr.Parent.Kind == HandleKind.TypeSpecification
                    && ResolveTypeTokenForScan(module, mr.Parent, ctx) is
                        { Kind: TypeKind.Class, Class: { IsInterface: true } pi }
                    && pi.Context.TypeArgs.Length == 1)
                {
                    switch (GenericDefFullName(pi))
                    {
                        case "System.IEquatable" when name == "Equals"
                            && EffectiveTypedEquals(c) is { } teq:
                            Reach(teq);
                            return;
                        case "System.IComparable" when name == "CompareTo"
                            && EffectiveTypedCompareTo(c) is { } tct:
                            Reach(tct);
                            return;
                    }
                }
                break;
            case HandleKind.MethodDefinition:
                var md = module.Reader.GetMethodDefinition((MethodDefinitionHandle)calleeHandle);
                name = module.Reader.GetString(md.Name);
                sig = md.DecodeSignature(SigProvider, ctx);
                break;
            default:
                return;
        }
        // A `constrained. <int> callvirt ISpanFormattable::TryFormat`
        // (a `{value:fmt}` interpolation hole lowered through DefaultInterpolatedString
        // Handler) is emitted inline via dn2cpp_try_format_int|uint_c (TryEmitValueConstrained),
        // so do NOT reach the integer primitive's real TryFormat body — it pulls in the whole
        // System.Number.TryFormat* subtree (the dominant remaining self-host cascade, reached
        // from SRM SignatureDecoder.CheckHeader via Byte.TryFormat). The concrete cut for the
        // direct (non-constrained) call lives in ResolveCallTarget.
        //
        // ONE predicate, shared with the emit route (TryEmitValueConstrained asks
        // CoreIntrinsics.LoweredIntegerTryFormat too) and with ReachVirtualImpl. No asker
        // restates the name set: a copy that drifted from this one would surface as an
        // undefined symbol at C++ LINK time, with no cause attached. The route is TOTAL
        // over the predicate, so a TryFormat overload shape the emitter does not model
        // throws loudly rather than falling through to a body this cut already deleted.
        if (CoreIntrinsics.CvIntegerTryFormat.Matches(c.FullName, name))
            return;
        // The enum analogue: `constrained. <enum> callvirt ISpanFormattable::TryFormat`,
        // out of a `{enumValue}` interpolation hole lowered through
        // DefaultInterpolatedStringHandler. Emit boxes the enum and formats it via
        // dn2cpp_enum_format + dn2cpp_string_try_copy_to_span (TryEmitValueConstrained), so
        // the real Enum.System.ISpanFormattable.TryFormat body — which reaches
        // TryFormatPrimitiveDefault -> GetEnumInfo -> GetEnumValuesAndNames (InternalCall) —
        // must not enter the tree. This is the CONSTRAINED mouth of the boxed-dispatch row
        // CoreIntrinsics.CvEnumTryFormat; it cannot be that row because the predicate is
        // non-pure (the token names the interface method "TryFormat" over an arbitrary enum
        // type name, so only IsEnum distinguishes it — see the row's doc). The same non-pure
        // test guards the emit route, so cut and route cannot drift.
        if (c.IsEnum && name == "TryFormat")
            return;
        // `constrained. <struct> callvirt object::Equals|GetHashCode` on a struct that
        // overrides neither: the emit boxes the receiver and dispatches through the runtime
        // helper (MethodCompiler's constrained arms), so the answer comes from the type-info
        // slot — which has to carry the structural walk, or the helper falls back to
        // reference equality / an identity hash and answers wrong. There is no `box` opcode
        // on this path, so nothing else marks the type boxed; wire it here.
        // (ToString needs nothing: an unwired tostring slot IS ValueType.ToString().)
        if (name is "Equals" or "GetHashCode" && !c.IsByRefLike
            && (name == "GetHashCode" ? EffectiveGetHashCode(c) : EffectiveEquals(c)) is null)
        {
            NoteObjectEqualityDispatch();
            ReachSynthesizedValueEquality(c,
                includeHash: name == "GetHashCode", includeEquals: name == "Equals");
        }
        // Past the cuts, the emitted call is a direct call to the body ConstrainedImplOf
        // picks (MethodCompiler.EmitConstrainedCall asks the same function), so reach that
        // one. INVARIANT: no canon-placeholder guard here — the emit side's value-type path
        // has none either, so a shared canonical body's `constrained.<Wrap<!T>> callvirt`
        // emits a direct call that only this edge reaches.
        if (resolvedCallee is not null && ConstrainedImplOf(c, resolvedCallee) is { } bound)
        {
            Reach(bound);
            return;
        }
        // No resolved callee (or none bound): fall back to the name+shape walk, which can
        // still answer a MethodDefinition token or an unresolvable MemberRef.
        //
        // The same renderer the key itself is built from (MethodInfo.SigKey), rather than a
        // second copy of its format — the two have to agree exactly or every scan below misses.
        string key = name + AbiContract.SigShape(sig);
        for (var b = c; b is not null; b = b.BaseClass)
        {
            EnsureCompleted(b);
            // The emit side binds an explicit interface implementation first — its dotted
            // metadata name means the SigKey scan below can never see it. Mirror that here
            // or the bound body is never reached and the emitted call dangles
            // (Regex.ValueMatchEnumerator's `IDisposable.Dispose`). Matching by the
            // DECLARATION's SigKey is a superset of the emit side's exact-method probe (two
            // interfaces can declare the same name+shape), so reach every match:
            // over-reaching is safe, a dangling symbol is not.
            bool matched = false;
            foreach (var kv in b.ExplicitInterfaceImpls)
                if (kv.Key.Name == name && kv.Key.SigKey == key)
                {
                    Reach(kv.Value);
                    matched = true;
                }
            if (matched)
                return;
            if (b.Methods.FirstOrDefault(x => !x.IsStatic && x.Rva != 0
                    && x.Name == name && x.SigKey == key) is { } impl)
            {
                Reach(impl);
                return;
            }
        }
    }

    /// <summary>Reaches the constrained type's <b>static</b> implementation of the
    /// static-abstract interface member <paramref name="target"/> (already resolved +
    /// substituted) invoked through <c>constrained. call</c> (generic math, Linq's
    /// IMinMaxCalc&lt;T&gt; Min/Max comparers, user static-virtual interfaces — on value
    /// types and concrete reference classes alike). The static analogue of
    /// <see cref="ReachConstrainedImpl"/>: the emit side resolves the call to the type's
    /// static impl (<see cref="ResolveStaticVirtualImpl"/>), so that impl must be transpiled
    /// or the generated call references an undeclared function.</summary>
    private void ReachStaticVirtualImpl(ClassInfo cls, MethodInfo target)
    {
        if (ResolveStaticVirtualImpl(cls, target) is { } impl)
            Reach(impl);
    }

    /// <summary>The <c>IBinaryInteger&lt;TSelf&gt;.ReadBigEndian(ReadOnlySpan&lt;byte&gt;|
    /// Span&lt;byte&gt;, bool)</c> span overload on an integer-primitive TSelf — the shape
    /// <see cref="MethodCompiler.TryEmitGenericMathIntrinsic"/> lowers to the runtime
    /// <c>dn2cpp_read_big_endian</c>. It is a static-virtual whose DEFAULT interface body
    /// calls the static-abstract <c>TryReadBigEndian</c> (an InternalCall with no IL that
    /// dn2cpp cannot transpile), so the emit route replaces the whole call and the reach
    /// side must delete that DIM body. This single predicate is asked by BOTH askers of the
    /// constrained-static-virtual <c>call</c> pair (the reach cut before the ordinary
    /// <c>Reach(t)</c> edge, and the emit route) so <c>cut ⟹ route</c> cannot drift. Name
    /// first, so the signature decode runs only for methods actually named ReadBigEndian.</summary>
    public bool IsInterceptedReadBigEndian(MethodInfo m) =>
        m.Name == "ReadBigEndian"
        && m.DeclaringClass.IsInterface
        && m.DeclaringClass.Namespace.StartsWith("System.Numerics", StringComparison.Ordinal)
        && m.Signature.ReturnType is { Kind: TypeKind.Primitive } rt
        && CoreIntrinsics.IsIntegerPrimitiveCode(rt.Primitive)
        && m.Signature.ParameterTypes is
            [{ Kind: TypeKind.Class, Class: { } sc }, { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Boolean }]
        && GenericDefFullName(sc) is "System.ReadOnlySpan" or "System.Span";

    /// <summary>Records that a virtual/interface slot is dispatched (some
    /// callvirt/ldvirtftn targets <paramref name="decl"/>) and reaches that slot's
    /// implementation in every already-allocated reference type.</summary>
    private void ReachUsedVirtual(MethodInfo decl)
    {
        if (!_usedVirtualDecls.Add(decl))
            return;
        foreach (var c in _allocatedRefTypes)
            ReachVirtualImpl(c, decl);
        // String's interface map (when wired) dispatches like an allocated type,
        // but String never enters _allocatedRefTypes — cross its impls in here.
        ReachStringVirtualImpl(decl);
        // The boxed-enum map (when wired) likewise: System.Enum never enters
        // _allocatedRefTypes (boxes are minted under the concrete enum's own ti_),
        // so its interface impls cross in here too.
        ReachEnumVirtualImpl(decl);
    }

    // ---- Generic virtual method (GVM) dispatch ----
    //
    // A generic *virtual* method (a virtual whose own signature has method type
    // parameters, e.g. System.Linq's `Enumerable.Iterator<TSource>.Select<TResult>`)
    // can't live in the flat per-type vtable: the slot would have to vary with the
    // method's type arguments. dn2cpp skips such methods when building a vtable
    // (BuildVtableForSpecialization), so an instantiation reached only through a
    // callvirt gets VtableSlot == -1 and the naive codegen would emit `vtable[-1]`.
    //
    // Instead each used GVM instantiation (closed declaring type + method args, e.g.
    // Iterator<Int32>.Select<Int32>) gets a dedicated type-switch dispatcher
    // (dn2cpp_gvm_*) emitted by CppEmitter: it branches on the receiver's concrete
    // type to the matching override and falls back to the base default. Reachability
    // mirrors the non-generic used-virtual × allocated-type cross product
    // (_usedVirtualDecls / ReachVirtualImpl): every allocated concrete type assignable
    // to the GVM's declaring type contributes its override (instantiated at the same
    // method args), and a newly-allocated type re-runs against every used GVM.
    //
    // Safety: if no override is found for a concrete type, the dispatcher routes to
    // the base default, which is semantically correct (it just loses the fusion the
    // override would have provided), so an incomplete override match never miscompiles.
    //
    // A GVM declared on an *interface* (IOrderedEnumerable<TElement>.
    // CreateOrderedEnumerable<TKey>, behind Enumerable.ThenBy) goes through the same
    // dispatcher: its closed instantiation has no interface-table slot either (the
    // plain interface dispatch would index the slots array at -1). The cases are the
    // allocated types implementing the closed interface, each bound to its own —
    // usually explicit, dotted-name — implementation template instantiated at the
    // call's method args; with no default body to fall back on, a case-less dispatch
    // traps, matching an abstract slot that cannot bind.

    internal sealed class GvmDispatch
    {
        public required MethodInfo Gvm;          // the base instantiation (declaring type's default impl)
        public required ClassInfo Decl;          // closed declaring type the callvirt is statically typed to
        public required TypeDesc[] MethodArgs;   // the method's type arguments (closed)
        public required string WantKey;          // open parameter signature, for override template matching
        public int ParamCount;
        // concrete allocated type -> its override impl (or Gvm itself for the base default).
        public readonly Dictionary<ClassInfo, MethodInfo> Cases = new();
    }

    private readonly Dictionary<string, GvmDispatch> _usedGvms = new();

    /// <summary>The registered generic-virtual-method dispatchers, for emission.</summary>
    internal IEnumerable<GvmDispatch> UsedGvms => _usedGvms.Values.OrderBy(g => g.Gvm.CppName, StringComparer.Ordinal);

    /// <summary>True when a callvirt target is a generic virtual method instantiation
    /// that needs a GVM dispatcher (rather than a vtable or interface-table slot): a
    /// method with its own type arguments, virtual, with no assigned vtable slot.
    /// Declared on a class, the dispatcher's cases are the allocated subclasses'
    /// overrides; declared on an interface, the allocated implementing types'
    /// (possibly explicit) implementations — an interface GVM's closed instantiation
    /// has no interface-table slot either, so routing it through the plain interface
    /// dispatch would index the slots array at -1.</summary>
    internal static bool IsGvmCall(MethodInfo m) =>
        m.IsVirtual && !m.IsStatic && m.VtableSlot < 0 && m.NameSuffix.Length > 0;

    /// <summary>The C++ name of the type-switch dispatcher for a GVM instantiation.
    /// Derived from the instantiation's own (unique) method name so the call site,
    /// the registration, and the emitter all agree without threading state.</summary>
    internal static string GvmDispatchName(MethodInfo gvm) => "dn2cpp_gvm_" + gvm.CppName.Substring(2);

    /// <summary>Registers a used GVM instantiation and reaches each allocated type's
    /// override at its method args (mirrors <see cref="ReachUsedVirtual"/>).</summary>
    private void ReachUsedGvm(MethodInfo gvm)
    {
        if (_usedGvms.ContainsKey(gvm.CppName))
            return;
        var openParams = gvm.Module.Reader.GetMethodDefinition(gvm.Handle)
            .DecodeSignature(SigProvider, GenericContext.Empty).ParameterTypes;
        var disp = new GvmDispatch
        {
            Gvm = gvm,
            Decl = gvm.DeclaringClass,
            MethodArgs = gvm.Context.MethodArgs,
            WantKey = string.Join(",", openParams.Select(p => p.ToString())),
            ParamCount = gvm.Signature.ParameterTypes.Length,
        };
        _usedGvms.Add(gvm.CppName, disp);
        foreach (var c in _allocatedRefTypes.ToList())
            ReachGvmImpl(disp, c);
    }

    /// <summary>Reaches concrete type <paramref name="c"/>'s override of GVM
    /// <paramref name="disp"/> at the dispatcher's method args, recording the case.
    /// Routes to the base default when <paramref name="c"/> declares no override.</summary>
    private void ReachGvmImpl(GvmDispatch disp, ClassInfo c)
    {
        if (disp.Cases.ContainsKey(c))
            return;
        if (disp.Decl.IsInterface)
        {
            // Interface GVM: the cases are the allocated types implementing the
            // closed interface, each dispatching to its own (usually explicit)
            // implementation instantiated at the dispatcher's method args. An
            // explicit implementation's metadata name is the source-qualified
            // dotted form ("System.Linq.IOrderedEnumerable<TElement>.
            // CreateOrderedEnumerable"), so the template lookup matches by
            // suffix + the interface's simple name as well as by plain name.
            if (c.IsInterface || !ImplementsInterface(c, disp.Decl))
                return;
            // A closed spec's Name is the mangled form ("IOrderedEnumerable_String");
            // the explicit implementation's dotted qualifier names the *definition*
            // ("…IOrderedEnumerable<TElement>…"), so take the typedef's simple name.
            string itfSimple = disp.Decl.Module.Reader.GetString(
                disp.Decl.Module.Reader.GetTypeDefinition(disp.Decl.Handle).Name);
            int tick = itfSimple.IndexOf('`');
            if (tick >= 0)
                itfSimple = itfSimple[..tick];
            for (var b = c; b is not null; b = b.BaseClass)
            {
                var tmpl = FindGenericMethodTemplate(b.Module, b.Handle, disp.Gvm.Name,
                    disp.MethodArgs.Length, disp.ParamCount, disp.WantKey, itfSimple);
                if (tmpl is null)
                    continue;
                var impl = InstantiateMethodOnClass(b, b.Module, tmpl.Value, disp.MethodArgs);
                Reach(impl);
                disp.Cases[c] = impl;
                return;
            }
            // No implementation template anywhere in the chain: the type is never
            // dispatched through this GVM (no case emitted; the dispatcher's
            // fallback traps, matching an abstract slot that cannot bind).
            return;
        }
        if (!DerivesFromOrIs(c, disp.Decl))
            return;
        // Walk from the concrete type up to the GVM's declaring type, taking the most
        // derived override template. A match whose declaring type def is the GVM's own
        // (the base virtual) means c does not override -> route to the base default.
        for (var b = c; b is not null; b = b.BaseClass)
        {
            var tmpl = FindGenericMethodTemplate(b.Module, b.Handle, disp.Gvm.Name,
                disp.MethodArgs.Length, disp.ParamCount, disp.WantKey);
            if (tmpl is null)
                continue;
            if (b.Handle == disp.Decl.Handle && b.Module == disp.Decl.Module)
            {
                disp.Cases[c] = disp.Gvm; // no override below: the base default applies
                return;
            }
            var impl = InstantiateMethodOnClass(b, b.Module, tmpl.Value, disp.MethodArgs);
            Reach(impl);
            disp.Cases[c] = impl;
            return;
        }
        disp.Cases[c] = disp.Gvm; // no template found at all: base default
    }

    /// <summary>If <paramref name="msh"/> is one of the element-scanning generic
    /// intrinsics — <c>Array.{IndexOf,LastIndexOf}&lt;T&gt;</c>, or a
    /// <c>MemoryExtensions</c> span scan — reaches the equality T's elements are
    /// compared by. The scan is an emitted loop, not an IL callvirt through
    /// <c>EqualityComparer&lt;T&gt;</c>, so the MemberRef hook that catches a real
    /// comparer dispatch never sees it: <c>List&lt;Point&gt;.Contains</c> resolves to a
    /// MethodSpec of an intrinsic-mapped type and names Point's Equals nowhere an edge
    /// can be read from. Without this the emit would call an Equals that was never
    /// transpiled.
    ///
    /// The hash half is deliberately NOT reached: a Contains does not hash, and
    /// pulling a struct's GetHashCode body in behind every List&lt;T&gt;.Contains would
    /// drag its whole formatting/HashCode cascade into a program that never hashes.
    ///
    /// A span scan's net9+ overloads carry a trailing <c>IEqualityComparer&lt;T&gt;</c>,
    /// and the emitted loop dispatches a genuine comparer object through the interface
    /// slot (the span-scan comparer hoist in MethodCompiler.GenericIntrinsic — the same
    /// template as <c>TryEmitComparerDispatch</c>). That dispatch is no IL callvirt
    /// either, so mark the closed interface's <c>Equals</c> used here: the
    /// used×allocated cross then reaches every allocated implementer's Equals,
    /// whichever order the allocation and this scan arrive in. Equals only — a scan
    /// never hashes (same rationale as the hash half above).</summary>
    private void ReachElementScanEquality(Module module, MethodSpecificationHandle msh, GenericContext callerCtx)
    {
        var ms = module.Reader.GetMethodSpecification(msh);
        bool arrayScan = MethodSpecParentTypeName(module, ms) == "System.Array"
            && MethodSpecMethodName(module, ms) is "IndexOf" or "LastIndexOf";
        // Every MemoryExtensions generic reads its elements with the same equality
        // (Contains/IndexOf/SequenceEqual/StartsWith/…); Sort is the ordering
        // exception, and it is ReachSortComparerCompare's.
        bool spanScan = MethodSpecParentTypeName(module, ms) == "System.MemoryExtensions"
            && MethodSpecMethodName(module, ms) != "Sort";
        if (!arrayScan && !spanScan)
            return;
        var margs = ms.DecodeSignature(SigProvider, callerCtx).ToArray();
        if (margs.Length >= 1)
            ReachValueKeyEquality(margs[0], includeHash: false);
        if (!spanScan || margs.Length < 1)
            return;
        // The trailing-comparer overloads only: decode the member signature closed at
        // the spec args (the same decode ReachSortComparerCompare makes) and test the
        // last parameter — the comparerless overloads mark nothing.
        var sctx = new GenericContext(System.Array.Empty<TypeDesc>(), margs);
        var sig = ms.Method.Kind == HandleKind.MethodDefinition
            ? module.Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(SigProvider, sctx)
            : module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(SigProvider, sctx);
        if (sig.ParameterTypes is not { Length: >= 2 } pts
            || pts[^1] is not { Kind: TypeKind.Class, Class: { } icls }
            || GenericDefFullName(icls) != "System.Collections.Generic.IEqualityComparer")
            return;
        icls.EnsureMembers();
        foreach (var im in icls.Methods)
            if (!im.IsStatic && im.Name == "Equals")
                ReachUsedVirtual(im);
    }

    /// <summary>The members an <c>Array.Sort</c> / <c>Array.BinarySearch</c> /
    /// <c>MemoryExtensions.Sort</c> spec needs reached. Neither the comparer dispatch nor
    /// the element ordering is an IL callvirt — both are emitted (a captureless thunk, an
    /// inline compare) on a MethodSpec of an intrinsic-mapped type — so nothing else in the
    /// scan can see them.
    ///
    /// Two edges, and the second is easy to miss: a comparer ARGUMENT may be null at run
    /// time, and null means <c>Comparer&lt;T&gt;.Default</c> — that is the arm
    /// <c>List&lt;T&gt;.Sort()</c> lands on, which reaches
    /// <c>Array.Sort(…, (IComparer&lt;T&gt;)null)</c> and nothing else. So the default order
    /// is reachable from EVERY overload, comparer or not.</summary>
    private void ReachSortComparerCompare(Module module, MethodSpecificationHandle msh, GenericContext callerCtx)
    {
        var ms = module.Reader.GetMethodSpecification(msh);
        if (MethodSpecMethodName(module, ms) is not ("Sort" or "BinarySearch")
            || MethodSpecParentTypeName(module, ms) is not ("System.Array" or "System.MemoryExtensions"))
            return;
        // Decode the parameters closed (T substituted from the spec args).
        var margs = ms.DecodeSignature(SigProvider, callerCtx).ToArray();
        if (margs.Length == 0)
            return;
        ReachDefaultOrder(margs[0]);
        var sctx = new GenericContext(System.Array.Empty<TypeDesc>(), margs);
        var sig = ms.Method.Kind == HandleKind.MethodDefinition
            ? module.Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(SigProvider, sctx)
            : module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(SigProvider, sctx);
        if (sig.ParameterTypes is not { Length: > 0 } pts
            || pts[^1] is not { Kind: TypeKind.Class, Class: { } ccls }
            || !(ccls.IsDelegate || ccls.IsInterface || FindComparerInterface(ccls) is not null))
            return;
        EnsureCompleted(ccls);
        if (ccls.IsDelegate)
        {
            // Comparison<T> — the thunk calls the delegate's invoke (dginvoke_*); reach
            // Invoke so that function is emitted.
            if (ccls.Methods.FirstOrDefault(mm => mm.Name == "Invoke") is { } invoke)
                Reach(invoke);
            return;
        }
        // IComparer<T>.Compare — dispatched by the thunk via the interface table. The param
        // is the IComparer<T> interface itself for Array.Sort, but a *concrete* comparer
        // class for MemoryExtensions.Sort<T,TComparer>; for the latter, reach the
        // Compare on the IComparer<T> interface it implements.
        ClassInfo? itf = ccls.IsInterface && GenericDefFullName(ccls) == "System.Collections.Generic.IComparer"
            ? ccls
            : FindComparerInterface(ccls);
        if (itf is not null && itf.Methods.FirstOrDefault(mm => mm.Name == "Compare") is { } cmpDecl)
            ReachUsedVirtual(cmpDecl);
    }

    /// <summary>The reachability counterpart of <see cref="ReachSortComparerCompare"/> for the
    /// NON-generic boxed-ordering surface. Two families, because both dispatch a boxed
    /// element's order through an emitted <c>dn2cpp_resolve_interface</c> / <c>dn2cpp_object_compare</c>
    /// rather than an IL callvirt reachability sees:
    /// <list type="bullet">
    /// <item><c>Array.Sort(Array[, index, length][, IComparer])</c> / <c>Array.BinarySearch(Array,
    /// object[, index, length][, IComparer])</c> — System.Array is intrinsic-mapped, so
    /// <see cref="ResolveCallTarget"/> returned null; mark non-generic <c>System.IComparable</c>
    /// (the default order) and, for a comparer overload, <c>System.Collections.IComparer</c> used and
    /// emit their type-infos.</item>
    /// <item><c>System.Collections.Comparer.Compare</c> / <c>IComparer.Compare</c> — the
    /// body-intercepted <c>Comparer.Compare</c> and its inline route both name
    /// <c>&amp;ti_System_IComparable</c> and dispatch a user element through <c>IComparable</c>.</item>
    /// </list>
    /// Additive and interface-agnostic — it names decls as used exactly as the generic path does for
    /// IComparer&lt;T&gt;, adding no rule to the interface-dispatch model.</summary>
    private void ReachNonGenericOrderingReferences(Module module, EntityHandle handle)
    {
        string? name = CallTargetMethodName(module, handle);
        if (name is not ("Sort" or "BinarySearch" or "Compare"))
            return;
        string? type = CallTargetTypeName(module, handle);
        // Comparer.Compare / IComparer.Compare in IL: the body-intercepted Comparer.Compare
        // and its inline route both name &ti_System_IComparable and dispatch a user element through
        // non-generic IComparable, so ensure both are emitted/wired even in a program that never
        // sorts an array (an ArrayList.Sort()/SortedList lookup reaches Comparer through the Array
        // path below instead — a C++ resolve_interface, invisible to this IL-call scan).
        if (name == "Compare")
        {
            if (type is "System.Collections.Comparer" or "System.Collections.IComparer")
                NoteNonGenericIComparableUsed();
            return;
        }
        if (type != "System.Array")
            return;
        // The comparerless / null-comparer default order dispatches IComparable.CompareTo(object).
        NoteNonGenericIComparableUsed();
        // A trailing IComparer overload dispatches IComparer.Compare(object, object).
        if (NonGenericArraySortHasComparerParam(module, handle)
            && FindClassByFullName("System.Collections.IComparer") is { } icmp)
        {
            EnsureCompleted(icmp);
            if (icmp.Methods.FirstOrDefault(mm => mm.Name == "Compare") is { } compare)
                ReachUsedVirtual(compare);
            NoteReferencedType(icmp);
        }
    }

    /// <summary>Lay down non-generic <c>System.IComparable</c>'s type-info (so an emitted
    /// <c>&amp;ti_System_IComparable</c> links) and mark its <c>CompareTo(object)</c> used-virtual (so
    /// every allocated element type contributes its impl, which <c>dn2cpp_object_compare</c>'s user-
    /// reference-type arm dispatches). Idempotent — the two sets it feeds dedupe.</summary>
    private void NoteNonGenericIComparableUsed()
    {
        if (FindClassByFullName("System.IComparable") is not { } ic)
            return;
        EnsureCompleted(ic);
        if (ic.Methods.FirstOrDefault(mm => mm.Name == "CompareTo") is { } compareTo)
            ReachUsedVirtual(compareTo);
        NoteReferencedType(ic);
    }

    /// <summary>Whether a non-generic Array.Sort/BinarySearch call's last parameter is
    /// <c>System.Collections.IComparer</c> — the overloads whose comparer arm the emitted lowering
    /// dispatches. A signature decode, safe in this scan context exactly as
    /// <see cref="ReachSortComparerCompare"/>'s is; the method and its declaring type are both
    /// non-generic, so the empty context is exact.</summary>
    private bool NonGenericArraySortHasComparerParam(Module module, EntityHandle handle)
    {
        try
        {
            var pts = handle.Kind == HandleKind.MethodDefinition
                ? module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle)
                    .DecodeSignature(SigProvider, GenericContext.Empty).ParameterTypes
                : module.Reader.GetMemberReference((MemberReferenceHandle)handle)
                    .DecodeMethodSignature(SigProvider, GenericContext.Empty).ParameterTypes;
            return pts.Length > 0
                && pts[^1] is { Kind: TypeKind.Class, Class.FullName: "System.Collections.IComparer" };
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return false;
        }
    }

    /// <summary>Reaches an element type's <c>Comparer&lt;T&gt;.Default</c> — the order a
    /// comparerless (or null-comparer) sort/search runs on. A no-op for a T that orders
    /// inline (primitive/string/enum/intrinsic value type): the emit devirtualizes it to an
    /// expression, so pulling GenericComparer&lt;T&gt; in behind a program that merely sorts
    /// ints would be pure footprint. A no-op too for a T with no order at all — real .NET's
    /// comparer throws for it, so the emit does, and instantiating a comparer around a
    /// CompareTo that does not exist would only fail to transpile.</summary>
    private void ReachDefaultOrder(TypeDesc elem)
    {
        if (DefaultComparerFor(elem) is not { } gc)
            return;
        // A struct's typed CompareTo(T): called directly by the inline BinarySearch compare,
        // and devirtualized inside GenericComparer<T>.Compare.
        if (elem is { Kind: TypeKind.Class, Class: { IsValueType: true } sc }
            && TranspiledTypedCompareTo(sc) is { } tct)
            Reach(tct);
        if (ParameterlessCtor(gc) is { } gctor)
            Reach(gctor);
        ReachAllocatedType(gc);
        // The comparer object is dispatched through IComparer<T> by an emitted thunk, not an
        // IL callvirt — mark the slot used, or the allocated comparer's Compare is never
        // emitted and the interface row stays nullptr.
        if (ComparerInterfaceFor(elem)?.Methods.FirstOrDefault(mm => mm.Name == "Compare") is { } cmpDecl)
            ReachUsedVirtual(cmpDecl);
    }

    /// <summary>Whether a <c>MemoryExtensions.{IndexOfAny*,ContainsAny*}</c> method spec
    /// is the 2-arg <c>SearchValues&lt;T&gt;</c> overload — intercepted inline as a runtime
    /// set scan (see MethodCompiler), so its real SIMD/ProbabilisticMap body is neither a
    /// reachability edge nor a managed call. The ROS&lt;T&gt;/value/fixed-arity forms return
    /// false (they keep their normal scalar handling / real-IL transpile).</summary>
    public bool IsMemoryExtSearchValuesForm(Module module, MethodSpecification ms, GenericContext ctx)
    {
        var margs = ms.DecodeSignature(SigProvider, ctx).ToArray();
        var sctx = new GenericContext(System.Array.Empty<TypeDesc>(), margs);
        var sig = ms.Method.Kind == HandleKind.MethodDefinition
            ? module.Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(SigProvider, sctx)
            : module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(SigProvider, sctx);
        return sig.ParameterTypes is [_, { Kind: TypeKind.Class, Class: { } svcls }]
            && GenericDefFullName(svcls) == "System.Buffers.SearchValues";
    }

    /// <summary>The closed <c>IComparer&lt;T&gt;</c> interface <paramref name="c"/>
    /// implements (transitively), or null. A concrete comparer passed to
    /// <c>span.Sort(TComparer)</c> is dispatched through this interface.</summary>
    private ClassInfo? FindComparerInterface(ClassInfo c)
    {
        for (var b = c; b is not null; b = b.BaseClass)
            foreach (var i in b.Interfaces)
            {
                if (GenericDefFullName(i) == "System.Collections.Generic.IComparer")
                    return i;
                if (FindComparerInterface(i) is { } nested)
                    return nested;
            }
        return null;
    }

    /// <summary>True if the call target is <c>Comparer&lt;T&gt;.get_Default</c>. Its real
    /// getter routes through reflection (CreateInstanceForAnotherGenericParameter); we
    /// intercept it and synthesize a <c>GenericComparer&lt;T&gt;</c> instance instead so
    /// the comparer can be stored and dispatched virtually (e.g. by SortedDictionary's
    /// tree), not only devirtualized at a direct call site.</summary>
    internal bool IsComparerGetDefault(MethodInfo m) =>
        m.Name == "get_Default"
        && m.DeclaringClass.GenericArity == 1
        && GenericDefFullName(m.DeclaringClass) == "System.Collections.Generic.Comparer";

    /// <summary>True if the call target is a culture-sensitive <c>StringComparer</c>
    /// factory getter (<c>CurrentCulture</c>/<c>InvariantCulture</c>). Its real getter
    /// allocates a <c>CultureAwareComparer</c> wrapping a <c>CompareInfo</c> (ICU); we
    /// intercept it to an ordinal <c>GenericComparer&lt;string&gt;</c> instead — dn2cpp
    /// has no culture support, so string ordering is ordinal (matching the
    /// <c>Comparer&lt;string&gt;.Default</c> / <c>string.CompareTo</c> lowering). This is
    /// what makes the real <c>System.Linq</c> <c>OrderBy</c>-over-<c>string</c> path
    /// transpilable: its <c>EnumerableSorter</c> substitutes <c>StringComparer.CurrentCulture</c>
    /// for <c>Comparer&lt;string&gt;.Default</c>, and cutting it here keeps the whole
    /// <c>CompareInfo</c>/<c>GlobalizationMode</c>/ICU subtree unreachable.</summary>
    internal bool IsStringComparerCultureGetter(MethodInfo m) =>
        m.DeclaringClass.FullName == "System.StringComparer"
        && m.Name is "get_CurrentCulture" or "get_InvariantCulture";

    /// <summary>True if the handle is a <c>System.Activator.CreateInstance&lt;T&gt;()</c>
    /// MethodSpec — the generic factory lowered inline to <c>new T()</c>.</summary>
    private bool IsActivatorCreateInstanceSpec(Module module, MethodSpecificationHandle msh)
    {
        var ms = module.Reader.GetMethodSpecification(msh);
        return MethodSpecParentTypeName(module, ms) == "System.Activator"
            && MethodSpecMethodName(module, ms) == "CreateInstance";
    }

    /// <summary>The closed <c>GenericComparer&lt;T&gt;</c> backing
    /// <c>Comparer&lt;T&gt;.Default</c> for a comparable element type — instantiated and
    /// completed so its ctor/Compare are available — or null if GenericComparer`1 is not
    /// loaded (no CoreLib).</summary>
    internal ClassInfo? GenericComparerFor(TypeDesc elem)
    {
        if (!TypeIndex().TryGetValue(("System.Collections.Generic", "GenericComparer`1"), out var cands))
            return null;
        var (mod, tdh) = cands[0];
        var cls = Instantiate(mod, tdh, new[] { elem });
        EnsureCompleted(cls);
        return cls;
    }

    /// <summary>The closed <c>IComparer&lt;T&gt;</c> interface for an element type — what
    /// every comparer a sort/search dispatches (a user's, or a synthesized
    /// <c>Comparer&lt;T&gt;.Default</c>) is reached through. Null if IComparer`1 is not
    /// loaded (no CoreLib).</summary>
    internal ClassInfo? ComparerInterfaceFor(TypeDesc elem)
    {
        if (!TypeIndex().TryGetValue(("System.Collections.Generic", "IComparer`1"), out var cands))
            return null;
        var (mod, tdh) = cands[0];
        var cls = Instantiate(mod, tdh, new[] { elem });
        EnsureCompleted(cls);
        return cls;
    }

    /// <summary>The closed <c>IEqualityComparer&lt;T&gt;</c> interface for an element type —
    /// the interface a user <c>class MyCmp : EqualityComparer&lt;T&gt;</c> (and a
    /// Dictionary/HashSet's <c>_comparer</c>) dispatches through. Null if IEqualityComparer`1
    /// is not loaded (no CoreLib). The interface is NOT intrinsic-mapped, so
    /// <see cref="EnsureCompleted"/> populates its <c>Methods</c>/<c>VtableSlot</c>
    /// normally — unlike the opaque <c>EqualityComparer&lt;T&gt;</c> base itself.</summary>
    internal ClassInfo? IEqualityComparerInterfaceFor(TypeDesc elem)
    {
        if (!TypeIndex().TryGetValue(("System.Collections.Generic", "IEqualityComparer`1"), out var cands))
            return null;
        var (mod, tdh) = cands[0];
        var cls = Instantiate(mod, tdh, new[] { elem });
        EnsureCompleted(cls);
        return cls;
    }

    /// <summary>The non-generic comparer interface inherited by every
    /// <c>EqualityComparer&lt;T&gt;</c>. Its calls use the same default-object lowering as
    /// Tuple's structural equality path.</summary>
    internal ClassInfo? NonGenericEqualityComparerInterface()
    {
        if (!TypeIndex().TryGetValue(("System.Collections", "IEqualityComparer"), out var cands)
            || !cands[0].Item1.ClassMap.TryGetValue(cands[0].Item2, out var cls))
            return null;
        EnsureCompleted(cls);
        return cls;
    }

    /// <summary>If <paramref name="c"/>'s base chain reaches the intrinsic-opaque
    /// <c>EqualityComparer&lt;T&gt;</c> (the shape a user comparer subclass takes), the
    /// element type <c>T</c> it was closed at; otherwise null. Walks the base chain because
    /// the derivation may be indirect. The base's shape decode (<see cref="CompleteShape"/>)
    /// short-circuits on the intrinsic map before populating its interfaces, so the inherited
    /// <c>IEqualityComparer&lt;T&gt;</c> is invisible to the emitter's base-chain interface
    /// walk — <see cref="ReachAllocatedType"/> uses this to hand the concrete subclass the
    /// interface directly.</summary>
    internal TypeDesc? EqualityComparerElement(ClassInfo c)
    {
        for (var b = c.BaseClass; b is not null; b = b.BaseClass)
            if (b.IntrinsicCppName is not null
                && GenericDefFullName(b) == "System.Collections.Generic.EqualityComparer"
                && b.Context.TypeArgs.Length == 1)
                return b.Context.TypeArgs[0];
        return null;
    }

    /// <summary>Closed <c>IEqualityComparer&lt;T&gt;</c> interfaces (concrete AND their
    /// shared-generics canonical forms) some allocated comparer receiver carries — a user
    /// <c>EqualityComparer&lt;T&gt;</c> subclass, a direct implementer, a boxed struct
    /// comparer — precisely the ones whose type-info <see cref="ReachAllocatedType"/>
    /// caused to be emitted (the receiver's RenderItfTables row lays it down), so
    /// dispatching a real receiver through them is link-safe.</summary>
    private readonly HashSet<ClassInfo> _userComparerInterfaces = new();
    private bool _anyUserComparerSubclass;

    /// <summary>The closed <c>IEqualityComparer&lt;T&gt;</c> method (Equals / GetHashCode) a
    /// non-Default comparer receiver — an <c>EqualityComparer&lt;T&gt;</c>-typed call's
    /// receiver, or a MemoryExtensions span scan's trailing comparer operand
    /// — should dispatch through, but ONLY when an implementer for this element type was
    /// actually allocated (its interface type-info is therefore emitted). With no such
    /// implementer, every comparer value a program can normally hold is Default, so the
    /// caller's default-op is both correct and the only link-safe choice —
    /// routing through an un-emitted interface type-info would fail at C++ link time. (The
    /// span scans additionally keep their runtime guard on that arm, for the one receiver
    /// reachability cannot see: a reflection-built comparer.) The leading bool short-
    /// circuits the common (no-implementer) program before instantiating anything.</summary>
    internal MethodInfo? UserComparerInterfaceMethod(TypeDesc elem, string name)
    {
        if (!_anyUserComparerSubclass || IEqualityComparerInterfaceFor(elem) is not { } itf
            || !_userComparerInterfaces.Contains(itf))
            return null;
        foreach (var m in itf.Methods)
            if (!m.IsStatic && m.Name == name)
                return m;
        return null;
    }

    /// <summary>Whether an element type's default order is a devirtualized INLINE compare
    /// — a primitive, a string (ordinal), an enum (at its own width), or an intrinsic value
    /// type with a runtime three-way (Decimal/TimeSpan/DateTime/…). These need no comparer
    /// object at all, so the reachability must not drag <c>GenericComparer&lt;T&gt;</c> in
    /// behind a program that merely sorts ints. The emit's own branch order
    /// (MethodCompiler.TryCompareLValue) is what this mirrors.</summary>
    internal static bool HasInlineOrder(TypeDesc t) =>
        t.IsString
        || (t.Kind == TypeKind.Primitive && !t.IsObject)
        || t is { Kind: TypeKind.Class, Class.IsEnum: true }
        || MethodCompiler.IntrinsicValueTypeFn(t) is not null;

    /// <summary>The <c>GenericComparer&lt;T&gt;</c> that backs <c>Comparer&lt;T&gt;.Default</c>
    /// for an element whose natural order is a real <c>CompareTo</c> — a struct with a typed
    /// <c>IComparable&lt;T&gt;.CompareTo(T)</c>, or a reference type implementing
    /// <c>IComparable&lt;T&gt;</c>. Null both when T orders inline (see
    /// <see cref="HasInlineOrder"/> — no object needed) and when T has no order at all: real
    /// .NET's default comparer throws when it is asked to compare such a T, and instantiating
    /// GenericComparer&lt;T&gt; for it would only fail to transpile its <c>x.CompareTo(y)</c>.
    /// </summary>
    internal ClassInfo? DefaultComparerFor(TypeDesc elem)
    {
        if (HasInlineOrder(elem))
            return null;
        bool comparable = elem switch
        {
            { Kind: TypeKind.Class, Class: { IsValueType: true, IsEnum: false } sc } =>
                TranspiledTypedCompareTo(sc) is not null,
            { Kind: TypeKind.Class, Class: { IsValueType: false, IsInterface: false } rc } =>
                ImplementsGenericComparable(rc),
            _ => false,
        };
        return comparable && ComparerInterfaceFor(elem) is not null ? GenericComparerFor(elem) : null;
    }

    /// <summary>A synthesized <c>GenericComparer&lt;T&gt;.Compare</c> body dn2cpp cannot
    /// transpile because <c>T</c> is a <b>value type real .NET's default comparer cannot
    /// order</b> — not inline-ordered and with no typed <c>CompareTo</c> (Vector128&lt;byte&gt;
    /// and the SIMD family, or a plain non-<c>IComparable</c> struct). dn2cpp synthesizes a
    /// <c>GenericComparer&lt;T&gt;</c> for <em>every</em> <c>Comparer&lt;T&gt;.Default</c>
    /// (<see cref="GenericComparerFor"/> at the get_Default intercept), so a
    /// <c>ValueTuple&lt;Vector128&lt;byte&gt;,_&gt;.CompareTo</c> reaches this one; its real IL
    /// boxes the value type for the <c>x != null</c> / <c>y != null</c> checks (an intrinsic
    /// value type has no emitted <c>ti_</c> to box through) and then dispatches a
    /// <c>CompareTo</c> that does not exist. Real .NET's <c>Comparer&lt;T&gt;.Default</c> for
    /// such a T is an <c>ObjectComparer&lt;T&gt;</c> whose <c>Compare</c> boxes and dispatches
    /// non-generic <c>System.IComparable</c> — which the box does not implement, so it throws
    /// <c>ArgumentException</c>. So <see cref="CppEmitter"/> body-replaces this method with a
    /// catchable throw (the comparer object stays real; only its <c>Compare</c> faults, exactly
    /// where real .NET's does). Reference-type <c>T</c> (object, a non-comparable class) is NOT
    /// matched: its null-check boxes are identity no-ops and its <c>CompareTo</c> devirtualizes
    /// to <c>dn2cpp_object_compare</c> (MethodCompiler.TryCompareLValue's Object arm).</summary>
    internal bool IsUnorderableComparerCompareBody(MethodInfo m) =>
        m is { IsStatic: false, Name: "Compare" }
        && GenericDefFullName(m.DeclaringClass) == "System.Collections.Generic.GenericComparer"
        && m.DeclaringClass.Context.TypeArgs is [{ Kind: TypeKind.Class, Class: { IsValueType: true } tc } t]
        && !HasInlineOrder(t)
        && TranspiledTypedCompareTo(tc) is null;

    /// <summary>The typed <c>CompareTo(T)</c> a value type's DEFAULT order calls — its own,
    /// and only when it is a body dn2cpp actually transpiles. An intrinsic-mapped struct
    /// declares a real CompareTo in metadata whose body <see cref="Reach"/> cuts
    /// (Vector128&lt;T&gt; and the SIMD family: their bodies are emitted inline, never
    /// transpiled), so ordering a T by it would reach a method that is never emitted — a gap
    /// from inside a synthesized GenericComparer&lt;T&gt;, and from the inline BinarySearch
    /// compare a direct call to a symbol nothing ever defines. Such a T has no order the emit
    /// can build, and the sort/search faults the way real .NET's comparer would.
    ///
    /// The <see cref="NonIntrinsic"/> rule for the ordering half — spelled against BOTH tests
    /// Reach's cut uses, not just the name table (a closed generic intrinsic is caught only by
    /// the second).</summary>
    internal static MethodInfo? TranspiledTypedCompareTo(ClassInfo c) =>
        EffectiveTypedCompareTo(c) is { } m
        && !CoreIntrinsics.IsIntrinsicType(m.DeclaringClass.FullName)
        && m.DeclaringClass.IntrinsicCppName is null
            ? m
            : null;

    /// <summary>Whether a class implements <c>IComparable&lt;T&gt;</c> anywhere in its base
    /// chain — the target <c>GenericComparer&lt;T&gt;.Compare</c>'s <c>x.CompareTo(y)</c>
    /// resolves to.</summary>
    private bool ImplementsGenericComparable(ClassInfo c)
    {
        for (var b = c; b is not null; b = b.BaseClass)
        {
            EnsureCompleted(b);
            if (b.Interfaces.Any(Comparable))
                return true;
        }
        return false;

        bool Comparable(ClassInfo i) =>
            (GenericDefFullName(i) == "System.IComparable" && i.Context.TypeArgs.Length == 1)
            || i.Interfaces.Any(Comparable);
    }

    /// <summary>Resolves the three interface methods needed to enumerate an
    /// arbitrary <c>IEnumerable&lt;T&gt;</c> for <c>string.Join</c>/<c>Concat</c>:
    /// the closed <c>IEnumerable&lt;T&gt;.GetEnumerator</c>, the boxed
    /// <c>IEnumerator&lt;T&gt;.get_Current</c>, and the non-generic
    /// <c>IEnumerator.MoveNext</c> (inherited by <c>IEnumerator&lt;T&gt;</c>).
    /// Each is dispatched on its declaring *interface* type-info, so only the
    /// element type T is needed — the concrete operand class never appears here.
    /// Returns null if the BCL enumeration interfaces are not loaded (no CoreLib).</summary>
    internal (MethodInfo GetEnumerator, MethodInfo GetCurrent, MethodInfo MoveNext)? EnumerationDispatch(TypeDesc elem)
    {
        // Any IEnumerable<char> enumeration can receive a string at runtime (a
        // full-length string-backed ReadOnlyMemory<char> surfaces the string object
        // itself), so wire String's dispatch map. Idempotent — NoteStringInterfaces
        // sets its state before calling back in here for the char triple.
        if (elem is { Kind: TypeKind.Primitive, Primitive: PrimitiveTypeCode.Char })
            NoteStringInterfaces();
        var idx = TypeIndex();
        if (!idx.TryGetValue(("System.Collections.Generic", "IEnumerable`1"), out var enCands)
            || !idx.TryGetValue(("System.Collections.Generic", "IEnumerator`1"), out var erCands))
            return null;

        var ienumerable = Instantiate(enCands[0].Item1, enCands[0].Item2, new[] { elem });
        EnsureCompleted(ienumerable);
        var ienumerator = Instantiate(erCands[0].Item1, erCands[0].Item2, new[] { elem });
        EnsureCompleted(ienumerator);

        var getEnum = ienumerable.Methods.FirstOrDefault(
            m => m.Name == "GetEnumerator" && m.Signature.ParameterTypes.Length == 0);
        var getCur = ienumerator.Methods.FirstOrDefault(m => m.Name == "get_Current");
        // MoveNext is declared on the non-generic IEnumerator that IEnumerator<T>
        // extends; find it among the closed enumerator's (already completed)
        // interface map rather than re-resolving the non-generic type by name.
        var moveNext = ienumerator.Interfaces
            .FirstOrDefault(i => i.FullName == "System.Collections.IEnumerator")
            ?.Methods.FirstOrDefault(m => m.Name == "MoveNext" && m.Signature.ParameterTypes.Length == 0);

        if (getEnum is null || getCur is null || moveNext is null)
            return null;
        return (getEnum, getCur, moveNext);
    }

    /// <summary>Marks the three enumeration interface methods used so each allocated
    /// IEnumerable&lt;T&gt; collection's enumerator impl is emitted, and notes the
    /// three interface types referenced so their C++ struct typedefs exist — the
    /// bare-interface Join/Concat dual path names <c>IEnumerator&lt;T&gt;*</c> in its
    /// (runtime-dead-for-arrays but still compiled) enumerate branch even when no
    /// managed enumerator is ever allocated.</summary>
    private void ReachEnumeration((MethodInfo GetEnumerator, MethodInfo GetCurrent, MethodInfo MoveNext) ed)
    {
        ReachUsedVirtual(ed.GetEnumerator);
        ReachUsedVirtual(ed.GetCurrent);
        ReachUsedVirtual(ed.MoveNext);
        NoteReferencedType(ed.GetEnumerator.DeclaringClass);
        NoteReferencedType(ed.GetCurrent.DeclaringClass);
        NoteReferencedType(ed.MoveNext.DeclaringClass);
    }

    /// <summary>Resolves the non-generic <c>System.Collections.IEnumerable</c> + its
    /// parameterless <c>GetEnumerator</c>, marks the dispatch used (so an array's map emits
    /// the non-generic <c>GetEnumerator</c> thunk that wraps the array in a fresh
    /// <c>SZArrayEnumerable&lt;object&gt;</c>) and notes the interface type (so its type-info
    /// is emitted). Lets <c>System.Array.GetEnumerator()</c> lower to a dispatch on the
    /// array's own SZArray map instead of a compile-time wrap.
    /// Null when the non-generic enumeration interface isn't loaded (no CoreLib).</summary>
    internal (ClassInfo Itf, MethodInfo GetEnumerator)? ReachNonGenericArrayEnumerator()
    {
        if (!TypeIndex().TryGetValue(("System.Collections", "IEnumerable"), out var cands)
            || !cands[0].Item1.ClassMap.TryGetValue(cands[0].Item2, out var itf))
            return null;
        var ge = itf.Methods.FirstOrDefault(
            m => m.Name == "GetEnumerator" && m.Signature.ParameterTypes.Length == 0);
        if (ge is null || ge.VtableSlot < 0)
            return null;
        ReachUsedVirtual(ge);
        NoteReferencedType(itf);
        return (itf, ge);
    }

    /// <summary>The support-shim type <paramref name="ns"/>.<paramref name="name"/>, or a
    /// hard failure naming the remedy. The transpiler synthesizes types out of
    /// Dn2Cpp.Runtime.dll on demand, and every such lowering has a tempting silent
    /// fallback: skip the wiring, emit slightly different C++. The program then
    /// transpiles, compiles and links, and dies at run time on the first construct that
    /// needed the shim. Fail here instead, whichever way the shim went missing — a CLI
    /// installed without its sibling shim, an old bundle, a stale shim that predates the
    /// type. <paramref name="need"/> names the construct that demanded it, so the
    /// message says what will not work rather than only what is absent.</summary>
    private List<(Module Module, TypeDefinitionHandle Handle)> RequireShimType(string ns, string name, string need)
    {
        if (TypeIndex().TryGetValue((ns, name), out var cands) && cands.Count > 0)
            return cands;
        throw new NotSupportedException(
            $"Dn2Cpp.Runtime.dll (dn2cpp's managed support shim) is not loaded, but {need} needs "
            + $"{ns}.{name}. Pass Dn2Cpp.Runtime.dll with -r, or place it next to the dn2cpp executable.");
    }

    /// <summary>The closed <c>Dn2Cpp.Runtime.SZArrayEnumerable&lt;elem&gt;</c> wrapper
    /// and its <c>(elem[])</c> constructor, instantiated, completed, allocated and
    /// fully reached (so its interface-dispatch tables are emitted). The emit-time
    /// boundary (a <c>T[]</c> used where <c>IEnumerable&lt;T&gt;</c> is expected) calls
    /// this to box the array into a real managed enumerable; reaching it during body
    /// compilation is fine — the emit fixpoint picks up the newly-reachable wrapper
    /// methods (same pattern the generic-on-first-use discovery relies on). A missing
    /// or stale shim throws (see RequireShimType): without the wrapper the array would
    /// carry no interface map and the first dispatch through it would abort at run time.</summary>
    internal (ClassInfo Cls, MethodInfo Ctor) SZArrayEnumerableFor(TypeDesc elem)
    {
        const string Need = "exposing an array as a collection interface";
        var cands = RequireShimType("Dn2Cpp.Runtime", "SZArrayEnumerable`1", Need);
        var (mod, tdh) = cands[0];
        var cls = Instantiate(mod, tdh, new[] { elem });
        EnsureCompleted(cls);
        var ctor = cls.Methods.FirstOrDefault(
            m => m.Name == ".ctor" && m.Signature.ParameterTypes is [{ Kind: TypeKind.SZArray }]);
        if (ctor is null)
            throw new NotSupportedException(
                "Dn2Cpp.Runtime.SZArrayEnumerable`1 has no (T[]) constructor: the loaded "
                + "Dn2Cpp.Runtime.dll is stale or does not match this dn2cpp. It is needed for "
                + Need + ".");
        ReachAllocatedType(cls);
        Reach(ctor);
        // ReachAllocatedType wires the wrapper's impl of every interface slot the
        // program actually dispatches (the consumer's foreach / Count / indexer /
        // Contains callvirts already marked those decls used-virtual in the scan, and
        // the wrapper's closed interfaces are the same Instantiate-deduped instances).
        // So only the used members are reached — the size-changing IList<T> members
        // (Add/Insert/Remove/...) stay unreached unless the program dispatches them,
        // and an unused IList<T>.Contains doesn't drag EqualityComparer<T> in.
        foreach (var itf in cls.Interfaces)
            NoteReferencedType(itf);
        // Reaching happens during emit (after the initial scan), so drain the scan
        // queue now to pull in the transitive closure of the just-reached wrapper
        // members (e.g. Contains -> IndexOf -> EqualityComparer<T>); the emit fixpoint
        // then compiles them.
        DrainReachability();
        return (cls, ctor);
    }

    /// <summary>The resolved <c>Dn2Cpp.Runtime.Dn2CppConsoleWriter</c> class + its
    /// parameterless ctor, once <see cref="ConsoleErrorWriter"/> has reached them; null
    /// before any <c>Console.Error</c> use, and null forever in a CoreLib-less load set
    /// (see <see cref="ConsoleErrorWriter"/>). CppEmitter reads this to emit the cached
    /// singleton accessor, and the TextWriter fast-path checks reuse
    /// <see cref="ConsoleErrorWriterCppType"/>.</summary>
    internal (ClassInfo Cls, MethodInfo Ctor)? ConsoleErrorWriterInfo { get; private set; }

    /// <summary>Whether <see cref="ConsoleErrorWriter"/> has already answered. Separate
    /// from <see cref="ConsoleErrorWriterInfo"/> because null is a real answer, not only
    /// the not-yet-asked state: it memoizes the CoreLib-less verdict so a second
    /// <c>Console.Error</c> use re-resolves nothing.</summary>
    private bool _consoleErrorWriterResolved;

    /// <summary>The C++ receiver type (<c>t_…*</c>) a <c>Console.get_Error</c> result carries
    /// once the writer has been resolved, or null before then. The TextWriter Write/WriteLine
    /// intercept keys its fast path on this: a receiver of exactly this type is the
    /// non-spilled singleton (direct dn2cpp_textwriter_* write); anything else (a spilled
    /// base <c>System.IO.TextWriter*</c> local) falls through to real vtable dispatch.</summary>
    internal string? ConsoleErrorWriterCppType =>
        ConsoleErrorWriterInfo is { } i ? i.Cls.CppStructName + "*" : null;

    /// <summary>Resolves, completes, allocates and fully reaches the
    /// <c>Dn2Cpp.Runtime.Dn2CppConsoleWriter</c> singleton the transpiler returns from
    /// <c>Console.Error</c>. A real header-bearing <see cref="TextWriter"/> subtype:
    /// its vtable's core write slots (populated by the normal BuildVtables from the C#
    /// overrides, which route to the <c>ConsoleRuntime.Err*</c> intrinsics) let a spilled
    /// <c>callvirt</c> dispatch correctly instead of crashing on the header-less runtime
    /// writer struct. Mirrors <see cref="SZArrayEnumerableFor"/>: reaching during body
    /// compilation is fine — the emit fixpoint picks up the newly-reachable overrides. A
    /// missing/stale shim throws (see RequireShimType).
    ///
    /// <para><b>Answers null when the shim's own base did not resolve</b> — i.e. a
    /// transpile with no CoreLib in the load set, where <c>System.IO.TextWriter</c> is an
    /// External name rather than a loaded class. The subtype is not merely unnecessary
    /// there, it is inexpressible: with no base class there is no base vtable, so
    /// <c>RenderVtable</c> types the shim's own <c>get_Encoding</c> slot and dies on the
    /// External <c>System.Text.Encoding</c> return, and the ctor's chained
    /// <c>TextWriter::.ctor()</c> resolves to nothing. <c>Console.get_Error</c> falls back
    /// to the header-less runtime writer, which is exactly what that load set can express,
    /// and the fallback cannot crash a dispatch: every receiver shape that bypasses the
    /// receiver-keyed fast path — a Roslyn spill, a <c>TextWriter</c> parameter — needs a
    /// C++ type for the External base, and <see cref="CppTypes.Of"/> refuses that loudly at
    /// transpile time instead of emitting a callvirt through a missing header.</para></summary>
    internal (ClassInfo Cls, MethodInfo Ctor)? ConsoleErrorWriter()
    {
        if (_consoleErrorWriterResolved)
            return ConsoleErrorWriterInfo;
        const string Need = "returning a managed writer from Console.Error";
        var cands = RequireShimType("Dn2Cpp.Runtime", "Dn2CppConsoleWriter", Need);
        var (mod, tdh) = cands[0];
        if (!mod.ClassMap.TryGetValue(tdh, out var cls))
            throw new NotSupportedException(
                "Dn2Cpp.Runtime.Dn2CppConsoleWriter could not be resolved from the loaded "
                + "Dn2Cpp.Runtime.dll (stale or mismatched shim). It is needed for " + Need + ".");
        // No loaded base => no CoreLib in the load set (see the summary). Nothing is
        // reached, so there is no cut/route asymmetry to answer for: the writer's class,
        // ctor and overrides simply never enter the tree. The memo is set on the two
        // ANSWER paths only, never ahead of the throws above: --measure records a body's
        // NotSupportedException as a gap row and keeps draining, and a memo set before
        // the throw would turn the stale-shim diagnostic into a silent fallback for
        // every later Console.Error body.
        if (cls.BaseClass is null)
        {
            _consoleErrorWriterResolved = true;
            return null;
        }
        var ctor = ParameterlessCtor(cls)
            ?? throw new NotSupportedException(
                "Dn2Cpp.Runtime.Dn2CppConsoleWriter has no parameterless constructor: the loaded "
                + "Dn2Cpp.Runtime.dll is stale or does not match this dn2cpp. It is needed for "
                + Need + ".");
        NoteReferencedType(cls);
        ReachAllocatedType(cls);
        Reach(ctor);
        // Reaching happens during emit (after the initial scan); drain now so the
        // just-reached override bodies (WriteLine(string) -> ConsoleRuntime.ErrWriteLine,
        // …) and the base TextWriter ctor chain are pulled into the tree before the emit
        // fixpoint compiles them.
        DrainReachability();
        ConsoleErrorWriterInfo = (cls, ctor);
        _consoleErrorWriterResolved = true;
        return (cls, ctor);
    }

    /// <summary>If <paramref name="handle"/> is a <c>Task.WhenAll</c>/<c>WhenAny</c>
    /// call whose single parameter is an <c>IEnumerable&lt;Task&lt;T&gt;&gt;</c> (the
    /// non-array combinator overload — the emit lowers it to an inline interface-
    /// enumeration loop), returns the closed element task type whose enumeration must
    /// be reached; null otherwise. Handles both the generic (MethodSpec) and
    /// non-generic (MemberRef) forms.</summary>
    private TypeDesc? TaskCombinatorEnumerableElement(Module module, EntityHandle handle, GenericContext ctx)
    {
        // The name gates are pure metadata-string reads (MethodSpec/MemberRef parent
        // and method name); they must be checked BEFORE the signature decode below,
        // because that decode is not free — it MINTS the closed generics its parameter
        // and return types name (AGENTS.md: "a signature read is a decode, and a decode
        // grows Compilation.Classes"). This helper runs for EVERY call token the scan
        // sees, and the overwhelming majority are not Task.WhenAll/WhenAny; decoding
        // each one's signature only to reject it mints specializations nothing else
        // references — including, when the scanned body belongs to an open generic
        // definition (ctx binds a slot to its own !0), the OPEN definition of a callee's
        // declaring type (e.g. SettingValue<!0> from a `callvirt SettingValue<!0>::get_Value`),
        // which then reaches the struct emitter as a placeholder-typed layout and throws
        // on a bare `!0`. Gate first, decode only on a real match.
        string? declName;
        string methodName;
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            var msName = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
            declName = MethodSpecParentTypeName(module, msName);
            methodName = MethodSpecMethodName(module, msName);
        }
        else if (handle.Kind == HandleKind.MemberReference)
        {
            var mr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
            declName = MemberRefParentTypeName(module, (MemberReferenceHandle)handle);
            methodName = module.Reader.GetString(mr.Name);
        }
        else
            return null;

        if (declName != "System.Threading.Tasks.Task" || methodName is not ("WhenAll" or "WhenAny"))
            return null;

        MethodSignature<TypeDesc> sig;
        if (handle.Kind == HandleKind.MethodSpecification)
        {
            var ms = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
            var margs = ms.DecodeSignature(SigProvider, ctx);
            var mctx = new GenericContext(System.Array.Empty<TypeDesc>(), margs.ToArray());
            sig = ms.Method.Kind == HandleKind.MethodDefinition
                ? module.Reader.GetMethodDefinition((MethodDefinitionHandle)ms.Method).DecodeSignature(SigProvider, mctx)
                : module.Reader.GetMemberReference((MemberReferenceHandle)ms.Method).DecodeMethodSignature(SigProvider, mctx);
        }
        else
            sig = module.Reader.GetMemberReference((MemberReferenceHandle)handle).DecodeMethodSignature(SigProvider, ctx);

        if (sig.ParameterTypes is [{ Kind: TypeKind.Class, Class: { } col }]
            && GenericDefFullName(col) == "System.Collections.Generic.IEnumerable"
            && col.Context.TypeArgs.Length == 1)
            return col.Context.TypeArgs[0];
        return null;
    }

    /// <summary>The C++ type-info symbol of System.OperationCanceledException once
    /// <see cref="ReachCancellationException"/> has emitted it, else null.</summary>
    internal string? CanceledExceptionTypeInfoName { get; private set; }

    /// <summary>The C++ type-info symbol of System.Threading.Tasks.TaskCanceledException,
    /// reached alongside the OCE one — a CANCELED task carries this type, as in real
    /// .NET, while the token-side throw keeps the plain OCE. Null when the load set
    /// does not carry the type (the runtime then falls back to the OCE identity).</summary>
    internal string? TaskCanceledExceptionTypeInfoName { get; private set; }

    /// <summary>Force-reaches System.OperationCanceledException and
    /// System.Threading.Tasks.TaskCanceledException (type-info + base chain) when a
    /// program uses cancellation, so a CANCELED task / ThrowIfCancellationRequested can
    /// be registered to throw objects catchable by a typed
    /// <c>catch (OperationCanceledException)</c> even if the program itself only
    /// catches <c>Exception</c>.</summary>
    internal void ReachCancellationException()
    {
        if (CanceledExceptionTypeInfoName is not null)
            return;
        if (!TypeIndex().TryGetValue(("System", "OperationCanceledException"), out var cands))
            return;
        var (mod, tdh) = cands[0];
        if (!mod.ClassMap.TryGetValue(tdh, out var cls))
            return;
        NoteReferencedType(cls);
        ReachAllocatedType(cls);
        CanceledExceptionTypeInfoName = cls.CppTypeInfoName;
        // TCE derives from OCE, so reaching it here keeps `catch (OperationCanceled
        // Exception)` matching a canceled task through the emitted base chain.
        if (TypeIndex().TryGetValue(("System.Threading.Tasks", "TaskCanceledException"), out var tceCands))
        {
            var (tceMod, tceTdh) = tceCands[0];
            if (tceMod.ClassMap.TryGetValue(tceTdh, out var tce))
            {
                NoteReferencedType(tce);
                ReachAllocatedType(tce);
                TaskCanceledExceptionTypeInfoName = tce.CppTypeInfoName;
            }
        }
    }

    internal static MethodInfo? ParameterlessCtor(ClassInfo c) =>
        c.Methods.FirstOrDefault(m => !m.IsStatic && m.Name == ".ctor" && m.Signature.ParameterTypes.Length == 0);

    /// <summary>The parameterless instance ctor that is also <b>public</b>, or null.
    /// The generic factory <c>Activator.CreateInstance&lt;T&gt;()</c> (and <c>new T()</c>
    /// off it) reflects through <c>RuntimeType.CreateInstanceOfT</c>, which binds ONLY a
    /// public parameterless ctor: a type whose only parameterless ctor is non-public
    /// (<c>Task&lt;T&gt;</c>'s internal ctor, a type with a private ctor) is uninstantiable
    /// there and yields a run-time <c>MissingMethodException</c>. Distinct from
    /// <see cref="ParameterlessCtor"/>, which the <c>where T:new()</c> newobj paths use —
    /// their C# constraint already guarantees the match is public, so the visibility test
    /// is unneeded there and would only cost a decode.</summary>
    internal static MethodInfo? PublicParameterlessCtor(ClassInfo c) =>
        c.Methods.FirstOrDefault(m => !m.IsStatic && m.IsPublic && m.Name == ".ctor" && m.Signature.ParameterTypes.Length == 0);

    /// <summary>Reaches <paramref name="c"/>'s implementation of the virtual or
    /// interface slot declared by <paramref name="decl"/>, if it provides one.</summary>
    private void ReachVirtualImpl(ClassInfo c, MethodInfo decl)
    {
        EnsureCompleted(c);   // the override lives in its vtable / interface impls
        if (decl.DeclaringClass.IsInterface)
        {
            if (ImplementsInterface(c, decl.DeclaringClass)
                && ResolveItfImplOrNull(c, decl) is { } impl)
            {
                // An integer primitive's ISpanFormattable::TryFormat impl
                // is lowered inline (dn2cpp_try_format_int|uint_c — see MethodCompiler /
                // TryEmitValueConstrained), so don't reach the real body even when the byte/
                // int is boxed (an allocated value type) and TryFormat is a used virtual slot.
                // The real body pulls in the whole System.Number.TryFormat* subtree (the
                // dominant remaining self-host cascade, reached from SRM SignatureDecoder).
                // The SAME row ReachConstrainedImpl and TryEmitValueConstrained ask, so
                // this boxed-dispatch cut cannot drift from the constrained one.
                if (CoreIntrinsics.CvIntegerTryFormat.Matches(impl.DeclaringClass.FullName, impl.Name))
                    return;
                // A sub-word integer's Object::ToString /
                // IFormattable::ToString impl is likewise never needed via a *boxed* value —
                // dn2cpp_object_tostring formats a boxed Byte/SByte/Int16/UInt16 directly from
                // its type-info (the handwritten dn2cpp_*_type carry a nullptr tostring slot),
                // so the real Byte.ToString body is dead. The direct-call edge is cut
                // in ResolveCallTarget, but a boxed sub-word + the Object.ToString used-virtual
                // slot still reached it here, re-rooting the same Number-format subtree onto
                // SRM SignatureDecoder.CheckHeader. Cut it so that cascade fully collapses.
                // The same row the boxed-ToString mouths ask; the row's predicate gates on the
                // NAME itself before delegating to IsInlineLoweredPrimitiveMember, which
                // covers Parse/TryParse/TryFormat too and would over-cut this slot.
                if (CoreIntrinsics.CvBoxedSubWordToString.Matches(impl.DeclaringClass.FullName, impl.Name))
                    return;
                // An enum's ISpanFormattable::TryFormat impl (inherited from System.Enum),
                // reached as a used virtual slot on a BOXED enum (EnumConverter.ConvertTo ->
                // Enum.System.ISpanFormattable.TryFormat). The real body pulls in
                // Enum.TryFormatPrimitiveDefault -> GetEnumInfo -> GetEnumValuesAndNames (an
                // InternalCall with no IL), so it must not enter the tree. Both mouths are
                // routed: the constrained one (interpolation) by TryEmitValueConstrained, the
                // boxed one by the enum interface map's slot, which CppEmitter points at
                // dn2cpp_enum_box_try_format rather than at the dispatch trap.
                if (CoreIntrinsics.CvEnumTryFormat.Matches(impl.DeclaringClass.FullName, impl.Name))
                    return;
                Reach(impl);
                return;
            }
            // The exact interface missed. It may still be dispatched on this receiver
            // through generic variance, which the runtime resolves and this closure did
            // not — the one asymmetry that turns a tree-shake into a null slot.
            ReachVariantItfImpl(c, decl);
            // …or through the SZArray reference-element fallback rules, whose dispatch is
            // again more generous than exact + variant.
            ReachArrayWrapperItfImpl(c, decl);
            return;
        }
        // Class virtual: the override sits at decl's slot in c's (longer) vtable,
        // which preserves base slot order. Only meaningful when c derives from the
        // declaring type.
        if (DerivesFromOrIs(c, decl.DeclaringClass)
            && decl.VtableSlot >= 0 && decl.VtableSlot < c.Vtable.Count
            && c.Vtable[decl.VtableSlot] is { } over)
            Reach(over);
    }

    /// <summary>Reaches <paramref name="c"/>'s implementation of an interface slot it
    /// satisfies only through <b>generic variance</b>: a request for the covariant
    /// <c>ICovariant&lt;Animal&gt;</c> is served by an implemented
    /// <c>ICovariant&lt;Cat&gt;</c>, and a request for the contravariant
    /// <c>IContravariant&lt;Cat&gt;</c> by an implemented
    /// <c>IContravariant&lt;Animal&gt;</c>.
    ///
    /// <para>The mirror of the runtime's <c>dn2cpp_itf_variant_match</c>, and it has to
    /// be: the runtime resolves a variant dispatch against the receiver's real interface
    /// row, so an implementation this closure does not reach becomes a <em>null slot</em>
    /// the emitter fills in and the program jumps through. Reachability must be at least
    /// as generous as dispatch, never less. The per-argument test (<see cref="RefAssignable"/>)
    /// recurses when an argument is itself a variant instantiation
    /// (<c>ICovariant&lt;ICovariant&lt;Cat&gt;&gt;</c> to
    /// <c>ICovariant&lt;ICovariant&lt;Animal&gt;&gt;</c>), each nested level re-applying its
    /// own definition's mask — and the runtime side recurses identically, so the two accept
    /// exactly the same set at every depth.</para>
    ///
    /// <para>The correspondence is by <b>row index</b>, not signature: an interface
    /// method's <see cref="MethodInfo.VtableSlot"/> is its row within its own interface,
    /// and two instantiations of one generic definition have identical row order — which
    /// is the invariant the runtime's dispatch already rests on. A <c>SigKey</c> match
    /// cannot work here by construction: variance is precisely the case where the
    /// signatures differ (<c>Get():Cat</c> vs <c>Get():Animal</c>).</para>
    ///
    /// <para>Pre-filtered on the request being a closed generic whose definition declares
    /// an <c>in</c>/<c>out</c> parameter, so the used-virtual × allocated-type cross
    /// product this sits in is untouched for everything else.</para></summary>
    private void ReachVariantItfImpl(ClassInfo c, MethodInfo decl)
    {
        var want = decl.DeclaringClass;
        if (want.Context.TypeArgs.Length == 0)
            return;
        int mask = GenericVarianceMask(want);
        if (mask == 0)
            return;
        int row = decl.VtableSlot;
        if (row < 0)
            return;
        // Collect first, reach after: a Reach can complete classes and grow the model,
        // and the closure walk below is over the interface graph, not over Classes.
        List<MethodInfo>? impls = null;
        var stack = new Stack<ClassInfo>();
        PushDirectInterfaces(stack, c);
        var visited = new HashSet<ClassInfo>();
        while (stack.Count > 0)
        {
            var have = stack.Pop();
            if (!visited.Add(have))
                continue;
            PushDirectInterfaces(stack, have);
            if (have == want || !VariantMatches(have, want, mask))
                continue;
            have.EnsureMembers();   // the interface's declarations ARE its rows
            var rows = have.Methods;
            if (row >= rows.Count)
                continue;
            if (ResolveItfImplOrNull(c, rows[row]) is { } impl)
                (impls ??= new List<MethodInfo>()).Add(impl);
        }
        if (impls is null)
            return;
        foreach (var impl in impls)
            Reach(impl);
    }

    /// <summary>Reaches the <c>Dn2Cpp.Runtime.SZArrayEnumerable&lt;A&gt;</c> wrapper's
    /// implementation of an interface slot that only the runtime's reference-element
    /// SZArray rules can land on — the dispatch arms of
    /// <c>dn2cpp_resolve_interface_walk</c> that exact matching and generic variance
    /// (<see cref="ReachVariantItfImpl"/>) cannot see. Two arms, mirroring the runtime
    /// exactly; like the variant mirror, reachability must be at least as generous as
    /// dispatch, or the served row holds a slot nothing filled and the dispatch is a
    /// null-pointer call:
    /// <list type="bullet">
    /// <item>the <b>covariant-element</b> arm: a request for one of the five SZArray
    /// generic collection interfaces (<c>DN2CPP_TF_ARRAY_GEN_ITF</c>) at element E is
    /// served for an A[] whose element A is reference-covariant to E — the CLR's
    /// array-interface rule (<c>FieldInfo[]</c> as the invariant
    /// <c>ICollection&lt;MemberInfo&gt;</c>), which lands on the wrapper's thunks
    /// both through the fallback table and through the canonical alias rows a shared
    /// body probes (<c>ICollection&lt;__CnRef&gt;</c> on <c>ti_arr_FieldInfo</c>);</item>
    /// <item>the <b>ref-erased</b> arm (<c>DN2CPP_TF_REF_ERASED_ITF</c>, stamped on
    /// <c>SZArrayEnumerable&lt;object&gt;</c> only): any instantiation of an
    /// implemented generic-interface definition whose arguments are ALL reference
    /// types is answered by the object-keyed row — the shape the shared fallback
    /// table and the enumerator it hands back are dispatched through.</item>
    /// </list>
    /// The correspondence is by row index, exactly as in
    /// <see cref="ReachVariantItfImpl"/> (variant and erased requests are precisely
    /// where signatures differ). Value-type elements/arguments never match either arm
    /// — a value-element array is answered by its own eagerly-wired per-element map
    /// (the value loop in <see cref="ExpandArrayEnumerableMaps"/>), an exact row the
    /// plain reach closure already fills, so the fallback arms never serve it.</summary>
    private void ReachArrayWrapperItfImpl(ClassInfo c, MethodInfo decl)
    {
        if (GenericDefFullName(c) is not "Dn2Cpp.Runtime.SZArrayEnumerable"
            || c.Context.TypeArgs is not [{ } elem] || elem.IsCanonPlaceholder)
            return;
        var want = decl.DeclaringClass;
        var wa = want.Context.TypeArgs;
        if (wa.Length == 0)
            return;
        bool allRefArgs = true;
        foreach (var a in wa)
        {
            if (ContainsCanonPlaceholder(a))
                return; // canonical alias rows share the concrete rows' slot pools
            allRefArgs &= IsReferenceArg(a);
        }
        int row = decl.VtableSlot;
        if (row < 0)
            return;
        bool serves =
            // ref-erased arm: the object wrapper answers any all-reference-argument
            // instantiation of a definition it implements (the walk below is the
            // implemented-definition filter).
            (elem.IsObject && allRefArgs)
            // covariant-element arm: the five SZArray collection definitions, one
            // reference argument, element reference-covariant to it. RefAssignable is
            // the transpiler's mirror of the runtime's dn2cpp_array_elem_assignable —
            // the reference half of dn2cpp_array_elem_covariant (the fallback arm
            // rejects value elements before asking, so the primitive/enum equivalence
            // breadth never applies there).
            || (wa.Length == 1 && IsReferenceArg(elem) && IsReferenceArg(wa[0])
                && GenericDefFullName(want) is "System.Collections.Generic.IEnumerable"
                    or "System.Collections.Generic.ICollection"
                    or "System.Collections.Generic.IList"
                    or "System.Collections.Generic.IReadOnlyList"
                    or "System.Collections.Generic.IReadOnlyCollection"
                && RefAssignable(elem, wa[0]));
        if (!serves)
            return;
        // Collect first, reach after — same discipline as ReachVariantItfImpl.
        List<MethodInfo>? impls = null;
        var stack = new Stack<ClassInfo>();
        PushDirectInterfaces(stack, c);
        var visited = new HashSet<ClassInfo>();
        while (stack.Count > 0)
        {
            var have = stack.Pop();
            if (!visited.Add(have))
                continue;
            PushDirectInterfaces(stack, have);
            if (have == want
                || have.Module != want.Module || have.Handle != want.Handle
                || have.Context.TypeArgs.Length != wa.Length)
                continue;
            have.EnsureMembers();   // the interface's declarations ARE its rows
            var rows = have.Methods;
            if (row >= rows.Count)
                continue;
            if (ResolveItfImplOrNull(c, rows[row]) is { } impl)
                (impls ??= new List<MethodInfo>()).Add(impl);
        }
        if (impls is null)
            return;
        foreach (var impl in impls)
            Reach(impl);
    }

    /// <summary>Whether an implemented interface <paramref name="have"/> satisfies a
    /// request for <paramref name="want"/> under <paramref name="want"/>'s definition's
    /// per-parameter variance (see <see cref="GenericVarianceMask"/>): same definition,
    /// same arity, and every argument assignable in the direction its parameter declares
    /// (<c>out</c> forward, <c>in</c> backward, invariant exactly).</summary>
    private bool VariantMatches(ClassInfo have, ClassInfo want, int mask)
    {
        if (have.Module != want.Module || have.Handle != want.Handle)
            return false;
        var ha = have.Context.TypeArgs;
        var wa = want.Context.TypeArgs;
        if (ha.Length == 0 || ha.Length != wa.Length || ha.Length > MaxVarianceParams)
            return false;
        for (int i = 0; i < ha.Length; i++)
        {
            bool ok = ((mask >> (2 * i)) & 3) switch
            {
                VarianceCovariant => RefAssignable(ha[i], wa[i]),
                VarianceContravariant => RefAssignable(wa[i], ha[i]),
                _ => SameTypeArg(ha[i], wa[i]),
            };
            if (!ok)
                return false;
        }
        return true;
    }

    /// <summary>CLR reference-assignability between two closed type arguments — what the
    /// runtime's <c>dn2cpp_array_elem_assignable</c> decides. A value-type (or canonical
    /// placeholder) argument is invariant: an exact match only, which is also what the CLR
    /// says — variance applies to reference types alone.
    ///
    /// <para>Nested variance: when <paramref name="to"/> is itself a variant interface
    /// instantiation, an argument may be assignable to it by variance rather than by exact
    /// identity — either because it IS an instantiation of the same variant definition whose
    /// arguments sit the right way round (<c>ICovariant&lt;Cat&gt;</c> to
    /// <c>ICovariant&lt;Animal&gt;</c>), or because it IMPLEMENTS one
    /// (<c>Box&lt;Cat&gt;</c> to <c>ICovariant&lt;Animal&gt;</c>). This method recurses into
    /// <see cref="VariantMatches"/> for that, closing the mutual recursion — depth bounded by
    /// the finite type-argument nesting. It mirrors the runtime's variant tail on
    /// <c>dn2cpp_array_elem_assignable</c> exactly; the two must accept the same set, or a
    /// runtime match the closure did not reach lands the slot on the trap stub.</para></summary>
    private bool RefAssignable(TypeDesc from, TypeDesc to)
    {
        if (SameTypeArg(from, to))
            return true;
        if (!IsReferenceArg(from) || !IsReferenceArg(to))
            return false;
        if (to.IsObject)
            return true;
        if (from is not { Kind: TypeKind.Class, Class: { } fc }
            || to is not { Kind: TypeKind.Class, Class: { } tc })
            return false;
        if (DerivesFromOrIs(fc, tc) || ImplementsInterface(fc, tc))
            return true;
        // `to` variant? Then `from` may match it by variance. Gate on the mask (the same
        // pre-filter dn2cpp_itf_is_variant is on the runtime side) so exact-match arguments
        // pay nothing. Two shapes, mirroring the runtime walk: `from` is itself an
        // instantiation of `to`'s definition, or it implements one somewhere in its
        // interface closure.
        int vmask = GenericVarianceMask(tc);
        if (vmask == 0)
            return false;
        if (VariantMatches(fc, tc, vmask))
            return true;
        var stack = new Stack<ClassInfo>();
        PushDirectInterfaces(stack, fc);
        var visited = new HashSet<ClassInfo>();
        while (stack.Count > 0)
        {
            var itf = stack.Pop();
            if (!visited.Add(itf))
                continue;
            PushDirectInterfaces(stack, itf);
            if (VariantMatches(itf, tc, vmask))
                return true;
        }
        return false;
    }

    /// <summary>Exact type-argument identity, decided by the same mangle the
    /// instantiation cache keys on — two arguments that mangle alike ARE one type.
    /// Routed through the cache's own element comparer so the two answers
    /// cannot drift, and no mangle strings are built per ask.</summary>
    private static bool SameTypeArg(TypeDesc a, TypeDesc b) =>
        TypeArgsComparer.ElementEquals(a, b);

    /// <summary>Whether a closed type argument is a reference type, so variance may move
    /// it. A canonical-generics placeholder is deliberately NOT one: it stands for a whole
    /// layout group, and letting the probe cross between the canonical and the real world
    /// would reach bodies neither of them dispatches.</summary>
    private static bool IsReferenceArg(TypeDesc t) => !t.IsCanonPlaceholder && t.Kind switch
    {
        TypeKind.Class => t.Class is { IsValueType: false, IsEnum: false },
        TypeKind.SZArray or TypeKind.MDArray or TypeKind.External => true,
        TypeKind.Primitive => t.Primitive is PrimitiveTypeCode.Object or PrimitiveTypeCode.String,
        _ => false,
    };

    internal const int VarianceCovariant = 1;      // `out T`
    internal const int VarianceContravariant = 2;  // `in T`
    /// <summary>Type parameters the 2-bits-per-parameter variance mask can carry
    /// (int32 / 2 bits). Nothing in the BCL comes close; a wider definition simply
    /// reports "no variance" and keeps the exact-match behavior.</summary>
    internal const int MaxVarianceParams = 16;

    private readonly Dictionary<(int Module, int Token), int> _varianceMasks = new();

    /// <summary>The generic definition's per-parameter variance, 2 bits per parameter
    /// (parameter <c>i</c> at bits <c>2i</c>): 0 invariant, 1 <c>out</c>, 2 <c>in</c>.
    /// 0 for a definition with no variant parameter — which is the pre-filter every caller
    /// leans on, so it is cached per definition rather than re-read from metadata.
    /// The same encoding the emitter stamps on the definition's type-info for the
    /// runtime.</summary>
    internal int GenericVarianceMask(ClassInfo cls)
    {
        if (cls.GenericArity == 0 || cls.Context.TypeArgs.Length == 0)
            return 0;
        var key = (cls.Module.Index, SRME.GetToken(cls.Handle));
        if (_varianceMasks.TryGetValue(key, out var cached))
            return cached;
        int mask = 0;
        try
        {
            var reader = cls.Module.Reader;
            foreach (var gph in reader.GetTypeDefinition(cls.Handle).GetGenericParameters())
            {
                var gp = reader.GetGenericParameter(gph);
                if (gp.Index >= MaxVarianceParams)
                    continue;
                int v = (gp.Attributes & GenericParameterAttributes.VarianceMask) switch
                {
                    GenericParameterAttributes.Covariant => VarianceCovariant,
                    GenericParameterAttributes.Contravariant => VarianceContravariant,
                    _ => 0,
                };
                mask |= v << (2 * gp.Index);
            }
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            mask = 0;   // unreadable metadata -> no variance, exact matching only
        }
        _varianceMasks[key] = mask;
        return mask;
    }

    private static bool DerivesFromOrIs(ClassInfo c, ClassInfo baseCls)
    {
        for (var b = c; b is not null; b = b.BaseClass)
            if (b == baseCls)
                return true;
        return false;
    }

    private static bool ImplementsInterface(ClassInfo c, ClassInfo itf)
    {
        // Iterative closure walk with a visited set: the interface graph is a DAG
        // (IList<T> -> ICollection<T> -> IEnumerable<T> <- IReadOnlyCollection<T>, …),
        // so a naive recursion re-walks shared parents once per inbound path —
        // exponential for diamond-rich BCL hierarchies. Each interface node is
        // expanded at most once here, making the check linear in the closure size.
        //
        // The closure is cached per entry class: this is asked per
        // (allocated type × used interface) pair by the GVM/dispatch reachers, and
        // an uncached ask re-walks the DAG and allocates a Stack+HashSet. The walk is
        // pure shape reads (Interfaces/BaseClass — never a member pull), so the
        // full closure costs what one miss cost; membership probes are O(1) after.
        // Cached ONLY when every node the walk read was ShapeReady: a
        // specialization minted mid-scan has empty Interfaces (and no BaseClass)
        // until its CompleteShape turn, and freezing that immature answer would
        // change a later ask that the uncached walk answered from the live lists.
        if (c.InterfaceClosureCache is { } cached)
            return cached.Contains(itf);
        var stack = new Stack<ClassInfo>();
        bool ready = PushDirectInterfaces(stack, c);
        var closure = new HashSet<ClassInfo>();
        while (stack.Count > 0)
        {
            var i = stack.Pop();
            if (closure.Add(i))
                ready &= PushDirectInterfaces(stack, i);
        }
        if (ready)
            c.InterfaceClosureCache = closure;
        return closure.Contains(itf);
    }

    /// <summary>Pushes every interface directly implemented by <paramref name="x"/>
    /// or any class in its base chain onto <paramref name="stack"/>. Returns true
    /// when every base-chain node read was <see cref="ClassInfo.ShapeReady"/> — the
    /// closure-cache validity bit (a not-yet-completed specialization's interface
    /// list and base link are still to come, so a closure read through one is
    /// provisional and must not be stored).</summary>
    private static bool PushDirectInterfaces(Stack<ClassInfo> stack, ClassInfo x)
    {
        bool ready = true;
        for (var b = x; b is not null; b = b.BaseClass)
        {
            ready &= b.ShapeReady;
            foreach (var i in b.Interfaces)
                stack.Push(i);
        }
        return ready;
    }

    /// <summary>Resolves <paramref name="c"/>'s body for an interface method
    /// (explicit impl first, then signature match), or null if it has none.</summary>
    internal static MethodInfo? ResolveItfImplOrNull(ClassInfo c, MethodInfo itfMethod)
    {
        for (var b = c; b is not null; b = b.BaseClass)
        {
            b.EnsureMembers();
            // Exact-handle hit is the common case — one O(1) dictionary probe
            // instead of a linear key scan + re-index.
            if (b.ExplicitInterfaceImpls.TryGetValue(itfMethod, out var direct))
                return direct;
            foreach (var kv in b.ExplicitInterfaceImpls)
                if (kv.Key.DeclaringClass.FullName == itfMethod.DeclaringClass.FullName
                    && kv.Key.Name == itfMethod.Name
                    && kv.Key.SigKey == itfMethod.SigKey)
                    return kv.Value;
        }
        for (var b = c; b is not null; b = b.BaseClass)
        {
            // Name-indexed: same candidates in the same order as the full
            // Methods scan — the index filters on Name (a plain field) exactly as the
            // predicate's short-circuit did, so SigKey (a decode on first read) is
            // still consulted only for same-name candidates.
            if (b.MethodsNamed(itfMethod.Name) is { } named)
                foreach (var m in named)
                    if (!m.IsStatic && m.Rva != 0 && m.SigKey == itfMethod.SigKey)
                        return m;
        }
        // No class in the hierarchy provides an implementation. If the interface
        // method itself is a *default interface method* — a concrete (non-abstract)
        // instance body declared on the interface — the CLR binds the dispatch to
        // that default. Return it so the implementing type's interface slot points at
        // the default body rather than a null one, which would fault on dispatch (a
        // class that inherits an interface's default without overriding it, e.g.
        // ConsoleBackend inheriting IEmitBackend.ExternallyAllocatedClasses's default).
        // A truly abstract slot (Rva == 0) has no default and stays null.
        if (!itfMethod.IsStatic && !itfMethod.IsAbstract && itfMethod.Rva != 0)
            return itfMethod;
        return null;
    }

    // ---- third-party custom async task types ----
    //
    // A library in the GDTask / UniTask mould declares its own task-like type
    // ([AsyncMethodBuilder(typeof(AsyncGDTaskMethodBuilder))] readonly struct GDTask)
    // and its own compiler builder. Transpiling that IL like any other is fatal twice
    // over:
    //
    //   * It cannot work. The builder's Start<TSM> is a `constrained. !!TSM callvirt
    //     IAsyncStateMachine::MoveNext` with no lowering, and its AwaitUnsafeOnCompleted
    //     closes the library's whole promise/pool machinery over every compiler-generated
    //     state machine in the program.
    //   * Termination is owed to on-demand member decode (CompleteMembers). GDTask<T>
    //     declares `GDTask<(bool IsCanceled, T Result)> SuppressCancellationThrow()` —
    //     the self-referential-signature shape — so eagerly decoding every method
    //     signature at completion would instantiate GDTask<(bool,X)>, then
    //     GDTask<(bool,(bool,X))>, without end, from `-r GDTask.dll` alone, no call
    //     required. On demand, an uncalled method's signature is never decoded, so
    //     the deeper GDTask never exists — for every generic, adopted or not.
    //
    // Adoption is still what makes the FIRST bullet go away, and it is still worth its
    // keep: the task type, its builder and its awaiter become IntrinsicCppName-mapped, so
    // CompleteShape leaves them empty shells — exactly as it already does for the BCL's
    // Task family, and for exactly the reason its comment there gives — and their members
    // are served from the intrinsic tables rather than their IL.
    //
    // The mapping follows the BCL member names and exact signature shapes required by
    // the async method and await patterns. An adopted type therefore answers to a BCL
    // dispatch key and every existing Task/ValueTask intrinsic case fires unchanged.
    // A member OUTSIDE that contract (GDTask.DelayFrame, .Forget(), .Status) has no BCL
    // counterpart, so a program that reaches one cannot run adopted at all. A MemberRef
    // pre-scan therefore
    // DECLINES the adoption of any assembly whose task/builder/awaiter is referenced
    // cross-assembly outside the mapped contract, and its real IL transpiles through the
    // general pipeline instead — the same route --no-adopt-async takes by hand. The scan
    // cannot see a same-assembly call (a MethodDef has no MemberRef row), so that one
    // still fails loud at emit naming the adoption and the manual opt-out; it is never
    // silently miscompiled.
    //
    // Discovery is best-effort and never throws: a shape that cannot be mapped is simply
    // not adopted and transpiles through the general pipeline. Throwing here would turn `-r` of any
    // assembly carrying an unmappable task-like type into a hard error even for a program
    // that never touches it.

    /// <summary>Registers every <c>[AsyncMethodBuilder]</c>-attributed task-like type in
    /// the loaded modules — with its builder and awaiter — in <see cref="_adoptedAsync"/>,
    /// then stamps the non-generic forms. Every ClassInfo that exists at this point is
    /// non-generic (Pass 1 holds the templates back) and no specialization has been
    /// created yet, so this loop plus <see cref="Instantiate"/>'s stamp cover every form.</summary>
    private void AdoptCustomAsyncTaskTypes()
    {
        // Validate the opt-out list BEFORE using it: a typo that silently fell back
        // to adoption would surface much later as the maximally confusing
        // "<Task type>::<member> has no intrinsic mapping".
        foreach (string name in _noAdoptAsync)
            if (!Modules.Any(m => string.Equals(m.AssemblyName, name, StringComparison.OrdinalIgnoreCase)))
                throw new NotSupportedException(
                    $"--no-adopt-async {name}: no loaded assembly has that simple name "
                    + $"(loaded: {string.Join(", ", Modules.Select(m => m.AssemblyName))})");
        var candidates = new List<AsyncAdoptionCandidate>();
        foreach (var module in Modules)
        {
            // Opted out: the module's task types stay un-adopted and their real IL
            // transpiles. Skipping the fill is the whole opt-out — see _noAdoptAsync.
            if (_noAdoptAsync.Any(n => string.Equals(module.AssemblyName, n, StringComparison.OrdinalIgnoreCase)))
                continue;
            var reader = module.Reader;
            foreach (var tdh in reader.TypeDefinitions)
            {
                var td = reader.GetTypeDefinition(tdh);
                if (AsyncMethodBuilderArg(reader, td) is { } builderName
                    && CollectAsyncTaskCandidate(module, tdh, td, builderName) is { } cand)
                    candidates.Add(cand);
            }
        }
        if (candidates.Count == 0)
            return;
        var declined = DeclineOutOfContractAdoptions(candidates);
        foreach (var cand in candidates)
        {
            if (declined.Contains(cand.AssemblyName))
                continue;
            foreach (var row in cand.Rows)
                _adoptedAsync.TryAdd((row.Module.Index, row.Handle), row.Mapping);
        }
        if (_adoptedAsync.Count == 0)
            return;
        foreach (var cls in Classes)
            if (cls.IntrinsicCppName is null && AdoptedAsyncCpp(cls.Module, cls.Handle) is { } icn)
            {
                cls.IntrinsicCppName = icn;
                // Same rule as the intrinsic by-value case in Pass 1: a mapping with no
                // trailing '*' is a struct.
                if (!icn.EndsWith('*'))
                    cls.IsValueType = true;
            }
    }

    /// <summary>The task-like role, the builder role and the awaiter role of one would-be
    /// adoption — each with its own member contract in the pre-scan.</summary>
    private enum AdoptedRole { TaskType, Builder, Awaiter }

    /// <summary>One would-be adoption: the up-to-three rows the commit loop fills
    /// <see cref="_adoptedAsync"/> with unless the contract pre-scan declines the task
    /// type's declaring assembly.</summary>
    private sealed class AsyncAdoptionCandidate
    {
        // Decline granularity is the ASSEMBLY, matching --no-adopt-async, and for the same
        // reason: sibling task types' promises interlock, so half-adopting one is unsound.
        public required string AssemblyName;
        public required bool IsStruct;
        public required int TaskArity;
        public required string TaskSignature;
        public required string BuilderSignature;
        public string? AwaiterSignature;
        public required List<(Module Module, TypeDefinitionHandle Handle,
            (string Cpp, string Key) Mapping, AdoptedRole Role)> Rows;
    }

    private static string OpenTypeSignature(MetadataReader reader, TypeDefinitionHandle handle)
    {
        string name = RawSignatureProvider.TypeDefinitionName(reader, handle);
        int arity = reader.GetTypeDefinition(handle).GetGenericParameters().Count;
        if (arity == 0)
            return name;
        string result = name + "<";
        for (int i = 0; i < arity; i++)
        {
            if (i != 0)
                result += ",";
            result += $"!{i}";
        }
        return result + ">";
    }

    /// <summary>Collects one task-like type, its builder and its awaiter as an adoption
    /// candidate, or nothing if any of the three does not fit the model. The commit loop
    /// TryAdds the rows in this order, so a type claimed in one role keeps it (a
    /// fire-and-forget task type — UniTaskVoid's shape — whose GetAwaiter returns itself
    /// stays the TASK, and awaiting it then fails loud, which is what real .NET does
    /// too).</summary>
    private AsyncAdoptionCandidate? CollectAsyncTaskCandidate(
        Module module, TypeDefinitionHandle tdh, TypeDefinition td, string builderName)
    {
        var reader = module.Reader;
        string taskName = DefFullName(reader, td);

        // The BCL's own task-like types (Task<T> and ValueTask/ValueTask<T> all carry a
        // type-level [AsyncMethodBuilder]) are already modeled by the static maps.
        if (CoreIntrinsics.IntrinsicGenericCppType(taskName) is not null
            || CoreIntrinsics.IsIntrinsicType(taskName))
            return null;
        // Arity 0 (async GDTask) or 1 (async GDTask<T>) — the only shapes the Task-family
        // runtime structs model.
        if (td.GetGenericParameters().Count > 1)
            return null;

        if (ResolveSerializedTypeName(builderName) is not { } b)
            return null; // the builder's assembly is not loaded: nothing to adopt it to
        var btd = b.Module.Reader.GetTypeDefinition(b.Handle);
        // The builder is embedded BY VALUE in the state machine (Dn2CppAsyncBuilder), and
        // it must carry the compiler-required shape or the intrinsic cases have nothing to
        // answer. (Start / AwaitOnCompleted / AwaitUnsafeOnCompleted are generic and route
        // through TranslateAsyncGenericIntrinsic, which never consults the dispatch key.)
        if (!IsValueTypeDef(b.Module.Reader, btd))
            return null;
        foreach (string required in s_builderShape)
            if (!HasMethodNamed(b.Module.Reader, btd, required))
                return null;

        // A struct task type is modeled on ValueTask, a reference one on Task. The kind
        // drives all three keys TOGETHER: get_Task's C++ shape differs between the two
        // builder switches (a Dn2CppTask* by reference vs a Dn2CppTaskAwaiter by value),
        // so a mismatched pair would not even compile. The struct arm is also the
        // semantically right one — a zeroed `default(GDTask)` is a null task pointer, and
        // dn2cpp_vtask normalizes null to a pre-completed sentinel, which is exactly what
        // `default(UniTask)` means; the Task arm's raw ->task->status read would fault.
        bool isStruct = IsValueTypeDef(reader, td);
        var rows = new List<(Module, TypeDefinitionHandle, (string, string), AdoptedRole)>(3)
        {
            (module, tdh, isStruct
                ? ("Dn2CppTaskAwaiter", "System.Threading.Tasks.ValueTask")
                : ("Dn2CppTask*", "System.Threading.Tasks.Task"), AdoptedRole.TaskType),
            (b.Module, b.Handle, ("Dn2CppAsyncBuilder", isStruct
                ? "System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder"
                : "System.Runtime.CompilerServices.AsyncTaskMethodBuilder"), AdoptedRole.Builder),
        };
        var candidate = new AsyncAdoptionCandidate
        {
            AssemblyName = module.AssemblyName,
            IsStruct = isStruct,
            TaskArity = td.GetGenericParameters().Count,
            TaskSignature = OpenTypeSignature(reader, tdh),
            BuilderSignature = OpenTypeSignature(b.Module.Reader, b.Handle),
            Rows = rows,
        };

        // The awaiter, discovered structurally: what GetAwaiter() returns. Optional — a
        // fire-and-forget task type has none — and skipped when it is an interface, which
        // the concrete Dn2CppTaskAwaiter value struct cannot back.
        if (AwaiterOf(module, td) is not { } a)
            return candidate;
        var atd = a.Module.Reader.GetTypeDefinition(a.Handle);
        if ((atd.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
            return candidate;
        candidate.AwaiterSignature = OpenTypeSignature(a.Module.Reader, a.Handle);
        rows.Add((a.Module, a.Handle, ("Dn2CppTaskAwaiter", isStruct
            ? "System.Runtime.CompilerServices.ValueTaskAwaiter"
            : "System.Runtime.CompilerServices.TaskAwaiter"), AdoptedRole.Awaiter));
        return candidate;
    }

    /// <summary>The members the C# compiler requires of an async method builder, and which
    /// the Task-family intrinsic switches answer by the same names. Start /
    /// AwaitOnCompleted / AwaitUnsafeOnCompleted are generic and handled separately.</summary>
    private static readonly string[] s_builderShape =
        { "Create", "Start", "SetStateMachine", "SetResult", "SetException", "get_Task" };

    /// <summary>The one task-type FIELD an adopted type models: the pre-completed constant
    /// (MethodCompiler.TryIntrinsicStaticField). Builders and awaiters model no field.</summary>
    private const string AdoptedTaskFieldContract = "CompletedTask";

    /// <summary>A raw metadata signature shape. Decoding through
    /// <see cref="RawSignatureProvider"/> never resolves a <see cref="TypeDesc"/> or grows
    /// <see cref="Classes"/>.</summary>
    private readonly record struct AsyncMemberShape(bool IsField, bool IsInstance,
        int GenericArity, string ReturnType, ImmutableArray<string> Parameters);

    private static bool IsAdoptedMember(
        AsyncAdoptionCandidate candidate, AdoptedRole role, string name, AsyncMemberShape s)
    {
        if (s.IsField)
            return role == AdoptedRole.TaskType && name == AdoptedTaskFieldContract
                && !s.IsInstance && s.ReturnType == candidate.TaskSignature;
        bool Method(bool instance, int arity, int parameters) =>
            s.IsInstance == instance && s.GenericArity == arity && s.Parameters.Length == parameters;
        bool Returns(string type) => s.ReturnType == type;
        bool Parameter(int index, string type) => s.Parameters[index] == type;
        string configured = candidate.IsStruct
            ? "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable"
            : "System.Runtime.CompilerServices.ConfiguredTaskAwaitable";
        if (candidate.TaskArity != 0)
            configured += "`1<!0>";
        string task = candidate.TaskArity == 0
            ? "System.Threading.Tasks.Task"
            : "System.Threading.Tasks.Task`1<!0>";

        return role switch
        {
            AdoptedRole.TaskType => name switch
            {
                "GetAwaiter" => Method(true, 0, 0)
                    && candidate.AwaiterSignature is { } awaiter && Returns(awaiter),
                "get_Result" => candidate.TaskArity == 1 && Method(true, 0, 0) && Returns("!0"),
                "AsTask" => Method(true, 0, 0) && Returns(task),
                "ConfigureAwait" => Method(true, 0, 1) && Parameter(0, "System.Boolean")
                    && Returns(configured),
                "get_IsCompleted" or "get_IsCompletedSuccessfully" =>
                    Method(true, 0, 0) && Returns("System.Boolean"),
                "get_CompletedTask" => Method(false, 0, 0) && Returns(candidate.TaskSignature),
                "FromCanceled" => Method(false, 0, 1)
                    && Parameter(0, "System.Threading.CancellationToken")
                    && Returns(candidate.TaskSignature),
                "FromException" => Method(false, 0, 1)
                    && Parameter(0, "System.Exception") && Returns(candidate.TaskSignature),
                _ => false,
            },
            AdoptedRole.Builder => name switch
            {
                "Create" => Method(false, 0, 0) && Returns(candidate.BuilderSignature),
                "Start" => Method(true, 1, 1) && Parameter(0, "!!0&")
                    && Returns("System.Void"),
                "SetStateMachine" => Method(true, 0, 1)
                    && Parameter(0, "System.Runtime.CompilerServices.IAsyncStateMachine")
                    && Returns("System.Void"),
                "SetResult" => Returns("System.Void") && (candidate.TaskArity == 0
                    ? Method(true, 0, 0)
                    : Method(true, 0, 1) && Parameter(0, "!0")),
                "SetException" => Method(true, 0, 1) && Parameter(0, "System.Exception")
                    && Returns("System.Void"),
                "get_Task" => Method(true, 0, 0) && Returns(candidate.TaskSignature),
                "AwaitOnCompleted" or "AwaitUnsafeOnCompleted" => Method(true, 2, 2)
                    && Parameter(0, "!!0&") && Parameter(1, "!!1&")
                    && Returns("System.Void"),
                _ => false,
            },
            _ => name switch
            {
                "GetAwaiter" => Method(true, 0, 0)
                    && candidate.AwaiterSignature is { } awaiter && Returns(awaiter),
                "GetResult" => Method(true, 0, 0)
                    && Returns(candidate.TaskArity == 0 ? "System.Void" : "!0"),
                "get_IsCompleted" => Method(true, 0, 0) && Returns("System.Boolean"),
                "OnCompleted" or "UnsafeOnCompleted" => Method(true, 0, 1)
                    && Parameter(0, "System.Action") && Returns("System.Void"),
                _ => false,
            },
        };
    }

    private static AsyncMemberShape? ReadAsyncMemberShape(MetadataReader reader, MemberReference mr,
        Module targetModule, TypeDefinitionHandle targetHandle)
    {
        try
        {
            var header = reader.GetBlobReader(mr.Signature).ReadSignatureHeader();
            if (header.Kind == SignatureKind.Field)
            {
                string fieldType = mr.DecodeFieldSignature(RawSignatureProvider.Instance, null);
                var targetReader = targetModule.Reader;
                string member = reader.GetString(mr.Name);
                foreach (var fieldHandle in targetReader.GetTypeDefinition(targetHandle).GetFields())
                {
                    var field = targetReader.GetFieldDefinition(fieldHandle);
                    if (targetReader.GetString(field.Name) != member
                        || field.DecodeSignature(RawSignatureProvider.Instance, null) != fieldType)
                        continue;
                    bool isInstance = (field.Attributes & FieldAttributes.Static) == 0;
                    return new AsyncMemberShape(true, isInstance, 0, fieldType, []);
                }
                return null;
            }
            if (header.Kind != SignatureKind.Method)
                return null;
            var signature = mr.DecodeMethodSignature(RawSignatureProvider.Instance, null);
            return new AsyncMemberShape(false, header.IsInstance,
                signature.GenericParameterCount, signature.ReturnType, signature.ParameterTypes);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    /// <summary>The adoption pre-scan: walks every module's MemberReference table for a
    /// cross-assembly reference to a candidate's task/builder/awaiter outside that role's
    /// contract, and returns the assembly simple names to decline (their real IL then
    /// transpiles, exactly as --no-adopt-async would arrange). Signature shapes decode
    /// through raw metadata only and cannot grow <see cref="Classes"/> in Pass 1.5.
    /// Over-detection (a dead MemberRef declining a
    /// live adoption) is safe: declined IL is merely transpiled for real. A same-module
    /// reference is skipped — same-assembly out-of-contract calls are MethodDefs or
    /// same-module MemberRefs and stay the emitter's fail-loud hole.</summary>
    private HashSet<string> DeclineOutOfContractAdoptions(List<AsyncAdoptionCandidate> candidates)
    {
        // The role/mapping still follows first-claim wins. Ownership does not: one shared
        // builder or awaiter makes every task assembly depend on the same contract.
        var byHandle = new Dictionary<(int, TypeDefinitionHandle),
            (AdoptedRole Role, List<AsyncAdoptionCandidate> Candidates)>();
        foreach (var cand in candidates)
            foreach (var row in cand.Rows)
            {
                var key = (row.Module.Index, row.Handle);
                if (!byHandle.TryGetValue(key, out var target))
                {
                    target = (row.Role, new List<AsyncAdoptionCandidate>());
                    byHandle.Add(key, target);
                }
                if (!target.Candidates.Contains(cand))
                    target.Candidates.Add(cand);
            }
        // Candidate (namespace, name) pairs: the allocation-free prefilter a TypeReference
        // parent must pass before the (allocating) full resolve is paid for.
        var names = new List<(string Ns, string Name)>();
        foreach (var cand in candidates)
            foreach (var row in cand.Rows)
            {
                var td = row.Module.Reader.GetTypeDefinition(row.Handle);
                var key = (row.Module.Reader.GetString(td.Namespace), row.Module.Reader.GetString(td.Name));
                if (!names.Contains(key))
                    names.Add(key);
            }

        var declined = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in Modules)
        {
            var reader = module.Reader;
            foreach (var mrh in reader.MemberReferences)
            {
                var mr = reader.GetMemberReference(mrh);
                EntityHandle parent = mr.Parent;
                // A generic candidate is referenced through a TypeSpec (GENERICINST
                // (CLASS|VALUETYPE) TypeDefOrRefOrSpec ...): peel it to the open template
                // handle the registry keys on, exactly as AwaiterOf reads a return blob.
                if (parent.Kind == HandleKind.TypeSpecification)
                {
                    try
                    {
                        var blob = reader.GetBlobReader(
                            reader.GetTypeSpecification((TypeSpecificationHandle)parent).Signature);
                        var tc = blob.ReadSignatureTypeCode();
                        if (tc == SignatureTypeCode.GenericTypeInstance)
                            tc = blob.ReadSignatureTypeCode();
                        if (tc != SignatureTypeCode.TypeHandle)
                            continue;
                        parent = blob.ReadTypeHandle();
                    }
                    catch (BadImageFormatException)
                    {
                        continue; // an unreadable TypeSpec references no candidate
                    }
                }
                // A TypeDefinition parent (bare or inside the TypeSpec) is a same-module
                // reference; every other kind (MethodDef vararg site, ModuleRef) is not a
                // type member reference at all.
                if (parent.Kind != HandleKind.TypeReference)
                    continue;
                var tr = reader.GetTypeReference((TypeReferenceHandle)parent);
                bool maybe = false;
                foreach (var (ns, nm) in names)
                    if (reader.StringComparer.Equals(tr.Name, nm)
                        && reader.StringComparer.Equals(tr.Namespace, ns))
                    {
                        maybe = true;
                        break;
                    }
                if (!maybe)
                    continue;
                var (m, h) = TemplateOrClassDef(ResolveTypeRef(module, (TypeReferenceHandle)parent));
                if (m is null || m.Index == module.Index
                    || !byHandle.TryGetValue((m.Index, h), out var target))
                    continue;
                string member = reader.GetString(mr.Name);
                var shape = ReadAsyncMemberShape(reader, mr, m, h);
                bool inContract = shape is not null;
                if (shape is { } memberShape)
                    foreach (var candidate in target.Candidates)
                        if (!IsAdoptedMember(candidate, target.Role, member, memberShape))
                        {
                            inContract = false;
                            break;
                        }
                if (inContract)
                    continue;
                // stderr only: the notice must not perturb the generated bytes.
                foreach (var candidate in target.Candidates)
                {
                    string assemblyName = candidate.AssemblyName;
                    if (declined.Add(assemblyName))
                        Console.Error.WriteLine(
                            $"dn2cpp: adoption declined: {DefFullName(m.Reader, m.Reader.GetTypeDefinition(h))}"
                            + $"::{member} is outside the mapped contract; transpiling "
                            + $"{assemblyName}'s real IL");
                }
            }
        }
        return declined;
    }

    /// <summary>The C++ runtime struct an adopted async task-family type lowers to, or
    /// null. Probed by <see cref="Instantiate"/> for the closed forms.</summary>
    internal string? AdoptedAsyncCpp(Module module, TypeDefinitionHandle handle) =>
        _adoptedAsync.TryGetValue((module.Index, handle), out var v) ? v.Cpp : null;

    /// <summary>The BCL intrinsic dispatch key an adopted async task-family type's members
    /// answer to (its task/builder/awaiter member names are the BCL's), or null.</summary>
    public string? AdoptedAsyncKey(ClassInfo cls) =>
        _adoptedAsync.TryGetValue((cls.Module.Index, cls.Handle), out var v) ? v.Key : null;

    /// <summary>Whether this compilation adopted any custom async task type at all — the
    /// cheap guard the per-call-site dispatch probes take before doing any work.</summary>
    public bool HasAdoptedAsync => _adoptedAsync.Count > 0;

    /// <summary>The assembly-qualified builder type name in a type-level
    /// <c>[AsyncMethodBuilder(typeof(B))]</c>, or null. Read straight off the value blob
    /// (ECMA-335 II.23.3: a 0x0001 prolog, then the SerString type name) exactly as
    /// <see cref="InlineArrayLength"/> does — DecodeCustomAttributes drops a CoreLib
    /// attribute like AsyncMethodBuilderAttribute unless user code typeof-names it, and
    /// this read must work regardless. METHOD-level
    /// <c>[AsyncMethodBuilder]</c> (the BCL's own PoolingAsyncValueTaskMethodBuilder
    /// overrides on the async Stream paths) is deliberately not read: it swaps the builder
    /// for one method, it does not make a type task-like.</summary>
    private static string? AsyncMethodBuilderArg(MetadataReader reader, TypeDefinition td)
    {
        foreach (var cah in td.GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(cah);
            if (AttributeTypeName(reader, ca) != "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute")
                continue;
            try
            {
                var br = reader.GetBlobReader(ca.Value);
                if (br.Length >= 2 && br.ReadUInt16() == 0x0001
                    && br.ReadSerializedString() is { Length: > 0 } name)
                    return name;
            }
            catch (BadImageFormatException)
            {
                // An undecodable blob adopts nothing.
            }
        }
        return null;
    }

    /// <summary>The (module, TypeDef) a serialized attribute type name — <c>"Ns.B`1,
    /// TheAssembly, Version=..."</c> — names, or null when it is not a loaded type.
    /// <see cref="TypeIndex"/> is keyed on the RAW name (arity backtick included), which is
    /// exactly what an open generic builder serializes as.</summary>
    private (Module Module, TypeDefinitionHandle Handle)? ResolveSerializedTypeName(string serialized)
    {
        int comma = serialized.IndexOf(',');
        string full = (comma >= 0 ? serialized[..comma] : serialized).Trim();
        if (full.Contains('+'))
            return null; // a nested builder: not a shape any real library uses
        int dot = full.LastIndexOf('.');
        var key = dot >= 0 ? (full[..dot], full[(dot + 1)..]) : ("", full);
        return TypeIndex().TryGetValue(key, out var c) && c.Count > 0 ? c[0] : null;
    }

    /// <summary>The awaiter a task-like type's parameterless <c>GetAwaiter()</c> returns,
    /// as a (module, TypeDef) — read off the signature BLOB rather than decoded, because
    /// for a generic task the return type is <c>GDTask&lt;!0&gt;.Awaiter</c> and decoding
    /// that here (before Pass 2) would instantiate a bogus open specialization. The return
    /// shape is <c>[GENERICINST] (CLASS|VALUETYPE) TypeDefOrRefOrSpec</c>:
    /// SignatureTypeCode.TypeHandle collapses CLASS and VALUETYPE, and for a GENERICINST
    /// the handle names the open template — which is what the registry keys on. Null when
    /// there is no such method, or its return is not a plain (possibly generic) type.</summary>
    private (Module Module, TypeDefinitionHandle Handle)? AwaiterOf(Module module, TypeDefinition td)
    {
        var reader = module.Reader;
        foreach (var mdh in td.GetMethods())
        {
            var md = reader.GetMethodDefinition(mdh);
            if (reader.GetString(md.Name) != "GetAwaiter")
                continue;
            EntityHandle ret;
            try
            {
                var blob = reader.GetBlobReader(md.Signature);
                var hdr = blob.ReadSignatureHeader();
                if (hdr.IsGeneric)
                    blob.ReadCompressedInteger();       // method generic-param count
                if (blob.ReadCompressedInteger() != 0)  // parameter count: must be 0
                    continue;
                var tc = blob.ReadSignatureTypeCode();
                if (tc == SignatureTypeCode.GenericTypeInstance)
                    tc = blob.ReadSignatureTypeCode();
                if (tc != SignatureTypeCode.TypeHandle)
                    continue;
                ret = blob.ReadTypeHandle();
            }
            catch (BadImageFormatException)
            {
                continue;
            }
            switch (ret.Kind)
            {
                case HandleKind.TypeDefinition:
                    return (module, (TypeDefinitionHandle)ret);
                case HandleKind.TypeReference:
                    var (m, h) = TemplateOrClassDef(ResolveTypeRef(module, (TypeReferenceHandle)ret));
                    return m is null ? null : (m, h);
            }
        }
        return null;
    }

    /// <summary>Whether a TypeDef's base is System.ValueType (a struct), from raw metadata
    /// — Pass 1.5 runs before Pass 2's base-type scan sets <see cref="ClassInfo.IsValueType"/>.</summary>
    private static bool IsValueTypeDef(MetadataReader reader, TypeDefinition td) => td.BaseType switch
    {
        { IsNil: false, Kind: HandleKind.TypeDefinition } b =>
            TypeFullName(reader, (TypeDefinitionHandle)b) == "System.ValueType",
        { IsNil: false, Kind: HandleKind.TypeReference } b =>
            TypeRefFullName(reader, (TypeReferenceHandle)b) == "System.ValueType",
        _ => false,
    };

    /// <summary>The kind bits of an open generic DEFINITION at <paramref name="handle"/> in
    /// <paramref name="m"/> — interface/abstract/sealed from its <see cref="TypeAttributes"/>,
    /// value-type from its base — for a SYNTHETIC <c>gendef_</c> handle to answer
    /// <c>Type.IsInterface</c>/<c>IsAbstract</c>/<c>IsValueType</c>/<c>IsSealed</c>.
    /// Used for a same-assembly open def a bare <c>typeof</c> named but no closed
    /// instantiation minted a natural gendef for; the cross-assembly (<c>External</c>) case
    /// has no ClassInfo to open and stays <see cref="GenericDefKind.Unknown"/>.</summary>
    public static GenericDefKind ResolveOpenGenericDefKind(Module m, TypeDefinitionHandle handle)
    {
        var reader = m.Reader;
        var td = reader.GetTypeDefinition(handle);
        var kind = GenericDefKind.Unknown;
        if ((td.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
            kind |= GenericDefKind.Interface | GenericDefKind.Abstract;   // an interface is abstract
        else if ((td.Attributes & TypeAttributes.Abstract) != 0)
            kind |= GenericDefKind.Abstract;
        if ((td.Attributes & TypeAttributes.Sealed) != 0)
            kind |= GenericDefKind.Sealed;
        if (IsValueTypeDef(reader, td))
            kind |= GenericDefKind.ValueType;
        return kind;
    }

    /// <summary>The declared type-parameter names of the generic definition at
    /// <paramref name="handle"/> in <paramref name="m"/>, comma-joined in declaration order
    /// ("TKey,TValue"), or null when the type declares none or its metadata is unreadable.
    /// Stamped on the <c>gendef_</c> handle so <c>Type.ToString()</c> spells
    /// <c>List`1[T]</c> as real .NET does; the names are display text only — dn2cpp
    /// materializes no <c>Type</c> for a type parameter. Ordinal-sorted by
    /// <see cref="GenericParameter.Index"/>, since metadata order is not guaranteed.</summary>
    public static string? GenericParamNames(Module m, TypeDefinitionHandle handle)
    {
        try
        {
            var reader = m.Reader;
            var byIndex = new SortedDictionary<int, string>();
            foreach (var gph in reader.GetTypeDefinition(handle).GetGenericParameters())
            {
                var gp = reader.GetGenericParameter(gph);
                string n = reader.GetString(gp.Name);
                // The names go into a C string literal; a metadata identifier is not
                // obliged to be one, so a name that cannot be spelled degrades the whole
                // list rather than emitting text that does not compile.
                foreach (char ch in n)
                    if (ch < ' ' || ch == '"' || ch == '\\')
                        return null;
                byIndex[gp.Index] = n;
            }
            return byIndex.Count == 0 ? null : string.Join(",", byIndex.Values);
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return null;   // unreadable metadata -> ToString degrades to the bare FullName
        }
    }

    /// <summary>The SUBSTITUTION-INVARIANT ancestry of the open generic definition at
    /// <paramref name="handle"/>: its nearest ancestor class that names no type parameter —
    /// non-generic, or closed over fixed arguments (<c>class D&lt;T&gt; : B&lt;int&gt;</c>) —
    /// and every such interface in its transitive closure.
    ///
    /// <para>Substitution cannot touch a type that names no type parameter, so this set is
    /// identical for the definition and for every closed instantiation of it. That identity
    /// is what lets a <c>gendef_</c> shell carry the rows and answer
    /// <c>Type.IsAssignableFrom</c>; a relation spelled in the definition's own parameters
    /// (<c>IEnumerable&lt;T&gt;</c> ← <c>List&lt;&gt;</c>) stays False because no row can
    /// name one. A closed entry (<c>B&lt;int&gt;</c>) is materialized once, by the wiring
    /// pass (<paramref name="materialize"/>, Compilation.WireTypeofOpenGenericDefAncestry)
    /// — a reflected-over definition observably needs those type-infos, so its rows must
    /// not depend on unrelated code happening to instantiate them. Every later read is
    /// lookup-only and finds what the wiring minted; a close absent even then degrades to
    /// the walk stepping past it, the same monotone subset the emitter's definedness
    /// filter makes.</para>
    ///
    /// <para>Everything ABOVE the nearest invariant ancestor is left out: it becomes the
    /// shell's base pointer and the runtime's own base walk reaches the rest from there.
    /// Metadata order throughout — interface list order, then base-chain order — so the
    /// emitted row order is a pure function of the input.</para></summary>
    internal (ClassInfo? Base, List<ClassInfo> Interfaces) OpenGenericDefAncestry(
        Module m, TypeDefinitionHandle handle, bool materialize = false)
    {
        var itfs = new List<ClassInfo>();
        var seen = new HashSet<(int, TypeDefinitionHandle)>();
        ClassInfo? nearest = null;
        Module? node = m;
        var nodeHandle = handle;
        while (node is { } nm)
        {
            CollectArgumentFreeInterfaces(nm, nodeHandle, itfs, seen, materialize);
            var rawBase = BaseTypeHandleOf(nm, nodeHandle);
            var (bm, bh) = ResolveTypeDefRefSpec(nm, rawBase);
            if (bm is null)
                break;
            if (GenericArityOf(bm, bh) != 0)
            {
                if (ClosedInvariantClass(nm, rawBase, materialize) is { } closed)
                {
                    nearest = closed;
                    break;
                }
                node = bm;
                nodeHandle = bh;
                continue;
            }
            bm.ClassMap.TryGetValue(bh, out nearest);
            break;
        }
        return (nearest, itfs);
    }

    /// <summary>The by-NAME entry to <see cref="OpenGenericDefAncestry"/>, for a definition
    /// reached as a bare <c>typeof(D&lt;&gt;)</c> (no ClassInfo, only the backtick name).
    /// A name no loaded module declares yields an empty ancestry — the same degrade
    /// <see cref="OpenGenericDefFactsByName"/> makes.</summary>
    internal (ClassInfo? Base, List<ClassInfo> Interfaces) OpenGenericDefAncestryByName(
        string defName, bool materialize = false) =>
        OpenGenericDefHandleByName(defName) is { } d
            ? OpenGenericDefAncestry(d.Module, d.Handle, materialize)
            : (null, new List<ClassInfo>());

    /// <summary>Appends every substitution-invariant interface reachable from
    /// <paramref name="handle"/>'s own interface list: non-generic ones, closed ones over
    /// fixed arguments (<c>I&lt;string&gt;</c>), and — recursing THROUGH the
    /// generic entries either way — a generic interface's closure can still hold
    /// argument-free members (<c>IEnumerable&lt;T&gt;</c> is an <c>IEnumerable</c>).</summary>
    private void CollectArgumentFreeInterfaces(Module m, TypeDefinitionHandle handle,
        List<ClassInfo> itfs, HashSet<(int, TypeDefinitionHandle)> seen, bool materialize)
    {
        if (!seen.Add((m.Index, handle)))
            return;
        var reader = m.Reader;
        InterfaceImplementationHandleCollection impls;
        try
        {
            impls = reader.GetTypeDefinition(handle).GetInterfaceImplementations();
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return;
        }
        foreach (var iih in impls)
        {
            var raw = reader.GetInterfaceImplementation(iih).Interface;
            var (im, ih) = ResolveTypeDefRefSpec(m, raw);
            if (im is null)
                continue;
            if (GenericArityOf(im, ih) != 0)
            {
                if (ClosedInvariantClass(m, raw, materialize) is { } closed && !itfs.Contains(closed))
                    itfs.Add(closed);
                CollectArgumentFreeInterfaces(im, ih, itfs, seen, materialize);
            }
            else if (im.ClassMap.TryGetValue(ih, out var itf) && !itfs.Contains(itf))
                itfs.Add(itf);
        }
    }

    private static EntityHandle BaseTypeHandleOf(Module m, TypeDefinitionHandle handle)
    {
        try
        {
            return m.Reader.GetTypeDefinition(handle).BaseType;
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return default;
        }
    }

    private static int GenericArityOf(Module m, TypeDefinitionHandle handle)
    {
        try
        {
            return m.Reader.GetTypeDefinition(handle).GetGenericParameters().Count;
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return 0;
        }
    }

    /// <summary>A TypeDefOrRefOrSpec resolved to the (module, TypeDef) of its DEFINITION.
    /// A TypeSpec is read off the signature BLOB rather than decoded: its GENERICINST names
    /// the open template, which is exactly what an argument-free walk wants, and decoding it
    /// would instantiate a specialization the load set never asked for. Null module when the
    /// load set does not carry the target.</summary>
    private (Module?, TypeDefinitionHandle) ResolveTypeDefRefSpec(Module m, EntityHandle h)
    {
        switch (h.IsNil ? HandleKind.Blob : h.Kind)
        {
            case HandleKind.TypeDefinition:
                return (m, (TypeDefinitionHandle)h);
            case HandleKind.TypeReference:
                return TemplateOrClassDef(ResolveTypeRef(m, (TypeReferenceHandle)h));
            case HandleKind.TypeSpecification:
                try
                {
                    var blob = m.Reader.GetBlobReader(
                        m.Reader.GetTypeSpecification((TypeSpecificationHandle)h).Signature);
                    var tc = blob.ReadSignatureTypeCode();
                    if (tc == SignatureTypeCode.GenericTypeInstance)
                        tc = blob.ReadSignatureTypeCode();
                    if (tc != SignatureTypeCode.TypeHandle)
                        return (null, default);
                    var inner = blob.ReadTypeHandle();
                    // A GENERICINST names a TypeDef or TypeRef; anything else is not a
                    // shape this walk can open, and re-entering would not terminate.
                    return inner.Kind == HandleKind.TypeSpecification
                        ? (null, default)
                        : ResolveTypeDefRefSpec(m, inner);
                }
                catch (Exception e) when (!IsMustEscape(e))
                {
                    return (null, default);
                }
            default:
                return (null, default);
        }
    }

    /// <summary>The ClassInfo of a substitution-invariant closed TypeSpec — a GENERICINST
    /// whose blob names no VAR/MVAR, e.g. <c>B&lt;int&gt;</c> under
    /// <c>class D&lt;T&gt; : B&lt;int&gt;</c>. With <paramref name="materialize"/> the
    /// decode runs the real provider and MINTS the close (the wiring pass, inside the emit
    /// fixpoint); without it the decode answers from the existing instance table only, so
    /// a close nothing minted yields null instead of a fresh shell — the post-fixpoint
    /// emitter reads must not grow the emit set. The VAR/MVAR probe walks the signature
    /// grammar, so only a genuine parameter occurrence disqualifies — a compressed-integer
    /// byte (an arg count of 19, say) cannot collide with the codes.</summary>
    private ClassInfo? ClosedInvariantClass(Module m, EntityHandle h, bool materialize)
    {
        if (h.IsNil || h.Kind != HandleKind.TypeSpecification)
            return null;
        try
        {
            var spec = m.Reader.GetTypeSpecification((TypeSpecificationHandle)h);
            if (spec.DecodeSignature(ParameterMentionProbe.Instance, null))
                return null;
            if (materialize)
            {
                var d = spec.DecodeSignature(SigProvider, null);
                return d.Kind != TypeKind.Class ? null : d.Class;
            }
            var lookup = new LookupOnlySignatureProvider(this);
            var t = spec.DecodeSignature(lookup, null);
            return lookup.Missed || t.Kind != TypeKind.Class ? null : t.Class;
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return null;
        }
    }

    /// <summary>A <see cref="SignatureProvider"/> whose GENERICINST arm answers from
    /// <see cref="InstancesFor"/> instead of <see cref="Instantiate"/>: a hit returns the
    /// existing close, a miss sets <see cref="Missed"/> and the caller discards the decode.
    /// Everything else delegates to the real provider.</summary>
    private sealed class LookupOnlySignatureProvider : ISignatureTypeProvider<TypeDesc, object?>
    {
        private readonly Compilation _c;
        internal bool Missed;

        internal LookupOnlySignatureProvider(Compilation c) => _c = c;

        public TypeDesc GetGenericInstantiation(TypeDesc genericType, ImmutableArray<TypeDesc> typeArguments)
        {
            if (!Missed && genericType.Kind == TypeKind.Template
                && _c.InstancesFor(genericType.TemplateModule!, genericType.TemplateHandle)
                     .TryGetValue(typeArguments.ToArray(), out var cls))
                return TypeDesc.MakeClass(cls);
            Missed = true;
            return genericType;
        }

        public TypeDesc GetPrimitiveType(PrimitiveTypeCode typeCode) => _c.SigProvider.GetPrimitiveType(typeCode);
        public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) =>
            _c.SigProvider.GetTypeFromDefinition(reader, handle, rawTypeKind);
        public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) =>
            _c.SigProvider.GetTypeFromReference(reader, handle, rawTypeKind);
        public TypeDesc GetSZArrayType(TypeDesc elementType) => _c.SigProvider.GetSZArrayType(elementType);
        public TypeDesc GetArrayType(TypeDesc elementType, ArrayShape shape) => _c.SigProvider.GetArrayType(elementType, shape);
        public TypeDesc GetByReferenceType(TypeDesc elementType) => _c.SigProvider.GetByReferenceType(elementType);
        public TypeDesc GetPointerType(TypeDesc elementType) => _c.SigProvider.GetPointerType(elementType);
        public TypeDesc GetFunctionPointerType(MethodSignature<TypeDesc> signature) => _c.SigProvider.GetFunctionPointerType(signature);
        public TypeDesc GetGenericMethodParameter(object? genericContext, int index) =>
            _c.SigProvider.GetGenericMethodParameter(genericContext, index);
        public TypeDesc GetGenericTypeParameter(object? genericContext, int index) =>
            _c.SigProvider.GetGenericTypeParameter(genericContext, index);
        public TypeDesc GetModifiedType(TypeDesc modifier, TypeDesc unmodifiedType, bool isRequired) => unmodifiedType;
        public TypeDesc GetPinnedType(TypeDesc elementType) => elementType;
        public TypeDesc GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
    }

    /// <summary>Decodes to "does this signature mention a VAR/MVAR anywhere?" — the
    /// substitution-invariance test <see cref="ClosedInvariantClass"/> runs before the
    /// real decode. Stateless; composites OR their children, leaves are false.</summary>
    private sealed class ParameterMentionProbe : ISignatureTypeProvider<bool, object?>
    {
        internal static readonly ParameterMentionProbe Instance = new();

        public bool GetGenericTypeParameter(object? genericContext, int index) => true;
        public bool GetGenericMethodParameter(object? genericContext, int index) => true;

        public bool GetGenericInstantiation(bool genericType, ImmutableArray<bool> typeArguments)
        {
            if (genericType)
                return true;
            foreach (bool arg in typeArguments)
                if (arg)
                    return true;
            return false;
        }

        public bool GetFunctionPointerType(MethodSignature<bool> signature)
        {
            if (signature.ReturnType)
                return true;
            foreach (bool p in signature.ParameterTypes)
                if (p)
                    return true;
            return false;
        }

        public bool GetPrimitiveType(PrimitiveTypeCode typeCode) => false;
        public bool GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => false;
        public bool GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => false;
        public bool GetTypeFromSpecification(MetadataReader reader, object? genericContext, TypeSpecificationHandle handle, byte rawTypeKind) =>
            reader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);
        public bool GetSZArrayType(bool elementType) => elementType;
        public bool GetArrayType(bool elementType, ArrayShape shape) => elementType;
        public bool GetByReferenceType(bool elementType) => elementType;
        public bool GetPointerType(bool elementType) => elementType;
        public bool GetModifiedType(bool modifier, bool unmodifiedType, bool isRequired) => modifier || unmodifiedType;
        public bool GetPinnedType(bool elementType) => elementType;
    }

    private static bool HasMethodNamed(MetadataReader reader, TypeDefinition td, string name)
    {
        foreach (var mdh in td.GetMethods())
            if (reader.GetString(reader.GetMethodDefinition(mdh).Name) == name)
                return true;
        return false;
    }

    /// <summary>A TypeDef's arity-stripped full name (<c>GDTask`1</c> -> "GodotTask.GDTask"),
    /// the key the intrinsic maps use.</summary>
    private static string DefFullName(MetadataReader reader, TypeDefinition td)
    {
        string ns = reader.GetString(td.Namespace);
        string nm = StripArity(reader.GetString(td.Name));
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    /// <summary>True if the type carries IsByRefLikeAttribute (a C# ref struct).
    /// Such types are never boxed, so their virtual slots are never dispatched.</summary>
    private static bool IsByRefLikeType(MetadataReader reader, TypeDefinition td)
    {
        foreach (var cah in td.GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(cah);
            if (AttributeTypeName(reader, ca) == "System.Runtime.CompilerServices.IsByRefLikeAttribute")
                return true;
        }
        return false;
    }

    /// <summary>The full name of a custom attribute's type (its constructor's
    /// declaring type), resolving both the MethodDef (within-assembly) and MemberRef
    /// (cross-assembly) constructor forms; null if it can't be resolved. Internal:
    /// the Godot backend's attribute probes ([Export]/[Signal]) share this instead
    /// of carrying their own copies of the two-form resolution.</summary>
    internal static string? AttributeTypeName(MetadataReader reader, CustomAttribute ca) =>
        ca.Constructor.Kind switch
        {
            HandleKind.MethodDefinition =>
                TypeFullName(reader, reader.GetMethodDefinition((MethodDefinitionHandle)ca.Constructor).GetDeclaringType()),
            HandleKind.MemberReference =>
                reader.GetMemberReference((MemberReferenceHandle)ca.Constructor).Parent switch
                {
                    { Kind: HandleKind.TypeReference } p => TypeRefFullName(reader, (TypeReferenceHandle)p),
                    { Kind: HandleKind.TypeDefinition } p => TypeFullName(reader, (TypeDefinitionHandle)p),
                    _ => null,
                },
            _ => null,
        };

    /// <summary>True when the field carries [System.ThreadStaticAttribute] (a BCL
    /// attribute DecodeCustomAttributes drops unless user code typeof-names it, and this
    /// read must work regardless — so detected by name). Used to emit the static field
    /// as thread_local.</summary>
    private static bool HasThreadStatic(MetadataReader reader, FieldDefinition fd)
    {
        foreach (var cah in fd.GetCustomAttributes())
            if (AttributeTypeName(reader, reader.GetCustomAttribute(cah)) == "System.ThreadStaticAttribute")
                return true;
        return false;
    }

    /// <summary>Detects the interop attributes the transpiler honors on a method, in
    /// one walk over its custom attributes (both are matched by full name like
    /// [ThreadStatic] above, so neither attribute's declaring assembly needs to be
    /// loaded or referenced):
    ///
    /// <c>[UnmanagedCallersOnly]</c> — decodes its optional <c>EntryPoint</c> named
    /// field (the C symbol the method is exported under). The <c>CallConvs</c> named
    /// field is ignored: every supported target's default C calling convention already
    /// matches CallConvCdecl, the only convention the attribute is used with outside
    /// Windows-specific interop. A value blob that fails to decode still marks the
    /// method (address-only use needs no entry point).
    ///
    /// <c>[Dn2Cpp.Runtime.NativeImplementation(module[, entryPoint])]</c> — decodes
    /// the (module, entry point) P/Invoke import this method is the managed
    /// implementation of (see <see cref="NativeImpls"/>); the entry point defaults to
    /// the method's own name, mirroring [DllImport]'s default.</summary>
    private (bool IsUco, string? EntryPoint, (string Module, string Entry)? NativeImpl)
        ReadMethodInteropAttributes(Module module, MethodDefinition md)
    {
        var reader = module.Reader;
        bool isUco = false;
        string? ucoEntryPoint = null;
        (string Module, string Entry)? nativeImpl = null;
        foreach (var cah in md.GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(cah);
            switch (AttributeTypeName(reader, ca))
            {
                case "System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute":
                    isUco = true;
                    try
                    {
                        var val = ca.DecodeValue(AttrProvider);
                        foreach (var na in val.NamedArguments)
                            if (na.Name == "EntryPoint" && na.Value is string s)
                                ucoEntryPoint = s;
                    }
                    catch (Exception e) when (!IsMustEscape(e))
                    {
                        // An undecodable blob (exotic CallConvs encoding) still marks the
                        // method as a native entry point; it just exports no symbol.
                    }
                    break;
                case "Dn2Cpp.Runtime.NativeImplementationAttribute":
                    var niVal = ca.DecodeValue(AttrProvider);
                    if (niVal.FixedArguments.Length >= 1 && niVal.FixedArguments[0].Value is string niModule)
                        nativeImpl = (niModule,
                            niVal.FixedArguments.Length >= 2 && niVal.FixedArguments[1].Value is string niEntry
                                ? niEntry
                                : reader.GetString(md.Name));
                    break;
            }
        }
        return (isUco, ucoEntryPoint, nativeImpl);
    }

    /// <summary>A field's decoded <c>[MarshalAs]</c> descriptor: the
    /// <c>UnmanagedType</c> itself, its <c>SizeConst</c> (-1 when absent), and a
    /// <c>ByValArray</c>'s optional <c>ArraySubType</c>. <c>Kind</c> is <c>default</c> (0)
    /// when the field carries no descriptor — <c>UnmanagedType</c> has no 0-valued member,
    /// so the zero is unambiguous.</summary>
    internal readonly record struct FieldMarshalDesc(
        System.Runtime.InteropServices.UnmanagedType Kind,
        int SizeConst,
        System.Runtime.InteropServices.UnmanagedType ArraySubType);

    /// <summary>Decodes a field's marshalling-descriptor blob (ECMA-335 §II.23.4). The blob
    /// is the UnmanagedType byte, followed — for the two INLINE forms — by a compressed
    /// element/character count:
    /// <list type="bullet">
    /// <item><c>ByValArray</c> (0x1E): compressed SizeConst, then an optional compressed
    /// element UnmanagedType.</item>
    /// <item><c>ByValTStr</c> (0x17): compressed SizeConst.</item>
    /// </list>
    /// Every other kind is the bare byte as far as this reader is concerned; a trailing
    /// blob it does not model (LPArray's parameter/flags tail, a custom marshaller's type
    /// names) is left unread, because the KIND alone is what decides the shape and a
    /// half-understood tail is worse than none.
    ///
    /// <para>A malformed or unreadable blob degrades to "no descriptor", which is the
    /// conservative answer in both directions: the P/Invoke layout emitter lays the field
    /// out normally, and the marshalled-size model treats the field by its declared type —
    /// neither invents an inline extent nobody wrote.</para></summary>
    private static FieldMarshalDesc ReadFieldMarshal(MetadataReader reader, FieldDefinition fd)
    {
        try
        {
            var bh = fd.GetMarshallingDescriptor();
            if (bh.IsNil)
                return default;
            var blob = reader.GetBlobReader(bh);
            if (blob.Length == 0)
                return default;
            var kind = (System.Runtime.InteropServices.UnmanagedType)blob.ReadByte();
            int sizeConst = -1;
            var sub = default(System.Runtime.InteropServices.UnmanagedType);
            if (kind is System.Runtime.InteropServices.UnmanagedType.ByValArray
                or System.Runtime.InteropServices.UnmanagedType.ByValTStr
                && blob.RemainingBytes > 0)
            {
                sizeConst = blob.ReadCompressedInteger();
                if (kind == System.Runtime.InteropServices.UnmanagedType.ByValArray
                    && blob.RemainingBytes > 0)
                    sub = (System.Runtime.InteropServices.UnmanagedType)blob.ReadCompressedInteger();
            }
            return new FieldMarshalDesc(kind, sizeConst, sub);
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            return default;
        }
    }

    /// <summary>The SizeConst (element count N) of a field's
    /// <c>[MarshalAs(UnmanagedType.ByValArray, SizeConst = N)]</c> descriptor — the field is
    /// an inline fixed-length array laid out contiguously inside its declaring struct — or
    /// -1 when the field carries no ByValArray descriptor (a normal field) or one without an
    /// explicit SizeConst. Kept as its own accessor over <see cref="ReadFieldMarshal"/>
    /// because it is what the P/Invoke <c>tn_</c> layout emitter reads, and that reader must
    /// not start seeing the other descriptor kinds: they are shapes it does not lay out.</summary>
    private static int ByValArraySizeOf(in FieldMarshalDesc d) =>
        d.Kind == System.Runtime.InteropServices.UnmanagedType.ByValArray ? d.SizeConst : -1;

    /// <summary>A custom attribute applied to a reflected element, resolved to its
    /// attribute class + constructor and its decoded positional/named arguments. Only
    /// attribute types built into the program are produced: defined in a loaded
    /// non-framework module (the app module or a user library pulled in with -r), or
    /// framework-declared but named by a reachable user-module typeof
    /// (<see cref="IsUserTypeofNamedFrameworkType"/>).</summary>
    internal sealed class DecodedAttribute
    {
        public required ClassInfo AttrClass;
        public required MethodInfo Ctor;
        public required ImmutableArray<CustomAttributeTypedArgument<TypeDesc>> Fixed;
        public required ImmutableArray<CustomAttributeNamedArgument<TypeDesc>> Named;
    }

    /// <summary>Decodes the custom attributes applied to one reflected element (a type,
    /// member or assembly), keeping only attribute types built into the program — defined
    /// in a loaded non-framework module — whose value blob decodes. A same-module
    /// attribute's ctor is a MethodDefinition; a cross-module one (e.g. an app element
    /// carrying a library-defined attribute) is a MemberReference resolved against the
    /// loaded modules. A BCL/compiler attribute (its class lives in a framework assembly)
    /// is skipped either way, matching the IL2CPP-managed-stripping bound: only
    /// attributes built into the program are reflectable — with ONE bounded widening.
    /// A framework-declared attribute type that a reachable USER-module body names with
    /// typeof (<see cref="IsUserTypeofNamedFrameworkType"/>) is kept too: the typeof is
    /// the statically visible evidence that the program reflects over that BCL attribute
    /// (`GetCustomAttributes(typeof(DescriptionAttribute), false)` — Thrive's EnumHelper
    /// over [Description] enum fields), and the naming is what bounds the keep — an
    /// indiscriminate framework keep would decode + reach every Nullable/CompilerGenerated
    /// row on every reflected element. A framework attribute NOT so named stays dropped
    /// (its element reflects an empty set where real .NET reports the row — the same
    /// managed-stripping divergence as before, asserted by the reflect-types gate's
    /// framework-attribute section).</summary>
    internal List<DecodedAttribute> DecodeCustomAttributes(Module module, CustomAttributeHandleCollection handles)
    {
        var result = new List<DecodedAttribute>();
        var reader = module.Reader;
        foreach (var cah in handles)
        {
            MethodInfo? ctor = null;
            var ca = reader.GetCustomAttribute(cah);
            if (ca.Constructor.Kind == HandleKind.MethodDefinition)
                module.MethodMap.TryGetValue((MethodDefinitionHandle)ca.Constructor, out ctor);
            else if (ca.Constructor.Kind == HandleKind.MemberReference)
                ctor = ResolveAttrCtorRef(module, (MemberReferenceHandle)ca.Constructor);
            if (ctor is null
                || (IsFrameworkAssemblyName(ctor.DeclaringClass.Module.AssemblyName)
                    && !IsUserTypeofNamedFrameworkType(ctor.DeclaringClass.FullName)))
                continue;
            CustomAttributeValue<TypeDesc> val;
            try { val = ca.DecodeValue(AttrProvider); }
            catch (Exception e) when (!IsMustEscape(e)) { continue; }
            result.Add(new DecodedAttribute
            {
                AttrClass = ctor.DeclaringClass,
                Ctor = ctor,
                Fixed = val.FixedArguments,
                Named = val.NamedArguments,
            });
        }
        return result;
    }

    /// <summary>Resolves a custom attribute's constructor MemberReference to the loaded
    /// attribute class's matching .ctor. Two parent shapes reach here. A TypeReference is
    /// the cross-module case (an app element carrying a library-defined attribute): the
    /// parent's full name is looked up across the loaded modules. A TypeSpecification is a
    /// generic attribute — <c>[MyAttr&lt;int&gt;]</c>, C# 11 — whose ctor is referenced
    /// through the closed instantiation even inside its own assembly, because that is how a
    /// closed generic's member is always referenced; it decodes to the specialization's own
    /// ClassInfo, which is what the attribute type has to be (the type argument is part of
    /// the attribute's identity: <c>MyAttr&lt;int&gt;</c> and <c>MyAttr&lt;string&gt;</c>
    /// are different attributes, and each closed form carries its own type-info symbol, so
    /// the runtime's pointer-identity match works unchanged). The ctor is then picked by
    /// decoded parameter arity + type shape. Null when the class isn't loaded or no overload
    /// matches.</summary>
    private MethodInfo? ResolveAttrCtorRef(Module module, MemberReferenceHandle handle)
    {
        ClassInfo cls;
        MethodSignature<TypeDesc> sig;
        try
        {
            var reader = module.Reader;
            var mr = reader.GetMemberReference(handle);
            if (mr.Parent.Kind == HandleKind.TypeSpecification)
            {
                // An attribute application carries no generic context of its own — an
                // attribute argument is a constant, and the parent TypeSpec is closed — so
                // the empty context is the whole context. Decoding it interns the closed
                // ClassInfo (Instantiate), whose members are pulled on demand: the overload
                // scan below reads them, so complete it first.
                if (reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent)
                        .DecodeSignature(SigProvider, GenericContext.Empty) is not { Class: { } spec })
                    return null;
                cls = spec;
                EnsureCompleted(cls);
                sig = mr.DecodeMethodSignature(SigProvider, cls.Context);
            }
            else if (mr.Parent.Kind == HandleKind.TypeReference)
            {
                // Early-out on the resolution scope: a BCL/compiler attribute (the
                // overwhelmingly common MemberReference case — Nullable/CompilerGenerated/…)
                // is skipped by name before the linear class lookup below — unless a
                // reachable user-module typeof named it (the DecodeCustomAttributes
                // widening), in which case it must resolve like a user attribute.
                var tref = reader.GetTypeReference((TypeReferenceHandle)mr.Parent);
                if (tref.ResolutionScope.Kind == HandleKind.AssemblyReference
                    && IsFrameworkAssemblyName(reader.GetString(
                        reader.GetAssemblyReference((AssemblyReferenceHandle)tref.ResolutionScope).Name))
                    // The name-set probe is behind the emptiness test so the common
                    // framework row (empty set) still skips without composing a name.
                    && !(_userTypeofNamedFrameworkTypes.Count != 0
                        && IsUserTypeofNamedFrameworkType(
                            TypeRefFullName(reader, (TypeReferenceHandle)mr.Parent))))
                    return null;
                string full = TypeRefFullName(reader, (TypeReferenceHandle)mr.Parent);
                if (FindClassByFullName(full) is not { } found)
                    return null;
                cls = found;
                sig = mr.DecodeMethodSignature(SigProvider, cls.Context);
            }
            else
            {
                return null;
            }
        }
        catch (Exception e) when (!IsMustEscape(e))
        {
            // An unresolvable/undecodable REFERENCE just contributes no attribute.
            return null;
        }
        // The overload scan sits outside that catch, and has to. It reads a candidate's
        // signature, and a read is a decode (MethodInfo.Signature) — of the program's own
        // metadata, where a failure is a real failure rather than a dangling reference to
        // someone else's. Left inside, it would be swallowed and the attribute would silently
        // contribute nothing instead of failing the transpile. (Only
        // the .ctors are decoded: the name test short-circuits ahead of the signature.)
        foreach (var m in cls.Methods)
        {
            if (m.Name != ".ctor" || m.IsStatic
                || m.Signature.ParameterTypes.Length != sig.ParameterTypes.Length)
                continue;
            bool match = true;
            for (int i = 0; i < sig.ParameterTypes.Length && match; i++)
                match = m.Signature.ParameterTypes[i].ToString() == sig.ParameterTypes[i].ToString();
            if (match)
                return m;
        }
        return null;
    }

    /// <summary>N for a <c>[InlineArray(N)]</c> struct, or 0 if the type is not an
    /// inline array. The attribute's single fixed argument is the element count —
    /// the struct's one declared field provides storage for N contiguous elements
    /// (params-<c>ReadOnlySpan</c> lowering).</summary>
    private static int InlineArrayLength(MetadataReader reader, TypeDefinition td)
    {
        foreach (var cah in td.GetCustomAttributes())
        {
            var ca = reader.GetCustomAttribute(cah);
            if (AttributeTypeName(reader, ca) != "System.Runtime.CompilerServices.InlineArrayAttribute")
                continue;
            // Decode the value blob directly (ECMA-335 II.23.3): a 0x0001 prolog
            // then the single int32 fixed arg (length). Avoids needing a full
            // ICustomAttributeTypeProvider just for one int.
            var br = reader.GetBlobReader(ca.Value);
            if (br.Length < 6 || br.ReadUInt16() != 0x0001)
                continue;
            int n = br.ReadInt32();
            if (n > 0)
                return n;
        }
        return 0;
    }

    private static string TypeFullName(MetadataReader reader, TypeDefinitionHandle h)
    {
        var td = reader.GetTypeDefinition(h);
        string ns = reader.GetString(td.Namespace);
        string nm = reader.GetString(td.Name);
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    private static string TypeRefFullName(MetadataReader reader, TypeReferenceHandle h)
    {
        var tr = reader.GetTypeReference(h);
        string ns = reader.GetString(tr.Namespace);
        string nm = reader.GetString(tr.Name);
        return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
    }

    /// <summary>Drains pending generic specializations to a fixpoint. Body codegen
    /// (<see cref="MethodCompiler.Compile"/>) can resolve a generic type the
    /// discovery scan never instantiated (e.g. a local/cast type), which appends a
    /// fresh <see cref="ClassInfo"/> and enqueues it for completion. Completing one
    /// can enqueue more (its base/field TypeSpecs), so loop until the queue empties.
    /// Returns true if anything was completed (i.e. emission must re-scan).
    ///
    /// <para>Shape only, like the drain inside <see cref="DrainReachability"/> — the queue
    /// says a type was named, not that anything uses it. Callers that go on to read a
    /// drained class's methods pull them (<see cref="EnsureCompleted"/>).</para></summary>
    public bool CompletePendingSpecializations()
    {
        bool any = false;
        while (PendingCount > 0)
        {
            CompleteShape(DequeuePending());
            any = true;
        }
        return any;
    }

    /// <summary>Decodes one method's signature: the single decode point, which is why the
    /// bound's breadcrumb and the shape drain both belong here rather than at any of the
    /// hundreds of places that read a signature.</summary>
    internal void DecodeSignature(MethodInfo m)
    {
        var saved = _decodingMember;
        // The bound's attribution. It sits at the single decode point so EVERY decode is
        // covered — including the ones no member loop makes (an override scan forcing a
        // SigKey, an emitted reflection row). Saved and restored rather than nulled: a decode can fire
        // from inside CompleteShape's field loop, whose own breadcrumb must survive it.
        _decodingMember = (m.DeclaringClass, m.Name, false);
        try
        {
            m.Signature = m.Module.Reader.GetMethodDefinition(m.Handle)
                .DecodeSignature(SigProvider, m.Context);
            ModelCensus.SignaturesDecoded++;
        }
        finally
        {
            _decodingMember = saved;
        }
        // Outside the finally — and this line is what makes the deferral sound rather than
        // merely cheap. The decode may have NAMED closed generics that did not exist yet
        // (SignatureProvider.GetGenericInstantiation calls Instantiate), and a fresh
        // specialization is a SHELL until its shape is completed: IsValueType false, no
        // fields, no base. The caller is about to hand these very descriptors to
        // CppTypes.Of, which spells a pointer when IsValueType is false — so it would not
        // fail, it would quietly emit `t_Span_int32*` where `t_Span_int32` belongs. Every
        // decode the eager model made already sat inside one of the pipeline's drains; this
        // one sits nowhere, so it brings its own.
        //
        // (Outside, because a decode that threw has nothing to shape for, and because a
        // throw out of the drain must not displace the decode's — that one is actionable.)
        CompletePendingSpecializations();
    }

    /// <summary>Decodes one field's type — the field half of <see cref="DecodeSignature"/>, and
    /// the same shape for the same reasons: one decode point, so the bound's breadcrumb and the
    /// shape drain belong here rather than at any of the dozens of places that read a field type.
    ///
    /// <para>The breadcrumb is <c>IsField: true</c>: a field type is decoded
    /// when something asks, so the ask has a context and the reach chain can point at it. What
    /// stays true is that the ask can come from a place no chain reaches — the emit-set closure
    /// asks every emitted class for its layout, and a layout is not a call.</para></summary>
    internal void DecodeFieldType(FieldInfo f)
    {
        var saved = _decodingMember;
        _decodingMember = (f.DeclaringClass, f.Name, true);
        try
        {
            f.Type = f.DeclaringClass.Module.Reader.GetFieldDefinition(f.Handle)
                .DecodeSignature(SigProvider, f.DeclaringClass.Context);
            ModelCensus.FieldTypesDecoded++;
        }
        finally
        {
            _decodingMember = saved;
        }
        // The same load-bearing line DecodeSignature ends on, and for the same reason: this
        // decode may have NAMED closed generics that did not exist, and the caller is about to
        // hand the descriptor it just got to CppTypes, which spells a shell as a pointer rather
        // than failing. A field type is the one place that would be hardest to see — the shell
        // would become a `t_Foo*` member where a `t_Foo` belongs, and the struct would be the
        // wrong SIZE.
        CompletePendingSpecializations();
    }

    /// <summary>Populates what a specialization <em>is</em>: its base, the kind flags that
    /// follow from that base, its layout attributes, its interfaces and its fields.
    ///
    /// <para>This half stays eager — the drain runs it on every instantiation, exactly as
    /// the old single-stage completion did — because every reader that merely <em>names</em>
    /// a type reads it, and a shell answering those with defaults would not fail loudly, it
    /// would quietly emit the wrong thing: <see cref="CppTypes.Of"/> spells a pointer when
    /// <see cref="ClassInfo.IsValueType"/> is false, a struct with no
    /// <see cref="ClassInfo.Fields"/> lays out at size zero, and
    /// <c>GodotBackend.IsExportedGodotClass</c> walks <see cref="ClassInfo.BaseClass"/> to
    /// decide whether a class registers with ClassDB at all. Keeping shape eager makes
    /// that entire class of bug unrepresentable rather than merely unlikely.</para>
    ///
    /// <para>What it costs is one signature decode each for the base, the fields and the
    /// interfaces. What it does not cost is the members — see
    /// <see cref="CompleteMembers"/>, which is where the model's weight and its
    /// non-termination both actually live.</para></summary>
    private void CompleteShape(ClassInfo spec)
    {
        if (spec.ShapeCompleted)
            return;
        // The flag goes up BEFORE the base/interface fill below, so from here until
        // the interface loop closes there is a window where ShapeReady is true but
        // Interfaces is still filling. The InterfaceClosureCache (ImplementsInterface)
        // relies on ImplementsInterface never being asked inside this window: every
        // caller is a reacher/assignability helper unreachable from CompleteShape's
        // call tree (the decodes below reach Instantiate/ResolveTypeRef, never a
        // dispatch resolver). If one ever becomes reachable, a closure read through
        // the half-filled list would be cached as final — move the flag or gate the
        // cache on it then.
        spec.ShapeCompleted = true;
        // A Task-family generic (e.g. AsyncTaskMethodBuilder<int>) lowers to a
        // runtime struct; its members are emitted inline as intrinsics. Decoding
        // its real fields would instantiate the TPL's internal closed generics
        // (Task<T>, scheduler state, …) and drag the whole BCL in, so leave the
        // shell empty — and close the member tier too, so nothing ever pulls it.
        if (spec.IntrinsicCppName is not null)
        {
            spec.MembersCompleted = true;
            // Reference forms (Task<T> -> "Dn2CppTask*") stay reference types;
            // the value forms (builder/awaiter) are structs embedded by value.
            spec.IsValueType = !spec.IntrinsicCppName.EndsWith('*');
            return;
        }
        var module = spec.Module;
        var reader = module.Reader;
        var td = reader.GetTypeDefinition(spec.Handle);
        spec.IsByRefLike = IsByRefLikeType(reader, td);
        spec.InlineArrayLength = InlineArrayLength(reader, td);
        spec.IsExplicitLayout = (td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout;
        spec.IsAutoLayout = (td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout;
        var typeLayout = td.GetLayout();
        spec.LayoutPack = typeLayout.PackingSize;
        spec.LayoutSize = typeLayout.Size;
        spec.LayoutCharSetUnicode =
            (td.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass;
        var ctx = spec.Context;

        if (!td.BaseType.IsNil)
        {
            if (td.BaseType.Kind == HandleKind.TypeDefinition)
            {
                spec.BaseClass = module.ClassMap[(TypeDefinitionHandle)td.BaseType];
                // Same within-assembly TypeDef base case as the non-generic path:
                // derive the kind flags from the base, so a generic corlib value
                // type (e.g. Span<T>) is recognized as such.
                switch (spec.BaseClass.FullName)
                {
                    case "System.ValueType": spec.IsValueType = true; break;
                    case "System.Enum": spec.IsEnum = true; break;
                    case "System.MulticastDelegate" or "System.Delegate": spec.IsDelegate = true; break;
                }
            }
            else if (td.BaseType.Kind == HandleKind.TypeSpecification)
            {
                var bt = reader.GetTypeSpecification((TypeSpecificationHandle)td.BaseType).DecodeSignature(SigProvider, ctx);
                if (bt.Kind == TypeKind.Class)
                    spec.BaseClass = bt.Class;
            }
            else if (td.BaseType.Kind == HandleKind.TypeReference)
            {
                var baseRef = reader.GetTypeReference((TypeReferenceHandle)td.BaseType);
                string bn = reader.GetString(baseRef.Name);
                string bns = reader.GetString(baseRef.Namespace);
                if (bns == "System" && bn == "ValueType") spec.IsValueType = true;
                else if (bns == "System" && bn == "Enum") spec.IsEnum = true;
                else if (bns == "System" && bn is "MulticastDelegate" or "Delegate")
                {
                    spec.IsDelegate = true;
                    if (ResolveTypeRef(module, (TypeReferenceHandle)td.BaseType) is { Kind: TypeKind.Class } cbt)
                        spec.BaseClass = cbt.Class;
                }
                else if (ResolveTypeRef(module, (TypeReferenceHandle)td.BaseType) is { Kind: TypeKind.Class } cbt)
                    spec.BaseClass = cbt.Class;
                else if (ResolveTypeRef(module, (TypeReferenceHandle)td.BaseType) is null
                         && !(bns == "System" && bn == "Object"))
                    // Unloaded cross-assembly base — same External-BCL-exception
                    // ancestor bookkeeping as the Pass-2 non-generic path.
                    spec.ExternalBaseName = TypeRefFullName(reader, (TypeReferenceHandle)td.BaseType);
            }
        }

        // No field type is decoded here — see FieldInfo.Type, and the method loop below, which
        // says the same of signatures. So completing a specialization's SHAPE
        // instantiates none of the generics its fields are merely TYPED at, and a field typed at a
        // deeper instantiation of its own declaring type mints nothing until something asks
        // that field what it is. What this loop must get right is Context — the whole of what
        // the deferred decode needs — and it is right by construction: it is spec.Context.
        foreach (var fdh in td.GetFields())
        {
            var fd = reader.GetFieldDefinition(fdh);
            ModelCensus.FieldsSpec++;
            var fm = ReadFieldMarshal(reader, fd);
            spec.Fields.Add(new FieldInfo
            {
                DeclaringClass = spec,
                Name = reader.GetString(fd.Name),
                Handle = fdh,
                IsStatic = (fd.Attributes & FieldAttributes.Static) != 0,
                IsLiteral = (fd.Attributes & FieldAttributes.Literal) != 0,
                IsThreadStatic = HasThreadStatic(reader, fd),
                ByValArraySize = ByValArraySizeOf(fm),
                MarshalAs = fm.Kind,
                MarshalSizeConst = fm.SizeConst,
                MarshalArraySubType = fm.ArraySubType,
                ExplicitOffset = fd.GetOffset(),
                Attributes = fd.Attributes,
            });
        }

        // CompleteShape leaves the breadcrumb alone: neither loop above decodes a
        // member, and the base/interface decodes below are not a member's signature. Whoever
        // asked for this shape keeps their attribution — a decode's drain runs outside the
        // finally that restores it, so a specialization completed from inside a field or
        // signature decode is still attributed to the member that named it.
        foreach (var iih in td.GetInterfaceImplementations())
        {
            var ii = reader.GetInterfaceImplementation(iih);
            var it = ii.Interface.Kind switch
            {
                HandleKind.TypeDefinition => TypeDesc.MakeClass(module.ClassMap[(TypeDefinitionHandle)ii.Interface]),
                HandleKind.TypeSpecification => reader.GetTypeSpecification((TypeSpecificationHandle)ii.Interface).DecodeSignature(SigProvider, ctx),
                HandleKind.TypeReference => ResolveTypeRef(module, (TypeReferenceHandle)ii.Interface) ?? TypeDesc.MakeExternal("?"),
                _ => throw new NotSupportedException($"{spec.FullName}: unsupported interface kind"),
            };
            if (it.Kind == TypeKind.Class)
                spec.Interfaces.Add(it.Class!);
        }
        ApplyPreservationAfterShape(spec);
    }

    /// <summary>Populates what a specialization can <em>do</em>: its methods, the vtable
    /// built from them, and its explicit interface implementations. Pulled, never pushed —
    /// <see cref="EnsureCompleted"/> is the only way in, and a specialization nothing
    /// reaches never arrives here.
    ///
    /// <para>That is the whole of the model tree-shake: most specializations in a real
    /// corpus have no reachable method, and their members would otherwise be decoded, held,
    /// and never once looked at. Splitting them off costs nothing, because the reachable set
    /// is discovered by resolving calls, and resolving a call is itself a pull: a method
    /// cannot become reachable without its declaring class having been completed.
    /// Reachability and completion therefore stay in step on their own.</para>
    ///
    /// <para>It is also what makes a self-deepening member signature terminate. Decoding a
    /// signature instantiates the types it names, so a member like
    /// <c>GDTask&lt;T&gt;.SuppressCancellationThrow() -&gt; GDTask&lt;(bool, T)&gt;</c> would
    /// recurse without end if the drain decoded <em>their</em> members in turn — with
    /// nothing calling it, <c>-r GDTask.dll</c> alone being enough. A deeper instantiation
    /// is a shell instead, and a shell nothing reaches decodes nothing, so the recursion has
    /// no step to take. The depth bound remains for the shapes that still do: a generic that
    /// <em>calls</em> itself deeper, and a field whose type does.</para></summary>
    internal void CompleteMembers(ClassInfo spec)
    {
        if (spec.MembersCompleted)
            return;
        // Members are decoded against the shape's generic context and the vtable is built on
        // the base's, so the shape has to exist first. (For an intrinsic shell CompleteShape
        // closes the member tier as well — hence the second check rather than an else.)
        CompleteShape(spec);
        if (spec.MembersCompleted)
            return;
        spec.MembersCompleted = true;
        var module = spec.Module;
        var reader = module.Reader;
        var td = reader.GetTypeDefinition(spec.Handle);
        var ctx = spec.Context;

        foreach (var mdh in td.GetMethods())
        {
            var md = reader.GetMethodDefinition(mdh);
            if (md.GetGenericParameters().Count > 0)
                continue; // generic method inside a generic type: not modeled;
                          // skip so closed BCL generics still load
            // No signature is decoded here — see MethodInfo.Signature. So completing a
            // specialization instantiates none of the generics its members merely NAME, and
            // a member whose signature names a deeper instantiation of its own declaring type
            // mints nothing at all until something asks that member for its signature. What
            // this loop must get right is Context: it is the whole of what the deferred decode
            // needs, and it is set right here, at construction.
            ModelCensus.MethodsSpec++;
            var mi = new MethodInfo
            {
                DeclaringClass = spec,
                Name = reader.GetString(md.Name),
                Handle = mdh,
                Module = module,
                Attributes = md.Attributes,
                ImplAttributes = md.ImplAttributes,
                Rva = md.RelativeVirtualAddress,
                Context = ctx,
                PInvoke = ReadPInvoke(reader, md),
            };
            spec.Methods.Add(mi);
            spec.MethodByTemplate[mdh] = mi;
        }

        PopulateMethodImpls(spec, td, module, ctx);

        BuildVtableForSpecialization(spec);

        // The canonical link, if one was made, was made before this class had a vtable to
        // compare. This is the first moment it can be checked — so check it here rather
        // than force every specialization to decode just to be compared.
        VerifyCanonicalLink(spec);
        if (_preservationSeedingActive)
            ApplyPreservation(spec);
    }

    private void BuildVtableForSpecialization(ClassInfo spec)
    {
        if (spec.IsInterface)
        {
            int slot = 0;
            foreach (var m in spec.Methods)
                m.VtableSlot = slot++;
            return;
        }

        var owners = new List<MethodInfo>();
        var impls = new List<MethodInfo?>();
        if (spec.BaseClass is not null)
        {
            // The base's members must be decoded first (its vtable feeds ours). This is the
            // one place a pull recurses, and it recurses along the inheritance chain, which
            // is finite and shallow — not along the signature graph, which is neither.
            EnsureCompleted(spec.BaseClass);
            owners.AddRange(spec.BaseClass.SlotOwners);
            impls.AddRange(spec.BaseClass.Vtable);
        }

        var classOverrides = ClassOverrideDecls(spec);
        foreach (var m in spec.Methods)
        {
            if (!m.IsVirtual)
                continue;
            int slot = ExplicitBaseSlot(spec, m, classOverrides, owners.Count);
            if (slot < 0 && !m.IsNewSlot)
            {
                for (int i = 0; i < owners.Count; i++)
                {
                    if (owners[i].Name == m.Name && owners[i].SigKey == m.SigKey)
                    {
                        slot = i;
                        break;
                    }
                }
            }
            if (slot < 0)
            {
                slot = owners.Count;
                owners.Add(m);
                impls.Add(null);
            }
            owners[slot] = m;
            impls[slot] = m.IsAbstract ? null : m;
            m.VtableSlot = slot;
        }
        spec.SlotOwners = owners;
        spec.Vtable = impls;
    }

    /// <summary>The explicit <c>.override</c> rows of <paramref name="cls"/> whose
    /// declaration is a <b>class</b> virtual rather than an interface method, as
    /// body → declaration. Null when the class has none, which is the overwhelming
    /// majority — the vtable builders skip the lookup entirely then.</summary>
    private static Dictionary<MethodInfo, MethodInfo>? ClassOverrideDecls(ClassInfo cls)
    {
        Dictionary<MethodInfo, MethodInfo>? map = null;
        foreach (var kv in cls.ExplicitInterfaceImpls)
            if (!kv.Key.DeclaringClass.IsInterface)
                (map ??= new Dictionary<MethodInfo, MethodInfo>())[kv.Value] = kv.Key;
        return map;
    }

    /// <summary>The base-class vtable slot an explicit <c>.override</c> row binds
    /// <paramref name="m"/> to, or -1 when no row does.
    ///
    /// <para>This is what makes a <b>covariant return</b> override dispatch. Roslyn
    /// compiles <c>override Dog Clone()</c> against <c>virtual Animal Clone()</c> as a
    /// <c>newslot</c> virtual carrying a MethodImpl row that names the base declaration —
    /// it must, because the runtime's implicit override matching is signature-exact and
    /// the return type has narrowed. The implicit scan therefore cannot see the override
    /// (it skips <c>newslot</c>, and the signatures differ anyway), the method claims a
    /// fresh slot of its own, and a call through the base type keeps dispatching the base
    /// body: not a crash, a silently wrong answer. The row IS the binding, so read it.</para>
    ///
    /// <para>The slot is the declaration's own — a derived vtable preserves base slot
    /// order, the same invariant <see cref="ReachVirtualImpl"/>'s class arm rests on. The
    /// row also covers a plain explicit override of a base class virtual (legal IL, rare
    /// in C#), and it is checked BEFORE the implicit scan so an explicit row always
    /// wins.</para></summary>
    private static int ExplicitBaseSlot(
        ClassInfo cls, MethodInfo m, Dictionary<MethodInfo, MethodInfo>? classOverrides, int slotCount)
    {
        if (classOverrides is null || !classOverrides.TryGetValue(m, out var decl))
            return -1;
        if (decl.DeclaringClass == cls || !DerivesFromOrIs(cls, decl.DeclaringClass))
            return -1;
        return decl.VtableSlot >= 0 && decl.VtableSlot < slotCount ? decl.VtableSlot : -1;
    }

    /// <summary>Decodes the tokens of a reachable method body: materializes
    /// generic instantiations referenced only in IL and follows call/newobj/
    /// ldftn edges so the reachable set grows transitively.</summary>
    private void ScanBodyForGenerics(MethodInfo m)
    {
        _currentScan = m;
        // --trim-godot-classes: whether this body is a USER naming site — anything
        // outside the registry's own module. GodotSharp-internal mentions of engine
        // wrappers never release (counting them cascades back to the full set).
        bool trimUserSite = _godotClassTrim is not null && m.Module != _trimRegistryModule;
        var module = m.Module;
        var reader = module.Reader;
        var body = module.PE.GetMethodBody(m.Rva);
        // Both IL decode and edge resolution can surface unsupported constructs;
        // report them with the offending method and its predecessor (reachability)
        // chain so a failure deep in the BCL points back at the app call site.
        // The pending `constrained.` prefix type for the next callvirt.
        TypeDesc? constrained = null;
        // The struct boxed by the immediately-preceding instruction, pending a
        // formatting call that would dispatch its ToString.
        ClassInfo? boxedForFormat = null;
        // The most recent `ldtoken <type>` operand, pending a
        // RuntimeHelpers.RunClassConstructor call (the typeof(T).TypeHandle chain
        // keeps the token live across the intermediate GetTypeFromHandle /
        // get_TypeHandle calls, so this window persists until the next ldtoken).
        TypeDesc? lastLdtokenType = null;
        try
        {
            var insns = ILDecoder.Decode(body.GetILBytes()!.ToImmutableArrayCompat());
            // Folded-guard dead arms are pruned from reachability: a dead
            // instruction's tokens never become edges. Emission prunes the same
            // offsets (MethodCompiler.Compile reads the identical memoized
            // BranchLiveness), so the emitted call graph and the reachable set
            // stay in lockstep. This scan is the first asker, so it fills
            // MethodInfo.LivenessCache; the planning and emission compiles of
            // the same body both hit (3 computations -> 1).
            var liveness = BranchLiveness.ComputeCached(m, insns, body,
                tok => ConstFoldedCallTarget(module, tok),
                tok => ClassifyTypeIdentityCall(module, tok),
                (tokA, tokB) => TypeEqualityVerdict(module, tokA, tokB, m.Context));
            foreach (var insn in insns)
            {
                if (liveness is not null && !liveness.LiveAt(insn.Offset))
                    continue;
                // Skip non-entity tokens (ldstr carries a UserString handle).
                if (insn.Token == 0 || (uint)insn.Token >> 24 == 0x70)
                    continue;
                var handle = SRME.EntityHandle(insn.Token);
                // boxedForFormat is a single-instruction window: it survives only to
                // the next entity-token instruction (the call that consumes the box).
                var prevBoxed = boxedForFormat;
                boxedForFormat = null;

                switch (insn.OpCode)
                {
                    case ILOpCode.Constrained:
                        constrained = ResolveTypeTokenForScan(module, handle, m.Context);
                        continue;
                    case ILOpCode.Ldtoken:
                        // Track type tokens for the RunClassConstructor edge below.
                        // Field tokens (static array initializers) are not types.
                        if (handle.Kind is HandleKind.TypeDefinition or HandleKind.TypeReference
                            or HandleKind.TypeSpecification)
                        {
                            lastLdtokenType = ResolveTypeTokenForScan(module, handle, m.Context);
                            // Framework-attribute keep-set record (consumed by
                            // DecodeCustomAttributes): a USER-module body's typeof over a
                            // FRAMEWORK-module class is the evidence that the program can
                            // reflect over it. Recorded for every named class, attribute
                            // or not — membership is only ever tested against attribute
                            // ctors' declaring classes, and testing attribute-hood here
                            // would cost a base-chain shape walk per token.
                            if (IsUserModule(module)
                                && lastLdtokenType is { Kind: TypeKind.Class, Class: { } ltc }
                                && IsFrameworkAssemblyName(ltc.Module.AssemblyName))
                                _userTypeofNamedFrameworkTypes.Add(ltc.FullName);
                            // Reflection-invoke keep-set record: a typeof over a
                            // user-LIBRARY class is the evidence that the program can
                            // reflect over its members — consumed by the
                            // typeof-named-library loop beside the app-module invoke
                            // route (the app module needs no record: that route reaches
                            // its every body).
                            if (lastLdtokenType is { Kind: TypeKind.Class, Class: { } lnc }
                                && lnc.Module != AppModule && IsUserModule(lnc.Module)
                                && _typeofNamedLibraryClassesSeen.Add(lnc))
                                _typeofNamedLibraryClasses.Add(lnc);
                            // --trim-godot-classes release trigger: typeof(Sprite2D)
                            // names the wrapper (and a generic token's arguments).
                            if (trimUserSite)
                                TrimNoteNamedType(lastLdtokenType);
                            // Runtime-instantiation trigger record: typeof over an
                            // OPEN generic definition is the only static mouth that
                            // hands MakeGenericType a definition handle, so its
                            // presence in reachable IL is what scopes the $CnAny
                            // template pass to definitions the program can name.
                            if (lastLdtokenType is { } lot
                                && OpenGenericDefBacktickNameOf(lot) is { } odn
                                && _typeofOpenGenericDefSeen.Add(odn))
                                _typeofOpenGenericDefNames.Add(odn);
                        }
                        continue;
                    case ILOpCode.Castclass:
                    case ILOpCode.Isinst:
                    case ILOpCode.Newarr:
                        // --trim-godot-classes release trigger: `(T)GetNode(...)`,
                        // `x is T` / `x as T`, and `new T[n]` all name T — for a cast
                        // that naming is precisely what makes the ancestor-wrapper
                        // fallback sound (a testable type is a named type, so it is
                        // released and the test answers as the true class would).
                        // Resolved only when the trim is armed; the discovery switch
                        // below then runs unchanged (a TypeSpec token was decoded
                        // there before this case existed, and still is).
                        if (trimUserSite)
                            TrimNoteNamedType(ResolveTypeTokenForScan(module, handle, m.Context));
                        break;
                    case ILOpCode.Call:
                    case ILOpCode.Callvirt:
                    case ILOpCode.Ldftn:
                    case ILOpCode.Ldvirtftn:
                    {
                        if (insn.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                            && ResolveStaticCallClass(module, handle, m.Context) is
                                { IsBeforeFieldInit: false, IntrinsicCppName: null } callCls)
                            ReachCctor(callCls);

                        // MethodInfo/MethodBase.Invoke usage enables the reflection-invoke
                        // reachability route: reach all app-module method bodies so
                        // a reflected method is invokable even if never called directly.
                        // The member NAME gates the parent-name read
                        // (CoreIntrinsics.ScanNeedsParentTypeName, whose set is the scan
                        // rows' names UNION the reflection-usage names — read the
                        // invariant at its definition before touching it): only that
                        // handful of member names ever consults the parent (the
                        // IsDynamicCodegenMember namespace-prefilter precedent), so
                        // every other call site skips building the parent full-name
                        // string. Both are raw metadata-name reads — neither resolves
                        // nor decodes anything, so what gets decoded (and in what
                        // order) is unchanged. Hoisted to this scope so the
                        // get_CompareInfo site below reuses them instead of recomputing.
                        string? mrName = null, mrParent = null;
                        if (insn.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                            && handle.Kind == HandleKind.MemberReference)
                        {
                            mrName = module.Reader.GetString(module.Reader.GetMemberReference((MemberReferenceHandle)handle).Name);
                            if (CoreIntrinsics.ScanNeedsParentTypeName(mrName))
                                mrParent = MemberRefParentTypeName(module, (MemberReferenceHandle)handle);
                            // Object-rooted equality on an object-typed receiver — the emit
                            // lowers it to dn2cpp_object_equals / _gethashcode, which answer
                            // from the type-info slots. This is the only "used slot" mark
                            // there can be for the pair: their declaring type is intrinsic,
                            // so they own no vtable slot to mark. (Free here — the name and
                            // the parent are already read.) MarkThenResolve: the token goes
                            // on to ResolveCallTarget below exactly as it would have.
                            if (CoreIntrinsics.ScObjectEqualityDispatch.Matches(mrParent, mrName))
                                NoteObjectEqualityDispatch();
                            if (mrName == "Invoke" && mrParent is "System.Reflection.MethodBase" or "System.Reflection.MethodInfo")
                                _reflectionInvokeUsed = true;
                            // PropertyInfo.GetValue/SetValue invoke the accessor methods,
                            // so treat them like Invoke usage — reach app-module methods
                            // (which include property get_/set_ accessors).
                            else if (mrName is "GetValue" or "SetValue" && mrParent == "System.Reflection.PropertyInfo")
                                _reflectionInvokeUsed = true;
                            // ConstructorInfo.Invoke / non-generic Activator.CreateInstance(Type)
                            // -> reach app-module ctors so a reflected ctor is invokable.
                            else if ((mrName == "Invoke" && mrParent == "System.Reflection.ConstructorInfo")
                                || (mrName == "CreateInstance" && mrParent == "System.Activator"))
                                _reflectionCtorUsed = true;
                            // Type.MakeGenericType -> arm the runtime-instantiation
                            // template pass (paired with the typeof(D<>) record in
                            // the Ldtoken case above).
                            else if (mrName == "MakeGenericType" && mrParent == "System.Type")
                                _makeGenericTypeUsed = true;
                            // MemberInfo/ParameterInfo/Assembly.GetCustomAttributes /
                            // IsDefined -> reach attribute ctors + named setters.
                            else if (mrName is "GetCustomAttributes" or "GetCustomAttribute" or "IsDefined"
                                && mrParent is "System.Reflection.MemberInfo"
                                    or "System.Reflection.ParameterInfo" or "System.Attribute"
                                    or "System.Reflection.CustomAttributeExtensions"
                                    or "System.Type" or "System.Reflection.MethodInfo"
                                    or "System.Reflection.FieldInfo" or "System.Reflection.PropertyInfo"
                                    or "System.Reflection.Assembly")
                                _reflectionAttrUsed = true;
                            // A callvirt to Enum.HasFlag is emitted inline as a bit test,
                            // so don't reach the real body — it pulls in GetMethodTable/
                            // InternalCall. (Checked here to reuse the name/parent reads;
                            // no name above matches "HasFlag", so the order is unchanged.)
                            // EffectThenSkipResolve with an empty effect: the skip is the
                            // whole action.
                            if (CoreIntrinsics.ScEnumHasFlag.Matches(mrParent, mrName))
                            {
                                constrained = null;
                                continue;
                            }
                            // RuntimeHelpers.RunClassConstructor through the MemberRef
                            // mouth — a user-module call site. The in-CoreLib mouth is a
                            // MethodDef token and takes the resolved-target arm below
                            // (`t is { Name: "RunClassConstructor", … }`); this mouth
                            // cannot reuse it, because ResolveCallTarget nulls an
                            // intrinsic-mapped parent (RuntimeHelpers) before that arm is
                            // reached. Same effect as that arm: reach the ldtoken'd
                            // type's cctor body so the __ensure wrapper the emit
                            // intrinsic lowers the call to (MethodCompiler.EmitIntrinsic.
                            // EnumArray's RunClassConstructor case) carries a real body —
                            // an unreached cctor gets a nullptr body, and the call would
                            // silently initialize nothing where real .NET runs the
                            // initializer. An app-module type is already rooted; a
                            // re-Reach is an idempotent no-op. EffectThenSkipResolve,
                            // like the MethodDef arm. "RunClassConstructor" is clause (c)
                            // of CoreIntrinsics.ScanNeedsParentTypeName — drop it there
                            // and mrParent is never read, so this arm never fires.
                            if (mrName == "RunClassConstructor"
                                && mrParent == "System.Runtime.CompilerServices.RuntimeHelpers")
                            {
                                if (lastLdtokenType is { Kind: TypeKind.Class, Class: { } rcc }
                                    && rcc.StaticCctor is { } rccc)
                                    Reach(rccc);
                                constrained = null;
                                continue;
                            }
                        }
                        // The same mark, for the shape the block above cannot see: a body of
                        // the loaded CoreLib calling Object::Equals/GetHashCode names them
                        // with a MethodDef token (same module), not a MemberRef. Read behind
                        // the flag, so this costs one type-name read per call site only until
                        // the first such site — and nothing at all after it.
                        else if (!_objectEqualityDispatched
                            && insn.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                            && handle.Kind == HandleKind.MethodDefinition)
                        {
                            var omd = module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle);
                            // The row's own name half, asked first: composing the
                            // declaring type's full name is what costs here, and the
                            // shared predicate is why this is the same member set as the
                            // row's, not a copy of it.
                            string oname = module.Reader.GetString(omd.Name);
                            if (CoreIntrinsics.IsObjectEqualityMemberName(oname)
                                && CoreIntrinsics.ScObjectEqualityDispatch.Matches(
                                    MethodDefParentTypeName(module, (MethodDefinitionHandle)handle), oname))
                                NoteObjectEqualityDispatch();
                        }
                        // CultureInfo.CompareInfo -> a synthesized zero-initialized
                        // CompareInfo (see the MethodCompiler intrinsic in
                        // EmitIntrinsic.Numbers): mark the transpiled CompareInfo
                        // allocated so its type-info emits. There is no ctor to
                        // reach — the real ctor reads fields of the intrinsic-mapped
                        // CultureInfo — and the invariant arms its reachable members
                        // run read no instance state. CultureInfo is intrinsic-mapped,
                        // so ResolveCallTarget returns null for both call forms.
                        // EffectThenSkipResolve. The row's own NAME half is asked first
                        // — through the shared predicate, never by reading the row's gate
                        // field as a value — and the parent only behind it: obtaining the
                        // parent on the MethodDefinition mouth builds a string, and every
                        // call token of every reachable body passes here.
                        if (insn.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                            && (handle.Kind switch
                                {
                                    HandleKind.MethodDefinition => module.Reader.GetString(
                                        module.Reader.GetMethodDefinition((MethodDefinitionHandle)handle).Name),
                                    // Reuses the name read above ("get_CompareInfo" is in
                                    // its parent-gate set, so mrParent is in hand too).
                                    HandleKind.MemberReference => mrName,
                                    _ => null,
                                }) is { } cmpName
                            && CoreIntrinsics.IsCultureCompareInfoMemberName(cmpName)
                            && CoreIntrinsics.ScCultureCompareInfo.Matches(
                                handle.Kind == HandleKind.MemberReference
                                    ? mrParent : CallTargetTypeName(module, handle),
                                cmpName)
                            && FindClassByFullName("System.Globalization.CompareInfo") is { } cmpInfoCls)
                        {
                            ReachAllocatedType(cmpInfoCls);
                            constrained = null;
                            continue;
                        }
                        var t = ResolveCallTarget(module, handle, m.Context);
                        // Reflection-ctor surface, generic-METHOD half (see
                        // ReachUserSurfaceNamedSpecializationCtors): record the type
                        // arguments of a MethodSpec an app-module body instantiates.
                        // The resolved target's Context already carries the closed
                        // TypeDescs — reading it decodes nothing — and recording is
                        // unconditional so the surface cannot depend on when (or
                        // whether-yet) _reflectionCtorUsed was set. Deliberately the
                        // APP module only, not every user module: a -r library's
                        // MethodSpec args are overwhelmingly its own plumbing, not
                        // deserialization seeds — GodotSharp's marshalling
                        // instantiations alone opened enough of its surface to
                        // allocate List<Godot.Variant> and fail the transpile on its
                        // IndexOf. A Deserialize<T> call INSIDE a library is the
                        // documented residue of that bound.
                        if (module == AppModule && t is not null && t.Context.MethodArgs.Length > 0)
                            _appMethodSpecTypeArgs.AddRange(t.Context.MethodArgs);
                        // Reflection-usage mark, MethodSpec mouth: the generic
                        // CustomAttributeExtensions.GetCustomAttribute<T>/
                        // GetCustomAttributes<T> forms arrive as MethodSpec tokens,
                        // which the MemberReference mark above never sees. Without
                        // this, a program whose only attribute read is the generic
                        // extension emits no attribute tables at all and silently
                        // answers "no attributes" (MarkThenResolve; the name gates
                        // the FullName read).
                        if (insn.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                            && handle.Kind == HandleKind.MethodSpecification
                            && t is { Name: "GetCustomAttributes" or "GetCustomAttribute" }
                            && t.DeclaringClass.FullName == "System.Reflection.CustomAttributeExtensions")
                            _reflectionAttrUsed = true;
                        // A bodyless P/Invoke with a registered managed native
                        // implementation: the call site lowers to a direct call to the
                        // implementation (MethodCompiler.EmitNativeImplCall), so reach
                        // it here — the Reach(t) below no-ops on Rva == 0.
                        if (t is { Rva: 0, PInvoke: { } tpinv } && NativeImplFor(tpinv) is { } timpl
                            && !ReferenceEquals(timpl, m))
                            Reach(timpl);
                        // --trim-godot-classes, the scan-side CUT: the registry
                        // cctor's per-class allocate lambdas are deferred here
                        // instead of reached; an unrecognized shape falls through
                        // to the ordinary Reach below (kept, never cut).
                        if (insn.OpCode == ILOpCode.Ldftn && t is not null
                            && TrimTryDeferRegistryLambda(m, t))
                        {
                            constrained = null;
                            continue;
                        }
                        // --trim-godot-classes release trigger: a user call site's
                        // generic arguments (GetNode<Sprite2D>, a method on
                        // Godot.Collections.Array<Sprite2D>) name engine wrappers.
                        if (trimUserSite)
                            TrimNoteCallSite(module, handle, m.Context);
                        // Comparer<T>.Default -> synthesized GenericComparer<T>:
                        // don't transpile the reflection-based real getter; reach the
                        // comparer's ctor and allocate it so its Compare vtable slot is
                        // emitted (its Compare uses x.CompareTo(y), devirtualized).
                        if (t is not null && IsComparerGetDefault(t)
                            && GenericComparerFor(t.DeclaringClass.Context.TypeArgs[0]) is { } gc)
                        {
                            if (ParameterlessCtor(gc) is { } gctor)
                                Reach(gctor);
                            ReachAllocatedType(gc);
                            constrained = null;
                            continue;
                        }
                        // StringComparer.CurrentCulture/InvariantCulture -> an ordinal
                        // GenericComparer<string> (see IsStringComparerCultureGetter): reach
                        // its ctor and allocate it exactly like Comparer<T>.Default, so the
                        // real culture getter (CultureAwareComparer -> CompareInfo -> ICU) is
                        // never a reachability edge.
                        if (t is not null && IsStringComparerCultureGetter(t)
                            && GenericComparerFor(TypeDesc.MakePrimitive(PrimitiveTypeCode.String)) is { } sgc)
                        {
                            if (ParameterlessCtor(sgc) is { } sgctor)
                                Reach(sgctor);
                            ReachAllocatedType(sgc);
                            constrained = null;
                            continue;
                        }
                        // Activator.CreateInstance<T>() -> synthesized `new T()`:
                        // the real reflection body is cut (ResolveCallTarget returns null
                        // above), so reach T's parameterless ctor — and, for a reference
                        // type, mark it allocated so its type-info/vtable emit. Mirrors
                        // the Comparer<T>.Default edge.
                        if (handle.Kind == HandleKind.MethodSpecification
                            && IsActivatorCreateInstanceSpec(module, (MethodSpecificationHandle)handle))
                        {
                            var aargs = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle)
                                .DecodeSignature(SigProvider, m.Context).ToArray();
                            if (aargs.Length > 0 && aargs[0].Kind == TypeKind.Class && aargs[0].Class is { } acls)
                            {
                                // Public-only, mirroring the emit verdict: the factory binds
                                // only a public parameterless ctor, so a non-public one
                                // (Task<T>'s internal ctor) is not reached — emit routes that
                                // T to the run-time MissingMethodException, not to a ctor call.
                                if (PublicParameterlessCtor(acls) is { } actor)
                                    Reach(actor);
                                if (!acls.IsValueType)
                                    ReachAllocatedType(acls);
                            }
                            constrained = null;
                            continue;
                        }
                        // RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle)
                        // lowers at emit to T's idempotent __ensure wrapper (see the
                        // intrinsic in MethodCompiler): reach T's cctor body here so
                        // the wrapper is not empty, and skip the real reflection body.
                        if (t is { Name: "RunClassConstructor", DeclaringClass.FullName: "System.Runtime.CompilerServices.RuntimeHelpers" })
                        {
                            if (lastLdtokenType is { Kind: TypeKind.Class, Class: { } lcc }
                                && lcc.StaticCctor is { } lccc)
                                Reach(lccc);
                            constrained = null;
                            continue;
                        }
                        // IBinaryInteger<TSelf>.ReadBigEndian span overload on an integer
                        // primitive: emit lowers the whole call to dn2cpp_read_big_endian
                        // (its default body's TryReadBigEndian leaf is an InternalCall with
                        // no IL), so its DIM body must NOT enter the tree — cut the ordinary
                        // Reach(t) edge below. cut ⟹ route: the same predicate gates the emit
                        // route (MethodCompiler.TryEmitGenericMathIntrinsic).
                        if (t is not null && insn.OpCode == ILOpCode.Call
                            && constrained is { Kind: TypeKind.Primitive, IsCanonPlaceholder: false }
                            && IsInterceptedReadBigEndian(t))
                        {
                            constrained = null;
                            continue;
                        }
                        // `constrained. Int128 call INumberBase<TSelf>::CreateTruncating<TOther>`
                        // (System.Number.ParseBinaryInteger<TInteger> -> Int128.Parse): emit
                        // lowers the whole call inline to the sign/zero-extending widening (the
                        // interface-mediated Create* arm -> TranslateGenericIntrinsic), and the
                        // static-virtual member's DEFAULT interface body branches to
                        // TOther.TryConvertToTruncating — an InternalCall with no IL. So its
                        // body must NOT enter the tree; cut the ordinary Reach(t) edge below,
                        // exactly as the ReadBigEndian cut above does for its DIM. `t` is the
                        // resolved INumberBase<Int128>::CreateTruncating, so match the SAME row
                        // the call-site cut asks (Compilation.Resolve's MethodSpec arm) and the
                        // emit route asks (CreateTargetPrimitiveName) on the CONSTRAINED type's
                        // name + the member name — the constrained cut cannot drift from either.
                        // cut ⟹ route.
                        if (t is not null && insn.OpCode == ILOpCode.Call
                            && constrained is { Kind: TypeKind.Class, Class: { } i128scn }
                            && t.DeclaringClass.IsInterface
                            && CoreIntrinsics.MsInt128CreateConversion.Matches(i128scn.FullName, t.Name))
                        {
                            constrained = null;
                            continue;
                        }
                        if (t is not null)
                        {
                            Reach(t);
                            // A virtual/interface dispatch reaches the actual
                            // override in each allocated reference type, not the
                            // whole dispatchable surface. A callvirt constrained to
                            // a concrete VALUE primitive is not a dispatch at all:
                            // the emit side always devirtualizes it to the
                            // type-specialized op (TryEmitValueConstrained) or fails
                            // loudly — it never boxes and never touches an interface
                            // slot — so it must not cross the slot with every
                            // allocated type (a generic ISpanFormattable helper
                            // closed over int would otherwise drag every boxed
                            // struct's TryFormat body, e.g. Half's float-formatting
                            // cascade, into the tree). A width-preserving integer
                            // canon placeholder is the same story — its group holds
                            // only integers/enums, which the shared body either
                            // devirtualizes or taints back to concrete trials. The
                            // reference primitives (Object/String, including the
                            // reference canon placeholder) keep the cross-product:
                            // those deref to a real dispatch.
                            if ((insn.OpCode == ILOpCode.Callvirt || insn.OpCode == ILOpCode.Ldvirtftn)
                                && (t.IsVirtual || t.DeclaringClass.IsInterface)
                                && !(insn.OpCode == ILOpCode.Callvirt
                                     && constrained is { Kind: TypeKind.Primitive, Primitive: not (PrimitiveTypeCode.Object or PrimitiveTypeCode.String) }))
                                ReachUsedVirtual(t);
                            // A generic virtual method has no vtable slot; its dispatch
                            // goes through a per-instantiation type-switch dispatcher whose
                            // override cases are reached here (see ReachUsedGvm).
                            if ((insn.OpCode == ILOpCode.Callvirt || insn.OpCode == ILOpCode.Ldvirtftn)
                                && IsGvmCall(t))
                                ReachUsedGvm(t);
                        }
                        // Array.Sort<T>(…, IComparer<T>) dispatches T's comparer via a
                        // transpiler-emitted thunk, not an IL callvirt — and Array
                        // is intrinsic-mapped so ResolveCallTarget returns null (above).
                        // Mark IComparer<T>.Compare used so each allocated comparer's
                        // Compare impl is emitted; otherwise the interface slot is nullptr.
                        if (handle.Kind == HandleKind.MethodSpecification)
                            ReachSortComparerCompare(module, (MethodSpecificationHandle)handle, m.Context);
                        // The non-generic counterpart of the sort-comparer reach above: the
                        // Array.Sort/BinarySearch(Array, …) overloads (System.Array is intrinsic-
                        // mapped, so ResolveCallTarget returned null and no callvirt marked the
                        // element order used) and Comparer.Compare / IComparer.Compare (the
                        // body-intercepted Comparer.Compare + its inline route). Mark non-generic
                        // System.IComparable (and IComparer for the comparer overloads) used and lay
                        // down their type-infos.
                        if (insn.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                            && handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
                            ReachNonGenericOrderingReferences(module, handle);
                        // Array.{Index,LastIndex}Of<T> / a MemoryExtensions span scan
                        // compares its elements in an emitted loop — same devirtualized
                        // EqualityComparer<T>.Default the Dictionary key path uses, and
                        // just as invisible to the MemberRef comparer hook below (these
                        // are MethodSpecs of an intrinsic type). Reach T's equality.
                        if (handle.Kind == HandleKind.MethodSpecification)
                            ReachElementScanEquality(module, (MethodSpecificationHandle)handle, m.Context);
                        // A custom (non-Task) awaiter that genuinely suspends —
                        // AwaitOnCompleted/AwaitUnsafeOnCompleted resolves only to
                        // MoveNext above; also reach the awaiter's OnCompleted and the
                        // synthesized System.Action delegate type.
                        if (handle.Kind == HandleKind.MethodSpecification)
                            ReachCustomAwaiterContinuation(module, (MethodSpecificationHandle)handle, m.Context);
                        // ThreadPool.UnsafeQueueUserWorkItem(IThreadPoolWorkItem, bool)
                        // lowers to a pool item calling Execute through the interface
                        // table at run time — no IL callvirt edge, so mark the slot
                        // used here (ThreadPool is intrinsic; ResolveCallTarget
                        // returned null above).
                        if (handle.Kind is HandleKind.MemberReference or HandleKind.MethodDefinition)
                            ReachThreadPoolWorkItemExecute(module, handle, m.Context);
                        // A dispatch through (I)EqualityComparer<T> — the GetHashCode/
                        // Equals a Dictionary/HashSet runs on its key — devirtualizes at
                        // emit to the key's type-specialized op. For a
                        // value-type (struct) key that means calling the struct's own
                        // GetHashCode / IEquatable<T>.Equals override, so reach those here
                        // — gated on the comparer dispatch, the genuine key-use site, so a
                        // struct merely boxed elsewhere doesn't drag its equality in.
                        if (handle.Kind == HandleKind.MemberReference
                            && module.Reader.GetMemberReference((MemberReferenceHandle)handle).Parent.Kind == HandleKind.TypeSpecification)
                        {
                            var cmpParent = module.Reader.GetTypeSpecification(
                                (TypeSpecificationHandle)module.Reader.GetMemberReference((MemberReferenceHandle)handle).Parent)
                                .DecodeSignature(SigProvider, m.Context);
                            if (cmpParent is { Kind: TypeKind.Class, Class: { } cmpCls }
                                && GenericDefFullName(cmpCls) is "System.Collections.Generic.EqualityComparer"
                                    or "System.Collections.Generic.IEqualityComparer"
                                && cmpCls.Context.TypeArgs.Length >= 1)
                                ReachValueKeyEquality(cmpCls.Context.TypeArgs[0]);
                        }
                        // A struct boxed by the immediately-preceding instruction and
                        // passed straight to a formatting call (Console.Write/WriteLine,
                        // String.Concat/Format) is formatted via Object.ToString —
                        // reach its override so dn2cpp_object_tostring's tostring slot
                        // is wired. Gated on the formatting call (not the box) so a
                        // struct boxed for other reasons doesn't drag its ToString in.
                        if (prevBoxed is { } pb
                            && insn.OpCode is ILOpCode.Call or ILOpCode.Callvirt
                            && IsObjectFormattingCall(module, handle)
                            && EffectiveToString(pb) is { } pbts)
                            Reach(pbts);
                        // $"{struct}" lowers to a generic
                        // DefaultInterpolatedStringHandler.AppendFormatted<T>(T) (no
                        // box); when T is a value type overriding ToString the hole is
                        // formatted via the struct's ToString — reach it so the emit
                        // can call it directly.
                        if (handle.Kind == HandleKind.MethodSpecification)
                        {
                            var fms = module.Reader.GetMethodSpecification((MethodSpecificationHandle)handle);
                            // Both interpolated-string handlers — the top-level
                            // DefaultInterpolatedStringHandler and the StringBuilder nested
                            // AppendInterpolatedStringHandler (bare name) —
                            // format a value-type hole via the struct's ToString override.
                            if (MethodSpecParentTypeName(module, fms) is "System.Runtime.CompilerServices.DefaultInterpolatedStringHandler"
                                    or "AppendInterpolatedStringHandler"
                                && MethodSpecMethodName(module, fms) is "AppendFormatted" or "AppendFormattedWithTempSpace"
                                && fms.DecodeSignature(SigProvider, m.Context).ToArray() is [{ Kind: TypeKind.Class, Class: { IsValueType: true } fc }, ..])
                            {
                                if (EffectiveToString(fc) is { } fts)
                                    Reach(fts);
                                // A `$"{struct:fmt}"` hole routes through the struct's
                                // ToString(string) overload when it has one (the emit
                                // can't tell here whether the spec carries a format
                                // component, so reach it whenever present).
                                if (EffectiveToStringFormat(fc) is { } ftsf)
                                    Reach(ftsf);
                            }
                            // string.Join<T>(sep, IEnumerable<T>) / Concat<T>(IEnumerable<T>)
                            // over a concrete collection (SortedSet, Sorted*.Keys/.Values)
                            // emits an interface-enumeration loop, not an IL callvirt — so
                            // the intrinsic GetEnumerator/MoveNext/Current edges are invisible
                            // to ResolveCallTarget above. Reach those interface methods so
                            // each *allocated* IEnumerable<T> collection's enumerator impl is
                            // emitted (use-site gated on the Join/Concat<T> spec).
                            if (MethodSpecParentTypeName(module, fms) == "System.String"
                                && MethodSpecMethodName(module, fms) is "Join" or "Concat"
                                && fms.DecodeSignature(SigProvider, m.Context).ToArray() is [{ } jt]
                                && EnumerationDispatch(jt) is { } ed)
                                ReachEnumeration(ed);
                        }
                        // The string-element non-generic overloads — Join(string,
                        // IEnumerable<string>) / Concat(IEnumerable<string>) — are a
                        // plain MemberReference (not a spec), so the spec branch above
                        // misses them. When such a call's signature carries a generic
                        // (IEnumerable<string>) operand, the emit may lower it to the
                        // same interface-enumeration loop, so reach string enumeration
                        // (gated on the IEnumerable param, not every Concat).
                        if (handle.Kind == HandleKind.MemberReference
                            && MemberRefParentTypeName(module, (MemberReferenceHandle)handle) == "System.String")
                        {
                            var smr = module.Reader.GetMemberReference((MemberReferenceHandle)handle);
                            if (module.Reader.GetString(smr.Name) is "Join" or "Concat"
                                && smr.DecodeMethodSignature(SigProvider, m.Context).ParameterTypes
                                       .Any(p => p is { Kind: TypeKind.Class, Class.GenericArity: > 0 })
                                && EnumerationDispatch(TypeDesc.MakePrimitive(PrimitiveTypeCode.String)) is { } sed)
                                ReachEnumeration(sed);
                        }
                        // Task.WhenAll/WhenAny over an IEnumerable<Task<T>> (List, a
                        // LINQ result, …) emits an inline interface-enumeration loop to
                        // materialize the inputs. Like the Join path this is
                        // invisible to ResolveCallTarget (Task is intrinsic-mapped, so
                        // the edge above is null) — reach the element task's enumeration
                        // so each allocated source collection's enumerator impl is
                        // emitted (gated on the combinator call's IEnumerable operand).
                        if (insn.OpCode == ILOpCode.Call
                            && TaskCombinatorEnumerableElement(module, handle, m.Context) is { } telem
                            && EnumerationDispatch(telem) is { } ted)
                            ReachEnumeration(ted);
                        // A program touching CancellationTokenSource / CancellationToken
                        // can produce a CANCELED task / ThrowIfCancellationRequested,
                        // which throws an OperationCanceledException built in the runtime
                        // — force-reach that type so it is catchable by a typed clause.
                        if (CallTargetTypeName(module, handle) is "System.Threading.CancellationTokenSource"
                            or "System.Threading.CancellationToken")
                            ReachCancellationException();
                        // TaskCompletionSource(<T>).SetCanceled/TrySetCanceled builds the
                        // same runtime OperationCanceledException — same force-reach. The
                        // generic TCS<T> is a TypeSpec MemberRef (invisible to
                        // CallTargetTypeName), so key on the cheap member-name check first
                        // and decode the parent only then.
                        if (handle.Kind == HandleKind.MemberReference
                            && module.Reader.GetMemberReference((MemberReferenceHandle)handle) is var tcsmr
                            && module.Reader.GetString(tcsmr.Name) is "SetCanceled" or "TrySetCanceled"
                            && (MemberRefParentTypeName(module, (MemberReferenceHandle)handle)
                                    == "System.Threading.Tasks.TaskCompletionSource"
                                || (tcsmr.Parent.Kind == HandleKind.TypeSpecification
                                    && module.Reader.GetTypeSpecification((TypeSpecificationHandle)tcsmr.Parent)
                                           .DecodeSignature(SigProvider, m.Context)
                                        is { Kind: TypeKind.Class, Class: { } tcscls }
                                    && GenericDefFullName(tcscls) == "System.Threading.Tasks.TaskCompletionSource")))
                            ReachCancellationException();
                        // `new ValueTask(source, token)` initialized IN PLACE lowers to
                        // ldloca + `call .ctor`, not newobj — the same source-backed
                        // bridge, dispatching GetResult/OnCompleted through the source's
                        // interface table, and just as invisible to ResolveCallTarget
                        // (ValueTask is intrinsic). Reach the same slots the newobj form
                        // does; the bridge no-ops on every other ctor shape and on every
                        // other type, so this only pays a name compare per call site.
                        if (insn.OpCode == ILOpCode.Call && IsCtorToken(module, handle))
                            ReachValueTaskSourceBridge(module, handle, m.Context);
                        // A constrained call/callvirt on a value type whose target is an
                        // interface or Object/ValueType virtual emits a direct call to the
                        // struct's implementation — reach it so it is transpiled.
                        if (insn.OpCode == ILOpCode.Callvirt && constrained is { } cn)
                            ReachConstrainedImpl(module, handle, cn, m.Context);
                        // The `call` (not callvirt) analogue: a static-abstract interface
                        // member invoked via `constrained. call` (generic math / Linq's
                        // IMinMaxCalc comparers / class-typed operator implementors)
                        // devirtualizes to the constrained type's static impl, which must
                        // therefore be transpiled. `t` is the resolved (substituted)
                        // interface method, so match its SigKey against the type's static
                        // methods. Mirrors the emit-side admission: value types plus
                        // placeholder-free reference classes — interfaces included, since
                        // an interface type argument carries its own explicit static
                        // impls (a no-impl interface resolves to null and reaches nothing).
                        if (insn.OpCode == ILOpCode.Call
                            && constrained is { Kind: TypeKind.Class, Class: { } scn }
                            && t is { IsStatic: true } && t.DeclaringClass.IsInterface)
                        {
                            // A generic struct comparer (e.g. Linq's MinCalc<T>) arrives
                            // unspecialized from ResolveTypeTokenForScan, so its IsValueType
                            // flag (set only by CompleteShape's ValueType-base scan)
                            // is still false — complete it first. Restricted to generic
                            // specializations: a non-generic struct (e.g. Int128) is already
                            // field-populated + flagged by pass 2, and re-completing it would
                            // duplicate its fields.
                            if (scn.Context.TypeArgs.Length > 0)
                                EnsureCompleted(scn);
                            if (scn.IsValueType || !ContainsCanonPlaceholder(scn))
                                ReachStaticVirtualImpl(scn, t);
                        }
                        // The integer-primitive twin of the arm above, mirroring the
                        // emit-side admission (ConstrainedStaticVirtualSelf): a
                        // constrained TSelf closed to one of the eight integer widths
                        // resolves against its CoreLib struct. Reach() itself cuts an
                        // impl on an intrinsic type (the word-size structs — emit routes
                        // those through the intrinsic tables), and the sub-word
                        // inline-lowered member set (Parse/TryParse and the format
                        // family) is excluded here exactly as emit excludes it from the
                        // direct call — so only the members emit direct-calls (the
                        // sub-word types' plain-named real bodies: Clamp, Max, …) enter
                        // the tree. Emit's remaining fallback, the member's default
                        // interface body, is reached by the ordinary Reach(t) edge above.
                        if (insn.OpCode == ILOpCode.Call
                            && constrained is { Kind: TypeKind.Primitive, Primitive: var cpc, IsCanonPlaceholder: false }
                            && t is { IsStatic: true } && t.DeclaringClass.IsInterface
                            && CoreIntrinsics.PrimitiveIntegerFullName(cpc) is { } cpn
                            && FindClassByFullName(cpn) is { } pcls
                            && ResolveStaticVirtualImpl(pcls, t) is { } pimpl
                            && !CoreIntrinsics.IsInlineLoweredPrimitiveMember(pimpl.DeclaringClass.FullName, pimpl.Name))
                            Reach(pimpl);
                        constrained = null;
                        continue; // edge handled; next instruction
                    }
                    case ILOpCode.Newobj:
                    {
                        var ctor = ResolveCallTarget(module, handle, m.Context);
                        if (ctor is not null)
                        {
                            // An exception newobj is intercepted at EMIT time so the base
                            // Dn2CppExceptionObject prefix (type/message/inner) is seeded
                            // by the transpiler, not by the real Exception(string) ctor.
                            // For System.Exception itself and runtime-raised BCL exceptions
                            // that leaves the transpiled ctor body unused; for a user-defined
                            // derived exception the emit-side path DirectCalls this body after
                            // seeding the prefix, so its `Result = code;` writes reach the
                            // derived struct. Reaching it either way keeps the exception
                            // type-info + base chain + GetType path emitting unchanged.
                            Reach(ctor);
                            ReachAllocatedType(ctor.DeclaringClass);
                        }
                        else
                            // The IValueTaskSource-backed ValueTask ctor (intrinsic
                            // ValueTask, so ResolveCallTarget returned null): its
                            // emit dispatches GetResult/OnCompleted through the
                            // source's interface table at run time — reach those
                            // slots + the continuation delegate type here.
                            ReachValueTaskSourceBridge(module, handle, m.Context);
                        continue;
                    }
                    case ILOpCode.Box:
                    {
                        // Boxing a value type allocates its boxed form on the heap;
                        // the box can then be dispatched through an interface
                        // (e.g. a List<T>.Enumerator struct boxed as IEnumerator<T>
                        // by foreach over IEnumerable<T>). Mark it allocated so its
                        // used interface/virtual slots are reached and its interface
                        // dispatch table is emitted — a value type is otherwise
                        // dispatched only via direct constrained calls.
                        if (ResolveTypeTokenForScan(module, handle, m.Context)
                            is { Kind: TypeKind.Class, Class: { IsValueType: true } bc })
                        {
                            ReachAllocatedType(bc);
                            // A boxed struct formatted as a unit (Console.WriteLine /
                            // "s" + tuple) dispatches its ToString through
                            // dn2cpp_object_tostring's tostring slot — but boxing
                            // alone doesn't mean formatting (a struct is also boxed for
                            // Equals/interface dispatch/storage). Remember it; the
                            // ToString is reached only if the *next* instruction is a
                            // formatting call (below). Primitives are excluded — the
                            // runtime formats boxed primitives directly, and their real
                            // ToString pulls culture/Calli.
                            if (!IsRuntimeFormattedPrimitive(bc))
                                boxedForFormat = bc;
                        }
                        continue;
                    }
                    case ILOpCode.Ldsfld:
                    case ILOpCode.Ldsflda:
                    case ILOpCode.Stsfld:
                    {
                        // A static field access on a type that is never allocated
                        // still needs the declaring type emitted and its static
                        // initializer run — e.g. List<T>'s empty-list fast path
                        // returns SZGenericArrayEnumerator<T>.Empty, a static field
                        // on a type reached only here. Without this the field/type
                        // C++ symbol is undeclared (it never enters the
                        // signature-driven emit set) and the static stays
                        // zero-inited. Reaching the .cctor declares a reachable
                        // method on the type (so it is emitted) and, if the .cctor
                        // allocates the type, pulls its layout + used virtual slots.
                        // Intrinsic/bounded declaring types are cut by ReachCctor ->
                        // Reach, so this does not drag their initializers in.
                        if (ResolveStaticFieldClass(module, handle, m.Context) is { } fc)
                            ReachCctor(fc);
                        continue;
                    }
                }

                switch (handle.Kind)
                {
                    case HandleKind.TypeSpecification:
                    {
                        var ts = reader.GetTypeSpecification((TypeSpecificationHandle)handle).DecodeSignature(SigProvider, m.Context);
                        // --trim-godot-classes release trigger: a generic type
                        // token's arguments (initobj/ldflda/… over
                        // Godot.Collections.Array<Sprite2D>) name engine wrappers.
                        if (trimUserSite)
                            TrimNoteNamedType(ts);
                        break;
                    }
                    case HandleKind.MemberReference:
                    {
                        var mr = reader.GetMemberReference((MemberReferenceHandle)handle);
                        if (mr.Parent.Kind == HandleKind.TypeSpecification)
                        {
                            var mrp = reader.GetTypeSpecification((TypeSpecificationHandle)mr.Parent).DecodeSignature(SigProvider, m.Context);
                            if (trimUserSite)   // as above: the member's declaring type arguments
                                TrimNoteNamedType(mrp);
                        }
                        break;
                    }
                    case HandleKind.MethodSpecification:
                        ResolveMethodSpec(module, (MethodSpecificationHandle)handle, m.Context);
                        if (trimUserSite)   // as above: the instantiation's arguments
                            TrimNoteCallSite(module, handle, m.Context);
                        break;
                }
            }
        }
        // Filtered, like every other broad catch (see Compilation.IsMustEscape): this arm
        // does not swallow, but it RE-WRAPS — and a re-wrap is a downgrade. An
        // InstantiationBoundException IS a NotSupportedException, so an unfiltered catch
        // here rethrows the bound as a plain one, and every `when (!IsMustEscape(e))`
        // upstream then reads it as an ordinary unsupported-shape report: the mode that
        // records those as gap rows and keeps draining would carry on feeding the very
        // overrun the bound exists to stop. It would also decorate twice — the bound's own
        // message already carries InstantiationDriver's `[chain: …]`.
        catch (NotSupportedException ex) when (!IsMustEscape(ex))
        {
            var chain = new List<string>();
            for (var p = m; p is not null; _predTrace.TryGetValue(p, out p))
                chain.Add(p.DeclaringClass.FullName + "." + p.Name);
            throw new NotSupportedException($"{m.DeclaringClass.FullName}.{m.Name}: {ex.Message} [chain: {string.Join(" <- ", chain)}]");
        }
    }
}

/// <summary>The reachable-method set, plus the append-only log of its admissions.
///
/// <para>The log is what lets a consumer act on what is new since it last looked
/// instead of re-deriving the whole answer: <c>CppEmitter.CompileReachableBodies</c>
/// drains it by cursor each round rather than re-walking every class. It is a wrapper
/// around the set rather than a second field beside it precisely so an admission
/// cannot land in one and not the other — <see cref="Add"/> is the only way in, and a
/// new reach path gets logged whether or not its author knew the log existed.</para>
///
/// <para>A log entry records that the method was admitted at that point, not that it
/// is a member now: <see cref="Remove"/> exists (<c>FinalizeSharedGenerics</c> evicts
/// the canonical methods no real user binds to, and <c>Compilation.Reach</c>'s
/// scan-less re-admission path evicts before re-adding). So a cursor consumer must
/// re-test <see cref="Contains"/> before acting — and may then forget the entry, since
/// a re-admission appends a fresh one.</para></summary>
internal sealed class ReachableSet : IEnumerable<MethodInfo>
{
    private readonly HashSet<MethodInfo> _set = new();
    private readonly List<MethodInfo> _order = new();

    /// <summary>Admissions in order, including any since removed (see the remarks).</summary>
    internal IReadOnlyList<MethodInfo> Order => _order;

    internal bool Add(MethodInfo m)
    {
        if (!_set.Add(m))
            return false;
        _order.Add(m);
        return true;
    }

    internal bool Contains(MethodInfo m) => _set.Contains(m);

    internal bool Remove(MethodInfo m) => _set.Remove(m);

    internal int Count => _set.Count;

    /// <summary>The set's own struct enumerator, so a <c>foreach</c> over this binds to it
    /// rather than to the boxing <see cref="IEnumerable{T}"/> one the LINQ surface needs.</summary>
    public HashSet<MethodInfo>.Enumerator GetEnumerator() => _set.GetEnumerator();

    IEnumerator<MethodInfo> IEnumerable<MethodInfo>.GetEnumerator() => _set.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();
}
