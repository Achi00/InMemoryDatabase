using InMemoryDatabase.Storage.List;

namespace InMemoryDatabase.Storage
{
    // partial class for list content
    public partial class RespStore : IListStorage
    {
        private readonly LinkedList<string> _items = new();
        public int Count { get => _items.Count; }

        public string Get(int index)
        {
            throw new NotImplementedException();
        }

        public string? PopLeft()
        {
            throw new NotImplementedException();
        }

        public string? PopRight()
        {
            throw new NotImplementedException();
        }

        public void PushLeft(string value)
        {
            throw new NotImplementedException();
        }

        public void PushRight(string value)
        {
            throw new NotImplementedException();
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
