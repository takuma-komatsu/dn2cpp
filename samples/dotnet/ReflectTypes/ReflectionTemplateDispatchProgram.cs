using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Dn2Cpp;

// Only reflection names Call. A clone cannot respell its delegate*<T, string> signature
// per type argument, so it may refuse the call, but never runs it with the
// placeholder's ABI in place of T's. The default run proves that every instantiation
// has a Call row and that no call through it returns a wrong result; a refusal and a
// correct result print alike there. The template-function-pointer-outcomes argument,
// prints each call's result or its refusal.
internal sealed class FunctionPointerCall<T>
{
    public unsafe string Call(IntPtr fn, object value) => ((delegate*<T, string>)fn)((T)value);
}

internal sealed class FixedFunctionPointerCall<T>
{
    public unsafe string Call(IntPtr fn, int value) => ((delegate*<int, string>)fn)(value);
}

// A template member signature names other generic classes over the template's own
// type parameters reordered, partially, or beside concrete arguments. A clone may
// refuse such a row, but never runs it wrongly; the default run prints a refusal and
// a correct result alike, and template-signature-placeholder-outcomes prints each.
internal sealed class SignatureSlot<T> { }

internal sealed class SignatureCouple<T, U> { }

internal sealed class SignatureReorder<T, U>
{
    public string Swapped(SignatureCouple<U, T>? value) => "swapped:" + typeof(U).Name + "," + typeof(T).Name;

    public string Second(SignatureSlot<U>? value) => "second:" + typeof(U).Name;

    public string Mixed(SignatureCouple<T, int>? value) => "mixed:" + typeof(T).Name;

    public string Fixed(SignatureSlot<long>? value) => "fixed:" + typeof(T).Name;
}

internal abstract class SignatureAbstract<T, U>
{
    public abstract string Pair(KeyValuePair<T, int> value);

    public abstract string Plain(string? value);

    public abstract string Mixed(SignatureCouple<T, int>? value);
}

internal sealed class SignatureConcrete<T, U> : SignatureAbstract<T, U>
{
    public override string Pair(KeyValuePair<T, int> value) => "pair:" + value.Value;

    public override string Plain(string? value) => "plain:" + typeof(U).Name;

    public override string Mixed(SignatureCouple<T, int>? value) => "mixed:" + typeof(T).Name;
}

// No site names these overrides of a clone. A preserve rule on Kept, the remedy its
// refusal prints, retains it (keep-template-signatures.xml); Bare passes a type-parameter
// value no clone can run, so its refusal names the closed instantiation instead.
internal abstract class RemedyBase<T>
{
    public abstract string Kept();

    public abstract string Bare(T value);
}

internal sealed class RemedyImpl<T> : RemedyBase<T>
{
    public override string Kept() => "kept:" + typeof(T).Name;

    public override string Bare(T value) => "bare:" + typeof(T).Name;
}

// Only reflection reaches the members below, each closed by MakeGenericType over an
// argument the program names in no instantiation: a body reflecting through its type
// parameter, a generic method of the closed type, and the closed type's member and
// field listings run as .NET runs them, and a constraint the argument violates
// throws.
internal sealed class ConsumerProduct
{
    public override string ToString() => "product";
}

internal sealed class ConsumerFactory<T>
{
    public object? Make() => Activator.CreateInstance(typeof(T));
}

internal sealed class ConsumerGeneric<T>
{
    public string Pair<U>() => "pair:" + typeof(T).Name + "," + typeof(U).Name;
}

internal struct ConsumerStamp
{
    public int Value;

    public override string ToString() => "stamp:" + Value;
}

internal sealed class ConsumerListed<T>
{
    public ConsumerStamp Stamp;

    public ConsumerListed() => Stamp.Value = 7;

    public string Describe() => "listed:" + typeof(T).Name;
}

internal interface IConsumerMark { }

internal sealed class ConsumerConstrained<T> where T : IConsumerMark, new() { }

internal interface IConsumerNamed
{
    string Name();
}

