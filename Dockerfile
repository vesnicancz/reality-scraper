FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx Directory.Build.props Directory.Packages.props ./
COPY SharedKernel/*.csproj SharedKernel/
COPY Domain/*.csproj Domain/
COPY Application/*.csproj Application/
COPY Infrastructure/*.csproj Infrastructure/
COPY Web.Api/*.csproj Web.Api/
COPY Web.Client/*.csproj Web.Client/
COPY Web.Shared/*.csproj Web.Shared/
COPY Application.Tests/*.csproj Application.Tests/
COPY Domain.Tests/*.csproj Domain.Tests/
COPY Infrastructure.Tests/*.csproj Infrastructure.Tests/
COPY IntegrationTests/*.csproj IntegrationTests/
RUN dotnet restore

COPY . .
RUN dotnet publish Web.Api/Web.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 5000

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd -r appuser && useradd -r -g appuser appuser \
    && mkdir -p /app/files && chown -R appuser:appuser /app/files

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

VOLUME ["/app/files"]

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "RealityScraper.Web.Api.dll"]
