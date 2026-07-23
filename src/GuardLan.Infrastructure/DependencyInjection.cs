using GuardLan.Application.Abstractions;
using GuardLan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GuardLan.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=guardlan;Username=guardlan;Password=guardlan";

    public static IServiceCollection AddGuardLanInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("GuardLanDb") ?? DefaultConnectionString;

        services.AddDbContext<GuardLanDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<IGuardLanRepository, GuardLanRepository>();

        return services;
    }
}
