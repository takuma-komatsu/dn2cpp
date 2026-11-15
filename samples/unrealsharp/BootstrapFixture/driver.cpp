#include "dn2cpp_unrealsharp_abi.h"
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <vector>
#include <thread>
#include <dlfcn.h>

extern "C" int unrealsharp_fixture_utf8(const char*, const char*);
extern "C" int unrealsharp_fixture_delegates(int (*)(int), int (*)(int), int);
extern "C" bool unrealsharp_fixture_bool(bool);

namespace
{
std::vector<int> events;
int failure_event;
void** reverse_callbacks;
void** managed_reverse_callbacks;
void require(bool condition, const char* message);
int callback(int event)
{
    events.push_back(event);
    if (event == -failure_event)
    {
        auto reverse = reinterpret_cast<void (*)()>(reverse_callbacks[0]);
        if (event == 50)
        {
            int status = 0;
            auto invoke = reinterpret_cast<int (*)(intptr_t, intptr_t, intptr_t, intptr_t, intptr_t)>(managed_reverse_callbacks[1]);
            std::thread worker([&] { status = invoke(0, 0, 0, 0, 0); });
            worker.join();
            require(status == 1, "managed invocation callback failure sentinel");
        }
        else reverse();
    }
    return event == 50 && failure_event == -51 ? 2 : event == failure_event ? 1 : 0;
}

void require(bool condition, const char* message)
{
    if (!condition)
    {
        std::fprintf(stderr, "FAIL: %s\n", message);
        std::exit(1);
    }
}

template<typename T> T symbol(void* library, const char* name)
{
    auto address = dlsym(library, name);
    require(address != nullptr, name);
    return reinterpret_cast<T>(address);
}
}

