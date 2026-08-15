#!/usr/bin/env bash
# CRI ADX LE Web E2E gate: the CriWare GodotSampleApp — a real C# Godot game
# driving the proprietary CRI Atom audio engine through ~980 [DllImport]s on
# module "cri_atom" — exports through the dn2cpp mono-module Web lane and runs
# in a real Chrome.
#
# The staged project is the adjacent cri_adx_le_for_csharp sample, patched the
# way the Android sibling patches its copy, plus the three things only the Web
# flavor needs:
#
#   * The managed CriWare.CriAtomLE.dll must be the package's BROWSER flavor
#     (compiled under the `browser` define — InitializePlatform routes to
#     InitializeWEBAUDIO and pins ThreadModel.Single). The package selects
#     flavors strictly by RuntimeIdentifier machinery, and the Web export
#     publishes under the HOST RID by design — the machinery never fires — so
#     the stage swaps the PackageReference in both csproj files for a direct
#     <Reference> to the browser DLL: what the publish copies is then what the
#     transpile sees, with no RID resolution involved.
#   * An ExecuteMain pump. Under ThreadModel.Single the whole ADX server
#     (CriFs I/O, decode, voice dispatch) runs inside CriAtomEx.ExecuteMain(),
#     and the sample has no pump anywhere — desktop/Android run real server
#     threads. The injected AutoVerify autoload pumps it every _Process frame.
#
#     Verified against the jslib timer pump before trusting either alone: the
#     package's crinc_webaudio2.jslib carries its own "enhanced timer"
#     (WAASRJS_SetMainFunc + dynCall) which drives the native main function
#     that crinc_voice_webaudio2.c registers — and that object imports
#     criAtomEx_ExecuteMain, so once the WebAudio ASR output voice exists the
#     JS timer already drives FULL server processing at the ADX server
#     frequency. But that timer starts only inside WAASRJS_Setup (i.e. after
#     CriAtomCSharp.Initialize has created the output) and dies with
#     WAASRJS_Finalize; nothing pumps outside that window, and the webaudio v1
#     output (WAJS_*) has no self-timer at all — its WAJS_ExecuteMain is called
#     FROM native ExecuteMain. So the _Process pump is still required, and the
#     two coexist safely: the wasm program is single-threaded, so the timer's
#     dynCall and the frame's ExecuteMain strictly interleave on the browser
#     main loop, and CRI's Single-model contract is "call ExecuteMain at LEAST
#     at server_frequency" — extra calls are legal. (Unity WebGL ships exactly
#     this pairing: CriAtomServer's per-frame ExecuteMain beside the same
#     jslib.) The run asserts the jslib arm actually armed ("CriNcAsr:
#     Starting enhanced timer") and that neither driver faulted.
#   * The ACB loads WITHOUT its separate streaming AWB (awb: null). The wasm
#     engine drop's separate-AWB arm — LoadAcbFile's awb-path parameter, and
#     criAtomExAcb_AttachAwbFile alike — rejects the request inside its
#     streaming-open path BEFORE the custom IoInterface is ever consulted
#     (E2012082901 "Invalid parameter"; probed with raw fixed-pointer
#     P/Invokes to rule out argument marshalling, and with a correct explicit
#     awb name to rule out the res:// path->name derivation, which the drop
#     also mis-parses — E2015052126 "AWB file's name is invalid"). The same
#     IoInterface serves criAtomAwb_LoadToc and the full 6.2 MB ACB read
#     without a hitch, so the io path itself is proven; the arm is the wasm
#     drop's own limitation, and CRI's first-party wasm-facing samples (Unity
#     WebGL, Blazor) both load with awb: null too. On-memory cues — cue 0
#     included, which is what the run plays — are unaffected.
#
# The side module deliberately links NO CRI archive: both archives are
# non-PIC, so they cannot ride any Emscripten side module — the whole cri_*
# P/Invoke surface stays dynamic imports that only the CRI-enabled Web export
# template's MAIN module satisfies (whole-archive + jslibs ×3, baked by
# CRI=1 gates/setup-godot-fork-web.sh). That closure is asserted statically,
# because emscripten's dlopen never fails on a missing symbol — the first call
# dies naming a function index. The Godot audio driver stays Dummy in the run:
# CRI's Web Audio output is its own jslib stack, independent of the engine's
# audio driver, and the Dummy driver keeps the engine-shutdown race the stock
# export-web gate documents out of the picture.
#
# Requires the CriWare.CriAtomLE NuGet package + sample repository (cri_*
# helpers in _common.sh), the fork editor cache, the CRI Web template and a
# python new enough for emcc to launch; each absence is a gate_skip. A missing
# Chrome degrades the browser section to a PARTIAL after every build artifact
# was asserted.
source "$(dirname "$0")/_common.sh"
source "$(dirname "$0")/_godot_fork.sh"

