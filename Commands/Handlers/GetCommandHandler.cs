using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

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
            return _store.TryGet(key, out RespValue value) ? value : RespValue.Null;
        }
    }
}
