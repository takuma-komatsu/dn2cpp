#!/usr/bin/env bash
# Retained RuntimeTypeHandle SZ-array identity,
# formal declaring-method identity and the private CoreCLR MethodDesc boundary,
# managed member-not-found and ambiguous-match exception factories,
# CoreCLR virtual-slot count boundaries,
# RuntimeTypeHandle interface validation,
# MethodInfo generic arity,
# RuntimeTypeHandle construction and cache-handle ownership,
# private generic-context validation,
# canonical type comparison and Signature blob-parser boundaries,
# managed type-handle copies,
# nullable reboxing, CoreCLR handle boundaries, public reflection,
# and companion process
# argument/stream/exit preservation and the console-self-host
# runtime primitives whose behaviour can be exercised exactly.
#
#  #3 Interop.GetRandomBytes -> the non-cryptographic random source behind
#     System.HashCode's process-global seed (the InternalCall
#     Interop+Sys::GetNonCryptographicallySecureRandomBytes, intercepted at the IL
#     forwarder Interop.GetRandomBytes and filled deterministically with a fixed
#     seed). A Dictionary / HashSet keyed on ValueTuple<string,string> drives
#     ValueTuple.GetHashCode -> HashCode.Combine -> GenerateGlobalSeed ->
#     GetRandomBytes. A fixed seed keeps every observed result identical to real
#     .NET: key lookups are hash-value-independent, and Dictionary/HashSet
#     enumeration is insertion order (no removals). Exercised end-to-end here.
#
#  #5 Exception.get_StackTrace -> null carve-out. An UN-THROWN exception has a null
#     StackTrace in real .NET too, so `ex.StackTrace is null` is exact in both. (A
#     thrown exception's StackTrace is an intentional divergence — native has no
#     stack-trace model — and is deliberately NOT exercised here.)
#
#  #4 AppContext.BaseDirectory / Environment.ProcessPath resolve the process image
#     (the PAL's executable-path query), the way NativeAOT does — a native binary has
#     no managed entry assembly whose Location it could derive them from. The strings
#     differ between the two runtimes by construction (`dotnet app.dll` reports the
#     dll's directory and the dotnet host's path), so the section asserts the
#     properties both must satisfy instead: the base directory exists, is rooted and
#     ends in a separator; ProcessPath is non-null, rooted and names a real file.
#
#  MiscIntrinsicSubset — the misc intrinsics dn2cpp's own
#     code reaches but that had no intrinsic mapping. Its real subjects are wider than
#     the section name suggests, and none of them is "reflection" or "math":
#       * Double/Single IsNaN / IsPositiveInfinity / IsNegativeInfinity (MethodCompiler
#         maps the special float constants to the C++ math macros; the System.Numerics
#         generic-math forms were already handled in TryEmitGenericMathIntrinsic);
#       * the static Object.Equals(objA, objB) (MethodCompiler.BranchTo merges the
#         per-slot StaticType record across a join) — reuses dn2cpp_object_equals,
#         whose null/reference/virtual-Equals semantics match the static overload;
#       * System.Numerics.BitOperations.{TrailingZeroCount, LeadingZeroCount, PopCount,
#         Log2} over int/uint/long/ulong, lowered to the ctz/clz/popcount clang builtins
#         at the argument's width (the nint/nuint overloads stay BCL-as-IL);
#       * Debugger.IsAttached, and the Trace/Debug write path: Debugger.IsLogging
#         const-folds false (NativeAOT parity), which prunes DebugProvider's
#         Debugger.Log arm and leaves the transpiled Interop.Sys.SysLog leaf — so a
#         Trace.WriteLine links the SystemNative_SysLog PAL entry and, like a direct
#         Debugger.Log, prints nothing. Trace puts System.Diagnostics.TraceSource on
#         the -r set below, and TraceListener.Attributes pulls
#         System.Collections.Specialized in behind it;
#       * the GC.CollectionCount / GetGCMemoryInfo bindings over
#         the vendored Boehm GC, and AssemblyLoadContext.LoadFromAssemblyPath's trap;
#       * the Thrive startup shape AppDomain.CurrentDomain.GetAssemblies().Where(...)
#         .Count() — the degraded set must carry the DECLARED Assembly[] runtime
#         identity, which is why System.Linq is on the -r set below;
#       * two null-receiver runtime helpers (dn2cpp_unbox and
#         dn2cpp_task_exception), which must raise a CATCHABLE NullReferenceException
#         rather than abort — i.e. the section's tail is an EH-recovery test.
#     It is driven LAST, after the sections above: it forces a GC.Collect() and prints
#     process-wide GC facts, so nothing else may inherit its collection.
#
#  GcAllocatedBytesSubset — GC.GetAllocatedBytesForCurrentThread and
#     GC.GetTotalAllocatedBytes(bool), lowered to the runtime's thread_local
#     accounting at the three dn2cpp_alloc entry points. Raw counts differ
#     (dn2cpp counts requested bytes, pinned included), so the section asserts
#     invariants: the per-thread counter is non-negative, monotone, covers a
#     1 MiB allocation, stays flat across a quiet window and across 8 MiB
#     allocated on another Thread — the line that separates per-thread
#     accounting from a process-wide approximation. The approximate total is
#     asserted non-negative and monotone ONLY: real .NET's figure reads stale
#     per-thread buffers and a fresh 1 MiB may not appear. It is also read while
#     a worker allocates, covering the synchronized Boehm statistics path. The
#     precise total has no such carve-out and must cover the 1 MiB. It sits in
#     this bucket because it starts a Thread: this gate is the bucket's sole
#     driver, so no wasm axis reaches the section.
# RuntimeTypeHandle instance accessors preserve interned Type identity and the
# checked default-handle ArgumentNullException across reflection/direct/calli.
# Driven by the real System.Private.CoreLib (passed with -r) -> cross-assembly resolve
# + tree-shake -> native binary -> run, asserting the output diffs exact vs real .NET.
DN2CPP_GATE_EXTRA_INPUTS="${DN2CPP_GATE_EXTRA_INPUTS:-} gates/_runtime-type-szarray.sh gates/fixtures/ldftn-local/RuntimeTypeIsSzArrayFixture.cs samples/dotnet/SelfHostPrimSubset/RuntimeTypeIsSzArraySubset.cs gates/_formal-declaring-method.sh gates/fixtures/ldftn-local/FormalDeclaringMethodFixture.cs samples/dotnet/SelfHostPrimSubset/FormalDeclaringMethodSubset.cs gates/fixtures/ldftn-local/MemberNotFoundFactoryFixture.cs gates/fixtures/ldftn-local/AmbiguousMatchFactoryFixture.cs gates/fixtures/ldftn-local/RuntimeMethodSlotLookupFixture.cs gates/fixtures/ldftn-local/RuntimeVirtualSlotCountFixture.cs gates/fixtures/ldftn-local/RuntimeTypeInterfaceCheckFixture.cs gates/fixtures/ldftn-local/MethodGenericCountFixture.cs gates/fixtures/ldftn-local/RuntimeTypeCacheHandleFixture.cs gates/fixtures/ldftn-local/RuntimeTypeHandleConstructorFixture.cs gates/fixtures/ldftn-local/ConstraintCheckFixture.cs gates/fixtures/ldftn-local/RuntimeTypeHandleAccessorFixture.cs gates/fixtures/ldftn-local/CanonicalTypeComparisonFixture.cs gates/fixtures/ldftn-local/SignatureBlobFixture.cs gates/fixtures/ldftn-local/RuntimeTypeHandleCopyFixture.cs gates/fixtures/ldftn-local/RuntimeFieldHandleFixture.cs gates/fixtures/ldftn-local/NullableReboxingFixture.cs gates/fixtures/ldftn-local/RuntimeTypeMaterializationFixture.cs gates/fixtures/ldftn-local/RuntimeMethodHandleFixture.cs gates/fixtures/ldftn-local/RuntimeMethodEnumerationFixture.cs gates/fixtures/ldftn-local/Program.cs gates/fixtures/ldftn-local/GcFinalizerWorkerFixture.cs gates/fixtures/ldftn-local/DelegateVmConstructorFixture.cs gates/fixtures/ldftn-local/DelegateNamedConstructorFixture.cs gates/fixtures/ldftn-local/LdftnLocalFixture.csproj"
source "$(dirname "$0")/_common.sh"

DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|formal-declaring:raw,diet,direct,pointer,address,ldvirtftn,flowing,reflection"
source gates/_formal-declaring-method.sh
source gates/_runtime-type-szarray.sh
DN2CPP_GATE_EXTRA_CONTEXT="${DN2CPP_GATE_EXTRA_CONTEXT:-}|runtime-type-szarray:raw,diet,direct,pointer,getter,address,reflection,generic"

member_not_found_factory_asserts() {
    local output="$1" routes="$2" kind label route
    for label in shape lookup-identity lookup-public lookup-instance lookup-parameter lookup-generic-arity lookup-ignore-case lookup-owner type-identity public-success public-null; do
        gate_output byte-witness "$output" "member-not-found-factory-$label:True" --count 1 --policy crlf || return 1
    done
    for kind in method field property event constructor type; do
        gate_output byte-witness "$output" "member-not-found-factory-public-$kind:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Managed member-not-found factory contract end' --count 1 --policy crlf || return 1
    for route in $routes; do
        for kind in method field property event constructor type; do
            for label in return fresh caller-throw; do
                gate_output byte-witness "$output" "member-not-found-factory-$route-$kind-$label:True" --count 1 --policy crlf || return 1
            done
        done
        gate_output byte-witness "$output" "member-not-found-factory-$route-null:True" --count 1 --policy crlf || return 1
    done
    if [[ " $routes " == *" direct "* ]]; then
        for label in method return fresh; do
            gate_output byte-witness "$output" "member-not-found-factory-delegate-$label:True" --count 1 --policy crlf || return 1
        done
    fi
}

