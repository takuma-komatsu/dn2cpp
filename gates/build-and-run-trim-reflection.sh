#!/usr/bin/env bash
# Property accessor arrays answer for kept owners and refuse stripped member metadata.
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
#      Selecting native metadata for LibWidget does not preserve its stripped members.
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
# Every arm also runs a Delegate.Method section over library receivers met only as
# their base: the delegate's declaring type is kept for the read, a stripped
# receiver that inherits the slot answers through its vtable, and a stripped level
# that overrides it throws the same PNSE naming that level. Generic-virtual
# bindings use the dispatcher's selected target with the same trim guard. Under
# the flag, its interface-default probes pin that a receiver bound to the
# declaration's default answers beside a stripped derived interface that
# overrides nothing, while a derived interface's selected override throws the
# PNSE naming that stripped interface. Arm 1 answers as .NET and keeps its
# pre-section output unchanged when skipped. The Object-virtual section binds Object's
# ToString and GetHashCode through library receivers met only as `object`: under the
# flag a stripped level whose override the answer needs throws the PNSE naming it, and
# a stripped receiver whose dispatch field proves it inherits Object's body answers
# Object's method. The unrecorded-receiver section binds receivers without a recorded
# case, whose class levels are walked: a MakeGenericType instantiation under an
# interface binding keeps its typeof-named definition's members, while a library
# subclass that inherits a generic virtual body still refuses its stripped level.
# The reflection-bound section binds that generic virtual through reflection over the same
# subclass: its dispatcher records no case for it, so it runs the row's own body and
# every arm names the row.
# Keep original member metadata while comparing the C++ reflection policies.
# ILDiet with --trim-reflection is covered by build-and-run-preserve-control.sh.
source "$(dirname "$0")/_common.sh"
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} "
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|member-enum-prefix:${DN2CPP_BEFORE_MEMBER_ENUM:-}|object-virtual-prefix:${DN2CPP_BEFORE_OBJECT_VIRTUAL:-}|unrecorded-receiver-prefix:${DN2CPP_BEFORE_UNRECORDED_RECEIVER:-}|reflected-unrecorded-receiver-prefix:${DN2CPP_BEFORE_REFLECTED_UNRECORDED_RECEIVER:-}|runtime-template-prefix-argv:before-runtime-template-members|property-accessors-prefix-argv:before-property-accessors"

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

# Delegate.Method witnesses (a PNSE line by its prefix), so the section cannot drop
# out of a frozen snapshot unnoticed.
assert_delegate_method_lines() {
    local out="$1" line
    shift
    grep -Fxq '== Delegate.Method over stripped receivers ==' <<<"$(strip_cr_win "$out")" \
        || { echo "FAIL: the Delegate.Method section did not run" >&2; exit 1; }
    local enum_before enum_prefix
    enum_before=$(DN2CPP_BEFORE_MEMBER_ENUM=1 run_bounded "$OUT/$PROJECT$EXE_EXT")
    enum_prefix=$(awk '/^== enum named by a reflected field ==$/ { exit } { print }' <<< "$(strip_cr_win "$out")")
    assert_output "$enum_prefix" "$(strip_cr_win "$enum_before")"
    grep -Fxq '  GetFields -> LibShade fields=3' <<< "$(strip_cr_win "$out")" \
        || { echo 'FAIL: member-only enum section did not run' >&2; exit 1; }
    for line in "$@"; do
        grep -Fq -- "$line" <<<"$(strip_cr_win "$out")" \
            || { echo "FAIL: Delegate.Method witness missing: $line" >&2; exit 1; }
    done
}

# The Object-virtual Delegate.Method witnesses. The output before the section's
# header must equal a run that stops before it.
assert_object_virtual_lines() {
    local out="$1" line before prefix
    shift
    out=$(strip_cr_win "$out")
    grep -Fxq '== Delegate.Method of Object virtuals over stripped receivers ==' <<<"$out" \
        || { echo "FAIL: the Object-virtual Delegate.Method section did not run" >&2; exit 1; }
    before=$(strip_cr_win "$(DN2CPP_BEFORE_OBJECT_VIRTUAL=1 run_bounded "$OUT/$PROJECT$EXE_EXT")")
    prefix=$(awk '/^== Delegate.Method of Object virtuals over stripped receivers ==$/ { exit } { print }' <<<"$out")
    assert_output "$prefix" "$before"
    for line in "$@"; do
        grep -Fq -- "$line" <<<"$out" \
            || { echo "FAIL: Object-virtual Delegate.Method witness missing: $line" >&2; exit 1; }
    done
}

