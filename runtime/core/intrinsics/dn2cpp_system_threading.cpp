// dn2cpp_system_threading.cpp — System.Threading.Interlocked / Monitor / Volatile
// intrinsics, backed by real hardware atomics and per-object mutexes.
//
// dn2cpp supports real OS threads, so these are genuine atomics and locks. The
// transpiler intercepts the corresponding BCL calls and binds them here. One
// per-namespace intrinsic unit (see runtime/core/intrinsics/).
#include "dn2cpp_core.h"
#include "platform/dn2cpp_pal.h" // dn2cpp_pal_membarrier_processwide

#include <atomic>
#include <chrono>
#include <condition_variable>
#include <cstdint>
#include <cstring>
#include <mutex>
#include <thread>
#include <unordered_map>

// ── Portable atomic primitives ───────────────────────────────────────────────
// GCC/Clang route straight to the __atomic_* builtins (unchanged from before —
// note this is a *runtime-side* fix only: the transpiler itself still emits
// __atomic_load_n/__atomic_store_n inline for the Volatile.Read/Write(int/
// pointer) fast path — see the comment further down — which is a separate,
// not-yet-done MSVC codegen gap in CppEmitter.cs, out of scope here). Real
// MSVC (cl.exe) has none of the __atomic_* builtins at all (clang-cl still
// defines __clang__ and takes the GNU path), so it routes to the matching
// _Interlocked* intrinsics instead — centralized here so the Interlocked/
// Volatile bodies below stay call-site-identical to what they always were.
#if defined(_MSC_VER) && !defined(__clang__)
#include <intrin.h>

static inline int8_t dn2cpp_atomic_cmpxchg_i1(int8_t* loc, int8_t expected, int8_t value)
{ return static_cast<int8_t>(_InterlockedCompareExchange8(reinterpret_cast<volatile char*>(loc), static_cast<char>(value), static_cast<char>(expected))); }
static inline int16_t dn2cpp_atomic_cmpxchg_i2(int16_t* loc, int16_t expected, int16_t value)
{ return _InterlockedCompareExchange16(reinterpret_cast<volatile short*>(loc), value, expected); }
static inline int32_t dn2cpp_atomic_cmpxchg_i4(int32_t* loc, int32_t expected, int32_t value)
{ return _InterlockedCompareExchange(reinterpret_cast<volatile long*>(loc), value, expected); }
static inline int64_t dn2cpp_atomic_cmpxchg_i8(int64_t* loc, int64_t expected, int64_t value)
{ return _InterlockedCompareExchange64(reinterpret_cast<volatile __int64*>(loc), value, expected); }
static inline void* dn2cpp_atomic_cmpxchg_ptr(void** loc, void* expected, void* value)
{ return _InterlockedCompareExchangePointer(loc, value, expected); }

static inline int8_t dn2cpp_atomic_exchange_i1(int8_t* loc, int8_t value)
{ return static_cast<int8_t>(_InterlockedExchange8(reinterpret_cast<volatile char*>(loc), static_cast<char>(value))); }
static inline int16_t dn2cpp_atomic_exchange_i2(int16_t* loc, int16_t value)
{ return _InterlockedExchange16(reinterpret_cast<volatile short*>(loc), value); }
static inline int32_t dn2cpp_atomic_exchange_i4(int32_t* loc, int32_t value)
{ return _InterlockedExchange(reinterpret_cast<volatile long*>(loc), value); }
static inline int64_t dn2cpp_atomic_exchange_i8(int64_t* loc, int64_t value)
{ return _InterlockedExchange64(reinterpret_cast<volatile __int64*>(loc), value); }
static inline void* dn2cpp_atomic_exchange_ptr(void** loc, void* value)
{ return _InterlockedExchangePointer(loc, value); }

static inline int32_t dn2cpp_atomic_add_i4(int32_t* loc, int32_t value) // returns the NEW value
{ return _InterlockedExchangeAdd(reinterpret_cast<volatile long*>(loc), value) + value; }
static inline int64_t dn2cpp_atomic_add_i8(int64_t* loc, int64_t value)
{ return _InterlockedExchangeAdd64(reinterpret_cast<volatile __int64*>(loc), value) + value; }

