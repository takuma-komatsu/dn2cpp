//
// Table-driven GDExtension bridge: registers transpiled C# static methods
// with the Godot ClassDB. Entry symbol: dn2cpp_gdext_init.

#include "dn2cpp_godot.h"
#include "gdextension_interface.h"

#include <vector>
#include <array>
#include <string>
#include <atomic>
#include <mutex>

// The export boundary decodes a bare Variant argument straight into a
// Dn2CppValue slot (and the gw_ thunks copy the shim struct in/out of one), so
// the decoded form must never outgrow the slot.
static_assert(sizeof(Dn2CppGodotVariant) <= sizeof(Dn2CppValue),
    "Dn2CppGodotVariant must fit the Dn2CppValue export-boundary slot");

namespace
{
    GDExtensionClassLibraryPtr g_library = nullptr;

    // Interface functions resolved through get_proc_address.
    GDExtensionInterfaceClassdbRegisterExtensionClassSignal gd_register_signal = nullptr;
    GDExtensionInterfaceClassdbRegisterExtensionClassProperty gd_register_property = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_string_name = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_object = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_object_from_variant = nullptr; // Object payload in a Variant
    GDExtensionInterfaceObjectMethodBindCall gd_method_bind_call = nullptr;
    GDExtensionPtrDestructor gd_string_name_destroy = nullptr;
    GDExtensionInterfaceVariantGetPtrConstructor gd_variant_get_ptr_constructor = nullptr;
    GDExtensionInterfaceStringNameNewWithLatin1Chars gd_string_name_new = nullptr;
    GDExtensionInterfaceStringNewWithUtf8CharsAndLen gd_string_new_utf8 = nullptr;
    GDExtensionInterfaceStringToUtf8Chars gd_string_to_utf8 = nullptr;
    GDExtensionInterfaceGetVariantFromTypeConstructor gd_get_variant_from_type = nullptr;
    GDExtensionInterfaceGetVariantToTypeConstructor gd_get_variant_to_type = nullptr;
    GDExtensionInterfaceVariantGetPtrDestructor gd_variant_get_ptr_destructor = nullptr;
    GDExtensionInterfaceClassdbRegisterExtensionClass4 gd_register_class = nullptr;
    GDExtensionInterfaceClassdbRegisterExtensionClassMethod gd_register_method = nullptr;
    GDExtensionInterfaceClassdbUnregisterExtensionClass gd_unregister_class = nullptr;
    GDExtensionInterfaceClassdbConstructObject2 gd_construct_object = nullptr;
    GDExtensionInterfaceObjectSetInstance gd_object_set_instance = nullptr;
    GDExtensionInterfaceObjectDestroy gd_object_destroy = nullptr;
    GDExtensionInterfaceVariantGetPtrUtilityFunction gd_get_utility = nullptr;
    GDExtensionInterfaceVariantDestroy gd_variant_destroy = nullptr;
    GDExtensionPtrUtilityFunction gd_utility_print = nullptr;
    GDExtensionInterfaceGlobalGetSingleton gd_global_get_singleton = nullptr;
    GDExtensionInterfaceClassdbGetMethodBind gd_get_method_bind = nullptr;
    GDExtensionInterfaceObjectMethodBindPtrcall gd_method_bind_ptrcall = nullptr;
    GDExtensionInterfaceVariantGetPtrBuiltinMethod gd_variant_get_ptr_builtin_method = nullptr;
    GDExtensionInterfaceVariantGetType gd_variant_get_type = nullptr;
    GDExtensionInterfaceObjectCastTo gd_object_cast_to = nullptr;
    GDExtensionInterfaceClassdbGetClassTag gd_classdb_get_class_tag = nullptr;
    GDExtensionInterfaceCallableCustomCreate2 gd_callable_custom_create2 = nullptr;
    GDExtensionInterfaceCallableCustomGetUserdata gd_callable_custom_get_userdata = nullptr;
    GDExtensionInterfaceVariantCall gd_variant_call = nullptr;
    GDExtensionInterfacePrintError gd_print_error = nullptr;

    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_bool = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_int = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_float = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_string = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_vector2 = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_vector3 = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_vector4 = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_color = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_transform2d = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_basis = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_transform3d = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_projection = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_quaternion = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_plane = nullptr;
    GDExtensionVariantFromTypeConstructorFunc gd_variant_from_aabb = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_bool_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_int_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_float_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_string_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_vector2_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_vector3_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_vector4_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_color_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_transform2d_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_basis_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_transform3d_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_projection_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_quaternion_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_plane_from_variant = nullptr;
    GDExtensionTypeFromVariantConstructorFunc gd_aabb_from_variant = nullptr;
    GDExtensionPtrDestructor gd_string_destroy = nullptr;

    // Packed-array element accessors. Each returns a pointer to
    // element [i]; for POD types the storage is contiguous, so &elem[0] is the base.
    GDExtensionInterfacePackedByteArrayOperatorIndex gd_pba_index = nullptr;
    GDExtensionInterfacePackedInt32ArrayOperatorIndex gd_pi32a_index = nullptr;
    GDExtensionInterfacePackedInt64ArrayOperatorIndex gd_pi64a_index = nullptr;
    GDExtensionInterfacePackedFloat32ArrayOperatorIndex gd_pf32a_index = nullptr;
    GDExtensionInterfacePackedFloat64ArrayOperatorIndex gd_pf64a_index = nullptr;
    GDExtensionInterfacePackedStringArrayOperatorIndex gd_psa_index = nullptr;
    GDExtensionInterfacePackedVector2ArrayOperatorIndex gd_pv2a_index = nullptr;
    GDExtensionInterfacePackedVector3ArrayOperatorIndex gd_pv3a_index = nullptr;
    GDExtensionInterfacePackedVector4ArrayOperatorIndex gd_pv4a_index = nullptr;
    GDExtensionInterfacePackedColorArrayOperatorIndex gd_pca_index = nullptr;
    GDExtensionInterfaceArrayOperatorIndex gd_array_index = nullptr; // heterogeneous Array element access
    GDExtensionInterfaceDictionaryOperatorIndex gd_dict_index = nullptr; // Dictionary key->value slot (inserts a nil for a missing key)
    GDExtensionInterfaceDictionaryOperatorIndexConst gd_dict_index_const = nullptr; // non-mutating read (null for a missing key)

    // Godot core types are opaque pointer-sized handles in extension code.
    struct GdStringName { void* opaque = nullptr; };
    struct GdString { void* opaque = nullptr; };
    struct GdNodePath { void* opaque = nullptr; };

    GdStringName MakeStringName(const char* text)
    {
        GdStringName sn;
        gd_string_name_new(&sn, text, /*is_static*/ 0);
        return sn;
    }

    GDExtensionVariantType ToVariantType(int32_t t)
    {
        switch (t)
        {
            case DN2CPP_GD_BOOL: return GDEXTENSION_VARIANT_TYPE_BOOL;
            case DN2CPP_GD_INT: return GDEXTENSION_VARIANT_TYPE_INT;
            case DN2CPP_GD_FLOAT: return GDEXTENSION_VARIANT_TYPE_FLOAT;
            case DN2CPP_GD_STRING: return GDEXTENSION_VARIANT_TYPE_STRING;
            case DN2CPP_GD_VECTOR2: return GDEXTENSION_VARIANT_TYPE_VECTOR2;
            case DN2CPP_GD_VECTOR3: return GDEXTENSION_VARIANT_TYPE_VECTOR3;
            case DN2CPP_GD_VECTOR4: return GDEXTENSION_VARIANT_TYPE_VECTOR4;
            case DN2CPP_GD_COLOR: return GDEXTENSION_VARIANT_TYPE_COLOR;
            case DN2CPP_GD_TRANSFORM2D: return GDEXTENSION_VARIANT_TYPE_TRANSFORM2D;
            case DN2CPP_GD_BASIS: return GDEXTENSION_VARIANT_TYPE_BASIS;
            case DN2CPP_GD_TRANSFORM3D: return GDEXTENSION_VARIANT_TYPE_TRANSFORM3D;
            case DN2CPP_GD_PROJECTION: return GDEXTENSION_VARIANT_TYPE_PROJECTION;
            case DN2CPP_GD_QUATERNION: return GDEXTENSION_VARIANT_TYPE_QUATERNION;
            case DN2CPP_GD_PLANE: return GDEXTENSION_VARIANT_TYPE_PLANE;
            case DN2CPP_GD_AABB: return GDEXTENSION_VARIANT_TYPE_AABB;
            case DN2CPP_GD_PACKED_BYTE: return GDEXTENSION_VARIANT_TYPE_PACKED_BYTE_ARRAY;
            case DN2CPP_GD_PACKED_INT32: return GDEXTENSION_VARIANT_TYPE_PACKED_INT32_ARRAY;
            case DN2CPP_GD_PACKED_INT64: return GDEXTENSION_VARIANT_TYPE_PACKED_INT64_ARRAY;
            case DN2CPP_GD_PACKED_FLOAT32: return GDEXTENSION_VARIANT_TYPE_PACKED_FLOAT32_ARRAY;
            case DN2CPP_GD_PACKED_FLOAT64: return GDEXTENSION_VARIANT_TYPE_PACKED_FLOAT64_ARRAY;
            case DN2CPP_GD_PACKED_STRING: return GDEXTENSION_VARIANT_TYPE_PACKED_STRING_ARRAY;
            case DN2CPP_GD_PACKED_VECTOR2: return GDEXTENSION_VARIANT_TYPE_PACKED_VECTOR2_ARRAY;
            case DN2CPP_GD_PACKED_VECTOR3: return GDEXTENSION_VARIANT_TYPE_PACKED_VECTOR3_ARRAY;
            case DN2CPP_GD_PACKED_VECTOR4: return GDEXTENSION_VARIANT_TYPE_PACKED_VECTOR4_ARRAY;
            case DN2CPP_GD_PACKED_COLOR: return GDEXTENSION_VARIANT_TYPE_PACKED_COLOR_ARRAY;
            case DN2CPP_GD_ARRAY: return GDEXTENSION_VARIANT_TYPE_ARRAY;
            case DN2CPP_GD_DICTIONARY: return GDEXTENSION_VARIANT_TYPE_DICTIONARY;
            case DN2CPP_GD_GODOT_ARRAY: return GDEXTENSION_VARIANT_TYPE_ARRAY;
            case DN2CPP_GD_CALLABLE: return GDEXTENSION_VARIANT_TYPE_CALLABLE;
            case DN2CPP_GD_SIGNAL: return GDEXTENSION_VARIANT_TYPE_SIGNAL;
            default: return GDEXTENSION_VARIANT_TYPE_NIL;
        }
    }

    // Packed-array marshalling. Forward-declared here (used by the
    // method-invoke thunks below) and defined after ResolveBuiltinMethod, which they
    // need for the `size`/`resize` builtin-method calls. `tag` is a DN2CPP_GD_PACKED_*.
    // - PackedToManaged: a native packed value (ptrcall arg) -> a new Dn2CppArray*.
    // - PackedVariantToManaged: a Variant carrying a packed array (call arg) -> array.
    // - ManagedToPackedValue: build a native packed value into `out` (ptrcall return).
    // - ManagedToPackedVariant: build a Variant carrying a packed array (call return).
    void* PackedToManaged(int32_t tag, const void* packed);
    void* PackedVariantToManaged(int32_t tag, const void* variant);
    void  ManagedToPackedValue(int32_t tag, void* outPacked, void* arr);
    void  ManagedToPackedVariant(int32_t tag, void* r_variant, void* arr);

    // Heterogeneous Array <-> managed Godot.Variant[], same four-way
    // split (native value / Variant carrier, in / out). Elements are engine Variants
    // decoded/encoded to the shim Variant struct via Decode/EncodeVariant.
    void* ArrayToManaged(const void* arrayValue);
    void* ArrayVariantToManaged(const void* variant);
    void  ManagedToArrayValue(void* outArray, void* arr);
    void  ManagedToArrayVariant(void* r_variant, void* arr);

    // General engine Variant <-> shim Variant struct. Used by
    // the Array marshalling above and the bare-Variant export boundary;
    // defined further down but referenced by the call thunks above them.
    void DecodeVariant(const void* v, Dn2CppGodotVariant* out);
    void EncodeVariant(const Dn2CppGodotVariant* in, void* slot);

    // Engine Callable/Signal (16-byte values) <-> the runtime PODs mirroring
    // the field-carrying shim structs. Build constructs into an
    // uninitialized/zeroed 16-byte slot (a delegate-backed Callable becomes a
    // custom engine callable); Decode reads a live engine value (one of our
    // custom callables recovers its pinned userdata POD — delegates intact).
    // Defined with the Callable bridge below (they need the Variant codec).
    void BuildCallableValue(void* out16, const Dn2CppGodotCallableShim* shim);
    void DecodeCallableValue(const void* cv, Dn2CppGodotCallableShim* out);
    void BuildSignalValue(void* out16, const Dn2CppGodotSignalShim* shim);
    void DecodeSignalValue(const void* sv, Dn2CppGodotSignalShim* out);

    // Engine container <-> the engine-backed Godot.Collections wrapper class
    // (tag = DN2CPP_GD_GODOT_ARRAY or DN2CPP_GD_DICTIONARY), same four-way
    // split (native value / Variant carrier, in / out) as the Array helpers.
    // The managed side is a wrapper object pointer; the engine container is
    // shared (reference-counted), never element-copied. The building blocks
    // (slot alloc, wrapper alloc, handle read) are shared with the Variant
    // codec's container payload kinds; all defined with the container bridge.
    void* ContainerToManaged(int32_t tag, const void* value);
    void* ContainerVariantToManaged(int32_t tag, const void* variant);
    void  ManagedToContainerValue(int32_t tag, void* outValue, void* wrapper);
    void  ManagedToContainerVariant(int32_t tag, void* r_variant, void* wrapper);
    GDExtensionVariantType ContainerVt(int32_t tag);
    void* ContainerHandleOf(const Dn2CppObject* wrapper);
    void* NewContainerSlot();
    Dn2CppObject* MakeContainerWrapper(const Dn2CppTypeInfo* ti, void* valueSlot);
    const Dn2CppTypeInfo* ContainerWrapperTi(int32_t tag);

    // Byte size of the large by-value types in their `big[16]` slot (Godot's native
    // ptrcall layout, single-precision). Used for the generic ptrcall memcpy.
    int32_t BigTypeByteSize(int32_t t)
    {
        switch (t)
        {
            case DN2CPP_GD_TRANSFORM2D: return 6 * (int32_t)sizeof(float);
            case DN2CPP_GD_BASIS: return 9 * (int32_t)sizeof(float);
            case DN2CPP_GD_TRANSFORM3D: return 12 * (int32_t)sizeof(float);
            case DN2CPP_GD_PROJECTION: return 16 * (int32_t)sizeof(float);
            case DN2CPP_GD_QUATERNION: return 4 * (int32_t)sizeof(float);
            case DN2CPP_GD_PLANE: return 4 * (int32_t)sizeof(float);
            case DN2CPP_GD_AABB: return 6 * (int32_t)sizeof(float);
            default: return 0;
        }
    }

    Dn2CppString* ManagedStringFromGd(const GdString* gs)
    {
        GDExtensionInt len = gd_string_to_utf8(gs, nullptr, 0);
        char* buf = static_cast<char*>(dn2cpp_alloc(static_cast<size_t>(len) + 1));
        gd_string_to_utf8(gs, buf, len);
        buf[len] = '\0';
        // Managed strings are UTF-16; decode the engine's UTF-8 bytes.
        return dn2cpp_string_from_utf8(buf, static_cast<int32_t>(len));
    }

    // Builds a Godot String from a managed (UTF-16) string via its UTF-8 form.
    void GdStringFromManaged(GdString* out, Dn2CppString* s)
    {
        int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
        char* buf = static_cast<char*>(dn2cpp_alloc(static_cast<size_t>(n) + 1));
        dn2cpp_string_to_utf8(s, buf, n);
        buf[n] = '\0';
        gd_string_new_utf8(out, buf, n);
    }

