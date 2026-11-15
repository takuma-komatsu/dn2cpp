cmake_minimum_required(VERSION 3.20)
project(dn2cppgodotshutdown CXX)

if(MSVC)
    set(CMAKE_BUILD_TYPE Release CACHE STRING "" FORCE)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
endif()

if(NOT DN2CPP_RUNTIME_EXPORT)
    message(FATAL_ERROR "DN2CPP_RUNTIME_EXPORT must name the runtime's dn2cpp-targets.cmake")
endif()
set(THREADS_PREFER_PTHREAD_FLAG ON)
find_package(Threads REQUIRED)
include("${DN2CPP_RUNTIME_EXPORT}")

# A controlled wait observation is only present in this replacement GC unit.
# The normal archive's GC object is left unextracted; all other runtime objects
# are reused, and shipped runtimes carry no probe hook.
add_executable(godotshutdown shutdown.cpp ../../../runtime/core/dn2cpp_gc.cpp)
target_compile_definitions(godotshutdown PRIVATE DN2CPP_FINALIZER_SHUTDOWN_TEST)
set_target_properties(godotshutdown PROPERTIES CXX_STANDARD 17 CXX_STANDARD_REQUIRED ON)
target_link_libraries(godotshutdown PRIVATE dn2cpp::dn2cpp_dotnetmodule)
if(MSVC)
    target_compile_options(godotshutdown PRIVATE /O2 /utf-8)
    target_compile_definitions(godotshutdown PRIVATE NOMINMAX WIN32_LEAN_AND_MEAN)
else()
    target_compile_options(godotshutdown PRIVATE -O2 -Wall)
endif()
