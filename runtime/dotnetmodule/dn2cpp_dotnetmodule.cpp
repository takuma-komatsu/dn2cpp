//
// Godot .NET-module (mono module) host runtime — see dn2cpp_dotnetmodule.h.

#include "dn2cpp_dotnetmodule.h"

#include <cstdarg>
#include <cstdio>
#include <cstdlib>
#include <cstring>

int dn2cpp_dm_trace_enabled()
{
    // Off unless DN2CPP_DM_TRACE says so, on every target. A browser has no
    // environment block, and a compile-time default for it is worse than no trace:
    // emscripten sends stderr to Module.printErr, which the Godot web shell shows
    // the player as console.error. The wasm gate's host sets the variable itself.
    //
    // Read once; the first call happens on the engine's init thread before any
    // other thread can reach the runtime, so the plain static is race-free.
    static int s_enabled = -1;
    if (s_enabled < 0)
    {
        const char* v = std::getenv("DN2CPP_DM_TRACE");
        s_enabled = (v != nullptr && *v != '\0' && std::strcmp(v, "0") != 0) ? 1 : 0;
    }
    return s_enabled;
}

void dn2cpp_dm_trace(const char* fmt, ...)
{
    if (!dn2cpp_dm_trace_enabled())
        return;
    std::va_list args;
    va_start(args, fmt);
    std::fprintf(stderr, "dn2cpp-dm: ");
    std::vfprintf(stderr, fmt, args);
    std::fputc('\n', stderr);
    std::fflush(stderr);
    va_end(args);
}

// Ensure the calling thread is registered with the collector before any GC
// activity. Kept as a named entry point because GENERATED code calls it
// (DotnetModuleBackend emits it at the top of every engine-invocable body);
// the body is the core's shared register-once-and-leave hook, which also
// serves the GDExtension lane — see dn2cpp_gc_ensure_thread_registered in
// runtime/core for the full rationale.
void dn2cpp_dotnetmodule_thread_guard()
{
    dn2cpp_gc_ensure_thread_registered();
}

// The original managed ScriptManagerBridge_FrameCallback, read out of the
// ManagedCallbacks table before its slot is overwritten with the wrapper below.
// Written once during godotsharp_game_main_init (before the engine's main loop
// starts), read once per frame on the engine's main thread — no synchronization
// needed.
static void (*g_managed_frame_callback)() = nullptr;

void dn2cpp_dm_set_managed_frame_callback(void (*callback)())
{
    g_managed_frame_callback = callback;
}

// The generated SynchronizationContext pump — see the header. Written once
// during init, called once per frame on the engine main thread.
static void (*g_sync_ctx_pump)() = nullptr;

void dn2cpp_dm_set_sync_ctx_pump(void (*pump)())
{
    g_sync_ctx_pump = pump;
}

// The generated startup-cctor pass, deferred to the first frame — see the
// header for why it cannot run inside godotsharp_game_main_init. Written once
// during init, consumed once on the main thread's first frame.
static void (*g_deferred_cctor_pass)() = nullptr;

void dn2cpp_dm_set_deferred_cctor_pass(void (*pass)())
{
    g_deferred_cctor_pass = pass;
}

