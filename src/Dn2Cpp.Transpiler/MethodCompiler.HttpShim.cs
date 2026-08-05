namespace Dn2Cpp;

/// <summary>Synthesizes the bodies of the System.Net.Http methods whose real IL reachability
/// cuts (see CoreIntrinsics.IsBodyReplacedHttpMethod). Each is emitted under the method's own
/// name and signature, so a virtual slot (CppEmitter.RenderVtable) points at a real body and
/// the abstract HttpMessageHandler dispatch lands here.
///
/// <para>The three intercepts answer in three different ways on purpose, because a cut with
/// no substitute is where a quiet body would be a lie:
/// <list type="bullet">
/// <item>The <b>transport</b> overrides (SocketsHttpHandler.Send/SendAsync/Dispose)
/// SUBSTITUTE: the connection-pool/socket/DNS/TLS subtree is gone and the DnHttp shim does
/// the work instead.</item>
/// <item>The <b>client-certificate</b> accessors REFUSE: the handshake runs inside
/// libcurl/Mbed TLS rather than .NET's SslStream, so nothing here can present a CLIENT
/// certificate. The setter is the refusal; the getter answers the one value the setter
/// accepts (see <see cref="EmitClientCertificateOptionsGetter"/>).</item>
/// <item>The <b>system-proxy</b> members (MacProxy's IWebProxy pair; HttpWindowsProxy's
/// ctor and Dispose plus the same pair) DEGRADE: "no proxy" is both what
/// System.Net.Http's own HttpNoProxy answers and truthful of a transport that is not wired
/// to one (see <see cref="EmitSystemProxyNoProxy"/>).</item>
/// </list></para></summary>
internal sealed partial class MethodCompiler
{
    /// <summary>Compiles the synthesized body for one intercepted System.Net.Http method,
    /// dispatched by re-asking the same pure predicates the cut and the route asked — never by
    /// a separate list, which is what would let the two drift. Throws (naming the remedy — see
    /// <see cref="MissingHttpShimReason"/>) when Send/SendAsync are reached but the DnHttp shim
    /// assembly is not in the load set: the cut has already deleted the real body, so there is
    /// nothing to fall back to.</summary>
    internal string CompileHttpShimBody()
    {
        if (CoreIntrinsics.IsInterceptedHttpCertOptionMethod(Method.DeclaringClass.FullName, Method.Name))
        {
            if (Method.Name == "get_ClientCertificateOptions")
            {
                EmitClientCertificateOptionsGetter();
            }
            else
            {
                EmitClientCertificateOptionsGuard();
            }
        }
        else if (CoreIntrinsics.IsInterceptedSystemProxyMethod(Method.DeclaringClass.FullName, Method.Name))
        {
            EmitSystemProxyNoProxy();
        }
        else if (CoreIntrinsics.IsInterceptedGrpcHandlerTypeGetter(Method.DeclaringClass.FullName, Method.Name))
        {
            EmitGrpcHandlerTypeCustom();
        }
        else if (Method.Name is "Send" or "SendAsync")
        {
            var shim = Comp.HttpShimTarget(Method.Name)
                ?? throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: {MissingHttpShimReason()}");
            // a0 = this, a1 = HttpRequestMessage, a2 = CancellationToken. All three flow to
            // the shim at their own CLR types (same instantiation on both sides), so no
            // erased-pointer cast is due. a0 is passed, not dropped: the handler instance is
            // the ONLY carrier of SocketsHttpHandler's connection-pool settings.
            Emit($"return {DirectCallSym(shim)}({ArgsWithRgctx("a0, a1, a2", shim)});");
        }
        // SocketsHttpHandler.Dispose(bool): empty body — void, no return.
        return RenderWrapperBody("dn2cpp HTTP shim");
    }

