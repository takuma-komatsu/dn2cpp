using System;
using System.Globalization;
using System.Reflection;
using HotUpdateBase;

namespace Dn2Cpp.Gates;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        if (args.Length > 0 && args[0] == "--type-getter-signature")
        {
            object receiver = new QuotaEx("oracle", 1);
            MethodInfo method = typeof(object).GetMethod(nameof(object.GetType))!;
            var getter = (TypeGetter)Delegate.CreateDelegate(typeof(TypeGetter), receiver, method)!;
            Console.WriteLine("== intrinsic Type getter signature ==");
            Console.WriteLine("type getter captured");
            Console.WriteLine("type getter result=" + getter().FullName);
            Console.WriteLine("intrinsic Type getter signature end");
            Delegate? incompatible = Delegate.CreateDelegate(typeof(CloneThunk), receiver, method,
                throwOnBindFailure: false);
            Console.WriteLine("clone getter nonthrowing=" + (incompatible is null));
            try
            {
                Delegate.CreateDelegate(typeof(CloneThunk), receiver, method, throwOnBindFailure: true);
                Console.WriteLine("clone getter throwing=accepted");
            }
            catch (ArgumentException error)
            {
                Console.WriteLine("clone getter throwing=" + error.GetType().Name);
            }
            Console.WriteLine("type getter signature oracle end");
            return;
        }
        if (args.Length > 0 && args[0] == "--derived-aggregate")
        {
            HotUpdateBase.DerivedAggregateSubset.Run();
            Console.WriteLine(HotUpdatePatch.InterpretedAggregateSubset.Run());
            return;
        }
        if (args.Length > 0 && args[0] == "--aggregate-collection")
        {
            Console.WriteLine(HotUpdateCoreLibPatch.InterpretedAggregateCollectionSubset.Run());
            if (args.Length > 1 && args[1] == "before-aggregate-message")
                return;
            Console.WriteLine(HotUpdateCoreLibPatch.InterpretedAggregateCollectionSubset.RunMessageDispatch());
            if (args.Length > 1 && args[1] == "before-ordinary-exception-message")
                return;
            Console.WriteLine(HotUpdateCoreLibPatch.InterpretedAggregateCollectionSubset.RunOrdinaryMessageDispatch());
            return;
        }
        Console.WriteLine(HotUpdatePatch.InterpretedConcatSubset.Run());
    }
}
