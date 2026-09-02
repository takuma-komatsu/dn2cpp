// dn2cpp_exceptions.cpp — exception machinery of the dn2cpp runtime:
// dn2cpp_fail / the vcall trap, throw/rethrow + the in-flight exception list,
// throw-time stack traces, and AggregateException.

#include "dn2cpp_core.h"
#include <algorithm>
#include <chrono>  // DateTime.Now/UtcNow wall clock
#include <ctime>   // localtime_r for DateTime.Now local components
#include <string>  // Path/File/Environment helpers
#include <vector>  // Path.GetFullPath segment stack
#ifndef _WIN32
#include <dlfcn.h> // dlsym for dn2cpp_native_library_get_symbol
#endif
#if defined(DN2CPP_USE_BOEHM_GC) && defined(__APPLE__)
#include <dlfcn.h>          // dladdr — locate the image holding the runtime
#include <mach-o/getsect.h> // getsegmentdata — its __DATA segment bounds
#include <mach-o/loader.h>  // mach_header_64
#endif
#include <cstdio>   // File read/write (fopen/fread/fwrite)
#include <cstdarg>  // dn2cpp_report_boundary_exception's printf-style boundary name
#include <cerrno>   // File.Delete errno/ENOENT
#include <cstdlib>  // Environment.GetEnvironmentVariable getenv
#include <cstring>  // Type.Assembly identity name compare (dn2cpp_assembly_equals)
#ifdef _MSC_VER
#include <malloc.h> // _aligned_malloc/_aligned_realloc/_aligned_free (no std::aligned_alloc on MSVC)
#endif
#ifdef _WIN32
// GetLastError() for dn2cpp_pinvoke_capture_last_error. NOMINMAX / WIN32_LEAN_AND_MEAN
// are project-wide compile definitions (runtime/CMakeLists.txt's MSVC arm), not
// repeated here — see platform/windows/dn2cpp_pal_windows.cpp's header comment.
#include <windows.h>
#include <io.h>     // _setmode / _fileno — put stdout/stderr in binary mode at init
#include <fcntl.h>  // _O_BINARY
#endif
#include <thread>              // real OS threads (Thread, worker pool)
#include <mutex>               // scheduler run-queue + task-completion + console locks
#include <condition_variable> // task_block sleeps until a cross-thread completion
#include <atomic>             // g_inflight_async_tasks, real Interlocked
#include <deque>             // Task.Run worker-pool work queue
#include <new>               // placement new (in-place ctor of a GC-allocated Timer)
#include <unordered_map>     // Dn2CppType intern table (one Type per type-info handle)

// Allocation-size query for NativeMemory.AlignedRealloc routes through the PAL
// (malloc_size on macOS, malloc_usable_size on glibc/musl/BSD).
#include "platform/dn2cpp_pal.h"

// An empty virtual slot was called. Name the type and, by scanning its method tables
// for the methods whose slot holds a trap, the method(s) it could have been — the whole
// point of the trap is that "0x0 is not a function" tells nobody which override went
// missing. See the declaration in dn2cpp.h for what this cannot see.
//
// The vtable twin of dn2cpp_slot_missing_report in dn2cpp_casts.cpp, and an abort for
// the same reason: reaching here means the reachability closure and the emitted vtable
// disagree — a build defect no caller could have avoided or can handle.
[[noreturn]] static void dn2cpp_vcall_report(Dn2CppObject* self, const void* trap)
{
    char buf[512];
    const Dn2CppTypeInfo* t = self != nullptr ? self->type : nullptr;
    if (t == nullptr || t->name == nullptr)
        dn2cpp_fail("virtual call through a slot with no emitted implementation "
                    "(receiver unreadable — a struct-returning virtual?)");
    int n = std::snprintf(buf, sizeof(buf),
        "virtual call on %s through a slot with no emitted implementation", t->name);
    if (t->vtable != nullptr)
    {
        const char* sep = " — the method is one of: ";
        for (const Dn2CppTypeInfo* b = t; b != nullptr && n > 0 && n < static_cast<int>(sizeof(buf)); b = b->base)
        {
            for (int32_t i = 0; i < b->methodCount; i++)
            {
                const Dn2CppMethodInfo& m = b->methods[i];
                if (m.vtableSlot < 0 || t->vtable[m.vtableSlot] != trap)
                    continue;
                n += std::snprintf(buf + n, sizeof(buf) - static_cast<size_t>(n), "%s%s.%s",
                    sep, b->name != nullptr ? b->name : "?", m.name != nullptr ? m.name : "?");
                sep = ", ";
                if (n <= 0 || n >= static_cast<int>(sizeof(buf)))
                    break;
            }
        }
    }
    dn2cpp_fail(buf);
}

[[noreturn]] void dn2cpp_vcall_unimplemented(Dn2CppObject* self)
{
    dn2cpp_vcall_report(self, reinterpret_cast<const void*>(&dn2cpp_vcall_unimplemented));
}

// Entered through a per-signature trap thunk (declaration comment in dn2cpp.h): the
// thunk hands over its own address, so the scan above matches exactly the slots
// holding it — the same-signature subset, usually one.
[[noreturn]] void dn2cpp_vcall_unimplemented_at(Dn2CppObject* self, const void* slotFn)
{
    dn2cpp_vcall_report(self, slotFn);
}

// One image, one registration (the generated init prologue), matching the other
// image-scoped installs (dn2cpp_string_set_interfaces et al.).
static const void* const* g_vcall_trap_fns;
static int32_t g_vcall_trap_count;

void dn2cpp_register_vcall_traps(const void* const* fns, int32_t count)
{
    g_vcall_trap_fns = fns;
    g_vcall_trap_count = count;
}

// Whether `fn` is a vtable dispatch trap — the shared symbol or one of the image's
// registered per-signature thunks. For probes that must not CALL a trapped slot to
// find out (the trap aborts). Linear over a small set, on already-cold paths.
static bool dn2cpp_is_vcall_trap(const void* fn)
{
    if (fn == reinterpret_cast<const void*>(&dn2cpp_vcall_unimplemented))
        return true;
    for (int32_t i = 0; i < g_vcall_trap_count; i++)
        if (g_vcall_trap_fns[i] == fn)
            return true;
    return false;
}

[[noreturn]] void dn2cpp_fail(const char* message)
{
    std::fprintf(stderr, "dn2cpp fatal: %s\n", message);
    // abort() does not flush open C streams, so every buffered Console.Write up to
    // this point would vanish — real .NET's crash path does not lose output written
    // before the crash. Through the PAL, not std::fflush: on a target whose console is
    // not stdio the buffer holding those lines is the sink's, not the CRT's.
    dn2cpp_pal_console_flush();
    std::abort();
}

// System.Exception-derived, so `catch (AggregateException)` and `catch (Exception)`
// both match (isinst walks the base chain). Carries an InnerExceptions array on top
// of the uniform exception layout; built by dn2cpp_aggregate_exception_new.
extern const Dn2CppType dn2cpp_aggregate_exception_type_obj;
const Dn2CppTypeInfo dn2cpp_aggregate_exception_type =
    dn2cpp_ti_with_typeobject({ "System.AggregateException", &dn2cpp_exception_type, 0, nullptr, nullptr, 0 }, &dn2cpp_aggregate_exception_type_obj);
const Dn2CppType dn2cpp_aggregate_exception_type_obj = { { &dn2cpp_type_type }, &dn2cpp_aggregate_exception_type };

// The in-flight exception list (see the declaration comment in dn2cpp.h): one
// GC-allocated node per thrown-but-not-yet-consumed managed exception, linked
// from a static head so the whole chain — and the exception graph each node
// points at — stays reachable while the object rides the (unscanned) __cxa
// exception buffer through unwinding and finally bodies. Pop is by value:
// popping an object that is not on the list is a harmless no-op. A push with
// no matching pop (a finally overriding the in-flight exception with its own
// throw abandons the original) strands its node, so the list is capped and
// the oldest node dropped beyond the cap — by then the exception is either
// long consumed (rooted from a handler) or abandoned. The mutex is a leaf:
// never held while taking any other lock (the node is allocated before it).
struct Dn2CppInflightExcNode
{
    Dn2CppObject* obj;
    Dn2CppInflightExcNode* next;
};
static std::mutex& g_inflight_exc_mtx = dn2cpp_never_destroyed<std::mutex>();
static DN2CPP_GC_STATIC_ROOT Dn2CppInflightExcNode* g_inflight_excs = nullptr; // static => a GC root
static int g_inflight_exc_count = 0;
static constexpr int kInflightExcCap = 512;

void dn2cpp_exc_inflight_push(Dn2CppObject* obj)
{
    auto* node = static_cast<Dn2CppInflightExcNode*>(dn2cpp_alloc(sizeof(Dn2CppInflightExcNode)));
    node->obj = obj;
    std::lock_guard<std::mutex> lk(g_inflight_exc_mtx);
    dn2cpp_gc_store_ref(&node->next, g_inflight_excs);
    g_inflight_excs = node;
    if (++g_inflight_exc_count > kInflightExcCap)
    {
        Dn2CppInflightExcNode* n = g_inflight_excs;
        while (n->next->next != nullptr)
            n = n->next;
        n->next = nullptr;
        g_inflight_exc_count--;
    }
}

void dn2cpp_exc_inflight_pop(Dn2CppObject* obj)
{
    std::lock_guard<std::mutex> lk(g_inflight_exc_mtx);
    for (Dn2CppInflightExcNode** pp = &g_inflight_excs; *pp != nullptr; pp = &(*pp)->next)
    {
        if ((*pp)->obj == obj)
        {
            Dn2CppInflightExcNode* dead = *pp;
            *pp = dead->next;
            dn2cpp_gc_write_barrier_if_heap(pp); // pp may name the always-rescanned static head
            dead->next = nullptr;
            g_inflight_exc_count--;
            return;
        }
    }
}