static inline int32_t dn2cpp_atomic_or_i4(int32_t* loc, int32_t value) // returns the ORIGINAL value
{ return _InterlockedOr(reinterpret_cast<volatile long*>(loc), value); }
static inline int64_t dn2cpp_atomic_or_i8(int64_t* loc, int64_t value)
{ return _InterlockedOr64(reinterpret_cast<volatile __int64*>(loc), value); }
static inline int32_t dn2cpp_atomic_and_i4(int32_t* loc, int32_t value)
{ return _InterlockedAnd(reinterpret_cast<volatile long*>(loc), value); }
static inline int64_t dn2cpp_atomic_and_i8(int64_t* loc, int64_t value)
{ return _InterlockedAnd64(reinterpret_cast<volatile __int64*>(loc), value); }

// SEQ_CST load/store on a plain (non-std::atomic) location: a same-value CAS
// (load) / Exchange (store) is the standard MSVC idiom here — both lower to a
// LOCK-prefixed instruction, i.e. a full fence, same as __atomic_*_n(SEQ_CST).
static inline uint32_t dn2cpp_atomic_load_u4(const uint32_t* p)
{ auto* mp = const_cast<uint32_t*>(p); return static_cast<uint32_t>(_InterlockedCompareExchange(reinterpret_cast<volatile long*>(mp), 0, 0)); }
static inline uint64_t dn2cpp_atomic_load_u8(const uint64_t* p)
{ auto* mp = const_cast<uint64_t*>(p); return static_cast<uint64_t>(_InterlockedCompareExchange64(reinterpret_cast<volatile __int64*>(mp), 0, 0)); }
static inline void dn2cpp_atomic_store_u4(uint32_t* p, uint32_t v)
{ _InterlockedExchange(reinterpret_cast<volatile long*>(p), v); }
static inline void dn2cpp_atomic_store_u8(uint64_t* p, uint64_t v)
{ _InterlockedExchange64(reinterpret_cast<volatile __int64*>(p), v); }
#else
static inline int8_t dn2cpp_atomic_cmpxchg_i1(int8_t* loc, int8_t expected, int8_t value)
{ __atomic_compare_exchange_n(loc, &expected, value, false, __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST); return expected; }
static inline int16_t dn2cpp_atomic_cmpxchg_i2(int16_t* loc, int16_t expected, int16_t value)
{ __atomic_compare_exchange_n(loc, &expected, value, false, __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST); return expected; }
static inline int32_t dn2cpp_atomic_cmpxchg_i4(int32_t* loc, int32_t expected, int32_t value)
{ __atomic_compare_exchange_n(loc, &expected, value, false, __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST); return expected; }
static inline int64_t dn2cpp_atomic_cmpxchg_i8(int64_t* loc, int64_t expected, int64_t value)
{ __atomic_compare_exchange_n(loc, &expected, value, false, __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST); return expected; }
static inline void* dn2cpp_atomic_cmpxchg_ptr(void** loc, void* expected, void* value)
{ __atomic_compare_exchange_n(loc, &expected, value, false, __ATOMIC_SEQ_CST, __ATOMIC_SEQ_CST); return expected; }

static inline int8_t dn2cpp_atomic_exchange_i1(int8_t* loc, int8_t value) { return __atomic_exchange_n(loc, value, __ATOMIC_SEQ_CST); }
static inline int16_t dn2cpp_atomic_exchange_i2(int16_t* loc, int16_t value) { return __atomic_exchange_n(loc, value, __ATOMIC_SEQ_CST); }
static inline int32_t dn2cpp_atomic_exchange_i4(int32_t* loc, int32_t value) { return __atomic_exchange_n(loc, value, __ATOMIC_SEQ_CST); }
static inline int64_t dn2cpp_atomic_exchange_i8(int64_t* loc, int64_t value) { return __atomic_exchange_n(loc, value, __ATOMIC_SEQ_CST); }
static inline void* dn2cpp_atomic_exchange_ptr(void** loc, void* value) { return __atomic_exchange_n(loc, value, __ATOMIC_SEQ_CST); }

static inline int32_t dn2cpp_atomic_add_i4(int32_t* loc, int32_t value) { return __atomic_add_fetch(loc, value, __ATOMIC_SEQ_CST); }
static inline int64_t dn2cpp_atomic_add_i8(int64_t* loc, int64_t value) { return __atomic_add_fetch(loc, value, __ATOMIC_SEQ_CST); }
static inline int32_t dn2cpp_atomic_or_i4(int32_t* loc, int32_t value) { return __atomic_fetch_or(loc, value, __ATOMIC_SEQ_CST); }
static inline int64_t dn2cpp_atomic_or_i8(int64_t* loc, int64_t value) { return __atomic_fetch_or(loc, value, __ATOMIC_SEQ_CST); }
static inline int32_t dn2cpp_atomic_and_i4(int32_t* loc, int32_t value) { return __atomic_fetch_and(loc, value, __ATOMIC_SEQ_CST); }
static inline int64_t dn2cpp_atomic_and_i8(int64_t* loc, int64_t value) { return __atomic_fetch_and(loc, value, __ATOMIC_SEQ_CST); }

