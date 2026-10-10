using System;
using System.Globalization;

namespace ReflectInvoke
{
    // Gate driver: runs each section's Run() in order. Each section keeps its
    // own namespace — reflected type names are namespace-sensitive.
    internal static class Program
    {
        private static void Main(string[] args)
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length > 0 && args[0] == "delegate-name-boundary-outcomes")
            {
                ReflectDelegateSubset.Program.RunNamedBoundaries();
                return;
            }

            if (args.Length > 0 && args[0] == "delegate-signature-boundary-outcomes")
            {
                ReflectDelegateSubset.Program.RunSignatureBoundaries();
                return;
            }

            if (args.Length > 0 && args[0] == "delegate-signature-before-ordinary-boundary")
            {
                ReflectDelegateSubset.Program.RunSignatureBoundaries(false, false);
                return;
            }

            if (args.Length > 0 && args[0] == "delegate-signature-before-overload-boundary")
            {
                ReflectDelegateSubset.Program.RunSignatureBoundaries(true, false);
                return;
            }

            if (args.Length > 0 && args[0] == "delegate-signature-before-shape-boundary")
            {
                ReflectDelegateSubset.Program.RunSignatureBoundaries(true, true, false);
                return;
            }

            if (args.Length > 0 && args[0] == "delegate-signature-before-runtime-argument-boundary")
            {
                ReflectDelegateSubset.Program.RunSignatureBoundaries(true, true, true, true, true, false);
                return;
            }

            if (args.Length > 0 && args[0] == "delegate-signature-before-identity-boundary")
            {
                ReflectDelegateSubset.Program.RunSignatureBoundaries(true, true, true, true, false);
                return;
            }

            if (args.Length > 0 && args[0] == "delegate-signature-before-leaf-boundary")
            {
                ReflectDelegateSubset.Program.RunSignatureBoundaries(true, true, true, false);
                return;
            }

