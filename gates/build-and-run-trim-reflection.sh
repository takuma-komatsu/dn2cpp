#!/usr/bin/env bash
# Consolidated --trim-reflection gate. The flag ships OFF by default and ON only for the
# Godot Web export, so without this nothing in the suite exercises it — yet it changes both
# the emitted C++ and the runtime reflection semantics. This gate is the console-side oracle
# for both (the real-engine oracle is build-and-run-godot-dotnet-trim.sh).
#
# ONE program (samples/dotnet/TrimReflect, over the reference library TrimReflectLib),
# transpiled three ways one flag apart, plus a fourth transpile that must FAIL:
#
#   1. no flag  -> diffed LIVE against real .NET (`dotnet TrimReflect.dll`). The control.
#      Every line is one real .NET and dn2cpp already agree on, so this arm certifies the
#      program's answers correct — which is what lets the two frozen arms below attribute
#      their every difference to the FLAG rather than to a transpiler gap or tree-shaking.
#
#   2. --trim-reflection -> diffed against a frozen snapshot. Reflecting over the MEMBERS of
#      a stripped framework type (TrimReflectLib.LibWidget / LibGadget / LibBox, which the
#      app meets only as `object`) throws the catchable PlatformNotSupportedException naming
#      the type and both remedies — it never answers an empty member list, which is the
#      silent wrong answer the flag exists not to ship. The snapshot holds those PNSE lines
#      verbatim, so a refactor that drops the DN2CPP_TF_METADATA_STRIPPED bit (turning the
#      throw back into an empty `any=False`) fails the diff. Everything the flag must NOT
#      touch is in the same snapshot answering unchanged: app-module reflection, the
#      base-chain / interface closure (an app type's inherited + interface members), the
#      whole non-reflection surface of a stripped type (Name/FullName/BaseType/cast/`is`/
#      boxing/enum.ToString), and — deliberately outside the strip — GetConstructor /
#      GetConstructors / Activator.CreateInstance, which keep answering on a stripped type.
#
#   3. --trim-reflection --reflection-root TrimReflectLib.LibWidget --reflection-root
#      TrimReflectLib.LibBox -> a second frozen snapshot in which exactly the rooted types
#      answer again: LibWidget by its EXACT full name, LibBox by its ARITY-STRIPPED generic
#      definition name (one root covering LibBox<int>, whose own name is the mangled
#      LibBox_Int32). LibGadget, rooted by neither, still throws — a root keeps exactly the
#      type it names and no more.
#
#   4. --reflection-root TrimReflectLib.NoSuchType -> the transpile must FAIL (a root
#      matching no loaded type is a hard error, not a silent no-op: a typo that quietly
#      became a no-op root would surface as a PlatformNotSupportedException only in the
#      shipped game, the one place the diagnostic reaches nobody).
#
# Why the stripped-type arms can only be FROZEN and not diffed live against real .NET: real
# .NET does not trim, so it never throws here; and dn2cpp tree-shakes UNREACHED members out
# of its reflection tables, so a member the app reflects over but never calls reads absent
# off dn2cpp and present off real .NET regardless of the flag. The program sidesteps that in
# the control arm by reaching every member it probes by name through a route that does NOT
# name (and so does not keep) the stripped type — an interface-slot dispatch for LibWidget's
# Twice/Tag, a field (fields are never tree-shaken) for the rest — so arm 1 matches real
# .NET exactly while those same types still strip. See samples/dotnet/TrimReflect/Program.cs.
source "$(dirname "$0")/_common.sh"

PROJECT=TrimReflect
LIBNAME=TrimReflectLib
EXPDIR="$(dirname "$0")/expected"

echo "== Building the app + reference library assemblies =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
APP="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"
# Named LIBDLL, not LIB: on Windows, ensure_msvc_env (_common.sh) exports LIB as
# the real MSVC library-search-path env var, and bash's export attribute sticks
# across a later plain reassignment — a same-named `LIB=` here would silently
# clobber it with this one DLL path, and link.exe would then fail to find even
# kernel32.lib.
LIBDLL="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$LIBNAME.dll"
CORELIB=$(locate_corelib)
echo "corelib: $CORELIB"

# ── Arm 1: no flag — live diff against real .NET ──────────────────────────────
echo "== Arm 1/4: no flag, exact diff vs real .NET =="
OUT=artifacts/trimreflect
invoke_cli "$APP" -r "$CORELIB" -r "$LIBDLL" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-plain|$CORELIB" \
        "$APP" "$LIBDLL" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    expected=$(dotnet "$APP"); expected_code=$?
    set -e
    assert_output "$native" "$expected"
    assert_exit_code "$native_code" "$expected_code"
    gate_cache_commit
fi

# ── Arm 2: --trim-reflection — freeze ─────────────────────────────────────────
echo "== Arm 2/4: --trim-reflection, diff vs frozen snapshot (stripped types throw) =="
OUT=artifacts/trimreflect-trim
invoke_cli "$APP" -r "$CORELIB" -r "$LIBDLL" --trim-reflection -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-trim|$CORELIB" \
        "$APP" "$LIBDLL" "$EXPDIR/trim-reflection-trimmed.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/trim-reflection-trimmed.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arm 3: --trim-reflection with two roots — freeze ──────────────────────────
echo "== Arm 3/4: --reflection-root (exact + arity-stripped def name), diff vs frozen =="
OUT=artifacts/trimreflect-rooted
invoke_cli "$APP" -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
    --reflection-root "$LIBNAME.LibWidget" --reflection-root "$LIBNAME.LibBox" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-rooted|$CORELIB" \
        "$APP" "$LIBDLL" "$EXPDIR/trim-reflection-rooted.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/trim-reflection-rooted.txt")"
    assert_exit_code "$native_code" 0
    gate_cache_commit
fi

# ── Arm 4: a root matching no loaded type is a hard error ──────────────────────
# Not cached: it is a ~1s transpile that must NOT produce output, so there is nothing to key
# a cache on, and re-running it every time is cheap insurance on the typo-is-loud contract.
echo "== Arm 4/4: --reflection-root naming no loaded type must FAIL the transpile =="
OUT=artifacts/trimreflect-typo
rm -rf "$OUT"
set +e
typo_err=$(invoke_cli "$APP" -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
    --reflection-root "$LIBNAME.NoSuchType" -o "$OUT" 2>&1)
typo_code=$?
set -e
printf '%s\n' "$typo_err" | tail -3
if [ "$typo_code" -eq 0 ]; then
    echo "FAIL: a --reflection-root naming no loaded type exited 0 — a typo became a silent no-op" >&2
    exit 1
fi
grep -q "no loaded type is named $LIBNAME.NoSuchType" <<<"$typo_err" \
    || { echo "FAIL: the failure did not name the missing root ($LIBNAME.NoSuchType)" >&2; exit 1; }
# generated*, not generated.cpp: emission streams the body/metadata TUs out
# during compilation and writes generated.h/generated.cpp only at the end, so a
# probe on the last-written file cannot see a transpile that died mid-emission
# The rm -rf above makes any hit this run's own.
! compgen -G "$OUT/generated*" >/dev/null \
    || { echo "FAIL: the failed transpile still emitted C++: $(ls -1 "$OUT" | tr '\n' ' ')" >&2; exit 1; }
echo "hard-error OK: exit $typo_code, named the missing root, emitted nothing"

echo "OK"
