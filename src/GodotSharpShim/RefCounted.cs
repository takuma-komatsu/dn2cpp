// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    public partial class RefCounted
    {
        // Whether this is a ClassDB-registered extension instance constructed by C#
        // `new`. TryEmitNewobj sets this to 1 only when it bound this shim to the
        // engine side via object_set_instance (bare RefCounted / return-value wraps
        // are never bound, so it stays 0). The finalizer uses it to decide whether
        // the binding needs to be detached.
        private bool _engineBound;

        // Thin bridge to the GDExtension object_destroy interface function. The __
        // prefix follows the existing convention for "helper with a body that the
        // transpiler intercepts" (see Node.cs).
        public void __Destroy()
        {
        }

        // Thin bridge that detaches the engine→shim extension-instance binding
        // (object_set_instance). Called only when a collectible shim is reclaimed
        // while the engine still holds an independent reference, so the surviving
        // engine object never keeps a dangling pointer to a collected shim. Never
        // called on the destruction path (there the binding is left in place so
        // free_instance_func reclaims the shim).
        public void __DetachBinding()
        {
        }

        // Thin bridge that anchors this shim in a GC-scanned strong-reference table,
        // keeping it reachable across a collection: a registered instance the engine
        // still independently references survives with its C# state and overrides
        // intact instead of degrading to the engine's override-less default. Returns
        // false when the table is full, and the caller then falls through to the
        // ordinary detach path.
        public bool __TryEngineAnchor()
        {
            return false;
        }

        ~RefCounted()
        {
            // Only a registered instance has C# behavior worth preserving
            // (object_set_instance dispatch reaching a C# override); an unbound one
            // skips the check and its engine call entirely.
            if (_engineBound && GetReferenceCount() > 1 && __TryEngineAnchor())
            {
                // Something besides this shim's own baseline hold still references
                // the engine object, so keep the C# state alive instead of
                // detaching: re-register for finalization (Boehm implicitly
                // unregisters once an object is queued) and let the anchor just
                // taken keep this instance reachable. A later sweep drops the anchor
                // once the engine's count falls back to the baseline, and the next
                // collection finalizes this object down the branch below.
                System.GC.ReRegisterForFinalize(this);
                return;
            }
            // Unreference() works as-is through the generic engine-call path. With a
            // null (unconstructed) handle it is a no-op returning false (the null
            // guard in dn2cpp_godot_call_ptrcall).
            if (Unreference())
            {
                // The reference count reached 0 — nobody holds this engine object
                // anymore, so destroy it. Do NOT detach here: nulling the extension
                // instance binding before destruction makes the engine's predelete
                // path dereference a null instance and crash. Destroying with the
                // binding in place lets the engine's free_instance_func reclaim this
                // collectible shim.
                __Destroy();
            }
            else if (_engineBound)
            {
                // References remain: the shim is being collected but the engine
                // object lives on, so detach the binding only — the surviving object
                // must never keep a dangling pointer to a collected shim.
                __DetachBinding();
            }
        }
    }
}
