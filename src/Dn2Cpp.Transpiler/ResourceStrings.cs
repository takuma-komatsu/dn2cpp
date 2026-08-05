using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Dn2Cpp;

/// <summary>Reads the string entries out of an assembly's embedded
/// <c>.resources</c> blobs — the recovery half of the BCL <c>SR.*</c> resource
/// path (see <see cref="MethodCompiler"/>'s System.SR intrinsic case). Every BCL
/// exception message is a resource string the real <c>ResourceManager</c> would
/// look up by name; dn2cpp never runs the manager (it drags in CultureInfo/ICU),
/// so it reads the English text out of the assembly's own embedded blob at
/// transpile time and folds it in as a literal.
///
/// <para>The wire format is parsed by hand off <see cref="PEReader"/> /
/// <see cref="BlobReader"/> — the surface the self-host closure already carries — rather
/// than through <c>System.Resources.ResourceReader</c>, which pulls in a
/// ResourceManager/BinaryFormatter subtree dn2cpp does not transpile. Malformed or
/// unrecognized input degrades to whatever was parsed, so a resource-format quirk in some
/// library never fails a transpile; a miss just leaves the SR key as its own text. Reads no
/// model members (strict-completion safe).</para>
///
/// <para>The same walk (<see cref="Locate"/>) serves <see cref="ReadBlobs"/>, the RAW half —
/// every embedded manifest resource's bytes, whatever its name — which
/// <see cref="CppEmitter"/> carries into the image so
/// <c>Assembly.GetManifestResourceStream</c> can be served.</para>
///
/// <para><c>runtime/core/intrinsics/dn2cpp_system_resources.cpp</c> parses the same format
/// in C++ and cannot be folded into this one: it reads a <c>.rodata</c> span at RUN time to
/// answer a key the program computes, so a misparse there would ANSWER rather than merely
/// lose a message — which is why that reader refuses a RuntimeResourceSet version it does
/// not know instead of guessing, and this one degrades.
/// <c>gates/build-and-run-manifest-resources.sh</c> diffs both against real .NET.</para>
/// </summary>
internal static class ResourceStrings
{
    /// <summary>One embedded manifest resource of a module: its manifest name, a
    /// <see cref="BlobReader"/> positioned at the first byte of its payload, and the
    /// payload's byte length (the uint32 the resource-directory entry prefixes it
    /// with). <see cref="Locate"/> is the single place that knows how to get from a
    /// <c>ManifestResource</c> row to its bytes — both readers below go through it,
    /// so the RVA/offset/length-prefix arithmetic has exactly one statement of
    /// itself.</summary>
    internal readonly struct EmbeddedResource
    {
        public EmbeddedResource(string name, BlobReader reader, int length)
        {
            Name = name;
            Reader = reader;
            Length = length;
        }

        public string Name { get; }
        public BlobReader Reader { get; }
        public int Length { get; }
    }

    /// <summary>Every EMBEDDED manifest resource of the module, in metadata
    /// (deterministic) order. A linked/forwarded resource (Implementation not nil)
    /// lives in another file we do not have and is skipped. Reads no model members
    /// (strict-completion safe).</summary>
    internal static List<EmbeddedResource> Locate(PEReader pe, MetadataReader reader)
    {
        var found = new List<EmbeddedResource>();
        var corHeader = pe.PEHeaders?.CorHeader;
        if (corHeader is null)
            return found;
        int resourcesRva = corHeader.ResourcesDirectory.RelativeVirtualAddress;
        if (resourcesRva == 0)
            return found;

        foreach (var mrh in reader.ManifestResources)
        {
            var mr = reader.GetManifestResource(mrh);
            if (!mr.Implementation.IsNil)
                continue;
            try
            {
                var block = pe.GetSectionData(resourcesRva);
                var r = block.GetReader();
                r.Offset = (int)mr.Offset;
                // The resource-directory entry is a uint32 byte length then the blob.
                uint length = r.ReadUInt32();
                if (length > int.MaxValue || r.RemainingBytes < (int)length)
                    continue;
                found.Add(new EmbeddedResource(reader.GetString(mr.Name), r, (int)length));
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                // Defensive: a malformed directory entry skips that resource rather than
                // failing the transpile (the ReadStrings posture, see the type doc).
            }
        }
        return found;
    }

    /// <summary>The name → raw bytes of every embedded manifest resource of a module,
    /// in metadata order — what <c>Assembly.GetManifestResourceStream</c> /
    /// <c>GetManifestResourceNames</c> serve. Unlike <see cref="ReadStrings"/> this
    /// does not interpret the payload: a resource is an opaque blob whatever its name
    /// (a <c>.resources</c> set, a text file, a binary asset).</summary>
    internal static List<(string Name, byte[] Bytes)> ReadBlobs(PEReader pe, MetadataReader reader)
    {
        var result = new List<(string, byte[])>();
        foreach (var res in Locate(pe, reader))
        {
            try
            {
                var r = res.Reader;
                result.Add((res.Name, r.ReadBytes(res.Length)));
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                // Defensive, as above: an unreadable blob is dropped, not fatal.
            }
        }
        return result;
    }

