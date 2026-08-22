using System.Globalization;
using MemoryPack;
using MemoryPack.Formatters;

namespace MemoryPackSample
{
    internal static class Program
    {
        private static void Main()
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            // A shape no [MemoryPackable] type has as a member reaches the provider's
            // reflection fallback (MakeGenericType + Activator), which no AOT target can
            // serve. Registering the closed formatter is the documented AOT path and the
            // IL that names the instantiation.
            MemoryPackFormatterProvider.Register(new ListFormatter<int>());

            MemoryPackObjectSubset.Program.__GateEntry();
            MemoryPackCollectionsSubset.Program.__GateEntry();
            MemoryPackUnionSubset.Program.__GateEntry();
            MemoryPackLayoutSubset.Program.__GateEntry();
        }
    }
}
