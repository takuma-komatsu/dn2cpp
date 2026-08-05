// Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md).
// Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.
// Licensed under the MIT License.

namespace Godot.Collections
{
    /// <summary>
    /// Engine-backed wrapper for Godot's <c>Array</c>: a reference type holding a
    /// runtime-allocated engine Array value (an 8-byte reference-counted handle)
    /// in its leading <c>_handle</c> field, with GodotSharp's identity semantics —
    /// copying the C# reference shares the engine container, and handing one to the
    /// engine keeps it shared, so later mutations are visible on both sides. Every
    /// member body below is a placeholder lowered by the transpiler's call
    /// intrinsics to runtime bridge calls (array_operator_index + the engine's own
    /// builtin methods); the engine value is destroyed by the finalizer once the
    /// wrapper is unreachable (the RefCounted finalizer pattern).
    /// </summary>
    public partial class Array
    {
        // The engine Array value: a pointer to a pinned runtime-allocated slot
        // holding the 8-byte engine container. Must stay the FIRST field (the
        // intrinsics and the runtime codec read it right after the object header,
        // like the engine-object shims' handle).
        private System.IntPtr _handle;

        public Array()
        {
        }

        public int Count => 0;

        public Variant this[int index]
        {
            get { _ = index; return default; }
            set { _ = index; _ = value; }
        }

        public void Add(Variant item)
        {
            _ = item;
        }

        public void Clear()
        {
        }

        // Thin bridge that destroys the wrapper's engine container value (ptr
        // destructor + slot free). Bodyless — the intrinsic emits the runtime
        // call — following the RefCounted.__Destroy convention.
        internal void __Destroy()
        {
        }

        ~Array()
        {
            __Destroy();
        }

        /// <summary>Duck-typed enumeration so <c>foreach</c> compiles without
        /// the interface machinery: a struct enumerator over Count + the
        /// indexer, whose accesses lower to the same bridge intrinsics.</summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator
        {
            private readonly Array _array;
            private int _index;

            internal Enumerator(Array array)
            {
                _array = array;
                _index = -1;
            }

            public Variant Current => _array[_index];

            public bool MoveNext()
            {
                _index = _index + 1;
                return _index < _array.Count;
            }
        }
    }

    /// <summary>
    /// The GodotSharp-style typed array surface over the same engine value:
    /// <c>typedarray::T</c> engine signatures map here. Derives from
    /// <see cref="Array"/> (sharing the handle field and the untyped surface), so
    /// a typed array passes anywhere an engine <c>Array</c> is expected. The
    /// typed element accessors are intercepted per concrete <typeparamref name="T"/>
    /// (the transpiler monomorphizes them): elements convert through the runtime
    /// Variant codec — engine-object elements wrap/unwrap by handle, scalars and
    /// strings by payload, container elements as nested wrappers.
    /// </summary>
    public sealed class Array<T> : Array
    {
        public Array()
        {
        }

        public new T this[int index]
        {
            get { _ = index; return default!; }
            set { _ = index; _ = value; }
        }

        public void Add(T item)
        {
            _ = item;
        }

        /// <summary>The typed counterpart of the base duck-typed enumerator:
        /// <c>foreach (T item in array)</c> reads through the typed indexer.</summary>
        public new Enumerator GetEnumerator() => new Enumerator(this);

        public new struct Enumerator
        {
            private readonly Array<T> _array;
            private int _index;

            internal Enumerator(Array<T> array)
            {
                _array = array;
                _index = -1;
            }

            public T Current => _array[_index];

            public bool MoveNext()
            {
                _index = _index + 1;
                return _index < _array.Count;
            }
        }
    }

    /// <summary>
    /// Engine-backed wrapper for Godot's <c>Dictionary</c>: a reference type
    /// holding the engine Dictionary value in its leading <c>_handle</c>
    /// field with GodotSharp identity semantics, exactly like
    /// <see cref="Array"/>. Element access goes through the GDExtension
    /// <c>dictionary_operator_index</c>; <see cref="ContainsKey"/> and
    /// <see cref="Keys"/> ride the engine's own <c>has</c>/<c>keys</c> builtin
    /// methods. All bodies are intrinsic-lowered placeholders.
    /// </summary>
    public partial class Dictionary
    {
        // The engine Dictionary value slot; must stay the FIRST field (see Array).
        private System.IntPtr _handle;

        public Dictionary()
        {
        }

        public int Count => 0;

        public Variant this[Variant key]
        {
            get { _ = key; return default; }
            set { _ = key; _ = value; }
        }

        public void Add(Variant key, Variant value)
        {
            _ = key;
            _ = value;
        }

        public bool ContainsKey(Variant key)
        {
            _ = key;
            return false;
        }

        /// <summary>The dictionary's keys as a fresh engine Array (the engine's
        /// <c>keys()</c> builtin), wrapped like any Array return.</summary>
        public Array Keys => null!;

        /// <summary>The dictionary's values as a fresh engine Array (the
        /// engine's <c>values()</c> builtin), wrapped like any Array return.</summary>
        public Array Values => null!;

        public void Clear()
        {
        }

        internal void __Destroy()
        {
        }

        ~Dictionary()
        {
            __Destroy();
        }
    }
}
