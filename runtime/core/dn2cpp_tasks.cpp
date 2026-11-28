#include "dn2cpp_metadata_native.h"
// dn2cpp_tasks.cpp — async machinery of the dn2cpp runtime:
// the async/await scheduler, cancellation,
// TaskCompletionSource, System.Threading.Thread, the Task.Run worker pool,
// cold tasks, ContinueWith, and the IValueTaskSource-backed ValueTask bridge.

#include "dn2cpp_core.h"
#include "dn2cpp_wait_condition.h"
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
Dn2CppTypeInfo dn2cpp_task_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Tasks.Task", &dn2cpp_object_type, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppTask), 0, 0, 0, 0, 0, dn2cpp_nonpublic_default_ctor_with_cctor_reflection }, &dn2cpp_task_type_obj);
const Dn2CppType dn2cpp_task_type_obj = { { &dn2cpp_type_type }, &dn2cpp_task_type };
const Dn2CppTypeInfo dn2cpp_task_factory_type = {
    "System.Threading.Tasks.TaskFactory", &dn2cpp_object_type, nullptr, nullptr,
    nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr,
    (int32_t)sizeof(Dn2CppTaskFactory)
};
extern const Dn2CppType dn2cpp_taskscheduler_type_obj;
static Dn2CppTypeReflection g_taskscheduler_reflection = dn2cpp_native_type_identity(nullptr,
    DN2CPP_TA_OMITTED_NONPUBLIC_DEFAULT_CTOR | DN2CPP_TA_DECLARED_NONPUBLIC_CCTOR);

const Dn2CppTypeInfo dn2cpp_taskscheduler_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Tasks.TaskScheduler", &dn2cpp_object_type, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppTaskScheduler), 0, 0, 0, 0, 0, &g_taskscheduler_reflection }, &dn2cpp_taskscheduler_type_obj);
const Dn2CppType dn2cpp_taskscheduler_type_obj = { { &dn2cpp_type_type }, &dn2cpp_taskscheduler_type };

void dn2cpp_set_taskscheduler_identity_metadata(Dn2CppMetadataTable<Dn2CppMethodInfo> methods,
    Dn2CppMetadataTable<Dn2CppPropInfo> props, int32_t propertyCount)
{
    g_taskscheduler_reflection.methods = methods;
    g_taskscheduler_reflection.methodCount = 1;
    g_taskscheduler_reflection.props = props;
    g_taskscheduler_reflection.propCount = propertyCount;
}

// CoreLib initializes the default pool scheduler (including its Id) on the
// first static field access, even when that access is another scheduler's Id.
struct Dn2CppTaskSchedulerStatics
{
    int32_t counter = 0;
    Dn2CppTaskScheduler pool{{&dn2cpp_taskscheduler_type}, ++counter};
};

static Dn2CppTaskSchedulerStatics& dn2cpp_taskscheduler_statics()
{
    static Dn2CppTaskSchedulerStatics state;
    return state;
}

Dn2CppObject* dn2cpp_taskscheduler_default()
{
    return &dn2cpp_taskscheduler_statics().pool;
}

int32_t dn2cpp_taskscheduler_default_try_dequeue(Dn2CppObject*, Dn2CppTask*)
{
    dn2cpp_throw_platform_not_supported("TaskScheduler.TryDequeue on the default scheduler requires a worker-local queue");
}

int32_t* dn2cpp_taskscheduler_id_counter()
{
    return &dn2cpp_taskscheduler_statics().counter;
}

// Task.Id numbering: positive, monotonic, minted once per task at alloc time.
// (Real .NET's Ids are also positive and unique; the numbering itself differs.)
// Function-local static so the counter is initialized before any static-init-time
// task allocation (dn2cpp_task_default_completed) can ask for an id.
static int32_t dn2cpp_task_next_id()
{
    static std::atomic<int32_t> next{1};
    return next.fetch_add(1, std::memory_order_relaxed);
}

static void dn2cpp_vts_arm(Dn2CppTask* t);

static std::mutex& g_task_mtx = dn2cpp_never_destroyed<std::mutex>();
static Dn2CppWaitCondition& g_task_completed = dn2cpp_never_destroyed<Dn2CppWaitCondition>();
static uint32_t g_task_completion_waiters; // protected by g_task_mtx

// Generated thunks keep the holder's real list, capture, and observation
// semantics in managed code without embedding any of its field offsets here.
static Dn2CppObject* (*g_task_holder_create)(Dn2CppTask*);
static void (*g_task_holder_add)(Dn2CppObject*, Dn2CppObject*, int32_t);
static Dn2CppArrayRef* (*g_task_holder_snapshot)(Dn2CppObject*);
static Dn2CppObject* (*g_task_holder_cancellation)(Dn2CppObject*);
static void (*g_task_holder_observe)(Dn2CppObject*);
static void (*g_task_holder_rethrow)(Dn2CppObject*, int32_t);
static Dn2CppArrayRef* (*g_task_holder_dispatch)(Dn2CppObject*);
static Dn2CppObject* (*g_task_holder_cancel_dispatch)(Dn2CppObject*);
static Dn2CppArrayRef* (*g_task_holder_normalize_dispatch)(Dn2CppArrayRef*);

enum
{
    DN2CPP_TASK_HOLDER_UNPUBLISHED = 0,
    DN2CPP_TASK_HOLDER_RESERVED,
    DN2CPP_TASK_HOLDER_OWNS_FAULTS,
};

void dn2cpp_set_task_exception_holder_hooks(
    Dn2CppObject* (*create)(Dn2CppTask*),
    void (*add)(Dn2CppObject*, Dn2CppObject*, int32_t),
    Dn2CppArrayRef* (*snapshotFaults)(Dn2CppObject*),
    Dn2CppObject* (*snapshotCancellation)(Dn2CppObject*),
    void (*observe)(Dn2CppObject*))
{
    g_task_holder_create = create;
    g_task_holder_add = add;
    g_task_holder_snapshot = snapshotFaults;
    g_task_holder_cancellation = snapshotCancellation;
    g_task_holder_observe = observe;
}

void dn2cpp_set_task_exception_holder_rethrow(void (*rethrow)(Dn2CppObject*, int32_t))
{
    g_task_holder_rethrow = rethrow;
}

void dn2cpp_set_task_exception_holder_dispatch(
    Dn2CppArrayRef* (*faults)(Dn2CppObject*), Dn2CppObject* (*cancellation)(Dn2CppObject*),
    Dn2CppArrayRef* (*normalize)(Dn2CppArrayRef*))
{
    g_task_holder_dispatch = faults;
    g_task_holder_cancel_dispatch = cancellation;
    g_task_holder_normalize_dispatch = normalize;
}

static Dn2CppObject* dn2cpp_task_holder_for_observation(Dn2CppTask* task)
{
    if (g_task_holder_create == nullptr)
        return nullptr;
    if (auto* holder = task->exceptionHolder.load(std::memory_order_acquire))
        return holder;
    std::lock_guard<std::mutex> lock(g_task_mtx);
    auto* holder = task->exceptionHolder.load(std::memory_order_relaxed);
    if (holder == nullptr)
        task->nativeFaultObserved = true;
    return holder;
}

void dn2cpp_task_add_exception(Dn2CppTask* task, Dn2CppObject* exception, int32_t representsCancellation)
{
    dn2cpp_null_check(task);
    if (g_task_holder_create == nullptr || g_task_holder_add == nullptr || g_task_holder_snapshot == nullptr
        || g_task_holder_cancellation == nullptr || g_task_holder_observe == nullptr)
        dn2cpp_throw_platform_not_supported("The managed Task exception holder is unavailable");
    auto* holder = task->exceptionHolder.load(std::memory_order_acquire);
    if (holder == nullptr)
    {
        auto* candidate = g_task_holder_create(task);
        Dn2CppMonitorGuard guard(candidate);
        bool seeded = false;
        bool seededFaults = false;
        for (;;)
        {
            Dn2CppObject* first = nullptr;
            Dn2CppObject* aggregate = nullptr;
            bool publish = false;
            bool observed = false;
            {
                std::lock_guard<std::mutex> lock(g_task_mtx);
                holder = task->exceptionHolder.load(std::memory_order_relaxed);
                if (holder == nullptr)
                {
                    if (!seeded && task->status == DN2CPP_TASK_FAULTED)
                    {
                        first = task->exception;
                        aggregate = task->exceptionAggregate.load(std::memory_order_acquire);
                    }
                    else
                    {
                        if (seededFaults)
                            task->exceptionHolderState = DN2CPP_TASK_HOLDER_OWNS_FAULTS;
                        observed = task->nativeFaultObserved;
                        dn2cpp_gc_store_ref(&task->exceptionHolder, candidate);
                        publish = true;
                    }
                }
            }
            if (publish)
            {
                if (observed)
                    g_task_holder_observe(candidate);
                g_task_holder_add(candidate, exception, representsCancellation);
                return;
            }
            if (holder != nullptr)
            {
                // A racing holder may have been seeded; never finalize both.
                g_task_holder_observe(candidate);
                break;
            }
            // Seed native faults before exposing the holder. The publication
            // loop rechecks both holder identity and the pending-to-faulted race.
            if (aggregate != nullptr)
            {
                auto* faults = dn2cpp_aggregate_inner_exceptions(aggregate, nullptr);
                for (int32_t i = 0; i < faults->length; ++i)
                    g_task_holder_add(candidate, faults->data[i], false);
                seededFaults = faults->length != 0;
            }
            else if (first != nullptr)
            {
                g_task_holder_add(candidate, first, false);
                seededFaults = true;
            }
            seeded = true;
        }
    }
    // The holder may enumerate user objects. Do not run this under g_task_mtx;
    // a recursive monitor also permits the managed callback to reenter Add.
    Dn2CppMonitorGuard guard(holder);
    g_task_holder_add(holder, exception, representsCancellation);
}

// TLS contains only the address of the active scanned stack scope, not a GC pointer.
struct Dn2CppTaskDelegateScope
{
    static thread_local Dn2CppTaskDelegateScope* active;
    Dn2CppTask* task;
    Dn2CppObject* scheduler;
    int32_t hiddenId = 0; // a Task.Run unwrap owns a distinct hidden task
    Dn2CppTaskDelegateScope* previous = active;

    explicit Dn2CppTaskDelegateScope(Dn2CppTask* current, Dn2CppObject* owner = nullptr)
        : task(current), scheduler(owner) { active = this; }
    ~Dn2CppTaskDelegateScope() { active = previous; }
    Dn2CppTaskDelegateScope(const Dn2CppTaskDelegateScope&) = delete;
    Dn2CppTaskDelegateScope& operator=(const Dn2CppTaskDelegateScope&) = delete;
};

thread_local Dn2CppTaskDelegateScope* Dn2CppTaskDelegateScope::active = nullptr;

int32_t dn2cpp_task_current_id()
{
    auto* scope = Dn2CppTaskDelegateScope::active;
    if (scope == nullptr) return 0;
    if (scope->task != nullptr) return scope->task->id;
    if (scope->hiddenId == -1) scope->hiddenId = dn2cpp_task_next_id();
    return scope->hiddenId;
}

Dn2CppObject* dn2cpp_taskscheduler_current()
{
    auto* scope = Dn2CppTaskDelegateScope::active;
    if (scope != nullptr && scope->scheduler != nullptr && !(scope->task->creationOptions & DN2CPP_TASK_HIDE_SCHEDULER))
        return scope->scheduler;
    return dn2cpp_taskscheduler_default();
}

template<class Fn>
static decltype(auto) dn2cpp_task_invoke_as(Dn2CppTask* task, Fn&& invoke)
{
    Dn2CppTaskDelegateScope scope(task);
    return invoke();
}

static Dn2CppTask* dn2cpp_task_attaching_parent(int32_t options)
{
    if ((options & DN2CPP_TASK_ATTACHED_TO_PARENT) == 0)
        return nullptr;
    const auto* scope = Dn2CppTaskDelegateScope::active;
    auto* parent = scope != nullptr ? scope->task : nullptr;
    return parent != nullptr && (parent->creationOptions.load(std::memory_order_relaxed) & DN2CPP_TASK_DENY_CHILD_ATTACH) == 0
        ? parent : nullptr;
}

static void dn2cpp_task_init(Dn2CppTask* t, int32_t options, Dn2CppCancelSource* source)
{
    // Relaxed here and in every other pre-completed mint below: the task is private
    // to this thread until the caller publishes it, and that handoff carries it.
    t->status.store(DN2CPP_TASK_PENDING, std::memory_order_relaxed);
    t->id = dn2cpp_task_next_id();
    t->exception = nullptr;
    t->exceptionAggregate.store(nullptr, std::memory_order_relaxed);
    t->exceptionHolder.store(nullptr, std::memory_order_relaxed);
    t->exceptionHolderState = DN2CPP_TASK_HOLDER_UNPUBLISHED;
    t->nativeFaultObserved = false;
    t->result = 0;
    t->continuations = nullptr;
    t->workerKeepAlive = nullptr;
    t->cold = nullptr;
    t->vtsBridge = nullptr;
    t->startKind = DN2CPP_TASK_ORIGIN_PROMISE;
    t->execution.store(DN2CPP_TASK_NOT_STARTED, std::memory_order_relaxed);
    t->asyncState = nullptr;
    t->completionWaitHandle = nullptr;
    t->delayTimer.store(nullptr, std::memory_order_relaxed);
    t->disposed = 0;
    t->creationOptions.store(options & DN2CPP_TASK_OPTIONS_MASK, std::memory_order_relaxed);
    dn2cpp_gc_store_ref(&t->cancellationSource, source);
    t->finishProvenance.store(0, std::memory_order_relaxed);
    if (auto* parent = dn2cpp_task_attaching_parent(options))
    {
        t->finishProvenance.store(DN2CPP_TASK_HAS_UNTRACKED_PARENT, std::memory_order_relaxed);
        parent->finishProvenance.fetch_or(DN2CPP_TASK_HAS_UNTRACKED_CHILD, std::memory_order_release);
    }
}

Dn2CppTask* dn2cpp_task_alloc(int32_t options, Dn2CppCancelSource* source)
{
    auto* t = static_cast<Dn2CppTask*>(dn2cpp_alloc(sizeof(Dn2CppTask)));
    t->type = &dn2cpp_task_type;
    dn2cpp_task_init(t, options, source);
    return t;
}

// Called under g_task_mtx: cancellation and a protected start have a single winner
// order. A completed or faulted promise need not ever have been scheduled.
static bool dn2cpp_task_mark_started_locked(Dn2CppTask* task)
{
    if (task->status == DN2CPP_TASK_CANCELED)
        return false;
    return (task->execution.fetch_or(DN2CPP_TASK_SCHEDULED) & DN2CPP_TASK_SCHEDULED) == 0;
}

int32_t dn2cpp_task_mark_started(Dn2CppTask* task)
{
    dn2cpp_null_check(task);
    std::lock_guard<std::mutex> lock(g_task_mtx);
    return dn2cpp_task_mark_started_locked(task);
}

int32_t dn2cpp_task_public_status(Dn2CppTask* task)
{
    dn2cpp_null_check(task);
    int32_t execution = task->execution.load(std::memory_order_acquire);
    switch (task->status.load(std::memory_order_acquire))
    {
        case DN2CPP_TASK_SUCCEEDED: return 5; // RanToCompletion
        case DN2CPP_TASK_CANCELED: return 6;
        case DN2CPP_TASK_FAULTED: return 7;
        default:
            if (execution & DN2CPP_TASK_INVOKED) return 3; // Running
            if (execution & DN2CPP_TASK_SCHEDULED) return 2; // WaitingToRun
            int32_t origin = task->startKind.load(std::memory_order_acquire);
            return origin == DN2CPP_TASK_ORIGIN_COLD || origin == DN2CPP_TASK_ORIGIN_UNOWNED_QUEUE
                ? 0 : 1; // Created / WaitingForActivation
    }
}

Dn2CppCancelToken dn2cpp_task_cancellation_token(Dn2CppTask* task)
{
    dn2cpp_null_check(task);
    return Dn2CppCancelToken{task->cancellationSource.load(std::memory_order_acquire)};
}

void dn2cpp_task_init_promise(Dn2CppTask* task, Dn2CppObject* state, int32_t options)
{
    dn2cpp_null_check(task);
    if ((options & ~0x44) != 0)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, "creationOptions");
    if (dn2cpp_task_attaching_parent(options) != nullptr)
        dn2cpp_throw_platform_not_supported("Task promise attachment requires parent-task tracking");
    // Keep the allocating subtype's header and fields. Ordinary continuations
    // already queue to their owner scheduler, including the asynchronous option.
    dn2cpp_task_init(task, options, nullptr);
    dn2cpp_gc_store_ref(&task->asyncState, state);
}

void dn2cpp_task_dispose(Dn2CppTask* task)
{
    dn2cpp_null_check(task);
    Dn2CppObject* handle = nullptr;
    bool pending;
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        pending = task->status.load(std::memory_order_acquire) == DN2CPP_TASK_PENDING;
        if (!pending && task->disposed >= 0)
        {
            task->disposed = 1;
            handle = task->completionWaitHandle;
        }
    }
    if (pending)
        dn2cpp_throw_invalid_operation_msg(
            "A task may only be disposed if it is in a completion state (RanToCompletion, Faulted or Canceled).");
    if (handle != nullptr)
        dn2cpp_waithandle_close(handle);
}

Dn2CppObject* dn2cpp_task_async_wait_handle(Dn2CppTask* task)
{
    dn2cpp_null_check(task);
    Dn2CppObject* handle;
    bool disposed;
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        disposed = task->disposed > 0;
        handle = task->completionWaitHandle;
    }
    if (disposed)
        dn2cpp_throw_object_disposed();
    if (handle != nullptr)
        return handle;
    if (task->vtsBridge != nullptr)
        dn2cpp_vts_arm(task);
    Dn2CppObject* candidate = dn2cpp_task_event_new();
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        disposed = task->disposed > 0;
        handle = task->completionWaitHandle;
        if (!disposed && handle == nullptr)
        {
            handle = candidate;
            dn2cpp_gc_store_ref(&task->completionWaitHandle, handle);
            if (task->status.load(std::memory_order_acquire) != DN2CPP_TASK_PENDING)
                dn2cpp_event_set_if_open(handle);
        }
    }
    if (handle != candidate)
        dn2cpp_waithandle_close(candidate);
    if (disposed)
        dn2cpp_throw_object_disposed();
    return handle;
}

Dn2CppTask* dn2cpp_async_task_alloc()
{
    auto* task = dn2cpp_task_alloc();
    task->startKind = DN2CPP_TASK_ORIGIN_ASYNC_UNEXPOSED;
    return task;
}

Dn2CppTask* dn2cpp_asyncbuilder_task(Dn2CppAsyncBuilder* builder)
{
    if (builder->task == nullptr)
        dn2cpp_gc_store_ref(&builder->task, dn2cpp_async_task_alloc());
    Dn2CppTask* task = builder->task;
    std::lock_guard<std::mutex> lk(g_task_mtx);
    int32_t origin = DN2CPP_TASK_ORIGIN_ASYNC_UNEXPOSED;
    int32_t exposedOrigin = task->status.load(std::memory_order_acquire) == DN2CPP_TASK_SUCCEEDED
        ? DN2CPP_TASK_ORIGIN_PRECOMPLETED : DN2CPP_TASK_ORIGIN_PROMISE;
    task->startKind.compare_exchange_strong(origin, exposedOrigin, std::memory_order_relaxed);
    return task;
}

void dn2cpp_async_task_suspend(Dn2CppTask* task)
{
    if (task == nullptr)
        return;
    std::lock_guard<std::mutex> lk(g_task_mtx);
    int32_t origin = DN2CPP_TASK_ORIGIN_ASYNC_UNEXPOSED;
    task->startKind.compare_exchange_strong(origin, DN2CPP_TASK_ORIGIN_PROMISE,
        std::memory_order_relaxed);
}

Dn2CppTask* dn2cpp_task_completed()
{
    auto* t = dn2cpp_task_alloc();
    t->startKind = DN2CPP_TASK_ORIGIN_PRECOMPLETED;
    t->status.store(DN2CPP_TASK_SUCCEEDED, std::memory_order_relaxed);
    t->disposed = -1;
    return t;
}

// The shared pre-completed sentinel a default(ValueTask)'s null task normalizes
// to. Its status and result are immutable; IAsyncResult may lazily attach a wait
// handle. Like the BCL cached tasks, Dispose does not close that shared handle.
// Aggregate-initialized: an atomic member makes Dn2CppTask non-copyable.
Dn2CppTask dn2cpp_task_default_completed{
    { &dn2cpp_task_type }, DN2CPP_TASK_SUCCEEDED,
    dn2cpp_task_next_id(), nullptr, nullptr, 0, nullptr, nullptr, nullptr, nullptr,
    DN2CPP_TASK_ORIGIN_PRECOMPLETED, DN2CPP_TASK_NOT_STARTED, nullptr, nullptr, -1, 0, nullptr, nullptr, 0, false, 0, nullptr };

Dn2CppTask* dn2cpp_task_from_result(uint64_t result, bool doNotDispose)
{
    auto* t = dn2cpp_task_alloc();
    t->startKind = DN2CPP_TASK_ORIGIN_PRECOMPLETED;
    t->status.store(DN2CPP_TASK_SUCCEEDED, std::memory_order_relaxed);
    t->result = result;
    t->disposed = doNotDispose ? -1 : 0;
    dn2cpp_gc_write_barrier(&t->result);
    return t;
}

Dn2CppTask* dn2cpp_task_stamp(Dn2CppTask* task, const Dn2CppTypeInfo* type)
{
    if (task == &dn2cpp_task_default_completed)
        task = dn2cpp_task_from_result(0);
    if (task->type == type)
        return task;
    if (task->type != &dn2cpp_task_type)
        dn2cpp_throw_invalid_operation_msg("A Task cannot change its closed result type.");
    task->type = type;
    return task;
}

Dn2CppTask** dn2cpp_task_default_result_field(const Dn2CppTypeInfo* taskType)
{
    // Task<TResult>.s_defaultResultTask has one GC-rooted slot per closed
    // type, independent of the small-integer and Boolean FromResult caches.
    struct Entry { const Dn2CppTypeInfo* type; Dn2CppTask* task; Entry* next; };
    static DN2CPP_GC_STATIC_ROOT Entry* entries = nullptr;
    std::lock_guard<std::mutex> lock(g_task_mtx);
    for (auto* entry = entries; entry != nullptr; entry = entry->next)
        if (entry->type == taskType)
            return &entry->task;
    auto* entry = static_cast<Entry*>(dn2cpp_alloc(sizeof(Entry)));
    entry->type = taskType;
    dn2cpp_gc_store_ref(&entry->task, dn2cpp_task_stamp(dn2cpp_task_from_result(0, true), taskType));
    dn2cpp_gc_store_ref(&entry->next, entries);
    dn2cpp_gc_store_ref(&entries, entry);
    return &entry->task;
}

Dn2CppTaskFactory* dn2cpp_default_task_factory(const Dn2CppTypeInfo* type)
{
    // The root mirrors the static backing field on each closed Task<TResult>.
    struct Entry : Dn2CppTaskFactory { Entry* next; };
    static DN2CPP_GC_STATIC_ROOT Entry* factories = nullptr;
    std::lock_guard<std::mutex> lock(g_task_mtx);
    for (auto* entry = factories; entry != nullptr; entry = entry->next)
        if (entry->type == type)
            return entry;
    auto* entry = static_cast<Entry*>(dn2cpp_alloc(sizeof(Entry)));
    entry->type = type;
    dn2cpp_gc_store_ref(&entry->next, factories);
    dn2cpp_gc_store_ref(&factories, entry);
    return entry;
}

static Dn2CppString* dn2cpp_task_result_tostring(Dn2CppTask* task,
                                                  const Dn2CppTypeInfo* resultType)
{
    if (resultType == nullptr)
        return dn2cpp_string_from_utf8("", 0);

    if ((resultType->flags & DN2CPP_TF_VALUETYPE) == 0)
        return dn2cpp_object_tostring(
            reinterpret_cast<Dn2CppObject*>(static_cast<uintptr_t>(task->result)));

    uint64_t bits = task->result;
    if (resultType == &dn2cpp_single_type)
    {
        float value = static_cast<float>(dn2cpp_bits_r8(bits));
        return dn2cpp_object_tostring(dn2cpp_box(resultType, &value, sizeof(value)));
    }
    if (resultType == &dn2cpp_double_type)
    {
        double value = dn2cpp_bits_r8(bits);
        return dn2cpp_object_tostring(dn2cpp_box(resultType, &value, sizeof(value)));
    }
    if (resultType == &dn2cpp_bool_type || resultType == &dn2cpp_char_type
        || resultType == &dn2cpp_byte_type || resultType == &dn2cpp_sbyte_type
        || resultType == &dn2cpp_int16_type || resultType == &dn2cpp_uint16_type
        || resultType == &dn2cpp_int32_type || resultType == &dn2cpp_uint32_type)
    {
        // The task slot carries every sub-word kind widened to I4, and normal
        // boxing stores that full promoted payload. Preserve the sign extension:
        // object_tostring intentionally reads all of these as int32_t.
        int32_t value = static_cast<int32_t>(bits);
        return dn2cpp_object_tostring(dn2cpp_box(resultType, &value, sizeof(value)));
    }
    const void* payload = &bits;
    if ((resultType->flags & DN2CPP_TF_PRIMITIVE) == 0
        && (resultType->flags & DN2CPP_TF_ENUM) == 0)
    {
        payload = reinterpret_cast<const void*>(static_cast<uintptr_t>(bits));
        // No stored struct payload (including default(ValueTask<T>)) means
        // default(T). Do not mutate the shared default-task sentinel.
        if (payload == nullptr)
            payload = dn2cpp_alloc(static_cast<size_t>(resultType->instanceSize));
    }

    // Nullable<T> is the one value type whose virtual ToString observes its boxing
    // rule. Its generated field getters already know the concrete layout, so use
    // them rather than guessing the value field's alignment here.
    if (resultType->genericDef != nullptr && resultType->genericDef->name != nullptr
        && std::strcmp(resultType->genericDef->name, "System.Nullable`1") == 0
        && payload != nullptr)
    {
        Dn2CppObject* nullableBox = dn2cpp_box(resultType, payload, resultType->instanceSize);
        Dn2CppMetadataHandle<Dn2CppFieldInfo> hasValue = nullptr;
        Dn2CppMetadataHandle<Dn2CppFieldInfo> value = nullptr;
        for (int32_t i = 0; i < resultType->reflection().fieldCount; i++)
        {
            if (std::strcmp(resultType->reflection().fields[i]->name, "hasValue") == 0)
                hasValue = resultType->reflection().fields[i];
            else if (std::strcmp(resultType->reflection().fields[i]->name, "value") == 0)
                value = resultType->reflection().fields[i];
        }
        if (hasValue == nullptr || value == nullptr)
            return dn2cpp_string_from_utf8("", 0);
        Dn2CppObject* hasValueBox = hasValue->getter(nullableBox);
        if (hasValueBox == nullptr
            || *reinterpret_cast<int32_t*>(hasValueBox + 1) == 0)
            return dn2cpp_string_from_utf8("", 0);
        return dn2cpp_object_tostring(value->getter(nullableBox));
    }

    return dn2cpp_object_tostring(
        dn2cpp_box(resultType, payload, resultType->instanceSize));
}