ambiguous_match_factory_asserts() {
    local output="$1" routes="$2" label route
    for label in MemberInfo-shape Attribute-shape public-member public-null-declaring-type public-attribute public-null-text public-text-fault; do
        gate_output byte-witness "$output" "ambiguous-factory-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Managed ambiguous-match factories contract end' --count 1 --policy crlf || return 1
    for route in $routes; do
        for label in member-return member-fresh type-return member-null caller-throw attribute-return attribute-fresh attribute-null attribute-null-text attribute-text-fault attribute-culture; do
            gate_output byte-witness "$output" "ambiguous-factory-$route-$label:True" --count 1 --policy crlf || return 1
        done
    done
}

method_slot_lookup_asserts() {
    local output="$1" routes="$2" member label route
    for member in GetMethodAt GetInterfaceMethodImplementation; do
        gate_output byte-witness "$output" "method-slot-$member-shape:True" --count 1 --policy crlf || return 1
    done
    for label in virtual base-definition invoke; do
        gate_output byte-witness "$output" "method-slot-public-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime method-slot lookup contract end' --count 1 --policy crlf || return 1
    for route in $routes; do
        for label in null null-negative pointer pointer-negative byref byref-negative parameter parameter-negative function-pointer function-pointer-negative negative object class interface value array md-array open-generic large-slot; do
            gate_output byte-witness "$output" "method-slot-GetMethodAt-$route-$label:True" --count 1 --policy crlf || return 1
        done
        for label in both-default receiver-default owner-default valid-handles non-interface-owner pointer-receiver; do
            gate_output byte-witness "$output" "method-slot-GetInterfaceMethodImplementation-$route-$label:True" --count 1 --policy crlf || return 1
        done
        if [[ "$route" != reflection ]]; then
            for label in unsigned-max unsigned-high-bit; do
                gate_output byte-witness "$output" "method-slot-GetMethodAt-$route-$label:True" --count 1 --policy crlf || return 1
            done
        fi
    done
}

virtual_slot_count_asserts() {
    local output="$1" routes="$2" member label route
    for member in GetNumVirtuals GetNumVirtualsAndStaticVirtuals; do
        gate_output byte-witness "$output" "virtual-slot-count-$member-shape:True" --count 1 --policy crlf || return 1
    done
    for label in virtual generic-virtual interface static-virtual base-definition invoke; do
        gate_output byte-witness "$output" "virtual-slot-count-public-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime virtual-slot count contract end' --count 1 --policy crlf || return 1
    for route in $routes; do
        for member in GetNumVirtuals GetNumVirtualsAndStaticVirtuals; do
            for label in object class interface value array pointer byref parameter null; do
                gate_output byte-witness "$output" "virtual-slot-count-$member-$route-$label:True" --count 1 --policy crlf || return 1
            done
        done
    done
}

interface_check_asserts() {
    local output="$1" routes="$2" route label shape
    for route in public $routes; do
        for label in direct inherited marker covariant contravariant definition-marker open-constructed closed primitive enum array md-array generic-array covariant-array integral-array missing wrong-variant unbound md-generic; do
            gate_output byte-witness "$output" "interface-check-$route-$label:True" --count 1 --policy crlf || return 1
        done
        if [[ "$route" == public ]]; then continue; fi
        for label in default-source default-target default-both interface-source interface-before-target class-target identity-not-interface parameter-source parameter-before-target parameter-target default-before-interface default-before-parameter; do
            gate_output byte-witness "$output" "interface-check-$route-$label:True" --count 1 --policy crlf || return 1
        done
        for shape in pointer byref function-pointer; do
            for label in "$shape" "$shape-before-target" "default-before-$shape"; do
                gate_output byte-witness "$output" "interface-check-$route-$label:True" --count 1 --policy crlf || return 1
            done
        done
    done
    for label in shape public-open-shape public-shape-separate public-invoke-handle public-invoke-default public-invoke-null public-bound-handle public-dynamic-handle public-dynamic-default public-object-control reflection-null-target reflection-null-receiver; do
        gate_output byte-witness "$output" "interface-check-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime type interface check artifact end' --count 1 --policy crlf || return 1
}

method_generic_count_asserts() {
    local output="$1" routes="$2" route label
    for route in public $routes; do
        for label in plain open-owner-plain one-definition one-closed two-definition two-closed unused-definition open-owner-definition open-owner-constructed partially-open inherited-definition inherited-closed roundtrip-definition; do
            gate_output byte-witness "$output" "method-generic-count-$route-$label:True" --count 1 --policy crlf || return 1
        done
        if [[ "$route" != public ]]; then
            gate_output byte-witness "$output" "method-generic-count-$route-null:True" --count 1 --policy crlf || return 1
            if [[ "$route" != reflection ]]; then
                gate_output byte-witness "$output" "method-generic-count-$route-no-allocation:True" --count 1 --policy crlf || return 1
            fi
        fi
    done
    for label in shape lookup-0 lookup-1 lookup-2 lookup-missing; do
        gate_output byte-witness "$output" "method-generic-count-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Method generic parameter count contract end' --count 1 --policy crlf || return 1
    if [[ -n "$routes" ]]; then
        gate_output byte-witness "$output" 'Method generic parameter count routes end' --count 1 --policy crlf || return 1
    fi
}

type_cache_handle_asserts() {
    local output="$1" label kind
    for label in GetGCHandle-shape FreeGCHandle-shape enum-names type-identity; do
        gate_output byte-witness "$output" "type-cache-handle-$label:True" --count 1 --policy crlf || return 1
    done
    for kind in Weak WeakTrackResurrection Normal Pinned; do
        for label in null live free; do
            gate_output byte-witness "$output" "type-cache-handle-public-$label-$kind:True" --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" 'Runtime type cache handle contract end' --count 1 --policy crlf
}

type_cache_handle_boundary_asserts() {
    local output="$1" routes="$2" route kind
    type_cache_handle_asserts "$output" || return 1
    for route in $routes; do
        for kind in default Weak WeakTrackResurrection Normal Pinned; do
            gate_output byte-witness "$output" "type-cache-handle-GetGCHandle-$route-$kind:True" --count 1 --policy crlf || return 1
        done
        for kind in default public-slot; do
            gate_output byte-witness "$output" "type-cache-handle-FreeGCHandle-$route-$kind:True" --count 1 --policy crlf || return 1
        done
        gate_output byte-witness "$output" "type-cache-handle-$route-public-slot-intact:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime type cache handle boundary end' --count 1 --policy crlf
}

handle_constructor_asserts() {
    local output="$1" routes="$2" label route
    for route in $routes; do
        for label in null object int struct array generic-string generic-object definition parameter byref pointer; do
            gate_output byte-witness "$output" "handle-ctor-$route-$label:True" --count 1 --policy crlf || return 1
        done
        if [[ "$route" == direct || "$route" == pointer ]]; then
            for label in replace clear; do
                gate_output byte-witness "$output" "handle-ctor-$route-$label:True" --count 1 --policy crlf || return 1
            done
        fi
    done
    for label in shape initobj boxed-replace boxed-clear public-identity; do
        gate_output byte-witness "$output" "handle-ctor-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime type handle constructor end' --count 1 --policy crlf
}

constraint_check_asserts() {
    local output="$1" label
    for label in shape closed-type closed-method parameter-metadata open-valid open-invalid type-null type-arity type-void type-byref method-null method-arity; do
        gate_output byte-witness "$output" "constraint-check-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Private constraint-check contract end' --count 1 --policy crlf
}

constraint_check_boundary_asserts() {
    local output="$1" routes="$2" label route
    constraint_check_asserts "$output" || return 1
    for label in null reference value; do
        for route in $routes; do
            gate_output byte-witness "$output" "constraint-check-$label-$route:PlatformNotSupportedException:System.RuntimeTypeHandle.SatisfiesConstraints: CoreCLR private generic-context validation is not supported in this AOT runtime. Use supported Type.MakeGenericType and MethodInfo.MakeGenericMethod APIs instead." --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" 'Private constraint-check boundary end' --count 1 --policy crlf
}

handle_accessor_asserts() {
    local output="$1" route="$2" label
    for label in default-unchecked default-checked object int struct array generic-string generic-object definition parameter byref; do
        gate_output byte-witness "$output" "handle-accessor-$route-$label:True" --count 1 --policy crlf || return 1
    done
    if [[ "$route" == reflection ]]; then
        for label in shape public-roundtrip generic-valid generic-invalid-argument generic-null-arguments; do
            gate_output byte-witness "$output" "handle-accessor-$label:True" --count 1 --policy crlf || return 1
        done
        gate_output byte-witness "$output" 'Runtime type handle accessors end' --count 1 --policy crlf || return 1
    fi
}

canonical_type_asserts() {
    local output="$1" label
    for label in shape type-identity handle-identity handle-roundtrip generic-definition reference-fields static-fields value-arguments nested-arguments value-owner; do
        gate_output byte-witness "$output" "canonical-type-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Canonical type comparison contract end' --count 1 --policy crlf
}

canonical_type_boundary_asserts() {
    local output="$1" pair route
    canonical_type_asserts "$output" || return 1
    for pair in same reference value; do
        for route in direct pointer reflection; do
            gate_output byte-witness "$output" "canonical-type-$pair-$route:PlatformNotSupportedException:System.RuntimeTypeHandle.CompareCanonicalHandles requires CoreCLR canonical method tables. Use supported public Type and FieldInfo APIs instead." --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" 'Canonical type comparison boundary end' --count 1 --policy crlf
}

signature_blob_asserts() {
    local output="$1" label
    for label in shape-parameter shape-type-parameter shape-calling-convention negative-type-offset negative-callconv plain-modifiers init-modreq init-modopt readonly-modreq function-pointer unmanaged-pointer; do
        gate_output byte-witness "$output" "signature-blob-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Signature blob contract end' --count 1 --policy crlf
}

signature_blob_boundary_asserts() {
    local output="$1" pair label member route
    signature_blob_asserts "$output" || return 1
    for pair in parameter:GetParameterOffsetInternal type-parameter:GetTypeParameterOffsetInternal calling-convention:GetCallingConventionFromFunctionPointerAtOffsetInternal; do
        label="${pair%%:*}"; member="${pair#*:}"
        for route in direct pointer reflection; do
            gate_output byte-witness "$output" "signature-blob-$label-$route:PlatformNotSupportedException:System.Signature.$member: CoreCLR private signature-blob parsing is not supported in this AOT runtime. Use supported public reflection APIs instead." --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" 'Signature blob boundary end' --count 1 --policy crlf
}

