#nullable enable
// A LINQ `Select`/`foreach` over a *value-type-element*
// array obtained from a method/indexer CALL RESULT must wire that array's SZArray
// interface-dispatch table — otherwise the generic `<Select>d__1<T,…>` iterator's
// `callvirt IEnumerable<T>.GetEnumerator()` traps in `dn2cpp_resolve_interface`
// (`EntryPointNotFoundException (interface dispatch)`, rc=134).
//
// This is the exact shape that aborted native dn2cpp mid-self-transpile.
// `Dn2Cpp.CppEmitter.EmitBlobs` does
// `_literals.Blobs[i].Select(b => "0x" + b.ToString("x2"))` where `Blobs` is a
// `List<byte[]>`. `Blobs[i]` lowers to `List<byte[]>.get_Item(i)` — a CALL RESULT
// returning a value-element `byte[]` — which flows straight into the shim
// `Enumerable.Select<byte,string>`'s `IEnumerable<byte>` parameter (array→
// IEnumerable<T> covariance). The shim's `<Select>d__1<byte,string>.MoveNext()`
// then `callvirt`s `IEnumerable<byte>.GetEnumerator()` on that raw `byte[]`.
//
// The same family for an array read from a FIELD (`ldfld`) is handled by
// threading the SZArray static type on the field read. But here the array comes
// from a CALL/indexer result, whose SZArray static type the transpiler dropped on
// the stack: `EmitCallResult` only threaded the return type as the result's
// StaticType when it was a `Class`. `CoerceTo` therefore never saw the `byte[]`
// flow into `IEnumerable<byte>`, never called `NoteArrayEnumerableElement`, and the
// `byte[]` type-info shipped with `interfaceCount == 0` → the runtime
// `dn2cpp_resolve_interface` trapped.
//
// Covers the canonical `List<T[]>` indexer source (the EmitBlobs shape) AND a
// plain method returning `int[]`, each `.Select(...)`-projected and iterated with
// `foreach` (MoveNext/get_Current/Dispose interface dispatch on the generic
// iterator). The native binary's output must match real .NET line-for-line
// (computed live below, not hard-coded). Real System.Private.CoreLib (-r) + the
// real System.Linq → tree-shaken transpile → run.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ValueArrayFromCallLinqSubset
{
    internal static class Program
    {
        // A method that RETURNS a value-element array (not read from a field) — the
        // minimal "array from a call result" form.
        private static int[] MakeBytes(int seed)
        {
            return new[] { seed, seed + 1, seed * 2, seed + 100 };
        }

        internal static int Run()
        {
            // (1) The genuine EmitBlobs shape: a List<byte[]> indexed (a get_Item
            //     call result) returning byte[], then `.Select(...)`.
            var blobs = new List<byte[]>
            {
                new byte[] { 0x00, 0x01, 0x7f, 0xff },
                new byte[] { 0xde, 0xad, 0xbe, 0xef },
                Array.Empty<byte>(),
            };
            for (int i = 0; i < blobs.Count; i++)
            {
                // blobs[i] == List<byte[]>.get_Item(i) : a value-element byte[] from
                // a CALL RESULT, flowing into Enumerable.Select<byte,string>.
                var sb = new StringBuilder();
                sb.Append("{ ");
                sb.Append(string.Join(", ", blobs[i].Select(b => "0x" + b.ToString("x2")).ToArray()));
                sb.Append(" }");
                Console.WriteLine(sb.ToString());
            }

            // (2) A method returning int[] (call result), then `.Select(...)` and a
            //     foreach over the projected sequence.
            int sum = 0;
            foreach (int doubled in MakeBytes(5).Select(x => x * 2))
                sum += doubled;
            Console.WriteLine(sum);

            // (3) The call-result array iterated directly via Select(...).Count()
            //     (also drives IEnumerable<T>.GetEnumerator on the raw array).
            Console.WriteLine(MakeBytes(10).Select(x => x + 1).Count());

            return 0;
        }
    }
}
