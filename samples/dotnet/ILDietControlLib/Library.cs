using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dn2Cpp.Runtime;

namespace ILDietControlLib;

public interface IFoo
{
    int Foo();
}

public class Base
{
    public int Foo() => 21;
    public int UnusedPublic() => -1;
    private int UnusedPrivate() => -2;
}

public sealed class Derived : Base, IFoo { }

public interface IGeneric<T>
{
    T Identity(T value);
}

public sealed class ExplicitGeneric<T> : IGeneric<T>
{
    T IGeneric<T>.Identity(T value) => value;
}

public interface IStatic<TSelf> where TSelf : IStatic<TSelf>
{
    static abstract int Evaluate(int value);
}

public readonly struct StaticValue : IStatic<StaticValue>
{
    public static int Evaluate(int value) => value * 7;
}

public static class Initialization
{
    public static int Value;

    [ModuleInitializer]
    public static void Initialize() => Value = 13;
}

public static class StaticInitialization
{
    private static readonly int s_value;
    static StaticInitialization() => s_value = 17;
    public static int Read() => s_value;
}

[StructLayout(LayoutKind.Sequential)]
public struct Layout
{
    public int Used;
    private long _unused;
    public byte Tail;
}

public static class Callbacks
{
    [UnmanagedCallersOnly(EntryPoint = "ildiet_control_callback")]
    public static int NativeCallback(int value) => CallbackLeaf(value);
    private static int CallbackLeaf(int value) => value + 1;

    [NativeImplementation("ildiet-control", "ildiet_control_implementation")]
    public static int NativeEntry(int value) => NativeLeaf(value);
    private static int NativeLeaf(int value) => value + 2;

    public static int ManagedDelegate(int value) => value + 3;
}

public sealed class UnusedType
{
    public static int UnusedMethod() => -3;
}


public sealed class ReflectedPayload : PayloadBase
{
    public ReflectedPayload() => Label = "library-payload";

    public ReflectedPayload(int value) => Label = "library-payload:" + value;

    public string Label { get; private set; }
}


public class PayloadBase
{
    protected PayloadBase() { }

    public PayloadBase(int value) => ConstructorOnlyDependency.Touch();
}

internal static class ConstructorOnlyDependency
{
    public static void Touch() => Console.WriteLine("unused-base-constructor");
}


public static class AccessorOnlyDependency
{
    public static string Read() => "unused-accessor";
}

public struct ReflectionMoney
{
    public override string ToString() => "0 USD";

    public static string UnusedBody() => UnusedBoxDependency.Read();
}

internal static class UnusedBoxDependency
{
    public static string Read() => "unused-box-dependency";
}

public struct ConstructionMoney
{
    public override string ToString() => "0 CHF";

    public static int UnusedBody() => ConstructionBodyOnlyDependency.Read();
}

public static class ConstructionBodyOnlyDependency
{
    public static int Read() => -1;
}

public struct MemberConstructionMoney
{
    public override string ToString() => "0 HKD";

    public static int UnusedBody() => MemberConstructionBodyOnlyDependency.Read();
}

public struct MemberParameterMoney
{
    public override string ToString() => "0 CNY";

    public static int UnusedBody() => MemberConstructionBodyOnlyDependency.Read();
}

public static class MemberConstructionBodyOnlyDependency
{
    public static int Read() => -1;
}

public struct BaseConstructionMoney
{
    public override string ToString() => "base-library-return";

    public static int UnusedBody() => BaseConstructionBodyOnlyDependency.Read();
}

public struct BaseParameterMoney
{
    public override string ToString() => "base-library-parameter";

    public static int UnusedBody() => BaseConstructionBodyOnlyDependency.Read();
}

public static class BaseConstructionBodyOnlyDependency
{
    public static int Read() => -1;
}

public struct InvokeSignatureMoney
{
    public override string ToString() => "invoke-library-money";

    public static int UnusedBody() => InvokeBodyOnlyDependency.Read();
}

public static class InvokeBodyOnlyDependency
{
    public static int Read() => -1;
}

public static class InvokeConstructorOnlyDependency
{
    public static int Read() => -1;
}

public struct ConstructorFieldMoney
{
    public override string ToString() => "ctor-field-owner-money";

    public static int UnusedBody() => ConstructorFieldBodyOnlyDependency.Read();
}

public static class ConstructorFieldBodyOnlyDependency
{
    public static int Read() => -1;
}

public enum ArrayBindingFlags
{
    Default,
}

public interface IAnnotatedContract
{
    public static int Identifier() => 31;
}

public interface IAnnotatedChild : IAnnotatedContract { }

public class AnnotatedBase
{
    private readonly int _seed;

    protected AnnotatedBase() { }
    private AnnotatedBase(int seed) => _seed = seed;
    private static int Hidden() => 13;

    public class Nested
    {
        private static string Read() => "nested";
    }
}

public sealed class AnnotatedMembers : AnnotatedBase, IAnnotatedContract
{
    private int _value;

    public AnnotatedMembers() => _value = 1;
    public AnnotatedMembers(int value) => _value = UnrequestedOverload(value);
    private static int UnrequestedOverload(int value) => value;

    public int Value { get => _value; private set => _value = value; }

    private event Action Changed
    {
        add => _value += 7;
        remove => _value -= 3;
    }
}

public sealed class AnnotatedMethodOwner : AnnotatedBase
{
    public static int Visible() => 17;
}

public struct AnnotatedValue
{
    private static readonly string Label;
    static AnnotatedValue() => Label = "initialized value";
    public override string ToString() => Label;
}
