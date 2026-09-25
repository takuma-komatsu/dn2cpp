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
            if (Environment.GetEnvironmentVariable("DN2CPP_BEFORE_VIRTUAL_INVOKE") == "1")
                return;
            ReflectVirtualInvokeSubset.Program.Run();
        }
    }
}
