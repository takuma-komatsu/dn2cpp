using System;
using System.Reflection;

namespace MultiAssembly
{
    internal static class Program
    {
        private static int Main()
        {
            // The REFERENCED library's [ModuleInitializer] (MiniBcl.Boot.Init) must already
            // have run: nothing calls it, so it runs only if the library's `<Module>` .cctor
            // is rooted across the -r boundary. A reference assembly's .cctor is otherwise
            // pulled in by ALLOCATING its declaring type, and `<Module>` is never allocated.
            // Assert it loudly — this gate has no stdout oracle, and a silently-missing
            // registration hook is exactly the failure being covered.
            // (This bucket runs against the mock MiniCorlib, not the real BCL — so the
            // failure path stays on plain Console.WriteLine + a non-zero exit.)
            if (!MiniBcl.Boot.Ready)
            {
                Console.WriteLine("FAIL: library module initializer did not run");
                return 1;
            }
            Console.WriteLine("lib module init: " + MiniBcl.Boot.Banner);

            // List<T> lives in a SEPARATE assembly (MiniCorlib.dll). The
            // transpiler resolves the cross-assembly reference, monomorphizes
            // List<int>/List<string>, and compiles their bodies into the app.
            MiniBcl.List<int> squares = new MiniBcl.List<int>();
            int i = 0;
            while (i < 8)
            {
                squares.Add(i * i);
                i = i + 1;
            }

            // foreach over a collection defined in another assembly.
            int sum = 0;
            foreach (int sq in squares)
            {
                sum = sum + sq;
            }
            Console.WriteLine(squares.Count);
            Console.WriteLine(squares[7]);
            Console.WriteLine(sum);

            MiniBcl.List<string> words = new MiniBcl.List<string>();
            words.Add("multi");
            words.Add("assembly");
            words[0] = "cross";
            string joined = "";
            foreach (string w in words)
            {
                joined = string.Concat(joined, w, " ");
            }
            Console.WriteLine(joined);

            // A second collection from the same library assembly.
            MiniBcl.Stack<int> stack = new MiniBcl.Stack<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            Console.WriteLine(string.Concat(
                stack.Pop().ToString(), stack.Pop().ToString(), stack.Pop().ToString()));

            // A custom attribute DECLARED in the referenced library (MiniBcl.TagAttribute),
            // applied to members DECLARED in that library (MiniBcl.Tagged), is visible from app
            // code across -r via GetCustomAttributes/IsDefined — with ctor args and named args
            // intact. The class is allocated and its method called so it is a fully-emitted
            // (non-opaque) type, which is the shape an app-module-only attribute table would
            // still drop. No stdout oracle here, so assert loudly with a distinct exit code
            // per site.
            MiniBcl.Tagged inst = new MiniBcl.Tagged();
            inst.Field = 7;
            inst.Prop = 9;
            inst.Method();

            Type tt = typeof(MiniBcl.Tagged);
            if (!tt.IsDefined(typeof(MiniBcl.TagAttribute), false))
            {
                Console.WriteLine("FAIL: library type attribute not defined");
                return 2;
            }
            object[] tas = tt.GetCustomAttributes(typeof(MiniBcl.TagAttribute), false);
            if (tas.Length != 1 || !(tas[0] is MiniBcl.TagAttribute ta)
                || ta.Name != "lib-class" || ta.Level != 3)
            {
                Console.WriteLine("FAIL: library type attribute args wrong");
                return 3;
            }

            MethodInfo mi = tt.GetMethod("Method");
            object[] mas = mi.GetCustomAttributes(typeof(MiniBcl.TagAttribute), false);
            if (mas.Length != 1 || !(mas[0] is MiniBcl.TagAttribute mta)
                || mta.Name != "lib-method" || mta.Level != 4)
            {
                Console.WriteLine("FAIL: library method attribute args wrong");
                return 4;
            }

            FieldInfo fi = tt.GetField("Field");
            object[] fas = fi.GetCustomAttributes(typeof(MiniBcl.TagAttribute), false);
            if (fas.Length != 1 || !(fas[0] is MiniBcl.TagAttribute fta)
                || fta.Name != "lib-field" || fta.Level != 1)
            {
                Console.WriteLine("FAIL: library field attribute args wrong");
                return 5;
            }

            PropertyInfo pi = tt.GetProperty("Prop");
            object[] pas = pi.GetCustomAttributes(typeof(MiniBcl.TagAttribute), false);
            if (pas.Length != 1 || !(pas[0] is MiniBcl.TagAttribute pta)
                || pta.Name != "lib-prop" || pta.Level != 2)
            {
                Console.WriteLine("FAIL: library property attribute args wrong");
                return 6;
            }

            Console.WriteLine("lib attr: type " + ta.Name + "/" + ta.Level
                + " method " + mta.Name + "/" + mta.Level
                + " field " + fta.Name + "/" + fta.Level
                + " prop " + pta.Name + "/" + pta.Level);

            // Reflection-ctor route, LIBRARY-target extension (the Thrive GameWiki
            // shape): types DECLARED in the referenced library, never constructed
            // statically, built late-bound the way a JSON deserializer builds them.
            // The route is opened by the NAMED user surface — here the generic-method
            // type argument LoadShaped<WikiPage>() passes, exactly Thrive's
            // LoadDirectObject<GameWiki>("wiki.json") — not by library membership.
            // Before the extension the surface walk skipped non-generic library
            // types entirely, so WikiPage's ctor table read (nullptr, 0). Four
            // Newtonsoft sub-shapes:
            //  (a) parameterless ctor via non-generic Activator.CreateInstance(Type);
            //  (b) population via PropertyInfo.SetValue/GetValue on accessors nothing
            //      calls statically (GameWiki's `{ get; set; } = null!;` members);
            //  (c) a type with NO parameterless ctor via GetConstructor(...).Invoke
            //      (Newtonsoft binds the parameterized ctor — GameWiki.Page);
            //  (d) a library generic specialization named ONLY by a library member
            //      type, constructed off PropertyInfo.PropertyType (the List<Page>
            //      shape: an OPENED target's member types join the surface).
            Type wpt = typeof(MiniBcl.WikiPage);
            object page = LoadShaped<MiniBcl.WikiPage>();
            MiniBcl.WikiPage typedPage = page as MiniBcl.WikiPage;
            if (typedPage == null)
            {
                Console.WriteLine("FAIL: library parameterless reflection ctor");
                return 7;
            }
            PropertyInfo titleProp = wpt.GetProperty("Title");
            titleProp.SetValue(page, "reflected-title");
            if ((string)titleProp.GetValue(page) != "reflected-title")
            {
                Console.WriteLine("FAIL: library property accessors via reflection");
                return 8;
            }
            ConstructorInfo eci = GetCtorShaped<MiniBcl.WikiEntry>(
                new Type[] { typeof(string), typeof(int) });
            if (eci == null)
            {
                Console.WriteLine("FAIL: library parameterized ctor row missing");
                return 9;
            }
            MiniBcl.WikiEntry entry =
                eci.Invoke(new object[] { "lib-entry", 5 }) as MiniBcl.WikiEntry;
            if (entry == null || entry.Name != "lib-entry" || entry.Level != 5)
            {
                Console.WriteLine("FAIL: library parameterized reflection ctor");
                return 10;
            }
            PropertyInfo sectionsProp = wpt.GetProperty("Sections");
            object sections = Activator.CreateInstance(sectionsProp.PropertyType);
            MiniBcl.List<MiniBcl.WikiSection> typedSections =
                sections as MiniBcl.List<MiniBcl.WikiSection>;
            if (typedSections == null)
            {
                Console.WriteLine("FAIL: library specialization ctor via PropertyType");
                return 11;
            }
            typedSections.Add(new MiniBcl.WikiSection());
            sectionsProp.SetValue(page, sections);
            if (typedPage.Sections.Count != 1 || typedPage.Sections[0].Heading != "empty")
            {
                Console.WriteLine("FAIL: library collection property round-trip");
                return 12;
            }
            Console.WriteLine("lib reflection ctor: " + typedPage.Title
                + " entry " + entry.Name + "/" + entry.Level
                + " sections " + typedPage.Sections.Count);

            // Creator-ARGUMENT surface of an opened library target (the Thrive
            // VersionPatchNotes shape): List<WikiRef> is named ONLY as
            // WikiChangelog's ctor parameter — no field, property or body names it
            // — so constructing it off ParameterInfo.ParameterType asserts that an
            // opened target's instance-ctor parameter types join the
            // reflection-ctor route's named surface (a deserializer binding a
            // parameterized creator deserializes each argument at the declared
            // parameter type before the object exists). WikiRef rides along as the
            // parameter type's own type argument: its ctor and accessors must be
            // open too, or the element the "deserializer" builds could not be
            // populated.
            ConstructorInfo cci = GetCtorsShaped<MiniBcl.WikiChangelog>()[0];
            if (cci.GetParameters().Length != 2)
            {
                Console.WriteLine("FAIL: library creator ctor row missing");
                return 13;
            }
            Type refsType = cci.GetParameters()[1].ParameterType;
            object refs = Activator.CreateInstance(refsType);
            if (refs == null)
            {
                Console.WriteLine("FAIL: creator-argument type not constructible");
                return 14;
            }
            Type refItem = refsType.GetGenericArguments()[0];
            object oneRef = Activator.CreateInstance(refItem);
            PropertyInfo targetProp = refItem.GetProperty("Target");
            targetProp.SetValue(oneRef, "w-ref");
            MiniBcl.WikiChangelog changelog =
                cci.Invoke(new object[] { "1.0", refs }) as MiniBcl.WikiChangelog;
            if (changelog == null || changelog.Version != "1.0" || changelog.RefCount != 0)
            {
                Console.WriteLine("FAIL: library creator reflection ctor");
                return 15;
            }
            Console.WriteLine("lib creator args: " + refsType.Name
                + " item " + refItem.Name
                + " target " + (string)targetProp.GetValue(oneRef)
                + " version " + changelog.Version + "/" + changelog.RefCount);
            return 0;
        }

        // Mirrors Thrive's LoadDirectObject<T>: the generic-method TYPE ARGUMENT is
        // what puts T on the reflection-ctor route's named user surface — a bare
        // typeof(LibType) handed straight to Activator is the documented residue
        // and stays closed. The construction itself is the non-generic
        // Activator.CreateInstance(Type) (the generic CreateInstance<T>() lowers
        // inline to `new T()` and would not exercise the late-bound route at all).
        private static object LoadShaped<T>()
        {
            return Activator.CreateInstance(typeof(T));
        }

        private static ConstructorInfo GetCtorShaped<T>(Type[] signature)
        {
            return typeof(T).GetConstructor(signature);
        }

        private static ConstructorInfo[] GetCtorsShaped<T>()
        {
            return typeof(T).GetConstructors();
        }
    }
}
