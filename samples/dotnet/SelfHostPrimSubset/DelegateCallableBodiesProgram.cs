using System;
using System.Globalization;

namespace DelegateCallableBodies;

internal static class Program
{
    internal static int Route;

    internal static void Run(string route, bool refusals)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        Route = route == "call" ? 1 : route == "calli" ? 2 : route == "callvirt" ? 3 : route == "ldvirtftn" ? 4 : 0;
        if (refusals)
        {
            MulticastFactorySubset.Program.RunNativeBoundary();
            MulticastNamedConstructorSubset.Program.RunNativeBoundary();
            MulticastVmConstructorSubset.Program.RunNativeBoundary();
            return;
        }
        DelegateValueSubset.Program.Run();
        MulticastAccessorSubset.Program.Run();
        MulticastFactorySubset.Program.Run();
        MulticastOperationsSubset.Program.Run();
        MulticastNamedConstructorSubset.Program.Run();
        MulticastNamedConstructorSubset.Program.RunRebind();
        MulticastVmConstructorSubset.Program.Run();
        Console.WriteLine("Delegate callable bodies end");
        if (route == "before-virtual-delegates")
            return;
        DelegateValueSubset.Program.RunOwnerContracts();
        DelegateValueSubset.Program.RunVirtualBindings();
        DelegateValueSubset.Program.RunVirtualBoundaries();
    }
}
