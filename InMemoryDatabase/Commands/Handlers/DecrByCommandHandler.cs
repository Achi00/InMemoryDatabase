using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;
using System.Buffers.Text;
using System.Text;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class DecrByCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "DECRBY";

        public DecrByCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 2)
            {
                return RespValue.Error("ERR wrong number of arguments for 'decrby' command");
            }
            string key = args[0].TypeString!;

            if (!Utf8Parser.TryParse(Encoding.UTF8.GetBytes(args[1].TypeString!), out long delta, out _))
            {
                return RespValue.Error("ERR value is not an integer or out of range");
            }

            // pass negative delta as this is decrement command
            if (!_store.TryIncrement(key, -delta, out long newValue, out string? error))
            {
                return RespValue.Error(error!);
            }

            return RespValue.Integer(newValue);
        }
    }
}
