// dn2cpp_threading.cpp — threading primitives of the dn2cpp runtime:
// Parallel.For/ForEach/Invoke, SemaphoreSlim,
// ManualResetEvent(Slim) / AutoResetEvent, CountdownEvent / Barrier,
// ReaderWriterLockSlim, and System.Threading.Timer.

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

// ===== Parallel.For / ForEach / Invoke (real fan-out, deterministic join barrier) ====
// A data-parallel loop runs its body across up to hardware_concurrency OS threads, each
// taking a contiguous chunk of the index space, and BLOCKS until every chunk finishes — a
// deterministic join barrier (the call returns only once all iterations are done, so a
// gate that reads results after the call is fully deterministic vs real .NET). This is a
// dedicated per-call fan-out, deliberately separate from the Task.Run worker pool so it
// cannot interact with the async bridge. The first exception any iteration throws is
// captured and rethrown on the calling thread after the join (real .NET wraps body
// exceptions in an AggregateException — that wrapping is a follow-up; here the first
// propagates as-is).

// One-argument multicast-aware delegate invokes (Action<int>/<long>/<float>/<double>/<T>),
// mirroring dn2cpp_action_invoke but passing the per-element argument.
static void dn2cpp_action_invoke_i4(Dn2CppObject* del, int32_t arg)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_i4(dg->prev, arg);
    reinterpret_cast<void (*)(Dn2CppObject*, int32_t)>(dg->method)(dg->target, arg);
}
static void dn2cpp_action_invoke_i8(Dn2CppObject* del, int64_t arg)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_i8(dg->prev, arg);
    reinterpret_cast<void (*)(Dn2CppObject*, int64_t)>(dg->method)(dg->target, arg);
}
static void dn2cpp_action_invoke_r4(Dn2CppObject* del, float arg)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_r4(dg->prev, arg);
    reinterpret_cast<void (*)(Dn2CppObject*, float)>(dg->method)(dg->target, arg);
}
static void dn2cpp_action_invoke_r8(Dn2CppObject* del, double arg)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_r8(dg->prev, arg);
    reinterpret_cast<void (*)(Dn2CppObject*, double)>(dg->method)(dg->target, arg);
}
// (the reference-element form is dn2cpp_paramthread_invoke — same {target,method} shape)

// ---- ParallelLoopState (Break/Stop) ----
// One Dn2CppParallelLoopShared is allocated on the calling thread's stack for the
// whole duration of one Parallel.For/ForEach(..., Action<T, ParallelLoopState>) call
// (it outlives every worker thread, since dn2cpp_parallel_run blocks until they all
// join) and is shared by every iteration. Each iteration gets its own short-lived
// Dn2CppParallelLoopState wrapping a pointer to that shared block plus its OWN
// iteration index — needed for ShouldExitCurrentIteration's "index greater than the
// break point" rule, confirmed against real .NET: Break() lets iterations with index
// <= the break point keep running, only later ones stop being scheduled; Stop() halts
// scheduling unconditionally, with no index rule.
struct Dn2CppParallelLoopShared
{
    std::atomic<int8_t> stopped{ 0 };
    std::atomic<int8_t> broken{ 0 };
    std::atomic<int8_t> exceptional{ 0 };
    std::atomic<int64_t> lowestBreak{ INT64_MAX }; // INT64_MAX sentinel = no Break() yet
};
struct Dn2CppParallelLoopState : Dn2CppObject
{
    Dn2CppParallelLoopShared* shared;
    int64_t iteration;
};
extern const Dn2CppType dn2cpp_parallel_loop_state_type_obj;
const Dn2CppTypeInfo dn2cpp_parallel_loop_state_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Tasks.ParallelLoopState", nullptr, (int32_t)sizeof(Dn2CppParallelLoopState), nullptr, nullptr, 0 }, &dn2cpp_parallel_loop_state_type_obj);
const Dn2CppType dn2cpp_parallel_loop_state_type_obj = { { &dn2cpp_type_type }, &dn2cpp_parallel_loop_state_type };

static Dn2CppObject* dn2cpp_parallel_loop_state_new(Dn2CppParallelLoopShared* shared, int64_t iteration)
{
    auto* s = static_cast<Dn2CppParallelLoopState*>(dn2cpp_alloc(sizeof(Dn2CppParallelLoopState)));
    s->type = &dn2cpp_parallel_loop_state_type;
    s->shared = shared;
    s->iteration = iteration;
    return s;
}

// Stop()-after-Break() / Break()-after-Stop() are mutually exclusive for the lifetime
// of one loop call (verified against real .NET: the SECOND call throws, with the
// exact wording below; calling the SAME method twice — Break-after-Break,
// Stop-after-Stop — is idempotent and does not throw).
void dn2cpp_parallel_loop_state_break(Dn2CppObject* state)
{
    auto* s = reinterpret_cast<Dn2CppParallelLoopState*>(state);
    auto* sh = s->shared;
    if (sh->stopped.load(std::memory_order_acquire))
    {
        const char* msg = "Break was called after Stop was called.";
        dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
            dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
    }
    sh->broken.store(1, std::memory_order_release);
    int64_t cur = sh->lowestBreak.load(std::memory_order_relaxed);
    while (s->iteration < cur
        && !sh->lowestBreak.compare_exchange_weak(cur, s->iteration, std::memory_order_acq_rel, std::memory_order_relaxed))
    {
    }
}

void dn2cpp_parallel_loop_state_stop(Dn2CppObject* state)
{
    auto* s = reinterpret_cast<Dn2CppParallelLoopState*>(state);
    auto* sh = s->shared;
    if (sh->broken.load(std::memory_order_acquire))
    {
        const char* msg = "Stop was called after Break was called.";
        dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
            dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
    }
    sh->stopped.store(1, std::memory_order_release);
}

// ShouldExitCurrentIteration: true once Stop() has been called by anyone (no index
// rule), once an unhandled exception escaped any iteration of this loop, or once
// Break() has been called and THIS iteration's index is greater than the lowest
// break point (an iteration at or before the break point keeps running).
int32_t dn2cpp_parallel_loop_state_should_exit_current_iteration(Dn2CppObject* state)
{
    auto* s = reinterpret_cast<Dn2CppParallelLoopState*>(state);
    auto* sh = s->shared;
    if (sh->stopped.load(std::memory_order_acquire))
        return 1;
    if (sh->exceptional.load(std::memory_order_acquire))
        return 1;
    if (sh->broken.load(std::memory_order_acquire) && s->iteration > sh->lowestBreak.load(std::memory_order_acquire))
        return 1;
    return 0;
}

int32_t dn2cpp_parallel_loop_state_is_stopped(Dn2CppObject* state)
{
    return reinterpret_cast<Dn2CppParallelLoopState*>(state)->shared->stopped.load(std::memory_order_acquire);
}

int32_t dn2cpp_parallel_loop_state_is_exceptional(Dn2CppObject* state)
{
    return reinterpret_cast<Dn2CppParallelLoopState*>(state)->shared->exceptional.load(std::memory_order_acquire);
}

int32_t dn2cpp_parallel_loop_state_lowest_break_has_value(Dn2CppObject* state)
{
    return reinterpret_cast<Dn2CppParallelLoopState*>(state)->shared->broken.load(std::memory_order_acquire);
}

int64_t dn2cpp_parallel_loop_state_lowest_break_value(Dn2CppObject* state)
{
    return reinterpret_cast<Dn2CppParallelLoopState*>(state)->shared->lowestBreak.load(std::memory_order_acquire);
}

static Dn2CppParallelLoopResult dn2cpp_parallel_loop_result_from_shared(const Dn2CppParallelLoopShared* sh)
{
    bool stopped = sh->stopped.load(std::memory_order_acquire);
    bool broken = sh->broken.load(std::memory_order_acquire);
    Dn2CppParallelLoopResult r;
    r.isCompleted = (!stopped && !broken) ? 1 : 0;
    r.hasLowestBreak = broken ? 1 : 0;
    r.lowestBreakIteration = broken ? sh->lowestBreak.load(std::memory_order_acquire) : -1;
    return r;
}

// Two-argument multicast-aware delegate invokes for the Action<int|long|float|double,
// ParallelLoopState> body overloads, mirroring the one-argument invokes above but
// passing the per-iteration ParallelLoopState as a trailing Dn2CppObject* argument.
static void dn2cpp_action_invoke_i4_state(Dn2CppObject* del, int32_t arg, Dn2CppObject* state)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_i4_state(dg->prev, arg, state);
    reinterpret_cast<void (*)(Dn2CppObject*, int32_t, Dn2CppObject*)>(dg->method)(dg->target, arg, state);
}
static void dn2cpp_action_invoke_i8_state(Dn2CppObject* del, int64_t arg, Dn2CppObject* state)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_i8_state(dg->prev, arg, state);
    reinterpret_cast<void (*)(Dn2CppObject*, int64_t, Dn2CppObject*)>(dg->method)(dg->target, arg, state);
}
static void dn2cpp_action_invoke_r4_state(Dn2CppObject* del, float arg, Dn2CppObject* state)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_r4_state(dg->prev, arg, state);
    reinterpret_cast<void (*)(Dn2CppObject*, float, Dn2CppObject*)>(dg->method)(dg->target, arg, state);
}
static void dn2cpp_action_invoke_r8_state(Dn2CppObject* del, double arg, Dn2CppObject* state)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_action_invoke_r8_state(dg->prev, arg, state);
    reinterpret_cast<void (*)(Dn2CppObject*, double, Dn2CppObject*)>(dg->method)(dg->target, arg, state);
}
// Reference-element ForEach<T> with state: T arrives as a Dn2CppObject*, mirroring
// dn2cpp_paramthread_invoke but with the trailing ParallelLoopState argument.
void dn2cpp_paramthread_invoke_state(Dn2CppObject* del, Dn2CppObject* arg, Dn2CppObject* state)
{
    if (del == nullptr) return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr) dn2cpp_paramthread_invoke_state(dg->prev, arg, state);
    reinterpret_cast<void (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*)>(dg->method)(dg->target, arg, state);
}