// Whether the type's base chain reaches System.Exception. Mirrors the
// interpreter's type_is_exception rather than calling it: dn2cpp_interp.cpp
// stays unlinked unless a hot-update base pulls it in, and a reference from
// this always-linked TU would drag it into every binary.
bool dn2cpp_type_is_exception(const Dn2CppTypeInfo* ti)
{
    for (const Dn2CppTypeInfo* t = ti; t != nullptr; t = t->base)
    {
        if (t == &dn2cpp_exception_type || std::strcmp(t->name, "System.Exception") == 0)
            return true;
    }
    return false;
}

static constexpr int32_t kExcTraceMax = 64;

// Shadow-stack storage (see the contract at Dn2CppShadowStack in
// dn2cpp_core.h). The Dn2CppShadowStack header lives inside a thread_local owner
// struct — valid for the whole thread lifetime — and only the frames buffer
// is heap memory, freed by the owner's destructor on thread exit (the same
// discipline as Dn2CppGCThread's thread-static release). Plain malloc, not
// GC memory: the entries are rodata string literals the collector must never
// scan, and a GC buffer reachable only from raw TLS would not be a root on
// every platform anyway.
static constexpr int32_t kShadowStackCap = 1024;

thread_local Dn2CppShadowStack* dn2cpp_shadow_tls = nullptr;

namespace
{
struct Dn2CppShadowStackOwner
{
    Dn2CppShadowStack st{ 0, 0, nullptr };
    ~Dn2CppShadowStackOwner()
    {
        std::free(st.frames);
        st.frames = nullptr;
        st.cap = 0; // a straggler guard destructor may still run: count-only, no write
        dn2cpp_shadow_tls = nullptr;
    }
};
} // namespace

Dn2CppShadowStack* dn2cpp_shadow_stack_acquire()
{
    static thread_local Dn2CppShadowStackOwner owner;
    Dn2CppShadowStack* st = &owner.st;
    if (dn2cpp_shadow_tls == nullptr)
    {
        st->frames = static_cast<const char**>(
            std::malloc(sizeof(const char*) * static_cast<size_t>(kShadowStackCap)));
        st->cap = st->frames != nullptr ? kShadowStackCap : 0; // 0: count-only mode
        dn2cpp_shadow_tls = st;
    }
    return st;
}

// Plain bool, no atomic: the only write is the generated init prologue, before
// any managed code runs, and later thread creation synchronizes-with it.
static bool g_shadow_stack_enabled = false;

void dn2cpp_shadow_stack_mark_enabled()
{
    g_shadow_stack_enabled = true;
}

bool dn2cpp_shadow_stack_is_enabled()
{
    return g_shadow_stack_enabled;
}

void dn2cpp_exc_stamp_trace(Dn2CppObject* obj)
{
    // Guard on the SHAPE, not the caller: IL `throw` may raise any object, and
    // writing the trace slot into one without the exception prefix would
    // corrupt whatever the allocation holds there instead.
    if (obj->type == nullptr || !dn2cpp_type_is_exception(obj->type))
        return;
    // Shadow stack first: when the transpiler planted frame guards and
    // any frame is live on this thread, the recorded names beat a PC walk —
    // they survive -O2 inlining and exist on WASM. Copy EVERY stored frame
    // (min(depth, cap), innermost first, matching kind 0's order), not the
    // kExcTraceMax the PC path caps at: the whole point of the baked names is
    // an exact trace. Frames past `cap` were counted but never stored; record
    // the loss in `dropped` instead of pretending the trace is complete.
    if (const Dn2CppShadowStack* ss = dn2cpp_shadow_tls; ss != nullptr && ss->depth > 0)
    {
        int32_t stored = ss->depth < ss->cap ? ss->depth : ss->cap;
        auto* t = static_cast<Dn2CppExcTrace*>(dn2cpp_alloc_atomic(
            offsetof(Dn2CppExcTrace, entries) + sizeof(void*) * static_cast<size_t>(stored)));
        t->kind = 1;
        t->dropped = ss->depth > ss->cap ? ss->depth - ss->cap : 0;
        t->count = stored;
        for (int32_t i = 0; i < stored; i++)
            t->entries[i] = const_cast<char*>(ss->frames[stored - 1 - i]);
        dn2cpp_gc_store_ref<const Dn2CppExcTrace>(
            &static_cast<Dn2CppExceptionObject*>(obj)->trace, t);
        return;
    }
    void* buf[kExcTraceMax];
    int32_t n = dn2cpp_pal_backtrace(buf, kExcTraceMax);
    if (n <= 0)
        return; // nothing captured (WASM): the slot stays as it was
    // Pointer-free (atomic) GC memory: the entries are function entry
    // addresses (the dn2cpp_pal_backtrace contract) the collector must not
    // scan; the exception object's own reference keeps it alive. The
    // allocation runs BEFORE the throw, in ordinary execution context — the
    // same safety class as the inflight node push.
    auto* t = static_cast<Dn2CppExcTrace*>(dn2cpp_alloc_atomic(
        offsetof(Dn2CppExcTrace, entries) + sizeof(void*) * static_cast<size_t>(n)));
    t->kind = 0;
    t->dropped = 0;
    t->count = n;
    std::memcpy(t->entries, buf, sizeof(void*) * static_cast<size_t>(n));
    dn2cpp_gc_store_ref<const Dn2CppExcTrace>(
        &static_cast<Dn2CppExceptionObject*>(obj)->trace, t);
}

[[noreturn]] void dn2cpp_rethrow(Dn2CppObject* obj)
{
    if (obj == nullptr)
        dn2cpp_throw_null_reference();
    // Preserve semantics: stamp only an exception that was never thrown (a
    // Task faulted with a constructed-but-unthrown exception still gets a
    // trace of this propagation site, which beats none).
    if (!dn2cpp_type_is_exception(obj->type)
        || static_cast<Dn2CppExceptionObject*>(obj)->trace == nullptr)
        dn2cpp_exc_stamp_trace(obj);
    dn2cpp_exc_inflight_push(obj);
    throw Dn2CppException{ obj };
}

const char* dn2cpp_sr_text(const char* key)
{
    for (int32_t i = 0; i < dn2cpp_bcl_message_count; i++)
        if (std::strcmp(dn2cpp_bcl_messages[i].key, key) == 0)
            return dn2cpp_bcl_messages[i].text;
    return nullptr;
}

// The composite-format substitution SR.Format performs, over a template this runtime
// already holds: `{0}`/`{1}` only, no alignment or format specifier, because these are
// exception-message resources and nothing else may reach it. An unresolved template
// (null) yields null, which every caller reads as "no message".
static Dn2CppString* dn2cpp_sr_format(const char* key, const std::string* args, int32_t argc)
{
    const char* tpl = dn2cpp_sr_text(key);
    if (tpl == nullptr)
        return nullptr;
    std::string s;
    for (const char* p = tpl; *p != '\0'; p++)
    {
        if (p[0] == '{' && p[1] >= '0' && p[1] <= '9' && p[2] == '}' && p[1] - '0' < argc)
        {
            s += args[p[1] - '0'];
            p += 2;
            continue;
        }
        s += *p;
    }
    return dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size()));
}

static std::string dn2cpp_sr_arg(Dn2CppString* s)
{
    if (s == nullptr)
        return std::string();
    int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
    std::string out(static_cast<size_t>(n > 0 ? n : 0), '\0');
    if (n > 0)
        dn2cpp_string_to_utf8(s, out.data(), n);
    return out;
}

// The message real .NET's parameterless ctor of this exception type gives — the SR text
// folded in by the emitter. Only the handles this runtime raises are mapped; a generated
// type-info reads null and keeps Exception.Message's type-name fallback, which is what
// .NET gives a derived exception whose ctor set no message.
static const char* dn2cpp_default_message_key(const Dn2CppTypeInfo* ti)
{
    if (ti == &dn2cpp_overflow_exception_type) return DN2CPP_SR_OVERFLOW;
    if (ti == &dn2cpp_index_out_of_range_exception_type) return DN2CPP_SR_INDEX_OUT_OF_RANGE;
    if (ti == &dn2cpp_argument_exception_type) return DN2CPP_SR_ARGUMENT;
    if (ti == &dn2cpp_argument_out_of_range_exception_type) return DN2CPP_SR_ARGUMENT_OUT_OF_RANGE;
    if (ti == &dn2cpp_argument_null_exception_type) return DN2CPP_SR_ARGUMENT_NULL;
    if (ti == &dn2cpp_invalid_operation_exception_type) return DN2CPP_SR_INVALID_OPERATION;
    if (ti == &dn2cpp_object_disposed_exception_type) return DN2CPP_SR_OBJECT_DISPOSED;
    if (ti == &dn2cpp_arithmetic_exception_type) return DN2CPP_SR_ARITHMETIC;
    if (ti == &dn2cpp_out_of_memory_exception_type) return DN2CPP_SR_OUT_OF_MEMORY;
    if (ti == &dn2cpp_invalid_cast_exception_type) return DN2CPP_SR_INVALID_CAST;
    if (ti == &dn2cpp_type_load_exception_type) return DN2CPP_SR_TYPE_LOAD;
    if (ti == &dn2cpp_not_supported_exception_type) return DN2CPP_SR_NOT_SUPPORTED;
    if (ti == &dn2cpp_platform_not_supported_exception_type) return DN2CPP_SR_PLATFORM_NOT_SUPPORTED;
    if (ti == &dn2cpp_format_exception_type) return DN2CPP_SR_FORMAT;
    if (ti == &dn2cpp_io_exception_type) return DN2CPP_SR_IO;
    if (ti == &dn2cpp_file_not_found_exception_type) return DN2CPP_SR_FILE_NOT_FOUND;
    if (ti == &dn2cpp_unauthorized_access_exception_type) return DN2CPP_SR_UNAUTHORIZED_ACCESS;
    if (ti == &dn2cpp_key_not_found_exception_type) return DN2CPP_SR_KEY_NOT_FOUND;
    if (ti == &dn2cpp_ambiguous_match_exception_type) return DN2CPP_SR_AMBIGUOUS_MATCH;
    if (ti == &dn2cpp_missing_method_exception_type) return DN2CPP_SR_MISSING_METHOD;
    if (ti == &dn2cpp_null_reference_exception_type) return DN2CPP_SR_NULL_REFERENCE;
    if (ti == &dn2cpp_divide_by_zero_exception_type) return DN2CPP_SR_DIVIDE_BY_ZERO;
    if (ti == &dn2cpp_synchronization_lock_exception_type) return DN2CPP_SR_SYNCHRONIZATION_LOCK;
    return nullptr;
}

