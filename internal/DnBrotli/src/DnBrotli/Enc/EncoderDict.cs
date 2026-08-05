// Port of c/enc/encoder_dict.{h,c} (brotli v1.1.0) — the RFC 7932 default
// path plus the compound/contextual struct shapes.
//
// Ported: the full struct hierarchy (SharedEncoderDictionary ->
// CompoundDictionary / ContextualEncoderDictionary -> BrotliEncoderDictionary),
// InitEncoderDictionary, BrotliInitSharedEncoderDictionary and
// BrotliCleanupSharedEncoderDictionary. The compound part stays inert
// (num_chunks == 0, num_prepared_instances_ == 0): the BROTLI_EXPERIMENTAL
// custom/shared-dictionary attach machinery (BuildDictionaryLut,
// BuildDictionaryHashTable, GenerateWordsHeavy, ComputeCutoffTransforms,
// BrotliInitCustomSharedEncoderDictionary) and the ManagedDictionary API are
// unreachable from the ported surface and deferred (the "DB-deferred"
// exception family).
//
// The dictionary_hash.c and static_dict_lut.h data tables live in
// DictionaryHash.g.cs / StaticDictLut.g.cs (little-endian blobs) and are
// decoded once at static init into NativeMemory for pointer-stable access,
// like the C statics (never freed).

using System.Runtime.InteropServices;

using DnBrotli.Common;

namespace DnBrotli.Enc;

/// <summary><c>struct DictWord</c> from <c>c/enc/static_dict_lut.h</c>.</summary>
internal struct DictWord
{
    /* Highest bit is used to indicate end of bucket. */
    public byte len;
    public byte transform;
    public ushort idx;
}

/// <summary><c>struct BrotliTrieNode</c>.</summary>
internal struct BrotliTrieNode
{
    public byte single;  /* if 1, sub is a single node for c instead of 256 */
    public byte c;
    public byte len_;    /* untransformed length */
    public uint idx_;    /* word index + num words * transform index */
    public uint sub;     /* index of sub node(s) in the pool */
}

/// <summary><c>struct BrotliTrie</c>.</summary>
internal unsafe struct BrotliTrie
{
    public BrotliTrieNode* pool;
    public nuint pool_capacity;
    public nuint pool_size;
    public BrotliTrieNode root;
}

/// <summary><c>struct BrotliEncoderDictionary</c>: dictionary data (words and
/// transforms) for 1 possible context.</summary>
internal unsafe struct BrotliEncoderDictionary
{
    public BrotliDictionary* words;
    public uint num_transforms;

    /* cut off for fast encoder */
    public uint cutoffTransformsCount;
    public ulong cutoffTransforms;

    /* from dictionary_hash.h, for fast encoder */
    public ushort* hash_table_words;
    public byte* hash_table_lengths;

    /* from static_dict_lut.h, for slow encoder */
    public ushort* buckets;
    public DictWord* dict_words;
    /* Heavy version, for use by slow encoder when there are custom transforms.
       Only populated by the (deferred) custom-dictionary path. */
    public BrotliTrie trie;
    public int has_words_heavy;  /* BROTLI_BOOL */

    /* Reference to other dictionaries. */
    public ContextualEncoderDictionary* parent;

    /* Allocated memory, used only when not using the Brotli defaults */
    public ushort* hash_table_data_words_;
    public byte* hash_table_data_lengths_;
    public nuint buckets_alloc_size_;
    public ushort* buckets_data_;
    public nuint dict_words_alloc_size_;
    public DictWord* dict_words_data_;
    public BrotliDictionary* words_instance_;
}

/// <summary><c>struct ContextualEncoderDictionary</c>: dictionary data for all
/// 64 contexts.</summary>
internal unsafe struct ContextualEncoderDictionary
{
    public int context_based;  /* BROTLI_BOOL */
    public byte num_dictionaries;
    public fixed byte context_map[SharedDictionary.SHARED_BROTLI_NUM_DICTIONARY_CONTEXTS];
    /* const BrotliEncoderDictionary* dict[SHARED_BROTLI_NUM_DICTIONARY_CONTEXTS]
       (pointer elements cannot form a fixed buffer; typed accessors below). */
    private fixed ulong dict_[SharedDictionary.SHARED_BROTLI_NUM_DICTIONARY_CONTEXTS];

    /* If num_instances_ is 1, instance_ is used, else dynamic allocation with
       instances_ is used. */
    public nuint num_instances_;
    public BrotliEncoderDictionary instance_;
    public BrotliEncoderDictionary* instances_;

