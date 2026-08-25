#!/usr/bin/env bash
# Binary-size attribution — where the bytes of a dn2cpp-built native binary go,
# and how a minimal `Console.WriteLine("Hello")` compares against NativeAOT.
#
# This is a measurement aid, NOT a regression gate: absolute byte counts drift
# with the toolchain and the installed CoreLib, so it prints numbers only and
# never asserts pass/fail. The `measure-*` name keeps it out of run-all-gates.sh,
# which discovers gates by the `build-and-run-*` glob.
#
# It reads the linked binary — it does not change how anything is built. The
# strip and dead-strip figures are measured on throwaway copies / scratch build
# directories; runtime/CMakeLists.txt and its defaults are untouched.
#
# How to read it: the per-bucket table attributes __TEXT/__DATA (or .text/.rodata)
# to the runtime, the vendored trees and the generated program, and the tail
# compares the stripped dn2cpp binary against a NativeAOT publish of the same
# program. Absolute bytes drift; the ratio and the bucket shares are the signal.
#
#   ./gates/measure-size.sh                # build MinimalHello both ways, compare
#   ./gates/measure-size.sh <binary>...    # attribute an existing binary instead
#   SIZE_RELINK=1 ./gates/measure-size.sh  # also relink with dead-strip (slower)
source "$(dirname "$0")/_common.sh"

OUT="${DN2CPP_SIZE_OUT:-artifacts/sizemeasure}"
RUNTIME_LIB_DIR="${DN2CPP_SIZE_RUNTIME_LIBS:-artifacts/.cmake-runtime}"

case "$DN2CPP_OS" in
    macos) IS_MACHO=1 ;;
    *)     IS_MACHO=  ;;
esac

# ── shared awk fragments ──────────────────────────────────────────────────────
# hex2dec: awk has no strtonum outside gawk, and macOS ships BWK awk.
# classify: map a linker symbol to the bucket it belongs to. Mangled C++ names
# arrive as `__ZL45methtab_Foo` (Mach-O) / `_ZL45methtab_Foo` (ELF); peel the
# leading underscores and the `Z[LN]<len>` prefix so the emitter's own naming
# (methtab_/ti_/m_/dn2cpp_) shows through.
read -r -d '' SIZE_AWK_LIB <<'AWKLIB' || true
function hex2dec(s,   i, c, d, n) {
    n = 0; s = tolower(s); sub(/^0x/, "", s)
    for (i = 1; i <= length(s); i++) {
        c = substr(s, i, 1); d = index("0123456789abcdef", c) - 1
        if (d < 0) return n
        n = n * 16 + d
    }
    return n
}
function demangle(s) {
    sub(/^_+/, "", s); sub(/^Z[LN]?[0-9]+/, "", s)
    return s
}
function classify(sym,   n) {
    n = demangle(sym)
    if (n ~ /^(methtab_|ctortab_|proptab_|parmtab_|parmpool_|itfpool_|itfspool_|genargpool_|arritfpool_|fldtab_|ti_|ty_|itf_|vt_|gendef_|rgctx_|enummembers_|genargs_)/) return "reflection metadata"
    if (n ~ /^dn2cpp_(type|assembly)_registry(_count)?$/) return "reflection metadata"
    if (n ~ /^GC_/)                     return "bdwgc (Boehm GC)"
    if (n ~ /^(m_|g_)/)                 return "generated code (user IL)"
    if (n ~ /[Dd]n2[Cc]pp/)             return "runtime/core (C++)"
    return "other"
}
AWKLIB

# ── section geometry ──────────────────────────────────────────────────────────

# section_table BIN — the per-segment/section byte table.
section_table() {
    if [ -n "$IS_MACHO" ]; then size -m "$1"
    else size -A "$1"
    fi
}

# macho_section_end BIN SEG SECT — end address (addr+size) of one Mach-O section.
macho_section_end() {
    otool -l "$1" | awk -v seg="$2" -v sect="$3" "$SIZE_AWK_LIB"'
        $1 == "sectname" { sn = $2 }
        $1 == "segname"  { sg = $2 }
        $1 == "addr"     { a = hex2dec($2) }
        $1 == "size"     { if (sn == sect && sg == seg) { print a + hex2dec($2); exit } }
    '
}

# ── symbol-table cost ─────────────────────────────────────────────────────────

