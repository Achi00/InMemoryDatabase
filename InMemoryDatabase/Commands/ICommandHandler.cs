
using InMemoryDatabase.Resp.Models;

namespace InMemoryDatabase.Commands
{
    public interface ICommandHandler
    {
        public string Name { get; }
        RespValue Execute(RespValue[] args);
    }
}
