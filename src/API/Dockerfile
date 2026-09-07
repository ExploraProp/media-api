# Context: this service folder only (services/Media). SharedKernel / IntegrationEvents from NuGet.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY NuGet.Config Directory.Build.props ./
COPY src/Media.csproj ./src/
COPY src/API/Media.API.csproj ./src/API/
RUN --mount=type=secret,id=nuget_auth_token \
    (dotnet nuget disable source local-artifacts || true) \
 && (dotnet nuget disable source local-nupkgs || true) \
 && dotnet nuget update source github-exploraprop \
    --username github \
    --password "$(cat /run/secrets/nuget_auth_token)" \
    --store-password-in-clear-text \
 && dotnet restore src/API/Media.API.csproj --force-evaluate
COPY src/ ./src/
RUN dotnet publish src/API/Media.API.csproj -c Release \
    -o /app/publish \
    -p:OpenApiGenerateDocumentsOnBuild=false \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN chown -R app:app /app
USER app
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 80
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=40s CMD curl -f http://localhost/health/ready || exit 1
ENTRYPOINT ["dotnet", "Media.API.dll"]
