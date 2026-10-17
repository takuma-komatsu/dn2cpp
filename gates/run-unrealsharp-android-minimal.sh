#!/usr/bin/env bash
# Build, package, and run the UnrealSharp native Game path on Android arm64.
source "$(dirname "$0")/_common.sh"

: "${UE_ROOT:?Set UE_ROOT to an Unreal Engine 5.8.3 installation with Android support}"
: "${UNREALSHARP_PLUGIN:?Set UNREALSHARP_PLUGIN to the UnrealSharp-dn2cpp fork checkout}"
case "$(uname -s):$(uname -m)" in Darwin:arm64) ;; *) gate_skip "UnrealSharp Android packaging requires macOS arm64" ;; esac

for executable in dotnet cmake ninja python3 rsync adb; do
    command -v "$executable" >/dev/null || { echo "error: missing $executable" >&2; exit 1; }
done
ue=$(cd "$UE_ROOT" && pwd -P)
plugin=$(cd "$UNREALSHARP_PLUGIN" && pwd -P)
ubt="$ue/Engine/Binaries/DotNET/UnrealBuildTool/UnrealBuildTool.dll"
uat="$ue/Engine/Build/BatchFiles/RunUAT.sh"
unrealpak="$ue/Engine/Binaries/Mac/UnrealPak"
for required in "$ubt" "$uat" "$unrealpak" "$plugin/UnrealSharp.uplugin"; do
    [ -e "$required" ] || { echo "error: missing $required" >&2; exit 1; }
done
for required in "$ue/Engine/Binaries/Android/UnrealGame-arm64.so" \
    "$ue/Engine/Binaries/Android/UnrealGame.target"; do
    [ -f "$required" ] || {
        echo "error: UE Android Target Platform is missing; install the UE 5.8.3 Android component ($required)" >&2
        exit 1
    }
done
[ -f src/Dn2Cpp.Cli/bin/Release/net10.0/dn2cpp.dll ] || {
    echo "error: build the Release dn2cpp CLI first" >&2
    exit 1
}

sdk=${ANDROID_HOME:-${ANDROID_SDK_ROOT:-$HOME/Library/Android/sdk}}
aapt="$sdk/build-tools/36.0.0/aapt"
[ -x "$aapt" ] || { echo "error: Android Build Tools 36.0.0 are required at $aapt" >&2; exit 1; }
[ -d "$sdk/platforms/android-36" ] || { echo "error: Android SDK Platform 36 is required" >&2; exit 1; }
ndk=${ANDROID_NDK_ROOT:-$sdk/ndk/27.2.12479018}
[ -f "$ndk/build/cmake/android.toolchain.cmake" ] || {
    echo "error: Android NDK r27c is required; set ANDROID_NDK_ROOT" >&2
    exit 1
}
if ! LC_ALL=C grep -Eq '^Pkg.Revision = 27\.2\.12479018$' "$ndk/source.properties"; then
    echo "error: UE 5.8.3 requires Android NDK r27c (27.2.12479018)" >&2
    exit 1
fi
export ANDROID_HOME="$sdk" ANDROID_NDK_ROOT="$ndk" ANDROID_NDK_HOME="$ndk" NDKROOT="$ndk"

devices=$(LC_ALL=C adb devices)
serial=${ANDROID_SERIAL:-}
if [ -z "$serial" ]; then
    serial=$(python3 -c 'import sys; rows=[line.split()[0] for line in sys.stdin if "\tdevice" in line]; print(rows[0] if len(rows)==1 else "")' <<<"$devices")
    [ -n "$serial" ] || { echo "error: connect one authorized Android device or set ANDROID_SERIAL; adb devices: $devices" >&2; exit 1; }
fi
LC_ALL=C adb -s "$serial" get-state >/dev/null

work="${UNREALSHARP_ANDROID_RESULT_DIR:-$PWD/artifacts/unrealsharp-android-minimal}"
mkdir -p "$work"
work=$(cd "$work" && pwd -P)
project="$work/Minimal"
archive="$work/archive"
rm -rf "$project" "$archive"
mkdir -p "$project/Plugins" "$archive"
cp -Rp samples/unrealsharp/Minimal/. "$project/"
project_plugin="$project/Plugins/UnrealSharp"
mkdir -p "$project_plugin"
rsync -a --delete --exclude=.git --exclude=Binaries --exclude=Intermediate \
    --exclude=bin --exclude=obj "$plugin/" "$project_plugin/"
plugin="$project_plugin"
cp "$project/Script/ManagedMinimal/ManagedMinimal.csproj.template" \
    "$project/Script/ManagedMinimal/ManagedMinimal.csproj"

echo "== Generate Android Game bindings with UBT =="
solution="$project/Script/ManagedMinimal.slnx"
held_solution="$project/ManagedMinimal.slnx"
mv "$solution" "$held_solution"
trap 'mv "$held_solution" "$solution"' EXIT
dotnet "$ubt" Minimal Android Development "-Project=$project/Minimal.uproject" \
    -UsePrecompiled -NoEngineChanges -NoHotReload -NoUBA -SkipDeploy -architecture=arm64 \
    "-Log=$work/game-build.log"
