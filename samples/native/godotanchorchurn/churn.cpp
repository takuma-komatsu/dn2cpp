// Deterministic churn probe for the RefCounted anchor table's compaction
// barrier (dn2cpp_godot_sweep_anchored_refcounted, runtime/godot/
// dn2cpp_godot.cpp), linked against the real runtime. The sweep's only engine
// dependency is RefCounted.get_reference_count, so a stubbed get_proc_address
// serves refcounts this probe scripts, and every anchor, keep, drop and
// backwards move below runs the real table code — anchored under refcount 2,
// dropped when the probe lowers it to 1, exactly the traffic that drives the
// real per-frame compaction.
//
// The invariant under test: the sweep moves a surviving entry backwards into a
// freed slot, possibly behind the marker's position inside the table's own
// scan. The table (8 KiB, uncollectable) exceeds one mark increment's credit
// (GC_mark_from spends HBLKSIZE of credit per call, in 1 KiB split chunks), so
// a full-mark cycle paused between increments can sit with the table's head
// scanned clean and its tail unseen; a compaction in that window moves a
// pointer into a region only the barrier can have rescanned, and without it
// the entry is swept while the table still names it.
//
// The window is held by construction, not by racing frames. GC_set_rate(1)
// makes one GC_collect_a_little one GC_mark_from, whose credit reaches at most
// 5080 table bytes from a fresh start; the first kept entry sits at table byte
// 5120, so no single increment can step over the whole droppable region. Each
// trial then steps the cycle one increment at a time until it OBSERVES, via
// GC_is_marked on two sentinel shims, first the fresh-cycle state (both
// unmarked — rules out stale bits from the previous collection) and then the
// split state (head marked, first kept not); only then does the sweep run. A
// trial that never reaches both states fails by name, never vacuously green.
//
// The mutation discipline is gcbarrier.cpp's: shims are allocated and anchored
// from a registered thread joined before the cycle opens, addresses cross
// threads only as complements, and the sentinel queries and the sweep run on
// threads the collector never learns about — so no conservative stack scan can
// mark a shim and save an unbarriered move. The teardown of every trial is the
// reclaim control: the same shims, dropped through the same sweep, must all
// finalize, separating "the barrier saved it" from "this host never reclaims
// it". GC_get_gc_no standing still at sweep time is corroboration only; the
// sentinel mark bits are the direct evidence the scan sat split.

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <thread>

#include <gc.h>
#include <gc_mark.h>

#include "dn2cpp_core.h"
#include "dn2cpp_godot.h"
#include "gdextension_interface.h"

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
const Dn2CppGodotMethod dn2cpp_godot_methods[] = { {} };
const int dn2cpp_godot_method_count = 0;
const Dn2CppGodotNodeClass dn2cpp_godot_node_classes[] = { {} };
const int dn2cpp_godot_node_class_count = 0;
void dn2cpp_godot_init_managed() {}

extern "C" GDExtensionBool dn2cpp_gdext_init(
    GDExtensionInterfaceGetProcAddress p_get_proc_address,
    GDExtensionClassLibraryPtr p_library,
    GDExtensionInitialization* r_initialization);

