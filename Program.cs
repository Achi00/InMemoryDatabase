using InMemoryDatabase.Parser;
using InMemoryDatabase.Parser.Enums;
using InMemoryDatabase.Parser.Models;
using InMemoryDatabase.TCP;
using System.Buffers;
using System.Text;


await TCPServer.Start(CancellationToken.None);