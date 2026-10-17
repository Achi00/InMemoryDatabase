namespace InMemoryDatabase.Storage.List
{
    internal interface IRespStoreList
    {
        int Count { get; }
        void PushLeft(byte[] value);
        void PushRight(byte[] value);
        byte[]? PopLeft();
        byte[]? PopRight();
        byte[]? Get(int index); // LINDEX
        bool Set(int index, byte[] value); // LSET
        IEnumerable<byte[]> Range(int start, int stop); // LRANGE
    }
}