namespace
{

// ---- stub engine surface ----
// A StringName is one opaque word to extension code, so the stub stores the
// text pointer itself; get_method_bind hands that pointer back as the bind, and
// ptrcall answers by the method name it therefore carries. The instance handle
// is a pointer to the probe's own int32 refcount, mimicking the engine's
// declared-int32 write (low 4 bytes only, per the note at the sweep site).

void StubStringNameNew(GDExtensionUninitializedStringNamePtr r_dest,
                       const char* p_contents, GDExtensionBool)
{
    *reinterpret_cast<const char**>(r_dest) = p_contents;
}

void StubDestroy(GDExtensionTypePtr) {}

GDExtensionPtrDestructor StubGetPtrDestructor(GDExtensionVariantType)
{
    return &StubDestroy;
}

void StubVariantFrom(GDExtensionUninitializedVariantPtr, GDExtensionTypePtr) {}
void StubVariantTo(GDExtensionUninitializedTypePtr, GDExtensionVariantPtr) {}

GDExtensionVariantFromTypeConstructorFunc StubGetVariantFrom(GDExtensionVariantType)
{
    return &StubVariantFrom;
}

GDExtensionTypeFromVariantConstructorFunc StubGetVariantTo(GDExtensionVariantType)
{
    return &StubVariantTo;
}

GDExtensionMethodBindPtr StubGetMethodBind(GDExtensionConstStringNamePtr,
                                           GDExtensionConstStringNamePtr p_method,
                                           GDExtensionInt)
{
    return const_cast<char*>(*reinterpret_cast<const char* const*>(p_method));
}

void StubPtrcall(GDExtensionMethodBindPtr p_bind, GDExtensionObjectPtr p_instance,
                 const GDExtensionConstTypePtr*, GDExtensionTypePtr r_ret)
{
    const char* method = static_cast<const char*>(p_bind);
    if (std::strcmp(method, "get_reference_count") != 0)
    {
        std::fprintf(stderr, "FAIL: unexpected engine call %s — the sweep's engine"
                             " surface grew and this probe's stub no longer covers"
                             " it\n", method);
        std::abort();
    }
    *static_cast<int32_t*>(r_ret) = *static_cast<const int32_t*>(p_instance);
}

// The two register_* stubs exist only to clear dn2cpp_gdext_init's null check;
// nothing here runs the Initialize callback that would call them.
void StubNeverCalled()
{
    std::fprintf(stderr, "FAIL: a class-registration interface function ran, but"
                         " this probe never initializes ClassDB\n");
    std::abort();
}

GDExtensionInterfaceFunctionPtr FakeGetProc(const char* name)
{
    if (std::strcmp(name, "string_name_new_with_latin1_chars") == 0)
        return reinterpret_cast<GDExtensionInterfaceFunctionPtr>(&StubStringNameNew);
    if (std::strcmp(name, "variant_get_ptr_destructor") == 0)
        return reinterpret_cast<GDExtensionInterfaceFunctionPtr>(&StubGetPtrDestructor);
    if (std::strcmp(name, "get_variant_from_type_constructor") == 0)
        return reinterpret_cast<GDExtensionInterfaceFunctionPtr>(&StubGetVariantFrom);
    if (std::strcmp(name, "get_variant_to_type_constructor") == 0)
        return reinterpret_cast<GDExtensionInterfaceFunctionPtr>(&StubGetVariantTo);
    if (std::strcmp(name, "classdb_get_method_bind") == 0)
        return reinterpret_cast<GDExtensionInterfaceFunctionPtr>(&StubGetMethodBind);
    if (std::strcmp(name, "object_method_bind_ptrcall") == 0)
        return reinterpret_cast<GDExtensionInterfaceFunctionPtr>(&StubPtrcall);
    if (std::strcmp(name, "classdb_register_extension_class4") == 0
        || std::strcmp(name, "classdb_register_extension_class_method") == 0
        || std::strcmp(name, "classdb_register_extension_class_signal") == 0)
        return reinterpret_cast<GDExtensionInterfaceFunctionPtr>(&StubNeverCalled);
    // Everything else stays null: an unexpected engine call crashes on the null
    // rather than quietly answering.
    return nullptr;
}

// ---- probe proper ----

// The shape the sweep reads: an object header, then the engine handle at
// offset sizeof(Dn2CppObject).
struct FakeShim
{
    Dn2CppObject obj;
    void* handle;
};

// 1024 fills today's table exactly (the anchor cap), and the fill must at
// least cover both sides of the sentinel gap; a smaller cap makes an anchor
// call fail and the probe says so.
const int kShimCount = 1024;
// First kept entry at table byte 5120 — past the 5080 bytes one fresh
// increment's credit can reach, so no increment jumps the gap (header comment).
const int kDropCount = 640;
const int kKeepCount = kShimCount - kDropCount;
const int kTrials = 16;
const int kMaxSteps = 200000;

int32_t g_refcounts[kShimCount];
uintptr_t g_hidden[kShimCount]; // complements: never a scannable pointer
volatile int g_kept_finalized;
volatile bool g_thread_failed;
volatile bool g_anchor_failed;

void GC_CALLBACK OnKeptFinalize(void* /*obj*/, void* /*client*/)
{
    g_kept_finalized = g_kept_finalized + 1;
}

DN2CPP_NOINLINE void RunUnregistered(void (*body)(void*), void* arg)
{
    std::thread worker([body, arg] { body(arg); });
    worker.join();
}

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

// Allocate and anchor a full table: droppables first (the low slots the sweep
// frees), kept after (the high slots it moves backwards from). Runs registered
// — it allocates — and leaves no pointer anywhere but the table and the
// complements.
void SetupShims(void*)
{
    for (int i = 0; i < kShimCount; i++)
    {
        FakeShim* s = static_cast<FakeShim*>(dn2cpp_alloc(sizeof(FakeShim)));
        s->obj.type = nullptr;
        g_refcounts[i] = 2;
        s->handle = &g_refcounts[i];
        if (i >= kDropCount)
            GC_register_finalizer_no_order(s, OnKeptFinalize, nullptr, nullptr, nullptr);
        g_hidden[i] = ~reinterpret_cast<uintptr_t>(s);
        if (dn2cpp_godot_try_anchor_refcounted(&s->obj) == 0)
            g_anchor_failed = true;
    }
}

struct SentinelQuery
{
    int headMarked;
    int keptMarked;
};

void QuerySentinels(void* arg)
{
    SentinelQuery* q = static_cast<SentinelQuery*>(arg);
    q->headMarked = GC_is_marked(reinterpret_cast<void*>(~g_hidden[0]));
    q->keptMarked = GC_is_marked(reinterpret_cast<void*>(~g_hidden[kDropCount]));
}

struct SweepRequest
{
    GC_word cycle;
    int cycleStood;
};

void SweepBody(void* arg)
{
    SweepRequest* req = static_cast<SweepRequest*>(arg);
    req->cycleStood = GC_get_gc_no() == req->cycle ? 1 : 0;
    dn2cpp_godot_sweep_anchored_refcounted();
}

void SweepOnly(void*)
{
    dn2cpp_godot_sweep_anchored_refcounted();
}

// Drop everything still anchored and prove the host reclaims it: the per-trial
// reclaim control, and what leaves the table empty for the next trial. Answers
// the number of kept finalizers that have run in total for this trial.
DN2CPP_NOINLINE int Teardown()
{
    for (int i = 0; i < kShimCount; i++)
        g_refcounts[i] = 1;
    RunUnregistered(SweepOnly, nullptr);
    for (int i = 0; i < 3 && g_kept_finalized < kKeepCount; i++)
    {
        GC_gcollect();
        GC_invoke_finalizers();
    }
    return g_kept_finalized;
}

} // namespace

