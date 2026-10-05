using System;
using System.Globalization;

namespace Dn2Cpp.Gates;

internal static class Program
{
    private static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
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
