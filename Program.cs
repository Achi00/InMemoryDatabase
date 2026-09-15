using InMemoryDatabase.Commands;
using InMemoryDatabase.Commands.Handlers;
using InMemoryDatabase.Exucutors;
using InMemoryDatabase.Servers;
using InMemoryDatabase.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<RespStore>();

builder.Services.AddSingleton<ICommandHandler, SetCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, GetCommandHandler>();


builder.Services.AddSingleton<RespCommandExecutor>();

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

builder.Services.AddSingleton<RespServer>(sp =>
    new RespServer(6380, sp.GetRequiredService<RespCommandExecutor>()));

builder.Services.AddHostedService<RespHostedService>();

var host = builder.Build();
await host.RunAsync();