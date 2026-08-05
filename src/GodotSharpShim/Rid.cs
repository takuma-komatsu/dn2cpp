// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>Hand-written shim for the engine's opaque resource-id type (the
    /// API dump's <c>RID</c>; named <c>Rid</c> to match GodotSharp). A single
    /// 8-byte id whose layout equals the native ptrcall encoding, so engine calls
    /// pass/return it like any other identity-layout POD value type. The dump
    /// carries no members for it, so the binding generator emits nothing and the
    /// whole type lives here.</summary>
    public partial struct Rid
    {
        private ulong _id;

        internal Rid(ulong id)
        {
            _id = id;
        }

        /// <summary>The raw engine id (0 for an invalid/empty RID).</summary>
        public ulong Id => _id;

        /// <summary>True when this RID references something (a non-zero id).
        /// Mirrors GodotSharp's <c>Rid.IsValid</c>.</summary>
        public bool IsValid => _id != 0;

        public static bool operator ==(Rid left, Rid right) => left._id == right._id;
        public static bool operator !=(Rid left, Rid right) => left._id != right._id;
        public override bool Equals(object? obj) => obj is Rid other && _id == other._id;
        public override int GetHashCode() => _id.GetHashCode();
        public override string ToString() => string.Concat("RID(", _id.ToString(), ")");
    }
}