    // Marshals a StringName/NodePath (a single-pointer heap value) to a managed
    // UTF-16 string by constructing a Godot String from it (String ctor index
    // `strFromIdx`: 2 from StringName, 3 from NodePath) then decoding its UTF-8.
    Dn2CppString* ManagedFromStringLike(const void* opaque, int32_t strFromIdx)
    {
        GDExtensionPtrConstructor ctor = gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_STRING, strFromIdx);
        GdString gs{};
        const void* a[] = { opaque };
        ctor(&gs, a);
        Dn2CppString* r = ManagedStringFromGd(&gs);
        gd_string_destroy(&gs);
        return r;
    }

    // Builds a StringName/NodePath from a managed string by going via a Godot
    // String (their ctor index 2 is "from String"), so non-Latin1 text round-trips
    // correctly. `vt` is the target variant type; returns the constructed value in
    // the caller-owned `out` (a single-pointer heap value).
    void StringLikeFromManaged(void* out, GDExtensionVariantType vt, Dn2CppString* s)
    {
        GdString gs{};
        GdStringFromManaged(&gs, s);
        GDExtensionPtrConstructor ctor = gd_variant_get_ptr_constructor(vt, 2);
        const void* a[] = { &gs };
        ctor(out, a);
        gd_string_destroy(&gs);
    }

    void VariantFromManagedString(GDExtensionVariantPtr r_variant, Dn2CppString* s)
    {
        GdString gs;
        GdStringFromManaged(&gs, s);
        gd_variant_from_string(r_variant, &gs);
        gd_string_destroy(&gs);
    }

    GdStringName MakeStringNameFromManaged(Dn2CppString* s)
    {
        int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
        std::vector<char> buf(n + 1);
        dn2cpp_string_to_utf8(s, buf.data(), n);
        buf[n] = '\0';
        GdStringName sn;
        gd_string_name_new(&sn, buf.data(), 0);
        return sn;
    }

    // ConvertToVariant's per-type classification, memoized by type-info pointer:
    // un-memoized it is a strcmp chain plus a base-chain strcmp walk PER BOXED
    // ARGUMENT, since this TU has only names to go on where the core runtime has
    // pointer compares. Kind values start at 1 so a packed 0 always means
    // "not yet published".
    enum class VariantEncodeKind : uint8_t
    {
        Nil = 1,      // no case matched: encode as null object
        String, Bool, Int32, Int64, Single, Double, Vector2, Vector3,
        CollectionsDict, CollectionsArray, // engine container wrapper (per-OBJECT handle test at use)
        EngineObject, // chain reaches the Godot.Object shim base
    };
    struct VariantEncodeClass
    {
        VariantEncodeKind kind;
        // For a Collections wrapper whose engine handle turns out null, the old
        // chain FELL THROUGH to the Godot.Object walk — a per-type question,
        // answered here once so the use site keeps the exact same behavior.
        VariantEncodeKind collectionsFallback;
    };

    // The classification itself: the old ConvertToVariant test chain, same
    // checks in the same order, minus the per-object parts (payload reads, the
    // container-handle null test), which stay at the use site.
    VariantEncodeClass ClassifyVariantEncode(const Dn2CppTypeInfo* ti)
    {
        VariantEncodeClass c = { VariantEncodeKind::Nil, VariantEncodeKind::Nil };
        const char* n = ti->name;
        if (std::strcmp(n, "System.String") == 0) { c.kind = VariantEncodeKind::String; return c; }
        if (std::strcmp(n, "System.Boolean") == 0) { c.kind = VariantEncodeKind::Bool; return c; }
        if (std::strcmp(n, "System.Int32") == 0) { c.kind = VariantEncodeKind::Int32; return c; }
        if (std::strcmp(n, "System.Int64") == 0) { c.kind = VariantEncodeKind::Int64; return c; }
        if (std::strcmp(n, "System.Single") == 0) { c.kind = VariantEncodeKind::Single; return c; }
        if (std::strcmp(n, "System.Double") == 0) { c.kind = VariantEncodeKind::Double; return c; }
        if (std::strcmp(n, "Godot.Vector2") == 0) { c.kind = VariantEncodeKind::Vector2; return c; }
        if (std::strcmp(n, "Godot.Vector3") == 0) { c.kind = VariantEncodeKind::Vector3; return c; }
        bool isCollections = std::strncmp(n, "Godot.Collections.", 18) == 0;
        if (isCollections)
            c.kind = std::strcmp(n, "Godot.Collections.Dictionary") == 0
                ? VariantEncodeKind::CollectionsDict
                : VariantEncodeKind::CollectionsArray;
        for (const Dn2CppTypeInfo* t = ti; t != nullptr; t = t->base)
        {
            if (std::strcmp(t->name, "Godot.Object") == 0)
            {
                if (isCollections)
                    c.collectionsFallback = VariantEncodeKind::EngineObject;
                else
                    c.kind = VariantEncodeKind::EngineObject;
                break;
            }
        }
        return c;
    }

    // Memo: fixed-size open addressing keyed on the type-info pointer (stable —
    // a Dn2CppTypeInfo is a data-segment static or GC-rooted forever, and its
    // name never changes, so a verdict never invalidates; the memo holds no
    // managed pointer the GC must see). The engine calls the bridges from
    // threads it spawned itself (threaded resource loading, WorkerThreadPool),
    // so unlike the ResolveBind caches this one publishes with atomics: an
    // entry is claimed by CAS on the key, the packed verdict is release-stored
    // after it, and every racy intermediate state (claimed-but-unpublished,
    // full table) degrades to classifying uncached — never to a wrong verdict.
    VariantEncodeClass VariantEncodeClassFor(const Dn2CppTypeInfo* ti)
    {
        constexpr uint32_t kSize = 256; // power of two; well above the encodable-type surface
        constexpr uint32_t kProbeLimit = 8;
        struct Entry
        {
            // Invariant: callers pass non-null keys — key == nullptr doubles as
            // the empty-slot marker the claim CAS targets.
            std::atomic<const Dn2CppTypeInfo*> key{nullptr};
            std::atomic<uint16_t> packed{0}; // kind | fallback << 8; 0 = unpublished
        };
        static Entry entries[kSize];
        uint32_t slot = static_cast<uint32_t>((reinterpret_cast<uintptr_t>(ti) >> 4) * 31u) & (kSize - 1);
        for (uint32_t i = 0; i < kProbeLimit; i++)
        {
            Entry& e = entries[(slot + i) & (kSize - 1)];
            const Dn2CppTypeInfo* ek = e.key.load(std::memory_order_relaxed);
            if (ek == ti)
            {
                uint16_t p = e.packed.load(std::memory_order_acquire);
                if (p != 0)
                    return { static_cast<VariantEncodeKind>(p & 0xFF),
                             static_cast<VariantEncodeKind>(p >> 8) };
                break; // claimed but not yet published — classify uncached
            }
            if (ek == nullptr)
            {
                VariantEncodeClass c = ClassifyVariantEncode(ti);
                const Dn2CppTypeInfo* expected = nullptr;
                if (e.key.compare_exchange_strong(expected, ti,
                        std::memory_order_acq_rel, std::memory_order_relaxed))
                    e.packed.store(static_cast<uint16_t>(
                        static_cast<uint16_t>(c.kind)
                        | (static_cast<uint16_t>(c.collectionsFallback) << 8)),
                        std::memory_order_release);
                return c;
            }
        }
        return ClassifyVariantEncode(ti);
    }

    void ConvertToVariant(Dn2CppObject* obj, void* r_variant)
    {
        if (obj == nullptr)
        {
            void* null_obj = nullptr;
            gd_variant_from_object(r_variant, &null_obj);
            return;
        }

        VariantEncodeClass c = VariantEncodeClassFor(obj->type);
        VariantEncodeKind kind = c.kind;
        if (kind == VariantEncodeKind::CollectionsDict || kind == VariantEncodeKind::CollectionsArray)
        {
            // A Godot.Collections container wrapper (Array / Array<T> /
            // Dictionary): the Variant shares the wrapper's engine container
            // value. A null handle falls through to the memoized fallback,
            // exactly as the old chain fell through past this block.
            void* q = ContainerHandleOf(obj);
            if (q != nullptr)
            {
                gd_get_variant_from_type(kind == VariantEncodeKind::CollectionsDict
                    ? GDEXTENSION_VARIANT_TYPE_DICTIONARY
                    : GDEXTENSION_VARIANT_TYPE_ARRAY)(r_variant, q);
                return;
            }
            kind = c.collectionsFallback;
        }
        switch (kind)
        {
            case VariantEncodeKind::String:
                VariantFromManagedString(r_variant, reinterpret_cast<Dn2CppString*>(obj));
                return;
            case VariantEncodeKind::Bool:
            {
                uint8_t val = *reinterpret_cast<uint8_t*>(obj + 1);
                gd_variant_from_bool(r_variant, &val);
                return;
            }
            case VariantEncodeKind::Int32:
            {
                int64_t val = *reinterpret_cast<int32_t*>(obj + 1);
                gd_variant_from_int(r_variant, &val);
                return;
            }
            case VariantEncodeKind::Int64:
            {
                int64_t val = *reinterpret_cast<int64_t*>(obj + 1);
                gd_variant_from_int(r_variant, &val);
                return;
            }
            case VariantEncodeKind::Single:
            {
                double val = *reinterpret_cast<float*>(obj + 1);
                gd_variant_from_float(r_variant, &val);
                return;
            }
            case VariantEncodeKind::Double:
            {
                double val = *reinterpret_cast<double*>(obj + 1);
                gd_variant_from_float(r_variant, &val);
                return;
            }
            case VariantEncodeKind::Vector2:
            {
                float* xy = reinterpret_cast<float*>(obj + 1);
                gd_variant_from_vector2(r_variant, xy);
                return;
            }
            case VariantEncodeKind::Vector3:
            {
                float* xyz = reinterpret_cast<float*>(obj + 1);
                gd_variant_from_vector3(r_variant, xyz);
                return;
            }
            case VariantEncodeKind::EngineObject:
            {
                // Any engine-object shim — a type whose inheritance chain reaches
                // the Godot.Object shim base (Node subclasses, RefCounted/Resource
                // subclasses, registered user classes alike): the Variant carries
                // the shim's engine handle (the Godot.Object base's first field).
                // The engine's Variant-from-Object constructor takes its own
                // reference for RefCounted objects, released when the Variant is
                // destroyed — the same convention as the export-boundary Variant
                // encode — so no manual reference() here.
                void* handle = *reinterpret_cast<void**>(reinterpret_cast<char*>(obj) + sizeof(Dn2CppObject));
                gd_variant_from_object(r_variant, &handle);
                return;
            }
            default:
                break;
        }

        // Fallback NIL
        void* null_obj = nullptr;
        gd_variant_from_object(r_variant, &null_obj);
    }

    // Ensure the calling thread is registered with the collector before any GC
    // activity — the engine calls the entry bridges below from threads it
    // spawned itself (threaded resource loading, WorkerThreadPool, editor
    // import) that Boehm knows nothing about. The body is the core's shared
    // register-once-and-leave hook (also serving the .NET-module lane's
    // dn2cpp_dotnetmodule_thread_guard); see
    // dn2cpp_gc_ensure_thread_registered in runtime/core for the rationale.
    inline void EnsureGcThreadRegistered()
    {
        dn2cpp_gc_ensure_thread_registered();
    }

    // Generic Variant-call thunk shared by all bound methods.
    void GenericCall(void* method_userdata, GDExtensionClassInstancePtr instance,
                     const GDExtensionConstVariantPtr* p_args, GDExtensionInt p_argument_count,
                     GDExtensionVariantPtr r_return, GDExtensionCallError* r_error)
    {
        EnsureGcThreadRegistered();
        const auto* m = static_cast<const Dn2CppGodotMethod*>(method_userdata);
        if (p_argument_count != m->argCount)
        {
            r_error->error = p_argument_count < m->argCount
                ? GDEXTENSION_CALL_ERROR_TOO_FEW_ARGUMENTS
                : GDEXTENSION_CALL_ERROR_TOO_MANY_ARGUMENTS;
            r_error->expected = m->argCount;
            return;
        }

        Dn2CppValue args[8] = {};
        for (int i = 0; i < m->argCount; i++)
        {
            auto arg = const_cast<GDExtensionVariantPtr>(p_args[i]);
            switch (m->argTypes[i])
            {
                case DN2CPP_GD_BOOL:
                {
                    uint8_t b = 0;
                    gd_bool_from_variant(&b, arg);
                    args[i].i = b;
                    break;
                }
                case DN2CPP_GD_INT:
                {
                    int64_t v = 0;
                    gd_int_from_variant(&v, arg);
                    args[i].i = v;
                    break;
                }
                case DN2CPP_GD_FLOAT:
                {
                    double v = 0;
                    gd_float_from_variant(&v, arg);
                    args[i].d = v;
                    break;
                }
                case DN2CPP_GD_STRING:
                {
                    GdString gs;
                    gd_string_from_variant(&gs, arg);
                    args[i].s = ManagedStringFromGd(&gs);
                    gd_string_destroy(&gs);
                    break;
                }
                case DN2CPP_GD_VECTOR2:
                {
                    gd_vector2_from_variant(args[i].v2, arg);
                    break;
                }
                case DN2CPP_GD_VECTOR3:
                {
                    gd_vector3_from_variant(args[i].v3, arg);
                    break;
                }
                case DN2CPP_GD_VECTOR4:
                {
                    gd_vector4_from_variant(args[i].v4, arg);
                    break;
                }
                case DN2CPP_GD_COLOR:
                {
                    gd_color_from_variant(args[i].v4, arg);
                    break;
                }
                case DN2CPP_GD_TRANSFORM2D:
                    gd_transform2d_from_variant(args[i].big, arg);
                    break;
                case DN2CPP_GD_BASIS:
                    gd_basis_from_variant(args[i].big, arg);
                    break;
                case DN2CPP_GD_TRANSFORM3D:
                    gd_transform3d_from_variant(args[i].big, arg);
                    break;
                case DN2CPP_GD_PROJECTION:
                    gd_projection_from_variant(args[i].big, arg);
                    break;
                case DN2CPP_GD_QUATERNION:
                    gd_quaternion_from_variant(args[i].big, arg);
                    break;
                case DN2CPP_GD_PLANE:
                    gd_plane_from_variant(args[i].big, arg);
                    break;
                case DN2CPP_GD_AABB:
                    gd_aabb_from_variant(args[i].big, arg);
                    break;
                case DN2CPP_GD_PACKED_BYTE:
                case DN2CPP_GD_PACKED_INT32:
                case DN2CPP_GD_PACKED_INT64:
                case DN2CPP_GD_PACKED_FLOAT32:
                case DN2CPP_GD_PACKED_FLOAT64:
                case DN2CPP_GD_PACKED_STRING:
                case DN2CPP_GD_PACKED_VECTOR2:
                case DN2CPP_GD_PACKED_VECTOR3:
                case DN2CPP_GD_PACKED_VECTOR4:
                case DN2CPP_GD_PACKED_COLOR:
                    args[i].arr = PackedVariantToManaged(m->argTypes[i], arg);
                    break;
                case DN2CPP_GD_ARRAY:
                    args[i].arr = ArrayVariantToManaged(arg);
                    break;
                case DN2CPP_GD_VARIANT:
                    // Decode the engine Variant straight into the (layout-identical)
                    // Dn2CppValue slot the gw_ thunk reads back as a shim Variant.
                    DecodeVariant(arg, reinterpret_cast<Dn2CppGodotVariant*>(&args[i]));
                    break;
                case DN2CPP_GD_DICTIONARY:
                case DN2CPP_GD_GODOT_ARRAY:
                    // The carried container is wrapped (shared, not copied) in a
                    // fresh finalizer-owned managed wrapper object.
                    args[i].arr = ContainerVariantToManaged(m->argTypes[i], arg);
                    break;
            }
        }

        Dn2CppValue ret = {};
        try
        {
            m->invoke(instance, args, &ret);
        }
        catch (Dn2CppException& ex)
        {
            dn2cpp_report_boundary_exception(ex.obj, "%s.%s", m->className, m->methodName);
            r_error->error = GDEXTENSION_CALL_ERROR_INVALID_METHOD;
            return;
        }

        switch (m->returnType)
        {
            case DN2CPP_GD_BOOL:
            {
                uint8_t b = ret.i != 0 ? 1 : 0;
                gd_variant_from_bool(r_return, &b);
                break;
            }
            case DN2CPP_GD_INT:
                gd_variant_from_int(r_return, &ret.i);
                break;
            case DN2CPP_GD_FLOAT:
                gd_variant_from_float(r_return, &ret.d);
                break;
            case DN2CPP_GD_STRING:
                VariantFromManagedString(r_return, ret.s);
                break;
            case DN2CPP_GD_VECTOR2:
                gd_variant_from_vector2(r_return, ret.v2);
                break;
            case DN2CPP_GD_VECTOR3:
                gd_variant_from_vector3(r_return, ret.v3);
                break;
            case DN2CPP_GD_VECTOR4:
                gd_variant_from_vector4(r_return, ret.v4);
                break;
            case DN2CPP_GD_COLOR:
                gd_variant_from_color(r_return, ret.v4);
                break;
            case DN2CPP_GD_TRANSFORM2D:
                gd_variant_from_transform2d(r_return, ret.big);
                break;
            case DN2CPP_GD_BASIS:
                gd_variant_from_basis(r_return, ret.big);
                break;
            case DN2CPP_GD_TRANSFORM3D:
                gd_variant_from_transform3d(r_return, ret.big);
                break;
            case DN2CPP_GD_PROJECTION:
                gd_variant_from_projection(r_return, ret.big);
                break;
            case DN2CPP_GD_QUATERNION:
                gd_variant_from_quaternion(r_return, ret.big);
                break;
            case DN2CPP_GD_PLANE:
                gd_variant_from_plane(r_return, ret.big);
                break;
            case DN2CPP_GD_AABB:
                gd_variant_from_aabb(r_return, ret.big);
                break;
            case DN2CPP_GD_PACKED_BYTE:
            case DN2CPP_GD_PACKED_INT32:
            case DN2CPP_GD_PACKED_INT64:
            case DN2CPP_GD_PACKED_FLOAT32:
            case DN2CPP_GD_PACKED_FLOAT64:
            case DN2CPP_GD_PACKED_STRING:
            case DN2CPP_GD_PACKED_VECTOR2:
            case DN2CPP_GD_PACKED_VECTOR3:
            case DN2CPP_GD_PACKED_VECTOR4:
            case DN2CPP_GD_PACKED_COLOR:
                ManagedToPackedVariant(m->returnType, r_return, ret.arr);
                break;
            case DN2CPP_GD_ARRAY:
                ManagedToArrayVariant(r_return, ret.arr);
                break;
            case DN2CPP_GD_VARIANT:
                // r_return is a pre-constructed nil Variant (variant-call convention);
                // EncodeVariant constructs the payload in place, leaving nil for kind 0.
                EncodeVariant(reinterpret_cast<const Dn2CppGodotVariant*>(&ret), r_return);
                break;
            case DN2CPP_GD_DICTIONARY:
            case DN2CPP_GD_GODOT_ARRAY:
                ManagedToContainerVariant(m->returnType, r_return, ret.arr);
                break;
            default:
                break; // void: leave NIL
        }
        r_error->error = GDEXTENSION_CALL_OK;
    }

    // Raw ptrcall thunk (used by typed GDScript / C# style calls).
    void GenericPtrCall(void* method_userdata, GDExtensionClassInstancePtr instance,
                        const GDExtensionConstTypePtr* p_args, GDExtensionTypePtr r_ret)
    {
        EnsureGcThreadRegistered();
        const auto* m = static_cast<const Dn2CppGodotMethod*>(method_userdata);
        Dn2CppValue args[8] = {};
        for (int i = 0; i < m->argCount; i++)
        {
            switch (m->argTypes[i])
            {
                case DN2CPP_GD_BOOL:
                    args[i].i = *static_cast<const uint8_t*>(p_args[i]);
                    break;
                case DN2CPP_GD_INT:
                    args[i].i = *static_cast<const int64_t*>(p_args[i]);
                    break;
                case DN2CPP_GD_FLOAT:
                    args[i].d = *static_cast<const double*>(p_args[i]);
                    break;
                case DN2CPP_GD_STRING:
                    args[i].s = ManagedStringFromGd(static_cast<const GdString*>(p_args[i]));
                    break;
                case DN2CPP_GD_VECTOR2:
                    std::memcpy(args[i].v2, p_args[i], sizeof(args[i].v2));
                    break;
                case DN2CPP_GD_VECTOR3:
                    std::memcpy(args[i].v3, p_args[i], sizeof(args[i].v3));
                    break;
                case DN2CPP_GD_VECTOR4:
                case DN2CPP_GD_COLOR:
                    std::memcpy(args[i].v4, p_args[i], sizeof(args[i].v4));
                    break;
                case DN2CPP_GD_TRANSFORM2D:
                case DN2CPP_GD_BASIS:
                case DN2CPP_GD_TRANSFORM3D:
                case DN2CPP_GD_PROJECTION:
                case DN2CPP_GD_QUATERNION:
                case DN2CPP_GD_PLANE:
                case DN2CPP_GD_AABB:
                    std::memcpy(args[i].big, p_args[i], BigTypeByteSize(m->argTypes[i]));
                    break;
                case DN2CPP_GD_PACKED_BYTE:
                case DN2CPP_GD_PACKED_INT32:
                case DN2CPP_GD_PACKED_INT64:
                case DN2CPP_GD_PACKED_FLOAT32:
                case DN2CPP_GD_PACKED_FLOAT64:
                case DN2CPP_GD_PACKED_STRING:
                case DN2CPP_GD_PACKED_VECTOR2:
                case DN2CPP_GD_PACKED_VECTOR3:
                case DN2CPP_GD_PACKED_VECTOR4:
                case DN2CPP_GD_PACKED_COLOR:
                    args[i].arr = PackedToManaged(m->argTypes[i], p_args[i]);
                    break;
                case DN2CPP_GD_ARRAY:
                    args[i].arr = ArrayToManaged(p_args[i]);
                    break;
                case DN2CPP_GD_VARIANT:
                    // ptrcall passes a Variant-typed arg as a pointer to a Variant.
                    DecodeVariant(p_args[i], reinterpret_cast<Dn2CppGodotVariant*>(&args[i]));
                    break;
                case DN2CPP_GD_DICTIONARY:
                case DN2CPP_GD_GODOT_ARRAY:
                    // ptrcall passes a container-typed arg as a pointer to its
                    // engine value; wrap it (shared) in a managed wrapper.
                    args[i].arr = ContainerToManaged(m->argTypes[i], p_args[i]);
                    break;
            }
        }

        Dn2CppValue ret = {};
        try
        {
            m->invoke(instance, args, &ret);
        }
        catch (Dn2CppException& ex)
        {
            dn2cpp_report_boundary_exception(ex.obj, "%s.%s", m->className, m->methodName);
            return; // r_ret left zero-initialized by the caller
        }

        switch (m->returnType)
        {
            case DN2CPP_GD_BOOL:
                *static_cast<uint8_t*>(r_ret) = ret.i != 0 ? 1 : 0;
                break;
            case DN2CPP_GD_INT:
                *static_cast<int64_t*>(r_ret) = ret.i;
                break;
            case DN2CPP_GD_FLOAT:
                *static_cast<double*>(r_ret) = ret.d;
                break;
            case DN2CPP_GD_STRING:
                GdStringFromManaged(static_cast<GdString*>(r_ret), ret.s);
                break;
            case DN2CPP_GD_VECTOR2:
                std::memcpy(r_ret, ret.v2, sizeof(ret.v2));
                break;
            case DN2CPP_GD_VECTOR3:
                std::memcpy(r_ret, ret.v3, sizeof(ret.v3));
                break;
            case DN2CPP_GD_VECTOR4:
            case DN2CPP_GD_COLOR:
                std::memcpy(r_ret, ret.v4, sizeof(ret.v4));
                break;
            case DN2CPP_GD_TRANSFORM2D:
            case DN2CPP_GD_BASIS:
            case DN2CPP_GD_TRANSFORM3D:
            case DN2CPP_GD_PROJECTION:
            case DN2CPP_GD_QUATERNION:
            case DN2CPP_GD_PLANE:
            case DN2CPP_GD_AABB:
                std::memcpy(r_ret, ret.big, BigTypeByteSize(m->returnType));
                break;
            case DN2CPP_GD_PACKED_BYTE:
            case DN2CPP_GD_PACKED_INT32:
            case DN2CPP_GD_PACKED_INT64:
            case DN2CPP_GD_PACKED_FLOAT32:
            case DN2CPP_GD_PACKED_FLOAT64:
            case DN2CPP_GD_PACKED_STRING:
            case DN2CPP_GD_PACKED_VECTOR2:
            case DN2CPP_GD_PACKED_VECTOR3:
            case DN2CPP_GD_PACKED_VECTOR4:
            case DN2CPP_GD_PACKED_COLOR:
                ManagedToPackedValue(m->returnType, r_ret, ret.arr);
                break;
            case DN2CPP_GD_ARRAY:
                ManagedToArrayValue(r_ret, ret.arr);
                break;
            case DN2CPP_GD_VARIANT:
            {
                // ptrcall return slot is uninitialised Variant memory (24-byte
                // opaque). Zero it to a valid nil first, then EncodeVariant builds
                // the payload in place (nil stays nil for kind 0).
                std::memset(r_ret, 0, 24);
                EncodeVariant(reinterpret_cast<const Dn2CppGodotVariant*>(&ret), r_ret);
                break;
            }
            case DN2CPP_GD_DICTIONARY:
            case DN2CPP_GD_GODOT_ARRAY:
                // ptrcall return slot is an uninitialised container value; build
                // it from (sharing) the returned wrapper's engine value.
                ManagedToContainerValue(m->returnType, r_ret, ret.arr);
                break;
            default:
                break;
        }
    }

    // Every class name this library handed to ClassDB, in registration order, so
    // deinit can unregister them in reverse. Deliberately NO cap: the number of
    // exported classes is a property of the game, not of the runtime, so a fixed
    // array fails a mid-sized game's boot on whichever class overflows it.
    //
    // INVARIANT: nothing may retain the ADDRESS of an element. Growth
    // reallocates, so a retained `&g_registeredClasses[i]` would dangle. The
    // stored `const char*` values are string literals in the generated tables
    // and are stable; the class_userdata handed to ClassDB is that literal (or
    // the Dn2CppGodotNodeClass row), never a slot of this vector. Keep it that
    // way — read elements by value.
    std::vector<const char*> g_registeredClasses;

    // Diagnostic: net count of StringNames allocated by the engine-object
    // construction helpers (construct_engine_object / CreateInstance) that have
    // not yet been destroyed. Each is a function-local temporary the engine only
    // reads by value, so it must be freed before its helper returns; a non-zero
    // value at library deinit means a construction path leaked an interned-string
    // reference (invisible to ObjectDB). Reported at deinit for the gate to catch.
    int64_t g_construct_sn_live = 0;

    // ---- Node-derived class support ----

    // Interned-StringName equality: a StringName is a single interned pointer, so
    // two names are equal iff their opaque pointers match. Used to match the
    // engine's get_virtual_call_data query against a class's virtual-table names.
    bool SnEq(GDExtensionConstStringNamePtr a, const GdStringName& b)
    {
        return *static_cast<void* const*>(a) == b.opaque;
    }

    GDExtensionObjectPtr NodeCreateInstance(void* class_userdata, GDExtensionBool /*notify_postinitialize*/)
    {
        EnsureGcThreadRegistered();
        const auto* nc = static_cast<const Dn2CppGodotNodeClass*>(class_userdata);
        // ClassDB-driven construction (GDScript's .new(), scene/resource
        // deserialization, the editor, ...) is always pinned regardless of
        // Node vs. RefCounted: the engine (invisible to Boehm) can be the sole
        // owner, so the self bound via object_set_instance must never be
        // collected.
        auto* self = static_cast<Dn2CppObject*>(dn2cpp_alloc_pinned(static_cast<size_t>(nc->instanceSize)));
        self->type = nc->type;
        // Unlike TryEmitNewobj's direct C# `new` path (which owns the only handle
        // to the freshly built engine object and must init_ref() it itself),
        // create_instance_func's caller is the engine's own ClassDB::instantiate()
        // (GDScript `.new()`, scene/resource deserialization, the editor, ...): it
        // wraps the returned raw pointer in its own Ref<RefCounted> and performs
        // the first reference itself once this call returns (mirroring the
        // ptrcall Ref<T> return-value convention). Passing isRefCounted here too
        // would double-init and leak (observable as get_reference_count() == 2),
        // so this call always passes 0 regardless of nc->isRefCounted.
        GDExtensionObjectPtr obj = static_cast<GDExtensionObjectPtr>(
            dn2cpp_godot_construct_engine_object(self, nc->parentClass, nc->className, /*is_ref_counted=*/0));
        if (nc->ctor != nullptr)
        {
            try
            {
                nc->ctor(self);
            }
            catch (Dn2CppException& ex)
            {
                dn2cpp_report_boundary_exception(ex.obj, "%s..ctor", nc->className);
            }
        }
        return obj;
    }

    void NodeFreeInstance(void* /*class_userdata*/, GDExtensionClassInstancePtr p_instance)
    {
        EnsureGcThreadRegistered();
        dn2cpp_free_pinned(p_instance);
    }

    // Resolve a virtual the engine asks about to its per-class table entry, or
    // null when this class does not override it. The engine caches the returned
    // call-data pointer per (class, virtual), so the linear scan + interned
    // StringName compare runs once per pair, not per call.
    void* NodeGetVirtualCallData(void* class_userdata, GDExtensionConstStringNamePtr p_name, uint32_t /*hash*/)
    {
        auto* nc = static_cast<const Dn2CppGodotNodeClass*>(class_userdata);
        for (int i = 0; i < nc->virtualCount; i++)
        {
            GdStringName sn = MakeStringName(nc->virtuals[i].name);
            bool eq = SnEq(p_name, sn);
            gd_string_name_destroy(&sn);
            if (eq)
                return const_cast<Dn2CppGodotVirtual*>(&nc->virtuals[i]);
        }
        return nullptr;
    }

    void NodeCallVirtualWithData(GDExtensionClassInstancePtr p_instance, GDExtensionConstStringNamePtr /*p_name*/,
                                 void* p_userdata, const GDExtensionConstTypePtr* p_args, GDExtensionTypePtr r_ret)
    {
        EnsureGcThreadRegistered();
        const auto* v = static_cast<const Dn2CppGodotVirtual*>(p_userdata);
        auto* self = static_cast<Dn2CppObject*>(p_instance);
        try
        {
            // The trampoline (emitted per class by GodotBackend) decodes each
            // p_args[i] from its native ptrcall encoding into the managed form,
            // calls the override, and encodes the return into r_ret.
            v->trampoline(self, static_cast<const void* const*>(p_args), r_ret);
        }
        catch (Dn2CppException& ex)
        {
            dn2cpp_report_boundary_exception(ex.obj, "%s.%s", self->type->name, v->name);
        }
        // Manual finalizer-drain mode (enabled at init) has no background thread,
        // so drain any finalizers that came due from organic collections here, on
        // the engine's main thread, once per frame. Gated to the process ticks
        // (recognised by name — the table is engine-named) so input/other
        // virtuals don't pay for it; a no-op outside manual mode. The
        // node-independent main-loop frame callback (MainLoopFrame) is the primary
        // per-frame consumer; this site stays as a redundant drain
        // (reentry-guarded, so the overlap is a cheap no-op) and as the only
        // per-frame one on pre-4.5 engines without the frame callback.
        if (std::strcmp(v->name, "_process") == 0 || std::strcmp(v->name, "_physics_process") == 0)
        {
            dn2cpp_godot_sweep_anchored_refcounted();
            dn2cpp_gc_drain_finalizers();
        }
    }

    // Recover the node-class row for a live instance. Unlike the virtual-call
    // path, notification_func is not handed the class_userdata, so we map the
    // instance's exact runtime type back to its registered row (the table is
    // tiny and lookups are rare — one per engine notification).
    const Dn2CppGodotNodeClass* NodeClassForType(const Dn2CppTypeInfo* type)
    {
        for (int i = 0; i < dn2cpp_godot_node_class_count; i++)
        {
            if (dn2cpp_godot_node_classes[i].type == type)
                return &dn2cpp_godot_node_classes[i];
        }
        return nullptr;
    }

    // Engine backing for the interpreter's `newobj` of a hot-update patch type
    // (registered via dn2cpp_interp_set_alloc_hook at init). A patch class
    // deriving from a ClassDB-registered class gets what the AOT C#-`new` path
    // emits inline: a collectible allocation (a patch instance is C#-owned and
    // GC-visible through patch/base statics, unlike NodeCreateInstance's pinned
    // engine-owned instances), the engine object constructed under the nearest
    // registered ancestor's class name — object_set_instance binds it there, so
    // engine virtual dispatch runs that class's virtual table, whose hotupdate
    // trampolines route through self->type's (patched) vtable into the
    // interpreted overrides — and init_ref for the RefCounted family (a direct
    // `new` owns the first reference). The header is stamped with the PATCH
    // type-info; the engine sees the instance as the registered ancestor class.
    // A type with no registered ancestor declines (null): the interpreter's
    // default allocation runs. The finalizer (RefCounted teardown, inherited
    // from the AOT base type-info) is registered by the interpreter after this
    // returns. Known gap vs the AOT path: the RefCounted family's engine-bound
    // flag lives at a generated-struct field offset this runtime cannot name,
    // so it stays 0 — a collected RefCounted patch shim whose engine object
    // survives skips the binding detach. The Node family (this hook's driving
    // case) has no finalizer-driven teardown at all.
    Dn2CppObject* InterpPatchAlloc(const Dn2CppTypeInfo* type, size_t size)
    {
        const Dn2CppGodotNodeClass* nc = nullptr;
        for (const Dn2CppTypeInfo* t = type; t != nullptr && nc == nullptr; t = t->base)
            nc = NodeClassForType(t);
        if (nc == nullptr)
            return nullptr;
        auto* self = static_cast<Dn2CppObject*>(dn2cpp_alloc(size));
        self->type = type;
        dn2cpp_godot_construct_engine_object(self, nc->parentClass, nc->className, nc->isRefCounted);
        return self;
    }

    // Engine notification hook (GDExtensionClassNotification2). Forwards every
    // notification constant (NOTIFICATION_ENTER_TREE/READY/EXIT_TREE/...) to the
    // managed _Notification(int) override when present. p_reversed (base-vs-derived
    // propagation order) is irrelevant to a single managed override, so ignore it.
    void NodeNotification(GDExtensionClassInstancePtr p_instance, int32_t p_what, GDExtensionBool /*p_reversed*/)
    {
        // A null instance is a detached binding: the collectible shim was
        // reclaimed while the engine still held references (see ~RefCounted's
        // __DetachBinding path), and the engine's notification dispatch — unlike
        // free_instance — is not gated on the instance pointer. Nothing managed
        // is left to notify (predelete of the surviving engine object lands
        // here when its last reference is dropped, e.g. by a Variant decode's
        // lifetime guard).
        if (p_instance == nullptr)
            return;
        EnsureGcThreadRegistered();
        auto* self = static_cast<Dn2CppObject*>(p_instance);
        const Dn2CppGodotNodeClass* nc = NodeClassForType(self->type);
        if (nc == nullptr || nc->notification == nullptr)
            return;
        try
        {
            nc->notification(self, p_what);
        }
        catch (Dn2CppException& ex)
        {
            dn2cpp_report_boundary_exception(ex.obj, "%s._notification", nc->className);
        }
    }

    void RegisterSignals(GDExtensionConstStringNamePtr class_name, const Dn2CppGodotSignal* signals, int32_t signal_count)
    {
        if (signals == nullptr)
            return;
        for (int32_t i = 0; i < signal_count; i++)
        {
            const auto& sig = signals[i];
            GdStringName sn_sig = MakeStringName(sig.name);

            GDExtensionPropertyInfo argInfos[8] = {};
            GdStringName argNames[8];
            GdStringName emptyName = MakeStringName("");
            GdString emptyHint;
            gd_string_new_utf8(&emptyHint, "", 0);

            for (int32_t ai = 0; ai < sig.argCount; ai++)
            {
                char nameBuf[8] = { 'a', 'r', 'g', static_cast<char>('0' + ai), 0 };
                argNames[ai] = MakeStringName(nameBuf);
                argInfos[ai].type = ToVariantType(sig.argTypes[ai]);
                argInfos[ai].name = &argNames[ai];
                argInfos[ai].class_name = &emptyName;
                argInfos[ai].hint_string = &emptyHint;
                argInfos[ai].usage = 6; // PROPERTY_USAGE_DEFAULT
            }

            gd_register_signal(g_library, class_name, &sn_sig, argInfos, sig.argCount);

            gd_string_name_destroy(&sn_sig);
            for (int32_t ai = 0; ai < sig.argCount; ai++)
            {
                gd_string_name_destroy(&argNames[ai]);
            }
            gd_string_name_destroy(&emptyName);
            gd_string_destroy(&emptyHint);
        }
    }

    void RegisterNodeClass(const Dn2CppGodotNodeClass* nc)
    {
        g_registeredClasses.push_back(nc->className);

        GdStringName sn = MakeStringName(nc->className);
        GdStringName parent = MakeStringName(nc->parentClass);
        GdString iconPath;
        gd_string_new_utf8(&iconPath, "", 0);

        GDExtensionClassCreationInfo4 info = {};
        info.is_virtual = 0;
        info.is_abstract = 0;
        info.is_exposed = 1;
        info.is_runtime = 0;
        info.icon_path = &iconPath;
        info.create_instance_func = &NodeCreateInstance;
        info.free_instance_func = &NodeFreeInstance;
        info.get_virtual_call_data_func = &NodeGetVirtualCallData;
        info.call_virtual_with_data_func = &NodeCallVirtualWithData;
        info.notification_func = &NodeNotification;
        info.class_userdata = const_cast<Dn2CppGodotNodeClass*>(nc);
        gd_register_class(g_library, &sn, &parent, &info);
    }

    void RegisterNodeClassPropertiesAndSignals(const Dn2CppGodotNodeClass* nc)
    {
        GdStringName sn = MakeStringName(nc->className);

        RegisterSignals(&sn, nc->signals, nc->signalCount);

        for (int i = 0; i < nc->propertyCount; i++)
        {
            const Dn2CppGodotProperty* p = &nc->properties[i];

            GdStringName getter_sn = MakeStringName(p->getterName);
            GdStringName setter_sn = MakeStringName(p->setterName ? p->setterName : "");
            GdStringName prop_name_sn = MakeStringName(p->propertyName);

            GDExtensionPropertyInfo pinfo = {};
            pinfo.type = ToVariantType(p->type);
            pinfo.name = &prop_name_sn;
            
            GdStringName empty_class = MakeStringName("");
            GdString empty_hint;
            gd_string_new_utf8(&empty_hint, "", 0);

            pinfo.class_name = &empty_class;
            pinfo.hint_string = &empty_hint;
            pinfo.hint = 0;
            pinfo.usage = 6; // PROPERTY_USAGE_DEFAULT

            gd_register_property(g_library, &sn, &pinfo, p->setterName ? &setter_sn : nullptr, &getter_sn);
        }
    }

    // Instance support so `ClassName.new()` also works (static-only classes
    // don't strictly need it, but Godot requires the callbacks).
    GDExtensionObjectPtr CreateInstance(void* class_userdata, GDExtensionBool /*notify_postinitialize*/)
    {
        GdStringName parent = MakeStringName("Object");
        g_construct_sn_live++;
        GDExtensionObjectPtr obj = gd_construct_object(&parent);
        gd_string_name_destroy(&parent);
        g_construct_sn_live--;
        auto* className = static_cast<const char*>(class_userdata);
        GdStringName sn = MakeStringName(className);
        g_construct_sn_live++;
        gd_object_set_instance(obj, &sn, obj);
        gd_string_name_destroy(&sn);
        g_construct_sn_live--;
        return obj;
    }

    void FreeInstance(void* /*class_userdata*/, GDExtensionClassInstancePtr /*instance*/)
    {
    }

    void RegisterClassOnce(const char* className)
    {
        for (const char* seen : g_registeredClasses)
        {
            if (std::strcmp(seen, className) == 0)
                return;
        }
        g_registeredClasses.push_back(className);

        GdStringName sn = MakeStringName(className);
        GdStringName parent = MakeStringName("Object");
        GdString iconPath;
        gd_string_new_utf8(&iconPath, "", 0);

        GDExtensionClassCreationInfo4 info = {};
        info.is_virtual = 0;
        // Static-method-only surface for now; abstract avoids editor paths
        // that instantiate every exposed class (e.g. doc generation).
        info.is_abstract = 1;
        info.is_exposed = 1;
        info.is_runtime = 0;
        info.icon_path = &iconPath;
        info.create_instance_func = &CreateInstance;
        info.free_instance_func = &FreeInstance;
        info.class_userdata = const_cast<char*>(className);

        gd_register_class(g_library, &sn, &parent, &info);
    }

    void RegisterMethod(const Dn2CppGodotMethod* m)
    {
        GdStringName className = MakeStringName(m->className);
        GdStringName methodName = MakeStringName(m->methodName);
        GdStringName emptyName = MakeStringName("");
        GdString emptyHint;
        gd_string_new_utf8(&emptyHint, "", 0);

        // A bare-Variant arg/return is type NIL; without PROPERTY_USAGE_NIL_IS_VARIANT
        // (1<<17) the engine reads a NIL-typed return as "void" (and a NIL arg as a
        // typed-null), so OR it in for tag 16 to mean "any Variant".
        const uint32_t kUsageDefault = 6;        // STORAGE | EDITOR
        const uint32_t kNilIsVariant = 1u << 17; // PROPERTY_USAGE_NIL_IS_VARIANT
        auto usageFor = [&](int32_t tag) -> uint32_t
        { return tag == DN2CPP_GD_VARIANT ? (kUsageDefault | kNilIsVariant) : kUsageDefault; };

        GDExtensionPropertyInfo retInfo = {};
        retInfo.type = ToVariantType(m->returnType);
        retInfo.name = &emptyName;
        retInfo.class_name = &emptyName;
        retInfo.hint_string = &emptyHint;
        retInfo.usage = usageFor(m->returnType);

        GDExtensionPropertyInfo argInfos[8] = {};
        GDExtensionClassMethodArgumentMetadata argMeta[8] = {};
        GdStringName argNames[8];
        for (int i = 0; i < m->argCount; i++)
        {
            char nameBuf[8] = { 'a', 'r', 'g', static_cast<char>('0' + i), 0 };
            argNames[i] = MakeStringName(nameBuf);
            argInfos[i].type = ToVariantType(m->argTypes[i]);
            argInfos[i].name = &argNames[i];
            argInfos[i].class_name = &emptyName;
            argInfos[i].hint_string = &emptyHint;
            argInfos[i].usage = usageFor(m->argTypes[i]);
            argMeta[i] = GDEXTENSION_METHOD_ARGUMENT_METADATA_NONE;
        }

        GDExtensionClassMethodInfo info = {};
        info.name = &methodName;
        info.method_userdata = const_cast<Dn2CppGodotMethod*>(m);
        info.call_func = &GenericCall;
        info.ptrcall_func = &GenericPtrCall;
        info.method_flags = m->isStatic ? GDEXTENSION_METHOD_FLAG_STATIC : GDEXTENSION_METHOD_FLAG_NORMAL;
        info.has_return_value = m->returnType != DN2CPP_GD_VOID ? 1 : 0;
        info.return_value_info = &retInfo;
        info.return_value_metadata = GDEXTENSION_METHOD_ARGUMENT_METADATA_NONE;
        info.argument_count = static_cast<uint32_t>(m->argCount);
        info.arguments_info = argInfos;
        info.arguments_metadata = argMeta;

        gd_register_method(g_library, &className, &info);
    }

    void Initialize(void* /*userdata*/, GDExtensionInitializationLevel p_level)
    {
        if (p_level != GDEXTENSION_INITIALIZATION_SCENE)
            return;

        if (gd_get_utility != nullptr)
        {
            GdStringName snPrint = MakeStringName("print");
            gd_utility_print = gd_get_utility(&snPrint, 2648703342); // print(Variant...) API hash
        }

        // Real-time default for Godot: bound GC frame pauses with Boehm's
        // incremental collector. Set before managed init (which runs the GC init)
        // so it takes effect as the default; DN2CPP_GC_INCREMENTAL=0 still opts out.
        dn2cpp_gc_set_incremental_default(1);
        // A game may hand [UnmanagedCallersOnly] function pointers to a native
        // library (an audio middleware's I/O vtable) that invokes them from
        // threads it spawned itself — threads Boehm has never seen, whose first
        // managed allocation would abort the collector. Enable the prologue's
        // register-on-entry (RAII unregister at thread exit) for every such
        // entry point.
        dn2cpp_set_native_callback_gc_registration(1);
        // A windowed engine process loads hundreds of system frameworks; Boehm's
        // stock per-image root scanning overflows its root-set table ("Too many
        // root sets" abort). Register only our image's roots instead;
        // DN2CPP_GC_SELF_ROOTS=0 still opts out.
        dn2cpp_gc_set_self_roots_default(1);
        // Run managed finalizers (RefCounted teardown: engine object destroy /
        // unreference / set_instance) on the engine's main thread, not a background
        // finalizer thread — Godot object lifecycle is main-thread-affine. The ring
        // is drained every frame by the main-loop frame callback (MainLoopFrame),
        // by the per-frame process hook (NodeCallVirtualWithData), and by
        // GC.WaitForPendingFinalizers. Set before the first finalizable allocation.
        dn2cpp_gc_set_manual_finalizer_drain(1);
        // Give hot-update patch instances their engine backing: the
        // interpreter's patch `newobj` consults this hook (a no-op setter in a
        // build that never loads a patch — the hook variable lives in the core
        // runtime, not the interpreter TU).
        dn2cpp_interp_set_alloc_hook(&InterpPatchAlloc);
        dn2cpp_godot_init_managed();

        // Size the registry once from the counts the emitter stamped into the
        // generated tables: every registration below comes from one of these two
        // tables, and a static-method class is registered at most once, so their
        // sum is an exact upper bound. This is a SIZING HINT, not a limit — a
        // registration path added later still grows the vector rather than
        // failing — but it means the common case allocates exactly once.
        g_registeredClasses.reserve(
            static_cast<size_t>(dn2cpp_godot_node_class_count) +
            static_cast<size_t>(dn2cpp_godot_method_count));

        for (int i = 0; i < dn2cpp_godot_node_class_count; i++)
            RegisterNodeClass(&dn2cpp_godot_node_classes[i]);

        for (int i = 0; i < dn2cpp_godot_method_count; i++)
        {
            const Dn2CppGodotMethod* m = &dn2cpp_godot_methods[i];
            RegisterClassOnce(m->className);
            RegisterMethod(m);
        }

        for (int i = 0; i < dn2cpp_godot_node_class_count; i++)
            RegisterNodeClassPropertiesAndSignals(&dn2cpp_godot_node_classes[i]);

        std::printf("[dn2cpp] GDExtension initialized: %d managed method(s), %d node class(es), %d class(es) total\n",
            dn2cpp_godot_method_count, dn2cpp_godot_node_class_count,
            static_cast<int>(g_registeredClasses.size()));
    }

    // Node-independent per-frame drain (main-loop frame callback, since
    // Godot 4.5): manual finalizer-drain mode has no background thread, and
    // the per-frame drain inside the process-virtual bridge only fires while
    // an exported class with process virtuals sits in the scene tree. Without
    // this hook a scene with no such node never drains — queued RefCounted
    // teardowns (engine object destroy / unreference) would pile up until
    // library deinit. The engine calls this on the main thread once per
    // process frame, after all Node _process() methods, so the drain keeps
    // the main-thread affinity chosen at init. The process-virtual drain
    // sites stay (the drain is reentry-guarded and serialized, so an extra
    // call is a cheap no-op).
    // This lane's half of the shared host-boundary contract (dn2cpp_core.h). A
    // managed fault the engine cannot be allowed to unwind through is reported
    // HERE, in the engine's own error log, rather than on stderr: an exported
    // game's stderr is nowhere the player or the editor's Errors panel looks,
    // and a degrade nobody is told about is indistinguishable from the node
    // silently doing nothing.
    //
    // print_error, not print_warning: the node did not finish its method. The
    // file/line arguments are the honest ones available — a native binary has
    // no C# source position, and the managed trace, when the throw captured
    // one, is inside the description.
    void GodotBoundarySink(const char* where, Dn2CppObject* exc)
    {
        std::string text = "[dn2cpp] unhandled managed exception in ";
        text += where != nullptr ? where : "<unknown>";
        text += ": ";
        if (exc != nullptr)
        {
            Dn2CppString* s = dn2cpp_exception_tostring(exc);
            int32_t n = dn2cpp_string_to_utf8(s, nullptr, 0);
            size_t base = text.size();
            text.resize(base + static_cast<size_t>(n));
            if (n > 0)
                dn2cpp_string_to_utf8(s, &text[base], n);
        }
        else
        {
            text += "<null>";
        }
        // A pre-4.1 engine (or a host that declined the function) leaves this
        // null. The report must still happen — the boundary's job is to be
        // heard, and losing the channel is not a reason to lose the message.
        if (gd_print_error != nullptr)
            gd_print_error(text.c_str(), where, "dn2cpp", 0, /*p_editor_notify=*/0);
        else
            std::fprintf(stderr, "%s\n", text.c_str());
    }

    void MainLoopStartup()
    {
    }

    void MainLoopShutdown()
    {
    }

    void MainLoopFrame()
    {
        EnsureGcThreadRegistered();
        // Frame-pump the main thread's cooperative scheduler: nothing on the
        // engine's main thread ever reaches the blocking drain, so without
        // this a continuation posted cross-thread (await Task.Run) or a
        // pending Task.Delay would sit on the run queue forever. No-op when
        // nothing is pending.
        dn2cpp_sched_pump();
        dn2cpp_godot_sweep_anchored_refcounted();
        dn2cpp_gc_drain_finalizers();
    }

    void Deinitialize(void* /*userdata*/, GDExtensionInitializationLevel p_level)
    {
        if (p_level != GDEXTENSION_INITIALIZATION_SCENE)
            return;
        // Stop the runtime's background threads before anything else: the host
        // dlcloses this library after deinit, which unmaps the code a surviving
        // pool worker executes (its data is never destroyed; its text is), and
        // the reclamation rounds below assume no worker is still mutating the
        // managed heap. Bounded — a worker stuck in managed code cannot be
        // stopped, only reported.
        int32_t quiesced = dn2cpp_runtime_quiesce(1000);
        if (quiesced < 0)
            std::fprintf(stderr, "dn2cpp: quiesce timed out; a background thread is "
                "still running managed code - unloading this library is unsafe\n");
        else if (quiesced > 0)
            std::printf("[dn2cpp] quiesced %d background thread(s) before unload\n",
                (int)quiesced);
        // Final reclamation, before the classes are unregistered (their
        // instances must still be destroyable): engine references released
        // only by managed finalizers (RefCounted unreference / object destroy)
        // would otherwise leak structurally — Godot reports "ObjectDB
        // instances leaked at exit". Two collect+drain rounds so references
        // freed by the first round's finalizers cascade into the second.
        // Deinit runs on the engine's main thread, matching the manual
        // finalizer-drain affinity chosen at init.
        for (int round = 0; round < 2; round++)
        {
            dn2cpp_godot_sweep_anchored_refcounted();
            dn2cpp_gc_collect();
            dn2cpp_gc_drain_finalizers();
        }
        for (size_t i = g_registeredClasses.size(); i-- > 0; )
        {
            GdStringName sn = MakeStringName(g_registeredClasses[i]);
            gd_unregister_class(g_library, &sn);
        }
        g_registeredClasses.clear();

        // Construction-helper StringNames must all be freed by now; a leftover
        // means a construct path leaked an interned-string reference (not an
        // ObjectDB object, so it needs its own report for the gate to catch).
        if (g_construct_sn_live != 0)
            std::fprintf(stderr, "dn2cpp: construct StringName leak: %lld\n",
                (long long)g_construct_sn_live);
    }
}

