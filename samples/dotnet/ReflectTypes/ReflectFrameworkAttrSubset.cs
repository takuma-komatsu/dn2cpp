using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

// FRAMEWORK-declared attributes on app elements. DecodeCustomAttributes drops a
// BCL/compiler attribute row (the IL2CPP-managed-stripping bound) — UNLESS a reachable
// user-module body names the attribute type with typeof
// (Compilation._userTypeofNamedFrameworkTypes), the statically visible evidence that
// the program reflects over it. The positive half is the Thrive transcription that
// motivated the widening: EnumHelper.GetAttribute<DescriptionAttribute> over
// [Description]-annotated enum fields (NewGameSettings' FogOfWar dropdown), i.e.
// GetMember(value.ToString())[0].GetCustomAttributes(typeof(T), false).Cast<T>()
// .First() — its lines match real .NET. The negative half pins the boundary:
// [Browsable(false)] rides the same field but NO typeof anywhere names
// BrowsableAttribute, so its row stays dropped and the untyped enumeration reports
// one attribute where real .NET reports two (a frozen divergence — the reason this
// bucket diffs a snapshot, not `dotnet $app`).

namespace ReflectFrameworkAttrSubset
{
    enum FogOfWarMode
    {
        [Description("FOG_OF_WAR_DISABLED")]
        Ignored = 0,

        [Description("FOG_OF_WAR_SOLID")]
        Solid = 1,

        [Description("FOG_OF_WAR_INTENSE")]
        Intense = 2,
    }

    class Host
    {
        // Description is typeof-named (kept); Browsable is not (dropped — the negative).
        [Description("host field"), Browsable(false)]
        public int F;
    }

    static class Program
    {
        // Thrive EnumHelper transcription: the typeof lives behind a generic parameter,
        // so the record happens while scanning the CLOSED instantiation's body.
        static TAttribute GetAttribute<TAttribute>(Enum value)
            where TAttribute : Attribute
        {
            var type = value.GetType();
            var memberInfo = type.GetMember(value.ToString());
            var attributes = memberInfo[0].GetCustomAttributes(typeof(TAttribute), false);
            return attributes.Cast<TAttribute>().First();
        }

        internal static void Run()
        {
            Console.WriteLine("== framework attrs (typeof-named keep) ==");
            foreach (FogOfWarMode mode in Enum.GetValues<FogOfWarMode>())
                Console.WriteLine($"  {mode} -> {GetAttribute<DescriptionAttribute>(mode).Description}");

            // A direct (non-generic) typeof filter on an ordinary field: same keep rule
            // without the generic-parameter indirection.
            FieldInfo f = typeof(Host).GetField("F");
            var direct = f.GetCustomAttributes(typeof(DescriptionAttribute), false);
            Console.WriteLine($"  direct Description={((DescriptionAttribute)direct[0]).Description}");
            Console.WriteLine($"  IsDefined(Description)={f.IsDefined(typeof(DescriptionAttribute), false)}");

            // The boundary's negative: BrowsableAttribute is framework-declared and
            // typeof-named NOWHERE, so its row on Host.F is dropped — the untyped
            // enumeration sees only Description (real .NET: Browsable + Description).
            var all = f.GetCustomAttributes(false);
            Console.WriteLine($"  untyped count={all.Length}");
            foreach (var name in all.Select(a => a.GetType().Name).OrderBy(n => n, StringComparer.Ordinal))
                Console.WriteLine($"  untyped {name}");
        }
    }
}
