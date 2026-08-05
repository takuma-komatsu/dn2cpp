using System;
using System.Net.Http;

namespace WasmHttpProbe
{
    // The Web lane's HTTP carve-out: wasm has no socket layer, so the fallback
    // arm must fail every call with a message NAMING the platform, catchably, and
    // leave the program running. That this MODULE LINKS at all is the fourth
    // assertion and the only one invisible in the output.
    //
    // The default-timeout probe is the load-bearing one: its CancelAfter arms
    // inside Send before the handler runs, so it proves the first diagnostic a
    // naive port meets is this carve-out and not the thread one. Send is
    // synchronous because SendAsync is a Task.Run and there are no threads.
    internal static class Program
    {
        private static int ProbeOnce(HttpClient client, string label)
        {
            // .invalid is reserved (RFC 2606), so a hand run against a desktop
            // curl build contacts nothing; on wasm the URL is never read.
            using var request = new HttpRequestMessage(HttpMethod.Get, "http://probe.invalid/never");
            try
            {
                using HttpResponseMessage response = client.Send(request);
                Console.WriteLine("UNREACHABLE[" + label + "]: a transport answered, status=" + (int)response.StatusCode);
                return 1;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(label + ": caught HttpRequestException");
                Console.WriteLine(label + ": message=" + ex.Message);
            }

            return 0;
        }

        private static int Main()
        {
            using (var def = new HttpClient())
            {
                int rc = ProbeOnce(def, "default");
                if (rc != 0)
                    return rc;
            }

            using (var inf = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan })
            {
                int rc = ProbeOnce(inf, "infinite");
                if (rc != 0)
                    return rc;
            }

            Console.WriteLine("still running");
            return 0;
        }
    }
}