void dn2cpp_godot_print_variants(const Dn2CppGodotVariant* args, int32_t n)
{
    if (n < 0)
        n = 0;

    if (gd_utility_print == nullptr)
    {
        // Pre-engine fallback: concatenate the arguments (no separator, like
        // Godot's print) and emit one line via the console path.
        Dn2CppString* line = nullptr;
        for (int32_t k = 0; k < n; k++)
        {
            const Dn2CppGodotVariant& a = args[k];
            Dn2CppString* piece;
            switch (a.kind)
            {
                case 1:  piece = dn2cpp_bool_to_string(a.i != 0 ? 1 : 0); break;
                case 2:  piece = dn2cpp_long_to_string(a.i); break;
                // Invariant EXPLICITLY, not through the provider-less
                // dn2cpp_double_to_string: this stands in for the engine's own
                // `print`, whose float rendering is String::num (culture-free),
                // not for Console.WriteLine, whose provider-less overload reads
                // CurrentCulture. The bare call would make GD.Print emit `1,5`
                // on a de-DE machine where the engine path beside it emits `1.5`.
                case 3:  piece = dn2cpp_double_to_string_c(a.f, dn2cpp_nfi_invariant()); break;
                case 4:  piece = a.s; break;
                default: piece = nullptr; break;
            }
            if (piece == nullptr)
                continue;
            line = line == nullptr ? piece : dn2cpp_string_concat2(line, piece);
        }
        dn2cpp_console_writeline_str(line);
        return;
    }

    // Build one engine Variant per argument. Godot's Variant is 24 bytes on
    // 64-bit; over-allocate to 32 and align like the single-arg path. A
    // zero-initialized Variant is already NIL, so EncodeVariant can construct
    // every modelled payload kind in place — the engine's print stringifies
    // whatever Variant it receives.
    std::vector<std::array<uint8_t, 32>> variants(n > 0 ? n : 1);
    std::vector<GDExtensionConstTypePtr> ptrs(n > 0 ? n : 1);

    for (int32_t k = 0; k < n; k++)
    {
        uint8_t* v = variants[k].data();
        EncodeVariant(&args[k], v);
        ptrs[k] = v;
    }

    gd_utility_print(nullptr, ptrs.data(), n);

    for (int32_t k = 0; k < n; k++)
        gd_variant_destroy(variants[k].data());
}

