#nullable enable
// A static-field `Dictionary<(string, string), string>` (a ValueTuple-keyed
// dictionary built in its owning type's static constructor) must look up keys *by
// value* — even though the dictionary is populated in a cctor.
//
// ValueTuple / record / struct `GetHashCode` hashes through `HashCode.Combine`, which
// reads the global seed `System.HashCode.s_seed`. A dictionary bucketed before that
// seed's initializer ran would hash its entries with `s_seed == 0` and miss on every
// later lookup, which hashes with the real seed. Reading the seed is a first-use
// trigger of `System.HashCode..cctor`, so entries and lookups hash alike. dn2cpp's own
// `CoreIntrinsicTypeMaps.s_intrinsicNestedCpp` is such a dictionary: a miss there makes
// `IntrinsicNestedCppType("System.Text.StringBuilder", "AppendInterpolatedStringHandler")`
// return null, and the `StringBuilder.AppendLine(ref handler)` intrinsic guard stops
// matching when dn2cpp transpiles itself.
//
// The keys here are built at RUNTIME (concatenation), so each lookup tuple holds
// distinct string references value-equal to the stored keys. The dict is a STATIC
// FIELD (built in the type's cctor), the shape an out-of-order seed breaks; a LOCAL
// dict (built in `__GateEntry`) is the contrast. No other section here writes the
// hash seed.
using System;
using System.Collections.Generic;

namespace ValueTupleKeyDictSubset
{
    internal static class Program
    {
        // STATIC-FIELD dict, built in the type's static constructor — exactly like
        // CoreIntrinsicTypeMaps.s_intrinsicNestedCpp.
        private static readonly Dictionary<(string Enclosing, string Name), string> s_map = new()
        {
            [("System.Text.StringBuilder", "AppendInterpolatedStringHandler")] = "Dn2CppStringBuilder*",
            [("System.Threading.Lock", "Scope")] = "Dn2CppLockScope",
        };

        internal static int __GateEntry()
        {
            string declFull = "System.Text" + ".StringBuilder";
            string name = "Append" + "InterpolatedStringHandler";
            Console.WriteLine("static-field dict:");
            Console.WriteLine(s_map.TryGetValue((declFull, name), out var v1));   // True
            Console.WriteLine(v1 ?? "<null>");                                    // Dn2CppStringBuilder*
            Console.WriteLine(s_map.TryGetValue(("System.Threading" + ".Lock", "Sco" + "pe"), out var v2)); // True
            Console.WriteLine(v2 ?? "<null>");                                    // Dn2CppLockScope

            // LOCAL dict (built in __GateEntry, after all static cctors) — the contrast path.
            var local = new Dictionary<(string, string), string>
            {
                [("System.Text.StringBuilder", "AppendInterpolatedStringHandler")] = "Dn2CppStringBuilder*",
            };
            Console.WriteLine("local dict:");
            Console.WriteLine(local.TryGetValue((declFull, name), out var v3));   // True
            Console.WriteLine(v3 ?? "<null>");                                    // Dn2CppStringBuilder*

            return 0;
        }
    }
}
