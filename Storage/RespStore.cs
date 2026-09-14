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

        public void TryGet(string key, out RespValue value)
        {
            _data.TryGetValue(key, out value);
        }
    }
}
