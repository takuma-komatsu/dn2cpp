#!/usr/bin/env bash
# Hosted Apple Silicon prerequisites, including the selected iOS console gate.
set -euo pipefail
cd "$(dirname "$0")/.."
source gates/_setup-premerge.sh
premerge_setup_init macos

brew install cmake ninja openssl@3
premerge_add_path "$(brew --prefix openssl@3)/bin"
export CC="$(xcrun --find clang)" CXX="$(xcrun --find clang++)"
premerge_export CC CXX
# The r26d NDK includes Intel host tools on Apple Silicon.
if ! arch -x86_64 /usr/bin/true; then
    sudo softwareupdate --install-rosetta --agree-to-license
fi
arch -x86_64 /usr/bin/true

xcodebuild -version
xcrun --sdk iphonesimulator --show-sdk-path
xcrun simctl list devices available --json > "$DN2CPP_PREMERGE_LOGROOT/simulators.json"
simulator=$(python - "$DN2CPP_PREMERGE_LOGROOT/simulators.json" <<'PY'
import json, subprocess, sys
devices = json.load(open(sys.argv[1], encoding='utf-8'))['devices']
for group in devices.values():
    for device in group:
        if device.get('isAvailable') and device['name'].startswith('iPhone'):
            print(device['udid'])
            raise SystemExit(0)
runtimes = json.loads(subprocess.check_output(['xcrun', 'simctl', 'list', 'runtimes', '--json']))['runtimes']
runtimes = [runtime for runtime in runtimes if runtime.get('isAvailable') and runtime['identifier'].startswith('com.apple.CoreSimulator.SimRuntime.iOS-')]
if not runtimes:
    raise SystemExit('The selected Xcode has no available iOS simulator runtime')
runtime = max(runtimes, key=lambda value: tuple(map(int, value['version'].split('.'))))
device_type = next(value for value in runtime['supportedDeviceTypes'] if value['name'].startswith('iPhone'))
print(subprocess.check_output(['xcrun', 'simctl', 'create', 'dn2cpp-premerge', device_type['identifier'], runtime['identifier']], text=True).strip())
PY
)
xcrun simctl bootstatus "$simulator" -b
premerge_prepare_compilers
premerge_prepare_godot
{
    "$CC" --version
    xcodebuild -version
    xcrun --sdk iphonesimulator --show-sdk-version
    xcrun simctl list devices available
    brew list --versions cmake ninja openssl@3
} > "$DN2CPP_PREMERGE_LOGROOT/tool-versions.txt" 2>&1
premerge_record_inputs
