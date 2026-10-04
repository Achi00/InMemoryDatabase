using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class ExistsCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "EXISTS";

        public ExistsCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length < 1)
            {
                return RespValue.Error("ERR wrong number of arguments for 'exists' command");
            }

            int count = 0;
            // no need to start from index 1, all element is key now
            for (int i = 0; i < args.Length; i++)
            {
                if (_store.TryGet(args[i].TypeString!, out _))
                {
                    count++;
                }
            }

            return RespValue.Integer(count);
        }
    }
}
