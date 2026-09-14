using InMemoryDatabase.Parser.Enums;

namespace InMemoryDatabase.Parser.Models
{
    public readonly struct RespValue
    {
        public RespValueType Type { get; }
        public string? TypeString { get; }
        public long TypeInteger { get; }
        public RespValue[] TypeArray { get; }

        private RespValue(RespValueType type, string? str = null, long integer = 0, RespValue[]? array = null)
        {
            Type = type; TypeString = str; TypeInteger = integer; TypeArray = array;
        }

        public static RespValue SimpleString(string s) => new(RespValueType.SimpleString, str: s);
        public static RespValue Error(string s) => new(RespValueType.Error, str: s);
        public static RespValue Integer(long i) => new(RespValueType.Integer, integer: i);
        public static RespValue BulkString(string s) => new(RespValueType.BulkString, str: s);
        public static RespValue Array(RespValue[] items) => new(RespValueType.Array, array: items);

        public static RespValue NullArray() => new(RespValueType.Null, array: null);
        public static RespValue NullBulkString() => new(RespValueType.Null);

        public static RespValue Null { get; } = new(RespValueType.Null);
    }
}
