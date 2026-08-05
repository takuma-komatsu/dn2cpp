namespace MiniBcl
{
    /// <summary>
    /// Enumerator for <see cref="List{T}"/>. C# foreach binds to GetEnumerator
    /// structurally (MoveNext/Current), so no IEnumerable/IDisposable needed.
    /// </summary>
    public sealed class ListEnumerator<T>
    {
        private readonly T[] _items;
        private readonly int _count;
        private int _index;

        public ListEnumerator(T[] items, int count)
        {
            _items = items;
            _count = count;
            _index = -1;
        }

        public bool MoveNext()
        {
            _index = _index + 1;
            return _index < _count;
        }

        public T Current
        {
            get { return _items[_index]; }
        }
    }

    /// <summary>
    /// A minimal generic growable list, in its own assembly. The transpiler
    /// pulls it into the app via cross-assembly reference resolution +
    /// monomorphization, exactly as it would a real BCL collection.
    /// </summary>
    public sealed class List<T>
    {
        private T[] _items;
        private int _count;

        public List()
        {
            _items = new T[4];
            _count = 0;
        }

        public int Count
        {
            get { return _count; }
        }

        public void Add(T item)
        {
            if (_count == _items.Length)
            {
                T[] bigger = new T[_items.Length * 2];
                int i = 0;
                while (i < _count)
                {
                    bigger[i] = _items[i];
                    i = i + 1;
                }
                _items = bigger;
            }
            _items[_count] = item;
            _count = _count + 1;
        }

        public T this[int index]
        {
            get { return _items[index]; }
            set { _items[index] = value; }
        }

        public ListEnumerator<T> GetEnumerator()
        {
            return new ListEnumerator<T>(_items, _count);
        }
    }
}