internal sealed class ConsumerNamed<T> : IConsumerNamed
{
    public string Name() => "named:" + typeof(T).Name;
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
            RunFixedFunctionPointers();
            return;
        }
        if (args.Length > 0 && args[0] == "invoke-diagnostics")
        {
            RunInvokeDiagnostics(outcomes: true);
            return;
        }
        if (args.Length > 0 && args[0] == "template-signature-placeholder-outcomes")
        {
            RunTemplateSignaturePlaceholders(outcomes: true);
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
        if (args.Length > 0 && args[0] == "before-reflected-delegates")
            return;
        ReflectRuntimeInstantiationSubset.Program.RunReflectedDelegates();
        if (args.Length > 0 && args[0] == "before-reflected-arrays")
            return;
        ReflectRuntimeInstantiationSubset.Program.RunReflectedArrays();
        if (args.Length > 0 && args[0] == "before-fixed-function-pointers")
            return;
        RunFixedFunctionPointers();
        if (args.Length > 0 && args[0] == "before-template-delegate-method-identity")
            return;
        ReflectRuntimeInstantiationSubset.Program.RunReflectedDelegateMethodIdentity();
        if (args.Length > 0 && args[0] == "before-template-signature-placeholders")
            return;
        RunTemplateSignaturePlaceholders(outcomes: false);
        if (args.Length > 0 && args[0] == "before-template-consumers")
            return;
        RunTemplateConsumers();
        if (args.Length > 0 && args[0] == "before-template-arguments")
            return;
        ReflectRuntimeInstantiationSubset.Program.RunTemplateArguments();
        if (args.Length > 0 && args[0] == "before-template-signature-allocation")
            return;
        TemplateSignatureAllocationProgram.Run();
        if (args.Length > 0 && args[0] == "before-forbidden-generic-arguments")
            return;
        unsafe
        {
            ReflectRuntimeInstantiationSubset.Program.RunForbiddenArguments(args, typeof(delegate*<int>));
        }
    }

    private static void RunTemplateConsumers()
    {
        Console.WriteLine("== template reflection consumers ==");
        Type factory = typeof(ConsumerFactory<>).MakeGenericType(typeof(ConsumerProduct));
        try
        {
            Console.WriteLine("factory made=" + factory.GetMethod("Make")!.Invoke(Activator.CreateInstance(factory), null));
        }
        catch (Exception e)
        {
            Console.WriteLine("factory fault=" + e.GetType().Name);
        }
        Type generic = typeof(ConsumerGeneric<>).MakeGenericType(typeof(int));
        try
        {
            Console.WriteLine("generic method=" + generic.GetMethod("Pair")!.MakeGenericMethod(typeof(string))
                .Invoke(Activator.CreateInstance(generic), null));
        }
        catch (Exception e)
        {
            Console.WriteLine("generic method fault=" + e.GetType().Name);
        }
        Type listed = typeof(ConsumerListed<>).MakeGenericType(typeof(long));
        object listedInstance = Activator.CreateInstance(listed)!;
        try
        {
            foreach (MemberInfo member in listed.GetMembers())
                if (member is MethodInfo { Name: "Describe" } describe)
                    Console.WriteLine("members describe=" + describe.Invoke(listedInstance, null));
            foreach (FieldInfo field in listed.GetFields())
                Console.WriteLine("fields " + field.Name + "=" + field.GetValue(listedInstance));
        }
        catch (Exception e)
        {
            Console.WriteLine("listing fault=" + e.GetType().Name);
        }
        // .NET throws ArgumentException; the image has no instantiation to return, so the
        // runtime refuses with its NotSupportedException.
        try
        {
            Console.WriteLine("constrained object=" + typeof(ConsumerConstrained<>).MakeGenericType(typeof(object)).Name);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException)
        {
            Console.WriteLine("constrained object=refused");
        }
        // The clone's interface is found by name, and the row read off it dispatches to the
        // clone's implementation.
        Type named = typeof(ConsumerNamed<>).MakeGenericType(typeof(short));
        try
        {
            Type itf = named.GetInterface("IConsumerNamed")
                ?? throw new InvalidOperationException("The clone lists no IConsumerNamed.");
            Console.WriteLine("interface named=" + itf.GetMethod("Name")!.Invoke(Activator.CreateInstance(named), null));
        }
        catch (Exception e)
        {
            Console.WriteLine("interface fault=" + e.GetType().Name);
        }
        Console.WriteLine("template reflection consumers end");
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
            + " reason=" + (invokeFault?.Message.Contains("this closed generic instantiation has no compiled code",
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

    private static unsafe void RunFixedFunctionPointers()
    {
        Console.WriteLine("== fixed template function pointers ==");
        delegate*<int, string> describe = &DescribeInt;
        CallFixedTemplate(typeof(int), (IntPtr)describe, 42);
        CallFixedTemplate(typeof(long), (IntPtr)describe, -7);
        CallFixedTemplate(typeof(string), (IntPtr)describe, 0);
        Console.WriteLine("fixed template function pointers end");
    }

    private static void RunTemplateSignaturePlaceholders(bool outcomes)
    {
        Console.WriteLine("== template signature placeholders ==");
        foreach (Type[] arguments in new[] { new[] { typeof(int), typeof(string) }, new[] { typeof(Guid), typeof(bool) } })
        {
            Type closed = typeof(SignatureReorder<,>).MakeGenericType(arguments);
            object inst = Activator.CreateInstance(closed)!;
            string prefix = "sigplace " + arguments[0].Name + "," + arguments[1].Name;
            string t = arguments[0].Name, u = arguments[1].Name;
            foreach (var (name, expected) in new[] { ("Swapped", "swapped:" + u + "," + t),
                ("Second", "second:" + u), ("Mixed", "mixed:" + t), ("Fixed", "fixed:" + t) })
            {
                MethodInfo method = closed.GetMethod(name)
                    ?? throw new InvalidOperationException("The " + name + " row is missing.");
                string? result = Attempt(() => (string)method.Invoke(inst, new object?[] { null })!,
                    out Exception? fault);
                Console.WriteLine(prefix + " " + name + " " + (outcomes ? DescribeOutcome(result, fault)
                    : "safe=" + (result == expected || fault is PlatformNotSupportedException)));
            }
            Console.WriteLine(prefix + " Fixed parameter=" + closed.GetMethod("Fixed")!.GetParameters()[0].ParameterType);
        }
        // The base row of an abstract template level whose signature names a mixed
        // placeholder class refuses a wrongly typed argument instead of reading its box as
        // that struct; its sibling over a closed signature dispatches to the override.
        foreach (Type[] arguments in new[] { new[] { typeof(int), typeof(string) }, new[] { typeof(Guid), typeof(bool) } })
        {
            Type closed = typeof(SignatureConcrete<,>).MakeGenericType(arguments);
            object inst = Activator.CreateInstance(closed)!;
            Type declaring = typeof(SignatureAbstract<,>).MakeGenericType(arguments);
            string prefix = "sigabstract " + arguments[0].Name + "," + arguments[1].Name;
            MethodInfo pair = declaring.GetMethod("Pair")
                ?? throw new InvalidOperationException("The abstract Pair row is missing.");
            Attempt(() => (string)pair.Invoke(inst, new object?[] { 5 })!, out Exception? pairFault);
            Console.WriteLine(prefix + " Pair safe=" + (pairFault is ArgumentException or PlatformNotSupportedException));
            MethodInfo plain = declaring.GetMethod("Plain")
                ?? throw new InvalidOperationException("The abstract Plain row is missing.");
            string? plained = Attempt(() => (string)plain.Invoke(inst, new object?[] { null })!, out Exception? plainFault);
            Console.WriteLine(prefix + " Plain " + DescribeOutcome(plained, plainFault));
            MethodInfo mixed = declaring.GetMethod("Mixed")
                ?? throw new InvalidOperationException("The abstract Mixed row is missing.");
            string? mixedResult = Attempt(() => (string)mixed.Invoke(inst, new object?[] { null })!, out Exception? mixedFault);
            Console.WriteLine(prefix + " Mixed " + (outcomes ? DescribeOutcome(mixedResult, mixedFault)
                : "safe=" + (mixedResult == "mixed:" + arguments[0].Name || mixedFault is PlatformNotSupportedException)));
        }
        foreach (Type[] arguments in new[] { new[] { typeof(int) }, new[] { typeof(Guid) } })
        {
            Type closed = typeof(RemedyImpl<>).MakeGenericType(arguments);
            object inst = Activator.CreateInstance(closed)!;
            Type declaring = typeof(RemedyBase<>).MakeGenericType(arguments);
            string prefix = "sigremedy " + arguments[0].Name;
            foreach (var (name, expected) in new[] { ("Kept", "kept:" + arguments[0].Name), ("Bare", "bare:" + arguments[0].Name) })
            {
                MethodInfo method = declaring.GetMethod(name)
                    ?? throw new InvalidOperationException("The abstract " + name + " row is missing.");
                object?[] parameters = new object?[method.GetParameters().Length];
                string? result = Attempt(() => (string)method.Invoke(inst, parameters)!, out Exception? fault);
                Console.WriteLine(prefix + " " + name + " " + (outcomes || name == "Kept" ? DescribeOutcome(result, fault)
                    : "safe=" + (result == expected || fault is PlatformNotSupportedException)));
            }
        }
        Console.WriteLine("template signature placeholders end");
    }

    private static void CallFixedTemplate(Type arg, IntPtr fn, int value)
    {
        Type closed = typeof(FixedFunctionPointerCall<>).MakeGenericType(arg);
        object inst = Activator.CreateInstance(closed)!;
        MethodInfo call = closed.GetMethod("Call")
            ?? throw new InvalidOperationException("The fixed function pointer Call row is missing.");
        string prefix = "fixedfn " + arg.Name;
        Console.WriteLine(prefix + " row=True");
        string? invoked = Attempt(() => (string)call.Invoke(inst, new object[] { fn, value })!,
            out Exception? invokeFault);
        string? bound = Attempt(() => ((Func<IntPtr, int, string>)call.CreateDelegate(
            typeof(Func<IntPtr, int, string>), inst))(fn, value), out Exception? boundFault);
        Console.WriteLine(prefix + " Invoke=" + DescribeOutcome(invoked, invokeFault));
        Console.WriteLine(prefix + " CreateDelegate=" + DescribeOutcome(bound, boundFault));
    }
}
