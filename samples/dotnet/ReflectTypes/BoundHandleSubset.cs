using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BoundHandleSubset
{
    // What a runtime-OWNED type-info answers about itself. Owned tables are hand-written
    // in runtime/, so where a table is absent the reflection surface answers "empty" — a
    // wrong answer that reads like a true one. Four shapes:
    //
    // (a) THE PRIMITIVES' FIELD TABLES. All fourteen DN2CPP_TF_PRIMITIVE handles carry
    //     real-.NET's exact public static field surface (dn2cpp_typeinfo.cpp's
    //     dn2cpp_primflds_*), values included. Hand-written rather than bound from emitted
    //     metadata: seven of the fourteen are intrinsic types the emitter emits no
    //     metadata FOR, and every row is a literal, which the emitter emits with no getter.
    //
    // (b) THE ARRAY INTERFACE LISTS. `is IList` on an array answers by flag and a CALL
    //     through one dispatches through the array maps; neither reaches
    //     Type.GetInterfaces(), which reads the type-info's own rows. One shared
    //     relation-only row set serves every array — nullptr slots, so it is invisible to
    //     dispatch and cannot shadow the maps.
    //
    // (c) THE RUNTIME-ALLOCATED ARRAY HANDLE. Enum.GetValues(Type) is a DECLARED
    //     divergence, marked below: its elements are boxes, so claiming the E[] handle
    //     would make ldelem read a pointer as an integer — the reason is written at the
    //     lowering (MethodCompiler.EmitIntrinsic.EnumArray.cs).
    //
    // (d) THE ERASED GENERICS. One Dn2CppThreadLocal struct serves every T, so no
    //     per-instantiation ti_ is stamped on an instance: ThreadLocal names the shared
    //     handle — the choice Task<T> already made — and inherits its residue, a type test
    //     that over-accepts a DIFFERENT instantiation. The Task lines below are the
    //     precedent, asserted beside them so the two can never quietly disagree.
    //     BlockingCollection<T> rides the identical IntrinsicCppName arm but lives in
    //     System.Collections.Concurrent, which this bucket does not reference; its
    //     agreeing half is asserted by build-and-run-blocking-collection.sh instead.
    //
    // Every line here matches real .NET except the THREE whose label starts "DIVERGES",
    // which are asserted precisely so a change to them is visible. (A fourth line carries
    // "DIVERGES" in its label and does NOT diverge: c-DIVERGES-but-works is the control
    // that the wrong Type identity above it costs nothing else.)
    internal static class Program
    {
        private enum Hue { Red = 1, Green = 2, Blue = 4 }

        // A value rendered so the expectation file stays printable and host-independent:
        // a char as its code point (MinValue is U+0000, which must not go into a text
        // fixture), everything else through the invariant culture.
        private static string Render(object v)
        {
            if (v is null)
                return "<null>";
            if (v is char c)
                return "U+" + ((int)c).ToString("X4", CultureInfo.InvariantCulture);
            if (v is string s)
                return "'" + s + "'";
            return Convert.ToString(v, CultureInfo.InvariantCulture);
        }

        private static string[] SortedNames(Type[] types)
        {
            string[] names = new string[types.Length];
            for (int i = 0; i < types.Length; i++)
                names[i] = types[i].Name;
            // Ordinal: GetInterfaces() order is unspecified, and Array.Sort(string[])
            // would compare with the current culture (which need not agree between
            // hosts). Same rule as ReflectGenericSubset.DumpInterfaces.
            Array.Sort(names, StringComparer.Ordinal);
            return names;
        }

        // How many of the six non-generic interfaces every CLR array implements appear in
        // this type's list. 6 for any array (and for System.Array), 0 for anything else —
        // the same on real .NET, which is what makes it usable as the leak control.
        private static int ArrayItfsOn(Type t)
        {
            string[] six =
            {
                "IEnumerable", "ICollection", "IList",
                "ICloneable", "IStructuralComparable", "IStructuralEquatable",
            };
            int n = 0;
            foreach (Type itf in t.GetInterfaces())
                foreach (string want in six)
                    if (itf.Name == want)
                        n++;
            return n;
        }

        private static void Itfs(string label, Type t)
        {
            string[] names = SortedNames(t.GetInterfaces());
            Console.WriteLine("itf " + label + " = " + names.Length + " [" + string.Join(",", names) + "]");
        }

        private static void PrimFields(string label, Type t)
        {
            FieldInfo[] fs = t.GetFields();
            string[] names = new string[fs.Length];
            for (int i = 0; i < fs.Length; i++)
                names[i] = fs[i].Name;
            Array.Sort(names, StringComparer.Ordinal);
            Console.WriteLine("prim " + label + " fields=" + names.Length + " [" + string.Join(",", names) + "]");
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
            Console.WriteLine("== bound handles (a): the primitives' reflection field tables ==");
            // All fourteen: filling half the set is what makes the OTHER half look like a
            // type that genuinely has no such field.
            PrimFields("bool", typeof(bool));
            PrimFields("char", typeof(char));
            PrimFields("sbyte", typeof(sbyte));
            PrimFields("byte", typeof(byte));
            PrimFields("short", typeof(short));
            PrimFields("ushort", typeof(ushort));
            PrimFields("int", typeof(int));
            PrimFields("uint", typeof(uint));
            PrimFields("long", typeof(long));
            PrimFields("ulong", typeof(ulong));
            PrimFields("nint", typeof(IntPtr));
            PrimFields("nuint", typeof(UIntPtr));
            PrimFields("float", typeof(float));
            PrimFields("double", typeof(double));
            // The shape this exists for: a lookup and the line after it, which used to
            // be a NullReferenceException.
            Console.WriteLine("a-shape short.MaxValue.FieldType = "
                + typeof(short).GetField("MaxValue").FieldType.FullName);
            // A miss still misses — the tables are exactly .NET's, not a superset.
            Console.WriteLine("a-miss bool.MaxValue = "
                + (typeof(bool).GetField("MaxValue") is null ? "null" : "found"));
            Console.WriteLine("a-miss nint.MaxValue = "
                + (typeof(IntPtr).GetField("MaxValue") is null ? "null" : "found"));
            // A hand-written row is a real row: it reaches every reader, not only GetFields.
            Console.WriteLine("a-member int.MaxValue via GetMember = "
                + typeof(int).GetMember("MaxValue").Length);
            Console.WriteLine("a-bindflags int public|static = "
                + typeof(int).GetFields(BindingFlags.Public | BindingFlags.Static).Length
                + " instance = "
                + typeof(int).GetFields(BindingFlags.Public | BindingFlags.Instance).Length);
            // An owned handle keeps what only it knows: the field tables are additive,
            // not a bind over emitted metadata.
            Console.WriteLine("a-owned int.IsPrimitive=" + typeof(int).IsPrimitive
                + " short.IsPrimitive=" + typeof(short).IsPrimitive
                + " decimal.IsPrimitive=" + typeof(decimal).IsPrimitive);

            Console.WriteLine("== bound handles (b): array interface enumeration ==");
            Itfs("string[]", typeof(string[]));
            Itfs("int[]", typeof(int[]));
            Itfs("Hue[]", typeof(Hue[]));
            Itfs("int[,]", typeof(int[,]));
            Itfs("string[,]", typeof(string[,]));
            Itfs("int[,,]", typeof(int[,,]));
            // The abstract base itself: an intrinsic type, so the emitter gives it an
            // opaque shell with no table, and real .NET's answer IS exactly these six.
            Itfs("Array", typeof(Array));
            // Nothing else gained rows — the shared set is gated on the array flags, and
            // a reader that forgot the guard would report ICloneable on every type in the
            // program. Asked as "how many of the six appear", not as a whole interface
            // list: a non-array's list is reachability-driven here (an enum's is 4 rows on
            // real .NET and 0 in a program that dispatches none of them), so a raw count
            // would assert this bucket's reach closure rather than the leak.
            Console.WriteLine("b-noleak object = " + ArrayItfsOn(typeof(object))
                + " Hue = " + ArrayItfsOn(typeof(Hue))
                + " int[] = " + ArrayItfsOn(typeof(int[]))
                + " int[,] = " + ArrayItfsOn(typeof(int[,])));
            // GetInterface / FindInterfaces read the SAME row set. A GetInterfaces() that
            // lists ICloneable while GetInterface("ICloneable") answers null would be an
            // internal contradiction, which is what three hand-written loops would give.
            Console.WriteLine("b-one int[,].GetInterface(ICloneable) = "
                + (typeof(int[,]).GetInterface("ICloneable")?.Name ?? "null"));
            Console.WriteLine("b-one string[].GetInterface(IStructuralEquatable) = "
                + (typeof(string[]).GetInterface("IStructuralEquatable")?.Name ?? "null"));
            Console.WriteLine("b-one string[].GetInterface(IEnumerable) = "
                + (typeof(string[]).GetInterface("IEnumerable")?.Name ?? "null"));
            Console.WriteLine("b-one object.GetInterface(ICloneable) = "
                + (typeof(object).GetInterface("ICloneable")?.Name ?? "null"));
            Console.WriteLine("b-find string[] = "
                + typeof(string[]).FindInterfaces(delegate { return true; }, null).Length
                + " int[,] = "
                + typeof(int[,]).FindInterfaces(delegate { return true; }, null).Length);
            // ldtoken needs an MD arm for these: a null Type makes the line above throw
            // rather than answer 0. Everything an MD Type must answer:
            Console.WriteLine("b-md name=" + typeof(int[,]).Name
                + " full=" + typeof(int[,]).FullName
                + " isArray=" + typeof(int[,]).IsArray
                + " rank=" + typeof(int[,]).GetArrayRank()
                + " elem=" + typeof(int[,]).GetElementType().Name
                + " base=" + typeof(int[,]).BaseType.Name);
            int[,] mdInstance = new int[2, 3];
            Console.WriteLine("b-md identity self=" + (mdInstance.GetType() == typeof(int[,]))
                + " rank3=" + (typeof(int[,]) == typeof(int[,,]))
                + " sz=" + (typeof(int[,]) == typeof(int[]))
                + " elem=" + (typeof(int[,]).GetElementType() == typeof(int)));
            // The type-TEST half answers by flag; enumeration reads rows. Assert they agree.
            Console.WriteLine("b-test IList<-int[,] = "
                + typeof(System.Collections.IList).IsAssignableFrom(typeof(int[,]))
                + " ICloneable<-int[,] = "
                + typeof(ICloneable).IsAssignableFrom(typeof(int[,]))
                + " IList<int><-int[,] = "
                + typeof(IList<int>).IsAssignableFrom(typeof(int[,])));
            // And DISPATCH through the shared MD map still works — the relation rows
            // carry nullptr slots precisely so they cannot shadow it.
            Console.WriteLine("b-dispatch md as IList Count = "
                + ((System.Collections.IList)mdInstance).Count
                + " IsFixedSize = " + ((System.Collections.IList)mdInstance).IsFixedSize);

            Console.WriteLine("== bound handles (c): a runtime-allocated array's handle ==");
            // The five allocation shapes that carry the precise handle already.
            Console.WriteLine("c-ok newarr = " + (new int[2].GetType() == typeof(int[])));
            Console.WriteLine("c-ok CreateInstance = "
                + (Array.CreateInstance(typeof(int), 2).GetType() == typeof(int[])));
            Console.WriteLine("c-ok Split = " + ("a,b".Split(',').GetType() == typeof(string[])));
            Console.WriteLine("c-ok GetMethods = "
                + (typeof(int).GetMethods().GetType() == typeof(MethodInfo[])));
            Console.WriteLine("c-ok enum newarr = " + (new Hue[1].GetType() == typeof(Hue[])));
            // The enum array materializations, which are easy to allocate untyped.
            Console.WriteLine("c-fix GetValues<Hue>() = "
                + (Enum.GetValues<Hue>().GetType() == typeof(Hue[]))
                + " name=" + Enum.GetValues<Hue>().GetType().Name);
            Console.WriteLine("c-fix GetNames<Hue>() = "
                + (Enum.GetNames<Hue>().GetType() == typeof(string[])));
            Console.WriteLine("c-fix GetNames(typeof(Hue)) = "
                + (Enum.GetNames(typeof(Hue)).GetType() == typeof(string[]))
                + " name=" + Enum.GetNames(typeof(Hue)).GetType().Name);
            // A statically-known typeof(E) answers like real .NET: the packed Hue[] the
            // generic spelling mints, which real code casts to E[].
            Console.WriteLine("c-fix GetValues(typeof(Hue)) == typeof(Hue[]) = "
                + (Enum.GetValues(typeof(Hue)).GetType() == typeof(Hue[]))
                + " name=" + Enum.GetValues(typeof(Hue)).GetType().Name);
            // …and it still enumerates through System.Array, boxing each element.
            int seen = 0;
            foreach (object v in Enum.GetValues(typeof(Hue)))
                seen += ((Hue)v == Hue.Green) ? 10 : 1;
            Console.WriteLine("c-fix GetValues walk = " + seen);
            // DIVERGES from real .NET (Hue[] there): a RUNTIME-chosen Type keeps the
            // boxed object[] answer — its elements are boxes, so the precise handle
            // would lie about the representation. Stated at the lowering
            // (MethodCompiler.EmitIntrinsic.EnumArray.cs); the walk still works.
            Type rtChosen = Environment.TickCount >= int.MinValue ? typeof(Hue) : typeof(DayOfWeek);
            Console.WriteLine("c-DIVERGES GetValues(runtime Type) name="
                + Enum.GetValues(rtChosen).GetType().Name);
            int rseen = 0;
            foreach (object v in Enum.GetValues(rtChosen))
                rseen += ((Hue)v == Hue.Green) ? 10 : 1;
            Console.WriteLine("c-DIVERGES-but-works GetValues(runtime Type) walk = " + rseen);

            Console.WriteLine("== bound handles (d): the erased generics, and the Task precedent ==");
            object tl = new ThreadLocal<string>();
            object tk = Task.FromResult("x");
            Console.WriteLine("d-right ThreadLocal<string> = " + (tl is ThreadLocal<string>));
            Console.WriteLine("d-right Task<string> = " + (tk is Task<string>));
            Console.WriteLine("d-right GetType()==typeof(ThreadLocal<string>) = "
                + (tl.GetType() == typeof(ThreadLocal<string>)));
            // DIVERGES from real .NET (False there): one handle per family cannot tell
            // instantiations apart. Asserted beside the Task line it is now identical to —
            // the whole point of (d) was to make these three agree, so a future change
            // that moves one of them must move all three or go red here.
            Console.WriteLine("d-DIVERGES ThreadLocal<int> = " + (tl is ThreadLocal<int>));
            Console.WriteLine("d-DIVERGES Task<int> = " + (tk is Task<int>));
            // The erasure is only about the HEADER: T rides as data, so the instance
            // still behaves at its own T.
            ((ThreadLocal<string>)tl).Value = "held";
            Console.WriteLine("d-data ThreadLocal value = " + ((ThreadLocal<string>)tl).Value);
            // A non-family type is unaffected: the arms key on the runtime STRUCT, so
            // nothing else started answering True.
            Console.WriteLine("d-other ThreadLocal<string> from object = "
                + (new object() is ThreadLocal<string>));
        }
    }
}
