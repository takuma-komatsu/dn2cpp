#!/usr/bin/env bash
# The platform-ISA surface, written in four places, agreeing: the generator's
# map (tools/gen-isa-map), the transpiler's family table it writes
# (src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs), the runtime's token
# header and helper headers under runtime/core/isa/, and the CLI's own account
# of what it lowers (--dump-isa-surface). A family may be Lowered only when
# every public static method of it has a helper the runtime defines, and a
# nested Lowered family implies its enclosing one; this gate is what decides
# that, so a family's IsSupported never answers true over a hole in its
# instruction surface. CoreLib-only calls are not part of that public surface;
# their hand-written helper residue is checked separately below.
#
# It runs no native build and no binary, so it never gate_skips, and it
# deliberately takes no result cache: its inputs are the generator's own output
# and the CLI's, and a key that forgot one of them would replay a green over
# exactly the drift this gate exists to catch.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_platform_isa.sh"

# Every check compares two SORTED sets and `comm` rejects input sorted under a
# different collation than its own; the names compared are ASCII either way.
export LC_ALL=C

echo "== platform-ISA surface (cli:$(_gate_cli_hash)) =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

TOKENS_H=runtime/core/isa/dn2cpp_isa_tokens.g.h
MANIFEST=runtime/core/isa/dn2cpp_isa_manifest.txt
for f in "$PLATFORM_ISA_TABLE" "$TOKENS_H" "$MANIFEST"; do
    [ -f "$f" ] || { echo "FAIL: $f not found; regenerate with: dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib $corelib" >&2; exit 1; }
done

echo "== 1/7 The generator reproduces the checked-in tree byte for byte =="
# --check exits nonzero when regenerating would change any file, which is the
# completeness proof: a family is written Lowered only when every public static
# method has a map row, and a hand edit to any generated file fails here.
dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib "$corelib" --check

echo "== 2/7 The CLI's account of the surface =="
build_proj "samples/dotnet/$PLATFORM_ISA_PROJECT/$PLATFORM_ISA_PROJECT.csproj"
app="samples/dotnet/$PLATFORM_ISA_PROJECT/bin/$CONFIG/$TFM/$PLATFORM_ISA_PROJECT.dll"
[ -f "$app" ] || { echo "error: not built: $app" >&2; exit 1; }
scratch=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_isa_surface.XXXXXX")
surface="$scratch/surface.tsv"
invoke_cli "$app" -r "$corelib" --dump-isa-surface "$surface" -o "$scratch/out"
[ -s "$surface" ] || { echo "FAIL: --dump-isa-surface wrote nothing to $surface" >&2; exit 1; }
echo "surface rows: $(grep -c . "$surface")"

