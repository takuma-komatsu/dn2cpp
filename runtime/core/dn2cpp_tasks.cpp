// dn2cpp_tasks.cpp — async machinery of the dn2cpp runtime:
// the async/await scheduler, cancellation,
// TaskCompletionSource, System.Threading.Thread, the Task.Run worker pool,
// cold tasks, ContinueWith, and the IValueTaskSource-backed ValueTask bridge.

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

// ---- async/await ----

extern const Dn2CppType dn2cpp_task_type_obj;
const Dn2CppTypeInfo dn2cpp_task_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Tasks.Task", nullptr, (int32_t)sizeof(Dn2CppTask), nullptr, nullptr, 0 }, &dn2cpp_task_type_obj);
const Dn2CppType dn2cpp_task_type_obj = { { &dn2cpp_type_type }, &dn2cpp_task_type };

// Task.Id numbering: positive, monotonic, minted once per task at alloc time.
// (Real .NET's Ids are also positive and unique; the numbering itself differs.)
// Function-local static so the counter is initialized before any static-init-time
// task allocation (dn2cpp_task_default_completed) can ask for an id.
static int32_t dn2cpp_task_next_id()
{
    static std::atomic<int32_t> next{1};
    return next.fetch_add(1, std::memory_order_relaxed);
}

Dn2CppTask* dn2cpp_task_alloc()
{
    auto* t = static_cast<Dn2CppTask*>(dn2cpp_alloc(sizeof(Dn2CppTask)));
    t->type = &dn2cpp_task_type;
    // Relaxed here and in every other pre-completed mint below: the task is private
    // to this thread until the caller publishes it, and that handoff carries it.
    t->status.store(DN2CPP_TASK_PENDING, std::memory_order_relaxed);
    t->id = dn2cpp_task_next_id();
    t->exception = nullptr;
    t->exceptionAggregate.store(nullptr, std::memory_order_relaxed);
    t->result = 0;
    t->continuations = nullptr;
    t->workerKeepAlive = nullptr;
    t->cold.store(nullptr, std::memory_order_relaxed);
    return t;
}

Dn2CppTask* dn2cpp_task_completed()
{
    auto* t = dn2cpp_task_alloc();
    t->status.store(DN2CPP_TASK_SUCCEEDED, std::memory_order_relaxed);
    return t;
}

// The shared immutable pre-completed sentinel a default(ValueTask)'s null task
// normalizes to (dn2cpp_vtask in dn2cpp.h). Lives in the scanned static data
// segment and references nothing; read-only by construction — every reader sees
// the non-PENDING status before touching result/exception, so the mutating
// suspend/continuation paths never run on it.
// Aggregate-initialized: an atomic member makes Dn2CppTask non-copyable.
Dn2CppTask dn2cpp_task_default_completed{
    { &dn2cpp_task_type }, DN2CPP_TASK_SUCCEEDED,
    dn2cpp_task_next_id() }; // keep the "Id > 0" invariant for the sentinel too

Dn2CppTask* dn2cpp_task_from_result(uint64_t result)
{
    auto* t = dn2cpp_task_alloc();
    t->status.store(DN2CPP_TASK_SUCCEEDED, std::memory_order_relaxed);
    t->result = result;
    return t;
}

void dn2cpp_task_throw_if_faulted(Dn2CppTask* t)
{
    // A faulted or canceled task re-raises its stored exception (a canceled task
    // carries a TaskCanceledException) when awaited. Rethrow,
    // not throw: real .NET preserves the original stack trace across await
    // propagation (EDI), so the trace stamped at the original throw survives.
    if (t != nullptr && (t->status == DN2CPP_TASK_FAULTED || t->status == DN2CPP_TASK_CANCELED))
        dn2cpp_rethrow(t->exception);
}

// Per-thread cooperative run queue + virtual clock (the "scheduler"). Each thread
// owns one (thread_local). Resumptions posted here run when the thread blocks on a
// task (Task.Wait/.Result drains until it completes). Async on the main thread alone
// behaves exactly as the old single-threaded model: every continuation's owner is
// the main scheduler, every completion happens on main, the run-queue mutex is
// uncontended, and no cross-thread wakeup ever fires.
//   - timers + virtual clock are touched ONLY by the owning thread (Task.Delay is
//     called by the owner; advance_timers runs inside its task_block) -> no lock.
//   - the run queue may receive cross-thread pushes (a worker completing a Task the
//     owner awaits routes the continuation here) -> guarded by `mtx`, with `cv` so
//     task_block can sleep until a cross-thread completion arrives.
struct Dn2CppTimer; // full definition below
struct Dn2CppScheduler
{
    Dn2CppCont* head = nullptr;
    Dn2CppCont* tail = nullptr;
    Dn2CppTimer* timers = nullptr;       // owner-only
    Dn2CppTimer* timers_tail = nullptr;  // owner-only
    int64_t virtual_now = 0;             // owner-only
    int64_t timer_seq = 0;               // owner-only
    // Real-time base of the frame pump (owner-only): virtual_now advances by the
    // real milliseconds elapsed between dn2cpp_sched_pump calls, with the sub-ms
    // remainder carried in pump_last so it is never lost to rounding.
    std::chrono::steady_clock::time_point pump_last{};
    bool pump_has_last = false;
    // Pool workers alternate this scheduler's owner-only work with the global
    // pool queue. The flag is also the scheduler's single settler principal:
    // an arbitrary number of queued resumptions/timers costs one count.
    bool pool_worker = false;                 // fixed before the worker runs managed code
    std::atomic<bool> pool_local_principal{false}; // transitions under mtx; read by pool wait
    std::mutex mtx;                      // guards head/tail (cross-thread push)
    std::condition_variable cv;          // task_block waits here
};

// A scheduler outlives the thread that owns it. Its address is published to other
// threads (a continuation stores it as `owner`, and a Task's continuation list is
// read by whatever thread settles the task), so if it lived in thread-local storage
// a Thread.Start body that awaits and returns before its task completes would leave
// the completing thread locking a destroyed mutex. Heap-allocate it and never free
// it. The cost is ~180 bytes per thread that ever runs async, and it is unbounded:
// a program that keeps spawning short-lived threads which await accumulates one
// scheduler each. Reclaiming them needs the lifetime tied to the continuations that
// name a scheduler — a refcount that a cont abandoned without being consumed (the
// already-complete fast path, a canceled Task.Delay) cannot decrement. The leak buys
// a use-after-free, which is the right trade at this size, but it is not a bound.
// Nothing else roots a scheduler: the run queue's continuations and the timer
// entries are kept reachable through g_pending_conts / g_pending_timers below.
static thread_local Dn2CppScheduler* t_sched_ptr = nullptr;

// Every scheduler ever created. A blocked drain's verdict depends on state that is not
// its own task's — the principal set — and nothing about a principal LEAVING touches the
// task the drain is waiting on, so there is no completion to ride: the last principal out
// has to wake the waiters itself. Schedulers are immortal (see t_sched_ptr's note), so
// the list only ever grows and holds no dangling entry. The registry lock is a LEAF: the
// waker copies the list under it, releases it, and only then touches any s->mtx.
static std::mutex& g_sched_registry_mtx = dn2cpp_never_destroyed<std::mutex>();
static std::vector<Dn2CppScheduler*>& g_sched_registry =
    dn2cpp_never_destroyed<std::vector<Dn2CppScheduler*>>();

static Dn2CppScheduler* dn2cpp_sched_self()
{
    if (t_sched_ptr == nullptr)
    {
        t_sched_ptr = new Dn2CppScheduler();
        std::lock_guard<std::mutex> lk(g_sched_registry_mtx);
        g_sched_registry.push_back(t_sched_ptr);
    }
    return t_sched_ptr;
}

// Wake every blocked drain so it re-evaluates the principal set. Called ONLY on the
// transition of a principal counter to zero, so the cost is paid once per emptying, never
// per completion. Each scheduler's mutex is taken (and immediately released) before its
// notify: a waiter evaluates the predicate under that mutex, so without the handshake a
// notify sent between its check and its wait would be lost and the wait would never end.
static void dn2cpp_sched_wake_all()
{
    std::vector<Dn2CppScheduler*> snapshot;
    {
        std::lock_guard<std::mutex> lk(g_sched_registry_mtx);
        snapshot = g_sched_registry;
    }
    for (Dn2CppScheduler* s : snapshot)
    {
        { std::lock_guard<std::mutex> lk(s->mtx); }
        s->cv.notify_all();
    }
}

// Number of pool principals that may still complete a Task some cooperative thread is
// blocked on. Each global item contributes one from dispatch through completion; each
// pool worker with owner-local continuations/timers contributes one until both queues
// empty. When it is >0 task_block sleeps instead of concluding anything. It is only ONE
// input to dn2cpp_task_settler_exists: it cannot see a user-space executor.
std::atomic<int> g_inflight_async_tasks{0};

// The SECOND principal a blocked wait must account for: threads the program started
// itself (System.Threading.Thread), which run arbitrary managed code and can therefore
// settle any task — through Task.RunSynchronously on a cold task, Task.Start,
// TaskCompletionSource.SetResult, anything. ++ before the OS thread is spawned (so a
// submitter cannot reach its Wait() before the count is visible), -- when the
// trampoline body returns.
//
// It exists because g_inflight_async_tasks cannot see a user-space executor: a
// ConcurrentQueue of cold `new Task(...)` drained by `new Thread(...)` workers calling
// RunSynchronously(), with submitters blocking in Wait(), goes nowhere near Task.Run,
// so the in-flight count stays 0 for the whole hand-off window and a blocked drain
// would read that 0 as proof nothing could settle its task. Only when there is neither
// pool work in flight nor another live user thread is a wait genuinely unsatisfiable.
static std::atomic<int> g_live_user_threads{0};
// Set on a thread running a user Thread body, so the deadlock test can discount the
// waiter ITSELF: a lone user thread blocking on a task nothing will ever settle must
// still get the diagnostic rather than hang on its own liveness.
static thread_local bool t_on_user_thread = false;

// The THIRD principal: a runtime-internal thread that runs managed code. It is not
// g_live_user_threads because nothing about it is a user thread — the program never
// started it and cannot join it — and calling it one would make every diagnosis that
// names the counter a lie. Its members are the ARMED timers of both kinds: the
// CancelAfter timer thread, which runs the source's registered cancel callbacks, and a
// System.Threading.Timer whose callback is pending or in flight. Without the counter
// each reads as an empty set and gets the defeated-wait verdict with its settler
// already ticking.
//
// INVARIANT: every increment is BOUNDED BY AN EVENT, never by an object's lifetime.
// The CancelAfter thread is one-shot, so its +1 is the pending delay.
// System.Threading.Timer's thread lives until Dispose, so its +1 follows the ARMED
// state instead (dn2cpp_timer_sync_principal, dn2cpp_threading.cpp) — a lifetime +1
// would disarm the defeated-wait report for as long as any program held an undisposed
// idle timer.
static std::atomic<int> g_live_timer_threads{0};
// The same self-discount t_on_user_thread provides, for the timer threads (CancelAfter's
// and System.Threading.Timer's alike): a cancel callback or TimerCallback that itself
// blocks on a task nothing will ever settle must get the report rather than hang on its
// own liveness. Sound for the managed Timer because a TimerCallback only ever runs
// inside the callback-in-flight window, during which its own timer holds exactly one
// count — the one the discount removes.
static thread_local bool t_on_timer_thread = false;

// True when some principal other than this thread could still settle a pending task:
// pool work in flight (global items and worker-local resumptions/timers alike), another
// live user thread, or an armed timer (a runtime timer thread with a fire pending or a
// TimerCallback in flight).
static bool dn2cpp_task_settler_exists()
{
    if (g_inflight_async_tasks.load(std::memory_order_acquire) != 0)
        return true;
    int live = g_live_user_threads.load(std::memory_order_acquire);
    if (t_on_user_thread)
        live--; // discount this thread; it is the one that is blocked
    if (live > 0)
        return true;
    int timers = g_live_timer_threads.load(std::memory_order_acquire);
    if (t_on_timer_thread)
        timers--; // ditto: a cancel callback blocking is not its own principal
    return timers > 0;
}

// A principal leaving the set. The wake is only needed on the transition to EMPTY — that
// is the only moment a blocked drain's verdict can change — so fetch_sub's previous value
// is the test. Every decrement of either counter goes through here; a site that decrements
// raw would leave a defeated wait asleep forever, which is the one failure this whole
// mechanism must not have.
static void dn2cpp_principal_left(std::atomic<int>& counter)
{
    if (counter.fetch_sub(1, std::memory_order_acq_rel) == 1)
        dn2cpp_sched_wake_all();
}

// System.Threading.Timer's mouths into the timer principal (declared in
// dn2cpp_runtime_internal.h; the armed-state machine that calls them lives in
// dn2cpp_threading.cpp — see g_live_timer_threads above for why the count follows the
// armed state and not the timer thread's lifetime). Join is a bare increment: joining
// can only turn a would-be verdict into a park, so no wake is owed. Leave goes through
// dn2cpp_principal_left, because a departure that empties the set has to be DELIVERED
// to every parked drain — a raw fetch_sub would strand a defeated wait asleep forever.
void dn2cpp_timer_principal_join()
{
    g_live_timer_threads.fetch_add(1, std::memory_order_acq_rel);
}

void dn2cpp_timer_principal_leave()
{
    dn2cpp_principal_left(g_live_timer_threads);
}

void dn2cpp_timer_principal_mark_self()
{
    t_on_timer_thread = true;
}

// Serializes a task's {status, continuations} transitions so a worker completing a
// task and a cooperative thread registering an await on it cannot race. Uncontended
// (and thus behavior-neutral) for pure single-thread async.
static std::mutex& g_task_mtx = dn2cpp_never_destroyed<std::mutex>();

// Global static-rooted mirrors of every scheduler's pending work. A scheduler lives
// on the malloc heap, reached only through a thread-local pointer — neither is memory
// the conservative collector scans — so a Dn2CppCont sitting in a run queue, or a
// Dn2CppTimer waiting for the virtual clock, can be the ONLY path to a suspended state
// machine and still be invisible to the GC. Every cont enqueued on any scheduler and
// every timer entry is therefore also linked into one of these doubly-linked lists
// (their heads are `static`, and the static data segment IS scanned) and unlinked when
// it is consumed. Both locks are leaves: they are never held while taking any other
// lock.
static std::mutex& g_sched_pending_mtx = dn2cpp_never_destroyed<std::mutex>();
static DN2CPP_GC_STATIC_ROOT Dn2CppCont* g_pending_conts = nullptr;
static DN2CPP_GC_STATIC_ROOT Dn2CppTimer* g_pending_timers = nullptr;

static void dn2cpp_pending_cont_link(Dn2CppCont* c)
{
    std::lock_guard<std::mutex> lk(g_sched_pending_mtx);
    c->gcprev = nullptr;
    dn2cpp_gc_store_ref(&c->gcnext, g_pending_conts);
    if (g_pending_conts != nullptr)
        dn2cpp_gc_store_ref(&g_pending_conts->gcprev, c);
    g_pending_conts = c;
}

static void dn2cpp_pending_cont_unlink(Dn2CppCont* c)
{
    std::lock_guard<std::mutex> lk(g_sched_pending_mtx);
    if (c->gcprev != nullptr)
        dn2cpp_gc_store_ref(&c->gcprev->gcnext, c->gcnext);
    else
        g_pending_conts = c->gcnext;
    if (c->gcnext != nullptr)
        dn2cpp_gc_store_ref(&c->gcnext->gcprev, c->gcprev);
    dn2cpp_gc_store_ref(&c->gcprev, static_cast<Dn2CppCont*>(nullptr));
    dn2cpp_gc_store_ref(&c->gcnext, static_cast<Dn2CppCont*>(nullptr));
}

// Defined beside the pool condition variable. A continuation may be posted before
// its owner reaches the pool wait, so notification needs no lock or handshake.
static void dn2cpp_pool_wake_for_scheduler();

// Append `c` (already populated, incl. its owner) onto scheduler `s`'s run queue and
// wake a waiter. Safe to call from any thread, and safe after the owning thread has
// exited — the scheduler is never freed. Nothing then pumps that queue, so the
// continuation is simply never run: a spawned thread that awaits and returns before
// the task completes drops its post-await tail. (Real .NET resumes it on the pool,
// having captured no synchronization context.)
static void dn2cpp_sched_enqueue(Dn2CppScheduler* s, Dn2CppCont* c)
{
    dn2cpp_pending_cont_link(c); // rooted before the queue makes it poppable
    bool wakePool;
    {
        std::lock_guard<std::mutex> lk(s->mtx);
        wakePool = s->pool_worker;
        if (wakePool && !s->pool_local_principal.load(std::memory_order_relaxed))
        {
            // Join before publishing the continuation. The completing principal
            // may leave immediately after enqueue, so a zero-count window here
            // would let a blocked waiter declare deadlock on runnable pool work.
            s->pool_local_principal.store(true, std::memory_order_release);
            g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
        }
        dn2cpp_gc_store_ref(&c->next, static_cast<Dn2CppCont*>(nullptr));
        if (s->tail != nullptr)
            dn2cpp_gc_store_ref(&s->tail->next, c);
        else
            s->head = c;
        s->tail = c;
        s->cv.notify_one();
    }
    if (wakePool)
        dn2cpp_pool_wake_for_scheduler();
}

// Post a fresh resumption onto scheduler `s` (owner = `s`).
static void dn2cpp_sched_post_to(Dn2CppScheduler* s, void (*fn)(void*), void* state)
{
    auto* c = static_cast<Dn2CppCont*>(dn2cpp_alloc(sizeof(Dn2CppCont)));
    c->fn = fn;
    c->state = state;
    c->owner = s;
    dn2cpp_sched_enqueue(s, c);
}

void dn2cpp_sched_post(void (*fn)(void*), void* state)
{
    dn2cpp_sched_post_to(dn2cpp_sched_self(), fn, state);
}

// Run one queued resumption on THIS thread's queue; returns false when empty. The
// continuation runs outside the lock (it may post more work or block).
static bool dn2cpp_sched_run_one()
{
    Dn2CppScheduler* s = dn2cpp_sched_self();
    Dn2CppCont* c;
    {
        std::lock_guard<std::mutex> lk(s->mtx);
        c = s->head;
        if (c == nullptr)
            return false;
        s->head = c->next;
        if (s->head == nullptr)
            s->tail = nullptr;
    }
    // `c` (and through it the state) is now rooted by this frame; drop the global link.
    dn2cpp_pending_cont_unlink(c);
    c->fn(c->state);
    return true;
}

// Route a completed task's stolen continuation list to each continuation's owner
// scheduler (oldest first), reusing the existing nodes (no allocation). Runs on
// whatever thread completed the task; cross-thread continuations land on (and wake)
// the awaiting thread, same-thread ones land on the caller's own queue.
static void dn2cpp_fire_conts(Dn2CppCont* conts)
{
    // The list is built by prepending, so reverse it to preserve await order.
    Dn2CppCont* prev = nullptr;
    for (Dn2CppCont* c = conts; c != nullptr;)
    {
        Dn2CppCont* next = c->next;
        dn2cpp_gc_store_ref(&c->next, prev);
        prev = c;
        c = next;
    }
    for (Dn2CppCont* c = prev; c != nullptr;)
    {
        Dn2CppCont* next = c->next; // save before the node is relinked
        dn2cpp_sched_enqueue(c->owner, c);
        c = next;
    }
}

