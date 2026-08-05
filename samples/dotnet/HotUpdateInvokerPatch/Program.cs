using System;
using HotUpdateBase;

namespace HotInvokerPatch;

// Corruption fixture for the kShapeInvoker receiver. A method import binds by
// NAME alone — declaring type, method name, sigShape and staticness, every one
// of them a string out of the image's own pool — so a malformed `.bpi` can label
// a call site with a base-image method whose declaring type its receiver is not
// an instance of. Nothing signs a `.bpi`; hotupdate_read_file loads whatever is
// on disk. This is the shape bound from the declaring type's own metadata, where
// a `callvirt` reads `self->type->vtable[slot]` at a slot index that came from
// the DECLARED type — a receiver that is not one carries no length for it.
//
// The gate rewrites a single pooled name, in place and with no length prefix to
// move (both are 21 bytes):
//
//   "HotUpdateBase.QuotaEx"  ->  "HotUpdateBase.Counter"
//
// which re-labels the one `Describe` import from QuotaEx's non-virtual reader
// onto Counter's VIRTUAL Describe — vtable slot and all — while the receiver is
// still the QuotaEx the base image handed over.
//
// So: do NOT add members here, and in particular never `new` a QuotaEx and never
// catch one. A ctor import or an EH clause on that type would follow the same
// pooled string when it is rewritten, and the load would fail on the broken bind
// before the confusion under test could fire. The instance comes from
// Counter.SeedQuota — a well-known static on a DIFFERENT type — for exactly that
// reason, which is also why that seed exists in the base image at all.
internal static class Program
{
    private static void Main()
    {
        QuotaEx q = Counter.SeedQuota;
        // The one import the gate re-labels.
        Console.WriteLine(q.Describe());
    }
}
