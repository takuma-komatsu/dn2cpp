// dlopen host for the UcoShared gate: loads the transpiled shared library,
// initializes the runtime through the generated `main`, then calls the exported
// [UnmanagedCallersOnly] entry points from the main thread and from a foreign
// thread the collector has never seen.
#include <cstdint>
#include <cstdio>
#include <cstdlib>
#include <thread>

#ifdef _WIN32
#include <io.h>     // _setmode / _fileno
#include <fcntl.h>  // _O_BINARY
// Windows has no dlfcn.h. Shimmed under the POSIX names so every call site
// below is untouched; this is the only platform-specific block in the file.
#include <windows.h>
namespace
{
constexpr int RTLD_NOW = 0;
constexpr int RTLD_LOCAL = 0;
void* dlopen(const char* path, int) { return static_cast<void*>(LoadLibraryA(path)); }
void* dlsym(void* handle, const char* name) { return reinterpret_cast<void*>(GetProcAddress(static_cast<HMODULE>(handle), name)); }
int dlclose(void* handle) { return FreeLibrary(static_cast<HMODULE>(handle)) ? 0 : -1; }
const char* dlerror()
{
    static char buf[256];
    DWORD n = FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, nullptr,
                              GetLastError(), 0, buf, sizeof(buf), nullptr);
    if (n == 0)
        std::snprintf(buf, sizeof(buf), "error 0x%08lx", GetLastError());
    return buf;
}
}
#else
#include <dlfcn.h>
#endif

namespace
{
// Must mirror the managed UcoShared.Pair exactly.
struct Pair
{
    int32_t x;
    int32_t y;
};

void* g_lib = nullptr;

void* sym(const char* name)
{
    void* p = dlsym(g_lib, name);
    if (p == nullptr)
    {
        std::fprintf(stderr, "dlsym %s: %s\n", name, dlerror());
        std::exit(2);
    }
    return p;
}

int32_t alloc_sum_expected(int32_t n)
{
    int32_t sum = 0;
    for (int32_t i = 0; i < n; i++)
        sum += i % 7;
    return sum;
}

void check(const char* what, int64_t actual, int64_t expected)
{
    if (actual != expected)
    {
        std::fprintf(stderr, "FAIL %s: got %lld, want %lld\n",
                     what, (long long)actual, (long long)expected);
        std::exit(1);
    }
    std::printf("%s=%lld\n", what, (long long)actual);
}
} // namespace

int main(int argc, char** argv)
{
#ifdef _WIN32
    // The MSVC CRT defaults to TEXT mode. This host is not dn2cpp-generated, so
    // it never reaches dn2cpp_runtime_init, which does the same for a transpiled
    // program; the gate diffs against an LF literal.
    _setmode(_fileno(stdout), _O_BINARY);
    _setmode(_fileno(stderr), _O_BINARY);
#endif
    if (argc < 2)
    {
        std::fprintf(stderr, "usage: driver <shared-library>\n");
        return 2;
    }
    g_lib = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (g_lib == nullptr)
    {
        std::fprintf(stderr, "dlopen: %s\n", dlerror());
        return 2;
    }

    // Running the generated entry once initializes the runtime; the managed Main
    // is a no-op.
    auto lib_main = (int (*)())sym("main");
    check("init", lib_main(), 0);

    // Enable foreign-thread GC registration before any off-main-thread call.
    auto set_reg = (void (*)(int))sym("dn2cpp_set_native_callback_gc_registration");
    set_reg(1);

    auto uco_add = (int32_t (*)(int32_t, int32_t))sym("uco_add");
    auto uco_pair_swap = (Pair (*)(Pair))sym("uco_pair_swap");
    auto uco_secret = (int32_t (*)())sym("uco_secret");
    auto uco_alloc_sum = (int32_t (*)(int32_t))sym("uco_alloc_sum");
    auto uco_get_mul = (intptr_t (*)())sym("uco_get_mul");

    check("add", uco_add(20, 22), 42);
    Pair p = uco_pair_swap(Pair{3, 4});
    check("swap.x", p.x, 4);
    check("swap.y", p.y, 3);
    check("secret", uco_secret(), 12345);
    check("alloc_main", uco_alloc_sum(100000), alloc_sum_expected(100000));

    // A non-exported [UnmanagedCallersOnly] method's address, handed out through
    // an exported entry point.
    auto mul = (int32_t (*)(int32_t, int32_t))uco_get_mul();
    check("mul", mul(6, 7), 42);

    // The collector has never seen this thread; only the prologue's registration
    // makes an allocation loop this large safe here.
    int32_t t_alloc = -1;
    int32_t t_mul = -1;
    std::thread th([&] {
        t_alloc = uco_alloc_sum(400000);
        t_mul = mul(t_alloc % 1000, 3);
    });
    th.join();
    check("alloc_thread", t_alloc, alloc_sum_expected(400000));
    check("mul_thread", t_mul, (alloc_sum_expected(400000) % 1000) * 3);

    // The pool threads Task.Run starts execute code the dlclose below unmaps, so
    // the quiesce must stop and join them all first; a positive count is how we
    // know the pool genuinely ran.
    auto uco_task_run_sum = (int32_t (*)(int32_t, int32_t))sym("uco_task_run_sum");
    check("task_run", uco_task_run_sum(40, 2), 42);
    auto quiesce = (int32_t (*)(int32_t))sym("dn2cpp_runtime_quiesce");
    int32_t quiesced = quiesce(5000);
    if (quiesced <= 0)
    {
        std::fprintf(stderr, "FAIL quiesce: got %d, want > 0\n", (int)quiesced);
        std::exit(1);
    }
    std::puts("quiesce OK");
    if (dlclose(g_lib) != 0)
    {
        std::fprintf(stderr, "dlclose: %s\n", dlerror());
        std::exit(1);
    }
    std::puts("dlclose OK");

    std::puts("driver OK");
    return 0;
}
