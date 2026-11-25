using System;
using System.Net.Http;
using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Principal;

namespace HttpReal
{
    // Plain-HTTP GET through the real HttpClient surface. Under real .NET this runs on the
    // actual SocketsHttpHandler; transpiled with -r DnHttp it runs on the libcurl-backed
    // DnHttp transport (SocketsHttpHandler.Send is intercepted). Both hit the SAME local
    // server, so the printed status / body / content-type are identical and the gate diffs
    // them byte for byte. The explicit handler also exposes its per-instance settings
    // through the same accessor ABI as HttpWebRequest.
    internal static class Program
    {
        private static int Main(string[] args) => Run(args);

        internal static int Run(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // Base URL like http://127.0.0.1:PORT (the gate's ephemeral python http.server);
            // an env var is the by-hand fallback.
            string baseUrl = args.Length > 0
                ? args[0]
                : Environment.GetEnvironmentVariable("DN2CPP_HTTP_BASE") ?? "";
            if (baseUrl.Length == 0)
            {
                Console.Error.WriteLine("usage: HttpReal <base-url>   (e.g. http://127.0.0.1:8080)");
                return 2;
            }
            string url = baseUrl + "/hello.txt";

            // Path A: the default handler stack — new HttpClient() builds an
            // HttpClientHandler over a SocketsHttpHandler. Path B: an explicit
            // SocketsHttpHandler. Both bottom out in the intercepted transport override, so
            // the two blocks must print identically.
            using (var client = new HttpClient())
                Get("default", client, url);

            var handler = new SocketsHttpHandler();
            bool stored;
            using (var client = new HttpClient(handler))
            {
                Get("explicit", client, url);
                object settings = GetSettings(handler);
                Console.WriteLine("handler settings=" + (settings is not null && ReferenceEquals(settings, GetSettings(handler))));
                ref var level = ref ImpersonationLevel(settings!);
                var original = level;
                level = TokenImpersonationLevel.Delegation;
                stored = ImpersonationLevel(settings!) == TokenImpersonationLevel.Delegation;
                level = original;
            }
            var cause = new Exception("transport");
            Console.WriteLine("inner exception=" + ReferenceEquals(cause, get_InnerException(new Exception("request failed", cause))));
            Console.WriteLine("handler field=" + stored);
            var options = new FieldOptions();
            Level(options) = TokenImpersonationLevel.Delegation;
            Console.WriteLine("local volatile field=" + (options.Level == TokenImpersonationLevel.Delegation));

            return 0;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Settings")]
        [return: UnsafeAccessorType("System.Net.Http.HttpConnectionSettings, System.Net.Http")]
        private static extern object GetSettings(SocketsHttpHandler handler);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_impersonationLevel")]
        private static extern ref TokenImpersonationLevel ImpersonationLevel(
            [UnsafeAccessorType("System.Net.Http.HttpConnectionSettings, System.Net.Http, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")] object settings);

        private sealed class FieldOptions
        {
            public volatile TokenImpersonationLevel Level = TokenImpersonationLevel.None;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Field)]
        private static extern ref TokenImpersonationLevel Level([UnsafeAccessorType("HttpReal.Program+FieldOptions")] object options);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnsafeAccessor(UnsafeAccessorKind.Method)]
        [return: UnsafeAccessorType("System.Exception, System.Runtime")]
        private static extern object get_InnerException(Exception exception);

        private static void Get(string label, HttpClient client, string url)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using HttpResponseMessage response = client.Send(request);
            byte[] body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            Console.WriteLine(label + " status=" + (int)response.StatusCode);
            Console.WriteLine(label + " len=" + body.Length);
            Console.WriteLine(label + " body=" + Encoding.UTF8.GetString(body));
            Console.WriteLine(label + " ctype=" + (response.Content.Headers.ContentType?.MediaType ?? "(none)"));
        }
    }
}
