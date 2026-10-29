using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage.Keys;
using System.Buffers;
using System.Collections.Concurrent;

namespace InMemoryDatabase.Storage
{
    public partial class RespStore
    {
        private readonly ConcurrentDictionary<string, StoredEntry> _data = new();
        // combines list and dictionary O(1) lookup with key + index
        private readonly VolatileKeySet _volatileKeySet;
        // used for array pool sizing, thread safe!!
        private const int SampleSize = 20;

        internal int PhysicalCount => _data.Count;
        internal int VolatileCount => _volatileKeySet.Count;

        public RespStore() : this(new VolatileKeySet()) { }

        // for tests
        internal RespStore(VolatileKeySet volatileKeySet)
        {
            _volatileKeySet = volatileKeySet;
        }

        // checks small buffer of (20) elements to check there expiry status by key
        // same key can be picked more than one, it will simply skip or clean it up
        internal (int sampled, int expired) ExpireSample()
        {
            // shared thread safe buffer, should call Rent in single place in lifetime of this chunk of mem
            string[] buffer = ArrayPool<string>.Shared.Rent(SampleSize);
            try
            {
                // passing shared array as pool instead of reference of array which will be shared and cause race condition
                int count = _volatileKeySet.Sample(buffer.AsSpan(0, SampleSize));
                int expired = 0;

                for (int i = 0; i < count; i++)
                {
                    string key = buffer[i];

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
            finally
            {
                // return array to pool
                ArrayPool<string>.Shared.Return(buffer, clearArray: true);
            }
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
            if (!TryGetLiveEntry(key, out StoredEntry entry))
            {
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

        // include expiry tracking based on SetCondition value
        private void TrackExpiry(string key, DateTimeOffset? expiresAt)
        {
            if (expiresAt is not null)
            {
                _volatileKeySet.Add(key);
            }
            else
            {
                // plain SET clears the TTL
                _volatileKeySet.Remove(key);
            }
        }
        // get existing key as StoredEntry or remove
        internal bool KeyExists(string key) => TryGetLiveEntry(key, out _);

        private bool TryGetLiveEntry(string key, out StoredEntry entry)
        {
            if (!_data.TryGetValue(key, out entry))
            {
                return false;
            }

            if (entry.IsExpired)
            {
                // remove only if its still the expired entry we read
                if (_data.TryRemove(new KeyValuePair<string, StoredEntry>(key, entry)))
                {
                    _volatileKeySet.Remove(key);
                }

                entry = default;
                return false;
            }

            return true;
        }
    }
}
