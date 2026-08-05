#!/usr/bin/env bash
# Build the fork's Web export template: mono + dlink + nothreads + wasm exceptions.
#
# Upstream ships no C#-capable Web template and cannot: Godot's Web platform
# never declared the mono module supported, so it has never been compiled for
# wasm32. The fork does declare it, and this script bakes the result.
#
# Three flags are not negotiable, and each is asserted below:
#
#   dlink_enabled=yes    The game ships as an Emscripten SIDE MODULE that GDMono
#                        dlopen()s. Without dlink the engine has no dlopen at all
#                        -- Emscripten's is a stub that returns NULL.
#   threads=no           The drop-in is built without -pthread, and Emscripten
#                        refuses to load a non-pthread side module into a pthread
#                        main module.
#   disable_exceptions=no + -fwasm-exceptions
#                        A .NET-semantics runtime cannot drop C++ exceptions --
#                        that is how a managed `throw` is implemented. Under
#                        Emscripten dynamic linking the exception TAG is defined
#                        and exported by the MAIN module and imported by every
#                        side module, so the engine must carry it or the drop-in
#                        cannot even be instantiated. Godot's own default is
#                        -fno-exceptions, which does not define the tag.
#
# CRI=1 builds the CRI-enabled VARIANT instead, published beside the stock
# template as web_template_cri.zip. Emscripten JS libraries can only live in
# the MAIN module -- the export template -- so a CRI game needs a template
# whose main-runtime link carries the package's three .jslib files (+ the
# $dynCall helper, per the package's own .targets recipe). It is a variant,
# not the default, because the stock template serves every existing export
# gate and a CRI-less game gains nothing from carrying the extra JS. The
# linked-in functions live in the glue's wasmImports object (INCLUDE_FULL_
# LIBRARY under MAIN_MODULE=1), NOT in godot.wasm's import section -- nothing
# in the main module's own code calls them -- which is why the assertions below
# read godot.js, and why gates/_wasm_symbols.js's `unsatisfied` takes the glue
# as its third argument. The import-closure proof against this template lives
# in the CRI wasm smoke gate.
#
# The variant carries the CRI NATIVE surface too: both of the package's
# static archives are compiled without -fPIC, so they cannot ride the game
# side module (wasm-ld rejects the relocations) and must instead be
# whole-archive-linked into the template's main runtime -- which plain scons
# `linkflags` cannot express, because those reach the engine's own
# side-module link too, where extracting the non-PIC members is fatal. The
# fork's `dlink_main_extra_libs` option is the main-link-only input channel
# this needs: the archives ride only the main-runtime link, whole-archived,
# and EXPORT_ALL surfaces every cri_* symbol for the game side module to
# import (asserted below, one probe symbol per archive; the exhaustive
# import-closure diff lives in the CRI wasm smoke gate).
#
# The emcc version is stamped beside each template: a side module and the main
# module must agree on the EH flavour, WASM_BIGINT and the dylink ABI, and
# nothing else pins that.
#
# The ENGINE the template was built from is stamped beside it too
# (<template>.provenance, godot_fork_engine_provenance in gates/_godot_fork.sh),
# and that stamp is the one the export gates read. Nothing else can: a preset's
# custom_template is validated by Godot with FileAccess::exists alone, so a
# template a patch release behind exports, runs, and matches every expectation
# while shipping the wrong engine. After the 4.7.1 re-pin all three baked zips
# were still 4.7 and only `strings` on the artifacts said so. Why a
# missing stamp is a refusal rather than a warning is written out at
# godot_fork_template_check.
#
# The stamp is written by the BUILD, beside the built zip in the fork's bin/ --
# the same placement and the same rule as an engine binary's .engine-hash stamp
# (gates/setup-godot-fork.sh) -- and the publish step COPIES it. It used to be
# written at the tail of this script unconditionally, which made it assert a
# toolchain version on runs where every step was skipped, i.e. about a product
# this emcc had not necessarily touched. Consequently the skip below asks for the
# stamp too: an unstamped zip is of unknown provenance, which is stale rather
# than current (a missing stamp is never a pass), and a zip stamped with a
# DIFFERENT emcc is relinked, because the drop-in that has to load into it is
# built by whatever emcc is on PATH at gate time -- this one.
#
# It is also the first thing to move when this script dies inside the COMPILER
# rather than in the engine's sources. An emsdk whose clang miscompiles the
# engine is not a rare event and it does not announce itself as a version
# problem: emsdk 6.0.3 crashed clang outright (SIGSEGV in WebAssembly
# instruction selection, on scene/resources/style_box_flat.cpp), and the trigger
# was the LOOP VECTORIZER — so it reproduces at every -O level Godot's web build
# uses and on every host, since none of those flags are the host's. 6.0.1
# compiles the same tree clean. Before working around a crash like that in the
# engine's sources or in this script's flags, try another emsdk: the flags here
# are the engine's, and a crash on them is the toolchain's bug, not ours.
#
# Idempotent: the scons build and the copy are skipped when their product is
# current *and of the requested flavor* *and stamped with this emcc* *and
# stamped with these engine sources* -- both flavors share one bin/ zip, so the
# skip probes the built zip's flavor rather than its existence, and a flavor
# switch costs a relink + zip (scons reuses every compiled object; only the link
# action's signature moves). FORCE=1 rebuilds.
set -euo pipefail
# _common.sh is sourced for the CRI asset resolvers (cri_wasm_native_files);
# its only source-time side effect is a cd to the repo root, which this
# script's absolute paths are indifferent to. Resolve the script dir first so
# that cd cannot orphan the second source.
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
source "$SCRIPT_DIR/_common.sh"
source "$SCRIPT_DIR/_godot_fork.sh"

