// Repo-local native library for the P/Invoke gates: a non-system shared library
// linked via the emitted `-ldn2cpptest` plus a gate-supplied `-L<dir>`
// (docs/PINVOKE-MARSHALLING.md). One section per marshalling feature.

#include <stddef.h>   // offsetof — the marshalled-layout reporter at the tail
#include <stdint.h>
#include <stdbool.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include <errno.h>
#ifdef _WIN32
#  define WIN32_LEAN_AND_MEAN
#  include <windows.h>  // SetLastError — the slot [DllImport(SetLastError=true)] reads here
#endif

int32_t dn2cpptest_add(int32_t a, int32_t b) { return a + b; }

int64_t dn2cpptest_mul(int64_t a, int64_t b) { return a * b; }

double dn2cpptest_scale(double x, int32_t n) { return x * (double)n; }

// Pointer argument: sum n int32s.
int32_t dn2cpptest_sum(const int32_t *p, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++)
        s += p[i];
    return s;
}

// Sub-word return (exercises the uint8_t native ABI width).
uint8_t dn2cpptest_lowbyte(uint64_t v) { return (uint8_t)(v & 0xFFu); }

// String argument (NUL-terminated UTF-8): byte length.
int32_t dn2cpptest_strlen(const char *s) { return (int32_t)strlen(s); }

// String return: a heap copy the caller's marshaller frees, as .NET does.
char *dn2cpptest_greeting(void) { return strdup("dn2cpp says hi"); }

// Blittable array arguments: .NET pins the array and passes the element pointer, so
// [In,Out] write-backs are visible. Null array -> null pointer, empty -> non-null.
// The element count is a separate `n` argument.
int32_t dn2cpptest_iarr_sum(const int32_t *a, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++) s += a[i];
    return s;
}

int64_t dn2cpptest_larr_sum(const int64_t *a, int32_t n)
{
    int64_t s = 0;
    for (int32_t i = 0; i < n; i++) s += a[i];
    return s;
}

double dn2cpptest_darr_sum(const double *a, int32_t n)
{
    double s = 0.0;
    for (int32_t i = 0; i < n; i++) s += a[i];
    return s;
}

// Sub-word element (byte[] -> packed uint8_t buffer).
int32_t dn2cpptest_barr_sum(const uint8_t *a, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++) s += a[i];
    return s;
}

// [In,Out] write-back on the pinned buffer: multiply each element in place.
void dn2cpptest_iarr_scale(int32_t *a, int32_t n, int32_t k)
{
    for (int32_t i = 0; i < n; i++) a[i] *= k;
}

// Blittable structs pass and return by value across the C ABI with no marshalling.

typedef struct { int32_t x; int32_t y; } dn2cpptest_point;

// Struct argument by value.
int32_t dn2cpptest_point_sum(dn2cpptest_point p) { return p.x + p.y; }

// Struct returned by value.
dn2cpptest_point dn2cpptest_point_make(int32_t x, int32_t y)
{
    dn2cpptest_point p;
    p.x = x;
    p.y = y;
    return p;
}

// Mixed int + double fields (INTEGER + SSE eightbyte classification).
typedef struct { int32_t id; double value; } dn2cpptest_record;

double dn2cpptest_record_weight(dn2cpptest_record r) { return r.value * (double)r.id; }

// .NET default-marshals bool as a 4-byte Win32 BOOL: 0/1 out, any non-zero in is true.
int32_t dn2cpptest_bool_and(int32_t a, int32_t b) { return (a && b) ? 1 : 0; }
int32_t dn2cpptest_bool_or(int32_t a, int32_t b)  { return (a || b) ? 1 : 0; }

// Non-1 truthy return: the managed side must normalize it to true.
int32_t dn2cpptest_bool_truthy(void) { return 42; }
int32_t dn2cpptest_bool_falsy(void)  { return 0; }

// Set the platform's last-error slot and return e*2. BOTH slots are set — Windows reads
// GetLastError, everything else errno — and SetLastError must come last, because no
// Win32 API may run after the errno store.
int32_t dn2cpptest_set_last_error(int32_t e)
{
    errno = e;
#ifdef _WIN32
    SetLastError((DWORD)e);
#endif
    return e * 2;
}

// Scalar char: the default/Ansi CharSet is UTF-8 on Unix, so a managed char crosses as
// the first byte of its UTF-8 encoding; CharSet.Unicode passes the raw UTF-16 code unit.

// ARG, default/Ansi: echo the delivered byte widened to int32 (no return marshalling).
int32_t dn2cpptest_char_ansi_byte(unsigned char c) { return (int32_t)c; }
// ARG, CharSet.Unicode: echo the 2-byte UTF-16 code unit, widened to int32.
int32_t dn2cpptest_char_uni_code(unsigned short c) { return (int32_t)c; }
// RETURN, default/Ansi: the managed side decodes the byte as UTF-8 (0x80-0xFF -> U+FFFD).
unsigned char dn2cpptest_char_ansi_make(int32_t v) { return (unsigned char)v; }
// RETURN, CharSet.Unicode: return a caller-chosen UTF-16 code unit -> managed char.
unsigned short dn2cpptest_char_uni_make(int32_t v) { return (unsigned short)v; }

