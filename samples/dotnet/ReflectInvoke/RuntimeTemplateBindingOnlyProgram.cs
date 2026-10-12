using System;
using System.Globalization;

namespace RuntimeTemplateBindingOnly;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "boundary")
        {
            ReflectDelegateSubset.Program.RunSignatureBoundaries();
            return;
        }
        if (args.Length > 0 && args[0] == "unsafe")
        {
            ReflectDelegateSubset.Program.RunUnsafeTemplateBindings();
            ReflectDelegateSubset.RuntimeBaseProjectionSubset.RunBoundaries();
            return;
        }
        if (args.Length > 0 && args[0] == "invoke")
        {
            ReflectDelegateSubset.Program.RunSignatureInvokeDescriptors();
            return;
        }
        if (args.Length > 0 && args[0] == "stripped")
        {
            object box = TrimReflectLib.Factory.MakeBox();
            string value = box.ToString()!;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    var target = (Func<string>)Delegate.CreateDelegate(typeof(Func<string>), box, box.GetType().GetMethod("ToString")!);
                    Console.WriteLine("template stripped binding => " + (target() == value));
                }
                catch (PlatformNotSupportedException)
                {
                    Console.WriteLine("template stripped binding => PlatformNotSupportedException");
                }
            }
            return;
        }
        ReflectDelegateSubset.Program.RunRuntimeTemplateBindings();
        if (args.Length > 0 && args[0] == "before-base-projections")
            return;
        ReflectDelegateSubset.RuntimeBaseProjectionSubset.Run();
    }
}
