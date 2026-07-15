namespace RealityScraper.Application.Features.Scraping.Model;

/// <summary>
/// Výsledek běhu scraperu včetně příznaku, zda proběhl bez chyby.
/// Neúspěšný běh může obsahovat částečný seznam inzerátů.
/// </summary>
public record ScraperRunResult(bool Success, List<ScraperListingItem> Listings);