// ValueTask<T>.ToString reads Result once IsCompletedSuccessfully holds, which consumes a
// source-backed operation as it does on the CLR.
Dn2CppString* dn2cpp_valuetask_tostring(Dn2CppTaskAwaiter* valueTask,
                                        const Dn2CppTypeInfo* resultType)
{
    Dn2CppTask* task = dn2cpp_vtask(valueTask != nullptr ? valueTask->task : nullptr);
    if (dn2cpp_vtask_status(task) != DN2CPP_TASK_SUCCEEDED)
        return dn2cpp_string_from_utf8("", 0);
    dn2cpp_vts_block(task);
    return dn2cpp_task_result_tostring(task, resultType);
}

Dn2CppString* dn2cpp_valuetask_box_tostring(Dn2CppObject* valueTaskBox)
{
    const Dn2CppTypeInfo* valueTaskType = valueTaskBox != nullptr ? valueTaskBox->type : nullptr;
    const Dn2CppTypeInfo* resultType = valueTaskType != nullptr
        && valueTaskType->genericArgCount == 1 ? valueTaskType->genericArgs[0] : nullptr;
    return dn2cpp_valuetask_tostring(
        reinterpret_cast<Dn2CppTaskAwaiter*>(valueTaskBox + 1), resultType);
}

// A source-backed task's completion signal: every continuation registration, blocking
// wait and AsTask over it arms the bridge first (the bridge section, end of file).
static void dn2cpp_vts_arm(Dn2CppTask* t);

Dn2CppTask* dn2cpp_vtask_as_task(Dn2CppTask* task, const Dn2CppTypeInfo* type, bool cacheDefault)
{
    if (task == nullptr || task == &dn2cpp_task_default_completed)
        return dn2cpp_task_stamp(dn2cpp_task_from_result(0, cacheDefault), type);
    if (task->vtsBridge != nullptr)
        dn2cpp_vts_arm(task);
    return task;
}

Dn2CppTaskCompletionSource* dn2cpp_tcs_alloc(const Dn2CppTypeInfo* type,
                                              const Dn2CppTypeInfo* taskType, Dn2CppObject* state, int32_t options)
{
    auto* source = static_cast<Dn2CppTaskCompletionSource*>(
        dn2cpp_alloc(sizeof(Dn2CppTaskCompletionSource)));
    source->type = type;
    dn2cpp_gc_store_ref(&source->task, dn2cpp_task_stamp(dn2cpp_task_alloc(options), taskType));
    dn2cpp_gc_store_ref(&source->task->asyncState, state);
    return source;
}

static Dn2CppObject* dn2cpp_make_task_canceled_exception(Dn2CppTask* task);

static Dn2CppArrayRef* dn2cpp_task_fault_snapshot(Dn2CppTask* t);

void dn2cpp_task_throw_if_faulted(Dn2CppTask* t)
{
    // Captured cancellation exceptions retain their identity and trace. A
    // synthetic cancellation has no captured EDI and creates a fresh exception
    // for each await, including awaits through a forwarding promise.
    if (t != nullptr && (t->status == DN2CPP_TASK_FAULTED || t->status == DN2CPP_TASK_CANCELED))
    {
        Dn2CppObject* exception = t->exception;
        auto* holder = t->status == DN2CPP_TASK_FAULTED
            ? dn2cpp_task_holder_for_observation(t) : t->exceptionHolder.load(std::memory_order_acquire);
        if (holder != nullptr)
        {
            Dn2CppMonitorGuard guard(holder);
            if (t->status == DN2CPP_TASK_FAULTED)
            {
                g_task_holder_observe(holder);
                if (t->exceptionHolderState == DN2CPP_TASK_HOLDER_OWNS_FAULTS && g_task_holder_rethrow != nullptr)
                    g_task_holder_rethrow(holder, false);
            }
            else
            {
                if (g_task_holder_rethrow != nullptr)
                    g_task_holder_rethrow(holder, true);
                if (auto* cancellation = g_task_holder_cancellation(holder))
                    exception = cancellation;
            }
        }
        if (t->status == DN2CPP_TASK_FAULTED && exception == nullptr)
        {
            auto* faults = dn2cpp_task_fault_snapshot(t);
            if (faults->length == 0)
                dn2cpp_throw(dn2cpp_aggregate_exception_new(faults));
            exception = faults->data[0];
            if (exception == nullptr)
                dn2cpp_throw_null_reference();
        }
        if (t->status == DN2CPP_TASK_CANCELED && exception == nullptr)
            dn2cpp_throw(dn2cpp_make_task_canceled_exception(t));
        dn2cpp_rethrow(exception);
    }
}

// Per-thread cooperative run queue + virtual clock (the "scheduler"). Each thread
// owns one (thread_local). Resumptions posted here run when the thread blocks on a
// task (Task.Wait/.Result drains until it completes). Async on the main thread alone
// behaves exactly as the old single-threaded model: every continuation's owner is
// the main scheduler, every completion happens on main, the run-queue mutex is
// uncontended, and no cross-thread wakeup ever fires.
//   - virtual time advances only on the owner. Timer list changes use `mtx` so
//     another thread completing a timed wait can remove its losing timer.
//   - the run queue may receive cross-thread pushes (a worker completing a Task the
//     owner awaits routes the continuation here) -> guarded by `mtx`, with `cv` so
//     task_block can sleep until a cross-thread completion arrives.
using Dn2CppTaskClock = std::chrono::steady_clock;

// The same wall/virtual origin serves frame pumps and nested finite waits.
struct Dn2CppSchedulerClock
{
    Dn2CppTaskClock::time_point wall;
    int64_t logical;

    int64_t at(Dn2CppTaskClock::time_point time) const
    {
        return logical + std::chrono::duration_cast<std::chrono::milliseconds>(time - wall).count();
    }
};

struct Dn2CppTimer; // full definition below
struct Dn2CppScheduler
{
    Dn2CppCont* head = nullptr;
    Dn2CppCont* tail = nullptr;
    Dn2CppTimer* timers = nullptr;       // guarded by mtx
    Dn2CppTimer* timers_tail = nullptr;  // guarded by mtx
    Dn2CppTimer* ready_timers = nullptr;
    Dn2CppTimer* ready_timers_tail = nullptr;
    std::thread::id timer_publisher{};   // guarded by mtx
    bool notify_timer_observers = false;
    int64_t virtual_now = 0;             // owner-only
    Dn2CppSchedulerClock frame_clock{};
    Dn2CppSchedulerClock* clock = nullptr; // owner-only, frame clock or outer finite wait
    // Pool workers alternate this scheduler's owner-only work with the global
    // pool queue. The flag is also the scheduler's single settler principal:
    // an arbitrary number of queued resumptions/timers costs one count.
    bool pool_worker = false;                 // fixed before the worker runs managed code
    std::atomic<bool> pool_local_principal{false}; // transitions under mtx; read by pool wait
    std::mutex mtx;                      // guards continuation and timer lists
    Dn2CppWaitCondition cv;              // task_block waits here
};

struct Dn2CppTimedTaskWait;

// MemberwiseClone copies a task's managed payload. Active stack frames belong
// only to its original identity. Bucket links are protected by g_task_mtx.
static Dn2CppTimedTaskWait*& dn2cpp_timed_wait_bucket(Dn2CppTask* task)
{
    constexpr size_t count = 64;
    static Dn2CppTimedTaskWait* buckets[count]{};
    return buckets[(reinterpret_cast<uintptr_t>(task) / alignof(Dn2CppTask)) % count];
}

struct Dn2CppTimedTaskWait
{
    Dn2CppTask* task;
    Dn2CppScheduler* scheduler;
    Dn2CppSchedulerClock origin;
    Dn2CppTaskClock::time_point deadline;
    Dn2CppTimedTaskWait* next;
    bool completedInTime = true; // read only after acquiring terminal status

    Dn2CppTimedTaskWait(Dn2CppTask* t, Dn2CppScheduler* s, int32_t ms)
        : task(t), scheduler(s), origin{Dn2CppTaskClock::now(), s->virtual_now},
          deadline(origin.wall + std::chrono::milliseconds(ms))
    {
        if (s->clock == nullptr)
            s->clock = &origin;
        std::lock_guard<std::mutex> lock(g_task_mtx);
        auto& head = dn2cpp_timed_wait_bucket(t);
        next = head;
        head = this;
    }

    ~Dn2CppTimedTaskWait()
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        auto** waiter = &dn2cpp_timed_wait_bucket(task);
        while (*waiter != this)
            waiter = &(*waiter)->next;
        *waiter = next;
        if (scheduler->clock == &origin)
        {
            scheduler->virtual_now = origin.at(Dn2CppTaskClock::now());
            scheduler->clock = nullptr;
        }
    }
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

// Synchronize with each drain's predicate check before asking it to reconsider.
// A change in potential settlers does not enqueue work on the entry scheduler.
static void dn2cpp_sched_wake_all(Dn2CppScheduler* except = nullptr)
{
    std::vector<Dn2CppScheduler*> snapshot;
    {
        std::lock_guard<std::mutex> lk(g_sched_registry_mtx);
        snapshot = g_sched_registry;
    }
    for (Dn2CppScheduler* s : snapshot)
    {
        if (s == except)
            continue;
        { std::lock_guard<std::mutex> lk(s->mtx); }
        s->cv.recheck();
    }
}

static std::mutex& g_entry_mtx = dn2cpp_never_destroyed<std::mutex>();
static std::condition_variable& g_entry_cv = dn2cpp_never_destroyed<std::condition_variable>();
static Dn2CppTaskEntryScope* g_entries;
static thread_local Dn2CppTaskEntryScope* t_entry = nullptr;
static std::atomic<int> g_parked_entries{0};
static std::atomic<int> g_entry_pollers{0};
static uint64_t g_entry_poll_generation;

Dn2CppTaskEntryScope::~Dn2CppTaskEntryScope()
{
    if (t_entry != this)
        return;
    {
        std::lock_guard<std::mutex> lock(g_entry_mtx);
        auto** entry = &g_entries;
        while (*entry != this)
            entry = &(*entry)->next;
        *entry = next;
        t_entry = nullptr;
    }
    if (observed)
        dn2cpp_sched_wake_all(t_sched_ptr);
}

bool dn2cpp_on_entry_thread()
{
    return t_entry != nullptr;
}

void dn2cpp_entry_wait(const void* condition, std::unique_lock<std::mutex>& resource)
{
    std::unique_lock<std::mutex> lock(g_entry_mtx);
    bool newlyParked = t_entry->waitingOn == nullptr;
    if (newlyParked)
        g_parked_entries.fetch_add(1, std::memory_order_release);
    t_entry->waitingOn = condition;
    t_entry->notified = false;
    // Publish the wait before releasing the predicate lock. Never hold either
    // lock across the scheduler broadcast, which takes other scheduler locks.
    resource.unlock();
    if (newlyParked)
    {
        lock.unlock();
        dn2cpp_sched_wake_all(t_sched_ptr);
        lock.lock();
    }
    g_entry_cv.wait(lock, [] { return t_entry->notified; });
    lock.unlock();
    resource.lock();
}

void dn2cpp_entry_resume()
{
    std::lock_guard<std::mutex> lock(g_entry_mtx);
    if (t_entry->waitingOn != nullptr)
    {
        t_entry->waitingOn = nullptr;
        g_parked_entries.fetch_sub(1, std::memory_order_release);
    }
}

void dn2cpp_entry_notify(const void* condition, bool runnable)
{
    bool polling = runnable && g_entry_pollers.load(std::memory_order_acquire) != 0;
    if (!polling && g_parked_entries.load(std::memory_order_acquire) == 0)
        return;
    std::lock_guard<std::mutex> lock(g_entry_mtx);
    if (polling)
        ++g_entry_poll_generation;
    for (auto* entry = g_entries; entry != nullptr; entry = entry->next)
    {
        if (entry->waitingOn != condition && !(polling && entry->waitingOn == &g_entry_poll_generation))
            continue;
        if (runnable)
        {
            entry->waitingOn = nullptr;
            g_parked_entries.fetch_sub(1, std::memory_order_release);
        }
        entry->notified = true;
        g_entry_cv.notify_all();
    }
}

Dn2CppEntryPollingWait::Dn2CppEntryPollingWait(bool indefinite)
    : observing(indefinite && dn2cpp_on_entry_thread())
{
    if (observing)
        g_entry_pollers.fetch_add(1, std::memory_order_acq_rel);
}

Dn2CppEntryPollingWait::~Dn2CppEntryPollingWait()
{
    if (observing)
    {
        dn2cpp_entry_resume();
        g_entry_pollers.fetch_sub(1, std::memory_order_acq_rel);
    }
}

uint64_t Dn2CppEntryPollingWait::sample() const
{
    if (!observing)
        return 0;
    std::lock_guard<std::mutex> lock(g_entry_mtx);
    return g_entry_poll_generation;
}

void Dn2CppEntryPollingWait::park_if_unchanged(uint64_t sample) const
{
    if (!observing)
        return;
    {
        std::lock_guard<std::mutex> lock(g_entry_mtx);
        if (t_entry->waitingOn != nullptr || sample != g_entry_poll_generation)
            return;
        t_entry->waitingOn = &g_entry_poll_generation;
        g_parked_entries.fetch_add(1, std::memory_order_release);
    }
    dn2cpp_sched_wake_all(t_sched_ptr);
}

void Dn2CppEntryPollingWait::resume() const
{
    if (observing)
        dn2cpp_entry_resume();
}

void dn2cpp_entry_poll_changed()
{
    dn2cpp_entry_notify(&g_entry_poll_generation, true);
}

bool dn2cpp_scheduler_has_work()
{
    Dn2CppScheduler* s = dn2cpp_sched_self();
    std::lock_guard<std::mutex> lock(s->mtx);
    return s->head != nullptr || s->timers != nullptr;
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

Dn2CppTaskEntryScope::Dn2CppTaskEntryScope()
{
    // Native reentry on a managed caller keeps the caller's existing principal.
    if (t_entry != nullptr || t_on_user_thread || t_on_timer_thread
        || (t_sched_ptr != nullptr && t_sched_ptr->pool_worker))
        return;
    std::lock_guard<std::mutex> lock(g_entry_mtx);
    next = g_entries;
    g_entries = this;
    t_entry = this;
}

// True when another entry, pool item, user thread or armed timer could settle a task.
static bool dn2cpp_task_settler_exists()
{
    // A thread started by the entry can take over before it parks. Conversely,
    // a settling thread can wake the entry and immediately leave its counter.
    std::lock_guard<std::mutex> lock(g_entry_mtx);
    for (auto* entry = g_entries; entry != nullptr; entry = entry->next)
        if (entry != t_entry && entry->waitingOn == nullptr)
        {
            // Removal takes the same lock. A waiter either sees the departure
            // or marks the entry that must wake it when the callback returns.
            entry->observed = true;
            return true;
        }
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

// A user or timer waiter discounts itself. Its last peer can therefore leave
// that counter at one while still removing the last possible settler.
static void dn2cpp_principal_left(std::atomic<int>& counter)
{
    int previous = counter.fetch_sub(1, std::memory_order_acq_rel);
    if (previous == 1 || (previous == 2 && &counter != &g_inflight_async_tasks))
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

// Wake a pool worker waiting on either a global item or its owner-local scheduler.
static void dn2cpp_pool_wake_for_scheduler();

static thread_local bool t_wall_timers = false;

static bool dn2cpp_has_custom_sync_context()
{
    auto* context = dn2cpp_sync_ctx_get();
    // SynchronizationContext itself derives directly from Object and posts to
    // the pool. Only a derived context owns the await's resumption.
    return context != nullptr && context->type->base != &dn2cpp_object_type;
}

static void dn2cpp_sched_invoke(void (*fn)(void*), void* state, bool wallTimers)
{
    struct TimerScope
    {
        bool previous = t_wall_timers;
        ~TimerScope() { t_wall_timers = previous; }
    } scope;
    t_wall_timers = wallTimers;
    dn2cpp_task_invoke_as(nullptr, [&] { fn(state); });
}

static void dn2cpp_sched_invoke_cont(Dn2CppCont* c)
{
    dn2cpp_sched_invoke(c->fn, c->state, c->wallTimers);
}

static bool dn2cpp_sched_can_dispatch_pool()
{
#ifdef __EMSCRIPTEN__
    return false;
#else
    // Host callbacks stay on their own scheduler. User threads and pool
    // workers have a scoped guard even in an engine-hosted process.
    return Dn2CppGCThread::active || !dn2cpp_boundary_sink_installed();
#endif
}

static void dn2cpp_sched_pool_cont(Dn2CppObject*, Dn2CppObject* state)
{
    dn2cpp_sched_invoke_cont(reinterpret_cast<Dn2CppCont*>(state));
}

static void dn2cpp_sched_queue_pool_cont(Dn2CppCont* c)
{
    static Dn2CppDelegate runner{{&dn2cpp_object_type}, nullptr,
        reinterpret_cast<void*>(&dn2cpp_sched_pool_cont), nullptr, nullptr, nullptr, nullptr, false};
    // The pool roots the continuation and its state before a worker can run it.
    dn2cpp_threadpool_queue(&runner, reinterpret_cast<Dn2CppObject*>(c));
}

// A real Object is the Post state. Keep the context and suspended machine
// reachable through the task until publication, then through the pump's queue.
struct Dn2CppPostedAwait : Dn2CppObject
{
    Dn2CppObject* context;
    Dn2CppSyncContextPost post;
    void (*resume)(void*);
    void* state;
    bool wallTimers;
};

static Dn2CppPostedAwait* dn2cpp_capture_posted_await(void (*fn)(void*), void* state)
{
    auto post = dn2cpp_sync_ctx_get_post();
    if (post.fn == nullptr)
        return nullptr;
    auto* work = static_cast<Dn2CppPostedAwait*>(dn2cpp_alloc(sizeof(Dn2CppPostedAwait)));
    work->type = &dn2cpp_object_type;
    dn2cpp_gc_store_ref(&work->context, dn2cpp_sync_ctx_get());
    dn2cpp_gc_store_ref(&work->state, state);
    work->post = post;
    work->resume = fn;
    work->wallTimers = t_wall_timers;
    return work;
}

static void dn2cpp_resume_posted_await(Dn2CppObject*, Dn2CppObject* state)
{
    auto* work = static_cast<Dn2CppPostedAwait*>(state);
    dn2cpp_sched_invoke(work->resume, work->state, work->wallTimers);
}

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
    dn2cpp_gc_write_barrier(&c->state);
    c->owner = s;
    c->capturedContext = dn2cpp_has_custom_sync_context();
    c->wallTimers = t_wall_timers;
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
    dn2cpp_sched_invoke_cont(c);
    return true;
}

// The active drain's queue heads live on a scanned stack frame; TLS holds
// only the frame's address, including when a nested completion appends work.
struct Dn2CppJoinDrain
{
    struct Queue
    {
        Dn2CppCont* head = nullptr;
        Dn2CppCont* tail = nullptr;

        void append(Dn2CppCont* c)
        {
            dn2cpp_gc_store_ref(&c->next, static_cast<Dn2CppCont*>(nullptr));
            if (tail != nullptr)
                dn2cpp_gc_store_ref(&tail->next, c);
            else
                head = c;
            tail = c;
        }

        Dn2CppCont* pop()
        {
            auto* c = head;
            if (c == nullptr)
                return nullptr;
            head = c->next;
            if (head == nullptr)
                tail = nullptr;
            return c;
        }
    };

    static thread_local Dn2CppJoinDrain* active;
    Queue joins;
    Queue posts;
    bool owns = active == nullptr;

    Dn2CppJoinDrain()
    {
        if (owns)
            active = this;
    }

    ~Dn2CppJoinDrain()
    {
        if (owns)
            active = nullptr;
    }

    void run()
    {
        if (!owns)
            return;
        // Nested completions append their joins to the same iterative drain.
        while (auto* c = joins.pop())
            dn2cpp_task_invoke_as(nullptr, [&] { c->fn(c->state); });

        // Managed Post can depend on the joined result and can settle another
        // task itself. Release the drain only after publishing its joins.
        active = nullptr;
        while (auto* c = posts.pop())
            dn2cpp_task_invoke_as(nullptr, [&] { c->fn(c->state); });
    }
};

thread_local Dn2CppJoinDrain* Dn2CppJoinDrain::active = nullptr;

static void dn2cpp_invoke_posted_await(Dn2CppPostedAwait* work, bool reraise)
{
    auto* callback = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    callback->type = work->post.callbackType;
    callback->method = reinterpret_cast<void*>(&dn2cpp_resume_posted_await);
    try
    {
        // An independent join started by managed Post must finish before its
        // producer returns, not append to the drain paused at this call.
        struct PauseJoinDrain
        {
            Dn2CppJoinDrain* previous = Dn2CppJoinDrain::active;
            PauseJoinDrain() { Dn2CppJoinDrain::active = nullptr; }
            ~PauseJoinDrain() { Dn2CppJoinDrain::active = previous; }
        } pause;
        using Post = void (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*);
        reinterpret_cast<Post>(const_cast<void*>(work->post.fn))(work->context, callback, work);
    }
    catch (const Dn2CppException& e)
    {
        if (!reraise)
            throw;
        dn2cpp_async_await_rejected(e.obj);
    }
}

static void dn2cpp_post_await(void* state)
{
    // A rejected Post must not escape into the task's completing thread.
    dn2cpp_invoke_posted_await(static_cast<Dn2CppPostedAwait*>(state), true);
}

void dn2cpp_async_yield_on_completed(void (*fn)(void*), void* state, bool reraise)
{
    if (auto* work = dn2cpp_capture_posted_await(fn, state))
        dn2cpp_invoke_posted_await(work, reraise);
    else
        dn2cpp_sched_post(fn, state);
}

static void dn2cpp_cont_with_run(void* state);
static bool dn2cpp_cont_with_prepare(Dn2CppCont* node);
static void dn2cpp_task_release_action_locked(Dn2CppTask* task);

// Join notifications finish before the outer completion returns. Await
// continuations go to their captured managed context or owner scheduler.
static void dn2cpp_fire_conts(Dn2CppCont* conts)
{
    Dn2CppJoinDrain drain;
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
        if (c->fn == &dn2cpp_post_await)
            Dn2CppJoinDrain::active->posts.append(c);
        // Only a ContinueWith node schedules another Task; await resumptions do not.
        else if (c->fn != &dn2cpp_cont_with_run || dn2cpp_cont_with_prepare(c))
        {
            if (c->owner == nullptr)
                Dn2CppJoinDrain::active->joins.append(c);
            else
                dn2cpp_sched_enqueue(c->owner, c);
        }
        c = next;
    }
    drain.run();
}

static void dn2cpp_task_publish_status_locked(Dn2CppTask* t, int32_t status)
{
    // The owner can be running managed code when its deadline passes. Record
    // which side won at publication, before the waiter can observe the status.
    auto* waiters = dn2cpp_timed_wait_bucket(t);
    if (waiters != nullptr)
    {
        auto now = Dn2CppTaskClock::now();
        for (auto* w = waiters; w != nullptr; w = w->next)
            if (w->task == t)
                w->completedInTime = now <= w->deadline;
    }
    t->status.store(status, std::memory_order_release);
    for (auto* w = waiters; w != nullptr; w = w->next)
    {
        if (w->task != t)
            continue;
        std::lock_guard<std::mutex> lock(w->scheduler->mtx);
        w->scheduler->cv.recheck();
    }
    if (g_task_completion_waiters != 0)
        g_task_completed.notify_all();
    if (t->completionWaitHandle != nullptr)
        dn2cpp_event_set_if_open(t->completionWaitHandle);
}

// Detach a completing task's continuation list under g_task_mtx (so a concurrent
// await-registration sees a settled status), then fire it outside the lock.
static Dn2CppCont* dn2cpp_task_publish_locked(Dn2CppTask* t, int32_t status, uint64_t result,
    Dn2CppObject* exception, bool cacheSynchronous, Dn2CppCancelSource* source, bool releaseAction = false)
{
    if (status == DN2CPP_TASK_CANCELED)
        dn2cpp_gc_store_ref(&t->cancellationSource, source);
    t->result = result;
    dn2cpp_gc_write_barrier(&t->result);
    dn2cpp_gc_store_ref(&t->exception, exception);
    if (cacheSynchronous && t->startKind == DN2CPP_TASK_ORIGIN_ASYNC_UNEXPOSED)
        t->disposed = -1;
    // Settled last, with release: await-registration gates on it under this
    // lock, and the drain reads it — and then result/exception — without it.
    dn2cpp_task_publish_status_locked(t, status);
    if (releaseAction || (t->cold != nullptr && (t->execution & DN2CPP_TASK_INVOKED)))
        dn2cpp_task_release_action_locked(t);
    auto* conts = t->continuations;
    dn2cpp_gc_store_ref(&t->continuations, static_cast<Dn2CppCont*>(nullptr));
    return conts;
}

static void dn2cpp_task_complete(Dn2CppTask* t, int32_t status, uint64_t result, Dn2CppObject* exception,
    bool cacheSynchronous = false, Dn2CppCancelSource* source = nullptr)
{
    Dn2CppCont* conts;
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        if (t->finishProvenance.load(std::memory_order_relaxed) & DN2CPP_TASK_EXPLICIT_FINISH)
            return;
        conts = dn2cpp_task_publish_locked(t, status, result, exception, cacheSynchronous, source);
    }
    dn2cpp_fire_conts(conts);
}

