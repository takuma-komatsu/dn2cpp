// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Dn2Cpp.Runtime
{
    // Wraps a T[] as a managed IEnumerable<T> so a raw array used where
    // IEnumerable<T> is expected (LINQ-over-array, foreach over (IEnumerable<T>)arr)
    // enumerates through a real managed interface dispatch table.
    //
    // dn2cpp arrays are intrinsic structs: their runtime type-info is one of two
    // shared shapes (array_i4 / array_ref) with the element type tracked *statically*,
    // so an array carries no managed interface map and `dn2cpp_resolve_interface`
    // fails on it (EntryPointNotFoundException). The transpiler therefore boxes the
    // array into this per-T wrapper at the T[] -> collection-interface boundary
    // (MethodCompiler argument/store coercion); being an ordinary C# generic class,
    // the existing monomorphization + interface-map machinery produces the dispatch
    // with no hand-written C++ enumerator. Mirrors the CLR's SZGenericArrayEnumerator.
    //
    // It lives in the Dn2Cpp.Runtime support assembly the CLI auto-references on every
    // transpile, so this works in any program. The transpiler resolves the type by its
    // full name (Dn2Cpp.Runtime.SZArrayEnumerable`1); the name is the contract.
    //
    // Implements the full interface set a T[] satisfies in the CLR, generic and
    // non-generic. Size-changing members throw NotSupportedException (fixed-size array
    // behaviour); the indexer setter writes through. The non-generic IList members
    // box/unbox T and follow the array's CLR quirks: IList.IsReadOnly is false (an
    // array's elements are writable) while ICollection<T>.IsReadOnly is true.
    public sealed class SZArrayEnumerable<T> : IList<T>, IReadOnlyList<T>, IList, IEnumerator<T>
    {
        private readonly T[] _array;
        private int _index;

        public SZArrayEnumerable(T[] array)
        {
            _array = array;
            _index = -1;
        }

        public int Count
        {
            get { return _array.Length; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        // The GENERIC indexer — IList<T> and IReadOnlyList<T> (one implicit member serves
        // both; .NET throws the same thing through either). Range-checked HERE rather than
        // left to the backing array: a direct `arr[bad]` raises IndexOutOfRangeException,
        // while the same element through IList<T> raises ArgumentOutOfRangeException, and
        // the two are not catch-compatible.
        //
        // The check belongs in this body, not in the emitted interface thunk: the thunk
        // only wraps and forwards (one check per slot), and the compile-time wrap at the
        // `T[] -> collection-interface` boundary never goes through a thunk at all.
        //
        // ArgumentException.get_Message appends the "(Parameter 'index')" suffix, so the
        // literal must NOT carry it.
        //
        // The NON-generic `object? IList.this[int]` below is deliberately NOT range-checked:
        // real .NET raises IndexOutOfRangeException on that route.
        public T this[int index]
        {
            get
            {
                // The (uint) cast folds the negative case into one comparison, the BCL idiom.
                if ((uint)index >= (uint)_array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), IndexRangeMessage);
                return _array[index];
            }
            set
            {
                if ((uint)index >= (uint)_array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index), IndexRangeMessage);
                _array[index] = value;
            }
        }

        // Real .NET's SR.ArgumentOutOfRange_IndexMustBeLess, verbatim and without the
        // "(Parameter 'index')" suffix that ArgumentException.get_Message adds.
        private const string IndexRangeMessage =
            "Index was out of range. Must be non-negative and less than the size of the collection.";

        public int IndexOf(T item)
        {
            EqualityComparer<T> cmp = EqualityComparer<T>.Default;
            for (int i = 0; i < _array.Length; i++)
                if (cmp.Equals(_array[i], item))
                    return i;
            return -1;
        }

        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _array.Length; i++)
                array[arrayIndex + i] = _array[i];
        }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        // Each GetEnumerator hands back a fresh cursor so independent / nested
        // enumerations of the same array don't share a position (array semantics).
        public IEnumerator<T> GetEnumerator()
        {
            return new SZArrayEnumerable<T>(_array);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SZArrayEnumerable<T>(_array);
        }

        public bool MoveNext()
        {
            int next = _index + 1;
            if (next < _array.Length)
            {
                _index = next;
                return true;
            }
            return false;
        }

        public T Current
        {
            get { return _array[_index]; }
        }

        object? IEnumerator.Current
        {
            get { return _array[_index]; }
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }

        // ---- non-generic IList / ICollection ----
        // A T[] implements these too; the object-typed members box/unbox T. ICollection.Count
        // is satisfied by the public Count above (same signature). The array CLR quirk: an
        // array is fixed-size but its elements are writable, so IList.IsReadOnly is false
        // even though the generic ICollection<T>.IsReadOnly (public IsReadOnly above) is true.
        bool IList.IsFixedSize
        {
            get { return true; }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return this; }
        }

        // Unchecked ON PURPOSE — see the generic indexer above. Real .NET raises
        // IndexOutOfRangeException through the non-generic IList route, which is what
        // letting the backing array fault produces.
        object? IList.this[int index]
        {
            get { return _array[index]; }
            set { _array[index] = (T)value!; }
        }

        int IList.Add(object? value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object? value)
        {
            return ((IList)this).IndexOf(value) >= 0;
        }

        int IList.IndexOf(object? value)
        {
            // A correctly-typed value forwards to the generic IndexOf (EqualityComparer<T>);
            // a null searches for a null reference element; anything else is not present.
            if (value is null)
            {
                for (int i = 0; i < _array.Length; i++)
                    if (_array[i] is null)
                        return i;
                return -1;
            }
            if (value is T t)
                return IndexOf(t);
            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object? value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            for (int i = 0; i < _array.Length; i++)
                array.SetValue(_array[i], index + i);
        }
    }
}
