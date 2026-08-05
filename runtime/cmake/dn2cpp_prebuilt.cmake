# dn2cpp_prebuilt.cmake — the toolchain bundle's prebuilt runtime.
#
# A bundle may carry <root>/prebuilt/<axis>/: the runtime archives plus the
# dn2cpp-targets.cmake that names them, so an export imports the runtime instead
# of configuring and compiling it (mostly saving the vendored curl's configure —
# see docs/EDITOR-EXPORT-DESIGN.md §4).
#
# One directory per AXIS — the host's, the three iOS slots an iOS export
# configures on a macOS host, and Android and Web on a packaging host carrying
# their cross toolchain. The axis directory's NAME is documentation only: the
# selection below reads every key.txt and takes the one describing this
# configure, because a name is a second expression for the axis and drifts.
#
# The key is defined ONCE and both sides read it here: dist/package-toolchain.sh
# asks for it with DN2CPP_EMIT_PREBUILT_KEY, the discovery below compares against
# what it wrote. A second expression would drift, and the drift is fail-open — an
# archive from another toolchain accepted, failing at link with no cause attached.

# _dn2cpp_key_path OUT VALUE — a path INSIDE the bundle is recorded relative to
# its root, because an absolute one pins the key to the machine that packaged it
# and every consumer elsewhere refuses the archives. Both sides are resolved
# first: a configure reached through a symlink (a macOS .app's
# Contents/MacOS/GodotSharp) spells the root differently and an unresolved
# comparison silently finds nothing under it. A path outside stays absolute —
# it identifies a toolchain the bundle does not carry.
function(_dn2cpp_key_path out value)
    set(${out} "${value}" PARENT_SCOPE)
    if(NOT DN2CPP_ROOT OR NOT EXISTS "${value}")
        return()
    endif()
    file(REAL_PATH "${value}" real)
    file(REAL_PATH "${DN2CPP_ROOT}" root)
    cmake_path(IS_PREFIX root "${real}" NORMALIZE under)
    if(under)
        cmake_path(RELATIVE_PATH real BASE_DIRECTORY "${root}" OUTPUT_VARIABLE rel)
        set(${out} "\${DN2CPP_ROOT}/${rel}" PARENT_SCOPE)
    endif()
endfunction()

# dn2cpp_prebuilt_key OUT — everything that changes the archives' bytes: the
# toolchain that compiled them and every option that reaches a runtime target.
# The make program is NOT one of those, and keying on it would make every bundle
# refuse its own byte-identical archives the moment the cmake pin moved; what the
# cmake-derived terms below need is that both sides run ONE cmake, which
# dist/package-toolchain.sh §3c arranges by building through the staged pair.
function(dn2cpp_prebuilt_key out)
    set(k "")
    foreach(v IN ITEMS
            CMAKE_SYSTEM_NAME CMAKE_SYSTEM_PROCESSOR CMAKE_SIZEOF_VOID_P
            CMAKE_CXX_COMPILER_ID CMAKE_CXX_COMPILER_VERSION
            CMAKE_C_COMPILER_ID CMAKE_C_COMPILER_VERSION
            CMAKE_OSX_SYSROOT CMAKE_OSX_ARCHITECTURES CMAKE_OSX_DEPLOYMENT_TARGET
            CMAKE_ANDROID_ARCH_ABI CMAKE_ANDROID_API CMAKE_ANDROID_NDK
            # CMAKE_ANDROID_API is EMPTY under the NDK's own toolchain file (it is
            # CMake's built-in Android support that fills it), so the API level
            # reaches the key only through ANDROID_PLATFORM. EMSCRIPTEN_VERSION is
            # what identifies a Web axis's emsdk: relativizing the toolchain path
            # of a bundled one leaves the LLVM major as the only other trace of it.
            ANDROID_PLATFORM CMAKE_TOOLCHAIN_FILE
            EMSCRIPTEN EMSCRIPTEN_VERSION
            DN2CPP_USE_GC DN2CPP_USE_ZLIB DN2CPP_USE_BROTLI DN2CPP_USE_CURL
            DN2CPP_USE_HIGHWAY DN2CPP_HIGHWAY_ARCH
            DN2CPP_GODOT DN2CPP_DOTNET_MODULE
            DN2CPP_HIDDEN_VISIBILITY DN2CPP_PAL_REFERENCE DN2CPP_MAX_STACK_FRAME
            DN2CPP_NULL_CHECKS DN2CPP_ARITH_CHECKS)
        set(val "${${v}}")
        if(v STREQUAL "CMAKE_TOOLCHAIN_FILE" OR v STREQUAL "CMAKE_ANDROID_NDK")
            _dn2cpp_key_path(val "${val}")
        endif()
        string(APPEND k "${v}=${val}\n")
    endforeach()
    set(${out} "${k}" PARENT_SCOPE)
endfunction()

if(DN2CPP_EMIT_PREBUILT_KEY)
    dn2cpp_prebuilt_key(_dn2cpp_key)
    file(WRITE "${DN2CPP_EMIT_PREBUILT_KEY}" "${_dn2cpp_key}")
endif()

# dn2cpp_use_prebuilt_runtime — point DN2CPP_RUNTIME_EXPORT at the bundle's
# prebuilt when it describes this configure, and say which way it went either
# way: a silent fallback reads as "the bundle shipped no prebuilt".
#
# Refusing is always safe — it is what every build did before there was one — so
# every uncertainty resolves that way. The stamp pairs the archives with the
# sources beside them: `--install-into` overwrites a bundle without deleting what
# the new one dropped, so a stale prebuilt/ can outlive the runtime/ it was cut
# from, and that is the one mismatch the toolchain key cannot see.
function(dn2cpp_use_prebuilt_runtime out_export)
    set(${out_export} "" PARENT_SCOPE)
    if(DN2CPP_NO_PREBUILT)
        return()
    endif()
    file(GLOB axes LIST_DIRECTORIES true "${DN2CPP_ROOT}/prebuilt/*")
    if(NOT axes)
        return()
    endif()
    if(NOT EXISTS "${DN2CPP_ROOT}/runtime/dn2cpp-source-stamp.txt")
        message(STATUS "dn2cpp: prebuilt runtime ignored (incomplete: no source stamp)")
        return()
    endif()
    file(READ "${DN2CPP_ROOT}/runtime/dn2cpp-source-stamp.txt" stamp)
    string(STRIP "${stamp}" stamp)
    dn2cpp_prebuilt_key(want)
    string(APPEND want "SOURCE_STAMP=${stamp}\n")
    set(carried "")
    foreach(dir IN LISTS axes)
        if(NOT IS_DIRECTORY "${dir}")
            continue()
        endif()
        get_filename_component(axis "${dir}" NAME)
        if(NOT EXISTS "${dir}/key.txt" OR NOT EXISTS "${dir}/dn2cpp-targets.cmake")
            message(STATUS "dn2cpp: prebuilt runtime '${axis}' ignored (incomplete: no key.txt or no dn2cpp-targets.cmake)")
            continue()
        endif()
        list(APPEND carried "${axis}")
        file(READ "${dir}/key.txt" recorded)
        if(recorded STREQUAL want)
            message(STATUS "dn2cpp: using the prebuilt runtime at ${dir}")
            set(${out_export} "${dir}/dn2cpp-targets.cmake" PARENT_SCOPE)
            return()
        endif()
    endforeach()
    if(carried)
        list(JOIN carried ", " carried)
        message(STATUS "dn2cpp: prebuilt runtime ignored (none of ${carried} was built for this toolchain or this source tree); building from source")
    endif()
endfunction()