// CharSet.Unicode strings: LPWStr = NUL-terminated UTF-16, dn2cpp's internal string rep.

// ARG: UTF-16 code-unit count up to the 2-byte NUL (proves the buffer is UTF-16).
int32_t dn2cpptest_wstr_len(const uint16_t *s)
{
    int32_t n = 0;
    while (s[n]) n++;
    return n;
}

// ARG: first UTF-16 code unit (a non-ASCII char proves UTF-16, not UTF-8).
int32_t dn2cpptest_wstr_first(const uint16_t *s) { return (int32_t)s[0]; }

// RETURN: malloc'd UTF-16 "Hi" + U+263A; the managed marshaller decodes it and frees it.
uint16_t *dn2cpptest_wstr_make(void)
{
    static const uint16_t lit[] = { 'H', 'i', 0x263A, 0 };
    uint16_t *p = (uint16_t *)malloc(sizeof(lit));
    memcpy(p, lit, sizeof(lit));
    return p;
}

// ROUND TRIP: copy the UTF-16 arg into a fresh buffer, exercising both directions.
uint16_t *dn2cpptest_wstr_echo(const uint16_t *s)
{
    int32_t n = 0;
    while (s[n]) n++;
    size_t bytes = (size_t)(n + 1) * 2;
    uint16_t *p = (uint16_t *)malloc(bytes);
    memcpy(p, s, bytes);
    return p;
}

// char[] under CharSet.Unicode: a pinned 2-byte-per-element UTF-16 buffer with no NUL,
// the element count in a separate `n`. Write-backs are visible to the managed caller.
int32_t dn2cpptest_warr_elem(const uint16_t *a, int32_t i) { return (int32_t)a[i]; }
int32_t dn2cpptest_warr_sum(const uint16_t *a, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++) s += a[i];
    return s;
}
// [In,Out] write-back: uppercase ASCII in place; non-ASCII 2-byte units stay intact.
void dn2cpptest_warr_upper(uint16_t *a, int32_t n)
{
    for (int32_t i = 0; i < n; i++)
        if (a[i] >= 'a' && a[i] <= 'z') a[i] = (uint16_t)(a[i] - 32);
}

// StringBuilder: a caller-allocated [In,Out] buffer of Capacity chars + NUL. The native
// reads the current content and may overwrite it within `cap` (the Win32 GetXxx idiom).
// Ansi is UTF-8 on Unix; Unicode is UTF-16.

// Ansi [In]: UTF-8 byte length of the current content (proves it was copied in).
int32_t dn2cpptest_sb_inlen(const char *s) { return (int32_t)strlen(s); }

// Ansi [In,Out]: append "+<inlen>", uppercase within cap; returns the input length.
int32_t dn2cpptest_sb_fill(char *buf, int32_t cap)
{
    int32_t inlen = (int32_t)strlen(buf);
    char tmp[512];
    int n = snprintf(tmp, sizeof(tmp), "%s+%d", buf, (int)inlen);
    for (int i = 0; i < n; i++)
        if (tmp[i] >= 'a' && tmp[i] <= 'z') tmp[i] = (char)(tmp[i] - 32);
    if (n > cap) n = cap;
    memcpy(buf, tmp, (size_t)n);
    buf[n] = '\0';
    return inlen;
}

// Ansi [Out] non-ASCII: overwrite with UTF-8 "cafe" + U+00E9 so the caller re-decodes
// a multi-byte buffer.
void dn2cpptest_sb_cafe(char *buf, int32_t cap)
{
    (void)cap;
    strcpy(buf, "caf\xc3\xa9");
}

// Unicode [In]: UTF-16 code-unit count of the current content.
int32_t dn2cpptest_sb_winlen(const uint16_t *s)
{
    int32_t n = 0;
    while (s[n]) n++;
    return n;
}

// Unicode [In,Out]: prepend 'W', uppercase ASCII within cap; returns the input length.
int32_t dn2cpptest_sb_wfill(uint16_t *buf, int32_t cap)
{
    int32_t inlen = 0;
    while (buf[inlen]) inlen++;
    uint16_t tmp[512];
    int j = 0;
    tmp[j++] = 'W';
    for (int32_t i = 0; i < inlen && j < 510; i++)
    {
        uint16_t c = buf[i];
        if (c >= 'a' && c <= 'z') c = (uint16_t)(c - 32);
        tmp[j++] = c;
    }
    if (j > cap) j = cap;
    int i = 0;
    for (; i < j; i++) buf[i] = tmp[i];
    buf[i] = 0;
    return inlen;
}

// byref/[Out] string: a pointer-to-pointer the native fills with an allocated buffer the
// managed marshaller decodes and frees. `out` passes a null slot; `ref` round-trips *s.

// out string (Ansi): allocate a NUL-terminated UTF-8 buffer and write *out.
void dn2cpptest_out_str(char **out)
{
    char *p = (char *)malloc(16);
    strcpy(p, "native-made");
    *out = p;
}

