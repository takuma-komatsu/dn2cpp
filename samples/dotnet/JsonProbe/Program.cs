using System.Text.Json;
using System.Text.Json.Serialization;

// Source-gen JSON probe (real System.Text.Json via real IL).
// The reflection-based JsonSerializer.Deserialize<T> hits the permanent AOT
// carve-out (MakeGenericType on an ungenerated generic); the source-gen path
// (JsonSerializerContext) generates every JsonTypeInfo<T> + converter at compile
// time, so there is no runtime generic instantiation. The generated converters
// are still the real STJ IL (JsonMetadataServices / Utf8JsonReader), not a shim.
// dotnet <dll> output is the diff oracle for the eventual end-to-end JSON gate.

namespace JsonProbe;

public class Item
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("hash")] public long Hash { get; set; }
    [JsonPropertyName("ratio")] public double Ratio { get; set; }
    [JsonPropertyName("weight")] public float Weight { get; set; }
}

public class Root
{
    [JsonPropertyName("items")] public System.Collections.Generic.List<Item> Items { get; set; }
}

[JsonSerializable(typeof(Root))]
internal partial class ProbeCtx : JsonSerializerContext
{
}

public static class Program
{
    public static int Main()
    {
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        System.Globalization.CultureInfo.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

        const string json =
            "{\"items\":[{\"name\":\"a\",\"hash\":1," +
            "\"ratio\":1.25,\"weight\":-2.5}]}";
        Root root = JsonSerializer.Deserialize(json, ProbeCtx.Default.Root);
        Item item = root.Items[0];
        bool ratioMatches = System.BitConverter.DoubleToInt64Bits(item.Ratio) ==
            System.BitConverter.DoubleToInt64Bits(1.25);
        bool weightMatches = System.BitConverter.SingleToInt32Bits(item.Weight) ==
            System.BitConverter.SingleToInt32Bits(-2.5f);

        System.Console.WriteLine(item.Name + " " + item.Hash);
        System.Console.WriteLine("-- System.Text.Json floating point --");
        System.Console.WriteLine(ratioMatches);
        System.Console.WriteLine(weightMatches);
        return 0;
    }
}