// Run fn(ctx, i) for i in [0, count) across contiguous chunks on up to
// hardware_concurrency OS threads; block until all complete. The last chunk runs inline
// on the (already GC-registered) calling thread; spawned workers register themselves.
static void dn2cpp_parallel_run(int64_t count, void* ctx, void (*fn)(void*, int64_t), int32_t maxDop,
    Dn2CppParallelLoopShared* loopShared)
{
    if (count <= 0)
        return;
    unsigned hw = std::thread::hardware_concurrency();
    if (hw == 0)
        hw = 4;
    int64_t nthreads = static_cast<int64_t>(hw) < count ? static_cast<int64_t>(hw) : count;
    if (maxDop > 0 && static_cast<int64_t>(maxDop) < nthreads)
        nthreads = static_cast<int64_t>(maxDop);

    std::mutex excMtx;
    // One node per iteration that threw, across all workers, kept in ascending
    // iteration order so the aggregated InnerExceptions are deterministic despite
    // the non-deterministic parallel completion order. The nodes are GC-allocated
    // and chained from `excHead`, a local this (GC-registered) thread's stack roots
    // for the whole call — a malloc container would hide the exception objects from
    // the collector while later iterations (or the array allocation after the join)
    // can still trigger a collection. Each iteration runs under its own try so one
    // throw never skips later iterations (matching Parallel.Invoke, which runs
    // every action).
    struct ExcNode
    {
        int64_t index;
        Dn2CppObject* exc;
        ExcNode* next;
    };
    ExcNode* excHead = nullptr;
    int32_t excCount = 0;

    auto runChunk = [&](int64_t lo, int64_t hi) {
        for (int64_t i = lo; i < hi; i++)
        {
            // ParallelLoopState callers only: once Stop()/an unhandled exception has
            // been observed, stop starting further iterations outright; once Break()
            // has been observed, stop starting iterations past the lowest break point.
            // Both conditions are monotonic (stopped/exceptional only ever flip on,
            // lowestBreak only ever shrinks), so once true for index i it stays true
            // for every later i in this chunk — breaking out of the loop entirely is
            // safe, not just skipping this one iteration. The state-less entry points
            // (loopShared == nullptr) keep running every iteration, unchanged.
            if (loopShared != nullptr)
            {
                if (loopShared->stopped.load(std::memory_order_acquire)
                    || loopShared->exceptional.load(std::memory_order_acquire))
                    break;
                if (loopShared->broken.load(std::memory_order_acquire)
                    && i > loopShared->lowestBreak.load(std::memory_order_acquire))
                    break;
            }
            try
            {
                fn(ctx, i);
            }
            catch (const Dn2CppException& e)
            {
                // Copy the exception object out of the __cxa exception buffer (malloc,
                // never scanned) into this frame FIRST: the node allocation below can
                // itself trigger a collection, and at that point this local is the only
                // GC-visible reference.
                Dn2CppObject* excObj = e.obj;
                if (loopShared != nullptr)
                    loopShared->exceptional.store(1, std::memory_order_release);
                auto* node = static_cast<ExcNode*>(dn2cpp_alloc(sizeof(ExcNode)));
                node->index = i;
                node->exc = excObj;
                dn2cpp_exc_inflight_pop(excObj); // rooted via the node chain from here
                std::lock_guard<std::mutex> lk(excMtx);
                // Insert in ascending index order (indices are unique across chunks).
                ExcNode** pp = &excHead;
                while (*pp != nullptr && (*pp)->index < i)
                    pp = &(*pp)->next;
                dn2cpp_gc_store_ref(&node->next, *pp);
                *pp = node;
                dn2cpp_gc_write_barrier_if_heap(pp); // pp names a heap node or the stack head
                excCount++;
            }
        }
    };

    // Contiguous partition: the first `rem` chunks get one extra element.
    int64_t span = count / nthreads;
    int64_t rem = count % nthreads;
    std::vector<std::thread> workers;
    workers.reserve(static_cast<size_t>(nthreads - 1));
    int64_t cursor = 0, lastLo = 0, lastHi = 0;
    for (int64_t t = 0; t < nthreads; t++)
    {
        int64_t len = span + (t < rem ? 1 : 0);
        int64_t lo = cursor, hi = cursor + len;
        cursor = hi;
        if (t == nthreads - 1)
        {
            lastLo = lo; lastHi = hi; // final chunk runs inline below
        }
        else
        {
            workers.emplace_back([&runChunk, lo, hi] {
                Dn2CppGCThread guard; // a spawned worker must register with the GC
                runChunk(lo, hi);
            });
        }
    }
    runChunk(lastLo, lastHi);
    for (auto& w : workers)
        w.join();

    if (excHead != nullptr)
    {
        // The chain is already in iteration order; `excHead` stays live across the
        // array allocation (it is read below), keeping every exception rooted while
        // dn2cpp_newarr_ref may collect.
        Dn2CppArrayRef* inner = dn2cpp_newarr_ref(excCount);
        int32_t k = 0;
        for (ExcNode* n = excHead; n != nullptr; n = n->next)
            inner->data[k++] = n->exc;
        // Real .NET Parallel.* always wraps in an AggregateException — even a single
        // iteration's exception.
        dn2cpp_throw(dn2cpp_aggregate_exception_new(inner));
    }
}

void dn2cpp_parallel_for_i4(int32_t from, int32_t to, Dn2CppObject* body, int32_t maxDop)
{
    struct Ctx { Dn2CppObject* body; int32_t from; };
    Ctx c{ body, from };
    dn2cpp_parallel_run(static_cast<int64_t>(to) - static_cast<int64_t>(from), &c,
        [](void* p, int64_t k) {
            auto* c = static_cast<Ctx*>(p);
            dn2cpp_action_invoke_i4(c->body, c->from + static_cast<int32_t>(k));
        }, maxDop, nullptr);
}

void dn2cpp_parallel_for_i8(int64_t from, int64_t to, Dn2CppObject* body, int32_t maxDop)
{
    struct Ctx { Dn2CppObject* body; int64_t from; };
    Ctx c{ body, from };
    dn2cpp_parallel_run(to - from, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        dn2cpp_action_invoke_i8(c->body, c->from + k);
    }, maxDop, nullptr);
}

void dn2cpp_parallel_invoke(Dn2CppArrayRef* actions, int32_t maxDop)
{
    if (actions == nullptr)
        return;
    dn2cpp_parallel_run(actions->length, actions, [](void* p, int64_t k) {
        auto* a = static_cast<Dn2CppArrayRef*>(p);
        dn2cpp_action_invoke(a->data[k]);
    }, maxDop, nullptr);
}

void dn2cpp_parallel_foreach_ref_n(Dn2CppArrayRef* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    struct Ctx { Dn2CppArrayRef* src; Dn2CppObject* body; };
    Ctx c{ src, body };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        dn2cpp_paramthread_invoke(c->body, c->src->data[k]);
    }, maxDop, nullptr);
}

void dn2cpp_parallel_foreach_ref(Dn2CppArrayRef* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    dn2cpp_parallel_foreach_ref_n(src, src->length, body, maxDop);
}

void dn2cpp_parallel_foreach_i4_n(Dn2CppArrayI4* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    struct Ctx { Dn2CppArrayI4* src; Dn2CppObject* body; };
    Ctx c{ src, body };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        dn2cpp_action_invoke_i4(c->body, c->src->data[k]);
    }, maxDop, nullptr);
}

void dn2cpp_parallel_foreach_i4(Dn2CppArrayI4* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    dn2cpp_parallel_foreach_i4_n(src, src->length, body, maxDop);
}

void dn2cpp_parallel_foreach_i8_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    struct Ctx { Dn2CppArrayN* src; Dn2CppObject* body; };
    Ctx c{ src, body };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        dn2cpp_action_invoke_i8(c->body, reinterpret_cast<int64_t*>(c->src->data)[k]);
    }, maxDop, nullptr);
}

void dn2cpp_parallel_foreach_i8(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    dn2cpp_parallel_foreach_i8_n(src, src->length, body, maxDop);
}

void dn2cpp_parallel_foreach_r4_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    struct Ctx { Dn2CppArrayN* src; Dn2CppObject* body; };
    Ctx c{ src, body };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        dn2cpp_action_invoke_r4(c->body, reinterpret_cast<float*>(c->src->data)[k]);
    }, maxDop, nullptr);
}

void dn2cpp_parallel_foreach_r4(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    dn2cpp_parallel_foreach_r4_n(src, src->length, body, maxDop);
}

void dn2cpp_parallel_foreach_r8_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    struct Ctx { Dn2CppArrayN* src; Dn2CppObject* body; };
    Ctx c{ src, body };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        dn2cpp_action_invoke_r8(c->body, reinterpret_cast<double*>(c->src->data)[k]);
    }, maxDop, nullptr);
}

void dn2cpp_parallel_foreach_r8(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
        return;
    dn2cpp_parallel_foreach_r8_n(src, src->length, body, maxDop);
}

// ---- Action<int|long|T, ParallelLoopState> body forms ----
// Like the state-less entry points above, but a Dn2CppParallelLoopShared lives on this
// (calling) thread's stack for the whole call — safe because dn2cpp_parallel_run
// blocks until every worker has joined before returning — and each iteration gets its
// own freshly allocated ParallelLoopState wrapping it plus that iteration's index. For
// ForEach, "iteration index" is the source array position (0-based), not the element
// value: real .NET's LowestBreakIteration after Parallel.ForEach(int[], ...) reports
// the array position the Break()-calling element was found at, not its value
// (confirmed against real .NET).
Dn2CppParallelLoopResult dn2cpp_parallel_for_i4_state(int32_t from, int32_t to, Dn2CppObject* body, int32_t maxDop)
{
    Dn2CppParallelLoopShared shared;
    struct Ctx { Dn2CppObject* body; int32_t from; Dn2CppParallelLoopShared* shared; };
    Ctx c{ body, from, &shared };
    dn2cpp_parallel_run(static_cast<int64_t>(to) - static_cast<int64_t>(from), &c,
        [](void* p, int64_t k) {
            auto* c = static_cast<Ctx*>(p);
            int32_t i = c->from + static_cast<int32_t>(k);
            Dn2CppObject* state = dn2cpp_parallel_loop_state_new(c->shared, i);
            dn2cpp_action_invoke_i4_state(c->body, i, state);
        }, maxDop, &shared);
    return dn2cpp_parallel_loop_result_from_shared(&shared);
}