// out string (Unicode): allocate a NUL-terminated UTF-16 buffer (Ok + U+263A).
void dn2cpptest_out_wstr(uint16_t **out)
{
    static const uint16_t lit[] = { 'O', 'k', 0x263A, 0 };
    uint16_t *p = (uint16_t *)malloc(sizeof(lit));
    memcpy(p, lit, sizeof(lit));
    *out = p;
}

// out string writing null -> the managed string marshals back as null.
void dn2cpptest_out_null(char **out) { *out = NULL; }

// ref string (Ansi): read through *s, replace it with a fresh "[<in>:<len>]".
void dn2cpptest_ref_str(char **s)
{
    const char *in = *s ? *s : "(null)";
    char buf[64];
    int n = snprintf(buf, sizeof(buf), "[%s:%d]", in, (int)strlen(in));
    char *p = (char *)malloc((size_t)n + 1);
    memcpy(p, buf, (size_t)n + 1);
    *s = p;
}

// ref string leaving *s unchanged -> the managed caller keeps its input value.
void dn2cpptest_ref_keep(char **s) { (void)s; }

// ---- [MarshalAs] overrides + non-cdecl CallingConvention ----

// Unix has one C calling convention, so .NET collapses StdCall/FastCall/Winapi/Cdecl to
// the platform default; the managed side imports this same add under each of them.
int32_t dn2cpptest_cc_add(int32_t a, int32_t b) { return a + b; }

// [MarshalAs(U1/I1)] crosses bool as a single 0/1 byte instead of the default 4-byte BOOL.
// Only bool is a useful width override: .NET requires other integers to match their size.
unsigned char dn2cpptest_u1_and(unsigned char a, unsigned char b) { return (a && b) ? 1 : 0; }
// 1-byte bool return: a non-1 truthy byte normalizes to managed true (zero -> false).
unsigned char dn2cpptest_u1_truthy(void) { return 7; }

// ---- char[] marshalling under the default/Ansi CharSet ----

// A char[] under the default/Ansi CharSet is encoded as one NUL-terminated UTF-8 buffer
// for the whole array, so its byte length differs from the element count. The default
// direction is [In]; only [In,Out]/[Out] decode the native's buffer back.

// [In]: UTF-8 byte length up to the NUL (proves the array was encoded as UTF-8).
int32_t dn2cpptest_aarr_len(const char *s) { return (int32_t)strlen(s); }
// [In]: the i-th raw byte (a multi-byte char proves UTF-8, not 1-byte-per-element).
int32_t dn2cpptest_aarr_byte(const unsigned char *a, int32_t i) { return (int32_t)a[i]; }
// null check (a null managed array marshals as a null pointer).
int32_t dn2cpptest_aarr_isnull(const char *a) { return a == NULL ? 1 : 0; }
// [In,Out]/[Out]: uppercase ASCII in place. A default (attribute-less) char[] never sees
// this write — its direction is [In].
void dn2cpptest_aarr_upper(char *buf, int32_t cap)
{
    for (int32_t i = 0; i < cap && buf[i]; i++)
        if (buf[i] >= 'a' && buf[i] <= 'z') buf[i] = (char)(buf[i] - 32);
}
// [Out]: overwrite with multi-byte UTF-8 so the write-back re-decodes it.
void dn2cpptest_aarr_cafe(char *buf, int32_t cap) { (void)cap; strcpy(buf, "caf\xc3\xa9"); }

// ---- byref/in/out of a blittable type ----

// A managed ref/out/in of a blittable type marshals as a pointer to its pinned
// native-layout storage: no copy, and write-backs are visible to the caller.
void dn2cpptest_ref_addone(int32_t *p) { (*p)++; }                       // ref/out int
void dn2cpptest_ref_dbl_scale(double *p, double k) { *p *= k; }          // ref double
void dn2cpptest_ref_long_neg(int64_t *p) { *p = -*p; }                   // ref long
void dn2cpptest_ref_byte_inc(uint8_t *p) { (*p)++; }                     // ref byte (sub-word width)
// out int: returns the quotient and writes the remainder through the pointer.
int32_t dn2cpptest_out_divmod(int32_t a, int32_t b, int32_t *rem) { *rem = a % b; return a / b; }
// ref blittable struct: swap the two fields in place (reuses dn2cpptest_point).
void dn2cpptest_ref_pt_swap(dn2cpptest_point *p) { int32_t t = p->x; p->x = p->y; p->y = t; }
// ref enum (int underlying): advance the enum value in place.
void dn2cpptest_ref_enum_next(int32_t *e) { (*e)++; }

// ---- string[] -> char** / char16_t** ----

