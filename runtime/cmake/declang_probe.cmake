cmake_minimum_required(VERSION 3.21)
if(NOT CMAKE_HOST_SYSTEM_NAME STREQUAL "Darwin")
    message(FATAL_ERROR "DeClang export currently requires macOS")
endif()
if(NOT EXISTS "${COMPILER}" OR IS_DIRECTORY "${COMPILER}")
    message(FATAL_ERROR "DeClang compiler is not an executable file: ${COMPILER}")
endif()
file(MAKE_DIRECTORY "${WORK_DIR}/disabled/.DeClang")
file(WRITE "${WORK_DIR}/disabled/.DeClang/config.json" "{\"enable_obfuscation\":0}\n")
set(ENV{DECLANG_HOME} "${WORK_DIR}/disabled")
execute_process(COMMAND "${COMPILER}" -print-resource-dir OUTPUT_VARIABLE resources OUTPUT_STRIP_TRAILING_WHITESPACE RESULT_VARIABLE status)
if(NOT status STREQUAL "0" OR NOT IS_DIRECTORY "${resources}")
    message(FATAL_ERROR "DeClang cannot report a usable resource directory")
endif()
if(ANDROID_NDK)
    if(NOT ANDROID_ABI)
        set(ANDROID_ABI arm64-v8a)
    endif()
    if(NOT ANDROID_PLATFORM)
        set(ANDROID_PLATFORM android-24)
    endif()
    if(NOT ANDROID_STL)
        set(ANDROID_STL c++_static)
    endif()
    set(ndk_root "${ANDROID_NDK}/toolchains/llvm/prebuilt/darwin-x86_64")
    if(NOT EXISTS "${ndk_root}/bin/clang")
        message(FATAL_ERROR "Android DeClang requires a macOS NDK toolchain")
    endif()
else()
    execute_process(COMMAND xcrun --show-sdk-path OUTPUT_VARIABLE sdk OUTPUT_STRIP_TRAILING_WHITESPACE RESULT_VARIABLE status)
    if(NOT status STREQUAL "0")
        message(FATAL_ERROR "DeClang requires an installed macOS SDK")
    endif()
endif()
file(SHA256 "${COMPILER}" compiler_hash)
file(SHA256 "${CMAKE_CURRENT_LIST_FILE}" probe_hash)
file(SHA256 "${CMAKE_CURRENT_LIST_DIR}/declang_launcher.cmake" launcher_hash)
file(SHA256 "${CMAKE_CURRENT_LIST_DIR}/dn2cpp_declang.cmake" integration_hash)
set(identity "${compiler_hash}\n${probe_hash}\n${launcher_hash}\n${integration_hash}\n${resources}\n${sdk}\n${DEPLOYMENT_TARGET}\n")
file(GLOB_RECURSE resource_files LIST_DIRECTORIES FALSE "${resources}/*")
list(SORT resource_files)
foreach(path IN LISTS resource_files)
    file(SHA256 "${path}" hash)
    string(APPEND identity "${path}:${hash}\n")
endforeach()
foreach(path "${sdk}/SDKSettings.json" "${sdk}/SDKSettings.plist")
    if(EXISTS "${path}")
        file(SHA256 "${path}" hash)
        string(APPEND identity "${hash}\n")
    endif()
endforeach()
if(ANDROID_NDK)
    string(APPEND identity "${ANDROID_NDK}\n${ANDROID_ABI}\n${ANDROID_PLATFORM}\n${ANDROID_STL}\n")
    foreach(path "${CMAKE_CURRENT_LIST_DIR}/android-declang.toolchain.cmake" "${CMAKE_CURRENT_LIST_DIR}/declang_android_link.cmake" "${ANDROID_NDK}/source.properties")
        file(SHA256 "${path}" hash)
        string(APPEND identity "${path}:${hash}\n")
    endforeach()
    # NDK resource libraries and sysroot inputs determine header and link ABI.
    file(GLOB_RECURSE ndk_files LIST_DIRECTORIES FALSE "${ndk_root}/lib/clang/*" "${ndk_root}/sysroot/usr/include/*" "${ndk_root}/sysroot/usr/lib/aarch64-linux-android/*" "${ANDROID_NDK}/build/cmake/*")
    list(APPEND ndk_files "${ndk_root}/bin/clang" "${ndk_root}/bin/clang++" "${ndk_root}/bin/ld.lld" "${ndk_root}/bin/llvm-ar" "${ndk_root}/bin/llvm-ranlib" "${ndk_root}/bin/llvm-readelf" "${ndk_root}/bin/llvm-strip")
    list(SORT ndk_files)
    foreach(path IN LISTS ndk_files)
        file(SHA256 "${path}" hash)
        string(APPEND identity "${path}:${hash}\n")
    endforeach()
endif()
string(SHA256 identity "${identity}")
if(EXISTS "${WORK_DIR}/identity.txt")
    file(READ "${WORK_DIR}/identity.txt" previous)
    if(previous STREQUAL identity)
        message(STATUS "DeClang compatibility probe cached")
        return()
    endif()
