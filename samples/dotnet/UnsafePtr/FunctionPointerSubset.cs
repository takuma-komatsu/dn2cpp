#nullable enable
using System;

// C# function pointers and `calli`: `&Method` loads the raw address and
// `fp(args)` lowers to a calli against the call-site standalone signature, from
// which the transpiler reconstructs the C++ function-pointer type. The final
// case binds a delegate AND a function pointer to the SAME static in one body —
// the delegate's ldftn must keep the target-slot adapter while the function
// pointer's takes the raw address.

namespace FunctionPointerSubset;

delegate int IntOp(int a, int b);

unsafe class Program
{
    static int Add(int a, int b) => a + b;
    static int Mul(int a, int b) => a * b;
    static int Inc(int x) => x + 1;
    static double Half(double x) => x / 2.0;

    static int s_sink;
    static void Bump(int by) => s_sink += by;

    // Crossing a call boundary in each direction: the pointer survives as a plain
    // void* value, so both call sites are genuinely indirect calli.
    static int Apply(delegate*<int, int, int> f, int a, int b) => f(a, b);

    static delegate*<int, int, int> Pick(int op) => op == 0 ? &Add : &Mul;

    internal static void __GateEntry()
    {
        delegate*<int, int, int> fp = &Add;
        Console.WriteLine(fp(20, 22));              // 42

        // Conditional selection, then an indirect invoke.
        bool useMul = true;
        delegate*<int, int, int> op = useMul ? &Mul : &Add;
        Console.WriteLine(op(6, 7));                // 42

        Console.WriteLine(Apply(&Add, 100, 23));   // 123
        Console.WriteLine(Apply(&Mul, 6, 7));      // 42
        Console.WriteLine(Pick(0)(40, 2));         // 42
        Console.WriteLine(Pick(1)(21, 2));         // 42

        // One-arg, double, and void-return shapes.
        delegate*<int, int> inc = &Inc;
        Console.WriteLine(inc(41));                 // 42

        delegate*<double, double> half = &Half;
        Console.WriteLine(half(85.0));             // 42.5

        delegate*<int, void> bump = &Bump;
        bump(40);
        bump(2);
        Console.WriteLine(s_sink);                 // 42

        // One static, two ldftn dispositions in one body.
        IntOp del = Add;
        delegate*<int, int, int> raw = &Add;
        Console.WriteLine(del(19, 23) + raw(0, 0)); // 42
    }
}
