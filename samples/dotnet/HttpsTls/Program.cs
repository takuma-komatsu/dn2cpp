using System;
using System.Net.Http;
using System.Text;
using System.Globalization;

namespace HttpsTls
{
    // ONE https:// GET through the real HttpClient surface, run twice by the gate against the
    // SAME local TLS server: once with the server's own CA in DN2CPP_HTTP_CAINFO (expected to
    // succeed) and once without it (expected to be rejected). The program does not know which
    // run it is in — it just reports what happened — so the two runs share one binary and the
    // outcome line is the whole verdict.
    //
    // WHAT IS DELIBERATELY NOT PRINTED. The transport's own diagnosis of a rejected certificate
    // goes to STDERR, never stdout. dn2cpp's transport is libcurl over the vendored Mbed TLS and
    // says "mbedTLS: The certificate is not correctly signed by the trusted CA"; real .NET's is
    // SslStream over the platform trust store and says "The SSL connection could not be
    // established, see inner exception." Both are correct and neither is the other, so a raw
    // message on stdout would make the gate's oracle diff impossible. What IS on stdout is the
    // part the two transports genuinely agree on — that the failure is a catchable
    // HttpRequestException, that the process is still alive afterwards, and that the flattened
    // cause names the certificate — and the gate diffs exactly that.
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // Base URL like https://127.0.0.1:PORT (the gate's ephemeral python TLS server);
            // an env var is the by-hand fallback.
            string baseUrl = args.Length > 0
                ? args[0]
                : Environment.GetEnvironmentVariable("DN2CPP_HTTPS_BASE") ?? "";
            if (baseUrl.Length == 0)
            {
                Console.Error.WriteLine("usage: HttpsTls <https-base-url>   (e.g. https://127.0.0.1:8443)");
                return 2;
            }
            string url = baseUrl + "/hello.txt";

            using (var client = new HttpClient())
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    using HttpResponseMessage response = client.Send(request);
                    byte[] body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    Console.WriteLine("tls outcome=ok");
                    Console.WriteLine("tls status=" + (int)response.StatusCode);
                    Console.WriteLine("tls len=" + body.Length);
                    Console.WriteLine("tls body=" + Encoding.UTF8.GetString(body));
                    Console.WriteLine("tls ctype=" + (response.Content.Headers.ContentType?.MediaType ?? "(none)"));
                }
                catch (HttpRequestException ex)
                {
                    // A rejected certificate is a TRANSPORT failure, so it must arrive the way a
                    // refused connection does — as a catchable exception, not as a dead process.
                    string cause = Flatten(ex);
                    Console.WriteLine("tls outcome=rejected");
                    Console.WriteLine("tls exception=" + ex.GetType().Name);
                    Console.WriteLine("tls cause-names-certificate=" + (MentionsCertificate(cause) ? "1" : "0"));
                    Console.Error.WriteLine("tls-cause: " + cause);
                }
            }

            // Reached only if the exception really was catchable: a transport that aborts the
            // process on a verification failure never prints this line.
            Console.WriteLine("tls alive=1");
            return 0;
        }

        // Message plus the whole InnerException chain. Real .NET buries the diagnosis one level
        // down (HttpRequestException -> AuthenticationException); dn2cpp's shim throws the
        // diagnosis directly. Walking the chain is what lets one predicate read both.
        private static string Flatten(Exception ex)
        {
            var sb = new StringBuilder();
            for (Exception e = ex; e is not null; e = e.InnerException)
            {
                if (sb.Length > 0)
                    sb.Append(" | ");
                sb.Append(e.GetType().Name).Append(": ").Append(e.Message);
            }

            return sb.ToString();
        }

        private static bool MentionsCertificate(string cause)
        {
            return cause.ToLowerInvariant().Contains("certificate");
        }
    }
}
