#!/usr/bin/env bash
# The real packaged UE loader reports absent and incompatible native libraries.
source "$(dirname "$0")/_common.sh"

: "${UNREALSHARP_APP:?Set UNREALSHARP_APP to the native cooked Baseline app}"
case "$(uname -s):$(uname -m)" in Darwin:arm64) ;; *) gate_skip "UnrealSharp package failures require macOS arm64" ;; esac
app=$(cd "$UNREALSHARP_APP" && pwd -P)
work="${UNREALSHARP_RESULT_DIR:-$PWD/artifacts/unrealsharp-package-failures}"
mkdir -p "$work"
work=$(cd "$work" && pwd -P)
compiler="${CMAKE_CXX_COMPILER:-$(xcrun --sdk macosx --find clang++)}"
sdk="${SDKROOT:-$(xcrun --sdk macosx --show-sdk-path)}"
cmake -S samples/unrealsharp/AbiMismatchNative -B "$work/fixture" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release -DCMAKE_OSX_ARCHITECTURES=arm64 \
    "-DCMAKE_CXX_COMPILER=$compiler" "-DCMAKE_OSX_SYSROOT=$sdk" > "$work/build.log" 2>&1
cmake --build "$work/fixture" >> "$work/build.log" 2>&1
unset DN2CPP_SMOKE_LIFECYCLE_FILE DN2CPP_SMOKE_PENDING_FILE DN2CPP_SMOKE_LATE_CALLBACK_FILE
for scenario in missing incompatible; do
    trial=$(mktemp -d "$work/$scenario.XXXXXX")
    cp -cR "$app" "$trial/Baseline.app"
    copy="$trial/Baseline.app"
    native="$copy/Contents/UE/Baseline/Binaries/Mac/libUnrealSharpGame.dylib"
    if [ "$scenario" = missing ]; then
        rm "$native"
        expected='Missing dn2cpp runtime or exports:'
    else
        cp "$work/fixture/libUnrealSharpGame.dylib" "$native"
        expected='UnrealSharp ABI mismatch: fixture library version 2, host version 1'
    fi
    codesign --force --deep --sign - --preserve-metadata=identifier,entitlements,requirements,flags,runtime "$copy" > "$trial/sign.log" 2>&1
    codesign --verify --deep --strict "$copy" > "$trial/verify.log" 2>&1
    executable=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$copy/Contents/Info.plist")
    args=(-Unattended -NullRHI -NoSound -NoCrashDialog -Dn2CppSmoke -Dn2CppSmokeExpectNative "-abslog=$trial/run.log")
    printf '%s\n' "${args[@]}" > "$trial/launch-args.txt"
    status=0
    DN2CPP_RUN_WATCHDOG_SECS=30 run_bounded "$copy/Contents/MacOS/$executable" \
        "${args[@]}" > "$trial/stdout.log" 2>&1 || status=$?
    printf '%s\n' "$status" > "$trial/exit.txt"
    if [ "$status" = 0 ] || grep -Fq 'WATCHDOG:' "$trial/stdout.log"; then
        echo "error: $scenario did not fail during startup; inspect $trial" >&2
        exit 1
    fi
    grep -F "$expected" "$trial/stdout.log"
done
echo "unrealsharp packaged native library failures OK"
