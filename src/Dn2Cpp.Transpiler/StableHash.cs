using System.Text;

namespace Dn2Cpp;

// The companion is self-hosted, so publication hashes use the same portable
// arithmetic in managed and native runs. Length frames separate adjacent values.
internal sealed class StableHash
{
    private ulong _value = 14695981039346656037UL;

    internal void AddBytes(byte[] bytes)
    {
        foreach (byte value in bytes)
            AddByte(value);
    }

    internal void AddFrame(byte[] bytes)
    {
        ulong length = (ulong)bytes.Length;
        for (int i = 0; i < 8; i++)
        {
            AddByte((byte)length);
            length >>= 8;
        }
        AddBytes(bytes);
    }

    internal void AddText(string text) => AddFrame(Encoding.UTF8.GetBytes(text));

    private void AddByte(byte value)
    {
        _value ^= value;
        _value = unchecked(_value * 1099511628211UL);
    }

    internal string Hex()
    {
        var chars = new char[16];
        ulong value = _value;
        for (int i = chars.Length - 1; i >= 0; i--)
        {
            chars[i] = "0123456789abcdef"[(int)(value & 15)];
            value >>= 4;
        }
        return new string(chars);
    }

    internal static bool IsHex(string value)
    {
        if (value.Length != 16) return false;
        foreach (char character in value)
            if (!(character >= '0' && character <= '9') && !(character >= 'a' && character <= 'f'))
                return false;
        return true;
    }
}
