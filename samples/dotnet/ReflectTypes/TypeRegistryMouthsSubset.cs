#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading;

// The two MOUTHS of one type registry, and the one LOOKUP behind a synthetic gendef
// Neither half is about reflection as a feature; both are about a fact being
// resolved on one route and not on the other, which is invisible from the route that
// works.
//
// (a) A generic definition dn2cpp mints neither ClassInfo nor Template for — an
// intrinsic-modeled one — decodes to a bare External NAME, so its synthetic gendef reads
// its own metadata through the type index. The parameter names took that route already;
// the kind bits did not, and read Unknown, so typeof(Vector128<>) answered IsValueType
// False and IsSealed False about a sealed struct. The List`1 / IComparable`1 lines beside
// it are the control: the same four bits down the Template route, so a green here is not
// a green about a route nothing takes.
//
// (b) typeof asks CoreIntrinsics.RuntimeTypeInfoSymbol (through ClassInfo.CppTypeInfoName)
// for the handle of a type the C++ runtime owns; Type.GetType reads the emitted name
// registry. While the registry's seed was a second, hand-written list, a type rowed in one
// and not the other answered null by NAME while typeof handed back a live handle — and
// null is what a caller cannot tell from "no such type". The names below span all three
// runtime tables (owned, bound, raised). Which of them a hand-written seed happened to
// miss was a fact about the PROGRAM as much as the list — a name the emit set also carries
// got its row from the emitted-class loop instead — and that is the argument for deriving
// the seed rather than listing it.
//
// Every line matches real .NET.

namespace TypeRegistryMouthsSubset
{
    class Program
    {
        internal static void Run()
        {
            // Seed a close, so the definition handle is reached the way a program reaches
            // it rather than only by this section's typeof.
            Vector128<long> v = Vector128.Create(3L, 4L);
            Console.WriteLine("regseed " + Vector128.Sum(v));
            ShowDef("Vector128`1", typeof(Vector128<>));
            ShowDef("List`1", typeof(List<>));
            ShowDef("IComparable`1", typeof(IComparable<>));

            SameByName("System.Int32", typeof(int));
            SameByName("System.Enum", typeof(Enum));
            SameByName("System.IntPtr", typeof(IntPtr));
            SameByName("System.Text.StringBuilder", typeof(StringBuilder));
            SameByName("System.Diagnostics.StackTrace", typeof(System.Diagnostics.StackTrace));
            SameByName("System.Threading.WaitHandle", typeof(WaitHandle));
            SameByName("System.Threading.ManualResetEventSlim", typeof(ManualResetEventSlim));
            SameByName("System.AggregateException", typeof(AggregateException));
        }

        private static void ShowDef(string tag, Type d) =>
            Console.WriteLine("regdef " + tag + " IsValueType=" + d.IsValueType
                + " IsAbstract=" + d.IsAbstract + " IsInterface=" + d.IsInterface
                + " IsSealed=" + d.IsSealed + " IsClass=" + d.IsClass
                + " ToString=" + d.ToString());

        private static void SameByName(string name, Type t)
        {
            Type g = Type.GetType(name);
            Console.WriteLine("regname " + name + " found=" + (g is not null) + " same=" + (g == t));
        }
    }
}
