using GuardLan.Infrastructure;
using GuardLan.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddGuardLanInfrastructure(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
