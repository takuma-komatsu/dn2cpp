#!/usr/bin/env bash
# Source after _common.sh. Native dn2cpp launches a self-contained companion,
# so a relocated toolchain needs no dotnet installation.
# Resolve only the published generation; flat paths can observe partial output.
ildiet_generation() {
    local output="$1" python="${PYTHON:-}"
    [ -n "$python" ] || python=$(resolve_python) || return $?
    $python - "$output" <<'PYTHON'
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET
output = Path(sys.argv[1])
root = ET.parse(output / "ildiet-result.xml").getroot()
generation = root.get("generation", "")
if root.tag != "ildietResult" or root.get("version") != "2" or not re.fullmatch(r"g-[0-9a-f]{16}", generation):
    raise SystemExit("invalid ILDiet generation manifest: " + str(output))
path = output / generation
if not path.is_dir():
    raise SystemExit("missing ILDiet generation: " + str(path))
sys.stdout.reconfigure(encoding="utf-8")
sys.stdout.write(path.as_posix())
PYTHON
}

ildiet_stage_native() (
    local destination="$1" rid os arch staging scratch
    case "$DN2CPP_OS" in
        macos) os=osx ;;
        windows) os=win ;;
        linux) os=linux ;;
        *) echo "error: no ILDiet host RID for $DN2CPP_OS" >&2; return 1 ;;
    esac
    case "$(uname -m)" in
        arm64|aarch64) arch=arm64 ;;
        x86_64|amd64|AMD64) arch=x64 ;;
        *) echo "error: unsupported ILDiet host architecture" >&2; return 1 ;;
    esac
    rid="$os-$arch"
    mkdir -p "$PWD/artifacts"
    scratch="$(mktemp -d "$PWD/artifacts/ildiet-$CONFIG-$rid.XXXXXX")"
    staging="$scratch/publish"
    trap 'rm -rf "$scratch"' EXIT
    dotnet publish src/ILDiet/ILDiet.csproj -c "$CONFIG" -r "$rid" \
        --self-contained true -p:UseAppHost=true -p:PublishSingleFile=false \
        -p:PublishAot=false -p:MSBuildProjectExtensionsPath="$scratch/obj/" \
        -o "$staging" --nologo
    [ -x "$staging/ILDiet$EXE_EXT" ] || {
        echo "error: ILDiet host app is missing: $staging/ILDiet$EXE_EXT" >&2; return 1; }
    for notice in Mono.Cecil.LICENSE.txt runtime-LICENSE.TXT runtime-THIRD-PARTY-NOTICES.TXT; do
        [ -f "$staging/$notice" ] || {
            echo "error: ILDiet redistribution notice is missing: $notice" >&2; return 1; }
    done
    rm -rf "$destination"
    mkdir -p "$destination"
    cp -R "$staging/." "$destination/"
)
