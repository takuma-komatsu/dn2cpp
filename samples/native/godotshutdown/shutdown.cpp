// Force collections in pending finalizers and the tracker while exercising the
// real shutdown callback. A joined allocating thread leaves no conservative
// stack root that could keep the tracker's short-weak target alive by accident.

#include <cstdio>
#include <thread>

#include <gc.h>

#include "dn2cpp_core.h"
#include "dn2cpp_dotnetmodule.h"

// Native probes supply the empty tables normally emitted in generated.cpp.
const Dn2CppTypeRegEntry dn2cpp_type_registry[] = { {} };
const Dn2CppMetadataBlock dn2cpp_metadata_blocks[] = { {} };
const std::size_t dn2cpp_metadata_block_count = 1;
const int32_t dn2cpp_type_registry_count = 0;
const Dn2CppTypeBind dn2cpp_type_binds[] = { {} };
const int32_t dn2cpp_type_bind_count = 0;
const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[] = { {} };
const int32_t dn2cpp_assembly_registry_count = 0;
const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[] = { {} };
const int32_t dn2cpp_delegate_refl_registry_count = 0;
const Dn2CppGvmRowDispatch dn2cpp_gvm_row_dispatch[] = { {} };
const int32_t dn2cpp_gvm_row_dispatch_count = 0;
const Dn2CppVirtualMethodTarget* const dn2cpp_virtual_method_targets[] = { nullptr };
const int32_t dn2cpp_virtual_method_target_count = 0;
const Dn2CppItfImplSlots dn2cpp_itf_impl_slots[] = { {} };
const int32_t dn2cpp_itf_impl_slot_count = 0;
const Dn2CppRenamedSlotBody dn2cpp_renamed_slot_bodies[] = { {} };
const int32_t dn2cpp_renamed_slot_body_count = 0;
const Dn2CppBclMessage dn2cpp_bcl_messages[] = { {} };
const int32_t dn2cpp_bcl_message_count = 0;
const int32_t dn2cpp_exception_get_message_slot = -1;
bool dn2cpp_argument_exception_store(Dn2CppObject*, Dn2CppString*, Dn2CppObject*) { return false; }
bool dn2cpp_object_disposed_exception_store(Dn2CppObject*, Dn2CppString*) { return false; }
const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = nullptr;
const int32_t dn2cpp_runtime_template_count = 0;

namespace
{

enum class Kind { Control, Pending, Tracked };
struct ProbeObject : Dn2CppObject { Kind kind; };
struct TrackerFailure {};

std::thread::id g_main_thread;
DN2CPP_GC_STATIC_ROOT intptr_t g_weak;
int g_control_finalized;
int g_pending_finalized;
int g_tracked_finalized;
int g_tracker_calls;
int g_disposed;
bool g_pending_collection_paused;
bool g_helpers_disposed;

void Require(bool condition, const char* message)
{
    if (!condition)
    {
        std::fprintf(stderr, "FAIL: shutdown probe: %s\n", message);
        dn2cpp_environment_exit(1);
    }
}

void Finalize(Dn2CppObject* object)
{
    Require(std::this_thread::get_id() == g_main_thread, "finalizer ran off the main thread");
    Require(!g_helpers_disposed, "finalizer ran after tracker helper disposal");
    switch (static_cast<ProbeObject*>(object)->kind)
    {
        case Kind::Control:
            ++g_control_finalized;
            break;
        case Kind::Pending:
        {
            ++g_pending_finalized;
            int32_t before = dn2cpp_gc_collection_count();
            dn2cpp_gc_collect();
            g_pending_collection_paused = dn2cpp_gc_collection_count() == before;
            break;
        }
        case Kind::Tracked:
            ++g_tracked_finalized;
            break;
    }
}

const Dn2CppTypeInfo g_probe_type = [] {
    Dn2CppTypeInfo type{};
    type.name = "ShutdownProbe.Object";
    type.instanceSize = static_cast<int32_t>(sizeof(ProbeObject));
    type.finalize = Finalize;
    return type;
}();

void Allocate(Kind kind)
{
    std::thread worker([kind] {
        struct GC_stack_base stack;
        Require(GC_get_stack_base(&stack) == GC_SUCCESS
            && GC_register_my_thread(&stack) == GC_SUCCESS, "allocating thread registration failed");
        auto* object = static_cast<ProbeObject*>(dn2cpp_alloc(sizeof(ProbeObject)));
        object->type = &g_probe_type;
        object->kind = kind;
        dn2cpp_register_finalizer(object);
        g_weak = dn2cpp_gchandle_internal_alloc(object, 0);
        GC_unregister_my_thread();
    });
    worker.join();
}

void ReleaseWeak()
{
    dn2cpp_gchandle_internal_free(g_weak);
    g_weak = 0;
}

void RequireCollectionResumed()
{
    int32_t before = dn2cpp_gc_collection_count();
    dn2cpp_gc_collect();
    Require(dn2cpp_gc_collection_count() != before, "collection stayed paused after callback");
}

void Tracker()
{
    ++g_tracker_calls;
    Require(g_pending_finalized == 1, "pending finalizer was not drained before tracker");
    int32_t before = dn2cpp_gc_collection_count();
    dn2cpp_gc_collect();
    Dn2CppObject* target = dn2cpp_gchandle_internal_get(g_weak);
    Require(target != nullptr, "shutdown collection cleared tracker target");
    Require(dn2cpp_gc_collection_count() == before, "tracker collection was not paused");
    Require(g_pending_collection_paused, "pending finalizer collection was not paused");
    Require(g_tracked_finalized == 0 && g_disposed == 0, "tracked target was released twice");
    ++g_disposed;
    dn2cpp_gc_suppress_finalize(target);
    g_helpers_disposed = true;
}

void CheckPausedTracker()
{
    ++g_tracker_calls;
    int32_t before = dn2cpp_gc_collection_count();
    dn2cpp_gc_collect();
    Require(dn2cpp_gc_collection_count() == before, "nested tracker collection was not paused");
}

void ThrowingTracker()
{
    CheckPausedTracker();
    throw TrackerFailure{};
}

} // namespace

