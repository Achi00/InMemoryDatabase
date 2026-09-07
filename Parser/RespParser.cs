using InMemoryDatabase.Exceptions;
using System.Buffers;

namespace InMemoryDatabase.Parser
{
    public static class RespParser
    {
        public static void Parser(ref SequenceReader<byte> reader)
        {
            if (!reader.TryRead(out byte prefic))
            {
                throw new RespProtocolException("Unexpected end of input");
            }
        }
    }
}
