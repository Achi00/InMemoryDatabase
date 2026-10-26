using InMemoryDatabase.Resp.Models;

namespace InMemoryDatabase.Commands.Handlers.List
{
    public sealed class RPushCommandHandler : ICommandHandler
    {
        public string Name => "RPUSH";

        public RespValue Execute(RespValue[] args)
        {
            throw new NotImplementedException();
        }
    }
}
