namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private void EmitInvariantCompareInfoFactories(CppOutput o)
    {
        foreach (var cls in _c.InvariantCompareInfoTypes)
        {
            if (!CoreIntrinsics.TryGetInvariantCompareInfoFields(cls, out var name, out var sortName)
                || !_emit.Contains(cls))
                throw new InvalidOperationException("Invariant CompareInfo requires its exact emitted string fields");
            string type = cls.CppStructName;
            string factory = CoreIntrinsics.InvariantCompareInfoFactoryName(cls);
            o.Header.AppendLine($"{type}* {factory}();");
            o.Data.AppendLine($"{type}* {factory}()");
            o.Data.AppendLine("{");
            o.Data.AppendLine("    static std::mutex& mutex = dn2cpp_never_destroyed<std::mutex>();");
            o.Data.AppendLine($"    static {type}** root = nullptr;");
            o.Data.AppendLine("    std::lock_guard<std::mutex> lock(mutex);");
            o.Data.AppendLine("    if (root != nullptr && *root != nullptr) return *root;");
            o.Data.AppendLine($"    if (root == nullptr) root = static_cast<{type}**>(dn2cpp_alloc_pinned(sizeof({type}*)));");
            o.Data.AppendLine("    Dn2CppString* empty = dn2cpp_string_literal(u\"\", 0);");
            o.Data.AppendLine($"    auto* value = static_cast<{type}*>(dn2cpp_alloc(sizeof({type})));");
            o.Data.AppendLine($"    value->type = {TypeInfoRef(cls, "invariant CompareInfo factory")};");
            o.Data.AppendLine($"    dn2cpp_gc_store_ref(&value->{name.CppName}, empty);");
            o.Data.AppendLine($"    dn2cpp_gc_store_ref(&value->{sortName.CppName}, empty);");
            o.Data.AppendLine("    dn2cpp_gc_store_ref(root, value);");
            o.Data.AppendLine("    return value;");
            o.Data.AppendLine("}");
            o.Data.AppendLine();
        }
    }
}
