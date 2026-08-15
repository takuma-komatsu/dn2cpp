#!/usr/bin/env bash
# Godot .NET-module (mono module) WASM gate: transpiles the same real Godot.NET.Sdk
# sample the other godot-dotnet-* gates use, against the real GodotSharp.dll with
# --dotnet-module, and links it into an Emscripten SIDE MODULE — the shape the Godot
# web export preloads and dlopens in place of the hostfxr-managed game assembly.
#
# No Godot here, on purpose. The engine's whole role in this handshake is "dlopen the
# library and call godotsharp_game_main_init with the interop table", so this gate
# plays that role itself (samples/godot-dotnet/DotnetSample/dm_wasm_host.cpp, built as
# a -sMAIN_MODULE=1 host and run under node). A failure is then unambiguously the
# drop-in's, and it lands in seconds rather than at the end of the longest gate in the
# suite as an opaque browser message.
#
# The assertions, in the order they buy the most:
#
#   1. The EXPORT surface is exactly the drop-in's. `emcc -shared` defaults to
#      SIDE_MODULE=1, which forces LINKABLE=1 — dead-code elimination off,
#      -sEXPORTED_FUNCTIONS silently ignored (emcc only warns), and every unresolved
#      symbol turned into a wasm import instead of a link error. The runtime build uses
#      SIDE_MODULE=2 instead; a regression back to =1 shows up here as an export count
#      in the tens of thousands, and nowhere else.
#
#   2. Every IMPORT resolves — both the env function imports (against the main module)
#      and the GOT.mem/GOT.func entries (against either module's exports, since the
#      loader fills the GOT from the pooled global symbol table). This is the assertion
#      that pays for the gate. A side module's unresolved symbol is not a link error and
#      is not even a dlopen error — emscripten hands back a valid handle, dlsym resolves,
#      and the lazy stub throws `TypeError: resolved is not a function` at the instant the
#      function is first CALLED, naming a wasm function index rather than the symbol; an
#      unresolved GOT entry is left at address 0 and misfires just as silently on first
#      use. Nothing else in the toolchain will ever tell you. (See gates/_wasm_symbols.js.)
#
#   3. An artifact size ceiling — a LINKABLE accident also shows up as an enormous .so.
#      Beside it, a tripwire that is about a DECISION rather than a defect: this closure
#      lowers no manifest-resource read, so it carries no resource blobs and the
#      --no-manifest-resources trim has nothing to shed on the lane whose budget would
#      have justified defaulting it on. The assert keeps that premise from rotting.
#
#   4. The module LOADS, and the entry runs, twice, in two processes (see the host).
#      Run A calls the entry with a deliberately wrong interop-table size: the transpiled
#      NativeFuncs.Initialize throws, and that managed throw — a C++ `throw` — must unwind
#      out of the transpiled body into the entry's catch THROUGH the __cpp_exception tag
#      the side module imports from the main module. Run B calls it with the size the
#      drop-in reports about itself and drives the success path: it asserts init PROGRESSES
#      through interop-received + managed-callbacks-written, then marks the engine-dependent
#      init tail partial — the script-class registration that follows runs GodotSharp static
#      constructors which call StringName construction back into the engine through the
#      interop table, and this engineless host hands a zeroed one. That tail's full init is
#      asserted, green, in the real engine by gates/build-and-run-godot-dotnet-trim.sh. Two
#      processes because Initialize latches `initialized` before it validates, so the second
#      call in a process can only ever answer "Already initialized."
#
# The transpile passes --trim-reflection, which is what makes section 4 reachable at all.
# Untrimmed, the drop-in's __wasm_apply_data_relocs (one i32.store per pointer in static
# data, emitted as a SINGLE function) was 9,031,961 bytes — over V8's hard 7,654,321-byte
# per-function ceiling — so no browser and no V8 could instantiate it, and this section
# could only be a gate_partial. Trimming the reflection member tables (the pointer-dense
# 75% of that function) brings it to ~4.9 MB, and the ceiling check below is now a hard
# assertion the load + entry runs behind. --trim-reflection is exactly what the Godot Web
# exporter passes, so this gate builds the same drop-in the export ships.
#
# Needs the artifacts of gates/setup-godot-dotnet.sh (found at that script's
# default root; DN2CPP_GODOT_DOTNET_ROOT overrides) plus the Emscripten
# toolchain and node; skips when either is absent.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_dotnet.sh"

