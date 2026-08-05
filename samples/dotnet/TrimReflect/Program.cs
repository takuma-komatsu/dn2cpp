using System;
using System.Reflection;
using TrimReflectLib;
using System.Globalization;

namespace TrimReflect
{
    // The app half of the --trim-reflection bucket. ONE program transpiled three ways, and
    // the three oracles one flag apart are the assertion: no flag (diffed against real
    // .NET, so the throws in the other arms are caused by the FLAG and not by a gap);
    // --trim-reflection (a frozen snapshot where the library types' member reads are
    // PlatformNotSupportedException, so an empty list — the silent wrong answer — reads as
    // `any=False` against a snapshot saying `PNSE`); and one more with --reflection-root,
    // where exactly the rooted types answer again. Nothing here asserts "this must throw":
    // one source runs in all three arms, so the oracles decide. See TrimReflectLib/Lib.cs
    // for why the library types are reached only as `object`.

    // App-module type: the app module is kept whole, so this is unchanged in every arm.
    public class Widget
    {
        public int Count;
        private string _name = "w";
        public string Name => _name;
        public int Add(int a, int b) => a + b;
        public Widget() { }
        public Widget(int c) { Count = c; }
    }

    // An app class over a LIBRARY base: its inherited members assert the keep-set's
    // base-chain closure, since the collectors test the stripped bit at EVERY level.
    public class DerivedWidget : LibBase
    {
        public int Own;
    }

