#!/usr/bin/env bash
# Multi-file split gate: forces the C++ emitter to split method bodies across several
# small translation units (DN2CPP_SPLIT_BYTES) and verifies the parallel-compiled,
# multi-TU native binary links and produces output byte-identical to real .NET.
#
# Exercises the external-linkage split path end to end — generated.h (type layouts +
# external declarations) + generated.cpp (data + entry point) + generated_b{k}.cpp (method
# body chunks) + generated_m{j}.cpp (per-class reflection metadata) — by reusing the
# ArrayCore program (already proven to match real .NET in build-and-run-array-core.sh)
# against the tree-shaken real CoreLib. A tiny per-TU budget guarantees the program spills
# into multiple chunks rather than staying a single generated.cpp; the assertion below
# fails the gate if it does not actually split.
#
# The chunks carry disjoint name spaces because emission STREAMS them — each is written
# the instant it is sealed, so it must be nameable then, and the two streams interleave in
# time. Also asserted here: a re-transpile into a directory holding a previous, larger
# run's chunks sweeps them, so the build glob cannot pick up a leftover TU and fail to link
# on its duplicate definitions.
source "$(dirname "$0")/_common.sh"

# ~100 KB per TU: ArrayCore + real CoreLib emit well over that, so the body chunks split
# into a handful of generated_N.cpp units that compile in parallel.
export DN2CPP_SPLIT_BYTES=100000

# ArrayCore is also transpiled by build-and-run-array-core.sh; claim a distinct
# artifacts dir so the two gates don't clobber each other under the parallel runner.
export DN2CPP_OUT_SUFFIX=-split

# Both asserts run INSIDE the wrapper's cached region, through the gate_extra_asserts
# hook (`gates/_common.sh`), so a warm hit replays a green that includes them instead
# of re-running them against a build this run did not make. $1 is the OUT the wrapper
# transpiled into: spelling that path out again would agree with the DN2CPP_OUT_SUFFIX
# above by construction and disagree with the wrapper's -hwy/-scalar axes, which is how
# a chunk-count assert ends up reading another build's directory.
gate_extra_asserts() {
    local out="$1" n left sweep
    n=$(ls "$out"/generated*.cpp 2>/dev/null | wc -l | tr -d ' ')
    if [ ! -f "$out/generated_b1.cpp" ] || [ ! -f "$out/generated_m1.cpp" ]; then
        echo "FAIL: DN2CPP_SPLIT_BYTES=$DN2CPP_SPLIT_BYTES did not split bodies AND metadata (found $n TU)" >&2
        ls "$out"/generated* >&2
        exit 1
    fi
    echo "OK: split into $n translation units; parallel multi-TU build matches real .NET"

    # Stale-chunk sweep. A re-transpile must remove the chunks a previous, larger run
    # left, or the `generated*.cpp` glob would compile a leftover and the link would die
    # on its duplicate definitions. The realistic shape of that: a directory holding many
    # chunks; re-transpile the same program with splitting OFF, which produces none at
    # all, and every one of them must be gone. A legacy generated_N.cpp is planted
    # alongside, so an output directory written by an older dn2cpp cannot poison the build
    # either.
    #
    # It runs in a SIBLING directory seeded with a copy of the chunks, never in OUT.
    # OUT is the directory the cache key hashed (`_gate_surface_lines`), and this section
    # used to re-transpile straight into it: the key was computed from the pre-sweep
    # surface and committed after that surface had been replaced by a DIFFERENT program's
    # output. Nothing ever went red, because the next run's step 3 rewrites OUT before the
    # key is read again — a property nobody had written down, true only while the
    # re-transpile stays the last thing the gate does, and silently false the moment an
    # assert is appended after it. A gate must not mutate the directory its own green was
    # keyed on; seeding a copy asserts exactly the same thing and cannot.
    sweep="$out-sweep"
    rm -rf "$sweep"; mkdir -p "$sweep"
    cp "$out"/generated* "$sweep/"
    touch "$sweep/generated_1.cpp"
    ( export DN2CPP_SPLIT_BYTES=0
      invoke_cli "samples/dotnet/ArrayCore/bin/$CONFIG/$TFM/ArrayCore.dll" \
          -r "$(locate_corelib)" -o "$sweep" > /dev/null )
    # find, not ls: a glob that matches nothing makes ls exit 1, and under `set -o pipefail`
    # that would fail the gate exactly when the sweep WORKED.
    left=$(find "$sweep" -maxdepth 1 -name 'generated_*.cpp' | wc -l | tr -d ' ')
    if [ "$left" -ne 0 ]; then
        echo "FAIL: an unsplit re-transpile left $left stale chunk(s) behind:" >&2
        find "$sweep" -maxdepth 1 -name 'generated_*.cpp' >&2
        exit 1
    fi
    echo "OK: stale chunks from a previous run are swept before the new ones land"
}

# The sweep runs the CLI a SECOND time, and what it asserts — that a re-transpile deletes
# the chunks it finds — is invisible in the first transpile's output. So the key's surface
# term cannot see a regression in it: a transpiler that stopped sweeping would emit a
# byte-identical OUT and be served a cached green forever. The CLI hash is the term that
# closes that, the same stand-in a behavior gate uses. The chunk-count assert needs nothing
# extra — it only reads OUT, which the surface term already is.
export DN2CPP_GATE_EXTRA_CONTEXT="stale-chunk-sweep|cli:$(_gate_cli_hash)"

corelib_diff_gate ArrayCore
