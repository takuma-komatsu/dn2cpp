using System;
using System.Collections.Generic;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePipePubSubSubset;

public sealed class IntMessage
{
    public int Value { get; set; }

    public IntMessage(int value)
    {
        Value = value;
    }
}

public sealed class FilterMessage
{
    public int Value { get; set; }

    public FilterMessage(int value)
    {
        Value = value;
    }
}

public static class Program
{
    public static void __GateEntry()
    {
        var services = new ServiceCollection();
        services.AddMessagePipe(options => options.EnableAutoRegistration = false);

        var provider = services.BuildServiceProvider();
        GlobalMessagePipe.SetProvider(provider);

        TestBasicPubSub(provider);
        TestDisposableBag(provider);
        TestFilteredSubscription(provider);
        TestKeyedPubSub(provider);
        TestBufferedPubSub(provider);
        TestEventHelper();
    }

    private static void TestBasicPubSub(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IPublisher<string>>();
        var subscriber = provider.GetRequiredService<ISubscriber<string>>();

        var received = new List<string>();
        var sub1 = subscriber.Subscribe(msg => received.Add("s1:" + msg));
        var sub2 = subscriber.Subscribe(msg => received.Add("s2:" + msg));

        publisher.Publish("hello");
        sub1.Dispose();

        publisher.Publish("world");
        sub2.Dispose();

        publisher.Publish("ignored");

        Console.WriteLine("[pubsub-basic] " + string.Join(",", received));
    }

    private static void TestDisposableBag(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IPublisher<IntMessage>>();
        var subscriber = provider.GetRequiredService<ISubscriber<IntMessage>>();

        var bag = DisposableBag.CreateBuilder();
        var received = new List<int>();

        subscriber.Subscribe(msg => received.Add(msg.Value * 2)).AddTo(bag);
        subscriber.Subscribe(msg => received.Add(msg.Value * 3)).AddTo(bag);

        var disposable = bag.Build();

        publisher.Publish(new IntMessage(10));
        disposable.Dispose();

        publisher.Publish(new IntMessage(20));

        Console.WriteLine("[pubsub-bag] " + string.Join(",", received));
    }

    private static void TestFilteredSubscription(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IPublisher<FilterMessage>>();
        var subscriber = provider.GetRequiredService<ISubscriber<FilterMessage>>();

        var receivedEven = new List<int>();
        var sub = subscriber.Subscribe(x => receivedEven.Add(x.Value), x => (x.Value & 1) == 0);

        for (int i = 1; i <= 6; i++)
        {
            publisher.Publish(new FilterMessage(i));
        }

        sub.Dispose();
        Console.WriteLine("[pubsub-filter] " + string.Join(",", receivedEven));
    }

    private static void TestKeyedPubSub(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IPublisher<string, string>>();
        var subscriber = provider.GetRequiredService<ISubscriber<string, string>>();

        var keyAReceived = new List<string>();
        var keyBReceived = new List<string>();

        var subA = subscriber.Subscribe("keyA", v => keyAReceived.Add(v));
        var subB = subscriber.Subscribe("keyB", v => keyBReceived.Add(v));

        publisher.Publish("keyA", "v100");
        publisher.Publish("keyB", "v200");
        publisher.Publish("keyA", "v101");

        subA.Dispose();
        publisher.Publish("keyA", "v102");
        publisher.Publish("keyB", "v201");
        subB.Dispose();

        Console.WriteLine("[pubsub-keyed] A=" + string.Join(",", keyAReceived) + " B=" + string.Join(",", keyBReceived));
    }

    private static void TestBufferedPubSub(IServiceProvider provider)
    {
        var publisher = provider.GetRequiredService<IBufferedPublisher<string>>();
        var subscriber = provider.GetRequiredService<IBufferedSubscriber<string>>();

        publisher.Publish("buffered-value-1");

        var received = new List<string>();
        var sub = subscriber.Subscribe(msg => received.Add("got:" + msg));

        publisher.Publish("buffered-value-2");
        sub.Dispose();

        Console.WriteLine("[pubsub-buffered] " + string.Join(",", received));
    }

    private static void TestEventHelper()
    {
        var (pub, sub) = GlobalMessagePipe.CreateEvent<int>();
        var received = new List<int>();

        using (sub.Subscribe(x => received.Add(x * 100)))
        {
            pub.Publish(1);
            pub.Publish(2);
        }

        pub.Publish(3);
        pub.Dispose();

        Console.WriteLine("[pubsub-event] " + string.Join(",", received));
    }
}
