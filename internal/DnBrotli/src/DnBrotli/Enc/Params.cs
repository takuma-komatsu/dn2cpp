// Port of c/enc/params.h (brotli v1.1.0). The embedded
// SharedEncoderDictionary lives in EncoderDict.cs (c/enc/encoder_dict.h).

namespace DnBrotli.Enc;

/// <summary><c>struct BrotliHasherParams</c>.</summary>
internal struct BrotliHasherParams
{
    public int type;
    public int bucket_bits;
    public int block_bits;
    public int num_last_distances_to_check;
}

/// <summary><c>struct BrotliDistanceParams</c>.</summary>
internal struct BrotliDistanceParams
{
    public uint distance_postfix_bits;
    public uint num_direct_distance_codes;
    public uint alphabet_size_max;
    public uint alphabet_size_limit;
    public nuint max_distance;
}

/// <summary><c>struct BrotliEncoderParams</c>: encoding parameters.</summary>
internal struct BrotliEncoderParams
{
    public BrotliEncoderMode mode;
    public int quality;
    public int lgwin;
    public int lgblock;
    public nuint stream_offset;
    public nuint size_hint;
    public int disable_literal_context_modeling;  /* BROTLI_BOOL */
    public int large_window;                      /* BROTLI_BOOL */
    public BrotliHasherParams hasher;
    public BrotliDistanceParams dist;
    public SharedEncoderDictionary dictionary;
}
