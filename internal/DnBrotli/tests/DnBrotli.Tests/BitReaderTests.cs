using DnBrotli.Dec;
using static DnBrotli.Dec.BitReader;

namespace DnBrotli.Tests;

/// <summary>Exercises the c/dec/bit_reader.{h,c} port over crafted buffers: init/warmup,
/// unsafe and safe reads across 32-bit refill boundaries, peeking, byte-boundary jumps,
/// byte copies and state save/restore.</summary>
public unsafe class BitReaderTests
{
    private static byte[] MakeInput(int len, int seed)
    {
        byte[] data = new byte[len];
        new Random(seed).NextBytes(data);
        return data;
    }

    /// <summary>Reference model: brotli consumes bits LSB-first from the byte stream.</summary>
    private static ulong RefBits(byte[] data, int bitPos, int n)
    {
        ulong v = 0;
        for (int i = 0; i < n; ++i)
        {
            int p = bitPos + i;
            v |= (ulong)((data[p >> 3] >> (p & 7)) & 1) << i;
        }
        return v;
    }

    [Fact]
    public void InitSetInputAndWarmup()
    {
        byte[] input = MakeInput(40, seed: 1);
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            Assert.Equal((nuint)0, br.val_);
            Assert.Equal((nuint)0, br.bit_pos_);

            BrotliBitReaderSetInput(&br, p, 40);
            Assert.True(br.last_in == p + 40);
            Assert.Equal((nuint)40, BrotliBitReaderGetAvailIn(&br));
            Assert.Equal(1, BrotliCheckInputAmount(&br));  // 40 + 1 > BROTLI_FAST_INPUT_SLACK

            Assert.Equal(1, BrotliWarmupBitReader(&br));
            Assert.Equal((nuint)8, BrotliGetAvailableBits(&br));
            Assert.Equal((nuint)input[0], BrotliGetBitsUnmasked(&br));
            Assert.Equal((nuint)40, BrotliGetRemainingBytes(&br));  // 39 unread + 1 buffered
        }
    }

    [Fact]
    public void WarmupFailsOnEmptyInput()
    {
        byte[] input = new byte[1];
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 0);
            Assert.Equal(0, BrotliCheckInputAmount(&br));  // guard_in == next_in
            Assert.Equal(0, BrotliWarmupBitReader(&br));
        }
    }

    [Fact]
    public void ReadBits24AcrossRefillBoundaries()
    {
        byte[] input = MakeInput(64, seed: 2);
        // Repeated 24-bit reads force a 32-bit refill on almost every call.
        int[] widths = { 1, 2, 7, 24, 16, 3, 24, 24, 8, 13, 5, 24, 24, 24, 1, 24 };
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 64);
            Assert.Equal(1, BrotliWarmupBitReader(&br));

            int bitPos = 0;
            foreach (int n in widths)
            {
                nuint got = BrotliReadBits24(&br, (nuint)n);
                Assert.Equal(RefBits(input, bitPos, n), got);
                bitPos += n;
            }
        }
    }

    [Fact]
    public void SafeReadBitsMatchesReferenceAndStopsAtEnd()
    {
        byte[] input = MakeInput(5, seed: 3);
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 5);

            int bitPos = 0;
            foreach (int n in (int[])[3, 11, 24, 1])
            {
                nuint val;
                Assert.Equal(1, BrotliSafeReadBits(&br, (nuint)n, &val));
                Assert.Equal(RefBits(input, bitPos, n), val);
                bitPos += n;
            }
            // 39 of 40 bits consumed; a 2-bit read must fail.
            nuint tail;
            Assert.Equal(0, BrotliSafeReadBits(&br, 2, &tail));
        }
    }

    [Fact]
    public void SafeReadBits32AndSlowVariant()
    {
        byte[] input = MakeInput(16, seed: 4);
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 16);

            nuint val;
            Assert.Equal(1, BrotliSafeReadBits32(&br, 32, &val));
            Assert.Equal(RefBits(input, 0, 32), val);

            Assert.Equal(1, BrotliSafeReadBits32Slow(&br, 30, &val));
            Assert.Equal(RefBits(input, 32, 30), val);
        }

        // The slow variant restores the reader state when input runs out mid-read.
        byte[] shortInput = MakeInput(3, seed: 5);
        fixed (byte* p = shortInput)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 3);
            nuint val;
            byte* nextBefore = br.next_in;
            Assert.Equal(0, BrotliSafeReadBits32Slow(&br, 32, &val));
            Assert.True(br.next_in == nextBefore);
            Assert.Equal((nuint)0, br.bit_pos_);
            // The reader is still usable after the failed attempt.
            Assert.Equal(1, BrotliSafeReadBits(&br, 24, &val));
            Assert.Equal(RefBits(shortInput, 0, 24), val);
        }
    }

    [Fact]
    public void GetBitsPeeksWithoutAdvancing()
    {
        byte[] input = MakeInput(40, seed: 6);
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 40);
            Assert.Equal(1, BrotliWarmupBitReader(&br));

            nuint peek1 = BrotliGetBits(&br, 14);
            nuint peek2 = BrotliGetBits(&br, 14);
            Assert.Equal(peek1, peek2);
            Assert.Equal(RefBits(input, 0, 14), peek1);

            nuint safePeek;
            Assert.Equal(1, BrotliSafeGetBits(&br, 14, &safePeek));
            Assert.Equal(peek1, safePeek);

            nuint taken;
            BrotliTakeBits(&br, 14, &taken);
            Assert.Equal(peek1, taken);
            Assert.Equal(RefBits(input, 14, 10), BrotliGetBits(&br, 10));
        }
    }

    [Fact]
    public void FillBitWindow16AndDropBits()
    {
        byte[] input = MakeInput(40, seed: 7);
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 40);
            Assert.Equal(1, BrotliWarmupBitReader(&br));

            BrotliFillBitWindow16(&br);
            Assert.True(BrotliGetAvailableBits(&br) >= 17);
            Assert.Equal(RefBits(input, 0, 16), BrotliGet16BitsUnmasked(&br) & 0xFFFF);

            BrotliDropBits(&br, 9);
            Assert.Equal(RefBits(input, 9, 16), BrotliGet16BitsUnmasked(&br) & 0xFFFF);
        }
    }

    [Fact]
    public void JumpToByteBoundaryVerifiesZeroPadding()
    {
        // Low 3 bits carry a value; the 5 pad bits up to the byte boundary are zero.
        byte[] input = [0b0000_0101, 0xAB, 0xCD, 0, 0, 0, 0, 0];
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 8);
            nuint val;
            Assert.Equal(1, BrotliSafeReadBits(&br, 3, &val));
            Assert.Equal((nuint)5, val);
            Assert.Equal(1, BrotliJumpToByteBoundary(&br));
            Assert.Equal(1, BrotliSafeReadBits(&br, 8, &val));
            Assert.Equal((nuint)0xAB, val);
        }

        // Non-zero pad bits must be rejected.
        byte[] dirty = [0xFF, 0xAB, 0, 0, 0, 0, 0, 0];
        fixed (byte* p = dirty)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 8);
            nuint val;
            Assert.Equal(1, BrotliSafeReadBits(&br, 3, &val));
            Assert.Equal(0, BrotliJumpToByteBoundary(&br));
        }
    }

    [Fact]
    public void CopyBytesDrainsAccumulatorThenInput()
    {
        byte[] input = MakeInput(24, seed: 8);
        byte[] dest = new byte[10];
        fixed (byte* p = input)
        fixed (byte* d = dest)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 24);
            // Fill the window so whole bytes sit in the accumulator, then consume one
            // byte so the copy starts at offset 1 (byte-aligned: 32 bits buffered - 8).
            BrotliFillBitWindow16(&br);
            BrotliDropBits(&br, 8);
            Assert.Equal(1, BrotliJumpToByteBoundary(&br));

            BrotliCopyBytes(d, &br, 10);
            Assert.Equal(input.AsSpan(1, 10).ToArray(), dest);

            // Reader continues correctly after the detour.
            Assert.Equal(1, BrotliWarmupBitReader(&br));
            nuint val;
            Assert.Equal(1, BrotliSafeReadBits(&br, 8, &val));
            Assert.Equal((nuint)input[11], val);
        }
    }

    [Fact]
    public void SaveAndRestoreStateReplaysReads()
    {
        byte[] input = MakeInput(40, seed: 9);
        fixed (byte* p = input)
        {
            BrotliBitReader br;
            BrotliInitBitReader(&br);
            BrotliBitReaderSetInput(&br, p, 40);
            Assert.Equal(1, BrotliWarmupBitReader(&br));

            nuint val;
            Assert.Equal(1, BrotliSafeReadBits(&br, 13, &val));

            BrotliBitReaderState memento;
            BrotliBitReaderSaveState(&br, &memento);

            nuint first = BrotliReadBits24(&br, 24);
            nuint second = BrotliReadBits24(&br, 24);

            BrotliBitReaderRestoreState(&br, &memento);
            Assert.Equal(first, BrotliReadBits24(&br, 24));
            Assert.Equal(second, BrotliReadBits24(&br, 24));
        }
    }

    [Fact]
    public void BitMaskMatchesTable()
    {
        for (int i = 0; i <= 32; ++i)
        {
            Assert.Equal(kBrotliBitMask[i], BitMask((nuint)i));
        }
    }
}
