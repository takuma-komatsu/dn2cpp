#!/usr/bin/env bash
# Consolidated P/Invoke gate (custom native library). Merges the former per-shape
# pinvoke-* subset gates plus runtime-prim into one multi-section program that
# P/Invokes the hand-written libdn2cpptest test library, transpiled once against
# the tree-shaken real CoreLib and diffed exactly vs real .NET. Covers primitive
# in/out, bool marshalling, byref strings, char & wide-char/string marshalling,
# string & StringBuilder, [StructLayout] structs, GetLastWin32Error, custom-lib
# linking, runtime-primitive interop, and cross-assembly P/Invoke: the
# PInvokeRefLib reference assembly declares its own [DllImport("dn2cpptest")]s.
# Both app and referenced-assembly imports use resolver/OS-loader lazy binding;
# neither contributes a static link manifest. A
# second negative arm (step 6) asserting a struct whose field carries
# a [MarshalAs] descriptor the struct marshaller does not implement (ByValTStr)
# REFUSES at transpile, naming the field — the shape used to cross as a pointer
# while the marshalled-layout model sized an inline buffer — and a third (step 7)
# asserting a width-MISMATCHED [MarshalAs] on a blittable-typed field
# ([MarshalAs(I2)] int) refuses too: it used to cross RAW through the blittable
# fast path where real .NET raises TypeLoadException (measured), and a fourth
# asserting that SysInt on a void* field is likewise refused. A fifth (step 9)
# holds FunctionPtr to a genuine function-pointer type, refusing it on void* as
# .NET does. A sixth (step 10) is the KIND half no width can answer —
# [MarshalAs(Struct)] on an ENUM field — and it runs real .NET itself for the
# verdict rather than citing a measurement.
#
# Also the system-libc section (PInvokeLibcSubset, folded in from its own gate):
# [DllImport] into libc/libSystem with blittable primitives, pointers and an ASCII
# string. These user declarations take the same resolver-first lazy path as every
# other app import; their resolver maps the logical `libc` name to the host's
# concrete C-library filename. CoreLib PAL declarations remain direct runtime calls.
# Covers int/long integer returns (abs/llabs), a double return (atof/strtod — NOT
# sqrt/pow/floor/ceil, which glibc keeps in libm, not libc.so.6; the double ARGUMENT
# crossing is PInvokeCustomLibSubset's), implicit vs explicit EntryPoint, a
# pointer argument with a pointer-sized return (strlen), a byref out-param pointer
# (strtod's end), and a void* (pointer) return aliasing its destination (memset) plus
# a pointer == pointer compare. That section used to assert a hard-coded expected
# string; folded here it is exact-diffed against real .NET instead. Its own
# DllImportResolver redirects `libc` onto ucrtbase.dll, libc.so.6 or
# libSystem.B.dylib. A bare `libc` is not a portable loader name.
#
# It adds nothing to this gate's link setup, which is why it can live here: the OS
# loader supplies libc and the lazy import writes no link token. The -L / loader-path
# machinery below is only for the custom test library.
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

echo "== 1/11 Building the native test library libdn2cpptest =="
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
    clang -O2 -fPIC -shared -pthread ${inflags[@]+"${inflags[@]}"} \
        samples/native/dn2cpptest/dn2cpptest.c -o "$libtest"
fi

echo "== 2/11 Locating the real CoreLib =="
corelib=$(locate_corelib)
echo "corlib: $corelib"

echo "== 3/11 Building app assembly (PInvokeRefLib rides along via ProjectReference) =="
build_proj "samples/dotnet/$PROJECT/$PROJECT.csproj"
app="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/$PROJECT.dll"
reflib="samples/dotnet/$PROJECT/bin/$CONFIG/$TFM/PInvokeRefLib.dll"

echo "== 4/11 Transpiling app + real CoreLib + PInvokeRefLib (lazy P/Invoke) =="
invoke_cli "$app" -r "$corelib" -r "$reflib" -o "$OUT"

echo "== 5/11 Asserting lazy imports stay out of the static link manifests =="
[ ! -e "$OUT/pinvoke-libs.txt" ] \
    || { echo "FAIL: lazy imports emitted pinvoke-libs.txt" >&2; exit 1; }
[ ! -e "$OUT/pinvoke-symbols.txt" ] \
    || { echo "FAIL: lazy imports emitted pinvoke-symbols.txt" >&2; exit 1; }

