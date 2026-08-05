using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utf8JsonProbe;

public sealed class M
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

[JsonSerializable(typeof(M))]
internal partial class Ctx : JsonSerializerContext { }

public static class Program
{
    public static int Main()
    {
        // One non-ASCII char, the only multi-byte one in the real
        // extension_api.json: STJ transcodes UTF-16 input to UTF-8 before parsing.
        string json = "{\"name\":\"a•b\"}";
        M m = JsonSerializer.Deserialize(json, Ctx.Default.M);
        System.Console.WriteLine("len=" + m.Name.Length + " c1=U+" + ((int)m.Name[1]).ToString("X4"));
        return 0;
    }
}