    /// <summary>Why the DnHttp transport shim is not there, worded per cause so the reader is
    /// sent at something they can act on. DnHttp is a <b>conditional default reference</b>
    /// (<c>Compilation.InjectDefaultRefs</c>) loaded from beside the CLI whenever
    /// <c>System.Net.Http</c> is in the load set — which, since this body exists only because
    /// an intercepted <c>SocketsHttpHandler</c> method was reached, is always. So the
    /// remedies differ per verdict (<c>Compilation.DefaultRefStatusOf</c>): a flag to drop
    /// (<c>Suppressed</c>), an install to repair (<c>NotFound</c>), the wrong or stale
    /// assembly behind a <c>-r</c> that did take (<c>AlreadyLoaded</c>). Only
    /// <c>TriggerAbsent</c> — the injection pass never ran (<c>--emit-patch</c>, or a direct
    /// embedder of the transpiler library) — is genuinely a missing manual <c>-r</c>.
    /// </summary>
    private string MissingHttpShimReason() =>
        Comp.DefaultRefStatusOf("DnHttp") switch
        {
            Compilation.DefaultRefOutcome.Suppressed =>
                "the DnHttp transport shim (DnHttp.DnHttpBackend) was declined by "
                + "`--no-default-ref DnHttp`, and the real transport IL is cut whether or not the "
                + "shim replaces it — so the intercepted HTTP handler has no body to forward to. "
                + "Drop that flag to take the shipped shim, or pass your own with `-r DnHttp.dll`",
            Compilation.DefaultRefOutcome.NotFound =>
                "the DnHttp transport shim (DnHttp.DnHttpBackend) ships beside the dn2cpp "
                + "executable and is not there, so this installation is incomplete — reinstall the "
                + "NuGet tool or the toolchain bundle, or point at the assembly explicitly with "
                + "`-r DnHttp.dll`",
            Compilation.DefaultRefOutcome.AlreadyLoaded =>
                "an assembly named DnHttp is loaded but does not define DnHttp.DnHttpBackend, so "
                + "the intercepted HTTP handler has no body to forward to — the `-r` took effect "
                + "and pointed at the wrong DnHttp (somebody else's, or one built before the "
                + "transport shim existed). Point it at this toolchain's copy, or drop the `-r` "
                + "and take the one shipped beside the CLI",
            _ =>
                "the DnHttp transport shim (DnHttp.DnHttpBackend) is not referenced — pass "
                + "`-r DnHttp.dll` so the intercepted HTTP handler has a body to forward to",
        };

