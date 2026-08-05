using System;
using System.Collections.Generic;

namespace DefaultInterfaceMethodSubset
{
    // Default interface methods (C# 8). A class that implements the interface
    // without overriding a default must dispatch, through its interface table, to
    // the interface's own default body — the slot cannot be left null (a null slot
    // faults at the call site). Regression cover for the itable wiring of inherited
    // defaults, the shape that broke the full-CLI self-host fixpoint when
    // ConsoleBackend began inheriting IEmitBackend.ExternallyAllocatedClasses's
    // default (the harness gates/selfhost-emit.sh is outside the regression glob).
    internal interface IGreeter
    {
        // Abstract member the defaults build on — dispatched back on `this` from
        // inside a default body (a default calling another interface member).
        string Name();

        // Inherited-by-default: builds on the abstract Name().
        string Greet() => "Hello, " + Name();

        // Default with a parameter and a numeric return.
        int Score(int bonus) => Name().Length + bonus;
    }

    // Inherits BOTH defaults (the exact regressing case).
    internal sealed class Plain : IGreeter
    {
        public string Name() => "Plain";
    }

    // Overrides one default, inherits the other.
    internal sealed class Fancy : IGreeter
    {
        public string Name() => "Fancy";
        public string Greet() => "Greetings from " + Name() + "!";
    }

    // A default whose return type is a collection interface fed by Array.Empty<T>()
    // — the array flows out as IEnumerable<T> and is enumerated by the caller. This
    // is the precise IEmitBackend.ExternallyAllocatedClasses shape.
    internal interface ISource
    {
        IEnumerable<int> Items() => Array.Empty<int>();
    }

    internal sealed class EmptySource : ISource
    {
    }

    internal sealed class FullSource : ISource
    {
        public IEnumerable<int> Items() => new[] { 1, 2, 3 };
    }

    // A delegate bound to an INTERFACE method group (`iface.Method`) lowers to
    // `ldvirtftn IFace::Method`, which — unlike ldvirtftn on a class virtual, which
    // reads a vtable slot — must resolve through the receiver's interface table, the
    // same way a callvirt on an interface does. It used to be unsupported outright
    // ("ldvirtftn of interface methods is not supported yet"), which is what blocked
    // the real GDTask's TaskTracker once its `new StackTrace()` stopped blocking it.
    // Cover all three shapes: an interface-typed receiver, a default interface method
    // reached the same way (the delegate must land on the interface's own body), and a
    // generic constrained to the interface.
    internal interface ICounter
    {
        int Add(int x);
        // A DEFAULT the delegate binds to: the itable slot holds the interface's own
        // body for an implementor that does not override it.
        int Twice(int x) => Add(x) + Add(x);
    }

    internal sealed class Adder : ICounter
    {
        private int _n;
        public int Add(int x) => _n += x;
    }

    internal sealed class Doubler : ICounter
    {
        public int Add(int x) => x * 2;
        public int Twice(int x) => x * 4;   // overrides the default
    }

    // A GENERIC-VIRTUAL default interface method whose by-VALUE overload is a DIM
    // that forwards to a by-REF override the implementor supplies — the exact Thrive
    // ISArchiveWriter/SArchiveWriterBase.WriteObject<T> shape. IWriter.Emit<T>(ref T)
    // is the real member; IWriter.Emit<T>(T) is a default that does
    // `var temp = value; Emit(ref temp);`. ConsoleWriter overrides only the by-ref
    // one, so the by-value generic-virtual SLOT for ConsoleWriter resolves to the
    // by-ref override. The GVM dispatcher hands the struct by value but the override
    // wants a pointer to it; forwarding must take the address of its own by-value
    // parameter (identical to the DIM's `temp`, a discarded copy). With value-type
    // type args this is where the dispatcher used to cast a by-value struct straight
    // to a pointer type, which does not compile (Thrive C++ build, Class B).
    internal interface IEmittable
    {
        string Describe();
    }

    internal interface IWriter
    {
        // By-ref overload — the real member, overridden by the implementor.
        void Emit<T>(ref T value) where T : struct, IEmittable;

        // By-value DIM — makes a copy and forwards to the by-ref overload.
        void Emit<T>(T value) where T : struct, IEmittable
        {
            var temp = value;
            Emit(ref temp);
        }
    }

    internal struct Point : IEmittable
    {
        public int X;
        public int Y;
        public string Describe() => "Point(" + X + "," + Y + ")";
    }

    internal struct Tag : IEmittable
    {
        public string Text;
        public string Describe() => "Tag[" + Text + "]";
    }