Dn2CppString* dn2cpp_default_message(const Dn2CppTypeInfo* ti)
{
    const char* key = dn2cpp_default_message_key(ti);
    if (key == nullptr)
        return nullptr;
    const char* text = dn2cpp_sr_text(key);
    return text == nullptr
        ? nullptr
        : dn2cpp_string_from_utf8(text, static_cast<int32_t>(std::strlen(text)));
}

void dn2cpp_overflow()
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_overflow_exception_type,
        dn2cpp_default_message(&dn2cpp_overflow_exception_type), nullptr));
}

// ThrowHelper trap intrinsics: allocate a managed exception of the matching type and
// throw it (catchable). The BCL's exception-construction IL stays out of the program;
// its message does not — the type's real .NET default text is folded in at transpile
// time. Allocation must go through dn2cpp_exception_new: a bare Dn2CppObject header
// would under-size the exception prefix that the throw stamps and that
// .Message/.InnerException/.HResult read.
[[noreturn]] void dn2cpp_throw_of(const Dn2CppTypeInfo* ti)
{
    dn2cpp_throw(dn2cpp_exception_new(ti, dn2cpp_default_message(ti), nullptr));
}

// The same trap with a message the EMITTER resolved: the text is already the final
// sentence, including the " (Parameter 'x')" tail when the site named an
// ExceptionArgument.
[[noreturn]] void dn2cpp_throw_of_msg(const Dn2CppTypeInfo* ti, const char* message)
{
    dn2cpp_throw(dn2cpp_exception_new(ti,
        dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message))), nullptr));
}

// A trap whose message is a composite SR format the CALLER can fill — the sites that
// hold the operand .NET's own message names (the parsed string, the duplicate key). The
// template resolving to null degrades to the type's default, never to a raw "{0}".
[[noreturn]] void dn2cpp_throw_sr1(const Dn2CppTypeInfo* ti, const char* key, Dn2CppString* a0)
{
    std::string args[1] = { dn2cpp_sr_arg(a0) };
    Dn2CppString* msg = dn2cpp_sr_format(key, args, 1);
    dn2cpp_throw(dn2cpp_exception_new(ti,
        msg != nullptr ? msg : dn2cpp_default_message(ti), nullptr));
}

// ArgumentOutOfRangeException as real .NET assembles it: the resource's own sentence,
// then the " (Parameter 'x')" ArgumentException.Message appends, then the newline +
// "Actual value was v." ArgumentOutOfRangeException.Message appends. A runtime-raised
// exception has no managed _paramName/_actualValue field for those overrides to read, so
// the site that knows them bakes the whole text in (ParamName itself stays null).
[[noreturn]] static void dn2cpp_throw_aoor(const char* key, const std::string* args,
    int32_t argc, const char* paramName, const std::string* actual)
{
    Dn2CppString* head = dn2cpp_sr_format(key, args, argc);
    if (head == nullptr)
        dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type);
    std::string s = dn2cpp_sr_arg(head);
    std::string one[1] = { std::string(paramName) };
    if (Dn2CppString* p = dn2cpp_sr_format(DN2CPP_SR_PARAM_NAME, one, 1); p != nullptr)
    {
        s += ' ';
        s += dn2cpp_sr_arg(p);
    }
    if (actual != nullptr)
    {
        one[0] = *actual;
        if (Dn2CppString* a = dn2cpp_sr_format(DN2CPP_SR_ACTUAL_VALUE, one, 1); a != nullptr)
        {
            // Environment.NewLine, which is what AOORE.Message concatenates.
#ifdef _WIN32
            s += "\r\n";
#else
            s += '\n';
#endif
            s += dn2cpp_sr_arg(a);
        }
    }
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_argument_out_of_range_exception_type,
        dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size())), nullptr));
}

[[noreturn]] void dn2cpp_throw_argument_out_of_range_value(const char* key,
    const char* paramName, Dn2CppString* value)
{
    std::string args[2] = { std::string(paramName), dn2cpp_sr_arg(value) };
    dn2cpp_throw_aoor(key, args, 2, paramName, &args[1]);
}

[[noreturn]] void dn2cpp_throw_argument_out_of_range_param(const char* key,
    const char* paramName)
{
    dn2cpp_throw_aoor(key, nullptr, 0, paramName, nullptr);
}

// ArgumentNullException as real .NET assembles it: the resource's sentence plus the
// " (Parameter 'x')" tail ArgumentException.Message appends. ParamName itself stays null
// for the same reason it does on every runtime-raised trap (see dn2cpp_exception_new).
[[noreturn]] void dn2cpp_throw_argument_null_param(const char* paramName)
{
    const char* head = dn2cpp_sr_text(DN2CPP_SR_ARGUMENT_NULL);
    std::string one[1] = { std::string(paramName) };
    Dn2CppString* tail = dn2cpp_sr_format(DN2CPP_SR_PARAM_NAME, one, 1);
    if (head == nullptr || tail == nullptr)
        dn2cpp_throw_argument_null();
    std::string s = head;
    s += ' ';
    s += dn2cpp_sr_arg(tail);
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_argument_null_exception_type,
        dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size())), nullptr));
}

void dn2cpp_throw_index_out_of_range() { dn2cpp_throw_of(&dn2cpp_index_out_of_range_exception_type); }
void dn2cpp_throw_argument_out_of_range() { dn2cpp_throw_of(&dn2cpp_argument_out_of_range_exception_type); }
void dn2cpp_throw_argument_null() { dn2cpp_throw_of(&dn2cpp_argument_null_exception_type); }
void dn2cpp_throw_argument() { dn2cpp_throw_of(&dn2cpp_argument_exception_type); }
void dn2cpp_throw_invalid_operation() { dn2cpp_throw_of(&dn2cpp_invalid_operation_exception_type); }
void dn2cpp_throw_object_disposed() { dn2cpp_throw_of(&dn2cpp_object_disposed_exception_type); }
void dn2cpp_throw_arithmetic() { dn2cpp_throw_of(&dn2cpp_arithmetic_exception_type); }
void dn2cpp_throw_out_of_memory() { dn2cpp_throw_of(&dn2cpp_out_of_memory_exception_type); }
void dn2cpp_throw_type_load() { dn2cpp_throw_of(&dn2cpp_type_load_exception_type); }
void dn2cpp_throw_not_supported() { dn2cpp_throw_of(&dn2cpp_not_supported_exception_type); }
void dn2cpp_throw_key_not_found() { dn2cpp_throw_of(&dn2cpp_key_not_found_exception_type); }
void dn2cpp_throw_ambiguous_match() { dn2cpp_throw_of(&dn2cpp_ambiguous_match_exception_type); }
void dn2cpp_throw_rank() { dn2cpp_throw_of(&dn2cpp_rank_exception_type); }
void dn2cpp_throw_null_reference() { dn2cpp_throw_of(&dn2cpp_null_reference_exception_type); }
void dn2cpp_throw_divide_by_zero() { dn2cpp_throw_of(&dn2cpp_divide_by_zero_exception_type); }
void dn2cpp_throw_format() { dn2cpp_throw_of(&dn2cpp_format_exception_type); }
void dn2cpp_throw_format_value(Dn2CppString* value)
{
    dn2cpp_throw_sr1(&dn2cpp_format_exception_type,
        DN2CPP_SR_FORMAT_INVALID_STRING_WITH_VALUE, value);
}

// Diagnosable NotSupportedException — the same catchable type as the bare
// dn2cpp_throw_not_supported, carrying the reason. For AOT-boundary misses
// whose cause is invisible at the catch site (Type.MakeGenericType over an
// instantiation the AOT closure never generated names WHICH instantiation).
void dn2cpp_throw_not_supported_msg(const char* message)
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_not_supported_exception_type,
        dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message))), nullptr));
}

// Diagnosable ArgumentException — the same catchable type as the bare
// dn2cpp_throw_argument, carrying the reason. The Marshal marshalability verdict
// (dn2cpp_marshal_require_size) is the caller: real .NET's refusal names the type it
// refused, and a bare ArgumentException at a Marshal.SizeOf call site says nothing
// about WHICH of the argument's properties disqualified it.
void dn2cpp_throw_argument_msg(const char* message)
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_argument_exception_type,
        dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message))), nullptr));
}

// Diagnosable InvalidOperationException — the same catchable type as the bare
// dn2cpp_throw_invalid_operation, carrying the reason: the family is what a caller
// catches, the message is what tells them which overload they wanted.
void dn2cpp_throw_invalid_operation_msg(const char* message)
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_invalid_operation_exception_type,
        dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message))), nullptr));
}

