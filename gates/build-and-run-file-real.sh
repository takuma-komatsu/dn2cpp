#!/usr/bin/env bash
# System.IO through the REAL BCL bodies. Everything beyond the six dn2cpp_file_*
# intercepted methods transpiles the real CoreLib implementation (FileStream
# strategies / SafeFileHandle / RandomAccess / StreamReader / StreamWriter),
# bottoming out in the Interop.Sys (libSystem.Native) P/Invokes that the dn2cpp
# runtime provides as self-contained SystemNative_* PAL shims
# (runtime/core/platform/posix/dn2cpp_system_native.cpp).
#
# The sections (samples/dotnet/FileReal/, one *.cs per theme):
#   FileCoreSubset      File.Copy/Move/Append*/ReadAllLines/WriteAllLines/ReadLines,
#                       File.Open*/Create*, the Path/File overloads that are NOT
#                       lowered, SafeFileHandle through the SafeHandle base surface,
#                       and a read into a collector-write-protected buffer
#   FileStreamSubset    the FileStream ctors DIRECTLY (incl. FileStreamOptions and
#                       its PreallocationSize), every FileMode, SetLength,
#                       Flush(flushToDisk), sync CopyTo, the span overloads, FileShare
#   StreamTextSubset    StreamReader/StreamWriter ctors directly, ReadToEnd/Peek/AutoFlush
#   FileMetaSubset      FileInfo, File.Get/SetAttributes, File.Replace, File.OpenHandle
#                       + the RandomAccess API over the SafeFileHandle
#   StreamAsyncSubset   the BASE Stream async slots (a subclass that overrides only the
#                       synchronous Read/Write), plus one that overrides the async
#                       surface and must keep dispatching to its own override
#   FileAsyncSubset     the async File.* one-shots, FileStream's own async surface, and
#                       a live CancellationToken threaded into file I/O
#   FileAsyncEnumSubset File.ReadLinesAsync — IAsyncEnumerable<string>, `await foreach`
#   PalIdentitySubset   the same PAL BEYOND file I/O: process id and the passwd
#                       lookup (Environment.ProcessId / Environment.UserName ->
#                       SystemNative_{GetPid,GetEUid,GetPwUidR} on Unix and
#                       GetCurrentProcessId/GetUserNameExW on Windows), plus
#                       System.Diagnostics.Process for the two entries no
#                       CoreLib-only closure can name (HasExited ->
#                       SystemNative_Kill, SessionId -> SystemNative_GetSid).
#                       Part of its regression is a LINK error rather than wrong
#                       output, so step 5 asserts the symbol set directly on top
#                       of the diff. It also covers the SECOND native module
#                       macOS Process needs — Interop.libproc's
#                       proc_pidinfo/proc_pidpath, behind
#                       ProcessName/StartTime/MainModule; those lower to the
#                       always-linked libSystem, so step 6 asserts no libproc row
#                       is reported bounded.
#   BoundedImportSubset the implemented dynamic loader and a bounded import at RUN
#                       time: NativeLibrary.Load must throw DllNotFoundException for
#                       a missing path, while the SILENT Debugger.IsAttached row keeps
#                       answering its truthful default. Plus
#                       the bound's OTHER mouth: an import reached as a METHOD
#                       GROUP rather than called, which is why this gate's
#                       transpile carries a --cut (see step 3) and why step 6
#                       asserts a report row, the link input and the stub's ABI.
#                       And that mouth's SIBLING, `ldvirtftn` — a virtual and an
#                       interface method group over --cut methods, plus the
#                       dynamic-codegen arm, which step 6 asserts on the emitted
#                       text because no program can hold a receiver for it.
#   FolderPathSubset    Environment.GetFolderPath / SystemDirectory over
#                       SystemNative_{Access,SearchPath}. Same LINK-error
#                       regression mode; SearchPath answers through real
#                       Foundation on macOS, so the HOME-relative folder suffixes
#                       diff exactly against the real-.NET oracle.
#
# The sample takes a scratch directory as args[0]. We give the native build and
# real .NET SEPARATE fresh directories and diff their stdout exactly — the program
# prints only content/bytes/results, never absolute paths (FileStream.Name IS one)
# and never an exception Message (it embeds one). CoreLib only.
source "$(dirname "$0")/_common.sh"

project=FileReal
out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')"

echo "== 1/7 Locating the real CoreLib (net10 pin) =="
# Pin net10.0: the samples build net10.0, and a newer preview CoreLib carries a
# different Interop.Sys surface (e.g. net11's SystemNative_FileSystemSupportsLocking)
# than the SystemNative_* PAL shims the runtime implements.
corelib=$(resolve_net10_corelib)
echo "corlib: $corelib"

