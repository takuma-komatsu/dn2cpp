/* A stand-in for a game's OWN native GDExtension: it registers no class and calls
 * nothing back, so the only thing under test is that the engine can FIND and LOAD
 * it alongside the .NET module. Built against the vendored
 * third_party/gdextension_interface.h, so it needs no godot-cpp.
 *
 * The marker prints at SCENE level, not at entry: the entry runs during library
 * open, so only the later point distinguishes "opened" from "live". */
#include <stdio.h>

#include "gdextension_interface.h"

#if defined(_WIN32)
#define PROBE_EXPORT __declspec(dllexport)
#else
#define PROBE_EXPORT __attribute__((visibility("default")))
#endif

static void probe_initialize(void *p_userdata, GDExtensionInitializationLevel p_level) {
    (void)p_userdata;
    if (p_level == GDEXTENSION_INITIALIZATION_SCENE) {
        printf("GAMEEXT_PROBE_EXTENSION_INIT\n");
        fflush(stdout);
    }
}

static void probe_deinitialize(void *p_userdata, GDExtensionInitializationLevel p_level) {
    (void)p_userdata;
    (void)p_level;
}

PROBE_EXPORT GDExtensionBool gameextprobe_init(
        GDExtensionInterfaceGetProcAddress p_get_proc_address,
        GDExtensionClassLibraryPtr p_library,
        GDExtensionInitialization *r_initialization) {
    (void)p_get_proc_address;
    (void)p_library;
    r_initialization->minimum_initialization_level = GDEXTENSION_INITIALIZATION_SCENE;
    r_initialization->userdata = NULL;
    r_initialization->initialize = probe_initialize;
    r_initialization->deinitialize = probe_deinitialize;
    return 1;
}
