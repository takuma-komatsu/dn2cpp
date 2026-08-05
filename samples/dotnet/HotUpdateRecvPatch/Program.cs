using System;
using HotUpdateBase;

namespace HotRecvPatch;

// Corruption fixture for an intrinsic-table receiver. A method import binds by
// NAME alone — declaring type, method name, sigShape and staticness, every one
// of them a string out of the image's own pool — so a malformed `.bpi` can label
// a call site with a row of the interpreter's intrinsic table that its receiver
// does not satisfy, and the helpers behind the kShapeRefRetObj shape cast the
// receiver raw. Nothing signs a `.bpi`; hotupdate_read_file loads whatever is on
// disk.
//
// This is the smallest patch whose import table holds exactly ONE
// kShapeRefRetObj call — System.Object::ToString on a base-image Counter — and
// in which the two pooled names the gate rewrites occur exactly once each:
//
//   "System.Object" (the ToString import's declaring type)  ->  "System.Type"
//   "ToString"      (its method name)                       ->  "get_Name"
//
// which together re-label that one import onto the System.Type::get_Name row,
// whose helper reads `((Dn2CppType*)receiver)->typeInfo`. The receiver is a
// RecvProbe. Two byte writes, both located by content rather than by a
// hardcoded offset, and the gate asserts the uniqueness before it writes.
//
// So: do NOT add members here. A second System.Object import, or any other
// occurrence of either name in the baked image, breaks the locator — the gate
// fails loudly rather than corrupting the wrong bytes, but the fixture is gone.
// RecvProbe derives from the base-image Counter on purpose: that keeps the
// TypeTable's base reference off System.Object, which is what leaves the
// declaring-type import as the pool entry's only user.
public class RecvProbe : Counter
{
    public RecvProbe(int start)
        : base(start)
    {
    }
}

internal static class Program
{
    private static void Main()
    {
        // Statically a Counter, which overrides nothing: the call binds
        // System.Object::ToString, the import the gate re-labels. Uncorrupted
        // it prints the instance's type full name.
        Counter c = new RecvProbe(1);
        Console.WriteLine(c.ToString());
    }
}
