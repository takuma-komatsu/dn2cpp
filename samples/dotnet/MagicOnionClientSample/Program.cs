using System;
using System.Buffers;
using System.Globalization;
using System.Threading.Tasks;
using Grpc.Core;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization.MessagePack;
using MessagePack;
using MessagePack.Resolvers;

namespace MagicOnionClientSample;

public interface IArithmeticService : IService<IArithmeticService>
{
    UnaryResult<int> SumAsync(int left, string right);
}

[MagicOnionClientGeneration(typeof(IArithmeticService))]
public partial class MagicOnionClientGeneratedInitializer;

internal sealed class MockCallInvoker : CallInvoker
{
    public byte[] RequestPayload { get; private set; } = Array.Empty<byte>();
    public byte[] ResponsePayload { get; set; } = Array.Empty<byte>();

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request)
    {
        var serializationContext = new MockSerializationContext();
        method.RequestMarshaller.ContextualSerializer(request, serializationContext);
        RequestPayload = serializationContext.ToMemory().ToArray();

        TResponse response = method.ResponseMarshaller.ContextualDeserializer(
            new MockDeserializationContext(ResponsePayload));
        return new AsyncUnaryCall<TResponse>(
            Task.FromResult(response),
            Task.FromResult(Metadata.Empty),
            static () => Status.DefaultSuccess,
            static () => Metadata.Empty,
            static () => { });
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request) => throw new NotImplementedException();

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options,
        TRequest request) => throw new NotImplementedException();

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options) => throw new NotImplementedException();

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        Method<TRequest, TResponse> method,
        string? host,
        CallOptions options) => throw new NotImplementedException();
}

internal sealed class MockSerializationContext : SerializationContext
{
    private readonly ArrayBufferWriter<byte> _writer = new();

    public override IBufferWriter<byte> GetBufferWriter() => _writer;

    public override void Complete(byte[] payload)
    {
        _writer.Clear();
        _writer.Write(payload);
    }

    public override void Complete()
    {
    }

    public ReadOnlyMemory<byte> ToMemory() => _writer.WrittenMemory;
}

internal sealed class MockDeserializationContext(byte[] payload) : DeserializationContext
{
    public override int PayloadLength => payload.Length;
    public override byte[] PayloadAsNewBuffer() => (byte[])payload.Clone();
    public override ReadOnlySequence<byte> PayloadAsReadOnlySequence() => new(payload);
}

internal static class Program
{
    private static async Task Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var resolvers = CompositeResolver.Create(
            MagicOnionClientGeneratedInitializer.Resolver,
            StandardResolver.Instance);
        var serializer = MessagePackMagicOnionSerializerProvider.Default.WithOptions(
            MessagePackSerializerOptions.Standard.WithResolver(resolvers));
        var invoker = new MockCallInvoker
        {
            ResponsePayload = MessagePackSerializer.Serialize(67890),
        };
        IArithmeticService client = MagicOnionClient.Create<IArithmeticService>(invoker, serializer);

        UnaryResult<int> call = client.SumAsync(12345, "Hello");
        int result = await call;

        Console.WriteLine("request=" + BitConverter.ToString(invoker.RequestPayload));
        Console.WriteLine("response=" + result);
    }
}