# Entry-point selectors are ordinal metadata matches. Only the selected symbol
# enters the static manifests; other imports of the same module stay lazy.
DIRECT_SELECTOR_OUT="artifacts/pinvokenative-direct-selector"
invoke_cli "$app" -r "$corelib" -r "$reflib" \
    --direct-pinvoke 'dn2cpptest!dn2cpptest_add' -o "$DIRECT_SELECTOR_OUT"
[ "$(tr -d '\r' < "$DIRECT_SELECTOR_OUT/pinvoke-libs.txt")" = 'dn2cpptest' ] \
    || { echo "FAIL: entry-point selector emitted the wrong library manifest" >&2; exit 1; }
[ "$(tr -d '\r' < "$DIRECT_SELECTOR_OUT/pinvoke-symbols.txt")" = $'dn2cpptest\tdn2cpptest_add' ] \
    || { echo "FAIL: entry-point selector emitted symbols beyond its exact match" >&2; exit 1; }

echo "== 6/11 Asserting the ByValTStr struct-field crossing refuses at transpile =="
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

echo "== 7/11 Asserting the width-mismatched [MarshalAs] struct field refuses at transpile =="
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

echo "== 8/11 Asserting [MarshalAs(SysInt)] on void* refuses at transpile =="
# SUBJECT: SysInt is not a valid descriptor for a void* field. Numeric width agreement is
# insufficient: real .NET raises TypeLoadException when the struct crosses a P/Invoke
# boundary. Same non-cached, before-the-cache-gate doctrine as the other refusal arms.
build_proj samples/dotnet/PInvokePointerDescriptorBad/PInvokePointerDescriptorBad.csproj
pd_app="samples/dotnet/PInvokePointerDescriptorBad/bin/$CONFIG/$TFM/PInvokePointerDescriptorBad.dll"
PD_OUT="artifacts/pinvokenative-pointerdescriptor-neg"
rm -rf "$PD_OUT"
pd_rc=0
pd_err=$(invoke_cli "$pd_app" -r "$corelib" -o "$PD_OUT" 2>&1) || pd_rc=$?
if [ "$pd_rc" -ne 2 ]; then
    echo "FAIL: the pointer-descriptor struct crossing transpiled with exit $pd_rc (want 2: the descriptor refusal)" >&2
    printf '%s\n' "$pd_err" | tail -3 >&2
    exit 1
fi
grep -q "error: .*DescribedPointer.*'Value' carries \[MarshalAs(UnmanagedType.SysInt)\]" <<<"$pd_err" \
    || { echo "FAIL: the refusal did not name the field and its SysInt descriptor" >&2; printf '%s\n' "$pd_err" | tail -3 >&2; exit 1; }
echo "pointer-descriptor crossing refusal OK: exit 2, named the field and descriptor"

echo "== 9/11 Asserting [MarshalAs(FunctionPtr)] on a void* parameter refuses at transpile =="
# SUBJECT: FunctionPtr is honoured only on a genuine function-pointer type (the
# PInvokeMarshalAsSubset positive rows); on void* real .NET raises
# MarshalDirectiveException at the call ("pointers must not have a MarshalAs attribute
# set"), so the acceptance must not widen to every pointer-shaped type. Same non-cached,
# before-the-cache-gate doctrine as the other refusal arms: a regression here leaves the
# positive transpile's bytes identical, so a warm cache would replay green right over it.
build_proj samples/dotnet/PInvokeFnPtrDescriptorBad/PInvokeFnPtrDescriptorBad.csproj
fpd_app="samples/dotnet/PInvokeFnPtrDescriptorBad/bin/$CONFIG/$TFM/PInvokeFnPtrDescriptorBad.dll"
FPD_OUT="artifacts/pinvokenative-fnptrdescriptor-neg"
rm -rf "$FPD_OUT"
fpd_rc=0
fpd_err=$(invoke_cli "$fpd_app" -r "$corelib" -o "$FPD_OUT" 2>&1) || fpd_rc=$?
if [ "$fpd_rc" -ne 2 ]; then
    echo "FAIL: the FunctionPtr-on-void* transpile exited $fpd_rc (want 2: the descriptor refusal)" >&2
    printf '%s\n' "$fpd_err" | tail -3 >&2
    exit 1
fi
grep -q "error: .*\[MarshalAs(UnmanagedType.FunctionPtr)\] on Void\* is not supported" <<<"$fpd_err" \
    || { echo "FAIL: the refusal did not name the FunctionPtr descriptor and the void* parameter" >&2; printf '%s\n' "$fpd_err" | tail -3 >&2; exit 1; }
echo "FunctionPtr-on-void* refusal OK: exit 2, named the descriptor and the parameter type"

