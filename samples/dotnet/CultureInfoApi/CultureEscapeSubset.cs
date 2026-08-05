using System;
using System.Collections.Generic;
using System.Globalization;

namespace CultureEscapeSubset
{
    // An intrinsic-represented reference type (CultureInfo / NumberFormatInfo /
    // TextInfo — all lowered to the headerless `const Dn2CppNumberFormatInfo*`)
    // escaping into an `object` context. The escape must mint a real managed
    // object (the interned runtime wrapper, dn2cpp_nfi_wrap) — a raw pointer
    // stored as `object` makes every object-generic consumer (ToString
    // dispatch, GetType, equality, object[] elements) misread the struct's
    // first field as the type header and jump through literal-pool data
    // (the Thrive GD.Print(cultureInfo) SIGILL).
    //
    // Every line matches real .NET run WITHOUT invariant globalization (the
    // named en-US culture): dn2cpp's model carries en-US as a built-in, while
    // an InvariantGlobalization=true .NET run would refuse `new
    // CultureInfo("en-US")` outright (PredefinedCulturesOnly) — one more
    // reason this bucket is a frozen-snapshot gate.
    internal static class Program
    {
        private static object s_slot;

        private static string Show(object o) => "'" + o + "'";

        internal static void Run()
        {
            Console.WriteLine("== CultureInfo-as-object escapes ==");
            CultureInfo inv = CultureInfo.InvariantCulture;
            object o = inv; // stloc object <- CultureInfo
            Console.WriteLine("wl(o):" + Show(o));
            Console.WriteLine($"interp:'{o}'");
            Console.WriteLine($"interpT:'{inv}'"); // AppendFormatted<CultureInfo>

            CultureInfo en = new CultureInfo("en-US");
            object[] arr = { inv, en, NumberFormatInfo.InvariantInfo }; // stelem.ref
            for (int i = 0; i < arr.Length; i++)
                Console.WriteLine("arr[" + i + "]='" + arr[i].ToString() + "'");
            Console.WriteLine(string.Format("fmt:'{0}'|'{1}'", inv, en));

            // GetType: the wrapper carries the real .NET identity, and typeof
            // names the same handle.
            Console.WriteLine("t0:" + arr[0].GetType().FullName);
            Console.WriteLine("t2:" + arr[2].GetType().FullName);
            Console.WriteLine("typeofEq:" + (o.GetType() == typeof(CultureInfo)));

            // Pattern tests + casts back out of object (unwrap).
            Console.WriteLine("isCI:" + (o is CultureInfo));
            Console.WriteLine("isNFI:" + (o is NumberFormatInfo));
            CultureInfo back = (CultureInfo)arr[1];
            Console.WriteLine("back:'" + back.Name + "'");
            CultureInfo asCi = arr[2] as CultureInfo;
            Console.WriteLine("as:" + (asCi is null ? "null" : asCi.Name));
            Console.WriteLine("d:" + 3.5.ToString((IFormatProvider)((object)en)));

            // Reference identity survives the escape (the wrap is interned).
            object o2 = CultureInfo.InvariantCulture;
            Console.WriteLine("refEq:" + ReferenceEquals(o, o2));
            Console.WriteLine("mixEq:" + (o == (object)inv));
            Console.WriteLine("mixNe:" + (o == (object)en));
            Console.WriteLine("nullCk:" + (en == null));

            // Provider-typed escape: the erased static type is recovered at run
            // time (a NumberFormatInfo instance is tagged; cultures are not).
            IFormatProvider p = en;
            object po = p;
            Console.WriteLine("prov:" + po.GetType().Name + ":'" + po + "'");
            IFormatProvider pn = new NumberFormatInfo();
            object pno = pn;
            Console.WriteLine("provN:" + pno.GetType().Name);
            object invInfo = NumberFormatInfo.InvariantInfo;
            Console.WriteLine("invInfo:" + invInfo.GetType().Name);

            // TextInfo escape (same headerless representation).
            object ti = en.TextInfo;
            Console.WriteLine("ti:" + ti.GetType().Name);

            // NumberFormatInfo's default ToString via a typed receiver.
            Console.WriteLine("nfiToStr:" + new NumberFormatInfo().ToString());

            // Through an object field and a generic collection.
            s_slot = en;
            Console.WriteLine("sfld:'" + s_slot + "'");
            var list = new List<object> { en };
            Console.WriteLine("list:'" + list[0] + "'");

            // A TYPED collection over the intrinsic element (List<CultureInfo> —
            // the shared canonical List body stores the managed-object form):
            // Add/indexer/Contains/IndexOf round-trip, and the indexer result
            // feeds a typed member read (Name).
            var cl = new List<CultureInfo> { inv, en };
            Console.WriteLine("cl:" + cl.Contains(en) + ":" + cl.IndexOf(en) + ":'" + cl[1].Name + "'");
            // Typed arrays: element loads/stores + the static scans.
            CultureInfo[] tarr = new CultureInfo[2];
            tarr[0] = inv;
            tarr[1] = en;
            Console.WriteLine("aIndexOf:" + Array.IndexOf(tarr, en));
            string names = "";
            foreach (CultureInfo c in tarr)
                names += "<" + c.Name + ">";
            Console.WriteLine("each:" + names);
            // The typed array covariantly read as object[] (ldelem.ref through
            // the object static type): elements come back as the wrapped form.
            object[] oarr = tarr;
            Console.WriteLine("cov:'" + oarr[1] + "'|" + oarr[1].GetType().Name);
        }
    }
}
