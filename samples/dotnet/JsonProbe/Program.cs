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
        Root r = JsonSerializer.Deserialize("{\"items\":[{\"name\":\"a\",\"hash\":1}]}", ProbeCtx.Default.Root);
        System.Console.WriteLine(r.Items[0].Name + " " + r.Items[0].Hash);
        return 0;
    }
}