echo "== 10/11 Asserting [MarshalAs(Struct)] on an enum field refuses at transpile =="
# SUBJECT: the KIND half of the same descriptor gate, which no width can answer.
# Struct names the inline-struct form and an enum marshals as its underlying integer,
# so real .NET refuses the struct outright — and this arm MEASURES that rather than
# asserting it from a comment, because the same row now decides both the P/Invoke
# classifier and the marshalled-layout model, and a drift between them is silent.
build_proj samples/dotnet/PInvokeStructDescriptorBad/PInvokeStructDescriptorBad.csproj
sd_app="samples/dotnet/PInvokeStructDescriptorBad/bin/$CONFIG/$TFM/PInvokeStructDescriptorBad.dll"
# The oracle: neither entry point exists, so the exception NAMES the stage that failed.
# TypeLoadException means the marshaller rejected the struct before it ever looked the symbol
# up; the Ok control gets past the marshaller and dies at the lookup instead (no such symbol
# on POSIX, no such library on Windows, where libc is not a module name). Drop the control and
# the arm passes on any exception at all, saying nothing about the descriptor.
sd_oracle=$(dotnet "$sd_app" 2>&1) || true
grep -q "^struct-on-enum=TypeLoadException" <<<"$sd_oracle" \
    || { echo "FAIL: real .NET did not refuse [MarshalAs(Struct)] on an enum field" >&2; printf '%s\n' "$sd_oracle" | tail -3 >&2; exit 1; }
grep -qE "^struct-on-value=(EntryPointNotFoundException|DllNotFoundException)" <<<"$sd_oracle" \
    || { echo "FAIL: the control did not get past the marshaller, so TypeLoadException proves nothing" >&2; printf '%s\n' "$sd_oracle" | tail -3 >&2; exit 1; }
echo "oracle OK: real .NET raises TypeLoadException on the enum arm, gets past the marshaller on the control"
SD_OUT="artifacts/pinvokenative-structdescriptor-neg"
rm -rf "$SD_OUT"
sd_rc=0
sd_err=$(invoke_cli "$sd_app" -r "$corelib" -o "$SD_OUT" 2>&1) || sd_rc=$?
if [ "$sd_rc" -ne 2 ]; then
    echo "FAIL: the Struct-on-enum struct crossing transpiled with exit $sd_rc (want 2: the descriptor refusal)" >&2
    printf '%s\n' "$sd_err" | tail -3 >&2
    exit 1
fi
grep -q "error: .*DescribedEnum.*'T' carries \[MarshalAs(UnmanagedType.Struct)\]" <<<"$sd_err" \
    || { echo "FAIL: the refusal did not name the field and its Struct descriptor" >&2; printf '%s\n' "$sd_err" | tail -3 >&2; exit 1; }
echo "Struct-on-enum crossing refusal OK: exit 2, named the field and descriptor"

echo "== asserting an unmodelled framework QCall stays behind the refusal boundary =="
build_proj samples/dotnet/PInvokeFrameworkQCallBad/PInvokeFrameworkQCallBad.csproj
qcall_app="samples/dotnet/PInvokeFrameworkQCallBad/bin/$CONFIG/$TFM/System.PInvokeFrameworkQCallBad.dll"
QCALL_OUT="artifacts/pinvokenative-framework-qcall-neg"
rm -rf "$QCALL_OUT"
qcall_rc=0
qcall_err=$(invoke_cli "$qcall_app" -r "$corelib" -o "$QCALL_OUT" 2>&1) || qcall_rc=$?
if [ "$qcall_rc" -ne 2 ]; then
    echo "FAIL: an unmodelled framework QCall transpiled with exit $qcall_rc (want 2)" >&2
    printf '%s\n' "$qcall_err" | tail -3 >&2
    exit 1
fi
grep -q "error: .*PInvokeFrameworkQCallBad.*Probe.*no IL body and no intrinsic mapping" <<<"$qcall_err" \
    || { echo "FAIL: the framework-QCall refusal did not name the unmodelled method" >&2; printf '%s\n' "$qcall_err" | tail -3 >&2; exit 1; }
echo "framework QCall refusal OK: exit 2, before any runtime library lookup"

echo "== dynamic resolver and NativeLibrary APIs (exact diff vs real .NET) =="
build_proj samples/dotnet/PInvokeDynamic/PInvokeDynamic.csproj
dynamic_app="samples/dotnet/PInvokeDynamic/bin/$CONFIG/$TFM/PInvokeDynamic.dll"
dynamic_ref="samples/dotnet/PInvokeDynamic/bin/$CONFIG/$TFM/PInvokeRefLib.dll"
dynamic_out="artifacts/pinvokedynamic"
invoke_cli "$dynamic_app" -r "$corelib" -r "$dynamic_ref" -o "$dynamic_out"
[ ! -e "$dynamic_out/pinvoke-libs.txt" ] \
    || { echo "FAIL: dynamic imports emitted pinvoke-libs.txt" >&2; exit 1; }
