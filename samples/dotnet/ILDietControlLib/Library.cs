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
