cmake_minimum_required(VERSION 3.20)
project(dn2cpp_editor_export_dependency C)

add_library(dn2cpp_editor_export_dependency SHARED dependency.c)
set_target_properties(dn2cpp_editor_export_dependency PROPERTIES
    WINDOWS_EXPORT_ALL_SYMBOLS ON
    C_STANDARD 11)
