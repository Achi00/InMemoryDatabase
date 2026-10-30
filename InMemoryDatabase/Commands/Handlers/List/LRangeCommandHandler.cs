using InMemoryDatabase.Resp.Models;
using InMemoryDatabase.Storage;
using InMemoryDatabase.Storage.List.Enums;

namespace InMemoryDatabase.Commands.Handlers.List
{
    public sealed class LRangeCommandHandler : ICommandHandler
    {
        private readonly RespStore _store;
        public string Name => "LRANGE";

        public LRangeCommandHandler(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue[] args)
        {
            if (args.Length > 3)
            {
                return RespValue.Error($"ERR wrong number of arguments for '{Name}' command");
            }

            var key = args[0].TypeString!;

            if (!long.TryParse(args[1].TypeString, out long start) || !long.TryParse(args[2].TypeString, out long end))
            {
                return RespValue.Error("ERR value is not an integer or out of range");
            }

            var status = _store.Range(key, ToIndex(start), ToIndex(end), out string[] arr);

            if (status == ListStatus.WrongType)
            {
                return RespValue.Error("WRONGTYPE Operation against a key holding the wrong kind of value");
            }

            // Ok or NotFound
            var reply = new RespValue[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                reply[i] = RespValue.BulkString(arr[i]);
            }

            return RespValue.Array(reply);
        }

        // parse long value to int
        private static int ToIndex(long value) => (int)Math.Clamp(value, int.MinValue, int.MaxValue);
    }
}
