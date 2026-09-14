using InMemoryDatabase.Exucutors;
using InMemoryDatabase.Servers;
using InMemoryDatabase.Storage;

var store = new RespStore();
var executor = new RespCommandExecutor(store);

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var server = new RespServer(6380, executor);

await server.RunAsync(cts.Token);