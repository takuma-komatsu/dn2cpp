namespace Dn2Cpp;

internal sealed partial class Compilation
{
    internal sealed record RuntimeInterfaceBinding(ClassInfo Receiver, ClassInfo Interface,
        MethodInfo Declaration, MethodInfo Target);

    internal readonly List<RuntimeInterfaceBinding> RuntimeInterfaceBindings = [];
    internal readonly Dictionary<MethodInfo, string> RuntimeInterfaceLowerings = new();
    internal readonly HashSet<ClassInfo> RuntimeInterfaceMetadata = [];
    private readonly HashSet<(ClassInfo, MethodInfo)> _runtimeInterfaceBindings = new();

    private static bool RuntimeDispatchInterface(ClassInfo itf) => itf.FullName is
        "System.Reflection.ICustomAttributeProvider" or "System.Reflection.IReflect"
        or "System.Reflection.IReflectableType" or "System.ICloneable"
        or "System.Runtime.Serialization.ISerializable"
        or "System.Runtime.Serialization.IDeserializationCallback"
        or "System.IConvertible" or "System.IAsyncResult"
        || itf.Namespace == "System.Numerics";

    internal void PrepareRuntimeInterfaces()
    {
        var declarations = _usedVirtualDecls.ToList();
        var runtimeBoxes = SelectedRuntimeHandleBoxes()
            .Where(t => t.Kind == TypeKind.Class && t.Class is not null)
            .Select(t => t.Class!).ToHashSet();
        foreach (var itf in ReferencedTypes.ToList())
        {
            if (!itf.IsInterface)
                continue;
            if (itf.FullName == "System.IConvertible")
                RuntimeInterfaceMetadata.Add(itf.EnsureMembers());
            // Late-bound native interface rows consume providers without a managed
            // call site. Metadata enumeration alone must not create this edge.
            if ((ReflectionInvokeUsed || NeedsReflectionDelegateBind) && itf.FullName is "System.IConvertible"
                    or "System.IFormattable" or "System.ISpanFormattable" or "System.IUtf8SpanFormattable")
                foreach (var method in itf.EnsureMembers().Methods)
                    if (RuntimeNumericProviderConsumer(method))
                        ReachFormatProviderGetFormat();
        }
        foreach (var receiver in Classes.ToList())
        {
            bool runtimeReceiver = CoreIntrinsics.RuntimeTypeInfoSymbol(receiver) is not null
                || CoreIntrinsics.IsIntrinsicType(receiver.FullName);
            if (receiver.IsInterface || receiver.IsEnum || receiver.FullName == "System.String" || ContainsGenericVar(receiver)
                || !runtimeReceiver && (!_allocatedRefTypes.Contains(receiver) || !InheritsFromException(receiver)))
                continue;
            // Native structs need a map only when a box can carry their payload.
            // Primitive box tokens use Primitive TypeDescs rather than allocation rows.
            if (runtimeReceiver && EnsureShape(receiver).IsValueType
                && WellKnownPrimitive(receiver.FullName) is null
                && !_allocatedRefTypes.Contains(receiver) && !runtimeBoxes.Contains(receiver))
                continue;
            foreach (var declaration in declarations)
            {
                var itf = declaration.DeclaringClass;
                if (!itf.IsInterface || itf.FullName == "System.IConvertible"
                    || declaration.IsStatic || !RuntimeDispatchInterface(itf)
                    || declaration.Context.MethodArgs.Length != 0
                    || !ImplementsInterface(receiver, itf))
                    continue;
                if (!_runtimeInterfaceBindings.Add((receiver, declaration)))
                    continue;
                var target = ResolveItfImplOrNull(receiver, declaration)
                    ?? (runtimeReceiver ? RuntimeFacadeInterfaceTarget(receiver, declaration) : null);
                if (target is null)
                    continue;
                target.EnsureSignature();
                RuntimeInterfaceMetadata.Add(itf);
                if (runtimeReceiver)
                    RuntimeInterfaceBindings.Add(new(receiver, itf, declaration, target));
                string? dispatch = RuntimeInterfaceDispatch(target);
                if (dispatch is not null)
                {
                    RuntimeInterfaceLowerings[target] = dispatch;
                    NoteIntrinsicFtnTarget(target);
                }
                else
                {
                    ReachIntrinsicTypeMethod(target);
                }
            }
        }
    }

    // Reflection handles use abstract CLR facades as their concrete runtime type.
    // Their public abstract slots still have the same native lowering as direct calls.
    private static MethodInfo? RuntimeFacadeInterfaceTarget(ClassInfo receiver, MethodInfo declaration)
    {
        for (var level = receiver; level is not null; level = level.BaseClass)
        {
            if (!CoreIntrinsics.IsIntrinsicType(level.FullName)
                || level.EnsureMembers().MethodsNamed(declaration.Name) is not { } methods)
                continue;
            foreach (var method in methods)
                if (!method.IsStatic && method.IsPublic && method.SigKey == declaration.SigKey)
                    return method;
        }
        return null;
    }

    private static string? RuntimeInterfaceDispatch(MethodInfo target)
    {
        string owner = target.DeclaringClass.FullName;
        if (owner == "System.RuntimeType" || owner == "System.Reflection.TypeInfo")
            return "System.Type";
        if (owner == "System.Reflection.RuntimeAssembly")
            return "System.Reflection.Assembly";
        if (owner == "System.Reflection.RuntimeModule")
            return "System.Reflection.Module";
        return CoreIntrinsics.IsIntrinsicType(owner) ? owner : null;
    }

    internal void ReachRuntimeInterfaceBody(MethodInfo target)
    {
        RuntimeInterfaceLowerings.Remove(target);
        ReachIntrinsicTypeMethod(target);
        DrainReachability();
    }
}
