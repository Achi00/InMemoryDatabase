using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class DecrCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "DECR";

        public DecrCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 1)
            {
                return RespValue.Error("ERR wrong number of arguments for 'decr' command");
            }

            string key = args[0].TypeString!;

            if (!_store.TryDecrement(key, delta: 1, out long newValue, out string? error))
            {
                return RespValue.Error(error!);
            }

            return RespValue.Integer(newValue);
        }
    }
}
