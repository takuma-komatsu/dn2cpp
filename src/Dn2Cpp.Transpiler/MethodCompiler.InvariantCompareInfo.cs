namespace Dn2Cpp;

internal sealed partial class MethodCompiler
{
    private string CompileInvariantCompareInfoInitializer()
    {
        var cls = _method.DeclaringClass;
        if (!CoreIntrinsics.IsInvariantCompareInfoInitializer(_method)
            || !CoreIntrinsics.TryGetInvariantCompareInfoInitializerFields(cls, out var name, out var sortName, out var asciiOrdinal))
            throw new NotSupportedException("Invariant CompareInfo initialization requires its exact declaration and fields");
        NoteFieldOwnerLayout(cls);
        var receiver = new StackEntry("a0", StackKind.Ref, CppTypes.Of(TypeDesc.MakeClass(cls)));
        const string culture = "((const Dn2CppNumberFormatInfo*)a1)";
        Emit($"dn2cpp_compare_info_require_native_culture({culture});");
        if (_method.Name == ".ctor")
        {
            string legacyName = NewTemp("Dn2CppString*");
            Emit($"{legacyName} = dn2cpp_culture_tostring({culture});");
            Emit($"dn2cpp_gc_store_ref(&{FieldAccess(cls, name, receiver)}, {legacyName});");
        }
        // SortName is evaluated before the receiver's field store. Invariant ICU
        // initialization sets only the ASCII flag; an existing sort handle survives.
        string identity = NewTemp("Dn2CppString*");
        Emit($"{identity} = dn2cpp_culture_sort_name({culture});");
        Emit($"dn2cpp_gc_store_ref(&{FieldAccess(cls, sortName, receiver)}, {identity});");
        Emit($"{FieldAccess(cls, asciiOrdinal, receiver)} = true;");
        return RenderWrapperBody("invariant CompareInfo initialization");
    }
}
