using System;
using System.Net.Http;
using System.Text;
using System.Globalization;

namespace HttpReal
{
    // Plain-HTTP GET through the real HttpClient surface. Under real .NET this runs on the
    // actual SocketsHttpHandler; transpiled with -r DnHttp it runs on the libcurl-backed
    // DnHttp transport (SocketsHttpHandler.Send is intercepted). Both hit the SAME local
    // server, so the printed status / body / content-type are identical and the gate diffs
    // them byte for byte. Everything printed is response-derived and deterministic — no
    // Date/Server header, no client IP, no absolute path.
    internal static class Program
    {
        private static int Main(string[] args)
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

            using (var client = new HttpClient(new SocketsHttpHandler()))
                Get("explicit", client, url);

            return 0;
        }

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
