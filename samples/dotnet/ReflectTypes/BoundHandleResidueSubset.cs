using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace BoundHandleResidueSubset
{
    // What a runtime-OWNED type-info answers about its own members, asked of every handle
    // BoundHandleSubset's fourteen primitives left out. Fields are closed — ten more
    // handles carry .NET's exact public field surface, and the four zeros are .NET's
    // answer too, so inventing rows there must go red. Four declines, each asserted so it
    // stays a stated boundary rather than a silence:
    //
    // - Type and Module. Their fields are MemberFilter/TypeFilter delegates and
    //   Missing.Value — values with no representation here, and a half-filled table makes
    //   the missing rows look like fields the CLR does not declare. Stated at
    //   dn2cpp_type_type.
    // - METHODS and PROPERTIES. Rows are not the cost: an owned handle's members are
    //   intrinsic — lowered at the call site, with no C++ body an invoker could name — so
    //   each row needs a hand-written invoker beside it, where a field row costs a constant.
    // - INTERFACES. Enumeration stays empty for every owned handle but String and the
    //   shared Enum, so the 0 below is structural rather than this program's reach; the
    //   consuming question is answered without rows by dn2cpp_wellknown_itf_bit
    //   (ConvertibleAssignabilitySubset above is its live oracle).
    // - The ARRAY sibling is CLOSED, not declined: a value element's SZArray map is wired
    //   eagerly (Compilation.ExpandArrayEnumerableMaps), and the two shapes that loop must
    //   skip — an intrinsic value element, a struct with no default equality — carry the
    //   five generic collection interfaces as RELATION-only rows on ti_arr_<E>, so
    //   enumeration agrees with the type test. Dispatch through one keeps its loud "has no
    //   map" abort: the rows have nullptr slot tables, so every dispatch reader skips them.
    //
    // The r-late line's subject is not the skip shapes but the TIMING: an array named only
    // by a reflection table ROW is noted after the emit fixpoint, so its rows can only come
    // from the confirmation-point sweep. Deleting it deletes the only proof that the answer
    // does not depend on WHEN an element was noted.

    // A value element nothing ever casts to a collection interface: the array-side
    // subject of the last two lines below.
    internal struct Marker
    {
        public int X;
    }

    // The timing subject: an element whose array is named ONLY by a reflection table row.
    // Nothing allocates a LateNoted[], names typeof(LateNoted[]) or casts one, so the
    // one thing that notes the element is the emitter's member-type pre-note
    // (TypeMetadataEmitter.NoteReflectedMemberArrayElements) — which runs after the
    // emit fixpoint, where no round can plant rows for it any more. It has a default
    // equality, so had it been noted in time the eager loop would have given it a real
    // dispatch map: 6 rows here means the planting is keyed on noting order again.
    internal struct LateNoted
    {
        public int N;
    }

    internal static class LateNotedHolder
    {
        // Never called, and its body names nothing: an app-module method row survives the
        // reflection trim whatever its reachability, so the method table types a parameter
        // with LateNoted[] while no IL site anywhere notes the element.
        internal static void Take(LateNoted[] items)
        {
        }
    }

    // A struct with NO default equality (explicit layout bars synthesis, nothing
    // overrides Equals): the eager value loop must skip its array, so its rows come
    // from the relation-only set.
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
    internal struct NoEqOverlay
    {
        [System.Runtime.InteropServices.FieldOffset(0)] public int A;
        [System.Runtime.InteropServices.FieldOffset(0)] public float B;
    }

    internal static class Program
    {
        private static string Render(object v)
        {
            if (v is null)
                return "<null>";
            if (v is string s)
                return "'" + s + "'";
            // Ticks rather than a formatted date: a fixture must not read the host.
            if (v is DateTime dt)
                return dt.Ticks.ToString(CultureInfo.InvariantCulture) + "/" + dt.Kind;
            if (v is DateTimeOffset dto)
                return dto.Ticks.ToString(CultureInfo.InvariantCulture)
                    + "/" + dto.Offset.Ticks.ToString(CultureInfo.InvariantCulture);
            if (v is TimeSpan ts)
                return ts.Ticks.ToString(CultureInfo.InvariantCulture);
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        private static void Fields(string label, Type t)
        {
            FieldInfo[] fs = t.GetFields();
            string[] names = new string[fs.Length];
            for (int i = 0; i < fs.Length; i++)
                names[i] = fs[i].Name;
            Array.Sort(names, StringComparer.Ordinal);
            Console.WriteLine("own " + label + " fields=" + names.Length
                + " [" + string.Join(",", names) + "]");
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = t.GetField(names[i]);
                Console.WriteLine("  " + label + "." + f.Name
                    + " type=" + f.FieldType.FullName
                    + " decl=" + f.DeclaringType.FullName
                    + " static=" + f.IsStatic + " lit=" + f.IsLiteral + " ro=" + f.IsInitOnly
                    + " pub=" + f.IsPublic
                    + " attrs=0x" + ((int)f.Attributes).ToString("X", CultureInfo.InvariantCulture)
                    + " val=" + Render(f.GetValue(null)));
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== bound handle residue: the remaining owned handles' field tables ==");
            Fields("string", typeof(string));
            Fields("decimal", typeof(decimal));
            Fields("TimeSpan", typeof(TimeSpan));
            Fields("DateTime", typeof(DateTime));
            Fields("DateTimeOffset", typeof(DateTimeOffset));
            Fields("ConstructorInfo", typeof(ConstructorInfo));
            Fields("StackTrace", typeof(System.Diagnostics.StackTrace));
            Fields("StackFrame", typeof(System.Diagnostics.StackFrame));
            Fields("ResourceManager", typeof(System.Resources.ResourceManager));
            Fields("WaitHandle", typeof(System.Threading.WaitHandle));
            // .NET declares no public field on these four, so an empty table IS the
            // right answer and a later addition must be deliberate.
            Console.WriteLine("r-none DateOnly=" + typeof(DateOnly).GetFields().Length
                + " TimeOnly=" + typeof(TimeOnly).GetFields().Length
                + " StringBuilder=" + typeof(StringBuilder).GetFields().Length
                + " MethodInfo=" + typeof(MethodInfo).GetFields().Length);
            // The same shape one level out from the primitives: a lookup and the line
            // after it, which is a NullReferenceException without a table.
            Console.WriteLine("r-shape decimal.MaxValue.FieldType = "
                + typeof(decimal).GetField("MaxValue").FieldType.FullName);
            Console.WriteLine("r-shape TimeSpan.TicksPerSecond.FieldType = "
                + typeof(TimeSpan).GetField("TicksPerSecond").FieldType.FullName);
            // Exactly .NET's set, not a superset, and reachable from every reader.
            Console.WriteLine("r-miss string.Length-as-field = "
                + (typeof(string).GetField("Length") is null ? "null" : "found"));
            Console.WriteLine("r-member string.Empty via GetMember = "
                + typeof(string).GetMember("Empty").Length);
            Console.WriteLine("r-bindflags TimeSpan public|static = "
                + typeof(TimeSpan).GetFields(BindingFlags.Public | BindingFlags.Static).Length
                + " instance = "
                + typeof(TimeSpan).GetFields(BindingFlags.Public | BindingFlags.Instance).Length);

            Console.WriteLine("== bound handle residue: the declined tables, stated ==");
            // DIVERGES from real .NET (6 and 2): the reason is at dn2cpp_type_type.
            Console.WriteLine("r-DIVERGES Type.GetFields = " + typeof(Type).GetFields().Length
                + " Module.GetFields = " + typeof(Module).GetFields().Length);
            // DIVERGES from real .NET (54 / 117 / 177 / 100 methods): an owned handle's
            // members are intrinsic, so a method row would need a hand-written invoker.
            Console.WriteLine("r-DIVERGES methods int=" + typeof(int).GetMethods().Length
                + " decimal=" + typeof(decimal).GetMethods().Length
                + " string=" + typeof(string).GetMethods().Length
                + " StringBuilder=" + typeof(StringBuilder).GetMethods().Length);
            // DIVERGES from real .NET (2 / 1 / 15 / 18 properties), same argument.
            Console.WriteLine("r-DIVERGES props string=" + typeof(string).GetProperties().Length
                + " decimal=" + typeof(decimal).GetProperties().Length
                + " TimeSpan=" + typeof(TimeSpan).GetProperties().Length
                + " DateTime=" + typeof(DateTime).GetProperties().Length);
            // DIVERGES from real .NET (32 / 31 / 10 / 1 interfaces). typeof(string) is
            // deliberately not counted here — its rows ARE this program's dispatch map,
            // where these four are structurally row-less.
            Console.WriteLine("r-DIVERGES itfs int=" + typeof(int).GetInterfaces().Length
                + " decimal=" + typeof(decimal).GetInterfaces().Length
                + " DateTime=" + typeof(DateTime).GetInterfaces().Length
                + " StringBuilder=" + typeof(StringBuilder).GetInterfaces().Length);
            // The half that is NOT declined, beside it: enumeration lags, the pair-gate
            // a real consumer asks does not.
            Console.WriteLine("r-ok assignable IConvertible<-int = "
                + typeof(IConvertible).IsAssignableFrom(typeof(int))
                + " IComparable<-decimal = "
                + typeof(IComparable).IsAssignableFrom(typeof(decimal))
                + " IFormattable<-DateTime = "
                + typeof(IFormattable).IsAssignableFrom(typeof(DateTime)));
            Console.WriteLine("r-DIVERGES GetInterface IConvertible on int = "
                + (typeof(int).GetInterface("IConvertible")?.Name ?? "null"));
            // The array half's POSITIVE control (header's fourth decline): a struct
            // element nothing casts still enumerates like .NET, so a change that stopped
            // wiring value elements eagerly is red here rather than on a count.
            Console.WriteLine("r-ok Marker[] itfs = " + typeof(Marker[]).GetInterfaces().Length
                + " IList`1 = " + (typeof(Marker[]).GetInterface("IList`1")?.Name ?? "null"));
            Console.WriteLine("r-ok assignable IList<Marker><-Marker[] = "
                + typeof(IList<Marker>).IsAssignableFrom(typeof(Marker[]))
                + " IEnumerable<-Marker[] = "
                + typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(Marker[])));
            // Both eager-loop skip shapes enumerate .NET's 11 rows.
            // The dispatch half stays a loud abort by design and cannot be asserted in
            // a running bucket; the relation rows' nullptr slots are what keep it so.
            string[] tsItfs = new string[typeof(TimeSpan[]).GetInterfaces().Length];
            for (int i = 0; i < tsItfs.Length; i++)
                tsItfs[i] = typeof(TimeSpan[]).GetInterfaces()[i].Name;
            Array.Sort(tsItfs, StringComparer.Ordinal);
            Console.WriteLine("r-closed TimeSpan[] itfs = " + tsItfs.Length
                + " [" + string.Join(",", tsItfs) + "]");
            Console.WriteLine("r-closed NoEqOverlay[] itfs = "
                + typeof(NoEqOverlay[]).GetInterfaces().Length
                + " IList`1 = " + (typeof(NoEqOverlay[]).GetInterface("IList`1")?.Name ?? "null"));
            bool tsRow = false;
            foreach (Type itf in typeof(TimeSpan[]).GetInterfaces())
                if (itf == typeof(IList<TimeSpan>))
                    tsRow = true;
            Console.WriteLine("r-closed identity IList<TimeSpan> row = " + tsRow
                + " assignable = " + typeof(IList<TimeSpan>).IsAssignableFrom(typeof(TimeSpan[])));
            // The timing half of the same contract: an array reached only through
            // a parameter ROW — no newarr, no typeof, no cast — is noted after the
            // fixpoint, so its rows come from the sweep rather than from a round.
            // The generic argument is read off the row itself, which is also what proves
            // the rows are closed over THIS element (their Names are element-free).
            Type lateArr = typeof(LateNotedHolder)
                .GetMethod("Take", BindingFlags.Static | BindingFlags.NonPublic)
                .GetParameters()[0].ParameterType;
            Type lateList = lateArr.GetInterface("IList`1");
            Console.WriteLine("r-late " + lateArr.Name + " itfs = " + lateArr.GetInterfaces().Length
                + " IList`1 = " + (lateList?.Name ?? "null")
                + " arg = " + (lateList is null ? "null" : lateList.GetGenericArguments()[0].Name));
        }
    }
}
