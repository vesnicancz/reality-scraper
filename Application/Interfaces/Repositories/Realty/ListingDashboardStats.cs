namespace RealityScraper.Application.Interfaces.Repositories.Realty;

/// <summary>
/// Souhrnná čísla inzerátů pro dashboard. <paramref name="NewCount"/> a <paramref name="RemovedCount"/>
/// se počítají v klouzavém okně předaném do <see cref="IListingRepository.GetDashboardStatsAsync"/>.
/// </summary>
/// <param name="ActiveCount">Aktuálně živé inzeráty napříč všemi úlohami.</param>
/// <param name="NewCount">Inzeráty poprvé zachycené v okně, bez ohledu na to, jestli mezitím zmizely.</param>
/// <param name="RemovedCount">Inzeráty, které jsou vyřazené teď a byly vyřazené v okně. Znovuobjevený
/// inzerát se nezapočítá – <c>RemovedListingDetector</c> mu <c>RemovedAt</c> vynuluje zpět.</param>
/// <param name="PriceDropCount">Inzeráty, jejichž poslední cenová změna v okně byla zlevnění.</param>
public record ListingDashboardStats(int ActiveCount, int NewCount, int RemovedCount, int PriceDropCount);