// Port of c/enc/ringbuffer.h (brotli v1.1.0): sliding window over the input.
//
// The C declares size_/mask_/tail_size_/total_size_ as `const uint32_t` and
// writes them through casts in RingBufferSetup; here they are plain fields
// assigned directly (same effect, no UB gymnastics needed).

using System.Runtime.CompilerServices;

using static DnBrotli.Common.Platform;
using static DnBrotli.Enc.MemoryManager;
using static DnBrotli.Enc.Quality;

namespace DnBrotli.Enc;

/* A RingBuffer(window_bits, tail_bits) contains `1 << window_bits' bytes of
   data in a circular manner: writing a byte writes it to:
     `position() % (1 << window_bits)'.
   For convenience, the RingBuffer array contains another copy of the
   first `1 << tail_bits' bytes:
     buffer_[i] == buffer_[i + (1 << window_bits)], if i < (1 << tail_bits),
   and another copy of the last two bytes:
     buffer_[-1] == buffer_[(1 << window_bits) - 1] and
     buffer_[-2] == buffer_[(1 << window_bits) - 2]. */
internal unsafe struct RingBuffer
{
    /* Size of the ring-buffer is (1 << window_bits) + tail_size_. */
    public uint size_;
    public uint mask_;
    public uint tail_size_;
    public uint total_size_;

    public uint cur_size_;
    /* Position to write in the ring buffer. */
    public uint pos_;
    /* The actual ring buffer containing the copy of the last two bytes, the data,
       and the copy of the beginning as a tail. */
    public byte* data_;
    /* The start of the ring-buffer. */
    public byte* buffer_;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RingBufferInit(RingBuffer* rb)
    {
        rb->cur_size_ = 0;
        rb->pos_ = 0;
        rb->data_ = null;
        rb->buffer_ = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RingBufferSetup(BrotliEncoderParams* @params, RingBuffer* rb)
    {
        int window_bits = ComputeRbBits(@params);
        int tail_bits = @params->lgblock;
        rb->size_ = 1u << window_bits;
        rb->mask_ = (1u << window_bits) - 1;
        rb->tail_size_ = 1u << tail_bits;
        rb->total_size_ = rb->size_ + rb->tail_size_;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RingBufferFree(MemoryManager* m, RingBuffer* rb)
    {
        BROTLI_FREE(m, ref rb->data_);
    }

    /* Allocates or re-allocates data_ to the given length + plus some slack
       region before and after. Fills the slack regions with zeros. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RingBufferInitBuffer(MemoryManager* m, uint buflen, RingBuffer* rb)
    {
        const nuint kSlackForEightByteHashingEverywhere = 7;
        byte* new_data = BROTLI_ALLOC<byte>(
            m, 2 + buflen + kSlackForEightByteHashingEverywhere);
        nuint i;
        if (BROTLI_IS_OOM(m) || BROTLI_IS_NULL(new_data)) return;
        if (rb->data_ != null)
        {
            Buffer.MemoryCopy(rb->data_, new_data,
                2 + buflen + kSlackForEightByteHashingEverywhere,
                2 + rb->cur_size_ + kSlackForEightByteHashingEverywhere);
            BROTLI_FREE(m, ref rb->data_);
        }
        rb->data_ = new_data;
        rb->cur_size_ = buflen;
        rb->buffer_ = rb->data_ + 2;
        rb->buffer_[-2] = rb->buffer_[-1] = 0;
        for (i = 0; i < kSlackForEightByteHashingEverywhere; ++i)
        {
            rb->buffer_[rb->cur_size_ + i] = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RingBufferWriteTail(byte* bytes, nuint n, RingBuffer* rb)
    {
        nuint masked_pos = rb->pos_ & rb->mask_;
        if (masked_pos < rb->tail_size_)
        {
            /* Just fill the tail buffer with the beginning data. */
            nuint p = rb->size_ + masked_pos;
            Buffer.MemoryCopy(bytes, &rb->buffer_[p],
                BROTLI_MIN(n, rb->tail_size_ - masked_pos),
                BROTLI_MIN(n, rb->tail_size_ - masked_pos));
        }
    }

    /* Push bytes into the ring buffer. */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void RingBufferWrite(
        MemoryManager* m, byte* bytes, nuint n, RingBuffer* rb)
    {
        if (rb->pos_ == 0 && n < rb->tail_size_)
        {
            /* Special case for the first write: to process the first block, we don't
               need to allocate the whole ring-buffer and we don't need the tail
               either. However, we do this memory usage optimization only if the
               first write is less than the tail size, which is also the input block
               size, otherwise it is likely that other blocks will follow and we
               will need to reallocate to the full size anyway. */
            rb->pos_ = (uint)n;
            RingBufferInitBuffer(m, rb->pos_, rb);
            if (BROTLI_IS_OOM(m)) return;
            Buffer.MemoryCopy(bytes, rb->buffer_, n, n);
            return;
        }
        if (rb->cur_size_ < rb->total_size_)
        {
            /* Lazily allocate the full buffer. */
            RingBufferInitBuffer(m, rb->total_size_, rb);
            if (BROTLI_IS_OOM(m)) return;
            /* Initialize the last two bytes to zero, so that we don't have to worry
               later when we copy the last two bytes to the first two positions. */
            rb->buffer_[rb->size_ - 2] = 0;
            rb->buffer_[rb->size_ - 1] = 0;
            /* Initialize tail; might be touched by "best_len++" optimization when
               ring buffer is "full". */
            rb->buffer_[rb->size_] = 241;
        }
        {
            nuint masked_pos = rb->pos_ & rb->mask_;
            /* The length of the writes is limited so that we do not need to worry
               about a write */
            RingBufferWriteTail(bytes, n, rb);
            if (masked_pos + n <= rb->size_)
            {
                /* A single write fits. */
                Buffer.MemoryCopy(bytes, &rb->buffer_[masked_pos], n, n);
            }
            else
            {
                /* Split into two writes.
                   Copy into the end of the buffer, including the tail buffer. */
                nuint len1 = BROTLI_MIN(n, rb->total_size_ - masked_pos);
                Buffer.MemoryCopy(bytes, &rb->buffer_[masked_pos], len1, len1);
                /* Copy into the beginning of the buffer */
                Buffer.MemoryCopy(bytes + (rb->size_ - masked_pos), &rb->buffer_[0],
                    n - (rb->size_ - masked_pos), n - (rb->size_ - masked_pos));
            }
        }
        {
            bool not_first_lap = (rb->pos_ & (1u << 31)) != 0;
            uint rb_pos_mask = (1u << 31) - 1;
            rb->buffer_[-2] = rb->buffer_[rb->size_ - 2];
            rb->buffer_[-1] = rb->buffer_[rb->size_ - 1];
            rb->pos_ = (rb->pos_ & rb_pos_mask) + (uint)(n & rb_pos_mask);
            if (not_first_lap)
            {
                /* Wrap, but preserve not-a-first-lap feature. */
                rb->pos_ |= 1u << 31;
            }
        }
    }
}
