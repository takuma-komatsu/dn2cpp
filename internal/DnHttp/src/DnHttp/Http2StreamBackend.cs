using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using DnHttp.Dn2CppInterop;

namespace DnHttp;

/// <summary>
/// dn2cpp's HTTP transport: the one route every intercepted
/// <c>SocketsHttpHandler.Send</c>/<c>SendAsync</c> takes (see <see cref="DnHttpBackend"/>
/// for why there is only one). A native <c>dn2cpp_http2_call_*</c> handle per request
/// (contract in <c>runtime/core/dn2cpp_core.h</c>): the send returns when the response
/// header block is complete — HttpCompletionOption.ResponseHeadersRead semantics, which
/// is what the handler boundary means in real .NET too, since the completion option never
/// crosses it and <c>HttpClient</c> itself does the buffering for ResponseContentRead —
/// the response content delivers incrementally through <see cref="Http2ResponseStream"/>,
/// and trailers land on <c>TrailingHeaders</c> exactly when the stream reports EOS, the
/// point after which grpc-dotnet reads them.
///
/// The REQUEST body still has two shapes, because curl can put only one of them on the
/// wire per request (<see cref="ClassifyBody"/>): a known length goes over whole at open,
/// with a Content-Length and rewindable for a redirect; an unknown one pumps CONCURRENTLY
/// with the response through <see cref="Http2RequestStream"/>, because buffering it would
/// deadlock an interlocked stream (gRPC's PushUnaryContent / PushStreamContent).
///
/// Threading: every BLOCKING native call runs on a Task.Run pool worker on the async
/// path, never on the cooperative scheduler; <see cref="Send"/> blocks its own caller
/// instead, which is what keeps the transport usable on wasm, where Task.Run throws.
/// The incremental send queue is BOUNDED: a write past its watermark is still taken,
/// but the next one must wait out await_send_drain — a blocking wait that runs on a
/// pool worker too, which is what lets a queue-full WriteAsync park as a pending Task
/// (see <c>Http2RequestStream</c>). Cancellation: a fired token aborts the NATIVE
/// call, which wakes the transfer's poll at once and releases every blocked worker;
/// each awaiting caller is additionally released by a WhenAny race so it observes the
/// canceled shape promptly.
/// </summary>
internal static class Http2StreamBackend
{
    // A zero-length byte[] does not reliably marshal to a valid pointer, and the two
    // bodyless modes still pass an array. The native side reads neither.
    private static readonly byte[] s_noBody = new byte[1];

