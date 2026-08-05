#nullable disable
using System;

namespace UriBasicSubset
{
    // System.Uri basics (transpiled from the real System.Private.Uri IL):
    // TryCreate absolute/relative/invalid, the component properties, scheme and
    // host canonicalization, percent-encoded paths, default-port elision, the
    // string ctor and Equals. ASCII hosts only — Unicode/IDN host canonicalization
    // runs the invariant-globalization arms (managed Punycode) and may diverge
    // from the ICU-backed oracle, so it stays out of this diffed section.
    internal static class Program
    {
        internal static void __GateEntry()
        {
            Uri u;
            Console.WriteLine(Uri.TryCreate("http://example.com:8080/path?q=1#frag", UriKind.Absolute, out u)); // True
            Console.WriteLine(u.Scheme);        // http
            Console.WriteLine(u.Host);          // example.com
            Console.WriteLine(u.Port);          // 8080
            Console.WriteLine(u.AbsolutePath);  // /path
            Console.WriteLine(u.Query);         // ?q=1
            Console.WriteLine(u.Fragment);      // #frag
            Console.WriteLine(u.PathAndQuery);  // /path?q=1
            Console.WriteLine(u.AbsoluteUri);   // http://example.com:8080/path?q=1#frag
            Console.WriteLine(u.ToString());    // http://example.com:8080/path?q=1#frag
            Console.WriteLine(u.IsAbsoluteUri); // True
            Console.WriteLine(u.IsDefaultPort); // False

            Uri d;
            Console.WriteLine(Uri.TryCreate("https://example.com/", UriKind.Absolute, out d)); // True
            Console.WriteLine(d.Port);          // 443 (scheme default)
            Console.WriteLine(d.IsDefaultPort); // True
            Console.WriteLine(d.Authority);     // example.com

            // Scheme + host canonicalize to lowercase; the path keeps its case.
            Uri c;
            Console.WriteLine(Uri.TryCreate("HTTP://EXAMPLE.COM/Path", UriKind.Absolute, out c)); // True
            Console.WriteLine(c.Scheme);        // http
            Console.WriteLine(c.Host);          // example.com
            Console.WriteLine(c.AbsolutePath);  // /Path

            // Percent-encoding is preserved in AbsolutePath.
            Uri pe;
            Console.WriteLine(Uri.TryCreate("http://example.com/a%20b", UriKind.Absolute, out pe)); // True
            Console.WriteLine(pe.AbsolutePath); // /a%20b

            Uri r;
            Console.WriteLine(Uri.TryCreate("foo/bar", UriKind.Relative, out r)); // True
            Console.WriteLine(r.ToString());     // foo/bar
            Console.WriteLine(r.IsAbsoluteUri);  // False

            Uri bad;
            Console.WriteLine(Uri.TryCreate("http://exa mple.com", UriKind.Absolute, out bad)); // False
            Console.WriteLine(bad is null);      // True

            Uri n1 = new Uri("http://example.com:8080/path?q=1");
            Console.WriteLine(n1.Host);          // example.com
            Console.WriteLine(n1.Equals(u));     // True (Uri.Equals ignores the fragment)
            Console.WriteLine(n1 == new Uri("HTTP://EXAMPLE.COM:8080/path?q=1")); // True (scheme/host case-insensitive)
            Console.WriteLine(new Uri("http://example.com/x") == new Uri("http://example.com/y")); // False
        }
    }
}
