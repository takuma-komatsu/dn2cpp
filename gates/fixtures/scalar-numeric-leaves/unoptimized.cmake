# Keep representation-read parity independent of optimizer cancellation.
# Apply after target creation so its PCH and generated units use the same mode.
function(dn2cpp_scalar_leaves_unoptimized)
    if(MSVC)
        target_compile_options("${DN2CPP_APP_NAME}" PRIVATE /Od)
    else()
        target_compile_options("${DN2CPP_APP_NAME}" PRIVATE -O0)
    endif()
endfunction()
cmake_language(DEFER CALL dn2cpp_scalar_leaves_unoptimized)
