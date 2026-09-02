namespace RealityScraper.Application.Features.Scraping.Model;

/// <summary>
/// Výsledek běhu scraperu včetně příznaku, zda proběhl bez chyby.
/// Neúspěšný běh může obsahovat částečný seznam inzerátů.
/// </summary>
/// <param name="FailedListingsCount">Karty, které shodily zpracování (selhaný selektor).</param>
/// <param name="SkippedListingsCount">Karty vynechané kvůli nepoužitelnému ID (chybějící nebo
/// neparsovatelný odkaz na detail) - typicky reklamní bloky vsunuté mezi inzeráty a developerské
/// projekty. Nejsou to chyby ani inzeráty; slouží k odhalení nepoměru, který by znamenal, že
/// portál změnil tvar URL detailu a přestaly procházet i skutečné inzeráty.</param>
public record ScraperRunResult(bool Success, List<ScraperListingItem> Listings, int FailedListingsCount = 0, int SkippedListingsCount = 0);