echo "== 2/7 Building app assembly =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"

echo "== 3/7 Transpiling app + real CoreLib + Process (tree-shaken) =="
# System.Diagnostics.Process and System.ComponentModel.Primitives are here for
# PalIdentitySubset's Process block, and the SECOND one is not optional: without
# it the failure is a misleading `Component::.ctor has no intrinsic mapping`,
# which reads as a transpiler gap and is a missing reference (Process derives
# from Component, which lives in Primitives). --auto-ref pulls the rest of the
# closure. On Windows the Process block compiles out (FileReal.csproj), but the
# references stay: they cost a reachability closure nothing reaches, and an
# OS-conditional argv would make the two hosts transpile different programs for
# no gain.
#
# The transcript is kept: step 6 asserts the bounded-native-import report the
# ORDINARY path prints (no --verbose here — that arm is asserted there, on its
# own run), so this has to be the default-shaped output a user sees. Beside the
# output dir rather than inside it, so it stays out of anything keyed on the
# transpile's own bytes.
#
# The --cut is BoundedImportSubset's, and it is the only way this bucket can hold a
# bounded import that is reached as a method group and by nothing else: the two rows
# the core table marks Loud (NativeLibrary's two dlopen QCalls) are private
# compiler-generated CoreLib thunks whose address no IL takes, and every other core
# row this program reaches, it calls.
# CutNative.Probe names a module no host has, so the cut is also what keeps the C++
# link from asking for it — which is step 6's assert, not a side effect.
#
# The other two cuts serve the `ldvirtftn` mouth, and they cannot be imports: it pops
# a receiver and a [DllImport] is static, so that mouth can only be reached through
# an ordinary managed method the cut bounds. CutVirtual::Answer is dispatched
# through the vtable and ICutItf::Answer through the interface table — the two arms the
# mouth used to fall through to, each landing on the slot the emitter fills with the
# unreached-slot trap. The interface cut names the INTERFACE declaration on purpose:
# that is the token the method group carries, and cutting the implementation instead
# would leave the mouth asking about an unbounded method and the section asserting
# nothing.
mkdir -p "$(dirname "$out")"
transpile_log="$out.transpile.log"
invoke_cli "$app" -r "$corelib" \
    -r "$(dirname "$corelib")/System.Diagnostics.Process.dll" \
    -r "$(dirname "$corelib")/System.ComponentModel.Primitives.dll" \
    --cut "BoundedImportSubset.CutNative::Probe" \
    --cut "BoundedImportSubset.CutVirtual::Answer" \
    --cut "BoundedImportSubset.ICutItf::Answer" \
    --auto-ref -o "$out" | tee "$transpile_log"

# The result cache, placed here — after the transpile, before anything that
# compiles or runs — exactly like the corelib_* wrappers place theirs. This
# gate is hand-rolled rather than one of those wrappers (seven steps, two run
# modes, a symbol-set assert, a bounded-import report assert), so it names its
# own CONTEXT; what follows is what the CONTEXT is FOR, term by term, because a
# cache key is only as sound as the person who listed its inputs.
#
# Already in the key without being named here (gate_cache_check supplies them):
# CONFIG/TFM and the build-axis env, `uname -sm` + the compiler banner, the
# runtime/ + third_party/ content hash (which is what the step-5 PAL object is
# built from), the installed shared-runtime set (the `dotnet $app` oracle
# resolves by roll-forward), the sha of THIS script and of _common.sh — so the
# two run modes' and the PAL assert's own code is keyed by construction — and
# the transpile surface written into $out just above, which is what every
# CoreLib byte reaches the key through.
#
# What only this gate can name, hence the CONTEXT terms:
#   - the RESOLVED CoreLib path. resolve_net10_corelib pins net10 and picks the
#     highest 10.0.* installed; a second one appearing moves the oracle and the
#     PAL surface together, and the path is where that shows.
#   - the two extra `-r` assemblies. They are not decoration: without Primitives
#     the transpile fails, and the step-5 SystemNative_Kill assert is also the
#     check that the Process reference survived. Name them so a future edit to
#     the reference list cannot be replayed from a green recorded without it.
#   - BOTH run modes. Step 7 re-runs the same binary under the incremental
#     collector, and that is the only place in the suite where a kernel write
#     into a write-protected managed page is exercised. AGENTS.md's rule for
#     `--keep-symbols` applies verbatim: a term that changes only what happens
#     AFTER the transpile must be in the CONTEXT, or a green recorded without it
#     gets replayed for a run with it.
#   - the ambient GC env that reaches run 1 (run 2 forces its own). Neither
#     DN2CPP_GC_INCREMENTAL nor DN2CPP_GC_STATS is in gate_cache_check's shared
#     env line — the first would make run 1 a second incremental run, the second
#     adds startup/exit lines to stdout that the .NET oracle does not print.
#   - the symbol-set assert, named so the CONTEXT reads as the whole covered
#     region rather than as the diff alone.
#   - step 6's bounded-import report, which is BOTH an assert and a
#     second transpile arm: it re-runs the CLI over the same inputs under
#     `--measure --verbose`, i.e. a switch pair that changes only what happens
#     after the transpile and so falls squarely under the `--keep-symbols`
#     rule above. Its ordinary-path half reads step 3's transcript, which is
#     written before the check and therefore present on the hit path too — but
#     nothing reads it on a hit, which is correct: a hit means this exact
#     transcript already passed every assert below.
# The app dll + its runtimeconfig/deps.json go in as content inputs, the same
# three every wrapper passes.
cache_ctx="file_real_gate|$project|$corelib"
cache_ctx="$cache_ctx|refs:System.Diagnostics.Process+System.ComponentModel.Primitives|--auto-ref"
cache_ctx="$cache_ctx|runs:default+DN2CPP_GC_INCREMENTAL=1+measure-verbose"
cache_ctx="$cache_ctx|cut:BoundedImportSubset.CutNative::Probe+CutVirtual::Answer+ICutItf::Answer"
cache_ctx="$cache_ctx|asserts:diff+exitcode+pal-syms+libproc-syms+bounded-imports+verdicts+ldftn-mouth+ldvirtftn-mouth"
cache_ctx="$cache_ctx|gcenv:${DN2CPP_GC_INCREMENTAL:-}:${DN2CPP_GC_STATS:-}"
if gate_cache_check "$out" "$cache_ctx" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/7 Compiling C++ and running (exact diff vs real .NET) =="
compile_console "$out" "$project"