Dn2CppParallelLoopResult dn2cpp_parallel_for_i8_state(int64_t from, int64_t to, Dn2CppObject* body, int32_t maxDop)
{
    Dn2CppParallelLoopShared shared;
    struct Ctx { Dn2CppObject* body; int64_t from; Dn2CppParallelLoopShared* shared; };
    Ctx c{ body, from, &shared };
    dn2cpp_parallel_run(to - from, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        int64_t i = c->from + k;
        Dn2CppObject* state = dn2cpp_parallel_loop_state_new(c->shared, i);
        dn2cpp_action_invoke_i8_state(c->body, i, state);
    }, maxDop, &shared);
    return dn2cpp_parallel_loop_result_from_shared(&shared);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_ref_state_n(Dn2CppArrayRef* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    Dn2CppParallelLoopShared shared;
    if (src == nullptr)
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    struct Ctx { Dn2CppArrayRef* src; Dn2CppObject* body; Dn2CppParallelLoopShared* shared; };
    Ctx c{ src, body, &shared };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        Dn2CppObject* state = dn2cpp_parallel_loop_state_new(c->shared, k);
        dn2cpp_paramthread_invoke_state(c->body, c->src->data[k], state);
    }, maxDop, &shared);
    return dn2cpp_parallel_loop_result_from_shared(&shared);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_ref_state(Dn2CppArrayRef* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
    {
        Dn2CppParallelLoopShared shared;
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    }
    return dn2cpp_parallel_foreach_ref_state_n(src, src->length, body, maxDop);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i4_state_n(Dn2CppArrayI4* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    Dn2CppParallelLoopShared shared;
    if (src == nullptr)
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    struct Ctx { Dn2CppArrayI4* src; Dn2CppObject* body; Dn2CppParallelLoopShared* shared; };
    Ctx c{ src, body, &shared };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        Dn2CppObject* state = dn2cpp_parallel_loop_state_new(c->shared, k);
        dn2cpp_action_invoke_i4_state(c->body, c->src->data[k], state);
    }, maxDop, &shared);
    return dn2cpp_parallel_loop_result_from_shared(&shared);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i4_state(Dn2CppArrayI4* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
    {
        Dn2CppParallelLoopShared shared;
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    }
    return dn2cpp_parallel_foreach_i4_state_n(src, src->length, body, maxDop);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i8_state_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    Dn2CppParallelLoopShared shared;
    if (src == nullptr)
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    struct Ctx { Dn2CppArrayN* src; Dn2CppObject* body; Dn2CppParallelLoopShared* shared; };
    Ctx c{ src, body, &shared };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        Dn2CppObject* state = dn2cpp_parallel_loop_state_new(c->shared, k);
        dn2cpp_action_invoke_i8_state(c->body, reinterpret_cast<int64_t*>(c->src->data)[k], state);
    }, maxDop, &shared);
    return dn2cpp_parallel_loop_result_from_shared(&shared);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_i8_state(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
    {
        Dn2CppParallelLoopShared shared;
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    }
    return dn2cpp_parallel_foreach_i8_state_n(src, src->length, body, maxDop);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r4_state_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    Dn2CppParallelLoopShared shared;
    if (src == nullptr)
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    struct Ctx { Dn2CppArrayN* src; Dn2CppObject* body; Dn2CppParallelLoopShared* shared; };
    Ctx c{ src, body, &shared };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        Dn2CppObject* state = dn2cpp_parallel_loop_state_new(c->shared, k);
        dn2cpp_action_invoke_r4_state(c->body, reinterpret_cast<float*>(c->src->data)[k], state);
    }, maxDop, &shared);
    return dn2cpp_parallel_loop_result_from_shared(&shared);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r4_state(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
    {
        Dn2CppParallelLoopShared shared;
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    }
    return dn2cpp_parallel_foreach_r4_state_n(src, src->length, body, maxDop);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r8_state_n(Dn2CppArrayN* src, int32_t n, Dn2CppObject* body, int32_t maxDop)
{
    Dn2CppParallelLoopShared shared;
    if (src == nullptr)
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    struct Ctx { Dn2CppArrayN* src; Dn2CppObject* body; Dn2CppParallelLoopShared* shared; };
    Ctx c{ src, body, &shared };
    dn2cpp_parallel_run(n, &c, [](void* p, int64_t k) {
        auto* c = static_cast<Ctx*>(p);
        Dn2CppObject* state = dn2cpp_parallel_loop_state_new(c->shared, k);
        dn2cpp_action_invoke_r8_state(c->body, reinterpret_cast<double*>(c->src->data)[k], state);
    }, maxDop, &shared);
    return dn2cpp_parallel_loop_result_from_shared(&shared);
}

Dn2CppParallelLoopResult dn2cpp_parallel_foreach_r8_state(Dn2CppArrayN* src, Dn2CppObject* body, int32_t maxDop)
{
    if (src == nullptr)
    {
        Dn2CppParallelLoopShared shared;
        return dn2cpp_parallel_loop_result_from_shared(&shared);
    }
    return dn2cpp_parallel_foreach_r8_state_n(src, src->length, body, maxDop);
}

// System.Threading.Tasks.ParallelOptions — a single int field, GC-allocated for a
// uniform managed object header (no mutex; never mutated concurrently with a
// Parallel.* call reading it).
extern const Dn2CppType dn2cpp_parallel_options_type_obj;
const Dn2CppTypeInfo dn2cpp_parallel_options_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Tasks.ParallelOptions", nullptr, (int32_t)sizeof(Dn2CppParallelOptions), nullptr, nullptr, 0 }, &dn2cpp_parallel_options_type_obj);
const Dn2CppType dn2cpp_parallel_options_type_obj = { { &dn2cpp_type_type }, &dn2cpp_parallel_options_type };

Dn2CppObject* dn2cpp_parallel_options_new()
{
    auto* o = static_cast<Dn2CppParallelOptions*>(dn2cpp_alloc(sizeof(Dn2CppParallelOptions)));
    o->type = &dn2cpp_parallel_options_type;
    o->maxDop = -1;
    return o;
}

// ===== SemaphoreSlim / ManualResetEvent(Slim) / AutoResetEvent ===============
// Native-allocated (so the std::mutex/condition_variable members get real ctors) with a
// managed type header. They live for the program (a small, bounded leak, like monitors);
// a managed field holding one is simply ignored by the conservative GC (not in its heap).
extern const Dn2CppType dn2cpp_semaphore_type_obj;
// NO_SHALLOW_CLONE: Dn2CppSemaphore is `new`-allocated on the NATIVE heap and embeds
// a std::mutex + std::condition_variable (same for the event/countdown/barrier/rwlock
// handles below). See the flag's definition in dn2cpp_core.h for why the bit and not
// a missing extent.
const Dn2CppTypeInfo dn2cpp_semaphore_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.SemaphoreSlim", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_semaphore_type_obj);
const Dn2CppType dn2cpp_semaphore_type_obj = { { &dn2cpp_type_type }, &dn2cpp_semaphore_type };
// The event family is FOUR CLR types over one Dn2CppEvent, and — unlike the Task and
// ThreadLocal families, which share one handle — they are not instantiations of one
// type: ManualResetEvent and AutoResetEvent are sealed siblings deriving
// EventWaitHandle, which derives WaitHandle, while ManualResetEventSlim derives Object.
// One shared handle would make `mre is AutoResetEvent` and `mres is WaitHandle` read
// True where .NET says False. The family is a fixed set of named types rather than an
// open set of instantiations, so four handles on the real base chain answer exactly;
// the ctor lowering knows which type it constructs and stamps it.
// WaitHandle's one public field, real .NET's whole surface for it (the hand-writing
// argument is at dn2cpp_primflds_bool in dn2cpp_typeinfo.cpp). The other four handles
// declare none, so their absent tables are already right.
static Dn2CppObject* dn2cpp_ownfld_waithandle_WaitTimeout(Dn2CppObject*)
{ int32_t v = 258; return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_ownflds_waithandle[] = {
    { "WaitTimeout", &dn2cpp_waithandle_type, &dn2cpp_int32_type,
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_ownfld_waithandle_WaitTimeout, nullptr, nullptr, 0, 0x8056, 0 },
};
extern const Dn2CppType dn2cpp_waithandle_type_obj;
const Dn2CppTypeInfo dn2cpp_waithandle_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.WaitHandle", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE, dn2cpp_ownflds_waithandle, 1 }, &dn2cpp_waithandle_type_obj);
const Dn2CppType dn2cpp_waithandle_type_obj = { { &dn2cpp_type_type }, &dn2cpp_waithandle_type };
extern const Dn2CppType dn2cpp_event_type_obj;
const Dn2CppTypeInfo dn2cpp_event_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.EventWaitHandle", &dn2cpp_waithandle_type, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_event_type_obj);
const Dn2CppType dn2cpp_event_type_obj = { { &dn2cpp_type_type }, &dn2cpp_event_type };
extern const Dn2CppType dn2cpp_manualresetevent_type_obj;
const Dn2CppTypeInfo dn2cpp_manualresetevent_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.ManualResetEvent", &dn2cpp_event_type, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_manualresetevent_type_obj);
const Dn2CppType dn2cpp_manualresetevent_type_obj = { { &dn2cpp_type_type }, &dn2cpp_manualresetevent_type };
extern const Dn2CppType dn2cpp_autoresetevent_type_obj;
const Dn2CppTypeInfo dn2cpp_autoresetevent_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.AutoResetEvent", &dn2cpp_event_type, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_autoresetevent_type_obj);
const Dn2CppType dn2cpp_autoresetevent_type_obj = { { &dn2cpp_type_type }, &dn2cpp_autoresetevent_type };
// Base nullptr — i.e. System.Object. ManualResetEventSlim is a lightweight
// non-WaitHandle type in real .NET, and stamping it under the event chain is exactly the
// over-accept this arrangement exists to avoid.
extern const Dn2CppType dn2cpp_manualreseteventslim_type_obj;
const Dn2CppTypeInfo dn2cpp_manualreseteventslim_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.ManualResetEventSlim", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_manualreseteventslim_type_obj);
const Dn2CppType dn2cpp_manualreseteventslim_type_obj = { { &dn2cpp_type_type }, &dn2cpp_manualreseteventslim_type };
extern const Dn2CppType dn2cpp_safewaithandle_type_obj;
const Dn2CppTypeInfo dn2cpp_safewaithandle_type =
    dn2cpp_ti_with_typeobject({ "Microsoft.Win32.SafeHandles.SafeWaitHandle", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_safewaithandle_type_obj);
const Dn2CppType dn2cpp_safewaithandle_type_obj = { { &dn2cpp_type_type }, &dn2cpp_safewaithandle_type };

struct Dn2CppSemaphore : Dn2CppObject
{
    std::mutex m;
    std::condition_variable cv;
    int count;
    int maxCount;
};

struct Dn2CppEvent : Dn2CppObject
{
    std::mutex m;
    std::condition_variable cv;
    bool signaled;
    bool manualReset;
};

struct Dn2CppSafeWaitHandle : Dn2CppObject
{
    intptr_t handle;
    bool ownsHandle;
    bool managedEvent;
    bool closed;
};

static std::mutex g_wait_handle_mutex;
static std::unordered_map<Dn2CppObject*, Dn2CppObject*> g_wait_safe_handles;

Dn2CppObject* dn2cpp_semaphore_new(int32_t initial, int32_t maxCount)
{
    auto* s = new Dn2CppSemaphore();
    s->type = &dn2cpp_semaphore_type;
    s->count = initial;
    s->maxCount = maxCount;
    return s;
}

void dn2cpp_semaphore_wait(Dn2CppObject* o)
{
    auto* s = static_cast<Dn2CppSemaphore*>(o);
    std::unique_lock<std::mutex> lk(s->m);
    s->cv.wait(lk, [s] { return s->count > 0; });
    s->count--;
}

// SemaphoreSlim.Wait(int)/Wait(TimeSpan): wait up to ms for a token. Returns 1 if a token
// was taken, 0 on timeout. ms == 0 is a single immediate check (wait_for(0)); a negative
// ms (Timeout.Infinite) is an infinite wait.
int32_t dn2cpp_semaphore_wait_timeout(Dn2CppObject* o, int32_t ms)
{
    if (ms < 0)
    {
        dn2cpp_semaphore_wait(o);
        return 1;
    }
    auto* s = static_cast<Dn2CppSemaphore*>(o);
    std::unique_lock<std::mutex> lk(s->m);
    if (!s->cv.wait_for(lk, std::chrono::milliseconds(ms), [s] { return s->count > 0; }))
        return 0;
    s->count--;
    return 1;
}

int32_t dn2cpp_semaphore_release(Dn2CppObject* o, int32_t count)
{
    auto* s = static_cast<Dn2CppSemaphore*>(o);
    int prev;
    {
        std::lock_guard<std::mutex> lk(s->m);
        prev = s->count;
        s->count += count;
    }
    for (int32_t i = 0; i < count; i++)
        s->cv.notify_one();
    return prev; // SemaphoreSlim.Release returns the count before the release
}

int32_t dn2cpp_semaphore_count(Dn2CppObject* o)
{
    auto* s = static_cast<Dn2CppSemaphore*>(o);
    std::lock_guard<std::mutex> lk(s->m);
    return s->count;
}

// SemaphoreSlim.WaitAsync([CancellationToken]) -> Task. An immediately available
// token completes synchronously with a pre-completed task — the common case for
// BufferedFileStreamStrategy's per-stream I/O serialization semaphore, whose
// callers await the acquire back-to-back. A contended acquire escapes the real
// blocking wait to the worker pool (dn2cpp_pool_submit, the Task.Run machinery):
// the returned task completes when a Release hands over a token, and the pool
// item's g_inflight_async_tasks count keeps an awaiting task_block asleep rather
// than deadlock-failing. FIFO fairness across concurrent pending waiters is not
// modeled (whichever blocked worker wakes first wins), matching the
// correctness-first minimal model.
static uint64_t dn2cpp_run_semaphore_wait(Dn2CppObject* sem)
{
    dn2cpp_semaphore_wait(sem);
    return 0;
}

Dn2CppTask* dn2cpp_semaphore_wait_async(Dn2CppObject* o)
{
    auto* s = static_cast<Dn2CppSemaphore*>(o);
    {
        std::lock_guard<std::mutex> lk(s->m);
        if (s->count > 0)
        {
            s->count--;
            return dn2cpp_task_completed();
        }
    }
    return dn2cpp_pool_submit(o, &dn2cpp_run_semaphore_wait);
}

// `ti` is the CONSTRUCTED type's handle, supplied by the newobj lowering; null
// degrades to EventWaitHandle, which is what every event carried before. The reset mode
// and the type are two different facts — an EventWaitHandle(false, ManualReset) is
// manual-reset AND is not a ManualResetEvent — so `manualReset` cannot stand in for it.
Dn2CppObject* dn2cpp_event_new(int32_t initial, int32_t manualReset, const Dn2CppTypeInfo* ti)
{
    auto* e = new Dn2CppEvent();
    e->type = ti != nullptr ? ti : &dn2cpp_event_type;
    e->signaled = initial != 0;
    e->manualReset = manualReset != 0;
    return e;
}

static Dn2CppSafeWaitHandle* dn2cpp_as_safe_handle(Dn2CppObject* o)
{
    if (o == nullptr)
        dn2cpp_throw_null_reference();
    return static_cast<Dn2CppSafeWaitHandle*>(o);
}

Dn2CppObject* dn2cpp_safewaithandle_new(intptr_t handle, int32_t ownsHandle)
{
    auto* safe = new Dn2CppSafeWaitHandle();
    safe->type = &dn2cpp_safewaithandle_type;
    safe->handle = handle;
    safe->ownsHandle = ownsHandle != 0;
    safe->managedEvent = false;
    safe->closed = false;
    return safe;
}

Dn2CppObject* dn2cpp_waithandle_get_safe(Dn2CppObject* waitHandle)
{
    if (waitHandle == nullptr)
        dn2cpp_throw_null_reference();
    std::lock_guard<std::mutex> lk(g_wait_handle_mutex);
    auto it = g_wait_safe_handles.find(waitHandle);
    if (it == g_wait_safe_handles.end())
    {
        auto* safe = static_cast<Dn2CppSafeWaitHandle*>(
            dn2cpp_safewaithandle_new(reinterpret_cast<intptr_t>(waitHandle), 0));
        safe->managedEvent = true;
        it = g_wait_safe_handles.emplace(waitHandle, safe).first;
    }
    return it->second;
}

void dn2cpp_waithandle_set_safe(Dn2CppObject* waitHandle, Dn2CppObject* safeHandle)
{
    if (waitHandle == nullptr)
        dn2cpp_throw_null_reference();
    std::lock_guard<std::mutex> lk(g_wait_handle_mutex);
    g_wait_safe_handles[waitHandle] = safeHandle;
}

intptr_t dn2cpp_safewaithandle_get(Dn2CppObject* safeHandle)
{
    return dn2cpp_as_safe_handle(safeHandle)->handle;
}

int32_t dn2cpp_safewaithandle_is_invalid(Dn2CppObject* safeHandle)
{
    if (safeHandle == nullptr)
        return 1;
    auto* safe = static_cast<Dn2CppSafeWaitHandle*>(safeHandle);
    return safe->handle == 0 || safe->handle == static_cast<intptr_t>(-1) ? 1 : 0;
}

int32_t dn2cpp_safewaithandle_is_closed(Dn2CppObject* safeHandle)
{
    return dn2cpp_as_safe_handle(safeHandle)->closed ? 1 : 0;
}

void dn2cpp_safewaithandle_close(Dn2CppObject* safeHandle)
{
    auto* safe = dn2cpp_as_safe_handle(safeHandle);
    if (safe->closed)
        return;
#ifdef _WIN32
    if (safe->ownsHandle && !safe->managedEvent && safe->handle != 0
        && safe->handle != static_cast<intptr_t>(-1))
        ::CloseHandle(reinterpret_cast<HANDLE>(safe->handle));
#endif
    safe->closed = true;
    safe->handle = 0;
}

static Dn2CppEvent* dn2cpp_event_from_operand(Dn2CppObject* o,
    Dn2CppSafeWaitHandle** external)
{
    if (o == nullptr)
        dn2cpp_throw_null_reference();
    if (o->type == &dn2cpp_safewaithandle_type)
    {
        auto* safe = static_cast<Dn2CppSafeWaitHandle*>(o);
        if (safe->managedEvent)
            return reinterpret_cast<Dn2CppEvent*>(safe->handle);
        *external = safe;
        return nullptr;
    }
    {
        std::lock_guard<std::mutex> lk(g_wait_handle_mutex);
        auto it = g_wait_safe_handles.find(o);
        if (it != g_wait_safe_handles.end())
        {
            auto* safe = static_cast<Dn2CppSafeWaitHandle*>(it->second);
            if (safe->managedEvent)
                return reinterpret_cast<Dn2CppEvent*>(safe->handle);
            *external = safe;
            return nullptr;
        }
    }
    if (o->type == &dn2cpp_waithandle_type)
    {
        dn2cpp_throw_invalid_operation();
    }
    return static_cast<Dn2CppEvent*>(o);
}

static int32_t dn2cpp_external_wait(Dn2CppSafeWaitHandle* safe, int32_t ms)
{
#ifdef _WIN32
    DWORD timeout = ms < 0 ? INFINITE : static_cast<DWORD>(ms);
    DWORD result = ::WaitForSingleObject(reinterpret_cast<HANDLE>(safe->handle), timeout);
    if (result == WAIT_OBJECT_0)
        return 1;
    if (result == WAIT_TIMEOUT)
        return 0;
    dn2cpp_throw_invalid_operation();
#else
    (void)safe;
    (void)ms;
    dn2cpp_throw_platform_not_supported("OS-backed WaitHandle is only available on Windows");
#endif
}

void dn2cpp_event_set(Dn2CppObject* o)
{
    Dn2CppSafeWaitHandle* external = nullptr;
    auto* e = dn2cpp_event_from_operand(o, &external);
    if (external != nullptr)
    {
#ifdef _WIN32
        if (::SetEvent(reinterpret_cast<HANDLE>(external->handle)) == 0)
            dn2cpp_throw_invalid_operation();
        return;
#else
        dn2cpp_throw_platform_not_supported("OS-backed WaitHandle is only available on Windows");
#endif
    }
    {
        std::lock_guard<std::mutex> lk(e->m);
        e->signaled = true;
    }
    // A manual-reset event releases all waiters; an auto-reset event releases one.
    if (e->manualReset)
        e->cv.notify_all();
    else
        e->cv.notify_one();
}

void dn2cpp_event_reset(Dn2CppObject* o)
{
    Dn2CppSafeWaitHandle* external = nullptr;
    auto* e = dn2cpp_event_from_operand(o, &external);
    if (external != nullptr)
    {
#ifdef _WIN32
        if (::ResetEvent(reinterpret_cast<HANDLE>(external->handle)) == 0)
            dn2cpp_throw_invalid_operation();
        return;
#else
        dn2cpp_throw_platform_not_supported("OS-backed WaitHandle is only available on Windows");
#endif
    }
    std::lock_guard<std::mutex> lk(e->m);
    e->signaled = false;
}

int32_t dn2cpp_event_wait(Dn2CppObject* o)
{
    Dn2CppSafeWaitHandle* external = nullptr;
    auto* e = dn2cpp_event_from_operand(o, &external);
    if (external != nullptr)
        return dn2cpp_external_wait(external, -1);
    std::unique_lock<std::mutex> lk(e->m);
    e->cv.wait(lk, [e] { return e->signaled; });
    if (!e->manualReset)
        e->signaled = false; // auto-reset consumes the signal on release
    return 1;                // WaitOne()/Wait() report success (signaled)
}

// WaitOne(int)/Wait(int) (and the TimeSpan forms): wait up to ms for the signal. Returns 1
// if signaled (applying the auto-reset consume), 0 on timeout. ms == 0 is a single
// immediate check; a negative ms (Timeout.Infinite) is an infinite wait.
int32_t dn2cpp_event_wait_timeout(Dn2CppObject* o, int32_t ms)
{
    if (ms < 0)
        return dn2cpp_event_wait(o);
    Dn2CppSafeWaitHandle* external = nullptr;
    auto* e = dn2cpp_event_from_operand(o, &external);
    if (external != nullptr)
        return dn2cpp_external_wait(external, ms);
    std::unique_lock<std::mutex> lk(e->m);
    if (!e->cv.wait_for(lk, std::chrono::milliseconds(ms), [e] { return e->signaled; }))
        return 0;
    if (!e->manualReset)
        e->signaled = false;
    return 1;
}

int32_t dn2cpp_event_is_set(Dn2CppObject* o)
{
    auto* e = static_cast<Dn2CppEvent*>(o);
    std::lock_guard<std::mutex> lk(e->m);
    return e->signaled ? 1 : 0;
}

// WaitHandle.WaitAny(WaitHandle[]): block until ANY handle is signaled, returning its
// index. Each handle is a native Dn2CppEvent (ManualResetEvent/AutoResetEvent, both
// derive WaitHandle). A round-robin non-blocking scan checks each event's signaled flag —
// consuming it for an auto-reset event, exactly like a single dn2cpp_event_wait — and
// returns the first ready index; when none is ready it sleeps briefly and retries (a
// polling wait: there is no shared condition variable to block on across the whole set).
// Argument checks match real .NET: a null array or a null element throws
// ArgumentNullException, an empty array throws ArgumentException. The timed
// WaitAny(..., int/TimeSpan) overloads stay unmapped.
int32_t dn2cpp_event_wait_any(Dn2CppArrayRef* handles)
{
    if (handles == nullptr)
        dn2cpp_throw_argument_null();
    if (handles->length == 0)
        dn2cpp_throw_argument();
    for (int32_t i = 0; i < handles->length; i++)
        if (handles->data[i] == nullptr)
            dn2cpp_throw_argument_null();
    for (;;)
    {
        for (int32_t i = 0; i < handles->length; i++)
        {
            auto* e = static_cast<Dn2CppEvent*>(handles->data[i]);
            std::lock_guard<std::mutex> lk(e->m);
            if (e->signaled)
            {
                if (!e->manualReset)
                    e->signaled = false; // auto-reset consumes the signal, like dn2cpp_event_wait
                return i;
            }
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(1));
    }
}

// ===== CountdownEvent / Barrier =============================================
// Counter-based synchronization primitives, modeled like the semaphore/event above,
// with a managed type header, built at newobj (the real handle-allocating ctors are
// skipped). They live for the program (a small, bounded leak, like monitors).
// CountdownEvent holds only ints, so it is native-allocated; Barrier holds a managed
// reference (postPhase), so it must be GC-allocated like Timer — the collector never
// scans native-heap words — with placement new constructing the mutex/cv in place.
// Both are non-const and externally visible for the same reason dn2cpp_timer_type is:
// the generated init prologue installs their IDisposable dispatch row, whose
// entry names program-specific emitted type-info the runtime cannot spell.
extern const Dn2CppType dn2cpp_countdown_type_obj;
Dn2CppTypeInfo dn2cpp_countdown_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.CountdownEvent", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_countdown_type_obj);
const Dn2CppType dn2cpp_countdown_type_obj = { { &dn2cpp_type_type }, &dn2cpp_countdown_type };
extern const Dn2CppType dn2cpp_barrier_type_obj;
Dn2CppTypeInfo dn2cpp_barrier_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Barrier", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_barrier_type_obj);
const Dn2CppType dn2cpp_barrier_type_obj = { { &dn2cpp_type_type }, &dn2cpp_barrier_type };

struct Dn2CppCountdown : Dn2CppObject
{
    std::mutex m;
    std::condition_variable cv;
    int count;
    int initial;
};

Dn2CppObject* dn2cpp_countdown_new(int32_t initialCount)
{
    auto* c = new Dn2CppCountdown();
    c->type = &dn2cpp_countdown_type;
    c->count = initialCount;
    c->initial = initialCount;
    return c; // initialCount == 0 starts already set (count already zero)
}

// CountdownEvent.Signal(n): decrement the count by n; release all waiters when it hits
// zero. Returns 1 if this signal drove the count to zero, else 0. Real .NET throws if a
// signal would drop the count below zero (or signals an already-zero event); here the
// count is clamped at zero instead.
int32_t dn2cpp_countdown_signal(Dn2CppObject* o, int32_t n)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    c->count -= n;
    if (c->count < 0)
        c->count = 0; // clamp (real .NET throws on signal-below-zero)
    bool zero = c->count == 0;
    if (zero)
        c->cv.notify_all();
    return zero ? 1 : 0;
}

// CountdownEvent.AddCount(n): increment the count. Real .NET throws if the event is
// already set; here it is added unconditionally.
void dn2cpp_countdown_add(Dn2CppObject* o, int32_t n)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    c->count += n;
}

// CountdownEvent.TryAddCount(n): add n only if the event is not already set (count > 0).
// Returns 1 if added, 0 if the event was already signaled.
int32_t dn2cpp_countdown_try_add(Dn2CppObject* o, int32_t n)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    if (c->count <= 0)
        return 0;
    c->count += n;
    return 1;
}

