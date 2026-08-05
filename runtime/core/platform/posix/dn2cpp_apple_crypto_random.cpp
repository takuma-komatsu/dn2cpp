// dn2cpp_apple_crypto_random.cpp — AppleCryptoNative_GetRandomBytes, the
// entropy source behind System.Security.Cryptography.RandomNumberGenerator
// on the Apple flavor of the real BCL (which does NOT route through
// SystemNative_*). Backed by the same real-entropy backend Stage 1 added for
// Guid.NewGuid (pal_random.c mirror in dn2cpp_system_native.cpp).
//
// ABI (decoded from the real net10 System.Security.Cryptography.dll):
//   int32_t AppleCryptoNative_GetRandomBytes(uint8_t* buf, int32_t num,
//                                            int32_t* errorCode)
// returns 1 on success, 0 on failure with *errorCode set (the managed side
// throws CryptographicException naming the code).

#include <cstdint>

extern "C" int32_t SystemNative_GetCryptographicallySecureRandomBytes(uint8_t* buffer, int32_t bufferLength);

extern "C" int32_t AppleCryptoNative_GetRandomBytes(uint8_t* pBuf, int32_t cbBuf, int32_t* pkCCStatus)
{
    if (pBuf == nullptr || cbBuf < 0 || pkCCStatus == nullptr)
        return -1;
    *pkCCStatus = 0; // kCCSuccess
    if (SystemNative_GetCryptographicallySecureRandomBytes(pBuf, cbBuf) != 0)
    {
        *pkCCStatus = -1;
        return 0;
    }
    return 1;
}