# symtab_cost BIN — what the binary pays for symbol names and dynamic exports.
symtab_cost() {
    local bin="$1"
    if [ -n "$IS_MACHO" ]; then
        otool -l "$bin" | awk '
            /cmd LC_SYMTAB/            { in_st = 1 }
            in_st && $1 == "nsyms"     { nsyms = $2 }
            in_st && $1 == "strsize"   { strsize = $2; in_st = 0 }
            /cmd LC_DYLD_EXPORTS_TRIE/ { in_ex = 1 }
            in_ex && $1 == "datasize"  { trie = $2; in_ex = 0 }
            END {
                st = nsyms * 16
                printf "  symbol table (nsyms %d x 16B) : %12d B\n", nsyms, st
                printf "  symbol name strings           : %12d B\n", strsize
                printf "  dynamic export trie           : %12d B\n", trie
                printf "  ----------------------------- : %12d B  (strippable)\n", st + strsize + trie
            }'
        if command -v dyld_info >/dev/null 2>&1; then
            printf "  exported symbols              : %12d\n" "$(dyld_info -exports "$bin" 2>/dev/null | wc -l | tr -d ' ')"
        fi
    else
        readelf -S -W "$bin" 2>/dev/null | awk '
            $2 == ".symtab" || $2 == ".strtab" || $2 == ".dynsym" || $2 == ".dynstr" {
                sz = strtonum("0x" $6); printf "  %-29s : %12d B\n", $2, sz
                if ($2 == ".symtab" || $2 == ".strtab") tot += sz
            }
            END { printf "  ----------------------------- : %12d B  (strippable)\n", tot }'
        printf "  exported symbols              : %12d\n" "$(nm -D --defined-only "$bin" 2>/dev/null | wc -l | tr -d ' ')"
    fi
}

# strip_saving BIN — strip a throwaway copy and report the delta. The original
# is never touched. NOTE: on arm64 macOS `strip` invalidates the ad-hoc code
# signature, so a real strip in the build would have to re-sign (codesign -f -s -).
strip_saving() {
    local bin="$1" tmp before after
    tmp="$(mktemp -d)"
    cp "$bin" "$tmp/stripped"
    if [ -n "$IS_MACHO" ]; then strip "$tmp/stripped" >/dev/null 2>&1 || true
    else strip --strip-all "$tmp/stripped" >/dev/null 2>&1 || true
    fi
    before=$(wc -c < "$bin" | tr -d ' ')
    after=$(wc -c < "$tmp/stripped" | tr -d ' ')
    rm -rf "$tmp"
    awk -v b="$before" -v a="$after" 'BEGIN {
        printf "  as linked                     : %12d B\n", b
        printf "  after strip                   : %12d B\n", a
        printf "  saving                        : %12d B  (%.1f%%)\n", b - a, 100.0 * (b - a) / b
    }'
}

# ── per-symbol sizes ──────────────────────────────────────────────────────────

# sym_sizes BIN KIND — emit "size<TAB>symbol" for every symbol in the code
# section (KIND=text) or the read-only data section (KIND=const).
#
# Mach-O carries no symbol sizes (`nm -S` warns and prints zeros), so sizes come
# from differencing consecutive addresses within one section — `nm -n` sorts by
# address and a section's symbols are contiguous, so the last one is closed off
# with the section's end address. ELF's `nm -S` reports real sizes.
sym_sizes() {
    local bin="$1" kind="$2"
    if [ -n "$IS_MACHO" ]; then
        local seg sect end
        if [ "$kind" = text ]; then seg=__TEXT; sect=__text
        else seg=__DATA_CONST; sect=__const
        fi
        end="$(macho_section_end "$bin" "$seg" "$sect")"
        [ -n "$end" ] || return 0
        nm -n -m "$bin" 2>/dev/null | awk -v want="($seg,$sect)" -v end="$end" "$SIZE_AWK_LIB"'
            $2 == want { addr[++n] = hex2dec($1); name[n] = $NF }
            END {
                for (i = 1; i < n; i++) printf "%d\t%s\n", addr[i+1] - addr[i], name[i]
                if (n > 0) printf "%d\t%s\n", end - addr[n], name[n]
            }'
    else
        local types
        if [ "$kind" = text ]; then types="tT"; else types="rR"; fi
        nm -n -S --defined-only "$bin" 2>/dev/null | awk -v types="$types" "$SIZE_AWK_LIB"'
            NF >= 4 && index(types, $3) > 0 { printf "%d\t%s\n", hex2dec($2), $4 }'
    fi
}

