using System;
using System.Globalization;
using System.Reflection;

namespace HotUpdateBase;

public delegate void FinalizerRunner();

public interface IRenamedMeasure
{
    int Measure();
}

public interface IPatchScore
{
    int Score(int value);
}

public interface IPatchVirtualMeasure
{
    int Weigh();
}

public class RenamedMeasure : IRenamedMeasure, IPatchScore, IPatchVirtualMeasure
{
    public override string ToString() => "measure";

    ~RenamedMeasure()
    {
        RenamedSlotProbe.Finalized++;
    }

    public virtual int Weigh() => 31;

    int IRenamedMeasure.Measure() => -1;

    int IPatchScore.Score(int value) => value - 1;
}

public static class RenamedSlotProbe
{
    public static int BeforeBlock;
    public static int MetadataRefusals;
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

    public static void RunPatchIdentity(string label, RenamedMeasure receiver)
    {
#if PATCH_METADATA_REFUSALS
        if (MetadataRefusals != 0)
        {
            RunUnavailablePatchMetadata(receiver);
            return;
        }
#endif
        IRenamedMeasure measure = receiver;
        ObjectHasher group = measure.Measure;
        var reflected = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver,
            typeof(IRenamedMeasure).GetMethod(nameof(IRenamedMeasure.Measure))!);
        var method = group.Method;
        var roundtrip = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver, method);
        Console.WriteLine(label + " identity=" + method.Name + "/" + method.DeclaringType!.Name
            + "/" + method.ReflectedType!.Name + "/" + method.MetadataToken);
        Console.WriteLine(label + " metadata=" + method.IsPublic + "/" + method.IsPrivate
            + "/" + method.IsStatic + "/" + method.IsVirtual + "/" + method.IsFinal
            + "/" + method.IsHideBySig + "/" + method.ReturnType.Name + "/" + method);
        Console.WriteLine(label + " rows=" + method.Equals(group.Method) + "/"
            + method.Equals(reflected.Method) + "/" + method.Equals(method.GetBaseDefinition())
            + "/" + method.Equals(roundtrip.Method));
        Console.WriteLine(label + " invoke=" + method.Invoke(receiver, null) + "/" + roundtrip()
            + "/" + group.Equals(roundtrip) + "/" + reflected.Equals(roundtrip));
        IPatchScore score = receiver;
        IntTransform scoreGroup = score.Score;
        var scoreMethod = scoreGroup.Method;
        var parameter = scoreMethod.GetParameters()[0];
        var scoreRoundtrip = (IntTransform)Delegate.CreateDelegate(typeof(IntTransform), receiver, scoreMethod);
        Console.WriteLine(label + " parameter=" + scoreMethod.Name + "/" + scoreMethod.DeclaringType!.Name
            + "/" + parameter.Name + "/" + parameter.ParameterType.Name + "/" + parameter.IsOptional + "/" + parameter.Position + "/" + parameter.Member.Equals(scoreMethod));
        Console.WriteLine(label + " score=" + scoreMethod.Invoke(receiver, new object[] { 5 })
            + "/" + scoreRoundtrip(5) + "/" + scoreGroup.Equals(scoreRoundtrip));
        try
        {
            method.Invoke(new object(), null);
            Console.WriteLine(label + " wrong receiver accepted");
        }
        catch (TargetException)
        {
            Console.WriteLine(label + " wrong receiver refused");
        }
    }

    public static void RunVirtualPatchIdentity(string label, RenamedMeasure receiver)
    {
        if (MetadataRefusals != 0)
            return;
        IPatchVirtualMeasure measure = receiver;
        ObjectHasher group = measure.Weigh;
        var method = group.Method;
        var reflected = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver,
            typeof(IPatchVirtualMeasure).GetMethod(nameof(IPatchVirtualMeasure.Weigh))!);
        var roundtrip = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver, method);
        var root = method.GetBaseDefinition();
        Console.WriteLine(label + " virtual identity=" + method.Name + "/" + method.DeclaringType!.Name
            + "/" + method.ReflectedType!.Name + "/" + method.MetadataToken);
        Console.WriteLine(label + " virtual base=" + root.Name + "/" + root.DeclaringType!.Name);
        Console.WriteLine(label + " virtual rows=" + method.Equals(reflected.Method) + "/"
            + method.Equals(roundtrip.Method) + "/" + group.Equals(roundtrip) + "/"
            + reflected.Equals(roundtrip));
        Console.WriteLine(label + " virtual invoke=" + method.Invoke(receiver, null) + "/" + roundtrip());
    }

    public static void RunVirtualPatchOverride(RenamedMeasure ancestor, RenamedMeasure receiver)
    {
        if (MetadataRefusals != 0)
            return;
        IPatchVirtualMeasure first = ancestor;
        IPatchVirtualMeasure second = receiver;
        ObjectHasher ancestorGroup = first.Weigh;
        ObjectHasher group = second.Weigh;
        var method = ancestorGroup.Method;
        var roundtrip = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver, method);
        Console.WriteLine("patch virtual override rows=" + group.Method.Equals(roundtrip.Method)
            + "/" + group.Equals(roundtrip) + "/" + roundtrip.Method.DeclaringType!.Name);
        Console.WriteLine("patch virtual override invoke=" + method.Invoke(receiver, null) + "/" + roundtrip());
    }

    public static void RunVirtualSplitIdentity(RenamedMeasure ancestor, RenamedMeasure receiver)
    {
        IPatchVirtualMeasure first = ancestor;
        IPatchVirtualMeasure second = receiver;
        ObjectHasher ancestorGroup = first.Weigh;
        ObjectHasher group = second.Weigh;
        var method = ancestorGroup.Method;
        var rebound = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver, method);
        var reflected = (ObjectHasher)Delegate.CreateDelegate(typeof(ObjectHasher), receiver,
            typeof(IPatchVirtualMeasure).GetMethod(nameof(IPatchVirtualMeasure.Weigh))!);
        Console.WriteLine("split reflective equal=" + reflected.Equals(rebound) + "/" + rebound.Equals(reflected));
        Console.WriteLine("split virtual equal=" + group.Equals(rebound) + "/" + rebound.Equals(group));
        Console.WriteLine("split virtual invoke=" + method.Invoke(receiver, null) + "/" + rebound() + "/" + group());
