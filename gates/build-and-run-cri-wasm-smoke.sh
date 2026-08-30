#!/usr/bin/env bash
# CRI ADX LE console-wasm link smoke: prove symbol resolution and the P/Invoke
# path against the REAL CRI archives before any engine is involved. The
# browser-flavor CriWare.CriAtomLE.dll is transpiled with
# `--pinvoke-module cri_atom`, linked as a wasm MAIN module against
# runtimes/browser-wasm/{cri_atom.a, libcri_ware_le.a} plus the package's three
# Emscripten JS libraries (a main module links jslibs directly — the .targets
# recipe: --js-library ×3 + -sDEFAULT_LIBRARY_FUNCS_TO_INCLUDE=$dynCall), and
# run under node.
#
# node has no AudioContext, so only the NO-AUDIO surface is asserted (see the
# sample's header for exactly what that is: version string, IsInitialized, and
# the error-callback machinery — raw-fnptr registration round-trip, the
# binding's own callback object, errid->message conversion, error counters —
# all without CriAtomCSharp.Initialize). Audio output stays for the Web E2E.
#
# The diff is against a frozen embedded expectation, not real .NET: the
# browser-flavor DLL + wasm-only native archives cannot run on the host, so
# there is no oracle to diff against. The expectation is deterministic — the
# version line is the pinned 2.30.196 archive's banner, the errids are the
# engine's fixed identifiers — and any change of the pinned SDK drop
# (DN2CPP_CRI_VERSION) is expected to show up here as a red diff.
#
# The last section diffs import closures against the stock/CRI Release/Debug Web
# template matrix baked by gates/setup-godot-fork-web.sh.
# What the Web lane ships is a game SIDE MODULE the exported page dlopens, and
# emscripten's dlopen never fails on a missing symbol — the first call dies
# naming a function index — so the closure must be asserted statically, and it
# splits into two halves along what the CRI SDK's binary layout allows:
#
#   (a) The JS half. The .jslib functions can only live in the MAIN module (the
#       template), in whose glue they sit invisible to the main wasm's import
#       section — which is why `unsatisfied` takes the glue as a third argument
#       (gates/_wasm_symbols.js). The measured demand is the section-5 artifact:
#       the one shape the archives can link into, so its WAJS_*/WAASRJS_*/
#       criFsIoJs_* import set is the real jslib surface this program's slice of
#       the engine calls. All of it must resolve against the CRI template, and
#       — the negative control — must NOT resolve against the stock one, or the
#       checker's glue parsing has gone blind.
#   (b) The native half. BOTH CRI archives are compiled without -fPIC (wasm-ld:
#       R_WASM_MEMORY_ADDR_* "recompile with -fPIC", engine and facade alike),
#       so no part of CRI can static-link into ANY Emscripten side module. The
#       drop-in-shaped side module built here therefore links no CRI archive;
#       its whole cri_* P/Invoke surface becomes dynamic imports, which only
#       the template's MAIN module can satisfy — and it does: the fork's
#       dlink_main_extra_libs option whole-archive-links both archives into
#       the main-runtime link alone (plain linkflags could not express that —
#       they reach the engine's own side-module link too, where extracting the
#       non-PIC members is fatal), and EXPORT_ALL surfaces every cri_* symbol.
#       So (b) asserts the full closure: every import of the drop-in-shaped
#       side module resolves against the real template, cri_* included. A
#       template baked before that knob leaves the cri_* remainder open, and
#       the section degrades to a PARTIAL naming it and the rebuild remedy.
#
# All four templates are REFERENCES here, never subjects — every verdict in
# section 6 is read off their glue — so all are provenance-checked up front
# (godot_fork_template_check_soft, gates/_godot_fork.sh), in the degrading form:
# this gate holds no $FORK and an absent template is a supported state, so the
# check runs on what is present and the absence keeps its existing gate_partial.
#
# Skips (gate_skip, counted separately from greens by the runner): no
# Emscripten toolchain / node, or the proprietary CRI package absent
# (cri_* resolvers in _common.sh).
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_fork.sh"

# --no-bundled: an -O-less link needs the -debug archives the frozen bundle lacks.
dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcc emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found — no Emscripten toolchain to build wasm with"
    fi
done