# category_breakdown BIN KIND — bucket one section by symbol origin.
category_breakdown() {
    local bin="$1" kind="$2"
    sym_sizes "$bin" "$kind" | awk -F'\t' "$SIZE_AWK_LIB"'
        { g[classify($2)] += $1; tot += $1 }
        END {
            if (tot == 0) { print "  (no symbols — already stripped?)"; exit }
            n = 0
            for (k in g) { keys[++n] = k }
            for (i = 1; i < n; i++) for (j = i + 1; j <= n; j++)
                if (g[keys[j]] > g[keys[i]]) { t = keys[i]; keys[i] = keys[j]; keys[j] = t }
            for (i = 1; i <= n; i++) printf "  %12d B  %5.1f%%  %s\n", g[keys[i]], 100.0 * g[keys[i]] / tot, keys[i]
            printf "  %12d B  100.0%%  TOTAL\n", tot
        }'
}

# tu_attribution BIN — which runtime translation unit dragged how much code in.
# Static archives link at `.o` granularity: one referenced symbol pulls its whole
# translation unit, unused functions included. Cross-references the binary's code
# symbols against the archives' member symbol tables to make that visible.
tu_attribution() {
    local bin="$1"
    # Ordered, not globbed: a name defined in more than one archive (an inline the
    # runtime and the Godot layer both emit) must be credited to the archive the
    # console link actually pulls it from, and first-wins resolves that.
    local libs=() lib
    for lib in dn2cpp_runtime dn2cpp_gc dn2cpp_zlib dn2cpp_brotli dn2cpp_godot dn2cpp_dotnetmodule; do
        [ -f "$RUNTIME_LIB_DIR/lib$lib.a" ] && libs+=("$RUNTIME_LIB_DIR/lib$lib.a")
    done
    if [ "${#libs[@]}" -eq 0 ]; then
        echo "  (no runtime archives under $RUNTIME_LIB_DIR — skipped)"
        return 0
    fi
    # Every defined symbol, local ones included: a static function is what makes a
    # translation unit big, and it is exactly what `nm`'s uppercase-only types miss.
    # U/w/v are undefined or weak-undefined — they define nothing.
    local owners; owners="$(mktemp)"
    nm "${libs[@]}" 2>/dev/null | awk '
        /\.o\)?:$/ { cur = $0; sub(/.*\(/, "", cur); sub(/\)?:$/, "", cur); next }
        /^[0-9a-f]+ [A-Za-z] / && $2 != "U" && $2 != "w" && $2 != "v" {
            if (!($3 in seen)) { seen[$3] = 1; print $3 "\t" cur }
        }
    ' > "$owners"
    sym_sizes "$bin" text | awk -F'\t' -v owners="$owners" "$SIZE_AWK_LIB"'
        BEGIN { while ((getline line < owners) > 0) { split(line, f, "\t"); own[f[1]] = f[2] } }
        {
            o = ($2 in own) ? own[$2] : ("[" classify($2) "]")
            g[o] += $1; tot += $1
        }
        END {
            if (tot == 0) { print "  (no symbols — already stripped?)"; exit }
            n = 0
            for (k in g) keys[++n] = k
            for (i = 1; i < n; i++) for (j = i + 1; j <= n; j++)
                if (g[keys[j]] > g[keys[i]]) { t = keys[i]; keys[i] = keys[j]; keys[j] = t }
            for (i = 1; i <= n; i++) printf "  %12d B  %5.1f%%  %s\n", g[keys[i]], 100.0 * g[keys[i]] / tot, keys[i]
            printf "  %12d B  100.0%%  TOTAL __text\n", tot
        }'
    rm -f "$owners"
}

# largest_symbols BIN KIND N — the individual symbols that dominate a section.
largest_symbols() {
    head -"$3" <<<"$(sym_sizes "$1" "$2" | sort -rn)" | awk -F'\t' '
        { printf "  %10d B  %s\n", $1, $2; seen = 1 }
        END { if (!seen) print "  (no symbols — already stripped?)" }'
}

# ── report ────────────────────────────────────────────────────────────────────

