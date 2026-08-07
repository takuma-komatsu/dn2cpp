#!/usr/bin/env bash
# Godot editor-export E2E gate, Web lane: the *forked editor* exports a C# game
# with `dotnet/export_backend = dn2cpp` to WebAssembly, and the exported game
# runs in a real browser.
#
# Godot 4 does not support C# on the Web at all -- a mono-enabled editor refuses
# every Web export outright. This gate is the proof that dn2cpp closes that hole,
# and it is an end-to-end one: one `--export-release`, then a browser.
#
# The mechanism, and what each assertion below is really pinning:
#
#   * The drop-in is an Emscripten SIDE MODULE staged as `<Assembly>.so`. The C#
#     export plugin hands it to the platform exporter with AddSharedObject(), the
#     Web exporter copies it FLAT next to index.html (it takes .get_file(), so the
#     target sub-directory every other platform uses is ignored here) and lists it
#     in the generated HTML's config as a `gdextensionLibs` entry.
#   * That list becomes Emscripten's Module.dynamicLibraries, which the loader
#     preloads before main() runs.
#   * GDMono finds no hostfxr and no coreclr, falls through to
#     try_load_native_aot_library(), and asks OS_Web::open_dynamic_library() for
#     `<api_assemblies_dir>/<Assembly>.so`. That function STRIPS THE DIRECTORY and
#     dlopen()s the bare file name -- which is already in the loaded-library
#     registry. The load never touches a file system, which is why the assemblies
#     directory the other platforms need does not exist here and does not have to.
#
# So the staged file name is the entire contract, and section 6 asserts it three
# ways: the file is beside index.html, the HTML names it, and it exports the
# entry symbol.
#
# Requires the artifacts of gates/setup-godot-fork.sh AND gates/setup-godot-fork-web.sh
# (the Web template: upstream ships no C#-capable one), plus emcc, node, a python
# new enough for emcc to launch, and a headless Chrome. Skips (exit 77) when any
# is absent.
#
# Registered in run-all-gates.sh's SERIAL Godot phase, in the chain that writes
# $FORK_GODOTSHARP/Dn2Cpp: it launches the engine, and every launch runs
# under a watchdog because a broken Godot run hangs rather than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_fork.sh"

OUT=gates/out-godot-editor-export-web
SAMPLE=samples/godot-dotnet/EditorExportSample
PROJECT_NAME=EditorExportSample

# V8's hard per-function ceiling, a compiled-in constant (Chrome enforces it too).
V8_MAX_FUNCTION_SIZE=7654321

godot_fork_preflight
if [ ! -f "$FORK_ROOT/web_template.zip" ]; then
    gate_skip "no Web template at $FORK_ROOT/web_template.zip — run gates/setup-godot-fork-web.sh"
fi
# Bundled SDK: the subject is the export shape its frozen cache is baked to.
dn2cpp_emsdk_resolve
for tool in emcc em++ emcmake node; do
    command -v "$tool" >/dev/null 2>&1 || gate_skip "$tool not on PATH (needed to build and run a Web export)"
done
# Python is two questions, and the loop above answers neither. The first is which
# interpreter EMCC starts, since every Emscripten front-end is a launcher over one
# and the export refuses a stale one — $EMSDK_PYTHON stands in for the export's,
# because the SDK dn2cpp_emsdk_resolve just chose is the one stage_editor_toolchain
# installs below and the export section asserts was compiled through.
godot_fork_emcc_python_check "$PATH" "${EMSDK_PYTHON:-}"
# The second is the interpreter THIS SCRIPT runs, for the wasm export reader and
# the http.server below — a different interpreter, and no floor. resolve_python
# probes by RUNNING one, the only thing that tells a real interpreter from the
# app-execution-alias stub a stock Windows install answers `python3` with.
py="$(resolve_python)" || gate_skip "no working Python 3 interpreter (needed to read wasm exports and serve the export)"

