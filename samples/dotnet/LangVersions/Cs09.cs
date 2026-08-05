// C# 9.0. Asserts: records (positional, `with`, value equality, the synthesized
// `ToString` and `Deconstruct`), `init`-only setters on an ordinary class,
// relational (`is > 0`) and logical (`and` / `or` / `not`) patterns, target-typed
// `new()`, covariant return types (a `virtual Base Clone()` overridden as
// `override Derived Clone()`), `nint` / `nuint`, function pointers
// (`delegate*<int,int,int>` bound with `&Method`), a module initializer,
// attributes on local functions, and `static` lambdas.
//
// Two deliberate restrictions:
//
//   * Only STATIC function pointers. An instance function pointer would exercise a
//     dn2cpp carve-out rather than C# 9, and this bucket is about the language.
//   * The module initializer only WRITES A STATIC FIELD; nothing is printed from it.
//     A module initializer runs before `Main`, so printing there would put its line
//     ahead of the C# 1.0 section — which real .NET and the transpiler could still
//     agree on, but only by agreeing on an ordering that is not this bucket's
//     subject. Writing a field and printing it from `__GateEntry` asserts that the
//     initializer RAN without making the assertion depend on when.
//
// Top-level statements — the other headline C# 9 feature — are asserted by the
// bucket's own `Program.cs` driver, which is written as top-level statements.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cs09;

internal static class ModInit
{
    // Written by the module initializer below, read by __GateEntry. Never assigned
    // anywhere else, so printing "ran" proves the initializer executed.
    internal static string Stamp;

    [ModuleInitializer]
    internal static void Init() => Stamp = "ran";
}

internal static class Program
{
    internal static void __GateEntry()
    {
        Console.WriteLine("== C# 9.0 ==");

        // Module initializer: the field was set before Main, from a method nothing calls.
        string stamp = ModInit.Stamp ?? "did-not-run";
        Console.WriteLine($"  module initializer: {stamp}");

        // Records: positional construction, synthesized ToString, value equality, `with`,
        // synthesized Deconstruct.
        var ada = new Person("Ada", "Lovelace", 36);
        var adaAgain = new Person("Ada", "Lovelace", 36);
        var older = ada with { Age = 37 };
        Console.WriteLine($"  record ToString: {ada}");
        Console.WriteLine($"  record with: {older}");
        Console.WriteLine($"  record equality: ada==adaAgain -> {ada == adaAgain}, ada==older -> {ada == older}");
        Console.WriteLine($"  record equality: ReferenceEquals -> {ReferenceEquals(ada, adaAgain)}, Equals -> {ada.Equals(adaAgain)}");
        var (first, last, age) = ada;
        Console.WriteLine($"  record Deconstruct: {first} {last} {age}");

        // `init`-only setters on a plain class: settable in an initializer, not after.
        var config = new Config { Host = "localhost", Port = 8080 };
        var defaulted = new Config { Host = "example" };     // Port keeps its initializer
        Console.WriteLine($"  init setters: {config} / {defaulted}");

        // Relational and logical patterns.
        Console.WriteLine($"  relational/logical patterns: {Classify(-3)} / {Classify(0)} / {Classify(7)} / {Classify(99)} / {Classify(42)}");
        object something = "text";
        Console.WriteLine($"  `not` pattern: is-not-null -> {something is not null}, is-not-int -> {something is not int}");

        // Target-typed `new()`.
        Config targetTyped = new() { Host = "target", Port = 1 };
        List<int> list = new() { 1, 2, 3 };
        string listText = string.Join(",", list);
        Console.WriteLine($"  target-typed new: {targetTyped} list=[{listText}]");

        // Covariant return: the override narrows the return type.
        Animal animal = new Dog();
        Animal clonedViaBase = animal.Clone();               // virtual slot, returns Animal statically
        Dog dog = new Dog();
        Dog clonedViaDerived = dog.Clone();                  // same slot, but typed Dog here
        Console.WriteLine($"  covariant return: {clonedViaBase} / {clonedViaDerived} / {clonedViaDerived.Fetch()}");

        // `nint` / `nuint`. Only small values are printed — printing IntPtr.Size would
        // make the output depend on the host's pointer width.
        nint na = 40;
        nint nb = 2;
        nuint nu = 7;
        Console.WriteLine($"  nint/nuint: na+nb={na + nb} na*nb={na * nb} nu*3={nu * 3} nint-max-of-small={NIntMax(na, nb)}");

        // Function pointers: `&Add` is a managed function pointer, called through `f(a,b)`.
        unsafe
        {
            delegate*<int, int, int> add = &Add;
            delegate*<int, int, int> mul = &Mul;
            Console.WriteLine($"  function pointer: add(3,4)={Apply(add, 3, 4)} mul(3,4)={Apply(mul, 3, 4)}");
        }

        // An attribute on a local function.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Triple(int x) => x * 3;

        Console.WriteLine($"  attributed local function: Triple(14)={Triple(14)}");

        // `static` lambda: cannot capture, so it has no closure to allocate.
        Func<int, int> square = static x => x * x;
        Func<int, int, int> min = static (x, y) => x < y ? x : y;
        Console.WriteLine($"  static lambda: square(7)={square(7)} min(3,9)={min(3, 9)}");
    }

    private static string Classify(int n) => n switch
    {
        < 0 => "negative",
        0 => "zero",
        > 0 and < 10 => "small",
        >= 10 and not 42 => "large",
        _ => "the-answer",
    };

    private static nint NIntMax(nint a, nint b) => a > b ? a : b;

    private static int Add(int a, int b) => a + b;

    private static int Mul(int a, int b) => a * b;

    private static unsafe int Apply(delegate*<int, int, int> f, int a, int b) => f(a, b);
}

internal record Person(string First, string Last, int Age);

internal sealed class Config
{
    public string Host { get; init; }
    public int Port { get; init; } = 80;

    public override string ToString() => $"Config(Host={Host}, Port={Port})";
}

internal class Animal
{
    public virtual Animal Clone() => new Animal();

    public override string ToString() => "Animal";
}

internal sealed class Dog : Animal
{
    public override Dog Clone() => new Dog();               // covariant return

    public string Fetch() => "fetch";

    public override string ToString() => "Dog";
}
