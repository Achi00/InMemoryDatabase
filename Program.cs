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


// multi segment path testing
// split across two segments to simulate simple incoming traffic behaivor
byte[] part1 = Encoding.UTF8.GetBytes("*1\r\n:12");
byte[] part2 = Encoding.UTF8.GetBytes("34\r\n");
var first = new SequenceSegment(part1);
var last = first.Add(part2);
var multiSegSequence = new ReadOnlySequence<byte>(first, 0, last, part2.Length);

var reader2 = new SequenceReader<byte>(multiSegSequence);
RespValue result2 = RespParser.ParseValue(ref reader2, depth: 0);

PrintRespValue(result2);

void PrintRespValue(RespValue value, int indent = 0)
{
    string pad = new string(' ', indent * 2);

    switch (value.Type)
    {
        case RespValueType.SimpleString:
            Console.WriteLine($"{pad}SimpleString: {value.TypeString}");
            break;
        case RespValueType.Error:
            Console.WriteLine($"{pad}Error: {value.TypeString}");
            break;
        case RespValueType.Integer:
            Console.WriteLine($"{pad}Integer: {value.TypeInteger}");
            break;
        case RespValueType.BulkString:
            Console.WriteLine($"{pad}BulkString: {value.TypeString}");
            break;
        case RespValueType.Null:
            Console.WriteLine($"{pad}Null");
            break;
        case RespValueType.Array:
            Console.WriteLine($"{pad}Array[{value.TypeArray.Length}]:");
            foreach (var item in value.TypeArray)
                PrintRespValue(item, indent + 1);
            break;
    }
}

class SequenceSegment : ReadOnlySequenceSegment<byte>
{
    public SequenceSegment(ReadOnlyMemory<byte> memory) => Memory = memory;
    public SequenceSegment Add(ReadOnlyMemory<byte> mem)
    {
        var segment = new SequenceSegment(mem) { RunningIndex = RunningIndex + Memory.Length };
        Next = segment;
        return segment;
    }
}

//await TCPServer.Start(CancellationToken.None);