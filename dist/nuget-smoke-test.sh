#!/usr/bin/env bash
# dist/nuget-smoke-test.sh — prove the dn2cpp NuGet tool package end to end.
#
# Using ONLY what an end user gets from `dotnet tool install dn2cpp`:
#   1. `dotnet pack` the CLI into a local folder feed,
#   2. install it from that feed into a temp --tool-path (hermetic NuGet cache,
#      so a re-run never resolves a stale package from the user's global cache),
#   3. assert the packed layout: print-runtime-dir resolves into the tool store,
#      runtime/ and third_party/ are siblings there, the managed support shim
#      sits beside the CLI,
#   4. transpile the HelloWorld sample with NO -r at all — --auto-ref must
#      default the CoreLib to the live shared framework's copy,
#   5. native-build against the packed runtime alone (single CMake pass from
#      source; nothing outside the tool store is compiled or included),
#   6. run the binary and byte-diff its output against `dotnet`,
# then prove the Dn2Cpp.Build MSBuild package on top:
#   7. pack Dn2Cpp.Build into the same feed and generate a scratch console app
#      that references it from the local feed,
#   8. `dotnet publish` the scratch app with the installed tool on PATH — the
#      packaged targets must transpile + cmake-build it — and byte-diff the
#      produced native binary against `dotnet run`.
#
# NOT a regression gate: the filename is deliberately outside the
# build-and-run-*.sh glob (like the sibling smoke-test.sh), so
# run-all-gates.sh ignores it. Run manually after touching the packaging.
#
# Usage: dist/nuget-smoke-test.sh
source "$(dirname "$0")/../gates/_common.sh"   # repo-root cd, $CONFIG/$TFM/$CMAKE, build_proj

WORK=artifacts/nuget-smoke
FEED="$WORK/feed"; TOOLS="$WORK/tools"; OUT="$WORK/out"; BUILD="$WORK/build"
rm -rf "$WORK"

echo "== 1/8 pack the dn2cpp tool package =="
dotnet pack src/Dn2Cpp.Cli/Dn2Cpp.Cli.csproj -c "$CONFIG" -o "$FEED"
nupkg=$(echo "$FEED"/dn2cpp.*.nupkg)
[ -f "$nupkg" ] || { echo "error: dotnet pack produced no nupkg in $FEED" >&2; exit 1; }
PKG_VERSION="$(basename "$nupkg" | sed -e 's/^dn2cpp\.//' -e 's/\.nupkg$//')"
echo "-- packed: $nupkg"

echo "== 2/8 install from the local feed only =="
NUGET_PACKAGES="$PWD/$WORK/nuget-cache" dotnet tool install dn2cpp \
    --tool-path "$TOOLS" --add-source "$FEED" --version "$PKG_VERSION" \
    --ignore-failed-sources
DN2CPP="$TOOLS/dn2cpp"
[ -x "$DN2CPP" ] || { echo "error: installed tool shim missing: $DN2CPP" >&2; exit 1; }

echo "== 3/8 packed-layout assertions =="
RUNTIME_DIR="$("$DN2CPP" --print-runtime-dir)"
case "$RUNTIME_DIR" in
    "$PWD/$TOOLS/.store/"*) ;;
    *) echo "error: print-runtime-dir left the tool store: $RUNTIME_DIR" >&2; exit 1 ;;
esac
ROOT_DIR="$(dirname "$RUNTIME_DIR")"
# One representative file per packed subtree: the CMake entry point, the two
# runtime translation units the backends link, and EVERY vendored third-party
# tree the CMakeLists references from ${DN2CPP_ROOT}/third_party — the list here
# has to stay the whole set, because a tree that is in the csproj but untested is
# indistinguishable from one that was never added, and the miss only shows up when
# a consumer turns the option that needs it on.
#
# curl and Mbed TLS get two probes each, one of which is deliberately awkward for
# a packing bug to carry:
#   * curl/lib/Makefile.inc — extension-less, and the authoritative source list
#     curl's own lib/CMakeLists.txt transforms into CSOURCES. Without it the curl
#     subtree configures into an empty library.
#   * curl/lib/vtls/mbedtls.c — the one TLS backend actually selected, three
#     directories deep.
#   * mbedtls/library/psa_crypto_driver_wrappers_no_static.c — one of the five
#     GENERATED sources that exist only in the release asset (see
#     third_party/mbedtls/DN2CPP-VENDORED.md); the glob does not link without it,
#     and it is the file a re-vendoring from a git checkout would silently lose.
#   * mbedtls/include/psa/crypto_config.h — the PSA header curl's backend includes
#     unconditionally, in the second of Mbed TLS's two include trees.
# cacert is the CA bundle DN2CPP_USE_CURL embeds; an absent .pem is the difference
# between "HTTPS works" and "every certificate fails to verify".
for f in runtime/CMakeLists.txt \
         runtime/core/dn2cpp_runtime.cpp \
         runtime/godot/dn2cpp_godot.cpp \
         runtime/dotnetmodule/dn2cpp_dotnetmodule.cpp \
         third_party/bdwgc/extra/gc.c \
         third_party/brotli/c/include/brotli/decode.h \
         third_party/cacert/cacert.pem \
         third_party/curl/CMakeLists.txt \
         third_party/curl/lib/Makefile.inc \
         third_party/curl/lib/vtls/mbedtls.c \
         third_party/curl/include/curl/curl.h \
         third_party/highway/hwy/highway.h \
         third_party/mbedtls/library/psa_crypto_driver_wrappers_no_static.c \
         third_party/mbedtls/include/mbedtls/ssl.h \
         third_party/mbedtls/include/psa/crypto_config.h \
         third_party/nghttp2/lib/nghttp2_session.c \
         third_party/nghttp2/lib/includes/nghttp2/nghttp2.h \
         third_party/zlib/zlib.h \
         third_party/gdextension_interface.h; do
    [ -f "$ROOT_DIR/$f" ] || { echo "error: packed layout is missing $f" >&2; exit 1; }
