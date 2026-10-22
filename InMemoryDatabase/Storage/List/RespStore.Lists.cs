using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage.List;
using InMemoryDatabase.Storage.List.Enums;

namespace InMemoryDatabase.Storage
{
    // partial class for list content
    // manages many instances of LinkedListStorage
    public partial class RespStore
    {
        internal ListStatus Push(string key, string[] values, ListSide side, out long length)
        {
            // uses main RespStore's dictionary
            while (true)
            {
                bool exists = _data.TryGetValue(key, out var entry);

                if (exists && !entry.IsExpired)
                {
                    if (entry.Value is not ListValue list)
                    {
                        length = 0;
                        return ListStatus.WrongType;
                    }

                    lock (list.Sync)
                    {
                        // check if data is still in list
                        if (!_data.TryGetValue(key, out var current) || !ReferenceEquals(current.Value, list))
                        {
                            continue;
                        }
                    }
                }
            }

            length = 0;
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
    }
}
