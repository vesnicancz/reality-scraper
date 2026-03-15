FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 5000

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd -r appuser && useradd -r -g appuser appuser \
    && mkdir -p /app/files && chown -R appuser:appuser /app/files

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY ./publish .

VOLUME ["/app/files"]

USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "RealityScraper.Web.Api.dll"]