done
# The managed support shim and the conditionally-referenced managed backends must all
# sit beside the installed CLI dll (the AppContext.BaseDirectory auto-reference; see
# TranspileDriver). Each is packed through the same csproj mechanism — a
# ReferenceOutputAssembly="false" ProjectReference with OutputItemType="Content", whose
# Pack="false" suppresses the NuGet dependency node and NOT the file — so a single
# attribute typo drops one silently, and the tool still installs and transpiles most
# programs perfectly.
#
# The DnHttp row matters most: without it in the package, a tool-installed dn2cpp cannot
# transpile an HttpClient program at all, because the transport swap's managed body is not
# on disk anywhere the transpiler can reach.
for sib in Dn2Cpp.Runtime DnZlib DnBrotli DnHttp; do
    [ -f "$ROOT_DIR/$sib.dll" ] || {
        echo "error: $sib.dll is not beside the installed CLI" >&2; exit 1; }
done
echo "-- layout OK: $ROOT_DIR"

echo "== 4/8 transpile with no -r (--auto-ref defaults the CoreLib) =="
build_proj samples/dotnet/HelloWorld/HelloWorld.csproj
APP="samples/dotnet/HelloWorld/bin/$CONFIG/$TFM/HelloWorld.dll"
"$DN2CPP" "$APP" --auto-ref -o "$OUT"

echo "== 5/8 native build against the packed runtime only =="
# The HOST's cmake, deliberately: the tool payload carries none and will not. It
# is one RID-neutral directory (tools/net10.0/any/), so three hosts' binaries
# would triple the package for every user while a RID-specific one breaks
# `dotnet tool install -g` — and a `dotnet tool` consumer runs
# `cmake -S "$(dn2cpp --print-runtime-dir)"` by hand. The toolchain bundle ships
# cmake + ninja because the EDITOR user is not a C++ developer.
"$CMAKE" -S "$RUNTIME_DIR" -B "$BUILD" -G Ninja \
    -DDN2CPP_APP_DIR="$PWD/$OUT" -DDN2CPP_APP_NAME=HelloWorld
"$CMAKE" --build "$BUILD"

echo "== 6/8 run + diff vs dotnet =="
"$BUILD/HelloWorld" > "$WORK/native.txt"
dotnet "$APP" > "$WORK/managed.txt"
diff -u "$WORK/managed.txt" "$WORK/native.txt"

echo "== 7/8 pack Dn2Cpp.Build + scaffold a consuming app =="
dotnet pack src/Dn2Cpp.Build/Dn2Cpp.Build.csproj -c "$CONFIG" -o "$FEED"
MSAPP="$WORK/msbuild-app"
mkdir -p "$MSAPP"
# Local feed first so both packages resolve from this run; nuget.org stays
# available for the implicit SDK dependencies. Machine-specific, so generated
# into the (gitignored) work dir, never committed.
cat > "$MSAPP/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="dn2cpp-smoke" value="$PWD/$FEED" />
  </packageSources>
</configuration>
EOF
cat > "$MSAPP/MsBuildSmoke.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$TFM</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Dn2Cpp.Build" Version="$PKG_VERSION" />
  </ItemGroup>
</Project>
EOF
cat > "$MSAPP/Program.cs" <<'EOF'
System.Console.WriteLine("hello from the Dn2Cpp.Build native publish");
for (int i = 1; i <= 3; i++)
    System.Console.WriteLine($"square({i}) = {i * i}");
EOF

echo "== 8/8 dotnet publish must produce a working native binary =="
# The targets invoke plain `dn2cpp`, exactly as a user with the global tool
# would; the tool-path install from step 2 stands in for it via PATH.
NUGET_PACKAGES="$PWD/$WORK/nuget-cache" PATH="$PWD/$TOOLS:$PATH" \
    dotnet publish "$MSAPP/MsBuildSmoke.csproj" -c "$CONFIG"
NATIVE="$MSAPP/bin/$CONFIG/$TFM/publish/MsBuildSmoke"
[ -x "$NATIVE" ] || { echo "error: publish produced no native binary: $NATIVE" >&2; exit 1; }
"$NATIVE" > "$WORK/msbuild-native.txt"
dotnet "$MSAPP/bin/$CONFIG/$TFM/publish/MsBuildSmoke.dll" > "$WORK/msbuild-managed.txt"
diff -u "$WORK/msbuild-managed.txt" "$WORK/msbuild-native.txt"

echo "OK: dn2cpp + Dn2Cpp.Build $PKG_VERSION install from a feed and are self-contained"