// Detach a completing task's continuation list under g_task_mtx (so a concurrent
// await-registration sees a settled status), then fire it outside the lock.
static void dn2cpp_task_complete(Dn2CppTask* t, int32_t status, uint64_t result, Dn2CppObject* exception)
{
    Dn2CppCont* conts;
    {
        std::lock_guard<std::mutex> lk(g_task_mtx);
        t->result = result;
        dn2cpp_gc_write_barrier(&t->result);
        dn2cpp_gc_store_ref(&t->exception, exception);
        // Settled last, with release: await-registration gates on it under this
        // lock, and the drain reads it — and then result/exception — without it.
        t->status.store(status, std::memory_order_release);
        conts = t->continuations;
        dn2cpp_gc_store_ref(&t->continuations, static_cast<Dn2CppCont*>(nullptr));
    }
    dn2cpp_fire_conts(conts);
}

void dn2cpp_task_set_result(Dn2CppTask* t, uint64_t result)
{
    dn2cpp_task_complete(t, DN2CPP_TASK_SUCCEEDED, result, t->exception);
}

void dn2cpp_task_set_exception(Dn2CppTask* t, Dn2CppObject* exception)
{
    dn2cpp_task_complete(t, DN2CPP_TASK_FAULTED, t->result, exception);
}

// Queue `fn` on a PENDING task, or answer false when `t` is already settled and the
// caller must dispose of the continuation itself. Never runs `fn`, and never runs
// anything under g_task_mtx.
static bool dn2cpp_task_try_queue_cont(Dn2CppTask* t, void (*fn)(void*), void* state)
{
    // Allocate the node before locking (keeps the critical section tiny). If the task
    // is already complete the node is dropped (GC-collected).
    auto* c = static_cast<Dn2CppCont*>(dn2cpp_alloc(sizeof(Dn2CppCont)));
    c->fn = fn;
    c->state = state;
    c->owner = dn2cpp_sched_self();
    std::lock_guard<std::mutex> lk(g_task_mtx);
    if (t->status != DN2CPP_TASK_PENDING)
        return false;
    dn2cpp_gc_store_ref(&c->next, t->continuations);
    dn2cpp_gc_store_ref(&t->continuations, c);
    return true;
}

void dn2cpp_task_on_completed(Dn2CppTask* t, void (*fn)(void*), void* state)
{
    if (!dn2cpp_task_try_queue_cont(t, fn, state))
        dn2cpp_sched_post(fn, state); // already complete: resume on our own next turn
}

// Registration with .NET's TaskContinuationOptions.ExecuteSynchronously: an
// already-settled `t` runs `fn` INLINE, on this thread, before returning. That is what
// makes Task.WhenAll/WhenAny over settled inputs complete before the call returns, as
// real .NET does; posting instead leaves the join PENDING until something turns the
// scheduler loop.
//
// Two invariants make inlining safe here, and both must survive any new caller:
//   * The call is outside g_task_mtx, so a callback that settles a task cannot deadlock.
//   * A task settling LATER fires through dn2cpp_fire_conts, which only ever enqueues,
//     so an inline callback cannot re-enter this on a task it just settled — the depth
//     is one callback, not the chain length of the combinators built over it.
// It is not the default: ContinueWith over a settled task must still be PENDING on
// return, and Task.Delay/await ordering is defined by the scheduler queue.
static void dn2cpp_task_on_completed_sync(Dn2CppTask* t, void (*fn)(void*), void* state)
{
    if (!dn2cpp_task_try_queue_cont(t, fn, state))
        fn(state);
}

// Invoke a no-arg System.Action delegate and its multicast chain. The continuation
// a custom suspending awaiter receives is an Action wrapping a boxed state machine's
// MoveNext (or any user Action); the uniform {target, method, prev} delegate layout
// lets one helper invoke any Action without the per-type dginvoke.
void dn2cpp_action_invoke(Dn2CppObject* action)
{
    if (action == nullptr)
        return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(action);
    if (dg->prev != nullptr)
        dn2cpp_action_invoke(dg->prev);
    reinterpret_cast<void (*)(Dn2CppObject*)>(dg->method)(dg->target);
}

static void dn2cpp_action_continuation(void* state)
{
    dn2cpp_action_invoke(static_cast<Dn2CppObject*>(state));
}

// Register a System.Action to run when the task completes — the runtime side of a
// user awaiter's TaskAwaiter.OnCompleted(Action) and of the Action-over-MoveNext
// continuation a custom suspending awaitable's await synthesizes.
void dn2cpp_task_on_completed_action(Dn2CppTask* t, Dn2CppObject* action)
{
    dn2cpp_task_on_completed(t, &dn2cpp_action_continuation, action);
}

// Action form of dn2cpp_sched_post — the delegate-continuation counterpart of
// awaiting Task.Yield(): run the Action on this thread's next scheduler pass.
void dn2cpp_sched_post_action(Dn2CppObject* action)
{
    dn2cpp_sched_post(&dn2cpp_action_continuation, action);
}

static void dn2cpp_complete_delay(void* p)
{
    dn2cpp_task_set_result(static_cast<Dn2CppTask*>(p), 0);
}

// Virtual-time timer queue for Task.Delay (per-thread, in the scheduler). There is no
// wall clock; instead a logical clock (virtual_now) advances only when the run
// queue is empty, jumping to the earliest pending timer. This makes concurrent delays
// complete in duration order — Delay(10) before Delay(50) — deterministically and
// instantly (no real sleeping), while ready work (Task.Yield, resumed awaiters)
// always runs first. Timers are owner-only: each thread's Task.Delay feeds its own
// queue and its own task_block advances its own clock, so no lock is needed here.
struct Dn2CppTimer
{
    Dn2CppTask* task;
    int64_t due;        // virtual time (ms) at which the delay completes
    int64_t seq;        // insertion order: ties at the same `due` fire FIFO
    Dn2CppTimer* next;
    // Global static-rooted list links (see g_pending_timers): the timer list hangs
    // off thread-local storage, so a pending entry — often the only path to its
    // delay task and any state machine awaiting it — must also be reachable
    // through scanned memory until it fires.
    Dn2CppTimer* gcprev;
    Dn2CppTimer* gcnext;
};

static void dn2cpp_pending_timer_link(Dn2CppTimer* e)
{
    std::lock_guard<std::mutex> lk(g_sched_pending_mtx);
    e->gcprev = nullptr;
    dn2cpp_gc_store_ref(&e->gcnext, g_pending_timers);
    if (g_pending_timers != nullptr)
        dn2cpp_gc_store_ref(&g_pending_timers->gcprev, e);
    g_pending_timers = e;
}

static void dn2cpp_pending_timer_unlink(Dn2CppTimer* e)
{
    std::lock_guard<std::mutex> lk(g_sched_pending_mtx);
    if (e->gcprev != nullptr)
        dn2cpp_gc_store_ref(&e->gcprev->gcnext, e->gcnext);
    else
        g_pending_timers = e->gcnext;
    if (e->gcnext != nullptr)
        dn2cpp_gc_store_ref(&e->gcnext->gcprev, e->gcprev);
    dn2cpp_gc_store_ref(&e->gcprev, static_cast<Dn2CppTimer*>(nullptr));
    dn2cpp_gc_store_ref(&e->gcnext, static_cast<Dn2CppTimer*>(nullptr));
}

// Advance virtual time to the earliest pending timer and complete every timer due at
// that instant (in insertion order), posting their continuations. Returns false when
// no timers remain or the earliest due lies beyond `limit` (a blocking drain passes
// INT64_MAX and keeps the classic jump-to-next-timer virtual clock; the frame pump
// passes "virtual_now + real elapsed ms" so a delay completes only once its real
// time has passed). Called only when this thread's run queue has drained.
static bool dn2cpp_sched_advance_timers(int64_t limit)
{
    Dn2CppScheduler* s = dn2cpp_sched_self();
    if (s->timers == nullptr)
        return false;
    int64_t minDue = s->timers->due;
    for (Dn2CppTimer* e = s->timers->next; e != nullptr; e = e->next)
        if (e->due < minDue)
            minDue = e->due;
    if (minDue > limit)
        return false;
    s->virtual_now = minDue;
    // Detach the due entries (insertion order preserved) and rebuild the rest.
    Dn2CppTimer* dueHead = nullptr;
    Dn2CppTimer* dueTail = nullptr;
    Dn2CppTimer* keepHead = nullptr;
    Dn2CppTimer* keepTail = nullptr;
    for (Dn2CppTimer* e = s->timers; e != nullptr;)
    {
        Dn2CppTimer* next = e->next;
        dn2cpp_gc_store_ref(&e->next, static_cast<Dn2CppTimer*>(nullptr));
        Dn2CppTimer** head = e->due == minDue ? &dueHead : &keepHead;
        Dn2CppTimer** tail = e->due == minDue ? &dueTail : &keepTail;
        if (*tail != nullptr)
            dn2cpp_gc_store_ref(&(*tail)->next, e);
        else
            *head = e;
        *tail = e;
        e = next;
    }
    s->timers = keepHead;
    s->timers_tail = keepTail;
    for (Dn2CppTimer* e = dueHead; e != nullptr;)
    {
        Dn2CppTimer* next = e->next;
        if (e->task->status == DN2CPP_TASK_PENDING) // skip a delay already canceled
            dn2cpp_task_set_result(e->task, 0);
        dn2cpp_pending_timer_unlink(e); // fired (its continuations are rooted now) — drop the entry
        e = next;
    }
    return true;
}

Dn2CppTask* dn2cpp_task_delay(int64_t ms)
{
    Dn2CppTask* t = dn2cpp_task_alloc();
    if (ms <= 0)
    {
        // Delay(0)/negative: ready immediately, like Task.Yield (no clock advance).
        dn2cpp_sched_post(&dn2cpp_complete_delay, t);
        return t;
    }
    Dn2CppScheduler* s = dn2cpp_sched_self();
    auto* e = static_cast<Dn2CppTimer*>(dn2cpp_alloc(sizeof(Dn2CppTimer)));
    e->task = t;
    e->due = s->virtual_now + ms;
    e->seq = s->timer_seq++;
    e->next = nullptr;
    dn2cpp_pending_timer_link(e);
    if (s->timers_tail != nullptr)
        dn2cpp_gc_store_ref(&s->timers_tail->next, e);
    else
        s->timers = e;
    s->timers_tail = e;
    return t;
}

// Task.WhenAll join state: the result task, the input tasks, a remaining-count,
// and how to materialize the result array.
struct Dn2CppWhenAllState
{
    Dn2CppTask* result;
    Dn2CppArrayRef* tasks;
    int32_t remaining;
    int32_t kind;
    int32_t elemSize;   // DN2CPP_WHENALL_STRUCT: byte size of each TStruct element
    // The TResult[] handle the emit arm supplied. The array is materialized inside
    // the completion callback, long after the lowering has returned, so a retag at
    // the call site is impossible and the handle has to RIDE here — without
    // it `(await Task.WhenAll(ts)).GetType()` reads System.Object[] for a reference
    // TResult, and for a value TResult a packed value array carries a REF-array tag,
    // which breaks covariance and IEnumerable<T> dispatch and not just the name.
    // Null degrades to the shared handle, which is what this did before.
    const Dn2CppTypeInfo* arrTi;
};

static void dn2cpp_task_set_canceled(Dn2CppTask* t);

// What a settled-but-failed task contributes to an aggregate built over it: its
// Task.Exception's inner set if it has one, else the single stored exception. .NET's
// WhenAll continuation AddRanges each input's Exception.InnerExceptions, so an input
// that is itself a WhenAll join FLATTENS — WhenAll(WhenAll(f1,f2), f3) has three
// inners, not two. A canceled task never mints the wrapper, so it contributes one.
static int32_t dn2cpp_task_fault_inner_count(Dn2CppTask* t)
{
    if (t->exceptionAggregate == nullptr)
        return 1;
    return dn2cpp_aggregate_inner_exceptions(t->exceptionAggregate, nullptr)->length;
}

// Append that set at `k`, answering the next free index.
static int32_t dn2cpp_task_fault_inners_copy(Dn2CppTask* t, Dn2CppArrayRef* out, int32_t k)
{
    if (t->exceptionAggregate == nullptr)
    {
        dn2cpp_gc_store_ref(&out->data[k++], t->exception);
        return k;
    }
    Dn2CppArrayRef* a = dn2cpp_aggregate_inner_exceptions(t->exceptionAggregate, nullptr);
    for (int32_t i = 0; i < a->length; i++)
        dn2cpp_gc_store_ref(&out->data[k++], a->data[i]);
    return k;
}

// Build the TResult[] from each input task's result slot and complete the WhenAll
// task — or propagate the outcome of an input that did not succeed.
static void dn2cpp_when_all_finish(Dn2CppWhenAllState* s)
{
    // A fault ANYWHERE outranks a cancellation anywhere: .NET's WhenAll is Faulted if
    // any input faulted, Canceled if none did and one was canceled. Without the second
    // arm a canceled input reads as RanToCompletion and its awaiter gets a result.
    // EVERY faulted input contributes, so the aggregate is sized in one pass and filled
    // in a second — a cancellation contributes nothing to it.
    int32_t n = s->tasks->length;
    bool canceled = false;
    int32_t total = 0;
    for (int32_t i = 0; i < n; i++)
    {
        auto* t = reinterpret_cast<Dn2CppTask*>(s->tasks->data[i]);
        if (t->status == DN2CPP_TASK_FAULTED)
            total += dn2cpp_task_fault_inner_count(t);
        else
            canceled = canceled || t->status == DN2CPP_TASK_CANCELED;
    }
    if (total > 0)
    {
        Dn2CppArrayRef* inner = dn2cpp_newarr_ref(total);
        int32_t k = 0;
        for (int32_t i = 0; i < n; i++)
        {
            auto* t = reinterpret_cast<Dn2CppTask*>(s->tasks->data[i]);
            if (t->status == DN2CPP_TASK_FAULTED)
                k = dn2cpp_task_fault_inners_copy(t, inner, k);
        }
        // Fill the Task.Exception slot while the join is still PENDING: the moment
        // set_exception publishes FAULTED, a get_Exception read would mint a
        // one-element wrapper if-absent, and the race closes without a lock.
        dn2cpp_gc_store_ref(&s->result->exceptionAggregate,
                            dn2cpp_aggregate_exception_new(inner));
        dn2cpp_task_set_exception(s->result, inner->data[0]); // await raises InnerExceptions[0]
        return;
    }
    if (canceled)
    {
        dn2cpp_task_set_canceled(s->result);
        return;
    }
    if (s->kind == DN2CPP_WHENALL_VOID) // non-generic WhenAll: no result array
    {
        dn2cpp_task_set_result(s->result, 0);
        return;
    }
    Dn2CppObject* arr;
    if (s->kind == DN2CPP_WHENALL_I4)
    {
        Dn2CppArrayI4* a = dn2cpp_newarr_i4_t(n, s->arrTi);
        for (int32_t i = 0; i < n; i++)
            a->data[i] = static_cast<int32_t>(reinterpret_cast<Dn2CppTask*>(s->tasks->data[i])->result);
        arr = reinterpret_cast<Dn2CppObject*>(a);
    }
    else if (s->kind == DN2CPP_WHENALL_REF)
    {
        Dn2CppArrayRef* a = dn2cpp_newarr_ref_t(n, s->arrTi);
        for (int32_t i = 0; i < n; i++)
            dn2cpp_gc_store_ref(&a->data[i], reinterpret_cast<Dn2CppObject*>(
                static_cast<uintptr_t>(reinterpret_cast<Dn2CppTask*>(s->tasks->data[i])->result)));
        arr = reinterpret_cast<Dn2CppObject*>(a);
    }
    else if (s->kind == DN2CPP_WHENALL_STRUCT)
    {
        // Each input's result slot holds a heap-boxed struct pointer
        // (dn2cpp_struct_result_box); copy each struct into a value array.
        Dn2CppArrayN* a = dn2cpp_newarr_n_t(n, s->elemSize, s->arrTi);
        for (int32_t i = 0; i < n; i++)
        {
            void* boxed = reinterpret_cast<void*>(static_cast<uintptr_t>(
                reinterpret_cast<Dn2CppTask*>(s->tasks->data[i])->result));
            std::memcpy(static_cast<char*>(a->data) + static_cast<size_t>(i) * s->elemSize,
                        boxed, static_cast<size_t>(s->elemSize));
        }
        arr = reinterpret_cast<Dn2CppObject*>(a);
    }
    else // DN2CPP_WHENALL_N8: long/ulong/double, the raw 8-byte result slot
    {
        Dn2CppArrayN* a = dn2cpp_newarr_n_t(n, 8, s->arrTi);
        for (int32_t i = 0; i < n; i++)
            reinterpret_cast<int64_t*>(a->data)[i] = static_cast<int64_t>(reinterpret_cast<Dn2CppTask*>(s->tasks->data[i])->result);
        arr = reinterpret_cast<Dn2CppObject*>(a);
    }
    dn2cpp_task_set_result(s->result, static_cast<uint64_t>(reinterpret_cast<uintptr_t>(arr)));
}

static void dn2cpp_when_all_one(void* p)
{
    auto* s = static_cast<Dn2CppWhenAllState*>(p);
    if (--s->remaining == 0)
        dn2cpp_when_all_finish(s);
}

static Dn2CppTask* dn2cpp_task_when_all_impl(Dn2CppArrayRef* tasks, int32_t kind, int32_t elemSize,
                                             const Dn2CppTypeInfo* arrTi)
{
    // Validate before allocating the state, as dn2cpp_task_when_any does: without this
    // a null array and a null element are raw dereferences (a crash where real .NET
    // answers with a catchable throw). The pair is INVERTED from WhenAny's — .NET gives
    // ArgumentNullException for the array but ArgumentException for an element here.
    if (tasks == nullptr)
        dn2cpp_throw_argument_null();
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (tasks->data[i] == nullptr)
            dn2cpp_throw_argument();
    }
    auto* s = static_cast<Dn2CppWhenAllState*>(dn2cpp_alloc(sizeof(Dn2CppWhenAllState)));
    dn2cpp_gc_store_ref(&s->result, dn2cpp_task_alloc());
    dn2cpp_gc_store_ref(&s->tasks, tasks);
    s->remaining = tasks->length;
    s->kind = kind;
    s->elemSize = elemSize;
    dn2cpp_gc_store_ref(&s->arrTi, arrTi);
    if (tasks->length == 0)
    {
        dn2cpp_when_all_finish(s); // no inputs: already done (empty array)
        return s->result;
    }
    // Synchronous registration: an input that is already settled decrements `remaining`
    // here, so an all-settled batch finishes before this returns and never touches the
    // scheduler. remaining can only reach 0 on the last input, so the loop is complete
    // when dn2cpp_when_all_finish runs.
    for (int32_t i = 0; i < tasks->length; i++)
        dn2cpp_task_on_completed_sync(reinterpret_cast<Dn2CppTask*>(tasks->data[i]), &dn2cpp_when_all_one, s);
    return s->result;
}

Dn2CppTask* dn2cpp_task_when_all(Dn2CppArrayRef* tasks, int32_t kind, const Dn2CppTypeInfo* arrTi)
{
    return dn2cpp_task_when_all_impl(tasks, kind, 0, arrTi);
}

