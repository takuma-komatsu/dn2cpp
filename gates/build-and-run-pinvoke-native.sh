#!/usr/bin/env bash
# Consolidated P/Invoke gate (custom native library). Merges the former per-shape
# pinvoke-* subset gates plus runtime-prim into one multi-section program that
# P/Invokes the hand-written libdn2cpptest test library, transpiled once against
# the tree-shaken real CoreLib and diffed exactly vs real .NET. Covers primitive
# in/out, bool marshalling, byref strings, char & wide-char/string marshalling,
# string & StringBuilder, [StructLayout] structs, GetLastWin32Error, custom-lib
# linking, runtime-primitive interop, and cross-assembly P/Invoke: the
# PInvokeRefLib reference assembly declares its own [DllImport("dn2cpptest")]s,
# admitted by `--pinvoke-module dn2cpptest` — with a negative arm asserting the
# flagless transpile still refuses them (the opt-in stays an opt-in), and a
# second negative arm (step 6) asserting a struct whose field carries
# a [MarshalAs] descriptor the struct marshaller does not implement (ByValTStr)
# REFUSES at transpile, naming the field — the shape used to cross as a pointer
# while the marshalled-layout model sized an inline buffer — and a third (step 7)
# asserting a width-MISMATCHED [MarshalAs] on a blittable-typed field
# ([MarshalAs(I2)] int) refuses too: it used to cross RAW through the blittable
# fast path where real .NET raises TypeLoadException (measured).
#
# Also the system-libc section (PInvokeLibcSubset, folded in from its own gate):
# [DllImport] into always-linked libc/libSystem with blittable primitives /
# pointers only. Each pinvokeimpl method lowers to a direct native call into an
# extern "C" entry point declared once by the emitter (aliased via an asm label so
# it never collides with the libc prototype the runtime header already includes).
# Covers int/long integer returns (abs/llabs), double ABI (sqrt/pow/floor/ceil),
# implicit vs explicit EntryPoint, a pointer argument with a pointer-sized return
# (strlen), and a void* (pointer) return aliasing its destination (memset) plus a
# pointer == pointer compare. That section used to assert a hard-coded expected
# string; folded here it is exact-diffed against real .NET instead. Its own
# DllImportResolver redirects `libc` onto ucrtbase.dll on Windows, where the
# platform C library carries those symbols under a name no default probe reaches —
# the ORACLE needs it; the transpiled binary link-resolves them and drops it.
#
# It adds NOTHING to this gate's link setup, which is why it can live here: `libc`
# is in Compilation.IsRuntimeProvidedPInvokeModule, so the transpile admits it with
# no --pinvoke-module opt-in, and the emitter writes no link token for a
# runtime-provided module — the retired gate's own pinvoke-libs.txt was empty, and
# this gate's still reads `dn2cpptest` alone. The -L/-rpath machinery below is the
# custom library's and stays untouched.
#
# Former gates: pinvoke-{array,bool,byrefstring,char,customlib,lasterror,string,
# stringbuilder,struct,widechararray,widestring}, runtime-prim, pinvoke-libc-subset.
#
# It also carries PInvokeMarshalLayoutSubset, whose SUBJECT is not a marshalling shape at
# all: it is the MARSHALLED-LAYOUT MODEL — Marshal.SizeOf / Marshal.OffsetOf over
# non-blittable structs — checked against the layout the C compiler gave the SAME structs
# in libdn2cpptest, which reports its own sizeof/offsetof through dn2cpptest_layout_query.
# That third opinion is the point: two models can agree and both be wrong about the ABI.
# Do not prune it as a duplicate of PInvokeMarshalStructSubset, which asserts the values
# that cross the boundary rather than the layout they cross in.
source "$(dirname "$0")/_common.sh"

PROJECT=PInvokeNative
OUT="artifacts/pinvokenative"
LIBDIR="$PWD/$OUT/lib"

