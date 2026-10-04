using InMemoryDatabase.Commands;
using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;

namespace InMemoryDatabase.Exucutors
{
    public class RespCommandExecutor
    {
        private readonly Dictionary<string, ICommandHandler> _handlers;

        public RespCommandExecutor(IEnumerable<ICommandHandler> handlers)
        {
            // key = command e.g: SET, GET, DEL...
            // value = case insensitive e.g: SET = set
            _handlers = handlers.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
        }

        public RespValue Execute(RespValue command)
        {
            if (command.Type != RespValueType.Array || command.TypeArray!.Length == 0)
            {
                return RespValue.Error("ERR invalid command format");
            }

            var parts = command.TypeArray;
            // strip command name in dispatcher before passing to executors, in this level its done only once
            var name = parts[0].TypeString!.ToUpperInvariant();
            var args = parts[1..];

            return _handlers.TryGetValue(name, out var handler) ? handler.Execute(args) : RespValue.Error($"ERR unknown command '{name}'");
        }
    }
}