enum class Dn2CppTaskFinishKind { Normal, PromiseCancellation, BeforeDelegate, AfterDelegate, RejectedAfterCompletion };

// A delegate Finish (and a promise TrySetException) drains previously recorded
// faults. Direct promise result and cancellation transitions do not.
static bool dn2cpp_task_finish_recorded(Dn2CppTask* t, int32_t status, uint64_t result,
    Dn2CppObject* exception, Dn2CppCancelSource* source = nullptr, Dn2CppArrayRef* payloads = nullptr,
    bool hasDispatch = false, Dn2CppObject* cancelDispatch = nullptr,
    Dn2CppTaskFinishKind kind = Dn2CppTaskFinishKind::Normal)
{
    // Dispatch payloads require a real holder; plain source exceptions can
    // keep the same compact native aggregate when no holder has been needed.
    Dn2CppObject* nativeAggregate = payloads == nullptr || hasDispatch
        ? nullptr : dn2cpp_aggregate_exception_new(payloads);
    std::unique_lock<std::mutex> lock(g_task_mtx);
    bool queueRejection = kind == Dn2CppTaskFinishKind::RejectedAfterCompletion;
    if ((!queueRejection && t->status != DN2CPP_TASK_PENDING) || t->exceptionHolderState == DN2CPP_TASK_HOLDER_RESERVED)
        return false;
    if (queueRejection && t->status == DN2CPP_TASK_CANCELED)
        t->execution.fetch_or(DN2CPP_TASK_CANCELED_BEFORE_REJECTION);
    bool explicitFinish = kind == Dn2CppTaskFinishKind::BeforeDelegate || kind == Dn2CppTaskFinishKind::AfterDelegate;
    if (explicitFinish)
    {
        uint8_t provenance = t->finishProvenance.load(std::memory_order_acquire);
        int32_t origin = t->startKind.load(std::memory_order_relaxed);
        const char* unsupported = nullptr;
        if (provenance & DN2CPP_TASK_HAS_UNTRACKED_PARENT)
            unsupported = "Task.Finish requires notification of its attached parent";
        else if (provenance & DN2CPP_TASK_HAS_UNTRACKED_CHILD)
            unsupported = kind == Dn2CppTaskFinishKind::AfterDelegate
                ? "Task.Finish requires the attached-child countdown and fault collection"
                : "Task.Finish before delegate execution cannot collect faults from previously attached children";
        else if (origin == DN2CPP_TASK_ORIGIN_POOL || origin == DN2CPP_TASK_ORIGIN_UNOWNED_QUEUE
            || origin == DN2CPP_TASK_ORIGIN_ACTIVE_CONTINUATION
            || (origin == DN2CPP_TASK_ORIGIN_COLD && (t->cold == nullptr || (t->execution & DN2CPP_TASK_INVOKED))))
            unsupported = "Task.Finish cannot take completion ownership from dispatched native work";
        if (unsupported != nullptr)
        {
            lock.unlock();
            dn2cpp_throw_platform_not_supported(unsupported);
        }
        t->finishProvenance.fetch_or(DN2CPP_TASK_EXPLICIT_FINISH, std::memory_order_relaxed);
        result = t->result;
    }
    auto* holder = t->exceptionHolder.load(std::memory_order_relaxed);
    if (holder == nullptr && !hasDispatch && cancelDispatch == nullptr)
    {
        if (nativeAggregate != nullptr)
        {
            dn2cpp_gc_store_ref(&t->exceptionAggregate, nativeAggregate);
            exception = payloads->length == 0 ? nullptr : payloads->data[0];
        }
        auto* conts = dn2cpp_task_publish_locked(t, status, result, exception, false, source, explicitFinish);
        lock.unlock();
        dn2cpp_fire_conts(conts);
        return true;
    }
    // Reserve before inserting the terminal fault; a losing TrySetException
    // must not append to the winner's holder. Do not publish completion yet.
    t->exceptionHolderState = DN2CPP_TASK_HOLDER_RESERVED;
    lock.unlock();
    if (holder == nullptr)
    {
        auto* candidate = g_task_holder_create(t);
        {
            std::lock_guard<std::mutex> publish(g_task_mtx);
            holder = t->exceptionHolder.load(std::memory_order_relaxed);
            if (holder == nullptr)
            {
                holder = candidate;
                dn2cpp_gc_store_ref(&t->exceptionHolder, holder);
            }
        }
        if (holder != candidate)
            g_task_holder_observe(candidate);
    }
    Dn2CppCont* conts;
    {
        Dn2CppMonitorGuard guard(holder);
        if (status == DN2CPP_TASK_FAULTED)
        {
            if (payloads == nullptr)
            {
                if (exception != nullptr)
                    g_task_holder_add(holder, exception, false);
            }
            else
            {
                // A collection may include an uninitialized EDI slot. Retain
                // the collection shape instead of invoking Add(null) per slot.
                g_task_holder_add(holder, reinterpret_cast<Dn2CppObject*>(
                    g_task_holder_normalize_dispatch(payloads)), false);
            }
        }
        else if (cancelDispatch != nullptr)
            g_task_holder_add(holder, cancelDispatch, true);
        else if (status == DN2CPP_TASK_CANCELED && exception != nullptr)
            g_task_holder_add(holder, exception, true);
        auto* recorded = kind == Dn2CppTaskFinishKind::PromiseCancellation ? nullptr : g_task_holder_snapshot(holder);
        if (recorded != nullptr)
        {
            // A cancellation acknowledgement cannot displace recorded faults.
            status = DN2CPP_TASK_FAULTED;
            exception = recorded->length == 0 ? nullptr : recorded->data[0];
        }
        else if (status == DN2CPP_TASK_CANCELED && (exception == nullptr || cancelDispatch != nullptr))
            exception = g_task_holder_cancellation(holder);
        std::lock_guard<std::mutex> publish(g_task_mtx);
        if (recorded == nullptr && nativeAggregate != nullptr)
            dn2cpp_gc_store_ref(&t->exceptionAggregate, nativeAggregate);
        t->exceptionHolderState = recorded == nullptr ? DN2CPP_TASK_HOLDER_UNPUBLISHED
            : DN2CPP_TASK_HOLDER_OWNS_FAULTS;
        conts = dn2cpp_task_publish_locked(t, status, result, exception, false, source, explicitFinish);
    }
    dn2cpp_fire_conts(conts);
    return true;
}

bool dn2cpp_task_finish_canceled_promise(Dn2CppTask* task, uint64_t result,
    Dn2CppCancelSource* source, Dn2CppObject* exception, Dn2CppObject* dispatch)
{
    // TrySetCanceled records the winning cancellation payload, but does not
    // run delegate Finish arbitration against previously recorded faults.
    return dn2cpp_task_finish_recorded(task, DN2CPP_TASK_CANCELED, result,
        exception, source, nullptr, false, dispatch, Dn2CppTaskFinishKind::PromiseCancellation);
}

void dn2cpp_task_finish(Dn2CppTask* task, int32_t userDelegateExecute)
{
    dn2cpp_null_check(task);
    // A recorded cancellation EDI (or a requested token) is not an acknowledgement.
    // The delegate exception path supplies Canceled only after matching the token.
    auto kind = userDelegateExecute ? Dn2CppTaskFinishKind::AfterDelegate : Dn2CppTaskFinishKind::BeforeDelegate;
    if (!dn2cpp_task_finish_recorded(task, DN2CPP_TASK_SUCCEEDED, 0, nullptr, nullptr, nullptr, false, nullptr, kind))
        dn2cpp_throw_platform_not_supported("Task.Finish requires one pending completion owner");
}

void dn2cpp_task_set_result(Dn2CppTask* t, uint64_t result)
{
    dn2cpp_task_complete(t, DN2CPP_TASK_SUCCEEDED, result, t->exception);
}

void dn2cpp_async_task_set_result(Dn2CppTask* t, uint64_t result, bool cacheResult)
{
    dn2cpp_task_complete(t, DN2CPP_TASK_SUCCEEDED, result, t->exception, cacheResult);
}

void dn2cpp_task_set_exception(Dn2CppTask* t, Dn2CppObject* exception)
{
    dn2cpp_task_complete(t, DN2CPP_TASK_FAULTED, t->result, exception);
}

// Link a pending registration, or return its already-ready node for dispatch
// outside g_task_mtx.
static Dn2CppCont* dn2cpp_task_register_cont(Dn2CppTask* t, void (*fn)(void*), void* state,
                                             bool synchronous, bool needsOwner)
{
    // Outside g_task_mtx: arming runs the source's own code and may settle `t`.
    if (t->vtsBridge != nullptr)
        dn2cpp_vts_arm(t);
    // Allocate before locking, including when completion already won.
    auto* c = static_cast<Dn2CppCont*>(dn2cpp_alloc(sizeof(Dn2CppCont)));
    c->fn = fn;
    c->state = state;
    dn2cpp_gc_write_barrier(&c->state);
    c->owner = synchronous ? nullptr : dn2cpp_sched_self();
    c->capturedContext = needsOwner;
    c->wallTimers = t_wall_timers;
    std::lock_guard<std::mutex> lk(g_task_mtx);
    if (t->status != DN2CPP_TASK_PENDING)
        return c;
    dn2cpp_gc_store_ref(&c->next, t->continuations);
    dn2cpp_gc_store_ref(&t->continuations, c);
    return nullptr;
}

static void dn2cpp_task_queue_cont(Dn2CppTask* t, void (*fn)(void*), void* state,
    bool needsOwner, bool poolIfReady = false)
{
    if (auto* ready = dn2cpp_task_register_cont(t, fn, state, false, needsOwner))
    {
        // A ready forced await cannot rely on an ordinary owner's pump.
        // Pool workers can drain their own queue while unwrapping Task.Run.
        if (poolIfReady && !needsOwner && !ready->owner->pool_worker && dn2cpp_sched_can_dispatch_pool())
            dn2cpp_sched_queue_pool_cont(ready);
        else
            dn2cpp_fire_conts(ready);
    }
}

void dn2cpp_task_on_completed(Dn2CppTask* t, void (*fn)(void*), void* state,
    bool captureContext, bool postToContext, bool poolIfReady)
{
    if (captureContext && postToContext)
    {
        if (auto* work = dn2cpp_capture_posted_await(fn, state))
        {
            if (auto* ready = dn2cpp_task_register_cont(t, &dn2cpp_post_await, work, true, false))
                dn2cpp_fire_conts(ready);
            return;
        }
    }
    dn2cpp_task_queue_cont(t, fn, state, captureContext && dn2cpp_has_custom_sync_context(), poolIfReady);
}

// .NET's builders hand a throwing continuation registration to Task.ThrowAsync, so the
// suspended method's own handlers never see it and the process ends as for an unhandled
// ThreadPool exception.
void dn2cpp_async_await_rejected(Dn2CppObject* exc)
{
    dn2cpp_task_throw_async(exc, nullptr); // the queued item roots the exception
    dn2cpp_exc_inflight_pop(exc);
}

// A source-backed task may run its source's OnCompleted while registering.
// A rejected registration registers nothing, so the method stays suspended.
void dn2cpp_async_await_on_completed(Dn2CppTask* t, void (*fn)(void*), void* state,
    bool captureContext, bool postToContext, bool poolIfReady)
{
    if (t->vtsBridge != nullptr)
    {
        try
        {
            dn2cpp_vts_arm(t);
        }
        catch (const Dn2CppException& e)
        {
            dn2cpp_async_await_rejected(e.obj);
            return;
        }
    }
    dn2cpp_task_on_completed(t, fn, state, captureContext, postToContext, poolIfReady);
}

// Internal join notifications run outside g_task_mtx, both for settled inputs and
// when a pending input settles. The completion drain bounds nested callback depth.
// Ordinary await and ContinueWith registrations retain their queued contract.
static void dn2cpp_task_on_completed_sync(Dn2CppTask* t, void (*fn)(void*), void* state)
{
    if (dn2cpp_task_register_cont(t, fn, state, true, false) != nullptr)
        dn2cpp_task_invoke_as(nullptr, [&] { fn(state); });
}

// A detached continuation may already be running; removing only a still-linked
// node leaves that callback's immutable state intact for its losing completion.
static void dn2cpp_task_remove_continuation(Dn2CppTask* task, void (*fn)(void*), void* state)
{
    std::lock_guard<std::mutex> lk(g_task_mtx);
    for (Dn2CppCont** p = &task->continuations; *p != nullptr; p = &(*p)->next)
    {
        Dn2CppCont* current = *p;
        if (current->fn == fn && current->state == state)
        {
            dn2cpp_gc_store_ref(p, current->next);
            dn2cpp_gc_store_ref(&current->next, static_cast<Dn2CppCont*>(nullptr));
            return;
        }
    }
}

// Invoke a no-arg System.Action delegate and its multicast chain. The continuation
// a custom suspending awaiter receives is an Action wrapping a boxed state machine's
// MoveNext (or any user Action); the uniform delegate layout's {target, method, prev}
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

void dn2cpp_configured_task_on_completed(const Dn2CppConfiguredTaskAwaiter* awaiter,
    Dn2CppObject* continuation)
{
    if (continuation == nullptr)
        dn2cpp_throw_argument_null_param("continuation");
    dn2cpp_task_on_completed(dn2cpp_null_check(awaiter->task), &dn2cpp_action_continuation,
        continuation, awaiter->captures_context(), true, awaiter->forces_yield());
}

// Register a System.Action to run when the task completes — the runtime side of a
// user awaiter's TaskAwaiter.OnCompleted(Action) and of the Action-over-MoveNext
// continuation a custom suspending awaitable's await synthesizes.
void dn2cpp_task_on_completed_action(Dn2CppTask* t, Dn2CppObject* action, bool captureContext)
{
    dn2cpp_task_on_completed(t, &dn2cpp_action_continuation, action, captureContext);
}

// Action form of dn2cpp_sched_post — the delegate-continuation counterpart of
// awaiting Task.Yield(): run the Action on this thread's next scheduler pass.
void dn2cpp_sched_post_action(Dn2CppObject* action)
{
    dn2cpp_sched_post(&dn2cpp_action_continuation, action);
}

// Untimed drains can jump the owner's logical clock after its run queue empties.
// Finite waits need the immutable wall deadline even when the owner is parked.
struct Dn2CppTimer
{
    Dn2CppTask* task;
    Dn2CppScheduler* owner;
    int64_t due;        // virtual time (ms) at which the delay completes
    Dn2CppTaskClock::time_point wall_due; // a finite wait can begin long after registration
    bool wall_only;
    uint64_t seq;       // registration order for equal wall deadlines across schedulers
    Dn2CppTimer* next;
    // Global static-rooted list links (see g_pending_timers): the timer list hangs
    // off thread-local storage, so a pending entry — often the only path to its
    // delay task and any state machine awaiting it — must also be reachable
    // through scanned memory until it fires.
    Dn2CppTimer* gcprev;
    Dn2CppTimer* gcnext;

    bool precedes(const Dn2CppTimer* other) const
    {
        return wall_due != other->wall_due ? wall_due < other->wall_due : seq < other->seq;
    }

    int64_t due_at(const Dn2CppSchedulerClock* clock, Dn2CppTaskClock::time_point now) const
    {
        if (wall_only)
        {
            if (now < wall_due)
                return INT64_MAX;
            if (clock != nullptr)
                return clock->at(wall_due);
        }
        return clock == nullptr ? due : std::min(due, clock->at(wall_due));
    }
};

static std::atomic<uint64_t> g_timer_sequence{0};
static std::atomic<uint64_t> g_timer_observer_generation{0};

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

// Claimed timers stay on the GC root list until publication. A wall-clock
// observer and the owner can append batches concurrently, but the same thread
// publishes them in claim order, including their native join notifications.
static void dn2cpp_sched_queue_timer_locked(Dn2CppScheduler* s, Dn2CppTimer* e)
{
    dn2cpp_gc_store_ref(&e->next, static_cast<Dn2CppTimer*>(nullptr));
    if (s->ready_timers_tail != nullptr)
        dn2cpp_gc_store_ref(&s->ready_timers_tail->next, e);
    else
        s->ready_timers = e;
    s->ready_timers_tail = e;
}

template<class Predicate, class Claim>
static void dn2cpp_sched_claim_timers_locked(Dn2CppScheduler* s, Predicate due, Claim claim)
{
    Dn2CppTimer* previous = nullptr;
    for (auto* e = s->timers; e != nullptr;)
    {
        auto* next = e->next;
        if (due(e))
        {
            if (previous != nullptr)
                dn2cpp_gc_store_ref(&previous->next, next);
            else
                s->timers = next;
            claim(e);
        }
        else
            previous = e;
        e = next;
    }
    s->timers_tail = previous;
}

static void dn2cpp_sched_fire_timer(Dn2CppTimer* timer)
{
    struct ReleaseTimer
    {
        Dn2CppTimer* timer;
        ~ReleaseTimer()
        {
            dn2cpp_gc_store_ref(&timer->task->delayTimer, static_cast<Dn2CppTimer*>(nullptr));
            dn2cpp_pending_timer_unlink(timer);
        }
    } release{timer};
    dn2cpp_task_try_set_result(timer->task, 0);
}

// A finite observation can span the caller and a delay's owner. Reserve both
// publication queues together, but release both locks around each completion.
struct Dn2CppTimerQueues
{
    Dn2CppScheduler* owners[2];
    int count;
    std::unique_lock<std::mutex> locks[2];
    bool claimed[2]{};
    bool foreign = false;
    Dn2CppTimerQueues* previous = nullptr;
    static thread_local Dn2CppTimerQueues* active;

    explicit Dn2CppTimerQueues(Dn2CppScheduler* first, Dn2CppScheduler* second = nullptr)
        : owners{first, second}, count(second != nullptr && first != second ? 2 : 1)
    {
        for (int i = 0; i < count; ++i)
            locks[i] = std::unique_lock<std::mutex>(owners[i]->mtx, std::defer_lock);
        lock();
    }

    void lock()
    {
        if (count == 2)
            std::lock(locks[0], locks[1]);
        else
            locks[0].lock();
    }

    void unlock()
    {
        for (int i = 0; i < count; ++i)
            locks[i].unlock();
    }

    bool busy(bool notify) const
    {
        for (int i = 0; i < count; ++i)
        {
            auto* s = owners[i];
            if (s->timer_publisher != std::thread::id{} && s->timer_publisher != std::this_thread::get_id())
            {
                if (notify)
                    s->notify_timer_observers = true;
                return true;
            }
        }
        return false;
    }

    Dn2CppTimer* earliest_ready() const
    {
        auto* first = owners[0]->ready_timers;
        auto* second = count == 2 ? owners[1]->ready_timers : nullptr;
        return second != nullptr && (first == nullptr || second->precedes(first)) ? second : first;
    }

    bool publish()
    {
        auto* e = earliest_ready();
        if (e == nullptr || busy(false))
            return false;
        previous = active;
        active = this;
        for (int i = 0; i < count; ++i)
        {
            auto* s = owners[i];
            claimed[i] = s->timer_publisher == std::thread::id{};
            s->timer_publisher = std::this_thread::get_id();
            if (claimed[i] && s != t_sched_ptr)
                foreign = true;
        }
        // A foreign publisher can hold an indefinite owner's last settler.
        if (foreign)
            g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
        do
        {
            auto* s = e->owner;
            s->ready_timers = e->next;
            if (s->ready_timers == nullptr)
                s->ready_timers_tail = nullptr;
            dn2cpp_gc_store_ref(&e->next, static_cast<Dn2CppTimer*>(nullptr));
            unlock();
            dn2cpp_sched_fire_timer(e);
            lock();
            e = earliest_ready();
        } while (e != nullptr);
        return true;
    }

    ~Dn2CppTimerQueues()
    {
        if (active != this)
            return;
        active = previous;
        if (!locks[0].owns_lock())
            lock();
        bool notify = false;
        for (int i = 0; i < count; ++i)
        {
            if (!claimed[i])
                continue;
            auto* s = owners[i];
            s->timer_publisher = {};
            notify |= s->notify_timer_observers;
            s->notify_timer_observers = false;
            // Restore unprocessed successors if a native notification threw.
            if (s->ready_timers != nullptr)
            {
                dn2cpp_gc_store_ref(&s->ready_timers_tail->next, s->timers);
                if (s->timers == nullptr)
                    s->timers_tail = s->ready_timers_tail;
                s->timers = s->ready_timers;
                s->ready_timers = s->ready_timers_tail = nullptr;
            }
        }
        if (notify)
            g_timer_observer_generation.fetch_add(1, std::memory_order_release);
        unlock();
        if (foreign)
        {
            dn2cpp_pool_wake_for_scheduler();
            dn2cpp_principal_left(g_inflight_async_tasks);
        }
        if (notify)
            dn2cpp_sched_wake_all();
    }
};

thread_local Dn2CppTimerQueues* Dn2CppTimerQueues::active = nullptr;

// A reentrant timer observation finishes already claimed work in its original
// order before claiming timers from a different pair of schedulers.
static bool dn2cpp_sched_resume_timers()
{
    auto* active = Dn2CppTimerQueues::active;
    if (active == nullptr)
        return false;
    Dn2CppTimerQueues queues(active->owners[0], active->owners[1]);
    return queues.publish();
}

// Advance virtual time to the earliest pending timer and complete every timer due at
// that instant (in insertion order), posting their continuations. Returns false when
// no timers remain or the earliest due lies beyond `limit` (a blocking drain passes
// INT64_MAX and keeps the classic jump-to-next-timer virtual clock; the frame pump
// passes "virtual_now + real elapsed ms" so a delay completes only once its real
// time has passed). Called only when this thread's run queue has drained.
static bool dn2cpp_sched_advance_timers(int64_t limit, const Dn2CppSchedulerClock* clock = nullptr,
                                       Dn2CppTaskClock::time_point* wake = nullptr)
{
    if (dn2cpp_sched_resume_timers())
        return true;
    Dn2CppScheduler* s = dn2cpp_sched_self();
    {
        Dn2CppTimerQueues queues(s);
        if (s->timers == nullptr)
            return false;
        auto now = Dn2CppTaskClock::now();
        int64_t minDue = INT64_MAX;
        for (Dn2CppTimer* e = s->timers; e != nullptr; e = e->next)
        {
            int64_t due = e->due_at(clock, now);
            if (due == INT64_MAX && wake != nullptr)
                *wake = std::min(*wake, e->wall_due);
            minDue = std::min(minDue, due);
        }
        if (minDue == INT64_MAX || minDue > limit)
            return false;
        s->virtual_now = std::max(s->virtual_now, minDue);
        if (s->clock != nullptr && clock == nullptr)
        {
            // An unbounded drain inside a real-time pump may jump ahead.
            if (minDue > s->clock->at(now))
                *s->clock = {now, minDue};
        }
        dn2cpp_sched_claim_timers_locked(s, [&](Dn2CppTimer* e) { return e->due_at(clock, now) == minDue; },
            [&](Dn2CppTimer* e) { dn2cpp_sched_queue_timer_locked(s, e); });
        queues.publish();
    }
    return true;
}

// Preparation precedes publication, so a timed wait's cleanup can safely inspect
// its immutable timer handle even if it completes while registrations are armed.
static Dn2CppTimer* dn2cpp_task_timer_new(Dn2CppTask* task, int64_t ms)
{
    Dn2CppScheduler* s = dn2cpp_sched_self();
    auto* e = static_cast<Dn2CppTimer*>(dn2cpp_alloc(sizeof(Dn2CppTimer)));
    dn2cpp_gc_store_ref(&e->task, task);
    e->owner = s;
    auto now = Dn2CppTaskClock::now();
    e->wall_due = now + std::chrono::milliseconds(ms);
    e->wall_only = t_wall_timers;
    e->due = (s->clock != nullptr ? s->clock->at(now) : s->virtual_now) + ms;
    e->seq = g_timer_sequence.fetch_add(1, std::memory_order_relaxed);
    return e;
}

static void dn2cpp_task_timer_schedule(Dn2CppTimer* e)
{
    dn2cpp_pending_timer_link(e);
    Dn2CppScheduler* s = e->owner;
    std::lock_guard<std::mutex> lk(s->mtx);
    if (s->timers_tail != nullptr)
        dn2cpp_gc_store_ref(&s->timers_tail->next, e);
    else
        s->timers = e;
    s->timers_tail = e;
}

static void dn2cpp_task_timer_cancel(Dn2CppTimer* timer)
{
    if (timer == nullptr)
        return;
    Dn2CppScheduler* s = timer->owner;
    bool removed = false;
    {
        std::lock_guard<std::mutex> lk(s->mtx);
        Dn2CppTimer* previous = nullptr;
        for (Dn2CppTimer** p = &s->timers; *p != nullptr; p = &(*p)->next)
        {
            if (*p == timer)
            {
                if (previous != nullptr)
                    dn2cpp_gc_store_ref(&previous->next, timer->next);
                else
                    s->timers = timer->next;
                if (s->timers_tail == timer)
                    s->timers_tail = previous;
                dn2cpp_gc_store_ref(&timer->next, static_cast<Dn2CppTimer*>(nullptr));
                removed = true;
                s->cv.recheck();
                break;
            }
            previous = *p;
        }
    }
    if (removed)
    {
        // Never nest the scheduler and global-root locks. A timer already
        // detached for firing is unrooted by its owner instead.
        dn2cpp_pending_timer_unlink(timer);
        dn2cpp_gc_store_ref(&timer->task->delayTimer, static_cast<Dn2CppTimer*>(nullptr));
        dn2cpp_pool_wake_for_scheduler();
    }
}