OUT=gates/out-godot-dotnet-wasm
HOST_SRC=samples/godot-dotnet/DotnetSample/dm_wasm_host.cpp
SO="$OUT/libDotnetSample.so"

# V8's hard ceiling on a single wasm function body (kV8MaxWasmFunctionSize). It is a
# compiled-in constant, not a flag — no node option raises it, and Chrome enforces the
# same one — so a module holding a bigger function is not merely rejected here, it is
# unloadable in the browser this lane exists to target.
V8_MAX_FUNCTION_SIZE=7654321
# A LINKABLE=1 accident (no DCE) roughly doubles the artifact; the real drop-in is ~28 MB.
MAX_ARTIFACT_BYTES=33554432

# Bundled SDK: the subject is the export shape its frozen cache is baked to.
dn2cpp_emsdk_resolve
for tool in em++ emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found — no Emscripten toolchain to build the wasm drop-in with"
    fi
done
if ! godot_dotnet_root_ok; then
    gate_skip "godot-dotnet artifacts absent/incomplete at $GODOT_DOTNET_ROOT — run gates/setup-godot-dotnet.sh (or set DN2CPP_GODOT_DOTNET_ROOT)"
fi

echo "== 1/6 Building the sample game assembly (real Godot.NET.Sdk) =="
godot_dotnet_nuget_config "$GODOT_DOTNET_SAMPLE_DIR"
dotnet build "$GODOT_DOTNET_SAMPLE_DIR/DotnetSample.csproj" -c ExportRelease --nologo -v q
APP="$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll"
[ -f "$APP" ] || { echo "error: not built: $APP" >&2; exit 1; }

echo "== 2/6 Transpiling (--dotnet-module --trim-reflection, real GodotSharp + net10 CoreLib) =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
CORELIB=$(resolve_net10_corelib)
rm -rf "$OUT"
invoke_cli "$APP" --dotnet-module -r "$CORELIB" -r "$GODOT_DOTNET_GODOTSHARP" --auto-ref --trim-reflection -o "$OUT"

# That premise, as a tripwire — and it sits above the cache check so it is asserted
# on a warm hit too. Manifest-resource blobs are carried only when an emitted body
# lowers a read (Compilation.ManifestResourcesUsed), and nothing in the GodotSharp +
# game closure does: this lane carries no resource bytes, so the trim would shed
# nothing here and is deliberately NOT on by default for the Web export. A table
# appearing means the payoff stopped being zero and the decision is worth re-taking.
if grep -Eq 'restab_assembly[0-9]+\[\]' "$OUT"/generated*.cpp; then
    echo "FAIL: the --dotnet-module closure now emits a manifest-resource table." >&2
    grep -ho 'static const Dn2CppManifestResource restab_assembly[0-9]*\[\] = {[^;]*};' \
        "$OUT"/generated*.cpp >&2 || true
    echo "      The trim default was decided on this lane carrying none. Re-measure the cost and" >&2
    echo "      the reads' keep-set (DN2CPP_MANIFEST_TRACE) before adopting either answer." >&2
    exit 1
fi
echo "manifest OK: the Godot closure lowers no manifest-resource read, so none is carried"

if gate_cache_check "$OUT" "godot-dotnet-wasm|trim|$CORELIB|$GODOT_DOTNET_GODOTSHARP" "$APP" "$HOST_SRC" gates/_wasm_symbols.js; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 3/6 Linking the Emscripten side module =="
compile_dotnet_module_wasm "$OUT" "$SO"
bytes=$(wc -c < "$SO" | tr -d ' ')
printf '%s: %s bytes\n' "$SO" "$bytes"

echo "== 4/6 Building the MAIN_MODULE host (the engine's role, without the engine) =="
# -fwasm-exceptions on the HOST too: the side module imports its __cpp_exception tag and
# its __cxa_* runtime from here, so a main module without EH gives it nothing to unwind
# through. NODERAWFS lets dlopen name the .so by its real path instead of a preloaded FS.
em++ -std=c++17 -O2 -fwasm-exceptions -sMAIN_MODULE=1 -sNODERAWFS=1 -sALLOW_MEMORY_GROWTH=1 \
    "$HOST_SRC" -o "$OUT/dm_wasm_host.js"