rm -f "$project/Script/ManagedMinimal.sln"
mv "$held_solution" "$solution"
trap - EXIT
bindings="$plugin/Intermediate/UnrealSharp/UHT/Game"
[ -d "$bindings" ] && [ -n "$(find "$bindings" -name '*.cs' -type f -print -quit)" ] || {
    echo "error: UBT did not generate Android Game bindings under $bindings" >&2
    exit 1
}

echo "== Build UnrealSharp analyzers for Game IL =="
analyzers="$plugin/Binaries/Managed/netstandard2.0"
mkdir -p "$analyzers"
for project_file in \
    Managed/UnrealSharp/UnrealSharp.GlueGenerator/UnrealSharp.GlueGenerator/UnrealSharp.GlueGenerator.csproj \
    Managed/UnrealSharp/UnrealSharp.Analyzers/UnrealSharp.Analyzers.csproj \
    Managed/UnrealSharp/UnrealSharp.CodeFixer/UnrealSharp.CodeFixer.csproj \
    Managed/UnrealSharp/UnrealSharp.SourceGenerators/UnrealSharp.SourceGenerators.csproj; do
    dotnet build "$plugin/$project_file" -c Release -o "$analyzers" --nologo
done
for assembly in Newtonsoft.Json UnrealSharp.GlueGenerator UnrealSharp.Analyzers \
    UnrealSharp.CodeFixer UnrealSharp.SourceGenerators; do
    [ -f "$analyzers/$assembly.dll" ] || {
        echo "error: UnrealSharp analyzer output missing: $assembly.dll" >&2
        exit 1
    }
done

echo "== Publish Game IL and stage the native library =="
CMAKE_BUILD_PARALLEL_LEVEL=${CMAKE_BUILD_PARALLEL_LEVEL:-4} "$uat" \
    "-ScriptDir=$plugin/Build/Scripts" StageUnrealSharp \
    "-Project=$project/Minimal.uproject" -UETargetType=Game \
    -UEBuildConfig=Development -TargetPlatform=Android -TargetArchitecture=arm64 \
    -PackagingBackend=Dn2Cpp "-Dn2CppRoot=$PWD" \
    > "$work/stage.log" 2>&1 || { cat "$work/stage.log" >&2; exit 1; }
native_stage="$project/Intermediate/UnrealSharp/NativeStage/Android/Development"
[ -f "$native_stage/Binaries/Android/arm64-v8a/libUnrealSharpGame.so" ] || {
    echo "error: native library missing from $native_stage" >&2
    exit 1
}
[ -f "$native_stage/Binaries/Managed/net10.0/UnrealSharpBuild.flag" ] || {
    echo "error: UnrealSharpBuild.flag missing from native stage" >&2
    exit 1
}
[ -n "$(find "$native_stage/Binaries/Managed/net10.0" -name '*.LoadOrder.json' -type f -print -quit)" ] || {
    echo "error: native stage has no load-order manifest" >&2
    exit 1
}

echo "== Refresh the Android Game receipt with staged UFS files =="
dotnet "$ubt" Minimal Android Development "-Project=$project/Minimal.uproject" \
    -UsePrecompiled -NoEngineChanges -NoHotReload -NoUBA -NoUBTMakefiles -SkipDeploy \
    -architecture=arm64 "-Log=$work/game-receipt.log"

