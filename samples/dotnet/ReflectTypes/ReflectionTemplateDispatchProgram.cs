using System;
using System.Globalization;
using System.Reflection;

namespace Dn2Cpp;

// Only reflection names Call. A clone cannot respell its delegate*<T, string> signature
// per type argument, so it may refuse the call, but never runs it with the
// placeholder's ABI in place of T's. The default run proves that every instantiation
// has a Call row and that no call through it returns a wrong result; a refusal and a
// correct result print alike there. The template-function-pointer-outcomes argument,
// which only the native gate passes, prints each call's result or its refusal.
internal sealed class FunctionPointerCall<T>
{
    public unsafe string Call(IntPtr fn, object value) => ((delegate*<T, string>)fn)((T)value);
}

internal static class ReflectionTemplateDispatchProgram
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "template-function-pointer-outcomes")
        {
            RunTemplateFunctionPointers(outcomes: true);
            return;
        }
        Console.WriteLine("== reflection template dispatch ==");
        ReflectRuntimeInstantiationSubset.Program.RunReflectedBodies();
        ReflectRuntimeInstantiationSubset.Program.RunTemplateCalls();
        ReflectRuntimeInstantiationSubset.Program.RunTemplateValues();
        ReflectRuntimeInstantiationSubset.Program.RunTemplateCopies();
        Console.WriteLine("reflection template dispatch end");
        if (args.Length > 0 && args[0] == "before-template-function-pointers")
            return;
        RunTemplateFunctionPointers(outcomes: false);
    }

    private static string DescribeInt(int value) => "int:" + value;

    private static string DescribeLong(long value) => "long:" + value;

    private static string DescribeString(string value) => "string:" + value;

    private static unsafe void RunTemplateFunctionPointers(bool outcomes)
    {
        Console.WriteLine("== template function pointers ==");
        delegate*<int, string> describeInt = &DescribeInt;
        delegate*<long, string> describeLong = &DescribeLong;
        delegate*<string, string> describeString = &DescribeString;
        CallThroughTemplate(typeof(int), (IntPtr)describeInt, 42, "int:42", outcomes);
        CallThroughTemplate(typeof(long), (IntPtr)describeLong, 1234567890123L, "long:1234567890123", outcomes);
        CallThroughTemplate(typeof(string), (IntPtr)describeString, "s", "string:s", outcomes);
        Console.WriteLine("template function pointers end");
    }

    private static void CallThroughTemplate(Type arg, IntPtr fn, object value, string expected, bool outcome)
    {
        Type closed = typeof(FunctionPointerCall<>).MakeGenericType(arg);
        object inst = Activator.CreateInstance(closed)!;
        MethodInfo? call = closed.GetMethod("Call");
        string? result;
        string? refusal = null;
        try
        {
            result = (string?)call!.Invoke(inst, new object[] { fn, value });
        }
        catch (Exception e)
        {
            result = null;
            refusal = "refused " + e.GetType().Name;
        }
        if (outcome)
        {
            Console.WriteLine("fnptr " + arg.Name + " outcome: " + (refusal ?? result ?? "null"));
            return;
        }
        Console.WriteLine("fnptr " + arg.Name + ": row=" + (call is not null)
            + " mismatched=" + (result is not null && result != expected));
    }
}