# Read a wasm module's export names. A release side module has no name section,
# so grepping the file for a symbol would also match an import; parse the export
# section instead.
wasm_exports() {
    # shellcheck disable=SC2086  # $py may be `py -3`: two words, deliberately split
    $py - "$1" <<'PY'
import sys

def leb(b, i):
    r = s = 0
    while True:
        x = b[i]; i += 1
        r |= (x & 0x7f) << s
        if not (x & 0x80):
            return r, i
        s += 7

b = open(sys.argv[1], "rb").read()
if b[:4] != b"\0asm":
    sys.exit("not a wasm module: " + sys.argv[1])
i = 8
while i < len(b):
    sid = b[i]; i += 1
    size, i = leb(b, i)
    end = i + size
    if sid == 7:  # export section
        n, j = leb(b, i)
        for _ in range(n):
            nl, j = leb(b, j)
            print(b[j:j + nl].decode("utf8", "replace"))
            j += nl
            j += 1                      # kind
            _, j = leb(b, j)            # index
    i = end
PY
}

echo "== 1/8 Fork pin + interop-ABI tripwire =="
# This lane is the first to change engine C++ at all, so the ABI surfaces the
# mono-module handshake hard-codes assumptions about matter MORE here, not less:
# they must still be byte-identical to the pinned base, or the fork has stopped
# being a drop-in host and every other claim in this gate is void.
godot_fork_pin_abi_check

# The template is the second engine in this gate, and the only one nothing else
# checks: the preset points `custom_template/release` at it and Godot validates
# that path with FileAccess::exists alone (platform/web/export/export_plugin.cpp),
# so a zip baked before a re-pin exports, runs and matches every expectation in
# this file while shipping the older engine, which a re-pin has already produced
# once. Live, before gate_cache_check, for the reason the
# preflight is: `tmpl=` in the key fingerprints the stale zip itself, so a warm
# key replays a green *because* nothing changed.
godot_fork_template_check "$FORK_ROOT/web_template.zip" "Web export template" \
    "FORCE=1 gates/setup-godot-fork-web.sh"

# gate_cache_check is (OUT, CONTEXT, MATERIAL...). The OUT argument was missing
# here, which shifted every other one: the context string landed in OUT (so the
# `cd "$OUT"` that hashes the transpile output surface silently failed into its
# `|| true`), and $SELFHOST_BIN landed in CONTEXT, where only its PATH is read and
# never its content. The transpiler could be rebuilt from end to end and this
# gate's key would not move -- which is precisely the stale-binary false green
# that the maxfunc assert in section 6 exists to catch, wired into the cache.
#
# The shared terms (pins, editor, the load-bearing GodotTools content hash) are
# godot_fork_ctx -- see gates/_godot_fork.sh for why `tools=` must be a CONTENT
# hash and nothing else in this key covers the exporter (this gate's
# --trim-reflection rebuild produced a DLL of byte-identical SIZE, 200192 -- a
# size test would have looked right through it).
# OUT holds the export, never a transpile output surface, so its surface term in
# the key is the `no-generated` marker — but only once OUT EXISTS. Create it here
# rather than at the staging step below: an absent OUT is unreadable, not empty,
# and gate_cache_check answers that with a warning and no key, which
# would leave this gate uncached on every fresh clone.
#
# `emsdk=` is the STAGED bundle's SDK stamp (version, upstream archive sha256,
# staging recipe). The drop-in is compiled through that SDK and `emcc=` names the
# one that baked the TEMPLATE, so nothing else here moves when a re-packaged
# bundle brings a different SDK to the half of the link this gate builds.
mkdir -p "$OUT"
if gate_cache_check "$OUT" \
    "godot-editor-export-web|$(godot_fork_ctx)|tmpl=$(file_sig "$FORK_ROOT/web_template.zip")|emcc=$(file_text "$FORK_ROOT/web_emcc.txt")|emsdk=$(file_text "$FORK_GODOTSHARP/Dn2Cpp/emsdk/.emsdk-stamp")" \
    "$SELFHOST_BIN" \
    dist/package-toolchain.sh \
    "$SAMPLE" \
    "$ABI_EXPECTED"; then
    { gate_cache_hit_msg; exit 0; }
fi

echo "== 2/8 Installing the working tree's toolchain into the fork editor =="
# Packaging runs every run (keeps a dn2cpp-side regression visible here); the
# install is content-idempotent and atomic — see stage_editor_toolchain in
# gates/_common.sh.
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log"

