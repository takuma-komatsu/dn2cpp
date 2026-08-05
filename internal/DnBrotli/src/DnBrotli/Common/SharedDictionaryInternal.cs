// Port of c/common/shared_dictionary_internal.h and the lifetime functions of
// c/common/shared_dictionary.c (brotli v1.1.0) — the minimal surface the
// decoder reaches through its embedded |dictionary| field.
//
// Scope (permanent deferral, mirrors the non-BROTLI_EXPERIMENTAL C build):
// serialized-dictionary parsing (DecodeSharedDictionary and friends) is NOT
// ported; BrotliSharedDictionaryAttach rejects BROTLI_SHARED_DICTIONARY_
// SERIALIZED exactly like a C build without BROTLI_EXPERIMENTAL.
//
// Layout notes:
//   - C# fixed buffers cannot hold pointers or nuint, so the C pointer arrays
//     (prefix, words, transforms) and the size_t array (prefix_size) are laid
//     out as fixed ulong buffers — identical layout on the 64-bit-only dn2cpp
//     targets — with typed accessor methods next to them.
//   - The brotli_alloc_func/brotli_free_func/opaque members are kept as
//     nint/void* fields for layout, but custom allocators are accepted and
//     ignored: all allocation goes through NativeMemory (the DnZlib rule).

using System.Runtime.InteropServices;

namespace DnBrotli.Common;

/// <summary><c>BrotliSharedDictionaryType</c> from <c>include/brotli/shared_dictionary.h</c>.
/// Values match the C enum exactly. Public because it appears in the Raw C-ABI surface
/// (<c>BrotliDecoderAttachDictionary</c>).</summary>
public enum BrotliSharedDictionaryType
{
    /// <summary>LZ77 prefix dictionary.</summary>
    BROTLI_SHARED_DICTIONARY_RAW = 0,
    /// <summary>Serialized shared dictionary (not supported by this port).</summary>
    BROTLI_SHARED_DICTIONARY_SERIALIZED = 1,
}

/// <summary><c>struct BrotliSharedDictionaryStruct</c> (aka
/// <c>BrotliSharedDictionaryInternal</c>), field-for-field.</summary>
internal unsafe struct BrotliSharedDictionaryInternal
{
    /* LZ77 prefixes (compound dictionary). */
    public uint num_prefix;  /* max SHARED_BROTLI_MAX_COMPOUND_DICTS */
    public fixed ulong prefix_size[SharedDictionary.SHARED_BROTLI_MAX_COMPOUND_DICTS];  /* size_t[] */
    private fixed ulong prefix_[SharedDictionary.SHARED_BROTLI_MAX_COMPOUND_DICTS];  /* const uint8_t*[] */

    /* If set, the context map is used to select word and transform list from 64
       contexts, if not set, the context map is not used and only words[0] and
       transforms[0] are to be used. */
    public int context_based;  /* BROTLI_BOOL */

    public fixed byte context_map[SharedDictionary.SHARED_BROTLI_NUM_DICTIONARY_CONTEXTS];

    /* Amount of word_list+transform_list combinations. */
    public byte num_dictionaries;

    /* Must use num_dictionaries values. */
    private fixed ulong words_[SharedDictionary.SHARED_BROTLI_NUM_DICTIONARY_CONTEXTS];  /* const BrotliDictionary*[] */

    /* Must use num_dictionaries values. */
    private fixed ulong transforms_[SharedDictionary.SHARED_BROTLI_NUM_DICTIONARY_CONTEXTS];  /* const BrotliTransforms*[] */

    /* Amount of custom word lists. May be 0 if only Brotli's built-in is used. */
    public byte num_word_lists;

    /* Contents of the custom words lists. Must be NULL if num_word_lists is 0. */
    public BrotliDictionary* words_instances;

    /* Amount of custom transform lists. May be 0 if only Brotli's built-in is used. */
    public byte num_transform_lists;

    /* Contents of the custom transform lists. Must be NULL if num_transform_lists is 0. */
    public BrotliTransforms* transforms_instances;

