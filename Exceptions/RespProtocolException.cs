namespace InMemoryDatabase.Exceptions
{
    public class RespProtocolException : Exception
    {
        public RespProtocolException()
        {
        }

        public RespProtocolException(string? message) : base(message)
        {
        }
    }
}
