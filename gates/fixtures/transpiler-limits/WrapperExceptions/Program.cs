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
        bool canonicalLink = args is [_, "canonical-link", _, _];
        if (args.Length != 1 && !canonicalLink)
            throw new ArgumentException("Pass the Dn2Cpp.Transpiler.dll used by this gate.");

        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[0]));
        if (canonicalLink)
            return EmitCanonicalLink(assembly, args[2], args[3]);

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

    private static int EmitCanonicalLink(Assembly assembly, string input, string outDir)
    {
        var optionsType = assembly.GetType("Dn2Cpp.TranspileOptions", throwOnError: true)!;
        var compilationType = assembly.GetType("Dn2Cpp.Compilation", throwOnError: true)!;
        var emitterType = assembly.GetType("Dn2Cpp.CppEmitter", throwOnError: true)!;
        var backendType = assembly.GetType("Dn2Cpp.ConsoleBackend", throwOnError: true)!;
        var boundType = assembly.GetType("Dn2Cpp.InstantiationBoundException", throwOnError: true)!;
        var backend = Activator.CreateInstance(backendType, nonPublic: true)!;
        var options = Activator.CreateInstance(optionsType)!;
        optionsType.GetProperty("Input")!.SetValue(options, input);
        optionsType.GetProperty("UseILDiet")!.SetValue(options, false);
        optionsType.GetProperty("Backend", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(options, backend);
        var splitBytes = optionsType.GetProperty("SplitBytes")!.GetValue(options)!;

        // The console support shim may participate, but no framework references
        // may spend the instantiation budget before the canonical link.
        string shim = Path.Combine(Path.GetDirectoryName(assembly.Location)!, "Dn2Cpp.Runtime.dll");
        string[] paths = File.Exists(shim) ? [input, shim] : [input];
        try
        {
            var compilation = compilationType.GetMethod("Create")!.Invoke(null, new object[] { paths, options })!;
            var emitter = Activator.CreateInstance(emitterType, compilation, backend, false)!;
            var emit = emitterType.GetMethod("Emit", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var sources = emit.Invoke(emitter, new[] { (Action<string, string>)Write, splitBytes })!;
            Write("generated.h", sources.GetType().GetProperty("Header")!.GetValue(sources)!.ToString()!);
            Write("generated.cpp", sources.GetType().GetProperty("Data")!.GetValue(sources)!.ToString()!);
            return 0;
        }
        catch (TargetInvocationException ex) when (ex.InnerException?.GetType() == boundType)
        {
            Console.Error.WriteLine($"error: {ex.InnerException!.Message}");
            return 2;
        }

        void Write(string name, string contents)
        {
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, name), contents);
        }
    }

    private static void SetSignature<T>(object method, object returnType)
    {
        var signature = new MethodSignature<T>(default, (T)returnType, 0, 0, ImmutableArray<T>.Empty);
        method.GetType().GetProperty("Signature")!.SetValue(method, signature);
    }
}