    /* Typed accessors over the pointer-array fixed buffer above. */

    public BrotliEncoderDictionary* dict(nuint i)
    {
        return (BrotliEncoderDictionary*)dict_[i];
    }

    public void set_dict(nuint i, BrotliEncoderDictionary* value)
    {
        dict_[i] = (ulong)value;
    }
}

/// <summary><c>struct CompoundDictionary</c> from
/// <c>c/enc/compound_dictionary.h</c>. Inert in this stage: nothing attaches
/// prepared dictionaries, so only the zeroed counters are ever read.</summary>
internal unsafe struct CompoundDictionary
{
    /* LZ77 prefix, compound dictionary */
    public nuint num_chunks;
    public nuint total_size;
    /* Client instances (const PreparedDictionary* chunks[16] / const uint8_t*
       chunk_source[16] in C; pointer elements kept as ulong slots). */
    public fixed ulong chunks_[SharedDictionary.SHARED_BROTLI_MAX_COMPOUND_DICTS + 1];
    public fixed ulong chunk_source_[SharedDictionary.SHARED_BROTLI_MAX_COMPOUND_DICTS + 1];
    public fixed ulong chunk_offsets[SharedDictionary.SHARED_BROTLI_MAX_COMPOUND_DICTS + 1];  /* size_t[] */

    public nuint num_prepared_instances_;
    /* Owned instances (PreparedDictionary* prepared_instances_[16] in C). */
    public fixed ulong prepared_instances_[SharedDictionary.SHARED_BROTLI_MAX_COMPOUND_DICTS + 1];
}

/// <summary><c>struct SharedEncoderDictionary</c>.</summary>
internal unsafe struct SharedEncoderDictionary
{
    /* Magic value to distinguish this struct from PreparedDictionary for
       certain external usages. */
    public uint magic;

    /* LZ77 prefix, compound dictionary */
    public CompoundDictionary compound;

    /* Custom static dictionary (optionally context-based) */
    public ContextualEncoderDictionary contextual;

    /* The maximum quality the dictionary was computed for */
    public int max_quality;
}

internal static unsafe class EncoderDict
{
    /* kSharedDictionaryMagic from c/enc/compound_dictionary.h. */
    internal const uint kSharedDictionaryMagic = 0xDEBCEDE1;

    /* ==================== static data (decoded from .g.cs blobs) ==================== */

    /// <summary><c>kStaticDictionaryHashWords</c> (c/enc/dictionary_hash.c), 32768 entries.</summary>
    internal static readonly ushort* kStaticDictionaryHashWords;
    /// <summary><c>kStaticDictionaryHashLengths</c> (c/enc/dictionary_hash.c), 32768 entries.</summary>
    internal static readonly byte* kStaticDictionaryHashLengths;
    /// <summary><c>kStaticDictionaryBuckets</c> (c/enc/static_dict_lut.h), 32768 entries.</summary>
    internal static readonly ushort* kStaticDictionaryBuckets;
    /// <summary><c>kStaticDictionaryWords</c> (c/enc/static_dict_lut.h), 31705 entries.</summary>
    internal static readonly DictWord* kStaticDictionaryWords;

    static EncoderDict()
    {
        {
            ReadOnlySpan<byte> blob = DictionaryHash.HashWordsLE;
            ushort* words = (ushort*)NativeMemory.Alloc((nuint)blob.Length);
            for (int i = 0; i < blob.Length / 2; ++i)
            {
                words[i] = (ushort)(blob[2 * i] | (blob[2 * i + 1] << 8));
            }
            kStaticDictionaryHashWords = words;
        }
        {
            ReadOnlySpan<byte> blob = DictionaryHash.HashLengths;
            byte* lengths = (byte*)NativeMemory.Alloc((nuint)blob.Length);
            blob.CopyTo(new Span<byte>(lengths, blob.Length));
            kStaticDictionaryHashLengths = lengths;
        }
        {
            ReadOnlySpan<byte> blob = StaticDictLut.BucketsLE;
            ushort* buckets = (ushort*)NativeMemory.Alloc((nuint)blob.Length);
            for (int i = 0; i < blob.Length / 2; ++i)
            {
                buckets[i] = (ushort)(blob[2 * i] | (blob[2 * i + 1] << 8));
            }
            kStaticDictionaryBuckets = buckets;
        }
        {
            ReadOnlySpan<byte> blob = StaticDictLut.WordsLE;
            /* Sized with sizeof(DictWord), not blob.Length: the 4-byte LE records
               match .NET's packed layout, but dn2cpp widens the small fields to
               int32 (sizeof == 12); the per-field decode below is layout-agnostic
               either way. */
            DictWord* words = (DictWord*)NativeMemory.Alloc(
                (nuint)(blob.Length / 4) * (nuint)sizeof(DictWord));
            for (int i = 0; i < blob.Length / 4; ++i)
            {
                words[i].len = blob[4 * i];
                words[i].transform = blob[4 * i + 1];
                words[i].idx = (ushort)(blob[4 * i + 2] | (blob[4 * i + 3] << 8));
            }
            kStaticDictionaryWords = words;
        }
    }