// Resolves and caches an engine method bind. The cache is keyed by the
// (class, method) name pointer pair — each call site passes stable string
// literals. The class must be part of the key: the compiler merges identical
// literals, so the same method name on two engine classes (Node2D.set_position
// vs Node3D.set_position) arrives with one shared `method` pointer, and a
// method-only key would hand the second class the first class's bind.
namespace
{
    // classdb_get_method_bind / variant_get_ptr_builtin_method look the bind up by
    // the StringName *value*, so the temporary StringNames a resolve builds are
    // ours to free afterwards — leaving them alive (the original code did) leaks a
    // Godot interned-string reference for every distinct method resolved.
    // Uncached resolve. A failed lookup is reported to stderr (once per cache
    // fill in practice — the null result is cached too): the callers no-op on a
    // null bind, and without the report a hardcoded-hash constant gone stale
    // against a different engine version would be an invisible no-op.
    GDExtensionMethodBindPtr ResolveBindUncached(const char* cls, const char* method, int64_t hash)
    {
        if (gd_get_method_bind == nullptr)
            return nullptr;
        GdStringName c = MakeStringName(cls);
        GdStringName m = MakeStringName(method);
        GDExtensionMethodBindPtr bind = gd_get_method_bind(&c, &m, hash);
        gd_string_name_destroy(&c);
        gd_string_name_destroy(&m);
        if (bind == nullptr)
        {
            std::fprintf(stderr, "[dn2cpp] engine method %s.%s (hash %lld) failed to resolve"
                " — stale hash / engine version mismatch?\n", cls, method, (long long)hash);
        }
        return bind;
    }

    GDExtensionMethodBindPtr ResolveBind(const char* cls, const char* method, int64_t hash)
    {
        // Sized well above the engine-method surface a typical extension touches so
        // the table stays sparse (a full table falls back to an uncached resolve).
        constexpr int kCacheSize = 512;
        static const char* keyCls[kCacheSize] = {};
        static const char* keyMethod[kCacheSize] = {};
        static GDExtensionMethodBindPtr binds[kCacheSize] = {};
        int slot = static_cast<int>(((reinterpret_cast<uintptr_t>(cls) >> 4) * 31
            + (reinterpret_cast<uintptr_t>(method) >> 4)) % kCacheSize);
        for (int i = 0; i < kCacheSize; i++)
        {
            int s = (slot + i) % kCacheSize;
            if (keyCls[s] == cls && keyMethod[s] == method)
                return binds[s];
            if (keyMethod[s] == nullptr)
            {
                GDExtensionMethodBindPtr bind = ResolveBindUncached(cls, method, hash);
                keyCls[s] = cls;
                keyMethod[s] = method;
                binds[s] = bind;
                return bind;
            }
        }
        return ResolveBindUncached(cls, method, hash);
    }

    // Uncached resolve, reporting a failed lookup like ResolveBindUncached: the
    // hand-kept builtin hashes below (size/resize/push_back/…) would otherwise
    // turn into silent no-ops if they went stale against the engine.
    GDExtensionPtrBuiltInMethod ResolveBuiltinMethodUncached(int32_t gdType, const char* method, int64_t hash)
    {
        if (gd_variant_get_ptr_builtin_method == nullptr)
            return nullptr;
        GdStringName m = MakeStringName(method);
        GDExtensionPtrBuiltInMethod bm = gd_variant_get_ptr_builtin_method(ToVariantType(gdType), &m, hash);
        gd_string_name_destroy(&m);
        if (bm == nullptr)
        {
            std::fprintf(stderr, "[dn2cpp] builtin method %s (variant type %d, hash %lld) failed to resolve"
                " — stale hash / engine version mismatch?\n", method, gdType, (long long)hash);
        }
        return bm;
    }

    // Cache of resolved builtin-method pointers, keyed by (method-name pointer,
    // variant type) — the name is a stable string literal in generated.cpp, so its
    // pointer identity is a valid key (mirrors ResolveBind).
    GDExtensionPtrBuiltInMethod ResolveBuiltinMethod(int32_t gdType, const char* method, int64_t hash)
    {
        constexpr int kCacheSize = 512;
        static const char* keys[kCacheSize] = {};
        static int32_t types[kCacheSize] = {};
        static GDExtensionPtrBuiltInMethod ms[kCacheSize] = {};
        int slot = static_cast<int>((reinterpret_cast<uintptr_t>(method) >> 4) % kCacheSize);
        for (int i = 0; i < kCacheSize; i++)
        {
            int s = (slot + i) % kCacheSize;
            if (keys[s] == method && types[s] == gdType)
                return ms[s];
            if (keys[s] == nullptr)
            {
                GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethodUncached(gdType, method, hash);
                keys[s] = method;
                types[s] = gdType;
                ms[s] = bm;
                return bm;
            }
        }
        return ResolveBuiltinMethodUncached(gdType, method, hash);
    }

    // ---- Packed-array marshalling ----
    // `size()`/`resize()` share one hash across every packed type (identical
    // signatures), so the two constants below cover all of them.
    constexpr int64_t kPackedSizeHash = 3173160232LL;
    constexpr int64_t kPackedResizeHash = 848867239LL;

    // Engine packed-array value storage. The GDExtension ABI sizes every
    // Packed*Array at 16 bytes (extension_api.json builtin_class_sizes — a
    // refcounted PackedArrayRef wrapper, NOT a bare 8-byte CowData pointer);
    // an undersized slot makes the engine's 16-byte construct/assign/destroy
    // overrun the neighbouring stack/heap bytes. The engine Array/Dictionary
    // values (8 bytes) reuse this slot too — over-sizing is harmless there.
    struct GdPacked { void* opaque[2] = { nullptr, nullptr }; };

    int32_t PackedElemSize(int32_t tag)
    {
        switch (tag)
        {
            case DN2CPP_GD_PACKED_BYTE: return 1;
            case DN2CPP_GD_PACKED_INT32: return 4;
            case DN2CPP_GD_PACKED_INT64: return 8;
            case DN2CPP_GD_PACKED_FLOAT32: return 4;
            case DN2CPP_GD_PACKED_FLOAT64: return 8;
            case DN2CPP_GD_PACKED_VECTOR2: return 8;  // 2 floats
            case DN2CPP_GD_PACKED_VECTOR3: return 12; // 3 floats
            case DN2CPP_GD_PACKED_VECTOR4: return 16; // 4 floats
            case DN2CPP_GD_PACKED_COLOR: return 16;   // RGBA floats
            default: return 0; // string is marshalled element-wise
        }
    }

    void* PackedElemPtr(int32_t tag, void* p, int64_t i)
    {
        switch (tag)
        {
            case DN2CPP_GD_PACKED_BYTE: return gd_pba_index(p, i);
            case DN2CPP_GD_PACKED_INT32: return gd_pi32a_index(p, i);
            case DN2CPP_GD_PACKED_INT64: return gd_pi64a_index(p, i);
            case DN2CPP_GD_PACKED_FLOAT32: return gd_pf32a_index(p, i);
            case DN2CPP_GD_PACKED_FLOAT64: return gd_pf64a_index(p, i);
            case DN2CPP_GD_PACKED_STRING: return gd_psa_index(p, i);
            case DN2CPP_GD_PACKED_VECTOR2: return gd_pv2a_index(p, i);
            case DN2CPP_GD_PACKED_VECTOR3: return gd_pv3a_index(p, i);
            case DN2CPP_GD_PACKED_VECTOR4: return gd_pv4a_index(p, i);
            case DN2CPP_GD_PACKED_COLOR: return gd_pca_index(p, i);
            default: return nullptr;
        }
    }

    int64_t PackedSize(int32_t tag, const void* p)
    {
        GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(tag, "size", kPackedSizeHash);
        if (bm == nullptr) return 0;
        int64_t n = 0;
        bm(const_cast<void*>(p), nullptr, &n, 0);
        return n;
    }

    void PackedResize(int32_t tag, void* p, int64_t n)
    {
        GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(tag, "resize", kPackedResizeHash);
        if (bm == nullptr) return;
        const void* a[] = { &n };
        int64_t err = 0;
        bm(p, a, &err, 1);
    }

    void* PackedToManaged(int32_t tag, const void* packed)
    {
        void* p = const_cast<void*>(packed);
        int32_t len = (int32_t)PackedSize(tag, p);
        if (len < 0) len = 0;
        // Precise array identity where the managed element has a runtime handle:
        // dn2cpp_array_ti prefers the image's emitted ti_arr_<T>, so
        // GetType()/casts/Array.Copy over the marshalled array answer as .NET's
        // byte[]/long[]/float[]/double[]/int[]/string[] would. The Vector/Color
        // packeds keep the imprecise packed handle (their elements are transpiled
        // shim structs this runtime cannot name).
        if (tag == DN2CPP_GD_PACKED_STRING)
        {
            Dn2CppArrayRef* arr = dn2cpp_newarr_ref_t(len, dn2cpp_array_ti(&dn2cpp_string_type, 1));
            // ManagedStringFromGd allocates, so an incremental cycle may blacken
            // `arr` mid-loop; each store must re-dirty its slot.
            for (int32_t i = 0; i < len; i++)
                dn2cpp_gc_store_ref(&arr->data[i], reinterpret_cast<Dn2CppObject*>(
                    ManagedStringFromGd(static_cast<const GdString*>(PackedElemPtr(tag, p, i)))));
            return arr;
        }
        int32_t es = PackedElemSize(tag);
        if (tag == DN2CPP_GD_PACKED_INT32)
        {
            Dn2CppArrayI4* arr = dn2cpp_newarr_i4_t(len, dn2cpp_array_ti(&dn2cpp_int32_type, 1));
            if (len > 0) std::memcpy(arr->data, PackedElemPtr(tag, p, 0), (size_t)len * es);
            return arr;
        }
        const Dn2CppTypeInfo* elem = tag == DN2CPP_GD_PACKED_BYTE ? &dn2cpp_byte_type
            : tag == DN2CPP_GD_PACKED_INT64 ? &dn2cpp_int64_type
            : tag == DN2CPP_GD_PACKED_FLOAT32 ? &dn2cpp_single_type
            : tag == DN2CPP_GD_PACKED_FLOAT64 ? &dn2cpp_double_type
            : nullptr;
        Dn2CppArrayN* arr = dn2cpp_newarr_n_t(len, es,
            elem != nullptr ? dn2cpp_array_ti(elem, 1) : nullptr);
        if (len > 0) std::memcpy(arr->data, PackedElemPtr(tag, p, 0), (size_t)len * es);
        return arr;
    }

    void* PackedVariantToManaged(int32_t tag, const void* variant)
    {
        GDExtensionVariantType vt = ToVariantType(tag);
        GdPacked tmp;
        gd_get_variant_to_type(vt)(&tmp, const_cast<void*>(variant));
        void* arr = PackedToManaged(tag, &tmp);
        gd_variant_get_ptr_destructor(vt)(&tmp);
        return arr;
    }

    void ManagedToPackedValue(int32_t tag, void* outPacked, void* arrPtr)
    {
        GDExtensionVariantType vt = ToVariantType(tag);
        gd_variant_get_ptr_constructor(vt, 0)(outPacked, nullptr); // empty packed array
        auto* base = static_cast<Dn2CppArray*>(arrPtr);
        int32_t len = (base != nullptr) ? base->length : 0;
        PackedResize(tag, outPacked, len);
        if (len <= 0) return;
        if (tag == DN2CPP_GD_PACKED_STRING)
        {
            auto* a = static_cast<Dn2CppArrayRef*>(arrPtr);
            for (int32_t i = 0; i < len; i++)
            {
                // The resized slot is a default (empty) String; gd_string_new_utf8
                // overwrites it (an empty String's CowData is null — nothing leaked).
                auto* slot = static_cast<GdString*>(PackedElemPtr(tag, outPacked, i));
                GdStringFromManaged(slot, reinterpret_cast<Dn2CppString*>(a->data[i]));
            }
            return;
        }
        int32_t es = PackedElemSize(tag);
        void* src = (tag == DN2CPP_GD_PACKED_INT32)
            ? (void*)static_cast<Dn2CppArrayI4*>(arrPtr)->data
            : (void*)static_cast<Dn2CppArrayN*>(arrPtr)->data;
        std::memcpy(PackedElemPtr(tag, outPacked, 0), src, (size_t)len * es);
    }

    void ManagedToPackedVariant(int32_t tag, void* r_variant, void* arrPtr)
    {
        GDExtensionVariantType vt = ToVariantType(tag);
        GdPacked tmp;
        ManagedToPackedValue(tag, &tmp, arrPtr);
        gd_get_variant_from_type(vt)(r_variant, &tmp);
        gd_variant_get_ptr_destructor(vt)(&tmp);
    }

    // ---- kind-9 (Object) decode: RefCounted lifetime guard ----
    // The engine container a Variant is decoded from (a bare Variant argument, an
    // Array element, a Dictionary value, a builtin-call return Variant) is
    // destroyed by the caller right after DecodeVariant returns. For a plain
    // Object/Node payload that is fine — the handle is a borrow of an object the
    // engine owns elsewhere. For a RefCounted payload the container's reference
    // may be the LAST one, so the decoded handle would dangle instantly. The
    // decode therefore takes one reference() of its own and parks it in a small
    // GC-visible guard object stored in the Variant's refGuard field: the guard
    // lives exactly as long as the managed Variant is reachable, and its
    // finalizer returns the reference (destroying the engine object if it held
    // the last one, mirroring ~RefCounted()). Detection uses the ClassDB class
    // tag + object_cast_to — deliberately NOT the extension reference_func/
    // unreference_func callbacks, whose observed firing is unreliable for
    // Variant hand-off paths.
    struct Dn2CppGodotVariantRefGuard
    {
        Dn2CppObject header;
        void* handle;
    };

    void VariantRefGuardFinalize(Dn2CppObject* obj)
    {
        void* h = reinterpret_cast<Dn2CppGodotVariantRefGuard*>(obj)->handle;
        if (h == nullptr)
            return;
        // unreference() returns true when the count reached zero: the guard held
        // the last reference, so the engine object must be destroyed (the same
        // contract the RefCounted shim finalizer follows).
        uint8_t hitZero = 0;
        gd_method_bind_ptrcall(ResolveBind("RefCounted", "unreference", 2240911060LL), h, nullptr, &hitZero);
        if (hitZero != 0 && gd_object_destroy != nullptr)
            gd_object_destroy(static_cast<GDExtensionObjectPtr>(h));
    }

