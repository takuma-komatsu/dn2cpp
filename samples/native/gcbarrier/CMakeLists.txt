# Build for the write-barrier probe. It imports the runtime the gate suite
# already built (DN2CPP_RUNTIME_EXPORT) rather than recompiling it, and links the
# same libraries a generated console binary does — the probe's subject is that
# runtime's barrier, so a private build of it would assert nothing.
cmake_minimum_required(VERSION 3.20)
project(dn2cppgcbarrier CXX)

# Both forcings mirror runtime/CMakeLists.txt, which compiled the objects this
# probe links: the Ninja+MSVC generator resolves a default Debug config whose
# /RTC1 cannot be combined with the /O2 below (error D8016), and a probe on the
# dynamic CRT cannot link that runtime's static-CRT archive. Ahead of
# add_executable() — the runtime-library variable is read at target creation.
if(MSVC)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "" FORCE)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

if(NOT DN2CPP_RUNTIME_EXPORT)
    message(FATAL_ERROR "DN2CPP_RUNTIME_EXPORT must name the runtime's dn2cpp-targets.cmake")
endif()
# The export names Threads::Threads in its link interface; an importer that has
# not found the package sees an undefined target, not a missing library.
set(THREADS_PREFER_PTHREAD_FLAG ON)
find_package(Threads REQUIRED)
include("${DN2CPP_RUNTIME_EXPORT}")

add_executable(gcbarrier gcbarrier.cpp)
set_target_properties(gcbarrier PROPERTIES CXX_STANDARD 17 CXX_STANDARD_REQUIRED ON)
target_link_libraries(gcbarrier PRIVATE dn2cpp::dn2cpp_runtime)
if(MSVC)
    # /utf-8: this source's comments hold real UTF-8, which MSVC otherwise reads
    # in the host's ANSI code page (932 on a Japanese install).
    target_compile_options(gcbarrier PRIVATE /O2 /utf-8)
else()
    target_compile_options(gcbarrier PRIVATE -O2 -Wall)
endif()
