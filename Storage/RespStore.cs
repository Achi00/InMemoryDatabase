using InMemoryDatabase.Parser.Enums;
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
            while (true)
            {
                if (!_data.TryGetValue(key, out StoredEntry current) || current.IsExpired)
                {
                    // key does not exists or is expired, no race conditions!!
                    return false;
                }

                var updated = new StoredEntry(current.Value, expiresAt);

                if (_data.TryUpdate(key, updated, current))
                {
                    // if current was still current at the moment of the swap
                    return true;
                }
            }
        }

        public long GetTtlSeconds(string key)
        {
            if (!_data.TryGetValue(key, out StoredEntry entry) || entry.IsExpired)
            {
                // does not exists or expired
                return -2;
            }

            if (entry.ExpiresAt is null)
            {
                // exists, no expiry set
                return -1;
            }

            double remaining = (entry.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalSeconds;

            return Math.Max(0, (long)remaining);
        }

        public bool TryIncrement(string key, int delta, out long newValue, out string? error)
        {
            while (true)
            {
                bool existed = _data.TryGetValue(key, out StoredEntry current);

                if (!existed || current.IsExpired)
                {
                    // missing or expired key behaves as if it was 0
                    current = new StoredEntry(RespValue.BulkString("0"), null);
                }

                // check if value type is bulk string in first place or can be parsed as long
                if (current.Value.Type != RespValueType.BulkString || !long.TryParse(current.Value.TypeString, out long currentNum))
                {
                    newValue = 0;
                    error = "ERR value is not an integer or is out of range";
                    return false;
                }

                try
                {
                    // throws oberflow ex instead of wrapping, should avoid unwanted/unpredictable integet value
                    long candidate = checked(currentNum + delta);

                    // update already parsed long back to string again
                    var updated = new StoredEntry(RespValue.BulkString(candidate.ToString()), current.ExpiresAt);

                    bool swapped = existed 
                        ? _data.TryUpdate(key, updated, current) 
                        // key did not exist, add nes instead of update
                        : _data.TryAdd(key, updated);

                    if (swapped)
                    {
                        newValue = candidate;
                        error = null;
                        return true;
                    }

                    // else waca happended, someone changed it between our read and here
                }
                // if candidate overflowed from its type
                catch (OverflowException)
                {
                    newValue = 0;
                    error = "ERR value is not an integer or is out of range";
                    return false;
                }
            }
        }

        internal bool TryDecrement(string key, int delta, out long newValue, out string? error)
        {
            
        }
    }
}
