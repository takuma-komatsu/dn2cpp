#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;

// Collection-interface DISPATCH whose runtime resolution succeeds through the
// reference-element SZArray rules — the covariant-element fallback arm and the ref-erased
// object-wrapper rows — but which the REACH closure must mirror: exact + variant matching
// alone leaves the served row's slots unfilled, and a null slot is a null-pointer call
// (SIGSEGV, not a loud abort). An unreached array-map slot is a trap stub, never nullptr.
//
//  - `new List<MemberInfo>(typeof(Probe).GetFields())`: the runtime-materialized
//    FieldInfo[] is dispatched by the List ctor as ICollection<MemberInfo> — an
//    INVARIANT interface at an element the corpus never names as an array-as-interface
//    shape (no cast/boundary anywhere says IEnumerable<MemberInfo> etc. over an
//    array). Only the CLR's array-element covariance (FieldInfo[] is
//    ICollection<MemberInfo>) serves it: through the object-map fallback arm, and —
//    with shared generics — through the canonical alias row on ti_arr_FieldInfo.
//  - the invariant generic trio over a Gem[] reached through `object`, requested at
//    the BASE element (ICollection<Stone>/IList<Stone> over a Gem[]): array
//    covariance again — variance matching cannot serve an invariant interface, and
//    the exact per-element rows are Gem-keyed. Rides the ref-erased object rows
//    (and, in shared bodies, the canonical alias rows), i.e. both new reach arms.
//
// Value-element arrays reached this way are now closed — served by their own eagerly
// wired per-element maps, never these fallback/erased rows — and are exercised by
// ObjectReachedValueDispatchSubset. CoreLib only. Diffed exact vs .NET.

namespace ReflectionArrayFallbackSubset
{
    public sealed class Probe
    {
        public int A;
        public string B;
        public double C;
    }

    class Stone
    {
        public string Tag;
        public Stone(string tag) { Tag = tag; }
        public override string ToString() { return "Stone:" + Tag; }
    }

    // Created only as a Gem[] that is immediately forgotten behind `object`; the only
    // collection-interface casts in this file name Stone, never Gem — so Gem's own
    // array map rows can never carry the requested instantiations, and the dispatch
    // genuinely rides the fallback/erased rows.
    sealed class Gem : Stone
    {
        public Gem(string tag) : base(tag) { }
        public override string ToString() { return "Gem:" + Tag; }
    }

    static class Program
    {
        // The reflection arrays are runtime-materialized; nothing in this program
        // statically names MemberInfo's array-as-interface shape.
        static void ReflectionMemberList()
        {
            List<MemberInfo> members = new List<MemberInfo>(typeof(Probe).GetFields());
            Console.WriteLine("refl-n=" + members.Count);
            string all = "";
            foreach (MemberInfo m in members)
                all += "[" + m.Name + "]";
            Console.WriteLine("refl-names=" + all);

            // The exact-element list over the same runtime-materialized array
            // must stay green beside the interface-typed path above.
            List<FieldInfo> fields = new List<FieldInfo>(typeof(Probe).GetFields());
            Console.WriteLine("refl-exact-n=" + fields.Count + " refl-exact-0=" + fields[0].Name);
        }

        static object MakeGems()
        {
            return new Gem[] { new Gem("ruby"), new Gem("opal"), new Gem("jade") };
        }

        // Invariant generic collection interfaces at the BASE element over a
        // Derived[] behind object: served by array-element covariance only.
        static void InvariantBaseTrioOnObject(object o)
        {
            ICollection<Stone> c = (ICollection<Stone>)o;
            Console.WriteLine("gem-count=" + c.Count);
            IList<Stone> l = (IList<Stone>)o;
            Stone s1 = l[1];
            Console.WriteLine("gem-idx1=" + s1 + " gem-contains=" + l.Contains(s1)
                + " gem-miss=" + l.Contains(new Gem("onyx")) + " gem-indexOf=" + l.IndexOf(s1));
            string caught = "none";
            try { c.Add(new Gem("amber")); }
            catch (NotSupportedException) { caught = "NotSupportedException"; }
            Console.WriteLine("gem-add=" + caught);
            List<Stone> list = new List<Stone>((IEnumerable<Stone>)o);
            Console.WriteLine("gem-list-n=" + list.Count + " gem-list-2=" + list[2]);
        }

        public static void Run()
        {
            ReflectionMemberList();
            InvariantBaseTrioOnObject(MakeGems());
        }
    }
}
