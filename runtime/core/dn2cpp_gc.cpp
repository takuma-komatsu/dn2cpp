// dn2cpp_gc.cpp — GC integration of the dn2cpp runtime:
// Boehm init + dn2cpp_runtime_init, GC mode/env
// config, thread & native-callback registration, thread-statics, allocators,
// static-constructor guards, quiesce, the finalizer thread, and GCHandle.

#include "dn2cpp_core.h"
#include <algorithm>
#include <chrono>  // DateTime.Now/UtcNow wall clock
#include <ctime>   // localtime_r for DateTime.Now local components
#include <string>  // Path/File/Environment helpers
#include <vector>  // Path.GetFullPath segment stack
#ifndef _WIN32
#include <dlfcn.h> // dlsym for dn2cpp_native_library_get_symbol
#endif
#if defined(DN2CPP_USE_BOEHM_GC) && defined(__APPLE__)
#include <dlfcn.h>          // dladdr — locate the image holding the runtime
#include <mach-o/getsect.h> // getsegmentdata — its __DATA segment bounds
#include <mach-o/loader.h>  // mach_header_64
#endif
#include <cstdio>   // File read/write (fopen/fread/fwrite)
#include <cerrno>   // File.Delete errno/ENOENT
#include <cstdlib>  // Environment.GetEnvironmentVariable getenv
#include <cstring>  // Type.Assembly identity name compare (dn2cpp_assembly_equals)
#ifdef _MSC_VER
#include <malloc.h> // _aligned_malloc/_aligned_realloc/_aligned_free (no std::aligned_alloc on MSVC)
#endif
#ifdef _WIN32
// GetLastError() for dn2cpp_pinvoke_capture_last_error. NOMINMAX / WIN32_LEAN_AND_MEAN
// are project-wide compile definitions (runtime/CMakeLists.txt's MSVC arm), not
// repeated here — see platform/windows/dn2cpp_pal_windows.cpp's header comment.
#include <windows.h>
#include <io.h>     // _setmode / _fileno — put stdout/stderr in binary mode at init
#include <fcntl.h>  // _O_BINARY
#endif
#include <thread>              // real OS threads (Thread, worker pool)
#include <mutex>               // scheduler run-queue + task-completion + console locks
#include <condition_variable> // task_block sleeps until a cross-thread completion
#include <atomic>             // g_inflight_async_tasks, real Interlocked
#include <deque>             // Task.Run worker-pool work queue
#include <new>               // placement new (in-place ctor of a GC-allocated Timer)
#include <unordered_map>     // Dn2CppType intern table (one Type per type-info handle)

// Allocation-size query for NativeMemory.AlignedRealloc routes through the PAL
// (malloc_size on macOS, malloc_usable_size on glibc/musl/BSD).
#include "platform/dn2cpp_pal.h"

#include "dn2cpp_runtime_internal.h" // cross-TU internals of the split runtime unit

// Boehm GC is vendored under third_party/bdwgc and compiled into the runtime
// (see gates/_common.sh::_cache_gc_obj). DN2CPP_USE_BOEHM_GC is defined by
// default; build with DN2CPP_NO_GC=1 to opt out to the calloc fallback.
#ifdef DN2CPP_USE_BOEHM_GC
#include <gc.h>

// ── GC stop-the-world pause-time instrumentation (opt-in: DN2CPP_GC_STATS) ─────
// A diagnostic (not on any hot path) for measuring collection pauses — used by
// gates/measure-gcpause.sh to compare incremental vs stop-the-world mode. The
// on-collection-event callback runs with the GC lock held / the world stopped, so
// it performs NO GC calls and NO allocation: it only reads steady_clock and bumps
// relaxed atomics. The summary (which queries GC_get_gc_no / GC_get_heap_size, both
// of which take the GC lock) is printed from an atexit handler, outside collection.
namespace
{
constexpr int kPauseBucketCount = 7;
// Exclusive upper bounds in ns for the first 6 buckets; the 7th is the overflow.
constexpr long long kPauseBucketNs[kPauseBucketCount - 1] = {
    100000LL, 500000LL, 1000000LL, 5000000LL, 10000000LL, 50000000LL
};
const char* const kPauseBucketLabel[kPauseBucketCount] = {
    "<0.1ms", "<0.5ms", "<1ms", "<5ms", "<10ms", "<50ms", ">=50ms"
};

// Static storage duration → zero-initialized before any use (the atomic default
// ctor leaves the already-zeroed storage untouched), so no explicit init needed.
std::atomic<unsigned long long> g_pause_count;
std::atomic<unsigned long long> g_pause_total_ns;
std::atomic<unsigned long long> g_pause_max_ns;
std::atomic<unsigned long long> g_pause_hist[kPauseBucketCount];
// Written at POST_STOP_WORLD, read at PRE_START_WORLD. The STW window is single-
// threaded and collections never overlap, so a plain static suffices.
std::chrono::steady_clock::time_point g_stop_world_at;

void GC_CALLBACK dn2cpp_gc_event(GC_EventType ev)
{
    if (ev == GC_EVENT_POST_STOP_WORLD)
    {
        g_stop_world_at = std::chrono::steady_clock::now();
        return;
    }
    if (ev != GC_EVENT_PRE_START_WORLD)
        return;

    auto ns = std::chrono::duration_cast<std::chrono::nanoseconds>(
                  std::chrono::steady_clock::now() - g_stop_world_at).count();
    if (ns < 0)
        ns = 0;
    unsigned long long u = static_cast<unsigned long long>(ns);

    g_pause_count.fetch_add(1, std::memory_order_relaxed);
    g_pause_total_ns.fetch_add(u, std::memory_order_relaxed);
    unsigned long long prev = g_pause_max_ns.load(std::memory_order_relaxed);
    while (u > prev && !g_pause_max_ns.compare_exchange_weak(prev, u, std::memory_order_relaxed))
        ; // retry: prev refreshed by compare_exchange_weak

    int bucket = kPauseBucketCount - 1;
    for (int i = 0; i < kPauseBucketCount - 1; ++i)
        if (ns < kPauseBucketNs[i]) { bucket = i; break; }
    g_pause_hist[bucket].fetch_add(1, std::memory_order_relaxed);
}

void dn2cpp_gc_stats_dump()
{
    unsigned long long count = g_pause_count.load();
    unsigned long long total = g_pause_total_ns.load();
    unsigned long long maxns = g_pause_max_ns.load();
    std::fprintf(stderr, "\n=== dn2cpp GC pause stats ===\n");
    std::fprintf(stderr, "collections (GC_no): %lu\n", static_cast<unsigned long>(GC_get_gc_no()));
    std::fprintf(stderr, "heap size:           %.2f MB\n",
                 static_cast<double>(GC_get_heap_size()) / (1024.0 * 1024.0));
    std::fprintf(stderr, "STW pauses:          %llu\n", count);
    std::fprintf(stderr, "  max pause:         %.3f ms\n", maxns / 1e6);
    std::fprintf(stderr, "  total pause:       %.3f ms\n", total / 1e6);
    std::fprintf(stderr, "  mean pause:        %.3f ms\n",
                 count ? (static_cast<double>(total) / static_cast<double>(count)) / 1e6 : 0.0);
    std::fprintf(stderr, "  histogram:\n");
    for (int i = 0; i < kPauseBucketCount; ++i)
        std::fprintf(stderr, "    %-7s %llu\n", kPauseBucketLabel[i], g_pause_hist[i].load());
    std::fprintf(stderr, "=============================\n");
}

// The summary must print exactly once whether the process leaves through the
// exit funnel (which skips atexit) or through a host that returns and runs the
// atexit chain — both call this.
std::once_flag g_gc_stats_once;
void dn2cpp_gc_stats_dump_once()
{
    std::call_once(g_gc_stats_once, dn2cpp_gc_stats_dump);
}

// ── Suppressed-finalizer set work counters (opt-in report: DN2CPP_GC_SUPPRESS_STATS)
// The set is walked slot by slot, so a drain's cost tracks the set's high-water mark
// rather than its live size. These count that work directly; a wall clock cannot
// separate it from collection time. Independent of DN2CPP_GC_STATS on purpose —
// enabling one must not move the other's stderr.
//
// The walk runs under Boehm's allocator lock, where nothing may allocate, collect or
// do I/O — relaxed adds are all these are. They are unconditional: a knob test on the
// suppress and dequeue paths would cost more than the add it guards.
std::atomic<uint64_t> g_suppress_calls;      // SuppressFinalize past the no-Finalize return
std::atomic<uint64_t> g_suppress_recorded;   // …of which entered the set
std::atomic<uint64_t> g_suppress_dequeues;   // finalizer bodies dequeued
std::atomic<uint64_t> g_suppress_probes;     // …of which missed the bound and took the mutex
std::atomic<uint64_t> g_suppress_scans;      // slot walks
std::atomic<uint64_t> g_suppress_slots_walked;
std::atomic<uint64_t> g_suppress_chunks;     // chunks currently on the chain

void dn2cpp_gc_suppress_stats_dump()
{
    unsigned long long scans = g_suppress_scans.load();
    unsigned long long walked = g_suppress_slots_walked.load();
    std::fprintf(stderr, "\n=== dn2cpp GC suppress stats ===\n");
    std::fprintf(stderr, "suppress calls:      %llu\n",
                 static_cast<unsigned long long>(g_suppress_calls.load()));
    std::fprintf(stderr, "  set entries added: %llu\n",
                 static_cast<unsigned long long>(g_suppress_recorded.load()));
    std::fprintf(stderr, "dequeues:            %llu\n",
                 static_cast<unsigned long long>(g_suppress_dequeues.load()));
    std::fprintf(stderr, "  locked probes:     %llu\n",
                 static_cast<unsigned long long>(g_suppress_probes.load()));
    std::fprintf(stderr, "scans:               %llu\n", scans);
    std::fprintf(stderr, "slots walked:        %llu\n", walked);
    std::fprintf(stderr, "  slots per scan:    %.1f\n",
                 scans ? static_cast<double>(walked) / static_cast<double>(scans) : 0.0);
    std::fprintf(stderr, "chunks:              %llu\n",
                 static_cast<unsigned long long>(g_suppress_chunks.load()));
    std::fprintf(stderr, "================================\n");
}

std::once_flag g_gc_suppress_stats_once;
void dn2cpp_gc_suppress_stats_dump_once()
{
    std::call_once(g_gc_suppress_stats_once, dn2cpp_gc_suppress_stats_dump);
}

// Parse a boolean env override (0/1, true/false, yes/no, on/off — first letter,
// case-insensitive; "on"/"off" disambiguated by the 2nd char). Returns def unset.
bool dn2cpp_env_bool(const char* name, bool def)
{
    const char* v = dn2cpp_pal_getenv(name);
    if (v == nullptr || v[0] == '\0')
        return def;
    switch (v[0])
    {
        case '0': case 'f': case 'F': case 'n': case 'N': return false;
        case '1': case 't': case 'T': case 'y': case 'Y': return true;
        case 'o': case 'O': return !(v[1] == 'f' || v[1] == 'F'); // off -> false, on -> true
        default:  return def;
    }
}
} // namespace
#endif

// GC collection mode default. Console keeps the classic stop-the-world collector
// (lowest overhead for batch/throughput work); the Godot GDExtension flips this to
// Boehm's incremental mode for bounded frame pauses by calling the setter below
// before managed init runs. Either default is overridable at runtime via
// DN2CPP_GC_INCREMENTAL (see dn2cpp_runtime_init). Defined unconditionally so the
// setter links even under DN2CPP_NO_GC (where it is simply inert).
static int g_gc_incremental_default = 0;

void dn2cpp_gc_set_incremental_default(int on)
{
    g_gc_incremental_default = on ? 1 : 0;
}

// Apple static-roots mode default. Off, a binary keeps Boehm's stock
// dynamic-library scanning (every loaded image's data segments become roots
// via the dyld callbacks). The Godot hosts flip this on before managed init:
// a windowed engine process loads hundreds of system frameworks, which
// overflows the collector's root-set table (bdwgc aborts with "Too many root
// sets", MAX_ROOT_SETS = 2048) and makes every full mark scan framework data
// that can never hold managed pointers — so there the runtime disables the
// scanning and registers only its own image's __DATA instead (see
// dn2cpp_gc_register_host_image_roots). Scoped to the hosts that need it
// rather than applied always-on: console processes load few images, and the
// full parallel gate suite showed a saturation-only GC_stop_world abort in
// the dlopen-host gate with the mode always-on that never reproduced under
// any targeted load. Either default is overridable at runtime via
// DN2CPP_GC_SELF_ROOTS (see dn2cpp_runtime_init).
static int g_gc_self_roots_default = 0;

void dn2cpp_gc_set_self_roots_default(int on)
{
    g_gc_self_roots_default = on ? 1 : 0;
}

// Finalizer drain mode. Console (and anything that never flips this) keeps the
// dedicated finalizer thread: the GC callback enqueues onto the ring and a
// background thread runs each managed Finalize() body. A host with a main-thread
// affinity — the Godot GDExtension, where object destroy / unreference must run
// on the engine's main thread — flips this on before managed init: the finalizer
// thread is never started, and the ring is drained on whatever thread calls
// dn2cpp_gc_drain_finalizers() / GC.WaitForPendingFinalizers() instead. Set-once
// before any thread spawns, so a plain int (like g_gc_incremental_default) is
// enough; defined unconditionally so the setter links under DN2CPP_NO_GC too.
#ifdef __EMSCRIPTEN__
// wasm has no threads, so the dedicated finalizer thread cannot exist: default
// to manual drain, where Finalize() bodies run on whoever calls
// GC.WaitForPendingFinalizers() or the Godot per-frame drain hook.
static int g_finalizer_manual_drain = 1;
#else
static int g_finalizer_manual_drain = 0;
#endif

void dn2cpp_gc_set_manual_finalizer_drain(int on)
{
    g_finalizer_manual_drain = on ? 1 : 0;
}

// Hot-update external allocation hook (see dn2cpp.h). Defined here — not in
// dn2cpp_interp.cpp — so an embedding layer (the Godot GDExtension init) can
// register it unconditionally without pulling the interpreter TU, and its
// base-image-only symbols, into programs that never carry hot-update support.
// The interpreter's patch `newobj` consults it before its default allocation.
Dn2CppObject* (*dn2cpp_interp_alloc_hook)(const Dn2CppTypeInfo* type, size_t size) = nullptr;

void dn2cpp_interp_set_alloc_hook(Dn2CppObject* (*hook)(const Dn2CppTypeInfo* type, size_t size))
{
    dn2cpp_interp_alloc_hook = hook;
}

