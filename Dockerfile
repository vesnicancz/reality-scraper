# syntax=docker/dockerfile:1

# --- Build stage: restore + publish from source ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy central build/package configuration first so the restore layer is cached
# until one of these (or a .csproj below) actually changes.
COPY global.json Directory.Build.props Directory.Packages.props ./

# Copy every project file in Web.Api's graph so restore resolves the full set.
COPY Application/Application.csproj Application/
COPY Domain/Domain.csproj Domain/
COPY SharedKernel/SharedKernel.csproj SharedKernel/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Web.Api/Web.Api.csproj Web.Api/
COPY Web.Client/Web.Client.csproj Web.Client/
COPY Web.Shared/Web.Shared.csproj Web.Shared/

RUN dotnet restore Web.Api/Web.Api.csproj

# Copy the rest of the sources and publish (also builds the Blazor WASM client).
COPY . .
RUN dotnet publish Web.Api/Web.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000

# The image already ships a non-root user "app" (UID 1654 / $APP_UID); just install
# curl for the healthcheck and hand the writable files volume to that user.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/files && chown -R $APP_UID:$APP_UID /app/files

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

VOLUME ["/app/files"]

USER $APP_UID

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "RealityScraper.Web.Api.dll"]
