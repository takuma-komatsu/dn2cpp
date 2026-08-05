# runtime/CMakeLists.txt — cross-platform native build for the dn2cpp runtime
# and the transpiler's generated output.
#
# Two roles, selected by cache variables:
#   1. Runtime libraries — dn2cpp_runtime (core + intrinsics + the PAL's
#      platform/<os>/ implementation), the vendored Boehm GC, and (DN2CPP_GODOT)
#      the GDExtension runtime. Built once, then `export()`ed so per-app
#      configures import them instead of recompiling (preserves the build-once
#      speedup the shell gates rely on for the ~15 TUs + the large bdw-gc unit).
#   2. Final binary — when DN2CPP_APP_DIR points at a transpiler output dir, its
#      generated*.cpp are linked into a console executable (default) or, with
#      DN2CPP_GDEXTENSION, a GDExtension shared library.
#
# This mirrors gates/_common.sh's clang invocation exactly on macOS (-std=c++17
# -O2 -Wall, PIC, threads, the int64-atomic probe, the Boehm-GC compile flags) so
# the gate suite is behaviour-identical under DN2CPP_USE_CMAKE=1. The MSVC /
# clang-cl / em++ / console-toolchain arms are reached through CMake's native
# per-platform handling (compiler id, output naming, link model) plus toolchain
# files; they are wired but verified only on those hosts, not on macOS.

cmake_minimum_required(VERSION 3.20)
project(dn2cpp CXX C)

# CMAKE_BUILD_TYPE is intentionally left unset for the clang/gcc arms — see the
# -O2 comment near the bottom of this file — but project() itself already
# resolves the Ninja+MSVC generator's default CMAKE_BUILD_TYPE to "Debug"
# behind the scenes (so "NOT CMAKE_BUILD_TYPE" is false by the time this runs),
# whose per-config flags (/RTC1 /Od -MDd) then collide with this file's own
# hand-added /O2 (MSVC refuses to combine /RTC1 with /O2 at all: error D8016).
# Force Release unconditionally so no per-config flags are injected behind
# this file's back; harmless for clang/gcc, where the arm below is skipped
# entirely, and for anyone invoking this file directly with their own
# -DCMAKE_BUILD_TYPE=... under MSVC, since nothing here reads that variable.
if(MSVC)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "" FORCE)
    # Static CRT (/MT, /MTd), for the same reason the line above forces a
    # config: what this file emits is shipped to machines it was not built on.
    # The dynamic CRT makes every binary here — the .NET-module drop-in, the
    # GDExtension library, the console executable — import VCRUNTIME140.dll and
    # MSVCP140.dll, which exist only where the Visual C++ redistributable was
    # installed. On a player's machine without it the load fails, and nothing
    # says so: the engine reports only that it could not start the .NET runtime,
    # naming neither the library that failed nor the import that was missing.
    # The bug is also structurally invisible here — building at all means the
    # build tools are installed, and they install the redistributable — so this
    # is a defect no local run can reproduce and only a shipped game can find.
    # Godot's own Windows templates link the CRT statically too, so the engine
    # side gives no hint either.
    #
    # Directory-scoped, and that is what makes it safe: every object that ends
    # up in one of those binaries is configured by this file — the runtime
    # libraries, the vendored GC/zlib/brotli/curl, the generated translation
    # units, and all four final-target shapes — so they flip together and no
    # link mixes the two CRT models. It also pins what the per-config flags
    # would otherwise decide behind this file's back (a Debug config's -MDd),
    # which is the same failure the forced Release above exists to prevent.
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    # Several runtime sources hold real non-ASCII characters in u"..."/u'...'
    # literals (currency symbols, infinity, per-mille — CultureInfo data,
    # numeric formatting). MSVC's default source/execution charset is the
    # host's ANSI code page (932/Shift-JIS on a Japanese Windows install, not
    # UTF-8), so those multi-byte UTF-8 source bytes get misread — as a
    # runaway character constant, or (rarer) a stray byte that decodes to a
    # backslash and eats the rest of the line as an escape. /utf-8 pins both
    # charsets to UTF-8 regardless of host locale, matching every other
    # compiler's assumption for this source tree.
    add_compile_options(/utf-8)
    # <windows.h> is pulled in transitively on this target (bdwgc's
    # GC_WIN32_THREADS headers, the Win32 PAL, ...); without NOMINMAX its
    # max/min macros clobber every std::max/std::min call site project-wide
    # (a bare `std::max(` mis-expands mid-token: MSVC error C2589/C2059).
    # WIN32_LEAN_AND_MEAN trims the rest of the pulled-in surface (winsock,
    # shell, ...) that nothing here needs. Global compile definitions, not a
    # per-file fix, since any TU can reach <windows.h> transitively.
    add_compile_definitions(NOMINMAX WIN32_LEAN_AND_MEAN)
endif()

include(CheckCXXSourceCompiles)

# ── Options / inputs ──────────────────────────────────────────────────────────
option(DN2CPP_USE_GC      "Compile and link the vendored Boehm-Demers-Weiser GC" ON)
option(DN2CPP_USE_ZLIB    "Compile and link the vendored zlib for System.IO.Compression" ON)
option(DN2CPP_USE_BROTLI  "Compile and link the vendored brotli for System.IO.Compression.Brotli" ON)
option(DN2CPP_USE_CURL    "Compile and link the vendored libcurl (over vendored Mbed TLS + CA bundle) for HTTP/HTTPS transport" ON)
option(DN2CPP_USE_HIGHWAY "Back the portable-SIMD ops with vendored Google Highway (static single-ISA, header-only)" ON)
set(DN2CPP_HIGHWAY_ARCH   ""    CACHE STRING   "Extra -march/-mcpu flags for the Highway build (e.g. -mavx2); empty = compiler default ISA")
set(DN2CPP_HOT_ARCH       ""    CACHE STRING   "Extra ISA flags for the [HotPath] TU generated_hot.cpp (e.g. -mavx2, /arch:AVX2, -msimd128); empty = compiler default ISA")
option(DN2CPP_GODOT       "Build the Godot GDExtension runtime library"           OFF)
option(DN2CPP_GDEXTENSION "Build the app (DN2CPP_APP_DIR) as a shared library"    OFF)
option(DN2CPP_SHARED      "Build the app (DN2CPP_APP_DIR) as a plain shared library (no Godot runtime; exports the [UnmanagedCallersOnly] entry points)" OFF)
option(DN2CPP_DOTNET_MODULE "Build the Godot .NET-module host runtime library, and the app (DN2CPP_APP_DIR) as a mono-module shared library" OFF)
set(DN2CPP_APP_DIR        ""    CACHE PATH     "Transpiler output dir to build into a final binary")
set(DN2CPP_APP_NAME       "app" CACHE STRING   "Name of the app target / output file")
set(DN2CPP_APP_LINK_FLAGS ""    CACHE STRING   "Extra link flags for the app (e.g. -L<dir>)")
set(DN2CPP_RUNTIME_EXPORT ""    CACHE FILEPATH "Prebuilt dn2cpp-targets.cmake to import the runtime from")
option(DN2CPP_STRIP       "Strip the final binary's symbol table"                   ON)
option(DN2CPP_DEAD_STRIP  "Dead-strip unreferenced code from the final binary"      ON)
option(DN2CPP_HIDDEN_VISIBILITY "Compile the runtime and generated code with hidden symbol visibility" ON)
option(DN2CPP_PCH         "Precompile generated.h for the app target"               ON)
# The two managed-fault guards the emitted body carries: a null receiver raises
# NullReferenceException instead of faulting, and an integer division by zero
# raises DivideByZeroException instead of doing whatever the hardware does. Both
# ON; turning either OFF restores UNDEFINED BEHAVIOUR, not merely a crash — on
# arm64 an unchecked `x / 0` answers 0 without trapping, and an unchecked null
# deref clang can see is null may be deleted outright.
#
# Native-build options rather than transpiler flags on purpose: the emitted C++
# is byte-identical either way (the guards are inline functions the generated
# code names unconditionally), so nothing here perturbs the self-host fixpoint
# and gates/measure-faultchecks.sh can A/B the same generated.cpp.
option(DN2CPP_NULL_CHECKS  "Emitted instance access on a null receiver raises NullReferenceException (OFF = undefined behaviour)" ON)
option(DN2CPP_ARITH_CHECKS "Emitted integer div/rem by zero raises DivideByZeroException (OFF = undefined behaviour)" ON)

# The PAL-seam axis: swap runtime/core/platform/<host>/'s PAL implementation for
# the target-free runtime/core/platform/reference/ one. Never ON outside
# gates/build-and-run-pal-reference.sh — a porting-contract assertion, not a
# configuration anybody should ship. What it does and does not claim is stated
# beside the PLATFORM_SRCS selection below.
option(DN2CPP_PAL_REFERENCE "Build against the target-free reference PAL instead of the host's" OFF)

