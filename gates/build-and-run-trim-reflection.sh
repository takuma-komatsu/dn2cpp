#!/usr/bin/env bash
# Delegate.Method retains known selected owners, while unrelated member metadata strips.
# The full program compares each trim policy with live .NET. Its Object-virtual
# identities can select each allocated receiver's override, so their owners keep
# metadata. The isolated driver reads the same library types without those
# identities and pins the stripped reads and exact/generic-definition roots.
# Keep original IL metadata while comparing the native reflection policies.
# ILDiet with --trim-reflection is covered by build-and-run-preserve-control.sh.
source "$(dirname "$0")/_common.sh"
# Delegate name lookup refuses stripped receiver metadata even for a soft bind failure.
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} samples/dotnet/TrimReflect/StrippedMembersOnly.csproj samples/dotnet/TrimReflect/StrippedMembersOnlyProgram.cs"
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|delegate-method-prefix:${DN2CPP_BEFORE_DELEGATE_METHOD:-}|member-enum-prefix:${DN2CPP_BEFORE_MEMBER_ENUM:-}|object-virtual-prefix:${DN2CPP_BEFORE_OBJECT_VIRTUAL:-}|unrecorded-receiver-prefix:${DN2CPP_BEFORE_UNRECORDED_RECEIVER:-}|reflected-unrecorded-receiver-prefix:${DN2CPP_BEFORE_REFLECTED_UNRECORDED_RECEIVER:-}|runtime-template-prefix-argv:before-runtime-template-members|property-accessors-prefix-argv:before-property-accessors"
DN2CPP_GATE_EXTRA_CONTEXT="$DN2CPP_GATE_EXTRA_CONTEXT|delegate-name-bindings-prefix-argv:before-delegate-name-bindings|isolated-stripped-members:plain,trimmed,rooted"

PROJECT=TrimReflect
LIBNAME=TrimReflectLib

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

# Delegate.Method witnesses keep every selected-owner answer observable.
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

assert_named_delegate_lines() {
    local out="$1" before prefix line
    shift
    out=$(strip_cr_win "$out")
    before=$(run_bounded "$OUT/$PROJECT$EXE_EXT" before-delegate-name-bindings)
    prefix=$(awk '/^== delegate method names under trim ==$/ { exit } { print }' <<< "$out")
    assert_output "$prefix" "$(strip_cr_win "$before")"
    for line in '== delegate method names under trim ==' '  app name=42' \
        '  inherited name=1' 'delegate method names under trim end' "$@"; do
        grep -Fq -- "$line" <<< "$out" \
            || { echo "FAIL: delegate name trim witness missing: $line" >&2; return 1; }
    done
}

# ── Arm 1: no flag — live diff against real .NET ──────────────────────────────
echo "== Arm 1/4: no flag, exact diff vs real .NET =="
OUT=artifacts/trimreflect
invoke_cli "$APP" --no-ildiet -r "$CORELIB" -r "$LIBDLL" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-plain|no-ildiet|$CORELIB$(_gate_ctx_extras)" \
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
    assert_named_delegate_lines "$native" '  library name -> 42' \
        '  library missing soft name -> null' '  library static soft name -> null'
    before=$(strip_cr_win "$(DN2CPP_BEFORE_DELEGATE_METHOD=1 "./$OUT/$PROJECT")")
    prefix=$(awk '/^== Delegate.Method over stripped receivers ==$/ { exit } { print }' \
        <<<"$(strip_cr_win "$native")")
    assert_output "$prefix" "$before"
    gate_cache_commit
fi

# ── Arm 2: --trim-reflection — live diff ─────────────────────────────────────────
echo "== Arm 2/4: --trim-reflection with native metadata, live diff vs real .NET =="
OUT=artifacts/trimreflect-trim
invoke_cli "$APP" --no-ildiet -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
    --reflection-metadata "$LIBNAME.LibWidget=native" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-trim|no-ildiet|--reflection-metadata=$LIBNAME.LibWidget=native|$CORELIB$(_gate_ctx_extras)" \
        "$APP" "$LIBDLL" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json"; then
    gate_cache_hit_msg
else
    grep -qw 'md_native_refl_ti_TrimReflectLib_LibWidget' "$OUT"/generated*.cpp \
        || { echo "FAIL: the selected LibWidget owner did not retain its selected native metadata layout" >&2; exit 1; }
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    expected=$(run_bounded dotnet "$APP")
    assert_output "$(strip_cr_win "$native")" "$(strip_cr_win "$expected")"
    assert_exit_code "$native_code" 0
    assert_delegate_method_lines "$native" \
        '  inherited slot -> LibShape/shape' \
        '  overriding level -> LibCircle/circle' \
        '  inherited override -> LibCircle/circle' \
        '  generic virtual -> LibGvmLeaf/gvm-leaf' \
        '  interface slot -> LibWidget/8' \
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
    assert_named_delegate_lines "$native" \
        '  library name -> 42' \
        '  library missing soft name -> null' \
        '  library static soft name -> null'
    gate_cache_commit
