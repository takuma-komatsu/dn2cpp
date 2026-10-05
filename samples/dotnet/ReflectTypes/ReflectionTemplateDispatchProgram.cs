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
        if (args.Length > 0 && args[0] == "invoke-diagnostics")
        {
            RunInvokeDiagnostics(outcomes: true);
            return;
        }
        Console.WriteLine("== reflection template dispatch ==");
        ReflectRuntimeInstantiationSubset.Program.RunReflectedBodies();
        ReflectRuntimeInstantiationSubset.Program.RunTemplateCalls();
        ReflectRuntimeInstantiationSubset.Program.RunTemplateValues();
        ReflectRuntimeInstantiationSubset.Program.RunTemplateCopies();
        Console.WriteLine("reflection template dispatch end");
        if (args.Length > 0 && args[0] == "before-template-members")
            return;
        ReflectRuntimeInstantiationSubset.Program.RunTemplateMembers();
        if (args.Length > 0 && args[0] == "before-template-function-pointers")
            return;
        RunTemplateFunctionPointers(outcomes: false);
        if (args.Length > 0 && args[0] == "before-invoke-diagnostics")
            return;
        RunInvokeDiagnostics(outcomes: false);
    }

    private static string DescribeInt(int value) => "int:" + value;

    private static string DescribeLong(long value) => "long:" + value;

    private static string DescribeString(string value) => "string:" + value;

    private static string Echo(string value) => value;

    private static string? Attempt(Func<string> action, out Exception? fault)
    {
        try
        {
            fault = null;
            return action();
        }
        catch (Exception e)
        {
            fault = e;
            return null;
        }
    }

    private static string FaultName(Func<string> action)
    {
        Attempt(action, out Exception? fault);
        return fault?.GetType().Name ?? "none";
    }

    private static string DescribeOutcome(string? result, Exception? fault)
    {
        return fault is null ? "result:" + result
            : fault.GetType().Name + "|" + fault.HResult.ToString("X8") + "|" + fault.Message;
    }

    private static unsafe void RunInvokeDiagnostics(bool outcomes)
    {
        Console.WriteLine("== missing reflection invocation diagnostics ==");
        delegate*<int, string> describeInt = &DescribeInt;
        delegate*<long, string> describeLong = &DescribeLong;
        delegate*<string, string> describeString = &DescribeString;
        CheckInvokeDiagnostics(typeof(int), (IntPtr)describeInt, 42, "int:42", outcomes);
        CheckInvokeDiagnostics(typeof(long), (IntPtr)describeLong, 1234567890123L, "long:1234567890123", outcomes);
        CheckInvokeDiagnostics(typeof(string), (IntPtr)describeString, "s", "string:s", outcomes);
        MethodInfo echo = typeof(ReflectionTemplateDispatchProgram).GetMethod(nameof(Echo),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        Console.WriteLine("diag control=" + (echo.Invoke(null, new object[] { "control" })?.ToString() == Echo("control")));
        Console.WriteLine("missing reflection invocation diagnostics end");
    }

    private static void CheckInvokeDiagnostics(Type arg, IntPtr fn, object value, string expected, bool outcomes)
    {
        Type closed = typeof(FunctionPointerCall<>).MakeGenericType(arg);
        object inst = Activator.CreateInstance(closed)!;
        MethodInfo call = closed.GetMethod("Call")
            ?? throw new InvalidOperationException("The function pointer template Call row is missing.");
        object[] values = { fn, value };
        string? invoked = Attempt(() => (string)call.Invoke(inst, values)!, out Exception? invokeFault);
        string? unwrapped = Attempt(() => (string)call.Invoke(inst, BindingFlags.DoNotWrapExceptions,
            null, values, CultureInfo.InvariantCulture)!, out Exception? unwrappedFault);
        string? created = Attempt(() => ((Func<IntPtr, object, string>)call.CreateDelegate(
            typeof(Func<IntPtr, object, string>), inst))(fn, value), out Exception? createFault);
        string? createdFalse = Attempt(() => ((Func<IntPtr, object, string>)Delegate.CreateDelegate(
            typeof(Func<IntPtr, object, string>), inst, call, throwOnBindFailure: false)!)(fn, value),
            out Exception? createFalseFault);
        Console.WriteLine("diag " + arg.Name + " summary: row=True mismatched="
            + ((invoked is not null && invoked != expected)
                || (unwrapped is not null && unwrapped != expected)
                || (created is not null && created != expected)
                || (createdFalse is not null && createdFalse != expected)));
        Console.WriteLine("diag " + arg.Name + " validation="
            + FaultName(() => (string)call.Invoke(null, values)!) + "/"
            + FaultName(() => (string)call.Invoke(inst, null)!) + "/"
            + FaultName(() => (string)call.Invoke(inst, new object[] { "wrong", value })!) + "/"
            + FaultName(() => { call.CreateDelegate(typeof(Action), inst); return "bound"; }));
        if (!outcomes)
            return;
        Console.WriteLine("diag " + arg.Name + " Invoke=" + DescribeOutcome(invoked, invokeFault));
        Console.WriteLine("diag " + arg.Name + " DoNotWrap=" + DescribeOutcome(unwrapped, unwrappedFault));
        Console.WriteLine("diag " + arg.Name + " CreateDelegate=" + DescribeOutcome(created, createFault));
        Console.WriteLine("diag " + arg.Name + " CreateDelegateFalse=" + DescribeOutcome(createdFalse, createFalseFault));
        bool same = invokeFault is not null && unwrappedFault is not null && createFault is not null
            && createFalseFault is not null && invokeFault.GetType() == unwrappedFault.GetType()
            && invokeFault.GetType() == createFault.GetType() && invokeFault.GetType() == createFalseFault.GetType()
            && invokeFault.HResult == unwrappedFault.HResult && invokeFault.HResult == createFault.HResult
            && invokeFault.HResult == createFalseFault.HResult && invokeFault.Message == unwrappedFault.Message
            && invokeFault.Message == createFault.Message && invokeFault.Message == createFalseFault.Message;
        Console.WriteLine("diag " + arg.Name + " same refusal=" + same);
        Console.WriteLine("diag " + arg.Name + " message owner="
            + (invokeFault?.Message.Contains(closed.ToString(), StringComparison.Ordinal) == true)
            + " member=" + (invokeFault?.Message.Contains(call.Name, StringComparison.Ordinal) == true)
            + " reason=" + (invokeFault?.Message.Contains("the target method's body was not compiled into this image",
                StringComparison.Ordinal) == true) + " unwrapped="
            + (invokeFault is not null && invokeFault.InnerException is null
                && unwrappedFault is not null && unwrappedFault.InnerException is null));
    }

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