// Native-callback GC registration (default off). An [UnmanagedCallersOnly] method
// can be invoked from a thread the collector has never seen (a native host's own
// thread pool); with this enabled, the prologue the transpiler injects into every
// such method registers the calling thread on first entry. Set-once before the
// host makes off-main-thread calls, so a plain int suffices; defined
// unconditionally so the setter links under DN2CPP_NO_GC too (where the prologue
// is inert — the calloc fallback needs no thread registration).
static int g_native_callback_gc_registration = 0;

void dn2cpp_set_native_callback_gc_registration(int on)
{
    g_native_callback_gc_registration = on ? 1 : 0;
}

void dn2cpp_native_callback_prologue()
{
    // Thread registration only matters in a threaded collector build. The wasm
    // arm compiles without GC_THREADS (single-threaded, and bdwgc's EMSCRIPTEN
    // arm #errors under it): no foreign host thread can exist, so the prologue is
    // correctly inert — same empty body as the calloc fallback.
#if defined(DN2CPP_USE_BOEHM_GC) && defined(GC_THREADS)
    if (g_native_callback_gc_registration == 0)
        return;
    // Register a foreign host thread with the collector on first entry, and — the
    // point of the RAII latch below — unregister it when the thread exits.
    //
    // Unregistering is mandatory, not tidiness: on Linux bdwgc's pthread_stop_world
    // signals every entry of its own thread table, so a short-lived foreign thread that
    // never unregistered leaves a stale entry and every later collection signals a dead
    // TID until the collector aborts ("Signals delivery fails constantly"); pthread_t
    // reuse defeats the ESRCH check. macOS is immune only because darwin_stop_world
    // enumerates live threads from the kernel.
    //
    // So mirror Dn2CppGCThread: a thread_local guard whose destructor drops the
    // thread-static block and unregisters at thread exit. We latch only when *we*
    // newly registered the thread, so a thread the collector already owns (main, or a
    // runtime-spawned one) is left to its real owner and never double-unregistered.
    //
    // The guard's destructor runs while the library is still mapped: the gate's
    // driver join()s its foreign threads and quiesces the worker pool before
    // dlclose, so no thread outlives the code its TLS destructor calls.
    //
    // Registration requires GC_INIT; if a callback fires before runtime init, fall
    // through without latching so a later call retries after init.
    struct NativeCallbackGuard
    {
        bool resolved = false;
        bool ownsRegistration = false;
        ~NativeCallbackGuard()
        {
            if (ownsRegistration)
            {
                // Drop the thread-static block first, while still registered —
                // same ordering as Dn2CppGCThread's destructor.
                dn2cpp_threadstatic_release();
                GC_unregister_my_thread();
            }
        }
    };
    thread_local NativeCallbackGuard t_guard;
    if (t_guard.resolved)
        return;
    if (!GC_is_init_called())
        return;
    if (!GC_thread_is_registered())
    {
        struct GC_stack_base sb;
        if (GC_get_stack_base(&sb) == GC_SUCCESS
            && GC_register_my_thread(&sb) == GC_SUCCESS)
            t_guard.ownsRegistration = true; // we registered it => we unregister it
    }
    t_guard.resolved = true;
#endif
}

// Ensure the calling thread is registered with the collector before any GC
// activity (allocation, collection, stack scan) — the shared hook behind the
// Godot lanes' entry bridges, which the engine calls from threads it spawned
// itself (threaded resource loading, WorkerThreadPool, editor import) that
// Boehm knows nothing about; an unregistered thread's stack is never scanned
// and a collection triggered from it aborts. Engine threads are persistent, so
// the right shape is register-once-and-leave (never unregister) — unlike
// dn2cpp_native_callback_prologue's RAII guard for possibly short-lived foreign
// host threads, and unlike Dn2CppGCThread's scoped guard for threads the
// runtime spawns. The thread_local cache keeps the per-call hot path to a
// single branch. Registration requires GC_INIT (run by dn2cpp_runtime_init at
// module/extension init); if a call ever fires earlier, fall through without
// caching so a later call retries after init.
void dn2cpp_gc_ensure_thread_registered()
{
    // Threaded collector build only: the register/probe entry points are
    // declared by gc.h just under GC_THREADS. The wasm arm compiles without it
    // (single-threaded; bdwgc's EMSCRIPTEN arm #errors under GC_THREADS), so
    // there is no foreign engine thread to register and the hook is correctly
    // inert.
#if defined(DN2CPP_USE_BOEHM_GC) && defined(GC_THREADS)
    thread_local bool t_registered = false;
    if (t_registered)
        return;
    if (!GC_is_init_called())
        return;
    if (!GC_thread_is_registered())
    {
        struct GC_stack_base sb;
        if (GC_get_stack_base(&sb) == GC_SUCCESS)
            GC_register_my_thread(&sb); // GC_DUPLICATE is fine — already known
    }
    t_registered = true;
#endif
}

#if defined(DN2CPP_USE_BOEHM_GC) && defined(__APPLE__)
// Register the writable Mach-O segments of the image holding the runtime (and
// with it the transpiler's generated statics — plain C++ globals in __DATA) as
// the collector's only static roots. On Apple targets the collector's default
// dynamic-library scanning registers every loaded image's data segments via
// dyld callbacks; a windowed engine process loads hundreds of system
// frameworks, which both overflows the root-set table (bdwgc's MAX_ROOT_SETS
// abort: "Too many root sets") and makes every full mark scan framework data
// that can never hold managed pointers. Managed references live only in this
// image's globals, registered thread stacks, and the GC heap itself — so, like
// Mono, dynamic-library scanning is disabled (GC_set_no_dls before GC_INIT)
// and this image's __DATA (data/bss/common) is registered explicitly.
static void dn2cpp_gc_register_host_image_roots()
{
    Dl_info info;
    if (dladdr(reinterpret_cast<void*>(&dn2cpp_runtime_init), &info) == 0
        || info.dli_fbase == nullptr)
        return;
    const auto* hdr = static_cast<const struct mach_header_64*>(info.dli_fbase);
    for (const char* seg : { "__DATA", "__DATA_DIRTY" })
    {
        unsigned long size = 0;
        uint8_t* data = getsegmentdata(hdr, seg, &size);
        if (data != nullptr && size != 0)
            GC_add_roots(data, data + size);
    }
}
#endif

void dn2cpp_runtime_init()
{
    // Hand the generated metadata to the type-info handles the runtime owns, before
    // anything can allocate or throw one of them. Each handle ships as a stub (name +
    // base); the program being run is what knows the type's vtable, its instance size
    // and its reflection tables, and it says so here. Whole-struct copy, not a field
    // merge: `meta` describes the same managed type, so every member of it — including
    // the Type object the emitted metadata interned — is the more complete answer.
    for (int32_t i = 0; i < dn2cpp_type_bind_count; i++)
        *dn2cpp_type_binds[i].target = *dn2cpp_type_binds[i].meta;
#ifdef _WIN32
    // Windows CRT stdio defaults to text mode, translating every outgoing '\n' —
    // including newlines INSIDE a written string — to "\r\n". Real .NET writes a
    // string's bytes verbatim and appends Environment.NewLine only for WriteLine, so
    // put the streams in binary mode and let the console writers emit the platform
    // newline explicitly. No-op on POSIX.
    //
    // The only reference to `stdout` outside the PAL, and not a write: it configures
    // the stream the Windows PAL's console sink happens to use. A target whose console
    // is not stdio makes this inert rather than wrong.
    _setmode(_fileno(stdout), _O_BINARY);
    _setmode(_fileno(stderr), _O_BINARY);
#endif
#ifdef DN2CPP_USE_BOEHM_GC
#ifdef __APPLE__
    // Self-roots mode (default from the host via dn2cpp_gc_set_self_roots_default,
    // overridable via DN2CPP_GC_SELF_ROOTS=0/1). Decided before GC_INIT: init
    // installs the dyld image callbacks that would otherwise register (and keep
    // registering, at every later dlopen) each loaded image's data segments —
    // see dn2cpp_gc_register_host_image_roots.
    bool selfRoots = dn2cpp_env_bool("DN2CPP_GC_SELF_ROOTS", g_gc_self_roots_default != 0);
    if (selfRoots)
        GC_set_no_dls(1);
#endif
    GC_INIT();
#ifdef __APPLE__
    if (selfRoots)
        dn2cpp_gc_register_host_image_roots();
#endif
#ifdef __EMSCRIPTEN__
    // bdwgc's EMSCRIPTEN arm reports an empty static-data root range
    // (gcconfig.h: DATASTART == DATAEND), so no data-segment global — neither a
    // GC-ref static field nor one of the runtime's own root heads — is scanned
    // unless registered by hand. Every such global carries DN2CPP_GC_STATIC_ROOT,
    // which places it in the dn2cpp_roots data section; the linker synthesizes
    // __start_/__stop_ symbols around that section (for both the main module and
    // a SIDE_MODULE=2 link), and this registers the whole span in one call.
    // Strong references on purpose: this TU itself defines annotated globals, so
    // the section always exists — and were it ever to vanish we want a loud link
    // error, not a data-segment root the collector silently stops scanning.
    {
        extern char __start_dn2cpp_roots;
        extern char __stop_dn2cpp_roots;
        GC_add_roots(&__start_dn2cpp_roots, &__stop_dn2cpp_roots);
    }
#endif
    // Permit threads we spawn ourselves (std::thread, which calls the real
    // pthread_create — not GC's pthread redirect) to register with the collector.
    // Only declared by gc.h in a threaded build; the wasm arm compiles without
    // GC_THREADS (single-threaded, and bdwgc's EMSCRIPTEN arm #errors under it).
#ifdef GC_THREADS
    GC_allow_register_threads();
#endif
    // The real CoreLib stores weak-handle values *tagged*: WeakReference ORs
    // the trackResurrection bit (|1) and the COM-aware bit (|2) into the
    // Dn2CppWeakCell address it keeps in the heap field _taggedHandle. This
    // build does not enable ALL_INTERIOR_POINTERS, so a heap-scanned word
    // pointing at cell+1..3 fails Boehm's valid-offset check and would NOT
    // keep the cell alive — a long WeakReference held only through heap
    // fields would have its cell reclaimed and later reads would touch freed
    // memory. (Stack words are exempt: the stack scan falls back to GC_base.)
    // Registering the three tag displacements makes the tagged heap word a
    // recognized reference to the cell.
    GC_REGISTER_DISPLACEMENT(1);
    GC_REGISTER_DISPLACEMENT(2);
    GC_REGISTER_DISPLACEMENT(3);

    // GC collection mode. The default comes from g_gc_incremental_default (console
    // = stop-the-world, Godot = incremental); the env vars below override it at
    // startup, so the same binary can switch modes without a rebuild:
    //   DN2CPP_GC_INCREMENTAL=0/1  force incremental off / on (overrides the default)
    //   DN2CPP_GC_TIME_LIMIT_MS    per-increment pause target in ms (default 5)
    //   DN2CPP_GC_STATS            report the active mode now + a pause summary at exit
    bool incremental = dn2cpp_env_bool("DN2CPP_GC_INCREMENTAL", g_gc_incremental_default != 0);
#ifdef __EMSCRIPTEN__
    // wasm has no mprotect/signal VDB, so bdwgc would fall back to DEFAULT_VDB
    // (every page reported always-dirty) — the incremental machinery then costs
    // its bookkeeping and buys nothing. Force stop-the-world regardless of env or
    // default: the Godot-lane generated init sets the incremental default blindly
    // for every target, and this is the one where the mode cannot pay off.
    incremental = false;
#endif
    unsigned long ms = 5;
    if (incremental)
    {
        const char* lim = dn2cpp_pal_getenv("DN2CPP_GC_TIME_LIMIT_MS");
        if (lim != nullptr)
        {
            long v = std::strtol(lim, nullptr, 10);
            if (v > 0)
                ms = static_cast<unsigned long>(v);
        }
        GC_set_time_limit(ms);
        GC_enable_incremental();
    }
    if (dn2cpp_pal_getenv("DN2CPP_GC_STATS") != nullptr)
    {
        // Print the active mode immediately (not via atexit), so it is observable
        // even when the host does not exit cleanly (e.g. the Godot engine).
        unsigned gcVersion = GC_get_version();
        std::fprintf(stderr, "[dn2cpp] GC version: %d.%d.%d\n",
                     static_cast<int>((gcVersion >> 16) & 0xffu),
                     static_cast<int>((gcVersion >> 8) & 0xffu),
                     static_cast<int>(gcVersion & 0xffu));
        if (incremental)
            std::fprintf(stderr, "[dn2cpp] GC mode: incremental (time-limit %lu ms)\n", ms);
        else
            std::fprintf(stderr, "[dn2cpp] GC mode: stop-the-world\n");
        // How much of the heap the incremental collector WOULD write-protect to
        // recover dirty bits. It describes the platform's dirty-bit strategy, not the
        // mode in force, and reads the same under stop-the-world. Both selectable
        // backends run MANUAL_VDB and never actually protect a page, but they answer
        // this query differently: the fork's gcconfig.h forces MANUAL_VDB into the
        // same branch that undefines every mprotect-style VDB, so it always reads 0.
        // Upstream is only told via -DMANUAL_VDB, which leaves those VDBs compiled in
        // and skips just the runtime probe (GC_dirty_init) that would zero this out,
        // so it falls back to a page-size heuristic and reads nonzero — 1 where a
        // heap block is exactly one page, 3 where it is not (16 KB-page arm64 macOS
        // protects the pointer-free heap too) — despite protecting nothing.
        //
        // Whether a kernel write into a managed buffer actually bounces is this value
        // AND incremental mode — report that conjunction rather than leave a reader to
        // combine the two.
        int needs = GC_incremental_protection_needs();
        std::fprintf(stderr,
                     "[dn2cpp] GC protects: %d (0=none, 1=pointerful heap, 3=+pointer-free)"
                     "; kernel-write bounce: %s\n",
                     needs, (incremental && needs != GC_PROTECTS_NONE) ? "on" : "off");
        GC_set_on_collection_event(dn2cpp_gc_event);
        std::atexit(dn2cpp_gc_stats_dump_once);
    }
    //   DN2CPP_GC_SUPPRESS_STATS   suppressed-finalizer set work summary at exit
    if (dn2cpp_pal_getenv("DN2CPP_GC_SUPPRESS_STATS") != nullptr)
        std::atexit(dn2cpp_gc_suppress_stats_dump_once);
#endif
    // No curl_global_init here, though this is the ordering curl's documentation asks
    // for: a hard reference from a TU EVERY program links makes every program link
    // libcurl, Mbed TLS and the embedded CA bundle, which no dead-strip can undo. It
    // runs lazily under std::call_once on the first request instead — the trade is
    // written out at EnsureGlobalInit in intrinsics/dn2cpp_http2_stream.cpp.
    //
    // Pre-claim the main thread's Thread object as managed id 1 (init always
    // runs on the main thread, before any pool worker or host thread exists),
    // so the lazy path in dn2cpp_thread_current can hand every OTHER
    // non-trampoline thread a fresh id instead of assuming it is main.
    dn2cpp_thread_materialize_main();
}

