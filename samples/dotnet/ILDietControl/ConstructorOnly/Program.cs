using System;
using System.Globalization;

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
