using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Globalization;

namespace HttpBeyondGet
{
    // The System.Net.Http surface BEYOND the plain GET that HttpReal covers: POST/PUT
    // bodies (binary and typed text), DELETE, request-header emission (custom,
    // multi-value, content headers), response-header readback (the message-vs-content
    // split), and non-2xx statuses (404/500/201+Location/204). Under real .NET this
    // runs the actual SocketsHttpHandler; transpiled it runs the libcurl-backed DnHttp
    // transport. The gate runs both against the same local echo server and diffs the
    // outputs byte for byte, so everything printed is response-derived and
    // deterministic — the base URL (which carries the ephemeral port) is never
    // printed, and the echo server masks the port inside the Host header.
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            string baseUrl = args.Length > 0
                ? args[0]
                : Environment.GetEnvironmentVariable("DN2CPP_HTTP_BASE") ?? "";
            if (baseUrl.Length == 0)
            {
                Console.Error.WriteLine("usage: HttpBeyondGet <base-url>   (e.g. http://127.0.0.1:8080)");
                return 2;
            }

            using var client = new HttpClient();
            PostBinary(client, baseUrl);
            PostChunked(client, baseUrl);
            PostJson(client, baseUrl);
            PutText(client, baseUrl);
            DeleteNoBody(client, baseUrl);
            RequestHeaders(client, baseUrl);
            ResponseHeaders(client, baseUrl);
            Status404(client, baseUrl);
            Status500(client, baseUrl);
            Created201(client, baseUrl);
            NoContent204(client, baseUrl);
            PostZeroLength(client, baseUrl);
            HeadNoBody(client, baseUrl);