// Constructor-resolution miss (Activator.CreateInstance and friends): a
// catchable MissingMethodException carrying the diagnosable reason, like the
// dynamic-codegen PNSE trap below.
void dn2cpp_throw_missing_method(const char* message)
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_missing_method_exception_type,
        dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message))), nullptr));
}

[[noreturn]] void dn2cpp_throw_dll_not_found(const char* moduleName)
{
    std::string message = "Unable to load shared library '";
    message += moduleName;
    message += "'.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_dll_not_found_exception_type,
        dn2cpp_string_from_utf8(message.c_str(), static_cast<int32_t>(message.size())), nullptr));
}

[[noreturn]] void dn2cpp_throw_entry_point_not_found(const char* entryPoint)
{
    std::string message = "Unable to find an entry point named '";
    message += entryPoint;
    message += "'.";
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_entry_point_not_found_exception_type,
        dn2cpp_string_from_utf8(message.c_str(), static_cast<int32_t>(message.size())), nullptr));
}

// ResourceManager asked for a set no loaded assembly embeds. The TYPE is the point:
// .NET's documentation tells callers to catch MissingManifestResourceException, so any
// other family makes the documented `catch` silently not fire. The message stays
// dn2cpp's own; only the family is a compatibility claim.
void dn2cpp_throw_missing_manifest_resource(const char* message)
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_missing_manifest_resource_exception_type,
        dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message))), nullptr));
}

// Dynamic-code-generation trap: any statically-cut Reflection.Emit / DLR
// CallSite / Expression.Compile member throws this catchable
// PlatformNotSupportedException at run time (the NativeAOT posture), carrying
// the member name in the message for diagnosability.
void dn2cpp_throw_platform_not_supported(const char* message)
{
    dn2cpp_throw(dn2cpp_exception_new(&dn2cpp_platform_not_supported_exception_type,
        dn2cpp_string_from_utf8(message, static_cast<int32_t>(std::strlen(message))), nullptr));
}

// The DN2CPP_TF_LAYOUT_UNKNOWN guard. See the flag's doc in dn2cpp_core.h for why a
// stamped 1 must not be answered and why the test is the bit rather than the number.
void dn2cpp_require_layout(const Dn2CppTypeInfo* ti)
{
    if (ti == nullptr || (ti->flags & DN2CPP_TF_LAYOUT_UNKNOWN) == 0)
        return;
    const char* name = ti->name != nullptr ? ti->name : "<unnamed type>";
    char buf[512];
    std::snprintf(buf, sizeof(buf),
        "The size of '%s' is not available: the transpiler reached this value type "
        "through a type token only, so it carries no emitted field layout, and its CLR "
        "layout extent could not be modeled either (a field typed at an intrinsic or "
        "external type of unmodeled width, an uncompleted generic instantiation, or an "
        "explicit layout the emitted C++ cannot represent). Use the type in code — a "
        "value of it anywhere gives it a real layout — rather than naming it only in a "
        "typeof/MakeGenericType.",
        name);
    dn2cpp_throw_platform_not_supported(buf);
}

Dn2CppObject* dn2cpp_exception_new(const Dn2CppTypeInfo* ti, Dn2CppString* message, Dn2CppObject* inner)
{
    // Size by the type, not by the prefix: a runtime-RAISED ArgumentNullException is
    // allocated here, but the type declares a field of its own (_paramName), and after
    // the startup bind the handle says so (instanceSize > 0). Allocating the bare prefix
    // would put that field past the end of the object, where reading .ParamName on a
    // caught trap exception reads whatever follows it. The GC zeroes, so the field reads
    // back null — the runtime carries no paramName to seed it with, a divergence from
    // real .NET, but a quiet null instead of a wild read.
    size_t size = ti->instanceSize > static_cast<int32_t>(sizeof(Dn2CppExceptionObject))
        ? static_cast<size_t>(ti->instanceSize) : sizeof(Dn2CppExceptionObject);
    auto* e = static_cast<Dn2CppExceptionObject*>(dn2cpp_alloc(size));
    e->type = ti;
    // Every managed `new System.Exception` / `new AggregateException` (opaque intrinsics,
    // no emitted layout) and any runtime-raised exception type lands here. Every OTHER
    // exception type — user-defined or a BCL one with fields of its own — takes the
    // intercept path that sizes the allocation for the emitted struct and runs the real
    // ctor body; the finalize registration below still fires for those that reach here.
    if (ti->finalize != nullptr)
        dn2cpp_register_finalizer(e);
    dn2cpp_gc_store_ref(&e->message, message);
    dn2cpp_gc_store_ref(&e->inner, inner);
    // System.Exception's base ctor default (COR_E_EXCEPTION). A derived ctor that
    // runs (the AOT/interp newobj paths) overwrites this with its per-type value via
    // set_HResult; a runtime-RAISED exception (no ctor body) keeps the base default
    // rather than the per-type COR_E_* — the same documented divergence as its null
    // paramName, and honest (the runtime carries no per-type HResult to seed).
    e->hresult = static_cast<int32_t>(0x80131500);
    return e;
}

Dn2CppObject* dn2cpp_exception_for_hresult(int32_t hresult)
{
    if (hresult >= 0)
        return nullptr;

    const Dn2CppTypeInfo* ti = &dn2cpp_com_exception_type;
    switch (static_cast<uint32_t>(hresult))
    {
        case 0x80070057u: ti = &dn2cpp_argument_exception_type; break;
        case 0x8007000Eu: ti = &dn2cpp_out_of_memory_exception_type; break;
        case 0x80070005u: ti = &dn2cpp_unauthorized_access_exception_type; break;
    }
    auto* ex = reinterpret_cast<Dn2CppExceptionObject*>(
        dn2cpp_exception_new(ti, dn2cpp_default_message(ti), nullptr));
    ex->hresult = hresult;
    return ex;
}

// Exception.get_InnerException on an object dn2cpp_exception_new produced.
Dn2CppObject* dn2cpp_exception_inner(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    return reinterpret_cast<Dn2CppExceptionObject*>(ex)->inner;
}

// System.Exception.get_HResult: the int32 stored on the prefix — the base
// COR_E_EXCEPTION default seeded at allocation, overwritten by set_HResult from
// each derived ctor. A parameterless Argument*/FileNotFound* exception's
// get_Message override probes this (`_message == null && HResult == COR_E_*`) to
// pick its resource default, so a real value here (not a zero stub)
// makes those default messages exact.
int32_t dn2cpp_exception_hresult(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    return reinterpret_cast<Dn2CppExceptionObject*>(ex)->hresult;
}

// System.Exception.GetBaseException(): walk the inner-exception chain to its
// deepest element and return it; with no inner, return the exception ITSELF
// (real .NET's identity case). INTENTIONAL DIVERGENCE: real
// AggregateException.GetBaseException OVERRIDES this to collapse single-child
// chains — an opaque type here, so it takes this plain inner walk (the same
// documented approximation as its Message).
Dn2CppObject* dn2cpp_exception_get_base(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    Dn2CppObject* back = ex;
    for (Dn2CppObject* cur = reinterpret_cast<Dn2CppExceptionObject*>(ex)->inner;
         cur != nullptr; cur = reinterpret_cast<Dn2CppExceptionObject*>(cur)->inner)
        back = cur;
    return back;
}

// The STORED message: the Dn2CppString* the ctor chain seeded on the prefix, or the
// base Exception.Message fallback text when it is null. This is System.Exception's own
// (non-overridden) get_Message body — what a non-virtual `base.Message` inside a derived
// override resolves to, and what dn2cpp_exception_message falls back to when no override
// is dispatched.
Dn2CppString* dn2cpp_exception_message_stored(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    auto* e = reinterpret_cast<Dn2CppExceptionObject*>(ex);
    if (e->message != nullptr)
        return e->message;
    // Null message: .NET's base Exception.Message fallback. The exact per-derived
    // default text lives in unread BCL resources; dn2cpp always throws with a
    // message, so this base form is the documented approximation for the rare
    // default-constructed case.
    std::string s = "Exception of type '";
    s += (ex->type != nullptr && ex->type->name != nullptr) ? ex->type->name : "System.Exception";
    s += "' was thrown.";
    return dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size()));
}

// Exception.get_Message with virtual dispatch: the .NET semantics of `ex.Message` where
// ex is statically System.Exception. A derived type may OVERRIDE get_Message
// (ArgumentException appends "(Parameter 'x')", FileNotFoundException builds its text
// lazily); when the receiver's vtable carries a real override in the get_Message slot,
// call it. Otherwise — no override, so the slot holds a dispatch trap (the shared
// symbol or a registered per-signature thunk; get_Message returns a pointer, so the
// per-slot struct-return stub is never installed here) or nullptr — fall back to the
// stored message. dn2cpp_exception_get_message_slot is a generated program constant
// (the base declaration's slot; every override shares it).
Dn2CppString* dn2cpp_exception_message(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    int32_t slot = dn2cpp_exception_get_message_slot;
    if (slot >= 0 && ex->type != nullptr && ex->type->vtable != nullptr)
    {
        const void* fn = ex->type->vtable[slot];
        if (fn != nullptr && !dn2cpp_is_vcall_trap(fn))
            return reinterpret_cast<Dn2CppString* (*)(Dn2CppObject*)>(const_cast<void*>(fn))(ex);
    }
    return dn2cpp_exception_message_stored(ex);
}

