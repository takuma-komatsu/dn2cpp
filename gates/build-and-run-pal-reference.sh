#!/usr/bin/env bash
# The PORTING CONTRACT gate. It asserts the three claims docs/PORTING.md
# makes to somebody bringing dn2cpp up on a target this repository has never seen,
# each of which was previously a promise nothing checked:
#
#   1. "A port is one directory under runtime/core/platform/ plus a CMake arm; no
#      core file changes."  -> section 3 builds the whole runtime against
#      runtime/core/platform/reference/, a PAL implementation that names no
#      operating system, and section 4 runs a real transpiled program on it.
#   2. "Console output goes through the seam, so a target with no stdout only has
#      to implement two functions."  -> section 5 installs a sink through the
#      reference target's hook and asserts that every byte a program prints
#      arrives there instead of on stdout.
#   3. "-DDN2CPP_USE_GC=OFF is the supported retreat for a target with no working
#      conservative collector."  -> section 6 builds and runs on the calloc
#      fallback. Nothing had ever built it: DN2CPP_NO_GC was documented and
#      plumbed, and setting it reconfigured the SHARED runtime build dir with the
#      GC off, so the first gate to use it would have silently disarmed the
#      collector for every gate that ran afterwards. That is fixed in
#      _common.sh (the axis has its own dir now) and asserted here.
#
# Sections 1 and 2 are the cheap textual half and run first because they are the
# ones that fail on a typo: the seam's declared set against every implementation
# of it, and the reference target's refusal set against what it claims to refuse.
#
# WHY A NEW BUCKET rather than a section in an existing one (see AGENTS.md,
# "Consolidated gate structure", and PORTING.md §4): a genuinely new target is
# one of the few legitimate reasons. Every section here needs a
# runtime configured with a cache variable no other gate sets, so folding it into
# a themed bucket would mean that bucket building the runtime twice for reasons
# that have nothing to do with its theme.
#
# WHAT IT DOES NOT ASSERT, stated so nobody reads more into a green than is here:
# the reference target runs on a POSIX host, and it keeps that host's
# SystemNative_* and mmap TUs (see runtime/CMakeLists.txt's DN2CPP_PAL_REFERENCE
# arm for why those are separate seams). So this gate covers §2.1's seam and says
# nothing about §2.2's question — "which CoreLib flavour will the target's
# programs be transpiled from, and what does IT P/Invoke" — which on a real
# non-POSIX target is the larger part of the port. No in-repo axis can cover that
# one; it needs the target.
source "$(dirname "$0")/_common.sh"

PAL_H=runtime/core/platform/dn2cpp_pal.h
REF_CPP=runtime/core/platform/reference/dn2cpp_pal_reference.cpp

fail() { echo "FAIL: $*" >&2; exit 1; }

# ── 1/6 The seam's declared set, and every implementation of it ───────────────
#
# This is the question a porter asks first: what do I have to write? The answer
# is derived here, not written down, and diffed
# against each platform/*/ directory that supplies a PAL.
#
# The `grep -v '^static'` is not decoration. docs/PORTING.md used to hand a porter
# a parity command without it, which counts the POSIX file's two file-local locale
# helpers and reports 18 entries against the header's 16 — a "parity check" whose
# three numbers never matched each other and which therefore proved nothing about
# parity in either direction.
echo "== 1/6 The PAL seam: declared set vs every implementation =="
pal_names() { grep -E '^[A-Za-z_].*\bdn2cpp_pal_[a-z_0-9]+\(' "$1" \
    | grep -v '^static' | grep -oE 'dn2cpp_pal_[a-z_0-9]+' | sort -u; }

required=$(pal_names "$PAL_H")
[ -n "$required" ] || fail "derived an EMPTY required set from $PAL_H — the derivation is broken, not the tree"
echo "   the seam declares $(printf '%s\n' "$required" | wc -l | tr -d ' ') functions:"
printf '%s\n' "$required" | sed 's/^/     /'