int64_t dn2cpp_task_delay_ms(int32_t ms)
{
    if (ms < -1)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_TASK_DELAY_MS, "millisecondsDelay");
    return ms;
}

int64_t dn2cpp_task_delay_ms_from_ticks(int64_t ticks)
{
    int64_t ms = static_cast<int64_t>(static_cast<double>(ticks) / 10000.0);
    if (ms < -1 || ms > INT64_C(0xFFFFFFFE))
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_TASK_DELAY_SPAN, "delay");
    return ms;
}

Dn2CppTask* dn2cpp_task_delay(int64_t ms)
{
    if (ms == 0)
        return dn2cpp_task_completed();
    Dn2CppTask* t = dn2cpp_task_alloc();
    if (ms < 0)
        return t;
    auto* timer = dn2cpp_task_timer_new(t, ms);
    dn2cpp_gc_store_ref(&t->delayTimer, timer);
    dn2cpp_task_timer_schedule(timer);
    return t;
}

// Task.WhenAll join state: the result task, the input tasks, a remaining-count,
// and how to materialize the result array.
struct Dn2CppWhenAllState
{
    Dn2CppTask* result;
    Dn2CppArrayRef* tasks;
    std::atomic<int32_t> remaining;
    std::atomic<Dn2CppTask*> firstCanceled; // non-generic joins select completion order
    int32_t kind;
    int32_t elemSize;   // DN2CPP_WHENALL_N / STRUCT: byte size of each result element
    // The TResult[] handle the emit arm supplied. The array is materialized inside
    // the completion callback, long after the lowering has returned, so a retag at
    // the call site is impossible and the handle has to RIDE here — without
    // it `(await Task.WhenAll(ts)).GetType()` reads System.Object[] for a reference
    // TResult, and for a value TResult a packed value array carries a REF-array tag,
    // which breaks covariance and IEnumerable<T> dispatch and not just the name.
    // Null degrades to the shared handle, which is what this did before.
    const Dn2CppTypeInfo* arrTi;
};

static void dn2cpp_task_set_canceled(Dn2CppTask* t, Dn2CppCancelSource* source = nullptr);

// A plain terminal fault is immutable; AddException may append to the
// holder of an already faulted task. Snapshot once per observation so two
// passes cannot count one set and copy a different one.
static Dn2CppArrayRef* dn2cpp_task_fault_snapshot(Dn2CppTask* t)
{
    if (t->status == DN2CPP_TASK_CANCELED)
    {
        Dn2CppArrayRef* recorded = nullptr;
        if (auto* holder = t->exceptionHolder.load(std::memory_order_acquire))
        {
            Dn2CppMonitorGuard guard(holder);
            recorded = g_task_holder_snapshot(holder);
            if (recorded != nullptr)
                g_task_holder_observe(holder);
        }
        int32_t count = recorded == nullptr ? 0 : recorded->length;
        auto* inner = dn2cpp_newarr_ref(count + 1);
        for (int32_t i = 0; i < count; ++i)
        {
            auto* exception = recorded->data[i];
            dn2cpp_null_check(exception);
            dn2cpp_gc_store_ref(&inner->data[i], exception);
        }
        dn2cpp_gc_store_ref(&inner->data[count], dn2cpp_make_task_canceled_exception(t));
        return inner;
    }
    auto* base = t->exceptionAggregate.load(std::memory_order_acquire);
    auto* baseInners = base == nullptr ? nullptr : dn2cpp_aggregate_inner_exceptions(base, nullptr);
    Dn2CppArrayRef* recorded = nullptr;
    bool ownsFaults = false;
    if (auto* holder = dn2cpp_task_holder_for_observation(t))
    {
        Dn2CppMonitorGuard guard(holder);
        recorded = g_task_holder_snapshot(holder);
        g_task_holder_observe(holder);
        // The state of a holder installed after terminal publication is only
        // visible after its own acquire, not the earlier status acquire.
        ownsFaults = t->exceptionHolderState == DN2CPP_TASK_HOLDER_OWNS_FAULTS;
    }
    // A malformed private EDI collection can contain a null entry. It did
    // not prevent Finish; the observation that dereferences it fails now.
    if (recorded != nullptr)
        for (int32_t i = 0; i < recorded->length; ++i)
            if (recorded->data[i] == nullptr)
                dn2cpp_throw_null_reference();
    if (ownsFaults)
        return recorded;
    int32_t baseCount = baseInners == nullptr ? 1 : baseInners->length;
    int32_t tail = recorded == nullptr ? 0 : recorded->length;
    if (tail == 0 && baseInners != nullptr)
        return baseInners;
    auto* inner = dn2cpp_newarr_ref(baseCount + tail);
    for (int32_t i = 0; i < baseCount; ++i)
        dn2cpp_gc_store_ref(&inner->data[i], baseInners == nullptr ? t->exception : baseInners->data[i]);
    for (int32_t i = 0; i < tail; ++i)
        dn2cpp_gc_store_ref(&inner->data[baseCount + i], recorded->data[i]);
    return inner;
}

static void dn2cpp_task_collect_fault(Dn2CppRefList*& faults, Dn2CppTask* t)
{
    if (faults == nullptr)
        faults = dn2cpp_reflist_new();
    // Normal single faults need no intermediate array.
    Dn2CppArrayRef* snapshot;
    if (t->status == DN2CPP_TASK_CANCELED || dn2cpp_task_holder_for_observation(t) != nullptr)
        snapshot = dn2cpp_task_fault_snapshot(t);
    else if (auto* aggregate = t->exceptionAggregate.load(std::memory_order_acquire))
        snapshot = dn2cpp_aggregate_inner_exceptions(aggregate, nullptr);
    else
    {
        dn2cpp_reflist_add(faults, t->exception);
        return;
    }
    for (int32_t i = 0; i < snapshot->length; ++i)
        dn2cpp_reflist_add(faults, snapshot->data[i]);
}

struct Dn2CppForwardedFaults
{
    Dn2CppRefList* payloads = nullptr;
    bool hasDispatch = false;

    void append(Dn2CppArrayRef* array)
    {
        for (int32_t i = 0; i < array->length; ++i)
            dn2cpp_reflist_add(payloads, array->data[i]);
    }

    void collect(Dn2CppTask* task)
    {
        if (payloads == nullptr)
            payloads = dn2cpp_reflist_new();
        Dn2CppArrayRef* dispatch = nullptr;
        bool ownsFaults = false;
        if (auto* holder = dn2cpp_task_holder_for_observation(task))
        {
            Dn2CppMonitorGuard guard(holder);
            if (g_task_holder_dispatch != nullptr)
                dispatch = g_task_holder_dispatch(holder);
            ownsFaults = task->exceptionHolderState == DN2CPP_TASK_HOLDER_OWNS_FAULTS;
            g_task_holder_observe(holder);
        }
        if (!ownsFaults)
        {
            if (auto* aggregate = task->exceptionAggregate.load(std::memory_order_acquire))
                append(dn2cpp_aggregate_inner_exceptions(aggregate, nullptr));
            else
                dn2cpp_reflist_add(payloads, task->exception);
        }
        if (dispatch != nullptr)
        {
            hasDispatch = true;
            append(dispatch);
        }
    }

    bool finish(Dn2CppTask* task, uint64_t result = 0)
    {
        return dn2cpp_task_finish_recorded(task, DN2CPP_TASK_FAULTED, result, nullptr, nullptr,
            dn2cpp_reflist_to_array(payloads), hasDispatch);
    }
};

static Dn2CppObject* dn2cpp_task_cancellation_dispatch(Dn2CppTask* task)
{
    if (auto* holder = task->exceptionHolder.load(std::memory_order_acquire))
    {
        Dn2CppMonitorGuard guard(holder);
        if (g_task_holder_cancel_dispatch != nullptr)
            return g_task_holder_cancel_dispatch(holder);
    }
    return nullptr;
}

// Build the TResult[] from each input task's result slot and complete the WhenAll
// task — or propagate the outcome of an input that did not succeed.
static void dn2cpp_when_all_finish(Dn2CppWhenAllState* s)
{
    // A fault ANYWHERE outranks a cancellation anywhere: .NET's WhenAll is Faulted if
    // any input faulted, Canceled if none did and one was canceled. Without the second
    // arm a canceled input reads as RanToCompletion and its awaiter gets a result.
    // Every faulted input contributes its observed inners; cancellation contributes
    // nothing to a faulted WhenAll.
    int32_t n = s->tasks->length;
    Dn2CppTask* canceled = s->kind == DN2CPP_WHENALL_VOID
        ? s->firstCanceled.load(std::memory_order_acquire) : nullptr;
    Dn2CppForwardedFaults faults;
    for (int32_t i = 0; i < n; i++)
    {
        auto* t = reinterpret_cast<Dn2CppTask*>(s->tasks->data[i]);
        if (t->status == DN2CPP_TASK_FAULTED)
            faults.collect(t);
        else if (canceled == nullptr && t->status == DN2CPP_TASK_CANCELED)
            canceled = t;
    }
    if (faults.payloads != nullptr)
    {
        faults.finish(s->result);
        return;
    }
    if (canceled != nullptr)
    {
        auto* source = canceled->cancellationSource.load(std::memory_order_acquire);
        dn2cpp_task_finish_canceled_promise(s->result, 0, source, canceled->exception,
            dn2cpp_task_cancellation_dispatch(canceled));
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
            // The zero slot is default(T) when no delegate supplied a result.
            if (boxed != nullptr)
                dn2cpp_gc_memmove_refs(
                    static_cast<char*>(a->data) + static_cast<size_t>(i) * s->elemSize,
                    boxed, static_cast<size_t>(s->elemSize));
        }
        arr = reinterpret_cast<Dn2CppObject*>(a);
    }
    else // DN2CPP_WHENALL_N: scalar bits packed at the result type's storage width
    {
        if (s->elemSize != 1 && s->elemSize != 2 && s->elemSize != 8)
            dn2cpp_fail("WhenAll scalar result has invalid element size");
        Dn2CppArrayN* a = dn2cpp_newarr_n_t(n, s->elemSize, s->arrTi);
        for (int32_t i = 0; i < n; i++)
        {
            uint64_t bits = reinterpret_cast<Dn2CppTask*>(s->tasks->data[i])->result;
            if (s->elemSize == 1)
                reinterpret_cast<uint8_t*>(a->data)[i] = static_cast<uint8_t>(bits);
            else if (s->elemSize == 2)
                reinterpret_cast<uint16_t*>(a->data)[i] = static_cast<uint16_t>(bits);
            else
                reinterpret_cast<uint64_t*>(a->data)[i] = bits;
        }
        arr = reinterpret_cast<Dn2CppObject*>(a);
    }
    dn2cpp_task_set_result(s->result, static_cast<uint64_t>(reinterpret_cast<uintptr_t>(arr)));
}

static void dn2cpp_when_all_one(void* p)
{
    auto* s = static_cast<Dn2CppWhenAllState*>(p);
    if (s->remaining.fetch_sub(1, std::memory_order_acq_rel) == 1)
        dn2cpp_when_all_finish(s);
}

// Non-generic WhenAll chooses the first canceled input to notify it. The
// generic join instead scans input order after every input has completed.
struct Dn2CppWhenAllEntry
{
    Dn2CppWhenAllState* join;
    Dn2CppTask* task;
};

static void dn2cpp_when_all_void_one(void* p)
{
    auto* entry = static_cast<Dn2CppWhenAllEntry*>(p);
    if (entry->task->status == DN2CPP_TASK_CANCELED)
    {
        Dn2CppTask* empty = nullptr;
        if (entry->join->firstCanceled.compare_exchange_strong(empty, entry->task,
            std::memory_order_acq_rel))
            dn2cpp_gc_write_barrier(&entry->join->firstCanceled);
    }
    dn2cpp_when_all_one(entry->join);
}

static Dn2CppTask* dn2cpp_task_when_all_impl(Dn2CppArrayRef* tasks, int32_t kind, int32_t elemSize,
                                             const Dn2CppTypeInfo* arrTi)
{
    // Validate before allocating the state, as dn2cpp_task_when_any does: without this
    // a null array and a null element are raw dereferences (a crash where real .NET
    // answers with a catchable throw). .NET gives ArgumentNullException for the array
    // and ArgumentException for an element, both naming `tasks`.
    if (tasks == nullptr)
        dn2cpp_throw_argument_null_param("tasks");
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (tasks->data[i] == nullptr)
            dn2cpp_throw_argument_param(DN2CPP_SR_NULL_TASK, "tasks");
    }
    // A singleton non-generic join preserves the input task's identity and origin.
    if (kind == DN2CPP_WHENALL_VOID && tasks->length == 1)
        return reinterpret_cast<Dn2CppTask*>(tasks->data[0]);
    auto* s = static_cast<Dn2CppWhenAllState*>(dn2cpp_alloc(sizeof(Dn2CppWhenAllState)));
    dn2cpp_gc_store_ref(&s->result, dn2cpp_task_alloc());
    if (tasks->length == 0)
        s->result->startKind = DN2CPP_TASK_ORIGIN_PRECOMPLETED;
    dn2cpp_gc_store_ref(&s->tasks, tasks);
    s->remaining.store(tasks->length, std::memory_order_relaxed);
    s->firstCanceled.store(nullptr, std::memory_order_relaxed);
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
    {
        auto* task = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
        if (kind == DN2CPP_WHENALL_VOID)
        {
            auto* entry = static_cast<Dn2CppWhenAllEntry*>(dn2cpp_alloc(sizeof(Dn2CppWhenAllEntry)));
            dn2cpp_gc_store_ref(&entry->join, s);
            dn2cpp_gc_store_ref(&entry->task, task);
            dn2cpp_task_on_completed_sync(task, &dn2cpp_when_all_void_one, entry);
        }
        else
            dn2cpp_task_on_completed_sync(task, &dn2cpp_when_all_one, s);
    }
    return s->result;
}

Dn2CppTask* dn2cpp_task_when_all(Dn2CppArrayRef* tasks, int32_t kind, const Dn2CppTypeInfo* arrTi,
                                 int32_t elemSize)
{
    return dn2cpp_task_when_all_impl(tasks, kind, elemSize, arrTi);
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
    std::atomic<int32_t> done;
};
struct Dn2CppWhenAnyEntry
{
    Dn2CppWhenAnyState* shared;
    Dn2CppTask* task;
};

static void dn2cpp_when_any_one(void* p)
{
    auto* e = static_cast<Dn2CppWhenAnyEntry*>(p);
    if (e->shared->done.exchange(1, std::memory_order_acq_rel) != 0)
        return;
    dn2cpp_task_set_result(e->shared->result,
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(e->task)));
}

// The argument validation runs BEFORE the state allocation: dn2cpp_task_alloc would
// mint a Task nothing will ever settle, and since the rejection is a catchable throw
// the program keeps running with exactly the shape a later deadlock verdict has to
// reason about. The answers are real .NET's: a null array is ArgumentNullException,
// an empty array or a null element ArgumentException, all naming `tasks` — except
// that .NET forwards exactly two tasks from an array, a span, loose arguments or an
// exact List<TTask> to WhenAny(task1, task2), whose null argument is an
// ArgumentNullException naming `task1` or `task2`. No other sequence reaches here
// holding a null: the emitted enumeration loop rejects it at the first null, as .NET's
// general path does.
Dn2CppTask* dn2cpp_task_when_any(Dn2CppArrayRef* tasks, bool pairSource)
{
    if (tasks == nullptr)
        dn2cpp_throw_argument_null_param("tasks");
    if (tasks->length == 0)
        dn2cpp_throw_argument_param(DN2CPP_SR_EMPTY_TASK_LIST, "tasks");
    // A null element is checked in its own pass ahead of the registration loop:
    // half-registering the continuations and then throwing would leave the surviving
    // entries pointing at a shared state whose result task nothing returns, i.e. a
    // completion callback that fires into an abandoned join. dn2cpp_task_wait_any
    // rejects the same element before it builds anything, for the same reason.
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (tasks->data[i] != nullptr)
            continue;
        if (tasks->length == 2)
            dn2cpp_throw_argument_null_param(i == 0 ? "task1" : "task2");
        dn2cpp_throw_argument_param(DN2CPP_SR_NULL_TASK, "tasks");
    }
    if (pairSource && tasks->length == 2)
    {
        for (int32_t i = 0; i < 2; i++)
        {
            auto* task = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
            if (task->status.load(std::memory_order_acquire) != DN2CPP_TASK_PENDING)
                return dn2cpp_task_from_result(static_cast<uint64_t>(reinterpret_cast<uintptr_t>(task)));
        }
    }
    auto* s = static_cast<Dn2CppWhenAnyState*>(dn2cpp_alloc(sizeof(Dn2CppWhenAnyState)));
    dn2cpp_gc_store_ref(&s->result, dn2cpp_task_alloc());
    s->done.store(0, std::memory_order_relaxed);
    for (int32_t i = 0; i < tasks->length; i++)
    {
        auto* e = static_cast<Dn2CppWhenAnyEntry*>(dn2cpp_alloc(sizeof(Dn2CppWhenAnyEntry)));
        dn2cpp_gc_store_ref(&e->shared, s);
        dn2cpp_gc_store_ref(&e->task, reinterpret_cast<Dn2CppTask*>(tasks->data[i]));
        // Synchronous registration, so a settled input wins the join before this
        // returns; the winner is then the FIRST settled input in array order, as on
        // real .NET. Stop once one has won: a continuation on a still-pending input
        // could only no-op, and would pin the finished join until that input settles.
        dn2cpp_task_on_completed_sync(e->task, &dn2cpp_when_any_one, e);
        if (s->done.load(std::memory_order_acquire) != 0)
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
    dn2cpp_gc_memmove_refs(p, src, static_cast<size_t>(size));
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
    dn2cpp_gc_store_ref(&t->exception, exception);
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
    dn2cpp_ti_with_typeobject({ "System.OperationCanceledException", &dn2cpp_exception_type, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, dn2cpp_default_ctor_reflection }, &dn2cpp_operation_canceled_exception_type_obj);
const Dn2CppType dn2cpp_operation_canceled_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_operation_canceled_exception_type };
static std::atomic<const Dn2CppTypeInfo*> s_canceled_exc_type{
    &dn2cpp_operation_canceled_exception_type };

static std::atomic<size_t> s_canceled_token_offset{SIZE_MAX};

void dn2cpp_set_canceled_exception_type(const Dn2CppTypeInfo* ti, size_t tokenOffset)
{
    if (ti != nullptr)
    {
        if (tokenOffset != SIZE_MAX)
            s_canceled_token_offset.store(tokenOffset, std::memory_order_release);
        s_canceled_exc_type.store(ti, std::memory_order_release);
    }
}

static Dn2CppCancelSource* dn2cpp_canceled_exception_source(Dn2CppObject* exception)
{
    if (exception == nullptr || dn2cpp_isinst(exception,
        s_canceled_exc_type.load(std::memory_order_acquire)) == nullptr)
        return nullptr;
    size_t offset = s_canceled_token_offset.load(std::memory_order_acquire);
    if (offset == SIZE_MAX || offset + sizeof(Dn2CppCancelToken) > static_cast<size_t>(exception->type->instanceSize))
        dn2cpp_throw_platform_not_supported("OperationCanceledException token layout is unavailable");
    Dn2CppCancelToken token;
    std::memcpy(&token, reinterpret_cast<char*>(exception) + offset, sizeof(token));
    return token.source;
}

// The real TaskCanceledException type-info, registered beside the OCE one: a CANCELED
// *task* carries a TaskCanceledException (real .NET's Task.FromCanceled / canceled
// Task.Delay / TCS.SetCanceled all do), while the token-side throw keeps the plain
// OperationCanceledException. Null until registered — the fallback is the OCE identity
// above, so a program whose CoreLib never carried the type still works.
static std::atomic<const Dn2CppTypeInfo*> s_task_canceled_exc_type{ nullptr };
static std::atomic<size_t> s_canceled_task_offset{SIZE_MAX};

void dn2cpp_set_task_canceled_exception_type(const Dn2CppTypeInfo* ti, size_t taskOffset)
{
    if (ti != nullptr)
    {
        if (taskOffset != SIZE_MAX)
            s_canceled_task_offset.store(taskOffset, std::memory_order_release);
        s_task_canceled_exc_type.store(ti, std::memory_order_release);
    }
}

static Dn2CppObject* dn2cpp_make_canceled_exception_of(const Dn2CppTypeInfo* ti,
                                                       const char* msg, Dn2CppCancelSource* source)
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
    if (source != nullptr)
    {
        size_t offset = s_canceled_token_offset.load(std::memory_order_acquire);
        if (offset == SIZE_MAX || offset + sizeof(Dn2CppCancelToken) > sz)
            dn2cpp_throw_platform_not_supported("OperationCanceledException token layout is unavailable");
        auto* slot = reinterpret_cast<char*>(o) + offset;
        Dn2CppCancelToken token{source};
        std::memcpy(slot, &token, sizeof(token));
        dn2cpp_gc_write_barrier(slot);
    }
    return o;
}

// The token-side OCE (ThrowIfCancellationRequested).
static Dn2CppObject* dn2cpp_make_canceled_exception(Dn2CppCancelSource* source = nullptr)
{
    return dn2cpp_make_canceled_exception_of(
        s_canceled_exc_type.load(std::memory_order_acquire), "The operation was canceled.", source);
}

// The task-side TCE, materialized when cancellation is observed.
static Dn2CppObject* dn2cpp_make_task_canceled_exception(Dn2CppTask* task)
{
    auto* source = task->cancellationSource.load(std::memory_order_acquire);
    const Dn2CppTypeInfo* ti = s_task_canceled_exc_type.load(std::memory_order_acquire);
    if (ti == nullptr)
        return dn2cpp_make_canceled_exception(source);
    auto* exception = dn2cpp_make_canceled_exception_of(ti, "A task was canceled.", source);
    size_t offset = s_canceled_task_offset.load(std::memory_order_acquire);
    if (offset != SIZE_MAX)
    {
        if (offset + sizeof(task) > static_cast<size_t>(ti->instanceSize))
            dn2cpp_throw_platform_not_supported("TaskCanceledException task layout is unavailable");
        dn2cpp_gc_store_ref(reinterpret_cast<Dn2CppTask**>(reinterpret_cast<char*>(exception) + offset), task);
    }
    return exception;
}

[[noreturn]] void dn2cpp_throw_canceled(Dn2CppCancelSource* source)
{
    dn2cpp_throw(dn2cpp_make_canceled_exception(source));
}

static void dn2cpp_task_set_canceled(Dn2CppTask* t, Dn2CppCancelSource* source)
{
    dn2cpp_task_complete(t, DN2CPP_TASK_CANCELED, t->result, nullptr, false, source);
}

void dn2cpp_task_set_exception_or_canceled(Dn2CppTask* t, Dn2CppObject* exception)
{
    int32_t status = exception != nullptr
        && dn2cpp_isinst(exception,
            s_canceled_exc_type.load(std::memory_order_acquire)) != nullptr
        ? DN2CPP_TASK_CANCELED : DN2CPP_TASK_FAULTED;
    if (status == DN2CPP_TASK_CANCELED)
        dn2cpp_task_finish_canceled_promise(t, t->result, dn2cpp_canceled_exception_source(exception), exception);
    else
        dn2cpp_task_finish_recorded(t, status, t->result, exception);
}

// Task.FromCanceled(<T>) / ValueTask.FromCanceled(<T>): a pre-completed CANCELED
// task. No continuations to fire (fresh task, like dn2cpp_task_from_exception);
// awaiting creates a TaskCanceledException via dn2cpp_task_throw_if_faulted;
// blocking Wait/.Result creates a separate wrapped exception.
Dn2CppTask* dn2cpp_task_from_canceled(Dn2CppCancelSource* source)
{
    auto* t = dn2cpp_task_alloc(0, source);
    t->startKind = DN2CPP_TASK_ORIGIN_PRECOMPLETED;
    t->status.store(DN2CPP_TASK_CANCELED, std::memory_order_relaxed);
    return t;
}

// ---- TaskCompletionSource(<T>) -----------------------------------------------
// The wrapper's task surface is these exactly-once transitions. Unlike
// dn2cpp_task_complete (whose callers settle a task they exclusively own), a
// TCS may race concurrent TrySet* calls — the PENDING check and the transition
// happen under the same g_task_mtx hold, so exactly one caller wins.
static int32_t dn2cpp_task_try_complete_locked(Dn2CppTask* t, int32_t status, uint64_t result,
    Dn2CppObject* exception, Dn2CppObject* aggregate, Dn2CppCont*& conts,
    Dn2CppCancelSource* source, bool requireUnstarted)
{
    if (t->status != DN2CPP_TASK_PENDING || t->exceptionHolderState == DN2CPP_TASK_HOLDER_RESERVED
        || (requireUnstarted && t->execution != DN2CPP_TASK_NOT_STARTED))
        return 0;
    if (status == DN2CPP_TASK_CANCELED)
        dn2cpp_gc_store_ref(&t->cancellationSource, source);
    t->result = result;
    dn2cpp_gc_write_barrier(&t->result);
    dn2cpp_gc_store_ref(&t->exception, exception);
    dn2cpp_gc_store_ref(&t->exceptionAggregate, aggregate);
    dn2cpp_task_publish_status_locked(t, status);
    conts = t->continuations;
    dn2cpp_gc_store_ref(&t->continuations, static_cast<Dn2CppCont*>(nullptr));
    return 1;
}

static int32_t dn2cpp_task_try_complete_deferred(Dn2CppTask* t, int32_t status, uint64_t result,
    Dn2CppObject* exception, Dn2CppObject* aggregate, Dn2CppCont*& conts,
    Dn2CppCancelSource* source = nullptr, bool requireUnstarted = false)
{
    std::lock_guard<std::mutex> lock(g_task_mtx);
    return dn2cpp_task_try_complete_locked(t, status, result, exception, aggregate, conts, source, requireUnstarted);
}

