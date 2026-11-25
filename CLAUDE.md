# Reality Scraper

Features, configuration and telemetry are described in `README.md`; layers are the top-level folders.

## Language
- UI texts, scraping/report log messages and code comments are in Czech. Validation messages, `Error` descriptions and the logging decorators are in English.
- Commit messages and PR titles are in English, even though older history is in Czech.

## Build and test
- The solution is `RealityScraper.slnx` (no `.sln`): `dotnet build RealityScraper.slnx`, `dotnet test RealityScraper.slnx`.
- Tests run on Microsoft.Testing.Platform. Single class: `dotnet test Application.Tests/Application.Tests.csproj --filter-class "*PriceParserTests"`.
- No test needs a database, Selenium or Docker, including `IntegrationTests` (it only checks the EF model).
- A build failing with MSB3027/MSB3021 means a running `RealityScraper.Web.Api` locks the DLLs (it also holds port 5000). Stop that process.

## Running locally
- Needs PostgreSQL matching the connection string in `Web.Api/appsettings.Development.json` (gitignored, contains real secrets: never print or commit it).
- Without login: `ASPNETCORE_ENVIRONMENT=Development Authentication__Enabled=false ASPNETCORE_URLS=http://localhost:5000 dotnet run --project Web.Api --no-launch-profile`.
- Startup applies pending EF migrations to the configured database.
- The scraping pipeline needs a Selenium hub on `localhost:4444`; without it, verify scraping by unit tests.

## EF migrations
- `dotnet ef migrations add <Name> -p Infrastructure -s Web.Api`, never with `--no-build`: EF would load a stale Infrastructure.dll from Web.Api's output and silently generate an empty migration.
- `migrations remove` needs a running database. Without one, delete an unapplied migration's two files and restore `RealityDbContextModelSnapshot.cs` to its previous state.

## Conventions
- Command, query and domain event handlers, validators and `IEndpoint` implementations are registered by assembly scanning. Repositories too, if they implement `IRepository<>` and are named after their interface (`FooRepository` : `IFooRepository`). Scrapers and `IScheduledJob`s are registered by hand in `ApplicationServiceRegistration.cs`.
- Handlers return `Result`/`Result<T>`; endpoints map failures with `CustomResults.Problem`. Application DTOs are mapped to `Web.Shared` models in `Web.Api/Mappers`.
- Time comes from `IDateTimeProvider`, not `TimeProvider` or `DateTime.UtcNow`.
- Loaded entities are tracked: change them and call `IUnitOfWork.SaveChangesAsync`, don't call `Update`. Domain events are dispatched after the save, and a failing handler is only logged.
- `Web.Client` must not reference `Domain`. `ScraperType` (Web.Shared) therefore duplicates `ScrapersEnum` and is cast by its int value; change both together.
- Any request to a user-supplied or scraped URL must pass `IUrlSafetyValidator` first (SSRF).
- Client services injected into components (e.g. the `AddHx*` ones) must be registered in both `Web.Client/Program.cs` and `Web.Api/DependencyInjection.cs`; a missing server registration causes HTTP 500.
- Tests use plain xUnit `Assert` and Moq, named `Method_Behavior`.

## Pitfalls
- `Dockerfile`: keep the single `COPY . .` followed by `dotnet publish`. A csproj-only restore layer drops `_framework/blazor.web.js` from the output. To speed the build up, use BuildKit/GHA cache or a NuGet cache mount.
- E-mail templates (`Infrastructure/Utilities/Mailing/Templates`): the listing photo stays 160×120 px (cell `width="176"`), it is what recipients orient by. The shared CSS block must be identical in both templates (guarded by a test).
- The UI palette is the CSS variables in `Web.Api/wwwroot/app.css`, plus the sidebar gradient in `Web.Client/Layout/NavMenu.razor.css`.
