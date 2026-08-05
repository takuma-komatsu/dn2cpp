// dn2cpp_pal_reference.h — the reference PAL's own, target-free extras.
//
// Nothing in the runtime includes this file: the runtime talks to the reference
// PAL through platform/dn2cpp_pal.h like it talks to every other target. What is
// here is the two things a *test* of the reference target needs — the console
// sink hook and the unimplemented-entry count — and the surface a porter copying
// this directory will want to delete first.
//
// See dn2cpp_pal_reference.cpp's header for what the reference target is and is
// not.

#pragma once

#include <cstddef> // size_t

// Redirect the console byte sink. Passing nullptr restores the default (stdio),
// which is what the target starts with. Returns the previously installed sink.
//
// Makes the console seam TESTABLE rather than merely present: on the shipped
// targets dn2cpp_pal_console_write is `fwrite`, so the only way to observe that
// Console.WriteLine really funnels through it is a target where it does
// something else. gates/build-and-run-pal-reference.sh installs a sink that tags
// every write and diffs against the untagged run. A real port implements
// dn2cpp_pal_console_write directly and deletes this file.
//
// The sink is called with the runtime's console lock already held, so it must
// not write to the console itself. It is a plain global with no synchronisation
// of its own: install it before the program starts writing.
//
// Named dn2cpp_reference_* and not dn2cpp_pal_* on purpose:
// gates/build-and-run-doc-claims.sh derives the set of functions a new target
// must define by matching the `dn2cpp_pal_` prefix, and a hook that is not part
// of the contract must not answer that question.
using Dn2CppPalReferenceConsoleSink = void (*)(int stream, const char* bytes, size_t byteCount, void* ctx);
Dn2CppPalReferenceConsoleSink dn2cpp_reference_console_sink_install(
    Dn2CppPalReferenceConsoleSink sink, void* ctx);

// There is deliberately no run-time query for "which entries does this target
// refuse". A refusal here does not return — that is its whole content — so a
// counter it incremented could never be read back, and a counter it did NOT
// increment would be a second copy of the list to keep in sync. The refusal set
// is derived from the source instead, by the gate: `grep -c reference_unimplemented(`
// over the .cpp, diffed against the set docs/PORTING.md names.
