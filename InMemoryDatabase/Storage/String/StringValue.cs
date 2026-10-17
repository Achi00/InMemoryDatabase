namespace InMemoryDatabase.Storage.String
{
    internal class StringValue : StoredValue
    {
        public string Value { get; }
        public StringValue(string value)
        {
            Value = value;
        }
    }
}