OUT=gates/out-cri-web
PROJECT_NAME=GodotSampleApp

# V8's hard per-function ceiling, a compiled-in constant (Chrome enforces it too).
V8_MAX_FUNCTION_SIZE=7654321

godot_fork_preflight
if [ ! -f "$FORK_ROOT/web_template_cri.zip" ]; then
    gate_skip "no CRI Web template at $FORK_ROOT/web_template_cri.zip — run CRI=1 gates/setup-godot-fork-web.sh"
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
# installs into the fork editor below.
godot_fork_emcc_python_check "$PATH" "${EMSDK_PYTHON:-}"
# The second is the interpreter THIS SCRIPT runs, to serve the export over HTTP —
# a different interpreter, and no floor. resolve_python probes by RUNNING one, the
# only thing that tells a real interpreter from the app-execution-alias stub a
# stock Windows install answers `python3` with.
py="$(resolve_python)" || gate_skip "no working Python 3 interpreter (needed to serve the export over HTTP)"
CRI_MANAGED_BROWSER="$(cri_managed_dll browser)" \
    || gate_skip "CriWare.CriAtomLE package (browser flavor) not present — see cri_nuget_root in gates/_common.sh"
CRI_SAMPLE_ROOT="$(cri_sample_root)" \
    || gate_skip "cri_adx_le_for_csharp sample repository not present"
SAMPLE_SRC="$CRI_SAMPLE_ROOT/samples/GodotSampleApp"
EXT_SRC="$CRI_SAMPLE_ROOT/samples/SampleExtensions"
[ -d "$SAMPLE_SRC" ] && [ -d "$EXT_SRC" ] \
    || gate_skip "GodotSampleApp/SampleExtensions not found under $CRI_SAMPLE_ROOT"

echo "== 1/6 Fork pin + interop-ABI tripwire =="
godot_fork_pin_abi_check

# The CRI template is the second engine in this gate, and the flavor probes in
# its bake say nothing about its VERSION: the preset points custom_template at
# it and Godot validates that path with FileAccess::exists alone
# (platform/web/export/export_plugin.cpp), so a zip baked before a re-pin
# exports, boots in the browser and matches every expectation here while
# shipping the older engine, which a re-pin has already produced once.
# Live, before gate_cache_check, for the reason the preflight is:
# `tmpl=` in the key fingerprints the stale zip itself, so a warm key replays a
# green *because* nothing changed.
godot_fork_template_check "$FORK_ROOT/web_template_cri.zip" "CRI Web export template" \
    "CRI=1 FORCE=1 gates/setup-godot-fork-web.sh"

echo "== 2/6 Staging the sample project (Web) =="
# Copied, never exported in place: the source tree is a foreign repository this
# gate must not perturb, and every patch below applies to the copy alone.
STAGE="$OUT/stage"
PROJ="$STAGE/GodotSampleApp"
rm -rf "$STAGE"
mkdir -p "$STAGE"
cp -R "$SAMPLE_SRC" "$PROJ"
cp -R "$EXT_SRC" "$STAGE/SampleExtensions"
rm -rf "$PROJ/.godot" "$PROJ/bin" "$PROJ/obj" \
       "$STAGE/SampleExtensions/bin" "$STAGE/SampleExtensions/obj"

# The CriWare PackageReference becomes a direct Reference to the browser-flavor
# DLL — see the header for why the publish would otherwise ship the desktop
# flavor. The Sdk pin is rewritten onto whatever the fork feed carries: this is a
# THIRD-PARTY project pinned to the Godot it shipped against (4.2.1 / net7.0 on
# upstream master today), and nothing makes that track ours, so asserting either
# without first writing it reports a pristine clone as a failure. The TFM has to
# move with the Sdk, not merely alongside it: GodotSharp 4.7.1 targets net8.0, so
# a project left at the sample's net7.0 fails restore before anything is built.
# The pattern matches the ATTRIBUTE-LESS element only, leaving the sample's
# `<TargetFramework Condition="...ios...">` line alone.
#
# The HintPath below is written into a csproj, which MSBUILD reads, so it takes
# the same native-path conversion the nuget feed does — and this is the site
# where getting it wrong is quietest. An MSYS /c/... HintPath does not fail: the
# reference resolves to NOTHING, the restore is clean, and the build dies much
# later on every CriWare type being absent, naming the SDK's own API surface
# rather than a path. The grep asserts below cannot catch it either — they check
# the string that was written, which is exactly the wrong one.
SDK_VERSION="$(godot_nuget_sdk_version "$FORK_ROOT/nuget")"
CRI_MANAGED_BROWSER_NATIVE="$(godot_fork_native_path "$CRI_MANAGED_BROWSER")"
csproj_tmp="$(mktemp)"
sed -e "s|Sdk=\"Godot.NET.Sdk/[^\"]*\"|Sdk=\"Godot.NET.Sdk/$SDK_VERSION\"|" \
    -e 's|<TargetFramework>net[0-9][0-9.]*</TargetFramework>|<TargetFramework>net8.0</TargetFramework>|' \
    -e "s|<PackageReference Include=\"CriWare.CriAtomLE\" Version=\"[^\"]*\" */>|<Reference Include=\"CriWare.CriAtomLE\"><HintPath>$CRI_MANAGED_BROWSER_NATIVE</HintPath></Reference>|" \
    "$PROJ/$PROJECT_NAME.csproj" > "$csproj_tmp"