    internal static async Task<HttpResponseMessage> SendAsync(
        SocketsHttpHandler handler, HttpRequestMessage req, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        int mode = ClassifyBody(req);
        string[] lines = HeaderLines(req);
        // After the lines, never before: reading the content materializes header state
        // the wire must not be told about (see ClassifyBody).
        byte[] body = mode == Http2Native.BodyComplete
            ? await req.Content!.ReadAsByteArrayAsync(ct).ConfigureAwait(false)
            : s_noBody;
        var call = new Http2Call(Open(handler, req, lines, mode, body));

        if (mode == Http2Native.BodyIncremental)
        {
            // The pump runs concurrently with the header wait and is never awaited
            // before the response returns: a unary server holds its headers until it has
            // the request, a bidi server answers before the request completes. It never
            // throws — a failure aborts the call, which is what carries the diagnosis to
            // the waiters.
            _ = PumpRequestBodyAsync(call, req.Content!, ct);
        }
        else
        {
            call.MarkSendDone();
        }

        Task<ResponseHead?> work = Task.Run(() => call.WaitHeaders());
        if (ct.CanBeCanceled)
        {
            var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => { call.Abort("the request token was canceled"); canceled.TrySetResult(true); }))
            {
                if (await Task.WhenAny(work, canceled.Task).ConfigureAwait(false) != work)
                {
                    call.MarkReaderDone(); // no response stream will ever exist
                    throw new TaskCanceledException("The request was canceled.", null, ct);
                }
            }
        }
        ResponseHead? head = await work.ConfigureAwait(false);
        if (head is null)
        {
            string? err = call.ErrorText();
            call.MarkReaderDone();
            ct.ThrowIfCancellationRequested();
            throw new HttpRequestException(err ?? "The HTTP request failed at the transport level.");
        }
        return BuildResponse(call, req, head);
    }

    /// <summary>The synchronous transport: the same call, blocking this thread rather than
    /// a pool worker. Not a convenience wrapper over <see cref="SendAsync"/> — on wasm
    /// there are no threads, so Task.Run throws and this is the only path that reaches the
    /// transport at all.</summary>
    internal static HttpResponseMessage Send(
        SocketsHttpHandler handler, HttpRequestMessage req, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        int mode = ClassifyBody(req);
        string[] lines = HeaderLines(req);
        byte[] body = mode == Http2Native.BodyComplete
            ? req.Content!.ReadAsByteArrayAsync(ct).GetAwaiter().GetResult()
            : s_noBody;
        var call = new Http2Call(Open(handler, req, lines, mode, body));

        if (mode == Http2Native.BodyIncremental)
        {
            // Serialized up front rather than pumped: a synchronous send has no caller to
            // interleave with, and a write never blocks, so this cannot stall the transfer.
            try
            {
                req.Content!.CopyToAsync(new Http2RequestStream(call), ct).GetAwaiter().GetResult();
                // Without this the upload never reaches EOS: the read callback parks in
                // CURL_READFUNC_PAUSE waiting for bytes no one will queue.
                call.FinishSend();
            }
            catch (Exception ex)
            {
                // Same carve-out as PumpRequestBodyAsync: the sink's call-ended
                // refusal is a call with a response to serve, never a fault.
                if (!IsCallEnded(ex))
                    call.Abort("the request body pump faulted: " + ex);
            }
        }
        call.MarkSendDone();

        CancellationTokenRegistration reg = ct.CanBeCanceled
            ? ct.Register(() => call.Abort("the request token was canceled"))
            : default;
        ResponseHead? head;
        try
        {
            head = call.WaitHeaders();
        }
        finally
        {
            reg.Dispose();
        }

        if (head is null)
        {
            string? err = call.ErrorText();
            call.MarkReaderDone();
            // A cancel and a transport failure arrive alike; the token is what tells them
            // apart — real HttpClient answers a fired token with an
            // OperationCanceledException, never an HttpRequestException.
            ct.ThrowIfCancellationRequested();
            throw new HttpRequestException(err ?? "The HTTP request failed at the transport level.");
        }
        return BuildResponse(call, req, head);
    }

    /// <summary>Which of the native open's three body shapes this request is. The
    /// ContentLength getter MATERIALIZES a computed value into the header store, and
    /// <see cref="HeaderLines"/> must carry exactly what the store said before this asked,
    /// so a value this minted is removed again.</summary>
    private static int ClassifyBody(HttpRequestMessage req)
    {
        if (req.Content is not { } content)
            return Http2Native.BodyNone;
        bool stored = content.Headers.TryGetValues("Content-Length", out _);
        if (content.Headers.ContentLength is null)
            return Http2Native.BodyIncremental;
        if (!stored)
            content.Headers.Remove("Content-Length");
        return Http2Native.BodyComplete;
    }

    /// <summary>Request + content headers flattened into curl's "Name: Value" line form. A
    /// multi-valued header folds onto ONE line, values joined with ", " — the wire form
    /// SocketsHttpHandler's HttpConnection writes — because the fold is visible to the
    /// server: two <c>X-Custom: v</c> lines and one <c>X-Custom: v1, v2</c> line read back
    /// differently. (Cookie's "; " separator is the known exception, not modeled until
    /// something routes a multi-valued Cookie through here.)</summary>
    private static string[] HeaderLines(HttpRequestMessage req)
    {
        var lines = new List<string>();
        foreach (var h in req.Headers)
            lines.Add(h.Key + ": " + string.Join(", ", h.Value));
        if (req.Content is { } content)
        {
            foreach (var h in content.Headers)
                lines.Add(h.Key + ": " + string.Join(", ", h.Value));
        }
        return lines.ToArray();
    }

    /// <summary>The request's version demand travels to the native side as Major*10+Minor
    /// plus the raw HttpVersionPolicy value, and the native side owns the whole policy:
    /// what to offer (ALPN / h2c prior knowledge), and that a negotiation the policy cannot
    /// accept is a transport failure. gRPC is the load-bearing caller: it sends (2.0,
    /// RequestVersionExact) and rejects any response that does not read 2.0. The handler's
    /// four pool knobs cross with it: the two lifetimes in whole seconds,
    /// MaxConnectionsPerServer with int.MaxValue as 0 (unlimited — the value that means
    /// "I did not ask"), and EnableMultipleHttp2Connections as a flag (see
    /// <see cref="DnHttpBackend"/> for what the native side does with each). No timeout
    /// crosses: HttpClient.Timeout is enforced by HttpClient itself through the
    /// cancellation token, never surfaced to the handler.</summary>
    private static IntPtr Open(
        SocketsHttpHandler handler, HttpRequestMessage req, string[] lines, int mode, byte[] body)
    {
        int version = req.Version.Major * 10 + req.Version.Minor;
        int maxConns = handler.MaxConnectionsPerServer;
        return Http2Native.dn2cpp_http2_call_open(
            req.Method.Method, req.RequestUri!.AbsoluteUri, lines, lines.Length,
            mode, body.Length == 0 ? s_noBody : body, body.Length,
            version, (int)req.VersionPolicy,
            PoolSeconds(handler.PooledConnectionIdleTimeout),
            PoolSeconds(handler.PooledConnectionLifetime),
            maxConns == int.MaxValue ? 0 : maxConns,
            handler.EnableMultipleHttp2Connections ? 1 : 0);
    }

    /// <summary>A pool TimeSpan as curl's whole seconds. Infinite is 0, which is curl's
    /// spelling of "no limit" too. Anything positive rounds UP: curl's setopt truncates
    /// to seconds, so a 500 ms idle timeout would truncate to 0 and read as no limit at
    /// all — the exact opposite of what was asked.</summary>
    private static int PoolSeconds(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
            return 0; // Timeout.InfiniteTimeSpan is -1 ticks
        double seconds = Math.Ceiling(value.TotalSeconds);
        return seconds >= int.MaxValue ? 0 : (int)seconds;
    }

    private static HttpResponseMessage BuildResponse(Http2Call call, HttpRequestMessage req, ResponseHead head)
    {
        var msg = new HttpResponseMessage((HttpStatusCode)head.Status)
        {
            ReasonPhrase = string.IsNullOrEmpty(head.Reason) ? null : head.Reason,
            RequestMessage = req,
        };
        // The NEGOTIATED version, not the requested one: Version's default is 1.1, and
        // grpc-dotnet's response validation reads exactly this property to decide "the
        // protocol was downgraded" — leaving it defaulted fails every h2 response, and
        // stamping the REQUESTED version would hide a downgrade the policy allowed.
        if (head.Version > 0)
            msg.Version = new Version(head.Version / 10, head.Version % 10);
        var stream = new Http2ResponseStream(call, msg);
        msg.Content = new Http2StreamContent(stream);
        foreach (var h in head.Headers)
        {
            // Response headers land on the message; a content header (Content-Type,
            // Content-Length, …) is rejected there and belongs on the content instead —
            // the same split HttpResponseMessage exposes. No ContentLength pinning: this
            // content computes no length of its own, so the store carries exactly what the
            // wire said, which is what a real handler's response content does too.
            if (!msg.Headers.TryAddWithoutValidation(h.Key, h.Value))
                msg.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
        }
        return msg;
    }

    /// <summary>True when the pump's failure is the sink's call-ended refusal —
    /// HttpContent.CopyToAsync wraps stream exceptions ("Error while copying content
    /// to a stream."), so the sentinel is looked for one level down as well.</summary>
    private static bool IsCallEnded(Exception ex)
        => ex is CallEndedException || ex.InnerException is CallEndedException;

    private static async Task PumpRequestBodyAsync(Http2Call call, HttpContent content, CancellationToken ct)
    {
        try
        {
            var sink = new Http2RequestStream(call);
            await content.CopyToAsync(sink, ct).ConfigureAwait(false);
            call.FinishSend();
        }
        catch (Exception ex)
        {
            // A failed serialize (or a fired token) has no caller of its own to
            // report to; aborting the call is what surfaces it to the readers —
            // with the cause, or the reader's diagnosis is "stream failed". The one
            // failure that must NOT abort is the sink's own call-ended refusal
            // (see CallEndedException): a call that completed without draining the
            // upload has a response the reader is entitled to.
            if (!IsCallEnded(ex))
                call.Abort("the request body pump faulted: " + ex);
        }
        finally
        {
            call.MarkSendDone();
        }
    }

    internal sealed class ResponseHead
    {
        public int Status;
        public int Version;
        public string? Reason;
        public List<KeyValuePair<string, string>> Headers = new();
    }

    /// <summary>Owns the native handle. Frees it only when the body pump finished,
    /// the reader side is done, AND no thread is inside a native call on it — the
    /// serialization <c>dn2cpp_http2_call_free</c>'s contract demands. All methods
    /// are safe after the handle is gone (they answer the failed shape).</summary>
    internal sealed class Http2Call
    {
        private readonly IntPtr _h;
        private readonly object _gate = new();
        private int _inFlight;
        private bool _sendDone;
        private bool _readerDone;
        private bool _freed;
        private string? _abortReason; // first managed abort cause, for diagnostics

        public Http2Call(IntPtr h) => _h = h;

        public string? AbortReason
        {
            get
            {
                lock (_gate)
                {
                    return _abortReason;
                }
            }
        }

        private bool Enter()
        {
            lock (_gate)
            {
                if (_freed)
                    return false;
                _inFlight++;
                return true;
            }
        }

        private void Exit()
        {
            lock (_gate)
            {
                _inFlight--;
                MaybeFreeLocked();
            }
        }

        private void MaybeFreeLocked()
        {
            if (!_freed && _sendDone && _readerDone && _inFlight == 0)
            {
                _freed = true;
                Http2Native.dn2cpp_http2_call_free(_h);
            }
        }

        public void MarkSendDone()
        {
            lock (_gate)
            {
                _sendDone = true;
                MaybeFreeLocked();
            }
        }

        public void MarkReaderDone()
        {
            lock (_gate)
            {
                _readerDone = true;
                MaybeFreeLocked();
            }
        }

        /// <summary>What the native send queue answered a write with.</summary>
        public enum WriteResult
        {
            /// <summary>The call has ended; nothing was queued and nothing will be.</summary>
            Ended,
            /// <summary>Queued, with room left.</summary>
            Accepted,
            /// <summary>Queued, and the queue is now at its bound: park on
            /// <see cref="AwaitSendDrain"/> before handing over the next chunk.</summary>
            Full,
        }

        public WriteResult Write(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return WriteResult.Accepted;
            byte[] chunk = buffer;
            if (offset != 0 || count != buffer.Length)
            {
                chunk = new byte[count];
                Buffer.BlockCopy(buffer, offset, chunk, 0, count);
            }
            if (!Enter())
                return WriteResult.Ended;
            try
            {
                return Http2Native.dn2cpp_http2_call_write(_h, chunk, count) switch
                {
                    0 => WriteResult.Ended,
                    2 => WriteResult.Full,
                    _ => WriteResult.Accepted,
                };
            }
            finally
            {
                Exit();
            }
        }

        /// <summary>Blocks (pool worker only). True = the queue drained and the
        /// writer may hand over its next chunk; false = the call ended.</summary>
        public bool AwaitSendDrain()
        {
            if (!Enter())
                return false;
            try
            {
                return Http2Native.dn2cpp_http2_call_await_send_drain(_h) != 0;
            }
            finally
            {
                Exit();
            }
        }

        public void FinishSend()
        {
            if (!Enter())
                return;
            try
            {
                Http2Native.dn2cpp_http2_call_finish_send(_h);
            }
            finally
            {
                Exit();
            }
        }

        public void Abort(string reason)
        {
            lock (_gate)
            {
                _abortReason ??= reason;
            }
            if (!Enter())
                return;
            try
            {
                Http2Native.dn2cpp_http2_call_abort(_h);
            }
            finally
            {
                Exit();
            }
        }

        /// <summary>Blocks (pool worker only). Null = transport failure before the
        /// header block completed; <see cref="ErrorText"/> names why.</summary>
        public ResponseHead? WaitHeaders()
        {
            if (!Enter())
                return null;
            try
            {
                if (Http2Native.dn2cpp_http2_call_wait_headers(_h) == 0)
                    return null;
                var head = new ResponseHead
                {
                    Status = Http2Native.dn2cpp_http2_call_status(_h),
                    Version = Http2Native.dn2cpp_http2_call_http_version(_h),
                    Reason = Marshal.PtrToStringUTF8(Http2Native.dn2cpp_http2_call_reason(_h)),
                };
                int n = Http2Native.dn2cpp_http2_call_header_count(_h);
                for (int i = 0; i < n; i++)
                {
                    string? name = Marshal.PtrToStringUTF8(Http2Native.dn2cpp_http2_call_header_name(_h, i));
                    if (string.IsNullOrEmpty(name))
                        continue;
                    string value = Marshal.PtrToStringUTF8(Http2Native.dn2cpp_http2_call_header_value(_h, i)) ?? string.Empty;
                    head.Headers.Add(new KeyValuePair<string, string>(name, value));
                }
                return head;
            }
            finally
            {
                Exit();
            }
        }

        /// <summary>Blocks (pool worker only). Never throws: &gt;0 bytes, 0 EOS,
        /// -1 failure/abort/after-free.</summary>
        // A zero-byte read needs a non-null native pointer; an empty byte[] does not
        // reliably marshal to one.
        private static readonly byte[] s_zeroProbe = new byte[1];

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!Enter())
                return -1;
            try
            {
                if (count == 0)
                    return Http2Native.dn2cpp_http2_call_read(_h, s_zeroProbe, 0);
                if (offset == 0)
                    return Http2Native.dn2cpp_http2_call_read(_h, buffer, count);
                byte[] tmp = new byte[count];
                int n = Http2Native.dn2cpp_http2_call_read(_h, tmp, count);
                if (n > 0)
                    Buffer.BlockCopy(tmp, 0, buffer, offset, n);
                return n;
            }
            finally
            {
                Exit();
            }
        }

        public void CopyTrailers(HttpHeaders target)
        {
            if (!Enter())
                return;
            try
            {
                int n = Http2Native.dn2cpp_http2_call_trailer_count(_h);
                for (int i = 0; i < n; i++)
                {
                    string? name = Marshal.PtrToStringUTF8(Http2Native.dn2cpp_http2_call_trailer_name(_h, i));
                    if (string.IsNullOrEmpty(name))
                        continue;
                    string value = Marshal.PtrToStringUTF8(Http2Native.dn2cpp_http2_call_trailer_value(_h, i)) ?? string.Empty;
                    target.TryAddWithoutValidation(name, value);
                }
            }
            finally
            {
                Exit();
            }
        }

        public string? ErrorText()
        {
            if (!Enter())
                return null;
            try
            {
                return Marshal.PtrToStringUTF8(Http2Native.dn2cpp_http2_call_error(_h));
            }
            finally
            {
                Exit();
            }
        }
    }

    /// <summary>What the request-body sink throws when the CALL has ended — a
    /// distinct type, because the pump must tell it apart from the content itself
    /// faulting. "Ended" is failed, aborted, or DONE — and done is the
    /// legitimate case: a server may answer completely without draining the request
    /// body (an auth refusal, a validation error), which real .NET surfaces as the
    /// response, never as an upload error. So an ended sink is never a reason to
    /// abort: a done call has a response to serve, a failed one already carries its
    /// diagnosis, an aborted one is being torn down by whoever fired the abort.</summary>
    private sealed class CallEndedException : IOException
    {
        public CallEndedException()
            : base("The request body could not be written; the call has ended.")
        {
        }
    }

    /// <summary>The request-body sink the pump serializes into: write = enqueue on
    /// the native send queue, which takes the chunk whole and never blocks. A write
    /// after the call ended throws — answering "written" for bytes nothing will send
    /// is the silent shape.
    ///
    /// Backpressure: a write that fills the queue returns an INCOMPLETE task,
    /// resumed once the transfer drained it — so a writer outrunning the connection
    /// parks as a pending await, never as a blocked scheduler thread. The blocking
    /// half runs on a pool worker, the <see cref="SendAsync"/> pattern. The
    /// synchronous <see cref="Write(byte[], int, int)"/> has nowhere to park and so
    /// blocks, which is Stream's own contract for a full sink; every path this
    /// transport drives goes through <c>CopyToAsync</c>, i.e. through WriteAsync.</summary>
    private sealed class Http2RequestStream : Stream
    {
        private readonly Http2Call _call;

        public Http2RequestStream(Http2Call call) => _call = call;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        private static CallEndedException Ended()
            => new CallEndedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            Http2Call.WriteResult r = _call.Write(buffer, offset, count);
            if (r == Http2Call.WriteResult.Ended)
                throw Ended();
            if (r == Http2Call.WriteResult.Full && !_call.AwaitSendDrain())
                throw Ended();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Http2Call.WriteResult r = _call.Write(buffer, offset, count);
            if (r == Http2Call.WriteResult.Ended)
                return Task.FromException(Ended());
            // The common case completes synchronously and allocates nothing; only a
            // filled queue takes the awaiting path.
            return r == Http2Call.WriteResult.Full
                ? DrainAsync(cancellationToken)
                : Task.CompletedTask;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> seg))
                return new ValueTask(WriteAsync(seg.Array!, seg.Offset, seg.Count, cancellationToken));
            byte[] copy = buffer.ToArray();
            return new ValueTask(WriteAsync(copy, 0, copy.Length, cancellationToken));
        }

        /// <summary>Parks until the transfer drained the queue. The blocking native
        /// wait runs on a pool worker and the caller's token races it, the same shape
        /// <see cref="Http2ResponseStream.ReadAsync(byte[], int, int, CancellationToken)"/>
        /// uses — a fired token aborts the call, which is what releases the worker.</summary>
        private async Task DrainAsync(CancellationToken cancellationToken)
        {
            Task<bool> work = Task.Run(() => _call.AwaitSendDrain());
            if (cancellationToken.CanBeCanceled)
            {
                var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(() => { _call.Abort("the write token was canceled"); canceled.TrySetResult(true); }))
                {
                    if (await Task.WhenAny(work, canceled.Task).ConfigureAwait(false) != work)
                        throw new TaskCanceledException("The write was canceled.", null, cancellationToken);
                }
            }
            if (!await work.ConfigureAwait(false))
            {
                // Same ordering hazard as the read: the abort releases the worker, so the
                // token is asked before the drain failure is reported as an ended call.
                cancellationToken.ThrowIfCancellationRequested();
                throw Ended();
            }
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    /// <summary>The response body as it arrives. Read-only, forward-only; each async
    /// read runs the blocking native read on a pool worker and races the caller's
    /// token (a fired token aborts the call). EOS copies the trailers onto the
    /// response message BEFORE reporting 0, so a reader that observed the end can
    /// always see them — the ordering grpc-dotnet stands on. Dispose before EOS
    /// aborts the call (the mid-stream teardown route).</summary>
    private sealed class Http2ResponseStream : Stream
    {
        private readonly Http2Call _call;
        private readonly HttpResponseMessage _msg;
        private bool _eos;
        private bool _disposed;

        public Http2ResponseStream(Http2Call call, HttpResponseMessage msg)
        {
            _call = call;
            _msg = msg;
        }

        /// <summary>A DROPPED response — never read to EOS, never disposed — must not
        /// hold its transfer, and with it a connection stream, until process exit.
        /// Real .NET tears an abandoned response stream down when the GC
        /// reaches it (measured: the server's RequestAborted fires right after a
        /// collection), so this stream does the same; Stream.Close suppresses the
        /// finalizer on the Dispose path, and EOS already released the call.</summary>
        ~Http2ResponseStream()
        {
            if (!_disposed && !_eos)
                _call.Abort("the response stream was dropped without a read or a Dispose");
            _call.MarkReaderDone();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        // Never throws; the interpreting wrappers below do. Runs on whatever thread
        // blocks, and the EOS arm runs on it too, so trailers precede the 0.
        private int ReadNoThrow(byte[] buffer, int offset, int count)
        {
            if (_eos)
                return 0;
            int n = _call.Read(buffer, offset, count);
            // A zero-byte probe's 0 means data-or-EOS, never EOS alone — only a
            // SIZED read's 0 ends the stream (and freezes the trailers).
            if (n == 0 && count > 0)
            {
                _eos = true;
                _call.CopyTrailers(_msg.TrailingHeaders);
                // Released at the END OF THE STREAM, not at Dispose: every request takes
                // this path now, and a response the caller never disposes is ordinary
                // (`var r = await client.GetAsync(u); await r.Content.ReadAsStringAsync();`)
                // — holding the handle until a Dispose that may never come would leak its
                // transfer thread. Further reads short-circuit on _eos above and never
                // touch the freed handle.
                _call.MarkReaderDone();
            }
            return n;
        }

        private int Interpret(int n)
        {
            if (n < 0)
                throw new IOException(_call.ErrorText()
                    ?? (_call.AbortReason is { } r ? "The call was aborted: " + r : "The response stream failed."));
            return n;
        }

        public override int Read(byte[] buffer, int offset, int count)
            => Interpret(ReadNoThrow(buffer, offset, count));

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Task<int> work = Task.Run(() => ReadNoThrow(buffer, offset, count));
            if (cancellationToken.CanBeCanceled)
            {
                var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(() => { _call.Abort("the read token was canceled"); canceled.TrySetResult(true); }))
                {
                    if (await Task.WhenAny(work, canceled.Task).ConfigureAwait(false) != work)
                        throw new TaskCanceledException("The read was canceled.", null, cancellationToken);
                }
            }
            int n = await work.ConfigureAwait(false);
            // A fired token's abort and a transport failure both come back as n<0, and the
            // abort can complete the read before the arm above observes the cancel — so the
            // token, not that race, is what tells them apart: real HttpClient answers a
            // fired token with an OperationCanceledException, never an IOException.
            if (n < 0)
                cancellationToken.ThrowIfCancellationRequested();
            return Interpret(n);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (MemoryMarshal.TryGetArray<byte>(buffer, out ArraySegment<byte> seg))
                return new ValueTask<int>(ReadAsync(seg.Array!, seg.Offset, seg.Count, cancellationToken));
            return SlowReadAsync(buffer, cancellationToken);
        }

        private async ValueTask<int> SlowReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            byte[] tmp = new byte[buffer.Length];
            int n = await ReadAsync(tmp, 0, tmp.Length, cancellationToken).ConfigureAwait(false);
            if (n > 0)
                new ReadOnlyMemory<byte>(tmp, 0, n).CopyTo(buffer);
            return n;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                if (!_eos)
                    _call.Abort("the response stream was disposed before EOS");
                _call.MarkReaderDone();
            }
            base.Dispose(disposing);
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>Hands the live stream to ReadAsStreamAsync; SerializeToStream(Async)
    /// (the LoadIntoBuffer(Async) route HttpClient's default completion option takes)
    /// drains the same stream forward. The SYNCHRONOUS pair is not decoration: the base
    /// HttpContent's sync serialize throws, so without it every <c>HttpClient.Send</c>
    /// would fail the moment it buffered — which is the whole of the wasm lane.</summary>
    private sealed class Http2StreamContent : HttpContent
    {
        private readonly Http2ResponseStream _stream;

        public Http2StreamContent(Http2ResponseStream stream) => _stream = stream;

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => _stream.CopyToAsync(stream);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => _stream.CopyToAsync(stream, cancellationToken);

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            => _stream.CopyTo(stream);

        protected override Task<Stream> CreateContentReadStreamAsync()
            => Task.FromResult<Stream>(_stream);

        protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
            => _stream;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
