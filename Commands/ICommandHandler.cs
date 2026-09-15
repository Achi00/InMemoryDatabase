using InMemoryDatabase.Parser.Models;

namespace InMemoryDatabase.Commands
{
    public interface ICommandHandler
    {
        public string Name { get; }
        RespValue Execute(RespValue[] args);
    }
}