// A string[] marshals as an array of NUL-terminated buffer pointers — UTF-8 (Ansi) or
// UTF-16 (Unicode) — one per element; a null element or a null array is a null pointer.
int32_t dn2cpptest_sa_sumlen(const char **a, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++) s += a[i] ? (int32_t)strlen(a[i]) : -1;
    return s;
}
int32_t dn2cpptest_sa_byte(const char **a, int32_t i, int32_t j) { return (int32_t)(unsigned char)a[i][j]; }
int32_t dn2cpptest_sa_isnull(const char **a) { return a == NULL ? 1 : 0; }
int32_t dn2cpptest_sa_elem_isnull(const char **a, int32_t i) { return a[i] == NULL ? 1 : 0; }
// Unicode (UTF-16) variants: code-unit length sum / first code unit.
int32_t dn2cpptest_wsa_sumlen(const uint16_t **a, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++)
    {
        if (!a[i]) { s += -1; continue; }
        int32_t k = 0; while (a[i][k]) k++; s += k;
    }
    return s;
}
int32_t dn2cpptest_wsa_first(const uint16_t **a, int32_t i) { return (int32_t)a[i][0]; }

// [In,Out]/[Out] write-back: .NET frees each slot's pointer after the call, so every
// replacement must be heap-allocated and a null element left null (its IN buffer is
// tracked separately and must not be freed). [Out] ignores the zeroed input.
void dn2cpptest_strarr_upcase_inout(char **a, int32_t n)
{
    for (int32_t i = 0; i < n; i++)
    {
        if (!a[i]) continue;                       // null element: leave it null
        size_t len = strlen(a[i]);
        char *r = (char *)malloc(len + 1);
        for (size_t k = 0; k < len; k++)
        {
            char c = a[i][k];
            r[k] = (c >= 'a' && c <= 'z') ? (char)(c - 32) : c;
        }
        r[len] = '\0';
        a[i] = r;                                  // replace slot with a heap pointer
    }
}
void dn2cpptest_strarr_fill_out(char **a, int32_t n)
{
    for (int32_t i = 0; i < n; i++)
    {
        char tmp[32];
        snprintf(tmp, sizeof tmp, "out%d", i);
        a[i] = strdup(tmp);
    }
}
// UTF-16 (CharSet.Unicode) variants of the same.
void dn2cpptest_wstrarr_upcase_inout(uint16_t **a, int32_t n)
{
    for (int32_t i = 0; i < n; i++)
    {
        if (!a[i]) continue;
        int32_t len = 0; while (a[i][len]) len++;
        uint16_t *r = (uint16_t *)malloc((size_t)(len + 1) * sizeof(uint16_t));
        for (int32_t k = 0; k < len; k++)
        {
            uint16_t c = a[i][k];
            r[k] = (c >= 'a' && c <= 'z') ? (uint16_t)(c - 32) : c;
        }
        r[len] = 0;
        a[i] = r;
    }
}
void dn2cpptest_wstrarr_fill_out(uint16_t **a, int32_t n)
{
    for (int32_t i = 0; i < n; i++)
    {
        char tmp[32];
        int32_t len = snprintf(tmp, sizeof tmp, "out%d", i);
        uint16_t *r = (uint16_t *)malloc((size_t)(len + 1) * sizeof(uint16_t));
        for (int32_t k = 0; k < len; k++) r[k] = (uint16_t)(unsigned char)tmp[k];
        r[len] = 0;
        a[i] = r;
    }
}
// [In,Out] replacing ONLY even slots: an untouched slot still holds the caller's IN
// buffer, which the write-back must decode WITHOUT freeing — for dn2cpp those are
// GC-allocated, so freeing one corrupts the heap.
void dn2cpptest_strarr_upcase_even_inout(char **a, int32_t n)
{
    for (int32_t i = 0; i < n; i++)
    {
        if (i % 2 != 0 || !a[i]) continue;        // odd / null: leave the slot untouched
        size_t len = strlen(a[i]);
        char *r = (char *)malloc(len + 1);
        for (size_t k = 0; k < len; k++)
        {
            char c = a[i][k];
            r[k] = (c >= 'a' && c <= 'z') ? (char)(c - 32) : c;
        }
        r[len] = '\0';
        a[i] = r;
    }
}
void dn2cpptest_wstrarr_upcase_even_inout(uint16_t **a, int32_t n)
{
    for (int32_t i = 0; i < n; i++)
    {
        if (i % 2 != 0 || !a[i]) continue;
        int32_t len = 0; while (a[i][len]) len++;
        uint16_t *r = (uint16_t *)malloc((size_t)(len + 1) * sizeof(uint16_t));
        for (int32_t k = 0; k < len; k++)
        {
            uint16_t c = a[i][k];
            r[k] = (c >= 'a' && c <= 'z') ? (uint16_t)(c - 32) : c;
        }
        r[len] = 0;
        a[i] = r;
    }
}

// ---- blittable-struct array marshalling (Point[]) ----

// A blittable-struct array marshals BY COPY with direction semantics, unlike a primitive
// blittable array (pinned, two-way regardless): default [In], [In,Out] copies both ways,
// [Out] zeroes the input first. Null array -> null pointer, empty -> non-null.