mv "$csproj_tmp" "$PROJ/$PROJECT_NAME.csproj"
grep -q "Godot.NET.Sdk/$SDK_VERSION" "$PROJ/$PROJECT_NAME.csproj" \
    || { echo "FAIL: could not re-pin the staged project onto the fork feed's Godot.NET.Sdk $SDK_VERSION" >&2; exit 1; }
grep -q "<TargetFramework>net8.0</TargetFramework>" "$PROJ/$PROJECT_NAME.csproj" \
    || { echo "FAIL: could not retarget the staged project at net8.0" >&2; exit 1; }
grep -qF "$CRI_MANAGED_BROWSER_NATIVE" "$PROJ/$PROJECT_NAME.csproj" \
    || { echo "FAIL: could not swap the app's CriWare reference to the browser-flavor DLL" >&2; exit 1; }

# SampleExtensions multi-targets netstandard2.1;net6.0 off the package. A
# direct reference to the net7.0 browser DLL cannot feed either, and leaving
# the PackageReference would put a SECOND CriWare.CriAtomLE (the package's
# desktop compile surface) on the app's transitive closure beside the direct
# one — so it retargets to a single net8.0 (matching the app) and takes the
# same reference swap.
ext_tmp="$(mktemp)"
sed -e 's|<TargetFrameworks>netstandard2.1;net6.0</TargetFrameworks>|<TargetFramework>net8.0</TargetFramework>|' \
    -e "s|<PackageReference Include=\"CriWare.CriAtomLE\" Version=\"[^\"]*\" */>|<Reference Include=\"CriWare.CriAtomLE\"><HintPath>$CRI_MANAGED_BROWSER_NATIVE</HintPath></Reference>|" \
    "$STAGE/SampleExtensions/SampleExtensions.csproj" > "$ext_tmp"
mv "$ext_tmp" "$STAGE/SampleExtensions/SampleExtensions.csproj"
grep -q "<TargetFramework>net8.0</TargetFramework>" "$STAGE/SampleExtensions/SampleExtensions.csproj" \
    || { echo "FAIL: could not retarget SampleExtensions at net8.0" >&2; exit 1; }
grep -qF "$CRI_MANAGED_BROWSER_NATIVE" "$STAGE/SampleExtensions/SampleExtensions.csproj" \
    || { echo "FAIL: could not swap SampleExtensions' CriWare reference to the browser-flavor DLL" >&2; exit 1; }

# The Web-shape ACB load: awb becomes null (see the header's third bullet for
# the full evidence trail — the wasm drop's separate-AWB arm fails before the
# IoInterface is consulted, and CRI's own wasm samples ship exactly this
# shape). The awbPath parameter stays in the signature so the call sites are
# untouched.
acbload_tmp="$(mktemp)"
sed 's|currentAcb = CriAtomExAcb.LoadAcbFile(null, path, null, awbPath);|currentAcb = CriAtomExAcb.LoadAcbFile(null, path, null, null);|' \
    "$PROJ/script/CriAtomPreviewPlayer.cs" > "$acbload_tmp"
mv "$acbload_tmp" "$PROJ/script/CriAtomPreviewPlayer.cs"
grep -qF "LoadAcbFile(null, path, null, null)" "$PROJ/script/CriAtomPreviewPlayer.cs" \
    || { echo "FAIL: could not patch the Web-shape ACB load (awb: null)" >&2; exit 1; }

cat > "$PROJ/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<!-- GENERATED by gates/build-and-run-cri-web.sh — do not commit.
     Points the Godot.NET.Sdk resolver + restore at the fork's local feed. The
     feed path is native (godot_fork_native_path): MSYS converts a path it hands
     a native program as an ARGUMENT, never one written into a file, so an MSYS
     /c/... here reaches NuGet as C:\c\Users\... and restore fails on a feed
     that "does not exist". -->
<configuration>
  <packageSources>
    <clear />
    <add key="dn2cpp-godot-fork-local" value="$(godot_fork_native_path "$FORK_ROOT/nuget")" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

