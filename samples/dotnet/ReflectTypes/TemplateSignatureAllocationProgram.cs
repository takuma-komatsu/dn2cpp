using System;
using System.Globalization;
using System.Reflection;

namespace Dn2Cpp;

internal class SignatureAllocationBase
{
    public virtual string Describe() => "base";
}

internal sealed class SignatureOnlyPayload<T> : SignatureAllocationBase
{
    public override string Describe() => SignatureOnlyDependency.Marker();
}

internal static class SignatureOnlyDependency
{
    internal static string Marker() => "signature-only";
}

internal sealed class SignatureConstructedPayload<T> : SignatureAllocationBase
{
    public override string Describe() => "constructed:" + typeof(T).Name;
}

internal sealed class SignatureMetadataOwner<T> : SignatureAllocationBase
{
    public SignatureOnlyPayload<T>? Payload() => null;

    public SignatureConstructedPayload<T>? Constructed() => null;

    public override string Describe() => "metadata-only";
}

internal sealed class SignatureStaticOwner<T> : SignatureAllocationBase
{
    public static string Call() => "static:" + typeof(T).Name;

    public override string Describe() => "static-only";
}

internal static class TemplateSignatureAllocationProgram
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Run();
        if (args.Length > 0 && args[0] == "uninitialized-template-outcomes")
        {
            Console.WriteLine("== uninitialized template outcomes ==");
            ReadUninitialized(typeof(SignatureStaticOwner<>).MakeGenericType(typeof(string)), "static-only");
            ReadUninitialized(typeof(SignatureConstructedPayload<>).MakeGenericType(typeof(string)), "constructed");
            Console.WriteLine("uninitialized template outcomes end");
        }
        return;
    }

    internal static void Run()
    {
        Console.WriteLine("== template signature allocation ==");
        Console.WriteLine("ordinary dispatch=" + Dispatch(new SignatureAllocationBase()));
        Type metadata = typeof(SignatureMetadataOwner<>).MakeGenericType(typeof(string));
        MethodInfo[] rows = metadata.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Console.WriteLine("metadata rows=" + rows.Length);
        Console.WriteLine("signature row=" + metadata.GetMethod("Payload")!.Name);
        Type constructed = typeof(SignatureConstructedPayload<>).MakeGenericType(typeof(string));
        var instance = (SignatureAllocationBase)Activator.CreateInstance(constructed)!;
        Console.WriteLine("constructed dispatch=" + Dispatch(instance));
        Type staticOwner = typeof(SignatureStaticOwner<>).MakeGenericType(typeof(string));
        Console.WriteLine("static call=" + staticOwner.GetMethod("Call")!.Invoke(null, null));
        Console.WriteLine("template signature allocation end");
        return;
    }

    private static string Dispatch(SignatureAllocationBase value) => value.Describe();

    private static void ReadUninitialized(Type type, string label)
    {
        try
        {
            var instance = (SignatureAllocationBase)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
            Console.WriteLine(label + " blank=" + Dispatch(instance));
        }
        catch (PlatformNotSupportedException fault)
        {
            Console.WriteLine(label + " blank=" + fault.GetType().Name);
            Console.WriteLine(label + " remedy=" + fault.Message.Contains("SignatureStaticOwner")
                + ":" + fault.Message.Contains("which this image never allocates")
                + ":" + fault.Message.Contains("<method name=\".ctor\" />"));
        }
        return;
    }
}
