# Context: api/ (monorepo) so ProjectReference sibling clients resolve.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY media-api/NuGet.Config media-api/Directory.Build.props ./media-api/
COPY media-api/src/Media.csproj ./media-api/src/
COPY media-api/src/API/Media.API.csproj ./media-api/src/API/
COPY marketing-api/NuGet.Config ./marketing-api/NuGet.Config
COPY marketing-api/clients/ExploraProp.Marketing.Internal.ApiClient/ ./marketing-api/clients/ExploraProp.Marketing.Internal.ApiClient/
WORKDIR /src/media-api
RUN --mount=type=secret,id=nuget_auth_token \
    (dotnet nuget disable source local-artifacts || true) \
 && (dotnet nuget disable source local-nupkgs || true) \
 && dotnet nuget update source github-exploraprop \
    --username github \
    --password "$(cat /run/secrets/nuget_auth_token)" \
    --store-password-in-clear-text \
 && dotnet restore src/API/Media.API.csproj --force-evaluate
COPY media-api/src/ ./src/
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
