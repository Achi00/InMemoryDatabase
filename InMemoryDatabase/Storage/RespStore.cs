using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Resp.Enums;
using InMemoryDatabase.Resp.Models;
using System.Collections.Concurrent;

namespace InMemoryDatabase.Storage
{
    public class RespStore
    {
        private readonly ConcurrentDictionary<string, StoredEntry> _data = new();

        // dictionary value StoredEntry = RespValue + DateTime metadata
        public bool Set(string key, RespValue value, DateTimeOffset? expiresAt, SetCondition condition)
        {
            var entry = new StoredEntry(value, expiresAt);

            // if condition state does not contains any additional params about ttl or expiry
            if (condition == SetCondition.Always)
            {
                _data[key] = entry;
                return true;
            }

            while (true)
            {
                bool present = _data.TryGetValue(key, out var current);
                // still chack if current value is expires or not
                bool exists = present && !current.IsExpired;
                // check counter intuitive states on key
                if (condition == SetCondition.IfNotExists && exists)
                {
                    return false;
                }
                if (condition == SetCondition.IfExists && !exists)
                {
                    return false;
                }

                bool written;
                if (present)
                {
                    // key exists in dictionary, life for xx or expider for nx
                    written = _data.TryUpdate(key, entry, current);
                }
                else
                {
                    // key does not exists, insert only if still non existant
                    written = _data.TryAdd(key, entry);
                }

                if (written)
                {
                    return true;
                }
            }
        }

        // checks if expired, lazy eviction strategy, only remove when convenient, reduce cpu overhead
        // expired key value pair is removed when accessed
        // TODO: add worker later to clean old expired data, if those not accessed they will sit in memory forever
        internal bool TryGet(string key, out RespValue value)
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

        internal bool Delete(string key)
        {
            return _data.TryRemove(key, out _);
        }

        internal bool SetExpiry(string key, DateTimeOffset expiresAt)
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

        internal long GetTtlSeconds(string key)
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

        internal bool TryIncrement(string key, long delta, out long newValue, out string? error)
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
                    error = "ERR value is not an integer or out of range";
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
                        // key did not exist, add new instead
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
                    error = "ERR value is not an integer or out of range";
                    return false;
                }
            }
        }
    }
}