CRI_ROOT=$(cri_nuget_root) || gate_skip "CRI ADX LE package not present"
BROWSER_DLL=$(cri_managed_dll browser) || gate_skip "CRI ADX LE browser-flavor managed DLL not present"
cri_wasm_native_files >/dev/null || gate_skip "CRI ADX LE browser-wasm native artifact set incomplete"
CRI_WASM_DIR="$CRI_ROOT/runtimes/browser-wasm"

export WASM=1

PROJECT=CriWasmSmoke
OUT="artifacts/criwasmsmoke"
LIBDIR="$PWD/$OUT/lib"

TEMPLATE_CRI="$FORK_ROOT/web_template_cri.zip"
TEMPLATE_CRI_DEBUG="$FORK_ROOT/web_template_cri_debug.zip"
TEMPLATE_STOCK="$FORK_ROOT/web_template.zip"
TEMPLATE_STOCK_DEBUG="$FORK_ROOT/web_template_debug.zip"

# Section 6 diffs this program's import closure against this template matrix's
# glue, so all are REFERENCES this gate's verdicts are read off. The soft form,
# because unlike the three export gates
# this one has no $FORK and tolerates an absent template by design: absent stays
# the caller's business (the gate_partial arms in section 6 name which half went
# unasserted), unresolvable degrades to a loud warning, present-and-wrong is the
# same refusal everywhere else makes. Live and BEFORE gate_cache_check, for the
# reason the export gates run their check there: `tmpl_cri=`/`tmpl_stock=`
# fingerprint the stale zips THEMSELVES — they hold still precisely while the
# artifact is wrong — and the fork engine sources their stamps are compared
# against are in no key at all. (gates/_godot_fork.sh's own content IS in the
# key; that closes a different hole and leaves this one exactly where it was.)
# So a check whose effect were only visible afterwards would let
# a warm key replay a green over the very artifact it was meant to refuse.
godot_fork_template_check_soft "$TEMPLATE_CRI" "CRI Web export template" \
    "CRI=1 gates/setup-godot-fork-web.sh"
godot_fork_template_check_soft "$TEMPLATE_CRI_DEBUG" "CRI Debug Web export template" \
    "CRI=1 gates/setup-godot-fork-web.sh"
godot_fork_template_check_soft "$TEMPLATE_STOCK" "stock Web export template" \
    "gates/setup-godot-fork-web.sh"
godot_fork_template_check_soft "$TEMPLATE_STOCK_DEBUG" "stock Debug Web export template" \
    "gates/setup-godot-fork-web.sh"

echo "== 1/6 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 2/6 Building app assembly (references the browser-flavor CRI DLL) =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
app="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"

echo "== 3/6 Transpiling app + real CoreLib + CriWare.CriAtomLE (--pinvoke-module cri_atom) =="
invoke_cli "$app" -r "$corelib" -r "$BROWSER_DLL" --pinvoke-module cri_atom -o "$OUT"
grep -qx 'cri_atom' <<<"$(strip_cr_win_file "$OUT/pinvoke-libs.txt")" \
    || { echo "FAIL: cri_atom missing from $OUT/pinvoke-libs.txt" >&2; exit 1; }

# Key inputs beyond the transpile surface: the browser-flavor managed DLL, the
# whole browser-wasm native artifact directory (both archives + the three
# jslibs are link inputs below), the four Web export templates section 6 diffs
# against (file_sig echoes "none" for an absent one, so template
# presence/absence moves the key too — a cached partial from before the CRI
# template existed does not outlive its arrival), and the wasm symbol checker
# whose verdicts section 6 asserts.
if gate_cache_check "$OUT" \
        "cri-wasm-smoke|$corelib|$BROWSER_DLL|$CRI_WASM_DIR|tmpl_cri=$(file_sig "$TEMPLATE_CRI")|tmpl_cri_debug=$(file_sig "$TEMPLATE_CRI_DEBUG")|tmpl_stock=$(file_sig "$TEMPLATE_STOCK")|tmpl_stock_debug=$(file_sig "$TEMPLATE_STOCK_DEBUG")" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" \
        "$BROWSER_DLL" "$CRI_WASM_DIR" gates/_wasm_symbols.js; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/6 Staging the CRI archives =="
