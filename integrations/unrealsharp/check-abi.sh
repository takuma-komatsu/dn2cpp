#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
plugin="${1:-$root/artifacts/unrealsharp/plugin}"
if ! cmp -s "$root/runtime/unrealsharp/dn2cpp_unrealsharp_abi.h" \
    "$plugin/Source/UnrealSharpCore/Public/dn2cpp_unrealsharp_abi.h"; then
    echo "error: UnrealSharp fork and dn2cpp runtime ABI headers differ" >&2
    exit 1
fi
work="$(mktemp -d "${TMPDIR:-/tmp}/unrealsharp-abi.XXXXXX")"
trap 'rm -rf "$work"' EXIT
python3 - "$plugin" "$work/probe.cpp" <<'PY'
import pathlib, re, sys
root = pathlib.Path(sys.argv[1])
header = (root / 'Source/UnrealSharpCore/Public/DotNet/CSDotNetRuntimeHost.h').read_text()
match = re.search(r'struct FCSInitializationResult\s*\{.*?\n\};', header, re.S)
if not match: raise SystemExit('Missing initialization result declaration')
managed = (root / 'Managed/UnrealSharp/UnrealSharp.Plugins/Main.cs').read_text()
if 'MessageCapacity = 4096;' not in managed or 'fixed byte Message[MessageCapacity]' not in managed:
    raise SystemExit('Managed initialization layout changed; audit the ABI')
native_bool = (root / 'Managed/UnrealSharp/UnrealSharp.Core/NativeBool.cs').read_text()
if not re.search(r'enum NativeBool\s*:\s*byte', native_bool):
    raise SystemExit('NativeBool must be byte-sized')
callbacks = (root / 'Source/UnrealSharpCore/Public/CSManagedCallbacksCache.h').read_text()
if not re.search(r'ManagedCallbacks_InvokeDelegate\s*=\s*void', callbacks):
    raise SystemExit('InvokeDelegate must return void')
output = pathlib.Path(sys.argv[2])
(output.parent / 'CoreMinimal.h').write_text('#pragma once\n')
source = (root / 'Source/UnrealSharpCore/Private/CSManagedCallbacksCache.cpp').read_text()
storage = re.search(r'FCSManagedCallbacks& GetManagedCallbacks\(\)\s*\{.*?\n\}', source, re.S)
if not storage:
    raise SystemExit('Missing exported callback table storage')
prelude = '#include <cstdint>\nusing uint8 = uint8_t;\nusing TCHAR = char16_t;\n#define UNREALSHARPCORE_API __attribute__((visibility("default")))\n#include "' + str((root / 'Source/UnrealSharpCore/Public/CSManagedCallbacksCache.h').resolve()) + '"\n'
(output.parent / 'storage.cpp').write_text(prelude + storage.group() + '\n')
(output.parent / 'peer.cpp').write_text(prelude + 'extern "C" UNREALSHARPCORE_API void* peer_table() { return &GetManagedCallbacks(); }\n')
output.write_text(prelude + 'extern "C" void* peer_table();\n' + '#include <cstdint>\n#include <cstddef>\nusing uint8 = uint8_t;\nusing UTF8CHAR = char;\n' + match.group() + '\nstatic_assert(sizeof(FCSInitializationResult) == 4097);\nstatic_assert(alignof(FCSInitializationResult) == 1);\nstatic_assert(offsetof(FCSInitializationResult, Message) == 1);\nint main() { FCSInitializationResult result; return result.bSuccess || result.Message[4095] || peer_table() != &GetManagedCallbacks(); }\n')
PY
cat > "$work/CMakeLists.txt" <<'CMAKE'
cmake_minimum_required(VERSION 3.20)
project(UnrealSharpAbiProbe LANGUAGES CXX)
include_directories("${CMAKE_CURRENT_SOURCE_DIR}")
set(CMAKE_CXX_VISIBILITY_PRESET hidden)
add_library(storage SHARED storage.cpp)
add_library(peer SHARED peer.cpp)
target_link_libraries(peer PRIVATE storage)
add_executable(probe probe.cpp)
target_link_libraries(probe PRIVATE storage peer)
target_compile_features(probe PRIVATE cxx_std_17)
target_compile_options(probe PRIVATE -Wall -Wextra -Werror)
CMAKE
cmake_args=(-G Ninja)
if [[ "$(uname -s)" == Darwin ]]; then
    cmake_args+=("-DCMAKE_OSX_SYSROOT=${SDKROOT:-$(xcrun --sdk macosx --show-sdk-path)}")
    cmake_args+=("-DCMAKE_CXX_COMPILER=${CMAKE_CXX_COMPILER:-${CXX:-$(xcrun --sdk macosx --find clang++)}}")
fi
if [[ -n "${SDKROOT:-}" ]]; then cmake_args+=("-DCMAKE_OSX_SYSROOT=$SDKROOT"); fi
if [[ -n "${CMAKE_CXX_COMPILER:-}" ]]; then cmake_args+=("-DCMAKE_CXX_COMPILER=$CMAKE_CXX_COMPILER"); fi
cmake -S "$work" -B "$work/build" "${cmake_args[@]}"
cmake --build "$work/build"
"$work/build/probe"
echo 'UNREALSHARP_CLR_ABI_OK'