static int32_t dn2cpp_task_try_complete(Dn2CppTask* t, int32_t status, uint64_t result,
    Dn2CppObject* exception, Dn2CppObject* aggregate = nullptr, Dn2CppCancelSource* source = nullptr)
{
    Dn2CppCont* conts = nullptr;
    int32_t won = dn2cpp_task_try_complete_deferred(t, status, result, exception, aggregate, conts, source);
    if (won)
        dn2cpp_fire_conts(conts);
    return won;
}

static void dn2cpp_task_forward_completion(Dn2CppTask* target, Dn2CppTask* input, uint64_t result,
    bool copyNativeAggregate = false)
{
    if (target->status != DN2CPP_TASK_PENDING)
        return;
    auto status = input->status.load(std::memory_order_acquire);
    if (status == DN2CPP_TASK_FAULTED)
    {
        Dn2CppCont* conts = nullptr;
        bool forwardedNative = false;
        {
            std::lock_guard<std::mutex> lock(g_task_mtx);
            if (target->status != DN2CPP_TASK_PENDING || target->exceptionHolderState == DN2CPP_TASK_HOLDER_RESERVED)
                return;
            if (input->exceptionHolder.load(std::memory_order_relaxed) == nullptr
                && target->exceptionHolder.load(std::memory_order_relaxed) == nullptr)
            {
                // Keep each proxy's existing native-only representation. With
                // a holder at either end, preserve the recorded dispatch set.
                if (g_task_holder_create != nullptr)
                    input->nativeFaultObserved = true;
                auto* aggregate = copyNativeAggregate ? input->exceptionAggregate.load(std::memory_order_acquire) : nullptr;
                forwardedNative = dn2cpp_task_try_complete_locked(target, status, result,
                    input->exception, aggregate, conts, nullptr, false);
            }
        }
        if (forwardedNative)
        {
            dn2cpp_fire_conts(conts);
            return;
        }
        Dn2CppForwardedFaults faults;
        faults.collect(input);
        faults.finish(target, result);
    }
    else if (status == DN2CPP_TASK_CANCELED)
    {
        auto* source = input->cancellationSource.load(std::memory_order_acquire);
        dn2cpp_task_finish_canceled_promise(target, result, source, input->exception,
            dn2cpp_task_cancellation_dispatch(input));
    }
    else
        dn2cpp_task_try_complete(target, status, result, nullptr);
}

void dn2cpp_task_try_set_result_deferred(Dn2CppTask* t, uint64_t result, Dn2CppCont** batch)
{
    Dn2CppCont* conts = nullptr;
    if (!dn2cpp_task_try_complete_deferred(t, DN2CPP_TASK_SUCCEEDED, result, nullptr, nullptr, conts)
        || conts == nullptr)
        return;
    // Each detached list is reversed registration order. Prepend the later task's
    // whole list so the final reversal also preserves completion-batch order.
    Dn2CppCont* tail = conts;
    while (tail->next != nullptr)
        tail = tail->next;
    dn2cpp_gc_store_ref(&tail->next, *batch);
    *batch = conts; // caller-owned stack root, not a GC heap slot
}

void dn2cpp_task_dispatch_deferred(Dn2CppCont* batch)
{
    dn2cpp_fire_conts(batch);
}

int32_t dn2cpp_task_try_set_result(Dn2CppTask* t, uint64_t result)
{
    return dn2cpp_task_try_complete(t, DN2CPP_TASK_SUCCEEDED, result, nullptr);
}

int32_t dn2cpp_task_try_set_exception(Dn2CppTask* t, Dn2CppObject* exception)
{
    return dn2cpp_task_finish_recorded(t, DN2CPP_TASK_FAULTED, 0, exception);
}

int32_t dn2cpp_task_try_set_canceled(Dn2CppTask* t, Dn2CppCancelSource* source)
{
    return dn2cpp_task_try_complete(t, DN2CPP_TASK_CANCELED, 0, nullptr, nullptr, source);
}

static int32_t dn2cpp_task_await_winner(Dn2CppTask* t, int32_t won)
{
    if (won || t->status != DN2CPP_TASK_PENDING)
        return won;
    // A competing finish can reserve the task while capturing managed exceptions.
    std::unique_lock<std::mutex> lock(g_task_mtx);
    ++g_task_completion_waiters;
    g_task_completed.wait(lock, [t] { return t->status != DN2CPP_TASK_PENDING; });
    --g_task_completion_waiters;
    return 0;
}

int32_t dn2cpp_tcs_try_set_result(Dn2CppTask* t, uint64_t result)
{
    return dn2cpp_task_await_winner(t, dn2cpp_task_try_set_result(t, result));
}

int32_t dn2cpp_tcs_try_set_exception(Dn2CppTask* t, Dn2CppObject* exception)
{
    return dn2cpp_task_await_winner(t, dn2cpp_task_try_set_exception(t, exception));
}

int32_t dn2cpp_tcs_try_set_canceled(Dn2CppTask* t, Dn2CppCancelSource* source)
{
    return dn2cpp_task_await_winner(t, dn2cpp_task_try_set_canceled(t, source));
}

int32_t dn2cpp_tcs_try_set_from_task(Dn2CppTaskCompletionSource* source, Dn2CppTask* completed, bool copyResult)
{
    if (completed == nullptr)
        dn2cpp_throw_argument_null_param("completedTask");
    int32_t status = completed->status.load(std::memory_order_acquire);
    if (status == DN2CPP_TASK_PENDING)
        dn2cpp_throw_argument_param(DN2CPP_SR_TASK_MUST_BE_COMPLETED, "completedTask");
    auto* target = dn2cpp_null_check(source)->task;
    int32_t won;
    if (status == DN2CPP_TASK_FAULTED)
    {
        // Getting the dispatch records observes the source even when the destination has already settled.
        Dn2CppForwardedFaults faults;
        faults.collect(completed);
        won = faults.finish(dn2cpp_null_check(target));
    }
    else if (status == DN2CPP_TASK_CANCELED)
    {
        won = dn2cpp_task_finish_canceled_promise(dn2cpp_null_check(target), 0,
            completed->cancellationSource.load(std::memory_order_acquire), completed->exception,
            dn2cpp_task_cancellation_dispatch(completed));
    }
    else
        won = dn2cpp_task_try_set_result(dn2cpp_null_check(target), copyResult ? completed->result : 0);
    return dn2cpp_task_await_winner(target, won);
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
    if (!dn2cpp_tcs_try_set_result(t, result))
        dn2cpp_tcs_throw_settled();
}

void dn2cpp_tcs_set_exception(Dn2CppTask* t, Dn2CppObject* exception)
{
    if (!dn2cpp_tcs_try_set_exception(t, exception))
        dn2cpp_tcs_throw_settled();
}

void dn2cpp_tcs_set_canceled(Dn2CppTask* t, Dn2CppCancelSource* source)
{
    if (!dn2cpp_tcs_try_set_canceled(t, source))
        dn2cpp_tcs_throw_settled();
}

void dn2cpp_tcs_set_from_task(Dn2CppTaskCompletionSource* source, Dn2CppTask* completed, bool copyResult)
{
    if (!dn2cpp_tcs_try_set_from_task(source, completed, copyResult))
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
    Dn2CppTask* task;            // non-null: a pending task to transition to CANCELED
    bool delay;                // only Delay cancellation adds RunContinuationsAsynchronously
    bool delayArmed;           // guarded by g_cts_mtx; consumed by claim or cleanup
    bool coldTask;             // consult the scheduler before canceling queued work
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

static bool dn2cpp_delay_claim_cancellation(Dn2CppCancelReg* registration)
{
    std::lock_guard<std::mutex> lock(g_cts_mtx);
    if (!registration->delayArmed)
        return false;
    registration->delayArmed = false;
    return true;
}

// A queued task cancels only when its scheduler removes it. The virtual
// call may reenter the task or the source and cannot run under either lock.
static void dn2cpp_cancel_cold_task(Dn2CppCancelReg* registration)
{
    auto* task = registration->task;
    Dn2CppTaskCold* work;
    Dn2CppCont* continuations = nullptr;
    bool canceled;
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        work = task->cold;
        if (work == nullptr || task->status != DN2CPP_TASK_PENDING)
            return;
        canceled = dn2cpp_task_try_complete_locked(task, DN2CPP_TASK_CANCELED, 0, nullptr,
            nullptr, continuations, registration->source, true);
        if (canceled)
            dn2cpp_task_release_action_locked(task);
        else if (work->customQueue == nullptr)
            return;
    }
    if (!canceled)
    {
        auto* scheduler = work->scheduler;
        auto* queue = work->customQueue;
        using Dequeue = int32_t (*)(Dn2CppObject*, Dn2CppTask*);
        auto dequeue = reinterpret_cast<Dequeue>(const_cast<void*>(scheduler->type->vtable[queue->dequeueSlot]));
        try
        {
            if (!dequeue(scheduler, task)) return;
        }
        catch (const Dn2CppException& e)
        {
            auto* wrapped = queue->wrapException(e.obj);
            dn2cpp_exc_inflight_pop(e.obj);
            dn2cpp_throw(wrapped);
        }
        std::lock_guard<std::mutex> lock(g_task_mtx);
        if (task->cold != work || (task->execution & DN2CPP_TASK_INVOKED)) return;
        if (dn2cpp_task_try_complete_locked(task, DN2CPP_TASK_CANCELED, 0, nullptr,
            nullptr, continuations, registration->source, false))
            dn2cpp_task_release_action_locked(task);
    }
    dn2cpp_fire_conts(continuations);
}

// Every CancelAfter timer thread waits here, so a reschedule / Cancel / Dispose on any
// source wakes every waiter with one notify_all. One shared variable rather than one per
// source keeps Dn2CppCancelSource a plain GC block with no members needing a constructor
// (contrast Dn2CppManagedTimer, which is placement-new'd for exactly that reason); the
// spurious wakeups cost a predicate re-test on a handful of threads.
static std::condition_variable& g_cts_timer_cv = dn2cpp_never_destroyed<std::condition_variable>();

extern const Dn2CppType dn2cpp_cancel_source_type_obj;
Dn2CppTypeInfo dn2cpp_cancel_source_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.CancellationTokenSource", nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppCancelSource), 0, 0, 0, 0, 0, dn2cpp_default_ctor_with_cctor_reflection }, &dn2cpp_cancel_source_type_obj);
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
    // Boolean token construction shares the BCL's permanently canceled source.
    static DN2CPP_GC_STATIC_ROOT Dn2CppCancelSource* source = [] {
        auto* s = dn2cpp_cts_new();
        s->canceled = 1;
        return s;
    }();
    return source;
}

void dn2cpp_cts_cancel(Dn2CppCancelSource* src, bool throwOnFirstException)
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
    Dn2CppRefList* faults = nullptr;
    for (Dn2CppCancelReg* r = head; r != nullptr; r = r->next)
    {
        try
        {
            if (r->coldTask)
                dn2cpp_cancel_cold_task(r);
            else if (r->task != nullptr)
            {
                // Delay's cancellation callback sets this bit before trying to settle,
                // even if an in-flight timer completion won the race. WaitAsync does not.
                if (r->delay)
                {
                    if (!dn2cpp_delay_claim_cancellation(r))
                        continue;
                    r->task->creationOptions.fetch_or(DN2CPP_TASK_ASYNC_CONTINUATIONS, std::memory_order_relaxed);
                }
                if (r->task->status == DN2CPP_TASK_PENDING)
                    dn2cpp_task_try_set_canceled(r->task, r->source);
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
                // A linked child always collects its own faults; the parent policy
                // decides whether to rethrow that aggregate or keep it as one fault.
                dn2cpp_cts_cancel(r->child, false);
            }
        }
        catch (const Dn2CppException& exception)
        {
            if (throwOnFirstException)
                throw;
            if (faults == nullptr)
                faults = dn2cpp_reflist_new();
            dn2cpp_reflist_add(faults, exception.obj);
            dn2cpp_exc_inflight_pop(exception.obj); // rooted by faults before leaving the catch
        }
    }
    if (faults != nullptr)
        dn2cpp_throw(dn2cpp_aggregate_exception_new(dn2cpp_reflist_to_array(faults)));
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
    r->delay = false;
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
    r->delay = false;
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
    r->delay = false;
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
    r->delay = false;
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

int32_t dn2cpp_ctr_unregister(Dn2CppCancelReg* reg)
{
    if (reg == nullptr || reg->source == nullptr)
        return 0;
    Dn2CppCancelSource* src = reg->source;
    std::lock_guard<std::mutex> lk(g_cts_mtx);
    for (Dn2CppCancelReg** pp = &src->regs; *pp != nullptr; pp = &(*pp)->next)
    {
        if (*pp == reg)
        {
            dn2cpp_gc_store_ref(pp, reg->next);
            return 1;
        }
    }
    return 0;
}

void dn2cpp_cts_unregister(Dn2CppCancelReg* reg)
{
    (void)dn2cpp_ctr_unregister(reg);
}

Dn2CppCancelToken dn2cpp_ctr_token(Dn2CppCancelReg* reg)
{
    return Dn2CppCancelToken{ reg != nullptr ? reg->source : nullptr };
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
// somebody is blocked on. Wake any drain for which the departing timer was
// the last other settler.
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
            try
            {
                dn2cpp_cts_cancel(s);
            }
            catch (const Dn2CppException& exception)
            {
                dn2cpp_abort_unhandled_exception(exception.obj);
            }
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
// Rescheduling and rooting are described above. Callvirt checks a null source before
// the delay; call validates the delay first. Valid delays on a disposed source throw,
// while an already canceled source is a no-op. HttpClient.Timeout reaches this helper
// through PrepareCancellationTokenSource.
//
// Delay contract, measured against real .NET: ms == -1 is Timeout.Infinite and disarms
// without ever cancelling; anything outside [-1, Timer.MaxSupportedTimeout] is
// ArgumentOutOfRangeException naming `paramName`, the overload's parameter
// ("millisecondsDelay" or "delay") — only the TimeSpan overload can exceed the ceiling, and
// checking it is also what keeps the deadline arithmetic below inside int64. ms == 0
// cancels here, synchronously, where real .NET posts to its timer queue and cancels a
// moment later — a declared divergence, because the only observable difference is whether
// the very next statement sees IsCancellationRequested, which is a race on real .NET too.
void dn2cpp_cts_cancel_after(Dn2CppCancelSource* src, int64_t ms, const char* paramName)
{
    if (ms < -1 || ms > 4294967294LL) // Timer.MaxSupportedTimeout
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, paramName);
    if (src == nullptr)
        dn2cpp_throw_null_reference();
    {
        std::lock_guard<std::mutex> lk(g_cts_mtx);
        if (src->disposed)
            dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_object_disposed_exception_type,
                dn2cpp_sr_message(DN2CPP_SR_CANCELLATION_SOURCE_DISPOSED, nullptr, 0), nullptr));
    }
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
// free. Dispose releases cold-task registrations and stops any pending CancelAfter (a
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
        if (!src->disposed)
        {
            // Cancel owns an already-detached list. Drop only cold tasks still
            // registered on this source, before they can be claimed by Cancel.
            auto** link = &src->regs;
            while (auto* registration = *link)
            {
                if (!registration->coldTask)
                {
                    link = &registration->next;
                    continue;
                }
                dn2cpp_gc_store_ref(link, registration->next);
                dn2cpp_gc_store_ref(&registration->next, static_cast<Dn2CppCancelReg*>(nullptr));
                dn2cpp_gc_store_ref(&registration->task, static_cast<Dn2CppTask*>(nullptr));
            }
        }
        src->disposed = 1;
        src->timerDueNs = 0;
    }
    g_cts_timer_cv.notify_all();
}

Dn2CppCancelSource* dn2cpp_cts_new_after(int64_t ms, const char* paramName)
{
    Dn2CppCancelSource* s = dn2cpp_cts_new();
    dn2cpp_cts_cancel_after(s, ms, paramName);
    return s;
}

static void dn2cpp_delay_unregister(void* state)
{
    auto* registration = static_cast<Dn2CppCancelReg*>(state);
    {
        // Cancel may already have detached the list. Disarm the node itself so a
        // younger callback can finish the delay before its cancellation is claimed.
        // A callback that already claimed the node keeps its right to run.
        std::lock_guard<std::mutex> lock(g_cts_mtx);
        registration->delayArmed = false;
    }
    dn2cpp_cts_unregister(registration);
    if (registration->task->status == DN2CPP_TASK_CANCELED)
        dn2cpp_task_timer_cancel(registration->task->delayTimer.load(std::memory_order_acquire));
}

Dn2CppTask* dn2cpp_task_delay_ct(int64_t ms, Dn2CppCancelSource* src)
{
    // Already-canceled source: the delay is born canceled (no timer scheduled).
    if (src != nullptr && dn2cpp_cts_is_cancelled(src))
    {
        Dn2CppTask* t = dn2cpp_task_alloc();
        t->startKind = DN2CPP_TASK_ORIGIN_PRECOMPLETED;
        dn2cpp_task_set_canceled(t, src);
        return t;
    }
    Dn2CppTask* t = dn2cpp_task_delay(ms);
    // Delay(0) is already complete, and .NET registers nothing for it.
    if (src != nullptr && ms != 0)
    {
        auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
        r->source = src;
        r->task = t;
        r->delay = true;
        r->delayArmed = true;
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
        dn2cpp_task_on_completed_sync(t, &dn2cpp_delay_unregister, r);
        if (raced && dn2cpp_delay_claim_cancellation(r))
        {
            t->creationOptions.fetch_or(DN2CPP_TASK_ASYNC_CONTINUATIONS, std::memory_order_relaxed);
            dn2cpp_task_try_set_canceled(t, src);
        }
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
    // Before the verdict below: an unarmed source-backed task has no settler counted yet.
    if (t->vtsBridge != nullptr)
        dn2cpp_vts_arm(t);
    bool waker_registered = false;
    while (t->status == DN2CPP_TASK_PENDING)
    {
        // Drain ready work first; only when nothing is runnable do we advance the
        // virtual clock to the next Task.Delay.
        if (dn2cpp_sched_run_one())
            continue;
        auto wake = Dn2CppTaskClock::time_point::max();
        if (dn2cpp_sched_advance_timers(INT64_MAX, nullptr, &wake))
            continue;
        // Nothing runnable on this thread. Either another principal will still settle
        // `t` — a pool worker (Task.Run / Start / pool work in flight) or a live user
        // thread (RunSynchronously on a cold task, a TaskCompletionSource set from a
        // thread of the program's own) — in which case we sleep until it posts a
        // continuation here and wakes our cv, or there is no such principal and the
        // wait can never be satisfied.
        if (wake == Dn2CppTaskClock::time_point::max() && !dn2cpp_task_settler_exists())
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
        if (wake != Dn2CppTaskClock::time_point::max())
        {
            if (s->head == nullptr && s->timers != nullptr && t->status == DN2CPP_TASK_PENDING)
                s->cv.wait_until(lk, wake);
            continue;
        }
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
// missing and what to do about it. A parked drain rechecks when another possible
// settler departs or the entry thread becomes blocked.
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

Dn2CppConfiguredTaskAwaiter dn2cpp_task_configure_await(Dn2CppTask* task,
    uint32_t options, bool isGeneric)
{
    constexpr uint32_t allowed = DN2CPP_AWAIT_CONTINUE_ON_CONTEXT
        | DN2CPP_AWAIT_SUPPRESS_THROWING | DN2CPP_AWAIT_FORCE_YIELDING;
    if (isGeneric && (options & DN2CPP_AWAIT_SUPPRESS_THROWING) != 0)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_TASK_GENERIC_AWAIT_OPTIONS, "options");
    if ((options & ~allowed) != 0)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, "options");
    return {task, options};
}

void dn2cpp_configured_task_get_result(const Dn2CppConfiguredTaskAwaiter* awaiter)
{
    auto* task = dn2cpp_null_check(awaiter->task);
    if ((awaiter->options & DN2CPP_AWAIT_SUPPRESS_THROWING) == 0)
    {
        dn2cpp_task_block(task);
        return;
    }
    if (!dn2cpp_task_drain_settle(task))
        dn2cpp_task_throw_deadlock();
    // Suppression observes a fault without clearing it or raising its EDI.
    if (task->status == DN2CPP_TASK_FAULTED)
    {
        if (auto* holder = dn2cpp_task_holder_for_observation(task))
        {
            Dn2CppMonitorGuard guard(holder);
            g_task_holder_observe(holder);
        }
    }
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
    dn2cpp_null_check(t);
    if (!dn2cpp_task_drain_settle(t))
        dn2cpp_task_throw_deadlock();
    if (t->status == DN2CPP_TASK_FAULTED || t->status == DN2CPP_TASK_CANCELED)
    {
        dn2cpp_throw(dn2cpp_aggregate_exception_new(dn2cpp_task_fault_snapshot(t)));
    }
    return t;
}

static Dn2CppCont* dn2cpp_sched_take_unbound_locked(Dn2CppScheduler* s)
{
    Dn2CppCont* prev = nullptr;
    for (auto* c = s->head; c != nullptr; c = c->next)
    {
        if (!c->capturedContext)
        {
            if (prev == nullptr)
                s->head = c->next;
            else
                dn2cpp_gc_store_ref(&prev->next, c->next);
            if (s->tail == c)
                s->tail = prev;
            dn2cpp_gc_store_ref(&c->next, static_cast<Dn2CppCont*>(nullptr));
            return c;
        }
        prev = c;
    }
    return nullptr;
}

// A timeout and the delay it observes can be on different schedulers. Snapshot
// both against the same wall instant before either one's notifications can win.
static bool dn2cpp_task_observe_timers(Dn2CppTask* task, Dn2CppTaskClock::time_point through,
                                       Dn2CppTaskClock::time_point* wake = nullptr)
{
    dn2cpp_sched_resume_timers();
    auto* timer = task->delayTimer.load(std::memory_order_acquire);
    auto* owner = timer != nullptr && timer->task == task ? timer->owner : nullptr;
    auto* local = t_sched_ptr;
    if (local == nullptr && owner == nullptr)
        return false;
    Dn2CppTimerQueues queues(local != nullptr ? local : owner, owner);
    if (queues.busy(wake != nullptr))
        return false;
    std::vector<Dn2CppTimer*> elapsed;
    for (int i = 0; i < queues.count; ++i)
    {
        for (auto* e = queues.owners[i]->timers; e != nullptr; e = e->next)
        {
            if (e->wall_due <= through)
                elapsed.push_back(e);
            else if (wake != nullptr)
                *wake = std::min(*wake, e->wall_due);
        }
    }
    if (!elapsed.empty())
    {
        std::sort(elapsed.begin(), elapsed.end(), [](Dn2CppTimer* a, Dn2CppTimer* b) { return a->precedes(b); });
        for (int i = 0; i < queues.count; ++i)
            dn2cpp_sched_claim_timers_locked(queues.owners[i],
                [&](Dn2CppTimer* e) { return e->wall_due <= through; }, [](Dn2CppTimer*) {});
        for (auto* e : elapsed)
            dn2cpp_sched_queue_timer_locked(e->owner, e);
    }
    return queues.publish();
}

static bool dn2cpp_task_drain_timed(Dn2CppTask* t, int32_t ms)
{
    auto* s = dn2cpp_sched_self();
    Dn2CppTimedTaskWait wait(t, s, ms);
    const bool canDispatch = dn2cpp_sched_can_dispatch_pool();
    if (t->vtsBridge != nullptr)
        dn2cpp_vts_arm(t);
    while (t->status == DN2CPP_TASK_PENDING)
    {
        auto generation = g_timer_observer_generation.load(std::memory_order_acquire);
        auto now = Dn2CppTaskClock::now();
        auto wake = wait.deadline;
        if (dn2cpp_task_observe_timers(t, std::min(now, wait.deadline), &wake))
            continue;
        if (now >= wait.deadline)
            break;
        std::unique_lock<std::mutex> lock(s->mtx);
        if (t->status != DN2CPP_TASK_PENDING)
            break;
        // A positive wait cannot run arbitrary managed code on its caller.
        // Context-free resumptions may move to the pool; captured ones stay put.
        if (auto* c = canDispatch ? dn2cpp_sched_take_unbound_locked(s) : nullptr)
        {
            lock.unlock();
            dn2cpp_pending_cont_unlink(c);
            c->wallTimers = true;
            dn2cpp_sched_queue_pool_cont(c);
        }
        else
        {
            // A contending publisher can finish between observing its queue
            // and parking here. A finite entry remains a possible settler.
            if (g_timer_observer_generation.load(std::memory_order_acquire) == generation)
                s->cv.wait_until(lock, wake);
        }
    }
    return t->status != DN2CPP_TASK_PENDING && wait.completedInTime;
}

int32_t dn2cpp_task_wait_timeout(Dn2CppTask* t, int64_t ticks)
{
    int64_t ms = static_cast<int64_t>(static_cast<double>(ticks) / 10000.0);
    if (ms < -1 || ms > INT32_MAX)
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_ARGUMENT_OUT_OF_RANGE, "timeout");
    dn2cpp_null_check(t);
    if (ms == 0 && t->status == DN2CPP_TASK_PENDING)
    {
        dn2cpp_task_observe_timers(t, Dn2CppTaskClock::now());
        if (t->status == DN2CPP_TASK_PENDING)
            return 0;
    }
    else if (ms > 0 && t->status == DN2CPP_TASK_PENDING && !dn2cpp_task_drain_timed(t, static_cast<int32_t>(ms)))
        return 0;
    dn2cpp_task_block_wait(t);
    return 1;
}