# pinvoke-libs.txt carries the bare token `cri_atom` (-> -lcri_atom), but the
# package ships the facade archive WITHOUT the lib prefix (cri_atom.a, directly
# under the RID folder — a package-layout fact, see the resolvers' header), so
# stage a lib-prefixed copy where -L can find it. The engine archive
# libcri_ware_le.a needs no token: it rides DN2CPP_EXTRA_LINK_LIBS by full
# path, AFTER the facade in link order (facade members pull engine symbols).
mkdir -p "$LIBDIR"
cp -f "$CRI_WASM_DIR/cri_atom.a" "$LIBDIR/libcri_atom.a"

echo "== 5/6 Linking the CRI archives + jslibs, running under node =="
# $dynCall is a literal Emscripten library-function name, not a shell variable
# — keep it escaped (the package's .targets carries the same flag). The
# --js-library=path spelling is load-bearing: CMake de-duplicates repeated
# option tokens, so three space-separated `--js-library <path>` pairs reach
# em++ with two of the three flag tokens dropped and the orphaned paths are
# fed to wasm-ld as unknown-file-type link inputs.
# Every path here is converted with native_path (_common.sh), because each one
# is buried INSIDE a longer argument and MSYS converts only an argument that is
# a path by itself: em++ is a native Python program, and `--js-library=/c/Users/...`
# reached it verbatim and died with a FileNotFoundError naming the MSYS spelling
# — no mention of the path dialect, and the failure landed inside a build whose
# output was discarded, so the gate log named nothing at all.
_LIBDIR_N="$(native_path "$LIBDIR")"
_CRI_WASM_N="$(native_path "$CRI_WASM_DIR")"
DN2CPP_EXTRA_LINK_FLAGS="-L$_LIBDIR_N --js-library=$_CRI_WASM_N/crinc_webaudio.jslib --js-library=$_CRI_WASM_N/crinc_webaudio2.jslib --js-library=$_CRI_WASM_N/crifs_io.jslib -sDEFAULT_LIBRARY_FUNCS_TO_INCLUDE=\$dynCall" \
DN2CPP_EXTRA_LINK_LIBS="$_CRI_WASM_N/libcri_ware_le.a" \
compile_console_wasm "$OUT" "$PROJECT"

actual=$(node "./$OUT/$PROJECT.js")
# Frozen expectation (see header: no host oracle exists for this flavor). The
# version banner carries its own trailing newline — the blank line below it is
# the engine's, not an accident — and the errid string already embeds the
# message text, which is why ConvertIdToMessage echoes it verbatim.
expected=$(cat <<'EOF'
== CRI wasm smoke (no-audio surface) ==
atom version: CRI Atom(LE)/WASM Ver.2.30.196 Build:Jan 27 2026 21:11:05

initialized: False
counts before: err=0 warn=0
raw fired: 1 errid: E2014012101:Invalid parameter.
raw fired after unregister: 1
counts after raw phase: err=2 warn=0
object fired: 1 message: E2014012101:Invalid parameter.
object fired after remove: 1
counts after reset: err=0 warn=0
done
EOF
)
assert_output "$actual" "$expected"

echo "== 6/6 Import closure vs the CRI Web export template =="
if [ ! -f "$TEMPLATE_CRI" ]; then
    gate_partial "no CRI Web export template at $TEMPLATE_CRI — run CRI=1 gates/setup-godot-fork-web.sh;
    the import-closure halves (the build-time stand-in for emscripten's silent
    dlopen — see the header) were not asserted"