// The single exit path of a generated console `main` (and of Environment.Exit).
//
// The background threads this runtime spawns — the finalizer thread and the
// Task.Run pool workers — are detached and have no stop mechanism, by design
// ("process-lifetime pool"). Returning from `main` would run `exit()`, which
// destroys the namespace-scope mutexes those threads lock; a thread that locks
// one after its destructor ran gets EINVAL from pthread_mutex_lock, which
// libc++ turns into an uncaught std::system_error and the process aborts. So
// terminate without unwinding any of it: nothing is destroyed, so nothing can
// be locked after destruction. Real .NET exits the same way — background
// threads are not joined and static state is not torn down.
//
// Only the C stdio buffers must be committed by hand, since _Exit does not.
[[noreturn]] void dn2cpp_environment_exit(int32_t code)
{
#ifdef DN2CPP_USE_BOEHM_GC
    // _Exit skips the atexit chain this is normally registered on.
    if (dn2cpp_pal_getenv("DN2CPP_GC_STATS") != nullptr)
        dn2cpp_gc_stats_dump_once();
    if (dn2cpp_pal_getenv("DN2CPP_GC_SUPPRESS_STATS") != nullptr)
        dn2cpp_gc_suppress_stats_dump_once();
#endif
    dn2cpp_pal_console_flush();
#ifdef DN2CPP_EXIT_VIA_STDEXIT
    // Sanitizer builds: LeakSanitizer reports from an atexit handler, which
    // _Exit would skip. Such a build reintroduces the teardown race by design.
    std::exit(code);
#else
    std::_Exit(code);
#endif
}

// Per-thread GC-visible storage for managed thread-static fields whose type holds
// GC references. Raw C++ thread_local (TLV) storage is not scanned by the collector
// on every platform (Darwin TLV blocks are malloc-backed), so an object whose only
// reference lives in such a field could be collected while still in use. Generated
// code funnels those fields through this block instead: uncollectable => scanned as
// a GC root, zero-filled => a fresh thread sees default(T), which matches .NET
// ([ThreadStatic] initializers run only on the initializing thread).
static thread_local void* t_threadstatic_block = nullptr;

void* dn2cpp_threadstatic_block(int32_t size)
{
    if (t_threadstatic_block == nullptr)
        t_threadstatic_block = dn2cpp_alloc_pinned((size_t)size);
    return t_threadstatic_block;
}

// Thread exit: .NET drops a dead thread's thread-static values, so free the
// (uncollectable, hence never collected) block or every dead thread would leak it
// and everything it references. Called from the GC-thread guard destructor, after
// which no managed code runs on the thread. Threads the runtime did not spawn
// never pass through a guard; their blocks are deliberately leaked (such engine/
// host threads normally live for the process anyway).
void dn2cpp_threadstatic_release()
{
    if (t_threadstatic_block != nullptr)
    {
        dn2cpp_free_pinned(t_threadstatic_block);
        t_threadstatic_block = nullptr;
    }
}

// Per-thread SynchronizationContext.Current. The installed context is a
// managed object whose only reference may be this slot, so the slot lives in
// GC-visible pinned storage — the t_threadstatic_block rationale above: raw C++
// thread_local is not scanned on every platform. The thread_local here only
// caches the slot's address. Reads before any install stay allocation-free.
static thread_local Dn2CppObject** t_sync_ctx_slot = nullptr;

Dn2CppObject* dn2cpp_sync_ctx_get()
{
    return t_sync_ctx_slot != nullptr ? *t_sync_ctx_slot : nullptr;
}

void dn2cpp_sync_ctx_set(Dn2CppObject* ctx)
{
    if (t_sync_ctx_slot == nullptr)
    {
        if (ctx == nullptr)
            return; // clearing an never-installed slot: nothing to store
        t_sync_ctx_slot = static_cast<Dn2CppObject**>(dn2cpp_alloc_pinned(sizeof(Dn2CppObject*)));
    }
    dn2cpp_gc_store_ref(t_sync_ctx_slot, ctx);
}

// Thread exit: drop the slot with the thread, like the thread-static block —
// .NET's Thread._synchronizationContext dies with its thread too.
void dn2cpp_sync_ctx_release()
{
    if (t_sync_ctx_slot != nullptr)
    {
        dn2cpp_free_pinned(t_sync_ctx_slot);
        t_sync_ctx_slot = nullptr;
    }
}

// ===== static-constructor first-use guards (slow path) ========================
// The generated inline X__ensure guards fast-path on an acquire load of the
// per-cctor atomic flag; the first use of each type lands here. One global
// mutex + condvar serialize the bookkeeping only — the cctor body itself runs
// OUTSIDE the lock, so independent types initialize in parallel and a cctor
// that first-uses another type re-enters this function cleanly. The in-flight
// list nodes live on the initializing threads' stacks (linked and unlinked
// under the mutex), so there is nothing to allocate or collect.
struct Dn2CppCctorRun
{
    std::atomic<int8_t>* flag;
    std::thread::id owner;
    Dn2CppCctorRun* next;
};
static std::mutex& g_cctor_mtx = dn2cpp_never_destroyed<std::mutex>();
static std::condition_variable& g_cctor_cv = dn2cpp_never_destroyed<std::condition_variable>();
static Dn2CppCctorRun* g_cctor_running = nullptr; // guarded by g_cctor_mtx

// INVARIANT: a cctor that ends in an exception leaves its type PERMANENTLY
// UNINITIALIZED, as on .NET. Latching the done flag on a throwing body would declare
// a half-initialized type ready, so a caller that catches the failure and touches the
// type again sails through the fast path onto statics that were never assigned — a
// SIGSEGV frames away from anything mentioning a static constructor.
//
// A failure is REMEMBERED instead: the flag stays 0, the thrown managed object is
// recorded against the flag's address, and every later __ensure re-raises that same
// object. .NET rethrows a cached TypeInitializationException wrapping it; dn2cpp does
// not model that type, so it re-raises the original — a caller that unwraps
// InnerException reads the same exception on both runtimes.
//
// The record is consulted only on the slow path and a SUCCEEDING cctor still latches
// to 1, so the inline fast path emitted ahead of every static access is untouched.
struct Dn2CppCctorFailure
{
    std::atomic<int8_t>* flag; // identity of the type whose initializer failed
    Dn2CppObject* exc;         // the managed exception it threw (null: a non-managed failure)
    Dn2CppCctorFailure* next;
};
// static => a GC root, so the recorded exception graph outlives the throw (the
// DN2CPP_GC_STATIC_ROOT / dn2cpp_alloc'd-node discipline of the in-flight list).
static DN2CPP_GC_STATIC_ROOT Dn2CppCctorFailure* g_cctor_failed = nullptr; // guarded by g_cctor_mtx

// Caller holds g_cctor_mtx.
static const Dn2CppCctorFailure* dn2cpp_cctor_find_failure(const std::atomic<int8_t>* flag)
{
    for (const Dn2CppCctorFailure* f = g_cctor_failed; f != nullptr; f = f->next)
    {
        if (f->flag == flag)
            return f;
    }
    return nullptr;
}

// Re-raise a remembered type-initialization failure. dn2cpp_rethrow preserves the
// already-captured trace (it stamps only a never-thrown object), so every touch
// reports the site the initializer actually failed at, exactly as .NET's cached
// TypeInitializationException does.
[[noreturn]] static void dn2cpp_cctor_replay_failure(Dn2CppObject* exc)
{
    if (exc != nullptr)
        dn2cpp_rethrow(exc);
    // A non-managed failure (a C++ exception with no managed object) cannot be
    // replayed as a managed throw, and answering with a *different* exception
    // would be a lie about what went wrong. Fail loudly instead — the type is
    // unusable and there is nothing catchable to hand back.
    dn2cpp_fail("static constructor failed with a non-managed exception; the type is unusable");
}

static void dn2cpp_cctor_unlink(Dn2CppCctorRun* node, Dn2CppCctorFailure* failure)
{
    std::lock_guard<std::mutex> lk(g_cctor_mtx);
    for (Dn2CppCctorRun** pp = &g_cctor_running; *pp != nullptr; pp = &(*pp)->next)
    {
        if (*pp == node)
        {
            *pp = node->next;
            break;
        }
    }
    if (failure != nullptr)
    {
        // Failure: record, and leave the flag at 0 so no fast path ever reads this
        // type as initialized. A thread parked in the wait below re-tests the record.
        failure->next = g_cctor_failed;
        g_cctor_failed = failure;
    }
    else
    {
        node->flag->store(1, std::memory_order_release);
    }
    g_cctor_cv.notify_all();
}

void dn2cpp_cctor_run_once(std::atomic<int8_t>* flag, void (*body)())
{
    std::unique_lock<std::mutex> lk(g_cctor_mtx);
    for (;;)
    {
        if (flag->load(std::memory_order_acquire))
            return; // completed while we contended for the lock / waited
        if (const Dn2CppCctorFailure* f = dn2cpp_cctor_find_failure(flag))
        {
            // Initialization already failed: re-raise, never re-run. Outside the
            // lock — the replay allocates (the in-flight root node) and runs
            // arbitrary unwinding from here.
            Dn2CppObject* exc = f->exc;
            lk.unlock();
            dn2cpp_cctor_replay_failure(exc);
        }
        Dn2CppCctorRun* r = g_cctor_running;
        while (r != nullptr && r->flag != flag)
            r = r->next;
        if (r == nullptr)
            break; // no one is initializing this type — it is ours to run
        if (r->owner == std::this_thread::get_id())
            return; // re-entrant circular init: proceed on the partial state
        g_cctor_cv.wait(lk); // another thread's init is in flight — block for it
    }
    Dn2CppCctorRun node{ flag, std::this_thread::get_id(), g_cctor_running };
    g_cctor_running = &node;
    lk.unlock();
    // The failure record is allocated in the handler — ordinary execution context,
    // the exception already caught — never while unwinding, the same safety class as
    // the in-flight node push. Re-entrancy is unaffected: the record is published only
    // after the run node is unlinked, so a nested __ensure from this very cctor still
    // meets the owner test above and sees the partial state.
    try
    {
        if (body != nullptr)
            body();
    }
    catch (const Dn2CppException& e)
    {
        auto* f = static_cast<Dn2CppCctorFailure*>(dn2cpp_alloc(sizeof(Dn2CppCctorFailure)));
        f->flag = flag;
        f->exc = e.obj;
        dn2cpp_cctor_unlink(&node, f);
        throw;
    }
    catch (...)
    {
        auto* f = static_cast<Dn2CppCctorFailure*>(dn2cpp_alloc(sizeof(Dn2CppCctorFailure)));
        f->flag = flag;
        f->exc = nullptr;
        dn2cpp_cctor_unlink(&node, f);
        throw;
    }
    dn2cpp_cctor_unlink(&node, nullptr);
}

void dn2cpp_cctor_run_startup(void (*ensure)(), const char* type)
{
    // The eager startup pass is dn2cpp's stand-in for .NET's lazy first-use
    // initialization, so it runs initializers .NET might never run at all. A failure
    // in it must therefore not be the process's problem: letting the exception leave
    // `main`'s init prologue (which sits ahead of the entry point's own handler)
    // terminates via std::terminate — killing a program whose .NET twin runs fine
    // because it never touches the type. Swallow it here; dn2cpp_cctor_run_once has
    // recorded the failure and the first real USE of the type re-raises it, which is
    // when .NET would have raised it too.
    //
    // Swallowed, but never silently: the failure is reported through the
    // shared host-boundary reporter, naming the type — the GDExtension lane's sink
    // (installed before dn2cpp_godot_init_managed runs this pass) delivers it to the
    // engine's print_error, and a console binary's sinkless fallback puts it on
    // stderr. Without the report, a type whose statics are only ever read through
    // guarded use sites that some caller catches — or whose failure surfaces three
    // frames away as "that feature does nothing" — has no first cause anywhere.
    //
    // The --dotnet-module lane exempts one class of failure from the report; that
    // exemption does not carry over, and no cctor the core lanes run qualifies for
    // one. A class may only be exempted if its failure is structural (fires on every
    // run of a correct program), is an artefact of running cctors eagerly rather than
    // a fact about the program, and still reaches the developer from the first-use
    // re-raise.
    //
    // The reporter pops the in-flight root — the discipline every runtime site that
    // swallows a managed exception follows.
    try
    {
        ensure();
    }
    catch (const Dn2CppException& e)
    {
        dn2cpp_report_boundary_exception(
            e.obj, "the startup static constructor of %s",
            type != nullptr ? type : "<unknown>");
    }
}