// Task.WhenAll<TStruct>(Task<TStruct>[]) -> Task<TStruct[]>: each input's result is
// a heap-boxed struct; the join copies them into a value array of elemSize-byte
// elements.
Dn2CppTask* dn2cpp_task_when_all_struct(Dn2CppArrayRef* tasks, int32_t elemSize,
                                        const Dn2CppTypeInfo* arrTi)
{
    return dn2cpp_task_when_all_impl(tasks, DN2CPP_WHENALL_STRUCT, elemSize, arrTi);
}

// Task.WhenAny(Task[]) / WhenAny<T>(Task<T>[]): a Task<Task>/Task<Task<T>> that
// completes (always SUCCEEDED — WhenAny never faults; the winner's fault is
// observed through its own .Result) with the FIRST input task to complete. The
// continuation only receives its `state`, so each input gets its own entry that
// pairs the shared state with that input task; a `done` guard makes the first
// firing win and the rest no-op.
struct Dn2CppWhenAnyState
{
    Dn2CppTask* result;
    int32_t done;
};
struct Dn2CppWhenAnyEntry
{
    Dn2CppWhenAnyState* shared;
    Dn2CppTask* task;
};

static void dn2cpp_when_any_one(void* p)
{
    auto* e = static_cast<Dn2CppWhenAnyEntry*>(p);
    if (e->shared->done)
        return;
    e->shared->done = 1;
    dn2cpp_task_set_result(e->shared->result,
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(e->task)));
}

// The argument validation runs BEFORE the state allocation: dn2cpp_task_alloc would
// mint a Task nothing will ever settle, and since the rejection is a catchable throw
// the program keeps running with exactly the shape a later deadlock verdict has to
// reason about. The answers are real .NET's: an empty array is ArgumentException
// (ParamName "tasks"), a null array or a null element is ArgumentNullException.
Dn2CppTask* dn2cpp_task_when_any(Dn2CppArrayRef* tasks)
{
    if (tasks == nullptr)
        dn2cpp_throw_argument_null();
    if (tasks->length == 0)
        dn2cpp_throw_argument();
    // A null element is ArgumentNullException in real .NET, and it is checked in
    // its own pass ahead of the registration loop: half-registering the
    // continuations and then throwing would leave the surviving entries pointing
    // at a shared state whose result task nothing returns, i.e. a completion
    // callback that fires into an abandoned join. dn2cpp_task_wait_any rejects
    // the same element before it builds anything, for the same reason.
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (tasks->data[i] == nullptr)
            dn2cpp_throw_argument_null();
    }
    auto* s = static_cast<Dn2CppWhenAnyState*>(dn2cpp_alloc(sizeof(Dn2CppWhenAnyState)));
    dn2cpp_gc_store_ref(&s->result, dn2cpp_task_alloc());
    s->done = 0;
    for (int32_t i = 0; i < tasks->length; i++)
    {
        auto* e = static_cast<Dn2CppWhenAnyEntry*>(dn2cpp_alloc(sizeof(Dn2CppWhenAnyEntry)));
        e->shared = s;
        e->task = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
        // Synchronous registration, so a settled input wins the join before this
        // returns; the winner is then the FIRST settled input in array order, as on
        // real .NET. Stop once one has won: a continuation on a still-pending input
        // could only no-op, and would pin the finished join until that input settles.
        dn2cpp_task_on_completed_sync(e->task, &dn2cpp_when_any_one, e);
        if (s->done)
            break;
    }
    return s->result;
}

// Materialize an IEnumerable<Task<T>> into a ref array for WhenAll/WhenAny: the
// emitted interface-enumeration loop drives reflist_new + reflist_add per element,
// then reflist_to_array. GC-safe (see the header note).
Dn2CppRefList* dn2cpp_reflist_new()
{
    auto* l = static_cast<Dn2CppRefList*>(dn2cpp_alloc(sizeof(Dn2CppRefList)));
    l->data = nullptr;
    l->count = 0;
    l->cap = 0;
    return l;
}

void dn2cpp_reflist_add(Dn2CppRefList* l, Dn2CppObject* o)
{
    if (l->count == l->cap)
    {
        int32_t newCap = l->cap == 0 ? 4 : l->cap * 2;
        auto* nd = static_cast<Dn2CppObject**>(
            dn2cpp_alloc(static_cast<size_t>(newCap) * sizeof(Dn2CppObject*)));
        for (int32_t i = 0; i < l->count; i++)
            nd[i] = l->data[i];
        dn2cpp_gc_store_ref(&l->data, nd);
        l->cap = newCap;
    }
    dn2cpp_gc_store_ref(&l->data[l->count++], o);
}

// The shared System.Object[] tag these two leave on the result is deliberate: both build
// the INPUT normalization for the WhenAll/WhenAny/WaitAll/WaitAny combinators, which read
// only ->length and ->data. The array never becomes a managed value, so there is no
// GetType() to be wrong. The RESULT array, which does escape, carries its handle on the
// join state instead.
Dn2CppArrayRef* dn2cpp_reflist_to_array(Dn2CppRefList* l)
{
    Dn2CppArrayRef* a = dn2cpp_newarr_ref(l->count);
    for (int32_t i = 0; i < l->count; i++)
        dn2cpp_gc_store_ref(&a->data[i], l->data[i]);
    return a;
}

// A struct Task<T> result does not fit the 8-byte result slot — heap-copy it and
// store the pointer in the slot (the reader copies it back out by the same T).
// GC-safe: the buffer is a scanned dn2cpp_alloc allocation.
void* dn2cpp_struct_result_box(const void* src, int32_t size)
{
    void* p = dn2cpp_alloc(static_cast<size_t>(size));
    std::memcpy(p, src, static_cast<size_t>(size));
    return p;
}

// Copy a ReadOnlySpan<Task<T>>'s {reference,length} (the .NET 9+ params
// ReadOnlySpan<Task> combinator overload, lowered by Roslyn through an
// [InlineArray] of N tasks) into a fresh ref array for WhenAll/WhenAny.
Dn2CppArrayRef* dn2cpp_refspan_to_array(Dn2CppObject** data, int32_t len)
{
    Dn2CppArrayRef* a = dn2cpp_newarr_ref(len);
    for (int32_t i = 0; i < len; i++)
        dn2cpp_gc_store_ref(&a->data[i], data[i]);
    return a;
}

// Task.FromException<T>(ex) / Task.FromException(ex): a pre-completed faulted task
// carrying `ex`. No continuations to fire (fresh task); awaiting/.Result re-raises
// the stored exception via dn2cpp_task_throw_if_faulted.
Dn2CppTask* dn2cpp_task_from_exception(Dn2CppObject* exception)
{
    auto* t = dn2cpp_task_alloc();
    t->exception = exception;
    t->status.store(DN2CPP_TASK_FAULTED, std::memory_order_relaxed);
    return t;
}

// ---- cancellation ----

// The real OperationCanceledException type-info, registered from generated code so a
// canceled task / ThrowIfCancellationRequested throws an object catchable by a typed
// `catch (OperationCanceledException)` (pointer-identity match). Falls back to the
// bare Exception type if a program cancels without ever naming the type.
extern const Dn2CppType dn2cpp_operation_canceled_exception_type_obj;
static const Dn2CppTypeInfo dn2cpp_operation_canceled_exception_type =
    dn2cpp_ti_with_typeobject({ "System.OperationCanceledException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_operation_canceled_exception_type_obj);
const Dn2CppType dn2cpp_operation_canceled_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_operation_canceled_exception_type };
static const Dn2CppTypeInfo* s_canceled_exc_type = &dn2cpp_operation_canceled_exception_type;

void dn2cpp_set_canceled_exception_type(const Dn2CppTypeInfo* ti)
{
    if (ti != nullptr)
        s_canceled_exc_type = ti;
}

// The real TaskCanceledException type-info, registered beside the OCE one: a CANCELED
// *task* carries a TaskCanceledException (real .NET's Task.FromCanceled / canceled
// Task.Delay / TCS.SetCanceled all do), while the token-side throw keeps the plain
// OperationCanceledException. Null until registered — the fallback is the OCE identity
// above, so a program whose CoreLib never carried the type still works.
static const Dn2CppTypeInfo* s_task_canceled_exc_type = nullptr;

void dn2cpp_set_task_canceled_exception_type(const Dn2CppTypeInfo* ti)
{
    if (ti != nullptr)
        s_task_canceled_exc_type = ti;
}

static Dn2CppObject* dn2cpp_make_canceled_exception_of(const Dn2CppTypeInfo* ti,
                                                       const char* msg)
{
    // Floor at the exception prefix, not the bare header: the throw stamps the
    // trace slot, and the prefix fields are read off a caught OCE.
    size_t sz = ti->instanceSize > static_cast<int32_t>(sizeof(Dn2CppExceptionObject))
        ? static_cast<size_t>(ti->instanceSize) : sizeof(Dn2CppExceptionObject);
    auto* o = static_cast<Dn2CppObject*>(dn2cpp_alloc(sz)); // zero-filled -> empty fields
    o->type = ti;
    // The default message real .NET's parameterless ctor bakes in — observable
    // through Message and through the AggregateException a blocking wait wraps.
    dn2cpp_gc_store_ref(&reinterpret_cast<Dn2CppExceptionObject*>(o)->message,
        dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))));
    return o;
}

// The token-side OCE (ThrowIfCancellationRequested).
static Dn2CppObject* dn2cpp_make_canceled_exception()
{
    return dn2cpp_make_canceled_exception_of(s_canceled_exc_type, "The operation was canceled.");
}

// The task-side TCE (every CANCELED task transition below).
static Dn2CppObject* dn2cpp_make_task_canceled_exception()
{
    const Dn2CppTypeInfo* ti = s_task_canceled_exc_type;
    if (ti == nullptr)
        return dn2cpp_make_canceled_exception();
    return dn2cpp_make_canceled_exception_of(ti, "A task was canceled.");
}

[[noreturn]] void dn2cpp_throw_canceled()
{
    dn2cpp_throw(dn2cpp_make_canceled_exception());
}

static void dn2cpp_task_set_canceled(Dn2CppTask* t)
{
    dn2cpp_task_complete(t, DN2CPP_TASK_CANCELED, t->result, dn2cpp_make_task_canceled_exception());
}

// Task.FromCanceled(<T>) / ValueTask.FromCanceled(<T>): a pre-completed CANCELED
// task. No continuations to fire (fresh task, like dn2cpp_task_from_exception);
// awaiting re-raises the stored TaskCanceledException via
// dn2cpp_task_throw_if_faulted, and a blocking Wait/.Result wraps it
// (dn2cpp_task_block_wait).
Dn2CppTask* dn2cpp_task_from_canceled()
{
    auto* t = dn2cpp_task_alloc();
    dn2cpp_gc_store_ref(&t->exception, dn2cpp_make_task_canceled_exception());
    t->status.store(DN2CPP_TASK_CANCELED, std::memory_order_relaxed);
    return t;
}

// ---- TaskCompletionSource(<T>) -----------------------------------------------
// A TCS is modeled as the bare Dn2CppTask* it completes (get_Task is the
// identity), so the whole surface is these exactly-once transitions. Unlike
// dn2cpp_task_complete (whose callers settle a task they exclusively own), a
// TCS may race concurrent TrySet* calls — the PENDING check and the transition
// happen under the same g_task_mtx hold, so exactly one caller wins.
static int32_t dn2cpp_task_try_complete(Dn2CppTask* t, int32_t status, uint64_t result,
                                        Dn2CppObject* exception)
{
    Dn2CppCont* conts;
    {
        std::lock_guard<std::mutex> lk(g_task_mtx);
        if (t->status != DN2CPP_TASK_PENDING)
            return 0;
        t->result = result;
        dn2cpp_gc_write_barrier(&t->result);
        dn2cpp_gc_store_ref(&t->exception, exception);
        t->status.store(status, std::memory_order_release); // settled last, as above
        conts = t->continuations;
        dn2cpp_gc_store_ref(&t->continuations, static_cast<Dn2CppCont*>(nullptr));
    }
    dn2cpp_fire_conts(conts);
    return 1;
}

int32_t dn2cpp_task_try_set_result(Dn2CppTask* t, uint64_t result)
{
    return dn2cpp_task_try_complete(t, DN2CPP_TASK_SUCCEEDED, result, nullptr);
}

int32_t dn2cpp_task_try_set_exception(Dn2CppTask* t, Dn2CppObject* exception)
{
    return dn2cpp_task_try_complete(t, DN2CPP_TASK_FAULTED, 0, exception);
}

int32_t dn2cpp_task_try_set_canceled(Dn2CppTask* t)
{
    return dn2cpp_task_try_complete(t, DN2CPP_TASK_CANCELED, 0, dn2cpp_make_task_canceled_exception());
}

// The non-Try setters throw when the task has already settled, like real .NET.
[[noreturn]] static void dn2cpp_tcs_throw_settled()
{
    const char* msg = "An attempt was made to transition a task to a final state when it had already completed.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
        dn2cpp_string_from_utf8(msg, static_cast<int32_t>(std::strlen(msg))), nullptr));
}

void dn2cpp_tcs_set_result(Dn2CppTask* t, uint64_t result)
{
    if (!dn2cpp_task_try_set_result(t, result))
        dn2cpp_tcs_throw_settled();
}

void dn2cpp_tcs_set_exception(Dn2CppTask* t, Dn2CppObject* exception)
{
    if (!dn2cpp_task_try_set_exception(t, exception))
        dn2cpp_tcs_throw_settled();
}

void dn2cpp_tcs_set_canceled(Dn2CppTask* t)
{
    if (!dn2cpp_task_try_set_canceled(t))
        dn2cpp_tcs_throw_settled();
}

// One registration node on a source's LIFO list. A node is exactly one kind: a bound
// Task.Delay (task != null), a no-arg Action callback (callback != null), a state-carrying
// Action<object> callback (stateCallback != null, invoked with `state`), a state-AND-token
// Action<object, CancellationToken> callback (tokenCallback != null, invoked with `state`
// and this source's token), or a LINKED source (child != null). `source` lets
// dn2cpp_cts_unregister detach the node without the caller re-supplying it.
struct Dn2CppCancelReg
{
    Dn2CppCancelSource* source;  // owning source (for unregister)
    Dn2CppTask* task;            // non-null: a pending Delay to transition to CANCELED
    Dn2CppObject* callback;      // non-null: an Action to invoke on Cancel()
    Dn2CppObject* stateCallback; // non-null: an Action<object> to invoke with `state`
    Dn2CppObject* tokenCallback; // non-null: an Action<object, CancellationToken>
    Dn2CppObject* state;         // the object threaded to state/tokenCallback; may be null
    Dn2CppCancelSource* child;   // non-null: a LINKED source to cancel on Cancel()
    Dn2CppCancelReg* next;
};

// Serializes {canceled, disposed, regs, timerLive, timerDueNs} on every
// CancellationTokenSource so Cancel(), Register(), CancelAfter() and Dispose() from
// different threads cannot race. Callbacks always
// run OUTSIDE this lock (a callback may itself Register/Cancel/poll
// IsCancellationRequested without re-entering it), so the lock never wraps user
// code and cross-thread cancellation cannot self-deadlock.
static std::mutex& g_cts_mtx = dn2cpp_never_destroyed<std::mutex>();
// Every CancelAfter timer thread waits here, so a reschedule / Cancel / Dispose on any
// source wakes every waiter with one notify_all. One shared variable rather than one per
// source keeps Dn2CppCancelSource a plain GC block with no members needing a constructor
// (contrast Dn2CppManagedTimer, which is placement-new'd for exactly that reason); the
// spurious wakeups cost a predicate re-test on a handful of threads.
static std::condition_variable& g_cts_timer_cv = dn2cpp_never_destroyed<std::condition_variable>();

extern const Dn2CppType dn2cpp_cancel_source_type_obj;
const Dn2CppTypeInfo dn2cpp_cancel_source_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.CancellationTokenSource", nullptr, (int32_t)sizeof(Dn2CppCancelSource), nullptr, nullptr, 0 }, &dn2cpp_cancel_source_type_obj);
const Dn2CppType dn2cpp_cancel_source_type_obj = { { &dn2cpp_type_type }, &dn2cpp_cancel_source_type };

Dn2CppCancelSource* dn2cpp_cts_new()
{
    auto* s = static_cast<Dn2CppCancelSource*>(dn2cpp_alloc(sizeof(Dn2CppCancelSource)));
    s->type = &dn2cpp_cancel_source_type;
    s->canceled = 0;
    s->disposed = 0;
    s->timerLive = 0;
    s->timerDueNs = 0;
    s->regs = nullptr;
    return s;
}

Dn2CppCancelSource* dn2cpp_cts_canceled()
{
    Dn2CppCancelSource* s = dn2cpp_cts_new();
    s->canceled = 1;
    return s;
}

void dn2cpp_cts_cancel(Dn2CppCancelSource* src)
{
    if (src == nullptr)
        return;
    Dn2CppCancelReg* head;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (src->canceled)
            return;
        src->canceled = 1;
        // Detach the whole list under the lock; it is processed below without the
        // lock so callbacks can freely re-enter Register/Cancel/IsCancellationRequested.
        head = src->regs;
        dn2cpp_gc_store_ref(&src->regs, static_cast<Dn2CppCancelReg*>(nullptr));
    }
    // A pending CancelAfter timer has nothing left to do — wake it so it exits promptly
    // and releases the pinned root holding `src` (see dn2cpp_cts_cancel_after).
    g_cts_timer_cv.notify_all();
    // `regs` is prepend-ordered (newest first), so walking head->tail runs the
    // callbacks in LIFO registration order — matching real .NET.
    for (Dn2CppCancelReg* r = head; r != nullptr; r = r->next)
    {
        if (r->task != nullptr)
        {
            // A pending Task.Delay bound to this source: its timer entry is skipped
            // once the task leaves PENDING.
            if (r->task->status == DN2CPP_TASK_PENDING)
                dn2cpp_task_set_canceled(r->task);
        }
        else if (r->callback != nullptr)
        {
            dn2cpp_action_invoke(r->callback);
        }
        else if (r->stateCallback != nullptr)
        {
            dn2cpp_paramthread_invoke(r->stateCallback, r->state);
        }
        else if (r->tokenCallback != nullptr)
        {
            dn2cpp_tokenthread_invoke(r->tokenCallback, r->state, Dn2CppCancelToken{ src });
        }
        else if (r->child != nullptr)
        {
            // A LINKED source (CancellationTokenSource.CreateLinkedTokenSource): cancelling
            // a parent cancels the child, which cascades to the child's own children. Safe
            // to recurse here — this sweep runs OUTSIDE g_cts_mtx, so the nested Cancel()
            // takes the lock cleanly, and a source already canceled returns immediately, so
            // a diamond (two parents linked to one child) cancels it exactly once.
            dn2cpp_cts_cancel(r->child);
        }
    }
}

// Attach `child` to `parent`: cancelling the parent cancels the child. An already-canceled
// parent cancels the child now — a linked source must never be born un-canceled under a
// canceled parent, which is the whole contract .NET's linked source has. A null parent
// (CancellationToken.None) can never cancel, so there is nothing to attach.
static void dn2cpp_cts_link_one(Dn2CppCancelSource* child, Dn2CppCancelSource* parent)
{
    if (parent == nullptr || child == nullptr)
        return;
    auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
    r->source = parent;
    r->task = nullptr;
    r->callback = nullptr;
    r->stateCallback = nullptr;
    r->tokenCallback = nullptr;
    r->state = nullptr;
    r->child = child;
    r->next = nullptr;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (!parent->canceled)
        {
            dn2cpp_gc_store_ref(&r->next, parent->regs);
            dn2cpp_gc_store_ref(&parent->regs, r);
            return;
        }
    }
    // Already canceled: cascade now, outside the lock (same shape as dn2cpp_cts_register's
    // already-canceled arm — the decision is taken under the lock, so a racing Cancel()
    // cascades either here or in its sweep, never both).
    dn2cpp_cts_cancel(child);
}

