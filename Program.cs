using InMemoryDatabase.Parser;
using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.TCP;
using System.Buffers;
using System.Text;

void TestParse(string label, byte[] data)
{
    var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(data));
    try
    {
        RespValue result = RespParser.ParseValue(ref reader, depth: 0);
        Console.WriteLine($"{label}: OK -> {Describe(result)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{label}: THREW {ex.GetType().Name} - {ex.Message}");
    }
}

string Describe(RespValue v) => v.Type switch
{
    RespValueType.SimpleString => $"SimpleString({v.TypeString})",
    RespValueType.Error => $"Error({v.TypeString})",
    RespValueType.Integer => $"Integer({v.TypeInteger})",
    RespValueType.BulkString => $"BulkString({v.TypeString})",
    RespValueType.Array => $"Array[{v.TypeArray.Length}]",
    RespValueType.Null => "Null",
    _ => "?"
};

TestParse("simple string", Encoding.UTF8.GetBytes("+OK\r\n"));
TestParse("error", Encoding.UTF8.GetBytes("-ERR unknown command\r\n"));
TestParse("integer", Encoding.UTF8.GetBytes(":1000\r\n"));
TestParse("bulk string", Encoding.UTF8.GetBytes("$5\r\nhello\r\n"));
TestParse("null bulk", Encoding.UTF8.GetBytes("$-1\r\n"));
TestParse("array", Encoding.UTF8.GetBytes("*2\r\n$3\r\nfoo\r\n:42\r\n"));
TestParse("nested array", Encoding.UTF8.GetBytes("*2\r\n*1\r\n:1\r\n$3\r\nbar\r\n"));
TestParse("incomplete", Encoding.UTF8.GetBytes("$5\r\nhel")); // should throw RespIncompleteDataException
TestParse("bad prefix", Encoding.UTF8.GetBytes("?xyz\r\n")); // should throw RespProtocolException


//await TCPServer.Start(CancellationToken.None);