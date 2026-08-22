using System;
using System.Globalization;

namespace ReflectTypes
{
    // Auto-merged gate driver: runs each consolidated sample's Run() in
    // order. Each section keeps its own namespace so reflected type names
    // and other namespace-sensitive output stay identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            // Host-culture pin. This bucket's expectation is a FIXED snapshot while the
            // transpiled binary resolves the HOST's culture, so any culture-sensitive line
            // below would be wrong off the machine the snapshot was taken on. Pinned once
            // here rather than per call site: the mechanism is number FORMATTING as much as
            // parsing, so there is no small set of sites to annotate.
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            // CurrentUICulture is the SECOND reader of the host: it selects the resource
            // set, moving a DisplayName, a TimeZoneInfo.StandardName and an LCID without
            // touching a separator. dn2cpp folds it to invariant unconditionally, so this
            // line pins only the oracle.
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            ReflectTypeSubset.Program.Run();
            ReflectArrayTypeSubset.Program.Run();
            ArrayAssignabilitySubset.Program.Run();
            ConvertibleAssignabilitySubset.Program.Run();
            ReflectEnumTypeSubset.Program.Run();
            ReflectGenericSubset.Program.Run();
            ReflectNestedSubset.Program.Run();
            ReflectNullableSubset.Program.Run();
            ReflectFieldSubset.Program.Run();
            ReflectPropSubset.Program.Run();
            ReflectMethodSubset.Program.Run();
            ReflectCtorSubset.Program.Run();
            ReflectAttrSubset.Program.Run();
            ReflectAttrEnumerableSubset.Program.Run();
            ReflectGenericAttrSubset.Program.Run();
            ReflectAttrBaseSubset.Program.Run();
            ReflectAttrNamedBaseSubset.Program.Run();
            ReflectFrameworkAttrSubset.Program.Run();
            ReflectEnumSubset.Program.Run();
            ReflectEnumValuesSubset.Program.Run();
            ReflectAsmAttrSubset.Program.Run();
            ReflectAssemblySubset.Program.Run();
            ReflectLongtailSubset.Program.Run();
            ReflectLookupSubset.Program.Run();
            ReflectGenericMethodSubset.Program.Run();
            ReflectMakeGenericSeedSubset.Program.Run();
            ReflectSerializerWrapperSeedSubset.Program.Run();
            ReflectIndexerEdgeSubset.Program.Run();
            DynamicCodegenSubset.Program.Run();
            VoidIdentitySubset.Program.Run();
            StackTraceSubset.Program.Run();
            StackTraceThrowSubset.Program.Run();
            EventSourceSubset.Program.Run();
            VariantAssignabilitySubset.Program.Run();
            // Every section below is appended at the tail, in the order the frozen
            // expectation grew: a new section must leave the bucket's previous output an
            // unchanged PREFIX of the new one, which is what catches a section perturbing
            // one that ran before it.
            GetTypeSubset.Program.Run();
            TypeCategorySubset.Program.Run();
            TypeNameRegistrySubset.Program.Run();
            TypePatternSubset.Program.Run();
            NestedNameSubset.Program.Run();
            ReflectUnmodeledLayoutSubset.Program.Run();
            ReflectMarshalVerdictSubset.Program.Run();
            ReflectShallowCloneRefusalSubset.Program.Run();
            TypeIdentitySubset.Program.Run();
            AssemblyIdentitySubset.Program.Run();
            BoundHandleSubset.Program.Run();
            RuntimeArrayIdentitySubset.Program.Run();
            GenericTypeNameSubset.Program.Run();
            BoundHandleResidueSubset.Program.Run();
            IntrinsicTypeNameSubset.Program.Run();
            TypeRegistryMouthsSubset.Program.Run();
            DelegateBaseTypeSubset.Program.Run();
            ReflectRuntimeInstantiationSubset.Program.Run();
            GenericDefAssignabilitySubset.Program.Run();
            ReflectIntrinsicTemplateSubset.Program.Run();
        }
    }
}
