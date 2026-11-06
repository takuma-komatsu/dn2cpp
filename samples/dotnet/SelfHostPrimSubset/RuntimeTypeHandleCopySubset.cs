using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RuntimeTypeHandleCopySubset;

internal static class Program
{
    private delegate ReadOnlySpan<IntPtr> CopyHandles(RuntimeTypeHandle[] input, Span<IntPtr> scratch);
    private delegate IntPtr[] CopyTypes(Type[] input, out int length);
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;
    private static int s_route;
    private static string s_label;

    private static MethodInfo HandlesMethod() => typeof(RuntimeTypeHandle).GetMethod("CopyRuntimeTypeHandles", Flags,
        new Type[] { typeof(RuntimeTypeHandle[]), typeof(Span<IntPtr>) });
    private static MethodInfo TypesMethod() => typeof(RuntimeTypeHandle).GetMethod("CopyRuntimeTypeHandles", Flags,
        new Type[] { typeof(Type[]), typeof(int).MakeByRefType() });

    // The fixture replaces these wrappers with the exact CoreLib call/calli ABI.
    private static ReadOnlySpan<IntPtr> HandlesDirect(RuntimeTypeHandle[] input, Span<IntPtr> scratch) =>
        HandlesMethod().CreateDelegate<CopyHandles>()(input, scratch);
    private static ReadOnlySpan<IntPtr> HandlesPointer(RuntimeTypeHandle[] input, Span<IntPtr> scratch) =>
        HandlesMethod().CreateDelegate<CopyHandles>()(input, scratch);
    private static IntPtr[] TypesDirect(Type[] input, out int length) =>
        TypesMethod().CreateDelegate<CopyTypes>()(input, out length);
    private static IntPtr[] TypesPointer(Type[] input, out int length) =>
        TypesMethod().CreateDelegate<CopyTypes>()(input, out length);

    private static ReadOnlySpan<IntPtr> Copy(RuntimeTypeHandle[] input, Span<IntPtr> scratch) =>
        s_route == 1 ? HandlesDirect(input, scratch) : HandlesPointer(input, scratch);
    private static IntPtr[] Copy(Type[] input, ref int length)
    {
        if (s_route == 1) return TypesDirect(input, out length);
        if (s_route == 2) return TypesPointer(input, out length);
        object[] args = { input, length };
        try
        {
            var result = (IntPtr[])typeof(RuntimeTypeHandle).GetMethod("CopyRuntimeTypeHandles", Flags,
                new Type[] { typeof(Type[]), typeof(int).MakeByRefType() }).Invoke(null, args);
            length = (int)args[1];
            return result;
        }
        catch (TargetInvocationException e) when (e.InnerException is NullReferenceException)
        {
            length = (int)args[1];
            throw e.InnerException;
        }
    }

    private static void Check(string name, bool success)
    {
        Console.WriteLine("handle-copy-" + s_label + "-" + name + ":" + success);
        if (!success) throw new InvalidOperationException(name);
    }

    private static bool IsDefault(ReadOnlySpan<IntPtr> value) =>
        value.Length == 0 && Unsafe.IsNullRef(ref MemoryMarshal.GetReference(value));

    private static void RunTypes()
    {
        int length = 17;
        var result = Copy((Type[])null, ref length);
        Check("type-null", result is null && length == 0);
        length = 17;
        result = Copy(Array.Empty<Type>(), ref length);
        Check("type-empty", result is null && length == 0);
        Type[] types = { typeof(string), typeof(int), typeof(string) };
        result = Copy(types, ref length);
        bool values = length == types.Length && result.Length == types.Length;
        for (int i = 0; i < result.Length; i++) values &= result[i] == types[i].TypeHandle.Value;
        types[0] = typeof(int);
        Check("type-values", values && result[0] == typeof(string).TypeHandle.Value);
        length = 17;
        bool nullElement = false;
        try { Copy(new Type[] { typeof(string), null }, ref length); }
        catch (NullReferenceException) { nullElement = length == 17; }
        Check("type-null-element", nullElement);
    }

    private static void RunHandles()
    {
        IntPtr sentinel = new IntPtr(42);
        IntPtr[] scratch = { sentinel, sentinel, sentinel, sentinel, sentinel };
        Check("null", IsDefault(Copy(null, scratch)) && scratch[0] == sentinel);
        Check("empty", IsDefault(Copy(Array.Empty<RuntimeTypeHandle>(), scratch)) && scratch[0] == sentinel);
        RuntimeTypeHandle[] input = { typeof(string).TypeHandle, default(RuntimeTypeHandle), typeof(int).TypeHandle, typeof(string).TypeHandle };
        ReadOnlySpan<IntPtr> result = Copy(input, scratch);
        bool values = result.Length == input.Length;
        for (int i = 0; i < result.Length; i++) values &= result[i] == input[i].Value;
        scratch[0] = sentinel;
        Check("scratch-alias", values && result[0] == sentinel && scratch[4] == sentinel);
        IntPtr[] exact = new IntPtr[4];
        result = Copy(input, exact);
        exact[1] = sentinel;
        Check("exact-alias", result.Length == 4 && result[1] == sentinel);
        IntPtr[] shortScratch = { sentinel };
        result = Copy(input, shortScratch);
        bool allocated = result.Length == input.Length;
        for (int i = 0; i < result.Length; i++) allocated &= result[i] == input[i].Value;
        input[0] = typeof(int).TypeHandle;
        GC.Collect();
        Check("allocation", allocated && shortScratch[0] == sentinel && result[0] == typeof(string).TypeHandle.Value);
        Span<IntPtr> stack = stackalloc IntPtr[4];
        ReadOnlySpan<IntPtr> stackResult = Copy(input, stack);
        stack[1] = sentinel;
        Check("stack-alias", stackResult.Length == 4 && stackResult[1] == sentinel);
        result = Copy(input, Span<IntPtr>.Empty);
        Check("no-scratch", result.Length == 4 && result[0] == input[0].Value && result[1] == IntPtr.Zero);
    }

    internal static void Run()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Console.WriteLine("== Runtime type handle copies ==");
        s_route = 0;
        s_label = "reflection";
        MethodInfo handles = HandlesMethod();
        MethodInfo types = TypesMethod();
        Check("handles-shape", handles.IsAssembly && handles.IsStatic && !handles.IsGenericMethod
            && handles.ReturnType == typeof(ReadOnlySpan<IntPtr>) && handles.GetParameters().Length == 2);
        Check("types-shape", types.IsAssembly && types.IsStatic && !types.IsGenericMethod
            && types.ReturnType == typeof(IntPtr[]) && types.GetParameters().Length == 2);
        RunTypes();
        bool rejected = false;
        try
        {
            typeof(RuntimeTypeHandle).GetMethod("CopyRuntimeTypeHandles", Flags,
                new Type[] { typeof(RuntimeTypeHandle[]), typeof(Span<IntPtr>) }).Invoke(null, new object[] { null, null });
        }
        catch (NotSupportedException) { rejected = true; }
        Check("span-rejected-before-body", rejected);
        Console.WriteLine("Runtime type handle copies end");
    }

    internal static void RunArtifactChecks(string[] args)
    {
        Run();
        for (int route = 1; route <= 2; route++)
        {
            s_route = route;
            s_label = route == 1 ? "direct" : "pointer";
            RunHandles();
            RunTypes();
        }
        Console.WriteLine("Runtime type handle copy artifact end");
    }
}
