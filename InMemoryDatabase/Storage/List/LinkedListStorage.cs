namespace InMemoryDatabase.Storage.List
{
    internal sealed class LinkedListStorage : IListStorage
    {
        private readonly LinkedList<string> _items = new();
        public int Count { get => _items.Count; }

        public string Get(int index)
        {
            return _items.ElementAt(index);
        }

        public string? PopLeft()
        {
            if (_items.Count == 0)
            {
                return null;
            }

            // keep before removing
            string value = _items.First!.Value;
            _items.RemoveFirst();
            return value;
        }

        public string? PopRight()
        {
            if (_items.Count == 0)
            {
                return null;
            }

            string value = _items.Last!.Value;
            _items.RemoveLast();

            return value;
        }

        public void PushLeft(string value)
        {
            _items.AddFirst(value);
        }

        public void PushRight(string value)
        {
            _items.AddLast(value);
        }

        public string[] Range(int start, int count)
        {
            throw new NotImplementedException();
        }

        public void Set(int index, string value)
        {
            throw new NotImplementedException();
        }
    }
}