native_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_filereal_native.XXXXXX")
dotnet_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_filereal_dotnet.XXXXXX")
inc_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_filereal_inc.XXXXXX")
trap 'rm -rf "$native_dir" "$dotnet_dir" "$inc_dir"' EXIT

# Real .NET is the oracle for the exit status too, not just the output (see
# assert_exit_code in _common.sh). Both invocations are bracketed with
# set +e/set -e exactly like corelib_diff_gate does, and the exit codes are
# asserted explicitly rather than only the stdout.
#
# Both halves are load-bearing. The brackets keep a nonzero-exiting sample from
# tripping `set -e` and aborting THIS SCRIPT before the diff runs. The exit-code
# assertion is what catches a TRUNCATED transcript: a matched pair of truncated
# stdouts still diffs clean, which once passed a run of 1 of the 7 sections.
set +e
native_out=$("./$out/$project" "$native_dir"); native_code=$?
expected=$(dotnet "$app" "$dotnet_dir"); expected_code=$?
set -e
assert_output "$native_out" "$expected"
assert_exit_code "$native_code" "$expected_code"

echo "== 5/7 Asserting the PAL symbol set =="
# The PalIdentitySubset section's failure mode is not wrong output, it is a C++
# LINK error, and a link error is the one failure this bucket's diff cannot see.
# The run above already proves the link closed; this step proves WHY,
# from both ends — which is the native-symbol analogue of AGENTS.md's
# `cut ⟹ route`, a shape CppEmitter.AssertCalledBodiesEmitted structurally
# cannot check (it diffs managed MethodInfo symbols, and a P/Invoke has no
# emitted body to be in its defined set).
#
# NAMED: the emitted objects reference the two the section reaches. Without this
# the section could be deleted, or an intercept could quietly swallow the call,
# and the gate would stay green with the coverage gone. Read from OBJECT files,
# not the executable — a release link strips that to imports only.
undefined_syms="$out/pal-undefined-syms.txt"
dump_object_symbols_undefined "$out/.cmake" > "$undefined_syms"
if [ "$DN2CPP_OS" = windows ]; then
    # Interop.Sys IS the Unix PAL — on Windows the same CoreLib properties bind to
    # kernel32 and secur32 directly, so there is no SystemNative_* symbol to look for
    # and the defined half below has no object to read. Assert both Windows entry
    # points from the generated objects.
    for sym in GetCurrentProcessId GetUserNameExW; do
        grep -qE "^_?$sym\$" "$undefined_syms" || {
            echo "FAIL: no app object references $sym — PalIdentitySubset stopped reaching the Windows identity PAL" >&2
            exit 1; }
    done
    grep -q -- '/bigobj' "$out/.cmake/build.ninja" || {
        echo "FAIL: the MSVC app compile lost /bigobj — large generated registry objects can exceed COFF's section limit" >&2
        exit 1; }
    echo "OK: the app names GetCurrentProcessId and GetUserNameExW"
    echo "OK: MSVC app objects compile with /bigobj"
