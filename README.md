# media-api

Owns media files, variants, upload policies, and internal media URL reads for sibling services.

| Item | Value |
|------|--------|
| Image | `ghcr.io/exploraprop/media-api` |
| NuGet | SharedKernel / IntegrationEvents / Architecture / **API.Scripts** from GitHub Packages (ExploraProp) |
| Public SDK | [csharp_media_sdk](https://github.com/ExploraProp/csharp_media_sdk) (`ExploraProp.Media.ApiClient`) |
| Internal SDK | [csharp_media_internal_sdk](https://github.com/ExploraProp/csharp_media_internal_sdk) (`ExploraProp.Media.Internal.ApiClient`) |
| Contracts | [openapi-contracts](https://github.com/ExploraProp/openapi-contracts) / [openapi-contracts-internal](https://github.com/ExploraProp/openapi-contracts-internal) |
| CI | In-tree `.github/workflows/` (secrets wired per BC) |

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| .NET 10 SDK | Matches CI (`10.0.x`) |
| Docker | Local compose and SQL Server |
| PowerShell 7+ | Scripts and `api-scripts` on Windows |
| `NUGET_AUTH_TOKEN` | GitHub Packages access token |

## Repository layout

```text
src/API/              ASP.NET host, controllers, Dockerfile, docker-compose, .env
src/Application/      Upload, scan, and variant use cases
src/Domain/           Media files, policies, variants, errors
src/Infrastructure/   EF, storage/scanning integrations, migrations, schema docs
tests/Unit/           Unit tests
tests/Functional/     Reqnroll + mesh functional tests
scripts/seeding/      Canonical seed-database.sql
clients/              Ephemeral Kiota client (gitignored)
docs/                 ERROR_CATALOG.md, errors.html
.config/              Local dotnet tools
```

## API surface

| Surface | Base route | Consumers |
|---------|------------|-----------|
| Public | `api/v1/media` | Apps / public SDK |
| Internal | `api/v1/internal/media` | Sibling BCs via internal SDK |

- Host `7005` -> container `80`
- Health: `/health/ready`

## Local development

```powershell
Copy-Item src/API/.env.example src/API/.env
dotnet tool run api-scripts -- secrets-local --service Media
dotnet restore Media.slnx
dotnet build Media.slnx -c Release
dotnet test tests/Unit/Media.Unit.csproj -c Release
```

Run from `src/API`:

```powershell
docker compose up --build
```

Fill Firebase storage, ClamAV, and watermark settings in `src/API/.env`. After the API is healthy, seed media policies:

```powershell
dotnet tool run api-scripts -- seed --service .
```

## Tooling (API.Scripts)

```powershell
dotnet tool restore
dotnet tool run api-scripts -- secrets-local --service Media
dotnet tool run api-scripts -- generate-openapi --service Media
dotnet tool run api-scripts -- generate-client --service Media
dotnet tool run api-scripts -- seed --service .
```

## Testing

```powershell
dotnet test tests/Unit/Media.Unit.csproj -c Release
```

```powershell
$env:SIBLING_IMAGE_TAG = "main"
docker login ghcr.io
dotnet tool run api-scripts -- test-functional --service-root .
Remove-Item Env:SIBLING_IMAGE_TAG
```

## OpenAPI, contracts & clients

`publish-openapi.yml` publishes public and internal contracts; SDK workflows consume those contracts to publish Kiota packages. Regenerate locally with `generate-openapi` and `generate-client`.

## Error catalog

- Catalog: [docs/ERROR_CATALOG.md](docs/ERROR_CATALOG.md) and `docs/errors.html`
- Regenerate after error changes:

```powershell
dotnet tool run error-catalog -- --service-root . --md --html
```

## Seeding

| Path | Role |
|------|------|
| `scripts/seeding/seed-database.sql` | Canonical media policy seed |
| `src/Infrastructure/Schema/README.md` | SQL schema overview for the Media database |

## CI & publishing

| Workflow | Purpose |
|----------|---------|
| `tests.yml` | Unit, functional, coverage, SonarCloud, GHCR image publish |
| `publish-openapi.yml` | OpenAPI -> contracts repos |
| `configure-api-env.yml` | Manual env/secrets validation |
| `snyk.yml` | Open Source scan |
| Dependabot | NuGet / Actions updates |

## Related repos

| Repo | Role |
|------|------|
| [packages](https://github.com/ExploraProp/packages) | Shared packages |
| [api-scripts](https://github.com/ExploraProp/api-scripts) | `api-scripts` CLI |
| [openapi-contracts](https://github.com/ExploraProp/openapi-contracts) | Public OpenAPI |
| [openapi-contracts-internal](https://github.com/ExploraProp/openapi-contracts-internal) | Internal OpenAPI |

## Troubleshooting

| Symptom | Likely fix |
|---------|------------|
| Upload succeeds but file never becomes public | Check ClamAV and scan callback settings |
| Storage writes fail | Verify Firebase credentials and bucket config in `src/API/.env` |
| Seed does not create media policies | Run `api-scripts seed` after migrations |