// ---- stack-trace resolution (function entry -> Dn2CppMethodInfo) ------------
//
// The name table is the reflection method table the program already carries: every
// reachable emitted body has a Dn2CppMethodInfo row whose fnPtr is the emitted C++
// function's address. Sorted by fnPtr it doubles as a reverse index, and a captured
// frame — the function's ENTRY ADDRESS, per dn2cpp_pal_backtrace's contract — resolves
// by EXACT match or not at all. Built lazily on the first RENDER (never at throw)
// under std::call_once, which also sequences it after the type-bind copy in init.
//
// Exact match is load-bearing. A nearest-row-below rule with any tolerance renames
// the throw machinery as whatever managed body precedes it in the image — the linker
// interleaves runtime C++, intrinsic bodies and non-emitted functions between the
// rows, and no tolerance separates the two populations. The table carries no sizes,
// so inside-vs-past is not decidable from it; the frame's own function entry, from
// the platform's unwind data, is what decides it.
//
// Declared degradations:
//   - -O2 inlining erases frames; only what the unwinder sees is reported.
//   - A shared-generic canonical body emits no row of its own; every
//     instantiation's row points at it. The index keeps the lexicographically
//     smallest (declaringType, name, paramCount) row and marks the frame
//     "[shared generic]" — deterministic, but the named instantiation may not
//     be the executing one. Linker ICF collapses the same way.
//   - A frame whose function is not a table row (runtime C++, an intrinsic body, an
//     unemitted BCL body, the PAL, another image) is DROPPED, never misnamed — which
//     is also why the capture's own frames need no skip count.
//   - Interpreted (hot-update patch) frames have no native fnPtr and are
//     invisible; the AOT frames around them still resolve.

struct Dn2CppExcFrameEntry
{
    uintptr_t fn;
    const Dn2CppMethodInfo* mi;
    bool collapsed; // several distinct rows share this fnPtr (shared generics / ICF)
};

static std::vector<Dn2CppExcFrameEntry>& dn2cpp_exc_fn_index()
{
    static std::vector<Dn2CppExcFrameEntry>& index =
        dn2cpp_never_destroyed<std::vector<Dn2CppExcFrameEntry>>();
    static std::once_flag once;
    std::call_once(once, []
    {
        std::vector<Dn2CppExcFrameEntry> rows;
        auto add = [&rows](const Dn2CppMethodInfo* table, int32_t count)
        {
            for (int32_t i = 0; i < count; i++)
                if (table[i].fnPtr != nullptr)
                    rows.push_back({ reinterpret_cast<uintptr_t>(table[i].fnPtr), &table[i], false });
        };
        for (int32_t k = 0; k < dn2cpp_type_registry_count; k++)
        {
            const Dn2CppTypeInfo* ti = dn2cpp_type_registry[k].type;
            add(ti->methods, ti->methodCount);
            add(ti->ctors, ti->ctorCount);
        }
        auto nameOf = [](const Dn2CppMethodInfo* mi, bool decl)
        {
            const char* s = decl
                ? (mi->declaringType != nullptr ? mi->declaringType->name : nullptr)
                : mi->name;
            return s != nullptr ? s : "";
        };
        // Sort by fnPtr, ties by (declaringType, name, paramCount): the FIRST
        // row per fnPtr is then the deterministic representative, independent
        // of emit/registry order.
        std::sort(rows.begin(), rows.end(),
            [&nameOf](const Dn2CppExcFrameEntry& a, const Dn2CppExcFrameEntry& b)
            {
                if (a.fn != b.fn)
                    return a.fn < b.fn;
                int c = std::strcmp(nameOf(a.mi, true), nameOf(b.mi, true));
                if (c != 0)
                    return c < 0;
                c = std::strcmp(nameOf(a.mi, false), nameOf(b.mi, false));
                if (c != 0)
                    return c < 0;
                return a.mi->paramCount < b.mi->paramCount;
            });
        for (size_t i = 0; i < rows.size(); i++)
        {
            if (!index.empty() && index.back().fn == rows[i].fn)
            {
                // A second row naming the same body: keep the representative,
                // note the collapse when it is a genuinely different identity.
                if (std::strcmp(nameOf(index.back().mi, true), nameOf(rows[i].mi, true)) != 0
                    || std::strcmp(nameOf(index.back().mi, false), nameOf(rows[i].mi, false)) != 0)
                    index.back().collapsed = true;
                continue;
            }
            index.push_back(rows[i]);
        }
    });
    return index;
}

// The captured entry either IS a table row's fnPtr — the row's body is the
// frame's function, no approximation involved — or the frame belongs to a
// function the table does not carry and is dropped. The return-address -1
// correction lives with the platform data that needs it (the PAL), not here:
// an entry address is not a PC.
static const Dn2CppExcFrameEntry* dn2cpp_exc_resolve_entry(void* fnEntry)
{
    const std::vector<Dn2CppExcFrameEntry>& index = dn2cpp_exc_fn_index();
    if (index.empty() || fnEntry == nullptr)
        return nullptr;
    uintptr_t p = reinterpret_cast<uintptr_t>(fnEntry);
    size_t lo = 0, hi = index.size();
    while (lo < hi)
    {
        size_t mid = (lo + hi) / 2;
        if (index[mid].fn < p)
            lo = mid + 1;
        else
            hi = mid;
    }
    if (lo == index.size() || index[lo].fn != p)
        return nullptr; // not an emitted table body: drop, never misattribute
    return &index[lo];
}

// One captured frame's rendered text ("Type.Method()[ [shared generic]]",
// no "   at " prefix, no newline), appended to `out`; false — with nothing
// appended — when the entry does not resolve, and the frame is dropped. Shared
// by the exception trace render and the StackTrace(Exception) frame
// materialization so the two cannot drift: a frame the render prints
// and the materialization drops (or renders differently) would be a silent
// inconsistency.
static bool dn2cpp_exc_frame_text(void* fnEntry, std::string& out)
{
    const Dn2CppExcFrameEntry* e = dn2cpp_exc_resolve_entry(fnEntry);
    if (e == nullptr)
        return false;
    out += e->mi->declaringType != nullptr && e->mi->declaringType->name != nullptr
        ? e->mi->declaringType->name : "<unknown>";
    out += '.';
    out += e->mi->name != nullptr ? e->mi->name : "<unknown>";
    out += "()";
    if (e->collapsed)
        out += " [shared generic]";
    return true;
}

// Renders the resolved frames ("   at Type.Method()" per line, '\n'-joined, no
// trailing newline) into `out`; false when nothing was captured or nothing
// resolves — the caller degrades to null / no trace section. Frame format
// divergences from real .NET, declared: no parameter list, no "in file:line"
// (dn2cpp emits no debug map), '\n' on every platform, and a shared-generic
// frame names its representative instantiation with a "[shared generic]" mark.
// A kind-1 (shadow-stack) trace renders its baked names directly — no capture
// resolution, so no frame can fail to resolve or be dropped here.
static bool dn2cpp_exc_trace_render(Dn2CppObject* ex, std::string& out)
{
    if (ex == nullptr || ex->type == nullptr || !dn2cpp_type_is_exception(ex->type))
        return false;
    const Dn2CppExcTrace* trace = static_cast<Dn2CppExceptionObject*>(ex)->trace;
    if (trace == nullptr)
        return false;
    if (trace->kind == 1)
    {
        // Frames the shadow stack's capacity lost at capture were the
        // innermost ones: say so, deterministically, at the head — that is
        // where they would have been. (Also covers count-only mode, where
        // cap == 0 stored nothing and every frame is in `dropped`.)
        bool any = false;
        if (trace->dropped > 0)
        {
            out += "   at <";
            out += std::to_string(trace->dropped);
            out += " innermost frames past shadow-stack capacity>";
            any = true;
        }
        for (int32_t i = 0; i < trace->count; i++)
        {
            if (any)
                out += '\n';
            out += "   at ";
            out += static_cast<const char*>(trace->entries[i]);
            any = true;
        }
        return any;
    }
    if (trace->count <= 0)
        return false;
    bool any = false;
    for (int32_t i = 0; i < trace->count; i++)
    {
        std::string line;
        if (!dn2cpp_exc_frame_text(trace->entries[i], line))
            continue;
        if (any)
            out += '\n';
        out += "   at ";
        out += line;
        any = true;
    }
    return any;
}

// Exception.get_StackTrace: the trace captured when this exception was thrown,
// resolved against the reflection method table — or null for an exception that
// was never thrown (exact: real .NET is null there too), on a target with no
// stack walk (WASM), or when nothing resolves.
Dn2CppString* dn2cpp_exception_stacktrace(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    std::string s;
    if (!dn2cpp_exc_trace_render(ex, s))
        return nullptr;
    return dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size()));
}

// Exception.ToString(): the .NET shape — "FullTypeName: Message" plus the
// nested " ---> inner.ToString()" chain, then the stack-trace section when a
// trace was captured at throw (INTENTIONAL DIVERGENCES, declared at
// dn2cpp_exc_trace_render: frame format differs from real .NET's, and the
// "--- End of inner exception stack trace ---" line real .NET ends the inner
// chain with is not emitted).
Dn2CppString* dn2cpp_exception_tostring(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    std::string s = (ex->type != nullptr && ex->type->name != nullptr)
        ? ex->type->name : "System.Exception";
    s += ": ";
    Dn2CppString* msg = dn2cpp_exception_message(ex);
    int32_t n = dn2cpp_string_to_utf8(msg, nullptr, 0);
    size_t base = s.size();
    s.resize(base + static_cast<size_t>(n));
    dn2cpp_string_to_utf8(msg, &s[base], n);
    if (Dn2CppObject* inner = dn2cpp_exception_inner(ex); inner != nullptr)
    {
        s += "\n ---> ";
        Dn2CppString* it = dn2cpp_exception_tostring(inner);
        int32_t m = dn2cpp_string_to_utf8(it, nullptr, 0);
        base = s.size();
        s.resize(base + static_cast<size_t>(m));
        dn2cpp_string_to_utf8(it, &s[base], m);
    }
    std::string trace;
    if (dn2cpp_exc_trace_render(ex, trace))
    {
        s += '\n';
        s += trace;
    }
    return dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size()));
}