# The reflection-bound section is the program's last: the output before its header
# must equal a run that stops before it, and every arm names the row.
assert_reflected_unrecorded_lines() {
    local out before prefix
    out=$(strip_cr_win "$1")
    grep -Fxq '== reflection-bound Delegate.Method over a receiver without a recorded case ==' <<<"$out" \
        || { echo "FAIL: the reflection-bound unrecorded-receiver section did not run" >&2; exit 1; }
    before=$(strip_cr_win "$(DN2CPP_BEFORE_REFLECTED_UNRECORDED_RECEIVER=1 run_bounded "$OUT/$PROJECT$EXE_EXT")")
    prefix=$(awk '/^== reflection-bound Delegate.Method over a receiver without a recorded case ==$/ { exit } { print }' <<<"$out")
    assert_output "$prefix" "$before"
    grep -Fxq '  reflected unrecorded generic virtual -> LibGvmShape/gvm-base' <<<"$out" \
        || { echo "FAIL: reflection-bound unrecorded-receiver witness missing" >&2; exit 1; }
}

# The witnesses for receivers without a recorded case. The output before its header
# must equal a run that stops before it.
assert_unrecorded_receiver_lines() {
    local out="$1" line before prefix
    shift
    out=$(strip_cr_win "$out")
    grep -Fxq '== Delegate.Method over receivers without a recorded case ==' <<<"$out" \
        || { echo "FAIL: the unrecorded-receiver Delegate.Method section did not run" >&2; exit 1; }
    before=$(strip_cr_win "$(DN2CPP_BEFORE_UNRECORDED_RECEIVER=1 run_bounded "$OUT/$PROJECT$EXE_EXT")")
    prefix=$(awk '/^== Delegate.Method over receivers without a recorded case ==$/ { exit } { print }' <<<"$out")
    assert_output "$prefix" "$before"
    for line in "$@"; do
        grep -Fq -- "$line" <<<"$out" \
            || { echo "FAIL: unrecorded-receiver Delegate.Method witness missing: $line" >&2; exit 1; }
    done
}

# The template section matches CLR under each trim policy; stripped reads remain frozen.
assert_runtime_template_members() {
    local actual="$1" before prefix expected_section actual_section line
    actual=$(strip_cr_win "$actual")
    before=$(run_bounded "$OUT/$PROJECT$EXE_EXT" before-property-accessors) || return $?
    prefix=$(awk '/^== property accessors under trim ==$/ { exit } { print }' <<< "$actual")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    before=$(run_bounded "$OUT/$PROJECT$EXE_EXT" before-runtime-template-members) || return $?
    prefix=$(awk '/^== typeof-kept runtime template members ==$/ { exit } { print }' <<< "$actual")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    expected_section=$(run_bounded dotnet "$APP") || return $?
    expected_section=$(awk '/^== typeof-kept runtime template members ==$/ { found=1 } found { print } /^typeof-kept runtime template members end$/ { found=0 }' <<< "$(strip_cr_win "$expected_section")")
    actual_section=$(awk '/^== typeof-kept runtime template members ==$/ { found=1 } found { print } /^typeof-kept runtime template members end$/ { found=0 }' <<< "$actual")
    assert_output "$actual_section" "$expected_section"
    for line in '== typeof-kept runtime template members ==' \
        '  application Widget GetMethod/Invoke -> AppTemplateRead`1/application:Widget/same=True' \
        '  library Widget GetMethod/Invoke -> LibTemplateRead`1/library:Widget/same=True' \
        '  library Widget Delegate.Method -> LibTemplateRead`1/library:Widget' \
        '  library Int32 GetMethod/Invoke -> LibTemplateRead`1/library:Int32/same=True' \
        '  library Int32 Delegate.Method -> LibTemplateRead`1/library:Int32' \
        '  library Widget generic Delegate.Method -> LibTemplateRead`1/Widget/Int32' \
        '  library Int32 generic Delegate.Method -> LibTemplateRead`1/Int32/Int32' \
        'typeof-kept runtime template members end' \
        '== property accessors under trim ==' '  app accessor=get_Name/same=True' \
        'property accessors under trim end'; do
        grep -Fxq -- "$line" <<< "$actual" \
            || { echo "FAIL: runtime template metadata witness missing: $line" >&2; return 1; }
    done
}

