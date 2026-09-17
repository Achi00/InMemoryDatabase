using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Resp.Models;
using System.Collections.Concurrent;

namespace InMemoryDatabase.Storage
{
    public class RespStore
    {
        private readonly ConcurrentDictionary<string, StoredEntry> _data = new();

        // dictionary value StoredEntry = RespValue + DateTime metadata
        public void Set(string key, RespValue value, DateTimeOffset? expiresAt = null)
        {
            _data[key] = new StoredEntry(value, expiresAt);
        }

        // checks if expired, lazy eviction strategy, only remove when convenient, reduce cpu overhead
        // expired key value pair is removed when accessed
        // TODO: add worker later to clean old expired data, if those not accessed they will sit in memory forever
        public bool TryGet(string key, out RespValue value)
        {
            if (_data.TryGetValue(key, out StoredEntry entry))
            {
                if (entry.IsExpired)
                {
                    _data.TryRemove(key, out _);
                    value = default;
                    return false;
                }

                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool Delete(string key)
        {
            return _data.TryRemove(key, out _);
        }

        public bool SetExpiry(string key, DateTimeOffset expiresAt)
        {
            if (!_data.TryGetValue(key, out StoredEntry entry))
            {
                return false;
            }

            _data[key] = new StoredEntry(entry.Value, expiresAt);
            return true;
        }
    }
}
