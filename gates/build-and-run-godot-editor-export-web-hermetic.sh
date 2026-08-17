#!/usr/bin/env bash
# Godot editor-export gate, Web lane: the export runs on a machine carrying
# everything the export needs but dotnet. The Emscripten SDK, the node it runs
# every link on, the cmake and the ninja that ship INSIDE the toolchain bundle
# are the only ones reachable, and they are read-only while the export compiles.
#
# The subject is what the bundle carries, not the Web export: what a Web export
# produces is byte-identical whether it compiled through the bundle's tools or
# through the host's, so the plain Web gate stays green on a bundle that stopped
# shipping an SDK or a cmake, or whose baked cache is short a variant the export
# resolves — on every machine that develops this lane, i.e. every machine that
# has them installed. The only way to see that is to take the host's away:
#
#   * PATH loses every Emscripten front-end plus node, cmake and ninja
#     (path_without_tools), and every variable an activated emsdk exports is
#     unset, so nothing the editor inherits can name a second SDK. It also loses
#     every host C++ compiler: a Web export must need none, Emscripten bringing
#     its own, and this is the only gate holding that claim — the desktop lane
#     asserts the opposite refusal. node goes with them because the SDK now
#     carries one, and a machine that has none of its own is the only place that
#     proves it. python3 and dotnet stay: emcc runs the first, the publish the
#     second.
#   * The staged bundle's emsdk and buildtools are `chmod -R a-w` for the
#     export's duration. An editor installs the bundle where the user cannot
#     write, so a cache write is a failure on the user's machine and must be one
#     here; the frozen cache is what makes that possible, and this is what proves
#     it holds for a real export rather than for the three probe links packaging
#     does.
#
# An absent SDK, node, cmake or ninja is therefore NOT a skip reason — it is the
# premise. The skips are for the fork editor, the Web template, and the python
# emcc runs.
#
# No browser: the plain Web gate runs the exported game, and everything downstream
# of the link is identical here. This one ends at the artifacts.
#
# Registered in run-all-gates.sh's SERIAL Godot phase, in the chain that writes
# $FORK_GODOTSHARP/Dn2Cpp: it launches the engine, and every launch runs under a
# watchdog because a broken Godot run hangs rather than fails.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_fork.sh"

OUT=gates/out-godot-editor-export-web-hermetic
SAMPLE=samples/godot-dotnet/EditorExportSample
PROJECT_NAME=EditorExportSample

# V8's hard per-function ceiling, a compiled-in constant (Chrome enforces it too).
V8_MAX_FUNCTION_SIZE=7654321

godot_fork_preflight
if [ ! -f "$FORK_ROOT/web_template.zip" ]; then
    gate_skip "no Web template at $FORK_ROOT/web_template.zip — run gates/setup-godot-fork-web.sh"
fi
# emcc, node, cmake and ninja are NOT probed here, deliberately: the bundle ships
# all four, and a host holding none is this gate's premise rather than a reason
# not to run. The assertions below use the bundle's node too.

echo "== 1/6 The machine, minus everything the bundle carries =="
# path_without_tools reads THIS PATH, so it runs before anything edits it; the
# result is handed to the editor and never exported here, because the assertions
# below use the gate's own tools.
# The compiler names cover what HostCxxCompiler() probes and what cmake would
# pick unaided; taking them away is what turns the exporter's "Web needs no host
# C++ compiler" carve-out into an assertion. path_without_tools mirrors a
# directory minus the names rather than dropping it, which is what lets /usr/bin
# lose clang++ and keep the other nine hundred tools.
# shellcheck disable=SC2086 — the front-end list is a word list, deliberately split.
HERMETIC_PATH="$(path_without_tools $EMSDK_FRONTENDS node cmake ninja clang clang++ cc c++ gcc g++)"
HERMETIC_ENV=(env)
for v in EMSDK EMSDK_NODE EMSDK_PYTHON EMSCRIPTEN; do
    HERMETIC_ENV+=(-u "$v")
done
# EM_* is read from the live environment rather than listed: emcc grows settings
# variables, and one this file has never heard of still steers the SDK.
while IFS= read -r v; do
    HERMETIC_ENV+=(-u "$v")