// [In] read: sum x+y over the elements (strides by sizeof(point) == 8).
int32_t dn2cpptest_ptarr_sum(const dn2cpptest_point *a, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++) s += a[i].x + a[i].y;
    return s;
}
// [In] read: the i-th element's x (proves correct element stride/indexing).
int32_t dn2cpptest_ptarr_x(const dn2cpptest_point *a, int32_t i) { return a[i].x; }
// null check (a null managed array marshals as a null pointer).
int32_t dn2cpptest_ptarr_isnull(const dn2cpptest_point *a) { return a == NULL ? 1 : 0; }
// [In,Out] / [Out] write-back: swap each element's fields in place.
void dn2cpptest_ptarr_swap_all(dn2cpptest_point *a, int32_t n)
{
    for (int32_t i = 0; i < n; i++) { int32_t t = a[i].x; a[i].x = a[i].y; a[i].y = t; }
}
// [In] read of the mixed struct: reads value at offset 8, pinning the trailing padding.
double dn2cpptest_recarr_sum(const dn2cpptest_record *a, int32_t n)
{
    double s = 0.0;
    for (int32_t i = 0; i < n; i++) s += a[i].value + (double)a[i].id;
    return s;
}
// [In,Out]/[Out] write-back on the mixed struct: scale value, bump id at their offsets.
void dn2cpptest_recarr_bump(dn2cpptest_record *a, int32_t n, double k)
{
    for (int32_t i = 0; i < n; i++) { a[i].value *= k; a[i].id += 1; }
}

// A managed delegate with a blittable Invoke signature marshals as a native function
// pointer invoked SYNCHRONOUSLY during the call. The pointer carries no context argument,
// so dn2cpp recovers the delegate through a per-delegate-type thread-local slot.

// Map each element through fn and sum (Σ fn(a[i])).
int32_t dn2cpptest_apply_sum(const int32_t *a, int32_t n, int32_t (*fn)(int32_t))
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++) s += fn(a[i]);
    return s;
}
// Two-argument callback: left-fold a over fn starting from seed.
int32_t dn2cpptest_reduce(const int32_t *a, int32_t n, int32_t (*fn)(int32_t, int32_t), int32_t seed)
{
    int32_t acc = seed;
    for (int32_t i = 0; i < n; i++) acc = fn(acc, a[i]);
    return acc;
}
// double native ABI: map each element through fn and sum (Σ fn(a[i])).
double dn2cpptest_apply_sumd(const double *a, int32_t n, double (*fn)(double))
{
    double s = 0.0;
    for (int32_t i = 0; i < n; i++) s += fn(a[i]);
    return s;
}

// Raw-pointer twin of apply_sum for Marshal.GetFunctionPointerForDelegate. A separate
// symbol rather than an EntryPoint alias: dn2cpp emits one extern per native symbol, so a
// second import with a different native parameter type would collide.
int32_t dn2cpptest_apply_sum_fp(const int32_t *a, int32_t n, void *fn)
{
    return dn2cpptest_apply_sum(a, n, (int32_t (*)(int32_t))fn);
}
// Returns dn2cpptest_add's address for Marshal.GetDelegateForFunctionPointer.
void *dn2cpptest_fnptr_add(void)
{
    return (void *)&dn2cpptest_add;
}

// ---- non-blittable value-struct marshalling (string/bool/nested-blittable fields) ----

// Non-blittable struct (int + string): .NET marshals it field by field, encoding the name
// to a NUL-terminated buffer (UTF-8 for the default/Ansi struct CharSet on Unix).
typedef struct { int32_t id; const char *name; } NativePerson;

// by-value [In]: a by-value struct admits no write-back.
int32_t dn2cpptest_person_score(NativePerson p) { return (int32_t)strlen(p.name) + p.id; }

// by-ref [In,Out]: bump id, replace name with an upper-cased heap copy. The old pointer is
// the caller's own [In] buffer — .NET frees only the pointer we wrote, so leave it alone.
void dn2cpptest_person_bump(NativePerson *p)
{
    p->id += 1;
    size_t len = strlen(p->name);
    char *r = (char *)malloc(len + 1);
    for (size_t k = 0; k < len; k++)
    {
        char c = p->name[k];
        r[k] = (c >= 'a' && c <= 'z') ? (char)(c - 32) : c;
    }
    r[len] = '\0';
    p->name = r;                  // do NOT free the old p->name (the caller's [In] buffer)
}

// A 4-byte Win32 BOOL + a nested blittable struct + an int tag: bool and nested-struct
// fields marshal alongside scalars.
typedef struct { int32_t on; dn2cpptest_point pt; int32_t tag; } NativeFlagPoint;

// by-value [In]: combine the bool, the nested point, and the tag deterministically.
int32_t dn2cpptest_flagpoint_eval(NativeFlagPoint f)
{
    return (f.on ? 1000 : 0) + f.pt.x * 10 + f.pt.y + f.tag;
}

// by-ref [In,Out]: toggle the bool, swap the nested point's fields, negate the tag.
void dn2cpptest_flagpoint_flip(NativeFlagPoint *f)
{
    f->on = f->on ? 0 : 1;
    int32_t t = f->pt.x; f->pt.x = f->pt.y; f->pt.y = t;
    f->tag = -f->tag;
}

// UTF-16 (CharSet.Unicode) variant: the name is a NUL-terminated UTF-16 buffer.
typedef struct { int32_t id; const uint16_t *name; } NativeWidePerson;

// by-value [In]: UTF-16 code-unit length of name + id.
int32_t dn2cpptest_wperson_score(NativeWidePerson p)
{
    int32_t n = 0; while (p.name[n]) n++;
    return n + p.id;
}