echo "== 3/8 Staging the sample project =="
# Staged fresh every run. The exporter deliberately persists its CMake build dir
# under .godot/mono/dn2cpp/build to compile the runtime once, and CMake never
# resets an already-cached variable to its default — so a build dir a PRIOR
# exporter configured with -DDN2CPP_USE_GC=OFF would keep the GC off no matter
# what the current exporter passes (or stops passing). Reusing that stale dir is
# exactly how this gate could report GC-on green while shipping a GC-off drop-in;
# a clean stage is what makes "the exporter builds GC on" an honest assertion.
PROJ="$OUT/project"
rm -rf "$PROJ"
mkdir -p "$PROJ"
cp -R "$SAMPLE/." "$PROJ/"

cat > "$PROJ/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- GENERATED by gates/build-and-run-godot-editor-export-web.sh — do not commit.
     The feed path is native (godot_fork_native_path): the restore runs under the
     editor, and NuGet on Windows cannot read an MSYS /c/... feed. -->
<configuration>
  <packageSources>
    <clear />
    <add key="dn2cpp-godot-fork-local" value="$(godot_fork_native_path "$FORK_ROOT/nuget")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# Only the Web preset's template path is machine-specific. The desktop/iOS/Android
# presets in the same file keep their empty custom_template, which is fine: we
# never export them here.
#
# The path written is native (godot_fork_native_path): the editor resolves it with
# FileAccess, and an MSYS /c/... custom_template/release reaches Godot as a missing
# file — reported as "Custom release template not found", not as a path-format bug.
#
# The scope test names the preset it wants rather than the ones it does not: an
# "everything below preset.3 until preset.0-2" rule silently extends over any
# preset ADDED after the Web one (the Windows desktop preset is preset.4), and
# would then write a Web template zip into a desktop preset's custom_template.
WEB_TEMPLATE_NATIVE="$(godot_fork_native_path "$FORK_ROOT/web_template.zip")"
presets_tmp="$(mktemp)"
awk -v tmpl="$WEB_TEMPLATE_NATIVE" '
    /^\[preset\./ { inweb = ($0 ~ /^\[preset\.3[.\]]/) }
    inweb && /^custom_template\/release=""/ { print "custom_template/release=\"" tmpl "\""; next }
    { print }
' "$PROJ/export_presets.cfg" > "$presets_tmp"
mv "$presets_tmp" "$PROJ/export_presets.cfg"
grep -qF "$WEB_TEMPLATE_NATIVE" "$PROJ/export_presets.cfg" \
    || { echo "FAIL: could not patch custom_template/release into the Web preset" >&2; exit 1; }

echo "== 4/8 Importing the project (fork editor, headless) =="
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --import >"$OUT/import.log" 2>&1 || true

echo "== 5/8 Exporting with dotnet/export_backend = dn2cpp =="
WEBDIR="$PWD/$OUT/web"
rm -rf "$WEBDIR"
mkdir -p "$WEBDIR"
if ! godot_export_step 2400 "$OUT/export.log" "$WEBDIR/index.html" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release dn2cpp-web "$WEBDIR/index.html"; then
    echo "FAIL: --export-release failed (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# `--export-release` exits 0 even when an export plugin reported an error: the C#
# plugin catches everything and turns it into AddMessage(Error, …), which the
# editor downgrades to "completed with warnings". So a broken export produces a
# plausible-looking directory and a zero exit code, and these assertions -- not
# the exit code -- are what makes this gate an oracle.
if grep -q "ERROR: Export .NET Project" "$OUT/export.log"; then
    echo "FAIL: the C# export plugin reported an error (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
for marker in "dn2cpp: transpiling" "dn2cpp: compiling the drop-in library" "dn2cpp: staged"; do
    grep -qF "$marker" "$OUT/export.log" \
        || { echo "FAIL: export log lacks the dn2cpp marker: $marker" >&2; cat "$OUT/export.log" >&2; exit 1; }
done
# The slot imported the bundle's web prebuilt runtime. What identifies this axis
# is the emsdk both sides compile through, and both now reach for the SAME one --
# the bundle's own emsdk/ -- so the key records its toolchain file relative to the
# bundle root and matching no longer rests on two repositories resolving one host
# path alike. A drift is still fail-safe and therefore silent: the slot builds the
# runtime from source, correct and ~3 s slower, and nothing else here can tell.
# The line lands in the exporter's own log, which tees the configure's stdout; the
# engine's export.log never sees it.
EXPORTER_LOG="$(first_line "$(ls -t "$PROJ"/.godot/mono/temp/bin/dn2cpp/logs/export-*.log 2>/dev/null)")"
[ -n "$EXPORTER_LOG" ] || { echo "FAIL: the exporter wrote no log under .godot/mono/temp/bin/dn2cpp/logs" >&2; exit 1; }
grep -qF "using the prebuilt runtime" "$EXPORTER_LOG" || {
    echo "FAIL: the Web slot did not import the bundle's prebuilt runtime ($EXPORTER_LOG)" >&2
    grep -i "prebuilt" "$EXPORTER_LOG" >&2 || echo "  (the configure said nothing about a prebuilt at all)" >&2
    exit 1; }
# And it compiled through the SDK inside that bundle, not one this host happens to
# carry. Nothing downstream can tell the two apart -- a host SDK of the same
# version produces the same artifacts -- so a bundle that quietly stopped shipping
# an emsdk would be invisible everywhere a machine has one, which is every machine
# that develops this lane. The origin is matched as a PREFIX: an SDK named by the
# editor setting says so after the path.
EMSDK_ORIGIN="$(first_line "$(grep -E '^emscripten: ' "$EXPORTER_LOG" || true)")"
case "$EMSDK_ORIGIN" in
    *"(bundled: "*) ;;
    *) echo "FAIL: the export did not compile through a bundled Emscripten SDK: ${EMSDK_ORIGIN:-<no emscripten line in $EXPORTER_LOG>}" >&2
       exit 1 ;;