FORCE="${FORCE:-0}"
CRI="${CRI:-0}"
JOBS="$(sysctl -n hw.ncpu 2>/dev/null || nproc 2>/dev/null || echo 8)"

# Resolved by RUNNING a candidate, never by `command -v`: on Windows that test
# answers "no" while SCons is installed and usable (pip puts console scripts in a
# per-user Scripts directory that is not on PATH, and SCons is importable as a
# module regardless). Shared with the other two fork setup aids — the resolver
# lives in gates/_godot_fork.sh so all three cannot drift.
PY="$(resolve_python)"
SCONS="$(godot_fork_scons "$PY")"
# --no-bundled: this link resolves system-library variants an export never asks
# for (pic/libGL-webgl2-getprocaddr.a, from -sMAIN_MODULE=1 -sMAX_WEBGL_VERSION=2),
# and the bundled SDK's cache is frozen to the export's set — it would die on a
# missing cache file. The version still has to match the bundle's, because the
# drop-in that loads into this template is compiled by THAT SDK; asserted below.
dn2cpp_emsdk_resolve --no-bundled
command -v emcc >/dev/null 2>&1 || {
    echo "error: emcc not found. gates/setup-emsdk.sh unpacks the pinned SDK; the one" >&2
    echo "       inside the toolchain bundle is deliberately not used here (frozen cache)." >&2
    exit 1; }
command -v node >/dev/null 2>&1 || { echo "error: node not found (emcc itself needs it; check the emsdk install)" >&2; exit 1; }

# Resolve the fork worktree from the path the artifact root records, the same way
# the editor-export gates do (godot_fork_resolve, gates/_godot_fork.sh).
godot_fork_resolve || exit 1

# Godot's own floor is Emscripten 4.0.0 (platform/web/detect.py).
EMCC_VERSION="$(first_line "$(emcc --version)")"

# A frozen SDK cannot bake this template at all, and the failure it produces
# ("FROZEN_CACHE is set, but cache file is missing") names a cache file rather
# than the SDK that has to change.
if [ -n "${EM_CONFIG:-}" ] && grep -q '^FROZEN_CACHE' "$EM_CONFIG"; then
    echo "error: $EM_CONFIG freezes its cache, so this build cannot fill the variants an" >&2
    echo "       engine template link resolves. Point DN2CPP_EMSDK at an unpacked SDK" >&2
    echo "       (gates/setup-emsdk.sh) instead of a packaged bundle's." >&2
    exit 1