else
    TDIR="artifacts/criwasmsmoke-side/template"
    rm -rf "$TDIR"
    mkdir -p "$TDIR/cri"
    unzip -q "$TEMPLATE_CRI" godot.wasm godot.js -d "$TDIR/cri"

    # (a) The JS half, measured on the section-5 artifact (see header). First
    # the vacuity guard: the demand set must be non-empty, or tree-shaking (or
    # a lowering regression) silently emptied the assertion.
    grep -qE '^func env\.(criFsIoJs_|WAJS_|WAASRJS_)' \
            <<<"$(node gates/_wasm_symbols.js imports "$OUT/$PROJECT.wasm")" || {
        echo "FAIL: $OUT/$PROJECT.wasm imports no CRI jslib symbol — the JS-half closure would be vacuous" >&2
        exit 1
    }
    jslib_gap=$(node gates/_wasm_symbols.js unsatisfied "$OUT/$PROJECT.wasm" "$TDIR/cri/godot.wasm" "$TDIR/cri/godot.js" \
            | grep -E '^(GOT\.(mem|func)\.)?(criFsIoJs_|WAJS_|WAASRJS_)' || true)
    if [ -n "$jslib_gap" ]; then
        echo "FAIL: the CRI template's glue does not supply every .jslib function this program's" >&2
        echo "      slice of the engine calls:" >&2
        printf '%s\n' "$jslib_gap" | LC_ALL=C sed 's/^/  /' >&2
        echo "      Rebuild the template (CRI=1 gates/setup-godot-fork-web.sh)." >&2
        exit 1
    fi
    echo "JS half OK: the CRI template's glue supplies the artifact's whole jslib demand"

    # Negative control: the STOCK template's glue carries no CRI jslib, and the
    # checker must say so. This is what stands between the assertion above and
    # a glue parser that quietly started answering "provided" for everything.
    if [ -f "$TEMPLATE_STOCK" ]; then
        mkdir -p "$TDIR/stock"
        unzip -q "$TEMPLATE_STOCK" godot.wasm godot.js -d "$TDIR/stock"
        grep -qE '^(criFsIoJs_|WAJS_|WAASRJS_)' \
                <<<"$(node gates/_wasm_symbols.js unsatisfied "$OUT/$PROJECT.wasm" "$TDIR/stock/godot.wasm" "$TDIR/stock/godot.js")" || {
            echo "FAIL: the stock template appears to satisfy the CRI jslib demand — the" >&2
            echo "      unsatisfied checker has gone blind (or the stock template is CRI-linked)" >&2
            exit 1
        }
        echo "negative control OK: the stock template leaves the jslib demand unsatisfied"
    else
        # Silently dropping this arm is the failure to avoid: the JS half
        # above would keep printing OK with the blindness check gone, and a
        # box that only ever baked the CRI variant never bakes the stock one
        # (setup-godot-fork-web.sh writes web_template_cri.zip under CRI=1 and
        # web_template.zip only on a separate stock run), so the loss is
        # permanent on that box rather than momentary. Environmental, not
        # structural — hence gate_partial, matching the CRI-template arm above.
        gate_partial "no stock Web export template at $TEMPLATE_STOCK — run
    gates/setup-godot-fork-web.sh (without CRI=1); the negative control that proves the
    unsatisfied checker can still say 'not provided' did not run, so the JS-half
    assertion above is unguarded against a glue parser that answers 'provided' for
    everything. The JS half itself and the native half below DID run and hold."
    fi

    # (b) The native half: the drop-in-shaped side module. A symlink film over
    # the transpile output is this build's app dir, so the side module is built
    # from byte-identical sources without disturbing $OUT (whose contents are
    # the gate cache's transpile-surface key) and without sharing $OUT's CMake
    # build dir. pinvoke-libs.txt is deliberately NOT in the film, and no CRI
    # archive rides the link: neither archive is PIC, so the drop-in cannot
    # carry them (see header) — the whole cri_* surface must arrive as dynamic
    # imports, exactly as asserted below. The film is re-laid every run (a
    # stale symlink from a previous, larger emit must not feed the glob), and
    # the -U args clear any LINK_FLAGS/LINK_LIBS a previous configure cached.
    SIDE_OUT="artifacts/criwasmsmoke-side"
    rm -f "$SIDE_OUT"/generated*
    for f in "$PWD/$OUT"/generated*.cpp "$PWD/$OUT/generated.h"; do
        ln -s "$f" "$SIDE_OUT/$(basename "$f")"
    done
    SIDE_SO="$SIDE_OUT/lib$PROJECT.so"
    DN2CPP_EXTRA_CMAKE_ARGS="-UDN2CPP_APP_LINK_FLAGS -UDN2CPP_APP_LINK_LIBS" \
    compile_shared_wasm "$SIDE_OUT" "$SIDE_SO"

    # Vacuity guard for this half: the side module must actually import the
    # cri_* P/Invoke surface.
    grep -qE '^func env\.cri' <<<"$(node gates/_wasm_symbols.js imports "$SIDE_SO")" || {
        echo "FAIL: $SIDE_SO imports no cri_* symbol — the native-half closure would be vacuous" >&2
        exit 1
    }

    unsat_side=$(node gates/_wasm_symbols.js unsatisfied "$SIDE_SO" "$TDIR/cri/godot.wasm" "$TDIR/cri/godot.js")
    noncri=$(printf '%s\n' "$unsat_side" | grep -vE '^(GOT\.(mem|func)\.)?cri' | grep . || true)
    if [ -n "$noncri" ]; then
        echo "FAIL: $SIDE_SO imports non-CRI symbols the CRI Web template cannot provide:" >&2
        printf '%s\n' "$noncri" | LC_ALL=C sed 's/^/  /' >&2
        echo "      Each would dlopen and dlsym cleanly in the exported game, then die on" >&2
        echo "      first CALL naming a wasm function index. Define them for the wasm target." >&2
        exit 1
    fi
    echo "native half OK: every non-CRI side-module import resolves against the CRI template"
    if [ -z "$unsat_side" ]; then
        # A template whose main module carries (and exports) the CRI archives
        # would land here — keep the full green path.
        echo "native half fully closed: the template supplies the cri_* surface too"
    else
        gate_partial "the CRI template does not supply the drop-in's native cri_* surface:
    $(printf '%s\n' "$unsat_side" | grep -c .) P/Invoke imports (criErr_*/criAtom_*/criAtomEx_*) stay unsatisfied. Both CRI
    archives are non-PIC, so they cannot ride the side module; the template's MAIN
    module must carry them (whole-archive) and export them, which is what the
    fork's dlink_main_extra_libs option does — this template predates it. Rebuild
    with FORCE=1 CRI=1 gates/setup-godot-fork-web.sh on a fork that has the
    option; the JS half above and the non-CRI closure DID run and hold."
    fi

    if [ ! -f "$TEMPLATE_CRI_DEBUG" ] || [ ! -f "$TEMPLATE_STOCK_DEBUG" ]; then
        gate_partial "the Debug CRI/stock Web template pair is absent; run both flavors of
    gates/setup-godot-fork-web.sh so the stock/CRI × Release/Debug closure matrix can run"
    else
        mkdir -p "$TDIR/cri-debug" "$TDIR/stock-debug"
        unzip -q "$TEMPLATE_CRI_DEBUG" godot.wasm godot.js -d "$TDIR/cri-debug"
        unzip -q "$TEMPLATE_STOCK_DEBUG" godot.wasm godot.js -d "$TDIR/stock-debug"
        debug_jslib_gap=$(node gates/_wasm_symbols.js unsatisfied "$OUT/$PROJECT.wasm" \
            "$TDIR/cri-debug/godot.wasm" "$TDIR/cri-debug/godot.js" \
            | grep -E '^(GOT\.(mem|func)\.)?(criFsIoJs_|WAJS_|WAASRJS_)' || true)
        [ -z "$debug_jslib_gap" ] || {
            echo "FAIL: the CRI Debug template leaves CRI jslib imports unsatisfied:" >&2
            printf '%s\n' "$debug_jslib_gap" | LC_ALL=C sed 's/^/  /' >&2
            exit 1
        }
        grep -qE '^(criFsIoJs_|WAJS_|WAASRJS_)' \
            <<<"$(node gates/_wasm_symbols.js unsatisfied "$OUT/$PROJECT.wasm" \
                "$TDIR/stock-debug/godot.wasm" "$TDIR/stock-debug/godot.js")" || {
            echo "FAIL: the stock Debug template appears to satisfy the CRI jslib demand" >&2
            exit 1
        }
        debug_unsat_side=$(node gates/_wasm_symbols.js unsatisfied "$SIDE_SO" \
            "$TDIR/cri-debug/godot.wasm" "$TDIR/cri-debug/godot.js")
        [ -z "$debug_unsat_side" ] || {
            echo "FAIL: the CRI Debug template does not close the side-module imports:" >&2
            printf '%s\n' "$debug_unsat_side" | LC_ALL=C sed 's/^/  /' >&2
            exit 1
        }
        echo "Debug closure OK: CRI supplies JS/native imports; stock remains the negative control"
    fi
fi

gate_cache_commit
