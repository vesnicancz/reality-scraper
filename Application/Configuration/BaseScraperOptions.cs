namespace RealityScraper.Application.Configuration;

public class BaseScraperOptions
{
	public required string ListingSelector { get; set; }

	public required string DetailLinkSelector { get; set; }

	public required string TitleSelector { get; set; }

	public required string PriceSelector { get; set; }

	public required string LocationSelector { get; set; }

	public required string ImageSelector { get; set; }

	public required string NextPageSelector { get; set; }

	/// <summary>
	/// Ověří, že jsou vyplněné všechny povinné CSS selektory. Volá se při startu
	/// (ValidateOnStart), aby chybná konfigurace selhala hned, ne až ve scrape smyčce.
	/// </summary>
	public virtual bool HasRequiredSelectors()
	{
		return !string.IsNullOrWhiteSpace(ListingSelector)
			&& !string.IsNullOrWhiteSpace(DetailLinkSelector)
			&& !string.IsNullOrWhiteSpace(TitleSelector)
			&& !string.IsNullOrWhiteSpace(PriceSelector)
			&& !string.IsNullOrWhiteSpace(LocationSelector)
			&& !string.IsNullOrWhiteSpace(ImageSelector)
			&& !string.IsNullOrWhiteSpace(NextPageSelector);
	}
}
