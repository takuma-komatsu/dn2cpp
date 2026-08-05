using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssemblyIdentitySubset
{
    // The Assembly/Module object-escape surface. Both types are modeled
    // as the defining assembly's simple-name `const char*` — headerless, like
    // the NFI trio — and used to TRAP (PlatformNotSupportedException) on any
    // escape into `object`. They now take the interned-wrapper treatment
    // (dn2cpp_asm_wrap), whose header is the PRIVATE implementation identity
    // real .NET reports (RuntimeAssembly/RuntimeModule over the abstract
    // shells), so every line below matches real .NET verbatim — including
    // GetType().Name == "RuntimeAssembly" and GetType() != typeof(Assembly).
    //
    // The section's real SUBJECT is wider than its reflection heading: the
    // wrap/unwrap funnel at Cast (escape + reverse), castclass/isinst, array
    // slots (stelem wraps, ldelem unwraps), object-keyed Dictionary interning,
    // reference equality across two escapes, the reflection Invoke thunk's _A
    // arms, reflected field Get/SetValue, CreateDelegate's dgrefl trampolines
    // under the erased f_method ABI, and delegate VARIANCE over both a
    // dgrefl-bound and a method-group delegate (the conversion that emits no
    // IL at all). Do not prune it by theme.
    //
    // Func<object> is deliberately NEVER constructed here: it exists only as a
    // variance VIEW of Func<Assembly>, so the cd-var/mg-var invocations are also
    // the regression assert for view-only delegate types: without
    // Compilation.DelegateInvokerUses one gets no emitted dginvoke_* at all — a green
    // transpile that dies in clang on an undeclared identifier.
    internal static class Refl
    {
        public static string Take(Assembly a) => "T:" + a.FullName;
        public static Assembly Give() => typeof(Refl).Assembly;
        public static string TakeMod(Module m) => "M:" + m.Name;
        public static Assembly Field;
    }

    internal static class Program
    {
        internal static void Run()
        {
            Console.WriteLine("== assembly/module identity ==");
            Assembly app = typeof(Program).Assembly;
            Module mod = typeof(Program).Module;

            // The escape itself: ToString, isinst, GetType over the wrapper.
            object oa = app;
            object om = mod;
            Console.WriteLine("tostr-a:'" + oa + "'");
            Console.WriteLine("tostr-m:'" + om + "'");
            Console.WriteLine("is-a:" + (oa is Assembly) + ":" + (oa is Module));
            Console.WriteLine("is-m:" + (om is Module) + ":" + (om is Assembly));
            Console.WriteLine("gettype-a:" + oa.GetType().Name + ":" + oa.GetType().FullName);
            Console.WriteLine("gettype-m:" + om.GetType().Name);
            Console.WriteLine("gt-eq-typeofasm:" + (oa.GetType() == typeof(Assembly)));
            Console.WriteLine("cast-back:" + ((Assembly)oa).FullName);
            Console.WriteLine("cast-back-m:" + ((Module)om).Name);

            // Interning: two escapes of one assembly are reference-equal, like
            // .NET's singleton Assembly instance.
            object oa2 = typeof(Refl).Assembly;
            Console.WriteLine("refeq:" + ReferenceEquals(oa, oa2));
            Console.WriteLine("refeq2:" + ReferenceEquals(oa, app));
            Console.WriteLine("eq:" + oa.Equals(app) + ":" + Equals(oa, om));

            // Array slots (stelem wraps, ldelem unwraps) and the object view.
            object[] arr = { app, mod };
            Console.WriteLine("arr:" + (arr[0] is Assembly) + ":'" + arr[1] + "'");
            Assembly[] aarr = { app, typeof(Refl).Assembly };
            object cov = aarr[1];
            Console.WriteLine("aarr:" + ReferenceEquals(cov, app) + ":'" + aarr[0] + "'");

            // Object-keyed dictionary: identity hash over the interned wrapper.
            var d = new Dictionary<object, string> { [app] = "APP" };
            Console.WriteLine("dict:" + d[typeof(Refl).Assembly]);

            // The failing casts stay honest.
            try { object x = (Assembly)om; Console.WriteLine("xcast:none:" + x); }
            catch (InvalidCastException) { Console.WriteLine("xcast:InvalidCastException"); }
            Console.WriteLine("xis:" + (om is Assembly));

            // Reflection Invoke: Assembly/Module arguments and an Assembly return
            // (the invoker thunk's _A shape letters).
            MethodInfo mt = typeof(Refl).GetMethod("Take");
            Console.WriteLine("inv-take:" + mt.Invoke(null, new object[] { app }));
            MethodInfo mg = typeof(Refl).GetMethod("Give");
            object gv = mg.Invoke(null, null);
            Console.WriteLine("inv-give:" + (gv is Assembly) + ":'" + gv + "':" + ReferenceEquals(gv, app));
            MethodInfo mm = typeof(Refl).GetMethod("TakeMod");
            Console.WriteLine("inv-mod:" + mm.Invoke(null, new object[] { mod }));

            // Reflected field: SetValue unwraps, GetValue wraps.
            typeof(Refl).GetField("Field").SetValue(null, app);
            Console.WriteLine("fld-set:" + ReferenceEquals(Refl.Field, app));
            object fv = typeof(Refl).GetField("Field").GetValue(null);
            Console.WriteLine("fld-get:" + (fv is Assembly) + ":" + ReferenceEquals(fv, app));

            // CreateDelegate (the dgrefl trampolines) with an Assembly
            // parameter and an Assembly return.
            var ft = (Func<Assembly, string>)mt.CreateDelegate(typeof(Func<Assembly, string>));
            Console.WriteLine("cd-take:" + ft(app));
            var fg = (Func<Assembly>)mg.CreateDelegate(typeof(Func<Assembly>));
            Assembly ga = fg();
            Console.WriteLine("cd-give:'" + ga.FullName + "':" + ReferenceEquals(ga, app));

            // Variance over the dgrefl-bound delegate, then over a method group —
            // one f_method read through two delegate types, under the erased ABI.
            // Func<object> exists ONLY as these views (see the header).
            Func<Assembly> fgv = fg;
            Func<object> fo = fgv;
            object vo = fo();
            Console.WriteLine("cd-var:" + (vo is Assembly) + ":'" + vo + "'");
            Func<Assembly> mgd = Refl.Give;
            Func<object> mgo = mgd;
            object mo = mgo();
            Console.WriteLine("mg-var:" + (mo is Assembly) + ":'" + mo + "'");
            Func<Assembly, string> tk = Refl.Take;
            Console.WriteLine("mg-par:" + tk(app));

            // Identity hash consistency between the escaped and the static view,
            // and Module.Assembly's identity under the single-module model.
            Console.WriteLine("hash:" + (oa.GetHashCode() == app.GetHashCode()));
            Console.WriteLine("mod-asm:" + ReferenceEquals(mod.Assembly, app));

            // The CoreLib assembly's display name, straight from the registry
            // entry of the very DLL the gate transpiled.
            object oc = typeof(int).Assembly;
            Console.WriteLine("corelib:'" + oc + "'");
        }
    }
}
