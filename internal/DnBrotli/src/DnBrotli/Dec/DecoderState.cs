// Port of c/dec/state.{h,c} (brotli v1.1.0).
//
// The C `struct BrotliDecoderStateStruct` (typedef'd BrotliDecoderStateInternal,
// #define'd BrotliDecoderState) is mirrored field-for-field as the unsafe
// struct BrotliDecoderState below; instances live in unmanaged memory only.
//
// Layout notes (PORTING.md):
//   - brotli_reg_t / size_t fields are nuint; embedded brotli_reg_t arrays are
//     fixed ulong buffers (identical on the 64-bit-only dn2cpp targets),
//     because fixed buffers cannot have nuint elements.
//   - Embedded HuffmanCode arrays are fixed uint buffers (HuffmanCode is
//     4 bytes) with HuffmanCode* accessor properties, keeping 4-byte alignment.
//   - The C unions (buffer, arena) are LayoutKind.Explicit structs with both
//     members at offset 0.
//   - The C bit-fields (is_last_metablock:1 ... size_nibbles:8) are expanded
//     to one uint field each; only internal consistency matters, no external
//     ABI is shared with C.
//   - brotli_alloc_func/brotli_free_func/opaque are accepted and ignored
//     (kept as nint/void* fields for layout only): BROTLI_DECODER_ALLOC/FREE
//     always map to NativeMemory.Alloc/Free.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DnBrotli.Common;

namespace DnBrotli.Dec;

/// <summary><c>BrotliRunningState</c>. Values/order match the C enum exactly.</summary>
internal enum BrotliRunningState
{
    BROTLI_STATE_UNINITED,
    BROTLI_STATE_LARGE_WINDOW_BITS,
    BROTLI_STATE_INITIALIZE,
    BROTLI_STATE_METABLOCK_BEGIN,
    BROTLI_STATE_METABLOCK_HEADER,
    BROTLI_STATE_METABLOCK_HEADER_2,
    BROTLI_STATE_CONTEXT_MODES,
    BROTLI_STATE_COMMAND_BEGIN,
    BROTLI_STATE_COMMAND_INNER,
    BROTLI_STATE_COMMAND_POST_DECODE_LITERALS,
    BROTLI_STATE_COMMAND_POST_WRAP_COPY,
    BROTLI_STATE_UNCOMPRESSED,
    BROTLI_STATE_METADATA,
    BROTLI_STATE_COMMAND_INNER_WRITE,
    BROTLI_STATE_METABLOCK_DONE,
    BROTLI_STATE_COMMAND_POST_WRITE_1,
    BROTLI_STATE_COMMAND_POST_WRITE_2,
    BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_HEADER,
    BROTLI_STATE_HUFFMAN_CODE_0,
    BROTLI_STATE_HUFFMAN_CODE_1,
    BROTLI_STATE_HUFFMAN_CODE_2,
    BROTLI_STATE_HUFFMAN_CODE_3,
    BROTLI_STATE_CONTEXT_MAP_1,
    BROTLI_STATE_CONTEXT_MAP_2,
    BROTLI_STATE_TREE_GROUP,
    BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_BODY,
    BROTLI_STATE_DONE,
}

/// <summary><c>BrotliRunningMetablockHeaderState</c>.</summary>
internal enum BrotliRunningMetablockHeaderState
{
    BROTLI_STATE_METABLOCK_HEADER_NONE,
    BROTLI_STATE_METABLOCK_HEADER_EMPTY,
    BROTLI_STATE_METABLOCK_HEADER_NIBBLES,
    BROTLI_STATE_METABLOCK_HEADER_SIZE,
    BROTLI_STATE_METABLOCK_HEADER_UNCOMPRESSED,
    BROTLI_STATE_METABLOCK_HEADER_RESERVED,
    BROTLI_STATE_METABLOCK_HEADER_BYTES,
    BROTLI_STATE_METABLOCK_HEADER_METADATA,
}

