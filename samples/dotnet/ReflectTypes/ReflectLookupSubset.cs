#nullable enable
using System;
using System.Reflection;

// Exercises the COMPLETE System.Type member-lookup overload surface:
// every public GetMethod (13) / GetConstructor (4) / GetProperty (7)
// overload — including the genericParameterCount, Binder and
// CallingConventions forms — plus GetMember/GetMembers/GetDefaultMembers
// and GetMemberWithSameMetadataDefinitionAs. Covers null-return no-match
// cases and the AmbiguousMatchException cases real .NET throws.
namespace ReflectLookupSubset
{
    class Base
    {
        public Base() { }
        public Base(int a, string b) { _ = a; _ = b; }
        protected Base(double d) { _ = d; }
        public int Dup(int x) => x;
        public string Named(string s) => s;
        public string Named(int i) => "i" + i;
        public virtual int Virt() => 1;
        public static double Stat(double d) => d * 2;
        private long Hidden(long v) => v + 1;
        public long TouchHidden() => Hidden(3);
        public int Field0 = 7;
        public string? Prop { get; set; }
        public int RO => 5;
    }

    class Derived : Base
    {
        public new int Dup(int x) => x + 1;
        public override int Virt() => 2;
        public T Echo<T>(T v) => v;
        public int Echo(int v) => v + 10;
        public T? Pick<T>(string s, int i) { _ = s; _ = i; return default; }
        public string this[int i] => "i" + i;
        public string this[string k] => "k" + k;
        public new string? Prop { get; set; }
        public class Inner { public int Z; }
    }

    class Box<T>
    {
        public T? Val() => default;
    }

    static class Program
    {
        static string M(MethodBase? m) =>
            m is null ? "null" : $"{m.Name}/{m.GetParameters().Length}p decl={m.DeclaringType!.Name} static={m.IsStatic}";

        static string Pr(PropertyInfo? p) =>
            p is null ? "null" : $"{p.Name}:{p.PropertyType.Name} decl={p.DeclaringType!.Name}";

        static void Try(string tag, Func<object?> f)
        {
            try
            {
                Console.WriteLine($"{tag} => {f() ?? "null"}");
            }
            catch (AmbiguousMatchException) { Console.WriteLine($"{tag} => ambiguous"); }
            catch (ArgumentNullException) { Console.WriteLine($"{tag} => argnull"); }
            catch (ArgumentException) { Console.WriteLine($"{tag} => argerr"); }
        }

        // Order is unspecified; sort for a stable diff. Object-inherited members
        // are opaque to dn2cpp, so exclude them on both sides; a Type member's
        // DeclaringType (enclosing type) is not modeled, so print it typelessly.
        static void DumpMembers(string tag, MemberInfo[] ms)
        {
            var kept = new System.Collections.Generic.List<string>();
            foreach (MemberInfo m in ms)
                if (m.DeclaringType != typeof(object))
                    kept.Add($"{(int)m.MemberType} {m.Name}" + (m is Type ? "" : $" decl={m.DeclaringType!.Name}"));
            kept.Sort(StringComparer.Ordinal);
            Console.WriteLine($"{tag} ({kept.Count})");
            foreach (string s in kept)
                Console.WriteLine($"  {s}");
        }

