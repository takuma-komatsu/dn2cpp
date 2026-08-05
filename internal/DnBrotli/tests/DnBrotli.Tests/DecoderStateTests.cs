using System.Runtime.InteropServices;
using DnBrotli.Common;
using DnBrotli.Dec;

namespace DnBrotli.Tests;

/// <summary>Exercises the c/dec/state.{h,c} port: init defaults, metablock begin/cleanup
/// and Huffman tree-group allocation over a state struct in unmanaged memory.</summary>
public unsafe class DecoderStateTests
{
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