# The unattended run: the sample's own SampleScript._Ready wires the Godot
# FileAccess IoInterface and loads the ACF/ACB from res:// (out of the pck);
# this autoload then drives the same preview player the UI buttons drive and
# prints markers the browser section greps out of the console stream. The
# playback status poll is what stands in for audibility: Playing means the
# voice pool allocated a voice and the WebAudio output stream is being rendered
# into, which no headless assert can hear. The _Process ExecuteMain pump is the
# Web-only addition — the header states the full verdict on it versus the
# package's own jslib timer pump.
cat > "$PROJ/script/AutoVerify.cs" <<'EOF'
using Godot;
using System;
using System.Linq;
using CriWare;

// GENERATED by gates/build-and-run-cri-web.sh — do not commit.
// Unattended E2E driver: prints CRI_E2E_* markers for the gate to assert from
// the browser console (GD.Print reaches console.log through the harness's
// onPrint), then quits the game so the page reports its exit deterministically.
public partial class AutoVerify : Node
{
    private IDisposable _errorHandler;
    private bool _errored;
    private bool _started;
    private bool _sawPlaying;
    private int _frames;
    private int _playFrames;
    private CriAtomExPlayback _playback;

    public override void _Ready()
    {
        _errorHandler = CriBaseCSharp.ErrorCallback.RegisterListener(msg =>
        {
            // CRI message ids carry their severity as the first letter
            // ("W..." vs "E..."): a warning must not fail the run.
            if (msg != null && msg.StartsWith("W", StringComparison.Ordinal))
            {
                GD.Print("CRI_E2E_WARN " + msg);
                return;
            }
            _errored = true;
            GD.Print("CRI_E2E_ERROR " + msg);
        }, false);
        GD.Print("CRI_E2E_BOOT");
    }

    public override void _Process(double delta)
    {
        _frames++;
        try
        {
            // The ThreadModel.Single pump: on the Web the whole ADX server
            // (CriFs I/O, decode, voice dispatch) runs inside ExecuteMain.
            // The library initializes in SampleScript._Ready (before the first
            // frame); the guard keeps a failed initialization from turning
            // every subsequent frame into an uninitialized-call error storm.
            if (CriAtomEx.IsInitialized())
                CriAtomEx.ExecuteMain();
            Drive();
        }
        catch (Exception e)
        {
            GD.Print("CRI_E2E_EXCEPTION " + e);
            GD.Print("CRI_E2E_DONE");
            GetTree().Quit(1);
        }
    }

    private void Drive()
    {
        if (!_started)
        {
            // The main scene's SampleScript._Ready has run long before frame
            // 30; by then the ACF/ACB are loaded through the Godot FileAccess
            // IoInterface and the preview player is initialized.
            if (_frames < 30)
                return;
            _started = true;
            GD.Print("CRI_E2E_VERSION " + CriAtom.GetVersionString());
            var infos = CriAtomPreviewPlayer.Instance.GetCurrentCueInfoList().ToArray();
            GD.Print("CRI_E2E_CUES " + infos.Length);
            int cueId = infos.Length > 0 ? infos[0].id : 0;
            _playback = CriAtomPreviewPlayer.Instance.Play(cueId);
            GD.Print("CRI_E2E_PLAY_STARTED cue=" + cueId);
            return;
        }
        _playFrames++;
        var status = _playback.GetStatus();
        if (!_sawPlaying && status == CriAtomExPlayback.Status.Playing)
        {
            _sawPlaying = true;
            GD.Print("CRI_E2E_PLAYING");
        }
        // ~3 s at 60 fps: enough for the streaming voice to reach Playing and
        // render into the WebAudio graph, short enough for an automated run.
        if (_playFrames >= 180)
        {
            GD.Print("CRI_E2E_RESULT playing=" + _sawPlaying + " status=" + status
                + " errors=" + _errored);
            GD.Print("CRI_E2E_DONE");
            GetTree().Quit();
        }
    }

    public override void _ExitTree()
    {
        _errorHandler?.Dispose();
        _errorHandler = null;
    }
}
EOF

# Registered as an autoload so no scene edit is needed; the sample's own
# MainScene stays the entry scene. The [dotnet] append re-opens the sample's
# existing section (Godot's ConfigFile merges repeated headers) to add the fork
# exporter's knob: extra_transpile_args is how --pinvoke-module cri_atom
# reaches the editor-driven transpile. No link knobs, on purpose — the side
# module must NOT link the (non-PIC) CRI archives; the template's main module
# carries them (see the header).
cat >> "$PROJ/project.godot" <<EOF

[autoload]

AutoVerify="*res://script/AutoVerify.cs"

[dotnet]

dn2cpp/extra_transpile_args=PackedStringArray("--pinvoke-module", "cri_atom")
EOF

