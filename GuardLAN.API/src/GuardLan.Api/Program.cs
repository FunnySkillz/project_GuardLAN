using GuardLan.Api.Hubs;
using GuardLan.Api.Realtime;
using GuardLan.Application;
using GuardLan.Application.Abstractions;
using GuardLan.Infrastructure;
using GuardLan.Infrastructure.Persistence;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var enableHttpsRedirection = builder.Configuration.GetValue("HttpsRedirection:Enabled", false);
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(origin => origin.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!)
    .ToArray();

if (allowedOrigins.Length == 0)
{
    allowedOrigins = ["http://localhost:4200"];
}

builder.Services.AddGuardLanApplication();
builder.Services.AddGuardLanInfrastructure(builder.Configuration);
builder.Services.AddSignalR();
builder.Services.AddScoped<ILiveUpdatePublisher, SignalRLiveUpdatePublisher>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("GuardLanWeb", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GuardLanDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GuardLan.DatabaseSeeder");

        await DatabaseSeeder.InitializeAsync(dbContext, logger);
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(
            exception,
            "GuardLAN database initialization skipped. Start PostgreSQL with docker compose up -d postgres.");
    }
}

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("GuardLanWeb");
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(CreateHealthResponse()));
app.MapGet("/api/health", () => Results.Ok(CreateHealthResponse()));

app.MapControllers();
app.MapHub<GuardLanHub>("/hubs/guardlan");

app.Run();

static object CreateHealthResponse()
{
    return new
    {
        Service = "GuardLAN API",
        Status = "ok",
        Utc = DateTime.UtcNow
    };
}
