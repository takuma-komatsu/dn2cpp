using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

// Console self-host runtime primitives, exercised exactly: the deterministic
// HashCode seed (every line here is hash-value-independent), an un-thrown
// exception's null StackTrace, and the process-image paths (asserted by property).
internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && (args[0] == "runtime-type-routes" || args[0] == "native-boundary"))
        {
            RuntimeTypeIsPointerSubset.IsPointerChecks.RunArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "gc-native-boundary")
        {
            GcNotificationSubset.Program.RunNativeUnavailable();
            GcFinalizerWorkerSubset.Program.RunNativeRefusal();
            Console.WriteLine("gc-max-generation:" + GC.MaxGeneration);
            return;
        }
        // Separate processes keep the primitive and GC observations unchanged.
        if (args.Length > 0 && (args[0] == "delegate-callable" || args[0] == "delegate-refusals"))
        {
            DelegateCallableBodies.Program.Run(args.Length > 1 ? args[1] : "reflection",
                args[0] == "delegate-refusals");
            return;
        }
        // Dictionary keyed on ValueTuple<string,string>: enumeration is insertion
        // order (no removals here), so the output is seed-independent.
        var d = new Dictionary<(string, string), string>();
        d[("a", "b")] = "ab";
        d[("c", "d")] = "cd";
        d[("e", "f")] = "ef";
        d[("a", "b")] = "AB"; // overwrite keeps the original insertion slot

        Console.WriteLine("count " + d.Count);
        Console.WriteLine("idx " + d[("c", "d")]);
        Console.WriteLine("has-ab " + d.ContainsKey(("a", "b")));
        Console.WriteLine("has-xy " + d.ContainsKey(("x", "y")));
        Console.WriteLine("try-ef " + (d.TryGetValue(("e", "f"), out var v1) ? v1 : "<none>"));
        Console.WriteLine("try-zz " + (d.TryGetValue(("z", "z"), out var v2) ? v2 : "<none>"));
        foreach (var kv in d)
            Console.WriteLine("kv " + kv.Key.Item1 + "/" + kv.Key.Item2 + "=" + kv.Value);

        // HashSet: Count and membership only, no enumeration-order dependency.
        var set = new HashSet<(string, string)>();
        set.Add(("p", "q"));
        set.Add(("r", "s"));
        set.Add(("p", "q")); // duplicate
        Console.WriteLine("set-count " + set.Count);
        Console.WriteLine("set-has-pq " + set.Contains(("p", "q")));
        Console.WriteLine("set-has-tt " + set.Contains(("t", "t")));

        // An un-thrown exception has a null StackTrace in both runtimes. A thrown
        // one diverges (native has no stack-trace model) and is not exercised.
        var ex = new InvalidOperationException("boom");
        Console.WriteLine("st-null " + (ex.StackTrace is null));
        Console.WriteLine("msg " + ex.Message);

        // The process image. Properties only: under `dotnet app.dll` the base
        // directory is the dll's and ProcessPath is the host's.
        string baseDir = AppContext.BaseDirectory;
        Console.WriteLine("base-exists " + Directory.Exists(baseDir));
        Console.WriteLine("base-rooted " + Path.IsPathRooted(baseDir));
        Console.WriteLine("base-trailing-sep " + Path.EndsInDirectorySeparator(baseDir));

        string? procPath = Environment.ProcessPath;
        Console.WriteLine("proc-nonnull " + (procPath is not null));
        Console.WriteLine("proc-exists " + File.Exists(procPath));
        Console.WriteLine("proc-rooted " + Path.IsPathRooted(procPath));

        // Later sections force collections; the primitives above run first and
        // never observe them.
        MiscIntrinsicSubset.Program.__GateEntry();
        GcAllocatedBytesSubset.Program.__GateEntry();
        if (args.Length > 0 && args[0] == "before-nullable-reboxing")
            return;
        NullableReboxingSubset.Program.Run();
        ActivatorDefaultSubset.ActivatorChecks.Run();
        if (args.Length > 0 && args[0] == "before-gc-contracts")
            return;
        GcCollectionSubset.Program.Run();
        GcGenerationSubset.Program.Run();
        GcMemoryQuerySubset.Program.Run();
        GcNotificationSubset.Program.Run();
        GcPauseSubset.Program.Run();
        GcFinalizerWorkerSubset.Program.Run();
        BufferReferenceMoveSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-primitives")
            return;
        RuntimeMethodEnumerationSubset.Program.Run();
        RuntimeMethodHandleSubset.Program.Run();
        RuntimeTypeMaterializationSubset.Program.Run();
        RuntimeFieldHandleSubset.Program.Run();
        RuntimeTypeHandleCopySubset.Program.Run();
        SignatureBlobSubset.Program.Run();
        CanonicalTypeComparisonSubset.Program.Run();
        RuntimeTypeHandleAccessorSubset.Program.Run();
        ConstraintCheckSubset.Program.Run();
        RuntimeTypeHandleConstructorSubset.Program.Run();
        RuntimeTypeCacheHandleSubset.Program.Run();
        MethodGenericCountSubset.Program.Run();
        RuntimeTypeInterfaceCheckSubset.Program.Run();
        RuntimeVirtualSlotCountSubset.Program.Run();
        RuntimeMethodSlotLookupSubset.Program.Run();
        AmbiguousMatchFactorySubset.Program.Run();
        MemberNotFoundFactorySubset.Program.Run();
        FormalDeclaringMethodSubset.Program.Run();
        RuntimeTypeInstantiationSubset.Program.Run();
        RuntimeTypeMakeGenericSubset.Program.Run();
        GenericParameterPositionSubset.Program.Run();
        ContainsGenericVariablesSubset.Program.Run();
        GenericConstraintArraySubset.Program.Run();
        CompoundTypeHandleSubset.Program.Run();
        ArrayTypeHandleSubset.Program.Run();
        PrimitiveElementTypeSubset.Program.Run();
        PrimitiveWideningSubset.Program.Run();
        RuntimeTypeQueriesSubset.Program.Run();
        RuntimeTypeDefaultCtorSubset.DefaultCtorChecks.Run();
        RuntimeTypeDefaultCtorSubset.DefaultCtorChecks.RunOfT();
        RuntimeTypeDefaultStructSubset.StructChecks.Run();
        CoreClrActivationInfoSubset.ActivationChecks.Run();
        RuntimeTypeHandleNameSubset.NameChecks.Run();
        RuntimeTypeDeclaringSubset.DeclaringChecks.Run();
        CustomAttributeDataMemberSubset.DataChecks.Run();
        CustomAttributeConstructorSubset.ConstructorChecks.Run();
        RuntimeTypeCacheGenericDefinitionSubset.DefinitionChecks.Run();
        RuntimeIntroducedMethodsSubset.Program.Run();
        RuntimeInterfacesSubset.Program.Run();
        RuntimeFieldsSubset.Program.Run();
        StaticGenericVariablesSubset.Program.Run();
        RuntimeUtf8NameSubset.Program.Run();
        FunctionPointerArgumentsSubset.Program.Run();
        AppContextExitSubset.Program.Run();
        AssemblyExecutionSubset.Program.Run();
        ActivationSubset.Program.Run();
        DefaultPrincipalPublicSubset.Program.Run();
        KnownConstantSubset.Program.Run();
    }
}