static inline uint32_t dn2cpp_atomic_load_u4(const uint32_t* p) { return __atomic_load_n(p, __ATOMIC_SEQ_CST); }
static inline uint64_t dn2cpp_atomic_load_u8(const uint64_t* p) { return __atomic_load_n(p, __ATOMIC_SEQ_CST); }
static inline void dn2cpp_atomic_store_u4(uint32_t* p, uint32_t v) { __atomic_store_n(p, v, __ATOMIC_SEQ_CST); }
static inline void dn2cpp_atomic_store_u8(uint64_t* p, uint64_t v) { __atomic_store_n(p, v, __ATOMIC_SEQ_CST); }
#endif

// ---- Interlocked ----
// Real seq_cst atomics on the in-memory location. The signatures and return-value
// contract are unchanged from the old plain-RMW versions, so the transpiler emits
// the same calls: CompareExchange/Exchange return the ORIGINAL value, Add returns
// the NEW value (Increment/Decrement lower to Add(±1) in the transpiler).
//
// 32-bit-target note: the 64-bit (_i8) variants below,
// the 64-bit Volatile read/write further down, and the std::atomic<uint64_t>
// task-id counter use __atomic_* builtins on int64_t. On a 64-bit target these
// are native lock-free instructions. On a 32-bit target they lower to out-of-line
// __atomic_*_8 calls that live in libatomic, so the final link must add -latomic
// (the gate build probes this; the CMake build mirrors it). The runtime code
// itself stays unchanged across word sizes — only the link line differs.

Dn2CppObject* dn2cpp_interlocked_cmpxchg_ref(Dn2CppObject** loc, Dn2CppObject* value, Dn2CppObject* comparand)
{
    // the value found in *loc (== comparand on success)
    auto* original = static_cast<Dn2CppObject*>(
        dn2cpp_atomic_cmpxchg_ptr(reinterpret_cast<void**>(loc), comparand, value));
    dn2cpp_gc_write_barrier_if_heap(loc);
    return original;
}

// Sub-word (1-/2-byte) variants: declared-width hardware CAS/exchange, so the
// operation never touches memory beyond the location — a `ref byte` may point
// into a packed struct field or array element where the adjacent bytes are live
// neighboring data that a wider RMW would corrupt.
int8_t dn2cpp_interlocked_cmpxchg_i1(int8_t* loc, int8_t value, int8_t comparand)
{
    return dn2cpp_atomic_cmpxchg_i1(loc, comparand, value);
}

int16_t dn2cpp_interlocked_cmpxchg_i2(int16_t* loc, int16_t value, int16_t comparand)
{
    return dn2cpp_atomic_cmpxchg_i2(loc, comparand, value);
}

int32_t dn2cpp_interlocked_cmpxchg_i4(int32_t* loc, int32_t value, int32_t comparand)
{
    return dn2cpp_atomic_cmpxchg_i4(loc, comparand, value);
}

int64_t dn2cpp_interlocked_cmpxchg_i8(int64_t* loc, int64_t value, int64_t comparand)
{
    return dn2cpp_atomic_cmpxchg_i8(loc, comparand, value);
}

Dn2CppObject* dn2cpp_interlocked_exchange_ref(Dn2CppObject** loc, Dn2CppObject* value)
{
    auto* original = static_cast<Dn2CppObject*>(
        dn2cpp_atomic_exchange_ptr(reinterpret_cast<void**>(loc), value));
    dn2cpp_gc_write_barrier_if_heap(loc);
    return original;
}

int8_t dn2cpp_interlocked_exchange_i1(int8_t* loc, int8_t value)
{
    return dn2cpp_atomic_exchange_i1(loc, value);
}

int16_t dn2cpp_interlocked_exchange_i2(int16_t* loc, int16_t value)
{
    return dn2cpp_atomic_exchange_i2(loc, value);
}

int32_t dn2cpp_interlocked_exchange_i4(int32_t* loc, int32_t value)
{
    return dn2cpp_atomic_exchange_i4(loc, value);
}

int64_t dn2cpp_interlocked_exchange_i8(int64_t* loc, int64_t value)
{
    return dn2cpp_atomic_exchange_i8(loc, value);
}

