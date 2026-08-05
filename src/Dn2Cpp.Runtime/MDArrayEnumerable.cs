// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace Dn2Cpp.Runtime
{
    // Wraps a multi-dimensional (rank >= 2) array as the six non-generic interfaces a
    // CLR MD array implements — IEnumerable/ICollection/IList/ICloneable/
    // IStructuralComparable/IStructuralEquatable — so an interface dispatch on an MD
    // array resolves through a real managed dispatch table. dn2cpp MD arrays carry a
    // runtime-interned type-info (dn2cpp_mdarr_ti) with no interface rows, so
    // dn2cpp_resolve_interface cannot answer on them; the generated init prologue
    // installs ONE shared dispatch table for all rank>=2 arrays
    // (dn2cpp_array_set_md_fallback_interfaces) whose thunks wrap the receiver into
    // this class.
    //
    // Non-generic over a System.Array receiver on purpose: every member an MD array
    // answers is either element-agnostic (enumeration, Count/Clear/Clone go through
    // the Array reflection surface, whose runtime helpers dispatch on the receiver's
    // type-info) or an unconditional throw, so one class serves every element type
    // and rank. The transpiler resolves the type by full name
    // (Dn2Cpp.Runtime.MDArrayEnumerable); the name is the contract.
    //
    // Exception types AND messages mirror real .NET on a rank>=2 receiver. Clear()
    // and Clone() are the only mutating members real .NET serves there.
    //
    // GetEnumerator hands back a fresh cursor instance of this same class (array
    // semantics — independent enumerations never share a position), walking a flat
    // index in rightmost-fastest order, which is both the MD data block's element
    // order (dn2cpp_md_flat_index) and real .NET's enumeration order. Like real
    // .NET's ArrayEnumerator it does NOT implement IDisposable (a foreach's
    // `as IDisposable` probe answers null).
    public sealed class MDArrayEnumerable : IList, IEnumerator, ICloneable,
        IStructuralComparable, IStructuralEquatable
    {
        private readonly Array _array;
        private readonly int _total;
        private int _index;

        public MDArrayEnumerable(Array array)
        {
            _array = array;
            _total = array.Length;
            _index = -1;
        }

        // ---- IEnumerable / IEnumerator ----

        public IEnumerator GetEnumerator()
        {
            return new MDArrayEnumerable(_array);
        }

        public bool MoveNext()
        {
            if (_index < _total)
                _index++;
            return _index < _total;
        }

        public object? Current
        {
            get
            {
                if (_index < 0)
                    throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
                if (_index >= _total)
                    throw new InvalidOperationException("Enumeration already finished.");
                // The flat cursor divmod-decomposed rightmost-fastest into per-
                // dimension indices (dn2cpp zeroes lower bounds, so 0-based
                // indices address every element). A zero-length dimension makes
                // _total 0, so the guards above keep the divisions unreachable.
                int rank = _array.Rank;
                int[] indices = new int[rank];
                int rem = _index;
                for (int d = rank - 1; d >= 0; d--)
                {
                    int len = _array.GetLength(d);
                    indices[d] = rem % len;
                    rem /= len;
                }
                return _array.GetValue(indices);
            }
        }

        public void Reset()
        {
            _index = -1;
        }

        // ---- ICollection ----

        public int Count
        {
            get { return _total; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        // Real .NET: an array's SyncRoot is the array itself (reference identity
        // with the receiver, not with this per-dispatch wrapper).
        public object SyncRoot
        {
            get { return _array; }
        }

        public void CopyTo(Array array, int index)
        {
            // Real Array.CopyTo's own guard order: a rank>=2 destination is
            // refused by CopyTo itself, a null one by Array.Copy underneath, and
            // a rank-1 destination reaches Copy's rank-match test — which always
            // fails against this rank>=2 receiver.
            if (array is not null && array.Rank != 1)
                throw new ArgumentException("Only single dimension arrays are supported here.", nameof(array));
            if (array is null)
                throw new ArgumentNullException("destinationArray");
            throw new RankException("The specified arrays must have the same number of dimensions.");
        }

        // ---- IList ----

        public bool IsFixedSize
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        // Real .NET routes the IList indexer through GetValue(int)/SetValue(int),
        // whose one-dimensional precondition a rank>=2 receiver always fails.
        public object? this[int index]
        {
            get { throw new ArgumentException("Array was not a one-dimensional array."); }
            set { throw new ArgumentException("Array was not a one-dimensional array."); }
        }

        public int Add(object? value)
        {
            throw new NotSupportedException("Collection was of a fixed size.");
        }

        public void Insert(int index, object? value)
        {
            throw new NotSupportedException("Collection was of a fixed size.");
        }

        public void Remove(object? value)
        {
            throw new NotSupportedException("Collection was of a fixed size.");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("Collection was of a fixed size.");
        }

        // Real .NET routes these through Array.IndexOf(Array, object), whose MD
        // refusal is Rank-flavored — a different family from the indexer's.
        public bool Contains(object? value)
        {
            throw new RankException("Only single dimension arrays are supported here.");
        }

        public int IndexOf(object? value)
        {
            throw new RankException("Only single dimension arrays are supported here.");
        }

        // Real .NET: IList.Clear on an MD array SUCCEEDS and zeroes every element
        // in place (the one non-throwing mutator). Array.Clear's runtime helper
        // dispatches on the receiver's type-info, so the MD data block is zeroed
        // byte-wise whatever the element type.
        public void Clear()
        {
            Array.Clear(_array, 0, _total);
        }

        // ---- ICloneable ----

        // A shallow copy: same type/rank/lengths, independent storage. The runtime
        // clone helper dispatches on the receiver's type-info (MD arm included).
        public object Clone()
        {
            return _array.Clone();
        }

        // ---- IStructuralComparable / IStructuralEquatable ----

        // Real .NET's element walks call GetValue(int), whose one-dimensional
        // precondition a rank>=2 receiver always fails — but the guards ahead of
        // the walk answer without throwing, so each member mirrors the real guard
        // order and throws exactly where the walk's first GetValue would. A
        // zero-element receiver never enters the walk; its structural hash is
        // HashCode-seeded (per-process randomized) in real .NET, so 0 here is a
        // deliberate simplification of a case nobody can assert against.

        public int CompareTo(object? other, IComparer comparer)
        {
            if (other is null)
                return 1;
            Array? oa = other as Array;
            if (oa is null || _total != oa.Length)
                throw new ArgumentException(
                    "The object is not an array with the same number of elements as the array to compare it to.",
                    "other");
            if (_total != 0)
                throw new ArgumentException("Array was not a one-dimensional array.");
            return 0;
        }

        public bool Equals(object? other, IEqualityComparer comparer)
        {
            if (other is null)
                return false;
            if (object.ReferenceEquals(_array, other))
                return true;
            Array? oa = other as Array;
            if (oa is null || oa.Length != _total)
                return false;
            if (_total != 0)
                throw new ArgumentException("Array was not a one-dimensional array.");
            return true;
        }

        public int GetHashCode(IEqualityComparer comparer)
        {
            if (comparer is null)
                throw new ArgumentNullException("comparer");
            if (_total != 0)
                throw new ArgumentException("Array was not a one-dimensional array.");
            return 0;
        }
    }
}
