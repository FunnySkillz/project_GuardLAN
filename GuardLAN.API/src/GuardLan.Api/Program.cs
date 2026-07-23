using GuardLan.Application;
using GuardLan.Infrastructure;
using GuardLan.Infrastructure.Persistence;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("GuardLanWeb", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
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

app.UseHttpsRedirection();
app.UseCors("GuardLanWeb");
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new
{
    Service = "GuardLAN API",
    Status = "ok",
    Utc = DateTime.UtcNow
}));

app.MapControllers();

app.Run();
