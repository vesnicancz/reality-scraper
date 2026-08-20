using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Interfaces.Repositories.Realty;

/// <summary>
/// Zlevněný inzerát i s cenou, ze které se zlevnilo. Aktuální cena je na <see cref="Listing.Price"/>,
/// okamžik zlevnění na <see cref="Listing.PriceFrom"/>.
/// </summary>
public record ListingPriceDrop(Listing Listing, decimal PreviousPrice);