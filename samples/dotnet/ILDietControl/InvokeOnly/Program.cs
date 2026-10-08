using System;
using System.Globalization;

namespace ILDietInvokeOnly;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("invoke-only prefix");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_SIGNATURE_INVOCATION") == "1")
            return;
        Console.WriteLine("== signature invocation ==");
        Type owner = typeof(SignatureOwner);
        Invoke("parameter", owner.GetMethod("ParameterOnly")!.GetParameters()[0].ParameterType);
        Invoke("return", owner.GetMethod("ReturnOnly")!.ReturnType);
        Console.WriteLine("signature invocation end");
        if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_CTOR_SIGNATURE_INVOCATION") == "1")
            return;
        Console.WriteLine("== constructor signature invocation ==");
        Type selected = typeof(ConstructorSignatureHolder).GetConstructors()[0].GetParameters()[0].ParameterType;
        Console.WriteLine(selected.GetMethod("Value")!.Invoke(null, null));
        selected = typeof(LibrarySignatureHolder).GetConstructors()[0].GetParameters()[0].ParameterType;
        Console.WriteLine(selected.GetMethod("Get")!.Invoke(null, null));
        Console.WriteLine("constructor signature invocation end");
    }

    private static void Invoke(string label, Type selected)
    {
        Console.WriteLine(label + "=" + selected.GetMethod("Value")!.Invoke(null, null));
    }
}

internal sealed class SignatureOwner
{
    public static void ParameterOnly(SignatureTarget<int> value) { }

    public static SignatureTarget<string>? ReturnOnly() => null;
}

internal sealed class SignatureTarget<T>
{
    public static string Value() => typeof(T).Name;
}

internal sealed class ConstructorSignatureHolder
{
    public ConstructorSignatureHolder(ConstructorSignatureTarget<int> value)
        => ILDietControlLib.InvokeConstructorOnlyDependency.Read();
}

internal sealed class ConstructorSignatureTarget<T>
{
    public static string Value() => typeof(T).Name;
}

internal sealed class LibrarySignatureHolder
{
    public LibrarySignatureHolder(LibrarySignatureTarget<ILDietControlLib.InvokeSignatureMoney> value)
        => ILDietControlLib.InvokeConstructorOnlyDependency.Read();
}

internal sealed class LibrarySignatureTarget<T>
{
    public static T Get() => default!;
}