for impl in runtime/core/platform/*/dn2cpp_pal_*.cpp; do
    case "$impl" in *_reference.cpp|*posix*|*windows*|*wasm*) ;; *) continue ;; esac
    have=$(pal_names "$impl")
    missing=$(comm -23 <(printf '%s\n' "$required") <(printf '%s\n' "$have") | tr '\n' ' ')
    extra=$(comm -13 <(printf '%s\n' "$required") <(printf '%s\n' "$have") | tr '\n' ' ')
    [ -z "${missing// }" ] || fail "$impl does not define: $missing"
    # An extra dn2cpp_pal_* definition is a failure too, and not a pedantic one: it
    # is a function a porter reading one implementation would copy into theirs
    # while nothing in the header obliges it, i.e. the seam growing a member on one
    # target only. Name it something outside the dn2cpp_pal_ prefix instead — the
    # reference target's console-sink hook is spelled dn2cpp_reference_* for
    # exactly this reason.
    [ -z "${extra// }" ] || fail "$impl defines dn2cpp_pal_* entries the seam does not declare: $extra"
    echo "   OK $impl — all $(printf '%s\n' "$have" | wc -l | tr -d ' ') defined"
done

# ── 2/6 The reference target refuses exactly what it says it refuses ──────────
#
# The reference target answers one MUST entry with a loud abort rather than a
# plausible value (dn2cpp_pal_malloc_usable_size: C++17 has no usable-size query,
# and the caller uses the answer as a memcpy length, so a fabricated 0 truncates
# every AlignedRealloc and a fabricated large value reads off the end of the
# block). That is a deliberate, documented hole, and the thing that makes it a
# declaration rather than a shrug is that its SIZE is pinned: a second refusal
# added later without a matching update to the docs is a target quietly getting
# less complete, which is the fail-open direction.
echo "== 2/6 The reference target's refusal set =="
refusals=$(grep -oE 'reference_unimplemented\("dn2cpp_pal_[a-z_0-9]+' "$REF_CPP" \
    | grep -oE 'dn2cpp_pal_[a-z_0-9]+' | sort -u)
expected_refusals="dn2cpp_pal_malloc_usable_size"
if [ "$refusals" != "$expected_refusals" ]; then
    echo "   expected: $expected_refusals" >&2
    echo "   measured: ${refusals:-<none>}" >&2
    fail "the reference PAL's refusal set changed. If that is intended, update this gate,
      the file's own header table, and docs/PORTING.md §8 — all three, because the
      point of the set being pinned is that it cannot grow quietly."
fi
echo "   OK refuses exactly: $refusals"

# ── 3/6 The whole runtime builds with the host's PAL swapped out ──────────────
echo "== 3/6 Building the runtime against the reference PAL =="
refexport="$(export PAL_REFERENCE=1; ensure_cmake_runtime)" || \
    fail "the runtime does not build with -DDN2CPP_PAL_REFERENCE=ON.
      That is claim 1 of docs/PORTING.md: a port is a platform/<target>/ directory
      and a CMake arm, with no core file changed. A failure here is either a core
      file that grew a host dependency, or the seam growing an entry the reference
      target was not given."
refruntimedir="$(dirname "$refexport")"

# The swap actually happened. Without this the section above would pass on a
# configure that silently fell through to the POSIX arm — the exact failure
# docs/PORTING.md §3.3 puts first in its "silently wrong if skipped" order, since
# the `else()` in the PAL selection is a fallthrough rather than an error.
# find_generated_objects, not a hand-written -name: CMake's Ninja generator appends
# the toolchain's object extension to the WHOLE source file name, so the same TU is
# dn2cpp_pal_reference.cpp.o under clang and .cpp.obj under MSVC. A pattern carrying
# only one of the two spellings misses the positive check on the other toolchain and
# — worse — makes the host-PAL absence check below unable to fail there at all.
refobjs=$(find_generated_objects "$refruntimedir" dn2cpp_pal_reference)
[ -n "$refobjs" ] || fail "the reference PAL TU was not compiled — the DN2CPP_PAL_REFERENCE
      arm did not select it and the configure fell through to the host PAL"
hostobjs=$(find_generated_objects "$refruntimedir" dn2cpp_pal_posix
           find_generated_objects "$refruntimedir" dn2cpp_pal_windows)
[ -z "$hostobjs" ] || fail "the host PAL was compiled INTO the reference build:
$hostobjs
      Both PALs in one archive is not a swap; it is a duplicate-symbol link waiting
      to happen, and on a target where one of them happened to compile it would link
      and answer from whichever the linker reached first."
echo "   OK the reference PAL TU is in the archive and the host PAL is not"

# The stack-frame ceiling reached the compile line. DN2CPP_MAX_STACK_FRAME is a
# cache STRING with a default, so a rename, a typo in the option name or an arm
# that stopped calling _dn2cpp_stack_frame_limit all leave a build that is green
# for the wrong reason — the ceiling is not enforced and nothing says so, which is
# the fail-open direction for a check whose whole job is to be enforced. Read off
# the compile database, which is what the compiler was actually handed. (MSVC has
# no equivalent flag; the check follows the CMake arm in skipping it there.)
case "$(basename "${CMAKE_CXX_COMPILER:-cc}")" in
    cl|cl.exe) echo "   (stack-frame ceiling: MSVC has no -Wframe-larger-than; not checked)" ;;
    *)
        # build.ninja rather than compile_commands.json: the latter is written only
        # when CMAKE_EXPORT_COMPILE_COMMANDS is on, which nothing here sets, so a
        # check keyed on it would silently not run.
        framed=$(grep -c 'Wframe-larger-than' "$refruntimedir/build.ninja" || true)
        [ "${framed:-0}" -gt 0 ] || fail "no runtime TU is compiled with -Wframe-larger-than.
      DN2CPP_MAX_STACK_FRAME did not reach the compile line, so the ceiling that keeps
      the runtime usable on a small-stack target is not being enforced — and a build
      that does not enforce it is green for the wrong reason."
        echo "   OK the stack-frame ceiling is armed ($framed rule(s) carry the flag)"
        ;;
esac

# ── 4/6 A real transpiled program runs on it ─────────────────────────────────
#
# Compiling is the cheap half. What this asserts is that the seam's entries answer
# correctly enough to run a program end to end — the file-system five, getenv, the
# time conversions and the console sink are all exercised by the sample's own
# output, and a wrong answer from any of them shows up as a diff.
echo "== 4/6 Running a transpiled program on the reference PAL =="
build_proj samples/dotnet/HelloWorld/HelloWorld.csproj
app="samples/dotnet/HelloWorld/bin/$CONFIG/$TFM/HelloWorld.dll"
OUT=artifacts/palreference
invoke_cli "$app" -o "$OUT"
if gate_cache_check "$OUT" "pal-reference|palref+nogc" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" \
        "$PAL_H" "$REF_CPP" runtime/core/platform/reference/dn2cpp_pal_reference.h; then
    gate_cache_hit_msg
    exit 0
fi
( export PAL_REFERENCE=1; compile_console "$OUT" HelloWorld ) || \
    fail "linking a program against the reference-PAL runtime failed"
refout="$("./$OUT/HelloWorld")" || fail "the program built on the reference PAL did not run"
oracle="$(dotnet "$app")" || fail "the real-.NET oracle did not run"
[ "$refout" = "$oracle" ] || {
    echo "   reference PAL: $refout" >&2
    echo "   real .NET    : $oracle" >&2
    fail "a program on the reference PAL does not produce real .NET's output"
}
echo "   OK output matches real .NET"

# ── 5/6 The console seam is a seam ────────────────────────────────────────────
#
# On every shipped target dn2cpp_pal_console_write IS fwrite, so nothing there can
# tell the difference between "Console.WriteLine funnels through the seam" and
# "Console.WriteLine writes to stdout and the seam happens to exist". The
# reference target is where the difference becomes observable, because its sink is
# replaceable — see runtime/core/platform/reference/dn2cpp_pal_reference.h for why
# the hook lives on the reference target and why a real port deletes it.
#
# There is no managed surface for installing a native sink and there should not be
# — a public API existing only to be tested is worse than the test it enables — so
# the installation is a native TU compiled into the program, as described below.
echo "== 5/6 The console sink hook actually intercepts =="
# The probe is an extra translation unit dropped into a COPY of the transpiler's
# output directory, where the app target's `generated*.cpp` glob picks it up. So
# the subject is the real transpiled HelloWorld — the same program section 4 just
# diffed against real .NET — with one static initialiser added that installs a
# capture sink before main runs.
#
# It is not a standalone C++ main linked against libdn2cpp_runtime.a. That was
# tried first and does not link: the runtime references symbols the GENERATED
# program defines (the type registry, the bind table, the exception message slot),
# so a hand-written probe would have to stub a list of them — a list with no
# owner, which goes stale as a link error in a gate nobody associates with the
# runtime change that moved it.
#
# What it asserts: with the sink installed, stdout receives NOTHING and the sink
# receives exactly the bytes the program would have printed. Both halves matter —
# the empty-stdout half is what catches a console path that still writes directly,
# and it is the half a "did the sink get called" test would miss.
sinkout="$OUT-sink"
rm -rf "$sinkout"; cp -R "$OUT" "$sinkout"; rm -rf "$sinkout/.cmake"* "$sinkout/consoleprobe"
cat > "$sinkout/generated_palsinkprobe.cpp" <<'PROBE'
// Gate-injected TU (build-and-run-pal-reference.sh section 5). Installs a
// capture sink on the reference PAL's console hook before main runs, and writes
// what it captures to stderr — tagged per stream, so the gate can strip the tags
// and compare against what the program prints without it.
//
// stderr and not a buffer printed at exit: the generated main can leave through
// _Exit (dn2cpp_environment_exit), which runs no atexit handler and no static
// destructor, so a buffer would be the thing that vanished.
#include "platform/reference/dn2cpp_pal_reference.h"
#include <cstdio>

namespace
{
    void capture(int stream, const char* bytes, size_t byteCount, void* ctx)
    {
        (void)ctx;
        std::fputs(stream == 1 ? "[E]" : "[O]", stderr);
        std::fwrite(bytes, 1, byteCount, stderr);
    }

    // Static initialisation order is not a hazard here: the sink pointer this
    // assigns is a zero-initialised POD in the PAL's TU, so it is already in its
    // final pre-install state before any dynamic initialiser runs.
    struct Install
    {
        Install() { dn2cpp_reference_console_sink_install(capture, nullptr); }
    };
    Install g_install;
}
PROBE
( export PAL_REFERENCE=1; compile_console "$sinkout" HelloWorld ) || \
    fail "the sink-probe build failed"
sinkstdout="$sinkout/stdout.txt"
sinkstderr="$sinkout/stderr.txt"
"./$sinkout/HelloWorld" > "$sinkstdout" 2> "$sinkstderr" || fail "the sink-probe binary did not run"
if [ -s "$sinkstdout" ]; then
    head -5 "$sinkstdout" >&2
    fail "stdout is NOT empty with the console sink installed — some console output
      bypasses dn2cpp_pal_console_write. That is the failure this section exists to
      find: on every shipped target the seam and stdout are the same destination, so
      a direct write is invisible there and only shows up here."
fi
# CR-insensitive on BOTH sides, unlike the direct comparison in section 4: the
# tag strip runs through sed, which reads in text mode on Windows and hands back
# LF where the oracle still carries the host's CRLF.
captured=$(strip_cr_win "$(LC_ALL=C sed 's/\[[OE]\]//g' "$sinkstderr")")
[ "$captured" = "$(strip_cr_win "$oracle")" ] || {
    echo "   captured (tags stripped): $captured" >&2
    echo "   real .NET               : $oracle" >&2
    fail "the sink did not receive the bytes the program prints"
}
# The stream tags are asserted too, not merely stripped: a seam that routed
# Console.Error to the stdout stream id would produce identical text and the wrong
# destination on a target where the two are different devices.
grep -q '\[O\]' "$sinkstderr" || fail "no write arrived tagged as Console.Out"
echo "   OK stdout empty; the sink received exactly the program's output"

# ── 6/6 The calloc GC fallback ────────────────────────────────────────────────
#
# -DDN2CPP_USE_GC=OFF is docs/PORTING.md §7 step 4's supported retreat. No other
# gate sets DN2CPP_NO_GC, because doing so reconfigures the shared runtime dir
# with the collector off and leaves it that way for whatever runs next. So this
# is the suite's only build of the configuration — break it and
# only this section is red, which is the same shape as the curl opt-out's single
# section in build-and-run-http-get.sh.
echo "== 6/6 The calloc GC fallback builds and runs =="
nogcexport="$(export DN2CPP_NO_GC=1; ensure_cmake_runtime)" || \
    fail "the runtime does not build with -DDN2CPP_USE_GC=OFF — the documented retreat is broken"
nogcdir="$(dirname "$nogcexport")"
# The option was obeyed, not merely accepted. Captured into a variable rather than
# piped into `grep -q`: under `set -o pipefail` a `| grep -q` assert fails only when
# it MATCHES, so the negative direction of this test would be fail-open.
gcarchives=$(find "$nogcdir" -name 'libdn2cpp_gc*.a' -o -name 'dn2cpp_gc*.lib')
[ -z "$gcarchives" ] || {
    printf '%s\n' "$gcarchives" >&2
    fail "a Boehm GC archive exists in the GC-less runtime build — the option was not obeyed"
}
( export DN2CPP_NO_GC=1; compile_console "$OUT" HelloWorld ) || \
    fail "linking a program against the GC-less runtime failed"
nogcout="$("./$OUT/HelloWorld")" || fail "the calloc-fallback binary did not run"
[ "$nogcout" = "$oracle" ] || {
    echo "   calloc build: $nogcout" >&2
    echo "   real .NET   : $oracle" >&2
    fail "the calloc-fallback binary does not produce real .NET's output"
}
echo "   OK the calloc fallback builds, links and matches real .NET"

gate_cache_commit