endif()
file(REMOVE "${WORK_DIR}/identity.txt")
set(probe_dir "${WORK_DIR}/probe")
file(REMOVE_RECURSE "${probe_dir}")
file(MAKE_DIRECTORY "${probe_dir}/src")
file(WRITE "${probe_dir}/src/generated.cpp" [=[
#include <atomic>
#include <stdexcept>
#include <string>
#include <vector>
extern "C" int dn2cpp_declang_c_probe(void);
__attribute__((noinline)) int selected(int n) {
    volatile int value = n;
    int result = 0;
    for (int i = 0; i < n; ++i) {
        if ((i & 1) == 0) result += value + i;
        else result -= value - i;
    }
    return result;
}
__attribute__((noinline)) int untouched(int n) {
    volatile int value = n;
    return value * 3;
}
int main(int argc, char**) {
    std::atomic<unsigned long long> counter {0};
    counter.fetch_add(1);
    std::vector<std::string> values {"probe"};
    try { throw std::runtime_error(values.at(0)); }
    catch (const std::exception& e) {
        return dn2cpp_declang_c_probe() == 1 && counter.load() == 1 && std::string(e.what()) == "probe" && selected(argc + 4) == 15 && untouched(argc) == 3 ? 0 : 1;
    }
}
]=])
file(WRITE "${probe_dir}/src/smoke.c" "#include <stdatomic.h>\nint dn2cpp_declang_c_probe(void) { atomic_int n = 0; return atomic_fetch_add(&n, 1) + 1; }\n")
file(WRITE "${probe_dir}/targets.json" "{\"version\":1,\"targets\":[{\"cppFile\":\"generated.cpp\",\"symbolPattern\":\"^_Z8selected.*$\",\"seed\":\"0123456789abcdef0123456789abcdef\"}]}\n")
set(launcher "${CMAKE_CURRENT_LIST_DIR}/declang_launcher.cmake")
file(WRITE "${probe_dir}/src/CMakeLists.txt" "cmake_minimum_required(VERSION 3.20)\nproject(DeClangProbe C CXX)\nset(CMAKE_CXX_STANDARD 17)\nset(CMAKE_CXX_COMPILER_LAUNCHER \"${CMAKE_COMMAND};-DCONFIG=${probe_dir}/targets.json;-DAPP_DIR=${probe_dir}/src;-DWORK_DIR=${probe_dir};-P;${launcher};--\")\nadd_executable(probe generated.cpp smoke.c)\ntarget_compile_options(probe PRIVATE -O2)\ntarget_precompile_headers(probe PRIVATE \"$<$<COMPILE_LANGUAGE:CXX>:<string$<ANGLE-R>>\")\n")
set(ninja_arg)
if(NINJA_EXE)
    list(APPEND ninja_arg "-DCMAKE_MAKE_PROGRAM=${NINJA_EXE}")
endif()
if(ANDROID_NDK)
    set(platform_args "-DCMAKE_TOOLCHAIN_FILE=${CMAKE_CURRENT_LIST_DIR}/android-declang.toolchain.cmake" "-DDN2CPP_DECLANG_COMPILER=${COMPILER}" "-DANDROID_NDK=${ANDROID_NDK}" "-DANDROID_ABI=${ANDROID_ABI}" "-DANDROID_PLATFORM=${ANDROID_PLATFORM}" "-DANDROID_STL=${ANDROID_STL}")
else()
    set(platform_args "-DCMAKE_CXX_COMPILER=${COMPILER}" "-DCMAKE_C_COMPILER=${COMPILER}" "-DCMAKE_C_COMPILER_ARG1=--driver-mode=gcc" "-DCMAKE_OSX_DEPLOYMENT_TARGET=${DEPLOYMENT_TARGET}" "-DCMAKE_OSX_SYSROOT=${sdk}")
endif()
execute_process(COMMAND "${CMAKE_COMMAND}" -S "${probe_dir}/src" -B "${probe_dir}/build" -G Ninja ${platform_args} ${ninja_arg} RESULT_VARIABLE status)
if(NOT status STREQUAL "0")
    message(FATAL_ERROR "DeClang compatibility configure failed")
endif()
execute_process(COMMAND "${CMAKE_COMMAND}" --build "${probe_dir}/build" RESULT_VARIABLE status)
if(NOT status STREQUAL "0")
    message(FATAL_ERROR "DeClang compatibility compile or flatten validation failed")
endif()
if(ANDROID_NDK)
    execute_process(COMMAND "${ndk_root}/bin/llvm-readelf" -h "${probe_dir}/build/probe" OUTPUT_VARIABLE elf_header RESULT_VARIABLE status)
    if(NOT status STREQUAL "0" OR NOT elf_header MATCHES "Machine: +AArch64")
        message(FATAL_ERROR "DeClang compatibility output is not an AArch64 ELF")
    endif()
else()
    execute_process(COMMAND "${probe_dir}/build/probe" RESULT_VARIABLE status)
    if(NOT status STREQUAL "0")
        message(FATAL_ERROR "DeClang compatibility executable failed: ${status}")
    endif()
endif()
file(WRITE "${WORK_DIR}/identity.txt" "${identity}")
message(STATUS "DeClang compatibility probe passed")
