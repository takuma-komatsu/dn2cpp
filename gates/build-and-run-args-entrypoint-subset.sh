#!/usr/bin/env bash
# Console entry-point arguments preserve their text and string[] identity.
# Windows executables receive UTF-16 through wmain; other hosts receive UTF-8
# through main. The program name is excluded from the managed array.
#
# Verified by running BOTH the native build and real .NET with identical argv and
# diffing exactly — a multi-arg run (including a space-containing arg) and a
# no-arg run, plus Unicode, empty and quoted arguments. CoreLib only.
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
#
# InitializerTriggersOnly pins the triggers of a precise-init (explicit static
# constructor) type initializer that touch no static field of the type: a run-time root
# (the entry type's initializer runs before Main), a base-constructor call from a derived
# constructor, a struct constructor or instance method called on a local, a pointer to a
# static method, a static call or method pointer inside a shared generic body, and a
# constrained call binding a generic struct's own method, its own ToString, Equals or
# GetHashCode override, or a generic class's static abstract implementation.
# typeof and default(struct) stay non-triggers. Its own driver, so no other section
# names those types; exact diff vs real .NET, with the generic witnesses telling a
# first-use guard from a startup run.
source "$(dirname "$0")/_common.sh"

echo "== Precise type initializers run on every trigger, and only on a trigger =="
(
    DN2CPP_SAMPLE_PROJECT_DIR=samples/dotnet/ArgsEntrypointSubset/InitializerTriggersOnly
    DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|sample-path:$DN2CPP_SAMPLE_PROJECT_DIR"
    gate_extra_asserts() {
        local native line
        native="$1/native.stdout"
        for line in 'entry initializer before Main=True' 'base constructor call=True' \
                'struct instance method=1/True' 'method pointer=42/True' 'default struct=0/False' \
                'shared method pointer=8/registered' 'shared static call=9/registered' \
                'constrained struct method=3/registered' 'constrained static method=4/registered' \
                'constrained struct ToString=text/registered' 'constrained struct Equals=True/registered' \
                'constrained struct GetHashCode=6/registered' 'initializer triggers end'; do
            gate_output byte-witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: initializer trigger witness missing: $line" >&2; return 1; }
        done
    }
    corelib_diff_gate InitializerTriggersOnly
)

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
gate_capture "$out/native-1" run_bounded "./$out/$project" alpha "beta gamma" 42
gate_capture "$out/oracle-1" run_bounded dotnet "$app" alpha "beta gamma" 42
gate_output compare "$out/native-1.stdout" "$out/oracle-1.stdout"
gate_output status "$(cat "$out/native-1.status")" "$(cat "$out/oracle-1.status")"
cp "$out/native-1.stderr" "$out/native.err"
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
gate_capture "$out/native-2" run_bounded "./$out/$project"
gate_capture "$out/oracle-2" run_bounded dotnet "$app"
gate_output compare "$out/native-2.stdout" "$out/oracle-2.stdout"
gate_output status "$(cat "$out/native-2.status")" "$(cat "$out/oracle-2.status")"

echo "-- Unicode and quoted arguments --"
argv=("日本語" "😀" "" "two words" 'a"b' 'end\')
gate_capture "$out/native-3" run_bounded "./$out/$project" "${argv[@]}"
gate_capture "$out/oracle-3" run_bounded dotnet "$app" "${argv[@]}"
gate_output compare "$out/native-3.stdout" "$out/oracle-3.stdout"
gate_output status "$(cat "$out/native-3.status")" "$(cat "$out/oracle-3.status")"
cp "$out/native-3.stderr" "$out/unicode.err"

gate_cache_commit
