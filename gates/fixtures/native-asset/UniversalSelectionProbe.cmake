include("${CMAKE_CURRENT_LIST_DIR}/../../../runtime/cmake/dn2cpp_native_assets.cmake")

set(_package_a_universal "/cache/package.a/1.0.0/runtimes/osx-universal/native/libfixture.a")
set(_package_a_exact "/cache/package.a/1.0.0/runtimes/osx-arm64/native/libfixture.a")
set(_package_b_exact "/cache/package.b/1.0.0/runtimes/osx-arm64/native/libfixture.a")

dn2cpp_prefer_macos_exact_native_assets(
    _same_package "${_package_a_universal}" "${_package_a_exact}")
if(NOT _same_package STREQUAL "${_package_a_exact}")
    message(FATAL_ERROR "exact-RID asset did not shadow its own package's universal asset")
endif()

dn2cpp_prefer_macos_exact_native_assets(
    _different_packages "${_package_a_universal}" "${_package_b_exact}")
list(LENGTH _different_packages _different_package_count)
if(NOT _different_package_count EQUAL 2)
    message(FATAL_ERROR "an exact-RID asset shadowed another package's universal asset")
endif()
