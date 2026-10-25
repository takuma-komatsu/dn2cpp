#!/usr/bin/env bash
# Pending finalizers run before Godot's shutdown tracker disposes interop helpers.
# Sourced by the scripted-node gate after its unchanged normal-scene checks.
godot_dotnet_shutdown_gate() {
    local out="$1" arch="$2" library="$3" os rid_arch rid
    local root="$PWD/$out/shutdown" python
    python=$(resolve_python)
    case "$DN2CPP_OS" in
        macos) os=osx ;;
        windows) os=win ;;
        linux) os=linux ;;
        *) echo "FAIL: unsupported shutdown probe host: $DN2CPP_OS" >&2; return 1 ;;
    esac
    case "$arch" in
        arm64) rid_arch=arm64 ;;
        x86_64) rid_arch=x64 ;;
        *) echo "FAIL: unsupported shutdown probe architecture: $arch" >&2; return 1 ;;
    esac
    rid="$os-$rid_arch"
    mkdir -p "$root/input" "$root/native" "$root/clr"
    local data="data_DotnetSample_${GODOT_DOTNET_PLATFORM}_$arch"
    mkdir -p "$root/native/$data" "$root/clr/$data"
    # Preserve the exact IL already used by godot_dotnet_transpile. Publishing
    # supplies the CLR host and deps; its RID-specific rebuild is not our oracle.
    cp "$GODOT_DOTNET_SAMPLE_DIR/.godot/mono/temp/bin/ExportRelease/DotnetSample.dll" "$root/input/"
    cp "$GODOT_DOTNET_GODOTSHARP" "$root/input/"
    dotnet publish "$GODOT_DOTNET_SAMPLE_DIR/DotnetSample.csproj" -c ExportRelease \
        -r "$rid" --self-contained true -p:PublishTrimmed=false -p:PublishAot=false \
        -p:GodotTargetPlatform="$DN2CPP_OS" -o "$root/clr/$data" --nologo \
        >"$root/publish.log" 2>&1 || { cat "$root/publish.log" >&2; return 1; }
    for assembly in DotnetSample GodotSharp; do
        cp "$root/input/$assembly.dll" "$root/clr/$data/$assembly.dll"
        cmp "$root/input/$assembly.dll" "$root/clr/$data/$assembly.dll"
    done
    # The CLR uses the sample's net8 self-contained runtime. Native emission
    # uses the net10 CoreLib selected by godot_dotnet_transpile.
    $python - "$root" "$data" <<'PY'
import hashlib, json, sys
from pathlib import Path
root, data = Path(sys.argv[1]), sys.argv[2]
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest()
identity = {name: sha(root / 'input' / (name + '.dll')) for name in ('DotnetSample', 'GodotSharp')}
config = json.loads((root / 'clr' / data / 'DotnetSample.runtimeconfig.json').read_text())
identity['clr_runtime'] = config['runtimeOptions']
(root / 'input-identity.json').write_text(json.dumps(identity, indent=2) + '\n')
PY
    if ! godot_export_step 300 "$root/export.log" "$root/native/godot.pck" \
        "$GODOT_DOTNET_EDITOR" --headless --path "$GODOT_DOTNET_SAMPLE_DIR" \
        --export-pack lifetime-shutdown-pack "$root/native/godot.pck"; then
        cat "$root/export.log" >&2
        return 1
    fi
    cp "$root/native/godot.pck" "$root/clr/godot.pck"
    for runtime in clr native; do
        cp -L "$GODOT_DOTNET_TEMPLATE" "$root/$runtime/godot$EXE_EXT"
        chmod +x "$root/$runtime/godot$EXE_EXT"
    done
    cp "$library" "$root/native/$data/DotnetSample.$LIB_EXT"
    cmp "$root/native/godot$EXE_EXT" "$root/clr/godot$EXE_EXT"
    cmp "$root/native/godot.pck" "$root/clr/godot.pck"

    local runtime mode log rc
    for runtime in clr native; do
        for mode in hold drop collect drain dispose exitdrain exitcollect; do
            log="$root/$runtime/$mode.log"
            rc=0
            run_with_watchdog 120 env DN2CPP_DM_SHUTDOWN_MODE="$mode" DN2CPP_DM_TRACE=1 \
                "$root/$runtime/godot$EXE_EXT" --headless --verbose --quit-after 120 \
                >"$log" 2>&1 || rc=$?
            if [ "$rc" -ne 0 ]; then
                echo "FAIL: $runtime shutdown $mode exited $rc" >&2
                cat "$log" >&2
                return 1
            fi
            $python - "$log" "$runtime" "$mode" <<'PY'
import re, sys
from pathlib import Path
path, runtime, mode = Path(sys.argv[1]), sys.argv[2], sys.argv[3]
text = path.read_text(errors='replace')
def require(ok, message):
    if not ok:
        raise SystemExit(f'FAIL: {runtime} shutdown {mode}: {message}; see {path}')
for marker in (
    f'DN2CPP_DM_SHUTDOWN_ALLOCATED mode={mode} count=48',
    f'DN2CPP_DM_SHUTDOWN_QUIT mode={mode}',
    f'DN2CPP_DM_SHUTDOWN_EXIT mode={mode}',
    'Unloading: Disposing tracked instances...',
    'Unloading: Finished disposing tracked instances.',
):
    require(text.count(marker) == 1, 'missing or repeated marker: ' + marker)
require(not re.search(r'ERROR|ObjectDB instances (?:were )?leaked|RID allocations.*leaked|resources still in use', text),
        'engine error or shutdown leak')
if runtime == 'clr':
    require('.NET: hostfxr initialized' in text, 'CLR host was not initialized')
    require('dn2cpp-dm: init ok' not in text, 'CLR run loaded the native module')
else:
    require('dn2cpp-dm: init ok' in text, 'native module was not initialized')
collected = mode in ('collect', 'drain', 'exitcollect', 'exitdrain')
if collected:
    require(text.count(f'DN2CPP_DM_SHUTDOWN_COLLECT mode={mode} weakEmpty=True') == 1,
            'collected population remains weakly reachable')
rows = list(re.finditer(r'DN2CPP_DM_SHUTDOWN_FINALIZER id=(\d+) once=(True|False) main=(True|False)', text))
ids = [int(row[1]) for row in rows]
require(len(ids) == len(set(ids)) and all(row[2] == 'True' for row in rows), 'double finalization')
if runtime == 'native':
    require(all(row[3] == 'True' for row in rows), 'finalizer left the engine main thread')
    if collected:
        require(sorted(ids) == list(range(48)), 'pending finalizers did not all run')
        boundary = 'DN2CPP_DM_SHUTDOWN_QUIT' if mode in ('collect', 'drain') else 'Unloading: Disposing tracked instances...'
        require(all(row.start() < text.index(boundary) for row in rows), 'finalizers ran too late')
        if mode == 'exitcollect':
            require(all(row.start() > text.index('DN2CPP_DM_SHUTDOWN_EXIT') for row in rows),
                    'exit collection was drained by the application')
markers = [line for line in text.splitlines() if line.startswith('DN2CPP_DM_SHUTDOWN_')
           and not line.startswith('DN2CPP_DM_SHUTDOWN_FINALIZER')]
path.with_suffix('.markers').write_text('\n'.join(markers) + '\n')
print(f'   {runtime} shutdown {mode}: markers, lifetime and teardown verified')
PY
            if [ "$runtime" = native ]; then
                diff -u "$root/clr/$mode.markers" "$root/native/$mode.markers"
            fi
        done
    done
}