// Once-per-frame callback, installed in the managed-callback table's
// FrameCallback slot. The engine calls it on the main thread every process
// frame, which makes it the .NET-module twin of the godot lane's MainLoopFrame:
// manual finalizer-drain mode has no background thread, so without this drain
// queued RefCounted teardowns (engine object destroy / unreference) would pile
// up until module exit. It also frame-pumps the main thread's cooperative
// scheduler — the native mirror of GodotSharp's per-frame
// GodotTaskScheduler.Activate(), whose managed install stays cut — so a
// continuation posted cross-thread (await Task.Run) or a pending Task.Delay
// resumes on the main thread instead of sitting on the run queue forever.
// Order: deferred cctors first (a resumed continuation sees initialized
// statics), then the pump, then the finalizer drain (a final reference the
// pump dropped is reclaimed the same frame, keeping the main-thread affinity
// chosen at init); the original managed callback (script method scheduling)
// runs last.
// Run one stage of the frame callback with the host-boundary contract applied:
// a managed fault stops here, is reported through the engine's error log, and
// the remaining stages still run.
//
// This is what makes the wrapper safe. The engine calls
// dn2cpp_dm_frame_callback through a C function pointer read out of the
// managed-callback table; the frame above it is CSharpLanguage::frame(), which
// has no landing pad, so an exception leaving any stage below is
// std::terminate — the process, mid-game.
//
// The hazard is one dn2cpp CREATED, which is why it is fixed here rather than
// at each stage's author: in real GodotSharp this work runs inside
// ScriptManagerBridge.FrameCallback, one try/catch around
// GodotTaskScheduler.Activate(). dn2cpp cuts Activate (its TaskScheduler base is
// an intrinsic type no transpiled class can derive from) and mirrors its two
// halves natively — the scheduler pump and the SynchronizationContext drain —
// moving them out of that try/catch and into this C frame. Restoring the handler
// restores .NET parity; it invents no new policy.
//
// The same criterion is why the two unguarded GodotSharp bridge callbacks
// (ScriptManagerBridge.GetGlobalClassName, GetOrCreateScriptBridgeForPath) are
// NOT wrapped: dn2cpp transpiles that IL verbatim and real .NET has the
// identical hole at the identical place. Wrapping them would mean a
// hand-written list of third-party member names this tree cannot derive or
// check — a re-pin adding an unguarded callback leaves the list wrong in the
// fail-OPEN direction — and would make dn2cpp diverge from the runtime it is a
// drop-in for on a shipped-game failure path, where upstream answers a failed
// bridge with "Could not create C# script" and carries on scriptless. That fix
// belongs upstream, inside the method that needs it.
static void dm_frame_stage(const char* stage, void (*fn)())
{
    try
    {
        fn();
    }
    catch (Dn2CppException& ex)
    {
        dn2cpp_report_boundary_exception(ex.obj, "the engine frame callback (%s)", stage);
    }
}

void dn2cpp_dm_frame_callback()
{
    // First-frame-only trace marker: proves the engine's main loop reached the
    // wrapped callback. Main-thread only (the engine calls this once per
    // process frame on the main thread), so the plain static is race-free.
    static bool s_first_frame_traced = false;
    if (!s_first_frame_traced)
    {
        s_first_frame_traced = true;
        dn2cpp_dm_trace("frame");
    }
    dn2cpp_dotnetmodule_thread_guard();
    if (g_deferred_cctor_pass != nullptr)
    {
        void (*pass)() = g_deferred_cctor_pass;
        g_deferred_cctor_pass = nullptr; // clear first: the pass itself may pump a frame
        dm_frame_stage("startup cctors", pass);
    }
    dm_frame_stage("scheduler pump", &dn2cpp_sched_pump);
    // Drain the main-thread SynchronizationContext after the scheduler pump: a
    // continuation the pump resumed may Post follow-up work, which then still
    // runs this same frame. The first call also creates + installs the
    // singleton, binding it to this (the engine main) thread.
    //
    // This stage is the one with no managed handler anywhere beneath it:
    // GodotSynchronizationContext.ExecutePendingContinuations runs each queued
    // SendOrPostCallback bare, exactly as upstream wrote it, because upstream's
    // caller was the protected FrameCallback.
    if (g_sync_ctx_pump != nullptr)
        dm_frame_stage("SynchronizationContext drain", g_sync_ctx_pump);
    // Deliberately UNGUARDED: dn2cpp_run_finalizer_body already decides what a
    // throwing finalizer costs, and it decides "the process" — because that is
    // what real .NET does, and because the finalizers after it are then
    // correctly left un-run rather than papered over. Catching here would
    // silently overrule a parity decision made elsewhere; it is also
    // unreachable, since that site fails fast rather than propagating.
    dn2cpp_gc_drain_finalizers();
    // The original managed callback protects itself — ScriptManagerBridge.
    // FrameCallback is one try/catch around its whole body — but it is reached
    // through a raw function pointer, so nothing here can check that and the
    // guard costs an untaken branch.
    if (g_managed_frame_callback != nullptr)
        dm_frame_stage("managed frame callback", g_managed_frame_callback);
}
