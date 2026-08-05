using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Greet;
using Grpc.Core;
using Grpc.Net.Client;

namespace GrpcUnary
{
    // Transpiled gRPC client — unary (1)-(4) AND the three streaming shapes
    // (5)-(9): server streaming with headers/trailers/mid-stream failure/
    // mid-stream deadline, client streaming, and an INTERLOCKED bidi echo
    // (the incrementality proof: it deadlocks loudly on a buffering
    // transport). Run against the real .NET oracle server in
    // ../GrpcUnaryServer/. Every line printed below is server-derived and
    // deterministic (never message.ToString(), never a client-synthesized
    // exception message, never a timing) so the gate can diff this program's
    // native output against real .NET byte-for-byte.
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            try
            {
                string baseAddress = args[0];
                using var channel = GrpcChannel.ForAddress(baseAddress);
                var client = new Greeter.GreeterClient(channel);

                // (1) Plain unary call with a fully populated request.
                var request = new HelloRequest
                {
                    Name = "dn2cpp",
                    Count = 21,
                    Payload = ByteString.CopyFrom(new byte[] { 0xDE, 0xAD }),
                    Mood = HelloRequest.Types.Mood.Happy,
                };
                request.Values.Add(1);
                request.Values.Add(2);
                request.Values.Add(3);

                HelloReply reply = await client.SayHelloAsync(request);
                Console.WriteLine($"[unary] message={reply.Message}");
                Console.WriteLine($"[unary] doubled={reply.Doubled}");
                Console.WriteLine($"[unary] values={string.Join(",", reply.Values)}");
                Console.WriteLine($"[unary] payload={BitConverter.ToString(reply.Payload.ToByteArray())}");

                // (2) Headers + trailers on an explicit AsyncUnaryCall.
                var metaRequest = new HelloRequest { Name = "dn2cpp" };
                AsyncUnaryCall<HelloReply> call = client.SayHelloAsync(metaRequest);
                Metadata headers = await call.ResponseHeadersAsync;
                string headerValue = headers.GetValue("x-gate-header");
                await call.ResponseAsync;
                Metadata trailers = call.GetTrailers();
                string trailerValue = trailers.GetValue("x-gate-trailer");
                Console.WriteLine($"[meta] header x-gate-header={headerValue}");
                Console.WriteLine($"[meta] trailer x-gate-trailer={trailerValue}");

                // (3) Error status: server rejects the name and returns a
                // deterministic status, detail and trailer.
                try
                {
                    var failRequest = new HelloRequest { Name = "boom" };
                    await client.FailAsync(failRequest);
                }
                catch (RpcException e)
                {
                    Console.WriteLine($"[fail] status={e.StatusCode}");
                    Console.WriteLine($"[fail] detail={e.Status.Detail}");
                    Console.WriteLine($"[fail] trailer x-fail-detail={e.Trailers.GetValue("x-fail-detail")}");
                }

                // (4) Deadline: the server sleeps far longer than the client's
                // deadline, so the client must observe DeadlineExceeded.
                try
                {
                    var sleepRequest = new SleepRequest { Millis = 30000 };
                    await client.SleepAsync(sleepRequest, deadline: DateTime.UtcNow.AddMilliseconds(500));
                }
                catch (RpcException e)
                {
                    Console.WriteLine($"[deadline] status={e.StatusCode}");
                }

                // (5) Server streaming: response headers surface before the stream is
                // drained, every message arrives, trailers read after EOS.
                using (AsyncServerStreamingCall<HelloReply> sCall =
                    client.Countdown(new StreamRequest { Count = 4 }))
                {
                    Metadata sHeaders = await sCall.ResponseHeadersAsync;
                    Console.WriteLine($"[sstream] header x-stream-header={sHeaders.GetValue("x-stream-header")}");
                    while (await sCall.ResponseStream.MoveNext(CancellationToken.None))
                    {
                        HelloReply tick = sCall.ResponseStream.Current;
                        Console.WriteLine($"[sstream] {tick.Message} doubled={tick.Doubled}");
                    }
                    Console.WriteLine($"[sstream] trailer x-stream-trailer={sCall.GetTrailers().GetValue("x-stream-trailer")}");
                }

                // (6) Server streaming, mid-stream failure: two messages arrive,
                // then the server raises — status, detail and the exception trailer
                // must surface after a partially delivered body.
                using (AsyncServerStreamingCall<HelloReply> fCall =
                    client.Countdown(new StreamRequest { Count = 5, FailAfterTwo = true }))
                {
                    int got = 0;
                    try
                    {
                        while (await fCall.ResponseStream.MoveNext(CancellationToken.None))
                        {
                            got++;
                        }
                    }
                    catch (RpcException e)
                    {
                        Console.WriteLine($"[sfail] got={got} status={e.StatusCode}");
                        Console.WriteLine($"[sfail] detail={e.Status.Detail}");
                        Console.WriteLine($"[sfail] trailer x-sfail={e.Trailers.GetValue("x-sfail")}");
                    }
                }

                // (7) Client streaming: four messages up, one aggregated reply back.
                using (AsyncClientStreamingCall<HelloRequest, HelloReply> aCall = client.Accumulate())
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        await aCall.RequestStream.WriteAsync(new HelloRequest { Name = $"n{i}", Count = i });
                    }
                    await aCall.RequestStream.CompleteAsync();
                    HelloReply acc = await aCall.ResponseAsync;
                    Console.WriteLine($"[cstream] message={acc.Message}");
                    Console.WriteLine($"[cstream] doubled={acc.Doubled}");
                    Console.WriteLine($"[cstream] trailer x-acc-trailer={aCall.GetTrailers().GetValue("x-acc-trailer")}");
                }

                // (8) Bidi, INTERLOCKED: each echo is awaited before the next
                // message is sent, so this section only passes if both directions
                // flow incrementally — a transport that buffers either side
                // deadlocks here (and times out loudly), it cannot pass wrong.
                using (AsyncDuplexStreamingCall<HelloRequest, HelloReply> bCall = client.EchoChat())
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        await bCall.RequestStream.WriteAsync(new HelloRequest { Name = $"m{i}", Count = i });
                        bool ok = await bCall.ResponseStream.MoveNext(CancellationToken.None);
                        Console.WriteLine($"[bidi] ok={ok} {bCall.ResponseStream.Current.Message} doubled={bCall.ResponseStream.Current.Doubled}");
                    }
                    await bCall.RequestStream.CompleteAsync();
                    bool end = await bCall.ResponseStream.MoveNext(CancellationToken.None);
                    Console.WriteLine($"[bidi] end={end}");
                    Console.WriteLine($"[bidi] trailer x-chat-trailer={bCall.GetTrailers().GetValue("x-chat-trailer")}");
                }

                // (9) Deadline MID-STREAM: the first tick arrives immediately, the
                // server then sleeps past the deadline — the fired token must abort
                // the in-flight stream (got=1), not fail the call before it (got=0)
                // nor wait the server out.
                using (AsyncServerStreamingCall<HelloReply> dCall = client.Countdown(
                    new StreamRequest { Count = 3, DelayMillis = 30000 },
                    deadline: DateTime.UtcNow.AddMilliseconds(500)))
                {
                    int got = 0;
                    try
                    {
                        while (await dCall.ResponseStream.MoveNext(CancellationToken.None))
                        {
                            got++;
                        }
                    }
                    catch (RpcException e)
                    {
                        Console.WriteLine($"[sdeadline] got={got} status={e.StatusCode}");
                    }
                }

                // (10) Descriptor reflection, local to the client — no server.
                // JsonFormatter, message ToString() and descriptor-driven field access
                // ride the reflection accessors (FieldDescriptor/OneofDescriptor.
                // CreateAccessor -> GetProperty/GetMethod + MethodInfo.Invoke), over both
                // an app-module message (HelloRequest) and a library one
                // (WellKnownTypes.Value, whose Clear*/Has* members nothing calls
                // statically). Deterministic: JsonFormatter is culture-invariant and
                // MapField preserves insertion order.
                var reflRequest = new HelloRequest { Name = "refl", Count = 7, Mood = HelloRequest.Types.Mood.Happy };
                reflRequest.Values.Add(5);
                Console.WriteLine($"[refl] tostring={reflRequest}");
                var nameField = HelloRequest.Descriptor.FindFieldByName("name");
                Console.WriteLine($"[refl] get name={nameField.Accessor.GetValue(reflRequest)}");
                nameField.Accessor.SetValue(reflRequest, "set-by-accessor");
                Console.WriteLine($"[refl] set name={reflRequest.Name}");
                nameField.Accessor.Clear(reflRequest);
                Console.WriteLine($"[refl] cleared name='{reflRequest.Name}'");
                var value = Value.ForString("vs");
                Console.WriteLine($"[refl] value tostring={value}");
                var oneof = Value.Descriptor.Oneofs[0];
                Console.WriteLine($"[refl] oneof {oneof.Name} case={oneof.Accessor.GetCaseFieldDescriptor(value).Name}");
                var stringValueField = Value.Descriptor.FindFieldByName("string_value");
                Console.WriteLine($"[refl] string_value={stringValueField.Accessor.GetValue(value)} has={stringValueField.Accessor.HasValue(value)}");
                oneof.Accessor.Clear(value); // ClearKind via MethodInfo.Invoke
                Console.WriteLine($"[refl] after clear case={(oneof.Accessor.GetCaseFieldDescriptor(value) is { } caseField ? caseField.Name : "none")}");
                var payload = new Struct();
                payload.Fields["num"] = Value.ForNumber(1.5);
                payload.Fields["flag"] = Value.ForBool(true);
                payload.Fields["text"] = Value.ForString("t");
                Console.WriteLine($"[refl] struct={JsonFormatter.Default.Format(payload)}");

                return;
            }
            catch (Exception ex)
            {
                // Full detail to stderr only: stdout stays deterministic for the diff.
                Console.WriteLine($"UNEXPECTED {ex.GetType().Name}");
                Console.Error.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
        }
    }
}
