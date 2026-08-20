// Deterministic write-barrier probe, linked against the real runtime.
//
// A managed churn program reaches the incremental collector's mark window only
// by luck. Here the cycle is driven by hand — GC_start_incremental_collection
// then a counted number of GC_collect_a_little steps — so the store lands at a
// known point of a known cycle and the outcome is a fact, not a race.
//
// The invariant under test: what runs here is a partial cycle, which marks from
// the dirty blocks and the roots only, so a block it already marked and left
// clean is never rescanned and a plain store into one is invisible to it — the
// referent is swept while live. dn2cpp_gc_write_barrier — and
// dn2cpp_gc_memmove_refs for the bulk move — is the only thing that says
// otherwise. The unbarriered arms are negative controls: were they to survive,
// the barriered arms would be asserting nothing.
//
// Every store therefore runs on a thread the collector never learns about. In a
// threaded build the conservative scan dirties whatever object it finds a
// pointer to on a scanned stack, so a holder named from any thread the collector
// knows is rescanned inside the same cycle whatever the barrier did, and the
// unbarriered arms survive vacuously. Registering that thread and joining it
// does not help: unregistering waits out the collection in progress, and does so
// with the storing stack still in the root set. Only the main thread is ever
// registered while a cycle is live, and it reaches the holder through an
// uncollectable anchor rather than naming it — pushing an anchor stops at the
// holder's mark bit without looking inside it.
//
// Two ways this probe can read as a pass while testing nothing, so neither is
// assumed. A cycle that ended before the store: the window is calibrated once
// and re-proved on every trial. And a payload the host retains regardless: a
// fifth subject publishes nowhere, under the identical cycle, and must be lost
// every time. Each failure is by name.

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <thread>

#include <gc.h>

#include "dn2cpp_core.h"

// These tables live in generated.cpp for every real program; this one holds no
// managed code, so it supplies the empty forms the runtime's startup reads. One
// zeroed row rather than none, the emitter's own spelling for an empty table:
// the count is what the runtime reads, and MSVC rejects a zero-sized array.
const Dn2CppTypeRegEntry dn2cpp_type_registry[] = { {} };
const int32_t dn2cpp_type_registry_count = 0;
const Dn2CppTypeBind dn2cpp_type_binds[] = { {} };
const int32_t dn2cpp_type_bind_count = 0;
const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[] = { {} };
const int32_t dn2cpp_assembly_registry_count = 0;
const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[] = { {} };
const int32_t dn2cpp_delegate_refl_registry_count = 0;
const Dn2CppBclMessage dn2cpp_bcl_messages[] = { {} };
const int32_t dn2cpp_bcl_message_count = 0;
const int32_t dn2cpp_exception_get_message_slot = -1;
const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = nullptr;
const int32_t dn2cpp_runtime_template_count = 0;

