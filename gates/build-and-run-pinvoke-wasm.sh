#!/usr/bin/env bash
# WASM-axis sibling of the consolidated P/Invoke gate: the SAME multi-section
# PInvokeNative program (every desktop section, cross-assembly PInvokeRefLib
# included), transpiled once against the tree-shaken real CoreLib with
# `--direct-pinvoke '*'`, then built by em++ into a wasm32 module that
# statically links libdn2cpptest.a — dn2cpptest.c compiled by emcc into a
# static archive, resolved through the same pinvoke-libs.txt ->
# target_link_libraries token plus a gate-supplied -L via DN2CPP_APP_LINK_FLAGS
# that the native gate uses — run under node, and diffed exactly against real
# .NET (`dotnet $app` against a host build of the same C source; on Windows the
# oracle is an apphost carrying a UTF-8 ANSI code page instead — see the run
# step, which states why the manifest is the only lever and why it is scoped to
# this gate).
#
# What this axis uniquely proves, beyond "the link plumbing reaches wasm":
# wasm indirect calls are STRICTLY type-checked — a signature mismatch between
# a stored [UnmanagedCallersOnly] function pointer (the reverse-call sections,
# the installed-vtable struct) and the C side's call through it is a trap, not
# UB — and the singleton-aggregate ABI of the single-pointer-field struct
# return must flatten identically in the generated C++ extern declaration and
# the archive's C definition.
#
# PInvokeExplicitUnionSubset's size checks ride this axis for a reason that has nothing
# to do with P/Invoke: an explicit layout's emitted union FIXES its own total, so a total
# decided by a pointer field can only be wrong on a 32-bit target — and this is the only
# gate that builds such a struct for one. SequentialSizeSubset is here for the same
# reason and is the same subject: a [StructLayout(Size = N)] sequential value type reaches
# N through an emitted trailing pad, which likewise fixes the total, and whose width the
# 64-bit reading alone cannot state when the pad is 0 there and positive at 32.
#
# The flagless-refusal negative arm stays in build-and-run-pinvoke-native.sh
# alone: the refusal is target-independent (it fires at transpile time), so
# re-proving it per axis would re-run the transpile for no new information.
#
# Machines without the Emscripten toolchain (or node) skip: the suite stays
# usable without emscripten, but the runner reports the gate as SKIPPED rather
# than passed (gate_skip in _common.sh).
#
# It also carries PInvokeMarshalLayoutSubset, whose SUBJECT is not a marshalling shape at
# all: it is the MARSHALLED-LAYOUT MODEL — Marshal.SizeOf / Marshal.OffsetOf over
# non-blittable structs — checked against the layout the C compiler gave the SAME structs
# in libdn2cpptest, which reports its own sizeof/offsetof through dn2cpptest_layout_query.
# That third opinion is the point: two models can agree and both be wrong about the ABI.
# On this axis the ABI is the 32-bit one, and the LINK is what checks it: the tn_<Name>
# static_asserts CppEmitter stamps carry the model's size and offsets as sizeof(void*)
# expressions, which em++ evaluates at 4 — per struct, before the program runs. The oracle
# here is still 64-bit, so the section's pointer-bearing rows cross as relations against
# IntPtr.Size and as agreement verdicts rather than as numbers.
# Do not prune it as a duplicate of PInvokeMarshalStructSubset, which asserts the values
# that cross the boundary rather than the layout they cross in.
source "$(dirname "$0")/_common.sh"

# --no-bundled: an -O-less link needs the -debug archives the frozen bundle lacks.
dn2cpp_emsdk_resolve --no-bundled
for tool in em++ emcc emar emcmake node; do
    if ! command -v "$tool" >/dev/null 2>&1; then
        gate_skip "$tool not found — no Emscripten toolchain to build wasm with"
    fi
done

export WASM=1

PROJECT=PInvokeNative
OUT="artifacts/pinvokenative-wasm"
LIBDIR="$PWD/$OUT/lib"            # wasm static archive for the module link
HOSTLIBDIR="$PWD/$OUT/lib-host"   # host shared library for the dotnet oracle

echo "== 1/5 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 2/5 Building app assembly (PInvokeRefLib rides along via ProjectReference) =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
app="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"
reflib="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/PInvokeRefLib.dll"

echo "== 3/5 Transpiling app + real CoreLib + PInvokeRefLib (--direct-pinvoke '*') =="
invoke_cli "$app" -r "$corelib" -r "$reflib" --direct-pinvoke '*' -o "$OUT"

# The native test library's source dir is a key input beyond the transpile
# surface: both the linked archive and the oracle's host library are built
# from it below. (The emcc/node versions are already in the key — the
# gate-cache WASM arm records them.)
if gate_cache_check "$OUT" "pinvoke-wasm|$corelib" \
        "$app" "$reflib" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" \
        samples/native/dn2cpptest gates/_utf8-acp.manifest; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 4/5 Building libdn2cpptest (wasm static archive + host oracle library) =="
