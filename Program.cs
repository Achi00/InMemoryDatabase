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
builder.Services.AddSingleton<ICommandHandler, DelCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, ExistsCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, PingCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, ExpireCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, TtlCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, IncrCommandHandler>();
builder.Services.AddSingleton<ICommandHandler, DecrCommandHandler>();


builder.Services.AddSingleton<RespCommandExecutor>();

builder.Services.AddSingleton<RespServer>(sp =>
    new RespServer(6380, sp.GetRequiredService<RespCommandExecutor>()));

builder.Services.AddHostedService<RespHostedService>();

var host = builder.Build();
await host.RunAsync();