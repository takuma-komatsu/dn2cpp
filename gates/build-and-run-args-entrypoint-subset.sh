#!/usr/bin/env bash
# A `static int Main(string[] args)` entry
# point. The transpiler now emits `int main(int argc, char** argv)` for a
# string[] entry, builds the managed args array from argv[1..] (the program
# name argv[0] is excluded, matching .NET's args), tags it with the precise
# ti_arr_string handle (so args.GetType()/`is string[]` are correct), and passes
# it to the managed entry point. A parameterless entry keeps `int main()`; any
# other parameter shape is still rejected (NotSupportedException).
#
# Verified by running BOTH the native build and real .NET with identical argv and
# diffing exactly — a multi-arg run (including a space-containing arg) and a
# no-arg run. CoreLib only (no Linq shim).
#
# The bucket also owns what the emitted `main()` does BEFORE it hands control to the
# managed entry point, which is where a [ModuleInitializer] (C# 9) has to run — see
# samples/dotnet/ArgsEntrypointSubset/ModuleInitSubset.cs. The C# compiler calls a
# module initializer from the `<Module>` pseudo-type's .cctor and from nowhere else,
# so a transpiler that drops the `<Module>` TypeDef row drops the initializer with it
# and no call-graph tree-shaker can notice: the program just runs with the side effect
# missing. The section pins that it ran, that it ran BEFORE Main (Main stamps its own
# place in a sequence on its first line), that its writes survive the type initializers
# of the types it touches, and that making `<Module>` a real type did not make it a
# visible one (it stays out of Assembly.GetTypes(), as in .NET, while Type.GetType
# still resolves it — also as in .NET).
#
# The same prologue also runs every type initializer eagerly (dn2cpp's stand-in for
# .NET's lazy first-use init), so it owns what happens when one THROWS — see
# samples/dotnet/ArgsEntrypointSubset/FailedCctorSubset.cs. Two halves, both invisible
# without the section: the prologue must not let the failure escape (it sits ahead of
# the entry point's own handler, so an escaping exception std::terminates a program
# whose .NET twin runs fine because it never touches the type), and the failed type must
# stay UNINITIALIZED — the guard used to latch its done flag on a throwing body, so the
# first touch threw and every touch after it silently handed back statics the initializer
# never assigned. The section touches such a type three times, asserts all three throw
# with the same exception instance, asserts a static assigned before the throw is still
# unreadable, and asserts a catch-and-fall-back caller (the AngleSharp/Thrive shape that
# found the bug: a second Encoding.GetEncoding SIGSEGV'd on a null static) survives.
# Diffed exactly vs real .NET, with ONE normalization written out at its site: .NET
# reports a failed initializer as TypeInitializationException wrapping the original and
# dn2cpp models no TypeInitializationException, so the section unwraps before printing.
# Everything else — that it throws, that it throws every time, the identity, the
# survival — is compared raw against the real-.NET oracle rather than frozen.
#
# GenericCctorFirstUseSubset pins the corresponding successful first-use path: closed
# generic initializers stay dormant until first use, so startup cannot freeze or inspect
# a registry before Main has populated it. The section covers static-field, static-call
# and newobj triggers, early returns both inside and before EmitManagedCall, plus two
# independent cache instantiations.
#
# The swallowed startup failure must also be REPORTED, naming the failed type (the
# same contract the --dotnet-module boundary report holds): the multi-arg run's stderr is
# asserted to carry the boundary report for FailedCctorSubset.Broken with the original
# exception. The prologue itself is silent — .NET is silent about an initializer it never
# runs, and the pass runs initializers .NET never would — so the report is emitted at the
# FIRST TOUCH of the type, once, alongside the re-raise. That this section touches
# Broken at all is therefore what makes the report observable here; a bucket that never
# touches its failed type must see an empty stderr. On this console lane the report is
# the sinkless stderr fallback of the shared host-boundary reporter
# (dn2cpp_report_boundary_exception); the GDExtension lane routes the SAME call through
# its print_error sink, so this assert covers the funnel both lanes share. It is a
# deliberate stdout/stderr split — the report is dn2cpp's own diagnostic, real .NET
# prints nothing there, so it must never leak into the exact stdout diff above.
source "$(dirname "$0")/_common.sh"

project=ArgsEntrypointSubset
out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')"

echo "== 1/4 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 2/4 Building app assembly =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"

echo "== 3/4 Transpiling app + real CoreLib (tree-shaken) =="
invoke_cli "$app" -r "$corelib" -o "$out"
if gate_cache_check "$out" "args-entrypoint|$project|$corelib" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running (exact diff vs real .NET) =="
compile_console "$out" "$project"

# Identical argv to both the native binary and real .NET; assert exact equality.
# Exit statuses are captured explicitly (`$(...)` inline would swallow them, so
# a binary aborting AFTER printing the full expected output would pass silently)
# and must match real .NET's.
echo "-- multi-arg run --"
set +e
native=$("./$out/$project" alpha "beta gamma" 42 2>"$out/native.err"); native_code=$?
expected=$(dotnet "$app" alpha "beta gamma" 42); expected_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"
echo "-- startup-cctor failure report (stderr) --"
# grep on the captured FILE, never `| grep -q` (SIGPIPE under pipefail makes the
# matched case the failing one). The phrase pins the whole contract: the boundary
# report ran, it names the TYPE whose initializer failed, and it carries the
# original exception rather than only a class of bug.
if ! grep -F "unhandled managed exception in the startup static constructor of FailedCctorSubset.Broken: System.InvalidOperationException: initializer failed on purpose" \
        "$out/native.err" >/dev/null; then
    echo "FAIL: startup-cctor failure report missing or unnamed on stderr" >&2
    echo "---- native stderr ----" >&2
    cat "$out/native.err" >&2
    exit 1
fi
echo "-- no-arg run --"
set +e
native=$("./$out/$project"); native_code=$?
expected=$(dotnet "$app"); expected_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"
gate_cache_commit