// float/double Exchange/CompareExchange: .NET's semantics are a single hardware
// atomic on the BIT PATTERN — the CompareExchange comparison is bitwise equality,
// not IEEE ==. So a -0.0 comparand does NOT match a +0.0 location, and a
// bit-identical NaN comparand DOES match (and swaps). One integer CAS/exchange
// over a memcpy bit-cast reproduces this exactly; no compare loop, no FP compare.
float dn2cpp_interlocked_exchange_r4(float* loc, float value)
{
    uint32_t v;
    std::memcpy(&v, &value, sizeof v);
    int32_t old = dn2cpp_atomic_exchange_i4(reinterpret_cast<int32_t*>(loc), static_cast<int32_t>(v));
    float f;
    std::memcpy(&f, &old, sizeof f);
    return f;
}

double dn2cpp_interlocked_exchange_r8(double* loc, double value)
{
    uint64_t v;
    std::memcpy(&v, &value, sizeof v);
    int64_t old = dn2cpp_atomic_exchange_i8(reinterpret_cast<int64_t*>(loc), static_cast<int64_t>(v));
    double d;
    std::memcpy(&d, &old, sizeof d);
    return d;
}

float dn2cpp_interlocked_cmpxchg_r4(float* loc, float value, float comparand)
{
    uint32_t v, c;
    std::memcpy(&v, &value, sizeof v);
    std::memcpy(&c, &comparand, sizeof c);
    int32_t old = dn2cpp_atomic_cmpxchg_i4(reinterpret_cast<int32_t*>(loc), static_cast<int32_t>(c), static_cast<int32_t>(v));
    float f;
    std::memcpy(&f, &old, sizeof f);
    return f;
}

double dn2cpp_interlocked_cmpxchg_r8(double* loc, double value, double comparand)
{
    uint64_t v, c;
    std::memcpy(&v, &value, sizeof v);
    std::memcpy(&c, &comparand, sizeof c);
    int64_t old = dn2cpp_atomic_cmpxchg_i8(reinterpret_cast<int64_t*>(loc), static_cast<int64_t>(c), static_cast<int64_t>(v));
    double d;
    std::memcpy(&d, &old, sizeof d);
    return d;
}

// Interlocked.Read(ref long/ulong): a seq_cst atomic 64-bit load (atomic even on
// a 32-bit target, where a plain load could tear).
int64_t dn2cpp_interlocked_read_i8(int64_t* loc)
{
    return static_cast<int64_t>(dn2cpp_atomic_load_u8(reinterpret_cast<const uint64_t*>(loc)));
}

// Interlocked.MemoryBarrierProcessWide: a process-wide barrier that also fences
// every OTHER thread of the process (the asymmetric-fence pattern: lets hot
// readers drop their fences and have the slow writer pay for everyone). The
// per-OS mechanism lives behind the PAL seam.
void dn2cpp_interlocked_membarrier_processwide()
{
    dn2cpp_pal_membarrier_processwide();
}

int32_t dn2cpp_interlocked_add_i4(int32_t* loc, int32_t value)
{
    return dn2cpp_atomic_add_i4(loc, value); // returns the new value
}

int64_t dn2cpp_interlocked_add_i8(int64_t* loc, int64_t value)
{
    return dn2cpp_atomic_add_i8(loc, value);
}

int32_t dn2cpp_interlocked_or_i4(int32_t* loc, int32_t value)
{
    return dn2cpp_atomic_or_i4(loc, value); // returns the original value
}

int64_t dn2cpp_interlocked_or_i8(int64_t* loc, int64_t value)
{
    return dn2cpp_atomic_or_i8(loc, value);
}

int32_t dn2cpp_interlocked_and_i4(int32_t* loc, int32_t value)
{
    return dn2cpp_atomic_and_i4(loc, value);
}

int64_t dn2cpp_interlocked_and_i8(int64_t* loc, int64_t value)
{
    return dn2cpp_atomic_and_i8(loc, value);
}

