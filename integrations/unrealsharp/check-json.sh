#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/../.." && pwd)"
plugin="${1:-$root/artifacts/unrealsharp/plugin}"
: "${UE_ROOT:?Set UE_ROOT to the installed Unreal Engine}"
work="$(mktemp -d "${TMPDIR:-/tmp}/unrealsharp-json.XXXXXX")"
trap 'rm -rf "$work"' EXIT
python3 - "$plugin" "$work/probe.cpp" <<'PY'
from pathlib import Path
import sys
source = (Path(sys.argv[1]) / 'Source/UnrealSharpUtilities/Private/Json/CSRapidJsonUtilties.cpp').read_text()
start = source.index('\tbool ParseJsonString(')
end = source.index('\n\tTOptional<FConstObject>', start)
Path(sys.argv[2]).write_text(r'''
#include <rapidjson/document.h>
#include <rapidjson/error/en.h>
#include <string>
#include <utility>
#include <iostream>
using TCHAR = char16_t;
using FEncoding = rapidjson::UTF16<TCHAR>;
using FDocument = rapidjson::GenericDocument<FEncoding>;
#define UE_LOGFMT(...) ((void)0)
#define MoveTemp(value) std::move(value)
''' + source[start:end] + r'''
int main()
{
    static constexpr char16_t literal[] = u"{\"name\":\"日本語\\n\\u2605\"}";
    FDocument document;
    if (!ParseJsonString(literal, document)) return 1;
    if (std::u16string(document[u"name"].GetString()) != u"日本語\n★") return 2;
    std::u16string input = u"{\"name\":\"owned-value\"}";
    const auto original = input;
    if (!ParseJsonString(input.c_str(), document) || input != original) return 3;
    input.assign(input.size(), u'x');
    if (std::u16string(document[u"name"].GetString()) != u"owned-value") return 4;
    if (ParseJsonString(u"{", document)) return 5;
    if (std::u16string(document[u"name"].GetString()) != u"owned-value") return 6;
    std::cout << "UNREALSHARP_IMMUTABLE_JSON_OK\n";
}
''')
PY
cat > "$work/CMakeLists.txt" <<'CMAKE'
cmake_minimum_required(VERSION 3.20)
project(UnrealSharpImmutableJson LANGUAGES CXX)
add_executable(probe probe.cpp)
target_include_directories(probe PRIVATE "${UE_RAPIDJSON}")
target_compile_features(probe PRIVATE cxx_std_17)
CMAKE
cmake_args=(-G Ninja "-DUE_RAPIDJSON=$UE_ROOT/Engine/Source/ThirdParty/RapidJSON/1.1.0")
if [[ "$(uname -s)" == Darwin ]]; then
    cmake_args+=("-DCMAKE_OSX_SYSROOT=${SDKROOT:-$(xcrun --sdk macosx --show-sdk-path)}")
    cmake_args+=("-DCMAKE_CXX_COMPILER=${CMAKE_CXX_COMPILER:-${CXX:-$(xcrun --sdk macosx --find clang++)}}")
fi
cmake -S "$work" -B "$work/build" "${cmake_args[@]}"
cmake --build "$work/build"
"$work/build/probe"