#if PATCH_METADATA_REFUSALS
        if (MetadataRefusals != 0)
        {
            try
            {
                var selected = rebound.Method;
                Console.WriteLine("patch unrecorded override accepted");
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("patch unrecorded override refused");
            }
        }
#endif
    }

#if PATCH_METADATA_REFUSALS
    public static void RunUnavailablePatchMetadata(RenamedMeasure receiver)
    {
        IRenamedMeasure measure = receiver;
        ObjectHasher group = measure.Measure;
        var method = group.Method;
        try
        {
            method.GetCustomAttributes(typeof(ExternalObjectAttribute), false);
            Console.WriteLine("patch method attributes accepted");
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("patch method attributes refused");
        }
        IPatchScore score = receiver;
        IntTransform scoreGroup = score.Score;
        var parameter = scoreGroup.Method.GetParameters()[0];
        try
        {
            parameter.GetCustomAttributes(typeof(ExternalObjectAttribute), false);
            Console.WriteLine("patch parameter attributes accepted");
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("patch parameter attributes refused");
        }
        try
        {
            parameter.GetRequiredCustomModifiers();
            Console.WriteLine("patch parameter modifiers accepted");
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("patch parameter modifiers refused");
        }
        try
        {
            method.ReturnParameter.GetOptionalCustomModifiers();
            Console.WriteLine("patch return modifiers accepted");
        }
        catch (PlatformNotSupportedException)
        {
            Console.WriteLine("patch return modifiers refused");
        }
    }

#endif

    private static int StaticScore(int value) => value + 3;
    private static int OtherStaticScore(int value) => value + 3;

    public static void RunCrossRouteEquality()
    {
        IntTransform group = StaticScore;
        IntTransform other = OtherStaticScore;
        var reflected = (IntTransform)Delegate.CreateDelegate(typeof(IntTransform), group.Method);
        var distinct = (IntTransform)Delegate.CreateDelegate(typeof(IntTransform), other.Method);
        Console.WriteLine("aot cross-route=" + group.Equals(reflected) + "/" + reflected.Equals(group)
            + "/" + (group.GetHashCode() == reflected.GetHashCode()) + "/" + group.Equals(distinct));
        var combined = (IntTransform)Delegate.Combine(group, reflected);
        var reverse = (IntTransform)Delegate.Combine(reflected, group);
        var removed = (IntTransform?)Delegate.Remove(combined, group);
        Console.WriteLine("aot cross-route lists=" + combined.Equals(reverse)
            + "/" + (combined.GetHashCode() == reverse.GetHashCode())
            + "/" + combined.GetInvocationList().Length + "/" + removed!.Equals(group)
            + "/" + combined(5) + "/" + removed(5));
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
