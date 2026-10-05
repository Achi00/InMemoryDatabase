using InMemoryDatabase.Exceptions;
using InMemoryDatabase.Exucutors;
using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Resp;
using InMemoryDatabase.Resp.Parser;
using System.Buffers;
using System.IO.Pipelines;

namespace InMemoryDatabase.Handlers
{
    public class RespConnectionHandler
    {
        private static readonly TimeSpan IncompleteCommandTimeout = TimeSpan.FromSeconds(10);

        public static async Task ProcessAsync(PipeReader pipeReader, PipeWriter pipeWriter, RespCommandExecutor executor, CancellationToken ct)
        {
            while (true)
            {
                // trigger timeout in case process hand or not responsing
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(IncompleteCommandTimeout);

                ReadResult result;

                try
                {
                    result = await pipeReader.ReadAsync(ct);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // throw in case of timeout
                    throw new RespProtocolException("Connecten timed out waiting for complete command");
                }

                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryParseOne(ref buffer, out var command))
                {
                    var response = executor.Execute(command);
                    RespWriter.Write(response, pipeWriter);

                    // pushes data into pipe
                    await pipeWriter.FlushAsync();
                }

                pipeReader.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // tells pipe that we are done reading, should release all resources it holds
            await pipeReader.CompleteAsync();
            await pipeWriter.CompleteAsync();
        }

        private static bool TryParseOne(ref ReadOnlySequence<byte> buffer, out RespValue command)
        {
            var reader = new SequenceReader<byte>(buffer);

            try
            {
                // reader is passed to orcestrator method which will decite if this command needs inline parsing or is raw array of bytes
                command = RespParser.ParseCommand(ref reader);
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