done < <(env | LC_ALL=C sed -n 's/^\(EM_[A-Za-z0-9_]*\)=.*/\1/p')
# TERM is load-bearing, not cosmetic: a macOS Godot that sees none decides it was
# launched from Finder and re-imports the login shell's whole environment,
# OVERWRITING PATH — so under a TERM-less launcher (launchd, cron) the editor
# un-strips its own PATH and every assertion below holds about the host's tools.
HERMETIC_ENV+=("PATH=$HERMETIC_PATH" TERM=dumb)
# The synthesis is the gate's premise, so it is asserted rather than assumed: a
# mirror directory that silently kept its emcc — or its cmake — would leave every
# assertion below passing about the host's copy.
for tool in emcc em++ emcmake emar node cmake ninja clang clang++ cc c++ gcc g++; do
    if PATH="$HERMETIC_PATH" command -v "$tool" >/dev/null 2>&1; then
        echo "FAIL: $tool is still reachable on the synthesized PATH — this gate would prove nothing" >&2
        echo "      $HERMETIC_PATH" >&2
        exit 1
    fi
done
# The one the bundle deliberately carries none of.
for tool in dotnet; do
    PATH="$HERMETIC_PATH" command -v "$tool" >/dev/null 2>&1 \
        || gate_skip "$tool is not on PATH, and the bundle ships none (the export needs the machine's)"
done
# The other, and the one presence cannot answer: emcc is a launcher over python
# and macOS answers `python3` with an Xcode stub stuck at 3.9.6 the export
# refuses. The empty second argument is $HERMETIC_ENV's `env -u EMSDK_PYTHON`
# stated where the probe can see it — the editor is launched without one, so it
# falls to the PATH arm, which is the whole difference from the plain Web gate.
godot_fork_emcc_python_check "$HERMETIC_PATH" ""
echo "PATH without the bundled tools: $(printf '%s' "$HERMETIC_PATH" | tr ':' '\n' | wc -l | tr -d ' ') entries"

# The pin/ABI tripwire also resolves $FORK and $BASE_COMMIT, which the template
# check fingerprints the zip against. Both run live, before the cache check: a
# `tmpl=` key term holds still precisely while the artifact is stale.
godot_fork_pin_abi_check
godot_fork_template_check "$FORK_ROOT/web_template.zip" "Web export template" \
    "FORCE=1 gates/setup-godot-fork-web.sh"

# The bundled tools are the subject, so their identity is a key term: a
# re-packaged bundle carrying a different SDK or a different cmake is a different
# gate run. Each stamp is the staged tree's own (versions, upstream archive
# sha256, staging recipe) — the export reads those copies, not the layout they
# were assembled in.
STAGED_DN2CPP="$FORK_GODOTSHARP/Dn2Cpp"
STAGED_EMSDK="$STAGED_DN2CPP/emsdk"
STAGED_BUILDTOOLS="$STAGED_DN2CPP/buildtools"
mkdir -p "$OUT"
if gate_cache_check "$OUT" \
    "godot-editor-export-web-hermetic|$(godot_fork_ctx)|tmpl=$(file_sig "$FORK_ROOT/web_template.zip")|emsdk=$(file_text "$STAGED_EMSDK/.emsdk-stamp")|buildtools=$(file_text "$STAGED_BUILDTOOLS/.buildtools-stamp")" \
    "$SELFHOST_BIN" \
    dist/package-toolchain.sh \
    "$SAMPLE" \
    gates/_wasm_symbols.js \
    "$ABI_EXPECTED"; then
    { gate_cache_hit_msg; exit 0; }
fi

echo "== 2/6 Installing the working tree's toolchain into the fork editor =="
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log"
[ -x "$STAGED_EMSDK/emscripten/emcc" ] || {
    echo "FAIL: the staged toolchain carries no Emscripten SDK at $STAGED_EMSDK" >&2
    echo "      That SDK is this gate's whole subject, and a host with none cannot export to" >&2
    echo "      the Web at all. Unpack the pinned one (gates/setup-emsdk.sh) and re-package." >&2
    exit 1; }