// by-ref [In,Out]: bump id, replace name with an upper-cased UTF-16 copy; the old pointer
// is the caller's [In] buffer and is not freed.
void dn2cpptest_wperson_bump(NativeWidePerson *p)
{
    p->id += 1;
    int32_t len = 0; while (p->name[len]) len++;
    uint16_t *r = (uint16_t *)malloc((size_t)(len + 1) * sizeof(uint16_t));
    for (int32_t k = 0; k < len; k++)
    {
        uint16_t c = p->name[k];
        r[k] = (c >= 'a' && c <= 'z') ? (uint16_t)(c - 32) : c;
    }
    r[len] = 0;
    p->name = r;
}

// [Out]-only: the native sees a zeroed struct (name == NULL) and fills both fields.
void dn2cpptest_person_make(NativePerson *p, int32_t id)
{
    p->id = id;
    p->name = strdup("made-by-native");
}

// ---- [MarshalAs(ByValArray, SizeConst=N)] inline fixed-length array field ----
// A ByValArray field embeds its elements INLINE in the struct. .NET copies the managed
// array into the slots [In] (zero-filling null, truncating longer, throwing on shorter)
// and copies them back into a fresh array for a by-ref [In,Out]/[Out].

// int32 element rep: a count + 4 inline ints.
typedef struct { int32_t n; int32_t vals[4]; } FixedVec;

// by-value [In]: sum the first n inline ints (n clamped to 4).
int32_t dn2cpptest_fixedvec_sum(FixedVec v)
{
    int32_t lim = v.n < 4 ? v.n : 4, s = 0;
    for (int32_t i = 0; i < lim; i++) s += v.vals[i];
    return s;
}

// by-ref [In,Out]: scale every inline int by k and bump the count.
void dn2cpptest_fixedvec_scale(FixedVec *v, int32_t k)
{
    for (int32_t i = 0; i < 4; i++) v->vals[i] *= k;
    v->n += 1;
}

// short element rep (2-byte packed stride): 5 inline int16s.
typedef struct { int16_t svals[5]; } ShortVec;

// by-value [In]: sum the 5 inline shorts.
int32_t dn2cpptest_shortvec_sum(ShortVec v)
{
    int32_t s = 0;
    for (int32_t i = 0; i < 5; i++) s += v.svals[i];
    return s;
}

// by-ref [In,Out]: negate each inline short.
void dn2cpptest_shortvec_negate(ShortVec *v)
{
    for (int32_t i = 0; i < 5; i++) v->svals[i] = (int16_t)(-v->svals[i]);
}

// double element rep (8-byte packed stride): 3 inline doubles.
typedef struct { double dvals[3]; } DblVec;

// by-value [In]: sum the 3 inline doubles.
double dn2cpptest_dblvec_sum(DblVec v) { return v.dvals[0] + v.dvals[1] + v.dvals[2]; }

// by-ref [Out]-only: the native sees zeroes and the marshaller allocates a fresh array.
void dn2cpptest_dblvec_fill(DblVec *v, double basis)
{
    v->dvals[0] = basis;
    v->dvals[1] = basis * 2.0;
    v->dvals[2] = basis * 3.0;
}

// ---- sub-word-field blittable struct marshalling ----
//
// A blittable struct of sub-word fields is packed at the real storage widths (1-byte bits
// at 0, 2-byte value at 2, sizeof == 4), so it crosses by value, by ref and as an array
// exactly like this C struct.
typedef struct { uint8_t bits; uint16_t value; } dn2cpptest_huff;

// by-value [In] + by-value return: reads both fields at their packed offsets.
dn2cpptest_huff dn2cpptest_huff_make(int32_t bits, int32_t value)
{
    dn2cpptest_huff h;
    h.bits = (uint8_t)bits;
    h.value = (uint16_t)value;
    return h;
}
int32_t dn2cpptest_huff_sum(dn2cpptest_huff h) { return (int32_t)h.bits + (int32_t)h.value; }

// ref: 8-bit and 16-bit writes through the pinned managed storage.
void dn2cpptest_huff_bump(dn2cpptest_huff *h)
{
    h->bits = (uint8_t)(h->bits + 1);
    h->value = (uint16_t)(h->value + 0x0101);
}

// array of sub-word structs: 4-byte packed stride, with an [In,Out] write-back.
int32_t dn2cpptest_huff_arr_sum_bump(dn2cpptest_huff *a, int32_t n)
{
    int32_t s = 0;
    for (int32_t i = 0; i < n; i++)
    {
        s += (int32_t)a[i].bits + (int32_t)a[i].value;
        a[i].bits = (uint8_t)(a[i].bits + 1);
    }
    return s;
}

// ---- [UnmanagedCallersOnly] reverse calls through raw function pointers ----

// The callback is a raw C function pointer with no context and no delegate: the managed
// side passes &Method on an [UnmanagedCallersOnly(CallConvCdecl)] static as a
// delegate* unmanaged[Cdecl]<...>, so each call re-enters managed code from a native frame.

