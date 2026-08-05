#nullable enable
// A static-field `Dictionary<(string, string), string>` (a ValueTuple-keyed
// dictionary built in its owning type's static constructor) must look up keys *by
// value* — even though the dictionary is populated in a cctor.
//
// This is the divergence that trapped the native dn2cpp binary while it transpiled
// dn2cpp itself. dn2cpp runs every static constructor eagerly at startup
// (EmitInitCalls), whereas .NET runs each lazily on first use. That breaks exactly
// one invariant — the *global hash seed*: `System.HashCode.s_seed` (set by
// `System.HashCode..cctor`, consumed transitively via `HashCode.Combine` by every
// ValueTuple / record / struct `GetHashCode`). `CoreIntrinsics.s_intrinsicNestedCpp`
// is a static `Dictionary<(string,string),string>`; populated in a cctor that ran
// *before* `System.HashCode..cctor`, its entries were bucketed with `s_seed==0`
// while later lookups recomputed with the real seed and missed. The concrete
// victim: `IntrinsicNestedCppType("System.Text.StringBuilder",
// "AppendInterpolatedStringHandler")` returned null, so the
// `StringBuilder.AppendLine(ref handler)` intrinsic guard no longer matched.
//
// The fix (CppEmitter) hoists `System.HashCode..cctor` — a dependency-free leaf
// that only fills a deterministic seed — ahead of every other cctor.
//
// The keys here are built at RUNTIME (concatenation), so each lookup tuple holds
// distinct string references value-equal to the stored keys. The dict is a STATIC
// FIELD (built in the type's cctor) — the path that regressed; a LOCAL dict (built
// in `__GateEntry`, after all cctors) is the contrast that always worked.
//
// Former standalone gate: build-and-run-valuetuple-key-dict-subset.sh. Folding it
// into this bucket does NOT weaken the cctor-order probe, and the reason is
// structural rather than positional: `EmitInitCalls` orders cctors by
// assembly-reference depth (CoreLib depth 0, the entry assembly deepest) *before*
// reach order, and `System.HashCode` is hoisted ahead of even that. So this
// section's cctor — an app-module cctor — runs after `System.HashCode..cctor` in
// the bucket for exactly the same reason it did standalone, whatever else the
// driver calls first. No other section here writes the hash seed.
using System;
using System.Collections.Generic;

namespace ValueTupleKeyDictSubset
{
    internal static class Program
    {
        // STATIC-FIELD dict, built in the type's static constructor — exactly like
        // CoreIntrinsics.s_intrinsicNestedCpp.
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
