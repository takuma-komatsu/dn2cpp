#nullable disable
using System;
using System.Globalization;
using System.Reflection;
using System.Resources;

// [NeutralResourcesLanguage(..., UltimateResourceFallbackLocation.Satellite)] — its own
// program because the declaration is ASSEMBLY-level and inverts what the sibling
// ManifestResources sample declares, so the two cannot share one assembly.
//
// Satellite says the neutral resources live in a SATELLITE and the copy embedded in the
// main assembly is not the ultimate fallback — so real .NET reads the satellite for EVERY
// ask, the culture-less one included, and with no satellite built raises
// MissingSatelliteAssemblyException for all three asks below. dn2cpp links no satellite
// assembly at all, so the only answer it could give is the embedded blob — exactly the
// answer real .NET refuses, which would make it a wrong value rather than an error. It
// refuses instead, and the frozen snapshot asserts that the refusal names the declaration.
[assembly: NeutralResourcesLanguage("en", UltimateResourceFallbackLocation.Satellite)]

namespace ManifestResourcesSatellite
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

        private static void Main()
        {
            Console.WriteLine("== satellite ultimate fallback ==");
            Assembly asm = Assembly.GetExecutingAssembly();
            var rm = new ResourceManager("ManifestResourcesSatellite.Strings", asm);

            // The culture-LESS ask first: it is the one that looks answerable, since the
            // embedded blob is right there — and real .NET still reads the satellite.
            Try("neutral", () => rm.GetString("Greeting"));
            Try("invariant", () => rm.GetString("Greeting", CultureInfo.InvariantCulture));
            // And the declared culture itself, which under MainAssembly would be served
            // from here: the fallback location is what decides, not the culture name.
            Try("declared-en", () => rm.GetString("Greeting", new CultureInfo("en")));

            // The blob really is embedded — this is the control, and without it a
            // section of expected refusals would read the same if the resource had
            // simply been left out of the assembly.
            Console.WriteLine("embedded "
                + (asm.GetManifestResourceStream("ManifestResourcesSatellite.Strings.resources")
                    != null));
        }
    }
}
