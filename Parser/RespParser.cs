using InMemoryDatabase.Exceptions;
using InMemoryDatabase.Parser.Models;
using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace InMemoryDatabase.Parser
{
    public static class RespParser
    {
        // should embed static data in assembly with no runtime allocation and no stack reservation, without stackalloc!
        private static ReadOnlySpan<byte> Crlf => new byte[] { (byte)'\r', (byte)'\n' };

        // protects call stack
        private const int MaxNestingDepth = 32;
        private const int MaxArrayElements = 1_000_000;
        // 512 MB cap size for BulkStrings
        private const int MaxBulkStringLength = 512 * 1024 * 1024;

        public static RespValue ParseValue(ref SequenceReader<byte> reader, int depth)
        {
            if (depth > MaxNestingDepth)
            {
                throw new RespProtocolException("Array nesting too deep");
            }

            if (!reader.TryPeek(out byte prefix))
            {
                throw new RespIncompleteDataException();
            }

            // consume the type byte, move readers internal cursor
            reader.Advance(1);

            return prefix switch
            {
                (byte)'+' => ParseSimpleString(ref reader),
                (byte)'-' => ParseError(ref reader),
                (byte)':' => RespValue.Integer(ReadIntLine(ref reader)),
                (byte)'$' => ParseBulkString(ref reader),
                (byte)'*' => ParseArray(ref reader, depth + 1),
                _ => throw new RespProtocolException($"Unknown prefix: {(char)prefix}")
            };
        }

        // zero allication array parser
        private static RespValue ParseArray(ref SequenceReader<byte> reader, int depth)
        {
            int count = ReadIntLine(ref reader);

            if (count < 0)
            {
                return RespValue.NullArray();
            }
            if (count > MaxArrayElements)
            {
                throw new RespProtocolException("Array too large");
            }
            
            var items = new RespValue[count];

            for (int i = 0; i < count; i++)
            {
                items[i] = ParseValue(ref reader, depth);
            }

            return RespValue.Array(items);
        }

        private static int ReadIntLine(ref SequenceReader<byte> reader)
        {
            // tryes to read data untill specified delimiter is matched in span above, moves reader cursor withit
            ReadOnlySequence<byte> line = ReadLine(ref reader);

            // ascii to integer
            // if data is in one contigues block of memory, no reconstruction needed
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

                // added safety buffer
                const int MaxIntLineLength = 20;
                // because of stack allocation, if number is too long, stack overflow will happend
                if (line.Length > MaxIntLineLength)
                {
                    throw new RespProtocolException("Integer line too long");
                }

                // copy data into single one contiguous buffer if multiple segment arrives
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
            int length = ReadIntLine(ref reader);

            if (length < 0)
            {
                return RespValue.NullBulkString();
            }
            // 512 MB cap
            if (length > MaxBulkStringLength)
            {
                throw new RespProtocolException("Bulk string too large");
            }
            // +2 for trailing \r\n
            if (reader.Remaining < length + 2) 
            {
                throw new RespIncompleteDataException();
            }

            ReadOnlySequence<byte> payload = reader.Sequence.Slice(reader.Position, length);
            // move readers internal cursor
            reader.Advance(length);

            if (!reader.IsNext((ReadOnlySpan<byte>)Crlf, advancePast: true))
            {
                throw new RespProtocolException("Missing CRLF after bulk string");
            }

            string value = payload.IsSingleSegment
                ? Encoding.UTF8.GetString(payload.FirstSpan)
                // fallback, allocates the array too
                : Encoding.UTF8.GetString(payload.ToArray());

            return RespValue.BulkString(value);
        }

        private static RespValue ParseError(ref SequenceReader<byte> reader) => RespValue.Error(LineToString(ReadLine(ref reader)));

        private static RespValue ParseSimpleString(ref SequenceReader<byte> reader) => RespValue.SimpleString(LineToString(ReadLine(ref reader)));

        // checking lines and segments
        private static ReadOnlySequence<byte> ReadLine(ref SequenceReader<byte> reader)
        {
            // if TryReadTo returns false at this points means that it searched every byte in current buffer and no delimiter "\r\n", meaning incomplete line
            if (!reader.TryReadTo(out ReadOnlySequence<byte> line, Crlf))
            {
                throw new RespIncompleteDataException();
            }

            return line;
        }

        private static string LineToString(ReadOnlySequence<byte> line) =>
            line.IsSingleSegment
                ? Encoding.UTF8.GetString(line.FirstSpan)
                : Encoding.UTF8.GetString(line.ToArray());
    }
}
