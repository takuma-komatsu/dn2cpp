using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ILDietControlLib;

namespace ILDietConstructorOnly;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("constructor-only prefix");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_SIGNATURE_CONSTRUCTION") == "1")
            return;
        Console.WriteLine("== signature construction ==");
        Type owner = typeof(SignatureOwner);
        Construct("parameter", owner.GetMethod("ParameterOnly")!.GetParameters()[0].ParameterType);
        Construct("return", owner.GetMethod("ReturnOnly")!.ReturnType);
        Console.WriteLine("accessor-metadata=" + (typeof(AccessorOwner).GetProperty("Label")!.GetMethod is not null));
        Console.WriteLine("signature construction end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_CONSTRUCTION_PAYLOAD") == "1")
            return;
        Console.WriteLine("== generic construction dispatch ==");
        object value = Activator.CreateInstance(typeof(Ledger).GetMethod("Post")!.GetParameters()[0].ParameterType)!;
        Console.WriteLine(value.ToString());
        Console.WriteLine("generic construction dispatch end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_MEMBER_CONSTRUCTION_SIGNATURE") == "1")
            return;
        Console.WriteLine("== closed member construction signatures ==");
        Type memberOwner = typeof(MemberConstructionOwner<int>);
        Console.WriteLine(Activator.CreateInstance(memberOwner.GetMethod("Return")!.ReturnType)!.ToString());
        Console.WriteLine(Activator.CreateInstance(memberOwner.GetMethod("Parameter")!.GetParameters()[0].ParameterType)!.ToString());
        Console.WriteLine("closed member construction signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_BASE_CONSTRUCTION_SIGNATURE") == "1")
            return;
        Console.WriteLine("== closed base construction signatures ==");
        Type baseOwner = typeof(BaseConstructionDerived<int>).BaseType!;
        Console.WriteLine(Activator.CreateInstance(baseOwner.GetMethod("Return")!.ReturnType)!.ToString());
        Console.WriteLine(Activator.CreateInstance(baseOwner.GetMethod("Parameter")!.GetParameters()[0].ParameterType)!.ToString());
        Console.WriteLine("closed base construction signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_CTOR_FIELD_OWNER_SIGNATURE") == "1")
            return;
        Console.WriteLine("== constructor field owner signatures ==");
        Type fieldOwner = typeof(ConstructorFieldOuter<int>).GetField("Next")!.FieldType;
        Console.WriteLine(Activator.CreateInstance(fieldOwner.GetMethod("Make")!.ReturnType)!.ToString());
        Console.WriteLine("constructor field owner signatures end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_CAPTURED_ARRAY_DEFAULTS") == "1")
            return;
        Console.WriteLine("== captured array defaults ==");
        Console.WriteLine("conditional=" + CapturedConditionalFlags(false));
        Console.WriteLine("unwritten=" + CapturedUnwrittenFlags());
        Console.WriteLine("copied=" + CapturedCopiedFlags(false));
        Console.WriteLine("published=" + CapturedPublishedFlags(false));
        Console.WriteLine("initialized=" + CapturedInitializedFlags());
        Console.WriteLine("captured array defaults end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_INTERFACE_ARRAY_DEFAULTS") == "1")
            return;
        Console.WriteLine("== interface array defaults ==");
        Console.WriteLine("local=" + LocalInterfaceArguments());
        Console.WriteLine("captured=" + CapturedInterfaceArguments());
        Console.WriteLine("interface array defaults end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_EXTERNAL_ARRAY_DEFAULTS") == "1")
            return;
        Console.WriteLine("== external array defaults ==");
        Console.WriteLine("external-local=" + ExternalLocalFlags());
        Console.WriteLine("external-captured=" + ExternalCapturedFlags());
        Console.WriteLine("external-conditional=" + ExternalConditionalFlags(false));
        Console.WriteLine("external-copied=" + ExternalCopiedFlags(false));
        Console.WriteLine("external-initialized=" + ExternalInitializedFlags());
        Console.WriteLine("external-untyped=" + ExternalUntypedFlags());
        Console.WriteLine("external array defaults end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_REPEATED_ARRAYS") == "1")
            return;
        Console.WriteLine("== repeated array allocations ==");
        ReportRepeated("repeated-local", RepeatedLocalFlags);
        ReportRepeated("repeated-published", RepeatedPublishedFlags);
        ReportRepeated("repeated-copy", RepeatedCopiedFlags);
        ReportRepeated("repeated-type", RepeatedArrayType);
        ReportRepeated("acyclic-initialized", AcyclicInitializedFlags);
        Console.WriteLine("repeated array allocations end");
    }

    private static void ReportRepeated(string label, Func<string> run)
    {
        try
        {
            Console.WriteLine(label + "=" + run());
        }
        catch (Exception e)
        {
            Console.WriteLine(label + "=" + e.GetType().Name);
        }
    }

    private static string RepeatedLocalFlags()
    {
        BindingFlags[]? previous = null, current = null;
        int iteration = 0;
        while (true)
        {
            previous = current;
            current = new BindingFlags[1];
            if (++iteration == 2)
            {
                current[0] = BindingFlags.Instance | BindingFlags.NonPublic;
                break;
            }
        }
        return ((RepeatedLocalTarget)Activator.CreateInstance(typeof(RepeatedLocalTarget),
            previous![0], null, new object[0], null)!).Made;
    }

    private static string RepeatedPublishedFlags()
    {
        BindingFlags[]? previous = null, current = null;
        int iteration = 0;
        while (true)
        {
            previous = current;
            current = new BindingFlags[1];
            if (++iteration == 2)
            {
                current[0] = BindingFlags.Instance | BindingFlags.NonPublic;
                break;
            }
        }
        BindingFlags[] captured = previous!;
        Func<object> create = () => Activator.CreateInstance(typeof(RepeatedPublishedTarget),
            captured[0], null, new object[0], null)!;
        return ((RepeatedPublishedTarget)create()).Made;
    }

    private static string RepeatedCopiedFlags()
    {
        var source = new[] { BindingFlags.Instance | BindingFlags.Public };
        BindingFlags[]? previous = null, current = null;
        int iteration = 0;
        while (true)
        {
            previous = current;
            current = source.ToArray();
            if (++iteration == 2)
            {
                current[0] = BindingFlags.Instance | BindingFlags.NonPublic;
                break;
            }
        }
        return ((RepeatedCopiedTarget)Activator.CreateInstance(typeof(RepeatedCopiedTarget),
            previous![0], null, new object[0], null)!).Made;
    }

    private static string RepeatedArrayType()
    {
        RepeatedIdentityTarget[]? previous = null, current = null;
        int iteration = 0;
        while (true)
        {
            previous = current;
            current = new RepeatedIdentityTarget[1];
            if (++iteration == 2)
                break;
        }
        return ((RepeatedIdentityTarget)Activator.CreateInstance(previous!.GetType().GetElementType()!)!).Made;
    }

    private static string AcyclicInitializedFlags()
    {
        var flags = new[] { BindingFlags.Instance | BindingFlags.NonPublic };
        return ((AcyclicInitializedTarget)Activator.CreateInstance(typeof(AcyclicInitializedTarget),
            flags[0], null, new object[] { "exact" }, null)!).Made;
    }

    private static string ExternalLocalFlags()
    {
        var flags = new ArrayBindingFlags[1];
        return ((ExternalLocalTarget)Activator.CreateInstance(typeof(ExternalLocalTarget),
            (BindingFlags)flags[0], null, null, null)!).Made;
    }

    private static string ExternalCapturedFlags()
    {
        var flags = new ArrayBindingFlags[1];
        Func<object> create = () => Activator.CreateInstance(typeof(ExternalCapturedTarget),
            (BindingFlags)flags[0], null, null, null)!;
        return ((ExternalCapturedTarget)create()).Made;
    }

    private static string ExternalConditionalFlags(bool write)
    {
        var flags = new ArrayBindingFlags[1];
        if (write)
            flags[0] = (ArrayBindingFlags)(BindingFlags.Instance | BindingFlags.NonPublic);
        Func<object> create = () => Activator.CreateInstance(typeof(ExternalConditionalTarget),
            (BindingFlags)flags[0], null, null, null)!;
        return ((ExternalConditionalTarget)create()).Made;
    }

    private static string ExternalCopiedFlags(bool write)
    {
        var flags = new ArrayBindingFlags[1];
        Action replace = () => flags[0] = (ArrayBindingFlags)(BindingFlags.Instance | BindingFlags.NonPublic);
        if (write)
            replace();
        ArrayBindingFlags copied = flags[0];
        Func<object> create = () => Activator.CreateInstance(typeof(ExternalCopiedTarget),
            (BindingFlags)copied, null, null, null)!;
        return ((ExternalCopiedTarget)create()).Made;
    }

    private static string ExternalInitializedFlags()
    {
        var flags = new[] { (ArrayBindingFlags)(BindingFlags.Instance | BindingFlags.NonPublic) };
        Func<object> create = () => Activator.CreateInstance(typeof(ExternalInitializedTarget),
            (BindingFlags)flags[0], null, new object[] { "exact" }, null)!;
        return ((ExternalInitializedTarget)create()).Made;
    }

    private static string ExternalUntypedFlags()
    {
        var flags = new ArrayBindingFlags[1];
        return ((ExternalUntypedTarget)Activator.CreateInstance(typeof(ExternalUntypedTarget),
            (BindingFlags)flags.First(), null, null, null)!).Made;
    }

    private static string LocalInterfaceArguments()
    {
        var arguments = new ISignatureValue[1];
        return ((InterfaceArrayTarget)Activator.CreateInstance(typeof(InterfaceArrayTarget), arguments)!).Made;
    }

    private static string CapturedInterfaceArguments()
    {
        var arguments = new ISignatureValue[1];
        Func<object> create = () => Activator.CreateInstance(typeof(InterfaceArrayTarget), arguments)!;
        return ((InterfaceArrayTarget)create()).Made;
    }

    private static string CapturedConditionalFlags(bool write)
    {
        var flags = new BindingFlags[1];
        if (write)
            flags[0] = BindingFlags.Instance | BindingFlags.NonPublic;
        Func<object> create = () => Activator.CreateInstance(typeof(CapturedConditionalTarget), flags[0], null, null, null)!;
        return ((CapturedConditionalTarget)create()).Made;
    }

    private static string CapturedUnwrittenFlags()
    {
        var flags = new BindingFlags[1];
        Func<object> create = () => Activator.CreateInstance(typeof(CapturedUnwrittenTarget), flags[0], null, null, null)!;
        return ((CapturedUnwrittenTarget)create()).Made;
    }

    private static string CapturedCopiedFlags(bool write)
    {
        var flags = new BindingFlags[1];
        Action replace = () => flags[0] = BindingFlags.Instance | BindingFlags.NonPublic;
        if (write)
            replace();
        BindingFlags copied = flags[0];
        Func<object> create = () => Activator.CreateInstance(typeof(CapturedCopiedTarget), copied, null, null, null)!;
        return ((CapturedCopiedTarget)create()).Made;
    }

    private static string CapturedPublishedFlags(bool write)
    {
        var local = new BindingFlags[1];
        if (write)
            local[0] = BindingFlags.Instance | BindingFlags.NonPublic;
        BindingFlags[] captured = local;
        Func<object> create = () => Activator.CreateInstance(typeof(CapturedPublishedTarget), captured[0], null, null, null)!;
        return ((CapturedPublishedTarget)create()).Made;
    }

    private static string CapturedInitializedFlags()
    {
        var flags = new[] { BindingFlags.Instance | BindingFlags.NonPublic };
        Func<object> create = () => Activator.CreateInstance(typeof(CapturedInitializedTarget), flags[0], null,
            new object[] { "exact" }, null)!;
        return ((CapturedInitializedTarget)create()).Made;
    }

    private static void Construct(string label, Type selected)
    {
        try
        {
            object value = Activator.CreateInstance(selected)!;
            Console.WriteLine(label + "=" + selected.GetGenericArguments()[0].Name + ":"
                + ((ISignatureValue)value).Read());
        }
        catch (Exception e)
        {
            Console.WriteLine(label + "=" + e.GetType().Name);
        }
    }
}

