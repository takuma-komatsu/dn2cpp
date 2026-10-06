#nullable enable
using System;
using System.Reflection;

namespace ReflectDelegateSubset
{
    // SUBJECT: the reflection -> delegate bridge — MethodInfo.CreateDelegate and
    // the Delegate.CreateDelegate statics across the four binding modes
    // (open/closed x static/instance), variance, value-type receivers and
    // arguments, bind failures, identity and multicast. The created delegates
    // dispatch through a boxed-invoker trampoline, which is invisible here.
    class Greeter
    {
        private readonly string prefix;
        public Greeter(string prefix) => this.prefix = prefix;
        public string Greet(string name) => prefix + name;
        public int Mul(int a, int b) => a * b;
        public static int AddOne(int x) => x + 1;
        public static string Shout(string s, int n) => s + n;
        public void Note(object o) => Console.WriteLine($"note => {prefix}{o}");
        public object Fetch(string s) => prefix + s;
    }

    struct Counter
    {
        public int Value;
        public int Bump() => Value + 1;
    }

    class NameBase
    {
        public NameBase() { }
        private NameBase(int value) { }
        private string Secret(string value) => "private:" + value;
        private static int StaticSecret(int value) => value + 5;
        public virtual string Describe(string value) => "base:" + value;
        public string Overload(string value) => "base-overload:" + value;
        public virtual string SlotCase(string value) => "base-slot:" + value;
    }

    unsafe class NameTarget : NameBase
    {
        static NameTarget() { }
        public override string Describe(string value) => "derived:" + value;
        public string Overload(object value) => "object:" + value;
        public int Overload(int value) => value + 10;
        public string Écho(string value) => "unicode:" + value;
        public string Σend(string value) => "greek:" + value;
        public string Short(string value) => "long-s:" + value;
        public string Choice(string value) => "first:" + value;
        public string CHOICE(string value) => "last:" + value;
        public string SLOTCASE(string value) => "derived-nonvirtual:" + value;
        public override string SlotCase(string value) => "derived-slot:" + value;
        public static int Twice(int value) => value * 2;
        public static int TWICE(int value) => value * 3;
        public static object WiderReturn(string value) => value;
        public static string NarrowerReturn(string value) => value;
        public static void WiderArgument(object value) { }
        public static int RefValue(ref int value) => ++value;
        public static int OutValue(out int value) => value = 42;
        public static int Value(int value) => value + 1;
        public static T Generic<T>(T value) => value;
        public static int* Pointer(int* value) => value;
        public static uint* UnsignedPointer(int* value) => (uint*)value;
        public static ref int RefReturn(ref int value) => ref value;
        public static delegate*<int> FunctionReturn(int value) => (delegate*<int>)(nint)value;
        public static NameEnum EnumIdentity(NameEnum value) => value;
        public static int ObjectValue(object value) => 42;
    }

    class NameCovariantBase
    {
        public virtual object Kind() => "base";
    }

    class NameCovariantChild : NameCovariantBase
    {
        public virtual string KIND() => "case-before";
        public override string Kind() => "covariant";
    }

    class NameCovariantGrandchild : NameCovariantChild
    {
        public override string Kind() => "grandchild";
    }

    class NameVirtualChoices
    {
        public virtual string Pick() => "first";
        public virtual string PICK() => "last";
    }

    class NameGeneric<T>
    {
        public T Identity(T value) => value;
        private static T StaticIdentity(T value) => value;
    }

    class NameTemplate<T>
    {
        public Type Kind() => typeof(T);
        private static Type StaticKind() => typeof(T);
    }

    interface NameStaticVirtual
    {
        static virtual int Value() => 42;
        static abstract int AbstractValue();
    }

