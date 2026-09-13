using InMemoryDatabase.Servers;

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var server = new RespServer(6380);

await server.RunAsync(cts.Token);