/// <summary><c>BrotliRunningUncompressedState</c>.</summary>
internal enum BrotliRunningUncompressedState
{
    BROTLI_STATE_UNCOMPRESSED_NONE,
    BROTLI_STATE_UNCOMPRESSED_WRITE,
}

/// <summary><c>BrotliRunningTreeGroupState</c>.</summary>
internal enum BrotliRunningTreeGroupState
{
    BROTLI_STATE_TREE_GROUP_NONE,
    BROTLI_STATE_TREE_GROUP_LOOP,
}

/// <summary><c>BrotliRunningContextMapState</c>.</summary>
internal enum BrotliRunningContextMapState
{
    BROTLI_STATE_CONTEXT_MAP_NONE,
    BROTLI_STATE_CONTEXT_MAP_READ_PREFIX,
    BROTLI_STATE_CONTEXT_MAP_HUFFMAN,
    BROTLI_STATE_CONTEXT_MAP_DECODE,
    BROTLI_STATE_CONTEXT_MAP_TRANSFORM,
}

/// <summary><c>BrotliRunningHuffmanState</c>.</summary>
internal enum BrotliRunningHuffmanState
{
    BROTLI_STATE_HUFFMAN_NONE,
    BROTLI_STATE_HUFFMAN_SIMPLE_SIZE,
    BROTLI_STATE_HUFFMAN_SIMPLE_READ,
    BROTLI_STATE_HUFFMAN_SIMPLE_BUILD,
    BROTLI_STATE_HUFFMAN_COMPLEX,
    BROTLI_STATE_HUFFMAN_LENGTH_SYMBOLS,
}

/// <summary><c>BrotliRunningDecodeUint8State</c>.</summary>
internal enum BrotliRunningDecodeUint8State
{
    BROTLI_STATE_DECODE_UINT8_NONE,
    BROTLI_STATE_DECODE_UINT8_SHORT,
    BROTLI_STATE_DECODE_UINT8_LONG,
}

/// <summary><c>BrotliRunningReadBlockLengthState</c>.</summary>
internal enum BrotliRunningReadBlockLengthState
{
    BROTLI_STATE_READ_BLOCK_LENGTH_NONE,
    BROTLI_STATE_READ_BLOCK_LENGTH_SUFFIX,
}

/// <summary><c>BrotliDecoderCompoundDictionary</c>: BrotliDecoderState addon, used for
/// Compound Dictionary functionality.</summary>
internal unsafe struct BrotliDecoderCompoundDictionary
{
    public int num_chunks;
    public int total_size;
    public int br_index;
    public int br_offset;
    public int br_length;
    public int br_copied;
    private fixed ulong chunks_[16];  /* const uint8_t*[16] */
    public fixed int chunk_offsets[16];
    public int block_bits;
    public fixed byte block_map[256];

    public byte* chunks(int i)
    {
        return (byte*)chunks_[i];
    }

    public void set_chunks(int i, byte* value)
    {
        chunks_[i] = (ulong)value;
    }
}

/// <summary><c>BrotliMetablockHeaderArena</c>.</summary>
internal unsafe struct BrotliMetablockHeaderArena
{
    public BrotliRunningTreeGroupState substate_tree_group;
    public BrotliRunningContextMapState substate_context_map;
    public BrotliRunningHuffmanState substate_huffman;

    public nuint sub_loop_counter;

    public nuint repeat_code_len;
    public nuint prev_code_len;

    /* For ReadHuffmanCode. */
    public nuint symbol;
    public nuint repeat;
    public nuint space;