// CountdownEvent.Reset(n): reset both the current and initial count to n.
void dn2cpp_countdown_reset(Dn2CppObject* o, int32_t n)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    c->count = n;
    c->initial = n;
}

// CountdownEvent.Reset(): reset the current count back to the initial count.
void dn2cpp_countdown_reset_default(Dn2CppObject* o)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    c->count = c->initial;
}

void dn2cpp_countdown_wait(Dn2CppObject* o)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::unique_lock<std::mutex> lk(c->m);
    c->cv.wait(lk, [c] { return c->count == 0; });
}

// CountdownEvent.Wait(int/TimeSpan): wait up to ms for the count to reach zero. Returns 1
// if signaled, 0 on timeout. ms == 0 is a single immediate check; a negative ms
// (Timeout.Infinite) is an infinite wait.
int32_t dn2cpp_countdown_wait_timeout(Dn2CppObject* o, int32_t ms)
{
    if (ms < 0)
    {
        dn2cpp_countdown_wait(o);
        return 1;
    }
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::unique_lock<std::mutex> lk(c->m);
    if (!c->cv.wait_for(lk, std::chrono::milliseconds(ms), [c] { return c->count == 0; }))
        return 0;
    return 1;
}

int32_t dn2cpp_countdown_current(Dn2CppObject* o)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    return c->count;
}

