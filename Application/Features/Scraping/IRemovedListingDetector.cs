using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping;

public interface IRemovedListingDetector
{
	/// <summary>
	/// Porovná aktivní inzeráty v databázi s inzeráty viděnými v aktuálním běhu scrapu.
	/// Neviděné označí jako vyřazené, viděným aktualizuje LastSeenAt a případně zruší vyřazení.
	/// </summary>
	/// <remarks>
	/// Detekce se přeskočí jen tehdy, když jsou data prokazatelně nepoužitelná - scraper selhal,
	/// nebo běh nevrátil vůbec nic, přestože v databázi jsou aktivní inzeráty. Dílčí anomálie
	/// (selhaná karta, karta bez ID, prázdný cíl či portál) detekci nevypínají, jen zapnou opatrný
	/// režim, ve kterém se vyřadí pouze inzeráty chybějící i v předchozím běhu.
	/// </remarks>
	Task DetectAsync(ScrapingReport report, CancellationToken cancellationToken);
}
