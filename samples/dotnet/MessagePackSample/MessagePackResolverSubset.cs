using System;
using System.Buffers;
using MessagePack;
using MessagePack.Formatters;

namespace MessagePackResolverSubset;

public readonly struct WrappedInt
{
    public int Value { get; }

    public WrappedInt(int value)
    {
        Value = value;
    }
}

public sealed class WrappedIntFormatter : IMessagePackFormatter<WrappedInt>
{
    public void Serialize(ref MessagePackWriter writer, WrappedInt value,
        MessagePackSerializerOptions options)
    {
        writer.Write(value.Value + 1000);
    }

    public WrappedInt Deserialize(ref MessagePackReader reader,
        MessagePackSerializerOptions options)
    {
        return new WrappedInt(reader.ReadInt32() - 1000);
    }
}

internal static class Program
{
    internal static void __GateEntry(MessagePackSerializerOptions options)
    {
        byte[] custom = MessagePackSerializer.Serialize(new WrappedInt(-17), options);
        WrappedInt customBack = MessagePackSerializer.Deserialize<WrappedInt>(custom, options);
        Console.WriteLine("[resolver] hex=" + Convert.ToHexString(custom)
            + " back=" + customBack.Value);

        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        writer.WriteArrayHeader(3);
        writer.Write(17);
        writer.Write("direct");
        writer.Write(true);
        writer.Flush();
        byte[] direct = buffer.WrittenSpan.ToArray();
        var reader = new MessagePackReader(direct);
        int count = reader.ReadArrayHeader();
        Console.WriteLine("[writer-reader] hex=" + Convert.ToHexString(direct)
            + " back=" + count + ":" + reader.ReadInt32() + ":" + reader.ReadString()
            + ":" + reader.ReadBoolean());

        byte[] objectBytes = MessagePackSerializer.Serialize(
            new MessagePackObjectSubset.Person { Id = 4, Name = "sequence", Score = 8 }, options);
        var sequence = new ReadOnlySequence<byte>(objectBytes);
        MessagePackObjectSubset.Person sequenceBack =
            MessagePackSerializer.Deserialize<MessagePackObjectSubset.Person>(sequence, options);
        Console.WriteLine("[sequence] hex=" + Convert.ToHexString(objectBytes)
            + " back=" + sequenceBack.Id + ":" + sequenceBack.Name + ":" + sequenceBack.Score);

        var repeated = new int[64];
        for (int i = 0; i < repeated.Length; i++)
        {
            repeated[i] = i % 4;
        }
        MessagePackSerializerOptions compressedOptions = options.WithCompression(
            MessagePackCompression.Lz4BlockArray);
        byte[] compressed = MessagePackSerializer.Serialize(repeated, compressedOptions);
        int[] compressedBack = MessagePackSerializer.Deserialize<int[]>(compressed, compressedOptions);
        Console.WriteLine("[lz4-block-array] bytes=" + compressed.Length
            + " first=" + compressedBack[0] + ":" + compressedBack[1]
            + " last=" + compressedBack[62] + ":" + compressedBack[63]);
    }
}
