using System;
using System.Net.Http;

namespace HttpClientCert
{
    // The client-certificate cut, driven on the HttpClient CONSTRUCTION path.
    //
    // HttpClientHandler..ctor sets ClientCertificateOptions = Manual, and the real setter takes
    // the address of a lambda in each of its two arms; reachability follows an ldftn, so both
    // enter the tree whatever the argument, dragging in CertificateHelper -> X509Store -> the
    // platform keychain -> ASN.1 -> BigInteger. dn2cpp cuts the setter's IL and emits a guard
    // body instead (CoreIntrinsics.IsInterceptedHttpCertOptionMethod). This program is what
    // proves the cut at the BINARY level rather than the transpile level: it is the part of the
    // System.Net.Http surface that reaches a native executable today, since it never sends a
    // request and so never touches the header/cancellation BCL that still stops HttpReal.
    //
    // Deterministic and response-free by construction — nothing here opens a socket.
    internal static class Program
    {
        // Spelled out rather than ClientCertificateOption.ToString(): formatting a
        // cross-assembly enum by name is a live gap of its own (it bottoms out in an unmodeled
        // NumberStyles.AllowCurrencySymbol), and it is not what this gate is about. The two
        // names below are what real .NET's ToString() would print, so both sides still agree.
        private static string Name(ClientCertificateOption o) =>
            o == ClientCertificateOption.Manual ? "Manual"
            : o == ClientCertificateOption.Automatic ? "Automatic"
            : "UNKNOWN";

        private static int Main()
        {
            // The default the ctor installs. Manual is accepted as a no-op, and the GETTER
            // must be intercepted to answer Manual too: the replaced setter stores nothing,
            // and the real getter chain bottoms out in a field whose initializer is
            // Automatic, which only the cut setter's forward ever overwrote.
            using (var handler = new HttpClientHandler())
                Console.WriteLine("ctor default = " + Name(handler.ClientCertificateOptions));

            // Setting the default explicitly reaches the same one body, and is likewise a
            // no-op; the read-back is the round-trip the getter intercept exists to keep.
            using (var handler = new HttpClientHandler())
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                Console.WriteLine("explicit Manual = " + Name(handler.ClientCertificateOptions));
            }

            // new HttpClient() builds an HttpClientHandler, i.e. it drives the ctor path above.
            using (var client = new HttpClient())
                Console.WriteLine("default HttpClient built");

            // THE DELIBERATE DIVERGENCE, and why the gate diffs against a frozen block rather
            // than against real .NET. Automatic asks for a certificate to be selected out of
            // the system store — the subtree the cut deleted — and TLS here runs in the
            // vendored libcurl/Mbed TLS transport, which never consults a .NET selection
            // callback. Accepting the value would silently report success for work that
            // cannot happen, so it is refused catchably; the read-back asserts the refusal
            // stored nothing.
            var refusing = new HttpClientHandler();
            try
            {
                refusing.ClientCertificateOptions = ClientCertificateOption.Automatic;
                Console.WriteLine("Automatic = accepted");
            }
            catch (PlatformNotSupportedException e)
            {
                bool named = e.Message.Contains("ClientCertificateOption.Manual");
                Console.WriteLine("Automatic = refused (" + (named ? "names the remedy" : "MESSAGE UNEXPECTED") + ")");
            }
            Console.WriteLine("after Automatic = " + Name(refusing.ClientCertificateOptions));
            refusing.Dispose();

            return 0;
        }
    }
}
