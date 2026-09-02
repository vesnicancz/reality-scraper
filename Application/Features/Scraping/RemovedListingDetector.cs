using Microsoft.Extensions.Logging;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Scraping;

public class RemovedListingDetector : IRemovedListingDetector
{
	private readonly IListingRepository listingRepository;
	private readonly IDateTimeProvider dateTimeProvider;
	private readonly ILogger<RemovedListingDetector> logger;

	public RemovedListingDetector(
		IListingRepository listingRepository,
		IDateTimeProvider dateTimeProvider,
		ILogger<RemovedListingDetector> logger)
	{
		this.listingRepository = listingRepository;
		this.dateTimeProvider = dateTimeProvider;
		this.logger = logger;
	}

	public async Task DetectAsync(ScrapingReport report, CancellationToken cancellationToken)
	{
		var listings = await listingRepository.GetByScraperTaskIdAsync(report.ScraperTaskId, cancellationToken);
		var now = dateTimeProvider.UtcNow;

		// Okamžik minulého běhu = nejvyšší LastSeenAt v úloze PŘED aktualizací. Každý běh razítkuje
		// všem viděným inzerátům stejnou hodnotu, takže maximum je čas minulého běhu. Díky tomu jde
		// odlišit "chybí poprvé" od "chybí opakovaně", aniž by se ukládal další stav.
		var previousRunAt = listings.Count > 0 ? listings.Max(l => l.LastSeenAt) : now;

		var reappearedCount = 0;

		// Viděný inzerát prokazatelně existuje, takže LastSeenAt (a případný návrat
		// vyřazeného) se aktualizuje i při částečném selhání scrapování.
		foreach (var listing in listings)
		{
			if (report.SeenListings.ContainsKey(listing.ExternalId))
			{
				listing.LastSeenAt = now;
				if (listing.RemovedAt != null)
				{
					listing.RemovedAt = null;
					reappearedCount++;
					logger.LogInformation("Inzerát {ExternalId} se znovu objevil.", listing.ExternalId);
				}
			}
		}

		if (!report.ScrapingSucceeded)
		{
			logger.LogWarning("Scrapování úlohy '{TaskName}' neproběhlo celé úspěšně, detekce vyřazených inzerátů se přeskakuje.", report.TaskName);
			return;
		}

		var activeListings = listings.Where(l => l.RemovedAt == null).ToList();

		// Ochrana proti planému poplachu: úspěšný běh s nula inzeráty při neprázdné DB spíš znamená
		// rozbité selektory než skutečně prázdné portály - není s čím porovnávat.
		if (activeListings.Count > 0 && report.SeenListings.Count == 0)
		{
			logger.LogWarning("Scrapování úlohy '{TaskName}' nevrátilo žádné inzeráty, ale v databázi je {Count} aktivních. Detekce vyřazených se přeskakuje.", report.TaskName, activeListings.Count);
			return;
		}

		// Dílčí anomálie (selhaná karta, karta bez ID, prázdný cíl nebo portál) neznamenají, že jsou
		// data nepoužitelná - jen že inzerát mohl v tomhle běhu chybět, aniž by zmizel z portálu.
		// Dřív kterákoli z nich vypnula detekci pro celou úlohu, takže jediný prázdný cíl (úzký filtr,
		// malá obec) umlčel vyřazování i pro stovky inzerátů z ostatních cílů, klidně na týdny.
		// Nově se místo toho zapne opatrný režim: vyřadí se jen inzerát, který chyběl i v minulém běhu.
		var anomalies = CollectAnomalies(report);
		var cautious = anomalies.Count > 0;

		if (cautious)
		{
			logger.LogWarning("Scrapování úlohy '{TaskName}': {Anomalies}. Opatrný režim - vyřadí se jen inzeráty chybějící i v běhu z {PreviousRunAt}.",
				report.TaskName, string.Join("; ", anomalies), previousRunAt);
		}

		var removedCount = 0;
		var postponedCount = 0;

		foreach (var listing in listings)
		{
			if (report.SeenListings.ContainsKey(listing.ExternalId) || listing.RemovedAt != null)
			{
				continue;
			}

			// Ještě v minulém běhu tam byl - jednorázový výpadek nesmí vyřadit živý inzerát.
			if (cautious && listing.LastSeenAt >= previousRunAt)
			{
				postponedCount++;
				continue;
			}

			listing.RemovedAt = now;
			removedCount++;
			logger.LogInformation("Inzerát {ExternalId} ('{Title}') byl vyřazen.", listing.ExternalId, listing.Title);
		}

		if (removedCount > 0 || reappearedCount > 0 || postponedCount > 0)
		{
			logger.LogInformation("Úloha '{TaskName}': {RemovedCount} inzerátů vyřazeno, {ReappearedCount} se znovu objevilo, {PostponedCount} odloženo na další běh.",
				report.TaskName, removedCount, reappearedCount, postponedCount);
		}
	}

	/// <summary>
	/// Důvody, proč nemusí být seznam viděných inzerátů úplný. Nejde o chyby, které by data
	/// znehodnotily - jen o situace, kdy chybějící inzerát nemusí znamenat vyřazení.
	/// </summary>
	private static List<string> CollectAnomalies(ScrapingReport report)
	{
		var anomalies = new List<string>();

		if (report.FailedListingsCount > 0)
		{
			anomalies.Add($"{report.FailedListingsCount} inzerátů se nepodařilo zpracovat");
		}

		// Reklamní bloky a developerské projekty ve výpisu nemají detail s ID, takže se přeskakují
		// při každém běhu. Do databáze se nikdy nedostanou, a co v ní není, nemůže vypadat jako
		// vyřazené - jejich stálý podíl (na SReality jednotky procent) proto není důvod k opatrnosti.
		// Podezřelý je až nepoměr: kdyby portál změnil tvar URL detailu, přestala by projít většina
		// karet a živé inzeráty by vypadaly jako vyřazené.
		var scannedCount = report.TotalListingsCount + report.SkippedListingsCount;
		if (scannedCount > 0 && report.SkippedListingsCount * 3 > scannedCount)
		{
			anomalies.Add($"{report.SkippedListingsCount} z {scannedCount} karet nemá použitelné ID");
		}

		var emptyPortals = report.Results.Where(r => r.TotalListingsCount == 0).Select(r => r.SiteName).ToList();
		if (emptyPortals.Count > 0)
		{
			anomalies.Add($"portály {string.Join(", ", emptyPortals)} nevrátily žádný inzerát");
		}

		// Více cílů téhož portálu se agreguje do jednoho PortalReportu - prázdný cíl tak kontrola
		// prázdných portálů nezachytí.
		if (report.AnyTargetEmpty)
		{
			anomalies.Add("některý cíl nevrátil žádný inzerát");
		}

		return anomalies;
	}
}
