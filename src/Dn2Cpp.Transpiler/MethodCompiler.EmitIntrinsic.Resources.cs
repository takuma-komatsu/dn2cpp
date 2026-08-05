using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    /// <summary>System.Resources.ResourceManager — the lookup half, served from
    /// the assembly's already-carried <c>&lt;BaseName&gt;.resources</c> blob.
    ///
    /// <para>The type is intrinsic (see <c>CoreIntrinsics.s_intrinsicTypes</c>), so an
    /// unmapped member here does NOT fall through to a transpiled body — there is none —
    /// it reaches the loud intrinsic miss. Every member this arm declines is one whose real
    /// implementation is the groveler/satellite closure the intrinsic mapping deleted, and
    /// the alternative to a loud miss is a call to a C++ symbol nothing defines.</para>
    ///
    /// <para>The ctors are intercepted at <c>newobj</c>
    /// (<see cref="MethodCompiler"/>'s Newobj arm), not here.</para></summary>
    private bool TryEmitResourcesIntrinsic(string declType, string name, MethodSignature<TypeDesc> sig)
    {
        if (declType != "System.Resources.ResourceManager")
            return false;
        // The emitter only carries a blob when some emitted body asked for one, so noting
        // the use here is what makes a ResourceManager program carry its own resources;
        // without it the registry rows stay resource-less and every lookup misses.
        switch (name)
        {
            case "GetString":
            case "GetObject":
            {
                // (string name) and (string name, CultureInfo culture). The culture is
                // passed through to the runtime rather than tested here: which cultures
                // can be served is a run-time question (the argument is a value, not a
                // token), and the refusal has to name the culture that actually arrived.
                if (sig.ParameterTypes is not ([{ IsString: true }]
                        or [{ IsString: true }, { Kind: TypeKind.Class, Class.FullName: "System.Globalization.CultureInfo" }]))
                    return false;
                Comp.NoteManifestResourceUse();
                string culture = "nullptr";
                if (sig.ParameterTypes.Length == 2)
                    culture = Cast(Pop(), "const Dn2CppNumberFormatInfo*");
                string key = Cast(Pop(), "Dn2CppString*");
                string rm = Cast(Pop(), "Dn2CppResourceManager*");
                if (name == "GetString")
                {
                    Push(StackKind.Ref, "Dn2CppString*",
                        $"dn2cpp_resourcemanager_get_string({rm}, {key}, {culture})");
                }
                else
                {
                    // GetObject can answer a byte[] (a ByteArray entry), and only the
                    // emitter can name a precise array type-info — so the Byte[] handle
                    // is noted and passed in. Noted unconditionally rather than
                    // per-entry: which ResourceTypeCode a key carries is a run-time fact.
                    var byteElem = TypeDesc.MakePrimitive(PrimitiveTypeCode.Byte);
                    Comp.NoteArrayElementType(byteElem);
                    Push(StackKind.Ref, "Dn2CppObject*",
                        $"dn2cpp_resourcemanager_get_object({rm}, {key}, {culture}, "
                        + $"{PreciseArrayTypeInfoExpr(byteElem)})");
                }
                return true;
            }
            case "get_BaseName":
                Push(StackKind.Ref, "Dn2CppString*",
                    $"dn2cpp_resourcemanager_base_name({Cast(Pop(), "Dn2CppResourceManager*")})");
                return true;
            // ReleaseAllResources: the sets are .rodata spans, so there is nothing to
            // release and nothing to re-grovel. .NET's contract is "drop the cached sets",
            // which a model that caches nothing has already met.
            case "ReleaseAllResources":
                Pop(); // the ResourceManager receiver
                return true;
            // GetStream returns an UnmanagedMemoryStream over the mapped image, whose body
            // dn2cpp cannot transpile (the SafeBuffer/Volatile subtree). The signature
            // NAMES UnmanagedMemoryStream, so a MemoryStream cannot stand in for it: loud
            // and catchable, naming the route that does work.
            case "GetStream":
            {
                for (int i = 0; i <= sig.ParameterTypes.Length; i++)
                    Pop(); // args + instance receiver
                Emit("dn2cpp_throw_platform_not_supported(\"ResourceManager.GetStream returns "
                    + "an UnmanagedMemoryStream, whose body is not transpilable in AOT-compiled "
                    + "code. Read the resource through "
                    + "Assembly.GetManifestResourceStream(<BaseName>.resources), which hands "
                    + "back a read-only MemoryStream over the same bytes.\");");
                // Unreachable; stack typing only. The base object pointer avoids naming
                // the return type's opaque shell, which is never emitted for a body that
                // only trips this cut (the Assembly.GetManifestResource* arm's reason).
                Push(StackKind.Ref, "Dn2CppObject*", "nullptr");
                return true;
            }
        }
        // A `call` to ResourceManager::.ctor rather than a `newobj` is a DERIVED class
        // chaining to its base. An intrinsic type has no C++ base to embed, so the subclass
        // cannot exist, and the intrinsic miss would point at a BCL member rather than at
        // the derived class that is the thing to change. Named here instead. (`newobj` on
        // ResourceManager itself never reaches this arm — it is intercepted one layer up,
        // in the Newobj arm.)
        if (name == ".ctor")
        {
            throw new NotSupportedException(
                $"{Method.DeclaringClass.FullName}.{Method.Name}: deriving from " +
                "System.Resources.ResourceManager is not supported. dn2cpp models it as " +
                "an intrinsic (baseName, assembly) pair reading the assembly's carried " +
                ".resources blob — there is no base object for a subclass to extend, and " +
                "the virtuals it would override (GetResourceSet, InternalGetResourceSet) " +
                "hand back the ResourceSet this model does not build. Use a plain " +
                "ResourceManager and wrap it, rather than deriving from it");
        }
        // GetResourceSet / CreateFileBasedResourceManager / get_ResourceSetType and the
        // protected grovel hooks each hand back or consume a ResourceSet, the object this
        // model does not build (a lookup reads .rodata). Declining sends them to the loud
        // intrinsic miss, which names the member.
        return false;
    }
}
