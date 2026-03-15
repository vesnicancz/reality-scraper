# Reality Scraper

[![.NET Build](https://github.com/vesnicancz/reality-scraper/actions/workflows/dotnet.yml/badge.svg)](https://github.com/vesnicancz/reality-scraper/actions/workflows/dotnet.yml)

.NET 10 aplikace pro automatické monitorování českých realitních portálů. Sleduje nové nabídky a změny cen na portálech **SReality** a **Reality iDNES** a zasílá e-mailové notifikace.

## Funkce

- **Webové scrapování** – Selenium-based automatizace prohlížeče pro SReality a Reality iDNES
- **Detekce změn** – Porovnání nových dat s databází, sledování nových nabídek a cenové historie
- **E-mailové notifikace** – HTML e-maily generované přes Razor šablony (Resend / SMTP)
- **Plánování úloh** – Cron výrazy pro pravidelné spouštění scrapování
- **REST API + Web UI** – Správa úloh přes API a Blazor WebAssembly dashboard
- **Stahování obrázků** – Automatické ukládání obrázků z nabídek
- **Health check** – Endpoint `/health` pro monitoring

## Architektura

Projekt využívá **Clean Architecture** s oddělením vrstev:

```
SharedKernel/       – Bázové třídy (Entity, AggregateRoot, Result<T>, IDomainEvent)
Domain/             – Doménové entity, enumy, události
Application/        – CQRS handlery (commands, queries), validace (FluentValidation)
Infrastructure/     – EF Core repositáře, Selenium services, e-mail, scheduler
Web.Api/            – ASP.NET Core backend, REST endpointy, Blazor server
Web.Client/         – Blazor WebAssembly klient
Web.Shared/         – Sdílené DTO a validační modely
```

### Doménové entity

- `Listing` – Realitní nabídka (URL, cena, lokace, metadata)
- `PriceHistory` – Historie změn cen
- `ScraperTask` – Konfigurace naplánované úlohy
- `ScraperTaskRecipient` – E-mailoví příjemci notifikací
- `ScraperTaskTarget` – Cílové URL a typ portálu

## Tech stack

| Kategorie | Technologie |
|---|---|
| Runtime | .NET 10, ASP.NET Core 10 |
| Frontend | Blazor WebAssembly, Havit Bootstrap komponenty |
| Databáze | PostgreSQL (Npgsql), SQLite (alternativa) |
| ORM | Entity Framework Core 10 |
| Scrapování | Selenium WebDriver 4.41 |
| E-maily | Resend, SendGrid (alternativa), RazorEngineCore |
| Plánování | Cronos |
| Validace | FluentValidation |
| Logování | Serilog |
| API dokumentace | Scalar (OpenAPI) |
| Testy | xUnit v3, Moq, FluentAssertions |

## Požadavky

- .NET 10 SDK
- PostgreSQL (nebo SQLite)
- Chrome/Chromium pro Selenium (nebo Selenium Standalone kontejner)
- E-mailová služba – Resend API klíč nebo SMTP server

## Konfigurace

Konfigurace v `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=reality-scraper;Username=postgres;Password=..."
  },
  "SchedulerSettings": {
    "Tasks": [
      {
        "Name": "Reality Praha",
        "CronExpression": "0 0,12 * * *",
        "Enabled": true,
        "ScrapingConfiguration": {
          "EmailRecipients": ["vas-email@example.com"],
          "Scrapers": [
            {
              "ScraperType": "SReality",
              "Url": "https://www.sreality.cz/hledani/prodej/domy/praha"
            },
            {
              "ScraperType": "RealityIdnes",
              "Url": "https://reality.idnes.cz/s/prodej/domy/praha/"
            }
          ]
        }
      }
    ]
  },
  "SeleniumSettings": {
    "UseRemoteDriver": true,
    "RemoteDriverUrl": "http://localhost:4444/wd/hub"
  },
  "ResendSettings": {
    "ApiKey": "re_...",
    "FromEmail": "noreply@example.com",
    "FromName": "Reality Scraper"
  }
}
```

## Spuštění

### Lokální vývoj

```bash
dotnet restore
dotnet run --project Web.Api
```

Aplikace poběží na výchozím portu. V development módu je dostupná OpenAPI dokumentace přes Scalar.

### Docker

```bash
docker build -t reality-scraper .
docker run -p 5000:5000 \
  -v reality-data:/app/files \
  -e ConnectionStrings__DefaultConnection="Host=..." \
  -e ResendSettings__ApiKey="re_..." \
  reality-scraper
```

Docker image je dostupný i z GitHub Container Registry:

```
ghcr.io/vesnicancz/reality-scraper:latest
```

Kontejner běží pod neprivilegovaným uživatelem, exponuje port `5000` a obsahuje health check.

## Testy

```bash
dotnet test
```

Projekt obsahuje 4 testovací projekty:

- `Application.Tests` – Testy handlerů a features
- `Domain.Tests` – Testy doménových entit
- `Infrastructure.Tests` – Testy služeb a repositářů
- `IntegrationTests` – Integrační testy

## CI/CD

GitHub Actions workflow:

- **Build & Test** – Automaticky při push na `master` a pull requestech
- **Publish Docker** – Manuální dispatch, build a push do GitHub Container Registry
- **PR Assignment** – Automatické přiřazení autora PR

## Licence

Viz [LICENSE](LICENSE) soubor.
