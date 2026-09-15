using InMemoryDatabase.Commands;
using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.Storage;

namespace InMemoryDatabase.Exucutors
{
    public class RespCommandExecutor
    {
        private readonly Dictionary<string, ICommandHandler> _handlers;

        public RespCommandExecutor(IEnumerable<ICommandHandler> handlers)
        {
            _handlers = handlers.ToDictionary(h => h.Name, StringComparer.OrdinalIgnoreCase);
        }

        public RespValue Execute(RespValue command)
        {
            if (command.Type != RespValueType.Array || command.TypeArray!.Length == 0)
                return RespValue.Error("ERR invalid command format");

            RespValue[] parts = command.TypeArray;
            string name = parts[0].TypeString!.ToUpperInvariant();

            return _handlers.TryGetValue(name, out var handler) ? handler.Execute(parts) : RespValue.Error($"ERR unknown command '{name}'");
        }
    }
}