# family<TAB>Row<TAB>arch<TAB>token<TAB>lowered ; method<TAB>Row<TAB>Method<TAB>helper
cli_families=$(awk -F'\t' '$1 == "family" { print $2 "\t" $5 }' "$surface" | sort -u)
cli_tokens=$(awk -F'\t' '$1 == "family" { print $4 }' "$surface" | sort -u)
cli_lowered=$(awk -F'\t' '$1 == "family" && $5 == "true" { print $2 }' "$surface" | sort -u)
table_fields=$(_platform_isa_table_fields)
table_families=$(awk -F'\t' '{ print $2 "\t" $4 }' <<<"$table_fields" | sort -u)
table_tokens=$(awk -F'\t' '{ print $3 }' <<<"$table_fields" | sort -u)
header_tokens=$(grep -oE 'DN2CPP_ISA_(X86|Arm|Wasm)_[A-Za-z0-9_]+' "$TOKENS_H" | sort -u)
manifest=$(grep -v '^[[:space:]]*$' "$MANIFEST" | sort -u || true)
# Helper DEFINITIONS in the per-arch headers: the DN2CPP_ISA_INLINE declaration
# lines, so the calls a body makes into dn2cpp_isa_common.h (dn2cpp_isa_require,
# dn2cpp_isa_not_lowered, the shared arithmetic) do not count as helpers. A tree
# with no header yet is empty.
helper_defs=$(grep -hE 'DN2CPP_ISA_INLINE' runtime/core/isa/*/dn2cpp_isa_*.h 2>/dev/null | grep -oE 'dn2cpp_isa_[a-z0-9_]+\(' | sed 's/($//' | sort -u || true)

echo "== 3/7 Family and token sets agree across the CLI, the table and the runtime header =="
platform_isa_set_eq "family rows and Lowered flags: CLI vs table" \
    "--dump-isa-surface" "$cli_families" "$PLATFORM_ISA_TABLE" "$table_families"
platform_isa_set_eq "token set: CLI vs runtime header" \
    "--dump-isa-surface" "$cli_tokens" "$TOKENS_H" "$header_tokens"
platform_isa_set_eq "token set: table vs runtime header" \
    "$PLATFORM_ISA_TABLE" "$table_tokens" "$TOKENS_H" "$header_tokens"

echo "== 4/7 The public helper manifest is exactly the set the runtime defines =="
platform_isa_set_eq "helper names: manifest vs runtime/core/isa/*/ definitions" \
    "$MANIFEST" "$manifest" "helper headers" "$helper_defs"

echo "== 5/7 The BCL-internal helper residue is closed =="
internal_header=runtime/core/isa/dn2cpp_isa_bcl_internal.h
internal_want=$(printf '%s\n' \
    dn2cpp_isa_x86_x86base_bitscanforward_u32 \
    dn2cpp_isa_x86_x86base_bitscanreverse_u32 \
    dn2cpp_isa_x86_x86base_x64_bigmul_i64_i64 \
    dn2cpp_isa_x86_x86base_x64_bigmul_u64_u64 \
    dn2cpp_isa_x86_x86base_x64_bitscanforward_u64 \
    dn2cpp_isa_x86_x86base_x64_bitscanreverse_u64 | sort -u)
internal_defs=$(grep -E 'DN2CPP_ISA_INLINE' "$internal_header" \
    | grep -oE 'dn2cpp_isa_[a-z0-9_]+\(' | sed 's/($//' | sort -u)
platform_isa_set_eq "BCL-internal helpers: required residue vs definitions" \
    "CoreLib internal ISA calls" "$internal_want" "$internal_header" "$internal_defs"

echo "== 6/7 Every Lowered family has every helper, and a nested Lowered family has a Lowered parent =="
n_lowered=$(grep -c . <<<"$cli_lowered" || true)
if [ "$n_lowered" -eq 0 ]; then
    platform_isa_ok "no family is Lowered; the completeness check has nothing to cover"
fi
while IFS= read -r fam; do
    [ -n "$fam" ] || continue
    want=$(awk -F'\t' -v f="$fam" '$1 == "method" && $2 == f { print $4 }' "$surface" | sort -u)
    missing=$(comm -23 <(printf '%s\n' "$want") <(printf '%s\n' "$manifest") | tr '\n' ' ')
    if [ -z "${missing// }" ]; then
        platform_isa_ok "$fam: every helper the CLI expects is in the manifest ($(grep -c . <<<"$want") methods)"
    else
        platform_isa_bad "$fam is Lowered but the manifest lacks: [$missing]"
    fi
    case "$fam" in
        *.*.*)
            parent="${fam%.*}"
            if grep -qxF "$parent" <<<"$cli_lowered"; then
                platform_isa_ok "$fam Lowered ⇒ $parent Lowered"
            else
                platform_isa_bad "$fam is Lowered but its enclosing family $parent is not"
            fi
            ;;
    esac
done <<<"$cli_lowered"

