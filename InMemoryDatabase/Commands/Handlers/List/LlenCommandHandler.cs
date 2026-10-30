using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers.List
{
    public sealed class LlenCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "LLEN";

        public LlenCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length < 1)
            {
                return RespValue.Error($"ERR wrong number of arguments for '{Name}' command");
            }

            string key = args[0].TypeString!;

            var status = _store.Length(key, out int count);


            if (status == StoreStatus.WrongType)
            {
                return RespValue.Error("WRONGTYPE Operation against a key holding the wrong kind of value");
            }
            else
            {
                return RespValue.Integer(count);
            }
        }
    }
}
