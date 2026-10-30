using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;
using InMemoryDatabase.Storage.List.Enums;

namespace InMemoryDatabase.Commands.Handlers.List.Push
{
    public sealed class LPushCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "LPUSH";

        public LPushCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length < 2)
            {
                return RespValue.Error($"ERR wrong number of arguments for '{Name}' command");
            }

            string key = args[0].TypeString!;

            string[] values = new string[args.Length - 1];

            // skip command i = 1
            for (int i = 1; i < args.Length; i++)
            {
                values[i - 1] = args[i].TypeString!;
            }

            var status = _store.Push(key, values, ListSide.Left, out long length);

            if (status == StoreStatus.WrongType)
            {
                return RespValue.Error("WRONGTYPE Operation against a key holding the wrong kind of value");
            }
            else
            {
                return RespValue.Integer(length);
            }
        }
    }
}
