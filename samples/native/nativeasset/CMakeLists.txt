cmake_minimum_required(VERSION 3.20)
project(dn2cpp_native_asset C)
set(DN2CPP_SHARED_DEP_VALUE 3 CACHE STRING "Shared dependency fixture result")

option(DN2CPP_NATIVE_ASSET_SHARED "Build the negative-gate shared payload" OFF)
if(DN2CPP_NATIVE_ASSET_SHARED)
    add_library(dn2cpp_native_asset SHARED nativeasset.c)
else()
    add_library(dn2cpp_native_dependency STATIC nativedependency.c)
    add_library(dn2cpp_native_asset STATIC nativeasset.c)
    target_link_libraries(dn2cpp_native_asset PRIVATE dn2cpp_native_dependency)
endif()
set_target_properties(dn2cpp_native_asset PROPERTIES
    C_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN ON)
if(TARGET dn2cpp_native_dependency)
    set_target_properties(dn2cpp_native_dependency PROPERTIES
        C_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN ON)
endif()

if((UNIX OR WIN32) AND NOT DN2CPP_NATIVE_ASSET_SHARED)
    add_library(dn2cpp_shared_dependency SHARED shareddependency.c)
    add_library(dn2cpp_shared_asset SHARED sharedasset.c)
    target_link_libraries(dn2cpp_shared_asset PRIVATE dn2cpp_shared_dependency)
    target_compile_definitions(dn2cpp_shared_dependency PRIVATE
        DN2CPP_SHARED_DEP_VALUE=${DN2CPP_SHARED_DEP_VALUE})
    set_target_properties(dn2cpp_shared_dependency PROPERTIES
        C_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN ON)
    set_target_properties(dn2cpp_shared_asset PROPERTIES
        C_VISIBILITY_PRESET hidden
        VISIBILITY_INLINES_HIDDEN ON)
    if(APPLE)
        add_library(dn2cpp_no_id_bundle MODULE shareddependency.c)
        set_target_properties(dn2cpp_no_id_bundle PROPERTIES
            OUTPUT_NAME "dn2cpp_no_id"
            PREFIX ""
            SUFFIX ".bundle"
            C_VISIBILITY_PRESET hidden
            VISIBILITY_INLINES_HIDDEN ON)
        set_target_properties(dn2cpp_shared_asset PROPERTIES
            BUILD_WITH_INSTALL_NAME_DIR ON
            INSTALL_NAME_DIR "/non-relocatable/nuget-cache")
        set_target_properties(dn2cpp_shared_dependency PROPERTIES
            BUILD_WITH_INSTALL_NAME_DIR ON
            INSTALL_NAME_DIR "/non-relocatable/nuget-cache")
    elseif(UNIX)
        set_target_properties(dn2cpp_shared_asset PROPERTIES NO_SONAME ON)
        set_target_properties(dn2cpp_shared_dependency PROPERTIES NO_SONAME ON)
        add_library(dn2cpp_soname_asset SHARED shareddependency.c)
        set_target_properties(dn2cpp_soname_asset PROPERTIES
            C_VISIBILITY_PRESET hidden
            VISIBILITY_INLINES_HIDDEN ON
            SOVERSION 1)
    endif()
endif()