echo "== 1/8 Building the native test library libdn2cpptest =="
mkdir -p "$LIBDIR"
libtest="$LIBDIR/$(lib_name dn2cpptest)"
if is_msvc_compiler; then
    # A direct cl.exe invocation from here would need to fight Git Bash's argv
    # path-mangling (MSVC switches start with '/') and MSVC exports nothing
    # from a DLL by default — go through the tiny CMake project instead (see
    # samples/native/dn2cpptest/CMakeLists.txt), the same backend every other
    # gate already requires.
    testlib_builddir="$OUT/.cmake-testlib"
    # Through _cmake_step, not `>/dev/null`: ninja funnels each edge's compiler
    # diagnostics to its own stdout, so discarding it would leave a cl.exe error
    # here killing the gate under `set -e` with nothing printed past the banner
    # above. mkdir first — the log lands beside the build dir, which cmake has
    # not created yet on a cold run.
    mkdir -p "$testlib_builddir"
    _cmake_step "$testlib_builddir/dn2cpp-configure.log" "configuring the test library build" \
        "$CMAKE" -S samples/native/dn2cpptest -B "$testlib_builddir" -G Ninja \
        -DCMAKE_BUILD_TYPE=Release -DCMAKE_C_COMPILER="${CMAKE_CXX_COMPILER}" || exit 1
    _cmake_step "$testlib_builddir/dn2cpp-build.log" "building the test library" \
        "$CMAKE" --build "$testlib_builddir" || exit 1
    cp -f "$testlib_builddir/$(lib_name dn2cpptest)" "$libtest"
    # The import library link.exe needs at app-link time (libpath_flag below
    # points the linker at $LIBDIR, where target_link_libraries's bare
    # "dn2cpptest" token then resolves to dn2cpptest.lib).
    cp -f "$testlib_builddir/dn2cpptest.lib" "$LIBDIR/dn2cpptest.lib"
else
    inflags=(); while IFS= read -r f; do inflags+=("$f"); done < <(install_name_flags "$libtest")
    clang -O2 -fPIC -shared ${inflags[@]+"${inflags[@]}"} \
        samples/native/dn2cpptest/dn2cpptest.c -o "$libtest"
fi

echo "== 2/8 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 3/8 Building app assembly (PInvokeRefLib rides along via ProjectReference) =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
app="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"
reflib="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/PInvokeRefLib.dll"

echo "== 4/8 Transpiling app + real CoreLib + PInvokeRefLib (--pinvoke-module dn2cpptest) =="
invoke_cli "$app" -r "$corelib" -r "$reflib" --pinvoke-module dn2cpptest -o "$OUT"

echo "== 5/8 Asserting the flagless transpile refuses the cross-assembly import =="
# Deliberately NOT cached, and BEFORE the cache gate (the trim-reflection
# typo-arm doctrine): the refusal leaves no output surface to key on, and the
# regression this arm pins — a referenced module's [DllImport] lowering without
# the opt-in (say, its module name hardcoded into IsRuntimeProvidedPInvokeModule)
# — leaves the POSITIVE transpile's bytes identical, so a warm cache would
# replay green right over it. Re-running the ~seconds transpile every time is
# the insurance that keeps the opt-in an opt-in.
NEG_OUT="artifacts/pinvokenative-noflag"
rm -rf "$NEG_OUT"
set +e
neg_err=$(invoke_cli "$app" -r "$corelib" -r "$reflib" -o "$NEG_OUT" 2>&1)
neg_code=$?
set -e
printf '%s\n' "$neg_err" | tail -2
if [ "$neg_code" -ne 2 ]; then
    echo "FAIL: flagless cross-assembly [DllImport] transpile exited $neg_code (want 2: the no-IL-body refusal)" >&2
    exit 1
fi
grep -q "error: .*PInvokeRefLib.*dn2cpptest_.*no IL body and no intrinsic mapping" <<<"$neg_err" \
    || { echo "FAIL: the refusal did not name the cross-assembly import" >&2; exit 1; }
echo "flagless refusal OK: exit 2, named the cross-assembly import"

echo "== 6/8 Asserting the ByValTStr struct-field crossing refuses at transpile =="
# SUBJECT: the P/Invoke STRUCT-FIELD [MarshalAs] descriptor gate
# (CppTypes.StructFieldDescriptorSupported), not another marshalling shape. A
# [MarshalAs(ByValTStr)] string field asks for an INLINE character buffer; the
# tn_ emitter lays a string field out as a pointer and never read the
# descriptor, while the marshalled-layout model sizes the inline
# buffer — and the disagreement, where it surfaced at all, was the model's
# cross-check static_assert failing the C++ COMPILE. The transpile must refuse
# instead, naming the field and the descriptor; Marshal.SizeOf/OffsetOf over
# the same shape keep ANSWERING (SizeOfOffsetOfSubset's byvaltstr rows). Same
# non-cached, before-the-cache-gate doctrine as step 5, for the same reason:
# a regression here leaves the positive transpile's bytes identical, so a warm
# cache would replay green right over it.
build_proj samples/dotnet/PInvokeByValTStrBad/PInvokeByValTStrBad.csproj
bvt_app="samples/dotnet/PInvokeByValTStrBad/bin/$CONFIG/$TFM/PInvokeByValTStrBad.dll"
BVT_OUT="artifacts/pinvokenative-byvaltstr-neg"
rm -rf "$BVT_OUT"
bvt_rc=0
bvt_err=$(invoke_cli "$bvt_app" -r "$corelib" -o "$BVT_OUT" 2>&1) || bvt_rc=$?
if [ "$bvt_rc" -ne 2 ]; then
    echo "FAIL: the ByValTStr-field struct crossing transpiled with exit $bvt_rc (want 2: the descriptor refusal)" >&2
    printf '%s\n' "$bvt_err" | tail -3 >&2
    exit 1
