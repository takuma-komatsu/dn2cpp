//
// dn2cpp minimal runtime — object model and intrinsics for transpiled IL.
//
// The generated-code-facing entry header (the ConsoleBackend's RuntimeHeader;
// the Godot / dotnetmodule lane headers layer on top of it). The portable-SIMD
// vector surface is NOT pulled in here: generated code — the only consumer of
// dn2cpp_vec_* — includes dn2cpp_vectors.h explicitly beside this header, so
// the runtime's own translation units (which reach this header through the
// lane headers) never preprocess the ~100k lines of hwy/highway.h it costs
// under the default DN2CPP_USE_HIGHWAY=ON.

#pragma once

#include "dn2cpp_core.h"