fi
# The drop-in a Web export ships is compiled by the SDK INSIDE the toolchain
# bundle, this template by the SDK above. They are two halves of one wasm
# program: a version split links clean on both sides and dies in the browser.
BUNDLED_EMSDK_JSON="artifacts/toolchain/dn2cpp-toolchain-0.1.0-${DN2CPP_OS}-$(uname -m)/emsdk/emsdk.json"
if [ -f "$BUNDLED_EMSDK_JSON" ]; then
    bundled_emcc="$(LC_ALL=C sed -n 's/.*"emcc_version": "\(.*\)".*/\1/p' "$BUNDLED_EMSDK_JSON")"
    bundled_emcc="$(first_line "$bundled_emcc")"
    if [ -n "$bundled_emcc" ] && [ "$bundled_emcc" != "$EMCC_VERSION" ]; then
        echo "error: this SDK is not the one the toolchain bundle carries." >&2
        echo "       template: $EMCC_VERSION" >&2
        echo "       bundle:   $bundled_emcc" >&2
        echo "       Re-package the bundle from this SDK (dist/package-toolchain.sh --emsdk DIR)" >&2
        echo "       or unpack the pinned one (gates/setup-emsdk.sh)." >&2
        exit 1
    fi
fi
# The pinned base and the fingerprint of the engine sources the fork would
# compile -- the second thing the built zip is stamped with (see header). Read
# from the checked-in pin file rather than hardcoded, so this aid and
# gates/setup-godot-fork.sh cannot name two different bases.
BASE_COMMIT="$(godot_fork_base_commit)"
ENGINE_PROVENANCE="$(godot_fork_engine_provenance)"
echo "fork:  $FORK"
echo "emcc:  $EMCC_VERSION"
echo "engine: $ENGINE_PROVENANCE"

BUILT="$FORK/bin/godot.web.template_release.wasm32.nothreads.dlink.mono.zip"
# The emcc that built the zip now in bin/, recorded beside it (see header). The
# fork's bin/ is covered by Godot's .gitignore, like the .engine-hash stamps.
BUILT_STAMP="$BUILT.emcc"
# The engine sources that zip was built from, same placement and same rule. Two
# stamps rather than one line holding both, because they answer to different
# owners: the emcc stamp moves when the toolchain on PATH changes and the engine
# stamp when the fork's C++ does, and a single string would force a relink on
# either -- while the published copies are read by different consumers (the
# gates' cache key takes web_emcc*.txt verbatim).
BUILT_ENGINE_STAMP="$BUILT.provenance"
if [ "$CRI" = 1 ]; then
    OUT="$FORK_ROOT/web_template_cri.zip"
    STAMP="$FORK_ROOT/web_emcc_cri.txt"
    WANT=cri
else
    OUT="$FORK_ROOT/web_template.zip"
    STAMP="$FORK_ROOT/web_emcc.txt"
    WANT=stock
fi
# Beside the published template, which is what a gate reads
# (godot_fork_template_check).
ENGINE_STAMP="$OUT.provenance"

