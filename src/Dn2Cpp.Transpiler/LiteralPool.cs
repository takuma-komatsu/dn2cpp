namespace Dn2Cpp;

/// <summary>Interns string literals (ldstr) into global Dn2CppString* variables,
/// and raw byte blobs (ldtoken field RVA data for RuntimeHelpers.InitializeArray)
/// into static const arrays.
/// <para>Everything that lands here is emitted, so only bodies that are actually KEPT may
/// write to the pool. The shared-generics planning pass compiles bodies it then drops, so it
/// is handed a scratch pool of its own (<c>CppEmitter.CompileReachableBodies</c>); pointing
/// it at the real one would emit a literal for every dropped body and a second copy of every
/// kept one.</para></summary>
internal sealed class LiteralPool
{
    private readonly Dictionary<string, int> _map = new();
    public List<string> Literals { get; } = new();

    /// <summary>Raw RVA blobs for static array initializers, by byte content.</summary>
    public List<byte[]> Blobs { get; } = new();

    /// <summary>Content index over <see cref="Blobs"/>: FNV-1a of the bytes -> the indices
    /// carrying that hash. The hash only narrows the search and the byte-for-byte compare
    /// decides; trusting the hash alone would silently merge two colliding initializer tables
    /// into one array.</summary>
    private readonly Dictionary<int, List<int>> _blobsByHash = new();

    public string GetOrAdd(string value)
    {
        if (!_map.TryGetValue(value, out int idx))
        {
            idx = Literals.Count;
            Literals.Add(value);
            _map.Add(value, idx);
        }
        return "str_" + idx;
    }

    /// <summary>Interns one RVA blob by content. Two <c>ldtoken</c>s of the same
    /// initializer — or of two distinct fields that happen to hold identical bytes —
    /// share one emitted array. The data is immutable <c>const uint8_t[]</c> that only
    /// ever gets copied out (RuntimeHelpers.InitializeArray) or read through, so
    /// sharing is unobservable.</summary>
    public string AddBlob(byte[] data)
    {
        int hash = ContentHash(data);
        if (_blobsByHash.TryGetValue(hash, out var sameHash))
        {
            foreach (int i in sameHash)
                if (SameContent(Blobs[i], data))
                    return "blob_" + i;
        }
        else
        {
            sameHash = new List<int>();
            _blobsByHash.Add(hash, sameHash);
        }
        int idx = Blobs.Count;
        Blobs.Add(data);
        sameHash.Add(idx);
        return "blob_" + idx;
    }

    /// <summary>FNV-1a over the bytes. Plain arithmetic, no hashing BCL surface, so it
    /// stays deterministic and transpilable under self-host (same reason as
    /// <see cref="CppNaming.ShapeHash"/>).</summary>
    private static int ContentHash(byte[] data)
    {
        uint h = 2166136261u;
        foreach (byte b in data)
        {
            h ^= b;
            h *= 16777619u;
        }
        return (int)h;
    }

    private static bool SameContent(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
            if (a[i] != b[i])
                return false;
        return true;
    }
}
