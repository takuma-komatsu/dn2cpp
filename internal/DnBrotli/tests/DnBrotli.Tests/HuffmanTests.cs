using DnBrotli.Common;
using DnBrotli.Dec;
using static DnBrotli.Dec.Huffman;

namespace DnBrotli.Tests;

/// <summary>Exercises the c/dec/huffman.{h,c} port: simple tables, the code-lengths
/// table and the two-level general table built from a known code-length vector.</summary>
public unsafe class HuffmanTests
{
    /// <summary>Decodes one symbol from a (possibly two-level) table built with
    /// <c>root_bits</c>, given an LSB-first bit pattern — the same walk decode.c's
    /// <c>ReadSymbol</c> performs. Returns (consumed bits, symbol).</summary>
    private static (int bits, int value) Decode(HuffmanCode* table, int root_bits, uint pattern)
    {
        uint rootIndex = pattern & ((1u << root_bits) - 1);
        HuffmanCode e = table[rootIndex];
        if (e.bits <= root_bits)
        {
            return (e.bits, e.value);
        }
        int nbits = e.bits - root_bits;
        HuffmanCode* sub = table + rootIndex + e.value;
        HuffmanCode e2 = sub[(pattern >> root_bits) & ((1u << nbits) - 1)];
        return (root_bits + e2.bits, e2.value);
    }

    [Fact]
    public void BuildSimpleHuffmanTableTwoSymbols()
    {
        HuffmanCode* table = stackalloc HuffmanCode[8];
        ushort* val = stackalloc ushort[4];
        val[0] = 5;
        val[1] = 2;

        // num_symbols == 1 means two symbols (1-bit codes); val is not pre-sorted.
        uint size = BrotliBuildSimpleHuffmanTable(table, root_bits: 2, val, num_symbols: 1);
        Assert.Equal(4u, size);

        // Lower symbol value gets the 0 bit.
        Assert.Equal((1, 2), Decode(table, 2, 0b00));
        Assert.Equal((1, 5), Decode(table, 2, 0b01));
        // Replicated across the 2-bit root table.
        Assert.Equal((1, 2), Decode(table, 2, 0b10));
        Assert.Equal((1, 5), Decode(table, 2, 0b11));
    }

    [Fact]
    public void BuildSimpleHuffmanTableFourSymbolsSkewed()
    {
        HuffmanCode* table = stackalloc HuffmanCode[8];
        ushort* val = stackalloc ushort[4];
        val[0] = 10;
        val[1] = 20;
        val[2] = 40;  // deliberately unsorted pair (40, 30):
        val[3] = 30;  // the builder swaps val[2]/val[3]

        // num_symbols == 4 means four symbols with lengths [1, 2, 3, 3].
        uint size = BrotliBuildSimpleHuffmanTable(table, root_bits: 3, val, num_symbols: 4);
        Assert.Equal(8u, size);

        // Codes (LSB-first): 0 -> val[0]; 01 -> val[1]; 011 -> val[2]; 111 -> val[3].
        Assert.Equal((1, 10), Decode(table, 3, 0b000));
        Assert.Equal((1, 10), Decode(table, 3, 0b110));
        Assert.Equal((2, 20), Decode(table, 3, 0b001));
        Assert.Equal((2, 20), Decode(table, 3, 0b101));
        Assert.Equal((3, 30), Decode(table, 3, 0b011));  // swapped into val[2] slot
        Assert.Equal((3, 40), Decode(table, 3, 0b111));
    }

    [Fact]
    public void BuildCodeLengthsHuffmanTable()
    {
        // Code lengths (in symbol order) for the BROTLI_CODE_LENGTH_CODES == 18 symbols:
        // symbol 0..5 get lengths 1,2,3,4,5,5 (Kraft-complete), the rest are unused.
        byte* code_lengths = stackalloc byte[BrotliConstants.BROTLI_CODE_LENGTH_CODES];
        int[] lengths = [1, 2, 3, 4, 5, 5];
        for (int s = 0; s < lengths.Length; ++s) code_lengths[s] = (byte)lengths[s];

        ushort* count = stackalloc ushort[16];
        new Span<ushort>(count, 16).Clear();
        foreach (int l in lengths) count[l]++;

        HuffmanCode* table = stackalloc HuffmanCode[32];
        BrotliBuildCodeLengthsHuffmanTable(table, code_lengths, count);

        // Every 5-bit pattern decodes to the symbol owning that prefix; each symbol of
        // length L owns 2^(5-L) of the 32 slots and reports bits == L.
        int[] hits = new int[lengths.Length];
        for (uint pattern = 0; pattern < 32; ++pattern)
        {
            HuffmanCode e = table[pattern];
            Assert.InRange(e.value, 0, lengths.Length - 1);
            Assert.Equal(lengths[e.value], e.bits);
            hits[e.value]++;
        }
        for (int s = 0; s < lengths.Length; ++s)
        {
            Assert.Equal(32 >> lengths[s], hits[s]);
        }

        // First symbol (1-bit code 0) occupies every even slot.
        Assert.Equal(0, table[0].value);
        Assert.Equal(0, table[30].value);
    }

