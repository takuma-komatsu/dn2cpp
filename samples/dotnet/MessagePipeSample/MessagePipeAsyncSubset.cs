using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePipeAsyncSubset;

public static class Program
{
    public static void __GateEntry()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe(options => options.EnableAutoRegistration = false);

        var provider = services.BuildServiceProvider();

        TestAsyncPubSub(provider).GetAwaiter().GetResult();
        TestKeyedAsyncPubSub(provider).GetAwaiter().GetResult();
        TestBufferedAsyncPubSub(provider).GetAwaiter().GetResult();
        TestAsyncEventHelper().GetAwaiter().GetResult();
    }

    private static async Task TestAsyncPubSub(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IAsyncPublisher<string>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<string>>();

        var received = new List<string>();

        var sub1 = subscriber.Subscribe((string msg, CancellationToken ct) =>
        {
            received.Add("async1:" + msg);
            return ValueTask.CompletedTask;
        });

        var sub2 = subscriber.Subscribe((string msg, CancellationToken ct) =>
        {
            received.Add("async2:" + msg);
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync("first");
        sub1.Dispose();

        await publisher.PublishAsync("second");
        sub2.Dispose();

        await publisher.PublishAsync("third");

        Console.WriteLine("[async-pubsub] " + string.Join(",", received));
    }

    private static async Task TestKeyedAsyncPubSub(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IAsyncPublisher<string, string>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<string, string>>();

        var received = new List<string>();

        var sub1 = subscriber.Subscribe("k1", (string msg, CancellationToken ct) =>
        {
            received.Add("k1:" + msg);
            return ValueTask.CompletedTask;
        });

        var sub2 = subscriber.Subscribe("k2", (string msg, CancellationToken ct) =>
        {
            received.Add("k2:" + msg);
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync("k1", "msgA");
        await publisher.PublishAsync("k2", "msgB");
        await publisher.PublishAsync("k1", "msgC");

        sub1.Dispose();
        await publisher.PublishAsync("k1", "msgD");
        await publisher.PublishAsync("k2", "msgE");
        sub2.Dispose();

        Console.WriteLine("[async-keyed] " + string.Join(",", received));
    }

    private static async Task TestBufferedAsyncPubSub(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IBufferedAsyncPublisher<string>>();
        var subscriber = provider.GetRequiredService<IBufferedAsyncSubscriber<string>>();

        await publisher.PublishAsync("init-async-val");

        var received = new List<string>();
        var sub = await subscriber.SubscribeAsync((string msg, CancellationToken ct) =>
        {
            received.Add("b-async:" + msg);
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync("second-async-val");
        sub.Dispose();

        Console.WriteLine("[async-buffered] " + string.Join(",", received));
    }

    private static async Task TestAsyncEventHelper()
    {
        var (pub, sub) = GlobalMessagePipe.CreateAsyncEvent<int>();
        var received = new List<int>();

        using (sub.Subscribe((int x, CancellationToken ct) =>
        {
            received.Add(x * 10);
            return ValueTask.CompletedTask;
        }))
        {
            await pub.PublishAsync(5);
            await pub.PublishAsync(6);
        }

        await pub.PublishAsync(7);
        pub.Dispose();

        Console.WriteLine("[async-event] " + string.Join(",", received));
    }
}