STAGED_NODE="$(bundled_node "$STAGED_DN2CPP")"
[ -x "$STAGED_NODE" ] || {
    echo "FAIL: the staged toolchain carries no node at $STAGED_NODE" >&2
    echo "      The SDK's own node is this gate's subject alongside the SDK: every emcc link" >&2
    echo "      runs one, and the export below runs on a PATH carrying none. Unpack the" >&2
    echo "      pinned SDK (gates/setup-emsdk.sh) and re-package." >&2
    exit 1; }
# Same shape, same reason: PATH has no cmake and no ninja by now, so a bundle
# short either cannot export at all — and a bundle that quietly stopped staging
# them must fail here rather than skip.
for exe in "$(bundled_cmake "$STAGED_DN2CPP")" "$(bundled_ninja "$STAGED_DN2CPP")"; do
    [ -x "$exe" ] || {
        echo "FAIL: the staged toolchain carries no $exe" >&2
        echo "      The bundled cmake and ninja are this gate's subject alongside the SDK, and" >&2
        echo "      the export below runs on a PATH holding neither. Unpack the pinned pair" >&2
        echo "      (gates/setup-buildtools.sh) and re-package." >&2
        exit 1; }
done

echo "== 3/6 Staging the sample project =="
PROJ="$OUT/project"
rm -rf "$PROJ"
mkdir -p "$PROJ"
cp -R "$SAMPLE/." "$PROJ/"

cat > "$PROJ/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- GENERATED by gates/build-and-run-godot-editor-export-web-hermetic.sh — do not commit. -->
<configuration>
  <packageSources>
    <clear />
    <add key="dn2cpp-godot-fork-local" value="$(godot_fork_native_path "$FORK_ROOT/nuget")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# Only the Web preset (preset.3) gets the template path; the scope test names the
# preset it wants rather than the ones it does not, so a preset added later cannot
# inherit a Web template zip.
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

echo "== 4/6 Importing the project (fork editor, headless, no SDK in the environment) =="
run_with_watchdog 600 "${HERMETIC_ENV[@]}" "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --import >"$OUT/import.log" 2>&1 || true

echo "== 5/6 Exporting with the bundled SDK and build tools read-only =="
WEBDIR="$PWD/$OUT/web"
rm -rf "$WEBDIR"
mkdir -p "$WEBDIR"
# The reference for the write test is touched AFTER staging, so a re-staged tree's
# fresh mtimes do not read as writes by the export.
touch "$OUT/ro-reference"
chmod -R a-w "$STAGED_EMSDK" "$STAGED_BUILDTOOLS"
# A hook, not a bare `trap … EXIT`: this gate holds the machine lock for its whole
# run, and the shell's single trap slot would evict that lock's release.
gate_add_exit_hook "chmod -R u+w '$STAGED_EMSDK' '$STAGED_BUILDTOOLS' 2>/dev/null || true"
export_rc=0
godot_export_step 2400 "$OUT/export.log" "$WEBDIR/index.html" \
    "${HERMETIC_ENV[@]}" "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release dn2cpp-web "$WEBDIR/index.html" || export_rc=$?
# Read the write test while the tree is still read-only, before restoring: a chmod
# moves ctime, and `find -newer` reads mtime, but the order removes the question.
ro_written="$(find "$STAGED_EMSDK" "$STAGED_BUILDTOOLS" -newer "$OUT/ro-reference" -print -quit)"
chmod -R u+w "$STAGED_EMSDK" "$STAGED_BUILDTOOLS"
if [ "$export_rc" -ne 0 ]; then
    echo "FAIL: --export-release failed with nothing on PATH but dotnet (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
if [ -n "$ro_written" ]; then
    echo "FAIL: the export wrote into the bundle: $ro_written" >&2
    echo "      An editor installs the bundle where the user cannot write, so this export" >&2
    echo "      fails on their machine. The SDK's cache must stay frozen and complete, and" >&2
    echo "      cmake writes nothing into CMAKE_ROOT." >&2
    exit 1
fi
echo "the bundled SDK and build tools were read-only across the export and nothing under them was written"

echo "== 6/6 Asserting the export =="
# The plugin reports errors as warnings, so the log is the export's failure
# signal; a headless editor may still exit 0.
if grep -q "ERROR: Export .NET Project" "$OUT/export.log"; then
    echo "FAIL: the C# export plugin reported an error (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# The python line is the whole trace of the preflight that runs the interpreter
