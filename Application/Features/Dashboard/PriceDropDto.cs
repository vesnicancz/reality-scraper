using RealityScraper.Application.Features.Listings;

namespace RealityScraper.Application.Features.Dashboard;

public class PriceDropDto
{
	public ListingDto Listing { get; set; } = null!;

	/// <summary>
	/// Cena, ze které se zlevnilo. Aktuální cena je na <see cref="Listing"/>.
	/// </summary>
	public decimal PreviousPrice { get; set; }
}