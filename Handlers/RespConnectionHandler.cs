using InMemoryDatabase.Exceptions;
using InMemoryDatabase.Parser;
using InMemoryDatabase.Parser.Models;
using System.Buffers;
using System.IO.Pipelines;
using System.Reflection.PortableExecutable;

namespace InMemoryDatabase.Handlers
{
    public class RespConnectionHandler
    {
        public async Task ProcessAsync(PipeReader reader, CancellationToken ct)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync(ct);
                ReadOnlySequence<byte> buffer = result.Buffer;
            }
        }

        private static bool TryParseOne(ref ReadOnlySequence<byte> buffer, out RespValue command)
        {
            var reader = new SequenceReader<byte>(buffer);

            try
            {
                command = RespParser.ParseValue(ref reader, depth: 0);
                // consumes already seccessfully parsed data
                buffer = buffer.Slice(reader.Position);

                return true;
            }
            catch (RespIncompleteDataException)
            {
                command = default;
                // wait for more data from pipe, buffer is unchanged, no throwing
                return false;
            }
            // RespProtocolException not cought here, it should bubble up and kill connection
        }
    }
}
