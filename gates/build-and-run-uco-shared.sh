#!/usr/bin/env bash
# [UnmanagedCallersOnly] shared-library gate: transpile samples/dotnet/UcoShared
# into a plain shared library (DN2CPP_SHARED — no Godot runtime), then drive it
# from a C++ dlopen host (samples/dotnet/UcoShared/driver.cpp) that
#   - runs the generated `main` once to initialize the runtime,
#   - enables foreign-thread GC registration via the dlsym'd runtime setter,
#   - publishes a delegate thunk, disables the host opt-in, and proves the thunk's
#     one-way registration latch still protects later foreign-thread calls,
#   - calls the exported entry points from the main thread AND from a foreign
#     std::thread (allocation loop big enough to trigger collections),
#   - exercises by-value blittable structs, private-method rooting, and a
#     non-exported callback's address handed out through an exported one,
#   - starts the Task.Run worker pool, quiesces it (dn2cpp_runtime_quiesce
#     must join every worker), and dlcloses the library — the one lane that
#     exercises a real unload after background threads ran,
#   - separately returns from a hosted process while a managed WaitAny worker
#     remains live, exercising process static teardown without a runtime quiesce.
#     The destroyed-mutex abort this guards against needs a non-trivial ~mutex
#     (Apple libc++); where ~mutex is a no-op the run only proves the exit stays
#     clean and bounded with the worker live.
source "$(dirname "$0")/_common.sh"

OUT=artifacts/ucoshared
SAMPLE=samples/dotnet/UcoShared

echo "== 1/5 Building sample C# assembly =="
build_proj "$SAMPLE/UcoShared.csproj"

echo "== 2/5 Transpiling IL -> C++ (with the tree-shaken real CoreLib) =="
# The real CoreLib rides along for the Task.Run section (Func<T> + Result);
# everything reachable still hangs off the [UnmanagedCallersOnly] roots.
corelib=$(locate_corelib)
app="$SAMPLE/bin/$CONFIG/$TFM/UcoShared.dll"
invoke_cli "$app" -r "$corelib" -o "$OUT"
# driver.cpp is a key input beyond the transpile surface: the run drives the
# library through it, so its content must miss the cache when it changes.
if gate_cache_check "$OUT" "uco-shared|$corelib" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" \
        "$SAMPLE/driver.cpp"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/5 Compiling shared library =="
DYLIB="$OUT/$(lib_name ucoshared)"
compile_shared "$OUT" "$DYLIB"

echo "== 4/5 Compiling dlopen driver =="
if is_msvc_compiler; then
    # `-`-prefixed switches (cl.exe treats '-' and '/' as equivalent switch
    # characters) sidestep Git Bash's argv path-mangling, which conflates a
    # leading '/' with a POSIX absolute path.
    cl.exe -nologo -std:c++17 -O2 -EHsc -Fo:"$OUT/driver.obj" -Fe:"$OUT/driver" "$SAMPLE/driver.cpp" >/dev/null
else
    clang++ -std=c++17 -O2 -Wall "$SAMPLE/driver.cpp" -o "$OUT/driver"
fi

echo "== 5/5 Running driver against the shared library =="
EXPECTED='init=0
add=42
swap.x=4
swap.y=3
secret=12345
alloc_main=299995
mul=42
alloc_thread=1199997
mul_thread=2991
task_run=42
quiesce OK
dlclose OK
driver OK
delegate_latch=1'
# Capture the exit status explicitly: `$(...)` inline would swallow it, and a
# driver that aborts in dlclose teardown AFTER printing everything (the exact
# failure mode this gate exists to catch) would pass the diff silently.
set +e
driver_out=$("$OUT/driver" "$DYLIB"); driver_code=$?
set -e
assert_output "$driver_out" "$EXPECTED"
assert_exit_code "$driver_code" 0

EXIT_EXPECTED='init=0
waitany_worker=1
exit_worker_live OK'
set +e
# Bounded: a teardown that deadlocks instead of aborting must fail, not hang.
exit_out=$(run_bounded "$OUT/driver" "$DYLIB" --exit-with-waitany); exit_code=$?
set -e
assert_output "$exit_out" "$EXIT_EXPECTED"
assert_exit_code "$exit_code" 0
gate_cache_commit
