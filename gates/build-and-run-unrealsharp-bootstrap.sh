#!/usr/bin/env bash
# UnrealSharp backend lifecycle contract using a UE-independent managed fixture.
# This gate does not establish real UnrealSharp or cooked-game compatibility.
source "$(dirname "$0")/_common.sh"

case "$(uname -s):$(uname -m)" in Darwin:arm64) ;; *) gate_skip "UnrealSharp native backend requires macOS arm64" ;; esac
sample=samples/unrealsharp/BootstrapFixture
registration=samples/unrealsharp/RegistrationFixture
build_proj "$registration/RegistrationFixture.csproj"
build_proj samples/unrealsharp/CalliOracle/CalliOracle.csproj
app="$registration/bin/$CONFIG/$TFM/RegistrationFixture.dll"
plugins="$registration/bin/$CONFIG/$TFM/UnrealSharp.Plugins.dll"
core="$registration/bin/$CONFIG/$TFM/UnrealSharp.Core.dll"
corelib=$(locate_corelib)
mkdir -p artifacts/unrealsharp-bootstrap
order=artifacts/unrealsharp-bootstrap/load-order.json
printf '%s\n' '{"Priority":0,"Collectible":false,"LoadOrder":["UnrealSharp.Plugins","RegistrationFixture"]}' > "$order"

expect_rejected() {
    local name="$1" diagnostic="$2" expected_status="$3" status=0
    shift 3
    invoke_cli "$app" -r "$plugins" -r "$core" -r "$corelib" -o artifacts/unrealsharp-bootstrap/rejected \
        "$@" > "artifacts/unrealsharp-bootstrap/$name.log" 2>&1 || status=$?
    if [ "$status" -ne "$expected_status" ] || ! grep -Fq -- "$diagnostic" "artifacts/unrealsharp-bootstrap/$name.log"; then
        cat "artifacts/unrealsharp-bootstrap/$name.log" >&2
        echo "FAIL: UnrealSharp $name input was not rejected" >&2
        exit 1
    fi
}
expect_rejected missing-order 'requires --unrealsharp-load-order' 1 --unrealsharp
expect_rejected duplicate-order 'duplicate load-order assembly' 2 --unrealsharp \
    --unrealsharp-load-order "$order" --unrealsharp-load-order "$order"
expect_rejected backend-conflict 'mutually exclusive' 1 --unrealsharp --gdextension \
    --unrealsharp-load-order "$order"
expect_rejected trim-reflection 'does not support --trim-reflection' 1 --unrealsharp --trim-reflection \
    --unrealsharp-load-order "$order"
invalid=artifacts/unrealsharp-bootstrap/invalid.json
printf '%s\n' '{"Priority":0,"LoadOrder":["MissingAssembly"]}' > "$invalid"
expect_rejected missing-assembly 'load-order assembly is missing' 2 --unrealsharp --unrealsharp-load-order "$invalid"
printf '%s\n' '{"Priority":0,"LoadOrder":["../RegistrationFixture"]}' > "$invalid"
expect_rejected invalid-name 'invalid assembly simple name' 2 --unrealsharp --unrealsharp-load-order "$invalid"
printf '%s\n' '{"Priority":0,"LoadOrder":["UnrealSharp.Plugins"]}' > "$invalid"
expect_rejected omitted-input 'input assembly is absent' 2 --unrealsharp --unrealsharp-load-order "$invalid"
printf '%s\n' '{' > "$invalid"
expect_rejected malformed-json 'error:' 2 --unrealsharp --unrealsharp-load-order "$invalid"
printf '%s\n' '{"Priority":"high","LoadOrder":[]}' > "$invalid"
expect_rejected malformed-priority 'error:' 2 --unrealsharp --unrealsharp-load-order "$invalid"
printf '%s\n' '{"Priority":0,"LoadOrder":42}' > "$invalid"
expect_rejected malformed-order 'error:' 2 --unrealsharp --unrealsharp-load-order "$invalid"

# The input argument order must not override manifest priority or filename ties.
first=artifacts/unrealsharp-bootstrap/A.LoadOrder.json
last=artifacts/unrealsharp-bootstrap/Z.LoadOrder.json
for priority in 0 10; do
    if [ "$priority" = 0 ]; then
        printf '%s\n' '{"Priority":0,"LoadOrder":["UnrealSharp.Plugins"]}' > "$first"
        printf '%s\n' '{"Priority":0,"LoadOrder":["RegistrationFixture"]}' > "$last"
    else
        printf '%s\n' '{"Priority":0,"LoadOrder":["RegistrationFixture"]}' > "$first"
        printf '%s\n' '{"Priority":10,"LoadOrder":["UnrealSharp.Plugins"]}' > "$last"
    fi
    ordered="artifacts/unrealsharp-bootstrap/order-$priority"
    invoke_cli "$app" -r "$plugins" -r "$core" -r "$corelib" --unrealsharp \
        --unrealsharp-load-order "$last" --unrealsharp-load-order "$first" -o "$ordered"
    grep -Fq 'dn2cpp_us_next == 0 && std::strcmp(name, "UnrealSharp.Plugins")' "$ordered/generated.cpp"
    grep -Fq 'dn2cpp_us_next == 1 && std::strcmp(name, "RegistrationFixture")' "$ordered/generated.cpp"
done

for retention in diet full; do
    out="artifacts/unrealsharp-bootstrap/$retention"
    flags=()
    [ "$retention" = full ] && flags+=(--no-ildiet)
    invoke_cli "$app" -r "$plugins" -r "$core" -r "$corelib" --unrealsharp \
        --unrealsharp-load-order "$order" ${flags[@]+"${flags[@]}"} -o "$out"
    cmake_build_app "$out" unrealsharp_fixture unrealsharp
    library="$(_cmake_app_builddir "$out")/$(lib_name unrealsharp_fixture)"
    hostbuild="$out/host"
    mkdir -p "$hostbuild"
    _cmake_step "$hostbuild/configure.log" "configure UnrealSharp contract host" \
        "$CMAKE" -S "$sample" -B "$hostbuild" -G Ninja -DDN2CPP_ROOT="$PWD" \
        -DCMAKE_CXX_COMPILER="${CMAKE_CXX_COMPILER:-clang++}"
    _cmake_step "$hostbuild/build.log" "build UnrealSharp contract host" \
        "$CMAKE" --build "$hostbuild"
    expected=$(dotnet "samples/unrealsharp/CalliOracle/bin/$CONFIG/$TFM/CalliOracle.dll" "$PWD/$hostbuild/$(lib_name unrealsharp_utf8_fixture)")
    actual=$(run_bounded "$hostbuild/unrealsharp_bootstrap_host" "$library" utf8)
    assert_output "$actual" "$expected"
    for scenario in success version size revision ue-version callbacks order bootstrap-failure bootstrap-callback-failure worker-callback-failure delegate-callback-failure initializer-failure tick-failure shutdown-failure; do
        actual=$(run_bounded "$hostbuild/unrealsharp_bootstrap_host" "$library" "$scenario")
        assert_output "$actual" "unrealsharp-bootstrap $scenario OK"
    done
done
echo "unrealsharp-bootstrap gate OK"
