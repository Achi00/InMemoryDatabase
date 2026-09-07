using InMemoryDatabase.Exceptions;
using InMemoryDatabase.Parser.Models;
using System.Buffers;
using System.Buffers.Text;

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

        // zero allication array parser
        private static RespValue ParseArray(ref SequenceReader<byte> reader)
        {
            int count = ReadIntLine(ref reader);

            var items = new RespValue[count];

            for (int i = 0; i < count; i++)
            {
                items[i] = ParseArray(ref reader);
            }

            return RespValue.Array(items);
        }

        private static int ReadIntLine(ref SequenceReader<byte> reader)
        {
            // look for delimiter \r\n
            ReadOnlySpan<byte> crlf = stackalloc byte[] { (byte)'\r', (byte)'\n' };

            if (!reader.TryReadTo(out ReadOnlySequence<byte> line, crlf))
            {
                throw new RespProtocolException("Incomplete line");
            }

            // ascii to integer
            if (line.IsSingleSegment)
            {
                if (!Utf8Parser.TryParse(line.FirstSpan, out int result, out _))
                {
                    throw new RespProtocolException("Invalid integer format");
                }
                return result;
            }
            else
            {
                // fallback for multi-segment sequences
                Span<byte> localSpan = stackalloc byte[(int)line.Length];
                line.CopyTo(localSpan);
               
                if (!Utf8Parser.TryParse(localSpan, out int result, out _))
                {
                    throw new RespProtocolException("Invalid integer format");
                }

                return result;
            }
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