    /// <summary>The body of HttpClientHandler.set_ClientCertificateOptions: accept the default
    /// (Manual) silently, refuse everything else with a catchable PlatformNotSupportedException.
    ///
    /// <para>The test is at RUN time because the intercept replaces the callee, not the call
    /// site: HttpClientHandler..ctor's Manual and a caller's Automatic both land in this ONE
    /// body, so no static test could tell them apart.</para>
    ///
    /// <para>Manual is a no-op: the real setter's callback install is dead (a
    /// LocalCertificateSelectionCallback is consulted only by .NET's own SslStream handshake,
    /// and this build's TLS runs inside the vendored libcurl/Mbed TLS transport), and its
    /// forward to <c>_underlyingHandler.ClientCertificateOptions</c> is DROPPED here rather
    /// than replayed, which would bake a three-hop internal BCL field chain into a synthesized
    /// body. That field initializes to Automatic, so the getter must be intercepted too
    /// (<see cref="EmitClientCertificateOptionsGetter"/>).</para>
    ///
    /// <para>Automatic throws rather than returning quietly: it asks for a certificate to be
    /// selected out of the system store — precisely the subtree the cut deleted — and no
    /// client certificate can be presented whatever the caller asked for, so a quiet return
    /// would report success for work that did not happen and leave a server rejection naming
    /// nothing. A value that is neither (real .NET: ArgumentOutOfRangeException) takes the
    /// same arm; both are unsupportable here and both are loud.</para></summary>
    private void EmitClientCertificateOptionsGuard()
    {
        // Cut by NAME, lower by SHAPE: the predicate matched a name, so the route satisfies
        // itself that what it got is what it models before emitting code that reads a1 as an
        // int32-backed enum. An unmodeled shape throws — it does not fall through to a guard
        // compiled against the wrong operand.
        if (Method.IsStatic
            || Method.Signature.ParameterTypes is not
                [{ Kind: TypeKind.Class, Class: { IsEnum: true, FullName: "System.Net.Http.ClientCertificateOption" } }])
        {
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: dn2cpp models this as an instance "
                + "setter taking one System.Net.Http.ClientCertificateOption, and it no longer is. The "
                + "client-certificate cut must be re-checked against the current System.Net.Http before "
                + "this body can stand in for it.");
        }
        // a0 = this (unused), a1 = value. An int32-backed enum is an int32_t (CppTypes.Of) and
        // ClientCertificateOption.Manual is 0 — the assert above covers only the type, but a
        // renumbering makes HttpClientHandler..ctor's own Manual throw on the first
        // `new HttpClient()`, which is loud.
        Emit("if (a1 != 0) {  // != ClientCertificateOption.Manual");
        Emit("    dn2cpp_throw_platform_not_supported("
            + "\"HttpClientHandler.ClientCertificateOptions: only ClientCertificateOption.Manual (the "
            + "default) is supported. Ordinary https:// works — this build's TLS runs in the vendored "
            + "libcurl/Mbed TLS transport, which verifies the SERVER against a CA bundle compiled into "
            + "the binary — but that handshake happens outside .NET, so the client-certificate "
            + "selection callback this setter installs is never consulted, and the X509Store/keychain/"
            + "ASN.1 subtree behind it is cut from the binary. No CLIENT certificate can be presented, "
            + "so Automatic would be ignored rather than honoured, and it throws instead of failing "
            + "quietly. Leave the property at its default, catching this exception if no client "
            + "certificate is actually needed; an endpoint that REQUIRES one (mutual TLS) cannot be "
            + "reached by this build.\");");
        Emit("}");
    }

    /// <summary>The body of HttpClientHandler.get_ClientCertificateOptions: answer Manual, as
    /// a constant.
    ///
    /// <para>The constant IS the stored value, not an approximation: the property has one
    /// observable state here, since every accepted set is Manual and every other value throws
    /// in the guard body — including after a refused Automatic, where real .NET's storage
    /// would also still hold Manual. Asserted by gates/build-and-run-http-get.sh section 6.
    /// </para>
    ///
    /// <para>The real getter would not do: it forwards to a settings field that initializes
    /// to Automatic and is corrected only by the setter forward the guard body drops, so left
    /// alone it answers the .NET default rather than the accepted value. Replaying that
    /// forward instead would tie a synthesized body to three internal BCL field names; the
    /// constant ties it only to the enum value the guard body already bakes in.</para>
    /// </summary>
    private void EmitClientCertificateOptionsGetter()
    {
        // Cut by NAME, lower by SHAPE — the getter twin of the guard's check below: before
        // emitting a body that answers an int32-backed enum, be sure that is still what the
        // property accessor is.
        if (Method.IsStatic
            || Method.Signature.ParameterTypes is not []
            || Method.Signature.ReturnType is not
                { Kind: TypeKind.Class, Class: { IsEnum: true, FullName: "System.Net.Http.ClientCertificateOption" } })
        {
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: dn2cpp models this as an instance "
                + "getter returning System.Net.Http.ClientCertificateOption, and it no longer is. The "
                + "client-certificate cut must be re-checked against the current System.Net.Http before "
                + "this body can stand in for it.");
        }
        // a0 = this (unused). 0 == ClientCertificateOption.Manual, the same value the guard
        // body's `a1 != 0` test accepts — renumber the enum and both bodies go wrong together,
        // loudly (see the guard's comment).
        Emit("return 0;  // ClientCertificateOption.Manual — the only value the setter accepts");
    }

    /// <summary>The body of Grpc.Net.Client's <c>GrpcChannel.HttpHandlerType</c> getter:
    /// answer <c>HttpHandlerType.Custom</c>. Why that is the truthful answer rather than a
    /// workaround is argued at
    /// <see cref="CoreIntrinsics.IsInterceptedGrpcHandlerTypeGetter"/>.
    ///
    /// <para>Cut by name, lower by shape: the assert below names the enum by full name, so a
    /// grpc version that renames or renumbers it fails LOUDLY on the first transpile instead
    /// of steering the transport by a stale constant. The value is read from the enum's own
    /// metadata for the same reason.</para></summary>
    private void EmitGrpcHandlerTypeCustom()
    {
        if (Method.IsStatic
            || Method.Signature.ParameterTypes is not []
            || Method.Signature.ReturnType is not
                { Kind: TypeKind.Class, Class: { IsEnum: true, FullName: "Grpc.Net.Client.HttpHandlerType" } retEnum })
        {
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: dn2cpp models this as an instance "
                + "getter returning Grpc.Net.Client.HttpHandlerType, and it no longer is. The "
                + "grpc transport-kind correction must be re-checked against the current "
                + "Grpc.Net.Client before this body can stand in for it.");
        }
        Comp.EnsureCompleted(retEnum);
        var members = EnumMembersModel(retEnum);
        long custom = members.FirstOrDefault(m => m.name == "Custom") is { name: "Custom" } hit
            ? hit.value
            : throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: Grpc.Net.Client.HttpHandlerType has "
                + "no Custom member — the transport-kind correction has nothing truthful to answer");
        Emit($"return {custom};  // HttpHandlerType.Custom — this handler is not a sockets setup");
    }

    /// <summary>The bodies of the intercepted system-proxy members
    /// (<see cref="CoreIntrinsics.IsInterceptedSystemProxyMethod"/>): the GetProxy(Uri) /
    /// IsBypassed(Uri) queries answer "no proxy for this URI", which is what
    /// System.Net.Http's own <c>HttpNoProxy</c> answers — null and true respectively —
    /// and HttpWindowsProxy's ctor and Dispose become empty bodies (the
    /// WinHttp session, interface walk and registry watch the ctor performed fed only the
    /// replaced queries, which read no instance state — so there is also nothing for
    /// Dispose to release). Why a degrade rather than a refusal, and why only the
    /// platform-P/Invoke half of the proxy stack is replaced, is argued at the
    /// predicate.
    ///
    /// <para>One builder for all, because they are one answer: a GetProxy returning null
    /// while IsBypassed returns false is a pair no IWebProxy in the BCL produces, and two
    /// emitters is how the pair drifts apart. Cut by NAME, lower by SHAPE.</para></summary>
    private void EmitSystemProxyNoProxy()
    {
        if (Method.Name is ".ctor" or "Dispose")
        {
            bool ctor = Method.Name == ".ctor";
            if (Method.IsStatic
                || (ctor
                    ? Method.Signature.ParameterTypes is not
                        [{ Kind: TypeKind.Class, Class.FullName: "System.Net.Http.WinInetProxyHelper" }]
                    : Method.Signature.ParameterTypes is not [])
                || Method.Signature.ReturnType is not
                    { Kind: TypeKind.Primitive, Primitive: System.Reflection.Metadata.PrimitiveTypeCode.Void })
            {
                throw new NotSupportedException(
                    $"{Method.DeclaringClass.FullName}.{Method.Name}: dn2cpp models this as the "
                    + "instance member it is — .ctor(WinInetProxyHelper), Dispose() — and it no "
                    + "longer is. The system-proxy cut must be re-checked against the current "
                    + "System.Net.Http before this body can stand in for it.");
            }
            // a0 = this (unused); the ctor's a1 (a WinInetProxyHelper — null at the one call
            // site, ConstructSystemProxy's ldnull) too. Empty bodies: the ctor fills no field
            // the replaced queries read, and Dispose has nothing the empty ctor acquired.
            return;
        }
        bool bypass = Method.Name == "IsBypassed";
        if (Method.IsStatic
            || Method.Signature.ParameterTypes is not [{ Kind: TypeKind.Class, Class.FullName: "System.Uri" }]
            || (bypass
                ? Method.Signature.ReturnType is not
                    { Kind: TypeKind.Primitive, Primitive: System.Reflection.Metadata.PrimitiveTypeCode.Boolean }
                : Method.Signature.ReturnType is not { Kind: TypeKind.Class, Class.FullName: "System.Uri" }))
        {
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: dn2cpp models this as the instance "
                + "IWebProxy query it is — GetProxy(Uri) -> Uri, IsBypassed(Uri) -> bool — and it no "
                + "longer is. The system-proxy cut must be re-checked against the current "
                + "System.Net.Http before this body can stand in for it.");
        }
        // a0 = this (unused), a1 = the target Uri (unused — the answer does not depend on it,
        // exactly as HttpNoProxy's does not).
        Emit(bypass
            ? "return 1;  // HttpNoProxy.IsBypassed — every host bypasses a proxy that is not there"
            : "return nullptr;  // HttpNoProxy.GetProxy — no proxy for any URI");
    }
}
