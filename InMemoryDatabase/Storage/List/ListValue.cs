namespace InMemoryDatabase.Storage.List
{
    internal sealed class ListValue : StoredValue
    {
        public object Sync { get; } = new();
        public IListStorage Items { get; }

        public ListValue(IListStorage items)
        {
            Items = items;
        }
    }
}
