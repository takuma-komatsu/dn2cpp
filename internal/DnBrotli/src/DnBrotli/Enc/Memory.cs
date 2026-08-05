// Port of c/enc/memory.{h,c} (brotli v1.1.0).
//
// The vendored C is built with the default BROTLI_ENCODER_EXIT_ON_OOM
// configuration (neither BROTLI_ENCODER_CLEANUP_ON_OOM nor _EXIT_ON_OOM is
// defined by the build, so _EXIT_ON_OOM is force-defined): MemoryManager
// carries no slot bookkeeping, BROTLI_IS_OOM is constant false, and an
// allocation failure aborts. Only that configuration is ported; the C# analog
// of exit(EXIT_FAILURE) is the OutOfMemoryException NativeMemory.Alloc throws.
//
// Custom allocators (brotli_alloc_func/brotli_free_func/opaque) are accepted
// for signature fidelity but ignored — memory always comes from the native
// heap (PORTING.md), exactly like the decoder side.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DnBrotli.Enc;

/// <summary><c>struct MemoryManager</c> (EXIT_ON_OOM shape: no slot arrays).</summary>
internal unsafe struct MemoryManager
{
    public nint alloc_func;
    public nint free_func;
    public void* opaque;

    /// <summary><c>BrotliInitMemoryManager</c>.</summary>
    internal static void BrotliInitMemoryManager(
        MemoryManager* m, nint alloc_func, nint free_func, void* opaque)
    {
        if (alloc_func == 0)
        {
            m->alloc_func = 0;  /* BrotliDefaultAllocFunc (native heap) */
            m->free_func = 0;   /* BrotliDefaultFreeFunc */
            m->opaque = null;
        }
        else
        {
            m->alloc_func = alloc_func;
            m->free_func = free_func;
            m->opaque = opaque;
        }
    }

    /// <summary><c>BrotliAllocate</c> (EXIT_ON_OOM variant): failure never returns.</summary>
    internal static void* BrotliAllocate(MemoryManager* m, nuint n)
    {
        return NativeMemory.Alloc(n);
    }

    /// <summary><c>BROTLI_ALLOC(M, T, N)</c>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T* BROTLI_ALLOC<T>(MemoryManager* m, nuint n) where T : unmanaged
    {
        /* C: `N ? BROTLI_ALLOCATOR(...) : NULL` — spelled as if/return because
           dn2cpp types the ternary arms differently (T* vs null) and rejects
           the stack merge. */
        if (n > 0)
        {
            return (T*)BrotliAllocate(m, n * (nuint)sizeof(T));
        }
        return null;
    }

    /// <summary><c>BrotliFree</c>.</summary>
    internal static void BrotliFree(MemoryManager* m, void* p)
    {
        if (p == null) return;
        NativeMemory.Free(p);
    }

    /// <summary><c>BROTLI_FREE(M, P)</c>: frees and nulls the pointer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void BROTLI_FREE<T>(MemoryManager* m, ref T* p) where T : unmanaged
    {
        BrotliFree(m, p);
        p = null;
    }

    /// <summary><c>BROTLI_IS_OOM(M)</c>: constant false in the EXIT_ON_OOM build.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool BROTLI_IS_OOM(MemoryManager* m)
    {
        return false;
    }

    /// <summary><c>BROTLI_IS_NULL(A)</c>: a static-analyzer aid, constant false.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool BROTLI_IS_NULL(void* a)
    {
        return false;
    }

    /// <summary><c>BrotliWipeOutMemoryManager</c>: no-op in the EXIT_ON_OOM build.</summary>
    internal static void BrotliWipeOutMemoryManager(MemoryManager* m)
    {
        /* BROTLI_UNUSED(m); */
    }

    /// <summary><c>BROTLI_ENSURE_CAPACITY(M, T, A, C, R)</c>.</summary>
    internal static void BROTLI_ENSURE_CAPACITY<T>(
        MemoryManager* m, ref T* a, ref nuint c, nuint r) where T : unmanaged
    {
        if (c < r)
        {
            nuint _new_size = (c == 0) ? r : c;
            T* new_array;
            while (_new_size < r) _new_size *= 2;
            new_array = BROTLI_ALLOC<T>(m, _new_size);
            if (!BROTLI_IS_OOM(m) && !BROTLI_IS_NULL(new_array) && c != 0)
            {
                Buffer.MemoryCopy(a, new_array, _new_size * (nuint)sizeof(T), c * (nuint)sizeof(T));
            }
            BROTLI_FREE(m, ref a);
            a = new_array;
            c = _new_size;
        }
    }

    /// <summary><c>BrotliBootstrapAlloc</c>: allocations not tracked by the manager;
    /// used only for the structure containing the MemoryManager itself. The C returns
    /// NULL when exactly one of the allocator pair is provided.</summary>
    internal static void* BrotliBootstrapAlloc(
        nuint size, nint alloc_func, nint free_func, void* opaque)
    {
        if (alloc_func == 0 && free_func == 0)
        {
            return NativeMemory.Alloc(size);
        }
        else if (alloc_func != 0 && free_func != 0)
        {
            return NativeMemory.Alloc(size);
        }
        return null;
    }

    /// <summary><c>BrotliBootstrapFree</c>.</summary>
    internal static void BrotliBootstrapFree(void* address, MemoryManager* m)
    {
        if (address == null)
        {
            /* Should not happen! */
            return;
        }
        NativeMemory.Free(address);
    }
}