    Dn2CppTypeInfo g_variant_refguard_type = [] {
        Dn2CppTypeInfo ti{};
        ti.name = "Godot.<VariantRefGuard>";
        ti.instanceSize = static_cast<int32_t>(sizeof(Dn2CppGodotVariantRefGuard));
        ti.finalize = &VariantRefGuardFinalize;
        // Interned Type companion (lock-free typeof/GetType); taking the
        // enclosing variable's address inside its own initializer is fine —
        // only the (constant) address is used, never the value.
        static const Dn2CppType tyObj = { { &dn2cpp_type_type }, &g_variant_refguard_type };
        ti.typeObject = &tyObj;
        return ti;
    }();

    bool IsRefCountedHandle(void* h)
    {
        if (gd_object_cast_to == nullptr || gd_classdb_get_class_tag == nullptr)
            return false;
        static void* tag = [] {
            GdStringName sn = MakeStringName("RefCounted");
            void* t = gd_classdb_get_class_tag(&sn);
            gd_string_name_destroy(&sn);
            return t;
        }();
        return tag != nullptr && gd_object_cast_to(h, tag) != nullptr;
    }

    Dn2CppObject* MakeVariantRefGuard(void* h)
    {
        // Take the reference BEFORE the GC allocation: the allocation can run a
        // collection, and until reference() lands the source container is the
        // only thing keeping the object alive.
        uint8_t ok = 0;
        gd_method_bind_ptrcall(ResolveBind("RefCounted", "reference", 2240911060LL), h, nullptr, &ok);
        auto* g = static_cast<Dn2CppGodotVariantRefGuard*>(dn2cpp_alloc(sizeof(Dn2CppGodotVariantRefGuard)));
        g->header.type = &g_variant_refguard_type;
        g->handle = h;
        dn2cpp_register_finalizer(&g->header);
        return &g->header;
    }

    // ---- General Variant <-> shim Variant struct ----

    // Transpiled type-infos for the payload kinds the codec must allocate on
    // decode: the boxed shim struct for the big-POD kinds (13..20), the precise
    // ti_arr_<elem> for the packed-array kinds (21..30), the container wrapper
    // classes for kinds 31/32. Filled by generated code via
    // dn2cpp_godot_variant_register_type; an unregistered kind decodes as nil
    // (the compilation has no type to receive that payload anyway).
    const Dn2CppTypeInfo* g_variant_payload_ti[40] = {};

    // Column-major shim <-> row-major native float permutation for the
    // transpose matrix payloads (Basis n=9, Transform3D n=12 — origin floats
    // 9..11 pass through). An involution, so it serves both directions;
    // mirrors GdTag.BigTypeFloatMap.
    void TransposeBasisFloats(const float* src, float* dst, int32_t n)
    {
        static const int32_t map[12] = { 0, 3, 6, 1, 4, 7, 2, 5, 8, 9, 10, 11 };
        for (int32_t k = 0; k < n; k++)
            dst[k] = src[map[k]];
    }

    // Shape of a big-POD Variant payload kind (13..20): its engine variant
    // type, flat-float count, and whether the shim layout is the 3x3 transpose
    // of Godot's (Basis / Transform3D) rather than identical to it.
    struct VariantPodPayload { GDExtensionVariantType vt; int32_t floats; bool transpose; };

    bool VariantPodPayloadInfo(int32_t kind, VariantPodPayload* out)
    {
        switch (kind)
        {
            case DN2CPP_GDV_RECT2: *out = { GDEXTENSION_VARIANT_TYPE_RECT2, 4, false }; return true;
            case DN2CPP_GDV_PLANE: *out = { GDEXTENSION_VARIANT_TYPE_PLANE, 4, false }; return true;
            case DN2CPP_GDV_AABB: *out = { GDEXTENSION_VARIANT_TYPE_AABB, 6, false }; return true;
            case DN2CPP_GDV_QUATERNION: *out = { GDEXTENSION_VARIANT_TYPE_QUATERNION, 4, false }; return true;
            case DN2CPP_GDV_TRANSFORM2D: *out = { GDEXTENSION_VARIANT_TYPE_TRANSFORM2D, 6, false }; return true;
            case DN2CPP_GDV_BASIS: *out = { GDEXTENSION_VARIANT_TYPE_BASIS, 9, true }; return true;
            case DN2CPP_GDV_TRANSFORM3D: *out = { GDEXTENSION_VARIANT_TYPE_TRANSFORM3D, 12, true }; return true;
            case DN2CPP_GDV_PROJECTION: *out = { GDEXTENSION_VARIANT_TYPE_PROJECTION, 16, false }; return true;
            default: return false;
        }
    }

    // Payload kind for an engine variant type handled by the box/array decode
    // paths below, or 0 for anything the specific DecodeVariant cases (or the
    // nil fallback) cover.
    int32_t VariantPodKindOf(GDExtensionVariantType t)
    {
        switch (t)
        {
            case GDEXTENSION_VARIANT_TYPE_RECT2: return DN2CPP_GDV_RECT2;
            case GDEXTENSION_VARIANT_TYPE_PLANE: return DN2CPP_GDV_PLANE;
            case GDEXTENSION_VARIANT_TYPE_AABB: return DN2CPP_GDV_AABB;
            case GDEXTENSION_VARIANT_TYPE_QUATERNION: return DN2CPP_GDV_QUATERNION;
            case GDEXTENSION_VARIANT_TYPE_TRANSFORM2D: return DN2CPP_GDV_TRANSFORM2D;
            case GDEXTENSION_VARIANT_TYPE_BASIS: return DN2CPP_GDV_BASIS;
            case GDEXTENSION_VARIANT_TYPE_TRANSFORM3D: return DN2CPP_GDV_TRANSFORM3D;
            case GDEXTENSION_VARIANT_TYPE_PROJECTION: return DN2CPP_GDV_PROJECTION;
            default: return 0;
        }
    }

    int32_t VariantPackedKindOf(GDExtensionVariantType t)
    {
        switch (t)
        {
            case GDEXTENSION_VARIANT_TYPE_PACKED_BYTE_ARRAY: return DN2CPP_GDV_PACKED_BYTE;
            case GDEXTENSION_VARIANT_TYPE_PACKED_INT32_ARRAY: return DN2CPP_GDV_PACKED_INT32;
            case GDEXTENSION_VARIANT_TYPE_PACKED_INT64_ARRAY: return DN2CPP_GDV_PACKED_INT64;
            case GDEXTENSION_VARIANT_TYPE_PACKED_FLOAT32_ARRAY: return DN2CPP_GDV_PACKED_FLOAT32;
            case GDEXTENSION_VARIANT_TYPE_PACKED_FLOAT64_ARRAY: return DN2CPP_GDV_PACKED_FLOAT64;
            case GDEXTENSION_VARIANT_TYPE_PACKED_STRING_ARRAY: return DN2CPP_GDV_PACKED_STRING;
            case GDEXTENSION_VARIANT_TYPE_PACKED_VECTOR2_ARRAY: return DN2CPP_GDV_PACKED_VECTOR2;
            case GDEXTENSION_VARIANT_TYPE_PACKED_VECTOR3_ARRAY: return DN2CPP_GDV_PACKED_VECTOR3;
            case GDEXTENSION_VARIANT_TYPE_PACKED_VECTOR4_ARRAY: return DN2CPP_GDV_PACKED_VECTOR4;
            case GDEXTENSION_VARIANT_TYPE_PACKED_COLOR_ARRAY: return DN2CPP_GDV_PACKED_COLOR;
            default: return 0;
        }
    }

    // A big-POD engine Variant -> a boxed shim struct in out->obj (nil when the
    // compilation never emitted the shim type — there is nothing to unbox into).
    bool DecodePodPayload(GDExtensionVariantType t, void* vv, Dn2CppGodotVariant* out)
    {
        int32_t kind = VariantPodKindOf(t);
        if (kind == 0)
            return false;
        const Dn2CppTypeInfo* ti = g_variant_payload_ti[kind];
        if (ti == nullptr)
            return true; // out stays nil
        VariantPodPayload p{};
        VariantPodPayloadInfo(kind, &p);
        float nat[16] = {};
        gd_get_variant_to_type(p.vt)(nat, vv);
        float shim[16];
        if (p.transpose)
            TransposeBasisFloats(nat, shim, p.floats);
        else
            std::memcpy(shim, nat, static_cast<size_t>(p.floats) * sizeof(float));
        out->kind = kind;
        out->obj = dn2cpp_box(ti, shim, static_cast<size_t>(p.floats) * sizeof(float));
        return true;
    }

    // A packed-array engine Variant -> a fresh managed array in out->obj,
    // stamped with its precise T[] type-info so managed casts see the exact
    // array type (nil when the compilation never mentioned that array type).
    bool DecodePackedPayload(GDExtensionVariantType t, void* vv, Dn2CppGodotVariant* out)
    {
        int32_t kind = VariantPackedKindOf(t);
        if (kind == 0)
            return false;
        const Dn2CppTypeInfo* ti = g_variant_payload_ti[kind];
        if (ti == nullptr)
            return true; // out stays nil
        void* arr = PackedVariantToManaged(kind - DN2CPP_GDV_PACKED_TAG_OFFSET, vv);
        if (arr != nullptr)
            static_cast<Dn2CppObject*>(arr)->type = ti;
        out->kind = kind;
        out->obj = static_cast<Dn2CppObject*>(arr);
        return true;
    }

    // Decode an engine Variant into the runtime Dn2CppGodotVariant POD, whose
    // layout is identical to the shim's t_Godot_Variant. Covers every payload
    // kind the shim Variant models (see the kind list in dn2cpp_godot.h);
    // Array/Dictionary/Callable/Signal payloads decode as nil for now.
    void DecodeVariant(const void* v, Dn2CppGodotVariant* out)
    {
        *out = {};
        void* vv = const_cast<void*>(v);
        GDExtensionVariantType t = gd_variant_get_type != nullptr
            ? gd_variant_get_type(vv) : GDEXTENSION_VARIANT_TYPE_NIL;
        switch (t)
        {
            case GDEXTENSION_VARIANT_TYPE_BOOL:
            {
                uint8_t b = 0; gd_bool_from_variant(&b, vv);
                out->kind = DN2CPP_GDV_BOOL; out->i = b;
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_INT:
            {
                int64_t i = 0; gd_int_from_variant(&i, vv);
                out->kind = DN2CPP_GDV_INT; out->i = i;
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_FLOAT:
            {
                double d = 0; gd_float_from_variant(&d, vv);
                out->kind = DN2CPP_GDV_FLOAT; out->f = d;
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_STRING:
            {
                GdString gs{}; gd_string_from_variant(&gs, vv);
                out->kind = DN2CPP_GDV_STRING; out->s = ManagedStringFromGd(&gs);
                gd_string_destroy(&gs);
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_VECTOR3:
            {
                float vec[3] = {}; gd_vector3_from_variant(vec, vv);
                out->kind = DN2CPP_GDV_VECTOR3; out->vx = vec[0]; out->vy = vec[1]; out->vz = vec[2];
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_VECTOR2:
            {
                float vec[2] = {}; gd_vector2_from_variant(vec, vv);
                out->kind = DN2CPP_GDV_VECTOR2; out->vx = vec[0]; out->vy = vec[1];
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_VECTOR4:
            {
                float vec[4] = {}; gd_vector4_from_variant(vec, vv);
                out->kind = DN2CPP_GDV_VECTOR4; out->vx = vec[0]; out->vy = vec[1]; out->vz = vec[2]; out->vw = vec[3];
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_COLOR:
            {
                float vec[4] = {}; gd_color_from_variant(vec, vv);
                out->kind = DN2CPP_GDV_COLOR; out->vx = vec[0]; out->vy = vec[1]; out->vz = vec[2]; out->vw = vec[3];
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_OBJECT:
            {
                // Carry the engine Object as its handle in the `i` field. A plain
                // Object/Node stays a borrow; a RefCounted gets its own reference
                // parked in the guard (see the ownership notes above) because the
                // source container dies right after this decode.
                void* h = nullptr;
                if (gd_object_from_variant != nullptr) gd_object_from_variant(&h, vv);
                out->kind = DN2CPP_GDV_OBJECT; out->i = reinterpret_cast<int64_t>(h);
                if (h != nullptr && IsRefCountedHandle(h))
                    out->refGuard = MakeVariantRefGuard(h);
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_RID:
            {
                int64_t id = 0;
                gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_RID)(&id, vv);
                out->kind = DN2CPP_GDV_RID; out->i = id;
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_STRING_NAME:
            {
                // Carried as its text plus the kind tag — the shim's string-family
                // C# type is `string`, so only the tag keeps the engine identity.
                GdStringName sn{};
                gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_STRING_NAME)(&sn, vv);
                out->kind = DN2CPP_GDV_STRING_NAME; out->s = ManagedFromStringLike(&sn, /*String ctor from StringName*/ 2);
                gd_string_name_destroy(&sn);
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_NODE_PATH:
            {
                GdNodePath np{};
                gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_NODE_PATH)(&np, vv);
                out->kind = DN2CPP_GDV_NODE_PATH; out->s = ManagedFromStringLike(&np, /*String ctor from NodePath*/ 3);
                gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_NODE_PATH)(&np);
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_CALLABLE:
            case GDEXTENSION_VARIANT_TYPE_SIGNAL:
            {
                // Decodes to a boxed shim struct (like the big PODs). One of
                // our own delegate-backed callables recovers its pinned
                // userdata POD, so the carried delegates survive the round
                // trip; a standard callable / signal decodes to target+method
                // (owner+name). Nil when the shim type was never emitted.
                bool isSignal = t == GDEXTENSION_VARIANT_TYPE_SIGNAL;
                int32_t kind = isSignal ? DN2CPP_GDV_SIGNAL : DN2CPP_GDV_CALLABLE;
                const Dn2CppTypeInfo* ti = g_variant_payload_ti[kind];
                if (ti == nullptr)
                    break; // out stays nil
                alignas(8) uint8_t cv[16] = {};
                gd_get_variant_to_type(t)(cv, vv);
                if (isSignal)
                {
                    Dn2CppGodotSignalShim pod{};
                    DecodeSignalValue(cv, &pod);
                    out->obj = dn2cpp_box(ti, &pod, sizeof(pod));
                }
                else
                {
                    Dn2CppGodotCallableShim pod{};
                    DecodeCallableValue(cv, &pod);
                    out->obj = dn2cpp_box(ti, &pod, sizeof(pod));
                }
                out->kind = kind;
                gd_variant_get_ptr_destructor(t)(cv);
                break;
            }
            case GDEXTENSION_VARIANT_TYPE_ARRAY:
            case GDEXTENSION_VARIANT_TYPE_DICTIONARY:
            {
                // A container payload decodes as a fresh finalizer-owned
                // Godot.Collections wrapper SHARING the carried container (the
                // to-type conversion bumps its refcount), so identity semantics
                // survive: mutations through the wrapper stay visible to every
                // other holder. Nil when the wrapper class was never emitted.
                bool isDict = t == GDEXTENSION_VARIANT_TYPE_DICTIONARY;
                int32_t kind = isDict ? DN2CPP_GDV_DICTIONARY : DN2CPP_GDV_ARRAY;
                const Dn2CppTypeInfo* ti = g_variant_payload_ti[kind];
                if (ti == nullptr)
                    break; // out stays nil
                void* q = NewContainerSlot();
                gd_get_variant_to_type(t)(q, vv);
                out->kind = kind;
                out->obj = MakeContainerWrapper(ti, q);
                break;
            }
            default:
                // Big-POD payloads decode to a boxed shim struct, packed arrays
                // to a managed array; anything else (Callable/Signal — later
                // slices) stays nil.
                if (DecodePodPayload(t, vv, out))
                    break;
                if (DecodePackedPayload(t, vv, out))
                    break;
                out->kind = DN2CPP_GDV_NIL;
                break;
        }
    }

    // Construct an engine Variant into `slot` (an uninitialised / nil Variant) from
    // the shim Variant POD. nil leaves the slot as the constructed nil.
    void EncodeVariant(const Dn2CppGodotVariant* in, void* slot)
    {
        switch (in->kind)
        {
            case DN2CPP_GDV_BOOL: { uint8_t b = in->i != 0 ? 1 : 0; gd_variant_from_bool(slot, &b); break; }
            case DN2CPP_GDV_INT: { int64_t i = in->i; gd_variant_from_int(slot, &i); break; }
            case DN2CPP_GDV_FLOAT: { double d = in->f; gd_variant_from_float(slot, &d); break; }
            case DN2CPP_GDV_STRING: VariantFromManagedString(slot, in->s); break;
            case DN2CPP_GDV_VECTOR3: { float vec[3] = { in->vx, in->vy, in->vz }; gd_variant_from_vector3(slot, vec); break; }
            case DN2CPP_GDV_VECTOR2: { float vec[2] = { in->vx, in->vy }; gd_variant_from_vector2(slot, vec); break; }
            case DN2CPP_GDV_VECTOR4: { float vec[4] = { in->vx, in->vy, in->vz, in->vw }; gd_variant_from_vector4(slot, vec); break; }
            case DN2CPP_GDV_COLOR: { float vec[4] = { in->vx, in->vy, in->vz, in->vw }; gd_variant_from_color(slot, vec); break; }
            // Constructing an engine Variant from an Object handle makes the
            // engine take its own reference for RefCounted (released when the
            // Variant is destroyed) — no manual reference here.
            case DN2CPP_GDV_OBJECT: { void* h = reinterpret_cast<void*>(in->i); gd_variant_from_object(slot, &h); break; }
            case DN2CPP_GDV_RID: { int64_t id = in->i; gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_RID)(slot, &id); break; }
            case DN2CPP_GDV_STRING_NAME:
            {
                if (in->s == nullptr) break;
                GdStringName sn{};
                StringLikeFromManaged(&sn, GDEXTENSION_VARIANT_TYPE_STRING_NAME, in->s);
                gd_variant_from_string_name(slot, &sn);
                gd_string_name_destroy(&sn);
                break;
            }
            case DN2CPP_GDV_NODE_PATH:
            {
                if (in->s == nullptr) break;
                GdNodePath np{};
                StringLikeFromManaged(&np, GDEXTENSION_VARIANT_TYPE_NODE_PATH, in->s);
                gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_NODE_PATH)(slot, &np);
                gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_NODE_PATH)(&np);
                break;
            }
            default:
            {
                // Big-POD kinds: the boxed shim struct's flat floats, transposed
                // into Godot's native order for Basis/Transform3D.
                VariantPodPayload p{};
                if (VariantPodPayloadInfo(in->kind, &p))
                {
                    if (in->obj == nullptr) break; // nil
                    const float* shim = reinterpret_cast<const float*>(in->obj + 1);
                    float nat[16];
                    if (p.transpose)
                        TransposeBasisFloats(shim, nat, p.floats);
                    else
                        std::memcpy(nat, shim, static_cast<size_t>(p.floats) * sizeof(float));
                    gd_get_variant_from_type(p.vt)(slot, nat);
                    break;
                }
                // Packed-array kinds: build the engine packed value from the
                // carried managed array (kind - 4 == DN2CPP_GD_PACKED_*).
                if (in->kind >= DN2CPP_GDV_PACKED_BYTE && in->kind <= DN2CPP_GDV_PACKED_COLOR)
                {
                    if (in->obj == nullptr) break; // nil
                    ManagedToPackedVariant(in->kind - DN2CPP_GDV_PACKED_TAG_OFFSET, slot, in->obj);
                    break;
                }
                // Container kinds: the Variant shares the wrapper's engine
                // container value (from-type construction bumps its refcount).
                if (in->kind == DN2CPP_GDV_ARRAY || in->kind == DN2CPP_GDV_DICTIONARY)
                {
                    void* q = ContainerHandleOf(in->obj);
                    if (q == nullptr) break; // nil
                    gd_get_variant_from_type(in->kind == DN2CPP_GDV_DICTIONARY
                        ? GDEXTENSION_VARIANT_TYPE_DICTIONARY
                        : GDEXTENSION_VARIANT_TYPE_ARRAY)(slot, q);
                    break;
                }
                // Callable/Signal kinds: build a temporary engine value from
                // the boxed shim struct, wrap it in the Variant, destroy it
                // (the Variant holds its own reference to a custom callable).
                if (in->kind == DN2CPP_GDV_CALLABLE || in->kind == DN2CPP_GDV_SIGNAL)
                {
                    if (in->obj == nullptr) break; // nil
                    bool isSignal = in->kind == DN2CPP_GDV_SIGNAL;
                    GDExtensionVariantType vt = isSignal
                        ? GDEXTENSION_VARIANT_TYPE_SIGNAL
                        : GDEXTENSION_VARIANT_TYPE_CALLABLE;
                    alignas(8) uint8_t cv[16] = {};
                    if (isSignal)
                        BuildSignalValue(cv, reinterpret_cast<const Dn2CppGodotSignalShim*>(in->obj + 1));
                    else
                        BuildCallableValue(cv, reinterpret_cast<const Dn2CppGodotCallableShim*>(in->obj + 1));
                    gd_get_variant_from_type(vt)(slot, cv);
                    gd_variant_get_ptr_destructor(vt)(cv);
                    break;
                }
                break; // nil: slot stays nil
            }
        }
    }

    // ---- Engine Callable/Signal <-> the shim Callable/Signal structs ----
    // Both are 16-byte engine value types. A standard Callable (target +
    // method) and a Signal go through the builtin (Object, StringName)
    // constructor (index 2, the same one the old connect intrinsic used); a
    // delegate-backed Callable becomes a *custom* engine callable whose
    // userdata is a pinned copy of the shim POD — GC-scanned (uncollectable),
    // so the carried delegates stay alive exactly as long as the engine holds
    // the callable, and recoverable on decode so a round trip preserves them.
    // Builtin-method hashes from extension_api.json; Signal's get_object /
    // get_name share Callable's get_object / get_method hashes (identical
    // signatures).
    constexpr int64_t kCallableGetObjectHash = 4008621732LL;
    constexpr int64_t kCallableGetMethodHash = 1825232092LL;

    // The generated dispatch bridge (gw_callable_dispatch in generated.cpp),
    // re-entering managed Godot.Callable.__Dispatch. Null until
    // dn2cpp_godot_set_callable_dispatch runs in dn2cpp_godot_init_managed.
    void (*g_callable_dispatch)(void* callableShim, void* variantArgs) = nullptr;

    // Wraps a borrowed engine object handle in a fresh Godot.Object shim (the
    // registered slot-35 type-info), for a decoded standard Callable's target /
    // Signal's owner. Null handle — or no registered shim type — wraps as null.
    // Borrowed like the engine's own Callable semantics: a Callable stores an
    // ObjectID, not a strong reference, so neither does the wrap.
    // Shared shim construction: allocate an instance of `ti` and plant `handle`
    // in its leading field (right after the object header) — the layout every
    // engine-object shim and container wrapper follows.
    Dn2CppObject* AllocShimWithHandle(const Dn2CppTypeInfo* ti, void* handle)
    {
        auto* o = static_cast<Dn2CppObject*>(dn2cpp_alloc(static_cast<size_t>(ti->instanceSize)));
        o->type = ti;
        *reinterpret_cast<void**>(reinterpret_cast<char*>(o) + sizeof(Dn2CppObject)) = handle;
        return o;
    }

    Dn2CppObject* WrapBorrowedObjectShim(void* handle)
    {
        const Dn2CppTypeInfo* ti = g_variant_payload_ti[DN2CPP_GDV_OBJECT_SHIM_SLOT];
        if (handle == nullptr || ti == nullptr)
            return nullptr;
        return AllocShimWithHandle(ti, handle);
    }

    // The engine handle a shim engine-object reference carries (its leading
    // field, right after the object header), or null.
    void* EngineHandleOf(const Dn2CppObject* shim)
    {
        if (shim == nullptr)
            return nullptr;
        return *reinterpret_cast<void* const*>(
            reinterpret_cast<const char*>(shim) + sizeof(Dn2CppObject));
    }

    // call_func of a delegate-backed custom callable: decode the engine
    // Variant args into a managed Variant[] and re-enter managed code through
    // the generated dispatch bridge. The engine may invoke connected callables
    // from threads it spawned itself, so register with the collector first
    // (the GenericCall convention). r_return stays the pre-constructed nil —
    // the supported delegate shapes return void.
    void CallableCustomCall(void* userdata, const GDExtensionConstVariantPtr* p_args,
                            GDExtensionInt argc, GDExtensionVariantPtr r_return,
                            GDExtensionCallError* r_error)
    {
        (void)r_return;
        EnsureGcThreadRegistered();
        int32_t n = argc > 0 ? static_cast<int32_t>(argc) : 0;
        Dn2CppArrayN* arr = dn2cpp_newarr_n(n, (int32_t)sizeof(Dn2CppGodotVariant));
        auto* dst = reinterpret_cast<Dn2CppGodotVariant*>(arr->data);
        // DecodeVariant allocates, so an incremental cycle may blacken `arr`
        // mid-loop; each store must re-dirty its slot or the ref is lost.
        for (int32_t i = 0; i < n; i++)
        {
            DecodeVariant(p_args[i], &dst[i]);
            dn2cpp_gc_write_barrier(&dst[i]);
        }
        if (g_callable_dispatch != nullptr)
        {
            try
            {
                g_callable_dispatch(userdata, arr);
            }
            catch (Dn2CppException& ex)
            {
                dn2cpp_report_boundary_exception(ex.obj, "a Callable dispatch");
                if (r_error != nullptr)
                    r_error->error = GDEXTENSION_CALL_ERROR_INVALID_METHOD;
                return;
            }
        }
        if (r_error != nullptr)
            r_error->error = GDEXTENSION_CALL_OK;
    }

    // free_func: the engine dropped its last reference to the custom callable;
    // release the pinned userdata POD (un-rooting the carried delegates).
    void CallableCustomFree(void* userdata)
    {
        dn2cpp_free_pinned(userdata);
    }

    void BuildCallableValue(void* out16, const Dn2CppGodotCallableShim* shim)
    {
        bool hasDelegate = shim->action != nullptr
            || shim->actionVariant != nullptr || shim->actionVariants != nullptr;
        if (hasDelegate && gd_callable_custom_create2 != nullptr)
        {
            auto* ud = static_cast<Dn2CppGodotCallableShim*>(
                dn2cpp_alloc_pinned(sizeof(Dn2CppGodotCallableShim)));
            *ud = *shim;
            dn2cpp_gc_write_barrier(ud);
            GDExtensionCallableCustomInfo2 info = {};
            info.callable_userdata = ud;
            info.token = g_library;
            info.object_id = 0; // delegate lifetime is the userdata's, not an object's
            info.call_func = &CallableCustomCall;
            info.free_func = &CallableCustomFree;
            gd_callable_custom_create2(out16, &info);
            return;
        }
        void* h = EngineHandleOf(shim->target);
        if (h != nullptr && shim->method != nullptr)
        {
            // Constructor index 2: Callable(Object target, StringName method).
            GdStringName sn = MakeStringNameFromManaged(shim->method);
            const void* a[] = { &h, &sn };
            gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_CALLABLE, 2)(out16, a);
            gd_string_name_destroy(&sn);
            return;
        }
        // Default (empty) callable — a default shim struct maps to it.
        gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_CALLABLE, 0)(out16, nullptr);
    }

    void DecodeCallableValue(const void* cv, Dn2CppGodotCallableShim* out)
    {
        *out = {};
        // One of our own delegate-backed callables: recover the pinned POD it
        // carries (keyed by the library token — a foreign extension's custom
        // callable yields null). The copy shares the delegate references, so
        // the decoded struct is invocable-equivalent to the original.
        if (gd_callable_custom_get_userdata != nullptr)
        {
            void* ud = gd_callable_custom_get_userdata(cv, g_library);
            if (ud != nullptr)
            {
                *out = *static_cast<const Dn2CppGodotCallableShim*>(ud);
                return;
            }
        }
        // Standard callable: target + method via the Callable builtins.
        GDExtensionPtrBuiltInMethod getObj =
            ResolveBuiltinMethod(DN2CPP_GD_CALLABLE, "get_object", kCallableGetObjectHash);
        GDExtensionPtrBuiltInMethod getMethod =
            ResolveBuiltinMethod(DN2CPP_GD_CALLABLE, "get_method", kCallableGetMethodHash);
        void* h = nullptr;
        if (getObj != nullptr)
            getObj(const_cast<void*>(cv), nullptr, &h, 0);
        out->target = WrapBorrowedObjectShim(h);
        if (getMethod != nullptr)
        {
            GdStringName sn{};
            getMethod(const_cast<void*>(cv), nullptr, &sn, 0);
            out->method = ManagedFromStringLike(&sn, /*String ctor from StringName*/ 2);
            gd_string_name_destroy(&sn);
        }
    }

    void BuildSignalValue(void* out16, const Dn2CppGodotSignalShim* shim)
    {
        void* h = EngineHandleOf(shim->owner);
        if (h != nullptr && shim->name != nullptr)
        {
            // Constructor index 2: Signal(Object object, StringName signal).
            GdStringName sn = MakeStringNameFromManaged(shim->name);
            const void* a[] = { &h, &sn };
            gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_SIGNAL, 2)(out16, a);
            gd_string_name_destroy(&sn);
            return;
        }
        gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_SIGNAL, 0)(out16, nullptr);
    }

    void DecodeSignalValue(const void* sv, Dn2CppGodotSignalShim* out)
    {
        *out = {};
        GDExtensionPtrBuiltInMethod getObj =
            ResolveBuiltinMethod(DN2CPP_GD_SIGNAL, "get_object", kCallableGetObjectHash);
        GDExtensionPtrBuiltInMethod getName =
            ResolveBuiltinMethod(DN2CPP_GD_SIGNAL, "get_name", kCallableGetMethodHash);
        void* h = nullptr;
        if (getObj != nullptr)
            getObj(const_cast<void*>(sv), nullptr, &h, 0);
        out->owner = WrapBorrowedObjectShim(h);
        if (getName != nullptr)
        {
            GdStringName sn{};
            getName(const_cast<void*>(sv), nullptr, &sn, 0);
            out->name = ManagedFromStringLike(&sn, /*String ctor from StringName*/ 2);
            gd_string_name_destroy(&sn);
        }
    }

    // ---- Heterogeneous Array <-> managed Godot.Variant[] ----
    // The managed element type is the shim Variant struct, laid out identically to
    // Dn2CppGodotVariant, so the array is a Dn2CppArrayN of Dn2CppGodotVariant.
    void* ArrayToManaged(const void* arrayValue)
    {
        void* p = const_cast<void*>(arrayValue);
        int32_t len = (int32_t)PackedSize(DN2CPP_GD_ARRAY, p);
        if (len < 0) len = 0;
        Dn2CppArrayN* arr = dn2cpp_newarr_n(len, (int32_t)sizeof(Dn2CppGodotVariant));
        auto* dst = reinterpret_cast<Dn2CppGodotVariant*>(arr->data);
        // DecodeVariant allocates, so an incremental cycle may blacken `arr`
        // mid-loop; each store must re-dirty its slot or the ref is lost.
        for (int32_t i = 0; i < len; i++)
        {
            DecodeVariant(gd_array_index(p, i), &dst[i]);
            dn2cpp_gc_write_barrier(&dst[i]);
        }
        return arr;
    }

    void ManagedToArrayValue(void* outArray, void* arrPtr)
    {
        gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_ARRAY, 0)(outArray, nullptr); // empty Array
        auto* a = static_cast<Dn2CppArrayN*>(arrPtr);
        int32_t len = (a != nullptr) ? a->length : 0;
        PackedResize(DN2CPP_GD_ARRAY, outArray, len);
        if (len <= 0) return;
        auto* src = reinterpret_cast<Dn2CppGodotVariant*>(a->data);
        for (int32_t i = 0; i < len; i++)
            EncodeVariant(&src[i], gd_array_index(outArray, i));
    }

