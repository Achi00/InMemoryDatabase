using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Exucutors
{
    public class RespCommandExecutor
    {
        private readonly RespStore _store;

        public RespCommandExecutor(RespStore store)
        {
            _store = store;
        }

        public RespValue Execute(RespValue command)
        {
            if (command.Type != RespValueType.Array || command.TypeArray!.Length == 0)
                return RespValue.Error("ERR invalid command format");

            RespValue[] parts = command.TypeArray;
            string name = parts[0].TypeString!.ToUpperInvariant();

            return name switch
            {
                "PING" => RespValue.SimpleString("PONG"),
                "SET" => HandleSet(parts),
                "GET" => HandleGet(parts),
                _ => RespValue.Error($"ERR unknown command '{name}'")
            };
        }

        private RespValue HandleSet(RespValue[] parts)
        {
            if (parts.Length != 3)
            {
                return RespValue.Error("ERR wrong number of arguments for 'set' command");
            }

            string key = parts[1].TypeString!;
            _store.Set(key, RespValue.BulkString(parts[2].TypeString!));
            
            return RespValue.SimpleString("OK");
        }

        private RespValue HandleGet(RespValue[] parts)
        {
            if (parts.Length != 2)
            {
                return RespValue.Error("ERR wrong number of arguments for 'get' command");
            }

            string key = parts[1].TypeString!;
            return _store.TryGet(key, out RespValue value) ? value : RespValue.Null;
        }
    }
}