    internal sealed class ConsoleWriter : IWriter
    {
        // Implements ONLY the by-ref overload; inherits the by-value default.
        public void Emit<T>(ref T value) where T : struct, IEmittable
            => Console.WriteLine("emit " + value.Describe());
    }

    // A generic-virtual interface method whose RETURN TYPE is a value struct, with no
    // default body (the interface member is abstract). When the GVM dispatcher's base
    // fallback is unreachable — every concrete type overrides the abstract member — the
    // emitter traps and then emits a default return of the struct type. For a value
    // struct that default cannot be a C-style cast from 0 ((t_Val)0 / (uint?)0 have no
    // matching conversion); it must be value-initialized (t_Val{}). This is the exact
    // Thrive shape (a GVM returning Arch.Core.Entity / uint?) that failed the C++ build
    // (Class F). The struct and Nullable<uint> returns cover both faulting forms.
    internal struct Val
    {
        public int A;
        public override string ToString() => "Val(" + A + ")";
    }

    internal interface IMaker
    {
        Val Make<T>(T seed);
        uint? Maybe<T>(T seed);
    }

    internal sealed class MakerA : IMaker
    {
        public Val Make<T>(T seed) => new Val { A = 1 };
        public uint? Maybe<T>(T seed) => 7u;
    }

    internal sealed class MakerB : IMaker
    {
        public Val Make<T>(T seed) => new Val { A = 2 };
        public uint? Maybe<T>(T seed) => 9u;
    }

    internal static class Program
    {
        private static Func<int, int> BindGeneric<T>(T c) where T : ICounter => c.Add;

        internal static void Run()
        {
            IGreeter[] greeters = { new Plain(), new Fancy() };
            foreach (IGreeter g in greeters)
            {
                Console.WriteLine(g.Greet());
                Console.WriteLine(g.Score(10));
            }

            // Direct interface-typed dispatch of the inherited defaults.
            IGreeter p = new Plain();
            Console.WriteLine(p.Greet());
            Console.WriteLine(p.Score(0));

            // Default returning a collection interface, iterated by the caller
            // (the array-as-IEnumerable exit that segfaulted in self-host).
            ISource[] sources = { new EmptySource(), new FullSource() };
            foreach (ISource s in sources)
            {
                int sum = 0;
                int count = 0;
                foreach (int v in s.Items())
                {
                    sum += v;
                    count++;
                }
                Console.WriteLine("count=" + count + " sum=" + sum);
            }

            // ldvirtftn on an interface method: the delegate must bind to the
            // RECEIVER's implementation, not to the declaration.
            ICounter[] counters = { new Adder(), new Doubler() };
            foreach (ICounter c in counters)
            {
                Func<int, int> add = c.Add;         // ldvirtftn ICounter::Add
                Func<int, int> twice = c.Twice;     // ldvirtftn on a DEFAULT member
                Console.WriteLine("add=" + add(5) + " again=" + add(5) + " twice=" + twice(3));
            }

            // Same, through a generic constrained to the interface — and stored, so a
            // bad function pointer cannot hide behind an inlined single use.
            var stored = new List<Func<int, int>>();
            foreach (ICounter c in counters)
                stored.Add(c.Add);
            stored.Add(BindGeneric(new Doubler()));
            foreach (Func<int, int> d in stored)
                Console.WriteLine("stored=" + d(7));

            // Generic-virtual by-value DIM -> by-ref override, on VALUE-TYPE type
            // args (the dispatcher bridges value->ref by taking the address of its
            // own by-value parameter). The interface-typed receiver forces the GVM
            // dispatcher rather than a direct call.
            IWriter w = new ConsoleWriter();
            w.Emit(new Point { X = 1, Y = 2 });   // binds the by-value overload
            w.Emit(new Tag { Text = "hi" });      // a second, differently-laid-out struct
            var pt = new Point { X = 3, Y = 4 };
            w.Emit(ref pt);                        // the by-ref overload directly

            // Generic-virtual interface methods with VALUE-STRUCT returns, dispatched
            // through an interface-typed receiver (forces the GVM dispatcher). Both
            // concrete makers override the abstract members, so the dispatcher's base
            // fallback is a trap + a default struct return — which must value-init the
            // struct, not cast 0 to it.
            IMaker[] makers = { new MakerA(), new MakerB() };
            foreach (IMaker m in makers)
            {
                Console.WriteLine("make=" + m.Make<int>(5) + " maybe=" + m.Maybe<string>("x"));
            }
        }

        internal static void __GateEntry()
        {
            Run();
        }
    }
}