// ---- Monitor / lock / System.Threading.Lock ----
// A per-object monitor keyed by object identity. The GC is non-moving, so object
// pointers are stable map keys. Entries live for the object's lifetime (a small,
// bounded leak — acceptable, mirrors how a real CLR's sync-block table retains
// inflated locks).
//
// The lock is a hand-rolled recursive mutex built on a plain std::mutex plus two
// condition variables, rather than std::recursive_mutex. We need this because
// Monitor.Wait must atomically (a) fully release the lock — including the entire
// recursion depth — (b) block on a condition signaled by Pulse/PulseAll, and
// (c) re-acquire to the saved depth on wake; std::recursive_mutex exposes none of
// that. Tracking the owner thread id and recursion count by hand gives us all
// three. `lock_cv` wakes threads waiting to acquire the lock; `wait_cv` wakes
// threads parked in Monitor.Wait.
struct Dn2CppMonitor
{
    std::mutex mtx;                   // guards the fields below + serializes enter/exit
    std::condition_variable lock_cv; // signaled when the lock becomes free (enter-waiters)
    std::condition_variable wait_cv; // signaled by Pulse/PulseAll (Wait-waiters)
    std::thread::id owner;
    bool has_owner = false;
    uint32_t count = 0;              // recursion depth
    int waiters = 0;                 // threads currently parked in Wait()
    int to_release = 0;             // pending Pulse credits (waiters cleared to wake)
};

// The object -> monitor table is STRIPED: one global mutex here used to be
// taken twice per `lock` statement (enter + exit), serializing every monitor
// operation in the process — including threads locking entirely unrelated
// objects. Each stripe owns its mutex and its map shard; an object maps to a
// stripe by pointer hash, so operations on distinct objects mostly touch
// distinct stripes and only same-stripe operations contend. Semantics are
// unchanged: an object's monitor still lives in exactly one shard (the stripe
// choice is a pure function of the pointer), entries are still created once
// and never removed, so a Dn2CppMonitor* handed out is stable for the
// object's lifetime — Wait/Pulse/recursion state all live on the monitor
// itself, which this table only finds, never touches. The GC is non-moving
// and the keyed object is live while anyone holds its lock, so keys are
// stable here for the same reason they were in the single-map version (the
// small monitor leak for dead objects is unchanged, and the maps' internal
// buffers hold raw keys the collector never scans — the values are plain
// `new`, invisible to the GC either way). The stripe count is a power of two;
// >>4 drops the allocation-alignment zero bits so consecutive heap objects
// spread across stripes. Single-threaded wasm compiles this unchanged
// (std::mutex is available there; it just never contends).
namespace
{
struct Dn2CppMonitorTable
{
    static constexpr uint32_t kStripes = 64;
    struct Stripe
    {
        std::mutex mtx;
        std::unordered_map<void*, Dn2CppMonitor*> map;
    };
    Stripe stripes[kStripes];

    Stripe& StripeFor(void* o)
    {
        return stripes[(reinterpret_cast<uintptr_t>(o) >> 4) & (kStripes - 1)];
    }
};
}

static Dn2CppMonitorTable& g_monitor_table = dn2cpp_never_destroyed<Dn2CppMonitorTable>();

static Dn2CppMonitor* dn2cpp_monitor_for(void* o)
{
    Dn2CppMonitorTable::Stripe& s = g_monitor_table.StripeFor(o);
    std::lock_guard<std::mutex> lk(s.mtx);
    Dn2CppMonitor*& slot = s.map[o];
    if (slot == nullptr)
        slot = new Dn2CppMonitor();
    return slot;
}

void dn2cpp_monitor_enter(Dn2CppObject* o)
{
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    std::thread::id self = std::this_thread::get_id();
    if (mon->has_owner && mon->owner == self)
    {
        mon->count++;
        return;
    }
    mon->lock_cv.wait(lk, [&] { return !mon->has_owner; });
    mon->has_owner = true;
    mon->owner = self;
    mon->count = 1;
}

void dn2cpp_monitor_exit(Dn2CppObject* o)
{
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    // Assume the caller owns it (matches the old recursive_mutex contract — no
    // SynchronizationLockException is raised on a mismatched Exit).
    if (--mon->count == 0)
    {
        mon->has_owner = false;
        mon->lock_cv.notify_one();
    }
}

int32_t dn2cpp_monitor_try_enter(Dn2CppObject* o)
{
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    std::thread::id self = std::this_thread::get_id();
    if (mon->has_owner && mon->owner == self)
    {
        mon->count++;
        return 1;
    }
    if (!mon->has_owner)
    {
        mon->has_owner = true;
        mon->owner = self;
        mon->count = 1;
        return 1;
    }
    return 0; // non-blocking — the bare TryEnter / TryEnter(obj, 0) form
}

