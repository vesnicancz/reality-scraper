using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping;

public interface IRemovedListingDetector
{
	/// <summary>
	/// Porovná aktivní inzeráty v databázi s inzeráty viděnými v aktuálním běhu scrapu.
	/// Neviděné označí jako vyřazené, viděným aktualizuje LastSeenAt a případně zruší vyřazení.
	/// </summary>
	Task DetectAsync(ScrapingReport report, CancellationToken cancellationToken);
}
