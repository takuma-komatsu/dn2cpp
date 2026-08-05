using System;
using System.Globalization;
using System.Resources;

// The assembly's own neutral-resources declaration. It is here rather than in the
// csproj's <NeutralLanguage> because the two-argument form is the one worth carrying:
// the transpiler reads the constructor's ARITY to decide whether an
// UltimateResourceFallbackLocation follows the culture name, and MainAssembly is the
// value under which the neutral blob embedded here IS the ultimate fallback.
//
// The culture must be the SPECIFIC "en-US", not the neutral "en": dn2cpp resolves
// `new CultureInfo("en")` to its builtin en-US entry (dn2cpp_culture_by_name aliases the
// six neutral names to their specific ones), so a declaration of "en" would be compared
// against a CultureInfo.Name of "en-US" and refused.
//
// The Satellite value inverts this and cannot be declared in the same assembly, which is
// why samples/dotnet/ManifestResourcesSatellite exists as its own program.
[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.MainAssembly)]

namespace ManifestResources
{
    // Gate driver: runs each section's __GateEntry() in order. Each section keeps
    // its own namespace so type/namespace-sensitive output stays stable.
    //
    // One argv token splits the program in two, and the split is forced by what can be
    // asserted rather than by taste. Argless (the default) runs the sections whose
    // output real .NET reproduces exactly, so the gate's control arm can diff the two
    // LIVE. "limits" runs the ResourceManager carve-outs instead — every read whose
    // honest answer DIVERGES from .NET (a culture-specific lookup, a missing set,
    // GetStream, a non-String entry), which no live diff can hold and which the gate
    // therefore freezes. Keeping both halves in one program keeps them transpiled
    // together, under one set of flags, out of one compiled binary.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Only the oracle has a host culture, so its number/date formatting must be
            // pinned or this gate is red on a de-DE machine and nowhere else.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            // The UI culture selects the resource set that renders the oracle's exception
            // messages — load-bearing for the fault section below, not boilerplate.
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length > 0 && args[0] == "limits")
            {
                ResourceManagerLimitsSubset.Program.__GateEntry();
                return;
            }

            ResourceStreamSubset.Program.__GateEntry();
            ResourceCatalogSubset.Program.__GateEntry();
            ResourceManagerSubset.Program.__GateEntry();
            BclFaultMessageSubset.Program.__GateEntry();
        }
    }
}