# The export preset: the Web dlink preset against dotnet/export_backend = 2
# (dn2cpp), modeled on the editor-export Web gate's, with the CRI template and
# the data include_filter (the ACF/ACB/AWB must reach the pck). Only a "dlink"
# template has a working dlopen, and thread_support stays off — the drop-in is
# built without -pthread, and Emscripten will not load a non-pthread side
# module into a pthread main module.
cat > "$PROJ/export_presets.cfg" <<EOF
[preset.0]

name="dn2cpp-web"
platform="Web"
runnable=true
advanced_options=false
dedicated_server=false
custom_features=""
export_filter="all_resources"
include_filter="*.acb,*.acf,*.awb"
exclude_filter=""
export_path=""
patches=PackedStringArray()
encryption_include_filters=""
encryption_exclude_filters=""
seed=0
encrypt_pck=false
encrypt_directory=false
script_export_mode=2

[preset.0.options]

custom_template/debug=""
custom_template/release="$(godot_fork_native_path "$FORK_ROOT/web_template_cri.zip")"
variant/extensions_support=true
variant/thread_support=false
vram_texture_compression/for_desktop=true
vram_texture_compression/for_mobile=false
html/export_icon=false
html/custom_html_shell=""
html/head_include=""
html/canvas_resize_policy=2
html/focus_canvas_on_start=true
html/experimental_virtual_keyboard=false
progressive_web_app/enabled=false
dotnet/include_scripts_content=false
dotnet/include_debug_symbols=false
dotnet/embed_build_outputs=false
dotnet/export_backend=2
EOF

echo "== 3/6 Transpiling with --pinvoke-module cri_atom (browser-flavor managed DLL) =="
build_proj src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj
dotnet build "$PROJ/$PROJECT_NAME.csproj" -c ExportRelease --nologo -v q
APPDIR="$PROJ/.godot/mono/temp/bin/ExportRelease"
[ -f "$APPDIR/$PROJECT_NAME.dll" ] || { echo "FAIL: not built: $APPDIR/$PROJECT_NAME.dll" >&2; exit 1; }

# The reference swap must have carried the browser flavor into the build
# output byte-for-byte — this is the DLL both the direct transpile below and
# the editor-driven export publish will see.
cmp -s "$APPDIR/CriWare.CriAtomLE.dll" "$CRI_MANAGED_BROWSER" \
    || { echo "FAIL: the built output's CriWare.CriAtomLE.dll is not the browser flavor" >&2; exit 1; }

# Pin net10.0 (the JSON/measure gates' rule) and reference the fork editor's
# own GodotSharp, exactly as the fork's Dn2CppExporter does — including WHICH
# framework flavour: the exporter picks it off the export target
# (Dn2CppToolchain.CoreLibRefFor), so this must too, or the manifest asserted
# below describes a program the export does not build. On a POSIX host the two
# spellings resolve to the same file.
corelib=$(locate_corelib_cross_posix net10)
GEN="$OUT/gen"
rm -rf "$GEN"
invoke_cli "$APPDIR/$PROJECT_NAME.dll" --dotnet-module \
    -r "$corelib" \
    -r "$FORK_GODOTSHARP/Api/Release/GodotSharp.dll" \
    -r "$APPDIR/CriWare.CriAtomLE.dll" \
    -r "$APPDIR/SampleExtensions.dll" \
    --auto-ref \
    --pinvoke-module cri_atom \
    -o "$GEN"

# The opt-in's manifest half: the module reaches pinvoke-libs.txt. On this
# lane the CMake side then deliberately IGNORES it (an Emscripten side module
# must not link the archive — runtime/CMakeLists.txt), but the manifest is
# still the record of what the main module must supply.
[ -f "$GEN/pinvoke-libs.txt" ] \
    || { echo "FAIL: the transpile emitted no pinvoke-libs.txt" >&2; exit 1; }
printf 'cri_atom\n' | diff -u - <(strip_cr_win_file "$GEN/pinvoke-libs.txt") \
    || { echo "FAIL: pinvoke-libs.txt is not exactly 'cri_atom'" >&2; exit 1; }
echo "transpile OK: pinvoke-libs.txt = cri_atom"

# Chrome probe, resolved here because it is a cache-key term: a browser
# arriving later must move the key and force a live re-run of the run section
# it unlocks. It ASKS the aid that will do the launching (`--probe`) rather than
# mirroring its candidate list, because a mirror is what drifted: this was a
# hand-copied list with no Windows arm long after gates/_run_in_chrome.js grew
# one, so on a Windows box with Chrome installed the key recorded `chrome=no`
# while the run section would have launched it — and the gate replayed a cached
# partial rather than running. One list, one answer, one place to extend.
CHROME_OK=no
node gates/_run_in_chrome.js --probe >/dev/null 2>&1 && CHROME_OK=yes