        internal static void Run()
        {
            Console.WriteLine("== lookup ==");
            Type d = typeof(Derived);
            Type b = typeof(Base);
            const BindingFlags PI = BindingFlags.Public | BindingFlags.Instance;

            // Keep the generic methods, indexers and nested type reachable.
            // Echo<int> is instantiated EXPLICITLY (plain Echo(1) binds to the
            // non-generic overload) so the gm-gen1-closed divergence case below
            // exercises a closed generic row that really exists in the image.
            var inst = new Derived();
            _ = inst.Echo(1); _ = inst.Echo<int>(2); _ = inst.Echo("x"); _ = inst.Pick<double>("p", 1);
            _ = inst[3]; _ = inst["k"]; _ = inst.TouchHidden(); _ = Base.Stat(1.5);
            _ = new Derived.Inner { Z = 1 }.Z;
            _ = new Box<int>().Val(); _ = new Box<string>().Val();

            // GetMethod: every public overload shape.
            Try("gm-name", () => M(d.GetMethod("Dup")));
            Try("gm-ambiguous", () => M(d.GetMethod("Named")));
            Try("gm-missing", () => M(d.GetMethod("Nope")));
            Try("gm-flags", () => M(b.GetMethod("Hidden", BindingFlags.NonPublic | BindingFlags.Instance)));
            Try("gm-flags-types", () => M(d.GetMethod("Named", PI, new[] { typeof(int) })));
            Try("gm-types", () => M(d.GetMethod("Named", new[] { typeof(string) })));
            Try("gm-types-mods", () => M(d.GetMethod("Named", new[] { typeof(int) }, null)));
            Try("gm-binder", () => M(d.GetMethod("Named", PI, null, new[] { typeof(int) }, null)));
            Try("gm-defaultbinder", () => M(d.GetMethod("Dup", PI, Type.DefaultBinder, new[] { typeof(int) }, null)));
            Try("gm-callconv", () => M(d.GetMethod("Named", PI, null, CallingConventions.Any, new[] { typeof(string) }, null)));
            Try("gm-gen1", () => M(d.GetMethod("Pick", 1, new[] { typeof(string), typeof(int) })));
            Try("gm-gen1-mods", () => M(d.GetMethod("Pick", 1, new[] { typeof(string), typeof(int) }, null)));
            Try("gm-gen0", () => M(d.GetMethod("Echo", 0, new[] { typeof(int) })));
            Try("gm-gen1-flags", () => M(d.GetMethod("Pick", 1, PI, new[] { typeof(string), typeof(int) })));
            Try("gm-gen1-binder", () => M(d.GetMethod("Pick", 1, PI, null, new[] { typeof(string), typeof(int) }, null)));
            Try("gm-gen1-cc", () => M(d.GetMethod("Pick", 1, PI, null, CallingConventions.Standard, new[] { typeof(string), typeof(int) }, null)));
            Try("gm-gen2-miss", () => M(d.GetMethod("Pick", 2, new[] { typeof(string), typeof(int) })));
            // INTENTIONAL DIVERGENCE: real .NET matches genericParameterCount
            // lookups against the OPEN definition's parameter types (Echo<T>'s T
            // never equals typeof(int) -> null); the AOT model carries closed
            // instantiation rows, so the Echo<int> row matches and is returned.
            Try("gm-gen1-closed", () => M(d.GetMethod("Echo", 1, new[] { typeof(int) })));
            Try("gm-null-name", () => M(d.GetMethod(null!)));

            // GetConstructor: every public overload shape.
            Try("gc-empty", () => M(b.GetConstructor(Type.EmptyTypes)));
            Try("gc-types", () => M(b.GetConstructor(new[] { typeof(int), typeof(string) })));
            Try("gc-miss", () => M(b.GetConstructor(new[] { typeof(bool) })));
            Try("gc-flags", () => M(b.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, new[] { typeof(double) })));
            Try("gc-binder", () => M(b.GetConstructor(PI, null, Type.EmptyTypes, null)));
            Try("gc-cc", () => M(b.GetConstructor(PI, null, CallingConventions.Any, new[] { typeof(int), typeof(string) }, null)));

            // GetProperty: every public overload shape.
            Try("gp-name", () => Pr(d.GetProperty("Prop")));
            Try("gp-miss", () => Pr(d.GetProperty("Nope")));
            Try("gp-flags", () => Pr(d.GetProperty("Prop", PI)));
            Try("gp-ret", () => Pr(b.GetProperty("RO", typeof(int))));
            Try("gp-ret-miss", () => Pr(b.GetProperty("RO", typeof(string))));
            Try("gp-indexer-ambiguous", () => Pr(d.GetProperty("Item")));
            Try("gp-types", () => Pr(d.GetProperty("Item", new[] { typeof(int) })));
            Try("gp-ret-types", () => Pr(d.GetProperty("Item", typeof(string), new[] { typeof(string) })));
            Try("gp-ret-types-mods", () => Pr(d.GetProperty("Item", typeof(string), new[] { typeof(int) }, null)));
            Try("gp-binder", () => Pr(d.GetProperty("Item", PI, null, typeof(string), new[] { typeof(int) }, null)));

            // GetMember / GetMembers / GetDefaultMembers.
            DumpMembers("member-du*", d.GetMember("Du*"));
            DumpMembers("member-item", d.GetMember("Item"));
            DumpMembers("member-field", d.GetMember("Field0"));
            DumpMembers("member-nested", d.GetMember("Inner"));
            DumpMembers("member-named-methods", d.GetMember("Named", MemberTypes.Method, PI));
            DumpMembers("member-named-props", d.GetMember("Named", MemberTypes.Property, PI));
            DumpMembers("member-ctor", b.GetMember(".ctor", MemberTypes.Constructor, PI));
            DumpMembers("default-members", d.GetDefaultMembers());
            DumpMembers("default-members-none", b.GetDefaultMembers());
            DumpMembers("members-base", b.GetMembers(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));

            // GetMemberWithSameMetadataDefinitionAs across a generic instantiation.
            MethodInfo? val = typeof(Box<int>).GetMethod("Val");
            Try("samemeta", () =>
            {
                MemberInfo found = typeof(Box<string>).GetMemberWithSameMetadataDefinitionAs(val!);
                return $"{(int)found.MemberType} {found.Name} ret={((MethodInfo)found).ReturnType.Name}";
            });
            Try("samemeta-miss", () => b.GetMemberWithSameMetadataDefinitionAs(val!));
        }
    }
}