else
    # Non-vacuity first: an empty dump would pass every check below while proving
    # nothing (a stripped artifact, changed mangling, the wrong directory).
    pal_named=$(grep -cE '^_?SystemNative_' "$undefined_syms" || true)
    [ "${pal_named:-0}" -gt 10 ] || {
        echo "FAIL: only ${pal_named:-0} SystemNative_* references visible in the objects — the checks below would be vacuous" >&2
        exit 1; }
    # SystemNative_Kill joins the three CoreLib ones: the Process
    # block reaches it through HasExited (ProcessWaitState's liveness probe). It
    # is declared in System.Diagnostics.Process.dll, so this line is also the
    # check that step 3's -r survived.
    for sym in SystemNative_GetPid SystemNative_GetEUid SystemNative_GetPwUidR SystemNative_Kill; do
        grep -qE "^_?$sym\$" "$undefined_syms" || {
            echo "FAIL: no app object references $sym — PalIdentitySubset stopped reaching the P/Invoke" >&2
            exit 1; }
    done
    # GetSid is Darwin-only on the NAMED side, and for a reason that is about the
    # BCL rather than about the PAL: Process.SessionId reaches Interop.Sys.GetSid
    # only in the OSX flavor of System.Diagnostics.Process (ProcessManager.OSX's
    # CreateProcessInfo). The Linux flavor parses /proc/self/stat and enters the
    # PAL not at all — so the section still runs and still diffs its SessionId
    # there, it just does not name this symbol. Same Darwin-only shape as
    # SystemNative_SearchPath below, different cause.
    if [ "$DN2CPP_OS" = macos ]; then
        grep -qE '^_?SystemNative_GetSid$' "$undefined_syms" || {
            echo "FAIL: no app object references SystemNative_GetSid — PalIdentitySubset stopped reaching Process.SessionId" >&2
            exit 1; }
        # libproc. These are NOT dn2cpp symbols: proc_pidinfo/proc_pidpath are exported
        # by the always-linked libSystem, which is why admitting /usr/lib/libproc.dylib
        # to IsRuntimeProvidedPInvokeModule needs no shim TU and no -l token. The
        # objects NAMING them is the half the diff cannot see: that the call sites
        # lowered to real native calls instead of being substituted with a zero.
        for sym in proc_pidinfo proc_pidpath; do
            grep -qE "^_?$sym\$" "$undefined_syms" || {
                echo "FAIL: no app object references $sym — the libproc imports are being" >&2
                echo "      substituted again rather than lowered; Process.ProcessName" >&2
                echo "      would read \"\" and Process.StartTime would raise Win32Exception" >&2
                exit 1; }
        done
        # And the link manifest must stay empty of it: there is no libproc.dylib on disk,
        # so a `-lproc` token would be a link error waiting for the first host whose SDK
        # ships no libproc.tbd. Compilation.LinkLibToken maps the module to null.
        if [ -f "$out/pinvoke-libs.txt" ] && grep -qx 'proc' "$out/pinvoke-libs.txt"; then
            echo "FAIL: pinvoke-libs.txt carries a 'proc' token — libproc's symbols come from" >&2
            echo "      the always-linked libSystem and must contribute no -l flag" >&2
            exit 1
        fi
    fi
    # The folder-path pair, reached by FolderPathSubset. SystemNative_Access is
    # declared in every Unix flavor of CoreLib; SystemNative_SearchPath only in
    # the OSX flavor (Interop.SearchPath.cs), so a Linux transpile never names it
    # and the assert is Darwin-only — as is the PAL definition it pairs with.
    folder_syms="SystemNative_Access"
    [ "$DN2CPP_OS" = macos ] && folder_syms="$folder_syms SystemNative_SearchPath"
    for sym in $folder_syms; do
        grep -qE "^_?$sym\$" "$undefined_syms" || {
            echo "FAIL: no app object references $sym — FolderPathSubset stopped reaching the P/Invoke" >&2
            exit 1; }
    done
