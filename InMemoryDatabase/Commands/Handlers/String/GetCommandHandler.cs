using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;
using InMemoryDatabase.Storage.List.Enums;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class GetCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "GET";

        public GetCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 1)
            {
                return RespValue.Error("ERR wrong number of arguments for 'get' command");
            }

            string key = args[0].TypeString!;

            var status = _store.TryGetString(key, out string? value);

            return status switch
            {
                StoreStatus.Ok => RespValue.BulkString(value!),
                StoreStatus.NotFound => RespValue.Null,
                StoreStatus.WrongType => RespValue.Error("WRONGTYPE Operation against a key holding the wrong kind of value"),
                _ => throw new InvalidOperationException($"Unexpected status {status}")
            };
        }
    }
}
