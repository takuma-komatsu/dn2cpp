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

    static class Program
    {
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