# DEFINED: the PAL object defines the whole identity surface from ITS OWN object,
# rather than from anything the link happened to find — the failure mode being a
# scratch archive that answers every SystemNative_* with a plausible constant.
# The runtime contract is held by the static_asserts and the signal-translation
# table at the call site — notably PAL SIGSTOP=19 vs Darwin's 17, where a
# pass-through would resume a process the caller asked to stop, and SIGKILL being
# 9 on both platforms is exactly what makes a pass-through look correct.
    pal_objdir="$(dirname "$(ensure_cmake_runtime)")"
    pal_defined="$out/pal-defined-syms.txt"
    dump_object_symbols_defined "$pal_objdir" dn2cpp_system_native > "$pal_defined"
    # SIX entries, not four: SystemNative_Access (all Unix) and
    # SystemNative_SearchPath (Darwin-only, like the pal_searchpath.m it
    # reproduces) come from the folder-path pair.
    pal_expected="SystemNative_GetPid SystemNative_GetSid SystemNative_Kill SystemNative_GetPwUidR SystemNative_Access"
    [ "$DN2CPP_OS" = macos ] && pal_expected="$pal_expected SystemNative_SearchPath"
    for sym in $pal_expected; do
        grep -qE "^_?$sym\$" "$pal_defined" || {
            echo "FAIL: the runtime PAL object does not define $sym — scratch link stubs would be needed again" >&2
            exit 1; }
    done
    echo "OK: PAL defines GetPid/GetSid/Kill/GetPwUidR/Access(+SearchPath on macOS); the app names GetPid/GetEUid/GetPwUidR/Kill/Access(+GetSid,SearchPath,libSystem's proc_pidinfo/proc_pidpath on macOS)"
fi

echo "== 6/7 Asserting the bounded-native-import report and its VERDICTS =="
# The one degrade class in this bucket that has no other witness. A bounded import
# is a bodyless [DllImport] whose call sites the transpile answers itself, and every
# other channel is blind to it by construction: the reachability drain's `m.Rva == 0`
# early-out fires BEFORE its bounded arm, so --measure records no gap row; the cut is
# exactly what keeps the import out of pinvoke-libs.txt, so step 5's symbol assert
# cannot name it either; and the transpile exits 0.
#
# TWO halves are asserted here, and they are different in kind:
#   - VISIBILITY: the ordinary transpile prints one count line, and --measure writes
#     the s0-bounded-imports.tsv sidecar.
#   - VERDICT: each row says what its call sites DO — `throws`
#     for an import whose zero would be indistinguishable from a real result, `silent`
#     for one whose default is simply the truth about a transpiled binary. The column
#     is asserted here; that the call sites HONOUR it is asserted by the run in step 4,
#     through samples/dotnet/FileReal/BoundedImportSubset.cs (read it — the two probes
#     are written so the exact diff against real .NET is what fails).
#
# Non-vacuity first — an empty or truncated transcript would pass a `grep -q`
# for an ABSENCE and prove nothing, which is the exact shape of the failure the
# arm below is written against.
grep -q '^dn2cpp: 5 assemblies,' "$transpile_log" || {
    echo "FAIL: step 3's transcript does not carry the transpile headline — the checks below would be vacuous" >&2
    exit 1; }
bounded_measure="$out-bounded-measure"
rm -rf "$bounded_measure"
# The --measure half, on its own run: the row class lives in a SIDECAR
# (s0-bounded-imports.tsv) and not in s0-gaps.tsv, deliberately — a bounded
# import is not a gap, and s0-gaps.tsv's readers treat every row as one
# (build-and-run-gdtask.sh asserts that file has zero lines; the thrive-gap
# harness diffs it against a saved baseline). --verbose here so the same run
# also asserts the per-import naming.
invoke_cli "$app" -r "$corelib" \
    -r "$(dirname "$corelib")/System.Diagnostics.Process.dll" \
    -r "$(dirname "$corelib")/System.ComponentModel.Primitives.dll" \
    --cut "BoundedImportSubset.CutNative::Probe" \
    --cut "BoundedImportSubset.CutVirtual::Answer" \
    --cut "BoundedImportSubset.ICutItf::Answer" \
    --auto-ref --measure --verbose -o "$bounded_measure" > "$bounded_measure.log" 2>&1
tail -10 "$bounded_measure.log"
bounded_tsv="$bounded_measure/s0-bounded-imports.tsv"

# ---- The host-independent bounded imports BoundedImportSubset reaches ----
# These are in CoreLib, which every host has, so this arm carries the verdict assert
# and the Darwin arm below only adds libproc — keeping the verdict column off a
# Darwin-only path. The "report is silent at zero" negative control lives in
# build-and-run-sample.sh over HelloWorld; this program deliberately bounds several.
[ -f "$bounded_tsv" ] || {
    echo "FAIL: --measure wrote no $bounded_tsv — the bounded-import row class is gone" >&2
    exit 1; }
