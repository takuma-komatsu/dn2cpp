// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot
{
    /// <summary>
    /// The shim Variant's marshalling kind tags (<see cref="Variant"/>._kind) —
    /// the single C# source for every producer/consumer: the Variant conversions
    /// here in the shim, and the transpiler's Godot backend/intrinsics (which
    /// compile this file directly). The C++ runtime mirrors the values as
    /// <c>Dn2CppGodotVariantKind</c> in <c>dn2cpp_godot.h</c> under the existing
    /// layout contract (the codec reads/writes the transpiled struct's fields
    /// directly), so the two sides must stay in lockstep — never renumber.
    /// </summary>
    internal static class VariantKind
    {
        public const int Nil = 0;
        public const int Bool = 1;
        public const int Int = 2;
        public const int Float = 3;
        public const int String = 4;
        public const int Vector3 = 5;
        public const int Vector2 = 6;
        public const int Vector4 = 7;
        public const int Color = 8;
        // Engine object handle in the int payload.
        public const int Object = 9;
        // Raw 8-byte resource id in the int payload.
        public const int Rid = 10;
        // Text in the string payload — the shim's string-family C# type is
        // `string`, so only the tag keeps the engine identity.
        public const int StringName = 11;
        public const int NodePath = 12;
        // The big builtin PODs: a boxed shim struct in the object payload.
        public const int Rect2 = 13;
        public const int Plane = 14;
        public const int Aabb = 15;
        public const int Quaternion = 16;
        public const int Transform2D = 17;
        public const int Basis = 18;
        public const int Transform3D = 19;
        public const int Projection = 20;
        // Packed arrays: the managed array in the object payload. The runtime's
        // DN2CPP_GD_PACKED_* tag is the kind minus PackedTagOffset.
        public const int PackedByteArray = 21;
        public const int PackedInt32Array = 22;
        public const int PackedInt64Array = 23;
        public const int PackedFloat32Array = 24;
        public const int PackedFloat64Array = 25;
        public const int PackedStringArray = 26;
        public const int PackedVector2Array = 27;
        public const int PackedVector3Array = 28;
        public const int PackedVector4Array = 29;
        public const int PackedColorArray = 30;
        public const int PackedTagOffset = 4;
        // The engine-backed container wrappers in the object payload.
        public const int GodotArray = 31;
        public const int GodotDictionary = 32;
        // Boxed Callable/Signal shim structs in the object payload.
        public const int Callable = 33;
        public const int Signal = 34;
        // Not a payload kind of its own: the registration slot for the
        // Godot.Object shim the runtime uses to wrap a decoded standard
        // Callable's target / Signal's owner.
        public const int ObjectShimSlot = 35;
    }
}
