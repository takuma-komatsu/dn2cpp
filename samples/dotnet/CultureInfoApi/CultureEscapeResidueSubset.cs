using System;
using System.Buffers;
using System.Globalization;
using System.Reflection;

namespace CultureEscapeResidueSubset
{
    // The object-escape boundaries CultureEscapeSubset does NOT reach.
    // Its sections cover the mainstream ones — assignment, stelem/stfld, call
    // arguments and results, castclass/isinst, reflection field thunks. These
    // five reach the headerless `const Dn2CppNumberFormatInfo*` through paths
    // that spell no object conversion at all, and every one of them used to pun
    // the raw pointer and die reading a type header out of culture fields:
    //
    //   (a) a `ref CultureInfo` read/written through the type-ERASED ldind.ref /
    //       stind.ref, whose one spelling covers every byref slot;
    //   (b) a covariant method group (Func<object> over a CultureInfo-returning
    //       method) AND the delegate VARIANCE conversion, which emits no IL to
    //       hook — one delegate instance invoked through two delegate types;
    //   (c) a generic body instantiated at CultureInfo calling GetType() through
    //       `constrained.`, which never goes through ldind.ref;
    //   (d) a NON-generic class implementing IHolder<CultureInfo>: the caller
    //       wraps because the canonical IHolder<__Canon> reads the position as an
    //       object, and this implementer's concrete body does not. This one
    //       failed SILENTLY — an empty CultureInfo.Name, no crash;
    //   (e) reflection Invoke, whose thunk shape collapsed every pointer
    //       parameter and return onto one letter.
    //
    // The tail covers the OTHER headerless representations. Assembly/Module
    // (`const char*`) take the interned-wrapper treatment — the identity .NET reports for
    // them is the FIXED RuntimeAssembly/RuntimeModule pair, which a hand-written runtime
    // type-info can carry — so f-asm/f-mod print .NET's "ok" (the deep asserts live in the
    // ReflectTypes bucket's AssemblyIdentitySubset). SearchValues<T> cannot be wrapped
    // (its .NET identity is a private PER-SHAPE subclass this model cannot enumerate) and
    // still traps loudly; that one line is a DELIBERATE divergence, and it is why this
    // bucket is a frozen-snapshot gate rather than a live diff.
    internal interface IHolder<T>
    {
        string Show(T v);
    }

    internal sealed class CiHolder : IHolder<CultureInfo>
    {
        public string Show(CultureInfo v) => "H:" + v.Name;
    }

    internal class Gvm
    {
        public virtual string M<T>(T t) => "G:" + t.GetType().Name + ":" + t;
    }

    internal sealed class GvmD : Gvm
    {
        public override string M<T>(T t) => "D:" + t.GetType().Name + ":" + t;
    }

    internal static class Refl
    {
        public static string Take(CultureInfo c) => "T:" + c.Name;
        public static CultureInfo Give() => CultureInfo.InvariantCulture;
        public static string TakeProv(IFormatProvider p) => "P:" + p.GetType().Name;
    }

    internal static class Program
    {
        private static CultureInfo s_ci = CultureInfo.InvariantCulture;

        private static CultureInfo GetCi() => CultureInfo.InvariantCulture;

        private static void ReadByRef(ref CultureInfo r)
        {
            object o = r; // ldarg.0; ldind.ref — no conversion in the IL
            Console.WriteLine("a-read:" + o.GetType().Name + ":'" + o + "'");
        }

        private static void WriteByRef(ref object slot, CultureInfo c)
        {
            slot = c; // ldarg.0; ldarg.1; stind.ref
        }

        // Each escape kept in its own method; the catch documents what a
        // regression back to the trap would print in place of "ok".
        private static string AssemblyEscape()
        {
            try
            {
                object o = typeof(int).Assembly;
                return o is Assembly ? "ok" : "wrong";
            }
            catch (PlatformNotSupportedException)
            {
                return "PlatformNotSupportedException";
            }
        }

        private static string ModuleEscape()
        {
            try
            {
                object o = typeof(int).Module;
                return o is Module ? "ok" : "wrong";
            }
            catch (PlatformNotSupportedException)
            {
                return "PlatformNotSupportedException";
            }
        }

        private static string SearchValuesEscape()
        {
            SearchValues<char> sv = SearchValues.Create("abc");
            try
            {
                object o = sv;
                return o is SearchValues<char> ? "ok" : "wrong";
            }
            catch (PlatformNotSupportedException)
            {
                return "PlatformNotSupportedException";
            }
        }

        internal static void Run()
        {
            Console.WriteLine("== headerless-escape residue ==");
            CultureInfo en = new CultureInfo("en-US");

            // (a) the type-erased byref pair.
            ReadByRef(ref s_ci);
            object slot = "x";
            WriteByRef(ref slot, en);
            Console.WriteLine("a-write:" + slot.GetType().Name + ":'" + slot + "'");

            // (b) covariant method group, then the variance conversion of an
            // already-built delegate (no IL of its own).
            Func<object> f = GetCi;
            object r1 = f();
            Console.WriteLine("b-mg:" + r1.GetType().Name + ":'" + r1 + "'");
            Func<CultureInfo> g = GetCi;
            Func<object> f2 = g;
            object r2 = f2();
            Console.WriteLine("b-cov:" + r2.GetType().Name + ":'" + r2 + "'");
            // The parameter half of the same ABI, in both directions: a delegate
            // declaring the NFI type over a method taking it, and the
            // CONTRAVARIANT conversion of an object-taking delegate to it.
            Func<CultureInfo, string> fp = Refl.Take;
            Console.WriteLine("b-par:" + fp(en));
            Func<object, string> fo = o => "O:" + o.GetType().Name + ":'" + o + "'";
            Func<CultureInfo, string> fc = fo;
            Console.WriteLine("b-ctr:" + fc(en));

            // (c) GetType() through `constrained.` in a generic body at T=CultureInfo,
            // reached both virtually and non-virtually.
            Gvm gv = new GvmD();
            Console.WriteLine("c-gvm:" + gv.M<CultureInfo>(en));
            Console.WriteLine("c-gvmb:" + new Gvm().M<CultureInfo>(en));

            // (d) the erased interface slot of a non-generic implementer, beside the
            // direct call the same body serves.
            IHolder<CultureInfo> h = new CiHolder();
            Console.WriteLine("d-itf:" + h.Show(en));
            Console.WriteLine("d-dir:" + new CiHolder().Show(en));

            // (e) reflection Invoke with an NFI argument, an NFI return, and an
            // IFormatProvider parameter (the erased static type).
            MethodInfo mt = typeof(Refl).GetMethod("Take");
            Console.WriteLine("e-take:" + mt.Invoke(null, new object[] { en }));
            MethodInfo mg = typeof(Refl).GetMethod("Give");
            object gvv = mg.Invoke(null, null);
            Console.WriteLine("e-give:" + gvv.GetType().Name + ":'" + gvv + "'");
            MethodInfo mp = typeof(Refl).GetMethod("TakeProv");
            Console.WriteLine("e-prov:" + mp.Invoke(null, new object[] { en }));

            // Assembly/Module wrap ("ok", as real .NET); SearchValues stays the loud trap
            // (real .NET: "ok" — the declared divergence).
            Console.WriteLine("f-asm:" + AssemblyEscape());
            Console.WriteLine("f-mod:" + ModuleEscape());
            Console.WriteLine("g-sv:" + SearchValuesEscape());
        }
    }
}