echo "== 5/6 Asserting the export surface =="
exports=$(node gates/_wasm_symbols.js exports "$SO")
printf '%s\n' "$exports" | LC_ALL=C sed 's/^/  /'
for want in godotsharp_game_main_init dn2cpp_dm_interop_size; do
    grep -qx "func $want" <<<"$exports" \
        || { echo "FAIL: $SO does not export $want" >&2; exit 1; }
done
# Not merely present but REQUIRED: the loader calls __wasm_call_ctors after instantiating
# a side module, and without it every C++ static constructor in the runtime silently never
# runs. emscripten adds it (and the reloc appliers) to a side module's REQUIRED_EXPORTS on
# its own — assert that rather than trust it.
grep -qx "func __wasm_call_ctors" <<<"$exports" \
    || { echo "FAIL: $SO does not export __wasm_call_ctors — its static ctors would never run" >&2; exit 1; }
n_exports=$(printf '%s\n' "$exports" | grep -c .)
if [ "$n_exports" -gt 16 ]; then
    echo "FAIL: $SO exports $n_exports symbols — SIDE_MODULE=2 honours -sEXPORTED_FUNCTIONS," >&2
    echo "      so this many means the link fell back to SIDE_MODULE=1 (LINKABLE=1: no DCE," >&2
    echo "      export list ignored, unresolved symbols silently become imports)." >&2
    exit 1
fi
echo "export OK: $n_exports exports (entry + probe + the loader's own)"

echo "== 6/6 Asserting the import closure, the size, and the load =="
missing=$(node gates/_wasm_symbols.js unsatisfied "$SO" "$OUT/dm_wasm_host.wasm")
if [ -n "$missing" ]; then
    echo "FAIL: $SO imports symbols no main module can provide:" >&2
    printf '%s\n' "$missing" | LC_ALL=C sed 's/^/  /' >&2
    echo "      Each would dlopen and dlsym cleanly, then throw TypeError from the JS glue" >&2
    echo "      the first time it is CALLED. Define them for the Emscripten target." >&2
    exit 1
fi
echo "import OK: every function and GOT.mem/GOT.func import resolves against the pooled exports"

if [ "$bytes" -gt "$MAX_ARTIFACT_BYTES" ]; then
    echo "FAIL: $SO is $bytes bytes (ceiling $MAX_ARTIFACT_BYTES)" >&2
    exit 1
fi
echo "size OK: $bytes bytes (ceiling $MAX_ARTIFACT_BYTES)"

# The V8 per-function ceiling, now a HARD assertion rather than the gate_partial it was
# before --trim-reflection. __wasm_apply_data_relocs — the PIC data-relocation applier
# wasm-ld emits as ONE function with a store per pointer in static data — was 9,031,961
# bytes untrimmed, over the ceiling, so no V8 could instantiate the module and the load +
# entry below could not run. dn2cpp's reflection metadata was the pointer-dense 75% of it;
# --trim-reflection drops it and the function falls to ~4.9 MB. A regression that stopped
# the exporter passing the flag, or the transpiler honouring it, would put this function
# back over the ceiling and fail here — the one observable that sees it (every exit code
# upstream stays green on an untrimmed build).
read -r maxfn maxfn_name < <(node gates/_wasm_symbols.js maxfunc "$SO")
if [ "$maxfn" -ge "$V8_MAX_FUNCTION_SIZE" ]; then
    echo "FAIL: the drop-in's largest function is $maxfn bytes ($maxfn_name), at or over V8's" >&2
    echo "      hard per-function ceiling of $V8_MAX_FUNCTION_SIZE — no browser or node V8 will" >&2
    echo "      instantiate it. Expected --trim-reflection (passed in section 2) to shrink" >&2
    echo "      __wasm_apply_data_relocs by dropping the reflection member tables; a transpiler" >&2
    echo "      predating the flag ignores it silently and exits 0." >&2
    exit 1
fi
echo "largest wasm function: $maxfn bytes ($maxfn_name), $((V8_MAX_FUNCTION_SIZE - maxfn)) under V8's $V8_MAX_FUNCTION_SIZE ceiling"