internal sealed class SignatureOwner
{
    public static void ParameterOnly(SignatureTarget<int> value) { }

    public static SignatureTarget<string>? ReturnOnly() => null;
}

internal interface ISignatureValue
{
    int Read();
}

internal sealed class InterfaceArrayTarget
{
    public string Made;

    public InterfaceArrayTarget(ISignatureValue? value) => Made = value is null ? "null" : "value";
}

internal sealed class SignatureTarget<T> : ISignatureValue
{
    public int Value;

    public SignatureTarget() => Value = 7;

    public int Read() => Value;
}

internal sealed class AccessorOwner
{
    public string Label => ILDietControlLib.AccessorOnlyDependency.Read();
}

internal sealed class Ledger
{
    public static void Post(Slot<ILDietControlLib.ConstructionMoney> value) => ILDietControlLib.ConstructionBodyOnlyDependency.Read();
}

internal sealed class Slot<T> where T : struct
{
    public override string ToString() => default(T).ToString()!;
}

internal sealed class MemberConstructionOwner<T>
{
    public static MemberConstructionPair<T, ILDietControlLib.MemberConstructionMoney>? Return()
    {
        ILDietControlLib.MemberConstructionBodyOnlyDependency.Read();
        return null;
    }

    public static void Parameter(MemberConstructionPair<T, ILDietControlLib.MemberParameterMoney> value)
        => ILDietControlLib.MemberConstructionBodyOnlyDependency.Read();
}