# emcc's launchers would start; a prefix, because the exe, its version and which
# of the three arms resolved it all vary by host. The node line is the same
# preflight's, and which one it named is pinned below.
for marker in "dn2cpp: python " "dn2cpp: node " "dn2cpp: transpiling" \
              "dn2cpp: compiling the drop-in library" "dn2cpp: staged"; do
    grep -qF "$marker" "$OUT/export.log" \
        || { echo "FAIL: export log lacks the dn2cpp marker: $marker" >&2; cat "$OUT/export.log" >&2; exit 1; }
done

# Which SDK it compiled through, from the engine log's own line. Matched as a
# PREFIX of the path: an SDK named by the editor setting says so after it, and
# that spelling is a DIFFERENT origin — it would mean the machine still had one.
EMSDK_LINE="$(first_line "$(grep -F 'dn2cpp: emscripten' "$OUT/export.log" || true)")"
[ -n "$EMSDK_LINE" ] || {
    echo "FAIL: the export log carries no 'dn2cpp: emscripten' line — the backend named no SDK" >&2
    exit 1; }
# The editor is a native app, so on Windows it logs a native BACKSLASH path
# (godot_fork_native_path's header, above, is the same invariant for the paths
# this script hands the editor). Fold both sides to forward slashes before the
# match; a no-op on POSIX, where neither form ever contains a backslash.
STAGED_EMSDK_NATIVE="$(godot_fork_native_path "$STAGED_EMSDK" | tr '\\' /)"
EMSDK_LINE_NATIVE="${EMSDK_LINE//\\//}"
case "$EMSDK_LINE_NATIVE" in
    *"(bundled: $STAGED_EMSDK_NATIVE)"*|*"(bundled: $STAGED_EMSDK_NATIVE,"*) ;;
    *) echo "FAIL: the export did not compile through the staged bundle's SDK" >&2
       echo "      line:   $EMSDK_LINE" >&2
       echo "      staged: $STAGED_EMSDK_NATIVE" >&2
       exit 1 ;;
esac
echo "$EMSDK_LINE"

# And which node every link of that SDK ran, from the same preflight's line. The
# path is matched WHOLE against the staged SDK's own: the other two origins the
# backend can report — an inherited EM_NODE_JS, or a PATH search — would each
# mean the machine still had a node of its own, which is what this gate takes
# away.
NODE_LINE="$(first_line "$(grep -F 'dn2cpp: node ' "$OUT/export.log" || true)")"
[ -n "$NODE_LINE" ] || {
    echo "FAIL: the export log carries no 'dn2cpp: node' line — the backend named no node" >&2
    exit 1; }
STAGED_NODE_NATIVE="$(godot_fork_native_path "$STAGED_NODE" | tr '\\' /)"
NODE_LINE_NATIVE="${NODE_LINE//\\//}"
case "$NODE_LINE_NATIVE" in
    *"dn2cpp: node $STAGED_NODE_NATIVE ("*) ;;
    *) echo "FAIL: the export did not link through the staged bundle's own node" >&2
       echo "      line:   $NODE_LINE" >&2
       echo "      staged: $STAGED_NODE_NATIVE" >&2
       exit 1 ;;
esac
echo "$NODE_LINE"

# The exporter's own log, which the engine's never sees: the Web slot must have
# imported the bundle's prebuilt runtime rather than rebuilding it. A drift is
# fail-safe and therefore silent — the slot builds the runtime from source,
# correct and slower — so nothing but this line can tell.
EXPORTER_LOG="$(first_line "$(ls -t "$PROJ"/.godot/mono/temp/bin/dn2cpp/logs/export-*.log 2>/dev/null)")"
[ -n "$EXPORTER_LOG" ] || { echo "FAIL: the exporter wrote no log under .godot/mono/temp/bin/dn2cpp/logs" >&2; exit 1; }
grep -qF "using the prebuilt runtime" "$EXPORTER_LOG" || {
    echo "FAIL: the Web slot did not import the bundle's prebuilt runtime ($EXPORTER_LOG)" >&2
    grep -i "prebuilt" "$EXPORTER_LOG" >&2 || echo "  (the configure said nothing about a prebuilt at all)" >&2
    exit 1; }

