namespace InMemoryDatabase
{
    internal sealed class VolatileKeySet
    {
        private readonly List<string> _keys = new();
        // key = position in _keys
        private readonly Dictionary<string, int> _index = new();
        private readonly object _lock = new object();

        public void Add(string key)
        {
            lock (_lock)
            {
                if (_index.ContainsKey(key))
                {
                    return;
                }
                // if key does not exist store key and count in 2 storages
                _index[key] = _keys.Count;
                _keys.Add(key);
            }
        }

        public void Remove(string key)
        {
            lock (_lock)
            {
                // returns position of key in _keys array
                if (!_index.Remove(key, out int position))
                {
                    return;
                }

                int last = _keys.Count - 1;

                if (position != last)
                {
                    // move the last key into emply space, so removal will not shift the list
                    string moved = _keys[last];
                    _keys[position] = moved;
                    _index[moved] = position;
                }
                // remove last element
                _keys.RemoveAt(last);
            }
        }
    }
}
