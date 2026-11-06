#include <vector>

// Deliberately incomplete native rows exercise refusal, not fabricated CLR objects.
static int32_t metadata_definition_custom_override(Dn2CppObject*, Dn2CppObject* other)
{
    return other == nullptr ? 7 : 9;
}

static void metadata_definition_native_contract()
{
    auto check = [](bool result, const char* label) {
        if (!result) { std::fprintf(stderr, "metadata definition native contract: %s\n", label); std::abort(); }
    };
    auto refuses = [](Dn2CppObject* a, Dn2CppObject* b) {
        try { dn2cpp_memberinfo_has_same_metadata(a, b); }
        catch (Dn2CppException& error) { return error.obj->type == &dn2cpp_platform_not_supported_exception_type; }
        return false;
    };
    Dn2CppTypeReflection leftMetadata{}, rightMetadata{};
    leftMetadata.metadataToken = rightMetadata.metadataToken = 0x02000001;
    leftMetadata.assemblyName = "NativeIdentityLeft";
    rightMetadata.assemblyName = "NativeIdentityRight";
    Dn2CppTypeInfo left{}, right{}, unknown{};
    left.name = right.name = "NativeIdentityFixture";
    left.reflectionData = &leftMetadata;
    right.reflectionData = &rightMetadata;
    unknown.name = "UnrecordedIdentity";
    Dn2CppType leftType{}, rightType{}, unknownType{};
    leftType.type = rightType.type = unknownType.type = &dn2cpp_type_type;
    leftType.typeInfo = &left; rightType.typeInfo = &right; unknownType.typeInfo = &unknown;
    check(!dn2cpp_memberinfo_has_same_metadata(&leftType, &rightType), "same TypeDef token in different modules");
    check(dn2cpp_memberinfo_has_same_metadata(&leftType, &leftType), "same recorded type");
    check(refuses(&unknownType, &unknownType), "unknown type self is not a guessed identity");
    unknown.flags = DN2CPP_TF_GENERICPARAM;
    check(refuses(&unknownType, &unknownType), "unregistered formal flag");
    unknown.flags = DN2CPP_TF_ARRAY;
    check(refuses(&unknownType, &unknownType), "compound without element identity");
    unknown.flags = 0;
    Dn2CppTypeInfo knownArray{}, unknownModuleArray{};
    knownArray.flags = unknownModuleArray.flags = DN2CPP_TF_ARRAY;
    knownArray.elementType = &dn2cpp_int32_type;
    unknownModuleArray.elementType = &dn2cpp_parallel_loop_result_type;
    Dn2CppType knownArrayType{}, unknownModuleArrayType{};
    knownArrayType.type = unknownModuleArrayType.type = &dn2cpp_type_type;
    knownArrayType.typeInfo = &knownArray; unknownModuleArrayType.typeInfo = &unknownModuleArray;
    Dn2CppType canonicalMissingModule{};
    canonicalMissingModule.type = &dn2cpp_type_type;
    canonicalMissingModule.typeInfo = &dn2cpp_parallel_loop_result_type;
    check(dn2cpp_memberinfo_has_same_metadata(&canonicalMissingModule, &canonicalMissingModule),
        "canonical builtin identity is known independently of module");
    check(refuses(&unknownModuleArrayType, &knownArrayType), "canonical builtin without proven defining module");
    Dn2CppMethodInfo lm{}, rm{};
    lm.declaringType = &left; rm.declaringType = &right;
    lm.metadataToken = rm.metadataToken = 0x06000001;
    Dn2CppMethodRef lmethod{}, rmethod{}, ctor{};
    lmethod.type = rmethod.type = &dn2cpp_methodinfo_type; ctor.type = &dn2cpp_constructorinfo_type;
    lmethod.method = &lm; rmethod.method = &rm; ctor.method = &lm;
    check(!dn2cpp_memberinfo_has_same_metadata(&lmethod, &rmethod), "same MethodDef token in different modules");
    check(!dn2cpp_memberinfo_has_same_metadata(&lmethod, &ctor), "method and constructor kind collision");
    rm.declaringType = &left;
    check(dn2cpp_memberinfo_has_same_metadata(&lmethod, &rmethod), "different handles one recorded method");
    rm.metadataToken = 0;
    check(refuses(&rmethod, &rmethod), "unknown tokenless method self");
    Dn2CppFieldInfo lf{}, rf{};
    lf.declaringType = &left; rf.declaringType = &right;
    lf.metadataToken = rf.metadataToken = 0x04000001;
    Dn2CppFieldRef lfield{}, rfield{};
    lfield.type = rfield.type = &dn2cpp_fieldinfo_type; lfield.field = &lf; rfield.field = &rf;
    check(!dn2cpp_memberinfo_has_same_metadata(&lfield, &rfield), "same FieldDef token in different modules");
    rf.metadataToken = 0;
    check(refuses(&rfield, &rfield), "unknown tokenless field self");
    rf.declaringType = nullptr;
    check(refuses(&rfield, &rfield), "missing declaring owner");
    Dn2CppPropInfo lp{}, rp{};
    lp.declaringType = &left; rp.declaringType = &right;
    lp.metadataToken = rp.metadataToken = 0x17000001;
    Dn2CppPropRef lprop{}, rprop{};
    lprop.type = rprop.type = &dn2cpp_propertyinfo_type; lprop.prop = &lp; rprop.prop = &rp;
    check(!dn2cpp_memberinfo_has_same_metadata(&lprop, &rprop), "same Property token in different modules");
    Dn2CppEventInfo le{}, re{};
    le.declaringType = &left; re.declaringType = &right;
    le.metadataToken = re.metadataToken = 0x14000001;
    Dn2CppEventRef levent{}, revent{};
    levent.type = revent.type = &dn2cpp_eventinfo_type; levent.event = &le; revent.event = &re;
    check(!dn2cpp_memberinfo_has_same_metadata(&levent, &revent), "same Event token in different modules");
    check(dn2cpp_memberinfo_has_same_metadata_slot >= 0, "retained MemberInfo virtual slot");
    std::vector<const void*> slots(static_cast<std::size_t>(dn2cpp_memberinfo_has_same_metadata_slot + 1), nullptr);
    slots[dn2cpp_memberinfo_has_same_metadata_slot] = reinterpret_cast<const void*>(&metadata_definition_custom_override);
    Dn2CppTypeInfo customType{}; customType.vtable = slots.data();
    Dn2CppObject custom{}; custom.type = &customType;
    check(dn2cpp_memberinfo_has_same_metadata(&custom, nullptr) == 7, "managed override sees null without runtime validation");
    check(dn2cpp_memberinfo_has_same_metadata(&custom, &leftType) == 9, "managed override is preserved");
    Dn2CppTypeInfo unavailableType{};
    Dn2CppObject unavailable{}; unavailable.type = &unavailableType;
    check(refuses(&unavailable, nullptr), "unrepresented managed receiver refuses before runtime argument validation");
    check(!dn2cpp_memberinfo_has_same_metadata(&leftType, &custom), "runtime kind guard rejects custom before identity reads");
    const int32_t recordedSlot = dn2cpp_memberinfo_has_same_metadata_slot;
    dn2cpp_set_memberinfo_has_same_metadata_slot(-1);
    check(refuses(&custom, nullptr), "uninitialized image refuses rather than dispatching slot zero");
    dn2cpp_set_memberinfo_has_same_metadata_slot(recordedSlot);
    std::printf("member metadata definition native contract: passed\n");
}
