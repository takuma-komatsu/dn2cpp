// dn2cpp_runtime_internal.h — cross-TU internals of the split runtime unit
// (dn2cpp_runtime.cpp and its per-topic siblings: dn2cpp_gc / _exceptions /
// _tasks / _threading / _typeinfo / _casts / _marshal). Visible only to those
// TUs; nothing outside them may include this header. Keep the list minimal: a
// symbol belongs here only if its definition and a caller genuinely land in
// different TUs of the split.
#pragma once

#include "dn2cpp_core.h"

#include <chrono> // dn2cpp_pool_quiesce deadline

// <gc.h> must be visible before Dn2CppGCThread below: its inline ctor/dtor
// call the GC_* thread-registration API (GC_get_stack_base et al.). GC_THREADS
// itself is a CMake command-line define (runtime/CMakeLists.txt, PUBLIC on
// dn2cpp_runtime), not something gc.h provides — dropping this include fails
// loudly at compile time on the undeclared GC_* names.
#ifdef DN2CPP_USE_BOEHM_GC
#include <gc.h>
#endif

// (dn2cpp_tasks.cpp) Materialize the main thread's Thread object — called once
// from dn2cpp_runtime_init (dn2cpp_gc.cpp).
void dn2cpp_thread_materialize_main();

// (dn2cpp_tasks.cpp) Drain the worker pool — called from dn2cpp_runtime_quiesce
// (dn2cpp_gc.cpp).
int32_t dn2cpp_pool_quiesce(std::chrono::steady_clock::time_point deadline, bool infinite);

// (dn2cpp_tasks.cpp) Allocate-and-enqueue a pool work item — SemaphoreSlim's
// WaitAsync (dn2cpp_threading.cpp) routes its blocking wait through the pool.
Dn2CppTask* dn2cpp_pool_submit(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*));

// (dn2cpp_tasks.cpp) Invoke a one-arg delegate (Action<object> /
// ParameterizedThreadStart / TimerCallback) and its multicast chain — reused by
// Parallel bodies and Timer callbacks (dn2cpp_threading.cpp).
void dn2cpp_paramthread_invoke(Dn2CppObject* del, Dn2CppObject* arg);

// (dn2cpp_threading.cpp) Same with a trailing ParallelLoopState argument —
// reused by ContinueWith's state-carrying continuations (dn2cpp_tasks.cpp).
void dn2cpp_paramthread_invoke_state(Dn2CppObject* del, Dn2CppObject* arg, Dn2CppObject* state);

// (dn2cpp_tasks.cpp) System.Threading.Timer's armed-state principal transitions —
// the timer counter of the blocked-wait principal set lives with the other principal
// counters in dn2cpp_tasks.cpp, while the timer's state machine lives in
// dn2cpp_threading.cpp. Join/leave follow the timer's ARMED state (a
// pending due time, or a callback in flight), never the timer thread's lifetime;
// mark_self sets the calling thread's self-discount so a TimerCallback blocking
// on an unsatisfiable wait still gets the defeated-wait report.
void dn2cpp_timer_principal_join();
void dn2cpp_timer_principal_leave();
void dn2cpp_timer_principal_mark_self();

// (dn2cpp_gc.cpp) Thread-exit release of the thread-static block and the
// SynchronizationContext slot — called from ~Dn2CppGCThread below on every
// runtime-spawned thread.
void dn2cpp_threadstatic_release();
void dn2cpp_sync_ctx_release();

// RAII Boehm-GC registration for a spawned std::thread (Thread.Start, the Task.Run
// worker pool). The main thread is implicitly registered by GC_INIT; every other
// thread that touches GC pointers must register before its first allocation and
// unregister on exit (incl. exception unwind). The registration compiles to nothing
// under DN2CPP_NO_GC — the calloc fallback is already thread-safe — but the
// thread-static block release still runs (the calloc fallback allocates one too).
// Also compiles to nothing in the wasm GC build (no GC_THREADS: the thread
// register/unregister entry points are not even declared there); the spawn path
// stays, so std::thread's ctor throws at runtime — an intended loud degrade.
struct Dn2CppGCThread
{
#if defined(DN2CPP_USE_BOEHM_GC) && defined(GC_THREADS)
    bool registered = false;
    Dn2CppGCThread()
    {
        struct GC_stack_base sb;
        if (GC_get_stack_base(&sb) == GC_SUCCESS)
            registered = (GC_register_my_thread(&sb) == GC_SUCCESS);
    }
#endif
    ~Dn2CppGCThread()
    {
        // The thread is exiting: drop its thread-static block and its
        // SynchronizationContext slot first, while the thread is still
        // registered with the collector.
        dn2cpp_threadstatic_release();
        dn2cpp_sync_ctx_release();
#if defined(DN2CPP_USE_BOEHM_GC) && defined(GC_THREADS)
        if (registered)
            GC_unregister_my_thread();
#endif
    }
};
