# Reality Scraper

Reality Scraper je .NET Worker Service aplikace, která automaticky monitoruje realitní nabídky z několika českých realitních portálů (jako SReality a Reality iDNES), sleduje změny a zasílá e-mailové notifikace o nových nabídkách a změnách cen.

## Funkce

- **Automatizované webové scrapování**: Pravidelně prochází realitní weby a sbírá nové nabídky
- **Monitorování změn cen**: Detekuje a sleduje změny cen u existujících nabídek
- **E-mailové notifikace**: Zasílá krásně formátované HTML e-maily s aktualizacemi
- **Plánované spouštění**: Používá cron výrazy k plánování úloh scrapování
- **Databázové úložiště**: Ukládá data o nabídkách do SQLite databáze
- **Stahování obrázků**: Automaticky stahuje a ukládá obrázky nabídek

## Jak to funguje

Aplikace běží jako background service, která spouští úlohy scrapování na základě konfigurovatelného plánu.
Porovnává nově scrapované nabídky s těmi, které jsou již v databázi, aby detekovala nové nabídky a změny cen.
Když jsou zjištěny změny, zasílá e-mailové notifikace konfigurovaným příjemcům.
Pro vyhledávání nabídek používá Selenium Standalone(https://hub.docker.com/r/selenium/standalone-chrome) v docker containeru.

### Hlavní komponenty

1. **Scheduler Service**: Spravuje naplánované úlohy pomocí cron výrazů
2. **Scraper Services**: Implementace specifické pro jednotlivé realitní portály
3. **Databázová vrstva**: Ukládá a spravuje data o nabídkách
4. **E-mail Service**: Generuje a odesílá HTML e-mailové notifikace

## Konfigurace

Aplikace je konfigurována prostřednictvím souboru `appsettings.json`. Zde je ukázka konfigurace:

```json
{
  "SchedulerSettings": {
	"Tasks": [
	  {
		"Name": "Reality Praha+10",
		"CronExpression": "0 0,12 * * *",
		"Enabled": true,
		"ScrapingConfiguration": {
		  "EmailRecipients": [
			"your-email@example.com"
		  ],
		  "Scrapers": [
			{
			  "ScraperType": "SReality",
			  "Url": "https://www.sreality.cz/hledani/prodej/domy/praha"
			},
			{
			  "ScraperType": "RealityIdnes",
			  "Url": "https://reality.idnes.cz/s/prodej/domy/praha/?s-rd=4"
			}
		  ]
		}
	  }
	]
  }
}
```

## Požadavky

- .NET 9
- Selenium WebDriver
- Chrome nebo kompatibilní prohlížeč pro webové scrapování
- SendGrid účet pro e-mailové notifikace (volitelné)

## Instalace a použití

1. Naklonujte repozitář
2. Nakonfigurujte `appsettings.json` s vašimi nastaveními
3. Spusťte aplikaci pomocí `dotnet run` nebo ji nasaďte jako službu

Aplikace bude spuštěna jako background service, bude provádět úlohy scrapování na základě nakonfigurovaného plánu a zasílat e-mailové notifikace při zjištění nových nabídek nebo změn cen.