internal sealed class MemberConstructionPair<A, B> where B : struct
{
    public override string ToString() => default(B).ToString()!;
}

internal sealed class BaseConstructionDerived<T> : BaseConstructionOwner<T> { }

internal class BaseConstructionOwner<T>
{
    public static MemberConstructionPair<T, ILDietControlLib.BaseConstructionMoney>? Return()
    {
        ILDietControlLib.BaseConstructionBodyOnlyDependency.Read();
        return null;
    }

    public static void Parameter(MemberConstructionPair<T, ILDietControlLib.BaseParameterMoney> value)
        => ILDietControlLib.BaseConstructionBodyOnlyDependency.Read();
}

#pragma warning disable CS0649, CS8618
internal sealed class ConstructorFieldOuter<T>
{
    public static ConstructorFieldInner<T> Next;
}
#pragma warning restore CS0649, CS8618

internal sealed class ConstructorFieldInner<T>
{
    public static ConstructorFieldSlot<T, ILDietControlLib.ConstructorFieldMoney>? Make()
    {
        ILDietControlLib.ConstructorFieldBodyOnlyDependency.Read();
        return null;
    }
}

internal sealed class ConstructorFieldSlot<A, B> where B : struct
{
    public override string ToString() => default(B).ToString()!;
}

internal sealed class CapturedConditionalTarget
{
    public string Made;
    public CapturedConditionalTarget() => Made = "default";
}