# The custom `linkflags` reach BOTH links -- the MAIN_MODULE=1 runtime and the
# SIDE_MODULE=2 engine (SConstruct appends them to the base env before the web
# SCsub clones it). That is wanted for -fwasm-exceptions and harmless for the
# CRI flags: on a wasm-output side link emcc ignores JS libraries and warns the
# library-funcs setting away (verified against emcc 6.0.1).
LINKFLAGS="-fwasm-exceptions"
# The comma-separated archive list handed to the fork's dlink_main_extra_libs
# scons option -- the main-link-only channel (see header). Empty on a stock
# build, which is the option's default.
MAIN_EXTRA_LIBS=""
# One representative JS function per CRI .jslib, in resolver order (crifs_io,
# crinc_webaudio, crinc_webaudio2) -- the setup-time sanity probes that the
# link actually took; the smoke gate's import-closure diff covers the full
# surface. Names verified against the pinned 2.30.196 drop; re-check on a
# DN2CPP_CRI_VERSION bump.
CRI_PROBE_SYMS=(criFsIoJs_Load WAJS_Initialize WAASRJS_SetMainFunc)
# One exported wasm function per CRI static archive, in resolver order
# (cri_atom.a facade, libcri_ware_le.a engine) -- the setup-time probes that
# both archives made the MAIN-runtime link and EXPORT_ALL surfaced them for
# the game side module to import. Same version caveat as above.
CRI_EXPORT_PROBE_SYMS=(criNativeAllocator_GetRoot criAtom_GetVersionString)
if [ "$CRI" = 1 ]; then
    if ! cri_files="$(cri_wasm_native_files)"; then
        echo "error: CRI ADX LE browser-wasm assets not found (CriWare.CriAtomLE $CRI_ATOM_VERSION)." >&2
        echo "       Install the NuGet package, or point DN2CPP_CRI_NUGET_ROOT at an unpacked root" >&2
        echo "       (see the cri_* resolvers in gates/_common.sh)." >&2
        exit 1
    fi
    command -v emar >/dev/null 2>&1 || { echo "error: emar not found (it ships with emcc; check the emsdk install)" >&2; exit 1; }
    # The archives are staged as filtered copies rather than passed in place:
    # a whole-archive link opens EVERY member, and libcri_ware_le.a carries two
    # a selective link never would — each fatal only when opened. Dropping them
    # reproduces selective-link behavior exactly:
    #   - criatom_streaming_cache_stub.c.o duplicates the real member's nine
    #     criAtomStreamingCache* definitions (the real member precedes it in
    #     the archive index, so a selective link reads only that) — a hard
    #     duplicate-symbol error when both are opened.
    #   - cri_imask_common.c.o defines three criImask_* functions NOTHING in
    #     either archive references, while itself reading a data symbol
    #     (criimask_ims_imasklevel) defined nowhere — and under the main
    #     module's PIC link, a non-PIC object's direct LEB relocation against
    #     an undefined data symbol is fatal (an undefined FUNCTION becomes a
    #     dynamic import; an undefined data address cannot).
    # The remaining unresolved surface was audited: libc/math/pthread names
    # (the main link's own libraries satisfy them) plus the mic jslib's JSMIC_*
    # functions, which this LE drop does not ship — function imports, resolved
    # to report-on-call stubs, unreachable without mic use.
    # The staging path is STABLE on purpose: it rides the link action's
    # signature, so a fresh temp dir per run would force a relink every run.
    #
    # Every path that leaves this shell for SCONS is converted first
    # (godot_fork_native_path). scons is a native Python program, so nothing
    # converts an MSYS /c/... on its way in the way MSYS converts a native
    # program's argv — the value is read as a relative path and prefixed with the
    # drive, and the failure names the mangled result ("C:\c\Users\...not found")
    # without hinting that the input was a path in the wrong dialect. The shell's
    # own uses (cp, emar, the heredoc below) keep the MSYS form, which is what
    # those tools want. A no-op off Windows, so every other host is unchanged.
    MAIN_LIBS_DIR="$FORK_ROOT/cri_main_libs"
    mkdir -p "$MAIN_LIBS_DIR"
    while IFS= read -r f; do
        case "$f" in
            *.jslib) LINKFLAGS="$LINKFLAGS --js-library=$(godot_fork_native_path "$f")" ;;
            *.a)
                staged="$MAIN_LIBS_DIR/$(basename "$f")"
                cp -f "$f" "$staged"
                if [ "$(basename "$f")" = libcri_ware_le.a ]; then
                    emar d "$staged" criatom_streaming_cache_stub.c.o cri_imask_common.c.o
                fi
                MAIN_EXTRA_LIBS="${MAIN_EXTRA_LIBS:+$MAIN_EXTRA_LIBS,}$(godot_fork_native_path "$staged")"
                ;;
        esac
    done <<<"$cri_files"
    # This one setting has to survive THREE expanders to reach emcc intact, and
    # each eats a bare `$dynCall` for a different reason:
    #   - scons, which substitutes its own construction variables into linkflags.
    #     `$$` is its escape, and it is the one that was missing: without it the
    #     setting reached emcc as the two-character string '' on every host, i.e.
    #     the package's recipe has never actually been applied. On POSIX that is
    #     invisible — the /bin/sh spawn below then strips those quotes, emcc sees
    #     a truly empty value and treats it as an empty list — which is why the
    #     bug surfaced only on Windows, where nothing strips them: emcc splits ''
    #     into ONE EMPTY symbol name, and the glue gets a wasmImports entry with
    #     an empty key. That is a JS syntax error, and it is reported by the
    #     minifier as `Unexpected token` at a line number in generated code.
    #   - /bin/sh, which scons hands the whitespace-split tokens to unquoted on
    #     POSIX. The single quotes are what stop it expanding $dynCall to empty.
    #   - emcc itself, which strips a surrounding pair of quotes from a -s value,
    #     so the same quotes are harmless on Windows, where no shell removed them.
    LINKFLAGS="$LINKFLAGS -sDEFAULT_LIBRARY_FUNCS_TO_INCLUDE='\$\$dynCall'"
    # Signatures for the .jslib functions the CRI native code takes the
    # ADDRESS of (the crincbank interface stores them in a function-pointer
    # struct). The package's .targets recipe targets a non-dlink build, where
    # wasm-ld resolves such an address itself off the import's wasm type; under
    # this MAIN_MODULE link the same reference becomes a GOT.func import that
    # the page's loader fills at boot by placing the glue's JS function into
    # the indirect table — addFunction() — and THAT needs an explicit __sig,
    # which CRI's .jslib files do not carry. Without it every boot of the
    # template dies inside convertJsFunctionToWasm (a TypeError on undefined,
    # naming no symbol) before main() ever runs, game or no game. The merge is
    # additive — a later library may attach <name>__sig to a function an
    # earlier one defined — so the annotations live in this generated fourth
    # library rather than in a patch of the proprietary files. Names and
    # signatures follow the pinned 2.30.196 drop's JS definitions (all-int
    # ADX LE bank API; p = pointer); the flavor assertion below re-derives the
    # address-taken set from the built wasm, so a future drop that adds one
    # fails the bake loudly instead of shipping a template that cannot boot.
    SIG_JSLIB="$MAIN_LIBS_DIR/cri_jslib_sigs.jslib"
    cat > "$SIG_JSLIB" <<'EOF'
