

namespace InMemoryDatabase
{
    internal sealed class VolatileKeySet
    {
        private readonly List<string> _keys = new();
        // key = position in _keys
        private readonly Dictionary<string, int> _index = new();
        private readonly object _lock = new object();
        private readonly Random _random;
        public VolatileKeySet(Random? random = null)
        {
            _random = random ?? Random.Shared;
        }

        // expose key counts for internal RespStore
        public int Count
        {
            get { lock (_lock) return _keys.Count; }
        }

        public void Add(string key)
        {
            lock (_lock)
            {
                if (_index.ContainsKey(key))
                {
                    return;
                }
                // if key does not exist store key and count in 2 storages
                // store as count, in list it only stores last element, will get O(1) to get it with index from list
                _index[key] = _keys.Count;
                _keys.Add(key);
            }
        }

        public void Remove(string key)
        {
            lock (_lock)
            {
                // returns position of element in _keys array from dictionary
                if (!_index.Remove(key, out int position))
                {
                    return;
                } 

                int last = _keys.Count - 1;

                // if element to remove is not last swap places with last element, then remove
                if (position != last)
                {
                    // move the last key into currently removed key's space, so removal will not shift the list onlt last element will be removed
                    string moved = _keys[last];
                    _keys[position] = moved;
                    _index[moved] = position;
                }
                // remove last element which was switched instead of last one
                _keys.RemoveAt(last);
            }
        }

        public int Sameple(Span<string> destination)
        {
            lock (_lock)
            {
                if (_keys.Count == 0)
                {
                    return 0;
                }

                int count = Math.Min(destination.Length, _keys.Count);
                for (int i = 0; i < count; i++)
                {
                    // get random key for sampling, should be in range of count!!
                    destination[i] = _keys[_random.Next(_keys.Count)];
                }

                return count;
            }
        }
    }
}
