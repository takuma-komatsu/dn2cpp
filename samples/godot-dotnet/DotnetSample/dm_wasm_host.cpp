// Emscripten MAIN_MODULE host for the Godot .NET-module drop-in: it dlopens the
// side module and calls godotsharp_game_main_init exactly as the engine would, so
// a failure here is the drop-in's and not Godot's. Run under node.
//
// Built as -sMAIN_MODULE=1 -fwasm-exceptions because the side module imports the
// __cpp_exception tag and the __cxa_* runtime from the main module — this host is
// what its C++ exceptions unwind THROUGH.
//
// Modes (argv[2]): `badsize` claims a wrong interop-table size, so the transpiled
// NativeFuncs.Initialize throws and the throw must unwind across the dlink boundary
// into the entry's catch and be reported rather than trap; `ok` uses the reported
// size and must succeed. The two MUST run in separate processes — Initialize
// latches `initialized` before validating, so a later call reports "Already
// initialized." instead.
//
// The interop table handed over is ZEROED, which caps this host at the writes: the
// entry stores the table and fills the callback struct without calling through it,
// but the registration tail runs GodotSharp cctors that call BACK through the table
// and ends in a null indirect call. That tail is asserted for real by
// gates/build-and-run-godot-editor-export-web.sh and
// gates/build-and-run-godot-dotnet-trim.sh.

#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <dlfcn.h>

namespace
{
// The drop-in's ABI, exactly as src/Dn2Cpp.DotnetModule/DotnetModuleBackend.cs emits it.
using GodotSharpGameMainInit = int8_t (*)(void* godot_dll_handle, void* out_managed_callbacks,
                                          const void** interop_funcs, int32_t interop_funcs_size_bytes);
using Dn2CppDmInteropSize = int32_t (*)(void);

// The ManagedCallbacks struct's exact size is the drop-in's business, not this
// host's: over-allocating a write target is safe, guessing it short is not.
constexpr size_t kManagedCallbacksCapacity = 4096;

void* g_lib = nullptr;

void* sym(const char* name)
{
    void* p = dlsym(g_lib, name);
    if (p == nullptr)
    {
        std::fprintf(stderr, "FAIL dlsym %s: %s\n", name, dlerror());
        std::exit(2);
    }
    return p;
}
} // namespace

int main(int argc, char** argv)
{
    if (argc < 3)
    {
        std::fprintf(stderr, "usage: dm_wasm_host <side-module.so> <badsize|ok>\n");
        return 2;
    }
    const char* mode = argv[2];

    g_lib = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (g_lib == nullptr)
    {
        std::fprintf(stderr, "FAIL dlopen: %s\n", dlerror());
        return 2;
    }
    std::printf("host: dlopen OK\n");

    auto entry = reinterpret_cast<GodotSharpGameMainInit>(sym("godotsharp_game_main_init"));
    auto interop_size = reinterpret_cast<Dn2CppDmInteropSize>(sym("dn2cpp_dm_interop_size"));
    std::printf("host: dlsym OK (godotsharp_game_main_init, dn2cpp_dm_interop_size)\n");

    const int32_t real_size = interop_size();
    std::printf("host: drop-in reports interop table size = %d bytes\n", (int)real_size);
    if (real_size <= 0)
    {
        std::fprintf(stderr, "FAIL: nonsensical interop size %d\n", (int)real_size);
        return 1;
    }

    // Room for whichever size we claim, so a size the entry MISBELIEVES can never
    // make it read off the end of a real allocation.
    const int32_t claimed = std::strcmp(mode, "badsize") == 0 ? real_size + 8 : real_size;
    const size_t alloc = static_cast<size_t>(real_size > claimed ? real_size : claimed);

    void** interop = static_cast<void**>(std::calloc(alloc, 1));
    void* managed_callbacks = std::calloc(kManagedCallbacksCapacity, 1);
    if (interop == nullptr || managed_callbacks == nullptr)
    {
        std::fprintf(stderr, "FAIL: out of memory\n");
        return 2;
    }

    std::printf("host: calling godotsharp_game_main_init (size=%d, real=%d)\n", (int)claimed, (int)real_size);
    const int8_t rc = entry(nullptr, managed_callbacks, const_cast<const void**>(interop), claimed);
    // Reaching this line is half the assertion: an unwind that failed to cross the
    // dlink boundary would have trapped the instance instead of returning.
    std::printf("host: entry returned %d (no trap — the unwind crossed the dlink boundary)\n", (int)rc);

    if (std::strcmp(mode, "badsize") == 0)
    {
        if (rc != 0)
        {
            std::fprintf(stderr, "FAIL: entry accepted a wrong interop size (returned %d, want 0)\n", (int)rc);
            return 1;
        }
        std::printf("host: badsize OK — the size mismatch was REPORTED, not trapped\n");
        return 0;
    }

    if (rc != 1)
    {
        std::fprintf(stderr, "FAIL: entry failed with the size it reported itself (returned %d, want 1)\n", (int)rc);
        return 1;
    }
    std::printf("host: init OK\n");
    return 0;
}