esac
EMSDK_USED="${EMSDK_ORIGIN#*\(bundled: }"
EMSDK_USED="${EMSDK_USED%%,*}"
EMSDK_USED="${EMSDK_USED%%)*}"
STAGED_EMSDK="$FORK_GODOTSHARP/Dn2Cpp/emsdk"
[ -d "$EMSDK_USED" ] && [ "$(cd "$EMSDK_USED" && pwd -P)" = "$(cd "$STAGED_EMSDK" && pwd -P)" ] || {
    echo "FAIL: the export's Emscripten SDK is not the staged bundle's" >&2
    echo "      used:   $EMSDK_USED" >&2
    echo "      staged: $STAGED_EMSDK" >&2
    exit 1; }

echo "== 6/8 Asserting the artifacts =="
DROPIN="$WEBDIR/$PROJECT_NAME.so"
# (a) the drop-in is beside the HTML, under the exact name the engine will dlopen
[ -f "$DROPIN" ] || {
    echo "FAIL: no drop-in beside index.html at $DROPIN" >&2; ls -la "$WEBDIR" >&2; exit 1; }
# (b) it is a wasm side module exporting the entry point GDMono resolves
wasm_exports "$DROPIN" > "$OUT/dropin-exports.txt"
grep -qx "godotsharp_game_main_init" "$OUT/dropin-exports.txt" || {
    echo "FAIL: $DROPIN does not export godotsharp_game_main_init" >&2
    head -20 "$OUT/dropin-exports.txt" >&2; exit 1; }
# (c) the generated HTML tells the loader to preload it -- without this the
#     dlopen finds nothing, and it is the exporter's job, not the engine's
grep -q "\"gdextensionLibs\":\[\"$PROJECT_NAME.so\"\]" "$WEBDIR/index.html" || {
    echo "FAIL: index.html does not list $PROJECT_NAME.so in gdextensionLibs" >&2
    head -c 600 >&2 <<<"$(grep -o 'GODOT_CONFIG = .*' "$WEBDIR/index.html")"; echo >&2; exit 1; }
# (d) the engine itself is a side module: this is a dlink template, and only a
#     dlink template has a dlopen that is not a stub returning NULL
[ -f "$WEBDIR/index.side.wasm" ] || {
    echo "FAIL: no index.side.wasm — the export used a non-dlink template" >&2; exit 1; }
