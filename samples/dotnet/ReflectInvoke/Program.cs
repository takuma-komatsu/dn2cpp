using System;
using System.Globalization;

namespace ReflectInvoke
{
    // Gate driver: runs each section's Run() in order. Each section keeps its
    // own namespace — reflected type names are namespace-sensitive.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (Environment.GetEnvironmentVariable("DN2CPP_REFLECTION_MEASURE") == "1")
            {
                ReflectMetadataMeasureSubset.Program.Run();
                return;
            }

            ReflectInvokeSubset.Program.Run();
            ReflectDispatchSubset.Program.Run();
            ReflectFieldValueSubset.Program.Run();
            ReflectSerializerSubset.Program.Run();
            ReflectGenericMethodSubset.Program.Run();
            ReflectDelegateSubset.Program.Run();
            ReflectActivatorSubset.Program.Run();
            ReflectActivatorGenericSubset.Program.Run();
            ReflectBclCtorSubset.Program.Run();
            ReflectEnumFieldSubset.Program.Run();
            ReflectMemberIdentitySubset.Program.Run();
            ReflectIntrinsicSizeOfSubset.Program.Run();
            DateTimeLayoutSubset.Program.Run();
            TypePredicateFoldSubset.Program.Run();
            ReflectNullHandleSubset.Program.Run();
            ActivatorSubset.Program.Run();
            EventSubset.Program.Run();
            MemberwiseCloneSubset.Program.Run();
            GetInterfaceSubset.Program.Run();
            ReflectedTypeSubset.Program.Run();
            ReflectToStringSubset.Program.Run();
            ReflectMetadataPreservationSubset.Program.Run();
            ReflectMetadataLayoutSubset.Program.Run();
            ReflectMetadataCompressionSubset.Program.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_EXISTING_CONSTRUCTOR") == "1")
                return;
            ReflectExistingConstructorSubset.Program.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_COLD_ACTIVATOR") == "1")
                return;
            ReflectActivatorGenericSubset.ColdGenericFactory.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_DELEGATE_METHOD") == "1")
                return;
            if (!ReflectDelegateIdentitySubset.Program.Run())
                return;
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_LDFTN_LOCAL") == "1")
                return;
            LdftnLocalSubset.Program.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_INVOKE_VALIDATION") == "1")
                return;
            ReflectInvokeValidationSubset.Program.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RUNTIME_HANDLE_RELATIONS") == "1")
                return;
            RuntimeHandleRelationSubset.Program.Run();
            NullDelegateTargetSubset.Program.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_EMPTY_STRING_CLONE") == "1")
                return;
            MemberwiseCloneSubset.Program.RunEmptyStrings();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_DELEGATE_LISTS") == "1")
                return;
            DelegateInvocationListSubset.Program.RunRemoveRuns();
            DelegateInvocationListSubset.Program.Run();
            DelegateInvocationListSubset.Program.RunEntryIdentity();
            DelegateInvocationListSubset.Program.RunEnumeration();
            DelegateInvocationListSubset.Program.RunEnumerationScale();
            DelegateInvokerDeclarationSubset.Program.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RECURSIVE_DELEGATE") == "1")
                return;
            DelegateInvokerDeclarationSubset.Program.RunRecursive();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_ORDINARY_IL_INTERFACE") == "1")
                return;
            Console.WriteLine("== ordinary interface and ValueType IL ==");
            LdftnLocalSubset.Program.RunSealedInterface();
            LdftnLocalSubset.Program.RunValueTypeReceivers();
            TypePredicateFoldSubset.Program.RunValueTypeFolds();
            Console.WriteLine("ordinary interface and ValueType IL end");
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_OBJECT_METHODIMPL") == "1")
                return;
            LdftnLocalSubset.Program.RunObjectMethodImpl();

            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_REFLECTION_DISPATCH") == "1")
                return;
            Console.WriteLine("== reflection dispatch extensions ==");
            ReflectVirtualInvokeSubset.Program.Run();
            ReflectVirtualInvokeSubset.Program.RunGenericVirtual();
            ReflectVirtualInvokeSubset.Program.RunReflectedOnly();
            ReflectWideHierarchySubset.Program.Run();
            ReflectBoundDelegateSubset.Program.Run();
            ReflectVirtualInvokeSubset.Program.RunDefinitionSignatures();
            ReflectVirtualInvokeSubset.Program.RunRepeatedCalls();
            ReflectDelegateIdentitySubset.Program.RunObjectVirtual();
            ReflectBoundDelegateSubset.Program.RunNullBoundBodies();
            ReflectInvokeValidationSubset.Program.RunPlannedChecks();
            ReflectBoundDelegateSubset.Program.RunRemoveRuns();
            ReflectVirtualInvokeSubset.Program.RunCrossLevelDefinitions();
            ReflectRouteClassSubset.Program.Run();
            ReflectBoundDelegateSubset.Program.RunNullBoundCalls();
            ReflectInvokeValidationSubset.Program.RunByRefArguments();
            ReflectVirtualInvokeSubset.Program.RunSameLevelDefinitions();
            ReflectRouteClassSubset.Program.RunDeepMinted();
            ReflectBoundDelegateSubset.Program.RunNullBoundLookups();
            ReflectRouteClassSubset.Program.RunShallowFirst();
            ReflectRouteClassSubset.Program.RunWrittenBack();
            ReflectVirtualInvokeSubset.Program.RunObjectEqualsArguments();
            DelegateInvocationListSubset.Program.RunDynamicInvoke();
            ReflectBoundDelegateSubset.Program.RunNullBoundOtherReceivers();
            ReflectRouteClassSubset.Program.RunSlotRows();
            ReflectBoundDelegateSubset.Program.RunNullBoundSharedContext();
            ReflectInvokeValidationSubset.Program.RunInvokePlanReuse();
            ReflectRouteClassSubset.Program.RunWrittenBackPairs();
            Console.WriteLine("reflection dispatch extensions end");

            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_ATTRIBUTE_MINTED") == "1")
                return;
            ReflectRouteClassSubset.Program.RunAttributeMinted();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_TEMPLATE_ACCESSORS") == "1")
                return;
            ReflectBoundDelegateSubset.Program.RunTemplateAccessors();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_POINTER_RETURNS") == "1")
                return;
            ReflectInvokeValidationSubset.Program.RunPointerReturns();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_DELEGATE_INVOKE_TARGETS") == "1")
                return;
            DelegateInvokeTargetSubset.Program.Run();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_NULL_BOUND_CHAINS") == "1")
                return;
            ReflectBoundDelegateSubset.Program.RunNullBoundChains();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RENAMED_SLOT_BINDINGS") == "1")
                return;
            LdftnLocalSubset.Program.RunRenamedSlotBindings();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_SETTLED_OBJECT_VIRTUAL") == "1")
                return;
            ReflectDelegateIdentitySubset.Program.RunSettledObjectVirtual();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_RENAMED_SLOT_FILLERS") == "1")
                return;
            LdftnLocalSubset.Program.RunRenamedSlotFillers();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_TYPEDEF_MEMBERREF") == "1")
                return;
            LdftnLocalSubset.Program.RunTypeDefMemberRefs();
        }
    }
}
