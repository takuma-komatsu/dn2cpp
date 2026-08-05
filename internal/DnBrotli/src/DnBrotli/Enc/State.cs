// Port of c/enc/state.h (brotli v1.1.0).
//
// The C `struct BrotliEncoderStateStruct` (typedef'd BrotliEncoderStateInternal,
// #define'd BrotliEncoderState) is mirrored field-for-field as the unsafe
// struct BrotliEncoderState below; instances live in unmanaged memory only
// (allocated by BrotliBootstrapAlloc in EncoderEngine, PORTING.md).
//
// Layout notes:
//   - The tiny_buf_ union (u64[2] / u8[16]) is a LayoutKind.Explicit struct
//     with both fixed buffers at offset 0.
//   - flint_ is int8_t in C (values are BrotliEncoderFlintState) -> sbyte.
//   - The embedded Hasher is HasherCommon + the per-type `privat` union
//     (Hash.cs; the union currently holds the quickly-hasher rooms).

using System.Runtime.InteropServices;

namespace DnBrotli.Enc;

/// <summary><c>BrotliEncoderStreamState</c>. Values match the C enum exactly.</summary>
internal enum BrotliEncoderStreamState
{
    /* Default state. */
    BROTLI_STREAM_PROCESSING = 0,
    /* Intermediate state; after next block is emitted, byte-padding should be
       performed before getting back to default state. */
    BROTLI_STREAM_FLUSH_REQUESTED = 1,
    /* Last metablock was produced; no more input is acceptable. */
    BROTLI_STREAM_FINISHED = 2,
    /* Flushing compressed block and writing meta-data block header. */
    BROTLI_STREAM_METADATA_HEAD = 3,
    /* Writing metadata block body. */
    BROTLI_STREAM_METADATA_BODY = 4,
}

/// <summary><c>BrotliEncoderFlintState</c>.</summary>
internal enum BrotliEncoderFlintState
{
    BROTLI_FLINT_NEEDS_2_BYTES = 2,
    BROTLI_FLINT_NEEDS_1_BYTE = 1,
    BROTLI_FLINT_WAITING_FOR_PROCESSING = 0,
    BROTLI_FLINT_WAITING_FOR_FLUSHING = -1,
    BROTLI_FLINT_DONE = -2,
}

/// <summary><c>union { uint64_t u64[2]; uint8_t u8[16]; } tiny_buf_</c>: temporary
/// buffer for padding flush bits or metadata block header / body.</summary>
[StructLayout(LayoutKind.Explicit)]
internal unsafe struct BrotliEncoderTinyBuf
{
    [FieldOffset(0)] public fixed ulong u64[2];
    [FieldOffset(0)] public fixed byte u8[16];
}

/// <summary><c>struct BrotliEncoderStateStruct</c> (aka <c>BrotliEncoderState</c>),
/// mirrored field-for-field. Public because the Raw API hands out opaque
/// <c>BrotliEncoderState*</c> pointers, exactly like <c>BrotliDecoderState</c>.</summary>
public unsafe struct BrotliEncoderState
{
    internal BrotliEncoderParams @params;

    internal MemoryManager memory_manager_;

    internal ulong input_pos_;
    internal RingBuffer ringbuffer_;
    internal nuint cmd_alloc_size_;
    internal Command* commands_;
    internal nuint num_commands_;
    internal nuint num_literals_;
    internal nuint last_insert_len_;
    internal ulong last_flush_pos_;
    internal ulong last_processed_pos_;
    internal fixed int dist_cache_[16];      /* [BROTLI_NUM_DISTANCE_SHORT_CODES] */
    internal fixed int saved_dist_cache_[4];
    internal ushort last_bytes_;
    internal byte last_bytes_bits_;
    /* "Flint" is a tiny uncompressed block emitted before the continuation
       block to unwire literal context from previous data. Despite being int8_t,
       field is actually BrotliEncoderFlintState enum. */
    internal sbyte flint_;
    internal byte prev_byte_;
    internal byte prev_byte2_;
    internal nuint storage_size_;
    internal byte* storage_;

    internal Hasher hasher_;

    /* Hash table for FAST_ONE_PASS_COMPRESSION_QUALITY mode. */
    internal fixed int small_table_[1 << 10];  /* 4KiB */
    internal int* large_table_;                /* Allocated only when needed */
    internal nuint large_table_size_;

    internal BrotliOnePassArena* one_pass_arena_;
    internal BrotliTwoPassArena* two_pass_arena_;

    /* Command and literal buffers for FAST_TWO_PASS_COMPRESSION_QUALITY. */
    internal uint* command_buf_;
    internal byte* literal_buf_;

    internal ulong total_in_;
    internal byte* next_out_;
    internal nuint available_out_;
    internal ulong total_out_;
    /* Temporary buffer for padding flush bits or metadata block header / body. */
    internal BrotliEncoderTinyBuf tiny_buf_;
    internal uint remaining_metadata_bytes_;
    internal BrotliEncoderStreamState stream_state_;

    internal int is_last_block_emitted_;  /* BROTLI_BOOL */
    internal int is_initialized_;         /* BROTLI_BOOL */
}