# (e) no function in the drop-in exceeds V8's per-function ceiling.
#
# This is the assert that makes the gate an oracle for the trim, and it must be on
# the ARTIFACT, never on an exit code. The exporter passes --trim-reflection to the
# transpiler; src/Dn2Cpp.Cli/Program.cs's argument loop has no unknown-argument
# branch, so a transpiler predating the flag ACCEPTS it, exits 0, and emits fully
# untrimmed output. Every exit code in this gate stays green, the export log keeps
# all three dn2cpp markers, the .so is produced, it exports the entry point, and
# index.html preloads it -- (a) through (d) above all pass -- and the module is
# simply one no browser can instantiate. That is a stale toolchain, and the only
# thing that can see it is the size of the function the trim exists to shrink:
# __wasm_apply_data_relocs, one i32.store per pointer in static data, emitted by
# wasm-ld as a SINGLE function. Untrimmed it was 9,031,961 bytes against a ceiling
# of 7,654,321; with --trim-reflection (and --trim-godot-classes beside it)
# it is ~0.77 MB.
#
# Section 7 would eventually catch it in the browser, but only for as long as a
# browser is reachable: it degrades to gate_partial when Chrome is absent, so on a
# machine without one the gate would report a green having asserted nothing about
# the one property that blocks the ship. Assert it here, where nothing can opt out.
read -r maxfn maxfn_name < <(node gates/_wasm_symbols.js maxfunc "$DROPIN")
if [ "$maxfn" -ge "$V8_MAX_FUNCTION_SIZE" ]; then
    echo "FAIL: the drop-in's largest function is $maxfn bytes ($maxfn_name), at or over V8's" >&2
    echo "      hard per-function ceiling of $V8_MAX_FUNCTION_SIZE — no browser will instantiate it." >&2
    echo "      Expected the exporter to pass --trim-reflection and the transpiler to honour it." >&2
    echo "      A transpiler predating the flag ignores it silently and exits 0; re-bake the" >&2
    echo "      self-host CLI (gates/selfhost-emit.sh) if $SELFHOST_BIN is stale." >&2
    exit 1
fi
echo "drop-in: $(wc -c < "$DROPIN" | tr -d ' ') bytes, exports godotsharp_game_main_init; index.html preloads it"
echo "largest wasm function: $maxfn bytes ($maxfn_name), $((V8_MAX_FUNCTION_SIZE - maxfn)) under V8's $V8_MAX_FUNCTION_SIZE ceiling"

echo "== 7/8 Running the exported game in a real browser =="
# node cannot host a Godot web build (the engine JS targets ENVIRONMENT=web,worker
# and needs a DOM), so this runs in a real Chrome — see gates/_run_in_chrome.js for
# why that browser is not headless and rasterizes through SwiftShader.
#
# The harness reuses the EXPORTER'S OWN config verbatim -- gdextensionLibs and all
# -- and overrides only the arguments, so what runs is what a user would get.
#
# The engine gets NO --headless, and that is the point. This used to pass Godot
# `--headless`, which picks DisplayServerHeadless: registered on every platform and
# rasterizing to nothing, so the browser only had to run wasm, not draw. It made the
# gate cheap and it made it a liar. Godot's driver fallback deliberately never
# selects the headless display server, so a game in a browser ALWAYS gets a real
# DisplayServerWeb -- the gate was asserting a path no shipped game takes, and
# skipping the one that ships. It is also the path with the bug: upstream's
# DisplayServerWeb::get_singleton() static_casts whatever display server exists, so
# under --headless it reinterprets a DisplayServerHeadless and main_loop_callback()
# reads past the end of the object. Whether that traps is an address lottery -- any
# dlopen'd side module with a data segment shifts the wasm heap and can turn it on --
# which is exactly the kind of failure a gate must not be structurally blind to.
# The run below proves the real one comes up: the engine reports a WebGL2 context.
#
# Audio, and ONLY audio, is stubbed out, because the real Web audio driver cannot
# survive a program that quits as fast as a probe does. GodotAudioWorklet.create()
# reads GodotAudio.ctx inside the .then() of an async audioWorklet.addModule(), while
# the engine's shutdown (close_async) sets that same ctx to null -- so a game that
# quits inside the module-load window gets `new AudioWorkletNode(null, ...)`, a
# TypeError thrown into the page from engine JS. It is not cosmetic and it is not
# ours: it also kills the exit path, so the module never calls onExit and the run can
# only end by timing out. A real game cannot lose that race -- it quits minutes after
# boot, not milliseconds -- but this probe quits on its FIRST _Process frame, which is
# precisely the pathological timing. Stubbing the driver keeps the assertion the gate
# actually makes (the drop-in, the display server, the managed lifecycle) exact and
# deterministic; this gate never asserted anything about audio.
#
# FIXED in the fork (`web: don't build an audio worklet on a closed context`)
# -- and the stub stays anyway, which is the opposite of a leftover. Taking
# it out would not assert the fix: on a local server the module lands in
# milliseconds, so this probe wins the race and exits cleanly with the real driver
# even against a template WITHOUT the fix (measured, 2026-08-02). What it would buy
# is a real AudioContext in a headed browser -- an autoplay policy and an output
# device this gate has no assertion about. Reproducing the race takes stalling the
# worklet module's own request: with `index.audio.worklet.js` delayed 10 s, the
# pre-fix template throws 'TypeError: Failed to construct AudioWorkletNode' and
# never reaches onExit inside 60 s, the fixed one exits 0 in 13 s.
cat > "$WEBDIR/harness.html" <<'EOF'
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>dn2cpp web export gate</title></head>
<body>
<canvas id="canvas" width="64" height="64"></canvas>
<script src="index.js"></script>
<script>
function exportedConfig(html) {
    // Brace-match rather than regex: the config is JSON and may contain braces.
    var key = 'const GODOT_CONFIG = ';
    var at = html.indexOf(key);
    if (at < 0) { throw new Error('index.html carries no GODOT_CONFIG'); }
    var i = html.indexOf('{', at), depth = 0, inStr = false, esc = false;
    for (var j = i; j < html.length; j++) {
        var c = html[j];
        if (inStr) {
            if (esc) { esc = false; } else if (c === '\\') { esc = true; } else if (c === '"') { inStr = false; }
        } else if (c === '"') { inStr = true; }
        else if (c === '{') { depth++; }
        else if (c === '}' && --depth === 0) { return JSON.parse(html.slice(i, j + 1)); }
    }
    throw new Error('unterminated GODOT_CONFIG');
}