int main()
{
    dn2cpp_runtime_init();

    const bool incremental = GC_is_incremental_mode() != 0;
    std::printf("incremental mode: %s\n", incremental ? "on" : "off");

    GDExtensionInitialization init;
    std::memset(&init, 0, sizeof(init));
    if (!dn2cpp_gdext_init(&FakeGetProc, nullptr, &init))
    {
        std::fprintf(stderr, "FAIL: dn2cpp_gdext_init rejected the stub interface\n");
        return 1;
    }

    // A zero mark time limit is what makes increments minimal at every heap
    // size, and rate 1 narrows one GC_collect_a_little to one GC_mark_from —
    // the credit arithmetic the sentinel gap is sized against.
    GC_set_time_limit(0);
    GC_set_rate(1);
    // Make every trial cycle a FULL mark — the opposite of gcbarrier.cpp's
    // setting, because the subjects differ: a partial cycle keeps yesterday's
    // mark bits and never rescans a clean table, so anchored shims survive it
    // without the table being scanned at all and the split this probe waits for
    // never exists. The table's split scan happens in the full cycle's
    // clear-marks-and-rescan-everything mark, which is where the sweep's
    // backwards move can land behind the marker.
    GC_set_full_freq(0);

    // Prewarm: one anchor + two sweeps outside any cycle caches the
    // get_reference_count bind, so the mid-cycle sweep below resolves nothing.
    g_kept_finalized = 0;
    RunRegistered([](void*) {
        FakeShim* s = static_cast<FakeShim*>(dn2cpp_alloc(sizeof(FakeShim)));
        s->obj.type = nullptr;
        g_refcounts[0] = 2;
        s->handle = &g_refcounts[0];
        if (dn2cpp_godot_try_anchor_refcounted(&s->obj) == 0)
            g_anchor_failed = true;
    }, nullptr);
    RunUnregistered(SweepOnly, nullptr); // refcount 2: kept, bind now cached
    g_refcounts[0] = 1;
    RunUnregistered(SweepOnly, nullptr); // dropped: table empty again
    GC_gcollect();
    GC_invoke_finalizers();

    int totalLost = 0;
    int totalReclaimed = 0;
    int windowHeld = 0;
    for (int trial = 0; trial < kTrials; trial++)
    {
        g_kept_finalized = 0;
        RunRegistered(SetupShims, nullptr);
        if (g_thread_failed || g_anchor_failed)
        {
            std::fprintf(stderr, g_thread_failed
                ? "FAIL: a probe thread could not register with the collector\n"
                : "FAIL: an anchor call failed — the table's capacity fell below"
                  " this probe's fill; re-derive the fill and the sentinel gap\n");
            return 1;
        }
        GC_gcollect();
        GC_invoke_finalizers();
        if (g_kept_finalized != 0)
        {
            std::fprintf(stderr, "FAIL: %d anchored kept shims were reclaimed by a"
                                 " full collection before the trial began — anchoring"
                                 " itself does not hold them\n", g_kept_finalized);
            return 1;
        }

        // The refcount traffic: every droppable's engine count falls to its
        // baseline, so the next sweep compacts while nothing inserts.
        for (int i = 0; i < kDropCount; i++)
            g_refcounts[i] = 1;

        SweepRequest req;
        req.cycle = GC_get_gc_no();
        req.cycleStood = 0;
        bool sawFresh = false, window = false;
        GC_start_incremental_collection();
        for (int step = 0; step < kMaxSteps; step++)
        {
            int more = GC_collect_a_little();
            SentinelQuery q = { 0, 0 };
            RunUnregistered(QuerySentinels, &q);
            if (!sawFresh)
            {
                if (!q.headMarked && !q.keptMarked)
                    sawFresh = true;
            }
            else if (q.headMarked && !q.keptMarked)
            {
                window = true;
                break;
            }
            if (more == 0)
                break; // the cycle ended without the split ever being visible
        }

        if (incremental && !window)
        {
            std::fprintf(stderr, "FAIL: trial %d never observed the table's scan"
                                 " split (fresh state seen: %d) — the compaction"
                                 " below would test nothing\n", trial, sawFresh ? 1 : 0);
            return 1;
        }
        if (window)
            windowHeld++;

        RunUnregistered(SweepBody, &req);
        // Corroboration only — the sentinel bits above are the direct evidence.
        // An advanced gc_no here would mean something drove the collector
        // between the observation and the sweep, and the observation is stale.
        if (incremental && req.cycleStood == 0)
        {
            std::fprintf(stderr, "FAIL: trial %d's cycle advanced between the"
                                 " sentinel observation and the sweep\n", trial);
            return 1;
        }

        for (int i = 0; i < kMaxSteps && GC_collect_a_little() != 0; i++)
        {
        }
        GC_invoke_finalizers();
        int lost = g_kept_finalized;
        totalLost += lost;
        if (lost != 0)
        {
            // Report before Teardown: its sweep would walk the freed entries
            // this loss left dangling in the table.
            std::printf("kept shims lost: %d of %d\n", totalLost, kTrials * kKeepCount);
            std::fprintf(stderr, "FAIL: trial %d lost %d kept shims to the paused"
                                 " cycle — the sweep's backwards move escaped the"
                                 " marker\n", trial, lost);
            return 1;
        }

        int reclaimed = Teardown();
        totalReclaimed += reclaimed;
        if (reclaimed != kKeepCount)
        {
            std::fprintf(stderr, "FAIL: teardown reclaimed %d of %d kept shims —"
                                 " this host retains them regardless of the barrier,"
                                 " so the trials above were vacuous\n",
                         reclaimed, kKeepCount);
            return 1;
        }
    }

    std::printf("kept shims lost: %d of %d\n", totalLost, kTrials * kKeepCount);
    std::printf("reclaimed at teardown: %d of %d\n", totalReclaimed, kTrials * kKeepCount);
    if (!incremental)
        return 0;

    // Stop-the-world has no window by construction, so the count is reported
    // only where it means something.
    std::printf("window held: %d of %d\n", windowHeld, kTrials);
    return 0;
}
