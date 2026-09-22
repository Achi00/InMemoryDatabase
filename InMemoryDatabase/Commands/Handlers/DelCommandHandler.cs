using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class DelCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "DEL";

        public DelCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length < 1)
            {
                return RespValue.Error("ERR wrong number of arguments for 'del' command");
            }

            int deleted = 0;

            // no need to start from index 1, all element is key now
            for (int i = 0; i < args.Length; i++)
            {
                if (_store.Delete(args[i].TypeString!))
                {
                    deleted++;
                }
            }

            return RespValue.Integer(deleted);
        }
    }
}
