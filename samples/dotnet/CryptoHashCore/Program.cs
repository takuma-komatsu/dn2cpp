using System;

namespace CryptoHashCore
{
    // Auto-merged gate driver: runs each consolidated sample's __GateEntry()
    // in order. Each section keeps its own namespace so namespace-sensitive
    // output stays identical to the originals.
    internal static class Program
    {
        private static void Main()
        {
            System.Globalization.CultureInfo.CurrentCulture =
                System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.CurrentUICulture =
                System.Globalization.CultureInfo.InvariantCulture;

            HashOneShotSubset.Program.__GateEntry();
            HashInstanceSubset.Program.__GateEntry();
            HashIncrementalSubset.Program.__GateEntry();
            HashStreamSubset.Program.__GateEntry();
            HmacSubset.Program.__GateEntry();
            AchievementKeyHmacSubset.Program.__GateEntry();
            RandomBytesSubset.Program.__GateEntry();
        }
    }
}
