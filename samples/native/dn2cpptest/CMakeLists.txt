# Portable build for the pinvoke-native gate's custom native test library.
#
# macOS/Linux still build it with a direct `clang -shared` invocation (see
# gates/build-and-run-pinvoke-native.sh) — that path is untouched. On Windows,
# a direct cl.exe invocation from the gate script would need to fight Git Bash's
# argv path-mangling (MSVC switches start with '/', which collides with MSYS's
# POSIX-path heuristic) and MSVC exports nothing from a DLL by default (unlike
# -shared on GNU/clang, which exports every global symbol) — WINDOWS_EXPORT_ALL_SYMBOLS
# below reproduces that Unix default instead of hand-listing every
# dn2cpptest_* function. Going through CMake, the same backend every other
# gate already requires, sidesteps both problems at once.
cmake_minimum_required(VERSION 3.20)
project(dn2cpptest C)

find_package(Threads REQUIRED)

add_library(dn2cpptest SHARED dn2cpptest.c)
target_link_libraries(dn2cpptest PRIVATE Threads::Threads)
set_target_properties(dn2cpptest PROPERTIES
    WINDOWS_EXPORT_ALL_SYMBOLS ON
    C_STANDARD 11)
if(MSVC)
    target_compile_options(dn2cpptest PRIVATE /O2)
else()
    target_compile_options(dn2cpptest PRIVATE -O2 -fPIC)
endif()
