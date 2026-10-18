namespace InMemoryDatabase.Storage.String
{
    internal sealed class StringValue : StoredValue
    {
        public string Value { get; }
        public StringValue(string value)
        {
            Value = value;
        }
    }
}