// Task.WaitAll(Task[]): block until every input settles, then raise. Real .NET waits
// for ALL of them before throwing and reports every failure in one AggregateException
// (a single faulted input still gets a wrapper), so the drain loop must not re-raise as
// it goes — it settles first and collects after. Cancellation counts as a failure, like
// .NET's WaitAllCore. A canceled task contributes a TaskCanceledException minted
// for the blocking wait, while its stored exception remains the original OCE that
// await rethrows.
void dn2cpp_task_wait_all(Dn2CppArrayRef* tasks)
{
    if (tasks == nullptr)
        dn2cpp_throw_argument_null_param("tasks");
    // The null-element scan runs AHEAD of the drain: .NET validates the whole array
    // before it waits on anything, so WaitAll([pending, null]) is ArgumentException
    // rather than a block on the pending input that never sees the null.
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (tasks->data[i] == nullptr)
            dn2cpp_throw_argument_param(DN2CPP_SR_WAIT_NULL_TASK, "tasks");
    }
    for (int32_t i = 0; i < tasks->length; i++)
    {
        if (!dn2cpp_task_drain_settle(reinterpret_cast<Dn2CppTask*>(tasks->data[i])))
            dn2cpp_task_throw_deadlock();
    }
    Dn2CppRefList* faults = nullptr;
    for (int32_t i = 0; i < tasks->length; i++)
    {
        auto* t = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
        if (t->status == DN2CPP_TASK_FAULTED || t->status == DN2CPP_TASK_CANCELED)
            dn2cpp_task_collect_fault(faults, t);
    }
    if (faults != nullptr)
        dn2cpp_throw(dn2cpp_aggregate_exception_new(dn2cpp_reflist_to_array(faults)));
}

// Task.WaitAny(Task[]) -> the index of the first input to settle. Never raises the
// winner's fault (real .NET returns the index and leaves the fault to be observed
// through that task), so it drains the WhenAny join task — which always SUCCEEDS —
// rather than any input. An already-settled input short-circuits before the join is
// built, matching .NET's "returns immediately" contract and keeping a settled batch
// from touching the scheduler at all.
//
// Its argument contract differs from WhenAny's, so do not copy one onto the other: an
// EMPTY array answers -1 here (WhenAny rejects it), and a null element is always
// ArgumentException with WaitAny's own message (a two-task WhenAny gives
// ArgumentNullException).
int32_t dn2cpp_task_wait_any(Dn2CppArrayRef* tasks)
{
    if (tasks == nullptr)
        dn2cpp_throw_argument_null_param("tasks");
    // The null scan covers the WHOLE array before an index may be returned: .NET
    // rejects WaitAny([completed, null]) rather than answering 0.
    int32_t settled = -1;
    for (int32_t i = 0; i < tasks->length; i++)
    {
        auto* t = reinterpret_cast<Dn2CppTask*>(tasks->data[i]);
        if (t == nullptr)
            dn2cpp_throw_argument_param(DN2CPP_SR_WAIT_NULL_TASK, "tasks");
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

// Task.get_Exception. FAULTED is a settled status, so the primary exception is
// immutable. Holder-backed observations follow the holder's current fault set.
// The ordinary native wrapper is cached on first read. Allocations happen OUTSIDE the lock (the
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
    if (dn2cpp_task_holder_for_observation(t) != nullptr)
        return dn2cpp_aggregate_exception_new(dn2cpp_task_fault_snapshot(t));
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
    // A finite wait may already be driving this scheduler from a wall-clock origin.
    // Keep that origin when the host takes over frame pumping.
    Dn2CppScheduler* s = dn2cpp_sched_self();
    auto now = Dn2CppTaskClock::now();
    if (s->clock != &s->frame_clock)
    {
        s->frame_clock = s->clock != nullptr ? *s->clock : Dn2CppSchedulerClock{now, s->virtual_now};
        s->clock = &s->frame_clock;
    }
    int64_t target = s->clock->at(now);
    while (dn2cpp_sched_advance_timers(target, s->clock))
    {
    }
    s->virtual_now = std::max(s->virtual_now, target);
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
            dn2cpp_sched_invoke_cont(c);
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
                dn2cpp_abort_unhandled_exception(__ex.obj);
        }
        c = next;
    }
}

// ===== System.Threading.Thread (real OS threads) =============================
extern const Dn2CppType dn2cpp_thread_type_obj;
// NO_SHALLOW_CLONE: Dn2CppThread's `handle` is a `new std::thread` and `sync` a
// native-heap Dn2CppThreadSync; a bitwise copy would be a second joiner and a second
// deleter of one OS thread.
Dn2CppTypeInfo dn2cpp_thread_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.Thread", nullptr, nullptr, nullptr, nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, DN2CPP_TF_NO_SHALLOW_CLONE, 0, 0, 0, nullptr }, &dn2cpp_thread_type_obj);
const Dn2CppType dn2cpp_thread_type_obj = { { &dn2cpp_type_type }, &dn2cpp_thread_type };

// Native completion signal for a timed Join. Lives on the native heap (so its
// mutex/condition_variable get real ctors) and is reachable from the GC-allocated
// Dn2CppThread via a plain pointer; it outlives the program like the monitor table.
struct Dn2CppThreadSync
{
    std::mutex m;
    Dn2CppWaitCondition cv;
    bool done = false;
};

struct Dn2CppThread : Dn2CppObject
{
    Dn2CppObject* start;   // ThreadStart (no-arg) / ParameterizedThreadStart (object) delegate
    Dn2CppObject* arg;     // Start(object) payload — held here so the GC keeps it alive
    void* handle;          // std::thread* (native heap; not a GC pointer)
    void* sync;            // Dn2CppThreadSync*: null until Start; a materialized thread's never signals
    Dn2CppString* name;
    int32_t managedId;
    int32_t isBackground;
    int32_t alive;         // advisory (IsAlive); set 1 in the trampoline, 0 on exit
    int32_t parameterized; // built over a ParameterizedThreadStart
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

static void dn2cpp_thread_trampoline(Dn2CppThread* t)
{
    Dn2CppGCThread guard;        // register this thread with the GC for its lifetime
    g_current_thread = t;        // Thread.CurrentThread inside the body
    t_on_user_thread = true;     // this thread is its own deadlock-test exemption
    t->alive = 1;
    try
    {
        if (t->parameterized)
            dn2cpp_paramthread_invoke(t->start, t->arg);
        else
            dn2cpp_action_invoke(t->start);
    }
    catch (const Dn2CppException& exception)
    {
        // An unhandled exception on a thread terminates the process in .NET.
        t->alive = 0;
        dn2cpp_thread_signal_done(t); // release any timed Join before we bail out
        dn2cpp_abort_unhandled_exception(exception.obj);
    }
    t->alive = 0;
    dn2cpp_thread_signal_done(t);
    // Publish the Join handoff before this thread leaves the settler set.
    dn2cpp_principal_left(g_live_user_threads);
}

Dn2CppThread* dn2cpp_thread_new(Dn2CppObject* start, int32_t parameterized, int32_t maxStackSize)
{
    if (start == nullptr)
        dn2cpp_throw_argument_null_param("start");
    if (maxStackSize < 0)
        dn2cpp_throw_argument_out_of_range_value(DN2CPP_SR_MUST_BE_NON_NEGATIVE, "maxStackSize", maxStackSize);
    auto* t = static_cast<Dn2CppThread*>(dn2cpp_alloc(sizeof(Dn2CppThread)));
    t->type = &dn2cpp_thread_type;
    dn2cpp_gc_store_ref(&t->start, start);
    t->arg = nullptr;
    t->handle = nullptr;
    t->sync = nullptr;
    t->name = nullptr;
    t->managedId = g_next_thread_id.fetch_add(1, std::memory_order_relaxed);
    t->isBackground = 0;
    t->alive = 0;
    t->parameterized = parameterized;
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
//
// The handle is published under s->m, held from before the thread exists: the body's done
// signal takes the same lock, so a joiner that saw done also sees the handle to reap.
static void dn2cpp_thread_spawn(Dn2CppThread* t)
{
    g_live_user_threads.fetch_add(1, std::memory_order_acq_rel);
    auto* s = static_cast<Dn2CppThreadSync*>(t->sync);
    try
    {
        std::lock_guard<std::mutex> lk(s->m);
        t->handle = new std::thread([t] { dn2cpp_thread_trampoline(t); });
    }
    catch (...)
    {
        dn2cpp_principal_left(g_live_user_threads);
        dn2cpp_thread_signal_done(t); // no body will run: release a Join instead of stranding it
        throw;
    }
}

// A thread starts once. The claim is atomic, so neither a second Start nor two racing
// ones run the body again over the same object, and a refused Start(object) leaves the
// thread unstarted. As in .NET, the already-started refusal precedes the delegate-kind one.
static std::mutex& g_thread_start_mtx = dn2cpp_never_destroyed<std::mutex>();

static void dn2cpp_thread_claim_start(Dn2CppThread* t, bool withArg, Dn2CppObject* arg)
{
    const Dn2CppTypeInfo* refusedAs = nullptr;
    const char* refusal = nullptr;
    {
        std::lock_guard<std::mutex> lk(g_thread_start_mtx);
        if (t->sync != nullptr)
        {
            refusedAs = &dn2cpp_thread_state_exception_type;
            refusal = DN2CPP_SR_THREAD_ALREADY_STARTED;
        }
        else if (withArg && !t->parameterized)
        {
            refusedAs = &dn2cpp_invalid_operation_exception_type;
            refusal = DN2CPP_SR_THREAD_WRONG_THREAD_START;
        }
        else
        {
            dn2cpp_gc_store_ref(&t->arg, arg);
            t->sync = new Dn2CppThreadSync(); // before the thread, so the trampoline sees it
        }
    }
    if (refusedAs != nullptr)
        dn2cpp_throw_sr0(refusedAs, refusal);
}

void dn2cpp_thread_start(Dn2CppThread* t)
{
    dn2cpp_thread_claim_start(t, false, nullptr);
    dn2cpp_thread_spawn(t);
}

void dn2cpp_thread_start_param(Dn2CppThread* t, Dn2CppObject* arg)
{
    dn2cpp_thread_claim_start(t, true, arg);
    dn2cpp_thread_spawn(t);
}

static Dn2CppThreadSync* dn2cpp_thread_started_sync(Dn2CppThread* t)
{
    Dn2CppThreadSync* s;
    {
        std::lock_guard<std::mutex> lk(g_thread_start_mtx);
        s = static_cast<Dn2CppThreadSync*>(t->sync);
    }
    if (s == nullptr)
        dn2cpp_throw_sr0(&dn2cpp_thread_state_exception_type, DN2CPP_SR_THREAD_NOT_STARTED);
    return s;
}

// Joiners wait on the done signal, and the first one to see it takes the OS handle, so
// concurrent Joins never join one std::thread twice.
static void dn2cpp_thread_reap(Dn2CppThread* t, Dn2CppThreadSync* s)
{
    std::thread* th;
    {
        std::lock_guard<std::mutex> lk(s->m);
        th = static_cast<std::thread*>(t->handle);
        t->handle = nullptr;
    }
    if (th == nullptr)
        return;
    if (th->joinable())
        th->join();
    delete th;
}

void dn2cpp_thread_join(Dn2CppThread* t)
{
    auto* s = dn2cpp_thread_started_sync(t);
    {
        std::unique_lock<std::mutex> lk(s->m);
        s->cv.wait(lk, [s] { return s->done; });
    }
    dn2cpp_thread_reap(t, s);
}

// Thread.Join(int)/Join(TimeSpan): wait up to ms for the body to finish. Returns 1 if it
// terminated (and joins the underlying std::thread), 0 on timeout (the thread is still
// running — a later Join() reaps it). -1 means an infinite wait; less is refused as
// ThrowIfLessThan refuses it, before the thread's state is.
int32_t dn2cpp_thread_join_timeout(Dn2CppThread* t, int32_t ms)
{
    if (ms < -1)
        dn2cpp_throw_argument_out_of_range_bound(DN2CPP_SR_MUST_BE_GREATER_OR_EQUAL,
            "millisecondsTimeout", ms, -1, 4);
    auto* s = dn2cpp_thread_started_sync(t);
    if (ms < 0)
    {
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
    dn2cpp_thread_reap(t, s);
    return 1;
}

void dn2cpp_thread_sleep(int32_t ms)
{
    dn2cpp_timeout_require_ms(ms);
    if (ms == -1)
    {
        std::mutex mutex;
        std::unique_lock<std::mutex> lock(mutex);
        Dn2CppWaitCondition never;
        never.wait(lock, [] { return false; });
        return;
    }
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
    // Started and never finishing: Start refuses it, and a Join on it times out.
    t->sync = new Dn2CppThreadSync();
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
    Dn2CppCancelSource* cancellationSource; // scheduling token for an unwrap operation
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
    { std::lock_guard<std::mutex> lock(g_pool_mtx); }
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
    dn2cpp_gc_store_ref(&node->task, task);
    dn2cpp_gc_store_ref(&node->del, del);
    dn2cpp_gc_store_ref(&node->state, state);
    node->next = nullptr;
    node->cancellationSource = nullptr;
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
        if (s->ready_timers != nullptr)
        {
            dn2cpp_gc_store_ref(&s->ready_timers_tail->next, timers);
            timers = s->ready_timers;
            s->ready_timers = s->ready_timers_tail = nullptr;
        }
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
        dn2cpp_gc_store_ref(&timers->task->delayTimer, static_cast<Dn2CppTimer*>(nullptr));
        dn2cpp_pending_timer_unlink(timers);
        dn2cpp_gc_store_ref(&timers->next, static_cast<Dn2CppTimer*>(nullptr));
        timers = next;
    }
    if (left)
        dn2cpp_principal_left(g_inflight_async_tasks);
}

// Invoke the entire Func<Task> chain before observing its last return. Awaiting an
// earlier task here can deadlock when a later handler settles it; worker-local work from
// discarded returns remains rooted and is driven by the pool scheduler.
static Dn2CppTask* dn2cpp_pool_inner_chain(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_pool_inner_chain(d->prev);
    return reinterpret_cast<Dn2CppTask* (*)(Dn2CppObject*)>(d->method)(d->target);
}

static void dn2cpp_task_append_terminal_fault(Dn2CppTask* task, Dn2CppObject* exception)
{
    if (g_task_holder_create != nullptr)
    {
        dn2cpp_task_add_exception(task, exception, false);
        return;
    }
    Dn2CppRefList* faults = nullptr;
    dn2cpp_task_collect_fault(faults, task);
    dn2cpp_reflist_add(faults, exception);
    dn2cpp_gc_store_ref(&task->exceptionAggregate, dn2cpp_aggregate_exception_new(dn2cpp_reflist_to_array(faults)));
}

static void dn2cpp_task_finish_cold_delegate(Dn2CppTask* task, uint64_t result, Dn2CppObject* exception)
{
    int32_t status = DN2CPP_TASK_SUCCEEDED;
    Dn2CppCancelSource* cancellation = nullptr;
    if (exception != nullptr)
    {
        auto* source = task->cancellationSource.load(std::memory_order_acquire);
        if (source != nullptr && dn2cpp_cts_is_cancelled(source) && dn2cpp_canceled_exception_source(exception) == source)
            cancellation = source;
        status = cancellation != nullptr ? DN2CPP_TASK_CANCELED : DN2CPP_TASK_FAULTED;
    }
    if (dn2cpp_task_finish_recorded(task, status, result, exception, cancellation)
        || !(task->execution & DN2CPP_TASK_QUEUE_REJECTED_WHILE_RUNNING)) return;

    // The queue's rejection won completion. Preserve the delegate's later fault
    // in the same order without notifying continuations a second time.
    dn2cpp_task_await_winner(task, false);
    if (status == DN2CPP_TASK_FAULTED) dn2cpp_task_append_terminal_fault(task, exception);
    dn2cpp_throw_sr0(&dn2cpp_invalid_cast_exception_type, DN2CPP_SR_INVALID_CAST);
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
    auto localWake = Dn2CppTaskClock::time_point::min();
    for (;;)
    {
        Dn2CppWorkItem it;
        bool runLocal = false;
        {
            std::unique_lock<std::mutex> lk(g_pool_mtx);
            auto localReady = [&] {
                if (!scheduler->pool_local_principal.load(std::memory_order_acquire))
                    return false;
                if (localWake == Dn2CppTaskClock::time_point::min() || Dn2CppTaskClock::now() >= localWake)
                    return true;
                std::lock_guard<std::mutex> queue(scheduler->mtx);
                return scheduler->head != nullptr || scheduler->timers == nullptr;
            };
            auto ready = [&] { return g_pool_stop || !g_pool_q.empty() || localReady(); };
            if (localWake == Dn2CppTaskClock::time_point::min())
                g_pool_cv.wait(lk, ready);
            else
                g_pool_cv.wait_until(lk, localWake, ready);
            if (g_pool_stop)
                break; // quiesce: the queue was already dropped, leave at once
            if (!g_pool_q.empty() && (!preferLocal || !localReady()))
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
        localWake = Dn2CppTaskClock::time_point::min();
        if (runLocal)
        {
            try
            {
                // One turn only. A self-rescheduling await loop retains its
                // principal but cannot monopolize the worker over global items.
                if (!dn2cpp_sched_run_one())
                {
                    auto wake = Dn2CppTaskClock::time_point::max();
                    if (!dn2cpp_sched_advance_timers(INT64_MAX, nullptr, &wake)
                        && wake != Dn2CppTaskClock::time_point::max())
                        localWake = wake;
                }
            }
            catch (const Dn2CppException& exception)
            {
                dn2cpp_abort_unhandled_exception(exception.obj);
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
            catch (const Dn2CppException& exception)
            {
                dn2cpp_abort_unhandled_exception(exception.obj);
            }
            dn2cpp_sched_sync_pool_principal(scheduler);
            dn2cpp_pool_unlink(it.node);
            dn2cpp_principal_left(g_inflight_async_tasks);
            continue;
        }
        try
        {
            if (!it.unwrap)
                it.task->execution.fetch_or(DN2CPP_TASK_INVOKED);
            Dn2CppCancelSource* source = it.unwrap ? it.node->cancellationSource
                : it.task->cancellationSource.load(std::memory_order_acquire);
            if (source != nullptr && dn2cpp_cts_is_cancelled(source))
                dn2cpp_task_set_canceled(it.task, source);
            else if (it.unwrap)
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
                // The hidden Task.Run owner denies child attachment.
                Dn2CppTask* inner;
                {
                    Dn2CppTaskDelegateScope scope(nullptr);
                    scope.hiddenId = -1;
                    inner = dn2cpp_pool_inner_chain(it.del);
                }
                // Unwrapping a null task is CANCELED on .NET, not a crash: an async lambda
                // whose body returns null hands one back, and the outer is all the caller
                // holds.
                if (inner == nullptr)
                {
                    dn2cpp_task_set_canceled(it.task);
                }
                else
                {
                    dn2cpp_task_drain(inner);
                    dn2cpp_task_forward_completion(it.task, inner, inner->result);
                }
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
                Dn2CppTask* inner = dn2cpp_task_invoke_as(it.task, [&] { return dn2cpp_pool_inner_chain(it.del); });
                dn2cpp_task_finish_recorded(it.task, DN2CPP_TASK_SUCCEEDED,
                    static_cast<uint64_t>(reinterpret_cast<uintptr_t>(inner)), nullptr);
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
                    catch (const Dn2CppException& exception)
                    {
                        dn2cpp_abort_unhandled_exception(exception.obj);
                    }
                }
            }
            else if (it.invoke2 != nullptr)
            {
                // State-carrying Task item: the 2-arg thunk reads the delegate and
                // its state argument from the GC-rooted holder node.
                uint64_t r = dn2cpp_task_invoke_as(it.task, [&] { return it.invoke2(it.node->del, it.node->state); });
                dn2cpp_task_finish_recorded(it.task, DN2CPP_TASK_SUCCEEDED, r, nullptr);
            }
            else
            {
                uint64_t r = dn2cpp_task_invoke_as(it.task, [&] { return it.invoke(it.del); });
                dn2cpp_task_finish_recorded(it.task, DN2CPP_TASK_SUCCEEDED, r, nullptr);
            }
        }
        catch (const Dn2CppException& e)
        {
            int32_t origin = it.task->startKind.load(std::memory_order_acquire);
            if (origin == DN2CPP_TASK_ORIGIN_COLD || origin == DN2CPP_TASK_ORIGIN_UNOWNED_QUEUE)
                dn2cpp_task_finish_cold_delegate(it.task, it.task->result, e.obj);
            else
                dn2cpp_task_finish_recorded(it.task, DN2CPP_TASK_FAULTED, it.task->result, e.obj);
            dn2cpp_exc_inflight_pop(e.obj);
        }
        dn2cpp_sched_sync_pool_principal(scheduler);
        // Work done: allow the delegate to be collected.
        dn2cpp_gc_store_ref(&it.task->workerKeepAlive, static_cast<void*>(nullptr));
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
    dn2cpp_gc_store_ref(&t->workerKeepAlive, static_cast<void*>(del));
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

Dn2CppTask* dn2cpp_pool_submit(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*), int32_t options, Dn2CppCancelSource* source)
{
    Dn2CppTask* t = dn2cpp_task_alloc(options, source);
    t->startKind = DN2CPP_TASK_ORIGIN_POOL;
    t->execution = DN2CPP_TASK_SCHEDULED;
    dn2cpp_pool_enqueue(t, del, nullptr, invoke, nullptr);
    return t;
}

// TaskFactory.StartNew(Action<object?>, state): like dn2cpp_pool_submit but the queued
// item carries a 2-arg thunk; the worker invokes it with the holder node's del + state
// (both rooted by the node from enqueue until the task settles).
static Dn2CppTask* dn2cpp_pool_submit_state(Dn2CppObject* del, Dn2CppObject* state,
                                            uint64_t (*invoke2)(Dn2CppObject*, Dn2CppObject*), int32_t options, Dn2CppCancelSource* source)
{
    Dn2CppTask* t = dn2cpp_task_alloc(options, source);
    t->startKind = DN2CPP_TASK_ORIGIN_POOL;
    t->execution = DN2CPP_TASK_SCHEDULED;
    dn2cpp_gc_store_ref(&t->asyncState, state);
    dn2cpp_pool_enqueue(t, del, state, nullptr, invoke2);
    return t;
}

// Task.Run(Func<Task> / Func<Task<T>>): like dn2cpp_pool_submit but the queued item is
// flagged `unwrap`, so the worker invokes `del` for its inner Task and settles the outer
// task with the inner's status/result/exception (see the worker loop). The outer task
// stays counted in g_inflight_async_tasks from here until the worker settles it, keeping
// an awaiting thread asleep (not deadlock-failed) for the whole inner-task lifetime.
static Dn2CppTask* dn2cpp_pool_submit_unwrap(Dn2CppObject* del, Dn2CppCancelSource* source)
{
    if (source != nullptr && dn2cpp_cts_is_cancelled(source))
        return dn2cpp_task_from_canceled(source);
    Dn2CppTask* t = dn2cpp_task_alloc();
    // A caller holding the task also keeps its worker delegate reachable.
    dn2cpp_gc_store_ref(&t->workerKeepAlive, static_cast<void*>(del));
    Dn2CppPoolNode* node = dn2cpp_pool_node_new(t, del, nullptr);
    dn2cpp_gc_store_ref(&node->cancellationSource, source);
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
static Dn2CppTask* dn2cpp_pool_submit_nested(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    Dn2CppTask* t = dn2cpp_task_alloc(options, source);
    t->startKind = DN2CPP_TASK_ORIGIN_POOL;
    t->execution = DN2CPP_TASK_SCHEDULED;
    // A caller holding the task also keeps its worker delegate reachable.
    dn2cpp_gc_store_ref(&t->workerKeepAlive, static_cast<void*>(del));
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

// Every delegate-taking entry — the pool submissions below and the cold constructors —
// rejects a null delegate HERE, synchronously, before a Task exists: that is where real
// .NET throws, and a queued null would instead reach the worker as a call through a null
// method pointer, with no managed exception to catch. The paramName is the overload's
// own: "action" for the void kinds, "function" for every result kind (the Task-returning
// unwrap/nested forms included) — except the constructors, which say "action" for every
// kind, the Func ones included, .NET's sole check being its Task(Delegate action, ...).
static void dn2cpp_task_require_delegate(Dn2CppObject* del, const char* paramName)
{
    if (del == nullptr)
        dn2cpp_throw_argument_null_param(paramName);
}

// Result-kind invoke thunks: run the delegate's whole multicast chain and pack the
// return value into the 8-byte Task result slot using the same conventions as WhenAll /
// FromResult. .NET runs a combined Func front-to-back and keeps only the LAST handler's
// value, which is what the prev-first recursion gives: `prev` is the earlier handler.
static uint64_t dn2cpp_run_void(Dn2CppObject* del)
{
    dn2cpp_action_invoke(del);
    return 0;
}
static uint64_t dn2cpp_run_i4(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_i4(d->prev);
    return static_cast<uint64_t>(static_cast<uint32_t>(
        reinterpret_cast<int32_t (*)(Dn2CppObject*)>(d->method)(d->target)));
}
static uint64_t dn2cpp_run_i8(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_i8(d->prev);
    return static_cast<uint64_t>(reinterpret_cast<int64_t (*)(Dn2CppObject*)>(d->method)(d->target));
}
static uint64_t dn2cpp_run_r4(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_r4(d->prev);
    return dn2cpp_r8_bits(static_cast<double>(reinterpret_cast<float (*)(Dn2CppObject*)>(d->method)(d->target)));
}
static uint64_t dn2cpp_run_r8(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_r8(d->prev);
    return dn2cpp_r8_bits(reinterpret_cast<double (*)(Dn2CppObject*)>(d->method)(d->target));
}
static uint64_t dn2cpp_run_ref(Dn2CppObject* del)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_ref(d->prev);
    return static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
        reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*)>(d->method)(d->target)));
}

Dn2CppTask* dn2cpp_task_run_void(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "action");
    return dn2cpp_pool_submit(del, &dn2cpp_run_void, options, source);
}
Dn2CppTask* dn2cpp_task_run_i4(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit(del, &dn2cpp_run_i4, options, source);
}
Dn2CppTask* dn2cpp_task_run_i8(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit(del, &dn2cpp_run_i8, options, source);
}
Dn2CppTask* dn2cpp_task_run_r4(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit(del, &dn2cpp_run_r4, options, source);
}
Dn2CppTask* dn2cpp_task_run_r8(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit(del, &dn2cpp_run_r8, options, source);
}
Dn2CppTask* dn2cpp_task_run_ref(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit(del, &dn2cpp_run_ref, options, source);
}
// A struct result carries its own boxing thunk (the transpiler stamps in the struct's
// exact type); the worker packs the boxed pointer into the 8-byte slot like any other run.
Dn2CppTask* dn2cpp_task_run_struct(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*), int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit(del, invoke, options, source);
}
Dn2CppTask* dn2cpp_task_run_unwrap(Dn2CppObject* del, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit_unwrap(del, source);
}
Dn2CppTask* dn2cpp_task_run_nested(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit_nested(del, options, source);
}

