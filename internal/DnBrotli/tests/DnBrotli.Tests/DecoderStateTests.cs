using System.Runtime.InteropServices;
using DnBrotli.Common;
using DnBrotli.Dec;

namespace DnBrotli.Tests;

/// <summary>Exercises the c/dec/state.{h,c} port: init defaults, metablock begin/cleanup
/// and Huffman tree-group allocation over a state struct in unmanaged memory.</summary>
public unsafe class DecoderStateTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 64)]
    [InlineData(1, 1)]
    [InlineData(1, 64)]
    [InlineData(2, 1)]
    [InlineData(2, 64)]
    public void ExhaustedSingleTypeBlockIsTerminalWithoutWriting(int treeType, int inputSize)
    {
        BrotliDecoderState* s = DecoderEngine.BrotliDecoderCreateInstance(0, 0, null);
        Assert.True(s != null);
        try
        {
            State.BrotliDecoderStateMetablockBegin(s);
            // Reach each switch without decoding a block-cap worth of symbols.
            // A single-type block has no switch trees to consult after exhaustion.
            s->state = treeType switch
            {
                0 => BrotliRunningState.BROTLI_STATE_COMMAND_INNER,
                1 => BrotliRunningState.BROTLI_STATE_COMMAND_BEGIN,
                _ => BrotliRunningState.BROTLI_STATE_COMMAND_POST_DECODE_LITERALS,
            };
            s->block_length[treeType] = 0;
            s->meta_block_remaining_len = 1;
            s->loop_counter = 1;
            s->distance_code = -1;
            s->ringbuffer_size = 64;
            s->ringbuffer_mask = 63;
            s->ringbuffer = (byte*)NativeMemory.Alloc(64);
            new Span<byte>(s->ringbuffer, 64).Fill(0xA5);
            s->trivial_literal_context = 1;
            HuffmanCode* literalTree = stackalloc HuffmanCode[256];
            new Span<HuffmanCode>(literalTree, 256).Clear();
            s->literal_htree = literalTree;

            byte* input = stackalloc byte[64];
            new Span<byte>(input, 64).Clear();
            byte* output = stackalloc byte[18];
            new Span<byte>(output, 18).Fill(0x5A);
            byte* nextIn = input;
            byte* nextOut = output + 1;
            nuint availableIn = (nuint)inputSize;
            nuint availableOut = 16;
            nuint totalOut = 0;

            // Tiny input takes SafeProcessCommands; ample input takes ProcessCommands.
            Assert.Equal(BrotliDecoderResult.Error, DecoderEngine.BrotliDecoderDecompressStream(
                s, &availableIn, &nextIn, &availableOut, &nextOut, &totalOut));
            Assert.Equal((int)BrotliDecoderErrorCode.ErrorFormatBlockSwitch, s->error_code);
            Assert.Equal((nuint)0, totalOut);
            Assert.Equal((nuint)16, availableOut);
            Assert.Equal(0, s->pos);
            Assert.Equal(0ul, s->block_length[treeType]);
            for (int i = 0; i < 64; i++) Assert.Equal((byte)0xA5, s->ringbuffer[i]);
            for (int i = 0; i < 18; i++) Assert.Equal((byte)0x5A, output[i]);

            byte* stoppedAt = nextIn;
            nuint unconsumed = availableIn;
            Assert.Equal(BrotliDecoderResult.Error, DecoderEngine.BrotliDecoderDecompressStream(
                s, &availableIn, &nextIn, &availableOut, &nextOut, &totalOut));
            Assert.True(nextIn == stoppedAt);
            Assert.Equal(unconsumed, availableIn);
            Assert.Equal((nuint)16, availableOut);
            Assert.Equal((int)BrotliDecoderErrorCode.ErrorFormatBlockSwitch, s->error_code);
        }
        finally
        {
            DecoderEngine.BrotliDecoderDestroyInstance(s);
        }
    }

    [Fact]
    public void StateInitSetsCDefaults()
    {
        BrotliDecoderState* s = (BrotliDecoderState*)
            NativeMemory.AllocZeroed((nuint)sizeof(BrotliDecoderState));
        try
        {
            Assert.Equal(1, State.BrotliDecoderStateInit(s, 0, 0, null));

            Assert.Equal(BrotliRunningState.BROTLI_STATE_UNINITED, s->state);
            Assert.Equal(0, s->error_code);
            // c/dec/state.c lines 77-80: initial distance ring buffer.
            Assert.Equal(16, s->dist_rb[0]);
            Assert.Equal(15, s->dist_rb[1]);
            Assert.Equal(11, s->dist_rb[2]);
            Assert.Equal(4, s->dist_rb[3]);
            Assert.Equal((nuint)63, s->mtf_upper_bound);
            Assert.Equal(1u, s->canny_ringbuffer_allocation);
            Assert.True(s->compound_dictionary == null);
            // The embedded shared dictionary defaults to the builtin word list.
            Assert.True(s->dictionary != null);
            Assert.Equal(1, s->dictionary->num_dictionaries);
            Assert.True(s->dictionary->words(0) == Dictionary.BrotliGetDictionary());

            State.BrotliDecoderStateCleanup(s);
            Assert.True(s->dictionary == null);
        }
        finally
        {
            NativeMemory.Free(s);
        }
    }

    [Fact]
    public void MetablockBeginResetsPerMetablockFields()
    {
        BrotliDecoderState* s = (BrotliDecoderState*)
            NativeMemory.AllocZeroed((nuint)sizeof(BrotliDecoderState));
        try
        {
            Assert.Equal(1, State.BrotliDecoderStateInit(s, 0, 0, null));
            s->meta_block_remaining_len = 123;
            s->dist_htree_index = 7;

            State.BrotliDecoderStateMetablockBegin(s);
            Assert.Equal(0, s->meta_block_remaining_len);
            Assert.Equal(State.BROTLI_BLOCK_SIZE_CAP, s->block_length[0]);
            Assert.Equal(State.BROTLI_BLOCK_SIZE_CAP, s->block_length[2]);
            Assert.Equal(1ul, s->num_block_types[0]);
            Assert.Equal(1ul, s->block_type_rb[4]);
            Assert.Equal(0ul, s->block_type_rb[5]);
            Assert.Equal(0, s->dist_htree_index);
            Assert.True(s->context_map == null);
            Assert.True(s->literal_hgroup.codes == null);

            State.BrotliDecoderStateCleanup(s);
        }
        finally
        {
            NativeMemory.Free(s);
        }
    }

    [Fact]
    public void HuffmanTreeGroupInitAllocatesCodesAfterHtrees()
    {
        BrotliDecoderState* s = (BrotliDecoderState*)
            NativeMemory.AllocZeroed((nuint)sizeof(BrotliDecoderState));
        try
        {
            Assert.Equal(1, State.BrotliDecoderStateInit(s, 0, 0, null));

            HuffmanTreeGroup* group = &s->literal_hgroup;
            Assert.Equal(1, State.BrotliDecoderHuffmanTreeGroupInit(
                s, group, alphabet_size_max: 256, alphabet_size_limit: 256, ntrees: 2));
            Assert.Equal(256, group->alphabet_size_max);
            Assert.Equal(256, group->alphabet_size_limit);
            Assert.Equal(2, group->num_htrees);
            Assert.True(group->htrees != null);
            // codes starts right after the ntrees htree pointers, like the C.
            Assert.True((void*)group->codes == (void*)&group->htrees[2]);

            State.BrotliDecoderStateCleanupAfterMetablock(s);
            Assert.True(group->htrees == null);
            State.BrotliDecoderStateCleanup(s);
        }
        finally
        {
            NativeMemory.Free(s);
        }
    }
}
