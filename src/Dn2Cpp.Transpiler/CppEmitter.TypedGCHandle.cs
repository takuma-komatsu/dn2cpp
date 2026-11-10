namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private sealed partial class TypeMetadataEmitter
    {
        private (string Interfaces, int Count) TypedGCHandleInterfaces(ClassInfo owner)
        {
            var entries = new List<string>();
            foreach (var itf in owner.Interfaces.OrderBy(t => t.CppName, StringComparer.Ordinal))
            {
                var declaration = _c.TypedGCHandleInterfaceMethod(owner, itf);
                string stem = $"typed_gchandle_itf_{owner.CppName}_{itf.CppName}";
                if (declaration.Name == "Dispose")
                    _sb.AppendLine($"static void {stem}({itf.CppStructName}* self) {{ auto* value = (Dn2CppGCHandle*)((Dn2CppObject*)self + 1); if (dn2cpp_gchandle_is_allocated(*value)) dn2cpp_gchandle_free(value); }}");
                else
                    _sb.AppendLine($"static int32_t {stem}({itf.CppStructName}* self, Dn2CppGCHandle other) {{ return ((Dn2CppGCHandle*)((Dn2CppObject*)self + 1))->cell == other.cell ? 1 : 0; }}");
                var slots = Enumerable.Repeat("nullptr", declaration.VtableSlot + 1).ToArray();
                slots[declaration.VtableSlot] = $"(const void*)&{stem}";
                _sb.AppendLine($"static const void* {stem}_slots[] = {{ {string.Join(", ", slots)} }};");
                entries.Add($"{{ {_e.TypeInfoRef(itf, "typed GCHandle interface")}, {stem}_slots }}");
            }
            if (entries.Count == 0)
                throw new InvalidOperationException($"{owner.FullName}: typed GCHandle interfaces were not prepared");
            string interfaces = $"typed_gchandle_itfs_{owner.CppName}";
            _sb.AppendLine($"static const Dn2CppInterfaceEntry {interfaces}[] = {{ {string.Join(", ", entries)} }};");
            return (interfaces, entries.Count);
        }
    }
}
