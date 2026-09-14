using InMemoryDatabase.Exceptions;
using InMemoryDatabase.Parser;
using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;
using System.Buffers;
using System.IO.Pipelines;

namespace InMemoryDatabase.Handlers
{
    public class RespConnectionHandler
    {
        private static readonly TimeSpan IncompleteCommandTimeout = TimeSpan.FromSeconds(10);

        public static async Task ProcessAsync(PipeReader reader, PipeWriter pipeWriter, RespCommandExecutor executor, CancellationToken ct)
        {
            while (true)
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(IncompleteCommandTimeout);

                ReadResult result;

                try
                {
                    result = await reader.ReadAsync(ct);
                }
                catch (Exception)
                {

                    throw;
                }
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryParseOne(ref buffer, out var command))
                {
                    // simple printing at this stage
                    PrintRespValue(command);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // tells pipe that we are done reading, should release all resources it holds
            await reader.CompleteAsync();
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

        // print in console
        private static void PrintRespValue(RespValue value, int indent = 0)
        {
            string pad = new string(' ', indent * 2);

            switch (value.Type)
            {
                case RespValueType.SimpleString:
                    Console.WriteLine($"{pad}SimpleString: {value.TypeString}");
                    break;
                case RespValueType.Error:
                    Console.WriteLine($"{pad}Error: {value.TypeString}");
                    break;
                case RespValueType.Integer:
                    Console.WriteLine($"{pad}Integer: {value.TypeInteger}");
                    break;
                case RespValueType.BulkString:
                    Console.WriteLine($"{pad}BulkString: {value.TypeString}");
                    break;
                case RespValueType.Null:
                    Console.WriteLine($"{pad}Null");
                    break;
                case RespValueType.Array:
                    Console.WriteLine($"{pad}Array[{value.TypeArray.Length}]:");
                    foreach (var item in value.TypeArray)
                        PrintRespValue(item, indent + 1);
                    break;
            }
        }
    }
}
