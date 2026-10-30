using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage.List;
using InMemoryDatabase.Storage.List.Enums;

namespace InMemoryDatabase.Storage
{
    // partial class for list content
    // manages many instances of LinkedListStorage
    // lock object lives in ListValue to have one lock per list, so no locking entire dictionary
    public partial class RespStore
    {
        // push: RPUSH, LPUSH decidd by side property
        internal StoreStatus Push(string key, string[] values, ListSide side, out long length)
        {
            // uses main RespStore's dictionary
            while (true)
            {
                bool exists = _data.TryGetValue(key, out var entry);

                if (exists && !entry.IsExpired)
                {
                    // key's value is not list
                    if (entry.Value is not ListValue list)
                    {
                        length = 0;
                        return StoreStatus.WrongType;
                    }

                    lock (list.Sync)
                    {
                        // check if data is still in list
                        // needed because of gap between first TryGet and lock, might other thread modify/lock it
                        if (!_data.TryGetValue(key, out var current) || !ReferenceEquals(current.Value, list))
                        {
                            // information is out of date
                            // list is no longer stored under this key, read the dictionary again
                            continue;
                        }

                        PushAll(list.Items, values, side);
                        length = list.Items.Count;
                        return StoreStatus.Ok;
                    }
                }

                // key is missing or expired
                var newList = new ListValue(new LinkedListStorage());
                PushAll(newList.Items, values, side);
                var newEntry = new StoredEntry(newList, expiresAt: null);

                bool added = exists 
                    // replace/update expired entry
                    ? _data.TryUpdate(key, newEntry, entry) 
                    // add new key
                    : _data.TryAdd(key, newEntry);

                if (added)
                {
                    // add tracking in keysets
                    _volatileKeySet.Remove(key);
                    length = newList.Items.Count;
                    return StoreStatus.Ok;
                }
            }
        }

        internal StoreStatus Pop(string key, ListSide side, out string? value)
        {
            while (true)
            {
                if (!TryGetLiveEntry(key, out StoredEntry entry))
                {
                    value = null;
                    return StoreStatus.NotFound;
                }


                // key's value is not list
                if (entry.Value is not ListValue list)
                {
                    value = null;
                    return StoreStatus.WrongType;
                }

                lock (list.Sync)
                {
                    // check if data is still in list
                    // needed because of gap between first TryGet and lock, might other thread modify/lock it
                    if (!_data.TryGetValue(key, out var current) || !ReferenceEquals(current.Value, list))
                    {
                        // information is out of date
                        // list is no longer stored under this key, read the dictionary again
                        continue;
                    }

                    // pop based on side
                    value = side == ListSide.Left ? list.Items.PopLeft() : list.Items.PopRight();

                    if (list.Items.Count == 0)
                    {
                        // if list is empty remove it
                        RemoveEmptyList(key, list);
                    }

                    return StoreStatus.Ok;
                }
            }
        }

        internal StoreStatus Length(string key, out int count)
        {
            if (!TryGetLiveEntry(key, out StoredEntry entry))
            {
                count = 0;
                return StoreStatus.NotFound;
            }

            // key's value is not list
            if (entry.Value is not ListValue list)
            {
                count = 0;
                return StoreStatus.WrongType;
            }

            lock (list.Sync)
            {
                count = list.Items.Count;
            }
            return StoreStatus.Ok;
        }

        internal StoreStatus Range(string key, int start, int end, out string[] arr)
        {
            if (!TryGetLiveEntry(key, out StoredEntry entry))
            {
                arr = Array.Empty<string>();
                return StoreStatus.NotFound;
            }
            if (entry.Value is not ListValue list)
            {
                arr = Array.Empty<string>();
                return StoreStatus.WrongType;
            }

            lock (list.Sync)
            {
                var length = list.Items.Count;
                // negative index starter, return from back
                if (start < 0)
                {
                    start += length;
                }
                if (end < 0)
                {
                    end += length;
                }

                // clamping values, adjust start and end according to array size if adjusted values are still outside of list
                if (start < 0)
                {
                    start = 0;
                }

                // if 
                if (end >= length)
                {
                    end = length - 1;
                }

                // nothing in array
                if (start > end || start >= length)
                {
                    arr = Array.Empty<string>();
                    return StoreStatus.Ok;
                }

                arr = list.Items.Range(start, end - start + 1);
                return StoreStatus.Ok;
            }
        }

        // bulk push
        private static void PushAll(IListStorage items, string[] values, ListSide side)
        {
            foreach (string value in values)
            {
                if (side == ListSide.Left)
                {
                    items.PushLeft(value);
                }
                else items.PushRight(value);
            }
        }

        private void RemoveEmptyList(string key, ListValue list)
        {
            while (_data.TryGetValue(key, out var current) && ReferenceEquals(current.Value, list))
            {
                if (_data.TryRemove(new KeyValuePair<string, StoredEntry>(key, current)))
                {
                    _volatileKeySet.Remove(key);
                    return;
                }
            }
        }
    }
}
