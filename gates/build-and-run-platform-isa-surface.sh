#!/usr/bin/env bash
# The platform-ISA surface, written in four places, agreeing: the generator's
# map (tools/gen-isa-map), the transpiler's family table it writes
# (src/Dn2Cpp.Transpiler/CoreIntrinsics.PlatformIsa.g.cs), the runtime's token
# header and helper headers under runtime/core/isa/, and the CLI's own account
# of what it lowers (--dump-isa-surface). A family may be Lowered only when
# every public static method of it has a helper the runtime defines, and a
# nested Lowered family implies its enclosing one; this gate is what decides
# that, so a family's IsSupported never answers true over a hole in its
# instruction surface.
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

echo "== 1/5 The generator reproduces the checked-in tree byte for byte =="
# --check exits nonzero when regenerating would change any file, which is the
# completeness proof: a family is written Lowered only when every public static
# method has a map row, and a hand edit to any generated file fails here.
dotnet run tools/gen-isa-map/gen-isa-map.cs -- --corelib "$corelib" --check

echo "== 2/5 The CLI's account of the surface =="
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

echo "== 3/5 Family and token sets agree across the CLI, the table and the runtime header =="
platform_isa_set_eq "family rows and Lowered flags: CLI vs table" \
    "--dump-isa-surface" "$cli_families" "$PLATFORM_ISA_TABLE" "$table_families"
platform_isa_set_eq "token set: CLI vs runtime header" \
    "--dump-isa-surface" "$cli_tokens" "$TOKENS_H" "$header_tokens"
platform_isa_set_eq "token set: table vs runtime header" \
    "$PLATFORM_ISA_TABLE" "$table_tokens" "$TOKENS_H" "$header_tokens"

echo "== 4/5 The helper manifest is exactly the set the runtime defines =="
platform_isa_set_eq "helper names: manifest vs runtime/core/isa/*/ definitions" \
    "$MANIFEST" "$manifest" "helper headers" "$helper_defs"

echo "== 5/5 Every Lowered family has every helper, and a nested Lowered family has a Lowered parent =="
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

rm -rf "$scratch"
echo
if [ "$PLATFORM_ISA_FAILS" -ne 0 ]; then
    printf 'error: %d of %d platform-ISA surface checks failed.\n' "$PLATFORM_ISA_FAILS" "$PLATFORM_ISA_CHECKS" >&2
    exit 1
fi
printf '== OK — %d platform-ISA surface checks, all true ==\n' "$PLATFORM_ISA_CHECKS"
