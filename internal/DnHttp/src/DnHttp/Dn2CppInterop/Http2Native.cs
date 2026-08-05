using System.Runtime.InteropServices;

namespace DnHttp.Dn2CppInterop;

/// <summary>
/// The managed P/Invoke surface of dn2cpp's libcurl-backed HTTP transport — the
/// streaming call family (<c>runtime/core/intrinsics/dn2cpp_http2_stream.cpp</c>),
/// the route EVERY request takes. Each entry point is a plain unmangled C symbol
/// in the <c>dn2cpp_http</c> module, which the transpiler treats as a
/// runtime-provided module (<c>Compilation.IsRuntimeProvidedPInvokeModule</c>), so
/// these <c>[DllImport]</c>s direct-link to the native symbols — the same posture
/// as DnZlib's <c>CompressionNative_*</c> surface. The declared shapes are exactly
/// the ones dn2cpp's P/Invoke marshalling emits: <c>string -&gt; char*</c> (UTF-8),
/// <c>string[] -&gt; char**</c>, <c>byte[] -&gt; uint8_t*</c>, and <c>IntPtr</c>
/// for the opaque <c>Dn2CppHttp2Call*</c> handle and the borrowed <c>char*</c> the
/// accessors hand back (decoded by the caller — the handle owns those buffers and
/// frees them). One handle per call; the full contract (blocking semantics,
/// EOS/trailer ordering, abort, ownership) is documented on the declarations in
/// <c>runtime/core/dn2cpp_core.h</c>. The blocking entry points
/// (<see cref="dn2cpp_http2_call_wait_headers"/>, <see cref="dn2cpp_http2_call_read"/>)
/// are only ever invoked from Task.Run pool workers (see <see cref="Http2StreamBackend"/>),
/// never from the cooperative scheduler thread. Under a normal .NET runtime this type
/// is never reached — real <c>SocketsHttpHandler</c> serves the request — so the
/// missing native library is never loaded.
/// </summary>
internal static class Http2Native
{
    private const string Module = "dn2cpp_http";

    /// <summary>Open a call and start its transfer. Never null, never throws; a
    /// refused/failed open is a handle whose
    /// <see cref="dn2cpp_http2_call_wait_headers"/> answers 0 with
    /// <see cref="dn2cpp_http2_call_error"/> naming why. The caller owns the handle
    /// and must <see cref="dn2cpp_http2_call_free"/> it, serialized against every
    /// in-flight call on the same handle (see <c>Http2StreamBackend.Http2Call</c>).
    /// <paramref name="bodyMode"/> is <see cref="BodyNone"/> /
    /// <see cref="BodyComplete"/> / <see cref="BodyIncremental"/>; only
    /// <see cref="BodyComplete"/> reads <paramref name="body"/>, and the other two
    /// still pass a non-empty array because an empty <c>byte[]</c> does not reliably
    /// marshal to a valid pointer.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr dn2cpp_http2_call_open(
        string method, string url, string[] headers, int headerCount,
        int bodyMode, byte[] body, int bodyLen, int httpVersion, int versionPolicy,
        int maxIdleSeconds, int maxLifetimeSeconds,
        int maxConnsPerServer, int multipleH2);

    /// <summary>No request body.</summary>
    internal const int BodyNone = 0;

    /// <summary>The whole body is handed over at open: a Content-Length on the wire,
    /// and rewindable, so a redirect can re-send it.</summary>
    internal const int BodyComplete = 1;

    /// <summary>Unknown length, fed by <see cref="dn2cpp_http2_call_write"/>: h2 DATA
    /// frames or a chunked HTTP/1.1 framing, and a redirect fails rather than
    /// silently re-sending what curl cannot rewind.</summary>
    internal const int BodyIncremental = 2;

    /// <summary>Queue request-body bytes; never blocks, never takes a chunk
    /// partially. 0 when the call already failed, finished, or had
    /// <see cref="dn2cpp_http2_call_finish_send"/>; 1 taken; 2 taken and the queue
    /// is at its bound — write nothing more until
    /// <see cref="dn2cpp_http2_call_await_send_drain"/> answers.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_write(IntPtr c, byte[] buf, int len);

    /// <summary>BLOCKS (pool worker only) until the send queue drains below its low
    /// water mark (1 — writable again) or the call ended (0).</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_await_send_drain(IntPtr c);

    /// <summary>Close the send side (h2 END_STREAM / final chunk).</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void dn2cpp_http2_call_finish_send(IntPtr c);

    /// <summary>BLOCKS until the response header block is complete (1) or the
    /// transport failed before headers (0 — <see cref="dn2cpp_http2_call_error"/>
    /// names why, including a version-policy miss).</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_wait_headers(IntPtr c);

    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_status(IntPtr c);

    /// <summary>The NEGOTIATED protocol as Major*10+Minor (10/11/20), 0 unknown —
    /// what <c>HttpResponseMessage.Version</c> is rebuilt from.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_http_version(IntPtr c);

    /// <summary>Borrowed <c>char*</c> (owned by the handle): reason phrase, or empty.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr dn2cpp_http2_call_reason(IntPtr c);

    /// <summary>Borrowed <c>char*</c> (owned by the handle): the failure text, or null
    /// while the call has not failed.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr dn2cpp_http2_call_error(IntPtr c);

    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_header_count(IntPtr c);

    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr dn2cpp_http2_call_header_name(IntPtr c, int i);

    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr dn2cpp_http2_call_header_value(IntPtr c, int i);

    /// <summary>BLOCKS for the next body bytes: &gt;0 bytes written into
    /// <paramref name="buf"/>, 0 = EOS (trailers final from then on), -1 = failure
    /// (buffered bytes drain first — the stream breaks where the transfer did).
    /// <paramref name="cap"/> 0 is the zero-byte availability probe: blocks until
    /// data-or-end, consumes nothing, and its 0 is NOT an EOS.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_read(IntPtr c, byte[] buf, int cap);

    /// <summary>Trailers are a SEPARATE list from headers (h2 trailers, or chunked
    /// HTTP/1.1 trailers): gRPC reads <c>grpc-status</c> from the trailer section,
    /// and a <c>grpc-status</c> among the headers means a trailers-only response, so
    /// the two must never be folded together. Valid once
    /// <see cref="dn2cpp_http2_call_read"/> returned 0.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int dn2cpp_http2_call_trailer_count(IntPtr c);

    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr dn2cpp_http2_call_trailer_name(IntPtr c, int i);

    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern IntPtr dn2cpp_http2_call_trailer_value(IntPtr c, int i);

    /// <summary>Stop the transfer and release every blocked caller; idempotent.</summary>
    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void dn2cpp_http2_call_abort(IntPtr c);

    [DllImport(Module, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void dn2cpp_http2_call_free(IntPtr c);
}
