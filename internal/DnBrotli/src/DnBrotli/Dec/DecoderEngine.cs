// Port of c/dec/decode.c (brotli v1.1.0).
//
// Function-for-function, in the C source order, with the resumable
// switch/goto structure preserved (PORTING.md): C fallthrough becomes
// `goto case`, `goto saveStateAndReturn` becomes a C# label.
//
// Port-wide notes:
//   - The BROTLI_SAFE(METHOD) macro is expanded at every use site exactly as
//     the C preprocessor does, so each paired function keeps its explicit
//     Safe* twin (ReadCommand/SafeReadCommand, ...). This doubling is
//     intentional.
//   - BROTLI_HC_* "hot copy" macros (an ARM32-only optimization) collapse to
//     plain pointer arithmetic and field access, as in Huffman.cs.
//   - BROTLI_DECODER_* error/result names are kept via private const aliases
//     of the public enums (whose values match the C exactly).
//   - The metadata callback fields are layout-only stand-ins (DecoderState.cs)
//     and always 0; the C callback invocations are retained as comments.
//   - BROTLI_LOG*/BROTLI_DCHECK/BROTLI_DUMP are no-ops in a release C build
//     and are dropped; BROTLI_FAILURE(CODE) is therefore the identity.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DnBrotli.Common;

using static DnBrotli.Common.BrotliConstants;
using static DnBrotli.Common.Context;
using static DnBrotli.Common.SharedDictionary;
using static DnBrotli.Common.Transforms;
using static DnBrotli.Dec.BitReader;
using static DnBrotli.Dec.BrotliRunningContextMapState;
using static DnBrotli.Dec.BrotliRunningDecodeUint8State;
using static DnBrotli.Dec.BrotliRunningHuffmanState;
using static DnBrotli.Dec.BrotliRunningMetablockHeaderState;
using static DnBrotli.Dec.BrotliRunningReadBlockLengthState;
using static DnBrotli.Dec.BrotliRunningState;
using static DnBrotli.Dec.BrotliRunningTreeGroupState;
using static DnBrotli.Dec.BrotliRunningUncompressedState;
using static DnBrotli.Dec.Huffman;
using static DnBrotli.Dec.Prefix;
using static DnBrotli.Dec.State;

namespace DnBrotli.Dec;

internal static unsafe class DecoderEngine
{
    private const int BROTLI_TRUE = 1;
    private const int BROTLI_FALSE = 0;

    /* C-name aliases for the public enum members (values match the C exactly). */
    private const BrotliDecoderResult BROTLI_DECODER_RESULT_ERROR = BrotliDecoderResult.Error;
    private const BrotliDecoderResult BROTLI_DECODER_RESULT_SUCCESS = BrotliDecoderResult.Success;
    private const BrotliDecoderResult BROTLI_DECODER_RESULT_NEEDS_MORE_INPUT = BrotliDecoderResult.NeedsMoreInput;
    private const BrotliDecoderResult BROTLI_DECODER_RESULT_NEEDS_MORE_OUTPUT = BrotliDecoderResult.NeedsMoreOutput;

    private const BrotliDecoderErrorCode BROTLI_DECODER_SUCCESS = BrotliDecoderErrorCode.Success;
    private const BrotliDecoderErrorCode BROTLI_DECODER_NEEDS_MORE_INPUT = BrotliDecoderErrorCode.NeedsMoreInput;
    private const BrotliDecoderErrorCode BROTLI_DECODER_NEEDS_MORE_OUTPUT = BrotliDecoderErrorCode.NeedsMoreOutput;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_EXUBERANT_NIBBLE = BrotliDecoderErrorCode.ErrorFormatExuberantNibble;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_RESERVED = BrotliDecoderErrorCode.ErrorFormatReserved;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_EXUBERANT_META_NIBBLE = BrotliDecoderErrorCode.ErrorFormatExuberantMetaNibble;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_SIMPLE_HUFFMAN_ALPHABET = BrotliDecoderErrorCode.ErrorFormatSimpleHuffmanAlphabet;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_SIMPLE_HUFFMAN_SAME = BrotliDecoderErrorCode.ErrorFormatSimpleHuffmanSame;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_CL_SPACE = BrotliDecoderErrorCode.ErrorFormatClSpace;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_HUFFMAN_SPACE = BrotliDecoderErrorCode.ErrorFormatHuffmanSpace;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_CONTEXT_MAP_REPEAT = BrotliDecoderErrorCode.ErrorFormatContextMapRepeat;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_BLOCK_LENGTH_1 = BrotliDecoderErrorCode.ErrorFormatBlockLength1;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_BLOCK_LENGTH_2 = BrotliDecoderErrorCode.ErrorFormatBlockLength2;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_TRANSFORM = BrotliDecoderErrorCode.ErrorFormatTransform;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_DICTIONARY = BrotliDecoderErrorCode.ErrorFormatDictionary;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_WINDOW_BITS = BrotliDecoderErrorCode.ErrorFormatWindowBits;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_PADDING_1 = BrotliDecoderErrorCode.ErrorFormatPadding1;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_PADDING_2 = BrotliDecoderErrorCode.ErrorFormatPadding2;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_FORMAT_DISTANCE = BrotliDecoderErrorCode.ErrorFormatDistance;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_COMPOUND_DICTIONARY = BrotliDecoderErrorCode.ErrorCompoundDictionary;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_DICTIONARY_NOT_SET = BrotliDecoderErrorCode.ErrorDictionaryNotSet;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_INVALID_ARGUMENTS = BrotliDecoderErrorCode.ErrorInvalidArguments;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_ALLOC_CONTEXT_MODES = BrotliDecoderErrorCode.ErrorAllocContextModes;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_ALLOC_TREE_GROUPS = BrotliDecoderErrorCode.ErrorAllocTreeGroups;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_ALLOC_CONTEXT_MAP = BrotliDecoderErrorCode.ErrorAllocContextMap;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_ALLOC_RING_BUFFER_1 = BrotliDecoderErrorCode.ErrorAllocRingBuffer1;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_ALLOC_RING_BUFFER_2 = BrotliDecoderErrorCode.ErrorAllocRingBuffer2;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_ALLOC_BLOCK_TYPE_TREES = BrotliDecoderErrorCode.ErrorAllocBlockTypeTrees;
    private const BrotliDecoderErrorCode BROTLI_DECODER_ERROR_UNREACHABLE = BrotliDecoderErrorCode.ErrorUnreachable;

    /// <summary><c>BROTLI_FAILURE(CODE)</c>: <c>BROTLI_DUMP()</c> is a release no-op.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static BrotliDecoderErrorCode BROTLI_FAILURE(BrotliDecoderErrorCode code)
    {
        return code;
    }

    private const int HUFFMAN_TABLE_BITS = 8;
    private const int HUFFMAN_TABLE_MASK = 0xFF;

    /* We need the slack region for the following reasons:
        - doing up to two 16-byte copies for fast backward copying
        - inserting transformed dictionary word:
            255 prefix + 32 base + 255 suffix */
    private const nuint kRingBufferWriteAheadSlack = 542;

    private static ReadOnlySpan<byte> kCodeLengthCodeOrder =>  /* [BROTLI_CODE_LENGTH_CODES] */
    [
        1, 2, 3, 4, 0, 5, 17, 6, 16, 7, 8, 9, 10, 11, 12, 13, 14, 15,
    ];

    /* Static prefix code for the complex code length code lengths. */
    private static ReadOnlySpan<byte> kCodeLengthPrefixLength =>  /* [16] */
    [
        2, 2, 2, 3, 2, 2, 2, 4, 2, 2, 2, 3, 2, 2, 2, 4,
    ];

    private static ReadOnlySpan<byte> kCodeLengthPrefixValue =>  /* [16] */
    [
        0, 4, 3, 2, 0, 4, 3, 1, 0, 4, 3, 2, 0, 4, 3, 5,
    ];

    /// <summary><c>BrotliDecoderSetParameter</c>.</summary>
    internal static int BrotliDecoderSetParameter(
        BrotliDecoderState* state, BrotliDecoderParameter p, uint value)
    {
        if (state->state != BROTLI_STATE_UNINITED) return BROTLI_FALSE;
        switch (p)
        {
            case BrotliDecoderParameter.DisableRingBufferReallocation:
                state->canny_ringbuffer_allocation = value != 0 ? 0u : 1u;
                return BROTLI_TRUE;

            case BrotliDecoderParameter.LargeWindow:
                state->large_window = value != 0 ? 1u : 0u;  /* TO_BROTLI_BOOL(!!value) */
                return BROTLI_TRUE;

            default: return BROTLI_FALSE;
        }
    }

    /// <summary><c>BrotliDecoderCreateInstance</c>. Custom allocators are accepted for
    /// signature fidelity but ignored (see DecoderState.cs); the C's "exactly one of the
    /// pair set" rejection is preserved.</summary>
    internal static BrotliDecoderState* BrotliDecoderCreateInstance(
        nint alloc_func, nint free_func, void* opaque)
    {
        BrotliDecoderState* state = null;
        if (alloc_func == 0 && free_func == 0)
        {
            state = (BrotliDecoderState*)NativeMemory.Alloc((nuint)sizeof(BrotliDecoderState));
        }
        else if (alloc_func != 0 && free_func != 0)
        {
            state = (BrotliDecoderState*)NativeMemory.Alloc((nuint)sizeof(BrotliDecoderState));
        }
        if (state == null)
        {
            return null;
        }
        if (BrotliDecoderStateInit(state, alloc_func, free_func, opaque) == 0)
        {
            NativeMemory.Free(state);
            return null;
        }
        return state;
    }

    /// <summary><c>BrotliDecoderDestroyInstance</c>: deinitializes and frees the
    /// instance.</summary>
    internal static void BrotliDecoderDestroyInstance(BrotliDecoderState* state)
    {
        if (state == null)
        {
            return;
        }
        else
        {
            BrotliDecoderStateCleanup(state);
            NativeMemory.Free(state);
        }
    }

    /* Saves error code and converts it to BrotliDecoderResult. */
    private static BrotliDecoderResult SaveErrorCode(
        BrotliDecoderState* s, BrotliDecoderErrorCode e, nuint consumed_input)
    {
        s->error_code = (int)e;
        s->used_input += consumed_input;
        if ((s->buffer_length != 0) && (s->br.next_in == s->br.last_in))
        {
            /* If internal buffer is depleted at last, reset it. */
            s->buffer_length = 0;
        }
        switch (e)
        {
            case BROTLI_DECODER_SUCCESS:
                return BROTLI_DECODER_RESULT_SUCCESS;

            case BROTLI_DECODER_NEEDS_MORE_INPUT:
                return BROTLI_DECODER_RESULT_NEEDS_MORE_INPUT;

            case BROTLI_DECODER_NEEDS_MORE_OUTPUT:
                return BROTLI_DECODER_RESULT_NEEDS_MORE_OUTPUT;

            default:
                return BROTLI_DECODER_RESULT_ERROR;
        }
    }