namespace
{

enum Subject
{
    PlainStore = 0,
    BarrieredStore,
    PlainMemmove,
    BarrieredMemmove,
    Unpublished,
    SubjectCount
};

const char* const kSubjectNames[SubjectCount] = {
    "plain store", "barriered store", "plain memmove", "dn2cpp_gc_memmove_refs",
    "unpublished control"
};

// Every increment count from 0 up, rather than one chosen count: which counts
// land the store inside the window is a heap-shape artefact, while the totals
// the gate asserts are all-or-nothing under either mode.
const int kSteps = 64;

// The window must outlast the step budget on every host, with room for the
// increment GC_start_incremental_collection spends on the caller's behalf.
const int kMinCycleIncrements = 96;

// A size class of the payload's own. Allocating in the ballast's class walks the
// ballast's reclaim list, and every step of that walk spends a collector
// increment — enough of them to close the window inside the allocation.
const size_t kPayloadSize = 500;

// Reclamation is read off the finalizer, never off the slot: a swept block may
// still hold its old bytes, so reading the payload back would answer "alive" for
// memory the collector has already handed out.
volatile int g_finalized;

// Set by a worker that could not register with the collector. Its body is then
// skipped, because an unregistered thread must not allocate.
volatile bool g_thread_failed;

void GC_CALLBACK OnFinalize(void* /*obj*/, void* /*client*/)
{
    g_finalized = 1;
}

// The anchor is uncollectable, so it is a root and never moves; the holder every
// subject stores into hangs off it and is reached ONLY through it. The hop is
// what makes a pointer the main thread leaves behind harmless: the conservative
// scan dirties the anchor, and rescanning the anchor stops at the holder's mark
// bit without scanning the holder's contents.
void** g_anchor;

// The trial's payload, carried from the thread that allocates it to the thread
// that stores it as a complement, so the word itself is never a pointer the
// conservative scan could follow.
uintptr_t g_hidden_payload;

// Ballast, rooted uncollectably and re-dirtied before every cycle: a partial
// cycle is no longer than its dirty-block count, and a clean block is invisible
// to it. Too short a cycle finishes inside the step budget, leaving no window for
// the store to fall into — which reads as "no barrier needed", green and wrong.
void** g_ballast;
int g_ballast_nodes;

DN2CPP_NOINLINE void GrowBallast(int nodes)
{
    void** head = static_cast<void**>(*g_ballast);
    for (int i = 0; i < nodes; i++)
    {
        void** node = static_cast<void**>(dn2cpp_alloc(64));
        *node = head;
        head = node;
    }
    *g_ballast = head;
    dn2cpp_gc_write_barrier(g_ballast);
    g_ballast_nodes += nodes;
}

// The dirty bits a cycle marks from are consumed when it starts, so this is per
// cycle, not once.
DN2CPP_NOINLINE void DirtyBallast()
{
    for (void** node = static_cast<void**>(*g_ballast); node != nullptr;
         node = static_cast<void**>(*node))
        dn2cpp_gc_write_barrier(node);
}

// Run `body` on a thread the collector never learns about, and join it. Nothing
// that thread leaves on its stack or in its registers is ever scanned, which is
// the only way to touch a heap object inside a live cycle and leave no trace.
// The body must not allocate, and nothing may drive the collector while it runs
// — the join is what guarantees the second, the callers the first.
DN2CPP_NOINLINE void RunUnregistered(void (*body)(void*), void* arg)
{
    std::thread worker([body, arg] { body(arg); });
    worker.join();
}

// Run `body` on a registered thread and join it. For bodies that allocate, and
// only where no cycle is in progress: unregistering drives one that is to
// completion, which would spend the whole window inside this call.
DN2CPP_NOINLINE void RunRegistered(void (*body)(void*), void* arg)
{
    std::thread worker([body, arg] {
        struct GC_stack_base sb;
        if (GC_get_stack_base(&sb) != GC_SUCCESS
            || GC_register_my_thread(&sb) != GC_SUCCESS)
        {
            g_thread_failed = true;
            return;
        }
        body(arg);
        GC_unregister_my_thread();
    });
    worker.join();
}

// A size class of its own: dirtiness is per block, so a holder sharing a block
// with a ballast node would be rescanned by the very dirtying that holds the
// window open.
void CreateHolder(void*)
{
    *g_anchor = dn2cpp_alloc(3000);
    dn2cpp_gc_write_barrier(g_anchor);
}

void ClearHolder(void*)
{
    static_cast<void**>(*g_anchor)[0] = nullptr;
}

// Allocated after the trial has settled and before its cycle opens, so the
// payload is unmarked for that whole cycle and the store is the only thing that
// can reach it.
void AllocPayload(void*)
{
    void* payload = dn2cpp_alloc(kPayloadSize);
    GC_register_finalizer_no_order(payload, OnFinalize, nullptr, nullptr, nullptr);
    g_hidden_payload = ~reinterpret_cast<uintptr_t>(payload);
}

struct StoreRequest
{
    int subject;
    GC_word cycle;
    int windowHeld;
};

void StorePayload(void* arg)
{
    StoreRequest* req = static_cast<StoreRequest*>(arg);

    // gc_no advances only where a cycle's mark completes, so an unchanged count
    // is proof that the store below lands inside the window rather than after it.
    req->windowHeld = GC_get_gc_no() == req->cycle ? 1 : 0;

    void* payload = reinterpret_cast<void*>(~g_hidden_payload);
    void** holder = static_cast<void**>(*g_anchor);
    switch (req->subject)
    {
        case PlainStore:
            holder[0] = payload;
            break;
        case BarrieredStore:
            holder[0] = payload;
            dn2cpp_gc_write_barrier(holder);
            break;
        case PlainMemmove:
            std::memmove(holder, &payload, sizeof(void*));
            break;
        case BarrieredMemmove:
            dn2cpp_gc_memmove_refs(holder, &payload, sizeof(void*));
            break;
        default:
            break; // the unpublished control stores nowhere
    }
}

// Drive a fresh cycle to its end, counting the increments it takes. The count
// includes the one GC_start_incremental_collection spends itself.
DN2CPP_NOINLINE int MeasureCycleLength()
{
    GC_gcollect();
    GC_invoke_finalizers();
    DirtyBallast();

    int increments = 1;
    GC_start_incremental_collection();
    while (increments < 1000000 && GC_collect_a_little() != 0)
        increments++;
    return increments;
}

// Asked before either arm: a host that keeps an unreferenced payload alive makes
// every subject below survive for a reason that has nothing to do with the
// barrier. The incremental arm re-asks it under the trial's own cycle (the
// unpublished subject); this form is the one that also covers stop-the-world,
// where that subject's survival is by construction.
DN2CPP_NOINLINE bool UnpublishedIsCollected()
{
    g_finalized = 0;
    RunRegistered(AllocPayload, nullptr);
    for (int i = 0; i < 3 && g_finalized == 0; i++)
    {
        GC_gcollect();
        GC_invoke_finalizers();
    }
    return g_finalized != 0;
}

// One trial: settle, allocate the payload, advance a fresh cycle by `steps`
// increments, store from a thread the collector cannot see, then drive that same
// cycle to its end. Answers 1 when the payload was reclaimed, and reports through
// `windowHeld` whether the store landed inside the cycle.
DN2CPP_NOINLINE int RunOnce(int subject, int steps, int* windowHeld)
{
    RunUnregistered(ClearHolder, nullptr);
    GC_gcollect();
    GC_invoke_finalizers();
    RunRegistered(AllocPayload, nullptr);
    g_finalized = 0;

    DirtyBallast();
    StoreRequest req;
    req.subject = subject;
    req.cycle = GC_get_gc_no();
    req.windowHeld = 0;
    GC_start_incremental_collection();
    for (int i = 0; i < steps; i++)
        GC_collect_a_little();

    RunUnregistered(StorePayload, &req);
    *windowHeld = req.windowHeld;

    // Finish the cycle the store raced: GC_collect_a_little answers 0 once no
    // collection is in progress, which is past that cycle's mark.
    for (int i = 0; i < 100000 && GC_collect_a_little() != 0; i++)
    {
    }
    GC_invoke_finalizers();
    return g_finalized != 0;
}

} // namespace

