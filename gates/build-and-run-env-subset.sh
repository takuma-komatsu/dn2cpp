#!/usr/bin/env bash
# System.Environment + System.IO.Directory.
# GetEnvironmentVariable / CurrentDirectory get+set / Directory.Exists+GetCurrentDirectory+
# SetCurrentDirectory lower to the dn2cpp_env_* / dn2cpp_directory_* helpers
# (getenv / getcwd / chdir / stat) — the real BCL bodies pull in the same file-I/O /
# globalization cascade File/Path do. They are intercepted before real resolution AND
# excluded from reachability (the three-point pattern). GetEnvironmentVariable
# returns null (never "") when a variable is unset; Directory.Exists is true only for a
# directory.
#
# The sample takes a scratch directory as args[0]. We give the
# native build and real .NET SEPARATE fresh directories and export IDENTICAL env vars
# to both, then diff their output exactly — the program prints only env-var values and
# booleans, never absolute paths. CoreLib only (no Linq shim).
source "$(dirname "$0")/_common.sh"

project=EnvSubset
out="artifacts/$(printf '%s' "$project" | tr '[:upper:]' '[:lower:]')"

echo "== 1/4 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 2/4 Building app assembly =="
build_proj "samples/dotnet/$project/$project.csproj"
app="samples/dotnet/$project/bin/$CONFIG/$TFM/$project.dll"

echo "== 3/4 Transpiling app + real CoreLib (tree-shaken) =="
invoke_cli "$app" -r "$corelib" -o "$out"
if gate_cache_check "$out" "env-subset|$project|$corelib" \
        "$app" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json"; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/4 Compiling C++ and running (exact diff vs real .NET) =="
compile_console "$out" "$project"

native_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_env_native.XXXXXX")
dotnet_dir=$(mktemp -d "${TMPDIR:-/tmp}/dn2cpp_env_dotnet.XXXXXX")
trap 'rm -rf "$native_dir" "$dotnet_dir"' EXIT

# DN2CPP_PROBE: a set value; DN2CPP_EMPTY: explicitly empty (defined, "" not null);
# DN2CPP_UNSET: left undefined so getenv returns null. Same env for both runs.
# The program ends in Environment.Exit(3), so the exit status is part of the
# contract: it proves the funnel carries the code through and flushes stdout, which
# `_Exit` does not do on its own.
set +e
native=$(env DN2CPP_PROBE='dn2cpp-value-42' DN2CPP_EMPTY= "./$out/$project" "$native_dir")
native_code=$?
expected=$(env DN2CPP_PROBE='dn2cpp-value-42' DN2CPP_EMPTY= dotnet "$app" "$dotnet_dir")
expected_code=$?
set -e
assert_output "$native" "$expected"
assert_exit_code "$native_code" "$expected_code"
assert_exit_code "$native_code" 3
gate_cache_commit
