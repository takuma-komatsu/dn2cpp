#nullable enable
// A `HashSet<string>(StringComparer.Ordinal)` (and a
// `Dictionary<string,…>(StringComparer.Ordinal)`) must de-duplicate keys *by
// value*, not by reference. The transpiler's own output-dedup sets — the
// invoker-thunk set (CppEmitter `invokerThunks`), the type-registry set, and the
// generic-def map (`_genericDefSyms`) — are all `HashSet`/`Dictionary` keyed by a
// freshly-built string with `StringComparer.Ordinal`. When native dn2cpp ran its
// own transpiled HashSet/Dictionary, the explicit `StringComparer.Ordinal`
// comparer dispatched to *reference* equality (not the value-correct
// `dn2cpp_string_equals`/`dn2cpp_string_hashcode`), so value-equal-but-distinct
// keys never collapsed: native HelloWorld emitted duplicate `inv_*` thunks +
// duplicate `gendef_*`/type-registry rows.
//
// The keys here are built at RUNTIME (concatenation / a Sanitize-style char
// transform), so each occurrence is a distinct reference that is value-equal —
// exactly the shape the transpiler's dedup sets use. A DEFAULT `HashSet<string>`
// (no comparer) is included to show that path already worked; the
// `StringComparer.Ordinal` paths are the ones that regressed. Output must match
// real .NET line-for-line (the bucket is an exact diff against real .NET).
//
// Former standalone gate: build-and-run-ordinal-stringset-dedup-subset.sh.
using System;
using System.Collections.Generic;

namespace OrdinalStringSetDedupSubset
{
    internal static class Program
    {
        // A Sanitize-style transform: build "inv_" + the cleaned token. Two calls
        // with the same input yield two value-equal but reference-distinct strings.
        private static string Key(string raw)
        {
            var sb = new System.Text.StringBuilder("inv_");
            foreach (char c in raw)
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            return sb.ToString();
        }

        internal static int __GateEntry()
        {
            // 1) HashSet<string>(StringComparer.Ordinal): add each distinct key
            //    twice via independently-built strings. Add must return false the
            //    second time; Count must be the distinct count (3), not 6.
            var set = new HashSet<string>(StringComparer.Ordinal);
            string[] raws = { "i.r:v", "i.r:P", "s_int32__r" };
            int added = 0;
            foreach (var r in raws)
            {
                if (set.Add(Key(r))) added++;   // first build  -> inserted
                if (set.Add(Key(r))) added++;   // second build -> value-equal dup
            }
            Console.WriteLine(added);            // 3 (not 6)
            Console.WriteLine(set.Count);        // 3
            // Contains with yet another freshly-built, value-equal key.
            Console.WriteLine(set.Contains(Key("i.r:v")));   // True
            Console.WriteLine(set.Contains(Key("nope")));    // False
            // foreach surfaces the deduped set (insertion order == .NET).
            foreach (var k in set)
                Console.WriteLine(k);            // inv_i_r_v / inv_i_r_P / inv_s_int32__r

            // 2) Dictionary<string,int>(StringComparer.Ordinal): the second
            //    indexer write with a value-equal key updates, never adds a row.
            var map = new Dictionary<string, int>(StringComparer.Ordinal);
            map[Key("Box`1")] = 1;
            map[Key("Box`1")] = 2;               // same key by value -> overwrite
            map[Key("Pair`2")] = 3;
            Console.WriteLine(map.Count);        // 2
            Console.WriteLine(map[Key("Box`1")]); // 2
            Console.WriteLine(map.ContainsKey(Key("Pair`2"))); // True

            // 3) Default HashSet<string>() (no explicit comparer) — already correct;
            //    pinned here so the gate covers both comparer paths.
            var def = new HashSet<string>();
            def.Add(Key("dup"));
            def.Add(Key("dup"));
            Console.WriteLine(def.Count);        // 1

            return 0;
        }
    }
}