fi

# ── Arm 3: --trim-reflection with two roots — live diff ──────────────────────────
echo "== Arm 3/4: --reflection-root (exact + arity-stripped def name), live diff vs real .NET =="
OUT=artifacts/trimreflect-rooted
invoke_cli "$APP" --no-ildiet -r "$CORELIB" -r "$LIBDLL" --trim-reflection \
    --reflection-root "$LIBNAME.LibWidget" --reflection-root "$LIBNAME.LibBox" -o "$OUT"
if gate_cache_check "$OUT" "trim-reflection-rooted|no-ildiet|$CORELIB$(_gate_ctx_extras)" \
        "$APP" "$LIBDLL" "${APP%.dll}.runtimeconfig.json" "${APP%.dll}.deps.json"; then
    gate_cache_hit_msg
else
    compile_console "$OUT" "$PROJECT"
    set +e
    native=$("./$OUT/$PROJECT"); native_code=$?
    set -e
    expected=$(run_bounded dotnet "$APP")
    assert_output "$(strip_cr_win "$native")" "$(strip_cr_win "$expected")"
    assert_exit_code "$native_code" 0
    assert_delegate_method_lines "$native" \
        '  inherited slot -> LibShape/shape' \
        '  overriding level -> LibCircle/circle' \
        '  inherited override -> LibCircle/circle' \
        '  generic virtual -> LibGvmLeaf/gvm-leaf' \
        '  interface slot -> LibWidget/8' \
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
    assert_named_delegate_lines "$native" '  library name -> 42' \
        '  library missing soft name -> null' '  library static soft name -> null'
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
echo "== Isolated member reads without selected delegate identities =="
dotnet build samples/dotnet/TrimReflect/StrippedMembersOnly.csproj -c "$CONFIG" --nologo -v q -m:1
strip_app="samples/dotnet/TrimReflect/bin/$CONFIG/$TFM/StrippedMembersOnly.dll"
strip_clr=$(run_bounded dotnet "$strip_app")
for mode in plain trimmed rooted; do
    OUT="artifacts/trimreflect-stripped-members-$mode"
    strip_flags=()
    case "$mode" in
        trimmed) strip_flags=(--trim-reflection --reflection-metadata "$LIBNAME.LibWidget=native") ;;
        rooted) strip_flags=(--trim-reflection --reflection-root "$LIBNAME.LibWidget" --reflection-root "$LIBNAME.LibBox") ;;
    esac
    invoke_cli "$strip_app" --no-ildiet -r "$CORELIB" -r "$LIBDLL" ${strip_flags[@]+"${strip_flags[@]}"} -o "$OUT"
    compile_console "$OUT" StrippedMembersOnly
    strip_native=$(strip_cr_win "$(run_bounded "$OUT/StrippedMembersOnly$EXE_EXT")")
    for line in '== isolated stripped member metadata ==' 'direct interface=42/lib' \
        'isolated stripped member metadata end'; do
        test "$(grep -Fxc -- "$line" <<< "$strip_native")" = 1 \
            || { echo "FAIL: isolated member metadata witness: $mode/$line" >&2; exit 1; }
    done
    if [ "$mode" = plain ]; then
        assert_output "$strip_native" "$(strip_cr_win "$strip_clr")"
        continue
    fi
    for owner in LibWidget LibGadget LibBox_Int32; do
        if [ "$mode" = rooted ] && [ "$owner" != LibGadget ]; then
            continue
        fi
        grep -Eq "^const Dn2CppTypeInfo ti_TrimReflectLib_$owner = .*DN2CPP_TF_METADATA_STRIPPED" "$OUT"/generated*.cpp \
            || { echo "FAIL: unrelated member owner was kept: $mode/$owner" >&2; exit 1; }
        grep -Fq "PNSE: Reflection over the members of 'TrimReflectLib.$owner'" <<< "$strip_native" \
            || { echo "FAIL: stripped member read did not name its owner: $mode/$owner" >&2; exit 1; }
    done
    if [ "$mode" = trimmed ]; then
        test "$(grep -Fc -- ' -> PNSE:' <<< "$strip_native")" = 8
        grep -qw 'md_native_refl_ti_TrimReflectLib_LibWidget' "$OUT"/generated*.cpp
    else
        test "$(grep -Fc -- ' -> PNSE:' <<< "$strip_native")" = 2
        for line in 'widget methods -> any=True' 'widget field -> found=True' \
            'widget property -> found=True' 'widget name bind -> 42' \
            'box methods -> any=True' 'box field -> found=True'; do
            grep -Fxq -- "$line" <<< "$strip_native" \
                || { echo "FAIL: rooted member read missing: $line" >&2; exit 1; }
        done
    fi
done
echo "OK"
