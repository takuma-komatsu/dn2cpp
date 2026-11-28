using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using TrimReflectLib;

namespace RuntimeTemplateBindingOnly;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "boundary")
        {
            ReflectDelegateSubset.Program.RunSignatureBoundaries();
            return;
        }
        if (args.Length > 0 && args[0] == "unsafe")
        {
            ReflectDelegateSubset.Program.RunUnsafeTemplateBindings();
            ReflectDelegateSubset.RuntimeBaseProjectionSubset.RunBoundaries();
            return;
        }
        if (args.Length > 0 && args[0] == "invoke")
        {
            ReflectDelegateSubset.Program.RunSignatureInvokeDescriptors();
            return;
        }
        if (args.Length > 0 && args[0] == "stripped")
        {
            object box = TrimReflectLib.Factory.MakeBox();
            string value = box.ToString()!;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    var target = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), box, box.GetType().GetMethod("ToString")!);
                    Console.WriteLine("template stripped binding => " + (target() == value));
                }
                catch (PlatformNotSupportedException)
                {
                    Console.WriteLine("template stripped binding => PlatformNotSupportedException");
                }
            }
            return;
        }
        ReflectDelegateSubset.Program.RunRuntimeTemplateBindings();
        if (args.Length > 0 && args[0] == "before-base-projections")
            return;
        ReflectDelegateSubset.RuntimeBaseProjectionSubset.Run();
        RunInvalidInitializers();
    }

    private sealed class InvalidDerived : LibBare { }

    private static void RunInvalidInitializers()
    {
        var library = typeof(Factory).Assembly;
        Console.WriteLine("== invalid type initializers ==");
        CheckLoad("type", ReadInvalidType);
        CheckLoad("array", ReadInvalidArray);
        CheckLoad("generic", ReadInvalidGeneric);
        CheckLoad("base", ReadInvalidBase);
        CheckLoad("enum", ReadInvalidEnum);
        CheckLoad("name false", () => library.GetType("TrimReflectLib.LibBare", false));
        CheckLoad("name true", () => library.GetType("TrimReflectLib.LibBare", true));
        CheckLoad("run", RunInvalidInitializer);
        CheckLoad("generic definition and argument", ReadInvalidDefinitionAndArgument);
    }

    private static void CheckLoad(string label, Func<Type?> read)
    {
        try
        {
            Console.WriteLine(label + " => " + (read()?.FullName ?? "<null>"));
        }
        catch (TypeLoadException error)
        {
            Console.WriteLine(label + " => " + error.GetType().Name + " "
                + (error.TypeName ?? "<null>") + ": " + error.Message);
        }
    }

    // The CLR validates each token while compiling the helper, before entering its body.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ReadInvalidType() => typeof(LibBare);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ReadInvalidArray() => typeof(LibBare[]);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ReadInvalidGeneric() => typeof(ReflectDelegateSubset.SignatureBox<LibBare>);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ReadInvalidDefinitionAndArgument() => typeof(InvalidBox<LibBare>);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ReadInvalidBase() => typeof(InvalidDerived);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type ReadInvalidEnum() => typeof(LibColor);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type RunInvalidInitializer()
    {
        RuntimeHelpers.RunClassConstructor(typeof(LibBare).TypeHandle);
        return typeof(void);
    }
}

internal sealed class InvalidBox<T> : LibBox<T>
{
    private static void Initializer(int _) { }
}
