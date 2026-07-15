using RealityScraper.Application.Features.Scraping.Model.Report;

namespace RealityScraper.Application.Features.Scraping.Builders;

public class ScraperResultBuilder
{
	private readonly string siteName;
	private readonly List<ListingItem> newListings = new();
	private readonly List<ListingItemWithNewPrice> priceChangedListings = new();
	private int totalCount = 0;

	public ScraperResultBuilder(string siteName)
	{
		this.siteName = siteName;
	}

	public ScraperResultBuilder AddNewListing(ListingItem listing)
	{
		newListings.Add(listing);
		return this;
	}

	public ScraperResultBuilder AddPriceChangedListing(ListingItemWithNewPrice listing)
	{
		priceChangedListings.Add(listing);
		return this;
	}

	/// <summary>
	/// Započítá jeden unikátní scrapovaný inzerát do celkového počtu (nový, se změnou ceny i beze změny).
	/// </summary>
	public ScraperResultBuilder IncrementTotalCount()
	{
		totalCount++;
		return this;
	}

	public PortalReport Build()
	{
		return new PortalReport
		{
			SiteName = siteName,
			TotalListingsCount = totalCount,
			NewListings = new List<ListingItem>(newListings),
			PriceChangedListings = new List<ListingItemWithNewPrice>(priceChangedListings)
		};
	}
}