namespace DnHttp;

/// <summary>
/// dn2cpp transpile-time transport swap for <c>System.Net.Http</c>: the managed body an
/// intercepted <c>SocketsHttpHandler.Send</c> / <c>SendAsync</c> forwards to (see
/// <c>CoreIntrinsics.IsInterceptedHttpHandlerMethod</c> and
/// <c>MethodCompiler.CompileHttpShimBody</c>). The real handler's transport IL — the
/// SetupHandlerChain → connection-pool → socket/DNS/TLS/QUIC subtree — is cut, and
/// <see cref="Http2StreamBackend"/> takes its place: it reads the request through the
/// ordinary <see cref="HttpRequestMessage"/> surface (transpiled as normal BCL IL), runs
/// it over the native libcurl transport, and rebuilds an
/// <see cref="HttpResponseMessage"/> the caller reads back through the same surface.
/// Under a normal .NET runtime this type is unreached dead code — the real handler serves
/// the request.
///
/// <para>This type is the two named entry points and nothing else; the transport itself is
/// ONE code path, in <see cref="Http2StreamBackend"/>. The split it used to carry — a
/// buffered one-shot for known-length requests, the streaming family for the rest — keyed
/// on the REQUEST's shape and so decided the RESPONSE's semantics by accident: a bodyless
/// GET came back only once the whole body had arrived, where a real handler returns at the
/// header block, and its cancel had to wait out curl's progress-callback interval because
/// <c>curl_easy_perform</c> hides the multi handle a wakeup needs. The request's two shapes
/// survive — they are a real difference in what curl can put on the wire (a Content-Length
/// and a rewindable body, versus a chunked/DATA framing that a redirect cannot re-send) —
/// but they are now an argument to one open, not two transports.</para>
///
/// <para>The handler instance is passed, not dropped — it is the only carrier of
/// <c>SocketsHttpHandler</c>'s connection-pool settings, and a transport that never sees it
/// can only ignore them. All four cross per request: <c>PooledConnectionLifetime</c>
/// and <c>PooledConnectionIdleTimeout</c> through the curl options consulted when a
/// connection is picked out of the transport's one process-wide multi;
/// <c>MaxConnectionsPerServer</c> as curl's cap on CONCURRENT connections per host
/// (<c>CURLMOPT_MAX_HOST_CONNECTIONS</c> — transfers past it queue, as real .NET queues
/// past the cap; the one approximation is scope, per process rather than per handler,
/// stated in the transport TU's header); and <c>EnableMultipleHttp2Connections=false</c>
/// (the default) as <c>CURLOPT_PIPEWAIT</c> on every h2-capable request — concurrent
/// calls to one server MULTIPLEX one connection instead of opening one each, which the
/// single shared multi is what makes possible at all.</para>
///
/// <para>Four request/response properties are the CONTRACT grpc-dotnet's unary path stands
/// on, and each is asserted by the grpc gate; drop one and gRPC fails at run time, not at
/// transpile: (1) <c>req.Version</c> + <c>req.VersionPolicy</c> travel to the native side,
/// which owns version negotiation (gRPC sends 2.0/RequestVersionExact); (2)
/// <c>msg.Version</c> is rebuilt from the NEGOTIATED protocol — grpc-dotnet reads it to
/// reject a downgraded response, and the default (1.1) rejects every h2 response; (3)
/// <c>msg.TrailingHeaders</c> carries the response trailers, separately from the headers —
/// <c>grpc-status</c> lives there; (4) a canceled token releases the awaiting caller
/// promptly AND aborts the native transfer — gRPC deadlines ride this token.</para>
/// </summary>
internal static class DnHttpBackend
{
    /// <summary>The synchronous transport. A transport-level failure (bad URL, DNS/TCP
    /// refused, timeout) throws <see cref="HttpRequestException"/> — the shape HttpClient
    /// reports before <c>EnsureSuccessStatusCode()</c>; an HTTP response, including
    /// 4xx/5xx, comes back as a <see cref="HttpResponseMessage"/> with that status.</summary>
    internal static HttpResponseMessage Send(
        SocketsHttpHandler handler, HttpRequestMessage req, CancellationToken ct)
        => Http2StreamBackend.Send(handler, req, ct);

    /// <summary>The asynchronous transport.</summary>
    internal static Task<HttpResponseMessage> SendAsync(
        SocketsHttpHandler handler, HttpRequestMessage req, CancellationToken ct)
        => Http2StreamBackend.SendAsync(handler, req, ct);
}
