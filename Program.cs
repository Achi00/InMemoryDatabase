using InMemoryDatabase.Parser;
using InMemoryDatabase.TCP;
using System.Buffers;
using System.Text;

// test parser
byte[] data = Encoding.UTF8.GetBytes("+OK\r\n");
var sequence = new ReadOnlySequence<byte>(data);
var reader = new SequenceReader<byte>(sequence);

var result = RespParser.ParseValue(ref reader, depth: 0);
Console.WriteLine($"{result.Type}: {result.TypeString}");
//await TCPServer.Start(CancellationToken.None);