int32_t dn2cpp_countdown_initial(Dn2CppObject* o)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    return c->initial;
}

int32_t dn2cpp_countdown_is_set(Dn2CppObject* o)
{
    auto* c = static_cast<Dn2CppCountdown*>(o);
    std::lock_guard<std::mutex> lk(c->m);
    return c->count == 0 ? 1 : 0;
}

struct Dn2CppBarrier : Dn2CppObject
{
    std::mutex m;
    std::condition_variable cv;
    int total;
    int waiting;
    int64_t phase;
    Dn2CppObject* postPhase;
};

Dn2CppObject* dn2cpp_barrier_new(int32_t participantCount, Dn2CppObject* postPhaseAction)
{
    auto* raw = dn2cpp_alloc(sizeof(Dn2CppBarrier)); // GC-allocated: postPhase must be scanned
    auto* b = new (raw) Dn2CppBarrier();
    b->type = &dn2cpp_barrier_type;
    b->total = participantCount;
    b->waiting = 0;
    b->phase = 0;
    dn2cpp_gc_store_ref(&b->postPhase, postPhaseAction);
    return b;
}

// Invoke the post-phase Action<Barrier> (and its multicast chain), passing the barrier as
// the single argument, mirroring dn2cpp_paramthread_invoke. Run by the last arriving thread
// while it holds the barrier mutex (so the action must NOT re-enter the same barrier).
static void dn2cpp_barrier_run_post(Dn2CppObject* del, Dn2CppObject* barrier)
{
    if (del == nullptr)
        return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr)
        dn2cpp_barrier_run_post(dg->prev, barrier);
    reinterpret_cast<void (*)(Dn2CppObject*, Dn2CppObject*)>(dg->method)(dg->target, barrier);
}

// Barrier.SignalAndWait(): the calling thread arrives at the current phase and blocks until
// every participant has arrived. The last arriver runs the post-phase action, advances the
// phase, and releases the rest. Always returns 1.
int32_t dn2cpp_barrier_signal_and_wait(Dn2CppObject* o)
{
    auto* b = static_cast<Dn2CppBarrier*>(o);
    std::unique_lock<std::mutex> lk(b->m);
    int64_t myPhase = b->phase;
    b->waiting++;
    if (b->waiting == b->total)
    {
        dn2cpp_barrier_run_post(b->postPhase, o); // last arriver, lock held
        b->phase++;
        b->waiting = 0;
        b->cv.notify_all();
    }
    else
    {
        b->cv.wait(lk, [b, myPhase] { return b->phase != myPhase; });
    }
    return 1;
}

// Barrier.SignalAndWait(int/TimeSpan): like the blocking form, but a non-last arriver waits
// at most ms. On timeout it rolls back its own signal (waiting--) and returns 0, matching
// .NET (the timed-out signal does not count). ms < 0 (Timeout.Infinite) blocks forever.
int32_t dn2cpp_barrier_signal_and_wait_timeout(Dn2CppObject* o, int32_t ms)
{
    if (ms < 0)
        return dn2cpp_barrier_signal_and_wait(o);
    auto* b = static_cast<Dn2CppBarrier*>(o);
    std::unique_lock<std::mutex> lk(b->m);
    int64_t myPhase = b->phase;
    b->waiting++;
    if (b->waiting == b->total)
    {
        dn2cpp_barrier_run_post(b->postPhase, o); // last arriver, lock held
        b->phase++;
        b->waiting = 0;
        b->cv.notify_all();
        return 1;
    }
    if (!b->cv.wait_for(lk, std::chrono::milliseconds(ms), [b, myPhase] { return b->phase != myPhase; }))
    {
        b->waiting--; // roll back the signal on timeout
        return 0;
    }
    return 1;
}

// Barrier.AddParticipant(s): grow the participant count; returns the phase the new
// participants first take part in (the current phase).
int64_t dn2cpp_barrier_add(Dn2CppObject* o, int32_t n)
{
    auto* b = static_cast<Dn2CppBarrier*>(o);
    std::lock_guard<std::mutex> lk(b->m);
    b->total += n;
    return b->phase;
}