void dn2cpp_report_unhandled_exception(Dn2CppObject* ex)
{
    Dn2CppString* msg = dn2cpp_exception_message(ex);
    int32_t n = dn2cpp_string_to_utf8(msg, nullptr, 0);
    char* buf = static_cast<char*>(dn2cpp_alloc(static_cast<size_t>(n) + 1));
    dn2cpp_string_to_utf8(msg, buf, n);
    buf[n] = '\0';
    std::fprintf(stderr, "Unhandled managed exception: %s: %s\n", ex->type->name, buf);
    // The trace stamped at throw, when one was captured and resolves —
    // same lines Exception.StackTrace renders, best-effort by design.
    std::string trace;
    if (dn2cpp_exc_trace_render(ex, trace))
        std::fprintf(stderr, "%s\n", trace.c_str());
}

// Appends a managed string's UTF-8 bytes to `s` (defined just below, beside its
// other caller).
static void dn2cpp_append_utf8(std::string& s, Dn2CppString* str);

// ---- the host boundary (see the doctrine at the declarations in dn2cpp_core.h)
// Written once during a host's initialization, read from every boundary
// afterwards. A plain pointer rather than an atomic on purpose: the write
// happens on the host's init thread before the host has published anything that
// could call back in (the GDExtension init before ClassDB registration, the
// mono-module entry before it returns the callback table), so no reader exists
// yet at the moment of the store.
static Dn2CppBoundarySink g_boundary_sink = nullptr;

void dn2cpp_set_boundary_exception_sink(Dn2CppBoundarySink sink)
{
    g_boundary_sink = sink;
}

int dn2cpp_boundary_sink_installed()
{
    return g_boundary_sink != nullptr ? 1 : 0;
}

// The sinkless report, and the fallback when a sink itself fails. Deliberately
// prints the exception's ToString rather than only its type name: a boundary
// report is the ONLY trace of a fault the caller decided to survive, and
// "NullReferenceException" without the message names a class of bug rather than
// a bug. The wording keeps the "unhandled managed exception" phrase every
// existing consumer greps for.
static void dn2cpp_report_boundary_tostring_failure(Dn2CppObject* exc, const char* where)
{
    const char* type = exc->type != nullptr && exc->type->name != nullptr
        ? exc->type->name : "<unknown>";
    std::fprintf(stderr,
        "[dn2cpp] unhandled managed exception in %s: %s (its ToString threw)\n",
        where, type);
    std::fflush(stderr);
}

static void dn2cpp_report_boundary_stderr(Dn2CppObject* exc, const char* where)
{
    if (exc == nullptr)
    {
        std::fprintf(stderr, "[dn2cpp] unhandled managed exception in %s: <null>\n", where);
        std::fflush(stderr);
        return;
    }
    std::string text;
    // ToString carries "<FullTypeName>: <Message>", the " ---> " inner chain and
    // the trace captured at throw. It runs managed-adjacent code (a virtual
    // get_Message override is a transpiled body), so a fault in there must not
    // replace the report we are in the middle of making.
    try
    {
        dn2cpp_append_utf8(text, dn2cpp_exception_tostring(exc));
    }
    catch (Dn2CppException& nested)
    {
        dn2cpp_exc_inflight_pop(nested.obj);
        dn2cpp_report_boundary_tostring_failure(exc, where);
        return;
    }
    catch (...)
    {
        dn2cpp_report_boundary_tostring_failure(exc, where);
        return;
    }
    std::fprintf(stderr, "[dn2cpp] unhandled managed exception in %s: %s\n", where, text.c_str());
    std::fflush(stderr);
}

void dn2cpp_report_boundary_exception(Dn2CppObject* exc, const char* where_fmt, ...)
{
    // The exception is being swallowed here — drop the in-flight root first, so
    // a sink that allocates (every one of them does: they format a message) does
    // not keep a dead exception object reachable through the in-flight list.
    if (exc != nullptr)
        dn2cpp_exc_inflight_pop(exc);
    char where[512];
    std::va_list args;
    va_start(args, where_fmt);
    int n = std::vsnprintf(where, sizeof(where), where_fmt, args);
    va_end(args);
    if (n < 0)
        where[0] = '\0'; // an encoding error in the NAME must not lose the report
    Dn2CppBoundarySink sink = g_boundary_sink;
    if (sink != nullptr)
    {
        try
        {
            sink(where, exc);
            return;
        }
        catch (Dn2CppException& nested)
        {
            // The host's own logger faulted. Report both, on the one channel
            // that cannot fail — losing the original here is how a boundary
            // becomes silent.
            dn2cpp_exc_inflight_pop(nested.obj);
            dn2cpp_report_boundary_stderr(nested.obj, "the boundary error sink");
        }
        catch (...)
        {
            std::fprintf(stderr, "[dn2cpp] the boundary error sink raised a non-managed exception\n");
        }
    }
    dn2cpp_report_boundary_stderr(exc, where);
}

// Appends a managed string's UTF-8 bytes to `s` (a no-op for null).
static void dn2cpp_append_utf8(std::string& s, Dn2CppString* str)
{
    if (str == nullptr)
        return;
    int32_t n = dn2cpp_string_to_utf8(str, nullptr, 0);
    size_t base = s.size();
    s.resize(base + static_cast<size_t>(n));
    if (n > 0)
        dn2cpp_string_to_utf8(str, &s[base], n);
}

// The dn2cpp_fail on the POSIX arm below must NEVER become a managed throw:
// Environment.FailFast's whole contract is that it terminates without running catch
// or finally. Here abort IS the requested behaviour, not a fault report.
[[noreturn]] void dn2cpp_environment_failfast(Dn2CppString* message, Dn2CppObject* exception)
{
    // Real .NET opens its stderr report with "Process terminated." and follows it
    // with the message, the (managed) stack trace and the exception's ToString.
    // dn2cpp keeps no managed stack traces, so the trace section is simply absent;
    // the rest is reproduced, and the abort below matches .NET's termination
    // (SIGABRT on POSIX), which is what the exit status a caller can observe is
    // pinned to there.
    std::string s = "Process terminated. ";
    dn2cpp_append_utf8(s, message);
    if (exception != nullptr)
    {
        s += '\n';
        dn2cpp_append_utf8(s, dn2cpp_exception_tostring(exception));
    }
#ifdef _WIN32
    // On Windows real .NET's FailFast is not a crash: the CLR's fatal-error policy
    // hands the process a clean exit of COR_E_FAILFAST (0x80131623), never an SEH
    // exception. Only FailFast diverges this way — dn2cpp_fail's other callers crash
    // on both sides and stay on the abort() arm.
    std::fprintf(stderr, "dn2cpp fatal: %s\n", s.c_str());
    dn2cpp_pal_console_flush();
    std::_Exit(static_cast<int>(0x80131623));
#else
    dn2cpp_fail(s.c_str()); // flushes stdio, then abort()
#endif
}

// ---- managed stack traces (real where a capture exists, degraded elsewhere —
//      see the header for the rationale) ----

// The text a walked-but-unavailable trace/frame reports. It says WHY it is empty:
// returning "" would read as "the stack was empty", which is a silent lie, and a
// human reading a log deserves the reason. Real .NET would print real frames here —
// this is an INTENTIONAL DIVERGENCE, which is why its gate is a freeze gate.
static const char* const k_stacktrace_unavailable = "   at <stack trace unavailable in AOT>\n";
static const char* const k_stackframe_unavailable = "<stack frame unavailable in AOT>\n";