int main(int argc, char** argv)
{
    require(argc == 3, "library and scenario required");
    void* library = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (!library) std::fprintf(stderr, "%s\n", dlerror());
    require(library != nullptr, "load fixture library");
    auto initialize = symbol<int32_t (*)(const Dn2CppUnrealSharpHost*, Dn2CppUnrealSharpResult*)>(library, "dn2cpp_unrealsharp_initialize");
    auto register_assembly = symbol<int32_t (*)(const char*, Dn2CppUnrealSharpResult*)>(library, "dn2cpp_unrealsharp_register_assembly");
    auto tick = symbol<int32_t (*)(float, Dn2CppUnrealSharpResult*)>(library, "dn2cpp_unrealsharp_tick");
    auto shutdown = symbol<int32_t (*)(Dn2CppUnrealSharpResult*)>(library, "dn2cpp_unrealsharp_shutdown");
    Dn2CppUnrealSharpResult result{};
    void* plugin_callbacks[2]{};
    plugin_callbacks[1] = reinterpret_cast<void*>(&unrealsharp_fixture_utf8);
    void* managed_callbacks[9]{};
    managed_callbacks[7] = reinterpret_cast<void*>(&unrealsharp_fixture_bool);
    managed_callbacks[8] = reinterpret_cast<void*>(&unrealsharp_fixture_delegates);
    reverse_callbacks = plugin_callbacks;
    managed_reverse_callbacks = managed_callbacks;
    Dn2CppUnrealSharpHost host{};
    host.abi_version = 1;
    host.struct_size = sizeof(host);
    host.ue_major = 5;
    host.ue_minor = 8;
    host.ue_patch = 3;
    host.unrealsharp_revision = "b78e073ab4e81e6eae3c57ba1f5ecf5f29eef1f4";
    host.working_directory = ".";
    host.plugin_callbacks = plugin_callbacks;
    host.binds_callbacks = reinterpret_cast<void*>(&callback);
    host.managed_callbacks = managed_callbacks;
    host.plugin_callbacks_size = sizeof(plugin_callbacks);
    host.managed_callbacks_size = sizeof(managed_callbacks);
    require(initialize(nullptr, &result) == 0 && result.error[0], "null host rejected");
    require(initialize(&host, nullptr) == 0, "null result rejected before initialization");
    uint32_t short_host[2] = {DN2CPP_UNREALSHARP_ABI_VERSION, sizeof(short_host)};
    require(initialize(reinterpret_cast<const Dn2CppUnrealSharpHost*>(short_host), &result) == 0,
        "short host rejected before reading later fields");
    const char* scenario = argv[2];
    if (!std::strcmp(scenario, "version")) host.abi_version++;
    if (!std::strcmp(scenario, "size")) host.struct_size--;
    if (!std::strcmp(scenario, "ue-version")) host.ue_patch++;
    if (!std::strcmp(scenario, "revision")) host.unrealsharp_revision = "incompatible";
    if (!std::strcmp(scenario, "callbacks")) host.managed_callbacks_size--;
    if (!std::strcmp(scenario, "bootstrap-failure")) failure_event = 10;
    if (!std::strcmp(scenario, "initializer-failure")) failure_event = 20;
    if (!std::strcmp(scenario, "tick-failure")) failure_event = 50;
    if (!std::strcmp(scenario, "delegate-callback-failure")) failure_event = -51;
    if (!std::strcmp(scenario, "shutdown-failure")) failure_event = 60;
    if (!std::strcmp(scenario, "bootstrap-callback-failure")) failure_event = -10;
    if (!std::strcmp(scenario, "worker-callback-failure")) failure_event = -50;
    int status = initialize(&host, &result);
    if (!std::strcmp(scenario, "version") || !std::strcmp(scenario, "size") ||
        !std::strcmp(scenario, "revision") || !std::strcmp(scenario, "callbacks") || !std::strcmp(scenario, "ue-version") ||
        !std::strcmp(scenario, "bootstrap-failure") || !std::strcmp(scenario, "bootstrap-callback-failure"))
    {
        require(status == 0 && result.success == 0 && result.error[0], "initialization failure diagnostic");
        require(events.size() == (std::abs(failure_event) == 10 ? 1u : 0u), "invalid ABI must not enter managed code");
        if (std::abs(failure_event) == 10)
        {
            require(shutdown(&result) == 1, "failed initialization can release partial state");
            require(events == std::vector<int>({10,60,70}), "failed bootstrap shutdown callback");
            require(shutdown(&result) == 0, "shutdown is one-shot after bootstrap failure");
        }
    }
    else
    {
        require(status == 1 && result.success == 1, result.error);
        require(events == std::vector<int>{10}, "initialize must defer registration");
        require(initialize(&host, &result) == 0, "duplicate initialization rejected");
        require(register_assembly("UnknownAssembly", &result) == 0, "unknown assembly rejected");
        require(register_assembly(nullptr, &result) == 0, "null assembly rejected");
        require(register_assembly("UnrealSharp.Plugins", nullptr) == 0, "null registration result rejected");
        require(tick(0.25f, &result) == 0, "tick before registration rejected");
        if (!std::strcmp(scenario, "order"))
        {
            require(register_assembly("RegistrationFixture", &result) == 0 && result.error[0], "out-of-order registration rejected");
            require(events == std::vector<int>{10}, "out-of-order registration must not execute");
        }
        else
        {
            require(register_assembly("UnrealSharp.Plugins", &result) == 1, result.error);
            require(register_assembly("UnrealSharp.Plugins", &result) == 0, "duplicate registration rejected");
            int registration_status = register_assembly("RegistrationFixture", &result);
            if (failure_event == 20)
            {
                require(registration_status == 0 && result.error[0], "initializer failure diagnostic");
                require(events == std::vector<int>({10,30,31,40,20}), "failed initializer must not complete registration");
                require(tick(0.25f, &result) == 0, "failed registration never becomes ready");
                require(shutdown(&result) == 1, "failed registration cleanup");
                require(events == std::vector<int>({10,30,31,40,20,60,70}), "failed registration shutdown callback");
            }
            else
            {
                require(registration_status == 1, result.error);
                require(events == std::vector<int>({10,30,31,40,20,41}), "registration and initializer order");
                int foreign_status = -1;
                std::thread worker([&] { Dn2CppUnrealSharpResult foreign_result{}; foreign_status = tick(0.25f, &foreign_result); });
                worker.join();
                require(foreign_status == 0, "tick requires the initializing thread");
                require(tick(0.25f, nullptr) == 0, "null tick result rejected");
                require(shutdown(nullptr) == 0, "null shutdown result rejected");
                require((tick(0.25f, &result) == 0) == (std::abs(failure_event) == 50 || failure_event == -51), "tick result");
                require((shutdown(&result) == 0) == (failure_event == 60), "shutdown result");
                require(events == (failure_event == 60 ? std::vector<int>({10,30,31,40,20,41,50,60})
                    : std::vector<int>({10,30,31,40,20,41,50,60,70})), "callback lifecycle and deferred handle release");
                require(tick(0.25f, &result) == 0, "tick after shutdown rejected");
                require(shutdown(&result) == 0, "shutdown cannot repeat even after failure");
                require(initialize(&host, &result) == 0, "runtime cannot reload");
            }
        }
    }
    std::printf("unrealsharp-bootstrap %s OK\n", scenario);
    return 0;
}