    void* ArrayVariantToManaged(const void* variant)
    {
        GdPacked tmp;
        gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_ARRAY)(&tmp, const_cast<void*>(variant));
        void* arr = ArrayToManaged(&tmp);
        gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_ARRAY)(&tmp);
        return arr;
    }

    void ManagedToArrayVariant(void* r_variant, void* arrPtr)
    {
        GdPacked tmp;
        ManagedToArrayValue(&tmp, arrPtr);
        gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_ARRAY)(r_variant, &tmp);
        gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_ARRAY)(&tmp);
    }

    // ---- Engine containers <-> the Godot.Collections wrapper classes ----
    // A wrapper (Godot.Collections.Array / Array<T> / Dictionary) is a managed
    // class whose leading field (right after the Dn2CppObject header, like the
    // engine-object shims' handle) points to a pinned runtime-allocated slot
    // holding the 8-byte engine container value. The container itself is
    // reference-counted engine memory: crossing the boundary shares it (a copy
    // construction bumps its refcount), never element-copies, so identity
    // semantics survive the round trip. The engine builtin-method hashes below
    // come from extension_api.json (size/resize live with the packed constants).
    constexpr int64_t kContainerClearHash = 3218959716LL;
    constexpr int64_t kArrayPushBackHash = 3316032543LL;
    constexpr int64_t kArraySetHash = 3798478031LL;
    constexpr int64_t kContainerHasHash = 3680194679LL;
    constexpr int64_t kDictKeysHash = 4144163970LL;

    GDExtensionVariantType ContainerVt(int32_t tag)
    {
        return tag == DN2CPP_GD_DICTIONARY
            ? GDEXTENSION_VARIANT_TYPE_DICTIONARY
            : GDEXTENSION_VARIANT_TYPE_ARRAY;
    }

    // The wrapper's engine-value slot (its leading handle field), or null —
    // the same leading-field layout the engine-object shims use.
    void* ContainerHandleOf(const Dn2CppObject* wrapper)
    {
        return EngineHandleOf(wrapper);
    }

    // A fresh pinned engine-container slot, zeroed (a zeroed 8-byte container
    // value is a valid null/empty target for the engine's assign/convert paths,
    // the same convention the packed marshalling relies on).
    void* NewContainerSlot()
    {
        void* p = dn2cpp_alloc_pinned(sizeof(GdPacked));
        std::memset(p, 0, sizeof(GdPacked));
        return p;
    }

    // Allocates a managed wrapper of the registered type around an engine value
    // slot whose ownership transfers to it (the wrapper's finalizer destroys the
    // value and frees the slot).
    Dn2CppObject* MakeContainerWrapper(const Dn2CppTypeInfo* ti, void* valueSlot)
    {
        Dn2CppObject* w = AllocShimWithHandle(ti, valueSlot);
        dn2cpp_register_finalizer(w);
        return w;
    }

    const Dn2CppTypeInfo* ContainerWrapperTi(int32_t tag)
    {
        return g_variant_payload_ti[tag == DN2CPP_GD_DICTIONARY ? DN2CPP_GDV_DICTIONARY : DN2CPP_GDV_ARRAY];
    }

    // Engine container value -> a fresh managed wrapper sharing it (copy ctor
    // index 1 bumps the container's refcount). Null when the wrapper class was
    // never emitted (nothing could receive the value anyway).
    void* ContainerToManaged(int32_t tag, const void* value)
    {
        const Dn2CppTypeInfo* ti = ContainerWrapperTi(tag);
        if (ti == nullptr)
            return nullptr;
        void* q = NewContainerSlot();
        const void* a[] = { value };
        gd_variant_get_ptr_constructor(ContainerVt(tag), 1)(q, a);
        return MakeContainerWrapper(ti, q);
    }

    void* ContainerVariantToManaged(int32_t tag, const void* variant)
    {
        const Dn2CppTypeInfo* ti = ContainerWrapperTi(tag);
        if (ti == nullptr)
            return nullptr;
        void* q = NewContainerSlot();
        gd_get_variant_to_type(ContainerVt(tag))(q, const_cast<void*>(variant));
        return MakeContainerWrapper(ti, q);
    }

    // Managed wrapper -> an engine container value built into the caller's slot
    // (sharing the wrapper's container; a null wrapper builds an empty one).
    void ManagedToContainerValue(int32_t tag, void* outValue, void* wrapper)
    {
        void* q = ContainerHandleOf(static_cast<Dn2CppObject*>(wrapper));
        if (q == nullptr)
        {
            gd_variant_get_ptr_constructor(ContainerVt(tag), 0)(outValue, nullptr);
            return;
        }
        const void* a[] = { q };
        gd_variant_get_ptr_constructor(ContainerVt(tag), 1)(outValue, a);
    }

    void ManagedToContainerVariant(int32_t tag, void* r_variant, void* wrapper)
    {
        void* q = ContainerHandleOf(static_cast<Dn2CppObject*>(wrapper));
        if (q != nullptr)
        {
            gd_get_variant_from_type(ContainerVt(tag))(r_variant, q);
            return;
        }
        GdPacked tmp;
        gd_variant_get_ptr_constructor(ContainerVt(tag), 0)(&tmp, nullptr);
        gd_get_variant_from_type(ContainerVt(tag))(r_variant, &tmp);
        gd_variant_get_ptr_destructor(ContainerVt(tag))(&tmp);
    }
}

void* dn2cpp_godot_construct_engine_object(Dn2CppObject* self, const char* engine_parent_class,
    const char* extension_class_name, int32_t is_ref_counted)
{
    GdStringName parent = MakeStringName(engine_parent_class);
    g_construct_sn_live++;
    GDExtensionObjectPtr obj = gd_construct_object(&parent);
    // construct_object caches the StringName value; the temporary is ours to
    // free (leaving it alive leaks an interned-string reference every call).
    gd_string_name_destroy(&parent);
    g_construct_sn_live--;
    // The shim type's first field (right after the Dn2CppObject header) holds
    // the engine object handle (f__handle), for both Node and RefCounted shims.
    *reinterpret_cast<void**>(reinterpret_cast<char*>(self) + sizeof(Dn2CppObject)) = obj;
    if (extension_class_name != nullptr)
    {
        GdStringName sn = MakeStringName(extension_class_name);
        g_construct_sn_live++;
        gd_object_set_instance(obj, &sn, self);
        gd_string_name_destroy(&sn);
        g_construct_sn_live--;
    }
    if (is_ref_counted != 0)
    {
        uint8_t ok = 0;
        gd_method_bind_ptrcall(ResolveBind("RefCounted", "init_ref", 2240911060LL), obj, nullptr, &ok);
    }
    return obj;
}

Dn2CppObject* dn2cpp_godot_wrap_borrowed_object(const Dn2CppTypeInfo* ti, void* handle)
{
    if (handle == nullptr || ti == nullptr)
        return nullptr;
    return AllocShimWithHandle(ti, handle);
}

void dn2cpp_godot_object_destroy(void* obj)
{
    if (obj != nullptr && gd_object_destroy != nullptr)
        gd_object_destroy(static_cast<GDExtensionObjectPtr>(obj));
}

namespace
{
    // Uncached singleton lookup. The engine looks the singleton up by the
    // StringName *value*, so the temporary is ours to destroy afterwards — the
    // lesson ResolveBindUncached already carries: leaving it alive leaks a Godot
    // interned-string reference per resolve, which the godot-sample gate's
    // StringName-leak assert reports at library deinit.
    //
    // `report` is off on the cache-overflow path, which cannot remember that it
    // already complained; an absent singleton still degrades to null there, it
    // just says so at most once from the cached path.
    void* GetSingletonUncached(const char* name, bool report)
    {
        GdStringName n = MakeStringName(name);
        void* obj = gd_global_get_singleton(&n);
        gd_string_name_destroy(&n);
        if (obj == nullptr && report)
        {
            // Absent from this build: an editor-only singleton in an exported
            // game, JavaScriptBridge off-web, JavaClassWrapper off-Android. The
            // contract is to degrade — the caller wraps a null handle as a null
            // reference — but a null-returning engine call is indistinguishable
            // from a working one that returned nothing, so say it once.
            std::fprintf(stderr, "[dn2cpp] engine singleton %s is not present in this build"
                " — it reads as null and calls on it are no-ops\n", name);
        }
        return obj;
    }
}

void* dn2cpp_godot_get_singleton(const char* name)
{
    if (gd_global_get_singleton == nullptr || name == nullptr)
        return nullptr;

    // Keyed on the name literal's pointer identity, like ResolveBind. Sized well
    // above the engine's singleton surface (41 in the 4.7 dump), so the table
    // stays sparse and a program never reaches the uncached fallback.
    constexpr int kCacheSize = 64;
    static const char* keys[kCacheSize] = {};
    static void* objs[kCacheSize] = {};

    int slot = static_cast<int>((reinterpret_cast<uintptr_t>(name) >> 4) % kCacheSize);
    for (int i = 0; i < kCacheSize; i++)
    {
        int s = (slot + i) % kCacheSize;
        // A filled slot answers even when its object is null: caching the absence
        // is the point — the engine is asked once, not once per frame.
        if (keys[s] == name)
            return objs[s];
        if (keys[s] == nullptr)
        {
            void* obj = GetSingletonUncached(name, /*report*/ true);
            keys[s] = name;
            objs[s] = obj;
            return obj;
        }
    }
    return GetSingletonUncached(name, /*report*/ false);
}

void dn2cpp_godot_object_clear_instance(Dn2CppObject* self)
{
    if (self == nullptr || gd_object_set_instance == nullptr)
        return;
    void* obj = *reinterpret_cast<void**>(reinterpret_cast<char*>(self) + sizeof(Dn2CppObject));
    if (obj == nullptr)
        return;
    // object_set_instance keys on the *registered extension class name* — the
    // short name ClassDB knows (what the bind in construct_engine_object used) —
    // not the shim's namespace-qualified CLR type-info name. Recover it from the
    // node-class row for this exact runtime type so the clear targets the same
    // class the bind did; passing the full CLR name makes ClassDB reject the call
    // ("Cannot get class") and the binding is never cleared. A null instance
    // clears the binding. No row (never registered) means nothing to clear.
    const Dn2CppGodotNodeClass* nc = NodeClassForType(self->type);
    if (nc == nullptr)
        return;
    GdStringName sn = MakeStringName(nc->className);
    gd_object_set_instance(static_cast<GDExtensionObjectPtr>(obj), &sn, nullptr);
    gd_string_name_destroy(&sn);
}

