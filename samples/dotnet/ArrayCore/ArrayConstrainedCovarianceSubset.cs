using System;

namespace ArrayConstrainedCovarianceSubset;

internal static class Program
{
    private static void Copy(object[] source, object[] destination)
    {
        Array.ConstrainedCopy(source, 0, destination, 0, 1);
    }

    internal static void Run()
    {
        object[] invalidSource = { new object() };
        string[] invalidDestination = { "old" };
        try
        {
            Copy(invalidSource, invalidDestination);
            Console.WriteLine("constrained-static-covariant-refuse: copied");
        }
        catch (Exception ex)
        {
            Console.WriteLine("constrained-static-covariant-refuse: " + ex.GetType().Name);
        }
        Console.WriteLine("constrained-static-covariant-preserved: " + invalidDestination[0]);

        object[] sameSource = new string[] { "same" };
        object[] sameDestination = new string[] { "old" };
        Copy(sameSource, sameDestination);
        Console.WriteLine("constrained-static-covariant-same: " + sameDestination[0]);

        object[] upcastSource = new string[] { "new" };
        object[] upcastDestination = { "old" };
        Copy(upcastSource, upcastDestination);
        Console.WriteLine("constrained-static-covariant-upcast: " + upcastDestination[0]);
    }
}