# Four columns: module, entry point, declaring method, verdict. A row that
# lost its verdict column would still satisfy every name grep below.
bad_cols=$(awk -F'\t' 'NF != 4' "$bounded_tsv")
bad_cols=${bad_cols%%$'\n'*}
[ -z "$bad_cols" ] || {
    echo "FAIL: $bounded_tsv has a row without exactly 4 columns (module/entry/method/verdict):" >&2
    printf '%s\n' "$bad_cols" >&2
    exit 1; }
# NativeLibrary.Load is no longer a bounded QCall. It is implemented by the
# platform loader and must not appear in this report.
[ -z "$(awk -F'\t' '$2 ~ /^NativeLibrary_Load/' "$bounded_tsv")" ] || {
    echo "FAIL: NativeLibrary.Load regressed to a bounded QCall" >&2
    cat "$bounded_tsv" >&2
    exit 1; }
# The SILENT row, asserted just as hard: `false` is the correct answer for a managed
# debugger that cannot exist, and turning the whole table loud is the edit this line
# exists to fail.
[ -n "$(awk -F'\t' '$2 == "DebugDebugger_IsManagedDebuggerAttached" && $4 == "silent"' "$bounded_tsv")" ] || {
    echo "FAIL: $bounded_tsv carries no silent DebugDebugger_IsManagedDebuggerAttached row" >&2
    cat "$bounded_tsv" >&2
    exit 1; }
# The ordinary path says the same thing in one line, and the split is what a reader
# acts on: "N bounded" alone does not say whether their program will throw.
grep -qE '^dn2cpp: [0-9]+ native imports bounded \(.*\) — 0 throw PlatformNotSupportedException naming the module, [1-9][0-9]* answer 0/null silently.*pass --verbose to name them$' \
    "$transpile_log" || {
    echo "FAIL: the ordinary transpile's bounded-import line does not carry the throwing/silent split" >&2
    grep -n 'native imports bounded' "$transpile_log" >&2 || echo "      (no such line at all)" >&2
    exit 1; }
grep -qE -- '-- native imports bounded: [0-9]+ \(0 throwing, [1-9][0-9]* silent\) --' "$bounded_measure.log" || {
    echo "FAIL: the --measure summary does not carry the bounded-import count and split beside the gap histograms" >&2
    exit 1; }

# ---- libproc must be ABSENT from the report, on every host ----
# /usr/lib/libproc.dylib is a runtime-provided P/Invoke module
# (Compilation.IsRuntimeProvidedPInvokeModule) whose imports lower to direct native
# calls against the always-linked libSystem. Re-bounding them would restore
# Process.ProcessName == "" and Process.StartTime raising Win32Exception, with the
# transpile still green.
#
# Note WHICH way this check points. Step 5 asserts the positive — the app objects name
# proc_pidinfo and proc_pidpath, so the calls really lowered. This asserts the
# negative — nothing bounded them. Neither implies the other: a single restored
# bounded row leaves step 4's diff and step 5's symbol assert green, because the
# remaining stubs still name the symbol and the properties still answer. Route 1
# wins over route 5 in MethodCompiler.EmitManagedCall, so a stale row shadows an
# admitted module silently.
#
# Host-independent even though the cause is not: only the OSX flavor of
# System.Diagnostics.Process declares the module, so on Linux there is no libproc
# row to lose and asserting its absence costs one arm instead of two.
if [ -n "$(awk -F'\t' '$1 ~ /libproc/' "$bounded_tsv")" ]; then
    echo "FAIL: $bounded_tsv carries a libproc row — a macOS process-introspection import is" >&2
    echo "      being bounded to a zero again instead of lowering to libSystem's real" >&2
    echo "      proc_pidinfo/proc_pidpath. With the full set bounded, Process.ProcessName" >&2
    echo "      reads \"\", MainModule reads null and StartTime raises Win32Exception." >&2
    cat "$bounded_tsv" >&2
    exit 1
fi
if grep -q 'libproc' "$transpile_log"; then
    echo "FAIL: the ordinary transpile named libproc among the modules it bounded" >&2
    grep -n 'libproc' "$transpile_log" >&2
    exit 1
fi
# Two rows exactly: the Debugger QCall plus the one this gate MANUFACTURES
# with step 3's --cut, whose whole purpose is to be reached as a method group and by
# nothing else (asserted row by row below). An extra row is not necessarily wrong, but
# it is always a decision somebody made, and this is where it gets noticed. The count
# is host-independent: the cut module and both QCalls are named by CoreLib.
bounded_row_count=$(grep -c . "$bounded_tsv" || true)
[ "${bounded_row_count:-0}" -eq 2 ] || {
    echo "FAIL: $bounded_tsv has ${bounded_row_count:-0} rows, want 2 (the Debugger QCall" >&2
    echo "      plus step 3's --cut method-group import)" >&2
    cat "$bounded_tsv" >&2
    exit 1; }
