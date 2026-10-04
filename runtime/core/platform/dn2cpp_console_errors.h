#pragma once
#include "dn2cpp_core.h"
#include <cerrno>
#include <cstring>

// Console descriptors have no path; errno still determines the managed type and HResult.
[[noreturn]] inline void dn2cpp_console_throw_errno(int error)
{
    const char* text = (error == EAGAIN || error == EWOULDBLOCK)
        ? "The process cannot access the file because it is being used by another process."
        : std::strerror(error);
    auto* io = reinterpret_cast<Dn2CppExceptionObject*>(dn2cpp_exception_new(
        &dn2cpp_io_exception_type, dn2cpp_string_from_utf8(text, static_cast<int32_t>(std::strlen(text))), nullptr));
    io->hresult = error;
    if (error == EACCES || error == EBADF || error == EPERM)
        dn2cpp_throw_of_msg_inner(&dn2cpp_unauthorized_access_exception_type, "Access to the path is denied.", io);
    if (error == EFBIG)
        dn2cpp_throw_argument_text(&dn2cpp_argument_out_of_range_exception_type, "The file is too long. This operation is currently limited to supporting files less than 2 gigabytes in size.", "value");
    dn2cpp_throw(io);
}
