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
        }
    }
}
