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
        private static void Main(string[] args)
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
            GenericStaticsSubset.Program.SynchronizedPrologues();
            if (args.Length > 0 && args[0] == "legacy")
                return;
            GenericStaticsSubset.Program.ConcreteAndDependentStatics();
            if (args.Length > 0 && args[0] == "before-forwarding")
                return;
            RgctxForwardingSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-gvm-hider")
                return;
            GvmHiderSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-width-join")
                return;
            WidthJoinSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-default-comparison")
                return;
            Console.WriteLine("== default comparison validation ==");
            ConstrainedObjectInterfaceSubset.Program.Run();
            Console.WriteLine("default comparison validation end");
            if (args.Length > 0 && args[0] == "before-ordinary-interface-leaves")
                return;
            Console.WriteLine("== ordinary interface and virtual definitions ==");
            VirtualHiderSubset.Program.Run();
            VirtualHiderSubset.Program.RunSubstitutionCollisions();
            VirtualHiderSubset.Program.RunInterfaceCollisions();
            VirtualHiderSubset.Program.RunCrossLevelCollisions();
            VirtualHiderSubset.Program.RunDefinitionMirrors();
            SharedForEachSubset.Program.Run();
            Console.WriteLine("ordinary interface and virtual definitions end");
            if (args.Length > 0 && args[0] == "before-self-nesting")
                return;
            RgctxSubset.Program.RunSelfNesting();
            if (args.Length > 0 && args[0] == "before-methodimpl-shapes")
                return;
            MethodImplShapeSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-null-stack-joins")
                return;
            KnownNullJoinSubset.Program.Run();
            if (args.Length > 0 && args[0] == "before-boolean-receivers")
                return;
            ConstrainedObjectInterfaceSubset.Program.RunBooleanReceivers();
            if (args.Length > 0 && args[0] == "before-width-typed-slots")
                return;
            WidthJoinSubset.Program.RunTypedSlots();

            if (args.Length > 0 && args[0] == "before-substitution-collisions")
                return;
            Console.WriteLine("== generic virtual substitution collisions ==");
            GvmHiderSubset.Program.RunSubstitutionCollisions();
            GvmHiderSubset.Program.RunCrossLevelCollisions();
            Console.WriteLine("generic virtual substitution collisions end");
            if (args.Length > 0 && args[0] == "before-variant-dispatch")
                return;
            Console.WriteLine("== variant interface dispatch ==");
            GvmHiderSubset.Program.RunVariantInterfaces();
            Console.WriteLine("variant interface dispatch end");
            if (args.Length > 0 && args[0] == "before-trial-dispatch")
                return;
            GvmCanonicalSubset.Program.RunTrialDispatch();
            if (args.Length > 0 && args[0] == "before-interface-rows")
                return;
            GvmCanonicalSubset.Program.RunInterfaceRows();
        }
    }
}