    /* Huffman table for "histograms": HuffmanCode table[32], flattened.
       8 bytes per entry, not 4: under .NET sizeof(HuffmanCode) == 4 (byte +
       ushort, packed), but the dn2cpp backend widens small struct fields to
       int32, making sizeof(HuffmanCode) == 8 — the slot must hold either. */
    private fixed ulong table_[32];
    /* List of heads of symbol chains. */
    public ushort* symbol_lists;
    /* Storage from symbol_lists. */
    public fixed ushort symbols_lists_array[Huffman.BROTLI_HUFFMAN_MAX_CODE_LENGTH + 1 +
                                            BrotliConstants.BROTLI_NUM_COMMAND_SYMBOLS];
    /* Tails of symbol chains. */
    public fixed int next_symbol[32];
    public fixed byte code_length_code_lengths[BrotliConstants.BROTLI_CODE_LENGTH_CODES];
    /* Population counts for the code lengths. */
    public fixed ushort code_length_histo[16];

    /* For HuffmanTreeGroupDecode. */
    public int htree_index;
    public HuffmanCode* next;

    /* For DecodeContextMap. */
    public nuint context_index;
    public nuint max_run_length_prefix;
    public nuint code;
    /* HuffmanCode context_map_table[BROTLI_HUFFMAN_MAX_SIZE_272], flattened.
       ulong slots for the same reason as table_ above. */
    private fixed ulong context_map_table_[Huffman.BROTLI_HUFFMAN_MAX_SIZE_272];

    /// <summary><c>table</c> as <c>HuffmanCode*</c>. Only valid because the state lives
    /// in unmanaged memory (never GC-moved).</summary>
    public HuffmanCode* table
    {
        get { fixed (ulong* p = table_) { return (HuffmanCode*)p; } }
    }

    /// <summary><c>context_map_table</c> as <c>HuffmanCode*</c>.</summary>
    public HuffmanCode* context_map_table
    {
        get { fixed (ulong* p = context_map_table_) { return (HuffmanCode*)p; } }
    }
}

/// <summary><c>BrotliMetablockBodyArena</c>.</summary>
internal unsafe struct BrotliMetablockBodyArena
{
    public fixed byte dist_extra_bits[544];
    public fixed ulong dist_offset[544];  /* brotli_reg_t[544] */
}

/// <summary>The anonymous <c>union { header; body; } arena</c> member of the C
/// state struct.</summary>
[StructLayout(LayoutKind.Explicit)]
internal struct BrotliDecoderStateArena
{
    [FieldOffset(0)] public BrotliMetablockHeaderArena header;
    [FieldOffset(0)] public BrotliMetablockBodyArena body;
}

/// <summary>The anonymous <c>union { uint64_t u64; uint8_t u8[8]; } buffer</c> member:
/// temporary storage for remaining input. Brotli stream format is designed in a way,
/// that 64 bits are enough to make progress in decoding.</summary>
[StructLayout(LayoutKind.Explicit)]
internal unsafe struct BrotliDecoderStateBuffer
{
    [FieldOffset(0)] public ulong u64;
    [FieldOffset(0)] public fixed byte u8[8];
}

/// <summary><c>struct BrotliDecoderStateStruct</c> (aka <c>BrotliDecoderState</c>),
/// field-for-field in C declaration order. The type is public so the Raw C-ABI layer
/// (<c>RawBrotli</c>) can traffic in <c>BrotliDecoderState*</c> exactly like decode.h —
/// but it stays opaque: every member is internal.</summary>
public unsafe struct BrotliDecoderState
{
    internal BrotliRunningState state;

    /* This counter is reused for several disjoint loops. */
    internal int loop_counter;

    internal BrotliBitReader br;

    /* Accepted and ignored: layout-only stand-ins for
       brotli_alloc_func / brotli_free_func / opaque. */
    internal nint alloc_func;
    internal nint free_func;
    internal void* memory_manager_opaque;

    internal BrotliDecoderStateBuffer buffer;
    internal nuint buffer_length;

    internal int pos;
    internal int max_backward_distance;
    internal int max_distance;
    internal int ringbuffer_size;
    internal int ringbuffer_mask;
    internal int dist_rb_idx;
    internal fixed int dist_rb[4];
    internal int error_code;
    internal int meta_block_remaining_len;