fetch('index.html').then(function (r) { return r.text(); }).then(function (html) {
    var cfg = exportedConfig(html);
    console.log('HARNESS_CONFIG gdextensionLibs=' + JSON.stringify(cfg.gdextensionLibs || []));
    cfg.canvas = document.getElementById('canvas');
    cfg.args = ['--audio-driver', 'Dummy'];
    cfg.onPrint = function () { console.log(Array.prototype.join.call(arguments, ' ')); };
    cfg.onPrintError = function () { console.log('[stderr] ' + Array.prototype.join.call(arguments, ' ')); };
    cfg.onExit = function (code) { console.log('HARNESS_EXIT code=' + code); };
    var engine = new Engine(cfg);
    return engine.startGame().then(function () { console.log('HARNESS_START_OK'); });
}).catch(function (e) {
    console.log('HARNESS_FAILED ' + (e && e.message ? e.message : e));
    console.log('HARNESS_EXIT code=-1');
});
</script>
</body>
</html>
EOF

SRV_LOG="$OUT/httpd.log"
# -u: python block-buffers stdout when it is a file, so the "Serving HTTP on …
# port N" line would sit in the buffer and the port scrape below would time out.
# shellcheck disable=SC2086  # $py may be `py -3`: two words, deliberately split
$py -u -m http.server 0 --bind 127.0.0.1 --directory "$WEBDIR" >"$SRV_LOG" 2>&1 &
SRV_PID=$!
# A hook, not a bare `trap … EXIT`: this gate holds the machine lock for its
# whole run (gate_machine_lock in _common.sh), and the shell's single trap slot
# would evict that lock's release.
gate_add_exit_hook 'kill "$SRV_PID" 2>/dev/null || true'
PORT=""
for _ in $(seq 1 60); do
    PORT="$(LC_ALL=C sed -n 's/.*port \([0-9][0-9]*\).*/\1/p' "$SRV_LOG")"
    PORT="${PORT%%$'\n'*}"
    [ -n "$PORT" ] && break
    sleep 0.2
done
[ -n "$PORT" ] || { echo "FAIL: the local http server never reported a port" >&2; cat "$SRV_LOG" >&2; exit 1; }
# .wasm must be served as application/wasm: the loader uses instantiateStreaming
# with no MIME fallback. python3's mimetypes has mapped it since 3.9.
echo "serving $WEBDIR on 127.0.0.1:$PORT"

LOG="$OUT/run.log"
rc=0
node gates/_run_in_chrome.js "http://127.0.0.1:$PORT/harness.html" 180000 "HARNESS_EXIT" \
    >"$LOG" 2>&1 || rc=$?