# ── Arm 1: no flag — live diff against real .NET ──────────────────────────────
echo "== Arm 1/4: no flag, exact diff vs real .NET =="
OUT=artifacts/trimreflect
invoke_cli "$APP" --no-ildiet -r "$CORELIB" -r "$LIBDLL" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-plain|no-ildiet|$CORELIB" \
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
    assert_delegate_method_lines "$native" \
        '  inherited slot -> LibShape/shape' \
        '  overriding level -> LibCircle/circle' \
        '  inherited override -> LibCircle/circle' \
        '  generic virtual -> LibGvmLeaf/gvm-leaf' \
        '  interface slot -> LibWidget/8'
    assert_delegate_method_lines "$native" \
        '  unrelated stripped interface -> IDefaultKind/base' \
        '  selected stripped interface -> IChosenDefaultKind/chosen'
    assert_object_virtual_lines "$native" \
        '  object overriding level -> LibLabel.ToString/label' \
        '  object inherited override -> LibLabel.ToString/label' \
        '  object inherited body -> Object.ToString/TrimReflectLib.LibBare' \
        '  object hash override -> LibLabel.GetHashCode/7' \
        '  object reflected binding -> LibLabel.ToString/label'
    assert_unrecorded_receiver_lines "$native" \
        '  instantiation interface binding -> LibGenericKind`1/generic-kind' \
        '  unrecorded generic virtual -> LibGvmShape/gvm-base'
    assert_reflected_unrecorded_lines "$native"
    assert_runtime_template_members "$native"
    before=$(strip_cr_win "$(DN2CPP_BEFORE_DELEGATE_METHOD=1 "./$OUT/$PROJECT")")
    prefix=$(awk '/^== Delegate.Method over stripped receivers ==$/ { exit } { print }' \
        <<<"$(strip_cr_win "$native")")
    assert_output "$prefix" "$before"
    gate_cache_commit
fi

# ── Arm 2: --trim-reflection — freeze ─────────────────────────────────────────
echo "== Arm 2/4: --trim-reflection with native metadata, diff vs frozen snapshot =="
OUT=artifacts/trimreflect-trim
invoke_cli "$APP" --no-ildiet -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
    --reflection-metadata "$LIBNAME.LibWidget=native" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-trim|no-ildiet|--reflection-metadata=$LIBNAME.LibWidget=native|$CORELIB" \
        "$APP" "$LIBDLL" "$EXPDIR/trim-reflection-trimmed.txt"; then
    gate_cache_hit_msg
else
    grep -qw 'md_native_refl_ti_TrimReflectLib_LibWidget' "$OUT"/generated*.cpp \
        || { echo "FAIL: the stripped LibWidget type did not retain its selected native metadata layout" >&2; exit 1; }
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/trim-reflection-trimmed.txt")"
    assert_exit_code "$native_code" 0
    assert_delegate_method_lines "$native" \
        '  inherited slot -> LibShape/shape' \
        "  overriding level -> PNSE: Reflection over the members of 'TrimReflectLib.LibCircle'" \
        "  inherited override -> PNSE: Reflection over the members of 'TrimReflectLib.LibCircle'" \
        "  generic virtual -> PNSE: Reflection over the members of 'TrimReflectLib.LibGvmLeaf'" \
        "  interface slot -> PNSE: Reflection over the members of 'TrimReflectLib.LibWidget'" \
        '  unrelated stripped interface -> IDefaultKind/base' \
        "  selected stripped interface -> PNSE: Reflection over the members of 'TrimReflectLib.IChosenDefaultKind'"
    assert_object_virtual_lines "$native" \
        "  object overriding level -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'" \
        "  object inherited override -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'" \
        '  object inherited body -> Object.ToString/TrimReflectLib.LibBare' \
        "  object hash override -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'" \
        "  object reflected binding -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'"
    assert_unrecorded_receiver_lines "$native" \
        '  instantiation interface binding -> LibGenericKind`1/generic-kind' \
        "  unrecorded generic virtual -> PNSE: Reflection over the members of 'TrimReflectLib.LibGvmPlain'"
    assert_reflected_unrecorded_lines "$native"
    assert_runtime_template_members "$native"
    gate_cache_commit
