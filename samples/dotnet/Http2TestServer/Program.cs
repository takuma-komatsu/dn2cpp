// Http2TestServer — REAL .NET ONLY, oracle infrastructure for
// gates/build-and-run-http2-unary.sh. This project is NEVER transpiled and does not
// enter any transpile input; the gate always runs it with `dotnet
// Http2TestServer.dll ...` as a plain external Kestrel process, the same way
// build-and-run-http-get.sh's local http.server / TLS python servers are oracle
// infrastructure rather than subject code.
//
// Listener registration order is the WHOLE CONTRACT with the gate: it reads the bound
// ports back off IServerAddressesFeature.Addresses after StartAsync. The concrete
// ServerAddressesFeature backs Addresses with a List<string>, and Kestrel's
// AddressBinder appends to it in the order the listeners were registered below — so
// index 0 is always the h2c (cleartext HTTP/2) listener, index 1 the plain HTTP/1.1
// one, and (only when a certificate was supplied) index 2 the TLS+ALPN one. Changing
// the registration order here without changing the gate silently swaps which port is
// which.
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

string certPath = null;
string keyPath = null;
for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--cert") certPath = args[i + 1];
    else if (args[i] == "--key") keyPath = args[i + 1];
}

var builder = WebApplication.CreateBuilder(args);
// Suppress all stdout except the one READY line this process is contracted to print:
// the default console logging provider would otherwise print the startup banner
// ("Now listening on: ...") and per-request info lines.
builder.Logging.ClearProviders();

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;

    // (1) h2c prior knowledge — cleartext, HTTP/2 only.
    options.Listen(IPAddress.Loopback, 0, lo => lo.Protocols = HttpProtocols.Http2);
    // (2) plain HTTP/1.1.
    options.Listen(IPAddress.Loopback, 0, lo => lo.Protocols = HttpProtocols.Http1);
    // (3) TLS + ALPN — only when a certificate was supplied on argv.
    if (certPath is not null && keyPath is not null)
    {
        options.Listen(IPAddress.Loopback, 0, lo =>
        {
            lo.Protocols = HttpProtocols.Http1AndHttp2;
            // Re-export through Pkcs12 into a fresh X509Certificate2: on macOS a
            // certificate loaded straight off PEM key files carries an ephemeral
            // private-key handle that SslStream/Kestrel cannot use for a handshake —
            // the same quirk build-and-run-http-get.sh's TLS section works around
            // (search that file for "ephemeral"), and the fix is identical: round-trip
            // the pair through PKCS#12.
            using X509Certificate2 pem = X509Certificate2.CreateFromPemFile(certPath, keyPath);
            X509Certificate2 cert = X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), null);
            lo.UseHttps(cert);
        });
    }
});

var app = builder.Build();

app.MapMethods("/echo", new[] { "GET", "POST" }, EchoHandler);
app.MapGet("/trailers", TrailersHandler);
app.MapGet("/connid", ConnIdHandler);
app.MapGet("/connhold", ConnHoldHandler);
app.MapGet("/holdstat", HoldStatHandler);
app.MapGet("/holdreset", HoldResetHandler);
app.MapGet("/slow", SlowHandler);
app.MapGet("/slowbody", SlowBodyHandler);
app.MapGet("/slowreset", SlowResetHandler);
app.MapGet("/slowstat", SlowStatHandler);
app.MapPost("/sink", SinkHandler);
app.MapGet("/drip", DripHandler);
app.MapGet("/dripstat", DripStatHandler);
app.MapGet("/dripreset", DripResetHandler);
app.MapPost("/earlyreply", EarlyReplyHandler);

await app.StartAsync();

IServerAddressesFeature addressesFeature = app.Services.GetRequiredService<IServer>()
    .Features.Get<IServerAddressesFeature>()!;
var ports = new List<int>();
foreach (string address in addressesFeature.Addresses)
    ports.Add(new Uri(address).Port);

var ready = new StringBuilder("READY");
foreach (int port in ports)
    ready.Append(' ').Append(port);
Console.Out.WriteLine(ready.ToString());
Console.Out.Flush();

await app.WaitForShutdownAsync();

