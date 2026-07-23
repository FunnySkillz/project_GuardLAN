using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class IntegrationHealthRepository(GuardLanDbContext dbContext)
    : GenericRepository<IntegrationHealth>(dbContext),
      IIntegrationHealthRepository
{
    private bool schemaChecked;

    public async Task<IReadOnlyList<IntegrationHealth>> GetAllOrderedAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);

        return await DbSet
            .AsNoTracking()
            .OrderBy(health => health.Kind)
            .ThenBy(health => health.Source)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IntegrationHealth?> GetBySourceAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);

        return await DbSet
            .FirstOrDefaultAsync(health => health.Source == source, cancellationToken);
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (schemaChecked)
        {
            return;
        }

        await DbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS integration_health (
                "Id" uuid NOT NULL,
                "Source" character varying(96) NOT NULL,
                "Kind" character varying(32) NOT NULL,
                "Status" character varying(32) NOT NULL,
                "SourceEnabled" boolean NOT NULL,
                "SourceAvailable" boolean NOT NULL,
                "LastCheckedUtc" timestamp with time zone NOT NULL,
                "StaleAfterUtc" timestamp with time zone NULL,
                "LastSuccessUtc" timestamp with time zone NULL,
                "LastFailureUtc" timestamp with time zone NULL,
                "RecordsRead" integer NOT NULL,
                "RecordsImported" integer NOT NULL,
                "RecordsRejected" integer NOT NULL,
                "Message" character varying(512) NOT NULL,
                CONSTRAINT "PK_integration_health" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_integration_health_Source"
                ON integration_health ("Source");
            CREATE INDEX IF NOT EXISTS "IX_integration_health_Kind"
                ON integration_health ("Kind");
            CREATE INDEX IF NOT EXISTS "IX_integration_health_Status"
                ON integration_health ("Status");
            CREATE INDEX IF NOT EXISTS "IX_integration_health_LastCheckedUtc"
                ON integration_health ("LastCheckedUtc");

            ALTER TABLE integration_health
                ADD COLUMN IF NOT EXISTS "StaleAfterUtc" timestamp with time zone NULL;
            """,
            cancellationToken);

        schemaChecked = true;
    }
}
