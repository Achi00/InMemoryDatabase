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
                // if key does not exist store key and cound in 2 storages
                _index[key] = _keys.Count;
                _keys.Add(key);
            }
        }
    }
}
