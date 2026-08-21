using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePipeFilterSubset;

public sealed class LoggingFilter<T> : MessageHandlerFilter<T>
{
    public static readonly List<string> Log = new List<string>();

    public override void Handle(T message, Action<T> next)
    {
        Log.Add("pre:" + message);
        next(message);
        Log.Add("post:" + message);
    }
}

public sealed class AsyncLoggingFilter<T> : AsyncMessageHandlerFilter<T>
{
    public static readonly List<string> AsyncLog = new List<string>();

    public override async ValueTask HandleAsync(T message, CancellationToken cancellationToken, Func<T, CancellationToken, ValueTask> next)
    {
        AsyncLog.Add("async-pre:" + message);
        await next(message, cancellationToken);
        AsyncLog.Add("async-post:" + message);
    }
}

public sealed class RequestAuditFilter<TReq, TRes> : RequestHandlerFilter<TReq, TRes>
{
    public static readonly List<string> AuditLog = new List<string>();

    public override TRes Invoke(TReq request, Func<TReq, TRes> next)
    {
        AuditLog.Add("req-in:" + request);
        var res = next(request);
        AuditLog.Add("req-out:" + res);
        return res;
    }
}

public sealed class AsyncRequestAuditFilter<TReq, TRes> : AsyncRequestHandlerFilter<TReq, TRes>
{
    public static readonly List<string> AsyncAuditLog = new List<string>();

    public override async ValueTask<TRes> InvokeAsync(TReq request, CancellationToken cancellationToken, Func<TReq, CancellationToken, ValueTask<TRes>> next)
    {
        AsyncAuditLog.Add("areq-in:" + request);
        var res = await next(request, cancellationToken);
        AsyncAuditLog.Add("areq-out:" + res);
        return res;
    }
}

public sealed class UpperHandler : IRequestHandler<string, string>
{
    public string Invoke(string request)
    {
        return request.ToUpperInvariant();
    }
}

public sealed class AsyncDoubleHandler : IAsyncRequestHandler<string, string>
{
    public async ValueTask<string> InvokeAsync(string request, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return request + "+" + request;
    }
}

public static class Program
{
    public static void __GateEntry()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessagePipe(options =>
        {
            options.EnableAutoRegistration = false;
            options.AddGlobalMessageHandlerFilter(typeof(LoggingFilter<>));
            options.AddGlobalAsyncMessageHandlerFilter(typeof(AsyncLoggingFilter<>));
            options.AddGlobalRequestHandlerFilter(typeof(RequestAuditFilter<,>));
            options.AddGlobalAsyncRequestHandlerFilter(typeof(AsyncRequestAuditFilter<,>));
        });

        builder.AddRequestHandler<UpperHandler>();
        builder.AddAsyncRequestHandler<AsyncDoubleHandler>();

        var provider = services.BuildServiceProvider();
        GlobalMessagePipe.SetProvider(provider);

        TestGlobalMessagePipe();
        TestSyncFilter(provider);
        TestAsyncFilter(provider).GetAwaiter().GetResult();
        TestRequestFilter(provider);
        TestAsyncRequestFilter(provider).GetAwaiter().GetResult();
    }

    private static void TestGlobalMessagePipe()
    {
        var pub = GlobalMessagePipe.GetPublisher<string>();
        var sub = GlobalMessagePipe.GetSubscriber<string>();

        string received = null;
        using var d = sub.Subscribe(msg => received = msg);

        pub.Publish("via-global");
        Console.WriteLine("[global-mp] received=" + received);
    }

    private static void TestSyncFilter(IServiceProvider provider)
    {
        LoggingFilter<string>.Log.Clear();
        var publisher = provider.GetRequiredService<IPublisher<string>>();
        var subscriber = provider.GetRequiredService<ISubscriber<string>>();

        string received = null;
        using var d = subscriber.Subscribe(x => received = x + "-handled");

        publisher.Publish("filter-item");
        Console.WriteLine("[filter-sync] val=" + received + " log=" + string.Join(",", LoggingFilter<string>.Log));
    }

    private static async Task TestAsyncFilter(IServiceProvider provider)
    {
        AsyncLoggingFilter<string>.AsyncLog.Clear();
        var publisher = provider.GetRequiredService<IAsyncPublisher<string>>();
        var subscriber = provider.GetRequiredService<IAsyncSubscriber<string>>();

        string received = null;
        using var d = subscriber.Subscribe((msg, ct) =>
        {
            received = "handled:" + msg;
            return ValueTask.CompletedTask;
        });

        await publisher.PublishAsync("test-async-filter");
        Console.WriteLine("[filter-async] res=" + received + " log=" + string.Join(",", AsyncLoggingFilter<string>.AsyncLog));
    }

    private static void TestRequestFilter(IServiceProvider provider)
    {
        RequestAuditFilter<string, string>.AuditLog.Clear();
        var handler = provider.GetRequiredService<IRequestHandler<string, string>>();

        string result = handler.Invoke("hello world");
        Console.WriteLine("[filter-req] result=" + result + " log=" + string.Join(",", RequestAuditFilter<string, string>.AuditLog));
    }

    private static async Task TestAsyncRequestFilter(IServiceProvider provider)
    {
        AsyncRequestAuditFilter<string, string>.AsyncAuditLog.Clear();
        var handler = provider.GetRequiredService<IAsyncRequestHandler<string, string>>();

        string result = await handler.InvokeAsync("twenty-one");
        Console.WriteLine("[filter-areq] result=" + result + " log=" + string.Join(",", AsyncRequestAuditFilter<string, string>.AsyncAuditLog));
    }
}