// GET and POST /echo answer alike (empty body-hex on GET, since it carries no body):
// method/path/protocol/body-hex, plus two fixed response headers. `proto=` is the
// SERVER's own observation of what the connection spoke — the one line in this whole
// gate a client cannot fabricate.
static async Task EchoHandler(HttpContext context)
{
    string bodyHex = "";
    if (HttpMethods.IsPost(context.Request.Method))
    {
        using var ms = new MemoryStream();
        await context.Request.Body.CopyToAsync(ms);
        bodyHex = Convert.ToHexString(ms.ToArray()).ToLowerInvariant();
    }

    context.Response.Headers["X-Resp-One"] = "alpha";
    context.Response.Headers["X-Resp-Two"] = "beta";
    context.Response.ContentType = "text/plain";

    var sb = new StringBuilder();
    sb.Append("method=").Append(context.Request.Method).Append('\n');
    sb.Append("path=").Append(context.Request.Path.Value).Append('\n');
    sb.Append("proto=").Append(context.Request.Protocol).Append('\n');
    sb.Append("body-hex=").Append(bodyHex).Append('\n');
    await context.Response.WriteAsync(sb.ToString());
}

// GET /connid: the SERVER's own id for the connection this request arrived on. Two
// sequential requests reading the same id is the only proof of connection reuse a
// client cannot fabricate — the transport's own timings are self-reported. The id
// itself is random per connection, so the gate's subject prints only whether the two
// matched (see Http2Unary's ReuseOverH2c).
static async Task ConnIdHandler(HttpContext context)
{
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("conn=" + context.Connection.Id);
}

// GET /connhold?ms=N: /connid with a deliberate overlap window — hold the request
// open for N ms (default 800, capped), then answer with the server's own id for the
// connection it arrived on. A gauge counts concurrent holders and keeps its peak,
// because a distinct-connection count alone reads correct on a run whose requests
// never overlapped at all: peak is the SERVER's testimony of how many requests were
// in flight together, which is what the multiplexing and connection-cap sections
// (Http2Unary's [mux] / [maxconn]) stand on.
static async Task ConnHoldHandler(HttpContext context)
{
    int ms = 800;
    if (int.TryParse(context.Request.Query["ms"], out int q) && q > 0 && q <= 10000)
        ms = q;
    int now = Interlocked.Increment(ref HoldStats.Holding);
    int seen;
    while (now > (seen = Volatile.Read(ref HoldStats.Peak)) &&
           Interlocked.CompareExchange(ref HoldStats.Peak, now, seen) != seen)
    {
    }
    try
    {
        await Task.Delay(TimeSpan.FromMilliseconds(ms), context.RequestAborted);
    }
    finally
    {
        Interlocked.Decrement(ref HoldStats.Holding);
    }
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("conn=" + context.Connection.Id);
}

static Task HoldStatHandler(HttpContext context)
{
    context.Response.ContentType = "text/plain";
    return context.Response.WriteAsync("peak=" + Volatile.Read(ref HoldStats.Peak));
}

static Task HoldResetHandler(HttpContext context)
{
    Interlocked.Exchange(ref HoldStats.Peak, 0);
    context.Response.ContentType = "text/plain";
    return context.Response.WriteAsync("reset");
}

// GET /slow, /slowreset, /slowstat: the mid-flight-abort observation. /slow never
// answers within any client's patience; RequestAborted is what the h2 RST_STREAM (or a
// dropped connection) trips, so `aborted` counts transfers the CLIENT really tore down
// rather than waited out. The counters are process-wide and the gate runs its native and
// oracle subjects against ONE server, so a subject resets before it measures.
static async Task SlowHandler(HttpContext context)
{
    Interlocked.Increment(ref SlowStats.Started);
    try
    {
        // Long enough that a client waiting it out is a gate timeout, not a pass.
        await Task.Delay(TimeSpan.FromSeconds(120), context.RequestAborted);
        Interlocked.Increment(ref SlowStats.Completed);
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("slow-done");
    }
    catch (OperationCanceledException)
    {
        Interlocked.Increment(ref SlowStats.Aborted);
    }
}

// GET /slowbody: the response HEADER block, flushed, then a gap, then the body. The
// gap is what separates a transport that returns at the headers from one that returns
// at the end of the body — the two are indistinguishable against every other endpoint
// here, because those answer both at once.
static async Task SlowBodyHandler(HttpContext context)
{
    context.Response.ContentType = "text/plain";
    // StartAsync sends the header block on its own; without it Kestrel would buffer
    // and the whole point of the endpoint would be lost.
    await context.Response.StartAsync();
    await context.Response.Body.FlushAsync();
    await Task.Delay(TimeSpan.FromMilliseconds(1500), context.RequestAborted);
    await context.Response.WriteAsync("slow-body");
}