    /// <summary>The name → text map of every string entry across all embedded
    /// <c>*.resources</c> blobs of a module, in metadata (deterministic) order.</summary>
    internal static Dictionary<string, string> ReadStrings(PEReader pe, MetadataReader reader)
    {
        var result = new Dictionary<string, string>(System.StringComparer.Ordinal);
        foreach (var res in Locate(pe, reader))
        {
            if (!res.Name.EndsWith(".resources", System.StringComparison.Ordinal))
                continue;
            try
            {
                ParseResourceSet(res.Reader, result);
            }
            catch (Exception e) when (!Compilation.IsMustEscape(e))
            {
                // Defensive: a format the parser does not recognize leaves whatever was
                // recovered so far and skips the rest — never fails the transpile.
            }
        }
        return result;
    }

    /// <summary>Parses one runtime <c>.resources</c> blob, adding its string entries
    /// (ResourceTypeCode.String, code 1) to <paramref name="into"/>. Validated against
    /// the real net10 CoreLib blob (magic 0xBEEFCACE, RuntimeResourceSet v2).</summary>
    private static void ParseResourceSet(BlobReader r, Dictionary<string, string> into)
    {
        int origin = r.Offset; // all "relative to blob start" offsets are from here
        if (r.ReadUInt32() != 0xBEEFCACE)
            return; // ResourceManager magic — not a runtime resource blob
        _ = r.ReadInt32();          // ResourceManager header version
        int headerSize = r.ReadInt32();
        r.Offset += headerSize;     // skip the reader/set type-name header

        _ = r.ReadInt32();          // RuntimeResourceSet version (1 or 2)
        int numResources = r.ReadInt32();
        int numTypes = r.ReadInt32();
        if (numResources < 0 || numTypes < 0)
            return;
        for (int i = 0; i < numTypes; i++)
            SkipPrefixedUtf8(ref r); // type-name strings (unused — only String entries wanted)

        // Name-hash / name-position tables are 8-aligned relative to the blob start.
        int rel = r.Offset - origin;
        int pad = (8 - (rel & 7)) & 7;
        r.Offset += pad;

        r.Offset += 4 * numResources; // name hashes (int32 each)
        var namePositions = new int[numResources];
        for (int i = 0; i < numResources; i++)
            namePositions[i] = r.ReadInt32();
        int dataSectionOffset = r.ReadInt32(); // absolute, from blob start
        int nameSectionStart = r.Offset;       // name positions are relative to here

        for (int i = 0; i < numResources; i++)
        {
            r.Offset = nameSectionStart + namePositions[i];
            int nameByteLen = Read7BitEncodedInt(ref r);
            if (nameByteLen < 0)
                continue;
            var nameBytes = r.ReadBytes(nameByteLen);
            string name = Encoding.Unicode.GetString(nameBytes);
            int dataOffset = r.ReadInt32();

            r.Offset = origin + dataSectionOffset + dataOffset;
            int typeCode = Read7BitEncodedInt(ref r);
            // ResourceTypeCode.String == 1: a 7-bit-length-prefixed UTF-8 string. Every
            // other code (numeric/binary/serialized) is skipped — exception messages are
            // all strings.
            if (typeCode != 1)
                continue;
            int valLen = Read7BitEncodedInt(ref r);
            if (valLen < 0)
                continue;
            var valBytes = r.ReadBytes(valLen);
            into[name] = Encoding.UTF8.GetString(valBytes);
        }
    }

    /// <summary>Skips a <c>BinaryWriter</c>-style string (7-bit-encoded byte length +
    /// that many UTF-8 bytes) — the type-name entries in the resource header.</summary>
    private static void SkipPrefixedUtf8(ref BlobReader r)
    {
        int len = Read7BitEncodedInt(ref r);
        if (len > 0)
            r.Offset += len;
    }

    /// <summary>.NET's <c>BinaryReader.Read7BitEncodedInt</c> — a variable-length int,
    /// little-endian 7 bits per byte with the high bit as the continuation flag. NOT the
    /// ECMA compressed integer <see cref="BlobReader.ReadCompressedInteger"/> reads (a
    /// different, big-endian scheme), which is why it is spelled out here.</summary>
    private static int Read7BitEncodedInt(ref BlobReader r)
    {
        int value = 0, shift = 0;
        byte b;
        do
        {
            if (shift >= 35)
                return -1; // malformed (more than 5 bytes) — defensive
            b = r.ReadByte();
            value |= (b & 0x7F) << shift;
            shift += 7;
        }
        while ((b & 0x80) != 0);
        return value;
    }
}