Dn2CppCancelSource* dn2cpp_cts_link2(Dn2CppCancelSource* a, Dn2CppCancelSource* b)
{
    Dn2CppCancelSource* c = dn2cpp_cts_new();
    dn2cpp_cts_link_one(c, a);
    dn2cpp_cts_link_one(c, b);
    return c;
}

Dn2CppCancelSource* dn2cpp_cts_link_array(Dn2CppArrayN* tokens)
{
    Dn2CppCancelSource* c = dn2cpp_cts_new();
    if (tokens == nullptr)
        return c;
    for (int32_t i = 0; i < tokens->length; i++)
    {
        auto* t = reinterpret_cast<Dn2CppCancelToken*>(
            tokens->data + static_cast<size_t>(i) * static_cast<size_t>(tokens->elemSize));
        dn2cpp_cts_link_one(c, t->source);
    }
    return c;
}

static int64_t dn2cpp_steady_now_ns(); // defined with CancelAfter's timer below

int32_t dn2cpp_cts_is_cancelled(Dn2CppCancelSource* src)
{
    if (src == nullptr)
        return 0;
#ifdef __EMSCRIPTEN__
    // The wasm CancelAfter arm spawns no timer thread (see dn2cpp_cts_cancel_after's
    // Emscripten arm, which owns the soundness argument); an armed wall-clock deadline
    // fires HERE instead, at the token's one observation funnel — both
    // IsCancellationRequested properties, ThrowIfCancellationRequested and
    // Task.Delay(ct)'s born-canceled check all route through this function, so a poll
    // past the deadline observes exactly what the timer thread's fire would have made
    // visible. The fire runs OUTSIDE the lock like every other cancel: it walks the
    // registration chain, and a callback may re-enter Register/Cancel/this very poll.
    bool fire = false;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (src->canceled)
            return 1;
        if (!src->disposed && src->timerDueNs != 0 && dn2cpp_steady_now_ns() >= src->timerDueNs)
        {
            src->timerDueNs = 0;
            fire = true;
        }
    }
    if (!fire)
        return 0;
    dn2cpp_cts_cancel(src);
    return 1;
#else
    std::lock_guard<std::mutex> lk(g_cts_mtx);
    return src->canceled ? 1 : 0;
#endif
}

Dn2CppCancelReg* dn2cpp_cts_register(Dn2CppCancelSource* src, Dn2CppObject* callback)
{
    // CancellationToken.None (null source) never cancels, so a registered callback
    // could never fire — drop it (matching .NET's default, no-op registration).
    if (src == nullptr)
        return nullptr;
    // Allocate the node before locking so the critical section is just a few stores.
    auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
    r->source = src;
    r->task = nullptr;
    r->callback = callback;
    r->stateCallback = nullptr;
    r->tokenCallback = nullptr;
    r->state = nullptr;
    r->child = nullptr;
    r->next = nullptr;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (!src->canceled)
        {
            dn2cpp_gc_store_ref(&r->next, src->regs);
            dn2cpp_gc_store_ref(&src->regs, r);
            return r;
        }
    }
    // Already canceled: run synchronously on the caller's thread, outside the lock.
    // The `canceled` decision is taken under the lock, so a concurrent Cancel() runs
    // this callback either here or in its sweep — exactly once, never both.
    dn2cpp_action_invoke(callback);
    return nullptr;
}

// CancellationToken.Register(Action<object> callback, object state): the state-carrying
// sibling of dn2cpp_cts_register. Same LIFO/already-canceled discipline; the only
// difference is the node fires callback(state) — through the one-arg invoker — rather
// than a no-arg Action.
Dn2CppCancelReg* dn2cpp_cts_register_state(Dn2CppCancelSource* src, Dn2CppObject* callback, Dn2CppObject* state)
{
    if (src == nullptr)
        return nullptr;
    auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
    r->source = src;
    r->task = nullptr;
    r->callback = nullptr;
    r->stateCallback = callback;
    r->tokenCallback = nullptr;
    r->state = state;
    r->child = nullptr;
    r->next = nullptr;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (!src->canceled)
        {
            dn2cpp_gc_store_ref(&r->next, src->regs);
            dn2cpp_gc_store_ref(&src->regs, r);
            return r;
        }
    }
    dn2cpp_paramthread_invoke(callback, state);
    return nullptr;
}

// Invoke a two-arg Action<object, CancellationToken> delegate (and its multicast chain),
// mirroring dn2cpp_paramthread_invoke with the token appended. The token is a
// one-pointer struct passed BY VALUE, exactly as an emitted delegate invoke passes it.
void dn2cpp_tokenthread_invoke(Dn2CppObject* del, Dn2CppObject* arg, Dn2CppCancelToken tok)
{
    if (del == nullptr)
        return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr)
        dn2cpp_tokenthread_invoke(dg->prev, arg, tok);
    reinterpret_cast<void (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppCancelToken)>(dg->method)(
        dg->target, arg, tok);
}

// CancellationToken.Register/UnsafeRegister(Action<object, CancellationToken> callback,
// object state): the sibling of dn2cpp_cts_register_state whose node fires
// callback(state, token) — the token being this very source's, which is what .NET hands
// the callback. Same LIFO/already-canceled discipline as its two siblings; the ONLY
// difference between Register and UnsafeRegister is whether the ExecutionContext is
// captured and flowed, and dn2cpp flows none (ExecutionContext.Capture lowers to the null
// "nothing to flow" encoding — CoreIntrinsics.MdExecutionContextCapture), so both spellings
// land here and the pair cannot drift.
Dn2CppCancelReg* dn2cpp_cts_register_state_token(Dn2CppCancelSource* src, Dn2CppObject* callback,
                                                 Dn2CppObject* state)
{
    if (src == nullptr)
        return nullptr;
    auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
    r->source = src;
    r->task = nullptr;
    r->callback = nullptr;
    r->stateCallback = nullptr;
    r->tokenCallback = callback;
    r->state = state;
    r->child = nullptr;
    r->next = nullptr;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (!src->canceled)
        {
            dn2cpp_gc_store_ref(&r->next, src->regs);
            dn2cpp_gc_store_ref(&src->regs, r);
            return r;
        }
    }
    dn2cpp_tokenthread_invoke(callback, state, Dn2CppCancelToken{ src });
    return nullptr;
}

void dn2cpp_cts_unregister(Dn2CppCancelReg* reg)
{
    if (reg == nullptr || reg->source == nullptr)
        return;
    Dn2CppCancelSource* src = reg->source;
    std::lock_guard<std::mutex> lk(g_cts_mtx);
    for (Dn2CppCancelReg** pp = &src->regs; *pp != nullptr; pp = &(*pp)->next)
    {
        if (*pp == reg)
        {
            dn2cpp_gc_store_ref(pp, reg->next);
            return;
        }
    }
}

// ---- CancelAfter's timer -----------------------------------------------------------
//
// **A timer thread must never hold its source only in the closure it was spawned with.**
// `std::thread([src]{ ... })` puts the sole copy of `src` in the thread's state block,
// which is `operator new` memory: neither a GC object nor a registered root range. Being
// GC-registered roots a thread's stack and callee-saved registers; it does NOT root a
// heap block the thread merely points into. Whether a copy of `src` survives in a
// register across a multi-second sleep is a codegen accident, so the root below is the
// only thing that makes the wait sound.
//
// The root is a one-word `dn2cpp_alloc_pinned` cell (GC_MALLOC_UNCOLLECTABLE: scanned by
// the collector, never collected — same device as a GCHandle cell), allocated by the
// SCHEDULING thread, where `src` is provably reachable. Allocating it inside the timer
// thread would leave the gap between spawn and first instruction wide open. The thread
// reads `*root` on every iteration and never keeps its own copy, so no unscanned copy of
// a GC pointer exists at any point; it releases the cell — under RAII, so a callback that
// throws cannot leak it — only after the last use of the source.
//
// At most ONE timer thread exists per source, and its deadline lives on the source, so a
// second CancelAfter reschedules by overwriting `timerDueNs` and waking the waiter — real
// .NET's `_timer.Change`, where a later CancelAfter replaces the earlier one in both
// directions. Cancel() and Dispose() wake it too, so the thread (and its root) go away as
// soon as the source can no longer fire rather than at the original deadline.
struct Dn2CppCtsTimerRoot
{
    Dn2CppCancelSource** cell;
    ~Dn2CppCtsTimerRoot()
    {
        *cell = nullptr;
        dn2cpp_free_pinned(cell);
    }
};

static int64_t dn2cpp_steady_now_ns()
{
    return std::chrono::duration_cast<std::chrono::nanoseconds>(
               std::chrono::steady_clock::now().time_since_epoch()).count();
}

// Leaving the settler set on the way out of the timer thread, however it leaves (an early
// return, the fire, an unwind). Declared BEFORE the root below so it is destroyed AFTER it:
// the departure must be the last thing this thread does, because until the cancel and the
// root release are both behind it, a callback it is still running could settle a task
// somebody is blocked on. Going through dn2cpp_principal_left is what makes that departure
// DELIVERED rather than noticed — a drain that parked because of this thread sleeps in an
// untimed wait, and only the wake on the counter's transition to zero ends it.
struct Dn2CppCtsTimerPrincipal
{
    ~Dn2CppCtsTimerPrincipal() { dn2cpp_principal_left(g_live_timer_threads); }
};

static void dn2cpp_cts_timer_thread(Dn2CppCancelSource** cell)
{
    Dn2CppGCThread guard;             // register the stack; the cell is what roots the source
    Dn2CppCtsTimerPrincipal principal; // leaves the settler set last (see above)
    Dn2CppCtsTimerRoot root{ cell };  // released last, and on unwind
    t_on_timer_thread = true;         // this thread is its own deadlock-test exemption
    for (;;)
    {
        std::unique_lock<std::mutex> lk(g_cts_mtx);
        Dn2CppCancelSource* s = *cell;   // always re-read: never cache a GC pointer here
        if (s->canceled || s->disposed || s->timerDueNs == 0)
        {
            s->timerLive = 0;
            return;
        }
        int64_t due = s->timerDueNs;
        int64_t now = dn2cpp_steady_now_ns();
        if (now >= due)
        {
            s->timerDueNs = 0;
            s->timerLive = 0;
            lk.unlock();
            // `s` is still rooted by the cell, which ~Dn2CppCtsTimerRoot releases after
            // this returns — the walk over the registration chain needs it live.
            dn2cpp_cts_cancel(s);
            return;
        }
        g_cts_timer_cv.wait_for(lk, std::chrono::nanoseconds(due - now));
    }
}

// CancellationTokenSource.CancelAfter(delay): cancel `src` after `ms` milliseconds on the
// source's own timer thread — the same per-timer-thread device as System.Threading.Timer,
// and distinct from the scheduler's virtual clock (which a synchronous program never
// pumps). On Emscripten there is no thread to spawn: the arm below records the deadline
// and dn2cpp_cts_is_cancelled fires it lazily at the poll (reasoning at that arm).
// Rescheduling and rooting are described above. A null source
// (CancellationToken.None never times out) is a no-op, and so is a source already canceled
// or disposed. HttpClient.Timeout's default 100 s reaches here through
// PrepareCancellationTokenSource.
//
// Delay contract, measured against real .NET: ms == -1 is Timeout.Infinite and disarms
// without ever cancelling; anything outside [-1, Timer.MaxSupportedTimeout] is
// ArgumentOutOfRangeException — only the TimeSpan overload can exceed the ceiling, and
// checking it is also what keeps the deadline arithmetic below inside int64. ms == 0
// cancels here, synchronously, where real .NET posts to its timer queue and cancels a
// moment later — a declared divergence, because the only observable difference is whether
// the very next statement sees IsCancellationRequested, which is a race on real .NET too.
void dn2cpp_cts_cancel_after(Dn2CppCancelSource* src, int64_t ms)
{
    if (ms < -1 || ms > 4294967294LL) // Timer.MaxSupportedTimeout
        dn2cpp_throw_argument_out_of_range();
    if (src == nullptr)
        return;
    if (ms == 0)
    {
        dn2cpp_cts_cancel(src);
        return;
    }
    Dn2CppCancelSource** cell = nullptr;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (src->canceled || src->disposed)
            return;
        src->timerDueNs = ms < 0 ? 0 : dn2cpp_steady_now_ns() + ms * 1000000LL;
#ifndef __EMSCRIPTEN__
        if (src->timerDueNs != 0 && !src->timerLive)
        {
            cell = static_cast<Dn2CppCancelSource**>(dn2cpp_alloc_pinned(sizeof(Dn2CppCancelSource*)));
            dn2cpp_gc_store_ref(cell, src);
            src->timerLive = 1;
        }
#endif
    }
    // Emscripten: ARM WITH NO THREAD. The deadline is recorded above and nothing is
    // spawned — `cell` stays null, so the spawn block below never runs. Without this arm
    // the std::thread ctor's throw ("thread constructor failed: Not supported") escapes
    // every managed catch and aborts the module — and HttpClient arms its DEFAULT
    // 100-second timeout through here inside Send, BEFORE the handler runs, so a naive
    // `new HttpClient()` died
    // of the thread carve-out without ever reaching the named HTTP one.
    //
    // Sound because wasm is single-threaded and cooperative: a deadline can only be
    // acted on inside a runtime entry point the program itself calls, and the complete
    // set of those is the dn2cpp_cts_is_cancelled funnel, whose Emscripten arm fires an
    // expired deadline on the spot. A timer thread could make nothing visible that the
    // poll-site fire does not. Real .NET's own single-threaded-wasm shape is the same.
    //
    // The VIRTUAL clock must NOT carry this deadline: dn2cpp_sched_advance_timers jumps
    // virtual_now to the earliest pending timer whenever a blocking drain runs out of
    // ready work, so a wall deadline registered there fires observably early.
    //
    // Named residue: a registered callback that is the only settler of a blocked wait
    // never fires (the drain blocks without polling the token) and gets the
    // defeated-wait report. g_live_timer_threads is untouched here for the same
    // reason — no principal exists, and counting one would sleep such a wait forever.
    if (cell != nullptr)
    {
        // Join the settler set BEFORE the thread exists: the caller's very next statement
        // may be the Wait() this timer is going to settle, and the timer thread has not
        // necessarily run an instruction by then — an empty set read in that window is the
        // false verdict this counting exists to prevent.
        g_live_timer_threads.fetch_add(1, std::memory_order_acq_rel);
        try
        {
            std::thread(dn2cpp_cts_timer_thread, cell).detach();
        }
        catch (...)
        {
            // No thread was started — a native host can refuse one (wasm never gets here:
            // its arm above never allocates the cell, so the spawn is skipped, not
            // attempted-and-caught). Undo the arm: leaving
            // timerLive set would turn every later CancelAfter on this source into a
            // notify nothing is listening for, and leaving the cell allocated would pin
            // the source for the life of the process — and leaving the +1 would disarm
            // the defeated-wait report for the rest of the process, so every later
            // defeated wait would hang instead of reporting. Nothing decrements it: the
            // thread that would have is the one that does not exist.
            {
                std::lock_guard<std::mutex> lk(g_cts_mtx);
                src->timerLive = 0;
                src->timerDueNs = 0;
            }
            *cell = nullptr;
            dn2cpp_free_pinned(cell);
            dn2cpp_principal_left(g_live_timer_threads);
            throw;
        }
    }
    else
        g_cts_timer_cv.notify_all();   // reschedule (or disarm) an already-running timer
}

// CancellationTokenSource.Dispose(): the source is GC-managed, so there is nothing to
// free — what Dispose releases is the pending CancelAfter, which real .NET stops (a
// disposed source never fires, and `using (var cts = ...)` around an awaited call relies
// on exactly that). The timer thread wakes on the flag, exits, and drops its root.
//
// Residue, deliberate: real .NET additionally makes Cancel(), CancelAfter() and .Token
// throw ObjectDisposedException on a disposed source. dn2cpp does not — `disposed` only
// disarms — because that surface turns a use-after-dispose that is harmless here into a
// throw, and nothing in the corpus asks for it. So on a disposed source CancelAfter() is
// a no-op (it can no longer fire either way, which is the part that matters), an explicit
// Cancel() still cancels, and .Token and IsCancellationRequested keep answering — the
// last of these matching real .NET, which leaves IsCancellationRequested readable.
void dn2cpp_cts_dispose(Dn2CppCancelSource* src)
{
    if (src == nullptr)
        return;
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        src->disposed = 1;
        src->timerDueNs = 0;
    }
    g_cts_timer_cv.notify_all();
}

Dn2CppCancelSource* dn2cpp_cts_new_after(int64_t ms)
{
    Dn2CppCancelSource* s = dn2cpp_cts_new();
    dn2cpp_cts_cancel_after(s, ms);
    return s;
}

Dn2CppTask* dn2cpp_task_delay_ct(int64_t ms, Dn2CppCancelSource* src)
{
    // Already-canceled source: the delay is born canceled (no timer scheduled).
    if (src != nullptr && dn2cpp_cts_is_cancelled(src))
    {
        Dn2CppTask* t = dn2cpp_task_alloc();
        dn2cpp_task_set_canceled(t);
        return t;
    }
    Dn2CppTask* t = dn2cpp_task_delay(ms);
    if (src != nullptr)
    {
        auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
        r->source = src;
        r->task = t;
        r->callback = nullptr;
        r->stateCallback = nullptr;
        r->tokenCallback = nullptr;
        r->state = nullptr;
        r->child = nullptr;
        bool raced;
        {
            std::lock_guard<std::mutex> lk(g_cts_mtx);
            raced = src->canceled != 0;  // a Cancel() between the check above and here
            if (!raced)
            {
                dn2cpp_gc_store_ref(&r->next, src->regs);
                dn2cpp_gc_store_ref(&src->regs, r);
            }
        }
        if (raced)
            dn2cpp_task_set_canceled(t);
    }
    return t;
}

// A continuation that does nothing: the one-shot "waker" task_block registers so a
// cross-thread completion of `t` (with no other awaiter) still pushes to and wakes
// this thread's queue.
static void dn2cpp_noop_cont(void*) {}

// The diagnosis a defeated wait carries, wherever it surfaces. Kept as one string so
// the message is identical whether it aborts (a drain with no managed frame under it)
// or is thrown (Task.Wait / .Result / GetResult, which have one).
// The parenthetical is the principal set, item by item: it names every question the
// verdict asked, so a reader can tell which one they expected to answer yes. Adding a
// principal without adding its clause here makes the diagnosis quietly incomplete.
static const char* const kDn2CppDeadlockMsg =
    "async: deadlock — task never completes (no runnable continuations on this thread, "
    "no pool work in flight, no other live user thread, no armed timer). "
    "A task is only settled by "
    "something that runs: start the Thread / Task.Run / RunSynchronously that completes "
    "it before blocking on it, and do not block a thread on work only that thread could do.";

