using InMemoryDatabase.Parser.Enums;

namespace InMemoryDatabase.Parser.Models
{
    public struct RespValue
    {
        public RespValueType Type { get; }
        public string String { get; }
        public long Integer { get; }
        public RespValue[] Array { get; }
    }
}