    // An interface has no base chain, so its own table is the only place its members live
    // and the closure has to keep it.
    public class Impl : ILibThing
    {
        public int Twice(int x) => x * 2;
        public string Tag => "impl";
    }

    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            AppModule();
            BaseChain();
            Stripped();
            KeptByToken();
            Roots();
        }

        // Consumes side values so the transpiler cannot fold reaching calls away.
        private static int Sink;

        // 1. Reflection over the app's own types must keep working: identical in all arms.
        private static void AppModule()
        {
            Console.WriteLine("== app module ==");
            Type w = typeof(Widget);
            Console.WriteLine("  fields=" + w.GetFields(BindingFlags.Public | BindingFlags.Instance).Length);
            Console.WriteLine("  getmethod(Add)=" + (w.GetMethod("Add") != null));
            Console.WriteLine("  getproperty(Name)=" + (w.GetProperty("Name") != null));
            Console.WriteLine("  ctors=" + w.GetConstructors().Length);
            object made = Activator.CreateInstance(w);
            Console.WriteLine("  activator=" + made.GetType().Name);
            Console.WriteLine("  invoke(Add,20,22)=" + w.GetMethod("Add").Invoke(made, new object[] { 20, 22 }));
        }

        // 2. The base-chain / interface closure. An INHERITED member read goes to the
        //    library base's method table, so this throws the moment the closure is dropped
        //    — which is how Godot script registration would break silently, since GodotSharp
        //    finds a script's native class name by reflecting off its engine-wrapper base.
        private static void BaseChain()
        {
            Console.WriteLine("== base chain (app class over a library base) ==");
            // Reach Ping through a real call first, so its row is in the untrimmed table.
            Sink += new DerivedWidget().Ping();
            Type d = typeof(DerivedWidget);
            Console.WriteLine("  base=" + d.BaseType.Name);
            MethodInfo ping = d.GetMethod("Ping");
            Console.WriteLine("  inherited method(Ping)=" + (ping != null));
            Console.WriteLine("  inherited field(Inherited)=" + (d.GetField("Inherited") != null));
            Console.WriteLine("  own field(Own)=" + (d.GetField("Own") != null));
            // Guarded: the assertion is the base-chain closure, not null handling.
            Console.WriteLine("  invoke(Ping)=" + (ping != null ? ping.Invoke(new DerivedWidget(), null).ToString() : "<null>"));
            Type itf = typeof(Impl).GetInterfaces()[0];
            Console.WriteLine("  interface=" + itf.Name);
            Console.WriteLine("  interface method(Twice)=" + (itf.GetMethod("Twice") != null));
        }

        // 3. A library type met only as `object`: its sole route to a Type is GetType(),
        //    which no static keep-set can see, so the trim strips it.
        private static void Stripped()
        {
            object o = Factory.Make();
            Factory.MakeDefault();  // reaches LibWidget's () ctor — see Lib.cs
            Type s = o.GetType();

            // Through the INTERFACE slot: the dispatch reaches the implementor's body
            // without naming the concrete type, so these members land in LibWidget's
            // reflection table while LibWidget still strips. A direct `((LibWidget)o)`
            // would name it and keep it, defeating the strip.
            ILibThing it = (ILibThing)o;
            Sink += it.Twice(21) + it.Tag.Length;

            Console.WriteLine("== stripped type: everything that is NOT member metadata ==");
            Console.WriteLine("  name=" + s.Name);
            Console.WriteLine("  fullname=" + s.FullName);
            Console.WriteLine("  namespace=" + s.Namespace);
            Console.WriteLine("  base=" + s.BaseType.Name);
            Console.WriteLine("  assembly=" + s.Assembly.GetName().Name);
            Console.WriteLine("  isclass=" + s.IsClass + " isvaluetype=" + s.IsValueType + " isenum=" + s.IsEnum);
            Console.WriteLine("  tostring=" + o.ToString());
            Console.WriteLine("  gettype-identity=" + (o.GetType() == s));
            Console.WriteLine("  isinstanceoftype=" + s.IsInstanceOfType(o));
            Console.WriteLine("  isassignablefrom(object)=" + typeof(object).IsAssignableFrom(s));
            Console.WriteLine("  interfaces[0]=" + s.GetInterfaces()[0].Name);
            // castclass into a stripped class, then a vtable dispatch: the trim touches
            // neither.
            Console.WriteLine("  cast+dispatch((ILibThing)o).Twice(21)=" + it.Twice(21));

            // A stripped ENUM: its members live in their own table, so ToString() keeps
            // answering — and it is read without ever naming LibColor, since an unbox.any
            // would name it and naming it is what KEEPS it.
            object boxed = Factory.MakeColor();
            Type ct = boxed.GetType();
            Console.WriteLine("  boxed enum tostring=" + boxed.ToString());
            Console.WriteLine("  boxed enum type=" + ct.Name + " isenum=" + ct.IsEnum);
            Console.WriteLine("  boxed enum underlying=" + Enum.GetUnderlyingType(ct).Name);
            Console.WriteLine("  boxed enum getnames=" + string.Join(",", Enum.GetNames(ct)));

            Console.WriteLine("== stripped type: member metadata must FAIL LOUDLY ==");
            // `any=` rather than a count: a library type's table holds only the REACHED
            // members, so no exact count is a number real .NET could be asked to agree with
            // — but "did it answer at all" is, and `any=False` is precisely the silent wrong
            // answer the trimmed arm's `PNSE` replaces.
            Probe("GetMethods", () => "any=" + (s.GetMethods().Length > 0));
            Probe("GetFields", () => "any=" + (s.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Length > 0));
            Probe("GetProperties", () => "any=" + (s.GetProperties().Length > 0));
            Probe("GetMembers", () => "any=" + (s.GetMembers().Length > 0));
            Probe("GetMethod(Twice)", () => "found=" + (s.GetMethod("Twice") != null));
            Probe("GetField(Amount)", () => "found=" + (s.GetField("Amount") != null));
            Probe("GetProperty(Tag)", () => "found=" + (s.GetProperty("Tag") != null));
            Probe("GetMember(Twice)", () => "any=" + (s.GetMember("Twice").Length > 0));

            Console.WriteLine("== stripped type: constructors are NOT stripped ==");
            // Deliberately outside the rule: Type.GetConstructor over a type chosen at RUN
            // time is a live cross-assembly path no static keep-set can see, so these must
            // answer in every arm.
            Probe("GetConstructors", () => "count=" + s.GetConstructors().Length);
            Probe("GetConstructor(int)", () => "found=" + (s.GetConstructor(new Type[] { typeof(int) }) != null));
            Probe("Activator.CreateInstance", () => "made=" + Activator.CreateInstance(s));
            Probe("Activator.CreateInstance(42)", () => "made=" + Activator.CreateInstance(s, new object[] { 42 }));
        }

        // 4. The other side of the keep rule: a LIBRARY type the app names with a type token
        //    keeps its metadata, in every arm and with no root needed.
        private static void KeptByToken()
        {
            Console.WriteLine("== library type named by a type token: kept ==");
            Sink += new LibKept().Echo(1);  // reach Echo for the untrimmed arm
            Type k = typeof(LibKept);
            Console.WriteLine("  getmethod(Echo)=" + (k.GetMethod("Echo") != null));
            Console.WriteLine("  getfield(Keeper)=" + (k.GetField("Keeper") != null));
        }

        // 5. The --reflection-root escape hatch from both ends: LibGadget is never rooted
        //    (so it throws under the trim in every arm), while LibWidget is rooted by exact
        //    full name and LibBox by its arity-stripped definition name, in one arm only.
        private static void Roots()
        {
            // Neither implements an interface, so no method of theirs can be reached without
            // a typed call that would NAME and thus KEEP them. Hence enumerations and fields
            // only — a method probed by name would read False untrimmed and muddy the diff.

            Console.WriteLine("== never rooted (LibGadget) ==");
            object g = Factory.MakeGadget();
            Factory.MakeGadgetDefault();
            Type gt = g.GetType();
            Console.WriteLine("  tostring=" + g.ToString());
            // NOT covered by --reflection-root LibWidget, so these throw in both the trimmed
            // and the rooted arm: a root keeps EXACTLY the type it names and no more.
            Probe("GetMethods", () => "any=" + (gt.GetMethods().Length > 0));
            Probe("GetField(Size)", () => "found=" + (gt.GetField("Size") != null));
            // Constructors are never stripped, so this answers in every arm.
            Probe("GetConstructors", () => "count=" + gt.GetConstructors().Length);

            Console.WriteLine("== rooted by generic definition name (LibBox) ==");
            object b = Factory.MakeBox();
            Factory.MakeBoxDefault();
            Type bt = b.GetType();
            // No Name/FullName here: dn2cpp and real .NET spell a closed generic's name
            // differently (real .NET bakes in each argument's assembly-qualified name), and
            // the untrimmed arm is diffed against real .NET.
            Console.WriteLine("  tostring=" + b.ToString());
            // Rooted by the arity-stripped "TrimReflectLib.LibBox", so these answer in the
            // rooted arm and throw under the plain trim.
            Probe("GetMethods", () => "any=" + (bt.GetMethods().Length > 0));
            Probe("GetField(Value)", () => "found=" + (bt.GetField("Value") != null));
        }

        // Prints what a member-metadata read answers, or the exception it throws. The full
        // message goes in: it is the diagnostic a shipped game's author gets, so the
        // snapshot asserts it names the offending type and both remedies.
        private static void Probe(string what, Func<string> f)
        {
            try
            {
                Console.WriteLine("  " + what + " -> " + f());
            }
            catch (PlatformNotSupportedException ex)
            {
                Console.WriteLine("  " + what + " -> PNSE: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("  " + what + " -> " + ex.GetType().Name + ": " + ex.Message);
            }
        }
    }
}
