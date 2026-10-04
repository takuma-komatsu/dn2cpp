#include "dn2cpp_core.h"
#include <cstdio>
#include <fcntl.h>
#include <unistd.h>

const Dn2CppMetadataBlock dn2cpp_metadata_blocks[] = { {} };
const std::size_t dn2cpp_metadata_block_count = 0;
// This native probe supplies the empty generated-image tables required by the runtime.
const Dn2CppTypeRegEntry dn2cpp_type_registry[] = { {} };
const int32_t dn2cpp_type_registry_count = 0;
const Dn2CppTypeBind dn2cpp_type_binds[] = { {} };
const int32_t dn2cpp_type_bind_count = 0;
const Dn2CppAssemblyRegEntry dn2cpp_assembly_registry[] = { {} };
const int32_t dn2cpp_assembly_registry_count = 0;
const Dn2CppDelegateReflEntry dn2cpp_delegate_refl_registry[] = { {} };
const int32_t dn2cpp_delegate_refl_registry_count = 0;
const Dn2CppGvmRowDispatch dn2cpp_gvm_row_dispatch[] = { {} };
const int32_t dn2cpp_gvm_row_dispatch_count = 0;
const Dn2CppItfImplSlots dn2cpp_itf_impl_slots[] = { {} };
const int32_t dn2cpp_itf_impl_slot_count = 0;
const Dn2CppRenamedSlotBody dn2cpp_renamed_slot_bodies[] = { {} };
const int32_t dn2cpp_renamed_slot_body_count = 0;
const Dn2CppBclMessage dn2cpp_bcl_messages[] = { { nullptr, nullptr } };
const int32_t dn2cpp_bcl_message_count = 0;
const int32_t dn2cpp_exception_get_message_slot = -1;
bool dn2cpp_argument_exception_store(Dn2CppObject*, Dn2CppString*, Dn2CppObject*) { return false; }
bool dn2cpp_object_disposed_exception_store(Dn2CppObject*, Dn2CppString*) { return false; }
const Dn2CppRuntimeTemplate* const dn2cpp_runtime_templates = nullptr;
const int32_t dn2cpp_runtime_template_count = 0;

int main()
{
    for (int stream = 0; stream < 3; stream++)
    {
        int handle = static_cast<int>(dn2cpp_console_stream_open(stream));
        int flags = fcntl(handle, F_GETFD);
        if (handle == stream || flags < 0 || (flags & FD_CLOEXEC) == 0)
        {
            std::fputs("console handles must be independent and close on exec\n", stderr);
            return 1;
        }
        close(handle);
        if (fcntl(stream, F_GETFD) < 0)
        {
            std::fputs("disposing a wrapper must preserve the standard handle\n", stderr);
            return 1;
        }
    }
    std::puts("standard handle lifetime end");
    dn2cpp_main_exit(0);
    return 0;
}
