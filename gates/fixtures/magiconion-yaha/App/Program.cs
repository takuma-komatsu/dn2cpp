using System;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Net.Http;
using Grpc.Core;
using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using MagicOnion.Serialization.MessagePack;
using MessagePack;
using MessagePack.Resolvers;

namespace MagicOnionYahaGate;

public interface IArithmeticService : IService<IArithmeticService>
{
    UnaryResult<int> SumAsync(int left, string right);
}

[MagicOnionClientGeneration(typeof(IArithmeticService))]
public partial class MagicOnionClientGeneratedInitializer;

internal sealed class HeaderFramedUnaryHandler(HttpMessageHandler transport) : DelegatingHandler(transport)
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Keep generated serialization while avoiding YAHH's asynchronous request-body pipe.
        if (request.Content is { } content)
        {
            byte[] body = await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            request.Headers.TryAddWithoutValidation(
                "x-gate-magiconion-payload",
                Convert.ToBase64String(body));
            request.Content = null;
            content.Dispose();
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}

internal static class Program
{
    private static async Task Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var address = new Uri(args[0]);
        var handler = new HeaderFramedUnaryHandler(
            new YetAnotherHttpHandler { Http2Only = true });
        using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler,
            DisposeHttpClient = true,
        });
        var resolvers = CompositeResolver.Create(
            MagicOnionClientGeneratedInitializer.Resolver,
            StandardResolver.Instance);
        var serializer = MessagePackMagicOnionSerializerProvider.Default.WithOptions(
            MessagePackSerializerOptions.Standard.WithResolver(resolvers));
        IArithmeticService client = MagicOnionClient.Create<IArithmeticService>(
            channel.CreateCallInvoker(),
            serializer);

        UnaryResult<int> firstCall;
        int first;
        Metadata firstTrailers;
        using (var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            firstCall = client.WithCancellationToken(cancellation.Token).SumAsync(12345, "Hello");
            first = await firstCall;
            firstTrailers = firstCall.GetTrailers();
        }

        UnaryResult<int> secondCall;
        int second;
        Metadata secondTrailers;
        using (var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
        {
            secondCall = client.WithCancellationToken(cancellation.Token).SumAsync(7, "again");
            second = await secondCall;
            secondTrailers = secondCall.GetTrailers();
        }

        Console.WriteLine($"magiconion first={first}");
        Console.WriteLine($"magiconion second={second}");
        Console.WriteLine($"status={firstCall.GetStatus().StatusCode},{secondCall.GetStatus().StatusCode}");
        Console.WriteLine($"trailers={firstTrailers.GetValue("x-server-result")},{secondTrailers.GetValue("x-server-result")}");
        Console.WriteLine("transport=YetAnotherHttpHandler");
    }
}