// ---- strong/weak resurrection table for engine-referenced RefCounted ----
// A C#-new registered RefCounted shim handed to the engine (stored in an
// Array/Dictionary/Resource property/scene tree/etc.) may still be
// referenced by the engine after every C# reference is dropped. Detaching
// the binding at that point (the plain construct/clear-instance behavior
// above) is memory-safe but throws away the shim's state and virtual
// overrides. Anchoring instead keeps the shim reachable — a fixed-capacity,
// GC-scanned (dn2cpp_alloc_pinned) table plays the same "reachable while
// pending" role the finalizer ring plays for queued-but-not-yet-run objects —
// for as long as the engine's own get_reference_count() reports more than
// the shim's own baseline hold. The table only ever needs to be consulted by
// value: how the engine came to hold that extra reference never has to be
// tracked, since the anchor decision happens lazily at the moment the shim
// would otherwise be collected, not at the moment of hand-off.
namespace
{
    constexpr int32_t kRefCountedAnchorCapacity = 1024;
    Dn2CppObject** g_refcounted_anchor_table = nullptr; // lazily dn2cpp_alloc_pinned'd
    int32_t        g_refcounted_anchor_count = 0;
    std::mutex     g_refcounted_anchor_mtx;
}

uint8_t dn2cpp_godot_try_anchor_refcounted(Dn2CppObject* self)
{
    std::lock_guard<std::mutex> lock(g_refcounted_anchor_mtx);
    if (g_refcounted_anchor_table == nullptr)
        g_refcounted_anchor_table = static_cast<Dn2CppObject**>(
            dn2cpp_alloc_pinned(sizeof(Dn2CppObject*) * kRefCountedAnchorCapacity));
    if (g_refcounted_anchor_count >= kRefCountedAnchorCapacity)
    {
        // Silent degradation would be indistinguishable from "everything works":
        // this caller falls through to the plain detach path (memory-safe but
        // override-less), which quietly changes engine-visible behavior. Warn
        // once so a workload that outgrows the cap surfaces in the log rather
        // than as unexplained "the override stopped firing" reports.
        static bool warned = false;
        if (!warned)
        {
            warned = true;
            std::fprintf(stderr,
                "[dn2cpp] refcounted anchor table full (cap=%d); further engine "
                "hand-offs will fall back to detach and lose C# behavior for those "
                "instances. Memory-safe, but consider raising kRefCountedAnchorCapacity "
                "for this workload.\n",
                kRefCountedAnchorCapacity);
        }
        return 0;
    }
    dn2cpp_gc_store_ref(&g_refcounted_anchor_table[g_refcounted_anchor_count], self);
    g_refcounted_anchor_count++;
    return 1;
}

void dn2cpp_godot_sweep_anchored_refcounted()
{
    std::lock_guard<std::mutex> lock(g_refcounted_anchor_mtx);
    for (int32_t i = 0; i < g_refcounted_anchor_count; )
    {
        Dn2CppObject* self = g_refcounted_anchor_table[i];
        void* h = *reinterpret_cast<void**>(reinterpret_cast<char*>(self) + sizeof(Dn2CppObject));
        // extension_api.json declares get_reference_count as int32; ptrcall only
        // writes the low 4 bytes, so the explicit = 0 init keeps the upper half
        // deterministic (do not drop it — the buffer would otherwise carry
        // whatever garbage lived on the stack).
        int64_t cnt = 0;
        if (h != nullptr)
            gd_method_bind_ptrcall(ResolveBind("RefCounted", "get_reference_count", 3905245786LL), h, nullptr, &cnt);
        if (h == nullptr || cnt <= 1)
        {
            // No engine-side holder left beyond the shim's own baseline hold:
            // drop the anchor. The shim is an ordinary collectible+
            // finalizable object again; the next organic collection
            // finalizes it for real (this time GetReferenceCount() <= 1,
            // taking the plain Unreference()/__Destroy() path). Boehm scans
            // this whole pinned block regardless of the logical count, so the
            // vacated tail slot must be cleared too — otherwise a stale
            // pointer there keeps pinning the object forever (the same
            // "clear before running" reasoning the finalizer ring itself
            // uses).
            // The moved slot must be re-dirtied like the insert path's store:
            // under MANUAL_VDB an unbarriered store into this table is invisible
            // to an incremental cycle, and this table is the shim's only root.
            int32_t last = --g_refcounted_anchor_count;
            dn2cpp_gc_store_ref(&g_refcounted_anchor_table[i], g_refcounted_anchor_table[last]);
            g_refcounted_anchor_table[last] = nullptr;
            continue;
        }
        i++;
    }
}

// Object.connect has no dedicated bridge anymore: with Callable a first-class
// engine-call argument type, the generated shim's Connect rides the generic
// ptrcall path through dn2cpp_godot_callable_arg_new like any other method.

void dn2cpp_godot_set_callable_dispatch(void (*fn)(void* callableShim, void* variantArgs))
{
    g_callable_dispatch = fn;
}

void* dn2cpp_godot_callable_arg_new(const Dn2CppGodotCallableShim* shim)
{
    // The 16-byte engine Callable holds engine pointers, so it lives in pinned
    // (non-GC) memory; its address is the native ptrcall arg (the String
    // ownership pattern — callable_arg_free destroys it after the call).
    void* p = dn2cpp_alloc_pinned(16);
    std::memset(p, 0, 16);
    BuildCallableValue(p, shim);
    return p;
}

void dn2cpp_godot_callable_arg_free(void* p)
{
    if (p == nullptr) return;
    gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_CALLABLE)(p);
    dn2cpp_free_pinned(p);
}

void* dn2cpp_godot_signal_arg_new(const Dn2CppGodotSignalShim* shim)
{
    void* p = dn2cpp_alloc_pinned(16);
    std::memset(p, 0, 16);
    BuildSignalValue(p, shim);
    return p;
}

void dn2cpp_godot_signal_arg_free(void* p)
{
    if (p == nullptr) return;
    gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_SIGNAL)(p);
    dn2cpp_free_pinned(p);
}

// Resolves the engine instance for a method-bind bridge call. A static call is
// marked by the DN2CPP_GODOT_STATIC_INSTANCE sentinel: the engine wants a null
// instance, so it unwraps to nullptr and the call proceeds. A genuinely null
// obj is an instance call on a null managed receiver and is skipped (returns
// false) — the bridge no-ops it exactly as before.
static inline bool EngineCallInstance(void* obj, void*& inst)
{
    if (obj == DN2CPP_GODOT_STATIC_INSTANCE)
    {
        inst = nullptr;
        return true;
    }
    inst = obj;
    return obj != nullptr;
}

void dn2cpp_godot_call_ptrcall_ret_callable(void* obj, const char* cls, const char* method, int64_t hash,
                                            const void** args, Dn2CppGodotCallableShim* out)
{
    *out = {};
    void* inst;
    if (!EngineCallInstance(obj, inst)) return;
    // ptrcall writes a constructed engine Callable into r_ret (a zeroed slot
    // is a valid default Callable for the engine's assign path); we own it and
    // must destroy it after decoding — a custom callable of ours is decoded by
    // recovering its userdata POD, so nothing dangles when the value dies.
    alignas(8) uint8_t cv[16] = {};
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, cv);
    DecodeCallableValue(cv, out);
    gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_CALLABLE)(cv);
}

void dn2cpp_godot_call_ptrcall_ret_signal(void* obj, const char* cls, const char* method, int64_t hash,
                                          const void** args, Dn2CppGodotSignalShim* out)
{
    *out = {};
    void* inst;
    if (!EngineCallInstance(obj, inst)) return;
    alignas(8) uint8_t sv[16] = {};
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, sv);
    DecodeSignalValue(sv, out);
    gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_SIGNAL)(sv);
}

void dn2cpp_godot_callable_call(const Dn2CppGodotCallableShim* shim, void* variantArgs, Dn2CppGodotVariant* out)
{
    *out = {};
    if (gd_variant_call == nullptr)
        return;
    // The engine's `call` is a vararg builtin (no ptrcall), so wrap the built
    // Callable in a Variant and go through variant_call — which routes a
    // custom callable back into the managed dispatch trampoline.
    alignas(8) uint8_t cv[16] = {};
    BuildCallableValue(cv, shim);
    struct alignas(8) GdVariant { uint8_t data[24]; };
    GdVariant self = {};
    gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_CALLABLE)(self.data, cv);

    auto* arr = static_cast<Dn2CppArrayN*>(variantArgs);
    int32_t n = arr != nullptr ? arr->length : 0;
    // Call args overwhelmingly fit the stack buffers; the heap vectors only
    // kick in beyond them (spares two allocations on the common per-call path).
    constexpr int32_t kStackArgs = 8;
    GdVariant vbuf[kStackArgs];
    const void* pbuf[kStackArgs];
    std::vector<GdVariant> vheap;
    std::vector<const void*> pheap;
    GdVariant* vargs = vbuf;
    const void** aptrs = pbuf;
    if (n > kStackArgs)
    {
        vheap.resize(static_cast<size_t>(n));
        pheap.resize(static_cast<size_t>(n));
        vargs = vheap.data();
        aptrs = pheap.data();
    }
    auto* src = arr != nullptr ? reinterpret_cast<Dn2CppGodotVariant*>(arr->data) : nullptr;
    for (int32_t i = 0; i < n; i++)
    {
        vargs[i] = {}; // zeroed slot is a valid NIL for EncodeVariant to build into
        EncodeVariant(&src[i], vargs[i].data);
        aptrs[i] = vargs[i].data;
    }

    GdStringName snCall = MakeStringName("call");
    GdVariant ret = {};
    GDExtensionCallError err = {};
    gd_variant_call(self.data, &snCall, aptrs, n, ret.data, &err);
    if (err.error == GDEXTENSION_CALL_OK)
        DecodeVariant(ret.data, out);
    gd_variant_destroy(ret.data);
    for (int32_t i = 0; i < n; i++)
        gd_variant_destroy(vargs[i].data);
    gd_string_name_destroy(&snCall);
    gd_variant_destroy(self.data);
    gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_CALLABLE)(cv);
}

void dn2cpp_godot_emit_signal(void* obj, Dn2CppString* signal_name, Dn2CppArrayRef* args)
{
    if (obj == nullptr)
        return;

    GDExtensionMethodBindPtr emit_signal_bind = ResolveBind("Object", "emit_signal", 4047867050LL);
    if (emit_signal_bind == nullptr)
        return;

    GdStringName sn = MakeStringNameFromManaged(signal_name);

    int32_t arg_count = (args != nullptr) ? args->length : 0;
    int32_t total_args = 1 + arg_count;

    struct alignas(8) GdVariant
    {
        uint8_t data[24];
    };

    std::vector<GdVariant> variant_data(total_args);
    std::vector<const void*> arg_ptrs(total_args);

    GdVariant* var_signal = &variant_data[0];
    gd_variant_from_string_name(var_signal->data, &sn);
    arg_ptrs[0] = var_signal->data;

    for (int32_t i = 0; i < arg_count; i++)
    {
        GdVariant* var_arg = &variant_data[1 + i];
        ConvertToVariant(args->data[i], var_arg->data);
        arg_ptrs[i + 1] = var_arg->data;
    }

    GdVariant ret_variant = {};
    GDExtensionCallError call_err = {};
    gd_method_bind_call(emit_signal_bind, obj, arg_ptrs.data(), total_args, ret_variant.data, &call_err);

    for (int32_t i = 0; i < total_args; i++)
    {
        gd_variant_destroy(variant_data[i].data);
    }
    gd_string_name_destroy(&sn);
    gd_variant_destroy(ret_variant.data);
}

void dn2cpp_godot_call_vararg(void* obj, const char* cls, const char* method, int64_t hash,
    const Dn2CppGodotVariant* fixedArgs, int32_t fixedCount, void* variantArgs, Dn2CppGodotVariant* out)
{
    *out = {};
    void* inst;
    if (!EngineCallInstance(obj, inst) || gd_method_bind_call == nullptr)
        return;
    GDExtensionMethodBindPtr bind = ResolveBind(cls, method, hash);
    if (bind == nullptr)
        return;

    // The full argument list is the pre-encoded fixed args followed by the
    // managed Variant[] tail (layout-identical to Dn2CppGodotVariant, like the
    // Callable.Call path); each is built into an engine Variant via the codec.
    auto* tail = static_cast<Dn2CppArrayN*>(variantArgs);
    int32_t tailCount = tail != nullptr ? tail->length : 0;
    if (fixedCount < 0) fixedCount = 0;
    int32_t total = fixedCount + tailCount;

    struct alignas(8) GdVariant { uint8_t data[24]; };
    // Same stack-first buffering as dn2cpp_godot_callable_call above.
    constexpr int32_t kStackArgs = 8;
    GdVariant vbuf[kStackArgs];
    const void* pbuf[kStackArgs];
    std::vector<GdVariant> vheap;
    std::vector<const void*> pheap;
    GdVariant* vargs = vbuf;
    const void** aptrs = pbuf;
    if (total > kStackArgs)
    {
        vheap.resize(static_cast<size_t>(total));
        pheap.resize(static_cast<size_t>(total));
        vargs = vheap.data();
        aptrs = pheap.data();
    }

    for (int32_t i = 0; i < fixedCount; i++)
    {
        vargs[i] = {}; // zeroed slot is a valid NIL for EncodeVariant to build into
        EncodeVariant(&fixedArgs[i], vargs[i].data);
        aptrs[i] = vargs[i].data;
    }
    auto* src = tail != nullptr ? reinterpret_cast<Dn2CppGodotVariant*>(tail->data) : nullptr;
    for (int32_t i = 0; i < tailCount; i++)
    {
        GdVariant* v = &vargs[static_cast<size_t>(fixedCount) + i];
        *v = {};
        EncodeVariant(&src[i], v->data);
        aptrs[fixedCount + i] = v->data;
    }

    GdVariant ret = {};
    GDExtensionCallError err = {};
    gd_method_bind_call(bind, inst, aptrs, total, ret.data, &err);
    if (err.error == GDEXTENSION_CALL_OK)
        DecodeVariant(ret.data, out);
    gd_variant_destroy(ret.data);
    for (int32_t i = 0; i < total; i++)
        gd_variant_destroy(vargs[i].data);
}

void dn2cpp_godot_call_ptrcall(void* obj, const char* cls, const char* method, int64_t hash, const void** args, void* r_ret)
{
    void* inst;
    if (EngineCallInstance(obj, inst))
    {
        gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, r_ret);
    }
}

Dn2CppString* dn2cpp_godot_call_ptrcall_ret_str(void* obj, const char* cls, const char* method, int64_t hash, const void** args)
{
    void* inst;
    if (!EngineCallInstance(obj, inst)) return nullptr;
    // ptrcall writes a constructed (heap) Godot String into r_ret; we own it and
    // must destroy it after marshalling to a managed UTF-16 string.
    GdString gs{};
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, &gs);
    Dn2CppString* r = ManagedStringFromGd(&gs);
    gd_string_destroy(&gs);
    return r;
}

void* dn2cpp_godot_str_arg_new(Dn2CppString* s)
{
    // The wrapper holds an engine pointer (the String's CowData), so it lives in
    // pinned (non-GC) memory; str_arg_free destroys the engine String and frees it.
    // A GdString IS a single pointer, so its address is the native ptrcall arg.
    GdString* gs = static_cast<GdString*>(dn2cpp_alloc_pinned(sizeof(GdString)));
    gs->opaque = nullptr;
    GdStringFromManaged(gs, s);
    return gs;
}

void dn2cpp_godot_str_arg_free(void* gstr)
{
    if (gstr == nullptr) return;
    GdString* gs = static_cast<GdString*>(gstr);
    gd_string_destroy(gs);
    dn2cpp_free_pinned(gs);
}

Dn2CppString* dn2cpp_godot_call_ptrcall_ret_strname(void* obj, const char* cls, const char* method, int64_t hash, const void** args)
{
    void* inst;
    if (!EngineCallInstance(obj, inst)) return nullptr;
    GdStringName sn{};
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, &sn);
    Dn2CppString* r = ManagedFromStringLike(&sn, /*String ctor from StringName*/ 2);
    gd_string_name_destroy(&sn);
    return r;
}

Dn2CppString* dn2cpp_godot_call_ptrcall_ret_nodepath(void* obj, const char* cls, const char* method, int64_t hash, const void** args)
{
    void* inst;
    if (!EngineCallInstance(obj, inst)) return nullptr;
    GdNodePath np{};
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, &np);
    Dn2CppString* r = ManagedFromStringLike(&np, /*String ctor from NodePath*/ 3);
    gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_NODE_PATH)(&np);
    return r;
}

void* dn2cpp_godot_strname_arg_new(Dn2CppString* s)
{
    GdStringName* sn = static_cast<GdStringName*>(dn2cpp_alloc_pinned(sizeof(GdStringName)));
    sn->opaque = nullptr;
    StringLikeFromManaged(sn, GDEXTENSION_VARIANT_TYPE_STRING_NAME, s);
    return sn;
}

void dn2cpp_godot_strname_arg_free(void* p)
{
    if (p == nullptr) return;
    gd_string_name_destroy(static_cast<GdStringName*>(p));
    dn2cpp_free_pinned(p);
}

void* dn2cpp_godot_nodepath_arg_new(Dn2CppString* s)
{
    GdNodePath* np = static_cast<GdNodePath*>(dn2cpp_alloc_pinned(sizeof(GdNodePath)));
    np->opaque = nullptr;
    StringLikeFromManaged(np, GDEXTENSION_VARIANT_TYPE_NODE_PATH, s);
    return np;
}

void dn2cpp_godot_nodepath_arg_free(void* p)
{
    if (p == nullptr) return;
    gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_NODE_PATH)(static_cast<GdNodePath*>(p));
    dn2cpp_free_pinned(p);
}

void* dn2cpp_godot_call_ptrcall_ret_packed(int32_t tag, void* obj, const char* cls, const char* method, int64_t hash, const void** args)
{
    void* inst;
    if (!EngineCallInstance(obj, inst)) return nullptr;
    // ptrcall writes a constructed (heap) engine packed array into r_ret; we own
    // it and must destroy it after the element-wise copy-out into a managed array.
    GdPacked p{};
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, &p);
    void* arr = PackedToManaged(tag, &p);
    gd_variant_get_ptr_destructor(ToVariantType(tag))(&p);
    return arr;
}

void* dn2cpp_godot_packed_arg_new(int32_t tag, void* arr)
{
    // Mirror of dn2cpp_godot_str_arg_new for the packed types: the wrapper holds
    // engine pointers, so it lives in pinned (non-GC) memory; packed_arg_free
    // destroys the engine value and frees it. A GdPacked IS the engine packed
    // value (16-byte PackedArrayRef wrapper), so its address is the native
    // ptrcall arg. A null managed array marshals as an empty packed array.
    GdPacked* p = static_cast<GdPacked*>(dn2cpp_alloc_pinned(sizeof(GdPacked)));
    *p = GdPacked{};
    ManagedToPackedValue(tag, p, arr);
    return p;
}

void dn2cpp_godot_packed_arg_free(int32_t tag, void* p)
{
    if (p == nullptr) return;
    gd_variant_get_ptr_destructor(ToVariantType(tag))(static_cast<GdPacked*>(p));
    dn2cpp_free_pinned(p);
}

// ---- Reverse (engine -> managed) virtual marshalling ----
// Mirror of the forward ptrcall arg/return helpers for the generic
// engine-virtual trampolines. `decode` reads an engine-owned value the engine
// passed us (borrowed — never destroyed here); `encode` constructs the engine
// value into the engine-provided return slot (the engine owns and destroys it).

Dn2CppString* dn2cpp_godot_decode_str(const void* p)
{
    return ManagedStringFromGd(static_cast<const GdString*>(p));
}

Dn2CppString* dn2cpp_godot_decode_strname(const void* p)
{
    return ManagedFromStringLike(p, /*String ctor from StringName*/ 2);
}

Dn2CppString* dn2cpp_godot_decode_nodepath(const void* p)
{
    return ManagedFromStringLike(p, /*String ctor from NodePath*/ 3);
}

void dn2cpp_godot_encode_str(void* r_ret, Dn2CppString* s)
{
    // A null managed string becomes the empty engine String.
    if (s == nullptr)
    {
        gd_string_new_utf8(static_cast<GdString*>(r_ret), "", 0);
        return;
    }
    static_cast<GdString*>(r_ret)->opaque = nullptr;
    GdStringFromManaged(static_cast<GdString*>(r_ret), s);
}

void dn2cpp_godot_encode_strname(void* r_ret, Dn2CppString* s)
{
    if (s == nullptr)
    {
        gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_STRING_NAME, 0)(r_ret, nullptr);
        return;
    }
    StringLikeFromManaged(r_ret, GDEXTENSION_VARIANT_TYPE_STRING_NAME, s);
}

void dn2cpp_godot_encode_nodepath(void* r_ret, Dn2CppString* s)
{
    if (s == nullptr)
    {
        gd_variant_get_ptr_constructor(GDEXTENSION_VARIANT_TYPE_NODE_PATH, 0)(r_ret, nullptr);
        return;
    }
    StringLikeFromManaged(r_ret, GDEXTENSION_VARIANT_TYPE_NODE_PATH, s);
}

void dn2cpp_godot_decode_variant(const void* p, Dn2CppGodotVariant* out)
{
    *out = {};
    // Borrowed engine Variant (the engine owns the arg). A RefCounted Object
    // payload still gets DecodeVariant's own lifetime guard, so the managed
    // Variant can safely outlive the call.
    DecodeVariant(p, out);
}

