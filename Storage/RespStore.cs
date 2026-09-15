using InMemoryDatabase.Parser.Models;
using System.Collections.Concurrent;

namespace InMemoryDatabase.Storage
{
    public class RespStore
    {
        private readonly ConcurrentDictionary<string, RespValue> _data = new();

        public void Set(string key, RespValue value)
        {
            _data[key] = value; 
        }

        public bool TryGet(string key, out RespValue value)
        {
            return _data.TryGetValue(key, out value);
        }

        public bool Delete(string key)
        {
            return _data.TryRemove(key, out _);
        }
    }
}
