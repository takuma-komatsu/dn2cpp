// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    // Vector2 is generated from extension_api.json (fields/ctors/operators/
    // constants); its hand-written methods live in Vector2.cs.

    public partial class Node2D
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Delegate)]
    public class SignalAttribute : System.Attribute
    {
    }

    [System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
    public class ExportAttribute : System.Attribute
    {
    }

    // Callable / Signal live in Callable.cs (first-class engine-call value
    // types with a delegate-backed custom-callable form).

    // The engine lifecycle/input virtuals (_Ready, _Process, _EnterTree,
    // _ExitTree, _PhysicsProcess, _Input, ...) are no longer hand-written here:
    // every `is_virtual` engine method is generated as a `public virtual`
    // placeholder in GodotShims.g.cs and bridged generically through the
    // node-class virtual table. Only _Notification stays hand-written — it is
    // not a ptrcall virtual (the engine delivers it through a separate
    // GDExtension notification callback).

    public partial class Object
    {
        // Engine notification callback (GodotObject._Notification in real
        // GodotSharp). Bridged via the GDExtension notification_func slot, not the
        // virtual-call path. Empty default; user classes override.
        public virtual void _Notification(int what)
        {
            _ = what;
        }

        // Connect is no longer hand-written: with Callable admitted as an
        // engine-call argument type, the generated shim surfaces the real
        // engine `connect(StringName, Callable, int) -> Error` and the call
        // rides the general ptrcall path like any other method.

        public void EmitSignal(string signal, params object[] args)
        {
            _ = signal;
            _ = args;
        }

        // Plant / read the borrowed engine handle. Used by the Variant↔Object/Node
        // conversions: a `(Node)variant` cast allocates a fresh shim of the
        // static target type and plants the handle the Variant carries (kind 9), so
        // engine methods can be called on it; the reverse builds a Variant from a
        // live shim's handle. The `__` prefix marks these as transpiler-internal
        // helpers with real bodies (the GodotBackend emits them rather than treating
        // them as inline-replaced engine API).
        public void __SetHandle(System.IntPtr h)
        {
            _handle = h;
        }

        public System.IntPtr __GetHandle()
        {
            return _handle;
        }
    }
}