echo "-- Run A: wrong interop size (the managed throw must unwind across the dlink boundary)"
runA=$(node "$OUT/dm_wasm_host.js" "$PWD/$SO" badsize 2>&1) || {
    printf '%s\n' "$runA" | LC_ALL=C sed 's/^/  /' >&2
    echo "FAIL: the host trapped instead of reporting the size mismatch" >&2
    exit 1
}
printf '%s\n' "$runA" | LC_ALL=C sed 's/^/  /'
grep -q "badsize OK" <<<"$runA" \
    || { echo "FAIL: Run A did not report the size mismatch cleanly" >&2; exit 1; }
grep -q "unhandled managed exception" <<<"$runA" \
    || { echo "FAIL: Run A did not raise a managed exception — was the size actually rejected?" >&2; exit 1; }
echo "Run A OK: the C++ exception unwound across the Emscripten dlink boundary"

echo "-- Run B: the self-reported size (fresh process — 'initialized' latches) drives the success path"
# 'ok' mode drives the SUCCESS path where Run A drove the error path: the entry accepts the
# size, receives the interop table and writes the managed callbacks — asserted below — and
# THEN registers the game's script classes. That registration touches GodotSharp types whose
# static constructors build StringNames by calling BACK into the engine through the interop
# table (DotnetModuleBackend's own note: "static constructors ... already call through it").
# This host is the engine's role WITH NO ENGINE IN IT, so it hands a ZEROED interop table
# (dm_wasm_host.cpp) — and that cctor call is then a null indirect call. Completing a full
# init here would mean the host emulating the subset of the ~180 godotsharp_* engine
# functions the startup cctors call, correctly typed (a wrong signature is a wasm trap too) —
# a real engine-stub layer, orthogonal to --trim-reflection and out of this gate's scope.
#
# So assert the progress this engineless host CAN observe, and DECLARE the engine-dependent
# tail via gate_expected_partial (_common.sh) — a permanent hole, covered for real elsewhere,
# which therefore passes under DN2CPP_REQUIRE_ALL=1 while still being named in the suite
# summary. Both halves of that claim: the reason is structural (an engine-stub layer is a
# non-goal of this gate, not a missing toolchain), and the tail it leaves dark is asserted by
# gates/build-and-run-godot-editor-export-web.sh (this same wasm drop-in, init completing in a
# real browser against the real engine fork) and gates/build-and-run-godot-dotnet-trim.sh (the
# same trimmed drop-in's full init, natively, against the real GodotSharp).
#
# This is NOT the old maxfunc opt-out returning by another door: that opt-out — the drop-in
# being too big for V8 to instantiate at all — is retired above as a HARD assertion, and Run A
# now runs and asserts for the first time.
set +e
runB=$(node "$OUT/dm_wasm_host.js" "$PWD/$SO" ok 2>&1)
set -e
printf '%s\n' "$runB" | LC_ALL=C sed 's/^/  /'
for marker in "interop received" "managed callbacks written"; do
    grep -q "$marker" <<<"$runB" \
        || { echo "FAIL: Run B did not reach '$marker' — the entry's success path broke before the engine boundary" >&2; exit 1; }
done
if grep -q "host: init OK" <<<"$runB"; then
    # A future host that stubs the engine functions would land here — keep the full green path.
    echo "Run B OK: the drop-in fully initialized"
else
    gate_expected_partial "the standalone wasm host structurally cannot complete godotsharp_game_main_init: past 'managed callbacks written' the entry registers the game's script classes, whose static constructors call BACK into the engine (StringName construction) through the interop table — and this host is the engine's role with no engine in it, so it hands a ZEROED table and that cctor call is a null indirect call. Permanent, not environmental: completing it would mean stubbing ~180 godotsharp_* functions at correct signatures (a wrong one is a wasm trap too), an engine-stub layer that is a declared non-goal of this gate. The load, the entry, the managed-exception unwind across the dlink boundary (Run A) and every artifact assertion above DID run and hold. The uncovered init tail IS asserted for real elsewhere: gates/build-and-run-godot-editor-export-web.sh runs the same wasm drop-in to a completed init in a real browser against the real engine fork, and gates/build-and-run-godot-dotnet-trim.sh runs the same trimmed drop-in's full init natively in the real engine against the real GodotSharp."
fi

gate_cache_commit
echo "OK"