internal sealed class CapturedUnwrittenTarget
{
    public string Made;
    public CapturedUnwrittenTarget() => Made = "default";
}

internal sealed class CapturedCopiedTarget
{
    public string Made;
    public CapturedCopiedTarget() => Made = "default";
}

internal sealed class CapturedPublishedTarget
{
    public string Made;
    public CapturedPublishedTarget() => Made = "default";
}

internal sealed class CapturedInitializedTarget
{
    public string Made;
    public CapturedInitializedTarget(int unused) => Made = UnselectedCapturedConstructor();
    private CapturedInitializedTarget(string made) => Made = made;

    private static string UnselectedCapturedConstructor() => "unselected";
}

internal sealed class ExternalLocalTarget
{
    public string Made;
    public ExternalLocalTarget() => Made = SelectedDefault();
    public ExternalLocalTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class ExternalCapturedTarget
{
    public string Made;
    public ExternalCapturedTarget() => Made = SelectedDefault();
    public ExternalCapturedTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class ExternalConditionalTarget
{
    public string Made;
    public ExternalConditionalTarget() => Made = SelectedDefault();
    public ExternalConditionalTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class ExternalCopiedTarget
{
    public string Made;
    public ExternalCopiedTarget() => Made = SelectedDefault();
    public ExternalCopiedTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class ExternalInitializedTarget
{
    public string Made;
    public ExternalInitializedTarget(int unused) => Made = UnselectedArgument();
    private ExternalInitializedTarget(string value) => Made = SelectedArgument(value);
    private static string SelectedArgument(string value) => value;
    private static string UnselectedArgument() => "unselected";
}

internal sealed class ExternalUntypedTarget
{
    public string Made;
    public ExternalUntypedTarget() => Made = SelectedDefault();
    public ExternalUntypedTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class RepeatedLocalTarget
{
    public string Made;
    public RepeatedLocalTarget() => Made = SelectedDefault();
    public RepeatedLocalTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class RepeatedPublishedTarget
{
    public string Made;
    public RepeatedPublishedTarget() => Made = SelectedDefault();
    public RepeatedPublishedTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class RepeatedCopiedTarget
{
    public string Made;
    public RepeatedCopiedTarget() => Made = SelectedDefault();
    public RepeatedCopiedTarget(int unused) => Made = UnselectedArgument();
    private static string SelectedDefault() => "default";
    private static string UnselectedArgument() => "unselected";
}

internal sealed class RepeatedIdentityTarget
{
    public string Made;
    public RepeatedIdentityTarget() => Made = SelectedDefault();
    private static string SelectedDefault() => "identity";
}

internal sealed class AcyclicInitializedTarget
{
    public string Made;
    public AcyclicInitializedTarget(int unused) => Made = UnselectedArgument();
    private AcyclicInitializedTarget(string made) => Made = SelectedArgument(made);
    private static string SelectedArgument(string made) => made;
    private static string UnselectedArgument() => "unselected";
}