    internal byte* ringbuffer;
    internal byte* ringbuffer_end;
    internal HuffmanCode* htree_command;
    internal byte* context_lookup;
    internal byte* context_map_slice;
    internal byte* dist_context_map_slice;

    /* This ring buffer holds a few past copy distances that will be used by
       some special distance codes. */
    internal HuffmanTreeGroup literal_hgroup;
    internal HuffmanTreeGroup insert_copy_hgroup;
    internal HuffmanTreeGroup distance_hgroup;
    internal HuffmanCode* block_type_trees;
    internal HuffmanCode* block_len_trees;
    /* This is true if the literal context map histogram type always matches the
       block type. It is then not needed to keep the context (faster decoding). */
    internal int trivial_literal_context;
    /* Distance context is actual after command is decoded and before distance is
       computed. After distance computation it is used as a temporary variable. */
    internal int distance_context;
    internal fixed ulong block_length[3];  /* brotli_reg_t[3] */
    internal nuint block_length_index;
    internal fixed ulong num_block_types[3];  /* brotli_reg_t[3] */
    internal fixed ulong block_type_rb[6];  /* brotli_reg_t[6] */
    internal nuint distance_postfix_bits;
    internal nuint num_direct_distance_codes;
    internal nuint num_dist_htrees;
    internal byte* dist_context_map;
    internal HuffmanCode* literal_htree;

    /* For partial write operations. */
    internal nuint rb_roundtrips;    /* how many times we went around the ring-buffer */
    internal nuint partial_pos_out;  /* how much output to the user in total */

    /* For InverseMoveToFrontTransform. */
    internal nuint mtf_upper_bound;
    internal fixed uint mtf[64 + 1];

    internal int copy_length;
    internal int distance_code;

    internal byte dist_htree_index;

    /* Less used attributes are at the end of this struct. */

    /* Accepted and ignored: layout-only stand-ins for
       brotli_decoder_metadata_start_func / brotli_decoder_metadata_chunk_func. */
    internal nint metadata_start_func;
    internal nint metadata_chunk_func;
    internal void* metadata_callback_opaque;

    /* For reporting. */
    internal ulong used_input;  /* how many bytes of input are consumed */

    /* States inside function calls. */
    internal BrotliRunningMetablockHeaderState substate_metablock_header;
    internal BrotliRunningUncompressedState substate_uncompressed;
    internal BrotliRunningDecodeUint8State substate_decode_uint8;
    internal BrotliRunningReadBlockLengthState substate_read_block_length;

    internal int new_ringbuffer_size;

    /* The C packs these eight as bit-fields (:1 ... :6, :8); expanded to one
       uint each — no external ABI depends on the packing. */
    internal uint is_last_metablock;
    internal uint is_uncompressed;
    internal uint is_metadata;
    internal uint should_wrap_ringbuffer;
    internal uint canny_ringbuffer_allocation;
    internal uint large_window;
    internal uint window_bits;
    internal uint size_nibbles;

    internal nuint num_literal_htrees;
    internal byte* context_map;
    internal byte* context_modes;

    internal BrotliSharedDictionaryInternal* dictionary;
    internal BrotliDecoderCompoundDictionary* compound_dictionary;

    internal fixed uint trivial_literal_contexts[8];  /* 256 bits */

    internal BrotliDecoderStateArena arena;
}

internal static unsafe class State
{
    /* Literal/Command/Distance block size maximum; same as maximum metablock size;
       used as block size when there is no block switching. */
    internal const uint BROTLI_BLOCK_SIZE_CAP = 1u << 24;

    /// <summary><c>BROTLI_DECODER_ALLOC(S, L)</c>. Custom allocators are ignored:
    /// always NativeMemory (malloc semantics — not zeroed).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void* BROTLI_DECODER_ALLOC(BrotliDecoderState* s, nuint len)
    {
        _ = s;
        return NativeMemory.Alloc(len);
    }

