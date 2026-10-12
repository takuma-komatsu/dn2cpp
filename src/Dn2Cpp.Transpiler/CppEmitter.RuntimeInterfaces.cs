using System.Text;

namespace Dn2Cpp;

internal sealed partial class CppEmitter
{
    private (string Sym, int Count)? _runtimeInterfaceMaps;

    private void EmitRuntimeInterfaceMaps(StringBuilder sb)
    {
        int sequence = 0;
        var maps = new List<string>();
        foreach (var group in _c.RuntimeInterfaceBindings
            .Where(b => _emit.Contains(b.Interface))
            .GroupBy(b => TypeInfoRef(b.Receiver, "runtime interface receiver"))
            .OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var entries = new List<string>();
            var aliases = new HashSet<ClassInfo>();
            var existing = _intrinsicItfMaps.FirstOrDefault(m => "&" + m.TypeInfoSym == group.Key);
            if (existing.Sym is not null)
                for (int i = 0; i < existing.Count; i++)
                    entries.Add($"{existing.Sym}[{i}]");
            foreach (var interfaceGroup in group.GroupBy(b => b.Interface))
            {
                var itf = interfaceGroup.Key;
                int count = itf.Methods.Count;
                var slots = Enumerable.Repeat("(const void*)&dn2cpp_itf_slot_missing_anon", count).ToArray();
                foreach (var binding in interfaceGroup)
                {
                    var declaration = binding.Declaration;
                    var target = binding.Target.Emittable;
                    if (!_c.Reachable.Contains(binding.Target) || declaration.VtableSlot < 0)
                        continue;
                    string thunk = $"runtime_itf_thunk_{sequence++}";
                    string ret = declaration.Signature.ReturnType.IsVoid ? "void" : CppTypes.Of(declaration.Signature.ReturnType);
                    var parameters = new List<string> { $"{itf.CppStructName}* self" };
                    var args = new List<string>();
                    string receiverType = MethodCompiler.ReceiverCppType(target.DeclaringClass);
                    string receiver = target.DeclaringClass.IsValueType
                        ? "((Dn2CppObject*)self + 1)"
                        : MethodCompiler.IsHeaderlessWrapCpp(receiverType)
                            ? MethodCompiler.HeaderlessUnwrapExpr("self", receiverType) : "self";
                    args.Add($"({receiverType})({receiver})");
                    for (int i = 0; i < declaration.Signature.ParameterTypes.Length; i++)
                    {
                        parameters.Add($"{CppTypes.Of(declaration.Signature.ParameterTypes[i])} a{i}");
                        args.Add(NfiSlotArg(target.Signature.ParameterTypes[i], $"a{i}"));
                    }
                    string call = $"{target.CppName}({string.Join(", ", args)})";
                    string body = declaration.Signature.ReturnType.IsVoid ? call + ";" : "return " + call + ";";
                    sb.AppendLine($"static {ret} {thunk}({string.Join(", ", parameters)}) {{ {body} }}");
                    slots[declaration.VtableSlot] = $"(const void*)&{thunk}";
                }
                string table = $"runtime_itf_slots_{sequence++}";
                sb.AppendLine($"static const void* {table}[] = {{ {string.Join(", ", slots)} }};");
                string methods = EmitInterfaceMethodTargets(sb, $"runtime_itf_methods_{sequence++}", count,
                    interfaceGroup.Select(b => (b.Declaration, b.Target)).ToList());
                entries.Add($"{{ {TypeInfoRef(itf, "runtime interface map")}, {table}, {methods} }}");
                aliases.Add(itf);
                if (_c.SharedGenericsEnabled && _c.CanonicalInterfaceOf(itf) is { } canonical
                    && aliases.Add(canonical))
                    entries.Add($"{{ {TypeInfoRef(canonical, "runtime interface canonical alias")}, {table}, {methods} }}");
                NoteRuntimeMapInterface(group.Key, itf);
            }
            string map = $"runtime_itfs_{sequence++}";
            sb.AppendLine($"static const Dn2CppInterfaceEntry {map}[] = {{ {string.Join(", ", entries)} }};");
            maps.Add($"{{ {group.Key}, {map}, {entries.Count} }}");
        }
        if (maps.Count == 0)
            return;
        sb.AppendLine($"static const Dn2CppRelationRows runtime_itf_maps[] = {{ {string.Join(", ", maps)} }};");
        _runtimeInterfaceMaps = ("runtime_itf_maps", maps.Count);
    }
}
