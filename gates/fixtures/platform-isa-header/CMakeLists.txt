cmake_minimum_required(VERSION 3.20)
project(PlatformIsaAvx10Headers LANGUAGES CXX)

get_filename_component(DN2CPP_REPO_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../../.." ABSOLUTE)
set(DN2CPP_CORE "${DN2CPP_REPO_ROOT}/runtime/core")
option(DN2CPP_AVX10_EXPECT_X64 "Require x86-64 instruction bodies" OFF)

file(GLOB avx10_headers CONFIGURE_DEPENDS
    "${DN2CPP_CORE}/isa/x86/dn2cpp_isa_x86_avx10*.h"
    "${DN2CPP_CORE}/isa/x86/dn2cpp_isa_x86_avxvnniint*_v512.h")
if(NOT avx10_headers)
    message(FATAL_ERROR "No generated AVX10 headers found")
endif()

set(probe "#include \"dn2cpp.h\"\n#include \"dn2cpp_vectors.h\"\n")
set(helper_names)
foreach(header IN LISTS avx10_headers)
    file(RELATIVE_PATH relative_header "${DN2CPP_CORE}" "${header}")
    string(APPEND probe "#include \"${relative_header}\"\n")
    file(STRINGS "${header}" definitions
        REGEX "DN2CPP_ISA_INLINE .* dn2cpp_isa_[a-z0-9_]+\\(")
    if(NOT definitions)
        message(FATAL_ERROR "No helper declarations in ${relative_header}")
    endif()
    foreach(definition IN LISTS definitions)
        string(REGEX REPLACE ".* (dn2cpp_isa_[a-z0-9_]+)\\(.*" "\\1" name "${definition}")
        list(APPEND helper_names "${name}")
    endforeach()
endforeach()
list(REMOVE_DUPLICATES helper_names)
list(SORT helper_names)
list(LENGTH helper_names helper_count)

string(APPEND probe "#if DN2CPP_AVX10_EXPECT_X64 && !DN2CPP_TARGET_X64\n#error Expected x86-64 AVX10 instruction bodies\n#endif\n")
# An externally visible address table forces code generation for every inline
# helper, including immediate switches that an unused include would not check.
string(APPEND probe "extern \"C\" void (*const avx10_helpers[])() = {\n")
foreach(name IN LISTS helper_names)
    string(APPEND probe "    reinterpret_cast<void (*)()>(&${name}),\n")
endforeach()
string(APPEND probe "};\n")
file(WRITE "${CMAKE_CURRENT_BINARY_DIR}/avx10_headers.cpp" "${probe}")

add_library(platform_isa_avx10_headers OBJECT "${CMAKE_CURRENT_BINARY_DIR}/avx10_headers.cpp")
target_compile_features(platform_isa_avx10_headers PRIVATE cxx_std_17)
target_include_directories(platform_isa_avx10_headers PRIVATE "${DN2CPP_CORE}")
target_compile_definitions(platform_isa_avx10_headers PRIVATE
    DN2CPP_AVX10_EXPECT_X64=$<BOOL:${DN2CPP_AVX10_EXPECT_X64}>)
message(STATUS "AVX10 compile closure: ${helper_count} helpers; require x86-64=${DN2CPP_AVX10_EXPECT_X64}")
