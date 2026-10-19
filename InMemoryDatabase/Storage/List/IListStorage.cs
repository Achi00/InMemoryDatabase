namespace InMemoryDatabase.Storage.List
{
    // used for different list implamentations, linked list, ring buffer
    public interface IListStorage
    {
        int Count { get; }
        void PushLeft(string value);
        void PushRight(string value);
        string? PopLeft();
        string? PopRight();
        // index already validated by the caller
        string Get(int index);
        void Set(int index, string value);
        // materialized while the caller holds the lock
        string[] Range(int start, int count);
    }
}
