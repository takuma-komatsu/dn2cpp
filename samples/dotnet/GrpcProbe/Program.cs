using System;
using System.Globalization;
using System.Threading.Tasks;
using Google.Protobuf;
using Greet;
using Grpc.Core;
using Grpc.Net.Client;

namespace GrpcProbe
{
    // A REACH probe for the Grpc.Net.Client + Google.Protobuf stack, not a round-trip
    // test. There is no server behind 127.0.0.1:1, so every call below fails
    // (Unavailable, or DeadlineExceeded for the short-deadline one) — that is expected.
    // The point is to walk dn2cpp's --measure over the client-side call machinery
    // (channel construction, unary invocation, deadlines, headers/trailers,
    // cancellation) and the generated message code to see what is not yet supported.
    internal static class Program
    {
        private static async Task Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            using var channel = GrpcChannel.ForAddress("http://127.0.0.1:1");
            var client = new Greeter.GreeterClient(channel);

            // (a) A plain unary call with a fully populated request, printing the
            // reply's fields explicitly (never relying on IMessage.ToString()).
            try
            {
                var request = new HelloRequest
                {
                    Name = "probe",
                    Count = 3,
                    Payload = ByteString.CopyFrom(new byte[] { 1, 2, 3, 4 }),
                    Mood = HelloRequest.Types.Mood.Happy,
                };
                request.Values.Add(10);
                request.Values.Add(20);
                request.Values.Add(30);

                HelloReply reply = await client.SayHelloAsync(request);
                Console.WriteLine($"message={reply.Message}");
                Console.WriteLine($"doubled={reply.Doubled}");
                Console.WriteLine($"values.count={reply.Values.Count}");
                Console.WriteLine($"payload.length={reply.Payload.Length}");
            }
            catch (RpcException e)
            {
                Console.WriteLine($"rpc-status={e.StatusCode}");
            }

            // (b) A call bounded by a short absolute deadline.
            try
            {
                var request = new HelloRequest { Name = "deadline-probe" };
                var options = new CallOptions(deadline: DateTime.UtcNow.AddMilliseconds(200));
                HelloReply reply = await client.SayHelloAsync(request, options);
                Console.WriteLine($"message={reply.Message}");
            }
            catch (RpcException e)
            {
                Console.WriteLine($"rpc-status={e.StatusCode}");
            }

            // (c) A call whose RpcException trailers are read.
            try
            {
                var request = new HelloRequest { Name = "fail-probe" };
                await client.FailAsync(request);
            }
            catch (RpcException e)
            {
                Console.WriteLine($"rpc-status={e.StatusCode}");
                string probeTrailer = e.Trailers.GetValue("x-probe");
                Console.WriteLine($"trailer.x-probe={probeTrailer ?? "(none)"}");
            }

            // (d) An AsyncUnaryCall whose ResponseHeadersAsync and GetTrailers()
            // are both read.
            try
            {
                var request = new SleepRequest { Millis = 50 };
                AsyncUnaryCall<HelloReply> call = client.SleepAsync(request);
                Metadata headers = await call.ResponseHeadersAsync;
                Console.WriteLine($"headers.count={headers.Count}");
                HelloReply reply = await call;
                Console.WriteLine($"message={reply.Message}");
                Metadata trailers = call.GetTrailers();
                Console.WriteLine($"trailers.count={trailers.Count}");
            }
            catch (RpcException e)
            {
                Console.WriteLine($"rpc-status={e.StatusCode}");
            }

            return;
        }
    }
}
