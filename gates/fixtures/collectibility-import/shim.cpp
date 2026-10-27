#include <cstddef>
#include <cstdint>

// Only the fixture links this symbol; no CoreCLR runtime is loaded or called.
struct QCallTypeHandleFixture
{
    void* objectSlot;
    intptr_t handle;
};
static_assert(offsetof(QCallTypeHandleFixture, handle) == sizeof(void*));
static_assert(sizeof(QCallTypeHandleFixture) == 2 * sizeof(void*));
extern "C" int32_t RuntimeTypeHandle_IsCollectible(QCallTypeHandleFixture value)
{
    return value.objectSlot == nullptr && value.handle == 29 ? 91 : -1;
}
