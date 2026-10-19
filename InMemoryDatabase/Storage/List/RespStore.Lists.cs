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
            // if key was not found create new
            if (!_data.TryGetValue(key, out StoredEntry entry) && !entry.IsExpired)
            {
                if (entry.Value is not ListValue list)
                {
                    length = 0;
                    return ListStatus.WrongType;
                }
            }

            length = 0;
            return ListStatus.Ok;
        }
    }
}
