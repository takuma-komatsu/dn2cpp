using System;
using System.Globalization;
using System.Reflection;

namespace CollectibilitySubset
{
    public class Owner<T>
    {
        public T Field = default!;
        public T Property { get; set; } = default!;
        public T Echo(T value) => value;
    }

    public static class Program
    {
        private static void Check(string label, bool value)
        {
            Console.WriteLine("collectibility-" + label + ":" + value);
            if (!value) throw new InvalidOperationException(label);
        }

        public static void Run()
        {
            Console.WriteLine("== noncollectible reflection types ==");
            Type[] types = { typeof(int), typeof(Owner<>), typeof(Owner<int>), typeof(int[]),
                typeof(int).MakeByRefType(), typeof(int).MakePointerType(), typeof(Owner<>).GetGenericArguments()[0] };
            for (int i = 0; i < types.Length; i++)
                Check("type-" + i, !types[i].IsCollectible && !((MemberInfo)types[i]).IsCollectible);
            foreach (MemberInfo member in typeof(Owner<int>).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                Check("member-" + member.Name, !member.IsCollectible);
            Console.WriteLine("noncollectible reflection types end");
        }
    }
}

namespace Dn2Cpp.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class NativeImplementationAttribute : Attribute
    {
        public NativeImplementationAttribute(string module, string entryPoint) { }
    }
}

namespace CollectibilityImportRoutes
{
    public static class Program
    {
        // Rewritten artifacts run only natively; the fixture never invokes a CoreCLR QCall.
        public static int Direct() => throw new InvalidOperationException("collectibility fixture missing");
        public static int Pointer() => throw new InvalidOperationException("collectibility fixture missing");
        public static int Managed(nint ignored) => 73;
        public static MethodBase Reflected() => typeof(RuntimeTypeHandle).GetMethod("IsCollectible",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        private static void Check(string label, bool value)
        {
            Console.WriteLine("collectibility-import-" + label + ":" + value);
            if (!value) throw new InvalidOperationException(label);
        }

        private static void Refused(string label, Func<int> call)
        {
            try { call(); }
            catch (PlatformNotSupportedException error)
            {
                Check(label, error.Message.Contains("QCall!RuntimeTypeHandle_IsCollectible")
                    && error.Message.Contains("requires the CoreCLR runtime"));
                return;
            }
            throw new InvalidOperationException("raw QCall did not refuse: " + label);
        }

        public static void RunArtifactChecks(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            if (args.Length > 0 && args[0] == "native-binding")
            {
                Check("direct-binding", Direct() == 91);
                Check("pointer-binding", Pointer() == 91);
            }
            else
            {
                Refused("direct-refusal", Direct);
                Refused("pointer-refusal", Pointer);
            }
            // Intrinsic-owner metadata omits this private import; no reflective invocation reaches it.
            Check("reflection-row-unavailable", Reflected() is null);
            Console.WriteLine("collectibility import routes end");
        }
    }
}