mkdir -p "$LIBDIR" "$HOSTLIBDIR"
emcc -O2 -c samples/native/dn2cpptest/dn2cpptest.c -o "$LIBDIR/dn2cpptest.o"
emar rcs "$LIBDIR/libdn2cpptest.a" "$LIBDIR/dn2cpptest.o"
# The oracle side runs real .NET on the HOST, so it needs a host shared
# library beside the wasm archive — same recipe as the native pinvoke gate.
hostlib="$HOSTLIBDIR/$(lib_name dn2cpptest)"
if is_msvc_compiler; then
    # See the native pinvoke gate: cl.exe is driven through the tiny CMake
    # project (samples/native/dn2cpptest/CMakeLists.txt) to sidestep Git
    # Bash's argv mangling and MSVC's export-nothing default.
    testlib_builddir="$OUT/.cmake-testlib"
    # Through _cmake_step, not `>/dev/null`: ninja funnels each edge's compiler
    # diagnostics to its own stdout, so discarding it would leave a cl.exe error
    # here killing the gate under `set -e` with nothing printed past the banner
    # above. mkdir first — the log lands beside the build dir, which cmake has
    # not created yet on a cold run.
    mkdir -p "$testlib_builddir"
    _cmake_step "$testlib_builddir/dn2cpp-configure.log" "configuring the host oracle library build" \
        "$CMAKE" -S samples/native/dn2cpptest -B "$testlib_builddir" -G Ninja \
        -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER="${CMAKE_CXX_COMPILER}" || exit 1
    _cmake_step "$testlib_builddir/dn2cpp-build.log" "building the host oracle library" \
        "$CMAKE" --build "$testlib_builddir" || exit 1
    cp -f "$testlib_builddir/$(lib_name dn2cpptest)" "$hostlib"
else
    inflags=(); while IFS= read -r f; do inflags+=("$f"); done < <(install_name_flags "$hostlib")
    clang -O2 -fPIC -shared ${inflags[@]+"${inflags[@]}"} \
        samples/native/dn2cpptest/dn2cpptest.c -o "$hostlib"
fi

echo "== 5/5 Linking the archive, running under node (exact diff vs real .NET) =="
# The archive resolves through pinvoke-libs.txt's bare `dn2cpptest` token
# (-> -ldn2cpptest on wasm-ld); the -L below is the search path that makes
# the token find $LIBDIR/libdn2cpptest.a. No rpath machinery: a static
# archive is consumed at link time, nothing is loaded at run time.
# Routed through native_path (_common.sh) rather than embedded as MSYS's own
# /d/... spelling: this is a single -Ldir token today, which MSYS happens to
# convert on its own, but that is a property of staying single-token, not a
# guarantee this call makes.
DN2CPP_EXTRA_LINK_FLAGS="-L$(native_path "$LIBDIR")" compile_console_wasm "$OUT" "$PROJECT"
if [ "$DN2CPP_OS" = windows ]; then
    # `Ansi` is a PLATFORM encoding, and the two sides of this diff sit on two
    # platforms: Emscripten has no CP_ACP, so the wasm PAL marshals UTF-8, while
    # real .NET on Windows marshals through CP_ACP with best-fit substitution.
    # dn2cpp's NATIVE Windows lane does the latter deliberately, and
    # build-and-run-pinvoke-native.sh asserts exactly that against a stock
    # oracle on this same host — so it is the ORACLE that moves here, and only
    # here: the subject cannot, and that gate's oracle must keep the host's real
    # ANSI code page.
    #
    # A manifest's activeCodePage is the only lever — there is no SetACP, no
    # environment variable and no runtimeconfig knob — and Windows reads it from
    # the manifest of the PROCESS IMAGE. Three near-misses, each measured on a
    # cp932 host: an external `<exe>.manifest` beside the binary is ignored for
    # this setting, `-p:ApplicationManifest=` is ignored for a .NET console app,
    # and `dotnet <dll>` reads dotnet.exe's manifest rather than one of ours.
    # What works is `-p:Win32Manifest=` — it reaches csc's /win32manifest, and
    # the apphost inherits the managed assembly's Win32 resources — plus running
    # that apphost .exe instead of the dll.
    #
    # Private intermediate and output trees: sharing either would hand the
    # native gate an apphost it never asked for. The intermediate is set through
    # IntermediateOutputPath (the leaf) and NOT BaseIntermediateOutputPath,
    # which looks like the natural knob and breaks the build: the SDK derives
    # DefaultItemExcludes from the BASE path, so moving it drops the stock obj/
    # out of the exclude set and the compile then picks up BOTH trees' generated
    # AssemblyInfo.cs — a wall of CS0579 duplicate-attribute errors that names
    # nothing to do with the path. Keeping the base at obj/ leaves one exclude
    # covering both, and obj/ is already ignored by git.
    #
    # The build runs even under DN2CPP_SKIP_BUILD, because the orchestrator's
    # prebuild phase knows only this project's stock configuration.
    oracle_dir="$PWD/$OUT/oracle-utf8acp"
    dotnet build "samples/dotnet/$PROJECT/$PROJECT.csproj" -c "$CONFIG" --nologo -v q \
        -o "$oracle_dir" -p:Win32Manifest="$PWD/gates/_utf8-acp.manifest" \
        -p:IntermediateOutputPath=obj/utf8acp/
    # The oracle's DLL lookup uses PATH — prepended in POSIX form (a C:-style
    # drive colon would split the list); node needs no library path at all.
    hostlibdir_posix=$(cygpath -u "$HOSTLIBDIR")
    native_out=$(node "./$OUT/$PROJECT.js")
    dotnet_out=$(env "PATH=$hostlibdir_posix:$PATH" "$oracle_dir/$PROJECT.exe")
else
    native_out=$(node "./$OUT/$PROJECT.js")
    dotnet_out=$(env "$LIB_PATH_ENV=$HOSTLIBDIR" dotnet "$app")
fi
assert_output "$native_out" "$(strip_cr_win "$dotnet_out")"
gate_cache_commit