report() {
    local bin="$1"
    echo
    echo "════════════════════════════════════════════════════════════════"
    echo " $bin  ($(wc -c < "$bin" | tr -d ' ') bytes)"
    echo "════════════════════════════════════════════════════════════════"
    echo "-- segments / sections --"
    section_table "$bin"
    echo
    echo "-- symbol-table cost --"
    symtab_cost "$bin"
    echo
    echo "-- strip (measured on a copy; the build is unchanged) --"
    strip_saving "$bin"
    echo
    echo "-- code: bytes by origin --"
    category_breakdown "$bin" text
    echo
    echo "-- code: which runtime .o pulled it in --"
    tu_attribution "$bin"
    echo
    echo "-- read-only data: bytes by origin --"
    category_breakdown "$bin" const
    echo
    echo "-- read-only data: 10 largest symbols --"
    largest_symbols "$bin" const 10
}

# ── dead-strip contribution ───────────────────────────────────────────────────
# Relinks the SAME generated C++ into a scratch directory, letting the build's
# default dead-strip apply, and diffs it against the opt-out baseline. Strip is
# off on both sides so the delta isolates dead-strip alone.
#
# Only meaningful for an executable. A Mach-O executable's globals are not
# dead-strip roots (the export trie is built after the strip), so the linker walks
# out from the entry point and drops whole unreferenced translation-unit tails —
# large. A shared library exports every global by default, which makes every global
# a root, so the build skips dead-strip there.
relink_dead_strip() {
    # Separate `local` statements: bash 3.2 (the macOS system bash) does not see an
    # earlier name of the same `local` statement, and `set -u` turns that into a fatal.
    local src="$1" name="$2"
    local dst="$src-deadstrip"
    local flags="-Wl,-dead_strip"
    [ -n "$IS_MACHO" ] || flags="-Wl,--gc-sections"
    rm -rf "$dst"; mkdir -p "$dst"
    cp "$src"/generated*.cpp "$src"/generated.h "$dst"/
    [ -f "$src/pinvoke-libs.txt" ] && cp "$src/pinvoke-libs.txt" "$dst"/
    [ -f "$src/pinvoke-symbols.txt" ] && cp "$src/pinvoke-symbols.txt" "$dst"/
    ( export DN2CPP_EXTRA_CMAKE_ARGS="-DDN2CPP_STRIP=OFF"
      compile_console "$dst" "$name" )
    awk -v b="$(wc -c < "$src/$name" | tr -d ' ')" -v a="$(wc -c < "$dst/$name" | tr -d ' ')" -v f="$flags" 'BEGIN {
        printf "  as linked                     : %12d B\n", b
        printf "  with %-24s : %12d B\n", f, a
        printf "  saving                        : %12d B  (%.1f%%)\n", b - a, 100.0 * (b - a) / b
    }'
}

# ── default mode: MinimalHello, dn2cpp vs NativeAOT ───────────────────────────

if [ "$#" -gt 0 ]; then
    for bin in "$@"; do
        [ -f "$bin" ] || { echo "error: not a file: $bin" >&2; exit 1; }
        report "$bin"
    done
    exit 0
fi

case "$DN2CPP_OS-$(uname -m)" in
    macos-arm64)               RID=osx-arm64 ;;
    macos-x86_64)              RID=osx-x64 ;;
    linux-aarch64|linux-arm64) RID=linux-arm64 ;;
    linux-x86_64)              RID=linux-x64 ;;
    *) echo "error: unsupported OS/arch for the NativeAOT publish: $DN2CPP_OS/$(uname -m)" >&2; exit 1 ;;
esac

rm -rf "$OUT"
PROJ="$OUT/MinimalHello"
mkdir -p "$PROJ"

echo "== 1/5 Synthesizing a minimal hello world =="
# Not checked into samples/dotnet/: it exists only to be measured, and the same
# csproj must feed both the dn2cpp and the NativeAOT build. InvariantGlobalization
# matches src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj and keeps NativeAOT's ICU out of the
# comparison — dn2cpp has no ICU to begin with.
cat > "$PROJ/MinimalHello.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <Optimize>true</Optimize>
    <DebugType>none</DebugType>
    <InvariantGlobalization>true</InvariantGlobalization>
  </PropertyGroup>

</Project>
EOF
cat > "$PROJ/Program.cs" <<'EOF'
using System;