echo "OK: 2 imports reported with verdicts — QCall Debugger silent and the --cut"
echo "    dn2cpp-absent-module silent (reached only"
echo "    as a method group); no libproc row on any host"

# ---- The bound's OTHER mouth: an import reached as a METHOD GROUP ----
# A bounded import has two substitutes and they are written in two places: the
# call-site neutralization (MethodCompiler.Call.cs) and the `ldftn` stub
# (MethodCompiler.cs). Everything above this line exercises the first; the three
# checks here are the three ways the second is free to be wrong.
#
# 1. REPORTED. CutNative.Probe is never called, only bound, so the only code that can
#    have put its row in the tsv is the ldftn arm's NoteBoundedImport. The row is
#    therefore evidence about this mouth specifically, which no other row here is.
[ -n "$(awk -F'\t' '$1 == "dn2cpp-absent-module" && $2 == "dn2cpp_absent_entry" && $4 == "silent"' \
    "$bounded_tsv")" ] || {
    echo "FAIL: $bounded_tsv has no silent dn2cpp-absent-module row — a bounded import" >&2
    echo "      reached ONLY as a method group stopped being reported (the ldftn mouth's" >&2
    echo "      NoteBoundedImport is the only thing that can report it)" >&2
    cat "$bounded_tsv" >&2
    exit 1; }
# 2. NOT LINKED AGAINST. `cut => route` in its native-symbol dimension, and the one
#    AssertCalledBodiesEmitted structurally cannot see (it diffs managed method
#    symbols; a P/Invoke has no emitted body to be in its defined set). If the
#    ftn-target note runs ahead of the bounded test, taking the address of a cut
#    import puts its module back into the link inputs: the transpile stays green and
#    the build dies at `ld: library '...' not found`. Step 4 having linked at all is
#    half the assert; this is the half that names the cause instead of leaving a link
#    error to be misread as a toolchain problem.
if [ -f "$out/pinvoke-libs.txt" ] && grep -q 'dn2cpp-absent-module' "$out/pinvoke-libs.txt"; then
    echo "FAIL: $out/pinvoke-libs.txt names dn2cpp-absent-module — taking a CUT import's" >&2
    echo "      address put its module back into the link inputs" >&2
    exit 1
fi
# 3. WRITTEN IN THE CONSUMER'S ABI. A delegate parks the address in f_method and
#    CppEmitter's invoker calls that slot as (Dn2CppObject* target, <Invoke's params>).
#    A stub carrying the callee's parameter list instead is one parameter short of the
#    pointer type its own invoker casts to — invisible on a flat native ABI (the extra
#    leading argument lands in a register a stub that ignores its arguments ignores),
#    so step 4's diff cannot see it; on wasm `call_indirect` carries a type immediate
#    and traps. Hence a check on the emitted text: it is the only oracle this host has.
# Every body TU, not a named one: which generated_b{k}.cpp a body lands in is a
# streaming-chunk detail and nothing here depends on it.
stub_file=$(ls "$out"/generated_b*.cpp)
grep -Fq '(void*)+[](Dn2CppObject*, int32_t) -> int32_t { return {}; }' $stub_file || {
    echo "FAIL: no body TU under $out carries a bounded-import ldftn stub in the delegate invoker's ABI" >&2
    echo "      (want a leading Dn2CppObject* target slot — see MethodCompiler.FtnStubShape)" >&2
    grep -Fn '(void*)+[]' $stub_file >&2 || echo "      (no ldftn stub at all)" >&2
    exit 1; }
if grep -Fq '(void*)+[](int32_t) -> int32_t' $stub_file; then
    echo "FAIL: a body TU under $out carries a bounded-import ldftn stub WITHOUT the target slot —" >&2
    echo "      the delegate invoker casts f_method to (Dn2CppObject*, int32_t), so this" >&2
    echo "      stub is one parameter short of the type it is called through" >&2
    exit 1
fi
echo "OK: the ldftn mouth reports its import, keeps the cut module out of the link inputs,"
echo "    and writes its stub in the delegate invoker's ABI"