// TaskFactory.StartNew(Action<object?>, state): run the 1-arg delegate chain with the
// state argument (the same multicast-aware invoke the fire-and-forget items use).
static uint64_t dn2cpp_run_void_state(Dn2CppObject* del, Dn2CppObject* state)
{
    dn2cpp_paramthread_invoke(del, state);
    return 0;
}
static uint64_t dn2cpp_run_i4_state(Dn2CppObject* del, Dn2CppObject* state)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_i4_state(d->prev, state);
    return static_cast<uint64_t>(static_cast<uint32_t>(
        reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, state)));
}
static uint64_t dn2cpp_run_i8_state(Dn2CppObject* del, Dn2CppObject* state)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_i8_state(d->prev, state);
    return static_cast<uint64_t>(reinterpret_cast<int64_t (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, state));
}
static uint64_t dn2cpp_run_r4_state(Dn2CppObject* del, Dn2CppObject* state)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_r4_state(d->prev, state);
    return dn2cpp_r8_bits(static_cast<double>(reinterpret_cast<float (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, state)));
}
static uint64_t dn2cpp_run_r8_state(Dn2CppObject* del, Dn2CppObject* state)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_r8_state(d->prev, state);
    return dn2cpp_r8_bits(reinterpret_cast<double (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, state));
}
static uint64_t dn2cpp_run_ref_state(Dn2CppObject* del, Dn2CppObject* state)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_run_ref_state(d->prev, state);
    return static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
        reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, state)));
}

Dn2CppTask* dn2cpp_task_run_void_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "action");
    return dn2cpp_pool_submit_state(del, state, &dn2cpp_run_void_state, options, source);
}
Dn2CppTask* dn2cpp_task_run_i4_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit_state(del, state, &dn2cpp_run_i4_state, options, source);
}
Dn2CppTask* dn2cpp_task_run_i8_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit_state(del, state, &dn2cpp_run_i8_state, options, source);
}
Dn2CppTask* dn2cpp_task_run_r4_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit_state(del, state, &dn2cpp_run_r4_state, options, source);
}
Dn2CppTask* dn2cpp_task_run_r8_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit_state(del, state, &dn2cpp_run_r8_state, options, source);
}
Dn2CppTask* dn2cpp_task_run_ref_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "function");
    return dn2cpp_pool_submit_state(del, state, &dn2cpp_run_ref_state, options, source);
}

// ---- cold tasks: `new Task(...)` / `new Task<T>(...)` + Start / RunSynchronously ----

static void dn2cpp_unregister_cold_cancellation(void* state)
{
    dn2cpp_cts_unregister(static_cast<Dn2CppCancelReg*>(state));
}

static void dn2cpp_register_cold_cancellation(Dn2CppTask* task, Dn2CppCancelSource* source)
{
    auto* registration = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
    dn2cpp_gc_store_ref(&registration->task, task);
    dn2cpp_gc_store_ref(&registration->source, source);
    registration->coldTask = true;
    // CoreLib registers during construction. A later user registration must fire
    // first, even if it was installed before Start assigned the scheduler.
    bool alreadyCanceled;
    {
        std::lock_guard<std::mutex> lock(g_cts_mtx);
        if (source->disposed) return;
        alreadyCanceled = source->canceled;
        if (!alreadyCanceled)
        {
            dn2cpp_gc_store_ref(&registration->next, source->regs);
            dn2cpp_gc_store_ref(&source->regs, registration);
        }
    }
    dn2cpp_task_on_completed_sync(task, &dn2cpp_unregister_cold_cancellation, registration);
    if (alreadyCanceled)
        dn2cpp_cancel_cold_task(registration);
}

static Dn2CppTask* dn2cpp_task_cold(Dn2CppObject* del, Dn2CppObject* state,
                                    uint64_t (*invoke)(Dn2CppObject*),
                                    uint64_t (*invoke2)(Dn2CppObject*, Dn2CppObject*), int32_t options, Dn2CppCancelSource* source)
{
    dn2cpp_task_require_delegate(del, "action");
    Dn2CppTask* t = dn2cpp_task_alloc(options, source);
    t->startKind = DN2CPP_TASK_ORIGIN_COLD;
    dn2cpp_gc_store_ref(&t->asyncState, state);
    if (source != nullptr && dn2cpp_cts_is_cancelled(source))
    {
        dn2cpp_task_set_canceled(t, source);
        return t;
    }
    auto* c = static_cast<Dn2CppTaskCold*>(dn2cpp_alloc(sizeof(Dn2CppTaskCold)));
    c->scheduler = nullptr;
    c->customQueue = nullptr;
    dn2cpp_gc_store_ref(&c->del, del);
    dn2cpp_gc_store_ref(&c->state, state);
    c->invoke = invoke;
    c->invoke2 = invoke2;
    dn2cpp_gc_store_ref(&t->cold, c);
    if (source != nullptr)
        dn2cpp_register_cold_cancellation(t, source);
    return t;
}

Dn2CppTask* dn2cpp_task_cold_void(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_void, nullptr, options, source); }
Dn2CppTask* dn2cpp_task_cold_i4(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_i4, nullptr, options, source); }
Dn2CppTask* dn2cpp_task_cold_i8(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_i8, nullptr, options, source); }
Dn2CppTask* dn2cpp_task_cold_r4(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_r4, nullptr, options, source); }
Dn2CppTask* dn2cpp_task_cold_r8(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_r8, nullptr, options, source); }
Dn2CppTask* dn2cpp_task_cold_ref(Dn2CppObject* del, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, nullptr, &dn2cpp_run_ref, nullptr, options, source); }
Dn2CppTask* dn2cpp_task_cold_struct(Dn2CppObject* del, uint64_t (*invoke)(Dn2CppObject*), int32_t options, Dn2CppCancelSource* source)
{
    return dn2cpp_task_cold(del, nullptr, invoke, nullptr, options, source);
}
Dn2CppTask* dn2cpp_task_cold_void_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source)
{
    return dn2cpp_task_cold(del, state, nullptr, &dn2cpp_run_void_state, options, source);
}
Dn2CppTask* dn2cpp_task_cold_i4_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, state, nullptr, &dn2cpp_run_i4_state, options, source); }
Dn2CppTask* dn2cpp_task_cold_i8_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, state, nullptr, &dn2cpp_run_i8_state, options, source); }
Dn2CppTask* dn2cpp_task_cold_r4_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, state, nullptr, &dn2cpp_run_r4_state, options, source); }
Dn2CppTask* dn2cpp_task_cold_r8_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, state, nullptr, &dn2cpp_run_r8_state, options, source); }
Dn2CppTask* dn2cpp_task_cold_ref_state(Dn2CppObject* del, Dn2CppObject* state, int32_t options, Dn2CppCancelSource* source) { return dn2cpp_task_cold(del, state, nullptr, &dn2cpp_run_ref_state, options, source); }
Dn2CppTask* dn2cpp_task_cold_struct_state(Dn2CppObject* del, Dn2CppObject* state,
    uint64_t (*invoke)(Dn2CppObject*, Dn2CppObject*), int32_t options, Dn2CppCancelSource* source)
{
    return dn2cpp_task_cold(del, state, nullptr, invoke, options, source);
}

// Claim a cold task's unstarted work exactly once, under g_task_mtx (the same
// lock that serializes completion/await-registration, so two racing Starts see
// one winner). The origin distinguishes promise, continuation and started tasks.
static Dn2CppTaskCold* dn2cpp_task_claim_cold(Dn2CppTask* t, bool synchronous,
                                               Dn2CppObject* scheduler, const Dn2CppTaskSchedulerQueue* queue = nullptr)
{
    if (synchronous && scheduler == nullptr)
        dn2cpp_throw_argument_null_param("scheduler");
    dn2cpp_null_check(t);
    int32_t status = t->status.load(std::memory_order_acquire);
    if (!synchronous && status != DN2CPP_TASK_PENDING)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type, DN2CPP_SR_TASK_START_COMPLETED);
    if (!synchronous && scheduler == nullptr)
        dn2cpp_throw_argument_null_param("scheduler");
    const int32_t origin = t->startKind.load(std::memory_order_acquire);
    if (origin == DN2CPP_TASK_ORIGIN_CONTINUATION || origin == DN2CPP_TASK_ORIGIN_ACTIVE_CONTINUATION)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type,
            synchronous ? DN2CPP_SR_TASK_SYNC_CONTINUATION : DN2CPP_SR_TASK_START_CONTINUATION);
    if (origin == DN2CPP_TASK_ORIGIN_PROMISE || origin == DN2CPP_TASK_ORIGIN_ASYNC_UNEXPOSED)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type,
            synchronous ? DN2CPP_SR_TASK_SYNC_PROMISE : DN2CPP_SR_TASK_START_PROMISE);
    if (synchronous && status != DN2CPP_TASK_PENDING)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type, DN2CPP_SR_TASK_SYNC_COMPLETED);
    Dn2CppTaskCold* c;
    bool marked = false;
    bool dispatchedUnowned;
    bool finishing;
    {
        std::unique_lock<std::mutex> lk(g_task_mtx);
        dispatchedUnowned = t->startKind == DN2CPP_TASK_ORIGIN_UNOWNED_QUEUE;
        finishing = (t->finishProvenance.load(std::memory_order_relaxed) & DN2CPP_TASK_EXPLICIT_FINISH) != 0;
        c = finishing ? nullptr : t->cold;
        if (c != nullptr && c->scheduler != nullptr)
            c = nullptr;
        if (c != nullptr)
        {
            if (!synchronous && scheduler != dn2cpp_taskscheduler_default())
            {
                auto slots = scheduler->type->vtable;
                if (queue == nullptr || slots == nullptr || slots[queue->queueSlot] == nullptr
                    || slots[queue->dequeueSlot] == nullptr)
                {
                    lk.unlock();
                    dn2cpp_throw_platform_not_supported("Task.Start requires its custom scheduler queue slots");
                }
                c->customQueue = queue;
            }
            if (scheduler == dn2cpp_taskscheduler_default())
                t->execution.fetch_or(DN2CPP_TASK_DEFAULT_OWNER);
            else
            {
                dn2cpp_gc_store_ref(&t->workerKeepAlive, static_cast<void*>(scheduler));
                t->execution.fetch_or(DN2CPP_TASK_CUSTOM_OWNER);
            }
            dn2cpp_gc_store_ref(&c->scheduler, scheduler);
            marked = dn2cpp_task_mark_started_locked(t);
            // A losing start claim keeps its never-enqueued delegate and thunk.
            if (marked && c->customQueue == nullptr)
                dn2cpp_gc_store_ref(&t->cold, static_cast<Dn2CppTaskCold*>(nullptr));
        }
    }
    if (finishing)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type,
            synchronous ? DN2CPP_SR_TASK_SYNC_COMPLETED : DN2CPP_SR_TASK_START_COMPLETED);
    if (dispatchedUnowned)
        dn2cpp_throw_platform_not_supported("Starting an already dispatched unowned task requires managed scheduler queue coordination");
    if (c == nullptr)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type,
            synchronous ? DN2CPP_SR_TASK_SYNC_ALREADY : DN2CPP_SR_TASK_START_ALREADY);
    // The scheduler claim succeeds even when MarkStarted independently lost.
    // Start returns without queuing; RunSynchronously reports a completed task.
    if (!marked && synchronous)
        dn2cpp_throw_sr0(&dn2cpp_invalid_operation_exception_type, DN2CPP_SR_TASK_SYNC_COMPLETED);
    return marked ? c : nullptr;
}

void dn2cpp_task_start(Dn2CppTask* t, Dn2CppObject* scheduler)
{
    dn2cpp_task_start(t, scheduler, nullptr);
}

void dn2cpp_task_start(Dn2CppTask* t, Dn2CppObject* scheduler, const Dn2CppTaskSchedulerQueue* queue)
{
    Dn2CppTaskCold* c = dn2cpp_task_claim_cold(t, false, scheduler, queue);
    if (c == nullptr) return;
    if (c->customQueue == nullptr)
    {
        dn2cpp_pool_enqueue(t, c->del, c->state, c->invoke, c->invoke2);
        return;
    }
    using QueueTask = void (*)(Dn2CppObject*, Dn2CppTask*);
    auto submit = reinterpret_cast<QueueTask>(const_cast<void*>(scheduler->type->vtable[queue->queueSlot]));
    try
    {
        submit(scheduler, t);
    }
    catch (const Dn2CppException& e)
    {
        auto* wrapped = queue->wrapException(e.obj);
        dn2cpp_exc_inflight_pop(e.obj);
        {
            std::lock_guard<std::mutex> lock(g_task_mtx);
            if (t->status == DN2CPP_TASK_PENDING && (t->execution & DN2CPP_TASK_INVOKED))
                t->execution.fetch_or(DN2CPP_TASK_QUEUE_REJECTED_WHILE_RUNNING);
            dn2cpp_task_release_action_locked(t);
        }
        if (!dn2cpp_task_await_winner(t, dn2cpp_task_finish_recorded(t, DN2CPP_TASK_FAULTED, 0, wrapped)))
        {
            // QueueTask may execute or cancel the delegate before throwing. Its
            // rejection appends to any delegate faults after continuations have fired.
            if (t->status != DN2CPP_TASK_FAULTED)
                dn2cpp_task_finish_recorded(t, DN2CPP_TASK_FAULTED, 0, wrapped, nullptr, nullptr,
                    false, nullptr, Dn2CppTaskFinishKind::RejectedAfterCompletion);
            else
                dn2cpp_task_append_terminal_fault(t, wrapped);
            dn2cpp_throw_sr0(&dn2cpp_invalid_cast_exception_type, DN2CPP_SR_INVALID_CAST);
        }
        if (auto* holder = dn2cpp_task_holder_for_observation(t))
        {
            Dn2CppMonitorGuard guard(holder);
            g_task_holder_observe(holder);
        }
        dn2cpp_throw(wrapped);
    }
}

static void dn2cpp_task_invoke_cold(Dn2CppTask* t, Dn2CppTaskCold* c)
{
    // Inline delegate failures settle the task; Wait/Result/await observes them.
    Dn2CppCancelSource* source = t->cancellationSource.load(std::memory_order_acquire);
    if (source != nullptr && dn2cpp_cts_is_cancelled(source))
    {
        dn2cpp_task_set_canceled(t, source);
        return;
    }
    Dn2CppTaskDelegateScope scope(t, c->customQueue != nullptr ? c->scheduler : nullptr);
    uint64_t result = 0;
    Dn2CppObject* exception = nullptr;
    try
    {
        result = c->invoke2 != nullptr ? c->invoke2(c->del, c->state) : c->invoke(c->del);
    }
    catch (const Dn2CppException& e)
    {
        exception = e.obj;
        dn2cpp_exc_inflight_pop(e.obj);
    }
    dn2cpp_task_finish_cold_delegate(t, result, exception);
}

Dn2CppObject* dn2cpp_task_executing_scheduler(Dn2CppTask* task)
{
    dn2cpp_null_check(task);
    auto flags = task->execution.load(std::memory_order_acquire);
    if (flags & DN2CPP_TASK_CUSTOM_OWNER) return static_cast<Dn2CppObject*>(task->workerKeepAlive);
    auto origin = task->startKind.load(std::memory_order_acquire);
    if ((flags & DN2CPP_TASK_DEFAULT_OWNER) || origin == DN2CPP_TASK_ORIGIN_POOL || origin == DN2CPP_TASK_ORIGIN_ACTIVE_CONTINUATION)
        return dn2cpp_taskscheduler_default();
    return nullptr;
}

int32_t dn2cpp_task_execute_entry(Dn2CppTask* task)
{
    Dn2CppTaskCold* work;
    {
        std::unique_lock<std::mutex> lock(g_task_mtx);
        auto flags = task->execution.load(std::memory_order_relaxed);
        if (task->status == DN2CPP_TASK_CANCELED || (flags & DN2CPP_TASK_CANCELED_BEFORE_REJECTION)) return true;
        if (task->status != DN2CPP_TASK_PENDING || (flags & DN2CPP_TASK_INVOKED)) return false;
        work = task->cold;
        if (work == nullptr && task->startKind != DN2CPP_TASK_ORIGIN_PROMISE)
        {
            lock.unlock();
            dn2cpp_throw_platform_not_supported("Task execution requires a retained delegate");
        }
        task->execution.fetch_or(DN2CPP_TASK_INVOKED);
        // A cancellation racing the running delegate still calls the assigned
        // scheduler's TryDequeue. Completion releases the retained queue data.
    }
    if (work != nullptr)
        dn2cpp_task_invoke_cold(task, work);
    else
        dn2cpp_task_finish_recorded(task, DN2CPP_TASK_SUCCEEDED, task->result, nullptr);
    return true;
}

void dn2cpp_task_run_synchronously(Dn2CppTask* t, Dn2CppObject* scheduler)
{
    Dn2CppTaskCold* c = dn2cpp_task_claim_cold(t, true, scheduler);
    t->execution.fetch_or(DN2CPP_TASK_INVOKED);
    dn2cpp_task_invoke_cold(t, c);
}

int32_t dn2cpp_taskscheduler_try_run_inline(Dn2CppObject*, Dn2CppTask* task, int32_t previouslyQueued)
{
    // The managed body reads the task first and delegates to its assigned owner.
    // A null receiver is rejected by callvirt, not by a direct call's body.
    dn2cpp_null_check(task);
    Dn2CppTaskCold* work = nullptr;
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        int32_t origin = task->startKind.load(std::memory_order_relaxed);
        if ((task->finishProvenance.load(std::memory_order_relaxed) & DN2CPP_TASK_EXPLICIT_FINISH)
            || task->status == DN2CPP_TASK_CANCELED || (task->execution & DN2CPP_TASK_INVOKED)
            || origin == DN2CPP_TASK_ORIGIN_PROMISE || origin == DN2CPP_TASK_ORIGIN_ASYNC_UNEXPOSED
            || origin == DN2CPP_TASK_ORIGIN_PRECOMPLETED || origin == DN2CPP_TASK_ORIGIN_CONTINUATION
            || origin == DN2CPP_TASK_ORIGIN_UNOWNED_QUEUE)
            return false;
        if (origin == DN2CPP_TASK_ORIGIN_COLD && task->cold != nullptr)
        {
            auto* cold = task->cold;
            if (cold->scheduler == nullptr)
                return false;
            if (cold->scheduler == dn2cpp_taskscheduler_default())
            {
                // A retained cold node has never entered a queue. Only a false
                // previouslyQueued claim can dispatch it, exactly once.
                if (previouslyQueued)
                    return false;
                work = cold;
                task->execution.fetch_or(DN2CPP_TASK_INVOKED);
                dn2cpp_gc_store_ref(&task->cold, static_cast<Dn2CppTaskCold*>(nullptr));
            }
        }
    }
    if (work == nullptr)
        dn2cpp_throw_platform_not_supported("Managed TaskScheduler inline execution requires scheduler-owned task dispatch");
    dn2cpp_task_invoke_cold(task, work);
    return true;
}

void dn2cpp_taskscheduler_internal_queue(Dn2CppObject* scheduler, Dn2CppTask* task)
{
    dn2cpp_null_check(scheduler);
    if (scheduler != dn2cpp_taskscheduler_default())
        dn2cpp_throw_platform_not_supported("Queuing on a custom TaskScheduler requires managed QueueTask dispatch");
    dn2cpp_null_check(task);
#ifdef __EMSCRIPTEN__
    dn2cpp_throw_platform_not_supported("TaskScheduler queuing requires worker threads");
#else
    if (task->creationOptions & DN2CPP_TASK_LONG_RUNNING)
        dn2cpp_throw_platform_not_supported("LongRunning TaskScheduler work requires a dedicated background thread");
    auto* source = task->cancellationSource.load(std::memory_order_acquire);
    bool cancellationRequested = source != nullptr && dn2cpp_cts_is_cancelled(source);
    Dn2CppTaskCold* work = nullptr;
    Dn2CppCont* conts = nullptr;
    bool canceled = false;
    bool foreignOwner = false;
    {
        std::lock_guard<std::mutex> lock(g_task_mtx);
        int32_t origin = task->startKind.load(std::memory_order_relaxed);
        // A canceled cold task has no delegate left to dispatch or cancellation to publish.
        if (task->status == DN2CPP_TASK_CANCELED
            && (origin == DN2CPP_TASK_ORIGIN_COLD || origin == DN2CPP_TASK_ORIGIN_UNOWNED_QUEUE))
            return;
        if (origin == DN2CPP_TASK_ORIGIN_COLD
            && (task->finishProvenance.load(std::memory_order_relaxed) & DN2CPP_TASK_EXPLICIT_FINISH) == 0)
        {
            work = task->cold;
            if (work != nullptr)
            {
                foreignOwner = work->scheduler != nullptr && work->scheduler != scheduler;
                if (!foreignOwner)
                {
                    // A protected start prevents pre-dispatch cancellation; the worker then observes the token.
                    canceled = cancellationRequested && dn2cpp_task_try_complete_locked(task, DN2CPP_TASK_CANCELED,
                        0, nullptr, nullptr, conts, source, true);
                    if (!canceled && work->scheduler == nullptr)
                        task->startKind = DN2CPP_TASK_ORIGIN_UNOWNED_QUEUE;
                    dn2cpp_gc_store_ref(&task->cold, static_cast<Dn2CppTaskCold*>(nullptr));
                }
            }
        }
    }
    if (foreignOwner)
        dn2cpp_throw_platform_not_supported("Queued task execution requires its assigned custom scheduler context");
    if (work == nullptr)
        dn2cpp_throw_platform_not_supported("TaskScheduler queuing requires an undispatched native task delegate");
    if (canceled)
    {
        dn2cpp_fire_conts(conts);
        return;
    }
    // QueueTask is downstream of scheduler assignment and MarkStarted. Neither belongs here.
    dn2cpp_pool_enqueue(task, work->del, work->state, work->invoke, work->invoke2);
#endif
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
    uint64_t (*invokeState)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*);
};

// The antecedent can still own a registration for this continuation. Clear the
// delegate there as well, without clearing the continuation task's public state.
static void dn2cpp_task_release_action_locked(Dn2CppTask* t)
{
    dn2cpp_gc_store_ref(&t->cold, static_cast<Dn2CppTaskCold*>(nullptr));
    if (t->startKind == DN2CPP_TASK_ORIGIN_CONTINUATION && t->workerKeepAlive != nullptr)
    {
        auto* continuation = static_cast<Dn2CppContWith*>(t->workerKeepAlive);
        dn2cpp_gc_store_ref(&continuation->del, static_cast<Dn2CppObject*>(nullptr));
    }
}

// The ContinueWith result-kind thunks: the two-argument mirror of the dn2cpp_run_*
// family above — the same prev-first multicast walk and result packing, with the
// settled antecedent as every handler's argument. The void kinds get their walk from
// dn2cpp_paramthread_invoke.
static uint64_t dn2cpp_contwith_i4(Dn2CppObject* del, Dn2CppObject* ante)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_contwith_i4(d->prev, ante);
    return static_cast<uint64_t>(static_cast<uint32_t>(
        reinterpret_cast<int32_t (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante)));
}
static uint64_t dn2cpp_contwith_i8(Dn2CppObject* del, Dn2CppObject* ante)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_contwith_i8(d->prev, ante);
    return static_cast<uint64_t>(
        reinterpret_cast<int64_t (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante));
}
static uint64_t dn2cpp_contwith_r4(Dn2CppObject* del, Dn2CppObject* ante)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_contwith_r4(d->prev, ante);
    return dn2cpp_r8_bits(static_cast<double>(
        reinterpret_cast<float (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante)));
}
static uint64_t dn2cpp_contwith_r8(Dn2CppObject* del, Dn2CppObject* ante)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_contwith_r8(d->prev, ante);
    return dn2cpp_r8_bits(
        reinterpret_cast<double (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante));
}
static uint64_t dn2cpp_contwith_ref(Dn2CppObject* del, Dn2CppObject* ante)
{
    auto* d = reinterpret_cast<Dn2CppDelegate*>(del);
    if (d->prev != nullptr)
        dn2cpp_contwith_ref(d->prev, ante);
    return static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
        reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*, Dn2CppObject*)>(d->method)(d->target, ante)));
}

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

static void dn2cpp_cont_with_cancel(void* p)
{
    auto* c = static_cast<Dn2CppContWith*>(p);
    auto* source = c->task->cancellationSource.load(std::memory_order_acquire);
    Dn2CppCont* conts = nullptr;
    // A token-bearing filter uses protected cancellation; the default-token
    // initial-state path cancels even when a start claim was already taken.
    if (dn2cpp_task_try_complete_deferred(c->task, DN2CPP_TASK_CANCELED, 0,
        nullptr, nullptr, conts, source, source != nullptr))
        dn2cpp_fire_conts(conts);
}

// The antecedent is settled. Claim eligible work before it can be observed on
// the queue. Filtered tasks take no start claim and settle in the bounded drain.
static bool dn2cpp_cont_with_prepare(Dn2CppCont* node)
{
    auto* c = static_cast<Dn2CppContWith*>(node->state);
    if (dn2cpp_cont_with_filtered_out(c->options, c->antecedent->status))
    {
        node->owner = nullptr;
        node->fn = &dn2cpp_cont_with_cancel;
        return true;
    }
    // Filtering leaves the continuation unassigned. For eligible work the
    // scheduler is assigned even if a separate protected start already won.
    std::lock_guard<std::mutex> lock(g_task_mtx);
    if (c->task->finishProvenance.load(std::memory_order_relaxed) & DN2CPP_TASK_EXPLICIT_FINISH)
        return false;
    c->task->startKind = DN2CPP_TASK_ORIGIN_ACTIVE_CONTINUATION;
    return dn2cpp_task_mark_started_locked(c->task);
}