            if (args.Length > 0 && args[0] == "generic-method-boundary-outcomes")
            {
                ReflectGenericMethodSubset.Program.RunDefinitionBoundaries();
                return;
            }
            if (args.Length > 0 && args[0] == "formal-type-boundary-outcomes")
            {
                ReflectGenericMethodSubset.Program.RunFormalTypeBoundaries();
                return;
            }
            if (args.Length > 0 && args[0] == "generic-signature-boundary-outcomes")
            {
                ReflectGenericMethodSubset.Program.RunSignatureBoundaries();
                return;
            }

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
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_PRIMITIVE_BINDER") == "1")
                return;
            ReflectActivatorSubset.Program.RunPrimitiveBinder();
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_OPTIONAL_ARGUMENTS") == "1")
                return;
            ReflectInvokeValidationSubset.Program.RunOptionalArguments();
            if (args.Length > 0 && args[0] == "before-delegate-origin-boundaries")
                return;
            LdftnLocalSubset.Program.RunOriginBoundaries();
            if (args.Length > 0 && args[0] == "before-runtime-type-relations")
                return;
            RuntimeHandleRelationSubset.Program.RunRuntimeTypeRelations();
            if (args.Length > 0 && args[0] == "before-generic-method-definitions")
                return;
            ReflectGenericMethodSubset.Program.RunDefinitionLookups();
            if (args.Length > 0 && args[0] == "before-formal-method-parameters")
                return;
            ReflectGenericMethodSubset.Program.RunFormalParameters();
            if (args.Length > 0 && args[0] == "before-mixed-generic-definitions")
                return;
            ReflectGenericMethodSubset.Program.RunMixedDefinitionLookups();
            if (args.Length > 0 && args[0] == "before-formal-reflected-owners")
                return;
            ReflectGenericMethodSubset.Program.RunFormalReflectedOwners();
            if (args.Length > 0 && args[0] == "before-formal-classification")
                return;
            ReflectGenericMethodSubset.Program.RunFormalClassification();
            if (args.Length > 0 && args[0] == "before-metadata-formal-attributes")
                return;
            ReflectGenericMethodSubset.Program.RunMetadataFormalAttributes();
            if (args.Length > 0 && args[0] == "before-formal-member-types")
                return;
            ReflectGenericMethodSubset.Program.RunFormalMemberTypes();
            if (args.Length > 0 && args[0] == "before-definition-signature-closure")
                return;
            ReflectGenericMethodSubset.Program.RunDefinitionSignatureClosure();
            if (args.Length > 0 && args[0] == "before-delegate-name-bindings")
                return;
            ReflectDelegateSubset.Program.RunNamedBindings();
            if (args.Length > 0 && args[0] == "before-delegate-signature-bindings")
                return;
            ReflectDelegateSubset.Program.RunSignatureBindings();
            if (args.Length > 0 && args[0] == "before-runtime-function-pointer-invoke")
                return;
            ReflectDelegateSubset.Program.RunSignatureInvokeDescriptors();
            if (args.Length > 0 && args[0] == "before-unsupported-referent-signatures")
                return;
            ReflectDelegateSubset.Program.RunOpaqueRefSignatures();
            if (args.Length > 0 && args[0] == "before-ordinary-template-signatures")
                return;
            ReflectDelegateSubset.Program.RunOrdinaryTemplateSignatures();
            if (args.Length > 0 && args[0] == "before-ordinary-overload-selection")
                return;
            ReflectDelegateSubset.Program.RunOrdinaryOverloads();
            if (args.Length > 0 && args[0] == "before-shape-overload-selection")
                return;
            ReflectDelegateSubset.Program.RunShapeOverloads();
            if (args.Length > 0 && args[0] == "before-leaf-overload-selection")
                return;
            ReflectDelegateSubset.Program.RunLeafOverloads();
            if (args.Length > 0 && args[0] == "before-identity-overload-selection")
                return;
            ReflectDelegateSubset.Program.RunIdentityOverloads();
            if (args.Length > 0 && args[0] == "before-runtime-argument-overloads")
                return;
            ReflectDelegateSubset.Program.RunRuntimeArgumentOverloads();
            if (args.Length > 0 && args[0] == "before-family-type-overloads")
                return;
            ReflectDelegateSubset.Program.RunFamilyTypeOverloads();
            if (args.Length > 0 && args[0] == "before-enum-signature-bindings")
                return;
            ReflectDelegateEnumSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-intrinsic-pointer-bindings")
                return;
            ReflectIntrinsicPointerSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-intrinsic-pointer-overloads")
                return;
            ReflectIntrinsicPointerSubset.Program.RunOverloads();
            if (args.Length > 0 && args[0] == "before-intrinsic-pointer-families")
                return;
            ReflectIntrinsicPointerSubset.Program.RunFamilies();
            if (args.Length > 0 && args[0] == "before-intrinsic-generic-pointers")
                return;
            ReflectIntrinsicPointerSubset.Program.RunGenericPointees();
            if (args.Length > 0 && args[0] == "before-intrinsic-array-arguments")
                return;
            ReflectIntrinsicPointerSubset.Program.RunArrayArguments();
            if (args.Length > 0 && args[0] == "before-intrinsic-constant-arguments")
                return;
            ReflectIntrinsicPointerSubset.Program.RunConstantArguments();
            if (args.Length > 0 && args[0] == "before-intrinsic-array-children")
                return;
            ReflectIntrinsicPointerSubset.Program.RunArrayChildren();
            if (args.Length > 0 && args[0] == "before-intrinsic-MD-array-children")
                return;
            ReflectIntrinsicPointerSubset.Program.RunMdArrayChildren();
            if (args.Length > 0 && args[0] == "before-delegate-constructor-names")
                return;
            ReflectDelegateConstructorSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-virtual-delegate-identity")
                return;
            DelegateVirtualIdentitySubset.Program.Run();
            DelegateVirtualIdentitySubset.Program.RunReflected();
            if (args.Length > 0 && args[0] == "before-runtime-owned-method-groups")
                return;
            DelegateVirtualIdentitySubset.Program.RunRuntimeOwned();
            if (args.Length > 0 && args[0] == "before-reflection-signature-types")
                return;
            ReflectionSignatureTypesSubset.Program.Run();
        }
    }
}
