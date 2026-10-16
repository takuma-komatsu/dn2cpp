using System;
using System.Globalization;

namespace InitializerTriggersOnly;

// Every initializer below is precise (an explicit static constructor, so the type is
// not beforefieldinit) and is triggered by one IL shape that names no static field of
// its type: a run-time root, a base-constructor call, a struct constructor or instance
// method called on a local or through a constrained call, a static abstract member's
// implementation called through a constrained call, and a method pointer. A witness is
// read only after its trigger, so it answers the same whether the initializer ran in the
// startup pass or at first use. The generic witnesses also record whether the
// initializer ran after Main set Registered, which only a first-use guard at the trigger
// satisfies.
internal static class Witness
{
    internal static bool Entry;
    internal static bool Base;
    internal static bool Struct;
    internal static bool Method;
    internal static bool Function;
    internal static bool StaticCall;
    internal static bool Untouched;
    internal static bool DefaultStruct;
    internal static bool Registered;
    internal static string? GenericBase;
    internal static string? SharedBase;
    internal static string? GenericStruct;
    internal static string? GenericMethod;
    internal static string? GenericFunction;
    internal static string? SharedFunction;
    internal static string? SharedStaticCall;
    internal static string? Constrained;
    internal static string? StaticVirtual;
    internal static string? ConstrainedText;
    internal static string? ConstrainedEquality;
    internal static string? ConstrainedHash;

    internal static string Stage => Registered ? "registered" : "too-early";
}

internal class Base
{
    static Base() => Witness.Base = true;
}

internal sealed class Derived : Base
{
}

internal struct Built
{
    internal int Value;

    static Built() => Witness.Struct = true;

    internal Built(int value) => Value = value;
}

internal struct MethodOnly
{
    static MethodOnly() => Witness.Method = true;

    internal int Read() => 1;
}

internal struct GenericMethodOnly<T>
{
    static GenericMethodOnly() => Witness.GenericMethod = Witness.Stage;

    internal int Read() => 2;
}

internal static class FunctionOwner
{
    static FunctionOwner() => Witness.Function = true;

    internal static int Answer() => 42;
}

internal static class StaticCallOwner
{
    static StaticCallOwner() => Witness.StaticCall = true;

    internal static int Touch() => 1;
}

internal static class Untouched
{
    static Untouched() => Witness.Untouched = true;
}

internal struct DefaultOnly
{
#pragma warning disable CS0649 // only ever default: initobj and ldfld are not triggers
    internal int Value;
#pragma warning restore CS0649

    static DefaultOnly() => Witness.DefaultStruct = true;
}

internal class GenericBase<T>
{
    static GenericBase() => Witness.GenericBase = Witness.Stage;
}

internal sealed class FromGeneric : GenericBase<int>
{
}

internal class SharedBase<T>
{
    static SharedBase() => Witness.SharedBase = Witness.Stage;
}

internal sealed class SharedDerived<T> : SharedBase<T>
{
}

internal struct GenericBuilt<T>
{
    internal T Value;

    static GenericBuilt() => Witness.GenericStruct = Witness.Stage;

    internal GenericBuilt(T value) => Value = value;
}

internal static class GenericFunction<T>
{
    static GenericFunction() => Witness.GenericFunction = Witness.Stage;

    internal static int Answer() => 7;
}

internal static class SharedFunction<T>
{
    static SharedFunction() => Witness.SharedFunction = Witness.Stage;

    internal static int Answer() => 8;
}

internal static class SharedStaticCall<T>
{
    static SharedStaticCall() => Witness.SharedStaticCall = Witness.Stage;

    internal static int Touch() => 9;
}

internal interface IReadable
{
    int Read();
}

internal struct ConstrainedOnly<T> : IReadable
{
    static ConstrainedOnly() => Witness.Constrained = Witness.Stage;

    public int Read() => 3;
}

internal interface ICreatable
{
    static abstract int Create();
}

internal sealed class StaticVirtualOwner<T> : ICreatable
{
    static StaticVirtualOwner() => Witness.StaticVirtual = Witness.Stage;

    public static int Create() => 4;
}

// Each overrides one System.Object virtual that a constrained call through an
// unconstrained type parameter runs; the other overrides are never called.
internal struct ConstrainedText<T>
{
    static ConstrainedText() => Witness.ConstrainedText = Witness.Stage;