// Monitor.TryEnter(obj, int)/(obj, TimeSpan): like enter, but block at most ms for the
// lock. Returns 1 if acquired (recursive re-entry by the owner always succeeds), 0 on
// timeout. ms == 0 is a single immediate attempt; a negative ms is an infinite wait.
int32_t dn2cpp_monitor_try_enter_timeout(Dn2CppObject* o, int32_t ms)
{
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    std::thread::id self = std::this_thread::get_id();
    if (mon->has_owner && mon->owner == self)
    {
        mon->count++;
        return 1;
    }
    if (ms < 0)
        mon->lock_cv.wait(lk, [&] { return !mon->has_owner; });
    else if (!mon->lock_cv.wait_for(lk, std::chrono::milliseconds(ms), [&] { return !mon->has_owner; }))
        return 0; // timeout — not acquired
    mon->has_owner = true;
    mon->owner = self;
    mon->count = 1;
    return 1;
}

// Monitor.Wait: the caller owns the monitor. Fully release the lock (saving the
// recursion depth), park on wait_cv until Pulse/PulseAll grants a credit, then
// re-acquire the lock to the saved depth. The no-timeout form always wakes on a
// credit, so it reacquires and returns 1 (true).
int32_t dn2cpp_monitor_wait(Dn2CppObject* o)
{
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    std::thread::id self = std::this_thread::get_id();
    uint32_t saved = mon->count;
    // Fully release the lock — including the whole recursion depth — and let an
    // enter-waiter in.
    mon->count = 0;
    mon->has_owner = false;
    mon->waiters++;
    mon->lock_cv.notify_one();
    // Park until a Pulse/PulseAll credit is available.
    mon->wait_cv.wait(lk, [&] { return mon->to_release > 0; });
    mon->to_release--;
    mon->waiters--;
    // Re-acquire the lock (re-block if the pulser still holds it), restoring depth.
    mon->lock_cv.wait(lk, [&] { return !mon->has_owner; });
    mon->has_owner = true;
    mon->owner = self;
    mon->count = saved;
    return 1;
}

// Monitor.Wait(obj, int)/(obj, TimeSpan): as above, but park for at most ms. Returns 1 if
// woken by a Pulse/PulseAll credit (consuming it), 0 on timeout. Either way the waiter
// re-acquires the monitor to its saved recursion depth before returning (the .NET
// contract — a timed-out waiter still owns the lock on return). A negative ms is an
// infinite wait. CRITICAL: on timeout the pending-credit count is left untouched.
int32_t dn2cpp_monitor_wait_timeout(Dn2CppObject* o, int32_t ms)
{
    if (ms < 0)
        return dn2cpp_monitor_wait(o);
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    std::thread::id self = std::this_thread::get_id();
    uint32_t saved = mon->count;
    mon->count = 0;
    mon->has_owner = false;
    mon->waiters++;
    mon->lock_cv.notify_one();
    int32_t acquired;
    if (mon->wait_cv.wait_for(lk, std::chrono::milliseconds(ms), [&] { return mon->to_release > 0; }))
    {
        mon->to_release--; // a credit was granted: consume it
        acquired = 1;
    }
    else
    {
        acquired = 0; // timed out with no credit — leave to_release untouched
    }
    mon->waiters--;
    // A timed-out waiter must still re-acquire the monitor before returning.
    mon->lock_cv.wait(lk, [&] { return !mon->has_owner; });
    mon->has_owner = true;
    mon->owner = self;
    mon->count = saved;
    return acquired;
}

// Monitor.Pulse: wake at most one parked waiter (grant one credit, but never more
// credits than there are uncredited waiters).
void dn2cpp_monitor_pulse(Dn2CppObject* o)
{
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    if (mon->waiters > mon->to_release)
    {
        mon->to_release++;
        mon->wait_cv.notify_one();
    }
}

// Monitor.PulseAll: wake every parked waiter (grant a credit to each).
void dn2cpp_monitor_pulse_all(Dn2CppObject* o)
{
    Dn2CppMonitor* mon = dn2cpp_monitor_for(o);
    std::unique_lock<std::mutex> lk(mon->mtx);
    mon->to_release = mon->waiters;
    mon->wait_cv.notify_all();
}

// ---- Volatile.Read/Write (float/double) ----
// Integer and pointer overloads are emitted inline by the transpiler as
// __atomic_load_n / __atomic_store_n. Floating types are not valid operands for the
// generic-N atomic builtins, so route them through an integer bit-cast.
float dn2cpp_volatile_read_r4(const float* p)
{
    uint32_t u = dn2cpp_atomic_load_u4(reinterpret_cast<const uint32_t*>(p));
    float f;
    std::memcpy(&f, &u, sizeof f);
    return f;
}

double dn2cpp_volatile_read_r8(const double* p)
{
    uint64_t u = dn2cpp_atomic_load_u8(reinterpret_cast<const uint64_t*>(p));
    double d;
    std::memcpy(&d, &u, sizeof d);
    return d;
}