    /// <summary><c>BROTLI_DECODER_FREE(S, X)</c>: frees and nulls the field.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BROTLI_DECODER_FREE<T>(BrotliDecoderState* s, ref T* x)
        where T : unmanaged
    {
        _ = s;
        NativeMemory.Free(x);
        x = null;
    }

    /// <summary><c>BROTLI_DECODER_FREE</c> overload for pointer-to-pointer fields
    /// (pointer types cannot be generic type arguments).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BROTLI_DECODER_FREE(BrotliDecoderState* s, ref HuffmanCode** x)
    {
        _ = s;
        NativeMemory.Free(x);
        x = null;
    }

    /// <summary><c>BrotliDecoderStateInit</c>. The alloc_func/free_func/opaque triple is
    /// accepted for signature fidelity but ignored (stored for layout only).</summary>
    internal static int BrotliDecoderStateInit(BrotliDecoderState* s,
        nint alloc_func, nint free_func, void* opaque)
    {
        s->alloc_func = alloc_func;
        s->free_func = free_func;
        s->memory_manager_opaque = opaque;

        s->error_code = 0; /* BROTLI_DECODER_NO_ERROR */

        BitReader.BrotliInitBitReader(&s->br);
        s->state = BrotliRunningState.BROTLI_STATE_UNINITED;
        s->large_window = 0;
        s->substate_metablock_header =
            BrotliRunningMetablockHeaderState.BROTLI_STATE_METABLOCK_HEADER_NONE;
        s->substate_uncompressed =
            BrotliRunningUncompressedState.BROTLI_STATE_UNCOMPRESSED_NONE;
        s->substate_decode_uint8 =
            BrotliRunningDecodeUint8State.BROTLI_STATE_DECODE_UINT8_NONE;
        s->substate_read_block_length =
            BrotliRunningReadBlockLengthState.BROTLI_STATE_READ_BLOCK_LENGTH_NONE;

        s->buffer_length = 0;
        s->loop_counter = 0;
        s->pos = 0;
        s->rb_roundtrips = 0;
        s->partial_pos_out = 0;
        s->used_input = 0;

        s->block_type_trees = null;
        s->block_len_trees = null;
        s->ringbuffer = null;
        s->ringbuffer_size = 0;
        s->new_ringbuffer_size = 0;
        s->ringbuffer_mask = 0;

        s->context_map = null;
        s->context_modes = null;
        s->dist_context_map = null;
        s->context_map_slice = null;
        s->dist_context_map_slice = null;

        s->literal_hgroup.codes = null;
        s->literal_hgroup.htrees = null;
        s->insert_copy_hgroup.codes = null;
        s->insert_copy_hgroup.htrees = null;
        s->distance_hgroup.codes = null;
        s->distance_hgroup.htrees = null;

        s->is_last_metablock = 0;
        s->is_uncompressed = 0;
        s->is_metadata = 0;
        s->should_wrap_ringbuffer = 0;
        s->canny_ringbuffer_allocation = 1;

        s->window_bits = 0;
        s->max_distance = 0;
        s->dist_rb[0] = 16;
        s->dist_rb[1] = 15;
        s->dist_rb[2] = 11;
        s->dist_rb[3] = 4;
        s->dist_rb_idx = 0;
        s->block_type_trees = null;
        s->block_len_trees = null;

        s->mtf_upper_bound = 63;

        s->compound_dictionary = null;
        s->dictionary = SharedDictionary.BrotliSharedDictionaryCreateInstance();
        if (s->dictionary == null) return 0;

        s->metadata_start_func = 0;
        s->metadata_chunk_func = 0;
        s->metadata_callback_opaque = null;

        return 1;
    }