fi
grep -q "error: .*FixedName.*'Name' carries \[MarshalAs(UnmanagedType.ByValTStr)\]" <<<"$bvt_err" \
    || { echo "FAIL: the refusal did not name the field and its ByValTStr descriptor" >&2; printf '%s\n' "$bvt_err" | tail -3 >&2; exit 1; }
echo "ByValTStr crossing refusal OK: exit 2, named the field and descriptor"

echo "== 7/8 Asserting the width-mismatched [MarshalAs] struct field refuses at transpile =="
# SUBJECT: the SAME descriptor gate asked by IsBlittableStruct —
# a width-MISMATCHED [MarshalAs] on a blittable-typed field ([MarshalAs(I2)] int)
# used to leave the struct on the blittable fast path and cross RAW, where real
# .NET raises TypeLoadException ("Invalid managed/unmanaged type combination") in
# every position (by value, byref, nested; measured). The width-MATCHING no-ops
# ([MarshalAs(I4)] int, [MarshalAs(Struct)]) still pass as blittable. Same
# non-cached, before-the-cache-gate doctrine as steps 5 and 6.
build_proj samples/dotnet/PInvokeWidthMismatchBad/PInvokeWidthMismatchBad.csproj
wm_app="samples/dotnet/PInvokeWidthMismatchBad/bin/$CONFIG/$TFM/PInvokeWidthMismatchBad.dll"
WM_OUT="artifacts/pinvokenative-widthmismatch-neg"
rm -rf "$WM_OUT"
wm_rc=0
wm_err=$(invoke_cli "$wm_app" -r "$corelib" -o "$WM_OUT" 2>&1) || wm_rc=$?
if [ "$wm_rc" -ne 2 ]; then
    echo "FAIL: the width-mismatched-descriptor struct crossing transpiled with exit $wm_rc (want 2: the descriptor refusal)" >&2
    printf '%s\n' "$wm_err" | tail -3 >&2
    exit 1
fi
grep -q "error: .*MisWidth.*'X' carries \[MarshalAs(UnmanagedType.I2)\]" <<<"$wm_err" \
    || { echo "FAIL: the refusal did not name the field and its I2 descriptor" >&2; printf '%s\n' "$wm_err" | tail -3 >&2; exit 1; }
echo "width-mismatch crossing refusal OK: exit 2, named the field and descriptor"

# The native test library's source dir is a key input beyond the transpile
# surface: the run dlopens what step 1 built from it.
if gate_cache_check "$OUT" "pinvoke-native|$corelib" \
        "$app" "$reflib" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" \
        samples/native/dn2cpptest; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 8/8 Linking against libdn2cpptest and running (exact diff vs real .NET) =="
extra_link_flags="$(libpath_flag "$LIBDIR")"
consumer_rpath="$(consumer_rpath_flags "$LIBDIR")"
[ -n "$consumer_rpath" ] && extra_link_flags="$extra_link_flags $consumer_rpath"
DN2CPP_EXTRA_LINK_FLAGS="$extra_link_flags" compile_console "$OUT" "$PROJECT"
# On macOS the native binary needs no runtime env change: install_name_flags
# baked the absolute -install_name into libdn2cpptest at link time (step 1),
# and that path is copied into the consumer's LC_LOAD_DYLIB, so the loader
# finds it unassisted. On Linux there is no such propagation — the .so only
# carries a DT_SONAME, so the consumer binary needs its own DT_RUNPATH,
# which consumer_rpath_flags adds above via DN2CPP_EXTRA_LINK_FLAGS. Either
# way, only the `dotnet` oracle needs DYLD_LIBRARY_PATH/LD_LIBRARY_PATH (a
# dedicated var — replacing it outright is harmless). Windows has no rpath
# equivalent, so BOTH the native binary and the oracle need $LIBDIR on PATH
# (the DLL search path) at run time — prepended, not replacing, since `env`'s
# own PATH-based lookup of its argv[0] (dotnet/the native exe) also uses the
# new PATH.
if [ "$DN2CPP_OS" = windows ]; then
    # POSIX form, or a C:-style $PWD's drive colon splits the PATH list.
    libdir_posix=$(cygpath -u "$LIBDIR")
    native_out=$(env "PATH=$libdir_posix:$PATH" "./$OUT/$PROJECT")
    dotnet_out=$(env "PATH=$libdir_posix:$PATH" dotnet "$app")
else
    native_out=$("./$OUT/$PROJECT")
    dotnet_out=$(env "$LIB_PATH_ENV=$LIBDIR" dotnet "$app")
fi
assert_output "$native_out" "$dotnet_out"
gate_cache_commit
