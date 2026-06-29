# syntax=docker/dockerfile:1

# --- Build stage: publish from source ---
# Note: a single COPY + publish is intentional. Splitting into a csproj-only
# restore layer (for caching) breaks Blazor static web asset composition and
# drops _framework/blazor.web.js from the published output.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Web.Api/Web.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

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