# The runtime's per-function stack-frame ceiling, in bytes (empty disables it).
#
# A console or embedded host gives its threads far less stack than a desktop
# main thread's 8 MiB and the runtime nests (a reflection collector calls a
# formatter calls a parser), so an oversized frame fails as a stack overflow
# inside somebody else's game. The number is per-TARGET — the POSIX PAL's
# executable_path holds three PATH_MAX buffers, 3 KiB on macOS and 12 KiB on
# Linux from identical source — which is why a grep for large arrays cannot
# stand in for it.
#
# 4096 is a policy, roughly 1.5x the worst real frame, leaving headroom for
# targets whose PATH_MAX, wchar_t or calling convention widens the same source.
# Re-derive rather than trust it: configure with -DDN2CPP_MAX_STACK_FRAME= and
# -DCMAKE_CXX_FLAGS=-Wframe-larger-than=512 and read the warnings.
# dn2cpp_scrub_stack's deliberate 16 KiB band is exempted at its own site.
#
# A -Werror rather than a warning, because a warning in a build emitting
# thousands of lines is not a check. MSVC has no equivalent flag, so the ceiling
# is silently absent there — which is why gates/build-and-run-pal-reference.sh
# asserts the flag reached the compile line rather than trusting the variable.
set(DN2CPP_MAX_STACK_FRAME "4096" CACHE STRING
    "Per-function stack-frame ceiling for the runtime's own TUs, in bytes (empty = no check)")

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_C_STANDARD 11)
# PIC objects link cleanly into both an executable (console) and a shared library
# (GDExtension), so one set of runtime objects serves both — as in the shell build.
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# Repo root (this file lives in <root>/runtime).
get_filename_component(DN2CPP_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/.." ABSOLUTE)

# ── Emscripten (WASM) arm ─────────────────────────────────────────────────────
# em++ compiles to single-threaded wasm32: no -pthread (a SharedArrayBuffer
# worker pool would be needed; instead std::thread's ctor throws at runtime,
# which is the intended loud degrade). C++ exceptions use the native wasm
# exception proposal (-fwasm-exceptions) instead of the slow JS-based unwinding.
#
# The Boehm GC is REAL on this arm: single-threaded bdwgc (no GC_THREADS —
# bdwgc's EMSCRIPTEN arm #errors under it), conservative stack-scan soundness
# restored by Binaryen's SpillPointers pass at link (pointer-typed wasm locals
# are spilled to the linear-memory shadow stack so the collector can see them),
# and static-data GC roots registered via the dn2cpp_roots section (bdwgc's
# EMSCRIPTEN arm registers an empty DATASTART/DATAEND, so the section is how the
# statics get scanned). -DDN2CPP_USE_GC=OFF remains the calloc escape hatch.
#
# Only the flags EVERY Emscripten target needs are directory-scoped here.
# -sEXIT_RUNTIME / -sSTACK_SIZE / -sALLOW_MEMORY_GROWTH are NOT among them: they
# configure the JS glue and the linear memory, both of which belong to the MAIN
# module. A side module has no JS glue, does not own the memory it runs in, and
# has no runtime to exit — so those three say nothing about it. emcc does not
# reject them there (as of 6.0.1 it silently ignores them: the side module links
# byte-identically with and without), which is precisely why they must not be
# left lying around at directory scope — a flag that is quietly inert is one
# nobody notices is misplaced. They ride on the console executable instead, the
# one target shape they actually describe.
if(EMSCRIPTEN)
    # Certain CMake versions cannot build an Emscripten shared library at all:
    # emscripten's own Platform/Emscripten.cmake sets the global property
    # TARGET_SUPPORTS_SHARED_LIBS=FALSE on cmake [4.2.0,4.2.6) and [4.3.0,4.3.3)
    # (kitware #27240), and on those versions add_library(... SHARED) SILENTLY
    # degrades to a static archive. The side module would then never be produced,
    # and the first symptom would be a missing .so — or worse, a stale one from a
    # previous configure — with nothing naming the cause. Read the property rather
    # than re-deriving the version band, so this tracks whatever emscripten
    # currently believes; the empty case is "unset", i.e. cmake's supported
    # default, not a failure.
    get_property(_dn2cpp_shared_libs_ok GLOBAL PROPERTY TARGET_SUPPORTS_SHARED_LIBS)
    if(DN2CPP_APP_DIR AND (DN2CPP_GDEXTENSION OR DN2CPP_SHARED OR DN2CPP_DOTNET_MODULE)
            AND NOT "${_dn2cpp_shared_libs_ok}" STREQUAL "" AND NOT _dn2cpp_shared_libs_ok)
        message(FATAL_ERROR
            "This CMake (${CMAKE_VERSION}) cannot build Emscripten shared libraries: "
            "emscripten's toolchain file has set TARGET_SUPPORTS_SHARED_LIBS=FALSE, so "
            "add_library(SHARED) would silently produce a STATIC archive and no side "
            "module would be emitted. Use cmake < 4.2.0 or >= 4.3.3.")
    endif()

    add_compile_options(-fwasm-exceptions)
    add_link_options(-fwasm-exceptions)
    if(DN2CPP_USE_GC)
        # What makes conservative stack scanning sound on wasm: Binaryen's
        # SpillPointers pass spills pointer-typed wasm locals to the linear-memory
        # shadow stack at every call site, so the collector's stack scan can see
        # them. Without it a pointer held only in a wasm local is invisible to the
        # collector and its referent can be freed while still live. Directory-scoped
        # so both the console executable and the dotnet-module side module inherit it.
        add_link_options("-sBINARYEN_EXTRA_PASSES=--spill-pointers")
    endif()
    set(_dn2cpp_threads "")

    # ── No libcurl on the Web lane ────────────────────────────────────────────
    # DN2CPP_USE_CURL is forced off here whatever the caller asked for, and the
    # first of the three reasons is the one no configuration of either side can
    # answer:
    #
    #   * A BROWSER HAS NO TCP. Emscripten's socket() is emulated over a
    #     WebSocket proxy, so a libcurl that built and linked here would still be
    #     speaking to something no HTTP server is listening on. That is a
    #     property of the host, not of the build — the same class of carve-out as
    #     "there are no threads", and it is why this arm ships a transport that
    #     ANSWERS rather than one that is merely absent (below).
    #   * IT WOULD NOT LINK ANYWAY. curl's own CMakeLists runs an unconditional
    #     `if(NOT WIN32) find_package(Threads)` and appends Threads::Threads to
    #     the static libcurl's link interface. This is precisely the arm that does
    #     NOT go through find_package(Threads) (see _dn2cpp_threads above), so
    #     that name would be written into the exported dn2cpp-targets.cmake and
    #     the per-app configure importing it would die at generate time on an
    #     unresolvable target — a failure naming a CMake target, with nothing
    #     naming wasm or curl.
    #   * IT WOULD COST MINUTES PER CONFIGURE. curl's configure runs ~135 check_*
    #     probes and its _CURL_PREFILL fast path defaults to WIN32 only, so on
    #     Emscripten every one of them launches em++.
    #
    # A set() without CACHE shadows the cache entry for this directory and below,
    # which is every scope that reads it: the curl/mbedTLS/CA-bundle arm, the
    # target_link_libraries arm, and the per-app configure, which re-enters this
    # same file. The transpiler's HttpClient route is unconditional, so the
    # dn2cpp_http2_call_* symbols it names must still be DEFINED on this axis — and
    # they are, by the Emscripten fallback arm of
    # runtime/core/intrinsics/dn2cpp_http2_stream.cpp. Read the two-meanings note in
    # that file's header before changing either side: "no curl because the target
    # cannot have it" and "no curl because you asked for none" are deliberately
    # different, and only the second one is a link error.
    if(DN2CPP_USE_CURL)
        message(STATUS "dn2cpp: DN2CPP_USE_CURL forced OFF on Emscripten "
                       "(a browser has no TCP socket layer; System.Net.Http degrades to a "
                       "named transport failure — see the Web-lane carve-out in runtime/CMakeLists.txt)")
    endif()
    set(DN2CPP_USE_CURL OFF)
else()
    find_package(Threads REQUIRED)
    set(_dn2cpp_threads Threads::Threads)
endif()

# ── iOS arm ───────────────────────────────────────────────────────────────────
# The iOS target needs no source-level divergence: the POSIX PAL, pthreads and
# the vendored Boehm GC (TARGET_OS_IPHONE arms in gcconfig.h, Mach stop-world)
# all compile and run unchanged, so the caller only retargets the configure
# with -DCMAKE_SYSTEM_NAME=iOS -DCMAKE_OSX_SYSROOT=iphoneos|iphonesimulator.
# Two constraints live with the caller / here:
#   - CMAKE_OSX_DEPLOYMENT_TARGET must be >= 16.3: the formatting core uses the
#     floating-point std::to_chars overloads, which Apple's libc++ marks
#     unavailable before iOS 16.3 (the gates' iOS axis passes 16.3).
#   - Console executables must stay plain Mach-O binaries — CMake wraps
#     add_executable into an .app bundle on iOS by default, but app packaging
#     is the Godot export's concern, and `simctl spawn` runs a plain binary on
#     a simulator directly.
if(CMAKE_SYSTEM_NAME STREQUAL "iOS")
    set(CMAKE_MACOSX_BUNDLE OFF)
endif()

# ── ELF dead-strip prerequisite ───────────────────────────────────────────────
# Mach-O's -dead_strip works at symbol granularity, but ELF's --gc-sections can
# only drop whole input sections — so without these the linker sees one section
# per translation unit and can free almost nothing. Directory-scoped, so both the
# runtime configure (gc/zlib/brotli/runtime) and the per-app configure (which
# re-enters this file) compile with them. Emscripten's wasm-ld already collects
# unreferenced code, and Apple needs no help. Written for Linux/Android hosts and
# exercised there, like the other cross-platform arms in this file.
if(DN2CPP_DEAD_STRIP AND NOT APPLE AND NOT EMSCRIPTEN)
    if(MSVC)
        # MSVC's nearest equivalent is function-level linking (/Gy); there is no
        # -fdata-sections analogue to ask for separately.
        add_compile_options(/Gy)
    else()
        add_compile_options(-ffunction-sections -fdata-sections)
    endif()
endif()

# ── Hidden symbol visibility ──────────────────────────────────────────────────
# The generated C++ leans on weak definitions — the inline cctor-ensure guards
# and every other header-inline the 40+ translation units share. With default
# visibility each one is exported, and on Mach-O that is paid twice: the dyld
# export trie names them all (2.84 MB on the self-hosted CLI), and every data
# reference to one becomes a launch-time weak bind. Hidden visibility keeps the
# linker's weak coalescing (single image — pointer identity is unaffected)
# while dropping both. Measured on the self-hosted CLI:
# 15,604,576 -> 12,567,728 bytes (-19.5%), byte-identical re-emit.
#
# For a shared library this is also what makes dead-strip possible at all: a
# -shared link otherwise exports every global, and every exported global is a
# dead-strip root. The host-facing ABI hooks that must survive — the
# .gdextension entry symbol, the dlsym'd runtime hooks, the generated `main` a
# DN2CPP_SHARED host runs — carry DN2CPP_RT_EXPORT (runtime/core/dn2cpp.h),
# and the [UnmanagedCallersOnly] entry points their own DN2CPP_UCO_EXPORT, so
# they stay in the export trie. Hot-update bases are fine — a patch's imports
# resolve through dn2cpp_type_registry, not dlsym.
#
# Directory-scoped like the section flags above, so the runtime configure
# (gc/zlib/brotli/runtime/godot/dotnetmodule static libraries) and the per-app
# configure (the generated TUs, every target shape) both compile with it —
# a shared library's export surface is only as small as its least-hidden
# object file. MSVC has no -fvisibility (a DLL exports nothing by default;
# the explicit dllexport/WINDOWS_EXPORT_ALL_SYMBOLS arms cover it).
#
# Emscripten used to be excluded here, on the grounds that its executable-shaped
# output pays no export table. That was true of the executable and false of
# everything else, and the .NET-module SIDE MODULE is what exposed it: wasm-ld
# exports every non-hidden global of a `-shared` link, and — exactly as the
# Mach-O/ELF paragraph above says — every exported global is then a DCE root, so
# nothing can be collected. The drop-in came out at 37.5 MB with a 4.6 MB export
# section listing 100,180 functions, and V8 refused to compile it at all: PIC data
# relocations scale with the surviving data, and __wasm_apply_data_relocs had grown
# to an 8.2 MB function body, past the 7.65 MB ceiling V8 puts on a single wasm
# function. Hiding by default lets wasm-ld's own DCE walk out from the few
# visibility("default") roots instead, which is the whole reason the flag exists.
# The executable is unaffected: `main` is the entry point and emcc exports it
# regardless of visibility (verified — a hidden-visibility wasm console binary
# links and runs).
if(DN2CPP_HIDDEN_VISIBILITY AND NOT MSVC)
    add_compile_options(-fvisibility=hidden -fvisibility-inlines-hidden)
endif()

# ── .NET-module trace gate: compile-time default ──────────────────────────────
# dn2cpp_dm_trace_enabled() reads DN2CPP_DM_TRACE from the environment, and a wasm
# module has no environment to read — so on Emscripten the init trace could never
# be turned on, by anyone, including the gate whose job is to assert the markers it
# prints. Raise the compile-time floor there. An explicitly-set variable still wins
# on every target that has one, so no other axis changes.
# TARGET is the dn2cpp_dotnetmodule library, whose single TU defines the gate.
function(_dn2cpp_dotnetmodule_trace_default target)
    if(EMSCRIPTEN)
        target_compile_definitions(${target} PRIVATE DN2CPP_DM_TRACE_DEFAULT=1)
    endif()
endfunction()

# ── Shared per-target compile options ─────────────────────────────────────────
# Every target here compiles -O2. Our own code warns (/W3 on MSVC, -Wall
# elsewhere); vendored third-party code is warning-suppressed (/w, -w — its
# warnings are not ours to fix). One function per flavor replaces the eight
# per-target if(MSVC) copies, so the flag sets cannot drift.
function(_dn2cpp_compile_options_warn target)
    if(MSVC)
        target_compile_options(${target} PRIVATE /O2 /W3)
    else()
        target_compile_options(${target} PRIVATE -O2 -Wall)
    endif()
endfunction()

# The stack-frame ceiling (DN2CPP_MAX_STACK_FRAME — rationale at the option).
#
# Applied to the runtime's OWN targets and deliberately NOT to the app: the app's
# TUs are generated C++ whose frame is a function of the input program's locals,
# so a ceiling there would fail somebody's game rather than a dn2cpp regression,
# and it is not what a small thread stack breaks first. It is also not applied to
# the vendored third_party targets, which build under _silent for the same reason
# they always have — they are not ours to fix.
function(_dn2cpp_stack_frame_limit target)
    if(DN2CPP_MAX_STACK_FRAME AND NOT MSVC)
        target_compile_options(${target} PRIVATE
            "-Wframe-larger-than=${DN2CPP_MAX_STACK_FRAME}" -Werror=frame-larger-than)
    endif()
endfunction()

function(_dn2cpp_compile_options_silent target)
    if(MSVC)
        target_compile_options(${target} PRIVATE /O2 /w)
    else()
        target_compile_options(${target} PRIVATE -O2 -w)
    endif()
endfunction()

# ── .NET-module host runtime target ───────────────────────────────────────────
# Defined from two places with identical shape — the runtime-libraries section
# (the normal from-source build) and the app section's fallback for a stale
# imported dn2cpp-targets.cmake that predates the .NET-module arm — so the
# definition lives once, here. runtime_target is the core-runtime link name
# valid in the caller's context: the real dn2cpp_runtime when building from
# source, the imported dn2cpp::dn2cpp_runtime in the fallback. Export-set
# membership stays at the call site (a list(APPEND) in here would not escape
# this function's variable scope).
function(_dn2cpp_add_dotnetmodule_target runtime_target)
    add_library(dn2cpp_dotnetmodule STATIC "${DN2CPP_ROOT}/runtime/dotnetmodule/dn2cpp_dotnetmodule.cpp")
    _dn2cpp_compile_options_warn(dn2cpp_dotnetmodule)
    _dn2cpp_stack_frame_limit(dn2cpp_dotnetmodule)
    target_include_directories(dn2cpp_dotnetmodule PUBLIC
        "${DN2CPP_ROOT}/runtime/core"
        "${DN2CPP_ROOT}/runtime/dotnetmodule")
    target_link_libraries(dn2cpp_dotnetmodule PUBLIC ${runtime_target})
    _dn2cpp_dotnetmodule_trace_default(dn2cpp_dotnetmodule)
    add_library(dn2cpp::dn2cpp_dotnetmodule ALIAS dn2cpp_dotnetmodule)
endfunction()

# ── Runtime libraries: build from source, or import a prebuilt export ──────────
# An explicit -DDN2CPP_RUNTIME_EXPORT (every gate build) wins; otherwise a
# toolchain bundle carrying prebuilt/ supplies one (runtime/cmake/).
include("${CMAKE_CURRENT_SOURCE_DIR}/cmake/dn2cpp_prebuilt.cmake")
if(NOT DN2CPP_RUNTIME_EXPORT)
    dn2cpp_use_prebuilt_runtime(DN2CPP_RUNTIME_EXPORT)
endif()
if(DN2CPP_RUNTIME_EXPORT)
    # Per-app configure: pull in dn2cpp::dn2cpp_runtime / _godot (with their
    # INTERFACE include dirs, compile defs and link deps) built once elsewhere.
    include("${DN2CPP_RUNTIME_EXPORT}")
else()
    # 64-bit atomic link requirement (mirrors gates/_common.sh::_resolve_atomic_flags):
    # the Interlocked/Volatile/async int64 atomics lower to native lock-free ops on a
    # 64-bit target (no extra lib) but to out-of-line __atomic_*_8 (libatomic) on a
    # 32-bit one. Probe the actual question rather than hard-coding arch detection.
    set(_atomic_probe "#include <cstdint>\nint main(){int64_t v=0,e=0;\n__atomic_fetch_add(&v,1,__ATOMIC_SEQ_CST);\n__atomic_compare_exchange_n(&v,&e,2,false,__ATOMIC_SEQ_CST,__ATOMIC_SEQ_CST);\n__atomic_exchange_n(&v,3,__ATOMIC_SEQ_CST);\n__atomic_store_n(&v,4,__ATOMIC_SEQ_CST);\nreturn (int)__atomic_load_n(&v,__ATOMIC_SEQ_CST);}")
    check_cxx_source_compiles("${_atomic_probe}" DN2CPP_ATOMIC_LOCKFREE)
    set(DN2CPP_ATOMIC_LIBS "")
    if(NOT DN2CPP_ATOMIC_LOCKFREE)
        set(CMAKE_REQUIRED_LIBRARIES atomic)
        check_cxx_source_compiles("${_atomic_probe}" DN2CPP_ATOMIC_NEEDS_LIBATOMIC)
        unset(CMAKE_REQUIRED_LIBRARIES)
        if(DN2CPP_ATOMIC_NEEDS_LIBATOMIC)
            set(DN2CPP_ATOMIC_LIBS atomic)
        endif()
    endif()

    set(_rt_export_targets dn2cpp_runtime)

    # Vendored bdw-gc amalgamation, compiled as C and warning-suppressed (third-party
    # source). Thread-aware with compiler builtin atomics, matching BDWGC_CFLAGS.
    if(DN2CPP_USE_GC)
        add_library(dn2cpp_gc STATIC "${DN2CPP_ROOT}/third_party/bdwgc/extra/gc.c")
        if(EMSCRIPTEN)
            # No GC_THREADS — bdwgc's EMSCRIPTEN arm #errors under it (the wasm
            # build is single-threaded). GC_DISABLE_INCREMENTAL — there is no
            # mprotect-based dirty-bit VDB on wasm, a belt to the runtime's own
            # force-off of incremental mode. _GNU_SOURCE — emscripten's unistd.h
            # hides the sbrk() declaration behind it, and os_dep.c's EMSCRIPTEN arm
            # calls sbrk() unconditionally, so without it the gc lib fails to build.
            target_compile_definitions(dn2cpp_gc PRIVATE
                GC_BUILTIN_ATOMIC NO_EXECUTE_PERMISSION GC_DISABLE_INCREMENTAL _GNU_SOURCE)
        else()
            target_compile_definitions(dn2cpp_gc PRIVATE GC_THREADS GC_BUILTIN_ATOMIC NO_EXECUTE_PERMISSION)
        endif()
        if(WIN32)
            # gc_config_macros.h auto-defines GC_DLL (-> GC_API = __declspec(dllimport)
            # at every include site, gc.c's own included as well) whenever _DLL is
            # defined and GC_NOT_DLL/GC_DLL are not. _DLL is a CRT-model macro, not a
            # statement about *this* library: MSVC's dynamic CRT defines it whether or
            # not what is being built is a DLL. The MSVC arm at the top of this file
            # pins the static CRT, so nothing defines _DLL and the auto-define does
            # not fire — but GC_NOT_DLL stays, unconditionally, because the correctness
            # of a static link must not ride on a CRT choice made elsewhere in the file.
            # Whenever the auto-define does fire, every consumer (dn2cpp_runtime, and
            # the app's generated*.cpp, which both include <gc.h>) emits an __imp_GC_*
            # dllimport reference instead of a plain static-link one, and the final link
            # fails with dozens of LNK2019s even though dn2cpp_gc.lib is right there on
            # the command line — a failure that names the CRT nowhere. GC_NOT_DLL is
            # bdwgc's documented opt-out for exactly this static-linking case, and this
            # library is statically linked into every target here on either CRT; PUBLIC
            # so it reaches every consumer's own <gc.h> include, not just this TU's.
            target_compile_definitions(dn2cpp_gc PUBLIC GC_NOT_DLL)
        endif()
        _dn2cpp_compile_options_silent(dn2cpp_gc)
        target_include_directories(dn2cpp_gc PUBLIC "${DN2CPP_ROOT}/third_party/bdwgc/include")
        list(APPEND _rt_export_targets dn2cpp_gc)
    endif()

    # Vendored zlib (classic, not zlib-ng), compiled as its normal multi-file
    # layout (unlike bdw-gc's single-TU amalgamation) and warning-suppressed
    # (third-party source). Backs the System.IO.Compression native shim
    # (runtime/core/intrinsics/dn2cpp_zlib_native.cpp).
    if(DN2CPP_USE_ZLIB)
        file(GLOB DN2CPP_ZLIB_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/third_party/zlib/*.c")
        add_library(dn2cpp_zlib STATIC ${DN2CPP_ZLIB_SRCS})
        _dn2cpp_compile_options_silent(dn2cpp_zlib)
        target_include_directories(dn2cpp_zlib PUBLIC "${DN2CPP_ROOT}/third_party/zlib")
        list(APPEND _rt_export_targets dn2cpp_zlib)
    endif()

    # Vendored brotli (the same version dotnet/runtime links), compiled in its
    # normal multi-file c/{common,dec,enc} layout and warning-suppressed
    # (third-party source). Backs System.IO.Compression.Brotli's P/Invoke
    # surface directly: the [DllImport] entry points name raw brotli API
    # symbols (BrotliDecoderCreateInstance, ...), so the transpiler's lowered
    # calls link straight against this library's own exports — no shim TU
    # exists (see third_party/brotli/DN2CPP-VENDORED.md). With the option OFF,
    # a program whose managed code reaches BrotliStream/BrotliEncoder/
    # BrotliDecoder fails at link with unresolved Brotli* symbols — loud and
    # intended, same design as DN2CPP_USE_ZLIB's OFF behaviour.
    if(DN2CPP_USE_BROTLI)
        file(GLOB DN2CPP_BROTLI_SRCS CONFIGURE_DEPENDS
            "${DN2CPP_ROOT}/third_party/brotli/c/common/*.c"
            "${DN2CPP_ROOT}/third_party/brotli/c/dec/*.c"
            "${DN2CPP_ROOT}/third_party/brotli/c/enc/*.c")
        add_library(dn2cpp_brotli STATIC ${DN2CPP_BROTLI_SRCS})
        _dn2cpp_compile_options_silent(dn2cpp_brotli)
        target_include_directories(dn2cpp_brotli PUBLIC "${DN2CPP_ROOT}/third_party/brotli/c/include")
        list(APPEND _rt_export_targets dn2cpp_brotli)
    endif()

    # Vendored libcurl over vendored Mbed TLS — http:// and https:// (see
    # third_party/curl/DN2CPP-VENDORED.md, third_party/mbedtls/DN2CPP-VENDORED.md and
    # third_party/cacert/DN2CPP-VENDORED.md). Backs System.Net.Http's transport
    # (runtime/core/intrinsics/dn2cpp_http2_stream.cpp). ON by default, like the other two
    # vendored codecs — HttpClient is BCL surface, and a default that made it fail to link
    # is a default that made the transpiler's own unconditional route a landmine. What
    # makes the default affordable is that the transport initializes LAZILY
    # (std::call_once around curl_global_init, see that file): a program that never issues
    # a request never names dn2cpp_http2_call_open, so the linker's dead-strip removes libcurl,
    # Mbed TLS and the ~180 KB CA bundle in full — measured +0 bytes on hello-world.
    #
    # -DDN2CPP_USE_CURL=OFF is the supported opt-out and its behaviour is deliberate: a
    # program that then reaches the HTTP helpers fails at link with unresolved
    # dn2cpp_http_* symbols — loud and intended, same design as DN2CPP_USE_ZLIB's OFF
    # behaviour. The Emscripten arm near the top of this file is the one place the option
    # is overridden rather than obeyed, and there the same TU's fallback arm defines those
    # symbols so the Web export links and degrades instead; the difference between the two
    # kinds of "no curl" is spelled out in that file's header.
    #
    # The two vendors are built in DIFFERENT styles, and each is the right one for its
    # tree: curl goes through its own CMakeLists via add_subdirectory because it needs a
    # generated curl_config.h (~200 platform probes) and a large set of -D defines that
    # only its own configure produces, while Mbed TLS is hand-globbed like zlib/brotli
    # because its shipped mbedtls_config.h has the choices already made in it and no
    # configure step is involved at all.
    #
    # The FORCE cache vars below pin curl to the one shape this project wants: a static
    # library only (no CLI, tests, examples, docs or install rules), HTTP the sole
    # protocol, TLS from the vendored backend, and — the point of the "no external
    # dependency" design — nothing whatsoever linked in from the host.
    set(DN2CPP_CA_BUNDLE_SRC "")
    if(DN2CPP_USE_CURL)
        # ── Vendored Mbed TLS: curl's TLS backend ─────────────────────────────
        # One static library out of library/*.c — upstream's three-archive split
        # (mbedcrypto/mbedx509/mbedtls) is a strict dependency chain that all lands in
        # one binary here, so splitting buys nothing and a glob survives upstream moving
        # a file between the lists. No -D defines: the shipped default configuration is
        # what curl's backend is written against (the table in
        # third_party/mbedtls/DN2CPP-VENDORED.md tabulates the symbols curl actually
        # reads, MBEDTLS_CTR_DRBG_C and MBEDTLS_SSL_PROTO_TLS1_3 among them).
        #
        # It inherits the directory-scoped -fvisibility=hidden set near the top of this
        # file, and THAT is what lets a dn2cpp GDExtension/mono-module library coexist
        # inside a Godot process that has its own built-in mbedtls: both define the same
        # mbedtls_* symbol names, and with default visibility the dynamic loader would be
        # free to bind one image's call to the other image's definition — two
        # independently-configured copies of a TLS library sharing struct layouts by
        # accident. Hidden visibility keeps every mbedtls_* definition here internal to
        # this binary, so the two copies never see each other. Do not exempt this target.
        file(GLOB DN2CPP_MBEDTLS_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/third_party/mbedtls/library/*.c")
        add_library(dn2cpp_mbedtls STATIC ${DN2CPP_MBEDTLS_SRCS})
        _dn2cpp_compile_options_silent(dn2cpp_mbedtls)
        target_include_directories(dn2cpp_mbedtls PUBLIC "${DN2CPP_ROOT}/third_party/mbedtls/include")
        list(APPEND _rt_export_targets dn2cpp_mbedtls)

        # ── Handing that library to curl's find_package(MbedTLS MODULE REQUIRED) ──
        # curl's CMake/FindMbedTLS.cmake skips every search when the four input variables
        # are already defined, then defines the imported target CURL::mbedtls only
        # `if(NOT TARGET CURL::mbedtls)`. Both hooks are used here:
        #
        #   * the four variables are pre-seeded so nothing is looked for on the host —
        #     no pkg-config, no MbedTLS CONFIG package, no find_library. The three
        #     _LIBRARY ones are satisfied by the TARGET NAME rather than a path: the
        #     module only tests them for non-emptiness (find_package_handle_standard_args)
        #     and, because CURL::mbedtls already exists, never builds a link interface out
        #     of them. MBEDTLS_INCLUDE_DIR must be real, though — the module reads
        #     MBEDTLS_VERSION out of mbedtls/build_info.h under it, and curl FATAL_ERRORs
        #     below 3.2.0.
        #
        #   * CURL::mbedtls is pre-created as an ALIAS of the real dn2cpp_mbedtls target,
        #     and THAT choice is what keeps the per-app configure working. curl links its
        #     TLS backend PRIVATE into the static libcurl, which on a static library lands
        #     in the INTERFACE as $<LINK_ONLY:...> — i.e. the name is written into the
        #     dn2cpp-targets.cmake this file export()s below, and a per-app configure that
        #     imports it fails at generate time on any name it cannot resolve. The three
        #     ways out are: define a matching CURL::mbedtls on the import side too (a
        #     second declaration to keep in sync, in a branch that does not even know
        #     whether curl is on), export the imported target itself (export() cannot —
        #     an INTERFACE IMPORTED target belongs to no export set), or make the name
        #     curl links resolve to a real target that IS in the export set. The ALIAS is
        #     the third: CMake resolves an alias to its target when it writes the export
        #     file, so what lands there is dn2cpp::dn2cpp_mbedtls — one declaration, one
        #     library, and nothing for the import side to know.
        set(MBEDTLS_INCLUDE_DIR "${DN2CPP_ROOT}/third_party/mbedtls/include" CACHE PATH     "" FORCE)
        set(MBEDTLS_LIBRARY     dn2cpp_mbedtls                              CACHE STRING   "" FORCE)
        set(MBEDX509_LIBRARY    dn2cpp_mbedtls                              CACHE STRING   "" FORCE)
        set(MBEDCRYPTO_LIBRARY  dn2cpp_mbedtls                              CACHE STRING   "" FORCE)
        add_library(CURL::mbedtls ALIAS dn2cpp_mbedtls)
        # Pre-fill the one feature probe curl runs against that target. check_function_exists
        # generates a throwaway try_compile project, and CMAKE_REQUIRED_LIBRARIES would carry
        # the name CURL::mbedtls into it — where no such target exists, so the configure dies
        # naming a target instead of a missing function. The answer is not in doubt: the
        # vendored default config has MBEDTLS_DES_C on, so library/des.c defines
        # mbedtls_des_crypt_ecb (that is the whole question the probe asks; it gates curl's
        # NTLM core, nothing else).
        set(HAVE_MBEDTLS_DES_CRYPT_ECB 1 CACHE INTERNAL "vendored mbedTLS has MBEDTLS_DES_C")

        # ── Vendored nghttp2: curl's HTTP/2 framing layer ─────────────────────
        # Hand-globbed like zlib/brotli/Mbed TLS rather than driven through its own
        # CMakeLists, and for the same reason as Mbed TLS: nghttp2's own build exists to
        # produce an installable shared+static pair with an export set, a pkg-config file
        # and a generated nghttp2ver.h, none of which this tree wants, and every choice its
        # configure makes is expressible as the handful of -D flags below. See
        # third_party/nghttp2/DN2CPP-VENDORED.md.
        #
        # Like dn2cpp_mbedtls it inherits the directory-scoped -fvisibility=hidden set near
        # the top of this file, and here that is not merely tidy: NGHTTP2_STATICLIB below
        # makes NGHTTP2_EXTERN expand to NOTHING (nghttp2.h's first arm), so every
        # nghttp2_* definition takes the directory default and stays internal to this
        # binary. A dn2cpp GDExtension loaded into a host process that has its own nghttp2
        # therefore cannot have its calls bound to the other image's definitions. Do not
        # exempt this target, and do not drop NGHTTP2_STATICLIB — without it the
        # BUILDING_NGHTTP2 arm would stamp visibility("default") back onto the whole API.
        file(GLOB DN2CPP_NGHTTP2_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/third_party/nghttp2/lib/*.c")
        add_library(dn2cpp_nghttp2 STATIC ${DN2CPP_NGHTTP2_SRCS})
        _dn2cpp_compile_options_silent(dn2cpp_nghttp2)
        target_include_directories(dn2cpp_nghttp2 PUBLIC "${DN2CPP_ROOT}/third_party/nghttp2/lib/includes")
        # PUBLIC, so curl's own http2.c/nghttp2-consuming TUs see the same declarations
        # this library was compiled with (see the visibility note above).
        target_compile_definitions(dn2cpp_nghttp2 PUBLIC NGHTTP2_STATICLIB)
        # The configless build. Every `#include <config.h>` in the tree sits behind
        # `#ifdef HAVE_CONFIG_H`, which is never defined here, so no config header is
        # needed or supplied — but the HAVE_* symbols the sources go on to consult are
        # still consulted, and two of them change generated CODE rather than merely
        # enabling an include. That is why they are set here instead of being left at
        # their silent, working-looking defaults:
        #
        #   * HAVE_ARPA_INET_H / HAVE_NETINET_IN_H gate nghttp2_net.h's only includes, and
        #     nghttp2_helper.c / nghttp2_hd_huffman.c call htonl/htons/ntohl/ntohs. On
        #     Darwin those arrive as macros transitively (<sys/types.h> ->
        #     machine/endian.h), so omitting these compiles clean HERE and fails on glibc,
        #     where the declarations live in <arpa/inet.h> and nowhere else. Measured both
        #     ways on this host before writing them down.
        #   * HAVE_CLOCK_GETTIME + HAVE_DECL_CLOCK_MONOTONIC pick nghttp2_time.c's
        #     CLOCK_MONOTONIC arm. Omitting them selects the time(NULL) fallback, which
        #     compiles and runs and is a WALL clock: nghttp2_ratelim's token bucket would
        #     then jump on an NTP step. HAVE_DECL_CLOCK_MONOTONIC is tested by VALUE
        #     (`#elif defined(HAVE_CLOCK_GETTIME) && HAVE_DECL_CLOCK_MONOTONIC`), so it
        #     must be =1, not merely defined.
        #
        # BUILDING_NGHTTP2, which upstream's own build sets, is deliberately NOT set: under
        # NGHTTP2_STATICLIB its only remaining effect is `#undef NGHTTP2_NO_SSIZE_T`, which
        # nothing here defines, and leaving it out means no arm of nghttp2.h can select
        # default visibility even if the STATICLIB define were ever lost.
        if(WIN32)
            target_compile_definitions(dn2cpp_nghttp2 PRIVATE HAVE_WINDOWS_H HAVE_GETTICKCOUNT64)
            # MSVC has no ssize_t, and nghttp2.h spells its pre-v2 API in it — the arms
            # curl still calls, so NGHTTP2_NO_SSIZE_T, which hides exactly those, is not
            # the way out. PUBLIC because ssize_t is part of those function-pointer types:
            # a TU resolving it differently compiles and links and then calls through a
            # mismatched prototype. The spelling is curl's own, out of the curl_config.h
            # its configure generates — long where size_t is 32-bit, __int64 where 64.
            if(MSVC)
                if(CMAKE_SIZEOF_VOID_P EQUAL 8)
                    target_compile_definitions(dn2cpp_nghttp2 PUBLIC ssize_t=__int64)
                else()
                    target_compile_definitions(dn2cpp_nghttp2 PUBLIC ssize_t=long)
                endif()
            endif()
        else()
            target_compile_definitions(dn2cpp_nghttp2 PRIVATE
                HAVE_ARPA_INET_H HAVE_NETINET_IN_H
                HAVE_CLOCK_GETTIME HAVE_DECL_CLOCK_MONOTONIC=1)
        endif()
        list(APPEND _rt_export_targets dn2cpp_nghttp2)

        # ── Handing that library to curl's find_package(NGHTTP2 MODULE) ───────
        # Exactly the shape of the MbedTLS handoff above, and for exactly the same two
        # reasons; read that block first. CMake/FindNGHTTP2.cmake skips its pkg-config and
        # CONFIG-package probes when NGHTTP2_INCLUDE_DIR and NGHTTP2_LIBRARY are both
        # already defined, and its find_path/find_library calls are no-ops against cache
        # entries that already hold a value — so nothing is looked for on the host.
        #
        #   * NGHTTP2_LIBRARY is satisfied by the TARGET NAME: the module only tests it for
        #     non-emptiness, and because CURL::nghttp2 already exists it never builds a link
        #     interface out of it.
        #   * NGHTTP2_INCLUDE_DIR must be REAL, though — the module reads NGHTTP2_VERSION
        #     out of ${NGHTTP2_INCLUDE_DIR}/nghttp2/nghttp2ver.h, and lib/http2.c #errors
        #     below 1.15.0.
        #   * CURL::nghttp2 is pre-created as an ALIAS of dn2cpp_nghttp2 so that the name
        #     curl writes into its static library's $<LINK_ONLY:...> interface — and thus
        #     into the dn2cpp-targets.cmake export()ed below — resolves to a target that IS
        #     in the export set (it lands as dn2cpp::dn2cpp_nghttp2). An INTERFACE IMPORTED
        #     target belongs to no export set, so a per-app configure importing the runtime
        #     would fail at generate time on an unresolvable CURL::nghttp2.
        set(NGHTTP2_INCLUDE_DIR "${DN2CPP_ROOT}/third_party/nghttp2/lib/includes" CACHE PATH   "" FORCE)
        set(NGHTTP2_LIBRARY     dn2cpp_nghttp2                                   CACHE STRING "" FORCE)
        add_library(CURL::nghttp2 ALIAS dn2cpp_nghttp2)

        set(BUILD_CURL_EXE       OFF CACHE BOOL   "" FORCE)
        set(BUILD_SHARED_LIBS    OFF CACHE BOOL   "" FORCE)
        set(BUILD_STATIC_LIBS    ON  CACHE BOOL   "" FORCE)
        set(BUILD_TESTING        OFF CACHE BOOL   "" FORCE)
        set(BUILD_EXAMPLES       OFF CACHE BOOL   "" FORCE)
        set(BUILD_LIBCURL_DOCS   OFF CACHE BOOL   "" FORCE)
        set(BUILD_MISC_DOCS      OFF CACHE BOOL   "" FORCE)
        set(ENABLE_CURL_MANUAL   OFF CACHE BOOL   "" FORCE)
        set(CURL_DISABLE_INSTALL ON  CACHE BOOL   "" FORCE)
        # HTTP_ONLY disables every protocol handler except http — and https is NOT one of
        # the ones it disables: lib/protocol.c gates the https handler on USE_SSL alone.
        # So "HTTP only" and "TLS on" are orthogonal here, and both are wanted.
        set(HTTP_ONLY            ON  CACHE BOOL   "" FORCE)
        set(CURL_ENABLE_SSL      ON  CACHE BOOL   "" FORCE)
        set(CURL_USE_MBEDTLS     ON  CACHE BOOL   "" FORCE)
        # CURL_USE_OPENSSL is forced OFF rather than left to its default, because its
        # default is not OFF: curl computes _openssl_default as "ON unless some other
        # backend is already selected", so turning CURL_ENABLE_SSL on without this line
        # is one edit away from a find_package(OpenSSL REQUIRED) that grabs whatever the
        # BUILD HOST happens to have — the exact host dependency this vendor exists to
        # avoid, and one that would link and pass its tests on the developer's machine.
        set(CURL_USE_OPENSSL     OFF CACHE BOOL   "" FORCE)
        set(CURL_USE_SCHANNEL    OFF CACHE BOOL   "" FORCE)
        set(CURL_USE_WOLFSSL     OFF CACHE BOOL   "" FORCE)
        set(CURL_USE_GNUTLS      OFF CACHE BOOL   "" FORCE)
        set(CURL_USE_RUSTLS      OFF CACHE BOOL   "" FORCE)
        # No OS trust store, on any platform: the CA anchors are the embedded bundle
        # below and only that, so the same code verifies the same way everywhere. (The
        # mbedTLS backend has no native-store arm to use in the first place.)
        set(CURL_CA_NATIVE       OFF CACHE BOOL   "" FORCE)
        set(USE_APPLE_SECTRUST   OFF CACHE BOOL   "" FORCE)
        # "none", not the "auto" default: on a non-cross build auto makes curl SEARCH THE
        # BUILD HOST for a CA bundle and bake the path it finds (/etc/ssl/cert.pem, ...)
        # into curl_config.h. That path is a property of the machine that ran cmake, not
        # of the machine that runs the binary, so it is wrong the moment the artifact
        # moves — and wrong silently, since the embedded blob makes verification work
        # anyway on the host where it was built.
        set(CURL_CA_BUNDLE       none CACHE STRING "" FORCE)
        set(CURL_CA_PATH         none CACHE STRING "" FORCE)
        set(CURL_USE_LIBSSH2     OFF CACHE BOOL   "" FORCE)
        set(CURL_USE_LIBSSH      OFF CACHE BOOL   "" FORCE)
        set(CURL_USE_LIBPSL      OFF CACHE BOOL   "" FORCE)
        set(USE_LIBIDN2          OFF CACHE BOOL   "" FORCE)
        # HTTP/2 is ON, and it is a REQUIREMENT rather than an upgrade: gRPC is defined
        # over HTTP/2 — Grpc.Net.Client speaks nothing else, and the parts of it that have
        # no HTTP/1.1 spelling are the ones a call actually needs (a single long-lived
        # connection multiplexing concurrent streams, and the trailing HEADERS frame that
        # carries `grpc-status`; a trailer is not a thing an HTTP/1.1 response curl parses
        # for the caller). Both ride this library: curl's h2 filter is lib/http2.c over
        # nghttp2, and CURLOPT_HTTP_VERSION / the trailer surface only do anything when it
        # is compiled in. ALPN then offers h2, and a server that does not accept it still
        # gets HTTP/1.1 over the same TLS, so no existing behaviour is displaced.
        set(USE_NGHTTP2          ON  CACHE BOOL   "" FORCE)
        # The content-encoding options default to AUTO, not OFF, so they are forced OFF
        # or curl would find and link the system copies and drag them into
        # libcurl_static's link interface.
        set(CURL_ZLIB            OFF CACHE STRING "" FORCE)
        set(CURL_BROTLI          OFF CACHE STRING "" FORCE)
        set(CURL_ZSTD            OFF CACHE STRING "" FORCE)
        add_subdirectory("${DN2CPP_ROOT}/third_party/curl" "${CMAKE_BINARY_DIR}/third_party/curl" EXCLUDE_FROM_ALL)
        # The USE_NGHTTP2 above is a REQUEST, not a result: curl's CMakeLists answers a
        # failed find_package(NGHTTP2 MODULE) with `set(USE_NGHTTP2 OFF)` and no diagnostic
        # (third_party/curl/CMakeLists.txt, beside its option()), so a handoff that stopped
        # working would produce an HTTP/1.1-only libcurl out of a green build — and the
        # first thing to notice would be a gRPC call failing at run time, on somebody
        # else's machine. Assert the answer.
        if(NOT USE_NGHTTP2)
            message(FATAL_ERROR
                "curl configured WITHOUT HTTP/2: its find_package(NGHTTP2 MODULE) did not "
                "find the vendored library, so it silently turned USE_NGHTTP2 off. Fix the "
                "nghttp2 handoff block above (NGHTTP2_INCLUDE_DIR must point at a directory "
                "containing nghttp2/nghttp2ver.h, NGHTTP2_LIBRARY must be non-empty, and "
                "CURL::nghttp2 must already exist) — do not work around this by disabling "
                "HTTP/2, which gRPC requires.")
        endif()
        # curl names the STATIC target libcurl_static (its own CURL::libcurl_static
        # alias too); export it in the same set as dn2cpp_runtime so the per-app
        # configure imports it with the runtime rather than reconfiguring curl.
        list(APPEND _rt_export_targets libcurl_static)

        # ── CA trust anchors, compiled in ─────────────────────────────────────
        # Mbed TLS has no platform-trust-store integration (curl's mbedTLS backend
        # implements no native_ca_store arm), so without anchors of our own EVERY
        # handshake fails verification. They ride in the binary's rodata rather than in a
        # file beside it because a file is not something every dn2cpp target has: an APK,
        # an .ipa and a wasm module each carry their payload differently and none of them
        # has a path open(2) can reach the way a desktop build does. One generated TU, one
        # CURLOPT_CAINFO_BLOB call, identical on every platform.
        #
        # Generated at BUILD time into the build tree, never committed: it is ~1 MB of hex
        # derived mechanically from a file that gets refreshed several times a year, and a
        # committed copy is one more thing to forget. DEPENDS on both inputs, so a bundle
        # refresh is picked up with no edit here.
        set(DN2CPP_CA_BUNDLE_PEM "${DN2CPP_ROOT}/third_party/cacert/cacert.pem")
        set(DN2CPP_CA_BUNDLE_SRC "${CMAKE_BINARY_DIR}/generated/dn2cpp_ca_bundle.cpp")
        add_custom_command(
            OUTPUT "${DN2CPP_CA_BUNDLE_SRC}"
            COMMAND "${CMAKE_COMMAND}"
                "-DIN=${DN2CPP_CA_BUNDLE_PEM}"
                "-DOUT=${DN2CPP_CA_BUNDLE_SRC}"
                "-DSYM=dn2cpp_ca_bundle_pem"
                -P "${DN2CPP_ROOT}/runtime/cmake/dn2cpp_embed_bytes.cmake"
            DEPENDS "${DN2CPP_CA_BUNDLE_PEM}" "${DN2CPP_ROOT}/runtime/cmake/dn2cpp_embed_bytes.cmake"
            COMMENT "Embedding CA bundle from third_party/cacert/cacert.pem"
            VERBATIM)
    endif()

    # Core runtime = the runtime unit + every per-namespace intrinsic + the PAL's
    # platform implementation. Globbing matches the shell's INTRINSIC_UNITS /
    # PLATFORM_UNITS auto-discovery: a new file under intrinsics/ or platform/posix/
    # is picked up with no edit here (re-run cmake to re-glob).
    file(GLOB DN2CPP_INTRINSIC_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/runtime/core/intrinsics/*.cpp")
    if(DN2CPP_PAL_REFERENCE)
        # The reference PAL: platform/reference/ replaces the host's PAL
        # implementation and NOTHING ELSE changes — the exact claim
        # docs/PORTING.md makes to a porter ("add platform/<target>/ plus a
        # toolchain file, touch no core file"), performed rather than promised.
        #
        # The host's non-PAL platform TUs stay: the .NET PAL surface
        # (SystemNative_*) and the memory-mapped-file unit are separate seams
        # (§2.2, §2.3), and reusing a POSIX TU under a non-POSIX PAL is a move
        # §2.3 already documents for wasm. So the axis isolates dn2cpp_pal.h's
        # implementation and does not pretend to be a non-POSIX host, where
        # §2.2's question (which CoreLib flavour, and what it P/Invokes) is the
        # larger part of the port.
        #
        # Never the default: DN2CPP_PAL_REFERENCE is OFF everywhere except
        # gates/build-and-run-pal-reference.sh.
        file(GLOB DN2CPP_PLATFORM_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/runtime/core/platform/reference/*.cpp")
        if(WIN32)
            list(APPEND DN2CPP_PLATFORM_SRCS "${DN2CPP_ROOT}/runtime/core/platform/windows/dn2cpp_mmap_windows.cpp")
        else()
            file(GLOB DN2CPP_PLATFORM_POSIX_REST CONFIGURE_DEPENDS "${DN2CPP_ROOT}/runtime/core/platform/posix/*.cpp")
            list(REMOVE_ITEM DN2CPP_PLATFORM_POSIX_REST "${DN2CPP_ROOT}/runtime/core/platform/posix/dn2cpp_pal_posix.cpp")
            list(APPEND DN2CPP_PLATFORM_SRCS ${DN2CPP_PLATFORM_POSIX_REST})
        endif()
    elseif(EMSCRIPTEN)
        # WASM PAL, plus the POSIX mmap unit as-is: Emscripten's musl provides
        # open/fstat/mmap/msync against MEMFS, so that TU needs no port.
        file(GLOB DN2CPP_PLATFORM_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/runtime/core/platform/wasm/*.cpp")
        list(APPEND DN2CPP_PLATFORM_SRCS "${DN2CPP_ROOT}/runtime/core/platform/posix/dn2cpp_mmap_posix.cpp")
    elseif(WIN32)
        # Win32/CRT PAL, including dn2cpp_mmap_windows.cpp
        # (CreateFileMappingA/MapViewOfFile instead of POSIX mmap) — picked up
        # by this glob with no edit needed here.
        file(GLOB DN2CPP_PLATFORM_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/runtime/core/platform/windows/*.cpp")
    else()
        file(GLOB DN2CPP_PLATFORM_SRCS CONFIGURE_DEPENDS "${DN2CPP_ROOT}/runtime/core/platform/posix/*.cpp")
    endif()
    add_library(dn2cpp_runtime STATIC
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_runtime.cpp"
        # The runtime, split per topic (dn2cpp_runtime.cpp keeps the remainder).
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_gc.cpp"
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_exceptions.cpp"
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_tasks.cpp"
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_threading.cpp"
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_typeinfo.cpp"
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_casts.cpp"
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_marshal.cpp"
        # Hot-update BPI loader + IL interpreter. Static-archive semantics keep
        # it out of programs that never call dn2cpp_hotupdate_run.
        "${DN2CPP_ROOT}/runtime/core/dn2cpp_interp.cpp"
        ${DN2CPP_INTRINSIC_SRCS}
        ${DN2CPP_PLATFORM_SRCS}
        # The embedded CA bundle (empty unless DN2CPP_USE_CURL — see the arm above).
        # It rides in this archive rather than in one of its own so static-archive
        # semantics apply unchanged: the ~180 KB of anchors is pulled in only because
        # dn2cpp_http2_stream.cpp's object references the symbol.
        ${DN2CPP_CA_BUNDLE_SRC})
    _dn2cpp_compile_options_warn(dn2cpp_runtime)
    _dn2cpp_stack_frame_limit(dn2cpp_runtime)
    target_include_directories(dn2cpp_runtime PUBLIC
        "${DN2CPP_ROOT}/runtime/core"
        "${DN2CPP_ROOT}/third_party")
    target_link_libraries(dn2cpp_runtime PUBLIC ${_dn2cpp_threads} ${DN2CPP_ATOMIC_LIBS})
    if(APPLE)
        # Interop.AppleCrypto's cctor P/Invokes CFStringCreateWithCString by the
        # framework's absolute path (the real System.Security.Cryptography.dll);
        # those calls resolve against the real CoreFoundation. PUBLIC so the
        # app's generated*.cpp — whose lowered P/Invoke call sites reference the
        # CF* symbols directly — inherit the link.
        #
        # Foundation + libobjc are for the PAL's SystemNative_SearchPath
        # (dn2cpp_system_native.cpp, "special-folder search paths"), which sends
        # to NSFileManager through the objc runtime. Foundation's link is
        # load-bearing beyond resolving symbols: the class is found by a RUNTIME
        # lookup (objc_getClass), which answers nil — an empty
        # Environment.GetFolderPath, silently — unless dyld actually loaded the
        # framework; linking it is what guarantees that.
        target_link_libraries(dn2cpp_runtime PUBLIC
            "-framework CoreFoundation" "-framework Foundation" objc)
    endif()
    if(WIN32)
        # The win-x64 System.Net.NameResolution's Interop.Winsock P/Invokes
        # ws2_32.dll directly (gethostname / WSAStartup / WSACleanup —
        # Dns.GetHostName's primitive, the Windows counterpart of the POSIX PAL's
        # SystemNative_GetHostName). ws2_32 is NOT in CMake's default Windows link
        # set (Windows-MSVC.cmake / Windows-GNU.cmake set
        # CMAKE_C_STANDARD_LIBRARIES_INIT to kernel32 user32 gdi32 winspool shell32
        # ole32 oleaut32 uuid comdlg32 advapi32 — so kernel32/ole32/advapi32 need no
        # explicit link, but ws2_32 does), and without this the lowered call sites
        # fail at link time with an unresolved __imp_gethostname and nothing naming
        # winsock as the cause. ntdll and BCrypt are outside that default set too:
        # they are on IsRuntimeProvidedPInvokeModule's allowlist (Compilation.cs) but
        # no corpus program reaches a call to one, so nothing has needed the link yet
        # — a reachable one would fail here exactly as winsock did.
        # PUBLIC so the app's generated*.cpp — whose lowered P/Invoke call sites
        # reference the winsock symbols directly — inherit the link.
        target_link_libraries(dn2cpp_runtime PUBLIC ws2_32)
    endif()
    if(DN2CPP_USE_GC)
        # PUBLIC so the app's generated*.cpp inherit the GC define + gc.h include too.
        target_compile_definitions(dn2cpp_runtime PUBLIC DN2CPP_USE_BOEHM_GC)
        # GC_THREADS only off wasm: bdwgc's EMSCRIPTEN arm #errors under it, so it
        # must not reach any consumer's <gc.h> on the wasm axis.
        if(NOT EMSCRIPTEN)
            target_compile_definitions(dn2cpp_runtime PUBLIC GC_THREADS)
        endif()
        target_link_libraries(dn2cpp_runtime PUBLIC dn2cpp_gc)
    endif()
    # PUBLIC on both: the guards are inline functions in dn2cpp_core.h, so the
    # define has to reach the app's generated*.cpp (where the guarded sites are)
    # as well as the runtime TUs calling the same helpers (the interpreter's
    # interp_binary, decimal's div/rem). Reaching only one side would make the
    # two execution modes disagree about a fault.
    if(NOT DN2CPP_NULL_CHECKS)
        target_compile_definitions(dn2cpp_runtime PUBLIC DN2CPP_NO_NULL_CHECKS)
    endif()
    if(NOT DN2CPP_ARITH_CHECKS)
        target_compile_definitions(dn2cpp_runtime PUBLIC DN2CPP_NO_ARITH_CHECKS)
    endif()
    if(DN2CPP_USE_ZLIB)
        # PUBLIC so dn2cpp_zlib_native.cpp's own compile (already part of
        # DN2CPP_INTRINSIC_SRCS, unconditionally globbed above) sees the define +
        # zlib.h include dir; CMake resolves usage requirements regardless of
        # target_link_libraries/add_library ordering, so this is safe even though
        # dn2cpp_runtime's sources were listed before this call.
        target_compile_definitions(dn2cpp_runtime PUBLIC DN2CPP_USE_ZLIB)
        target_link_libraries(dn2cpp_runtime PUBLIC dn2cpp_zlib)
    endif()
    if(DN2CPP_USE_BROTLI)
        # PUBLIC link (like dn2cpp_zlib) so the app's generated*.cpp — whose
        # lowered P/Invoke call sites reference the Brotli* symbols directly —
        # pull the static archive in; no define is needed since no runtime TU
        # conditionally compiles against brotli (there is no shim TU).
        target_link_libraries(dn2cpp_runtime PUBLIC dn2cpp_brotli)
    endif()
    if(DN2CPP_USE_CURL)
        # PUBLIC on both counts (like dn2cpp_zlib): the define + the <curl/curl.h>
        # include dir reach dn2cpp_http2_stream.cpp's own compile — it is part of
        # DN2CPP_INTRINSIC_SRCS, unconditionally globbed above, and is a no-op TU
        # unless DN2CPP_USE_CURL is set — and the link + curl's INTERFACE include dir
        # (its $<BUILD_INTERFACE:.../include>, which export() carries through) reach the
        # app's generated*.cpp. CMake resolves usage requirements regardless of the
        # source/link ordering, so this is safe even though dn2cpp_runtime's sources
        # were listed before this call.
        target_compile_definitions(dn2cpp_runtime PUBLIC DN2CPP_USE_CURL)
        target_link_libraries(dn2cpp_runtime PUBLIC libcurl_static)
    endif()
    if(DN2CPP_USE_HIGHWAY)
        # Header-only static dispatch: no library to link, just the include path +
        # the define. PUBLIC so the app's generated*.cpp (which include dn2cpp_vectors.h)
        # see the Highway-backed ops and the vendored headers too. The optional -march
        # rides as an INTERFACE compile option so the app build inherits the same ISA.
        target_compile_definitions(dn2cpp_runtime PUBLIC DN2CPP_USE_HIGHWAY)
        target_include_directories(dn2cpp_runtime PUBLIC "${DN2CPP_ROOT}/third_party/highway")
        if(DN2CPP_HIGHWAY_ARCH)
            separate_arguments(_hwy_arch NATIVE_COMMAND "${DN2CPP_HIGHWAY_ARCH}")
            target_compile_options(dn2cpp_runtime PUBLIC ${_hwy_arch})
        endif()
    endif()
    add_library(dn2cpp::dn2cpp_runtime ALIAS dn2cpp_runtime)

    # Godot GDExtension runtime (table-driven bridge). Off by default; the gdext
    # gates configure with -DDN2CPP_GODOT=ON.
    if(DN2CPP_GODOT)
        add_library(dn2cpp_godot STATIC "${DN2CPP_ROOT}/runtime/godot/dn2cpp_godot.cpp")
        _dn2cpp_compile_options_warn(dn2cpp_godot)
        _dn2cpp_stack_frame_limit(dn2cpp_godot)
        target_include_directories(dn2cpp_godot PUBLIC
            "${DN2CPP_ROOT}/runtime/core"
            "${DN2CPP_ROOT}/runtime/godot"
            "${DN2CPP_ROOT}/third_party")
        target_link_libraries(dn2cpp_godot PUBLIC dn2cpp_runtime)
        add_library(dn2cpp::dn2cpp_godot ALIAS dn2cpp_godot)
        list(APPEND _rt_export_targets dn2cpp_godot)
    endif()

    # Godot .NET-module (mono module) host runtime. Off by default; the
    # dotnet-module gates configure with -DDN2CPP_DOTNET_MODULE=ON. Unlike the
    # GDExtension runtime it links only the core runtime (the engine interop
    # goes through the transpiled GodotSharp, not a hand-written bridge).
    if(DN2CPP_DOTNET_MODULE)
        _dn2cpp_add_dotnetmodule_target(dn2cpp_runtime)
        list(APPEND _rt_export_targets dn2cpp_dotnetmodule)
    endif()

    # Export the runtime targets to the build tree so a per-app configure can import
    # them (with their INTERFACE deps) via -DDN2CPP_RUNTIME_EXPORT, instead of
    # recompiling the runtime for every gate.
    export(TARGETS ${_rt_export_targets} NAMESPACE dn2cpp:: FILE "${CMAKE_BINARY_DIR}/dn2cpp-targets.cmake")
endif()

# ── Final binary: the transpiler's generated output ───────────────────────────
if(DN2CPP_APP_DIR)
    file(GLOB DN2CPP_APP_SRCS CONFIGURE_DEPENDS "${DN2CPP_APP_DIR}/generated*.cpp")
    if(DN2CPP_APP_SRCS STREQUAL "")
        message(FATAL_ERROR "DN2CPP_APP_DIR has no generated*.cpp: ${DN2CPP_APP_DIR}")
    endif()

    # [HotPath]-attributed method bodies land in dedicated TUs so stronger
    # per-file optimization flags can attach without touching the flat -O2 the
    # rest of the build compiles under: generated_hot.cpp carries the plain
    # marked set, generated_hot_fast.cpp the [HotPath(FastMath = true)] subset,
    # which adds relaxed floating-point semantics on top. FastMath is a second
    # unit rather than a flag on the first because -ffast-math / /fp:fast is a
    # translation-unit property — one kernel opting out of IEEE-exact arithmetic
    # must not drag every other marked kernel with it. Both TUs opt out of the
    # shared PCH: a PCH is only reusable under the exact flags it was built with,
    # and these flags diverge by design (each carries its own
    # #include "generated.h").
    if(MSVC)
        # /O2 is already cl.exe's top optimization tier; ISA widening comes
        # from DN2CPP_HOT_ARCH (e.g. /arch:AVX2).
        set(_hot_opts "")
        set(_hot_fast_opts "/fp:fast")
    else()
        set(_hot_opts "-O3")
        set(_hot_fast_opts "-O3" "-ffast-math")
    endif()
    if(DN2CPP_HOT_ARCH)
        separate_arguments(_hot_arch NATIVE_COMMAND "${DN2CPP_HOT_ARCH}")
        list(APPEND _hot_opts ${_hot_arch})
        list(APPEND _hot_fast_opts ${_hot_arch})
    endif()
    set(DN2CPP_HOT_TU "${DN2CPP_APP_DIR}/generated_hot.cpp")
    if(EXISTS "${DN2CPP_HOT_TU}")
        set_source_files_properties("${DN2CPP_HOT_TU}" PROPERTIES
            SKIP_PRECOMPILE_HEADERS ON
            COMPILE_OPTIONS "${_hot_opts}")
    endif()
    set(DN2CPP_HOT_FAST_TU "${DN2CPP_APP_DIR}/generated_hot_fast.cpp")
    if(EXISTS "${DN2CPP_HOT_FAST_TU}")
        set_source_files_properties("${DN2CPP_HOT_FAST_TU}" PROPERTIES
            SKIP_PRECOMPILE_HEADERS ON
            COMPILE_OPTIONS "${_hot_fast_opts}")
    endif()

    if(DN2CPP_GDEXTENSION)
        add_library(${DN2CPP_APP_NAME} SHARED ${DN2CPP_APP_SRCS})
        target_link_libraries(${DN2CPP_APP_NAME} PRIVATE dn2cpp::dn2cpp_runtime dn2cpp::dn2cpp_godot)
    elseif(DN2CPP_SHARED)
        # Plain shared library (no Godot runtime): a native host dlopens it, runs
        # the generated `main` once to initialize the runtime, and calls the
        # exported [UnmanagedCallersOnly] entry points. The define makes that `main`
        # return to the host instead of exiting the process (see dn2cpp_main_exit).
        add_library(${DN2CPP_APP_NAME} SHARED ${DN2CPP_APP_SRCS})
        target_compile_definitions(${DN2CPP_APP_NAME} PRIVATE DN2CPP_SHARED)
        target_link_libraries(${DN2CPP_APP_NAME} PRIVATE dn2cpp::dn2cpp_runtime)
        if(WIN32)
            # A dlopen host (e.g. the UcoShared gate's driver.cpp, via GetProcAddress)
            # also resolves plain runtime entry points (`main`,
            # dn2cpp_runtime_quiesce, dn2cpp_set_native_callback_gc_registration),
            # not just the [UnmanagedCallersOnly] exports that already carry
            # __declspec(dllexport) (see DN2CPP_UCO_EXPORT in the generated code).
            # Unix's -shared exports every global by default; MSVC's /DLL does not,
            # so mirror that default here rather than hand-annotating every such
            # runtime symbol.
            set_target_properties(${DN2CPP_APP_NAME} PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS ON)
        endif()
        if(EMSCRIPTEN)
            # A plain shared library on the wasm axis is an Emscripten SIDE MODULE
            # — =2, never the =1 a bare `emcc -shared` defaults to; the .NET-module
            # arm below spells out what LINKABLE=1 would cost. The export list is
            # the dlopen host's entry surface: the generated `main` (which
            # DN2CPP_SHARED makes return to the host instead of exiting). The CRI
            # wasm gate builds this shape to diff a drop-in-shaped side module's
            # import closure against the Web export template's main module.
            target_link_options(${DN2CPP_APP_NAME} PRIVATE
                -sSIDE_MODULE=2
                "-sEXPORTED_FUNCTIONS=_main")
            # Pin the artifact name (see the .NET-module arm: under emcmake the
            # ".so" suffix is otherwise an accident of package load order).
            set_target_properties(${DN2CPP_APP_NAME} PROPERTIES PREFIX "lib" SUFFIX ".so")
        endif()
    elseif(DN2CPP_DOTNET_MODULE)
        # Godot mono-module shared library (NativeAOT drop-in style: the engine
        # loads it and calls the exported godotsharp_game_main_init). If the
        # imported runtime export predates the .NET-module arm (a stale
        # dn2cpp-targets.cmake configured before the option existed), build the
        # single-TU host runtime here instead of failing the configure.
        if(NOT TARGET dn2cpp::dn2cpp_dotnetmodule)
            _dn2cpp_add_dotnetmodule_target(dn2cpp::dn2cpp_runtime)
        endif()
        add_library(${DN2CPP_APP_NAME} SHARED ${DN2CPP_APP_SRCS})
        target_link_libraries(${DN2CPP_APP_NAME} PRIVATE dn2cpp::dn2cpp_runtime dn2cpp::dn2cpp_dotnetmodule)
        if(EMSCRIPTEN)
            # The drop-in as an Emscripten SIDE MODULE: the engine's web export
            # preloads it and dlopens it, exactly as the desktop engine dlopens the
            # .dylib/.so.
            #
            # SIDE_MODULE=2, never the =1 that a bare `emcc -shared` defaults to
            # (tools/link.py). =1 forces LINKABLE=1, and LINKABLE has three effects,
            # each of which turns a link-time error into a runtime mystery:
            #   - dead-code elimination is off, so the library carries the whole
            #     tree-shaken CoreLib whether or not anything reaches it;
            #   - -sEXPORTED_FUNCTIONS becomes a no-op (emcc warns and ignores it),
            #     so the export surface silently becomes "everything";
            #   - every unresolved symbol becomes a wasm IMPORT rather than a link
            #     error — the library links "clean" and then dies on first CALL of
            #     the offending function. Not at dlopen: emscripten's loader hands
            #     back a valid handle, dlsym resolves, and the lazy stub throws
            #     `TypeError: resolved is not a function` from inside the JS glue,
            #     naming a wasm function index and not the symbol. That import
            #     surface is what build-and-run-godot-dotnet-wasm.sh diffs against
            #     the main module's exports, because nothing else will.
            # =2 keeps DCE and honours the export list; the imports stay (they are
            # how a side module reaches the runtime it is loaded into), but they are
            # now only the ones that survived DCE.
            #
            # __wasm_call_ctors is deliberately absent from the list: link.py adds it
            # to REQUIRED_EXPORTS for every side module, and the loader calls it after
            # instantiation to run the static initializers. The gate ASSERTS it is
            # exported rather than trusting that, since without it every C++ static
            # ctor in the runtime silently never runs.
            target_link_options(${DN2CPP_APP_NAME} PRIVATE
                -sSIDE_MODULE=2
                "-sEXPORTED_FUNCTIONS=_godotsharp_game_main_init,_dn2cpp_dm_interop_size")
            # Pin the artifact name. Under emcmake the suffix comes out ".so" only by
            # an accident of module load order between two independently-versioned
            # packages (cmake's and emscripten's), and the engine's UNIX branch
            # dlopens <Assembly>.so — so being born with the name it will die with
            # removes a whole class of confusion.
            set_target_properties(${DN2CPP_APP_NAME} PROPERTIES PREFIX "lib" SUFFIX ".so")
        endif()
    else()
        add_executable(${DN2CPP_APP_NAME} ${DN2CPP_APP_SRCS})
        target_link_libraries(${DN2CPP_APP_NAME} PRIVATE dn2cpp::dn2cpp_runtime)
        if(EMSCRIPTEN)
            # The JS-glue / linear-memory settings, on the one target shape they
            # describe (see the directory-scoped arm at the top of this file): a
            # console executable owns its memory and its runtime, and must run
            # atexit + flush stdio when main returns, which is what EXIT_RUNTIME
            # buys. A side module owns none of that.
            target_link_options(${DN2CPP_APP_NAME} PRIVATE
                -sEXIT_RUNTIME=1 -sALLOW_MEMORY_GROWTH=1 -sSTACK_SIZE=1048576)
            if(DN2CPP_USE_GC)
                # SpillPointers finds __stack_pointer BY NAME, but emcc's finalize
                # strips the wasm name section BEFORE it invokes wasm-opt — so the
                # name section must survive into that call or the pass hard-fails
                # with "getStackSpace: failed to find the stack pointer". -g2 keeps
                # it (-g1 is not enough); this is the main-module executable only.
                # The side module is untouched: there __stack_pointer is an IMPORT,
                # which carries its name at protocol level regardless of -g.
                target_link_options(${DN2CPP_APP_NAME} PRIVATE -g2)
            endif()
        endif()
    endif()
    _dn2cpp_compile_options_warn(${DN2CPP_APP_NAME})
    # generated.h lives next to the sources; dn2cpp.h comes from the runtime include
    # dir (also carried transitively, but listed for the from-source build too).
    target_include_directories(${DN2CPP_APP_NAME} PRIVATE
        "${DN2CPP_APP_DIR}"
        "${DN2CPP_ROOT}/runtime/core")

    # The managed-fault guards again, on the APP target. They are inline
    # functions the generated*.cpp name directly, so the app's own compile has
    # to see the define — and when the runtime is IMPORTED
    # (DN2CPP_RUNTIME_EXPORT, which is every gate build) the PUBLIC definitions
    # set beside dn2cpp_runtime above never ran here at all, so this is the only
    # arm that fires on that path. It also lets gates/measure-faultchecks.sh vary
    # the axis without reconfiguring — and so without poisoning — the shared
    # runtime build dir. Setting only this one leaves the RUNTIME's own copies
    # (interp_binary, decimal div/rem) checked, which is what an A/B of the
    # EMITTED code's cost wants; a whole-program opt-out passes the option to
    # both configures.
    if(NOT DN2CPP_NULL_CHECKS)
        target_compile_definitions(${DN2CPP_APP_NAME} PRIVATE DN2CPP_NO_NULL_CHECKS)
    endif()
    if(NOT DN2CPP_ARITH_CHECKS)
        target_compile_definitions(${DN2CPP_APP_NAME} PRIVATE DN2CPP_NO_ARITH_CHECKS)
    endif()

    # ── Precompiled header ────────────────────────────────────────────────────
    # generated.h is multi-MB on a large input and every split generated_*.cpp
    # TU re-includes it, so precompiling it once removes a per-TU reparse that
    # dominates the split TUs' compile time. `#pragma once` in generated.h makes
    # the TUs' own #include a no-op after CMake's forced-include of the PCH.
    #
    # Emscripten is excluded outright: em++'s PCH support is flaky, and the
    # wasm lane's builds are not TU-parallel enough to miss it. MSVC goes
    # through CMake's native /Yc + /Yu handling and stays ON — to be validated
    # on the Windows lane; -DDN2CPP_PCH=OFF is the escape hatch either way.
    #
    # Clang stamps the source files' timestamps into the .pch, which both
    # defeats ccache (a byte-identical re-emit of generated.h would produce a
    # different .pch) and rejects a cache-restored .pch against a re-emitted
    # generated.h whose mtime moved. -fno-pch-timestamp drops the stamps —
    # validation then keys on content-stable properties, which is sound here
    # because the emit is deterministic. Paired with the gate wrappers'
    # CCACHE_SLOPPINESS=pch_defines,time_macros (see gates/_common.sh).
    if(DN2CPP_PCH AND NOT EMSCRIPTEN)
        target_precompile_headers(${DN2CPP_APP_NAME} PRIVATE "${DN2CPP_APP_DIR}/generated.h")
        if(CMAKE_CXX_COMPILER_ID MATCHES "Clang" AND NOT MSVC)
            target_compile_options(${DN2CPP_APP_NAME} PRIVATE -Xclang -fno-pch-timestamp)
        endif()
    endif()

    # P/Invoke libraries the transpiler resolved (one bare library-name token
    # per line — CMake maps each to `-l<name>` on a GNU-like linker or
    # `<name>.lib` on MSVC's link.exe).
    #
    # Not on an Emscripten SIDE MODULE (any of the three shared shapes above):
    # there an undefined P/Invoke symbol is exactly what the shape wants — it
    # stays a wasm import the MAIN module satisfies at dlopen — so a `-l` token
    # would demand an archive on this link line that the side module must not
    # (and, for a non-PIC vendor archive like CRI's, cannot) carry; wasm-ld
    # would fail the link on the missing library instead. A side module that
    # really does want to static-link a PIC archive routes it through
    # DN2CPP_APP_LINK_LIBS. The wasm *executable* (main module) keeps consuming
    # the manifest: there the archive is a link input like anywhere else.
    if(EXISTS "${DN2CPP_APP_DIR}/pinvoke-libs.txt"
            AND NOT (EMSCRIPTEN AND (DN2CPP_GDEXTENSION OR DN2CPP_SHARED OR DN2CPP_DOTNET_MODULE)))
        file(STRINGS "${DN2CPP_APP_DIR}/pinvoke-libs.txt" DN2CPP_PINVOKE_LIBS)
        if(DN2CPP_PINVOKE_LIBS)
            target_link_libraries(${DN2CPP_APP_NAME} PRIVATE ${DN2CPP_PINVOKE_LIBS})
        endif()
    endif()

    # Extra link flags (e.g. a gate's -L<dir> for a native lib it just built).
    if(DN2CPP_APP_LINK_FLAGS)
        separate_arguments(DN2CPP_APP_LINK_FLAGS_LIST NATIVE_COMMAND "${DN2CPP_APP_LINK_FLAGS}")
        target_link_options(${DN2CPP_APP_NAME} PRIVATE ${DN2CPP_APP_LINK_FLAGS_LIST})
    endif()

    # Extra link libraries (full archive paths or -l tokens a gate resolved
    # itself — e.g. a vendor static archive beside a pinvoke-libs.txt token).
    # These must ride LINK_LIBRARIES, not LINK_FLAGS: link *options* land
    # before the object files on the link line, where a static archive cannot
    # resolve anything the objects reference; libraries land after them, in
    # order, after the pinvoke-libs.txt tokens above.
    if(DN2CPP_APP_LINK_LIBS)
        separate_arguments(DN2CPP_APP_LINK_LIBS_LIST NATIVE_COMMAND "${DN2CPP_APP_LINK_LIBS}")
        target_link_libraries(${DN2CPP_APP_NAME} PRIVATE ${DN2CPP_APP_LINK_LIBS_LIST})
    endif()

    # ── Dead-strip unreferenced code ──────────────────────────────────────────
    # A Mach-O executable's globals are not dead-strip roots — the export trie is
    # built after the strip — so the linker walks out from the entry point and
    # drops unreferenced translation-unit tails wholesale. Measured -26%..-73%
    # across the console gates, with byte-identical stdout.
    #
    # A shared library dead-strips too, now that the directory-scoped
    # -fvisibility=hidden (see the top of this file) keeps its export surface
    # down to the DN2CPP_RT_EXPORT / DN2CPP_UCO_EXPORT hooks: only those few
    # exports are roots, so the linker can walk out from them the same way.
    # (With default visibility this was a no-op — every global was exported and
    # therefore a root; measured +1.2% on a GDExtension, paid in alignment.)
    #
    # Not for a hot-update base: dn2cpp_interp.cpp resolves a patch's imports by
    # walking dn2cpp_type_registry -> ti_ -> methtab_, a chain the linker cannot
    # see through from the entry point. --hotupdate-base drops a base-abi.json
    # next to the generated sources, so detect it the way pinvoke-libs.txt is
    # detected above.
    set(_dn2cpp_dead_strip ${DN2CPP_DEAD_STRIP})
    if(EXISTS "${DN2CPP_APP_DIR}/base-abi.json")
        set(_dn2cpp_dead_strip OFF)
    endif()
    if(_dn2cpp_dead_strip AND NOT EMSCRIPTEN)
        if(APPLE)
            target_link_options(${DN2CPP_APP_NAME} PRIVATE "-Wl,-dead_strip")
        elseif(MSVC)
            # /OPT:REF drops unreferenced COMDATs (MSVC's nearest --gc-sections
            # equivalent, effective now that /Gy above put each function in its
            # own COMDAT); /OPT:ICF folds identical ones. Both are the linker's
            # /OPT default under a Release configuration, but CMAKE_BUILD_TYPE
            # is intentionally unset here (see the -O2 comment below), so they
            # are requested explicitly instead of relying on that default.
            target_link_options(${DN2CPP_APP_NAME} PRIVATE "/OPT:REF" "/OPT:ICF")
        else()
            target_link_options(${DN2CPP_APP_NAME} PRIVATE "-Wl,--gc-sections")
        endif()
    endif()

    # ── Strip the symbol table ────────────────────────────────────────────────
    # Nothing else strips it: CMAKE_BUILD_TYPE is unset and the -O2 above is added
    # by hand. For an executable, a post-link `strip`, not -Wl,-S -Wl,-x: the
    # linker flags drop only debug and local symbols and leave a Mach-O
    # executable's global names in LC_SYMTAB, which is where the bytes are.
    #
    # A shared library is the mirror image. A post-link strip is off the table —
    # the re-sign it forces collides with the way an embedded dylib is signed
    # inside an iOS app bundle (the ios-sim gate caught the extension loading
    # but never initializing) — but under the hidden visibility above its global
    # names are all *local* symbols now, exactly what link-time -Wl,-x omits
    # (measured -50% on the mono-module library). The linker signs the output it
    # produces, so nothing is invalidated.
    #
    # Hot-update is unaffected: the interpreter resolves a patch's imports through
    # the dn2cpp_type_registry table, not by symbol name. P/Invoke is unaffected:
    # each [DllImport] binds to an external library symbol at link time (the asm()
    # labels in dn2cpp.h), never through a self-lookup.
    #
    # Emscripten carries no Mach-O/ELF symbol table — skip the whole arm. MSVC
    # has no post-link `strip` step either: a release binary carries no
    # embedded debug info by default (it all lives in the separate .pdb, which
    # this build simply does not copy out), so there is nothing to strip.
    if(DN2CPP_STRIP AND NOT EMSCRIPTEN AND NOT MSVC
            AND (DN2CPP_GDEXTENSION OR DN2CPP_SHARED OR DN2CPP_DOTNET_MODULE))
        target_link_options(${DN2CPP_APP_NAME} PRIVATE "-Wl,-x")
    endif()
    if(DN2CPP_STRIP AND NOT EMSCRIPTEN AND NOT MSVC
            AND NOT (DN2CPP_GDEXTENSION OR DN2CPP_SHARED OR DN2CPP_DOTNET_MODULE))
        add_custom_command(TARGET ${DN2CPP_APP_NAME} POST_BUILD
            COMMAND "${CMAKE_STRIP}" "$<TARGET_FILE:${DN2CPP_APP_NAME}>"
            COMMENT "Stripping ${DN2CPP_APP_NAME}"
            VERBATIM)
        # strip invalidates the arm64 ad-hoc code signature, leaving the binary
        # unrunnable on Apple silicon until it is re-signed. POST_BUILD commands run
        # in the order they are added, so this follows the strip. find_program keeps
        # a host without codesign from failing the build.
        if(APPLE)
            if(NOT DEFINED DN2CPP_CODESIGN)
                find_program(DN2CPP_CODESIGN codesign)
            endif()
            if(DN2CPP_CODESIGN)
                add_custom_command(TARGET ${DN2CPP_APP_NAME} POST_BUILD
                    COMMAND "${DN2CPP_CODESIGN}" --force --sign - "$<TARGET_FILE:${DN2CPP_APP_NAME}>"
                    COMMENT "Re-signing ${DN2CPP_APP_NAME} (ad-hoc) after strip"
                    VERBATIM)
            endif()
        endif()
    endif()

    # MSVC's linked EXE carries no symbol table at all by default (see the
    # comment above — there is nothing for a Windows nm-equivalent to read).
    # The compression-dnbrotli/dnzlib gates need to prove a native-vs-managed
    # codec symbol is present/absent in the final binary; on POSIX that reads
    # straight off Mach-O/ELF's always-present symbol table via `nm`, so mirror
    # DN2CPP_STRIP=OFF's intent ("keep whatever this platform needs to answer
    # that question") with a linker /MAP file — MSVC's documented mechanism for
    # a text-greppable symbol listing when no PDB is being kept either. Only
    # requested when DN2CPP_STRIP=OFF (the same override those two gates
    # already pass), so it costs every other MSVC gate nothing.
    if(NOT DN2CPP_STRIP AND MSVC)
        target_link_options(${DN2CPP_APP_NAME} PRIVATE
            "/MAP:$<TARGET_FILE_DIR:${DN2CPP_APP_NAME}>/$<TARGET_FILE_BASE_NAME:${DN2CPP_APP_NAME}>.map")
    endif()
endif()