// The two handles' public field surface, real .NET's exactly (the hand-writing
// argument is at dn2cpp_primflds_bool in dn2cpp_typeinfo.cpp). Both are consts whose
// value is a fact about the CLR, so the degraded trace model does not reach them.
static Dn2CppObject* dn2cpp_ownfld_stackframe_OFFSET_UNKNOWN(Dn2CppObject*)
{ int32_t v = -1; return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_ownflds_stackframe[] = {
    { "OFFSET_UNKNOWN", &dn2cpp_stackframe_type, &dn2cpp_int32_type,
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_ownfld_stackframe_OFFSET_UNKNOWN, nullptr, nullptr, 0, 0x8056, 0 },
};
static Dn2CppObject* dn2cpp_ownfld_stacktrace_METHODS_TO_SKIP(Dn2CppObject*)
{ int32_t v = 0; return dn2cpp_box(&dn2cpp_int32_type, &v, sizeof(v)); }
static const Dn2CppFieldInfo dn2cpp_ownflds_stacktrace[] = {
    { "METHODS_TO_SKIP", &dn2cpp_stacktrace_type, &dn2cpp_int32_type,
      DN2CPP_FLDA_STATIC | DN2CPP_FLDA_PUBLIC | DN2CPP_FLDA_LITERAL,
      dn2cpp_ownfld_stacktrace_METHODS_TO_SKIP, nullptr, nullptr, 0, 0x8056, 0 },
};

extern const Dn2CppType dn2cpp_stackframe_type_obj;
const Dn2CppTypeInfo dn2cpp_stackframe_type =
    dn2cpp_ti_with_typeobject({ "System.Diagnostics.StackFrame", nullptr, (int32_t)sizeof(Dn2CppStackFrame), nullptr, nullptr, 0,
                                dn2cpp_stackframe_tostring, nullptr, nullptr, 0,
                                dn2cpp_ownflds_stackframe, 1 },
                              &dn2cpp_stackframe_type_obj);
const Dn2CppType dn2cpp_stackframe_type_obj = { { &dn2cpp_type_type }, &dn2cpp_stackframe_type };

extern const Dn2CppType dn2cpp_stacktrace_type_obj;
const Dn2CppTypeInfo dn2cpp_stacktrace_type =
    dn2cpp_ti_with_typeobject({ "System.Diagnostics.StackTrace", nullptr, (int32_t)sizeof(Dn2CppStackTrace), nullptr, nullptr, 0,
                                dn2cpp_stacktrace_tostring, nullptr, nullptr, 0,
                                dn2cpp_ownflds_stacktrace, 1 },
                              &dn2cpp_stacktrace_type_obj);
const Dn2CppType dn2cpp_stacktrace_type_obj = { { &dn2cpp_type_type }, &dn2cpp_stacktrace_type };

Dn2CppStackFrame* dn2cpp_stackframe_new(Dn2CppString* fileName, int32_t line, int32_t column)
{
    auto* f = static_cast<Dn2CppStackFrame*>(dn2cpp_alloc(sizeof(Dn2CppStackFrame)));
    f->type = &dn2cpp_stackframe_type;
    dn2cpp_gc_store_ref(&f->fileName, fileName);
    f->lineNumber = line;
    f->columnNumber = column;
    f->methodDesc = nullptr; // only a materialized capture (below) fills it
    return f;
}

// One materialized frame of a captured trace: the rendered method text
// and nothing else — a captured frame never carries a fileName (dn2cpp emits no
// debug map), which is what keeps the render arms below exclusive.
static Dn2CppStackFrame* dn2cpp_stackframe_of_text(const char* text, size_t len)
{
    Dn2CppStackFrame* f = dn2cpp_stackframe_new(nullptr, 0, 0);
    dn2cpp_gc_store_ref(&f->methodDesc,
        dn2cpp_string_from_utf8(text, static_cast<int32_t>(len)));
    return f;
}

// One rendered frame line, without the "   at " prefix the trace adds.
static void dn2cpp_stackframe_render(std::string& s, Dn2CppStackFrame* f)
{
    // A frame materialized from a shadow-stack / exception capture carries its
    // rendered method text (and never a fileName, so this arm and the
    // caller-supplied one below cannot both apply). The text matches the
    // exception render's frame lines byte-for-byte — same source strings.
    if (f != nullptr && f->methodDesc != nullptr)
    {
        dn2cpp_append_utf8(s, f->methodDesc);
        s += '\n';
        return;
    }
    if (f == nullptr || f->fileName == nullptr)
    {
        s += k_stackframe_unavailable;
        return;
    }
    // A caller-supplied (file, line) frame is real information, so report it. The
    // method is the part AOT cannot recover, and the text says so rather than
    // inventing a name.
    s += "<unknown method> in ";
    dn2cpp_append_utf8(s, f->fileName);
    s += ":line ";
    s += std::to_string(static_cast<long long>(f->lineNumber));
    s += '\n';
}

Dn2CppString* dn2cpp_stackframe_tostring(Dn2CppObject* sf)
{
    std::string s;
    dn2cpp_stackframe_render(s, reinterpret_cast<Dn2CppStackFrame*>(sf));
    return dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size()));
}

// Null-tolerant by design — see the header. A null frame is one WE handed out, and it
// reports exactly what a walked frame would: no file, no line, no column.
Dn2CppString* dn2cpp_stackframe_file_name(Dn2CppStackFrame* sf)
{
    return sf != nullptr ? sf->fileName : nullptr;
}

int32_t dn2cpp_stackframe_line_number(Dn2CppStackFrame* sf)
{
    return sf != nullptr ? sf->lineNumber : 0;
}

int32_t dn2cpp_stackframe_column_number(Dn2CppStackFrame* sf)
{
    return sf != nullptr ? sf->columnNumber : 0;
}

// Wraps a frames array into a trace object — every Dn2CppStackTrace is built
// here, so `dropped` cannot be left uninitialized by a new constructor.
static Dn2CppStackTrace* dn2cpp_stacktrace_wrap(Dn2CppArrayRef* frames, int32_t dropped)
{
    auto* st = static_cast<Dn2CppStackTrace*>(dn2cpp_alloc(sizeof(Dn2CppStackTrace)));
    st->type = &dn2cpp_stacktrace_type;
    dn2cpp_gc_store_ref(&st->frames, frames);
    st->dropped = dropped;
    return st;
}

// The zero-frame trace every degraded answer shares: a real object, no frames,
// nothing dropped — the degraded zero-frame shape, bit-for-bit.
static Dn2CppStackTrace* dn2cpp_stacktrace_empty(const Dn2CppTypeInfo* frameArrTi)
{
    return dn2cpp_stacktrace_wrap(dn2cpp_array_empty_ref(frameArrTi), 0);
}

// The current-stack capture — contract in the header. skipFrames consumes the
// innermost end of the LOGICAL stack: the logical stack is [depth - cap
// overflowed frames, counted but never stored] + the stored frames, so skip
// eats the overflow count first, then stored frames; whatever overflow
// remains un-skipped is the trace's `dropped`. A negative skip clamps to 0
// and a skip past the depth yields an empty trace (real .NET throws
// ArgumentOutOfRangeException on negative — declared divergence, this family
// never throws). The innermost entry is the body that executed the `newobj`:
// its guard ran at its prologue and this runtime ctor pushes no frame — the
// same window start as real .NET's StackTrace().
Dn2CppStackTrace* dn2cpp_stacktrace_new(const Dn2CppTypeInfo* frameArrTi, int32_t skipFrames)
{
    // No live shadow frames (flag-off, or a thread that has run no guarded
    // body): the degraded zero-frame object, unchanged.
    const Dn2CppShadowStack* ss = dn2cpp_shadow_tls;
    if (ss == nullptr || ss->depth <= 0)
        return dn2cpp_stacktrace_empty(frameArrTi);
    int32_t skip = skipFrames < 0 ? 0 : skipFrames;
    int32_t stored = ss->depth < ss->cap ? ss->depth : ss->cap;
    int32_t overflowed = ss->depth - stored; // innermost, counted but never stored
    int32_t skipOverflow = skip < overflowed ? skip : overflowed;
    int32_t skipStored = skip - skipOverflow;
    if (skipStored > stored)
        skipStored = stored;
    int32_t n = stored - skipStored;
    Dn2CppArrayRef* frames = n > 0 ? dn2cpp_newarr_ref_t(n, frameArrTi)
                                   : dn2cpp_array_empty_ref(frameArrTi);
    for (int32_t i = 0; i < n; i++)
    {
        // ss->frames[] is push order (outermost first); mirror the stamp
        // loop's reversal so entry 0 is the innermost un-skipped frame.
        const char* name = ss->frames[stored - 1 - skipStored - i];
        dn2cpp_gc_store_ref(&frames->data[i],
            reinterpret_cast<Dn2CppObject*>(dn2cpp_stackframe_of_text(name, std::strlen(name))));
    }
    return dn2cpp_stacktrace_wrap(frames, overflowed - skipOverflow);
}

// new StackTrace(Exception[, skipFrames]) — contract in the header. Runs
// UNCONDITIONALLY of --shadow-stack: the stamped trace exists flag-off too
// (kind 0) and ex.StackTrace already renders it there, so answering
// "unavailable" beside it would be the silent inconsistency §4-B forbids.
Dn2CppStackTrace* dn2cpp_stacktrace_of_exception(const Dn2CppTypeInfo* frameArrTi,
                                                 Dn2CppObject* ex, int32_t skipFrames)
{
    // Same shape test as the stamp: null, or a base chain that never reaches
    // System.Exception (no trace slot to read), degrades to zero frames (real
    // .NET throws ArgumentNullException on null — declared divergence). An
    // UNTHROWN exception's trace is null: zero frames too (real .NET reports
    // an empty trace there as well).
    if (ex == nullptr || ex->type == nullptr || !dn2cpp_type_is_exception(ex->type))
        return dn2cpp_stacktrace_empty(frameArrTi);
    const Dn2CppExcTrace* trace = static_cast<Dn2CppExceptionObject*>(ex)->trace;
    if (trace == nullptr)
        return dn2cpp_stacktrace_empty(frameArrTi);
    int32_t skip = skipFrames < 0 ? 0 : skipFrames;
    if (trace->kind == 1)
    {
        // Shadow names, innermost first. skip consumes the logical innermost
        // end: the capacity-dropped count first, then the stored entries.
        int32_t skipDropped = skip < trace->dropped ? skip : trace->dropped;
        int32_t skipEntries = skip - skipDropped;
        if (skipEntries > trace->count)
            skipEntries = trace->count;
        int32_t n = trace->count - skipEntries;
        Dn2CppArrayRef* frames = n > 0 ? dn2cpp_newarr_ref_t(n, frameArrTi)
                                       : dn2cpp_array_empty_ref(frameArrTi);
        for (int32_t i = 0; i < n; i++)
        {
            const auto* name = static_cast<const char*>(trace->entries[skipEntries + i]);
            dn2cpp_gc_store_ref(&frames->data[i],
                reinterpret_cast<Dn2CppObject*>(dn2cpp_stackframe_of_text(name, std::strlen(name))));
        }
        return dn2cpp_stacktrace_wrap(frames, trace->dropped - skipDropped);
    }
    // Kind 0: resolve each entry through the same helper the render uses, dropping
    // what does not resolve exactly as the render drops it — so skip counts only
    // frames a caller could observe (FrameCount and ToString stay consistent
    // under skip), never the capture machinery's own unresolvable entries.
    std::vector<std::string> lines;
    for (int32_t i = 0; i < trace->count; i++)
    {
        std::string line;
        if (dn2cpp_exc_frame_text(trace->entries[i], line))
            lines.push_back(std::move(line));
    }
    size_t start = static_cast<size_t>(skip) < lines.size()
        ? static_cast<size_t>(skip) : lines.size();
    int32_t n = static_cast<int32_t>(lines.size() - start);
    Dn2CppArrayRef* frames = n > 0 ? dn2cpp_newarr_ref_t(n, frameArrTi)
                                   : dn2cpp_array_empty_ref(frameArrTi);
    for (int32_t i = 0; i < n; i++)
    {
        const std::string& line = lines[start + static_cast<size_t>(i)];
        dn2cpp_gc_store_ref(&frames->data[i],
            reinterpret_cast<Dn2CppObject*>(dn2cpp_stackframe_of_text(line.c_str(), line.size())));
    }
    return dn2cpp_stacktrace_wrap(frames, 0);
}

