using InMemoryDatabase.Parser.Models;

namespace InMemoryDatabase.Commands.Handlers
{
    public sealed class PingCommandHandler : ICommandHandler
    {
        public string Name => "PING";

        public RespValue Execute(RespValue[] args)
        {
            if (args == null || args.Length == 0)
            {
                return RespValue.SimpleString("PONG");
            }

            if (args.Length == 1)
            {
                return args[0];
            }

            return RespValue.Error("ERR wrong number of arguments for 'PING' command");
        }
    }
}