void dn2cpp_volatile_write_r4(float* p, float v)
{
    uint32_t u;
    std::memcpy(&u, &v, sizeof u);
    dn2cpp_atomic_store_u4(reinterpret_cast<uint32_t*>(p), u);
}

void dn2cpp_volatile_write_r8(double* p, double v)
{
    uint64_t u;
    std::memcpy(&u, &v, sizeof u);
    dn2cpp_atomic_store_u8(reinterpret_cast<uint64_t*>(p), u);
}

// ---- ThreadLocal<T> (per-instance, per-thread storage) ----
// Each thread sees its own value for a given ThreadLocal instance. The value is stored
// boxed in a single uniform Dn2CppObject* slot, so one storage shape serves int/long/
// float/double/reference T. The optional valueFactory (a Func<T> delegate) runs lazily
// on first access on each thread, dispatched by result kind — the same delegate-call
// shape as the Task.Run run thunks (dn2cpp_run_i4/i8/r4/r8/ref).
//
// Storage is a per-instance linked list of nodes keyed by a per-thread id. Both the
// instance and the nodes are GC-allocated (dn2cpp_alloc), so the boxed values are
// reachable from the GC heap and never collected out from under a live thread — unlike
// a thread_local std container, whose nodes live in unscanned malloc memory. A single
// global mutex serializes list mutation (critical sections are tiny; contention across
// distinct ThreadLocal instances is rare). The valueFactory itself runs OUTSIDE the
// lock because it is arbitrary user code that may allocate, block, or recurse.
// Each static type-info bakes its interned Type companion in (lock-free typeof/GetType).
// Non-const and externally visible: the generated init prologue installs its IDisposable
// dispatch row, whose entry names program-specific emitted type-info.
struct Dn2CppThreadLocalNode : Dn2CppObject  // GC-allocated; scanned for `value`/`next`
{
    uint64_t threadId;            // owning thread's stable id (see dn2cpp_tls_self_id)
    Dn2CppObject* value;          // boxed value-type T, or the reference T directly
    Dn2CppThreadLocalNode* next;
};

struct Dn2CppThreadLocal : Dn2CppObject  // GC-allocated
{
    Dn2CppObject* factory;            // Func<T> delegate, or null (=> default(T))
    const Dn2CppTypeInfo* ti;         // type-info for boxing a value-type T (null for reference T)
    int32_t size;                     // sizeof(T) for boxing/default (0 for reference T)
    int32_t kind;                     // 0=ref 1=i4 2=i8 3=r4 4=r8 5=struct
    Dn2CppThreadLocalBoxFn boxFn;     // emitted "call the factory at T's ABI and box" thunk (null iff kind==0)
    Dn2CppThreadLocalNode* head;      // per-thread node list (GC-reachable from this instance)
};

// Defined after the struct so the type-info can state the struct's extent: it is what
// the shallow clone and GetUninitializedObject size an allocation from.
extern const Dn2CppType dn2cpp_threadlocal_type_obj;
Dn2CppTypeInfo dn2cpp_threadlocal_type =
    dn2cpp_ti_with_typeobject({ "System.Threading.ThreadLocal`1", nullptr,
                                (int32_t)sizeof(Dn2CppThreadLocal), nullptr, nullptr, 0 },
                              &dn2cpp_threadlocal_type_obj);
const Dn2CppType dn2cpp_threadlocal_type_obj = { { &dn2cpp_type_type }, &dn2cpp_threadlocal_type };

static std::mutex& g_threadlocal_mtx = dn2cpp_never_destroyed<std::mutex>();

// A stable, unique id for the calling thread. A plain thread_local integer (no managed
// pointer, so no GC concern) assigned lazily from a monotonic sequence — avoids any
// dependence on std::thread::id hashing being collision-free.
static uint64_t dn2cpp_tls_self_id()
{
    static std::atomic<uint64_t> seq{ 1 };
    static thread_local uint64_t id = seq.fetch_add(1, std::memory_order_relaxed);
    return id;
}

static Dn2CppThreadLocalNode* dn2cpp_threadlocal_find(Dn2CppThreadLocal* h, uint64_t id)
{
    for (Dn2CppThreadLocalNode* n = h->head; n != nullptr; n = n->next)
        if (n->threadId == id)
            return n;
    return nullptr;
}