# Which cmake and ninja it configured through. There is no other candidate on
# this PATH, so a bundle short either would have died above — what this catches
# is the export resolving them from somewhere the premise does not cover, an
# editor setting a stale project file left behind among them.
godot_fork_assert_bundled_buildtools "$OUT/export.log" "$EXPORTER_LOG" "$STAGED_DN2CPP" "$OUT" || exit 1

DROPIN="$WEBDIR/$PROJECT_NAME.so"
[ -f "$DROPIN" ] || {
    echo "FAIL: no drop-in beside index.html at $DROPIN" >&2; ls -la "$WEBDIR" >&2; exit 1; }
# A release side module has no name section, so grepping the file for a symbol
# would also match an import: parse the export section.
"$STAGED_NODE" gates/_wasm_symbols.js exports "$DROPIN" > "$OUT/dropin-exports.txt"
grep -qx "func godotsharp_game_main_init" "$OUT/dropin-exports.txt" || {
    echo "FAIL: $DROPIN does not export godotsharp_game_main_init" >&2
    head -20 "$OUT/dropin-exports.txt" >&2; exit 1; }

# No function may reach V8's per-function ceiling — the assert that makes this an
# oracle for the trim, on the ARTIFACT rather than an exit code. A transpiler
# predating --trim-reflection accepts the flag, exits 0 and emits untrimmed
# output; every other assertion here stays green and the module is simply one no
# browser can instantiate.
read -r maxfn maxfn_name < <("$STAGED_NODE" gates/_wasm_symbols.js maxfunc "$DROPIN")
if [ "$maxfn" -ge "$V8_MAX_FUNCTION_SIZE" ]; then
    echo "FAIL: the drop-in's largest function is $maxfn bytes ($maxfn_name), at or over V8's" >&2
    echo "      hard per-function ceiling of $V8_MAX_FUNCTION_SIZE — no browser will instantiate it." >&2
    echo "      Expected the exporter to pass --trim-reflection and the transpiler to honour it;" >&2
    echo "      re-bake the self-host CLI (gates/selfhost-emit.sh) if $SELFHOST_BIN is stale." >&2
    exit 1
fi
# Every import the drop-in makes must resolve against the page's own main module
# plus its JS glue. This gate never starts a browser, so it is the only thing here
# that can see an unresolved one: emscripten's dlopen hands back a valid handle and
# dlsym resolves, and the lazy stub throws `TypeError: resolved is not a function`
# at the instant the symbol is first CALLED, naming a wasm function index and never
# the symbol.
unsat="$("$STAGED_NODE" gates/_wasm_symbols.js unsatisfied "$DROPIN" "$WEBDIR/index.wasm" "$WEBDIR/index.js")"
if [ -n "$unsat" ]; then
    echo "FAIL: the exported main module + glue do not satisfy every drop-in import:" >&2
    printf '%s\n' "$unsat" | LC_ALL=C sed 's/^/  /' >&2
    echo "      A SystemNative_* name here is a .NET PAL symbol the wasm build does not" >&2
    echo "      define — runtime/core/platform/wasm/ carries only the sliver this target" >&2
    echo "      needs, and the POSIX translation unit is excluded from the Emscripten" >&2
    echo "      build (runtime/CMakeLists.txt's EMSCRIPTEN arm)." >&2
    exit 1
fi
echo "drop-in: $(wc -c < "$DROPIN" | tr -d ' ') bytes, exports godotsharp_game_main_init"
echo "largest wasm function: $maxfn bytes ($maxfn_name), $((V8_MAX_FUNCTION_SIZE - maxfn)) under V8's $V8_MAX_FUNCTION_SIZE ceiling"
echo "import closure OK: every function and GOT.mem/GOT.func import resolves against the exported page"

# Tripwire: a concurrent suite re-staging the shared toolchain mid-run must
# surface as this self-explaining FAIL, not as a cryptic include error above.
assert_editor_toolchain_current "$FORK_GODOTSHARP"
gate_cache_commit
echo "OK"
