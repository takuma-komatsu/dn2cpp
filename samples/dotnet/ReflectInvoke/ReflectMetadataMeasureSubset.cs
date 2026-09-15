using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReflectMetadataMeasureSubset;

[AttributeUsage(AttributeTargets.Class)]
sealed class ValueAttribute : Attribute
{
    public readonly int Value;
    public ValueAttribute(int value) => Value = value;
}

[Value(17)]
sealed class Subject
{
    public int Value;
    public int Read(int value) => value + Value;
}

[Value(17)]
sealed class Packed
{
    public int Value;
    public int Read(int value) => value + Value;
}

abstract class DispatchBase
{
    public abstract int Read(int value);
}

sealed class DispatchDerived : DispatchBase
{
    public override int Read(int value) => value + 1;
}

static class Program
{
    static readonly Subject Instance = new Subject();
    static readonly Type SubjectType = typeof(Subject);
    static readonly Packed PackedInstance = new Packed();
    static readonly Type PackedType = PackedInstance.GetType();
    static readonly DispatchBase Dispatch = new DispatchDerived();
    static readonly object[] Arguments = new object[] { 7 };
    static object Sink;
    static bool Predicate;
    static int Number;
    static MethodInfo Method;
    static ValueAttribute Attribute;

    static void Measure(string operation, Action action)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        long started = Stopwatch.GetTimestamp();
        action();
        long firstTicks = Stopwatch.GetTimestamp() - started;
        long firstBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        before = GC.GetAllocatedBytesForCurrentThread();
        started = Stopwatch.GetTimestamp();
        for (int i = 0; i < 1000; i++)
            action();
        long repeatedTicks = Stopwatch.GetTimestamp() - started;
        long repeatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Console.WriteLine("reflection-measure," + operation + "," + firstBytes + "," + repeatedBytes + "," + firstTicks + "," + repeatedTicks);
    }

    internal static void Run()
    {
        // Initialize the counters before measuring the first reflection operation.
        GC.GetAllocatedBytesForCurrentThread();
        Stopwatch.GetTimestamp();
        Console.WriteLine("reflection-measure-managed-held-before," + GC.GetTotalMemory(true));
        Console.WriteLine("reflection-measure-frequency," + Stopwatch.Frequency);
        Console.WriteLine("reflection-measure-iterations,1000");
        Console.WriteLine("reflection-measure-columns,operation,first-bytes,repeated-bytes,first-ticks,repeated-ticks");
        Measure("typeof", () => Sink = typeof(Subject));
        Measure("get-type", () => Sink = Instance.GetType());
        Measure("type-test", () => Predicate = SubjectType.IsInstanceOfType(Instance));
        Measure("direct-call", () => Number = Instance.Read(7));
        Measure("virtual-call", () => Number = Dispatch.Read(7));
        Measure("find-field", () => Sink = SubjectType.GetField("Value"));
        Measure("find-method", () => Sink = SubjectType.GetMethod("Read"));
        Measure("find-intrinsic-method", () => Sink = typeof(Unsafe).GetMethod("SizeOf"));
        Method = SubjectType.GetMethod("Read");
        Measure("type-name", () => Sink = SubjectType.FullName);
        Measure("member-name", () => Sink = Method.Name);
        Measure("signature", () => Sink = Method.ToString());
        Measure("enumerate-methods", () => Sink = SubjectType.GetMethods());
        Measure("parameters", () => Sink = Method.GetParameters());
        Measure("attribute-data", () => Sink = SubjectType.GetCustomAttributesData());
        Measure("create-attributes", () => Sink = SubjectType.GetCustomAttributes(typeof(ValueAttribute), false));
        Attribute = (ValueAttribute)SubjectType.GetCustomAttributes(typeof(ValueAttribute), false)[0];
        Measure("attribute-value", () => Number = Attribute.Value);
        Measure("invoke", () => Sink = Method.Invoke(Instance, Arguments));
        Console.WriteLine("reflection-measure-observed," + Predicate + "," + Number + "," + Sink);
        Measure("get-type-packed", () => Sink = PackedInstance.GetType());
        Measure("type-test-packed", () => Predicate = PackedType.IsInstanceOfType(PackedInstance));
        Measure("direct-call-packed", () => Number = PackedInstance.Read(7));
        Measure("find-field-packed", () => Sink = PackedType.GetField("Value"));
        Measure("find-method-packed", () => Sink = PackedType.GetMethod("Read"));
        Method = PackedType.GetMethod("Read");
        Measure("type-name-packed", () => Sink = PackedType.FullName);
        Measure("member-name-packed", () => Sink = Method.Name);
        Measure("signature-packed", () => Sink = Method.ToString());
        Measure("enumerate-methods-packed", () => Sink = PackedType.GetMethods());
        Measure("parameters-packed", () => Sink = Method.GetParameters());
        Measure("attribute-data-packed", () => Sink = PackedType.GetCustomAttributesData());
        Measure("create-attributes-packed", () => Sink = PackedType.GetCustomAttributes(typeof(ValueAttribute), false));
        Attribute = (ValueAttribute)PackedType.GetCustomAttributes(typeof(ValueAttribute), false)[0];
        Measure("attribute-value-packed", () => Number = Attribute.Value);
        Measure("invoke-packed", () => Sink = Method.Invoke(PackedInstance, Arguments));
        Console.WriteLine("reflection-measure-packed-observed," + Predicate + "," + Number + "," + Sink);
        Console.WriteLine("reflection-measure-managed-held-after," + GC.GetTotalMemory(true));
    }
}