// Barrier.RemoveParticipant(s): shrink the participant count. Lowering the count can satisfy
// the current phase for the threads already waiting, so complete it if so.
void dn2cpp_barrier_remove(Dn2CppObject* o, int32_t n)
{
    auto* b = static_cast<Dn2CppBarrier*>(o);
    std::lock_guard<std::mutex> lk(b->m);
    b->total -= n;
    if (b->total > 0 && b->waiting >= b->total)
    {
        dn2cpp_barrier_run_post(b->postPhase, o);
        b->phase++;
        b->waiting = 0;
        b->cv.notify_all();
    }
}

int32_t dn2cpp_barrier_participant_count(Dn2CppObject* o)
{
    auto* b = static_cast<Dn2CppBarrier*>(o);
    std::lock_guard<std::mutex> lk(b->m);
    return b->total;
}

int32_t dn2cpp_barrier_participants_remaining(Dn2CppObject* o)
{
    auto* b = static_cast<Dn2CppBarrier*>(o);
    std::lock_guard<std::mutex> lk(b->m);
    return b->total - b->waiting;
}

int64_t dn2cpp_barrier_current_phase(Dn2CppObject* o)
{
    auto* b = static_cast<Dn2CppBarrier*>(o);
    std::lock_guard<std::mutex> lk(b->m);
    return b->phase;
}

// ===== ReaderWriterLockSlim ==================================================
// A reader-writer lock on real OS threads: many concurrent readers XOR one writer.
// Modeled like the semaphore/event/countdown/barrier primitives above — native-allocated
// (so the std::mutex/condition_variable members get real ctors) with a managed type header,
// built at newobj (the real handle-allocating ctor is skipped). It lives for the program (a
// small, bounded leak, like monitors).
//
// Writer preference avoids writer starvation: a new reader blocks while a writer is active
// OR any writer is waiting. An upgradeable-read lock (at most one at a time) is tracked by
// its own flag plus its owner thread; it coexists with plain readers but blocks writers
// (a foreign writer waits for readers == 0 AND no upgradeable holder) and excludes other
// upgradeable holders. The upgradeable holder itself may enter the write lock (the .NET
// upgrade path): it waits only for the plain readers to drain, holds both states while
// upgraded, and drops back to upgradeable-read on ExitWriteLock.
//
// Per-thread ownership IS tracked: without it a same-thread re-entry is
// indistinguishable from contention and the lock waits on itself — a hang with no
// output, which real programs reach (GodotSharp's script-load path re-enters its
// upgradeable lock). Every Enter/TryEnter therefore applies real .NET's recursion
// matrix FIRST:
//   NoRecursion       — read->read, write->write, upg->upg, read->write, read->upg,
//                       write->read, write->upg throw LockRecursionException;
//                       upg->read and upg->write (the upgrade) are granted.
//   SupportsRecursion — same-mode re-entries count up; write->read, write->upg,
//                       upg->read, upg->write are granted; read->write and read->upg
//                       still throw (the deadlock-prone shapes .NET refuses even here).
// A granted re-entry never waits (it already holds access — waiting would deadlock
// against a waiting foreign writer), and TryEnter* throws the same way its Enter does
// (measured: the recursion verdict precedes the timeout in .NET). Exit* by a thread
// that does not hold the lock throws SynchronizationLockException instead of corrupting
// the counts. The messages are real .NET's, asserted verbatim by the SyncPrimitives
// gate's RwLockRecursion section against a live oracle.
// Non-const and externally visible: the init prologue installs its IDisposable row.
// ReaderWriterLockSlim.Dispose is already a no-op at the intrinsic call site — the
// interface mouth is what needs the row.
extern const Dn2CppType dn2cpp_rwlock_type_obj;
Dn2CppTypeInfo dn2cpp_rwlock_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.ReaderWriterLockSlim", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_rwlock_type_obj);
const Dn2CppType dn2cpp_rwlock_type_obj = { { &dn2cpp_type_type }, &dn2cpp_rwlock_type };

struct Dn2CppRwReadHold
{
    std::thread::id owner;
    int count;                           // > 1 only under SupportsRecursion
};

struct Dn2CppRwLock : Dn2CppObject
{
    std::mutex m;
    std::condition_variable readersCv;   // waiting readers + upgradeable acquirers
    std::condition_variable writersCv;   // waiting writers
    std::vector<Dn2CppRwReadHold> readHolds; // per-thread read holds (size == reader threads)
    bool writer;                         // a write lock is held
    int writeCount;                      // its recursion depth (SupportsRecursion)
    std::thread::id writeOwner;          // the write holder (valid while `writer`)
    int waitingWriters;                  // writers blocked (drives writer preference)
    bool upgradeable;                    // an upgradeable-read lock is held (at most one)
    int upgradeCount;                    // its recursion depth (SupportsRecursion)
    std::thread::id upgradeableOwner;    // the upgradeable holder (upgrade-path identity)
    int32_t policy;                      // LockRecursionPolicy: 0 NoRecursion, 1 SupportsRecursion
};

[[noreturn]] static void dn2cpp_rwlock_throw(const Dn2CppTypeInfo* ti, const char* msg)
{
    dn2cpp_throw(dn2cpp_exception_new(ti,
        dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
}

// Real .NET's LockRecursionException texts (measured; see the section comment).
static const char* const kRwRecursiveRead =
    "Recursive read lock acquisitions not allowed in this mode.";
static const char* const kRwRecursiveWrite =
    "Recursive write lock acquisitions not allowed in this mode.";
static const char* const kRwRecursiveUpgrade =
    "Recursive upgradeable lock acquisitions not allowed in this mode.";
static const char* const kRwReadAfterWrite =
    "A read lock may not be acquired with the write lock held in this mode.";
static const char* const kRwWriteAfterRead =
    "Write lock may not be acquired with read lock held. This pattern is prone to "
    "deadlocks. Please ensure that read locks are released before taking a write "
    "lock. If an upgrade is necessary, use an upgrade lock in place of the read lock.";
static const char* const kRwUpgradeAfterRead =
    "Upgradeable lock may not be acquired with read lock held.";
static const char* const kRwUpgradeAfterWrite =
    "Upgradeable lock may not be acquired with write lock held in this mode. Acquiring "
    "Upgradeable lock gives the ability to read along with an option to upgrade to a writer.";

// Caller holds r->m.
static Dn2CppRwReadHold* dn2cpp_rwlock_find_read(Dn2CppRwLock* r, std::thread::id self)
{
    for (auto& h : r->readHolds)
    {
        if (h.owner == self)
            return &h;
    }
    return nullptr;
}

Dn2CppObject* dn2cpp_rwlock_new(int32_t policy)
{
    auto* r = new Dn2CppRwLock();
    r->type = &dn2cpp_rwlock_type;
    r->writer = false;
    r->writeCount = 0;
    r->waitingWriters = 0;
    r->upgradeable = false;
    r->upgradeCount = 0;
    r->policy = policy;
    return r;
}

// The pre-wait recursion verdict for a read acquire (caller holds r->m). Returns true
// when the hold was granted (or counted) without waiting — a re-entry never queues
// behind a foreign waiting writer, because this thread already has read access.
static bool dn2cpp_rwlock_read_precheck(Dn2CppRwLock* r, std::thread::id self)
{
    if (Dn2CppRwReadHold* h = dn2cpp_rwlock_find_read(r, self))
    {
        if (r->policy == 0)
            dn2cpp_rwlock_throw(&dn2cpp_lock_recursion_exception_type, kRwRecursiveRead);
        h->count++;
        return true;
    }
    if (r->writer && r->writeOwner == self)
    {
        if (r->policy == 0)
            dn2cpp_rwlock_throw(&dn2cpp_lock_recursion_exception_type, kRwReadAfterWrite);
        r->readHolds.push_back({ self, 1 });
        return true;
    }
    if (r->upgradeable && r->upgradeableOwner == self)
    {
        // The upgradeable holder already has read access — granted in BOTH modes
        // (measured), and immediately, so a waiting foreign writer cannot deadlock it.
        r->readHolds.push_back({ self, 1 });
        return true;
    }
    return false;
}

void dn2cpp_rwlock_enter_read(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::unique_lock<std::mutex> lk(r->m);
    std::thread::id self = std::this_thread::get_id();
    if (dn2cpp_rwlock_read_precheck(r, self))
        return;
    // writer preference: yield to an active or waiting writer
    r->readersCv.wait(lk, [r] { return !r->writer && r->waitingWriters == 0; });
    r->readHolds.push_back({ self, 1 });
}

void dn2cpp_rwlock_exit_read(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    bool last;
    {
        std::lock_guard<std::mutex> lk(r->m);
        std::thread::id self = std::this_thread::get_id();
        Dn2CppRwReadHold* h = dn2cpp_rwlock_find_read(r, self);
        if (h == nullptr)
            dn2cpp_rwlock_throw(&dn2cpp_synchronization_lock_exception_type,
                "The read lock is being released without being held.");
        if (--h->count > 0)
            return;
        r->readHolds.erase(r->readHolds.begin() + (h - r->readHolds.data()));
        last = r->readHolds.empty();
    }
    if (last)
        r->writersCv.notify_one(); // a waiting writer may now proceed
}

// The pre-wait recursion verdict for a write acquire (caller holds r->m). Returns
// true when the acquire was satisfied by recursion.
static bool dn2cpp_rwlock_write_precheck(Dn2CppRwLock* r, std::thread::id self)
{
    if (r->writer && r->writeOwner == self)
    {
        if (r->policy == 0)
            dn2cpp_rwlock_throw(&dn2cpp_lock_recursion_exception_type, kRwRecursiveWrite);
        r->writeCount++;
        return true;
    }
    if (dn2cpp_rwlock_find_read(r, self) != nullptr)
    {
        // Refused under BOTH policies (measured): a reader waiting for readers to
        // drain is the deadlock .NET's message spells out.
        dn2cpp_rwlock_throw(&dn2cpp_lock_recursion_exception_type, kRwWriteAfterRead);
    }
    return false;
}

void dn2cpp_rwlock_enter_write(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::unique_lock<std::mutex> lk(r->m);
    std::thread::id self = std::this_thread::get_id();
    if (dn2cpp_rwlock_write_precheck(r, self))
        return;
    // The upgradeable holder upgrading to write: its own upgradeable hold must
    // not block it — it only waits for the plain readers to drain.
    bool upgrading = r->upgradeable && r->upgradeableOwner == self;
    r->waitingWriters++;
    r->writersCv.wait(lk, [r, upgrading] {
        return !r->writer && r->readHolds.empty() && (upgrading || !r->upgradeable);
    });
    r->waitingWriters--;
    r->writer = true;
    r->writeCount = 1;
    r->writeOwner = self;
}

void dn2cpp_rwlock_exit_write(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    {
        std::lock_guard<std::mutex> lk(r->m);
        if (!r->writer || r->writeOwner != std::this_thread::get_id())
            dn2cpp_rwlock_throw(&dn2cpp_synchronization_lock_exception_type,
                "The write lock is being released without being held.");
        if (--r->writeCount > 0)
            return;
        r->writer = false;
    }
    r->writersCv.notify_one(); // prefer handing off to a waiting writer
    r->readersCv.notify_all(); // else release all blocked readers / upgradeable acquirers
}

// The pre-wait recursion verdict for an upgradeable acquire (caller holds r->m).
static bool dn2cpp_rwlock_upgradeable_precheck(Dn2CppRwLock* r, std::thread::id self)
{
    if (r->upgradeable && r->upgradeableOwner == self)
    {
        if (r->policy == 0)
            dn2cpp_rwlock_throw(&dn2cpp_lock_recursion_exception_type, kRwRecursiveUpgrade);
        r->upgradeCount++;
        return true;
    }
    if (r->writer && r->writeOwner == self)
    {
        if (r->policy == 0)
            dn2cpp_rwlock_throw(&dn2cpp_lock_recursion_exception_type, kRwUpgradeAfterWrite);
        // SupportsRecursion write->upg (measured OK): the write holder already
        // excludes every other upgradeable candidate, so grant immediately.
        r->upgradeable = true;
        r->upgradeCount = 1;
        r->upgradeableOwner = self;
        return true;
    }
    if (dn2cpp_rwlock_find_read(r, self) != nullptr)
        dn2cpp_rwlock_throw(&dn2cpp_lock_recursion_exception_type, kRwUpgradeAfterRead);
    return false;
}

void dn2cpp_rwlock_enter_upgradeable(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::unique_lock<std::mutex> lk(r->m);
    std::thread::id self = std::this_thread::get_id();
    if (dn2cpp_rwlock_upgradeable_precheck(r, self))
        return;
    // at most one upgradeable holder; it coexists with plain readers but excludes writers
    r->readersCv.wait(lk, [r] { return !r->writer && !r->upgradeable; });
    r->upgradeable = true;
    r->upgradeCount = 1;
    r->upgradeableOwner = self;
}

void dn2cpp_rwlock_exit_upgradeable(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    {
        std::lock_guard<std::mutex> lk(r->m);
        if (!r->upgradeable || r->upgradeableOwner != std::this_thread::get_id())
            dn2cpp_rwlock_throw(&dn2cpp_synchronization_lock_exception_type,
                "The upgradeable lock is being released without being held.");
        if (--r->upgradeCount > 0)
            return;
        r->upgradeable = false;
    }
    r->writersCv.notify_one(); // a writer was waiting on the upgradeable hold
    r->readersCv.notify_all(); // and another upgradeable acquirer may proceed
}

// TryEnter*(int ms / TimeSpan): a real timed acquire. Returns 1 if the lock was taken,
// 0 on timeout. ms == 0 is a single immediate check; a negative ms (Timeout.Infinite) is
// an infinite wait. The recursion verdict precedes the timed wait, exactly as in
// Enter* — measured: real .NET's TryEnter* throws LockRecursionException rather than
// returning false.
int32_t dn2cpp_rwlock_try_enter_read(Dn2CppObject* o, int32_t ms)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::unique_lock<std::mutex> lk(r->m);
    std::thread::id self = std::this_thread::get_id();
    if (dn2cpp_rwlock_read_precheck(r, self))
        return 1;
    auto pred = [r] { return !r->writer && r->waitingWriters == 0; };
    if (ms < 0)
        r->readersCv.wait(lk, pred);
    else if (!r->readersCv.wait_for(lk, std::chrono::milliseconds(ms), pred))
        return 0;
    r->readHolds.push_back({ self, 1 });
    return 1;
}

int32_t dn2cpp_rwlock_try_enter_write(Dn2CppObject* o, int32_t ms)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::unique_lock<std::mutex> lk(r->m);
    std::thread::id self = std::this_thread::get_id();
    if (dn2cpp_rwlock_write_precheck(r, self))
        return 1;
    bool upgrading = r->upgradeable && r->upgradeableOwner == self;
    auto pred = [r, upgrading] {
        return !r->writer && r->readHolds.empty() && (upgrading || !r->upgradeable);
    };
    r->waitingWriters++;
    bool ok;
    if (ms < 0)
    {
        r->writersCv.wait(lk, pred);
        ok = true;
    }
    else
    {
        ok = r->writersCv.wait_for(lk, std::chrono::milliseconds(ms), pred);
    }
    r->waitingWriters--;
    if (!ok)
    {
        // a timed-out writer no longer blocks readers; release any held off by it
        lk.unlock();
        r->readersCv.notify_all();
        return 0;
    }
    r->writer = true;
    r->writeCount = 1;
    r->writeOwner = self;
    return 1;
}