// Produce the initial per-thread value: run the factory or, with no factory, default(T)
// — null for reference T, a zero-boxed payload otherwise.
// Runs with NO lock held (the factory is user code).
//
// A value-type T's factory is called through the emitted boxFn thunk rather than
// through a switch over result kinds: such a switch could only name the C ABIs it can
// spell (int32/int64/float/double), so every T whose return is a LAYOUT rather than a
// width — every struct, and IntPtr where intptr_t is not int64_t — would `dn2cpp_fail`
// for a program real .NET runs. See the typedef in dn2cpp_core.h for why the emitter is
// the only side that can spell it. The thunk boxes the exact sizeof(T), so no
// little-endian-only narrowing of a widened scalar is involved.
static Dn2CppObject* dn2cpp_threadlocal_make_initial(Dn2CppThreadLocal* h)
{
    if (h->factory != nullptr)
    {
        if (h->boxFn != nullptr)
            return h->boxFn(h->factory);
        // Reference T: every Func<T> here returns a pointer, so one cast serves them
        // all and no per-T thunk is emitted (boxFn == nullptr iff kind == 0).
        auto* dg = reinterpret_cast<Dn2CppDelegate*>(h->factory);
        return reinterpret_cast<Dn2CppObject* (*)(Dn2CppObject*)>(dg->method)(dg->target);
    }
    if (h->kind == 0)
        return nullptr; // default(T) for reference T
    // default(T) for a value type: box a zero-filled payload. dn2cpp_alloc is GC-zeroed.
    size_t sz = h->size > 0 ? static_cast<size_t>(h->size) : 1;
    void* zero = dn2cpp_alloc(sz);
    return dn2cpp_box(h->ti, zero, static_cast<size_t>(h->size));
}

Dn2CppObject* dn2cpp_threadlocal_new(Dn2CppObject* factory, const Dn2CppTypeInfo* ti, int32_t size,
                                     int32_t kind, Dn2CppThreadLocalBoxFn boxFn)
{
    auto* h = static_cast<Dn2CppThreadLocal*>(dn2cpp_alloc(sizeof(Dn2CppThreadLocal)));
    h->type = &dn2cpp_threadlocal_type;
    h->factory = factory;
    h->ti = ti;
    h->size = size;
    h->kind = kind;
    h->boxFn = boxFn;
    h->head = nullptr;
    return h;
}

Dn2CppObject* dn2cpp_threadlocal_get(Dn2CppObject* o)
{
    auto* h = static_cast<Dn2CppThreadLocal*>(o);
    uint64_t id = dn2cpp_tls_self_id();
    {
        std::lock_guard<std::mutex> lk(g_threadlocal_mtx);
        if (Dn2CppThreadLocalNode* n = dn2cpp_threadlocal_find(h, id))
            return n->value;
    }
    // Absent on this thread: build the initial value (factory runs lock-free). `initial`
    // stays on this frame's stack -> GC-reachable until the node holds it.
    Dn2CppObject* initial = dn2cpp_threadlocal_make_initial(h);
    std::lock_guard<std::mutex> lk(g_threadlocal_mtx);
    // Re-check: only this thread inserts for `id`, but the factory may have re-entrantly
    // created the slot (recursive init isn't modeled as the .NET InvalidOperationException).
    if (Dn2CppThreadLocalNode* n = dn2cpp_threadlocal_find(h, id))
        return n->value;
    auto* node = static_cast<Dn2CppThreadLocalNode*>(dn2cpp_alloc(sizeof(Dn2CppThreadLocalNode)));
    node->threadId = id;
    node->value = initial;
    node->next = h->head;
    h->head = node;
    return initial;
}

void dn2cpp_threadlocal_set(Dn2CppObject* o, Dn2CppObject* boxedOrRef)
{
    auto* h = static_cast<Dn2CppThreadLocal*>(o);
    uint64_t id = dn2cpp_tls_self_id();
    std::lock_guard<std::mutex> lk(g_threadlocal_mtx);
    if (Dn2CppThreadLocalNode* n = dn2cpp_threadlocal_find(h, id))
    {
        n->value = boxedOrRef;
        return;
    }
    auto* node = static_cast<Dn2CppThreadLocalNode*>(dn2cpp_alloc(sizeof(Dn2CppThreadLocalNode)));
    node->threadId = id;
    node->value = boxedOrRef;
    node->next = h->head;
    h->head = node;
}

int32_t dn2cpp_threadlocal_is_created(Dn2CppObject* o)
{
    auto* h = static_cast<Dn2CppThreadLocal*>(o);
    uint64_t id = dn2cpp_tls_self_id();
    std::lock_guard<std::mutex> lk(g_threadlocal_mtx);
    return dn2cpp_threadlocal_find(h, id) != nullptr ? 1 : 0;
}