    [Fact]
    public void BuildCodeLengthsHuffmanTableDegenerate()
    {
        // All symbols but one have zero code length: table is flooded with that symbol.
        byte* code_lengths = stackalloc byte[BrotliConstants.BROTLI_CODE_LENGTH_CODES];
        new Span<byte>(code_lengths, BrotliConstants.BROTLI_CODE_LENGTH_CODES).Clear();
        code_lengths[17] = 1;

        ushort* count = stackalloc ushort[16];
        new Span<ushort>(count, 16).Clear();
        count[1] = 1;

        HuffmanCode* table = stackalloc HuffmanCode[32];
        BrotliBuildCodeLengthsHuffmanTable(table, code_lengths, count);
        for (uint pattern = 0; pattern < 32; ++pattern)
        {
            Assert.Equal(0, table[pattern].bits);
            Assert.Equal(17, table[pattern].value);
        }
    }

    [Fact]
    public void BuildHuffmanTableTwoLevel()
    {
        // Code lengths per symbol: 1,2,3,4,5,6,7,7 (Kraft-complete); with root_bits == 5
        // the 6- and 7-bit codes land in 2nd-level tables.
        int[] lengths = [1, 2, 3, 4, 5, 6, 7, 7];

        // Build count[] and the symbol_lists linked chains exactly like decode.c's
        // ReadHuffmanCode does before calling BrotliBuildHuffmanTable
        // (c/dec/decode.c lines 834-837 and ProcessSingleCodeLength, lines 524-541).
        ushort* symbols_lists_array = stackalloc ushort[
            BROTLI_HUFFMAN_MAX_CODE_LENGTH + 1 + BrotliConstants.BROTLI_NUM_COMMAND_SYMBOLS];
        ushort* symbol_lists = symbols_lists_array + (BROTLI_HUFFMAN_MAX_CODE_LENGTH + 1);
        int* next_symbol = stackalloc int[32];
        ushort* count = stackalloc ushort[16];
        new Span<ushort>(count, 16).Clear();
        for (int i = 0; i <= BROTLI_HUFFMAN_MAX_CODE_LENGTH; ++i)
        {
            next_symbol[i] = i - (BROTLI_HUFFMAN_MAX_CODE_LENGTH + 1);
            symbol_lists[next_symbol[i]] = 0xFFFF;
        }
        for (int symbol = 0; symbol < lengths.Length; ++symbol)
        {
            int code_len = lengths[symbol];
            symbol_lists[next_symbol[code_len]] = (ushort)symbol;
            next_symbol[code_len] = symbol;
            count[code_len]++;
        }

        HuffmanCode* table = stackalloc HuffmanCode[BROTLI_HUFFMAN_MAX_SIZE_26];
        uint total = BrotliBuildHuffmanTable(table, root_bits: 5, symbol_lists, count);
        // Root (32) + one 2-bit second-level table (4) shared by the 6- and 7-bit codes
        // (all of them share the 5-bit root prefix).
        Assert.Equal(32u + 4u, total);

        // Walk all 7-bit patterns; each symbol of length L owns 2^(7-L) patterns and
        // decodes with exactly L consumed bits.
        int[] hits = new int[lengths.Length];
        for (uint pattern = 0; pattern < 128; ++pattern)
        {
            (int bits, int value) = Decode(table, 5, pattern);
            Assert.InRange(value, 0, lengths.Length - 1);
            Assert.Equal(lengths[value], bits);
            hits[value]++;
        }
        for (int s = 0; s < lengths.Length; ++s)
        {
            Assert.Equal(128 >> lengths[s], hits[s]);
        }
    }
}
