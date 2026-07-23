# GuardLAN API Agent Instructions

When creating or modifying backend API functionality in this folder, follow [docs/BACKEND_ARCHITECTURE.md](docs/BACKEND_ARCHITECTURE.md).

Key defaults:

- Keep controllers thin and route business logic through application services.
- Do not expose EF Core entities through API responses or bind them as request models.
- Keep EF Core access inside Infrastructure.
- Use DTOs, explicit mapping, cancellation tokens, async persistence, and UTC timestamps.
- For new backend endpoints, migrate the touched feature toward the documented Unit of Work and repository structure.
- Do not introduce alternative endpoint architecture patterns without explicit approval.
