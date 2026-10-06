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

    // The only thing that names LibShade.
    public class Palette
    {
        public LibShade Shade;
    }

    public class AppTemplateRead<T> : ILibKind, ILibTemplateGvm
    {
        public string Kind() => "application:" + typeof(T).Name;
        public virtual string GenericKind<U>() => typeof(T).Name + "/" + typeof(U).Name;
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            AppModule();
            BaseChain();
            Stripped();
            KeptByToken();
            Roots();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_DELEGATE_METHOD") == "1")
                return;
            DelegateMethod();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_MEMBER_ENUM") == "1")
                return;
            MemberNamedEnum();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_OBJECT_VIRTUAL") == "1")
                return;
            ObjectVirtualMethod();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_UNRECORDED_RECEIVER") == "1")
                return;
            UnrecordedReceiverMethod();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_REFLECTED_UNRECORDED_RECEIVER") == "1")
                return;
            ReflectedUnrecordedReceiverMethod();
            if (args.Length != 0 && args[0] == "before-runtime-template-members")
                return;
            RuntimeTemplateMembers();
            if (args.Length != 0 && args[0] == "before-property-accessors")
                return;
            PropertyAccessors();
            if (args.Length != 0 && args[0] == "before-delegate-name-bindings")
                return;
            NamedDelegateBindings();
        }

        private static void NamedDelegateBindings()
        {
            Console.WriteLine("== delegate method names under trim ==");
            var app = (Func<int, int, int>)Delegate.CreateDelegate(typeof(Func<int, int, int>), new Widget(), "Add");
            Console.WriteLine("  app name=" + app(20, 22));
            var inherited = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), new DerivedWidget(), "Ping");
            Console.WriteLine("  inherited name=" + inherited());
            object library = Factory.Make();
            Probe("library name", () => ((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), library, "Twice"))(21).ToString());
            Probe("library missing soft name", () => Delegate.CreateDelegate(typeof(Func<int>), library, "Missing", false, false) is null ? "null" : "bound");
            Probe("library static soft name", () => Delegate.CreateDelegate(typeof(Func<int, int>), library.GetType(), "Twice", false, false) is null ? "null" : "bound");
            Console.WriteLine("delegate method names under trim end");
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

        // 6. Delegate.Method over stripped receivers. The declaring type is kept for the
        //    read, and a stripped receiver level is passed only where its vtable proves it
        //    inherits the slot; a level that overrides it answers PNSE naming that level.
        private static void DelegateMethod()
        {
            Console.WriteLine("== Delegate.Method over stripped receivers ==");
            Func<string> square = Factory.MakeSquare().Kind;
            Func<string> circle = Factory.MakeCircle().Kind;
            Func<string> disc = Factory.MakeDisc().Kind;
            Func<string> generic = Factory.MakeGenericShape().Kind<int>;
            ILibThing thing = (ILibThing)Factory.Make();
            Func<int, int> twice = thing.Twice;
            Probe("inherited slot", () => square.Method.DeclaringType.Name + "/" + square());
            Probe("overriding level", () => circle.Method.DeclaringType.Name + "/" + circle());
            Probe("inherited override", () => disc.Method.DeclaringType.Name + "/" + disc());
            Probe("generic virtual", () => generic.Method.DeclaringType.Name + "/" + generic());
            Probe("interface slot", () => twice.Method.DeclaringType.Name + "/" + twice(4));
            Func<string> unusedDefault = Factory.MakeUnusedDefault().Kind;
            Func<string> chosenDefault = Factory.MakeChosenDefault().Kind;
            Probe("unrelated stripped interface", () => unusedDefault.Method.DeclaringType.Name + "/" + unusedDefault());
            Probe("selected stripped interface", () => chosenDefault.Method.DeclaringType.Name + "/" + chosenDefault());
        }

        // 7. A library enum only a kept type's field names keeps its field rows in every
        //    arm: they are the enum's whole member surface.
        private static void MemberNamedEnum()
        {
            Console.WriteLine("== enum named by a reflected field ==");
            Type shade = new Palette().GetType().GetField("Shade").FieldType;
            Probe("GetFields", () => shade.Name + " fields=" + shade.GetFields().Length);
        }

        // 8. Delegate.Method of an Object virtual bound through a receiver met only as
        //    `object`. A stripped level the answer needs throws PNSE naming that level; a
        //    stripped level whose dispatch field equals its base's inherits the body and
        //    is passed.
        private static void ObjectVirtualMethod()
        {
            Console.WriteLine("== Delegate.Method of Object virtuals over stripped receivers ==");
            object label = Factory.MakeLabel();
            object plain = Factory.MakePlainLabel();
            object bare = Factory.MakeBare();
            Func<string> labelText = label.ToString;
            Func<string> plainText = plain.ToString;
            Func<string> bareText = bare.ToString;
            Func<int> labelHash = label.GetHashCode;
            var reflected = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), plain,
                typeof(object).GetMethod("ToString", Type.EmptyTypes));
            Probe("object overriding level", () => Describe(labelText.Method) + "/" + labelText());
            Probe("object inherited override", () => Describe(plainText.Method) + "/" + plainText());
            Probe("object inherited body", () => Describe(bareText.Method) + "/" + bareText());
            Probe("object hash override", () => Describe(labelHash.Method) + "/" + labelHash());
            Probe("object reflected binding", () => Describe(reflected.Method) + "/" + reflected());
        }

        private static string Describe(MethodInfo m) =>
            m is null ? "null" : m.DeclaringType.Name + "." + m.Name;

        // Runtime template receivers retain the typeof-named definition's members;
        // an unrecorded library receiver still checks each declaring level's metadata.
        private static void UnrecordedReceiverMethod()
        {
            Console.WriteLine("== Delegate.Method over receivers without a recorded case ==");
            var made = (ILibKind)Activator.CreateInstance(typeof(LibGenericKind<>).MakeGenericType(typeof(Widget)));
            Func<string> madeKind = made.Kind;
            Func<string> inherited = Factory.MakePlainGenericShape().Kind<int>;
            Probe("instantiation interface binding", () => madeKind.Method.DeclaringType.Name + "/" + madeKind());
            Probe("unrecorded generic virtual", () => inherited.Method.DeclaringType.Name + "/" + inherited());
        }

        // 10. A reflection-bound generic virtual over a library subclass that inherits the
        //     row's body. Its dispatcher records no case for the subclass, which runs the
        //     row's own body, so Delegate.Method names the row in every arm.
        private static void ReflectedUnrecordedReceiverMethod()
        {
            Console.WriteLine("== reflection-bound Delegate.Method over a receiver without a recorded case ==");
            MethodInfo kind = typeof(LibGvmShape).GetMethod("Kind").MakeGenericMethod(typeof(int));
            Probe("reflected unrecorded generic virtual", () =>
            {
                var bound = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>),
                    Factory.MakePlainGenericShape(), kind);
                return bound.Method.DeclaringType.Name + "/" + bound();
            });
        }

        private static string AccessorNames(MethodInfo[] methods)
        {
            string names = "";
            foreach (MethodInfo method in methods)
                names += (names.Length == 0 ? "" : ",") + method.Name;
            return names;
        }

        private static void PropertyAccessors()
        {
            Console.WriteLine("== property accessors under trim ==");
            PropertyInfo app = typeof(Widget).GetProperty("Name");
            Console.WriteLine("  app accessor=" + AccessorNames(app.GetAccessors())
                + "/same=" + ReferenceEquals(app.GetAccessors(true)[0], app.GetGetMethod()));
            Type library = Factory.Make().GetType();
            Probe("library accessors default", () => AccessorNames(library.GetProperty("Tag").GetAccessors()));
            Probe("library accessors false", () => AccessorNames(library.GetProperty("Tag").GetAccessors(false)));
            Probe("library accessors true", () => AccessorNames(library.GetProperty("Tag").GetAccessors(true)));
            Console.WriteLine("property accessors under trim end");
        }

        private static void RuntimeTemplateMembers()
        {
            Console.WriteLine("== typeof-kept runtime template members ==");
            foreach (Type argument in new[] { typeof(Widget), typeof(int) })
            {
                TemplateMembers("application " + argument.Name, typeof(AppTemplateRead<>), argument);
                TemplateMembers("library " + argument.Name, typeof(LibTemplateRead<>), argument);
            }
            Console.WriteLine("typeof-kept runtime template members end");
        }

        private static void TemplateMembers(string label, Type definition, Type argument)
        {
            Type type = definition.MakeGenericType(argument);
            var instance = (ILibKind)Activator.CreateInstance(type);
            Func<string> bound = instance.Kind;
            Console.WriteLine("  " + label + " direct=" + bound());
            Probe(label + " GetMethod/Invoke", () =>
            {
                MethodInfo method = type.GetMethod("Kind");
                return method.DeclaringType.Name + "/" + method.Invoke(instance, null)
                    + "/same=" + ReferenceEquals(method, type.GetMethod("Kind"));
            });
            Probe(label + " Delegate.Method", () => bound.Method.DeclaringType.Name + "/" + bound());
            Func<string> generic = ((ILibTemplateGvm)instance).GenericKind<int>;
            Console.WriteLine("  " + label + " generic direct=" + generic());
            Probe(label + " generic Delegate.Method", () => generic.Method.DeclaringType.Name + "/" + generic());
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
