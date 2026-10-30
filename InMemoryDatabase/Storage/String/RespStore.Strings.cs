using InMemoryDatabase.Resp.Enums;
using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage.String;

namespace InMemoryDatabase.Storage
{
    // partial class for string content
    public partial class RespStore
    {
        // dictionary value StoredEntry = RespValue + DateTime metadata
        public bool Set(string key, string value, DateTimeOffset? expiresAt, SetCondition condition)
        {
            var entry = new StoredEntry(new StringValue(value), expiresAt);

            // if condition state does not contains any additional params about ttl or expiry
            if (condition == SetCondition.Always)
            {
                _data[key] = entry;
                // include VolatileKeySet key tracking
                TrackExpiry(key, expiresAt);
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
                    // include VolatileKeySet key tracking
                    TrackExpiry(key, expiresAt);
                    return true;
                }
            }
        }

        // checks if expired, lazy eviction strategy, only remove when convenient, reduce cpu overhead
        // expired key value pair is removed when accessed
        //internal bool TryGet(string key, out RespValue value)

        internal bool TryIncrement(string key, long delta, out long newValue, out string? error)
        {
            while (true)
            {
                bool present = _data.TryGetValue(key, out StoredEntry stored);
                // exists and is not expired
                bool live = present && !stored.IsExpired;

                // live value or "0" for a missing/expired key
                StoredValue currentValue = live ? stored.Value : new StringValue("0");
                DateTimeOffset? expiresAt = live ? stored.ExpiresAt : null;

                // exlicit value's type check for new stringValue
                if (currentValue is not StringValue str)
                {
                    newValue = 0;
                    error = "WRONGTYPE Operation against a key holding the wrong kind of value";
                    return false;
                }

                // check if value type is bulk string in first place or can be parsed as long
                if (!long.TryParse(str.Value, out long currentNum))
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
                    // updating result
                    var updated = new StoredEntry(new StringValue(candidate.ToString()), expiresAt);

                    // compare against "stored", entry which was read from dictionary
                    bool swapped = present
                        ? _data.TryUpdate(key, updated, stored)
                        // key did not exist, add new instead
                        : _data.TryAdd(key, updated);

                    if (swapped)
                    {
                        if (present && !live)
                        {
                            // replaced an expired key: the new value has no TTL
                            _volatileKeySet.Remove(key);
                        }

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

        // same function as TryGet but for strings only, checks if type is StringValue instead of list or any other
        internal StoreStatus TryGetString(string key, out string? value)
        {
            value = null;

            if (!TryGetLiveEntry(key, out StoredEntry entry))
            {
                return StoreStatus.NotFound;
            }

            if (entry.Value is not StringValue str)
            {
                return StoreStatus.WrongType;
            }

            value = str.Value;
            return StoreStatus.Ok;
        }
    }
}
