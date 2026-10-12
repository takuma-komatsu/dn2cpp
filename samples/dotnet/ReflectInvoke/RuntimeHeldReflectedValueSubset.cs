#nullable enable
#pragma warning disable SYSLIB0050, SYSLIB0051
using System;
using System.Runtime.Serialization;

namespace ReflectInvoke;

internal static class RuntimeHeldReflectedValueSubset
{
    private sealed class Holder
    {
#pragma warning disable CS0649
        public static DateTime DateTimeValue;
        public static decimal DecimalValue;
#pragma warning restore CS0649
    }

    // Typed serialization writes never ask the converter to construct a value.
    private sealed class Converter : IFormatterConverter
    {
        public object Convert(object value, Type type) => throw new InvalidOperationException("Unexpected conversion");
        public object Convert(object value, TypeCode typeCode) => throw new InvalidOperationException("Unexpected conversion");
        public bool ToBoolean(object value) => throw new InvalidOperationException("Unexpected conversion");
        public byte ToByte(object value) => throw new InvalidOperationException("Unexpected conversion");
        public char ToChar(object value) => throw new InvalidOperationException("Unexpected conversion");
        public DateTime ToDateTime(object value) => throw new InvalidOperationException("Unexpected conversion");
        public decimal ToDecimal(object value) => throw new InvalidOperationException("Unexpected conversion");
        public double ToDouble(object value) => throw new InvalidOperationException("Unexpected conversion");
        public short ToInt16(object value) => throw new InvalidOperationException("Unexpected conversion");
        public int ToInt32(object value) => throw new InvalidOperationException("Unexpected conversion");
        public long ToInt64(object value) => throw new InvalidOperationException("Unexpected conversion");
        public sbyte ToSByte(object value) => throw new InvalidOperationException("Unexpected conversion");
        public float ToSingle(object value) => throw new InvalidOperationException("Unexpected conversion");
        public string ToString(object value) => throw new InvalidOperationException("Unexpected conversion");
        public ushort ToUInt16(object value) => throw new InvalidOperationException("Unexpected conversion");
        public uint ToUInt32(object value) => throw new InvalidOperationException("Unexpected conversion");
        public ulong ToUInt64(object value) => throw new InvalidOperationException("Unexpected conversion");
    }

    internal static void Run()
    {
        object date = typeof(Holder).GetField(nameof(Holder.DateTimeValue))!.GetValue(null)!;
        var info = new SerializationInfo(date.GetType(), new Converter());
        ((ISerializable)date).GetObjectData(info, default);
        Console.WriteLine("reflected DateTime serialization=completed");

        object number = typeof(Holder).GetField(nameof(Holder.DecimalValue))!.GetValue(null)!;
        ((IDeserializationCallback)number).OnDeserialization(null);
        Console.WriteLine("reflected Decimal callback=completed");
    }
}