// The defeated-wait report, printed BEFORE the throw and never gated. A caller is free to
// catch the exception — a game host boundary should — and a swallowed exception leaves no
// trace at all, which is how a loud abort would otherwise degrade into a silent stall:
// worse to operate than the abort it replaced. So the diagnosis reaches stderr on its own,
// once per defeat, whatever the program does with the exception. Deliberately not behind
// an env var: a diagnostic nobody turns on is one nobody reads (AGENTS.md's gated-assert
// rule), and this only ever prints on a wait that provably cannot be satisfied.
static void dn2cpp_report_defeated_wait()
{
    std::fprintf(stderr, "dn2cpp: %s\n", kDn2CppDeadlockMsg);
    std::fflush(stderr);
}

// Drive THIS thread's cooperative scheduler until `t` settles, WITHOUT re-raising a
// fault — the caller decides what to do with the settled status. Returns false if the
// wait is unsatisfiable: nothing runnable here, no pool work in flight (global items or
// worker-local scheduler work), no other live user thread and no armed timer,
// so no principal can ever settle `t`. Shared by the public blocking
// get (dn2cpp_task_block, which throws) and the Task.Run(Func<Task>) unwrap worker
// (which aborts — see dn2cpp_task_drain). The unwrap worker keeps its outer task counted
// in g_inflight_async_tasks while draining, so the guard never fires spuriously while
// the inner task is still in flight.
static bool dn2cpp_task_drain_settle(Dn2CppTask* t)
{
    bool waker_registered = false;
    while (t->status == DN2CPP_TASK_PENDING)
    {
        // Drain ready work first; only when nothing is runnable do we advance the
        // virtual clock to the next Task.Delay.
        if (dn2cpp_sched_run_one())
            continue;
        if (dn2cpp_sched_advance_timers(INT64_MAX))
            continue;
        // Nothing runnable on this thread. Either another principal will still settle
        // `t` — a pool worker (Task.Run / Start / pool work in flight) or a live user
        // thread (RunSynchronously on a cold task, a TaskCompletionSource set from a
        // thread of the program's own) — in which case we sleep until it posts a
        // continuation here and wakes our cv, or there is no such principal and the
        // wait can never be satisfied.
        if (!dn2cpp_task_settler_exists())
        {
            // Re-probe before the verdict: a departing settler settles tasks and
            // enqueues continuations BEFORE it leaves the count, so a zero read
            // (acquire) makes both visible here. Only "queue still empty and `t`
            // still pending" proves no one holds a claim that could settle `t`.
            if (dn2cpp_sched_run_one())
                continue;
            if (t->status != DN2CPP_TASK_PENDING)
                break;
            return false;
        }
        if (!waker_registered)
        {
            dn2cpp_task_on_completed(t, &dn2cpp_noop_cont, nullptr);
            waker_registered = true;
        }
        Dn2CppScheduler* s = dn2cpp_sched_self();
        std::unique_lock<std::mutex> lk(s->mtx);
        // The predicate carries the principal set as well as the task, so the wait ends
        // the moment the LAST principal leaves — dn2cpp_sched_wake_all, called on that
        // transition, is what delivers it. Without that term the wait is a hang: nothing
        // about a principal exiting touches `t`, so a notify would find the old predicate
        // false and go straight back to sleep. This is an untimed wait on purpose: a
        // periodic re-test would make every blocked wait in every program pay for a case
        // that is exactly detectable, and behaviour outside the defeated case must stay
        // bit-for-bit what it was before the principal set gained its second member.
        if (s->head == nullptr && t->status == DN2CPP_TASK_PENDING)
            s->cv.wait(lk, [&] {
                return s->head != nullptr || t->status != DN2CPP_TASK_PENDING
                    || !dn2cpp_task_settler_exists();
            });
    }
    return true;
}

// The abort-on-defeat drain, for the callers with no managed frame beneath them: the
// pool worker's unwrap/nested arms, where the drain runs at the top of a worker's stack.
//
// This one stays an abort. dn2cpp_task_throw_deadlock below is the same verdict as a
// catchable InvalidOperationException, and every caller with a managed frame under it
// uses that one; here there is none, so a throw would unwind out of the worker
// entrypoint into the C++ runtime with no handler — a crash with the diagnosis thrown
// away. The other aborts in this file are parity: real .NET also terminates the process
// for an unhandled exception on a thread or a pool item.
static void dn2cpp_task_drain(Dn2CppTask* t)
{
    if (!dn2cpp_task_drain_settle(t))
        dn2cpp_fail(kDn2CppDeadlockMsg);
}

// The defeated-wait report for a caller that DOES have a managed frame beneath it.
//
// INVARIANT — a deliberate divergence from real .NET, do not "fix" it into a hang:
// real .NET has no deadlock detector, so a Wait() nothing can satisfy blocks forever
// there. dn2cpp throws instead, because a transpiled game is the wrong place to lose a
// frame silently — one wedged worker in a Godot _Process would look like a freeze with
// nothing named. The throw is catchable (InvalidOperationException, the closest real
// type for "this wait can never complete") so a host boundary can log and degrade
// instead of taking the process with it, and it carries the full diagnosis: what was
// missing and what to do about it. What it must NEVER become is a silent hang: the
// verdict is only reached with no principal alive, and the wait it follows is an untimed
// `s->cv.wait(lk, pred)` whose predicate carries the principal set — so the last
// principal's exit cannot leave that wait asleep forever, not because anybody re-tests on
// a timer but because dn2cpp_principal_left wakes every scheduler in the registry
// (dn2cpp_sched_wake_all) on the counter's transition to zero. The exactness is the point:
// no blocked wait in any program pays a periodic re-test for a case that is detectable
// precisely when it happens.
[[noreturn]] static void dn2cpp_task_throw_deadlock()
{
    dn2cpp_report_defeated_wait(); // the exception may be swallowed; the line is not
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
        dn2cpp_string_from_utf8(kDn2CppDeadlockMsg,
                                static_cast<int32_t>(std::strlen(kDn2CppDeadlockMsg))),
        nullptr));
}

Dn2CppTask* dn2cpp_task_block(Dn2CppTask* t)
{
    if (!dn2cpp_task_drain_settle(t))
        dn2cpp_task_throw_deadlock();
    dn2cpp_task_throw_if_faulted(t);
    return t;
}

// The BLOCKING-WAIT funnel: Task.Wait()/Wait(timeout)/Task<T>.Result.
// Real .NET wraps a fault or cancellation in a fresh AggregateException here —
// the wrap is the contract of the blocking wait, not of the task, so this is the
// one place it happens; await / GetAwaiter().GetResult() / ValueTask.Result go
// through dn2cpp_task_block above and re-raise the stored exception unwrapped.
// A fresh throw (not a rethrow): the wrapper is minted now, so the trace stamped
// here is the wait's, while the inner exception keeps the original's — exactly
// real .NET's EDI split. The deadlock verdict stays dn2cpp's own unwrapped
// InvalidOperationException: it is a diagnosis of THIS wait, not a task failure.
Dn2CppTask* dn2cpp_task_block_wait(Dn2CppTask* t)
{
    if (!dn2cpp_task_drain_settle(t))
        dn2cpp_task_throw_deadlock();
    if (t->status == DN2CPP_TASK_FAULTED || t->status == DN2CPP_TASK_CANCELED)
    {
        Dn2CppArrayRef* inner = dn2cpp_newarr_ref(dn2cpp_task_fault_inner_count(t));
        dn2cpp_task_fault_inners_copy(t, inner, 0);
        dn2cpp_throw(dn2cpp_aggregate_exception_new(inner));
    }
    return t;
}

// Task.WaitAll(Task[]): block until every input settles, then raise. Real .NET waits
// for ALL of them before throwing and reports every failure in one AggregateException
// (a single faulted input still gets a wrapper), so the drain loop must not re-raise as
// it goes — it settles first and collects after. Cancellation counts as a failure, like
// .NET's WaitAllCore. Every FAULTED/CANCELED task carries an exception object by
// construction (the three CANCELED transitions all install
// dn2cpp_make_task_canceled_exception), so the collected array holds no nulls.
void dn2cpp_task_wait_all(Dn2CppArrayRef* tasks)
{
    if (tasks == nullptr)
        dn2cpp_throw_argument_null();
    // The null-element scan runs AHEAD of the drain: .NET validates the whole array
    // before it waits on anything, so WaitAll([pending, null]) is ArgumentException
    // rather than a block on the pending input that never sees the null.
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (tasks->data[i] == nullptr)
            dn2cpp_throw_argument();
    }
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (!dn2cpp_task_drain_settle(reinterpret_cast<Dn2CppTask*>(tasks->data[i])))
            dn2cpp_task_throw_deadlock();
    }
    int32_t total = 0;
    for (int32_t i = 0; i < tasks->length; i++)
    {
        auto* t = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
        if (t->status == DN2CPP_TASK_FAULTED || t->status == DN2CPP_TASK_CANCELED)
            total += dn2cpp_task_fault_inner_count(t);
    }
    if (total == 0)
        return;
    Dn2CppArrayRef* inner = dn2cpp_newarr_ref(total);
    int32_t k = 0;
    for (int32_t i = 0; i < tasks->length; i++)
    {
        auto* t = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
        if (t->status == DN2CPP_TASK_FAULTED || t->status == DN2CPP_TASK_CANCELED)
            k = dn2cpp_task_fault_inners_copy(t, inner, k);
    }
    dn2cpp_throw(dn2cpp_aggregate_exception_new(inner));
}

// Task.WaitAny(Task[]) -> the index of the first input to settle. Never raises the
// winner's fault (real .NET returns the index and leaves the fault to be observed
// through that task), so it drains the WhenAny join task — which always SUCCEEDS —
// rather than any input. An already-settled input short-circuits before the join is
// built, matching .NET's "returns immediately" contract and keeping a settled batch
// from touching the scheduler at all.
//
// Its argument contract differs from WhenAny's on both counts, so do not copy one onto
// the other: an EMPTY array answers -1 here (WhenAny rejects it), and a null element is
// ArgumentException (WhenAny gives ArgumentNullException). Measured against net10.0.
int32_t dn2cpp_task_wait_any(Dn2CppArrayRef* tasks)
{
    if (tasks == nullptr)
        dn2cpp_throw_argument_null();
    // The null scan covers the WHOLE array before an index may be returned: .NET
    // rejects WaitAny([completed, null]) rather than answering 0.
    int32_t settled = -1;
    for (int32_t i = 0; i < tasks->length; i++)
    {
        auto* t = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
        if (t == nullptr)
            dn2cpp_throw_argument();
        if (settled < 0 && t->status != DN2CPP_TASK_PENDING)
            settled = i;
    }
    if (settled >= 0 || tasks->length == 0)
        return settled;
    Dn2CppTask* join = dn2cpp_task_when_any(tasks);
    if (!dn2cpp_task_drain_settle(join))
        dn2cpp_task_throw_deadlock();
    auto* winner = reinterpret_cast<Dn2CppObject*>(static_cast<uintptr_t>(join->result));
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (tasks->data[i] == winner)
            return i;
    }
    // The join only ever completes with one of its own inputs, so this is unreachable
    // — an internal invariant, not bad input.
    dn2cpp_fail("async: Task.WaitAny join completed with a task that is not an input");
}

// Task.get_Exception. FAULTED is a settled status, so `->exception` is immutable
// by the time this can observe it; the AggregateException wrapper is minted on
// the first read and cached in its own slot so every read returns the same
// object (real .NET identity). The allocations happen OUTSIDE the lock (the
// allocator may collect); the install is if-absent under g_task_mtx, so two
// racing readers agree on one wrapper (the loser's is garbage). A reader that
// finds one already installed holds no lock, so the slot is atomic: that release
// install is the only edge making the wrapper's own fields visible to it.
Dn2CppObject* dn2cpp_task_exception(Dn2CppTask* t)
{
    if (t == nullptr)
        dn2cpp_throw_null_reference();
    if (t->status != DN2CPP_TASK_FAULTED)
        return nullptr; // SUCCEEDED / PENDING / CANCELED: null, matching real .NET
    if (t->exceptionAggregate == nullptr)
    {
        Dn2CppArrayRef* inner = dn2cpp_newarr_ref(1);
        dn2cpp_gc_store_ref(&inner->data[0], t->exception);
        Dn2CppObject* agg = dn2cpp_aggregate_exception_new(inner);
        std::lock_guard<std::mutex> lk(g_task_mtx);
        if (t->exceptionAggregate == nullptr)
            dn2cpp_gc_store_ref(&t->exceptionAggregate, agg);
    }
    return t->exceptionAggregate;
}

void dn2cpp_sched_pump()
{
    // Timers first: advance the virtual clock by the real time elapsed since the
    // previous pump, firing every delay whose due falls inside the window (the
    // shared helper keeps the (due, insertion) firing order). A blocking drain
    // interleaved between pumps can only have jumped virtual_now FORWARD, which
    // just re-bases later delays — relative ordering stays correct either way.
    Dn2CppScheduler* s = dn2cpp_sched_self();
    auto now_tp = std::chrono::steady_clock::now();
    if (!s->pump_has_last)
    {
        s->pump_has_last = true; // first pump only establishes the baseline
        s->pump_last = now_tp;
    }
    else
    {
        int64_t delta = std::chrono::duration_cast<std::chrono::milliseconds>(
                            now_tp - s->pump_last).count();
        if (delta > 0)
        {
            s->pump_last += std::chrono::milliseconds(delta);
            int64_t target = s->virtual_now + delta;
            while (dn2cpp_sched_advance_timers(target))
            {
            }
            s->virtual_now = target; // later delays stay real-time-relative
        }
    }
    // Steal the queue in one lock acquisition and run the snapshot outside the
    // lock. Timer completions above already enqueued their continuations, so a
    // delay that expired this frame also resumes this frame; anything posted BY
    // a pumped continuation runs on the next pump. Each node stays on the
    // g_pending_conts static root until the moment it runs, and the local list
    // is rooted by this frame's stack.
    Dn2CppCont* c;
    {
        std::lock_guard<std::mutex> lk(s->mtx);
        c = s->head;
        s->head = nullptr;
        s->tail = nullptr;
    }
    while (c != nullptr)
    {
        Dn2CppCont* next = c->next;
        dn2cpp_pending_cont_unlink(c);
        try
        {
            c->fn(c->state);
        }
        catch (Dn2CppException& __ex)
        {
            // A state machine's MoveNext never throws (faults settle into its task);
            // only a user OnCompleted(Action) callback gets here. The C++ exception may
            // never unwind through the host's C ABI frame callback, so what replaces it
            // depends on whether a host frame is contractually allowed to survive:
            // a console main has none, and failing fast is what .NET does with an
            // exception escaping a threadpool callback; an engine host has one and real
            // GodotSharp runs this work inside its own try/catch, so reporting and
            // pumping the next node is .NET parity. An installed boundary sink answers
            // exactly that question — only a host that can log through the engine has
            // one.
            if (dn2cpp_boundary_sink_installed())
                dn2cpp_report_boundary_exception(__ex.obj, "a pumped async continuation");
            else
                dn2cpp_fail("async: unhandled managed exception in a pumped continuation");
        }
        c = next;
    }
}

// ===== System.Threading.Thread (real OS threads) =============================
extern const Dn2CppType dn2cpp_thread_type_obj;
// NO_SHALLOW_CLONE: Dn2CppThread's `handle` is a `new std::thread` and `sync` a
// native-heap Dn2CppThreadSync; a bitwise copy would be a second joiner and a second
// deleter of one OS thread.
const Dn2CppTypeInfo dn2cpp_thread_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Thread", nullptr, 0, nullptr, nullptr, 0, nullptr, nullptr, nullptr, DN2CPP_TF_NO_SHALLOW_CLONE }, &dn2cpp_thread_type_obj);
const Dn2CppType dn2cpp_thread_type_obj = { { &dn2cpp_type_type }, &dn2cpp_thread_type };

// Native completion signal for a timed Join. Lives on the native heap (so its
// mutex/condition_variable get real ctors) and is reachable from the GC-allocated
// Dn2CppThread via a plain pointer; it outlives the program like the monitor table.
struct Dn2CppThreadSync
{
    std::mutex m;
    std::condition_variable cv;
    bool done = false;
};

struct Dn2CppThread : Dn2CppObject
{
    Dn2CppObject* start;   // ThreadStart (no-arg) / ParameterizedThreadStart (object) delegate
    Dn2CppObject* arg;     // Start(object) payload — held here so the GC keeps it alive
    void* handle;          // std::thread* (native heap; not a GC pointer)
    void* sync;            // Dn2CppThreadSync* (native heap; signaled when the body exits)
    Dn2CppString* name;
    int32_t managedId;
    int32_t isBackground;
    int32_t alive;         // advisory (IsAlive); set 1 in the trampoline, 0 on exit
};

// Mark the thread complete and wake any timed Join waiting on it.
static void dn2cpp_thread_signal_done(Dn2CppThread* t)
{
    auto* s = static_cast<Dn2CppThreadSync*>(t->sync);
    if (s == nullptr)
        return;
    {
        std::lock_guard<std::mutex> lk(s->m);
        s->done = true;
    }
    s->cv.notify_all();
}

static std::atomic<int> g_next_thread_id{2}; // main thread is id 1
static thread_local Dn2CppThread* g_current_thread = nullptr;

// Invoke a one-arg ParameterizedThreadStart delegate (and its multicast chain),
// mirroring dn2cpp_action_invoke but passing the start-object argument.
void dn2cpp_paramthread_invoke(Dn2CppObject* del, Dn2CppObject* arg)
{
    if (del == nullptr)
        return;
    auto* dg = reinterpret_cast<Dn2CppDelegate*>(del);
    if (dg->prev != nullptr)
        dn2cpp_paramthread_invoke(dg->prev, arg);
    reinterpret_cast<void (*)(Dn2CppObject*, Dn2CppObject*)>(dg->method)(dg->target, arg);
}

static void dn2cpp_thread_trampoline(Dn2CppThread* t, bool param)
{
    Dn2CppGCThread guard;        // register this thread with the GC for its lifetime
    g_current_thread = t;        // Thread.CurrentThread inside the body
    t_on_user_thread = true;     // this thread is its own deadlock-test exemption
    t->alive = 1;
    try
    {
        if (param)
            dn2cpp_paramthread_invoke(t->start, t->arg);
        else
            dn2cpp_action_invoke(t->start);
    }
    catch (const Dn2CppException&)
    {
        // An unhandled exception on a thread terminates the process in .NET.
        t->alive = 0;
        dn2cpp_thread_signal_done(t); // release any timed Join before we bail out
        dn2cpp_fail("thread: unhandled managed exception");
    }
    t->alive = 0;
    dn2cpp_thread_signal_done(t);
    // Leave the settler set LAST: until this returns, the body could still have
    // completed a task some other thread is blocked on. A blocked drain sleeps in an
    // untimed `s->cv.wait(lk, pred)` whose predicate carries the principal set, so the
    // departure has to be DELIVERED rather than noticed — which is exactly what going
    // through dn2cpp_principal_left buys: on the counter's transition to zero it calls
    // dn2cpp_sched_wake_all, notifying every scheduler in the registry so each blocked
    // drain re-evaluates. That is why the last user thread leaving is what lets a
    // genuinely defeated wait report instead of hang, and why a raw fetch_sub here would
    // strand it asleep forever with nobody ever coming back to look.
    dn2cpp_principal_left(g_live_user_threads);
}

