// dn2cpp_hash_cores.h — portable MD5 / SHA-1 / SHA-2 + HMAC cores (FIPS 180-4 /
// RFC 1321 / RFC 2104) shared by the crypto PAL translation units. The two
// flavors of System.Security.Cryptography.dll bottom out in different native
// modules but need identical algorithm math, so the math lives here once and each
// PAL TU adds only its thin extern "C" surface over the unified DigestCtx/HmacCtx:
//   dn2cpp_apple_crypto_digest.cpp   -> AppleCryptoNative_* (DigestFinal re-inits)
//   dn2cpp_openssl_crypto_digest.cpp -> CryptoNative_*      (Reset is a separate call)
//
// Everything here has internal linkage (anonymous namespace), so each including TU
// gets a private copy and only the per-TU PAL symbols are exported. The cores link
// no CommonCrypto / OpenSSL, keeping a transpiled binary self-contained.
//
// PAL_HashAlgorithm: Unknown=0, Md5=1, Sha1=2, Sha256=3, Sha384=4, Sha512=5.

#pragma once

#include <cstdint>
#include <cstdlib>
#include <cstring>

namespace
{

// ---- bit helpers ---------------------------------------------------------

inline uint32_t rotl32(uint32_t x, int n) { return (x << n) | (x >> (32 - n)); }
inline uint32_t rotr32(uint32_t x, int n) { return (x >> n) | (x << (32 - n)); }
inline uint64_t rotr64(uint64_t x, int n) { return (x >> n) | (x << (64 - n)); }

inline uint32_t load_le32(const uint8_t* p)
{
    return (uint32_t)p[0] | ((uint32_t)p[1] << 8) | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}
inline uint32_t load_be32(const uint8_t* p)
{
    return ((uint32_t)p[0] << 24) | ((uint32_t)p[1] << 16) | ((uint32_t)p[2] << 8) | (uint32_t)p[3];
}
inline uint64_t load_be64(const uint8_t* p)
{
    return ((uint64_t)load_be32(p) << 32) | load_be32(p + 4);
}
inline void store_le32(uint8_t* p, uint32_t v)
{
    p[0] = (uint8_t)v; p[1] = (uint8_t)(v >> 8); p[2] = (uint8_t)(v >> 16); p[3] = (uint8_t)(v >> 24);
}
inline void store_be32(uint8_t* p, uint32_t v)
{
    p[0] = (uint8_t)(v >> 24); p[1] = (uint8_t)(v >> 16); p[2] = (uint8_t)(v >> 8); p[3] = (uint8_t)v;
}
inline void store_be64(uint8_t* p, uint64_t v)
{
    store_be32(p, (uint32_t)(v >> 32)); store_be32(p + 4, (uint32_t)v);
}

// ---- MD5 (RFC 1321) ------------------------------------------------------

struct Md5Ctx
{
    uint32_t h[4];
    uint64_t len;     // total bytes absorbed
    uint8_t buf[64];  // partial block
};

const uint32_t kMd5K[64] = {
    0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee, 0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
    0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be, 0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
    0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa, 0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
    0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed, 0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
    0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c, 0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
    0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05, 0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
    0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039, 0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
    0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1, 0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391,
};
const uint8_t kMd5S[64] = {
    7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
    5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20, 5,  9, 14, 20,
    4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
    6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21,
};

inline void md5_init(Md5Ctx& c)
{
    c.h[0] = 0x67452301; c.h[1] = 0xefcdab89; c.h[2] = 0x98badcfe; c.h[3] = 0x10325476;
    c.len = 0;
}

inline void md5_block(Md5Ctx& c, const uint8_t* p)
{
    uint32_t m[16];
    for (int i = 0; i < 16; i++)
        m[i] = load_le32(p + i * 4);
    uint32_t a = c.h[0], b = c.h[1], d0 = c.h[2], d = c.h[3];
    for (int i = 0; i < 64; i++)
    {
        uint32_t f; int g;
        if (i < 16)      { f = (b & d0) | (~b & d);      g = i; }
        else if (i < 32) { f = (d & b) | (~d & d0);      g = (5 * i + 1) & 15; }
        else if (i < 48) { f = b ^ d0 ^ d;               g = (3 * i + 5) & 15; }
        else             { f = d0 ^ (b | ~d);            g = (7 * i) & 15; }
        uint32_t tmp = d;
        d = d0; d0 = b;
        b = b + rotl32(a + f + kMd5K[i] + m[g], kMd5S[i]);
        a = tmp;
    }
    c.h[0] += a; c.h[1] += b; c.h[2] += d0; c.h[3] += d;
}

inline void md5_update(Md5Ctx& c, const uint8_t* data, size_t n)
{
    size_t fill = (size_t)(c.len & 63);
    c.len += n;
    if (fill != 0)
    {
        size_t take = 64 - fill; if (take > n) take = n;
        std::memcpy(c.buf + fill, data, take);
        data += take; n -= take;
        if (fill + take == 64)
            md5_block(c, c.buf);
        else
            return;
    }
    while (n >= 64) { md5_block(c, data); data += 64; n -= 64; }
    if (n != 0) std::memcpy(c.buf, data, n);
}

inline void md5_final(Md5Ctx& c, uint8_t* out) // 16 bytes; ctx NOT reusable until re-init
{
    uint64_t bitLen = c.len * 8;
    uint8_t pad = 0x80;
    md5_update(c, &pad, 1);
    uint8_t zero = 0;
    while ((c.len & 63) != 56) md5_update(c, &zero, 1);
    uint8_t lenBuf[8];
    store_le32(lenBuf, (uint32_t)bitLen); store_le32(lenBuf + 4, (uint32_t)(bitLen >> 32));
    md5_update(c, lenBuf, 8);
    for (int i = 0; i < 4; i++) store_le32(out + i * 4, c.h[i]);
}

// ---- SHA-1 (FIPS 180-4) --------------------------------------------------

struct Sha1Ctx
{
    uint32_t h[5];
    uint64_t len;
    uint8_t buf[64];
};

inline void sha1_init(Sha1Ctx& c)
{
    c.h[0] = 0x67452301; c.h[1] = 0xefcdab89; c.h[2] = 0x98badcfe; c.h[3] = 0x10325476; c.h[4] = 0xc3d2e1f0;
    c.len = 0;
}

inline void sha1_block(Sha1Ctx& c, const uint8_t* p)
{
    uint32_t w[80];
    for (int i = 0; i < 16; i++) w[i] = load_be32(p + i * 4);
    for (int i = 16; i < 80; i++) w[i] = rotl32(w[i - 3] ^ w[i - 8] ^ w[i - 14] ^ w[i - 16], 1);
    uint32_t a = c.h[0], b = c.h[1], d0 = c.h[2], d = c.h[3], e = c.h[4];
    for (int i = 0; i < 80; i++)
    {
        uint32_t f, k;
        if (i < 20)      { f = (b & d0) | (~b & d);           k = 0x5a827999; }
        else if (i < 40) { f = b ^ d0 ^ d;                    k = 0x6ed9eba1; }
        else if (i < 60) { f = (b & d0) | (b & d) | (d0 & d); k = 0x8f1bbcdc; }
        else             { f = b ^ d0 ^ d;                    k = 0xca62c1d6; }
        uint32_t tmp = rotl32(a, 5) + f + e + k + w[i];
        e = d; d = d0; d0 = rotl32(b, 30); b = a; a = tmp;
    }
    c.h[0] += a; c.h[1] += b; c.h[2] += d0; c.h[3] += d; c.h[4] += e;
}

inline void sha1_update(Sha1Ctx& c, const uint8_t* data, size_t n)
{
    size_t fill = (size_t)(c.len & 63);
    c.len += n;
    if (fill != 0)
    {
        size_t take = 64 - fill; if (take > n) take = n;
        std::memcpy(c.buf + fill, data, take);
        data += take; n -= take;
        if (fill + take == 64)
            sha1_block(c, c.buf);
        else
            return;
    }
    while (n >= 64) { sha1_block(c, data); data += 64; n -= 64; }
    if (n != 0) std::memcpy(c.buf, data, n);
}

inline void sha1_final(Sha1Ctx& c, uint8_t* out) // 20 bytes
{
    uint64_t bitLen = c.len * 8;
    uint8_t pad = 0x80;
    sha1_update(c, &pad, 1);
    uint8_t zero = 0;
    while ((c.len & 63) != 56) sha1_update(c, &zero, 1);
    uint8_t lenBuf[8];
    store_be64(lenBuf, bitLen);
    sha1_update(c, lenBuf, 8);
    for (int i = 0; i < 5; i++) store_be32(out + i * 4, c.h[i]);
}

// ---- SHA-256 (FIPS 180-4) ------------------------------------------------

struct Sha256Ctx
{
    uint32_t h[8];
    uint64_t len;
    uint8_t buf[64];
};

const uint32_t kSha256K[64] = {
    0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
    0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
    0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
    0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
    0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
    0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
    0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
    0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
};

inline void sha256_init(Sha256Ctx& c)
{
    c.h[0] = 0x6a09e667; c.h[1] = 0xbb67ae85; c.h[2] = 0x3c6ef372; c.h[3] = 0xa54ff53a;
    c.h[4] = 0x510e527f; c.h[5] = 0x9b05688c; c.h[6] = 0x1f83d9ab; c.h[7] = 0x5be0cd19;
    c.len = 0;
}

inline void sha256_block(Sha256Ctx& c, const uint8_t* p)
{
    uint32_t w[64];
    for (int i = 0; i < 16; i++) w[i] = load_be32(p + i * 4);
    for (int i = 16; i < 64; i++)
    {
        uint32_t s0 = rotr32(w[i - 15], 7) ^ rotr32(w[i - 15], 18) ^ (w[i - 15] >> 3);
        uint32_t s1 = rotr32(w[i - 2], 17) ^ rotr32(w[i - 2], 19) ^ (w[i - 2] >> 10);
        w[i] = w[i - 16] + s0 + w[i - 7] + s1;
    }
    uint32_t a = c.h[0], b = c.h[1], cc = c.h[2], d = c.h[3], e = c.h[4], f = c.h[5], g = c.h[6], h = c.h[7];
    for (int i = 0; i < 64; i++)
    {
        uint32_t s1 = rotr32(e, 6) ^ rotr32(e, 11) ^ rotr32(e, 25);
        uint32_t ch = (e & f) ^ (~e & g);
        uint32_t t1 = h + s1 + ch + kSha256K[i] + w[i];
        uint32_t s0 = rotr32(a, 2) ^ rotr32(a, 13) ^ rotr32(a, 22);
        uint32_t maj = (a & b) ^ (a & cc) ^ (b & cc);
        uint32_t t2 = s0 + maj;
        h = g; g = f; f = e; e = d + t1; d = cc; cc = b; b = a; a = t1 + t2;
    }
    c.h[0] += a; c.h[1] += b; c.h[2] += cc; c.h[3] += d; c.h[4] += e; c.h[5] += f; c.h[6] += g; c.h[7] += h;
}

inline void sha256_update(Sha256Ctx& c, const uint8_t* data, size_t n)
{
    size_t fill = (size_t)(c.len & 63);
    c.len += n;
    if (fill != 0)
    {
        size_t take = 64 - fill; if (take > n) take = n;
        std::memcpy(c.buf + fill, data, take);
        data += take; n -= take;
        if (fill + take == 64)
            sha256_block(c, c.buf);
        else
            return;
    }
    while (n >= 64) { sha256_block(c, data); data += 64; n -= 64; }
    if (n != 0) std::memcpy(c.buf, data, n);
}

inline void sha256_final(Sha256Ctx& c, uint8_t* out) // 32 bytes
{
    uint64_t bitLen = c.len * 8;
    uint8_t pad = 0x80;
    sha256_update(c, &pad, 1);
    uint8_t zero = 0;
    while ((c.len & 63) != 56) sha256_update(c, &zero, 1);
    uint8_t lenBuf[8];
    store_be64(lenBuf, bitLen);
    sha256_update(c, lenBuf, 8);
    for (int i = 0; i < 8; i++) store_be32(out + i * 4, c.h[i]);
}

// ---- SHA-512 / SHA-384 (FIPS 180-4; 384 = different IV, truncated out) ----

struct Sha512Ctx
{
    uint64_t h[8];
    uint64_t len;      // total bytes (< 2^61 — plenty; the 128-bit bit-length
                       // field is encoded from this single counter)
    uint8_t buf[128];
};

const uint64_t kSha512K[80] = {
    0x428a2f98d728ae22ULL, 0x7137449123ef65cdULL, 0xb5c0fbcfec4d3b2fULL, 0xe9b5dba58189dbbcULL,
    0x3956c25bf348b538ULL, 0x59f111f1b605d019ULL, 0x923f82a4af194f9bULL, 0xab1c5ed5da6d8118ULL,
    0xd807aa98a3030242ULL, 0x12835b0145706fbeULL, 0x243185be4ee4b28cULL, 0x550c7dc3d5ffb4e2ULL,
    0x72be5d74f27b896fULL, 0x80deb1fe3b1696b1ULL, 0x9bdc06a725c71235ULL, 0xc19bf174cf692694ULL,
    0xe49b69c19ef14ad2ULL, 0xefbe4786384f25e3ULL, 0x0fc19dc68b8cd5b5ULL, 0x240ca1cc77ac9c65ULL,
    0x2de92c6f592b0275ULL, 0x4a7484aa6ea6e483ULL, 0x5cb0a9dcbd41fbd4ULL, 0x76f988da831153b5ULL,
    0x983e5152ee66dfabULL, 0xa831c66d2db43210ULL, 0xb00327c898fb213fULL, 0xbf597fc7beef0ee4ULL,
    0xc6e00bf33da88fc2ULL, 0xd5a79147930aa725ULL, 0x06ca6351e003826fULL, 0x142929670a0e6e70ULL,
    0x27b70a8546d22ffcULL, 0x2e1b21385c26c926ULL, 0x4d2c6dfc5ac42aedULL, 0x53380d139d95b3dfULL,
    0x650a73548baf63deULL, 0x766a0abb3c77b2a8ULL, 0x81c2c92e47edaee6ULL, 0x92722c851482353bULL,
    0xa2bfe8a14cf10364ULL, 0xa81a664bbc423001ULL, 0xc24b8b70d0f89791ULL, 0xc76c51a30654be30ULL,
    0xd192e819d6ef5218ULL, 0xd69906245565a910ULL, 0xf40e35855771202aULL, 0x106aa07032bbd1b8ULL,
    0x19a4c116b8d2d0c8ULL, 0x1e376c085141ab53ULL, 0x2748774cdf8eeb99ULL, 0x34b0bcb5e19b48a8ULL,
    0x391c0cb3c5c95a63ULL, 0x4ed8aa4ae3418acbULL, 0x5b9cca4f7763e373ULL, 0x682e6ff3d6b2b8a3ULL,
    0x748f82ee5defb2fcULL, 0x78a5636f43172f60ULL, 0x84c87814a1f0ab72ULL, 0x8cc702081a6439ecULL,
    0x90befffa23631e28ULL, 0xa4506cebde82bde9ULL, 0xbef9a3f7b2c67915ULL, 0xc67178f2e372532bULL,
    0xca273eceea26619cULL, 0xd186b8c721c0c207ULL, 0xeada7dd6cde0eb1eULL, 0xf57d4f7fee6ed178ULL,
    0x06f067aa72176fbaULL, 0x0a637dc5a2c898a6ULL, 0x113f9804bef90daeULL, 0x1b710b35131c471bULL,
    0x28db77f523047d84ULL, 0x32caab7b40c72493ULL, 0x3c9ebe0a15c9bebcULL, 0x431d67c49c100d4cULL,
    0x4cc5d4becb3e42b6ULL, 0x597f299cfc657e2aULL, 0x5fcb6fab3ad6faecULL, 0x6c44198c4a475817ULL,
};

inline void sha512_init(Sha512Ctx& c, bool is384)
{
    if (is384)
    {
        c.h[0] = 0xcbbb9d5dc1059ed8ULL; c.h[1] = 0x629a292a367cd507ULL;
        c.h[2] = 0x9159015a3070dd17ULL; c.h[3] = 0x152fecd8f70e5939ULL;
        c.h[4] = 0x67332667ffc00b31ULL; c.h[5] = 0x8eb44a8768581511ULL;
        c.h[6] = 0xdb0c2e0d64f98fa7ULL; c.h[7] = 0x47b5481dbefa4fa4ULL;
    }
    else
    {
        c.h[0] = 0x6a09e667f3bcc908ULL; c.h[1] = 0xbb67ae8584caa73bULL;
        c.h[2] = 0x3c6ef372fe94f82bULL; c.h[3] = 0xa54ff53a5f1d36f1ULL;
        c.h[4] = 0x510e527fade682d1ULL; c.h[5] = 0x9b05688c2b3e6c1fULL;
        c.h[6] = 0x1f83d9abfb41bd6bULL; c.h[7] = 0x5be0cd19137e2179ULL;
    }
    c.len = 0;
}

inline void sha512_block(Sha512Ctx& c, const uint8_t* p)
{
    uint64_t w[80];
    for (int i = 0; i < 16; i++) w[i] = load_be64(p + i * 8);
    for (int i = 16; i < 80; i++)
    {
        uint64_t s0 = rotr64(w[i - 15], 1) ^ rotr64(w[i - 15], 8) ^ (w[i - 15] >> 7);
        uint64_t s1 = rotr64(w[i - 2], 19) ^ rotr64(w[i - 2], 61) ^ (w[i - 2] >> 6);
        w[i] = w[i - 16] + s0 + w[i - 7] + s1;
    }
    uint64_t a = c.h[0], b = c.h[1], cc = c.h[2], d = c.h[3], e = c.h[4], f = c.h[5], g = c.h[6], h = c.h[7];
    for (int i = 0; i < 80; i++)
    {
        uint64_t s1 = rotr64(e, 14) ^ rotr64(e, 18) ^ rotr64(e, 41);
        uint64_t ch = (e & f) ^ (~e & g);
        uint64_t t1 = h + s1 + ch + kSha512K[i] + w[i];
        uint64_t s0 = rotr64(a, 28) ^ rotr64(a, 34) ^ rotr64(a, 39);
        uint64_t maj = (a & b) ^ (a & cc) ^ (b & cc);
        uint64_t t2 = s0 + maj;
        h = g; g = f; f = e; e = d + t1; d = cc; cc = b; b = a; a = t1 + t2;
    }
    c.h[0] += a; c.h[1] += b; c.h[2] += cc; c.h[3] += d; c.h[4] += e; c.h[5] += f; c.h[6] += g; c.h[7] += h;
}

inline void sha512_update(Sha512Ctx& c, const uint8_t* data, size_t n)
{
    size_t fill = (size_t)(c.len & 127);
    c.len += n;
    if (fill != 0)
    {
        size_t take = 128 - fill; if (take > n) take = n;
        std::memcpy(c.buf + fill, data, take);
        data += take; n -= take;
        if (fill + take == 128)
            sha512_block(c, c.buf);
        else
            return;
    }
    while (n >= 128) { sha512_block(c, data); data += 128; n -= 128; }
    if (n != 0) std::memcpy(c.buf, data, n);
}

inline void sha512_final(Sha512Ctx& c, uint8_t* out, int outWords) // 6 words = SHA-384, 8 = SHA-512
{
    uint64_t bitLenLo = c.len << 3, bitLenHi = c.len >> 61;
    uint8_t pad = 0x80;
    sha512_update(c, &pad, 1);
    uint8_t zero = 0;
    while ((c.len & 127) != 112) sha512_update(c, &zero, 1);
    uint8_t lenBuf[16];
    store_be64(lenBuf, bitLenHi); store_be64(lenBuf + 8, bitLenLo);
    sha512_update(c, lenBuf, 16);
    for (int i = 0; i < outWords; i++) store_be64(out + i * 8, c.h[i]);
}

// ---- unified digest context (the PAL handle) ------------------------------

enum PalHashAlgorithm : int32_t
{
    PAL_Unknown = 0, PAL_Md5 = 1, PAL_Sha1 = 2, PAL_Sha256 = 3, PAL_Sha384 = 4, PAL_Sha512 = 5,
};

inline int32_t digest_size(int32_t alg)
{
    switch (alg)
    {
        case PAL_Md5: return 16;
        case PAL_Sha1: return 20;
        case PAL_Sha256: return 32;
        case PAL_Sha384: return 48;
        case PAL_Sha512: return 64;
        default: return -1;
    }
}

inline int32_t block_size(int32_t alg) // HMAC block length
{
    return (alg == PAL_Sha384 || alg == PAL_Sha512) ? 128 : 64;
}

// Plain POD: Clone is a struct copy, Free a plain free(). The managed side
// holds the pointer in a SafeHandle and never inspects it.
struct DigestCtx
{
    int32_t alg;
    union
    {
        Md5Ctx md5;
        Sha1Ctx sha1;
        Sha256Ctx sha256;
        Sha512Ctx sha512;
    } u;
};

inline void digest_init(DigestCtx& d)
{
    switch (d.alg)
    {
        case PAL_Md5: md5_init(d.u.md5); break;
        case PAL_Sha1: sha1_init(d.u.sha1); break;
        case PAL_Sha256: sha256_init(d.u.sha256); break;
        case PAL_Sha384: sha512_init(d.u.sha512, true); break;
        default: sha512_init(d.u.sha512, false); break;
    }
}

inline void digest_update(DigestCtx& d, const uint8_t* data, size_t n)
{
    switch (d.alg)
    {
        case PAL_Md5: md5_update(d.u.md5, data, n); break;
        case PAL_Sha1: sha1_update(d.u.sha1, data, n); break;
        case PAL_Sha256: sha256_update(d.u.sha256, data, n); break;
        default: sha512_update(d.u.sha512, data, n); break;
    }
}

inline void digest_final(DigestCtx& d, uint8_t* out) // consumes the state; caller re-inits if reusing
{
    switch (d.alg)
    {
        case PAL_Md5: md5_final(d.u.md5, out); break;
        case PAL_Sha1: sha1_final(d.u.sha1, out); break;
        case PAL_Sha256: sha256_final(d.u.sha256, out); break;
        case PAL_Sha384: sha512_final(d.u.sha512, out, 6); break;
        default: sha512_final(d.u.sha512, out, 8); break;
    }
}

// ---- HMAC (RFC 2104) over the unified digest ------------------------------

struct HmacCtx
{
    int32_t alg;
    DigestCtx inner;       // running H(K ^ ipad || message...)
    DigestCtx outerPrimed; // H(K ^ opad) absorbed, ready to take the inner digest
};

// (Re)key the context: derive K0 (hash-then-zero-pad an overlong key), prime
// both the inner and the saved outer state. Works on a used context —
// LiteHmac.Reset calls HmacInit again after HmacFinal.
inline void hmac_init(HmacCtx& h, const uint8_t* key, int32_t keyLen)
{
    uint8_t k0[128];
    int32_t bs = block_size(h.alg);
    std::memset(k0, 0, (size_t)bs);
    if (keyLen > bs)
    {
        DigestCtx kd; kd.alg = h.alg;
        digest_init(kd);
        digest_update(kd, key, (size_t)keyLen);
        digest_final(kd, k0);
    }
    else if (keyLen > 0)
        std::memcpy(k0, key, (size_t)keyLen);
    uint8_t pad[128];
    h.inner.alg = h.alg;
    digest_init(h.inner);
    for (int32_t i = 0; i < bs; i++) pad[i] = (uint8_t)(k0[i] ^ 0x36);
    digest_update(h.inner, pad, (size_t)bs);
    h.outerPrimed.alg = h.alg;
    digest_init(h.outerPrimed);
    for (int32_t i = 0; i < bs; i++) pad[i] = (uint8_t)(k0[i] ^ 0x5c);
    digest_update(h.outerPrimed, pad, (size_t)bs);
}

inline void hmac_final(const HmacCtx& h, uint8_t* out) // non-destructive (works on copies)
{
    uint8_t innerDigest[64];
    DigestCtx tmp = h.inner;
    digest_final(tmp, innerDigest);
    DigestCtx outer = h.outerPrimed;
    digest_update(outer, innerDigest, (size_t)digest_size(h.alg));
    digest_final(outer, out);
}

} // namespace

