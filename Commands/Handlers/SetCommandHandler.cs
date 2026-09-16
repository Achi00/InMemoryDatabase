using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class SetCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;

        public SetCommandHandler(RespStore store)
        {
            _store = store;
        }
        public string Name => "SET";

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 3)
            {
                return RespValue.Error("ERR wrong number of arguments for 'set' command");
            }

            string key = args[1].TypeString!;
            _store.Set(key, RespValue.BulkString(args[2].TypeString!));

            return RespValue.SimpleString("OK");
        }
    }
}
