using GuardLan.Domain.Entities;
using GuardLan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GuardLan.Infrastructure.Persistence.Repositories;

public sealed class IntegrationImportRunRepository(GuardLanDbContext dbContext)
    : GenericRepository<IntegrationImportRun>(dbContext),
      IIntegrationImportRunRepository
{
    private bool schemaChecked;

    public async Task<IReadOnlyList<IntegrationImportRun>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);

        return await DbSet
            .AsNoTracking()
            .OrderByDescending(run => run.CompletedUtc)
            .Take(Math.Clamp(limit, 1, 100))
            .ToArrayAsync(cancellationToken);
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        if (schemaChecked)
        {
            return;
        }

        await DbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS integration_import_runs (
                "Id" uuid NOT NULL,
                "Source" character varying(96) NOT NULL,
                "Kind" character varying(32) NOT NULL,
                "Status" character varying(32) NOT NULL,
                "SourceEnabled" boolean NOT NULL,
                "SourceAvailable" boolean NOT NULL,
                "CompletedUtc" timestamp with time zone NOT NULL,
                "RecordsRead" integer NOT NULL,
                "RecordsImported" integer NOT NULL,
                "RecordsRejected" integer NOT NULL,
                "Message" character varying(512) NOT NULL,
                CONSTRAINT "PK_integration_import_runs" PRIMARY KEY ("Id")
            );

            CREATE INDEX IF NOT EXISTS "IX_integration_import_runs_Source"
                ON integration_import_runs ("Source");
            CREATE INDEX IF NOT EXISTS "IX_integration_import_runs_Kind"
                ON integration_import_runs ("Kind");
            CREATE INDEX IF NOT EXISTS "IX_integration_import_runs_Status"
                ON integration_import_runs ("Status");
            CREATE INDEX IF NOT EXISTS "IX_integration_import_runs_CompletedUtc"
                ON integration_import_runs ("CompletedUtc");
            """,
            cancellationToken);

        schemaChecked = true;
    }
}
