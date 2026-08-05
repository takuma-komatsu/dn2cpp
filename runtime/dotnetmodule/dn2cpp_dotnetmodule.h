//
// Godot .NET-module (mono module) host runtime. The transpiler's --dotnet-module
// backend emits a godotsharp_game_main_init export into generated.cpp — the
// entry the engine's mono module calls on the library it loads in place of the
// hostfxr-managed game assembly (NativeAOT drop-in style). This unit provides
// the small host-side pieces that entry and the per-frame callback need: GC
// thread registration for engine threads and the wrapped frame callback that
// drains managed finalizers on the engine's main thread.

#pragma once

#include "dn2cpp.h"

// Register the calling thread with the Boehm collector (once per thread, cached
// in a thread_local). The engine calls the module entry and the managed-callback
// table from threads it spawned itself, which Boehm knows nothing about; an
// unregistered thread's stack is never scanned and a collection triggered from
// it aborts. Call at the top of every entry point the engine can invoke.
void dn2cpp_dotnetmodule_thread_guard();

// The per-frame callback pair. godotsharp_game_main_init lets the transpiled
// ManagedCallbacks.Create fill the callback table, then reads the managed
// ScriptManagerBridge_FrameCallback pointer out of its slot, hands it to the
// setter below, and overwrites the slot with dn2cpp_dm_frame_callback — so the
// engine's once-per-frame call first runs the runtime's frame work (thread
// guard + scheduler frame pump + manual finalizer drain — the pump is the
// native mirror of GodotTaskScheduler.Activate, resuming main-thread Task
// continuations and real-time Task.Delay; the drain keeps RefCounted teardown
// main-thread-affine) and then tail-calls the original managed callback.
void dn2cpp_dm_set_managed_frame_callback(void (*callback)());
void dn2cpp_dm_frame_callback();

// The main-thread SynchronizationContext bootstrap + drain. The managed
// GodotTaskScheduler install stays cut (its TaskScheduler base is an intrinsic
// type no transpiled class can derive from), so the generated module epilogue
// owns a GodotSynchronizationContext singleton natively:
//   - dn2cpp_dm_sync_ctx() (DEFINED BY THE GENERATED EPILOGUE, declared here so
//     any body TU can reference it) lazily creates the singleton, installs it
//     as the creating thread's SynchronizationContext.Current, and returns it.
//     The first caller is the frame pump below, on the engine main thread.
//   - the pump registered here runs once per frame after dn2cpp_sched_pump():
//     it ensures the singleton exists and drains its queue by running the
//     transpiled ExecutePendingContinuations — the missing half of
//     GodotTaskScheduler.Activate (worker-thread Post -> main-thread run).
Dn2CppObject* dn2cpp_dm_sync_ctx();
void dn2cpp_dm_set_sync_ctx_pump(void (*pump)());

// The deferred startup static-constructor pass. The module entry must NOT run
// the eager cctor pass inside godotsharp_game_main_init: the engine copies the
// managed-callback table into its GDMonoCache only after the entry returns, so
// a cctor that reaches back into the engine (e.g. an Engine-singleton wrapper
// creation, which the engine serves by calling the CreateManagedForGodot-
// ObjectBinding managed callback) would jump through a still-null slot. The
// entry registers the generated pass here instead; the wrapped frame callback
// runs it exactly once, at the top of the first engine frame — the earliest
// point where the engine-side callback cache is guaranteed live. Until then,
// cctors run lazily through the use-site first-use guards, matching .NET.
void dn2cpp_dm_set_deferred_cctor_pass(void (*pass)());

// Env-gated stage tracing (DN2CPP_DM_TRACE=1): the module entry and the frame
// callback print positive "dn2cpp-dm: <stage>" markers to stderr so an engine
// E2E run can assert the handshake actually happened (a failed .NET init only
// *logs* an error — the engine keeps running and exits 0, so absence of errors
// proves nothing). Off by default: one cached getenv, no output when unset.
int dn2cpp_dm_trace_enabled();
void dn2cpp_dm_trace(const char* fmt, ...);