echo "== 7/7 A Lowered family implies only Lowered families =="
# The feature bits a family requires (families.csv) and what each bit implies
# (the DN2CPP_CPU_FEATURE_TABLE parents in dn2cpp_cpu_features.h) define the
# families a Lowered one must carry with it: every family whose bits lie in the
# implication closure of its own. Otherwise Dp.IsSupported could be true while
# AdvSimd.IsSupported is the constant 0, a state .NET never has. Re-derived here
# from the two sources rather than trusted from the generator's table.
cpu_header=runtime/core/dn2cpp_cpu_features.h
isa_csv=tools/gen-isa-map/families.csv
parents_tsv=$(sed -nE 's/^[[:space:]]*X\(([A-Z0-9_]+),[[:space:]]*"[^"]*",[[:space:]]*[A-Z0-9]+,[[:space:]]*([^)]*)\).*$/\1\t\2/p' "$cpu_header" \
    | sed -E 's/DN2CPP_CPU_//g; s/[[:space:]]*\|[[:space:]]*/ /g; s/\t0$/\t/')
family_bits=$(awk -F, '!/^#/ && $1 != "qualified_name" && $3 != "" { sub(/^System\.Runtime\.Intrinsics\./, "", $1); gsub(/\+/, ".", $1); print $1 "\t" $3 }' "$isa_csv" | tr '|' ' ')
n_parents=$(grep -c . <<<"$parents_tsv" || true)
n_bits=$(grep -cE '^[[:space:]]*X\(' "$cpu_header" || true)
if [ "$n_parents" -ne "$n_bits" ] || [ "$n_parents" -eq 0 ]; then
    platform_isa_bad "$cpu_header: $n_bits X(...) rows but $n_parents parse; a row's shape changed"
else
    platform_isa_ok "$cpu_header: $n_parents feature-bit rows with parents"
fi
implied_violations=$(awk -F'\t' -v lowered="$(tr '\n' ' ' <<<"$cli_lowered")" '
    FNR == NR { n = split($2, ps, " "); parents[$1] = ""; for (i = 1; i <= n; i++) parents[$1] = parents[$1] " " ps[i]; next }
    { bits[$1] = $2 }
    END {
        split(lowered, lw, " "); for (i in lw) if (lw[i] != "") isLowered[lw[i]] = 1
        for (f in bits) {
            if (!(f in isLowered)) continue
            # closure of f'"'"'s bits
            delete closure; n = split(bits[f], q, " "); head = 1; tail = n
            for (i = 1; i <= n; i++) closure[q[i]] = 1
            while (head <= tail) {
                b = q[head++]; m = split(parents[b], ps, " ")
                for (j = 1; j <= m; j++) if (ps[j] != "" && !(ps[j] in closure)) { closure[ps[j]] = 1; q[++tail] = ps[j] }
            }
            for (g in bits) {
                if (g == f) continue
                m = split(bits[g], gb, " "); inside = 1
                for (j = 1; j <= m; j++) if (!(gb[j] in closure)) inside = 0
                if (inside && !(g in isLowered)) print f " implies " g
            }
        }
    }' <(printf '%s\n' "$parents_tsv") <(printf '%s\n' "$family_bits") | sort)
if [ "$n_lowered" -eq 0 ]; then
    platform_isa_ok "no family is Lowered; the implication rule has nothing to cover"
elif [ -z "$implied_violations" ]; then
    platform_isa_ok "every family a Lowered family implies is Lowered ($n_lowered Lowered families checked)"
else
    platform_isa_bad "Lowered families implying non-Lowered ones: $(tr '\n' ';' <<<"$implied_violations")"
fi

rm -rf "$scratch"
echo
if [ "$PLATFORM_ISA_FAILS" -ne 0 ]; then
    printf 'error: %d of %d platform-ISA surface checks failed.\n' "$PLATFORM_ISA_FAILS" "$PLATFORM_ISA_CHECKS" >&2
    exit 1
fi
printf '== OK — %d platform-ISA surface checks, all true ==\n' "$PLATFORM_ISA_CHECKS"
