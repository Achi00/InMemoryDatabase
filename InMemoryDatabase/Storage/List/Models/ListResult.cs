using InMemoryDatabase.Storage.List.Enums;

namespace InMemoryDatabase.Storage.List.Models
{
    internal readonly record struct ListResult(StoreStatus Status, long Length);
}