    /// <summary><c>BrotliDecoderStateMetablockBegin</c>.</summary>
    internal static void BrotliDecoderStateMetablockBegin(BrotliDecoderState* s)
    {
        s->meta_block_remaining_len = 0;
        s->block_length[0] = BROTLI_BLOCK_SIZE_CAP;
        s->block_length[1] = BROTLI_BLOCK_SIZE_CAP;
        s->block_length[2] = BROTLI_BLOCK_SIZE_CAP;
        s->num_block_types[0] = 1;
        s->num_block_types[1] = 1;
        s->num_block_types[2] = 1;
        s->block_type_rb[0] = 1;
        s->block_type_rb[1] = 0;
        s->block_type_rb[2] = 1;
        s->block_type_rb[3] = 0;
        s->block_type_rb[4] = 1;
        s->block_type_rb[5] = 0;
        s->context_map = null;
        s->context_modes = null;
        s->dist_context_map = null;
        s->context_map_slice = null;
        s->literal_htree = null;
        s->dist_context_map_slice = null;
        s->dist_htree_index = 0;
        s->context_lookup = null;
        s->literal_hgroup.codes = null;
        s->literal_hgroup.htrees = null;
        s->insert_copy_hgroup.codes = null;
        s->insert_copy_hgroup.htrees = null;
        s->distance_hgroup.codes = null;
        s->distance_hgroup.htrees = null;
    }

    /// <summary><c>BrotliDecoderStateCleanupAfterMetablock</c>.</summary>
    internal static void BrotliDecoderStateCleanupAfterMetablock(BrotliDecoderState* s)
    {
        BROTLI_DECODER_FREE(s, ref s->context_modes);
        BROTLI_DECODER_FREE(s, ref s->context_map);
        BROTLI_DECODER_FREE(s, ref s->dist_context_map);
        BROTLI_DECODER_FREE(s, ref s->literal_hgroup.htrees);
        BROTLI_DECODER_FREE(s, ref s->insert_copy_hgroup.htrees);
        BROTLI_DECODER_FREE(s, ref s->distance_hgroup.htrees);
    }

    /// <summary><c>BrotliDecoderStateCleanup</c>.</summary>
    internal static void BrotliDecoderStateCleanup(BrotliDecoderState* s)
    {
        BrotliDecoderStateCleanupAfterMetablock(s);

        BROTLI_DECODER_FREE(s, ref s->compound_dictionary);
        SharedDictionary.BrotliSharedDictionaryDestroyInstance(s->dictionary);
        s->dictionary = null;
        BROTLI_DECODER_FREE(s, ref s->ringbuffer);
        BROTLI_DECODER_FREE(s, ref s->block_type_trees);
    }

    /// <summary><c>BrotliDecoderHuffmanTreeGroupInit</c>. Returns 1 on success, 0 on
    /// allocation failure.</summary>
    internal static int BrotliDecoderHuffmanTreeGroupInit(BrotliDecoderState* s,
        HuffmanTreeGroup* group, nuint alphabet_size_max,
        nuint alphabet_size_limit, nuint ntrees)
    {
        /* 376 = 256 (1-st level table) + 4 + 7 + 15 + 31 + 63 (2-nd level mix-tables)
           This number is discovered "unlimited" "enough" calculator; it is actually
           a wee bigger than required in several cases (especially for alphabets with
           less than 16 symbols). */
        nuint max_table_size = alphabet_size_limit + 376;
        nuint code_size = (nuint)sizeof(HuffmanCode) * ntrees * max_table_size;
        nuint htree_size = (nuint)sizeof(HuffmanCode*) * ntrees;
        /* Pointer alignment is, hopefully, wider than sizeof(HuffmanCode). */
        HuffmanCode** p = (HuffmanCode**)BROTLI_DECODER_ALLOC(s, code_size + htree_size);
        group->alphabet_size_max = (ushort)alphabet_size_max;
        group->alphabet_size_limit = (ushort)alphabet_size_limit;
        group->num_htrees = (ushort)ntrees;
        group->htrees = p;
        group->codes = (HuffmanCode*)(&p[ntrees]);
        return (p != null) ? 1 : 0;
    }
}
