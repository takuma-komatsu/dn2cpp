// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    // The delegate shapes a delegate-backed Callable supports. Declared here
    // (not System.Action / Action<T>) because the Godot lane transpiles
    // against the shim assembly alone — an external BCL generic like
    // Action<Variant> cannot be instantiated. Lambdas convert to these
    // implicitly, so GodotSharp-style `Callable.From(() => ...)` call sites
    // compile unchanged.
    public delegate void CallableAction();
    public delegate void CallableVariantAction(Variant arg);
    public delegate void CallableVariantArgsAction(Variant[] args);

    /// <summary>
    /// Stand-in for the official <c>Godot.Callable</c> value type: a
    /// field-carrying struct (bitwise-copy-safe — no owned engine storage)
    /// converted to/from a real 16-byte engine Callable only at the runtime
    /// boundary. Two forms share the struct:
    /// <list type="bullet">
    /// <item><b>Standard</b> (<see cref="Target"/> + <see cref="Method"/>): built
    /// at the boundary via the engine's <c>Callable(Object, StringName)</c>
    /// constructor — dispatch stays entirely engine-side.</item>
    /// <item><b>Delegate-backed</b> (the <see cref="From(CallableAction)"/>
    /// overloads): built via <c>callable_custom_create2</c> with a pinned copy
    /// of this struct as userdata; the runtime trampoline decodes the incoming
    /// engine Variant args and re-enters managed code through
    /// <see cref="__Dispatch"/>. Only the zero-arg, single-<see cref="Variant"/>
    /// and <c>Variant[]</c> delegate shapes are supported (typed-argument
    /// delegates like GodotSharp's <c>Callable.From&lt;T&gt;</c> are not).</item>
    /// </list>
    /// The field order is a layout contract with the runtime's
    /// <c>Dn2CppGodotCallableShim</c> POD (all pointer-sized fields) and must not
    /// change.
    /// </summary>
    public struct Callable
    {
        public Object Target;
        public string Method;
        // Delegate-backed form: at most one of the three is non-null — every `From`
        // overload sets exactly one and the ctors set none, and a fourth delegate
        // shape must maintain that. Typed fields, one per supported shape (rather
        // than one System.Delegate + downcasts), so __Dispatch needs no type checks.
        internal CallableAction? _action;
        internal CallableVariantAction? _actionVariant;
        internal CallableVariantArgsAction? _actionVariants;

        public Callable(Object target, string method)
        {
            Target = target;
            Method = method;
            _action = null;
            _actionVariant = null;
            _actionVariants = null;
        }

        public static Callable From(CallableAction action)
        {
            var c = default(Callable);
            c._action = action;
            return c;
        }

        public static Callable From(CallableVariantAction action)
        {
            var c = default(Callable);
            c._actionVariant = action;
            return c;
        }

        public static Callable From(CallableVariantArgsAction action)
        {
            var c = default(Callable);
            c._actionVariants = action;
            return c;
        }

        /// <summary>Invokes the callable with the given arguments and returns the
        /// engine's result Variant (NIL for a delegate-backed callable — the
        /// supported delegate shapes return void). Placeholder body: the call is
        /// intrinsic-lowered to the runtime bridge, which builds the engine
        /// Callable and routes through the engine's own <c>call</c>.</summary>
        public Variant Call(params Variant[] args)
        {
            _ = args;
            return default;
        }

        /// <summary>Re-entry point for the runtime's custom-callable trampoline:
        /// the engine invoked a delegate-backed callable; <paramref name="c"/> is
        /// the pinned struct copy the callable carries, <paramref name="args"/>
        /// the decoded call arguments. Real body — referenced only from the
        /// generated dispatch bridge, so the backend seeds it as a root.</summary>
        internal static void __Dispatch(Callable c, Variant[] args)
        {
            if (c._action is not null)
            {
                c._action();
                return;
            }
            if (c._actionVariant is not null)
            {
                c._actionVariant(args is { Length: > 0 } ? args[0] : default);
                return;
            }
            if (c._actionVariants is not null)
            {
                c._actionVariants(args);
            }
        }
    }

    /// <summary>
    /// Stand-in for the official <c>Godot.Signal</c> value type: the
    /// field-carrying (owner, name) pair, converted to/from the 16-byte engine
    /// Signal at the runtime boundary via its <c>Signal(Object, StringName)</c>
    /// constructor / <c>get_object</c>+<c>get_name</c> builtins. The field order
    /// is a layout contract with the runtime's <c>Dn2CppGodotSignalShim</c> POD.
    /// </summary>
    public struct Signal
    {
        public Object Owner;
        public string Name;

        public Signal(Object owner, string name)
        {
            Owner = owner;
            Name = name;
        }
    }
}
