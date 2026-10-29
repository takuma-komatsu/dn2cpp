using System;
using System.Collections.Generic;
using System.IO;

// Console self-host runtime primitives, exercised exactly: the deterministic
// HashCode seed (every line here is hash-value-independent), an un-thrown
// exception's null StackTrace, and the process-image paths (asserted by property).
internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "array-type-handle")
        {
            ArrayTypeHandleSubset.Program.RunHandleArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "array-type-reflection")
        {
            ArrayTypeHandleSubset.Program.RunReflectionArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "compound-handle")
        {
            CompoundTypeHandleSubset.Program.RunHandleArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "compound-reflection")
        {
            CompoundTypeHandleSubset.Program.RunReflectionArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "constraint-array-handle")
        {
            GenericConstraintArraySubset.Program.RunHandleArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "constraint-array-reflection")
        {
            GenericConstraintArraySubset.Program.RunReflectionArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "contains-generic-handle")
        {
            ContainsGenericVariablesSubset.Program.RunHandleArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "contains-generic-reflection")
        {
            ContainsGenericVariablesSubset.Program.RunReflectionArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "generic-position-public")
        {
            GenericParameterPositionSubset.Program.RunPublicArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "generic-position-handle")
        {
            GenericParameterPositionSubset.Program.RunHandleArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "generic-position-reflection")
        {
            GenericParameterPositionSubset.Program.RunReflectionArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "make-generic-public")
        {
            RuntimeTypeMakeGenericSubset.Program.RunPublicArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "make-generic-contract")
        {
            RuntimeTypeMakeGenericSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "type-instantiation-artifact")
        {
            RuntimeTypeInstantiationSubset.Program.RunArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "type-instantiation-reflection")
        {
            RuntimeTypeInstantiationSubset.Program.RunReflectionArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-generic-variable-contract")
        {
            RuntimeTypeGenericVariableSubset.GenericVariablePredicateChecks.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-generic-variable-artifact")
        {
            RuntimeTypeGenericVariableSubset.GenericVariablePredicateChecks.RunArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-generic-variable-reflection")
        {
            RuntimeTypeGenericVariableSubset.GenericVariablePredicateChecks.RunReflectionArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-assembly-contract")
        {
            RuntimeTypeAssemblySubset.RuntimeAssemblyChecks.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-assembly-artifact")
        {
            RuntimeTypeAssemblySubset.RuntimeAssemblyChecks.RunArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-assembly-reflection")
        {
            RuntimeTypeAssemblySubset.RuntimeAssemblyChecks.RunReflectionArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-function-pointer-contract")
        {
            RuntimeTypeFunctionPointerSubset.FunctionPointerPredicateChecks.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-function-pointer-artifact")
        {
            RuntimeTypeFunctionPointerSubset.FunctionPointerPredicateChecks.RunArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-function-pointer-reflection")
        {
            RuntimeTypeFunctionPointerSubset.FunctionPointerPredicateChecks.RunReflectionArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-szarray-artifact")
        {
            RuntimeTypeIsSzArraySubset.Program.RunArtifact(Array.Empty<string>());
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-szarray-contract")
        {
            RuntimeTypeIsSzArraySubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "formal-declaring-public")
        {
            FormalDeclaringMethodSubset.Program.RunPublicArtifact(args);
            return;
        }
        if (args.Length > 0 && args[0] == "formal-declaring-contract")
        {
            FormalDeclaringMethodSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "member-not-found-factory-contract")
        {
            MemberNotFoundFactorySubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "ambiguous-match-factory-contract")
        {
            AmbiguousMatchFactorySubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-method-slot-lookup-contract")
        {
            RuntimeMethodSlotLookupSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-virtual-slot-count-contract")
        {
            RuntimeVirtualSlotCountSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-interface-check-contract")
        {
            RuntimeTypeInterfaceCheckSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "method-generic-count-contract")
        {
            MethodGenericCountSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-cache-handle-contract")
        {
            RuntimeTypeCacheHandleSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-handle-constructor-artifact")
        {
            RuntimeTypeHandleConstructorSubset.Program.RunArtifactChecks(args);
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-handle-constructor-reflection")
        {
            RuntimeTypeHandleConstructorSubset.Program.RunReflectionChecks(args);
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-handle-accessor-artifact")
        {
            RuntimeTypeHandleAccessorSubset.Program.RunArtifactChecks(args);
            return;
        }
        if (args.Length > 0 && args[0] == "runtime-type-handle-copy-artifact")
        {
            RuntimeTypeHandleCopySubset.Program.RunArtifactChecks(args);
            return;
        }
        if (args.Length > 0 && args[0] == "nullable-reboxing-only")
        {
            NullableReboxingSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "nullable-reboxing-artifact")
        {
            NullableReboxingSubset.Program.RunArtifactChecks(args);
            return;
        }
        if (args.Length > 0 && args[0] == "named-contracts-only")
        {
            MulticastNamedConstructorSubset.Program.RunContracts();
            return;
        }
        if (args.Length > 0 && args[0] == "named-constructors-only")
        {
            MulticastNamedConstructorSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "vm-constructors-only")
        {
            MulticastVmConstructorSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "native-vm-constructor-boundary")
        {
            MulticastVmConstructorSubset.Program.RunNativeBoundary();
            return;
        }
        if (args.Length > 0 && args[0] == "multicast-accessors-only")
        {
            MulticastAccessorSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "multicast-operations-only")
        {
            MulticastOperationsSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "multicast-factory-only")
        {
            MulticastFactorySubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "native-multicast-factory-boundary")
        {
            MulticastFactorySubset.Program.RunNativeBoundary();
            return;
        }
        if (args.Length > 0 && args[0] == "delegate-values-only")
        {
            DelegateValueSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "gc-pause-only")
        {
            GcPauseSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "native-gc-notification-unavailable")
        {
            GcNotificationSubset.Program.RunNativeUnavailable();
            return;
        }
        if (args.Length > 0 && args[0] == "native-finalizer-boundary")
        {
            GcFinalizerWorkerSubset.Program.RunNativeBoundary();
            return;
        }
        if (args.Length > 0 && args[0] == "gc-collection-only")
        {
            GcCollectionSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "gc-generation-only")
        {
            GcGenerationSubset.Program.Run();
            return;
        }
        if (args.Length > 0 && args[0] == "gc-memory-only")
        {
            GcMemoryQuerySubset.Program.Run();
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

        // Driven last so nothing it does can perturb the sections above.
        MiscIntrinsicSubset.Program.__GateEntry();
        GcAllocatedBytesSubset.Program.__GateEntry();
        if (args.Length > 0 && args[0] == "before-gc-memory-queries")
            return;
        GcMemoryQuerySubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-gc-generations") return;
        GcGenerationSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-gc-collections") return;
        GcCollectionSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-gc-worker-contract") return;
        GcFinalizerWorkerSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-gc-notifications") return;
        GcNotificationSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-gc-pause") return;
        GcPauseSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-delegate-values") return;
        DelegateValueSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-multicast-factory") return;
        MulticastFactorySubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-multicast-operations") return;
        MulticastOperationsSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-multicast-accessors") return;
        MulticastAccessorSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-vm-constructors") return;
        MulticastVmConstructorSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-named-constructors") return;
        MulticastNamedConstructorSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-method-enumeration") return;
        RuntimeMethodEnumerationSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-method-handle") return;
        RuntimeMethodHandleSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-materialization") return;
        RuntimeTypeMaterializationSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-nullable-reboxing") return;
        NullableReboxingSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-field-handle") return;
        RuntimeFieldHandleSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-handle-copy") return;
        RuntimeTypeHandleCopySubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-signature-blob") return;
        SignatureBlobSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-canonical-type-comparison") return;
        CanonicalTypeComparisonSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-handle-accessor") return;
        RuntimeTypeHandleAccessorSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-constraint-check") return;
        ConstraintCheckSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-handle-constructor") return;
        RuntimeTypeHandleConstructorSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-cache-handle") return;
        RuntimeTypeCacheHandleSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-method-generic-count") return;
        MethodGenericCountSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-interface-check") return;
        RuntimeTypeInterfaceCheckSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-virtual-slot-count") return;
        RuntimeVirtualSlotCountSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-method-slot-lookup") return;
        RuntimeMethodSlotLookupSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-ambiguous-match-factory") return;
        AmbiguousMatchFactorySubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-member-not-found-factory") return;
        MemberNotFoundFactorySubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-formal-declaring-method") return;
        FormalDeclaringMethodSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-szarray") return;
        RuntimeTypeIsSzArraySubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-type-instantiation") return;
        RuntimeTypeInstantiationSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-make-generic") return;
        RuntimeTypeMakeGenericSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-generic-position") return;
        GenericParameterPositionSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-contains-generic") return;
        ContainsGenericVariablesSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-constraint-array") return;
        GenericConstraintArraySubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-compound-handle") return;
        CompoundTypeHandleSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-array-type-handle") return;
        ArrayTypeHandleSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-primitive-element") return;
        PrimitiveElementTypeSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-primitive-widening") return;
        PrimitiveWideningSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-function-pointer") return;
        RuntimeTypeFunctionPointerSubset.FunctionPointerPredicateChecks.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-assembly") return;
        RuntimeTypeAssemblySubset.RuntimeAssemblyChecks.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-generic-variable") return;
        RuntimeTypeGenericVariableSubset.GenericVariablePredicateChecks.Run();
        if (args.Length > 0 && args[0] == "before-activator-default") return;
        ActivatorDefaultSubset.ActivatorChecks.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-default-ctor") return;
        RuntimeTypeDefaultCtorSubset.DefaultCtorChecks.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-create-instance-of-t") return;
        RuntimeTypeDefaultCtorSubset.DefaultCtorChecks.RunOfT();
        if (args.Length > 0 && args[0] == "before-runtime-type-default-struct") return;
        RuntimeTypeDefaultStructSubset.StructChecks.Run();
        if (args.Length > 0 && args[0] == "before-coreclr-activation-info") return;
        CoreClrActivationInfoSubset.ActivationChecks.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-handle-name") return;
        RuntimeTypeHandleNameSubset.NameChecks.Run();
        if (args.Length > 0 && args[0] == "before-runtime-type-declaring") return;
        RuntimeTypeDeclaringSubset.DeclaringChecks.Run();
        if (args.Length > 0 && args[0] == "before-cad-member") return;
        CustomAttributeDataMemberSubset.DataChecks.Run();
        if (args.Length > 0 && args[0] == "before-cad-constructor") return;
        CustomAttributeConstructorSubset.ConstructorChecks.Run();
        if (args.Length > 0 && args[0] == "before-cache-generic-definition") return;
        RuntimeTypeCacheGenericDefinitionSubset.DefinitionChecks.Run();
        if (args.Length > 0 && args[0] == "before-introduced-methods") return;
        RuntimeIntroducedMethodsSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-interfaces") return;
        RuntimeInterfacesSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-fields") return;
        RuntimeFieldsSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-static-generic-variables") return;
        StaticGenericVariablesSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-utf8-name") return;
        RuntimeUtf8NameSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-fnptr-arguments") return;
        FunctionPointerArgumentsSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-appcontext-exit") return;
        AppContextExitSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-assembly-execution") return;
        AssemblyExecutionSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-named-activation") return;
        NamedActivationSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-named-flags") return;
        NamedActivationFlagsSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-named-attrs") return;
        NamedActivationAttributesSubset.Program.Run();
        if (args.Length > 0 && args[0] == "before-path-activation") return;
        PathActivationSubset.Program.Run();
    }
}
