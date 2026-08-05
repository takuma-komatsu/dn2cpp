// GATE INFRASTRUCTURE — the real .NET oracle server for
// samples/dotnet/GrpcUnary/'s transpiled gRPC client. This program is never
// transpiled; it runs only as a plain .NET process the gate starts, dials,
// and tears down. Every response field below is a deterministic function of
// the request, so the client's captured output can be diffed byte-for-byte
// against a frozen expectation.
using System;
using System.Net;
using System.Threading.Tasks;
using Google.Protobuf;
using Greet;
using Grpc.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GrpcUnaryServer
{
    internal sealed class GreeterService : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return SayHelloCore(request, context);
        }

        private static async Task<HelloReply> SayHelloCore(HelloRequest request, ServerCallContext context)
        {
            await context.WriteResponseHeadersAsync(new Metadata { { "x-gate-header", "hv1" } });
            context.ResponseTrailers.Add("x-gate-trailer", "tv1");

            var reply = new HelloReply
            {
                Message = $"Hello {request.Name}",
                Doubled = request.Count * 2,
            };
            foreach (int v in request.Values)
            {
                reply.Values.Add(v + 10);
            }
            byte[] source = request.Payload.ToByteArray();
            byte[] reversed = new byte[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                reversed[i] = source[source.Length - 1 - i];
            }
            reply.Payload = ByteString.CopyFrom(reversed);
            return reply;
        }

        public override Task<HelloReply> Fail(HelloRequest request, ServerCallContext context)
        {
            throw new RpcException(
                new Status(StatusCode.InvalidArgument, "name is not allowed: " + request.Name),
                new Metadata { { "x-fail-detail", "fd1" } });
        }

        public override async Task<HelloReply> Sleep(SleepRequest request, ServerCallContext context)
        {
            await Task.Delay(request.Millis, context.CancellationToken);
            return new HelloReply();
        }

        // Server streaming: headers first, one tick per count, trailers at the end.
        // DelayMillis (before every tick after the first) serves the client's
        // mid-stream-deadline section; FailAfterTwo the mid-stream-failure one.
        public override async Task Countdown(StreamRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {
            await context.WriteResponseHeadersAsync(new Metadata { { "x-stream-header", "sh1" } });
            for (int i = 0; i < request.Count; i++)
            {
                if (i > 0 && request.DelayMillis > 0)
                {
                    await Task.Delay(request.DelayMillis, context.CancellationToken);
                }
                if (request.FailAfterTwo && i == 2)
                {
                    throw new RpcException(
                        new Status(StatusCode.Unavailable, "failed after two ticks"),
                        new Metadata { { "x-sfail", "sf1" } });
                }
                await responseStream.WriteAsync(new HelloReply { Message = $"tick {i}", Doubled = i * 2 });
            }
            context.ResponseTrailers.Add("x-stream-trailer", "st1");
        }

        // Client streaming: aggregate everything, answer once.
        public override async Task<HelloReply> Accumulate(IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
        {
            int sum = 0;
            var names = new System.Collections.Generic.List<string>();
            while (await requestStream.MoveNext())
            {
                sum += requestStream.Current.Count;
                names.Add(requestStream.Current.Name);
            }
            context.ResponseTrailers.Add("x-acc-trailer", "at1");
            return new HelloReply { Message = "acc " + string.Join("+", names), Doubled = sum * 2 };
        }

        // Bidi: echo each message as it arrives — the client interlocks on these
        // echoes, so this must not buffer the request stream.
        public override async Task EchoChat(IAsyncStreamReader<HelloRequest> requestStream, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                await responseStream.WriteAsync(new HelloReply
                {
                    Message = $"echo {requestStream.Current.Name}",
                    Doubled = requestStream.Current.Count * 2,
                });
            }
            context.ResponseTrailers.Add("x-chat-trailer", "ct1");
        }
    }

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.ClearProviders();

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Loopback, 0, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });

            builder.Services.AddGrpc();

            var app = builder.Build();

            app.MapGrpcService<GreeterService>();

            await app.StartAsync();

            var addressesFeature = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
                .Features.Get<IServerAddressesFeature>();
            string address = null;
            foreach (string candidate in addressesFeature.Addresses)
            {
                address = candidate;
                break;
            }
            int port = new Uri(address).Port;

            Console.WriteLine($"READY {port}");
            Console.Out.Flush();

            await app.WaitForShutdownAsync();
        }
    }
}
