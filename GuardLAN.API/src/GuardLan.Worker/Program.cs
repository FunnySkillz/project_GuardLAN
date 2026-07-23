using GuardLan.Application;
using GuardLan.Application.Abstractions;
using GuardLan.Infrastructure;
using GuardLan.Worker;
using GuardLan.Worker.Realtime;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddGuardLanApplication();
builder.Services.AddGuardLanInfrastructure(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ILiveUpdatePublisher, ApiWorkerLiveUpdatePublisher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