int main()
{
    dn2cpp_runtime_init();

    const bool incremental = GC_is_incremental_mode() != 0;
    std::printf("incremental mode: %s\n", incremental ? "on" : "off");

    // A zero mark time limit is what makes the window exist at every heap size:
    // with the runtime's default the whole mark finishes inside the first
    // increment on a heap this small, and the probe would measure nothing.
    GC_set_time_limit(0);

    // Keep every trial partial. A full cycle clears the marks and rescans the
    // holder mid-cycle, so the outcome would turn on the increment count.
    GC_set_full_freq(1000000);

    g_anchor = static_cast<void**>(dn2cpp_alloc_pinned(sizeof(void*)));
    g_ballast = static_cast<void**>(dn2cpp_alloc_pinned(sizeof(void*)));
    GrowBallast(1 << 17);
    RunRegistered(CreateHolder, nullptr);
    if (g_thread_failed)
    {
        std::fprintf(stderr, "FAIL: a probe thread could not register with the"
                             " collector, so it allocated nothing and every arm"
                             " below would be vacuous\n");
        return 1;
    }

    if (!UnpublishedIsCollected())
    {
        std::fprintf(stderr, "FAIL: an unreferenced payload was never finalized, so"
                             " this host retains it regardless of the barrier and"
                             " every arm below would survive vacuously\n");
        return 1;
    }

    // Calibrate rather than assume: the ballast sets the cycle length, but how
    // many blocks it occupies is the allocator's business. stderr, never stdout —
    // the gate diffs stdout exactly, and it must not vary with the answer.
    if (incremental)
    {
        int cycleIncrements = MeasureCycleLength();
        for (int i = 0; i < 3 && cycleIncrements < kMinCycleIncrements; i++)
        {
            GrowBallast(g_ballast_nodes);
            cycleIncrements = MeasureCycleLength();
        }
        std::fprintf(stderr, "calibration: cycle length %d increments over %d"
                             " ballast nodes\n", cycleIncrements, g_ballast_nodes);
        if (cycleIncrements < kMinCycleIncrements)
        {
            std::fprintf(stderr, "FAIL: cycle length %d increments is under the %d"
                                 " a %d-step budget needs, and growing the ballast"
                                 " did not lengthen it\n",
                         cycleIncrements, kMinCycleIncrements, kSteps);
            return 1;
        }
    }

    int windowHeldMin = kSteps;
    int windowFailedSubject = -1;
    for (int subject = 0; subject < SubjectCount; subject++)
    {
        int lost = 0;
        int windowHeld = 0;
        for (int steps = 0; steps < kSteps; steps++)
        {
            int held = 0;
            lost += RunOnce(subject, steps, &held);
            windowHeld += held;
        }
        std::printf("%s: lost %d of %d\n", kSubjectNames[subject], lost, kSteps);
        if (windowHeld < windowHeldMin)
        {
            windowHeldMin = windowHeld;
            windowFailedSubject = subject;
        }
    }

    if (g_thread_failed)
    {
        std::fprintf(stderr, "FAIL: a probe thread could not register with the"
                             " collector, so its payload was never allocated\n");
        return 1;
    }

    // Stop-the-world has no window by construction, so the count is reported only
    // where it means something.
    if (!incremental)
        return 0;

    std::printf("window held: %d of %d\n", windowHeldMin, kSteps);
    if (windowHeldMin < kSteps)
    {
        std::fprintf(stderr, "FAIL: the mark cycle completed before the store in %d"
                             " of %d trials of '%s', so those trials tested nothing\n",
                     kSteps - windowHeldMin, kSteps, kSubjectNames[windowFailedSubject]);
        return 1;
    }
    return 0;
}