    /* ==================== encoder_dict.c functions ==================== */

    private static void BrotliTrieInit(BrotliTrie* trie)
    {
        trie->pool_capacity = 0;
        trie->pool_size = 0;
        trie->pool = null;

        /* Set up the root node */
        trie->root.single = 0;
        trie->root.len_ = 0;
        trie->root.idx_ = 0;
        trie->root.sub = 0;
    }

    private static void BrotliTrieFree(MemoryManager* m, BrotliTrie* trie)
    {
        MemoryManager.BrotliFree(m, trie->pool);
    }

    /* Initializes to RFC 7932 static dictionary / transforms. */
    private static void InitEncoderDictionary(BrotliEncoderDictionary* dict)
    {
        dict->words = Dictionary.BrotliGetDictionary();
        dict->num_transforms = Transforms.BrotliGetTransforms()->num_transforms;

        dict->hash_table_words = kStaticDictionaryHashWords;
        dict->hash_table_lengths = kStaticDictionaryHashLengths;
        dict->buckets = kStaticDictionaryBuckets;
        dict->dict_words = kStaticDictionaryWords;

        dict->cutoffTransformsCount = Hash.kCutoffTransformsCount;
        dict->cutoffTransforms = Hash.kCutoffTransforms;

        dict->parent = null;

        dict->hash_table_data_words_ = null;
        dict->hash_table_data_lengths_ = null;
        dict->buckets_alloc_size_ = 0;
        dict->buckets_data_ = null;
        dict->dict_words_alloc_size_ = 0;
        dict->dict_words_data_ = null;
        dict->words_instance_ = null;
        dict->has_words_heavy = 0;
        BrotliTrieInit(&dict->trie);
    }

    private static void BrotliDestroyEncoderDictionary(MemoryManager* m,
        BrotliEncoderDictionary* dict)
    {
        MemoryManager.BrotliFree(m, dict->hash_table_data_words_);
        MemoryManager.BrotliFree(m, dict->hash_table_data_lengths_);
        MemoryManager.BrotliFree(m, dict->buckets_data_);
        MemoryManager.BrotliFree(m, dict->dict_words_data_);
        MemoryManager.BrotliFree(m, dict->words_instance_);
        BrotliTrieFree(m, &dict->trie);
    }

    internal static void BrotliInitSharedEncoderDictionary(SharedEncoderDictionary* dict)
    {
        dict->magic = kSharedDictionaryMagic;

        dict->compound.num_chunks = 0;
        dict->compound.total_size = 0;
        dict->compound.chunk_offsets[0] = 0;
        dict->compound.num_prepared_instances_ = 0;

        dict->contextual.context_based = 0;
        dict->contextual.num_dictionaries = 1;
        dict->contextual.instances_ = null;
        dict->contextual.num_instances_ = 1;  /* The instance_ field */
        dict->contextual.set_dict(0, &dict->contextual.instance_);
        InitEncoderDictionary(&dict->contextual.instance_);
        dict->contextual.instance_.parent = &dict->contextual;

        dict->max_quality = BrotliConstants.MaxQuality;
    }

    internal static void BrotliCleanupSharedEncoderDictionary(MemoryManager* m,
                                                              SharedEncoderDictionary* dict)
    {
        /* C also destroys compound prepared instances here; the compound part is
           inert (num_prepared_instances_ == 0), so the loop body is unreachable. */
        if (dict->contextual.num_instances_ == 1)
        {
            BrotliDestroyEncoderDictionary(m, &dict->contextual.instance_);
        }
        else if (dict->contextual.num_instances_ > 1)
        {
            nuint i;
            for (i = 0; i < dict->contextual.num_instances_; i++)
            {
                BrotliDestroyEncoderDictionary(m, &dict->contextual.instances_[i]);
            }
            MemoryManager.BrotliFree(m, dict->contextual.instances_);
        }
    }
}