Dn2CppThread* dn2cpp_thread_new(Dn2CppObject* start)
{
    auto* t = static_cast<Dn2CppThread*>(dn2cpp_alloc(sizeof(Dn2CppThread)));
    t->type = &dn2cpp_thread_type;
    t->start = start;
    t->arg = nullptr;
    t->handle = nullptr;
    t->sync = nullptr;
    t->name = nullptr;
    t->managedId = g_next_thread_id.fetch_add(1, std::memory_order_relaxed);
    t->isBackground = 0;
    t->alive = 0;
    return t;
}

// `dn2cpp_thread_spawn` captures the GC-allocated `t` into the std::thread closure block,
// which is NOT a GC root (see dn2cpp_cts_cancel_after). It is sound here only because the
// trampoline takes `t` as an argument and uses it on both sides of the user body, so it
// stays in a callee-saved register the collector scans for the whole exposure window. If
// a start ever grows a stretch where `t` is dead but later used, root it in a pinned cell
// as CancelAfter does.

// Spawn the trampoline, keeping the settler set honest across a failed spawn. The count
// goes up BEFORE the OS thread exists — a caller that hands this thread a task and
// immediately blocks on it must never observe an empty set, and the trampoline has not
// necessarily run a single instruction by then — so a std::thread ctor that throws has to
// take it back down. That is not a hypothetical: on wasm the construction ALWAYS fails
// (the template is single-threaded, which is what makes Thread throw there), and a leaked
// +1 would disarm the defeated-wait report for the rest of the process — silently turning
// wasm's reporting behavior into hanging, which is exactly what must not change.
static void dn2cpp_thread_spawn(Dn2CppThread* t, bool param)
{
    g_live_user_threads.fetch_add(1, std::memory_order_acq_rel);
    try
    {
        t->handle = new std::thread([t, param] { dn2cpp_thread_trampoline(t, param); });
    }
    catch (...)
    {
        dn2cpp_principal_left(g_live_user_threads);
        throw;
    }
}

void dn2cpp_thread_start(Dn2CppThread* t)
{
    t->sync = new Dn2CppThreadSync(); // create before the thread so the trampoline sees it
    dn2cpp_thread_spawn(t, false);
}

void dn2cpp_thread_start_param(Dn2CppThread* t, Dn2CppObject* arg)
{
    dn2cpp_gc_store_ref(&t->arg, arg);
    t->sync = new Dn2CppThreadSync();
    dn2cpp_thread_spawn(t, true);
}

void dn2cpp_thread_join(Dn2CppThread* t)
{
    auto* th = static_cast<std::thread*>(t->handle);
    if (th != nullptr && th->joinable())
        th->join();
}

// Thread.Join(int)/Join(TimeSpan): wait up to ms for the body to finish. Returns 1 if it
// terminated (and joins the underlying std::thread), 0 on timeout (the thread is still
// running — a later Join() reaps it). A negative ms means an infinite wait.
int32_t dn2cpp_thread_join_timeout(Dn2CppThread* t, int32_t ms)
{
    if (ms < 0)
    {
        dn2cpp_thread_join(t);
        return 1;
    }
    auto* s = static_cast<Dn2CppThreadSync*>(t->sync);
    if (s == nullptr)
    {
        // Never started (or the lazily-materialized main-thread object): nothing to wait on.
        dn2cpp_thread_join(t);
        return 1;
    }
    bool done;
    {
        std::unique_lock<std::mutex> lk(s->m);
        done = s->cv.wait_for(lk, std::chrono::milliseconds(ms), [s] { return s->done; });
    }
    if (!done)
        return 0; // timeout — leave the thread running, do not join
    dn2cpp_thread_join(t);
    return 1;
}

void dn2cpp_thread_sleep(int32_t ms)
{
    if (ms > 0)
        std::this_thread::sleep_for(std::chrono::milliseconds(ms));
    else
        std::this_thread::yield(); // Sleep(0) yields the remainder of the time slice
}

int32_t dn2cpp_thread_yield()
{
    std::this_thread::yield();
    return 1; // Thread.Yield() reports whether it yielded; we always do
}

// Materialize a Thread object for a thread that never ran the trampoline (the
// main thread at init, a pool worker, a host thread calling in). Unlike
// trampoline-run threads (whose Thread object is rooted by the trampoline's
// stack frame for the thread's whole life), this object is held only through
// thread-local storage, which the collector does not scan on every platform —
// so it is also linked into a static-rooted list (the static data segment IS
// scanned; the g_pending_conts pattern). Not an uncollectable anchor cell: in
// a dylib-hosted heap (the Godot .NET module) a cell allocated during
// runtime_init was observed NOT to keep its referent alive across later
// collections, while a static root does. Entries are never removed — these
// threads (main, pool workers, engine threads) live for the process.
struct Dn2CppMaterializedThread
{
    Dn2CppThread* t;
    Dn2CppMaterializedThread* next;
};
static DN2CPP_GC_STATIC_ROOT Dn2CppMaterializedThread* g_materialized_threads = nullptr;
static std::mutex& g_materialized_mtx = dn2cpp_never_destroyed<std::mutex>();

static void dn2cpp_thread_materialize_current(int32_t managedId)
{
    auto* t = static_cast<Dn2CppThread*>(dn2cpp_alloc(sizeof(Dn2CppThread)));
    t->type = &dn2cpp_thread_type;
    t->managedId = managedId;
    t->alive = 1;
    auto* node = static_cast<Dn2CppMaterializedThread*>(dn2cpp_alloc(sizeof(Dn2CppMaterializedThread)));
    node->t = t;
    {
        std::lock_guard<std::mutex> lk(g_materialized_mtx);
        dn2cpp_gc_store_ref(&node->next, g_materialized_threads);
        g_materialized_threads = node;
    }
    g_current_thread = t;
}

void dn2cpp_thread_materialize_main()
{
    // Called once from dn2cpp_runtime_init, on the main thread by construction
    // (every backend's entry runs init before anything else), claiming id 1.
    if (g_current_thread == nullptr)
        dn2cpp_thread_materialize_current(1);
}

Dn2CppThread* dn2cpp_thread_current()
{
    // Init pre-claimed the main thread as id 1, so a thread reaching this lazy
    // path is a pool worker or a host thread — give it a fresh id like any
    // trampoline-run thread, so ManagedThreadId distinguishes it from main.
    if (g_current_thread == nullptr)
        dn2cpp_thread_materialize_current(g_next_thread_id.fetch_add(1, std::memory_order_relaxed));
    return g_current_thread;
}

int32_t dn2cpp_thread_managed_id(Dn2CppThread* t) { return t->managedId; }
int32_t dn2cpp_thread_is_alive(Dn2CppThread* t) { return t->alive ? 1 : 0; }
int32_t dn2cpp_thread_get_background(Dn2CppThread* t) { return t->isBackground; }
void dn2cpp_thread_set_background(Dn2CppThread* t, int32_t v) { t->isBackground = v; }
Dn2CppString* dn2cpp_thread_get_name(Dn2CppThread* t) { return t->name; }
void dn2cpp_thread_set_name(Dn2CppThread* t, Dn2CppString* n)
{
    dn2cpp_gc_store_ref(&t->name, n);
}

// ===== Task.Run / ThreadPool (real worker pool) ==============================
// A fixed pool of GC-registered worker threads drains a thread-safe work queue. Each
// item runs its delegate, completes its Task (dn2cpp_task_set_result/exception), and
// routes the awaiting state machine's continuation back to the
// scheduler that registered the await (so `await Task.Run(...)` resumes on the awaiting
// thread). g_inflight_async_tasks keeps a blocked task_block sleeping (rather than
// deadlock-failing) while a global item or worker-local continuation can still settle it.
// A GC-allocated holder rooting one queued work item's managed pieces from enqueue
// until the worker is done with them. The work queue itself is a malloc-backed deque
// the collector never scans, so while an item waits for a worker NOTHING else is
// guaranteed to keep its object graph alive: a fire-and-forget item returns no Task,
// and a Task.Run whose caller discarded the returned Task (or suspended without a
// scanned reference to it) roots neither the task nor the delegate. The holders are
// linked into g_pool_pending, whose head is a `static` pointer — Boehm scans the
// static data segment, so the whole list (and everything each node points at) stays
// reachable; the worker unlinks the node only after the item has fully run (for a
// Task item: after the task has settled).
struct Dn2CppPoolNode
{
    Dn2CppTask* task;    // Task.Run item: the outer task (null for fire-and-forget)
    Dn2CppObject* del;   // the delegate: Task.Run lambda / WaitCallback (possibly multicast)
    Dn2CppObject* state; // fire-and-forget: the WaitCallback state argument (else null)
    Dn2CppPoolNode* next;
};

struct Dn2CppWorkItem
{
    Dn2CppTask* task = nullptr;
    Dn2CppObject* del = nullptr;
    uint64_t (*invoke)(Dn2CppObject*) = nullptr;
    Dn2CppPoolNode* node = nullptr; // roots task/del/state while the item is queued/running
    // State-carrying Task item (TaskFactory.StartNew(Action<object?>, state)): the
    // worker runs invoke2(node->del, node->state) instead of invoke(del); the node
    // roots the state exactly like a fire-and-forget item's.
    uint64_t (*invoke2)(Dn2CppObject*, Dn2CppObject*) = nullptr;
    bool ff = false;                // fire-and-forget: invoke node->del(node->state); no Task
    bool unwrap = false;            // Task.Run(Func<Task>): `del` returns an inner task to unwrap (invoke is unused)
    bool nested = false;            // TaskFactory.StartNew(Func<Task>): no unwrap — settle outer with the inner pointer, then drain the inner (invoke is unused)
};

static std::mutex& g_pool_mtx = dn2cpp_never_destroyed<std::mutex>();
static std::condition_variable& g_pool_cv = dn2cpp_never_destroyed<std::condition_variable>();
static std::deque<Dn2CppWorkItem>& g_pool_q = dn2cpp_never_destroyed<std::deque<Dn2CppWorkItem>>();
// Worker handles are retained, not detached: a host that unloads this library
// (dlclose unmaps the code the workers execute) must be able to stop and join
// them first — see dn2cpp_runtime_quiesce.
static std::vector<std::thread>& g_pool_threads = dn2cpp_never_destroyed<std::vector<std::thread>>();
static bool g_pool_running = false; // guarded by g_pool_mtx
static bool g_pool_stop = false;    // guarded by g_pool_mtx; workers leave their loop when set
static unsigned g_pool_live = 0;    // guarded by g_pool_mtx; workers still inside their loop
static DN2CPP_GC_STATIC_ROOT Dn2CppPoolNode* g_pool_pending = nullptr; // static => a GC root that keeps queued items reachable

static void dn2cpp_pool_wake_for_scheduler()
{
    g_pool_cv.notify_all();
}

// Link a fresh holder under g_pool_mtx, BEFORE its work item is pushed, so the item's
// graph is rooted before any worker can pop it.
static void dn2cpp_pool_link(Dn2CppPoolNode* node)
{
    dn2cpp_gc_store_ref(&node->next, g_pool_pending);
    g_pool_pending = node;
}

static void dn2cpp_pool_unlink_locked(Dn2CppPoolNode* node)
{
    for (Dn2CppPoolNode** pp = &g_pool_pending; *pp != nullptr; pp = &(*pp)->next)
    {
        if (*pp == node)
        {
            *pp = node->next;
            dn2cpp_gc_write_barrier_if_heap(pp); // pp may name the always-rescanned static head
            node->next = nullptr;
            break;
        }
    }
}

// Unlink a finished holder from g_pool_pending so it (and the task/delegate/state it
// rooted) can be collected. Walks the list under g_pool_mtx.
static void dn2cpp_pool_unlink(Dn2CppPoolNode* node)
{
    std::lock_guard<std::mutex> lk(g_pool_mtx);
    dn2cpp_pool_unlink_locked(node);
}

static Dn2CppPoolNode* dn2cpp_pool_node_new(Dn2CppTask* task, Dn2CppObject* del, Dn2CppObject* state)
{
    auto* node = static_cast<Dn2CppPoolNode*>(dn2cpp_alloc(sizeof(Dn2CppPoolNode)));
    node->task = task;
    node->del = del;
    node->state = state;
    node->next = nullptr;
    return node;
}

// Transfer a worker item's settler claim to owner-local work before the item leaves
// its own principal count, and release it once both local queues are empty. Queue
// publication and the false transition share s->mtx, so a cross-thread post can never
// land between the emptiness check and the departure.
static void dn2cpp_sched_sync_pool_principal(Dn2CppScheduler* s)
{
    bool left = false;
    {
        std::lock_guard<std::mutex> lk(s->mtx);
        bool pending = s->head != nullptr || s->timers != nullptr;
        bool principal = s->pool_local_principal.load(std::memory_order_relaxed);
        if (pending && !principal)
        {
            s->pool_local_principal.store(true, std::memory_order_release);
            g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
        }
        else if (!pending && principal)
        {
            s->pool_local_principal.store(false, std::memory_order_release);
            left = true;
        }
    }
    if (left)
        dn2cpp_principal_left(g_inflight_async_tasks);
}

// Shutdown discards scheduler-owned work just like it discards queued pool items.
// Detach under the scheduler lock, then unlink from the static GC roots outside it:
// enqueue takes those locks in the opposite order.
static void dn2cpp_sched_abandon_pool_local(Dn2CppScheduler* s)
{
    Dn2CppCont* conts;
    Dn2CppTimer* timers;
    bool left;
    {
        std::lock_guard<std::mutex> lk(s->mtx);
        s->pool_worker = false;
        conts = s->head;
        timers = s->timers;
        s->head = nullptr;
        s->tail = nullptr;
        s->timers = nullptr;
        s->timers_tail = nullptr;
        left = s->pool_local_principal.exchange(false, std::memory_order_acq_rel);
    }
    while (conts != nullptr)
    {
        Dn2CppCont* next = conts->next;
        dn2cpp_pending_cont_unlink(conts);
        dn2cpp_gc_store_ref(&conts->next, static_cast<Dn2CppCont*>(nullptr));
        conts = next;
    }
    while (timers != nullptr)
    {
        Dn2CppTimer* next = timers->next;
        dn2cpp_pending_timer_unlink(timers);
        dn2cpp_gc_store_ref(&timers->next, static_cast<Dn2CppTimer*>(nullptr));
        timers = next;
    }
    if (left)
        dn2cpp_principal_left(g_inflight_async_tasks);
}

static void dn2cpp_pool_worker()
{
    Dn2CppGCThread guard; // workers allocate managed objects -> must be GC-registered
    Dn2CppScheduler* scheduler = dn2cpp_sched_self();
    {
        std::lock_guard<std::mutex> lk(scheduler->mtx);
        scheduler->pool_worker = true;
    }
    bool preferLocal = false;
    for (;;)
    {
        Dn2CppWorkItem it;
        bool runLocal = false;
        {
            std::unique_lock<std::mutex> lk(g_pool_mtx);
            g_pool_cv.wait(lk, [&] {
                return g_pool_stop || !g_pool_q.empty()
                    || scheduler->pool_local_principal.load(std::memory_order_acquire);
            });
            if (g_pool_stop)
                break; // quiesce: the queue was already dropped, leave at once
            bool localReady = scheduler->pool_local_principal.load(std::memory_order_acquire);
            if (!g_pool_q.empty() && (!localReady || !preferLocal))
            {
                it = g_pool_q.front();
                g_pool_q.pop_front();
                preferLocal = true;
            }
            else
            {
                runLocal = true;
                preferLocal = false;
            }
        }
        if (runLocal)
        {
            try
            {
                // One turn only. A self-rescheduling await loop retains its
                // principal but cannot monopolize the worker over global items.
                if (!dn2cpp_sched_run_one())
                    dn2cpp_sched_advance_timers(INT64_MAX);
            }
            catch (const Dn2CppException&)
            {
                dn2cpp_fail("threadpool: unhandled managed exception in a local continuation");
            }
            dn2cpp_sched_sync_pool_principal(scheduler);
            continue;
        }
        if (it.ff)
        {
            // Fire-and-forget ThreadPool item: invoke the WaitCallback(state) chain, then
            // unlink the holder so the delegate/state can be collected. It carries no Task
            // (not awaitable), but it IS a principal — arbitrary managed code that can
            // settle any task — so it is counted in g_inflight_async_tasks from the enqueue
            // (see dn2cpp_threadpool_queue) until here, and a blocked wait sleeps rather
            // than concluding that nothing can settle it. An unhandled managed exception
            // terminates the process, as it does in real .NET — so the decrement below is
            // unreachable on that path, and correctly so.
            try
            {
                dn2cpp_paramthread_invoke(it.node->del, it.node->state);
            }
            catch (const Dn2CppException&)
            {
                dn2cpp_fail("threadpool: unhandled managed exception");
            }
            dn2cpp_sched_sync_pool_principal(scheduler);
            dn2cpp_pool_unlink(it.node);
            dn2cpp_principal_left(g_inflight_async_tasks);
            continue;
        }
        try
        {
            if (it.unwrap)
            {
                // Task.Run(Func<Task> / Func<Task<T>>): the async lambda returns its INNER
                // task. Drive THIS worker's own scheduler until the inner settles — its
                // Task.Delay timers and any nested Task.Run continuations are owned by and
                // land on this worker — then settle the outer task with the inner's exact
                // status/result/exception. A canceled inner makes the outer canceled, a
                // faulted inner makes it faulted, and the 8-byte result slot copies opaquely
                // (0 for a void inner Task). A synchronous throw before the inner Task is
                // returned (a non-async Func<Task> body) is caught below and faults the
                // outer, matching real .NET's eager fault.
                auto* d = reinterpret_cast<Dn2CppDelegate*>(it.del);
                Dn2CppTask* inner = reinterpret_cast<Dn2CppTask* (*)(Dn2CppObject*)>(d->method)(d->target);
                dn2cpp_task_drain(inner);
                dn2cpp_task_complete(it.task, inner->status, inner->result, inner->exception);
            }
            else if (it.nested)
            {
                // TaskFactory.StartNew(Func<Task> / Func<Task<T>>): real .NET's
                // Task<Task<T>> semantics — NO unwrap. The async lambda returns its
                // INNER task; the outer task settles with that pointer as its result
                // the moment the delegate returns. But if the lambda suspended, the
                // inner task's Task.Delay timers and MoveNext continuations are owned
                // by THIS worker's thread-local scheduler — no other thread can drive
                // them — so keep draining until the inner settles. The epilogue below
                // decrements g_inflight_async_tasks only after the drain, so a thread
                // awaiting the inner task stays asleep (counted in flight) instead of
                // tripping the deadlock guard.
                auto* d = reinterpret_cast<Dn2CppDelegate*>(it.del);
                Dn2CppTask* inner = reinterpret_cast<Dn2CppTask* (*)(Dn2CppObject*)>(d->method)(d->target);
                dn2cpp_task_set_result(it.task, static_cast<uint64_t>(reinterpret_cast<uintptr_t>(inner)));
                if (inner != nullptr)
                {
                    // The outer task has already settled, so a managed exception
                    // escaping the drain must NOT reach the shared catch below (a
                    // set_exception there would double-settle the outer). The inner
                    // task's own fault is captured in `inner`, not thrown by the
                    // drain; anything leaking out of a resumed continuation here is
                    // unobservable — fail loudly, like an unhandled fire-and-forget
                    // exception. (A synchronous throw before the delegate returns
                    // its inner task still flows to the shared catch and faults the
                    // outer, matching real .NET's eager fault.)
                    try
                    {
                        dn2cpp_task_drain(inner);
                    }
                    catch (const Dn2CppException&)
                    {
                        dn2cpp_fail("threadpool: unhandled managed exception while draining a nested task");
                    }
                }
            }
            else if (it.invoke2 != nullptr)
            {
                // State-carrying Task item: the 2-arg thunk reads the delegate and
                // its state argument from the GC-rooted holder node.
                uint64_t r = it.invoke2(it.node->del, it.node->state);
                dn2cpp_task_set_result(it.task, r);
            }
            else
            {
                uint64_t r = it.invoke(it.del);
                dn2cpp_task_set_result(it.task, r);
            }
        }
        catch (const Dn2CppException& e)
        {
            dn2cpp_task_set_exception(it.task, e.obj); // rooted via the task before the pop
            dn2cpp_exc_inflight_pop(e.obj);
        }
        dn2cpp_sched_sync_pool_principal(scheduler);
        // Work done: allow the delegate to be collected.
        dn2cpp_gc_store_ref(&it.task->workerKeepAlive, static_cast<Dn2CppObject*>(nullptr));
        dn2cpp_pool_unlink(it.node);        // the task has settled; drop the queue root
        dn2cpp_principal_left(g_inflight_async_tasks);
    }
    dn2cpp_sched_abandon_pool_local(scheduler);
    {
        std::lock_guard<std::mutex> lk(g_pool_mtx);
        g_pool_live--;
    }
    // Wake the quiescer waiting for live == 0. Returning then runs the GC
    // guard's destructor (GC_unregister_my_thread) before a join() completes.
    g_pool_cv.notify_all();
}