int32_t dn2cpp_rwlock_try_enter_upgradeable(Dn2CppObject* o, int32_t ms)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::unique_lock<std::mutex> lk(r->m);
    std::thread::id self = std::this_thread::get_id();
    if (dn2cpp_rwlock_upgradeable_precheck(r, self))
        return 1;
    auto pred = [r] { return !r->writer && !r->upgradeable; };
    if (ms < 0)
        r->readersCv.wait(lk, pred);
    else if (!r->readersCv.wait_for(lk, std::chrono::milliseconds(ms), pred))
        return 0;
    r->upgradeable = true;
    r->upgradeCount = 1;
    r->upgradeableOwner = self;
    return 1;
}

int32_t dn2cpp_rwlock_current_read_count(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::lock_guard<std::mutex> lk(r->m);
    // CurrentReadCount counts reader THREADS (recursion does not raise it). Unlike
    // the pre-ownership model, an upgradeable holder that also entered the read lock
    // holds a readHolds entry and is counted — matching real .NET, whose upgradeable
    // owner is excluded only until it acquires a read hold of its own.
    return static_cast<int32_t>(r->readHolds.size());
}

int32_t dn2cpp_rwlock_waiting_write_count(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::lock_guard<std::mutex> lk(r->m);
    return r->waitingWriters;
}

// The per-thread lock-held queries — answerable now that ownership is tracked.
int32_t dn2cpp_rwlock_is_read_held(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::lock_guard<std::mutex> lk(r->m);
    return dn2cpp_rwlock_find_read(r, std::this_thread::get_id()) != nullptr ? 1 : 0;
}

int32_t dn2cpp_rwlock_is_write_held(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::lock_guard<std::mutex> lk(r->m);
    return (r->writer && r->writeOwner == std::this_thread::get_id()) ? 1 : 0;
}

int32_t dn2cpp_rwlock_is_upgradeable_held(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    std::lock_guard<std::mutex> lk(r->m);
    return (r->upgradeable && r->upgradeableOwner == std::this_thread::get_id()) ? 1 : 0;
}

int32_t dn2cpp_rwlock_recursion_policy(Dn2CppObject* o)
{
    auto* r = static_cast<Dn2CppRwLock*>(o);
    return r->policy; // immutable after construction — no lock needed
}

