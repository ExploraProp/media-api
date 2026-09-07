# media-api

Extracted **Media** bounded-context API from [InmoBackend](https://github.com/ExploraProp/InmoBackend).

| Item | Value |
|------|--------|
| Image | `ghcr.io/exploraprop/media-api` |
| NuGet | SharedKernel / IntegrationEvents / **API.Scripts** from GitHub Packages (ExploraProp) |
| CI | In-tree `.github/workflows/tests.yml` (secrets wired per BC) |

## Local build

```powershell
dotnet restore Media.slnx
dotnet build Media.slnx -c Release
dotnet test tests/Unit/Media.Unit.csproj -c Release
```

## Tooling (API.Scripts)

```powershell
dotnet tool restore
dotnet tool run api-scripts -- secrets-local --service Media
dotnet tool run api-scripts -- generate-openapi --service Media
# After migrations, if scripts/seeding/seed-database.sql exists:
dotnet tool run api-scripts -- seed --service .
```

Functional (registry siblings — required for meshes with siblings):

```powershell
$env:SIBLING_IMAGE_TAG = "main"
docker login ghcr.io
dotnet test tests/Functional/Media.Functional.csproj -c Release
Remove-Item Env:SIBLING_IMAGE_TAG
```

See [bc-repo-runbook.md](https://github.com/ExploraProp/InmoBackend/blob/main/docs/extraction/bc-repo-runbook.md).
