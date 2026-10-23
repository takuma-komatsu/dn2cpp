using System.Reflection.Metadata;

namespace Dn2Cpp;

internal sealed partial class Compilation
{
    private readonly Dictionary<ClassInfo, uint> _attributeUsageFlags = new();

    // Zero is AttributeUsage's default: inherited, with one instance per type.
    internal uint AttributeUsageFlags(ClassInfo attribute)
    {
        if (_attributeUsageFlags.TryGetValue(attribute, out uint flags))
            return flags;
        // CLR reads usage from this definition, not from its attribute base class.
        if (!attribute.Handle.IsNil)
        {
            var reader = attribute.Module.Reader;
            foreach (var handle in reader.GetTypeDefinition(attribute.Handle).GetCustomAttributes())
            {
                var row = reader.GetCustomAttribute(handle);
                if (AttributeTypeName(reader, row) != "System.AttributeUsageAttribute")
                    continue;
                var usage = row.DecodeValue(AttrProvider);
                foreach (var argument in usage.NamedArguments)
                {
                    if (argument.Name == "Inherited" && argument.Value is false)
                        flags |= 1;
                    if (argument.Name == "AllowMultiple" && argument.Value is true)
                        flags |= 2;
                }
                _attributeUsageFlags.Add(attribute, flags);
                return flags;
            }
        }
        _attributeUsageFlags.Add(attribute, flags);
        return flags;
    }

    private MethodInfo? AttributeBaseMethod(MethodInfo method)
    {
        if (!method.IsVirtual || method.IsNewSlot || method.DeclaringClass.IsInterface)
            return null;
        string signature = ReflectionOverrideSignature(method);
        for (var level = EnsureShape(method.DeclaringClass).BaseClass; level is not null;
            level = EnsureShape(level).BaseClass)
        {
            EnsureCompleted(level);
            foreach (var candidate in LookupMethods(level))
                if (candidate.IsVirtual && candidate.Name == method.Name
                    && (method.VtableSlot >= 0 && candidate.VtableSlot >= 0
                        ? candidate.VtableSlot == method.VtableSlot
                        : ReflectionOverrideSignature(candidate) == signature))
                    return candidate;
        }
        return null;
    }
}
