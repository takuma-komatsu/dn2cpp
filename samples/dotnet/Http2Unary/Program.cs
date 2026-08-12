using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace Http2Unary
{
    // The h2 transport surface beneath gRPC's unary call, exercised directly: version
    // negotiation (h2c prior knowledge over http://, ALPN h2 over https://), the
    // negotiated-version readback, the header/trailer split (TrailingHeaders is a
    // SEPARATE collection from Headers), the loud failure an EXACT version policy
    // produces against a peer that will not negotiate it, and properties of the
    // connection rather than of the message: reuse, mid-flight cancellation,
    // pool settings, multiplexing, and when the send returns. Under real .NET this
    // runs SocketsHttpHandler; transpiled with -r DnHttp it runs the libcurl+nghttp2
    // transport (SocketsHttpHandler.Send is intercepted).
    // gates/build-and-run-http2-unary.sh runs both against the SAME Kestrel oracle
    // (samples/dotnet/Http2TestServer — real .NET only, never transpiled) and diffs
    // the h2c output byte for byte; the TLS+ALPN mode is self-contained and not
    // diffed (see that gate's own comment).
    //
    // Mode-dispatched on argv:
    //   h2c <h2c-base-url> <h1-base-url>   — sections (a)-(m) below
    //   tls <https-base-url>               — the TLS+ALPN probe
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // Real .NET's SocketsHttpHandler refuses HTTP/2 over cleartext unless this
            // switch is set. It gates only that handler's own check, which the dn2cpp
            // intercept cuts, so it is inert natively and load-bearing only for the real
            // .NET oracle run of this same program.
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: Http2Unary h2c <h2c-base-url> <h1-base-url>");
                Console.Error.WriteLine("       Http2Unary tls <https-base-url>");
                return 2;
            }

            switch (args[0])
            {
                case "h2c":
                    if (args.Length < 3)
                    {
                        Console.Error.WriteLine("usage: Http2Unary h2c <h2c-base-url> <h1-base-url>");
                        return 2;
                    }
                    RunH2c(args[1], args[2]);
                    return 0;

                case "tls":
                    if (args.Length < 2)
                    {
                        Console.Error.WriteLine("usage: Http2Unary tls <https-base-url>");
                        return 2;
                    }
                    RunTls(args[1]);
                    return 0;


                default:
                    Console.Error.WriteLine("usage: Http2Unary h2c|tls ...");
                    return 2;
            }
        }

        // 64 deterministic bytes — small enough to keep the wire trace readable, large
        // enough that a transport folding the body wrong is still a visible diff.
        private static byte[] FixedBody()
        {
            var body = new byte[64];
            for (int i = 0; i < body.Length; i++)
                body[i] = (byte)(i * 7 + 13);
            return body;
        }

        private static void RunH2c(string h2cBase, string h1Base)
        {
            using var client = new HttpClient();
            EchoOverH2c(client, h2cBase);
            TrailersOverH2c(client, h2cBase);
            ExactMissOverH1(client, h1Base);
            OrLowerOverH1(client, h1Base);
            ReuseOverH2c(client, h2cBase);
            HeadersReadOverH2c(client, h2cBase);
            PoolSettingsOverH2c(h2cBase);
            // (f) after (g)/(h): aborting a transfer tears down the connection it was
            // riding, which is exactly what (e) measures the survival of; (i)-(k) are
            // appended last so every earlier section's output stays an unchanged
            // prefix of this program's.
            CancelOverH2c(client, h2cBase);
            SinkOverH2c(client, h2cBase);
            MuxOverH2c(client, h2cBase);
            MaxConnOverH1(h1Base);
            EarlyReplyOverH2c(client, h2cBase);
            DropOverH2c(client, h2cBase);
        }

        // (a) POST /echo over h2c prior knowledge (Version=2.0, VersionPolicy=Exact — no
        // TLS, so this is the ONLY thing that can make an h2 connection: a raw h2
        // preface sent straight over cleartext, no negotiation). The server-side
        // `proto=HTTP/2` line in the echoed body is the proof: a client cannot
        // fabricate what the SERVER observed the connection speaking.
        private static void EchoOverH2c(HttpClient client, string h2cBase)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, h2cBase + "/echo")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                Content = new ByteArrayContent(FixedBody()),
            };
            using HttpResponseMessage response = Send(client, request);
            Console.WriteLine("[h2c] version=" + response.Version + " status=" + (int)response.StatusCode);
            PrintBodyLines("[h2c]", response);
            PrintHeadersSorted("[h2c]", response);
        }

        // (b) GET /trailers, same version/policy. HTTP trailers are a collection
        // SEPARATE from the response headers (TrailingHeaders), and .NET only
        // populates them once the content has been read to completion — which
        // PrintBodyLines already does, so the trailer read below always sees the final
        // set, never a partial one.
        private static void TrailersOverH2c(HttpClient client, string h2cBase)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, h2cBase + "/trailers")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };
            using HttpResponseMessage response = Send(client, request);
            Console.WriteLine("[trailers] version=" + response.Version + " status=" + (int)response.StatusCode);
            PrintBodyLines("[trailers]", response);

            var lines = new List<string>();
            foreach (KeyValuePair<string, IEnumerable<string>> h in response.TrailingHeaders)
                lines.Add(h.Key.ToLowerInvariant() + "=" + string.Join(", ", h.Value));
            lines.Sort(StringComparer.Ordinal);
            foreach (string line in lines)
                Console.WriteLine("[trailers] trailer " + line);
            // Always printed — including zero — so a regression that drops the
            // trailer section reads as a visible "count=0" rather than a silently
            // shorter output.
            Console.WriteLine("[trailers] count=" + lines.Count);
        }

        // (c) The SAME request shape as (a), but VersionPolicy=Exact against an
        // HTTP/1.1-ONLY listener: the peer cannot satisfy "exactly 2.0", so this MUST
        // be a loud, catchable transport failure — never a silent downgrade to 1.1.
        // Only the exception TYPE is asserted: the message text is transport-specific
        // (curl/nghttp2 vs SocketsHttpHandler each phrase the same refusal
        // differently) and is not part of the diffed contract.
        private static void ExactMissOverH1(HttpClient client, string h1Base)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, h1Base + "/echo")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = new ByteArrayContent(FixedBody()),
                };
                using HttpResponseMessage response = Send(client, request);
                Console.WriteLine("[exact-miss] UNEXPECTED success status=" + (int)response.StatusCode);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("[exact-miss] HttpRequestException");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[exact-miss] UNEXPECTED " + ex.GetType().Name);
            }
        }

        // (d) The same target as (c), RequestVersionOrLower instead of Exact: a policy
        // that ACCEPTS a downgrade must actually get one over cleartext (there is no
        // ALPN to negotiate with — the h1-only listener speaks 1.1, and OrLower is the
        // policy that lets a request settle for it instead of failing outright as (c)
        // does).
        private static void OrLowerOverH1(HttpClient client, string h1Base)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, h1Base + "/echo")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                Content = new ByteArrayContent(FixedBody()),
            };
            using HttpResponseMessage response = Send(client, request);
            Console.WriteLine("[orlower] version=" + response.Version + " status=" + (int)response.StatusCode);
        }

        // (e) CONNECTION REUSE. Two sequential GET /connid on one HttpClient: the server
        // answers with its own id for the connection the request arrived on, so matching
        // ids are the SERVER's testimony that the second request paid no handshake. The
        // ids are random per connection, so only whether they matched is printed.
        private static void ReuseOverH2c(HttpClient client, string h2cBase)
        {
            string first = GetText(client, h2cBase + "/connid");
            string second = GetText(client, h2cBase + "/connid");
            Console.WriteLine("[reuse] same-connection=" + (first == second) +
                              " non-empty=" + (first.Length > 0));
        }

        // (g) HttpCompletionOption.ResponseHeadersRead. /slowbody flushes its header
        // block well before its body, so it is the only endpoint here against which the
        // two completion options are distinguishable: under ResponseHeadersRead the send
        // returns at the header block and only the body read waits; under
        // ResponseContentRead both return together. Reported as the ORDERING, never as
        // milliseconds — absolute times move with the machine, the gap does not.
        private static void HeadersReadOverH2c(HttpClient client, string h2cBase)
        {
            Console.WriteLine("[headersread] body-lagged-headers="
                + BodyLagsHeaders(client, h2cBase, HttpCompletionOption.ResponseHeadersRead));
            Console.WriteLine("[contentread] body-lagged-headers="
                + BodyLagsHeaders(client, h2cBase, HttpCompletionOption.ResponseContentRead));
        }

        private static bool BodyLagsHeaders(HttpClient client, string h2cBase, HttpCompletionOption option)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, h2cBase + "/slowbody")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };
            DateTime start = DateTime.UtcNow;
            using HttpResponseMessage response = client.SendAsync(request, option).GetAwaiter().GetResult();
            double headersMs = (DateTime.UtcNow - start).TotalMilliseconds;
            ReadBodyText(response);
            double bodyMs = (DateTime.UtcNow - start).TotalMilliseconds;
            // Half the server's 1500 ms gap: far outside any scheduling noise, and far
            // inside the gap itself, so neither answer is a close call.
            return bodyMs - headersMs > 750;
        }

        private const int PoolIdleTimeoutSeconds = 1;

        // The idle gap both arms wait out. Real .NET evicts an over-idle connection only on
        // the pool manager's scavenge tick (period max(idle timeout / 4, 1 s)), so the gap
        // must clear the timeout PLUS one tick — 2 s here — by a margin of further ticks: at
        // 2 s exactly the oracle's verdict is that timer's phase, i.e. a coin flip.
        private const int PoolIdleGapMs = 5000;

        // (h) CONNECTION-POOL SETTINGS. The observable that a lifetime knob really reached
        // the transport is the SERVER's own connection id: a handler that will not reuse a
        // connection idle for more than a second must open a new one across the gap above,
        // and a default-configured one (a minute) must not. Two separate HttpClients
        // rather than one reconfigured handler, because a SocketsHttpHandler refuses these
        // setters after its first request. The structural knobs (MaxConnectionsPerServer,
        // EnableMultipleHttp2Connections) have their own sections, (j) and (k).
        private static void PoolSettingsOverH2c(string h2cBase)
        {
            using (var pooled = new HttpClient(new SocketsHttpHandler()))
                Console.WriteLine("[pool] default-reuse=" + SameConnAcrossIdle(pooled, h2cBase));

            var expiring = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(PoolIdleTimeoutSeconds),
            };
            using (var client = new HttpClient(expiring))
                Console.WriteLine("[pool] expiring-reuse=" + SameConnAcrossIdle(client, h2cBase));
        }

        private static bool SameConnAcrossIdle(HttpClient client, string h2cBase)
        {
            string first = GetText(client, h2cBase + "/connid");
            Thread.Sleep(PoolIdleGapMs);
            string second = GetText(client, h2cBase + "/connid");
            return first.Length > 0 && first == second;
        }

        // (f) MID-FLIGHT CANCELLATION. A token fired 300 ms into a request the server will
        // not answer for two minutes: the CALLER must be released with the canceled shape
        // real .NET produces, and the SERVER must see the transfer torn down
        // (RequestAborted) rather than draining behind an abandoned caller. /slowreset
        // first because one server serves both the native and the oracle run.
        private static void CancelOverH2c(HttpClient client, string h2cBase)
        {
            GetText(client, h2cBase + "/slowreset");

            string outcome;
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(300);
                using var request = new HttpRequestMessage(HttpMethod.Get, h2cBase + "/slow")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                };
                try
                {
                    using HttpResponseMessage response =
                        client.SendAsync(request, cts.Token).GetAwaiter().GetResult();
                    outcome = "UNEXPECTED success status=" + (int)response.StatusCode;
                }
                catch (OperationCanceledException)
                {
                    // TaskCanceledException derives from this; the exact derived type is
                    // not asserted because HttpClient's own wrapper picks it.
                    outcome = "OperationCanceledException";
                }
                catch (Exception ex)
                {
                    outcome = "UNEXPECTED " + ex.GetType().Name;
                }
            }
            Console.WriteLine("[cancel] caller=" + outcome);

            // The abort is asynchronous on both transports (dn2cpp's lands within curl's
            // progress-callback interval), so poll rather than sample once. A transport
            // that only released the caller leaves aborted=0 here until the deadline.
            string stat = "";
            for (int i = 0; i < 100; i++)
            {
                stat = GetText(client, h2cBase + "/slowstat");
                if (stat.Contains("aborted=1"))
                    break;
                Thread.Sleep(100);
            }
            Console.WriteLine("[cancel] server-started=" + stat.Contains("started=1") +
                              " server-aborted=" + stat.Contains("aborted=1") +
                              " server-none-completed=" + stat.Contains("completed=0"));
        }

        // (i) SEND-QUEUE BACKPRESSURE. A 4 MiB body pushed through a content that
        // declares NO length, which is what routes it to the streaming transport whose
        // send queue is under test; the gate lowers the native queue bound to 64 KiB so
        // every chunk after the first parks the writer. Asserted here is only the half a
        // client can see — that the bytes arrived whole and in order, per the server's
        // own length and content hash. That the writer really parked is native-only and
        // is asserted off the transport's stderr by the gate, since a counter on stdout
        // could not be diffed against real .NET.
        private static void SinkOverH2c(HttpClient client, string h2cBase)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, h2cBase + "/sink")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                Content = new UnsizedContent(4 << 20, 64 << 10),
            };
            using HttpResponseMessage response = Send(client, request);
            Console.WriteLine("[sink] status=" + (int)response.StatusCode);
            PrintBodyLines("[sink]", response);
        }

        // A body of known content but UNDECLARED length: TryComputeLength answering
        // false is exactly the routing predicate that sends a request to the
        // streaming family (DnHttpBackend.SendAsync), so this shape — not the size —
        // is what makes the section reach the send queue at all. The one async body
        // in this file, and deliberately so: WriteAsync is where a full queue hands
        // back an incomplete task, which a blocking write would never observe.
        private sealed class UnsizedContent : System.Net.Http.HttpContent
        {
            private readonly long _total;
            private readonly int _chunk;

            public UnsizedContent(long total, int chunk)
            {
                _total = total;
                _chunk = chunk;
            }

            protected override async System.Threading.Tasks.Task SerializeToStreamAsync(
                System.IO.Stream stream, TransportContext context)
            {
                var buf = new byte[_chunk];
                long written = 0;
                while (written < _total)
                {
                    long remaining = _total - written;
                    int n = remaining < _chunk ? (int)remaining : _chunk;
                    for (int i = 0; i < n; i++)
                        buf[i] = (byte)((written + i) * 31 + 7);
                    await stream.WriteAsync(buf, 0, n).ConfigureAwait(false);
                    written += n;
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return false;
            }
        }

        // (j) CONCURRENT h2 MULTIPLEXING. Four requests in flight at once over h2c with
        // the default handler (EnableMultipleHttp2Connections=false): all four must land
        // on ONE connection. The server's /connhold ids are the testimony and its
        // concurrency peak is the non-vacuity floor — peak=4 proves the four really were
        // in flight together, so one distinct connection is multiplexing, not luck.
        private static void MuxOverH2c(HttpClient client, string h2cBase)
        {
            GetText(client, h2cBase + "/holdreset");
            var tasks = new System.Threading.Tasks.Task<string>[4];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = GetTextAsync(client, h2cBase + "/connhold?ms=800", useH2: true);
            System.Threading.Tasks.Task.WhenAll(tasks).GetAwaiter().GetResult();
            Console.WriteLine("[mux] " + SummarizeConnIds(tasks, 1) + " " +
                              GetText(client, h2cBase + "/holdstat"));
        }

        // (k) MaxConnectionsPerServer. Three requests in flight against the HTTP/1.1-only
        // listener under a cap of 2: at most two may be on the wire together and the third
        // queues, which the server's concurrency peak reading 2 — not 3 — asserts. Version
        // 1.1 because one multiplexed h2 connection serves any number of streams, so an h2
        // run would assert nothing.
        private static void MaxConnOverH1(string h1Base)
        {
            using var client = new HttpClient(new SocketsHttpHandler
            {
                MaxConnectionsPerServer = 2,
            });
            GetText11(client, h1Base + "/holdreset");
            var tasks = new System.Threading.Tasks.Task<string>[3];
            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = GetTextAsync(client, h1Base + "/connhold?ms=1000", useH2: false);
            System.Threading.Tasks.Task.WhenAll(tasks).GetAwaiter().GetResult();
            Console.WriteLine("[maxconn] " + SummarizeConnIds(tasks, 2) + " " +
                              GetText11(client, h1Base + "/holdstat"));
        }

        // The distinct-connection summary both concurrency sections print: the raw ids
        // are random per connection, so only the count against the expected ceiling is
        // diffable. all-ok separates "few connections" from "few responses".
        private static string SummarizeConnIds(System.Threading.Tasks.Task<string>[] tasks, int ceiling)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool allOk = true;
            foreach (System.Threading.Tasks.Task<string> t in tasks)
            {
                string id = t.GetAwaiter().GetResult();
                allOk = allOk && id.StartsWith("conn=", StringComparison.Ordinal) && id.Length > 5;
                ids.Add(id);
            }
            return "within-ceiling=" + (ids.Count <= ceiling) + " all-ok=" + allOk;
        }

        // (l) EARLY REPLY WITHOUT DRAINING THE REQUEST. The server answers /earlyreply
        // completely after reading ONE byte of the incremental 4 MiB request body —
        // legitimate HTTP (an auth refusal, a validation error), which must surface as
        // the response. A transport that confuses "the call ended before my upload
        // finished" with "my upload failed" reports an exception here instead.
        private static void EarlyReplyOverH2c(HttpClient client, string h2cBase)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, h2cBase + "/earlyreply")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                    Content = new UnsizedContent(4 << 20, 64 << 10),
                };
                using HttpResponseMessage resp = Send(client, req);
                Console.WriteLine("[earlyreply] status=" + (int)resp.StatusCode +
                                  " body=" + ReadBodyText(resp));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[earlyreply] EX " + ex.GetType().Name + ": " + ex.Message);
            }
        }

        // (m) A DROPPED ResponseHeadersRead RESPONSE. /drip streams body forever; the
        // response is taken at the header block and then dropped — never read, never
        // disposed — so its teardown is the GC's job on both transports (the response
        // stream carries a finalizer for exactly this). Collected in a loop because a
        // single collection is not a guarantee; the SERVER's aborted counter is the
        // testimony, as in the cancel section.
        private static void DropOverH2c(HttpClient client, string h2cBase)
        {
            GetText(client, h2cBase + "/dripreset");
            DropResponse(client, h2cBase);
            string stat = "";
            for (int i = 0; i < 150; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                stat = GetText(client, h2cBase + "/dripstat");
                if (stat.Contains("aborted=1"))
                    break;
                Thread.Sleep(100);
            }
            Console.WriteLine("[drop] server-started=" + stat.Contains("started=1") +
                              " server-aborted=" + stat.Contains("aborted=1"));
        }

        // Its own REAL call frame, so the dropped response is dead once this
        // returns: on a conservative collector the graph is only unrooted when an
        // epilogue restores the caller's callee-saved registers, so the inliner
        // must be blocked — inlined, a register can name the send's Task through
        // every collection of the loop above.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DropResponse(HttpClient client, string h2cBase)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, h2cBase + "/drip")
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };
            HttpResponseMessage resp =
                client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            Console.WriteLine("[drop] status=" + (int)resp.StatusCode);
            // Deliberately dropped: never read, never disposed.
        }

        // The TLS+ALPN probe: GET /echo with (2.0, RequestVersionExact) over https://.
        // Self-contained by design (see the gate's own comment) — a rejected
        // certificate or a downgraded ALPN negotiation would surface here as an
        // exception, and its full message IS printed (unlike (c)'s type-only assert)
        // because this arm is never diffed against an oracle.
        private static void RunTls(string httpsBase)
        {
            using var client = new HttpClient();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, httpsBase + "/echo")
                {
                    Version = HttpVersion.Version20,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                };
                using HttpResponseMessage response = Send(client, request);
                Console.WriteLine("[tls] version=" + response.Version + " status=" + (int)response.StatusCode);
                PrintBodyLines("[tls]", response);
                // The reuse (e) asserts over cleartext, where it saves a TCP handshake;
                // over TLS the same share also carries the session, which is the
                // expensive half. Same server-told evidence, second connection id.
                string first = GetText(client, httpsBase + "/connid");
                string second = GetText(client, httpsBase + "/connid");
                Console.WriteLine("[tls] same-connection=" + (first == second));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("[tls] HttpRequestException: " + ex.Message);
            }
        }

        // ── plumbing ──────────────────────────────────────────────────────────────

        // Every call in this file goes through here, never HttpClient.Send: real .NET's
        // synchronous Send throws NotSupportedException before any I/O for any request at
        // Version 2.0 or higher, while under dn2cpp SocketsHttpHandler.Send is intercepted
        // and never reaches that check — so a literal Send would diff a native binary that
        // attempted the request against an oracle that never tried. Blocking on SendAsync
        // makes both sides execute the SAME request.
        private static HttpResponseMessage Send(HttpClient client, HttpRequestMessage request)
        {
            return client.SendAsync(request).GetAwaiter().GetResult();
        }

        // h2c prior knowledge like every other request here, body decoded as text.
        private static string GetText(HttpClient client, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url)
            {
                Version = HttpVersion.Version20,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            };
            using HttpResponseMessage response = Send(client, request);
            return ReadBodyText(response);
        }

        // The default-version (1.1) form, for the HTTP/1.1-only listener.
        private static string GetText11(HttpClient client, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using HttpResponseMessage response = Send(client, request);
            return ReadBodyText(response);
        }

        // The concurrent form sections (j) and (k) stand on: the requests must be IN
        // FLIGHT together, so each rides its own pending task and nothing blocks between
        // the sends.
        private static async System.Threading.Tasks.Task<string> GetTextAsync(
            HttpClient client, string url, bool useH2)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (useH2)
            {
                request.Version = HttpVersion.Version20;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            }
            using HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
            byte[] body = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return Encoding.UTF8.GetString(body);
        }

        private static void PrintBodyLines(string prefix, HttpResponseMessage response)
        {
            string text = ReadBodyText(response);
            foreach (string line in text.Split('\n'))
                if (line.Length > 0)
                    Console.WriteLine(prefix + " body-line " + line);
        }

        // Message headers and content headers combined into ONE sorted set — the
        // split HttpBeyondGet prints separately does not matter here, what matters is
        // that every header the server sent (bar the ever-moving Date) reads back
        // identically on both transports. Never ReasonPhrase: h2 carries none, and the
        // two transports render a missing one differently (null vs. the empty string).
        private static void PrintHeadersSorted(string prefix, HttpResponseMessage response)
        {
            var lines = new List<string>();
            AddHeaderLines(response.Headers, lines);
            AddHeaderLines(response.Content.Headers, lines);
            lines.Sort(StringComparer.Ordinal);
            foreach (string line in lines)
                Console.WriteLine(prefix + " hdr " + line);
        }

        private static void AddHeaderLines(HttpHeaders headers, List<string> lines)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> h in headers)
            {
                string name = h.Key.ToLowerInvariant();
                if (name == "date")
                    continue;
                lines.Add(name + ": " + string.Join(", ", h.Value));
            }
        }

        private static string ReadBodyText(HttpResponseMessage response)
        {
            byte[] body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return Encoding.UTF8.GetString(body);
        }
    }
}