// GENERATED by gates/setup-godot-fork-web.sh -- do not commit.
// __sig annotations for the address-taken CRI .jslib functions (see the
// generating script for why a dlink main module needs them).
addToLibrary({
  WAJS_CreateBank__sig: "ii",
  WAJS_DestroyBank__sig: "vi",
  WAJS_PreloadData__sig: "iipiiiiiipp",
});
EOF
    LINKFLAGS="$LINKFLAGS --js-library=$(godot_fork_native_path "$SIG_JSLIB")"
fi

echo "== 1/3 scons: the Web template ($WANT) =="
built_emcc="$(cat "$BUILT_STAMP" 2>/dev/null || echo '<no stamp>')"
built_engine="$(head -1 "$BUILT_ENGINE_STAMP" 2>/dev/null || echo '<no stamp>')"
[ -n "$built_engine" ] || built_engine='<no stamp>'
if [ "$(godot_fork_web_template_flavor "$BUILT")" = "$WANT" ] && [ "$built_emcc" = "$EMCC_VERSION" ] \
    && [ "$built_engine" = "$ENGINE_PROVENANCE" ] && [ "$FORCE" != 1 ]; then
    echo "skip: already built ($WANT, by this emcc, from these engine sources): $BUILT"
else
    if [ -f "$BUILT" ] && [ "$(godot_fork_web_template_flavor "$BUILT")" = "$WANT" ] \
        && [ "$built_emcc" != "$EMCC_VERSION" ]; then
        echo "-- the $WANT zip in bin/ was not built by this emcc"
        echo "   stamped $built_emcc != $EMCC_VERSION"
    fi
    if [ -f "$BUILT" ] && [ "$(godot_fork_web_template_flavor "$BUILT")" = "$WANT" ] \
        && [ "$built_engine" != "$ENGINE_PROVENANCE" ]; then
        echo "-- the $WANT zip in bin/ was not built from these engine sources"
        echo "   stamped $built_engine"
        echo "   wanted  $ENGINE_PROVENANCE"
    fi
    # The stamps are removed first and written last, so an interrupted or failed
    # build cannot leave the previous zip standing behind a stamp that calls it
    # this run's product (install_binary in setup-godot-fork.sh, same rule).
    rm -f "$BUILT_STAMP" "$BUILT_ENGINE_STAMP"
    # shellcheck disable=SC2086
    (cd "$FORK" && $SCONS platform=web target=template_release \
        module_mono_enabled=yes dlink_enabled=yes threads=no \
        disable_exceptions=no \
        cxxflags=-fwasm-exceptions "linkflags=$LINKFLAGS" \
        "dlink_main_extra_libs=$MAIN_EXTRA_LIBS" "-j$JOBS")
    [ -f "$BUILT" ] || { echo "error: scons produced no $BUILT" >&2; exit 1; }
    printf '%s\n' "$EMCC_VERSION" > "$BUILT_STAMP"
    printf '%s\n' "$ENGINE_PROVENANCE" > "$BUILT_ENGINE_STAMP"