handle_copy_asserts() {
    local output="$1" route="$2" label
    for label in type-null type-empty type-values type-null-element; do
        gate_output byte-witness "$output" "handle-copy-$route-$label:True" --count 1 --policy crlf || return 1
    done
    if [[ "$route" == reflection ]]; then
        for label in handles-shape types-shape span-rejected-before-body; do
            gate_output byte-witness "$output" "handle-copy-$route-$label:True" --count 1 --policy crlf || return 1
        done
        gate_output byte-witness "$output" 'Runtime type handle copies end' --count 1 --policy crlf || return 1
    else
        for label in null empty scratch-alias exact-alias allocation stack-alias no-scratch; do
            gate_output byte-witness "$output" "handle-copy-$route-$label:True" --count 1 --policy crlf || return 1
        done
    fi
}

field_handle_ic_members="GetUtf8NameInternal GetAttributes GetApproxDeclaringMethodTable IsFastPathSupported GetInstanceFieldOffset GetStaticFieldAddress GetToken GetStaticFieldForGenericType AcquiresContextFromThis GetLoaderAllocatorInternal"
field_handle_qcall_members="GetFieldDataReference GetValueDirect SetValueDirect GetEnCFieldAddr GetRVAFieldInfo GetValue SetValue"
field_handle_public_asserts() {
    local output="$1" member label
    for label in default-value default-equality default-hash public-access public-metadata private-access static-access readonly-value struct-access generic-access generic-static rva-array rva-span; do
        gate_output byte-witness "$output" "field-handle-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'field-handle-default-validation:ArgumentNullException' --count 1 --policy crlf || return 1
    gate_output byte-witness "$output" 'field-handle-readonly-store:FieldAccessException' --count 1 --policy crlf || return 1
    for member in $field_handle_ic_members $field_handle_qcall_members; do
        gate_output byte-witness "$output" "field-handle-shape-$member:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime field handle metadata end' --count 1 --policy crlf || return 1
    gate_output byte-witness "$output" 'Runtime field handle contract end' --count 1 --policy crlf
}

field_handle_boundary_asserts() {
    local output="$1" member route
    field_handle_public_asserts "$output" || return 1
    for member in $field_handle_ic_members; do
        for route in direct pointer reflection; do
            gate_output byte-witness "$output" "field-handle-$member-$route:PlatformNotSupportedException:System.RuntimeFieldHandle.$member requires CoreCLR field descriptors. Use FieldInfo instead." --count 1 --policy crlf || return 1
        done
    done
    for member in $field_handle_qcall_members; do
        for route in direct pointer; do
            gate_output byte-witness "$output" "field-handle-$member-$route:PlatformNotSupportedException:the native import QCall!RuntimeFieldHandle_$member requires the CoreCLR runtime, which a dn2cpp-transpiled binary does not provide. Use a supported managed API, a NativeImplementation shim, or an explicit --direct-pinvoke binding." --count 1 --policy crlf || return 1
        done
    done
    for member in $field_handle_ic_members $field_handle_qcall_members; do
        for route in direct pointer; do
            gate_output byte-witness "$output" "field-handle-$member-$route-nonmutation:True" --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" 'field-handle-GetRVAFieldInfo-reflection:PlatformNotSupportedException:the native import QCall!RuntimeFieldHandle_GetRVAFieldInfo requires the CoreCLR runtime, which a dn2cpp-transpiled binary does not provide. Use a supported managed API, a NativeImplementation shim, or an explicit --direct-pinvoke binding.' --count 1 --policy crlf || return 1
    gate_output byte-witness "$output" 'field-handle-GetRVAFieldInfo-reflection-nonmutation:True' --count 1 --policy crlf || return 1
    for member in GetFieldDataReference GetValueDirect SetValueDirect GetEnCFieldAddr GetValue SetValue; do
        gate_output byte-witness "$output" "field-handle-$member-reflection:NotSupportedException" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime field handle boundary end' --count 1 --policy crlf
}

method_handle_members="GetAttributes GetImplAttributes GetMethodTable GetSlot GetMethodDef GetUtf8NameInternal HasMethodInstantiation GetStubIfNeededInternal GetMethodFromCanonical IsGenericMethodDefinition IsTypicalMethodDefinition GetGenericParameterCount IsDynamicMethod GetResolver IsConstructor GetLoaderAllocatorInternal"
method_handle_shapes() {
    local output="$1" member
    for member in $method_handle_members; do
        gate_output byte-witness "$output" "method-handle-shape-$member:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime method handle metadata end' --count 1 --policy crlf
}

type_materialize_asserts() {
    local output="$1" label
    for label in public-roundtrip public-array-roundtrip public-default public-value public-object public-base public-invoke shape-method-table shape-method-table-parameter shape-handle shape-slow shape-qcall shape-maybe-null maybe-null-zero; do
        gate_output byte-witness "$output" "type-materialize-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'Runtime type materialization contract end' --count 1 --policy crlf
}

nullable_rebox_asserts() {
    local output="$1" route="$2" label check
    for label in null-identity int-identity string-identity object-identity raw-copy-independent gc-payload public-generic-copyback; do
        gate_output byte-witness "$output" "nullable-rebox-$route-$label:True" --count 1 --policy crlf || return 1
    done
    for label in byte sbyte ulong enum decimal generic-ref generic-value; do
        for check in raw copy value empty; do
            gate_output byte-witness "$output" "nullable-rebox-$route-$label-$check:True" --count 1 --policy crlf || return 1
        done
    done
    for label in 0 1 2 3; do
        gate_output byte-witness "$output" "nullable-rebox-$route-float-bits-$label:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "nullable-rebox-$route-public-float-copyback-$label:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "nullable-rebox-$route-public-float-echo-$label:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "nullable-rebox-$route-array-float-bits-$label:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "nullable-rebox-$route-array-float-empty-$label:True" --count 1 --policy crlf || return 1
    done
    for label in public-return public-copyback constructor-copyback; do
        for check in 0 1; do
            gate_output byte-witness "$output" "nullable-rebox-$route-$label-$check:True" --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" "Nullable reboxing $route end" --count 1 --policy crlf
}

named_ctor_asserts() {
    local output="$1" owner form label check
    for owner in Delegate MulticastDelegate; do
        for form in instance static; do
            label="named-$owner-$form"
            for check in bind identity target method list; do
                gate_output byte-witness "$output" "$label-$check:True" --count 1 --policy crlf || return 1
            done
            gate_output byte-witness "$output" "$label-target-null:ArgumentNullException:target" --count 1 --policy crlf || return 1
            gate_output byte-witness "$output" "$label-method-null:ArgumentNullException:method" --count 1 --policy crlf || return 1
        done
        gate_output byte-witness "$output" "named-$owner-static-initializer:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "named-$owner-static-open:ArgumentException:target" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "named-$owner-static-no-match:True" --count 1 --policy crlf || return 1
        gate_output byte-witness "$output" "named-$owner-instance-failed-unchanged:True" --count 1 --policy crlf || return 1
        for check in case static-only signature; do
            gate_output byte-witness "$output" "named-$owner-instance-$check:ArgumentException:" --count 1 --policy crlf || return 1
        done
    done
    gate_output byte-witness "$output" 'Retained delegate named constructors end' --count 1 --policy crlf || return 1
}