void dn2cpp_godot_encode_variant(void* r_ret, const Dn2CppGodotVariant* v)
{
    // The return slot is uninitialised 24-byte Variant memory; zero it to a valid
    // nil, then build the payload in place (nil stays nil for kind 0).
    std::memset(r_ret, 0, 24);
    EncodeVariant(v, r_ret);
}

void* dn2cpp_godot_decode_packed(int32_t tag, const void* p)
{
    return PackedToManaged(tag, p);
}

void dn2cpp_godot_encode_packed(int32_t tag, void* r_ret, void* arr)
{
    *static_cast<GdPacked*>(r_ret) = GdPacked{};
    ManagedToPackedValue(tag, r_ret, arr);
}

void dn2cpp_godot_builtin_call(int32_t gdType, const char* method, int64_t hash,
                               const void* base, const void** args, void* r_ret, int32_t argc)
{
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(gdType, method, hash);
    if (bm != nullptr)
        bm(const_cast<void*>(base), args, r_ret, static_cast<int>(argc));
}

Dn2CppString* dn2cpp_godot_builtin_call_str(int32_t gdType, const char* method, int64_t hash,
                                            const void* base, const void** args, int32_t argc)
{
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(gdType, method, hash);
    if (bm == nullptr) return nullptr;
    GdString gs{};
    bm(const_cast<void*>(base), args, &gs, static_cast<int>(argc));
    Dn2CppString* r = ManagedStringFromGd(&gs);
    gd_string_destroy(&gs);
    return r;
}

void dn2cpp_godot_builtin_call_variant(int32_t gdType, const char* method, int64_t hash,
                                       const void* base, const void** args,
                                       Dn2CppGodotVariant* out, int32_t argc)
{
    *out = {};
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(gdType, method, hash);
    if (bm == nullptr) return;
    // The engine method writes a constructed Variant (24-byte opaque) into r_ret;
    // we own it and must destroy it after decoding. DecodeVariant handles every
    // payload kind the shim Variant models (others decode as nil).
    struct alignas(8) GdVariant { uint8_t data[24]; } v{};
    bm(const_cast<void*>(base), args, v.data, static_cast<int>(argc));
    DecodeVariant(v.data, out);
    gd_variant_destroy(v.data);
}

void dn2cpp_godot_variant_register_type(int32_t kind, const Dn2CppTypeInfo* ti)
{
    if (kind > 0 && kind < 40)
        g_variant_payload_ti[kind] = ti;
}

void* dn2cpp_godot_variant_arg_new(const Dn2CppGodotVariant* v)
{
    // Engine Variant argument storage: 24 bytes on 64-bit, over-allocated to 32
    // like the print bridge; a zeroed slot is a valid NIL for EncodeVariant to
    // construct into. Pinned (non-GC) — it holds engine payload pointers and its
    // address is the native ptrcall arg; variant_arg_free destroys + frees it.
    void* slot = dn2cpp_alloc_pinned(32);
    std::memset(slot, 0, 32);
    EncodeVariant(v, slot);
    return slot;
}

void dn2cpp_godot_variant_arg_free(void* p)
{
    if (p == nullptr) return;
    gd_variant_destroy(p);
    dn2cpp_free_pinned(p);
}

void dn2cpp_godot_call_ptrcall_ret_variant(void* obj, const char* cls, const char* method, int64_t hash,
                                           const void** args, Dn2CppGodotVariant* out)
{
    *out = {};
    void* inst;
    if (!EngineCallInstance(obj, inst)) return;
    // ptrcall writes a constructed engine Variant into r_ret; we own it and must
    // destroy it after decoding. A RefCounted Object payload survives the destroy
    // via the decode's refGuard reference — the same ownership pattern as the
    // builtin-call Variant return above.
    struct alignas(8) GdVariant { uint8_t data[24]; } v{};
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, v.data);
    DecodeVariant(v.data, out);
    gd_variant_destroy(v.data);
}

void* dn2cpp_godot_container_new(int32_t tag)
{
    void* p = NewContainerSlot();
    gd_variant_get_ptr_constructor(ContainerVt(tag), 0)(p, nullptr);
    return p;
}

void dn2cpp_godot_container_destroy(int32_t tag, void* p)
{
    if (p == nullptr) return;
    gd_variant_get_ptr_destructor(ContainerVt(tag))(p);
    dn2cpp_free_pinned(p);
}

int32_t dn2cpp_godot_container_size(int32_t tag, void* p)
{
    if (p == nullptr) return 0;
    // Array and Dictionary share the packed types' `size` signature and hash.
    return (int32_t)PackedSize(tag, p);
}

void dn2cpp_godot_container_clear(int32_t tag, void* p)
{
    if (p == nullptr) return;
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(tag, "clear", kContainerClearHash);
    if (bm != nullptr)
        bm(p, nullptr, nullptr, 0);
}

void* dn2cpp_godot_call_ptrcall_ret_container(int32_t tag, void* obj, const char* cls, const char* method, int64_t hash, const void** args)
{
    void* inst;
    if (!EngineCallInstance(obj, inst)) return nullptr;
    // ptrcall builds the returned engine container value into the zeroed slot
    // (transferring one container reference); the caller's managed wrapper takes
    // ownership and its finalizer destroys the value + frees the slot.
    void* q = NewContainerSlot();
    gd_method_bind_ptrcall(ResolveBind(cls, method, hash), inst, args, q);
    return q;
}

void dn2cpp_godot_array_get(void* p, int64_t idx, Dn2CppGodotVariant* out)
{
    *out = {};
    if (p == nullptr || gd_array_index == nullptr) return;
    // array_operator_index returns the element Variant's slot in place — a
    // borrow of storage the array owns, so nothing to destroy here (a RefCounted
    // Object payload still gets DecodeVariant's own lifetime guard).
    void* slot = gd_array_index(p, idx);
    if (slot != nullptr)
        DecodeVariant(slot, out);
}

void dn2cpp_godot_array_set(void* p, int64_t idx, const Dn2CppGodotVariant* v)
{
    if (p == nullptr) return;
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(DN2CPP_GD_GODOT_ARRAY, "set", kArraySetHash);
    if (bm == nullptr) return;
    // The engine's own `set` (not a raw operator_index slot write), so a typed
    // engine array still validates the element — the push_back convention.
    struct alignas(8) GdVariant { uint8_t data[24]; } gv{};
    EncodeVariant(v, gv.data);
    const void* a[] = { &idx, gv.data };
    bm(p, a, nullptr, 2);
    gd_variant_destroy(gv.data);
}

void dn2cpp_godot_array_add(void* p, const Dn2CppGodotVariant* v)
{
    if (p == nullptr) return;
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(DN2CPP_GD_GODOT_ARRAY, "push_back", kArrayPushBackHash);
    if (bm == nullptr) return;
    // push_back goes through the engine's own method (not a raw slot write), so
    // a typed engine array still validates the element.
    struct alignas(8) GdVariant { uint8_t data[24]; } gv{};
    EncodeVariant(v, gv.data);
    const void* a[] = { gv.data };
    bm(p, a, nullptr, 1);
    gd_variant_destroy(gv.data);
}

void* dn2cpp_godot_array_get_object(void* p, int64_t idx)
{
    if (p == nullptr || gd_array_index == nullptr) return nullptr;
    void* slot = gd_array_index(p, idx);
    if (slot == nullptr) return nullptr;
    if (gd_variant_get_type == nullptr
        || gd_variant_get_type(slot) != GDEXTENSION_VARIANT_TYPE_OBJECT)
        return nullptr;
    void* h = nullptr;
    if (gd_object_from_variant != nullptr)
        gd_object_from_variant(&h, slot);
    // Transfer one engine reference for a RefCounted element (the array's own
    // reference may be the last), matching the engine-object return convention:
    // the managed wrapper registers a finalizer whose Unreference() returns it.
    if (h != nullptr && IsRefCountedHandle(h))
    {
        uint8_t ok = 0;
        gd_method_bind_ptrcall(ResolveBind("RefCounted", "reference", 2240911060LL), h, nullptr, &ok);
    }
    return h;
}

void dn2cpp_godot_dict_get(void* p, const Dn2CppGodotVariant* key, Dn2CppGodotVariant* out)
{
    *out = {};
    if (p == nullptr || gd_dict_index_const == nullptr) return;
    struct alignas(8) GdVariant { uint8_t data[24]; } kv{};
    EncodeVariant(key, kv.data);
    // The non-mutating operator_index_const: a missing key reads back as nil
    // WITHOUT inserting a slot (the mutating dictionary_operator_index carries
    // GDScript's insert-on-read semantics, which would corrupt Count and
    // ContainsKey for a mere read).
    void* slot = gd_dict_index_const(p, kv.data);
    if (slot != nullptr)
        DecodeVariant(slot, out);
    gd_variant_destroy(kv.data);
}

void dn2cpp_godot_dict_set(void* p, const Dn2CppGodotVariant* key, const Dn2CppGodotVariant* v)
{
    if (p == nullptr || gd_dict_index == nullptr) return;
    struct alignas(8) GdVariant { uint8_t data[24]; } kv{};
    EncodeVariant(key, kv.data);
    void* slot = gd_dict_index(p, kv.data);
    if (slot != nullptr)
    {
        // An existing key's slot holds a live Variant (a new key's slot is the
        // freshly inserted nil); destroy-then-construct covers both.
        gd_variant_destroy(slot);
        std::memset(slot, 0, 24);
        EncodeVariant(v, slot);
    }
    gd_variant_destroy(kv.data);
}

int32_t dn2cpp_godot_dict_has(void* p, const Dn2CppGodotVariant* key)
{
    if (p == nullptr) return 0;
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(DN2CPP_GD_DICTIONARY, "has", kContainerHasHash);
    if (bm == nullptr) return 0;
    struct alignas(8) GdVariant { uint8_t data[24]; } kv{};
    EncodeVariant(key, kv.data);
    uint8_t r = 0;
    const void* a[] = { kv.data };
    bm(p, a, &r, 1);
    gd_variant_destroy(kv.data);
    return r;
}

void* dn2cpp_godot_dict_keys(void* p)
{
    if (p == nullptr) return nullptr;
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(DN2CPP_GD_DICTIONARY, "keys", kDictKeysHash);
    if (bm == nullptr) return nullptr;
    // keys() writes a fresh engine Array into the zeroed slot; the caller's
    // wrapper takes ownership (the container-return convention).
    void* q = NewContainerSlot();
    bm(p, nullptr, q, 0);
    return q;
}

void* dn2cpp_godot_dict_values(void* p)
{
    if (p == nullptr) return nullptr;
    // values() shares keys()' hash (identical signatures).
    GDExtensionPtrBuiltInMethod bm = ResolveBuiltinMethod(DN2CPP_GD_DICTIONARY, "values", kDictKeysHash);
    if (bm == nullptr) return nullptr;
    void* q = NewContainerSlot();
    bm(p, nullptr, q, 0);
    return q;
}


// The .gdextension entry symbol: the engine resolves it by name out of the
// loaded library, so it must stay in the export trie under -fvisibility=hidden.
extern "C" DN2CPP_RT_EXPORT GDExtensionBool dn2cpp_gdext_init(
    GDExtensionInterfaceGetProcAddress p_get_proc_address,
    GDExtensionClassLibraryPtr p_library,
    GDExtensionInitialization* r_initialization)
{
    g_library = p_library;

    auto load = [&](const char* name) { return p_get_proc_address(name); };
    gd_string_name_new = reinterpret_cast<GDExtensionInterfaceStringNameNewWithLatin1Chars>(load("string_name_new_with_latin1_chars"));
    gd_string_new_utf8 = reinterpret_cast<GDExtensionInterfaceStringNewWithUtf8CharsAndLen>(load("string_new_with_utf8_chars_and_len"));
    gd_string_to_utf8 = reinterpret_cast<GDExtensionInterfaceStringToUtf8Chars>(load("string_to_utf8_chars"));
    gd_get_variant_from_type = reinterpret_cast<GDExtensionInterfaceGetVariantFromTypeConstructor>(load("get_variant_from_type_constructor"));
    gd_get_variant_to_type = reinterpret_cast<GDExtensionInterfaceGetVariantToTypeConstructor>(load("get_variant_to_type_constructor"));
    gd_variant_get_ptr_destructor = reinterpret_cast<GDExtensionInterfaceVariantGetPtrDestructor>(load("variant_get_ptr_destructor"));
    gd_register_class = reinterpret_cast<GDExtensionInterfaceClassdbRegisterExtensionClass4>(load("classdb_register_extension_class4"));
    gd_register_method = reinterpret_cast<GDExtensionInterfaceClassdbRegisterExtensionClassMethod>(load("classdb_register_extension_class_method"));
    gd_unregister_class = reinterpret_cast<GDExtensionInterfaceClassdbUnregisterExtensionClass>(load("classdb_unregister_extension_class"));
    gd_construct_object = reinterpret_cast<GDExtensionInterfaceClassdbConstructObject2>(load("classdb_construct_object2"));
    gd_object_set_instance = reinterpret_cast<GDExtensionInterfaceObjectSetInstance>(load("object_set_instance"));
    gd_object_destroy = reinterpret_cast<GDExtensionInterfaceObjectDestroy>(load("object_destroy"));
    gd_get_utility = reinterpret_cast<GDExtensionInterfaceVariantGetPtrUtilityFunction>(load("variant_get_ptr_utility_function"));
    gd_variant_destroy = reinterpret_cast<GDExtensionInterfaceVariantDestroy>(load("variant_destroy"));
    gd_global_get_singleton = reinterpret_cast<GDExtensionInterfaceGlobalGetSingleton>(load("global_get_singleton"));
    gd_get_method_bind = reinterpret_cast<GDExtensionInterfaceClassdbGetMethodBind>(load("classdb_get_method_bind"));
    gd_method_bind_ptrcall = reinterpret_cast<GDExtensionInterfaceObjectMethodBindPtrcall>(load("object_method_bind_ptrcall"));
    gd_variant_get_ptr_builtin_method = reinterpret_cast<GDExtensionInterfaceVariantGetPtrBuiltinMethod>(load("variant_get_ptr_builtin_method"));
    gd_variant_get_type = reinterpret_cast<GDExtensionInterfaceVariantGetType>(load("variant_get_type"));
    gd_object_cast_to = reinterpret_cast<GDExtensionInterfaceObjectCastTo>(load("object_cast_to"));
    gd_classdb_get_class_tag = reinterpret_cast<GDExtensionInterfaceClassdbGetClassTag>(load("classdb_get_class_tag"));
    gd_callable_custom_create2 = reinterpret_cast<GDExtensionInterfaceCallableCustomCreate2>(load("callable_custom_create2"));
    gd_callable_custom_get_userdata = reinterpret_cast<GDExtensionInterfaceCallableCustomGetUserdata>(load("callable_custom_get_userdata"));
    gd_variant_call = reinterpret_cast<GDExtensionInterfaceVariantCall>(load("variant_call"));
    gd_print_error = reinterpret_cast<GDExtensionInterfacePrintError>(load("print_error"));
    gd_pba_index = reinterpret_cast<GDExtensionInterfacePackedByteArrayOperatorIndex>(load("packed_byte_array_operator_index"));
    gd_pi32a_index = reinterpret_cast<GDExtensionInterfacePackedInt32ArrayOperatorIndex>(load("packed_int32_array_operator_index"));
    gd_pi64a_index = reinterpret_cast<GDExtensionInterfacePackedInt64ArrayOperatorIndex>(load("packed_int64_array_operator_index"));
    gd_pf32a_index = reinterpret_cast<GDExtensionInterfacePackedFloat32ArrayOperatorIndex>(load("packed_float32_array_operator_index"));
    gd_pf64a_index = reinterpret_cast<GDExtensionInterfacePackedFloat64ArrayOperatorIndex>(load("packed_float64_array_operator_index"));
    gd_psa_index = reinterpret_cast<GDExtensionInterfacePackedStringArrayOperatorIndex>(load("packed_string_array_operator_index"));
    gd_pv2a_index = reinterpret_cast<GDExtensionInterfacePackedVector2ArrayOperatorIndex>(load("packed_vector2_array_operator_index"));
    gd_pv3a_index = reinterpret_cast<GDExtensionInterfacePackedVector3ArrayOperatorIndex>(load("packed_vector3_array_operator_index"));
    gd_pv4a_index = reinterpret_cast<GDExtensionInterfacePackedVector4ArrayOperatorIndex>(load("packed_vector4_array_operator_index"));
    gd_pca_index = reinterpret_cast<GDExtensionInterfacePackedColorArrayOperatorIndex>(load("packed_color_array_operator_index"));
    gd_array_index = reinterpret_cast<GDExtensionInterfaceArrayOperatorIndex>(load("array_operator_index"));
    gd_dict_index = reinterpret_cast<GDExtensionInterfaceDictionaryOperatorIndex>(load("dictionary_operator_index"));
    gd_dict_index_const = reinterpret_cast<GDExtensionInterfaceDictionaryOperatorIndexConst>(load("dictionary_operator_index_const"));

    gd_register_signal = reinterpret_cast<GDExtensionInterfaceClassdbRegisterExtensionClassSignal>(load("classdb_register_extension_class_signal"));
    gd_register_property = reinterpret_cast<GDExtensionInterfaceClassdbRegisterExtensionClassProperty>(load("classdb_register_extension_class_property"));
    gd_variant_from_string_name = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_STRING_NAME);
    gd_variant_from_object = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_OBJECT);
    gd_object_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_OBJECT);
    gd_method_bind_call = reinterpret_cast<GDExtensionInterfaceObjectMethodBindCall>(load("object_method_bind_call"));
    gd_string_name_destroy = gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_STRING_NAME);
    gd_variant_get_ptr_constructor = reinterpret_cast<GDExtensionInterfaceVariantGetPtrConstructor>(load("variant_get_ptr_constructor"));

    if (gd_string_name_new == nullptr || gd_register_class == nullptr || gd_register_method == nullptr || gd_register_signal == nullptr)
        return 0;

    // Install the boundary reporter before anything managed can run. It is also
    // what tells the core there IS a host frame able to survive a managed fault,
    // which is how dn2cpp_sched_pump chooses reporting over failing fast — so it
    // goes in unconditionally, even on an engine whose print_error did not
    // resolve (the sink degrades its channel, not its duty).
    dn2cpp_set_boundary_exception_sink(&GodotBoundarySink);

    gd_variant_from_bool = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_BOOL);
    gd_variant_from_int = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_INT);
    gd_variant_from_float = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_FLOAT);
    gd_variant_from_string = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_STRING);
    gd_bool_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_BOOL);
    gd_int_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_INT);
    gd_float_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_FLOAT);
    gd_string_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_STRING);
    gd_vector2_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_VECTOR2);
    gd_variant_from_vector2 = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_VECTOR2);
    gd_vector3_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_VECTOR3);
    gd_variant_from_vector3 = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_VECTOR3);
    gd_vector4_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_VECTOR4);
    gd_variant_from_vector4 = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_VECTOR4);
    gd_color_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_COLOR);
    gd_variant_from_color = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_COLOR);
    gd_transform2d_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_TRANSFORM2D);
    gd_variant_from_transform2d = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_TRANSFORM2D);
    gd_basis_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_BASIS);
    gd_variant_from_basis = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_BASIS);
    gd_transform3d_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_TRANSFORM3D);
    gd_variant_from_transform3d = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_TRANSFORM3D);
    gd_projection_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_PROJECTION);
    gd_variant_from_projection = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_PROJECTION);
    gd_quaternion_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_QUATERNION);
    gd_variant_from_quaternion = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_QUATERNION);
    gd_plane_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_PLANE);
    gd_variant_from_plane = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_PLANE);
    gd_aabb_from_variant = gd_get_variant_to_type(GDEXTENSION_VARIANT_TYPE_AABB);
    gd_variant_from_aabb = gd_get_variant_from_type(GDEXTENSION_VARIANT_TYPE_AABB);
    gd_string_destroy = gd_variant_get_ptr_destructor(GDEXTENSION_VARIANT_TYPE_STRING);

    // Per-frame main-loop callback (available since Godot 4.5; the loader
    // returns null on older engines, where the process-virtual drain sites
    // remain the only per-frame consumers). Registered before init returns so
    // the engine has it when the main loop starts; frame callbacks only fire
    // once the loop iterates, which is after the SCENE-level Initialize (and
    // dn2cpp_gc_drain_finalizers is a no-op until manual mode is enabled
    // there anyway).
    auto gd_register_main_loop_callbacks =
        reinterpret_cast<GDExtensionInterfaceRegisterMainLoopCallbacks>(load("register_main_loop_callbacks"));
    if (gd_register_main_loop_callbacks != nullptr)
    {
        static const GDExtensionMainLoopCallbacks main_loop_callbacks = {
            &MainLoopStartup,
            &MainLoopShutdown,
            &MainLoopFrame,
        };
        gd_register_main_loop_callbacks(g_library, &main_loop_callbacks);
    }

    r_initialization->minimum_initialization_level = GDEXTENSION_INITIALIZATION_SCENE;
    r_initialization->userdata = nullptr;
    r_initialization->initialize = &Initialize;
    r_initialization->deinitialize = &Deinitialize;
    return 1;
}