fi

echo "== 2/3 asserting the template is what the drop-in needs ($WANT) =="
# dlink + the wasm exception tag (godot_fork_web_template_assert,
# gates/_godot_fork.sh) — the same two properties the release packaging
# re-checks on the published copy.
godot_fork_web_template_assert "$BUILT"

# Flavor: what sits in bin/ must be what this run is about to publish -- the
# shared bin/ zip is the one place a stock run could otherwise pick up a
# CRI-linked leftover (or vice versa) and ship it under the wrong name.
flavor_now="$(godot_fork_web_template_flavor "$BUILT")"
if [ "$flavor_now" != "$WANT" ]; then
    echo "error: $BUILT is a $flavor_now-flavor build, expected $WANT -- rerun with FORCE=1" >&2
    exit 1
fi
if [ "$CRI" = 1 ]; then
    tmp="$(mktemp -d)"
    trap 'rm -rf "$tmp"' EXIT
    unzip -q "$BUILT" -d "$tmp"
    # The CRI JS surface lives in the glue (see header), one probe per .jslib.
    for sym in "${CRI_PROBE_SYMS[@]}"; do
        grep -aq "$sym" "$tmp/godot.js" || {
            echo "error: $BUILT's JS glue lacks $sym -- a CRI .jslib did not make the main-runtime link" >&2
            exit 1
        }
    done
    # The .jslib pump drives transpiled callbacks through the glue's dynCall
    # helper; its definition must have survived into the glue (this is also the
    # tripwire for the $dynCall shell-quoting above collapsing to an empty
    # setting on some future scons/emcc pairing).
    grep -aqE 'dynCall\s*=\s*\(' "$tmp/godot.js" || {
        echo "error: $BUILT's JS glue has no dynCall helper" >&2
        exit 1
    }
    echo "ok: CRI JS libraries in the glue (${CRI_PROBE_SYMS[*]}), dynCall present"
    # The native surface: the game side module cannot carry the non-PIC
    # archives, so its whole cri_* P/Invoke demand arrives as dynamic imports
    # that only the MAIN module's exports can satisfy. Assert the export
    # actually happened — one probe per archive — with the section parser, not
    # a string grep: this is the yes/no the exported game's dlopen never gives.
    # Through a file, not a pipe: grep -q's early exit would hand node an
    # EPIPE (the same pipefail trap as the flavor probes).
    node "$SCRIPT_DIR/_wasm_symbols.js" exports "$tmp/godot.wasm" > "$tmp/main_exports.txt"
    for sym in "${CRI_EXPORT_PROBE_SYMS[@]}"; do
        grep -qx "func $sym" "$tmp/main_exports.txt" || {
            echo "error: $BUILT's main module does not export $sym -- a CRI archive did not make the main-runtime link" >&2
            exit 1
        }
    done
    echo "ok: main module exports the CRI native surface (${CRI_EXPORT_PROBE_SYMS[*]})"
    # The address-taken half of the JS surface: every CRI .jslib function the
    # main module GOT-imports (its PIC link makes one GOT.func per
    # address-taken undefined function) must carry a .sig in the glue, or the
    # loader's addFunction() dies at boot before main() — the failure the
    # generated sig library above exists to prevent. Re-derived from the built
    # wasm rather than hard-coded, so an SDK drop that adds an address-taken
    # function fails HERE, naming it, instead of baking a template that cannot
    # boot. (grep -a: the glue is one multi-MB minified line.)
    node "$SCRIPT_DIR/_wasm_symbols.js" imports "$tmp/godot.wasm" > "$tmp/main_imports.txt"
    missing=""
    while IFS= read -r sym; do
        grep -aq "${sym}\.sig=" "$tmp/godot.js" || missing="$missing $sym"
    done < <(LC_ALL=C sed -n 's/^global GOT\.func\.\(criFsIoJs_[A-Za-z0-9_]*\|WAJS_[A-Za-z0-9_]*\|WAASRJS_[A-Za-z0-9_]*\)$/\1/p' "$tmp/main_imports.txt" | sort -u)
    if [ -n "$missing" ]; then
        echo "error: address-taken CRI .jslib functions lack a __sig annotation in the glue:$missing" >&2
        echo "       extend the generated sig library in this script (signature from the" >&2
        echo "       function's C prototype / JS definition)" >&2
        exit 1
    fi
    echo "ok: every GOT-imported CRI .jslib function carries a signature in the glue"
