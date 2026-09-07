using InMemoryDatabase.Exceptions;
using InMemoryDatabase.Parser.Models;
using System.Buffers;

namespace InMemoryDatabase.Parser
{
    public static class RespParser
    {
        public static RespValue Parser(ref SequenceReader<byte> reader)
        {
            if (!reader.TryRead(out byte prefix))
            {
                throw new RespProtocolException("Unexpected end of input");
            }

            return prefix switch
            {
                (byte)'+' => ParseSimpleString(ref reader),
                (byte)'-' => ParseError(ref reader),
                (byte)':' => ParseInteger(ref reader),
                (byte)'$' => ParseBulkString(ref reader),
                (byte)'*' => ParseArray(ref reader),
                _ => throw new RespProtocolException($"Unknown prefix: {(char)prefix}")
            };
        }

        private static RespValue ParseArray(ref SequenceReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        private static RespValue ParseBulkString(ref SequenceReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        private static RespValue ParseInteger(ref SequenceReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        private static RespValue ParseError(ref SequenceReader<byte> reader)
        {
            throw new NotImplementedException();
        }

        private static RespValue ParseSimpleString(ref SequenceReader<byte> reader)
        {
            throw new NotImplementedException();
        }
    }
}
