using System;
using System.Globalization;

namespace SharedGenerics
{
    // Gate driver. Each section keeps its own namespace so reflected type names
    // stay identical to a standalone program. The whole program is transpiled
    // WITH --shared-generics, so every section runs against canonical shared
    // bodies (or their per-method monomorphic fallbacks) and must still match
    // real .NET exactly.
    internal static class Program
    {
        private static void Main()
        {
            // Pin both cultures first: gate output must not depend on the host locale (see AGENTS.md).
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            EnumDictSubset.Program.__GateEntry();
            EnumHashSetSubset.Program.__GateEntry();
            EnumWidthSubset.Program.__GateEntry();
            GenericStaticsSubset.Program.__GateEntry();
            TypeIdentitySubset.Program.__GateEntry();
            SortedEnumSubset.Program.__GateEntry();
            EnumEnumeratorSubset.Program.__GateEntry();
            RgctxSubset.Program.__GateEntry();
            DictSharedSubset.Program.__GateEntry();
            StringKeyDictSubset.Program.__GateEntry();
            RefListSubset.Program.__GateEntry();
            RefEnumDispatchSubset.Program.__GateEntry();
            TypeofLeakSubset.Program.__GateEntry();
            TypeofFoldSubset.Program.__GateEntry();
            AliasCollisionSubset.Program.__GateEntry();
            MethodShareSubset.Program.__GateEntry();
            TaskRgctxSubset.Program.__GateEntry();
            SettingWrapperSubset.Program.__GateEntry();
            PrimitiveNameShadowSubset.Program.__GateEntry();
            MangleKindShadowSubset.Program.__GateEntry();
            GenericMethodSubset.Program.__GateEntry();
            GvmCanonicalSubset.Program.__GateEntry();
        }
    }
}