# Cache: keyed on the transpile surface just emitted into $GEN plus every input
# the later sections consume — the fork context (pins, editor, GodotTools
# content), the CRI template and the emcc stamps on both sides of the dlink
# boundary (the template's bake and the emcc that will compile the side
# module), the CRI package artifacts, the self-hosted CLI the export toolchain
# packages, the runtime sources the drop-in compiles against, the sample trees,
# and the two node aids whose verdicts the assert sections trust. A missing
# Chrome is recorded as partial and re-run live under DN2CPP_REQUIRE_ALL=1,
# like every gate.
if gate_cache_check "$GEN" \
    "cri-web|$(godot_fork_ctx)|tmpl=$(file_sig "$FORK_ROOT/web_template_cri.zip")|tmplemcc=$(file_text "$FORK_ROOT/web_emcc_cri.txt")|emcc=$(or_none "$(first_line "$(emcc --version 2>/dev/null)")")|chrome=$CHROME_OK|cri=$CRI_ATOM_VERSION|crimanaged=$(file_sig "$CRI_MANAGED_BROWSER")" \
    "$SELFHOST_BIN" \
    dist/package-toolchain.sh \
    runtime \
    "$SAMPLE_SRC" \
    "$EXT_SRC" \
    gates/_wasm_symbols.js \
    gates/_run_in_chrome.js; then
    { gate_cache_hit_msg; exit 0; }
fi

echo "== 4/6 Exporting to Web (fork editor, dotnet/export_backend = dn2cpp) =="
stage_editor_toolchain "$FORK_GODOTSHARP" "$SELFHOST_BIN" "$OUT/package.log"

# The first import may abort in editor doc-gen teardown (Godot headless bug,
# harmless); tolerate a non-zero exit.
run_with_watchdog 600 "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --import >"$OUT/import.log" 2>&1 || true

WEBDIR="$PWD/$OUT/web"
rm -rf "$WEBDIR"
mkdir -p "$WEBDIR"
if ! godot_export_step 3600 "$OUT/export.log" "$WEBDIR/index.html" \
    "$FORK_EDITOR" --headless \
    --path "$PWD/$PROJ" --export-release dn2cpp-web "$WEBDIR/index.html"; then
    echo "FAIL: --export-release failed (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
# An export plugin's Error fails the export, so godot_export_step above already
# asserted the exit code. This grep pins the MESSAGE: which plugin objected.
if grep -q "ERROR: Export .NET Project" "$OUT/export.log"; then
    echo "FAIL: the C# export plugin reported an error (see below)" >&2
    cat "$OUT/export.log" >&2
    exit 1
fi
for marker in "dn2cpp: extra transpile args (project setting): --pinvoke-module cri_atom" \
              "dn2cpp: transpiling" "dn2cpp: compiling the drop-in library" "dn2cpp: staged"; do
    grep -qF "$marker" "$OUT/export.log" \
        || { echo "FAIL: marker \"$marker\" missing from the export log" >&2
             cat "$OUT/export.log" >&2; exit 1; }
done

# The publish behind the export must also have carried the browser flavor —
# the host-RID publish resolves the direct Reference, never the package's RID
# machinery, and this is the byte-level proof.
PUBLISHED_CRI="$(find "$APPDIR" -mindepth 2 -name CriWare.CriAtomLE.dll)"
PUBLISHED_CRI="${PUBLISHED_CRI%%$'\n'*}"
[ -n "$PUBLISHED_CRI" ] \
    || { echo "FAIL: no published CriWare.CriAtomLE.dll under $APPDIR/<rid>/" >&2; exit 1; }
cmp -s "$PUBLISHED_CRI" "$CRI_MANAGED_BROWSER" \
    || { echo "FAIL: the publish did not carry the browser-flavor CriWare.CriAtomLE.dll" >&2
         echo "      published: $PUBLISHED_CRI" >&2; exit 1; }
echo "publish flavor OK: browser CriWare.CriAtomLE.dll"

echo "== 5/6 Asserting the artifacts =="
DROPIN="$WEBDIR/$PROJECT_NAME.so"
# (a) the drop-in is beside the HTML, under the exact name the engine will
#     dlopen, and it exports the entry point GDMono resolves
[ -f "$DROPIN" ] || {
    echo "FAIL: no drop-in beside index.html at $DROPIN" >&2; ls -la "$WEBDIR" >&2; exit 1; }
node gates/_wasm_symbols.js exports "$DROPIN" > "$OUT/dropin-exports.txt"
grep -qx "func godotsharp_game_main_init" "$OUT/dropin-exports.txt" || {
    echo "FAIL: $DROPIN does not export godotsharp_game_main_init" >&2
    head -20 "$OUT/dropin-exports.txt" >&2; exit 1; }