fi

# ── Arm 3: --trim-reflection with two roots — freeze ──────────────────────────
echo "== Arm 3/4: --reflection-root (exact + arity-stripped def name), diff vs frozen =="
OUT=artifacts/trimreflect-rooted
invoke_cli "$APP" --no-ildiet -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
    --reflection-root "$LIBNAME.LibWidget" --reflection-root "$LIBNAME.LibBox" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-rooted|no-ildiet|$CORELIB" \
        "$APP" "$LIBDLL" "$EXPDIR/trim-reflection-rooted.txt"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    assert_output "$(strip_cr_win "$native")" "$(cat "$EXPDIR/trim-reflection-rooted.txt")"
    assert_exit_code "$native_code" 0
    assert_delegate_method_lines "$native" \
        '  inherited slot -> LibShape/shape' \
        "  overriding level -> PNSE: Reflection over the members of 'TrimReflectLib.LibCircle'" \
        "  inherited override -> PNSE: Reflection over the members of 'TrimReflectLib.LibCircle'" \
        "  generic virtual -> PNSE: Reflection over the members of 'TrimReflectLib.LibGvmLeaf'" \
        '  interface slot -> LibWidget/8' \
        '  unrelated stripped interface -> IDefaultKind/base' \
        "  selected stripped interface -> PNSE: Reflection over the members of 'TrimReflectLib.IChosenDefaultKind'"
    assert_object_virtual_lines "$native" \
        "  object overriding level -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'" \
        "  object inherited override -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'" \
        '  object inherited body -> Object.ToString/TrimReflectLib.LibBare' \
        "  object hash override -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'" \
        "  object reflected binding -> PNSE: Reflection over the members of 'TrimReflectLib.LibLabel'"
    assert_unrecorded_receiver_lines "$native" \
        '  instantiation interface binding -> LibGenericKind`1/generic-kind' \
        "  unrecorded generic virtual -> PNSE: Reflection over the members of 'TrimReflectLib.LibGvmPlain'"
    assert_reflected_unrecorded_lines "$native"
    assert_runtime_template_members "$native"
    gate_cache_commit
fi

# ── Arm 4: a root matching no loaded type is a hard error ──────────────────────
# Not cached: it is a ~1s transpile that must NOT produce output, so there is nothing to key
# a cache on, and re-running it every time is cheap insurance on the typo-is-loud contract.
echo "== Arm 4/4: --reflection-root naming no loaded type must FAIL the transpile =="
OUT=artifacts/trimreflect-typo
rm -rf "$OUT"
set +e
typo_err=$(invoke_cli "$APP" --no-ildiet -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
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

echo "== ILDiet accepts generic roots and rejects missing roots before emission =="
for root in "$LIBNAME.LibBox" "$LIBNAME.LibBox_Int32"; do
    OUT="artifacts/trimreflect-ildiet-$root"
    invoke_cli "$APP" -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
        --reflection-root "$root" -o "$OUT"
    [ -f "$OUT/ildiet/$LIBNAME.dll" ] && [ -f "$OUT/generated.h" ] \
        || { echo "FAIL: ILDiet root $root did not reach C++ emission" >&2; exit 1; }
done
OUT=artifacts/trimreflect-ildiet-typo
rm -rf "$OUT"
set +e
diet_typo_err=$(invoke_cli "$APP" -r "$CORELIB" -r "$LIBDLL" \
    --reflection-root "$LIBNAME.NoSuchType" -o "$OUT" 2>&1)
diet_typo_code=$?
set -e
[ "$diet_typo_code" -ne 0 ] && grep -Fq "$LIBNAME.NoSuchType" <<<"$diet_typo_err" \
    || { echo "FAIL: missing ILDiet root did not fail naming the selector" >&2; exit 1; }
! compgen -G "$OUT/generated*" >/dev/null \
    || { echo "FAIL: invalid ILDiet root reached C++ emission" >&2; exit 1; }
echo "OK"