static Task SlowResetHandler(HttpContext context)
{
    Interlocked.Exchange(ref SlowStats.Started, 0);
    Interlocked.Exchange(ref SlowStats.Aborted, 0);
    Interlocked.Exchange(ref SlowStats.Completed, 0);
    context.Response.ContentType = "text/plain";
    return context.Response.WriteAsync("reset");
}

static Task SlowStatHandler(HttpContext context)
{
    context.Response.ContentType = "text/plain";
    return context.Response.WriteAsync(
        "started=" + Volatile.Read(ref SlowStats.Started) +
        "\naborted=" + Volatile.Read(ref SlowStats.Aborted) +
        "\ncompleted=" + Volatile.Read(ref SlowStats.Completed) + "\n");
}

// POST /sink: drain an unknown-length request body and answer with its length and a
// content hash. The hash is the point — a send queue that parks a writer and resumes
// it later can lose or reorder bytes in ways a length alone reads as correct, and the
// SERVER is the only party that can testify to what actually arrived. FNV-1a 64
// because it is four lines nobody has to trust a library for; hex, so the answer is
// culture-free.
static async Task SinkHandler(HttpContext context)
{
    ulong hash = 14695981039346656037UL;
    long len = 0;
    byte[] buf = new byte[65536];
    int n;
    while ((n = await context.Request.Body.ReadAsync(buf, 0, buf.Length)) > 0)
    {
        for (int i = 0; i < n; i++)
        {
            hash ^= buf[i];
            hash *= 1099511628211UL;
        }
        len += n;
    }
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("len=" + len + "\nsum=" + hash.ToString("x16") + "\n");
}

// GET /drip: the response header block, then body chunks FOREVER — until the client
// tears the stream down (RequestAborted). `aborted` counting up is the server's
// testimony that a dropped, never-read ResponseHeadersRead response was really torn
// down rather than parked until process exit. Chunks are paced so an unread client
// window fills without racing the counter reads.
static async Task DripHandler(HttpContext context)
{
    Interlocked.Increment(ref DripStats.Started);
    context.Response.ContentType = "application/octet-stream";
    try
    {
        await context.Response.StartAsync();
        var chunk = new byte[8192];
        for (;;)
        {
            await context.Response.Body.WriteAsync(chunk, 0, chunk.Length, context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
            await Task.Delay(50, context.RequestAborted);
        }
    }
    catch (Exception) when (context.RequestAborted.IsCancellationRequested)
    {
        Interlocked.Increment(ref DripStats.Aborted);
    }
}

static Task DripStatHandler(HttpContext context)
{
    context.Response.ContentType = "text/plain";
    return context.Response.WriteAsync(
        "started=" + Volatile.Read(ref DripStats.Started) +
        "\naborted=" + Volatile.Read(ref DripStats.Aborted) + "\n");
}

static Task DripResetHandler(HttpContext context)
{
    Interlocked.Exchange(ref DripStats.Started, 0);
    Interlocked.Exchange(ref DripStats.Aborted, 0);
    context.Response.ContentType = "text/plain";
    return context.Response.WriteAsync("reset");
}

// POST /earlyreply: a complete, legitimate response with the request body deliberately
// left UNDRAINED — 1 byte of 4 MiB. A server that answers before draining the request is
// ordinary HTTP (an auth failure, a validation error, a cache hit), and the client must
// surface the response, never an IOException about its own upload. The one byte is what
// makes the client's send path provably engaged rather than racing the reply.
static async Task EarlyReplyHandler(HttpContext context)
{
    byte[] one = new byte[1];
    int n = await context.Request.Body.ReadAsync(one, 0, 1, context.RequestAborted);
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("early-reply read=" + n);
}

// GET /trailers: a fixed body plus two trailers. HTTP trailers must be DECLARED
// before the body is written (DeclareTrailer, which stamps the h2/chunked framing
// that names them ahead of time) and APPENDED only after (AppendTrailer) — Kestrel
// throws if either order is violated.
static async Task TrailersHandler(HttpContext context)
{
    context.Response.DeclareTrailer("x-gate-trailer");
    context.Response.DeclareTrailer("x-gate-status");
    context.Response.ContentType = "text/plain";
    await context.Response.WriteAsync("trailer-body");
    context.Response.AppendTrailer("x-gate-trailer", "tv1");
    context.Response.AppendTrailer("x-gate-status", "0");
}

static class SlowStats
{
    public static int Started;
    public static int Aborted;
    public static int Completed;
}

static class HoldStats
{
    public static int Holding;
    public static int Peak;
}

static class DripStats
{
    public static int Started;
    public static int Aborted;
}