# (b) the generated HTML tells the loader to preload it, and the template was a
#     dlink build (only a dlink template has a dlopen that is not a NULL stub)
grep -q "\"gdextensionLibs\":\[\"$PROJECT_NAME.so\"\]" "$WEBDIR/index.html" || {
    echo "FAIL: index.html does not list $PROJECT_NAME.so in gdextensionLibs" >&2; exit 1; }
[ -f "$WEBDIR/index.side.wasm" ] || {
    echo "FAIL: no index.side.wasm — the export used a non-dlink template" >&2; exit 1; }
# (c) no function exceeds V8's per-function ceiling (the stale-toolchain /
#     missing-trim oracle — see the stock export-web gate for the full story)
read -r maxfn maxfn_name < <(node gates/_wasm_symbols.js maxfunc "$DROPIN")
if [ "$maxfn" -ge "$V8_MAX_FUNCTION_SIZE" ]; then
    echo "FAIL: the drop-in's largest function is $maxfn bytes ($maxfn_name), at or over V8's" >&2
    echo "      hard per-function ceiling of $V8_MAX_FUNCTION_SIZE — no browser will instantiate it." >&2
    exit 1
fi
echo "drop-in: $(wc -c < "$DROPIN" | tr -d ' ') bytes; largest function $maxfn bytes ($maxfn_name)"

# (d) the CRI closure. The side module must import the cri_* P/Invoke surface
#     (vacuity guard: tree-shaking silently emptying the demand would blind the
#     next assert), no WASAPI-named import may appear — the browser-flavor DLL
#     still CARRIES the desktop WASAPI P/Invoke wrappers, and reachability must
#     have left every one of them behind, or wasm-ld got handed a phantom
#     extern no module on the page defines — and every import must resolve
#     against the exported page's own main module + glue, cri_* included
#     (emscripten's dlopen never fails on a missing symbol; the first call
#     dies naming a function index, so the closure is asserted statically).
node gates/_wasm_symbols.js imports "$DROPIN" > "$OUT/dropin-imports.txt"
grep -qE '^func env\.cri' "$OUT/dropin-imports.txt" || {
    echo "FAIL: $DROPIN imports no cri_* symbol — the CRI closure assert would be vacuous" >&2
    exit 1
}
if grep -qi wasapi "$OUT/dropin-imports.txt"; then
    echo "FAIL: the drop-in imports WASAPI-named symbols — the desktop audio path was not tree-shaken:" >&2
    grep -i wasapi "$OUT/dropin-imports.txt" | LC_ALL=C sed 's/^/  /' >&2
    exit 1
fi
unsat="$(node gates/_wasm_symbols.js unsatisfied "$DROPIN" "$WEBDIR/index.wasm" "$WEBDIR/index.js")"
if [ -n "$unsat" ]; then
    echo "FAIL: the exported main module + glue do not satisfy every drop-in import:" >&2
    printf '%s\n' "$unsat" | LC_ALL=C sed 's/^/  /' >&2
    echo "      Each would dlopen and dlsym cleanly in the player's browser, then die on" >&2
    echo "      first CALL naming a wasm function index. If cri_* symbols are listed, the" >&2
    echo "      template predates the main-module CRI link — rebuild with" >&2
    echo "      FORCE=1 CRI=1 gates/setup-godot-fork-web.sh." >&2
    exit 1
fi
echo "import closure OK: $(grep -c '^func env\.cri' "$OUT/dropin-imports.txt") cri_* imports, all satisfied by the main module; no WASAPI import"

# (e) the game data reached the pck (the include_filter's job; a missing ACF
#     would otherwise only surface in the browser section, which can be skipped)
for data in DemoProj.acf DemoProj.acb DemoProj.awb; do
    grep -qa "$data" "$WEBDIR/index.pck" \
        || { echo "FAIL: the pck does not carry $data" >&2; exit 1; }
done
echo "pck OK: ACF/ACB/AWB present"

echo "== 6/6 Running the exported game in Chrome =="
# A real Chrome (SwiftShader-rasterized — see gates/_run_in_chrome.js), because
# node cannot host a Godot web build and, unlike node, Chrome has a real
# AudioContext: CRI's own WebAudio stack (crinc_webaudio*.jslib in the
# template's main module) initializes for real. The runner passes
# --autoplay-policy=no-user-gesture-required, so the context is born running
# rather than suspended-until-gesture. The ENGINE's audio driver stays Dummy —
# CRI's output is independent of it, and the Dummy driver keeps the
# engine-shutdown worklet race (documented in the stock export-web gate) out of
# a probe that quits seconds after boot.
cat > "$WEBDIR/harness.html" <<'EOF'
<!DOCTYPE html>
<html>
<head><meta charset="utf-8"><title>dn2cpp CRI web gate</title></head>
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
echo "serving $WEBDIR on 127.0.0.1:$PORT"

