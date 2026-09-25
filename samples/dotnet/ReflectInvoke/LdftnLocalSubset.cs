using System;
using System.Runtime.CompilerServices;

namespace LdftnLocalSubset;

class VirtualBase
{
    public virtual int Scale(int value) => value * 2;
}

sealed class VirtualDerived : VirtualBase
{
    public override int Scale(int value) => value * 3;
}

sealed class InstanceHolder
{
    public int Delta;

    public int Offset(int value) => value + Delta;
}

// After Build, gates/fixtures/ldftn-local/Program.cs replaces each throwing stub's
// body with IL that C# cannot express.
static class Program
{
    static int Add(int value) => value + 7;
    static int Subtract(int value) => value - 3;
    static string Decorate(string prefix, string value) => prefix + value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> Stored() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> NopSeparated() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> NativeConvert() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> SnapshotBeforeOverwrite() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> Selected(bool first) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> StackJoin(bool first) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<string, string> ClosedStored() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RawCalli() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> DeadOrigins() => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> VirtualStored(VirtualBase receiver) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> InstanceStored(InstanceHolder holder) => throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Func<int, int> Int64Stored() => throw new InvalidOperationException();

    // Roslyn takes these locals' addresses in a body that also creates a delegate.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static string AddressTakenBesideDelegate()
    {
        IntPtr.TryParse("42", out IntPtr handle);
        long.TryParse("9", out long count);
        Func<int, int> add = Add;
        return handle.ToString() + "/" + count + "/" + add(5) + "/" + add.Method.Name;
    }

    public static void Run()
    {
        Console.WriteLine("ldftn-local-begin");
        var stored = Stored();
        Console.WriteLine("ldftn-local-direct=" + stored(5) + "/" + stored.Method.Name);
        var separated = NopSeparated();
        Console.WriteLine("ldftn-local-nop=" + separated(5) + "/" + separated.Method.Name);
        var converted = NativeConvert();
        Console.WriteLine("ldftn-local-conv=" + converted(5) + "/" + converted.Method.Name);
        var snapshot = SnapshotBeforeOverwrite();
        Console.WriteLine("ldftn-local-snapshot=" + snapshot(5) + "/" + snapshot.Method.Name);
        var first = Selected(true);
        var second = Selected(false);
        Console.WriteLine("ldftn-local-selected=" + first(5) + "/" + first.Method.Name
            + "/" + second(5) + "/" + second.Method.Name);
        var stackFirst = StackJoin(true);
        var stackSecond = StackJoin(false);
        Console.WriteLine("ldftn-local-stack-join=" + stackFirst(5) + "/" + stackFirst.Method.Name
            + "/" + stackSecond(5) + "/" + stackSecond.Method.Name);
        var closed = ClosedStored();
        Console.WriteLine("ldftn-local-closed=" + closed("x") + "/" + closed.Method.Name);
        Console.WriteLine("ldftn-local-calli=" + RawCalli());
        var dead = DeadOrigins();
        Console.WriteLine("ldftn-local-dead-origin=" + dead(5) + "/" + dead.Method.Name);
        var virtualStored = VirtualStored(new VirtualDerived());
        Console.WriteLine("ldftn-local-virtual=" + virtualStored(5) + "/"
            + virtualStored.Method.DeclaringType.Name + "." + virtualStored.Method.Name);
        var instance = InstanceStored(new InstanceHolder { Delta = 10 });
        Console.WriteLine("ldftn-local-instance=" + instance(5) + "/" + instance.Method.Name);
        var wide = Int64Stored();
        Console.WriteLine("ldftn-local-int64=" + wide(5) + "/" + wide.Method.Name);
        Console.WriteLine("ldftn-local-address-taken=" + AddressTakenBesideDelegate());
        Console.WriteLine("ldftn-local-end");
    }
}