int main()
{
    dn2cpp_gc_set_manual_finalizer_drain(1);
    dn2cpp_runtime_init();
    g_main_thread = std::this_thread::get_id();

    Allocate(Kind::Control);
    dn2cpp_gc_collect();
    Require(dn2cpp_gchandle_internal_get(g_weak) == nullptr, "control stayed weakly reachable");
    dn2cpp_gc_wait_for_pending_finalizers();
    Require(g_control_finalized == 1, "control finalizer did not run once");
    ReleaseWeak();
    std::puts("shutdown control: collection=observed target=finalized");

    Allocate(Kind::Pending);
    dn2cpp_gc_collect();
    Require(dn2cpp_gchandle_internal_get(g_weak) == nullptr, "pending object stayed reachable");
    Require(g_pending_finalized == 0, "pending finalizer ran before shutdown");
    ReleaseWeak();
    {
        Dn2CppGcCollectionPause pause;
        Allocate(Kind::Tracked);
    }
    dn2cpp_dm_set_managed_shutdown_callback(Tracker);
    dn2cpp_dm_shutdown_callback();
    Require(g_tracker_calls == 1 && g_disposed == 1, "tracker did not dispose its target once");
    RequireCollectionResumed();
    dn2cpp_gc_wait_for_pending_finalizers();
    Require(g_tracked_finalized == 0, "disposed tracker target was finalized");
    ReleaseWeak();
    std::puts("shutdown pause: pending=drained tracker=disposed collection=restored");

    dn2cpp_dm_set_managed_shutdown_callback(CheckPausedTracker);
    {
        Dn2CppGcCollectionPause outer;
        dn2cpp_dm_shutdown_callback();
        int32_t before = dn2cpp_gc_collection_count();
        dn2cpp_gc_collect();
        Require(dn2cpp_gc_collection_count() == before, "callback released an enclosing pause");
    }
    Require(g_tracker_calls == 2, "nested tracker did not run");
    RequireCollectionResumed();
    std::puts("shutdown pause: nested=preserved collection=restored");

    dn2cpp_dm_set_managed_shutdown_callback(ThrowingTracker);
    bool caught = false;
    try
    {
        dn2cpp_dm_shutdown_callback();
    }
    catch (const TrackerFailure&)
    {
        caught = true;
    }
    Require(caught && g_tracker_calls == 3, "tracker exception did not escape");
    RequireCollectionResumed();
    std::puts("shutdown pause: exception=propagated collection=restored");
    dn2cpp_main_exit(0);
    return 0;
}