[ ! -e "$dynamic_out/pinvoke-symbols.txt" ] \
    || { echo "FAIL: dynamic imports emitted pinvoke-symbols.txt" >&2; exit 1; }
if [ "$DN2CPP_OS" = macos ]; then
    # The selector matches the raw metadata name; only the emitted binding sees
    # the Darwin ABI bridge rewrite.
    darwin_selector_out="artifacts/pinvokedynamic-darwin-selector"
    rm -rf "$darwin_selector_out"
    invoke_cli "$dynamic_app" -r "$corelib" -r "$dynamic_ref" \
        --direct-pinvoke 'libc!setattrlist' -o "$darwin_selector_out"
    grep -Fq 'DN2CPP_PINVOKE_ASM("dn2cpp_setattrlist")' \
        "$darwin_selector_out/generated.h" \
        || { echo "FAIL: raw Darwin selector did not emit the rewritten ABI symbol" >&2; exit 1; }
    if grep -Fq 'DN2CPP_PINVOKE_ASM("setattrlist")' \
        "$darwin_selector_out/generated.h"; then
        echo "FAIL: Darwin selector bound the raw metadata symbol" >&2
        exit 1
    fi
fi
compile_console "$dynamic_out" PInvokeDynamic
assembly_lib_name=$(lib_name dn2cpptest_assembly)
cp -f "$libtest" "$dynamic_out/$assembly_lib_name"
dynamic_managed_dir=$(dirname "$dynamic_app")
cp -f "$libtest" "$dynamic_managed_dir/$assembly_lib_name"
if [ "$DN2CPP_OS" = windows ]; then
    libdir_posix=$(cygpath -u "$LIBDIR")
    libtest_native=$(native_path "$libtest")
    dynamic_native=$(env "PATH=$libdir_posix:$PATH" DN2CPP_PINVOKE_TEST_LIBRARY="$libtest_native" "./$dynamic_out/PInvokeDynamic")
    dynamic_dotnet=$(env "PATH=$libdir_posix:$PATH" DN2CPP_PINVOKE_TEST_LIBRARY="$libtest_native" dotnet "$dynamic_app")
else
    dynamic_native=$(env "$LIB_PATH_ENV=$LIBDIR" DN2CPP_PINVOKE_TEST_LIBRARY="$libtest" "./$dynamic_out/PInvokeDynamic")
    dynamic_dotnet=$(env "$LIB_PATH_ENV=$LIBDIR" DN2CPP_PINVOKE_TEST_LIBRARY="$libtest" dotnet "$dynamic_app")
fi
assert_output "$dynamic_native" "$dynamic_dotnet"

# The native test library's source dir is a key input beyond the transpile
# surface: the run dlopens what step 1 built from it.
if gate_cache_check "$OUT" "pinvoke-native|$corelib" \
        "$app" "$reflib" "${app%.dll}.runtimeconfig.json" "${app%.dll}.deps.json" \
        samples/native/dn2cpptest; then
    gate_cache_hit_msg
    exit 0
fi

echo "== 11/11 Linking against libdn2cpptest and running (exact diff vs real .NET) =="
compile_console "$OUT" "$PROJECT"
# Both POSIX binaries resolve the lazy import through the platform loader, so
# the test library directory is supplied through DYLD_LIBRARY_PATH / LD_LIBRARY_PATH.
# Windows has no rpath equivalent, so both the native binary and the oracle need
# $LIBDIR on PATH
# (the DLL search path) at run time — prepended, not replacing, since `env`'s
# own PATH-based lookup of its argv[0] (dotnet/the native exe) also uses the
# new PATH.
if [ "$DN2CPP_OS" = windows ]; then
    # POSIX form, or a C:-style $PWD's drive colon splits the PATH list.
    libdir_posix=$(cygpath -u "$LIBDIR")
    native_out=$(env "PATH=$libdir_posix:$PATH" "./$OUT/$PROJECT")
    dotnet_out=$(env "PATH=$libdir_posix:$PATH" dotnet "$app")
else
    native_out=$(env "$LIB_PATH_ENV=$LIBDIR" "./$OUT/$PROJECT")
    dotnet_out=$(env "$LIB_PATH_ENV=$LIBDIR" dotnet "$app")
fi
assert_output "$native_out" "$dotnet_out"
gate_cache_commit