static void dn2cpp_cont_with_run(void* p)
{
    auto* c = static_cast<Dn2CppContWith*>(p);
    c->task->execution.fetch_or(DN2CPP_TASK_INVOKED);
    auto* ante = reinterpret_cast<Dn2CppObject*>(c->antecedent);
    dn2cpp_gc_store_ref(&c->antecedent, static_cast<Dn2CppTask*>(nullptr));
    // Before invocation the task owns the continuation state, including its
    // antecedent. The running frame now owns that state until it returns.
    dn2cpp_gc_store_ref(&c->task->workerKeepAlive, static_cast<void*>(nullptr));
    try
    {
        uint64_t r = 0;
        dn2cpp_task_invoke_as(c->task, [&]
        {
            if (c->invokeState != nullptr)
                r = c->invokeState(c->del, ante, c->state);
            else switch (c->kind)
            {
                case DN2CPP_CONTWITH_VOID: // Action<Task> (multicast-aware)
                    dn2cpp_paramthread_invoke(c->del, ante);
                    break;
                case DN2CPP_CONTWITH_VOID_STATE: // Action<Task, object?>
                    dn2cpp_paramthread_invoke_state(c->del, ante, c->state);
                    break;
                case DN2CPP_CONTWITH_I4:
                    r = dn2cpp_contwith_i4(c->del, ante);
                    break;
                case DN2CPP_CONTWITH_I8:
                    r = dn2cpp_contwith_i8(c->del, ante);
                    break;
                case DN2CPP_CONTWITH_R4:
                    r = dn2cpp_contwith_r4(c->del, ante);
                    break;
                case DN2CPP_CONTWITH_R8:
                    r = dn2cpp_contwith_r8(c->del, ante);
                    break;
                case DN2CPP_CONTWITH_STRUCT: // Func<Task, TStruct> — the trampoline boxes the result
                    r = c->invokeStruct(c->del, ante);
                    break;
                default: // DN2CPP_CONTWITH_REF
                    r = dn2cpp_contwith_ref(c->del, ante);
                    break;
            }
        });
        dn2cpp_task_finish_recorded(c->task, DN2CPP_TASK_SUCCEEDED, r, nullptr);
    }
    catch (const Dn2CppException& e)
    {
        dn2cpp_task_finish_recorded(c->task, DN2CPP_TASK_FAULTED, c->task->result, e.obj);
        dn2cpp_exc_inflight_pop(e.obj);
    }
}

Dn2CppTask* dn2cpp_task_continue_with(Dn2CppTask* t, Dn2CppObject* del, Dn2CppObject* state,
                                      int32_t kind, int32_t options, Dn2CppCancelSource* source, bool needsOwner)
{
    dn2cpp_null_check(t);
    Dn2CppTask* ct = dn2cpp_task_alloc(options, source);
    ct->startKind = DN2CPP_TASK_ORIGIN_CONTINUATION;
    dn2cpp_gc_store_ref(&ct->asyncState, state);
    auto* c = static_cast<Dn2CppContWith*>(dn2cpp_alloc(sizeof(Dn2CppContWith)));
    dn2cpp_gc_store_ref(&c->antecedent, t);
    dn2cpp_gc_store_ref(&c->task, ct);
    dn2cpp_gc_store_ref(&c->del, del);
    dn2cpp_gc_store_ref(&c->state, state);
    c->kind = kind;
    c->options = options;
    c->invokeStruct = nullptr;
    c->invokeState = nullptr;
    dn2cpp_gc_store_ref(&ct->workerKeepAlive, static_cast<void*>(c));
    dn2cpp_task_queue_cont(t, &dn2cpp_cont_with_run, c, needsOwner);
    return ct;
}

Dn2CppTask* dn2cpp_task_continue_with_state_result(Dn2CppTask* t, Dn2CppObject* del,
    Dn2CppObject* state, uint64_t (*invoke)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*), int32_t options,
    Dn2CppCancelSource* source, bool needsOwner)
{
    dn2cpp_null_check(t);
    Dn2CppTask* task = dn2cpp_task_alloc(options, source);
    task->startKind = DN2CPP_TASK_ORIGIN_CONTINUATION;
    dn2cpp_gc_store_ref(&task->asyncState, state);
    auto* continuation = static_cast<Dn2CppContWith*>(dn2cpp_alloc(sizeof(Dn2CppContWith)));
    dn2cpp_gc_store_ref(&continuation->antecedent, t);
    dn2cpp_gc_store_ref(&continuation->task, task);
    dn2cpp_gc_store_ref(&continuation->del, del);
    dn2cpp_gc_store_ref(&continuation->state, state);
    continuation->kind = DN2CPP_CONTWITH_STRUCT;
    continuation->options = options;
    continuation->invokeStruct = nullptr;
    continuation->invokeState = invoke;
    dn2cpp_gc_store_ref(&task->workerKeepAlive, static_cast<void*>(continuation));
    dn2cpp_task_queue_cont(t, &dn2cpp_cont_with_run, continuation, needsOwner);
    return task;
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
    dn2cpp_task_forward_completion(w->task, a, a->result);
}

Dn2CppTask* dn2cpp_task_wait_async(Dn2CppTask* t, Dn2CppCancelSource* src)
{
    Dn2CppTask* wt = dn2cpp_task_alloc();
    auto* w = static_cast<Dn2CppWaitAsync*>(dn2cpp_alloc(sizeof(Dn2CppWaitAsync)));
    dn2cpp_gc_store_ref(&w->antecedent, t);
    dn2cpp_gc_store_ref(&w->task, wt);
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
            dn2cpp_task_try_set_canceled(wt, src);
        }
        else
        {
            auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
            r->source = src;
            r->task = wt;
            r->delay = false;
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
                dn2cpp_task_try_set_canceled(wt, src);
        }
    }
    dn2cpp_task_on_completed(t, &dn2cpp_wait_async_settle, w);
    return wt;
}

struct Dn2CppTimedWaitAsync
{
    Dn2CppTask* antecedent;
    Dn2CppTask* task;
    Dn2CppTimer* timer;
    Dn2CppCancelReg* registration;
};

static void dn2cpp_wait_async_timeout_settle(void* p)
{
    auto* w = static_cast<Dn2CppTimedWaitAsync*>(p);
    Dn2CppTask* a = w->antecedent;
    dn2cpp_task_forward_completion(w->task, a, a->result, true);
}

static void dn2cpp_wait_async_timeout_expire(void* p)
{
    auto* task = static_cast<Dn2CppTask*>(p);
    if (task->status != DN2CPP_TASK_PENDING)
        return;
    dn2cpp_task_try_set_exception(task, dn2cpp_exception_new(&dn2cpp_timeout_exception_type,
        dn2cpp_default_message(&dn2cpp_timeout_exception_type), nullptr));
}

static void dn2cpp_wait_async_cleanup(void* p)
{
    auto* w = static_cast<Dn2CppTimedWaitAsync*>(p);
    dn2cpp_task_remove_continuation(w->antecedent, &dn2cpp_wait_async_timeout_settle, w);
    dn2cpp_task_timer_cancel(w->timer);
    dn2cpp_cts_unregister(w->registration);
}

Dn2CppTask* dn2cpp_task_wait_async_timeout(Dn2CppTask* t, int64_t ticks,
    Dn2CppCancelSource* src, const Dn2CppTypeInfo* resultType)
{
    int64_t ms = static_cast<int64_t>(static_cast<double>(ticks) / 10000.0);
    if (ms < -1 || ms > INT64_C(0xFFFFFFFE))
        dn2cpp_throw_argument_out_of_range_param(DN2CPP_SR_TASK_DELAY_SPAN, "timeout");
    dn2cpp_null_check(t);
    if (t->status != DN2CPP_TASK_PENDING || (ms == -1 && src == nullptr))
        return t;
    if (src != nullptr && dn2cpp_cts_is_cancelled(src))
        return dn2cpp_task_stamp(dn2cpp_task_from_canceled(src), resultType);
    Dn2CppTask* task = dn2cpp_task_stamp(dn2cpp_task_alloc(), resultType);
    if (ms == 0)
    {
        dn2cpp_wait_async_timeout_expire(task);
        return task;
    }
    auto* w = static_cast<Dn2CppTimedWaitAsync*>(dn2cpp_alloc(sizeof(Dn2CppTimedWaitAsync)));
    dn2cpp_gc_store_ref(&w->antecedent, t);
    dn2cpp_gc_store_ref(&w->task, task);
    if (ms > 0)
    {
        dn2cpp_gc_store_ref(&w->timer, dn2cpp_task_timer_new(dn2cpp_task_alloc(), ms));
        dn2cpp_task_on_completed_sync(w->timer->task, &dn2cpp_wait_async_timeout_expire, task);
    }
    if (src != nullptr)
    {
        auto* r = static_cast<Dn2CppCancelReg*>(dn2cpp_alloc(sizeof(Dn2CppCancelReg)));
        dn2cpp_gc_store_ref(&r->source, src);
        dn2cpp_gc_store_ref(&r->task, task);
        dn2cpp_gc_store_ref(&w->registration, r);
    }
    // All cleanup handles are immutable before any callback can settle task.
    dn2cpp_task_on_completed_sync(task, &dn2cpp_wait_async_cleanup, w);
    dn2cpp_task_on_completed_sync(t, &dn2cpp_wait_async_timeout_settle, w);
    if (src != nullptr && task->status == DN2CPP_TASK_PENDING)
    {
        Dn2CppCancelReg* r = w->registration;
        bool canceled;
        {
            std::lock_guard<std::mutex> lk(g_cts_mtx);
            canceled = src->canceled != 0;
            if (!canceled)
            {
                dn2cpp_gc_store_ref(&r->next, src->regs);
                dn2cpp_gc_store_ref(&src->regs, r);
            }
        }
        if (canceled)
            dn2cpp_task_try_set_canceled(task, src);
    }
    if (w->timer != nullptr && task->status == DN2CPP_TASK_PENDING)
        dn2cpp_task_timer_schedule(w->timer);
    // A winning completion may have cleaned up before a registration was linked.
    if (task->status != DN2CPP_TASK_PENDING)
        dn2cpp_wait_async_cleanup(w);
    return task;
}

// ContinueWith with a value-type result: same continuation node, but the delegate is
// invoked through the transpiler-emitted boxing trampoline (DN2CPP_CONTWITH_STRUCT).
Dn2CppTask* dn2cpp_task_continue_with_struct(Dn2CppTask* t, Dn2CppObject* del,
                                             uint64_t (*invoke)(Dn2CppObject*, Dn2CppObject*),
                                             int32_t options, Dn2CppCancelSource* source, bool needsOwner)
{
    dn2cpp_null_check(t);
    Dn2CppTask* ct = dn2cpp_task_alloc(options, source);
    ct->startKind = DN2CPP_TASK_ORIGIN_CONTINUATION;
    auto* c = static_cast<Dn2CppContWith*>(dn2cpp_alloc(sizeof(Dn2CppContWith)));
    dn2cpp_gc_store_ref(&c->antecedent, t);
    dn2cpp_gc_store_ref(&c->task, ct);
    dn2cpp_gc_store_ref(&c->del, del);
    c->state = nullptr;
    c->kind = DN2CPP_CONTWITH_STRUCT;
    c->options = options;
    c->invokeStruct = invoke;
    c->invokeState = nullptr;
    dn2cpp_gc_store_ref(&ct->workerKeepAlive, static_cast<void*>(c));
    dn2cpp_task_queue_cont(t, &dn2cpp_cont_with_run, c, needsOwner);
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
    if (callback == nullptr)
        dn2cpp_throw_argument_null_param("callBack");
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
    dn2cpp_ti_with_typeobject({ "dn2cpp.WorkItemInvoker", nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppWorkItemInvoker), 0, 0, 0, 0, 0, nullptr },
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
    dn2cpp_gc_store_ref(&inv->wi, wi);
    inv->fn = executeFn;
    auto* del = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    del->type = &dn2cpp_workitem_invoker_type; // opaque header; only {target, method} are read
    dn2cpp_gc_store_ref(&del->target, &inv->header);
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
    dn2cpp_rethrow(target); // the pool reports this fault and terminates the process
}

void dn2cpp_task_throw_async(Dn2CppObject* exc, Dn2CppObject* /*syncCtx*/)
{
    if (exc == nullptr)
        return;
    auto* del = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    del->type = &dn2cpp_workitem_invoker_type; // opaque header; only {target, method, prev} read
    dn2cpp_gc_store_ref(&del->target, exc);
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
// RandomAccess' ThreadPoolValueTaskSource read/write scheduler returns. The dn2cpp
// ValueTask is always a {task} struct, so the ctor fronts the source with a pending
// Dn2CppTask and keeps the CLR's consumption protocol over it:
//   * Until something needs a completion signal, the source answers directly: status
//     reads call GetStatus and a synchronous read calls GetResult, so a pending source
//     refuses with its own exception.
//   * The first await suspension, task continuation, blocking wait or AsTask arms the
//     bridge the way ValueTask.AsTask does: a completed source is read on the spot, a
//     pending one gets OnCompleted. From then on the task is the only completion signal
//     and only the registered continuation calls the source. A producer may still read
//     the continuation slot after it has reported completion, so a consumer that read,
//     and so recycled, the source after registration could hand that producer the next
//     operation's continuation.
//   * A registration the source rejects by throwing throws to whoever armed the bridge,
//     except at a BCL builder's await suspension, which re-raises it through
//     Task.ThrowAsync (dn2cpp_async_await_on_completed).
//   * A source that stores the continuation and then rejects the registration by
//     throwing leaves an orphan, as a failed ValueTask.AsTask does. Like .NET's, the
//     orphan reads the source once more: a GetStatus failure reaches whoever runs it, and
//     the GetResult outcome is dropped. It never settles the task or leaves the in-flight
//     count, and a read that consumes the operation without a registration stores
//     CONSUMED, which no continuation can claim.
// So each result the bridge delivers is read from the source once, and an orphan's
// extra read delivers nothing; after a pending source refuses a read, the next read
// still goes to the source.
// The continuation is a runtime-built delegate stamped with the real Action<object>
// type-info so both invoke paths work (the source's own compiled continuation-invoke IL
// and the pool's fire-and-forget requeue). OnCompleted is called with flags None, so no
// ExecutionContext or scheduling context is captured.
// Rooting: the task holds the bridge and the bridge holds the source and the task; once
// registered, the source stores delegate+state in its own scanned fields. The task counts
// in g_inflight_async_tasks from registration until the continuation runs, so an awaiting
// task_block sleeps instead of deadlock-failing.
enum : int32_t
{
    DN2CPP_VTS_DIRECT = 0, // nothing registered: consumers call the source themselves
    DN2CPP_VTS_HELD = 1,   // one consumer is inside a direct source call
    DN2CPP_VTS_TASK = 2,   // registered: the task is the only signal
    DN2CPP_VTS_RAN = 3,    // the registered continuation has run
    DN2CPP_VTS_CONSUMED = 4, // read with nothing registered: the task has the outcome
};

struct Dn2CppVtsBridge
{
    Dn2CppObject header;
    Dn2CppObject* vts;      // the IValueTaskSource receiver
    Dn2CppTask* task;       // the pending task the ValueTask wraps
    const void* getStatusFn; // resolved GetStatus impl: int32_t (*)(receiver, int32_t)
    const void* getResultFn; // resolved GetResult impl: R (*)(receiver, int32_t)
    const void* onCompletedFn; // resolved OnCompleted impl
    const Dn2CppTypeInfo* actionTi; // Action<object>, stamped on the continuation delegate
    int16_t version;        // the source's token for this operation
    int32_t resultKind;     // 0=void 1=int32 2=int64 3=reference 4=struct
    uint64_t (*getStructResult)(const void*, Dn2CppObject*, int16_t);
    std::atomic<int32_t> state; // DN2CPP_VTS_*
};

extern const Dn2CppType dn2cpp_vts_bridge_type_obj;
static const Dn2CppTypeInfo dn2cpp_vts_bridge_type =
    dn2cpp_ti_with_typeobject({ "dn2cpp.ValueTaskSourceBridge", nullptr, nullptr, nullptr, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, (int32_t)sizeof(Dn2CppVtsBridge), 0, 0, 0, 0, 0, nullptr }, &dn2cpp_vts_bridge_type_obj);
const Dn2CppType dn2cpp_vts_bridge_type_obj = { { &dn2cpp_type_type }, &dn2cpp_vts_bridge_type };

static int32_t dn2cpp_vts_get_status(Dn2CppVtsBridge* b)
{
    // ValueTaskSourceStatus and DN2CPP_TASK_* share Pending/Succeeded/Faulted/Canceled
    // values 0..3. Returning the managed enum raw keeps one comparison contract.
    return reinterpret_cast<int32_t (*)(Dn2CppObject*, int32_t)>(
        const_cast<void*>(b->getStatusFn))(b->vts, b->version);
}

// Takes the source for direct calls, or answers false once the task is the only signal.
// A hold spans a GetStatus and at most one GetResult, which do not block by contract, so
// waiting out another consumer's hold is bounded.
static bool dn2cpp_vts_hold(Dn2CppVtsBridge* b)
{
    for (;;)
    {
        int32_t expected = DN2CPP_VTS_DIRECT;
        if (b->state.compare_exchange_strong(expected, DN2CPP_VTS_HELD,
                std::memory_order_acq_rel, std::memory_order_acquire))
            return true;
        if (expected != DN2CPP_VTS_HELD)
            return false;
        std::this_thread::yield();
    }
}

// The source's GetResult, packed into the task's 8-byte slot by kind.
static uint64_t dn2cpp_vts_get_result(Dn2CppVtsBridge* b)
{
    switch (b->resultKind)
    {
        case 0:
            reinterpret_cast<void (*)(Dn2CppObject*, int32_t)>(
                const_cast<void*>(b->getResultFn))(b->vts, b->version);
            return 0;
        case 1:
            return static_cast<uint64_t>(static_cast<uint32_t>(
                reinterpret_cast<int32_t (*)(Dn2CppObject*, int32_t)>(
                    const_cast<void*>(b->getResultFn))(b->vts, b->version)));
        case 2:
            return static_cast<uint64_t>(
                reinterpret_cast<int64_t (*)(Dn2CppObject*, int32_t)>(
                    const_cast<void*>(b->getResultFn))(b->vts, b->version));
        case 4:
            return b->getStructResult(b->getResultFn, b->vts, b->version);
        default:
            return static_cast<uint64_t>(reinterpret_cast<uintptr_t>(
                reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*, int32_t)>(
                    const_cast<void*>(b->getResultFn))(b->vts, b->version)));
    }
}

// Reads the source into the task. The caller owns the operation.
static void dn2cpp_vts_settle(Dn2CppVtsBridge* b)
{
    try
    {
        dn2cpp_task_set_result(b->task, dn2cpp_vts_get_result(b));
    }
    catch (const Dn2CppException& e)
    {
        dn2cpp_task_set_exception_or_canceled(b->task, e.obj); // rooted via the task
        dn2cpp_exc_inflight_pop(e.obj);
    }
}

static void dn2cpp_vts_continuation(Dn2CppObject* target, Dn2CppObject* /*state*/)
{
    auto* b = reinterpret_cast<Dn2CppVtsBridge*>(target);
    int32_t seen = DN2CPP_VTS_TASK;
    if (b->state.compare_exchange_strong(seen, DN2CPP_VTS_RAN, std::memory_order_acq_rel))
    {
        dn2cpp_vts_settle(b);
        dn2cpp_principal_left(g_inflight_async_tasks);
        return;
    }
    if (seen == DN2CPP_VTS_RAN)
        return; // once per registration
    // Finding DIRECT, HELD or CONSUMED means the source rejected this registration, so
    // this run is its orphan: the task and the in-flight count belong to other readers.
    dn2cpp_vts_get_status(b);
    try
    {
        dn2cpp_vts_get_result(b);
    }
    catch (const Dn2CppException& e)
    {
        dn2cpp_exc_inflight_pop(e.obj);
    }
}

// Gives the task its completion signal; a no-op once armed or consumed.
static void dn2cpp_vts_arm(Dn2CppTask* t)
{
    auto* b = reinterpret_cast<Dn2CppVtsBridge*>(t->vtsBridge);
    if (!dn2cpp_vts_hold(b))
        return;
    int32_t sourceStatus;
    Dn2CppDelegate* del = nullptr;
    try
    {
        sourceStatus = dn2cpp_vts_get_status(b);
        if (sourceStatus != DN2CPP_TASK_PENDING)
            dn2cpp_vts_settle(b); // registering would only have the source requeue it
        else // inside the try, so a throw while held still releases the hold
            del = static_cast<Dn2CppDelegate*>(dn2cpp_alloc(sizeof(Dn2CppDelegate)));
    }
    catch (...)
    {
        b->state.store(DN2CPP_VTS_DIRECT, std::memory_order_release);
        throw;
    }
    if (sourceStatus != DN2CPP_TASK_PENDING)
    {
        b->state.store(DN2CPP_VTS_CONSUMED, std::memory_order_release);
        return;
    }
    del->type = b->actionTi;
    dn2cpp_gc_store_ref(&del->target, &b->header);
    del->method = reinterpret_cast<void*>(&dn2cpp_vts_continuation);
    del->prev = nullptr;
    // Counted and published before OnCompleted, which may run the continuation
    // synchronously; the continuation leaves the count.
    g_inflight_async_tasks.fetch_add(1, std::memory_order_acq_rel);
    b->state.store(DN2CPP_VTS_TASK, std::memory_order_release);
    try
    {
        // OnCompleted(continuation, state, token, ValueTaskSourceOnCompletedFlags.None).
        // The state is the bridge itself, so the source's _continuationState field roots
        // it for the whole pending window.
        reinterpret_cast<void (*)(Dn2CppObject*, Dn2CppObject*, Dn2CppObject*, int32_t, int32_t)>(
            const_cast<void*>(b->onCompletedFn))(b->vts, del, &b->header, b->version, 0);
    }
    catch (...)
    {
        // A rejected registration leaves the operation to direct reads again.
        int32_t armed = DN2CPP_VTS_TASK;
        if (b->state.compare_exchange_strong(armed, DN2CPP_VTS_DIRECT, std::memory_order_acq_rel))
            dn2cpp_principal_left(g_inflight_async_tasks);
        throw;
    }
}

int32_t dn2cpp_vtask_status(Dn2CppTask* t)
{
    if (t == nullptr)
        return DN2CPP_TASK_SUCCEEDED;
    auto* b = reinterpret_cast<Dn2CppVtsBridge*>(t->vtsBridge);
    if (b == nullptr || !dn2cpp_vts_hold(b))
        return t->status.load(std::memory_order_seq_cst);
    int32_t status;
    try
    {
        status = dn2cpp_vts_get_status(b);
    }
    catch (...)
    {
        b->state.store(DN2CPP_VTS_DIRECT, std::memory_order_release);
        throw;
    }
    b->state.store(DN2CPP_VTS_DIRECT, std::memory_order_release);
    return status;
}

Dn2CppTask* dn2cpp_vts_block(Dn2CppTask* t)
{
    if (t->status.load(std::memory_order_seq_cst) != DN2CPP_TASK_PENDING)
        return t;
    auto* b = reinterpret_cast<Dn2CppVtsBridge*>(t->vtsBridge);
    if (b == nullptr || !dn2cpp_vts_hold(b))
        return dn2cpp_task_block(t); // a builder, a Task, or the armed continuation settles it
    int32_t sourceStatus;
    try
    {
        // Read before GetResult: a terminal GetResult may Reset() a pooled source,
        // making this operation's token invalid before it throws or returns.
        sourceStatus = dn2cpp_vts_get_status(b);
    }
    catch (...)
    {
        b->state.store(DN2CPP_VTS_DIRECT, std::memory_order_release);
        throw;
    }
    uint64_t r;
    try
    {
        r = dn2cpp_vts_get_result(b);
    }
    catch (const Dn2CppException& e)
    {
        // A pending source refused the read; as on .NET, the next read also goes to
        // the source. A terminal one is consumed, so the task keeps the fault for
        // every later read.
        if (sourceStatus == DN2CPP_TASK_PENDING)
            b->state.store(DN2CPP_VTS_DIRECT, std::memory_order_release);
        else
        {
            dn2cpp_task_set_exception_or_canceled(t, e.obj);
            b->state.store(DN2CPP_VTS_CONSUMED, std::memory_order_release);
        }
        throw;
    }
    catch (...)
    {
        b->state.store(DN2CPP_VTS_DIRECT, std::memory_order_release);
        throw;
    }
    // A read that returns consumed the operation, even one whose status read as pending.
    dn2cpp_task_set_result(t, r);
    b->state.store(DN2CPP_VTS_CONSUMED, std::memory_order_release);
    return t;
}

Dn2CppTask* dn2cpp_vts_task(Dn2CppObject* vts, int16_t version,
                            const void* getStatusFn, const void* getResultFn,
                            const void* onCompletedFn,
                            const Dn2CppTypeInfo* actionTi, int32_t resultKind,
                            uint64_t (*getStructResult)(const void*, Dn2CppObject*, int16_t))
{
    auto* b = static_cast<Dn2CppVtsBridge*>(dn2cpp_alloc(sizeof(Dn2CppVtsBridge)));
    b->header.type = &dn2cpp_vts_bridge_type;
    dn2cpp_gc_store_ref(&b->vts, vts);
    Dn2CppTask* t = dn2cpp_task_alloc();
    dn2cpp_gc_store_ref(&b->task, t);
    b->getStatusFn = getStatusFn;
    b->getResultFn = getResultFn;
    b->onCompletedFn = onCompletedFn;
    b->actionTi = actionTi;
    b->version = version;
    b->resultKind = resultKind;
    b->getStructResult = getStructResult;
    b->state.store(DN2CPP_VTS_DIRECT, std::memory_order_relaxed);
    dn2cpp_gc_store_ref(&t->vtsBridge, &b->header);
    return t;
}
