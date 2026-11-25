# Reality Scraper

[![.NET Build](https://github.com/vesnicancz/reality-scraper/actions/workflows/dotnet.yml/badge.svg)](https://github.com/vesnicancz/reality-scraper/actions/workflows/dotnet.yml)

.NET 11 aplikace pro automatické monitorování českých realitních portálů. Sleduje nové nabídky a změny cen na portálech **SReality** a **Reality iDNES** a zasílá e-mailové notifikace.

## Funkce

- **Webové scrapování** – Selenium-based automatizace prohlížeče pro SReality a Reality iDNES
- **Detekce změn** – Porovnání nových dat s databází, sledování nových nabídek a cenové historie
- **E-mailové notifikace** – HTML e-maily generované přes Razor šablony (Resend)
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
Web.Api/            – ASP.NET Core backend, REST endpointy, host Blazor WASM (interaktivní render)
Web.Client/         – Blazor WebAssembly klient
Web.Shared/         – Sdílené DTO a validační modely
```

### Doménové entity

- `Listing` – Realitní nabídka (URL, cena, lokace, metadata)
- `PriceHistory` – Historie změn cen
- `ScraperTask` – Konfigurace naplánované scrapovací úlohy
- `ScraperTaskRecipient` – E-mailoví příjemci notifikací
- `ScraperTaskTarget` – Cílové URL a typ portálu
- `RemovedListingsReportTask` – Konfigurace úlohy reportu vyřazených inzerátů
- `ReportTaskRecipient` – Příjemci reportu vyřazených inzerátů
- `ReportTaskSource` – Zdrojové scraper úlohy zahrnuté do reportu

## Tech stack

| Kategorie | Technologie |
|---|---|
| Runtime | .NET 11, ASP.NET Core 11 |
| Frontend | Blazor WebAssembly, Havit Bootstrap komponenty |
| Databáze | PostgreSQL (Npgsql) |
| ORM | Entity Framework Core 11 |
| Scrapování | Selenium WebDriver 4.54 |
| E-maily | Resend, RazorEngineCore |
| Plánování | Cronos |
| Validace | FluentValidation |
| Logování | Serilog |
| Observabilita | OpenTelemetry (OTLP: trace, metriky, logy) |
| API dokumentace | Scalar (OpenAPI) |
| Testy | xUnit v3, Moq |

## Požadavky

- .NET 11 SDK
- PostgreSQL
- Chrome/Chromium pro Selenium (nebo Selenium Standalone kontejner)
- E-mailová služba – Resend API klíč

## Konfigurace

Konfigurace v `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=reality-scraper;Username=postgres;Password=..."
  },
  "SeleniumSettings": {
    "UseRemoteDriver": true,
    "SeleniumHubUrl": "http://localhost:4444/wd/hub"
  },
  "ResendSettings": {
    "ApiKey": "re_...",
    "FromEmail": "noreply@example.com",
    "FromName": "Reality Scraper"
  }
}
```

## Autentizace (volitelná)

Aplikace podporuje volitelnou autentizaci přes **OpenID Connect** (OIDC). Ve výchozím stavu je vypnutá: v Development běží aplikace bez přihlášení, v ostatních prostředích (včetně Docker image) bez ní odmítne nastartovat, pokud není vědomě nastaveno `Authentication:AllowAnonymous=true`.

Po zapnutí (`"Enabled": true`) se aktivuje:
- OIDC Authorization Code flow s cookie session
- Aplikační API endpointy vyžadují přihlášení (kromě `/health` a `/account/*`)
- Všechny Blazor stránky přesměrují na login
- Endpointy `/account/login`, `/account/logout`, `/account/user-info`

### Konfigurace

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Scopes": ["openid", "profile", "email"],
    "RequireHttpsMetadata": true,
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### Příklady providerů

**Microsoft Entra ID:**
```json
"Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0"
```

**Keycloak:**
```json
"Authority": "https://keycloak.example.com/realms/{realm}"
```

**Další OIDC providery** (Google, Auth0, Okta, …) – stačí nastavit správné `Authority`, `ClientId` a `ClientSecret`.

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

Docker image je dostupný z GitHub Container Registry ve dvou pohyblivých tazích podle prostředí:

```
ghcr.io/vesnicancz/reality-scraper:test     # poslední build (workflow „Build and push image (test)")
ghcr.io/vesnicancz/reality-scraper:stable   # otestovaný build pro produkci
```

Každý build navíc dostane neměnný tag `:sha-<short>` podle commitu. Tok: build → `:test` (+ `:sha-…`); po
otestování workflow „Promote image to stable" přeznačí stejný digest na `:stable` (bez rebuildu). Test
prostředí tedy pulluje `:test`, produkce `:stable`.

Kontejner běží pod neprivilegovaným uživatelem (UID 1654), exponuje port `5000` a obsahuje health check na `/health`.

### Observabilita (OpenTelemetry)

Export je volitelný a zapíná se jen nastavením `OTEL_EXPORTER_OTLP_ENDPOINT`. Bez něj se nic
neregistruje a aplikace se chová jako dřív.

```yaml
environment:
  OTEL_EXPORTER_OTLP_ENDPOINT: http://otel-collector:4317   # gRPC; pro HTTP 4318 + OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
  OTEL_SERVICE_NAME: reality-scraper                       # výchozí hodnota
  OTEL_RESOURCE_ATTRIBUTES: deployment.environment=production
```

- **Trace** – kořenový span `Task <typ>` pro každý běh úlohy ze scheduleru, pod ním `Scrape <portál>`
  (`Open browser`, `Navigate`, `Parse page`), `Process report`, `Send email`, SQL dotazy (Npgsql)
  a odchozí HTTP. Příkazy WebDriveru se z trace filtrují (tisíce spanů na běh), `/health`
  a statické soubory Blazoru také.
- **Logy** – Serilog sink posílá logy přes OTLP s `TraceId`/`SpanId`, konzole i log úlohy v UI zůstávají.
- **Metriky** – vestavěné (ASP.NET Core, HttpClient, Npgsql, `System.Runtime`) a vlastní meter
  `RealityScraper`. V Prometheu (po OTLP překladu názvů):

| Metrika | Tagy | Popis |
|---|---|---|
| `reality_scraper_task_runs_total` | `task_type`, `outcome` | Běhy plánovaných úloh |
| `reality_scraper_task_duration_seconds` | `task_type`, `outcome` | Délka běhu úlohy (histogram) |
| `reality_scraper_scraper_duration_seconds` | `scraper_site`, `outcome` | Délka scrapování jednoho cíle |
| `reality_scraper_scraper_pages_total` | `scraper_site` | Prošlé stránky výpisu |
| `reality_scraper_listings_scraped_total` | `scraper_site`, `listing_result` (`ok`/`failed`/`skipped`) | Zpracované karty inzerátů |
| `reality_scraper_listings_changes_total` | `scraper_site`, `listing_change` (`new`/`price_changed`) | Nové inzeráty a změny cen (po uložení) |
| `reality_scraper_emails_sent_total` | `outcome` | Odeslané e-maily (jeden na příjemce) |

`outcome` je `success`, `failure` nebo `canceled`. Histogramy nesou exemplary s `trace_id`,
takže z grafu v Grafaně jde prokliknout na konkrétní běh v Tempu.

## Testy

```bash
dotnet test
```

Projekt obsahuje 4 testovací projekty:

- `Application.Tests` – Testy handlerů, features, scrapingu a validace
- `Infrastructure.Tests` – Testy služeb a utilit (mailing, plánovač, URL safety)
- `Domain.Tests` – Testy doménových entit
- `IntegrationTests` – Testy EF Core modelu (bez běžící databáze)

## CI/CD

GitHub Actions workflow:

- **Build & Test** – Automaticky při push na `master` a pull requestech
- **Publish Docker** – Manuální dispatch, build a push do GitHub Container Registry
- **Promote** – Manuální dispatch, přeznačení otestovaného image na `:stable`
- **PR Assignment** – Automatické přiřazení autora PR

## Licence

Viz [LICENSE](LICENSE) soubor.