            return 0;
        }

        // POST a body with non-text bytes (zeros, the high bit) and a size past 1 KiB —
        // large enough that a transport tempted to volunteer `Expect: 100-continue`
        // would do it here. The echoed request (method, sorted headers, body hex) is
        // the assertion: the body must arrive byte-exact, and the header set must be
        // exactly what real .NET sends — no transport-invented Content-Type or Expect.
        private static void PostBinary(HttpClient client, string baseUrl)
        {
            byte[] payload = new byte[2000];
            for (int i = 0; i < payload.Length; i++)
                payload[i] = (byte)(i * 7 + 13);
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/echo")
            {
                Content = new ByteArrayContent(payload),
            };
            Send("post-binary", client, request);
        }

        // POST a body whose length is UNKNOWN before it is sent — the only one in this
        // file, and a different transport path from every other: no Content-Length, a
        // chunked HTTP/1.1 framing, and the body fed to the transport incrementally
        // rather than handed over whole. The combination this pins is synchronous
        // Send + unknown length, which nothing else in the suite reaches: gRPC is the
        // other unknown-length caller and it is always async. A non-seekable stream is
        // what makes the length unknown — StreamContent computes one from a seekable
        // stream and this would be an ordinary Content-Length body.
        private static void PostChunked(HttpClient client, string baseUrl)
        {
            byte[] payload = new byte[300];
            for (int i = 0; i < payload.Length; i++)
                payload[i] = (byte)(i * 3 + 1);
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/echo")
            {
                Content = new StreamContent(new UnseekableStream(payload)),
            };
            Send("post-chunked", client, request);
        }

        // A read-only forward-only view of a byte[]: everything StreamContent needs and
        // nothing it could compute a length from.
        private sealed class UnseekableStream : System.IO.Stream
        {
            private readonly System.IO.MemoryStream _inner;

            public UnseekableStream(byte[] data) => _inner = new System.IO.MemoryStream(data);

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
                => _inner.Read(buffer, offset, count);

            public override void Flush()
            {
            }

            public override long Seek(long offset, System.IO.SeekOrigin origin)
                => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count)
                => throw new NotSupportedException();
        }

        // POST typed text: StringContent stamps `Content-Type: application/json;
        // charset=utf-8` on the content, and the echo shows whether it reached the wire.
        private static void PostJson(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/echo")
            {
                Content = new StringContent("{\"answer\":42}", Encoding.UTF8, "application/json"),
            };
            Send("post-json", client, request);
        }

        private static void PutText(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, baseUrl + "/echo")
            {
                Content = new StringContent("putted payload", Encoding.UTF8, "text/plain"),
            };
            Send("put-text", client, request);
        }

        // DELETE carries no content at all: the bodyless CUSTOMREQUEST path, and the
        // echo proves no Content-Length/Content-Type is invented for it.
        private static void DeleteNoBody(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, baseUrl + "/echo");
            Send("delete", client, request);
        }

        // Custom request headers on both stores: message headers (single- and
        // multi-valued) plus a custom content header. The echo shows the exact wire
        // form, including how a multi-valued header is folded.
        private static void RequestHeaders(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/echo")
            {
                Content = new ByteArrayContent(Encoding.ASCII.GetBytes("hdrs")),
            };
            request.Headers.TryAddWithoutValidation("X-Req-One", "first value");
            request.Headers.TryAddWithoutValidation("X-Req-Two", "second-value");
            request.Headers.TryAddWithoutValidation("X-Req-Multi", "m1");
            request.Headers.TryAddWithoutValidation("X-Req-Multi", "m2");
            request.Content.Headers.TryAddWithoutValidation("X-Content-Extra", "on-the-content");
            Send("req-headers", client, request);
        }

        // Response-header readback: the server sends X-Resp-* (one multi-valued) plus
        // an explicit Content-Type. Message headers and content headers are printed
        // SEPARATELY — the split is what proves the readback routed each header to the
        // collection HttpResponseMessage exposes it on.
        private static void ResponseHeaders(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/resphdr");
            using HttpResponseMessage response = client.Send(request);
            PrintStatus("resphdr", response);
            var lines = new List<string>();
            foreach (KeyValuePair<string, IEnumerable<string>> h in response.Headers)
                lines.Add(h.Key + "=" + JoinValues(h.Value));
            PrintSorted("resphdr msg-header", lines);
            lines.Clear();
            foreach (KeyValuePair<string, IEnumerable<string>> h in response.Content.Headers)
                lines.Add(h.Key + "=" + JoinValues(h.Value));
            PrintSorted("resphdr content-header", lines);
            Console.WriteLine("resphdr body=" + ReadBodyText(response).TrimEnd('\n'));
        }

        // A 404 is a perfectly good HttpResponseMessage — status, reason and body all
        // read back — and EnsureSuccessStatusCode is what turns it into the typed
        // HttpRequestException carrying the status.
        private static void Status404(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/status/404");
            using HttpResponseMessage response = client.Send(request);
            PrintStatus("s404", response);
            Console.WriteLine("s404 body=" + ReadBodyText(response).TrimEnd('\n'));
            try
            {
                response.EnsureSuccessStatusCode();
                Console.WriteLine("s404 ensure=NO-THROW");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine("s404 ensure=HttpRequestException status="
                    + (ex.StatusCode.HasValue ? ((int)ex.StatusCode.Value).ToString() : "(none)"));
            }
        }

        private static void Status500(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/status/500");
            using HttpResponseMessage response = client.Send(request);
            PrintStatus("s500", response);
            Console.WriteLine("s500 body=" + ReadBodyText(response).TrimEnd('\n'));
        }

        // 201 Created with a relative Location — a response header parsed into a Uri,
        // and one HttpClient must NOT follow (only 3xx redirects are).
        private static void Created201(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/created")
            {
                Content = new StringContent("make one", Encoding.UTF8, "text/plain"),
            };
            using HttpResponseMessage response = client.Send(request);
            PrintStatus("s201", response);
            Console.WriteLine("s201 location=" + (response.Headers.Location?.ToString() ?? "(none)"));
            Console.WriteLine("s201 body=" + ReadBodyText(response).TrimEnd('\n'));
        }

        // 204 No Content: a success status whose response has no body by definition.
        private static void NoContent204(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/nocontent");
            using HttpResponseMessage response = client.Send(request);
            PrintStatus("s204", response);
            byte[] body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            Console.WriteLine("s204 len=" + body.Length);
        }

        // A non-null ZERO-LENGTH content still frames the request (`Content-Length: 0` on
        // the wire): a zero-byte body and NO body are different HTTP messages and servers
        // route on the difference. The echo's sorted header lines are the assertion — a
        // transport that skips the framing at len==0 is missing the content-length line
        // here, invisibly to every non-empty POST above.
        private static void PostZeroLength(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/echo")
            {
                Content = new ByteArrayContent(Array.Empty<byte>()),
            };
            Send("post-zerolen", client, request);
        }

        // HEAD: the response carries the headers of the GET it mirrors — including that
        // GET's Content-Length — and, by definition, no body. The TRANSPORT must know no
        // body follows (curl needs CURLOPT_NOBODY, since a custom verb alone changes only
        // the wire spelling); one that waits for the advertised bytes stalls here forever
        // on a keep-alive connection rather than failing loudly.
        private static void HeadNoBody(HttpClient client, string baseUrl)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, baseUrl + "/resphdr");
            using HttpResponseMessage response = client.Send(request);
            PrintStatus("head", response);
            var lines = new List<string>();
            foreach (KeyValuePair<string, IEnumerable<string>> h in response.Content.Headers)
                lines.Add(h.Key + "=" + JoinValues(h.Value));
            PrintSorted("head content-header", lines);
            byte[] body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            Console.WriteLine("head len=" + body.Length);
        }

        // ── plumbing ──────────────────────────────────────────────────────────────

        private static void Send(string label, HttpClient client, HttpRequestMessage request)
        {
            using HttpResponseMessage response = client.Send(request);
            PrintStatus(label, response);
            // The echo body is ASCII lines (headers sorted server-side, body as hex);
            // print it verbatim under a per-line prefix so a diff names the section.
            string text = ReadBodyText(response);
            foreach (string line in text.Split('\n'))
                if (line.Length > 0)
                    Console.WriteLine(label + " | " + line);
        }

        private static void PrintStatus(string label, HttpResponseMessage response)
        {
            Console.WriteLine(label + " status=" + (int)response.StatusCode
                + " reason=" + (response.ReasonPhrase ?? "(none)"));
        }

        private static string ReadBodyText(HttpResponseMessage response)
        {
            byte[] body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            return Encoding.UTF8.GetString(body);
        }

        private static string JoinValues(IEnumerable<string> values)
        {
            var sb = new StringBuilder();
            foreach (string v in values)
            {
                if (sb.Length > 0)
                    sb.Append('|');
                sb.Append(v);
            }
            return sb.ToString();
        }

        // Ordinal insertion sort: header enumeration order is an implementation detail
        // of each header store, so both lanes print a canonical order instead.
        private static void PrintSorted(string prefix, List<string> lines)
        {
            for (int i = 1; i < lines.Count; i++)
            {
                string x = lines[i];
                int j = i - 1;
                while (j >= 0 && string.CompareOrdinal(lines[j], x) > 0)
                {
                    lines[j + 1] = lines[j];
                    j--;
                }
                lines[j + 1] = x;
            }
            foreach (string line in lines)
                Console.WriteLine(prefix + " " + line);
        }
    }
}
