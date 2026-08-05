namespace MiniBcl
{
    /// <summary>A minimal generic LIFO stack over a backing array.</summary>
    public sealed class Stack<T>
    {
        private T[] _items;
        private int _count;

        public Stack()
        {
            _items = new T[4];
            _count = 0;
        }

        public int Count
        {
            get { return _count; }
        }

        public void Push(T item)
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

        public T Pop()
        {
            _count = _count - 1;
            return _items[_count];
        }
    }
}