void* dn2cpp_alloc(size_t size)
{
#ifdef DN2CPP_USE_BOEHM_GC
    // Conservative GC heap (zero-initialized by GC_MALLOC).
    void* p = GC_MALLOC(size);
#else
    // Fallback allocator: zero-filled, never collected.
    void* p = std::calloc(1, size);
#endif
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

void* dn2cpp_alloc_atomic(size_t size)
{
#ifdef DN2CPP_USE_BOEHM_GC
    // Pointer-free GC block: collected/reachable like GC_MALLOC's, but never
    // scanned. NOT zero-filled (the atomic allocator does not clear) — callers
    // that rely on zeroed memory must memset.
    void* p = GC_MALLOC_ATOMIC(size);
#else
    // Fallback allocator: leave it uninitialized too, so the two builds agree on
    // the (no-zeroing) contract.
    void* p = std::malloc(size);
#endif
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

void dn2cpp_gc_write_barrier(void* heapAddress)
{
#ifdef DN2CPP_USE_BOEHM_GC
    if (heapAddress != nullptr)
        GC_end_stubborn_change(heapAddress);
#else
    (void)heapAddress;
#endif
}

void dn2cpp_gc_write_barrier_if_heap(void* address)
{
#ifdef DN2CPP_USE_BOEHM_GC
    if (address == nullptr)
        return;
    void* base = GC_base(address);
    if (base != nullptr)
        GC_end_stubborn_change(base);
#else
    (void)address;
#endif
}

void dn2cpp_gc_memmove_refs(void* destination, const void* source, size_t bytes)
{
    if (bytes == 0)
        return;
    std::memmove(destination, source, bytes);
    dn2cpp_gc_write_barrier_if_heap(destination);
}

int dn2cpp_gc_kernel_write_unsafe(const void* p)
{
#ifdef DN2CPP_USE_BOEHM_GC
    if (p == nullptr)
        return 0;
    // Stop-the-world never protects anything, so every console binary answers
    // here and pays nothing.
    if (!GC_is_incremental_mode())
        return 0;
    // Under MANUAL_VDB nothing is ever mprotected, but only the fork's build
    // reports that as GC_PROTECTS_NONE; upstream's plain -DMANUAL_VDB build
    // reports a nonzero page-size heuristic instead (see dn2cpp_core.h), so
    // this check short-circuits on the fork only — upstream falls through to
    // the heap-pointer check below and answers 1, conservatively.
    if (GC_incremental_protection_needs() == GC_PROTECTS_NONE)
        return 0;
    // Outside the heap — the C stack (where a `localloc`/`stackalloc` buffer
    // lands), malloc'd PAL memory, the data segment — nothing is protected.
    return GC_is_heap_ptr(p) ? 1 : 0;
#else
    (void)p; // calloc fallback: no collector, no protection.
    return 0;
#endif
}

void* dn2cpp_alloc_pinned(size_t size)
{
#ifdef DN2CPP_USE_BOEHM_GC
    void* p = GC_MALLOC_UNCOLLECTABLE(size);
    if (p != nullptr)
        std::memset(p, 0, size);
#else
    void* p = std::calloc(1, size);
#endif
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

void dn2cpp_free_pinned(void* p)
{
#ifdef DN2CPP_USE_BOEHM_GC
    GC_FREE(p);
#else
    std::free(p);
#endif
}

// ── raw native (GC-unmanaged) heap ──────────────────────────────────────────
// Thin wrappers over the C allocator. Unlike dn2cpp_alloc, these never touch the
// GC heap (no GC_MALLOC even under DN2CPP_USE_BOEHM_GC), so the block is neither
// scanned nor collected — exactly like .NET's NativeMemory / Marshal.AllocHGlobal
// unmanaged heap. Allocation failure raises OutOfMemoryException, matching .NET.
void* dn2cpp_native_alloc(size_t byteCount)
{
    void* p = std::malloc(byteCount);
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

// Alloc(elementCount, elementSize): an overflow-checked product, allocated
// uninitialized (NativeMemory.Alloc's two-arg form is malloc, not calloc).
void* dn2cpp_native_alloc_checked(size_t elementCount, size_t elementSize)
{
    // The overflow arms below throw and the allocation arm aborts, and the split is
    // the point: the product not fitting is decided before the allocator is asked
    // anything, so an exception object can still be allocated (real .NET agrees —
    // NativeMemory.Alloc(nuint.MaxValue, 4) is a catchable OutOfMemoryException).
    // Once malloc has said no, allocating that object is the one thing that cannot
    // be done, so that arm stays an abort.
    size_t total;
#if defined(__GNUC__) || defined(__clang__)
    if (__builtin_mul_overflow(elementCount, elementSize, &total))
        dn2cpp_throw_out_of_memory();
#else
    // MSVC has no __builtin_mul_overflow; do the overflow check by hand (dn2cpp
    // targets 64-bit only, so size_t is always wide enough for this division).
    total = elementCount * elementSize;
    if (elementCount != 0 && total / elementCount != elementSize)
        dn2cpp_throw_out_of_memory();
#endif
    void* p = std::malloc(total);
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

// AllocZeroed: calloc zero-fills and does its own overflow-checked product.
void* dn2cpp_native_calloc(size_t elementCount, size_t elementSize)
{
    void* p = std::calloc(elementCount, elementSize);
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

void* dn2cpp_native_realloc(void* ptr, size_t byteCount)
{
    void* p = std::realloc(ptr, byteCount);
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

void dn2cpp_native_free(void* ptr)
{
    // free(nullptr) is a no-op, matching NativeMemory.Free(null) /
    // Marshal.FreeHGlobal(IntPtr.Zero).
    std::free(ptr);
}

// ── aligned native (GC-unmanaged) heap ──────────────────────────────────────
// Round `byteCount` up to a multiple of `alignment` (a power of two), as
// std::aligned_alloc requires the size to be a multiple of the alignment.
static size_t dn2cpp_align_up(size_t byteCount, size_t alignment)
{
    return (byteCount + (alignment - 1)) & ~(alignment - 1);
}

// NativeMemory.AlignedAlloc(byteCount, alignment): alignment must be a non-zero
// power of two (else ArgumentException, matching .NET). A zero byteCount still
// returns a valid, distinct, aligned pointer (one alignment unit). Allocation
// failure raises OutOfMemoryException.
//
// The alignment check is the ONE argument validation in this file, and therefore the
// one failure here that is a managed throw rather than an abort. Every other abort in
// this TU reports an allocation that failed, where a managed throw is not available at
// all — minting the exception object needs the allocator that just said no. Here
// nothing has been asked of the allocator yet, the fault is a caller's argument, and
// real .NET answers it with a catchable ArgumentException (measured on CoreCLR:
// alignment 0, 3 and 24 all give System.ArgumentException with no ParamName;
// alignment 1 and byteCount 0 are both accepted). A pool that sizes its
// alignment from configuration must be able to reject one bad row and carry on.
void* dn2cpp_native_aligned_alloc(size_t byteCount, size_t alignment)
{
    if (alignment == 0 || (alignment & (alignment - 1)) != 0)
        dn2cpp_throw_argument();
    // Every POSIX aligned allocator refuses an alignment below sizeof(void*) —
    // posix_memalign says so in its contract (EINVAL), and std::aligned_alloc
    // inherits it in practice on both the macOS and the glibc implementations,
    // so AlignedAlloc(n, 1) / (n, 2) / (n, 4) came back null and reported
    // "OutOfMemoryException" for a request .NET accepts and satisfies. Clamping
    // is sound rather than a papering-over: both values are powers of two, so a
    // block aligned to the larger one is aligned to the smaller one too, and the
    // caller asked for a MINIMUM. The Android arm has always clamped for exactly
    // this reason; the clamp is hoisted here so the generic arm cannot be the one
    // that forgot.
    size_t effAlign = alignment < sizeof(void*) ? sizeof(void*) : alignment;
    // Round to the EFFECTIVE alignment: C11 aligned_alloc wants a size that is a
    // multiple of the alignment it is actually handed, so rounding to the
    // caller's smaller value would hand it a size it may reject.
    size_t rounded = dn2cpp_align_up(byteCount, effAlign);
    if (rounded == 0)
        rounded = effAlign;  // AlignedAlloc(0, a) returns a valid pointer in .NET
    // MSVC's CRT has no std::aligned_alloc; _aligned_malloc is its native
    // equivalent (note the reversed (size, alignment) argument order), paired
    // with _aligned_free/_aligned_realloc below rather than plain free/realloc
    // (mixing the two allocator families corrupts the heap on Windows).
#if defined(_MSC_VER)
    void* p = _aligned_malloc(rounded, effAlign);
#elif defined(__ANDROID__)
    // Bionic's own aligned_alloc requires API 28+; posix_memalign is available
    // from a much lower API and is freed with plain free() just like
    // aligned_alloc.
    void* p = nullptr;
    if (::posix_memalign(&p, effAlign, rounded) != 0)
        p = nullptr;
#else
    void* p = std::aligned_alloc(effAlign, rounded);
#endif
    if (p == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return p;
}

// NativeMemory.AlignedRealloc: there is no portable aligned realloc, so allocate
// a fresh aligned block, copy the lesser of the old and new usable sizes (the old
// size comes from the platform's allocation-size query — copying within both
// blocks is always in-bounds), then free the old block. A null `ptr` degenerates
// to a plain AlignedAlloc, matching realloc / .NET.
void* dn2cpp_native_aligned_realloc(void* ptr, size_t byteCount, size_t alignment)
{
#ifdef _MSC_VER
    // _aligned_realloc does its own copy-and-free (and behaves like
    // _aligned_malloc when ptr is null), so no manual copy is needed here —
    // unlike the portable arm below, which has no such primitive to call.
    // Same validation, same answer as the POSIX arm — which reaches it by
    // delegating to dn2cpp_native_aligned_alloc below, so the two arms agree on
    // a bad alignment without this line having a POSIX twin to drift from.
    if (alignment == 0 || (alignment & (alignment - 1)) != 0)
        dn2cpp_throw_argument();
    // The same sizeof(void*) clamp dn2cpp_native_aligned_alloc applies, and here
    // it is a correctness requirement rather than a portability one: MSVC's
    // _aligned_realloc must be handed the alignment the block was originally
    // allocated with, and that block came from _aligned_malloc(effAlign).
    size_t effAlign = alignment < sizeof(void*) ? sizeof(void*) : alignment;
    size_t rounded = dn2cpp_align_up(byteCount, effAlign);
    if (rounded == 0)
        rounded = effAlign;
    void* fresh = _aligned_realloc(ptr, rounded, effAlign);
    if (fresh == nullptr)
        dn2cpp_fail("OutOfMemoryException");
    return fresh;
#else
    void* fresh = dn2cpp_native_aligned_alloc(byteCount, alignment);
    if (ptr != nullptr)
    {
        size_t oldSize = dn2cpp_pal_malloc_usable_size(ptr);
        size_t newRounded = dn2cpp_align_up(byteCount, alignment);
        if (newRounded == 0)
            newRounded = alignment;
        std::memcpy(fresh, ptr, oldSize < newRounded ? oldSize : newRounded);
        std::free(ptr);
    }
    return fresh;
#endif
}

void dn2cpp_native_aligned_free(void* ptr)
{
#ifdef _MSC_VER
    _aligned_free(ptr); // _aligned_free(nullptr) is a no-op too
#else
    // macOS/Linux release aligned_alloc memory with plain free; free(nullptr) is
    // a no-op, matching NativeMemory.AlignedFree(null).
    std::free(ptr);
#endif
}

// Finalizer support. A Boehm finalization callback runs from GC context, possibly
// RE-ENTRANTLY from inside another GC_MALLOC call, so a callback that takes a lock or
// allocates can deadlock against itself on one thread. The callback here therefore does
// the strict minimum — a fixed-capacity ring buffer via plain atomics, no lock, no GC
// allocation on the fast path — and a dedicated finalizer thread, the only place a
// managed Finalize() override runs, polls the ring. A full ring spills to an unbounded
// malloc'd overflow list (see the spill comment below). The thread starts lazily on the
// first dn2cpp_register_finalizer call.
#ifdef DN2CPP_USE_BOEHM_GC
namespace
{
// Sized generously: the ring only needs to outrun the finalizer thread's
// drain rate between polls, not hold every finalizable object a program will
// ever create. GC_MALLOC_UNCOLLECTABLE (not GC_MALLOC): it must never move
// and must stay alive for the process's lifetime, but Boehm still needs to
// scan it (it holds live Dn2CppObject* pointers — the ring is the thing that
// keeps a finalizable object reachable between "queued" and "run").
constexpr uint64_t kFinalizerRingCapacity = 1u << 16;
// Each slot is itself an atomic and doubles as its own publish flag: the GC
// callback release-stores the object *into the slot* rather than bumping head
// and then writing a plain pointer. The plain-pointer form had a data race —
// the callback published head (fetch_add) before writing the slot, so the
// finalizer thread could observe the advanced head and read the slot before it
// was written, dispatching Finalize() on a stale (or still-zero) pointer. A
// null slot now means "not yet published"; the finalizer thread waits on it and
// clears it back to null once drained, so a wrapped physical slot can't be
// mistaken for a fresh entry. GC_MALLOC_UNCOLLECTABLE zero-fills, so a freshly
// allocated ring is all-null (all "empty").
static_assert(std::atomic<Dn2CppObject*>::is_always_lock_free,
    "finalizer ring relies on lock-free atomic pointer slots");
static_assert(sizeof(std::atomic<Dn2CppObject*>) == sizeof(Dn2CppObject*),
    "atomic slot must match a raw pointer so zero-filled GC memory reads as all-null");
std::atomic<Dn2CppObject*>* g_finalizer_ring = nullptr;
std::atomic<uint64_t> g_finalizer_head{0}; // count enqueued (slot each producer claims)
std::atomic<uint64_t> g_finalizer_tail{0}; // count drained by the finalizer thread
// True only on the dedicated finalizer thread. WaitForPendingFinalizers called
// from a Finalize() body would otherwise wait on its own not-yet-published tail
// forever; real .NET returns immediately on the finalizer thread (verified on
// net10.0: a finalizer calling GC.WaitForPendingFinalizers completes normally).
thread_local bool t_on_finalizer_thread = false;
// Serializes manual-mode drains across OS threads. The tail counter has a
// single writer per drain pass, but manual mode lets *any* managed thread
// drain (per-frame hook, GC.WaitForPendingFinalizers from a worker): two
// concurrent drains would snapshot the same tail and either run the same
// object's Finalize() twice or spin forever on a slot the other thread
// already cleared. The same-thread reentrancy guard below stays a
// thread_local checked *before* this lock, so a Finalize() body that drains
// again returns instead of self-deadlocking.
std::mutex& g_finalizer_drain_mtx = dn2cpp_never_destroyed<std::mutex>();

// Overflow spill for the ring (real .NET's f-reachable queue is unbounded): one
// collection wave can make far more objects finalizable than the ring holds, and in
// manual-drain mode there may be no consumer until the next frame. Blocking the
// producer would deadlock — manual mode has nobody to wait for, and a Finalize() body
// that allocates is itself a producer — so overflow grows into malloc'd chunks.
//
// The callback runs after GC_invoke_finalizers releases the allocator lock, with the
// world running, so malloc and GC_add_roots are both safe here. GC ALLOCATION is not:
// it re-enters GC_notify_or_invoke_finalizers and can nest another callback on this
// thread, so the spill mutex is never held across one.
//
// Rooting: a pending object's only reference may be the queue, and a malloc'd chunk is
// invisible to the collector — so every chunk is GC_add_roots'd for exactly as long as
// it lives and unregistered right before free(). Entries are nulled as they are handed
// to the finalizer body, so a drained object is not pinned by a stale slot.
constexpr uint32_t kFinalizerSpillChunkEntries = 1u << 13;
struct Dn2CppFinalizerSpillChunk
{
    Dn2CppFinalizerSpillChunk* next;
    uint32_t count;   // filled entries (producer end)
    uint32_t drained; // consumed entries (consumer end)
    Dn2CppObject* objs[kFinalizerSpillChunkEntries];
};
std::mutex& g_finalizer_spill_mtx = dn2cpp_never_destroyed<std::mutex>();
Dn2CppFinalizerSpillChunk* g_finalizer_spill_head = nullptr; // oldest, consumed first
Dn2CppFinalizerSpillChunk* g_finalizer_spill_tail = nullptr; // newest, appended to
// Enqueued/drained counts mirror the ring's head/tail so
// WaitForPendingFinalizers can wait on a "pending as of now" snapshot that
// covers spilled entries too.
std::atomic<uint64_t> g_finalizer_spill_enqueued{0};
std::atomic<uint64_t> g_finalizer_spill_drained{0};

void dn2cpp_finalizer_spill_push(Dn2CppObject* obj)
{
    std::lock_guard<std::mutex> lock(g_finalizer_spill_mtx);
    Dn2CppFinalizerSpillChunk* c = g_finalizer_spill_tail;
    if (c == nullptr || c->count == kFinalizerSpillChunkEntries)
    {
        c = static_cast<Dn2CppFinalizerSpillChunk*>(
            std::malloc(sizeof(Dn2CppFinalizerSpillChunk)));
        if (c == nullptr)
            dn2cpp_fail("out of memory (finalizer overflow chunk)");
        c->next = nullptr;
        c->count = 0;
        c->drained = 0;
        // Root the chunk before anything is stored into it; until then the
        // object is still rooted by this callback frame's stack. Conservative
        // scanning of the header/index words is harmless over-approximation.
        GC_add_roots(c, reinterpret_cast<char*>(c) + sizeof(Dn2CppFinalizerSpillChunk));
        if (g_finalizer_spill_tail != nullptr)
            g_finalizer_spill_tail->next = c;
        else
            g_finalizer_spill_head = c;
        g_finalizer_spill_tail = c;
    }
    c->objs[c->count++] = obj;
    g_finalizer_spill_enqueued.fetch_add(1, std::memory_order_release);
}

// Take the oldest spilled entry, or null if none is pending. Chunks that are
// both full and fully drained are unrooted and freed here; a partially filled
// tail chunk is kept so an active overflow wave does not thrash
// malloc/GC_add_roots at every push/pop interleave.
Dn2CppObject* dn2cpp_finalizer_spill_pop()
{
    std::lock_guard<std::mutex> lock(g_finalizer_spill_mtx);
    for (;;)
    {
        Dn2CppFinalizerSpillChunk* c = g_finalizer_spill_head;
        if (c == nullptr)
            return nullptr;
        if (c->drained < c->count)
        {
            Dn2CppObject* obj = c->objs[c->drained];
            c->objs[c->drained] = nullptr; // stop rooting once handed to the body
            c->drained++;
            return obj;
        }
        if (c->count < kFinalizerSpillChunkEntries)
            return nullptr; // partially filled tail chunk, currently empty — keep it
        g_finalizer_spill_head = c->next;
        if (g_finalizer_spill_tail == c)
            g_finalizer_spill_tail = nullptr;
        GC_remove_roots(c, reinterpret_cast<char*>(c) + sizeof(Dn2CppFinalizerSpillChunk));
        std::free(c);
    }
}

void GC_CALLBACK dn2cpp_finalizer_callback(void* obj, void* /* clientData */)
{
    // Fast path: claim a ring slot only when there is room (a CAS claim, not a
    // blind fetch_add — a full ring must leave head untouched so the overflow
    // entry is not double-counted by the ring's own accounting). The room
    // check can race a concurrent drain advancing tail; the miss direction is
    // a spurious spill, which is correct just slower.
    uint64_t h = g_finalizer_head.load(std::memory_order_relaxed);
    for (;;)
    {
        if (h - g_finalizer_tail.load(std::memory_order_acquire) >= kFinalizerRingCapacity)
        {
            // Ring full: spill to the unbounded overflow list instead of
            // failing — a bulk wave (or a manual-drain frame gap) legitimately
            // exceeds any fixed capacity, and real .NET's f-reachable queue
            // never drops or blocks.
            dn2cpp_finalizer_spill_push(static_cast<Dn2CppObject*>(obj));
            return;
        }
        // Release-store publishes obj: it pairs with the finalizer thread's
        // acquire load of this same slot, ordering the object write before the
        // reader can see the slot turn non-null. The room check above
        // guarantees the previous occupant of this physical slot was already
        // drained and cleared.
        if (g_finalizer_head.compare_exchange_weak(h, h + 1, std::memory_order_relaxed))
        {
            g_finalizer_ring[h % kFinalizerRingCapacity].store(
                static_cast<Dn2CppObject*>(obj), std::memory_order_release);
            // A pinned block is rescanned only when dirty; the release/acquire
            // pairing on the slot is unaffected.
            dn2cpp_gc_write_barrier(&g_finalizer_ring[h % kFinalizerRingCapacity]);
            return;
        }
    }
}

// Client-data tag on every registration ReRegisterForFinalize arms. An
// allocation-time registration passes null instead, so a suppress that removes
// a registration can tell "armed after a queuing" (worth a marker below) from
// the plain Dispose-pattern suppress, which stays markerless and cheap.
char g_finalizer_rereg_tag;

// Objects suppressed AFTER the collector had already queued them. Boehm's
// deregistration only edits its finalizer table, which no longer holds an entry
// the collection moved onto the ring, so the ring's consumer has to drop the
// entry instead — real .NET reads a header bit at dequeue for the same reason.
//
// A slot is a SHORT Boehm disappearing link, and both halves earn their keep. Weak,
// so membership roots nothing and the set can be unbounded. Short, so an entry
// naming an object that was never really queued (a second suppress on a live one)
// is cleared exactly when that object becomes finalizer-reachable, and so cannot
// swallow a finalizer a later ReRegisterForFinalize armed; a long link outlives
// that transition. Chunks are malloc'd, never rooted — a scanned slot would mark
// its own referent, and Boehm requires a heap address of the referent, not of the
// link — and never moved, since relocating a slot means re-registering its link
// against a referent that may have died in between.
//
// The registrar edit that JUSTIFIES a set write, and the set read that justifies
// a registrar probe, are one step under g_suppress_mtx — hence the _locked
// halves below. A dequeue may skip the mutex only on a zero bound, and a
// suppress raises the bound before its first registrar edit, so a zero read
// predates the whole critical section. Observed half-done, a suppress raced
// by ReRegisterForFinalize runs one registration's body twice: the dequeue
// misses a marker not yet published and runs it, then the collector clears
// that marker as it queues the re-registration, and the second entry runs it
// again.
//
// An entry also records whether the suppress REMOVED a registration that
// ReRegisterForFinalize had armed (recognisable by its tagged client data).
// Queued, that order means "drop the queued entry, keep the future one" —
// real .NET runs the finalizer once, after the NEXT unreachability — so the
// dequeue that drops the entry re-arms the registration the suppress removed.
// On an object that was merely live the collector clears the marker at
// f-reachability before it can act, and the removal already was the suppress.
constexpr uint32_t kSuppressChunkEntries = 64;
struct Dn2CppSuppressChunk
{
    Dn2CppSuppressChunk* next;
    void* slots[kSuppressChunkEntries]; // GC_HIDE_POINTER'd; null = free or collector-cleared
    uint8_t restore[kSuppressChunkEntries]; // re-arm the re-registration on drop
};
std::mutex& g_suppress_mtx = dn2cpp_never_destroyed<std::mutex>();
Dn2CppSuppressChunk* g_suppress_head = nullptr;
// Upper bound on occupied slots — an in-flight suppress holds a transient +1,
// and the collector clears a slot without telling us, so this only falls back
// to the truth on a full miss scan. It exists so the drain of a program that
// never suppresses post-enqueue (i.e. nearly every one) costs a single
// relaxed load.
std::atomic<uint64_t> g_suppress_count{0};

// The slot walk shared by add and take. It must run under Boehm's ALLOCATOR
// lock, not just g_suppress_mtx: the collector clears a registered slot with a
// plain write while the world runs (GC_finalize), so a read anywhere else is a
// data race. Only the walk goes in the callback — the public link register /
// unregister calls take the allocator lock themselves and would self-deadlock
// inside it. A matched slot stays valid after the callback returns: every
// caller strong-roots obj (parameter or dequeue local), so its short link
// cannot be collector-cleared in between. restore[] is ours alone, under
// g_suppress_mtx.
struct Dn2CppSuppressScan
{
    void* hidden;
    void** match;
    uint8_t* matchRestore;
    void** freeSlot;
    uint8_t* freeRestore;
    uint64_t occupied;
};

void* GC_CALLBACK dn2cpp_scan_suppress_slots(void* data)
{
    auto& scan = *static_cast<Dn2CppSuppressScan*>(data);
    // Accumulated locally and published once: the counted quantity is the loop's
    // trip count, and an atomic per slot would itself dominate what it measures.
    uint64_t walked = 0;
    for (Dn2CppSuppressChunk* c = g_suppress_head; c != nullptr; c = c->next)
        for (uint32_t i = 0; i < kSuppressChunkEntries; i++)
        {
            walked++;
            void* value = c->slots[i];
            if (value == scan.hidden)
            {
                scan.match = &c->slots[i];
                scan.matchRestore = &c->restore[i];
            }
            if (value == nullptr && scan.freeSlot == nullptr)
            {
                scan.freeSlot = &c->slots[i];
                scan.freeRestore = &c->restore[i];
            }
            if (value != nullptr)
                scan.occupied++;
        }
    g_suppress_scans.fetch_add(1, std::memory_order_relaxed);
    g_suppress_slots_walked.fetch_add(walked, std::memory_order_relaxed);
    return nullptr;
}

void dn2cpp_suppress_set_add_locked(Dn2CppObject* obj, bool restore)
{
    void* hidden = reinterpret_cast<void*>(GC_HIDE_POINTER(obj));
    Dn2CppSuppressScan scan{hidden, nullptr, nullptr, nullptr, nullptr, 0};
    GC_call_with_alloc_lock(dn2cpp_scan_suppress_slots, &scan);
    if (scan.match != nullptr)
    {
        // Already recorded; a repeated suppress is legal. It can only
        // strengthen the record: a later suppress that removed a fresh
        // re-registration owes the dequeue its re-arm.
        if (restore)
            *scan.matchRestore = 1;
        return;
    }
    void** slot = scan.freeSlot;
    uint8_t* restoreSlot = scan.freeRestore;
    if (slot == nullptr)
    {
        auto* c = static_cast<Dn2CppSuppressChunk*>(
            std::calloc(1, sizeof(Dn2CppSuppressChunk)));
        if (c == nullptr)
            dn2cpp_fail("out of memory (suppressed-finalizer set)");
        c->next = g_suppress_head;
        g_suppress_head = c;
        g_suppress_chunks.fetch_add(1, std::memory_order_relaxed);
        slot = &c->slots[0];
        restoreSlot = &c->restore[0];
    }
    *restoreSlot = restore ? 1 : 0; // before the slot: a reused slot's byte is stale
    *slot = hidden;
    // An unregistered link never disappears, so the slot would name this address
    // forever — and Boehm hands a dead object's address to the next allocation, so
    // an unrelated object landing there would match and lose its finalizer with
    // nothing said. Hand the slot back instead: the suppress is then simply not
    // honoured for an already-queued object, which costs one extra finalization and
    // is visible. Both non-success codes are reachable — GC_NO_MEMORY once the
    // default GC_oom_fn has returned null, GC_UNIMPLEMENTED under GC_FIND_LEAK,
    // where the whole disappearing-link machinery opts out.
    int rc = GC_general_register_disappearing_link(slot, obj);
    if (rc != GC_SUCCESS && rc != GC_DUPLICATE)
    {
        *slot = nullptr;
        return;
    }
    g_suppress_recorded.fetch_add(1, std::memory_order_relaxed);
    g_suppress_count.fetch_add(1, std::memory_order_release);
}

// True when obj's finalization was suppressed post-enqueue; consumes the entry.
bool dn2cpp_suppress_set_take_locked(Dn2CppObject* obj, bool* restore)
{
    void* hidden = reinterpret_cast<void*>(GC_HIDE_POINTER(obj));
    Dn2CppSuppressScan scan{hidden, nullptr, nullptr, nullptr, nullptr, 0};
    GC_call_with_alloc_lock(dn2cpp_scan_suppress_slots, &scan);
    if (scan.match != nullptr)
    {
        *restore = *scan.matchRestore != 0;
        GC_unregister_disappearing_link(scan.match);
        // Unregistered first: the collector no longer knows the slot, so this
        // plain clear cannot race its own.
        *scan.match = nullptr;
        g_suppress_count.fetch_sub(1, std::memory_order_release);
        return true;
    }
    // A full miss saw every slot, so the bound can be retightened here — and only
    // here, which is what lets a set the collector emptied stop costing a scan.
    // Cannot erase an in-flight suppress's +1: bump and retighten share the mutex.
    g_suppress_count.store(scan.occupied, std::memory_order_release);
    return false;
}

// Run one dequeued object's Finalize() body. Shared by the dedicated finalizer
// thread and the manual (main-thread) drain so both consumers behave identically.
// An uncaught finalizer exception crashes the process in real .NET (verified
// against net10.0: an InvalidOperationException thrown from a finalizer body
// aborts with no further finalizers run) — match that instead of swallowing it,
// so any other queued finalizer is correctly left un-run rather than papered over.
//
// NOINLINE so this frame DIES on return: inlined, its object slots join the
// consumer's long-lived loop frame, which is scanned live on every collection
// and out of the idle scrub's reach — pinning a dropped entry's object past
// the second unreachability its re-registration waits for.
DN2CPP_NOINLINE void dn2cpp_run_finalizer_body(Dn2CppObject* obj)
{
    g_suppress_dequeues.fetch_add(1, std::memory_order_relaxed);
    // The one place every queued entry passes through, so the one place a
    // suppress that arrived after the enqueue can still cancel the body. The
    // unlocked bound is the fast path for the programs that never suppress one:
    // a suppress raises the bound before its first registrar edit, so reading
    // zero means no suppress critical section is in flight or published — this
    // dequeue linearizes before all of them, a race it legitimately wins.
    if (g_suppress_count.load(std::memory_order_acquire) != 0)
    {
        g_suppress_probes.fetch_add(1, std::memory_order_relaxed);
        // The body must NOT run under this lock: a Finalize calling
        // SuppressFinalize(this) takes the same, non-recursive, mutex.
        std::lock_guard<std::mutex> lock(g_suppress_mtx);
        bool restore = false;
        if (dn2cpp_suppress_set_take_locked(obj, &restore))
        {
            // A registration still on the object was armed AFTER that suppress:
            // the collector consumed the one whose queuing produced this entry,
            // so the only thing that can have put one back is
            // ReRegisterForFinalize undoing the suppress. Honour it from THIS
            // entry, taking it so nothing fires twice. Dropping instead needs
            // the object to become unreachable a second time, which a
            // conservative scan may never grant.
            GC_finalization_proc rearmed = nullptr;
            void* rearmedData = nullptr;
            GC_register_finalizer_no_order(obj, nullptr, nullptr, &rearmed, &rearmedData);
            if (rearmed == nullptr)
            {
                // The marker's suppress came AFTER a re-registration and removed
                // it (queue -> re-register -> suppress). The queued entry is
                // dropped, but the registration must come back: real .NET runs
                // it once, after the next unreachability. Re-armed under the
                // mutex, so a racing suppress serializes behind this dequeue
                // and sees the registration it must remove.
                if (restore)
                    GC_register_finalizer_no_order(obj, dn2cpp_finalizer_callback,
                        &g_finalizer_rereg_tag, &rearmed, &rearmedData);
                return;
            }
        }
    }
    try
    {
        if (obj->type->finalize != nullptr)
            obj->type->finalize(obj);
    }
    catch (Dn2CppException& ex)
    {
        const char* name = (ex.obj != nullptr && ex.obj->type != nullptr)
            ? ex.obj->type->name : "<unknown>";
        char buf[256];
        std::snprintf(buf, sizeof(buf), "unhandled exception in finalizer: %s", name);
        dn2cpp_fail(buf);
    }
}

// Zero the band of dead stack just below the caller's frame. A conservative scan walks
// everything above the scanned thread's live SP, so a stale object pointer parked in
// that band pins whatever heap block it names — including one already freed and reused,
// which then never becomes f-reachable however often the program collects. Managed code
// cannot scrub this itself: a source-level stomp helper is pure and the optimizer
// deletes it. noinline so the buffer occupies fresh frames below the caller; volatile
// so the stores are not elided.
//
// This is the tree's ONE deliberate large stack frame, exempted from
// DN2CPP_MAX_STACK_FRAME by hand: the frame IS the function, so moving the band to the
// heap would scrub heap — the opposite of what a conservative stack scan needs.
//
// 16 KiB is a band, not a bound. On a target with smaller thread stacks this call is
// itself the overflow: shrinking it trades scrub coverage for headroom, deleting it
// trades a leak no test reproduces.
#if defined(__clang__) || defined(__GNUC__)
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wframe-larger-than="
#endif
DN2CPP_NOINLINE void dn2cpp_scrub_stack()
{
    volatile char band[16384];
    for (size_t i = 0; i < sizeof(band); i++)
        band[i] = 0;
}
#if defined(__clang__) || defined(__GNUC__)
#pragma GCC diagnostic pop
#endif

// The dedicated finalizer thread's lifecycle, for dn2cpp_runtime_quiesce: the
// handle is retained (never detached) so a host can join the thread before
// unloading the library. The running flag is double-checked because
// dn2cpp_ensure_finalizer_thread runs on every finalizable allocation — the
// common already-running case must stay a single atomic load.
std::mutex& g_finalizer_thread_mtx = dn2cpp_never_destroyed<std::mutex>();
std::thread& g_finalizer_thread = dn2cpp_never_destroyed<std::thread>();
std::atomic<int> g_finalizer_thread_running{0};
std::atomic<int> g_finalizer_stop{0};
std::atomic<int> g_finalizer_exited{0};

void dn2cpp_finalizer_thread_main()
{
    Dn2CppGCThread guard; // Finalize() bodies run managed code -> must be GC-registered
    t_on_finalizer_thread = true;
    // Resume from the global drain cursor, not 0: a restarted thread (after a
    // quiesce) must keep consuming where its predecessor stopped, or its ring
    // position would fall out of step with the producers'.
    uint64_t tail = g_finalizer_tail.load(std::memory_order_acquire);
    bool ranSinceIdle = false;
    for (;;)
    {
        std::atomic<Dn2CppObject*>& slot = g_finalizer_ring[tail % kFinalizerRingCapacity];
        Dn2CppObject* obj = slot.load(std::memory_order_acquire);
        if (obj == nullptr)
        {
            // Slot not yet published (a producer bumped head but has not
            // release-stored obj here yet, or nothing is queued). Catch up on
            // any overflow spill first — spilled entries only exist because
            // the ring was full at their enqueue, so by the time the ring
            // looks empty they are the oldest pending work. Then plain
            // sleep-poll, not a condition_variable: this thread must never
            // block on a primitive the GC callback (running on whatever thread
            // triggers a collection) could contend on.
            Dn2CppObject* spilled = dn2cpp_finalizer_spill_pop();
            if (spilled != nullptr)
            {
                dn2cpp_run_finalizer_body(spilled);
                g_finalizer_spill_drained.fetch_add(1, std::memory_order_release);
                ranSinceIdle = true;
                continue;
            }
            // Checked only with the ring and the spill both empty, so a stop
            // never abandons work that is already queued.
            if (g_finalizer_stop.load(std::memory_order_acquire))
                break;
            // Going idle: scrub the stack the drained bodies dirtied, once per
            // idle transition, so no stale object pointer sits in the scanned
            // band for as long as this thread keeps sleeping.
            if (ranSinceIdle)
            {
                dn2cpp_scrub_stack();
                ranSinceIdle = false;
            }
            std::this_thread::sleep_for(std::chrono::milliseconds(2));
            continue;
        }
        // Clear the slot before running the finalizer: a wrapped producer can
        // then reuse this physical cell without its leftover pointer being
        // mistaken for a fresh entry, and the ring stops keeping a finalized
        // (or resurrection-eligible) object reachable. The release store on
        // g_finalizer_tail below publishes this clear to a reusing producer.
        slot.store(nullptr, std::memory_order_relaxed);
        dn2cpp_run_finalizer_body(obj);
        // Ring drains must arm the idle scrub exactly like spill drains: this
        // body's dead frames (and the enqueue callback's, from any collection it
        // ran) hold object pointers, and a thread that sleeps over them pins
        // those blocks for as long as it keeps sleeping — including an object a
        // dropped-entry's re-registration needs collectable a second time.
        ranSinceIdle = true;
        tail++;
        g_finalizer_tail.store(tail, std::memory_order_release);
    }
    g_finalizer_exited.store(1, std::memory_order_release);
}

void dn2cpp_ensure_finalizer_thread()
{
    // std::call_once (not compare_exchange_strong on its own): guarantees
    // g_finalizer_ring is fully allocated — with a happens-before edge — before
    // any other thread's dn2cpp_register_finalizer call returns and reaches a
    // GC_MALLOC that could invoke the callback that writes into it.
    static std::once_flag once;
    std::call_once(once, [] {
        g_finalizer_ring = static_cast<std::atomic<Dn2CppObject*>*>(
            GC_MALLOC_UNCOLLECTABLE(sizeof(std::atomic<Dn2CppObject*>) * kFinalizerRingCapacity));
    });
    // Manual-drain mode (Godot): no background thread — the ring is drained
    // on the calling thread via dn2cpp_gc_drain_finalizers(). The GC callback
    // still enqueues here as normal. g_finalizer_manual_drain is set once,
    // before managed init and thus before this first registration runs.
    if (g_finalizer_manual_drain != 0)
        return;
    if (g_finalizer_thread_running.load(std::memory_order_acquire) != 0)
        return;
    // The spawn is not part of the call_once: after a quiesce joined the
    // thread, the next finalizable allocation must be able to start it again.
    std::lock_guard<std::mutex> lk(g_finalizer_thread_mtx);
    if (g_finalizer_thread_running.load(std::memory_order_relaxed) != 0)
        return;
    g_finalizer_stop.store(0, std::memory_order_relaxed);
    g_finalizer_exited.store(0, std::memory_order_relaxed);
    g_finalizer_thread = std::thread(dn2cpp_finalizer_thread_main);
    g_finalizer_thread_running.store(1, std::memory_order_release);
}

// The finalizer-thread half of dn2cpp_runtime_quiesce. Polls, no cv: the
// finalizer thread never blocks on a primitive the GC callback could contend
// on, and its idle loop already wakes every 2ms. Returns 1 (joined), 0 (was
// not running — Godot's manual-drain lane always lands here), or -1 (deadline
// passed with a Finalize() body still running; the thread is detached and
// left stopped for good).
int32_t dn2cpp_finalizer_quiesce(std::chrono::steady_clock::time_point deadline, bool infinite)
{
    {
        std::lock_guard<std::mutex> lk(g_finalizer_thread_mtx);
        if (g_finalizer_thread_running.load(std::memory_order_relaxed) == 0)
            return 0;
    }
    g_finalizer_stop.store(1, std::memory_order_release);
    while (g_finalizer_exited.load(std::memory_order_acquire) == 0)
    {
        if (!infinite && std::chrono::steady_clock::now() >= deadline)
        {
            g_finalizer_thread.detach();
            return -1; // running + stop stay set: dead, not respawnable
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
    g_finalizer_thread.join();
    std::lock_guard<std::mutex> lk(g_finalizer_thread_mtx);
    g_finalizer_stop.store(0, std::memory_order_relaxed);
    g_finalizer_exited.store(0, std::memory_order_relaxed);
    g_finalizer_thread_running.store(0, std::memory_order_release);
    return 1;
}
} // namespace
#endif

int32_t dn2cpp_runtime_quiesce(int32_t timeout_ms)
{
    // Serialize whole quiesces: two callers joining the same handles is UB.
    static std::mutex& mtx = dn2cpp_never_destroyed<std::mutex>();
    std::lock_guard<std::mutex> lk(mtx);
    bool infinite = timeout_ms < 0;
    auto deadline = std::chrono::steady_clock::now()
        + std::chrono::milliseconds(infinite ? 0 : timeout_ms);
    // Pool first: a worker's final items may register finalizers, which the
    // finalizer thread (when it runs at all) can still consume before stopping.
    int32_t pool = dn2cpp_pool_quiesce(deadline, infinite);
#ifdef DN2CPP_USE_BOEHM_GC
    int32_t fin = dn2cpp_finalizer_quiesce(deadline, infinite);
#else
    int32_t fin = 0;
#endif
    if (pool < 0 || fin < 0)
        return -1;
    return pool + fin;
}

void dn2cpp_register_finalizer(Dn2CppObject* obj)
{
#ifdef DN2CPP_USE_BOEHM_GC
    dn2cpp_ensure_finalizer_thread();
    GC_finalization_proc oldProc;
    void* oldData;
    // _no_order: dn2cpp does not model finalization ordering constraints
    // between objects that reference each other — neither does .NET, which
    // documents finalization order across a reachable graph as unspecified —
    // so the unordered registrar is the correct match.
    GC_register_finalizer_no_order(obj, dn2cpp_finalizer_callback, nullptr, &oldProc, &oldData);
#else
    // DN2CPP_NO_GC: the calloc fallback never collects, so an object is never
    // unreachable and its finalizer would never fire anyway — no-op.
    (void)obj;
#endif
}

void dn2cpp_gc_suppress_finalize(Dn2CppObject* obj)
{
    if (obj == nullptr)
        dn2cpp_throw_argument_null(); // real .NET: ArgumentNullException, catchable
#ifdef DN2CPP_USE_BOEHM_GC
    if (obj->type->finalize == nullptr)
        return; // no Finalize override -> nothing was ever registered
    g_suppress_calls.fetch_add(1, std::memory_order_relaxed);
    // Initialised because the call below is a no-op that leaves both UNTOUCHED
    // under GC_FIND_LEAK, and oldProc is read.
    GC_finalization_proc oldProc = nullptr;
    void* oldData = nullptr;
    // Under the set's lock, because the unregister is what the record MEANS: a
    // consumer that reads the set between the two sees an object with neither.
    std::lock_guard<std::mutex> lock(g_suppress_mtx);
    // Raise the bound BEFORE the registrar edit: a drain that read zero then
    // predates this whole critical section. A transient over-count is legal —
    // the bound is an upper bound by contract.
    g_suppress_count.fetch_add(1, std::memory_order_release);
    // Passing a null fn unregisters the current finalizer (GC_register_
    // finalizer's documented contract) without invoking it.
    GC_register_finalizer_no_order(obj, nullptr, nullptr, &oldProc, &oldData);
    // A null previous registration means Boehm's table no longer held the object:
    // a collection has already moved it onto the ring (or it has already been
    // finalized, or suppressed before). Only the queue's consumer can honour the
    // suppress from here, so record it for that consumer.
    if (oldProc == nullptr)
        dn2cpp_suppress_set_add_locked(obj, /*restore=*/false);
    else if (oldData == &g_finalizer_rereg_tag)
        // The registration this removed was armed by ReRegisterForFinalize, so
        // the object may be sitting queued (queue -> re-register -> suppress):
        // the marker tells the dequeue to drop the entry AND re-arm what this
        // removed. If the object was merely live, the collector clears the
        // marker at f-reachability and the removal alone was the suppress.
        dn2cpp_suppress_set_add_locked(obj, /*restore=*/true);
    g_suppress_count.fetch_sub(1, std::memory_order_release);
#else
    (void)obj;
#endif
}

void dn2cpp_gc_reregister_for_finalize(Dn2CppObject* obj)
{
    if (obj == nullptr)
        dn2cpp_throw_argument_null(); // real .NET: ArgumentNullException, catchable
#ifdef DN2CPP_USE_BOEHM_GC
    if (obj->type->finalize == nullptr)
        return; // matches real GC.ReRegisterForFinalize: legal, but a no-op
                 // on a type that was never finalizable
    // Outside g_suppress_mtx, and it is the only one of the three paths that may
    // be: this one never reads the set, and Boehm publishes the registration
    // atomically, so a concurrent dequeue probe reads it before or after.
    //
    // No suppressed-set edit here on purpose, in either direction. For an entry
    // naming a LIVE object the collector drops it when obj next becomes
    // finalizer-reachable, before it could cancel the registration this call
    // just made. For one naming an already-queued object the registration
    // itself is the undo, and the ring's consumer reads it —
    // dn2cpp_run_finalizer_body; clearing the entry here instead would let the
    // queued body run in ADDITION to the re-registered one.
    dn2cpp_ensure_finalizer_thread();
    GC_finalization_proc oldProc;
    void* oldData;
    // Tagged client data — see g_finalizer_rereg_tag: a suppress that removes
    // this registration must know it was armed by a re-register, because on a
    // queued object it then has to survive the drop of the queued entry.
    GC_register_finalizer_no_order(obj, dn2cpp_finalizer_callback,
        &g_finalizer_rereg_tag, &oldProc, &oldData);
#else
    (void)obj;
#endif
}

void dn2cpp_keep_alive(Dn2CppObject* obj)
{
    // The out-of-line call is the barrier (no LTO in the build); the empty asm
    // additionally pins `obj` as read even if a future build inlines this.
#if defined(__GNUC__) && !defined(__EMSCRIPTEN__)
    __asm__ __volatile__("" : : "r"(obj) : "memory");
#else
    (void)obj;
#endif
}

void dn2cpp_gc_collect()
{
#ifdef DN2CPP_USE_BOEHM_GC
    // Clear the dead band this call chain is about to be scanned over: the
    // collector's frames land in part of it, but every deterministic retry of
    // Collect() rebuilds the same layout, so a stale pointer in a slot the
    // collector's own frames never reach would otherwise survive round after
    // round and pin its (possibly reused) block forever.
    dn2cpp_scrub_stack();
    GC_gcollect();
    // A collection only *decides* which newly-unreachable objects are
    // finalizable; actually invoking their callbacks normally happens lazily
    // on a later allocation. Drain explicitly so a Collect() the caller pairs
    // with WaitForPendingFinalizers observes them promptly instead of racing
    // the next GC_MALLOC.
    while (GC_should_invoke_finalizers())
        GC_invoke_finalizers();
#endif
}

// Run every managed Finalize() body queued on the ring as of entry, on the
// calling thread. This is the manual-drain counterpart to the dedicated
// finalizer thread: the Godot GDExtension flips manual mode on so RefCounted
// teardown (engine object destroy / unreference) runs on the engine's main
// thread — via GC.WaitForPendingFinalizers and the per-frame process hook —
// rather than off-thread. No-op unless manual mode is active.
void dn2cpp_gc_drain_finalizers()
{
#ifdef DN2CPP_USE_BOEHM_GC
    // Only meaningful in manual-drain mode: with the dedicated finalizer thread
    // running it is the sole ring consumer, and a second consumer here would race
    // the tail counter. No-op otherwise, so callers need not know the active mode.
    if (g_finalizer_manual_drain == 0)
        return;
    if (g_finalizer_ring == nullptr)
        return; // nothing finalizable was ever registered -> ring not allocated

    // Reentrancy guard: a Finalize() body that itself collects + drains (directly
    // or via GC.WaitForPendingFinalizers) must not re-enter this loop and spin on
    // the slot the outer call already cleared. The reentrant call returns; the
    // outer loop's head snapshot bounds this drain, and anything a finalizer body
    // queues is picked up by the next top-level drain (matching real
    // WaitForPendingFinalizers' "pending as of now" contract).
    static thread_local bool draining = false;
    if (draining)
        return;
    draining = true;
    struct DrainGuard { ~DrainGuard() { draining = false; } } drainGuard;

    // Cross-thread exclusion (the thread_local guard above only stops the
    // same thread re-entering): manual mode lets any managed thread drain, and
    // two concurrent drains sharing a tail snapshot would double-run or spin.
    // Taken *after* the reentrancy check so a draining Finalize() body never
    // self-deadlocks; a second thread blocks here until the first pass ends,
    // which also gives its WaitForPendingFinalizers caller the completion
    // guarantee it came for. The head/tail snapshot happens under the lock so
    // it observes the previous drainer's progress.
    std::lock_guard<std::mutex> drainLock(g_finalizer_drain_mtx);

    uint64_t target = g_finalizer_head.load(std::memory_order_acquire);
    uint64_t spillTarget = g_finalizer_spill_enqueued.load(std::memory_order_acquire);
    uint64_t tail = g_finalizer_tail.load(std::memory_order_relaxed);
    bool ranAny = false;
    while (tail < target)
    {
        std::atomic<Dn2CppObject*>& slot = g_finalizer_ring[tail % kFinalizerRingCapacity];
        Dn2CppObject* obj = slot.load(std::memory_order_acquire);
        if (obj == nullptr)
        {
            // head was bumped but this slot is not yet published — only possible
            // when a collection is triggered off the draining thread (the GC
            // callback runs on whatever thread hits the allocation). Yield until
            // the producer's release-store lands, mirroring the finalizer thread.
            std::this_thread::yield();
            continue;
        }
        // Clear before running (same reasoning as the thread): a wrapped producer
        // can reuse this cell, and the ring stops pinning a finalized object.
        slot.store(nullptr, std::memory_order_relaxed);
        dn2cpp_run_finalizer_body(obj);
        ranAny = true;
        tail++;
        g_finalizer_tail.store(tail, std::memory_order_release);
    }
    // Overflow spill: entries that could not claim a ring slot. Bounded by the
    // same entry snapshot (anything spilled by a finalizer body above belongs
    // to the next top-level drain). Every snapshot entry was fully pushed
    // before the snapshot, so a pop can only come back empty here if the
    // counters were corrupted — bail rather than spin forever.
    while (g_finalizer_spill_drained.load(std::memory_order_relaxed) < spillTarget)
    {
        Dn2CppObject* obj = dn2cpp_finalizer_spill_pop();
        if (obj == nullptr)
            break;
        dn2cpp_run_finalizer_body(obj);
        ranAny = true;
        g_finalizer_spill_drained.fetch_add(1, std::memory_order_release);
    }
    // Same idle-transition hygiene as the dedicated thread: the drained bodies'
    // stack band belongs to this (usually long-lived engine main) thread, and a
    // stale object pointer left there would pin a reused block until the next
    // drain happens to dirty the same slot.
    if (ranAny)
        dn2cpp_scrub_stack();
#endif
}

void dn2cpp_gc_wait_for_pending_finalizers()
{
#ifdef DN2CPP_USE_BOEHM_GC
    // Mirror dn2cpp_gc_collect's drain — needed here too, since
    // WaitForPendingFinalizers can be called without a preceding explicit
    // Collect() (e.g. right after an organic collection).
    while (GC_should_invoke_finalizers())
        GC_invoke_finalizers();
    // Manual-drain mode: no finalizer thread to wait on — the callbacks above
    // only enqueued onto the ring. Run those bodies here, on the calling (main)
    // thread, so RefCounted engine teardown happens on the main thread.
    if (g_finalizer_manual_drain != 0)
    {
        dn2cpp_gc_drain_finalizers();
        return;
    }
    // On the finalizer thread itself (i.e. called from a Finalize() body):
    // return immediately, matching real .NET (verified on net10.0). The
    // in-flight item's tail is only published after its body returns, so
    // waiting here would deadlock the finalizer thread against itself and
    // stall all finalization for the rest of the process.
    if (t_on_finalizer_thread)
        return;
    // Wait until the finalizer thread has drained everything queued as of
    // this call (a snapshot of head plus the overflow-spill count — anything
    // queued afterward is not this call's problem, matching real
    // WaitForPendingFinalizers' "as of now" contract). No finalizer thread has
    // been started yet iff nothing was ever finalizable, in which case both
    // counts are still 0 and this returns immediately.
    uint64_t target = g_finalizer_head.load(std::memory_order_acquire);
    uint64_t spillTarget = g_finalizer_spill_enqueued.load(std::memory_order_acquire);
    while (g_finalizer_tail.load(std::memory_order_acquire) < target
        || g_finalizer_spill_drained.load(std::memory_order_acquire) < spillTarget)
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
#endif
}

int64_t dn2cpp_gc_get_total_memory(int32_t forceFullCollection)
{
#ifdef DN2CPP_USE_BOEHM_GC
    if (forceFullCollection)
        dn2cpp_gc_collect();
    return static_cast<int64_t>(GC_get_heap_size() - GC_get_free_bytes());
#else
    (void)forceFullCollection;
    return 0; // heap usage is not modeled under the calloc fallback
#endif
}

int32_t dn2cpp_gc_collection_count()
{
#ifdef DN2CPP_USE_BOEHM_GC
    // GC_get_gc_no is the count of completed collection cycles; it is the same
    // value dn2cpp_gc_stats_dump already reports. Boehm has no per-generation
    // counters, so this answers every generation.
    return static_cast<int32_t>(GC_get_gc_no());
#else
    return 0; // no collections occur under the calloc fallback
#endif
}

int64_t dn2cpp_gc_heap_size_bytes()
{
#ifdef DN2CPP_USE_BOEHM_GC
    return static_cast<int64_t>(GC_get_heap_size());
#else
    return 0; // heap size is not modeled under the calloc fallback
#endif
}

int64_t dn2cpp_gc_free_bytes()
{
#ifdef DN2CPP_USE_BOEHM_GC
    return static_cast<int64_t>(GC_get_free_bytes());
#else
    return 0; // free-byte accounting is not modeled under the calloc fallback
#endif
}

// GCHandle low-level handle table (GCHandle.InternalAlloc/Get/Set/Free/
// CompareExchange) — real weak references.
// See the Dn2CppWeakCell header note for the short/long link split and why
// hiddenTarget must stay GC_HIDE_POINTER-obscured. The handle value is the
// Dn2CppWeakCell's address.
namespace
{
#ifdef DN2CPP_USE_BOEHM_GC
// The body of dn2cpp_weakcell_read, run while holding Boehm's allocator lock.
void* dn2cpp_weakcell_read_locked(void* p)
{
    auto* cell = static_cast<const Dn2CppWeakCell*>(p);
    // Boehm clears a disappearing/long link by writing a *raw* 0 into the slot
    // (finalize.c: `*link = NULL`) once the referent is collected — this must
    // read back as null. That raw 0 is NOT the same bit pattern as a hidden
    // null: GC_HIDE_POINTER(p) is ~p, so a live referent never stores 0, and a
    // WeakReference constructed on null stores GC_HIDE_POINTER(nullptr) == ~0
    // (which reveals back to null). So hiddenTarget == 0 uniquely means
    // "collected"; revealing it would instead yield ~0, a non-null garbage
    // pointer the caller would go on to dereference.
    if (cell->hiddenTarget == 0)
        return nullptr;
    return reinterpret_cast<void*>(GC_REVEAL_POINTER(cell->hiddenTarget));
}
// Serializes weak-cell mutations (set/free/compare-exchange) against each
// other. Real .NET's GCHandle.InternalCompareExchange is atomic and
// WeakReference.Target set is thread-safe; the unlink→write→relink sequence
// here is not, so without exclusion two racing setters can leave the slot's
// hidden value and the registered link disagreeing (a stale link never
// cleared -> a read returning a dangling pointer). Readers stay lock-free
// against this mutex; their synchronization against the *collector's* clear
// is the allocator lock above.
std::mutex& g_weakcell_mtx = dn2cpp_never_destroyed<std::mutex>();
#endif

Dn2CppObject* dn2cpp_weakcell_read(const Dn2CppWeakCell* cell)
{
#ifdef DN2CPP_USE_BOEHM_GC
    // Boehm's documented contract for disappearing links (gc.h): a disguised
    // pointer must be accessed under the allocator lock — otherwise the
    // collector can decide the referent is dead between our load of the
    // hidden value and the reveal, handing back a pointer to reclaimed
    // memory. Holding the lock stalls collection until the revealed pointer
    // is a visible strong reference on this (registered, scanned) stack.
    return static_cast<Dn2CppObject*>(GC_call_with_alloc_lock(
        dn2cpp_weakcell_read_locked, const_cast<Dn2CppWeakCell*>(cell)));
#else
    return reinterpret_cast<Dn2CppObject*>(cell->hiddenTarget);
#endif
}

// Stores target (hidden) and, if non-null, registers &cell->hiddenTarget as a
// disappearing/long link of the kind cell->isLong says. Boehm requires the
// link to be a pointer-sized slot inside a GC-allocated object —
// Dn2CppWeakCell itself, allocated via dn2cpp_alloc, satisfies that.
void dn2cpp_weakcell_write_and_link(Dn2CppWeakCell* cell, Dn2CppObject* target)
{
#ifdef DN2CPP_USE_BOEHM_GC
    cell->hiddenTarget = static_cast<intptr_t>(GC_HIDE_POINTER(target));
    if (target == nullptr)
        return;
    void** link = reinterpret_cast<void**>(&cell->hiddenTarget);
    if (cell->isLong)
        GC_register_long_link(link, target);
    else
        GC_general_register_disappearing_link(link, target);
#else
    cell->hiddenTarget = reinterpret_cast<intptr_t>(target);
#endif
}

// Unregisters whatever link kind cell currently holds (a no-op if none is
// registered, or if the link was already cleared by a collection).
void dn2cpp_weakcell_unlink(Dn2CppWeakCell* cell)
{
#ifdef DN2CPP_USE_BOEHM_GC
    void** link = reinterpret_cast<void**>(&cell->hiddenTarget);
    if (cell->isLong)
        GC_unregister_long_link(link);
    else
        GC_unregister_disappearing_link(link);
#endif
}
} // namespace

intptr_t dn2cpp_gchandle_internal_alloc(Dn2CppObject* target, int32_t handleType)
{
    // dn2cpp_alloc_atomic (GC_MALLOC_ATOMIC), NOT dn2cpp_alloc: a pointer-containing
    // cell is scanned by the ordinary mark phase regardless of GC_HIDE_POINTER
    // obscuring the bit pattern, which re-marks the referent live through this very
    // cell and silently defeats the disappearing link. Only the non-scanned allocator
    // keeps the link's storage out of that phase.
    // Only the two weak kinds may land here: a Normal/Pinned handleType would
    // silently become a short weak link in an atomic (unscanned) cell — i.e. a
    // handle the caller believes is strong that roots nothing. The one known
    // corelib caller passing Normal (ComAwareWeakReference.SetComInfoInConstructor)
    // is unreachable on non-COM platforms; if it is ever reached, fail loudly
    // instead of corrupting liveness.
    if (handleType != 0 && handleType != 1)
        dn2cpp_fail("GCHandle.InternalAlloc: only Weak/WeakTrackResurrection reach the internal weak-handle intrinsics");
    auto* cell = static_cast<Dn2CppWeakCell*>(dn2cpp_alloc_atomic(sizeof(Dn2CppWeakCell)));
    cell->isLong = (handleType == 1) ? 1 : 0; // 1 = GCHandleType.WeakTrackResurrection
    dn2cpp_weakcell_write_and_link(cell, target);
    return reinterpret_cast<intptr_t>(cell);
}

Dn2CppObject* dn2cpp_gchandle_internal_get(intptr_t handle)
{
    return handle == 0 ? nullptr : dn2cpp_weakcell_read(reinterpret_cast<Dn2CppWeakCell*>(handle));
}

void dn2cpp_gchandle_internal_set(intptr_t handle, Dn2CppObject* target)
{
    if (handle == 0)
        return;
    auto* cell = reinterpret_cast<Dn2CppWeakCell*>(handle);
#ifdef DN2CPP_USE_BOEHM_GC
    std::lock_guard<std::mutex> lk(g_weakcell_mtx);
#endif
    dn2cpp_weakcell_unlink(cell);
    dn2cpp_weakcell_write_and_link(cell, target);
}

void dn2cpp_gchandle_internal_free(intptr_t handle)
{
    if (handle == 0)
        return;
    auto* cell = reinterpret_cast<Dn2CppWeakCell*>(handle);
#ifdef DN2CPP_USE_BOEHM_GC
    std::lock_guard<std::mutex> lk(g_weakcell_mtx);
#endif
    dn2cpp_weakcell_unlink(cell);
    cell->hiddenTarget = 0;
    // The cell itself is GC-managed; dropping the handle is enough to reclaim
    // it (no manual free of the Dn2CppWeakCell block).
}

Dn2CppObject* dn2cpp_gchandle_internal_compare_exchange(intptr_t handle, Dn2CppObject* value, Dn2CppObject* oldValue)
{
    if (handle == 0)
        return nullptr;
    auto* cell = reinterpret_cast<Dn2CppWeakCell*>(handle);
#ifdef DN2CPP_USE_BOEHM_GC
    std::lock_guard<std::mutex> lk(g_weakcell_mtx);
#endif
    Dn2CppObject* prev = dn2cpp_weakcell_read(cell);
    if (prev == oldValue)
    {
        dn2cpp_weakcell_unlink(cell);
        dn2cpp_weakcell_write_and_link(cell, value);
    }
    return prev;
}

// DependentHandle (see the Dn2CppDependentCell header note): the target rides
// the low-level weak table above, the dependent is an ordinary scanned pointer
// in the cell. The cell is ordinary GC memory — the handle value embedded in
// ConditionalWeakTable's Entry structs keeps it alive, and a Dispose'd cell is
// reclaimed organically once no handle copy survives.
Dn2CppDependentHandle dn2cpp_dependenthandle_alloc(Dn2CppObject* target, Dn2CppObject* dependent)
{
    auto* cell = static_cast<Dn2CppDependentCell*>(dn2cpp_alloc(sizeof(Dn2CppDependentCell)));
    cell->targetWeak = dn2cpp_gchandle_internal_alloc(target, 0); // 0 = Weak (short link)
    cell->dependent = dependent;
    // The internal alloc above can run an incremental step that blackens the
    // fresh cell before these stores; one barrier covers both fields.
    dn2cpp_gc_write_barrier(cell);
    return Dn2CppDependentHandle{cell};
}

int32_t dn2cpp_dependenthandle_is_allocated(Dn2CppDependentHandle h)
{
    return h.cell != nullptr ? 1 : 0;
}

Dn2CppObject* dn2cpp_dependenthandle_target(Dn2CppDependentHandle h)
{
    return h.cell == nullptr ? nullptr : dn2cpp_gchandle_internal_get(h.cell->targetWeak);
}

Dn2CppObject* dn2cpp_dependenthandle_target_and_dependent(Dn2CppDependentHandle h, Dn2CppObject** dependent)
{
    // The ephemeron read contract: the dependent is handed out only while the
    // target is still alive (a collected/nulled target yields (null, null)).
    Dn2CppObject* target = dn2cpp_dependenthandle_target(h);
    *dependent = target != nullptr ? h.cell->dependent : nullptr;
    return target;
}

void dn2cpp_dependenthandle_set_target_null(Dn2CppDependentHandle h)
{
    if (h.cell != nullptr)
        dn2cpp_gchandle_internal_set(h.cell->targetWeak, nullptr);
}

void dn2cpp_dependenthandle_set_dependent(Dn2CppDependentHandle h, Dn2CppObject* dependent)
{
    if (h.cell != nullptr)
        dn2cpp_gc_store_ref(&h.cell->dependent, dependent);
}

void dn2cpp_dependenthandle_free_value(Dn2CppDependentHandle h)
{
    if (h.cell == nullptr)
        return;
    dn2cpp_gchandle_internal_free(h.cell->targetWeak);
    h.cell->targetWeak = 0;
    dn2cpp_gc_store_ref(&h.cell->dependent, static_cast<Dn2CppObject*>(nullptr));
}

void dn2cpp_dependenthandle_free(Dn2CppDependentHandle* h)
{
    dn2cpp_dependenthandle_free_value(*h);
    h->cell = nullptr;
}

int64_t dn2cpp_tickcount64()
{
    return std::chrono::duration_cast<std::chrono::milliseconds>(
               std::chrono::steady_clock::now().time_since_epoch())
        .count();
}

int32_t dn2cpp_current_processor_id()
{
    // A stable per-thread round-robin id in [0, hardware_concurrency) — callers
    // (SharedArrayPoolPartitions) use it only as a partition-spreading hint.
    static std::atomic<uint32_t> next{0};
    static const uint32_t ncpu = std::max(1u, std::thread::hardware_concurrency());
    thread_local const int32_t id =
        static_cast<int32_t>(next.fetch_add(1, std::memory_order_relaxed) % ncpu);
    return id;
}

// GCHandle's public struct model (see the Dn2CppGCHandle header note): every allocated
// handle owns one shared cell — the moral equivalent of a real .NET handle-table slot.
// The cell is GC_MALLOC_UNCOLLECTABLE (dn2cpp_alloc_pinned): scanned by the collector,
// so a Normal/Pinned cell's `target` is a strong root even when nothing in GC-visible
// memory references the cell (a ToIntPtr value parked on the native side still keeps
// its target alive), but never collected, so the ToIntPtr address stays valid for the
// handle's lifetime. Weak/WeakTrackResurrection cells hold only the internal
// weak-table handle above. Free clears the cell (kind -> 0) and recycles it through
// the pool below — never GC_FREE (see the header note: a stale struct copy may still
// point at the cell, and reallocation as an unrelated object would turn that stale
// read into type confusion; a pooled cell is always a valid cell, so a stale read at
// worst observes a later handle's slot, exactly like real .NET's recycled table slot).
struct Dn2CppGCHandleCell
{
    Dn2CppObject* target; // Normal/Pinned: the referent (strong — the cell is scanned)
    void* dataAddr;       // Pinned: cached AddrOfPinnedObject address; freed+pooled: the pool's next link
    intptr_t weakCell;    // Weak/WeakTrackResurrection: dn2cpp_gchandle_internal_alloc handle
    int32_t kind;         // 0=freed 1=Weak 2=WeakTrackResurrection 3=Normal 4=Pinned
};

namespace
{
// Free pool of cleared cells (linked through dataAddr, which is only ever read as a
// link while kind == 0). The head is a static (data segment = a Boehm root), but the
// cells are uncollectable anyway — the pool exists to bound the footprint, not to
// root anything. The mutex also serializes the freed-state check-and-clear so a
// double Free through two copies cannot push one cell twice.
Dn2CppGCHandleCell* g_gchandle_cell_pool = nullptr;
std::mutex& g_gchandle_pool_mtx = dn2cpp_never_destroyed<std::mutex>();

// Real .NET's exact wording (measured on net10.0), thrown for every operation on a
// zeroed/never-allocated handle and for FromIntPtr(0).
[[noreturn]] void dn2cpp_gchandle_throw_not_initialized()
{
    const char* msg = "Handle is not initialized.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
        dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
}

// Real .NET's exact wording (measured) for AddrOfPinnedObject on a live non-Pinned
// (Normal/Weak/WeakTrackResurrection) handle.
[[noreturn]] void dn2cpp_gchandle_throw_not_pinned()
{
    const char* msg = "Handle is not pinned.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
        dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
}

// Clears the cell and returns it to the pool; a cell already freed through another
// copy is a silent no-op (measured: real .NET does not throw on a stale copy's
// Free). The weak-table release happens after the pool lock drops — no nested locks
// (g_weakcell_mtx is independent), and the weak handle was already detached from the
// cell, so a concurrent Alloc reusing the cell cannot interfere.
void dn2cpp_gchandle_cell_release(Dn2CppGCHandleCell* cell)
{
    intptr_t weak = 0;
    int32_t kind;
    {
        std::lock_guard<std::mutex> lk(g_gchandle_pool_mtx);
        kind = cell->kind;
        if (kind == 0)
            return;
        weak = cell->weakCell;
        cell->kind = 0;
        cell->target = nullptr;
        cell->weakCell = 0;
        cell->dataAddr = g_gchandle_cell_pool;
        g_gchandle_cell_pool = cell;
    }
    if (kind == 1 || kind == 2)
        dn2cpp_gchandle_internal_free(weak);
}
} // namespace

// handleType is the raw GCHandleType (0=Weak, 1=WeakTrackResurrection, 2=Normal,
// 3=Pinned); cell kind = handleType + 1.
Dn2CppGCHandle dn2cpp_gchandle_alloc(Dn2CppObject* target, void* dataAddr, int32_t handleType)
{
    Dn2CppGCHandleCell* cell = nullptr;
    {
        std::lock_guard<std::mutex> lk(g_gchandle_pool_mtx);
        if (g_gchandle_cell_pool != nullptr)
        {
            cell = g_gchandle_cell_pool;
            g_gchandle_cell_pool = static_cast<Dn2CppGCHandleCell*>(cell->dataAddr);
            cell->dataAddr = nullptr;
        }
    }
    if (cell == nullptr) // uncollectable + zero-filled (see dn2cpp_alloc_pinned)
        cell = static_cast<Dn2CppGCHandleCell*>(dn2cpp_alloc_pinned(sizeof(Dn2CppGCHandleCell)));
    if (handleType <= 1) // Weak or WeakTrackResurrection: no strong ref in the cell
    {
        // The stored value is the collectable weak cell's address; the pooled
        // uncollectable cell is old, so the store must dirty it.
        cell->weakCell = dn2cpp_gchandle_internal_alloc(target, handleType);
        dn2cpp_gc_write_barrier(&cell->weakCell);
    }
    else // Normal or Pinned: a strong pointer + (Pinned) the pinned data address
    {
        dn2cpp_gc_store_ref(&cell->target, target);
        cell->dataAddr = dataAddr;
    }
    cell->kind = handleType + 1;
    return Dn2CppGCHandle{cell};
}

Dn2CppObject* dn2cpp_gchandle_target(Dn2CppGCHandle h)
{
    Dn2CppGCHandleCell* cell = h.cell;
    if (cell == nullptr)
        dn2cpp_gchandle_throw_not_initialized(); // default(GCHandle) / the copy Free zeroed
    if (cell->kind == 1 || cell->kind == 2)      // weak: the referent may already be gone
        return dn2cpp_gchandle_internal_get(cell->weakCell);
    // A freed cell (kind 0) reads null WITHOUT throwing — measured: a stale copy of a
    // Free'd handle returns a null Target on real .NET (the invalidated slot).
    return cell->target;
}

void dn2cpp_gchandle_set_target(Dn2CppGCHandle h, Dn2CppObject* value)
{
    Dn2CppGCHandleCell* cell = h.cell;
    // kind 0 (a stale copy of a Free'd handle) throws too: real .NET's write to an
    // invalidated slot is undefined, and writing here would clobber the pool link.
    if (cell == nullptr || cell->kind == 0)
        dn2cpp_gchandle_throw_not_initialized();
    if (cell->kind == 1 || cell->kind == 2)
    {
        dn2cpp_gchandle_internal_set(cell->weakCell, value);
        return;
    }
    if (cell->kind == 4)
    {
        // Pinned re-pins (measured: real .NET allows the set and AddrOfPinnedObject
        // then reports the NEW referent). The rep is not statically known here, so
        // discover the data address from the runtime type.
        cell->dataAddr = dn2cpp_pinned_data_addr(value);
    }
    dn2cpp_gc_store_ref(&cell->target, value);
}

void* dn2cpp_gchandle_addr(Dn2CppGCHandle h)
{
    Dn2CppGCHandleCell* cell = h.cell;
    if (cell == nullptr || cell->kind == 0)
        dn2cpp_gchandle_throw_not_initialized();
    if (cell->kind != 4)
        dn2cpp_gchandle_throw_not_pinned(); // live Normal/Weak handle: not pinned
    return cell->dataAddr;
}

int32_t dn2cpp_gchandle_is_allocated(Dn2CppGCHandle h)
{
    // The struct's own word, NOT the cell state — real .NET reads _handle != 0, so a
    // stale copy of a Free'd handle still reports true (measured) while the copy Free
    // zeroed reports false. Never throws, unlike Target.
    return h.cell != nullptr ? 1 : 0;
}

void dn2cpp_gchandle_free(Dn2CppGCHandle* h)
{
    if (h->cell == nullptr)
        dn2cpp_gchandle_throw_not_initialized(); // re-Free of this copy / default(GCHandle)
    dn2cpp_gchandle_cell_release(h->cell);       // reaches every copy (shared cell)
    h->cell = nullptr; // this copy reads unallocated; other copies keep the stale pointer
}

void dn2cpp_gchandle_free_value(Dn2CppGCHandle h)
{
    // Free called on an rvalue receiver: no storage to zero back into, but clearing
    // the shared cell still invalidates every other copy of the handle.
    if (h.cell == nullptr)
        dn2cpp_gchandle_throw_not_initialized();
    dn2cpp_gchandle_cell_release(h.cell);
}

intptr_t dn2cpp_gchandle_to_intptr(Dn2CppGCHandle h)
{
    // The cell address IS the handle value (real .NET returns the stable table slot):
    // stable across calls and collections, and — the cell being uncollectable AND
    // scanned — it roots the target even if this integer is the handle's only
    // surviving representation. 0 (never a throw — measured) for a zeroed handle.
    return reinterpret_cast<intptr_t>(h.cell);
}

Dn2CppGCHandle dn2cpp_gchandle_from_intptr(intptr_t v)
{
    if (v == 0)
        dn2cpp_gchandle_throw_not_initialized(); // measured: real .NET rejects 0
    return Dn2CppGCHandle{reinterpret_cast<Dn2CppGCHandleCell*>(v)};
}

