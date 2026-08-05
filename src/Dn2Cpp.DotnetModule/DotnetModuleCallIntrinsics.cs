using System.Reflection.Metadata;

namespace Dn2Cpp.DotnetModule;

/// <summary>Call lowerings for the real-GodotSharp surface. One entry today:
/// <c>Godot.CustomGCHandle</c>, GodotSharp's ALC-reload-aware GC-handle wrappers.
/// ALC reloading is permanently off on the embedded game path, so each wrapper
/// reduces to the plain GCHandle operation it wraps and the call site lowers
/// straight to the Dn2CppGCHandle runtime model.
///
/// <para>The wrapper *bodies* (the dead ALC bookkeeping and the AssemblyLoadContext /
/// ConditionalWeakTable machinery they drag in) are cut at reachability by the
/// matching rows in <see cref="DotnetModuleBackend.AdditionalBoundedMethods"/>. That
/// composes because call intrinsics get first refusal ahead of the bounded call-site
/// neutralization: the body is cut, yet the call still lowers to something real
/// instead of a default value. Both sets are projected from
/// <see cref="s_customGCHandleMethods"/> so they cannot drift.</para></summary>
internal sealed class DotnetModuleCallIntrinsics : ICallIntrinsics
{
    /// <summary>The wrapper methods. <see cref="DotnetModuleBackend"/> projects the same
    /// array into its bounded set.</summary>
    internal static readonly string[] s_customGCHandleMethods = { "AllocStrong", "AllocWeak", "Free" };

    internal const string CustomGCHandleType = "Godot.CustomGCHandle";

    public bool TryEmitCall(MethodCompiler mc, MethodInfo callee, bool isCallvirt)
    {
        // Dispatcher.SynchronizationContext: the real static getter reads
        // the cut GodotTaskScheduler's field, so its body is bounded (see the
        // backend's s_boundedMethods row); the call site pushes the DM lane's
        // native singleton instead (created + installed on the engine main
        // thread by the generated epilogue's dn2cpp_dm_sync_ctx).
        if (callee.DeclaringClass.FullName == "Godot.Dispatcher"
            && callee.Name == "get_SynchronizationContext"
            && callee.IsStatic)
        {
            string t = CppTypes.Of(callee.Signature.ReturnType);
            mc.Push(StackKind.Ref, t, $"(({t})dn2cpp_dm_sync_ctx())");
            return true;
        }
        if (callee.DeclaringClass.FullName != CustomGCHandleType)
            return false;
        if (Array.IndexOf(s_customGCHandleMethods, callee.Name) < 0)
            return false;

        EmitCustomGCHandleForward(mc, callee.Name, callee.Signature);
        return true;
    }

    /// <summary>Nothing on this lane is constructed target-side — CustomGCHandle is a
    /// static class, and the real GodotSharp types allocate through the normal managed
    /// path.</summary>
    public bool TryEmitNewobj(MethodCompiler mc, MethodInfo ctor) => false;

    /// <summary>Each wrapper lowered to the plain GCHandle operation it wraps:
    /// AllocStrong -> a Normal handle (the 2-arg overload's Type key only feeds the dead
    /// ALC-reload registry, so it is dropped), AllocWeak -> a Weak handle, Free -> clear
    /// the shared cell (the by-value GCHandle parameter has no caller storage to zero —
    /// same as GCHandle.Free on an rvalue). Reuses MethodCompiler's EmitGCHandleAlloc /
    /// GCHVal rather than restating them, so this forward cannot drift from the core's own
    /// GCHandle intrinsic.
    ///
    /// <para>An unmodeled shape must THROW, not return false: the declaring type is in the
    /// bounded set, so falling through would reach the bounded call-site neutralization and
    /// silently push a zeroed handle. A GCHandle that quietly refers to nothing is exactly
    /// the failure this lane cannot afford.</para></summary>
    private static void EmitCustomGCHandleForward(
        MethodCompiler mc, string name, MethodSignature<TypeDesc> sig)
    {
        var ps = sig.ParameterTypes;
        switch (name)
        {
            case "AllocStrong" when ps.Length is 1 or 2:
            {
                if (ps.Length == 2)
                    mc.Pop(); // the ALC-lookup Type key — unused
                var value = mc.Pop();
                mc.Push(StackKind.Struct, "Dn2CppGCHandle", MethodCompiler.EmitGCHandleAlloc(value, "2"));
                return;
            }
            case "AllocWeak" when ps.Length == 1:
            {
                var value = mc.Pop();
                mc.Push(StackKind.Struct, "Dn2CppGCHandle", MethodCompiler.EmitGCHandleAlloc(value, "0"));
                return;
            }
            case "Free" when ps.Length == 1:
            {
                var h = mc.Pop();
                mc.Emit($"dn2cpp_gchandle_free_value({mc.GCHVal(h)});");
                return;
            }
        }
        throw new NotSupportedException(
            $"{CustomGCHandleType}::{name} has no GCHandle lowering "
            + "(supported: AllocStrong/AllocWeak/Free)");
    }
}