# The AutoVerify sequence quits itself after ~210 frames; SwiftShader renders
# slowly, so the ceiling is generous. HARNESS_EXIT is the settle marker.
LOG="$OUT/run.log"
rc=0
node gates/_run_in_chrome.js "http://127.0.0.1:$PORT/harness.html" 300000 "HARNESS_EXIT" \
    >"$LOG" 2>&1 || rc=$?
kill "$SRV_PID" 2>/dev/null || true
cat "$LOG"
if [ "$rc" -eq 77 ]; then
    # gate_partial does not exit: the assertions below have nothing to read, so
    # they must not run.
    gate_partial "no full Chrome — the export was asserted but never run"
else
    if [ "$rc" -ne 0 ]; then
        echo "FAIL: the browser runner failed with $rc" >&2
        exit 1
    fi
    # Positive markers: engine boot; the drop-in's C# alive (BOOT can only come
    # from the transpiled game); CRI initialized on the WASM flavor at the
    # pinned version; the package's own jslib timer pump armed (the enhanced
    # timer only starts once the WebAudio ASR output voice exists — this is
    # the "audio pipeline initialized" evidence, and its wording is pinned to
    # the SDK drop exactly like the smoke gate's frozen banner); the cue
    # reaching Playing — the stand-in for audibility — and the summary line
    # with the playing=True / errors=False verdict baked in; a clean quit.
    for marker in \
        "Godot Engine v" \
        "CRI_E2E_BOOT" \
        "CRI_E2E_VERSION CRI Atom(LE)/WASM Ver.$CRI_ATOM_VERSION" \
        "CriNcAsr: Starting enhanced timer" \
        "CRI_E2E_PLAY_STARTED" \
        "CRI_E2E_PLAYING" \
        "CRI_E2E_RESULT playing=True" \
        "CRI_E2E_DONE" \
        "HARNESS_EXIT code=0"; do
        grep -qF "$marker" "$LOG" || { echo "FAIL: missing marker: $marker" >&2; exit 1; }
    done
    # The ACF/ACB really loaded through the Godot-FileAccess IoInterface: the
    # cue list read out of the loaded ACB must be non-empty.
    # No `| head -1`: head quits after its line, awk behind it dies of SIGPIPE
    # and `pipefail` reports that as the pipeline's status. Take the first line
    # off the captured string instead.
    _cues_all="$(grep -o 'CRI_E2E_CUES [0-9]*' "$LOG" | awk '{print $2}' || true)"
    ncues="${_cues_all%%$'\n'*}"
    [ -n "$ncues" ] && [ "$ncues" -gt 0 ] \
        || { echo "FAIL: CRI_E2E_CUES reports no cues (ACF/ACB load did not produce a cue list)" >&2; exit 1; }
    grep -q "errors=False" <<<"$(grep -F "CRI_E2E_RESULT" "$LOG" || true)" \
        || { echo "FAIL: the CRI error callback fired during the run (CRI_E2E_RESULT errors!=False)" >&2; exit 1; }
    for once in "CRI_E2E_BOOT" "CRI_E2E_VERSION" "CRI_E2E_CUES" "CRI_E2E_PLAY_STARTED" \
        "CRI_E2E_PLAYING" "CRI_E2E_RESULT" "CRI_E2E_DONE"; do
        n="$(grep -c "$once" "$LOG" || true)"
        [ "$n" -eq 1 ] || { echo "FAIL: marker $once appeared $n times (expected exactly 1)" >&2; exit 1; }
    done
    # Negative markers: no CRI error callback, no managed exception, no
    # mono-module init failure, no wasm trap or page error, no CRI JS-side
    # fault (a missing AudioContext or a throwing timer callback logs its own
    # fixed line), no emscripten abort.
    for bad in "CRI_E2E_ERROR" "CRI_E2E_EXCEPTION" \
        "GodotPlugins initialization failed" \
        "Failed to load hostfxr" \
        "Unable to find the .NET assemblies directory" \
        "HARNESS_FAILED" \
        "function signature mismatch" \
        "__PAGE_EXCEPTION" \
        "SCRIPT ERROR" \
        "AudioContext is not exist" \
        "CriNcAsr: Error in main function execution" \
        "Aborted("; do
        if grep -qF "$bad" "$LOG"; then
            echo "FAIL: the browser run reported \"$bad\"" >&2
            exit 1
        fi
    done
    echo "browser run OK: booted, CRI initialized on WebAudio, cue reached Playing, no errors"
fi

# Tripwire: a concurrent suite re-staging the shared toolchain mid-run must
# surface as this self-explaining FAIL, not as a cryptic include error inside
# the export above.
assert_editor_toolchain_current "$FORK_GODOTSHARP"
gate_cache_commit
echo "OK"
