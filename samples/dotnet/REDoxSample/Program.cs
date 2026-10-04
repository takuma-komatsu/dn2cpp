using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using REDox;
using REDox.Json;
using REDox.Serialization;

namespace Dn2Cpp;

internal static class Program
{
    private static void Main()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

        var settings = new DoxSerializerSettings { AllowDynamicGenericConverters = false };
        ParseAndEdit(settings);
        Json5(settings);
        try { Serialize(settings); }
        catch (Exception e)
        {
            for (Exception? inner = e; inner is not null; inner = inner.InnerException)
                Console.Error.WriteLine(inner.GetType().FullName + ": " + inner.Message);
            throw;
        }
        Scalars(settings);
        CustomConverter();
        Console.WriteLine("REDox end");
    }

    private static void ParseAndEdit(DoxSerializerSettings settings)
    {
        Console.WriteLine("== REDox JSON DOM ==");
        const string json = "{\"Name\":\"Leon\\n\\u65e5\\ud83d\\ude00\",\"Level\":42,\"Items\":[\"Handgun\",\"Green Herb\"],\"alive\":true,\"nil\":null,\"min\":-9223372036854775808,\"max\":18446744073709551615,\"fraction\":-1.25e2}";
        using var doc = JsonDocument.Parse(json, settings,
            new JsonDocumentOptions { EnableValueValidation = true });
        var root = doc.RootElement;
        Console.WriteLine("name=" + root.GetProperty("Name").GetString()!.Replace("\n", "|"));
        Console.WriteLine("numbers=" + root.GetProperty("min").GetInt64() + ":" +
            root.GetProperty("max").GetUInt64() + ":" + root.GetProperty("fraction").GetDouble());
        Console.WriteLine("alive=" + root.GetProperty("alive").GetBoolean() +
            " nil=" + root.GetProperty("nil").Token.Kind);

        var obj = root.AsObject();
        obj["Name"] = "Claire 日本😀";
        obj.Add("Hp", 100);
        Console.WriteLine("removed=" + obj.Remove("Level") + ":" + obj.Remove("absent"));
        var items = obj["Items"].AsArray();
        items.Add("First Aid Spray");
        items.Insert(0, "Knife");
        items.RemoveAt(1);
        Console.WriteLine("edited=" + doc.RootElement.ToJsonString());

        var clone = doc.RootElement.Clone();
        var cloneObject = clone.AsObject();
        cloneObject["Name"] = "Ada";
        Console.WriteLine("clone=" + cloneObject["Name"].AsElement().GetString() +
            " original=" + doc.RootElement.GetProperty("Name").GetString());

        byte[] encoded = DoxDocument.Encode(doc.RootElement);
        using var binary = DoxDocument.Parse(encoded, settings);
        Console.WriteLine("dox=" + binary.RootElement.ToJsonString());
        using var utf8 = JsonDocument.Parse(Encoding.UTF8.GetBytes(json), settings);
        Console.WriteLine("utf8=" + utf8.RootElement.GetProperty("Name").GetString()!.Replace("\n", "|"));
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var streamed = JsonDocument.Parse(stream, settings);
        Console.WriteLine("stream items=" + streamed.RootElement.GetProperty("Items").GetArrayLength());
        Console.WriteLine("JSON DOM end");
    }

    private static void Json5(DoxSerializerSettings settings)
    {
        Console.WriteLine("== REDox JSON5 ==");
        const string text = "{ /* inventory */ name: 'Leon', count: 0x2a, items: ['Knife',], enabled: true, }";
        using var doc = Json5Document.Parse(text, settings,
            new Json5DocumentOptions { PreserveTrivia = true, EnableValueValidation = true });
        Console.WriteLine("hex=" + doc.RootElement.GetProperty("count").GetInt32());
        doc.RootElement.AsObject()["name"] = "Claire";
        string edited = Json5Document.EncodeToString(doc.RootElement,
            new Json5WriteOptions { PreserveTrivia = true });
        Console.WriteLine("trivia=" + edited.Contains("/* inventory */", StringComparison.Ordinal));
        using var reparsed = Json5Document.Parse(edited, settings);
        Console.WriteLine("json5=" + reparsed.RootElement.ToJsonString());
        Console.WriteLine("JSON5 end");
    }

    private static void Serialize(DoxSerializerSettings settings)
    {
        Console.WriteLine("== REDox AOT serialization ==");
        var player = new Player { Name = "Leon 日本😀", Level = 42, Items = new[] { "Knife", "Green Herb" } };
        string json = JsonSerializer.Serialize(player, settings);
        Console.WriteLine("player=" + json);
        var restored = JsonSerializer.Deserialize<Player>(json, settings)!;
        PrintPlayer("json", restored);
        byte[] dox = DoxSerializer.Serialize(player, settings);
        PrintPlayer("dox", DoxSerializer.Deserialize<Player>(dox, settings)!);
        var list = JsonSerializer.Deserialize<List<int>>("[3,-1,42]", settings)!;
        Console.WriteLine("list=" + JsonSerializer.Serialize(list, settings));
        var map = JsonSerializer.Deserialize<Dictionary<string, int>>("{\"a\":7,\"b\":9}", settings)!;
        Console.WriteLine("map=" + map["a"] + ":" + map["b"]);
        var immutable = JsonSerializer.Deserialize<Inventory>("{\"Count\":12,\"Name\":\"Herb\"}", settings)!;
        Console.WriteLine("constructor=" + immutable.Name + ":" + immutable.Count);
        Console.WriteLine("AOT serialization end");
    }

    private static void PrintPlayer(string label, Player player)
    {
        Console.WriteLine(label + "=" + player.Name + ":" + player.Level + ":" + string.Join("|", player.Items));
    }

    private static void Scalars(DoxSerializerSettings settings)
    {
        Console.WriteLine("== REDox scalar converters ==");
        Scalar(true, settings);
        Scalar('漢', settings);
        Scalar(sbyte.MinValue, settings);
        Scalar(byte.MaxValue, settings);
        Scalar(short.MinValue, settings);
        Scalar(ushort.MaxValue, settings);
        Scalar(int.MinValue, settings);
        Scalar(uint.MaxValue, settings);
        Scalar(long.MinValue, settings);
        Scalar(ulong.MaxValue, settings);
        Scalar(-1.25f, settings);
        Scalar(-1.25d, settings);
        Scalar((Half)(-1.25f), settings);
        Scalar(decimal.MaxValue, settings);
        Scalar(Int128.MinValue, settings);
        Scalar(UInt128.MaxValue, settings);
        Scalar(new DateTime(638713838451234567L, DateTimeKind.Utc), settings);
        Scalar(new DateTimeOffset(2025, 1, 1, 12, 34, 56, TimeSpan.FromHours(9)), settings);
        Scalar(Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), settings);
        Scalar(TimeSpan.FromTicks(-123456789), settings);
        Scalar(new DateOnly(2025, 1, 1), settings);
        Scalar(new TimeOnly(12, 34, 56), settings);
        byte[] data = { 0, 0xab, 0xff };
        string json = JsonSerializer.Serialize(data, settings);
        Console.WriteLine("binary=" + json + ":" + BitConverter.ToString(JsonSerializer.Deserialize<byte[]>(json, settings)!));
        Console.WriteLine("scalar converters end");
    }

    private static void Scalar<T>(T value, DoxSerializerSettings settings)
    {
        string json = JsonSerializer.Serialize(value, settings);
        T restored = JsonSerializer.Deserialize<T>(json, settings)!;
        byte[] dox = DoxSerializer.Serialize(value, settings);
        T binary = DoxSerializer.Deserialize<T>(dox, settings)!;
        bool jsonEqual = EqualityComparer<T>.Default.Equals(value, restored);
        bool doxEqual = EqualityComparer<T>.Default.Equals(value, binary);
        Console.WriteLine("scalar:" + typeof(T).Name + "=" + json + ":" + jsonEqual + ":" + doxEqual);
        if (!jsonEqual || !doxEqual)
            throw new InvalidOperationException("scalar round trip failed: " + typeof(T).Name);
    }

    private static void CustomConverter()
    {
        Console.WriteLine("== REDox custom converter ==");
        var settings = new DoxSerializerSettings
        {
            AllowDynamicGenericConverters = false,
            Converters = new DataConverter[] { new ScoreConverter() }
        };
        var value = new Score(42);
        string json = JsonSerializer.Serialize(value, settings);
        Console.WriteLine("custom json=" + json + ":" + JsonSerializer.Deserialize<Score>(json, settings)!.Value);
        byte[] dox = DoxSerializer.Serialize(value, settings);
        Console.WriteLine("custom dox=" + DoxSerializer.Deserialize<Score>(dox, settings)!.Value);
        Console.WriteLine("custom converter end");
    }

    public sealed class Player
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public string[] Items { get; set; } = Array.Empty<string>();
    }

    public sealed class Inventory
    {
        public string Name { get; }
        public int Count { get; }

        public Inventory(string name, int count)
        {
            Name = name;
            Count = count;
        }
    }

    public sealed class Score
    {
        public int Value { get; }

        public Score(int value)
        {
            Value = value;
        }
    }

    private sealed class ScoreConverter : DataConverter<Score>
    {
        public override Score Read(in DataReader reader, uint tokenId, Score? existingValue)
        {
            return new Score(reader.ReadInt32(tokenId) - 100);
        }

        public override void Write(DataWriter writer, Score? value)
        {
            writer.WriteInt32(value!.Value + 100);
        }
    }
}