internal static class Program
{
    private static int Main()
    {
        Console.WriteLine("Hello");
        return 0;
    }
}
EOF
dotnet build "$PROJ/MinimalHello.csproj" -c "$CONFIG" --nologo -v q

echo "== 2/5 Transpiling IL -> C++ -> native =="
DN2CPP_BIN="$OUT/dn2cpp-build"
invoke_cli "$PROJ/bin/$CONFIG/$TFM/MinimalHello.dll" -o "$DN2CPP_BIN"
# Attribution needs a symbol table and the untrimmed set of translation units, so
# this build opts out of the committed defaults: it is the "as linked" baseline
# every breakdown below is computed on. The committed build (strip + dead-strip)
# is measured separately, in step 5.
( export DN2CPP_EXTRA_CMAKE_ARGS="-DDN2CPP_STRIP=OFF -DDN2CPP_DEAD_STRIP=OFF"
  compile_console "$DN2CPP_BIN" MinimalHello )
"./$DN2CPP_BIN/MinimalHello"

# The binary the build actually ships, from the same generated C++.
DN2CPP_SHIPPED="$OUT/dn2cpp-shipped"
rm -rf "$DN2CPP_SHIPPED"; mkdir -p "$DN2CPP_SHIPPED"
cp "$DN2CPP_BIN"/generated*.cpp "$DN2CPP_BIN"/generated.h "$DN2CPP_SHIPPED"/
[ -f "$DN2CPP_BIN/pinvoke-libs.txt" ] && cp "$DN2CPP_BIN/pinvoke-libs.txt" "$DN2CPP_SHIPPED"/
[ -f "$DN2CPP_BIN/pinvoke-symbols.txt" ] && cp "$DN2CPP_BIN/pinvoke-symbols.txt" "$DN2CPP_SHIPPED"/
compile_console "$DN2CPP_SHIPPED" MinimalHello
"./$DN2CPP_SHIPPED/MinimalHello"

echo "== 3/5 NativeAOT publish of the same project =="
AOT_BIN="$OUT/aot-publish"
dotnet publish "$PROJ/MinimalHello.csproj" -c "$CONFIG" -r "$RID" \
    --self-contained -p:PublishAot=true -o "$AOT_BIN" --nologo -v q
"./$AOT_BIN/MinimalHello"

echo "== 4/5 Attribution =="
report "$DN2CPP_BIN/MinimalHello"
report "$AOT_BIN/MinimalHello"

if [ -n "${SIZE_RELINK:-}" ]; then
    echo
    echo "-- dead-strip relink (scratch build dir; defaults untouched) --"
    relink_dead_strip "$DN2CPP_BIN" MinimalHello
fi

echo
echo "== 5/5 Summary =="
dn_raw=$(wc -c < "$DN2CPP_BIN/MinimalHello" | tr -d ' ')
dn_ship=$(wc -c < "$DN2CPP_SHIPPED/MinimalHello" | tr -d ' ')
aot_raw=$(wc -c < "$AOT_BIN/MinimalHello" | tr -d ' ')
tmp="$(mktemp -d)"
cp "$AOT_BIN/MinimalHello" "$tmp/aot"
if [ -n "$IS_MACHO" ]; then strip "$tmp/aot" >/dev/null 2>&1 || true
else strip --strip-all "$tmp/aot" >/dev/null 2>&1 || true
fi
aot_str=$(wc -c < "$tmp/aot" | tr -d ' ')
rm -rf "$tmp"

echo "════════════════════════════════════════════════════════════════"
echo " Minimal hello world — single native executable ($RID)"
echo "════════════════════════════════════════════════════════════════"
# "as linked" is the -DDN2CPP_STRIP=OFF build the attribution above ran on;
# "shipped" is what the default build emits. NativeAOT ships unstripped and
# keeps its symbols in a separate .dSYM, so it is shown both ways.
awk -v dr="$dn_raw" -v dsh="$dn_ship" -v ar="$aot_raw" -v as="$aot_str" 'BEGIN {
    printf "  %-22s %12s %12s\n", "", "as linked", "shipped"
    printf "  %-22s %12d %12d\n", "dn2cpp", dr, dsh
    printf "  %-22s %12d %12d\n", "NativeAOT", ar, as
    printf "\n  ratio (dn2cpp / aot)   %12.2fx %11.2fx\n", dr / ar, dsh / as
}'
echo
echo "Absolute bytes drift with the toolchain; compare the ratio, not the totals."