// ===== System.Threading.Timer (per-timer OS thread) =========================
// A Timer owns a dedicated OS thread that waits dueTime, fires TimerCallback(state),
// and — when period is finite (> 0) — re-fires every period. (Distinct from the
// scheduler's virtual-clock Dn2CppTimer above, which models Task.Delay.) Unlike the
// blocking primitives above (which hold only ints/bools), a Timer holds *managed*
// references — the callback delegate and its state object — so it is GC-allocated
// (dn2cpp_alloc). While the timer thread runs it keeps the Dn2CppManagedTimer* on its
// GC-scanned stack, so the timer — and through it the callback/state — stay reachable.
// Consequence: a timer is not collected while its thread runs, even if the program drops
// its last reference; in real .NET dropping the reference can let finalization stop the
// timer. dn2cpp is more lenient — a timer runs until Dispose (the gate always disposes).
// The mutex/condition_variable are constructed in place (the GC heap is raw) and never
// destructed, a small bounded leak like the other primitives.
extern const Dn2CppType dn2cpp_timer_type_obj;
extern const Dn2CppType dn2cpp_timeprovider_timer_type_obj;
// Non-const and externally visible (like dn2cpp_string_type): its IDisposable interface
// row points at a program-specific emitted type-info (`ti_System__IDisposable`), so the
// generated init prologue wires it in at startup (dn2cpp_intrinsic_set_interfaces).
// Without the row, `using (new Timer(...))` — which lowers to
// `callvirt IDisposable::Dispose` — dies on the interface-dispatch "has no map" abort
// while the direct Dispose() call works.
// NO_SHALLOW_CLONE: Dn2CppManagedTimer embeds a std::mutex/condition_variable, owns a
// `new std::thread` through `handle`, and holds a count in the process-wide blocked-wait
// principal registry through `counted` — a bitwise copy would be a second owner of all
// three. (The GC allocates it, but placement-new constructs it.)
Dn2CppTypeInfo dn2cpp_timer_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Timer", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_timer_type_obj);
const Dn2CppType dn2cpp_timer_type_obj = { { &dn2cpp_type_type }, &dn2cpp_timer_type };
Dn2CppTypeInfo dn2cpp_timeprovider_timer_type =
    dn2cpp_ti_with_typeobject({ "System.TimeProvider+SystemTimeProviderTimer", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_timeprovider_timer_type_obj);
const Dn2CppType dn2cpp_timeprovider_timer_type_obj = { { &dn2cpp_type_type }, &dn2cpp_timeprovider_timer_type };

struct Dn2CppManagedTimer : Dn2CppObject
{
    Dn2CppObject* callback;   // TimerCallback delegate (possibly a multicast chain)
    Dn2CppObject* state;      // the object threaded to TimerCallback(state); may be null
    std::mutex m;
    std::condition_variable cv;
    int64_t dueMs;            // ms until the next fire; < 0 => idle (Timeout.Infinite)
    int64_t periodMs;         // <= 0 (or Infinite, -1) => one-shot (no re-arm)
    bool disposed;
    bool inCallback;          // a TimerCallback is running (fired, not yet returned)
    bool counted;             // holds one count in the blocked-wait timer principal
    uint64_t generation;      // bumped by Change to abandon an in-progress wait
    void* handle;             // std::thread* (native heap; not a GC pointer)
    std::thread::id threadId; // the timer thread's id (for self-Dispose detection)
};

// Keep `counted` equal to the timer's ARMED state — (a fire is pending and the timer is
// not disposed) OR a callback is in flight — joining/leaving the blocked-wait principal
// set on each transition. Called with t->m held by EVERY writer of dueMs /
// disposed / inCallback, which is what makes the transitions total: Change and Dispose
// are principal transitions exactly like the loop's own fire -> re-arm / fire -> idle
// steps. The count deliberately does NOT follow the timer thread's lifetime: that thread
// lives until Dispose, so a lifetime +1 would disarm the defeated-wait report for as
// long as any program held an undisposed idle timer — a silent hang everywhere, traded
// for the false verdict this fixes (see g_live_timer_threads, dn2cpp_tasks.cpp).
//
// The inCallback term is OR'd outside the !disposed guard on purpose: a Dispose (or a
// disarming Change) racing an in-flight callback must not drop the count while the
// callback — arbitrary managed code that can still settle a task somebody is blocked
// on — is running; the loop's post-callback step is what retires it. Lock order: leave
// wakes every scheduler (t->m, then each scheduler's mtx, taken-and-released); nothing
// takes t->m while holding a scheduler mutex — managed code never runs under one — so
// the order is consistent.
static void dn2cpp_timer_sync_principal(Dn2CppManagedTimer* t)
{
    bool armed = (!t->disposed && t->dueMs >= 0) || t->inCallback;
    if (armed == t->counted)
        return;
    t->counted = armed;
    if (armed)
        dn2cpp_timer_principal_join();
    else
        dn2cpp_timer_principal_leave();
}

// The timer thread body: an interruptible sleep loop. It waits dueMs (or sleeps idle
// when dueMs < 0), then fires the callback OUTSIDE the lock (user code may Change/Dispose
// or block). A Change during the wait bumps `generation` and reschedules; Dispose wakes
// it and ends the loop. After a fire, a positive periodMs re-arms; otherwise the timer
// goes idle (a one-shot fires exactly once until a later Change).
static void dn2cpp_timer_thread(Dn2CppManagedTimer* t)
{
    Dn2CppGCThread guard; // it invokes managed callbacks -> must be GC-registered
    dn2cpp_timer_principal_mark_self(); // a blocking TimerCallback is not its own principal
    std::unique_lock<std::mutex> lk(t->m);
    while (!t->disposed)
    {
        if (t->dueMs < 0)
        {
            // Idle (Timeout.Infinite): sleep until Change arms it or Dispose ends it.
            t->cv.wait(lk, [t] { return t->disposed || t->dueMs >= 0; });
            continue;
        }
        uint64_t gen = t->generation;
        // Interruptible sleep: wakes early on Dispose or a Change (generation bump).
        t->cv.wait_for(lk, std::chrono::milliseconds(t->dueMs),
                       [t, gen] { return t->disposed || t->generation != gen; });
        if (t->disposed)
            break;
        if (t->generation != gen)
            continue; // Change happened during the wait -> reschedule with the new dueMs
        Dn2CppObject* cb = t->callback;
        Dn2CppObject* st = t->state;
        // The callback-in-flight window opens BEFORE the lock drops: the callback can
        // settle a task, so the armed count must cover every instant of its run even if
        // a concurrent Change disarms (or a Dispose lands) while it executes. No
        // principal transition happens here — dueMs >= 0 means `counted` already holds.
        t->inCallback = true;
        lk.unlock();
        dn2cpp_paramthread_invoke(cb, st); // TimerCallback(state) incl. multicast chain
        lk.lock();
        t->inCallback = false;
        if (!t->disposed)
        {
            if (t->generation != gen)
            {
                // A Change landed while the callback was in flight: its dueMs/periodMs
                // ARE the schedule now — real .NET honors a mid-callback Change's due
                // time, and the common idiom depends on it (a one-shot whose callback
                // re-arms itself via Change). An unconditional re-arm/idle overwrite
                // below would clobber it. Nothing to write here — the loop's next
                // iteration waits the Change's dueMs under a fresh generation capture.
            }
            else if (t->periodMs > 0)
                t->dueMs = t->periodMs; // periodic: re-arm for the next tick
            else
                t->dueMs = -1;          // one-shot: go idle until a later Change
        }
        // The loop's own principal transition: a one-shot going idle (or a Dispose that
        // landed mid-callback) leaves the settler set HERE, with the callback fully
        // behind it — delivered, so a wait this timer could no longer settle re-reads
        // its verdict instead of sleeping on. A periodic re-arm keeps the count.
        dn2cpp_timer_sync_principal(t);
        if (t->disposed)
            break;
    }
}

static Dn2CppObject* dn2cpp_timer_new_with_type(Dn2CppTypeInfo* type,
                                                Dn2CppObject* callback, Dn2CppObject* state,
                                                int64_t dueMs, int64_t periodMs)
{
    // GC-allocated (it holds managed callback/state), then constructed in place so the
    // std::mutex / std::condition_variable members get real ctors over the raw GC heap.
    auto* t = static_cast<Dn2CppManagedTimer*>(dn2cpp_alloc(sizeof(Dn2CppManagedTimer)));
    new (t) Dn2CppManagedTimer();
    t->type = type;
    t->callback = callback;
    t->state = state;
    t->dueMs = dueMs;
    t->periodMs = periodMs;
    t->disposed = false;
    t->inCallback = false;
    t->counted = false;
    t->generation = 0;
    t->handle = nullptr;
    // A timer born armed joins the settler set BEFORE its thread exists (the CancelAfter
    // rule, dn2cpp_cts_cancel_after): the caller's very next statement may be the Wait()
    // this timer is going to settle, and the timer thread has not necessarily run an
    // instruction by then, and an empty set read in that window is a false verdict.
    // No lock: nothing else can reach `t` yet. A timer born idle (dueMs < 0, the 1-arg
    // ctor) counts for nothing until a Change arms it.
    if (t->dueMs >= 0)
    {
        t->counted = true;
        dn2cpp_timer_principal_join();
    }
    // Capturing the GC-allocated `t` (see dn2cpp_cts_cancel_after in dn2cpp_tasks.cpp):
    // a std::thread closure block is `operator new` memory, not a GC
    // root. It is sound here because the timer loop re-reads `t`'s fields around every
    // wait, so `t` itself stays in a callee-saved register the collector scans — not
    // because capturing it is safe. A timer whose thread ever went to sleep holding
    // nothing but the capture would need CancelAfter's pinned cell.
    // Publish threadId/handle under t->m with the spawn in the same critical section: the
    // timer thread's first act is to take t->m, so an immediate (dueMs == 0) fire cannot
    // see them unset and self-Dispose against a stale identity test — which would join its
    // own thread. Holding t->m across the spawn only blocks the child's t->m acquire.
    std::thread* th;
    {
        std::lock_guard<std::mutex> lk(t->m);
        try
        {
            th = new std::thread([t] { dn2cpp_timer_thread(t); });
        }
        catch (...)
        {
            // No thread was started (a native host can refuse one; on wasm the ctor always
            // throws). Take the +1 back down — nothing else ever will, and a leaked count
            // disarms the defeated-wait report for the rest of the process, turning every
            // later defeated wait into a silent hang.
            if (t->counted)
            {
                t->counted = false;
                dn2cpp_timer_principal_leave();
            }
            throw;
        }
        t->threadId = th->get_id();
        t->handle = th;
    }
    return t;
}

Dn2CppObject* dn2cpp_timer_new(Dn2CppObject* callback, Dn2CppObject* state,
                               int64_t dueMs, int64_t periodMs)
{
    return dn2cpp_timer_new_with_type(&dn2cpp_timer_type, callback, state, dueMs, periodMs);
}

Dn2CppObject* dn2cpp_timeprovider_timer_new(Dn2CppObject* callback, Dn2CppObject* state,
                                            int64_t dueMs, int64_t periodMs)
{
    return dn2cpp_timer_new_with_type(&dn2cpp_timeprovider_timer_type,
                                      callback, state, dueMs, periodMs);
}

// Timer.Change(dueTime, period): reschedule. Sets the new dueMs/periodMs, bumps the
// generation so the timer thread abandons any in-progress wait and reschedules, then
// wakes it. After Dispose it returns 0 and leaves the schedule untouched (real .NET
// reports false there); rewriting a disposed timer's dueMs would re-enter the settler
// accounting for a thread that is already joined.
// A Change is a principal transition: arming an idle timer joins the settler
// set before this returns — the caller may block on the callback's settle next — and a
// Change to Timeout.Infinite retires the pending fire, so the principal leaves HERE,
// not at Dispose (a callback in flight keeps its count until it returns).
int32_t dn2cpp_timer_change(Dn2CppObject* o, int64_t dueMs, int64_t periodMs)
{
    auto* t = static_cast<Dn2CppManagedTimer*>(o);
    {
        std::lock_guard<std::mutex> lk(t->m);
        if (t->disposed)
            return 0;
        t->dueMs = dueMs;
        t->periodMs = periodMs;
        t->generation++;
        dn2cpp_timer_sync_principal(t);
    }
    t->cv.notify_one();
    return 1;
}

// Timer.Dispose(): stop the timer. Sets disposed under the lock, wakes the thread, and
// joins it so no callback fires after Dispose returns and the thread is reaped. (This is
// stricter than real .NET, which does not guarantee an in-flight callback has finished;
// joining keeps the gate deterministic.) If the callback itself calls Dispose, joining
// self would deadlock — detect that by comparing thread ids and detach instead, letting
// the loop exit on the disposed flag once the callback returns. Always returns 1.
// Dispose is the other principal transition: a pending fire that will now never
// happen leaves the settler set here, synchronously — a defeated wait re-reads its
// verdict at the Dispose, not at some later thread teardown. A callback in flight keeps
// its count (the inCallback term of the armed predicate) until the loop's post-callback
// step retires it, which the join below waits out.
int32_t dn2cpp_timer_dispose(Dn2CppObject* o)
{
    auto* t = static_cast<Dn2CppManagedTimer*>(o);
    std::thread* th;
    std::thread::id tid;
    {
        std::lock_guard<std::mutex> lk(t->m);
        t->disposed = true;
        dn2cpp_timer_sync_principal(t);
        // Read under t->m: this is the only edge ordering the ctor's publish of
        // handle/threadId before these reads.
        th = static_cast<std::thread*>(t->handle);
        tid = t->threadId;
    }
    t->cv.notify_one();
    // The join stays OUTSIDE t->m — the timer thread holds it around every wait.
    if (th != nullptr && th->joinable())
    {
        if (std::this_thread::get_id() == tid)
            th->detach(); // Dispose() called from the callback — cannot join self
        else
            th->join();
    }
    return 1;
}