Dn2CppStackTrace* dn2cpp_stacktrace_of_frame(const Dn2CppTypeInfo* frameArrTi, Dn2CppObject* frame)
{
    Dn2CppArrayRef* frames = dn2cpp_newarr_ref_t(1, frameArrTi);
    dn2cpp_gc_store_ref(&frames->data[0], frame);
    return dn2cpp_stacktrace_wrap(frames, 0);
}

int32_t dn2cpp_stacktrace_frame_count(Dn2CppStackTrace* st)
{
    if (st == nullptr)
        dn2cpp_throw_null_reference();
    return st->frames->length;
}

Dn2CppArrayRef* dn2cpp_stacktrace_get_frames(Dn2CppStackTrace* st)
{
    if (st == nullptr)
        dn2cpp_throw_null_reference();
    return st->frames;
}

Dn2CppObject* dn2cpp_stacktrace_get_frame(Dn2CppStackTrace* st, int32_t index)
{
    if (st == nullptr)
        dn2cpp_throw_null_reference();
    // Out of range is null, not a throw — that is real .NET's contract, and it is
    // what makes GetFrame(0) on an empty trace the well-defined answer every caller
    // of a degraded StackTrace ends up taking.
    if (index < 0 || index >= st->frames->length)
        return nullptr;
    return st->frames->data[index];
}

Dn2CppString* dn2cpp_stacktrace_tostring(Dn2CppObject* obj)
{
    auto* st = reinterpret_cast<Dn2CppStackTrace*>(obj);
    // "Unavailable" is the answer ONLY when there is genuinely nothing: no
    // frames AND nothing dropped. A trace whose every frame fell past the
    // shadow-stack capacity still says what happened via the marker below.
    if (st == nullptr || (st->frames->length == 0 && st->dropped == 0))
        return dn2cpp_string_from_utf8(k_stacktrace_unavailable,
                                       static_cast<int32_t>(std::strlen(k_stacktrace_unavailable)));
    std::string s;
    if (st->dropped > 0)
    {
        // The same capacity marker the exception-side kind-1 render prints, at
        // the head — where the lost innermost frames would have been. A LINE,
        // never a frame: FrameCount/GetFrames/GetFrame cover materialized
        // frames only.
        s += "   at <";
        s += std::to_string(st->dropped);
        s += " innermost frames past shadow-stack capacity>";
        s += '\n';
    }
    for (int32_t i = 0; i < st->frames->length; i++)
    {
        s += "   at ";
        dn2cpp_stackframe_render(s, reinterpret_cast<Dn2CppStackFrame*>(st->frames->data[i]));
    }
    return dn2cpp_string_from_utf8(s.c_str(), static_cast<int32_t>(s.size()));
}

// A managed AggregateException: the REAL exception prefix (inherited, exactly
// like every emitted derived exception struct) plus a trailing slot holding
// the InnerExceptions array (the aggregated inner exceptions, in a
// deterministic order chosen by the producer). Inheritance is load-bearing,
// not style: a hand-mirrored prefix drifts — grow the prefix by one slot (the
// trace slot) and the mirror silently places
// `innerExceptions` at the new slot's offset, so throwing the aggregate stamps
// a trace pointer OVER the inner-exceptions array (a SIGSEGV in the parallel
// gates). Deriving from Dn2CppExceptionObject makes the layout follow the
// prefix forever. This is the reusable base both the Parallel
// exception-aggregation path and (later) Task sync-wait aggregation build.
struct Dn2CppAggregateExceptionObject : Dn2CppExceptionObject
{
    Dn2CppArrayRef* innerExceptions; // the aggregated inner exceptions
    // The managed ReadOnlyCollection<Exception> the get_InnerExceptions lowering builds
    // over `innerExceptions`, memoized because real .NET's InnerExceptions is a stored
    // FIELD: two reads are reference-equal. Null until the first read; GC alloc
    // zero-fills, so the construction sites need no initialization.
    Dn2CppObject* innerWrapper;
};

// Build an AggregateException wrapping `inner` (an Exception[]). InnerException is the
// first element (matching real .NET's AggregateException(Exception[]) ctor), or null
// when empty. `inner` stays GC-reachable through the returned object's field.
// The message is composed here the way real .NET's get_Message does — the base text
// plus each inner's Message in parentheses ("One or more errors occurred. (boom)") —
// eagerly rather than lazily, because a settled exception's message is immutable and
// the stored-message slot is what every Message read funnels to. The inner Message
// reads go through the virtual funnel, so an override is honored.
Dn2CppObject* dn2cpp_aggregate_exception_new(Dn2CppArrayRef* inner)
{
    std::string msg = "One or more errors occurred.";
    if (inner != nullptr)
    {
        for (int32_t i = 0; i < inner->length; i++)
        {
            if (inner->data[i] == nullptr)
                continue;
            msg += " (";
            dn2cpp_append_utf8(msg, dn2cpp_exception_message(inner->data[i]));
            msg += ")";
        }
    }
    Dn2CppString* msgStr = dn2cpp_string_from_utf8(msg.c_str(), static_cast<int32_t>(msg.size()));
    auto* e = static_cast<Dn2CppAggregateExceptionObject*>(dn2cpp_alloc(sizeof(Dn2CppAggregateExceptionObject)));
    e->type = &dn2cpp_aggregate_exception_type;
    dn2cpp_gc_store_ref(&e->message, msgStr);
    dn2cpp_gc_store_ref(&e->inner,
                        (inner != nullptr && inner->length > 0) ? inner->data[0] : nullptr);
    e->hresult = static_cast<int32_t>(0x80131500); // base default; get_HResult reads the shared prefix slot
    // `inner` is whatever its caller allocated, and the four runtime callers
    // (dn2cpp_task_block_wait, Task.WaitAll, dn2cpp_task_exception, the Parallel fault
    // aggregation) all use the untyped allocator, so it arrives tagged System.Object[].
    // That is not observable: get_InnerExceptions is the only door from managed code to
    // this array and it stamps the precise ti_arr_System_Exception before handing it
    // back. Re-tagging at allocation would need a handle the runtime cannot name —
    // dn2cpp_exception_type is the RUNTIME's Exception, not the transpiled CoreLib's.
    // A reader that bypasses the getter is what would make this a defect.
    dn2cpp_gc_store_ref(&e->innerExceptions, inner); // trace stays null (GC alloc zero-fills) until the throw stamps it
    return e;
}

// The memoized ReadOnlyCollection<Exception> for get_InnerExceptions, and its setter.
// The WRAPPER is built by the emit arm (it is managed BCL IL, which the runtime cannot
// construct); the runtime owns only the slot it is cached in, because the slot has to
// live on the exception object and outlive the call. Real .NET's InnerExceptions is a
// field, so two reads are reference-equal.
Dn2CppObject* dn2cpp_aggregate_inner_wrapper(Dn2CppObject* ex)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    return reinterpret_cast<Dn2CppAggregateExceptionObject*>(ex)->innerWrapper;
}

void dn2cpp_aggregate_set_inner_wrapper(Dn2CppObject* ex, Dn2CppObject* wrapper)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    dn2cpp_gc_store_ref(&reinterpret_cast<Dn2CppAggregateExceptionObject*>(ex)->innerWrapper,
                        wrapper);
}

// AggregateException.get_InnerExceptions: the stored Exception[] array. `arrTi` is the
// caller-supplied precise per-element handle (ti_arr_System_Exception, which carries
// the SZArray interface-dispatch map); stamping it lets the result resolve
// IReadOnlyList<Exception>/IEnumerable<Exception> member calls on the array itself.
// This is now the wrapper's BACKING list rather than the property's answer — the
// property hands back the ReadOnlyCollection above.
Dn2CppArrayRef* dn2cpp_aggregate_inner_exceptions(Dn2CppObject* ex, const Dn2CppTypeInfo* arrTi)
{
    if (ex == nullptr)
        dn2cpp_throw_null_reference();
    auto* a = reinterpret_cast<Dn2CppAggregateExceptionObject*>(ex)->innerExceptions;
    if (a == nullptr)
    {
        // A parameterless `new AggregateException()` stores no array, but real .NET's
        // InnerExceptions is an EMPTY collection there, never null — and the
        // ReadOnlyCollection ctor wrapping it rejects a null list. Hand back a real
        // zero-length Exception[] so the empty case reads .Count == 0 like .NET.
        a = dn2cpp_newarr_ref_t(0, arrTi);
        dn2cpp_gc_store_ref(
            &reinterpret_cast<Dn2CppAggregateExceptionObject*>(ex)->innerExceptions, a);
        return a;
    }
    if (arrTi != nullptr)
        a->type = arrTi;
    return a;
}
