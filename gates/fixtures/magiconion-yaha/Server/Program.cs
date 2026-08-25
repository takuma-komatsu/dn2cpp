using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.ConfigureKestrel(options =>
    options.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2));
var app = builder.Build();
var requestCount = 0;

app.MapPost("/IArithmeticService/SumAsync", async context =>
{
    Console.WriteLine($"REQUEST {Interlocked.Increment(ref requestCount)}");
    Console.Out.Flush();
    string encoded = context.Request.Headers["x-gate-magiconion-payload"].ToString();
    if (encoded.Length == 0)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }
    byte[] framed = Convert.FromBase64String(encoded);
    if (framed.Length < 6 || framed[0] != 0 ||
        BinaryPrimitives.ReadInt32BigEndian(framed.AsSpan(1, 4)) != framed.Length - 5)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    ReadOnlySpan<byte> payload = framed.AsSpan(5);
    int result = payload.SequenceEqual(new byte[] { 0x92, 0xcd, 0x30, 0x39, 0xa5, (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' })
        ? 67890
        : payload.SequenceEqual(new byte[] { 0x92, 0x07, 0xa5, (byte)'a', (byte)'g', (byte)'a', (byte)'i', (byte)'n' })
            ? 2468
            : -1;
    if (result < 0)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    byte[] response = new byte[10];
    BinaryPrimitives.WriteInt32BigEndian(response.AsSpan(1, 4), 5);
    response[5] = 0xce;
    BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(6, 4), (uint)result);
    context.Response.ContentType = "application/grpc";
    context.Response.DeclareTrailer("grpc-status");
    context.Response.DeclareTrailer("x-server-result");
    await context.Response.Body.WriteAsync(response);
    context.Response.AppendTrailer("grpc-status", "0");
    context.Response.AppendTrailer("x-server-result", result.ToString(CultureInfo.InvariantCulture));
});

await app.StartAsync();
var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
string address = addresses!.Addresses.Single();
Console.WriteLine($"READY {new Uri(address).Port}");
Console.Out.Flush();
await app.WaitForShutdownAsync();