# Heap accounting differs by runtime; assert the public query contracts.
gate_extra_asserts() {
    local out="$1" native="$1/native.stdout" line
    gate_capture_ok "$out/assert-before-gc-memory" run_bounded dotnet "$_CG_APP" before-gc-memory-queries || return 1
    gate_output select "$native" "$out/assert-prefix-gc-memory.stdout" --before '== GC memory query entry points ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-memory.stdout" "$out/assert-before-gc-memory.stdout" || return 1
    for line in '== GC memory query entry points ==' \
        'gc-public-direct-nonnegative:True' 'gc-public-delegate-nonnegative:True' \
        'gc-public-pointer-nonnegative:True' 'gc-public-collect-nonnegative:True' \
        'gc-live-payload:True' 'GC memory query entry points end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-generations" run_bounded dotnet "$_CG_APP" before-gc-generations || return 1
    gate_output select "$native" "$out/assert-prefix-gc-generations.stdout" --before '== GC logical generations ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-generations.stdout" "$out/assert-before-gc-generations.stdout" || return 1
    for line in '== GC logical generations ==' \
        'gc-generation-max-valid:True' 'gc-generation-direct:True' \
        'gc-generation-delegate:True' 'gc-generation-pointer:True' \
        'gc-generation-weak:True' 'gc-generation-collected:True' \
        'gc-generation-weak-collected:True' 'gc-generation-null:ArgumentNullException:obj' \
        'gc-generation-delegate-null:ArgumentNullException:obj' \
        'gc-generation-null-weak:NullReferenceException:' \
        'gc-generation-empty-weak:ArgumentNullException:wo' 'GC logical generations end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-collections" run_bounded dotnet "$_CG_APP" before-gc-collections || return 1
    gate_output select "$native" "$out/assert-prefix-gc-collections.stdout" --before '== GC collection overloads ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-collections.stdout" "$out/assert-before-gc-collections.stdout" || return 1
    for line in '== GC collection overloads ==' 'gc-collect-routes:True' \
        'gc-collect-counter-advanced:True' 'gc-collect-live-payload:True' \
        'gc-collect-negative:ArgumentOutOfRangeException:generation' \
        'gc-collect-negative-first:ArgumentOutOfRangeException:generation' \
        'gc-collect-mode-low:ArgumentOutOfRangeException:mode' \
        'gc-collect-mode-high:ArgumentOutOfRangeException:mode' \
        'gc-collect-aggressive-generation:ArgumentException:generation' \
        'gc-collect-aggressive-blocking:ArgumentException:blocking' \
        'gc-collect-aggressive-compacting:ArgumentException:compacting' 'GC collection overloads end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-worker" run_bounded dotnet "$_CG_APP" before-gc-worker-contract || return 1
    gate_output select "$native" "$out/assert-prefix-gc-worker.stdout" --before '== GC finalizer worker contract ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-worker.stdout" "$out/assert-before-gc-worker.stdout" || return 1
    for line in '== GC finalizer worker contract ==' 'gc-worker-private:True' \
        'gc-worker-static:True' 'gc-worker-nongeneric:True' 'gc-worker-uint32:True' \
        'gc-worker-zero:True' 'GC finalizer worker contract end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-notifications" run_bounded dotnet "$_CG_APP" before-gc-notifications || return 1
    gate_output select "$native" "$out/assert-prefix-gc-notifications.stdout" --before '== GC notification validation ==' --policy crlf || return 1
    gate_output compare "$out/assert-prefix-gc-notifications.stdout" "$out/assert-before-gc-notifications.stdout" || return 1
    for line in '== GC notification validation ==' \
        'fullgc-max-zero:ArgumentOutOfRangeException:maxGenerationThreshold:Argument must be between 1 and 99. (Parameter '"'"'maxGenerationThreshold'"'"')' \
        'fullgc-max-hundred:ArgumentOutOfRangeException:maxGenerationThreshold:Argument must be between 1 and 99. (Parameter '"'"'maxGenerationThreshold'"'"')' \
        'fullgc-loh-zero:ArgumentOutOfRangeException:largeObjectHeapThreshold:Argument must be between 1 and 99. (Parameter '"'"'largeObjectHeapThreshold'"'"')' \
        'fullgc-loh-hundred:ArgumentOutOfRangeException:largeObjectHeapThreshold:Argument must be between 1 and 99. (Parameter '"'"'largeObjectHeapThreshold'"'"')' \
        'fullgc-max-first:ArgumentOutOfRangeException:maxGenerationThreshold:Argument must be between 1 and 99. (Parameter '"'"'maxGenerationThreshold'"'"')' \
        'fullgc-approach-negative:ArgumentOutOfRangeException:millisecondsTimeout:millisecondsTimeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'millisecondsTimeout'"'"')' \
        'fullgc-complete-negative:ArgumentOutOfRangeException:millisecondsTimeout:millisecondsTimeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'millisecondsTimeout'"'"')' \
        'fullgc-approach-timespan:ArgumentOutOfRangeException:timeout:timeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'timeout'"'"')' \
        'fullgc-complete-timespan:ArgumentOutOfRangeException:timeout:timeout ('"'"'-2'"'"') must be greater than or equal to '"'"'-1'"'"'. (Parameter '"'"'timeout'"'"')' \
        'fullgc-approach-overflow:ArgumentOutOfRangeException:timeout:timeout ('"'"'2147483648'"'"') must be less than or equal to '"'"'2147483647'"'"'. (Parameter '"'"'timeout'"'"')' \
        'fullgc-complete-overflow:ArgumentOutOfRangeException:timeout:timeout ('"'"'2147483648'"'"') must be less than or equal to '"'"'2147483647'"'"'. (Parameter '"'"'timeout'"'"')' \
        'GC notification validation end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-gc-pause" run_bounded dotnet "$_CG_APP" before-gc-pause || return 1
    gate_output prefix "$native" "$out/assert-before-gc-pause.stdout" --before '== GC pause duration ==' --policy crlf || return 1
    for line in '== GC pause duration ==' 'pause-nonnegative:True' 'pause-monotonic:True' \
        'pause-advanced:True' 'pause-delegate:True' 'pause-pointer:True' 'GC pause duration end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-delegate-values" run_bounded dotnet "$_CG_APP" before-delegate-values || return 1
    gate_output prefix "$native" "$out/assert-before-delegate-values.stdout" --before '== Retained delegate value members ==' --policy crlf || return 1
    for line in '== Retained delegate value members ==' 'delegate-same:True' 'delegate-same-hash:True' \
        'delegate-closed-type:True' 'delegate-declaration:True' 'delegate-null:True' \
        'delegate-target-same:True' 'delegate-target-same-hash:True' 'delegate-target-other:True' \
        'delegate-chain:True' 'delegate-chain-hash:True' 'delegate-order:True' 'delegate-length:True' \
        'delegate-reflected-equals:True' 'delegate-reflected-hash:True' \
        'delegate-base-equals:True' 'delegate-base-hash:True' \
        'delegate-bound-equals:True' 'delegate-bound-hash:True' 'Retained delegate value members end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/assert-before-multicast-factory" run_bounded dotnet "$_CG_APP" before-multicast-factory || return 1
    gate_output prefix "$native" "$out/assert-before-multicast-factory.stdout" --before '== Retained multicast construction ==' --policy crlf || return 1
    for line in '== Retained multicast construction =='         'factory-order-0:True' 'factory-identity-0:True' 'factory-entries-0:True' 'factory-prefix-0:True'         'factory-order-1:True' 'factory-identity-1:True' 'factory-entries-1:True' 'factory-prefix-1:True'         'factory-order-2:True' 'factory-identity-2:True' 'factory-entries-2:True' 'factory-prefix-2:True'         'factory-closed-type:True' 'factory-public-transitions:True' 'Retained multicast construction end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done
    gate_capture_ok "$out/factory-boundary" run_bounded "./$out/SelfHostPrimSubset$EXE_EXT" native-multicast-factory-boundary || return 1
    for line in 'factory-boundary-0:True' 'factory-boundary-1:True'; do
        gate_output byte-witness "$out/factory-boundary.stdout" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-multicast-operations" run_bounded dotnet "$_CG_APP" before-multicast-operations || return 1
    gate_output prefix "$native" "$out/assert-before-multicast-operations.stdout" --before '== Retained multicast operations ==' --policy crlf || return 1
    for line in '== Retained multicast operations ==' \
        'multicast-combine-order:True' 'multicast-combine-identity:True' 'multicast-null-noop:True' \
        'multicast-no-match:True' 'multicast-remove-last:True' 'multicast-source-immutable:True' \
        'multicast-remove-transitions:True' 'multicast-remove-longer:True' \
        'multicast-protected-mismatch:True' 'multicast-public-mismatch:True' \
        'multicast-closed-types:True' 'Retained multicast operations end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-multicast-accessors" run_bounded dotnet "$_CG_APP" before-multicast-accessors || return 1
    gate_output prefix "$native" "$out/assert-before-multicast-accessors.stdout" --before '== Retained multicast accessors ==' --policy crlf || return 1
    for line in '== Retained multicast accessors ==' \
        'accessor-target:True' 'accessor-method:True' 'accessor-bound:True' 'accessor-list:True' \
        'accessor-list-isolated:True' 'accessor-single:True' 'accessor-index:True' \
        'accessor-closed-method:True' 'accessor-public-enumerator:True' 'Retained multicast accessors end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-vm-constructors" run_bounded dotnet "$_CG_APP" before-vm-constructors || return 1
    gate_output prefix "$native" "$out/assert-before-vm-constructors.stdout" --before '== CoreCLR delegate construction boundary ==' --policy crlf || return 1
    for line in '== CoreCLR delegate construction boundary ==' 'vm-ctor-metadata:True' \
        'vm-ctor-public-value:True' 'vm-ctor-public-instance:True' 'vm-ctor-public-static:True' \
        'vm-ctor-public-generic:True' 'vm-ctor-public-bind:True' 'CoreCLR delegate construction boundary end'; do
        gate_output byte-witness "$native" "$line" --count 1 --policy crlf || return 1
    done

    gate_capture_ok "$out/assert-before-named-constructors" run_bounded dotnet "$_CG_APP" before-named-constructors || return 1
    gate_output prefix "$native" "$out/assert-before-named-constructors.stdout" --before '== Named delegate construction public paths ==' --policy crlf || return 1
    for line in named-public-instance named-public-static named-public-multicast; do
        gate_output byte-witness "$native" "$line:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$native" 'Named delegate construction public paths end' --count 1 --policy crlf || return 1

    gate_capture_ok "$out/assert-before-method-enum" run_bounded dotnet "$_CG_APP" before-runtime-method-enumeration || return 1
    gate_output prefix "$native" "$out/assert-before-method-enum.stdout" --before '== Runtime method enumeration contract ==' --policy crlf || return 1
    for label in first-shape next-shape public-methods public-ctors public-invoke public-delegate; do
        gate_output byte-witness "$native" "method-enum-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$native" 'Runtime method enumeration contract end' --count 1 --policy crlf || return 1

    gate_capture_ok "$out/assert-before-method-handle" run_bounded dotnet "$_CG_APP" before-runtime-method-handle || return 1
    gate_output prefix "$native" "$out/assert-before-method-handle.stdout" --before '== Runtime method handle contract ==' --policy crlf || return 1
    for label in default-value default-equality default-hash public-attributes public-constructor public-generic public-invoke; do
        gate_output byte-witness "$native" "method-handle-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$native" 'method-handle-default-validation:ArgumentNullException' --count 1 --policy crlf || return 1
    method_handle_shapes "$native" || return 1
    gate_output byte-witness "$native" 'Runtime method handle contract end' --count 1 --policy crlf || return 1

    gate_capture_ok "$out/assert-before-type-materialize" run_bounded dotnet "$_CG_APP" before-runtime-type-materialization || return 1
    gate_output prefix "$native" "$out/assert-before-type-materialize.stdout" --before '== Runtime type materialization contract ==' --policy crlf || return 1
    type_materialize_asserts "$native" || return 1

    gate_capture_ok "$out/assert-before-nullable-rebox" run_bounded dotnet "$_CG_APP" before-nullable-reboxing || return 1
    gate_output prefix "$native" "$out/assert-before-nullable-rebox.stdout" --before '== Nullable reboxing contract ==' --policy crlf || return 1
    nullable_rebox_asserts "$native" reflection || return 1
    for label in from-shape to-shape; do
        gate_output byte-witness "$native" "nullable-rebox-reflection-$label:True" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$native" 'Nullable reboxing contract end' --count 1 --policy crlf || return 1
    gate_capture_ok "$out/assert-before-field-handle" run_bounded dotnet "$_CG_APP" before-runtime-field-handle || return 1
    gate_output prefix "$native" "$out/assert-before-field-handle.stdout" --before '== Runtime field handle contract ==' --policy crlf || return 1
    field_handle_public_asserts "$native" || return 1
    gate_capture_ok "$out/assert-before-handle-copy" run_bounded dotnet "$_CG_APP" before-runtime-type-handle-copy || return 1
    gate_output prefix "$native" "$out/assert-before-handle-copy.stdout" --before '== Runtime type handle copies ==' --policy crlf || return 1
    handle_copy_asserts "$native" reflection || return 1
    gate_capture_ok "$out/assert-before-signature-blob" run_bounded dotnet "$_CG_APP" before-signature-blob || return 1
    gate_output prefix "$native" "$out/assert-before-signature-blob.stdout" --before '== Signature blob contract ==' --policy crlf || return 1
    signature_blob_asserts "$native" || return 1
    gate_capture_ok "$out/assert-before-canonical-type" run_bounded dotnet "$_CG_APP" before-canonical-type-comparison || return 1
    gate_output prefix "$native" "$out/assert-before-canonical-type.stdout" --before '== Canonical type comparison contract ==' --policy crlf || return 1
    canonical_type_asserts "$native" || return 1
    gate_capture_ok "$out/assert-before-handle-accessor" run_bounded dotnet "$_CG_APP" before-runtime-type-handle-accessor || return 1
    gate_output prefix "$native" "$out/assert-before-handle-accessor.stdout" --before '== Runtime type handle accessors ==' --policy crlf || return 1
    handle_accessor_asserts "$native" reflection || return 1
    gate_capture_ok "$out/assert-before-constraint-check" run_bounded dotnet "$_CG_APP" before-constraint-check || return 1
    gate_output prefix "$native" "$out/assert-before-constraint-check.stdout" --before '== Private constraint-check contract ==' --policy crlf || return 1
    constraint_check_asserts "$native" || return 1
    gate_capture_ok "$out/assert-before-handle-constructor" run_bounded dotnet "$_CG_APP" before-runtime-type-handle-constructor || return 1
    gate_output prefix "$native" "$out/assert-before-handle-constructor.stdout" --before '== Runtime type handle constructor ==' --policy crlf || return 1
    handle_constructor_asserts "$native" reflection || return 1
    gate_capture_ok "$out/assert-before-type-cache-handle" run_bounded dotnet "$_CG_APP" before-runtime-type-cache-handle || return 1
    gate_output prefix "$native" "$out/assert-before-type-cache-handle.stdout" --before '== Runtime type cache handle contract ==' --policy crlf || return 1
    type_cache_handle_asserts "$native" || return 1
    gate_capture_ok "$out/assert-before-method-generic-count" run_bounded dotnet "$_CG_APP" before-method-generic-count || return 1
    gate_output prefix "$native" "$out/assert-before-method-generic-count.stdout" --before '== Method generic parameter count contract ==' --policy crlf || return 1
    method_generic_count_asserts "$native" '' || return 1
    gate_capture_ok "$out/assert-before-virtual-slot-count" run_bounded dotnet "$_CG_APP" before-runtime-virtual-slot-count || return 1
    gate_output prefix "$native" "$out/assert-before-virtual-slot-count.stdout" --before '== Runtime virtual-slot count contract ==' --policy crlf || return 1
    virtual_slot_count_asserts "$native" '' || return 1
    gate_capture_ok "$out/assert-before-method-slot-lookup" run_bounded dotnet "$_CG_APP" before-runtime-method-slot-lookup || return 1
    gate_output prefix "$native" "$out/assert-before-method-slot-lookup.stdout" --before '== Runtime method-slot lookup contract ==' --policy crlf || return 1
    method_slot_lookup_asserts "$native" '' || return 1
    gate_capture_ok "$out/assert-before-ambiguous-factory" run_bounded dotnet "$_CG_APP" before-ambiguous-match-factory || return 1
    gate_output prefix "$native" "$out/assert-before-ambiguous-factory.stdout" --before '== Managed ambiguous-match factories ==' --policy crlf || return 1
    ambiguous_match_factory_asserts "$native" '' || return 1
    gate_capture_ok "$out/assert-before-member-not-found-factory" run_bounded dotnet "$_CG_APP" before-member-not-found-factory || return 1
    gate_output prefix "$native" "$out/assert-before-member-not-found-factory.stdout" --before '== Managed member-not-found factory ==' --policy crlf || return 1
    member_not_found_factory_asserts "$native" '' || return 1
    gate_capture_ok "$out/assert-before-formal-declaring-method" run_bounded dotnet "$_CG_APP" before-formal-declaring-method || return 1
    gate_output prefix "$native" "$out/assert-before-formal-declaring-method.stdout" --before '== Formal declaring method contract ==' --policy crlf || return 1
    formal_declaring_contract_asserts "$native" || return 1
    gate_capture_ok "$out/assert-before-runtime-type-szarray" run_bounded dotnet "$_CG_APP" before-runtime-type-szarray || return 1
    gate_output prefix "$native" "$out/assert-before-runtime-type-szarray.stdout" --before '== Runtime type SZ-array contract ==' --policy crlf || return 1
    runtime_type_szarray_contract_asserts "$native" || return 1
}