# Earlier receipts can leave native manifests in the CLR Editor lookup directory.
legacy_managed="$project/Binaries/Managed/net10.0"
if [ -d "$legacy_managed" ]; then
    rm -f "$legacy_managed/UnrealSharpBuild.flag" "$legacy_managed"/*.LoadOrder.json
fi

echo "== Build the Editor target for Cook =="
mv "$solution" "$held_solution"
trap 'rm -f "$project/Script/ManagedMinimal.sln"; mv "$held_solution" "$solution"' EXIT
dotnet "$ubt" MinimalEditor Mac Development "-Project=$project/Minimal.uproject" \
    -UsePrecompiled -NoEngineChanges -NoHotReload -NoUBA -NoUBTMakefiles -SkipDeploy \
    -architecture=arm64 "-Log=$work/editor-build.log"

echo "== Cook and package a Development APK =="
"$uat" BuildCookRun "-project=$project/Minimal.uproject" -noP4 -unattended \
    -platform=Android -targetplatform=Android -clientconfig=Development \
    -skipbuild -cook -map=/Engine/Maps/Entry -stage -package -archive \
    "-archivedirectory=$archive" -pak -skipiostore \
    -AdditionalCookerOptions=-SkipZenStore \
    > "$work/package.log" 2>&1 || { cat "$work/package.log" >&2; exit 1; }
rm -f "$project/Script/ManagedMinimal.sln"
mv "$held_solution" "$solution"
trap - EXIT
apk=$(find "$archive" -name '*.apk' -type f -print)
[ -n "$apk" ] && [ "${apk#*$'\n'}" = "$apk" ] || {
    echo "error: expected one APK in $archive; found: $apk" >&2
    exit 1
}

echo "== Verify APK payload =="
python3 - "$apk" "$unrealpak" "$work" <<'PY'
import io
from pathlib import Path
import re
import subprocess
import sys
import zipfile

apk, unrealpak, work = Path(sys.argv[1]), sys.argv[2], Path(sys.argv[3])
content = []
pak_listings = []
pak_count = 0

def inspect_zip(bundle):
    global pak_count
    for name in bundle.namelist():
        content.append(name)
        if name.lower().endswith(('.obb', '.obb.png')):
            with zipfile.ZipFile(io.BytesIO(bundle.read(name))) as obb:
                inspect_zip(obb)
        elif name.lower().endswith('.pak'):
            extracted = work / f'payload-{pak_count}.pak'
            pak_count += 1
            extracted.write_bytes(bundle.read(name))
            listing = subprocess.run([unrealpak, str(extracted), '-List'],
                                     capture_output=True, text=True, check=True).stdout
            content.append(listing)
            pak_listings.append(listing)
            extracted.unlink()

with zipfile.ZipFile(apk) as bundle:
    expected_lib = 'lib/arm64-v8a/libUnrealSharpGame.so'
    if expected_lib not in bundle.namelist():
        raise SystemExit(f'APK lacks {expected_lib}')
    inspect_zip(bundle)

contents = '\n'.join(content)
(work / 'apk-contents.txt').write_text(contents + '\n')
pak_contents = '\n'.join(pak_listings)
ufs_path = r'Content/Dn2Cpp/Managed/net10\.0/'
if not re.search(ufs_path + r'UnrealSharpBuild\.flag\b', pak_contents) or not re.search(
        ufs_path + r'[^/\s"]+\.LoadOrder\.json\b', pak_contents):
    raise SystemExit('APK UFS payload lacks native flag or load-order manifests under Content/Dn2Cpp/Managed/net10.0')
if re.search(r'\.dll\b|lib(?:hostfxr|coreclr|hostpolicy|clrjit)', contents, re.IGNORECASE):
    raise SystemExit('APK payload contains CLR execution artifacts')
PY
LC_ALL=C "$aapt" dump badging "$apk" > "$work/apk-badging.txt"
package=$(LC_ALL=C sed -n "s/^package: name='\([^']*\)'.*/\1/p" "$work/apk-badging.txt")
[ -n "$package" ] || { echo "error: aapt did not report the APK package name" >&2; exit 1; }

echo "== Install and launch on $serial =="
LC_ALL=C adb -s "$serial" install -r "$apk" > "$work/install.log"
LC_ALL=C adb -s "$serial" shell am force-stop "$package" >/dev/null
LC_ALL=C adb -s "$serial" logcat -c
LC_ALL=C adb -s "$serial" shell monkey -p "$package" 1 > "$work/launch.log"
pid=
for ((attempt=0; attempt<120; attempt++)); do
    pid_list=$(LC_ALL=C adb -s "$serial" shell pidof "$package" | tr -d '\r' || true)
    pid=${pid_list%% *}
    [ -z "$pid" ] || break
    sleep 1
done
[ -n "$pid" ] || { echo "error: APK did not start; inspect $work/launch.log" >&2; exit 1; }
printf '%s\n' "$pid" > "$work/device-pid.txt"
success=
for ((attempt=0; attempt<180; attempt++)); do
    LC_ALL=C adb -s "$serial" logcat -d -v threadtime --pid="$pid" > "$work/logcat.txt"
    if LC_ALL=C grep -Fq 'Dn2CppAndroidMinimal result=42 clr=absent status=OK' "$work/logcat.txt" &&
       LC_ALL=C grep -Fq 'managed-start=OK' "$work/logcat.txt"; then
        success=1
        break
    fi
    sleep 1
done
[ -n "$success" ] || { echo "error: missing managed startup or result marker; inspect $work/logcat.txt" >&2; exit 1; }
sleep 10
LC_ALL=C adb -s "$serial" logcat -d -v threadtime --pid="$pid" > "$work/logcat.txt"
if LC_ALL=C grep -Eq 'Dn2CppAndroidMinimal .*status=FAIL|Fatal signal|FATAL EXCEPTION|Fatal error:|Assertion failed:' "$work/logcat.txt"; then
    echo "error: game reported a fatal error; inspect $work/logcat.txt" >&2
    exit 1
fi
live_pids=$(LC_ALL=C adb -s "$serial" shell pidof "$package" | tr -d '\r' || true)
still_running=
for candidate in $live_pids; do
    if [ "$candidate" = "$pid" ]; then
        still_running=1
        break
    fi
done
[ -n "$still_running" ] || { echo "error: game process exited or restarted after passing the probe" >&2; exit 1; }
echo "unrealsharp Android Minimal device smoke OK; APK=$apk"
