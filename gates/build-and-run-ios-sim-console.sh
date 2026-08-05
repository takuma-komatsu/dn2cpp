#!/usr/bin/env bash
# iOS-simulator-axis console gate. Transpiles each program against the
# tree-shaken real CoreLib, cross-compiles it for the iOS simulator SDK (the
# IOS_SIM=1 build axis in _common.sh retargets the CMake configure with
# CMAKE_SYSTEM_NAME=iOS + the iphonesimulator sysroot, arm64), runs the plain
# Mach-O binary on a booted simulator via `xcrun simctl spawn`, and diffs the
# stdout exactly against real .NET (`dotnet $app`) — proving the
# C# -> IL -> C++ -> iOS pipeline end to end.
#
# Unlike the wasm axis, the iOS build keeps the full native feature set: the
# Boehm GC (Mach stop-world + the finalizer thread), real OS threads and the
# POSIX PAL all run unchanged on the simulator.
#
# Three sections: StringCore covers the string/formatting core (including the
# float std::to_chars path behind the 16.3 deployment-target floor),
# NestedFinallySubset proves the EH pipeline, and Finalizers proves the Boehm
# GC end to end on iOS — collection under allocation flood, the finalizer
# thread, resurrection, WeakReference, and the intentional finalizer-exception
# abort as its last section (stdout still matches; the abort exit code is not
# part of the oracle, same as the native gate).
#
# Machines without the Xcode toolchain, the iphonesimulator SDK, or an iPhone
# simulator device skip: the suite stays usable without Xcode, but the runner
# reports the gate as SKIPPED rather than passed (gate_skip in _common.sh).
#
# SIMULATOR SHARING. The booted device is a machine-wide singleton this
# gate shares with godot-ios-sim and godot-editor-export-ios, so every window
# that touches it runs under sim_lock (gates/_common.sh): the boot below, and
# each section's `simctl spawn`, which ios_sim_corelib_diff_gate takes the lock
# around itself. The cross-compiles stay outside it — the lock must not become
# a build lock. This gate is NOT in DN2CPP_MACHINE_LOCK_GATES and must not be
# added: that table is the Phase-5 chain membership, and the reasons a chain
# move is the wrong instrument here (it would drop this gate out of the
# SKIP_GODOT=1 suite, and would not even stop it racing chain A) are written
# out at the table.
source "$(dirname "$0")/_common.sh"

if ! command -v xcrun >/dev/null 2>&1 || ! command -v xcodebuild >/dev/null 2>&1; then
    gate_skip "Xcode toolchain (xcrun/xcodebuild) not found"
fi
if ! xcrun --sdk iphonesimulator --show-sdk-path >/dev/null 2>&1; then
    gate_skip "iphonesimulator SDK not installed"
fi
# Snapshot the device list once and match from a here-string, never
# `simctl … | grep -q`: under `set -o pipefail` grep -q matches "iPhone" near the
# top of a long list, exits, SIGPIPEs simctl, and the PIPELINE reports 141 — so a
# machine with simulators right there skips, blaming Xcode. Measured as exactly
# that false skip, with `simctl list devices available` printing a booted iPhone
# the same second. It is the worst place for this bug: a gate_skip prerequisite
# probe, where the failure is fail-OPEN in normal mode and, under
# DN2CPP_REQUIRE_ALL=1, a merge-gate failure pointing at the wrong thing.
_sim_devices="$(xcrun simctl list devices available 2>/dev/null || true)"
if ! grep -q iPhone <<<"$_sim_devices"; then
    gate_skip "no available iPhone simulator device"
fi

export IOS_SIM=1
# Under the lock: `simctl bootstatus -b` boots a shut-down device, and two of
# those on one device at once is the cold-boot race sim_lock was written for —
# unchecked here (`>/dev/null 2>&1` with no `||`), so a failure would take the
# gate out under set -e. Released immediately: what follows is a cross-compile.
sim_lock
IOS_SIM_UDID=$(ensure_booted_sim)
sim_unlock
export IOS_SIM_UDID
echo "simulator: $IOS_SIM_UDID"

ios_sim_corelib_diff_gate StringCore System.Linq
ios_sim_corelib_diff_gate NestedFinallySubset
ios_sim_corelib_diff_gate Finalizers
