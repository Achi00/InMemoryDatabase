using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class TtlCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "TTL";

        public TtlCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 2)
            {
                return RespValue.Error("ERR wrong number of arguments for 'ttl' command");
            }

            string key = args[1].TypeString!;
            long ttlSeconds = _store.GetTtlSeconds(key);

            return RespValue.Integer(ttlSeconds);
        }
    }
}
