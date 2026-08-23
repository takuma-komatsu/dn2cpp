using System.Globalization;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace MessagePackSample;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        IFormatterResolver resolver = CompositeResolver.Create(
            new IMessagePackFormatter[] { new MessagePackResolverSubset.WrappedIntFormatter() },
            new IFormatterResolver[]
            {
                global::MessagePack.GeneratedMessagePackResolver.Instance,
                BuiltinResolver.Instance,
            });
        MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard.WithResolver(resolver);

        MessagePackObjectSubset.Program.__GateEntry(options);
        MessagePackCollectionsSubset.Program.__GateEntry(options);
        MessagePackUnionSubset.Program.__GateEntry(options);
        MessagePackResolverSubset.Program.__GateEntry(options);
    }
}