    /* Decodes WBITS by reading 1 - 7 bits, or 0x11 for "Large Window Brotli".
       Precondition: bit-reader accumulator has at least 8 bits. */
    private static BrotliDecoderErrorCode DecodeWindowBits(BrotliDecoderState* s,
        BrotliBitReader* br)
    {
        nuint n;
        int large_window = (int)s->large_window;
        s->large_window = 0;  /* BROTLI_FALSE */
        BrotliTakeBits(br, 1, &n);
        if (n == 0)
        {
            s->window_bits = 16;
            return BROTLI_DECODER_SUCCESS;
        }
        BrotliTakeBits(br, 3, &n);
        if (n != 0)
        {
            s->window_bits = (uint)((17 + n) & 63);
            return BROTLI_DECODER_SUCCESS;
        }
        BrotliTakeBits(br, 3, &n);
        if (n == 1)
        {
            if (large_window != 0)
            {
                BrotliTakeBits(br, 1, &n);
                if (n == 1)
                {
                    return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_WINDOW_BITS);
                }
                s->large_window = 1;  /* BROTLI_TRUE */
                return BROTLI_DECODER_SUCCESS;
            }
            else
            {
                return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_WINDOW_BITS);
            }
        }
        if (n != 0)
        {
            s->window_bits = (uint)((8 + n) & 63);
            return BROTLI_DECODER_SUCCESS;
        }
        s->window_bits = 17;
        return BROTLI_DECODER_SUCCESS;
    }

    /// <summary><c>memmove16</c>: the C non-NEON branch reads all 16 bytes into a local
    /// buffer before writing, so overlapping regions behave like a buffered copy.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void memmove16(byte* dst, byte* src)
    {
        ulong buffer0 = Internal.Bits.BROTLI_UNALIGNED_LOAD64LE(src);
        ulong buffer1 = Internal.Bits.BROTLI_UNALIGNED_LOAD64LE(src + 8);
        Internal.Bits.BROTLI_UNALIGNED_STORE64LE(dst, buffer0);
        Internal.Bits.BROTLI_UNALIGNED_STORE64LE(dst + 8, buffer1);
    }

    /* Decodes a number in the range [0..255], by reading 1 - 11 bits. */
    private static BrotliDecoderErrorCode DecodeVarLenUint8(
        BrotliDecoderState* s, BrotliBitReader* br, nuint* value)
    {
        nuint bits;
        switch (s->substate_decode_uint8)
        {
            case BROTLI_STATE_DECODE_UINT8_NONE:
                if (BrotliSafeReadBits(br, 1, &bits) == 0)
                {
                    return BROTLI_DECODER_NEEDS_MORE_INPUT;
                }
                if (bits == 0)
                {
                    *value = 0;
                    return BROTLI_DECODER_SUCCESS;
                }
                /* Fall through. */
                goto case BROTLI_STATE_DECODE_UINT8_SHORT;

            case BROTLI_STATE_DECODE_UINT8_SHORT:
                if (BrotliSafeReadBits(br, 3, &bits) == 0)
                {
                    s->substate_decode_uint8 = BROTLI_STATE_DECODE_UINT8_SHORT;
                    return BROTLI_DECODER_NEEDS_MORE_INPUT;
                }
                if (bits == 0)
                {
                    *value = 1;
                    s->substate_decode_uint8 = BROTLI_STATE_DECODE_UINT8_NONE;
                    return BROTLI_DECODER_SUCCESS;
                }
                /* Use output value as a temporary storage. It MUST be persisted. */
                *value = bits;
                /* Fall through. */
                goto case BROTLI_STATE_DECODE_UINT8_LONG;

            case BROTLI_STATE_DECODE_UINT8_LONG:
                if (BrotliSafeReadBits(br, *value, &bits) == 0)
                {
                    s->substate_decode_uint8 = BROTLI_STATE_DECODE_UINT8_LONG;
                    return BROTLI_DECODER_NEEDS_MORE_INPUT;
                }
                *value = ((nuint)1u << (int)*value) + bits;
                s->substate_decode_uint8 = BROTLI_STATE_DECODE_UINT8_NONE;
                return BROTLI_DECODER_SUCCESS;

            default:
                return
                    BROTLI_FAILURE(BROTLI_DECODER_ERROR_UNREACHABLE);  /* COV_NF_LINE */
        }
    }

    /* Decodes a metablock length and flags by reading 2 - 31 bits. */
    private static BrotliDecoderErrorCode DecodeMetaBlockLength(
        BrotliDecoderState* s, BrotliBitReader* br)
    {
        nuint bits;
        int i;
        for (;;)
        {
            switch (s->substate_metablock_header)
            {
                case BROTLI_STATE_METABLOCK_HEADER_NONE:
                    if (BrotliSafeReadBits(br, 1, &bits) == 0)
                    {
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    s->is_last_metablock = bits != 0 ? 1u : 0u;
                    s->meta_block_remaining_len = 0;
                    s->is_uncompressed = 0;
                    s->is_metadata = 0;
                    if (s->is_last_metablock == 0)
                    {
                        s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_NIBBLES;
                        break;
                    }
                    s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_EMPTY;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_HEADER_EMPTY;

                case BROTLI_STATE_METABLOCK_HEADER_EMPTY:
                    if (BrotliSafeReadBits(br, 1, &bits) == 0)
                    {
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    if (bits != 0)
                    {
                        s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_NONE;
                        return BROTLI_DECODER_SUCCESS;
                    }
                    s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_NIBBLES;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_HEADER_NIBBLES;

                case BROTLI_STATE_METABLOCK_HEADER_NIBBLES:
                    if (BrotliSafeReadBits(br, 2, &bits) == 0)
                    {
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    s->size_nibbles = (uint)(bits + 4);
                    s->loop_counter = 0;
                    if (bits == 3)
                    {
                        s->is_metadata = 1;
                        s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_RESERVED;
                        break;
                    }
                    s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_SIZE;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_HEADER_SIZE;

                case BROTLI_STATE_METABLOCK_HEADER_SIZE:
                    i = s->loop_counter;
                    for (; i < (int)s->size_nibbles; ++i)
                    {
                        if (BrotliSafeReadBits(br, 4, &bits) == 0)
                        {
                            s->loop_counter = i;
                            return BROTLI_DECODER_NEEDS_MORE_INPUT;
                        }
                        if (i + 1 == (int)s->size_nibbles && s->size_nibbles > 4 &&
                            bits == 0)
                        {
                            return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_EXUBERANT_NIBBLE);
                        }
                        s->meta_block_remaining_len |= (int)(bits << (i * 4));
                    }
                    s->substate_metablock_header =
                        BROTLI_STATE_METABLOCK_HEADER_UNCOMPRESSED;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_HEADER_UNCOMPRESSED;

                case BROTLI_STATE_METABLOCK_HEADER_UNCOMPRESSED:
                    if (s->is_last_metablock == 0)
                    {
                        if (BrotliSafeReadBits(br, 1, &bits) == 0)
                        {
                            return BROTLI_DECODER_NEEDS_MORE_INPUT;
                        }
                        s->is_uncompressed = bits != 0 ? 1u : 0u;
                    }
                    ++s->meta_block_remaining_len;
                    s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_NONE;
                    return BROTLI_DECODER_SUCCESS;

                case BROTLI_STATE_METABLOCK_HEADER_RESERVED:
                    if (BrotliSafeReadBits(br, 1, &bits) == 0)
                    {
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    if (bits != 0)
                    {
                        return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_RESERVED);
                    }
                    s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_BYTES;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_HEADER_BYTES;

                case BROTLI_STATE_METABLOCK_HEADER_BYTES:
                    if (BrotliSafeReadBits(br, 2, &bits) == 0)
                    {
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    if (bits == 0)
                    {
                        s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_NONE;
                        return BROTLI_DECODER_SUCCESS;
                    }
                    s->size_nibbles = (uint)bits;
                    s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_METADATA;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_HEADER_METADATA;

                case BROTLI_STATE_METABLOCK_HEADER_METADATA:
                    i = s->loop_counter;
                    for (; i < (int)s->size_nibbles; ++i)
                    {
                        if (BrotliSafeReadBits(br, 8, &bits) == 0)
                        {
                            s->loop_counter = i;
                            return BROTLI_DECODER_NEEDS_MORE_INPUT;
                        }
                        if (i + 1 == (int)s->size_nibbles && s->size_nibbles > 1 &&
                            bits == 0)
                        {
                            return BROTLI_FAILURE(
                                BROTLI_DECODER_ERROR_FORMAT_EXUBERANT_META_NIBBLE);
                        }
                        s->meta_block_remaining_len |= (int)(bits << (i * 8));
                    }
                    ++s->meta_block_remaining_len;
                    s->substate_metablock_header = BROTLI_STATE_METABLOCK_HEADER_NONE;
                    return BROTLI_DECODER_SUCCESS;

                default:
                    return
                        BROTLI_FAILURE(BROTLI_DECODER_ERROR_UNREACHABLE);  /* COV_NF_LINE */
            }
        }
    }

    /* Decodes the Huffman code.
       This method doesn't read data from the bit reader, BUT drops the amount of
       bits that correspond to the decoded symbol.
       bits MUST contain at least 15 (BROTLI_HUFFMAN_MAX_CODE_LENGTH) valid bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint DecodeSymbol(nuint bits, HuffmanCode* table, BrotliBitReader* br)
    {
        table += (int)(bits & HUFFMAN_TABLE_MASK);
        if (table->bits > HUFFMAN_TABLE_BITS)
        {
            nuint nbits = table->bits - (nuint)HUFFMAN_TABLE_BITS;
            BrotliDropBits(br, HUFFMAN_TABLE_BITS);
            table += table->value + (int)((bits >> HUFFMAN_TABLE_BITS) & BitMask(nbits));
        }
        BrotliDropBits(br, table->bits);
        return table->value;
    }

    /* Reads and decodes the next Huffman code from bit-stream.
       This method peeks 16 bits of input and drops 0 - 15 of them. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReadSymbol(HuffmanCode* table, BrotliBitReader* br)
    {
        return DecodeSymbol(BrotliGet16BitsUnmasked(br), table, br);
    }

    /* Same as DecodeSymbol, but it is known that there is less than 15 bits of
       input are currently available. */
    private static int SafeDecodeSymbol(
        HuffmanCode* table, BrotliBitReader* br, nuint* result)
    {
        nuint val;
        nuint available_bits = BrotliGetAvailableBits(br);
        if (available_bits == 0)
        {
            if (table->bits == 0)
            {
                *result = table->value;
                return BROTLI_TRUE;
            }
            return BROTLI_FALSE;  /* No valid bits at all. */
        }
        val = BrotliGetBitsUnmasked(br);
        table += (int)(val & HUFFMAN_TABLE_MASK);
        if (table->bits <= HUFFMAN_TABLE_BITS)
        {
            if (table->bits <= available_bits)
            {
                BrotliDropBits(br, table->bits);
                *result = table->value;
                return BROTLI_TRUE;
            }
            else
            {
                return BROTLI_FALSE;  /* Not enough bits for the first level. */
            }
        }
        if (available_bits <= HUFFMAN_TABLE_BITS)
        {
            return BROTLI_FALSE;  /* Not enough bits to move to the second level. */
        }

        /* Speculatively drop HUFFMAN_TABLE_BITS. */
        val = (val & BitMask(table->bits)) >> HUFFMAN_TABLE_BITS;
        available_bits -= HUFFMAN_TABLE_BITS;
        table += table->value + (int)val;
        if (available_bits < table->bits)
        {
            return BROTLI_FALSE;  /* Not enough bits for the second level. */
        }

        BrotliDropBits(br, HUFFMAN_TABLE_BITS + (nuint)table->bits);
        *result = table->value;
        return BROTLI_TRUE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SafeReadSymbol(
        HuffmanCode* table, BrotliBitReader* br, nuint* result)
    {
        nuint val;
        if (BrotliSafeGetBits(br, 15, &val) != 0)
        {
            *result = DecodeSymbol(val, table, br);
            return BROTLI_TRUE;
        }
        return SafeDecodeSymbol(table, br, result);
    }

    /* Makes a look-up in first level Huffman table. Peeks 8 bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PreloadSymbol(int safe, HuffmanCode* table, BrotliBitReader* br,
        nuint* bits, nuint* value)
    {
        if (safe != 0)
        {
            return;
        }
        table += (int)BrotliGetBits(br, HUFFMAN_TABLE_BITS);
        *bits = table->bits;
        *value = table->value;
    }

    /* Decodes the next Huffman code using data prepared by PreloadSymbol.
       Reads 0 - 15 bits. Also peeks 8 following bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReadPreloadedSymbol(HuffmanCode* table, BrotliBitReader* br,
        nuint* bits, nuint* value)
    {
        nuint result = *value;
        if (*bits > HUFFMAN_TABLE_BITS)
        {
            nuint val = BrotliGet16BitsUnmasked(br);
            HuffmanCode* ext = table + (int)(val & HUFFMAN_TABLE_MASK) + (int)*value;
            nuint mask = BitMask(*bits - HUFFMAN_TABLE_BITS);
            BrotliDropBits(br, HUFFMAN_TABLE_BITS);
            ext += (int)((val >> HUFFMAN_TABLE_BITS) & mask);
            BrotliDropBits(br, ext->bits);
            result = ext->value;
        }
        else
        {
            BrotliDropBits(br, *bits);
        }
        PreloadSymbol(0, table, br, bits, value);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint Log2Floor(nuint x)
    {
        nuint result = 0;
        while (x != 0)
        {
            x >>= 1;
            ++result;
        }
        return result;
    }

    /* Reads (s->symbol + 1) symbols.
       Totally 1..4 symbols are read, 1..11 bits each.
       The list of symbols MUST NOT contain duplicates. */
    private static BrotliDecoderErrorCode ReadSimpleHuffmanSymbols(
        nuint alphabet_size_max, nuint alphabet_size_limit,
        BrotliDecoderState* s)
    {
        /* max_bits == 1..11; symbol == 0..3; 1..44 bits will be read. */
        BrotliBitReader* br = &s->br;
        BrotliMetablockHeaderArena* h = &s->arena.header;
        nuint max_bits = Log2Floor(alphabet_size_max - 1);
        nuint i = h->sub_loop_counter;
        nuint num_symbols = h->symbol;
        while (i <= num_symbols)
        {
            nuint v;
            if (BrotliSafeReadBits(br, max_bits, &v) == 0)
            {
                h->sub_loop_counter = i;
                h->substate_huffman = BROTLI_STATE_HUFFMAN_SIMPLE_READ;
                return BROTLI_DECODER_NEEDS_MORE_INPUT;
            }
            if (v >= alphabet_size_limit)
            {
                return
                    BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_SIMPLE_HUFFMAN_ALPHABET);
            }
            h->symbols_lists_array[i] = (ushort)v;
            ++i;
        }

        for (i = 0; i < num_symbols; ++i)
        {
            nuint k = i + 1;
            for (; k <= num_symbols; ++k)
            {
                if (h->symbols_lists_array[i] == h->symbols_lists_array[k])
                {
                    return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_SIMPLE_HUFFMAN_SAME);
                }
            }
        }

        return BROTLI_DECODER_SUCCESS;
    }

    /* Process single decoded symbol code length:
        A) reset the repeat variable
        B) remember code length (if it is not 0)
        C) extend corresponding index-chain
        D) reduce the Huffman space
        E) update the histogram */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessSingleCodeLength(nuint code_len,
        nuint* symbol, nuint* repeat, nuint* space,
        nuint* prev_code_len, ushort* symbol_lists,
        ushort* code_length_histo, int* next_symbol)
    {
        *repeat = 0;
        if (code_len != 0)
        {  /* code_len == 1..15 */
            symbol_lists[next_symbol[code_len]] = (ushort)(*symbol);
            next_symbol[code_len] = (int)(*symbol);
            *prev_code_len = code_len;
            *space -= (nuint)32768u >> (int)code_len;
            code_length_histo[code_len]++;
        }
        (*symbol)++;
    }

    /* Process repeated symbol code length.
        A) Check if it is the extension of previous repeat sequence; if the decoded
           value is not BROTLI_REPEAT_PREVIOUS_CODE_LENGTH, then it is a new
           symbol-skip
        B) Update repeat variable
        C) Check if operation is feasible (fits alphabet)
        D) For each symbol do the same operations as in ProcessSingleCodeLength

       PRECONDITION: code_len == BROTLI_REPEAT_PREVIOUS_CODE_LENGTH or
                     code_len == BROTLI_REPEAT_ZERO_CODE_LENGTH */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessRepeatedCodeLength(nuint code_len,
        nuint repeat_delta, nuint alphabet_size, nuint* symbol,
        nuint* repeat, nuint* space, nuint* prev_code_len,
        nuint* repeat_code_len, ushort* symbol_lists,
        ushort* code_length_histo, int* next_symbol)
    {
        nuint old_repeat;
        nuint extra_bits = 3;  /* for BROTLI_REPEAT_ZERO_CODE_LENGTH */
        nuint new_len = 0;  /* for BROTLI_REPEAT_ZERO_CODE_LENGTH */
        if (code_len == BROTLI_REPEAT_PREVIOUS_CODE_LENGTH)
        {
            new_len = *prev_code_len;
            extra_bits = 2;
        }
        if (*repeat_code_len != new_len)
        {
            *repeat = 0;
            *repeat_code_len = new_len;
        }
        old_repeat = *repeat;
        if (*repeat > 0)
        {
            *repeat -= 2;
            *repeat <<= (int)extra_bits;
        }
        *repeat += repeat_delta + 3u;
        repeat_delta = *repeat - old_repeat;
        if (*symbol + repeat_delta > alphabet_size)
        {
            *symbol = alphabet_size;
            *space = 0xFFFFF;
            return;
        }
        if (*repeat_code_len != 0)
        {
            nuint last = *symbol + repeat_delta;
            int next = next_symbol[*repeat_code_len];
            do
            {
                symbol_lists[next] = (ushort)*symbol;
                next = (int)*symbol;
            } while (++(*symbol) != last);
            next_symbol[*repeat_code_len] = next;
            *space -= repeat_delta << (int)(15 - *repeat_code_len);
            code_length_histo[*repeat_code_len] =
                (ushort)(code_length_histo[*repeat_code_len] + repeat_delta);
        }
        else
        {
            *symbol += repeat_delta;
        }
    }

    /* Reads and decodes symbol codelengths. */
    private static BrotliDecoderErrorCode ReadSymbolCodeLengths(
        nuint alphabet_size, BrotliDecoderState* s)
    {
        BrotliBitReader* br = &s->br;
        BrotliMetablockHeaderArena* h = &s->arena.header;
        nuint symbol = h->symbol;
        nuint repeat = h->repeat;
        nuint space = h->space;
        nuint prev_code_len = h->prev_code_len;
        nuint repeat_code_len = h->repeat_code_len;
        ushort* symbol_lists = h->symbol_lists;
        ushort* code_length_histo = h->code_length_histo;
        int* next_symbol = h->next_symbol;
        if (BrotliWarmupBitReader(br) == 0)
        {
            return BROTLI_DECODER_NEEDS_MORE_INPUT;
        }
        while (symbol < alphabet_size && space > 0)
        {
            HuffmanCode* p = h->table;
            nuint code_len;
            if (BrotliCheckInputAmount(br) == 0)
            {
                h->symbol = symbol;
                h->repeat = repeat;
                h->prev_code_len = prev_code_len;
                h->repeat_code_len = repeat_code_len;
                h->space = space;
                return BROTLI_DECODER_NEEDS_MORE_INPUT;
            }
            BrotliFillBitWindow16(br);
            p += (int)(BrotliGetBitsUnmasked(br) &
                BitMask(BROTLI_HUFFMAN_MAX_CODE_LENGTH_CODE_LENGTH));
            BrotliDropBits(br, p->bits);  /* Use 1..5 bits. */
            code_len = p->value;  /* code_len == 0..17 */
            if (code_len < BROTLI_REPEAT_PREVIOUS_CODE_LENGTH)
            {
                ProcessSingleCodeLength(code_len, &symbol, &repeat, &space,
                    &prev_code_len, symbol_lists, code_length_histo, next_symbol);
            }
            else
            {  /* code_len == 16..17, extra_bits == 2..3 */
                nuint extra_bits =
                    (code_len == BROTLI_REPEAT_PREVIOUS_CODE_LENGTH) ? 2u : 3u;
                nuint repeat_delta =
                    BrotliGetBitsUnmasked(br) & BitMask(extra_bits);
                BrotliDropBits(br, extra_bits);
                ProcessRepeatedCodeLength(code_len, repeat_delta, alphabet_size,
                    &symbol, &repeat, &space, &prev_code_len, &repeat_code_len,
                    symbol_lists, code_length_histo, next_symbol);
            }
        }
        h->space = space;
        return BROTLI_DECODER_SUCCESS;
    }

    private static BrotliDecoderErrorCode SafeReadSymbolCodeLengths(
        nuint alphabet_size, BrotliDecoderState* s)
    {
        BrotliBitReader* br = &s->br;
        BrotliMetablockHeaderArena* h = &s->arena.header;
        int get_byte = BROTLI_FALSE;
        while (h->symbol < alphabet_size && h->space > 0)
        {
            HuffmanCode* p = h->table;
            nuint code_len;
            nuint available_bits;
            nuint bits = 0;
            if (get_byte != 0 && BrotliPullByte(br) == 0) return BROTLI_DECODER_NEEDS_MORE_INPUT;
            get_byte = BROTLI_FALSE;
            available_bits = BrotliGetAvailableBits(br);
            if (available_bits != 0)
            {
                bits = (uint)BrotliGetBitsUnmasked(br);
            }
            p += (int)(bits & BitMask(BROTLI_HUFFMAN_MAX_CODE_LENGTH_CODE_LENGTH));
            if (p->bits > available_bits)
            {
                get_byte = BROTLI_TRUE;
                continue;
            }
            code_len = p->value;  /* code_len == 0..17 */
            if (code_len < BROTLI_REPEAT_PREVIOUS_CODE_LENGTH)
            {
                BrotliDropBits(br, p->bits);
                ProcessSingleCodeLength(code_len, &h->symbol, &h->repeat, &h->space,
                    &h->prev_code_len, h->symbol_lists, h->code_length_histo,
                    h->next_symbol);
            }
            else
            {  /* code_len == 16..17, extra_bits == 2..3 */
                nuint extra_bits = code_len - 14u;
                nuint repeat_delta = (bits >> p->bits) & BitMask(extra_bits);
                if (available_bits < p->bits + extra_bits)
                {
                    get_byte = BROTLI_TRUE;
                    continue;
                }
                BrotliDropBits(br, p->bits + extra_bits);
                ProcessRepeatedCodeLength(code_len, repeat_delta, alphabet_size,
                    &h->symbol, &h->repeat, &h->space, &h->prev_code_len,
                    &h->repeat_code_len, h->symbol_lists, h->code_length_histo,
                    h->next_symbol);
            }
        }
        return BROTLI_DECODER_SUCCESS;
    }

    /* Reads and decodes 15..18 codes using static prefix code.
       Each code is 2..4 bits long. In total 30..72 bits are used. */
    private static BrotliDecoderErrorCode ReadCodeLengthCodeLengths(BrotliDecoderState* s)
    {
        BrotliBitReader* br = &s->br;
        BrotliMetablockHeaderArena* h = &s->arena.header;
        nuint num_codes = h->repeat;
        nuint space = h->space;
        nuint i = h->sub_loop_counter;
        for (; i < BROTLI_CODE_LENGTH_CODES; ++i)
        {
            byte code_len_idx = kCodeLengthCodeOrder[(int)i];
            nuint ix;
            nuint v;
            if (BrotliSafeGetBits(br, 4, &ix) == 0)
            {
                nuint available_bits = BrotliGetAvailableBits(br);
                if (available_bits != 0)
                {
                    ix = BrotliGetBitsUnmasked(br) & 0xF;
                }
                else
                {
                    ix = 0;
                }
                if (kCodeLengthPrefixLength[(int)ix] > available_bits)
                {
                    h->sub_loop_counter = i;
                    h->repeat = num_codes;
                    h->space = space;
                    h->substate_huffman = BROTLI_STATE_HUFFMAN_COMPLEX;
                    return BROTLI_DECODER_NEEDS_MORE_INPUT;
                }
            }
            v = kCodeLengthPrefixValue[(int)ix];
            BrotliDropBits(br, kCodeLengthPrefixLength[(int)ix]);
            h->code_length_code_lengths[code_len_idx] = (byte)v;
            if (v != 0)
            {
                space = space - (32u >> (int)v);
                ++num_codes;
                ++h->code_length_histo[v];
                if (space - 1u >= 32u)
                {
                    /* space is 0 or wrapped around. */
                    break;
                }
            }
        }
        if (!(num_codes == 1 || space == 0))
        {
            return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_CL_SPACE);
        }
        return BROTLI_DECODER_SUCCESS;
    }

    /* Decodes the Huffman tables.
       There are 2 scenarios:
        A) Huffman code contains only few symbols (1..4). Those symbols are read
           directly; their code lengths are defined by the number of symbols.
           For this scenario 4 - 49 bits will be read.

        B) 2-phase decoding:
        B.1) Small Huffman table is decoded; it is specified with code lengths
             encoded with predefined entropy code. 32 - 74 bits are used.
        B.2) Decoded table is used to decode code lengths of symbols in resulting
             Huffman table. In worst case 3520 bits are read. */
    private static BrotliDecoderErrorCode ReadHuffmanCode(nuint alphabet_size_max,
        nuint alphabet_size_limit, HuffmanCode* table, nuint* opt_table_size,
        BrotliDecoderState* s)
    {
        BrotliBitReader* br = &s->br;
        BrotliMetablockHeaderArena* h = &s->arena.header;
        /* State machine. */
        for (;;)
        {
            switch (h->substate_huffman)
            {
                case BROTLI_STATE_HUFFMAN_NONE:
                    if (BrotliSafeReadBits(br, 2, &h->sub_loop_counter) == 0)
                    {
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    /* The value is used as follows:
                       1 for simple code;
                       0 for no skipping, 2 skips 2 code lengths, 3 skips 3 code lengths */
                    if (h->sub_loop_counter != 1)
                    {
                        h->space = 32;
                        h->repeat = 0;  /* num_codes */
                        NativeMemory.Clear(h->code_length_histo, sizeof(ushort) *
                            (nuint)(BROTLI_HUFFMAN_MAX_CODE_LENGTH_CODE_LENGTH + 1));
                        NativeMemory.Clear(h->code_length_code_lengths,
                            BROTLI_CODE_LENGTH_CODES);
                        h->substate_huffman = BROTLI_STATE_HUFFMAN_COMPLEX;
                        continue;
                    }
                    /* Fall through. */
                    goto case BROTLI_STATE_HUFFMAN_SIMPLE_SIZE;

                case BROTLI_STATE_HUFFMAN_SIMPLE_SIZE:
                    /* Read symbols, codes & code lengths directly. */
                    if (BrotliSafeReadBits(br, 2, &h->symbol) == 0)
                    {  /* num_symbols */
                        h->substate_huffman = BROTLI_STATE_HUFFMAN_SIMPLE_SIZE;
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    h->sub_loop_counter = 0;
                    /* Fall through. */
                    goto case BROTLI_STATE_HUFFMAN_SIMPLE_READ;

                case BROTLI_STATE_HUFFMAN_SIMPLE_READ:
                {
                    BrotliDecoderErrorCode result =
                        ReadSimpleHuffmanSymbols(alphabet_size_max, alphabet_size_limit, s);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        return result;
                    }
                }
                /* Fall through. */
                goto case BROTLI_STATE_HUFFMAN_SIMPLE_BUILD;

                case BROTLI_STATE_HUFFMAN_SIMPLE_BUILD:
                {
                    nuint table_size;
                    if (h->symbol == 3)
                    {
                        nuint bits;
                        if (BrotliSafeReadBits(br, 1, &bits) == 0)
                        {
                            h->substate_huffman = BROTLI_STATE_HUFFMAN_SIMPLE_BUILD;
                            return BROTLI_DECODER_NEEDS_MORE_INPUT;
                        }
                        h->symbol += bits;
                    }
                    table_size = BrotliBuildSimpleHuffmanTable(table, HUFFMAN_TABLE_BITS,
                        h->symbols_lists_array, (uint)h->symbol);
                    if (opt_table_size != null)
                    {
                        *opt_table_size = table_size;
                    }
                    h->substate_huffman = BROTLI_STATE_HUFFMAN_NONE;
                    return BROTLI_DECODER_SUCCESS;
                }

                /* Decode Huffman-coded code lengths. */
                case BROTLI_STATE_HUFFMAN_COMPLEX:
                {
                    nuint i;
                    BrotliDecoderErrorCode result = ReadCodeLengthCodeLengths(s);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        return result;
                    }
                    BrotliBuildCodeLengthsHuffmanTable(h->table,
                                                       h->code_length_code_lengths,
                                                       h->code_length_histo);
                    NativeMemory.Clear(h->code_length_histo, sizeof(ushort) * 16);
                    for (i = 0; i <= BROTLI_HUFFMAN_MAX_CODE_LENGTH; ++i)
                    {
                        h->next_symbol[i] = (int)i - (BROTLI_HUFFMAN_MAX_CODE_LENGTH + 1);
                        h->symbol_lists[h->next_symbol[i]] = 0xFFFF;
                    }

                    h->symbol = 0;
                    h->prev_code_len = BROTLI_INITIAL_REPEATED_CODE_LENGTH;
                    h->repeat = 0;
                    h->repeat_code_len = 0;
                    h->space = 32768;
                    h->substate_huffman = BROTLI_STATE_HUFFMAN_LENGTH_SYMBOLS;
                }
                /* Fall through. */
                goto case BROTLI_STATE_HUFFMAN_LENGTH_SYMBOLS;

                case BROTLI_STATE_HUFFMAN_LENGTH_SYMBOLS:
                {
                    nuint table_size;
                    BrotliDecoderErrorCode result = ReadSymbolCodeLengths(
                        alphabet_size_limit, s);
                    if (result == BROTLI_DECODER_NEEDS_MORE_INPUT)
                    {
                        result = SafeReadSymbolCodeLengths(alphabet_size_limit, s);
                    }
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        return result;
                    }

                    if (h->space != 0)
                    {
                        return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_HUFFMAN_SPACE);
                    }
                    table_size = BrotliBuildHuffmanTable(
                        table, HUFFMAN_TABLE_BITS, h->symbol_lists, h->code_length_histo);
                    if (opt_table_size != null)
                    {
                        *opt_table_size = table_size;
                    }
                    h->substate_huffman = BROTLI_STATE_HUFFMAN_NONE;
                    return BROTLI_DECODER_SUCCESS;
                }

                default:
                    return
                        BROTLI_FAILURE(BROTLI_DECODER_ERROR_UNREACHABLE);  /* COV_NF_LINE */
            }
        }
    }

    /* Decodes a block length by reading 3..39 bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint ReadBlockLength(HuffmanCode* table, BrotliBitReader* br)
    {
        nuint code;
        nuint nbits;
        code = ReadSymbol(table, br);
        nbits = _kBrotliPrefixCodeRanges[(int)code].nbits;  /* nbits == 2..24 */
        return _kBrotliPrefixCodeRanges[(int)code].offset + BrotliReadBits24(br, nbits);
    }

    /* WARNING: if state is not BROTLI_STATE_READ_BLOCK_LENGTH_NONE, then
       reading can't be continued with ReadBlockLength. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SafeReadBlockLength(
        BrotliDecoderState* s, nuint* result, HuffmanCode* table,
        BrotliBitReader* br)
    {
        nuint index;
        if (s->substate_read_block_length == BROTLI_STATE_READ_BLOCK_LENGTH_NONE)
        {
            if (SafeReadSymbol(table, br, &index) == 0)
            {
                return BROTLI_FALSE;
            }
        }
        else
        {
            index = s->block_length_index;
        }
        {
            nuint bits;
            nuint nbits = _kBrotliPrefixCodeRanges[(int)index].nbits;
            nuint offset = _kBrotliPrefixCodeRanges[(int)index].offset;
            if (BrotliSafeReadBits(br, nbits, &bits) == 0)
            {
                s->block_length_index = index;
                s->substate_read_block_length = BROTLI_STATE_READ_BLOCK_LENGTH_SUFFIX;
                return BROTLI_FALSE;
            }
            *result = offset + bits;
            s->substate_read_block_length = BROTLI_STATE_READ_BLOCK_LENGTH_NONE;
            return BROTLI_TRUE;
        }
    }

    /* Transform:
        1) initialize list L with values 0, 1,... 255
        2) For each input element X:
        2.1) let Y = L[X]
        2.2) remove X-th element from L
        2.3) prepend Y to L
        2.4) append Y to output

       In most cases max(Y) <= 7, so most of L remains intact.
       To reduce the cost of initialization, we reuse L, remember the upper bound
       of Y values, and reinitialize only first elements in L.

       Most of input values are 0 and 1. To reduce number of branches, we replace
       inner for loop with do-while. */
    private static void InverseMoveToFrontTransform(
        byte* v, nuint v_len, BrotliDecoderState* state)
    {
        /* Reinitialize elements that could have been changed. */
        nuint i = 1;
        nuint upper_bound = state->mtf_upper_bound;
        uint* mtf = &state->mtf[1];  /* Make mtf[-1] addressable. */
        byte* mtf_u8 = (byte*)mtf;
        /* The C loads the {0,1,2,3} byte pattern endian-aware via memcpy; every
           dn2cpp target is little-endian (asserted in Bits), so the constant is
           inlined. */
        uint pattern = 0x03020100;

        /* Initialize list using 4 consequent values pattern. */
        mtf[0] = pattern;
        do
        {
            pattern += 0x04040404;  /* Advance all 4 values by 4. */
            mtf[i] = pattern;
            i++;
        } while (i <= upper_bound);

        /* Transform the input. */
        upper_bound = 0;
        for (i = 0; i < v_len; ++i)
        {
            int index = v[i];
            byte value = mtf_u8[index];
            upper_bound |= v[i];
            v[i] = value;
            mtf_u8[-1] = value;
            do
            {
                index--;
                mtf_u8[index + 1] = mtf_u8[index];
            } while (index >= 0);
        }
        /* Remember amount of elements to be reinitialized. */
        state->mtf_upper_bound = upper_bound >> 2;
    }

    /* Decodes a series of Huffman table using ReadHuffmanCode function. */
    private static BrotliDecoderErrorCode HuffmanTreeGroupDecode(
        HuffmanTreeGroup* group, BrotliDecoderState* s)
    {
        BrotliMetablockHeaderArena* h = &s->arena.header;
        if (h->substate_tree_group != BROTLI_STATE_TREE_GROUP_LOOP)
        {
            h->next = group->codes;
            h->htree_index = 0;
            h->substate_tree_group = BROTLI_STATE_TREE_GROUP_LOOP;
        }
        while (h->htree_index < group->num_htrees)
        {
            nuint table_size;
            BrotliDecoderErrorCode result = ReadHuffmanCode(group->alphabet_size_max,
                group->alphabet_size_limit, h->next, &table_size, s);
            if (result != BROTLI_DECODER_SUCCESS) return result;
            group->htrees[h->htree_index] = h->next;
            h->next += table_size;
            ++h->htree_index;
        }
        h->substate_tree_group = BROTLI_STATE_TREE_GROUP_NONE;
        return BROTLI_DECODER_SUCCESS;
    }

    /* Decodes a context map.
       Decoding is done in 4 phases:
        1) Read auxiliary information (6..16 bits) and allocate memory.
           In case of trivial context map, decoding is finished at this phase.
        2) Decode Huffman table using ReadHuffmanCode function.
           This table will be used for reading context map items.
        3) Read context map items; "0" values could be run-length encoded.
        4) Optionally, apply InverseMoveToFront transform to the resulting map. */
    private static BrotliDecoderErrorCode DecodeContextMap(nuint context_map_size,
        nuint* num_htrees, byte** context_map_arg, BrotliDecoderState* s)
    {
        BrotliBitReader* br = &s->br;
        BrotliDecoderErrorCode result = BROTLI_DECODER_SUCCESS;
        BrotliMetablockHeaderArena* h = &s->arena.header;

        switch (h->substate_context_map)
        {
            case BROTLI_STATE_CONTEXT_MAP_NONE:
                result = DecodeVarLenUint8(s, br, num_htrees);
                if (result != BROTLI_DECODER_SUCCESS)
                {
                    return result;
                }
                (*num_htrees)++;
                h->context_index = 0;
                *context_map_arg =
                    (byte*)BROTLI_DECODER_ALLOC(s, context_map_size);
                if (*context_map_arg == null)
                {
                    return BROTLI_FAILURE(BROTLI_DECODER_ERROR_ALLOC_CONTEXT_MAP);
                }
                if (*num_htrees <= 1)
                {
                    NativeMemory.Clear(*context_map_arg, context_map_size);
                    return BROTLI_DECODER_SUCCESS;
                }
                h->substate_context_map = BROTLI_STATE_CONTEXT_MAP_READ_PREFIX;
                /* Fall through. */
                goto case BROTLI_STATE_CONTEXT_MAP_READ_PREFIX;

            case BROTLI_STATE_CONTEXT_MAP_READ_PREFIX:
            {
                nuint bits;
                /* In next stage ReadHuffmanCode uses at least 4 bits, so it is safe
                   to peek 4 bits ahead. */
                if (BrotliSafeGetBits(br, 5, &bits) == 0)
                {
                    return BROTLI_DECODER_NEEDS_MORE_INPUT;
                }
                if ((bits & 1) != 0)
                { /* Use RLE for zeros. */
                    h->max_run_length_prefix = (bits >> 1) + 1;
                    BrotliDropBits(br, 5);
                }
                else
                {
                    h->max_run_length_prefix = 0;
                    BrotliDropBits(br, 1);
                }
                h->substate_context_map = BROTLI_STATE_CONTEXT_MAP_HUFFMAN;
            }
            /* Fall through. */
            goto case BROTLI_STATE_CONTEXT_MAP_HUFFMAN;

            case BROTLI_STATE_CONTEXT_MAP_HUFFMAN:
            {
                nuint alphabet_size = *num_htrees + h->max_run_length_prefix;
                result = ReadHuffmanCode(alphabet_size, alphabet_size,
                                         h->context_map_table, null, s);
                if (result != BROTLI_DECODER_SUCCESS) return result;
                h->code = 0xFFFF;
                h->substate_context_map = BROTLI_STATE_CONTEXT_MAP_DECODE;
            }
            /* Fall through. */
            goto case BROTLI_STATE_CONTEXT_MAP_DECODE;

            case BROTLI_STATE_CONTEXT_MAP_DECODE:
            {
                nuint context_index = h->context_index;
                nuint max_run_length_prefix = h->max_run_length_prefix;
                byte* context_map = *context_map_arg;
                nuint code = h->code;
                int skip_preamble = (code != 0xFFFF) ? BROTLI_TRUE : BROTLI_FALSE;
                while (context_index < context_map_size || skip_preamble != 0)
                {
                    if (skip_preamble == 0)
                    {
                        if (SafeReadSymbol(h->context_map_table, br, &code) == 0)
                        {
                            h->code = 0xFFFF;
                            h->context_index = context_index;
                            return BROTLI_DECODER_NEEDS_MORE_INPUT;
                        }

                        if (code == 0)
                        {
                            context_map[context_index++] = 0;
                            continue;
                        }
                        if (code > max_run_length_prefix)
                        {
                            context_map[context_index++] =
                                (byte)(code - max_run_length_prefix);
                            continue;
                        }
                    }
                    else
                    {
                        skip_preamble = BROTLI_FALSE;
                    }
                    /* RLE sub-stage. */
                    {
                        nuint reps;
                        if (BrotliSafeReadBits(br, code, &reps) == 0)
                        {
                            h->code = code;
                            h->context_index = context_index;
                            return BROTLI_DECODER_NEEDS_MORE_INPUT;
                        }
                        reps += (nuint)1u << (int)code;
                        if (context_index + reps > context_map_size)
                        {
                            return
                                BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_CONTEXT_MAP_REPEAT);
                        }
                        do
                        {
                            context_map[context_index++] = 0;
                        } while (--reps != 0);
                    }
                }
            }
            /* Fall through. */
            goto case BROTLI_STATE_CONTEXT_MAP_TRANSFORM;

            case BROTLI_STATE_CONTEXT_MAP_TRANSFORM:
            {
                nuint bits;
                if (BrotliSafeReadBits(br, 1, &bits) == 0)
                {
                    h->substate_context_map = BROTLI_STATE_CONTEXT_MAP_TRANSFORM;
                    return BROTLI_DECODER_NEEDS_MORE_INPUT;
                }
                if (bits != 0)
                {
                    InverseMoveToFrontTransform(*context_map_arg, context_map_size, s);
                }
                h->substate_context_map = BROTLI_STATE_CONTEXT_MAP_NONE;
                return BROTLI_DECODER_SUCCESS;
            }

            default:
                return
                    BROTLI_FAILURE(BROTLI_DECODER_ERROR_UNREACHABLE);  /* COV_NF_LINE */
        }
    }

    /* Decodes a command or literal and updates block type ring-buffer.
       Reads 3..54 bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeBlockTypeAndLength(
        int safe, BrotliDecoderState* s, int tree_type)
    {
        nuint max_block_type = (nuint)s->num_block_types[tree_type];
        HuffmanCode* type_tree = &s->block_type_trees[
            tree_type * BROTLI_HUFFMAN_MAX_SIZE_258];
        HuffmanCode* len_tree = &s->block_len_trees[
            tree_type * BROTLI_HUFFMAN_MAX_SIZE_26];
        BrotliBitReader* br = &s->br;
        nuint* ringbuffer = (nuint*)&s->block_type_rb[tree_type * 2];
        nuint block_type;
        if (max_block_type <= 1)
        {
            return BROTLI_FALSE;
        }

        /* Read 0..15 + 3..39 bits. */
        if (safe == 0)
        {
            block_type = ReadSymbol(type_tree, br);
            s->block_length[tree_type] = ReadBlockLength(len_tree, br);
        }
        else
        {
            BrotliBitReaderState memento;
            BrotliBitReaderSaveState(br, &memento);
            if (SafeReadSymbol(type_tree, br, &block_type) == 0) return BROTLI_FALSE;
            if (SafeReadBlockLength(s, (nuint*)&s->block_length[tree_type], len_tree, br) == 0)
            {
                s->substate_read_block_length = BROTLI_STATE_READ_BLOCK_LENGTH_NONE;
                BrotliBitReaderRestoreState(br, &memento);
                return BROTLI_FALSE;
            }
        }

        if (block_type == 1)
        {
            block_type = ringbuffer[1] + 1;
        }
        else if (block_type == 0)
        {
            block_type = ringbuffer[0];
        }
        else
        {
            block_type -= 2;
        }
        if (block_type >= max_block_type)
        {
            block_type -= max_block_type;
        }
        ringbuffer[0] = ringbuffer[1];
        ringbuffer[1] = block_type;
        return BROTLI_TRUE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void DetectTrivialLiteralBlockTypes(BrotliDecoderState* s)
    {
        nuint i;
        for (i = 0; i < 8; ++i) s->trivial_literal_contexts[i] = 0;
        for (i = 0; i < (nuint)s->num_block_types[0]; i++)
        {
            nuint offset = i << BROTLI_LITERAL_CONTEXT_BITS;
            nuint error = 0;
            nuint sample = s->context_map[offset];
            nuint j;
            for (j = 0; j < (1u << BROTLI_LITERAL_CONTEXT_BITS);)
            {
                /* BROTLI_REPEAT_4({ error |= s->context_map[offset + j++] ^ sample; }) */
                error |= s->context_map[offset + j++] ^ sample;
                error |= s->context_map[offset + j++] ^ sample;
                error |= s->context_map[offset + j++] ^ sample;
                error |= s->context_map[offset + j++] ^ sample;
            }
            if (error == 0)
            {
                s->trivial_literal_contexts[i >> 5] |= 1u << (int)(i & 31);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PrepareLiteralDecoding(BrotliDecoderState* s)
    {
        byte context_mode;
        nuint trivial;
        nuint block_type = (nuint)s->block_type_rb[1];
        nuint context_offset = block_type << BROTLI_LITERAL_CONTEXT_BITS;
        s->context_map_slice = s->context_map + context_offset;
        trivial = s->trivial_literal_contexts[block_type >> 5];
        s->trivial_literal_context = (int)((trivial >> (int)(block_type & 31)) & 1);
        s->literal_htree = s->literal_hgroup.htrees[s->context_map_slice[0]];
        context_mode = (byte)(s->context_modes[block_type] & 3);
        s->context_lookup = BROTLI_CONTEXT_LUT(context_mode);
    }

    /* Decodes the block type and updates the state for literal context.
       Reads 3..54 bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeLiteralBlockSwitchInternal(
        int safe, BrotliDecoderState* s)
    {
        if (DecodeBlockTypeAndLength(safe, s, 0) == 0)
        {
            return BROTLI_FALSE;
        }
        PrepareLiteralDecoding(s);
        return BROTLI_TRUE;
    }

    private static void DecodeLiteralBlockSwitch(BrotliDecoderState* s)
    {
        DecodeLiteralBlockSwitchInternal(0, s);
    }

    private static int SafeDecodeLiteralBlockSwitch(BrotliDecoderState* s)
    {
        return DecodeLiteralBlockSwitchInternal(1, s);
    }

    /* Block switch for insert/copy length.
       Reads 3..54 bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeCommandBlockSwitchInternal(
        int safe, BrotliDecoderState* s)
    {
        if (DecodeBlockTypeAndLength(safe, s, 1) == 0)
        {
            return BROTLI_FALSE;
        }
        s->htree_command = s->insert_copy_hgroup.htrees[s->block_type_rb[3]];
        return BROTLI_TRUE;
    }

    private static void DecodeCommandBlockSwitch(BrotliDecoderState* s)
    {
        DecodeCommandBlockSwitchInternal(0, s);
    }

    private static int SafeDecodeCommandBlockSwitch(BrotliDecoderState* s)
    {
        return DecodeCommandBlockSwitchInternal(1, s);
    }

    /* Block switch for distance codes.
       Reads 3..54 bits. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DecodeDistanceBlockSwitchInternal(
        int safe, BrotliDecoderState* s)
    {
        if (DecodeBlockTypeAndLength(safe, s, 2) == 0)
        {
            return BROTLI_FALSE;
        }
        s->dist_context_map_slice = s->dist_context_map +
            ((int)s->block_type_rb[5] << BROTLI_DISTANCE_CONTEXT_BITS);
        s->dist_htree_index = s->dist_context_map_slice[s->distance_context];
        return BROTLI_TRUE;
    }

    private static void DecodeDistanceBlockSwitch(BrotliDecoderState* s)
    {
        DecodeDistanceBlockSwitchInternal(0, s);
    }

    private static int SafeDecodeDistanceBlockSwitch(BrotliDecoderState* s)
    {
        return DecodeDistanceBlockSwitchInternal(1, s);
    }

    private static nuint UnwrittenBytes(BrotliDecoderState* s, int wrap)
    {
        nuint pos = (wrap != 0 && s->pos > s->ringbuffer_size) ?
            (nuint)s->ringbuffer_size : (nuint)s->pos;
        nuint partial_pos_rb = (s->rb_roundtrips * (nuint)s->ringbuffer_size) + pos;
        return partial_pos_rb - s->partial_pos_out;
    }

    /* Dumps output.
       Returns BROTLI_DECODER_NEEDS_MORE_OUTPUT only if there is more output to push
       and either ring-buffer is as big as window size, or |force| is true. */
    private static BrotliDecoderErrorCode WriteRingBuffer(
        BrotliDecoderState* s, nuint* available_out, byte** next_out,
        nuint* total_out, int force)
    {
        byte* start =
            s->ringbuffer + (s->partial_pos_out & (nuint)s->ringbuffer_mask);
        nuint to_write = UnwrittenBytes(s, BROTLI_TRUE);
        nuint num_written = *available_out;
        if (num_written > to_write)
        {
            num_written = to_write;
        }
        if (s->meta_block_remaining_len < 0)
        {
            return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_BLOCK_LENGTH_1);
        }
        if (next_out != null && *next_out == null)
        {
            *next_out = start;
        }
        else
        {
            if (next_out != null)
            {
                Buffer.MemoryCopy(start, *next_out, num_written, num_written);  /* memcpy */
                *next_out += num_written;
            }
        }
        *available_out -= num_written;
        s->partial_pos_out += num_written;
        if (total_out != null)
        {
            *total_out = s->partial_pos_out;
        }
        if (num_written < to_write)
        {
            if (s->ringbuffer_size == (1 << (int)s->window_bits) || force != 0)
            {
                return BROTLI_DECODER_NEEDS_MORE_OUTPUT;
            }
            else
            {
                return BROTLI_DECODER_SUCCESS;
            }
        }
        /* Wrap ring buffer only if it has reached its maximal size. */
        if (s->ringbuffer_size == (1 << (int)s->window_bits) &&
            s->pos >= s->ringbuffer_size)
        {
            s->pos -= s->ringbuffer_size;
            s->rb_roundtrips++;
            s->should_wrap_ringbuffer = (nuint)s->pos != 0 ? 1u : 0u;
        }
        return BROTLI_DECODER_SUCCESS;
    }

    private static void WrapRingBuffer(BrotliDecoderState* s)
    {
        if (s->should_wrap_ringbuffer != 0)
        {
            Buffer.MemoryCopy(s->ringbuffer_end, s->ringbuffer,
                (long)s->pos, (long)s->pos);  /* memcpy */
            s->should_wrap_ringbuffer = 0;
        }
    }

    /* Allocates ring-buffer.

       s->ringbuffer_size MUST be updated by BrotliCalculateRingBufferSize before
       this function is called.

       Last two bytes of ring-buffer are initialized to 0, so context calculation
       could be done uniformly for the first two and all other positions. */
    private static int BrotliEnsureRingBuffer(BrotliDecoderState* s)
    {
        byte* old_ringbuffer = s->ringbuffer;
        if (s->ringbuffer_size == s->new_ringbuffer_size)
        {
            return BROTLI_TRUE;
        }

        s->ringbuffer = (byte*)BROTLI_DECODER_ALLOC(s,
            (nuint)s->new_ringbuffer_size + kRingBufferWriteAheadSlack);
        if (s->ringbuffer == null)
        {
            /* Restore previous value. */
            s->ringbuffer = old_ringbuffer;
            return BROTLI_FALSE;
        }
        s->ringbuffer[s->new_ringbuffer_size - 2] = 0;
        s->ringbuffer[s->new_ringbuffer_size - 1] = 0;

        if (old_ringbuffer != null)
        {
            Buffer.MemoryCopy(old_ringbuffer, s->ringbuffer,
                (long)s->pos, (long)s->pos);  /* memcpy */
            BROTLI_DECODER_FREE(s, ref old_ringbuffer);
        }

        s->ringbuffer_size = s->new_ringbuffer_size;
        s->ringbuffer_mask = s->new_ringbuffer_size - 1;
        s->ringbuffer_end = s->ringbuffer + s->ringbuffer_size;

        return BROTLI_TRUE;
    }

    private static BrotliDecoderErrorCode SkipMetadataBlock(BrotliDecoderState* s)
    {
        BrotliBitReader* br = &s->br;

        if (s->meta_block_remaining_len == 0)
        {
            return BROTLI_DECODER_SUCCESS;
        }

        /* BROTLI_DCHECK((BrotliGetAvailableBits(br) & 7) == 0); */

        /* Drain accumulator. */
        if (BrotliGetAvailableBits(br) >= 8)
        {
            byte* buffer = stackalloc byte[8];
            int nbytes = (int)BrotliGetAvailableBits(br) >> 3;
            if (nbytes > s->meta_block_remaining_len)
            {
                nbytes = s->meta_block_remaining_len;
            }
            BrotliCopyBytes(buffer, br, (nuint)nbytes);
            /* if (s->metadata_chunk_func) s->metadata_chunk_func(...); — metadata
               callbacks are not supported in the port (layout-only fields). */
            s->meta_block_remaining_len -= nbytes;
            if (s->meta_block_remaining_len == 0)
            {
                return BROTLI_DECODER_SUCCESS;
            }
        }

        /* Direct access to metadata is possible. */
        {
            int nbytes = (int)BrotliGetRemainingBytes(br);
            if (nbytes > s->meta_block_remaining_len)
            {
                nbytes = s->meta_block_remaining_len;
            }
            if (nbytes > 0)
            {
                /* if (s->metadata_chunk_func) s->metadata_chunk_func(...); */
                BrotliDropBytes(br, (nuint)nbytes);
                s->meta_block_remaining_len -= nbytes;
                if (s->meta_block_remaining_len == 0)
                {
                    return BROTLI_DECODER_SUCCESS;
                }
            }
        }

        return BROTLI_DECODER_NEEDS_MORE_INPUT;
    }

    private static BrotliDecoderErrorCode CopyUncompressedBlockToOutput(
        nuint* available_out, byte** next_out, nuint* total_out,
        BrotliDecoderState* s)
    {
        /* TODO(eustas): avoid allocation for single uncompressed block. */
        if (BrotliEnsureRingBuffer(s) == 0)
        {
            return BROTLI_FAILURE(BROTLI_DECODER_ERROR_ALLOC_RING_BUFFER_1);
        }

        /* State machine */
        for (;;)
        {
            switch (s->substate_uncompressed)
            {
                case BROTLI_STATE_UNCOMPRESSED_NONE:
                {
                    int nbytes = (int)BrotliGetRemainingBytes(&s->br);
                    if (nbytes > s->meta_block_remaining_len)
                    {
                        nbytes = s->meta_block_remaining_len;
                    }
                    if (s->pos + nbytes > s->ringbuffer_size)
                    {
                        nbytes = s->ringbuffer_size - s->pos;
                    }
                    /* Copy remaining bytes from s->br.buf_ to ring-buffer. */
                    BrotliCopyBytes(&s->ringbuffer[s->pos], &s->br, (nuint)nbytes);
                    s->pos += nbytes;
                    s->meta_block_remaining_len -= nbytes;
                    if (s->pos < 1 << (int)s->window_bits)
                    {
                        if (s->meta_block_remaining_len == 0)
                        {
                            return BROTLI_DECODER_SUCCESS;
                        }
                        return BROTLI_DECODER_NEEDS_MORE_INPUT;
                    }
                    s->substate_uncompressed = BROTLI_STATE_UNCOMPRESSED_WRITE;
                }
                /* Fall through. */
                goto case BROTLI_STATE_UNCOMPRESSED_WRITE;

                case BROTLI_STATE_UNCOMPRESSED_WRITE:
                {
                    BrotliDecoderErrorCode result;
                    result = WriteRingBuffer(
                        s, available_out, next_out, total_out, BROTLI_FALSE);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        return result;
                    }
                    if (s->ringbuffer_size == 1 << (int)s->window_bits)
                    {
                        s->max_distance = s->max_backward_distance;
                    }
                    s->substate_uncompressed = BROTLI_STATE_UNCOMPRESSED_NONE;
                    break;
                }
            }
        }
    }

    private static int AttachCompoundDictionary(
        BrotliDecoderState* state, byte* data, nuint size)
    {
        BrotliDecoderCompoundDictionary* addon = state->compound_dictionary;
        if (state->state != BROTLI_STATE_UNINITED) return BROTLI_FALSE;
        if (addon == null)
        {
            addon = (BrotliDecoderCompoundDictionary*)BROTLI_DECODER_ALLOC(
                state, (nuint)sizeof(BrotliDecoderCompoundDictionary));
            if (addon == null) return BROTLI_FALSE;
            addon->num_chunks = 0;
            addon->total_size = 0;
            addon->br_length = 0;
            addon->br_copied = 0;
            addon->block_bits = -1;
            addon->chunk_offsets[0] = 0;
            state->compound_dictionary = addon;
        }
        if (addon->num_chunks == 15) return BROTLI_FALSE;
        addon->set_chunks(addon->num_chunks, data);
        addon->num_chunks++;
        addon->total_size += (int)size;
        addon->chunk_offsets[addon->num_chunks] = addon->total_size;
        return BROTLI_TRUE;
    }

    /* NB: the C function name typo ("Coumpound") is preserved. */
    private static void EnsureCoumpoundDictionaryInitialized(BrotliDecoderState* state)
    {
        BrotliDecoderCompoundDictionary* addon = state->compound_dictionary;
        /* 256 = (1 << 8) slots in block map. */
        int block_bits = 8;
        int cursor = 0;
        int index = 0;
        if (addon->block_bits != -1) return;
        while (((addon->total_size - 1) >> block_bits) != 0) block_bits++;
        block_bits -= 8;
        addon->block_bits = block_bits;
        while (cursor < addon->total_size)
        {
            while (addon->chunk_offsets[index + 1] < cursor) index++;
            addon->block_map[cursor >> block_bits] = (byte)index;
            cursor += 1 << block_bits;
        }
    }

    private static int InitializeCompoundDictionaryCopy(BrotliDecoderState* s,
        int address, int length)
    {
        BrotliDecoderCompoundDictionary* addon = s->compound_dictionary;
        int index;
        EnsureCoumpoundDictionaryInitialized(s);
        index = addon->block_map[address >> addon->block_bits];
        while (address >= addon->chunk_offsets[index + 1]) index++;
        if (addon->total_size < address + length) return BROTLI_FALSE;
        /* Update the recent distances cache. */
        s->dist_rb[s->dist_rb_idx & 3] = s->distance_code;
        ++s->dist_rb_idx;
        s->meta_block_remaining_len -= length;
        addon->br_index = index;
        addon->br_offset = address - addon->chunk_offsets[index];
        addon->br_length = length;
        addon->br_copied = 0;
        return BROTLI_TRUE;
    }

    private static int GetCompoundDictionarySize(BrotliDecoderState* s)
    {
        return s->compound_dictionary != null ? s->compound_dictionary->total_size : 0;
    }

    private static int CopyFromCompoundDictionary(BrotliDecoderState* s, int pos)
    {
        BrotliDecoderCompoundDictionary* addon = s->compound_dictionary;
        int orig_pos = pos;
        while (addon->br_length != addon->br_copied)
        {
            byte* copy_dst = &s->ringbuffer[pos];
            byte* copy_src =
                addon->chunks(addon->br_index) + addon->br_offset;
            int space = s->ringbuffer_size - pos;
            int rem_chunk_length = (addon->chunk_offsets[addon->br_index + 1] -
                addon->chunk_offsets[addon->br_index]) - addon->br_offset;
            int length = addon->br_length - addon->br_copied;
            if (length > rem_chunk_length) length = rem_chunk_length;
            if (length > space) length = space;
            Buffer.MemoryCopy(copy_src, copy_dst, length, length);  /* memcpy */
            pos += length;
            addon->br_offset += length;
            addon->br_copied += length;
            if (length == rem_chunk_length)
            {
                addon->br_index++;
                addon->br_offset = 0;
            }
            if (pos == s->ringbuffer_size) break;
        }
        return pos - orig_pos;
    }

    /// <summary><c>BrotliDecoderAttachDictionary</c>.</summary>
    internal static int BrotliDecoderAttachDictionary(
        BrotliDecoderState* state, BrotliSharedDictionaryType type,
        nuint data_size, byte* data)
    {
        uint i;
        uint num_prefix_before = state->dictionary->num_prefix;
        if (state->state != BROTLI_STATE_UNINITED) return BROTLI_FALSE;
        if (BrotliSharedDictionaryAttach(state->dictionary, type, data_size, data) == 0)
        {
            return BROTLI_FALSE;
        }
        for (i = num_prefix_before; i < state->dictionary->num_prefix; i++)
        {
            if (AttachCompoundDictionary(
                state, state->dictionary->prefix((int)i),
                (nuint)state->dictionary->prefix_size[i]) == 0)
            {
                return BROTLI_FALSE;
            }
        }
        return BROTLI_TRUE;
    }

    /* Calculates the smallest feasible ring buffer.

       If we know the data size is small, do not allocate more ring buffer
       size than needed to reduce memory usage.

       When this method is called, metablock size and flags MUST be decoded. */
    private static void BrotliCalculateRingBufferSize(BrotliDecoderState* s)
    {
        int window_size = 1 << (int)s->window_bits;
        int new_ringbuffer_size = window_size;
        /* We need at least 2 bytes of ring buffer size to get the last two
           bytes for context from there */
        int min_size = s->ringbuffer_size != 0 ? s->ringbuffer_size : 1024;
        int output_size;

        /* If maximum is already reached, no further extension is retired. */
        if (s->ringbuffer_size == window_size)
        {
            return;
        }

        /* Metadata blocks does not touch ring buffer. */
        if (s->is_metadata != 0)
        {
            return;
        }

        if (s->ringbuffer == null)
        {
            output_size = 0;
        }
        else
        {
            output_size = s->pos;
        }
        output_size += s->meta_block_remaining_len;
        min_size = min_size < output_size ? output_size : min_size;

        if (s->canny_ringbuffer_allocation != 0)
        {
            /* Reduce ring buffer size to save memory when server is unscrupulous.
               In worst case memory usage might be 1.5x bigger for a short period of
               ring buffer reallocation. */
            while ((new_ringbuffer_size >> 1) >= min_size)
            {
                new_ringbuffer_size >>= 1;
            }
        }

        s->new_ringbuffer_size = new_ringbuffer_size;
    }

    /* Reads 1..256 2-bit context modes. */
    private static BrotliDecoderErrorCode ReadContextModes(BrotliDecoderState* s)
    {
        BrotliBitReader* br = &s->br;
        int i = s->loop_counter;

        while (i < (int)s->num_block_types[0])
        {
            nuint bits;
            if (BrotliSafeReadBits(br, 2, &bits) == 0)
            {
                s->loop_counter = i;
                return BROTLI_DECODER_NEEDS_MORE_INPUT;
            }
            s->context_modes[i] = (byte)bits;
            i++;
        }
        return BROTLI_DECODER_SUCCESS;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void TakeDistanceFromRingBuffer(BrotliDecoderState* s)
    {
        int offset = s->distance_code - 3;
        if (s->distance_code <= 3)
        {
            /* Compensate double distance-ring-buffer roll for dictionary items. */
            s->distance_context = 1 >> s->distance_code;
            s->distance_code = s->dist_rb[(s->dist_rb_idx - offset) & 3];
            s->dist_rb_idx -= s->distance_context;
        }
        else
        {
            int index_delta = 3;
            int delta;
            int base_ = s->distance_code - 10;
            if (s->distance_code < 10)
            {
                base_ = s->distance_code - 4;
            }
            else
            {
                index_delta = 2;
            }
            /* Unpack one of six 4-bit values. */
            delta = ((0x605142 >> (4 * base_)) & 0xF) - 3;
            s->distance_code = s->dist_rb[(s->dist_rb_idx + index_delta) & 0x3] + delta;
            if (s->distance_code <= 0)
            {
                /* A huge distance will cause a BROTLI_FAILURE() soon.
                   This is a little faster than failing here. */
                s->distance_code = 0x7FFFFFFF;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SafeReadBits(
        BrotliBitReader* br, nuint n_bits, nuint* val)
    {
        if (n_bits != 0)
        {
            return BrotliSafeReadBits(br, n_bits, val);
        }
        else
        {
            *val = 0;
            return BROTLI_TRUE;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SafeReadBits32(
        BrotliBitReader* br, nuint n_bits, nuint* val)
    {
        if (n_bits != 0)
        {
            return BrotliSafeReadBits32(br, n_bits, val);
        }
        else
        {
            *val = 0;
            return BROTLI_TRUE;
        }
    }

    /* Calculates distance lookup table.
       NB: it is possible to have all 64 tables precalculated. */
    private static void CalculateDistanceLut(BrotliDecoderState* s)
    {
        BrotliMetablockBodyArena* b = &s->arena.body;
        nuint npostfix = s->distance_postfix_bits;
        nuint ndirect = s->num_direct_distance_codes;
        nuint alphabet_size_limit = s->distance_hgroup.alphabet_size_limit;
        nuint postfix = (nuint)1u << (int)npostfix;
        nuint j;
        nuint bits = 1;
        nuint half = 0;

        /* Skip short codes. */
        nuint i = BROTLI_NUM_DISTANCE_SHORT_CODES;

        /* Fill direct codes. */
        for (j = 0; j < ndirect; ++j)
        {
            b->dist_extra_bits[i] = 0;
            b->dist_offset[i] = j + 1;
            ++i;
        }

        /* Fill regular distance codes. */
        while (i < alphabet_size_limit)
        {
            nuint base_ = ndirect + ((((2 + half) << (int)bits) - 4) << (int)npostfix) + 1;
            /* Always fill the complete group. */
            for (j = 0; j < postfix; ++j)
            {
                b->dist_extra_bits[i] = (byte)bits;
                b->dist_offset[i] = base_ + j;
                ++i;
            }
            bits = bits + half;
            half = half ^ 1;
        }
    }

    /* Precondition: s->distance_code < 0. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadDistanceInternal(
        int safe, BrotliDecoderState* s, BrotliBitReader* br)
    {
        BrotliMetablockBodyArena* b = &s->arena.body;
        nuint code;
        nuint bits;
        BrotliBitReaderState memento;
        HuffmanCode* distance_tree = s->distance_hgroup.htrees[s->dist_htree_index];
        if (safe == 0)
        {
            code = ReadSymbol(distance_tree, br);
        }
        else
        {
            BrotliBitReaderSaveState(br, &memento);
            if (SafeReadSymbol(distance_tree, br, &code) == 0)
            {
                return BROTLI_FALSE;
            }
        }
        --s->block_length[2];
        /* Convert the distance code to the actual distance by possibly
           looking up past distances from the s->dist_rb. */
        s->distance_context = 0;
        if ((code & ~0xFu) == 0)
        {
            s->distance_code = (int)code;
            TakeDistanceFromRingBuffer(s);
            return BROTLI_TRUE;
        }
        if (safe == 0)
        {
            bits = BrotliReadBits32(br, b->dist_extra_bits[code]);
        }
        else
        {
            if (SafeReadBits32(br, b->dist_extra_bits[code], &bits) == 0)
            {
                ++s->block_length[2];
                BrotliBitReaderRestoreState(br, &memento);
                return BROTLI_FALSE;
            }
        }
        s->distance_code =
            (int)(b->dist_offset[code] + (ulong)(bits << (int)s->distance_postfix_bits));
        return BROTLI_TRUE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReadDistance(BrotliDecoderState* s, BrotliBitReader* br)
    {
        ReadDistanceInternal(0, s, br);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SafeReadDistance(BrotliDecoderState* s, BrotliBitReader* br)
    {
        return ReadDistanceInternal(1, s, br);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadCommandInternal(
        int safe, BrotliDecoderState* s, BrotliBitReader* br, int* insert_length)
    {
        nuint cmd_code;
        nuint insert_len_extra = 0;
        nuint copy_length;
        CmdLutElement v;
        BrotliBitReaderState memento;
        if (safe == 0)
        {
            cmd_code = ReadSymbol(s->htree_command, br);
        }
        else
        {
            BrotliBitReaderSaveState(br, &memento);
            if (SafeReadSymbol(s->htree_command, br, &cmd_code) == 0)
            {
                return BROTLI_FALSE;
            }
        }
        v = kCmdLut[cmd_code];
        s->distance_code = v.distance_code;
        s->distance_context = v.context;
        s->dist_htree_index = s->dist_context_map_slice[s->distance_context];
        *insert_length = v.insert_len_offset;
        if (safe == 0)
        {
            if (v.insert_len_extra_bits != 0)
            {
                insert_len_extra = BrotliReadBits24(br, v.insert_len_extra_bits);
            }
            copy_length = BrotliReadBits24(br, v.copy_len_extra_bits);
        }
        else
        {
            if (SafeReadBits(br, v.insert_len_extra_bits, &insert_len_extra) == 0 ||
                SafeReadBits(br, v.copy_len_extra_bits, &copy_length) == 0)
            {
                BrotliBitReaderRestoreState(br, &memento);
                return BROTLI_FALSE;
            }
        }
        s->copy_length = (int)copy_length + v.copy_len_offset;
        --s->block_length[1];
        *insert_length += (int)insert_len_extra;
        return BROTLI_TRUE;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReadCommand(
        BrotliDecoderState* s, BrotliBitReader* br, int* insert_length)
    {
        ReadCommandInternal(0, s, br, insert_length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SafeReadCommand(
        BrotliDecoderState* s, BrotliBitReader* br, int* insert_length)
    {
        return ReadCommandInternal(1, s, br, insert_length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CheckInputAmount(int safe, BrotliBitReader* br)
    {
        if (safe != 0)
        {
            return BROTLI_TRUE;
        }
        return BrotliCheckInputAmount(br);
    }

    /* The BROTLI_SAFE(METHOD) macro of the C is expanded inline at every call
       site below, exactly as the preprocessor does. */
    private static BrotliDecoderErrorCode ProcessCommandsInternal(
        int safe, BrotliDecoderState* s)
    {
        int pos = s->pos;
        int i = s->loop_counter;
        BrotliDecoderErrorCode result = BROTLI_DECODER_SUCCESS;
        BrotliBitReader* br = &s->br;
        int compound_dictionary_size = GetCompoundDictionarySize(s);

        if (CheckInputAmount(safe, br) == 0)
        {
            result = BROTLI_DECODER_NEEDS_MORE_INPUT;
            goto saveStateAndReturn;
        }
        if (safe == 0)
        {
            _ = BrotliWarmupBitReader(br);  /* BROTLI_UNUSED */
        }

        /* Jump into state machine. */
        if (s->state == BROTLI_STATE_COMMAND_BEGIN)
        {
            goto CommandBegin;
        }
        else if (s->state == BROTLI_STATE_COMMAND_INNER)
        {
            goto CommandInner;
        }
        else if (s->state == BROTLI_STATE_COMMAND_POST_DECODE_LITERALS)
        {
            goto CommandPostDecodeLiterals;
        }
        else if (s->state == BROTLI_STATE_COMMAND_POST_WRAP_COPY)
        {
            goto CommandPostWrapCopy;
        }
        else
        {
            return BROTLI_FAILURE(BROTLI_DECODER_ERROR_UNREACHABLE);  /* COV_NF_LINE */
        }

    CommandBegin:
        if (safe != 0)
        {
            s->state = BROTLI_STATE_COMMAND_BEGIN;
        }
        if (CheckInputAmount(safe, br) == 0)
        {
            s->state = BROTLI_STATE_COMMAND_BEGIN;
            result = BROTLI_DECODER_NEEDS_MORE_INPUT;
            goto saveStateAndReturn;
        }
        if (s->block_length[1] == 0)
        {
            /* BROTLI_SAFE(DecodeCommandBlockSwitch(s)); */
            if (safe != 0)
            {
                if (SafeDecodeCommandBlockSwitch(s) == 0)
                {
                    result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                    goto saveStateAndReturn;
                }
            }
            else
            {
                DecodeCommandBlockSwitch(s);
            }
            goto CommandBegin;
        }
        /* Read the insert/copy length in the command. */
        /* BROTLI_SAFE(ReadCommand(s, br, &i)); */
        if (safe != 0)
        {
            if (SafeReadCommand(s, br, &i) == 0)
            {
                result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                goto saveStateAndReturn;
            }
        }
        else
        {
            ReadCommand(s, br, &i);
        }
        if (i == 0)
        {
            goto CommandPostDecodeLiterals;
        }
        s->meta_block_remaining_len -= i;

    CommandInner:
        if (safe != 0)
        {
            s->state = BROTLI_STATE_COMMAND_INNER;
        }
        /* Read the literals in the command. */
        if (s->trivial_literal_context != 0)
        {
            nuint bits;
            nuint value;
            PreloadSymbol(safe, s->literal_htree, br, &bits, &value);
            do
            {
                if (CheckInputAmount(safe, br) == 0)
                {
                    s->state = BROTLI_STATE_COMMAND_INNER;
                    result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                    goto saveStateAndReturn;
                }
                if (s->block_length[0] == 0)
                {
                    goto NextLiteralBlock;
                }
                if (safe == 0)
                {
                    s->ringbuffer[pos] =
                        (byte)ReadPreloadedSymbol(s->literal_htree, br, &bits, &value);
                }
                else
                {
                    nuint literal;
                    if (SafeReadSymbol(s->literal_htree, br, &literal) == 0)
                    {
                        result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                        goto saveStateAndReturn;
                    }
                    s->ringbuffer[pos] = (byte)literal;
                }
                --s->block_length[0];
                ++pos;
                if (pos == s->ringbuffer_size)
                {
                    s->state = BROTLI_STATE_COMMAND_INNER_WRITE;
                    --i;
                    goto saveStateAndReturn;
                }
            } while (--i != 0);
        }
        else
        {
            byte p1 = s->ringbuffer[(pos - 1) & s->ringbuffer_mask];
            byte p2 = s->ringbuffer[(pos - 2) & s->ringbuffer_mask];
            do
            {
                HuffmanCode* hc;
                byte context;
                if (CheckInputAmount(safe, br) == 0)
                {
                    s->state = BROTLI_STATE_COMMAND_INNER;
                    result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                    goto saveStateAndReturn;
                }
                if (s->block_length[0] == 0)
                {
                    goto NextLiteralBlock;
                }
                context = BROTLI_CONTEXT(p1, p2, s->context_lookup);
                hc = s->literal_hgroup.htrees[s->context_map_slice[context]];
                p2 = p1;
                if (safe == 0)
                {
                    p1 = (byte)ReadSymbol(hc, br);
                }
                else
                {
                    nuint literal;
                    if (SafeReadSymbol(hc, br, &literal) == 0)
                    {
                        result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                        goto saveStateAndReturn;
                    }
                    p1 = (byte)literal;
                }
                s->ringbuffer[pos] = p1;
                --s->block_length[0];
                ++pos;
                if (pos == s->ringbuffer_size)
                {
                    s->state = BROTLI_STATE_COMMAND_INNER_WRITE;
                    --i;
                    goto saveStateAndReturn;
                }
            } while (--i != 0);
        }
        if (s->meta_block_remaining_len <= 0)
        {
            s->state = BROTLI_STATE_METABLOCK_DONE;
            goto saveStateAndReturn;
        }

    CommandPostDecodeLiterals:
        if (safe != 0)
        {
            s->state = BROTLI_STATE_COMMAND_POST_DECODE_LITERALS;
        }
        if (s->distance_code >= 0)
        {
            /* Implicit distance case. */
            s->distance_context = s->distance_code != 0 ? 0 : 1;
            --s->dist_rb_idx;
            s->distance_code = s->dist_rb[s->dist_rb_idx & 3];
        }
        else
        {
            /* Read distance code in the command, unless it was implicitly zero. */
            if (s->block_length[2] == 0)
            {
                /* BROTLI_SAFE(DecodeDistanceBlockSwitch(s)); */
                if (safe != 0)
                {
                    if (SafeDecodeDistanceBlockSwitch(s) == 0)
                    {
                        result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                        goto saveStateAndReturn;
                    }
                }
                else
                {
                    DecodeDistanceBlockSwitch(s);
                }
            }
            /* BROTLI_SAFE(ReadDistance(s, br)); */
            if (safe != 0)
            {
                if (SafeReadDistance(s, br) == 0)
                {
                    result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                    goto saveStateAndReturn;
                }
            }
            else
            {
                ReadDistance(s, br);
            }
        }
        if (s->max_distance != s->max_backward_distance)
        {
            s->max_distance =
                (pos < s->max_backward_distance) ? pos : s->max_backward_distance;
        }
        i = s->copy_length;
        /* Apply copy of LZ77 back-reference, or static dictionary reference if
           the distance is larger than the max LZ77 distance */
        if (s->distance_code > s->max_distance)
        {
            /* The maximum allowed distance is BROTLI_MAX_ALLOWED_DISTANCE = 0x7FFFFFFC.
               With this choice, no signed overflow can occur after decoding
               a special distance code (e.g., after adding 3 to the last distance). */
            if (s->distance_code > BROTLI_MAX_ALLOWED_DISTANCE)
            {
                return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_DISTANCE);
            }
            if (s->distance_code - s->max_distance - 1 < compound_dictionary_size)
            {
                int address = compound_dictionary_size -
                    (s->distance_code - s->max_distance);
                if (InitializeCompoundDictionaryCopy(s, address, i) == 0)
                {
                    return BROTLI_FAILURE(BROTLI_DECODER_ERROR_COMPOUND_DICTIONARY);
                }
                pos += CopyFromCompoundDictionary(s, pos);
                if (pos >= s->ringbuffer_size)
                {
                    s->state = BROTLI_STATE_COMMAND_POST_WRITE_1;
                    goto saveStateAndReturn;
                }
            }
            else if (i >= SHARED_BROTLI_MIN_DICTIONARY_WORD_LENGTH &&
                     i <= SHARED_BROTLI_MAX_DICTIONARY_WORD_LENGTH)
            {
                byte p1 = s->ringbuffer[(pos - 1) & s->ringbuffer_mask];
                byte p2 = s->ringbuffer[(pos - 2) & s->ringbuffer_mask];
                byte dict_id = s->dictionary->context_based != 0 ?
                    s->dictionary->context_map[BROTLI_CONTEXT(p1, p2, s->context_lookup)]
                    : (byte)0;
                BrotliDictionary* words = s->dictionary->words(dict_id);
                BrotliTransforms* transforms = s->dictionary->transforms(dict_id);
                int offset = (int)words->offsets_by_length[i];
                nuint shift = words->size_bits_by_length[i];
                int address =
                    s->distance_code - s->max_distance - 1 - compound_dictionary_size;
                int mask = (int)BitMask(shift);
                int word_idx = address & mask;
                int transform_idx = address >> (int)shift;
                /* Compensate double distance-ring-buffer roll. */
                s->dist_rb_idx += s->distance_context;
                offset += word_idx * i;
                /* If the distance is out of bound, select a next static dictionary if
                   there exist multiple. */
                if ((transform_idx >= (int)transforms->num_transforms ||
                    words->size_bits_by_length[i] == 0) &&
                    s->dictionary->num_dictionaries > 1)
                {
                    byte dict_id2;
                    int dist_remaining = address -
                        (int)((1u << (int)shift) & ~1u) * (int)transforms->num_transforms;
                    for (dict_id2 = 0; dict_id2 < s->dictionary->num_dictionaries;
                        dict_id2++)
                    {
                        BrotliDictionary* words2 = s->dictionary->words(dict_id2);
                        if (dict_id2 != dict_id && words2->size_bits_by_length[i] != 0)
                        {
                            BrotliTransforms* transforms2 =
                                s->dictionary->transforms(dict_id2);
                            nuint shift2 = words2->size_bits_by_length[i];
                            int num = (int)((1u << (int)shift2) & ~1u) *
                                (int)transforms2->num_transforms;
                            if (dist_remaining < num)
                            {
                                dict_id = dict_id2;
                                words = words2;
                                transforms = transforms2;
                                address = dist_remaining;
                                shift = shift2;
                                mask = (int)BitMask(shift);
                                word_idx = address & mask;
                                transform_idx = address >> (int)shift;
                                offset = (int)words->offsets_by_length[i] + word_idx * i;
                                break;
                            }
                            dist_remaining -= num;
                        }
                    }
                }
                if (words->size_bits_by_length[i] == 0)
                {
                    return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_DICTIONARY);
                }
                if (words->data == null)
                {
                    return BROTLI_FAILURE(BROTLI_DECODER_ERROR_DICTIONARY_NOT_SET);
                }
                if (transform_idx < (int)transforms->num_transforms)
                {
                    byte* word = &words->data[offset];
                    int len = i;
                    if (transform_idx == transforms->cutOffTransforms[0])
                    {
                        Buffer.MemoryCopy(word, &s->ringbuffer[pos], len, len);  /* memcpy */
                    }
                    else
                    {
                        len = BrotliTransformDictionaryWord(&s->ringbuffer[pos], word, len,
                            transforms, transform_idx);
                        if (len == 0 && s->distance_code <= 120)
                        {
                            return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_TRANSFORM);
                        }
                    }
                    pos += len;
                    s->meta_block_remaining_len -= len;
                    if (pos >= s->ringbuffer_size)
                    {
                        s->state = BROTLI_STATE_COMMAND_POST_WRITE_1;
                        goto saveStateAndReturn;
                    }
                }
                else
                {
                    return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_TRANSFORM);
                }
            }
            else
            {
                return BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_DICTIONARY);
            }
        }
        else
        {
            int src_start = (pos - s->distance_code) & s->ringbuffer_mask;
            byte* copy_dst = &s->ringbuffer[pos];
            byte* copy_src = &s->ringbuffer[src_start];
            int dst_end = pos + i;
            int src_end = src_start + i;
            /* Update the recent distances cache. */
            s->dist_rb[s->dist_rb_idx & 3] = s->distance_code;
            ++s->dist_rb_idx;
            s->meta_block_remaining_len -= i;
            /* There are 32+ bytes of slack in the ring-buffer allocation.
               Also, we have 16 short codes, that make these 16 bytes irrelevant
               in the ring-buffer. Let's copy over them as a first guess. */
            memmove16(copy_dst, copy_src);
            if (src_end > pos && dst_end > src_start)
            {
                /* Regions intersect. */
                goto CommandPostWrapCopy;
            }
            if (dst_end >= s->ringbuffer_size || src_end >= s->ringbuffer_size)
            {
                /* At least one region wraps. */
                goto CommandPostWrapCopy;
            }
            pos += i;
            if (i > 16)
            {
                if (i > 32)
                {
                    Buffer.MemoryCopy(copy_src + 16, copy_dst + 16,
                        i - 16, i - 16);  /* memcpy */
                }
                else
                {
                    /* This branch covers about 45% cases.
                       Fixed size short copy allows more compiler optimizations. */
                    memmove16(copy_dst + 16, copy_src + 16);
                }
            }
        }
        if (s->meta_block_remaining_len <= 0)
        {
            /* Next metablock, if any. */
            s->state = BROTLI_STATE_METABLOCK_DONE;
            goto saveStateAndReturn;
        }
        else
        {
            goto CommandBegin;
        }
    CommandPostWrapCopy:
        {
            int wrap_guard = s->ringbuffer_size - pos;
            while (--i >= 0)
            {
                s->ringbuffer[pos] =
                    s->ringbuffer[(pos - s->distance_code) & s->ringbuffer_mask];
                ++pos;
                if (--wrap_guard == 0)
                {
                    s->state = BROTLI_STATE_COMMAND_POST_WRITE_2;
                    goto saveStateAndReturn;
                }
            }
        }
        if (s->meta_block_remaining_len <= 0)
        {
            /* Next metablock, if any. */
            s->state = BROTLI_STATE_METABLOCK_DONE;
            goto saveStateAndReturn;
        }
        else
        {
            goto CommandBegin;
        }

    NextLiteralBlock:
        /* BROTLI_SAFE(DecodeLiteralBlockSwitch(s)); */
        if (safe != 0)
        {
            if (SafeDecodeLiteralBlockSwitch(s) == 0)
            {
                result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                goto saveStateAndReturn;
            }
        }
        else
        {
            DecodeLiteralBlockSwitch(s);
        }
        goto CommandInner;

    saveStateAndReturn:
        s->pos = pos;
        s->loop_counter = i;
        return result;
    }

    private static BrotliDecoderErrorCode ProcessCommands(BrotliDecoderState* s)
    {
        return ProcessCommandsInternal(0, s);
    }

    private static BrotliDecoderErrorCode SafeProcessCommands(BrotliDecoderState* s)
    {
        return ProcessCommandsInternal(1, s);
    }

    /// <summary><c>BrotliDecoderDecompress</c>: one-shot decode over a caller-provided
    /// output buffer.</summary>
    internal static BrotliDecoderResult BrotliDecoderDecompress(
        nuint encoded_size, byte* encoded_buffer,
        nuint* decoded_size, byte* decoded_buffer)
    {
        BrotliDecoderState s = default;
        BrotliDecoderResult result;
        nuint total_out = 0;
        nuint available_in = encoded_size;
        byte* next_in = encoded_buffer;
        nuint available_out = *decoded_size;
        byte* next_out = decoded_buffer;
        if (BrotliDecoderStateInit(&s, 0, 0, null) == 0)
        {
            return BROTLI_DECODER_RESULT_ERROR;
        }
        result = BrotliDecoderDecompressStream(
            &s, &available_in, &next_in, &available_out, &next_out, &total_out);
        *decoded_size = total_out;
        BrotliDecoderStateCleanup(&s);
        if (result != BROTLI_DECODER_RESULT_SUCCESS)
        {
            result = BROTLI_DECODER_RESULT_ERROR;
        }
        return result;
    }

    /* Invariant: input stream is never overconsumed:
        - invalid input implies that the whole stream is invalid -> any amount of
          input could be read and discarded
        - when result is "needs more input", then at least one more byte is REQUIRED
          to complete decoding; all input data MUST be consumed by decoder, so
          client could swap the input buffer
        - when result is "needs more output" decoder MUST ensure that it doesn't
          hold more than 7 bits in bit reader; this saves client from swapping input
          buffer ahead of time
        - when result is "success" decoder MUST return all unused data back to input
          buffer; this is possible because the invariant is held on enter */
    internal static BrotliDecoderResult BrotliDecoderDecompressStream(
        BrotliDecoderState* s, nuint* available_in, byte** next_in,
        nuint* available_out, byte** next_out, nuint* total_out)
    {
        BrotliDecoderErrorCode result = BROTLI_DECODER_SUCCESS;
        BrotliBitReader* br = &s->br;
        nuint input_size = *available_in;
        /* BROTLI_SAVE_ERROR_CODE(code) ==
           SaveErrorCode(s, (code), input_size - *available_in) — inlined below. */
        /* Ensure that |total_out| is set, even if no data will ever be pushed out. */
        if (total_out != null)
        {
            *total_out = s->partial_pos_out;
        }
        /* Do not try to process further in a case of unrecoverable error. */
        if (s->error_code < 0)
        {
            return BROTLI_DECODER_RESULT_ERROR;
        }
        if (*available_out != 0 && (next_out == null || *next_out == null))
        {
            return SaveErrorCode(s,
                BROTLI_FAILURE(BROTLI_DECODER_ERROR_INVALID_ARGUMENTS),
                input_size - *available_in);
        }
        if (*available_out == 0) next_out = null;
        if (s->buffer_length == 0)
        {  /* Just connect bit reader to input stream. */
            BrotliBitReaderSetInput(br, *next_in, *available_in);
        }
        else
        {
            /* At least one byte of input is required. More than one byte of input may
               be required to complete the transaction -> reading more data must be
               done in a loop -> do it in a main loop. */
            result = BROTLI_DECODER_NEEDS_MORE_INPUT;
            BrotliBitReaderSetInput(br, &s->buffer.u8[0], s->buffer_length);
        }
        /* State machine */
        for (;;)
        {
            if (result != BROTLI_DECODER_SUCCESS)
            {
                /* Error, needs more input/output. */
                if (result == BROTLI_DECODER_NEEDS_MORE_INPUT)
                {
                    if (s->ringbuffer != null)
                    {  /* Pro-actively push output. */
                        BrotliDecoderErrorCode intermediate_result = WriteRingBuffer(s,
                            available_out, next_out, total_out, BROTLI_TRUE);
                        /* WriteRingBuffer checks s->meta_block_remaining_len validity. */
                        if ((int)intermediate_result < 0)
                        {
                            result = intermediate_result;
                            break;
                        }
                    }
                    if (s->buffer_length != 0)
                    {  /* Used with internal buffer. */
                        if (br->next_in == br->last_in)
                        {
                            /* Successfully finished read transaction.
                               Accumulator contains less than 8 bits, because internal buffer
                               is expanded byte-by-byte until it is enough to complete read. */
                            s->buffer_length = 0;
                            /* Switch to input stream and restart. */
                            result = BROTLI_DECODER_SUCCESS;
                            BrotliBitReaderSetInput(br, *next_in, *available_in);
                            continue;
                        }
                        else if (*available_in != 0)
                        {
                            /* Not enough data in buffer, but can take one more byte from
                               input stream. */
                            result = BROTLI_DECODER_SUCCESS;
                            s->buffer.u8[s->buffer_length] = **next_in;
                            s->buffer_length++;
                            BrotliBitReaderSetInput(br, &s->buffer.u8[0], s->buffer_length);
                            (*next_in)++;
                            (*available_in)--;
                            /* Retry with more data in buffer. */
                            continue;
                        }
                        /* Can't finish reading and no more input. */
                        break;
                    }
                    else
                    {  /* Input stream doesn't contain enough input. */
                        /* Copy tail to internal buffer and return. */
                        *next_in = br->next_in;
                        *available_in = BrotliBitReaderGetAvailIn(br);
                        while (*available_in != 0)
                        {
                            s->buffer.u8[s->buffer_length] = **next_in;
                            s->buffer_length++;
                            (*next_in)++;
                            (*available_in)--;
                        }
                        break;
                    }
                    /* Unreachable. */
                }

                /* Fail or needs more output. */

                if (s->buffer_length != 0)
                {
                    /* Just consumed the buffered input and produced some output. Otherwise
                       it would result in "needs more input". Reset internal buffer. */
                    s->buffer_length = 0;
                }
                else
                {
                    /* Using input stream in last iteration. When decoder switches to input
                       stream it has less than 8 bits in accumulator, so it is safe to
                       return unused accumulator bits there. */
                    BrotliBitReaderUnload(br);
                    *available_in = BrotliBitReaderGetAvailIn(br);
                    *next_in = br->next_in;
                }
                break;
            }
            switch (s->state)
            {
                case BROTLI_STATE_UNINITED:
                    /* Prepare to the first read. */
                    if (BrotliWarmupBitReader(br) == 0)
                    {
                        result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                        break;
                    }
                    /* Decode window size. */
                    result = DecodeWindowBits(s, br);  /* Reads 1..8 bits. */
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    if (s->large_window != 0)
                    {
                        s->state = BROTLI_STATE_LARGE_WINDOW_BITS;
                        break;
                    }
                    s->state = BROTLI_STATE_INITIALIZE;
                    break;

                case BROTLI_STATE_LARGE_WINDOW_BITS:
                {
                    nuint bits;
                    if (BrotliSafeReadBits(br, 6, &bits) == 0)
                    {
                        result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                        break;
                    }
                    s->window_bits = (uint)(bits & 63);
                    if (s->window_bits < BROTLI_LARGE_MIN_WBITS ||
                        s->window_bits > BROTLI_LARGE_MAX_WBITS)
                    {
                        result = BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_WINDOW_BITS);
                        break;
                    }
                    s->state = BROTLI_STATE_INITIALIZE;
                }
                /* Fall through. */
                goto case BROTLI_STATE_INITIALIZE;

                case BROTLI_STATE_INITIALIZE:
                    /* Maximum distance, see section 9.1. of the spec. */
                    s->max_backward_distance = (1 << (int)s->window_bits) - WindowGap;

                    /* Allocate memory for both block_type_trees and block_len_trees. */
                    s->block_type_trees = (HuffmanCode*)BROTLI_DECODER_ALLOC(s,
                        (nuint)sizeof(HuffmanCode) * 3 *
                            (BROTLI_HUFFMAN_MAX_SIZE_258 + BROTLI_HUFFMAN_MAX_SIZE_26));
                    if (s->block_type_trees == null)
                    {
                        result = BROTLI_FAILURE(BROTLI_DECODER_ERROR_ALLOC_BLOCK_TYPE_TREES);
                        break;
                    }
                    s->block_len_trees =
                        s->block_type_trees + 3 * BROTLI_HUFFMAN_MAX_SIZE_258;

                    s->state = BROTLI_STATE_METABLOCK_BEGIN;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_BEGIN;

                case BROTLI_STATE_METABLOCK_BEGIN:
                    BrotliDecoderStateMetablockBegin(s);
                    s->state = BROTLI_STATE_METABLOCK_HEADER;
                    /* Fall through. */
                    goto case BROTLI_STATE_METABLOCK_HEADER;

                case BROTLI_STATE_METABLOCK_HEADER:
                    result = DecodeMetaBlockLength(s, br);  /* Reads 2 - 31 bits. */
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    if (s->is_metadata != 0 || s->is_uncompressed != 0)
                    {
                        if (BrotliJumpToByteBoundary(br) == 0)
                        {
                            result = BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_PADDING_1);
                            break;
                        }
                    }
                    if (s->is_metadata != 0)
                    {
                        s->state = BROTLI_STATE_METADATA;
                        /* if (s->metadata_start_func) s->metadata_start_func(...); —
                           metadata callbacks are not supported in the port. */
                        break;
                    }
                    if (s->meta_block_remaining_len == 0)
                    {
                        s->state = BROTLI_STATE_METABLOCK_DONE;
                        break;
                    }
                    BrotliCalculateRingBufferSize(s);
                    if (s->is_uncompressed != 0)
                    {
                        s->state = BROTLI_STATE_UNCOMPRESSED;
                        break;
                    }
                    s->state = BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_HEADER;
                    /* Fall through. */
                    goto case BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_HEADER;

                case BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_HEADER:
                {
                    BrotliMetablockHeaderArena* h = &s->arena.header;
                    s->loop_counter = 0;
                    /* Initialize compressed metablock header arena. */
                    h->sub_loop_counter = 0;
                    /* Make small negative indexes addressable. */
                    h->symbol_lists =
                        &h->symbols_lists_array[BROTLI_HUFFMAN_MAX_CODE_LENGTH + 1];
                    h->substate_huffman = BROTLI_STATE_HUFFMAN_NONE;
                    h->substate_tree_group = BROTLI_STATE_TREE_GROUP_NONE;
                    h->substate_context_map = BROTLI_STATE_CONTEXT_MAP_NONE;
                    s->state = BROTLI_STATE_HUFFMAN_CODE_0;
                }
                /* Fall through. */
                goto case BROTLI_STATE_HUFFMAN_CODE_0;

                case BROTLI_STATE_HUFFMAN_CODE_0:
                    if (s->loop_counter >= 3)
                    {
                        s->state = BROTLI_STATE_METABLOCK_HEADER_2;
                        break;
                    }
                    /* Reads 1..11 bits. */
                    result = DecodeVarLenUint8(s, br,
                        (nuint*)&s->num_block_types[s->loop_counter]);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    s->num_block_types[s->loop_counter]++;
                    if (s->num_block_types[s->loop_counter] < 2)
                    {
                        s->loop_counter++;
                        break;
                    }
                    s->state = BROTLI_STATE_HUFFMAN_CODE_1;
                    /* Fall through. */
                    goto case BROTLI_STATE_HUFFMAN_CODE_1;

                case BROTLI_STATE_HUFFMAN_CODE_1:
                {
                    nuint alphabet_size = (nuint)s->num_block_types[s->loop_counter] + 2;
                    int tree_offset = s->loop_counter * BROTLI_HUFFMAN_MAX_SIZE_258;
                    result = ReadHuffmanCode(alphabet_size, alphabet_size,
                        &s->block_type_trees[tree_offset], null, s);
                    if (result != BROTLI_DECODER_SUCCESS) break;
                    s->state = BROTLI_STATE_HUFFMAN_CODE_2;
                }
                /* Fall through. */
                goto case BROTLI_STATE_HUFFMAN_CODE_2;

                case BROTLI_STATE_HUFFMAN_CODE_2:
                {
                    nuint alphabet_size = BROTLI_NUM_BLOCK_LEN_SYMBOLS;
                    int tree_offset = s->loop_counter * BROTLI_HUFFMAN_MAX_SIZE_26;
                    result = ReadHuffmanCode(alphabet_size, alphabet_size,
                        &s->block_len_trees[tree_offset], null, s);
                    if (result != BROTLI_DECODER_SUCCESS) break;
                    s->state = BROTLI_STATE_HUFFMAN_CODE_3;
                }
                /* Fall through. */
                goto case BROTLI_STATE_HUFFMAN_CODE_3;

                case BROTLI_STATE_HUFFMAN_CODE_3:
                {
                    int tree_offset = s->loop_counter * BROTLI_HUFFMAN_MAX_SIZE_26;
                    if (SafeReadBlockLength(s, (nuint*)&s->block_length[s->loop_counter],
                        &s->block_len_trees[tree_offset], br) == 0)
                    {
                        result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                        break;
                    }
                    s->loop_counter++;
                    s->state = BROTLI_STATE_HUFFMAN_CODE_0;
                    break;
                }

                case BROTLI_STATE_UNCOMPRESSED:
                {
                    result = CopyUncompressedBlockToOutput(
                        available_out, next_out, total_out, s);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    s->state = BROTLI_STATE_METABLOCK_DONE;
                    break;
                }

                case BROTLI_STATE_METADATA:
                    result = SkipMetadataBlock(s);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    s->state = BROTLI_STATE_METABLOCK_DONE;
                    break;

                case BROTLI_STATE_METABLOCK_HEADER_2:
                {
                    nuint bits;
                    if (BrotliSafeReadBits(br, 6, &bits) == 0)
                    {
                        result = BROTLI_DECODER_NEEDS_MORE_INPUT;
                        break;
                    }
                    s->distance_postfix_bits = bits & BitMask(2);
                    bits >>= 2;
                    s->num_direct_distance_codes = bits << (int)s->distance_postfix_bits;
                    s->context_modes =
                        (byte*)BROTLI_DECODER_ALLOC(s, (nuint)s->num_block_types[0]);
                    if (s->context_modes == null)
                    {
                        result = BROTLI_FAILURE(BROTLI_DECODER_ERROR_ALLOC_CONTEXT_MODES);
                        break;
                    }
                    s->loop_counter = 0;
                    s->state = BROTLI_STATE_CONTEXT_MODES;
                }
                /* Fall through. */
                goto case BROTLI_STATE_CONTEXT_MODES;

                case BROTLI_STATE_CONTEXT_MODES:
                    result = ReadContextModes(s);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    s->state = BROTLI_STATE_CONTEXT_MAP_1;
                    /* Fall through. */
                    goto case BROTLI_STATE_CONTEXT_MAP_1;

                case BROTLI_STATE_CONTEXT_MAP_1:
                    result = DecodeContextMap(
                        (nuint)s->num_block_types[0] << BROTLI_LITERAL_CONTEXT_BITS,
                        &s->num_literal_htrees, &s->context_map, s);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    DetectTrivialLiteralBlockTypes(s);
                    s->state = BROTLI_STATE_CONTEXT_MAP_2;
                    /* Fall through. */
                    goto case BROTLI_STATE_CONTEXT_MAP_2;

                case BROTLI_STATE_CONTEXT_MAP_2:
                {
                    nuint npostfix = s->distance_postfix_bits;
                    nuint ndirect = s->num_direct_distance_codes;
                    nuint distance_alphabet_size_max = BROTLI_DISTANCE_ALPHABET_SIZE(
                        (uint)npostfix, (uint)ndirect, BROTLI_MAX_DISTANCE_BITS);
                    nuint distance_alphabet_size_limit = distance_alphabet_size_max;
                    int allocation_success = BROTLI_TRUE;
                    if (s->large_window != 0)
                    {
                        BrotliDistanceCodeLimit limit = BrotliCalculateDistanceCodeLimit(
                            BROTLI_MAX_ALLOWED_DISTANCE, (uint)npostfix,
                            (uint)ndirect);
                        distance_alphabet_size_max = BROTLI_DISTANCE_ALPHABET_SIZE(
                            (uint)npostfix, (uint)ndirect, BROTLI_LARGE_MAX_DISTANCE_BITS);
                        distance_alphabet_size_limit = limit.max_alphabet_size;
                    }
                    result = DecodeContextMap(
                        (nuint)s->num_block_types[2] << BROTLI_DISTANCE_CONTEXT_BITS,
                        &s->num_dist_htrees, &s->dist_context_map, s);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    allocation_success &= BrotliDecoderHuffmanTreeGroupInit(
                        s, &s->literal_hgroup, BROTLI_NUM_LITERAL_SYMBOLS,
                        BROTLI_NUM_LITERAL_SYMBOLS, s->num_literal_htrees);
                    allocation_success &= BrotliDecoderHuffmanTreeGroupInit(
                        s, &s->insert_copy_hgroup, BROTLI_NUM_COMMAND_SYMBOLS,
                        BROTLI_NUM_COMMAND_SYMBOLS, (nuint)s->num_block_types[1]);
                    allocation_success &= BrotliDecoderHuffmanTreeGroupInit(
                        s, &s->distance_hgroup, distance_alphabet_size_max,
                        distance_alphabet_size_limit, s->num_dist_htrees);
                    if (allocation_success == 0)
                    {
                        return SaveErrorCode(s,
                            BROTLI_FAILURE(BROTLI_DECODER_ERROR_ALLOC_TREE_GROUPS),
                            input_size - *available_in);
                    }
                    s->loop_counter = 0;
                    s->state = BROTLI_STATE_TREE_GROUP;
                }
                /* Fall through. */
                goto case BROTLI_STATE_TREE_GROUP;

                case BROTLI_STATE_TREE_GROUP:
                {
                    HuffmanTreeGroup* hgroup = null;
                    switch (s->loop_counter)
                    {
                        case 0: hgroup = &s->literal_hgroup; break;
                        case 1: hgroup = &s->insert_copy_hgroup; break;
                        case 2: hgroup = &s->distance_hgroup; break;
                        default:
                            return SaveErrorCode(s, BROTLI_FAILURE(
                                BROTLI_DECODER_ERROR_UNREACHABLE),
                                input_size - *available_in);  /* COV_NF_LINE */
                    }
                    result = HuffmanTreeGroupDecode(hgroup, s);
                    if (result != BROTLI_DECODER_SUCCESS) break;
                    s->loop_counter++;
                    if (s->loop_counter < 3)
                    {
                        break;
                    }
                    s->state = BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_BODY;
                }
                /* Fall through. */
                goto case BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_BODY;

                case BROTLI_STATE_BEFORE_COMPRESSED_METABLOCK_BODY:
                    PrepareLiteralDecoding(s);
                    s->dist_context_map_slice = s->dist_context_map;
                    s->htree_command = s->insert_copy_hgroup.htrees[0];
                    if (BrotliEnsureRingBuffer(s) == 0)
                    {
                        result = BROTLI_FAILURE(BROTLI_DECODER_ERROR_ALLOC_RING_BUFFER_2);
                        break;
                    }
                    CalculateDistanceLut(s);
                    s->state = BROTLI_STATE_COMMAND_BEGIN;
                    /* Fall through. */
                    goto case BROTLI_STATE_COMMAND_BEGIN;

                case BROTLI_STATE_COMMAND_BEGIN:
                /* Fall through. */
                case BROTLI_STATE_COMMAND_INNER:
                /* Fall through. */
                case BROTLI_STATE_COMMAND_POST_DECODE_LITERALS:
                /* Fall through. */
                case BROTLI_STATE_COMMAND_POST_WRAP_COPY:
                    result = ProcessCommands(s);
                    if (result == BROTLI_DECODER_NEEDS_MORE_INPUT)
                    {
                        result = SafeProcessCommands(s);
                    }
                    break;

                case BROTLI_STATE_COMMAND_INNER_WRITE:
                /* Fall through. */
                case BROTLI_STATE_COMMAND_POST_WRITE_1:
                /* Fall through. */
                case BROTLI_STATE_COMMAND_POST_WRITE_2:
                    result = WriteRingBuffer(
                        s, available_out, next_out, total_out, BROTLI_FALSE);
                    if (result != BROTLI_DECODER_SUCCESS)
                    {
                        break;
                    }
                    WrapRingBuffer(s);
                    if (s->ringbuffer_size == 1 << (int)s->window_bits)
                    {
                        s->max_distance = s->max_backward_distance;
                    }
                    if (s->state == BROTLI_STATE_COMMAND_POST_WRITE_1)
                    {
                        BrotliDecoderCompoundDictionary* addon = s->compound_dictionary;
                        if (addon != null && (addon->br_length != addon->br_copied))
                        {
                            s->pos += CopyFromCompoundDictionary(s, s->pos);
                            if (s->pos >= s->ringbuffer_size) continue;
                        }
                        if (s->meta_block_remaining_len == 0)
                        {
                            /* Next metablock, if any. */
                            s->state = BROTLI_STATE_METABLOCK_DONE;
                        }
                        else
                        {
                            s->state = BROTLI_STATE_COMMAND_BEGIN;
                        }
                        break;
                    }
                    else if (s->state == BROTLI_STATE_COMMAND_POST_WRITE_2)
                    {
                        s->state = BROTLI_STATE_COMMAND_POST_WRAP_COPY;
                    }
                    else
                    {  /* BROTLI_STATE_COMMAND_INNER_WRITE */
                        if (s->loop_counter == 0)
                        {
                            if (s->meta_block_remaining_len == 0)
                            {
                                s->state = BROTLI_STATE_METABLOCK_DONE;
                            }
                            else
                            {
                                s->state = BROTLI_STATE_COMMAND_POST_DECODE_LITERALS;
                            }
                            break;
                        }
                        s->state = BROTLI_STATE_COMMAND_INNER;
                    }
                    break;

                case BROTLI_STATE_METABLOCK_DONE:
                    if (s->meta_block_remaining_len < 0)
                    {
                        result = BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_BLOCK_LENGTH_2);
                        break;
                    }
                    BrotliDecoderStateCleanupAfterMetablock(s);
                    if (s->is_last_metablock == 0)
                    {
                        s->state = BROTLI_STATE_METABLOCK_BEGIN;
                        break;
                    }
                    if (BrotliJumpToByteBoundary(br) == 0)
                    {
                        result = BROTLI_FAILURE(BROTLI_DECODER_ERROR_FORMAT_PADDING_2);
                        break;
                    }
                    if (s->buffer_length == 0)
                    {
                        BrotliBitReaderUnload(br);
                        *available_in = BrotliBitReaderGetAvailIn(br);
                        *next_in = br->next_in;
                    }
                    s->state = BROTLI_STATE_DONE;
                    /* Fall through. */
                    goto case BROTLI_STATE_DONE;

                case BROTLI_STATE_DONE:
                    if (s->ringbuffer != null)
                    {
                        result = WriteRingBuffer(
                            s, available_out, next_out, total_out, BROTLI_TRUE);
                        if (result != BROTLI_DECODER_SUCCESS)
                        {
                            break;
                        }
                    }
                    return SaveErrorCode(s, result, input_size - *available_in);
            }
        }
        return SaveErrorCode(s, result, input_size - *available_in);
    }

    /// <summary><c>BrotliDecoderHasMoreOutput</c>.</summary>
    internal static int BrotliDecoderHasMoreOutput(BrotliDecoderState* s)
    {
        /* After unrecoverable error remaining output is considered nonsensical. */
        if (s->error_code < 0)
        {
            return BROTLI_FALSE;
        }
        return (s->ringbuffer != null && UnwrittenBytes(s, BROTLI_FALSE) != 0)
            ? BROTLI_TRUE : BROTLI_FALSE;
    }

    /// <summary><c>BrotliDecoderTakeOutput</c>.</summary>
    internal static byte* BrotliDecoderTakeOutput(BrotliDecoderState* s, nuint* size)
    {
        byte* result = null;
        nuint available_out = *size != 0 ? *size : (nuint)1u << 24;
        nuint requested_out = available_out;
        BrotliDecoderErrorCode status;
        if ((s->ringbuffer == null) || (s->error_code < 0))
        {
            *size = 0;
            return null;
        }
        WrapRingBuffer(s);
        status = WriteRingBuffer(s, &available_out, &result, null, BROTLI_TRUE);
        /* Either WriteRingBuffer returns those "success" codes... */
        if (status == BROTLI_DECODER_SUCCESS ||
            status == BROTLI_DECODER_NEEDS_MORE_OUTPUT)
        {
            *size = requested_out - available_out;
        }
        else
        {
            /* ... or stream is broken. Normally this should be caught by
               BrotliDecoderDecompressStream, this is just a safeguard. */
            if ((int)status < 0) SaveErrorCode(s, status, 0);
            *size = 0;
            result = null;
        }
        return result;
    }

    /// <summary><c>BrotliDecoderIsUsed</c>.</summary>
    internal static int BrotliDecoderIsUsed(BrotliDecoderState* s)
    {
        return (s->state != BROTLI_STATE_UNINITED ||
            BrotliGetAvailableBits(&s->br) != 0) ? BROTLI_TRUE : BROTLI_FALSE;
    }

    /// <summary><c>BrotliDecoderIsFinished</c>.</summary>
    internal static int BrotliDecoderIsFinished(BrotliDecoderState* s)
    {
        return (s->state == BROTLI_STATE_DONE &&
            BrotliDecoderHasMoreOutput(s) == 0) ? BROTLI_TRUE : BROTLI_FALSE;
    }

    /// <summary><c>BrotliDecoderGetErrorCode</c>.</summary>
    internal static BrotliDecoderErrorCode BrotliDecoderGetErrorCode(BrotliDecoderState* s)
    {
        return (BrotliDecoderErrorCode)s->error_code;
    }

    /* The strings the C's BROTLI_DECODER_ERROR_CODES_LIST/#PREFIX #NAME macro
       expansion produces, indexed by code (0..3) resp. 3 - code (-1..-31);
       reserved codes map to "INVALID", like the C switch default. */
    private static readonly byte*[] kErrorStrings = CreateErrorStrings();
    private static readonly byte* kInvalidErrorString = InternErrorString("INVALID");

    private static byte* InternErrorString(string s)
    {
        byte* p = (byte*)NativeMemory.Alloc((nuint)(s.Length + 1));
        for (int i = 0; i < s.Length; i++)
        {
            p[i] = (byte)s[i];
        }
        p[s.Length] = 0;
        return p;
    }

    private static byte*[] CreateErrorStrings()
    {
        string?[] names =
        [
            "_NO_ERROR",                              /* 0 */
            "_SUCCESS",                               /* 1 */
            "_NEEDS_MORE_INPUT",                      /* 2 */
            "_NEEDS_MORE_OUTPUT",                     /* 3 */
            "_ERROR_FORMAT_EXUBERANT_NIBBLE",         /* -1 */
            "_ERROR_FORMAT_RESERVED",                 /* -2 */
            "_ERROR_FORMAT_EXUBERANT_META_NIBBLE",    /* -3 */
            "_ERROR_FORMAT_SIMPLE_HUFFMAN_ALPHABET",  /* -4 */
            "_ERROR_FORMAT_SIMPLE_HUFFMAN_SAME",      /* -5 */
            "_ERROR_FORMAT_CL_SPACE",                 /* -6 */
            "_ERROR_FORMAT_HUFFMAN_SPACE",            /* -7 */
            "_ERROR_FORMAT_CONTEXT_MAP_REPEAT",       /* -8 */
            "_ERROR_FORMAT_BLOCK_LENGTH_1",           /* -9 */
            "_ERROR_FORMAT_BLOCK_LENGTH_2",           /* -10 */
            "_ERROR_FORMAT_TRANSFORM",                /* -11 */
            "_ERROR_FORMAT_DICTIONARY",               /* -12 */
            "_ERROR_FORMAT_WINDOW_BITS",              /* -13 */
            "_ERROR_FORMAT_PADDING_1",                /* -14 */
            "_ERROR_FORMAT_PADDING_2",                /* -15 */
            "_ERROR_FORMAT_DISTANCE",                 /* -16 */
            null,                                     /* -17 is reserved */
            "_ERROR_COMPOUND_DICTIONARY",             /* -18 */
            "_ERROR_DICTIONARY_NOT_SET",              /* -19 */
            "_ERROR_INVALID_ARGUMENTS",               /* -20 */
            "_ERROR_ALLOC_CONTEXT_MODES",             /* -21 */
            "_ERROR_ALLOC_TREE_GROUPS",               /* -22 */
            null,                                     /* -23 is reserved */
            null,                                     /* -24 is reserved */
            "_ERROR_ALLOC_CONTEXT_MAP",               /* -25 */
            "_ERROR_ALLOC_RING_BUFFER_1",             /* -26 */
            "_ERROR_ALLOC_RING_BUFFER_2",             /* -27 */
            null,                                     /* -28 is reserved */
            null,                                     /* -29 is reserved */
            "_ERROR_ALLOC_BLOCK_TYPE_TREES",          /* -30 */
            "_ERROR_UNREACHABLE",                     /* -31 */
        ];
        byte*[] table = new byte*[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            table[i] = names[i] is null ? null : InternErrorString(names[i]!);
        }
        return table;
    }

    /// <summary><c>BrotliDecoderErrorString</c>: NUL-terminated static string.</summary>
    internal static byte* BrotliDecoderErrorString(BrotliDecoderErrorCode c)
    {
        int code = (int)c;
        int index = code >= 0 ? code : 3 - code;
        if (index >= 0 && index < kErrorStrings.Length && kErrorStrings[index] != null)
        {
            return kErrorStrings[index];
        }
        return kInvalidErrorString;
    }

    /// <summary><c>BrotliDecoderVersion</c>.</summary>
    internal static uint BrotliDecoderVersion()
    {
        return BrotliConstants.Version;
    }
}