    delegate int NameRef(ref int value);
    delegate int NameOut(out int value);
    unsafe delegate int* NamePointer(int* value);
    delegate ref int NameRefReturn(ref int value);
    delegate T NameOpen<T>(T value);
    delegate int NameRefObject(ref object value);
    unsafe delegate delegate*<int> NameFunctionReturn(int value);
    unsafe delegate delegate*<long> NameOtherFunctionReturn(int value);
    unsafe delegate delegate*<int> NameWrongFunctionArgument(string value);
    enum NameEnum { Value }

    static class Program
    {
        private static void NameOutcome(string label, Func<Delegate?> bind)
        {
            try
            {
                Console.WriteLine($"name {label} => {(bind() is null ? "null" : "bound")}");
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine($"name {label} => {exception.GetType().Name}/{exception.ParamName ?? "-"}");
            }
        }

        internal static unsafe void RunNamedBindings()
        {
            Console.WriteLine("== delegate method name bindings ==");
            NameTarget target = new NameTarget();
            Type unary = typeof(Func<string, string>);
            var instance = (Func<string, string>)Delegate.CreateDelegate(unary, target, "Describe");
            var instanceCase = (Func<string, string>)Delegate.CreateDelegate(unary, target, "describe", true);
            var instanceSoft = (Func<string, string>)Delegate.CreateDelegate(unary, target, "describe", true, false)!;
            Console.WriteLine($"name instance siblings => {instance("a")}|{instanceCase("b")}|{instanceSoft("c")}");
            var stat = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "Twice");
            var statCase = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "tWiCe", true);
            var statSoft = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "Twice", false, false)!;
            Console.WriteLine($"name static siblings => {stat(7)}|{statCase(7)}|{statSoft(7)}");
            Console.WriteLine($"name identity => {ReferenceEquals(instance.Target, target)}/{instance.Method.Name}/{instance == instanceCase}");
            Console.WriteLine($"name static identity => {stat.Target is null}/{stat.Method.Name}");
            var getType = (Func<Type>)Delegate.CreateDelegate(typeof(Func<Type>), target, "gEtTyPe", true);
            var toString = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), target, "ToString");
            Console.WriteLine($"name Object methods => {getType() == typeof(NameTarget)}/{toString()}");
            var referenceEquals = (Func<object, object, bool>)Delegate.CreateDelegate(typeof(Func<object, object, bool>), typeof(NameTarget), "ReferenceEquals");
            Console.WriteLine($"name Object inherited static => {referenceEquals(target, target)}/{referenceEquals(target, new NameTarget())}");
            Console.WriteLine($"name private base => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "Secret"))("s")}");
            Console.WriteLine($"name private static base => {((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "StaticSecret"))(7)}");
            Console.WriteLine($"name inherited overload => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "Overload"))("o")}");
            Console.WriteLine($"name value overload => {((Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), target, "Overload"))(7)}");
            Console.WriteLine($"name case candidate => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "choice", true))("c")}");
            Console.WriteLine($"name nonvirtual before slot => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "slotcase", true))("v")}");
            Console.WriteLine($"name virtual case candidate => {((Func<string>)Delegate.CreateDelegate(typeof(Func<string>), new NameVirtualChoices(), "pick", true))()}");
            foreach (object receiver in new object[] { new NameCovariantChild(), new NameCovariantGrandchild() })
            {
                var objectReturn = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), receiver, "Kind");
                var stringReturn = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, "Kind");
                var caseReturn = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), receiver, "kind", true);
                Console.WriteLine($"name covariant slots => {objectReturn()}/{stringReturn()}/{caseReturn()}");
            }
            Console.WriteLine($"name unicode => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "éCHO", true))("u")}");
            Console.WriteLine($"name greek => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "σEND", true))("g")}");
            Console.WriteLine($"name invariant casing => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "ſHORT", true))("f")}");
            Console.WriteLine($"name nul suffix => {((Func<string, string>)Delegate.CreateDelegate(unary, target, "Describe\0ignored"))("n")}");
            var generic = (Func<string, string>)Delegate.CreateDelegate(unary, new NameGeneric<string>(), "Identity");
            var genericStatic = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameGeneric<int>), "StaticIdentity");
            Console.WriteLine($"name closed generic owner => {generic("closed")}/{genericStatic(42)}");
            Type template = typeof(NameTemplate<>).MakeGenericType(typeof(Guid));
            var templateInstance = (Func<Type>)Delegate.CreateDelegate(typeof(Func<Type>), Activator.CreateInstance(template)!, "Kind");
            var templateStatic = (Func<Type>)Delegate.CreateDelegate(typeof(Func<Type>), template, "StaticKind");
            Console.WriteLine($"name template owner => {templateInstance().Name}/{templateStatic().Name}/{templateInstance.Method.DeclaringType == template}");
            object boxed = new Counter { Value = 41 };
            Console.WriteLine($"name boxed receiver => {((Func<int>)Delegate.CreateDelegate(typeof(Func<int>), boxed, "Bump"))()}");
            int value = 6;
            var byRef = (NameRef)Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "RefValue");
            int result = byRef(ref value);
            Console.WriteLine($"name ref argument => {result}/{value}");
            var byOut = (NameOut)Delegate.CreateDelegate(typeof(NameOut), typeof(NameTarget), "OutValue");
            result = byOut(out value);
            Console.WriteLine($"name out argument => {result}/{value}");
            var refToOut = (NameRef)Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "OutValue");
            result = refToOut(ref value);
            Console.WriteLine($"name ref out signature => {result}/{value}");
            var pointer = (NamePointer)Delegate.CreateDelegate(typeof(NamePointer), typeof(NameTarget), "Pointer");
            int pointed = 42;
            Console.WriteLine($"name pointer call => {*pointer(&pointed)}");
            var refReturn = (NameRefReturn)Delegate.CreateDelegate(typeof(NameRefReturn), typeof(NameTarget), "RefReturn");
            ref int alias = ref refReturn(ref value);
            alias = 43;
            Console.WriteLine($"name ref return alias => {value}");

            NameOutcome("missing hard", () => Delegate.CreateDelegate(unary, target, "Missing"));
            NameOutcome("missing soft instance", () => Delegate.CreateDelegate(unary, target, "Missing", false, false));
            NameOutcome("missing soft static", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "Missing", false, false));
            NameOutcome("case sensitive instance", () => Delegate.CreateDelegate(unary, target, "describe", false, false));
            NameOutcome("case sensitive static", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "twice", false, false));
            NameOutcome("static via instance", () => Delegate.CreateDelegate(typeof(Func<int, int>), target, "Twice", false, false));
            NameOutcome("instance via type", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "Describe", false, false));
            NameOutcome("open instance", () => Delegate.CreateDelegate(typeof(Func<NameTarget, string, string>), typeof(NameTarget), "Describe", false, false));
            NameOutcome("closed static", () => Delegate.CreateDelegate(typeof(Func<string>), target, "NarrowerReturn", false, false));
            NameOutcome("contravariance", () => Delegate.CreateDelegate(typeof(Action<string>), typeof(NameTarget), "WiderArgument", false, false));
            NameOutcome("covariance", () => Delegate.CreateDelegate(typeof(Func<string, object>), typeof(NameTarget), "NarrowerReturn", false, false));
            NameOutcome("return mismatch", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "WiderReturn", false, false));
            NameOutcome("ref value mismatch", () => Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "Value", false, false));
            NameOutcome("value ref mismatch", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "RefValue", false, false));
            NameOutcome("ref object mismatch", () => Delegate.CreateDelegate(typeof(NameRefObject), typeof(NameTarget), "ObjectValue", false, false));
            NameOutcome("generic method", () => Delegate.CreateDelegate(unary, typeof(NameTarget), "Generic", false, false));
            NameOutcome("pointer signature", () => Delegate.CreateDelegate(typeof(NamePointer), typeof(NameTarget), "Pointer", false, false));
            NameOutcome("pointer return mismatch", () => Delegate.CreateDelegate(typeof(NamePointer), typeof(NameTarget), "UnsignedPointer", false, false));
            NameOutcome("byref return", () => Delegate.CreateDelegate(typeof(NameRefReturn), typeof(NameTarget), "RefReturn", false, false));
            NameOutcome("byref return mismatch", () => Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget), "RefReturn", false, false));
            NameOutcome("static virtual", () => Delegate.CreateDelegate(typeof(Func<int>), typeof(NameStaticVirtual), "Value", false, false));
            NameOutcome("static abstract", () => Delegate.CreateDelegate(typeof(Func<int>), typeof(NameStaticVirtual), "AbstractValue", false, false));

            NameOutcome("null type first", () => Delegate.CreateDelegate(null!, (object)null!, null!, false, false));
            NameOutcome("null object target", () => Delegate.CreateDelegate(typeof(int), (object)null!, null!, false, false));
            NameOutcome("null type target", () => Delegate.CreateDelegate(typeof(int), (Type)null!, null!, false, false));
            NameOutcome("null instance method", () => Delegate.CreateDelegate(typeof(int), target, null!, false, false));
            NameOutcome("null static method", () => Delegate.CreateDelegate(typeof(int), typeof(NameTarget), null!, false, false));
            NameOutcome("non delegate instance", () => Delegate.CreateDelegate(typeof(int), target, "Missing", false, false));
            NameOutcome("non delegate static", () => Delegate.CreateDelegate(typeof(int), typeof(NameTarget), "Missing", false, false));
            NameOutcome("delegate shell", () => Delegate.CreateDelegate(typeof(Delegate), typeof(NameTarget), "Missing", false, false));
            NameOutcome("multicast shell instance", () => Delegate.CreateDelegate(typeof(MulticastDelegate), target, "Missing", false, false));
            NameOutcome("multicast shell static", () => Delegate.CreateDelegate(typeof(MulticastDelegate), typeof(NameTarget), "Missing", false, false));
            NameOutcome("multicast null target", () => Delegate.CreateDelegate(typeof(MulticastDelegate), (object)null!, null!, false, false));
            NameOutcome("open target first", () => Delegate.CreateDelegate(typeof(int), typeof(NameGeneric<>), "Missing", false, false));
            NameOutcome("open target null method", () => Delegate.CreateDelegate(typeof(int), typeof(NameGeneric<>), null!, false, false));
            NameOutcome("open delegate", () => Delegate.CreateDelegate(typeof(Func<>), typeof(NameTarget), "Missing", false, false));
            NameOutcome("open user delegate", () => Delegate.CreateDelegate(typeof(NameOpen<>), typeof(NameTarget), "Missing", false, false));
            NameOutcome("open delegate method info", () => Delegate.CreateDelegate(typeof(Func<>), typeof(NameTarget).GetMethod("Twice")!, false));
            Console.WriteLine("delegate method name bindings end");
        }

        private static void BoundaryOutcome(string label, Func<Delegate?> bind)
        {
            try
            {
                Console.WriteLine($"name boundary {label} => {(bind() is null ? "null" : "bound")}");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine($"name boundary {label} => unsupported");
            }
        }

        internal static void RunNamedBoundaries()
        {
            Console.WriteLine("== delegate name signature boundaries ==");
            BoundaryOutcome("function return", () => Delegate.CreateDelegate(typeof(NameFunctionReturn), typeof(NameTarget), "FunctionReturn", false, false));
            BoundaryOutcome("function return mismatch", () => Delegate.CreateDelegate(typeof(NameOtherFunctionReturn), typeof(NameTarget), "FunctionReturn", false, false));
            BoundaryOutcome("unrelated function name", () => Delegate.CreateDelegate(typeof(NameFunctionReturn), typeof(NameTarget), "Missing", false, false));
            BoundaryOutcome("function argument mismatch", () => Delegate.CreateDelegate(typeof(NameWrongFunctionArgument), typeof(NameTarget), "FunctionReturn", false, false));
            BoundaryOutcome("MethodInfo function return mismatch", () => Delegate.CreateDelegate(typeof(NameOtherFunctionReturn), typeof(NameTarget).GetMethod("FunctionReturn")!, false));
            BoundaryOutcome("enum underlying", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget), "EnumIdentity", false, false));
            BoundaryOutcome("MethodInfo enum underlying", () => Delegate.CreateDelegate(typeof(Func<int, int>), typeof(NameTarget).GetMethod("EnumIdentity")!, false));
            BoundaryOutcome("MethodInfo ref value mismatch", () => Delegate.CreateDelegate(typeof(NameRef), typeof(NameTarget).GetMethod("Value")!, false));
            BoundaryOutcome("MethodInfo ref object mismatch", () => Delegate.CreateDelegate(typeof(NameRefObject), typeof(NameTarget).GetMethod("ObjectValue")!, false));
            BoundaryOutcome("own constructor", () => Delegate.CreateDelegate(typeof(Action), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("base constructor", () => Delegate.CreateDelegate(typeof(Action<int>), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("constructor query normalization", () => Delegate.CreateDelegate(typeof(Action), new NameTarget(), ".CTOR\0ignored", true, false));
            BoundaryOutcome("constructor return mismatch", () => Delegate.CreateDelegate(typeof(Func<int>), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("constructor argument mismatch", () => Delegate.CreateDelegate(typeof(Action<string>), new NameTarget(), ".ctor", false, false));
            BoundaryOutcome("static initializer", () => Delegate.CreateDelegate(typeof(Action), typeof(NameTarget), ".cctor", false, false));
            BoundaryOutcome("initializer query normalization", () => Delegate.CreateDelegate(typeof(Action), typeof(NameTarget), ".CCTOR\0ignored", true, false));
            BoundaryOutcome("initializer argument mismatch", () => Delegate.CreateDelegate(typeof(Action<string>), typeof(NameTarget), ".cctor", false, false));
            Console.WriteLine("delegate name signature boundaries end");
        }

        internal static void Run()
        {
            Greeter g = new Greeter("hi:");
            MethodInfo miGreet = typeof(Greeter).GetMethod("Greet")!;
            MethodInfo miMul = typeof(Greeter).GetMethod("Mul")!;
            MethodInfo miAdd = typeof(Greeter).GetMethod("AddOne")!;

            // Closed instance: CreateDelegate(Type, target).
            var closed = (Func<string, string>)miGreet.CreateDelegate(typeof(Func<string, string>), g);
            Console.WriteLine($"dg-closed => {closed("bob")}");

            // Generic closed instance: CreateDelegate<T>(target).
            var closedT = miGreet.CreateDelegate<Func<string, string>>(g);
            Console.WriteLine($"dg-closed-generic => {closedT("gen")}");

            // Open static: CreateDelegate(Type) / CreateDelegate<T>().
            var addOne = (Func<int, int>)miAdd.CreateDelegate(typeof(Func<int, int>));
            Console.WriteLine($"dg-open-static => {addOne(41)}");
            var addOneT = miAdd.CreateDelegate<Func<int, int>>();
            Console.WriteLine($"dg-open-static-generic => {addOneT(9)}");

            // Open instance: the delegate's first argument is the receiver
            // (Delegate.CreateDelegate with a null firstArgument).
            var open = (Func<Greeter, string, string>)Delegate.CreateDelegate(
                typeof(Func<Greeter, string, string>), null, miGreet)!;
            Console.WriteLine($"dg-open-instance => {open(g, "eve")}");

            // Closed static: the explicit firstArgument becomes the method's
            // first (reference-typed) parameter; a null firstArgument binds too.
            MethodInfo miShout = typeof(Greeter).GetMethod("Shout")!;
            var closedStatic = (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), "yo", miShout)!;
            Console.WriteLine($"dg-closed-static => {closedStatic(3)}");
            var closedStaticNull = (Func<int, string>)Delegate.CreateDelegate(typeof(Func<int, string>), null, miShout)!;
            Console.WriteLine($"dg-closed-static-null => {closedStaticNull(4)}");
            // A value-typed first parameter cannot be first-arg-bound (.NET
            // stores the bound object unconverted).
            try
            {
                Delegate.CreateDelegate(typeof(Func<int>), 41, miAdd);
                Console.WriteLine("dg-closed-static-value => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-closed-static-value => ArgumentException");
            }

            // Delegate.CreateDelegate(Type, MethodInfo): the plain open form.
            var openStatic2 = (Func<int, int>)Delegate.CreateDelegate(typeof(Func<int, int>), miAdd);
            Console.WriteLine($"dg-static-form => {openStatic2(10)}");

            // Value-type arguments box per call through the trampoline.
            var mul = (Func<int, int, int>)miMul.CreateDelegate(typeof(Func<int, int, int>), g);
            Console.WriteLine($"dg-value-args => {mul(6, 7)}");

            // Value-type receiver: closed over a boxed struct.
            object boxed = new Counter { Value = 5 };
            var bump = (Func<int>)typeof(Counter).GetMethod("Bump")!.CreateDelegate(typeof(Func<int>), boxed);
            Console.WriteLine($"dg-struct-receiver => {bump()}");

            // Argument contravariance (delegate string -> method object) and
            // return covariance (method object -> delegate object over string).
            var contra = (Action<string>)typeof(Greeter).GetMethod("Note")!
                .CreateDelegate(typeof(Action<string>), g);
            contra("c");
            Console.WriteLine("dg-contravariant => ok");
            var co = (Func<string, object>)typeof(Greeter).GetMethod("Fetch")!
                .CreateDelegate(typeof(Func<string, object>), g);
            Console.WriteLine($"dg-covariant => {co("v")}");

            // Identity: Target unwraps to the bound receiver, Method reports the
            // bound MethodInfo, and two equal bindings compare equal.
            Console.WriteLine($"dg-target => {ReferenceEquals(closed.Target, g)}");
            Console.WriteLine($"dg-method => {closed.Method.Name}");
            var closed2 = (Func<string, string>)miGreet.CreateDelegate(typeof(Func<string, string>), g);
            Console.WriteLine($"dg-equal => {closed == closed2}");
            Console.WriteLine($"dg-not-equal => {closed == closedT && (Delegate)closed == (Delegate)addOne}");

            // Multicast: reflection-bound delegates chain like any other, and
            // HasSingleTarget sees the chain length.
            var multi = closed + closed2;
            Console.WriteLine($"dg-multicast => {multi("m")}");
            Console.WriteLine($"dg-single-target => {closed.HasSingleTarget}|{multi.HasSingleTarget}");

            // Bind failures: signature mismatch -> ArgumentException; the
            // no-target form over an instance method -> ArgumentException;
            // a static method with full arity + target -> ArgumentException;
            // throwOnBindFailure: false -> null.
            try
            {
                miGreet.CreateDelegate(typeof(Func<int, int>), g);
                Console.WriteLine("dg-mismatch => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-mismatch => ArgumentException");
            }
            try
            {
                miGreet.CreateDelegate(typeof(Func<string, string>));
                Console.WriteLine("dg-instance-open => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-instance-open => ArgumentException");
            }
            try
            {
                miAdd.CreateDelegate(typeof(Func<int, int>), g);
                Console.WriteLine("dg-static-target => no exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("dg-static-target => ArgumentException");
            }
            Delegate? soft = Delegate.CreateDelegate(typeof(Func<int, int>), miGreet, false);
            Console.WriteLine($"dg-soft-fail => {(soft is null ? "null" : "bound")}");
        }
    }
}
