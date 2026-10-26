using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;
using InMemoryDatabase.Storage.List.Enums;

namespace InMemoryDatabase.Commands.Handlers.List.Pop
{
    public sealed class RPopCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "RPOP";

        public RPopCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 1)
            {
                return RespValue.Error($"ERR wrong number of arguments for '{Name}' command");
            }

            string key = args[0].TypeString!;

            ListStatus status = _store.Pop(key, ListSide.Right, out string? value);

            // depending on Pop returnes ListStatus value
            return status switch
            {
                ListStatus.Ok => RespValue.BulkString(value!),
                ListStatus.NotFound => RespValue.Null,
                ListStatus.WrongType => RespValue.Error("WRONGTYPE Operation against a key holding the wrong kind of value"),
                _ => throw new InvalidOperationException($"Unexpected status {status}")
            };
        }
    }
}