kill "$SRV_PID" 2>/dev/null || true
cat "$LOG"
if [ "$rc" -eq 77 ]; then
    # gate_partial does not exit: the assertions below have nothing to read, so
    # they must not run.
    gate_partial "no headless Chrome — the export was asserted but never run"
else
    if [ "$rc" -ne 0 ]; then
        echo "FAIL: the browser runner failed with $rc" >&2
        exit 1
    fi
    for marker in \
        "DN2CPP_EXPORT_READY name=Main answer=42" \
        "DN2CPP_EXPORT_GDPRINT engine logger path ok" \
        "DN2CPP_EXPORT_PROCESS class=Node inTree=True deltaOk=True" \
        "DN2CPP_EXPORT_GC finalized=True bounded=True" \
        "DN2CPP_EXPORT_DONE"; do
        grep -qF "$marker" "$LOG" || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
    done
    for once in "DN2CPP_EXPORT_READY" "DN2CPP_EXPORT_GDPRINT" "DN2CPP_EXPORT_PROCESS" "DN2CPP_EXPORT_GC" "DN2CPP_EXPORT_DONE"; do
        n="$(grep -c "$once" "$LOG" || true)"
        [ "$n" -eq 1 ] || { echo "FAIL: marker $once appeared $n times (expected exactly 1)" >&2; exit 1; }
    done
    # The C# side must have come up through the AOT drop-in, and nothing may have
    # trapped on the way. A wasm trap escaping into the page is not cosmetic: the
    # exported shell reports it to the player as a failure notice.
    #
    # "ERROR" is the engine's own error-log prefix, in this log only because the
    # harness routes cfg.onPrintError into it. Rejected for the reason the native
    # .NET-module gates reject it, which binds harder in a browser: the engine
    # reports and keeps running, so no exit code ever carries the failure.
    for bad in \
        "GodotPlugins initialization failed" \
        "Failed to load hostfxr" \
        "Unable to find the .NET assemblies directory" \
        "HARNESS_FAILED" \
        "null function or function signature mismatch" \
        "__PAGE_EXCEPTION" \
        "ERROR" \
        "SCRIPT ERROR"; do
        if grep -q "$bad" "$LOG"; then
            echo "FAIL: the browser run reported \"$bad\"" >&2
            exit 1
        fi
    done
fi

echo "== 8/8 Refusing a preset the engine could not load the drop-in from =="
# Without Extensions Support the template is not a dlink build, so its dlopen is
# an Emscripten stub that returns NULL: the drop-in would be built, staged,
# shipped, and then silently fail to load in the player's browser. The backend
# must refuse before it transpiles anything.
BADPROJ="$OUT/project-nodlink"
rm -rf "$BADPROJ"
mkdir -p "$BADPROJ"
cp -R "$PROJ/." "$BADPROJ/"
bad_tmp="$(mktemp)"
sed 's|^variant/extensions_support=true|variant/extensions_support=false|' \
    "$BADPROJ/export_presets.cfg" > "$bad_tmp"
mv "$bad_tmp" "$BADPROJ/export_presets.cfg"
grep -q '^variant/extensions_support=false' "$BADPROJ/export_presets.cfg" \
    || { echo "FAIL: could not turn Extensions Support off in the preset" >&2; exit 1; }

BADWEB="$PWD/$OUT/web-nodlink"
rm -rf "$BADWEB"
mkdir -p "$BADWEB"
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$BADPROJ" --export-release dn2cpp-web "$BADWEB/index.html" \
    >"$OUT/export-nodlink.log" 2>&1 || true
if grep -qF "dn2cpp: transpiling" "$OUT/export-nodlink.log"; then
    echo "FAIL: a non-dlink preset got as far as transpiling — the refusal is too late" >&2
    cat "$OUT/export-nodlink.log" >&2
    exit 1
fi
if [ -f "$BADWEB/$PROJECT_NAME.so" ]; then
    echo "FAIL: a non-dlink preset still shipped a drop-in at $BADWEB/$PROJECT_NAME.so" >&2
    exit 1
fi
echo "a non-dlink Web preset is refused before the transpile"

# Tripwire: a concurrent suite re-staging the shared toolchain mid-run must
# surface as this self-explaining FAIL, not as a cryptic include error inside
# the export above.
assert_editor_toolchain_current "$FORK_GODOTSHARP"
gate_cache_commit
echo "OK"