// Start the fixed worker pool if it is not running. Deliberately not a
// std::once_flag: a quiesced pool must be restartable — dlclose does not
// guarantee the image is unmapped, so a closed-and-reopened library can land
// here with its statics intact, and a consumed once_flag would leave every
// later submit queued with no worker to serve it.
static void dn2cpp_pool_ensure_started()
{
    std::lock_guard<std::mutex> lk(g_pool_mtx);
    if (g_pool_running)
        return; // includes a timed-out quiesce: the dying pool must not respawn
    unsigned n = std::thread::hardware_concurrency();
    if (n == 0)
        n = 4;
    g_pool_running = true;
    g_pool_stop = false;
    g_pool_live = n;
    for (unsigned i = 0; i < n; i++)
        g_pool_threads.emplace_back(dn2cpp_pool_worker);
}

// Stop and join the pool workers so the library's code can be unmapped. Queued
// items are dropped, not drained: an item is arbitrary managed code (it may
// never return), which no bounded deadline can absorb — and at unload nothing
// that could observe those tasks survives. Returns the number of workers
// joined, or -1 if the deadline passed with a worker still inside managed
// code: the stragglers are detached and the pool is left stopped for good, so
// no submit respawns workers into a library about to unload.
int32_t dn2cpp_pool_quiesce(std::chrono::steady_clock::time_point deadline, bool infinite)
{
    {
        std::unique_lock<std::mutex> lk(g_pool_mtx);
        if (!g_pool_running)
            return 0;
        g_pool_stop = true;
        // Drop each queued item's holder individually: holders of items RUNNING
        // on workers share g_pool_pending and must stay rooted until they settle.
        // A dropped item also leaves the in-flight count — every queued item is
        // counted, fire-and-forget ones included — or a task_block after a restart
        // would sleep forever on a principal that was thrown away here.
        for (Dn2CppWorkItem& it : g_pool_q)
        {
            dn2cpp_principal_left(g_inflight_async_tasks);
            dn2cpp_pool_unlink_locked(it.node);
        }
        g_pool_q.clear();
        g_pool_cv.notify_all();
        bool clean = true;
        if (infinite)
            g_pool_cv.wait(lk, [] { return g_pool_live == 0; });
        else
            clean = g_pool_cv.wait_until(lk, deadline, [] { return g_pool_live == 0; });
        if (!clean)
        {
            for (std::thread& t : g_pool_threads)
            {
                if (t.joinable())
                    t.detach();
            }
            g_pool_threads.clear();
            return -1;
        }
    }
    // Join outside g_pool_mtx: an exiting worker takes it to decrement
    // g_pool_live, so joining under the lock would deadlock. Every worker has
    // signaled by now; only the GC unregister and the thread epilogue remain,
    // so these joins are near-instant.
    int32_t joined = 0;
    for (std::thread& t : g_pool_threads)
    {
        if (t.joinable())
        {
            t.join();
            joined++;
        }
    }
    g_pool_threads.clear();
    {
        std::lock_guard<std::mutex> lk(g_pool_mtx);
        g_pool_running = false;
        g_pool_stop = false; // a later submit restarts fresh workers
    }
    return joined;
}

// Environment.ProcessorCount (its GetProcessorCount InternalCall): the available
// hardware concurrency, clamped to >= 1. Used as ConcurrentDictionary's default
// concurrency level (a sizing hint — any positive value is correct).
int32_t dn2cpp_environment_processor_count()
{
    unsigned n = std::thread::hardware_concurrency();
    return n == 0 ? 1 : static_cast<int32_t>(n);
}

// Queue an existing pending task's work on the pool: the shared tail of the
// dn2cpp_pool_submit* allocate-and-enqueue entry points and of Task.Start on a
// cold task (whose Dn2CppTask was allocated at the newobj site). Exactly one of
// invoke/invoke2 is set; the 2-arg thunk form reads del + state from the holder.
static void dn2cpp_pool_enqueue(Dn2CppTask* t, Dn2CppObject* del, Dn2CppObject* state,
                                uint64_t (*invoke)(Dn2CppObject*),
                                uint64_t (*invoke2)(Dn2CppObject*, Dn2CppObject*))
{
    // A caller holding the task also keeps its worker delegate reachable.
    dn2cpp_gc_store_ref(&t->workerKeepAlive, del);
    Dn2CppPoolNode* node = dn2cpp_pool_node_new(t, del, state);
    g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
    dn2cpp_pool_ensure_started();
    {
        std::lock_guard<std::mutex> lk(g_pool_mtx);
        dn2cpp_pool_link(node); // rooted before the work item is visible to a worker
        Dn2CppWorkItem wi{ t, del, invoke, node };
        wi.invoke2 = invoke2;
        g_pool_q.push_back(wi);
    }
    g_pool_cv.notify_one();
}

Dn2CppTask* dn2cpp_pool_submit(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*))
{
    Dn2CppTask* t = dn2cpp_task_alloc();
    dn2cpp_pool_enqueue(t, del, nullptr, invoke, nullptr);
    return t;
}

// TaskFactory.StartNew(Action<object?>, state): like dn2cpp_pool_submit but the queued
// item carries a 2-arg thunk; the worker invokes it with the holder node's del + state
// (both rooted by the node from enqueue until the task settles).
static Dn2CppTask* dn2cpp_pool_submit_state(Dn2CppObject* del, Dn2CppObject* state,
                                            uint64_t (*invoke2)(Dn2CppObject*, Dn2CppObject*))
{
    Dn2CppTask* t = dn2cpp_task_alloc();
    dn2cpp_pool_enqueue(t, del, state, nullptr, invoke2);
    return t;
}

// Task.Run(Func<Task> / Func<Task<T>>): like dn2cpp_pool_submit but the queued item is
// flagged `unwrap`, so the worker invokes `del` for its inner Task and settles the outer
// task with the inner's status/result/exception (see the worker loop). The outer task
// stays counted in g_inflight_async_tasks from here until the worker settles it, keeping
// an awaiting thread asleep (not deadlock-failed) for the whole inner-task lifetime.
static Dn2CppTask* dn2cpp_pool_submit_unwrap(Dn2CppObject* del)
{
    Dn2CppTask* t = dn2cpp_task_alloc();
    // A caller holding the task also keeps its worker delegate reachable.
    dn2cpp_gc_store_ref(&t->workerKeepAlive, del);
    Dn2CppPoolNode* node = dn2cpp_pool_node_new(t, del, nullptr);
    g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
    dn2cpp_pool_ensure_started();
    {
        std::lock_guard<std::mutex> lk(g_pool_mtx);
        dn2cpp_pool_link(node); // rooted before the work item is visible to a worker
        Dn2CppWorkItem wi{ t, del, nullptr, node };
        wi.unwrap = true;
        g_pool_q.push_back(wi);
    }
    g_pool_cv.notify_one();
    return t;
}

// TaskFactory.StartNew(Func<Task> / Func<Task<T>>): like dn2cpp_pool_submit_unwrap but
// the queued item is flagged `nested`, so the worker settles the outer task with the
// inner task POINTER (real .NET's Task<Task<T>> — no unwrap) and then drains its own
// scheduler until the inner settles (see the worker loop). The outer task stays counted
// in g_inflight_async_tasks from here until the drain finishes, keeping a thread that
// awaits the inner task asleep (not deadlock-failed) for the whole inner-task lifetime.
static Dn2CppTask* dn2cpp_pool_submit_nested(Dn2CppObject* del)
{
    Dn2CppTask* t = dn2cpp_task_alloc();
    // A caller holding the task also keeps its worker delegate reachable.
    dn2cpp_gc_store_ref(&t->workerKeepAlive, del);
    Dn2CppPoolNode* node = dn2cpp_pool_node_new(t, del, nullptr);
    g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
    dn2cpp_pool_ensure_started();
    {
        std::lock_guard<std::mutex> lk(g_pool_mtx);
        dn2cpp_pool_link(node); // rooted before the work item is visible to a worker
        Dn2CppWorkItem wi{ t, del, nullptr, node };
        wi.nested = true;
        g_pool_q.push_back(wi);
    }
    g_pool_cv.notify_one();
    return t;
}

// Result-kind invoke thunks: run the (single) delegate and pack its return value into
// the 8-byte Task result slot using the same conventions as WhenAll / FromResult.
static uint64_t dn2cpp_run_void(Dn2CppObject* del)
{
    dn2cpp_action_invoke(del);
    return 0;
}
static uint64_t dn2cpp_run_i4(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    return static_cast<uint64_t>(static_cast<uint32_t>(
        reinterpret_cast<int32_t (*)(Dn2CppObject*)>(d->method)(d->target)));
}
static uint64_t dn2cpp_run_i8(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    return static_cast<uint64_t>(reinterpret_cast<int64_t (*)(Dn2CppObject*)>(d->method)(d->target));
}
static uint64_t dn2cpp_run_r4(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    return dn2cpp_r8_bits(static_cast<double>(reinterpret_cast<float (*)(Dn2CppObject*)>(d->method)(d->target)));
}
static uint64_t dn2cpp_run_r8(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    return dn2cpp_r8_bits(reinterpret_cast<double (*)(Dn2CppObject*)>(d->method)(d->target));
}
static uint64_t dn2cpp_run_ref(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    return static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
        reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*)>(d->method)(d->target)));
}

Dn2CppTask* dn2cpp_task_run_void(Dn2CppObject* del) { return dn2cpp_pool_submit(del, &dn2cpp_run_void); }
Dn2CppTask* dn2cpp_task_run_i4(Dn2CppObject* del) { return dn2cpp_pool_submit(del, &dn2cpp_run_i4); }
Dn2CppTask* dn2cpp_task_run_i8(Dn2CppObject* del) { return dn2cpp_pool_submit(del, &dn2cpp_run_i8); }
Dn2CppTask* dn2cpp_task_run_r4(Dn2CppObject* del) { return dn2cpp_pool_submit(del, &dn2cpp_run_r4); }
Dn2CppTask* dn2cpp_task_run_r8(Dn2CppObject* del) { return dn2cpp_pool_submit(del, &dn2cpp_run_r8); }
Dn2CppTask* dn2cpp_task_run_ref(Dn2CppObject* del) { return dn2cpp_pool_submit(del, &dn2cpp_run_ref); }
// A struct result carries its own boxing thunk (the transpiler stamps in the struct's
// exact type); the worker packs the boxed pointer into the 8-byte slot like any other run.
Dn2CppTask* dn2cpp_task_run_struct(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*))
{
    return dn2cpp_pool_submit(del, invoke);
}
Dn2CppTask* dn2cpp_task_run_unwrap(Dn2CppObject* del) { return dn2cpp_pool_submit_unwrap(del); }
Dn2CppTask* dn2cpp_task_run_nested(Dn2CppObject* del) { return dn2cpp_pool_submit_nested(del); }

// TaskFactory.StartNew(Action<object?>, state): run the 1-arg delegate chain with the
// state argument (the same multicast-aware invoke the fire-and-forget items use).
static uint64_t dn2cpp_run_void_state(Dn2CppObject* del, Dn2CppObject* state)
{
    dn2cpp_paramthread_invoke(del, state);
    return 0;
}

Dn2CppTask* dn2cpp_task_run_void_state(Dn2CppObject* del, Dn2CppObject* state)
{
    return dn2cpp_pool_submit_state(del, state, &dn2cpp_run_void_state);
}

// ---- cold tasks: `new Task(...)` / `new Task<T>(...)` + Start / RunSynchronously ----

static Dn2CppTask* dn2cpp_task_cold(Dn2CppObject* del, Dn2CppObject* state,
                                    uint64_t (*invoke)(Dn2CppObject*),
                                    uint64_t (*invoke2)(Dn2CppObject*, Dn2CppObject*))
{
    Dn2CppTask* t = dn2cpp_task_alloc();
    auto* c = static_cast<Dn2CppTaskCold*>(dn2cpp_alloc(sizeof(Dn2CppTaskCold)));
    c->del = del;
    c->state = state;
    c->invoke = invoke;
    c->invoke2 = invoke2;
    dn2cpp_gc_store_ref(&t->cold, c);
    return t;
}

Dn2CppTask* dn2cpp_task_cold_void(Dn2CppObject* del) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_void, nullptr); }
Dn2CppTask* dn2cpp_task_cold_i4(Dn2CppObject* del) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_i4, nullptr); }
Dn2CppTask* dn2cpp_task_cold_i8(Dn2CppObject* del) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_i8, nullptr); }
Dn2CppTask* dn2cpp_task_cold_r4(Dn2CppObject* del) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_r4, nullptr); }
Dn2CppTask* dn2cpp_task_cold_r8(Dn2CppObject* del) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_r8, nullptr); }
Dn2CppTask* dn2cpp_task_cold_ref(Dn2CppObject* del) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_ref, nullptr); }
Dn2CppTask* dn2cpp_task_cold_struct(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*))
{
    return dn2cpp_task_cold(del, nullptr, invoke, nullptr);
}
Dn2CppTask* dn2cpp_task_cold_void_state(Dn2CppObject* del, Dn2CppObject* state)
{
    return dn2cpp_task_cold(del, state, nullptr, &dn2cpp_run_void_state);
}

// Claim a cold task's unstarted work exactly once, under g_task_mtx (the same
// lock that serializes completion/await-registration, so two racing Starts see
// one winner). A task that was never cold (an async-method or Task.Run task) or
// was already claimed throws InvalidOperationException, matching real .NET.
static Dn2CppTaskCold* dn2cpp_task_claim_cold(Dn2CppTask* t, const char* verb)
{
    Dn2CppTaskCold* c;
    {
        std::lock_guard<std::mutex> lk(g_task_mtx);
        c = t->cold;
        dn2cpp_gc_store_ref(&t->cold, static_cast<Dn2CppTaskCold*>(nullptr));
    }
    if (c == nullptr)
    {
        std::string msg = std::string(verb)
            + " may not be called on a task that has completed, has been started, or is a promise-style task.";
        dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
            dn2cpp_string_from_utf8(msg.c_str(), static_cast<int32_t>(msg.size())), nullptr));
    }
    return c;
}

void dn2cpp_task_start(Dn2CppTask* t)
{
    Dn2CppTaskCold* c = dn2cpp_task_claim_cold(t, "Start");
    dn2cpp_pool_enqueue(t, c->del, c->state, c->invoke, c->invoke2);
}

void dn2cpp_task_run_synchronously(Dn2CppTask* t)
{
    Dn2CppTaskCold* c = dn2cpp_task_claim_cold(t, "RunSynchronously");
    // Run inline on the calling thread. A fault settles the task FAULTED — it
    // surfaces at Wait/Result/await, not here — matching real .NET.
    try
    {
        uint64_t r = c->invoke2 != nullptr ? c->invoke2(c->del, c->state) : c->invoke(c->del);
        dn2cpp_task_set_result(t, r);
    }
    catch (const Dn2CppException& e)
    {
        dn2cpp_task_set_exception(t, e.obj);
        dn2cpp_exc_inflight_pop(e.obj);
    }
}

// ---- Task.ContinueWith -------------------------------------------------------

// The continuation node handed to dn2cpp_task_on_completed: run the delegate
// with the settled antecedent (+ the optional state argument) and settle the
// continuation task with its result. Runs on the registering thread's scheduler
// like an await resumption; a thrown managed exception settles the continuation
// task FAULTED (observed at its own Wait/Result/await). The node is reachable
// through the antecedent's continuation list (then g_pending_conts once posted),
// and it in turn roots the continuation task, delegate and state.
struct Dn2CppContWith
{
    Dn2CppTask* antecedent;
    Dn2CppTask* task;      // the continuation task to settle
    Dn2CppObject* del;
    Dn2CppObject* state;   // DN2CPP_CONTWITH_VOID_STATE only
    int32_t kind;          // DN2CPP_CONTWITH_*
    int32_t options;       // TaskContinuationOptions bits; only the NotOn* trio is read
    // DN2CPP_CONTWITH_STRUCT only: a transpiler-emitted trampoline over (del, antecedent)
    // that boxes the struct result; null for every other kind.
    uint64_t (*invokeStruct)(Dn2CppObject*, Dn2CppObject*);
};

// The three TaskContinuationOptions bits that FILTER rather than schedule. .NET spells
// the six combinations with both polarities — OnlyOnRanToCompletion is
// NotOnFaulted|NotOnCanceled, and so on — so reading the NotOn* trio covers the whole
// filter half of the enum with no combination left out.
enum
{
    DN2CPP_TCO_NOT_ON_RAN_TO_COMPLETION = 0x10000,
    DN2CPP_TCO_NOT_ON_FAULTED = 0x20000,
    DN2CPP_TCO_NOT_ON_CANCELED = 0x40000,
};

// Whether a continuation carrying `options` must NOT run for an antecedent that settled
// at `status`. Only the three filter bits are consulted; everything else in the enum is a
// scheduling hint.
static bool dn2cpp_cont_with_filtered_out(int32_t options, int32_t status)
{
    switch (status)
    {
        case DN2CPP_TASK_FAULTED:
            return (options & DN2CPP_TCO_NOT_ON_FAULTED) != 0;
        case DN2CPP_TASK_CANCELED:
            return (options & DN2CPP_TCO_NOT_ON_CANCELED) != 0;
        case DN2CPP_TASK_PENDING:
            return false; // unreachable: this runs only once the antecedent has settled
        default:          // DN2CPP_TASK_SUCCEEDED — ran to completion
            return (options & DN2CPP_TCO_NOT_ON_RAN_TO_COMPLETION) != 0;
    }
}

