#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqOrderStructKeySubset
{
    // SUBJECT: the shared-generics shape of the ordering pipeline's generic virtual
    // method. `OrderBy(...).ThenBy(...)` lowers ThenBy to
    // IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>, which has no
    // interface-table slot and so routes through a dn2cpp_gvm_* type-switch
    // dispatcher whose cases are the ALLOCATED implementing types.
    //
    // A VALUE-typed element with REFERENCE-typed keys is the shape that puts a
    // canonical group owner in that allocated set: Enumerable.OrderedIterator<Row,
    // string> groups under Enumerable.OrderedIterator<Row, $CnRef>, whose one shared
    // body does the newobj — so the owner is allocated exactly as its real member is,
    // while carrying no type-info of its own (the shared ctor stamps the real ti_ it
    // is handed through the rgctx ClassAlloc slot). The dispatcher must therefore
    // never branch on a canonical owner's ti_: that leaves the transpile green and
    // fails the C++ compile on an undeclared identifier. Compiling this section IS
    // the regression assert; running it proves the surviving real case still
    // dispatches (a dropped case traps instead of ordering).
    //
    // ws.OrderBy(...).ThenBy(...) in LinqOrderSubset does NOT reach this: with a
    // reference element type the instantiations stay real and no canonical owner
    // is allocated.
    internal readonly struct Row
    {
        internal Row(string module, string entryPoint, int order)
        {
            Module = module;
            EntryPoint = entryPoint;
            Order = order;
        }

        internal string Module { get; }
        internal string EntryPoint { get; }
        internal int Order { get; }
    }

    internal static class Program
    {
        internal static int Run()
        {
            List<Row> rows = new List<Row>
            {
                new Row("b", "y", 2),
                new Row("a", "z", 1),
                new Row("a", "y", 3),
                new Row("b", "x", 4),
            };

            // Two reference-typed keys: both OrderedIterator<Row, string>.
            foreach (Row r in rows.OrderBy(r => r.Module).ThenBy(r => r.EntryPoint))
                Console.WriteLine("k1 " + r.Module + "/" + r.EntryPoint + "/" + r.Order);

            // A second ThenBy at a VALUE-typed key, so the dispatcher carries a
            // second instantiation whose group is a different one.
            foreach (Row r in rows.OrderBy(r => r.Module).ThenBy(r => r.Order))
                Console.WriteLine("k2 " + r.Module + "/" + r.EntryPoint + "/" + r.Order);

            // Descending tie-break through the same dispatcher.
            foreach (Row r in rows.OrderBy(r => r.Module).ThenByDescending(r => r.EntryPoint))
                Console.WriteLine("k3 " + r.Module + "/" + r.EntryPoint + "/" + r.Order);

            return 0;
        }
    }
}