# ---- The SIBLING mouth: a cut method reached by `ldvirtftn` ----
# Everything above binds a STATIC method group. `ldvirtftn` is the same mouth for a
# virtual or interface one, where the address comes off the vtable / interface table
# whose slot the emitter fills with the unreached-slot trap when reachability cut the
# body. Unlike the P/Invoke defect above that is not a link error — the transpile is
# green, the C++ links, and the delegate's first invoke aborts.
#
# The RUN half is asserted by step 4: `cut virtual bound as a method group answers: 0`
# and its interface twin are in the exact diff, and a regression dies inside
# dn2cpp_vcall_unimplemented, failing the diff as a truncation. What step 4 cannot see
# is the ABI: the trap symbol is (Dn2CppObject*) -> void while the invoker casts
# f_method to (Dn2CppObject*, int32_t) -> int64_t, a mismatch a flat native ABI
# swallows and a wasm `call_indirect` type immediate traps on. Hence the emitted
# text — three stubs, one per arm of the mouth.
#
# The return types are long and double, not int, and that is load-bearing: with int these
# greps would be satisfied by the ldftn section's stub and the check would stop being about
# this mouth at all. See BoundedImportSubset.cs.
for want in '(void*)+[](Dn2CppObject*, int32_t) -> int64_t { return {}; }' \
            '(void*)+[](Dn2CppObject*, int32_t) -> double { return {}; }'; do
    grep -Fq "$want" $stub_file || {
        echo "FAIL: no body TU under $out carries the ldvirtftn bounded stub $want" >&2
        echo "      (a --cut virtual / interface method bound as a method group must get a" >&2
        echo "      stub in the slot's ABI, not the unreached-slot trap)" >&2
        grep -Fn '(void*)+[]' $stub_file >&2 || echo "      (no ftn stub at all)" >&2
        exit 1; }
done
for bad in '(void*)+[](int32_t) -> int64_t' '(void*)+[](int32_t) -> double'; do
    if grep -Fq "$bad" $stub_file; then
        echo "FAIL: a body TU under $out carries an ldvirtftn stub WITHOUT the receiver slot" >&2
        echo "      ($bad). Every vtable / interface-table slot is called as" >&2
        echo "      (receiver, <params>), so this stub is one parameter short of the type it" >&2
        echo "      is called through — invisible here, a call_indirect trap on wasm" >&2
        exit 1
    fi
done
# The dynamic-codegen arm of the same mouth. It has no run-time witness anywhere and never
# can: the surface it names is what a transpiled binary cannot do, so nothing can hold a
# receiver to bind. BoundedImportSubset.__NeverRun is reachable and never executed for
# exactly that reason, which makes this grep the whole of its coverage.
dyn_msg='System.Reflection.Emit.ILGenerator.ThrowException requires dynamic code generation'
grep -Eq '\(void\*\)\+\[\]\(Dn2CppObject\*, [^)]*\) \{ dn2cpp_throw_platform_not_supported\("'"$dyn_msg"'"\)' \
    $stub_file || {
    echo "FAIL: no body TU under $out carries the ldvirtftn dynamic-codegen stub in the" >&2
    echo "      slot's ABI (want a leading Dn2CppObject* receiver, then the callee's" >&2
    echo "      parameters, and the same message the call-site lowering throws)" >&2
    grep -Fn 'requires dynamic code generation' $stub_file >&2 \
        || echo "      (no dynamic-codegen stub at all — did __NeverRun stop being reachable?)" >&2
    exit 1; }
if grep -Eq '\(void\*\)\+\[\]\(t_System_Type\*\) \{ dn2cpp_throw_platform_not_supported\("'"$dyn_msg"'"\)' $stub_file; then
    echo "FAIL: the ldvirtftn dynamic-codegen stub is written without the receiver slot" >&2
    exit 1
fi
echo "OK: the ldvirtftn mouth substitutes a cut virtual, a cut interface method and the"
echo "    dynamic-codegen surface, each in the dispatch slot's own ABI"

echo "== 7/7 Re-running under the INCREMENTAL collector =="
# The Godot lane turns Boehm's incremental collector on by DEFAULT
# (dn2cpp_gc_set_incremental_default, runtime/godot/dn2cpp_godot.cpp), and
# incremental mode write-protects the heap to recover its dirty bits. A kernel
# write into a protected page does not reach bdwgc's fault handler — ::pread into
# a managed byte[] returns EFAULT, which FileStream surfaces as an IOException. So
# a file read that works perfectly in every console gate can be broken in a game.
#
# Every other file-I/O gate is stop-the-world, so none of them can see it. Same
# binary, same expected output, one env var. The runtime's answer is a bounce
# buffer in the SystemNative_* read paths (dn2cpp_gc_kernel_write_unsafe); this is
# the assertion that keeps it.
set +e
inc_out=$(DN2CPP_GC_INCREMENTAL=1 "./$out/$project" "$inc_dir"); inc_code=$?
set -e
assert_output "$inc_out" "$expected"
assert_exit_code "$inc_code" "$expected_code"

# Record only here: every assert of both run modes and of the symbol set has
# passed above, so the green this commits covers the whole six-step pipeline.
gate_cache_commit
