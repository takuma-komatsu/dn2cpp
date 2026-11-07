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
# A type initializer that THROWS is owned here too — see
# samples/dotnet/ArgsEntrypointSubset/FailedCctorSubset.cs. The failed type must stay
# UNINITIALIZED: the section touches it three times, asserts all three throw with the
# same exception instance, asserts a static assigned before the throw is still
# unreadable, and asserts a catch-and-fall-back caller (the AngleSharp/Thrive shape: a
# second Encoding.GetEncoding SIGSEGV'd on a null static) survives. Diffed exactly vs
# real .NET, with ONE normalization written out at its site: .NET reports a failed
# initializer as TypeInitializationException wrapping the original and dn2cpp models no
# TypeInitializationException, so the section unwraps before printing. Everything else —
# that it throws, that it throws every time, the identity, the survival — is compared
# raw against the real-.NET oracle rather than frozen.
#
# GenericCctorFirstUseSubset pins the corresponding successful first-use path: closed
# generic initializers stay dormant until first use, so startup cannot freeze or inspect
# a registry before Main has populated it. The section covers static-field, static-call
# and newobj triggers, early returns both inside and before EmitManagedCall, plus two
# independent cache instantiations.
#
# The startup pass runs module initializers ahead of the entry point's own handler, so
# it must not let a failure escape: an escaping exception std::terminates a program
# whose .NET twin runs fine. samples/dotnet/ArgsEntrypointSubset/ThrowingModuleInitializer
# is a library passed with -r whose module initializer throws and which the app never
# touches, so .NET never runs it while the startup pass does. Stdout still matches real
# .NET exactly, and the multi-arg run's stderr must carry the boundary report naming the
# library's `<Module>` and the original exception. On this console lane the report is the
# sinkless stderr fallback of the shared host-boundary reporter
# (dn2cpp_report_boundary_exception); the GDExtension lane routes the SAME call through
# its print_error sink, so this assert covers the funnel both lanes share. It is a
# deliberate stdout/stderr split — the report is dn2cpp's own diagnostic, real .NET
# prints nothing there, so it must never leak into the exact stdout diff above.
#
# InitializerTriggersOnly pins the triggers of a precise-init (explicit static
# constructor) type initializer that touch no static field of the type: a run-time root
# (the entry type's initializer runs just before Main), a base-constructor call from a
# derived constructor, a struct constructor or instance method called on a local, a
# pointer to a static method, a static call or method pointer inside a shared generic
# body, and a constrained call binding a generic struct's own method, its own ToString,
# Equals or GetHashCode override, or a generic class's static abstract implementation.
# typeof and default(struct) stay non-triggers. The last block enters through reflection
# and the runtime: Activator and new(), a constructor, static method or property invoked
# by reflection, GetUninitializedObject, a boxed interface call or ToString, an attribute
# a read constructs, and a closed generic's static method bound by name or invoked. A
# failed initializer under Activator.CreateInstance(Type) surfaces inside the
# TargetInvocationException, as the constructor's would. A value type's member that a
# lowered call or a runtime slot enters triggers too: an interpolation hole, a
# collection's default equality, hash and order, a typed or run-time Array.Initialize
# of a non-empty array, a boxed GetHashCode, Equals or generic virtual method, and a
# field-by-field comparison's override. A delegate bound to a value type's instance
# method, plain, generic or shared, triggers when invoked, not when bound. A state
# machine's MoveNext triggers when an async builder's Start or an async iterator
# builder's MoveNext enters it, and so does a continuation a builder's await registers;
# an awaiter's registration method triggers its own type. Entering an interface's own
# default method triggers the interface, and a class's override of it does not. A
# constrained call to a static abstract or static virtual member triggers the type whose
# body runs: the implementation, or the interface when its default body runs.
# GetUninitializedObject runs each precise-init initializer on the allocated type's base
# chain, the allocated type's first, and leaves a beforefieldinit type or level to its
# first static field access. The driver runs again without ILDiet, where a
# constructor named only by typeof keeps the metadata row a run-time Array.Initialize
# may enter. Its own driver, so no other section names those types; exact diff vs real
# .NET, with the generic and non-generic late witnesses telling a first-use guard from
# a startup run, and types reached only from an untaken branch never initializing.
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
                'constrained struct GetHashCode=6/registered' 'initializer triggers end' \
                'late initializer before its trigger=not-run' 'late static call=11/registered' \
                'late static field=12/registered' 'late constructor call=registered' \
                'late base constructor call=registered' 'late struct instance method=13/registered' \
                'late method pointer=14/registered' 'late announcer initializer' \
                'late announcer touch=15' 'untaken branch initializers=False/False/False' \
                'late initializers end' 'generic activation=True/registered' \
                'new constraint=True/registered' 'generic struct activation=16/registered' \
                'constructor invoke=True/registered' 'type activation=True/registered' \
                'type activation failure0=wrapped:True/InvalidOperationException:activation initializer failed' \
                'type activation failure1=wrapped:True/InvalidOperationException:activation initializer failed' \
                'method invoke=17/registered' 'static property=18/registered' \
                'uninitialized object=True/registered' 'boxed before call=not-run' \
                'boxed interface call=19/registered' 'boxed ToString=text/registered' \
                'attribute instance=1/registered' 'generic delegate by name=20/20/registered/runs=1' \
                'generic method invoke=21/21/registered/runs=1' 'reflective initializers end' \
                'interpolated hole=text/registered' 'formatted hole=formatted:X/registered' \
                'builder hole=builder/registered' 'default comparer GetHashCode=26/registered' \
                'default comparer Equals=True/registered' \
                'hashed key=True/registered' 'array search=0/registered' \
                'list before search=not-run' 'list search=True/registered' \
                'binary search=0/registered' 'array sort=2/registered' \
                'non-generic array search=-1/registered' 'typed array before initialize=not-run' \
                'typed array initialize=22/registered' 'empty array initialize=0/not-run' \
                'run-time array initialize=23/registered' 'empty run-time array initialize=0/not-run' \
                'type-only constructor=TypeOnlyInitialized/not-run' \
                'boxed generic virtual before call=not-run' 'boxed generic virtual=24/registered' \
                'boxed GetHashCode=25/registered' 'boxed Equals=True/registered' \
                'field walk Equals=True/registered' 'field walk GetHashCode=registered' \
                'lowered initializer entries end' 'struct method delegate before call=not-run' \
                'struct method delegate=27/registered' \
                'generic struct method delegate before call=not-run' \
                'generic struct method delegate=28/registered' \
                'shared struct method delegate before call=not-run' \
                'shared struct method delegate=29/registered' 'delegate initializer entries end' \
                'async builder Start=registered/True/30' \
                'async iterator builder MoveNext=registered/31' \
                'state machine initializer entries end' \
                'beforefieldinit uninitialized object=True/not-run' \
                'beforefieldinit static field=32/registered' 'beforefieldinit entries end' \
                'async continuation MoveNext=registered/33' 'awaiter registration=registered/34' \
                'continuation initializer entries end' \
                'default interface method=36/registered/35' \
                'shared default interface method=38/registered/37' \
                'overridden default interface method=40/not-run/39' \
                'default method initializer entries end' \
                'static abstract implementation=41/registered/not-run/43/registered' \
                'inherited static virtual=44/registered/not-run/45' \
                'overridden static virtual=46/registered/not-run/47/registered' \
                'static virtual initializer entries end' \
                'uninitialized beforefieldinit leaf=True/ChainPreciseRoot' \
                'uninitialized beforefieldinit leaf again=True/none' \
                'uninitialized precise leaf=True/ChainPreciseLeaf,ChainPreciseBase' \
                'uninitialized over beforefieldinit base=True/ChainOverRelaxed' \
                'beforefieldinit base static field=48/ChainRelaxedBase' \
                'uninitialized three precise levels=True/ChainThird,ChainSecond,ChainFirst' \
                'uninitialized over beforefieldinit middle=True/ChainOuterLeaf,ChainOuterRoot' \
                'uninitialized chain entries end'; do
            gate_output byte-witness "$native" "$line" --count any --policy crlf \
                || { echo "FAIL: initializer trigger witness missing: $line" >&2; return 1; }
        done
    }
    corelib_diff_gate InitializerTriggersOnly
    DN2CPP_OUT_SUFFIX=-no-ildiet
    corelib_diff_gate InitializerTriggersOnly --no-ildiet
)

project=ArgsEntrypointSubset
out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')"

echo "== 1/4 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 2/4 Building app assembly =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"
build_proj "samples/dotnet/$project/ThrowingModuleInitializer/ThrowingModuleInitializer.csproj"
lib="samples/dotnet/$project/ThrowingModuleInitializer/bin/$CONFIG/$TFM/ThrowingModuleInitializer.dll"

echo "== 3/4 Transpiling app + real CoreLib (tree-shaken) =="
invoke_cli "$app" -r "$corelib" -r "$lib" -o "$out"
if gate_cache_check "$out" "args-entrypoint|$project|$corelib" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" "$lib"; then
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
echo "-- startup module initializer failure report (stderr) --"
# grep on the captured FILE, never `| grep -q` (SIGPIPE under pipefail makes the
# matched case the failing one). The phrase pins the whole contract: the boundary
# report ran, it names the MODULE whose initializer failed, and it carries the
# original exception rather than only a class of bug.
if ! grep -F "unhandled managed exception in the startup static constructor of <Module>, ThrowingModuleInitializer: System.InvalidOperationException: module initializer failed on purpose" \
        "$out/native.err" >/dev/null; then
    echo "FAIL: startup module initializer failure report missing or unnamed on stderr" >&2
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
