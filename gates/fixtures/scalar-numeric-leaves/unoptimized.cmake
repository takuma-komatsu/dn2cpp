# Keep representation-read parity independent of optimizer cancellation.
file(GLOB scalar_leaf_sources "${DN2CPP_APP_DIR}/generated*.cpp")
if(MSVC)
    set_source_files_properties(${scalar_leaf_sources} PROPERTIES COMPILE_OPTIONS "/Od")
else()
    set_source_files_properties(${scalar_leaf_sources} PROPERTIES COMPILE_OPTIONS "-O0")
endif()
