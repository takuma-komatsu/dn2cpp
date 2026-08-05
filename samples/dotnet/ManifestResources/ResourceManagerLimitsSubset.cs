#nullable disable
using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

// The ResourceManager carve-outs — every read whose honest answer DIVERGES from
// real .NET, which is exactly why this section cannot be live-diffed and is driven
// separately (argv "limits") against a frozen snapshot instead. The snapshot carries
// each message verbatim, so it doubles as the assertion that the refusal names the
// thing that was refused and the way out.
//
// Each one is a refusal rather than a degraded answer, and the reason is the same in
// every case: the wrong answer available here is not an error but a plausible VALUE,
// which a shipped game would consume silently.
namespace ResourceManagerLimitsSubset
{
    internal static class Program
    {
        private static void Try(string label, Func<string> f)
        {
            try
            {
                Console.WriteLine(label + " -> " + f());
            }
            catch (Exception e)
            {
                Console.WriteLine(label + " -> " + e.GetType().Name + ": " + e.Message);
            }
        }

        internal static void __GateEntry()
        {
            Console.WriteLine("== resource manager limits ==");
            Assembly asm = Assembly.GetExecutingAssembly();
            var rm = new ResourceManager("ManifestResources.Strings", asm);

            // The CONTROL, first: the neutral read still works. Without this line the
            // rest of the section would pass just as well if the whole lookup were
            // broken, which is the failure mode a file full of expected throws has.
            Try("neutral", () => rm.GetString("Greeting"));

            // 1. A culture-SPECIFIC lookup. Real .NET answers the neutral string here,
            //    because this sample builds no fr satellite — and that is precisely the
            //    divergence: dn2cpp links no satellite assemblies at all, so from inside
            //    the image "no fr satellite was built" and "an fr satellite exists and I
            //    cannot reach it" are one state. Serving the neutral would be right in
            //    the first case and a silent wrong answer in the second.
            Try("culture-fr", () => rm.GetString("Greeting", new CultureInfo("fr-FR")));

            // 2. A missing resource SET. dn2cpp raises the same type .NET does
            //    (MissingManifestResourceException) with its own message, so the TYPE is
            //    asserted live in the other section and only the message is pinned here.
            //    What matters either way is that it is not the null a missing KEY gives —
            //    those are different bugs with different fixes.
            Try("missing-set",
                () => new ResourceManager("ManifestResources.NoSuchSet", asm).GetString("Greeting"));

            // 3. GetStream's signature NAMES UnmanagedMemoryStream, whose body is not
            //    transpilable — so unlike Assembly.GetManifestResourceStream, which
            //    hands back a MemoryStream over a copy, no substitute fits the return
            //    type. The message names the route that does work.
            Try("get-stream", () => rm.GetStream("Greeting") == null ? "<null>" : "<stream>");

            // 4. A non-String entry, out of the checked-in Mixed set (an Int32 beside a
            //    String). Real .NET raises InvalidOperationException naming the type it
            //    found and dn2cpp raises the same family — the LIVE section asserts
            //    that; what is pinned here is the message, which differs.
            var mixed = new ResourceManager("ManifestResources.Mixed", asm);
            Try("mixed-string", () => mixed.GetString("Label"));
            Try("non-string", () => mixed.GetString("Answer") ?? "<null>");

            // 5. A Stream entry. GetObject decodes every primitive code and
            //    byte[], so this and the BinaryFormatter-serialized user types are all
            //    that is left — and this one is refused for GetStream's exact reason:
            //    real .NET hands back an UnmanagedMemoryStream over the mapped image,
            //    a body that is not transpilable, and no MemoryStream can be returned
            //    in its place because the ANSWER is the object, not its bytes.
            Try("stream-obj",
                () => mixed.GetObject("p-stream") == null ? "<null>" : "<stream>");

            // 6. GetString over that same Stream entry names the type it found, which
            //    is the arm proving the "not a String" message reads the type table for
            //    a code GetObject would itself refuse: what the caller has to change is
            //    the overload either way.
            Try("stream-string", () => mixed.GetString("p-stream") ?? "<null>");
        }
    }
}
