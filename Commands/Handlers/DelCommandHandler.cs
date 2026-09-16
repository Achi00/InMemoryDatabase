using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public class DelCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "DEL";

        public DelCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length < 2)
            {
                return RespValue.Error("ERR wrong number of arguments for 'del' command");
            }

            int deleted = 0;

            // start at index 1, 0 is command itself
            for (int i = 1; i < args.Length; i++)
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
