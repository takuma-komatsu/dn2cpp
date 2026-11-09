#!/usr/bin/env bash
# Git Bash prerequisites for MSVC and POSIX cross-target validation.
set -euo pipefail
cd "$(dirname "$0")/.."
source gates/_setup-premerge.sh
premerge_setup_init windows

choco install ninja -y
export CMAKE_CXX_COMPILER=cl
premerge_export CMAKE_CXX_COMPILER
if ! command -v shasum >/dev/null 2>&1; then
    mkdir -p "$PREMERGE_TOOLS/bin"
    printf '%s\n' \
        '#!/usr/bin/env bash' \
        'if [ "$1" = "-a" ] && [ "$2" = "256" ]; then shift 2; fi' \
        'exec sha256sum "$@"' > "$PREMERGE_TOOLS/bin/shasum"
    chmod +x "$PREMERGE_TOOLS/bin/shasum"
    premerge_add_path "$PREMERGE_TOOLS/bin"
fi
# Python's ILDiet alias checks require real symlinks, including directory links.
pwsh -NoLogo -NoProfile -NonInteractive -Command \
    'New-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -Force | Out-Null; New-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" -Name AllowDevelopmentWithoutDevLicense -Value 1 -PropertyType DWord -Force | Out-Null'
export MSYS="${MSYS:+$MSYS }winsymlinks:nativestrict"
premerge_export MSYS
python - "$PREMERGE_TOOLS" <<'PY'
import pathlib, sys, tempfile
with tempfile.TemporaryDirectory(dir=sys.argv[1]) as directory:
    root = pathlib.Path(directory)
    (root / 'file').write_text('symlink probe', encoding='utf-8')
    (root / 'directory').mkdir()
    (root / 'file-link').symlink_to(root / 'file')
    (root / 'directory-link').symlink_to(root / 'directory', target_is_directory=True)
    assert (root / 'file-link').is_symlink() and (root / 'directory-link').is_symlink()
PY

# Cross-target emission must read the POSIX CoreLib, kept out of DOTNET_ROOT.
python - "$DN2CPP_PREMERGE_LOGROOT" > "$PREMERGE_TOOLS/cross-runtime-input.txt" <<'PY'
import json, pathlib, runpy, sys, urllib.request
runtime = runpy.run_path('gates/setup-culture-reference.py')['PROFILES']['ci'][1]
url = 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'
with urllib.request.urlopen(url, timeout=60) as response:
    data = response.read()
(pathlib.Path(sys.argv[1]) / 'dotnet-release-metadata.json').write_bytes(data)
release = next(value for value in json.loads(data)['releases'] if value['runtime']['version'] == runtime)
archive = next(value for value in release['runtime']['files'] if value['rid'] == 'linux-x64' and value['name'] == 'dotnet-runtime-linux-x64.tar.gz')
print(runtime, archive['url'], archive['hash'], sep='\t')
PY
IFS=$'\t' read -r cross_version cross_url cross_hash < "$PREMERGE_TOOLS/cross-runtime-input.txt"
cross_hash="${cross_hash%$'\r'}"
[[ "$cross_version" =~ ^10\.0\.[0-9]+$ && "$cross_hash" =~ ^[0-9a-fA-F]{128}$ ]]
premerge_download "$cross_url" "$PREMERGE_TOOLS/downloads/cross-runtime.tar.gz" sha512 "$cross_hash"
mkdir -p "$PREMERGE_TOOLS/cross-runtime"
tar -xzf "$(cygpath -u "$PREMERGE_TOOLS/downloads/cross-runtime.tar.gz")" \
    -C "$(cygpath -u "$PREMERGE_TOOLS/cross-runtime")"
rm "$PREMERGE_TOOLS/downloads/cross-runtime.tar.gz"
export DN2CPP_CORELIB_CROSS="$PREMERGE_TOOLS/cross-runtime/shared/Microsoft.NETCore.App/$cross_version/System.Private.CoreLib.dll"
test -f "$DN2CPP_CORELIB_CROSS" || {
    echo "error: cross-runtime archive has no CoreLib at $DN2CPP_CORELIB_CROSS" >&2; exit 1;
}
premerge_export DN2CPP_CORELIB_CROSS

premerge_prepare_compilers
premerge_prepare_godot
(
    source gates/_common.sh
    command -v cl.exe
    command -v link.exe
    command -v dumpbin.exe
    printf 'MSVC include=%s\nMSVC lib=%s\n' "$INCLUDE" "$LIB"
    '/c/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe' \
        -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64
) > "$DN2CPP_PREMERGE_LOGROOT/tool-versions.txt" 2>&1
premerge_record_inputs
