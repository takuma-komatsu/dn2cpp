using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace MessagePipeReqRespSubset;

public sealed class PingHandler : IRequestHandler<string, string>
{
    public string Invoke(string request)
    {
        return "pong:" + request;
    }
}

public sealed class AsyncMathHandler : IAsyncRequestHandler<string, string>
{
    public async ValueTask<string> InvokeAsync(string request, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        return "math:" + request + ":" + (request.Length * request.Length);
    }
}

public sealed class PrefixHandlerA : IRequestHandler<string, string>
{
    public string Invoke(string request)
    {
        return "A:" + request;
    }
}

public sealed class PrefixHandlerB : IRequestHandler<string, string>
{
    public string Invoke(string request)
    {
        return "B:" + request;
    }
}

public static class Program
{
    public static void __GateEntry()
    {
        var services = new ServiceCollection();
        var builder = services.AddMessagePipe(options => options.EnableAutoRegistration = false);

        builder.AddRequestHandler<PingHandler>();
        builder.AddAsyncRequestHandler<AsyncMathHandler>();
        builder.AddRequestHandler<PrefixHandlerA>();
        builder.AddRequestHandler<PrefixHandlerB>();

        var provider = services.BuildServiceProvider();

        TestSyncReqResp(provider);
        TestAsyncReqResp(provider).GetAwaiter().GetResult();
        TestRequestAll(provider);
    }

    private static void TestSyncReqResp(IServiceProvider provider)
    {
        var handler = provider.GetRequiredService<IRequestHandler<string, string>>();
        string res1 = handler.Invoke("hello");
        string res2 = handler.Invoke("dn2cpp");

        Console.WriteLine("[reqresp-sync] " + res1 + " / " + res2);
    }

    private static async Task TestAsyncReqResp(IServiceProvider provider)
    {
        var handler = provider.GetRequiredService<IAsyncRequestHandler<string, string>>();
        string res1 = await handler.InvokeAsync("five");
        string res2 = await handler.InvokeAsync("twelve");

        Console.WriteLine("[reqresp-async] " + res1 + " / " + res2);
    }

    private static void TestRequestAll(IServiceProvider provider)
    {
        var allHandler = provider.GetRequiredService<IRequestAllHandler<string, string>>();
        var results = allHandler.InvokeAll("item").ToArray();

        Console.WriteLine("[reqresp-all] " + string.Join(",", results));
    }
}
