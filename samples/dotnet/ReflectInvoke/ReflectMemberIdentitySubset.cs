#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReflectMemberIdentitySubset
{
    // SUBJECT: reflected member-handle IDENTITY. .NET's member cache hands back
    // the SAME instance for a member however it is reached, so ReferenceEquals,
    // Equals, a stable GetHashCode, HashSet dedup and List.Contains all agree
    // across two Get* calls; dn2cpp interns the wrappers per metadata row to
    // match. The tail is a serializer's member-selection shape, where
    // fresh-minted handles silently drop every unattributed public member.
    public class Gizmo
    {
        public string? Name;
        public int Count;
        public static double Rate;
        private bool hidden;

        public string? Label { get; set; }
        public int Size { get; set; }

        public Gizmo() { hidden = false; }
        public Gizmo(int size) { Size = size; hidden = true; }

        public int Alpha() { return hidden ? 1 : Count; }
        public string Beta(int x) { return (Name ?? "") + x; }
    }

    // Unattributed public fields beside an attributed one: the shape whose
    // unattributed half is what a broken identity model drops.
    public class MembraneLike
    {
        public string? Name;
        public string? AlbedoTexture;
        public string? NormalTexture;
        public bool CellWall;
        private int version;

        [Obsolete("attr-carrier")]
        public string? IconPath;

        public string? DissolverEnzyme { get; set; }

        public int Version() { return version; }
        public MembraneLike() { version = 1; }
    }

    static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("== field identity ==");
            FieldInfo[] fa = typeof(Gizmo).GetFields();
            FieldInfo[] fb = typeof(Gizmo).GetFields();
            Console.WriteLine($"count {fa.Length}/{fb.Length}");
            bool refEq = true, eq = true, opEq = true, hashEq = true;
            for (int i = 0; i < fa.Length; i++)
            {
                refEq &= ReferenceEquals(fa[i], fb[i]);
                eq &= fa[i].Equals(fb[i]) && ((object)fa[i]).Equals(fb[i]);
                opEq &= fa[i] == fb[i];
                hashEq &= fa[i].GetHashCode() == fb[i].GetHashCode();
            }
            Console.WriteLine($"refeq={refEq} equals={eq} op={opEq} hash={hashEq}");
            FieldInfo? byName = typeof(Gizmo).GetField("Name");
            Console.WriteLine($"getfield-same={ReferenceEquals(byName, fa[0])}");
            var fset = new HashSet<FieldInfo>();
            int accepted = 0;
            foreach (FieldInfo f in fa) if (fset.Add(f)) accepted++;
            foreach (FieldInfo f in fb) if (fset.Add(f)) accepted++;
            Console.WriteLine($"hashset unique={fset.Count} accepted={accepted}");
            var flist = new List<FieldInfo>(fa);
            bool contains = true;
            foreach (FieldInfo f in fb) contains &= flist.Contains(f);
            Console.WriteLine($"list-contains={contains}");

            Console.WriteLine("== property identity ==");
            PropertyInfo[] pa = typeof(Gizmo).GetProperties();
            PropertyInfo[] pb = typeof(Gizmo).GetProperties();
            bool pRef = true, pHash = true;
            for (int i = 0; i < pa.Length; i++)
            {
                pRef &= ReferenceEquals(pa[i], pb[i]);
                pHash &= pa[i].GetHashCode() == pb[i].GetHashCode();
            }
            Console.WriteLine($"count={pa.Length} refeq={pRef} hash={pHash}");
            PropertyInfo? pByName = typeof(Gizmo).GetProperty("Label");
            bool pFound = false;
            foreach (PropertyInfo p in pa) if (ReferenceEquals(p, pByName)) pFound = true;
            Console.WriteLine($"getproperty-same={pFound}");
            var pset = new HashSet<PropertyInfo>();
            foreach (PropertyInfo p in pa) pset.Add(p);
            foreach (PropertyInfo p in pb) pset.Add(p);
            Console.WriteLine($"hashset unique={pset.Count}");

            Console.WriteLine("== method identity ==");
            const BindingFlags declared =
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            MethodInfo[] ma = typeof(Gizmo).GetMethods(declared);
            MethodInfo[] mb = typeof(Gizmo).GetMethods(declared);
            bool mRef = true;
            for (int i = 0; i < ma.Length; i++)
                mRef &= ReferenceEquals(ma[i], mb[i]);
            Console.WriteLine($"count={ma.Length} refeq={mRef}");
            MethodInfo? mByName = typeof(Gizmo).GetMethod("Alpha");
            bool mFound = false;
            foreach (MethodInfo m in ma) if (ReferenceEquals(m, mByName)) mFound = true;
            Console.WriteLine($"getmethod-same={mFound}");
            var mset = new HashSet<MethodInfo>();
            foreach (MethodInfo m in ma) mset.Add(m);
            foreach (MethodInfo m in mb) mset.Add(m);
            Console.WriteLine($"hashset unique={mset.Count}");

            Console.WriteLine("== constructor identity ==");
            ConstructorInfo[] ca = typeof(Gizmo).GetConstructors();
            ConstructorInfo[] cb = typeof(Gizmo).GetConstructors();
            bool cRef = true;
            for (int i = 0; i < ca.Length; i++)
                cRef &= ReferenceEquals(ca[i], cb[i]);
            Console.WriteLine($"count={ca.Length} refeq={cRef}");
            ConstructorInfo? cByType = typeof(Gizmo).GetConstructor(new Type[] { typeof(int) });
            bool cFound = false;
            foreach (ConstructorInfo c in ca) if (ReferenceEquals(c, cByType)) cFound = true;
            Console.WriteLine($"getctor-same={cFound}");

            Console.WriteLine("== serializable members (Newtonsoft shape) ==");
            // Enumerate ALL members, enumerate the DEFAULT (public) ones
            // separately, keep an all-member when defaultMembers.Contains it:
            // reference identity is what makes the unattributed members survive.
            Type t = typeof(MembraneLike);
            var all = new List<MemberInfo>();
            foreach (FieldInfo f in t.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                all.Add(f);
            foreach (PropertyInfo p in t.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                all.Add(p);
            var defaultMembers = new List<MemberInfo>();
            foreach (FieldInfo f in t.GetFields()) defaultMembers.Add(f);
            foreach (PropertyInfo p in t.GetProperties()) defaultMembers.Add(p);
            var kept = new List<string>();
            foreach (MemberInfo m in all)
                if (defaultMembers.Contains(m))
                    kept.Add(m.Name);
            Console.WriteLine("kept=" + string.Join(",", kept.ToArray()));
        }
    }
}
