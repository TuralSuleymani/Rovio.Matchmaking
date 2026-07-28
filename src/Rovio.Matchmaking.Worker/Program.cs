using Rovio.Matchmaking.Infrastructure;
using Rovio.Matchmaking.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMatchmakingInfrastructure(builder.Configuration);
builder.Services.AddHostedService<MatchmakingWorker>();

var host = builder.Build();
await host.RunAsync();
