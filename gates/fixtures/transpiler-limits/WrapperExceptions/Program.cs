using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;

namespace WrapperExceptions;

internal static class Program
{
    private static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length != 1)
            throw new ArgumentException("Pass the Dn2Cpp.Transpiler.dll used by this gate.");

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
        var compilerType = assembly.GetType("Dn2Cpp.MethodCompiler", throwOnError: true)!;
        var methodType = assembly.GetType("Dn2Cpp.MethodInfo", throwOnError: true)!;
        var moduleType = assembly.GetType("Dn2Cpp.Module", throwOnError: true)!;
        var typeDescType = assembly.GetType("Dn2Cpp.TypeDesc", throwOnError: true)!;
        var boundType = assembly.GetType("Dn2Cpp.InstantiationBoundException", throwOnError: true)!;
        var strictType = assembly.GetType("Dn2Cpp.StrictCompletionException", throwOnError: true)!;
        var wrapper = compilerType.GetMethod("SynthesizeWrapperBody", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Load the gate's exact compiler and inject at its lowering boundary; a
        // real wrapper's instantiation count depends on the loaded BCL version.
        var method = Activator.CreateInstance(methodType)!;
        methodType.GetField("Attributes")!.SetValue(method, MethodAttributes.Static);
        methodType.GetField("Module")!.SetValue(method, Activator.CreateInstance(moduleType));
        var returnType = typeDescType.GetMethod("MakePrimitive")!.Invoke(null, new object[] { PrimitiveTypeCode.Void })!;
        typeof(Program).GetMethod(nameof(SetSignature), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeDescType).Invoke(null, new[] { method, returnType });
        var compiler = Activator.CreateInstance(compilerType, new object?[] { null, method, null, null })!;
        var errors = new[]
        {
            new NotSupportedException("unsupported wrapper shape"),
            (Exception)Activator.CreateInstance(boundType, new object[] { "instantiation bound" })!,
            (Exception)Activator.CreateInstance(strictType, new object[] { "strict completion" })!,
        };

        bool passed = true;
        foreach (var error in errors)
        {
            bool lowerRan = false;
            Func<bool> lower = () => { lowerRan = true; throw error; };
            bool fallback = false;
            bool escaped = false;
            try
            {
                fallback = wrapper.Invoke(compiler, new object[] { "Unused*", lower, "exception probe" }) is null;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is { } inner)
            {
                escaped = ReferenceEquals(inner, error);
            }
            bool expectedFallback = error.GetType() == typeof(NotSupportedException);
            bool correct = lowerRan && (expectedFallback ? fallback && !escaped : escaped && !fallback);
            passed &= correct;
            Console.WriteLine($"wrapper {error.GetType().Name}: {(correct ? "OK" : "FAIL")}");
        }
        return passed ? 0 : 1;
    }

    private static void SetSignature<T>(object method, object returnType)
    {
        var signature = new MethodSignature<T>(default, (T)returnType, 0, 0, ImmutableArray<T>.Empty);
        method.GetType().GetProperty("Signature")!.SetValue(method, signature);
    }
}
