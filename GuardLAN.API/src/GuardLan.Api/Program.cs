using GuardLan.Api.Auth;
using GuardLan.Api.Hubs;
using GuardLan.Api.Realtime;
using GuardLan.Application;
using GuardLan.Application.Abstractions;
using GuardLan.Infrastructure;
using GuardLan.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
builder.Services.Configure<GuardLanAuthOptions>(
    builder.Configuration.GetSection(GuardLanAuthOptions.SectionName));
builder.Services.AddSingleton<LocalUserAuthenticator>();
builder.Services.AddSingleton<InternalPublisherKeyValidator>();
builder.Services.AddSignalR();
builder.Services.AddScoped<ILiveUpdatePublisher, SignalRLiveUpdatePublisher>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        var authOptions = builder.Configuration
            .GetSection(GuardLanAuthOptions.SectionName)
            .Get<GuardLanAuthOptions>() ?? new GuardLanAuthOptions();
        var sessionHours = Math.Clamp(authOptions.SessionHours, 1, 24);

        options.Cookie.Name = string.IsNullOrWhiteSpace(authOptions.CookieName)
            ? "GuardLAN.Session"
            : authOptions.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = authOptions.RequireSecureCookies || enableHttpsRedirection
            ? CookieSecurePolicy.Always
            : CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(sessionHours);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
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
            "GuardLAN database initialization skipped. Start PostgreSQL with docker compose up -d database " +
            "from the repository root, or docker compose up -d postgres from GuardLAN.API.");
    }
}

if (enableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseCors("GuardLanWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(CreateHealthResponse())).AllowAnonymous();
app.MapGet("/api/health", () => Results.Ok(CreateHealthResponse())).AllowAnonymous();

app.MapControllers();
app.MapHub<GuardLanHub>("/hubs/guardlan").RequireAuthorization();

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