// Two calls with swapped operands prove repeated re-entry within one native call.
int32_t dn2cpptest_ucb_combine(int32_t (*fn)(int32_t, int32_t), int32_t a, int32_t b)
{
    return fn(a, b) * 1000 + fn(b, a);
}

// double ABI (SSE argument/return registers on x86-64): fn(x,n) + fn(x*10,n+1).
double dn2cpptest_ucb_scale(double (*fn)(double, int32_t), double x, int32_t n)
{
    return fn(x, n) + fn(x * 10.0, n + 1);
}

// void return: invoked n times; the managed side observes the calls in its own accumulator.
void dn2cpptest_ucb_notify(void (*fn)(int32_t), int32_t n)
{
    for (int32_t i = 1; i <= n; i++)
        fn(i);
}

// ---- function-pointer struct fields: an installed vtable called back later ----

// The IoInterface pattern: a struct of raw function pointers is installed ONCE (the native
// keeps a copy) and LATER calls dispatch through the stored pointers — the install itself
// invokes nothing. The managed side stores [UnmanagedCallersOnly] addresses, so no
// delegate lifetime is involved.
typedef struct
{
    int32_t (*read)(int32_t handle, int32_t n);
    int32_t (*seek)(int32_t handle, int32_t off, int32_t whence);
    void    (*close)(int32_t handle);
} dn2cpptest_io_vtbl;

static dn2cpptest_io_vtbl g_io_vtbl;
static int32_t g_io_installed = 0;

void dn2cpptest_io_install(const dn2cpptest_io_vtbl *v)
{
    g_io_vtbl = *v;
    g_io_installed = 1;
}

// Dispatches through the vtable stored by the earlier install call.
int32_t dn2cpptest_io_exercise(int32_t handle)
{
    if (!g_io_installed)
        return -1;
    int32_t r = g_io_vtbl.read(handle, 8) + g_io_vtbl.seek(handle, 100, 2);
    g_io_vtbl.close(handle);
    return r;
}

// ---- LayoutKind.Explicit union round-trips ----

// A C union over {int64 bits, double f64, 2x int32 halves} matching a managed
// [LayoutKind.Explicit] struct with overlapping [FieldOffset] arms: each side writes one
// arm and reads a different one, pinning the byte-punning across the boundary. The int64
// arm keeps the SysV eightbyte class INTEGER on both sides of every by-value crossing.
typedef union
{
    int64_t bits;
    double  f64;
    struct { int32_t lo; int32_t hi; } halves;
} dn2cpptest_union;

// by value [In]: C# wrote the f64 arm, scaled to an exact integer so the pun is byte-exact.
int64_t dn2cpptest_union_f64_scaled(dn2cpptest_union u)
{
    return (int64_t)(u.f64 * 8.0);
}

// by value return: the native writes the halves arms; C# reads bits (and f64).
dn2cpptest_union dn2cpptest_union_make(int32_t lo, int32_t hi)
{
    dn2cpptest_union u;
    u.halves.lo = lo;
    u.halves.hi = hi;
    return u;
}

// byref [In,Out]: swap the halves in place through the pinned managed storage.
void dn2cpptest_union_swap(dn2cpptest_union *u)
{
    int32_t t = u->halves.lo;
    u->halves.lo = u->halves.hi;
    u->halves.hi = t;
}

// ---- single-pointer-field struct returned by value ----

// A one-pointer struct returned BY VALUE — the singleton-aggregate ABI shape, flattened
// into a pointer register. The buffer is a static literal, so the managed side must not
// free it: exactly a version-string accessor.
typedef struct { const char *p; } dn2cpptest_nstr;

dn2cpptest_nstr dn2cpptest_nstr_version(void)
{
    dn2cpptest_nstr s;
    s.p = "dn2cpptest 1.2.3";
    return s;
}

// The same struct as a by-value ARGUMENT; a null pointer inside reads as -1.
int32_t dn2cpptest_nstr_len(dn2cpptest_nstr s)
{
    return s.p ? (int32_t)strlen(s.p) : -1;
}

// ---- stackalloc UTF-8 argument buffers ----

// The allocation-averse binding pattern: the managed side builds a NUL-terminated UTF-8
// string in a stackalloc'd Span<byte> and passes the raw pointer — no marshaller, no heap.
int32_t dn2cpptest_utf8_len(const uint8_t *s)
{
    return (int32_t)strlen((const char *)s);
}

int32_t dn2cpptest_utf8_sum(const uint8_t *s)
{
    int32_t sum = 0;
    for (; *s; s++)
        sum += *s;
    return sum;
}

// ---- godot_variant-shaped ref struct: an Explicit union in a Sequential(Pack=8) outer ----

// Mirrors Godot.NativeInterop.godot_variant: a 4-byte tag, 4 bytes of padding, then a union
// payload at offset 8. dn2cpp classifies the managed counterpart as blittable (no managed
// reference anywhere in the field graph), so `ref v` crosses as a pinned t_v*. The 16-byte
// vec arm sizes the union, making the whole struct 24 bytes on both sides.
typedef struct { float x, y, z, w; } dn2cpptest_vec4;
typedef union
{
    int64_t         as_int;
    double          as_float;
    void           *as_ptr;
    dn2cpptest_vec4 as_vec;
} dn2cpptest_variant_data;
typedef struct
{
    int32_t                 type;   // offset 0
    dn2cpptest_variant_data data;   // offset 8 (Pack=8 padding fills 4..7)
} dn2cpptest_variant;

