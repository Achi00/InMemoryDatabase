using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class IncrCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "INCR";

        public IncrCommandHandler(RespStore store)
        {
            _store = store;
        }
        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 1)
            {
                return RespValue.Error("ERR wrong number of arguments for 'incr' command");
            }

            string key = args[0].TypeString!;

            // try to parse key as integer and increment its value
            if (!_store.TryIncrement(key, delta: -1, out long newValue, out string? error))
            {
                return RespValue.Error(error!);
            }

            return RespValue.Integer(newValue);
        }
    }
}
