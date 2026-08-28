// Drives the string runtime's pre-allocation guards directly. Corrupt-layout
// arms are separate processes because their contract is immediate termination;
// overflow arms stay in-process to prove the managed OOM remains catchable.

#include <cstdint>
#include <cstdio>
#include <cstring>
#include <limits>
#include <stdexcept>

#include "dn2cpp_core.h"

// Generated programs supply these tables. This native probe has no managed
// image, so it supplies the emitter's empty-table forms.
const Dn2CppTypeRegEntry dn2cpp_type_registry[] = { {} };
const int32_t dn2cpp_type_registry_count = 0;
const Dn2CppTypeBind dn2cpp_type_binds[] = { {} };
const int32_t dn2cpp_type_bind_count = 0;
const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[] = { {} };
const int32_t dn2cpp_assembly_registry_count = 0;
const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[] = { {} };
const int32_t dn2cpp_delegate_refl_registry_count = 0;
const Dn2CppBclMessage dn2cpp_bcl_messages[] = { {} };
const int32_t dn2cpp_bcl_message_count = 0;
const int32_t dn2cpp_exception_get_message_slot = 0;
const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = nullptr;
const int32_t dn2cpp_runtime_template_count = 0;

namespace
{

int g_render_calls;

[[noreturn]] Dn2CppString* ThrowNative(Dn2CppObject*)
{
    g_render_calls++;
    throw std::runtime_error("broken exception message renderer");
}

const void* g_broken_vtable[] = {
    reinterpret_cast<const void*>(&ThrowNative)
};

const Dn2CppTypeInfo g_not_string_type = {
    "Probe.NotString", nullptr, static_cast<int32_t>(sizeof(Dn2CppObject)),
    nullptr, nullptr, 0
};

const Dn2CppTypeInfo g_broken_exception_type = {
    "Probe.BrokenException", &dn2cpp_exception_type,
    static_cast<int32_t>(sizeof(Dn2CppExceptionObject)), g_broken_vtable, nullptr, 0
};

bool IsOom(Dn2CppException& ex)
{
    bool result = ex.obj != nullptr && ex.obj->type == &dn2cpp_out_of_memory_exception_type;
    dn2cpp_exc_inflight_pop(ex.obj);
    return result;
}

int NormalStrings()
{
    const char japanese[] = {
        static_cast<char>(0xE6), static_cast<char>(0x97), static_cast<char>(0xA5),
        static_cast<char>(0xE6), static_cast<char>(0x9C), static_cast<char>(0xAC)
    };
    Dn2CppString* ascii = dn2cpp_string_from_utf8("plain", 5);
    Dn2CppString* wide = dn2cpp_string_from_utf8(japanese, sizeof(japanese));
    const char16_t faceChars[] = { 0xD83D, 0xDE03 };
    Dn2CppString* face = dn2cpp_string_from_chars(faceChars, 2);
    char encoded[8] = {};
    int32_t faceBytes = dn2cpp_string_to_utf8(face, encoded, sizeof(encoded));
    const unsigned char expectedFace[] = { 0xF0, 0x9F, 0x98, 0x83 };
    Dn2CppString* joined = dn2cpp_string_concat3(nullptr, ascii, nullptr);

    if (ascii->length != 5 || wide->length != 2 || faceBytes != 4 ||
        std::memcmp(encoded, expectedFace, sizeof(expectedFace)) != 0 ||
        joined->length != ascii->length ||
        std::memcmp(joined->chars, ascii->chars,
            static_cast<size_t>(ascii->length) * sizeof(char16_t)) != 0)
    {
        std::fprintf(stderr, "normal string result mismatch\n");
        return 1;
    }
    std::puts("normal strings: ascii+japanese+surrogate+null-concat ok");
    return 0;
}

int OverflowGuards()
{
    Dn2CppString hugeA = { { &dn2cpp_string_type },
        std::numeric_limits<int32_t>::max(), u"x" };
    Dn2CppString hugeB = { { &dn2cpp_string_type }, 1, u"y" };
    bool concatOom = false;
    bool utf8Oom = false;
    try
    {
        (void)dn2cpp_string_concat2(&hugeA, &hugeB);
    }
    catch (Dn2CppException& ex)
    {
        concatOom = IsOom(ex);
    }
    try
    {
        (void)dn2cpp_string_checked_length(
            static_cast<int64_t>(std::numeric_limits<int32_t>::max()) + 1);
    }
    catch (Dn2CppException& ex)
    {
        utf8Oom = IsOom(ex);
    }
    if (!concatOom || !utf8Oom)
    {
        std::fprintf(stderr, "overflow did not raise catchable OutOfMemoryException\n");
        return 1;
    }
    std::puts("overflow guards: concat=oom utf8-byte-count=oom");
    return 0;
}

int BadType()
{
    Dn2CppString s = { { &g_not_string_type }, 1, u"x" };
    return dn2cpp_string_to_utf8(&s, nullptr, 0);
}

int BadLength()
{
    Dn2CppString s = { { &dn2cpp_string_type }, -1, u"x" };
    return dn2cpp_string_to_utf8(&s, nullptr, 0);
}

int BadChars()
{
    Dn2CppString s = { { &dn2cpp_string_type }, 1, nullptr };
    return dn2cpp_string_to_utf8(&s, nullptr, 0);
}

int BadAllocation()
{
    char16_t* chars = nullptr;
    (void)dn2cpp_string_alloc(&chars, -1);
    return 0;
}

int BrokenBoundary()
{
    Dn2CppExceptionObject ex = {};
    ex.type = &g_broken_exception_type;
    dn2cpp_report_boundary_exception(reinterpret_cast<Dn2CppObject*>(&ex),
        "string guard probe");
    if (g_render_calls != 1)
    {
        std::fprintf(stderr, "boundary renderer called %d times\n", g_render_calls);
        return 1;
    }
    std::puts("boundary fallback: one ToString call");
    return 0;
}

} // namespace

int main(int argc, char** argv)
{
    dn2cpp_runtime_init();
    if (argc != 2)
    {
        std::fprintf(stderr,
            "usage: stringguards normal|overflow|bad-type|bad-length|bad-chars|bad-allocation|boundary\n");
        return 2;
    }
    if (std::strcmp(argv[1], "normal") == 0) return NormalStrings();
    if (std::strcmp(argv[1], "overflow") == 0) return OverflowGuards();
    if (std::strcmp(argv[1], "bad-type") == 0) return BadType();
    if (std::strcmp(argv[1], "bad-length") == 0) return BadLength();
    if (std::strcmp(argv[1], "bad-chars") == 0) return BadChars();
    if (std::strcmp(argv[1], "bad-allocation") == 0) return BadAllocation();
    if (std::strcmp(argv[1], "boundary") == 0) return BrokenBoundary();
    std::fprintf(stderr, "unknown mode: %s\n", argv[1]);
    return 2;
}