# Validate the negative-offset return before the CLR oracle allocates an uninitialized Signature.
build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --signature-blob-verify "$(resolve_net10_corelib)"

corelib_diff_gate SelfHostPrimSubset System.Linq System.Diagnostics.TraceSource \
    System.Collections.Specialized

# The real runner source is linked into this small fixture so this block proves
# its MethodDef interception independently of a full self-host native build.
probe_project=gates/fixtures/tool-process/ToolProcessProbe.csproj
build_gate_proj "$probe_project"
probe_bin="$PWD/gates/fixtures/tool-process/bin/$CONFIG/$TFM"
probe_out="artifacts/selfhost-tool-process-$CONFIG"
tool_process_diagnostics() {
    [ "${_GATE_EXIT_RC:-0}" -ne 0 ] || return 0
    local log
    for log in "$probe_out"/*.out "$probe_out"/*.err; do
        [ -f "$log" ] || continue
        printf '\n---- %s ----\n' "$log" >&2
        cat "$log" >&2
    done
}
gate_add_exit_hook tool_process_diagnostics
mkdir -p "$probe_out/child path 日本語"
cp -R "$probe_bin/." "$probe_out/child path 日本語/"
probe_child="$PWD/$probe_out/child path 日本語/ToolProcessProbe$EXE_EXT"
invoke_cli "$probe_bin/ToolProcessProbe.dll" --auto-ref -o "$probe_out/emitted"
compile_console "$probe_out/emitted" ToolProcessProbe
run_bounded "$PWD/$probe_out/emitted/ToolProcessProbe$EXE_EXT" "$probe_child" \
    > "$probe_out/native.out" 2> "$probe_out/native.err"
run_bounded dotnet "$probe_bin/ToolProcessProbe.dll" "$probe_child" \
    > "$probe_out/managed.out" 2> "$probe_out/managed.err"
diff -u "$probe_out/managed.out" "$probe_out/native.out"
diff -u "$probe_out/managed.err" "$probe_out/native.err"
grep -Fx 'child exit 23' "$probe_out/native.out"
grep -Fx 'launch failure caught' "$probe_out/native.out"
grep -Fx 'NUL argument rejected' "$probe_out/native.out"
grep -Fx 'tool process complete' "$probe_out/native.out"
grep -Fx 'child stderr' "$probe_out/native.err"

# Bare names must resolve through PATH in both host and native launchers.
PATH="$PWD/$probe_out/child path 日本語:$PATH" \
    run_bounded "$PWD/$probe_out/emitted/ToolProcessProbe$EXE_EXT" "ToolProcessProbe$EXE_EXT" \
    > "$probe_out/path-native.out" 2> "$probe_out/path-native.err"
diff -u "$probe_out/managed.out" "$probe_out/path-native.out"
diff -u "$probe_out/managed.err" "$probe_out/path-native.err"

# Never execute the private CLR worker as an oracle. Verify its declaration/IL,
# then patch only an artifact copy for explicit native-only refusal checks.
gc_worker_boundary_asserts() {
    local output="$1" label
    for label in worker-direct worker-delegate worker-pointer worker-reflected-delegate worker-reflection; do
        gate_output byte-witness "$output" "$label:PlatformNotSupportedException:System.GC.RunFinalizers is a CoreCLR-owned worker entry point; dn2cpp owns finalization. Use GC.WaitForPendingFinalizers instead." --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'GC finalizer worker boundary end' --count 1 --policy crlf || return 1
}
build_gate_proj gates/fixtures/ldftn-local/LdftnLocalFixture.csproj
worker_input="artifacts/gc-worker-input-$CONFIG"
mkdir -p "$worker_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$worker_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --gc-finalizer-worker "$worker_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
worker_out="artifacts/gc-worker-boundary-$CONFIG"
invoke_cli "$worker_input/SelfHostPrimSubset.dll" -r "$_CG_CORELIB" \
    -r "$(dirname "$_CG_CORELIB")/System.Linq.dll" \
    -r "$(dirname "$_CG_CORELIB")/System.Diagnostics.TraceSource.dll" \
    -r "$(dirname "$_CG_CORELIB")/System.Collections.Specialized.dll" -o "$worker_out"
compile_console "$worker_out" GcWorkerBoundary
gate_capture_ok "$worker_out/native" run_bounded "./$worker_out/GcWorkerBoundary$EXE_EXT" native-finalizer-boundary
gc_worker_boundary_asserts "$worker_out/native.stdout"

# Intentional native capability boundary, separate from CLR validation parity.
# CoreCLR may register successfully; never execute this mode as a CLR oracle.
gc_notification_unavailable_asserts() {
    local output="$1" label
    for label in fullgc-register-direct fullgc-register-delegate fullgc-register-pointer fullgc-cancel-direct fullgc-cancel-delegate; do
        gate_output byte-witness "$output" "$label:InvalidOperationException::This API is not available when the concurrent GC is enabled." --count 1 --policy crlf || return 1
    done
    for label in fullgc-approach-default fullgc-complete-default fullgc-approach-zero fullgc-complete-positive fullgc-approach-pointer fullgc-approach-time fullgc-complete-time fullgc-approach-fraction fullgc-complete-upper; do
        gate_output byte-witness "$output" "$label:NotApplicable" --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'GC notification unavailable end' --count 1 --policy crlf || return 1
}
gate_capture_ok "$_CG_OUT/notifications" run_bounded "./$_CG_OUT/SelfHostPrimSubset$EXE_EXT" native-gc-notification-unavailable
gc_notification_unavailable_asserts "$_CG_OUT/notifications.stdout"

# VM constructor stubs require CoreCLR PCODE/loader state. Only the native copy
# executes the rewritten direct/address sites and the explicit refusal probes.
vm_input="artifacts/delegate-vm-input-$CONFIG"
mkdir -p "$vm_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$vm_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll"     --delegate-vm-constructors "$vm_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
vm_out="artifacts/delegate-vm-boundary-$CONFIG"
invoke_cli "$vm_input/SelfHostPrimSubset.dll" -r "$_CG_CORELIB"     -r "$(dirname "$_CG_CORELIB")/System.Linq.dll"     -r "$(dirname "$_CG_CORELIB")/System.Diagnostics.TraceSource.dll"     -r "$(dirname "$_CG_CORELIB")/System.Collections.Specialized.dll" -o "$vm_out"
compile_console "$vm_out" DelegateVmBoundary
gate_capture_ok "$vm_out/native" run_bounded "./$vm_out/DelegateVmBoundary$EXE_EXT" native-vm-constructor-boundary
vm_ctor_refusal_asserts() {
    local output="$1" member route
    for member in CtorClosed CtorClosedStatic CtorRTClosed CtorOpened CtorVirtualDispatch CtorCollectibleClosedStatic CtorCollectibleOpened CtorCollectibleVirtualDispatch; do
        for route in direct reflection; do
            gate_output byte-witness "$output" "vm-$member-$route:PlatformNotSupportedException:System.MulticastDelegate.$member is a CoreCLR-owned delegate initialization stub. Use normal delegate construction or Delegate.CreateDelegate instead." --count 1 --policy crlf || return 1
        done
    done
    for route in pointer bound; do
        gate_output byte-witness "$output" "vm-$route:PlatformNotSupportedException:System.MulticastDelegate.CtorRTClosed is a CoreCLR-owned delegate initialization stub. Use normal delegate construction or Delegate.CreateDelegate instead." --count 1 --policy crlf || return 1
    done
    gate_output byte-witness "$output" 'vm-ctor-receiver-unchanged:True' --count 1 --policy crlf || return 1
    gate_output byte-witness "$output" 'CoreCLR delegate constructor refusal end' --count 1 --policy crlf || return 1
}
vm_ctor_refusal_asserts "$vm_out/native.stdout"

# Direct protected calls live only in an artifact with an isolated entry point.
named_input="artifacts/delegate-named-input-$CONFIG"
mkdir -p "$named_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$named_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --delegate-named-constructors "$named_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
named_out="artifacts/delegate-named-$CONFIG"
invoke_cli "$named_input/SelfHostPrimSubset.dll" --auto-ref -o "$named_out"
compile_console "$named_out" DelegateNamed
gate_capture_ok "$named_out/direct" run_bounded "./$named_out/DelegateNamed$EXE_EXT" native-boundary
for label in delegate-instance delegate-static multicast-instance multicast-static; do
    gate_output byte-witness "$named_out/direct.stdout" "named-direct-$label:True" --count 1 --policy crlf
done

# This CLR-only public oracle verifies why native declaration slots cannot stand in
# for VM slot counts. It does not claim native GetInterfaceMap support.
gate_capture_ok "$_CG_OUT/virtual-slot-order-clr" run_bounded dotnet \
    "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" --runtime-virtual-slot-order-oracle
gate_output byte-witness "$_CG_OUT/virtual-slot-order-clr.stdout" \
    'virtual-slot-order-clr:InstanceMiddle,DefaultInstance,GenericInstance,StaticFirst,StaticDefault' --count 1 --policy crlf
gate_output byte-witness "$_CG_OUT/virtual-slot-order-clr.stdout" \
    'virtual-slot-order-clr:default-and-inherited:True' --count 1 --policy crlf

# Public CLR reflection supplies the message oracle; imported private factories run only natively.
for ambiguous_mode in all reflection; do
    ambiguous_input="artifacts/ambiguous-match-factory-input-$ambiguous_mode-$CONFIG"
    mkdir -p "$ambiguous_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$ambiguous_input/"
    ambiguous_flag=--ambiguous-match-factory
    ambiguous_routes='direct pointer reflection'
    if [[ "$ambiguous_mode" == reflection ]]; then
        ambiguous_flag=--ambiguous-match-factory-reflection
        ambiguous_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$ambiguous_flag" "$ambiguous_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    ambiguous_out="artifacts/ambiguous-match-factory-$ambiguous_mode-$CONFIG"
    invoke_cli "$ambiguous_input/SelfHostPrimSubset.dll" --auto-ref -o "$ambiguous_out"
    compile_console "$ambiguous_out" AmbiguousMatchFactory
    gate_capture_ok "$ambiguous_out/native" run_bounded "./$ambiguous_out/AmbiguousMatchFactory$EXE_EXT"
    gate_capture_ok "$ambiguous_out/oracle" run_bounded dotnet "$_CG_APP" ambiguous-match-factory-contract
    gate_output prefix "$ambiguous_out/native.stdout" "$ambiguous_out/oracle.stdout" \
        --before "ambiguous-factory-${ambiguous_routes%% *}-member-return:True" --policy crlf
    ambiguous_match_factory_asserts "$ambiguous_out/native.stdout" "$ambiguous_routes"
    gate_output byte-witness "$ambiguous_out/native.stdout" 'Managed ambiguous-match factories artifact end' --count 1 --policy crlf
done

# The public lookup's null guard is distinct from the factory's Name dereference.
for member_factory_mode in all reflection; do
    member_factory_input="artifacts/member-not-found-factory-input-$member_factory_mode-$CONFIG"
    mkdir -p "$member_factory_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$member_factory_input/"
    member_factory_flag=--member-not-found-factory
    member_factory_routes='direct pointer reflection'
    if [[ "$member_factory_mode" == reflection ]]; then
        member_factory_flag=--member-not-found-factory-reflection
        member_factory_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$member_factory_flag" "$member_factory_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    member_factory_out="artifacts/member-not-found-factory-$member_factory_mode-$CONFIG"
    invoke_cli "$member_factory_input/SelfHostPrimSubset.dll" --auto-ref -o "$member_factory_out"
    compile_console "$member_factory_out" MemberNotFoundFactory
    gate_capture_ok "$member_factory_out/native" run_bounded "./$member_factory_out/MemberNotFoundFactory$EXE_EXT"
    gate_capture_ok "$member_factory_out/oracle" run_bounded dotnet "$_CG_APP" member-not-found-factory-contract
    gate_output prefix "$member_factory_out/native.stdout" "$member_factory_out/oracle.stdout" \
        --before "member-not-found-factory-${member_factory_routes%% *}-method-return:True" --policy crlf
    member_not_found_factory_asserts "$member_factory_out/native.stdout" "$member_factory_routes"
    gate_output byte-witness "$member_factory_out/native.stdout" 'Managed member-not-found factory artifact end' --count 1 --policy crlf
done

# VM lookups preserve managed validation before refusing their private descriptor ABI.
for lookup_mode in all reflection; do
    lookup_input="artifacts/runtime-method-slot-lookup-input-$lookup_mode-$CONFIG"
    mkdir -p "$lookup_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$lookup_input/"
    lookup_flag=--runtime-method-slot-lookup
    lookup_routes='direct pointer reflection'
    if [[ "$lookup_mode" == reflection ]]; then
        lookup_flag=--runtime-method-slot-lookup-reflection
        lookup_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$lookup_flag" "$lookup_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    lookup_out="artifacts/runtime-method-slot-lookup-$lookup_mode-$CONFIG"
    invoke_cli "$lookup_input/SelfHostPrimSubset.dll" --auto-ref -o "$lookup_out"
    compile_console "$lookup_out" RuntimeMethodSlotLookup
    gate_capture_ok "$lookup_out/native" run_bounded "./$lookup_out/RuntimeMethodSlotLookup$EXE_EXT"
    gate_capture_ok "$lookup_out/oracle" run_bounded dotnet "$_CG_APP" runtime-method-slot-lookup-contract
    gate_output prefix "$lookup_out/native.stdout" "$lookup_out/oracle.stdout" \
        --before "method-slot-GetMethodAt-${lookup_routes%% *}-null:True" --policy crlf
    method_slot_lookup_asserts "$lookup_out/native.stdout" "$lookup_routes"
    gate_output byte-witness "$lookup_out/native.stdout" 'Runtime method-slot lookup boundary end' --count 1 --policy crlf
done

# VM counts refuse on native only; both runtimes execute the public dispatch prefix.
for slot_mode in all reflection; do
    slot_input="artifacts/runtime-virtual-slot-count-input-$slot_mode-$CONFIG"
    mkdir -p "$slot_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$slot_input/"
    slot_flag=--runtime-virtual-slot-count
    slot_routes='direct pointer reflection'
    if [[ "$slot_mode" == reflection ]]; then
        slot_flag=--runtime-virtual-slot-count-reflection
        slot_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$slot_flag" "$slot_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    slot_out="artifacts/runtime-virtual-slot-count-$slot_mode-$CONFIG"
    invoke_cli "$slot_input/SelfHostPrimSubset.dll" --auto-ref -o "$slot_out"
    compile_console "$slot_out" RuntimeVirtualSlotCount
    gate_capture_ok "$slot_out/native" run_bounded "./$slot_out/RuntimeVirtualSlotCount$EXE_EXT"
    gate_capture_ok "$slot_out/oracle" run_bounded dotnet "$_CG_APP" runtime-virtual-slot-count-contract
    gate_output prefix "$slot_out/native.stdout" "$slot_out/oracle.stdout" \
        --before "virtual-slot-count-GetNumVirtuals-${slot_routes%% *}-object:True" --policy crlf
    virtual_slot_count_asserts "$slot_out/native.stdout" "$slot_routes"
    gate_output byte-witness "$slot_out/native.stdout" 'Runtime virtual-slot count boundary end' --count 1 --policy crlf
done

# The managed bridge uses native identities; CLR runs public relation queries only.
for interface_mode in all reflection; do
    interface_input="artifacts/runtime-type-interface-check-input-$interface_mode-$CONFIG"
    mkdir -p "$interface_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$interface_input/"
    interface_flag=--runtime-type-interface-check
    interface_routes='direct pointer reflection'
    if [[ "$interface_mode" == reflection ]]; then
        interface_flag=--runtime-type-interface-check-reflection
        interface_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$interface_flag" "$interface_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    interface_out="artifacts/runtime-type-interface-check-$interface_mode-$CONFIG"
    invoke_cli "$interface_input/SelfHostPrimSubset.dll" --auto-ref -o "$interface_out"
    compile_console "$interface_out" RuntimeTypeInterfaceCheck
    gate_capture_ok "$interface_out/native" run_bounded "./$interface_out/RuntimeTypeInterfaceCheck$EXE_EXT"
    gate_capture_ok "$interface_out/oracle" run_bounded dotnet "$_CG_APP" runtime-type-interface-check-contract
    gate_output prefix "$interface_out/native.stdout" "$interface_out/oracle.stdout" \
        --before "interface-check-${interface_routes%% *}-direct:True" --policy crlf
    interface_check_asserts "$interface_out/native.stdout" "$interface_routes"
done

# The internal getter consumes native MethodInfo handles; CLR supplies the public count oracle.
for count_mode in all reflection; do
    count_input="artifacts/method-generic-count-input-$count_mode-$CONFIG"
    mkdir -p "$count_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$count_input/"
    count_flag=--method-generic-count
    count_routes='direct virtual pointer virtual-pointer reflection'
    if [[ "$count_mode" == reflection ]]; then
        count_flag=--method-generic-count-reflection
        count_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$count_flag" "$count_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    count_out="artifacts/method-generic-count-$count_mode-$CONFIG"
    invoke_cli "$count_input/SelfHostPrimSubset.dll" --auto-ref -o "$count_out"
    compile_console "$count_out" MethodGenericCount
    gate_capture_ok "$count_out/native" run_bounded "./$count_out/MethodGenericCount$EXE_EXT"
    gate_capture_ok "$count_out/oracle" run_bounded dotnet "$_CG_APP" method-generic-count-contract
    gate_output prefix "$count_out/native.stdout" "$count_out/oracle.stdout" \
        --before "method-generic-count-${count_routes%% *}-plain:True" --policy crlf
    method_generic_count_asserts "$count_out/native.stdout" "$count_routes"
done

# The cache allocator wrappers are native-only; CLR runs the public handle contract.
for cache_mode in all reflection; do
    cache_input="artifacts/runtime-type-cache-handle-input-$cache_mode-$CONFIG"
    mkdir -p "$cache_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$cache_input/"
    cache_flag=--runtime-type-cache-handle
    cache_routes='direct pointer reflection'
    if [[ "$cache_mode" == reflection ]]; then
        cache_flag=--runtime-type-cache-handle-reflection
        cache_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$cache_flag" "$cache_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    cache_out="artifacts/runtime-type-cache-handle-$cache_mode-$CONFIG"
    invoke_cli "$cache_input/SelfHostPrimSubset.dll" --auto-ref -o "$cache_out"
    compile_console "$cache_out" RuntimeTypeCacheHandle
    gate_capture_ok "$cache_out/native" run_bounded "./$cache_out/RuntimeTypeCacheHandle$EXE_EXT"
    gate_capture_ok "$cache_out/oracle" run_bounded dotnet "$_CG_APP" runtime-type-cache-handle-contract
    gate_output prefix "$cache_out/native.stdout" "$cache_out/oracle.stdout" \
        --before "type-cache-handle-GetGCHandle-${cache_routes%% *}-default:True" --policy crlf
    type_cache_handle_boundary_asserts "$cache_out/native.stdout" "$cache_routes"
done
for owner in Delegate MulticastDelegate; do
    for form in instance static; do
        gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-$form:PlatformNotSupportedException:" --count 1 --policy crlf
    done
    gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-missing-instance:ArgumentException:" --count 1 --policy crlf
    gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-unchanged:True" --count 1 --policy crlf
    for label in copied-instance copied-static removed-entry; do
        gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-$label:PlatformNotSupportedException:" --count 1 --policy crlf
    done
    gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-copied-missing:ArgumentException:" --count 1 --policy crlf
    for label in copied-unchanged clone-independent removed-unchanged; do
        gate_output byte-witness "$named_out/direct.stdout" "named-hybrid-$owner-$label:True" --count 1 --policy crlf
    done
done
gate_output byte-witness "$named_out/direct.stdout" 'Named constructor artifact end' --count 1 --policy crlf
gate_capture_ok "$named_out/reflection" run_bounded "./$named_out/DelegateNamed$EXE_EXT" contracts
named_ctor_asserts "$named_out/reflection.stdout"
gate_capture_ok "$named_out/oracle" run_bounded dotnet "$_CG_APP" named-contracts-only
gate_output compare "$named_out/reflection.stdout" "$named_out/oracle.stdout" --policy crlf

# The private VM leaves are never invoked by the CLR oracle. Generated artifact
# calls and calli sites use their actual CoreLib value/byref signatures.
enum_input="artifacts/runtime-method-enum-input-$CONFIG"
mkdir -p "$enum_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$enum_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-method-enumeration "$enum_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
enum_out="artifacts/runtime-method-enum-$CONFIG"
invoke_cli "$enum_input/SelfHostPrimSubset.dll" --auto-ref -o "$enum_out"
compile_console "$enum_out" RuntimeMethodEnum
gate_capture_ok "$enum_out/native" run_bounded "./$enum_out/RuntimeMethodEnum$EXE_EXT"
for leaf in first next; do
    member=GetFirstIntroducedMethod
    [ "$leaf" != next ] || member=GetNextIntroducedMethod
    for route in direct pointer reflection; do
        gate_output byte-witness "$enum_out/native.stdout" "method-enum-$leaf-$route:PlatformNotSupportedException:System.RuntimeTypeHandle.$member requires CoreCLR method descriptors. Use Type.GetMethods or Type.GetConstructors instead." --count 1 --policy crlf
    done
done
gate_output byte-witness "$enum_out/native.stdout" 'method-enum-constructor:PlatformNotSupportedException:System.RuntimeTypeHandle.GetFirstIntroducedMethod requires CoreCLR method descriptors. Use Type.GetMethods or Type.GetConstructors instead.' --count 1 --policy crlf
gate_output byte-witness "$enum_out/native.stdout" 'method-enum-default-empty:True' --count 1 --policy crlf
gate_output byte-witness "$enum_out/native.stdout" 'Runtime method enumeration boundary end' --count 1 --policy crlf

# Native-only calls use imported value/interface/pointer signatures. The CLR
# oracle inspects metadata and public/default handles, never raw VM leaves.
handle_input="artifacts/runtime-method-handle-input-$CONFIG"
mkdir -p "$handle_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$handle_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-method-handle "$handle_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
handle_out="artifacts/runtime-method-handle-$CONFIG"
invoke_cli "$handle_input/SelfHostPrimSubset.dll" --auto-ref -o "$handle_out"
compile_console "$handle_out" RuntimeMethodHandle
gate_capture_ok "$handle_out/native" run_bounded "./$handle_out/RuntimeMethodHandle$EXE_EXT"
method_handle_shapes "$handle_out/native.stdout"
for member in $method_handle_members; do
    for route in direct pointer reflection; do
        gate_output byte-witness "$handle_out/native.stdout" "method-handle-$member-$route:PlatformNotSupportedException:System.RuntimeMethodHandle.$member requires CoreCLR method descriptors. Use MethodInfo or ConstructorInfo instead." --count 1 --policy crlf
    done
done
gate_output byte-witness "$handle_out/native.stdout" 'method-handle-managed-overload:NullReferenceException:Object reference not set to an instance of an object.' --count 1 --policy crlf
gate_output byte-witness "$handle_out/native.stdout" 'Runtime method handle boundary end' --count 1 --policy crlf

# Raw VM pointers execute only in the native artifact, never on the CLR oracle.
type_input="artifacts/runtime-type-materialization-input-$CONFIG"
mkdir -p "$type_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$type_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-type-materialization "$type_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
type_out="artifacts/runtime-type-materialization-$CONFIG"
invoke_cli "$type_input/SelfHostPrimSubset.dll" --auto-ref -o "$type_out"
compile_console "$type_out" RuntimeTypeMaterialization
gate_capture_ok "$type_out/native" run_bounded "./$type_out/RuntimeTypeMaterialization$EXE_EXT"
type_materialize_asserts "$type_out/native.stdout"
for shape in method-table handle slow qcall maybe-null; do
    case "$shape" in
        method-table) member=GetRuntimeType;;
        handle) member=GetRuntimeTypeFromHandle;;
        slow|qcall) member=GetRuntimeTypeFromHandleSlow;;
        maybe-null) member=GetRuntimeTypeFromHandleMaybeNull;;
    esac
    for route in direct pointer; do
        for value in 0 1; do
            if [ "$shape" = maybe-null ] && [ "$value" = 0 ]; then
                gate_output byte-witness "$type_out/native.stdout" "type-materialize-$shape-$route-$value:True" --count 1 --policy crlf
            else
                gate_output byte-witness "$type_out/native.stdout" "type-materialize-$shape-$route-$value:PlatformNotSupportedException:System.RuntimeTypeHandle.$member requires CoreCLR type handles. Use Type.TypeHandle and Type.GetTypeFromHandle instead." --count 1 --policy crlf
            fi
        done
    done
    if [ "$shape" = qcall ]; then
        gate_output byte-witness "$type_out/native.stdout" 'type-materialize-qcall-reflection:NotSupportedException:Cannot create boxed ByRef-like values.' --count 1 --policy crlf
    elif [ "$shape" != method-table ]; then
        gate_output byte-witness "$type_out/native.stdout" "type-materialize-$shape-reflection:PlatformNotSupportedException:System.RuntimeTypeHandle.$member requires CoreCLR type handles. Use Type.TypeHandle and Type.GetTypeFromHandle instead." --count 1 --policy crlf
    fi
done
gate_output byte-witness "$type_out/native.stdout" 'Runtime type materialization boundary end' --count 1 --policy crlf

# CLR uses only valid managed objects; native direct/address sites import the
# exact managed declarations instead of invoking raw MethodTable helpers.
rebox_input="artifacts/nullable-reboxing-input-$CONFIG"
mkdir -p "$rebox_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$rebox_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --nullable-reboxing "$rebox_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
rebox_out="artifacts/nullable-reboxing-$CONFIG"
invoke_cli "$rebox_input/SelfHostPrimSubset.dll" --auto-ref -o "$rebox_out"
compile_console "$rebox_out" NullableReboxing
gate_capture_ok "$rebox_out/native" run_bounded "./$rebox_out/NullableReboxing$EXE_EXT"
gate_capture_ok "$rebox_out/oracle" run_bounded dotnet "$_CG_APP" nullable-reboxing-artifact
gate_output compare "$rebox_out/native.stdout" "$rebox_out/oracle.stdout" --policy crlf
for route in reflection direct pointer; do
    nullable_rebox_asserts "$rebox_out/native.stdout" "$route"
done
gate_output byte-witness "$rebox_out/native.stdout" 'Nullable reboxing artifact end' --count 1 --policy crlf

# FieldDesc InternalCalls use the exact boundary; QCall leaves retain their
# existing import refusal. Byref-like QCall parameters stop reflection at validation.
field_input="artifacts/runtime-field-handle-input-$CONFIG"
mkdir -p "$field_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$field_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-field-handle "$field_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
field_out="artifacts/runtime-field-handle-$CONFIG"
invoke_cli "$field_input/SelfHostPrimSubset.dll" --auto-ref -o "$field_out"
compile_console "$field_out" RuntimeFieldHandle
gate_capture_ok "$field_out/native" run_bounded "./$field_out/RuntimeFieldHandle$EXE_EXT"
field_handle_boundary_asserts "$field_out/native.stdout"

# Raw private parser calls execute only in the native artifact, never on CLR.
signature_input="artifacts/signature-blob-input-$CONFIG"
mkdir -p "$signature_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$signature_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --signature-blob "$signature_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
signature_out="artifacts/signature-blob-$CONFIG"
invoke_cli "$signature_input/SelfHostPrimSubset.dll" --auto-ref -o "$signature_out"
compile_console "$signature_out" SignatureBlob
gate_capture_ok "$signature_out/native" run_bounded "./$signature_out/SignatureBlob$EXE_EXT"
signature_blob_boundary_asserts "$signature_out/native.stdout"

# These private canonical-table calls use valid Type objects, but execute only natively.
canonical_input="artifacts/canonical-type-comparison-input-$CONFIG"
mkdir -p "$canonical_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$canonical_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --canonical-type-comparison "$canonical_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
canonical_out="artifacts/canonical-type-comparison-$CONFIG"
invoke_cli "$canonical_input/SelfHostPrimSubset.dll" --auto-ref -o "$canonical_out"
compile_console "$canonical_out" CanonicalTypeComparison
gate_capture_ok "$canonical_out/native" run_bounded "./$canonical_out/CanonicalTypeComparison$EXE_EXT"
canonical_type_boundary_asserts "$canonical_out/native.stdout"

# Both copy helpers retain their real IL. Span reflection is a validation check;
# only these true-ABI direct/calli sites execute its byref-like body.
copy_input="artifacts/runtime-type-handle-copy-input-$CONFIG"
mkdir -p "$copy_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$copy_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-type-handle-copy "$copy_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
copy_out="artifacts/runtime-type-handle-copy-$CONFIG"
invoke_cli "$copy_input/SelfHostPrimSubset.dll" --auto-ref -o "$copy_out"
compile_console "$copy_out" RuntimeTypeHandleCopy
gate_capture_ok "$copy_out/native" run_bounded "./$copy_out/RuntimeTypeHandleCopy$EXE_EXT"
gate_capture_ok "$copy_out/oracle" run_bounded dotnet "$_CG_APP" runtime-type-handle-copy-artifact
gate_output compare "$copy_out/native.stdout" "$copy_out/oracle.stdout" --policy crlf
for route in reflection direct pointer; do
    handle_copy_asserts "$copy_out/native.stdout" "$route"
done
gate_output byte-witness "$copy_out/native.stdout" 'Runtime type handle copy artifact end' --count 1 --policy crlf

# A compile-only gap in each retained helper must be inventoried even when no
# output callback exists. This CoreLib artifact is measured, never executed.
copy_measure_core="$copy_input/measure-corelib/System.Private.CoreLib.dll"
mkdir -p "$(dirname "$copy_measure_core")"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-type-handle-copy-measure "$copy_measure_core" "$_CG_CORELIB"
invoke_cli "$copy_input/SelfHostPrimSubset.dll" --auto-ref --no-ildiet -r "$copy_measure_core" \
    --measure -o "$copy_out/measure" > "$copy_out/measure.stdout"
copy_gaps="$copy_out/measure/s0-gaps.tsv"
if [[ ! -f "$copy_gaps" ]] || [[ $(grep -Fc 'CopyMeasureProbe' "$copy_gaps") -ne 2 ]] \
    || grep -q '^dangling' "$copy_gaps"; then
    echo 'FAIL: --measure did not inventory both managed type-handle copy bodies' >&2
    cat "$copy_out/measure.stdout" >&2
    exit 1
fi

# Both instance accessors use native Type identity; the checked default keeps CoreLib validation.
accessor_input="artifacts/runtime-type-handle-accessor-input-$CONFIG"
mkdir -p "$accessor_input"
cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$accessor_input/"
dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
    --runtime-type-handle-accessor "$accessor_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
accessor_out="artifacts/runtime-type-handle-accessor-$CONFIG"
invoke_cli "$accessor_input/SelfHostPrimSubset.dll" --auto-ref -o "$accessor_out"
compile_console "$accessor_out" RuntimeTypeHandleAccessor
gate_capture_ok "$accessor_out/native" run_bounded "./$accessor_out/RuntimeTypeHandleAccessor$EXE_EXT"
gate_capture_ok "$accessor_out/oracle" run_bounded dotnet "$_CG_APP" runtime-type-handle-accessor-artifact
gate_output compare "$accessor_out/native.stdout" "$accessor_out/oracle.stdout" --policy crlf
for route in reflection direct pointer; do
    handle_accessor_asserts "$accessor_out/native.stdout" "$route"
done
gate_output byte-witness "$accessor_out/native.stdout" 'Runtime type handle accessor artifact end' --count 1 --policy crlf

# Neither rewritten entry point executes on CLR; only the public contract uses its oracle.
for constraint_mode in all reflection; do
    constraint_input="artifacts/constraint-check-input-$constraint_mode-$CONFIG"
    mkdir -p "$constraint_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$constraint_input/"
    constraint_flag=--constraint-check
    constraint_routes='direct pointer reflection'
    if [[ "$constraint_mode" == reflection ]]; then
        constraint_flag=--constraint-check-reflection
        constraint_routes=reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$constraint_flag" "$constraint_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    constraint_out="artifacts/constraint-check-$constraint_mode-$CONFIG"
    invoke_cli "$constraint_input/SelfHostPrimSubset.dll" --auto-ref -o "$constraint_out"
    compile_console "$constraint_out" ConstraintCheck
    gate_capture_ok "$constraint_out/native" run_bounded "./$constraint_out/ConstraintCheck$EXE_EXT"
    constraint_check_boundary_asserts "$constraint_out/native.stdout" "$constraint_routes"
done

# The CLR oracle uses ordinary reflection; imported private call sites run only natively.
for constructor_mode in all reflection; do
    constructor_input="artifacts/runtime-type-handle-constructor-input-$constructor_mode-$CONFIG"
    mkdir -p "$constructor_input"
    cp samples/dotnet/SelfHostPrimSubset/bin/$CONFIG/$TFM/* "$constructor_input/"
    constructor_flag=--runtime-type-handle-constructor
    constructor_routes='reflection newobj direct pointer'
    constructor_oracle=runtime-type-handle-constructor-artifact
    if [[ "$constructor_mode" == reflection ]]; then
        constructor_flag=--runtime-type-handle-constructor-reflection
        constructor_routes=reflection
        constructor_oracle=runtime-type-handle-constructor-reflection
    fi
    dotnet "gates/fixtures/ldftn-local/bin/$CONFIG/$TFM/LdftnLocalFixture.dll" \
        "$constructor_flag" "$constructor_input/SelfHostPrimSubset.dll" "$_CG_CORELIB"
    constructor_out="artifacts/runtime-type-handle-constructor-$constructor_mode-$CONFIG"
    invoke_cli "$constructor_input/SelfHostPrimSubset.dll" --auto-ref -o "$constructor_out"
    compile_console "$constructor_out" RuntimeTypeHandleConstructor
    gate_capture_ok "$constructor_out/native" run_bounded "./$constructor_out/RuntimeTypeHandleConstructor$EXE_EXT"
    gate_capture_ok "$constructor_out/oracle" run_bounded dotnet "$_CG_APP" "$constructor_oracle"
    gate_output compare "$constructor_out/native.stdout" "$constructor_out/oracle.stdout" --policy crlf
    handle_constructor_asserts "$constructor_out/native.stdout" "$constructor_routes"
    if [[ "$constructor_mode" == all ]]; then
        gate_output byte-witness "$constructor_out/native.stdout" 'Runtime type handle constructor artifact end' --count 1 --policy crlf
    fi
done

formal_declaring_routes_gate "artifacts/formal-declaring-method-$CONFIG"

runtime_type_szarray_routes_gate "artifacts/runtime-type-szarray-$CONFIG"
