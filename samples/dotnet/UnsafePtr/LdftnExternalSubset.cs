using System;
using System.Threading;

namespace LdftnExternalSubset
{
    // `ldftn` whose token is NOT a plain MethodDefinition. Three shapes, all of
    // which System.Linq reaches: a MethodSpecification (generic method bound to a
    // delegate), a MemberReference with a TypeSpecification parent (a method on a
    // closed generic type), and a generic method on an INTRINSIC-mapped type
    // (Array.Empty<T>) for which no transpiled body exists, so the emitter
    // synthesizes one per instantiation from the intrinsic's lowering. The first
    // two must resolve like a call site, then route through the existing
    // delegate-adapter / raw-address path.
    //
    // A fourth shape: a member a per-member intercept row CUT (ExecutionContext.Run,
    // whose calls lower to a direct callback invoke). Its declaring type is not
    // intrinsic-mapped, so nothing about the type says the body is missing.

    // The method group's own delegate type: Run's exact signature.
    internal delegate void RunDelegate(ExecutionContext ctx, ContextCallback cb, object state);

    internal sealed class Box<T>
    {
        private readonly T _v;
        public Box(T v) => _v = v;

        // Bound to a delegate this is a MemberReference with a TypeSpec parent.
        public T Get() => _v;
    }

    // Virtual on a closed generic type: `ldvirtftn` must resolve to the vtable slot
    // so dispatch reaches the most-derived override.
    internal abstract class Animal<T>
    {
        public abstract string Speak(T n);
    }

    internal sealed class Dog<T> : Animal<T>
    {
        public override string Speak(T n)
            => "woof:" + n;
    }

    internal static class Program
    {
        // MethodSpecification with a static target — routed through the delegate
        // adapter (dgadap_).
        private static U Identity<U>(U x)
            => x;

        // Bound inside Trivial<T>, so its MethodSpecification resolves in that
        // method's generic context rather than at a top-level site.
        private static bool AlwaysPositive<T>(T _)
            => true;

        private static Func<T, bool> Trivial<T>()
            => AlwaysPositive<T>;

        // A closure inside a generic method: the lambda is an instance method on a
        // generic display class, so its `ldftn` has a TypeSpecification parent.
        private static Func<T, bool> And<T>(Func<T, bool> p1, Func<T, bool> p2)
            => x => p1(x) && p2(x);

        private static int CompareByValue<T>(T a, T b) where T : IComparable<T>
            => a.CompareTo(b);

        private static bool IsPositive(int x)
            => x > 0;

        private static bool IsEven(int x)
            => (x & 1) == 0;

        internal static void __GateEntry()
        {
            // MethodSpec, static target -> delegate adapter path.
            Func<int, int> id = Identity<int>;
            Console.WriteLine(id(7));                 // 7
            Func<string, string> ids = Identity<string>;
            Console.WriteLine(ids("hi"));             // hi

            // MemberRef, instance target -> raw method address.
            Func<int> geti = new Box<int>(42).Get;
            Console.WriteLine(geti());                // 42
            Func<string> gets = new Box<string>("dn2cpp").Get;
            Console.WriteLine(gets());                // dn2cpp

            // MethodSpec nested inside a generic method.
            Func<int, bool> t = Trivial<int>();
            Console.WriteLine(t(-1));                 // True

            // Generic-method closure -> MemberRef.
            Func<int, bool> both = And<int>(IsPositive, IsEven);
            Console.WriteLine(both(4));               // True
            Console.WriteLine(both(3));               // False
            Console.WriteLine(both(-2));              // False

            Comparison<int> cmp = CompareByValue<int>;
            Console.WriteLine(cmp(2, 5));             // -1
            Console.WriteLine(cmp(5, 2));             //  1
            Console.WriteLine(cmp(3, 3));             //  0

            // ldvirtftn, dispatched through the vtable to the derived override.
            Animal<int> ai = new Dog<int>();
            Func<int, string> speak = ai.Speak;
            Console.WriteLine(speak(3));             // woof:3
            Animal<string> asx = new Dog<string>();
            Func<string, string> speaks = asx.Speak;
            Console.WriteLine(speaks("x"));          // woof:x

            // Intrinsic-mapped type: the synthesized body must keep Array.Empty<T>'s
            // per-element-type cached singleton, hence the identity check.
            Func<int[]> emptyInts = Array.Empty<int>;
            Console.WriteLine(emptyInts().Length);    // 0
            Console.WriteLine(ReferenceEquals(emptyInts(), Array.Empty<int>())); // True
            Func<string[]> emptyStrings = Array.Empty<string>;
            Console.WriteLine(emptyStrings().Length); // 0
            FnPtrEmpty();

            // Non-generic intrinsic: Roslyn folds a direct ReferenceEquals call to
            // ceq, so only the method-group form keeps a call to synthesize.
            Func<object, object, bool> refEq = ReferenceEquals;
            object o = new object();
            Console.WriteLine(refEq(o, o));           // True
            Console.WriteLine(refEq(o, new object())); // False
            Console.WriteLine(refEq(null, null));     // True

            // An intercepted CUT member, address-taken: reachability transpiles no
            // body, so the emitter must synthesize one from the row's own lowering.
            // Both sides only ever print from INSIDE the callback — dn2cpp ignores the
            // context argument, and real .NET throws on a null one, so it is Capture()'s.
            RunDelegate run = ExecutionContext.Run;
            run(ExecutionContext.Capture(), static s => Console.WriteLine("ec-run:" + s), "flowed");
            FnPtrRun();

            ClosedStaticDelegateSubset.Program.__GateEntry();
        }

        // ldftn NOT followed by a delegate newobj: the raw synthesized-body address,
        // invoked through calli.
        private static unsafe void FnPtrEmpty()
        {
            delegate*<int[]> f = &Array.Empty<int>;
            Console.WriteLine(f().Length);            // 0
            Console.WriteLine(ReferenceEquals(f(), Array.Empty<int>())); // True
            delegate*<string[]> g = &Array.Empty<string>;
            Console.WriteLine(g().Length);            // 0
        }

        // The intercepted CUT member as a RAW address, invoked through calli: no
        // delegate adapter in between, so the synthesized body is reached on its own.
        private static unsafe void FnPtrRun()
        {
            delegate*<ExecutionContext, ContextCallback, object, void> f = &ExecutionContext.Run;
            f(ExecutionContext.Capture(), static s => Console.WriteLine("ec-fnptr:" + s), "flowed");
        }
    }
}
