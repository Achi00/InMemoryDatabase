using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;
using System.Buffers.Text;
using System.Text;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class ExpireCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "EXPIRE";

        public ExpireCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length != 3)
            {
                return RespValue.Error("ERR wrong number of arguments for 'expire' command");
            }

            string key = args[1].TypeString!;

            if (!Utf8Parser.TryParse(Encoding.UTF8.GetBytes(args[2].TypeString!), out int seconds, out _))
            {
                return RespValue.Error("ERR value is not an integer or out of range");
            }

            bool set = _store.SetExpiry(key, DateTimeOffset.UtcNow.AddSeconds(seconds));
            return RespValue.Integer(set ? 1 : 0);
        }
    }
}