static void dn2cpp_cont_with_run(void* p)
{
    auto* c = static_cast<Dn2CppContWith*>(p);
    // A CONDITIONAL continuation (TaskContinuationOptions' NotOn* trio, which the
    // OnlyOn* names are pairs of): the antecedent has settled, so its final status is
    // what decides whether the delegate runs at all. A continuation whose predicate is
    // not satisfied does not merely skip — .NET transitions it to CANCELED, so an
    // `await` on it throws TaskCanceledException rather than hanging or completing
    // successfully. The scheduling bits in the same enum (ExecuteSynchronously,
    // LongRunning, RunContinuationsAsynchronously, …) are hints this scheduler does not
    // honour, exactly as the CancellationToken/TaskScheduler arguments beside them are.
    if (c->options != 0 && dn2cpp_cont_with_filtered_out(c->options, c->antecedent->status))
    {
        dn2cpp_task_set_canceled(c->task);
        return;
    }
    auto* ante = reinterpret_cast<Dn2CppObject*>(c->antecedent);
    auto* d = reinterpret_cast<Dn2CppDelegate*>(c->del);
    try
    {
        uint64_t r = 0;
        switch (c->kind)
        {
            case DN2CPP_CONTWITH_VOID: // Action<Task> (multicast-aware)
                dn2cpp_paramthread_invoke(c->del, ante);
                break;
            case DN2CPP_CONTWITH_VOID_STATE: // Action<Task, object?>
                dn2cpp_paramthread_invoke_state(c->del, ante, c->state);
                break;
            case DN2CPP_CONTWITH_I4:
                r = static_cast<uint64_t>(static_cast<uint32_t>(
                    reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante)));
                break;
            case DN2CPP_CONTWITH_I8:
                r = static_cast<uint64_t>(
                    reinterpret_cast<int64_t (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante));
                break;
            case DN2CPP_CONTWITH_R4:
                r = dn2cpp_r8_bits(static_cast<double>(
                    reinterpret_cast<float (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante)));
                break;
            case DN2CPP_CONTWITH_R8:
                r = dn2cpp_r8_bits(
                    reinterpret_cast<double (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante));
                break;
            case DN2CPP_CONTWITH_STRUCT: // Func<Task, TStruct> — the trampoline boxes the result
                r = c->invokeStruct(c->del, ante);
                break;
            default: // DN2CPP_CONTWITH_REF
                r = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
                    reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante)));
                break;
        }
        dn2cpp_task_set_result(c->task, r);
    }
    catch (const Dn2CppException& e)
    {
        dn2cpp_task_set_exception(c->task, e.obj);
        dn2cpp_exc_inflight_pop(e.obj);
    }
}

Dn2CppTask* dn2cpp_task_continue_with(Dn2CppTask* t, Dn2CppObject* del, Dn2CppObject* state,
                                      int32_t kind, int32_t options)
{
    Dn2CppTask* ct = dn2cpp_task_alloc();
    // A caller holding the continuation task also keeps its delegate reachable.
    dn2cpp_gc_store_ref(&ct->workerKeepAlive, del);
    auto* c = static_cast<Dn2CppContWith*>(dn2cpp_alloc(sizeof(Dn2CppContWith)));
    c->antecedent = t;
    c->task = ct;
    c->del = del;
    c->state = state;
    c->kind = kind;
    c->options = options;
    c->invokeStruct = nullptr;
    dn2cpp_task_on_completed(t, &dn2cpp_cont_with_run, c);
    return ct;
}

// ---- Task.WaitAsync(CancellationToken) --------------------------------------
// One node, two racing settlers: the antecedent's completion and the token's cancel.
// `task` is settled by whichever arrives first, and dn2cpp_task_try_set_* is what makes
// "first" mean exactly once — the loser's attempt returns false and is dropped, so no
// double settle is possible even when both fire on different threads.
struct Dn2CppWaitAsync
{
    Dn2CppTask* antecedent;
    Dn2CppTask* task;
};

static void dn2cpp_wait_async_settle(void* p)
{
    auto* w = static_cast<Dn2CppWaitAsync*>(p);
    Dn2CppTask* a = w->antecedent;
    switch (a->status)
    {
        case DN2CPP_TASK_FAULTED:
            dn2cpp_task_try_set_exception(w->task, a->exception);
            break;
        case DN2CPP_TASK_CANCELED:
            dn2cpp_task_try_set_canceled(w->task);
            break;
        default:
            // SUCCEEDED. The result slot is copied verbatim, so a Task<T>'s value survives the
            // wrapper for every result kind (the slot is 8 bytes whatever T is — a struct
            // result is already a boxed pointer in it).
            dn2cpp_task_try_set_result(w->task, a->result);
            break;
    }
}

Dn2CppTask* dn2cpp_task_wait_async(Dn2CppTask* t, Dn2CppCancelSource* src)
{
    Dn2CppTask* wt = dn2cpp_task_alloc();
    auto* w = static_cast<Dn2CppWaitAsync*>(dn2cpp_alloc(sizeof(Dn2CppWaitAsync)));
    w->antecedent = t;
    w->task = wt;
    // THE ANTECEDENT'S COMPLETION WINS OVER AN ALREADY-REQUESTED TOKEN, and the order of
    // these two blocks is that rule. .NET's WaitAsyncCore tests `IsCompleted` and returns
    // `this` BEFORE it ever looks at the token, so `Task.FromResult(5).WaitAsync(ct)` on an
    // already-canceled ct hands back the RESULT, not a cancellation. Arming the cancel side
    // first — the intuitive reading of "whichever fires first wins" — inverts it, which is
    // exactly what gates/build-and-run-async-combinators.sh's
    // WaitAsyncContinueWithSubset caught.
    if (t->status != DN2CPP_TASK_PENDING)
    {
        dn2cpp_wait_async_settle(w);
        return wt;
    }
    // A PENDING antecedent: arm the cancel side. The registration is a `task` node, the
    // same kind dn2cpp_task_delay_ct registers, so the source's cancel sweep already knows
    // how to settle it and there is one code path for "a token cancels a pending task". A
    // settle racing in between is harmless — dn2cpp_task_try_set_* makes exactly one of the
    // two win, and either verdict is a legitimate outcome of a genuine race, as it is on
    // real .NET.
    if (src != nullptr)
    {
        if (dn2cpp_cts_is_cancelled(src))
        {
            dn2cpp_task_try_set_canceled(wt);
        }
        else
        {
            auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
            r->source = src;
            r->task = wt;
            r->callback = nullptr;
            r->stateCallback = nullptr;
            r->tokenCallback = nullptr;
            r->state = nullptr;
            r->child = nullptr;
            r->next = nullptr;
            bool raced;
            {
                std::lock_guard<std::mutex> lk(g_cts_mtx);
                raced = src->canceled != 0;  // a Cancel() between the poll above and here
                if (!raced)
                {
                    dn2cpp_gc_store_ref(&r->next, src->regs);
                    dn2cpp_gc_store_ref(&src->regs, r);
                }
            }
            if (raced)
                dn2cpp_task_try_set_canceled(wt);
        }
    }
    dn2cpp_task_on_completed(t, &dn2cpp_wait_async_settle, w);
    return wt;
}

// ContinueWith with a value-type result: same continuation node, but the delegate is
// invoked through the transpiler-emitted boxing trampoline (DN2CPP_CONTWITH_STRUCT).
Dn2CppTask* dn2cpp_task_continue_with_struct(Dn2CppTask* t, Dn2CppObject* del,
                                             uint64_t (*invoke)(Dn2CppObject*, Dn2CppObject*),
                                             int32_t options)
{
    Dn2CppTask* ct = dn2cpp_task_alloc();
    // A caller holding the continuation task also keeps its delegate reachable.
    dn2cpp_gc_store_ref(&ct->workerKeepAlive, del);
    auto* c = static_cast<Dn2CppContWith*>(dn2cpp_alloc(sizeof(Dn2CppContWith)));
    c->antecedent = t;
    c->task = ct;
    c->del = del;
    c->state = nullptr;
    c->kind = DN2CPP_CONTWITH_STRUCT;
    c->options = options;
    c->invokeStruct = invoke;
    dn2cpp_task_on_completed(t, &dn2cpp_cont_with_run, c);
    return ct;
}

// ThreadPool.QueueUserWorkItem(WaitCallback [, object state]) — fire-and-forget work on
// the same worker pool, returning bool (always 1/true). Unlike Task.Run it returns no
// Task, so nothing roots the delegate/state. The static-rooted holder (linked under
// g_pool_mtx, before enqueueing the work item, so it is reachable before any worker can
// pop it; unlinked only after invoking) keeps them alive for the whole
// queued-until-run window.
//
// A fire-and-forget item IS a principal, and counting it is what makes
// `QueueUserWorkItem(_ => tcs.SetResult(v)); tcs.Task.Wait();` work: it carries no Task,
// but it is arbitrary managed code and can settle any task, exactly as a user thread can.
// Counting only delays the verdict — the park is an untimed `cv.wait` whose predicate
// carries the principal set and is woken when the counter hits zero. Named residue: an
// item that never returns disarms the report for the rest of the process, as a
// never-returning user thread does.
int32_t dn2cpp_threadpool_queue(Dn2CppObject* callback, Dn2CppObject* state)
{
    Dn2CppPoolNode* node = dn2cpp_pool_node_new(nullptr, callback, state);
    dn2cpp_pool_ensure_started();
    {
        std::lock_guard<std::mutex> lk(g_pool_mtx);
        // Under g_pool_mtx and before the push, so no worker can pop this item — and so
        // decrement it — before the count is visible. After dn2cpp_pool_ensure_started for
        // the reason the CancelAfter spawn undoes its own increment: a pool that fails to
        // start throws from there, and a +1 nothing will ever take down disarms the
        // defeated-wait report for the rest of the process.
        g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
        dn2cpp_pool_link(node);
        Dn2CppWorkItem wi{ nullptr, nullptr, nullptr, node };
        wi.ff = true;
        g_pool_q.push_back(wi);
    }
    g_pool_cv.notify_one();
    return 1;
}

// ThreadPool.UnsafeQueueUserWorkItem(IThreadPoolWorkItem, bool preferLocal) —
// fire-and-forget the work item's Execute() on the same pool. The transpiler
// resolves the Execute implementation through the receiver's interface table at
// the enqueue site (the object exists by then) and passes the raw function
// pointer; a GC-allocated invoker pairs it with the work item and is wrapped in
// a pseudo-delegate matching the {target, method, prev} layout the pool's
// fire-and-forget dn2cpp_paramthread_invoke already dispatches — so no new
// worker-loop branch is needed. The invoker/delegate carry an opaque type
// header (never reflected, never isinst'd); the pool holder node roots both
// (and through them the work item) for the queued-until-run window.
struct Dn2CppWorkItemInvoker
{
    Dn2CppObject header;
    Dn2CppObject* wi;  // the IThreadPoolWorkItem receiver
    const void* fn;    // its resolved Execute implementation: void (*)(receiver)
};

// Defined after the struct so the type-info can state the struct's extent.
extern const Dn2CppType dn2cpp_workitem_invoker_type_obj;
static const Dn2CppTypeInfo dn2cpp_workitem_invoker_type =
    dn2cpp_ti_with_typeobject({ "dn2cpp.WorkItemInvoker", nullptr,
                                (int32_t)sizeof(Dn2CppWorkItemInvoker), nullptr, nullptr, 0 },
                              &dn2cpp_workitem_invoker_type_obj);
const Dn2CppType dn2cpp_workitem_invoker_type_obj = { { &dn2cpp_type_type }, &dn2cpp_workitem_invoker_type };

static void dn2cpp_workitem_execute_thunk(Dn2CppObject* target, Dn2CppObject* /*state*/)
{
    auto* inv = reinterpret_cast<Dn2CppWorkItemInvoker*>(target);
    reinterpret_cast<void (*)(Dn2CppObject*)>(const_cast<void*>(inv->fn))(inv->wi);
}

int32_t dn2cpp_threadpool_queue_workitem(Dn2CppObject* wi, const void* executeFn)
{
    auto* inv = static_cast<Dn2CppWorkItemInvoker*>(dn2cpp_alloc(sizeof(Dn2CppWorkItemInvoker)));
    inv->header.type = &dn2cpp_workitem_invoker_type;
    inv->wi = wi;
    inv->fn = executeFn;
    auto* del = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    del->type = &dn2cpp_workitem_invoker_type; // opaque header; only {target, method} are read
    del->target = &inv->header;
    del->method = reinterpret_cast<void*>(&dn2cpp_workitem_execute_thunk);
    del->prev = nullptr;
    return dn2cpp_threadpool_queue(del, nullptr);
}

// Task.ThrowAsync(Exception, SynchronizationContext): AsyncVoidMethodBuilder.SetException's
// re-raise of an async void fault. Real .NET posts the exception to the target context (or,
// with none, the ThreadPool) where it is rethrown as an unhandled exception, crashing the
// process. dn2cpp models exactly that: a rethrow-only fire-and-forget pool item — a
// synthetic delegate (the same opaque-header shape dn2cpp_threadpool_queue_workitem builds)
// whose target IS the exception and whose method rethrows it — so the pool's own
// unhandled-exception handler fails the process. The SynchronizationContext is not honored
// (dn2cpp installs no context onto which a post would marshal), matching the dropped-context
// treatment across the threading surface.
static void dn2cpp_rethrow_thunk(Dn2CppObject* target, Dn2CppObject* /*state*/)
{
    dn2cpp_rethrow(target); // target is the exception; the pool's ff catch -> dn2cpp_fail
}

void dn2cpp_task_throw_async(Dn2CppObject* exc, Dn2CppObject* /*syncCtx*/)
{
    if (exc == nullptr)
        return;
    auto* del = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    del->type = &dn2cpp_workitem_invoker_type; // opaque header; only {target, method, prev} read
    del->target = exc;
    del->method = reinterpret_cast<void*>(&dn2cpp_rethrow_thunk);
    del->prev = nullptr;
    dn2cpp_threadpool_queue(del, nullptr);
}

// TaskScheduler.FromCurrentSynchronizationContext: with no installed context this
// throws exactly what real .NET throws (its message included). With one installed
// it must still throw — dn2cpp's TaskScheduler is an ignored scheduling hint, so a
// context-wrapping scheduler would silently run continuations off-context.
Dn2CppObject* dn2cpp_taskscheduler_from_sync_ctx()
{
    if (dn2cpp_sync_ctx_get() == nullptr)
        dn2cpp_throw_invalid_operation_msg(
            "The current SynchronizationContext may not be used as a TaskScheduler.");
    dn2cpp_throw_not_supported_msg(
        "TaskScheduler.FromCurrentSynchronizationContext: dn2cpp treats a TaskScheduler as "
        "a scheduling hint only and cannot wrap the installed SynchronizationContext.");
}

// ===== IValueTaskSource-backed ValueTask bridge ==============================
// `new ValueTask(<T>)(IValueTaskSource(<T>) source, short token)` — the shape
// RandomAccess' ThreadPoolValueTaskSource read/write scheduler returns. The
// dn2cpp ValueTask is always a {task} struct, so the ctor bridges the source onto a real
// pending Dn2CppTask: a continuation registered through the source's OnCompleted reads
// GetResult — its value or its fault/cancellation — into the task on completion. The
// continuation is a runtime-built delegate stamped with the real Action<object>
// type-info so both invoke paths work (the source's own compiled continuation-invoke IL
// and the pool's fire-and-forget requeue). OnCompleted is called with flags None, so no
// ExecutionContext or scheduling context is captured.
// Rooting: the source stores delegate+state in its own scanned fields and the bridge
// holds the task; the bridge task counts in g_inflight_async_tasks from registration
// until the continuation settles it, so an awaiting task_block sleeps instead of
// deadlock-failing.
struct Dn2CppVtsBridge
{
    Dn2CppObject header;
    Dn2CppObject* vts;      // the IValueTaskSource receiver
    Dn2CppTask* task;       // the pending task the ValueTask wraps
    const void* getResultFn; // resolved GetResult impl: R (*)(receiver, int16_t)
    int16_t version;        // the source's token for this operation
    int32_t resultKind;     // 0=void 1=int32 2=int64 3=reference
};

extern const Dn2CppType dn2cpp_vts_bridge_type_obj;
static const Dn2CppTypeInfo dn2cpp_vts_bridge_type =
    dn2cpp_ti_with_typeobject({ "dn2cpp.ValueTaskSourceBridge", nullptr, (int32_t)sizeof(Dn2CppVtsBridge), nullptr, nullptr, 0 }, &dn2cpp_vts_bridge_type_obj);
const Dn2CppType dn2cpp_vts_bridge_type_obj = { { &dn2cpp_type_type }, &dn2cpp_vts_bridge_type };

static void dn2cpp_vts_continuation(Dn2CppObject* target, Dn2CppObject* /*state*/)
{
    auto* b = reinterpret_cast<Dn2CppVtsBridge*>(target);
    try
    {
        uint64_t r = 0;
        switch (b->resultKind)
        {
            case 0:
                reinterpret_cast<void (*)(Dn2CppObject*, int16_t)>(
                    const_cast<void*>(b->getResultFn))(b->vts, b->version);
                break;
            case 1:
                r = static_cast<uint64_t>(static_cast<uint32_t>(
                    reinterpret_cast<int32_t (*)(Dn2CppObject*, int16_t)>(
                        const_cast<void*>(b->getResultFn))(b->vts, b->version)));
                break;
            case 2:
                r = static_cast<uint64_t>(
                    reinterpret_cast<int64_t (*)(Dn2CppObject*, int16_t)>(
                        const_cast<void*>(b->getResultFn))(b->vts, b->version));
                break;
            default:
                r = static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
                    reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*, int16_t)>(
                        const_cast<void*>(b->getResultFn))(b->vts, b->version)));
                break;
        }
        dn2cpp_task_set_result(b->task, r);
    }
    catch (const Dn2CppException& e)
    {
        dn2cpp_task_set_exception(b->task, e.obj); // rooted via the task
        dn2cpp_exc_inflight_pop(e.obj);
    }
    dn2cpp_principal_left(g_inflight_async_tasks);
}

Dn2CppTask* dn2cpp_vts_task(Dn2CppObject* vts, int16_t version,
                            const void* getResultFn, const void* onCompletedFn,
                            const Dn2CppTypeInfo* actionTi, int32_t resultKind)
{
    auto* b = static_cast<Dn2CppVtsBridge*>(dn2cpp_alloc(sizeof(Dn2CppVtsBridge)));
    b->header.type = &dn2cpp_vts_bridge_type;
    b->vts = vts;
    dn2cpp_gc_store_ref(&b->task, dn2cpp_task_alloc());
    b->getResultFn = getResultFn;
    b->version = version;
    b->resultKind = resultKind;
    auto* del = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    del->type = actionTi;
    del->target = &b->header;
    del->method = reinterpret_cast<void*>(&dn2cpp_vts_continuation);
    del->prev = nullptr;
    // Count the bridge in flight BEFORE registering: OnCompleted may invoke the
    // continuation synchronously (source already completed), and the continuation
    // decrements on settle either way.
    g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
    // OnCompleted(continuation, state, token, ValueTaskSourceOnCompletedFlags.None).
    // The state is the bridge itself — the continuation reads everything from its
    // delegate target, but passing it keeps the source's _continuationState field
    // rooting the bridge for the whole pending window.
    reinterpret_cast<void (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*, int16_t, int32_t)>(
        const_cast<void*>(onCompletedFn))(vts, del, &b->header, version, 0);
    return b->task;
}
