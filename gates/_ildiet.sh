#!/usr/bin/env bash
# Source after _common.sh. Native dn2cpp launches a self-contained companion,
# so a relocated toolchain needs no dotnet installation.
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
