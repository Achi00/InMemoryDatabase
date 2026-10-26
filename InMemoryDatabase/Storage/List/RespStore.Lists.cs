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
        internal ListStatus Push(string key, string[] values, ListSide side, out long length)
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
                        return ListStatus.WrongType;
                    }

                    lock (list.Sync)
                    {
                        // check if data is still in list
                        // needed because of gap between first TryGet and lock, might other thread modify/lock it
                        if (!_data.TryGetValue(key, out var current) || !ReferenceEquals(current.Value, list))
                        {
                            // information is out of date
                            // carry on with whatever is in this list again
                            continue;
                        }

                        PushAll(list.Items, values, side);
                        length = list.Items.Count;
                        return ListStatus.Ok;
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
                    return ListStatus.Ok;
                }
            }
        }

        internal ListStatus Pop(string key, ListSide side, out string? value)
        {
            while (true)
            {
                if (!_data.TryGetValue(key, out StoredEntry entry) || entry.IsExpired)
                {
                    value = null;
                    return ListStatus.NotFound;
                }


                // key's value is not list
                if (entry.Value is not ListValue list)
                {
                    value = null;
                    return ListStatus.WrongType;
                }

                lock (list.Sync)
                {
                    // check if data is still in list
                    // needed because of gap between first TryGet and lock, might other thread modify/lock it
                    if (!_data.TryGetValue(key, out var current) || !ReferenceEquals(current.Value, list))
                    {
                        // information is out of date
                        // carry on with whatever is in this list again
                        continue;
                    }

                    // pop based on side
                    value = side == ListSide.Left ? list.Items.PopLeft() : list.Items.PopRight();

                    if (list.Items.Count == 0)
                    {
                        // if list is empty remove it
                        RemoveEmptyList(key, list);
                    }

                    return ListStatus.Ok;
                }
            }
        }

        internal ListStatus Length(string key, out int count)
        {
            if (!_data.TryGetValue(key, out StoredEntry entry) || entry.IsExpired)
            {
                count = 0;
                return ListStatus.NotFound;
            }

            // key's value is not list
            if (entry.Value is not ListValue list)
            {
                count = 0;
                return ListStatus.WrongType;
            }

            lock (list.Sync)
            {
                count = list.Items.Count;
            }
            return ListStatus.Ok;
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