    public override string ToString() => "text";
}

internal struct ConstrainedEquality<T>
{
    static ConstrainedEquality() => Witness.ConstrainedEquality = Witness.Stage;

    public override bool Equals(object? other) => other is null;

    public override int GetHashCode() => 0;
}

internal struct ConstrainedHash<T>
{
    static ConstrainedHash() => Witness.ConstrainedHash = Witness.Stage;

    public override bool Equals(object? other) => false;

    public override int GetHashCode() => 6;
}

internal static class ConstrainedCalls
{
    internal static int Read<U>(U value) where U : IReadable => value.Read();

    internal static int Create<U>() where U : ICreatable => U.Create();

    internal static string? Text<U>(U value) => value!.ToString();

    internal static bool Same<U>(U value, object? other) => value!.Equals(other);

    internal static int Hash<U>(U value) => value!.GetHashCode();
}

internal static class SharedFunctionFactory
{
    internal static Func<int> Of<T>() => SharedFunction<T>.Answer;

    internal static int Call<T>() => SharedStaticCall<T>.Touch();
}

// Main is the only static member: nothing but the entry-point root triggers Program.
internal static class Program
{
    static Program() => Witness.Entry = true;

    private static int Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        Console.WriteLine($"entry initializer before Main={Witness.Entry}");

        _ = new Derived();
        Console.WriteLine($"base constructor call={Witness.Base}");

        Built built = new Built(5);
        Console.WriteLine($"struct constructor call={built.Value}/{Witness.Struct}");

        MethodOnly method = default;
        Console.WriteLine($"struct instance method={method.Read()}/{Witness.Method}");

        Func<int> function = FunctionOwner.Answer;
        Console.WriteLine($"method pointer={function()}/{Witness.Function}");

        Console.WriteLine($"static call={StaticCallOwner.Touch()}/{Witness.StaticCall}");

        Console.WriteLine($"typeof only={typeof(Untouched).Name}/{Witness.Untouched}");
        DefaultOnly empty = default;
        Console.WriteLine($"default struct={empty.Value}/{Witness.DefaultStruct}");

        Witness.Registered = true;
        _ = new FromGeneric();
        Console.WriteLine($"generic base constructor call={Witness.GenericBase ?? "not-run"}");
        _ = new SharedDerived<string>();
        Console.WriteLine($"shared base constructor call={Witness.SharedBase ?? "not-run"}");
        GenericBuilt<int> generic = new GenericBuilt<int>(3);
        Console.WriteLine($"generic struct constructor call={generic.Value}/{Witness.GenericStruct ?? "not-run"}");
        GenericMethodOnly<int> genericMethod = default;
        Console.WriteLine($"generic struct instance method={genericMethod.Read()}/{Witness.GenericMethod ?? "not-run"}");
        Func<int> genericFunction = GenericFunction<int>.Answer;
        Console.WriteLine($"generic method pointer={genericFunction()}/{Witness.GenericFunction ?? "not-run"}");
        Func<int> sharedFunction = SharedFunctionFactory.Of<string>();
        Console.WriteLine($"shared method pointer={sharedFunction()}/{Witness.SharedFunction ?? "not-run"}");
        Console.WriteLine($"shared static call={SharedFunctionFactory.Call<string>()}/{Witness.SharedStaticCall ?? "not-run"}");
        ConstrainedOnly<int> constrained = default;
        Console.WriteLine($"constrained struct method={ConstrainedCalls.Read(constrained)}/{Witness.Constrained ?? "not-run"}");
        Console.WriteLine($"constrained static method={ConstrainedCalls.Create<StaticVirtualOwner<int>>()}/{Witness.StaticVirtual ?? "not-run"}");
        Console.WriteLine($"constrained struct ToString={ConstrainedCalls.Text(new ConstrainedText<int>())}/{Witness.ConstrainedText ?? "not-run"}");
        Console.WriteLine($"constrained struct Equals={ConstrainedCalls.Same(new ConstrainedEquality<int>(), null)}/{Witness.ConstrainedEquality ?? "not-run"}");
        Console.WriteLine($"constrained struct GetHashCode={ConstrainedCalls.Hash(new ConstrainedHash<int>())}/{Witness.ConstrainedHash ?? "not-run"}");

        Console.WriteLine("initializer triggers end");
        return 0;
    }
}
