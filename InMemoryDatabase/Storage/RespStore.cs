using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Resp.Enums;
using InMemoryDatabase.Resp.Models;
using System.Collections.Concurrent;

namespace InMemoryDatabase.Storage
{
    public class RespStore
    {
        private readonly ConcurrentDictionary<string, StoredEntry> _data = new();
        // combines list and dictionary O(1) lookup with key + index
        private readonly VolatileKeySet _volatileKeySet = new();
        private readonly string[] _sampleBuffer = new string[20];

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

            // loops in case of race confition and value change, if updated first and then lookup with prev value, now it will look up again and get updated value
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

                bool written = present
                    // key exists in dictionary, life for xx or expider for nx
                    ? _data.TryUpdate(key, entry, current)
                    // key does not exists, insert only if still non existant
                    : _data.TryAdd(key, entry);

                if (written)
                {
                    if (expiresAt is not null)
                    {
                        _volatileKeySet.Add(key);
                    }
                    else
                    {
                        _volatileKeySet.Remove(key);
                    }
                    return true;
                }
            }
        }

        // checks if expired, lazy eviction strategy, only remove when convenient, reduce cpu overhead
        // expired key value pair is removed when accessed
        internal bool TryGet(string key, out RespValue value)
        {
            if (_data.TryGetValue(key, out StoredEntry entry))
            {
                if (entry.IsExpired)
                {
                    if (_data.TryRemove(new KeyValuePair<string, StoredEntry>(key, entry)))
                    {
                        _volatileKeySet.Remove(key);
                    }
                    
                    value = default;
                    return false;
                }

                value = entry.Value;
                return true;
            }

            value = default;
            return false;
        }

        // checks small buffer of (20) elements to check there expiry status by key
        // same key can be picked more than one, it will simply skip or clean it up
        internal (int sampled, int expired) ExpireSample()
        {
            int count = _volatileKeySet.Sameple(_sampleBuffer);
            int expired = 0;

            for (int i = 0; i < count; i++)
            {
                string key = _sampleBuffer[i];

                // key does not exists or no longer has TTL
                if (!_data.TryGetValue(key, out var entry) || entry.ExpiresAt is null)
                {
                    _volatileKeySet.Remove(key);
                    continue;
                }

                if (entry.IsExpired && _data.TryRemove(new KeyValuePair<string, StoredEntry>(key, entry)))
                {
                    expired++;
                    _volatileKeySet.Remove(key);
                }
            }

            return (count, expired);
        }

        internal bool Delete(string key)
        {
            if (_data.TryRemove(key, out _))
            {
                _volatileKeySet.Remove(key);
                return true;
            }
            return false;
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
                    _volatileKeySet.Add(key);
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
