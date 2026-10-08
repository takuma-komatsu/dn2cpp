using System;
using System.Globalization;
using System.Reflection;

namespace HotUpdateBase;

public delegate void FinalizerRunner();

public interface IRenamedMeasure
{
    int Measure();
}

public class RenamedMeasure : IRenamedMeasure
{
    public override string ToString() => "measure";

    ~RenamedMeasure()
    {
        RenamedSlotProbe.Finalized++;
    }

    public virtual int Weigh() => 31;

    int IRenamedMeasure.Measure() => -1;
}

public static class RenamedSlotProbe
{
    public static int BeforeBlock;
    public static int Finalized;

    public static void PinCulture()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
    }

    public static void Run(string label, RenamedMeasure receiver, bool names)
    {
        IRenamedMeasure itf = receiver;
        ObjectHasher group = itf.Measure;
        ObjectHasher bodyGroup = receiver.Weigh;
        var row = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver,
            typeof(IRenamedMeasure).GetMethod(nameof(IRenamedMeasure.Measure))!);
        var body = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver,
            typeof(RenamedMeasure).GetMethod(nameof(RenamedMeasure.Weigh))!);
        if (names)
            Console.WriteLine(label + " methods=" + group.Method.Name + "/" + bodyGroup.Method.Name
                + "/" + row.Method.Name + "/" + body.Method.Name);
        Console.WriteLine(label + " values=" + group() + "/" + bodyGroup() + "/" + row() + "/" + body());
        Console.WriteLine(label + " equal=" + group.Equals(bodyGroup) + "/" + row.Equals(body)
            + "/" + body.Equals(row) + "/" + (row.GetHashCode() == body.GetHashCode()));
    }

    public static void RunCopiedObject()
    {
        var receiver = new RenamedMeasure();
        var row = (Describer)Delegate.CreateDelegate(typeof(Describer), receiver,
            typeof(object).GetMethod(nameof(ToString))!);
        var body = (Describer)Delegate.CreateDelegate(typeof(Describer), receiver,
            typeof(RenamedMeasure).GetMethod("Show")!);
        Console.WriteLine("renamed Object methods=" + row.Method.Name + "/" + body.Method.Name);
        Console.WriteLine("renamed Object values=" + row() + "/" + body());
        Console.WriteLine("renamed Object equal=" + row.Equals(body) + "/" + body.Equals(row));
    }

    public static void RunCopiedFinalizer()
    {
        var receiver = new RenamedMeasure();
        GC.SuppressFinalize(receiver);
        Finalized = 0;
        var row = (FinalizerRunner)Delegate.CreateDelegate(typeof(FinalizerRunner), receiver,
            typeof(object).GetMethod("Finalize", BindingFlags.Instance | BindingFlags.NonPublic)!);
        var body = (FinalizerRunner)Delegate.CreateDelegate(typeof(FinalizerRunner), receiver,
            typeof(RenamedMeasure).GetMethod("Release", BindingFlags.Instance | BindingFlags.NonPublic)!);
        Console.WriteLine("renamed Finalize methods=" + row.Method.Name + "/" + body.Method.Name);
        Console.WriteLine("renamed Finalize equal=" + row.Equals(body) + "/" + body.Equals(row));
        row();
        Console.WriteLine("renamed Finalize first=" + Finalized);
        body();
        Console.WriteLine("renamed Finalize second=" + Finalized);
    }
}
