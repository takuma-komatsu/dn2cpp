#pragma once
// dn2cpp_isa.h — umbrella for the hardware-intrinsic helpers the transpiler
// calls (dn2cpp_isa_<arch>_<type>_<method>_<argsig>). Common support first,
// then the generated per-family headers.
#include "dn2cpp_isa_common.h"
#include "dn2cpp_isa_families.g.h"