// byref [In,Out]: negate the active arm in place (1 = int, 2 = float, 3 = the vec arm).
void dn2cpptest_variant_negate(dn2cpptest_variant *v)
{
    if (v->type == 1)
        v->data.as_int = -v->data.as_int;
    else if (v->type == 2)
        v->data.as_float = -v->data.as_float;
    else if (v->type == 3)
    {
        v->data.as_vec.x = -v->data.as_vec.x;
        v->data.as_vec.y = -v->data.as_vec.y;
        v->data.as_vec.z = -v->data.as_vec.z;
        v->data.as_vec.w = -v->data.as_vec.w;
    }
}

// The POINTER arm: the native dereferences and bumps an int through it, proving an
// unmanaged-pointer arm crosses intact and usable.
void dn2cpptest_variant_bump_via_ptr(dn2cpptest_variant *v)
{
    if (v->type == 4 && v->data.as_ptr)
        (*(int32_t *)v->data.as_ptr)++;
}

// ---- STORED delegate callback: a marshalled fnptr kept across native calls ----

// The collision-filter idiom: the native STORES the pointer marshalled from a managed
// delegate and invokes it on a LATER frame. A synchronous thread-local slot would already
// be restored by then, so dn2cpp routes delegate P/Invoke parameters through a persistent,
// GC-rooted thunk pool.
static int32_t (*g_stored_cb)(int32_t) = 0;

void dn2cpptest_store_cb(int32_t (*fn)(int32_t))
{
    g_stored_cb = fn;
}

int32_t dn2cpptest_invoke_stored(int32_t x)
{
    return g_stored_cb ? g_stored_cb(x) : -1;
}

// ---- collision-filter-shaped STORED callback: bool(BlittableStruct*) ----

// A stored `bool (*)(BlittableStruct*)` callback. The managed delegate is
// `delegate bool F(ref FilterInput)`: the `ref` crosses as a pointer to pinned
// native-layout storage and the bool return crosses in the low byte of the return
// register. The nested all-int struct makes the byref arm a struct-in-struct field graph.
typedef struct
{
    int32_t id;
    int32_t world;
    int32_t version;
} dn2cpptest_ent;

typedef struct
{
    dn2cpptest_ent ent;   // nested blittable struct
    int32_t a;
    int32_t b;
    double  scale;        // written back by the callback
    uint8_t flag;         // a bool stored as a byte
} dn2cpptest_filter_input;

static bool (*g_filter_cb)(dn2cpptest_filter_input *) = 0;

void dn2cpptest_filter_install(bool (*fn)(dn2cpptest_filter_input *))
{
    g_filter_cb = fn;
}

int32_t dn2cpptest_filter_run(int32_t a, int32_t b)
{
    if (!g_filter_cb)
        return -1;
    dn2cpptest_filter_input in;
    in.ent.id = a;
    in.ent.world = 0;
    in.ent.version = 0;
    in.a = a;
    in.b = b;
    in.scale = 0.0;
    in.flag = 0;
    bool verdict = g_filter_cb(&in);   // bool return, read as its low byte
    // Fold the verdict and the written-back fields so the diff pins both directions.
    return (verdict ? 1000 : 2000) + (int32_t)in.scale + in.flag;
}

// ---- the marshalled layout, as the C compiler laid it out ----
//
// Every struct below is one this file already marshals, so its C layout is the layout the
// ABI actually uses; the managed section diffs Marshal.SizeOf/OffsetOf against it rather
// than against a second opinion. One entry point with a selector keeps the symbol surface
// flat, and an unknown selector answers -1 — which no sizeof or offsetof can be, so a
// managed side that drifts out of step fails the diff instead of reading a plausible number.
int32_t dn2cpptest_layout_query(int32_t which)
{
    switch (which)
    {
        case 0: return (int32_t)sizeof(NativePerson);
        case 1: return (int32_t)offsetof(NativePerson, id);
        case 2: return (int32_t)offsetof(NativePerson, name);
        case 3: return (int32_t)sizeof(NativeWidePerson);
        case 4: return (int32_t)offsetof(NativeWidePerson, name);
        case 5: return (int32_t)sizeof(NativeFlagPoint);
        case 6: return (int32_t)offsetof(NativeFlagPoint, on);
        case 7: return (int32_t)offsetof(NativeFlagPoint, pt);
        case 8: return (int32_t)offsetof(NativeFlagPoint, tag);
        case 9: return (int32_t)sizeof(dn2cpptest_point);
        case 10: return (int32_t)sizeof(dn2cpptest_record);
        case 11: return (int32_t)offsetof(dn2cpptest_record, value);
        case 12: return (int32_t)sizeof(FixedVec);
        case 13: return (int32_t)offsetof(FixedVec, vals);
        default: return -1;
    }
}