fi

echo "== 3/3 publishing =="
# Stamps removed first, the 57 MB copy next, stamps written last — the rule
# install_binary (setup-godot-fork.sh) applies to a binary, applied here to the
# published pair. An interrupted publish must leave an UNSTAMPED template rather
# than a half-written one under the stamps of the template it was replacing:
# unstamped is refused, mis-stamped is used.
rm -f "$STAMP" "$ENGINE_STAMP"
cp -f "$BUILT" "$OUT"
# COPIED, not re-derived: the published stamps must name the emcc and the engine
# tree that built the zip beside them, which on a skipped run are not necessarily
# this run's (header). Step 1 leaves both stamps on every path that reaches here.
cp -f "$BUILT_STAMP" "$STAMP"
cp -f "$BUILT_ENGINE_STAMP" "$ENGINE_STAMP"

echo
echo "web template:  $OUT"
echo "emcc stamp:    $STAMP"
echo "engine stamp:  $ENGINE_STAMP"
echo
echo "Point a Web export preset's custom_template/release at the template, with"
echo "  variant/extensions_support = true   (dlink)"
echo "  variant/thread_support     = false  (the drop-in is single-threaded)"
if [ "$CRI" = 1 ]; then
    echo
    echo "CRI variant: this template supplies the package's .jslib surface from"
    echo "the main module's glue AND the cri_* native surface from its wasm"
    echo "exports (the archives are non-PIC, so the main-runtime link is the"
    echo "only one that can carry them — see header). A game side module links"
    echo "no CRI archive and imports the whole surface dynamically."
fi