    /* Concatenated prefix_suffix_maps of the custom transform lists. Must be NULL
       if num_transform_lists is 0. */
    public ushort* prefix_suffix_maps;

    /* Memory management: accepted and ignored (layout only). */
    public nint alloc_func;
    public nint free_func;
    public void* memory_manager_opaque;

    /* Typed accessors over the pointer-array fixed buffers above. */

    public byte* prefix(int i)
    {
        return (byte*)prefix_[i];
    }

    public void set_prefix(int i, byte* value)
    {
        prefix_[i] = (ulong)value;
    }

    public BrotliDictionary* words(int i)
    {
        return (BrotliDictionary*)words_[i];
    }

    public void set_words(int i, BrotliDictionary* value)
    {
        words_[i] = (ulong)value;
    }

    public BrotliTransforms* transforms(int i)
    {
        return (BrotliTransforms*)transforms_[i];
    }

    public void set_transforms(int i, BrotliTransforms* value)
    {
        transforms_[i] = (ulong)value;
    }
}

internal static unsafe class SharedDictionary
{
    internal const int SHARED_BROTLI_MIN_DICTIONARY_WORD_LENGTH = 4;
    internal const int SHARED_BROTLI_MAX_DICTIONARY_WORD_LENGTH = 31;
    internal const int SHARED_BROTLI_NUM_DICTIONARY_CONTEXTS = 64;
    internal const int SHARED_BROTLI_MAX_COMPOUND_DICTS = 15;

    /// <summary><c>BrotliSharedDictionaryCreateInstance</c>. Custom allocator arguments do
    /// not exist in this port (NativeMemory is always used); the C behavior for the
    /// default-allocator case is reproduced exactly.</summary>
    internal static BrotliSharedDictionaryInternal* BrotliSharedDictionaryCreateInstance()
    {
        BrotliSharedDictionaryInternal* dict = (BrotliSharedDictionaryInternal*)
            NativeMemory.Alloc((nuint)sizeof(BrotliSharedDictionaryInternal));
        if (dict == null)
        {
            return null;
        }

        /* memset(dict, 0, sizeof(BrotliSharedDictionary)); */
        NativeMemory.Clear(dict, (nuint)sizeof(BrotliSharedDictionaryInternal));

        dict->context_based = 0;  /* BROTLI_FALSE */
        dict->num_dictionaries = 1;
        dict->num_word_lists = 0;
        dict->num_transform_lists = 0;

        dict->set_words(0, Dictionary.BrotliGetDictionary());
        dict->set_transforms(0, Transforms.BrotliGetTransforms());

        return dict;
    }

    /// <summary><c>BrotliSharedDictionaryDestroyInstance</c>.</summary>
    internal static void BrotliSharedDictionaryDestroyInstance(BrotliSharedDictionaryInternal* dict)
    {
        if (dict == null)
        {
            return;
        }
        else
        {
            /* Cleanup. */
            NativeMemory.Free(dict->words_instances);
            NativeMemory.Free(dict->transforms_instances);
            NativeMemory.Free(dict->prefix_suffix_maps);
            /* Self-destruction. */
            NativeMemory.Free(dict);
        }
    }

    /// <summary><c>BrotliSharedDictionaryAttach</c>. Serialized dictionaries are rejected,
    /// matching a C build without <c>BROTLI_EXPERIMENTAL</c>.</summary>
    internal static int BrotliSharedDictionaryAttach(
        BrotliSharedDictionaryInternal* dict, BrotliSharedDictionaryType type,
        nuint data_size, byte* data)
    {
        if (dict == null)
        {
            return 0;
        }
        if (type == BrotliSharedDictionaryType.BROTLI_SHARED_DICTIONARY_RAW)
        {
            if (dict->num_prefix >= SHARED_BROTLI_MAX_COMPOUND_DICTS)
            {
                return 0;
            }
            dict->prefix_size[dict->num_prefix] = data_size;
            dict->set_prefix((int)dict->num_prefix, data);
            dict->num_prefix++;
            return 1;
        }
        return 0;
    }
}
